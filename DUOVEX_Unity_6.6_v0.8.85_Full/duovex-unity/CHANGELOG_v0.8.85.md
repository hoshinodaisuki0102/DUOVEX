# DUOVEX v0.8.85 — EASY / NORMAL / HARD, BOSS LOCK, and BLACK note legibility

- Five archived BOSS charts are retained under their original paths, but locked from the public difficulty selector; they can be revisited in a future release.
- EASY is a new readable chart derived from each song's NORMAL chart, with rhythm-onset-weighted selection and fewer simultaneous presses. SHORT and FULL variants were created where full audio exists.
- Old NORMAL/HARD chart IDs and note patterns are retained. Robust onset timing analysis indicated that LIGHT IT UP, EUPHORIANIC, and DESAPARECER benefit from small global corrections of -8ms, -38ms, and -30ms, respectively, applied consistently to FULL/SHORT.
- D / F / J / K, independent DUAL judgements, long-note ticks, and zero release timing remain unchanged.
- Lane surface is brighter desaturated blue/slate; BLACK tap and hold sprites now have defined lilac rim on dark charcoal, improving visibility without changing BLACK identity.
- Difficulty labels are 1-20, with EASY/NORMAL/HARD for all five music tracks; BOSS LOCKED notice is shown in the selected-song panel.
- Previous notes, audio and scores are preserved. All old chart IDs 1–18 remain fixed, EASY maps append as IDs 19–23.

## Apply
Back up the project. With Unity closed, merge Assets from the patch. Launch Unity 6.6, run `Tools > Duovex > Generate Editable UI Scene`, and open `DuovexV0885_EasyNormalHard.unity` to see the new lane sprite and labels. Previously manually edited scenes are not overwritten.

**Limits:** Static code/data/asset tests and spectral-flux alignment analysis only; direct Unity 6.6 runtime testing and human note-by-note musical transcription not performed.
