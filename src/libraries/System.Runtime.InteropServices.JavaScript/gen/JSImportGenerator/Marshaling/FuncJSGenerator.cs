// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class FuncJSGenerator(TypePositionInfo info, StubCodeContext context, bool isAction, MarshalerType[] argumentMarshalerTypes) : BaseJSGenerator(info, context)
    {
        public override IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            foreach (var statement in base.Generate(context))
            {
                yield return statement;
            }

            var (managed, js) = context.GetIdentifiers(TypeInfo);

            var jsty = (JSFunctionTypeInfo)((JSMarshallingInfo)TypeInfo.MarshallingAttributeInfo).TypeInfo;
            var sourceTypes = jsty.ArgsTypeInfo
                .Select(a => a.Syntax)
                .ToArray();

            if (context.CurrentStage == StubIdentifierContext.Stage.UnmarshalCapture && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return ToManagedMethod(js, Argument(IdentifierName(managed)), jsty);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Marshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return ToJSMethod(js, Argument(IdentifierName(managed)), jsty);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.PinnedMarshal && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return ToJSMethod(js, Argument(IdentifierName(managed)), jsty);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Unmarshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return ToManagedMethod(js, Argument(IdentifierName(managed)), jsty);
            }
        }

        private ExpressionStatementSyntax ToManagedMethod(string target, ArgumentSyntax source, JSFunctionTypeInfo info)
        {
            List<ArgumentSyntax> arguments = [source.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))];
            for (int i = 0; i < info.ArgsTypeInfo.Length; i++)
            {
                var sourceType = info.ArgsTypeInfo[i];
                if (!isAction && i + 1 == info.ArgsTypeInfo.Length)
                {
                    arguments.Add(ArgToManaged(i, sourceType.Syntax, argumentMarshalerTypes[i]));
                }
                else
                {
                    arguments.Add(ArgToJS(i, sourceType.Syntax, argumentMarshalerTypes[i]));
                }
            }

            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(isAction ? MarshalerType.Action : MarshalerType.Function)))
                .WithArgumentList(ArgumentList(SeparatedList(arguments))));
        }

        private ExpressionStatementSyntax ToJSMethod(string target, ArgumentSyntax source, JSFunctionTypeInfo info)
        {
            List<ArgumentSyntax> arguments = [source];
            for (int i = 0; i < info.ArgsTypeInfo.Length; i++)
            {
                var sourceType = info.ArgsTypeInfo[i];
                if (!isAction && i + 1 == info.ArgsTypeInfo.Length)
                {
                    arguments.Add(ArgToJS(i, sourceType.Syntax, argumentMarshalerTypes[i]));
                }
                else
                {
                    arguments.Add(ArgToManaged(i, sourceType.Syntax, argumentMarshalerTypes[i]));
                }
            }

            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(isAction ? MarshalerType.Action : MarshalerType.Function)))
                .WithArgumentList(ArgumentList(SeparatedList(arguments))));
        }

        private static ArgumentSyntax ArgToJS(int i, TypeSyntax sourceType, MarshalerType marshalerType) => Argument(ParenthesizedLambdaExpression()
                            .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                            .WithParameterList(ParameterList(SeparatedList(new[]{
                        Parameter(Identifier("__delegate_arg_arg"+(i+1)))
                        .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                        .WithType(IdentifierName(Constants.JSMarshalerArgumentGlobal)),
                        Parameter(Identifier("__delegate_arg"+(i+1)))
                        .WithType(sourceType)})))
                            .WithBlock(Block(SingletonList<StatementSyntax>(ExpressionStatement(
                                InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("__delegate_arg_arg" + (i + 1)), GetToJSMethod(marshalerType)))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName("__delegate_arg"+(i+1))),
                                }))))))));

        private static ArgumentSyntax ArgToManaged(int i, TypeSyntax sourceType, MarshalerType marshalerType) => Argument(ParenthesizedLambdaExpression()
                            .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                            .WithParameterList(ParameterList(SeparatedList(new[]{
                        Parameter(Identifier("__delegate_arg_arg"+(i+1)))
                        .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                        .WithType(IdentifierName(Constants.JSMarshalerArgumentGlobal)),
                        Parameter(Identifier("__delegate_arg"+(i+1)))
                        .WithModifiers(TokenList(Token(SyntaxKind.OutKeyword)))
                        .WithType(sourceType)})))
                            .WithBlock(Block(SingletonList<StatementSyntax>(ExpressionStatement(
                                InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("__delegate_arg_arg" + (i + 1)), GetToManagedMethod(marshalerType)))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName("__delegate_arg"+(i+1))).WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword)),
                                }))))))));
    }
}
