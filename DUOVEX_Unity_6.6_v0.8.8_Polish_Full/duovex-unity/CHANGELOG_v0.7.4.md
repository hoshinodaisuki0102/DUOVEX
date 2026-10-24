# DUOVEX v0.7.4 — Buttons, Music Volume & Judgment Center

- Rebuilt main and selection action buttons with high-contrast cyan/navy styles,
  numbering, arrows, border accents, and stronger typography.
- Restyled song rows and removed orphan accent bars in empty song slots.
- Added persistent MUSIC VOLUME slider independent of HIT SOUND VOLUME.
- Replaced the gameplay judgment-line graphic with a crisp cyan 288×8 line,
  white 288×2 center core, dark underlay, glow, and edge caps.
- Preserved **Y=586** as both judgment-line and scheduled TAP center.
- Moved the D/F/J/K key backgrounds down to **Y=618** (32px center-to-top gap).
- Added geometry/slider checks to Tools > Duovex > Run v0.7.4 Baseline Check.
- Created a new editable scene `DuovexV074.unity` to preserve older scenes.
- Preserved v0.7.3's Light It Up audio/520-note chart and prior practice maps.

Not changed: beat clocks, PERFECT ±50ms window, gameplay input bindings,
DUAL/HOLD judgment algorithms, chart schema. Unity live playback remains
untested in this build environment.
