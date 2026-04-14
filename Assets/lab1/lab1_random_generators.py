#!/usr/bin/env python3
"""
Lab 1 - Python
1) Generate a random color grid.
2) Generate a random 2D terrain map with symbols:
   ~ water, # mountains, . plains
"""

from __future__ import annotations

import argparse
import random
from pathlib import Path
from typing import List, Sequence, Tuple

RGB = Tuple[int, int, int]


def generate_random_color_grid(rows: int, cols: int, rng: random.Random) -> List[List[RGB]]:
    grid: List[List[RGB]] = []
    for _ in range(rows):
        row: List[RGB] = []
        for _ in range(cols):
            row.append((rng.randint(0, 255), rng.randint(0, 255), rng.randint(0, 255)))
        grid.append(row)
    return grid


def weighted_choice(symbols: Sequence[str], weights: Sequence[float], rng: random.Random) -> str:
    total = sum(weights)
    pick = rng.uniform(0.0, total)
    current = 0.0
    for symbol, weight in zip(symbols, weights):
        current += weight
        if pick <= current:
            return symbol
    return symbols[-1]


def generate_terrain_map(
    rows: int,
    cols: int,
    water_probability: float,
    mountain_probability: float,
    plains_probability: float,
    rng: random.Random,
) -> List[List[str]]:
    symbols = ("~", "#", ".")
    weights = (water_probability, mountain_probability, plains_probability)

    terrain: List[List[str]] = []
    for _ in range(rows):
        row: List[str] = []
        for _ in range(cols):
            row.append(weighted_choice(symbols, weights, rng))
        terrain.append(row)
    return terrain


def save_color_grid_as_text(color_grid: List[List[RGB]], output_path: Path) -> None:
    lines = []
    for row in color_grid:
        cell_values = [f"({r:3d},{g:3d},{b:3d})" for r, g, b in row]
        lines.append(" ".join(cell_values))
    output_path.write_text("\n".join(lines), encoding="utf-8")


def save_terrain_map_as_text(terrain_map: List[List[str]], output_path: Path) -> None:
    lines = ["".join(row) for row in terrain_map]
    output_path.write_text("\n".join(lines), encoding="utf-8")


def print_color_grid(color_grid: List[List[RGB]]) -> None:
    print("Random Color Grid (RGB):")
    for row in color_grid:
        print(" ".join(f"({r:3d},{g:3d},{b:3d})" for r, g, b in row))


def print_terrain_map(terrain_map: List[List[str]]) -> None:
    print("\nRandom 2D Terrain Map:")
    print("Legend: ~ = water, # = mountains, . = plains")
    for row in terrain_map:
        print("".join(row))


def validate_probabilities(water: float, mountain: float, plains: float) -> None:
    if water < 0 or mountain < 0 or plains < 0:
        raise ValueError("Probabilities must be non-negative.")

    total = water + mountain + plains
    if total <= 0:
        raise ValueError("Sum of probabilities must be > 0.")


def build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Lab1 random generators for colors and terrain map.")
    parser.add_argument("--rows", type=int, default=10, help="Number of rows.")
    parser.add_argument("--cols", type=int, default=20, help="Number of columns.")
    parser.add_argument("--water", type=float, default=0.35, help="Water probability.")
    parser.add_argument("--mountain", type=float, default=0.25, help="Mountain probability.")
    parser.add_argument("--plains", type=float, default=0.40, help="Plains probability.")
    parser.add_argument("--seed", type=int, default=None, help="Random seed for reproducible output.")
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path("output"),
        help="Directory where text outputs are saved.",
    )
    return parser


def main() -> None:
    parser = build_arg_parser()
    args = parser.parse_args()

    if args.rows <= 0 or args.cols <= 0:
        raise ValueError("Rows and columns must be > 0.")

    validate_probabilities(args.water, args.mountain, args.plains)

    rng = random.Random(args.seed)

    color_grid = generate_random_color_grid(args.rows, args.cols, rng)
    terrain_map = generate_terrain_map(
        args.rows,
        args.cols,
        args.water,
        args.mountain,
        args.plains,
        rng,
    )

    print_color_grid(color_grid)
    print_terrain_map(terrain_map)

    output_dir: Path = args.output_dir
    output_dir.mkdir(parents=True, exist_ok=True)

    save_color_grid_as_text(color_grid, output_dir / "random_color_grid.txt")
    save_terrain_map_as_text(terrain_map, output_dir / "random_terrain_map.txt")

    print(f"\nSaved files in: {output_dir.resolve()}")


if __name__ == "__main__":
    main()
