// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Security;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        public static unsafe DriveInfo[] GetDrives()
        {
            DriveInfo[] drives = null;
            Interop.libc.statfs* pBuffer = null;
            int count = Interop.libc.getmntinfo(&pBuffer, 0);
            if (count > 0)
            {
                drives = new DriveInfo[count];
                for (int i = 0; i < count; i++)
                {
                    String mountPoint = Marshal.PtrToStringAnsi((IntPtr)pBuffer[i].f_mntonname);
                    drives[i] = new DriveInfo(mountPoint);
                }
            }

            return drives;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}
