// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ObjectSpec : TypeSpec
    {
        public ObjectSpec(INamedTypeSymbol type) : base(type) { }
        public override TypeSpecKind SpecKind => TypeSpecKind.Object;
        public List<PropertySpec> Properties { get; } = new();
    }
}
