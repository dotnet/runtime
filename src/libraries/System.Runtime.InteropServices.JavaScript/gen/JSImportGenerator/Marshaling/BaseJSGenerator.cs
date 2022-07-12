// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal abstract class BaseJSGenerator : IJSMarshallingGenerator
    {
        protected IMarshallingGenerator _inner;
        public MarshalerType Type;

        protected BaseJSGenerator(MarshalerType marshalerType, IMarshallingGenerator inner)
        {
            _inner = inner;
            Type = marshalerType;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info) => _inner.AsNativeType(info);
        public ParameterSyntax AsParameter(TypePositionInfo info) => _inner.AsParameter(info);
        public bool IsSupported(TargetFramework target, Version version) => _inner.IsSupported(target, version);
        public virtual bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _inner.UsesNativeIdentifier(info, context);
        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => _inner.GetNativeSignatureBehavior(info);
        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => _inner.GetValueBoundaryBehavior(info, context);
        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => _inner.SupportsByValueMarshalKind(marshalKind, context);

        public virtual IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context)
        {
            yield return MarshalerTypeName(Type);
        }

        public virtual IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            string argName = context.GetAdditionalIdentifier(info, "js_arg");

            if (context.CurrentStage == StubCodeContext.Stage.Setup)
            {
                if (!info.IsManagedReturnPosition)
                {
                    yield return LocalDeclarationStatement(VariableDeclaration(RefType(IdentifierName(Constants.JSMarshalerArgumentGlobal)))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(argName))
                    .WithInitializer(EqualsValueClause(RefExpression(ElementAccessExpression(IdentifierName(Constants.ArgumentsBuffer))
                    .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(info.ManagedIndex + 2))))))))))));
                }
            }

            foreach (var x in _inner.Generate(info, context))
            {
                yield return x;
            }
        }

        protected static IdentifierNameSyntax MarshalerTypeName(MarshalerType marshalerType)
        {
            return IdentifierName(Constants.JSMarshalerTypeGlobalDot + marshalerType.ToString());
        }

        protected static IdentifierNameSyntax GetToManagedMethod(MarshalerType marshalerType)
        {
            switch (marshalerType)
            {
                case MarshalerType.BigInt64:
                    return IdentifierName(Constants.ToManagedBigMethod);
                default:
                    return IdentifierName(Constants.ToManagedMethod);
            }
        }

        protected static IdentifierNameSyntax GetToJSMethod(MarshalerType marshalerType)
        {
            switch (marshalerType)
            {
                case MarshalerType.BigInt64:
                    return IdentifierName(Constants.ToJSBigMethod);
                default:
                    return IdentifierName(Constants.ToJSMethod);
            }
        }
    }
}
