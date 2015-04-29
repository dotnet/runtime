// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System;
using System.Reflection;
using Xunit;
using System.Reflection.Compatibility.UnitTests;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    // System.Reflection.FieldInfo.GetValue(System.Object)
    public class FieldInfoGetValue1
    {
        // Positive Test 1: Test a general field in a system defined class
        [Fact]
        public void PosTest1()
        {
            string str = "Test String Value";
            Type type = typeof(System.String);
            FieldInfo fieldinfo = type.GetField("Empty");
            object obj = fieldinfo.GetValue(str);
            Assert.Equal("", obj.ToString());
        }

        // Positive Test 2: Test a string field in a customized class
        [Fact]
        public void PosTest2()
        {
            string argu_str1 = "ArgumentString1";
            string argu_str2 = "ArgumentString2";
            TestClassA str = new TestClassA(argu_str1, argu_str2);
            Type type = typeof(TestClassA);
            FieldInfo fieldinfo = type.GetField("str1");
            object obj = fieldinfo.GetValue(str);
            Assert.Equal(argu_str1, obj.ToString());
        }

        // Positive Test 3: Test a field of a sub class derived from its base class
        [Fact]
        public void PosTest3()
        {
            int int1 = new Random().Next(int.MinValue, int.MaxValue);
            subclass sub = new subclass(int1);
            Type type = typeof(subclass);
            FieldInfo fieldinfo = type.GetField("v_int", BindingFlags.NonPublic | BindingFlags.Instance);
            object obj = fieldinfo.GetValue(sub);
            Assert.Equal(int1, (int)obj);
        }

        // Positive Test 4: Test a nullable type field in a customized class
        [Fact]
        public void PosTest4()
        {
            TestClassA str = new TestClassA();
            Type type = typeof(TestClassA);
            FieldInfo fieldinfo = type.GetField("v_null_int");
            object obj = fieldinfo.GetValue(str);
            Assert.Null(obj);
        }

        // Positive Test 5: Get the object of a customized class type
        [Fact]
        public void PosTest5()
        {
            TestClassA str = new TestClassA();
            Type type = typeof(TestClassA);
            FieldInfo fieldinfo = type.GetField("tc");
            object obj = fieldinfo.GetValue(str);
            int int1 = (obj as TypeClass).value;
            Assert.Equal(1000, int1);
        }

        // Positive Test 6: Test a generic type field
        [Fact]
        public void PosTest6()
        {
            genClass<int> str = new genClass<int>(12345);
            Type type = typeof(genClass<int>);
            FieldInfo fieldinfo = type.GetField("t");
            object obj = fieldinfo.GetValue(str);
            Assert.Equal(12345, (int)obj);
        }

        // Positive Test 7: Test a static field
        [Fact]
        public void PosTest7()
        {
            Type type = typeof(TestClassA);
            TestClassA.sta_int = -99;
            FieldInfo fieldinfo = type.GetField("sta_int");
            object obj = fieldinfo.GetValue(null);
            Assert.Equal(-99, (int)obj);
        }

        // Negative Test 1: The argument object is null reference
        [Fact]
        public void NegTest1()
        {
            try
            {
                genClass<int> str = new genClass<int>(12345);
                Type type = typeof(genClass<int>);
                FieldInfo fieldinfo = type.GetField("t");
                object obj = fieldinfo.GetValue(null);
                Assert.True(false);
            }
            catch (Exception e)
            {
                if (e.GetType().ToString() != "System.Reflection.TargetException")
                {
                    Assert.True(false);
                }
            }
        }


        #region Test helper classes
        public class TestClassA
        {
            public string str1;
            public string str2;
            public TestClassA(string a, string b)
            {
                str1 = a;
                str2 = b;
            }
            public TestClassA()
            {
                v_null_int = null;
                tc = new TypeClass();
                _vpri = 100;
            }
            protected int v_int;
            public int? v_null_int;
            public TypeClass tc;
            public static int sta_int;
            private int _vpri;
            public void usingv()
            {
                int a = _vpri;
            }
        }
        public class subclass : TestClassA
        {
            public subclass(int c)
                : base(null, null)
            {
                v_int = c;
            }
        }

        public class genClass<T>
        {
            public T t;
            public genClass(T value)
            {
                t = value;
            }
        }
        public class TypeClass
        {
            public TypeClass()
            {
                value = 1000;
            }
            public int value;
        }
        #endregion
    }
}