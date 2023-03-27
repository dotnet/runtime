// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal record TypeSpec
    {
        public TypeSpec(ITypeSymbol type)
        {
            DisplayString = type.ToDisplayString();
            SpecialType = type.SpecialType;
            IsValueType = type.IsValueType;
        }

        public string DisplayString { get; }

        public SpecialType SpecialType { get; }

        public bool IsValueType { get; }

        public bool PassToBindCoreByRef => IsValueType || SpecKind == TypeSpecKind.Array;

        public virtual TypeSpecKind SpecKind { get; init; }

        public virtual ConstructionStrategy ConstructionStrategy { get; init; }

        /// <summary>
        /// Where in the input compilation we picked up a call to Bind, Get, or Configure.
        /// </summary>
        public required Location? Location { get; init; }
    }
}
