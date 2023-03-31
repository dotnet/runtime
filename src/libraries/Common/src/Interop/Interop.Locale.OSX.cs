// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleNameNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static unsafe partial string GetLocaleNameNative(string localeName);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoStringNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static unsafe partial string GetLocaleInfoStringNative(string localeName, uint localeStringData);
    }
}
