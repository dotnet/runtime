// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Generic.Tests
{
    public class ReferenceEqualityComparerTests
    {
        [Fact]
        public void TypeHasNoPublicCtors()
        {
            ConstructorInfo[] ctors = typeof(ReferenceEqualityComparer).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.DoesNotContain(ctors, ctor => ctor.IsPublic || ctor.IsFamily || ctor.IsFamilyOrAssembly);
        }

        [Fact]
        public void InstanceProperty_ReturnsSingleton()
        {
            ReferenceEqualityComparer comparer1 = ReferenceEqualityComparer.Instance;
            Assert.NotNull(comparer1);

            ReferenceEqualityComparer comparer2 = ReferenceEqualityComparer.Instance;
            Assert.Same(comparer1, comparer2);
        }

        [Fact]
        public void Equals_UsesReferenceEquals()
        {
            MyClass o1 = new MyClass { SomeInt = 10 };
            MyClass o2 = new MyClass { SomeInt = 10 };
            Assert.Equal(o1, o2);

            ReferenceEqualityComparer comparer1 = ReferenceEqualityComparer.Instance;
            IEqualityComparer comparer2 = ReferenceEqualityComparer.Instance;
            IEqualityComparer<object> comparer3 = ReferenceEqualityComparer.Instance;
            IEqualityComparer<MyClass> comparer4 = ReferenceEqualityComparer.Instance; // test contravariance

            Assert.True(comparer1.Equals(null, null));
            Assert.False(comparer1.Equals(null, o2));
            Assert.False(comparer1.Equals(o1, o2));
            Assert.False(comparer1.Equals(o1, null));

            Assert.True(comparer2.Equals(null, null));
            Assert.False(comparer2.Equals(null, o2));
            Assert.False(comparer2.Equals(o1, o2));
            Assert.False(comparer2.Equals(o1, null));

            Assert.True(comparer3.Equals(null, null));
            Assert.False(comparer3.Equals(null, o2));
            Assert.False(comparer3.Equals(o1, o2));
            Assert.False(comparer3.Equals(o1, null));

            Assert.True(comparer4.Equals(null, null));
            Assert.False(comparer4.Equals(null, o2));
            Assert.False(comparer4.Equals(o1, o2));
            Assert.False(comparer4.Equals(o1, null));
        }

        [Fact]
        public void GetHashCode_UsesRuntimeHelpers()
        {
            ClassWithBadGetHashCodeImplementation o = new ClassWithBadGetHashCodeImplementation(); // make sure we don't call object.GetHashCode()

            ReferenceEqualityComparer comparer1 = ReferenceEqualityComparer.Instance;
            IEqualityComparer comparer2 = ReferenceEqualityComparer.Instance;
            IEqualityComparer<object> comparer3 = ReferenceEqualityComparer.Instance;
            IEqualityComparer<ClassWithBadGetHashCodeImplementation> comparer4 = ReferenceEqualityComparer.Instance; // test contravariance

            int runtimeHelpersHashCode = RuntimeHelpers.GetHashCode(o);
            Assert.Equal(runtimeHelpersHashCode, comparer1.GetHashCode(o));
            Assert.Equal(runtimeHelpersHashCode, comparer2.GetHashCode(o));
            Assert.Equal(runtimeHelpersHashCode, comparer3.GetHashCode(o));
            Assert.Equal(runtimeHelpersHashCode, comparer4.GetHashCode(o));
        }

        private class ClassWithBadGetHashCodeImplementation
        {
            public override int GetHashCode() => throw new NotImplementedException();
        }

        private class MyClass
        {
            public int SomeInt;

            public override bool Equals(object obj) => obj is MyClass c && this.SomeInt == c.SomeInt;
            public override int GetHashCode() => this.SomeInt;
        }
    }
}
