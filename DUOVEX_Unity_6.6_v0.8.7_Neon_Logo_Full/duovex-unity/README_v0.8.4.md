# DUOVEX v0.8.4 — Song-first selection + EUPHORIANIC charts

## New features

- **Song-first library:** all bundled songs are listed once. The right-hand list chooses a song; the left information panel holds up to three difficulty buttons.
- **Controls:** Up/Down selects a song; Left/Right (or A/D) changes difficulty; Enter starts the selected difficulty; mouse can click each difficulty button. The in-game chart studio still edits local/imported tracks.
- **EUPHORIANIC:** NORMAL, HARD and BOSS charts are bundled. These replace the older EUPHORIANIC NORMAL/HARD charts and add `Assets/Resources/Charts/EuphorianicBoss.json`.
- **Preservation:** older built-in chart level numbers remain 1–12. Euphorianic BOSS was appended at index 13, without renumbering the existing gameplay resources. Main menu, player timing, and imported-user-song folder format are unchanged.

## Sync reference and limitations

The charts were generated from the **actual 69.28-second Euphorianic SHORT CUT MP3** already supplied with this project (same SHA-256 as the separately supplied recording).

- Audio-derived steady tempo: **140 BPM**.
- 16th-beat onset-strength alignment: **approximately +29ms** chart offset relative to audio start. The older bundled maps had +91ms.
- Selection of events uses measured harmonic attack changes and percussive accents rather than fixed random note placement. Repeating motifs and explicit F+J shared-lane accents are applied for playability.
- This is an **algorithm-assisted first-pass chart**, not a manually verified note-by-note transcription of the lead melody. Test it with speakers/headphones and use visual/audio calibration to adjust the offset, note omissions, and phrase accents. Different device latencies can require further global adjustment.
- Exact in-game grading still depends on the original judgement code (PERFECT ±50ms), independent F/J grading, and hold ticks with **no release judgement**.

### Testing

1. Back up the previous Unity project and exit Play Mode.
2. Apply the patch `Assets/` tree (or open the full project in Unity 6.6).
3. Use **Tools > Duovex > Generate Editable UI Scene**. This creates a NEW `DuovexV084_SongDifficulty.unity` scene without overwriting `DuovexV083_Monochrome.unity`.
4. Open the NEW scene. On PLAY, look for **EUPHORIANIC** once in the list. Switch **NORMAL / HARD / BOSS** on the left with click or Left/Right.
5. Play and check audio/head sync in the first 10 seconds and again around the 40-second mark. If a systematic timing error is present, use `offsetMs` in the three charts as the calibration value; do not change BPM until a true tempo drift has been demonstrated.
6. On any compilation error, use the full Unity Console message and file/line as feedback.

**Audio rights:** the supplied recording may be used as a local development reference. Do not publicly redistribute it with a web or Steam build unless you have permission/licensing. For public builds, substitute a licensed track with the same audio timing or secure rights.
