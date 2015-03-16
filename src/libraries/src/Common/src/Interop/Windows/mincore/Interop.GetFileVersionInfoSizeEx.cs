// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Version, CharSet = CharSet.Unicode, EntryPoint = "GetFileVersionInfoSizeExW")]
        internal extern static uint GetFileVersionInfoSizeEx(uint dwFlags, string lpwstrFilename, out uint lpdwHandle);
    }
}
