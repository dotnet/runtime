// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.MethodInfoTests
{
    public class TestClass
    {
        public T TestGenericMethod<T, U>(T p1, U p2)
        {
            return p1;
        }

        public void TestPartialGenericMethod<T>(T p1, int val)
        {
        }
    }

    public class TestGenericClass<T>
    {
        public void TestMultipleGenericMethod<U>(T p1, U p2)
        {
        }

        public void TestGenericMethod(T p1)
        {
        }

        public void TestMethod(int val)
        {
        }

        public T TestGenericReturnTypeMethod()
        {
            T ret = default(T);

            return ret;
        }
    }

    public class MethodInfoGetGenericArguments
    {
        // Positive Test 1: Call GetGenericArguments on a closed generic method
        [Fact]
        public void PosTest1()
        {
            Type type = typeof(TestGenericClass<int>);

            MethodInfo methodInfo = type.GetMethod("TestGenericMethod");
            Type[] actualArguments = methodInfo.GetGenericArguments();
            int count = actualArguments.Length;

            Assert.Equal(0, count);
        }

        // Positive Test 2: Call GetGenericArguments on a open generic method
        [Fact]
        public void PosTest2()
        {
            Type type = typeof(TestClass);
            String[] desiredArgument = new String[] { "T", "U" };

            MethodInfo methodInfo = type.GetMethod("TestGenericMethod");
            Type[] actualArguments = methodInfo.GetGenericArguments();
            int count = actualArguments.Length;

            Assert.Equal(2, count);
            for (int i = 0; i < actualArguments.Length; ++i)
            {
                Assert.Equal(desiredArgument[i], actualArguments[i].Name);
            }
        }

        // Positive Test 3: Call GetGenericArguments on a method contains open generic type
        [Fact]
        public void PosTest3()
        {
            Type type = typeof(TestClass);
            String[] desiredArgument = new String[] { "T" };

            MethodInfo methodInfo = type.GetMethod("TestPartialGenericMethod");
            Type[] actualArguments = methodInfo.GetGenericArguments();
            int count = actualArguments.Length;

            Assert.Equal(1, count);
            for (int i = 0; i < actualArguments.Length; ++i)
            {
                Assert.Equal(desiredArgument[i], actualArguments[i].Name);
            }
        }

        // Positive Test 4: Call GetGenericArguments on a generic method in a open generic type
        [Fact]
        public void PosTest4()
        {
            Type type = typeof(TestGenericClass<int>);
            String desiredArgument = "U";

            MethodInfo methodInfo = type.GetMethod("TestMultipleGenericMethod");
            Type[] actualArguments = methodInfo.GetGenericArguments();
            int count = actualArguments.Length;

            Assert.Equal(1, count);
            Assert.Equal(desiredArgument, actualArguments[0].Name);
        }

        // Negative Test 1: Call GetGenericArguments on a method does not contains generic argument
        [Fact]
        public void NegTest1()
        {
            Type type = typeof(TestGenericClass<>);
            MethodInfo methodInfo = type.GetMethod("TestMethod");
            Type[] actualArguments = methodInfo.GetGenericArguments();
            Assert.Equal(0, actualArguments.Length);
        }

        // Negative Test 2: Call GetGenericArguments on a method only contains a generic return type
        [Fact]
        public void NegTest2()
        {
            Type type = typeof(TestGenericClass<>);
            MethodInfo methodInfo = type.GetMethod("TestGenericReturnTypeMethod");
            Type[] actualArguments = methodInfo.GetGenericArguments();
            Assert.Equal(0, actualArguments.Length);
        }
    }
}