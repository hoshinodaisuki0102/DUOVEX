# DUOVEX v0.7.4 UI & Playfield Specification

Master resolution: **1280×720**, origin is top-left. uGUI CanvasScaler:
ScaleWithScreenSize / Expand. All objects editable under `DUOVEX_UI`.

## Gameplay center geometry

| Element | X | Y (top) | W | H | Center Y |
|---|---:|---:|---:|---:|---:|
| Game lanes (3 × 96px) | 496 | 78 | 288 | 508 | – |
| Game_HitUnderlay | 494 | 580 | 292 | 12 | **586** |
| Game_HitLine (cyan) | 496 | 582 | 288 | 8 | **586** |
| Game_HitCore (white) | 496 | 585 | 288 | 2 | **586** |
| TAP note on-time | lane X + 6 | 578 | 84 | 16 | **586** |
| D/F/J/K key backgrounds | lane positions | 618 | 92/44/44/92 | 29 | 632.5 |

The *logical* perfect timing is when `SongTime == chart.BeatToSeconds(note.beat)`;
the note renderer computes `y = HitY - (noteTime - SongTime) × speed`, then
draws the note top at `y - noteHeight/2`. Therefore at exactly zero difference,
the note's geometric center equals the line center **Y=586**. The bright white
core and cyan judgment line share that center. Changing note speed must not
change judgment timestamps.

The key backgrounds begin **32px lower than the judgment center**, with a
28px visible gap below the 8px line (bottom=590, key top=618).
No vertical gameplay separator lines were added.

## Navigation buttons

| Screen | ID | X | Y | W | H |
|---|---|---:|---:|---:|---:|
| Main | B_Main_Play | 99 | 416 | 399 | 65 |
| Main | B_Main_Edit | 99 | 497 | 399 | 54 |
| Main | B_Main_Options | 99 | 567 | 399 | 54 |
| Select | B_Song_0..6 | 91 | 180 + 57×index | 671 | 48 |
| Select | B_Select_Action | 823 | 392 | 356 | 56 |
| Select | B_Select_Add | 823 | 465 | 356 | 56 |
| Select | B_Select_Back | 823 | 542 | 356 | 56 |

Buttons use native uGUI `Image` rectangles, colored strips, index numerals,
and arrows. Primary is bright cyan with dark lettering; secondary is deep navy
with white lettering. They are not flattened PNG images.

## Options

| Object | X | Y | W | H |
|---|---:|---:|---:|---:|
| Slider_Music | 153 | 268 | 570 | 23 |
| Slider_SFX | 153 | 358 | 570 | 23 |
| B_White_Preview | 154 | 406 | 235 | 38 |
| B_Black_Preview | 402 | 406 | 235 | 38 |

`DUOVEX.MusicVolume` (default 80%) and `DUOVEX.HitSoundVolume` (default 32%)
are stored separately in Unity PlayerPrefs. Music controls AudioSource.volume,
while hit sound controls the separate SFX AudioSource.

Generated in **DuovexV074.unity**. Re-running scene generation will not
overwrite that scene if it exists; rename it first if a fresh copy is desired.
