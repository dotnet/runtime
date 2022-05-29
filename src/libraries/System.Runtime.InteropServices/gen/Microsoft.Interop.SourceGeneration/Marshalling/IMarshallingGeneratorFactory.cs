// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public interface IMarshallingGeneratorFactory
    {
        /// <summary>
        /// Create an <see cref="IMarshallingGenerator"/> instance for marshalling the supplied type in the given position.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="IMarshallingGenerator"/> instance.</returns>
        public IMarshallingGenerator Create(
            TypePositionInfo info,
            StubCodeContext context);
    }

    /// <summary>
    /// A structure that wraps an <see cref="IMarshallingGeneratorFactory"/> with a key for comparison.
    /// This value of this key should encompass all inputs that determine how the IMarshallingGeneratorFactory was created
    /// for a given source generator. This structure allows us to pass <see cref="IMarshallingGeneratorFactory"/> instances
    /// between different stages in an incremental source generator.
    /// </summary>
    /// <typeparam name="T">The type of key.</typeparam>
    public struct MarshallingGeneratorFactoryKey<T> : IEquatable<MarshallingGeneratorFactoryKey<T>>
        where T : IEquatable<T>
    {
        public T Key { get; init; }
        public IMarshallingGeneratorFactory GeneratorFactory { get; init; }

        public bool Equals(MarshallingGeneratorFactoryKey<T> other) => Key.Equals(other.Key);

        public override bool Equals(object obj) => obj is MarshallingGeneratorFactoryKey<T> other && Equals(other);

        public override int GetHashCode() => Key.GetHashCode();
    }

    public static class MarshallingGeneratorFactoryKey
    {
        public static MarshallingGeneratorFactoryKey<T> Create<T>(T key, IMarshallingGeneratorFactory factory) where T : IEquatable<T>
        {
            return new MarshallingGeneratorFactoryKey<T>
            {
                Key = key,
                GeneratorFactory = factory
            };
        }
    }
}
