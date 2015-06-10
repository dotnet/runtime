// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//Disable tailcall if the caller is marked no-inline.
//Test expects Foo() to catch the exception thrown by Bar(). 
using System;
using System.Runtime.CompilerServices;

public class My
{

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void bar()
    {
        throw new Exception();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int foo()
    {
        try
        {
            bar();
            return 201;
        }
        catch (System.Exception)
        {
            return 100;
        }
    }

    static int Main()
    {
        try
        {
            return foo();

        }
        catch (System.Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }

    }

}
