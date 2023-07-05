// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class BB
    {
        public double[] Method3(double param3)
        {
            return new double[7];
        }
        public static uint[] Method2(uint param1, BB param3)
        {
            double d = 0.0d;
            uint u = (uint)(param3.Method3(param3.Method3(d)[0])[0]);
            return new uint[4];
        }
        [Fact]
        public static int TestEntryPoint()
        {
            BB a = new BB();
            Method2(Method2(0u, a)[2], a);
            return 100;
        }
    }
}
