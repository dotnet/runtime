// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class TaskJSGenerator : BaseJSGenerator
    {
        private readonly MarshalerType _resultMarshalerType;

        public TaskJSGenerator(TypePositionInfo info, StubCodeContext context, MarshalerType resultMarshalerType)
            : base(MarshalerType.Task, new Forwarder().Bind(info, context))
        {
            _resultMarshalerType = resultMarshalerType;
        }

        public override IEnumerable<ExpressionSyntax> GenerateBind()
        {
            var jsty = (JSTaskTypeInfo)((JSMarshallingInfo)TypeInfo.MarshallingAttributeInfo).TypeInfo;
            if (jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void))
            {
                yield return InvocationExpression(MarshalerTypeName(MarshalerType.Task), ArgumentList());
            }
            else
            {
                yield return InvocationExpression(MarshalerTypeName(MarshalerType.Task),
                    ArgumentList(SingletonSeparatedList(Argument(MarshalerTypeName(_resultMarshalerType)))));
            }
        }

        public override IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            var jsty = (JSTaskTypeInfo)((JSMarshallingInfo)TypeInfo.MarshallingAttributeInfo).TypeInfo;

            string argName = context.GetAdditionalIdentifier(TypeInfo, "js_arg");
            var target = TypeInfo.IsManagedReturnPosition
                ? Constants.ArgumentReturn
                : argName;

            var source = TypeInfo.IsManagedReturnPosition
                ? Argument(IdentifierName(context.GetIdentifiers(TypeInfo).native))
                : _inner.AsArgument(context);

            if (context.CurrentStage == StubIdentifierContext.Stage.UnmarshalCapture && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToManagedMethodVoid(target, source)
                    : ToManagedMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Marshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToJSMethodVoid(target, source)
                    : ToJSMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }

            foreach (var x in base.Generate(context))
            {
                yield return x;
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.PinnedMarshal && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToJSMethodVoid(target, source)
                    : ToJSMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Unmarshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo is JSSimpleTypeInfo(KnownManagedType.Void)
                    ? ToManagedMethodVoid(target, source)
                    : ToManagedMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }
        }

        private ExpressionStatementSyntax ToManagedMethodVoid(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(Type)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))));
        }

        private ExpressionStatementSyntax ToJSMethodVoid(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(Type)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source))));
        }

        private ExpressionStatementSyntax ToManagedMethod(string target, ArgumentSyntax source, TypeSyntax sourceType)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(Type)))
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
                        IdentifierName("__task_result_arg"), GetToManagedMethod(_resultMarshalerType)))
                        .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName("__task_result")).WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword)),
                        }))))))))}))));
        }

        private ExpressionStatementSyntax ToJSMethod(string target, ArgumentSyntax source, TypeSyntax sourceType)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(Type)))
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
                        IdentifierName("__task_result_arg"), GetToJSMethod(_resultMarshalerType)))
                        .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName("__task_result")),
                        }))))))))}))));
        }
    }
}
