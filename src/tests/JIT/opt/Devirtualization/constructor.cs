// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_constructor 
{
    static string s;

    public override string ToString() 
    {
        return "Test";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Test_constructor() 
    {
        s = ToString();   // cannot be devirtualized
    }
    
    [Fact]
    public static int TestEntryPoint() 
    {
        new Child();
        return (s == "Child" ? 100 : 0);
    }
}

class Child : Test_constructor 
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Child() { }

    public override string ToString() 
    {
        return "Child";
    }
}
