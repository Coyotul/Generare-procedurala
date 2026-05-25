# Laboratorul 7 — GAN Generator pentru terenuri (Python / PyTorch)

## Cerinta laboratorului

Generati 5 harti aleatorii de teren folosind o retea Generator:
- Initializati un Generator
- Generati 5 terenuri diferite folosind vectori latenti aleatorii
- Vizualizati cele 5 terenuri intr-o grila
- Salvati imaginea rezultata

Bonus. Interpolati doi vectori latenti:
- Generati doi vectori latenti aleatorii (A si B)
- Interpolati intre ei in 10 pasi
- Vizualizati tranzitia de la A la B
- Salvati imaginea rezultata

---

## Arhitectura

Generator (MLP fully connected, conform laboratorului):

```
Input:  z = vector 100D (sample din N(0, 1))
Linear(100  -> 256)   + ReLU
Linear(256  -> 512)   + ReLU
Linear(512  -> 1024)  + ReLU
Linear(1024 -> 4096)  + Tanh         (output in [-1, +1])
reshape -> (64, 64)
```

Total: ~4.88 mil parametri. Reteaua e neantrenata - foloseste greutati random
initiate de PyTorch (Kaiming uniform). Output-ul nu reprezinta teren realist,
demonstreaza doar mapping-ul `latent -> imagine` ca functie continua si
deterministica.

---

## Cum functioneaza

### Sample vectori latenti

```python
z = torch.randn(5, 100)   # 5 vectori 100D din distributie normala standard
```

### Forward pass

```python
gen.eval()
with torch.no_grad():
    terrains = gen(z)     # (5, 64, 64) tensor in [-1, 1]
```

`torch.no_grad()` dezactiveaza autograd pe durata blocului - nu antrenam, nu
vrem grafurile de calcul.

`.cpu().numpy()` converteste tensor PyTorch -> array NumPy pentru matplotlib.

### Vizualizare

Mapez `[-1, +1] -> [0, 1]` prin `(t + 1) * 0.5`, apoi `imshow` cu `cmap='terrain'`
(paleta topografica blue-green-brown-white) si `interpolation='bicubic'` pentru
netezire la marire.

`fig.savefig(path, dpi=140, bbox_inches='tight')` salveaza PNG. `plt.close(fig)`
elibereaza memoria.

### Interpolare A->B (Bonus)

```python
z_a = torch.randn(1, 100)
z_b = torch.randn(1, 100)
alphas = torch.linspace(0, 1, 10).view(-1, 1)        # (10, 1)
z_interp = (1 - alphas) * z_a + alphas * z_b          # (10, 100) prin broadcasting
terrains = gen(z_interp)                              # (10, 64, 64)
```

Formula `z(α) = (1−α)·z_A + α·z_B` traseaza o dreapta in spatiul latent 100D.
Generator-ul fiind o functie continua, output-ul `G(z(α))` formeaza o
traiectorie neteda prin spatiul imaginilor - vezi tranzitie "topit" intre A si B.

### De ce Tanh la output

`Tanh` produce valori in `[-1, +1]` simetric in jurul lui 0:
- `-1` = adancime maxima (ocean adanc)
- ` 0` = nivelul marii
- `+1` = varful muntelui

### De ce 100D pentru latent

Standard de facto pentru GAN-uri mici. Mai mare = mai multa expresivitate
potentiala, mai mic = mai putine variatii distincte. 100D ofera suficient spatiu
fara overhead inutil.

### Reproductibilitate

```python
torch.manual_seed(args.seed)
np.random.seed(args.seed)
```
Acelasi seed = aceeasi secventa de `torch.randn` = aceleasi 5 terenuri si aceeasi
tranzitie A->B.

---

## Setup si rulare

```powershell
cd "Assets\lab7"
python -m venv venv
venv\Scripts\activate
pip install -r requirements.txt

python generate.py                       # default seed 2026
python generate.py --seed 42 --cmap gray # alt seed, alt colormap
```

Output:
- `output/terrains_grid.png` - 5 terenuri (Task)
- `output/interpolation.png` - 10 cadre A->B (Bonus)

---

## Cod sursa

### Assets/lab7/generator.py

```python
"""
Generator network for procedural terrain heightmaps.

Architecture (Lab 7 spec):
    latent (100) -> 256 -> 512 -> 1024 -> 64*64
    ReLU on hidden layers, Tanh on output (range [-1, 1])
"""

import torch
import torch.nn as nn


class Generator(nn.Module):
    def __init__(self, latent_dim: int = 100, output_size: int = 64):
        super().__init__()
        self.latent_dim = latent_dim
        self.output_size = output_size

        self.model = nn.Sequential(
            nn.Linear(latent_dim, 256),
            nn.ReLU(inplace=True),
            nn.Linear(256, 512),
            nn.ReLU(inplace=True),
            nn.Linear(512, 1024),
            nn.ReLU(inplace=True),
            nn.Linear(1024, output_size * output_size),
            nn.Tanh(),
        )

    def forward(self, z: torch.Tensor) -> torch.Tensor:
        """z: (B, latent_dim) -> (B, output_size, output_size) in [-1, 1]"""
        flat = self.model(z)
        return flat.view(-1, self.output_size, self.output_size)
```

### Assets/lab7/generate.py

```python
"""
Lab 7 - Procedural terrain generation with a GAN Generator (PyTorch).

Tasks:
  1) Initialize a Generator.
  2) Sample 5 random latent vectors and produce 5 terrains.
  3) Visualize them side-by-side in a grid and save the image.

Bonus:
  - Sample two latent vectors A and B.
  - Interpolate linearly in 10 steps between them.
  - Visualize the transition and save the image.

Usage:
    python generate.py
    python generate.py --seed 42 --cmap terrain
"""

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
```

### Assets/lab7/requirements.txt

```
torch>=2.0
numpy>=1.24
matplotlib>=3.7
```

---

## Concepte cheie de retinut

1. **Generator** = retea care mapa `latent -> imagine`. Spre deosebire de noise
   matematic (Perlin), reteaua e o functie complexa cu milioane de parametri.

2. **Spatiu latent** = spatiu vectorial abstract unde fiecare punct corespunde
   unei imagini posibile. Componentele individuale nu au semnificatie semantica,
   doar combinatia lor genereaza variatii.

3. **Continuitate** = `G` e o functie continua a parametrilor. `z` aproape =
   `G(z)` aproape. De aici vine interpolarea neteda in spatiul latent.

4. **Tanh la output** = constrange valorile in [-1, +1], potrivit pentru
   heightmap normalizat.

5. **`torch.no_grad()`** = dezactiveaza autograd la inferenta. Economiseste
   memorie cand nu antrenezi.

6. **Broadcasting in PyTorch** = `(10, 1) * (1, 100) = (10, 100)`. Permite
   interpolarea elegant cu o singura linie.

7. **Inferenta cu greutati random** = produs comportament *consistent* dar
   *abstract*. Demonstreaza arhitectura, nu calitatea generative.
