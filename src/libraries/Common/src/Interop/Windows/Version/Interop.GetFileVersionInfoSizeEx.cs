// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Version
    {
        [LibraryImport(Libraries.Version, EntryPoint = "GetFileVersionInfoSizeExW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint GetFileVersionInfoSizeEx(uint dwFlags, string lpwstrFilename, out uint lpdwHandle);
    }
}
