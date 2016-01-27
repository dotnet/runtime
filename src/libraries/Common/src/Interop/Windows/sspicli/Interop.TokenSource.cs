// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_SOURCE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = TOKEN_SOURCE_LENGTH)]
            internal byte[] SourceName;
            internal LUID SourceIdentifier;

            internal const int TOKEN_SOURCE_LENGTH = 8;
        }
    }
}
