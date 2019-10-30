// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SampleMetadata;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        [Fact]
        public static void TestGenericTypeParameterConstraints_None()
        {
            Type theT = typeof(GenericClassWithNoConstraint<>).Project().GetTypeInfo().GenericTypeParameters[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Assert.Equal(0, theT.GetGenericParameterConstraints().Length);
            Assert.Equal(typeof(object).Project(), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Class()
        {
            Type theT = typeof(GenericClassWithClassConstraint<>).Project().GetTypeInfo().GenericTypeParameters[0];
            Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, theT.GenericParameterAttributes);
            Assert.Equal(0, theT.GetGenericParameterConstraints().Length);
            Assert.Equal(typeof(object).Project(), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Struct()
        {
            Type theT = typeof(GenericClassWithStructConstraint<>).Project().GetTypeInfo().GenericTypeParameters[0];
            Assert.Equal(GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(typeof(ValueType).Project(), constraints[0]);
            Assert.Equal(typeof(ValueType).Project(), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_New()
        {
            Type theT = typeof(GenericClassWithNewConstraint<>).Project().GetTypeInfo().GenericTypeParameters[0];
            Assert.Equal(GenericParameterAttributes.DefaultConstructorConstraint, theT.GenericParameterAttributes);
            Assert.Equal(0, theT.GetGenericParameterConstraints().Length);
            Assert.Equal(typeof(object).Project(), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Type()
        {
            Type theT = typeof(GenericClassWithTypeConstraints<>).Project().GetTypeInfo().GenericTypeParameters[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            constraints = constraints.OrderBy(c => c.Name).ToArray();
            Assert.Equal(3, constraints.Length);
            Assert.Equal(typeof(CConstrained1).Project(), constraints[0]);
            Assert.Equal(typeof(IConstrained1).Project(), constraints[1]);
            Assert.Equal(typeof(IConstrained2<>).Project().MakeGenericType(theT), constraints[2]);
            Assert.Equal(typeof(CConstrained1).Project(), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Interface()
        {
            Type theT = typeof(GenericClassWithInterfaceConstraints<>).Project().GetTypeInfo().GenericTypeParameters[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            constraints = constraints.OrderBy(c => c.Name).ToArray();
            Assert.Equal(2, constraints.Length);
            Assert.Equal(typeof(IConstrained1).Project(), constraints[0]);
            Assert.Equal(typeof(IConstrained2<>).Project().MakeGenericType(theT), constraints[1]);
            Assert.Equal(typeof(object).Project(), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Quirky1()
        {
            Type theT = typeof(GenericClassWithQuirkyConstraints1<,>).Project().GetTypeInfo().GenericTypeParameters[0];
            Type theU = typeof(GenericClassWithQuirkyConstraints1<,>).Project().GetTypeInfo().GenericTypeParameters[1];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(theU, constraints[0]);

            // You'd expect the BaseType to be "U" but due to a compat quirk, it reports as "System.Object"
            Assert.Equal(typeof(object).Project(), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Quirky2()
        {
            Type theT = typeof(GenericClassWithQuirkyConstraints2<,>).Project().GetTypeInfo().GenericTypeParameters[0];
            Type theU = typeof(GenericClassWithQuirkyConstraints2<,>).Project().GetTypeInfo().GenericTypeParameters[1];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(theU, constraints[0]);

            // This one reports the BaseType to be "U" as expected. The "fix" was that U had a "class" constraint.
            Assert.Equal(theU, theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraintsAlwaysReturnsDifferentObject()
        {
            Type theT = typeof(GenericClassWithTypeConstraints<>).Project().GetTypeInfo().GenericTypeParameters[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            TestUtils.AssertNewObjectReturnedEachTime(() => theT.GetGenericParameterConstraints());
        }


        [Fact]
        public static void TestGenericMethodParameterConstraints()
        {
            TypeInfo t = typeof(GenericMethodWithTypeConstraints<>).Project().GetTypeInfo();
            Type theT = t.GetTypeInfo().GenericTypeParameters[0];
            MethodInfo m = t.GetDeclaredMethod("Foo");
            Type theM = m.GetGenericArguments()[0];
            Type theN = m.GetGenericArguments()[1];

            {
                Type[] constraints = theM.GetGenericParameterConstraints();
                Assert.Equal(1, constraints.Length);
                Assert.Equal(typeof(IConstrained2<>).Project().MakeGenericType(theN), constraints[0]);
            }

            {
                Type[] constraints = theN.GetGenericParameterConstraints();
                Assert.Equal(1, constraints.Length);
                Assert.Equal(typeof(IConstrained2<>).Project().MakeGenericType(theT), constraints[0]);
            }
        }
    }
}
