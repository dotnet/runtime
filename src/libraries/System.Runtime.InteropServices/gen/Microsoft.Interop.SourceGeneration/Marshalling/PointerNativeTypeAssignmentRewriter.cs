// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Rewrite assignment expressions to the native identifier to cast to IntPtr.
    /// This handles the case where the native type of a non-blittable managed type is a pointer,
    /// which are unsupported in generic type parameters.
    /// </summary>
    internal sealed class PointerNativeTypeAssignmentRewriter : CSharpSyntaxRewriter
    {
        private readonly string _nativeIdentifier;
        private readonly PointerTypeSyntax _nativeType;

        public PointerNativeTypeAssignmentRewriter(string nativeIdentifier, PointerTypeSyntax nativeType)
        {
            _nativeIdentifier = nativeIdentifier;
            _nativeType = nativeType;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (node.Left.ToString() == _nativeIdentifier)
            {
                return node.WithRight(
                    CastExpression(MarshallerHelpers.SystemIntPtrType, node.Right));
            }
            if (node.Right.ToString() == _nativeIdentifier)
            {
                return node.WithRight(CastExpression(_nativeType, node.Right));
            }

            return base.VisitAssignmentExpression(node);
        }

        public override SyntaxNode? VisitArgument(ArgumentSyntax node)
        {
            if (node.Expression.ToString() == _nativeIdentifier)
            {
                return node.WithExpression(
                    CastExpression(_nativeType, node.Expression));
            }
            return base.VisitArgument(node);
        }
    }
}
