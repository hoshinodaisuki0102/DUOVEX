# DUOVEX v0.8.2 UI Repair — installation and verification

This is a targeted **UI repair**, not a gameplay update. The original v0.8.2 chart files, songs, scoring, and input logic are unchanged.

## Existing project
1. Back up the project and close the Unity editor.
2. Merge the patch `Assets` into the project, replacing matching files.
3. Open Unity 6.6 and wait for asset reimport.
4. Run **Tools > Duovex > Generate Editable UI Scene**.
5. Open **Assets/Scenes/DuovexV082_UIRepair.unity** and enter Play Mode.

The generator creates a **new** scene to protect any hand-edited v0.8.2 scene. The old scene remains unchanged. Re-running generator will not overwrite an existing repair scene.

## QA checklist
- Main screen: no large white triangles, no text crossing background strokes.
- Main menu: START / CHART STUDIO / SETTINGS stay inside 1280x720.
- Song select: all 7 rows and selected cover are visible, text is legible.
- Test 16:9 Game View at 1280x720 and 1920x1080 (or Game view scale).
- Check song selection, navigation, chart studio, and gameplay.

## Changes
- `Assets/Resources/Visual082`: re-authored dark backgrounds and UI frames.
- `Assets/Scripts/DuovexUIFactory.cs`: re-laid out main menu and song select; text wrapping bounds.
- `Assets/Scripts/Editor/DuovexSceneGenerator.cs`: new repair scene path.

The Unity editor/compiler is not available in the file-generation environment. This patch has passed basic source and archive checks, but Unity gameplay/visual testing must be done locally.
