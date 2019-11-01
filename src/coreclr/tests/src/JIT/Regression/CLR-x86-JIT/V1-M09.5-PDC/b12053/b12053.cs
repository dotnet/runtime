// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

public class foo
{
    public static int Main()
    {
        int ian = -2147483648;
        System.Console.Write((long)0x80000000);
        System.Console.Write(" != ");
        System.Console.WriteLine((long)ian);
        if (ian == 0x80000000)
        {
            System.Console.WriteLine("Test failed!");
            return 1;
        }
        System.Console.WriteLine("Test passed.");
        return 100;
    }
}
