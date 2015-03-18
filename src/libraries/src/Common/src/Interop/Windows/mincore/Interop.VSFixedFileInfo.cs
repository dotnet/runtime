// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct VS_FIXEDFILEINFO
        {
            internal uint dwSignature;
            internal uint dwStrucVersion;
            internal uint dwFileVersionMS;
            internal uint dwFileVersionLS;
            internal uint dwProductVersionMS;
            internal uint dwProductVersionLS;
            internal uint dwFileFlagsMask;
            internal uint dwFileFlags;
            internal uint dwFileOS;
            internal uint dwFileType;
            internal uint dwFileSubtype;
            internal uint dwFileDateMS;
            internal uint dwFileDateLS;
        }
    }
}
