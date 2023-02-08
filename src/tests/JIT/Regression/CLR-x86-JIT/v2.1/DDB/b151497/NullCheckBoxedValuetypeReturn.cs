// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// JIT AVs and subsequently throws NullReferenceException when comparing a boxed valuetype return with null.

//  csc /o+ NullCheckBoxedValuetypeReturn.cs
// Bug output: 
//     A NullReferenceException.
// Correct Expected output: 
//     It should print out "Pass".

using System;
using Xunit;

struct MyStruct
{
    int i;
    int j;
}

public class MainApp
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static MyStruct Foo()
    {
        return new MyStruct();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if ((object)MainApp.Foo() == null)
        {
            Console.WriteLine("Fail");
            return 101;
        }
        else
        {
            Console.WriteLine("Pass");
            return 100;
        }
    }

}
