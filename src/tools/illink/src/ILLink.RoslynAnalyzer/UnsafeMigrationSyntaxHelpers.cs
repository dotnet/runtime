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
        // The analyzer builds against a Roslyn version that predates SyntaxKind.SafeKeyword.
        private static readonly SyntaxKind s_safeKeyword = SyntaxFacts.GetContextualKeywordKind("safe");

        /// <summary>
        /// The kind of the <c>safe</c> contextual keyword, or <see cref="SyntaxKind.None"/> when the hosting
        /// compiler does not know it.
        /// </summary>
        internal static SyntaxKind SafeKeywordKind => s_safeKeyword;

        /// <summary>
        /// The compiler feature flag that opts an assembly into the updated memory safety rules.
        /// </summary>
        /// <remarks>
        /// Roslyn does not expose the memory safety rules version through a public API yet
        /// (https://github.com/dotnet/roslyn/issues/82546), so the same feature flag the compiler itself reads
        /// is used to determine whether the updated rules are in effect.
        /// </remarks>
        private const string UpdatedMemorySafetyRulesFeature = "updated-memory-safety-rules";

        /// <summary>
        /// Determines whether the compilation a tree belongs to uses the updated memory safety rules.
        /// </summary>
        internal static bool UsesUpdatedMemorySafetyRules(SyntaxTree tree) =>
            tree.Options.Features.ContainsKey(UpdatedMemorySafetyRulesFeature);

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

        internal static SyntaxToken GetModifier(SyntaxNode declaration, SyntaxKind modifier)
        {
            foreach (SyntaxToken token in GetModifiers(declaration))
            {
                if (token.IsKind(modifier))
                    return token;
            }

            return default;
        }
    }
}
#endif
