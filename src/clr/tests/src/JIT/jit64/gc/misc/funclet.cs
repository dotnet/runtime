// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// The main purpose of this test is make sure that an object ref passed on the stack
// out of a funclet works properly. The varargs is a bit extraneous.

class test
{
    public static int Main()
    {
        try
        {
        }
        finally
        {
            System.Console.WriteLine("just before call");

            VarArgFunction(__arglist(
                     0,
                     0,
                     0,
                     0,
                     0,
                     0,
                     0,
                     0,
                     0,
                     "string"    // <-- ensure this is passed on the stack even for IA64
                     ));

            System.Console.WriteLine("just after call");
        }
        return 100;
    }

    public static void VarArgFunction(__arglist)
    {
        System.Console.WriteLine("inside call");
    }
}
