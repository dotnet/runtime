// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace defaultNamespace
{
    using System;

    public class jitbug
    {
        [Fact]
        public static int TestEntryPoint()
        {
            if ("15.0%Double.PositiveInfinity = " + 15.0 % Double.PositiveInfinity == "15.0%Double.PositiveInfinity = 15")
            {
                Console.WriteLine("*** PASSED ***");
                return 100;
            }
            Console.WriteLine("15.0%Double.PositiveInfinity = " + 15.0 % Double.PositiveInfinity);
            Console.WriteLine("*** FAILED ***");
            return 1;
        }
    }

}
