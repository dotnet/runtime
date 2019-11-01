// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace strswitch
{
    internal class Class1
    {
        private static int Main(string[] args)
        {
            string[] s = { "one", "two", "three", "four", "five", "six" };
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case "one":
                        Console.WriteLine("s == one");
                        break;
                    case "two":
                        Console.WriteLine("s == two");
                        break;
                    case "three":
                        try
                        {
                            Console.WriteLine("s == three");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            goto continueloop; // **** adding this will cause the asserts
                        }
                        break;
                    case "four":
                        Console.WriteLine("s == four");
                        break;
                    case "five":
                        Console.WriteLine("s == five");
                        break;
                    default:
                        Console.WriteLine("Greater than five");
                        break;
                };
                continue;
            continueloop:
                Console.WriteLine("Continuing");
            };
        finish:
            return 100;
        }
    }
}
