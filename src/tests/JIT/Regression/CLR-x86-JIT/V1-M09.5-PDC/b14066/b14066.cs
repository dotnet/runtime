// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace
{
    public class Prob
    {
        public static int Main(System.String[] Args)
        {
            System.Console.WriteLine(System.Math.Exp(System.Double.PositiveInfinity));
            System.Console.WriteLine(System.Math.Exp(System.Double.NegativeInfinity));

            return 100;
        }
    }
}
