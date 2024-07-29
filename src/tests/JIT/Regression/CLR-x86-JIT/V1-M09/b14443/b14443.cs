// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    using System;

    public class Bug4
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Decimal cy1;
            Decimal cy2;
            Decimal dm3;
            //

            dm3 = new Decimal(-2.34);

            cy1 = new Decimal(2.34);
            cy2 = -dm3;

            Console.WriteLine("cy1 ,cy2 ,dm3: " + cy1 + " ," + cy2 + " ," + dm3);

            if (cy1 != cy2)
            {
                Console.WriteLine("FAIL! (72c)");
                return 1;
            }
            else
            {
                Console.WriteLine("Good. (73g)");
                return 100;
            }
        }
    }
}
