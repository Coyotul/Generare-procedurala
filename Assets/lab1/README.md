# Lab 1 - Python

Cerinte implementate:
- Grid de culori alese aleator (RGB)
- Harta 2D aleatoare cu simboluri:
  - `~` apa
  - `#` munti
  - `.` campii

## Rulare

Din folderul `lab1`:

```bash
python lab1_random_generators.py
```

Exemplu cu parametri:

```bash
python lab1_random_generators.py --rows 12 --cols 30 --water 0.4 --mountain 0.2 --plains 0.4 --seed 42
```

## Output

Scriptul afiseaza in consola si salveaza fisiere text in `output/`:
- `random_color_grid.txt`
- `random_terrain_map.txt`
