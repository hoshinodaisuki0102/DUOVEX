# DUOVEX v0.7.2 — Narrow Playfield and Baseline Check

## Changed

- Reduced gameplay playfield from 3 × 120 px to **3 × 96 px**, centered at X=496.
- Changed default TAP size from 112 × 18 px to **84 × 16 px** (6 px inset on each side).
- Kept the judgment center at Y=586 and reduced the hit line to 288 × 8 px.
- Resized center gear plate and gameplay judgment text region to fit the new width.
- Synced fallback renderer coordinates with the UI factory layout constants.
- Set fresh DuovexTheme defaults to 16px height / 6px inset.
- Generate **DuovexV072.unity** without overwriting existing customized scenes.

## Added

- Editor command: `Tools > Duovex > Apply v0.7.2 Note Metrics to Theme`.
- Editor command: `Tools > Duovex > Run v0.7.2 Baseline Check`.
- Baseline chart validation, layout inspection, and documentation.

## Preserved

- Existing chart JSON schema v1 and built-in 3 levels.
- Song import, chart editing, score and judgment rules, hit sounds, HUD functions.
- Existing v0.7.1 and older scenes and custom theme sprites.

## Remaining work / limitations

- The handoff design specification's **new custom artwork** is not included.
- Existing customized scenes do not resize automatically; generate the new scene.
- Current editor test command performs static checks, not timing or Play Mode QA.
- Unity 6.6 compilation and live gameplay must be checked on the user's machine.
