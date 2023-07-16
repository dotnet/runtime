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

        private string _displayStringWithoutSpecialCharacters;
        public string DisplayStringWithoutSpecialCharacters =>
            _displayStringWithoutSpecialCharacters ??= $"{MinimalDisplayString.Replace(".", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty)}";

        public override bool NeedsMemberBinding => CanInitialize &&
            Properties.Values.Count > 0 &&
            Properties.Values.Any(p => p.ShouldBind());
    }
}
