// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Version
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
