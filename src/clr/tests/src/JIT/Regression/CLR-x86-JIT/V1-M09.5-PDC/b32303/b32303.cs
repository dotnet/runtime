// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    public class Temp
    {
        public static int Main(String[] args)
        {
            int x = 10;
            switch (x)
            {
                case 10:
                    Console.WriteLine("10");
                    break;
            }
            return 100;
        }
    }

}
