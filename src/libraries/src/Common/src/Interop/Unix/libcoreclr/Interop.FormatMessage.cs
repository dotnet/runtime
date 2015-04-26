// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcoreclr
    {
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        [DllImport(Interop.Libraries.LibCoreClr)]
        private extern static uint FormatMessage(
                    uint dwFlags,
                    IntPtr lpSource,
                    uint dwMessageId,
                    uint dwLanguageId,
                    char[] lpBuffer,
                    uint nSize,
                    IntPtr Arguments);

        // Gets an error message for a Win32 error code.
        internal static String GetMessage(int errorCode)
        {
            char[] buffer = new char[512];
            uint result = Interop.libcoreclr.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                IntPtr.Zero, (uint)errorCode, 0, buffer, (uint)buffer.Length, IntPtr.Zero);
            if (result != 0)
            {
                // result is the # of characters copied to the StringBuilder.
                return new string(buffer, 0, (int)result);
            }
            else
            {
                return SR.Format(SR.UnknownError_Num, errorCode);
            }
        }
    }
}
