// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    /* FURTURE
    internal sealed class NativeJSGenerator : PrimitiveJSGenerator
    {
        public NativeJSGenerator(IMarshallingGenerator inner)
            : base(MarshalerType.NativeMarshalling, inner)
        {
        }

        public override IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context)
        {
            yield return InvocationExpression(
                    GenericName(Constants.JSMarshalerTypeGlobalDot + Type.ToString())
                    .WithTypeArgumentList(TypeArgumentList(
                            SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] {
                                IdentifierName(info.ManagedType.FullTypeName),
                                Token(SyntaxKind.CommaToken),
                                _inner.AsNativeType(info)
                                }))));
        }


        protected override ArgumentSyntax ToManagedMethodRefOrOut(ArgumentSyntax argument)
        {
            return argument.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
        }

        protected override ArgumentSyntax ToJSMethodRefOrOut(ArgumentSyntax argument)
        {
            return argument.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
        }

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage == StubCodeContext.Stage.Unmarshal && info.IsManagedReturnPosition)
            {
                yield return ExpressionStatement(InvocationExpression(IdentifierName(Constants.UnsafeSkipInitGlobal))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(JSCodeGenerator.ReturnNativeIdentifier))
                            .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))));
            }

            foreach (var x in base.Generate(info, context))
            {
                yield return x;
            }
        }
    }*/
}
