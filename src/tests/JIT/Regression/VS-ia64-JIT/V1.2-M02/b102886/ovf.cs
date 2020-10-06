// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

class ovf
{

    public static void f()
    {

        uint x = 0xfffffffe;
        uint y = 0xfffffffe;

        checked
        {

            uint z = x * y;
        }

    }

    public static int Main()
    {
        try { f(); }
        catch (System.OverflowException) { return 100; }
        return 1;
    }

}
