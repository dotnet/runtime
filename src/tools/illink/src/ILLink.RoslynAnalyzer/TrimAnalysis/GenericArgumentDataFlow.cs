// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using ILLink.Shared;
using ILLink.RoslynAnalyzer.DataFlow;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
    internal readonly struct GenericArgumentDataFlow
    {
        private readonly RequiresAnalyzerBase? _analyzer;
        private readonly FeatureContext _featureContext;
        private readonly TypeNameResolver _typeNameResolver;
        private readonly ISymbol _owningSymbol;
        private readonly Location _location;
        private readonly Action<Diagnostic>? _reportDiagnostic;

        public GenericArgumentDataFlow(
            RequiresAnalyzerBase? analyzer,
            FeatureContext featureContext,
            TypeNameResolver typeNameResolver,
            ISymbol owningSymbol,
            Location location,
            Action<Diagnostic>? reportDiagnostic)
        {
            _analyzer = analyzer;
            _featureContext = featureContext;
            _typeNameResolver = typeNameResolver;
            _owningSymbol = owningSymbol;
            _location = location;
            _reportDiagnostic = reportDiagnostic;
        }

        public void ProcessGenericArgumentDataFlow(INamedTypeSymbol type)
        {
            while (type is { IsGenericType: true })
            {
                ProcessGenericArgumentDataFlow(type.TypeArguments, type.TypeParameters);
                type = type.ContainingType;
            }
        }

        public void ProcessGenericArgumentDataFlow(IMethodSymbol method)
        {
            ProcessGenericArgumentDataFlow(method.TypeArguments, method.TypeParameters);

            ProcessGenericArgumentDataFlow(method.ContainingType);
        }

        public void ProcessGenericArgumentDataFlow(IFieldSymbol field)
        {
            ProcessGenericArgumentDataFlow(field.ContainingType);
        }

        public void ProcessGenericArgumentDataFlow(IPropertySymbol property)
        {
            ProcessGenericArgumentDataFlow(property.ContainingType);
        }

        private void ProcessGenericArgumentDataFlow(
            ImmutableArray<ITypeSymbol> typeArguments,
            ImmutableArray<ITypeParameterSymbol> typeParameters)
        {
            for (int i = 0; i < typeArguments.Length; i++)
            {
                var typeArgument = typeArguments[i];
                var typeParameter = typeParameters[i];

                _analyzer?.ProcessGenericInstantiation(
                    typeArgument,
                    typeParameter,
                    _featureContext,
                    _typeNameResolver,
                    _owningSymbol,
                    _location,
                    _reportDiagnostic);

                // Recursively process generic argument data flow on the generic argument if it itself is generic
                if (typeArgument is INamedTypeSymbol namedTypeArgument && namedTypeArgument.IsGenericType)
                    ProcessGenericArgumentDataFlow(namedTypeArgument);
            }
        }

        public static bool RequiresGenericArgumentDataFlow(INamedTypeSymbol type)
        {
            while (type is { IsGenericType: true })
            {
                if (RequiresGenericArgumentDataFlow(type.TypeParameters))
                    return true;

                foreach (var typeArgument in type.TypeArguments)
                {
                    if (typeArgument is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType
                        && RequiresGenericArgumentDataFlow(namedTypeSymbol))
                        return true;
                }

                type = type.ContainingType;
            }

            return false;
        }

        public static bool RequiresGenericArgumentDataFlow(IMethodSymbol method)
        {
            if (method.IsGenericMethod)
            {
                if (RequiresGenericArgumentDataFlow(method.TypeParameters))
                    return true;

                foreach (var typeArgument in method.TypeArguments)
                {
                    if (typeArgument is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType
                        && RequiresGenericArgumentDataFlow(namedTypeSymbol))
                        return true;
                }
            }

            return RequiresGenericArgumentDataFlow(method.ContainingType);
        }

        public static bool RequiresGenericArgumentDataFlow(IFieldSymbol field)
        {
            return RequiresGenericArgumentDataFlow(field.ContainingType);
        }

        public static bool RequiresGenericArgumentDataFlow(IPropertySymbol property)
        {
            return RequiresGenericArgumentDataFlow(property.ContainingType);
        }

        private static bool RequiresGenericArgumentDataFlow(ImmutableArray<ITypeParameterSymbol> typeParameters)
        {
            foreach (var typeParameter in typeParameters)
            {
                if (typeParameter.HasConstructorConstraint)
                    return true;

                var genericParameterValue = new GenericParameterValue(typeParameter);
                if (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                    return true;
            }

            return false;
        }
    }
}
