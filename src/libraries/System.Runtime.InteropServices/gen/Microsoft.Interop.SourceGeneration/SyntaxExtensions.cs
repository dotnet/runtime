// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public static class SyntaxExtensions
    {
        public static ContainingSyntax GetDeclarationTemplate(this TypeDeclarationSyntax declaration)
        {
            return new ContainingSyntax(
                CodeWriterHelpers.GetModifiers(declaration.Modifiers),
                declaration.Kind().GetDeclarationKind(),
                declaration.Identifier.Text,
                GetTypeParameters(declaration.TypeParameterList));
        }

        public static ContainingSyntax GetDeclarationTemplate(this MethodDeclarationSyntax declaration)
        {
            return new ContainingSyntax(
                CodeWriterHelpers.GetModifiers(declaration.Modifiers),
                ContainingDeclarationKind.Method,
                declaration.Identifier.Text,
                GetTypeParameters(declaration.TypeParameterList));
        }

        public static ContainingSyntaxContext GetContainingSyntaxContext(this MemberDeclarationSyntax memberDeclaration)
        {
            var containingTypes = ImmutableArray.CreateBuilder<ContainingSyntax>();
            for (SyntaxNode? parent = memberDeclaration.Parent; parent is TypeDeclarationSyntax typeDeclaration; parent = parent.Parent)
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

        private static string? GetTypeParameters(TypeParameterListSyntax? typeParameters)
        {
            if (typeParameters is null)
            {
                return null;
            }

            return "<" + string.Join(", ", typeParameters.Parameters.Select(static parameter =>
                (parameter.AttributeLists.Count == 0 ? "" : string.Join(" ", parameter.AttributeLists.Select(static attributes => attributes.ToString())) + " ")
                + (parameter.VarianceKeyword.RawKind == 0 ? "" : parameter.VarianceKeyword.Text + " ")
                + parameter.Identifier.Text)) + ">";
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
