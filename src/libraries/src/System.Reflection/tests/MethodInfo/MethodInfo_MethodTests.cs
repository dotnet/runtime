// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class MethodInfoMethodTests
    {
        //Verify MakeGenericMethod() 
        [Fact]
        public void TestMakeGenericMethod1()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "GenericMethod");
            MethodInfo miConstructed = mi.MakeGenericMethod(typeof(int));
            Assert.NotNull(miConstructed);
            Assert.True(miConstructed.IsGenericMethod);
        }

        //Verify MakeGenericMethod() for method with multiple params
        [Fact]
        public void TestMakeGenericMethod2()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "GenericMethod2");
            Type[] types = new Type[2];
            types[0] = typeof(string);
            types[1] = typeof(int);
            MethodInfo miConstructed = mi.MakeGenericMethod(types);
            Assert.NotNull(miConstructed);
            Assert.True(miConstructed.IsGenericMethod);
        }

        //Verify MakeGenericMethod() for method that is non generic
        [Fact]
        public void TestMakeGenericMethod3()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "NonGenericMethod");
            Type[] types = new Type[2];
            types[0] = typeof(string);
            types[1] = typeof(int);
            MethodInfo miConstructed = null;
            Assert.Throws<InvalidOperationException>(() => { miConstructed = mi.MakeGenericMethod(types); });
        }

        //Verify MakeGenericMethod() with null
        [Fact]
        public void TestMakeGenericMethod4()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "GenericMethod2");
            MethodInfo miConstructed = null;

            Assert.Throws<ArgumentNullException>(() => { miConstructed = mi.MakeGenericMethod(null); });
        }

        //Verify MakeGenericMethod() with one type as null
        [Fact]
        public void TestMakeGenericMethod5()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "GenericMethod2");
            Type[] types = new Type[2];
            types[0] = typeof(string);
            types[1] = null;
            MethodInfo miConstructed = null;
            Assert.Throws<ArgumentNullException>(() => { miConstructed = mi.MakeGenericMethod(types); });
        }

        //Verify MakeGenericMethod() when number of type params do not match
        [Fact]
        public void TestMakeGenericMethod6()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "GenericMethod2");
            Type[] types = new Type[1];
            types[0] = typeof(string);
            MethodInfo miConstructed = null;

            Assert.Throws<ArgumentException>(() => { miConstructed = mi.MakeGenericMethod(types); });
        }

        //Verify MakeGenericMethod() for method with multiple params
        [Fact]
        public void TestGetGenericMethodDefinition1()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "GenericMethod2");
            Type[] types = new Type[2];
            types[0] = typeof(string);
            types[1] = typeof(int);
            MethodInfo miConstructed = mi.MakeGenericMethod(types);

            MethodInfo midef = miConstructed.GetGenericMethodDefinition();
            Assert.NotNull(midef);
            Assert.Equal(midef, mi);
        }

        //Verify GetGenericMethodDefinition() for method that is non generic
        [Fact]
        public void TestGetGenericMethodDefinition2()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "NonGenericMethod");
            MethodInfo miDef = null;

            Assert.Throws<InvalidOperationException>(() => { miDef = mi.GetGenericMethodDefinition(); });
        }

        //Verify MakeGenericMethod() for method with multiple params
        [Fact]
        public void TestGetGenericArguments1()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "GenericMethod2");
            Type[] types = null;

            types = mi.GetGenericArguments();
            Assert.Equal(types.Length, 2);
        }

        //Verify MakeGenericMethod() for method with multiple params
        [Fact]
        public void TestGetGenericArguments2()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "NonGenericMethod");
            Type[] types = null;
            types = mi.GetGenericArguments();

            Assert.Equal(types.Length, 0);
        }

        //Verify GetHashCode Method
        [Fact]
        public void TestGetHashCode()
        {
            MethodInfo mi = GetMethod(typeof(SampleM), "NonGenericMethod");
            int hcode = mi.GetHashCode();

            Assert.NotEqual(hcode, 0);
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
    }

    // Define a class with a generic method. 
    public class SampleM
    {
        public void GenericMethod<T>(T toDisplay)
        {
        }

        public void GenericMethod2<T, U>(T t, U u)
        {
        }

        public void NonGenericMethod()
        {
        }
    }
}
