// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.DirectoryServices
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class AdsVLV
    {
        public int beforeCount;
        public int afterCount;
        public int offset;
        public int contentCount;
        public IntPtr target;
        public int contextIDlength;
        public IntPtr contextID;
    }
}
