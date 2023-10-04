// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ObjectSpec : ComplexTypeSpec
    {
        public ObjectSpec(INamedTypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.Object;

        public override bool HasBindableMembers => Properties.Values.Any(p => p.ShouldBindTo);

        public override bool CanInstantiate => InstantiationStrategy is not InstantiationStrategy.None && InitExceptionMessage is null;

        public Dictionary<string, PropertySpec> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<ParameterSpec> ConstructorParameters { get; } = new();

        public string? InitExceptionMessage { get; set; }
    }
}
