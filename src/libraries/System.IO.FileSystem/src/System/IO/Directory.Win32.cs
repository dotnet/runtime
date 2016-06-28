// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public static partial class Directory
    {
        public static string[] GetLogicalDrives()
        {
            int drives = Interop.mincore.GetLogicalDrives();
            if (drives == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();

            uint d = (uint)drives;
            int count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0) count++;
                d >>= 1;
            }
            string[] result = new string[count];
            char[] root = new char[] { 'A', ':', '\\' };
            d = (uint)drives;
            count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0)
                {
                    result[count++] = new string(root);
                }
                d >>= 1;
                root[0]++;
            }
            return result;
        }
    }
}
