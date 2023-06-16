// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public interface IDiagnosticDescriptorProvider
    {
        DiagnosticDescriptor InvalidMarshallingAttributeInfo { get; }
        DiagnosticDescriptor GetConfigurationNotSupportedDescriptor(bool withValue);
        DiagnosticDescriptor GetDescriptor(GeneratorDiagnostic diagnostic);
    }

    public interface ISignatureDiagnosticLocations
    {
        DiagnosticInfo CreateDiagnosticInfo(IDiagnosticDescriptorProvider descriptorProvider, GeneratorDiagnostic diagnostic);
    }

    public sealed record MethodSignatureDiagnosticLocations(string MethodIdentifier, ImmutableArray<Location> ManagedParameterLocations, Location FallbackLocation) : ISignatureDiagnosticLocations
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

        public DiagnosticInfo CreateDiagnosticInfo(IDiagnosticDescriptorProvider descriptorProvider, GeneratorDiagnostic diagnostic)
        {
            DiagnosticDescriptor descriptor = descriptorProvider.GetDescriptor(diagnostic);
            var (location, elementName) = diagnostic.TypePositionInfo switch
            {
                { ManagedIndex: >= 0 and int index, InstanceIdentifier: string identifier } => (ManagedParameterLocations[index], identifier),
                _ => (FallbackLocation, MethodIdentifier),
            };
            return diagnostic.ToDiagnosticInfo(descriptor, location, elementName);
        }
    }
}
