// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class Generics
{
    internal static int Run()
    {
        TestDictionaryDependencyTracking.Run();
        TestStaticBaseLookups.Run();
        TestInitThisClass.Run();
        TestDelegateFatFunctionPointers.Run();
        TestDelegateToCanonMethods.Run();
        TestVirtualMethodUseTracking.Run();
        TestSlotsInHierarchy.Run();
        TestDelegateVirtualMethod.Run();
        TestDelegateInterfaceMethod.Run();
        TestThreadStaticFieldAccess.Run();
        TestConstrainedMethodCalls.Run();
        TestInstantiatingUnboxingStubs.Run();
        TestNameManglingCollisionRegression.Run();
        TestSimpleGVMScenarios.Run();
        TestGvmDelegates.Run();
        TestGvmDependencies.Run();
        TestGvmLookups.Run();
        TestInterfaceVTableTracking.Run();
        TestClassVTableTracking.Run();
        TestReflectionInvoke.Run();
        TestFieldAccess.Run();
        TestDevirtualization.Run();
        TestImportFailure.Run();
        TestGenericInlining.Run();
        TestGenericInliningDoesntHappen.Run();
        TestGvmDependenciesFromLazy.Run();
        TestGvmDependencyFromGenericLazy.Run();
        TestConstrainedGvmCalls.Run();
        TestConstrainedGvmValueTypeCalls.Run();
        TestDefaultGenericVirtualInterfaceMethods.Run();
        TestSimpleGenericRecursion.Run();
        TestGenericRecursionFromNpgsql.Run();
        TestRecursionInGenericVirtualMethods.Run();
        TestRecursionThroughGenericLookups.Run();
        TestGvmLookupDependency.Run();
        TestInvokeMemberCornerCaseInGenerics.Run();
#if !CODEGEN_CPP
        TestNullableCasting.Run();
        TestVariantCasting.Run();
        TestMDArrayAddressMethod.Run();
        TestNativeLayoutGeneration.Run();
        TestByRefLikeVTables.Run();
#endif
        return 100;
    }

    /// <summary>
    /// Tests that we properly track dictionary dependencies of generic methods.
    /// (Getting this wrong is a linker failure.)
    /// </summary>
    class TestDictionaryDependencyTracking
    {
        static object Gen1<T>()
        {
            return MakeArray<ClassGen<T>>();
        }

        static object MakeArray<T>()
        {
            return new T[0];
        }

        class Gen<T>
        {
            public object Frob()
            {
                return new ValueGen<T[]>();
            }

            public object Futz()
            {
                return Gen1<ValueGen<T>>();
            }
        }

        struct ValueGen<T>
        {
        }

        class ClassGen<T>
        {
        }

        public static void Run()
        {
            new Gen<string>().Frob();
            new Gen<object>().Futz();
        }
    }

    /// <summary>
    /// Tests static base access.
    /// </summary>
    class TestStaticBaseLookups
    {
        class C1 { }
        class C2 { }
        class C3 { }

        class GenHolder<T>
        {
            public static int IntField;
            public static string StringField;
        }

        class GenAccessor<T>
        {
            public static string Read()
            {
                return GenHolder<T>.IntField.ToString() + GenHolder<T>.StringField;
            }

            public static void SetSimple(int i, string s)
            {
                GenHolder<T>.IntField = i;
                GenHolder<T>.StringField = s;
            }

            public static void SetComplex<U>(int i, string s)
            {
                GenHolder<T>.IntField = i;
                GenHolder<T>.StringField = s;
                GenHolder<U>.IntField = i + 1;
                GenHolder<U>.StringField = s + "`";
            }
        }

        public static void Run()
        {
            GenAccessor<C1>.SetComplex<C2>(42, "Hello");
            GenAccessor<C3>.SetSimple(85, "World");

            if (GenAccessor<C1>.Read() != "42Hello")
                throw new Exception();

            if (GenHolder<C2>.IntField != 43 || GenHolder<C2>.StringField != "Hello`")
                throw new Exception();

            if (GenAccessor<C3>.Read() != "85World")
                throw new Exception();
        }
    }

    /// <summary>
    /// Tests that we can use a delegate that points to a generic method.
    /// </summary>
    class TestDelegateFatFunctionPointers
    {
        struct SmallStruct
        {
            public int X;
        }

        struct MediumStruct
        {
            public int X, Y, Z, W;
        }

        unsafe struct BigStruct
        {
            public const int Length = 128;
            public fixed byte Bytes[Length];
        }

        T Generic<T>(object o) where T : class
        {
            Func<object, T> f = OtherGeneric<T>;
            return f(o);
        }

        T OtherGeneric<T>(object o) where T : class
        {
            return o as T;
        }

        delegate void VoidGenericDelegate<T>(ref T x, T val);
        void VoidGeneric<T>(ref T x, T val)
        {
            x = val;
        }

        SmallStruct SmallStructGeneric<T>(SmallStruct x)
        {
            return x;
        }

        MediumStruct MediumStructGeneric<T>(MediumStruct x)
        {
            return x;
        }

        BigStruct BigStructGeneric<T>(BigStruct x)
        {
            return x;
        }

        public static void Run()
        {
            var o = new TestDelegateFatFunctionPointers();

            string hw = "Hello World";
            string roundtrip = o.Generic<string>(hw);
            if (roundtrip != hw)
                throw new Exception();

            {
                VoidGenericDelegate<object> f = o.VoidGeneric;
                object obj = new object();
                object location = null;
                f(ref location, obj);
                if (location != obj)
                    throw new Exception();
            }

            {
                Func<SmallStruct, SmallStruct> f = o.SmallStructGeneric<object>;
                SmallStruct x = new SmallStruct { X = 12345 };
                SmallStruct result = f(x);
                if (result.X != x.X)
                    throw new Exception();
            }

            {
                Func<MediumStruct, MediumStruct> f = o.MediumStructGeneric<object>;
                MediumStruct x = new MediumStruct { X = 12, Y = 34, Z = 56, W = 78 };
                MediumStruct result = f(x);
                if (result.X != x.X || result.Y != x.Y || result.Z != x.Z || result.W != x.W)
                    throw new Exception();
            }

            unsafe
            {
                Func<BigStruct, BigStruct> f = o.BigStructGeneric<object>;
                BigStruct x = new BigStruct();
                for (int i = 0; i < BigStruct.Length; i++)
                    x.Bytes[i] = (byte)(i * 2);

                BigStruct result = f(x);

                for (int i = 0; i < BigStruct.Length; i++)
                    if (x.Bytes[i] != result.Bytes[i])
                        throw new Exception();
            }
        }
    }

    class TestDelegateToCanonMethods
    {
        class Foo
        {
            public readonly int Value;
            public Foo(int value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        class Bar
        {
            public readonly int Value;
            public Bar(int value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        class FooShared
        {
            public readonly int Value;
            public FooShared(int value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        class BarShared
        {
            public readonly int Value;
            public BarShared(int value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        class GenClass<T>
        {
            public readonly T X;

            public GenClass(T x)
            {
                X = x;
            }

            public string MakeString()
            {
                // Use a constructed type that is not used elsewhere
                return typeof(T[,]).GetElementType().Name + ": " + X.ToString();
            }

            public string MakeGenString<U>()
            {
                // Use a constructed type that is not used elsewhere
                return typeof(T[,,]).GetElementType().Name + ", " +
                    typeof(U[,,,]).GetElementType().Name + ": " + X.ToString();
            }
        }

        struct GenStruct<T>
        {
            public readonly T X;

            public GenStruct(T x)
            {
                X = x;
            }

            public string MakeString()
            {
                // Use a constructed type that is not used elsewhere
                return typeof(T[,]).GetElementType().Name + ": " + X.ToString();
            }

            public string MakeGenString<U>()
            {
                // Use a constructed type that is not used elsewhere
                return typeof(T[,,]).GetElementType().Name + ", " +
                    typeof(U[,,,]).GetElementType().Name + ": " + X.ToString();
            }
        }

        private static void RunReferenceTypeShared<T>(T value)
        {
            // Delegate to a shared nongeneric reference type instance method
            {
                GenClass<T> g = new GenClass<T>(value);
                Func<string> f = g.MakeString;
                if (f() != "FooShared: 42")
                    throw new Exception();
            }

            // Delegate to a shared generic reference type instance method
            {
                GenClass<T> g = new GenClass<T>(value);
                Func<string> f = g.MakeGenString<T>;
                if (f() != "FooShared, FooShared: 42")
                    throw new Exception();
            }
        }

        private static void RunValueTypeShared<T>(T value)
        {
            // Delegate to a shared nongeneric value type instance method
            {
                GenStruct<T> g = new GenStruct<T>(value);
                Func<string> f = g.MakeString;
                if (f() != "BarShared: 42")
                    throw new Exception();
            }

            // Delegate to a shared generic value type instance method
            {
                GenStruct<T> g = new GenStruct<T>(value);
                Func<string> f = g.MakeGenString<T>;
                if (f() != "BarShared, BarShared: 42")
                    throw new Exception();
            }
        }

        public static void Run()
        {
            // Delegate to a shared nongeneric reference type instance method
            {
                GenClass<Foo> g = new GenClass<Foo>(new Foo(42));
                Func<string> f = g.MakeString;
                if (f() != "Foo: 42")
                    throw new Exception();
            }

            // Delegate to a unshared nongeneric reference type instance method
            {
                GenClass<int> g = new GenClass<int>(85);
                Func<string> f = g.MakeString;
                if (f() != "Int32: 85")
                    throw new Exception();
            }

            // Delegate to a shared generic reference type instance method
            {
                GenClass<Foo> g = new GenClass<Foo>(new Foo(42));
                Func<string> f = g.MakeGenString<Foo>;
                if (f() != "Foo, Foo: 42")
                    throw new Exception();
            }

            // Delegate to a unshared generic reference type instance method
            {
                GenClass<int> g = new GenClass<int>(85);
                Func<string> f = g.MakeGenString<int>;
                if (f() != "Int32, Int32: 85")
                    throw new Exception();
            }

            // Delegate to a shared nongeneric value type instance method
            {
                GenStruct<Bar> g = new GenStruct<Bar>(new Bar(42));
                Func<string> f = g.MakeString;
                if (f() != "Bar: 42")
                    throw new Exception();
            }

            // Delegate to a unshared nongeneric value type instance method
            {
                GenStruct<int> g = new GenStruct<int>(85);
                Func<string> f = g.MakeString;
                if (f() != "Int32: 85")
                    throw new Exception();
            }

            // Delegate to a shared generic value type instance method
            {
                GenStruct<Bar> g = new GenStruct<Bar>(new Bar(42));
                Func<string> f = g.MakeGenString<Bar>;
                if (f() != "Bar, Bar: 42")
                    throw new Exception();
            }

            // Delegate to a unshared generic value type instance method
            {
                GenStruct<int> g = new GenStruct<int>(85);
                Func<string> f = g.MakeGenString<int>;
                if (f() != "Int32, Int32: 85")
                    throw new Exception();
            }

            // Now the same from shared code
            RunReferenceTypeShared<FooShared>(new FooShared(42));
            RunValueTypeShared<BarShared>(new BarShared(42));
        }
    }

    class TestDelegateVirtualMethod
    {
        static void Generic<T>()
        {
            Base<T> o = new Derived<T>();
            Func<string> f = o.Do;
            if (f() != "Derived")
                throw new Exception();

            o = new Base<T>();
            f = o.Do;
            if (f() != "Base")
                throw new Exception();
        }

        public static void Run()
        {
            Generic<string>();
        }

        class Base<T>
        {
            public virtual string Do() => "Base";
        }

        class Derived<T> : Base<T>
        {
            public override string Do() => "Derived";
        }
    }

    class TestDelegateInterfaceMethod
    {
        static void Generic<T>()
        {
            IFoo<T> o = new Foo<T>();
            Func<string> f = o.Do;
            if (f() != "Foo")
                throw new Exception();
        }

        public static void Run()
        {
            Generic<string>();
        }

        interface IFoo<T>
        {
            string Do();
        }

        class Foo<T> : IFoo<T>
        {
            public string Do() => "Foo";
        }
    }

    /// <summary>
    /// Tests RyuJIT's initThisClass.
    /// </summary>
    class TestInitThisClass
    {
        class Gen1<T> where T : class
        {
            static string s_str1;
            static string s_str2;

            static Gen1()
            {
                s_str1 = ("Hello" as T) as string;
                s_str2 = ("World" as T) as string;
            }

            public static string Get1()
            {
                return (s_str1 as T) as string;
            }

            public static string Get2<U>()
            {
                return (s_str2 as T) as string;
            }
        }

        class Gen2<T> where T : class
        {
            public static string GetFromClassParam()
            {
                return (Gen1<T>.Get1() as T) as string;
            }

            public static string GetFromMethodParam()
            {
                return (Gen1<T>.Get2<T>() as T) as string;
            }
        }

        class NonGeneric
        {
            public static readonly string Message;

            static NonGeneric()
            {
                Message = "Hi there";
            }

            public static string Get<T>(object o)
            {
                if (o is T[])
                    return Message;
                return null;
            }
        }

        public static void Run()
        {
            if (Gen2<string>.GetFromClassParam() != "Hello")
                throw new Exception();

            if (Gen2<string>.GetFromMethodParam() != "World")
                throw new Exception();

            if (NonGeneric.Get<object>(new object[0]) != "Hi there")
                throw new Exception();
        }
    }

    /// <summary>
    /// Tests that lazily built vtables for canonically equivalent types have the same shape.
    /// </summary>
    class TestVirtualMethodUseTracking
    {
        class C1 { }
        class C2 { }

        class Base1<T> where T : class
        {
            public virtual T As(object o)
            {
                return o as T;
            }
        }

        class Derived1<T> : Base1<T> where T : class
        {
            public T AsToo(object o)
            {
                return o as T;
            }
        }

        class Base2<T>
        {
            public virtual string Method1() => "Base2.Method1";
            public virtual string Method2() => "Base2.Method2";
        }

        class Derived2<T> : Base2<T>
        {
            public override string Method1() => "Derived2.Method1";
            public override string Method2() => "Derived2.Method2";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string TestMethod1FromSharedCode<T>(Base2<T> o) => o.Method1();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string TestMethod2FromSharedCode<T>(Base2<T> o) => o.Method2();

        public static void Run()
        {
            C1 c1 = new C1();
            if (new Derived1<C1>().As(c1) != c1)
                throw new Exception();

            C2 c2 = new C2();
            if (new Derived1<C2>().AsToo(c2) != c2)
                throw new Exception();

            // Also test the stability of the vtables.
            Base2<string> b1 = new Derived2<string>();
            if (b1.Method1() != "Derived2.Method1")
                throw new Exception();
            Base2<object> b2 = new Derived2<object>();
            if (b2.Method2() != "Derived2.Method2")
                throw new Exception();
            if (TestMethod1FromSharedCode(b2) != "Derived2.Method1")
                throw new Exception();
            if (TestMethod1FromSharedCode(b1) != "Derived2.Method1")
                throw new Exception();
            if (TestMethod2FromSharedCode(b2) != "Derived2.Method2")
                throw new Exception();
            if (TestMethod2FromSharedCode(b1) != "Derived2.Method2")
                throw new Exception();
        }
    }

    /// <summary>
    /// Makes sure that during the base slot computation for types such as
    /// Derived&lt;__Canon&gt; (where the base type ends up being Base&lt;__Canon, string&gt;),
    /// the lazy vtable slot computation works.
    /// </summary>
    class TestSlotsInHierarchy
    {
        class Base<T, U>
        {
            public virtual int Do()
            {
                return 42;
            }
        }

        class Derived<T> : Base<T, string> where T : class
        {
            public T Cast(object v)
            {
                return v as T;
            }
        }

        public static void Run()
        {
            var derived = new Derived<string>();
            var derivedAsBase = (Base<string, string>)derived;

            if (derivedAsBase.Do() != 42)
                throw new Exception();

            if (derived.Cast("Hello") != "Hello")
                throw new Exception();
        }
    }

    class TestReflectionInvoke
    {
        static int s_NumErrors = 0;

        struct Foo<T>
        {
            public int Value;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public bool SetAndCheck<U>(int value, U check)
            {
                Value = value;
                return check != null && typeof(T) == typeof(U);
            }
        }

        public interface IFace<T>
        {
            string IFaceMethod1(T t);
            string IFaceGVMethod1<U>(T t, U u);
        }

        public class BaseClass<T> : IFace<T>
        {
            public virtual string Method1(T t) { return "BaseClass.Method1"; }
            public virtual string Method2(T t) { return "BaseClass.Method2"; }
            public virtual string Method3(T t) { return "BaseClass.Method3"; }
            public virtual string Method4(T t) { return "BaseClass.Method4"; }
            public virtual string GVMethod1<U>(T t, U u) { return "BaseClass.GVMethod1"; }
            public virtual string GVMethod2<U>(T t, U u) { return "BaseClass.GVMethod2"; }
            public virtual string GVMethod3<U>(T t, U u) { return "BaseClass.GVMethod3"; }
            public virtual string GVMethod4<U>(T t, U u) { return "BaseClass.GVMethod4"; }

            public virtual string IFaceMethod1(T t) { return "BaseClass.IFaceMethod1"; }
            public virtual string IFaceGVMethod1<U>(T t, U u) { return "BaseClass.IFaceGVMethod1"; }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public virtual string VirtualButNotUsedVirtuallyMethod(T t) { return "BaseClass.VirtualButNotUsedVirtuallyMethod"; }
        }

        public class DerivedClass1<T> : BaseClass<T>, IFace<T>
        {
            public override sealed string Method1(T t) { return "DerivedClass1.Method1"; }
            public override string Method2(T t) { return "DerivedClass1.Method2"; }
            public new virtual string Method3(T t) { return "DerivedClass1.Method3"; }
            public override sealed string GVMethod1<U>(T t, U u) { return "DerivedClass1.GVMethod1"; }
            public override string GVMethod2<U>(T t, U u) { return "DerivedClass1.GVMethod2"; }
            public new virtual string GVMethod3<U>(T t, U u) { return "DerivedClass1.GVMethod3"; }

            public override string IFaceMethod1(T t) { return "DerivedClass1.IFaceMethod1"; }

            public string UseVirtualButNotUsedVirtuallyMethod(T t)
            {
                // Calling through base produces a `call` instead of `callvirt` instruction.
                return base.VirtualButNotUsedVirtuallyMethod(t);
            }
        }

        public class DerivedClass2<T> : DerivedClass1<T>, IFace<T>
        {
            public override string Method3(T t) { return "DerivedClass2.Method3"; }
            public override string Method4(T t) { return "DerivedClass2.Method4"; }
            public override string GVMethod3<U>(T t, U u) { return "DerivedClass2.GVMethod3"; }
            public override string GVMethod4<U>(T t, U u) { return "DerivedClass2.GVMethod4"; }

            string IFace<T>.IFaceMethod1(T t) { return "DerivedClass2.IFaceMethod1"; }
            public override string IFaceGVMethod1<U>(T t, U u) { return "DerivedClass2.IFaceGVMethod1"; }
        }

        private static void Verify<T>(T expected, T actual)
        {
            if (!actual.Equals(expected))
            {
                Console.WriteLine("ACTUAL   : " + actual);
                Console.WriteLine("EXPECTED : " + expected);
                s_NumErrors++;
            }
        }

        public static void Run()
        {
            if (String.Empty.Length > 0)
            {
                // Make sure we compile this method body.
                var tmp = new Foo<string>();
                tmp.SetAndCheck<string>(0, null);
            }

            object o = new Foo<string>();

            {
                MethodInfo mi = typeof(Foo<string>).GetTypeInfo().GetDeclaredMethod("SetAndCheck").MakeGenericMethod(typeof(string));
                if (!(bool)mi.Invoke(o, new object[] { 123, "hello" }))
                    s_NumErrors++;

                var foo = (Foo<string>)o;
                if (foo.Value != 123)
                    s_NumErrors++;

                if ((bool)mi.Invoke(o, new object[] { 123, null }))
                    s_NumErrors++;
            }

            // Uncomment when we have the type loader to buld invoke stub dictionaries.
            {
                MethodInfo mi = typeof(Foo<string>).GetTypeInfo().GetDeclaredMethod("SetAndCheck").MakeGenericMethod(typeof(object));
                if ((bool)mi.Invoke(o, new object[] { 123, new object() }))
                    s_NumErrors++;
            }

            // VirtualInvokeMap testing
            {
                // Rooting some methods to make them reflectable
                new BaseClass<string>().Method1("string");
                new BaseClass<string>().Method2("string");
                new BaseClass<string>().Method3("string");
                new BaseClass<string>().Method4("string");
                new BaseClass<string>().GVMethod1<string>("string", "string2");
                new BaseClass<string>().GVMethod2<string>("string", "string2");
                new BaseClass<string>().GVMethod3<string>("string", "string2");
                new BaseClass<string>().GVMethod4<string>("string", "string2");
                new DerivedClass1<string>().Method1("string");
                new DerivedClass1<string>().Method2("string");
                new DerivedClass1<string>().Method3("string");
                new DerivedClass1<string>().Method4("string");
                new DerivedClass1<string>().GVMethod1<string>("string", "string2");
                new DerivedClass1<string>().GVMethod2<string>("string", "string2");
                new DerivedClass1<string>().GVMethod3<string>("string", "string2");
                new DerivedClass1<string>().GVMethod4<string>("string", "string2");
                new DerivedClass1<string>().UseVirtualButNotUsedVirtuallyMethod("string");
                new DerivedClass2<string>().Method1("string");
                new DerivedClass2<string>().Method2("string");
                new DerivedClass2<string>().Method3("string");
                new DerivedClass2<string>().Method4("string");
                new DerivedClass2<string>().GVMethod1<string>("string", "string2");
                new DerivedClass2<string>().GVMethod2<string>("string", "string2");
                new DerivedClass2<string>().GVMethod3<string>("string", "string2");
                new DerivedClass2<string>().GVMethod4<string>("string", "string2");
                Func<IFace<string>> f = () => new BaseClass<string>(); // Hack to prevent devirtualization
                f().IFaceMethod1("string");
                ((IFace<string>)new BaseClass<string>()).IFaceGVMethod1<string>("string1", "string2");

                MethodInfo m1 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("Method1");
                MethodInfo m2 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("Method2");
                MethodInfo m3 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("Method3");
                MethodInfo m4 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("Method4");
                MethodInfo unusedMethod = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("VirtualButNotUsedVirtuallyMethod");
                MethodInfo gvm1 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("GVMethod1").MakeGenericMethod(typeof(string));
                MethodInfo gvm2 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("GVMethod2").MakeGenericMethod(typeof(string));
                MethodInfo gvm3 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("GVMethod3").MakeGenericMethod(typeof(string));
                MethodInfo gvm4 = typeof(BaseClass<string>).GetTypeInfo().GetDeclaredMethod("GVMethod4").MakeGenericMethod(typeof(string));
                Verify("BaseClass.Method1", m1.Invoke(new BaseClass<string>(), new[] { "" }));
                Verify("BaseClass.Method2", m2.Invoke(new BaseClass<string>(), new[] { "" }));
                Verify("BaseClass.Method3", m3.Invoke(new BaseClass<string>(), new[] { "" }));
                Verify("BaseClass.Method4", m4.Invoke(new BaseClass<string>(), new[] { "" }));
                Verify("BaseClass.VirtualButNotUsedVirtuallyMethod", unusedMethod.Invoke(new BaseClass<string>(), new[] { "" }));
                Verify("DerivedClass1.Method1", m1.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("DerivedClass1.Method2", m2.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("BaseClass.Method3", m3.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("BaseClass.Method4", m4.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("DerivedClass1.Method1", m1.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass1.Method2", m2.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("BaseClass.Method3", m3.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass2.Method4", m4.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("BaseClass.GVMethod1", gvm1.Invoke(new BaseClass<string>(), new[] { "", "" }));
                Verify("BaseClass.GVMethod2", gvm2.Invoke(new BaseClass<string>(), new[] { "", "" }));
                Verify("BaseClass.GVMethod3", gvm3.Invoke(new BaseClass<string>(), new[] { "", "" }));
                Verify("BaseClass.GVMethod4", gvm4.Invoke(new BaseClass<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod1", gvm1.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod2", gvm2.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("BaseClass.GVMethod3", gvm3.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("BaseClass.GVMethod4", gvm4.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod1", gvm1.Invoke(new DerivedClass2<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod2", gvm2.Invoke(new DerivedClass2<string>(), new[] { "", "" }));
                Verify("BaseClass.GVMethod3", gvm3.Invoke(new DerivedClass2<string>(), new[] { "", "" }));
                Verify("DerivedClass2.GVMethod4", gvm4.Invoke(new DerivedClass2<string>(), new[] { "", "" }));

                m1 = typeof(DerivedClass1<string>).GetTypeInfo().GetDeclaredMethod("Method1");
                m2 = typeof(DerivedClass1<string>).GetTypeInfo().GetDeclaredMethod("Method2");
                m3 = typeof(DerivedClass1<string>).GetTypeInfo().GetDeclaredMethod("Method3");
                gvm1 = typeof(DerivedClass1<string>).GetTypeInfo().GetDeclaredMethod("GVMethod1").MakeGenericMethod(typeof(string));
                gvm2 = typeof(DerivedClass1<string>).GetTypeInfo().GetDeclaredMethod("GVMethod2").MakeGenericMethod(typeof(string));
                gvm3 = typeof(DerivedClass1<string>).GetTypeInfo().GetDeclaredMethod("GVMethod3").MakeGenericMethod(typeof(string));
                Verify("DerivedClass1.Method1", m1.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("DerivedClass1.Method2", m2.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("DerivedClass1.Method3", m3.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("DerivedClass1.Method1", m1.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass1.Method2", m2.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass2.Method3", m3.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass1.GVMethod1", gvm1.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod2", gvm2.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod3", gvm3.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod1", gvm1.Invoke(new DerivedClass2<string>(), new[] { "", "" }));
                Verify("DerivedClass1.GVMethod2", gvm2.Invoke(new DerivedClass2<string>(), new[] { "", "" }));
                Verify("DerivedClass2.GVMethod3", gvm3.Invoke(new DerivedClass2<string>(), new[] { "", "" }));

                m3 = typeof(DerivedClass2<string>).GetTypeInfo().GetDeclaredMethod("Method3");
                m4 = typeof(DerivedClass2<string>).GetTypeInfo().GetDeclaredMethod("Method4");
                gvm3 = typeof(DerivedClass2<string>).GetTypeInfo().GetDeclaredMethod("GVMethod3").MakeGenericMethod(typeof(string));
                gvm4 = typeof(DerivedClass2<string>).GetTypeInfo().GetDeclaredMethod("GVMethod4").MakeGenericMethod(typeof(string));
                Verify("DerivedClass2.Method3", m3.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass2.Method4", m4.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass2.GVMethod3", gvm3.Invoke(new DerivedClass2<string>(), new[] { "", "" }));
                Verify("DerivedClass2.GVMethod4", gvm4.Invoke(new DerivedClass2<string>(), new[] { "", "" }));

                // BaseClass<int>.Method1 has the same slot as BaseClass<float>.Method3 on CoreRT, because vtable entries
                // get populated on demand (the first type won't get a Method3 entry, and the latter won't get a Method1 entry)
                // On ProjectN, both types will get vtable entries for both methods.
                new BaseClass<int>().Method1(1);
                m1 = typeof(BaseClass<int>).GetTypeInfo().GetDeclaredMethod("Method1");
                Verify("BaseClass.Method1", m1.Invoke(new BaseClass<int>(), new object[] { (int)1 }));
                Verify("DerivedClass1.Method1", m1.Invoke(new DerivedClass1<int>(), new object[] { (int)1 }));
                Verify("DerivedClass1.Method1", m1.Invoke(new DerivedClass2<int>(), new object[] { (int)1 }));

                new BaseClass<float>().Method3(1);
                m3 = typeof(BaseClass<float>).GetTypeInfo().GetDeclaredMethod("Method3");
                Verify("BaseClass.Method3", m3.Invoke(new BaseClass<float>(), new object[] { 1.1f }));
                Verify("BaseClass.Method3", m3.Invoke(new DerivedClass1<float>(), new object[] { 1.1f }));
                Verify("BaseClass.Method3", m3.Invoke(new DerivedClass2<float>(), new object[] { 1.1f }));

                m1 = typeof(IFace<string>).GetTypeInfo().GetDeclaredMethod("IFaceMethod1");
                gvm1 = typeof(IFace<string>).GetTypeInfo().GetDeclaredMethod("IFaceGVMethod1").MakeGenericMethod(typeof(string));
                Verify("BaseClass.IFaceMethod1", m1.Invoke(new BaseClass<string>(), new[] { "" }));
                Verify("BaseClass.IFaceGVMethod1", gvm1.Invoke(new BaseClass<string>(), new[] { "", "" }));
                Verify("DerivedClass1.IFaceMethod1", m1.Invoke(new DerivedClass1<string>(), new[] { "" }));
                Verify("BaseClass.IFaceGVMethod1", gvm1.Invoke(new DerivedClass1<string>(), new[] { "", "" }));
                Verify("DerivedClass2.IFaceMethod1", m1.Invoke(new DerivedClass2<string>(), new[] { "" }));
                Verify("DerivedClass2.IFaceGVMethod1", gvm1.Invoke(new DerivedClass2<string>(), new[] { "", "" }));
            }

            if (s_NumErrors != 0)
                throw new Exception();
        }
    }

    class TestThreadStaticFieldAccess
    {
        class TypeWithThreadStaticField<T>
        {
            [ThreadStatic]
            public static int X;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static int Read()
            {
                return X;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Write(int x)
            {
                X = x;
            }
        }

        class BeforeFieldInitType<T>
        {
            [ThreadStatic]
            public static int X = 1985;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadFromBeforeFieldInitType<T>()
        {
            return BeforeFieldInitType<T>.X;
        }

        public static void Run()
        {
            // This will set the field to a value from non-shared code
            TypeWithThreadStaticField<object>.X = 42;

            // Now read the value from shared code
            if (TypeWithThreadStaticField<object>.Read() != 42)
                throw new Exception();

            // Set the value from shared code
            TypeWithThreadStaticField<string>.Write(112);

            // Now read the value from non-shared code
            if (TypeWithThreadStaticField<string>.X != 112)
                throw new Exception();

            // Check that the storage locations for string and object instantiations differ
            if (TypeWithThreadStaticField<object>.Read() != 42)
                throw new Exception();

            // Make sure we run the cctor
            if (ReadFromBeforeFieldInitType<object>() != 1985)
                throw new Exception();
        }
    }

    class TestConstrainedMethodCalls
    {
        class Atom1 { }
        class Atom2 { }

        interface IFoo<T>
        {
            bool Frob(object o);
        }

        struct Foo<T> : IFoo<T>
        {
            public int FrobbedValue;

            public bool Frob(object o)
            {
                FrobbedValue = 12345;
                return o is T[,,];
            }
        }

        struct MyDisposableStruct<T> : IDisposable
        {
            public static Type TypeSeenWhenDisposed { get; private set; }
            void IDisposable.Dispose() => TypeSeenWhenDisposed = typeof(T);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool DoFrob<T, U>(ref T t, object o) where T : IFoo<U>
        {
            // Perform a constrained interface call from shared code.
            // This should have been resolved to a direct call at compile time.
            return t.Frob(o);
        }

        public static void Run()
        {
            var foo1 = new Foo<Atom1>();
            bool result = DoFrob<Foo<Atom1>, Atom1>(ref foo1, new Atom1[0, 0, 0]);

            // If the FrobbedValue doesn't change when we frob, we must have done box+interface call.
            if (foo1.FrobbedValue != 12345)
                throw new Exception();

            // Also check we passed the right generic context to Foo.Frob
            if (!result)
                throw new Exception();

            // Also check dependency analysis:
            // EEType for Atom2[,,] that we'll check for was never allocated.
            var foo2 = new Foo<Atom2>();
            if (DoFrob<Foo<Atom2>, Atom2>(ref foo2, new object()))
                throw new Exception();

            using (var myDisposable = new MyDisposableStruct<string>())
            {
                if (MyDisposableStruct<string>.TypeSeenWhenDisposed != null)
                    throw new Exception();
            }
            if (MyDisposableStruct<string>.TypeSeenWhenDisposed != typeof(string))
                throw new Exception();
        }
    }

    class TestInstantiatingUnboxingStubs
    {
        static volatile IFoo s_foo;

        interface IFoo
        {
            bool IsInst(object o);

            void Set(int value);
        }

        struct Foo<T> : IFoo
        {
            public int Value;

            public bool IsInst(object o)
            {
                return o is T;
            }

            public void Set(int value)
            {
                Value = value;
            }
        }

        public static void Run()
        {
            s_foo = new Foo<string>();

            // Make sure the instantiation argument is properly passed
            if (!s_foo.IsInst("ab"))
                throw new Exception();

            if (s_foo.IsInst(new object()))
                throw new Exception();

            // Make sure the byref to 'this' is properly passed
            s_foo.Set(42);

            var foo = (Foo<string>)s_foo;
            if (foo.Value != 42)
                throw new Exception();
        }
    }

    class TestMDArrayAddressMethod
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PassByRef(ref object x)
        {
            x = new Object();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoGen<T>(object[,] arr)
        {
            // Here, the array type is known statically at the time of compilation
            PassByRef(ref arr[0, 0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PassByRef2<T>(ref T x)
        {
            x = default(T);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoGen2<T>(T[,] arr)
        {
            // Here, the array type needs to be looked up from the dictionary
            PassByRef2<T>(ref arr[0, 0]);
        }

        public static void Run()
        {
            int exceptionsSeen = 0;

            try
            {
                DoGen<object>(new string[1, 1]);
            }
            catch (ArrayTypeMismatchException)
            {
                exceptionsSeen++;
            }

            DoGen<object>(new object[1, 1]);

            try
            {
                DoGen2<object>(new string[1, 1]);
            }
            catch (ArrayTypeMismatchException)
            {
                exceptionsSeen++;
            }

            DoGen2<object>(new object[1, 1]);

            if (exceptionsSeen != 2)
                throw new Exception();
        }
    }

    //
    // Regression test for issue https://github.com/dotnet/corert/issues/1964
    //
    class TestNameManglingCollisionRegression
    {
        class Gen1<T>
        {
            public Gen1(T t) { }
        }

        public static void Run()
        {
            Gen1<object[]>[] g1 = new Gen1<object[]>[1];
            g1[0] = new Gen1<object[]>(new object[] { new object[1] });

            Gen1<object[][]> g2 = new Gen1<object[][]>(new object[1][]);
        }
    }

    class TestSimpleGVMScenarios
    {
        interface IFoo<out U>
        {
            string IMethod1<T>(T t1, T t2);
        }

        interface ICovariant<out T>
        {
            string ICovariantGVM<U>();
        }

        public interface IBar<T>
        {
            U IBarGVMethod<U>(Func<T, U> arg);
        }

        public interface IFace<T>
        {
            string IFaceGVMethod1<U>(T t, U u);
        }

        class Base : IFoo<string>, IFoo<int>
        {
            public virtual string GMethod1<T>(T t1, T t2) { return "Base.GMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
            public virtual string IMethod1<T>(T t1, T t2) { return "Base.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }
        class Derived : Base, IFoo<string>, IFoo<int>
        {
            public override string GMethod1<T>(T t1, T t2) { return "Derived.GMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
            string IFoo<string>.IMethod1<T>(T t1, T t2) { return "Derived.IFoo<string>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }
        class SuperDerived : Derived, IFoo<string>, IFoo<int>
        {
            string IFoo<int>.IMethod1<T>(T t1, T t2) { return "SuperDerived.IFoo<int>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }


        class GenBase<A> : IFoo<string>, IFoo<int>
        {
            public virtual string GMethod1<T>(T t1, T t2) { return "GenBase<" + typeof(A) + ">.GMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
            public virtual string IMethod1<T>(T t1, T t2) { return "GenBase<" + typeof(A) + ">.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }
        class GenDerived<A> : GenBase<A>, IFoo<string>, IFoo<int>
        {
            public override string GMethod1<T>(T t1, T t2) { return "GenDerived<" + typeof(A) + ">.GMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
            string IFoo<string>.IMethod1<T>(T t1, T t2) { return "GenDerived<" + typeof(A) + ">.IFoo<string>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }
        class GenSuperDerived<A> : GenDerived<A>, IFoo<string>, IFoo<int>
        {
            string IFoo<int>.IMethod1<T>(T t1, T t2) { return "GenSuperDerived<" + typeof(A) + ">.IFoo<int>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }

        struct MyStruct1 : IFoo<string>, IFoo<int>
        {
            string IFoo<string>.IMethod1<T>(T t1, T t2) { return "MyStruct1.IFoo<string>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
            string IFoo<int>.IMethod1<T>(T t1, T t2) { return "MyStruct1.IFoo<int>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }
        struct MyStruct2 : IFoo<string>, IFoo<int>
        {
            string IFoo<string>.IMethod1<T>(T t1, T t2) { return "MyStruct2.IFoo<string>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
            public string IMethod1<T>(T t1, T t2) { return "MyStruct2.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }
        struct MyStruct3 : IFoo<string>, IFoo<int>
        {
            string IFoo<int>.IMethod1<T>(T t1, T t2) { return "MyStruct3.IFoo<int>.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
            public string IMethod1<T>(T t1, T t2) { return "MyStruct3.IMethod1<" + typeof(T) + ">(" + t1 + "," + t2 + ")"; }
        }

        public class AnotherBaseClass<T>
        {
            public virtual string IFaceMethod1(T t) { return "AnotherBaseClass.IFaceMethod1"; }
            public virtual string IFaceGVMethod1<U>(T t, U u) { return "AnotherBaseClass.IFaceGVMethod1"; }
        }

        public class AnotherDerivedClass<T> : AnotherBaseClass<T>, IFace<T>
        {
        }

        public class BarImplementor : IBar<int>
        {
            public virtual U IBarGVMethod<U>(Func<int, U> arg) { return arg(123); }
        }

        public class Yahoo<T>
        {
            public virtual U YahooGVM<U>(Func<T, U> arg) { return default(U); }
        }

        public class YahooDerived : Yahoo<int>
        {
            public override U YahooGVM<U>(Func<int, U> arg) { return arg(456); }
        }

        public class Covariant<T> : ICovariant<T>
        {
            public string ICovariantGVM<U>() { return String.Format("Covariant<{0}>.ICovariantGVM<{1}>", typeof(T).Name, typeof(U).Name); }
        }

        static string s_GMethod1;
        static string s_IFooString;
        static string s_IFooObject;
        static string s_IFooInt;

        static int s_NumErrors = 0;

        private static void TestWithStruct(IFoo<string> ifooStr, IFoo<object> ifooObj, IFoo<int> ifooInt)
        {
            var res = ifooStr.IMethod1<int>(1, 2);
            WriteLineWithVerification(res, s_IFooString);

            res = ifooObj.IMethod1<int>(3, 4);
            WriteLineWithVerification(res, s_IFooObject);

            res = ifooInt.IMethod1<int>(5, 6);
            WriteLineWithVerification(res, s_IFooInt);
        }

        private static void TestWithClass(object o)
        {
            Base b = o as Base;
            var res = b.GMethod1<int>(1, 2);
            WriteLineWithVerification(res, s_GMethod1);

            IFoo<string> ifoo1 = o as IFoo<string>;
            res = ifoo1.IMethod1<int>(3, 4);
            WriteLineWithVerification(res, s_IFooString);

            IFoo<object> ifoo2 = o as IFoo<object>;
            res = ifoo2.IMethod1<int>(5, 6);
            WriteLineWithVerification(res, s_IFooObject);

            IFoo<int> ifoo3 = o as IFoo<int>;
            res = ifoo3.IMethod1<int>(7, 8);
            WriteLineWithVerification(res, s_IFooInt);
        }

        private static void TestWithGenClass<T>(object o)
        {
            GenBase<T> b = o as GenBase<T>;
            var res = b.GMethod1<int>(1, 2);
            WriteLineWithVerification(res, s_GMethod1);

            IFoo<string> ifoo1 = o as IFoo<string>;
            res = ifoo1.IMethod1<int>(3, 4);
            WriteLineWithVerification(res, s_IFooString);

            IFoo<object> ifoo2 = o as IFoo<object>;
            res = ifoo2.IMethod1<int>(5, 6);
            WriteLineWithVerification(res, s_IFooObject);

            IFoo<int> ifoo3 = o as IFoo<int>;
            res = ifoo3.IMethod1<int>(7, 8);
            WriteLineWithVerification(res, s_IFooInt);
        }

        private static void WriteLineWithVerification(string actual, string expected)
        {
            if (actual != expected)
            {
                Console.WriteLine("ACTUAL   : " + actual);
                Console.WriteLine("EXPECTED : " + expected);
                s_NumErrors++;
            }
            else
            {
                Console.WriteLine(actual);
            }
        }

        public static void Run()
        {
            {
                s_GMethod1 = "Base.GMethod1<System.Int32>(1,2)";
                s_IFooString = "Base.IMethod1<System.Int32>(3,4)";
                s_IFooObject = "Base.IMethod1<System.Int32>(5,6)";
                s_IFooInt = "Base.IMethod1<System.Int32>(7,8)";
                TestWithClass(new Base());
                Console.WriteLine("====================");


                s_GMethod1 = "Derived.GMethod1<System.Int32>(1,2)";
                s_IFooString = "Derived.IFoo<string>.IMethod1<System.Int32>(3,4)";
                s_IFooObject = "Derived.IFoo<string>.IMethod1<System.Int32>(5,6)";
                s_IFooInt = "Base.IMethod1<System.Int32>(7,8)";
                TestWithClass(new Derived());
                Console.WriteLine("====================");


                s_GMethod1 = "Derived.GMethod1<System.Int32>(1,2)";
                s_IFooString = "Derived.IFoo<string>.IMethod1<System.Int32>(3,4)";
                s_IFooObject = "Derived.IFoo<string>.IMethod1<System.Int32>(5,6)";
                s_IFooInt = "SuperDerived.IFoo<int>.IMethod1<System.Int32>(7,8)";
                TestWithClass(new SuperDerived());
                Console.WriteLine("====================");
            }

            {
                s_GMethod1 = "GenBase<System.Byte>.GMethod1<System.Int32>(1,2)";
                s_IFooString = "GenBase<System.Byte>.IMethod1<System.Int32>(3,4)";
                s_IFooObject = "GenBase<System.Byte>.IMethod1<System.Int32>(5,6)";
                s_IFooInt = "GenBase<System.Byte>.IMethod1<System.Int32>(7,8)";
                TestWithGenClass<byte>(new GenBase<byte>());
                Console.WriteLine("====================");


                s_GMethod1 = "GenDerived<System.Byte>.GMethod1<System.Int32>(1,2)";
                s_IFooString = "GenDerived<System.Byte>.IFoo<string>.IMethod1<System.Int32>(3,4)";
                s_IFooObject = "GenDerived<System.Byte>.IFoo<string>.IMethod1<System.Int32>(5,6)";
                s_IFooInt = "GenBase<System.Byte>.IMethod1<System.Int32>(7,8)";
                TestWithGenClass<byte>(new GenDerived<byte>());
                Console.WriteLine("====================");


                s_GMethod1 = "GenDerived<System.String>.GMethod1<System.Int32>(1,2)";
                s_IFooString = "GenDerived<System.String>.IFoo<string>.IMethod1<System.Int32>(3,4)";
                s_IFooObject = "GenDerived<System.String>.IFoo<string>.IMethod1<System.Int32>(5,6)";
                s_IFooInt = "GenBase<System.String>.IMethod1<System.Int32>(7,8)";
                TestWithGenClass<String>(new GenDerived<String>());
                Console.WriteLine("====================");


                s_GMethod1 = "GenDerived<System.Byte>.GMethod1<System.Int32>(1,2)";
                s_IFooString = "GenDerived<System.Byte>.IFoo<string>.IMethod1<System.Int32>(3,4)";
                s_IFooObject = "GenDerived<System.Byte>.IFoo<string>.IMethod1<System.Int32>(5,6)";
                s_IFooInt = "GenSuperDerived<System.Byte>.IFoo<int>.IMethod1<System.Int32>(7,8)";
                TestWithGenClass<byte>(new GenSuperDerived<byte>());
                Console.WriteLine("====================");
            }

            {
                s_IFooString = "MyStruct1.IFoo<string>.IMethod1<System.Int32>(1,2)";
                s_IFooObject = "MyStruct1.IFoo<string>.IMethod1<System.Int32>(3,4)";
                s_IFooInt = "MyStruct1.IFoo<int>.IMethod1<System.Int32>(5,6)";
                TestWithStruct(new MyStruct1(), new MyStruct1(), new MyStruct1());
                Console.WriteLine("====================");


                s_IFooString = "MyStruct2.IFoo<string>.IMethod1<System.Int32>(1,2)";
                s_IFooObject = "MyStruct2.IFoo<string>.IMethod1<System.Int32>(3,4)";
                s_IFooInt = "MyStruct2.IMethod1<System.Int32>(5,6)";
                TestWithStruct(new MyStruct2(), new MyStruct2(), new MyStruct2());
                Console.WriteLine("====================");


                s_IFooString = "MyStruct3.IMethod1<System.Int32>(1,2)";
                s_IFooObject = "MyStruct3.IMethod1<System.Int32>(3,4)";
                s_IFooInt = "MyStruct3.IFoo<int>.IMethod1<System.Int32>(5,6)";
                TestWithStruct(new MyStruct3(), new MyStruct3(), new MyStruct3());
                Console.WriteLine("====================");
            }

            {
                string res = ((IFace<string>)new AnotherDerivedClass<string>()).IFaceGVMethod1<string>("string1", "string2");
                WriteLineWithVerification("AnotherBaseClass.IFaceGVMethod1", res);

                res = ((IBar<int>)new BarImplementor()).IBarGVMethod<string>((i) => "BarImplementor:" + i.ToString());
                WriteLineWithVerification("BarImplementor:123", res);

                Yahoo<int> y = new YahooDerived();
                WriteLineWithVerification("YahooDerived:456", y.YahooGVM<string>((i) => "YahooDerived:" + i.ToString()));

                ICovariant<object> cov = new Covariant<string>();
                WriteLineWithVerification("Covariant<String>.ICovariantGVM<Exception>", cov.ICovariantGVM<Exception>());
            }

            if (s_NumErrors != 0)
                throw new Exception();
        }
    }

    class TestGvmDelegates
    {
        class Atom { }

        interface IFoo
        {
            string Frob<T>(int arg);
        }

        class FooUnshared : IFoo
        {
            public string Frob<T>(int arg)
            {
                return typeof(T[,]).GetElementType().Name + arg.ToString();
            }
        }

        class FooShared : IFoo
        {
            public string Frob<T>(int arg)
            {
                return typeof(T[,,]).GetElementType().Name + arg.ToString();
            }
        }

        class Base
        {
            public virtual string Frob<T>(string s)
            {
                return typeof(T).Name + ": Base: " + s;
            }
        }

        class Derived : Base
        {
            public override string Frob<T>(string s)
            {
                return typeof(T).Name + ": Derived: " + s;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void ValidateShared<T>(string s)
            {
                Func<string, string> f = Frob<T>;
                if (f(s) != typeof(T).Name + ": Derived: " + s)
                    throw new Exception();

                f = base.Frob<T>;
                if (f(s) != typeof(T).Name + ": Base: " + s)
                    throw new Exception();
            }

            public void Validate(string s)
            {
                Func<string, string> f = Frob<string>;
                if (f(s) != typeof(string).Name + ": Derived: " + s)
                    throw new Exception();

                f = base.Frob<string>;
                if (f(s) != typeof(string).Name + ": Base: " + s)
                    throw new Exception();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RunShared<T>(IFoo foo)
        {
            Func<int, string> a = foo.Frob<T>;
            if (a(456) != "Atom456")
                throw new Exception();
        }

        public static void Run()
        {
            IFoo foo = new FooUnshared();
            Func<int, string> a = foo.Frob<Atom>;
            if (a(123) != "Atom123")
                throw new Exception();

            RunShared<Atom>(new FooShared());

            new Derived().Validate("hello");
            new Derived().ValidateShared<object>("ola");
        }
    }

    class TestGvmDependencies
    {
        class Atom { }

        class Foo
        {
            public virtual object Frob<T>()
            {
                return new T[0, 0];
            }
        }

        class Bar : Foo
        {
            public override object Frob<T>()
            {
                return new T[0, 0, 0];
            }
        }

        public static void Run()
        {
            {
                Foo x = new Foo();
                x.Frob<Atom>();
            }

            {
                Foo x = new Bar();
                x.Frob<Atom>();
            }
        }
    }

    class TestGvmLookups
    {
        class Gen<T> { }
        class Atom1 { }
        class Atom2 { }

        class GenericBase<T>
        {
            public virtual (Type, Type) GenericMethod<U>() => (typeof(GenericBase<T>), typeof(U));
        }

        class NonGenericDerivedWithGenericBase : GenericBase<Atom1>
        {
            public override (Type, Type) GenericMethod<U>() => (typeof(NonGenericDerivedWithGenericBase), typeof(U));
        }

        class GenericDerivedWithGenericBase<T> : GenericBase<T>
        {
            public override (Type, Type) GenericMethod<U>() => (typeof(GenericDerivedWithGenericBase<T>), typeof(U));
        }

        class NonGenericBase
        {
            public virtual (Type, Type) GenericMethod<U>() => (typeof(NonGenericBase), typeof(U));
        }

        class GenericDerivedWithNonGenericBase<T> : NonGenericBase
        {
            public override (Type, Type) GenericMethod<U>() => (typeof(GenericDerivedWithNonGenericBase<T>), typeof(U));
        }

        static void TestInContext<T>()
        {
            {
                (var t1, var t2) = new GenericBase<T>().GenericMethod<Gen<T>>();
                if (t1 != typeof(GenericBase<T>) || t2 != typeof(Gen<T>))
                    throw new Exception();
            }

            {
                (var t1, var t2) = new GenericBase<T>().GenericMethod<Atom2>();
                if (t1 != typeof(GenericBase<T>) || t2 != typeof(Atom2))
                    throw new Exception();
            }

            {
                (var t1, var t2) = new NonGenericDerivedWithGenericBase().GenericMethod<Gen<T>>();
                if (t1 != typeof(NonGenericDerivedWithGenericBase) || t2 != typeof(Gen<T>))
                    throw new Exception();
            }

            {
                (var t1, var t2) = new NonGenericDerivedWithGenericBase().GenericMethod<Atom2>();
                if (t1 != typeof(NonGenericDerivedWithGenericBase) || t2 != typeof(Atom2))
                    throw new Exception();
            }

            {
                (var t1, var t2) = new GenericDerivedWithGenericBase<T>().GenericMethod<Gen<T>>();
                if (t1 != typeof(GenericDerivedWithGenericBase<T>) || t2 != typeof(Gen<T>))
                    throw new Exception();
            }

            {
                (var t1, var t2) = new GenericDerivedWithGenericBase<T>().GenericMethod<Atom2>();
                if (t1 != typeof(GenericDerivedWithGenericBase<T>) || t2 != typeof(Atom2))
                    throw new Exception();
            }

            {
                (var t1, var t2) = new GenericDerivedWithNonGenericBase<T>().GenericMethod<Gen<T>>();
                if (t1 != typeof(GenericDerivedWithNonGenericBase<T>) || t2 != typeof(Gen<T>))
                    throw new Exception();
            }

            {
                (var t1, var t2) = new GenericDerivedWithNonGenericBase<T>().GenericMethod<Atom2>();
                if (t1 != typeof(GenericDerivedWithNonGenericBase<T>) || t2 != typeof(Atom2))
                    throw new Exception();
            }
        }

        public static void Run()
        {
            TestInContext<object>();
            TestInContext<int>();
        }
    }

    class TestFieldAccess
    {
        class ClassType { }
        class ClassType2 { }
        struct StructType { }

        class Foo<T>
        {
            static Foo()
            {
                Console.WriteLine("Foo<" + typeof(T).Name + "> cctor");

                if (typeof(T) == typeof(ClassType))
                    TestFieldAccess.s_FooClassTypeCctorCount++;
                else
                    TestFieldAccess.s_FooStructTypeCctorCount++;
            }

            public static int s_intField;
            public static float s_floatField;
            public static string s_stringField;
            public static object s_objectField;
            public static long s_longField1;
            public static long s_longField2;
            public static long s_longField3;
            public static KeyValuePair<string, string> s_kvp;

            public int m_intField;
            public float m_floatField;
            public string m_stringField;
            public object m_objectField;
        }

        class Bar
        {
            static Bar()
            {
                Console.WriteLine("Bar cctor");
                TestFieldAccess.s_BarCctorCount++;
            }

            public static int s_intField;
            public static float s_floatField;
            public static string s_stringField;
            public static object s_objectField;
            public static long s_longField1;
            public static long s_longField2;
            public static long s_longField3;
            public static KeyValuePair<string, string> s_kvp;

            public int m_intField;
            public float m_floatField;
            public string m_stringField;
            public object m_objectField;
        }

        public class DynamicBase<T>
        {
            public T _t;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public DynamicBase() { }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public int SimpleMethod()
            {
                return 123;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public int MethodWithTInSig(T t)
            {
                _t = t;
                return 234;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public virtual string VirtualMethod(T t)
            {
                _t = t;
                return "DynamicBase<T>.VirtualMethod";
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string GenericMethod<U>(T t, U u)
            {
                _t = t;
                return typeof(U).ToString() + u.ToString();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public virtual string GenericVirtualMethod<U>(T t, U u)
            {
                _t = t;
                return "DynamicBase" + typeof(U).ToString() + u.ToString();
            }
        }

        public class DynamicDerived<T> : DynamicBase<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public DynamicDerived() { }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public override string VirtualMethod(T t)
            {
                _t = t;
                return "DynamicDerived<T>.VirtualMethod";
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public override string GenericVirtualMethod<U>(T t, U u)
            {
                _t = t;
                return "DynamicDerived" + typeof(U).ToString() + u.ToString();
            }
        }

        class UnconstructedTypeWithGCStatics
        {
#pragma warning disable 169
            static string s_gcField;
#pragma warning restore
        }

        class UnconstructedTypeWithNonGCStatics
        {
#pragma warning disable 169
            static float s_nonGcField;
#pragma warning restore
        }

        class UnconstructedTypeInstantiator<T> { }

        public static int s_FooClassTypeCctorCount = 0;
        public static int s_FooStructTypeCctorCount = 0;
        public static int s_BarCctorCount = 0;
        public static int s_NumErrors = 0;

        private static void Verify<T>(T expected, T actual)
        {
            if (!actual.Equals(expected))
            {
                Console.WriteLine("ACTUAL   : " + actual);
                Console.WriteLine("EXPECTED : " + expected);
                s_NumErrors++;
            }
        }

        private static void TestDynamicStaticFields()
        {
            Foo<object>.s_intField = 1234;
            Foo<object>.s_floatField = 12.34f;
            Foo<object>.s_longField1 = 0x1111;

            var fooDynamicOfClassType = typeof(Foo<>).MakeGenericType(typeof(ClassType)).GetTypeInfo();
            var fooDynamicOfClassType2 = typeof(Foo<>).MakeGenericType(typeof(ClassType2)).GetTypeInfo();

            FieldInfo fi = fooDynamicOfClassType.GetDeclaredField("s_intField");
            FieldInfo fi2 = fooDynamicOfClassType2.GetDeclaredField("s_intField");
            fi.SetValue(null, 1111);
            fi2.SetValue(null, 2222);
            Verify(1111, (int)fi.GetValue(null));
            Verify(2222, (int)fi2.GetValue(null));

            fi = fooDynamicOfClassType.GetDeclaredField("s_floatField");
            fi2 = fooDynamicOfClassType2.GetDeclaredField("s_floatField");
            fi.SetValue(null, 1.1f);
            fi2.SetValue(null, 2.2f);
            Verify(1.1f, (float)fi.GetValue(null));
            Verify(2.2f, (float)fi2.GetValue(null));

            fi = fooDynamicOfClassType.GetDeclaredField("s_longField1");
            fi2 = fooDynamicOfClassType2.GetDeclaredField("s_longField1");
            fi.SetValue(null, 0x11111111);
            fi2.SetValue(null, 0x22222222);
            Verify(0x11111111, (long)fi.GetValue(null));
            Verify(0x22222222, (long)fi2.GetValue(null));

            fi = fooDynamicOfClassType.GetDeclaredField("s_stringField");
            fi2 = fooDynamicOfClassType2.GetDeclaredField("s_stringField");
            fi.SetValue(null, "abc123");
            fi2.SetValue(null, "omgroflpwn");
            Verify("abc123", (string)fi.GetValue(null));
            Verify("omgroflpwn", (string)fi2.GetValue(null));

            fi = fooDynamicOfClassType.GetDeclaredField("s_objectField");
            fi2 = fooDynamicOfClassType2.GetDeclaredField("s_objectField");
            fi.SetValue(null, "qwerty");
            fi2.SetValue(null, "ytrewq");
            Verify("qwerty", (string)fi.GetValue(null));
            Verify("ytrewq", (string)fi2.GetValue(null));
        }

        private static void TestDynamicInvokeStubs()
        {
            Console.WriteLine("Testing dynamic invoke stubs...");
            // Root required methods / types statically instantiated over some unrelated type
            DynamicBase<Program> heh = new DynamicBase<Program>();
            heh.MethodWithTInSig(new Program());
            heh.SimpleMethod();
            heh.VirtualMethod(new Program());
            heh.GenericMethod(new Program(), "hello");
            heh.GenericVirtualMethod(new Program(), "hello");

            DynamicDerived<Program> heh2 = new DynamicDerived<Program>();
            heh2.VirtualMethod(new Program());
            heh2.GenericVirtualMethod(new Program(), "ayy");

            // Simple method invocation
            var dynamicBaseOfString = typeof(DynamicBase<>).MakeGenericType(typeof(string));
            object obj = Activator.CreateInstance(dynamicBaseOfString);
            {
                var simpleMethod = dynamicBaseOfString.GetTypeInfo().GetDeclaredMethod("SimpleMethod");
                int result = (int)simpleMethod.Invoke(obj, null);
                Verify((int)123, result);
            }

            // Method with T in the signature
            {
                var methodWithTInSig = dynamicBaseOfString.GetTypeInfo().GetDeclaredMethod("MethodWithTInSig");
                int result = (int)methodWithTInSig.Invoke(obj, new[] { "fad" });
                Verify((int)234, result);
            }

            // Test virtual method invocation
            {
                var virtualMethodDynamicBase = dynamicBaseOfString.GetTypeInfo().GetDeclaredMethod("VirtualMethod");
                string result = (string)virtualMethodDynamicBase.Invoke(obj, new[] { "fad" });
                Verify("DynamicBase<T>.VirtualMethod", result);
            }

            {
                var dynamicDerivedOfString = typeof(DynamicDerived<>).MakeGenericType(typeof(string));
                object dynamicDerivedObj = Activator.CreateInstance(dynamicDerivedOfString);
                var virtualMethodDynamicDerived = dynamicDerivedOfString.GetTypeInfo().GetDeclaredMethod("VirtualMethod");
                string result = (string)virtualMethodDynamicDerived.Invoke(dynamicDerivedObj, new[] { "fad" });
                Verify("DynamicDerived<T>.VirtualMethod", result);
            }

            // Test generic method invocation
            {
                var genericMethod = dynamicBaseOfString.GetTypeInfo().GetDeclaredMethod("GenericMethod").MakeGenericMethod(new[] { typeof(string) });
                string result = (string)genericMethod.Invoke(obj, new[] { "hey", "hello" });

                Verify("System.Stringhello", result);
            }

            // Test GVM invocation
            {
                var genericMethod = dynamicBaseOfString.GetTypeInfo().GetDeclaredMethod("GenericVirtualMethod");
                genericMethod = genericMethod.MakeGenericMethod(new[] { typeof(string) });
                string result = (string)genericMethod.Invoke(obj, new[] { "hey", "hello" });
                Verify("DynamicBaseSystem.Stringhello", result);
            }

            {
                var dynamicDerivedOfString = typeof(DynamicDerived<>).MakeGenericType(typeof(string));
                object dynamicDerivedObj = Activator.CreateInstance(dynamicDerivedOfString);
                var virtualMethodDynamicDerived = dynamicDerivedOfString.GetTypeInfo().GetDeclaredMethod("GenericVirtualMethod").MakeGenericMethod(new[] { typeof(string) });
                string result = (string)virtualMethodDynamicDerived.Invoke(dynamicDerivedObj, new[] { "hey", "fad" });
                Verify("DynamicDerivedSystem.Stringfad", result);
            }
        }

        private static void TestStaticFields()
        {
            Foo<ClassType>.s_intField = 11223344;
            Foo<ClassType>.s_stringField = "abcd";
            Foo<ClassType>.s_floatField = 12.34f;
            Foo<ClassType>.s_objectField = "123";
            Foo<ClassType>.s_kvp = new KeyValuePair<string, string>("1122", "3344");

            Foo<StructType>.s_intField = 44332211;
            Foo<StructType>.s_stringField = "dcba";
            Foo<StructType>.s_floatField = 43.21f;
            Foo<StructType>.s_objectField = "321";
            Foo<StructType>.s_kvp = new KeyValuePair<string, string>("4433", "2211");


            Bar.s_intField = 778899;
            Bar.s_stringField = "xxyyzz";
            Bar.s_floatField = 88.99f;
            Bar.s_objectField = "890";
            Bar.s_kvp = new KeyValuePair<string, string>("7788", "8899");

            // Testing correctness of cctor context
            {
                Foo<ClassType>.s_longField1 = 0xff00;
                Foo<ClassType>.s_longField2 = 0xff00;
                Foo<ClassType>.s_longField3 = 0xff00;
                if (TestFieldAccess.s_FooClassTypeCctorCount != 1)
                    s_NumErrors++;

                Foo<StructType>.s_longField1 = 0xff00;
                Foo<StructType>.s_longField2 = 0xff00;
                Foo<StructType>.s_longField3 = 0xff00;
                if (TestFieldAccess.s_FooStructTypeCctorCount != 1)
                    s_NumErrors++;

                Bar.s_longField1 = 0xff00;
                Bar.s_longField2 = 0xff00;
                Bar.s_longField3 = 0xff00;
                if (TestFieldAccess.s_BarCctorCount != 1)
                    s_NumErrors++;
            }

            Console.WriteLine("Testing static fields on type Foo<ClassType> ...");
            {
                FieldInfo fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_intField");
                Verify((int)11223344, (int)fi.GetValue(null));

                fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_stringField");
                Verify("abcd", (string)fi.GetValue(null));

                fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_floatField");
                Verify(12.34f, (float)fi.GetValue(null));

                fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_objectField");
                Verify("123", fi.GetValue(null));

                fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_kvp");
                var result = (KeyValuePair<string, string>)fi.GetValue(null);
                Verify("1122", result.Key);
                Verify("3344", result.Value);

                typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_stringField").SetValue(null, "ThisIsAString1");
                typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_objectField").SetValue(null, "ThisIsAString2");
                typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("s_kvp").SetValue(null, new KeyValuePair<string, string>("ThisIs", "AString"));
                Verify("ThisIsAString1", (string)Foo<ClassType>.s_stringField);
                Verify("ThisIsAString2", (string)Foo<ClassType>.s_objectField);
                Verify("ThisIs", (string)Foo<ClassType>.s_kvp.Key);
                Verify("AString", (string)Foo<ClassType>.s_kvp.Value);
            }

            Console.WriteLine("Testing static fields on type Foo<StructType> ...");
            {
                FieldInfo fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_intField");
                Verify(44332211, (int)fi.GetValue(null));

                fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_stringField");
                Verify("dcba", (string)fi.GetValue(null));

                fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_floatField");
                Verify(43.21f, (float)fi.GetValue(null));

                fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_objectField");
                Verify("321", fi.GetValue(null));

                fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_kvp");
                var result = (KeyValuePair<string, string>)fi.GetValue(null);
                Verify("4433", result.Key);
                Verify("2211", result.Value);

                typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_stringField").SetValue(null, "ThisIsAString3");
                typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_objectField").SetValue(null, "ThisIsAString4");
                typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("s_kvp").SetValue(null, new KeyValuePair<string, string>("ThisIs1", "AString1"));
                Verify("ThisIsAString3", (string)Foo<StructType>.s_stringField);
                Verify("ThisIsAString4", (string)Foo<StructType>.s_objectField);
                Verify("ThisIs1", (string)Foo<StructType>.s_kvp.Key);
                Verify("AString1", (string)Foo<StructType>.s_kvp.Value);
            }

            Console.WriteLine("Testing static fields on type Bar ...");
            {
                FieldInfo fi = typeof(Bar).GetTypeInfo().GetDeclaredField("s_intField");
                Verify(778899, (int)fi.GetValue(null));

                fi = typeof(Bar).GetTypeInfo().GetDeclaredField("s_stringField");
                Verify("xxyyzz", (string)fi.GetValue(null));

                fi = typeof(Bar).GetTypeInfo().GetDeclaredField("s_floatField");
                Verify(88.99f, (float)fi.GetValue(null));

                fi = typeof(Bar).GetTypeInfo().GetDeclaredField("s_objectField");
                Verify("890", fi.GetValue(null));

                fi = typeof(Bar).GetTypeInfo().GetDeclaredField("s_kvp");
                var result = (KeyValuePair<string, string>)fi.GetValue(null);
                Verify("7788", result.Key);
                Verify("8899", result.Value);

                typeof(Bar).GetTypeInfo().GetDeclaredField("s_stringField").SetValue(null, "ThisIsAString5");
                typeof(Bar).GetTypeInfo().GetDeclaredField("s_objectField").SetValue(null, "ThisIsAString6");
                typeof(Bar).GetTypeInfo().GetDeclaredField("s_kvp").SetValue(null, new KeyValuePair<string, string>("ThisIs2", "AString2"));
                Verify("ThisIsAString5", (string)Bar.s_stringField);
                Verify("ThisIsAString6", (string)Bar.s_objectField);
                Verify("ThisIs2", (string)Bar.s_kvp.Key);
                Verify("AString2", (string)Bar.s_kvp.Value);
            }
        }

        private static void TestInstanceFields()
        {
            Foo<ClassType> fooClassType = new Foo<ClassType>
            {
                m_intField = 1212,
                m_stringField = "2323",
                m_floatField = 34.34f,
                m_objectField = "4545",
            };

            Foo<StructType> fooStructType = new Foo<StructType>
            {
                m_intField = 2323,
                m_stringField = "3434",
                m_floatField = 45.45f,
                m_objectField = "5656",
            };

            Bar bar = new Bar
            {
                m_intField = 3434,
                m_stringField = "4545",
                m_floatField = 56.56f,
                m_objectField = "6767",
            };

            Console.WriteLine("Testing instance fields on type Foo<ClassType> ...");
            {
                FieldInfo fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("m_intField");
                Verify(1212, (int)fi.GetValue(fooClassType));

                fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("m_stringField");
                Verify("2323", (string)fi.GetValue(fooClassType));

                fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("m_floatField");
                Verify(34.34f, (float)fi.GetValue(fooClassType));

                fi = typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("m_objectField");
                Verify("4545", fi.GetValue(fooClassType));

                typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("m_stringField").SetValue(fooClassType, "ThisIsAString7");
                typeof(Foo<ClassType>).GetTypeInfo().GetDeclaredField("m_objectField").SetValue(fooClassType, "ThisIsAString8");
                Verify("ThisIsAString7", (string)fooClassType.m_stringField);
                Verify("ThisIsAString8", (string)fooClassType.m_objectField);
            }

            Console.WriteLine("Testing instance fields on type Foo<StructType> ...");
            {
                FieldInfo fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("m_intField");
                Verify(2323, (int)fi.GetValue(fooStructType));

                fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("m_stringField");
                Verify("3434", (string)fi.GetValue(fooStructType));

                fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("m_floatField");
                Verify(45.45f, (float)fi.GetValue(fooStructType));

                fi = typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("m_objectField");
                Verify("5656", fi.GetValue(fooStructType));

                typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("m_stringField").SetValue(fooStructType, "ThisIsAString9");
                typeof(Foo<StructType>).GetTypeInfo().GetDeclaredField("m_objectField").SetValue(fooStructType, "ThisIsAString10");
                Verify("ThisIsAString9", (string)fooStructType.m_stringField);
                Verify("ThisIsAString10", (string)fooStructType.m_objectField);
            }

            Console.WriteLine("Testing instance fields on type Bar ...");
            {
                FieldInfo fi = typeof(Bar).GetTypeInfo().GetDeclaredField("m_intField");
                Verify(3434, (int)fi.GetValue(bar));

                fi = typeof(Bar).GetTypeInfo().GetDeclaredField("m_stringField");
                Verify("4545", (string)fi.GetValue(bar));

                fi = typeof(Bar).GetTypeInfo().GetDeclaredField("m_floatField");
                Verify(56.56f, (float)fi.GetValue(bar));

                fi = typeof(Bar).GetTypeInfo().GetDeclaredField("m_objectField");
                Verify("6767", fi.GetValue(bar));

                typeof(Bar).GetTypeInfo().GetDeclaredField("m_stringField").SetValue(bar, "ThisIsAString11");
                typeof(Bar).GetTypeInfo().GetDeclaredField("m_objectField").SetValue(bar, "ThisIsAString12");
                Verify("ThisIsAString11", (string)bar.m_stringField);
                Verify("ThisIsAString12", (string)bar.m_objectField);
            }
        }

        private static void TestUnconstructedTypes()
        {
            // Testing for compilation failures due to references to unused static bases
            // See: https://github.com/dotnet/corert/issues/3211
            var a = typeof(UnconstructedTypeInstantiator<UnconstructedTypeWithGCStatics>).ToString();
            var b = typeof(UnconstructedTypeInstantiator<UnconstructedTypeWithNonGCStatics>).ToString();
        }

        public static void Run()
        {
            TestStaticFields();
            TestInstanceFields();
            TestDynamicStaticFields();
            TestDynamicInvokeStubs();
            TestUnconstructedTypes();

            if (s_NumErrors != 0)
                throw new Exception(s_NumErrors + " errors!");
        }
    }

    // Regression test for https://github.com/dotnet/corert/issues/3659
    class TestNativeLayoutGeneration
    {
#pragma warning disable 649 // s_ref was never assigned
        private static object s_ref;
#pragma warning restore 649

        class Used
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public virtual string DoStuff()
            {
                return "Used";
            }
        }

        class Unused<T> : Used
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public override string DoStuff()
            {
                return "Unused " + typeof(T).ToString();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Blagh()
            {
            }
        }

        public static void Run()
        {
            new Used().DoStuff();

            try
            {
                // Call an instance method on something we never allocated, but overrides a used virtual.
                // This asserted the compiler when trying to build a template for Unused<__Canon>.
                ((Unused<object>)s_ref).Blagh();
            }
            catch (NullReferenceException)
            {
                return;
            }

            throw new Exception();
        }
    }

    class TestInterfaceVTableTracking
    {
        class Gen<T> { }

        interface IFoo<T>
        {
            Array Frob();
        }

        class GenericBase<T> : IFoo<T>
        {
            public Array Frob()
            {
                return new Gen<T>[1, 1];
            }
        }

        class Derived<T> : GenericBase<Gen<T>>
        {
        }

        static volatile IFoo<Gen<string>> s_foo;

        public static void Run()
        {
            // This only really tests whether we can compile this.
            s_foo = new Derived<string>();
            Array arr = s_foo.Frob();
            arr.SetValue(new Gen<Gen<string>>(), new int[] { 0, 0 });
        }
    }

    class TestClassVTableTracking
    {
        class Unit { }

        class Gen<T, U>
        {
            public virtual int Test()
            {
                return 42;
            }
        }

        static int Call<T>()
        {
            return new Gen<T, Unit>().Test();
        }

        public static void Run()
        {
            // This only really tests whether we can compile this.
            Call<object>();
        }
    }

    class TestNullableCasting
    {
        struct Mine<T> { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CallWithNullable<T>(object m)
        {
            return m is T;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CallWithReferenceType<T>(object m)
        {
            return m is Nullable<Mine<T>>;
        }

        public static void Run()
        {
            if (!CallWithNullable<Nullable<Mine<object>>>(new Mine<object>()))
                throw new Exception();

            if (CallWithNullable<Nullable<Mine<object>>>(new Mine<string>()))
                throw new Exception();

            if (!CallWithReferenceType<object>(new Mine<object>()))
                throw new Exception();

            if (CallWithReferenceType<object>(new Mine<string>()))
                throw new Exception();

            if (!(((object)new Mine<object>()) is Nullable<Mine<object>>))
                throw new Exception();
        }
    }

    class TestVariantCasting
    {
        private delegate T GenericDelegate<out T>();

        class Base { }
        class Derived : Base { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IsInstanceOfGenericDelegateOf<T>(object o)
        {
            return o is GenericDelegate<T>;
        }

        public static void Run()
        {
            GenericDelegate<Derived> del = () => null;
            if (!IsInstanceOfGenericDelegateOf<Base>(del))
                throw new Exception();
        }
    }

    class TestByRefLikeVTables
    {
        class Atom<T> { }

        ref struct RefStruct<T>
        {
            public override bool Equals(object o) => o is Atom<T[]>;
            public override int GetHashCode() => 0;

            public override string ToString()
            {
                return typeof(T).ToString();
            }
        }

        public static void Run()
        {
            // This is a regression test making sure we can build a vtable for the byref-like type.
            // The vtable is necessary for a generic dictionary lookup in the ToString method.
            // Method bodies of Equals and GetHashCode become reachable through the magical
            // "unboxing" thunks we generate for byref-like types, and only through them.
            RefStruct<string> r = default;
            if (r.ToString() != "System.String")
                throw new Exception();
        }
    }

    class TestDevirtualization
    {
        interface IDevirt
        {
            int GetAndSet(int x);
        }

        struct Devirt : IDevirt
        {
            public int X;

            public int GetAndSet(int x)
            {
                int result = X;
                X = x;
                return result;
            }
        }

        interface IGenericDevirt
        {
            int GetAndSet(int x);
            Type GetTheType();
        }

        struct GenericDevirt<T> : IGenericDevirt
        {
            public int X;

            public int GetAndSet(int x)
            {
                int result = X;
                X = x;
                return result;
            }

            public Type GetTheType()
            {
                return typeof(T);
            }
        }

        static void DoSimpleDevirt()
        {
            // This will potentially transform to a direct call
            int result = ((IDevirt)new Devirt { X = 123 }).GetAndSet(456);
            if (result != 123)
                throw new Exception();
        }

        static void DoSimpleDevirtBoxed()
        {
            object o = new Devirt { X = 123 };

            // Force o to be boxed no matter what
            o.ToString();

            // This will potentially transform to a direct call
            int result = ((IDevirt)o).GetAndSet(456);
            if (result != 123)
                throw new Exception();

            if (((Devirt)o).X != 456)
                throw new Exception();
        }

        static void DoGenericDevirt()
        {
            // This will potentially transform to a direct call
            int result1 = ((IGenericDevirt)new GenericDevirt<string> { X = 123 }).GetAndSet(456);
            if (result1 != 123)
                throw new Exception();

            // This will potentially transform to a direct call
            Type result2 = ((IGenericDevirt)new GenericDevirt<string>()).GetTheType();
            if (result2 != typeof(string))
                throw new Exception();
        }

        static void DoGenericDevirtBoxed()
        {
            object o1 = new GenericDevirt<string> { X = 123 };

            // Force o1 to be boxed no matter what
            o1.ToString();

            // This will potentially transform to a direct call
            int result1 = ((IGenericDevirt)o1).GetAndSet(456);
            if (result1 != 123)
                throw new Exception();

            if (((GenericDevirt<string>)o1).X != 456)
                throw new Exception();

            object o2 = new GenericDevirt<string> { X = 123 };

            // Force o2 to be boxed no matter what
            o2.ToString();

            // This will potentially transform to a direct call
            Type result2 = ((IGenericDevirt)o2).GetTheType();
            if (result2 != typeof(string))
                throw new Exception();
        }

        static void DoGenericDevirtShared<T>()
        {
            // This will potentially transform to a direct call
            int result1 = ((IGenericDevirt)new GenericDevirt<T[]> { X = 123 }).GetAndSet(456);
            if (result1 != 123)
                throw new Exception();

            // This will potentially transform to a direct call
            Type result2 = ((IGenericDevirt)new GenericDevirt<T[]>()).GetTheType();
            if (result2 != typeof(T[]))
                throw new Exception();
        }

        static void DoGenericDevirtBoxedShared<T>()
        {
            object o1 = new GenericDevirt<T[]> { X = 123 };

            // Force o1 to be boxed no matter what
            o1.ToString();

            // This will potentially transform to a direct call
            int result1 = ((IGenericDevirt)o1).GetAndSet(456);
            if (result1 != 123)
                throw new Exception();

            if (((GenericDevirt<T[]>)o1).X != 456)
                throw new Exception();

            object o2 = new GenericDevirt<T[]> { X = 123 };

            // Force o2 to be boxed no matter what
            o2.ToString();

            // This will potentially transform to a direct call
            Type result2 = ((IGenericDevirt)o2).GetTheType();
            if (result2 != typeof(T[]))
                throw new Exception();
        }

        public static void Run()
        {
            DoSimpleDevirt();
            DoSimpleDevirtBoxed();
            DoGenericDevirt();
            DoGenericDevirtBoxed();
            DoGenericDevirtShared<string>();
            DoGenericDevirtBoxedShared<string>();
        }
    }

    class TestImportFailure
    {
        class Generic<T> { }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        class Unloadable<T> { }
        class Unloadable : Unloadable<object> { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void DoGenericLookup<T>()
        {
            // The method body for this will be converted to a throw, but when compiling this,
            // the code generator might attempt to do generic lookups before realizing this won't work.
            typeof(Generic<T>).ToString();
            new Unloadable().ToString();
        }

        public static void Run()
        {
            bool failed = false;

            try
            {
                DoGenericLookup<object>();
            }
            catch (TypeLoadException)
            {
                failed = true;
            }

            if (!failed)
                throw new Exception();
        }
    }

    class TestGenericInlining
    {
        class NeverSeenInstantiated<T> { }

        class AnotherNeverSeenInstantiated<T> { }

        class NeverAllocatedIndirection<T, U>
        {
            public string GetString() => new AnotherNeverSeenInstantiated<T>().ToString();
        }

        class NeverAllocated<T>
        {
            static NeverAllocatedIndirection<T, object> s_indirection = null;

            public string GetString() => new NeverSeenInstantiated<T>().ToString();
            public string GetStringIndirect() => s_indirection.GetString();
        }

        class Dummy { }

        static NeverAllocated<Dummy> s_neverAllocated = null;

        class GenericInline<T>
        {
            public GenericInline()
            {
                _arr = (T)(object)new string[1] { "ohai" };
            }
            T _arr;
            public T GetArr() => _arr;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static object InnerTest(object o, object dummy) => o;

        static object OtherTest() => null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static object Test(GenericInline<string[]> t)
        {
            return InnerTest(t.GetArr()[0], OtherTest());
        }

        public static void Run()
        {
            // We're just making sure the compiler doesn't crash.
            // Both of the calls below are expected to get inlined by an optimized codegen,
            // triggering interesting behaviors in the dependency analysis of the scanner
            // that runs before compilation.
            if (s_neverAllocated != null)
            {
                Console.WriteLine(s_neverAllocated.GetString());
                Console.WriteLine(s_neverAllocated.GetStringIndirect());
            }

            // Regression test for https://github.com/dotnet/corert/issues/7625
            if ((string)Test(new GenericInline<string[]>()) != "ohai")
                throw new Exception();
        }
    }

    class TestGenericInliningDoesntHappen
    {
        interface IGenericInterface<T>
        {
            int Execute();
        }

        class GenericInterfaceImpl<T> : IGenericInterface<T>
        {
            public int Execute() => 42;
        }

        class GenericType<T>
        {
            IGenericInterface<T> _instance = new GenericInterfaceImpl<T>();

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static GenericType<T> GenericMethod<U>()
            {
                var v = new GenericType<T>();
                // Codegen might attempt to inline this, but it requires generic inlining we don't support right now
                if (v.NonGenericMethod() != 42)
                    throw new Exception();
                return v;
            }

            private int NonGenericMethod() => _instance.Execute();
        }

        public static void Run()
        {
            // Regression test for https://github.com/dotnet/runtimelab/issues/485
            GenericType<object>.GenericMethod<int>();
        }
    }

    class TestGvmDependenciesFromLazy
    {
        interface IFoo
        {
            string FrobToo<T>();
            void Blah<T>();
        }

        class Foo : IFoo
        {
            public virtual string Frob<T>() => $"Foo.Frob<{typeof(T)}>()";
            public virtual string FrobToo<T>() => Frob<T>();

            // RyuJIT doesn't inline, but futureproofing
            [MethodImpl(MethodImplOptions.NoInlining)]
            void IFoo.Blah<T>() => Bork<T>();

            // In optimized builds, Bork method body will not be generated and its generic dictionary is
            // useless in practice (it gets inlined), but the native layout for Blah still needs info on
            // how to build the empty dictionary because the generic dictionary of Blah
            // refers to this method's dictionary.
            //
            // The dictionary layouts are computed without any assumptions about inlining.
            private static void Bork<T>() { }
        }

        class Bar : Foo
        {
            public override string Frob<T>() => $"Bar.Frob<{typeof(T)}>()";
        }

        // Make it unlikely things will devirtualize
        static IFoo s_f = new Bar();

        public static void Run()
        {
            // Regression test for https://github.com/dotnet/runtimelab/issues/537
            if (s_f.FrobToo<object>() != "Bar.Frob<System.Object>()")
                throw new Exception();

            s_f.Blah<object>();
        }
    }

    class TestGvmDependencyFromGenericLazy
    {
        abstract class GenericBase<T, U>
        {
            public abstract string Get<V>();
        }

        class GenericDerived<T> : GenericBase<T, object>
        {
            public override string Get<V>() => $"GenericDerived<{typeof(T)}>.Get<{typeof(V)}>";
        }

        abstract class LazyGenBase
        {
            public abstract string Gen<T, U>();
        }

        class LazyGenDerived : LazyGenBase
        {
            public override string Gen<T, U>()
            {
                GenericBase<T, object> b = new GenericDerived<T>();
                return Roundtrip(b).Get<U>();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static T Roundtrip<T>(T x) => x;

        public static void Run()
        {
            LazyGenBase b = new LazyGenDerived();
            Console.WriteLine(Roundtrip(b).Gen<object, string>());
        }
    }

    class TestConstrainedGvmCalls
    {
        interface IInterface<T>
        {
            string Method<X>();
        }

        class Base<T> : IInterface<T>
        {
            string IInterface<T>.Method<X>() => $"Base<{typeof(T)}>.Method<{typeof(X)}>()";
        }

        class Derived<T, U> : Base<T>, IInterface<U>
        {
            string IInterface<U>.Method<X>() => $"Derived<{typeof(T)},{typeof(U)}>.Method<{typeof(X)}>()";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string ConstrainedCall<T, U, X>(ref T instance) where T : IInterface<U>
        {
            return instance.Method<X>();
        }

        class Atom1 { }
        class Atom2 { }
        class Atom3 { }

        public static void Run()
        {
            var derived = new Derived<Atom1, Atom2>();
            if (ConstrainedCall<Derived<Atom1, Atom2>, Atom2, Atom3>(ref derived) != "Derived<Generics+TestConstrainedGvmCalls+Atom1,Generics+TestConstrainedGvmCalls+Atom2>.Method<Generics+TestConstrainedGvmCalls+Atom3>()")
                throw new Exception();
            if (ConstrainedCall<Derived<Atom1, Atom2>, Atom1, Atom3>(ref derived) != "Base<Generics+TestConstrainedGvmCalls+Atom1>.Method<Generics+TestConstrainedGvmCalls+Atom3>()")
                throw new Exception();
        }
    }

    class TestConstrainedGvmValueTypeCalls
    {
        class Atom1 { }
        class Atom2 { }

        interface IFoo<T>
        {
            bool Frob(object o);
        }

        struct Foo<T> : IFoo<T>
        {
            public int FrobbedValue;

            public bool Frob(object o)
            {
                FrobbedValue = 12345;
                return o is T[,,];
            }
        }

        interface IBar<T>
        {
            bool Frob<U>(object o);
        }

        struct Bar<T> : IBar<T>
        {
            public int FrobbedValue;

            public bool Frob<U>(object o)
            {
                FrobbedValue = 5678;
                return o is T[,,,];
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool DoFrob1<T, U>(ref T t, object o) where T : IFoo<U>
        {
            // Perform a constrained interface call from shared code.
            // This should have been resolved to a direct call at compile time.
            return t.Frob(o);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool DoFrob2<T, U>(ref T t, object o) where T : IBar<U>
        {
            // Perform a constrained interface call from shared code.
            // This should have been resolved to a direct call at compile time.
            return t.Frob<U>(o);
        }

        public static void Run()
        {
            {
                var foo1 = new Foo<Atom1>();
                bool result = DoFrob1<Foo<Atom1>, Atom1>(ref foo1, new Atom1[0, 0, 0]);

                // If the FrobbedValue doesn't change when we frob, we must have done box+interface call.
                if (foo1.FrobbedValue != 12345)
                    throw new Exception();

                // Also check we passed the right generic context to Foo.Frob
                if (!result)
                    throw new Exception();

                // Also check dependency analysis:
                // EEType for Atom2[,,] that we'll check for was never allocated.
                var foo2 = new Foo<Atom2>();
                if (DoFrob1<Foo<Atom2>, Atom2>(ref foo2, new object()))
                    throw new Exception();
            }

            {
                var bar1 = new Bar<Atom1>();
                bool result = DoFrob2<Bar<Atom1>, Atom1>(ref bar1, new Atom1[0, 0, 0, 0]);

                // If the FrobbedValue doesn't change when we frob, we must have done box+interface call.
                if (bar1.FrobbedValue != 5678)
                    throw new Exception();

                // Also check we passed the right generic context to Foo.Frob
                if (!result)
                    throw new Exception();

                // Also check dependency analysis:
                // EEType for Atom2[,,,] that we'll check for was never allocated.
                var bar2 = new Bar<Atom2>();
                if (DoFrob2<Bar<Atom2>, Atom2>(ref bar2, new object()))
                    throw new Exception();
            }
        }
    }

    class TestDefaultGenericVirtualInterfaceMethods
    {
        class Atom1 { }
        class Atom2 { }
        struct SAtom1 { }

        interface IFoo
        {
            string Foo<T>() => $"Hello from IFoo.Foo<{typeof(T).Name}>";
        }

        interface IFoo<T>
        {
            string Foo<U>() => $"Hello from IFoo<{typeof(T).Name}>.Foo<{typeof(U).Name}>";
        }

        interface IBar<T> : IFoo, IFoo<T[]>, IFoo<T>
        {
            string IFoo.Foo<U>() => $"Hello from IBar<{typeof(T).Name}>.IFoo.Foo<{typeof(U).Name}>";
            string IFoo<T[]>.Foo<U>() => $"Hello from IBar<{typeof(T).Name}>.IFoo<T[]>.Foo<{typeof(U).Name}>";
            string IFoo<T>.Foo<U>() => $"Hello from IBar<{typeof(T).Name}>.IFoo<T>.Foo<{typeof(U).Name}>";
        }

        class Foo : IFoo, IFoo<Atom1>, IFoo<Atom2>, IFoo<SAtom1>
        {
            string IFoo<Atom2>.Foo<T>() => $"Hello from Foo.Foo<{typeof(T).Name}>";
        }

        class Bar : IBar<Atom1> { }

        class Bar<T> : IBar<T> { }

        class DerivedBar : Bar<Atom1> { }

        class DerivedSBar : Bar<SAtom1> { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string ConstrainedCall<T, U, X>(ref T instance) where T : IFoo<U>
        {
            return instance.Foo<X>();
        }

        public static void Run()
        {
            {
                IFoo f = new Foo();
                if (f.Foo<Atom2>() != "Hello from IFoo.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom1> f = new Foo();
                if (f.Foo<Atom2>() != "Hello from IFoo<Atom1>.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom2> f = new Foo();
                if (f.Foo<Atom1>() != "Hello from Foo.Foo<Atom1>")
                    throw new Exception();
            }

            {
                IFoo<SAtom1> f = new Foo();
                if (f.Foo<SAtom1>() != "Hello from IFoo<SAtom1>.Foo<SAtom1>")
                    throw new Exception();
            }

            {
                IFoo f = new Bar();
                if (f.Foo<Atom2>() != "Hello from IBar<Atom1>.IFoo.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom1> f = new Bar();
                if (f.Foo<Atom2>() != "Hello from IBar<Atom1>.IFoo<T>.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom1[]> f = new Bar();
                if (f.Foo<Atom2>() != "Hello from IBar<Atom1>.IFoo<T[]>.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom1[]> f = new Bar();
                if (f.Foo<SAtom1>() != "Hello from IBar<Atom1>.IFoo<T[]>.Foo<SAtom1>")
                    throw new Exception();
            }

            {
                Foo f = new Foo();
                if (ConstrainedCall<Foo, Atom1, Atom1>(ref f) != "Hello from IFoo<Atom1>.Foo<Atom1>")
                    throw new Exception();
                if (ConstrainedCall<Foo, Atom2, Atom1>(ref f) != "Hello from Foo.Foo<Atom1>")
                    throw new Exception();
            }

            {
                IFoo f = new DerivedBar();
                if (f.Foo<Atom2>() != "Hello from IBar<Atom1>.IFoo.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom1> f = new DerivedBar();
                if (f.Foo<Atom2>() != "Hello from IBar<Atom1>.IFoo<T>.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom1[]> f = new DerivedBar();
                if (f.Foo<Atom2>() != "Hello from IBar<Atom1>.IFoo<T[]>.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<Atom1[]> f = new DerivedBar();
                if (f.Foo<SAtom1>() != "Hello from IBar<Atom1>.IFoo<T[]>.Foo<SAtom1>")
                    throw new Exception();
            }

            {
                IFoo f = new DerivedSBar();
                if (f.Foo<Atom2>() != "Hello from IBar<SAtom1>.IFoo.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<SAtom1> f = new DerivedSBar();
                if (f.Foo<Atom2>() != "Hello from IBar<SAtom1>.IFoo<T>.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<SAtom1[]> f = new DerivedSBar();
                if (f.Foo<Atom2>() != "Hello from IBar<SAtom1>.IFoo<T[]>.Foo<Atom2>")
                    throw new Exception();
            }

            {
                IFoo<SAtom1[]> f = new DerivedSBar();
                if (f.Foo<SAtom1>() != "Hello from IBar<SAtom1>.IFoo<T[]>.Foo<SAtom1>")
                    throw new Exception();
            }
        }
    }

    class TestSimpleGenericRecursion
    {
        struct GenStruct<T> { }

        class GenClass<T> { }

        private static object RecurseOverStruct<T>(int count) where T : new()
        {
            if (count > 0)
                RecurseOverStruct<GenStruct<T>>(count - 1);

            return new T();
        }

        private static object RecurseOverClass<T>(int count) where T : new()
        {
            if (count > 0)
                RecurseOverClass<GenClass<T>>(count - 1);

            return new T();
        }

        public static void Run()
        {
            RecurseOverStruct<int>(2);
            RecurseOverClass<int>(2);

            bool caughtException = false;
            try
            {
                RecurseOverStruct<int>(100);
            }
            catch (TypeLoadException) { caughtException = true; }

            if (!caughtException)
                throw new Exception();

            caughtException = false;
            try
            {
                RecurseOverClass<int>(100);
            }
            catch (TypeLoadException) { caughtException = true; }

            if (!caughtException)
                throw new Exception();
        }
    }

    class TestGenericRecursionFromNpgsql
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct SqlRange<T>
        {
            private T Value1;
            private T Value2;
        }

        class ChunkingTypeHandler<T> : TypeHandler<T>
        {
        }

        class RangeHandler<T> : ChunkingTypeHandler<SqlRange<T>>
        {
            public RangeHandler()
            {
                new TypeHandler<T>().ToString();
            }
        }

        class TypeHandler
        {
            public virtual void CreateRangeHandler() { }
        }

        class TypeHandler<T> : TypeHandler
        {
            public override void CreateRangeHandler()
            {
                new RangeHandler<T>().ToString();
            }
        }

        public static void Run()
        {
            // https://github.com/dotnet/corert/issues/6052
            // There is a generic recursion in the above hierarchy. This just tests that we can compile.
            new TypeHandler<bool>().CreateRangeHandler();
        }
    }

    class TestRecursionInGenericVirtualMethods
    {
        struct Buffer<T> { }

        class Getter<T> { }

        class Base
        {
            public virtual Getter<T> Get<T>() => new Getter<T>();
        }

        class Derived : Base
        {
            private Base _b = new Base();
            public override Getter<T> Get<T>() => Make<T>();
            private Getter<T> Make<T>() => _b.Get<Buffer<T>>() as Getter<T>;
        }

        static Base s_derived = new Derived();

        public static void Run()
        {
            // There is a generic recursion in the above hierarchy. This just tests that we can compile.
            // Inspired by https://github.com/dotnet/machinelearning/blob/cc5e6395e0d15e4d3db702b9cb1129e12838b840/src/Microsoft.ML.Transforms/UngroupTransform.cs#L610-L629
            s_derived.Get<object>();
        }
    }

    class TestRecursionThroughGenericLookups
    {
        abstract class Handler<T>
        {
            public abstract void Write(object val);
        }

        class RangeHandler<T> : Handler<Gen<T>>
        {
            public override void Write(object val) { if (val is ArrayHandler<Gen<T>> h) h.Write(default); }
        }

        class ArrayHandler<T>
        {
            public virtual void Write(object val) { if (val is RangeHandler<T> h) h.Write(default); }
        }

        struct Gen<T>
        {
        }

        public static void Run()
        {
            // There is a generic recursion in the above hierarchy. This just tests that we can compile.
            new ArrayHandler<object>().Write(null);
            new RangeHandler<object>().Write(default);
        }
    }

    static class TestGvmLookupDependency
    {
        struct SmallCat<T> { }

        interface ITechnique
        {
            void CatSlaps<T>() { /* Cannot reference T or it stops testing the thing it should */ }
        }

        struct Technique : ITechnique { }

        static void CatConcepts<T, U>() where T : ITechnique => default(T).CatSlaps<SmallCat<U>>();

        public static void Run()
        {
            CatConcepts<Technique, int>();
            CatConcepts<Technique, object>();
        }
    }

    class TestInvokeMemberCornerCaseInGenerics
    {
        class Generic<T>
        {
            public void Method(params T[] ts) { }
        }

        class Atom { }

        static Generic<Atom> s_instance = new Generic<Atom>();
        static Type s_atomType = typeof(Atom);

        public static void Run()
        {
            s_instance.Method(null);

            // Regression test for https://github.com/dotnet/runtime/issues/65612
            // This requires MethodTable for "Atom[]" - just make sure the compiler didn't crash and we can invoke
            typeof(Generic<>).MakeGenericType(s_atomType).InvokeMember("Method", BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance, null, s_instance, new object[] { new Atom() });
        }
    }
}
