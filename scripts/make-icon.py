#!/usr/bin/env python3
"""Generate a simple 256x256 beeswax icon PNG (no external deps)."""
import zlib, struct, math, os

W = H = 256

def px(x, y):
    # vertical amber gradient background
    t = y / H
    r = int(232 - 40 * t)
    g = int(166 - 60 * t)
    b = int(60 - 20 * t)
    # rounded-corner mask
    m = 28
    cx = min(max(x, m), W - m)
    cy = min(max(y, m), H - m)
    if (x - cx) ** 2 + (y - cy) ** 2 > m * m:
        return (0, 0, 0, 0)
    # darker border ring
    d = min(x, y, W - 1 - x, H - 1 - y)
    if d < 10:
        k = 0.55
        return (int(r * k), int(g * k), int(b * k), 255)
    # three wax blobs
    for (bx, by, br) in ((86, 96, 44), (150, 120, 52), (116, 176, 48)):
        dist = math.hypot(x - bx, y - by)
        if dist < br:
            k = 1.18 if dist < br * 0.55 else 0.92
            return (min(255, int(r * k)), min(255, int(g * k)), min(255, int(b * k)), 255)
    # drips from top
    for dx in (60, 128, 196):
        if abs(x - dx) < 12 and y < 54 + 26 * math.sin((x - dx) / 12):
            return (min(255, int(r * 1.08)), min(255, int(g * 0.95)), b, 255)
    return (r, g, b, 255)

raw = b"".join(b"\x00" + b"".join(bytes(px(x, y)) for x in range(W)) for y in range(H))

def chunk(tag, data):
    c = struct.pack(">I", len(data)) + tag + data
    return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

png = (b"\x89PNG\r\n\x1a\n"
       + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0))
       + chunk(b"IDAT", zlib.compress(raw, 9))
       + chunk(b"IEND", b""))

out = os.path.join(os.path.dirname(__file__), "..", "packaging", "icon.png")
with open(out, "wb") as f:
    f.write(png)
print("wrote", os.path.normpath(out), len(png), "bytes")
