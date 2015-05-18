// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredImplementedInterfacesTests
    {
        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces1()
        {
            VerifyInterfaces(typeof(I21), new Type[] { typeof(ImI1) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces2()
        {
            VerifyInterfaces(typeof(S1), new Type[] { typeof(ImI1), typeof(I21) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces3()
        {
            VerifyInterfaces(typeof(C1), new Type[] { typeof(I0) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces4()
        {
            VerifyInterfaces(typeof(D1), new Type[] { typeof(ImI1), typeof(I0), typeof(I21) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces5()
        {
            VerifyInterfaces(typeof(D2<>), new Type[] { typeof(ImI1), typeof(I0), typeof(I21) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces6()
        {
            VerifyInterfaces(typeof(D2<int>), new Type[] { typeof(ImI1), typeof(I0), typeof(I21) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces7()
        {
            VerifyInterfaces(typeof(D3<I21>), new Type[] { typeof(I3<I21>), typeof(I0) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces8()
        {
            VerifyInterfaces(typeof(D4<>), new Type[] { typeof(I3<string>), typeof(I0) });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces9()
        {
            VerifyInterfaces(typeof(D4<string>), new Type[] { typeof(I3<string>), typeof(I0) });
        }




        //private helper methods
        private static void VerifyInterfaces(Type type, params Type[] expected)
        {
            //Fix to initialize Reflection
            String str = typeof(Object).Name;

            TypeInfo typeInfo = type.GetTypeInfo();
            List<Type> list = new List<Type>();

            IEnumerator<Type> allinterfaces = typeInfo.ImplementedInterfaces.GetEnumerator();
            while (allinterfaces.MoveNext())
            {
                list.Add(allinterfaces.Current);
            }
            Type[] actual = list.ToArray();

            Array.Sort(actual, delegate (Type a, Type b) { return a.GetHashCode() - b.GetHashCode(); });
            Array.Sort(expected, delegate (Type a, Type b) { return a.GetHashCode() - b.GetHashCode(); });

            for (int i = 0; i < actual.Length; i++)
            {
                Assert.Equal(actual[i], expected[i]);

                Assert.True(expected[i].GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
            }
        }
    } //end class

    //Metadata for Reflection
    public interface ImI1 { }
    public interface I0 { }
    public interface I21 : ImI1 { }
    public interface I3<T> { }

    public struct S1 : I21 { }

    public class C1 : I0 { }
    public class D1 : C1, I21 { }
    public class D2<T> : C1, I21 { }
    public class D3<T> : C1, I3<T> { }
    public class D4<T> : C1, I3<string> { }

    public class E1<T> where T : ImI1 { }
    public class E2<T> where T : C1, I21 { }
    public class E3<T> where T : C1, I3<T> { }
    public class E4<T> where T : C1, I3<int> { }

    public enum MyEnum1 { A, B }
}
