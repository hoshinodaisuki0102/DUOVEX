# DUOVEX — Light It Up v0.7.3 Chart Guide

Original provided recording: **Robin Hustin & Tobimorrow — Light It Up [NCS Release]**.

## Measured tempo and timeline

- Audio duration: approximately `185.443 s` (the supplied MP3)
- Tempo: **128 BPM** / 4/4 working meter / 0.46875 seconds per beat
- Chart offset: **7412 ms**; beat 0 is at ~00:07.412 in the MP3
- JSON schema: v1; `beat` is offset from the chart's first musical beat, not 00:00 of the file.
- Final note tails end at beat 374 (~02:59 + 2.8 s), ahead of the final MP3 length.

| Beats | Approx. audio time | Arrangement | Chart direction |
|---|---|---|---|
| 0–32 | 00:07.4–00:22.4 | Intro | Two/three taps per measure; learn all four keys |
| 32–64 | 00:22.4–00:37.4 | Verse | Quarter notes and shared-lane hand changes |
| 64–96 | 00:37.4–00:52.4 | First build | Eighths gradually introduced |
| 96–160 | 00:52.4–01:22.4 | First drop | Alternating D/J/F/K phrases, DUAL downbeats |
| 160–176 | 01:22.4–01:29.9 | Break | Sustains and low-density hits |
| 176–240 | 01:29.9–01:59.9 | Verse reprise | Four-key melody rhythm, less dense |
| 240–272 | 01:59.9–02:14.9 | Second build | Tension and denser eighths |
| 272–368 | 02:14.9–02:59.9 | Final drop | Peak density, 1/8-note rhythms and DUAL |
| 368–374 | 02:59.9–03:02.7 | Outro | Final center DUAL and two outer HOLDs |

**Note:** This is a deterministic, playable **first-pass arrangement based on
audio tempo and section/onset analysis**, not a hand-verified transcription of
every instrument. A musician/playtester should revise patterns, accents,
difficulty, offset, and audio sync before showcasing.

## How to test

1. Open the project in Unity 6000.6.6f1 and generate/open the editable v0.7.2 scene.
2. Press Play -> PLAY -> MUSIC / LIGHT IT UP -> PLAY.
3. At the strong hits around **00:52.4** and **02:14.9**, check that DUAL notes meet the horizontal hit line in sync with the audio.
4. Test F and J separately, F+J together, the middle break's long notes,
   pause (Space), retry (Enter), and returning to track selection (Esc).
5. Run `Tools > Duovex > Run v0.7.2 Baseline Check` (now checks the fourth JSON too).

Files:

- `Assets/Resources/Tracks/LightItUp.mp3` — supplied audio, local testing only
- `Assets/Resources/Charts/LightItUp.json` — authored chart
- `tools/generate_light_it_up_chart.py` — repeatable chart authoring source

## Important rights boundary

The supplied track is labelled NCS, but the NCS official usage policy directs
**game developers** to enquire using its commercial licensing form. Don't
assume NCS's YouTube/Twitch allowance covers in-game music distribution,
including a festival build or Steam release. Secure appropriate permission
before a public build with the MP3. See https://ncs.io/usage-policy.
