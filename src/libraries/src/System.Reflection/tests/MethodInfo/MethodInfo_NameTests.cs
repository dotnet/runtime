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
        public static IEnumerable<object> TestMethodNameData()
        {
            yield return new object[] { typeof(MethodInfoNameTests), "DummyMethod1" };
            yield return new object[] { typeof(MethodInfoNameTests), "PrintStringArray" };
            yield return new object[] { typeof(MethodInfoNameTests), "DummyMethod2" };
            yield return new object[] { typeof(MethodInfoInterlocked2), "Increment" };
            yield return new object[] { typeof(MethodInfoInterlocked2), "Decrement" };
            yield return new object[] { typeof(MethodInfoInterlocked2), "Exchange" };
            yield return new object[] { typeof(MethodInfoInterlocked2), "CompareExchange" };
        }

        [Theory]
        [MemberData(nameof(TestMethodNameData))]

        public static void TestMethodName(Type type, string str1)
        {
            VerifyMethodName(type, str1);
        }

        //Helper Method to Verify Method Signature
        public static void VerifyMethodName(string methodName)
        {
            VerifyMethodName(typeof(MethodInfoNameTests), methodName);
        }

        //Helper Method to Verify Signatures
        public static void VerifyMethodName(Type type, string methodName)
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
        public void DummyMethod1(String str, int iValue, long lValue)
        {
        }

        public void DummyMethod2()
        {
        }

        public void PrintStringArray(String[] strArray)
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

        public static Object Exchange(ref Object location1, Object value) { return null; }
        public static Object CompareExchange(ref Object location1, Object value, Object comparand) { return null; }
    }
}
