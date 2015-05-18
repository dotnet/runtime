// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class ParamInfoTypeTests
    {
        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes1()
        {
            VerifyMethodParamTypes(typeof(MyClass), "Method1", new Type[] { typeof(string), typeof(int), typeof(long) });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes2()
        {
            VerifyMethodParamTypes(typeof(MyClass), "Method2", new Type[] { });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes3()
        {
            VerifyMethodParamTypes(typeof(MyClass), "MethodWithArray", new Type[] { typeof(string[]) });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes4()
        {
            VerifyMethodParamTypes(typeof(MyClass), "virtualMethod", new Type[] { typeof(long) });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes5()
        {
            VerifyMethodParamTypes(typeof(MyClass), "MethodWithRefKW", new Type[] { (typeof(string)).MakeByRefType() });
        }

        //Verify Method Param Type
        [Fact]
        public static void TestMethodParamTypes6()
        {
            VerifyMethodParamTypes(typeof(MyClass), "MethodWithOutKW", new Type[] { typeof(int), (typeof(string)).MakeByRefType() });
        }



        //Helper Method to Verify Parameters
        private static void VerifyMethodParamTypes(Type type, string methodName, Type[] paramsArray)
        {
            MethodInfo mi = getMethod(type, methodName);

            Assert.NotNull(mi);
            ParameterInfo[] allparams = mi.GetParameters();

            Assert.NotNull(allparams);

            Assert.Equal(paramsArray.Length, allparams.Length);

            for (int i = 0; i < paramsArray.Length; i++)
                Assert.True(paramsArray[i].Equals(allparams[i].ParameterType));
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
                for (int ii = 0; ii < strArray.Length; ++ii)
                {
                }
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
    }
}
