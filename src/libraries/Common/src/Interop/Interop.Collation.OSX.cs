// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_CompareStringNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int CompareStringNative(string localeName, int lNameLen, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);
    }
}
