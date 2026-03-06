import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data import Dataset, DataLoader
from pathlib import Path
import random
import math


class LevelDataset(Dataset):
    def __init__(self, level_dir):
        self.levels = []
        self.tile_to_idx = {}
        self.idx_to_tile = {}

        level_paths = list(Path(level_dir).glob("*.txt"))
        raw_levels = [self.load_level(p) for p in level_paths]

        self.max_height = max(len(l) for l in raw_levels)
        self.max_width = max(len(l[0]) for l in raw_levels)

        padded_levels = [self.pad_level(l) for l in raw_levels]

        self.build_vocab(padded_levels)
        self.levels = [self.encode_level(l) for l in padded_levels]

    def load_level(self, path):
        with open(path, 'r') as f:
            lines = f.read().splitlines()
        return [list(line) for line in lines if line.strip()]

    def pad_level(self, level):
        padded = []

        for row in level:
            row = row + ['-'] * (self.max_width - len(row))
            padded.append(row)

        for _ in range(self.max_height - len(level)):
            padded.append(['-'] * self.max_width)

        return padded

    def build_vocab(self, levels):
        tiles = set()
        for level in levels:
            for row in level:
                tiles.update(row)

        self.tile_to_idx = {t: i for i, t in enumerate(sorted(tiles))}
        self.idx_to_tile = {i: t for t, i in self.tile_to_idx.items()}

    def encode_level(self, level):
        return torch.tensor(
            [[self.tile_to_idx[t] for t in row] for row in level],
            dtype=torch.long
        )

    def __len__(self):
        return len(self.levels)

    def __getitem__(self, idx):
        level = self.levels[idx]

        # Autoregressive target: shift right
        input_level = level[:, :-1]
        target_level = level[:, 1:]

        return input_level, target_level


class LevelCNN(nn.Module):
    def __init__(self, vocab_size, embedding_dim=32):
        super().__init__()

        self.embedding = nn.Embedding(vocab_size, embedding_dim)

        self.conv1 = nn.Conv2d(embedding_dim, 64, kernel_size=3, padding=1)
        self.conv2 = nn.Conv2d(64, 64, kernel_size=3, padding=1)
        self.conv3 = nn.Conv2d(64, vocab_size, kernel_size=1)

    def forward(self, x):
        # x shape: (batch, H, W)
        x = self.embedding(x)  # (batch, H, W, embed)
        x = x.permute(0, 3, 1, 2)  # (batch, embed, H, W)

        x = F.relu(self.conv1(x))
        x = F.relu(self.conv2(x))
        x = self.conv3(x)

        return x  # (batch, vocab_size, H, W)


def train(model, dataloader, dataset, epochs=50, lr=0.001):
    optimizer = torch.optim.Adam(model.parameters(), lr=lr)

    flat = torch.cat([lvl.flatten() for lvl in dataset.levels])
    counts = torch.bincount(flat)

    weights = 1.0 / (counts.float() + 1e-6)
    weights = weights / weights.sum() * len(weights)

    class_weights = weights

    for epoch in range(epochs):
        total_loss = 0

        for inputs, targets in dataloader:
            outputs = model(inputs)

            loss = F.cross_entropy(outputs, targets, weight=class_weights)

            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

            total_loss += loss.item()

        print(f"Epoch {epoch+1}, Loss: {total_loss:.4f}")


def generate_level(model, dataset):
    model.eval()

    height = dataset.max_height
    width = dataset.max_width

    level = torch.zeros((1, height, width), dtype=torch.long)

    seed_level = random.choice(dataset.levels)
    level[0, :, 0] = seed_level[:, 0]

    for x in range(width - 1):
        output = model(level)
        probs = F.softmax(output[0, :, :, x], dim=0)

        for y in range(height):
            tile = torch.multinomial(probs[:, y], 1)
            level[0, y, x+1] = tile

    return level[0]


def print_level(level_tensor, dataset):
    for row in level_tensor:
        row_tiles = [dataset.idx_to_tile[int(t)] for t in row]
        print(''.join(row_tiles))
    print()

def save_level(level_tensor, dataset, path):
    with open(path, 'w') as f:
        for row in level_tensor:
            row_tiles = [dataset.idx_to_tile[int(t)] for t in row]
            f.write(''.join(row_tiles) + '\n')


if __name__ == "__main__":

    script_dir = Path(__file__).parent
    level_dir = script_dir.parent.parent / "Levels" / "TrainingLevels"

    dataset = LevelDataset(level_dir)
    dataloader = DataLoader(dataset, batch_size=2, shuffle=True)

    print("Levels:", len(dataset))
    print("Level shape:", dataset[0][0].shape)
    print("Vocab size:", len(dataset.tile_to_idx))

    model = LevelCNN(vocab_size=len(dataset.tile_to_idx))

    train(model, dataloader, dataset, epochs=100)

    generated = generate_level(model, dataset)

    print("\nGenerated Level:\n")
    print_level(generated, dataset)

    output_dir = script_dir.parent.parent / "Levels" / "GeneratedLevels"
    output_dir.mkdir(parents=True, exist_ok=True)

    output_path = output_dir / "GeneratedLevel.txt"
    save_level(generated, dataset, output_path)

    print("Saved generated level to", output_path)