// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace b52733
{
    using System;

    public struct AA
    {
        private static float[] s_af;
        private static bool s_b;

        private static float[] Method1() { return s_af = new float[5]; }

        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            bool b = false;
            if (b)
                b = __refvalue(__makeref(s_b), bool);
            else
                Method1();
        }
    }
}
