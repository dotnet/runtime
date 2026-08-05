// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
    public readonly partial struct DiagnosticContext
    {
        public readonly Location Location { get; }

        private readonly Action<Diagnostic>? _reportDiagnostic;
        private readonly Compilation _compilation;

        public DiagnosticContext(Location location, Action<Diagnostic>? reportDiagnostic, Compilation compilation)
        {
            Location = location;
            _reportDiagnostic = reportDiagnostic;
            _compilation = compilation;
        }

        private Diagnostic CreateDiagnostic(DiagnosticId id, params string[] args)
        {
            return Diagnostic.Create(DiagnosticDescriptors.GetDiagnosticDescriptor(id), Location, args);
        }

        public partial void AddDiagnostic(DiagnosticId id, params string[] args)
        {
            if (_reportDiagnostic is null)
                return;

            _reportDiagnostic(CreateDiagnostic(id, args));
        }

        public partial void AddDiagnostic(DiagnosticId id, ValueWithDynamicallyAccessedMembers actualValue, ValueWithDynamicallyAccessedMembers expectedAnnotationsValue, params string[] args)
        {
            if (_reportDiagnostic is null)
                return;

            _reportDiagnostic(CreateDiagnostic(id, actualValue, expectedAnnotationsValue, args));
        }

        private Diagnostic CreateDiagnostic(DiagnosticId id, ValueWithDynamicallyAccessedMembers actualValue, ValueWithDynamicallyAccessedMembers expectedAnnotationsValue, params string[] args)
        {
            actualValue = actualValue switch
            {
                NullableValueWithDynamicallyAccessedMembers nv => nv.UnderlyingTypeValue,
                NullableUnwrappedGenericParameterValue ng => ng.GenericParameter,
                _ => actualValue,
            };

            ISymbol symbol = actualValue switch
            {
                FieldValue field => field.FieldSymbol,
                MethodParameterValue { Parameter.IsImplicitThis: true } thisParameter => thisParameter.MethodSymbol,
                MethodParameterValue { Parameter.ParameterSymbol: { } parameterSymbol } => parameterSymbol,
                MethodReturnValue methodReturnValue => methodReturnValue.MethodSymbol,
                GenericParameterValue genericParameter => genericParameter.GenericParameter.TypeParameterSymbol,
                _ => throw new InvalidOperationException()
            };

            bool hasAttribute = actualValue is MethodReturnValue
                ? ((IMethodSymbol)symbol).TryGetReturnAttribute(
                    DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute,
                    out _)
                : symbol.TryGetAttribute(
                    DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute,
                    out _);

            Dictionary<string, string?>? properties = null;
            Location[]? additionalLocations = null;
            if (!hasAttribute && TryGetLocalDeclarationLocation(symbol, out Location declarationLocation))
            {
                properties = new Dictionary<string, string?>
                {
                    ["attributeArgument"] = expectedAnnotationsValue.DynamicallyAccessedMemberTypes.ToString(),
                };
                additionalLocations = [declarationLocation];
            }

            return Diagnostic.Create(DiagnosticDescriptors.GetDiagnosticDescriptor(id), Location, additionalLocations, properties?.ToImmutableDictionary(), args);
        }

        private bool TryGetLocalDeclarationLocation(ISymbol symbol, out Location location)
        {
            foreach (SyntaxReference syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                if (_compilation.ContainsSyntaxTree(syntaxReference.SyntaxTree))
                {
                    location = syntaxReference.GetSyntax().GetLocation();
                    return true;
                }
            }

            location = null!;
            return false;
        }
    }
}
