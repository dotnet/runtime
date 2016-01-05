// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace strswitch
{
    internal class Class1
    {
        [STAThread]
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
