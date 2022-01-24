// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Text.RegularExpressions.Generator
{
    // TODO: Delete once https://github.com/dotnet/csharplang/issues/5528 is addressed.

    // RegexGenerator injects RegexGeneratorAttribute as internal and conditional into the assembly
    // for use by code in the assembly.  However, if one assembly using RegexGenerator has
    // InternalsVisibleTo another assembly using RegexGenerator, a CS0436 warning will be emitted
    // highlighting the duplication.  This suppresses the benign warning.

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CS0436DiagnosticSuppressor : DiagnosticSuppressor
    {
        private const string AttributeName = "System.Text.RegularExpressions.RegexGeneratorAttribute";
        private const string SuppressedId = "CS0436";
        private const string Id = "RGDS0001";
        private const string Justification = "https://github.com/dotnet/csharplang/issues/5528";

        private static readonly SuppressionDescriptor Rule = new(Id, SuppressedId, Justification);

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create(Rule);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            // Suppress any diagnostic on a symbol whose name is the same as the target attribute.
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
            {
                if (diagnostic.Location.SourceTree is SyntaxTree tree)
                {
                    SyntaxNode node = tree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                    if (context.GetSemanticModel(tree).GetDeclaredSymbol(node, context.CancellationToken)?.Name == AttributeName)
                    {
                        context.ReportSuppression(Suppression.Create(Rule, diagnostic));
                    }
                }
            }
        }
    }
}
