// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models a resolved derived type for polymorphic serialization.
    /// </summary>
    public sealed record PolymorphicDerivedTypeSpec
    {
        public required TypeRef DerivedType { get; init; }

        /// <summary>
        /// The type discriminator, either a <see cref="string"/> or <see cref="int"/> value, or null.
        /// </summary>
        public required object? TypeDiscriminator { get; init; }
    }
}
