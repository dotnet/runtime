// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
