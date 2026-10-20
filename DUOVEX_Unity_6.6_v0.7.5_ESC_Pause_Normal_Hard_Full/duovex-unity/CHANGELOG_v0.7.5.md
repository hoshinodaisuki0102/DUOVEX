# DUOVEX v0.7.5 — ESC Pause & Music Difficulties

- Gameplay ESC now toggles a non-destructive pause instead of exiting the
  song-selection screen. Audio DSP playback pauses and resumes at its cursor.
- Redesigned in-game pause overlay with RESUME, RETRY and TRACK SELECT.
  SPACE keeps its previous pause toggle.
- Added independent NORMAL and HARD Light It Up note charts based on
  spectral-onset accents from the bundled audio.
- Updated song selection and chart loader to display and load both difficulty
  settings from separate Resources JSON assets, using one shared MP3.
- Preserved the original v0.7.3 chart JSON and all other built-in practice maps.
- New editable scene destination: `Assets/Scenes/DuovexV075.unity`.
- Baseline Editor check now validates both new difficulty charts.

No change to timing judgement windows, editor save format, tutorial charts,
audio volume preferences, lane sizing or hit-line center.
