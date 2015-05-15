// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class ParameterInfoNameTests
    {
        //Verify Method Params
        [Fact]
        public static void TestMethodParams1()
        {
            VerifyMethodParams(typeof(MyClass), "Method1", new string[] { "str", "ivalue", "lvalue" });
        }


        //Verify Method Params
        [Fact]
        public static void TestMethodParams2()
        {
            VerifyMethodParams(typeof(MyClass), "Method2", new string[] { });
        }

        //Verify Method Params
        [Fact]
        public static void TestMethodParams3()
        {
            VerifyMethodParams(typeof(MyClass), "MethodWithArray", new string[] { "strArray" });
        }

        //Verify Method Params
        [Fact]
        public static void TestMethodParams4()
        {
            VerifyMethodParams(typeof(MyClass), "virtualMethod", new string[] { "data" });
        }

        //Verify Method Params
        [Fact]
        public static void TestMethodParams5()
        {
            VerifyMethodParams(typeof(MyClass), "MethodWithRefKW", new string[] { "str" });
        }

        //Verify Method Params
        [Fact]
        public static void TestMethodParams6()
        {
            VerifyMethodParams(typeof(MyClass), "MethodWithOutKW", new string[] { "i", "str" });
        }

        //Verify Method Params
        [Fact]
        public static void TestMethodParams7()
        {
            VerifyMethodParams(typeof(GenericClass<string>), "genericMethod", new string[] { "t" });
        }



        //Helper Method to Verify Parameters
        private static void VerifyMethodParams(Type type, string methodName, string[] paramsArray)
        {
            MethodInfo mi = GetMethod(type, methodName);

            Assert.NotNull(mi);
            ParameterInfo[] allparams = mi.GetParameters();

            Assert.NotNull(allparams);

            Assert.Equal(paramsArray.Length, allparams.Length);

            for (int i = 0; i < paramsArray.Length; i++)
                Assert.True(paramsArray[i].Equals(allparams[i].Name, StringComparison.CurrentCultureIgnoreCase));
        }

        private static MethodInfo GetMethod(Type t, string method)
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
