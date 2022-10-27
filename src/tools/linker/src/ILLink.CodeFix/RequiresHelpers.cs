// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFixProvider
{
    sealed class RequiresHelpers
    {
        internal static SyntaxNode[] GetAttributeArgumentsForRequires(ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, bool hasPublicAccessibility)
        {
            var symbolDisplayName = targetSymbol.GetDisplayName();
            if (string.IsNullOrEmpty(symbolDisplayName) || hasPublicAccessibility)
                return Array.Empty<SyntaxNode>();

            return new[] { syntaxGenerator.AttributeArgument(syntaxGenerator.LiteralExpression($"Calls {symbolDisplayName}")) };
        }
    }
}
