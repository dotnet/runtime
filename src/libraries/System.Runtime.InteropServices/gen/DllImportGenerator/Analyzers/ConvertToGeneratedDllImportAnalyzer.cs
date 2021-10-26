// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertToGeneratedDllImportAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Interoperability";

        public static readonly DiagnosticDescriptor ConvertToGeneratedDllImport =
            new DiagnosticDescriptor(
                Ids.ConvertToGeneratedDllImport,
                GetResourceString(nameof(Resources.ConvertToGeneratedDllImportTitle)),
                GetResourceString(nameof(Resources.ConvertToGeneratedDllImportMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: false,
                description: GetResourceString(nameof(Resources.ConvertToGeneratedDllImportDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ConvertToGeneratedDllImport);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(
                compilationContext =>
                {
                    // Nothing to do if the GeneratedDllImportAttribute is not in the compilation
                    INamedTypeSymbol? generatedDllImportAttrType = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.GeneratedDllImportAttribute);
                    if (generatedDllImportAttrType == null)
                        return;

                    INamedTypeSymbol? dllImportAttrType = compilationContext.Compilation.GetTypeByMetadataName(typeof(DllImportAttribute).FullName);
                    if (dllImportAttrType == null)
                        return;

                    compilationContext.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dllImportAttrType), SymbolKind.Method);
                });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol dllImportAttrType)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Check if method is a DllImport
            DllImportData? dllImportData = method.GetDllImportData();
            if (dllImportData == null)
                return;

            // Ignore QCalls
            if (dllImportData.ModuleName == "QCall")
                return;

            context.ReportDiagnostic(method.CreateDiagnostic(ConvertToGeneratedDllImport, method.Name));
        }
    }
}
