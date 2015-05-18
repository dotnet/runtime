// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class MethodInfoCreateDelegateTests
    {
        //Create Open Instance delegate to a public method
        [Fact]
        public void TestCreateDelegate1()
        {
            Type typeTestClass = typeof(ClassA);
            RunBasicTestsHelper(typeTestClass);
        }

        //Inheritance Tests
        [Fact]
        public void TestCreateDelegate2()
        {
            Type typeTestClass = typeof(ClassA);
            Type TestSubClassType = typeof(SubClassA);
            RunInheritanceTestsHelper(typeTestClass, TestSubClassType);
        }

        //Generic Tests
        [Fact]
        public void TestCreateDelegate3()
        {
            Type typeGenericClassString = typeof(GenericClass<String>);
            RunGenericTestsHelper(typeGenericClassString);
        }


        //create open instance delegate with incorrect delegate type
        [Fact]
        public void TestCreateDelegate4()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            ClassA classAObj = new ClassA();
            Assert.Throws<ArgumentException>(() =>
            {
                miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Int));
            });
        }

        //Verify ArgumentNullExcpeption when type is null
        [Fact]
        public void TestCreateDelegate5()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            ClassA classAObj = new ClassA();
            Assert.Throws<ArgumentNullException>(() =>
            {
                miPublicInstanceMethod.CreateDelegate(null);
            });
        }

        //create closed instance delegate with incorrect delegate type
        [Fact]
        public void TestCreateDelegate6()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            ClassA classAObj = new ClassA();
            Assert.Throws<ArgumentException>(() =>
            {
                miPublicInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int), TestClass);
            });
        }

        //Verify ArgumentNullExcpeption when type is null
        [Fact]
        public void TestCreateDelegate7()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            ClassA classAObj = new ClassA();
            Assert.Throws<ArgumentNullException>(() =>
            {
                miPublicInstanceMethod.CreateDelegate(null, TestClass);
            });
        }

        //closed instance delegate with incorrect object type
        [Fact]
        public void TestCreateDelegate8()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            ClassA classAObj = new ClassA();
            Assert.Throws<ArgumentException>(() =>
            {
                miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Int), new DummyClass());
            });
        }

        //create closed static method with inccorect argument
        [Fact]
        public void TestCreateDelegate9()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            ClassA classAObj = new ClassA();
            Assert.Throws<ArgumentException>(() =>
            {
                miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Str), new DummyClass());
            });
        }

        [Fact]
        public void TestCreateDelegate10()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicStructMethod = GetMethod(typeof(ClassA), "PublicStructMethod");
            ClassA classAObj = new ClassA();
            Delegate dlgt = miPublicStructMethod.CreateDelegate(typeof(Delegate_DateTime_Str));
            Object retValue = ((Delegate_DateTime_Str)dlgt).DynamicInvoke(new Object[] { classAObj, null });
            Object actualReturnValue = classAObj.PublicStructMethod(new DateTime());
            Assert.True(retValue.Equals(actualReturnValue));
        }

        public void RunBasicTestsHelper(Type typeTestClass)
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeTestClass);
            MethodInfo miPublicInstanceMethod = GetMethod(typeTestClass, "PublicInstanceMethod");
            MethodInfo miPrivateInstanceMethod = GetMethod(typeTestClass, "PrivateInstanceMethod");
            MethodInfo miPublicStaticMethod = GetMethod(typeTestClass, "PublicStaticMethod");
            Delegate dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int));
            Object retValue = ((Delegate_TC_Int)dlgt).DynamicInvoke(new Object[] { TestClass });
            Assert.True(retValue.Equals(TestClass.PublicInstanceMethod()));
            Assert.NotNull(miPrivateInstanceMethod);
            dlgt = miPrivateInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int));
            retValue = ((Delegate_TC_Int)dlgt).DynamicInvoke(new Object[] { TestClass });
            Assert.True(retValue.Equals(21));
            dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Int), TestClass);
            retValue = ((Delegate_Void_Int)dlgt).DynamicInvoke(null);

            Assert.True(retValue.Equals(TestClass.PublicInstanceMethod()));
            dlgt = miPublicStaticMethod.CreateDelegate(typeof(Delegate_Str_Str));
            retValue = ((Delegate_Str_Str)dlgt).DynamicInvoke(new Object[] { "85" });

            Assert.True(retValue.Equals("85"));

            dlgt = miPublicStaticMethod.CreateDelegate(typeof(Delegate_Void_Str), "93");
            retValue = ((Delegate_Void_Str)dlgt).DynamicInvoke(null);

            Assert.True(retValue.Equals("93"));
        }

        public void RunInheritanceTestsHelper(Type typeTestClass, Type TestSubClassType)
        {
            SubClassA TestSubClass = (SubClassA)Activator.CreateInstance(TestSubClassType);
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeTestClass);
            MethodInfo miPublicInstanceMethod = GetMethod(typeTestClass, "PublicInstanceMethod");
            Delegate dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int));
            object retValue = ((Delegate_TC_Int)dlgt).DynamicInvoke(new Object[] { TestSubClass });

            Assert.True(retValue.Equals(TestSubClass.PublicInstanceMethod()));
            dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Int), TestSubClass);
            retValue = ((Delegate_Void_Int)dlgt).DynamicInvoke();

            Assert.True(retValue.Equals(TestSubClass.PublicInstanceMethod()));
        }

        public void RunGenericTestsHelper(Type typeGenericClassString)
        {
            GenericClass<String> genericClass = (GenericClass<String>)Activator.CreateInstance(typeGenericClassString);

            MethodInfo miMethod1String = GetMethod(typeGenericClassString, "Method1");
            MethodInfo miMethod2String = GetMethod(typeGenericClassString, "Method2");
            MethodInfo miMethod2IntGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(int) });
            MethodInfo miMethod2StringGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(String) });

            Delegate dlgt = miMethod1String.CreateDelegate(typeof(Delegate_GC_T_T<String>));
            object retValue = ((Delegate_GC_T_T<String>)dlgt).DynamicInvoke(new Object[] { genericClass, "TestGeneric" });

            Assert.True(retValue.Equals(genericClass.Method1("TestGeneric")));

            dlgt = miMethod1String.CreateDelegate(typeof(Delegate_T_T<String>), genericClass);
            retValue = ((Delegate_T_T<String>)dlgt).DynamicInvoke(new Object[] { "TestGeneric" });

            Assert.True(retValue.Equals(genericClass.Method1("TestGeneric")));

            dlgt = miMethod2IntGeneric.CreateDelegate(typeof(Delegate_T_T<int>));
            retValue = ((Delegate_T_T<int>)dlgt).DynamicInvoke(new Object[] { 58 });
            Assert.True(retValue.Equals(58));

            dlgt = miMethod2StringGeneric.CreateDelegate(typeof(Delegate_Void_T<String>), "firstArg");
            retValue = ((Delegate_Void_T<String>)dlgt).DynamicInvoke();

            Assert.True(retValue.Equals("firstArg"));
        }

        // Gets MethodInfo object from current class
        public static MethodInfo GetMethod(string method)
        {
            return GetMethod(typeof(MethodInfoCreateDelegateTests), method);
        }

        //Gets MethodInfo object from a Type
        public static MethodInfo GetMethod(Type t, string method)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> alldefinedMethods = ti.DeclaredMethods.GetEnumerator();
            MethodInfo mi = null;

            while (alldefinedMethods.MoveNext())
            {
                if (alldefinedMethods.Current.Name.Equals(method))
                {
                    //found method
                    mi = alldefinedMethods.Current;
                    break;
                }
            }
            return mi;
        }
    }

    public delegate int Delegate_TC_Int(ClassA tc);
    public delegate int Delegate_Void_Int();
    public delegate String Delegate_Str_Str(String x);
    public delegate String Delegate_Void_Str();
    public delegate String Delegate_DateTime_Str(ClassA tc, DateTime dt);

    public delegate T Delegate_GC_T_T<T>(GenericClass<T> gc, T x);
    public delegate T Delegate_T_T<T>(T x);
    public delegate T Delegate_Void_T<T>();

    public class ClassA
    {
        public virtual int PublicInstanceMethod() { return 17; }
        private int PrivateInstanceMethod() { return 21; }
        public static String PublicStaticMethod(String x) { return x; }
        public string PublicStructMethod(DateTime dt) { return dt.ToString(); }
    }

    public class SubClassA : ClassA
    {
        public override int PublicInstanceMethod() { return 79; }
    }

    public class DummyClass
    {
        public int DummyMethod() { return -1; }
        public override String ToString() { return "DummyClass"; }
    }

    public class GenericClass<T>
    {
        public T Method1(T t) { return t; }
        public static S Method2<S>(S s) { return s; }
    }
}
