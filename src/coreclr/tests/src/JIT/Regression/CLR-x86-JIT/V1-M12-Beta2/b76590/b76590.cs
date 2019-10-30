// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//<Stdheader>
//</Stdheader>
//<Expects Status=success>
//<<OUT
//e1==that: True
//that==e1: True
//OUT
//</Expects>


using System;
using System.IO;

public class Bug26518
{
    // Enums
    enum E1
    {
        one = 1,
    }

    public static int Main(String[] args)
    {
        E1 e1 = E1.one;
        Object that = E1.one;
        Console.WriteLine("e1==that: " + e1.Equals(that));
        Console.WriteLine("that==e1: " + that.Equals(e1));
        if (e1.Equals(that) == that.Equals(e1))
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else Console.WriteLine("FAIL");
        return 101;
    }
}
