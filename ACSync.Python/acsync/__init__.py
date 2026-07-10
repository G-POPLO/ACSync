"""
ACSync.Core — Incremental binary patch system for Python.

Provides functions to create manifests, diff directories, and create/apply
binary patches using SHA256 for change detection and 7z (LZMA2) for compression.
"""

import os as _os
import sys as _sys

_lib_dir = _os.path.join(_os.path.dirname(__file__), "lib")
_seven_zip_path = _os.path.join(_lib_dir, "7za.exe")

# Load the .NET assembly via pythonnet
import clr as _clr  # noqa: E402
_sys.path.insert(0, _lib_dir)
_clr.AddReference("ACSync.Core")
from ACSync import (  # noqa: E402
    ManifestService as _ManifestService,
    SyncPatch as _SyncPatch,
)


def create_manifest(directory_path):
    """Scan a directory and return a manifest dict with SHA256 of every file."""
    manifest = _ManifestService.ScanDirectory(directory_path)
    return _to_dict(manifest)


def save_manifest(manifest, file_path):
    """Save a manifest dict to a JSON file."""
    _ManifestService.SaveToFile(_to_net_manifest(manifest), file_path)


def load_manifest(file_path):
    """Load a manifest from a JSON file and return a dict."""
    manifest = _ManifestService.LoadFromFile(file_path)
    return _to_dict(manifest)


def load_manifest_from_directory(directory_path):
    """Load the manifest file (acsync_manifest.json) from a directory."""
    manifest = _ManifestService.LoadFromDirectory(directory_path)
    return _to_dict(manifest)


def diff(old_manifest, new_manifest):
    """Compare two manifests and return changed/deleted file lists.

    Returns:
        dict with keys:
          - 'changed': list of file entry dicts (new/changed files)
          - 'deleted': list of file entry dicts (removed files)
    """
    old = _to_net_manifest(old_manifest)
    new = _to_net_manifest(new_manifest)
    changed, deleted = _ManifestService.Diff(old, new)
    return {
        "changed": _to_dict_list(changed),
        "deleted": _to_dict_list(deleted),
    }


def create_patch(old_path, new_path, output_path=None):
    """Create an incremental patch between two directories.

    Args:
        old_path: Path to the old (base) version directory.
        new_path: Path to the new version directory.
        output_path: Optional path for the output .7z patch file.
    """
    _SyncPatch.CreatePatch(old_path, new_path, output_path or None, _seven_zip_path)


def apply_patch(target_path, patch_file=None, exclude_extensions=None):
    """Apply a patch to a target directory.

    Args:
        target_path: Target directory to patch.
        patch_file: Path to the .7z patch file. Defaults to 'acsync_patch.7z' in CWD.
        exclude_extensions: List of file extensions to skip (e.g. ['.exe', '.dll']).
    """
    if exclude_extensions is not None:
        arr = _clr.System.Array[str](exclude_extensions)
    else:
        arr = None
    _SyncPatch.ApplyPatch(target_path, patch_file or None, arr, _seven_zip_path)


# ── Helpers: .NET object ↔ Python dict ──────────────────────────────────────


def _to_dict(manifest):
    """Convert a .NET SyncManifest to a plain Python dict."""
    return {
        "files": [
            {
                "relativePath": entry.RelativePath,
                "lastWriteTimeUtc": entry.LastWriteTimeUtc.ToString("o"),
                "length": entry.Length,
                "sha256": entry.Sha256,
            }
            for entry in manifest.Files
        ]
    }


def _to_dict_list(entries):
    """Convert a .NET List<FileEntry> to a list of Python dicts."""
    return [
        {
            "relativePath": entry.RelativePath,
            "lastWriteTimeUtc": entry.LastWriteTimeUtc.ToString("o"),
            "length": entry.Length,
            "sha256": entry.Sha256,
        }
        for entry in entries
    ]


def _to_net_manifest(manifest_dict):
    """Convert a Python manifest dict back to a .NET SyncManifest."""
    manifest_type = _clr.System.Type.GetType(
        "ACSync.SyncManifest, ACSync.Core"
    )
    net_manifest = _clr.System.Activator.CreateInstance(manifest_type)
    net_files = net_manifest.Files

    entry_type = _clr.System.Type.GetType(
        "ACSync.FileEntry, ACSync.Core"
    )
    for f in manifest_dict.get("files", []):
        entry = _clr.System.Activator.CreateInstance(entry_type)
        entry.RelativePath = f["relativePath"]
        entry.LastWriteTimeUtc = _clr.System.DateTime.Parse(f["lastWriteTimeUtc"])
        entry.Length = f["length"]
        entry.Sha256 = f["sha256"]
        net_files.Add(entry)

    return net_manifest


__all__ = [
    "create_manifest",
    "save_manifest",
    "load_manifest",
    "load_manifest_from_directory",
    "diff",
    "create_patch",
    "apply_patch",
]
