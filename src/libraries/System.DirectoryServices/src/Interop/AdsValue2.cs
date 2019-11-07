// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Ads_Pointer
    {
        public IntPtr value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ads_OctetString
    {
        public int length;
        public IntPtr value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ads_Generic
    {
        public int a;
        public int b;
        public int c;
        public int d;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct AdsValue
    {
        [FieldOffset(0)]
        public int /*AdsType*/ dwType;
        [FieldOffset(4)]
        internal int pad;
        [FieldOffset(8)]
        public Ads_Pointer pointer;
        [FieldOffset(8)]
        public Ads_OctetString octetString;
        [FieldOffset(8)]
        public Ads_Generic generic;
    }
}
