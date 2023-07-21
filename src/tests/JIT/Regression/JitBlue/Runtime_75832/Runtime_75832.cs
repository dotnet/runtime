// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_75832
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Test(0);
            Console.WriteLine("FAIL: No exception thrown");
        }
        catch (DivideByZeroException)
        {
            return 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL: Caught {0}", ex.GetType().Name);
        }
        
        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(int i)
    {
        GetAction()(100 / i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Action<int> GetAction() => null;
}
