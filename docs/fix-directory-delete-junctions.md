# Fix: `Directory.Delete(path, recursive: true)` Fails on Directories Containing Junctions

## Background

On Windows, NTFS supports several types of reparse points for directory traversal:

| Type | Reparse Tag | Created by |
|---|---|---|
| Symbolic link | `IO_REPARSE_TAG_SYMLINK` (0xA000000C) | `mklink /D`, `Directory.CreateSymbolicLink` |
| Directory junction | `IO_REPARSE_TAG_MOUNT_POINT` (0xA0000003) | `mklink /J`, popular tools like `pnpm` |
| Volume mount point | `IO_REPARSE_TAG_MOUNT_POINT` (0xA0000003) | `mountvol`, disk management |

**Key insight**: directory junctions and volume mount points share the same reparse tag (`IO_REPARSE_TAG_MOUNT_POINT`). The difference is in their reparse data:
- Volume mount points store a volume GUID path (`\??\Volume{GUID}\`)
- Directory junctions store a target directory path (`\??\C:\path\to\target`)

## Root Cause

`Directory.Delete(path, recursive: true)` on Windows is implemented in
`FileSystem.Windows.cs` in the `RemoveDirectoryRecursive` method. When the code
encounters a name-surrogate reparse point with the `IO_REPARSE_TAG_MOUNT_POINT` tag,
it calls `DeleteVolumeMountPoint` before `RemoveDirectory`:

```csharp
// BEFORE (buggy)
if (findData.dwReserved0 == Interop.Kernel32.IOReparseOptions.IO_REPARSE_TAG_MOUNT_POINT)
{
    string mountPoint = Path.Join(fullPath, fileName, PathInternal.DirectorySeparatorCharAsString);
    if (!Interop.Kernel32.DeleteVolumeMountPoint(mountPoint) && exception == null)
    {
        errorCode = Marshal.GetLastPInvokeError();
        if (errorCode != Interop.Errors.ERROR_SUCCESS &&
            errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND)
        {
            exception = Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);  // <-- sets exception
        }
    }
}

// Note that RemoveDirectory on a symbolic link will remove the link itself.
if (!Interop.Kernel32.RemoveDirectory(Path.Combine(fullPath, fileName)) && exception == null)
{
    // exception is already set from above, so this block is skipped
    // even though RemoveDirectory succeeds!
}
```

`DeleteVolumeMountPoint` is a Windows API that only works for **volume mount points** — it cannot unmount a directory junction. When called on a junction, it fails and sets an error code (e.g., `ERROR_INVALID_PARAMETER` or `ERROR_NOT_A_REPARSE_POINT`).

This error was stored directly in the outer `exception` variable. Then `RemoveDirectory` was called and **succeeded** (removing the junction), but the error from `DeleteVolumeMountPoint` was already stored. After the loop, `exception != null` caused the exception to be thrown and `RemoveDirectoryInternal` (which removes the parent directory) was never called, leaving the parent directory behind.

## The Fix

The fix uses a local `mountPointException` variable to capture the `DeleteVolumeMountPoint` failure, without storing it in the loop's `exception` variable. This exception is only promoted to `exception` if `RemoveDirectory` **also** fails — meaning the directory could not be removed by either mechanism.

```csharp
// AFTER (fixed)
Exception? mountPointException = null;
if (findData.dwReserved0 == Interop.Kernel32.IOReparseOptions.IO_REPARSE_TAG_MOUNT_POINT)
{
    string mountPoint = Path.Join(fullPath, fileName, PathInternal.DirectorySeparatorCharAsString);
    if (!Interop.Kernel32.DeleteVolumeMountPoint(mountPoint))
    {
        errorCode = Marshal.GetLastPInvokeError();
        if (errorCode != Interop.Errors.ERROR_SUCCESS &&
            errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND)
        {
            mountPointException = Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);
        }
    }
}

if (!Interop.Kernel32.RemoveDirectory(Path.Combine(fullPath, fileName)))
{
    if (exception == null)
    {
        errorCode = Marshal.GetLastPInvokeError();
        if (errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND)
        {
            // For a true volume mount point, use its error (it indicates why the
            // unmount step failed). If this is a directory junction, RemoveDirectory
            // succeeds and this code path is not reached.
            exception = mountPointException ?? Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);
        }
    }
}
// If RemoveDirectory succeeded, mountPointException is discarded. This correctly
// handles directory junctions: DeleteVolumeMountPoint fails for them (since they
// are not volume mount points), but RemoveDirectory removes them successfully.
```

## Behavior Matrix

| Scenario | `DeleteVolumeMountPoint` | `RemoveDirectory` | Result |
|---|---|---|---|
| Volume mount point (normal) | ✅ succeeds | ✅ succeeds | Directory removed, no exception |
| Volume mount point (access denied) | ❌ fails | ❌ fails | Exception from `DeleteVolumeMountPoint` (descriptive) |
| Directory junction | ❌ fails (expected) | ✅ succeeds | `mountPointException` discarded, directory removed ✅ **BUG FIX** |
| Symbolic link | N/A (different tag) | ✅ succeeds | Directory removed, no exception |
| Symlink (access denied) | N/A | ❌ fails | Exception from `RemoveDirectory` |

## Test Coverage

A new Windows-specific test `RecursiveDelete_DirectoryContainingJunction` was added to
`Directory/Delete.Windows.cs`. It:

1. Creates a parent directory and a separate target directory
2. Creates a junction inside the parent pointing to the target (using `MountHelper.CreateJunction`)
3. Calls `Directory.Delete(linkParent, recursive: true)` and asserts it succeeds
4. Verifies the parent directory is deleted and the target directory still exists
   (the junction should not be followed)

This test would have failed with the old code (throwing `UnauthorizedAccessException` or
`IOException` depending on privilege level) and passes with the fix.

## Platforms Affected

This bug is Windows-only. Directory junctions are an NTFS-specific concept and the affected
code path (`FileSystem.Windows.cs`) is only compiled for Windows. The fix has no impact on
Linux or macOS behavior.

## Impact

Directory junctions are commonly created by package managers (notably `pnpm`) in
`node_modules` directories. Any application doing recursive directory cleanup on directories
created by these tools was affected by this bug.
