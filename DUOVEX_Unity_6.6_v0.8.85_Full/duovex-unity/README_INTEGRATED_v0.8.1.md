# DUOVEX v0.8.1 — KETSUDAN + 3 new songs

Unity project created by merging the **v0.7.5 complete version** with the v0.8 KETSUDAN boss level, while preserving ESC pause, independent music volume, refined UI and both Light It Up charts.

## Installation

1. Use Unity 6000.6.6f1 (or compatible Unity 6 installation) to open the `duovex-unity/` directory.
2. Open `Tools > Duovex > Generate Editable UI Scene`. It creates `Assets/Scenes/DuovexV081.unity` and does not overwrite older scenes.
3. Open the generated scene and enter Play Mode. From PLAY, select a track; UP/DOWN browses additional tracks beyond the first seven.
4. Run `Tools > Duovex > Run v0.8.1 Baseline Check` and inspect the Unity Console.
5. ESC toggles pause/resume during gameplay. Settings offers music and hit-SFX volume separately.

## Built-in track selection

1–3: Silent Tutorial / Interlock / Overdrive
4–5: Light It Up — NORMAL (454) / HARD (713)
6: 決断の瞬間 / KETSUDAN — BOSS (1277; variable timing points)
7–8: Euphorianic — NORMAL / HARD
9–10: Desaparecer — NORMAL / HARD
11–12: Overwrite — NORMAL / HARD
13: Local Chart / imported user audio library follows

## New chart generation and offsets

User-provided MP3s were analyzed for drum and high-frequency onset energy. Pattern density rises in stronger passages; HARD adds 16th patterns and two-key hits. The placements are *first playable drafts*, not hand-certified transcription.

| Track | BPM | Chart audio offset | NORMAL | HARD |
|---|---:|---:|---:|---:|
| Euphorianic | 140 | 91.0ms | 173 | 275 |
| Desaparecer | 174 | 127.0ms | 456 | 746 |
| Overwrite | 180 | 103.0ms | 542 | 886 |

**Note**: Offset is tied to the MP3 file timestamp. Device latency and Unity MP3 seek scheduling may require minor calibration.
**KETSUDAN**: uses 63 beat→seconds timing points from the boss chart; no conversion to a fixed BPM chart.

## Important testing and rights
- No Unity Editor/runtime was available in the creation environment. JSON, resource-path, audio-duration, hold collisions, note identifiers and ZIP integrity were checked statically, but compilation, player sync and UX must be verified in Unity.
- The supplied audio files for Pump It Up / Underverse are copyrighted. Bundled exclusively as user-provided audio for private prototyping; **do not publish a demo, festival build, Steam build, or redistribute publicly without the relevant rightsholders’ permission.**
- The `Assets/Resources/Charts/*.json` new chart data are editable text files, but the built-in Song Select treats them read-only to avoid accidental replacement.
