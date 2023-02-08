// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace DefaultNamespace
{
    public class Prob
    {
        [Fact]
        public static int TestEntryPoint()
        {
            System.Console.WriteLine(System.Math.Exp(System.Double.PositiveInfinity));
            System.Console.WriteLine(System.Math.Exp(System.Double.NegativeInfinity));

            return 100;
        }
    }
}
