

import argparse
import os

import matplotlib.pyplot as plt
import numpy as np
import torch

from generator import Generator


def parse_args():
    p = argparse.ArgumentParser()
    p.add_argument("--seed", type=int, default=2026, help="RNG seed for reproducibility.")
    p.add_argument("--latent-dim", type=int, default=100)
    p.add_argument("--output-dir", type=str, default="output")
    p.add_argument("--cmap", type=str, default="terrain",
                   help="Matplotlib colormap. 'terrain' = topographic blue-green-brown-white.")
    p.add_argument("--device", type=str, default="cpu")
    p.add_argument("--interp-steps", type=int, default=10,
                   help="Number of frames in the A->B latent interpolation (bonus).")
    return p.parse_args()


def normalize_for_display(arr: np.ndarray) -> np.ndarray:
    """Tanh output is in [-1, 1]. Map to [0, 1] for matplotlib."""
    return (arr + 1.0) * 0.5


def save_five_terrains(gen: Generator, args, device):
    with torch.no_grad():
        z = torch.randn(5, args.latent_dim, device=device)
        terrains = gen(z).cpu().numpy()

    fig, axes = plt.subplots(1, 5, figsize=(20, 4.5))
    for i, t in enumerate(terrains):
        img = normalize_for_display(t)
        axes[i].imshow(img, cmap=args.cmap, vmin=0.0, vmax=1.0, interpolation="bicubic")
        axes[i].set_title(f"Terrain {i + 1}", fontsize=12)
        axes[i].axis("off")

    fig.suptitle("Five terrains from random latent vectors", fontsize=14)
    fig.tight_layout()

    path = os.path.join(args.output_dir, "terrains_grid.png")
    fig.savefig(path, dpi=140, bbox_inches="tight")
    plt.close(fig)
    print(f"[generate] saved {path}")


def save_interpolation(gen: Generator, args, device):
    with torch.no_grad():
        z_a = torch.randn(1, args.latent_dim, device=device)
        z_b = torch.randn(1, args.latent_dim, device=device)
        alphas = torch.linspace(0.0, 1.0, args.interp_steps, device=device).view(-1, 1)
        z_interp = (1.0 - alphas) * z_a + alphas * z_b
        terrains = gen(z_interp).cpu().numpy()

    fig, axes = plt.subplots(1, args.interp_steps, figsize=(2.2 * args.interp_steps, 3.0))
    for i, t in enumerate(terrains):
        img = normalize_for_display(t)
        axes[i].imshow(img, cmap=args.cmap, vmin=0.0, vmax=1.0, interpolation="bicubic")
        alpha = i / max(1, args.interp_steps - 1)
        axes[i].set_title(f"α={alpha:.2f}", fontsize=10)
        axes[i].axis("off")

    fig.suptitle("Latent-space interpolation: A -> B", fontsize=14)
    fig.tight_layout()

    path = os.path.join(args.output_dir, "interpolation.png")
    fig.savefig(path, dpi=140, bbox_inches="tight")
    plt.close(fig)
    print(f"[generate] saved {path}")


def main():
    args = parse_args()
    os.makedirs(args.output_dir, exist_ok=True)

    torch.manual_seed(args.seed)
    np.random.seed(args.seed)
    device = torch.device(args.device)

    gen = Generator(latent_dim=args.latent_dim, output_size=64).to(device)
    gen.eval()

    save_five_terrains(gen, args, device)
    save_interpolation(gen, args, device)
    print("[generate] done.")


if __name__ == "__main__":
    main()
