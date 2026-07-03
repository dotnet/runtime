// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace StaticVirtualDIMOnReferenceType
{
    // Tests static virtual default interface method implementations on reference types
    // (classes) through constrained calls. This combines reference type dispatch with DIM
    // resolution, exercising both the assertion relaxation and FindDefaultInterfaceImplementation.

    interface IBase
    {
        static abstract string GetValue();
    }

    interface IWithDefault : IBase
    {
        static string IBase.GetValue() => "DefaultImpl";
    }

    interface IWithOverride : IBase
    {
        static string IBase.GetValue() => "OverrideImpl";
    }

    // Class using DIM from IWithDefault
    class ClassWithDIM : IWithDefault
    {
    }

    // Class using DIM from IWithOverride (more derived override)
    class ClassWithOverrideDIM : IWithOverride
    {
    }

    // IWithDefault is more specific than IBase for GetValue, so ClassWithMostSpecificDIM
    // should resolve to IWithDefault's implementation, not IWithOverride's.
    interface IWithMoreSpecific : IWithDefault
    {
        static string IBase.GetValue() => "MostSpecificImpl";
    }

    class ClassWithMostSpecificDIM : IWithMoreSpecific
    {
    }

    // Struct using DIM from IWithDefault
    struct StructWithDIM : IWithDefault
    {
    }

    // Class with explicit implementation (no DIM)
    class ClassWithExplicitImpl : IBase
    {
        public static string GetValue() => "ExplicitImpl";
    }

    // Generic interface with DIM
    interface IGenericBase<T>
    {
        static abstract string Describe();
    }

    interface IGenericWithDefault<T> : IGenericBase<T>
    {
        static string IGenericBase<T>.Describe() => $"Default<{typeof(T).Name}>";
    }

    class GenericClassWithDIM<T> : IGenericWithDefault<T>
    {
    }

    public class Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static string CallGetValue<T>() where T : IBase => T.GetValue();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string CallDescribe<T, U>() where T : IGenericBase<U> => T.Describe();

        [Fact]
        public static void TestEntryPoint()
        {
            // DIM on reference type
            Assert.Equal("DefaultImpl", CallGetValue<ClassWithDIM>());

            // DIM on reference type from independent interface
            Assert.Equal("OverrideImpl", CallGetValue<ClassWithOverrideDIM>());

            // More specific DIM wins (IWithMoreSpecific derives from IWithDefault)
            Assert.Equal("MostSpecificImpl", CallGetValue<ClassWithMostSpecificDIM>());

            // DIM on value type (existing scenario, for comparison)
            Assert.Equal("DefaultImpl", CallGetValue<StructWithDIM>());

            // Explicit implementation on reference type
            Assert.Equal("ExplicitImpl", CallGetValue<ClassWithExplicitImpl>());

            // Generic DIM on reference type
            Assert.Equal("Default<Int32>", CallDescribe<GenericClassWithDIM<int>, int>());
            Assert.Equal("Default<String>", CallDescribe<GenericClassWithDIM<string>, string>());
        }
    }
}
