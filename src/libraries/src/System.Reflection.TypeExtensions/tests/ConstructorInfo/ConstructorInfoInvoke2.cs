// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Globalization;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // Invoke(System.Reflection.BindingFlags, System.Reflection.Binder, System.Object[], System.Globalization.CultureInfo)
    public class ConstructorInfoInvoke2
    {
        public class TestClass
        {
            public TestClass() { }
            public TestClass(int i) { }
            public TestClass(int i, String s) { }
            public TestClass(String s, int i) { }
            public TestClass(int i, int j, int k) { throw new System.Exception(); }
        }

        // Positive Test 1: Ensure it can be called with default array
        [Fact]
        public void PosTest1()
        {
            ConstructorInfo ci = typeof(TestClass).GetConstructor(new Type[] { });
            Object[] testarray = { };
            Object myobject;
            myobject = ci.Invoke(testarray);
            Assert.NotNull(myobject);
        }

        // Positive Test 2: Ensure it can be called with array having one element
        [Fact]
        public void PosTest2()
        {
            Type[] types = new Type[1];
            types[0] = typeof(int);
            ConstructorInfo ci = typeof(TestClass).GetConstructor(types);
            Object[] testarray = new Object[1];
            testarray[0] = 1;
            Object myobject;
            myobject = ci.Invoke(testarray);
            Assert.NotNull(myobject);
        }

        // Positive Test 3: Ensure it can be called with array having two elements (int, string)
        [Fact]
        public void PosTest3()
        {
            Type[] types = new Type[2];
            types[0] = typeof(int);
            types[1] = typeof(String);
            ConstructorInfo ci = typeof(TestClass).GetConstructor(types);
            Object[] testarray = new Object[2];
            testarray[0] = 1;
            testarray[1] = "Hello, Test!";
            Object myobject;
            myobject = ci.Invoke(testarray);
            Assert.NotNull(myobject);
        }

        // Positive Test 4: Ensure it can be called with array having two elements (string, int)
        [Fact]
        public void PosTest4()
        {
            Type[] types = new Type[2];
            types[0] = typeof(String);
            types[1] = typeof(int);
            ConstructorInfo ci = typeof(TestClass).GetConstructor(types);
            Object[] testarray = new Object[2];
            testarray[0] = "Hello, Test!";
            testarray[1] = 1;
            Object myobject;
            myobject = ci.Invoke(testarray);
            Assert.NotNull(myobject);
        }

        // Positive Test 5: Ensure it can be called with array in which there is a null
        [Fact]
        public void PosTest5()
        {
            Type[] types = new Type[2];
            types[0] = typeof(String);
            types[1] = typeof(int);
            ConstructorInfo ci = typeof(TestClass).GetConstructor(types);
            Object[] testarray = new Object[2];
            testarray[0] = null;
            testarray[1] = 1;
            Object myobject;
            myobject = ci.Invoke(testarray);
            Assert.NotNull(myobject);
        }

        // Negative Test 1: MemberAccessException should be thrown when the class is abstract.
        [Fact]
        public void NegTest1()
        {
            Object[] testarray = new Object[] { };
            Type type = typeof(TestAbstractClass);
            ConstructorInfo myci = type.GetConstructor(new Type[] { });
            Assert.Throws<MemberAccessException>(() =>
           {
               myci.Invoke(testarray);
           });
        }

        // Negative Test 2: ArgumentException should be thrown when the parameters array does not contain values that match the types accepted by this constructor.
        [Fact]
        public void NegTest2()
        {
            Type[] types = new Type[2];
            types[0] = typeof(int);
            types[1] = typeof(String);
            ConstructorInfo ci = typeof(TestClass).GetConstructor(types);
            Object[] testarray = new Object[2];
            testarray[0] = 1;
            testarray[1] = 2;
            Object myobject;
            Assert.Throws<ArgumentException>(() =>
           {
               myobject = ci.Invoke(testarray);
           });
        }

        // Negative Test 3: TargetInvocationException should be thrown when the invoked constructor throws an exception.
        [Fact]
        public void NegTest3()
        {
            Type[] types = new Type[3];
            types[0] = typeof(int);
            types[1] = typeof(int);
            types[2] = typeof(int);
            ConstructorInfo ci = typeof(TestClass).GetConstructor(types);
            Object[] testarray = new Object[3];
            testarray[0] = 1;
            testarray[1] = 2;
            testarray[2] = 3;
            Object myobject;
            Assert.Throws<TargetInvocationException>(() =>
           {
               myobject = ci.Invoke(testarray);
           });
        }

        // Negative Test 4: TargetParameterCountException should be thrown when an incorrect number of parameters was passed.
        [Fact]
        public void NegTest4()
        {
            Type[] types = new Type[2];
            types[0] = typeof(int);
            types[1] = typeof(String);
            ConstructorInfo ci = typeof(TestClass).GetConstructor(types);
            Object[] testarray = new Object[3];
            testarray[0] = 1;
            testarray[1] = "test";
            testarray[2] = "test1";
            Object myobject;
            Assert.Throws<TargetParameterCountException>(() =>
            {
                myobject = ci.Invoke(testarray);
            });
        }
    }

    // Used by several tests
    public abstract class TestAbstractClass
    {
        public TestAbstractClass() { }

        public abstract void TestAbstractMethod();
    }
}
