// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    /// <summary>
    /// Encapsulates a generic type parameter that can be unpacked on-demand using a generic visitor,
    /// encoding an existential type. Can use reflection to instantiate type parameters that are not known at compile time.
    /// Uses concepts taken from https://www.microsoft.com/en-us/research/publication/generalized-algebraic-data-types-and-object-oriented-programming/
    /// </summary>
    internal abstract class TypeWitness
    {
        public abstract Type Type { get; }
        public abstract TResult Accept<TState, TResult>(ITypeVisitor<TState, TResult> builder, TState state);

        [RequiresUnreferencedCode("Uses Type.MakeGenericType to instantiate a generic TypeWitness<T> using reflection.")]
        [RequiresDynamicCode("Uses Type.MakeGenericType to instantiate a generic TypeWitness<T> using reflection.")]
        public static TypeWitness Create(Type type) => (TypeWitness)Activator.CreateInstance(typeof(TypeWitness<>).MakeGenericType(type))!;
    }

    internal sealed class TypeWitness<T> : TypeWitness
    {
        public override Type Type => typeof(T);
        public override TResult Accept<TState, TResult>(ITypeVisitor<TState, TResult> builder, TState state)
            => builder.Visit<T>(state);
    }

    /// <summary>
    /// Given a <typeparamref name="TState" /> input, visits the generic parameter of a
    /// <see cref="TypeWitness{T}"/> and returns a <typeparamref name="TResult" /> output.
    /// </summary>
    internal interface ITypeVisitor<TState, TResult>
    {
        TResult Visit<T>(TState state);
    }

    internal static class TypeVisitorExtensions
    {
        [RequiresUnreferencedCode("Uses Type.MakeGenericType to instantiate a generic TypeWitness<T> using reflection.")]
        [RequiresDynamicCode("Uses Type.MakeGenericType to instantiate a generic TypeWitness<T> using reflection.")]
        public static TResult Visit<TState, TResult>(this ITypeVisitor<TState, TResult> visitor, Type type, TState state)
            => TypeWitness.Create(type).Accept(visitor, state);
    }
}
