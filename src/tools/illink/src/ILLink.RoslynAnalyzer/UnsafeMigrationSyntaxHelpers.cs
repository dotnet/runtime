// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if UNSAFE_MIGRATION
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Provides source-shared modifier inspection for the unsafe-v2 analyzers and code fixes.
    /// </summary>
    internal static class UnsafeMigrationSyntaxHelpers
    {
        // The analyzer builds against a Roslyn version that predates SyntaxKind.SafeKeyword.
        private static readonly SyntaxKind s_safeKeyword = SyntaxFacts.GetContextualKeywordKind("safe");

        /// <summary>
        /// The kind of the <c>safe</c> contextual keyword, or <see cref="SyntaxKind.None"/> when the hosting
        /// compiler does not know it.
        /// </summary>
        internal static SyntaxKind SafeKeywordKind => s_safeKeyword;

        internal static SyntaxTokenList GetModifiers(SyntaxNode declaration) =>
            declaration switch
            {
                MemberDeclarationSyntax member => member.Modifiers,
                LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
                AccessorDeclarationSyntax accessor => accessor.Modifiers,
                _ => default,
            };

        internal static bool HasModifier(SyntaxNode declaration, SyntaxKind modifier) =>
            GetModifiers(declaration).Any(modifier);

        internal static bool HasSafeModifier(SyntaxNode declaration) =>
            s_safeKeyword != SyntaxKind.None && GetModifiers(declaration).Any(s_safeKeyword);

        /// <summary>
        /// Determines whether a declaration carries a <c>&lt;safety&gt;</c> XML documentation element.
        /// </summary>
        /// <remarks>
        /// The element is the audit record for a safety contract: it justifies keeping <c>unsafe</c> on a member
        /// (<c>IL5005</c>), states that a pointer signature is deliberately not caller-unsafe (<c>IL5006</c>), and
        /// justifies an explicit <c>safe</c> modifier (<c>IL5010</c>).
        /// </remarks>
        internal static bool HasSafetyDocumentation(SyntaxNode declaration) =>
            declaration.GetLeadingTrivia().Any(static trivia =>
                trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationComment
                && documentationComment.DescendantNodes().Any(static node =>
                    node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: "safety" }
                        or XmlEmptyElementSyntax { Name.LocalName.ValueText: "safety" }));

        internal static SyntaxToken GetModifier(SyntaxNode declaration, SyntaxKind modifier)
        {
            foreach (SyntaxToken token in GetModifiers(declaration))
            {
                if (token.IsKind(modifier))
                    return token;
            }

            return default;
        }

        /// <summary>
        /// Determines whether an <c>unsafe</c> keyword token introduces an <c>unsafe(...)</c> expression rather
        /// than a modifier or a block.
        /// </summary>
        /// <remarks>
        /// <c>UnsafeExpressionSyntax</c> is newer than the Roslyn these analyzers compile against, but it derives
        /// from <see cref="ExpressionSyntax"/>, which is not, so the expression form is recognized by its parent's
        /// base type. Matching on a following open parenthesis instead would misclassify the modifier on any
        /// member whose type is a tuple, such as <c>static unsafe (int, int) M()</c>.
        /// </remarks>
        internal static bool IsUnsafeExpressionKeyword(SyntaxToken token) =>
            token.IsKind(SyntaxKind.UnsafeKeyword) && token.Parent is ExpressionSyntax;

        /// <summary>
        /// Determines whether a node is itself an <c>unsafe(...)</c> expression.
        /// </summary>
        /// <remarks>
        /// Testing only the first token is not enough: a node's first token can belong to a descendant, so the
        /// enclosing binary expression in <c>unsafe(a) + b</c> also starts with an <c>unsafe</c> keyword. The
        /// token must therefore be owned by the node being tested.
        /// </remarks>
        internal static bool IsUnsafeExpression(SyntaxNode node)
        {
            SyntaxToken first = node.GetFirstToken();
            return IsUnsafeExpressionKeyword(first) && ReferenceEquals(first.Parent, node);
        }

        /// <summary>
        /// Determines whether a declaration contains an inactive <c>#if</c> branch with pointer operations, which may
        /// rely on the lexical unsafe context of the declaration's <c>unsafe</c> modifier in another configuration.
        /// </summary>
        /// <remarks>
        /// Only trivia inside the declaration's own span counts, since its leading trivia can hold the inactive branch
        /// of a preceding declaration. The inactive text is parsed on its own, so only syntactic pointer operations are
        /// found; calls to caller-unsafe members can't be told apart from other calls without binding.
        /// </remarks>
        internal static bool ContainsInactiveUnsafeCode(SyntaxNode declaration) =>
            declaration.DescendantTrivia().Any(trivia =>
                trivia.IsKind(SyntaxKind.DisabledTextTrivia)
                && declaration.Span.Contains(trivia.Span)
                && SyntaxFactory.ParseStatement("{" + trivia.ToString() + "\n}", options: declaration.SyntaxTree.Options as CSharpParseOptions)
                    .DescendantNodesAndSelf()
                    .Any(static node => node.Kind() is SyntaxKind.PointerIndirectionExpression
                        or SyntaxKind.PointerMemberAccessExpression
                        or SyntaxKind.AddressOfExpression
                        or SyntaxKind.PointerType
                        or SyntaxKind.FunctionPointerType
                        or SyntaxKind.StackAllocArrayCreationExpression
                        or SyntaxKind.ImplicitStackAllocArrayCreationExpression
                        or SyntaxKind.FixedStatement));
    }
}
#endif
