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
    internal static class GenericArgumentDataFlow
    {
        public static void ProcessGenericArgumentDataFlow(DataFlowAnalyzerContext context, FeatureContext featureContext, TypeNameResolver typeNameResolver, ISymbol owningSymbol, Location location, INamedTypeSymbol type, Action<Diagnostic>? reportDiagnostic)
        {
            while (type is { IsGenericType: true })
            {
                ProcessGenericArgumentDataFlow(context, featureContext, typeNameResolver, owningSymbol, location, type.TypeArguments, type.TypeParameters, reportDiagnostic);
                type = type.ContainingType;
            }
        }

        public static void ProcessGenericArgumentDataFlow(DataFlowAnalyzerContext context, FeatureContext featureContext, TypeNameResolver typeNameResolver, ISymbol owningSymbol, Location location, IMethodSymbol method, Action<Diagnostic>? reportDiagnostic)
        {
            ProcessGenericArgumentDataFlow(context, featureContext, typeNameResolver, owningSymbol, location, method.TypeArguments, method.TypeParameters, reportDiagnostic);

            ProcessGenericArgumentDataFlow(context, featureContext, typeNameResolver, owningSymbol, location, method.ContainingType, reportDiagnostic);
        }

        public static void ProcessGenericArgumentDataFlow(DataFlowAnalyzerContext context, FeatureContext featureContext, TypeNameResolver typeNameResolver, ISymbol owningSymbol, Location location, IFieldSymbol field, Action<Diagnostic>? reportDiagnostic)
        {
            ProcessGenericArgumentDataFlow(context, featureContext, typeNameResolver, owningSymbol, location, field.ContainingType, reportDiagnostic);
        }

        public static void ProcessGenericArgumentDataFlow(DataFlowAnalyzerContext context, FeatureContext featureContext, TypeNameResolver typeNameResolver, ISymbol owningSymbol, Location location, IPropertySymbol property, Action<Diagnostic> reportDiagnostic)
        {
            ProcessGenericArgumentDataFlow(context, featureContext, typeNameResolver, owningSymbol, location, property.ContainingType, reportDiagnostic);
        }

        private static void ProcessGenericArgumentDataFlow(
            DataFlowAnalyzerContext context,
            FeatureContext featureContext,
            TypeNameResolver typeNameResolver,
            ISymbol owningSymbol,
            Location location,
            ImmutableArray<ITypeSymbol> typeArguments,
            ImmutableArray<ITypeParameterSymbol> typeParameters,
            Action<Diagnostic>? reportDiagnostic)
        {
            var diagnosticContext = new DiagnosticContext(location, reportDiagnostic);
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
                        foreach (var analyzer in context.EnabledRequiresAnalyzers)
                        {
                            var attrName = analyzer.RequiresAttributeFullyQualifiedName;

                            if (featureContext.IsEnabled(attrName))
                                continue;

                            analyzer.CheckAndCreateRequiresDiagnostic(paramlessPublicCtor, owningSymbol, incompatibleMembers: ImmutableArray<ISymbol>.Empty, diagnosticContext);
                        }
                    }
                }

                // TODO: avoid duplicate warnings for new() and DAMT.PublicParameterlessConstructor.

                // Apply annotations to the generic argument
                var genericParameterValue = new GenericParameterValue(typeParameter);
                if (context.EnableTrimAnalyzer &&
                    !owningSymbol.IsInRequiresUnreferencedCodeAttributeScope(out _) &&
                    !featureContext.IsEnabled(RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute) &&
                    genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                {
                    SingleValue genericArgumentValue = SingleValueExtensions.FromTypeSymbol(typeArgument)!;
                    var reflectionAccessAnalyzer = new ReflectionAccessAnalyzer(reportDiagnostic, typeNameResolver, typeHierarchyType: null);
                    var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(context, featureContext, typeNameResolver, location, reportDiagnostic, reflectionAccessAnalyzer, owningSymbol);
                    requireDynamicallyAccessedMembersAction.Invoke(genericArgumentValue, genericParameterValue);
                }

                // Recursively process generic argument data flow on the generic argument if it itself is generic
                if (typeArgument is INamedTypeSymbol namedTypeArgument && namedTypeArgument.IsGenericType)
                    ProcessGenericArgumentDataFlow(context, featureContext, typeNameResolver, owningSymbol, location, namedTypeArgument, reportDiagnostic);
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
