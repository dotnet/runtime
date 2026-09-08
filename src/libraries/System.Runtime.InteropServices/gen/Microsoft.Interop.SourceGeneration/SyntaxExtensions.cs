// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public static class SyntaxExtensions
    {
        public static bool IsInPartialContext(this TypeDeclarationSyntax syntax, [NotNullWhen(false)] out SyntaxToken? nonPartialIdentifier)
        {
            for (SyntaxNode? parentNode = syntax; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    nonPartialIdentifier = typeDecl.Identifier;
                    return false;
                }
            }
            nonPartialIdentifier = null;
            return true;
        }
    }
}
