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
    print()  # Newline for better readability

class MarkovLevelGenerator:
    def __init__(self, n=3):
        self.n = n
        self.model = defaultdict(list)

    def train(self, levels):
        # levels: list of 2D lists (characters)
        for level in levels:
            for row in level:
                for i in range(len(row) - self.n):
                    gram = tuple(row[i:i + self.n - 1])  # context (n-1)
                    next_tile = row[i + self.n - 1]       # target tile
                    self.model[gram].append(next_tile)

    def generate_row(self, length):
        # Pick a random starting gram
        start = random.choice(list(self.model.keys()))
        result = list(start)

        for _ in range(length - len(start)):
            context = tuple(result[-(self.n - 1):])
            options = self.model.get(context)
            if not options:
                break  # Dead-end
            next_tile = random.choice(options)
            result.append(next_tile)
        return result

    def generate_level(self, width, height):
        return [self.generate_row(width) for _ in range(height)]
    
# Loading the level data from text files
script_dir = Path(__file__).parent# Get the directory of the current script
level_dir = script_dir.parent.parent / "Levels" / "TrainingLevels"# Get the path to the training data directory assuming the script is in "res://Scripts/LevelGenerators/" and the levels are in "res://Levels/TrainingLevels/"
level_paths = list(level_dir.glob("*.txt"))# Get all text files in the directory
raw_levels = [load_levels_from_txt(p) for p in level_paths]# Load the raw levels
max_width, max_height = get_max_dimensions(raw_levels)# Get the maximum dimensions of the levels
all_levels = [pad_level(level, max_width, max_height) for level in raw_levels]# Pad all levels to the maximum dimensions
print_level(all_levels[0])  # Print the first padded level for debugging

# Train the model
generator = MarkovLevelGenerator(n=3)
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