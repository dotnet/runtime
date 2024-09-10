// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using RuntimeLibrariesTest;
using TypeOfRepo;

public class MethodConstraintsTests {

    public interface IFoo { }

    public interface IFooer : IFoo { }

    public class Base : IFoo { }

    public class Derived : Base { }

    public class DerivedFromDerived : Derived { }

    public struct Struct : IFoo { }

    public class OtherBase : IFooer { }

    public class TypeRequiringIFoo
    {
        public void Method<T>() where T : IFoo { }
    }

    public class TypeWithPrivateCtor
    {
        private TypeWithPrivateCtor() { }

        static TypeWithPrivateCtor()
        {
            Assert.AreEqual(0, 0);
        }
    }

    public class TypeWithPublicCtor { }

    public class TypeWithClassConstraint
    {
        public void Method<T>() where T : class { }
    }

    public class TypeWithNewConstraint
    {
        public void Method<T>() where T : new() { }
    }

    public class TypeWithStructConstraint
    {
        public void Method<T>() where T : struct { }
    }

    public class TypeWithNoConstraint
    {
        public void Method<T>() { }
    }

    public class TypeWithNoConstraint<T> { }

    public class TypeWithSelfReferenceConstraint
    {
        public void Method<T, U>() where T : U { }
    }

    public class TypeWithSelfReferenceIEnumerableConstraint
    {
        public void Method<T, U>() where T : IEnumerable<U> { }
    }

    public interface IBar<in T> { }

    public class TypeImplementingIBarBase : IBar<Base> { }

    public class TypeImplementingIBarDerived : IBar<Derived> { }

    public class TypeImplementingIBar<T> : IBar<T> { }

    public class TypeWithVariance
    {
        public void Method<T, U>() where T : IBar<U> { }
    }

    public class TypeWithRecursiveConstraints
    {
        public void Method<T, S>() where T : TypeWithNoConstraint<S> { }
    }

    public class TypeWithMDArrayConstraints
    {
        public void Method<T>() where T : IEnumerable<Derived[, ,]>
        { }
    }

    public class GenericType<T, U>
    {
        public void Method<V>() where V : U { }
    }

    static MethodInfo MakeGenericMethod(Type t, Type genArg) { return t.GetTypeInfo().GetDeclaredMethod("Method").MakeGenericMethod(genArg); }
    static MethodInfo MakeGenericMethod(Type t, Type genArg1, Type genArg2) { return t.GetTypeInfo().GetDeclaredMethod("Method").MakeGenericMethod(genArg1, genArg2); }

    [TestMethod]
    public static unsafe void TestInvalidInstantiations() {
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithNoConstraint, typeof(Object), typeof(Object)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithNoConstraint, typeof(void)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithNoConstraint, typeof(int*)));
    }

    [TestMethod]
    public static void TestSpecialConstraints() {
        // These should satisfy constraint validation
        MakeGenericMethod(TypeOf.MCT_TypeWithClassConstraint, typeof(TypeWithPublicCtor));
        MakeGenericMethod(TypeOf.MCT_TypeWithNewConstraint, typeof(TypeWithPublicCtor));
        MakeGenericMethod(TypeOf.MCT_TypeWithClassConstraint, typeof(TypeWithPublicCtor[]));

        // These should throw
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithClassConstraint, typeof(int)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithNewConstraint, typeof(TypeWithPrivateCtor)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithStructConstraint, typeof(Base)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithStructConstraint, typeof(int?)));
    }

    [TestMethod]
    public static void TestTypeConstraints()
    {
        // These should satisfy constraint validation
        MakeGenericMethod(TypeOf.MCT_TypeWithSelfReferenceConstraint, typeof(Derived), typeof(Base));
        MakeGenericMethod(TypeOf.MCT_TypeWithSelfReferenceConstraint, typeof(Derived), typeof(Derived));
        MakeGenericMethod(TypeOf.MCT_TypeRequiringIFoo, typeof(Base));
        MakeGenericMethod(TypeOf.MCT_TypeRequiringIFoo, typeof(Derived));
        MakeGenericMethod(TypeOf.MCT_TypeRequiringIFoo, typeof(IFooer));
        MakeGenericMethod(TypeOf.MCT_TypeRequiringIFoo, typeof(OtherBase));
        MakeGenericMethod(TypeOf.MCT_TypeWithVariance, typeof(IBar<Base>), typeof(Derived));
        MakeGenericMethod(TypeOf.MCT_TypeWithVariance, typeof(TypeImplementingIBarBase), typeof(Derived));
        MakeGenericMethod(TypeOf.MCT_TypeWithVariance, typeof(TypeImplementingIBarBase), typeof(Base));
        MakeGenericMethod(TypeOf.MCT_TypeWithVariance, typeof(TypeImplementingIBar<Base>), typeof(Derived));
        MakeGenericMethod(TypeOf.MCT_TypeWithRecursiveConstraints, typeof(TypeWithNoConstraint<Base>), typeof(Base));
        MakeGenericMethod(TypeOf.MCT_TypeWithSelfReferenceIEnumerableConstraint, typeof(UInt32[]), typeof(Int32));
        MakeGenericMethod(TypeOf.MCT_TypeWithSelfReferenceIEnumerableConstraint, typeof(String[]), typeof(Object));
        MakeGenericMethod(TypeOf.MCT_GenericType.MakeGenericType(new Type[] { typeof(object), typeof(object) }), typeof(String));

        // These should throw
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithSelfReferenceConstraint, typeof(Base), typeof(Derived)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithVariance, typeof(IBar<Derived>), typeof(Base)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithVariance, typeof(TypeImplementingIBar<Derived>), typeof(Base)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithVariance, typeof(TypeImplementingIBarDerived), typeof(Base)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithRecursiveConstraints, typeof(TypeWithNoConstraint<Derived>), typeof(Base)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithRecursiveConstraints, typeof(TypeWithNoConstraint<Base>), typeof(Derived)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithRecursiveConstraints, typeof(Base), typeof(Base)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithSelfReferenceIEnumerableConstraint, typeof(UInt32[]), typeof(Int16)));
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_GenericType.MakeGenericType(new Type[] { typeof(string), typeof(string) }), typeof(object)));
    }

    [TestMethod]
    public static void TestMDTypeConstraints()
    {
        // Non-variant check
        MakeGenericMethod(TypeOf.MCT_TypeWithMDArrayConstraints, typeof(Derived[][, ,]));

        // Further derived type check
        MakeGenericMethod(TypeOf.MCT_TypeWithMDArrayConstraints, typeof(DerivedFromDerived[][,,]));

        // not as derived type check
        Assert.Throws<ArgumentException>(() => MakeGenericMethod(TypeOf.MCT_TypeWithMDArrayConstraints, typeof(Base[][,,])));
    }
}
