// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public HFA01 hfa01;
        public float f2;
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
}
