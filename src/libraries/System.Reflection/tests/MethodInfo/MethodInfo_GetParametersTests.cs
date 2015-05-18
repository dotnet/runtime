// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [Fact]
        public static void FactParams1()
        {
            string methodName = "DummyMethod1";
            String[] strParamNames = { "str", "iValue", "lValue" };

            VerifyGetParameters(methodName, strParamNames);
        }

        //Verify Method Parameters
        [Fact]
        public static void FactParams2()
        {
            string methodName = "PrintStringArray";
            String[] strParamNames = { "strArray" };

            VerifyGetParameters(methodName, strParamNames);
        }

        //Verify Method Parameters for ref parameters
        [Fact]
        public static void FactParams3()
        {
            Type type = typeof(Interlocked2);
            //case 1
            VerifyGetParameters(type, "Increment", new String[] { "location" });
            //case 2
            VerifyGetParameters(type, "Decrement", new String[] { "location" });
            //case 3
            VerifyGetParameters(type, "Exchange", new String[] { "location1", "value" });
            //case 4
            VerifyGetParameters(type, "CompareExchange", new String[] { "location1", "value", "comparand" });
        }

        //Test case for bug: 1720 (MethodInfo.GetParameters is doing shallow copy instead of deep copy)
        [Fact]
        public static void FactParams4()
        {
            string methodName = "DummyMethod1";
            String[] strParamNames = { "str", "iValue", "lValue" };

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

        //Helper Method to Verify GetParameters
        public static void VerifyGetParameters(string methodName, string[] methodParams)
        {
            VerifyGetParameters(typeof(MethodInfoParametersTests), methodName, methodParams);
        }

        //Helper Method to Verify GetParameters
        public static void VerifyGetParameters(Type type, string methodName, string[] methodParams)
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
                Assert.True(new String(allparams[i].Name.ToCharArray()).Trim().Equals(methodParams[i], StringComparison.CurrentCultureIgnoreCase));
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
        public void DummyMethod1(String str, int iValue, long lValue)
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

        public static Object Exchange(ref Object location1, Object value) { return null; }
        public static Object CompareExchange(ref Object location1, Object value, Object comparand) { return null; }
    }
}
