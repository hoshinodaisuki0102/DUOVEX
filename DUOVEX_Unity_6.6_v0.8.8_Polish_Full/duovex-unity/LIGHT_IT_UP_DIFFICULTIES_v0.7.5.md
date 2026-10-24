# Light It Up — DUOVEX NORMAL / HARD

Generated from the user-supplied Robin Hustin & Tobimorrow recording. Unity
loads both charts as independent, read-only built-in difficulties, sharing the
same `Tracks/LightItUp.mp3`.

| Property | NORMAL | HARD |
|---|---:|---:|
| BPM | 128 | 128 |
| Offset (first beat) | 7412 ms | 7412 ms |
| Logical notes | 454 | 713 |
| Simultaneous pairs | 13 | 36 |
| F+J diagonal DUAL pairs | 11 | 14 |
| Holds | 6 | 6 |
| Sixteenth-grid notes | 0 | 40 |

## Chart design

- **0:07–0:22 Intro:** sparse downbeats on NORMAL; gentle eighths in HARD.
- **0:22–0:37 Verse:** alternating D/F/J/K motifs, preserving left/right balance.
- **0:37–0:52 Build:** more offbeats as the instrumental grows, occasional
  fast fills in HARD.
- **0:52–1:22 First drop:** regular kick/snare grid, stronger eighth accents,
  mirrored outer-key chords and center F+J DUALs for HARD.
- **1:22–1:30 Breakdown:** reduced note density with sustained notes.
- **1:30–2:00 Verse 2:** reset to simpler rhythmic phrases.
- **2:00–2:15 Build 2:** rising note density and offbeat patterns.
- **2:15–3:00 Final drop:** highest energy, dual-hand call/response, repeated
  keys, 117 ms sixteenth-note fills in HARD.
- **3:00–3:05 Outro:** distinct concluding pattern.

The source audio was decoded with FFmpeg. A short-time Fourier transform was
used to extract attacks in low (kick), mid (body/snare) and high (hat/lead)
frequency bands. Musical sections and primary quarter beats were hand-defined;
offbeat and sixteenth-note placements were selected from the strongest attacks
in each bar. Attack positions were checked against the 128 BPM grid; energy
peaks differed by only a few tens of milliseconds from the existing 7.412 s
reference. These are musically informed draft charts, **not** a substitute for
play-testing by a human charter.

### Files and editing

- `Assets/Resources/Charts/LightItUpNormal.json`
- `Assets/Resources/Charts/LightItUpHard.json`
- `tools/generate_light_it_up_difficulties.py`

To regenerate: `python3 -m pip install numpy scipy`; install FFmpeg, then
run `python3 tools/generate_light_it_up_difficulties.py`. Unity requires no
Python packages or FFmpeg to **play** the included JSONs.

Original v0.7.3 `LightItUp.json` and its older generator remain untouched for
backwards comparison. The new Unity song select uses only NORMAL and HARD.

### Play-test checklist

1. Tap a quarter beat near 00:22 and each drop near 00:52.4 / 02:14.9 to
   verify simultaneous audio and note-center alignment. Adjust offset after
   consistent user-measured drift, not by changing the note renderer.
2. Press ESC mid-song. Confirm audio freezes, score/combo is unchanged, and
   ESC resumes the same phrase without jumping to the beginning.
3. Verify F+J on HARD is rendered as a diagonal split note and judged
   independently on the two keys.
4. Hold a long note through a paired strike in the 01:22 breakdown.
5. Verify HARD's 16th notes can be played on a real 144 Hz keyboard without
   accidental key-blocking; test entry-level keyboards too.

The included MP3 remains private test material. Contact the rightsholder /
NCS for video game exhibition, distribution and Steam permissions before a
public release. NCS video/streaming permissions do not automatically cover
games.
