# DUOVEX v0.51 — GitHub Pages Playtest

This is a **browser recreation** of the gameplay from the Unity 6.6 DUOVEX v0.51 prototype, *not a Unity WebGL export*. It uses the exact original `Tutorial.json`, `Level2.json`, and `Level3.json` files copied from the v0.51 Unity project, plus its original white/black hit-sound WAV samples, packaged as text-safe base64 assets for GitHub Pages.

## Play

- **D / F**: white notes (left lane / shared center lane)
- **J / K**: black notes (shared center lane / right lane)
- A diagonally divided shared note = **F + J** at the same time (scored independently)
- Hold notes: hold the key through the tail; there is no release timing judgement
- **Space**: pause, **Escape**: song select, **F3 / F4**: scroll speed (0.5×–3.0×)
- Perfect ±50ms, Great ±80ms, Good ±120ms, Bad ±160ms, then Miss
- Level 1 Tutorial (42 inputs), Level 2 Interlock (134 inputs), Level 3 Overdrive (258 inputs). No music; optional metronome in Options.
- Desktop browsers and keyboards recommended; mobile on-screen keys are supported experimentally.

## Put it online with GitHub Pages

1. Create a **public GitHub repository** (e.g. `duovex-play`) or use a dedicated branch/folder in a repository you own.
2. Upload the **contents** of this ZIP (the `index.html` must be in the published root; keep `game.js`, `style.css`, `charts/`, and `sfx/` alongside it).
3. Open the repository's **Settings → Pages**.
4. Under **Build and deployment**, choose **Deploy from a branch**, set **Branch** to `main` and **Folder** to `/ (root)`, and save.
5. GitHub Pages will publish at `https://YOUR_USERNAME.github.io/REPOSITORY_NAME/` after the deployment succeeds.

If your existing repository already contains the Unity project, do **not** replace or delete it. Instead, put these files in a new repository for a clean setup, or use `/docs` in the existing repository and choose that folder in Pages settings.

## Local test

Run a simple local web server in this directory (opening `index.html` via `file://` may block JSON/audio loading):

```bash
python -m http.server 8000
```

Open `http://localhost:8000`.

## Limitations

- This is a lightweight HTML5 Canvas / JavaScript implementation of the original v0.51 silent test levels, not a compiled Unity WebGL build. Menu/UI rendering differs from the Unity editor prototype, but chart data and core judgement thresholds are preserved.
- Unity project features such as song import and chart studio are **not** included in this browser playtest.
- Timings use the browser's `performance.now()` clock; keyboard hardware and browser latency may differ from the Unity build.
- Google Fonts are cosmetic only; all gameplay assets are local and there is no backend.