// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class MethodInvokeTests
    {
        //Verify ConstructorInfo.Invoke return a new Object
        [Fact]
        public void TestInvoke1()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoInvokeSample));
            Assert.Equal(cis.Length, 3);
            ConstructorInfoInvokeSample obj = (ConstructorInfoInvokeSample)cis[0].Invoke(null);
            Assert.NotNull(obj);
        }

        //Verify ConstructorInfo.Invoke(null ,null) for static ctor return null Object
        [Fact]
        public void TestInvoke2()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoClassA));
            Object obj = cis[0].Invoke(null, new object[] { });
            Assert.Null(obj);
        }

        //Verify MemberAccessException is thrown for Static ctor , for Invoke(new object[] { })
        [Fact]
        public void TestInvoke3()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoClassA));
            Object obj = null;
            Assert.Throws<MemberAccessException>(() => { obj = cis[0].Invoke(new object[] { }); });
            Assert.Null(obj);
        }

        //Try to Invoke Array Ctors for 1D Array
        [Fact]
        public void TestInvoke4()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(System.Object[]));
            object[] arr = null;
            //Test data for array length
            int[] arraylength = { 1, 2, 99, 65535 };

            //try to invoke Array ctors with different lengths
            foreach (int length in arraylength)
            {
                //create big Array with  elements 
                arr = (object[])cis[0].Invoke(new Object[] { length });
                Assert.NotNull(arr);
                Assert.Equal(arr.Length, length);
            }
        }

        //Try to Invoke Array Ctors for 1D Array with negative length
        [Fact]
        public void TestInvoke5()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(System.Object[]));
            object[] arr = null;
            //Test data for array length
            int[] arraylength = { -1, -2, -99 };
            //try to invoke Array ctors with different lengths
            foreach (int length in arraylength)
            {
                //create big Array with  elements 
                Assert.Throws<OverflowException>(() => { arr = (object[])cis[0].Invoke(new Object[] { length }); });
            }
        }

        //Try to Invoke Ctor with one parameter
        [Fact]
        public void TestInvoke6()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            obj = (ConstructorInfoInvokeSample)cis[1].Invoke(new object[] { 100 });

            Assert.NotNull(obj);
            Assert.Equal(obj.intValue, 100);
        }

        //Try to Invoke Ctor with two parameters
        [Fact]
        public void TestInvoke7()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            obj = (ConstructorInfoInvokeSample)cis[2].Invoke(new object[] { 101, "hello" });

            Assert.NotNull(obj);
            Assert.Equal(obj.intValue, 101);
            Assert.Equal(obj.strValue, "hello");
        }

        //Try to Invoke Ctor with two parameters without providing any param
        [Fact]
        public void TestInvoke8()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            Assert.Throws<TargetParameterCountException>(() => { obj = (ConstructorInfoInvokeSample)cis[2].Invoke(new object[] { }); });
        }

        //Try to Invoke Ctor with two parameters with param mismatch
        [Fact]
        public void TestInvoke9()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            Assert.Throws<TargetParameterCountException>(() => { obj = (ConstructorInfoInvokeSample)cis[2].Invoke(new object[] { 121 }); });
        }

        //Try to Invoke Ctor with one parameter with wrong type 
        [Fact]
        public void TestInvoke10()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            Assert.Throws<ArgumentException>(() => { obj = (ConstructorInfoInvokeSample)cis[1].Invoke(new object[] { "hello" }); });
        }

        // calling a contstructor on an existing instance.  Note this should not produce
        // a second object.  I suppose you would use this in a situation where you wanted to reset
        // the state of an object.
        //
        [Fact]
        public void TestInvoke11()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj1 = new ConstructorInfoInvokeSample(100, "hello");
            ConstructorInfoInvokeSample obj2 = null;
            obj2 = (ConstructorInfoInvokeSample)cis[2].Invoke(obj1, new object[] { 999, "initialized" });
            Assert.Null(obj2);
            Assert.Equal(obj1.intValue, 999);
            Assert.Equal(obj1.strValue, "initialized");
        }

        //Try to Invoke abstract class Ctor 
        [Fact]
        public void TestInvoke12()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(Base));
            Object obj = null;
            Assert.Throws<MemberAccessException>(() => { obj = (Base)cis[0].Invoke(new object[] { }); });
        }

        //Try to Invoke Derived class Ctor 
        [Fact]
        public void TestInvoke13()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(Derived));
            Derived obj = null;
            obj = (Derived)cis[0].Invoke(new object[] { });
            Assert.NotNull(obj);
        }

        //Try to Invoke ctor for struct 
        [Fact]
        public void TestInvoke14()
        {
            ConstructorInfo[] cis = GetConstructor(typeof(MyStruct));
            MyStruct obj;
            obj = (MyStruct)cis[0].Invoke(new object[] { 1, 2 });
            Assert.NotNull(obj);
            Assert.Equal(obj.x, 1);
            Assert.Equal(obj.y, 2);
        }

        //Gets ConstructorInfo object from a Type
        public static ConstructorInfo[] GetConstructor(Type t)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<ConstructorInfo> allctors = ti.DeclaredConstructors.GetEnumerator();
            List<ConstructorInfo> clist = new List<ConstructorInfo>();
            while (allctors.MoveNext())
            {
                clist.Add(allctors.Current);
            }

            return clist.ToArray();
        }
    }

    //Metadata for Reflection
    public class ConstructorInfoInvokeSample
    {
        public int intValue = 0;
        public string strValue = "";

        public ConstructorInfoInvokeSample() { }

        public ConstructorInfoInvokeSample(int i)
        {
            this.intValue = i;
        }

        public ConstructorInfoInvokeSample(int i, string s)
        {
            this.intValue = i;
            this.strValue = s;
        }

        public string Method1(DateTime t)
        {
            return "";
        }
    }

    public class ConstructorInfoClassA
    {
        static ConstructorInfoClassA()
        {
        }
    }

    public abstract class Base
    {
        public Base()
        {
        }
    }

    public class Derived : Base
    {
        public Derived()
        {
        }
    }

    public struct MyStruct
    {
        public int x;
        public int y;

        public MyStruct(int p1, int p2)
        {
            x = p1;
            y = p2;
        }
    };
}
