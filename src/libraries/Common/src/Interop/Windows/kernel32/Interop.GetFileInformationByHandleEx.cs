// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa364953.aspx
        [DllImport(Libraries.Kernel32, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public unsafe static extern bool GetFileInformationByHandleEx(
            IntPtr hFile,
            FILE_INFO_BY_HANDLE_CLASS FileInformationClass,
            byte[] lpFileInformation,
            uint dwBufferSize);
    }
}
