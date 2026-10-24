# DUOVEX v0.7.2 Playfield Specification

Reference resolution: 1280 × 720. Unity uGUI CanvasScaler: `ScaleWithScreenSize`, `Expand`.
Coordinates are pixels from the top-left of the design area.

| Element | X | Y | Width | Height |
|---|---:|---:|---:|---:|
| Gameplay gear (3 lanes) | 496 | 78 | 288 | 508 |
| Left D / WHITE lane | 496 | 78 | 96 | 508 |
| Center F/J / SHARED lane | 592 | 78 | 96 | 508 |
| Right K / BLACK lane | 688 | 78 | 96 | 508 |
| Gear plate (visual backdrop) | 482 | 69 | 316 | 571 |
| Judgment line | 496 | 582 | 288 | 8 |
| TAP (per lane) | lane X + 6 | note center Y - 8 | 84 | 16 |
| Key backgrounds | D=96, F=48, J=48, K=96 | 595 | lane/half-lane - 4 | 30 |

At the scheduled note time, the note **center** is at judgment line center Y=586.
F/J share one physical center lane; DUAL is displayed as one diagonal-split sprite.
Simultaneous center HOLDs display as two half-width bodies.

The renderer uses the scene's saved `Game_Lane_0..2` RectTransforms. Moving a
lane in the Inspector still moves its rendered notes; static gear plates, key
backgrounds, and hit line should be adjusted together to maintain alignment.

Fresh generation uses `DuovexTheme.noteHeight=16` and `noteSideInset=6`.
Pre-existing theme assets retain their custom values until intentionally updated
with `Tools > Duovex > Apply v0.7.2 Note Metrics to Theme`.

The editor's larger 3 × 178 px note grid remains unchanged in this release.
