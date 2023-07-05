// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace MS_vcall_cs
{
    public struct VT
    {
        private int _v;

        private static void Test(VT arg1, ref VT arg2)
        {
            arg2._v = 100;
            arg1._v = 10;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            VT vt;
            vt._v = 99;
            Test(vt, ref vt);
            return vt._v;
        }
    }
}
