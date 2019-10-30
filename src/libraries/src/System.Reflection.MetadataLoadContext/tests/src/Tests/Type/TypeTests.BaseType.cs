// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        public static IEnumerable<object[]> BaseTypeTestTheoryData => BaseTypeTestTypeData.Wrap();

        public static IEnumerable<object[]> BaseTypeTestTypeData
        {
            get
            {
                yield return new object[] { typeof(object).Project(), null };
                yield return new object[] { typeof(Interface1).Project(), null };
                yield return new object[] { typeof(int).Project(), typeof(ValueType).Project() };
                yield return new object[] { typeof(int[]).Project(), typeof(Array).Project() };
                yield return new object[] { typeof(int).Project().MakeByRefType(), null };
                yield return new object[] { typeof(int).Project().MakePointerType(), null };
                yield return new object[] { typeof(Derived1).Project(), typeof(Base1).Project() };
                yield return new object[] { typeof(Derived2).Project(), typeof(GenericClass1<int>).Project() };

                {
                    Type derived3 = typeof(Derived3<,>).Project();
                    Type theT = derived3.GetTypeInfo().GenericTypeParameters[0];
                    Type theU = derived3.GetTypeInfo().GenericTypeParameters[1];
                    Type baseType = typeof(GenericClass2<,>).Project();
                    yield return new object[] { derived3, baseType.MakeGenericType(theU, theT) };

                    Type t1 = typeof(int).Project();
                    Type t2 = typeof(string).Project();
                    yield return new object[] { derived3.MakeGenericType(t1, t2), baseType.MakeGenericType(t2, t1) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(BaseTypeTestTheoryData))]
        public static void TestBaseType(TypeWrapper tw, TypeWrapper expectedBaseTypeW)
        {
            Type t = tw?.Type;
            Type expectedBaseType = expectedBaseTypeW?.Type;

            Type actualBaseType = t.BaseType;
            Assert.Equal(expectedBaseType, actualBaseType);
        }
    }
}
