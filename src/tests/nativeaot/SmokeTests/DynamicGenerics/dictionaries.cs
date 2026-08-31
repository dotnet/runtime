// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using CoreFXTestLibrary;
using TypeOfRepo;


namespace Dictionaries
{
    public abstract class Base
    {
        public abstract Object Test(int i, Object o);
    }

    public struct GenericStruct<T>
    {
    }

    public class NullableTest<T> : Base where T : struct
    {
        public override Object Test(int i, Object o)
        {
            switch (i)
            {
                case 0:
                    T? nullable_t = (T?)o;
                    return nullable_t.HasValue ? nullable_t.Value : default(T);
                case 1:
                    return o is T?;
                case 2:
                    T?[] a = new T?[3];
                    a.SetValue(o, 1);
                    if (o == null)
                    {
                        Assert.IsTrue(!a[1].HasValue);
                        return default(T);
                    }
                    else
                        return (T)a[1];
            }
            return null;
        }
    }
    public delegate Object DelWithNullable<T>(Nullable<T> o) where T : struct;
    public class DelegateTarget<T> where T : struct
    {
        public static Object DelWithNullableTarget(T? n)
        {
            Assert.IsTrue(n is T?);
            if (n.HasValue)
                return (T)n;
            return default(T);
        }
    }

    public abstract class GenBase<T> : Base
    {
       public override Object Test(int i, Object o) { return null; }
    }

    public interface IFace<T>
    {
        int InterfaceMethod();
    }

    public interface IFace2<T>
    {
        string Interface2Method();
    }

    public interface IFace3<T>
    {
        string Interface3Method();
    }

    public interface IDerivedIFace<T> : IFace2<T>
    {
        string IDerivedIFaceMethod();
    }

    public class Gen2<T>
    {
       [MethodImpl(MethodImplOptions.NoInlining)]
        public static string M()
        {
            return typeof(T).ToString();
        }
    }

    public class Gen<T> : GenBase<T>, IFace<T>, IDerivedIFace<long>, IFace2<int>, IFace3<T>, IFace2<string>, IDerivedIFace<string>
    {
        public Gen()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string TestTypeDict()
        {
            return Gen2<T>.M();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public override Object Test(int i, Object o)
        {
            switch (i)
            {
                case 0:
                    return typeof(T);
                case 1:
                    return typeof(List<T>);
                case 2:
                    return ((IList<T>)o).Count;
                case 3:
                    return typeof(T[]);
                case 4:
                    return TestTypeDict();
                case 5:
                    return ((IList<IList<T>>)o).Count;
                default:
                    break;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int InterfaceMethod() { return 42; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        string IFace2<int>.Interface2Method() { return "IFace2<int>.Interface2Method on Gen<" + typeof(T) + ">"; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual string Interface2Method() { return "Interface2Method on Gen<" + typeof(T) + ">"; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual string Interface3Method() { return "Interface3Method on Gen<" + typeof(T) + ">"; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        string IDerivedIFace<long>.IDerivedIFaceMethod() { return "IDerivedIFace<long>.IDerivedIFaceMethod on Gen<" + typeof(T) + ">"; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual string IDerivedIFaceMethod() { return "IDerivedIFaceMethod on Gen<" + typeof(T) + ">"; }
    }

    // Do not extend this or use it for anything. You might accidentally
    // make it stop testing what this is supposed to test (DEK_ARRAY_TYPE).
    public class SingleUseArrayOnlyGen<T> : GenBase<T>
    {
        public override Object Test(int i, object o)
        {
            return new T[0][][];
        }
    }

    public class DictionariesTest
    {
        [TestMethod]
        public static void TestBasicDictionaryEntryTypes()
        {
            Type genOfType = TypeOf.D_Gen.MakeGenericType(TypeOf.CommonType1);
            Type genBaseOfType = TypeOf.D_GenBase.MakeGenericType(TypeOf.CommonType1);
            var genOf = (Base)Activator.CreateInstance(genOfType);
            MethodInfo Test_Method = genOfType.GetTypeInfo().GetDeclaredMethod("Test");
            MethodInfo GenBaseTest_Method = genBaseOfType.GetTypeInfo().GetDeclaredMethod("Test");
            MethodInfo BaseTest_Method = TypeOf.D_Base.GetTypeInfo().GetDeclaredMethod("Test");

            Console.WriteLine("Testing typeof() dictionary entries");
            var result0 = genOf.Test(0, null);
            Assert.AreEqual(result0.ToString(), "CommonType1");
            result0 = Test_Method.Invoke(genOf, new object[] { 0, null });
            Assert.AreEqual(result0.ToString(), "CommonType1");
            result0 = BaseTest_Method.Invoke(genOf, new object[] { 0, null });
            Assert.AreEqual(result0.ToString(), "CommonType1");
            result0 = GenBaseTest_Method.Invoke(genOf, new object[] { 0, null });
            Assert.AreEqual(result0.ToString(), "CommonType1");

            var result1 = genOf.Test(1, null);
            Assert.AreEqual(result1.ToString(), "System.Collections.Generic.List`1[CommonType1]");
            result1 = Test_Method.Invoke(genOf, new object[] { 1, null });
            Assert.AreEqual(result1.ToString(), "System.Collections.Generic.List`1[CommonType1]");
            result1 = BaseTest_Method.Invoke(genOf, new object[] { 1, null });
            Assert.AreEqual(result1.ToString(), "System.Collections.Generic.List`1[CommonType1]");
            result1 = GenBaseTest_Method.Invoke(genOf, new object[] { 1, null });
            Assert.AreEqual(result1.ToString(), "System.Collections.Generic.List`1[CommonType1]");

            var result2 = genOf.Test(3, null);
            Assert.AreEqual(result2.ToString(), "CommonType1[]");
            result2 = Test_Method.Invoke(genOf, new object[] { 3, null });
            Assert.AreEqual(result2.ToString(), "CommonType1[]");
            result2 = BaseTest_Method.Invoke(genOf, new object[] { 3, null });
            Assert.AreEqual(result2.ToString(), "CommonType1[]");
            result2 = GenBaseTest_Method.Invoke(genOf, new object[] { 3, null });
            Assert.AreEqual(result2.ToString(), "CommonType1[]");

            var result3 = genOf.Test(4, null);
            Assert.AreEqual(result3.ToString(), "CommonType1");
            result3 = Test_Method.Invoke(genOf, new object[] { 4, null });
            Assert.AreEqual(result3.ToString(), "CommonType1");
            result3 = BaseTest_Method.Invoke(genOf, new object[] { 4, null });
            Assert.AreEqual(result3.ToString(), "CommonType1");
            result3 = GenBaseTest_Method.Invoke(genOf, new object[] { 4, null });
            Assert.AreEqual(result3.ToString(), "CommonType1");

            Console.WriteLine("Testing interface dispatch dictionary entries");
            var listType = TypeOf.List.MakeGenericType(TypeOf.CommonType1);
            var listObject = Activator.CreateInstance(listType);
            var listAddMethod = listType.GetTypeInfo().GetDeclaredMethod("Add");
            listAddMethod.Invoke(listObject, new object[] { new CommonType1() });
            listAddMethod.Invoke(listObject, new object[] { new CommonType1() });
            listAddMethod.Invoke(listObject, new object[] { new CommonType1() });
            var result4 = genOf.Test(2, listObject);
            Assert.AreEqual(result4, 3);
            result4 = Test_Method.Invoke(genOf, new object[] { 2, listObject });
            Assert.AreEqual(result4, 3);
            result4 = BaseTest_Method.Invoke(genOf, new object[] { 2, listObject });
            Assert.AreEqual(result4, 3);
            result4 = GenBaseTest_Method.Invoke(genOf, new object[] { 2, listObject });
            Assert.AreEqual(result4, 3);

            Console.WriteLine("Testing invalid cast");
            try
            {
                genOf.Test(5, genOf);
                Console.WriteLine("Didn't throw expected exception!");
                Assert.AreEqual(false, true);
            }
            catch (InvalidCastException)
            {
            }
            try
            {
                Test_Method.Invoke(genOf, new object[] { 5, genOf });
                Console.WriteLine("Didn't throw expected exception!");
                Assert.AreEqual(false, true);
            }
            catch (TargetInvocationException ex)
            {
                Assert.AreEqual(ex.InnerException.GetType(), typeof(InvalidCastException));
            }

            object result_iface;
            Console.WriteLine("Testing interface dispatch");
            {
                result_iface = ((IFace<CommonType1>)genOf).InterfaceMethod();
                Assert.AreEqual(result_iface, 42);

                result_iface = ((IFace2<int>)genOf).Interface2Method();
                Assert.AreEqual(result_iface, "IFace2<int>.Interface2Method on Gen<" + TypeOf.CommonType1 + ">");

                result_iface = ((IFace2<string>)genOf).Interface2Method();
                Assert.AreEqual(result_iface, "Interface2Method on Gen<" + TypeOf.CommonType1 + ">");

                result_iface = ((IDerivedIFace<long>)genOf).IDerivedIFaceMethod();
                Assert.AreEqual(result_iface, "IDerivedIFace<long>.IDerivedIFaceMethod on Gen<" + TypeOf.CommonType1 + ">");

                result_iface = ((IDerivedIFace<string>)genOf).IDerivedIFaceMethod();
                Assert.AreEqual(result_iface, "IDerivedIFaceMethod on Gen<" + TypeOf.CommonType1 + ">");

                // IFace2<long>/<string> comes from the inheritance of IDerivedIFace<long>/<string>
                result_iface = ((IFace2<long>)genOf).Interface2Method();
                Assert.AreEqual(result_iface, "Interface2Method on Gen<" + TypeOf.CommonType1 + ">");

                result_iface = ((IFace2<string>)genOf).Interface2Method();
                Assert.AreEqual(result_iface, "Interface2Method on Gen<" + TypeOf.CommonType1 + ">");
            }

            // Reflection calls for statically existing interface instantiations
            {
                MethodInfo InterfaceMethod_Method = typeof(IFace2<int>).GetTypeInfo().GetDeclaredMethod("Interface2Method");
                result_iface = InterfaceMethod_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, "IFace2<int>.Interface2Method on Gen<" + TypeOf.CommonType1 + ">");

                InterfaceMethod_Method = typeof(IFace2<string>).GetTypeInfo().GetDeclaredMethod("Interface2Method");
                result_iface = InterfaceMethod_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, "Interface2Method on Gen<" + TypeOf.CommonType1 + ">");

                InterfaceMethod_Method = typeof(IFace<CommonType1>).GetTypeInfo().GetDeclaredMethod("InterfaceMethod");
                result_iface = (int)InterfaceMethod_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, 42);

                InterfaceMethod_Method = typeof(IDerivedIFace<long>).GetTypeInfo().GetDeclaredMethod("IDerivedIFaceMethod");
                result_iface = InterfaceMethod_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, "IDerivedIFace<long>.IDerivedIFaceMethod on Gen<" + TypeOf.CommonType1 + ">");

                InterfaceMethod_Method = typeof(IDerivedIFace<string>).GetTypeInfo().GetDeclaredMethod("IDerivedIFaceMethod");
                result_iface = InterfaceMethod_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, "IDerivedIFaceMethod on Gen<" + TypeOf.CommonType1 + ">");

                // IFace2<long>/<string> comes from the inheritance of IDerivedIFace<long>/<string>
                InterfaceMethod_Method = typeof(IFace2<long>).GetTypeInfo().GetDeclaredMethod("Interface2Method");
                result_iface = InterfaceMethod_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, "Interface2Method on Gen<" + TypeOf.CommonType1 + ">");

                InterfaceMethod_Method = typeof(IFace2<string>).GetTypeInfo().GetDeclaredMethod("Interface2Method");
                result_iface = InterfaceMethod_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, "Interface2Method on Gen<" + TypeOf.CommonType1 + ">");
            }

            // Reflection calls for dynamically created interface instantiation
            {
                Type IFace3Of = TypeOf.D_IFace3.MakeGenericType(TypeOf.CommonType1);
                MethodInfo Interface3Method_Method = IFace3Of.GetTypeInfo().GetDeclaredMethod("Interface3Method");
                result_iface = Interface3Method_Method.Invoke(genOf, new object[0]);
                Assert.AreEqual(result_iface, "Interface3Method on Gen<" + TypeOf.CommonType1 + ">");
            }

            Type singleUseArrayOnlyGenOfType = TypeOf.D_SingleUseArrayOnlyGen.MakeGenericType(TypeOf.CommonType1);
            Test_Method = singleUseArrayOnlyGenOfType.GetTypeInfo().GetDeclaredMethod("Test");
            var singleUseArrayOnlyGenOf = (Base)Activator.CreateInstance(singleUseArrayOnlyGenOfType);
            var result6 = singleUseArrayOnlyGenOf.Test(0, null);
            Assert.AreEqual(result6.GetType().ToString(), "CommonType1[][][]");
            result6 = Test_Method.Invoke(singleUseArrayOnlyGenOf, new object[] { 0, null });
            Assert.AreEqual(result6.GetType().ToString(), "CommonType1[][][]");
            result6 = BaseTest_Method.Invoke(singleUseArrayOnlyGenOf, new object[] { 0, null });
            Assert.AreEqual(result6.GetType().ToString(), "CommonType1[][][]");
            result6 = GenBaseTest_Method.Invoke(singleUseArrayOnlyGenOf, new object[] { 0, null });
            Assert.AreEqual(result6.GetType().ToString(), "CommonType1[][][]");
        }

        [TestMethod]
        public static void StaticMethodFolding_Test()
        {
            var genOfType = TypeOf.D_Gen.MakeGenericType(TypeOf.CommonType1);
            var result = genOfType.GetTypeInfo().GetDeclaredMethod("TestTypeDict").Invoke(null, new object[0]);
            Assert.AreEqual(result.ToString(), "CommonType1");

            var temp = typeof(Gen<string>);
            var staticallyExistingGenOfType = TypeOf.D_Gen.MakeGenericType(TypeOf.String);
            result = staticallyExistingGenOfType.GetTypeInfo().GetDeclaredMethod("TestTypeDict").Invoke(null, new object[0]);
            Assert.AreEqual(result.ToString(), "System.String");

            temp = typeof(Gen<Type>);
            staticallyExistingGenOfType = TypeOf.D_Gen.MakeGenericType(TypeOf.Type);
            result = staticallyExistingGenOfType.GetTypeInfo().GetDeclaredMethod("TestTypeDict").Invoke(null, new object[0]);
            Assert.AreEqual(result.ToString(), "System.Type");
        }

        [TestMethod]
        public static void NullableTesting()
        {
            NullableTesting_Inner(TypeOf.CommonType1, TypeOf.CommonType2);
            NullableTesting_Inner(TypeOf.CommonType2, TypeOf.CommonType3);
            NullableTesting_Inner(TypeOf.CommonType3, TypeOf.CommonType1);
        }
        static void NullableTesting_Inner(Type arg1, Type arg2)
        {
            var structOf = TypeOf.D_GenericStruct.MakeGenericType(arg1);

            var structInst1 = Activator.CreateInstance(TypeOf.D_GenericStruct.MakeGenericType(arg1));
            var structInst2 = Activator.CreateInstance(TypeOf.D_GenericStruct.MakeGenericType(arg2));

            var nullableTestOf = TypeOf.D_NullableTest.MakeGenericType(structOf);
            Base test = (Base)Activator.CreateInstance(nullableTestOf);

            // Type cast T -> T?
            {
                var result = test.Test(0, null);
                Assert.AreEqual(structOf, result.GetType());
                Assert.IsTrue(structInst1 != result);

                result = test.Test(0, structInst1);
                Assert.AreEqual(structOf, result.GetType());
                Assert.IsTrue(structInst1.Equals(result));
            }

            // is T?
            {
                var result = test.Test(1, null);
                Assert.IsTrue((bool)result == false);

                result = test.Test(1, structInst1);
                Assert.IsTrue((bool)result == true);

                result = test.Test(1, structInst2);
                Assert.IsTrue((bool)result == false);
            }

            // Arrays of T?
            {
                object result = test.Test(2, null);
                Assert.AreEqual(structOf, result.GetType());

                result = test.Test(2, structInst1);
                Assert.IsTrue(structInst1.Equals(result));

                try
                {
                    test.Test(2, structInst2);
                    Console.WriteLine("Didn't throw expected exception!");
                    Assert.AreEqual(false, true);
                }
                catch (InvalidCastException)
                {
                }
            }

            // Delegates taking Nullable<T>
            {
                var targetType = TypeOf.D_DelegateTarget.MakeGenericType(structOf);
                var delegateType = TypeOf.D_DelWithNullable.MakeGenericType(structOf);
                var targetMethod = targetType.GetTypeInfo().GetDeclaredMethod("DelWithNullableTarget");

                Delegate d = targetMethod.CreateDelegate(delegateType, null);
                Object[] args = { structInst1 };
                object result = d.DynamicInvoke(args);
                Assert.IsTrue(structInst1.Equals(result));
            }
        }
    }
}

namespace TypeDictTestTypes
{
    public class MyClass5 { }

    public class MyClass4<X, Y>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M(X x, Y y, string typename)
        {
            Assert.AreEqual(typeof(X) + "," + typeof(Y).ToString(), typename + ",TypeDictTestTypes.MyClass5");

            // Recursive reference
            TypeDictTestTypes.MyClass1<X>.M2(x, typename);
            TypeDictTestTypes.MyClass1<Y>.M2(y, "TypeDictTestTypes.MyClass5");
        }
    }
    public class MyClass3<U, V>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M(U u, V v, string typename)
        {
            Assert.AreEqual(typeof(U) + "," + typeof(V).ToString(), "TypeDictTestTypes.MyClass5," + typename);
            TypeDictTestTypes.MyClass4<V, U>.M(v, u, typename);
        }
    }

    public class MyClass7 { public int f; }

    public class MyClass6<T, U> where T : MyClass7
    {
        public int f;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M(T t) 
        {
            // VSO Bug 237266. This will become a MDIL_BOX_ANY operation when T is instantiated over
            // Universal Canon. The NUTC optimizer doesn't model the incoming parameter "t" as being
            // dereferenced, therefore the initialization to the parameter on the caller side will
            // be removed, firing the assert. 
            MyClass7 o = (MyClass7)t;
            Assert.AreEqual(o.f, 100);
        }
    }


    public class MyClass2<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M(T t, string typename)
        {
            Assert.AreEqual(typeof(T).ToString(), typename);
            TypeDictTestTypes.MyClass3<MyClass5, T>.M(new MyClass5(), t, typename);
            MyClass7 o = new MyClass7();
            // VSO Bug 237266. The assignment will be removed as the following instantiation will
            // be on Universal Canon. See the comment inside MyClass6.M.
            o.f = 100;
            TypeDictTestTypes.MyClass6<MyClass7, T>.M(o);
        }
    }
        
    public class MyClass1<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M(T t, string typename)
        {
            Assert.AreEqual(typeof(T).ToString(), typename);
            TypeDictTestTypes.MyClass2<T>.M(t, typename);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M2(T t, string typename)
        {
            Assert.AreEqual(typeof(T).ToString(), typename);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void M3(T t, string typename)
        {
            M(t, typename);
        }
    }

    public struct MyStruct
    {
        public int m_IntMember;
        public string m_StringMember;
    }

    public class DictionariesTest
    {
        [TestMethod]
        public static void TestGenericTypeDictionary()
        {
            TypeDictionaryTestInstance(TypeOf.CommonType1, new object[] { new CommonType1(), "CommonType1" });
            TypeDictionaryTestInstance(TypeOf.CommonType2, new object[] { new CommonType2(), "CommonType2" });
            TypeDictionaryTestInstance(TypeOf.Int32, new object[] { 1, "System.Int32" });
            TypeDictionaryTestInstance(TypeOf.Bool, new object[] { true, "System.Boolean" });
            TypeDictionaryTestInstance(TypeOf.TDT_MyStruct, new object[] { new MyStruct { m_IntMember = 2, m_StringMember = "fff" }, "TypeDictTestTypes.MyStruct" });
        }

        static void TypeDictionaryTestInstance(Type typeArg, object[] parameters)
        {
            Type genMyClass1OfTypeArg = TypeOf.TDT_MyClass1.MakeGenericType(typeArg);

            var methodM = genMyClass1OfTypeArg.GetTypeInfo().GetDeclaredMethod("M3");
            var instanceObj = Activator.CreateInstance(genMyClass1OfTypeArg);

            Assert.AreEqual(instanceObj.GetType(), genMyClass1OfTypeArg);
            Assert.AreEqual(instanceObj.GetType().ToString(), genMyClass1OfTypeArg.ToString());

            methodM.Invoke(instanceObj, parameters);
        }
    }
}

namespace MethodDictionaryTest
{
    public class MyClass1<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M()
        {
            DictionariesTest.s_Result.AppendLine("MyClass1<" + typeof(T).Name + ">.M()");
        }
    }

    public class MyClass2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void M<T>()
        {
            DictionariesTest.s_Result.AppendLine("MyClass2.M<" + typeof(T).Name + ">()");
        }
    }

    public class Yahoo<X>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SGenM<Y>()
        {
            DictionariesTest.s_Result.AppendLine("Yahoo<" + typeof(X).Name + ">.SGenM<" + typeof(Y).Name + ">()");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenM<Y>(bool recurse = true)
        {
            DictionariesTest.s_Result.AppendLine("Yahoo<" + typeof(X).Name + ">.GenM<" + typeof(Y).Name + ">()");

            if (recurse)
            {
                GenM<Y>(false);
                GenM<X>(false);
                MyClass1<X>.M();
                MyClass1<Y>.M();
                MyClass2.M<X>();
                MyClass2.M<Y>();
                Yahoo<X>.SGenM<Y>();
                Yahoo<X>.SGenM<X>();
                Yahoo<Y>.SGenM<Y>();
                Yahoo<Y>.SGenM<X>();
            }
        }
    }

    public class Foo
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenM<M, N>()
        {
            DictionariesTest.s_Result.AppendLine("Foo.GenM<" + typeof(M).Name + "," + typeof(N).Name + ">()");
            new Yahoo<N>().GenM<M>();
        }
    }

    public class Bar<T, U>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void M1()
        {
            DictionariesTest.s_Result.AppendLine("Bar<" + typeof(T).Name + "," + typeof(U).Name + ">.M1()");
            new Foo().GenM<U, T>();
        }
    }

    public class DictionariesTest
    {
        public static StringBuilder s_Result;

        static void DoTest(Type inst1, Type inst2)
        {
            s_Result = new StringBuilder();

            var t = TypeOf.MDT_Bar.MakeGenericType(new Type[] { inst1, inst2 });
            var o = Activator.CreateInstance(t);
            var mi = t.GetTypeInfo().GetDeclaredMethod("M1");
            mi.Invoke(o, null);

            StringBuilder expected = new StringBuilder();
            expected.AppendLine("Bar<" + inst1.Name + "," + inst2.Name + ">.M1()");
            expected.AppendLine("Foo.GenM<" + inst2.Name + "," + inst1.Name + ">()");
            expected.AppendLine("Yahoo<" + inst1.Name + ">.GenM<" + inst2.Name + ">()");
            expected.AppendLine("Yahoo<" + inst1.Name + ">.GenM<" + inst2.Name + ">()");
            expected.AppendLine("Yahoo<" + inst1.Name + ">.GenM<" + inst1.Name + ">()");
            expected.AppendLine("MyClass1<" + inst1.Name + ">.M()");
            expected.AppendLine("MyClass1<" + inst2.Name + ">.M()");
            expected.AppendLine("MyClass2.M<" + inst1.Name + ">()");
            expected.AppendLine("MyClass2.M<" + inst2.Name + ">()");
            expected.AppendLine("Yahoo<" + inst1.Name + ">.SGenM<" + inst2.Name + ">()");
            expected.AppendLine("Yahoo<" + inst1.Name + ">.SGenM<" + inst1.Name + ">()");
            expected.AppendLine("Yahoo<" + inst2.Name + ">.SGenM<" + inst2.Name + ">()");
            expected.AppendLine("Yahoo<" + inst2.Name + ">.SGenM<" + inst1.Name + ">()");

            using (StringReader expectedReader = new StringReader(expected.ToString()))
            {
                using (StringReader resultReader = new StringReader(s_Result.ToString()))
                {
                    string expectedLine = expectedReader.ReadLine();
                    string resultLine = resultReader.ReadLine();
                    Assert.AreEqual(expectedLine, resultLine);
                }
            }
        }
        [TestMethod]
        public static void TestMethodDictionaries()
        {
            DoTest(TypeOf.CommonType2, TypeOf.CommonType2);
            DoTest(TypeOf.CommonType1, TypeOf.CommonType2);
            DoTest(TypeOf.CommonType2, TypeOf.CommonType1);
            DoTest(TypeOf.CommonType1, TypeOf.CommonType1);
        }
    }
}

namespace BaseTypeDict
{
    public class MyClass1
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        virtual public string M1() { return "MyClass1.M1()"; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        virtual public string M2() { return "MyClass1.M2()"; }
    }

    public class MyClass2<T> : MyClass1
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M1() { return "MyClass2`1<" + typeof(T).Name + ">.M1() - " + base.M1(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M2() { return "MyClass2`1<" + typeof(T).Name + ">.M2() - " + base.M2(); }
    }

    public class MyClass3<T> : MyClass2<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        new virtual public string M1() { return "MyClass3`1<" + typeof(T).Name + ">.M1() - " + base.M1(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M2() { return "MyClass3`1<" + typeof(T).Name + ">.M2() - " + base.M2(); }
    }

    public class MyClass4<T> : MyClass3<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M1() { return "MyClass4`1<" + typeof(T).Name + ">.M1() - " + base.M1(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M2() { return "MyClass4`1<" + typeof(T).Name + ">.M2() - " + base.M2(); }
    }

    public class MyClass3_2 : MyClass2<MyClass1>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        new virtual public string M1() { return "MyClass3_2.M1() - " + base.M1(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M2() { return "MyClass3_2.M2() - " + base.M2(); }
    }

    public class MyClass4_2<T> : MyClass3_2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M1() { return "MyClass4_2`1<" + typeof(T).Name + ">.M1() - " + base.M1(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M2() { return "MyClass4_2`1<" + typeof(T).Name + ">.M2() - " + base.M2(); }
    }

    public class MyClass4_3<T> : MyClass2<MyClass1>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M1() { return "MyClass4_3`1<" + typeof(T).Name + ">.M1() - " + base.M1(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string M2() { return "MyClass4_3`1<" + typeof(T).Name + ">.M2() - " + base.M2(); }
    }

#if USC
    public struct Foo2 { }
#else
    public class Foo1
    {
    }

    public class Foo2 : Foo1
    {
    }
#endif

    public class GenBase2<T, U>
    {
        public virtual string M() { return "GenBase2<" + typeof(T).Name + "," + typeof(U).Name + ">"; }
    }

    public class GenBase1<T> : GenBase2<int, T>
    {
        public new virtual string M() { return "GenBase1<" + typeof(T).Name + "> - " + base.M(); }
    }

    public class Gen1<T> : GenBase1<T>
    {
        public new virtual string M() { return "Gen1<" + typeof(T).Name + "> - " + base.M(); }
    }

    public class Gen2<T> : GenBase1<int>
    {
        public new virtual string M() { return "Gen2<" + typeof(T).Name + "> - " + base.M(); }
    }

    public class GenBase2<T>
    {
        public virtual string M1() { return "GenBase2<" + typeof(T).Name + ">.M1()"; }
        public virtual string M2() { return "GenBase2<" + typeof(T).Name + ">.M2()"; }
    }
    public class GenDerived2<T, U> : GenBase2<U>
    {
        public override string M1() { return "GenDerived2<" + typeof(T).Name + "," + typeof(U).Name + ">.M1() - " + base.M1(); }
    }


    public class Test
    {
        [TestMethod]
        public static void TestVirtCallTwoGenParams()
        {
            var t = TypeOf.BTDT_GenDerived2.MakeGenericType(TypeOf.CommonType1, TypeOf.String);
            var o = Activator.CreateInstance(t);
            var m1 = t.GetTypeInfo().GetDeclaredMethod("M1");
            string m1result = (string)m1.Invoke(o, null);
            Assert.AreEqual("GenDerived2<CommonType1,String>.M1() - GenBase2<String>.M1()", m1result);

            GenBase2<string> oo = (GenBase2<string>)o;
            m1result = oo.M1();
            Assert.AreEqual("GenDerived2<CommonType1,String>.M1() - GenBase2<String>.M1()", m1result);

            string m2result = oo.M2();
            Assert.AreEqual("GenBase2<String>.M2()", m2result);
        }

        static string BuildResultString(string genericType, string genericArg, string methodName)
        {
            if (genericType == "MyClass4`1")
                return genericType + "<" + genericArg + ">." + methodName + "() - " + BuildResultString("MyClass3`1", genericArg, methodName);
            else if (genericType == "MyClass4_2`1")
                return genericType + "<" + genericArg + ">." + methodName + "() - " + BuildResultString("MyClass3_2", "MyClass1", methodName);
            else if (genericType == "MyClass4_3`1")
                return genericType + "<" + genericArg + ">." + methodName + "() - " + BuildResultString("MyClass2`1", "MyClass1", methodName);

            else if (genericType == "MyClass3`1")
                return genericType + "<" + genericArg + ">." + methodName + "() - " + BuildResultString("MyClass2`1", genericArg, methodName);
            else if (genericType == "MyClass3_2")
                return genericType + "." + methodName + "() - " + BuildResultString("MyClass2`1", "MyClass1", methodName);

            else if (genericType == "MyClass2`1")
                return genericType + "<" + genericArg + ">." + methodName + "() - " + BuildResultString("MyClass1", genericArg, methodName);

            else
                return genericType + "." + methodName + "()";
        }
        static void DoTest(Type genericType, Type genericArg)
        {
            var t = genericType.MakeGenericType(genericArg);
            var o = Activator.CreateInstance(t);
            var m1 = t.GetTypeInfo().GetDeclaredMethod("M1");
            var m2 = t.GetTypeInfo().GetDeclaredMethod("M2");
            string m1result = (string)m1.Invoke(o, null);
            string m2result = (string)m2.Invoke(o, null);
            string m1Expected = BuildResultString(genericType.Name, genericArg.Name, "M1");
            string m2Expected = BuildResultString(genericType.Name, genericArg.Name, "M2");
            Assert.AreEqual(m1Expected, m1result);
            Assert.AreEqual(m2Expected, m2result);
        }

        [TestMethod]
        public static void TestUsingPrimitiveTypes()
        {
            var t1 = TypeOf.BTDT_Gen1.MakeGenericType(TypeOf.BTDT_Foo2);
            var t2 = TypeOf.BTDT_Gen2.MakeGenericType(TypeOf.BTDT_Foo2);
            var o1 = Activator.CreateInstance(t1);
            var o2 = Activator.CreateInstance(t2);
            var mi1 = t1.GetTypeInfo().GetDeclaredMethod("M");
            var mi2 = t2.GetTypeInfo().GetDeclaredMethod("M");
            var result1 = (string)mi1.Invoke(o1, null);
            var result2 = (string)mi2.Invoke(o2, null);
            Assert.AreEqual("Gen1<Foo2> - GenBase1<Foo2> - GenBase2<Int32,Foo2>", result1);
            Assert.AreEqual("Gen2<Foo2> - GenBase1<Int32> - GenBase2<Int32,Int32>", result2);
        }

        [TestMethod]
        public static void TestBaseTypeDictionaries()
        {
            foreach (var genType in new[] { TypeOf.BTDT_MyClass2, TypeOf.BTDT_MyClass3, TypeOf.BTDT_MyClass4 })
            {
                DoTest(genType, TypeOf.CommonType1);
                DoTest(genType, TypeOf.CommonType2);
            }

            foreach (var genType in new[] { TypeOf.BTDT_MyClass4, TypeOf.BTDT_MyClass3, TypeOf.BTDT_MyClass2 })
            {
                DoTest(genType, TypeOf.CommonType3);
                DoTest(genType, TypeOf.CommonType2);
            }

            foreach (var genType in new[] { TypeOf.BTDT_MyClass2, TypeOf.BTDT_MyClass4_2, TypeOf.BTDT_MyClass4_3 })
            {
                DoTest(genType, TypeOf.CommonType4);
                DoTest(genType, TypeOf.CommonType2);
            }

            foreach (var genType in new[] { TypeOf.BTDT_MyClass4_3, TypeOf.BTDT_MyClass4_2, TypeOf.BTDT_MyClass2 })
            {
                DoTest(genType, TypeOf.CommonType5);
                DoTest(genType, TypeOf.CommonType2);
            }
        }
    }
}

namespace DictDependency
{
#if USC
    public struct MyType1<A, B> { }
    public struct MyType2<A, B> { }
    public struct MyType3 { }
    public struct MyType4 { }
#else
    public class MyType1<A, B> { }
    public class MyType2<A, B> { }
    public class MyType3 { }
    public class MyType4 { }
#endif

    public class TestClass1<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string M<U>()
        {
            return "TestClass1<" + typeof(T) + ">.M<" + typeof(U) + ">()";
        }
    }
    public class TestClass2<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string M<U>()
        {
            return "TestClass2<" + typeof(T) + ">.M<" + typeof(U) + ">()";
        }
    }

    public abstract class Base
    {
        public abstract string Method1();
        public abstract string Method2();
        public abstract string Method3();
        public abstract string Method4();
    }

    public class Yahoo<X, Y> : Base
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string Method1()
        {
            return TestClass1<X>.M<Action<MyType1<X, Y>>>();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string Method2()
        {
            return TestClass1<Y>.M<Action<MyType1<Y, X>>>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string Method3()
        {
            return TestClass2<Action<MyType2<X, Y>>>.M<X>();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string Method4()
        {
            return TestClass2<Action<MyType2<Y, X>>>.M<Y>();
        }
    }

    public class Test
    {
        [TestMethod]
        public static void TestIndirectDictionaryDependencies()
        {
            {
                var t = TypeOf.DDT_Yahoo.MakeGenericType(TypeOf.CommonType1, typeof(MyType3));
                Base b = (Base)Activator.CreateInstance(t);

                var result = b.Method1();
                Assert.AreEqual("TestClass1<CommonType1>.M<System.Action`1[DictDependency.MyType1`2[CommonType1,DictDependency.MyType3]]>()", result);
                result = b.Method2();
                Assert.AreEqual("TestClass1<DictDependency.MyType3>.M<System.Action`1[DictDependency.MyType1`2[DictDependency.MyType3,CommonType1]]>()", result);
            }

            {
                var t = TypeOf.DDT_Yahoo.MakeGenericType(TypeOf.CommonType1, typeof(MyType4));
                Base b = (Base)Activator.CreateInstance(t);

                var result = b.Method3();
                Assert.AreEqual("TestClass2<System.Action`1[DictDependency.MyType2`2[CommonType1,DictDependency.MyType4]]>.M<CommonType1>()", result);
                result = b.Method4();
                Assert.AreEqual("TestClass2<System.Action`1[DictDependency.MyType2`2[DictDependency.MyType4,CommonType1]]>.M<DictDependency.MyType4>()", result);
            }
        }
    }
}

namespace CtorDict
{
    public class MyType1
    {
        private string _n;
        public MyType1() { _n = "TEST"; }
        public override string ToString() { return "MyType1::" + _n; }
    }
    public class MyType2
    {
        private int _n;
        public MyType2() { _n = 123; }
        ~MyType2() { }
        public override string ToString() { return "MyType2::" + _n; }
    }
    public class MyType3
    {
        public MyType3(string s) { }
        public override string ToString() { return "MyType3"; }
    }
    public class MyType4<T>
    {
        private T _n;
        public MyType4() { _n = default(T); }
        public override string ToString() { return "MyType4<" + typeof(T) + ">::" + _n; }
    }
    public class MyType5
    {
        UInt64 _long;
        double _double;
        public MyType5() { _long = 123456789; _double = 12345.6789; }
        public override string ToString() { return "MyType5::" + _long + "~" + _double; }
    }
    public class MyType6
    {
        long _long;
        double _double;
        public MyType6() { _long = 123456789; _double = 12345.6789; }
        ~MyType6() { }
        public override string ToString() { return "MyType6::" + _long + "~" + _double; }
    }
    public class MyType7
    {
        private int _n;
        private string _s;
        public MyType7(string a, int b) { _s = a; _n = b; }
        public override string ToString() { return "MyType7::" + _s + "~" + _n; }
    }
    public class MyType8
    {
        private string _s;
        private MyType8(string a) { _s = a; }
        public override string ToString() { return "MyType8::" + _s; }
    }
    // TODO once we support instantiations over non-reference type arguments : Test with :
    //      enums(short) and valuetype that has alignment requirement <= 4, with finalizer          (to test usage of the NewFinalizable helper)
    //      enums(short) and valuetype that has alignment requirement <= 4, without finalizer       (to test usage of the NewFast helper)
    //      enums(long) and valuetype that has alignment requirement > 4, with finalizer            (to test usage of the NewFinalizableAlign8 helper)
    //      enums(long) and valuetype that has alignment requirement > 4, without finalizer         (to test usage of the NewFastMisalign helper)



    public class CtorTest<T, X>
        where T : new()
    {
        public Object TestMethod<U, Y>(int id)
            where U : new()
        {
            switch (id)
            {
                case 0: return new T();
                case 1: return new U();
                case 2: return new T[] { new T(), new T() }[0];
                case 3: return new U[] { new U(), new U() }[1];
                case 4: return new MyType4<T>();
                case 5: return new MyType4<U>();
                case 6: return Activator.CreateInstance<T>();
                case 7: return Activator.CreateInstance<U>();
                case 8: return Activator.CreateInstance(typeof(T));
                case 9: return Activator.CreateInstance(typeof(U));
            }
            return null;
        }

        public Object TestMethod2<U, Y>(int id)
        {
            try
            {
                switch (id)
                {
                    case 0: return Activator.CreateInstance<U>();
                    case 1: return Activator.CreateInstance(typeof(U));
                }
            }
            catch (MissingMemberException)
            {
                return "SUCCESS";
            }
            catch (Exception)
            {
                return "FAILED";
            }
            return null;
        }
    }

    public class SelfCtorTest<T,U>
    {
        public static SelfCtorTest<T,U> TestMethod()
        {
            return Activator.CreateInstance<SelfCtorTest<T,U>>();
        }
    }

    public class NoDefaultCtorTest<T, X>
    {
        public Object TestMethod<U, Y>(int id)
        {
            try
            {
                switch (id)
                {
                    case 0: return Activator.CreateInstance<T>();
                    case 1: return Activator.CreateInstance<U>();
                    case 2: return Activator.CreateInstance(typeof(T));
                    case 3: return Activator.CreateInstance(typeof(U));
                }
            }
            catch (MissingMemberException)
            {
                return "SUCCESS";
            }
            catch (Exception)
            {
                return "FAILED";
            }
            return null;
        }
    }

    public class DictionaryTesting
    {
        public static void DoTest(Type t1, Type t2)
        {
            var t = TypeOf.CDT_CtorTest.MakeGenericType(t1, TypeOf.CommonType1);
            var m = t.GetTypeInfo().GetDeclaredMethod("TestMethod").MakeGenericMethod(t2, TypeOf.CommonType2);
            object inst = Activator.CreateInstance(t);

            var result = m.Invoke(inst, new object[] { 0 });
            Assert.AreEqual(Activator.CreateInstance(t1).ToString(), result.ToString());

            result = m.Invoke(inst, new object[] { 1 });
            Assert.AreEqual(Activator.CreateInstance(t2).ToString(), result.ToString());

            result = m.Invoke(inst, new object[] { 2 });
            Assert.AreEqual(Activator.CreateInstance(t1).ToString(), result.ToString());

            result = m.Invoke(inst, new object[] { 3 });
            Assert.AreEqual(Activator.CreateInstance(t2).ToString(), result.ToString());

            result = m.Invoke(inst, new object[] { 4 });
            Assert.AreEqual("MyType4<" + t1 + ">::", result.ToString());

            result = m.Invoke(inst, new object[] { 5 });
            Assert.AreEqual("MyType4<" + t2 + ">::", result.ToString());

            // Type with no default ctor: we will actually fail on constraint validation instead of throwing the 
            // "no default constructor" exception when the type gets new'd
            try
            {
                m = t.GetTypeInfo().GetDeclaredMethod("TestMethod").MakeGenericMethod(TypeOf.CDT_MyType3);
                Console.WriteLine("ArgumentException not thrown!!");
                Assert.AreEqual(true, false);
            }
            catch (ArgumentException)
            {
            }

            result = m.Invoke(inst, new object[] { 6 });
            Assert.AreEqual(Activator.CreateInstance(t1).ToString(), result.ToString());

            result = m.Invoke(inst, new object[] { 7 });
            Assert.AreEqual(Activator.CreateInstance(t2).ToString(), result.ToString());


            result = m.Invoke(inst, new object[] { 8 });
            Assert.AreEqual(Activator.CreateInstance(t1).ToString(), result.ToString());

            result = m.Invoke(inst, new object[] { 9 });
            Assert.AreEqual(Activator.CreateInstance(t2).ToString(), result.ToString());

            // Test that a generic dictionary can contain a call that uses Activator.CreateInstance<T> on itself
            var tSelfType = TypeOf.CDT_SelfCtorTest.MakeGenericType(t1, TypeOf.CommonType1);
            var mSelf = tSelfType.GetTypeInfo().GetDeclaredMethod("TestMethod");
            result = mSelf.Invoke(null, null);
            Assert.AreEqual(result.GetType(), tSelfType);
        }

        public static void DoTest_NoDefaultCtor(Type t1, Type t2)
        {
            var t = TypeOf.CDT_NoDefaultCtorTest.MakeGenericType(t1, TypeOf.CommonType1);
            var m = t.GetTypeInfo().GetDeclaredMethod("TestMethod").MakeGenericMethod(t2, TypeOf.CommonType2);
            object inst = Activator.CreateInstance(t);

            for (int i = 0; i <= 3; i++)
            {
                var result = m.Invoke(inst, new object[] { i });
                Assert.AreEqual("SUCCESS", result.ToString());
            }
        }

        [TestMethod]
        public static void TestAllocationDictionaryEntryTypes()
        {
            DoTest(TypeOf.CDT_MyType1, TypeOf.CDT_MyType2);
            DoTest(TypeOf.CDT_MyType5, TypeOf.CDT_MyType6);
            DoTest_NoDefaultCtor(TypeOf.CDT_MyType7, TypeOf.CDT_MyType8);
        }
    }
}

namespace MethodAndUnboxingStubTesting
{
#if USC
    public struct Class1 { }
    public struct Class2 { }
#else
    public class Class1 { }
    public class Class2 { }
#endif

    public class GenericClass<T, U>
    {
        public static string SMethod() { return "GenericClass<" + typeof(T).Name + "," + typeof(U).Name + ">.SMethod"; }
        public string IMethod() { return "THIS = " + this.ToString() + " -- GenericClass<" + typeof(T).Name + "," + typeof(U).Name + ">.IMethod"; }

        public static string GSMethod<X>() { return "GenericClass<" + typeof(T).Name + "," + typeof(U).Name + ">.GSMethod<" + typeof(X).Name + ">"; }
        public string GIMethod<X>() { return "THIS = " + this.ToString() + " -- GenericClass<" + typeof(T).Name + "," + typeof(U).Name + ">.GIMethod<" + typeof(X).Name + ">"; }

        public void Test()
        {
            string expectedInstance = "THIS = " + this.ToString() + " -- ";
            string expectedTypeName = "GenericClass<" + typeof(T).Name + "," + typeof(U).Name + ">";

            Func<string> a;

            a = SMethod;
            Assert.AreEqual(expectedTypeName + ".SMethod", a());

            a = IMethod;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".IMethod", a());

            a = GSMethod<T>;
            Assert.AreEqual(expectedTypeName + ".GSMethod<" + typeof(T).Name + ">", a());

            a = GIMethod<U>;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".GIMethod<" + typeof(U).Name + ">", a());
        }
    }

    public class GenericClass2<T, U>
    {
        public void Test()
        {
            var o = new GenericClass<T, U>();
            string expectedInstance = "THIS = " + o.ToString() + " -- ";
            string expectedTypeName = "GenericClass<" + typeof(T).Name + "," + typeof(U).Name + ">";

            Func<string> a;

            a = GenericClass<T, U>.SMethod;
            Assert.AreEqual(expectedTypeName + ".SMethod", a());

            a = o.IMethod;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".IMethod", a());

            a = GenericClass<T, U>.GSMethod<T>;
            Assert.AreEqual(expectedTypeName + ".GSMethod<" + typeof(T).Name + ">", a());

            a = o.GIMethod<U>;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".GIMethod<" + typeof(U).Name + ">", a());
        }
    }

    public struct GenericStruct<T, U>
    {
        public static string SMethod() { return "GenericStruct<" + typeof(T).Name + "," + typeof(U).Name + ">.SMethod"; }
        public string IMethod() { return "THIS = " + this.ToString() + " -- GenericStruct<" + typeof(T).Name + "," + typeof(U).Name + ">.IMethod"; }

        public static string GSMethod<X>() { return "GenericStruct<" + typeof(T).Name + "," + typeof(U).Name + ">.GSMethod<" + typeof(X).Name + ">"; }
        public string GIMethod<X>() { return "THIS = " + this.ToString() + " -- GenericStruct<" + typeof(T).Name + "," + typeof(U).Name + ">.GIMethod<" + typeof(X).Name + ">"; }

        public void Test()
        {
            string expectedInstance = "THIS = " + this.ToString() + " -- ";
            string expectedTypeName = "GenericStruct<" + typeof(T).Name + "," + typeof(U).Name + ">";

            Func<string> a = SMethod;
            Assert.AreEqual(expectedTypeName + ".SMethod", a());

            a = IMethod;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".IMethod", a());

            a = GSMethod<T>;
            Assert.AreEqual(expectedTypeName + ".GSMethod<" + typeof(T).Name + ">", a());

            a = GIMethod<U>;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".GIMethod<" + typeof(U).Name + ">", a());
        }
    }

    public struct GenericStruct2<T, U>
    {
        public void Test()
        {
            var o = new GenericStruct<T, U>();
            string expectedInstance = "THIS = " + o.ToString() + " -- ";
            string expectedTypeName = "GenericStruct<" + typeof(T).Name + "," + typeof(U).Name + ">";

            Func<string> a = GenericStruct<T, U>.SMethod;
            Assert.AreEqual(expectedTypeName + ".SMethod", a());

            a = o.IMethod;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".IMethod", a());

            a = GenericStruct<T, U>.GSMethod<T>;
            Assert.AreEqual(expectedTypeName + ".GSMethod<" + typeof(T).Name + ">", a());

            a = o.GIMethod<U>;
            Assert.AreEqual(expectedInstance + expectedTypeName + ".GIMethod<" + typeof(U).Name + ">", a());
        }
    }

    public class Test
    {
        [TestMethod]
        public static void TestNoConstraints()
        {
            var t = TypeOf.MUST_GenericClass.MakeGenericType(TypeOf.CommonType1, typeof(Test));
            var m = t.GetTypeInfo().GetDeclaredMethod("Test");
            m.Invoke(Activator.CreateInstance(t), new object[] { });

            t = TypeOf.MUST_GenericClass2.MakeGenericType(typeof(Class1), typeof(Class2));
            m = t.GetTypeInfo().GetDeclaredMethod("Test");
            m.Invoke(Activator.CreateInstance(t), new object[] { });

            t = TypeOf.MUST_GenericClass2.MakeGenericType(TypeOf.CommonType2, TypeOf.CommonType2);
            m = t.GetTypeInfo().GetDeclaredMethod("Test");
            m.Invoke(Activator.CreateInstance(t), new object[] { });



            t = TypeOf.MUST_GenericStruct.MakeGenericType(TypeOf.CommonType1, typeof(Test));
            m = t.GetTypeInfo().GetDeclaredMethod("Test");
            m.Invoke(Activator.CreateInstance(t), new object[] { });

            t = TypeOf.MUST_GenericStruct2.MakeGenericType(typeof(Class1), typeof(Class2));
            m = t.GetTypeInfo().GetDeclaredMethod("Test");
            m.Invoke(Activator.CreateInstance(t), new object[] { });

            t = TypeOf.MUST_GenericStruct2.MakeGenericType(TypeOf.CommonType2, TypeOf.CommonType2);
            m = t.GetTypeInfo().GetDeclaredMethod("Test");
            m.Invoke(Activator.CreateInstance(t), new object[] { });
        }
    }
}

namespace ExistingInstantiations
{
#if USC
    public struct MyClass1 { }
    public struct MyClass2 { }
    public struct MyClass3 { }
    public struct MyClass4 { }
#else
    public class MyClass1 { }
    public class MyClass2 { }
    public class MyClass3 { }
    public class MyClass4 { }
#endif

    public class Gen<T, U>
    {
        public override string ToString()
        {
            return "Gen<" + typeof(T) + "," + typeof(U) + ">";
        }

        public string GMethod<X, Y>()
        {
            return "Gen<" + typeof(T) + "," + typeof(U) + ">.GMethod<" + typeof(X) + "," + typeof(Y) + ">";
        }

        public Func<string> GetGMethodDel<X, Y>()
        {
            return this.GMethod<X, Y>;
        }
    }

    public class Gen2<T, U>
    {
        public void GetGenObjects(out object o1, out object o2, out object o3) 
        {
            o1 = Activator.CreateInstance(typeof(Gen<T, U[]>));
            o2 = Activator.CreateInstance(typeof(Gen<T[], U>));
            o3 = Activator.CreateInstance(typeof(Gen<T[], U[]>));
        }

        public void GetGenDelegates<X, Y>(out Func<string> d1, out Func<string> d2, out Func<string> d3)
        {
            d1 = (new Gen<T, U[]>()).GetGMethodDel<X, Y[]>();
            d2 = (new Gen<T[], U>()).GetGMethodDel<X[], Y>();
            d3 = (new Gen<T[], U[]>()).GetGMethodDel<X[], Y[]>();
        }
    }

    public class Foo<T, U> { }

    public struct MyIntWrapper { public int _f; }

    public delegate RuntimeTypeHandle MyDel(out RuntimeTypeHandle u);

    public interface IFrobber
    {
        RuntimeTypeHandle Frob1<T>();
        RuntimeTypeHandle Frob2<T>();
        MyDel Frob3<T>();
        MyDel Frob4<T>();
    }

    public class Frobber1 : IFrobber
    {
        public RuntimeTypeHandle Frob1<T>()
        {
            return typeof(Foo<MyIntWrapper[], T>).TypeHandle;
        }
        public RuntimeTypeHandle Frob2<T>()
        {
            return typeof(Foo<MyIntWrapper[], T>).TypeHandle;
        }
        public MyDel Frob3<T>()
        {
            return new MyDel(Method<MyIntWrapper[], T>);
        }
        public MyDel Frob4<T>()
        {
            return new MyDel(Method<MyIntWrapper[], T>);
        }
        public static RuntimeTypeHandle Method<T, U>(out RuntimeTypeHandle u)
        {
            u = typeof(U).TypeHandle;
            return typeof(T).TypeHandle;
        }
    }

    // This type does not have all its methods reflectable, so we'll end up
    // using USG implementations for some of them (non-interface calls)
    public class Frobber2 : IFrobber
    {
        public RuntimeTypeHandle Frob1<T>()
        {
            return typeof(Foo<MyIntWrapper[], T>).TypeHandle;
        }
        public RuntimeTypeHandle Frob2<T>()
        {
            return typeof(Foo<MyIntWrapper[], T>).TypeHandle;
        }
        public MyDel Frob3<T>()
        {
            return new MyDel(Method<MyIntWrapper[], T>);
        }
        public MyDel Frob4<T>()
        {
            return new MyDel(Method<MyIntWrapper[], T>);
        }
        public static RuntimeTypeHandle Method<T, U>(out RuntimeTypeHandle u)
        {
            T tt = default(T);
            U uu = default(U);
            Test.s_dummyString = tt + " " + uu;

            u = typeof(U).TypeHandle;
            return typeof(T).TypeHandle;
        }
    }

    public class Test
    {
        public static string s_dummyString;

        [TestMethod]
        public static void TestWithExistingInst()
        {
            // Static instantiations
            var o1 = new Gen<MyClass1, MyClass2[]>();
            var d1 = o1.GetGMethodDel<MyClass3, MyClass4[]>();

            var o2 = new Gen<MyClass1[], MyClass2>();
            var d2 = o2.GetGMethodDel<MyClass3[], MyClass4>();

            var o3 = new Gen<MyClass1[], MyClass2[]>();
            var d3 = o3.GetGMethodDel<MyClass3[], MyClass4[]>();

            // Dynamic instantiations

            var t = TypeOf.EIT_Gen2.MakeGenericType(TypeOf.EIT_MyClass1, TypeOf.EIT_MyClass2);
            var gen2 = Activator.CreateInstance(t);

            // Testing typehandles that already exist statically, but reachable from dynamic code

            var mi = t.GetTypeInfo().GetDeclaredMethod("GetGenObjects");
            var parameters = new object[3];
            mi.Invoke(gen2, parameters);
            object o4 = parameters[0];
            object o5 = parameters[1];
            object o6 = parameters[2];

            Assert.AreNotEqual(o1, o4);
            Assert.AreNotEqual(o2, o5);
            Assert.AreNotEqual(o3, o6);
            Assert.AreEqual(o1.GetType(), o4.GetType());
            Assert.AreEqual(o2.GetType(), o5.GetType());
            Assert.AreEqual(o3.GetType(), o6.GetType());
            Assert.AreEqual(o1.GetType().TypeHandle, o4.GetType().TypeHandle);
            Assert.AreEqual(o2.GetType().TypeHandle, o5.GetType().TypeHandle);
            Assert.AreEqual(o3.GetType().TypeHandle, o6.GetType().TypeHandle);

            // Testing method dictionaries that already exist statically, but reachable from dynamic code

            mi = t.GetTypeInfo().GetDeclaredMethod("GetGenDelegates").MakeGenericMethod(TypeOf.EIT_MyClass3, TypeOf.EIT_MyClass4);
            parameters = new object[3];
            mi.Invoke(gen2, parameters);
            Func<string> d4 = (Func<string>)parameters[0];
            Func<string> d5 = (Func<string>)parameters[1];
            Func<string> d6 = (Func<string>)parameters[2];

            Assert.AreNotEqual(d1, d4);
            Assert.AreNotEqual(d2, d5);
            Assert.AreNotEqual(d3, d6);
            Assert.AreEqual(d1.GetMethodInfo(), d4.GetMethodInfo());
            Assert.AreEqual(d2.GetMethodInfo(), d5.GetMethodInfo());
            Assert.AreEqual(d3.GetMethodInfo(), d6.GetMethodInfo());
            Assert.AreEqual(d1(), d4());
            Assert.AreEqual(d2(), d5());
            Assert.AreEqual(d3(), d6());
        }

        [TestMethod]
        public static void TestInstantiationsWithExistingArrayTypeArgs()
        {
            // use the field on MyIntWrapper
            MyIntWrapper temp = new MyIntWrapper { _f = 1 };

            for (int i = 0; i < 4; i++)
            {
                // Make sure we start with a clean type loader context
                for (int j = 0; j < 10; j++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                Type frobberType = (i == 0 || i == 1) ? typeof(Frobber1) : typeof(Frobber2);

                IFrobber f = (IFrobber)Activator.CreateInstance(frobberType);

                if (i == 0 || i == 2)
                {
                    f.Frob1<object>();
                    f.Frob2<object>();

                    // Type loader context should have type "MyIntWrapper[]" loaded with a valid runtime type handle (due to previous GVM call).
                    // Note: MyIntWrapper[] is statically compiled, not dynamically created.
                    var mi1 = typeof(IFrobber).GetTypeInfo().GetDeclaredMethod("Frob1").MakeGenericMethod(TypeOf.CommonType1);
                    var th1 = (RuntimeTypeHandle)mi1.Invoke(f, null);
                    
                    // Make sure the cached type loader contexts are flushed
                    for (int j = 0; j < 10; j++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    // Starting with a clean type loader context, we should be able to find the instantiation "Foo<MyIntWrapper[], T>" created from the call to Frob1
                    // and we should not recreate it.
                    // Note: clean type loader context has no way to know the runtime type handle of MyIntWrapper[] when trying to search for the type handle for
                    // "Foo<MyIntWrapper[], T>"
                    var mi2 = typeof(IFrobber).GetTypeInfo().GetDeclaredMethod("Frob2").MakeGenericMethod(TypeOf.CommonType1);
                    var th2 = (RuntimeTypeHandle)mi2.Invoke(f, null);

                    Assert.IsTrue(th1.Equals(th2));
                }
                else
                {
                    // Similar to the previous test, but hitting the generic method creation code paths

                    f.Frob3<object>();
                    f.Frob4<object>();

                    var mi3 = frobberType.GetTypeInfo().GetDeclaredMethod("Frob3").MakeGenericMethod(TypeOf.CommonType2);
                    var del3 = (MyDel)mi3.Invoke(f, null);
                    RuntimeTypeHandle th3_2;
                    var th3_1 = del3(out th3_2);

                    // Make sure the cached type loader contexts are flushed
                    for (int j = 0; j < 10; j++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    var mi4 = frobberType.GetTypeInfo().GetDeclaredMethod("Frob4").MakeGenericMethod(TypeOf.CommonType2);
                    var del4 = (MyDel)mi4.Invoke(f, null);
                    RuntimeTypeHandle th4_2;
                    var th4_1 = del4(out th4_2);

                    Assert.IsTrue(th3_1.Equals(th4_1));
                    Assert.IsTrue(th3_2.Equals(th4_2));
                }
            }
        }
    }
}

namespace TemplateDependencyFromGenArgs
{
    public class A1<T, U> { } public class A2<T> { } public class A3<T> { }
    public class B1<T, U> { } public class B2<T> { } public class B3<T> { }
    public class C1<T, U> { } public class C2<T> { } public class C3<T> { }
    public class D1<T, U> { } public class D2<T> { } public class D3<T> { }
    public class E1<T, U> { } public class E2<T> { } public class E3<T> { }
    public class F1<T, U> { } public class F2<T> { } public class F3<T> { }
    public class G1<T, U> { } public class G2<T> { } public class G3<T> { }
    public class H1<T, U> { } public class H2<T> { } public class H3<T> { }
    public class I1<T, U> { } public class I2<T> { } public class I3<T> { }
    public class J1<T, U> { } public class J2<T> { } public class J3<T> { }
    public class K1<T, U> { } public class K2<T> { } public class K3<T> { }
    public class L1<T, U> { } public class L2<T> { } public class L3<T> { }
    
    public class M1<T, U> { public class Nested<V>{ } } public class M2<T> { public class Nested<V>{ } } public class M3<T> { public class Nested<V>{ } }
    public class N1<T, U> { public class Nested<V>{ } } public class N2<T> { public class Nested<V>{ } } public class N3<T> { public class Nested<V>{ } }
    
    public class O1<T, U> { public class Nested{ } } public class O2<T> { public class Nested{ } } public class O3<T> { public class Nested{ } }
    public class P1<T, U> { public class Nested{ } } public class P2<T> { public class Nested{ } } public class P3<T> { public class Nested{ } }
    public class Q1<T, U> { public class Nested{ } } public class Q2<T> { public class Nested{ } } public class Q3<T> { public class Nested{ } }
    public class R1<T, U> { public class Nested{ } } public class R2<T> { public class Nested{ } } public class R3<T> { public class Nested{ } }

    public static class MyTypeExtension
    {
        public static string FormattedName(this Type t)
        {
            string result = t.Name;
            if (t.GetTypeInfo().IsGenericType)
            {
                result += "<";
                for (int i = 0; i < t.GenericTypeArguments.Length; i++)
                    result += (i > 0 ? "," : "") + t.GenericTypeArguments[i].FormattedName();
                result += ">";
            }
            if (t.GetTypeInfo().IsNested)
            {
                result = t.GetTypeInfo().DeclaringType.FormattedName() + "+" + result;
            }
            return result;
        }
    }

    public class TestClass
    {
        public static string MyGenMethod<U>()
        {
            return "TestClass.MyGenMethod<" + typeof(U).FormattedName() + ">()";
        }

        public class NestedTestClass
        {
            public static string MyGenMethod<U>()
            {
                return "TestClass.NestedTestClass.MyGenMethod<" + typeof(U).FormattedName() + ">()";
            }
        }
        public class NestedGenTestClass<V>
        {
            public static string MyMethod()
            {
                return "TestClass.NestedGenTestClass<" + typeof(V).FormattedName() + ">.MyMethod()";
            }
            public static string MyGenMethod<U>()
            {
                return "TestClass.NestedGenTestClass<" + typeof(V).FormattedName() + ">.MyGenMethod<" + typeof(U).FormattedName() + ">()";
            }
        }
    }
    
    public class TestClass<T>
    {
        public static string MyMethod()
        {
            return "TestClass<" + typeof(T).FormattedName() + ">.MyMethod()";
        }
        public static string MyGenMethod<U>()
        {
            return "TestClass<" + typeof(T).FormattedName() + ">.MyGenMethod<" + typeof(U).FormattedName() + ">()";
        }

        public class NestedTestClass
        {
            public static string MyMethod()
            {
                return "TestClass<" + typeof(T).FormattedName() + ">.NestedTestClass.MyMethod()";
            }
            public static string MyGenMethod<U>()
            {
                return "TestClass<" + typeof(T).FormattedName() + ">.NestedTestClass.MyGenMethod<" + typeof(U).FormattedName() + ">()";
            }
        }
        public class NestedGenTestClass<V>
        {
            public static string MyMethod()
            {
                return "TestClass<" + typeof(T).FormattedName() + ">.NestedGenTestClass<" + typeof(V).FormattedName() + ">.MyMethod()";
            }
            public static string MyGenMethod<U>()
            {
                return "TestClass<" + typeof(T).FormattedName() + ">.NestedGenTestClass<" + typeof(V).FormattedName() + ">.MyGenMethod<" + typeof(U).FormattedName() + ">()";
            }
        }
    }
    
    public class CallerType<X, Y>
    {
        public string CallerMethod(int testId)
        {
            switch (testId)
            {
                case 0: return TestClass.MyGenMethod<A3<A2<A1<X, Y>>>>();
                case 1: return TestClass.NestedTestClass.MyGenMethod<B3<B2<B1<X, Y>>>>();
                case 2: return TestClass.NestedGenTestClass<C3<C2<C1<X, Y>>>>.MyMethod();
                case 3: return TestClass.NestedGenTestClass<D3<D2<D1<X, Y>>>>.MyGenMethod<X>();

                case 4: return TestClass<E3<E2<E1<X, Y>>>>.MyMethod();
                case 5: return TestClass<F3<F2<F1<X, Y>>>>.MyGenMethod<X>();
                case 6: return TestClass<G3<G2<G1<X, Y>>>>.NestedTestClass.MyMethod();
                case 7: return TestClass<H3<H2<H1<X, Y>>>>.NestedTestClass.MyGenMethod<X>();
                case 8: return TestClass<I3<I2<I1<X, Y>>>>.NestedGenTestClass<K3<K2<K1<X, Y>>>>.MyMethod();
                case 9: return TestClass<J3<J2<J1<X, Y>>>>.NestedGenTestClass<L3<L2<L1<X, Y>>>>.MyGenMethod<X>();

                case 10: return TestClass<M3<M2<M1<X, Y>.Nested<X>>.Nested<Y>>.Nested<X>>.MyMethod();
                case 11: return TestClass<N3<N2<N1<X, Y>.Nested<X>>.Nested<Y>>.Nested<X>>.MyGenMethod<X>();

                case 12: return TestClass.MyGenMethod<O3<O2<O1<X, Y>.Nested>.Nested>.Nested>();
                case 13: return TestClass.NestedTestClass.MyGenMethod<P3<P2<P1<X, Y>.Nested>.Nested>.Nested>();
                case 14: return TestClass.NestedGenTestClass<Q3<Q2<Q1<X, Y>.Nested>.Nested>.Nested>.MyMethod();
                case 15: return TestClass.NestedGenTestClass<R3<R2<R1<X, Y>.Nested>.Nested>.Nested>.MyGenMethod<X>();
            }
            return null;
        }
    }

    public class TestRunner
    {
        [TestMethod]
        public static void TemplateDependencyFromGenArgsTest()
        {
            string[] expectedResults = new string[]
            {
                "TestClass.MyGenMethod<A3`1<A2`1<A1`2<CommonType1,CommonType2>>>>()",
                "TestClass.NestedTestClass.MyGenMethod<B3`1<B2`1<B1`2<CommonType1,CommonType2>>>>()",
                "TestClass.NestedGenTestClass<C3`1<C2`1<C1`2<CommonType1,CommonType2>>>>.MyMethod()",
                "TestClass.NestedGenTestClass<D3`1<D2`1<D1`2<CommonType1,CommonType2>>>>.MyGenMethod<CommonType1>()",
                "TestClass<E3`1<E2`1<E1`2<CommonType1,CommonType2>>>>.MyMethod()",
                "TestClass<F3`1<F2`1<F1`2<CommonType1,CommonType2>>>>.MyGenMethod<CommonType1>()",
                "TestClass<G3`1<G2`1<G1`2<CommonType1,CommonType2>>>>.NestedTestClass.MyMethod()",
                "TestClass<H3`1<H2`1<H1`2<CommonType1,CommonType2>>>>.NestedTestClass.MyGenMethod<CommonType1>()",
                "TestClass<I3`1<I2`1<I1`2<CommonType1,CommonType2>>>>.NestedGenTestClass<K3`1<K2`1<K1`2<CommonType1,CommonType2>>>>.MyMethod()",
                "TestClass<J3`1<J2`1<J1`2<CommonType1,CommonType2>>>>.NestedGenTestClass<L3`1<L2`1<L1`2<CommonType1,CommonType2>>>>.MyGenMethod<CommonType1>()",
                "TestClass<M3`1<>+Nested`1<M2`1<>+Nested`1<M1`2<>+Nested`1<CommonType1,CommonType2,CommonType1>,CommonType2>,CommonType1>>.MyMethod()",
                "TestClass<N3`1<>+Nested`1<N2`1<>+Nested`1<N1`2<>+Nested`1<CommonType1,CommonType2,CommonType1>,CommonType2>,CommonType1>>.MyGenMethod<CommonType1>()",
                "TestClass.MyGenMethod<O3`1<>+Nested<O2`1<>+Nested<O1`2<>+Nested<CommonType1,CommonType2>>>>()",
                "TestClass.NestedTestClass.MyGenMethod<P3`1<>+Nested<P2`1<>+Nested<P1`2<>+Nested<CommonType1,CommonType2>>>>()",
                "TestClass.NestedGenTestClass<Q3`1<>+Nested<Q2`1<>+Nested<Q1`2<>+Nested<CommonType1,CommonType2>>>>.MyMethod()",
                "TestClass.NestedGenTestClass<R3`1<>+Nested<R2`1<>+Nested<R1`2<>+Nested<CommonType1,CommonType2>>>>.MyGenMethod<CommonType1>()"
            };

            var t = typeof(CallerType<,>).MakeGenericType(TypeOf.CommonType1, TypeOf.CommonType2);
            var o = Activator.CreateInstance(t);

            MethodInfo CallerMethod = t.GetTypeInfo().GetDeclaredMethod("CallerMethod");
            for (int i = 0; i <= 15; i++)
            {
                string result = (string)CallerMethod.Invoke(o, new object[] { i });
                Assert.AreEqual(expectedResults[i], result);
            }
        }
    }
}
