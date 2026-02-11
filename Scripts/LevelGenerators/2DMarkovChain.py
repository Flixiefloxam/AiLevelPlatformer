from collections import defaultdict
import random
from pathlib import Path

def load_levels_from_txt(file_path):
    with open(file_path, 'r') as f:
        lines = f.read().splitlines()
    return [list(line) for line in lines if line.strip()]

def save_level_to_file(level, file_path):
    with open(file_path, 'w') as f:
        for row in level:
            f.write(''.join(row) + '\n')

def get_max_dimensions(levels):
    max_width = 0
    max_height = 0

    for level in levels:
        max_height = max(max_height, len(level))
        for row in level:
            max_width = max(max_width, len(row))
    return max_width, max_height

def pad_level(level, target_width, target_height, pad_char='-'):
    padded_level = []

    for row in level:
        row = row[:target_width]  # Truncate if longer than target width (Shouldn't happen as I pad to max width and height in training data)
        padded_row = row + [pad_char] * (target_width - len(row))  # Pad to target width
        padded_level.append(padded_row)

    # Pad the level from the top if it's shorter than target height
    while len(padded_level) < target_height:
        padded_level.insert(0, [pad_char] * target_width)

    return padded_level

def print_level(level):
    for row in level:
        print(''.join(row))
    print()

class Markov2DLevelGenerator:
    def __init__(self):
        self.model = defaultdict(list)
        self.first_row_model = defaultdict(list)
        self.first_col_model = defaultdict(list)

    def train(self, levels):
        for level in levels:
            height = len(level)
            width = len(level[0])

            # Train first row (1D)
            for x in range(width - 2):
                context = (level[0][x], level[0][x + 1])
                self.first_row_model[context].append(level[0][x + 2])

            # Train first column (1D)
            for y in range(height - 2):
                context = (level[y][0], level[y + 1][0])
                self.first_col_model[context].append(level[y + 2][0])

            # Train full 2D contexts
            for y in range(1, height):
                for x in range(1, width):
                    context = (
                        level[y - 1][x - 1],
                        level[y - 1][x],
                        level[y][x - 1]
                    )
                    self.model[context].append(level[y][x])

    
    def generate_level(self, width, height, pad_char='-'):
        level = [[pad_char for _ in range(width)] for _ in range(height)]

        # ---- Seed first row using 1D Markov ----
        start = random.choice(list(self.first_row_model.keys()))
        level[0][0], level[0][1] = start

        for x in range(2, width):
            context = (level[0][x - 2], level[0][x - 1])
            options = self.first_row_model.get(context)
            level[0][x] = random.choice(options) if options else pad_char

        # ---- Seed first column using 1D Markov ----
        start = random.choice(list(self.first_col_model.keys()))
        level[0][0], level[1][0] = start

        for y in range(2, height):
            context = (level[y - 2][0], level[y - 1][0])
            options = self.first_col_model.get(context)
            level[y][0] = random.choice(options) if options else pad_char

        # ---- Fill the rest using 2D Markov ----
        for y in range(1, height):
            for x in range(1, width):
                context = (
                    level[y - 1][x - 1],
                    level[y - 1][x],
                    level[y][x - 1]
                )
                options = self.model.get(context)
                level[y][x] = random.choice(options) if options else pad_char

        return level


    
# Loading the level data from text files
script_dir = Path(__file__).parent# Get the directory of the current script
level_dir = script_dir.parent.parent / "Levels" / "TrainingLevels"# Get the path to the training data directory assuming the script is in "res://Scripts/LevelGenerators/" and the levels are in "res://Levels/TrainingLevels/"
level_paths = list(level_dir.glob("*.txt"))# Get all text files in the directory
raw_levels = [load_levels_from_txt(p) for p in level_paths]# Load the raw levels
max_width, max_height = get_max_dimensions(raw_levels)# Get the maximum dimensions of the levels
all_levels = [pad_level(level, max_width, max_height) for level in raw_levels]# Pad all levels to the maximum dimensions
print_level(all_levels[0])  # Print the first padded level for debugging

# Train the model
generator = Markov2DLevelGenerator()
generator.train(all_levels)

# Generate a new level
new_level = generator.generate_level(max_width, max_height)

# Print the generated level
print_level(new_level)

# Saving the generated level
output_dir = script_dir.parent.parent / "Levels" / "GeneratedLevels"
output_dir.mkdir(parents=True, exist_ok=True)  # Ensure the output directory exists

output_dir = output_dir / 'GeneratedLevel.txt'
print(f"Saving generated level to {output_dir}")
save_level_to_file(new_level, output_dir)