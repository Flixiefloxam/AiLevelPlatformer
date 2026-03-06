from collections import defaultdict
import random
from pathlib import Path

class OverlappingWFC2x2:
    def __init__(self):
        self.patterns = []
        self.pattern_set = set()
        self.compatible = {}  # pattern_index -> allowed neighbors per direction
        self.weights = []

    def extract_patterns(self, levels):
        pattern_counts = {}

        for level in levels:
            height = len(level)
            width = len(level[0])

            for y in range(height - 1):
                for x in range(width - 1):
                    pattern = (
                        level[y][x], level[y][x+1],
                        level[y+1][x], level[y+1][x+1]
                    )
                    pattern_counts[pattern] = pattern_counts.get(pattern, 0) + 1

        self.patterns = list(pattern_counts.keys())
        self.weights = [pattern_counts[p] for p in self.patterns]

    def build_compatibility(self):
        self.compatible = {}

        for i, p in enumerate(self.patterns):
            self.compatible[i] = {
                "right": set(),
                "left": set(),
                "down": set(),
                "up": set()
            }

        for i, p1 in enumerate(self.patterns):
            for j, p2 in enumerate(self.patterns):

                # Right compatibility
                if (p1[1], p1[3]) == (p2[0], p2[2]):
                    self.compatible[i]["right"].add(j)

                # Left compatibility
                if (p1[0], p1[2]) == (p2[1], p2[3]):
                    self.compatible[i]["left"].add(j)

                # Down compatibility
                if (p1[2], p1[3]) == (p2[0], p2[1]):
                    self.compatible[i]["down"].add(j)

                # Up compatibility
                if (p1[0], p1[1]) == (p2[2], p2[3]):
                    self.compatible[i]["up"].add(j)

    def initialize_wave(self, width, height):
        return [[set(range(len(self.patterns)))
                 for _ in range(width)]
                 for _ in range(height)]

    def get_lowest_entropy_cell(self, wave):
        min_entropy = float("inf")
        target = None

        for y in range(len(wave)):
            for x in range(len(wave[0])):
                options = wave[y][x]
                if 1 < len(options) < min_entropy:
                    min_entropy = len(options)
                    target = (x, y)

        return target
    
    def pattern_bottom_solid(self, pattern_index):
        p = self.patterns[pattern_index]
        # bottom row of pattern = p[2], p[3]
        return p[2] != '-' or p[3] != '-'

    def collapse(self, wave, x, y):
        options = list(wave[y][x])

        weights = []
        height = len(wave)

        for i in options:
            weight = self.weights[i]

            # Vertical bias
            if y > height * 0.7:  # bottom 30%
                if self.pattern_bottom_solid(i):
                    weight *= 3
            elif y < height * 0.3:  # top 30%
                if not self.pattern_bottom_solid(i):
                    weight *= 2

            weights.append(weight)

        choice = random.choices(options, weights=weights, k=1)[0]
        wave[y][x] = {choice}

    def propagate(self, wave, start_cells):
        from collections import deque

        queue = deque(start_cells)

        directions = [
            (1, 0, "right", "left"),
            (-1, 0, "left", "right"),
            (0, 1, "down", "up"),
            (0, -1, "up", "down")
        ]

        while queue:
            x, y = queue.popleft()

            for dx, dy, dir_to, dir_from in directions:
                nx, ny = x + dx, y + dy

                if 0 <= nx < len(wave[0]) and 0 <= ny < len(wave):

                    allowed = set()
                    for p in wave[y][x]:
                        allowed |= self.compatible[p][dir_to]

                    before = len(wave[ny][nx])
                    wave[ny][nx] &= allowed

                    if len(wave[ny][nx]) == 0:
                        return False  # contradiction

                    if len(wave[ny][nx]) < before:
                        queue.append((nx, ny))

        return True

    def generate(self, tile_width, tile_height, max_retries=50):

        pattern_width = tile_width - 1
        pattern_height = tile_height - 1

        for attempt in range(max_retries):

            wave = self.initialize_wave(pattern_width, pattern_height)

            scaffold = create_scaffold_level(tile_width, tile_height)
            self.constrain_wave_to_scaffold(wave, scaffold)

            if not self.propagate(wave, [(0,0)]):
                continue
            
            failed = False

            while True:
                cell = self.get_lowest_entropy_cell(wave)
                if cell is None:
                    break

                x, y = cell
                self.collapse(wave, x, y)
                success = self.propagate(wave, [(x, y)])

                if not success:
                    failed = True
                    break

                for row in wave:
                    for options in row:
                        if len(options) == 0:
                            failed = True
                            break
                    if failed:
                        break

                if failed:
                    break

            if not failed:
                return self.build_level_from_patterns(wave)

            print(f"Restarting (attempt {attempt+1})")

        raise Exception("Overlapping WFC failed after max retries.")
    
    def build_level_from_patterns(self, wave):
        height = len(wave)
        width = len(wave[0])

        level = [[None for _ in range(width+1)]
                 for _ in range(height+1)]

        for y in range(height):
            for x in range(width):
                pattern_index = next(iter(wave[y][x]))
                p = self.patterns[pattern_index]

                level[y][x] = p[0]
                level[y][x+1] = p[1]
                level[y+1][x] = p[2]
                level[y+1][x+1] = p[3]

        return level
    
    def constrain_wave_to_scaffold(self, wave, scaffold):
        for y in range(len(wave)):
            for x in range(len(wave[0])):
                
                allowed = set()
                
                for p_index in wave[y][x]:
                    p = self.patterns[p_index]
                    
                    # Pattern tiles
                    a, b, c, d = p
                    
                    if (
                        scaffold[y][x] in ('-', a) and
                        scaffold[y][x+1] in ('-', b) and
                        scaffold[y+1][x] in ('-', c) and
                        scaffold[y+1][x+1] in ('-', d)
                    ):
                        allowed.add(p_index)
                
                wave[y][x] = allowed

def generate_ground_heights(width, min_height, max_height, max_step=1):
    heights = []
    
    current = random.randint(min_height, max_height)
    
    for x in range(width):
        step = random.randint(-max_step, max_step)
        current += step
        current = max(min_height, min(max_height, current))
        heights.append(current)
    
    return heights

def create_scaffold_level(width, height):
    level = [['-' for _ in range(width)] for _ in range(height)]
    
    min_ground = int(height * 0.5)
    max_ground = int(height * 0.8)
    
    ground_heights = generate_ground_heights(width, min_ground, max_ground)
    
    for x in range(width):
        ground_y = ground_heights[x]
        
        for y in range(ground_y, height):
            level[y][x] = 'X'  # solid tile
    
    return level

    
# -------------------------
# Utility functions
# -------------------------

def load_levels_from_txt(file_path):
    with open(file_path, 'r') as f:
        lines = f.read().splitlines()
    return [list(line) for line in lines if line.strip()]

def save_level_to_file(level, file_path):
    with open(file_path, 'w') as f:
        for row in level:
            f.write(''.join(row) + '\n')

def print_level(level):
    for row in level:
        print(''.join(row))
    print()


# -------------------------
# Load Training Data
# -------------------------

script_dir = Path(__file__).parent
level_dir = script_dir.parent.parent / "Levels" / "TrainingLevels"

level_paths = list(level_dir.glob("*.txt"))
raw_levels = [load_levels_from_txt(p) for p in level_paths]

if not raw_levels:
    print("No training levels found.")
    exit()


# -------------------------
# Train Overlapping 2x2 WFC
# -------------------------

wfc = OverlappingWFC2x2()
wfc.extract_patterns(raw_levels)
print("Number of patterns:", len(wfc.patterns))
wfc.build_compatibility()

# -------------------------
# Generate Level
# -------------------------

tile_width = len(raw_levels[0][0])
tile_height = len(raw_levels[0])

new_level = wfc.generate(tile_width, tile_height)

print("Generated Level:\n")
print_level(new_level)

# -------------------------
# Save Output
# -------------------------

output_dir = script_dir.parent.parent / "Levels" / "GeneratedLevels"
output_dir.mkdir(parents=True, exist_ok=True)

output_path = output_dir / "GeneratedLevel.txt"
save_level_to_file(new_level, output_path)

print(f"Saved generated level to {output_path}")