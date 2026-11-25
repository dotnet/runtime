// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal static string? GetFileSystemTypeForPath(string path, IEnumerable<string> lines)
        {
            Dictionary<ulong, (ulong MountId, int MountPointLength, string FileSystemType)> childMounts = new();
            (ulong MountId, int MountPointLength, string FileSystemType) currentMount = (0, int.MaxValue, "");

            foreach (string line in lines)
            {
                if (!TryParseMountInfoLine(line, out ParsedMount mount) ||
                    !path.AsSpan().StartsWith(mount.MountPoint, StringComparison.Ordinal))
                {
                    continue;
                }

                int mountPointLength = mount.MountPoint.Length;
                if (mountPointLength > 1 && mountPointLength < path.Length && path[mountPointLength] != '/')
                {
                    continue;
                }

                if (!ulong.TryParse(mount.MountId, NumberStyles.None, CultureInfo.InvariantCulture, out ulong mountId) ||
                    !ulong.TryParse(mount.ParentId, NumberStyles.None, CultureInfo.InvariantCulture, out ulong parentId))
                {
                    continue;
                }

                var candidate = (MountId: mountId, MountPointLength: mountPointLength, FileSystemType: mount.FileSystemType.ToString());
                if (mountPointLength < currentMount.MountPointLength)
                {
                    currentMount = candidate;
                }

                // A shorter matching child hides longer matching siblings of the same parent.
                // https://man7.org/linux/man-pages/man5/proc_pid_mountinfo.5.html
                if (mountId != parentId &&
                    (!childMounts.TryGetValue(parentId, out var childMount) || mountPointLength < childMount.MountPointLength))
                {
                    childMounts[parentId] = candidate;
                }
            }

            // Bound traversal in case the mount table changes while it is read.
            for (int remaining = childMounts.Count; remaining >= 0; remaining--)
            {
                if (!childMounts.TryGetValue(currentMount.MountId, out var childMount))
                {
                    return currentMount.FileSystemType.Length == 0 ? null : currentMount.FileSystemType;
                }

                currentMount = childMount;
            }

            return null;
        }
    }
}
