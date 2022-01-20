// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using InvalidCSharp;

using Xunit;

class Validate
{
    [Fact]
    public static void Validate_Invalid_RefField_Fails()
    {
        Console.WriteLine($"{nameof(Validate_Invalid_RefField_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(InvalidStructWithRefField); });
    }

    [Fact]
    public static void Validate_RefStructWithRefField_Load()
    {
        Console.WriteLine($"{nameof(Validate_RefStructWithRefField_Load)}...");
        var t = typeof(WithRefField);
    }

    [Fact]
    public static void Validate_Create_RefField()
    {
        var str = nameof(Validate_Create_RefField);
        Console.WriteLine($"{str}...");

        WithRefField s = new(ref str);
        Assert.True(s.ConfirmFieldInstance(str));

        string newStr = new(str);
        Assert.False(s.ConfirmFieldInstance(newStr));
    }

    [Fact]
    public static void Validate_Create_RefStructField()
    {
        var str = nameof(Validate_Create_RefStructField);
        Console.WriteLine($"{str}...");

        WithRefField s = new(ref str);
        WithRefStructField t = new(ref s);
        Assert.True(t.ConfirmFieldInstance(ref s));
    }

    [Fact]
    public static void Validate_Create_TypedReferenceRefField()
    {
        Console.WriteLine($"{nameof(Validate_Create_TypedReferenceRefField)}...");

        Validate v = new();
        WithTypedReferenceField<Validate> s = new(v);
        Assert.Equal(typeof(Validate), s.GetFieldType());
    }
}