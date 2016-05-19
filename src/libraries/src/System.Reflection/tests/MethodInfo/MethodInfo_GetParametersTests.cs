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
    public class MethodInfoParametersTests
    {

        //Verify Method Parameters
        [Theory]
        [InlineData(typeof(MethodInfoParametersTests), "DummyMethod1", new string[] { "str", "iValue", "lValue" })]
        [InlineData(typeof(MethodInfoParametersTests), "PrintStringArray", new string[] { "strArray" })]
        [InlineData(typeof(Interlocked2), "Increment", new string[] { "location" })]
        [InlineData(typeof(Interlocked2), "Decrement", new string[] { "location" })]
        [InlineData(typeof(Interlocked2), "Exchange", new string[] { "location1", "value" })]
        [InlineData(typeof(Interlocked2), "CompareExchange", new string[] { "location1", "value", "comparand" })]

        public static void FactParams3(Type type, string methodName, string[] methodParams)
        {
            MethodInfo mi = GetMethod(type, methodName);

            Assert.NotNull(mi);
            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase));

            ParameterInfo[] allparams = mi.GetParameters();

            //Verify number of Parameters
            Assert.Equal(methodParams.Length, allparams.Length);
            //Verify Names of all Params
            for (int i = 0; i < allparams.Length; i++)
            {
                Assert.True(allparams[i].Name.Trim().Equals(methodParams[i], StringComparison.CurrentCultureIgnoreCase));
            }
        }

        //Test case for bug: 1720 (MethodInfo.GetParameters is doing shallow copy instead of deep copy)
        [Fact]
        public static void FactParams4()
        {
            string methodName = "DummyMethod1";
            string[] strParamNames = { "str", "iValue", "lValue" };

            MethodInfo mi = GetMethod(methodName);
            ParameterInfo[] pi = mi.GetParameters();

            if (pi.Length > 1)
                pi[0] = null;  //to force the bug; to Test whether shallow copy!

            ParameterInfo[] pi2 = mi.GetParameters();

            for (int i = 0; i < pi2.Length; i++)
            {
                Assert.NotNull(pi2[i]);
            }
        }

        public static MethodInfo GetMethod(string method)
        {
            return GetMethod(typeof(MethodInfoParametersTests), method);
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

        public void PrintStringArray(string[] strArray)
        {
            for (int ii = 0; ii < strArray.Length; ++ii)
            {
            }
        }
    }

    // Class For Reflection Metadata
    public class Interlocked2
    {
        public Interlocked2()
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
