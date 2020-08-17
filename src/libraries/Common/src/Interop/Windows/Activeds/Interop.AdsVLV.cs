// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Activeds
    {
        [StructLayout(LayoutKind.Sequential)]
        internal class AdsVLV
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
}
