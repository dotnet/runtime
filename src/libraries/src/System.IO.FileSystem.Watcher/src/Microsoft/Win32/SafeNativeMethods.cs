// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Win32
{
    internal static class SafeNativeMethods
    {
        public const int
            FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_STRING = 0x00000400,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        [SecurityCritical]
        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, BestFitMapping = true)]
        public static unsafe extern int FormatMessage(int dwFlags, IntPtr lpSource_mustBeNull, uint dwMessageId,
            int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr[] arguments);
    }
}
