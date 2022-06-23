// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.IO
{
    /// <summary>Contains internal volume helpers that are shared between many projects.</summary>
    internal static partial class DriveInfoInternal
    {
        public static string[] GetLogicalDrives()
        {
            int drives = Interop.Kernel32.GetLogicalDrives();
            if (drives == 0)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            // GetLogicalDrives returns a bitmask starting from
            // position 0 "A" indicating whether a drive is present.
            // Create a string for each bit that is set.

            int count = BitOperations.PopCount((uint)drives);

            string[] result = new string[count];
            Span<char> root = stackalloc char[] { 'A', ':', '\\' };
            uint d = (uint)drives;
            count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0)
                {
                    result[count++] = root.ToString();
                }
                d >>= 1;
                root[0]++;
            }
            return result;
        }

        public static string NormalizeDriveName(string driveName)
        {
            Debug.Assert(driveName != null);

            string? name;

            if (driveName.Length == 1)
            {
                name = driveName + ":\\";
            }
            else
            {
                name = Path.GetPathRoot(driveName);
                // Disallow null or empty drive letters and UNC paths
                if (string.IsNullOrEmpty(name) || name.StartsWith("\\\\", StringComparison.Ordinal))
                {
                    throw new ArgumentException(SR.Arg_MustBeDriveLetterOrRootDir, nameof(driveName));
                }
            }
            // We want to normalize to have a trailing backslash so we don't have two equivalent forms and
            // because some Win32 API don't work without it.
            if (name.Length == 2 && name[1] == ':')
            {
                name += "\\";
            }

            // Now verify that the drive letter could be a real drive name.
            // On Windows this means it's between A and Z, ignoring case.
            if (!char.IsAsciiLetter(driveName[0]))
            {
                throw new ArgumentException(SR.Arg_MustBeDriveLetterOrRootDir, nameof(driveName));
            }

            return name;
        }
    }
}
