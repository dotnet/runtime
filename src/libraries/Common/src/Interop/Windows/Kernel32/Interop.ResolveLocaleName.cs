// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Kernel32
    {
        internal const int LOCALE_NAME_MAX_LENGTH = 85;

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int ResolveLocaleName(string lpNameToResolve, char* lpLocaleName, int cchLocaleName);
    }
}
