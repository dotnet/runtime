// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityPackageInfo
    {
        // see SecPkgInfoW in <sspi.h>
        internal int Capabilities;
        internal short Version;
        internal short RPCID;
        internal int MaxToken;
        internal IntPtr Name;
        internal IntPtr Comment;

        internal static readonly int Size = Marshal.SizeOf<SecurityPackageInfo>();
        internal static readonly int NameOffest = (int)Marshal.OffsetOf<SecurityPackageInfo>("Name");
    }
}
