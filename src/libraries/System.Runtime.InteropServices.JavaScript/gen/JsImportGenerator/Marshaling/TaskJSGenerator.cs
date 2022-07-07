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

    internal sealed class TaskJSGenerator : BaseJSGenerator
    {
        private MarshalerType _resultMarshalerType;
        public TaskJSGenerator(MarshalerType resultMarshalerType)
            : base(MarshalerType.Task, new Forwarder())
        {
            _resultMarshalerType = resultMarshalerType;
        }

        public override IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context)
        {
            var jsty = (JSTaskTypeInfo)info.ManagedType;
            if (jsty.ResultTypeInfo.FullTypeName == "void")
            {
                yield return InvocationExpression(MarshalerTypeName(MarshalerType.Task), ArgumentList());
            }
            else
            {
                yield return InvocationExpression(MarshalerTypeName(MarshalerType.Task),
                    ArgumentList(SingletonSeparatedList(Argument(MarshalerTypeName(_resultMarshalerType)))));
            }
        }

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            var jsty = (JSTaskTypeInfo)info.ManagedType;

            string argName = context.GetAdditionalIdentifier(info, "js_arg");
            var target = info.IsManagedReturnPosition
                ? Constants.ArgumentReturn
                : argName;

            var source = info.IsManagedReturnPosition
                ? Argument(IdentifierName(context.GetIdentifiers(info).native))
                : _inner.AsArgument(info, context);

            if (context.CurrentStage == StubCodeContext.Stage.Unmarshal && context.Direction == CustomTypeMarshallerDirection.In && info.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo.FullTypeName == "void"
                    ? ToManagedMethodVoid(target, source)
                    : ToManagedMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }

            if (context.CurrentStage == StubCodeContext.Stage.Marshal && context.Direction == CustomTypeMarshallerDirection.Out && info.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo.FullTypeName == "void"
                    ? ToJSMethodVoid(target, source)
                    : ToJSMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }

            foreach (var x in base.Generate(info, context))
            {
                yield return x;
            }

            if (context.CurrentStage == StubCodeContext.Stage.Invoke && context.Direction == CustomTypeMarshallerDirection.In && !info.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo.FullTypeName == "void"
                    ? ToJSMethodVoid(target, source)
                    : ToJSMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }

            if (context.CurrentStage == StubCodeContext.Stage.Unmarshal && context.Direction == CustomTypeMarshallerDirection.Out && !info.IsManagedReturnPosition)
            {
                yield return jsty.ResultTypeInfo.FullTypeName == "void"
                    ? ToManagedMethodVoid(target, source)
                    : ToManagedMethod(target, source, jsty.ResultTypeInfo.Syntax);
            }
        }

        private StatementSyntax ToManagedMethodVoid(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(Type)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))));
        }

        private StatementSyntax ToJSMethodVoid(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(Type)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(source))));
        }

        private StatementSyntax ToManagedMethod(string target, ArgumentSyntax source, TypeSyntax sourceType)
        {
            /*
            __arg1_gen_native__js_arg.ToManaged(out value, static (ref JSMarshalerArgument __task_result_arg) =>
            {
                __task_result_arg.ToManaged(out string? __task_result);
                return __task_result;
            });
            */
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
                            // TODO do we have root for string ?
                        }))))))))}))));
        }

        private StatementSyntax ToJSMethod(string target, ArgumentSyntax source, TypeSyntax sourceType)
        {
            /*
            __arg_return.ToJS(ref __retVal, static (ref global::System.Runtime.InteropServices.JavaScript.JSMarshalerArgument __task_result_arg, ref object __task_result) =>
            {
                __task_result_arg.ToJS(ref __task_result);
            });
             */
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
                        // TODO do we have root for string ? Constants.ToJSReturnMethod
                        IdentifierName("__task_result_arg"), GetToJSMethod(_resultMarshalerType)))
                        .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName("__task_result")),
                        }))))))))}))));
        }
    }
}
