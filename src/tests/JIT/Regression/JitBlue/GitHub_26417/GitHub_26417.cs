// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_26417
{
    static int   _a;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void MyWriteLine(int v)
    {
        Console.WriteLine(v);
        if (v == 0)
        {
            throw new Exception();
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void Test()
    {
        _a = 1;
        
        while (_a == 1)
        {
            MyWriteLine(_a);
            _a = 0;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;
        try {
            Test();
        }
        catch (Exception)
        {
            Console.WriteLine("FAILED");
            result = -1;
        }
        return result;
    }
}
