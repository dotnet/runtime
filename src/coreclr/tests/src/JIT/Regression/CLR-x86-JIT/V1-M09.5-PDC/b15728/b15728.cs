// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace defaultNamespace
{
    using System;

    public class jitbug
    {
        public static int Main(String[] args)
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
