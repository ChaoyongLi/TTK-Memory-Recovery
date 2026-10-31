"""
Search a binary file for a byte pattern and report the count and offsets.

Usage:
    python find_pattern.py [file_path] [hex_pattern]

Defaults:
    file_path   -> "261028_155728.img" in the same directory as this script
    hex_pattern -> "5445535421" (ASCII "TEST!")
"""

import sys
from pathlib import Path

DEFAULT_FILE = Path(__file__).parent / "261028_155728.img"
DEFAULT_PATTERN_HEX = "5445535421"


def find_all(data: bytes, pattern: bytes):
    """Return the offsets of every occurrence of `pattern` in `data`.

    Overlapping matches are included: the search resumes one byte after
    each match start.
    """
    offsets = []
    start = data.find(pattern)
    while start != -1:
        offsets.append(start)
        start = data.find(pattern, start + 1)
    return offsets


if __name__ == "__main__":
    file_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_FILE
    pattern_hex = sys.argv[2] if len(sys.argv) > 2 else DEFAULT_PATTERN_HEX
    pattern = bytes.fromhex(pattern_hex)

    # Read the whole file in binary mode.
    data = file_path.read_bytes()

    offsets = find_all(data, pattern)

    print(f"File   : {file_path}")
    print(f"Size   : {len(data)} bytes")
    print(f"Pattern: {pattern_hex.upper()}")
    print(f"Count  : {len(offsets)}")
    for i, off in enumerate(offsets, 1):
        print(f"  #{i}: offset {off} (0x{off:X})")
