// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GeneratedDllImportAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Usage";

        public static readonly DiagnosticDescriptor GeneratedDllImportMissingModifiers =
            new DiagnosticDescriptor(
                Ids.GeneratedDllImportMissingRequiredModifiers,
                GetResourceString(nameof(Resources.GeneratedDllImportMissingModifiersTitle)),
                GetResourceString(nameof(Resources.GeneratedDllImportMissingModifiersMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.GeneratedDllImportMissingModifiersDescription)));

        public static readonly DiagnosticDescriptor GeneratedDllImportContainingTypeMissingModifiers =
            new DiagnosticDescriptor(
                Ids.GeneratedDllImportContaiingTypeMissingRequiredModifiers,
                GetResourceString(nameof(Resources.GeneratedDllImportContainingTypeMissingModifiersTitle)),
                GetResourceString(nameof(Resources.GeneratedDllImportContainingTypeMissingModifiersMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.GeneratedDllImportContainingTypeMissingModifiersDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(GeneratedDllImportMissingModifiers, GeneratedDllImportContainingTypeMissingModifiers);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(
                compilationContext =>
                {
                    INamedTypeSymbol? generatedDllImportAttributeType = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.GeneratedDllImportAttribute);
                    if (generatedDllImportAttributeType == null)
                        return;

                    compilationContext.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, generatedDllImportAttributeType), SymbolKind.Method);
                });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol generatedDllImportAttributeType)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;

            // Check if method is marked with GeneratedDllImportAttribute
            ImmutableArray<AttributeData> attributes = methodSymbol.GetAttributes();
            if (!attributes.Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatedDllImportAttributeType)))
                return;

            if (!methodSymbol.IsStatic)
            {
                // Must be marked static
                context.ReportDiagnostic(methodSymbol.CreateDiagnostic(GeneratedDllImportMissingModifiers, methodSymbol.Name));
            }
            else
            {
                // Make sure declarations are marked partial. Technically, we can just check one
                // declaration, since Roslyn would error on inconsistent partial declarations.
                foreach (SyntaxReference reference in methodSymbol.DeclaringSyntaxReferences)
                {
                    SyntaxNode syntax = reference.GetSyntax(context.CancellationToken);
                    if (syntax is MethodDeclarationSyntax methodSyntax && !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        // Must be marked partial
                        context.ReportDiagnostic(methodSymbol.CreateDiagnostic(GeneratedDllImportMissingModifiers, methodSymbol.Name));
                        break;
                    }
                }

                for (INamedTypeSymbol? typeSymbol = methodSymbol.ContainingType; typeSymbol is not null; typeSymbol = typeSymbol.ContainingType)
                {
                    foreach (SyntaxReference reference in typeSymbol.DeclaringSyntaxReferences)
                    {
                        SyntaxNode syntax = reference.GetSyntax(context.CancellationToken);
                        if (syntax is TypeDeclarationSyntax typeSyntax && !typeSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                        {
                            // Must be marked partial
                            context.ReportDiagnostic(typeSymbol.CreateDiagnostic(GeneratedDllImportContainingTypeMissingModifiers, typeSymbol.Name));
                            break;
                        }
                    }
                }
            }
        }
    }
}
