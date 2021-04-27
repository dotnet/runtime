// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        public const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        public const int FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
        public const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = true)]
        public static extern int FormatMessage(
            int dwFlags,
            SafeLibraryHandle lpSource,
            uint dwMessageId,
            int dwLanguageId,
            [Out] char[] lpBuffer,
            int nSize,
            IntPtr[] arguments);
    }
}
