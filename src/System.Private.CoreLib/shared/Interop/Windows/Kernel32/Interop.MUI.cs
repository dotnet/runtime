// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint MUI_PREFERRED_UI_LANGUAGES = 0x10;

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern unsafe bool GetFileMUIPath(uint dwFlags, string pcwszFilePath, char* pwszLanguage, ref int pcchLanguage, char* pwszFileMUIPath, ref int pcchFileMUIPath, ref long pululEnumerator);
    }
}
