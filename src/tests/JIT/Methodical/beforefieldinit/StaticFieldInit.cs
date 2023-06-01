// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

static class C
{
    public static int retVal = 100;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestMethod(bool cond)
    {
        if (cond)
        {
            // never invoked in this program
            return ClassB.X;
        }
        // always taken
        return 0;
    }
}

public class ClassB
{
    public static readonly int X = GetX();

    static int GetX()
    {
        Console.WriteLine("GetX call!..."); // not expected to be invoked in this program
        C.retVal = 42;
        return 42;
    }
}

public class StaticFieldInit
{
    public static int Main () {
        C.TestMethod(false);

        return C.retVal;
    }
}