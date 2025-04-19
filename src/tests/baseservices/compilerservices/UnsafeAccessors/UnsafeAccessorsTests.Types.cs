// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

public static class StaticClass
{
    static StaticClass()
    {
        StaticField = 123;
    }

    public static int StaticField;
    public static int StaticMethod() => StaticField;
}

class C1 { }
class C2 { }

struct S1 { }

class TargetClass
{
    private C2 _f1;
    private readonly C2 _f2;
    private TargetClass(C2 c2)
    {
        _f1 = c2;
        _f2 = c2;
    }
    private C2 M_C1(C1 a) => _f1;
    private C2 M_RC1(ref C1 a) => _f1;
    private C2 M_RROC1(ref readonly C1 a) => _f1;

    private C2 M_ListC1(List<C1> a) => _f1;
}

public static unsafe class UnsafeAccessorsTestsTypes
{
    [Fact]
    public static void Verify_Type_CallDefaultCtorClass()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallDefaultCtorClass)}");

        var local = CallPrivateConstructorClassByName();
        Assert.Equal("UserDataClass", local.GetType().Name);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("UnsafeAccessorsTests+UserDataClass")]
        extern static object CallPrivateConstructorClassByName();
    }

    [Fact]
    public static void Verify_Type_CallCtorClass()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallCtorClass)}");

        var local = CallPrivateConstructorClassByName(string.Empty);
        Assert.Equal("UserDataClass", local.GetType().Name);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("UnsafeAccessorsTests+UserDataClass")]
        extern static object CallPrivateConstructorClassByName(string a);
    }

    [Fact]
    public static void Verify_Type_InvalidArgument()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_InvalidArgument)}");

        Assert.Throws<COMException>(() => CallStaticMethod1(null));
        Assert.Throws<TypeLoadException>(() => CallStaticMethod2(null));
        Assert.Throws<NotSupportedException>(() => CallStaticMethod3(null));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "MethodName")]
        extern static ref int CallStaticMethod1([UnsafeAccessorType(null!)] object a);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "MethodName")]
        extern static ref int CallStaticMethod2([UnsafeAccessorType("_DoesNotExist_")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "MethodName")]
        extern static ref int CallStaticMethod3([UnsafeAccessorType("S1")] object a);
    }

    [Fact]
    public static void Verify_Type_StaticClass()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_StaticClass)}");

        var f = GetStaticClassField(null);
        Assert.Equal(StaticClass.StaticField, f);
        Assert.Equal(StaticClass.StaticField, CallStaticClassMethod(null));

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "StaticField")]
        extern static ref int GetStaticClassField([UnsafeAccessorType("StaticClass")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "StaticMethod")]
        extern static int CallStaticClassMethod([UnsafeAccessorType("StaticClass")] object a);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static TargetClass CreateTargetClass([UnsafeAccessorType("C2")] object a);

    [Fact]
    public static void Verify_Type_CallInstanceMethods()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallInstanceMethods)}");

        C2 c2 = new();
        TargetClass tgt = CreateTargetClass(c2);

        C1 c1 = new();
        object oc1 = c1;
        Assert.Equal(c2, CallM_C1(tgt, c1));
        Assert.Equal(c2, CallM_RC1(tgt, ref oc1));
        Assert.Equal(c2, CallM_RROC1(tgt, ref oc1));
        Assert.Equal(c2, CallM_ListC1(tgt, null));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1")]
        [return: UnsafeAccessorType("C2")]
        extern static object CallM_C1(TargetClass tgt, [UnsafeAccessorType("C1")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_RC1")]
        [return: UnsafeAccessorType("C2")]
        extern static object CallM_RC1(TargetClass tgt, [UnsafeAccessorType("C1")] ref object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_RROC1")]
        [return: UnsafeAccessorType("C2")]
        extern static object CallM_RROC1(TargetClass tgt, [UnsafeAccessorType("C1")] ref readonly object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_ListC1")]
        [return: UnsafeAccessorType("C2")]
        extern static object CallM_ListC1(TargetClass tgt, [UnsafeAccessorType("System.Collections.Generic.List`1[[C1]]")] object? a);
    }

    [Fact]
    public static void Verify_Type_GetInstanceFields()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_GetInstanceFields)}");

        C2 c2 = new();
        TargetClass tgt = CreateTargetClass(c2);

        Assert.Equal(c2, CallField1(tgt));
        Assert.Equal(c2, CallField2(tgt));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_f1")]
        [return: UnsafeAccessorType("C2")]
        extern static ref object CallField1(TargetClass tgt);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_f2")]
        [return: UnsafeAccessorType("C2")]
        extern static ref readonly object CallField2(TargetClass tgt);
    }

    [Fact]
    public static void Verify_Type_FromPrivateLib()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_FromPrivateLib)}");

        Assert.Equal("PrivateLib.Class1", CallGetClass(null).GetType().FullName);
        Assert.Equal("System.Collections.Generic.List`1[[PrivateLib.Class1, PrivateLib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]", CallGetListOfClass(null).GetType().FullName);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetClass")]
        [return: UnsafeAccessorType("PrivateLib.Class1, PrivateLib")]
        extern static object CallGetClass([UnsafeAccessorType("PrivateLib.Class1, PrivateLib")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetListOfClass")]
        [return: UnsafeAccessorType("System.Collections.Generic.List`1[[PrivateLib.Class1, PrivateLib]]")]
        extern static object CallGetListOfClass([UnsafeAccessorType("PrivateLib.Class1, PrivateLib")] object a);
    }
}