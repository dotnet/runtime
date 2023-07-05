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
    public K Get()
    {
        Console.WriteLine("Get called");
        count++;
        return k;
    }

    public bool Test()
    {
        bool x = Get() != null;
        bool y = (count == 1);
        return x && y;
    }

    K k;
    int count;
}

public class B
{
    [Fact]
    public static int TestEntryPoint()
    {
        var a = new X<int>(11);
        bool result = a.Test();
        Console.WriteLine("Passed: {0}", result);
        return result ? 100 : 0;
    }
}
