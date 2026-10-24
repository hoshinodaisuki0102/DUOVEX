"""Regenerate DUOVEX's Light It Up chart; standard library only.

The MP3's tempo (128 BPM), first grid beat (7.412 s), and song phrase
boundaries were measured from onset analysis of the user-supplied recording.
"""
import json
from collections import Counter
from pathlib import Path

BPM = 128.0
OFFSET_MS = 7412.0
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/Resources/Charts/LightItUp.json"
PATTERNS = {
    "intro": [[0, 2], [0, 1, 2], [0, 2], [0, 2, 3]],
    "verse": [[0, 1, 2, 3], [0, 2, 2.5, 3], [0, .5, 2, 3], [0, 1, 2, 3]],
    "build": [[0, 1, 2, 3], [0, .5, 1, 2, 2.5, 3],
              [0, .5, 1, 1.5, 2, 3], [0, .5, 1, 1.5, 2, 2.5, 3, 3.5]],
    "drop": [[0, .5, 1, 2, 2.5, 3], [0, 1, 1.5, 2, 3, 3.5],
             [0, .5, 1, 1.5, 2, 2.5, 3, 3.5], [0, 1, 2, 2.5, 3, 3.5]],
    "verse2": [[0, 1, 2], [0, 1.5, 2, 3], [0, 1, 2, 3], [0, 2, 2.5, 3]],
    "build2": [[0, 1, 2, 3], [0, .5, 1, 2, 2.5, 3],
               [0, .5, 1, 1.5, 2, 2.5, 3], [0, .5, 1, 1.5, 2, 2.5, 3, 3.5]],
    "drop2": [[0, .5, 1, 1.5, 2, 2.5, 3], [0, 1, 1.5, 2, 2.5, 3, 3.5],
              [0, .5, 1, 1.5, 2, 2.5, 3, 3.5], [0, .5, 1, 2, 2.5, 3, 3.5]],
}

notes = []
seen = set()
holds = []

def add(beat, key, duration=0):
    assert key in range(4) and beat >= 0 and duration >= 0
    assert round(beat * 4) == beat * 4, "Use quarter-beat grid"
    pos = (int(beat * 4), key)
    if pos in seen:
        raise ValueError(f"Duplicate key at beat {beat}: {key}")
    for old_beat, old_key, old_duration in holds:
        if old_key == key and old_beat < beat < old_beat + old_duration:
            raise ValueError(f"Tap on held key {key} at beat {beat}")
    seen.add(pos)
    notes.append({"id": len(notes) + 1, "beat": float(beat),
                  "input": key, "durationBeats": float(duration)})
    if duration:
        holds.append((beat, key, duration))

def dual(beat):
    add(beat, 1)
    add(beat, 2)

def pick_key(bar, position, index, style, previous):
    if style == "intro":
        key = [0, 1, 2, 3][(index + bar // 2) % 4]
    elif style in ("verse", "verse2"):
        key = ([0, 2, 1, 3] if bar % 2 == 0 else [3, 1, 2, 0])[index % 4]
    elif position in (0, 2):
        key = 0 if (bar + int(position)) % 2 == 0 else 3
    elif position in (1, 3):
        key = 1 if (bar + int(position)) % 2 == 0 else 2
    else:
        key = [1, 3, 2, 0][(bar + int(position * 2)) % 4]
    if previous is not None and key == previous:
        key = [1, 0, 3, 2][key]
    return key

def bars(start, end, style):
    for bar in range(start, end):
        pattern = PATTERNS[style][(bar - start) % 4]
        previous_key = None
        for i, position in enumerate(pattern):
            beat = bar * 4 + position
            is_dual = (style == "drop" and position == 0 and (bar-start) % 4 == 0) or (
                style == "drop2" and position == 0 and (bar-start) % 2 == 0) or (
                style == "drop2" and position == 2 and (bar-start) % 8 == 6) or (
                style == "build2" and position == 3.5 and bar == end-1)
            if is_dual:
                dual(beat)
                previous_key = None
            else:
                key = pick_key(bar, position, i, style, previous_key)
                add(beat, key)
                previous_key = key

# Beat-domain arrangement: build twice, with a four-bar HOLD break in between.
bars(0, 8, "intro")         # 00:07.4-00:22.4
bars(8, 16, "verse")        # 00:22.4-00:37.4
bars(16, 24, "build")       # 00:37.4-00:52.4
bars(24, 40, "drop")        # 00:52.4-01:22.4
add(160, 0, 4)
add(162, 2)
add(164, 3, 4)
add(166, 1)
add(168, 1, 4)
add(170, 3)
add(172, 2, 4)
add(174, 0)
bars(44, 60, "verse2")      # 01:29.9-01:59.9
bars(60, 68, "build2")      # 01:59.9-02:14.9
bars(68, 92, "drop2")       # 02:14.9-02:59.9
dual(368)
add(370, 0, 4)
add(370, 3, 4)

notes.sort(key=lambda n: (n["beat"], n["input"]))
for idx, note in enumerate(notes, 1):
    note["id"] = idx
chart = {
    "schemaVersion": 1,
    "title": "LIGHT IT UP",
    "bpm": BPM,
    "offsetMs": OFFSET_MS,
    "tickDivision": 4,
    "notes": notes,
}
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
OUTPUT.write_text(json.dumps(chart, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
counts = Counter(n["input"] for n in notes)
duals = sum(n["input"] == 1 and (int(n["beat"] * 4), 2) in seen for n in notes)
print(f"{len(notes)} logical notes; {duals} DUALs; "
      f"{sum(n['durationBeats'] > 0 for n in notes)} HOLDs; inputs={dict(counts)}")
