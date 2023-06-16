// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class SymbolDiagnosticLocations
    {
        private SymbolDiagnosticLocations(string identifier, Location location)
        {
            Identifier = identifier;
            IdentifierLocation = location;
        }

        public string Identifier { get; init; }

        public Location IdentifierLocation { get; init; }

        public required ImmutableArray<(string FullyQualifiedName, Location Location)> AttributeLocations { get; init; }

        public bool Equals(SymbolDiagnosticLocations other)
        {
            return Identifier == other.Identifier
                && AttributeLocations.SequenceEqual(other.AttributeLocations)
                && IdentifierLocation.Equals(other.IdentifierLocation);
        }

        public static SymbolDiagnosticLocations CreateForReturnType(IMethodSymbol symbol)
        {
            return new SymbolDiagnosticLocations(symbol.Name, symbol.Locations[0])
            {
                AttributeLocations = symbol.GetReturnTypeAttributes().Select(a => (a.AttributeClass.ToDisplayString(), Location.Create(a.ApplicationSyntaxReference.SyntaxTree, a.ApplicationSyntaxReference.Span))).ToImmutableArray()
            };
        }

        public static SymbolDiagnosticLocations Create(ISymbol symbol)
        {
            return new SymbolDiagnosticLocations(symbol.Name, symbol.Locations[0])
            {
                AttributeLocations = symbol.GetAttributes().Select(a => (a.AttributeClass.ToDisplayString(), Location.Create(a.ApplicationSyntaxReference.SyntaxTree, a.ApplicationSyntaxReference.Span))).ToImmutableArray()
            };
        }

        public override int GetHashCode() => throw new UnreachableException();
    }
}
