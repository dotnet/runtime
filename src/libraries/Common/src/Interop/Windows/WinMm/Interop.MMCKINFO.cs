// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        [StructLayout(LayoutKind.Sequential)]
        internal class MMCKINFO
        {
            internal int ckID;
            internal int cksize;
            internal int fccType;
            internal int dwDataOffset;
            internal int dwFlags;
        }
    }
}
