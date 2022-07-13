// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace System.Formats.Tar
{
    internal static partial class TarHelpers
    {
        private static readonly Lazy<UnixFileMode> s_umask = new Lazy<UnixFileMode>(DetermineUMask);

        private static UnixFileMode DetermineUMask()
        {
            Debug.Assert(!OperatingSystem.IsWindows());

            // To determine the umask, we'll create a file with full permissions and see
            // what gets filtered out.
            // note: only the owner of a file, and root can change file permissions.

            const UnixFileMode OwnershipPermissions =
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite |  UnixFileMode.OtherExecute;

            string filename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            FileStreamOptions options = new()
            {
                Mode = FileMode.CreateNew,
                UnixCreateMode = OwnershipPermissions,
                Options = FileOptions.DeleteOnClose,
                Access = FileAccess.Write,
                BufferSize = 0
            };
            using var fs = new FileStream(filename, options);
            UnixFileMode actual = File.GetUnixFileMode(fs.SafeFileHandle);

            return OwnershipPermissions & ~actual;
        }

        private sealed class ReverseStringComparer : IComparer<string>
        {
            public int Compare (string? x, string? y)
                => StringComparer.InvariantCulture.Compare(y, x);
        }

        private static readonly ReverseStringComparer s_reverseStringComparer = new();

        internal static UnixFileMode UMask => s_umask.Value;

        /*
            Tar files are usually ordered: parent directories come before their child entries.

            They may be unordered. In that case we need to create parent directories before
            we know the proper permissions for these directories.

            We create these directories with restrictive permissions. If we encounter an entry for
            the directory later, we store the mode to apply it later.

            If the archive doesn't have an entry for the parent directory, we use the default mask.

            The pending modes to be applied are tracked through a reverse-sorted dictionary.
            The reverse order is needed to apply permissions to children before their parent.
            Otherwise we may apply a restrictive mask to the parent, that prevents us from
            changing a child.
        */

        internal static SortedDictionary<string, UnixFileMode>? CreatePendingModesDictionary()
            => new SortedDictionary<string, UnixFileMode>(s_reverseStringComparer);

        internal static void CreateDirectory(string fullPath, UnixFileMode? mode, bool overwriteMetadata, SortedDictionary<string, UnixFileMode>? pendingModes)
        {
            // Restrictive mask for creating the missing parent directories while extracting.
            const UnixFileMode ExtractPermissions = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

            Debug.Assert(!OperatingSystem.IsWindows());
            Debug.Assert(pendingModes is not null);

            if (Directory.Exists(fullPath))
            {
                // Apply permissions to an existing directory when we're overwriting metadata
                // or the directory was created as a missing parent (stored in pendingModes).
                if (mode.HasValue)
                {
                    bool hasExtractPermissions = (mode & ExtractPermissions) == ExtractPermissions;
                    if (hasExtractPermissions)
                    {
                        bool removed = pendingModes.Remove(fullPath);
                        if (overwriteMetadata || removed)
                        {
                            UnixFileMode umask = UMask;
                            File.SetUnixFileMode(fullPath, mode.Value & ~umask);
                        }
                    }
                    else
                    {
                        if (overwriteMetadata || pendingModes.ContainsKey(fullPath))
                        {
                            pendingModes[fullPath] = mode.Value;
                        }
                    }
                }
                return;
            }

            if (mode.HasValue)
            {
                // Ensure we have sufficient permissions to extract in the directory.
                if ((mode & ExtractPermissions) != ExtractPermissions)
                {
                    pendingModes[fullPath] = mode.Value;
                    mode = ExtractPermissions;
                }
            }
            else
            {
                pendingModes.Add(fullPath, DefaultDirectoryMode);
                mode = ExtractPermissions;
            }

            string parentDir = Path.GetDirectoryName(fullPath)!;
            string rootDir = Path.GetPathRoot(parentDir)!;
            bool hasMissingParents = false;
            for (string dir = parentDir; dir != rootDir && !Directory.Exists(dir); dir = Path.GetDirectoryName(dir)!)
            {
                pendingModes.Add(dir, DefaultDirectoryMode);
                hasMissingParents = true;
            }

            if (hasMissingParents)
            {
                Directory.CreateDirectory(parentDir, ExtractPermissions);
            }

            Directory.CreateDirectory(fullPath, mode.Value);
        }

        internal static void SetPendingModes(SortedDictionary<string, UnixFileMode>? pendingModes)
        {
            Debug.Assert(!OperatingSystem.IsWindows());
            Debug.Assert(pendingModes is not null);

            if (pendingModes.Count == 0)
            {
                return;
            }

            UnixFileMode umask = UMask;
            foreach (var dir in pendingModes)
            {
                File.SetUnixFileMode(dir.Key, dir.Value & ~umask);
            }
        }
    }
}
