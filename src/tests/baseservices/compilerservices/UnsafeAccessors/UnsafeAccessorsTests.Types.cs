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

    private void M_ByRefs(C1 c, in C1 ic, ref C1 rc, out C1 oc)
    {
        Assert.Null(ic); // See caller
        rc = c;
        oc = c;
    }

    private Type M_C1Array(C1[] c) => typeof(C1[]);
    private Type M_C1Array(C1[,] c) => typeof(C1[,]);
    private Type M_C1Array(C1[,,] c) => typeof(C1[,,]);
    private Type M_C1Array(C1[][] c) => typeof(C1[][]);
    private Type M_C1Array(C1[][][] c) => typeof(C1[][][]);
    private Type M_C1Array(C1[][,] c) => typeof(C1[][,]);

    private Type M_S1Array(S1[] c) => typeof(S1[]);
    private Type M_S1Array(S1[,] c) => typeof(S1[,]);
    private Type M_S1Array(S1[,,] c) => typeof(S1[,,]);
    private Type M_S1Array(S1[][] c) => typeof(S1[][]);
    private Type M_S1Array(S1[][][] c) => typeof(S1[][][]);
    private Type M_S1Array(S1[][,] c) => typeof(S1[][,]);

#pragma warning disable CS8500
    private unsafe void M_C1Pointer(C1* c) { }
#pragma warning restore CS8500

    private class InnerClass
    {
        private InnerClass() { }
        private InnerClass(string _) { }
    }
}

public static unsafe class UnsafeAccessorsTestsTypes
{
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
        object arg = c2;
        TargetClass tgt = CreateTargetClass(arg);

        arg = new C1();
        Assert.Equal(c2, CallM_C1(tgt, arg));
        Assert.Equal(c2, CallM_RC1(tgt, ref arg));
        Assert.Equal(c2, CallM_RROC1(tgt, ref arg));
        AssertExtensions.ThrowsAny<NotSupportedException, MissingMethodException>(()=> CallM_C1_RC2(tgt, arg));

        object ic = null;
        object rc = null;
        object oc = null;
        CallM_ByRefs(tgt, arg, in ic, ref rc, out oc);
        Assert.Null(ic);
        Assert.Equal(arg, rc);
        Assert.Equal(arg, oc);

        Assert.Equal(typeof(C1[]), CallM_C1Array(tgt, Array.Empty<C1>()));
        Assert.Equal(typeof(C1[,]), CallM_C1MDArray2(tgt, new C1[1,1]));
        Assert.Equal(typeof(C1[,,]), CallM_C1MDArray3(tgt, new C1[1,1,1]));
        Assert.Equal(typeof(C1[][]), CallM_C1JaggedArray2(tgt, new C1[0][]));
        Assert.Equal(typeof(C1[][][]), CallM_C1JaggedArray3(tgt, new C1[0][][]));
        Assert.Equal(typeof(C1[][,]), CallM_C1MixedArrays(tgt, new C1[0][,]));

        Assert.Equal(typeof(S1[]), CallM_S1Array(tgt, Array.Empty<S1>()));
        Assert.Equal(typeof(S1[,]), CallM_S1MDArray2(tgt, new S1[1,1]));
        Assert.Equal(typeof(S1[,,]), CallM_S1MDArray3(tgt, new S1[1,1,1]));
        Assert.Equal(typeof(S1[][]), CallM_S1JaggedArray2(tgt, new S1[0][]));
        Assert.Equal(typeof(S1[][][]), CallM_S1JaggedArray3(tgt, new S1[0][][]));
        Assert.Equal(typeof(S1[][,]), CallM_S1MixedArrays(tgt, new S1[0][,]));

        CallM_C1Pointer(tgt, null);

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

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_ByRefs")]
        extern static void CallM_ByRefs(TargetClass tgt,
            [UnsafeAccessorType("C1")] object c,
            [UnsafeAccessorType("C1&")] in object ic,
            [UnsafeAccessorType("C1&")] ref object rc,
            [UnsafeAccessorType("C1&")] out object oc);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1Array")]
        extern static Type CallM_C1Array(TargetClass tgt, [UnsafeAccessorType("C1[]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1Array")]
        extern static Type CallM_C1MDArray2(TargetClass tgt, [UnsafeAccessorType("C1[,]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1Array")]
        extern static Type CallM_C1MDArray3(TargetClass tgt, [UnsafeAccessorType("C1[,,]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1Array")]
        extern static Type CallM_C1JaggedArray2(TargetClass tgt, [UnsafeAccessorType("C1[][]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1Array")]
        extern static Type CallM_C1JaggedArray3(TargetClass tgt, [UnsafeAccessorType("C1[][][]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1Array")]
        extern static Type CallM_C1MixedArrays(TargetClass tgt, [UnsafeAccessorType("C1[,][]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_S1Array")]
        extern static Type CallM_S1Array(TargetClass tgt, [UnsafeAccessorType("S1[]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_S1Array")]
        extern static Type CallM_S1MDArray2(TargetClass tgt, [UnsafeAccessorType("S1[,]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_S1Array")]
        extern static Type CallM_S1MDArray3(TargetClass tgt, [UnsafeAccessorType("S1[,,]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_S1Array")]
        extern static Type CallM_S1JaggedArray2(TargetClass tgt, [UnsafeAccessorType("S1[][]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_S1Array")]
        extern static Type CallM_S1JaggedArray3(TargetClass tgt, [UnsafeAccessorType("S1[][][]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_S1Array")]
        extern static Type CallM_S1MixedArrays(TargetClass tgt, [UnsafeAccessorType("S1[,][]")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M_C1Pointer")]
        extern static void CallM_C1Pointer(TargetClass tgt, [UnsafeAccessorType("C1*")] void* a);
    }

    [Fact]
    public static void Verify_Type_GetInstanceFields_NotSupported()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_GetInstanceFields_NotSupported)}");

        C2 c2 = new();
        TargetClass tgt = CreateTargetClass(c2);

        // The following calls should throw NotSupportedException.
        // Mono throws MissingFieldException since throwing NotSupportedException is difficult to implement.
        AssertExtensions.ThrowsAny<NotSupportedException, MissingFieldException>(()=> CallField1(tgt));
        AssertExtensions.ThrowsAny<NotSupportedException, MissingFieldException>(()=> CallField2(tgt));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_f1")]
        [return: UnsafeAccessorType("C2")]
        extern static ref object CallField1(TargetClass tgt);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_f2")]
        [return: UnsafeAccessorType("C2")]
        extern static ref readonly object CallField2(TargetClass tgt);
    }

    [Fact]
    public static void Verify_Type_CallInnerCtorClass()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallInnerCtorClass)}");

        object obj;

        obj = CreateInner();
        Assert.Equal("InnerClass", obj.GetType().Name);

        obj = CreateInnerString(string.Empty);
        Assert.Equal("InnerClass", obj.GetType().Name);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("TargetClass+InnerClass")]
        extern static object CreateInner();

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("TargetClass+InnerClass")]
        extern static object CreateInnerString(string a);
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

    partial class Accessors<T>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("PrivateLib.GenericClass`1[[!-0]], PrivateLib")]
        public extern static object CreateGenericClass_InvalidGenericIndex1();

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("PrivateLib.GenericClass`1[[!+0]], PrivateLib")]
        public extern static object CreateGenericClass_InvalidGenericIndex2();

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("PrivateLib.GenericClass`1[[!-1]], PrivateLib")]
        public extern static object CreateGenericClass_InvalidGenericIndex3();

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("PrivateLib.GenericClass`1[[!1]], PrivateLib")]
        public extern static object CreateGenericClass_InvalidGenericIndex4();

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

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M4")]
        [return: UnsafeAccessorType("System.Collections.Generic.List`1[[System.Object]]")]
        public extern static object CallGenericClassM4_InvalidReturn([UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M5")]
        public extern static bool CallGenericClassM5<V, W>(
            [UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object tgt,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[!0]]")]
            object a,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[!!0]]")]
            object b,
            List<W> c,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[PrivateLib.Class2, PrivateLib]]")]
            object d) where W : T;

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M5")]
        public extern static bool CallGenericClassM5_NoConstraint<V, W>(
            [UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object tgt,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[!0]]")]
            object a,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[!!0]]")]
            object b,
            List<W> c,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[PrivateLib.Class2, PrivateLib]]")]
            object d);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M6")]
        public extern static Type CallGenericClassM6<X>(
            [UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object tgt,
            [UnsafeAccessorType("System.Collections.Generic.Dictionary`2[[!!0],[System.Int32]]")]
            object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M7")]
        public extern static Type CallGenericClassM7<Y>(
            [UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object tgt,
            [UnsafeAccessorType("System.Collections.Generic.Dictionary`2[[System.Int32],[!!0]]")]
            object a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M8")]
        [return: UnsafeAccessorType("!!0")]
        public extern static object CallGenericClassM8<Z>(
            [UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object tgt) where Z : class, new();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M9")]
        public extern static bool CallGenericClassM9<A>(
            [UnsafeAccessorType("PrivateLib.GenericClass`1[[!0]], PrivateLib")] object tgt,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[!!0]]]]")]
            object a,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[!!0[,,]]]")]
            object b,
            [UnsafeAccessorType("System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[!0[][,]]]]]")]
            object c);
    }

    // Skip validating error cases on Mono runtime
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void Verify_Type_InvalidGenericTypeString()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_InvalidGenericTypeString)}");

        Assert.Throws<TypeLoadException>(() => Accessors<int>.CreateGenericClass_InvalidGenericIndex1());
        Assert.Throws<TypeLoadException>(() => Accessors<int>.CreateGenericClass_InvalidGenericIndex2());
        Assert.Throws<TypeLoadException>(() => Accessors<int>.CreateGenericClass_InvalidGenericIndex3());
        Assert.Throws<TypeLoadException>(() => Accessors<int>.CreateGenericClass_InvalidGenericIndex4());
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

            Assert.Throws<MissingMethodException>(() => Accessors<int>.CallGenericClassM4_InvalidReturn(genericClass));
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

            Assert.Throws<MissingMethodException>(() => Accessors<string>.CallGenericClassM4_InvalidReturn(genericClass));
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

            Assert.True(Accessors<int>.CallGenericClassM5<string, int>(genericClass, null, null, null, null));
            Assert.Equal(typeof(int), Accessors<int>.CallGenericClassM6<int>(genericClass, null));
            Assert.Equal(typeof(int), Accessors<int>.CallGenericClassM7<int>(genericClass, null));
            Assert.True(Accessors<int>.CallGenericClassM9<string>(genericClass, null, null, null));
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

            Assert.True(Accessors<string>.CallGenericClassM5<int, string>(genericClass, null, null, null, null));
            Assert.Equal(typeof(string), Accessors<string>.CallGenericClassM6<string>(genericClass, null));
            Assert.Equal(typeof(string), Accessors<string>.CallGenericClassM7<string>(genericClass, null));
            Assert.Equal(typeof(C1), Accessors<string>.CallGenericClassM8<C1>(genericClass).GetType());
            Assert.True(Accessors<string>.CallGenericClassM9<int>(genericClass, null, null, null));
        }
    }

    // Skip private types and Generic support on Mono runtime
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void Verify_Type_CallPrivateLibTypeAndMethodGenericParamsWithConstraints()
    {
        Console.WriteLine($"Running {nameof(Verify_Type_CallPrivateLibTypeAndMethodGenericParamsWithConstraints)}");

        {
            object genericClass = Accessors<int>.CreateGenericClass();
            Assert.True(Accessors<int>.CallGenericClassM5<string, int>(genericClass, null, null, null, null));
            Assert.Throws<InvalidProgramException>(() => Accessors<int>.CallGenericClassM5_NoConstraint<string, string>(genericClass, null, null, null, null));
        }

        {
            object genericClass = Accessors<string>.CreateGenericClass();
            Assert.True(Accessors<string>.CallGenericClassM5<int, string>(genericClass, null, null, null, null));
            Assert.Throws<InvalidProgramException>(() => Accessors<string>.CallGenericClassM5_NoConstraint<int, int>(genericClass, null, null, null, null));
        }
    }
}
