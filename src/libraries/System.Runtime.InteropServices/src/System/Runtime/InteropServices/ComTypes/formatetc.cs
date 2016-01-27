// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    public struct FORMATETC
    {
        [MarshalAs(UnmanagedType.U2)]
        public short cfFormat;
        public IntPtr ptd;
        [MarshalAs(UnmanagedType.U4)]
        public DVASPECT dwAspect;
        public int lindex;
        [MarshalAs(UnmanagedType.U4)]
        public TYMED tymed;
    }
}
