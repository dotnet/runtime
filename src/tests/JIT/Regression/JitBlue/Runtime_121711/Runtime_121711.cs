// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_121711
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Test(null);
            return 101;
        }
        catch (NullReferenceException)
        {
            return _exitCode;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(Base b)
    {
        b.Foo<string>(Bar());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Bar()
    {
        _exitCode = 100;
        return 42;
    }
    
    private static int _exitCode = 102;
}

public abstract class Base
{
    public abstract void Foo<T>(int x);
}

public class Derived : Base
{
    public override void Foo<T>(int x)
    {
    }
}