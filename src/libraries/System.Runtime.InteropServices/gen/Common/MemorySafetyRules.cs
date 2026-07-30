// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop
{
    /// <summary>
    /// Helpers for reasoning about the updated memory safety rules (unsafe evolution) in the interop generators.
    /// </summary>
    internal static class MemorySafetyRules
    {
        /// <summary>
        /// The compiler feature flag that opts an assembly into the updated memory safety rules.
        /// </summary>
        /// <remarks>
        /// Roslyn does not expose the memory safety rules version through a public API yet
        /// (https://github.com/dotnet/roslyn/issues/82546), so we look at the same feature flag the compiler
        /// itself uses to determine whether the updated rules are in effect.
        /// </remarks>
        internal const string UpdatedMemorySafetyRulesFeature = "updated-memory-safety-rules";

        /// <summary>
        /// The <c>safe</c> contextual keyword introduced by the unsafe evolution feature.
        /// </summary>
        /// <remarks>
        /// The generators compile against an older Roslyn than the one they run on, so the kind is resolved at
        /// run time instead of referencing <c>SyntaxKind.SafeKeyword</c> directly. It is
        /// <see cref="SyntaxKind.None"/> when the hosting compiler does not know the keyword.
        /// </remarks>
        private static readonly SyntaxKind s_safeKeyword = SyntaxFacts.GetContextualKeywordKind("safe");

        /// <summary>
        /// Determines whether the compilation the provided tree belongs to uses the updated memory safety rules.
        /// </summary>
        public static bool UsesUpdatedMemorySafetyRules(SyntaxTree tree)
            => tree.Options.Features.ContainsKey(UpdatedMemorySafetyRulesFeature);

        /// <summary>
        /// Determines whether a declaration explicitly states its safety contract with <c>safe</c> or <c>unsafe</c>.
        /// </summary>
        public static bool HasExplicitSafetyModifier(SyntaxTokenList modifiers)
            => modifiers.Any(SyntaxKind.UnsafeKeyword) || HasSafeModifier(modifiers);

        /// <summary>
        /// Determines whether a declaration is explicitly marked <c>safe</c>.
        /// </summary>
        public static bool HasSafeModifier(SyntaxTokenList modifiers)
            => s_safeKeyword != SyntaxKind.None && modifiers.Any(s_safeKeyword);

        /// <summary>
        /// Carries an explicit <c>safe</c> modifier from <paramref name="original"/> over to
        /// <paramref name="rewritten"/>, at the position it originally held.
        /// </summary>
        /// <remarks>
        /// <c>DeclarationModifiers</c> has no notion of <c>safe</c>, so a declaration rebuilt through it loses
        /// the modifier, which under the updated rules silently widens the contract to the caller.
        /// </remarks>
        public static SyntaxTokenList WithSafeModifierFrom(SyntaxTokenList rewritten, SyntaxTokenList original)
        {
            if (!HasSafeModifier(original) || HasSafeModifier(rewritten))
                return rewritten;

            SyntaxToken safeToken = SyntaxFactory.Token(s_safeKeyword).WithTrailingTrivia(SyntaxFactory.Space);
            int index = 0;
            while (index < original.Count && !original[index].IsKind(s_safeKeyword))
                index++;

            return rewritten.Insert(index < rewritten.Count ? index : rewritten.Count, safeToken);
        }
    }
}
