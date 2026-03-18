// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace GenericStaticVirtualMethod
{
    // Tests static virtual methods that have their own generic type parameters
    // (method-level generics). This exercises the methodHasCanonInstantiation check
    // and the MakeInstantiatedMethod path in TryResolveConstraintMethodApprox.

    interface IFactory
    {
        static abstract T Create<T>() where T : new();
        static abstract U Transform<U>(U input);
    }

    struct StructFactory : IFactory
    {
        public static T Create<T>() where T : new() => new T();
        public static U Transform<U>(U input) => input;
    }

    class ClassFactory : IFactory
    {
        public static T Create<T>() where T : new() => new T();
        public static U Transform<U>(U input) => input;
    }

    public class Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static T CallCreate<TFactory, T>() where TFactory : IFactory where T : new()
            => TFactory.Create<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static U CallTransform<TFactory, U>(U input) where TFactory : IFactory
            => TFactory.Transform(input);

        [Fact]
        public static void TestEntryPoint()
        {
            // Generic static virtual method on struct
            object obj = CallCreate<StructFactory, object>();
            Assert.NotNull(obj);

            // Generic static virtual method on class
            obj = CallCreate<ClassFactory, object>();
            Assert.NotNull(obj);

            // Transform with different type args
            Assert.Equal(42, CallTransform<StructFactory, int>(42));
            Assert.Equal("hello", CallTransform<ClassFactory, string>("hello"));
        }
    }
}
