//------------------------------------------------------------------------------
// <copyright file="AdsValue2.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {
    using System;    
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ads_Pointer {
        public IntPtr value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ads_OctetString {
        public int length;
        public IntPtr value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ads_Generic {
        public int a;
        public int b;
        public int c;
        public int d;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct AdsValue {
        [FieldOffset(0)] public int /*AdsType*/ dwType;
        [FieldOffset(4)] internal int pad;
        [FieldOffset(8)] public Ads_Pointer pointer;
        [FieldOffset(8)] public Ads_OctetString octetString;
        [FieldOffset(8)] public Ads_Generic generic;
    }

}

