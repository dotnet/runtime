// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class IsAssignableFromTest
    {
        [Fact]
        public void Test1()
        {
            VerifyIsAssignableFrom("B&null", typeof(B).GetTypeInfo(), null, false);
        }
        [Fact]
        public void Test2()
        {
            VerifyIsAssignableFrom("ListArray", typeof(IList<object>).GetTypeInfo(), typeof(object[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test3()
        {
            VerifyIsAssignableFrom("ArrayList", typeof(object[]).GetTypeInfo(), typeof(IList<object>).GetTypeInfo(), false);
        }
        [Fact]
        public void Test4()
        {
            VerifyIsAssignableFrom("B&D", typeof(B).GetTypeInfo(), typeof(D).GetTypeInfo(), true);
        }

        [Fact]
        public void Test5()
        {
            VerifyIsAssignableFrom("B[]&D[]", typeof(B[]).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test6()
        {
            VerifyIsAssignableFrom("IList<object>&B[]", typeof(IList<object>).GetTypeInfo(), typeof(B[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test7()
        {
            VerifyIsAssignableFrom("IList<B>*B[]", typeof(IList<B>).GetTypeInfo(), typeof(B[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test8()
        {
            VerifyIsAssignableFrom("IList<B>&D[]", typeof(IList<B>).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test9()
        {
            VerifyIsAssignableFrom("IList<D> & D[]", typeof(IList<D>).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test10()
        {
            VerifyIsAssignableFrom("I<object>&G2<object>", typeof(I<object>).GetTypeInfo(), typeof(G2<object>).GetTypeInfo(), true);
        }

        [Fact]
        public void Test11()
        {
            VerifyIsAssignableFrom("G<string>&G2<string>", typeof(G<string>).GetTypeInfo(), typeof(G2<string>).GetTypeInfo(), true);
        }

        [Fact]
        public void Test12()
        {
            VerifyIsAssignableFrom("G<string>&G<string>", typeof(G<string>).GetTypeInfo(), typeof(G<string>).GetTypeInfo(), true);
        }

        [Fact]
        public void Test13()
        {
            VerifyIsAssignableFrom("G<string>&G<object>", typeof(G<string>).GetTypeInfo(), typeof(G<object>).GetTypeInfo(), false);
        }

        [Fact]
        public void Test14()
        {
            VerifyIsAssignableFrom("G<object>&G<stgring>", typeof(G<object>).GetTypeInfo(), typeof(G<string>).GetTypeInfo(), false);
        }

        [Fact]
        public void Test15()
        {
            VerifyIsAssignableFrom("G2<object>&G<object>", typeof(G2<object>).GetTypeInfo(), typeof(G<object>).GetTypeInfo(), false);
        }

        [Fact]
        public void Test16()
        {
            VerifyIsAssignableFrom("G<string>&I<String>", typeof(G<string>).GetTypeInfo(), typeof(I<string>).GetTypeInfo(), false);
        }

        [Fact]
        public void Test17()
        {
            VerifyIsAssignableFrom("I2 I2", typeof(I2).GetTypeInfo(), typeof(I2).GetTypeInfo(), true);
        }

        [Fact]
        public void Test18()
        {
            VerifyIsAssignableFrom("I2 B", typeof(I2).GetTypeInfo(), typeof(B).GetTypeInfo(), true);
        }

        [Fact]
        public void Test19()
        {
            VerifyIsAssignableFrom("I2 D", typeof(I2).GetTypeInfo(), typeof(D).GetTypeInfo(), true);
        }

        [Fact]
        public void Test20()
        {
            VerifyIsAssignableFrom("I2 Gen<>", typeof(I2).GetTypeInfo(), typeof(Gen<>).GetTypeInfo(), true);
        }

        [Fact]
        public void Test21()
        {
            VerifyIsAssignableFrom("I2 Gen<string>", typeof(I2).GetTypeInfo(), typeof(Gen<string>).GetTypeInfo(), true);
        }

        [Fact]
        public void Test22()
        {
            VerifyIsAssignableFrom("D I1", typeof(D).GetTypeInfo(), typeof(I1).GetTypeInfo(), false);
        }

        [Fact]
        public void Test23()
        {
            TypeInfo gt = typeof(Gen2<>).GetGenericArguments()[0].GetTypeInfo();
            VerifyIsAssignableFrom("I1 Gen2<>.GenericTypeArguments", typeof(I1).GetTypeInfo(), gt, true);
        }

        [Fact]
        public void Test24()
        {
            TypeInfo gt = typeof(Gen2<>).GetGenericArguments()[0].GetTypeInfo();
        }

        [Fact]
        public void Test25()
        {
            TypeInfo gt = typeof(Gen2<>).GetGenericArguments()[0].GetTypeInfo();
            VerifyIsAssignableFrom("I2 Gen2<>.GenericTypeArguments", typeof(I2).GetTypeInfo(), gt, true);
        }

        [Fact]
        public void Test26()
        {
            TypeInfo gt = typeof(Gen2<>).GetGenericArguments()[0].GetTypeInfo();
            VerifyIsAssignableFrom("Gen<> Gen2<>.GenericTypeArguments", typeof(Gen<>).GetTypeInfo(), gt, false);
        }

        [Fact]
        public void Test27()
        {
            VerifyIsAssignableFrom("Case500.A Case500.B", typeof(Case500.A).GetTypeInfo(), typeof(Case500.B).GetTypeInfo(), true);
        }

        [Fact]
        public void Test28()
        {
            VerifyIsAssignableFrom("Case500.A Case500.C", typeof(Case500.A).GetTypeInfo(), typeof(Case500.C).GetTypeInfo(), true);
        }

        [Fact]
        public void Test29()
        {
            VerifyIsAssignableFrom("Case500.B Case500.C", typeof(Case500.B).GetTypeInfo(), typeof(Case500.C).GetTypeInfo(), true);
        }

        [Fact]
        public void Test30()
        {
            VerifyIsAssignableFrom("G10<>.GetGenericTypeArguments I1", typeof(G10<>).GetGenericArguments()[0].GetTypeInfo(), typeof(I1).GetTypeInfo(), false);
        }

        [Fact]
        public void Test31()
        {
            VerifyIsAssignableFrom("G10<>.GetGenericTypeArguments B", typeof(G10<>).GetGenericArguments()[0].GetTypeInfo(), typeof(B).GetTypeInfo(), false);
        }

        [Fact]
        public void Test32()
        {
            VerifyIsAssignableFrom("I1 G10<>.GetGenericTypeArguments", typeof(I1).GetTypeInfo(), typeof(G10<>).GetGenericArguments()[0].GetTypeInfo(), true);
        }

        [Fact]
        public void Test33()
        {
            VerifyIsAssignableFrom("B G10<>.GetGenericTypeArguments", typeof(B).GetTypeInfo(), typeof(G10<>).GetGenericArguments()[0].GetTypeInfo(), false);
        }

        [Fact]
        public void Test34()
        {
            VerifyIsAssignableFrom("I1 Gen2<>.GetGenericArguments", typeof(I1).GetTypeInfo(), typeof(Gen2<>).GetGenericArguments()[0].GetTypeInfo(), true);
        }

        [Fact]
        public void Test35()
        {
            VerifyIsAssignableFrom("I2 Gen2<>.GetGenericArguments", typeof(I2).GetTypeInfo(), typeof(Gen2<>).GetGenericArguments()[0].GetTypeInfo(), true);
        }

        // a T[] is assignable to IList<U> iff T[] is assignable to U[]
        [Fact]
        public void Test36()
        {
            VerifyIsAssignableFrom("I1[] S[]", typeof(I1[]).GetTypeInfo(), typeof(S[]).GetTypeInfo(), false);
        }

        [Fact]
        public void Test37()
        {
            VerifyIsAssignableFrom("I1[] D[]", typeof(I1[]).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test38()
        {
            VerifyIsAssignableFrom("IList<I1> S[]", typeof(IList<I1>).GetTypeInfo(), typeof(S[]).GetTypeInfo(), false);
        }

        [Fact]
        public void Test39()
        {
            VerifyIsAssignableFrom("IList<I1> D[]", typeof(IList<I1>).GetTypeInfo(), typeof(D[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test40()
        {
            VerifyIsAssignableFrom("int[] uint[]", typeof(int[]).GetTypeInfo(), typeof(uint[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test41()
        {
            VerifyIsAssignableFrom("uint[] int[]", typeof(uint[]).GetTypeInfo(), typeof(int[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test42()
        {
            VerifyIsAssignableFrom("IList<int> uint[]", typeof(IList<int>).GetTypeInfo(), typeof(uint[]).GetTypeInfo(), true);
        }

        [Fact]
        public void Test43()
        {
            VerifyIsAssignableFrom("IList<uint> int[]", typeof(IList<uint>).GetTypeInfo(), typeof(int[]).GetTypeInfo(), true);
        }

        private void VerifyIsAssignableFrom(String testName, TypeInfo left, TypeInfo right, Boolean expected)
        {
            Boolean actual = left.IsAssignableFrom(right);
            Assert.Equal(expected, actual);
        }
    }

    internal interface I1 { }
    internal interface I2 { }

    internal struct S : I1 { }

    internal class B : I1, I2 { }
    internal class D : B { }
    internal class Gen<T> : D { }

    internal class I<T> { }
    internal class G<T> : I<T> { }
    internal class G2<T> : G<T> { }

    internal class Gen2<T> where T : Gen<T>, I1, I2 { }

    namespace Case500
    {
        internal abstract class A { }
        internal abstract class B : A { }
        internal class C : B { }
    }

    internal class G10<T> where T : I1 { }

    public class TransparentRC : ReflectionContext
    {
        public override Assembly MapAssembly(Assembly assembly)
        {
            return assembly;
        }

        public override TypeInfo MapType(TypeInfo type)
        {
            return type;
        }
    }
}