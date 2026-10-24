# Playfield specification · v0.7.1

Reference screen space: 1280 × 720, uGUI CanvasScaler `ScaleWithScreenSize`, `Expand`.

| Object | Left X | Top Y | Width | Height |
|---|---:|---:|---:|---:|
| Gear (3 lanes) | 460 | 78 | 360 | 508 |
| Lane 1 (D, WHITE) | 460 | 78 | 120 | 508 |
| Lane 2 (F/J, SHARED) | 580 | 78 | 120 | 508 |
| Lane 3 (K, BLACK) | 700 | 78 | 120 | 508 |
| Hit line | 460 | 582 | 360 | 8 |
| Note (TAP) | lane left + 4 | note center - 9 | lane width - 8 | 18 |
| Key labels | 460…820 | 596 | D/F/J/K | 30 |

Judgment center: Y = 586, and a note centered at Y = 586 is exactly on the timing point. For HOLD, the rectangular body extends from head center to tail center and is masked inside the gear. DUAL TAP is one split diagonal sprite. When F and J are both HOLD, the bodies are rendered as side-by-side halves.

The note renderer follows `Game_Lane_0..2` individually (and the editor's own corresponding lanes), so moving a saved lane RectTransform updates note positions without modifying timing logic. For consistent layouts change all related objects together or regenerate the v0.7.1 scene. Additional custom designs may require moving key backgrounds and the gear plate to match.
