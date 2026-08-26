// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace StaticVirtualOnReferenceType
{
    // Tests static virtual method dispatch on reference types (classes) through
    // shared generic constrained calls. This exercises the code path where
    // TryResolveConstraintMethodApprox resolves a static virtual on a non-value-type,
    // which requires the relaxed assertion (IsValueType || Signature.IsStatic).

    interface IIdentity
    {
        static abstract string Name();
    }

    class ClassImpl : IIdentity
    {
        public static string Name() => nameof(ClassImpl);
    }

    class AnotherClassImpl : IIdentity
    {
        public static string Name() => nameof(AnotherClassImpl);
    }

    struct StructImpl : IIdentity
    {
        public static string Name() => nameof(StructImpl);
    }

    interface IGenericIdentity<T>
    {
        static abstract string Name();
    }

    class GenericClassImpl<T> : IGenericIdentity<T>
    {
        public static string Name() => typeof(T).Name;
    }

    public class Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static string GetName<T>() where T : IIdentity => T.Name();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string GetGenericName<T, U>() where T : IGenericIdentity<U> => T.Name();

        [Fact]
        public static void TestEntryPoint()
        {
            // Static virtual on reference type (class) through constrained call
            Assert.Equal(nameof(ClassImpl), GetName<ClassImpl>());
            Assert.Equal(nameof(AnotherClassImpl), GetName<AnotherClassImpl>());

            // Static virtual on value type (struct) through same constrained call
            Assert.Equal(nameof(StructImpl), GetName<StructImpl>());

            // Generic interface with reference type implementation
            Assert.Equal("Int32", GetGenericName<GenericClassImpl<int>, int>());
            Assert.Equal("String", GetGenericName<GenericClassImpl<string>, string>());
        }
    }
}
