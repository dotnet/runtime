// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        [Fact]
        public static void TestMakeArray()
        {
            Type et = typeof(int).Project();
            Type t = et.MakeArrayType();

            Assert.True(t.IsSZArray());
            Assert.Equal(et, t.GetElementType());

            t.TestSzArrayInvariants();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public static void TestMakeMdArray(int rank)
        {
            Type et = typeof(int).Project();
            Type t = et.MakeArrayType(rank);

            Assert.True(t.IsVariableBoundArray());
            Assert.Equal(rank, t.GetArrayRank());
            Assert.Equal(et, t.GetElementType());

            t.TestMdArrayInvariants();
        }

        [Fact]
        public static void TestMakeByRef()
        {
            Type et = typeof(int).Project();
            Type t = et.MakeByRefType();

            Assert.True(t.IsByRef);
            Assert.Equal(et, t.GetElementType());

            t.TestByRefInvariants();
        }


        [Fact]
        public static void TestMakePointer()
        {
            Type et = typeof(int).Project();
            Type t = et.MakePointerType();

            Assert.True(t.IsPointer);
            Assert.Equal(et, t.GetElementType());

            t.TestPointerInvariants();
        }

        [Fact]
        public static void TestMakeGenericType()
        {
            Type gt = typeof(GenericClass3<,,>).Project();
            Type[] gas = { typeof(int).Project(), typeof(string).Project(), typeof(double).Project() };
            Type t = gt.MakeGenericType(gas);

            Assert.True(t.IsConstructedGenericType);
            Assert.Equal(gt, t.GetGenericTypeDefinition());
            Assert.Equal<Type>(gas, t.GenericTypeArguments);

            t.TestConstructedGenericTypeInvariants();
        }

        [Fact]
        public static void TestMakeGenericTypeParameter()
        {
            Type gt = typeof(GenericClass3<,,>).Project();
            Type[] gps = gt.GetTypeInfo().GenericTypeParameters;
            Assert.Equal(3, gps.Length);
            Assert.Equal("T", gps[0].Name);
            Assert.Equal("U", gps[1].Name);
            Assert.Equal("V", gps[2].Name);
            foreach (Type gp in gps)
            {
                gp.TestGenericTypeParameterInvariants();
            }
        }

        [Fact]
        public static void TestMakeArrayNegativeIndex()
        {
            Type t = typeof(object).Project();
            Assert.Throws<IndexOutOfRangeException>(() => t.MakeArrayType(rank: -1));
        }

        [Fact]
        public static void TestMakeGenericTypeNegativeInput()
        {
            Type t = typeof(GenericClass3<,,>).Project();
            Type[] typeArguments = null;
            Assert.Throws<ArgumentNullException>(() => t.MakeGenericType(typeArguments));

            typeArguments = new Type[2];  // Wrong number of arguments
            Assert.Throws<ArgumentException>(() => t.MakeGenericType(typeArguments));

            typeArguments = new Type[4];  // Wrong number of arguments
            Assert.Throws<ArgumentException>(() => t.MakeGenericType(typeArguments));

            typeArguments = new Type[] { typeof(int).Project(), null, typeof(int).Project() }; // Null embedded in array.
            Assert.Throws<ArgumentNullException>(() => t.MakeGenericType(typeArguments));
        }
    }
}
