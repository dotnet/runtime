// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal const string ProcMountInfoFilePath = "/proc/self/mountinfo";

        internal static Error GetFileSystemTypeForRealPath(string path, out string format)
        {
            format = "";

            if (File.Exists(ProcMountInfoFilePath))
            {
                try
                {
                    ReadOnlySpan<char> currentFormat = default;
                    int currentBestLength = 0;

                    using StreamReader reader = new(ProcMountInfoFilePath);

                    string? line;
                    while ((line = reader.ReadLine()) is not null)
                    {
                        if (TryParseMountInfoLine(line, out ParsedMount mount))
                        {
                            if (mount.MountPoint.Length < currentBestLength)
                            {
                                continue;
                            }

                            if (!path.StartsWith(mount.MountPoint))
                            {
                                continue;
                            }

                            if (mount.MountPoint.Length == path.Length)
                            {
                                currentFormat = mount.FileSystemType;
                                break;
                            }

                            if (mount.MountPoint.Length > 1 && path[mount.MountPoint.Length] != '/')
                            {
                                continue;
                            }

                            currentBestLength = mount.MountPoint.Length;
                            currentFormat = mount.FileSystemType;
                        }
                    }

                    if (currentFormat.Length > 0)
                    {
                        format = currentFormat.ToString();
                        return Error.SUCCESS;
                    }
                    return Error.ENOENT;
                }
                catch (Exception e)
                {
                    Debug.Fail($"Failed to read \"{ProcMountInfoFilePath}\": {e}");
                }
            }

            return Error.ENOTSUP;
        }
    }
}
