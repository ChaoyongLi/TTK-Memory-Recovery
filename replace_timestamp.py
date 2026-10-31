"""
Patch the timestamps stored after each "TEST!" marker in a binary image.

For every occurrence of the marker bytes 54 45 53 54 21 ("TEST!") the 6 bytes
that immediately follow are a timestamp (YY MM DD hh mm ss, one byte each).
The FIRST occurrence is left untouched; the 2nd, 3rd, ... occurrences are
overwritten, in order, with the timestamps read from the depth txt file
(converted to hex by read_depth.py).

Only those 6 bytes per occurrence are modified; the rest of the file is kept
byte-for-byte identical.

Usage:
    python replace_timestamp.py [img_path] [depth_txt_path]
"""

import sys
from pathlib import Path

from find_pattern import find_all
from read_depth import read_file

BASE_DIR = Path(__file__).parent
DEFAULT_IMG = BASE_DIR / "261028_155728.img"
DEFAULT_TXT = BASE_DIR / "SG#1ZH~Run2~20261031154119~Depth.txt"

MARKER = bytes.fromhex("5445535421")  # ASCII "TEST!"
TIMESTAMP_LEN = 6                     # YY MM DD hh mm ss
SKIP_FIRST = 1                        # leave the first marker's timestamp alone


def main(img_path: Path, txt_path: Path) -> None:
    data = bytearray(img_path.read_bytes())

    offsets = find_all(bytes(data), MARKER)[SKIP_FIRST:]
    # Each item is (date_str, time_str, hex_str); keep only the hex string.
    new_stamps = [bytes.fromhex(h) for _, _, h in read_file(txt_path)]

    # Safety check: refuse to patch if the counts do not line up.
    if len(offsets) != len(new_stamps):
        raise SystemExit(
            f"Mismatch: {len(offsets)} markers to patch vs "
            f"{len(new_stamps)} timestamps in the txt file. Nothing written."
        )

    for off, stamp in zip(offsets, new_stamps):
        pos = off + len(MARKER)  # first byte after the marker
        old = bytes(data[pos:pos + TIMESTAMP_LEN])
        data[pos:pos + TIMESTAMP_LEN] = stamp
        print(f"offset 0x{pos:X}: {old.hex(' ').upper()} -> {stamp.hex(' ').upper()}")

    img_path.write_bytes(data)
    print(f"Patched {len(offsets)} timestamps in {img_path}")


if __name__ == "__main__":
    img = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_IMG
    txt = Path(sys.argv[2]) if len(sys.argv) > 2 else DEFAULT_TXT
    main(img, txt)
