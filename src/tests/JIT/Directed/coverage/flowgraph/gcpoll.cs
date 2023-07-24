// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.using System;
using System;
using Xunit;

public class TestClass13
{
    // With below flags, we were using uninitialized compCurBB during insertGCPolls()
    // DOTNET_JitDoAssertionProp=0
    // DOTNET_JitNoCSE=1
    internal void Method0()
    {
        Console.WriteLine();
    }
    [Fact]
    public static int TestEntryPoint()
    {
        TestClass13 objTestClass13 = new TestClass13();
        objTestClass13.Method0();
        return 100;
    }
}
