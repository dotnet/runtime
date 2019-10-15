// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;

internal static partial class Interop
{
    internal static partial class cgroups
    {
        /// <summary>Path to mountinfo file in procfs for the current process.</summary>
        private const string ProcMountInfoFilePath = "/proc/self/mountinfo";
        /// <summary>Path to cgroup directory in procfs for the current process.</summary>
        private const string ProcCGroupFilePath = "/proc/self/cgroup";

        /// <summary>Path to the found cgroup location, or null if it couldn't be found.</summary>
        internal static readonly string s_cgroupMemoryPath = FindCGroupPath("memory");
        /// <summary>Path to the found cgroup memory limit_in_bytes path, or null if it couldn't be found.</summary>
        private static readonly string s_cgroupMemoryLimitPath = s_cgroupMemoryPath != null ? s_cgroupMemoryPath + "/memory.limit_in_bytes" : null;

        /// <summary>Tries to read the memory limit from the cgroup memory location.</summary>
        /// <param name="limit">The read limit, or 0 if it couldn't be read.</param>
        /// <returns>true if the limit was read successfully; otherwise, false.</returns>
        public static bool TryGetMemoryLimit(out ulong limit)
        {
            string path = s_cgroupMemoryLimitPath;

            if (path != null &&
                TryReadMemoryValueFromFile(path, out limit))
            {
                return true;
            }

            limit = 0;
            return false;
        }

        /// <summary>Tries to parse a memory limit from the specified file.</summary>
        /// <param name="path">The path to the file to parse.</param>
        /// <param name="result">The parsed result, or 0 if it couldn't be parsed.</param>
        /// <returns>true if the value was read successfully; otherwise, false.</returns>
        private static bool TryReadMemoryValueFromFile(string path, out ulong result)
        {
            if (File.Exists(path))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    if (Utf8Parser.TryParse(bytes, out ulong ulongValue, out int bytesConsumed))
                    {
                        // If we successfully parsed the number, see if there's a K, M, or G
                        // multiplier value immediately following.
                        ulong multiplier = 1;
                        if (bytesConsumed < bytes.Length)
                        {
                            switch (bytes[bytesConsumed])
                            {

                                case (byte)'k':
                                case (byte)'K':
                                    multiplier = 1024;
                                    break;

                                case (byte)'m':
                                case (byte)'M':
                                    multiplier = 1024 * 1024;
                                    break;

                                case (byte)'g':
                                case (byte)'G':
                                    multiplier = 1024 * 1024 * 1024;
                                    break;
                            }
                        }

                        result = checked(ulongValue * multiplier);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Debug.Fail($"Failed to read \"{path}\": {e}");
                }
            }

            result = 0;
            return false;
        }

        /// <summary>Find the cgroup path for the specified subsystem.</summary>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <returns>The cgroup path if found; otherwise, null.</returns>
        private static string FindCGroupPath(string subsystem)
        {
            if (TryFindHierarchyMount(subsystem, out string hierarchyRoot, out string hierarchyMount) &&
                TryFindCGroupPathForSubsystem(subsystem, out string cgroupPathRelativeToMount))
            {
                // For a host cgroup, we need to append the relative path.
                // In a docker container, the root and relative path are the same and we don't need to append.
                return (hierarchyRoot != cgroupPathRelativeToMount) ?
                    hierarchyMount + cgroupPathRelativeToMount :
                    hierarchyMount;
            }

            return null;
        }

        /// <summary>Find the cgroup mount information for the specified subsystem.</summary>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <param name="root">The path of the directory in the filesystem which forms the root of this mount; null if not found.</param>
        /// <param name="path">The path of the mount point relative to the process's root directory; null if not found.</param>
        /// <returns>true if the mount was found; otherwise, null.</returns>
        private static bool TryFindHierarchyMount(string subsystem, out string root, out string path)
        {
            if (File.Exists(ProcMountInfoFilePath))
            {
                try
                {
                    using (var reader = new StreamReader(ProcMountInfoFilePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Look for an entry that has cgroup as the "filesystem type"
                            // and that has options containing the specified subsystem.
                            // See man page for /proc/[pid]/mountinfo for details, e.g.:
                            //     (1)(2)(3)   (4)   (5)      (6)      (7)   (8) (9)   (10)         (11)
                            //     36 35 98:0 /mnt1 /mnt2 rw,noatime master:1 - ext3 /dev/root rw,errors=continue
                            // but (7) is optional and could exist as multiple fields; the (8) separator marks
                            // the end of the optional values.

                            const string Separator = " - ";
                            int endOfOptionalFields = line.IndexOf(Separator);
                            if (endOfOptionalFields == -1)
                            {
                                // Malformed line.
                                continue;
                            }

                            string postSeparatorLine = line.Substring(endOfOptionalFields + Separator.Length);
                            string[] postSeparatorlineParts = postSeparatorLine.Split(' ');
                            if (postSeparatorlineParts.Length < 3)
                            {
                                // Malformed line.
                                continue;
                            }

                            if (postSeparatorlineParts[0] != "cgroup" ||
                                Array.IndexOf(postSeparatorlineParts[2].Split(','), subsystem) < 0)
                            {
                                // Not the relevant entry.
                                continue;
                            }

                            // Found the relevant entry.  Extract the mount root and path.
                            string[] lineParts = line.Substring(0, endOfOptionalFields).Split(' ');
                            root = lineParts[3];
                            path = lineParts[4];
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Fail($"Failed to read or parse \"{ProcMountInfoFilePath}\": {e}");
                }
            }

            root = null;
            path = null;
            return false;
        }

        /// <summary>Find the cgroup relative path for the specified subsystem.</summary>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <param name="path">The found path, or null if it couldn't be found.</param>
        /// <returns></returns>
        private static bool TryFindCGroupPathForSubsystem(string subsystem, out string path)
        {
            if (File.Exists(ProcCGroupFilePath))
            {
                try
                {
                    using (var reader = new StreamReader(ProcCGroupFilePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Find the first entry that has the subsystem listed in its controller
                            // list. See man page for cgroups for /proc/[pid]/cgroups format, e.g:
                            //     hierarchy-ID:controller-list:cgroup-path
                            //     5:cpuacct,cpu,cpuset:/daemons

                            string[] lineParts = line.Split(':');
                            if (lineParts.Length != 3)
                            {
                                // Malformed line.
                                continue;
                            }

                            if (Array.IndexOf(lineParts[1].Split(','), subsystem) < 0)
                            {
                                // Not the relevant entry.
                                continue;
                            }

                            path = lineParts[2];
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Fail($"Failed to read or parse \"{ProcMountInfoFilePath}\": {e}");
                }
            }

            path = null;
            return false;
        }
    }
}
