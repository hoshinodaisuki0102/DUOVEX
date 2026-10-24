# DUOVEX v0.8.8 - UI / Play Flow / Chart Completion (Unity 6.6)

The files in this package modify the Unity C# project itself, not a mock-up.

## Changes
- Fixed menu idle buttons by applying the correct sprite/text state *after* initializing the UI skin. Hover uses the dark-purple sprite with readable white text. Button accent strips, which appeared as detached vertical purple bars, are removed.
- Selection highlight now uses the angled button sprite itself, not a separate right-side strip.
- On gameplay pause: press ESC, SPACE, or click RESUME to start a 3-2-1 countdown. Song/audio clock and all note judgments remain frozen until countdown finishes. ESC/SPACE during the countdown cancels it back to PAUSED.
- Results: large CLEAR/FAIL, SCORE / 1,000,000, ACCURACY, grade (SS/S/A/B/C/F), MAX COMBO, MISS, judgment counts, retry/track/menu buttons.
- Provisional CLEAR rule: accuracy at least 70.00%, with at least one scored note. This is score-based, not a life-gauge. FAIL otherwise. No mid-song failure/life mechanic was added.
- All bundled *musical* songs now offer NORMAL/HARD/BOSS difficulties within Lv.1..Lv.20. LV1-3 remain training chart labels, not song difficulty modes.
- New charts: KETSUDAN NORMAL (Lv7), HARD (Lv15); LIGHT IT UP BOSS (Lv19); DESAPARECER BOSS (Lv20); OVERWRITE BOSS (Lv19). Every new difficulty supports SHORT and FULL.
- Existing chart levels (legacy IDs 1-13), audio, and local user charts remain unchanged. New chart IDs are 14-18.
- New boss charts extend existing HARD rhythms using extra quantized note positions selected by audio onset strength. KETSUDAN NORMAL/HARD were distilled from the supplied BOSS timing map, preserving section timing. Charts are authored *candidates*, not human-verified melody transcriptions. Test them by ear and adjust awkward patterns in Chart Studio.

## Installation
1. Back up and CLOSE the existing v0.8.7 Unity project.
2. Merge the patch's `Assets` folder, overwriting affected C# scripts and adding new chart JSON files.
3. Open Unity 6.6, wait for compilation, then choose `Tools > Duovex > Generate Editable UI Scene`.
4. Open `Assets/Scenes/DuovexV088_PlayPolish.unity` and Play. Existing v0.8.7 saved scene remains untouched.
5. Menu hover -> Song Select (all 5 musical songs have 3 difficulties) -> gameplay pause/restart -> results.

## Verification limitations
The package and chart JSONs were validated programmatically. Unity 6.6 is not available in this environment for an actual editor compile and play-through. Please check Console after importing, then verify UI at 16:9 and your game's free-aspect resolution. Copyright permission may be required for public distribution of the bundled music.
