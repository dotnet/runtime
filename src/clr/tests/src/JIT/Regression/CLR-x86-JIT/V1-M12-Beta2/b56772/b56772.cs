// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

public class InternTest
{
    public static int Main(String[] args)
    {
        StringBuilder sb = new StringBuilder().Append('A').Append('B').Append('C');

        switch (sb.ToString())
        {
            case "ABC":
                Console.WriteLine("Worked Correctly");
                return 100;
            default:
                Console.WriteLine("FAILED!");
                return 1;
        }
    }
}