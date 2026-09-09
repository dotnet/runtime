// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerators;

namespace Microsoft.Interop
{
    public static class SyntaxExtensions
    {
        public static DeclarationHeader GetDeclarationTemplate(this TypeDeclarationSyntax declaration)
            => ContainingTypeUtilities.GetDeclarationHeader(declaration);

        public static DeclarationHeader GetDeclarationTemplate(this MethodDeclarationSyntax declaration)
            => ContainingTypeUtilities.GetDeclarationHeader(declaration);

        public static ContainingSyntaxContext GetContainingSyntaxContext(this MemberDeclarationSyntax memberDeclaration)
        {
            var containingTypes = ImmutableArray.CreateBuilder<DeclarationHeader>();
            foreach (TypeDeclarationSyntax typeDeclaration in ContainingTypeUtilities.EnumerateContainingTypes(memberDeclaration.Parent as TypeDeclarationSyntax))
            {
                containingTypes.Add(typeDeclaration.GetDeclarationTemplate());
            }

            StringBuilder? containingNamespace = null;
            for (SyntaxNode? parent = memberDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>(); parent is BaseNamespaceDeclarationSyntax ns; parent = parent.Parent)
            {
                string name = string.Concat(ns.Name.DescendantTokens().Select(static token => token.Text));
                if (containingNamespace is null)
                {
                    containingNamespace = new StringBuilder(name);
                }
                else
                {
                    containingNamespace.Insert(0, name + ".");
                }
            }
            return new ContainingSyntaxContext(containingTypes.ToImmutable(), containingNamespace?.ToString());
        }

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
