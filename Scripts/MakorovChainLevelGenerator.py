from collections import defaultdict
import random
from pathlib import Path

def load_levels_from_txt(file_path):
    with open(file_path, 'r') as f:
        lines = f.read().splitlines()
    return [list(line) for line in lines if line.strip()]

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

    def generate_level(self, width=16, height=14):
        return [self.generate_row(width) for _ in range(height)]
    
# Loading the level data from text files
script_dir = Path(__file__).parent# Get the directory of the current script
level_dir = script_dir.parent / "Assets" / "TrainingData"# Path to the training data directory
level_paths = list(level_dir.glob("*.txt"))# Get all text files in the directory
all_levels = [load_levels_from_txt(p) for p in level_paths]# load the levels from each file

# Train the model
generator = MarkovLevelGenerator(n=3)
generator.train(all_levels)

# Generate a new level
new_level = generator.generate_level(width=16, height=14)

# Print the level
for row in new_level:
    print(''.join(row))

def save_level_to_file(level, file_path):
    with open(file_path, 'w') as f:
        for row in level:
            f.write(''.join(row) + '\n')

save_level_to_file(new_level, 'generated_level.txt')