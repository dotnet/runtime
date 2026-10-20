// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Formats.Tar
{
    internal struct FileId : IEquatable<FileId>
    {
        internal long Dev;
        internal long Ino;

        internal FileId(long dev, long ino)
        {
            Dev = dev;
            Ino = ino;
        }

        public bool Equals(FileId other) => Dev == other.Dev && Ino == other.Ino;
        public override bool Equals(object? obj) => obj is FileId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Dev, Ino);
    }

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

        internal static FileId GetFileId(string filePath)
        {
            Interop.CheckIo(Interop.Sys.Stat(filePath, out Interop.Sys.FileStatus status), filePath);
            return new FileId(status.Dev, status.Ino);
        }

        internal static bool TryGetPathInfo(string path, out FileId fileId, out bool isSymlink)
        {
            int rv = Interop.Sys.LStat(path, out Interop.Sys.FileStatus status);
            if (rv != 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.ENOENT || errorInfo.Error == Interop.Error.ENOTDIR)
                {
                    fileId = default;
                    isSymlink = false;
                    return false;
                }
                throw Interop.GetExceptionForIoErrno(errorInfo, path);
            }
            fileId = new FileId(status.Dev, status.Ino);
            isSymlink = (status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK;
            return true;
        }
    }
}
