// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComHostingDoesNotSupportGeneratedComInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ComHostingDoesNotSupportGeneratedComInterface);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.EnableComHosting", out string? enableComHosting)
                    || !bool.TryParse(enableComHosting, out bool enableComHostingValue)
                    || !enableComHostingValue)
                {
                    return;
                }

                INamedTypeSymbol? generatedComClassAttribute = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComClassAttribute);
                INamedTypeSymbol? generatedComInterfaceAttribute = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute);
                INamedTypeSymbol? comVisibleAttribute = context.Compilation.GetBestTypeByMetadataName(TypeNames.System_Runtime_InteropServices_ComVisibleAttribute)!;

                if (generatedComClassAttribute is null || generatedComInterfaceAttribute is null || comVisibleAttribute is null)
                {
                    return;
                }

                context.RegisterOperationAction(context =>
                {
                    IAttributeOperation attr = (IAttributeOperation)context.Operation;
                    if (attr.Operation is not IObjectCreationOperation ctor
                        || !comVisibleAttribute.Equals(ctor.Type, SymbolEqualityComparer.Default)
                        || ctor.Arguments[0].Value.ConstantValue.Value is not true)
                    {
                        return;
                    }

                    INamedTypeSymbol containingType = (INamedTypeSymbol)context.ContainingSymbol;

                    if (containingType.GetAttributes().Any(attr => generatedComClassAttribute.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)))
                    {
                        context.ReportDiagnostic(context.ContainingSymbol.CreateDiagnostic(ComHostingDoesNotSupportGeneratedComInterface, context.ContainingSymbol.Name));
                        return;
                    }

                    foreach (var iface in containingType.AllInterfaces)
                    {
                        if (iface.GetAttributes().Any(attr => generatedComInterfaceAttribute.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)))
                        {
                            context.ReportDiagnostic(context.ContainingSymbol.CreateDiagnostic(ComHostingDoesNotSupportGeneratedComInterface, context.ContainingSymbol.Name));
                            return;
                        }
                    }
                }, OperationKind.Attribute);
            });
        }
    }
}
