// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            Assert.Throws<ArgumentException>(null, () => miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Int)));
        }

        //Verify ArgumentNullExcpeption when type is null
        [Fact]
        public void TestCreateDelegate5()
        {
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            Assert.Throws<ArgumentNullException>("delegateType", () => miPublicInstanceMethod.CreateDelegate(null));
        }

        //create closed instance delegate with incorrect delegate type
        [Fact]
        public void TestCreateDelegate6()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            Assert.Throws<ArgumentException>(null, () => miPublicInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int), TestClass));
        }

        //Verify ArgumentNullExcpeption when type is null
        [Fact]
        public void TestCreateDelegate7()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            Assert.Throws<ArgumentNullException>("delegateType", () => miPublicInstanceMethod.CreateDelegate(null, TestClass));
        }

        [Theory]
        //closed instance delegate with incorrect object type
        [InlineData(typeof(Delegate_Void_Int))]
        //create closed static method with incorrect argument
        [InlineData(typeof(Delegate_Void_Str))]

        public void TestCreateDelegate(Type testData)
        {
            MethodInfo miPublicInstanceMethod = GetMethod(typeof(ClassA), "PublicInstanceMethod");
            Assert.Throws<ArgumentException>(null, () => miPublicInstanceMethod.CreateDelegate(testData, new DummyClass()));
        }
        
        [Fact]
        public void TestCreateDelegate10()
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeof(ClassA));
            MethodInfo miPublicStructMethod = GetMethod(typeof(ClassA), "PublicStructMethod");
            ClassA classAObj = new ClassA();
            Delegate dlgt = miPublicStructMethod.CreateDelegate(typeof(Delegate_DateTime_Str));
            object retValue = ((Delegate_DateTime_Str)dlgt).DynamicInvoke(new object[] { classAObj, null });
            object actualReturnValue = classAObj.PublicStructMethod(new DateTime());
            Assert.True(retValue.Equals(actualReturnValue));
        }

        public void RunBasicTestsHelper(Type typeTestClass)
        {
            ClassA TestClass = (ClassA)Activator.CreateInstance(typeTestClass);
            MethodInfo miPublicInstanceMethod = GetMethod(typeTestClass, "PublicInstanceMethod");
            MethodInfo miPrivateInstanceMethod = GetMethod(typeTestClass, "PrivateInstanceMethod");
            MethodInfo miPublicStaticMethod = GetMethod(typeTestClass, "PublicStaticMethod");
            Delegate dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int));
            object retValue = ((Delegate_TC_Int)dlgt).DynamicInvoke(new object[] { TestClass });
            Assert.True(retValue.Equals(TestClass.PublicInstanceMethod()));
            Assert.NotNull(miPrivateInstanceMethod);
            dlgt = miPrivateInstanceMethod.CreateDelegate(typeof(Delegate_TC_Int));
            retValue = ((Delegate_TC_Int)dlgt).DynamicInvoke(new object[] { TestClass });
            Assert.True(retValue.Equals(21));
            dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Int), TestClass);
            retValue = ((Delegate_Void_Int)dlgt).DynamicInvoke(null);

            Assert.True(retValue.Equals(TestClass.PublicInstanceMethod()));
            dlgt = miPublicStaticMethod.CreateDelegate(typeof(Delegate_Str_Str));
            retValue = ((Delegate_Str_Str)dlgt).DynamicInvoke(new object[] { "85" });

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
            object retValue = ((Delegate_TC_Int)dlgt).DynamicInvoke(new object[] { TestSubClass });

            Assert.True(retValue.Equals(TestSubClass.PublicInstanceMethod()));
            dlgt = miPublicInstanceMethod.CreateDelegate(typeof(Delegate_Void_Int), TestSubClass);
            retValue = ((Delegate_Void_Int)dlgt).DynamicInvoke();

            Assert.True(retValue.Equals(TestSubClass.PublicInstanceMethod()));
        }

        public void RunGenericTestsHelper(Type typeGenericClassString)
        {
            GenericClass<string> genericClass = (GenericClass<string>)Activator.CreateInstance(typeGenericClassString);

            MethodInfo miMethod1String = GetMethod(typeGenericClassString, "Method1");
            MethodInfo miMethod2String = GetMethod(typeGenericClassString, "Method2");

            MethodInfo miMethod2IntGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(int) });
            MethodInfo miMethod2StringGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(string) });

            Delegate dlgt = miMethod1String.CreateDelegate(typeof(Delegate_GC_T_T<string>));
            object retValue = ((Delegate_GC_T_T<string>)dlgt).DynamicInvoke(new object[] { genericClass, "TestGeneric" });

            Assert.True(retValue.Equals(genericClass.Method1("TestGeneric")));

            dlgt = miMethod1String.CreateDelegate(typeof(Delegate_T_T<string>), genericClass);
            retValue = ((Delegate_T_T<string>)dlgt).DynamicInvoke(new object[] { "TestGeneric" });

            Assert.True(retValue.Equals(genericClass.Method1("TestGeneric")));

            dlgt = miMethod2IntGeneric.CreateDelegate(typeof(Delegate_T_T<int>));
            retValue = ((Delegate_T_T<int>)dlgt).DynamicInvoke(new object[] { 58 });
            Assert.True(retValue.Equals(58));

            dlgt = miMethod2StringGeneric.CreateDelegate(typeof(Delegate_Void_T<string>), "firstArg");
            retValue = ((Delegate_Void_T<string>)dlgt).DynamicInvoke();

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
    public delegate string Delegate_Str_Str(string x);
    public delegate string Delegate_Void_Str();
    public delegate string Delegate_DateTime_Str(ClassA tc, DateTime dt);

    public delegate T Delegate_GC_T_T<T>(GenericClass<T> gc, T x);
    public delegate T Delegate_T_T<T>(T x);
    public delegate T Delegate_Void_T<T>();

    public class ClassA
    {
        public virtual int PublicInstanceMethod() { return 17; }
        private int PrivateInstanceMethod() { return 21; }
        public static string PublicStaticMethod(string x) { return x; }
        public string PublicStructMethod(DateTime dt) { return dt.ToString(); }
    }

    public class SubClassA : ClassA
    {
        public override int PublicInstanceMethod() { return 79; }
    }

    public class DummyClass
    {
        public int DummyMethod() { return -1; }
        public override string ToString() { return "DummyClass"; }
    }

    public class GenericClass<T>
    {
        public T Method1(T t) { return t; }
        public static S Method2<S>(S s) { return s; }
    }
}
