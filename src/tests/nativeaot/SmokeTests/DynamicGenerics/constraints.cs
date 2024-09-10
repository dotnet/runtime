// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using RuntimeLibrariesTest;
using TypeOfRepo;

public class ConstraintsTests {

    public interface IFoo { }

    public interface IFooer : IFoo { }

    public class Base : IFoo { }

    public class Derived : Base { }

    public class GenericBase<T> { }

    public class GenericDerived<T> : GenericBase<T> {}

    public struct Struct : IFoo { }

    public class OtherBase : IFooer { }

    public class TypeRequiringIFoo<T> where T : IFoo { }

    public class TypeWithPrivateCtor
    {
        private TypeWithPrivateCtor() { }

        static TypeWithPrivateCtor()
        {
            Assert.AreEqual(0, 0);
        }
    }

    public class TypeWithPublicCtor { }

    public class TypeWithClassConstraint<T> where T : class { }

    public class TypeWithNewConstraint<T> where T : new() { }

    public class TypeWithStructConstraint<T> where T : struct { }

    public class TypeWithNoConstraint<T> { }

    public class TypeWithSelfReferenceConstraint<T, U> where T : U { }

    public class TypeWithSelfReferenceIEnumerableConstraint<T, U> where T : IEnumerable<U> { }

    public interface IBar<in T> { }

    public class TypeImplementingIBarBase : IBar<Base> { }

    public class TypeImplementingIBarDerived : IBar<Derived> { }

    public class TypeImplementingIBar<T> : IBar<T> { }

    public class TypeWithVariance<T, U> where T : IBar<U> { }

    public class TypeWithRecursiveConstraints<T, S> : TypeWithClassConstraint<T> where T : TypeWithNoConstraint<S> { }

    [TestMethod]
    public static unsafe void TestInvalidInstantiations() {
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithNoConstraint.MakeGenericType(typeof(Object), typeof(Object)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithNoConstraint.MakeGenericType(typeof(void)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithNoConstraint.MakeGenericType(typeof(int*)));
    }

    [TestMethod]
    public static void TestSpecialConstraints() {
        // These should satisfy constraint validation
        TypeOf.CT_TypeWithClassConstraint.MakeGenericType(typeof(TypeWithPublicCtor));
        TypeOf.CT_TypeWithNewConstraint.MakeGenericType(typeof(TypeWithPublicCtor));
        TypeOf.CT_TypeWithClassConstraint.MakeGenericType(typeof(TypeWithPublicCtor[]));

        // These should throw
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithClassConstraint.MakeGenericType(typeof(int)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithNewConstraint.MakeGenericType(typeof(TypeWithPrivateCtor)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithStructConstraint.MakeGenericType(typeof(Base)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithStructConstraint.MakeGenericType(typeof(int?)));
    }

    [TestMethod]
    public static void TestTypeConstraints()
    {
        // These should satisfy constraint validation
        TypeOf.CT_TypeWithSelfReferenceConstraint.MakeGenericType(typeof(Derived), typeof(Base));
        TypeOf.CT_TypeWithSelfReferenceConstraint.MakeGenericType(typeof(Derived), typeof(Derived));
        TypeOf.CT_TypeWithSelfReferenceConstraint.MakeGenericType(typeof(GenericDerived<int>), typeof(GenericBase<int>));
        TypeOf.CT_TypeRequiringIFoo.MakeGenericType(typeof(Base));
        TypeOf.CT_TypeRequiringIFoo.MakeGenericType(typeof(Derived));
        TypeOf.CT_TypeRequiringIFoo.MakeGenericType(typeof(IFooer));
        TypeOf.CT_TypeRequiringIFoo.MakeGenericType(typeof(OtherBase));
        TypeOf.CT_TypeWithVariance.MakeGenericType(typeof(IBar<Base>), typeof(Derived));
        TypeOf.CT_TypeWithVariance.MakeGenericType(typeof(TypeImplementingIBarBase), typeof(Derived));
        TypeOf.CT_TypeWithVariance.MakeGenericType(typeof(TypeImplementingIBarBase), typeof(Base));
        TypeOf.CT_TypeWithVariance.MakeGenericType(typeof(TypeImplementingIBar<Base>), typeof(Derived));
        TypeOf.CT_TypeWithRecursiveConstraints.MakeGenericType(typeof(TypeWithNoConstraint<Base>), typeof(Base));
        TypeOf.CT_TypeWithSelfReferenceIEnumerableConstraint.MakeGenericType(typeof(UInt32[]), typeof(Int32));
        TypeOf.CT_TypeWithSelfReferenceIEnumerableConstraint.MakeGenericType(typeof(String[]), typeof(Object));

        // These should throw
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithSelfReferenceConstraint.MakeGenericType(typeof(Base), typeof(Derived)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithVariance.MakeGenericType(typeof(IBar<Derived>), typeof(Base)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithVariance.MakeGenericType(typeof(TypeImplementingIBar<Derived>), typeof(Base)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithVariance.MakeGenericType(typeof(TypeImplementingIBarDerived), typeof(Base)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithRecursiveConstraints.MakeGenericType(typeof(TypeWithNoConstraint<Derived>), typeof(Base)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithRecursiveConstraints.MakeGenericType(typeof(TypeWithNoConstraint<Base>), typeof(Derived)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithRecursiveConstraints.MakeGenericType(typeof(Base), typeof(Base)));
        Assert.Throws<ArgumentException>(() => TypeOf.CT_TypeWithSelfReferenceIEnumerableConstraint.MakeGenericType(typeof(UInt32[]), typeof(Int16)));
    }
}
