"""
Read a depth log text file and convert each record's date/time to a hex string.

Expected input file layout (fields separated by one or more tabs/spaces):

    Date            Time            MD      TVD          <- line 1: column labels (skipped)
    [dd.mm.yyyy]    [hh:mm:ss]      [m]     [m]          <- line 2: units (skipped)
    27.10.2026      10:58:00        2136    1372.98      <- line 3+: data records
    ...

Output format for each record:
    YY MM DD hh mm ss, each converted to a 2-digit uppercase hex value
    and concatenated, e.g. 27.10.2026 10:58:00 -> 1A0A1B0A3A00
"""

import sys
from pathlib import Path

# Default input file: located in the same directory as this script.
DEFAULT_FILE = Path(__file__).parent / "SG#1ZH~Run2~20261031154119~Depth.txt"

# Number of header lines (labels + units) to skip before the data begins.
HEADER_LINES = 2


def to_hex_datetime(date_str: str, time_str: str) -> str:
    """
    Convert a date string and a time string into a concatenated hex string.

    Args:
        date_str: Date in "dd.mm.yyyy" format, e.g. "27.10.2026".
        time_str: Time in "hh:mm:ss" format, e.g. "10:58:00".

    Returns:
        A 12-character uppercase hex string in the order
        year(2 digits) month day hour minute second,
        e.g. "1A0A1B0A3A00".
    """
    # Split the date into integers; the file uses day-first ordering.
    day, month, year = (int(x) for x in date_str.split("."))
    # Split the time into integer hour, minute and second.
    hour, minute, second = (int(x) for x in time_str.split(":"))

    # Keep only the last two digits of the year (2026 -> 26).
    # Output order is: year, month, day, hour, minute, second.
    fields = (year % 100, month, day, hour, minute, second)

    # Format each value as 2-digit uppercase hex (e.g. 26 -> "1A", 14 -> "0E")
    # and join them without separators.
    return "".join(f"{v:02X}" for v in fields)


def read_file(path: Path):
    """
    Read the depth file and return the date/time of every data record.

    The first HEADER_LINES lines are labels/units and are skipped.

    Args:
        path: Path to the input text file.

    Returns:
        A list of tuples: (date_str, time_str, hex_str).
    """
    results = []

    # "utf-8-sig" transparently strips a BOM if the file has one.
    with open(path, "r", encoding="utf-8-sig") as f:
        # splitlines() handles \n and \r\n; drop the header lines.
        lines = f.read().splitlines()[HEADER_LINES:]

    for line in lines:
        # split() with no argument splits on any run of whitespace,
        # so multiple consecutive tabs/spaces act as a single separator.
        parts = line.split()

        # Skip blank or malformed lines that lack both date and time.
        if len(parts) < 2:
            continue

        # The first two columns are date and time; remaining columns
        # (MD, TVD) are not needed here.
        date_str, time_str = parts[0], parts[1]
        results.append((date_str, time_str, to_hex_datetime(date_str, time_str)))

    return results


if __name__ == "__main__":
    # Optional command line argument overrides the default input file:
    #   python read_depth.py "path/to/file.txt"
    file_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_FILE

    # Print each record as: "<date> <time> -> <hex>"
    for date_str, time_str, hex_str in read_file(file_path):
        print(f"{date_str} {time_str} -> {hex_str}")
