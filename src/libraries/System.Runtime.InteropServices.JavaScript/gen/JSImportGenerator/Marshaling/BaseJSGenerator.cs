// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal abstract class BaseJSGenerator : IJSMarshallingGenerator
    {
        protected IBoundMarshallingGenerator _inner;
        public MarshalerType Type;

        protected BaseJSGenerator(MarshalerType marshalerType, IBoundMarshallingGenerator inner)
        {
            _inner = inner;
            Type = marshalerType;
        }

        public TypePositionInfo TypeInfo => _inner.TypeInfo;

        public StubCodeContext CodeContext => _inner.CodeContext;

        public ManagedTypeInfo NativeType => _inner.NativeType;

        public SignatureBehavior NativeSignatureBehavior => _inner.NativeSignatureBehavior;

        public ValueBoundaryBehavior ValueBoundaryBehavior => _inner.ValueBoundaryBehavior;

        public virtual bool UsesNativeIdentifier => _inner.UsesNativeIdentifier;

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
            => _inner.SupportsByValueMarshalKind(marshalKind, out diagnostic);

        public virtual IEnumerable<ExpressionSyntax> GenerateBind()
        {
            yield return MarshalerTypeName(Type);
        }

        public virtual IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            string argName = context.GetAdditionalIdentifier(TypeInfo, "js_arg");

            if (context.CurrentStage == StubIdentifierContext.Stage.Setup)
            {
                if (!TypeInfo.IsManagedReturnPosition)
                {
                    yield return LocalDeclarationStatement(VariableDeclaration(RefType(IdentifierName(Constants.JSMarshalerArgumentGlobal)))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(argName))
                    .WithInitializer(EqualsValueClause(RefExpression(ElementAccessExpression(IdentifierName(Constants.ArgumentsBuffer))
                    .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(TypeInfo.ManagedIndex + 2))))))))))));
                }
            }

            foreach (var x in _inner.Generate(context))
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
