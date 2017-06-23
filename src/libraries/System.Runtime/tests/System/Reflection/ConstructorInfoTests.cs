// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public static class ConstructorInfoTests
    {
        public static IEnumerable<object[]> Equality_TestData()
        {
            ConstructorInfo[] methodSampleConstructors1 = GetConstructors(typeof(ConstructorInfoInvokeSample));
            ConstructorInfo[] methodSampleConstructors2 = GetConstructors(typeof(ConstructorInfoInvokeSample));
            yield return new object[] { methodSampleConstructors1[0], methodSampleConstructors2[0], true };
            yield return new object[] { methodSampleConstructors1[1], methodSampleConstructors2[1], true };
            yield return new object[] { methodSampleConstructors1[2], methodSampleConstructors2[2], true };
            yield return new object[] { methodSampleConstructors1[1], methodSampleConstructors2[2], false };
        }
        
        [Theory]
        [MemberData(nameof(Equality_TestData))]
        public static void Test_Equality(ConstructorInfo constructorInfo1, ConstructorInfo constructorInfo2, bool expected)
        {
            Assert.Equal(expected, constructorInfo1 == constructorInfo2);
            Assert.NotEqual(expected, constructorInfo1 != constructorInfo2);
        }

        [Fact]
        public static void Verify_Invoke_ReturnsNewObject()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoInvokeSample));
            Assert.Equal(cis.Length, 3);
            ConstructorInfoInvokeSample obj = (ConstructorInfoInvokeSample)cis[0].Invoke(null);
            Assert.NotNull(obj);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Invoking static constructors not supported on UapAot")]
        public static void TestInvoke_Nullery()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoClassA));
            Object obj = cis[0].Invoke(null, new object[] { });
            Assert.Null(obj);
        }

        [Fact]
        public static void TestInvokeNeg()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoClassA));
            Object obj = null;
            Assert.Throws<MemberAccessException>(() => { obj = cis[0].Invoke(new object[] { }); });
            Assert.Null(obj);
        }

        [Fact]
        public static void TestInvoke_1DArray()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(System.Object[]));
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

        [Fact]
        public static void TestInvoke_1DArrayWithNegativeLength()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(System.Object[]));
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

        [Fact]
        public static void TestInvokeWithOneParam()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            obj = (ConstructorInfoInvokeSample)cis[1].Invoke(new object[] { 100 });

            Assert.NotNull(obj);
            Assert.Equal(obj.intValue, 100);
        }

        [Fact]
        public static void TestInvokeWithTwoParams()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            obj = (ConstructorInfoInvokeSample)cis[2].Invoke(new object[] { 101, "hello" });

            Assert.NotNull(obj);
            Assert.Equal(obj.intValue, 101);
            Assert.Equal(obj.strValue, "hello");
        }

        [Fact]
        public static void TestInvoke_NoParamsProvided()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            Assert.Throws<TargetParameterCountException>(() => { obj = (ConstructorInfoInvokeSample)cis[2].Invoke(new object[] { }); });
        }

        [Fact]
        public static void TestInvoke_ParamMismatch()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            Assert.Throws<TargetParameterCountException>(() => { obj = (ConstructorInfoInvokeSample)cis[2].Invoke(new object[] { 121 }); });
        }

        [Fact]
        public static void TestInvoke_WrongParamType()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj = null;
            AssertExtensions.Throws<ArgumentException>(null, () => { obj = (ConstructorInfoInvokeSample)cis[1].Invoke(new object[] { "hello" }); });
        }

        [Fact]
        public static void TestInvoke_ConstructorOnExistingInstance()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(ConstructorInfoInvokeSample));
            //try to invoke Array ctors with one param
            ConstructorInfoInvokeSample obj1 = new ConstructorInfoInvokeSample(100, "hello");
            ConstructorInfoInvokeSample obj2 = null;
            obj2 = (ConstructorInfoInvokeSample)cis[2].Invoke(obj1, new object[] { 999, "initialized" });
            Assert.Null(obj2);
            Assert.Equal(obj1.intValue, 999);
            Assert.Equal(obj1.strValue, "initialized");
        }

        [Fact]
        public static void TestInvoke_AbstractConstructor()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(Base));
            Object obj = null;
            Assert.Throws<MemberAccessException>(() => { obj = (Base)cis[0].Invoke(new object[] { }); });
        }

        [Fact]
        public static void TestInvoke_DerivedConstructor()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(Derived));
            Derived obj = null;
            obj = (Derived)cis[0].Invoke(new object[] { });
            Assert.NotNull(obj);
        }

        [Fact]
        public static void TestInvoke_Struct()
        {
            ConstructorInfo[] cis = GetConstructors(typeof(MyStruct));
            MyStruct obj;
            obj = (MyStruct)cis[0].Invoke(new object[] { 1, 2 });
            Assert.NotNull(obj);
            Assert.Equal(obj.x, 1);
            Assert.Equal(obj.y, 2);
        }

        public static ConstructorInfo[] GetConstructors(Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors.ToArray();
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

    public static class ConstructorInfoClassA
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
