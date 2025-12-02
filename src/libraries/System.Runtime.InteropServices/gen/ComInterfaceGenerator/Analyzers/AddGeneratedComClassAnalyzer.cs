// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddGeneratedComClassAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AddGeneratedComClassAttribute);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(context =>
            {
                var generatedComClassAttributeType = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComClassAttribute);
                var generatedComInterfaceAttributeType = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute);

                if (generatedComClassAttributeType is null || generatedComInterfaceAttributeType is null)
                {
                    return;
                }

                context.RegisterSymbolAction(context =>
                {
                    INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
                    if (type.GetAttributes().Any(attr => generatedComClassAttributeType.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)))
                    {
                        return;
                    }

                    // Only direct people to put the GeneratedComClassAttribute on classes.
                    if (type.TypeKind != TypeKind.Class)
                    {
                        return;
                    }

                    foreach (var iface in type.AllInterfaces)
                    {
                        if (iface.GetAttributes().Any(attr => generatedComInterfaceAttributeType.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)))
                        {
                            context.ReportDiagnostic(type.CreateDiagnostic(AddGeneratedComClassAttribute, type.Name));
                            return;
                        }
                    }

                    if (type.BaseType is not null
                        && type.BaseType.GetAttributes().Any(attr => generatedComClassAttributeType.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)))
                    {
                        context.ReportDiagnostic(type.CreateDiagnostic(AddGeneratedComClassAttribute, type.Name));
                    }
                }, SymbolKind.NamedType);
            });
        }
    }
}
