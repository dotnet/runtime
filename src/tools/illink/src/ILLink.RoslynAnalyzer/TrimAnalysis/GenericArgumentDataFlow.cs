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
        private readonly DataFlowAnalyzerContext _context;
        private readonly FeatureContext _featureContext;
        private readonly TypeNameResolver _typeNameResolver;
        private readonly ISymbol _owningSymbol;
        private readonly Location _location;
        private readonly Action<Diagnostic>? _reportDiagnostic;

        public GenericArgumentDataFlow(
            DataFlowAnalyzerContext context,
            FeatureContext featureContext,
            TypeNameResolver typeNameResolver,
            ISymbol owningSymbol,
            Location location,
            Action<Diagnostic>? reportDiagnostic)
        {
            _context = context;
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
            var diagnosticContext = new DiagnosticContext(_location, _reportDiagnostic);
            for (int i = 0; i < typeArguments.Length; i++)
            {
                var typeArgument = typeArguments[i];
                var typeParameter = typeParameters[i];

                // Process new() constraint: if present, check Requires* on the public parameterless constructor
                // And that also takes care of any DynamicallyAccessedMembers.PublicParameterlessConstructor.
                if (typeParameter.HasConstructorConstraint && typeArgument is INamedTypeSymbol namedTypeArg && namedTypeArg.InstanceConstructors.Length > 0)
                {
                    var paramlessPublicCtor = namedTypeArg.InstanceConstructors.FirstOrDefault(ctor => ctor.Parameters.IsEmpty && ctor.DeclaredAccessibility == Accessibility.Public);
                    if (paramlessPublicCtor is not null)
                    {
                        foreach (var analyzer in _context.EnabledRequiresAnalyzers)
                        {
                            var attrName = analyzer.RequiresAttributeFullyQualifiedName;

                            if (_featureContext.IsEnabled(attrName))
                                continue;

                            analyzer.CheckAndCreateRequiresDiagnostic(paramlessPublicCtor, _owningSymbol, incompatibleMembers: ImmutableArray<ISymbol>.Empty, diagnosticContext);
                        }
                    }
                }

                var parameterRequirements = typeParameter.GetDynamicallyAccessedMemberTypes();
                // Avoid duplicate warnings for new() and DAMT.PublicParameterlessConstructor
                if (typeParameter.HasConstructorConstraint)
                    parameterRequirements &= ~DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

                // Apply annotations to the generic argument
                var genericParameterValue = new GenericParameterValue(typeParameter, parameterRequirements);
                if (_context.EnableTrimAnalyzer &&
                    !_owningSymbol.IsInRequiresUnreferencedCodeAttributeScope(out _) &&
                    !_featureContext.IsEnabled(RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute) &&
                    genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                {
                    SingleValue genericArgumentValue = SingleValueExtensions.FromTypeSymbol(typeArgument)!;
                    var reflectionAccessAnalyzer = new ReflectionAccessAnalyzer(_reportDiagnostic, _typeNameResolver, typeHierarchyType: null);
                    var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(_context, _featureContext, _typeNameResolver, _location, _reportDiagnostic, reflectionAccessAnalyzer, _owningSymbol);
                    requireDynamicallyAccessedMembersAction.Invoke(genericArgumentValue, genericParameterValue);
                }

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
                var genericParameterValue = new GenericParameterValue(typeParameter);
                if (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None || typeParameter.HasConstructorConstraint)
                    return true;
            }

            return false;
        }
    }
}
