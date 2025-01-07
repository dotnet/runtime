// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal class PrimitiveJSGenerator : BaseJSGenerator
    {
        public PrimitiveJSGenerator(MarshalerType marshalerType, IBoundMarshallingGenerator inner)
            : base(marshalerType, inner)
        {
        }

        public PrimitiveJSGenerator(TypePositionInfo info, StubCodeContext context, MarshalerType marshalerType)
            : base(marshalerType, new Forwarder().Bind(info, context))
        {
        }

        // TODO order parameters in such way that affinity capturing parameters are emitted first
        public override IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            string argName = context.GetAdditionalIdentifier(TypeInfo, "js_arg");
            var target = TypeInfo.IsManagedReturnPosition
                ? Constants.ArgumentReturn
                : argName;

            var source = TypeInfo.IsManagedReturnPosition
                ? Argument(IdentifierName(context.GetIdentifiers(TypeInfo).native))
                : _inner.AsArgument(context);

            if (context.CurrentStage == StubIdentifierContext.Stage.UnmarshalCapture && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return ToManagedMethod(target, source);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Marshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && TypeInfo.IsManagedReturnPosition)
            {
                yield return ToJSMethod(target, source);
            }

            foreach (var x in base.Generate(context))
            {
                yield return x;
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.PinnedMarshal && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return ToJSMethod(target, source);
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Unmarshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsManagedReturnPosition)
            {
                yield return ToManagedMethod(target, source);
            }
        }

        protected virtual ArgumentSyntax ToManagedMethodRefOrOut(ArgumentSyntax argument)
        {
            return argument.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword));
        }

        protected virtual ArgumentSyntax ToJSMethodRefOrOut(ArgumentSyntax argument)
        {
            return argument;
        }

        private ExpressionStatementSyntax ToManagedMethod(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToManagedMethod(Type)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(ToManagedMethodRefOrOut(source)))));
        }

        private ExpressionStatementSyntax ToJSMethod(string target, ArgumentSyntax source)
        {
            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(target), GetToJSMethod(Type)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(ToJSMethodRefOrOut(source)))));
        }
    }
}
