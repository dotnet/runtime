// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_86498
{
    [Fact]
    public static int Test()
    {
        Foo f = new();
        try
        {
            f.X = 15;
            f.Y = 20;
            f.X += f.Y;
            f.Y *= f.X;

            // f will be physically promoted and will require a read back after this call.
            // However, there is implicit control flow happening that the read back should happen before.
            f = Call(f);
            ThrowException();
            return -1;
        }
        catch (Exception ex)
        {
            return f.X + f.Y;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Foo Call(Foo f)
    {
        return new Foo { X = 75, Y = 25 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowException()
    {
        throw new Exception();
    }

    private struct Foo
    {
        public short X, Y;
    }
}
