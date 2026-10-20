# DUOVEX v0.7.1 — Playfield Geometry Fix

An original Duovex UI gear, visually guided by the legibility of DJMAX RESPECT V; no third-party art/assets included.

- Reworked the gameplay gear around a **360 px / 3 x 120 px** centered playfield (1280 x 720 design coordinates).
- Set the default note height to **18 px**, with 4 px inset per lane side. Tap sizes remain identical at every chart snap subdivision.
- Repainted White/Black/DUAL TAP, HOLD, Lane and HitLine PNG sprite assets, including a stronger black-note outline.
- Note center now reaches the **center of the judgment line** at the scheduled time; this corrects the former top-edge alignment.
- Read each physical lane's saved Unity RectTransform rather than guessing positions from a hardcoded width.
- Mask notes and holds to their gear viewport, with a small margin past the judgment center to keep full notes visible at impact.
- Keep visuals separate from the chart, score, timing, input, and hit-sound logic.
- Scene generator now outputs `DuovexV071.unity`, **without overwriting** edited `DuovexV07.unity` scenes.
- A gameplay visual preview is supplied in `docs/preview_game_v071.png` (mockup; not a Unity runtime screenshot).

## Upgrade from v0.7 or v0.6.1

1. Exit Play Mode and **back up your project**.
2. Copy the patch's `Assets` folder into the Unity project and allow matching files to be replaced.
3. Open the project in Unity 6.6 and wait for recompilation. Inspect the Console before proceeding.
4. Use **Tools > Duovex > Generate Editable UI Scene** to create `Assets/Scenes/DuovexV071.unity`.
5. Open `DuovexV071.unity` and press Play. Check Levels 1–3, DUAL and HOLD notes, and editor saving.
6. Design values are editable in `DUOVEX_UI > DesignRoot > Page_Game` and Theme asset Inspector.

**Note:** If an existing `DuovexTheme.asset` already has custom `noteHeight` or `noteSideInset` values, these are preserved. For the new reference look, use 18 and 4 respectively. A regenerated scene is needed to apply the updated static gear positions (rather than using an old saved scene).

**Verification:** source/asset/JSON checks were performed; Unity editor compilation and live play were not available in this environment.
