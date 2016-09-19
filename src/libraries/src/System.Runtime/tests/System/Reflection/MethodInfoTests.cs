// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public class MethodInfoTests
    {
        [Theory]
        //Verify two same MethodInfo objects are equal 
        [InlineData("DummyMethod1", "DummyMethod1", true)]
        //Verify two different MethodInfo objects are not equal 
        [InlineData("DummyMethod1", "DummyMethod2", false)]

        public void TestEquality1(string str1, string str2, bool expected)
        {
            MethodInfo mi1 = GetMethod(str1);
            MethodInfo mi2 = GetMethod(str2);

            Assert.Equal(expected, mi1 == mi2);
            Assert.NotEqual(expected, mi1 != mi2);
        }

        public static IEnumerable<object[]> TestEqualityMethodData2()
        {
            //Verify two different MethodInfo objects with same name from two different classes are not equal 
            yield return new object[] { typeof(Sample), typeof(SampleG<>), "Method1", "Method1", false};
            //Verify two different MethodInfo objects with same name from two different classes are not equal 
            yield return new object[] { typeof(Sample), typeof(SampleG<string>), "Method2", "Method2", false };
        }

        [Theory]
        [MemberData(nameof(TestEqualityMethodData2))]
        public void TestEquality2(Type sample1, Type sample2, string str1, string str2, bool expected)
        {
            MethodInfo mi1 = GetMethod(sample1, str1);
            MethodInfo mi2 = GetMethod(sample2, str2);

            Assert.Equal(expected, mi1 == mi2);
            Assert.NotEqual(expected, mi1 != mi2);
        }

        // Gets MethodInfo object from current class
        public static MethodInfo GetMethod(string method)
        {
            return GetMethod(typeof(MethodInfoTests), method);
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
        public void DummyMethod1(string str, int iValue, long lValue)
        {
        }

        public void DummyMethod2()
        {
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