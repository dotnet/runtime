// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes
{
    [EditorBrowsable(EditorBrowsableState.Never)]
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
