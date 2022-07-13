// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Metadata
{
    internal interface IJsonPolymorphicTypeConfiguration
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        Type BaseType { get; }
        string? TypeDiscriminatorPropertyName { get; }
        JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; }
        bool IgnoreUnrecognizedTypeDiscriminators { get; }
        IEnumerable<(Type DerivedType, object? TypeDiscriminator)> GetSupportedDerivedTypes();
#pragma warning restore CA2252 // This API requires opting into preview features
    }
}
