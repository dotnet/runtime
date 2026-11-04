// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Recognizes contexts introduced by earlier passes (exact placeholder marker) so that later passes merge with
    /// them instead of nesting or duplicating contexts. Contexts with any other marker are audited and left alone.
    /// </summary>
    internal static class PlaceholderContexts
    {
        /// <summary>Whether the line directly above <paramref name="token"/> is the placeholder block marker.</summary>
        public static bool HasBlockMarker(SyntaxToken token) =>
            token.LeadingTrivia.LastNonLayout is var last
            && last.IsKind(SyntaxKind.SingleLineCommentTrivia)
            && last.ToString() == UnsafeContextFacts.BlockMarker;

        /// <summary>Whether <paramref name="node"/> is an <c>unsafe(...)</c> directly preceded by the placeholder marker.</summary>
        public static bool IsPlaceholderWrapper(SyntaxNode node) =>
            UnsafeContextFacts.IsUnsafeExpression(node)
            && IsExpressionMarker((HasMarkerOnPreviousToken(node) ? node.GetFirstToken().GetPreviousToken().TrailingTrivia : node.GetLeadingTrivia()).LastNonLayout);

        /// <summary>
        /// Whether the marker before <paramref name="node"/> is on the previous token: a comment after a token on the same
        /// line is parsed as that token's trailing trivia, e.g. <c>return /* SAFETY: ... */ unsafe(...)</c>.
        /// </summary>
        public static bool HasMarkerOnPreviousToken(SyntaxNode node) =>
            node.GetLeadingTrivia().IsLayoutOnly
            && node.GetFirstToken().GetPreviousToken() is { RawKind: not 0, EndsLine: false } previous
            && IsExpressionMarker(previous.TrailingTrivia.LastNonLayout);

        private static bool IsExpressionMarker(SyntaxTrivia trivia) =>
            trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) && trivia.ToString() == UnsafeContextFacts.ExpressionMarker;

        /// <summary>
        /// Whether <paramref name="statement"/> is a placeholder block that may be extended; comments or directives
        /// around its braces would be dropped, so such blocks are not.
        /// </summary>
        public static bool IsExtendableBlock(StatementSyntax statement) =>
            statement is UnsafeStatementSyntax { Block: var block } unsafeStatement
            && HasBlockMarker(unsafeStatement.UnsafeKeyword)
            && unsafeStatement.UnsafeKeyword.TrailingTrivia.IsLayoutOnly
            && block.OpenBraceToken.LeadingTrivia.IsLayoutOnly
            && block.OpenBraceToken.TrailingTrivia.IsLayoutOnly
            && block.CloseBraceToken.LeadingTrivia.IsLayoutOnly
            && block.CloseBraceToken.TrailingTrivia.IsLayoutOnly;

        /// <summary>
        /// Returns the clause an expression belongs to: its statement, switch label, switch arm, catch clause, accessor
        /// or member. A clause holds at most one <c>unsafe(...)</c>.
        /// </summary>
        public static SyntaxNode? GetClause(SyntaxNode node) =>
            node.Ancestors().FirstOrDefault(static ancestor => ancestor
                is (StatementSyntax and not BlockSyntax)
                or SwitchExpressionArmSyntax
                or CasePatternSwitchLabelSyntax
                or CatchClauseSyntax
                or AccessorDeclarationSyntax
                or MemberDeclarationSyntax);

        /// <summary>Returns every <c>unsafe(...)</c>, placeholder or audited, that belongs to <paramref name="clause"/>.</summary>
        public static IEnumerable<SyntaxNode> GetWrappersInClause(SyntaxNode clause) =>
            clause.DescendantNodes().Where(UnsafeContextFacts.IsUnsafeExpression).Where(wrapper => GetClause(wrapper) == clause);

        /// <summary>Returns the spans of the placeholder <c>unsafe(...)</c> expressions in <paramref name="node"/>.</summary>
        public static IEnumerable<TextSpan> GetPlaceholderWrapperSpans(SyntaxNode node) =>
            node.DescendantNodes().Where(IsPlaceholderWrapper).Select(static wrapper => wrapper.Span);

        /// <summary>Whether every <c>unsafe(...)</c> in <paramref name="node"/> is a placeholder that may be merged.</summary>
        public static bool ContainsOnlyPlaceholderWrappers(SyntaxNode node) =>
            node.DescendantNodes().Where(UnsafeContextFacts.IsUnsafeExpression).All(IsPlaceholderWrapper);

        /// <summary>
        /// Returns <paramref name="node"/> with its placeholder <c>unsafe(...)</c> expressions and their markers removed.
        /// A marker on a token before <paramref name="node"/> is outside it and stays.
        /// </summary>
        public static T Unwrap<T>(T node) where T : SyntaxNode
        {
            List<SyntaxNode> wrappers = [.. node.DescendantNodes().Where(IsPlaceholderWrapper)];
            List<SyntaxToken> markedTokens =
            [
                .. wrappers.Where(HasMarkerOnPreviousToken)
                    .Select(static wrapper => wrapper.GetFirstToken().GetPreviousToken())
                    .Where(token => node.Span.Contains(token.Span)),
            ];

            return node.ReplaceSyntax(
                wrappers,
                static (original, rewritten) => UnwrapExpression((ExpressionSyntax)original, (ExpressionSyntax)rewritten),
                markedTokens,
                static (_, rewritten) => rewritten.WithTrailingTrivia(RemoveExpressionMarker(rewritten.TrailingTrivia)),
                trivia: null,
                computeReplacementTrivia: null);
        }

        private static ExpressionSyntax UnwrapExpression(ExpressionSyntax original, ExpressionSyntax rewritten)
        {
            ExpressionSyntax operand = UnsafeContextFacts.GetUnsafeExpressionOperand(rewritten).WithoutTrivia();
            if (NeedsParentheses(original, operand))
                operand = SyntaxFactory.ParenthesizedExpression(operand);

            SyntaxTriviaList leading = rewritten.GetLeadingTrivia();
            return operand
                .WithLeadingTrivia(HasMarkerOnPreviousToken(original) ? leading : RemoveExpressionMarker(leading))
                .WithTrailingTrivia(rewritten.GetTrailingTrivia());
        }

        // Drops the marker and the space after it.
        private static SyntaxTriviaList RemoveExpressionMarker(SyntaxTriviaList trivia)
        {
            int index = trivia.IndexOf(trivia.LastNonLayout);
            return index < 0 ? trivia : SyntaxFactory.TriviaList([.. trivia.Take(index), .. trivia.Skip(index + 1).SkipWhile(static t => t.IsKind(SyntaxKind.WhitespaceTrivia))]);
        }

        /// <summary>Whether the unwrapped operand needs the parentheses that <c>unsafe(...)</c> provided.</summary>
        private static bool NeedsParentheses(ExpressionSyntax wrapper, ExpressionSyntax operand) =>
            !IsPrimary(operand) && wrapper.Parent switch
            {
                ArgumentSyntax or EqualsValueClauseSyntax or ReturnStatementSyntax or ArrowExpressionClauseSyntax
                    or ParenthesizedExpressionSyntax or YieldStatementSyntax or InterpolationSyntax
                    or SwitchExpressionArmSyntax or WhenClauseSyntax or InitializerExpressionSyntax => false,
                AssignmentExpressionSyntax assignment => assignment.Right != wrapper,
                ConditionalExpressionSyntax conditional => conditional.Condition == wrapper,
                _ => true,
            };

        private static bool IsPrimary(ExpressionSyntax expression) =>
            expression is InstanceExpressionSyntax
                or LiteralExpressionSyntax
                or SimpleNameSyntax
                or MemberAccessExpressionSyntax
                or InvocationExpressionSyntax
                or ElementAccessExpressionSyntax
                or ParenthesizedExpressionSyntax
                or BaseObjectCreationExpressionSyntax
                or ArrayCreationExpressionSyntax
                or ImplicitArrayCreationExpressionSyntax
                or TypeOfExpressionSyntax
                or SizeOfExpressionSyntax
                or DefaultExpressionSyntax
                or CheckedExpressionSyntax
                or InterpolatedStringExpressionSyntax
                or TupleExpressionSyntax
                or PostfixUnaryExpressionSyntax;
    }
}
