// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record NullableSpec : TypeSpec
    {
        public NullableSpec(ITypeSymbol type) : base(type) { }
        public override TypeSpecKind SpecKind => TypeSpecKind.Nullable;
        public required TypeSpec UnderlyingType { get; init; }
    }
}
