// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSW1 144257
// Loading type C resulted in TypeLoadException

using System;
using Xunit;

interface I
{
    void meth();
}

public class A
{
    public void meth(){}
}

public class B : A
{
    new private void meth(){}
}

public class C : B, I
{
    [Fact]
    public static void TestEntryPoint()
    {
        C c = new C();
        Console.WriteLine("PASS");
    }
}
