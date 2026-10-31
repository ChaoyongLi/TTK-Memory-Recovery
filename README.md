# TTK Memory

Tools for repairing TTK memory dump (`.img`) files whose timestamps or data blocks are damaged.

| Path | Description |
| --- | --- |
| `TTK Memory Recover/` | Avalonia (.NET 10) desktop app: the main tool. |
| `read_depth.py` | Reads a depth `.txt` file and prints each date/time as hex (`YYMMDDhhmmss`, one hex byte each). |
| `find_pattern.py` | Searches a binary file for a hex byte pattern and lists the count and offsets. |
| `replace_timestamp.py` | Patches the 6-byte timestamps after each `TEST!` marker in an `.img` using a depth `.txt`. |

## TTK Memory Recover (desktop app)

Current version: **1.0.0** (set in `TTK Memory Recover/TtkMemoryRecover.csproj`, shown in the window title and UI).

### Requirements

- Build: [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Run the published exe: Windows x64 only; no .NET install needed (self-contained).

### Build and run

```powershell
cd "TTK Memory Recover"

# Build only (Debug)
dotnet build

# Build only (Release)
dotnet build -c Release

# Build and run
dotnet run
```

Build output: `TTK Memory Recover/bin/<Configuration>/net10.0/win-x64/TTK Memory Recover.exe`.

### Publish a single-file exe

```powershell
cd "TTK Memory Recover"
dotnet publish -c Release -o publish
```

Output: `publish/TTK Memory Recover.exe` (self-contained, win-x64, single file).

### Optional: Native AOT build

The app is Native AOT compatible (faster startup, lower memory). It needs the Visual Studio C++ build tools ("Desktop development with C++") and produces an exe plus the Avalonia native libraries (`libSkiaSharp.dll`, `libHarfBuzzSharp.dll`, `av_libglesv2.dll`) that must stay in the same folder, so it is **not** a single file.

```powershell
cd "TTK Memory Recover"
dotnet publish -c Release -p:PublishAot=true -p:PublishSingleFile=false -o publish-aot
```

If MSBuild reports `MSB3061` (cannot delete the exe) right after a build, run the command again; the file is briefly locked by antivirus.

### Usage

1. Start the app and click **Open IMG File...** to choose an `.img` file.
   The app searches for the marker `464F524D4154494F4E205052455353555245205445535421`
   (ASCII `FORMATION PRESSURE TEST!`). The 6 bytes after each marker are the timestamp
   (`YY MM DD hh mm ss`, one binary byte each). A timestamp that is not a valid date/time is shown as `YYmmddHHmmss`.
2. **Time Recovery** tab
   - Click **Depth File...** to load a depth `.txt` (first two lines are headers; columns `dd.mm.yyyy` and `hh:mm:ss`).
   - Each time has a drop-down with the times read from the depth file; nothing is selected by default.
   - **Save As...** writes a new `.img` in which only the rows with a selected time are replaced.
3. **Memory Recovery** tab
   - Tick one or more times. The memory from just after a ticked time's 6 timestamp bytes to the start of the next unticked time's marker (or end of file) is scanned.
   - A match is a flag block (3 or more `AA` bytes) with an optional repeat block (a 1-64 byte unit repeated at least twice) directly before and after it.
   - Each match shows 10 bytes of context, the repeat block (blue), the flag block (red), the repeat block, and 10 bytes of context.
   - **Apply** marks the flag block plus the repeat block after it for deletion (shown with strikethrough; **Undo** reverts it). Nothing changes in the file yet.
   - **Save As...** writes a new `.img` with all applied ranges removed.

The original `.img` is never modified or overwritten. Time Recovery and Memory Recovery each work from the originally opened file, so their results are not combined.

### Project layout

```
TTK Memory Recover/
  Core/                  UI-independent logic (marker parsing, depth parsing, scanning, writing)
  App.axaml              Theme and shared styles
  MainWindow.axaml       Start screen and tabs
  TimeRecoveryView.*     Time Recovery tab
  MemoryRecoveryView.*   Memory Recovery tab
  AppInfo.cs             Version lookup
  app.ico                Application icon
```

## Python scripts

Run with [uv](https://docs.astral.sh/uv/) (Python 3.14):

```powershell
uv run read_depth.py
python find_pattern.py "<file>" 5445535421
python replace_timestamp.py "<img>" "<depth txt>"
```

`replace_timestamp.py` modifies the `.img` in place; keep a backup copy first.

## Versioning

Change `<Version>` in `TTK Memory Recover/TtkMemoryRecover.csproj` and update the version above when releasing.
