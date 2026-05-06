// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public partial class TypeTests
    {
        public static IEnumerable<object[]> GetInterface_TestData()
        {
            // No such interface.
            yield return new object[] { typeof(ClassWithNoInterfaces), "NoSuchInterface", false, null };
            yield return new object[] { typeof(ClassWithInterfaces), "NoSuchInterface", false, null };
            yield return new object[] { typeof(ClassWithNoInterfaces), "", false, null };
            yield return new object[] { typeof(ClassWithInterfaces), "", false, null };

            // Variations of case sensitivity.
            yield return new object[] { typeof(ClassWithInterfaces), "Interface1", true, typeof(Interface1) };
            yield return new object[] { typeof(ClassWithInterfaces), "Interface1", false, typeof(Interface1) };
            yield return new object[] { typeof(ClassWithInterfaces), "interface1", true, typeof(Interface1) };
            yield return new object[] { typeof(ClassWithInterfaces), "interface1", false, null };

            // Prefixing not supported.
            yield return new object[] { typeof(ClassWithInterfaces), "Interf*", false, null };

            // Namespaced.
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), "System.Tests.Inner.Interface1", false, typeof(Inner.Interface1) };
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), "Interface1", false, typeof(Inner.Interface1) };
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), "Inner.NamespacedInterface1", false, null };
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), "System.Tests.Inner.interface1", true, typeof(Inner.Interface1) };
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), "System.Tests.inner.Interface1", true, null };
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), "System.Tests.Inner.", false, null };
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), ".Interface1", false, null };
            yield return new object[] { typeof(ClassWithNamespacedInterfaces), ".", false, null };

            // Potential amibguities.
            yield return new object[]  { typeof(ClassWithMixedCaseInterfaces), "MixedInterface", false, typeof(MixedInterface) };

            // Generic type variables.
            yield return new object[] { typeof(GenericClassWithNoConstraints<>).GetTypeInfo().GenericTypeParameters[0], "NoSuchInterface", false, null };
            yield return new object[] { typeof(GenericClassWithNoConstraints<>).GetTypeInfo().GenericTypeParameters[0], "NoSuchInterface", true, null };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "Interface1", false, typeof(Interface1) };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "Interface1", true, typeof(Interface1) };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "interface1", false, null };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "interface1", true, typeof(Interface1) };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "Interface2", false, typeof(Interface2) };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "Interface2", true, typeof(Interface2) };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "NoSuchInterface", false, null };
            yield return new object[] { typeof(GenericClassWithConstraints<>).GetTypeInfo().GenericTypeParameters[0], "NoSuchInterface", true, null };
        }

        [Theory]
        [MemberData(nameof(GetInterface_TestData))]
        public void GetInterface_Invoke_ReturnsExpected(Type type, string name, bool ignoreCase, Type expected)
        {
            if (!ignoreCase)
            {
                Assert.Equal(expected, type.GetInterface(name));
                Assert.Equal(expected, type.GetTypeInfo().GetInterface(name));
            }

            Assert.Equal(expected, type.GetInterface(name, ignoreCase));
            Assert.Equal(expected, type.GetTypeInfo().GetInterface(name, ignoreCase));
        }

        [Fact]
        public void GetInterface_NullName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => typeof(int).GetInterface(null));
        }

        [Fact]
        public void GetInterface_SameNameInterfaces_ThrowsAmbiguousMatchException()
        {
            Assert.Throws<AmbiguousMatchException>(() => typeof(ClassWithTwoSameNameInterfaces).GetInterface("Interface1", ignoreCase: true));
        }

        [Fact]
        public void GetInterface_SameNameInterfaces_FullySpecified_Succeeds()
        {
            Assert.NotNull(typeof(ClassWithTwoSameNameInterfaces).GetInterface("System.Tests.Inner.Interface1", ignoreCase: true));
        }

        [Fact]
        public void GetInterface_MixedCaseAmbiguity_ThrowsAmbiguousMatchException()
        {
            Assert.Throws<AmbiguousMatchException>(() => typeof(ClassWithMixedCaseInterfaces).GetInterface("mixedinterface", ignoreCase: true));
        }
    }

    public class ClassWithNoInterfaces { }

    public class ClassWithInterfaces : Interface1, Interface2, Interface3 { }

    public class ClassWithNamespacedInterfaces : Inner.Interface1, Inner.Interface2, Inner.Interface3 { }

    public class ClassWithMixedCaseInterfaces : MixedInterface, mixedInterface {}

    public interface MixedInterface { }
    public interface mixedInterface { }

    public class ClassWithTwoSameNameInterfaces : Interface1, Inner.Interface1 { }

    public class GenericClassWithNoConstraints<T> { }

    public class GenericClassWithConstraints<T> where T : ClassWithInterfaces, Interface1 { }

    namespace Inner
    {
        public interface Interface1 { }
        public interface Interface2 { }
        public interface Interface3 { }
    }
}

public interface Interface1 { }
public interface Interface2 { }
public interface Interface3 { }
