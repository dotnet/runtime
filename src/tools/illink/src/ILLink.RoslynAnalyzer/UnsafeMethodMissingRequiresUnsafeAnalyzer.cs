// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    [DiagnosticAnalyzer (LanguageNames.CSharp)]
    public sealed class UnsafeMethodMissingRequiresUnsafeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_pointerRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnsafeMethodMissingRequiresUnsafe, diagnosticSeverity: DiagnosticSeverity.Info);
        private static readonly DiagnosticDescriptor s_externLibraryImportRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ExternMethodMissingRequiresUnsafe, diagnosticSeverity: DiagnosticSeverity.Info);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_pointerRule, s_externLibraryImportRule];

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

            if (method.HasAttribute (RequiresUnsafeAnalyzer.RequiresUnsafeAttributeName))
                return;

            // For property/indexer accessors, check the containing property instead
            if (method.AssociatedSymbol is IPropertySymbol property
                && property.HasAttribute (RequiresUnsafeAnalyzer.RequiresUnsafeAttributeName))
                return;

            if (HasPointerInSignature (method)) {
                foreach (var location in method.Locations) {
                    context.ReportDiagnostic (Diagnostic.Create (s_pointerRule, location, method.GetDisplayName ()));
                }
                return;
            }

            if (IsExternOrLibraryImportMethod (method)) {
                foreach (var location in method.Locations) {
                    context.ReportDiagnostic (Diagnostic.Create (s_externLibraryImportRule, location, method.GetDisplayName ()));
                }
            }
        }

        private static bool HasPointerInSignature (IMethodSymbol method)
        {
            if (IsPointerType (method.ReturnType))
                return true;

            foreach (var param in method.Parameters) {
                if (IsPointerType(param.Type))
                    return true;
            }

            return false;
        }

        private static bool IsPointerType (ITypeSymbol type) => type is IPointerTypeSymbol or IFunctionPointerTypeSymbol;

        private static bool IsExternOrLibraryImportMethod (IMethodSymbol method)
        {
            if (method.IsExtern) {
                // Consider all InternalCall methods as not requiring unsafe today.
                // We might revisit this in the future.
                foreach (AttributeData? attr in method.GetAttributes ()) {
                    if (attr.AttributeClass?.HasName ("System.Runtime.CompilerServices.MethodImplAttribute") == true) {
                        foreach (TypedConstant arg in attr.ConstructorArguments) {
                            // MethodImplAttribute has two ctors: one taking MethodImplOptions (int enum) and
                            // one taking short (legacy). Normalize both to int before testing the bitmask.
                            int? value = arg.Value switch {
                                int i => i,
                                short s => (int)s,
                                _ => null
                            };
                            if (value.HasValue && (value.Value & (int)MethodImplOptions.InternalCall) != 0)
                                return false;
                        }
                    }
                }
                return true;
            }

            // Since all [LibraryImport] methods are partial, we can check if the method is partial
            // before looking for the attribute to avoid unnecessary attribute lookups on non-partial methods.
            if (method.IsPartialDefinition) {
                foreach (AttributeData? attr in method.GetAttributes ()) {
                    if (attr.AttributeClass?.HasName ("System.Runtime.InteropServices.LibraryImportAttribute") == true)
                        return true;
                }
            }

            return false;
        }
    }
}
#endif
