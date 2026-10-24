// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop;

/// <summary>Adapts compiler syntax classifications to text-based generation.</summary>
public static class RoslynSyntaxHelpers
{
    extension(CodeWriterHelpers)
    {
        /// <summary>Escapes an identifier using the hosting compiler's keyword vocabulary.</summary>
        /// <param name="identifier">The identifier to escape.</param>
        /// <returns>The escaped identifier text.</returns>
        public static string EscapeIdentifier(string identifier) => CodeWriterHelpers.EscapeIdentifier(
            identifier,
            SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None);
    }
}
