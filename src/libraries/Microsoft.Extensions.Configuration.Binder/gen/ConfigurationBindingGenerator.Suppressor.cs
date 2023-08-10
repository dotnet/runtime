// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        /// <summary>
        /// Supresses false-positive diagnostics emitted by the linker analyzers
        /// when analyzing binding invocations that we have substituted.Workaround
        /// for https://github.com/dotnet/roslyn/issues/68669.
        /// </summary>
        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class Suppressor : DiagnosticSuppressor
        {
            private const string Justification = "The target method has been intercepted by a generated static variant.";

            private static readonly SuppressionDescriptor RUCDiagnostic = new(id: "SYSLIBSUPPRESS0002", suppressedDiagnosticId: "IL2026", Justification);

            private static readonly SuppressionDescriptor RDCDiagnostic = new(id: "SYSLIBSUPPRESS0003", suppressedDiagnosticId: "IL3050", Justification);

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(RUCDiagnostic, RDCDiagnostic);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
                {
                    string diagnosticId = diagnostic.Id;

                    if (diagnosticId != RDCDiagnostic.SuppressedDiagnosticId && diagnosticId != RUCDiagnostic.SuppressedDiagnosticId)
                    {
                        continue;
                    }

                    Location location = diagnostic.AdditionalLocations.Count > 0
                        ? diagnostic.AdditionalLocations[0]
                        : diagnostic.Location;

                    bool shouldSuppressDiagnostic =
                        location.SourceTree is SyntaxTree sourceTree &&
                        sourceTree.GetRoot().FindNode(location.SourceSpan) is SyntaxNode syntaxNode &&
                        BinderInvocation.IsCandidateSyntaxNode(syntaxNode) &&
                        context.GetSemanticModel(sourceTree)
                            .GetOperation((InvocationExpressionSyntax)syntaxNode, context.CancellationToken) is IInvocationOperation operation &&
                        BinderInvocation.IsBindingOperation(operation);

                    if (shouldSuppressDiagnostic)
                    {
                        SuppressionDescriptor targetSuppression = diagnosticId == RUCDiagnostic.SuppressedDiagnosticId
                                ? RUCDiagnostic
                                : RDCDiagnostic;
                        context.ReportSuppression(Suppression.Create(targetSuppression, diagnostic));
                    }
                }
            }
        }
    }
}
