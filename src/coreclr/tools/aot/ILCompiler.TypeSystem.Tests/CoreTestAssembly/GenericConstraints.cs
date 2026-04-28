// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GenericConstraints
{
    public interface INonGen { }

    public interface IGen<in T> { }

    public class Arg1 : INonGen { }

    public class Arg2<T> { }

    public class Arg3<T> : IGen<T> { }

    public struct StructArgWithDefaultCtor { }

    public struct StructArgWithoutDefaultCtor
    {
        public StructArgWithoutDefaultCtor(int argument) { }
    }

    public class ClassArgWithDefaultCtor : IGen<object>
    {
        public ClassArgWithDefaultCtor() { }
    }

    public abstract class AbstractClassArgWithDefaultCtor : IGen<object>
    {
        public AbstractClassArgWithDefaultCtor() { }
    }

    public class ClassArgWithPrivateDefaultCtor : IGen<object>
    {
        private ClassArgWithPrivateDefaultCtor() { }
    }

    public class ClassArgWithoutDefaultCtor : IGen<object>
    {
        public ClassArgWithoutDefaultCtor(int argument) { }
    }

    public class ReferenceTypeConstraint<T> where T : class { }

    public class DefaultConstructorConstraint<T> where T : new() { }

    public class NotNullableValueTypeConstraint<T> where T : struct { }

    public class SimpleTypeConstraint<T> where T : Arg1 { }

    public class DoubleSimpleTypeConstraint<T> where T : Arg1, INonGen { }

    public class SimpleGenericConstraint<T, U> where T : U { }

    public class ComplexGenericConstraint1<T, U> where T : Arg2<int> { }

    public class ComplexGenericConstraint2<T, U> where T : Arg2<Arg2<U>> { }

    public class ComplexGenericConstraint3<T, U> where T : IGen<U> { }

    public class ComplexGenericConstraint4<T, U> where T : U where U : IGen<T> { }

    public class MultipleConstraints<T, U> where T : class, IGen<U>, new() { }

    public class GenericMethods
    {
        public static void SimpleGenericConstraintMethod<T, U>() where T : U { }

        public static void ComplexGenericConstraintMethod<T, U>() where T : U where U : IGen<T> { }
    }
}
