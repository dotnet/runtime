// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
