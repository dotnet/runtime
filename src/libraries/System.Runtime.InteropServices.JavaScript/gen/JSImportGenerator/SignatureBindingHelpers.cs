// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal static class SignatureBindingHelpers
    {
        public static ArgumentSyntax CreateSignaturesArgument(ImmutableArray<TypePositionInfo> elements, StubCodeContext context)
        {
            List<CollectionElementSyntax> arguments = [];

            foreach (TypePositionInfo element in elements.Where(e => e.NativeIndex != TypePositionInfo.UnsetIndex).OrderBy(e => e.NativeIndex))
            {
                var (baseType, subTypes) = JSGeneratorResolver.GetMarshallerTypeForBinding(element, context);
                ExpressionSyntax bindSyntax = MarshalerTypeName(baseType);
                if (subTypes is not null)
                {
                    bindSyntax = InvocationExpression(bindSyntax,
                        ArgumentList(SeparatedList(subTypes.Select(s => Argument(MarshalerTypeName(s))))));
                }
                arguments.Add(ExpressionElement(bindSyntax));
            }

            return Argument(CollectionExpression(SeparatedList(arguments)));
        }

        private static IdentifierNameSyntax MarshalerTypeName(MarshalerType marshalerType)
        {
            return IdentifierName(Constants.JSMarshalerTypeGlobalDot + marshalerType.ToString());
        }

    }
}
