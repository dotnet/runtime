// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        [Fact]
        public static void TestGenericTypeParameterConstraints_None()
        {
            Type theT = typeof(GenericClassWithNoConstraint<>).GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Assert.Equal(0, theT.GetGenericParameterConstraints().Length);
            Assert.Equal(typeof(object), theT.BaseType);
            Assert.False(theT.IsValueType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Class()
        {
            Type theT = typeof(GenericClassWithClassConstraint<>).GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, theT.GenericParameterAttributes);
            Assert.Equal(0, theT.GetGenericParameterConstraints().Length);
            Assert.Equal(typeof(object), theT.BaseType);
            Assert.False(theT.IsValueType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Struct()
        {
            Type theT = typeof(GenericClassWithStructConstraint<>).GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(typeof(ValueType), constraints[0]);
            Assert.Equal(typeof(ValueType), theT.BaseType);
            Assert.True(theT.IsValueType);
            Assert.False(theT.IsEnum);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Enum()
        {
            Type theT = typeof(GenericClassWithEnumConstraint<>).GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(typeof(Enum), constraints[0]);
            Assert.Equal(typeof(Enum), theT.BaseType);
            Assert.True(theT.IsValueType);
            Assert.True(theT.IsEnum);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_New()
        {
            Type theT = typeof(GenericClassWithNewConstraint<>).GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.DefaultConstructorConstraint, theT.GenericParameterAttributes);
            Assert.Equal(0, theT.GetGenericParameterConstraints().Length);
            Assert.Equal(typeof(object), theT.BaseType);
            Assert.False(theT.IsValueType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Type()
        {
            Type theT = typeof(GenericClassWithTypeConstraints<>).GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            constraints = constraints.OrderBy(c => c.Name).ToArray();
            Assert.Equal(3, constraints.Length);
            Assert.Equal(typeof(CConstrained1), constraints[0]);
            Assert.Equal(typeof(IConstrained1), constraints[1]);
            Assert.Equal(typeof(IConstrained2<>).MakeGenericType(theT), constraints[2]);
            Assert.Equal(typeof(CConstrained1), theT.BaseType);
            Assert.False(theT.IsValueType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Interface()
        {
            Type theT = typeof(GenericClassWithInterfaceConstraints<>).GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            constraints = constraints.OrderBy(c => c.Name).ToArray();
            Assert.Equal(2, constraints.Length);
            Assert.Equal(typeof(IConstrained1), constraints[0]);
            Assert.Equal(typeof(IConstrained2<>).MakeGenericType(theT), constraints[1]);
            Assert.Equal(typeof(object), theT.BaseType);
            Assert.False(theT.IsValueType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Quirky1()
        {
            Type theT = typeof(GenericClassWithQuirkyConstraints1<,>).GetGenericArguments()[0];
            Type theU = typeof(GenericClassWithQuirkyConstraints1<,>).GetGenericArguments()[1];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(theU, constraints[0]);

            // You'd expect the BaseType to be "U" but due to a compat quirk, it reports as "System.Object"
            Assert.Equal(typeof(object), theT.BaseType);
        }

        [Fact]
        public static void TestGenericTypeParameterConstraints_Quirky2()
        {
            Type theT = typeof(GenericClassWithQuirkyConstraints2<,>).GetGenericArguments()[0];
            Type theU = typeof(GenericClassWithQuirkyConstraints2<,>).GetGenericArguments()[1];
            Assert.Equal(GenericParameterAttributes.None, theT.GenericParameterAttributes);
            Type[] constraints = theT.GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(theU, constraints[0]);

            // This one reports the BaseType to be "U" as expected. The "fix" was that U had a "class" constraint.
            Assert.Equal(theU, theT.BaseType);
        }

        [Fact]
        public static void TestGenericMethodParameterConstraints()
        {
            TypeInfo t = typeof(GenericMethodWithTypeConstraints<>).GetTypeInfo();
            Type theT = t.GetGenericArguments()[0];
            MethodInfo m = t.GetDeclaredMethod("Foo");
            Type theM = m.GetGenericArguments()[0];
            Type theN = m.GetGenericArguments()[1];

            {
                Type[] constraints = theM.GetGenericParameterConstraints();
                Assert.Equal(1, constraints.Length);
                Assert.Equal(typeof(IConstrained2<>).MakeGenericType(theN), constraints[0]);
            }

            {
                Type[] constraints = theN.GetGenericParameterConstraints();
                Assert.Equal(1, constraints.Length);
                Assert.Equal(typeof(IConstrained2<>).MakeGenericType(theT), constraints[0]);
            }
        }

        public interface IConstrained1 { }
        public interface IConstrained2<I> { }
        public class CConstrained1 { }

        public class GenericClassWithNoConstraint<T> { }
        public class GenericClassWithClassConstraint<T> where T : class { }
        public class GenericClassWithStructConstraint<T> where T : struct { }
        public class GenericClassWithNewConstraint<T> where T : new() { }
        public class GenericClassWithEnumConstraint<T> where T : Enum { }
        public class GenericClassWithTypeConstraints<T> where T : CConstrained1, IConstrained1, IConstrained2<T> { }
        public class GenericClassWithInterfaceConstraints<T> where T : IConstrained1, IConstrained2<T> { }
        public class GenericClassWithQuirkyConstraints1<T, U> where T : U where U : CConstrained1, IConstrained1 { }
        public class GenericClassWithQuirkyConstraints2<T, U> where T : U where U : class, IConstrained1 { }

        public class GenericMethodWithTypeConstraints<T>
        {
            public void Foo<M, N>() where M : IConstrained2<N> where N : IConstrained2<T> { }
        }
    }
}
