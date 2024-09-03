// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

public unsafe class Validate
{
    [Fact]
    public static void Explicit_Fails()
    {
        Console.WriteLine($"{nameof(Explicit_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(Explicit); });

        Assert.Throws<TypeLoadException>(() =>
        {
            return sizeof(Explicit);
        });
    }

    [Fact]
    public static void ZeroLength_Fails()
    {
        Console.WriteLine($"{nameof(ZeroLength_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(ZeroLength); });

        Assert.Throws<TypeLoadException>(() =>
        {
            var t = new ZeroLength()
            {
                field = 1
            };
            return t;
        });
    }

    [InlineArray(16777216)]
    private struct TooLarge
    {
        public long field;
    }

    [Fact]
    public static void TooLarge_Fails()
    {
        Console.WriteLine($"{nameof(TooLarge_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(TooLarge); });

        Assert.Throws<TypeLoadException>(() =>
        {
            var t = new TooLarge()
            {
                field = 1
            };
            return t;
        });
    }

    [Fact]
    public static void NegativeLength_Fails()
    {
        Console.WriteLine($"{nameof(NegativeLength_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(NegativeLength); });

        Assert.Throws<TypeLoadException>(() =>
        {
            var t = new NegativeLength()
            {
                field = 1
            };
            return t;
        });
    }

    [Fact]
    public static void NoFields_Fails()
    {
        Console.WriteLine($"{nameof(NoFields_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(NoFields); });

        Assert.Throws<TypeLoadException>(() =>
        {
            return (new NoFields()).ToString();
        });
    }

    [Fact]
    public static void TwoFields_Fails()
    {
        Console.WriteLine($"{nameof(TwoFields_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(TwoFields); });

        Assert.Throws<TypeLoadException>(() =>
        {
            return new TwoFields[12];
        });
    }
}
