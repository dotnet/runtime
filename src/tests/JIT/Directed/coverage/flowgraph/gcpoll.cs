// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.using System;
using System;

public class TestClass13
{
    // With below flags, we were using uninitialized compCurBB during insertGCPolls()
    // COMPlus_JitDoAssertionProp=0
    // COMPlus_JitNoCSE=1
    public void Method0()
    {
        Console.WriteLine();
    }
    public static int Main(string[] args)
    {
        TestClass13 objTestClass13 = new TestClass13();
        objTestClass13.Method0();
        return 100;
    }
}