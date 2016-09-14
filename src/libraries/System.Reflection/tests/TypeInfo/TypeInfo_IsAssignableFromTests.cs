// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 0414
#pragma warning disable 0067

namespace System.Reflection.Tests
{
    public class TypeInfoIsAssignableFromTests
    {
        // Verify IsAssignableFrom for null
        [Fact]
        public static void TestIsAssignable1()
        {
            VerifyIsAssignableFrom("B&null", typeof(ClassWithIterfaces).GetTypeInfo(), null, false);
        }

        // Verify IsAssignableFrom for List and Arrays
        [Fact]
        public static void TestIsAssignable2()
        {
            VerifyIsAssignableFrom("ListArray", typeof(IList<object>).GetTypeInfo(), typeof(object[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("ArrayList", typeof(object[]).GetTypeInfo(), typeof(IList<object>).GetTypeInfo(), false);

            VerifyIsAssignableFrom("B&D", typeof(ClassWithIterfaces).GetTypeInfo(), typeof(D).GetTypeInfo(), true);
            VerifyIsAssignableFrom("B[]&D[]", typeof(ClassWithIterfaces[]).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<object>&B[]", typeof(IList<object>).GetTypeInfo(), typeof(ClassWithIterfaces[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<B>*B[]", typeof(IList<ClassWithIterfaces>).GetTypeInfo(), typeof(ClassWithIterfaces[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<B>&D[]", typeof(IList<ClassWithIterfaces>).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<D> & D[]", typeof(IList<D>).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
        }

        // Verify IsAssignableFrom for String and Objects
        [Fact]
        public static void TestIsAssignable3()
        {
            VerifyIsAssignableFrom("I<object>&G2<object>", typeof(I<object>).GetTypeInfo(), typeof(G2<object>).GetTypeInfo(), true);
            VerifyIsAssignableFrom("G<string>&G2<string>", typeof(G<string>).GetTypeInfo(), typeof(G2<string>).GetTypeInfo(), true);
            VerifyIsAssignableFrom("G<string>&G<string>", typeof(G<string>).GetTypeInfo(), typeof(G<string>).GetTypeInfo(), true);

            VerifyIsAssignableFrom("G<string>&G<object>", typeof(G<string>).GetTypeInfo(), typeof(G<object>).GetTypeInfo(), false);
            VerifyIsAssignableFrom("G<object>&G<stgring>", typeof(G<object>).GetTypeInfo(), typeof(G<string>).GetTypeInfo(), false);
            VerifyIsAssignableFrom("G2<object>&G<object>", typeof(G2<object>).GetTypeInfo(), typeof(G<object>).GetTypeInfo(), false);
            VerifyIsAssignableFrom("G<string>&I<String>", typeof(G<string>).GetTypeInfo(), typeof(I<string>).GetTypeInfo(), false);
        }

        // Verify IsAssignableFrom for Interfaces
        [Fact]
        public static void TestIsAssignable4()
        {
            VerifyIsAssignableFrom("I2 I2", typeof(I2).GetTypeInfo(), typeof(I2).GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 B", typeof(I2).GetTypeInfo(), typeof(ClassWithIterfaces).GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 D", typeof(I2).GetTypeInfo(), typeof(D).GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 Gen<>", typeof(I2).GetTypeInfo(), typeof(Gen<>).GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 Gen<string>", typeof(I2).GetTypeInfo(), typeof(Gen<string>).GetTypeInfo(), true);
            VerifyIsAssignableFrom("D I1", typeof(D).GetTypeInfo(), typeof(I1).GetTypeInfo(), false);
        }

        // Verify IsAssignableFrom for namespaces
        [Fact]
        public static void TestIsAssignable5()
        {
            VerifyIsAssignableFrom("Case500.A Case500.B", typeof(Case500.A).GetTypeInfo(), typeof(Case500.B).GetTypeInfo(), true);
            VerifyIsAssignableFrom("Case500.A Case500.C", typeof(Case500.A).GetTypeInfo(), typeof(Case500.C).GetTypeInfo(), true);
            VerifyIsAssignableFrom("Case500.B Case500.C", typeof(Case500.B).GetTypeInfo(), typeof(Case500.C).GetTypeInfo(), true);
        }


        // Verify IsAssignableFrom 
        // a T[] is assignable to IList<U> iff T[] is assignable to U[]
        [Fact]
        public static void TestIsAssignable6()
        {
            VerifyIsAssignableFrom("I1[] S[]", typeof(I1[]).GetTypeInfo(), typeof(S[]).GetTypeInfo(), false);
            VerifyIsAssignableFrom("I1[] D[]", typeof(I1[]).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<I1> S[]", typeof(IList<I1>).GetTypeInfo(), typeof(S[]).GetTypeInfo(), false);
            VerifyIsAssignableFrom("IList<I1> D[]", typeof(IList<I1>).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);

            VerifyIsAssignableFrom("int[] uint[]", typeof(int[]).GetTypeInfo(), typeof(uint[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("uint[] int[]", typeof(uint[]).GetTypeInfo(), typeof(int[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<int> uint[]", typeof(IList<int>).GetTypeInfo(), typeof(uint[]).GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<uint> int[]", typeof(IList<uint>).GetTypeInfo(), typeof(int[]).GetTypeInfo(), true);
        }


        //private helper methods
        private static void VerifyIsAssignableFrom(String TestName, TypeInfo left, TypeInfo right, Boolean expected)
        {
            //Fix to initialize Reflection
            String str = typeof(Object).Name;

            Boolean actual = left.IsAssignableFrom(right);
            Assert.Equal(expected, actual);
        }
    } //end class

    //Metadata for Reflection
    public interface I1 { }
    public interface I2 { }

    public struct S : I1 { }

    public class ClassWithIterfaces : I1, I2 { }
    public class D : ClassWithIterfaces { }
    public class Gen<T> : D { }

    public class I<T> { }
    public class G<T> : I<T> { }
    public class G2<T> : G<T> { }

    public class Gen2<T> where T : Gen<T>, I1, I2 { }

    namespace Case500
    {
        public abstract class A { }
        public abstract class B : A { }
        public class C : B { }
    }

    public class G10<T> where T : I1 { }
}
