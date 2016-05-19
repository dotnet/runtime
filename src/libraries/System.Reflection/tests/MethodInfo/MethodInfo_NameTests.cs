// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class MethodInfoNameTests
    {

        //Verify Method Signatures
        [Theory]
        [InlineData(typeof(MethodInfoNameTests), "DummyMethod1")]
        [InlineData(typeof(MethodInfoNameTests), "PrintStringArray")]
        [InlineData(typeof(MethodInfoNameTests), "DummyMethod2")]
        [InlineData(typeof(MethodInfoInterlocked2), "Increment")]
        [InlineData(typeof(MethodInfoInterlocked2), "Decrement")]
        [InlineData(typeof(MethodInfoInterlocked2), "Exchange")]
        [InlineData(typeof(MethodInfoInterlocked2), "CompareExchange")]

        public static void TestMethodName(Type type, string methodName)
        {
            MethodInfo mi = GetMethod(type, methodName);
            Assert.NotNull(mi);
            Assert.True(mi.Name.Equals(methodName));
        }

        public static MethodInfo GetMethod(string method)
        {
            return GetMethod(typeof(MethodInfoNameTests), method);
        }

        public static MethodInfo GetMethod(Type t, string method)
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

    // Class For Reflection Metadata
    public class MethodInfoInterlocked2
    {
        public MethodInfoInterlocked2()
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
}
