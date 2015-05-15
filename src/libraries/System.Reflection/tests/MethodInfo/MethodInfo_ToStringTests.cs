// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class MethodInfoToStringTests
    {
        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature1()
        {
            VerifyMethodSignature("DummyMethod1", "Void DummyMethod1(System.String, Int32, Int64)");
        }


        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature2()
        {
            VerifyMethodSignature("PrintStringArray", "Void PrintStringArray(System.String[])");
        }

        //Verify Method Signatures for ref parameters
        [Fact]
        public static void TestMethodSignature3()
        {
            Type type = typeof(MethodInfoInterlocked3);

            //case 1
            VerifyMethodSignature(type, "Increment", "Int32 Increment(Int32 ByRef)");

            //case 2
            VerifyMethodSignature(type, "Decrement", "Int32 Decrement(Int32 ByRef)");

            //case 3
            VerifyMethodSignature(type, "Exchange", "Int32 Exchange(Int32 ByRef, Int32)");

            //case 4
            VerifyMethodSignature(type, "CompareExchange", "Int32 CompareExchange(Int32 ByRef, Int32, Int32)");
        }

        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature4()
        {
            VerifyMethodSignature("DummyMethod2", "Void DummyMethod2()");
        }

        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature5()
        {
            VerifyMethodSignature(typeof(MethodInfoToStringSample), "Method1", "System.String Method1(System.DateTime)");
        }


        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature6()
        {
            VerifyMethodSignature(typeof(MethodInfoToStringSample), "Method2", "System.String Method2[T,S](System.String, T, S)");
        }

        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature7()
        {
            VerifyMethodSignature(typeof(MethodInfoToStringSampleG<>), "Method1", "T Method1(T)");
        }


        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature8()
        {
            VerifyMethodSignature(typeof(MethodInfoToStringSampleG<>), "Method2", "T Method2[S](S, T, System.String)");
        }

        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature9()
        {
            VerifyMethodSignature(typeof(MethodInfoToStringSampleG<string>), "Method1", "System.String Method1(System.String)");
        }

        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature10()
        {
            VerifyMethodSignature(typeof(MethodInfoToStringSampleG<string>), "Method2", "System.String Method2[S](S, System.String, System.String)");
        }


        //Verify Method Signatures
        [Fact]
        public static void TestMethodSignature11()
        {
            //Make generic method
            MethodInfo gmi = getMethod(typeof(MethodInfoToStringSampleG<string>), "Method2").MakeGenericMethod(new Type[] { typeof(DateTime) });
            String sign = "System.String Method2[DateTime](System.DateTime, System.String, System.String)";

            Assert.True(gmi.ToString().Equals(sign));
        }



        //Helper Method to Verify Method Signature
        public static void VerifyMethodSignature(string methodName, string methodsign)
        {
            VerifyMethodSignature(typeof(MethodInfoToStringTests), methodName, methodsign);
        }


        //Helper Method to Verify Signatures
        public static void VerifyMethodSignature(Type type, string methodName, string methodsign)
        {
            MethodInfo mi = getMethod(type, methodName);

            Assert.NotNull(mi);

            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase));

            Assert.True(mi.ToString().Equals(methodsign, StringComparison.CurrentCultureIgnoreCase));
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

        public static Object Exchange(ref Object location1, Object value) { return null; }
        public static Object CompareExchange(ref Object location1, Object value, Object comparand) { return null; }
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
