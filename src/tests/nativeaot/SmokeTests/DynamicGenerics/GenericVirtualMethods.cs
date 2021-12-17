// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using CoreFXTestLibrary;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public static class GenericVirtualMethods
{
    public interface IInterfaceWithGVM
    {
        string GVM<T>(object o);
    }
    
    class GVMClass : IInterfaceWithGVM
    {
        public virtual string GVM<T>(object o) 
        {
            if (!(o is T))
                return "FAIL";
            else 
                return "Called GVM<T>"; 
        }
    }

    class GVMDerivedClass : GVMClass, IInterfaceWithGVM
    {
        public override string GVM<T>(object o) 
        {
            if (o == null)
                return "FAIL";

            if (!(o is T))
                return "FAIL2";
            else
            {
                if (o.GetType() != typeof(T) && o.GetType() != typeof(GVMDerivedClass))
                    return "FAIL3";

                if (typeof(T) == typeof(int))
                {
                    return "Called Derived.GVM<int>";
                }
                if (typeof(T) == typeof(GVMDerivedClass))
                {
                    return "Called Derived.GVM<GVMDerivedClass>";
                }
                if (typeof(T) == typeof(GVMClass))
                {
                    return "Called Derived.GVM<GVMClass>";
                }
                if (typeof(T) == typeof(string))
                {
                    return "Called Derived.GVM<string>";
                }
                return "Called GVMStruct.GVM<Unknown>";
            }
        }
    }

    struct GVMStruct : IInterfaceWithGVM
    {
        public string GVM<T>(object o)
        {
            if (o == null)
                return "FAIL";

            if (!(o is T))
                return "FAIL2";
            else
            {
                if (o.GetType() != typeof(T))
                    return "FAIL3";

                if (typeof(T) == typeof(int))
                {
                    return "Called GVMStruct.GVM<int>";
                }
                if (typeof(T) == typeof(GVMDerivedClass))
                {
                    return "Called GVMStruct.GVM<GVMDerivedClass>";
                }
                if (typeof(T) == typeof(string))
                {
                    return "Called GVMStruct.GVM<string>";
                }
                return "Called GVMStruct.GVM<Unknown>";
            }
        }
    }

    struct GVMStructGeneric<U> : IInterfaceWithGVM
    {
        public string GVM<T>(object o)
        {
            if (o == null)
                return "FAIL";

            if (!(o is T))
                return "FAIL2";
            else
            {
                if (o.GetType() != typeof(T))
                    return "FAIL3";

                if (typeof(U) == typeof(int))
                {
                    if (typeof(T) == typeof(int))
                    {
                        return "Called GVMStructGeneric<int>.GVM<int>";
                    }
                    if (typeof(T) == typeof(GVMDerivedClass))
                    {
                        return "Called GVMStructGeneric<int>.GVM<GVMDerivedClass>";
                    }
                    if (typeof(T) == typeof(string))
                    {
                        return "Called GVMStructGeneric<int>.GVM<string>";
                    }
                    return "Called GVMStructGeneric<int>.GVM<Unknown>";
                }
                else if (typeof(U) == typeof(object))
                {
                    if (typeof(T) == typeof(int))
                    {
                        return "Called GVMStructGeneric<object>.GVM<int>";
                    }
                    if (typeof(T) == typeof(GVMDerivedClass))
                    {
                        return "Called GVMStructGeneric<object>.GVM<GVMDerivedClass>";
                    }
                    if (typeof(T) == typeof(string))
                    {
                        return "Called GVMStructGeneric<object>.GVM<string>";
                    }
                    return "Called GVMStructGeneric<object>.GVM<Unknown>";
                }
                return "Called GVMStructGeneric<unknown>.GVM<Unknown>";
            }
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestConstrainedCalls<T> (T t, string intString, string derivedClassString, string stringString) where T:IInterfaceWithGVM
    {
        Assert.AreEqual(intString, t.GVM<int>(54));
        Assert.AreEqual(derivedClassString, t.GVM<GVMDerivedClass>(new GVMDerivedClass()));
        Assert.AreEqual(stringString, t.GVM<string>("testString"));
    }
    
     
    //TEST:GenericVirtualMethods.TestCalls
    [TestMethod]
    public static void TestCalls()
    {
        GVMClass testObject = new GVMDerivedClass();
        IInterfaceWithGVM igvm = testObject;

        // Test normal GVM call
        Assert.AreEqual("Called Derived.GVM<int>", testObject.GVM<int>(54));
        Assert.AreEqual("Called Derived.GVM<GVMClass>", testObject.GVM<GVMClass>(testObject));
        Assert.AreEqual("Called Derived.GVM<string>", testObject.GVM<string>("testString"));
        Assert.AreEqual("Called Derived.GVM<int>", igvm.GVM<int>(54));
        Assert.AreEqual("Called Derived.GVM<GVMDerivedClass>", igvm.GVM<GVMDerivedClass>(testObject));
        Assert.AreEqual("Called Derived.GVM<string>", igvm.GVM<string>("testString"));

        // Test GVM delegate dispatch
        Func<object, string> d;
        d = testObject.GVM<int>;
        Assert.AreEqual("Called Derived.GVM<int>", d(54));
        d = testObject.GVM<GVMClass>;
        Assert.AreEqual("Called Derived.GVM<GVMClass>", d(testObject));
        d = testObject.GVM<string>;
        Assert.AreEqual("Called Derived.GVM<string>", d("testString"));
        d = igvm.GVM<int>;
        Assert.AreEqual("Called Derived.GVM<int>", d(54));
        d = igvm.GVM<GVMClass>;
        Assert.AreEqual("Called Derived.GVM<GVMClass>", d(testObject));
        d = igvm.GVM<string>;
        Assert.AreEqual("Called Derived.GVM<string>", d("testString"));
        TestConstrainedCalls<IInterfaceWithGVM>(igvm, "Called Derived.GVM<int>", "Called Derived.GVM<GVMDerivedClass>", "Called Derived.GVM<string>");

        // GVM on structure
        IInterfaceWithGVM igvmStruct = new GVMStruct();
        Assert.AreEqual("Called GVMStruct.GVM<int>", igvmStruct.GVM<int>(54));
        Assert.AreEqual("Called GVMStruct.GVM<GVMDerivedClass>", igvmStruct.GVM<GVMDerivedClass>(testObject));
        Assert.AreEqual("Called GVMStruct.GVM<string>", igvmStruct.GVM<string>("testString"));
        TestConstrainedCalls<GVMStruct>(new GVMStruct(), "Called GVMStruct.GVM<int>", "Called GVMStruct.GVM<GVMDerivedClass>", "Called GVMStruct.GVM<string>");
        TestConstrainedCalls<IInterfaceWithGVM>(new GVMStruct(), "Called GVMStruct.GVM<int>", "Called GVMStruct.GVM<GVMDerivedClass>", "Called GVMStruct.GVM<string>");

        // GVM on Generic Structure (struct instantiated over exact type)
        IInterfaceWithGVM igvmStructGenericOverInt = new GVMStructGeneric<int>();
        Assert.AreEqual("Called GVMStructGeneric<int>.GVM<int>", igvmStructGenericOverInt.GVM<int>(54));
        Assert.AreEqual("Called GVMStructGeneric<int>.GVM<GVMDerivedClass>", igvmStructGenericOverInt.GVM<GVMDerivedClass>(testObject));
        Assert.AreEqual("Called GVMStructGeneric<int>.GVM<string>", igvmStructGenericOverInt.GVM<string>("testString"));
        TestConstrainedCalls<GVMStructGeneric<int>>(new GVMStructGeneric<int>(), "Called GVMStructGeneric<int>.GVM<int>", "Called GVMStructGeneric<int>.GVM<GVMDerivedClass>", "Called GVMStructGeneric<int>.GVM<string>");
        TestConstrainedCalls<IInterfaceWithGVM>(new GVMStructGeneric<int>(), "Called GVMStructGeneric<int>.GVM<int>", "Called GVMStructGeneric<int>.GVM<GVMDerivedClass>", "Called GVMStructGeneric<int>.GVM<string>");

        // GVM on Generic Structure (struct instantiated over reference type)
        IInterfaceWithGVM igvmStructGenericOverObject = new GVMStructGeneric<object>();
        Assert.AreEqual("Called GVMStructGeneric<object>.GVM<int>", igvmStructGenericOverObject.GVM<int>(54));
        Assert.AreEqual("Called GVMStructGeneric<object>.GVM<GVMDerivedClass>", igvmStructGenericOverObject.GVM<GVMDerivedClass>(testObject));
        Assert.AreEqual("Called GVMStructGeneric<object>.GVM<string>", igvmStructGenericOverObject.GVM<string>("testString"));
        TestConstrainedCalls<GVMStructGeneric<object>>(new GVMStructGeneric<object>(), "Called GVMStructGeneric<object>.GVM<int>", "Called GVMStructGeneric<object>.GVM<GVMDerivedClass>", "Called GVMStructGeneric<object>.GVM<string>");
        TestConstrainedCalls<IInterfaceWithGVM>(new GVMStructGeneric<object>(), "Called GVMStructGeneric<object>.GVM<int>", "Called GVMStructGeneric<object>.GVM<GVMDerivedClass>", "Called GVMStructGeneric<object>.GVM<string>");
    }

    static class GenericStaticClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Function()
        {
            if (typeof(T) == typeof(string))
            {
                return "string";
            }
            else if (typeof(T) == typeof(object))
            {
                return "object";
            }
            else
            {
                return "unknown";
            }
        }
    }

    class ClassThatUsesGenericStaticClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Func<string> GetTypeName()
        {
            return new Func<string>(GenericStaticClass<T>.Function);
        }
    }

    //TEST:GenericVirtualMethods.TestLdFtnToGetStaticMethodOnGenericType
    [TestMethod]
    public static void TestLdFtnToGetStaticMethodOnGenericType()
    {
        ClassThatUsesGenericStaticClass<object> testObject1 = new ClassThatUsesGenericStaticClass<object>();
        ClassThatUsesGenericStaticClass<string> testObject2 = new ClassThatUsesGenericStaticClass<string>();
        ClassThatUsesGenericStaticClass<Type> testObject3 = new ClassThatUsesGenericStaticClass<Type>();
        string result;

        result = testObject1.GetTypeName()();
        Console.WriteLine(result);
        Assert.IsTrue("object" == result);
        result = testObject2.GetTypeName()();
        Console.WriteLine(result);
        Assert.IsTrue("string" == result);
        result = testObject3.GetTypeName()();
        Console.WriteLine(result);
        Assert.IsTrue("unknown" == result);
    }

    public class GenericTypeWithThreeParameters<X, Y, Z>
    {
        public GenericTypeWithThreeParameters(int x)
        {
        }

        private GenericTypeWithThreeParameters(String s)
        {
        }

        public void SimpleMethod()
        {
        }

        public M SimpleGenericMethod<M, N>(X arg1, N arg2)
        {
            return default(M);
        }
    }

    public class GenericTypeWithTwoParametersWhereOnlyTheFirstIsActuallyUsed<T,U>
    {
        public static bool Method(object o)
        {
            return o is T;
        }
    }

    public class GenericTypeWhereNoParametersAreUsed<T>
    {
        public static bool Method(object o)
        {
            return o is string;
        }
    }

    public class ClassWithGenericMethod
    {
        public static bool MethodWithTwoGenericParametersWhereOnlyTheFirstIsUsed<T,U>(object o)
        {
            return o is T;
        }
    }

    //TEST:GenericVirtualMethods.TestLdFtnToInstanceGenericMethod
    [TestMethod]
    public static void TestLdFtnToInstanceGenericMethod()
    {
        GenericTypeWithThreeParameters<int, string, object> o = new GenericTypeWithThreeParameters<int, string, object>(123);
        Func<int, double, float> d = new Func<int, double, float>(o.SimpleGenericMethod<float, double>);
        Assert.AreEqual(default(float), d(1, 2));
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d));

        GenericTypeWithThreeParameters<int, string, Type> o2 = new GenericTypeWithThreeParameters<int, string, Type>(123);
        Func<int, double, float> d2 = new Func<int, double, float>(o2.SimpleGenericMethod<float, double>);
        Assert.AreEqual(default(float), d2(1, 2));
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d2));

        // Start checking to ensure that duplicate and/or empty generic dictionaries don't result in not having
        // the correct equality behavior

        // Empty Generic MethodDictionaries
        Assert.AreNotEqual(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d), System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d2));

        // Duplicate Generic Method Dictionaries
        Func<object, bool> d7 = ClassWithGenericMethod.MethodWithTwoGenericParametersWhereOnlyTheFirstIsUsed<object, string>;
        Func<object, bool> d8 = ClassWithGenericMethod.MethodWithTwoGenericParametersWhereOnlyTheFirstIsUsed<object, Type>;
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d7));
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d8));
        Assert.AreNotEqual(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d7), System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d8));

        // Empty Generic Type Dictionaries
        Func<object, bool> d3 = GenericTypeWhereNoParametersAreUsed<object>.Method;
        Func<object, bool> d4 = GenericTypeWhereNoParametersAreUsed<string>.Method;
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d3));
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d4));
        Assert.AreNotEqual(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d3), System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d4));

        // Duplicate Generic Type Dictionary
        Func<object, bool> d5 = GenericTypeWithTwoParametersWhereOnlyTheFirstIsActuallyUsed<object, string>.Method;
        Func<object, bool> d6 = GenericTypeWithTwoParametersWhereOnlyTheFirstIsActuallyUsed<object, Type>.Method;
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d5));
        Assert.IsNotNull(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d6));
        Assert.AreNotEqual(System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d5), System.Reflection.RuntimeReflectionExtensions.GetMethodInfo(d6));
    }

    class Exception<T> : Exception
    {

    }
    class MyGenericTypeWithExceptionCatchingSupport<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public MyGenericTypeWithExceptionCatchingSupport(bool b)
        {
            object o = 3;
            try
            {
                if (b)
                    throw new Exception<T>();
            }
            catch (Exception<T> e)
            {
                o = e;
            }
            if (!(o is Exception<T>))
            {
                throw new ArgumentException();
            }
        }

        public IEnumerable<string> EnumerateStrings(bool b)
        {
            yield return "x";
            
            List<string> strings = new List<string>();
            strings.Add("Hmm");
            try
            {
                bool x = !b;
                if (!x)
                    throw new Exception<T>();
            }
            catch (Exception<T> e)
            {
                strings.Add("caught - " + e.ToString());
            }

            foreach (string s in strings)
                yield return s;
        }

        void ThrowGenericException()
        {
           throw new Exception<T>();
        }

        Task ThrowGenericExceptionAsync()
        {
            Task t = Task.Run(new Action(ThrowGenericException));
            return t;
        }

        public async Task<bool> AsyncMethod(bool b)
        {
            
            try
            {
                await ThrowGenericExceptionAsync();
            }
            catch (Exception<T>)
            {
                return true;
            }

            return false;
        }
        public bool RunAsyncMethod(bool b)
        {
            Task<bool> t = AsyncMethod(b);
            t.Wait();
            return t.Result;
        }
    }

    //TEST:GenericVirtualMethods.TestGenericExceptionType
    [TestMethod]
    public static void TestGenericExceptionType()
    {
        MyGenericTypeWithExceptionCatchingSupport<string> mgt = new MyGenericTypeWithExceptionCatchingSupport<string>(true);

        bool foundCaught = false;
        foreach (string s in mgt.EnumerateStrings(true))
        {
            if (s.Contains("caught"))
                foundCaught = true;
        }

        Assert.IsTrue(foundCaught);
        Assert.IsTrue(mgt.RunAsyncMethod(true));
    }

    class Base
    {

    }

    class Derived : Base
    {

    }

    interface IInVariant<in T>
    {
        string Func<U>(T t);
    }

    interface IOutVariant<out T>
    {
        string Func<U>();
    }

    class ClassWithVariantGvms : IInVariant<object>, IInVariant<Base>, IOutVariant<Derived>, IOutVariant<Base>
    {
        string IInVariant<object>.Func<U>(object t)
        {
            return "CallOnObject";
        }
        string IInVariant<Base>.Func<U>(Base t)
        {
            return "CallOnBase";
        }

        string IOutVariant<Derived>.Func<U>()
        {
            return "CallOnDerived";
        }
        string IOutVariant<Base>.Func<U>()
        {
            return "CallOnBase";
        }
    }

    //TEST:GenericVirtualMethods.TestCoAndContraVariantCalls
    [TestMethod]
    public static void TestCoAndContraVariantCalls()
    {
        ClassWithVariantGvms testClass = new ClassWithVariantGvms();

        Assert.AreEqual<string>("CallOnObject"  , ((IInVariant<object>)testClass).Func<object>(new Derived()));
        Assert.AreEqual<string>("CallOnBase"    , ((IInVariant<Base   >)testClass).Func<object>(new Derived()));
        Assert.AreEqual<string>("CallOnObject"  , ((IInVariant<Derived>)testClass).Func<object>(new Derived()));

        Assert.AreEqual<string>("CallOnDerived" , ((IOutVariant<object>)testClass).Func<object>());
        Assert.AreEqual<string>("CallOnBase"    , ((IOutVariant<Base>)testClass).Func<object>());
        Assert.AreEqual<string>("CallOnDerived" , ((IOutVariant<Derived>)testClass).Func<object>());
    }
}

