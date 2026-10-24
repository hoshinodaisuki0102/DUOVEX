# DUOVEX v0.8.7 — Neon Logo Integration (Unity 6.6)

This is a **real Unity project update**, not a standalone design image. It replaces the previous signature logo UI and monogram with sprites derived from the user's approved neon logo.

## Files changed

- `Assets/Resources/Visual087/DuovexNeonLogo87.png`: transparent UI-ready artwork cut from the supplied user image.
- `Assets/Resources/Visual087/DuovexNeonMark87.png`: transparent companion DV mark.
- `Assets/Scripts/DuovexUIFactory.cs`: Main_Title uses neon sprite and correct aspect ratio/placement; Main_DV uses the new mark; existing START/CHART STUDIO/SETTINGS button IDs are preserved.
- `Assets/Scripts/Editor/DuovexSpriteImport.cs`: new sprite folder is automatically imported as Sprite (2D and UI).
- `Assets/Scripts/Editor/DuovexSceneGenerator.cs`: generates **DuovexV087_NeonLogo.unity** without overwriting previous scenes.
- `docs/User_Approved_Neon_Logo_Original.png`: exact user-supplied image for comparison, as a design reference.

## Update an existing v0.8.6 Unity project

1. Back up the Unity project and close Unity.
2. Unzip the **patch** and merge its `Assets` folder into your existing v0.8.6 project (overwrite scripts when asked).
3. Open the project with Unity 6.6.
4. Exit Play Mode, then choose **Tools > Duovex > Generate Editable UI Scene**.
5. Open `Assets/Scenes/DuovexV087_NeonLogo.unity`. Select **Game, 16:9** then Play.
6. In Hierarchy `DUOVEX_UI > DesignRoot > Page_Main`, select `Main_Title` or `Main_DV` to customize their images.

**Existing v0.8.6 scenes are unchanged.** The new logo appears automatically when running the old `Main.unity` bootstrap scene too because it builds the updated factory at runtime.

## What stays the same

All music, charts, difficulty, FULL/SHORT, judgement and game-play scripts are unchanged. Only the logo/monogram art and main menu layout are updated.

## QA checklist

- Both graphics display as sprites (not the default white rectangle).
- The logo fits without stretching or crossing the menu buttons.
- START, CHART STUDIO and SETTINGS still work.
- No new red C# console errors after asset reimport.
- 16:9 reference screen first; 16:10/free aspect need an additional visual check.

**Limitations:** Unity runtime/editor is not available in this workspace, so actual Unity 6.6 compiling and on-screen interaction are not independently verified. The mock preview in `docs/` is not a Unity screenshot.
