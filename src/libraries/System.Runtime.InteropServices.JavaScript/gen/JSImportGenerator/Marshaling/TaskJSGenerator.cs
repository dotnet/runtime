// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class TaskJSGenerator(TypePositionInfo info, StubCodeContext context, MarshalerType resultMarshalerType) : BaseJSGenerator(info, context)
    {
        public override IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            foreach (var statement in base.Generate(context))
            {
                yield return statement;
            }

            var jsty = (JSTaskTypeInfo)((JSMarshallingInfo)TypeInfo.MarshallingAttributeInfo).TypeInfo;

            var (managed, js) = context.GetIdentifiers(TypeInfo);

            if (context.CurrentStage == StubIdentifierContext.Stage.UnmarshalCapture && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToManagedMethodVoid(js, Argument(IdentifierName(managed)))
                    : ToManagedMethod(js, Argument(IdentifierName(managed)), jsty.ResultTypeInfo.Syntax);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Marshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToJSMethodVoid(js, Argument(IdentifierName(managed)))
                    : ToJSMethod(js, Argument(IdentifierName(managed)), jsty.ResultTypeInfo.Syntax);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.PinnedMarshal && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToJSMethodVoid(js, Argument(IdentifierName(managed)))
                    : ToJSMethod(js, Argument(IdentifierName(managed)), jsty.ResultTypeInfo.Syntax);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Unmarshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToManagedMethodVoid(js, Argument(IdentifierName(managed)))
                    : ToManagedMethod(js, Argument(IdentifierName(managed)), jsty.ResultTypeInfo.Syntax);
            }
        }

        private static ExpressionStatementSyntax ToManagedMethodVoid(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(MarshalerType.Task)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))));
        }

        private static ExpressionStatementSyntax ToJSMethodVoid(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(MarshalerType.Task)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source))));
        }

        private ExpressionStatementSyntax ToManagedMethod(string target, ArgumentSyntax source, TypeSyntax sourceType)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(MarshalerType.Task)))
                .WithArgumentList(ArgumentList(SeparatedList(new[]{
                    source.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword)),
                    Argument(ParenthesizedLambdaExpression()
                    .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(new[]{
                        Parameter(Identifier("__task_result_arg"))
                        .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                        .WithType(IdentifierName(Constants.JSMarshalerArgumentGlobal)),
                        Parameter(Identifier("__task_result"))
                        .WithModifiers(TokenList(Token(SyntaxKind.OutKeyword)))
                        .WithType(sourceType)})))
                    .WithBlock(Block(SingletonList<StatementSyntax>(ExpressionStatement(
                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("__task_result_arg"), GetToManagedMethod(resultMarshalerType)))
                        .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName("__task_result")).WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword)),
                        }))))))))}))));
        }

        private ExpressionStatementSyntax ToJSMethod(string target, ArgumentSyntax source, TypeSyntax sourceType)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(MarshalerType.Task)))
                .WithArgumentList(ArgumentList(SeparatedList(new[]{
                    source,
                    Argument(ParenthesizedLambdaExpression()
                    .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(new[]{
                        Parameter(Identifier("__task_result_arg"))
                        .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                        .WithType(IdentifierName(Constants.JSMarshalerArgumentGlobal)),
                        Parameter(Identifier("__task_result"))
                        .WithType(sourceType)})))
                    .WithBlock(Block(SingletonList<StatementSyntax>(ExpressionStatement(
                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("__task_result_arg"), GetToJSMethod(resultMarshalerType)))
                        .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName("__task_result")),
                        }))))))))}))));
        }
    }
}
