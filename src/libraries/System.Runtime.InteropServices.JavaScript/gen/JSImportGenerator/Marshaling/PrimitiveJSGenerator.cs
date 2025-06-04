// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class PrimitiveJSGenerator(TypePositionInfo info, StubCodeContext context, MarshalerType elementMarshallerType) : BaseJSGenerator(info, context)
    {
        // TODO order parameters in such way that affinity capturing parameters are emitted first
        public override IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            foreach (var statement in base.Generate(context))
            {
                yield return statement;
            }

            var (managed, js) = context.GetIdentifiers(TypeInfo);

            MarshalDirection marshalDirection = MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext);

            if (context.CurrentStage == StubIdentifierContext.Stage.UnmarshalCapture && marshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
            {
                yield return ToManagedMethod(js, Argument(IdentifierName(managed)));
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Marshal && marshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
            {
                yield return ToJSMethod(js, Argument(IdentifierName(managed)));
            }
        }

        private ExpressionStatementSyntax ToManagedMethod(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(elementMarshallerType)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))));
        }

        private ExpressionStatementSyntax ToJSMethod(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(elementMarshallerType)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source))));
        }
    }
}
