// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

internal static partial class Interop
{
    /// <summary>Provides access to some cgroup (v1 and v2) features</summary>
    internal static partial class cgroups
    {
        // For cgroup v1, see https://www.kernel.org/doc/Documentation/cgroup-v1/
        // For cgroup v2, see https://www.kernel.org/doc/Documentation/cgroup-v2.txt
        // For disambiguation, see https://systemd.io/CGROUP_DELEGATION/#three-different-tree-setups-

        /// <summary>The supported versions of cgroup.</summary>
        internal enum CGroupVersion { None, CGroup1, CGroup2 };

        /// <summary>Path to cgroup filesystem that tells us which version of cgroup is in use.</summary>
        private const string SysFsCgroupFileSystemPath = "/sys/fs/cgroup";
        /// <summary>Path to mountinfo file in procfs for the current process.</summary>
        private const string ProcMountInfoFilePath = "/proc/self/mountinfo";
        /// <summary>Path to cgroup directory in procfs for the current process.</summary>
        private const string ProcCGroupFilePath = "/proc/self/cgroup";

        /// <summary>The version of cgroup that's being used. Mutated by tests only.</summary>
        internal static readonly CGroupVersion s_cgroupVersion = FindCGroupVersion();

        /// <summary>Path to the found cgroup memory limit path, or null if it couldn't be found.</summary>
        internal static readonly string? s_cgroupMemoryLimitPath = FindCGroupMemoryLimitPath(s_cgroupVersion);

        /// <summary>Tries to read the memory limit from the cgroup memory location.</summary>
        /// <param name="limit">The read limit, or 0 if it couldn't be read.</param>
        /// <returns>true if the limit was read successfully; otherwise, false.</returns>
        public static bool TryGetMemoryLimit(out ulong limit)
        {
            string? path = s_cgroupMemoryLimitPath;

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
        internal static bool TryReadMemoryValueFromFile(string path, out ulong result)
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

                    // 'max' is also a possible valid value
                    //
                    // Treat this as 'no memory limit' and let the caller
                    // fallback to reading the real limit via other means
                }
                catch (Exception e)
                {
                    Debug.Fail($"Failed to read \"{path}\": {e}");
                }
            }

            result = 0;
            return false;
        }

        /// <summary>Find the cgroup version in use on the system.</summary>
        /// <returns>The cgroup version.</returns>
        private static CGroupVersion FindCGroupVersion()
        {
            try
            {
                return new DriveInfo(SysFsCgroupFileSystemPath).DriveFormat switch
                {
                    "cgroup2fs" => CGroupVersion.CGroup2,
                    "tmpfs" => CGroupVersion.CGroup1,
                    _ => CGroupVersion.None,
                };
            }
            catch (Exception ex) when (ex is DriveNotFoundException || ex is ArgumentException)
            {
                return CGroupVersion.None;
            }
        }

        /// <summary>Find the cgroup memory limit path.</summary>
        /// <param name="cgroupVersion">The cgroup version currently in use on the system.</param>
        /// <returns>The limit path if found; otherwise, null.</returns>
        private static string? FindCGroupMemoryLimitPath(CGroupVersion cgroupVersion)
        {
            string? cgroupMemoryPath = FindCGroupPath(cgroupVersion, "memory");
            if (cgroupMemoryPath != null)
            {
                if (cgroupVersion == CGroupVersion.CGroup1)
                {
                    return cgroupMemoryPath + "/memory.limit_in_bytes";
                }

                if (cgroupVersion == CGroupVersion.CGroup2)
                {
                    // 'memory.high' is a soft limit; the process may get throttled
                    // 'memory.max' is where OOM killer kicks in
                    return cgroupMemoryPath + "/memory.max";
                }
            }

            return null;
        }

        /// <summary>Find the cgroup path for the specified subsystem.</summary>
        /// <param name="cgroupVersion">The cgroup version currently in use on the system.</param>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <returns>The cgroup path if found; otherwise, null.</returns>
        private static string? FindCGroupPath(CGroupVersion cgroupVersion, string subsystem)
        {
            if (cgroupVersion == CGroupVersion.None)
            {
                return null;
            }

            if (TryFindHierarchyMount(cgroupVersion, subsystem, out string? hierarchyRoot, out string? hierarchyMount) &&
                TryFindCGroupPathForSubsystem(cgroupVersion, subsystem, out string? cgroupPathRelativeToMount))
            {
                return FindCGroupPath(hierarchyRoot, hierarchyMount, cgroupPathRelativeToMount);
            }

            return null;
        }

        internal static string FindCGroupPath(string hierarchyRoot, string hierarchyMount, string cgroupPathRelativeToMount)
        {
            // For a host cgroup, we need to append the relative path.
            // The root and cgroup path can share a common prefix of the path that should not be appended.
            // Example 1 (docker):
            // hierarchyMount:               /sys/fs/cgroup/cpu
            // hierarchyRoot:                /docker/87ee2de57e51bc75175a4d2e81b71d162811b179d549d6601ed70b58cad83578
            // cgroupPathRelativeToMount:    /docker/87ee2de57e51bc75175a4d2e81b71d162811b179d549d6601ed70b58cad83578/my_named_cgroup
            // append to the cgroupPath:     /my_named_cgroup
            // final cgroupPath:             /sys/fs/cgroup/cpu/my_named_cgroup
            //
            // Example 2 (out of docker)
            // hierarchyMount:               /sys/fs/cgroup/cpu
            // hierarchyRoot:                /
            // cgroupPathRelativeToMount:    /my_named_cgroup
            // append to the cgroupPath:     /my_named_cgroup
            // final cgroupPath:             /sys/fs/cgroup/cpu/my_named_cgroup

            int commonPathPrefixLength = hierarchyRoot.Length;
            if ((commonPathPrefixLength == 1) || !cgroupPathRelativeToMount.StartsWith(hierarchyRoot, StringComparison.Ordinal))
            {
                commonPathPrefixLength = 0;
            }

            return string.Concat(hierarchyMount, cgroupPathRelativeToMount.AsSpan(commonPathPrefixLength));
        }

        /// <summary>Find the cgroup mount information for the specified subsystem.</summary>
        /// <param name="cgroupVersion">The cgroup version currently in use on the system.</param>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <param name="root">The path of the directory in the filesystem which forms the root of this mount; null if not found.</param>
        /// <param name="path">The path of the mount point relative to the process's root directory; null if not found.</param>
        /// <returns>true if the mount was found; otherwise, null.</returns>
        private static bool TryFindHierarchyMount(CGroupVersion cgroupVersion, string subsystem, [NotNullWhen(true)] out string? root, [NotNullWhen(true)] out string? path)
        {
            return TryFindHierarchyMount(cgroupVersion, ProcMountInfoFilePath, subsystem, out root, out path);
        }

        /// <summary>Find the cgroup mount information for the specified subsystem.</summary>
        /// <param name="cgroupVersion">The cgroup version currently in use on the system.</param>
        /// <param name="mountInfoFilePath">The path to the /mountinfo file. Useful for tests.</param>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <param name="root">The path of the directory in the filesystem which forms the root of this mount; null if not found.</param>
        /// <param name="path">The path of the mount point relative to the process's root directory; null if not found.</param>
        /// <returns>true if the mount was found; otherwise, null.</returns>
        internal static bool TryFindHierarchyMount(CGroupVersion cgroupVersion, string mountInfoFilePath, string subsystem, [NotNullWhen(true)] out string? root, [NotNullWhen(true)] out string? path)
        {
            if (File.Exists(mountInfoFilePath))
            {
                try
                {
                    using (var reader = new StreamReader(mountInfoFilePath))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Look for an entry that has cgroup as the "filesystem type"
                            // and, for cgroup1, that has options containing the specified subsystem
                            // See man page for /proc/[pid]/mountinfo for details, e.g.:
                            //     (1)(2)(3)   (4)   (5)      (6)      (7)   (8) (9)   (10)         (11)
                            //     36 35 98:0 /mnt1 /mnt2 rw,noatime master:1 - ext3 /dev/root rw,errors=continue
                            // but (7) is optional and could exist as multiple fields; the (8) separator marks
                            // the end of the optional values.

                            const string Separator = " - ";
                            int endOfOptionalFields = line.IndexOf(Separator, StringComparison.Ordinal);
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

                            if (cgroupVersion == CGroupVersion.CGroup1)
                            {
                                bool validCGroup1Entry = ((postSeparatorlineParts[0] == "cgroup") &&
                                        (Array.IndexOf(postSeparatorlineParts[2].Split(','), subsystem) >= 0));
                                if (!validCGroup1Entry)
                                {
                                    continue;
                                }
                            }
                            else if (cgroupVersion == CGroupVersion.CGroup2)
                            {
                                bool validCGroup2Entry = postSeparatorlineParts[0] == "cgroup2";
                                if (!validCGroup2Entry)
                                {
                                    continue;
                                }

                            }
                            else
                            {
                                Debug.Fail($"Unexpected cgroup version \"{cgroupVersion}\"");
                            }


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
        /// <param name="cgroupVersion">The cgroup version currently in use on the system.</param>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <param name="path">The found path, or null if it couldn't be found.</param>
        /// <returns>true if a cgroup path for the subsystem is found.</returns>
        private static bool TryFindCGroupPathForSubsystem(CGroupVersion cgroupVersion, string subsystem, [NotNullWhen(true)] out string? path)
        {
            return TryFindCGroupPathForSubsystem(cgroupVersion, ProcCGroupFilePath, subsystem, out path);
        }

        /// <summary>Find the cgroup relative path for the specified subsystem.</summary>
        /// <param name="cgroupVersion">The cgroup version currently in use on the system.</param>
        /// <param name="procCGroupFilePath">Path to cgroup directory in procfs for the current process.</param>
        /// <param name="subsystem">The subsystem, e.g. "memory".</param>
        /// <param name="path">The found path, or null if it couldn't be found.</param>
        /// <returns>true if a cgroup path for the subsystem is found.</returns>
        internal static bool TryFindCGroupPathForSubsystem(CGroupVersion cgroupVersion, string procCGroupFilePath, string subsystem, [NotNullWhen(true)] out string? path)
        {
            if (File.Exists(procCGroupFilePath))
            {
                try
                {
                    using (var reader = new StreamReader(procCGroupFilePath))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] lineParts = line.Split(':');

                            if (lineParts.Length != 3)
                            {
                                // Malformed line.
                                continue;
                            }

                            if (cgroupVersion == CGroupVersion.CGroup1)
                            {
                                // cgroup v1: Find the first entry that has the subsystem listed in its controller
                                // list. See man page for cgroups for /proc/[pid]/cgroups format, e.g:
                                //     hierarchy-ID:controller-list:cgroup-path
                                //     5:cpuacct,cpu,cpuset:/daemons
                                if (Array.IndexOf(lineParts[1].Split(','), subsystem) < 0)
                                {
                                    // Not the relevant entry.
                                    continue;
                                }

                                path = lineParts[2];
                                return true;
                            }
                            else if (cgroupVersion == CGroupVersion.CGroup2)
                            {
                                // cgroup v2: Find the first entry that matches the cgroup v2 hierarchy:
                                //     0::$PATH

                                if ((lineParts[0] == "0") && (lineParts[1].Length == 0))
                                {
                                    path = lineParts[2];
                                    return true;
                                }
                            }
                            else
                            {
                                Debug.Fail($"Unexpected cgroup version: \"{cgroupVersion}\"");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Fail($"Failed to read or parse \"{procCGroupFilePath}\": {e}");
                }
            }

            path = null;
            return false;
        }
    }
}
