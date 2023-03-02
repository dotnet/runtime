// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
//

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "NativeGetLocaleName", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial string NativeGetLocaleName(string localeName, int valueLength);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "NativeGetLocaleInfoString", StringMarshalling = StringMarshalling.Utf8)]
        internal static unsafe partial string NativeGetLocaleInfoString(string localeName, uint localeStringData, int valueLength, string? uiLocaleName = null);
    }
}
