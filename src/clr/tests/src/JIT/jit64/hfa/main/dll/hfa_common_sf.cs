// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace HFATest
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HFA01
    {
        public float f1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA02
    {
        public float f1, f2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA03
    {
        public float f1, f2, f3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA05
    {
        public float f1, f2, f3, f4, f5;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA08
    {
        public float f1, f2, f3, f4, f5, f6, f7, f8;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA11
    {
        public float f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA19
    {
        public float f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15, f16, f17, f18, f19;
    }
}
