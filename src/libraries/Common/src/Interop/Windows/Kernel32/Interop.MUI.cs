// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint MUI_USER_PREFERRED_UI_LANGUAGES = 0x10;
        internal const uint MUI_USE_INSTALLED_LANGUAGES = 0x20;

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern unsafe bool GetFileMUIPath(uint dwFlags, string pcwszFilePath, char* pwszLanguage, ref uint pcchLanguage, char* pwszFileMUIPath, ref uint pcchFileMUIPath, ref ulong pululEnumerator);
    }
}
