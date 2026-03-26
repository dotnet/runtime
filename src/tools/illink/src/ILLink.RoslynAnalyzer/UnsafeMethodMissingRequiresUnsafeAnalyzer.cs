// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    [DiagnosticAnalyzer (LanguageNames.CSharp)]
    public sealed class UnsafeMethodMissingRequiresUnsafeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnsafeMethodMissingRequiresUnsafe, diagnosticSeverity: DiagnosticSeverity.Info);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_rule);

        public override void Initialize (AnalysisContext context)
        {
            context.EnableConcurrentExecution ();
            context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction (context => {
                if (!context.Options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableUnsafeAnalyzer))
                    return;

                if (context.Compilation.GetTypeByMetadataName (RequiresUnsafeAnalyzer.FullyQualifiedRequiresUnsafeAttribute) is null)
                    return;

                context.RegisterSymbolAction (
                    AnalyzeMethod,
                    SymbolKind.Method);
            });
        }

        private static void AnalyzeMethod (SymbolAnalysisContext context)
        {
            if (context.Symbol is not IMethodSymbol method)
                return;

            if (!HasPointerInSignature (method))
                return;

            if (method.HasAttribute (RequiresUnsafeAnalyzer.RequiresUnsafeAttributeName))
                return;

            // For property/indexer accessors, check the containing property instead
            if (method.AssociatedSymbol is IPropertySymbol property
                && property.HasAttribute (RequiresUnsafeAnalyzer.RequiresUnsafeAttributeName))
                return;

            foreach (var location in method.Locations) {
                context.ReportDiagnostic (Diagnostic.Create (s_rule, location, method.GetDisplayName ()));
            }
        }

        private static bool HasPointerInSignature (IMethodSymbol method)
        {
            if (IsPointerType (method.ReturnType))
                return true;

            foreach (var param in method.Parameters) {
                if (IsPointerType (param.Type))
                    return true;
            }

            return false;
        }

        private static bool IsPointerType (ITypeSymbol type) => type is IPointerTypeSymbol or IFunctionPointerTypeSymbol;
    }
}
#endif
