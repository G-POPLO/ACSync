# acsync-core

Incremental update/patch system for binary files. .NET Core library exposed for Node.js via [node-api-dotnet](https://www.npmjs.com/package/node-api-dotnet).

## Install

```bash
npm install acsync-core
```

Requires **Node.js 18+** and the **.NET 10 runtime** (bundled with `node-api-dotnet`).

## Usage

```js
const acsync = require('acsync-core');

// 1. Scan a directory to create a manifest
const manifest = acsync.createManifest('D:\\app\\v2.0');
console.log(`Found ${manifest.files.length} files`);

// 2. Save/Load manifest
acsync.saveManifest(manifest, './manifest.json');
const loaded = acsync.loadManifest('./manifest.json');

// 3. Diff two manifests
const oldManifest = acsync.loadManifestFromDirectory('D:\\app\\v1.0');
const newManifest = acsync.createManifest('D:\\app\\v2.0');
const { changed, deleted } = acsync.diff(oldManifest, newManifest);
console.log(`${changed.length} changed, ${deleted.length} deleted`);

// 4. Create a patch
acsync.createPatch('D:\\app\\v1.0', 'D:\\app\\v2.0');

// 5. Apply a patch (requires 7za.exe in PATH or app directory)
acsync.applyPatch('D:\\app\\user', null, ['.json', '.yaml']);
```

## API

| Method | Description |
|---|---|
| `createManifest(path)` | Scan directory, return manifest with SHA256 hashes |
| `saveManifest(manifest, filePath)` | Save manifest to JSON file |
| `loadManifest(filePath)` | Load manifest from JSON file |
| `loadManifestFromDirectory(path)` | Load manifest from a directory's `acsync_manifest.json` |
| `diff(oldManifest, newManifest)` | Compare two manifests, return `{ changed, deleted }` |
| `createPatch(oldPath, newPath, outputPath?)` | Build delta patch as `.7z` |
| `applyPatch(targetPath, patchFile?, excludeExts?)` | Apply patch to target directory |

> **Note:** `createPatch` and `applyPatch` require `7za.exe` (from [7-Zip.CommandLine](https://www.nuget.org/packages/7-Zip.CommandLine)) to be available in the process working directory or PATH.

## Requirements

- Node.js >= 18
- .NET 10 runtime (auto-installed by `node-api-dotnet`)
- Windows (for patching with 7za.exe; manifest scanning works cross-platform)
