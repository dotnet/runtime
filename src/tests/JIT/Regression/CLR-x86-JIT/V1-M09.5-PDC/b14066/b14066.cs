// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace b14066
{
    public class Prob
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            System.Console.WriteLine(System.Math.Exp(System.Double.PositiveInfinity));
            System.Console.WriteLine(System.Math.Exp(System.Double.NegativeInfinity));
        }
    }
}
