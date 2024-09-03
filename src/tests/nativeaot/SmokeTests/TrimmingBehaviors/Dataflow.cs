// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

#pragma warning disable 649 // 'blah' is never assigned to
#pragma warning disable 169 // 'blah' is never used

class Dataflow
{
    public static int Run()
    {
        TestReturnValue.Run();
        TestFieldAccess.Run();
        TestGetMethodEventFieldPropertyConstructor.Run();
        TestGetInterface.Run();
        TestInGenericCode.Run();
        TestAttributeDataflow.Run();
        TestGenericDataflow.Run();
        TestArrayDataflow.Run();
        TestAllDataflow.Run();
        TestDynamicDependency.Run();
        TestDynamicDependencyWithGenerics.Run();
        TestObjectGetTypeDataflow.Run();
        TestMakeGenericDataflow.Run();
        TestMakeGenericDataflowInvalid.Run();
        TestMarshalIntrinsics.Run();
        Regression97758.Run();

        return 100;
    }

    class TestReturnValue
    {
        class PublicOnly
        {
            public PublicOnly(int x) { }
            private PublicOnly(double x) { }
        }

        class PublicAndPrivate
        {
            public PublicAndPrivate(int x) { }
            private PublicAndPrivate(double x) { }
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        static Type GiveMePublic() => typeof(PublicOnly);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        static Type GiveMePublicAndPrivate() => typeof(PublicAndPrivate);

        public static void Run()
        {
            GiveMePublic();
            Assert.Equal(1, typeof(PublicOnly).CountConstructors());

            GiveMePublicAndPrivate();
            Assert.Equal(2, typeof(PublicAndPrivate).CountConstructors());
        }
    }

    class TestFieldAccess
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type s_annotatedType;

        static class TestClass
        {
            public static void UnusedButKeptMethod() { }
        }

        private static void SetField() => s_annotatedType = typeof(TestClass);

        public static void Run()
        {
            SetField();

            Assert.Equal(1, typeof(TestClass).CountPublicMethods());
        }
    }

    class TestGetMethodEventFieldPropertyConstructor
    {
        static class TestType1
        {
            public static void TestMethod() => throw null;
            public static void UnreferencedMethod() => throw null;
            public static int TestField;
            public static int UnreferencedField;
        }

        static class TestType2
        {
            public static int TestProperty { get; set; }
            public static int UnreferencedProperty { get; set; }
        }

        class TestType3
        {
            public TestType3(int val) { }
            private TestType3(double val) { }
        }

        public static void Run()
        {
            Assert.NotNull(typeof(TestType1).GetMethod(nameof(TestType1.TestMethod)));
            Assert.Equal(1, typeof(TestType1).CountMethods());

            Assert.NotNull(typeof(TestType1).GetField(nameof(TestType1.TestField)));
            Assert.Equal(1, typeof(TestType1).CountFields());

            Assert.NotNull(typeof(TestType2).GetProperty(nameof(TestType2.TestProperty)));
            Assert.NotNull(typeof(TestType2).GetProperty(nameof(TestType2.TestProperty)).GetGetMethod());
            Assert.NotNull(typeof(TestType2).GetProperty(nameof(TestType2.TestProperty)).GetSetMethod());
            Assert.Equal(1, typeof(TestType2).CountProperties());
            Assert.Equal(2, typeof(TestType2).CountMethods());

            Assert.NotNull(typeof(TestType3).GetConstructor(new Type[] { typeof(int) }));
            Assert.Equal(1, typeof(TestType3).CountConstructors());
        }
    }

    class TestGetInterface
    {
        interface INeverUsedInterface
        {
        }

        class UsedType : INeverUsedInterface
        {
        }

        public static void Run()
        {
            typeof(UsedType).GetInterfaces();
            Assert.Equal(1, typeof(UsedType).CountInterfaces());
        }
    }

    class TestInGenericCode
    {
        class MyGenericType<T>
        {
            public static void MyGenericMethod<U>(T param1, U param2)
            {

            }
        }

        static void GenericMethod<T, U>()
        {
            // Ensure this method body is looked at by dataflow analysis
            Assert.NotNull(typeof(TestType).GetConstructor(new Type[] { typeof(double) }));

            // Regression test for a bug where we would try to resolve !1 (the U parameter)
            // within the signature of MyGenericMethod (that doesn't have a second generic parameter
            // and would cause an out-of-bounds array access at analysis time)
            MyGenericType<U>.MyGenericMethod<U>(default, default);
        }

        class TestType
        {
            public TestType(double c) { }
        }

        public static void Run()
        {
            GenericMethod<object, object>();
        }
    }

    class TestAttributeDataflow
    {
        class RequiresNonPublicMethods : Attribute
        {
            public RequiresNonPublicMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type needed)
            {
            }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type AlsoNeeded { get; set; }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type AndAlsoNeeded;
        }

        static class Type1WithNonPublicKept
        {
            private static void KeptMethod() { }
            public static void RemovedMethod() { }
        }

        static class Type2WithAllKept
        {
            private static void KeptMethod() { }
            public static void AlsoKeptMethod() { }
        }

        static class Type3WithPublicKept
        {
            public static void KeptMethod() { }
            private static void RemovedMethod() { }
        }

        [RequiresNonPublicMethods(typeof(Type1WithNonPublicKept), AlsoNeeded = typeof(Type2WithAllKept), AndAlsoNeeded = typeof(Type3WithPublicKept))]
        public static void Run()
        {
            // Make it so that the analysis believes the Run method needs metadata. We wouldn't look at the attributes otherwise.
            typeof(TestAttributeDataflow).GetMethod(nameof(Run));

            Assert.Equal(0, typeof(Type1WithNonPublicKept).CountPublicMethods());
            Assert.Equal(1, typeof(Type1WithNonPublicKept).CountMethods());

            Assert.Equal(2, typeof(Type2WithAllKept).CountMethods());

            Assert.Equal(1, typeof(Type3WithPublicKept).CountPublicMethods());
            Assert.Equal(1, typeof(Type3WithPublicKept).CountMethods());
        }
    }

    class TestGenericDataflow
    {
        class Type1WithNonPublicKept
        {
            private static void KeptMethod() { }
            private static void AlsoKeptMethod() { }
            public static void RemovedMethod() { }
        }

        class Type2WithPublicKept
        {
            public static void KeptMethod() { }
            public static void AlsoKeptMethod() { }
            private static void RemovedMethod() { }
        }

        class Type3WithPublicKept
        {
            public static void KeptMethod() { }
            public static void AlsoKeptMethod() { }
            private static void RemovedMethod() { }
        }

        class Type4WithPublicKept
        {
            public static void KeptMethod() { }
            public static void AlsoKeptMethod() { }
            private static void RemovedMethod() { }
        }

        struct Struct1WithPublicKept
        {
            public static void KeptMethod() { }
            public static void AlsoKeptMethod() { }
            private static void RemovedMethod() { }
        }


        class KeepsNonPublic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] T>
        {
            public KeepsNonPublic()
            {
                Assert.NotNull(typeof(T).GetMethod("KeptMethod", BindingFlags.NonPublic | BindingFlags.Static));
            }
        }

        class KeepsNonPublic : KeepsNonPublic<Type1WithNonPublicKept>
        {
        }

        static void KeepPublic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
        {
            Assert.NotNull(typeof(T).GetMethod("KeptMethod", BindingFlags.Public | BindingFlags.Static));
        }

        class KeepsPublic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>
        {
            public static void Keep<U>()
            {
                Assert.NotNull(typeof(T).GetMethod("KeptMethod", BindingFlags.Public | BindingFlags.Static));
            }
        }

        static IKeepPublicThroughGvm s_keepPublicThroughGvm = new KeepPublicThroughGvm();

        interface IKeepPublicThroughGvm
        {
            void Keep<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>();
        }

        class KeepPublicThroughGvm : IKeepPublicThroughGvm
        {
            public void Keep<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
            {
                Assert.NotNull(typeof(T).GetMethod("KeptMethod", BindingFlags.Public | BindingFlags.Static));
            }
        }

        public static void Run()
        {
            new KeepsNonPublic();
            Assert.Equal(2, typeof(Type1WithNonPublicKept).CountMethods());
            Assert.Equal(0, typeof(Type1WithNonPublicKept).CountPublicMethods());

            KeepPublic<Type2WithPublicKept>();
            Assert.Equal(2, typeof(Type2WithPublicKept).CountMethods());
            Assert.Equal(2, typeof(Type2WithPublicKept).CountPublicMethods());

            KeepPublic<Struct1WithPublicKept>();
            Assert.Equal(2, typeof(Struct1WithPublicKept).CountMethods());
            Assert.Equal(2, typeof(Struct1WithPublicKept).CountPublicMethods());

            KeepsPublic<Type3WithPublicKept>.Keep<object>();
            Assert.Equal(2, typeof(Type3WithPublicKept).CountMethods());
            Assert.Equal(2, typeof(Type3WithPublicKept).CountPublicMethods());

            s_keepPublicThroughGvm.Keep<Type4WithPublicKept>();
            Assert.Equal(2, typeof(Type4WithPublicKept).CountMethods());
            Assert.Equal(2, typeof(Type4WithPublicKept).CountPublicMethods());
        }
    }

    class TestArrayDataflow
    {
        public static void Run()
        {
            // System.Array has 7 public properties
            // This test might be a bit fragile, but we want to make sure accessing properties
            // on an array triggers same as accessing properties on System.Array.
            Assert.Equal(7, typeof(int[]).GetProperties().Length);

            // Regression test for when dataflow analysis was trying to generate method bodies for these
            Assert.Equal(1, typeof(int[]).GetConstructors().Length);
        }
    }

    class TestAllDataflow
    {
        class Base
        {
            private static int GetNumber() => 42;
        }

        class Derived : Base
        {
        }

        private static MethodInfo GetPrivateMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t)
        {
            return t.BaseType.GetMethod("GetNumber", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static void Run()
        {
            if ((int)GetPrivateMethod(typeof(Derived)).Invoke(null, Array.Empty<object>()) != 42)
                throw new Exception();
        }
    }

    class TestDynamicDependency
    {
        class TypeWithPublicMethodsKept
        {
            public int Method1() => throw null;
            protected int Method2() => throw null;
        }

        class TypeWithAllMethodsKept
        {
            public int Method1() => throw null;
            protected int Method2() => throw null;
        }

        class TypeWithSpecificMethodKept
        {
            public int Method1() => throw null;
            public int Method2() => throw null;
            public int Method3() => throw null;
        }

        class TypeWithSpecificOverloadKept
        {
            public int Method(int x, int y) => throw null;
            public int Method(int x, char y) => throw null;
        }

        class TypeWithAllOverloadsKept
        {
            public int Method(int x, int y) => throw null;
            public int Method(int x, char y) => throw null;
        }

        class TypeWithPublicPropertiesKept
        {
            public int Property1 { get; set; }
            private int Property2 { get; set; }
        }

        public static void DependentMethod() => throw null;
        public static void UnreachedMethod() => throw null;

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(TypeWithPublicMethodsKept))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(TypeWithAllMethodsKept))]
        [DynamicDependency("Method2", typeof(TypeWithSpecificMethodKept))]
        [DynamicDependency("Method(System.Int32,System.Int32)", typeof(TypeWithSpecificOverloadKept))]
        [DynamicDependency("Method", typeof(TypeWithAllOverloadsKept))]
        [DynamicDependency(nameof(DependentMethod))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(TypeWithPublicPropertiesKept))]
        public static void Run()
        {
            Assert.Equal(1, typeof(TypeWithPublicMethodsKept).CountMethods());
            Assert.Equal(2, typeof(TypeWithAllMethodsKept).CountMethods());
            Assert.Equal(1, typeof(TypeWithSpecificMethodKept).CountMethods());
            Assert.Equal(1, typeof(TypeWithSpecificOverloadKept).CountMethods());
            Assert.Equal(2, typeof(TypeWithAllOverloadsKept).CountMethods());

            // We only expect DependentMethod. We specifically don't expect to see the Run method (current method).
            Assert.Equal(1, typeof(TestDynamicDependency).CountMethods());

            Assert.Equal(1, typeof(TypeWithPublicPropertiesKept).CountProperties());
        }
    }

    class TestDynamicDependencyWithGenerics
    {
        class TypeWithPublicMethodsKept<T>
        {
            public int Method1() => throw null;
            protected int Method2() => throw null;
        }

        class TypeWithAllMethodsKept<T>
        {
            public int Method1() => throw null;
            protected int Method2() => throw null;
        }

        class TypeWithSpecificMethodKept<T>
        {
            public int Method1() => throw null;
            public int Method2() => throw null;
            public int Method3() => throw null;
        }

        class TypeWithSpecificOverloadKept<T>
        {
            public int Method(int x, int y) => throw null;
            public int Method(int x, char y) => throw null;
        }

        class TypeWithAllOverloadsKept<T>
        {
            public int Method(int x, int y) => throw null;
            public int Method(int x, char y) => throw null;
        }

        class TypeWithPublicPropertiesKept<T>
        {
            public int Property1 { get; set; }
            private int Property2 { get; set; }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(TypeWithPublicMethodsKept<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(TypeWithAllMethodsKept<>))]
        [DynamicDependency("Method2", typeof(TypeWithSpecificMethodKept<>))]
        [DynamicDependency("Method(System.Int32,System.Int32)", typeof(TypeWithSpecificOverloadKept<>))]
        [DynamicDependency("Method", typeof(TypeWithAllOverloadsKept<>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(TypeWithPublicPropertiesKept<>))]
        public static void Run()
        {
            Assert.Equal(1, typeof(TypeWithPublicMethodsKept<>).CountMethods());
            Assert.Equal(2, typeof(TypeWithAllMethodsKept<>).CountMethods());
            Assert.Equal(1, typeof(TypeWithSpecificMethodKept<>).CountMethods());
            Assert.Equal(1, typeof(TypeWithSpecificOverloadKept<>).CountMethods());
            Assert.Equal(2, typeof(TypeWithAllOverloadsKept<>).CountMethods());
            Assert.Equal(1, typeof(TypeWithPublicPropertiesKept<>).CountProperties());
        }
    }

    class TestObjectGetTypeDataflow
    {
        static TypeWithNonPublicMethodsKept s_typeWithNonpublicMethodsKept = new TypeWithNonPublicMethodsKeptThroughBase();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
        class TypeWithNonPublicMethodsKept
        {
            private void Method1() { }
            public void Method2() { }
            private void Method3() { }
        }

        class TypeWithNonPublicMethodsKeptThroughBase : TypeWithNonPublicMethodsKept
        {
            private void Method4() { }
            public void Method5() { }
        }

        static NeverAllocatedTypeAskingForNonPublicMethods s_neverAllocatedTypeAskingForNonPublicMethods = null;

        class NeverAllocatedTypeAskingForNonPublicMethods : TypeWithNonPublicMethodsKept
        {
            private void Method4() { }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
        interface IInterfaceWithNonPublicMethodsKept
        {
            public static void Method1() { }
            private static void Method2() { }
        }

        interface IInterfaceWithNonPublicMethodsKeptIndirectly : IInterfaceWithNonPublicMethodsKept
        {
            public static void Method3() { }
            private static void Method4() { }
        }

        static IInterfaceWithNonPublicMethodsKeptIndirectly s_interfaceWithNonPublicMethodsKeptIndirectly = new TypeWithNonPublicMethodsKeptThroughIndirectInterface();

        class TypeWithNonPublicMethodsKeptThroughIndirectInterface : IInterfaceWithNonPublicMethodsKeptIndirectly
        {
            private void Method1() { }
            public void Method2() { }
        }

        class TypeWithNonPublicMethodsKeptThroughIndirectInterfaceNeverAllocated : IInterfaceWithNonPublicMethodsKept
        {
            private void Method1() { }
            public void Method2() { }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        interface IKeepNonPublicCtors { }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        interface IKeepPublicMethods { }

        static BaseWithMixKept s_baseWithMixKept = new DerivedWithMixKept(123);

        class BaseWithMixKept : IKeepNonPublicCtors, IKeepPublicMethods { }
        class DerivedWithMixKept : BaseWithMixKept
        {
            public DerivedWithMixKept(int x) { }
            public DerivedWithMixKept(double x) { }
            private DerivedWithMixKept(string y) { }
            public void PublicMethod() { }
            private void PrivateMethod() { }
        }

        public static void Run()
        {
            Assert.Equal(1, s_typeWithNonpublicMethodsKept.GetType().CountMethods());
            Assert.Equal(0, s_typeWithNonpublicMethodsKept.GetType().CountPublicMethods());

            Assert.Equal(2, s_typeWithNonpublicMethodsKept.GetType().BaseType.CountMethods());
            Assert.Equal(0, s_typeWithNonpublicMethodsKept.GetType().BaseType.CountPublicMethods());

            if (s_neverAllocatedTypeAskingForNonPublicMethods != null)
            {
                // This should never be reached, but we need to trick analysis into seeing this
                // so that we can tests this doesn't lead to keeping the type.
                Assert.Equal(666, s_neverAllocatedTypeAskingForNonPublicMethods.GetType().CountMethods());
            }
            // This is not great - the GetType() call above "wished" NeverAllocatedTypeAskingForNonPublicMethods
            // into existence, but it shouldn't have. We could do better here if this is a problem.
            // If we do that, change this .NotNull to .Null.
            Assert.NotNull(typeof(TestObjectGetTypeDataflow).GetNestedTypeSecretly(nameof(NeverAllocatedTypeAskingForNonPublicMethods)));
            // Sanity check
            Assert.NotNull(typeof(TestObjectGetTypeDataflow).GetNestedTypeSecretly(nameof(TypeWithNonPublicMethodsKept)));

            // DAM doesn't apply to the interface since it only goes into effect with an object.GetType call.
            Assert.Equal(0, typeof(IInterfaceWithNonPublicMethodsKept).CountMethods());
            Assert.Equal(0, typeof(IInterfaceWithNonPublicMethodsKeptIndirectly).CountMethods());

            Assert.Equal(1, s_interfaceWithNonPublicMethodsKeptIndirectly.GetType().CountMethods());
            Assert.Equal(0, s_interfaceWithNonPublicMethodsKeptIndirectly.GetType().CountPublicMethods());

            // Typeof shouldn't make the DAM annotation on the type to kick in.
            Assert.Equal(0, typeof(TypeWithNonPublicMethodsKeptThroughIndirectInterfaceNeverAllocated).CountMethods());
            Assert.Equal(0, typeof(TypeWithNonPublicMethodsKeptThroughIndirectInterfaceNeverAllocated).CountPublicMethods());

            Assert.Equal(1, s_baseWithMixKept.GetType().CountMethods());
            Assert.Equal(1, s_baseWithMixKept.GetType().CountPublicMethods());
            Assert.Equal(2, s_baseWithMixKept.GetType().CountPublicConstructors());
            Assert.Equal(2, s_baseWithMixKept.GetType().CountConstructors());
        }
    }

    class TestMakeGenericDataflow
    {
        class Gen1<T, U>
        {
            public static void Bridge() { }
        }

        class Gen1
        {
            public static void Bridge<T, U>() { }
        }


        class Gen2<T>
        {
            public static void Bridge() { }
        }

        class Gen2
        {
            public static void Bridge<T>() { }
        }

        struct MyStruct<T> { }

        static void DoBridgeT1<T, U>() => typeof(Gen1<,>).MakeGenericType([typeof(T), typeof(U)]).GetMethod(nameof(Gen1<T, U>.Bridge)).Invoke(null, []);

        static void DoBridgeT2<T>() => typeof(Gen2<>).MakeGenericType([typeof(MyStruct<T>)]).GetMethod(nameof(Gen2<T>.Bridge)).Invoke(null, []);

        static void DoBridgeM1<T, U>() => typeof(Gen1).GetMethod(nameof(Gen1.Bridge)).MakeGenericMethod([typeof(T), typeof(U)]).Invoke(null, []);

        static void DoBridgeM2<T>() => typeof(Gen2).GetMethod(nameof(Gen2.Bridge)).MakeGenericMethod([typeof(MyStruct<T>)]).Invoke(null, []);

        public static void Run()
        {
            DoBridgeT1<string, string>();
            DoBridgeT1<string, int>();
            DoBridgeT1<int, int>();

            DoBridgeT2<string>();
            DoBridgeT2<int>();

            DoBridgeM1<string, string>();
            DoBridgeM1<string, int>();
            DoBridgeM1<int, int>();

            DoBridgeM2<string>();
            DoBridgeM2<int>();

            typeof(Gen1<,>).MakeGenericType([typeof(float), typeof(string)]).GetMethod(nameof(Gen1<float, string>.Bridge)).Invoke(null, []);
            typeof(Gen2<>).MakeGenericType([typeof(MyStruct<float>)]).GetMethod(nameof(Gen2<float>.Bridge)).Invoke(null, []);
            typeof(Gen1).GetMethod(nameof(Gen1.Bridge)).MakeGenericMethod([typeof(float), typeof(string)]).Invoke(null, []);
            typeof(Gen2).GetMethod(nameof(Gen2.Bridge)).MakeGenericMethod([typeof(MyStruct<float>)]).Invoke(null, []);
        }
    }

    class TestMakeGenericDataflowInvalid
    {
        class Gen<T> { }

        class Gen
        {
            public static void Bridge<T>() { }
        }

        public static void Run()
        {
            try
            {
                typeof(Gen<>).MakeGenericType(null);
            }
            catch (ArgumentException) { }

            try
            {
                typeof(Gen<>).MakeGenericType([]);
            }
            catch (ArgumentException) { }

            try
            {
                typeof(Gen<>).MakeGenericType([typeof(float), typeof(double)]);
            }
            catch (ArgumentException) { }

            try
            {
                typeof(Gen<>).MakeGenericType([typeof(Gen<>)]);
            }
            catch (ArgumentException) { }

            try
            {
                typeof(Gen).GetMethod("Bridge").MakeGenericMethod(null);
            }
            catch (ArgumentException) { }

            try
            {
                typeof(Gen).GetMethod("Bridge").MakeGenericMethod([]);
            }
            catch (ArgumentException) { }

            try
            {
                typeof(Gen).GetMethod("Bridge").MakeGenericMethod([typeof(float), typeof(double)]);
            }
            catch (ArgumentException) { }
        }
    }

    class TestMarshalIntrinsics
    {
        [StructLayout(LayoutKind.Sequential)]
        class ClassWithLayout1 { public int Field; }
        [StructLayout(LayoutKind.Sequential)]
        class ClassWithLayout2 { public int Field; }
        [StructLayout(LayoutKind.Sequential)]
        class ClassWithLayout3 { public int Field; }
        [StructLayout(LayoutKind.Sequential)]
        class ClassWithLayout4 { public int Field; }
        [StructLayout(LayoutKind.Sequential)]
        class ClassWithLayout5 { public int Field; }

        static Type s_secretType = typeof(ClassWithLayout5);

        public static void Run()
        {
            // Check we detect these as intrinsics
            IntPtr pBytes = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ClassWithLayout1)));
            object o = Marshal.PtrToStructure(pBytes, typeof(ClassWithLayout2));
            Marshal.DestroyStructure(pBytes, typeof(ClassWithLayout3));
            Marshal.OffsetOf(typeof(ClassWithLayout4), "Field");

            SanityTest();

            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:UnrecognizedAotPattern",
                Justification = "That's the point")]
            static void SanityTest()
            {
                // Sanity check that intrinsic detection is necessary
                bool thrown = false;
                try
                {
                    Marshal.OffsetOf(s_secretType, "Field");
                }
                catch (Exception)
                {
                    thrown = true;
                }

                if (!thrown)
                    throw new Exception();
            }
        }
    }

    class Regression97758
    {
        class Foo<T>
        {
            public static void Trigger()
            {
                typeof(Bar).GetConstructor([]).Invoke([]);

                if (typeof(T).IsValueType && (object)default(T) == null)
                {
                    if (!RuntimeFeature.IsDynamicCodeCompiled)
                        return;

                    Unreachable();
                }

                static void Unreachable() { }
            }
        }

        class Bar { }

        public static void Run()
        {
            Foo<int>.Trigger();
        }
    }
}

static class Assert
{
    public static void Equal(int expected, int actual)
    {
        if (expected != actual)
            throw new Exception($"{expected} != {actual}");
    }

    public static void NotNull(object o)
    {
        if (o is null)
            throw new Exception();
    }

    public static void Null(object o)
    {
        if (o is not null)
            throw new Exception();
    }
}

static class Helpers
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static int CountConstructors(this Type t)
        => t.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length;
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static int CountPublicConstructors(this Type t)
        => t.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).Length;
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static int CountMethods(this Type t)
        => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Length;
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static int CountPublicMethods(this Type t)
        => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).Length;
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static int CountFields(this Type t)
        => t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Length;
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static int CountProperties(this Type t)
        => t.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Length;
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static int CountInterfaces(this Type t)
        => t.GetInterfaces().Length;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    public static Type GetNestedTypeSecretly(this Type t, string name)
        => t.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
}
