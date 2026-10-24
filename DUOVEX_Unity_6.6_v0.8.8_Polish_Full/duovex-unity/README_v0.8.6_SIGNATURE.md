# DUOVEX v0.8.6 — Signature / Main Menu Update

## What changed

- Replaced the prior generic text logo with a transparent, editable signature sprite based on the user-supplied hand sketch.
- Replaced the `DV` text with a companion D/V monogram derived from the signature crossings and the sweeping stroke.
- New subdued, *opaque* backdrop removes oversized bright triangles and visible Unity sky.
- Consolidated START / CHART STUDIO / SETTINGS into a consistent right-side button stack, with hover/selected styling and existing button IDs preserved.
- Each logo, monogram, button, caption and background is a separate editable uGUI object.
- All existing song metadata, full/short tracks, charts and gameplay/score/notes are unchanged.

## Unity 6.6 setup

1. Back up your project. Exit Play Mode before applying an update.
2. Either open the Full package as a new project OR merge the patch's Assets folder into v0.8.5.
3. In Unity run `Tools > Duovex > Generate Editable UI Scene`.
4. Open `Assets/Scenes/DuovexV086_Signature.unity` and Play.
5. Expand `DUOVEX_UI / DesignRoot / Page_Main` to edit `Main_Title` (signature), `Main_DV` (monogram), `B_Main_*` (buttons), `Main_Background` (decorative art).

The generator does **not** overwrite an existing DuovexV086_Signature.unity. Previous scenes are unaffected; save a duplicate for your customized layout.

## Assets

`Assets/Resources/Visual086/` has separated PNG artwork. Editable SVG sources are in `docs/` (no font files included). Main UI coordinates are in a 1280x720 reference frame.

## Test checklist (must test in your Unity editor)

- Console compiles without errors.
- Main menu at Game 16:9 and Free Aspect has no clipped text or white wedges.
- START navigates to song select; CHART STUDIO works; SETTINGS opens and returns.
- Hover, pointer click and keyboard selection are visible.
- Songs FULL/SHORT and per-song difficulty remain available.

Note: Unity Editor and full runtime testing were not available in the authoring environment; visual PNG preview is an asset layout composite, not an actual Unity screenshot.
