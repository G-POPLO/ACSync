export interface FileEntry {
  relativePath: string;
  lastWriteTimeUtc: string;
  length: number;
  sha256: string;
}

export interface Manifest {
  files: FileEntry[];
}

export interface DiffResult {
  changed: FileEntry[];
  deleted: FileEntry[];
}

/**
 * Scans a directory and creates a manifest with SHA256 hashes for all files.
 */
export function createManifest(directoryPath: string): Manifest;

/**
 * Saves a manifest object to a JSON file.
 */
export function saveManifest(manifest: Manifest, filePath: string): void;

/**
 * Loads a manifest from a JSON file.
 */
export function loadManifest(filePath: string): Manifest;

/**
 * Loads a manifest from the acsync_manifest.json in a directory.
 */
export function loadManifestFromDirectory(directoryPath: string): Manifest;

/**
 * Compares two manifests and returns changed/deleted file lists.
 */
export function diff(oldManifest: Manifest, newManifest: Manifest): DiffResult;

/**
 * Creates a patch from old version to new version.
 * @param oldPath - Path to the old version directory.
 * @param newPath - Path to the new version directory.
 * @param outputPath - Optional output path for the patch file.
 */
export function createPatch(oldPath: string, newPath: string, outputPath?: string): void;

/**
 * Applies a patch to the target directory.
 * @param targetPath - Target directory to patch.
 * @param patchFile - Path to the patch .7z file (default: acsync_patch.7z in CWD).
 * @param excludeExtensions - File extensions to exclude (e.g. ['.json', '.yaml']).
 */
export function applyPatch(targetPath: string, patchFile?: string, excludeExtensions?: string[]): void;
