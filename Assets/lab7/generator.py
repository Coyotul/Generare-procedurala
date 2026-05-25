

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
