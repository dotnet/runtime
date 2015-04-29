// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using Xunit;

namespace System.Reflection.Compatibility.UnitTests
{
    // ContainsGenericParameters
    public class MethodBaseContainsGenericParameters
    {
        public class TestClass
        {
            public TestClass()
            {
            }

            public TestClass(int val)
            {
            }

            public void TestGenericMethod<T>(T p1)
            {
            }

            public void TestMethod(int val)
            {
            }

            public void TestMethod2(int val)
            {
            }
            public void TestMethod2(int val1, float val2, string val3)
            {
            }

            public void TestPartialGenericMethod<T>(int val, T p1)
            {
            }

            public T TestGenericReturnTypeMethod<T>()
            {
                T ret = default(T);

                return ret;
            }
        }

        public class TestGenericClass<T>
        {
            public TestGenericClass()
            {
            }

            public TestGenericClass(T val)
            {
            }

            public TestGenericClass(T p, int val)
            {
            }

            public void TestMethod(T p1)
            {
            }

            public void TestMultipleGenericMethod<U>(U p2)
            {
            }

            public void TestVoidMethod()
            {
            }
        }

        // Positive Test 1: ContainsGenericParameters should return true for open generic method
        [Fact]
        public void PosTest1()
        {
            Type type = typeof(TestClass);
            MethodBase methodInfo = type.GetMethod("TestGenericMethod");
            Assert.True(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns false for open generic method" + methodInfo);
        }

        // Positive Test 2: ContainsGenericParameters should return false for a closed generic method
        [Fact]
        public void PosTest2()
        {
            Type type = typeof(TestClass);
            MethodInfo methodInfo = type.GetMethod("TestGenericMethod");
            MethodBase genericMethodInfo = methodInfo.MakeGenericMethod(typeof(int));
            Assert.False(genericMethodInfo.ContainsGenericParameters, "ContainsGenericParameters returns true for closed generic method");
        }

        // Positive Test 3: ContainsGenericParameters should return false for a non generic method
        [Fact]
        public void PosTest3()
        {
            Type type = typeof(TestClass);
            MethodBase methodInfo = type.GetMethod("TestMethod");
            Assert.False(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns true for non generic method");
        }

        // Positive Test 4: ContainsGenericParameters should return true for a generic method contains non generic parameter
        [Fact]
        public void PosTest4()
        {
            Type type = typeof(TestClass);
            MethodBase methodInfo = type.GetMethod("TestPartialGenericMethod");
            Assert.True(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns false for a generic method contains non generic parameter" + methodInfo);
        }

        // Positive Test 5: ContainsGenericParameters should return true for a generic method only contains generic return type
        [Fact]
        public void PosTest5()
        {
            Type type = typeof(TestClass);
            MethodBase methodInfo = type.GetMethod("TestPartialGenericMethod");
            Assert.True(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns false for a generic method only contains generic return type" + methodInfo);
        }

        // Positive Test 6: ContainsGenericParameters should return false for a generic method in a closed generic type
        [Fact]
        public void PosTest6()
        {
            Type type = typeof(TestGenericClass<int>);
            MethodBase methodInfo = type.GetMethod("TestMethod");
            Assert.False(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns true for a generic method in a closed generic type");
        }

        // Positive Test 7: ContainsGenericParameters should return true for a method in a opened generic type
        [Fact]
        public void PosTest7()
        {
            Type type = typeof(TestGenericClass<>);
            MethodBase methodInfo = type.GetMethod("TestMethod");
            Assert.True(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns false for a method in a opened generic type" + methodInfo);
        }

        // Positive Test 8: ContainsGenericParameters should return true for a generic method in a opened generic type
        [Fact]
        public void PosTest8()
        {
            Type type = typeof(TestGenericClass<>);
            MethodBase methodInfo = type.GetMethod("TestMultipleGenericMethod");
            Assert.True(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns false for a generic method in a opened generic type" + methodInfo);
        }

        // Positive Test 9: ContainsGenericParameters should return true for a generic method in a closed generic type
        [Fact]
        public void PosTest9()
        {
            Type type = typeof(TestGenericClass<int>);
            MethodBase methodInfo = type.GetMethod("TestMultipleGenericMethod");
            Assert.True(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns false for a generic method in a closed generic type" + methodInfo);
        }

        // Positive Test 10: ContainsGenericParameters should return true for a method in a generic type
        [Fact]
        public void PosTest10()
        {
            Type type = typeof(TestGenericClass<>);
            MethodBase methodInfo = type.GetMethod("TestVoidMethod");
            Assert.True(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns false for a generic method in a generic type" + methodInfo);
        }

        // Positive Test 11: ContainsGenericParameters should return false for a method in a closed generic type
        [Fact]
        public void PosTest11()
        {
            Type type = typeof(TestGenericClass<int>);
            MethodBase methodInfo = type.GetMethod("TestVoidMethod");
            Assert.False(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns truefor a generic method in a closed generic type");
        }

        // Positive Test 12: ContainsGenericParameters should return false for a constructor in a non generic type
        [Fact]
        public void PosTest12()
        {
            Type type = typeof(TestClass);
            MethodBase methodInfo = type.GetConstructor(new Type[] { });
            Assert.False(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns true for a constructor in a non generic type");

            methodInfo = type.GetConstructor(new Type[] { typeof(int) });
            Assert.False(methodInfo.ContainsGenericParameters, "ContainsGenericParameters returns true for a constructor in a non generic type");
        }

        // Positive Test 13: ContainsGenericParameters should return false for a constructor in a open generic type
        [Fact]
        public void PosTest13()
        {
            Type type = typeof(TestGenericClass<>);
            MethodBase[] ctors = type.GetConstructors();
            for (int i = 0; i < ctors.Length; ++i)
            {
                // ContainsGenericParameters should behave same for both methods and constructors.
                // If method/ctor or the declaring type contains uninstantiated open generic parameter,
                // ContainsGenericParameters should return true.  (Which also means we can't invoke that type)
                Assert.True(ctors[i].ContainsGenericParameters, "ContainsGenericParameters returns false for a constructor" + ctors[i] + " in a open generic type ");
            }
        }

        // Positive Test 14: ContainsGenericParameters should return false for a constructor in a closed generic type
        [Fact]
        public void PosTest14()
        {
            Type type = typeof(TestGenericClass<int>);
            MethodBase[] ctors = type.GetConstructors();
            for (int i = 0; i < ctors.Length; ++i)
            {
                Assert.False(ctors[i].ContainsGenericParameters, "ContainsGenericParameters returns true for a constructor" + ctors[i] + " in a closed generic type");
            }
        }

        // Positive Test 15: Exercise GetMethod(name, Type [])
        [Fact]
        public void PosTest15()
        {
            Type type = typeof(TestClass);
            MethodInfo specificMethod = type.GetMethod("TestMethod2", new Type[] { typeof(int), typeof(float), typeof(string) });
            Assert.NotNull(specificMethod);
        }
    }
}