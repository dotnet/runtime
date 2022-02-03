// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using InvalidCSharp;

using Xunit;

class Validate
{
    [Fact]
    public static void Validate_Invalid_RefField_Fails()
    {
        Console.WriteLine($"{nameof(Validate_Invalid_RefField_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(InvalidStructWithRefField); });
        Assert.Throws<TypeLoadException>(() => { var t = typeof(InvalidRefFieldAlignment); });
        Assert.Throws<TypeLoadException>(() => { var t = typeof(InvalidObjectRefRefFieldOverlap); });
    }

    [Fact]
    public static void Validate_RefStructWithRefField_Load()
    {
        Console.WriteLine($"{nameof(Validate_RefStructWithRefField_Load)}...");
        var t = typeof(WithRefField);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Create_RefField_Worker(string str, int depth)
    {
        if (depth == 5)
        {
            return;
        }

        WithRefField s = new(ref str);
        string newStr = new(str);

        Create_RefField_Worker(str + $" {depth}", depth + 1);
        Assert.False(s.ConfirmFieldInstance(newStr));
        Assert.True(s.ConfirmFieldInstance(str));
    }

    [Fact]
    public static void Validate_Create_RefField()
    {
        var str = nameof(Validate_Create_RefField);
        Console.WriteLine($"{str}...");
        Create_RefField_Worker(str, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Create_RefStructField_Worker(string str, int depth)
    {
        if (depth == 5)
        {
            return;
        }

        WithRefField s = new(ref str);
        WithRefStructField t = new(ref s);

        Create_RefStructField_Worker(str + $" {depth}", depth + 1);
        Assert.True(t.ConfirmFieldInstance(ref s));
    }

    [Fact]
    public static void Validate_Create_RefStructField()
    {
        var str = nameof(Validate_Create_RefStructField);
        Console.WriteLine($"{str}...");
        Create_RefStructField_Worker(str, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Create_TypedReferenceRefField_Worker(Validate v, int depth)
    {
        if (depth == 5)
        {
            return;
        }

        WithTypedReferenceField<Validate> s = new(ref v);

        Create_TypedReferenceRefField_Worker(v, depth + 1);
        Assert.Equal(typeof(Validate), s.GetFieldType());
        Assert.True(s.ConfirmFieldInstance(v));
    }

    [Fact]
    public static void Validate_Create_TypedReferenceRefField()
    {
        Console.WriteLine($"{nameof(Validate_Create_TypedReferenceRefField)}...");

        Validate v = new();
        Create_TypedReferenceRefField_Worker(v, 1);
    }
}