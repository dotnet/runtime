// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414
#pragma warning disable 0067

namespace System.Reflection.Tests
{
    public static class TypeInfoIsAssignableFromTests
    {
        // Verify IsAssignableFrom for null
        [Fact]
        public static void TestIsAssignable1()
        {
            VerifyIsAssignableFrom("B&null", typeof(B).Project().GetTypeInfo(), null, false);
        }

        // Verify IsAssignableFrom for List and Arrays
        [Fact]
        public static void TestIsAssignable2()
        {
            VerifyIsAssignableFrom("ListArray", typeof(IList<object>).Project().GetTypeInfo(), typeof(object[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("ArrayList", typeof(object[]).Project().GetTypeInfo(), typeof(IList<object>).Project().GetTypeInfo(), false);

            VerifyIsAssignableFrom("B&D", typeof(B).Project().GetTypeInfo(), typeof(D).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("B[]&D[]", typeof(B[]).Project().GetTypeInfo(), typeof(D[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<object>&B[]", typeof(IList<object>).Project().GetTypeInfo(), typeof(B[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<B>*B[]", typeof(IList<B>).Project().GetTypeInfo(), typeof(B[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<B>&D[]", typeof(IList<B>).Project().GetTypeInfo(), typeof(D[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<D> & D[]", typeof(IList<D>).Project().GetTypeInfo(), typeof(D[]).Project().GetTypeInfo(), true);
        }

        // Verify IsAssignableFrom for String and Objects
        [Fact]
        public static void TestIsAssignable3()
        {
            VerifyIsAssignableFrom("I<object>&G2<object>", typeof(I<object>).Project().GetTypeInfo(), typeof(G2<object>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("G<string>&G2<string>", typeof(G<string>).Project().GetTypeInfo(), typeof(G2<string>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("G<string>&G<string>", typeof(G<string>).Project().GetTypeInfo(), typeof(G<string>).Project().GetTypeInfo(), true);

            VerifyIsAssignableFrom("G<string>&G<object>", typeof(G<string>).Project().GetTypeInfo(), typeof(G<object>).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("G<object>&G<stgring>", typeof(G<object>).Project().GetTypeInfo(), typeof(G<string>).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("G2<object>&G<object>", typeof(G2<object>).Project().GetTypeInfo(), typeof(G<object>).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("G<string>&I<String>", typeof(G<string>).Project().GetTypeInfo(), typeof(I<string>).Project().GetTypeInfo(), false);
        }

        // Verify IsAssignableFrom for Interfaces
        [Fact]
        public static void TestIsAssignable4()
        {
            VerifyIsAssignableFrom("I2 I2", typeof(I2).Project().GetTypeInfo(), typeof(I2).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 B", typeof(I2).Project().GetTypeInfo(), typeof(B).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 D", typeof(I2).Project().GetTypeInfo(), typeof(D).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 Gen<>", typeof(I2).Project().GetTypeInfo(), typeof(Gen<>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("I2 Gen<string>", typeof(I2).Project().GetTypeInfo(), typeof(Gen<string>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("D I1", typeof(D).Project().GetTypeInfo(), typeof(I1).Project().GetTypeInfo(), false);
        }

        // Verify IsAssignableFrom for namespaces
        [Fact]
        public static void TestIsAssignable5()
        {
            VerifyIsAssignableFrom("Case500.A Case500.B", typeof(Case500.A).Project().GetTypeInfo(), typeof(Case500.B).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("Case500.A Case500.C", typeof(Case500.A).Project().GetTypeInfo(), typeof(Case500.C).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("Case500.B Case500.C", typeof(Case500.B).Project().GetTypeInfo(), typeof(Case500.C).Project().GetTypeInfo(), true);
        }


        // Verify IsAssignableFrom 
        // a T[] is assignable to IList<U> iff T[] is assignable to U[]
        [Fact]
        public static void TestIsAssignable6()
        {
            VerifyIsAssignableFrom("I1[] S[]", typeof(I1[]).Project().GetTypeInfo(), typeof(S[]).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("I1[] D[]", typeof(I1[]).Project().GetTypeInfo(), typeof(D[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<I1> S[]", typeof(IList<I1>).Project().GetTypeInfo(), typeof(S[]).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("IList<I1> D[]", typeof(IList<I1>).Project().GetTypeInfo(), typeof(D[]).Project().GetTypeInfo(), true);

            VerifyIsAssignableFrom("int[] uint[]", typeof(int[]).Project().GetTypeInfo(), typeof(uint[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("uint[] int[]", typeof(uint[]).Project().GetTypeInfo(), typeof(int[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<int> uint[]", typeof(IList<int>).Project().GetTypeInfo(), typeof(uint[]).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IList<uint> int[]", typeof(IList<uint>).Project().GetTypeInfo(), typeof(int[]).Project().GetTypeInfo(), true);
        }

        // Assignability from generic type parameters.
        [Fact]
        public static void TestIsAssignable7()
        {
            TypeInfo theT = typeof(Gen2<>).Project().Gp(0);
            VerifyIsAssignableFrom("I1 T", typeof(I1).Project().GetTypeInfo(), theT, true);
            VerifyIsAssignableFrom("I2 T", typeof(I2).Project().GetTypeInfo(), theT, true);
            VerifyIsAssignableFrom("Object T", typeof(object).Project().GetTypeInfo(), theT, true);
            VerifyIsAssignableFrom("Gen<T> T", typeof(Gen<>).Project().MakeGenericType(theT.AsType()).GetTypeInfo(), theT, true);
            VerifyIsAssignableFrom("String T", typeof(string).Project().GetTypeInfo(), theT, false);

            TypeInfo theTWithStructConstraint = typeof(Gen4<>).Project().Gp(0);
            VerifyIsAssignableFrom("Object T", typeof(object).Project().GetTypeInfo(), theTWithStructConstraint, true);
            VerifyIsAssignableFrom("ValueType T", typeof(ValueType).Project().GetTypeInfo(), theTWithStructConstraint, true);

            TypeInfo theTThatDerivesFromU = typeof(Gen5<,>).Project().Gp(0);
            TypeInfo theU = typeof(Gen5<,>).Project().Gp(1);
            VerifyIsAssignableFrom("U T", theU, theTThatDerivesFromU, true);

        }

        // Variant assignability for arrays and byrefs.
        [Fact]
        public static void TestIsAssignable8()
        {
            GC.KeepAlive(typeof(int[,]).Project());
            GC.KeepAlive(typeof(long[,]).Project());
            GC.KeepAlive(typeof(IntPtr[,]).Project());
            GC.KeepAlive(typeof(UIntPtr[,]).Project());
            GC.KeepAlive(typeof(short[,]).Project());
            GC.KeepAlive(typeof(ushort[,]).Project());
            GC.KeepAlive(typeof(byte[,]).Project());
            GC.KeepAlive(typeof(uint[,]).Project());
            GC.KeepAlive(typeof(Eint[,]).Project());
            GC.KeepAlive(typeof(Euint[,]).Project());
            GC.KeepAlive(typeof(bool[,]).Project());
            GC.KeepAlive(typeof(char[,]).Project());
            GC.KeepAlive(typeof(object[,]).Project());
            GC.KeepAlive(typeof(string[,]).Project());
            GC.KeepAlive(typeof(I1[,]).Project());

            VerifyElementedTypeIsAssignableFrom("int[] int[]", typeof(int).Project(), typeof(int).Project(), true);
            VerifyElementedTypeIsAssignableFrom("object[] string[]", typeof(object).Project(), typeof(string).Project(), true);
            VerifyElementedTypeIsAssignableFrom("string[] object[]", typeof(string).Project(), typeof(object).Project(), false);
            VerifyElementedTypeIsAssignableFrom("int[] Eint[]", typeof(int).Project(), typeof(Eint).Project(), true);
            VerifyElementedTypeIsAssignableFrom("int[] Euint[]", typeof(int).Project(), typeof(Euint).Project(), true);
            VerifyElementedTypeIsAssignableFrom("Eint[] Euint[]", typeof(Eint).Project(), typeof(Euint).Project(), true);

            VerifyElementedTypeIsAssignableFrom("I1[] T[]", typeof(I1).Project(), typeof(Gen3<>).Project().Gp(0).AsType(), true);


            VerifyElementedTypeIsAssignableFrom("int[] short[]", typeof(int).Project(), typeof(short).Project(), false);
            VerifyElementedTypeIsAssignableFrom("byte[] bool[]", typeof(byte).Project(), typeof(bool).Project(), false);
            VerifyElementedTypeIsAssignableFrom("ushort[] char[]", typeof(ushort).Project(), typeof(char).Project(), false);
        }

        // SzArrays vs. rank 1 mdarrays: T[*] can be assigned from T[], but not vice-versa.
        [Fact]
        public static void TestIsAssignable9()
        {
            // Why a generic parameter as the element type? .NETNative runtime doesn't support rank 1 mdarrays, but the reflection layer does as long as 
            // the element type is an open type.
            Type theT = typeof(Gen3<>).Project().Gp(0).AsType();
            TypeInfo szArrayT = theT.MakeArrayType().GetTypeInfo();
            TypeInfo mdArrayT = theT.MakeArrayType(1).GetTypeInfo();

            VerifyIsAssignableFrom("T[*] T[]", mdArrayT, szArrayT, true);
            VerifyIsAssignableFrom("T[] T[*]", szArrayT, mdArrayT, false);
        }

        // Nullable<T> can be assigned from T (Desktop compat: unless T is a generic parameter.)
        [Fact]
        public static void TestIsAssignable10()
        {
            TypeInfo theT = typeof(Gen4<>).Project().Gp(0);

            VerifyIsAssignableFrom("int? int", typeof(int?).Project().GetTypeInfo(), typeof(int).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("T? T", typeof(Nullable<>).Project().MakeGenericType(theT.AsType()).GetTypeInfo(), theT, false);

            VerifyIsAssignableFrom("int int?", typeof(int).Project().GetTypeInfo(), typeof(int?).GetTypeInfo(), false);
            VerifyIsAssignableFrom("T T?", theT, typeof(Nullable<>).Project().MakeGenericType(theT.AsType()).GetTypeInfo(), false);
        }

        [Fact]
        public static void TestIsAssignable11()
        {
            // Asking whether a generic type definition is assignable to another generic type definition is an odd question
            // since generic type definitions cannot be derived or implemented (only their instantiations can.)
            //
            // Nevertheless, this returns "true" under the "X is always assignable to X" rule. 
            // 
            VerifyIsAssignableFrom("GBase<> GBase<>", typeof(GBase<>).Project().GetTypeInfo(), typeof(GBase<>).Project().GetTypeInfo(), true);

            //
            // The fact that this returns "false" often surprises people. But it is the correct result under both .NET Native and the desktop. But they get there
            // through different reasoning.
            //
            // The .NET Native implements returns false because one cannot derive from a generic type definition (only an instantiation of 
            // a generic type definition.)
            //
            // The desktop, however, converts generic type definitions to instantiations closed over the generic type definition's formal type parameter
            // (that is, the "T" in GBase<T>) However, "false" is still the appropriate answer since GDerived<T> derives from GBase closed over
            // GDerived's T, not GBase's T.
            //
            VerifyIsAssignableFrom("GBase<> GDerived<>", typeof(GBase<>).Project().GetTypeInfo(), typeof(GDerived<>).Project().GetTypeInfo(), false);

            //
            // This test now corrects the "flaw" in the prior test. We close GBase over GDerived's "T" and ask if GDerived<> derives from that.
            //
            VerifyIsAssignableFrom("GBase<T> GDerived<>", typeof(GBase<>).Project().MakeGenericType(typeof(GDerived<>).Project().Gp(0).AsType()).GetTypeInfo(), typeof(GDerived<>).Project().GetTypeInfo(), true);
        }

        // Contravariance
        [Fact]
        public static void TestIsAssignable12()
        {
            VerifyIsAssignableFrom("IContravariant<string> IContravariance<object>", typeof(IContraVariant<string>).Project().GetTypeInfo(), typeof(IContraVariant<object>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IContravariant<int[]> IContravariance<uint[]>", typeof(IContraVariant<int[]>).Project().GetTypeInfo(), typeof(IContraVariant<uint[]>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IContravariant<int> IContravariance<uint>", typeof(IContraVariant<int>).Project().GetTypeInfo(), typeof(IContraVariant<uint>).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("IContravariant<int> IContravariance<int?>", typeof(IContraVariant<int>).Project().GetTypeInfo(), typeof(IContraVariant<int?>).Project().GetTypeInfo(), false);

            Type theT = typeof(G5<,>).Project().Gp(0).AsType();
            Type theU = typeof(G5<,>).Project().Gp(1).AsType();

            // Variance checks do check castability between generic type parameters but only if it can be proven that the type parameter never binds to a valuetype.
            // (either the type parameter has a "class" constraint (not one "inherited" from an ancestor), or has a non-interface, non-valuetype constraint.)
            VerifyIsAssignableFrom("IContravariant<T> IContravariance<object>", typeof(IContraVariant<>).Project().MakeGenericType(theT).GetTypeInfo(), typeof(IContraVariant<object>).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("IContravariant<U> IContravariance<object>", typeof(IContraVariant<>).Project().MakeGenericType(theU).GetTypeInfo(), typeof(IContraVariant<object>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("IContravariant<T> IContravariance<I1>", typeof(IContraVariant<>).Project().MakeGenericType(theT).GetTypeInfo(), typeof(IContraVariant<I1>).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("IContravariant<U> IContravariance<I1>", typeof(IContraVariant<>).Project().MakeGenericType(theU).GetTypeInfo(), typeof(IContraVariant<I1>).Project().GetTypeInfo(), true);
        }

        [Fact]
        public static void TestIsAssignable13()
        {
            VerifyIsAssignableFrom("ICoVariant<object> ICoVariant<string>", typeof(ICoVariant<object>).Project().GetTypeInfo(), typeof(ICoVariant<string>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("ICoVariant<uint[]> ICoVariant<int[]>", typeof(ICoVariant<uint[]>).Project().GetTypeInfo(), typeof(ICoVariant<int[]>).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("ICoVariant<uint> ICoVariant<int>", typeof(ICoVariant<uint>).Project().GetTypeInfo(), typeof(ICoVariant<int>).Project().GetTypeInfo(), false);
            VerifyIsAssignableFrom("ICoVariant<int?> ICoVariant<int>", typeof(ICoVariant<int?>).Project().GetTypeInfo(), typeof(ICoVariant<int>).Project().GetTypeInfo(), false);

            Type theT = typeof(G5<,>).Project().Gp(0).AsType();
            Type theU = typeof(G5<,>).Project().Gp(1).AsType();

            // Variance checks do check castability between generic type parameters but only if it can be proven that the type parameter never binds to a valuetype.
            // (either the type parameter has a "class" constraint (not one "inherited" from an ancestor), or has a non-interface, non-valuetype constraint.)
            VerifyIsAssignableFrom("ICoVariant<object> ICoVariant<T>", typeof(ICoVariant<object>).Project().GetTypeInfo(), typeof(ICoVariant<>).Project().MakeGenericType(theT).GetTypeInfo(), false);
            VerifyIsAssignableFrom("ICoVariant<object> ICoVariant<U>", typeof(ICoVariant<object>).Project().GetTypeInfo(), typeof(ICoVariant<>).Project().MakeGenericType(theU).GetTypeInfo(), true);
            VerifyIsAssignableFrom("ICoVariant<I1> ICoVariant<T>", typeof(ICoVariant<I1>).Project().GetTypeInfo(), typeof(ICoVariant<>).Project().MakeGenericType(theT).GetTypeInfo(), false);
            VerifyIsAssignableFrom("ICoVariant<I1> ICoVariant<U>", typeof(ICoVariant<I1>).Project().GetTypeInfo(), typeof(ICoVariant<>).Project().MakeGenericType(theU).GetTypeInfo(), true);
        }

        // Interfaces are castable to System.Object
        [Fact]
        public static void TestIsAssignable15()
        {
            Type theT = typeof(Gen4<>).Project().Gp(0).AsType();

            VerifyIsAssignableFrom("object I1", typeof(object).Project().GetTypeInfo(), typeof(I1).Project().GetTypeInfo(), true);
            VerifyIsAssignableFrom("object ICoVariant<T>", typeof(object).Project().GetTypeInfo(), typeof(ICoVariant<>).Project().MakeGenericType(theT).GetTypeInfo(), true);

        }
        //private helper methods
        private static void VerifyIsAssignableFrom(string TestName, TypeInfo left, TypeInfo right, Boolean expected)
        {
            {
                //Fix to initialize Reflection
                string str = typeof(Object).Project().Name;

                bool actual = left.IsAssignableFrom(right);
                Assert.Equal(expected, actual);
            }
        }

        private static void VerifyElementedTypeIsAssignableFrom(string TestName, Type leftElementType, Type rightElementType, bool expected)
        {
            VerifyIsAssignableFrom(TestName, leftElementType.MakeArrayType().GetTypeInfo(), rightElementType.MakeArrayType().GetTypeInfo(), expected);
            VerifyIsAssignableFrom(TestName, leftElementType.MakeArrayType(2).GetTypeInfo(), rightElementType.MakeArrayType(2).GetTypeInfo(), expected);
            VerifyIsAssignableFrom(TestName, leftElementType.MakeByRefType().GetTypeInfo(), rightElementType.MakeByRefType().GetTypeInfo(), expected);
        }

        private static TypeInfo Gp(this Type t, int index)
        {
            return t.GetTypeInfo().GenericTypeParameters[index].GetTypeInfo();
        }
    } //end class

    //Metadata for Reflection
    public interface I1 { }
    public interface I2 { }

    public struct S : I1 { }

    public class B : I1, I2 { }
    public class D : B { }
    public class Gen<T> : D { }

    public class I<T> { }
    public class G<T> : I<T> { }
    public class G2<T> : G<T> { }

    public class Gen2<T> where T : Gen<T>, I1, I2 { }

    public class Gen3<T> where T : D { }

    public class Gen4<T> where T : struct { }

    public class Gen5<T, U> where T : U { }

    public class GBase<T> { }

    public class GDerived<T> : GBase<T> { }

    namespace Case500
    {
        public abstract class A { }
        public abstract class B : A { }
        public class C : B { }
    }

    public class G10<T> where T : I1 { }

    public enum Eint : int { }
    public enum Euint : uint { }
    public enum Eshort : short { }
    public enum Eushort : ushort { }

    public interface IContraVariant<in T> { }
    public interface ICoVariant<out T> { }

    public class G5<T,U>
        where T : U
        where U : class, I1
    { }
}
