// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        public static unsafe DriveInfo[] GetDrives()
        {
            const int StringBufferLength = 8192; // there's no defined max size nor an indication through the API when you supply too small a buffer; choosing something that seems reasonable
            byte* strBuf = stackalloc byte[StringBufferLength];

            // Parse the mounts file
            const string MountsPath = "/proc/mounts"; // Linux mounts file
            IntPtr fp;
            Interop.CheckIoPtr(fp = Interop.libc.setmntent(MountsPath, Interop.libc.MNTOPT_RO), path: MountsPath);
            try
            {
                // Walk the entries in the mounts file, creating a DriveInfo for each that shouldn't be ignored.
                List<DriveInfo> drives = new List<DriveInfo>();
                Interop.libc.mntent mntent = default(Interop.libc.mntent);
                while (Interop.libc.getmntent_r(fp, ref mntent, strBuf, StringBufferLength) != IntPtr.Zero)
                {
                    string type = DecodeString(mntent.mnt_type);
                    if (!string.IsNullOrWhiteSpace(type) && type != Interop.libc.MNTTYPE_IGNORE)
                    {
                        string name = DecodeString(mntent.mnt_dir);
                        drives.Add(new DriveInfo(name));
                    }
                }
                return drives.ToArray();
            }
            finally
            {
                int result = Interop.libc.endmntent(fp);
                Debug.Assert(result == 1); // documented to always return 1
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Gets the string starting at the specifying pointer and going until null termination.</summary>
        /// <param name="str">Pointer to the first byte in the string.</param>
        /// <returns>The decoded string.</returns>
        private static unsafe string DecodeString(byte* str)
        {
            return str != null ? Marshal.PtrToStringAnsi((IntPtr)str) : null;
        }
    }
}
