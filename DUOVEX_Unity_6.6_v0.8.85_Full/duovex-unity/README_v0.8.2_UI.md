# DUOVEX v0.8.2 — Angular UI Overhaul

**Basis:** DUOVEX v0.8.1 full Unity 6.6 project. All 12 built-in chart choices, KETSUDAN timing points, imported songs, audio files, ESC pause, music/SFX volume, and gameplay timing are retained.

## Getting started

1. Extract the **Full** ZIP and open the `duovex-unity` directory in Unity Hub (original Unity 6000.6.6f1 project).
2. Open `Assets/Scenes/Main.unity` and press Play: the runtime UI factory uses the new art and motion immediately.
3. For an editable serialized UI scene, use **Tools > Duovex > Generate Editable UI Scene**. This creates **`Assets/Scenes/DuovexV082.unity`**, leaving Main and earlier editable scenes untouched. Open the new scene and use it for further design work.
4. Run **Tools > Duovex > Run v0.8.2 Baseline Check** in the new scene.
5. Test START, CHART STUDIO, SETTINGS, song selection/scroll, Play, ESC pause/retry, music volume and the editor. These are expected to keep their pre-0.8.2 behavior.

## What changed

- Main Menu: geometric split black/white background, editable DV logo badge, three angular action buttons, subdued purple accents.
- Select: left featured-song information + per-track generated cover artwork, right seven scrollable song cards, selected/hovered styles.
- Buttons: `DuovexButtonMotion` with eased hover slide, scale and purple accent plus press compression, using `Time.unscaledDeltaTime`.
- Page transitions: `DuovexPageReveal` short fade without touching DSP gameplay time.
- Keyboard: Up/Down arrows and Enter on song selection; ESC remains Back / Pause.
- Existing Options, Results, Pause backgrounds and buttons have a matching dark angular shell.

## UI source and editing

- Layout: `Assets/Scripts/DuovexUIFactory.cs`, methods `BuildMain082()` and `BuildSelect082()`.
- Runtime song card updates: `Assets/Scripts/DuovexVisual.cs`, `RefreshSelectVisual()`.
- Button hover/click animations: `Assets/Scripts/DuovexButtonMotion.cs`.
- Transition animation: `Assets/Scripts/DuovexPageReveal.cs`.
- Editable sprites: `Assets/Resources/Visual082/*.png`. The artwork is procedural and copyright-free; `tools/generate_ui_assets.py` regenerates the default assets (requires Python + Pillow).
- New themes are imported as Sprites automatically via `DuovexSpriteImport`.
- Layout uses 1280x720 reference coordinates. Game lane geometry, judge Y=586, and note sizes **have not changed**.

## Import and packaging notes

All UI buttons are real Unity UI `Button` objects; none of the text shown here is a noninteractive screenshot. No `ACCOUNT` or fake update/download controls have been added. Song-card images are illustrative placeholders, not official album art. The Unity UI code and generated sprites are included in the zip.

**Validation:** zip integrity, C# static checks, JSON/audio hash comparisons, and asset ID checks are run outside Unity. Actual Unity compilation and visual Play Mode tests remain required; the result is an implementation candidate, not a certified build.

Some bundled user-supplied tracks require rights clearance for public exhibition or redistribution, including streaming/Steam release.
