// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;

public class X<K> 
{
    public X(K k1)
    {
        k = k1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test(X<K> a)
    {
        return (a.k != null);
    }

    public K k;
}

class B
{
    public static int Main()
    {
        X<Vector3> a = null;
        bool result = false;
        try 
        {
            X<Vector3>.Test(a);
        }
        catch (Exception)
        {
            result = true;
        }
        Console.WriteLine("Passed: {0}", result);
        return result ? 100 : 0;
    }
}
