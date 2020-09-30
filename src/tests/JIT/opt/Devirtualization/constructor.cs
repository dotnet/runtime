// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Test 
{
    static string s;

    public override string ToString() 
    {
        return "Test";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Test() 
    {
        s = ToString();   // cannot be devirtualized
    }
    
    static int Main() 
    {
        new Child();
        return (s == "Child" ? 100 : 0);
    }
}

class Child : Test 
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Child() { }

    public override string ToString() 
    {
        return "Child";
    }
}
