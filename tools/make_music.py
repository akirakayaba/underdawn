# Makes the placeholder background music (assets/audio/music/grassland_theme.ogg)
# A minor, 90 bpm, 16 bars that loop: pad chords, arpeggio, bass, soft drums,
# melody comes in on the second half. Needs numpy + ffmpeg on the PATH.
#   python tools/make_music.py
import subprocess, wave
from pathlib import Path
import numpy as np

RATE = 32000
BPM = 90
BEAT = 60 / BPM
BAR = BEAT * 4
OUT = Path(__file__).resolve().parent.parent / "assets" / "audio" / "music" / "grassland_theme.ogg"

NOTES = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}
def freq(name, octave):
    return 440.0 * 2 ** ((NOTES[name] + 12 * (octave + 1) - 69) / 12)

# chord per bar: (root, notes in the chord)
CHORDS = [("A", ["A", "C", "E"]), ("F", ["F", "A", "C"]), ("C", ["C", "E", "G"]), ("G", ["G", "B", "D"]),
          ("A", ["A", "C", "E"]), ("F", ["F", "A", "C"]), ("D", ["D", "F", "A"]), ("E", ["E", "G#", "B"])] * 2
# melody for bars 8-15: (note, octave, start beat in bar, length in beats)
MELODY = [[("E", 5, 0, 2), ("C", 5, 2, 2)], [("A", 4, 0, 3), ("C", 5, 3, 1)], [("G", 5, 0, 2), ("E", 5, 2, 2)], [("D", 5, 0, 4)],
          [("E", 5, 0, 2), ("A", 5, 2, 2)], [("C", 5, 0, 3), ("A", 4, 3, 1)], [("F", 5, 0, 2), ("D", 5, 2, 2)], [("B", 4, 0, 2), ("G#", 4, 2, 2)]]

LENGTH = int(BAR * len(CHORDS) * RATE)
TAIL = int(2 * RATE)
song = np.zeros(LENGTH + TAIL)

def add(start_sec, wave_):
    i = int(start_sec * RATE)
    song[i:i + len(wave_)] += wave_[:len(song) - i]

def envelope(n, attack, release):
    env = np.ones(n)
    a, r = min(int(attack * RATE), n), min(int(release * RATE), n)
    env[:a] = np.linspace(0, 1, a)
    if r:
        env[-r:] *= np.linspace(1, 0, r)
    return env

def tone(f, seconds, kind="sine"):
    t = np.arange(int(seconds * RATE)) / RATE
    if kind == "sine":
        return np.sin(2 * np.pi * f * t)
    if kind == "triangle":
        return 2 * np.abs(2 * ((f * t) % 1) - 1) - 1
    # soft pulse: a few odd harmonics
    return sum(np.sin(2 * np.pi * f * k * t) / k for k in (1, 3, 5))

rng = np.random.default_rng(7)
for bar, (root, chord) in enumerate(CHORDS):
    start = bar * BAR
    # pad: chord held the whole bar, two slightly detuned sines per note
    for i, note in enumerate(chord):
        f = freq(note, 3 if i == 0 else 4)
        pad = (tone(f, BAR + 0.6) + tone(f * 1.004, BAR + 0.6)) * envelope(int((BAR + 0.6) * RATE), 0.4, 0.8)
        add(start, pad * 0.05)
    # arpeggio: eighth notes up and down the chord
    arp = [chord[0], chord[1], chord[2], chord[1]] * 2
    for step, note in enumerate(arp):
        octave = 5 if step % 4 == 2 else 4
        n = tone(freq(note, octave), 0.5, "triangle") * np.exp(-np.arange(int(0.5 * RATE)) / RATE * 9)
        add(start + step * BEAT / 2, n * envelope(len(n), 0.005, 0.05) * 0.06)
    # bass on beats 1 and 3
    for beat in (0, 2):
        b = tone(freq(root, 2), BEAT * 1.8) * envelope(int(BEAT * 1.8 * RATE), 0.01, 0.3)
        add(start + beat * BEAT, np.tanh(b * 1.5) * 0.16)
    # drums: soft kick on 1, hats on the off beats (quieter in the first half)
    kick_t = np.arange(int(0.25 * RATE)) / RATE
    kick = np.sin(2 * np.pi * (50 + 60 * np.exp(-kick_t * 30)) * kick_t) * np.exp(-kick_t * 18)
    add(start, kick * 0.25)
    hat_level = 0.02 if bar < 8 else 0.035
    for step in range(8):
        if step % 2:
            noise = rng.standard_normal(int(0.05 * RATE))
            noise = np.diff(noise, prepend=0) * np.exp(-np.arange(len(noise)) / RATE * 80)
            add(start + step * BEAT / 2, noise * hat_level)
    # melody on the second half
    if bar >= 8:
        for note, octave, beat, length in MELODY[bar - 8]:
            n = tone(freq(note, octave), length * BEAT, "pulse")
            vibrato = 1 + 0.003 * np.sin(2 * np.pi * 5 * np.arange(len(n)) / RATE)
            add(start + beat * BEAT, n * vibrato * envelope(len(n), 0.03, 0.25) * 0.045)

# wrap the tail (reverb-ish release) back to the start so the loop is seamless
song[:TAIL] += song[LENGTH:]
song = song[:LENGTH]
# simple low pass to soften it
kernel = np.ones(6) / 6
song = np.convolve(song, kernel, mode="same")
song = song / np.max(np.abs(song)) * 0.8

OUT.parent.mkdir(parents=True, exist_ok=True)
wav_path = OUT.with_suffix(".wav")
with wave.open(str(wav_path), "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes((song * 32767).astype(np.int16).tobytes())
subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(wav_path), "-c:a", "libvorbis", "-q:a", "4", str(OUT)], check=True)
wav_path.unlink()
print(f"wrote {OUT} ({LENGTH / RATE:.1f}s)")
