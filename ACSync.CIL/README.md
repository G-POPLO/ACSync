# ACSync — Incremental Update CLI Tool

A command-line tool for creating and applying incremental binary patches using 7z (LZMA2) compression.

## Install

```bash
dotnet tool install --global ACSync
```

## Usage

```
acsync <path> -l                  Create manifest
acsync <path> -u [-e ext1,ext2]   Apply patch
acsync <oldpath> <newpath> -m     Create patch
```

### Create manifest `-l`

Scan directory and generate `acsync_manifest.json` (SHA256 hashes for all files).

```bash
acsync D:\app\v1.0 -l
```

### Create patch `-m`

Compare old manifest with new directory, package delta as `acsync_patch.7z`.

```bash
acsync D:\app\v1.0 D:\app\v2.0 -m
```

If the new directory already has a manifest, it is loaded directly — no rescan needed.

### Apply patch `-u`

Extract `acsync_patch.7z` into target directory. Supports:

- **Auto-delete** — removes files that no longer exist in the new version
- **Exclude** — skip specific file types (config files, etc.)
- **Auto cleanup** — patch file deleted on success

```bash
acsync D:\app\user -u
acsync D:\app\user -u -e .json,.ini,.yaml
```

## Features

- SHA256 change detection — reliable, modification-time independent
- Multi-threaded scanning — fast for large directories
- LZMA2 compression — compact patches
- AOT compatible — low overhead, no heavy runtime dependencies
