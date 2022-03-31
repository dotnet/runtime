// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JavaScript.MarshalerGenerator
{
    public static class TypeSymbolExtensions
    {
        public static TypeSyntax AsTypeSyntax(this ITypeSymbol type)
        {
            string text = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return ParseTypeName(text);
        }

        public static NamespaceDeclarationSyntax AsNamespace(this INamespaceSymbol ns)
        {
            string text = ns.ToDisplayString();
            return NamespaceDeclaration(ParseName(text));
        }
    }
}
