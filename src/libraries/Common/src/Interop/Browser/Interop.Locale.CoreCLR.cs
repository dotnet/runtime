// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [LibraryImport(Libraries.SystemBrowserNative, EntryPoint = "SystemJS_GetLocaleInfo")]
        public static unsafe partial nint GetLocaleInfo(char* locale, int localeLength, char* culture, int cultureLength, char* buffer, int bufferLength, out int resultLength);
    }
}
