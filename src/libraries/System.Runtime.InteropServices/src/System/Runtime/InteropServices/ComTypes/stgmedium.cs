// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices.ComTypes
{
    public struct STGMEDIUM
    {
        public TYMED tymed;
        public IntPtr unionmember;
        [MarshalAs(UnmanagedType.IUnknown)]
        public object pUnkForRelease;
    }
}
