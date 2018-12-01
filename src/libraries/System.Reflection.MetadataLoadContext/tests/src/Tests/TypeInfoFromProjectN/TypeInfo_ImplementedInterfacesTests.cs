// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredImplementedInterfacesTests
    {
        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces1()
        {
            VerifyInterfaces(typeof(I21).Project(), new Type[] { typeof(ImI1).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces2()
        {
            VerifyInterfaces(typeof(S1).Project(), new Type[] { typeof(ImI1).Project(), typeof(I21).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces3()
        {
            VerifyInterfaces(typeof(C1).Project(), new Type[] { typeof(I0).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces4()
        {
            VerifyInterfaces(typeof(D1).Project(), new Type[] { typeof(ImI1).Project(), typeof(I0).Project(), typeof(I21).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces5()
        {
            VerifyInterfaces(typeof(D2<>).Project(), new Type[] { typeof(ImI1).Project(), typeof(I0).Project(), typeof(I21).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces6()
        {
            VerifyInterfaces(typeof(D2<int>).Project(), new Type[] { typeof(ImI1).Project(), typeof(I0).Project(), typeof(I21).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces7()
        {
            VerifyInterfaces(typeof(D3<I21>).Project(), new Type[] { typeof(I3<I21>).Project(), typeof(I0).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces8()
        {
            VerifyInterfaces(typeof(D4<>).Project(), new Type[] { typeof(I3<string>).Project(), typeof(I0).Project() });
        }

        // Verify implemented interfaces 
        [Fact]
        public static void TestInterFaces9()
        {
            VerifyInterfaces(typeof(D4<string>).Project(), new Type[] { typeof(I3<string>).Project(), typeof(I0).Project() });
        }




        //private helper methods
        private static void VerifyInterfaces(Type type, params Type[] expected)
        {
            //Fix to initialize Reflection
            string str = typeof(Object).Project().Name;

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
