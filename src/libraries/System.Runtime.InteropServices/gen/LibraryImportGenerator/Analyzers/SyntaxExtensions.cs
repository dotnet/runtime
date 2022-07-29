// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Microsoft.Interop.Analyzers
{
    internal static class SyntaxExtensions
    {
        public static AttributeArgumentSyntax FindArgumentWithNameOrArity(this AttributeSyntax attribute, string name, int arity)
        {
            return attribute.ArgumentList.Arguments.FirstOrDefault(arg => arg.NameColon?.Name.ToString() == name) ?? attribute.ArgumentList.Arguments[arity];
        }

        public static Location FindTypeExpressionOrNullLocation(this AttributeArgumentSyntax attributeArgumentSyntax)
        {
            var walker = new FindTypeLocationWalker();
            walker.Visit(attributeArgumentSyntax);
            return walker.TypeExpressionLocation;
        }

        public static AttributeData? FindAttributeData(this AttributeSyntax syntax, ISymbol targetSymbol)
        {
            AttributeTargetSpecifierSyntax attributeTarget = syntax.FirstAncestorOrSelf<AttributeListSyntax>().Target;
            if (attributeTarget is not null)
            {
                switch (attributeTarget.Identifier.Kind())
                {
                    case SyntaxKind.ReturnKeyword:
                        return ((IMethodSymbol)targetSymbol).GetReturnTypeAttributes().First(attributeSyntaxLocationMatches);
                    case SyntaxKind.AssemblyKeyword:
                        return targetSymbol.ContainingAssembly.GetAttributes().First(attributeSyntaxLocationMatches);
                    case SyntaxKind.ModuleKeyword:
                        return targetSymbol.ContainingModule.GetAttributes().First(attributeSyntaxLocationMatches);
                    default:
                        return null;
                }
            }
            // Sometimes an attribute is put on a symbol that is nested within the containing symbol.
            // For example, the ContainingSymbol for an AttributeSyntax on a parameter have a ContainingSymbol of the method.
            // Since this method is internal and the callers don't care about attributes on parameters, we just allow
            // this method to return null in those cases.
            return targetSymbol.GetAttributes().FirstOrDefault(attributeSyntaxLocationMatches);

            bool attributeSyntaxLocationMatches(AttributeData attrData)
            {
                return attrData.ApplicationSyntaxReference!.SyntaxTree == syntax.SyntaxTree && attrData.ApplicationSyntaxReference.Span == syntax.Span;
            }
        }

        private sealed class FindTypeLocationWalker : CSharpSyntaxWalker
        {
            public Location? TypeExpressionLocation { get; private set; }

            public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
            {
                TypeExpressionLocation = node.Type.GetLocation();
            }

            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    TypeExpressionLocation = node.GetLocation();
                }
                else
                {
                    base.VisitLiteralExpression(node);
                }
            }
        }

    }
}
