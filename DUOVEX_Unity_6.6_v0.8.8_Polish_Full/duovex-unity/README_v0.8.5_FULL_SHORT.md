# DUOVEX v0.8.5 — FULL / SHORT duration selection

Target: Unity 6.6, based on v0.8.4.

## Play
1. Open the project in Unity 6.6.
2. Choose **Tools > Duovex > Generate Editable UI Scene**.
3. Open `Assets/Scenes/DuovexV085_FullShort.unity` and press Play.
4. Under **PLAY > Select Track**, pick a song; use NORMAL / HARD / BOSS buttons for difficulty.
5. Use SHORT / FULL buttons or **Tab** to switch music length. The duration selector is independent from difficulty.
6. `Up / Down`: change track. `Left / Right`: change difficulty. `Enter`: play.

## Built-in music duration

| Song | FULL | SHORT | SHORT source interval (original audio) |
|---|---:|---:|---:|
| LIGHT IT UP | 3:05 | 1:02 | 02:01.787 – 03:01.787 |
| KETSUDAN | 2:30 | 1:02 | 01:27.672 – 02:27.474 |
| DESAPARECER | 2:26 | 1:01 | 01:21.506 – 02:20.817 |
| OVERWRITE | 2:47 | 0:59 | 00:10.770 – 01:08.103 |
| EUPHORIANIC | **source missing** | 1:09 | Existing 69.28-second source is already SHORT CUT |

The short assets for the four longer songs have a two-second silence lead-in, a short audio fade-in, and a 0.8-second ending fade-out. The clipped charts are rebased to match; long notes straddling a cut boundary were omitted instead of leaving broken hold tails. FULL audio and chart resources are left untouched.

SHORT sections were chosen by bar-aligned, high-energy / note-dense interval scoring. They were *not* manually transcribed or auditioned against the lead melody. Review the joins/phrase boundaries in Unity; the source may require musical fine-tuning.

EUPHORIANIC has no full-length source in the supplied files. To add FULL properly, a legitimate full-length recording and a full-length chart aligned to that recording are required. The FULL control is intentionally disabled for this song rather than playing a mislabeled short recording.

Practice levels 1/2/3 are silent and remain unchanged. Imported songs and local charts remain unchanged (a single user-authored audio version each). Existing game logic, all FULL charts, player settings and score data are preserved. Unity compile and play-mode tests are **not** available here; this project has passed asset, chart-timing and ZIP integrity validation only.

## Implementation
- `Assets/Resources/Tracks/Short/`: OGG short clips, ~1 minute each
- `Assets/Resources/Charts/Short/`: matching clipped NORMAL/HARD/BOSS JSON charts
- `Assets/Scripts/DuovexBuiltInTracks.cs`: resolves audio and chart paths by selected length
- `Assets/Scripts/DuovexLibraryUI.cs`: version selection and track startup
- `Assets/Scripts/DuovexUIFactory.cs`: editable version buttons
- `Assets/Scripts/DuovexVisual.cs`: version highlighting and missing-source disabled state
- `Assets/Scripts/Editor/DuovexSceneGenerator.cs`: new v0.8.5 scene path; old scenes untouched

### Important
Do not regenerate the same scene after making manual UI edits. Always save a copy of the scene or create a separate layout variant. Audio copyright and redistribution permissions must be checked before sharing a public build.
