#!/usr/bin/env python3
"""Convert a logo image to a Thunderstore 256x256 icon (letterboxed, transparent pad).

Usage: make-icon-from-logo.py <source> [dest]
Requires Pillow (python3 -m pip install Pillow).
"""
import sys
from pathlib import Path

from PIL import Image

def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__, file=sys.stderr)
        return 1
    src_path = Path(sys.argv[1])
    dst_path = Path(sys.argv[2]) if len(sys.argv) > 2 else Path("packaging/icon.png")

    src = Image.open(src_path).convert("RGBA")
    if src.width == 256 and src.height == 256:
        icon = src
    else:
        scale = min(256 / src.width, 256 / src.height)
        new_size = (round(src.width * scale), round(src.height * scale))
        fit = src.resize(new_size, Image.LANCZOS)
        icon = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        icon.paste(fit, ((256 - new_size[0]) // 2, (256 - new_size[1]) // 2), fit)

    dst_path.parent.mkdir(parents=True, exist_ok=True)
    icon.save(dst_path, optimize=True)
    print(f"wrote {dst_path} ({icon.width}x{icon.height}) from {src_path} ({src.width}x{src.height})")
    return 0

if __name__ == "__main__":
    sys.exit(main())
