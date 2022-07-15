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
        public static AttributeArgumentSyntax FindArgumentWithArityOrName(this AttributeSyntax attribute, int arity, string name)
        {
            return attribute.ArgumentList.Arguments.FirstOrDefault(arg => arg.NameColon?.Name.ToString() == name) ?? attribute.ArgumentList.Arguments[arity];
        }

        public static Location FindTypeExpressionOrNullLocation(this AttributeArgumentSyntax attributeArgumentSyntax)
        {
            var walker = new FindTypeLocationWalker();
            walker.Visit(attributeArgumentSyntax);
            return walker.TypeExpressionLocation;
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
