// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
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
        internal const string SafetyDocumentationElement = "safety";

        // This project compiles against MicrosoftCodeAnalysisVersion_LatestVS, which predates SyntaxKind.SafeKeyword,
        // but it runs inside whichever compiler the host loaded, which may be newer. Resolve the kind at run time so
        // the migration tooling works on both, and treats 'safe' as unsupported when the host cannot parse it.
        private static readonly SyntaxKind s_safeKeyword = SyntaxFacts.GetContextualKeywordKind("safe");

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

        internal static SyntaxToken GetSafeModifier(SyntaxNode declaration) =>
            s_safeKeyword == SyntaxKind.None ? default : GetModifier(declaration, s_safeKeyword);

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
        /// Determines whether a declaration carries a <c>&lt;safety&gt;</c> documentation element.
        /// </summary>
        internal static bool HasSafetyDocumentation(SyntaxNode declaration) =>
            declaration.GetLeadingTrivia().Any(static trivia =>
                trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationComment
                && documentationComment.DescendantNodes().Any(static node =>
                    node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: SafetyDocumentationElement }
                        or XmlEmptyElementSyntax { Name.LocalName.ValueText: SafetyDocumentationElement }));
    }
}
#endif
