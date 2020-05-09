// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;
using System.Runtime.CompilerServices;
using System.Tests;
using System.Tests.Types;
using Xunit;
using Xunit.Sdk;

namespace System.Reflection.Tests
{
    public unsafe class ObjectFactoryTests
    {
        // Types that should fail when used in the ObjectFactory
        public static IEnumerable<object[]> NegativeTypes()
        {
            foreach (Type type in EnumerateTestCases())
            {
                yield return new object[] { type };
            }

            IEnumerable<Type> EnumerateTestCases()
            {
                yield return null;
                yield return typeof(int*); // pointers are disallowed
                yield return typeof(int).MakeByRef(); // byrefs are disallowed
                yield return typeof(Span<byte>); // ref structs are disallowed
                yield return typeof(Enum); // uninstantiable type
                yield return typeof(ValueType); // uninstantiable type
                yield return typeof(Array); // uninstantiable type
                yield return typeof(IDisposable); // interfaces disallowed
                yield return typeof(string); // variable length types disallowed
                yield return typeof(int[]); // variable length types disallowed
                yield return typeof(MyAbstractClass); // abstract types disallowed
                yield return typeof(List<>); // open generic types disallowed
                yield return CanonType; // canon type disallowed
                yield return typeof(List<>).MakeGenericType(CanonType); // shared instantiation disallowed
                yield return typeof(ClassWithoutParameterlessCtor); // parameterless ctor not found
                yield return typeof(StaticClass); // static types disallowed
                yield return typeof(delegate*<void>); // function pointers disallowed
                yield return typeof(List<>).GetGenericArguments(0); // type parameters disallowed
            }
        }

        [Theory]
        [MemberData(nameof(NegativeTypes))]
        public void Activator_CreateFactory_OfInvalidType_Throws(Type type)
        {
            Exception throwException = Assert.ThrowsAny<Exception>(() => Activator.CreateFactory(type, nonPublic: true));

            switch (throwException)
            {
                case ArgumentException:
                case MissingMethodException:
                case NotSupportedException:
                    break;

                default:
                    throw new XunitException($"Unexpected exception {throwException.GetType()} thrown.");
            }
        }

        [Theory]
        [MemberData(nameof(NegativeTypes))]
        public void RuntimeHelpers_CreateUninitializedObjectFactory_OfInvalidType_Throws(Type type)
        {
            Exception throwException = Assert.ThrowsAny<Exception>(() => RuntimeHelpers.CreateUninitializedObjectFactory(type));

            switch (throwException)
            {
                case ArgumentException:
                case MissingMethodException:
                case NotSupportedException:
                    break;

                default:
                    throw new XunitException($"Unexpected exception {throwException.GetType()} thrown.");
            }
        }

        [Fact]
        public void Activator_CreateFactory_ReferenceTypeWithParameterlessCtor()
        {
            ClassWithPublicParameterlessCtor c1 = Activator.CreateFactory(typeof(ClassWithPublicParameterlessCtor), nonPublic: false)() as ClassWithPublicParameterlessCtor;
            Assert.NotNull(c1);
            Assert.True(c1.WasConstructorCalled);

            ClassWithPublicParameterlessCtor c2 = Activator.CreateFactory(typeof(ClassWithPublicParameterlessCtor), nonPublic: true)() as ClassWithPublicParameterlessCtor;
            Assert.NotNull(c2);
            Assert.True(c2.WasConstructorCalled);

            // if ctor not visible, shouldn't even get as far as creating the factory
            Assert.Throws<MissingMethodException>(() => Activator.CreateFactory(typeof(ClassWithoutParameterlessCtor), nonPublic: false));

            ClassWithoutParameterlessCtor c4 = Activator.CreateFactory(typeof(ClassWithoutParameterlessCtor), nonPublic: true)() as ClassWithoutParameterlessCtor;
            Assert.NotNull(c4);
            Assert.True(c4.WasConstructorCalled);
        }

        [Fact]
        public void RuntimeHelpers_CreateUninitializedObjectFactory_ReferenceTypeWithParameterlessCtor()
        {
            ClassWithPublicParameterlessCtor c1 = RuntimeHelpers.CreateUninitializedObjectFactory(typeof(ClassWithPublicParameterlessCtor))() as ClassWithPublicParameterlessCtor;
            Assert.NotNull(c1);
            Assert.False(c1.WasConstructorCalled);

            ClassWithNonPublicParameterlessCtor c2 = RuntimeHelpers.CreateUninitializedObjectFactory(typeof(ClassWithNonPublicParameterlessCtor))() as ClassWithNonPublicParameterlessCtor;
            Assert.NotNull(c2);
            Assert.False(c2.WasConstructorCalled);

            ClassWithoutParameterlessCtor c3 = RuntimeHelpers.CreateUninitializedObjectFactory(typeof(ClassWithoutParameterlessCtor))() as ClassWithoutParameterlessCtor;
            Assert.NotNull(c3);
            Assert.False(c3.WasConstructorCalled);
        }

        // TODO Unit tests:
        // When T is value type with custom ctor
        // When T is value type without custom ctor
        // When T is Nullable<U>: Activator should return null, RuntimeHelpers should return boxed default(U)
        // Activator.CreateFactory<T> helper

        public static Type CanonType
        {
            get
            {
                Type type = typeof(object).Assembly.GetType("System.__Canon");
                Assert.NotNull(type);
                return type;
            }
        }

        public abstract class MyAbstractClass { }

        public class ClassWithoutParameterlessCtor
        {
            public bool WasConstructorCalled;

            public ClassWithoutParameterlessCtor(int i)
            {
                WasConstructorCalled = true;
            }
        }

        public static class StaticClass { }

        public class ClassWithPublicParameterlessCtor
        {
            public bool WasConstructorCalled;

            public ClassWithPublicParameterlessCtor()
            {
                WasConstructorCalled = true;
            }
        }

        public class ClassWithNonPublicParameterlessCtor
        {
            public bool WasConstructorCalled;

            internal ClassWithNonPublicParameterlessCtor()
            {
                WasConstructorCalled = true;
            }
        }
    }
}
