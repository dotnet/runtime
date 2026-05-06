// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Formats.Tar
{
    internal static partial class TarHelpers
    {
        private static readonly Lazy<UnixFileMode> s_umask = new Lazy<UnixFileMode>(DetermineUMask);

        private static UnixFileMode DetermineUMask()
        {
            // To determine the umask, we'll create a file with full permissions and see
            // what gets filtered out.
            // note: only the owner of a file, and root can change file permissions.

            const UnixFileMode OwnershipPermissions =
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

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
            public int Compare(string? x, string? y)
                => StringComparer.Ordinal.Compare(y, x);
        }

        private static readonly ReverseStringComparer s_reverseStringComparer = new();

        private static UnixFileMode UMask => s_umask.Value;

        // Use a reverse-sorted dictionary to apply permission to children before their parents.
        // Otherwise we may apply a restrictive mask to the parent, that prevents us from changing a child.
        internal static SortedDictionary<string, UnixFileMode>? CreatePendingModesDictionary()
            => new SortedDictionary<string, UnixFileMode>(s_reverseStringComparer);

        internal static void CreateDirectory(string fullPath, UnixFileMode? mode, SortedDictionary<string, UnixFileMode>? pendingModes)
        {
            // Minimal permissions required for extracting.
            const UnixFileMode ExtractPermissions = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

            Debug.Assert(pendingModes is not null);

            if (Directory.Exists(fullPath))
            {
                // Apply permissions to an existing directory.
                if (mode.HasValue)
                {
                    // Ensure we have sufficient permissions to extract in the directory.
                    bool hasExtractPermissions = (mode.Value & ExtractPermissions) == ExtractPermissions;
                    if (hasExtractPermissions)
                    {
                        pendingModes.Remove(fullPath);

                        UnixFileMode umask = UMask;
                        File.SetUnixFileMode(fullPath, mode.Value & ~umask);
                    }
                    else
                    {
                        pendingModes[fullPath] = mode.Value;
                    }
                }
                return;
            }

            // If there are missing parents, Directory.CreateDirectory will create them using default permissions.
            if (mode.HasValue)
            {
                // Ensure we have sufficient permissions to extract in the directory.
                if ((mode.Value & ExtractPermissions) != ExtractPermissions)
                {
                    pendingModes[fullPath] = mode.Value;
                    mode = ExtractPermissions;
                }

                Directory.CreateDirectory(fullPath, mode.Value);
            }
            else
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        internal static void SetPendingModes(SortedDictionary<string, UnixFileMode>? pendingModes)
        {
            Debug.Assert(pendingModes is not null);

            if (pendingModes.Count == 0)
            {
                return;
            }

            UnixFileMode umask = UMask;
            foreach (KeyValuePair<string, UnixFileMode> dir in pendingModes)
            {
                File.SetUnixFileMode(dir.Key, dir.Value & ~umask);
            }
        }
    }
}
