// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace HFATest
{
#if NESTED_HFA

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA01
    {
#if FLOAT64
        public double f1;
#else
        public float f1;
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA02
    {
        public HFA01 hfa01;
#if FLOAT64
        public double f2;
#else
        public float f2;
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA03
    {
        public HFA01 hfa01;
        public HFA02 hfa02;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA05
    {
        public HFA02 hfa02;
        public HFA03 hfa03;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA08
    {
        public HFA03 hfa03;
        public HFA05 hfa05;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA11
    {
        public HFA03 hfa03;
        public HFA08 hfa08;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA19
    {
        public HFA08 hfa08;
        public HFA11 hfa11;
    }

#else // NESTED_HFA

#if FLOAT64

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA01
    {
        public double f1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA02
    {
        public double f1, f2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA03
    {
        public double f1, f2, f3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA05
    {
        public double f1, f2, f3, f4, f5;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA08
    {
        public double f1, f2, f3, f4, f5, f6, f7, f8;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA11
    {
        public double f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HFA19
    {
        public double f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15, f16, f17, f18, f19;
    }

#else

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

#endif // FLOAT64

#endif // NESTED_HFA
}
