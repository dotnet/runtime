// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
