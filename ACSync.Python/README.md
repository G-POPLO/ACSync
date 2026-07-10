# acsync-core

[![PyPI](https://img.shields.io/pypi/v/acsync-core)](https://pypi.org/project/acsync-core/)
[![License](https://img.shields.io/pypi/l/acsync-core)](https://www.apache.org/licenses/LICENSE-2.0)
[![Python](https://img.shields.io/pypi/pyversions/acsync-core)](https://pypi.org/project/acsync-core/)

**acsync-core** is an incremental binary patch system for Python. It uses SHA256 for change detection and 7z (LZMA2) for compression — ideal for distributing application updates with minimal bandwidth.

> Powered by [ACSync.Core](https://github.com/G-POPLO/ACSync) (.NET) via [pythonnet](https://github.com/pythonnet/pythonnet).

## Installation

```bash
pip install acsync-core
```

> **Windows only** — the underlying .NET assembly and 7za.exe are bundled for Windows x64. Requires a [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later runtime on the target machine.

## Quick Start

### 1. Create a manifest

```python
import acsync

# Scan directory A (v1.0) and save its manifest
manifest_a = acsync.create_manifest(r"D:\app\v1.0")
acsync.save_manifest(manifest_a, r"D:\app\manifest_v1.json")

# Scan directory B (v2.0) and save its manifest
manifest_b = acsync.create_manifest(r"D:\app\v2.0")
acsync.save_manifest(manifest_b, r"D:\app\manifest_v2.json")
```

### 2. Diff two manifests

```python
diff = acsync.diff(manifest_a, manifest_b)
print(f"Changed: {len(diff['changed'])} files")
print(f"Deleted: {len(diff['deleted'])} files")
for f in diff["changed"]:
    print(f"  + {f['relativePath']} ({f['sha256'][:12]}...)")
```

### 3. Create a patch

```python
acsync.create_patch(
    r"D:\app\v1.0",
    r"D:\app\v2.0",
    r"D:\app\update_patch.7z",
)
```

### 4. Apply a patch

```python
acsync.apply_patch(
    r"D:\deployed_app",
    r"D:\app\update_patch.7z",
    exclude_extensions=[".config"],  # preserve local config files
)
```

## API

| Function | Description |
|---|---|
| `create_manifest(directory)` | Scan a directory, returns manifest dict with SHA256 per file |
| `save_manifest(manifest, path)` | Save manifest dict to JSON file |
| `load_manifest(path)` | Load manifest from JSON file |
| `load_manifest_from_directory(dir)` | Load manifest from a directory (`acsync_manifest.json`) |
| `diff(old, new)` | Compare two manifests, returns `{changed: [...], deleted: [...]}` |
| `create_patch(old_dir, new_dir, output?)` | Create incremental 7z patch between two directories |
| `apply_patch(target, patch?, exclude?)` | Apply patch to target directory |

## License

Apache 2.0
