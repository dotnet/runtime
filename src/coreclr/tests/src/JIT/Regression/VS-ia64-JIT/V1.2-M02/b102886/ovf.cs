// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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