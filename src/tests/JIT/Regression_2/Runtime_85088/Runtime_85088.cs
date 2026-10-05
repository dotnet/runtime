// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_85088
{
    [Fact]
    public static int Test()
    {
        Foo f = new();
        try
        {
            try
            {
                throw new Exception();
            }
            finally
            {
                f.X = 15;
                f.Y = 20;
                f.X += f.Y;
                f.Y *= f.X;

                // f will be physically promoted and will require a read back after this call.
                // Since this is a finally, some platforms will have a GT_RETFILT that we were
                // inserting IR after instead of before.
                f = Call(f);
            }
        }
        catch
        {
        }

        return f.X + f.Y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Foo Call(Foo f)
    {
        return new Foo { X = 75, Y = 25 };
    }

    private struct Foo
    {
        public short X, Y;
    }
}
