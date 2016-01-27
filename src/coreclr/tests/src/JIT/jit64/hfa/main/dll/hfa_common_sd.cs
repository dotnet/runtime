// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace HFATest
{
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
}
