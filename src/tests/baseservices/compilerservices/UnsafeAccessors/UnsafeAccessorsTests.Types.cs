// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
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
    private TargetClass(ref C2 c2)
    {
        _f1 = c2;
        _f2 = c2;
    }
    private C2 M_C1(C1 a) => _f1;
    private C2 M_RC1(ref C1 a) => _f1;
    private C2 M_RROC1(ref readonly C1 a) => _f1;
    private ref C2 M_C1_RC2(C1 a) => ref _f1;
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

    // Skip validating error cases on Mono runtime
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void Verify_Type_InvalidArgument()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_InvalidArgument)}");

        AssertExtensions.ThrowsAny<COMException, InvalidProgramException>(() => CallStaticMethod1(null));
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

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static TargetClass CreateTargetClass([UnsafeAccessorType("C2&")] ref object a);

    // Skip validating error cases on Mono runtime
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void Verify_Type_TypeCheck()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_TypeCheck)}");

        Assert.Throws<InvalidCastException>(() => CreateTargetClass(new C1()));
        Assert.Throws<InvalidCastException>(() =>
        {
            object c1 = new C1();
            CreateTargetClass(ref c1);
        });
    }

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
        Assert.Equal(c2, CallM_C1_RC2(tgt, c1));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1")]
        [return: UnsafeAccessorType("C2")]
        extern static object CallM_C1(TargetClass tgt, [UnsafeAccessorType("C1")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_RC1")]
        [return: UnsafeAccessorType("C2")]
        extern static object CallM_RC1(TargetClass tgt, [UnsafeAccessorType("C1&")] ref object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_RROC1")]
        [return: UnsafeAccessorType("C2")]
        extern static object CallM_RROC1(TargetClass tgt, [UnsafeAccessorType("C1&")] ref readonly object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1_RC2")]
        [return: UnsafeAccessorType("C2&")]
        extern static ref object CallM_C1_RC2(TargetClass tgt, [UnsafeAccessorType("C1")] object a);
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

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetClass")]
    [return: UnsafeAccessorType("PrivateLib.Class1, PrivateLib")]
    extern static object CallGetClass([UnsafeAccessorType("PrivateLib.Class1, PrivateLib")] object a);

    [Fact]
    public static void Verify_Type_CallPrivateLibMethods()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallPrivateLibMethods)}");

        {
            object class1 = CreateClass();
            Assert.Equal("PrivateLib.Class1", class1.GetType().FullName);
        }

        {
            object class1 = CallGetClass(null);
            Assert.Equal("PrivateLib.Class1", class1.GetType().FullName);
            object listClass2 = CallGetClass2(class1);
            Assert.Equal("PrivateLib.Class2", listClass2.GetType().FullName);
        }

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("PrivateLib.Class1, PrivateLib")]
        extern static object CreateClass();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetClass2")]
        [return: UnsafeAccessorType("PrivateLib.Class2, PrivateLib")]
        extern static object CallGetClass2([UnsafeAccessorType("PrivateLib.Class1, PrivateLib")] object a);
    }

    [Fact]
    public static void Verify_Type_GetPrivateLibFields()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_GetPrivateLibFields)}");

        object class1 = CallGetClass(null);
        Assert.Equal("PrivateLib.Class1", class1.GetType().FullName);

        Assert.Equal(123, GetStaticField(null));
        Assert.Equal(456, GetInstanceField(class1));

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "StaticField")]
        extern static ref int GetStaticField([UnsafeAccessorType("PrivateLib.Class1, PrivateLib")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "InstanceField")]
        extern static ref int GetInstanceField([UnsafeAccessorType("PrivateLib.Class1, PrivateLib")] object a);
    }

    class Accessors<T>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")]
        public extern static object CreateGenericClass();

        // Class type variables
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M1")]
        [return: UnsafeAccessorType("System.Collections.Generic.List`1[[!0]]")]
        public extern static object CallGenericClassM1([UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object a);

        // Method type variables
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M2")]
        [return: UnsafeAccessorType("System.Collections.Generic.List`1[[!!0]]")]
        public extern static object CallGenericClassM2<U>([UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object a);

        // Bound type variables
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M3")]
        public extern static List<int> CallGenericClassM3([UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M4")]
        [return: UnsafeAccessorType("System.Collections.Generic.List`1[[PrivateLib.Class2, PrivateLib]]")]
        public extern static object CallGenericClassM4([UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object a);
    }

    private static bool TypeNameEquals(TypeName typeName1, TypeName typeName2)
    {
        if (typeName1.Name != typeName2.Name)
        {
            return false;
        }

        if (typeName1.IsConstructedGenericType != typeName2.IsConstructedGenericType)
        {
            return false;
        }

        var typeArgs1 = typeName1.GetGenericArguments();
        var typeArgs2 = typeName2.GetGenericArguments();
        if (typeArgs1.Length != typeArgs2.Length)
        {
            return false;
        }

        for (int i = 0; i < typeArgs1.Length; i++)
        {
            if (!TypeNameEquals(typeArgs1[i], typeArgs2[i]))
            {
                return false;
            }
        }

        return true;
    }

    // Skip private types and Generic support on Mono runtime
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void Verify_Type_CallPrivateLibTypeGenericParams()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallPrivateLibTypeGenericParams)}");

        {
            object genericClass = Accessors<int>.CreateGenericClass();
            TypeName genericClassName = TypeName.Parse(genericClass.GetType().FullName);
            Assert.True(TypeNameEquals(genericClassName, TypeName.Parse("PrivateLib.GenericClass`1[[System.Int32]]")));

            object genericListT = Accessors<int>.CallGenericClassM1(genericClass);
            TypeName genericListTName = TypeName.Parse(genericListT.GetType().FullName);
            Assert.True(TypeNameEquals(genericListTName, TypeName.Parse("System.Collections.Generic.List`1[[System.Int32]]")));

            List<int> boundListInt = Accessors<int>.CallGenericClassM3(genericClass);
            Assert.Empty(boundListInt);

            object genericListClass2 = Accessors<int>.CallGenericClassM4(genericClass);
            TypeName genericListClass2Name = TypeName.Parse(genericListClass2.GetType().FullName);
            Assert.True(TypeNameEquals(genericListClass2Name, TypeName.Parse("System.Collections.Generic.List`1[[PrivateLib.Class2, PrivateLib]]")));
        }

        {
            object genericClass = Accessors<string>.CreateGenericClass();
            TypeName genericClassName = TypeName.Parse(genericClass.GetType().FullName);
            Assert.True(TypeNameEquals(genericClassName, TypeName.Parse("PrivateLib.GenericClass`1[[System.String]]")));

            object genericListT = Accessors<string>.CallGenericClassM1(genericClass);
            TypeName genericListTName = TypeName.Parse(genericListT.GetType().FullName);
            Assert.True(TypeNameEquals(genericListTName, TypeName.Parse("System.Collections.Generic.List`1[[System.String]]")));

            List<int> boundListInt = Accessors<string>.CallGenericClassM3(genericClass);
            Assert.Empty(boundListInt);

            object genericListClass2 = Accessors<string>.CallGenericClassM4(genericClass);
            TypeName genericListClass2Name = TypeName.Parse(genericListClass2.GetType().FullName);
            Assert.True(TypeNameEquals(genericListClass2Name, TypeName.Parse("System.Collections.Generic.List`1[[PrivateLib.Class2, PrivateLib]]")));
        }
    }

    // Skip private types and Generic support on Mono runtime
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void Verify_Type_CallPrivateLibTypeAndMethodGenericParams()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallPrivateLibTypeAndMethodGenericParams)}");

        {
            object genericClass = Accessors<int>.CreateGenericClass();
            TypeName genericClassName = TypeName.Parse(genericClass.GetType().FullName);
            Assert.True(TypeNameEquals(genericClassName, TypeName.Parse("PrivateLib.GenericClass`1[[System.Int32]]")));

            object genericListInt = Accessors<int>.CallGenericClassM2<int>(genericClass);
            TypeName genericListIntName = TypeName.Parse(genericListInt.GetType().FullName);
            Assert.True(TypeNameEquals(genericListIntName, TypeName.Parse("System.Collections.Generic.List`1[[System.Int32]]")));

            object genericListString = Accessors<int>.CallGenericClassM2<string>(genericClass);
            TypeName genericListStringName = TypeName.Parse(genericListString.GetType().FullName);
            Assert.True(TypeNameEquals(genericListStringName, TypeName.Parse("System.Collections.Generic.List`1[[System.String]]")));
        }

        {
            object genericClass = Accessors<string>.CreateGenericClass();
            TypeName genericClassName = TypeName.Parse(genericClass.GetType().FullName);
            Assert.True(TypeNameEquals(genericClassName, TypeName.Parse("PrivateLib.GenericClass`1[[System.String]]")));

            object genericListInt = Accessors<string>.CallGenericClassM2<int>(genericClass);
            TypeName genericListIntName = TypeName.Parse(genericListInt.GetType().FullName);
            Assert.True(TypeNameEquals(genericListIntName, TypeName.Parse("System.Collections.Generic.List`1[[System.Int32]]")));

            object genericListString = Accessors<string>.CallGenericClassM2<string>(genericClass);
            TypeName genericListStringName = TypeName.Parse(genericListString.GetType().FullName);
            Assert.True(TypeNameEquals(genericListStringName, TypeName.Parse("System.Collections.Generic.List`1[[System.String]]")));
        }
    }
}
