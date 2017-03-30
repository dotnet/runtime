// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal static int CopyFile(String src, String dst, bool failIfExists)
        {
            uint copyFlags = failIfExists ? (uint)Interop.Kernel32.FileOperations.COPY_FILE_FAIL_IF_EXISTS : 0;
            Interop.Kernel32.COPYFILE2_EXTENDED_PARAMETERS parameters = new Interop.Kernel32.COPYFILE2_EXTENDED_PARAMETERS()
            {
                dwSize = (uint)Marshal.SizeOf<Interop.Kernel32.COPYFILE2_EXTENDED_PARAMETERS>(),
                dwCopyFlags = copyFlags
            };

            int hr = Interop.Kernel32.CopyFile2(src, dst, ref parameters);

            return Win32Marshal.TryMakeWin32ErrorCodeFromHR(hr);
        }
    }
}
