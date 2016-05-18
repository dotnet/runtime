// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class MethodInfoToStringTests
    {

        //Verify Method Signatures ref parameters
        [Theory]
        [InlineData(typeof(MethodInfoToStringTests), "DummyMethod1", "Void DummyMethod1(System.String, Int32, Int64)")]
        [InlineData(typeof(MethodInfoToStringTests), "PrintStringArray", "Void PrintStringArray(System.String[])")]
        [InlineData(typeof(MethodInfoToStringTests), "DummyMethod2", "Void DummyMethod2()")]
        [InlineData(typeof(MethodInfoInterlocked3), "Increment", "Int32 Increment(Int32 ByRef)")]
        [InlineData(typeof(MethodInfoInterlocked3), "Decrement", "Int32 Decrement(Int32 ByRef)")]
        [InlineData(typeof(MethodInfoInterlocked3), "Exchange", "Int32 Exchange(Int32 ByRef, Int32)")]
        [InlineData(typeof(MethodInfoInterlocked3), "CompareExchange", "Int32 CompareExchange(Int32 ByRef, Int32, Int32)")]
        [InlineData(typeof(MethodInfoToStringSample), "Method1", "System.String Method1(System.DateTime)")]
        [InlineData(typeof(MethodInfoToStringSample), "Method2", "System.String Method2[T,S](System.String, T, S)")]
        [InlineData(typeof(MethodInfoToStringSampleG<>), "Method1", "T Method1(T)")]
        [InlineData(typeof(MethodInfoToStringSampleG<>), "Method2", "T Method2[S](S, T, System.String)")]
        [InlineData(typeof(MethodInfoToStringSampleG<string>), "Method1", "System.String Method1(System.String)")]
        [InlineData(typeof(MethodInfoToStringSampleG<string>), "Method2", "System.String Method2[S](S, System.String, System.String)")]

        public static void TestMethodSignature(Type type, string methodName, string methodSign)
        {
            MethodInfo mi = getMethod(type, methodName);

            Assert.NotNull(mi);

            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase));

            Assert.True(mi.ToString().Equals(methodSign, StringComparison.CurrentCultureIgnoreCase));
        }
       
        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature()
        {
            //Make generic method
            MethodInfo gmi = getMethod(typeof(MethodInfoToStringSampleG<string>), "Method2").MakeGenericMethod(new Type[] { typeof(DateTime) });
            string signature = "System.String Method2[DateTime](System.DateTime, System.String, System.String)";

            Assert.True(gmi.ToString().Equals(signature));
        }

        public static MethodInfo getMethod(Type t, string method)
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

        //Methods for Reflection Metadata  
        public void DummyMethod1(string str, int iValue, long lValue)
        {
        }

        public void DummyMethod2()
        {
        }

        public void PrintStringArray(string[] strArray)
        {
            for (int ii = 0; ii < strArray.Length; ++ii)
            {
            }
        }
    }

    // Classes For Reflection Metadata
    public class MethodInfoInterlocked3
    {
        public MethodInfoInterlocked3()
        {
        }

        public static int Increment(ref int location) { return 0; }
        public static int Decrement(ref int location) { return 0; }
        public static int Exchange(ref int location1, int value) { return 0; }
        public static int CompareExchange(ref int location1, int value, int comparand) { return 0; }

        public static float Exchange(ref float location1, float value) { return 0; }
        public static float CompareExchange(ref float location1, float value, float comparand) { return 0; }

        public static object Exchange(ref object location1, object value) { return null; }
        public static object CompareExchange(ref object location1, object value, object comparand) { return null; }
    }

    public class MethodInfoToStringSample
    {
        public string Method1(DateTime t)
        {
            return "";
        }
        public string Method2<T, S>(string t2, T t1, S t3)
        {
            return "";
        }
    }

    public class MethodInfoToStringSampleG<T>
    {
        public T Method1(T t)
        {
            return t;
        }
        public T Method2<S>(S t1, T t2, string t3)
        {
            return t2;
        }
    }
}
