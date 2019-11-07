// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class Base { };
internal class Sibling1 : Base { };
internal class Sibling2 : Base { };

internal static class Repro
{
    private static int Bug(object o)
    {
        Base b = o as Sibling1;
        if (b == null)
        {
            b = o as Sibling2;
        }

        // At this point b is either null, Sibling1, or Sibling2
        if (b != null)
        {
            // But the bug makes us think it is only Sibling1 here (since we've eliminated null)
            if (b is Sibling2)
            {
                Console.WriteLine("Pass");
                return 100;
            }
            else
            {
                Console.WriteLine("b is {0}", b.GetType().ToString());
                Console.WriteLine("b is Sibling1 = {0}", b is Sibling1);
                Console.WriteLine("b is Sibling2 = {0}", b is Sibling2);
                Console.WriteLine("Fail");
                return 9;
            }
        }
        Console.WriteLine("bad");
        return 0;
    }

    private static int Main()
    {
        return Bug(new Sibling2());
    }
}
