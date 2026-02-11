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

def initialize_wave(width, height, tiles):
    return [[set(tiles) for _ in range(width)] for _ in range(height)]

def find_lowest_entropy_cell(wave):
    min_entropy = float('inf')
    candidates = []

    for y, row in enumerate(wave):
        for x, cell in enumerate(row):
            if 1 < len(cell) < min_entropy:
                min_entropy = len(cell)
                candidates = [(x, y)]
            elif len(cell) == min_entropy:
                candidates.append((x, y))

    return random.choice(candidates) if candidates else None

def collapse_cell(wave, x, y):
    chosen = random.choice(list(wave[y][x]))
    wave[y][x] = {chosen}
    return chosen

def propagate(wave, model, start_x, start_y):
    stack = [(start_x, start_y)]

    while stack:
        x, y = stack.pop()
        tile = next(iter(wave[y][x]))

        for dx, dy, direction, opposite in [
            (1, 0, "right", "left"),
            (-1, 0, "left", "right"),
            (0, 1, "down", "up"),
            (0, -1, "up", "down"),
        ]:
            nx, ny = x + dx, y + dy
            if 0 <= nx < len(wave[0]) and 0 <= ny < len(wave):
                allowed = model.allowed.get((tile, direction), set())
                before = wave[ny][nx]
                after = before & allowed

                if not after:
                    return False  # contradiction

                if after != before:
                    wave[ny][nx] = after
                    stack.append((nx, ny))

    return True

def generate_once(model, width, height):
    wave = initialize_wave(width, height, model.tiles)

    while True:
        cell = find_lowest_entropy_cell(wave)
        if not cell:
            break

        x, y = cell
        collapse_cell(wave, x, y)

        if not propagate(wave, model, x, y):
            return None

    # Success
    return [[next(iter(cell)) for cell in row] for row in wave]

def generate(model, width, height, max_attempts=100):
    for _ in range(max_attempts):
        level = generate_once(model, width, height)
        if level:
            return level

    raise RuntimeError("WFC failed after max attempts")

class WFCModel:
    def __init__(self):
        self.tiles = set()
        self.allowed = defaultdict(set)

    def train(self, levels):
        for level in levels:
            h = len(level)
            w = len(level[0])

            for y in range(h):
                for x in range(w):
                    tile = level[y][x]
                    self.tiles.add(tile)

                    if x < w - 1:
                        right = level[y][x + 1]
                        self.allowed[(tile, "right")].add(right)
                        self.allowed[(right, "left")].add(tile)

                    if y < h - 1:
                        down = level[y + 1][x]
                        self.allowed[(tile, "down")].add(down)
                        self.allowed[(down, "up")].add(tile)

    def generate_level(model, width, height):
        wave = initialize_wave(width, height, model.tiles)

        while True:
            cell = find_lowest_entropy_cell(wave)
            if not cell:
                break  # fully collapsed

            x, y = cell
            collapse_cell(wave, x, y)

            if not propagate(wave, model, x, y):
                return None  # failure, restart

        return [[next(iter(cell)) for cell in row] for row in wave]



    
# Loading the level data from text files
script_dir = Path(__file__).parent# Get the directory of the current script
level_dir = script_dir.parent.parent / "Levels" / "TrainingLevels"# Get the path to the training data directory assuming the script is in "res://Scripts/LevelGenerators/" and the levels are in "res://Levels/TrainingLevels/"
level_paths = list(level_dir.glob("*.txt"))# Get all text files in the directory
raw_levels = [load_levels_from_txt(p) for p in level_paths]# Load the raw levels
max_width, max_height = get_max_dimensions(raw_levels)# Get the maximum dimensions of the levels
all_levels = [pad_level(level, max_width, max_height) for level in raw_levels]# Pad all levels to the maximum dimensions
print_level(all_levels[0])  # Print the first padded level for debugging

# Train the model
generator = WFCModel()
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