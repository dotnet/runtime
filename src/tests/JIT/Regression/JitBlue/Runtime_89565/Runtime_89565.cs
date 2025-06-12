// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_89565
{
    [Fact]
    public static unsafe int Test()
    {
        int result = 0;
        try
        {
            Foo(null);
        }
        catch (NullReferenceException)
        {
            result += 50;
        }
        catch
        {

        }

        try 
        {
            Bar(null, 0);
        }
        catch (DivideByZeroException)
        {
            result += 50;
        }
        catch
        {

        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe float Foo(Vector256<float>* v)
    {
        return (*v)[8];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe float Bar(Vector256<float>* v, int x)
    {
        return (*v)[8/x];
    }
}
