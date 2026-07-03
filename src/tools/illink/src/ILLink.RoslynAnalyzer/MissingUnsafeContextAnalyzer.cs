// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Surfaces operations that require an <c>unsafe</c> context under the updated memory-safety rules as
    /// IL5006 so the migration code fix can wrap them. It re-reports the compiler's own missing-unsafe-context
    /// diagnostics (CS9360/CS9362/CS9363) rather than re-implementing the language rules, and additionally
    /// detects the <c>stackalloc</c>-to-<c>Span</c> case (CS9361) directly because that diagnostic is not always
    /// present in the semantic model a code-analysis workspace sees.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MissingUnsafeContextAnalyzer : DiagnosticAnalyzer
    {
        internal const string CompilerDiagnosticIdProperty = "CompilerDiagnosticId";

        private static readonly DiagnosticDescriptor s_rule = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.OperationRequiresUnsafeContext);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(static context =>
            {
                if (!context.Options.IsMSBuildPropertyValueTrue(MSBuildPropertyOptionNames.EnableUnsafeAnalyzer))
                    return;

                context.RegisterSemanticModelAction(AnalyzeSemanticModel);
                context.RegisterSyntaxNodeAction(AnalyzeStackAlloc,
                    SyntaxKind.StackAllocArrayCreationExpression, SyntaxKind.ImplicitStackAllocArrayCreationExpression);
            });
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            foreach (var diagnostic in context.SemanticModel.GetDiagnostics(cancellationToken: context.CancellationToken))
            {
                if (Array.IndexOf(UnsafeMigrationFacts.MissingUnsafeContextDiagnosticIds, diagnostic.Id) < 0)
                    continue;

                var properties = ImmutableDictionary<string, string?>.Empty.Add(CompilerDiagnosticIdProperty, diagnostic.Id);
                context.ReportDiagnostic(Diagnostic.Create(s_rule, diagnostic.Location, properties));
            }
        }

        // A stackalloc converted to Span/ReadOnlySpan without an initializer requires an unsafe context when
        // SkipLocalsInit is in effect (CS9361).
        private static void AnalyzeStackAlloc(SyntaxNodeAnalysisContext context)
        {
            if (!UnsafeMigrationFacts.UsesUpdatedMemorySafetyRules(context.Node.SyntaxTree))
                return;

            var initializer = context.Node switch
            {
                StackAllocArrayCreationExpressionSyntax explicitStackalloc => explicitStackalloc.Initializer,
                ImplicitStackAllocArrayCreationExpressionSyntax implicitStackalloc => implicitStackalloc.Initializer,
                _ => null,
            };
            if (initializer is not null || context.Node.IsInUnsafeContext())
                return;

            if (!UnsafeMigrationFacts.IsSpanType(context.SemanticModel.GetTypeInfo(context.Node, context.CancellationToken).ConvertedType))
                return;

            if (!UnsafeMigrationFacts.HasSkipLocalsInit(context.ContainingSymbol, context.Compilation))
                return;

            context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Node.GetLocation()));
        }
    }
}
#endif
