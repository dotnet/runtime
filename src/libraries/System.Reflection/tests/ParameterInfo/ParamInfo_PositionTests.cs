// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class ParamInfoPositionTests
    {
        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamPosition1()
        {
            VerifyMethodParamPosition(typeof(MyClass), "Method1", new string[] { "str", "iValue", "lValue" });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes2()
        {
            VerifyMethodParamPosition(typeof(MyClass), "Method2", new string[] { });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes3()
        {
            VerifyMethodParamPosition(typeof(MyClass), "MethodWithArray", new string[] { "strArray" });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes4()
        {
            VerifyMethodParamPosition(typeof(MyClass), "virtualMethod", new string[] { "data" });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes5()
        {
            VerifyMethodParamPosition(typeof(MyClass), "MethodWithRefKW", new string[] { "str" });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes6()
        {
            VerifyMethodParamPosition(typeof(MyClass), "MethodWithOutKW", new string[] { "i", "str" });
        }


        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes7()
        {
            VerifyMethodParamPosition(typeof(GenericClass<string>), "genericMethod", new string[] { "t" });
        }



        //Helper Method to Verify Parameters
        private static void VerifyMethodParamPosition(Type type, string methodName, string[] paramsArray)
        {
            MethodInfo mi = getMethod(type, methodName);

            Assert.NotNull(mi);
            ParameterInfo[] allparams = mi.GetParameters();

            Assert.NotNull(allparams);

            Assert.Equal(paramsArray.Length, allparams.Length);

            for (int i = 0; i < paramsArray.Length; i++)
                Assert.Equal(i, allparams[i].Position);
        }


        private static MethodInfo getMethod(Type t, string method)
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


        // Class For Reflection Metadata
        public class MyClass
        {
            public void Method1(String str, int iValue, long lValue)
            {
            }

            public void Method2()
            {
            }

            public void MethodWithArray(String[] strArray)
            {
            }

            public virtual void virtualMethod(long data)
            {
            }

            public void MethodWithRefKW(ref String str)
            {
                str = "newstring";
            }

            public void MethodWithOutKW(int i, out String str)
            {
                str = "newstring";
            }
        }

        public class GenericClass<T>
        {
            public void genericMethod(T t) { }
        }
    }
}
