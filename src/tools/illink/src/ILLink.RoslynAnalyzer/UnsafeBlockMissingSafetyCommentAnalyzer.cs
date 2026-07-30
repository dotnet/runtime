// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Reports <c>IL5009</c> for an <c>unsafe</c> region that has no <c>// SAFETY:</c> comment explaining how its
    /// obligations are discharged.
    /// </summary>
    /// <remarks>
    /// <c>IL5005</c> covers the signature side of the contract, where an <c>unsafe</c> member documents what it
    /// asks of its callers. This covers the other side: an <c>unsafe</c> region is where those obligations are
    /// actually discharged, and the reasoning is invisible unless it is written down. The convention follows
    /// <see href="https://std-dev-guide.rust-lang.org/policy/safety-comments.html">Rust's safety comments</see>,
    /// which the speclet recommends. The diagnostic is disabled by default while this migration tooling remains
    /// experimental.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnsafeBlockMissingSafetyCommentAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Matches the <c>SAFETY:</c> marker anywhere inside a comment.
        /// </summary>
        /// <remarks>
        /// The marker is matched as a whole word so that <c>// UNSAFETY:</c> or <c>// SAFETYNET</c> do not count,
        /// and it is case-sensitive to keep the convention greppable across a code base. Matching anywhere in the
        /// comment allows the marker to sit on an inner line of a block comment.
        /// </remarks>
        private static readonly Regex s_safetyComment = new(@"\bSAFETY\s*:", RegexOptions.CultureInvariant);

        private static readonly DiagnosticDescriptor s_rule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(
                DiagnosticId.UnsafeBlockMissingSafetyComment,
                isEnabledByDefault: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            if (!System.Diagnostics.Debugger.IsAttached)
                context.EnableConcurrentExecution();

            // 'unsafe' expressions are newer than the Roslyn this analyzer compiles against, so both forms are
            // matched on the keyword token rather than on a node type.
            context.RegisterSyntaxNodeAction(AnalyzeUnsafeStatement, SyntaxKind.UnsafeStatement);
            context.RegisterSyntaxTreeAction(AnalyzeUnsafeExpressions);
        }

        private static void AnalyzeUnsafeStatement(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode unsafeStatement = context.Node;

            // A nested region inherits the reasoning of the one that already documents it.
            if (IsNestedInUnsafeRegion(unsafeStatement)
                || HasSafetyComment(unsafeStatement.GetFirstToken()))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, unsafeStatement.GetFirstToken().GetLocation()));
        }

        private static void AnalyzeUnsafeExpressions(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
            foreach (SyntaxToken token in root.DescendantTokens())
            {
                if (!UnsafeMigrationSyntaxHelpers.IsUnsafeExpressionKeyword(token)
                    || token.Parent is null
                    || IsNestedInUnsafeRegion(token.Parent)
                    || HasSafetyComment(token))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(s_rule, token.GetLocation()));
            }
        }

        /// <summary>
        /// Determines whether a region sits inside another <c>unsafe</c> region, whose comment already covers it.
        /// </summary>
        private static bool IsNestedInUnsafeRegion(SyntaxNode region) =>
            region.Ancestors().Any(static ancestor =>
                ancestor.IsKind(SyntaxKind.UnsafeStatement)
                || UnsafeMigrationSyntaxHelpers.IsUnsafeExpression(ancestor));

        /// <summary>
        /// Looks for a <c>// SAFETY:</c> comment attached to the region or to the statement that contains it.
        /// </summary>
        /// <remarks>
        /// An <c>unsafe</c> expression sits inside a larger statement, so its comment is normally written above
        /// that statement rather than immediately before the keyword.
        /// </remarks>
        private static bool HasSafetyComment(SyntaxToken unsafeKeyword)
        {
            if (ContainsSafetyComment(unsafeKeyword.LeadingTrivia))
                return true;

            for (SyntaxNode? node = unsafeKeyword.Parent; node is not null; node = node.Parent)
            {
                if (ContainsSafetyComment(node.GetLeadingTrivia()))
                    return true;

                // Stop at the statement or member that owns the region; trivia further out documents something else.
                if (node is StatementSyntax or MemberDeclarationSyntax)
                    break;
            }

            return false;
        }

        private static bool ContainsSafetyComment(SyntaxTriviaList trivia) =>
            trivia.Any(static t =>
                (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                && s_safetyComment.IsMatch(t.ToString()));
    }
}
#endif
