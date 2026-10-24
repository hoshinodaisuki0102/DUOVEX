# DUOVEX v0.8.3 — Monochrome UI Foundation

This update uses your **second and third reference sketches** as the layout contract. It is **not** a replacement of the game, maps, input rules or chart editor. The main menu and track-select are rebuilt as independent, editable Unity uGUI objects.

## Install

### Full Unity project (recommended for review)
1. Extract the ZIP, open the `duovex-unity` folder in Unity 6.6.
2. Let the Editor finish compiling/importing assets.
3. Run **Tools > Duovex > Generate Editable UI Scene**.
4. Open `Assets/Scenes/DuovexV083_Monochrome.unity` and enter Play Mode.

### Patch over v0.8.2 UI Repair
1. Close Unity, make a complete backup of your project and keep any edited `.unity` scene.
2. Copy this patch's `Assets/` contents into your project's `Assets/` (merge, overwrite changed C# files).
3. Run **Tools > Duovex > Generate Editable UI Scene** and open `DuovexV083_Monochrome.unity`.
4. Existing v0.8.2 UI Repair and Main scenes are NOT overwritten by the generator.

The patch changes only `DuovexUIFactory.cs`, `DuovexVisual.cs`, `Editor/DuovexSceneGenerator.cs`, `Editor/DuovexSpriteImport.cs` and adds `Assets/Resources/Visual083/*.png`.

## What changed
- **Main**: circle DV logo and asymmetric placement from sketch 2; right START/CHART STUDIO, left SETTINGS, deliberate monochrome-X background constrained behind interactive content.
- **Track select**: bright selected-track information card on left and dark row list on right from sketch 3, consistent fixed hit areas/row spacing, updated original chart cover artwork, selected row distinguished by both brightness and violet outline.
- **Editable hierarchy**: `Page_Main`, `Page_Select` contain ordinary `RectTransform`, `Image`, `Text`, `Button` components; texture assets are separate PNG files under `Visual083`.
- **Animation**: existing button hover and page transitions preserved; decorative symbols have independent elements so the background does not collide with text.
- **Unchanged**: `DualSyncGame` judgment and hold logic, input mappings, speed controls, editor, imported songs and bundled charts/audio.

## Editor notes
- Logical UI reference resolution: **1280×720 (16:9)**, canvas scaler extends to other window sizes.
- New backgrounds: **2560×1440**; icons/button images: **2× their normal screen size**; art in `Assets/Resources/Visual083/`.
- Keep important text in Unity Text, rather than flattening it into the PNG. It remains dynamic and editable.
- Original chart cover sprites still exist in Visual082 and are not deleted.
- You can duplicate the v0.8.3 scene before editing it. The generator refuses to replace a scene already present.
- The mockups in `docs/` are composited images from asset files and designed coordinates, **not Unity runtime screenshots**.

## Acceptance checklist (run in Unity)
1. Open scene: check Console for **0 red compile errors**.
2. Main: START, CHART STUDIO, SETTINGS function as expected.
3. Select: rows, PREV/NEXT, PLAY, ADD SONG, BACK all respond.
4. Select changed song: cover, title, BPM and difficulty label update.
5. Play LV1/LV2/LV3 and a music chart; test DUAL and HOLD. Score and audio logic should match v0.8.2.
6. Resize Game view 16:9, 16:10 and narrower: no critical text clipped.
7. After any manual scene edits, do not re-generate into the same file; scene creator refuses to overwrite.

**Verification scope:** asset PNGs, zip integrity, JSON parse and static checks have been tested here. Unity 6.6 is not available in this environment, so compilation and play mode results remain unverified.
