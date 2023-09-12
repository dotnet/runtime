// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record NullableSpec : TypeSpec
    {
        private readonly TypeSpec _underlyingType;

        public NullableSpec(ITypeSymbol type, TypeSpec underlyingType) : base(type) => _underlyingType = underlyingType;

        public override bool CanBindTo => _underlyingType.CanBindTo;

        public override bool CanInstantiate => _underlyingType.CanInstantiate;

        public override TypeSpecKind SpecKind => TypeSpecKind.Nullable;

        public override TypeSpec EffectiveType => _underlyingType;
    }
}
