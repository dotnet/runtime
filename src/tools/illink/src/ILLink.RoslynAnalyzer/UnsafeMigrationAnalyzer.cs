// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnsafeMigrationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_modifierMigration = DiagnosticDescriptors.GetDiagnosticDescriptor(
            DiagnosticId.UnsafeModifierMigration,
            diagnosticSeverity: DiagnosticSeverity.Info);

        private static readonly DiagnosticDescriptor s_usageMigration = DiagnosticDescriptors.GetDiagnosticDescriptor(
            DiagnosticId.UnsafeUsageMigration,
            diagnosticSeverity: DiagnosticSeverity.Info);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_modifierMigration, s_usageMigration];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(static context =>
            {
                if (!context.Options.IsMSBuildPropertyValueTrue(MSBuildPropertyOptionNames.EnableUnsafeMigration))
                    return;

                context.RegisterSemanticModelAction(AnalyzeSemanticModel);
            });
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var removals = UnsafeMigrationAnalysis.GetModifierRemovals(context.SemanticModel, context.CancellationToken);
            if (!removals.IsEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_modifierMigration,
                    removals[0].Modifier.GetLocation()));
            }

            var declarationUpdates = UnsafeMigrationAnalysis.GetDeclarationUpdates(context.SemanticModel, context.CancellationToken);
            if (!declarationUpdates.IsEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_usageMigration,
                    declarationUpdates[0].Declaration.GetLocation()));
                return;
            }

            bool skipLocalsInit = context.Options.IsMSBuildPropertyValueTrue(MSBuildPropertyOptionNames.SkipLocalsInit);
            var operationLocations = UnsafeMigrationAnalysis.GetUnsafeOperationLocations(
                context.SemanticModel,
                skipLocalsInit,
                includeCompilerDiagnostics: true,
                context.CancellationToken);
            if (!operationLocations.IsEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_usageMigration,
                    operationLocations[0]));
            }
        }
    }
}
#endif
