// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//Disable tailcall if the caller is marked no-inline.
//Test expects Foo() to catch the exception thrown by Bar(). 
using System;
using System.Runtime.CompilerServices;
using Xunit;

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

    [Fact]
    public static int TestEntryPoint()
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
