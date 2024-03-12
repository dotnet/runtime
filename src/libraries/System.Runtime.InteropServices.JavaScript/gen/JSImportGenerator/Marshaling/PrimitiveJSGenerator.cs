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
        public PrimitiveJSGenerator(MarshalerType marshalerType, IMarshallingGenerator inner)
            : base(marshalerType, inner)
        {
        }

        public PrimitiveJSGenerator(MarshalerType marshalerType)
            : base(marshalerType, new Forwarder())
        {
        }

        // TODO order parameters in such way that affinity capturing parameters are emitted first
        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            string argName = context.GetAdditionalIdentifier(info, "js_arg");
            var target = info.IsManagedReturnPosition
                ? Constants.ArgumentReturn
                : argName;

            var source = info.IsManagedReturnPosition
                ? Argument(IdentifierName(context.GetIdentifiers(info).native))
                : _inner.AsArgument(info, context);

            if (context.CurrentStage == StubCodeContext.Stage.UnmarshalCapture && context.Direction == MarshalDirection.ManagedToUnmanaged && info.IsManagedReturnPosition)
            {
                yield return ToManagedMethod(target, source);
            }

            if (context.CurrentStage == StubCodeContext.Stage.Marshal && context.Direction == MarshalDirection.UnmanagedToManaged && info.IsManagedReturnPosition)
            {
                yield return ToJSMethod(target, source);
            }

            foreach (var x in base.Generate(info, context))
            {
                yield return x;
            }

            if (context.CurrentStage == StubCodeContext.Stage.PinnedMarshal && context.Direction == MarshalDirection.ManagedToUnmanaged && !info.IsManagedReturnPosition)
            {
                yield return ToJSMethod(target, source);
            }

            if (context.CurrentStage == StubCodeContext.Stage.Unmarshal && context.Direction == MarshalDirection.UnmanagedToManaged && !info.IsManagedReturnPosition)
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
