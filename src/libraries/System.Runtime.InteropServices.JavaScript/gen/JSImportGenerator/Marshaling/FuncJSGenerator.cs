﻿// Licensed to the .NET Foundation under one or more agreements.
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
    internal sealed class FuncJSGenerator : BaseJSGenerator
    {
        private readonly bool _isAction;
        private readonly MarshalerType[] _argumentMarshalerTypes;

        public FuncJSGenerator(bool isAction, MarshalerType[] argumentMarshalerTypes)
            : base(isAction ? MarshalerType.Action : MarshalerType.Function, new Forwarder())
        {
            _isAction = isAction;
            _argumentMarshalerTypes = argumentMarshalerTypes;
        }

        public override IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context)
        {
            var args = _argumentMarshalerTypes.Select(x => Argument(MarshalerTypeName(x))).ToList();
            yield return InvocationExpression(MarshalerTypeName(Type), ArgumentList(SeparatedList(args)));
        }

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            string argName = context.GetAdditionalIdentifier(info, "js_arg");
            var target = info.IsManagedReturnPosition
                ? Constants.ArgumentReturn
                : argName;

            var source = info.IsManagedReturnPosition
                ? Argument(IdentifierName(context.GetIdentifiers(info).native))
                : _inner.AsArgument(info, context);

            var jsty = (JSFunctionTypeInfo)((JSMarshallingInfo)info.MarshallingAttributeInfo).TypeInfo;
            var sourceTypes = jsty.ArgsTypeInfo
                .Select(a => a.Syntax)
                .ToArray();

            if (context.CurrentStage == StubCodeContext.Stage.UnmarshalCapture && context.Direction == MarshalDirection.ManagedToUnmanaged && info.IsManagedReturnPosition)
            {
                yield return ToManagedMethod(target, source, jsty);
            }

            if (context.CurrentStage == StubCodeContext.Stage.Marshal && context.Direction == MarshalDirection.UnmanagedToManaged && info.IsManagedReturnPosition)
            {
                yield return ToJSMethod(target, source, jsty);
            }

            foreach (var x in base.Generate(info, context))
            {
                yield return x;
            }

            if (context.CurrentStage == StubCodeContext.Stage.PinnedMarshal && context.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsManagedReturnPosition)
            {
                yield return ToJSMethod(target, source, jsty);
            }

            if (context.CurrentStage == StubCodeContext.Stage.Unmarshal && context.Direction == MarshalDirection.UnmanagedToManaged && !info.IsManagedReturnPosition)
            {
                yield return ToManagedMethod(target, source, jsty);
            }
        }

        private ExpressionStatementSyntax ToManagedMethod(string target, ArgumentSyntax source, JSFunctionTypeInfo info)
        {
            List<ArgumentSyntax> arguments = new List<ArgumentSyntax>();
            arguments.Add(source.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword)));
            for (int i = 0; i < info.ArgsTypeInfo.Length; i++)
            {
                var sourceType = info.ArgsTypeInfo[i];
                if (!_isAction && i + 1 == info.ArgsTypeInfo.Length)
                {
                    arguments.Add(ArgToManaged(i, sourceType.Syntax, _argumentMarshalerTypes[i]));
                }
                else
                {
                    arguments.Add(ArgToJS(i, sourceType.Syntax, _argumentMarshalerTypes[i]));
                }
            }

            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(Type)))
                .WithArgumentList(ArgumentList(SeparatedList(arguments))));
        }

        private ExpressionStatementSyntax ToJSMethod(string target, ArgumentSyntax source, JSFunctionTypeInfo info)
        {
            List<ArgumentSyntax> arguments = new List<ArgumentSyntax>();
            arguments.Add(source);
            for (int i = 0; i < info.ArgsTypeInfo.Length; i++)
            {
                var sourceType = info.ArgsTypeInfo[i];
                if (!_isAction && i + 1 == info.ArgsTypeInfo.Length)
                {
                    arguments.Add(ArgToJS(i, sourceType.Syntax, _argumentMarshalerTypes[i]));
                }
                else
                {
                    arguments.Add(ArgToManaged(i, sourceType.Syntax, _argumentMarshalerTypes[i]));
                }
            }

            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(Type)))
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
