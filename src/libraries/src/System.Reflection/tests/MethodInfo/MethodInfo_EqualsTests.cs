// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class MethodInfoEqualsTests
    {
        //Verify two same MethodInfo objects are equal 
        [Fact]
        public void TestEqualsMethod1()
        {
            MethodInfo mi1 = GetMethod("DummyMethod1");
            MethodInfo mi2 = GetMethod("DummyMethod1");

            Assert.True(mi1.Equals(mi2));
        }

        //Verify two different MethodInfo objects are not equal 
        [Fact]
        public void TestEqualsMethod2()
        {
            MethodInfo mi1 = GetMethod("DummyMethod1");
            MethodInfo mi2 = GetMethod("DummyMethod2");

            Assert.False(mi1.Equals(mi2));
        }


        //Verify two different MethodInfo objects with same name from two different classes are not equal 
        [Fact]
        public void TestEqualsMethod3()
        {
            MethodInfo mi1 = GetMethod(typeof(Sample), "Method1");
            MethodInfo mi2 = GetMethod(typeof(SampleG<>), "Method1");

            Assert.False(mi1.Equals(mi2));
        }


        //Verify two different MethodInfo objects with same name from two different classes are not equal 
        [Fact]
        public void TestEqualsMethod4()
        {
            MethodInfo mi1 = GetMethod(typeof(Sample), "Method2");
            MethodInfo mi2 = GetMethod(typeof(SampleG<string>), "Method2");

            Assert.False(mi1.Equals(mi2));
        }


        // Gets MethodInfo object from current class
        public static MethodInfo GetMethod(string method)
        {
            return GetMethod(typeof(MethodInfoEqualsTests), method);
        }


        //Gets MethodInfo object from a Type
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

    public class Sample
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

    public class SampleG<T>
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
