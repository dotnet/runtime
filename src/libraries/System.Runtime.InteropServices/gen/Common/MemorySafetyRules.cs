// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// SyntaxKind.SafeKeyword is experimental in the Roslyn versions that already declare it. Where the reference is
// old enough that it does not, the shim below supplies the same member and this suppression does nothing.
#pragma warning disable RSEXPERIMENTAL006

namespace Microsoft.Interop
{
    /// <summary>
    /// Stands in for the updated memory safety rules (unsafe evolution) APIs that the interop generators run
    /// against but cannot yet compile against.
    /// </summary>
    /// <remarks>
    /// The generators build against an older Roslyn than the one they run on, so each member here is spelled the
    /// way the newer Roslyn API is spelled. A declared member always takes precedence over an extension member,
    /// so once the Roslyn reference is updated the call sites keep compiling as they are and the shim they no
    /// longer reach can be deleted.
    /// </remarks>
    internal static class MemorySafetyRules
    {
        /// <summary>
        /// The compiler feature flag that opts an assembly into the updated memory safety rules, mirroring
        /// Roslyn's own <c>Feature.UpdatedMemorySafetyRules</c>.
        /// </summary>
        internal const string UpdatedMemorySafetyRulesFeature = "updated-memory-safety-rules";

        private static readonly SyntaxKind s_safeKeyword = SyntaxFacts.GetContextualKeywordKind("safe");

        extension(SyntaxKind)
        {
            /// <summary>
            /// Mirrors <c>SyntaxKind.SafeKeyword</c>, the <c>safe</c> contextual keyword introduced by the
            /// unsafe evolution feature.
            /// </summary>
            /// <remarks>
            /// The kind is resolved from the hosting compiler, and is <see cref="SyntaxKind.None"/> when that
            /// compiler does not know the keyword. A modifier is never of kind <see cref="SyntaxKind.None"/>, so
            /// testing a modifier list against it finds nothing, which is the answer a compiler without the
            /// keyword should give.
            /// </remarks>
            internal static SyntaxKind SafeKeyword => s_safeKeyword;
        }

        extension(SyntaxTree tree)
        {
            /// <summary>
            /// Mirrors <c>CSharpCompilationOptions.UseUpdatedMemorySafetyRules</c>, reporting whether the
            /// compilation the tree belongs to uses the updated memory safety rules.
            /// </summary>
            /// <remarks>
            /// Roslyn does not expose the memory safety rules version through a public API yet
            /// (https://github.com/dotnet/roslyn/issues/82546), so the same feature flag the compiler itself
            /// treats as the temporary way to opt in is read here.
            /// </remarks>
            internal bool UseUpdatedMemorySafetyRules
                => tree.Options.Features.ContainsKey(UpdatedMemorySafetyRulesFeature);
        }

        extension(SyntaxTokenList modifiers)
        {
            /// <summary>
            /// Determines whether a declaration states its safety contract explicitly, with either <c>safe</c>
            /// or <c>unsafe</c>.
            /// </summary>
            internal bool HasExplicitSafetyModifier
                => modifiers.Any(SyntaxKind.UnsafeKeyword) || modifiers.Any(SyntaxKind.SafeKeyword);

            /// <summary>
            /// Carries an explicit <c>safe</c> modifier from <paramref name="original"/> over to these modifiers,
            /// at the position it originally held.
            /// </summary>
            /// <remarks>
            /// <c>DeclarationModifiers</c> has no notion of <c>safe</c>, so a declaration rebuilt through it
            /// loses the modifier, which under the updated rules silently widens the contract to the caller.
            /// </remarks>
            internal SyntaxTokenList WithSafeModifierFrom(SyntaxTokenList original)
            {
                if (!original.Any(SyntaxKind.SafeKeyword) || modifiers.Any(SyntaxKind.SafeKeyword))
                    return modifiers;

                SyntaxToken safeToken = SyntaxFactory.Token(SyntaxKind.SafeKeyword).WithTrailingTrivia(SyntaxFactory.Space);
                int index = 0;
                while (index < original.Count && !original[index].IsKind(SyntaxKind.SafeKeyword))
                    index++;

                return modifiers.Insert(index < modifiers.Count ? index : modifiers.Count, safeToken);
            }
        }
    }
}
