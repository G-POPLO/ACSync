'use strict';

const path = require('path');

let _dotnet = null;
let _loaded = false;

/**
 * Lazy-loads the ACSync.Core assembly via node-api-dotnet.
 * @returns {object} The ACSync namespace with ManifestService and SyncPatch.
 */
function load() {
  if (_loaded) return _dotnet.ACSync;

  try {
    _dotnet = require('node-api-dotnet/net10.0');
  } catch {
    _dotnet = require('node-api-dotnet');
  }

  const dllPath = path.join(__dirname, 'lib', 'ACSync.Core.dll');
  _dotnet.load(dllPath);
  _loaded = true;
  return _dotnet.ACSync;
}

/**
 * Creates a manifest for the given directory.
 * @param {string} directoryPath - Path to the directory to scan.
 * @returns {object} The manifest object (JSON).
 */
function createManifest(directoryPath) {
  const acsync = load();
  const manifest = acsync.ManifestService.ScanDirectory(directoryPath);
  return JSON.parse(JSON.stringify(manifest)); // Convert .NET objects to plain JS
}

/**
 * Saves a manifest object to a JSON file.
 * @param {object} manifest - The manifest object.
 * @param {string} filePath - Output file path.
 */
function saveManifest(manifest, filePath) {
  const acsync = load();
  acsync.ManifestService.SaveToFile(manifest, filePath);
}

/**
 * Loads a manifest from a JSON file.
 * @param {string} filePath - Path to the manifest JSON file.
 * @returns {object} The manifest object.
 */
function loadManifest(filePath) {
  const acsync = load();
  const manifest = acsync.ManifestService.LoadFromFile(filePath);
  return JSON.parse(JSON.stringify(manifest));
}

/**
 * Loads a manifest from the acsync_manifest.json in a directory.
 * @param {string} directoryPath - Directory containing the manifest.
 * @returns {object} The manifest object.
 */
function loadManifestFromDirectory(directoryPath) {
  const acsync = load();
  const manifest = acsync.ManifestService.LoadFromDirectory(directoryPath);
  return JSON.parse(JSON.stringify(manifest));
}

/**
 * Diffs two manifests and returns changed/deleted file lists.
 * @param {object} oldManifest - The old manifest.
 * @param {object} newManifest - The new manifest.
 * @returns {{ changed: object[], deleted: object[] }}
 */
function diff(oldManifest, newManifest) {
  const acsync = load();
  const result = acsync.ManifestService.Diff(oldManifest, newManifest);
  return {
    changed: JSON.parse(JSON.stringify(result.changed)),
    deleted: JSON.parse(JSON.stringify(result.deleted)),
  };
}

/**
 * Creates a patch from old version to new version.
 * @param {string} oldPath - Path to the old version directory.
 * @param {string} newPath - Path to the new version directory.
 * @param {string} [outputPath] - Optional output path for the patch file.
 */
function createPatch(oldPath, newPath, outputPath) {
  const acsync = load();
  acsync.SyncPatch.CreatePatch(oldPath, newPath, outputPath || null);
}

/**
 * Applies a patch to the target directory.
 * @param {string} targetPath - Target directory to patch.
 * @param {string} [patchFile] - Path to the patch .7z file.
 * @param {string[]} [excludeExtensions] - File extensions to exclude (e.g. ['.json', '.yaml']).
 */
function applyPatch(targetPath, patchFile, excludeExtensions) {
  const acsync = load();
  acsync.SyncPatch.ApplyPatch(targetPath, patchFile || null, excludeExtensions || null);
}

module.exports = {
  createManifest,
  saveManifest,
  loadManifest,
  loadManifestFromDirectory,
  diff,
  createPatch,
  applyPatch,
};
