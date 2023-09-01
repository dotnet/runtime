// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ObjectSpec : TypeSpec
    {
        public ObjectSpec(INamedTypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.Object;

        public override InitializationStrategy InitializationStrategy { get; set; }

        public override bool CanInitialize => CanInitComplexObject();

        public Dictionary<string, PropertySpec> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<ParameterSpec> ConstructorParameters { get; } = new();

        public override bool NeedsMemberBinding => Properties.Values.Any(p => p.ShouldBind());
    }
}
