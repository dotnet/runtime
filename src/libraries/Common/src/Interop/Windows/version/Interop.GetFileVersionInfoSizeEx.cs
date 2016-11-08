// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Version
    {
        [DllImport(Libraries.Version, CharSet = CharSet.Unicode, EntryPoint = "GetFileVersionInfoSizeExW")]
        internal extern static uint GetFileVersionInfoSizeEx(uint dwFlags, string lpwstrFilename, out uint lpdwHandle);
    }
}
