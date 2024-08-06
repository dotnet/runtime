// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RuntimeLibrariesTest;
using TypeOfRepo;

// Test universal generics that have dependencies on non-universal canon generics

namespace TypeOfRepo
{
    public partial class TypeOf
    {
        static bool s_typeOfForPartialUniversalGenericsTests = InitPartialUniversalGenericTypes();
        static bool InitPartialUniversalGenericTypes()
        {
            InitTypeRepoDictionary();

            s_TypeRepo["PUG_DerivedWithArray"] = typeof(PartialUniversalGen.DerivedWithArray<>);
            s_TypeRepo["PUG_StructThatSatisfiesConstraints"] = typeof(PartialUniversalGen.StructThatSatisfiesConstraints);
            s_TypeRepo["PUG_GenericThatUsesAllocableGeneric"] = typeof(PartialUniversalGen.GenericThatUsesAllocableGeneric<>);
            s_TypeRepo["PUG_UniversalGenericImplementsInterfaceInstantiatedOverTArray"] = typeof(PartialUniversalGen.UniversalGenericImplementsInterfaceInstantiatedOverTArray<>);
            s_TypeRepo["PUG_UseCanonGenericMethodFromUniversalGenericClassType1"] = typeof(PartialUniversalGen.UseCanonGenericMethodFromUniversalGenericClassType1<>);
            s_TypeRepo["PUG_UseCanonGenericMethodFromUniversalGenericClassType2"] = typeof(PartialUniversalGen.UseCanonGenericMethodFromUniversalGenericClassType2<>);
            s_TypeRepo["PUG_UseCanonGenericTypeFromUniversalGenericClassType3"] = typeof(PartialUniversalGen.UseCanonGenericTypeFromUniversalGenericClassType3<,>);
            s_TypeRepo["PUG_UseCanonGenericMethodFromUniversalGenericClassType4"] = typeof(PartialUniversalGen.UseCanonGenericMethodFromUniversalGenericClassType4<,>);
            s_TypeRepo["PUG_UseCanonGenericTypeFromUniversalGenericClassType5"] = typeof(PartialUniversalGen.UseCanonGenericTypeFromUniversalGenericClassType5<,>);
            s_TypeRepo["PUG_UseCanonGenericTypeFromUniversalGenericClassType6"] = typeof(PartialUniversalGen.UseCanonGenericTypeFromUniversalGenericClassType6<,>);

            return s_TypeRepo.ContainsKey("PUG_DerivedWithArray");
        }

        public static Type PUG_DerivedWithArray { get { return s_TypeRepo["PUG_DerivedWithArray"]; } }
        public static Type PUG_StructThatSatisfiesConstraints { get { return s_TypeRepo["PUG_StructThatSatisfiesConstraints"]; } }
        public static Type PUG_GenericThatUsesAllocableGeneric { get { return s_TypeRepo["PUG_GenericThatUsesAllocableGeneric"]; } }
        public static Type PUG_UniversalGenericImplementsInterfaceInstantiatedOverTArray { get { return s_TypeRepo["PUG_UniversalGenericImplementsInterfaceInstantiatedOverTArray"]; } }
        public static Type PUG_UseCanonGenericMethodFromUniversalGenericClassType1 { get { return s_TypeRepo["PUG_UseCanonGenericMethodFromUniversalGenericClassType1"]; } }
        public static Type PUG_UseCanonGenericMethodFromUniversalGenericClassType2 { get { return s_TypeRepo["PUG_UseCanonGenericMethodFromUniversalGenericClassType2"]; } }
        public static Type PUG_UseCanonGenericTypeFromUniversalGenericClassType3 { get { return s_TypeRepo["PUG_UseCanonGenericTypeFromUniversalGenericClassType3"]; } }
        public static Type PUG_UseCanonGenericMethodFromUniversalGenericClassType4 { get { return s_TypeRepo["PUG_UseCanonGenericMethodFromUniversalGenericClassType4"]; } }
        public static Type PUG_UseCanonGenericTypeFromUniversalGenericClassType5 { get { return s_TypeRepo["PUG_UseCanonGenericTypeFromUniversalGenericClassType5"]; } }
        public static Type PUG_UseCanonGenericTypeFromUniversalGenericClassType6 { get { return s_TypeRepo["PUG_UseCanonGenericTypeFromUniversalGenericClassType6"]; } }
    }
}

namespace PartialUniversalGen
{
    public interface IBase<T>
    {
        object BaseMethod(T t, ref string s);
    }
    public interface IMiddle<T>
    {
        object MiddleMethod(T t, ref string s);
    }
    public interface IDerived<T>
    {
        object DerivedMethod(T t, ref string s);
    }

    public class Base<T> : IBase<T>
    {
        public virtual object BaseMethod(T t, ref string s)
        {
            s = "BaseMethod";
            return new T[] { t };
        }
    }
    public class MiddleClass<T> : Base<T[]>, IMiddle<T>
    {
        public virtual object MiddleMethod(T t, ref string s)
        {
            s = "MiddleMethod";
            return new T[] { t };
        }
    }
    public class DerivedWithArray<U> : MiddleClass<U>, IDerived<U>
    {
        public object DerivedMethod(U u, ref string s)
        {
            BaseMethod(new U[] { u }, ref s);
            Assert.AreEqual("BaseMethod", s);

            MiddleMethod(u, ref s);
            Assert.AreEqual("MiddleMethod", s);

            s = "DerivedMethod";
            return new U[] { u };
        }
    }
    public class MiddleClassLeadingToNonSharedGenerics<T> : Base<int>, IMiddle<T>
    {
        public virtual object MiddleMethod(T t, ref string s)
        {
            s = "MiddleMethod";
            return new T[] { t };
        }
    }
    public class DerivedWithNonSharedGenerics<U> : MiddleClassLeadingToNonSharedGenerics<U>, IDerived<U>
    {
        public object DerivedMethod(U u, ref string s)
        {
            BaseMethod(111, ref s);
            Assert.AreEqual("BaseMethod", s);

            MiddleMethod(u, ref s);
            Assert.AreEqual("MiddleMethod", s);

            s = "DerivedMethod";
            return new U[] { u };
        }
    }
    public class OtherDerivedType<T> : MiddleClass<T>
    {
        // This type forces the virtual methods to not be in the sealed vtable
        public override object BaseMethod(T[] t, ref string s) { return "OtherDerivedType"; }
        public override object MiddleMethod(T t, ref string s) { return "OtherDerivedType"; }
    }
    public class OtherDerivedTypeLeadingToNonSharedGenerics<T> : MiddleClassLeadingToNonSharedGenerics<T>
    {
        // This type forces the virtual methods to not be in the sealed vtable
        public override object BaseMethod(int t, ref string s) { return "OtherDerivedType"; }
        public override object MiddleMethod(T t, ref string s) { return "OtherDerivedType"; }
    }

    public class BaseWithTwoArgs<T, U> : IBase<T>
    {
        public virtual object BaseMethod(T t, ref string s)
        {
            s = "BaseMethod";
            return new T[] { t };
        }
    }
    public class MiddleClassWithTwoArgs<T, U> : BaseWithTwoArgs<T[], U>, IMiddle<T>
    {
        public virtual object MiddleMethod(T t, ref string s)
        {
            s = "MiddleMethod";
            return new T[] { t };
        }
    }
    public class MiddleClassWithTwoArgsLeadingToPartialAndNonShared<T, U> : BaseWithTwoArgs<int, U>, IMiddle<T>
    {
        public virtual object MiddleMethod(T t, ref string s)
        {
            s = "MiddleMethod";
            return new T[] { t };
        }
    }
    public class DerivedWithArrayWithTwoArgs<T, U> : MiddleClassWithTwoArgs<T, T>, IDerived<T>
    {
        public object DerivedMethod(T t, ref string s)
        {
            BaseMethod(new T[] { t }, ref s);
            Assert.AreEqual("BaseMethod", s);

            MiddleMethod(t, ref s);
            Assert.AreEqual("MiddleMethod", s);

            s = "DerivedMethod";
            return new T[] { t };
        }
    }
    public class DerivedWithArrayWithTwoArgsLeadingToPartialAndNonShared<T, U> : MiddleClassWithTwoArgsLeadingToPartialAndNonShared<T, T>, IDerived<T>
    {
        public object DerivedMethod(T t, ref string s)
        {
            BaseMethod(11, ref s);
            Assert.AreEqual("BaseMethod", s);

            MiddleMethod(t, ref s);
            Assert.AreEqual("MiddleMethod", s);

            s = "DerivedMethod";
            return new T[] { t };
        }
    }
    public class OtherDerivedTypeWithTwoArgs<T, U> : MiddleClassWithTwoArgs<T, T>
    {
        // This type forces the virtual methods to not be in the sealed vtable
        public override object BaseMethod(T[] t, ref string s) { return "OtherDerivedType"; }
        public override object MiddleMethod(T t, ref string s) { return "OtherDerivedType"; }
    }
    public class OtherDerivedTypeWithTwoArgsLeadingToPartialAndNonShared<T, U> : MiddleClassWithTwoArgsLeadingToPartialAndNonShared<T, T>
    {
        // This type forces the virtual methods to not be in the sealed vtable
        public override object BaseMethod(int t, ref string s) { return "OtherDerivedType"; }
        public override object MiddleMethod(T t, ref string s) { return "OtherDerivedType"; }
    }

    public interface IGetContainedObject
    {
        object WrappedObject();
    }

    // DummyInterface is used to ensure that we don't generate a __Canon instantiation of this type automatically.
    // This type is used to test that a dependency on a normal canonical generic from a universal generic works correctly.
    public class WrapperGeneric<T> : IGetContainedObject, DummyInterface1
    {
        public T value;

        public object WrappedObject()
        {
            return value;
        }
    }

    public class AllocableGeneric<T, U> : IGetContainedObject
    {
        public T val;
        public object WrappedObject()
        {
            return val;
        }
    }

    public interface DummyInterface1
    { }

    public interface DummyInterface2
    { }

    public interface ITest<T>
    {
        object TestMethod(T t);
    }

    public interface IFace<T, U>
    {
    }

    // DummyInterface is used to ensure that we don't generate a __Canon instantiation of this type automatically.
    // This type is used to test that a dependency on a normal canonical generic from a universal generic works correctly.
    public class UniversalGenericImplementsInterfaceInstantiatedOverTArray<T> : IFace<int, T[]> where T : DummyInterface1, DummyInterface2
    {
    }

    // DummyInterface is used to ensure that we don't generate a __Canon instantiation of this type automatically.
    // This type is used to test that a dependency on a normal canonical generic from a universal generic works correctly.
    public class GenericThatUsesAllocableGeneric<T> : ITest<T> where T : DummyInterface1, DummyInterface2
    {
        public object TestMethod(T t)
        {
            AllocableGeneric<WrapperGeneric<T>, int> canonGenericUsed = new AllocableGeneric<WrapperGeneric<T>,int>();
            WrapperGeneric<T> wrapper = new WrapperGeneric<T>();
            wrapper.value = t;
            canonGenericUsed.val = wrapper;
            return canonGenericUsed;
        }
    }

    public struct StructThatSatisfiesConstraints : DummyInterface1, DummyInterface2
    {
        public int data;
    }

    public class HandleWrapperGenericMethodClass1
    {
        static volatile object s_o;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static public object UnwrapWrapperGeneric<T,U>(T t)
        {
            // Some logic that will force the use of the method's generic dictionary.
            s_o = t;
            t = (T)s_o;

            return ((IGetContainedObject)t).WrappedObject();
        }
    }

    // DummyInterface is used to ensure that we don't generate a __Canon instantiation of this type automatically.
    // This type is used to test that a dependency on a normal canonical generic from a universal generic works correctly.
    public class UseCanonGenericMethodFromUniversalGenericClassType1<T> : ITest<T> where T : DummyInterface1, DummyInterface2
    {
        public object TestMethod(T t)
        {
            WrapperGeneric<T> wrapper = new WrapperGeneric<T>();
            wrapper.value = t;
            return HandleWrapperGenericMethodClass1.UnwrapWrapperGeneric<WrapperGeneric<T>,int>(wrapper);
        }
    }

    public class HandleWrapperGenericMethodClass2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static public object UnwrapWrapperGeneric<T,U>(T t)
        {
            // Some logic that will force the use of the method's generic dictionary.
            object o = Activator.CreateInstance<T>();
            Assert.IsFalse(o.Equals(t));

            return ((IGetContainedObject)t).WrappedObject();
        }
    }

    // DummyInterface is used to ensure that we don't generate a __Canon instantiation of this type automatically.
    // This type is used to test that a dependency on a normal canonical generic from a universal generic works correctly.
    // NOTE: DO NOT USE THIS CLASS OUTSIDE OF UseCanonGenericMethodFromUniversalGenericClassType2. That would stop the
    // test using UseCanonGenericMethodFromUniversalGenericClassType2 from testing Activator.CreateInstance<T> on a type
    // not yet constructed.
    public class WrapperGeneric2<T> : IGetContainedObject, DummyInterface1
    {
        public T value;

        public object WrappedObject()
        {
            return value;
        }
    }
    // DummyInterface is used to ensure that we don't generate a __Canon instantiation of this type automatically.
    // This type is used to test that a dependency on a normal canonical generic from a universal generic works correctly.
    public class UseCanonGenericMethodFromUniversalGenericClassType2<T> : ITest<T> where T : DummyInterface1, DummyInterface2
    {
        public object TestMethod(T t)
        {
            WrapperGeneric2<T> wrapper = new WrapperGeneric2<T>();
            wrapper.value = t;
            return HandleWrapperGenericMethodClass2.UnwrapWrapperGeneric<WrapperGeneric2<T>,int>(wrapper);
        }
    }

    public class BaseClass3 
    { 
        public override string ToString() { return "BaseClass3"; } 
    }
    public class DerivedClass3 : BaseClass3 
    {
        string _id;
        public DerivedClass3(string id) { _id = id; }
        public override string ToString() { return "DerivedClass3." + _id; } 
    }
    public class WrapperGeneric3<T, U> where T : BaseClass3
    {
        public T value;

        public object WrappedObject()
        {
            return value;
        }
    }
    public class UseCanonGenericTypeFromUniversalGenericClassType3<T, U> : ITest<T>
    {
        public object TestMethod(T t)
        {
            WrapperGeneric3<DerivedClass3, List<T>> wrapper = new WrapperGeneric3<DerivedClass3, List<T>>();
            wrapper.value = t as DerivedClass3;
            return wrapper.WrappedObject();
        }
    }

    public class WrapperGeneric4
    {
        public object WrappedObject<T, U>(T t) where T : BaseClass3
        {
            T value = t;
            return value;
        }
    }
    public class UseCanonGenericMethodFromUniversalGenericClassType4<T, U> : ITest<T>
    {
        public object TestMethod(T t)
        {
            WrapperGeneric4  wrapper = new WrapperGeneric4();
            return wrapper.WrappedObject<DerivedClass3, IFace<T, U>>(t as DerivedClass3);
        }
    }

    public struct MyStruct5
    {
        string _id;
        public MyStruct5(string id) { _id = id; }
        public override string ToString() { return "MyStruct5." + _id; }
    }
    public class WrapperGeneric5<T, U> where T : struct
    {
        public T value;

        public object WrappedObject()
        {
            return value;
        }
    }
    public class UseCanonGenericTypeFromUniversalGenericClassType5<T, U> : ITest<T> where T : struct
    {
        public object TestMethod(T t)
        {
            WrapperGeneric5<T, List<T>> wrapper = new WrapperGeneric5<T, List<T>>();
            wrapper.value = t;
            return wrapper.WrappedObject();
        }
    }

    public struct MyStruct6
    {
        string _id;
        public MyStruct6(string id) { _id = id; }
        public override string ToString() { return "MyStruct6." + _id; }
    }
    public class WrapperGeneric6<T, U> where T : struct
    {
        public T value;

        public object WrappedObject()
        {
            return value;
        }
    }
    public class UseCanonGenericTypeFromUniversalGenericClassType6<T, U> : ITest<MyStruct6> where T : struct
    {
        public object TestMethod(MyStruct6 t)
        {
            WrapperGeneric6<MyStruct6, List<T>> wrapper = new WrapperGeneric6<MyStruct6, List<T>>();
            wrapper.value = t;
            return wrapper.WrappedObject();
        }
    }

    public interface IFoo
    {
        string Method3();
        string InterfaceMethod2();
    }
    public abstract class ClassA<T>
    {
        public abstract string Method1();
        public abstract string Method2();
        public abstract string Method3();
        public abstract string Method4();
    }
    public class ClassB<T, U> : ClassA<U>
    {
        public sealed override string Method1() { return String.Format("ClassB<{0},{1}>.Method1", typeof(T).Name, typeof(U).Name); }
        public sealed override string Method2() { return String.Format("ClassB<{0},{1}>.Method2", typeof(T).Name, typeof(U).Name); }
        public sealed override string Method3() { return String.Format("ClassB<{0},{1}>.Method3", typeof(T).Name, typeof(U).Name); }
        public sealed override string Method4() { return String.Format("ClassB<{0},{1}>.Method4", typeof(T).Name, typeof(U).Name); }
    }
    public class ClassC<T> : ClassB<T, MyStruct5>, IFoo
    {
        public string InterfaceMethod2() { return String.Format("ClassC<{0}>.InterfaceMethod2", typeof(T).Name); }
    }
    public class ClassD<T> : ClassB<T, object>, IFoo
    {
        public string InterfaceMethod2() { return String.Format("ClassD<{0}>.InterfaceMethod2", typeof(T).Name); }
    }
    public class ClassE<T> : ClassB<int, object>, IFoo
    {
        public string InterfaceMethod2() { return String.Format("ClassE<{0}>.InterfaceMethod2", typeof(T).Name); }
    }
    public class ClassF<T> : ClassB<string, object>, IFoo
    {
        public string InterfaceMethod2() { return String.Format("ClassF<{0}>.InterfaceMethod2", typeof(T).Name); }
    }

    #region TestDependenciesOfPartialUniversalCanonicalCode
    public struct Bar<T> 
    {
        T _field1;
        T _field2;
        public T TProp1 { get { return _field1; } set { _field1 = value; } }
        public T TProp2 { get { return _field2; } set { _field2 = value; } }
    }
    public struct Foo<T, U>
    {
        T _field1;
        U _field2;
        public T TProp { get { return _field1; } set { _field1 = value; } }
        public U UProp { get { return _field2; } set { _field2 = value; } }

        public static string Method1() { return "Foo<" + typeof(T) + "," + typeof(U) + ">.Method1"; }
        public static string Method2() { return "Foo<" + typeof(T) + "," + typeof(U) + ">.Method2"; }
        public static string GMethod1<V>() { return "Foo<" + typeof(T) + "," + typeof(U) + ">.GMethod1<" + typeof(V) + ">"; }
        public static string GMethod2<V>() { return "Foo<" + typeof(T) + "," + typeof(U) + ">.GMethod2<" + typeof(V) + ">"; }
    }
    public class BaseCaller
    {
        public virtual Func<string> GVMethod1<T>() { return null; }
        public virtual Func<string> GVMethod2<T>() { return null; }

        public virtual string GVMethod3<T>() { return null; }
        public virtual string GVMethod4<T>() { return null; }
    }
    public class DerivedCaller : BaseCaller
    {
        public override Func<string> GVMethod1<T>() { return new Func<string>(Foo<T, Bar<T>>.Method1); }
        public override Func<string> GVMethod2<T>() { return new Func<string>(Foo<T, Bar<T>>.GMethod1<T>); }

        public override string GVMethod3<T>() { return Foo<T, Bar<T>>.Method2(); }
        public override string GVMethod4<T>() { return Foo<T, Bar<T>>.GMethod2<object>(); }
    }
    #endregion

    public class Test
    {
        public static void NullableGetHashCode<T>(T value, int expected) where T : struct
        {
            var comparer = EqualityComparer<T?>.Default;
            IEqualityComparer nonGenericComparer = comparer;

            Assert.AreEqual(expected, comparer.GetHashCode(value));
            Assert.AreEqual(expected, comparer.GetHashCode(value));

            Assert.AreEqual(expected, nonGenericComparer.GetHashCode(value));
            Assert.AreEqual(expected, nonGenericComparer.GetHashCode(value));
        }

        [TestMethod]
        public static void TestOverrideMethodOnDerivedTypeWhereInstantiationArgsAreDifferentThanBaseType()
        {
            // This can be easily tested using NullableEqualityComparer<T>, which derives from EqualityComparer<T?>,
            // and overrides some of the methods. The base type's method signatures will use T's, and the derived type
            // method signatures use Nullable<T>'s.

            MethodInfo mi = typeof(Test).GetTypeInfo().GetDeclaredMethod("NullableGetHashCode").MakeGenericMethod(TypeOf.Char);
            mi.Invoke(null, new object[] { (char)3, ((char)3).GetHashCode() });
        }

        [TestMethod]
        public static void TestUniversalGenericThatDerivesFromBaseInstantiatedOverArray()
        {
            string s = null;

            // Root derived types (these types cause the virtual methods to not be in the sealed vtable)
            new OtherDerivedType<string>().BaseMethod(new string[] { "s" }, ref s);
            new OtherDerivedType<string>().MiddleMethod("s", ref s);
            new OtherDerivedTypeLeadingToNonSharedGenerics<short>().BaseMethod(123, ref s);
            new OtherDerivedTypeLeadingToNonSharedGenerics<short>().MiddleMethod(123, ref s);
            new OtherDerivedTypeWithTwoArgs<string, string>().BaseMethod(new string[] { "s" }, ref s);
            new OtherDerivedTypeWithTwoArgs<string, string>().MiddleMethod("s", ref s);
            new OtherDerivedTypeWithTwoArgsLeadingToPartialAndNonShared<string, string>().BaseMethod(11, ref s);
            new OtherDerivedTypeWithTwoArgsLeadingToPartialAndNonShared<string, string>().MiddleMethod("s", ref s);

            Assert.AreEqual(typeof(PartialUniversalGen.DerivedWithArray<>).Name, TypeOf.PUG_DerivedWithArray.Name);

            {
                var t = TypeOf.PUG_DerivedWithArray.MakeGenericType(TypeOf.Short);
                IBase<short[]> b = (IBase<short[]>)Activator.CreateInstance(t);
                IMiddle<short> m = (IMiddle<short>)b;
                IDerived<short> d = (IDerived<short>)b;

                Assert.AreEqual(typeof(short[][]), b.BaseMethod(new short[0], ref s).GetType());
                Assert.AreEqual("BaseMethod", s);
                Assert.AreEqual(typeof(short[]), m.MiddleMethod(2, ref s).GetType());
                Assert.AreEqual("MiddleMethod", s);
                Assert.AreEqual(typeof(short[]), d.DerivedMethod(1, ref s).GetType());
                Assert.AreEqual("DerivedMethod", s);
            }

            {
                var t = typeof(DerivedWithNonSharedGenerics<>).MakeGenericType(TypeOf.Long);
                IBase<int> b = (IBase<int>)Activator.CreateInstance(t);
                IMiddle<long> m = (IMiddle<long>)b;
                IDerived<long> d = (IDerived<long>)b;

                Assert.AreEqual(typeof(int[]), b.BaseMethod(123, ref s).GetType());
                Assert.AreEqual("BaseMethod", s);
                Assert.AreEqual(typeof(long[]), m.MiddleMethod(2, ref s).GetType());
                Assert.AreEqual("MiddleMethod", s);
                Assert.AreEqual(typeof(long[]), d.DerivedMethod(1, ref s).GetType());
                Assert.AreEqual("DerivedMethod", s);
            }

            {
                var t = typeof(DerivedWithArrayWithTwoArgs<,>).MakeGenericType(TypeOf.Float, TypeOf.Float);
                IBase<float[]> b = (IBase<float[]>)Activator.CreateInstance(t);
                IMiddle<float> m = (IMiddle<float>)b;
                IDerived<float> d = (IDerived<float>)b;

                Assert.AreEqual(typeof(float[][]), b.BaseMethod(new float[0], ref s).GetType());
                Assert.AreEqual("BaseMethod", s);
                Assert.AreEqual(typeof(float[]), m.MiddleMethod(2.0f, ref s).GetType());
                Assert.AreEqual("MiddleMethod", s);
                Assert.AreEqual(typeof(float[]), d.DerivedMethod(1.0f, ref s).GetType());
                Assert.AreEqual("DerivedMethod", s);
            }

            {
                var t = typeof(DerivedWithArrayWithTwoArgsLeadingToPartialAndNonShared<,>).MakeGenericType(TypeOf.Double, TypeOf.Double);
                IBase<int> b = (IBase<int>)Activator.CreateInstance(t);
                IMiddle<double> m = (IMiddle<double>)b;
                IDerived<double> d = (IDerived<double>)b;

                Assert.AreEqual(typeof(int[]), b.BaseMethod(11, ref s).GetType());
                Assert.AreEqual("BaseMethod", s);
                Assert.AreEqual(typeof(double[]), m.MiddleMethod(2.0, ref s).GetType());
                Assert.AreEqual("MiddleMethod", s);
                Assert.AreEqual(typeof(double[]), d.DerivedMethod(1.0, ref s).GetType());
                Assert.AreEqual("DerivedMethod", s);
            }
        }

        [TestMethod]
        public static void TestUniversalGenericThatUsesCanonicalGeneric()
        {
            var t = TypeOf.PUG_GenericThatUsesAllocableGeneric.MakeGenericType(TypeOf.PUG_StructThatSatisfiesConstraints);
            ITest<StructThatSatisfiesConstraints> b = (ITest<StructThatSatisfiesConstraints>)Activator.CreateInstance(t);

            StructThatSatisfiesConstraints dataStruct;
            dataStruct.data = 123897;

            IGetContainedObject outerWrapper = (IGetContainedObject)b.TestMethod(dataStruct);
            IGetContainedObject innerWrapper = (IGetContainedObject)outerWrapper.WrappedObject();
            Assert.AreEqual(dataStruct, (StructThatSatisfiesConstraints)innerWrapper.WrappedObject());
        }

        [TestMethod]
        public static void TestUniversalGenericThatImplementsInterfaceOverArrayType()
        {
            var t = TypeOf.PUG_UniversalGenericImplementsInterfaceInstantiatedOverTArray.MakeGenericType(TypeOf.PUG_StructThatSatisfiesConstraints);
            var o = Activator.CreateInstance(t);
        }

        [TestMethod]
        public static void TestUniversalGenericThatUsesCanonicalGenericMethod()
        {
            var t = TypeOf.PUG_UseCanonGenericMethodFromUniversalGenericClassType1.MakeGenericType(TypeOf.PUG_StructThatSatisfiesConstraints);
            ITest<StructThatSatisfiesConstraints> b = (ITest<StructThatSatisfiesConstraints>)Activator.CreateInstance(t);

            StructThatSatisfiesConstraints dataStruct;
            dataStruct.data = 1234567;
            Assert.AreEqual(dataStruct, b.TestMethod(dataStruct));
        }

        // This test is just like the above test, but it finds an interesting issue with Activator.CreateInstance<T> default constructor slots
        [TestMethod]
        public static void TestUniversalGenericThatUsesCanonicalGenericMethodWithActivatorCreateInstance()
        {
            var t = TypeOf.PUG_UseCanonGenericMethodFromUniversalGenericClassType2.MakeGenericType(TypeOf.PUG_StructThatSatisfiesConstraints);
            ITest<StructThatSatisfiesConstraints> b = (ITest<StructThatSatisfiesConstraints>)Activator.CreateInstance(t);

            StructThatSatisfiesConstraints dataStruct;
            dataStruct.data = 1234567;
            Assert.AreEqual(dataStruct, b.TestMethod(dataStruct));
        }

        [TestMethod]
        public static void TestUniversalGenericThatUsesCanonicalGenericType()
        {
            {
                var t = TypeOf.PUG_UseCanonGenericTypeFromUniversalGenericClassType3.MakeGenericType(typeof(DerivedClass3), TypeOf.Double);
                ITest<DerivedClass3> b = (ITest<DerivedClass3>)Activator.CreateInstance(t);

                DerivedClass3 data = new DerivedClass3("abc");
                Assert.AreEqual("DerivedClass3.abc", b.TestMethod(data).ToString());
            }

            {
                var t = TypeOf.PUG_UseCanonGenericTypeFromUniversalGenericClassType5.MakeGenericType(typeof(MyStruct5), TypeOf.Double);
                ITest<MyStruct5> b = (ITest<MyStruct5>)Activator.CreateInstance(t);

                MyStruct5 data = new MyStruct5("123");
                Assert.AreEqual("MyStruct5.123", b.TestMethod(data).ToString());
            }

            {
                var t = TypeOf.PUG_UseCanonGenericTypeFromUniversalGenericClassType6.MakeGenericType(typeof(MyStruct6), TypeOf.Double);
                ITest<MyStruct6> b = (ITest<MyStruct6>)Activator.CreateInstance(t);

                MyStruct6 data = new MyStruct6("456");
                Assert.AreEqual("MyStruct6.456", b.TestMethod(data).ToString());
            }
        }

        [TestMethod]
        public static void TestUniversalGenericThatUsesCanonicalGenericMethodWithConstraints()
        {
            var t = TypeOf.PUG_UseCanonGenericMethodFromUniversalGenericClassType4.MakeGenericType(typeof(DerivedClass3), TypeOf.Double);
            ITest<DerivedClass3> b = (ITest<DerivedClass3>)Activator.CreateInstance(t);

            DerivedClass3 data = new DerivedClass3("def");
            Assert.AreEqual("DerivedClass3.def", b.TestMethod(data).ToString());
        }

        [TestMethod]
        public static void TestDependenciesOfPartialUniversalCanonicalCode()
        {
            // Dummy code to root fields
            {
                var b = new Bar<string>();
                b.TProp1 = b.TProp1;
                b.TProp2 = b.TProp2;
                var f = new Foo<string, string>();
                f.TProp = f.TProp;
                f.UProp = f.UProp;
            }

            Type[] typeArgs = new Type[] {
                TypeOf.Double,
                TypeOf.Long,
                TypeOf.CommonType1,
                TypeOf.Float,
                TypeOf.Short,
                TypeOf.Char
            };

            foreach (Type arg in typeArgs)
            {
                {
                    MethodInfo GVMethod1 = typeof(BaseCaller).GetTypeInfo().GetDeclaredMethod("GVMethod1").MakeGenericMethod(arg);
                    Func<string> del = (Func<string>)GVMethod1.Invoke(new DerivedCaller(), null);
                    string result = del();
                    Assert.AreEqual("Foo<" + arg + ",PartialUniversalGen.Bar`1[" + arg + "]>.Method1", result);
                }
                {
                    MethodInfo GVMethod2 = typeof(BaseCaller).GetTypeInfo().GetDeclaredMethod("GVMethod2").MakeGenericMethod(arg);
                    Func<string> del = (Func<string>)GVMethod2.Invoke(new DerivedCaller(), null);
                    string result = del();
                    Assert.AreEqual("Foo<" + arg + ",PartialUniversalGen.Bar`1[" + arg + "]>.GMethod1<" + arg + ">", result);
                }
                {
                    MethodInfo GVMethod3 = typeof(BaseCaller).GetTypeInfo().GetDeclaredMethod("GVMethod3").MakeGenericMethod(arg);
                    string result = (string)GVMethod3.Invoke(new DerivedCaller(), null);
                    Assert.AreEqual("Foo<" + arg + ",PartialUniversalGen.Bar`1[" + arg + "]>.Method2", result);
                }
                {
                    MethodInfo GVMethod4 = typeof(BaseCaller).GetTypeInfo().GetDeclaredMethod("GVMethod4").MakeGenericMethod(arg);
                    string result = (string)GVMethod4.Invoke(new DerivedCaller(), null);
                    Assert.AreEqual("Foo<" + arg + ",PartialUniversalGen.Bar`1[" + arg + "]>.GMethod2<System.Object>", result);
                }
            }
        }

        [TestMethod]
        public static void TestCornerCaseSealedVTableSlot()
        {
            MethodInfo mi = typeof(Test).GetTypeInfo().GetDeclaredMethod("TestCornerCaseSealedVTableSlot_Inner").MakeGenericMethod(TypeOf.Short);
            mi.Invoke(null, new object[] { });
        }

        public static void TestCornerCaseSealedVTableSlot_Inner<T>()
        {
            // Do NOT call Method3 in this test except on the interface (hits a rare case in the binder)

            // Root some calls on the abstract type
            ClassA<Type> obj = (ClassA<Type>)new ClassB<Type, Type>();
            obj.Method1();
            obj.Method2();
            obj.Method4();

            var obj1 = new ClassC<T>();
            Assert.AreEqual("ClassB<Int16,MyStruct5>.Method1", obj1.Method1());
            Assert.AreEqual("ClassB<Int16,MyStruct5>.Method2", obj1.Method2());
            Assert.AreEqual("ClassB<Int16,MyStruct5>.Method4", obj1.Method4());
            Assert.AreEqual("ClassB<Int16,MyStruct5>.Method3", ((IFoo)obj1).Method3());
            Assert.AreEqual("ClassC<Int16>.InterfaceMethod2", ((IFoo)obj1).InterfaceMethod2());

            var obj2 = new ClassD<T>();
            Assert.AreEqual("ClassB<Int16,Object>.Method1", obj2.Method1());
            Assert.AreEqual("ClassB<Int16,Object>.Method2", obj2.Method2());
            Assert.AreEqual("ClassB<Int16,Object>.Method4", obj2.Method4());
            Assert.AreEqual("ClassB<Int16,Object>.Method3", ((IFoo)obj2).Method3());
            Assert.AreEqual("ClassD<Int16>.InterfaceMethod2", ((IFoo)obj2).InterfaceMethod2());

            var obj3 = new ClassE<T>();
            Assert.AreEqual("ClassB<Int32,Object>.Method1", obj3.Method1());
            Assert.AreEqual("ClassB<Int32,Object>.Method2", obj3.Method2());
            Assert.AreEqual("ClassB<Int32,Object>.Method4", obj3.Method4());
            Assert.AreEqual("ClassB<Int32,Object>.Method3", ((IFoo)obj3).Method3());
            Assert.AreEqual("ClassE<Int16>.InterfaceMethod2", ((IFoo)obj3).InterfaceMethod2());

            var obj4 = new ClassF<T>();
            Assert.AreEqual("ClassB<String,Object>.Method1", obj4.Method1());
            Assert.AreEqual("ClassB<String,Object>.Method2", obj4.Method2());
            Assert.AreEqual("ClassB<String,Object>.Method4", obj4.Method4());
            Assert.AreEqual("ClassB<String,Object>.Method3", ((IFoo)obj4).Method3());
            Assert.AreEqual("ClassF<Int16>.InterfaceMethod2", ((IFoo)obj4).InterfaceMethod2());
        }
    }
}
