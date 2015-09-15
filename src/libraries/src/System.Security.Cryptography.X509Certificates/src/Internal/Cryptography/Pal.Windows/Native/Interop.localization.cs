// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Cryptography.Pal.Native;

internal static partial class Interop
{
    public static class localization
    {
        [DllImport(Libraries.Localization, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "FormatMessageW")]
        public static extern int FormatMessage(FormatMessageFlags dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, [Out] StringBuilder lpBuffer, int nSize, IntPtr Arguments);
    }
}


