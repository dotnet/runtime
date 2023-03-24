// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public sealed record MethodSignatureDiagnosticLocations(string MethodIdentifier, ImmutableArray<Location> ManagedParameterLocations, Location FallbackLocation)
    {
        public MethodSignatureDiagnosticLocations(MethodDeclarationSyntax syntax)
            : this(syntax.Identifier.Text, syntax.ParameterList.Parameters.Select(p => p.Identifier.GetLocation()).ToImmutableArray(), syntax.Identifier.GetLocation())
        {
        }

        public bool Equals(MethodSignatureDiagnosticLocations other)
        {
            return MethodIdentifier == other.MethodIdentifier
                && ManagedParameterLocations.SequenceEqual(other.ManagedParameterLocations)
                && FallbackLocation.Equals(other.FallbackLocation);
        }

        public override int GetHashCode() => throw new UnreachableException();
    }
}
