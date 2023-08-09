// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class X<K> 
{
    public X(K k1)
    {
        k = k1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test(X<K> a)
    {
        return (a.k == null);
    }

    public K k;
}

public class B
{
    [Fact]
    public static int TestEntryPoint()
    {
        X<int> a = null;
        bool result = false;
        try 
        {
            X<int>.Test(a);
        }
        catch (Exception)
        {
            result = true;
        }
        Console.WriteLine("Passed: {0}", result);
        return result ? 100 : 0;
    }
}
