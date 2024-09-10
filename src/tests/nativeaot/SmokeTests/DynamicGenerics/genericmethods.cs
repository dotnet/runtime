// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using RuntimeLibrariesTest;
using TypeOfRepo;


namespace MakeGenMethod
{
    #region Test Types For Dynamic Type/Method Creationg
    public enum MyEnumType
    {
        Val1 = 1,
        Val2 = 2,
        Val3 = 3,
    }

    public class GenericType<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GenericFunc<U>()
        {
            return typeof(T) + "::" + typeof(U);
        }
    }
    
    public class NonGenericType
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string VerySimpleGenericMethod<U>()
        {
            // ... that has an empty generic dictionary
            return "i'm useless!";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string FuncWithManyEnumArgsAndDefaultValues<T>(
            T myT,
            MyEnumType arg10 = MyEnumType.Val2,
            MyEnumType arg11 = MyEnumType.Val3,
            MyEnumType arg12 = MyEnumType.Val3,
            MyEnumType arg13 = MyEnumType.Val3,
            MyEnumType arg14 = MyEnumType.Val3,
            MyEnumType arg15 = MyEnumType.Val3,
            MyEnumType arg16 = MyEnumType.Val3,
            MyEnumType arg17 = MyEnumType.Val3,
            MyEnumType arg18 = MyEnumType.Val3,
            MyEnumType arg19 = MyEnumType.Val3)
        {
            return String.Format("FuncWithManyEnumArgsAndDefaultValues<{0}>: {1}", typeof(T), arg10);
        }
    }
    
    public class Gen2<T>
    {
       [MethodImpl(MethodImplOptions.NoInlining)]
        public static string M<U>()
        {
            return typeof(T).ToString() + "::" + typeof(U).ToString();
        }
    }

    public class Gen3<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string M()
        {
            return typeof(T).ToString();
        }
    }

    public class Gen4
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string M<T>()
        {
            return typeof(T).ToString();
        }
    }

    public class Foo<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GenericFooFunc1<M>() { return this.GetType() + "::GenericFooFunc1 called on " + typeof(T) + "::" + typeof(M); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GenericFooFunc2<M>() { return this.GetType() + "::GenericFooFunc2 called on " + typeof(T) + "::" + typeof(M); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string NonGenericFooFunc1() { return this.GetType() + "::NonGenericFooFunc1 called on " + typeof(T); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string NonGenericFooFunc2() { return this.GetType() + "::NonGenericFooFunc2 called on " + typeof(T); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StaticGenericFooFunc1<M>() { return "StaticGenericFooFunc1 called on " + typeof(T) + "::" + typeof(M); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StaticGenericFooFunc2<M>() { return "StaticGenericFooFunc2 called on " + typeof(T) + "::" + typeof(M); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StaticNonGenericFooFunc1() { return "StaticNonGenericFooFunc1 called on " + typeof(T); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StaticNonGenericFooFunc2() { return "StaticNonGenericFooFunc2 called on " + typeof(T); }
    }
    public class Bar<T> : Foo<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GenericFooFunc3<M>() { return this.GetType() + "::GenericFooFunc3 called on " + typeof(T) + "::" + typeof(M); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string NonGenericFooFunc3() { return this.GetType() + "::NonGenericFooFunc3 called on " + typeof(T); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StaticGenericFooFunc3<M>() { return "StaticGenericFooFunc3 called on " + typeof(T) + "::" + typeof(M); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StaticNonGenericFooFunc3() { return "StaticNonGenericFooFunc3 called on " + typeof(T); }
    }

    public class Gen<T>
    {
       public Gen()
       {
       }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string TestMethodDict<U>()
        {
            return Gen2<T>.M<U>() + "::" + Gen3<U>.M() + "::" + Gen4.M<U>();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public Object InstanceMethodTest<U>(int i, Object o, T t, U u)
        {
            switch (i)
            {
                case 0:
                    return typeof(T) + "::" + typeof(U);
                case 1:
                    return typeof(List<T>) + "::" + typeof(List<U>);
                case 2:
                    return ((IList<U>)o).Count;
                case 3:
                    return typeof(T[]) + "::" + typeof(U[]);
                case 4:
                    return TestMethodDict<U>();
                case 51:
                    return ((IList<IList<T>>)o).Count;
                case 52:
                    return ((IList<IList<U>>)o).Count;
                case 6:
                    return t.GetType() + "::" + u.GetType();
                default:
                    break;
            }
            return null;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Object StaticMethodTest<U>(int i, Object o, T t, U u)
        {
            switch (i)
            {
                case 0:
                    return typeof(T) + "::" + typeof(U);
                case 1:
                    return typeof(List<T>) + "::" + typeof(List<U>);
                case 2:
                    return ((IList<U>)o).Count;
                case 3:
                    return typeof(T[]) + "::" + typeof(U[]);
                case 4:
                    return TestMethodDict<U>();
                case 51:
                    return ((IList<IList<T>>)o).Count;
                case 52:
                    return ((IList<IList<U>>)o).Count;
                case 6:
                    return t.GetType() + "::" + u.GetType();
                default:
                    break;
            }
            return null;
        }
    }
    #endregion

    
    public class Test
    {
        // Basic MakeGenericMethod testing
        #region MakeGenericMethod and Dictionary Testing
        static void RunTest(Type containingType, MethodInfo testMethod, object thisPtr, object T_object, object U_object, object List_U_objects)
        {
            var result = testMethod.Invoke(thisPtr, new object[] { 0, null, T_object, U_object });
            Assert.AreEqual(T_object.GetType() + "::" + U_object.GetType(), result.ToString());

            result = testMethod.Invoke(thisPtr, new object[] { 1, null, T_object, U_object });
            Assert.AreEqual("System.Collections.Generic.List`1[" + T_object.GetType() + "]::" + "System.Collections.Generic.List`1[" + U_object.GetType() + "]", result.ToString());

            result = testMethod.Invoke(thisPtr, new object[] { 2, List_U_objects, T_object, U_object });
            Assert.AreEqual(2, result);

            result = testMethod.Invoke(thisPtr, new object[] { 3, null, T_object, U_object });
            Assert.AreEqual(T_object.GetType() + "[]::" + U_object.GetType() + "[]", result.ToString());

            result = testMethod.Invoke(thisPtr, new object[] { 4, null, T_object, U_object });
            Assert.AreEqual(T_object.GetType() + "::" + U_object.GetType() + "::" + U_object.GetType() + "::" + U_object.GetType(), result.ToString());

            try
            {
                result = testMethod.Invoke(thisPtr, new object[] { 51, T_object, T_object, U_object });
            }
            catch (TargetInvocationException ex)
            {
                Assert.AreEqual(typeof(InvalidCastException), ex.InnerException.GetType());
            }
            try
            {
                result = testMethod.Invoke(thisPtr, new object[] { 52, U_object, T_object, U_object });
            }
            catch (TargetInvocationException ex)
            {
                Assert.AreEqual(typeof(InvalidCastException), ex.InnerException.GetType());
            }

            result = testMethod.Invoke(thisPtr, new object[] { 6, null, T_object, U_object });
            Assert.AreEqual(T_object.GetType() + "::" + U_object.GetType(), result.ToString());
        }

        [TestMethod]
        public static void TestInstanceMethod()
        {
            // Run test a couple of times to hit cases where methods are already created and registered
            TestInstanceMakeGenericMethod_Inner();
            TestInstanceMakeGenericMethod_Inner();
            TestInstanceMakeGenericMethod_Inner();
        }
        static void TestInstanceMakeGenericMethod_Inner()
        {
            // Dynamically created method on a dynamically created type
            Type genOfType = TypeOf.GM_Gen.MakeGenericType(TypeOf.CommonType1);
            MethodInfo testMethod = genOfType.GetTypeInfo().GetDeclaredMethod("InstanceMethodTest").MakeGenericMethod(new Type[] { TypeOf.CommonType2 });
            object thisPtr = Activator.CreateInstance(genOfType);
            object T_object = new CommonType1();
            object U_object = new CommonType2();

            var listType = TypeOf.List.MakeGenericType(TypeOf.CommonType2);
            var List_U_objects = Activator.CreateInstance(listType);
            var listAddMethod = listType.GetTypeInfo().GetDeclaredMethod("Add");
            listAddMethod.Invoke(List_U_objects, new object[] { new CommonType2() });
            listAddMethod.Invoke(List_U_objects, new object[] { new CommonType2() });
            RunTest(genOfType, testMethod, thisPtr, T_object, U_object, List_U_objects);
            
            // Static entries exist for the following generic type instantiation and generic method instance
            genOfType = TypeOf.GM_Gen.MakeGenericType(TypeOf.Object);
            testMethod = genOfType.GetTypeInfo().GetDeclaredMethod("InstanceMethodTest").MakeGenericMethod(new Type[] { TypeOf.Object });
            thisPtr = Activator.CreateInstance(genOfType);
            T_object = new object();
            U_object = new object();
            RunTest(genOfType, testMethod, thisPtr, T_object, U_object, new List<object>() { new object(), new object() });
        }

        [TestMethod]
        public static void TestStaticMethod()
        {
            // Run test a couple of times to hit cases where methods are already created and registered
            TestStaticMakeGenericMethod_Inner();
            TestStaticMakeGenericMethod_Inner();
            TestStaticMakeGenericMethod_Inner();
        }
        static void TestStaticMakeGenericMethod_Inner()
        {
            // Dynamically created method on a dynamically created type
            Type genOfType = TypeOf.GM_Gen.MakeGenericType(TypeOf.CommonType3);
            MethodInfo testMethod = genOfType.GetTypeInfo().GetDeclaredMethod("StaticMethodTest").MakeGenericMethod(new Type[] { TypeOf.CommonType4 });
            object T_object = new CommonType3();
            object U_object = new CommonType4();

            var listType = TypeOf.List.MakeGenericType(TypeOf.CommonType4);
            var List_U_objects = Activator.CreateInstance(listType);
            var listAddMethod = listType.GetTypeInfo().GetDeclaredMethod("Add");
            listAddMethod.Invoke(List_U_objects, new object[] { new CommonType4() });
            listAddMethod.Invoke(List_U_objects, new object[] { new CommonType4() });
            RunTest(genOfType, testMethod, null, T_object, U_object, List_U_objects);
            
            // Static entries exist for the following generic type instantiation and generic method instance
            genOfType = TypeOf.GM_Gen.MakeGenericType(TypeOf.Object);
            testMethod = genOfType.GetTypeInfo().GetDeclaredMethod("StaticMethodTest").MakeGenericMethod(new Type[] { TypeOf.Object });
            T_object = new object();
            U_object = new object();
            RunTest(genOfType, testMethod, null, T_object, U_object, new List<object>() { new object(), new object() });
        }

        [TestMethod]
        public static void TestGenericMethodsWithEnumParametersHavingDefaultValues()
        {
            TypeInfo ti = typeof(NonGenericType).GetTypeInfo();

            // Avoid using "typeof(...)" to prevent toolchain analysis from producing the exact instantiations, and use
            // dynamic instantiations at runtime during the Invoke call.

            // Normal shared generics case
            MethodInfo mi = ti.GetDeclaredMethod("FuncWithManyEnumArgsAndDefaultValues").MakeGenericMethod(TypeOf.Type);
            string result = (string)mi.Invoke(new NonGenericType(), new object[] { typeof(string), null, null, null, null, null, null, null, null, null, null });
            Assert.AreEqual("FuncWithManyEnumArgsAndDefaultValues<System.Type>: 0", result);

#if UNIVERSAL_GENERICS
            // Universal shared generics case
            mi = ti.GetDeclaredMethod("FuncWithManyEnumArgsAndDefaultValues").MakeGenericMethod(TypeOf.Double);
            result = (string)mi.Invoke(new NonGenericType(), new object[] { 12.34, null, null, null, null, null, null, null, null, null, null });
            Assert.AreEqual("FuncWithManyEnumArgsAndDefaultValues<System.Double>: 0", result);

            mi = ti.GetDeclaredMethod("FuncWithManyEnumArgsAndDefaultValues").MakeGenericMethod(TypeOf.Int32);
            result = (string)mi.Invoke(new NonGenericType(), new object[] { 123, null, null, null, null, null, null, null, null, null, null });
            Assert.AreEqual("FuncWithManyEnumArgsAndDefaultValues<System.Int32>: 0", result);
#endif
        }

        [TestMethod]
        public static void TestNoDictionaries()
        {
            // Run test a couple of times to hit cases where methods are already created and registered
            TestGenericMethod_NoDictionaries_Inner();
            TestGenericMethod_NoDictionaries_Inner();
            TestGenericMethod_NoDictionaries_Inner();
        }
        static void TestGenericMethod_NoDictionaries_Inner()
        {
            MethodInfo testMethod = TypeOf.GM_NonGenericType.GetTypeInfo().GetDeclaredMethod("VerySimpleGenericMethod").MakeGenericMethod(new Type[] { TypeOf.CommonType1 });
            var result = testMethod.Invoke(new NonGenericType(), null);
            Assert.AreEqual("i'm useless!", result.ToString());
        }

        [TestMethod]
        public static void TestGenMethodOnGenType()
        {
            // Run test a couple of times to hit cases where methods are already created and registered
            TestGenericMethod_GenMethodOnGenType_Inner();
            TestGenericMethod_GenMethodOnGenType_Inner();
            TestGenericMethod_GenMethodOnGenType_Inner();
        }
        static void TestGenericMethod_GenMethodOnGenType_Inner()
        {
            RunTest_GenMethodOnGenType(TypeOf.CommonType1, TypeOf.CommonType2);
            RunTest_GenMethodOnGenType(TypeOf.CommonType1, TypeOf.CommonType1);
            RunTest_GenMethodOnGenType(TypeOf.CommonType1, TypeOf.CommonType3);

            RunTest_GenMethodOnGenType(TypeOf.CommonType2, TypeOf.CommonType2);
            RunTest_GenMethodOnGenType(TypeOf.CommonType2, TypeOf.CommonType1);
            RunTest_GenMethodOnGenType(TypeOf.CommonType2, TypeOf.CommonType3);

            RunTest_GenMethodOnGenType(TypeOf.CommonType3, TypeOf.CommonType2);
            RunTest_GenMethodOnGenType(TypeOf.CommonType3, TypeOf.CommonType1);
            RunTest_GenMethodOnGenType(TypeOf.CommonType3, TypeOf.CommonType3);
        }
        static void RunTest_GenMethodOnGenType(Type typeArg, Type methodArg)
        {
            var type = TypeOf.GM_GenericType.MakeGenericType(typeArg);
            var method = type.GetTypeInfo().GetDeclaredMethod("GenericFunc").MakeGenericMethod(methodArg);
            var result = (string)method.Invoke(Activator.CreateInstance(type), null);
            Assert.AreEqual(typeArg + "::" + methodArg, result);
        }
        #endregion


        // Tests focusing on reverse lookups:
        //      - RuntimeMethodHandle -> declaring type + method handle + generic method args
        //      - Function pointer -> declaring type + method handle + generic method args lookup
        #region MakeGenericMethod with Delegates and MethodHandles Testing
        [TestMethod]
        public static void TestReverseLookups()
        {
            TestGenericMethod_ReverseLookups_Inner();
            TestGenericMethod_ReverseLookups_Inner();
            TestGenericMethod_ReverseLookups_Inner();
        }
        static void TestGenericMethod_ReverseLookups_Inner()
        {
#if !USC    // BUG with reflection invoke codegen
            RunTest_Delegate(TypeOf.CommonType1, TypeOf.Object);
            RunTest_Delegate(TypeOf.CommonType1, TypeOf.String);

            RunTest_Delegate(TypeOf.CommonType2, TypeOf.Object);
            RunTest_Delegate(TypeOf.CommonType2, TypeOf.String);

            RunTest_Delegate(TypeOf.CommonType3, TypeOf.Object);
            RunTest_Delegate(TypeOf.CommonType3, TypeOf.String);
#endif

            // delegates for interface non-generic methods on dynamically created generic interface instantiations
            // require fat function pointers with:
            //      - target method pointer =   static method on the interface type
            //      - method dictioanry     =   dictionary of the interface type instantiation

            foreach (var funcid in new string[] { "1", "2", "3" })
            {
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType1, TypeOf.CommonType1, "GenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType3, TypeOf.CommonType4, "GenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType5, TypeOf.CommonType4, "GenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType6, TypeOf.CommonType3, "GenericFooFunc" + funcid);

                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType1, null, "NonGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType2, null, "NonGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType7, null, "NonGenericFooFunc" + funcid);

                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType1, TypeOf.CommonType1, "StaticGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType8, TypeOf.CommonType4, "StaticGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType9, TypeOf.CommonType4, "StaticGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType8, TypeOf.CommonType9, "StaticGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType7, TypeOf.CommonType2, "StaticGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType10, TypeOf.CommonType2, "StaticGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType7, TypeOf.CommonType10, "StaticGenericFooFunc" + funcid);

                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType1, null, "StaticNonGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType11, null, "StaticNonGenericFooFunc" + funcid);
                RunTest_MethodOnGenericTypeDelegate(TypeOf.CommonType6, null, "StaticNonGenericFooFunc" + funcid);
            }
        }
        public static TResult MethodForDelegate<T, TResult>(T inputParam)
        {
            return (TResult)((object)(typeof(T) + "::" + typeof(TResult)));
        }
        static void RunTest_Delegate(Type t_in, Type t_out)
        {
            MethodInfo method = TypeOf.GM_MakeGenericMethodTest.GetTypeInfo().GetDeclaredMethod("MethodForDelegate").MakeGenericMethod(new Type[] { t_in, t_out });

            var delegateType = TypeOf.Func2.MakeGenericType(new Type[] { t_in, t_out });
            Assert.AreEqual("System.Func`2[" + t_in + "," + t_out + "]", delegateType.ToString());

            Delegate d = method.CreateDelegate(delegateType);
            Assert.AreEqual(delegateType.ToString(), d.ToString());
            Assert.AreEqual(method.ToString(), d.GetMethodInfo().ToString());
            Assert.AreEqual(t_out + " MethodForDelegate[" + t_in.Name + "," + t_out.Name + "](" + t_in + ")", d.GetMethodInfo().ToString());

            var result = d.DynamicInvoke(new object[] { null });
            Assert.AreEqual(t_in + "::" + t_out, result.ToString());
        }
        static void RunTest_MethodOnGenericTypeDelegate(Type typeArg, Type methodArg, string methodName)
        {
            Type type = (methodName.EndsWith("3") ? TypeOf.GM_Bar.MakeGenericType(typeArg) : TypeOf.GM_Foo.MakeGenericType(typeArg));
            object inst = Activator.CreateInstance(TypeOf.GM_Bar.MakeGenericType(typeArg));

            var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
            if (methodArg != null)
                method = method.MakeGenericMethod(methodArg);

            Delegate d;
            Type delegateType = TypeOf.Func1.MakeGenericType(TypeOf.String);
            if (method.IsStatic)
                d = method.CreateDelegate(delegateType, null);
            else
                d = method.CreateDelegate(delegateType, inst);
            Assert.AreEqual(method.ToString(), d.GetMethodInfo().ToString());

            var result = d.DynamicInvoke();
            var expected = methodName + " called on " + typeArg;
            if (methodArg != null) expected += "::" + methodArg;
            if (!method.IsStatic) expected = inst.GetType().ToString() + "::" + expected;
            Assert.AreEqual(expected, result.ToString());

            if (method.IsStatic)
            {
                var result2 = method.Invoke(null, null);
                Assert.AreEqual(result.ToString(), result2.ToString());
            }
            else
            {
                var result2 = method.Invoke(inst, null);
                Assert.AreEqual(result.ToString(), result2.ToString());
            }
        }

        [TestMethod]
        public static void TestReverseLookupsWithArrayArg()
        {
            var staticallyCreatedDelegate = new Func<CommonType10[], CommonType10[]>(MethodForDelegate<CommonType10[], CommonType10[]>);

            var dynamicallyCreatedArray = TypeOf.CommonType10.MakeArrayType();

            MethodInfo method = TypeOf.GM_MakeGenericMethodTest.GetTypeInfo().GetDeclaredMethod("MethodForDelegate").MakeGenericMethod(new Type[] { dynamicallyCreatedArray, dynamicallyCreatedArray });

            var dynamicallyCreatedDelegate = method.CreateDelegate(typeof(Func<CommonType10[], CommonType10[]>));

            Assert.AreEqual(staticallyCreatedDelegate, dynamicallyCreatedDelegate);
        }
        #endregion
    }
}
