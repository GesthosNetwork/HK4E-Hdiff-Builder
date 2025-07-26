## HK4E Hdiff Builder

A tool to generate hdiff update files between two versions of an Anime Game. This tool replicates the official patch structure. Output is 1:1 identical to miHoYo’s format, but with improved compression. Unlike miHoYo’s use of ZIP, this tool utilizes 7z and hdiffz.exe with ultra compression settings, producing smaller and more efficient patch files with 100% full compatibility.


## Features

- Compares two full game folders (old & new)
- Outputs clean `.hdiff.7z` update files
- Sequential or parallel diffing
- Multithreaded (configurable)
- Strict version checks
- Full logging


## Folder Structure

```
/your-root-folder/  
├── GenshinImpact_5.5.0/                  ← Old version folder  
├── GenshinImpact_5.6.0/                  ← New version folder  
├── hdiffbuilder.exe                      ← Main executable  
├── config.json                           ← Execution config  
├── game_5.5.0_5.6.0_hdiff.7z             ← Patch output  
├── audio_en-us_5.5.0_5.6.0_hdiff.7z      ← Patch output  
├── audio_ja-jp_5.5.0_5.6.0_hdiff.7z      ← Patch output  
├── audio_ko-kr_5.5.0_5.6.0_hdiff.7z      ← Patch output  
└── audio_zh-cn_5.5.0_5.6.0_hdiff.7z      ← Patch output
```

Also, you can use YuanShen.


## config.json

This file must be in the same folder as the `.exe`.
It will be auto-generated on first run if missing.

```json
{
  "old_ver": "5.5.0",
  "new_ver": "5.6.0",
  "mode": 0,
  "max_threads": 4,
  "keep_source_folder": false,
  "log_level": "DEBUG"
}
```

| Parameters | Type | Description |
|----------------------|---------|-------------|
| `old_ver`            | string  | Source version (folder must exist) |
| `new_ver`            | string  | Target version (folder must exist) |
| `mode`               | int     | 0 = sequential, 1 = parallel |
| `max_threads`        | int     | Number of worker threads (1–CPU core count) |
| `keep_source_folder` | bool    | Keep old & new game folders after patching |
| `log_level`          | string  | DEBUG, INFO, WARN, ERROR, FATAL |

If the config is malformed or has invalid types, the program will terminate immediately.


## Usage

1. Place `hdiffbuilder.exe` in the same folder as both game versions.
2. Edit `config.json` with correct versions.
3. Run the EXE directly.
4. Output `.hdiff.7z` files will be generated in the same directory.


## Build Instructions

1. Install [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)  
2. Run `compile.bat`  
3. Output will be in `bin/hdiffbuilder.exe`


## Disclaimer

This tool is for reverse engineering & educational use only.  
Not affiliated with miHoYo, Cognosphere, or any official entity.  
Do not use this project for public distribution or commercial purposes.
