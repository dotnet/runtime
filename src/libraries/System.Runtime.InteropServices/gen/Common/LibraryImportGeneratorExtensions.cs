// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop
{
    internal static class LibraryImportGeneratorExtensions
    {
        private const string SafeModifier = "safe";
        private const string UpdatedMemorySafetyRulesFeature = "updated-memory-safety-rules";

        internal static bool HasSafetyModifier(this SyntaxTokenList modifiers)
        {
            foreach (SyntaxToken modifier in modifiers)
            {
                // LibraryImportGenerator supports compiler hosts whose public SyntaxKind API predates SafeKeyword.
                if (modifier.IsKind(SyntaxKind.UnsafeKeyword) || modifier.ValueText == SafeModifier)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool UsesUpdatedMemorySafetyRules(this SyntaxTree syntaxTree)
            => syntaxTree.Options.Features.ContainsKey(UpdatedMemorySafetyRulesFeature);
    }
}
