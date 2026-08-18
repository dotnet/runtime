// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace CanonicalEquivalentFound
{
    // Tests the canonicalEquivalentFound code path in TryResolveConstraintMethodApprox.
    // When a generic class implements IFoo<T> (which canonicalizes to IFoo<__Canon>),
    // calling through shared generic code with different concrete type arguments exercises
    // the canonical handling where the compiler must determine whether to resolve statically
    // or defer to a runtime lookup.

    interface IProcessor<T>
    {
        static abstract string Process();
    }

    // This class implements IProcessor<T> (becomes IProcessor<__Canon> in shared code)
    // which is the canonical equivalent of IProcessor<string>.
    class GenericProcessor<T> : IProcessor<T>
    {
        public static string Process() => typeof(T).Name;
    }

    struct GenericStructProcessor<T> : IProcessor<T>
    {
        public static string Process() => typeof(T).Name;
    }

    // Non-generic class implementing a specific instantiation
    class StringProcessor : IProcessor<string>
    {
        public static string Process() => "ExplicitString";
    }

    // Generic class implementing a fixed interface instantiation (not its own T)
    class MismatchProcessor<T> : IProcessor<string>
    {
        public static string Process() => $"Mismatch<{typeof(T).Name}>";
    }

    public class Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static string CallProcess<TImpl, T>() where TImpl : IProcessor<T>
            => TImpl.Process();

        [Fact]
        public static void TestEntryPoint()
        {
            // Generic class through shared generic constrained call with different type args.
            // Each call uses the same shared code but different concrete types.
            Assert.Equal("Int32", CallProcess<GenericProcessor<int>, int>());
            Assert.Equal("String", CallProcess<GenericProcessor<string>, string>());
            Assert.Equal("Object", CallProcess<GenericProcessor<object>, object>());

            // Generic struct through same path
            Assert.Equal("Int32", CallProcess<GenericStructProcessor<int>, int>());
            Assert.Equal("String", CallProcess<GenericStructProcessor<string>, string>());

            // Non-generic class implementing specific instantiation
            Assert.Equal("ExplicitString", CallProcess<StringProcessor, string>());

            // Generic class implementing a fixed interface instantiation
            Assert.Equal("Mismatch<Int32>", CallProcess<MismatchProcessor<int>, string>());
            Assert.Equal("Mismatch<Object>", CallProcess<MismatchProcessor<object>, string>());
        }
    }
}
