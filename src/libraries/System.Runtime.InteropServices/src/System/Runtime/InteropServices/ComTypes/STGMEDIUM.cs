// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.ComTypes
{
    public struct STGMEDIUM
    {
        public TYMED tymed;
        public IntPtr unionmember;
        [MarshalAs(UnmanagedType.IUnknown)]
        public object? pUnkForRelease;
    }
}
