// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Http
    {
        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_GetCurlVersionInfo")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCurlVersionInfo(
            out int age,
            [MarshalAs(UnmanagedType.Bool)] out bool supportsSsl,
            [MarshalAs(UnmanagedType.Bool)] out bool supportsAutoDecompression,
            [MarshalAs(UnmanagedType.Bool)] out bool supportsHttp2Multiplexing);
    }
}
