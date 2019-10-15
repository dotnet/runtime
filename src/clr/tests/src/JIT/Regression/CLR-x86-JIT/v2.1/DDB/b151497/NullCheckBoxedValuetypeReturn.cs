// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// JIT AVs and subsequently throws NullReferenceException when comparing a boxed valuetype return with null.

//  csc /o+ NullCheckBoxedValuetypeReturn.cs
// Bug output: 
//     A NullReferenceException.
// Correct Expected output: 
//     It should print out "Pass".

using System;

struct MyStruct
{
    int i;
    int j;
}

class MainApp
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static MyStruct Foo()
    {
        return new MyStruct();
    }

    public static int Main()
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
