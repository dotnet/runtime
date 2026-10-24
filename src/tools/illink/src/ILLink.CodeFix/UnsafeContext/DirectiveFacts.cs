// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Preprocessor queries that keep new contexts within one conditional-compilation arm and avoid rewrites whose
    /// meaning depends on code in inactive arms.
    /// </summary>
    internal static class DirectiveFacts
    {
        /// <summary>Whether the directives in <paramref name="span"/> form complete <c>#if</c>/<c>#region</c> groups.</summary>
        public static bool AreBalanced(SyntaxNode root, TextSpan span)
        {
            int conditionalDepth = 0, regionDepth = 0;
            foreach (SyntaxTrivia trivia in root.DescendantTrivia(span).Where(t => t.IsDirective && span.Contains(t.Span)))
            {
                switch (trivia.Kind())
                {
                    case SyntaxKind.IfDirectiveTrivia:
                        conditionalDepth++;
                        break;
                    case SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia when conditionalDepth == 0:
                        return false;
                    case SyntaxKind.EndIfDirectiveTrivia:
                        if (--conditionalDepth < 0)
                            return false;
                        break;
                    case SyntaxKind.RegionDirectiveTrivia:
                        regionDepth++;
                        break;
                    case SyntaxKind.EndRegionDirectiveTrivia:
                        if (--regionDepth < 0)
                            return false;
                        break;
                }
            }

            return conditionalDepth == 0 && regionDepth == 0;
        }

        /// <summary>Returns the span of every <c>#if</c> ... <c>#endif</c> group in a tree.</summary>
        public static ImmutableArray<TextSpan> GetConditionalSpans(SyntaxTree tree)
        {
            var spans = ImmutableArray.CreateBuilder<TextSpan>();
            Stack<int> starts = [];
            for (DirectiveTriviaSyntax? directive = ((CSharpSyntaxNode)tree.GetRoot()).GetFirstDirective(); directive is not null; directive = directive.GetNextDirective())
            {
                if (directive.IsKind(SyntaxKind.IfDirectiveTrivia))
                    starts.Push(directive.SpanStart);
                else if (directive.IsKind(SyntaxKind.EndIfDirectiveTrivia) && starts.Count > 0)
                    spans.Add(TextSpan.FromBounds(starts.Pop(), directive.Span.End));
            }

            return spans.ToImmutable();
        }

        /// <summary>Returns the identifiers in the inactive <c>#if</c> arms of <paramref name="node"/> within <paramref name="span"/>.</summary>
        public static IEnumerable<string> GetDisabledIdentifiers(SyntaxNode node, TextSpan span) =>
            from text in GetDisabledText(node, span)
            from token in SyntaxFactory.ParseTokens(text)
            where token.IsKind(SyntaxKind.IdentifierToken)
            select token.ValueText;

        /// <summary>Returns the locals and labels declared in the inactive <c>#if</c> arms within <paramref name="span"/>.</summary>
        public static IEnumerable<string> GetDisabledDeclarations(SyntaxNode node, TextSpan span) =>
            GetDisabledText(node, span)
                .SelectMany(static text => SyntaxFactory.ParseStatement("{" + text + "\n}").DescendantNodes())
                .Select(static declaration => declaration switch
                {
                    VariableDeclaratorSyntax variable => variable.Identifier.ValueText,
                    SingleVariableDesignationSyntax designation => designation.Identifier.ValueText,
                    LabeledStatementSyntax labeled => labeled.Identifier.ValueText,
                    _ => null,
                })
                .OfType<string>();

        private static IEnumerable<string> GetDisabledText(SyntaxNode node, TextSpan span) =>
            node.DescendantTrivia(span)
                .Where(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia) && span.Contains(trivia.Span))
                .Select(static trivia => trivia.ToFullString());
    }
}
