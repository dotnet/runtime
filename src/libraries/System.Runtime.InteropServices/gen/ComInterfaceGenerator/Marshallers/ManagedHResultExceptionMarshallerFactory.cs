// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed record ManagedHResultExceptionMarshallingInfo : MarshallingInfo;

    internal sealed class ManagedHResultExceptionMarshallerFactory : IMarshallingGeneratorFactory
    {
        private readonly IMarshallingGeneratorFactory _inner;
        private readonly MarshalDirection _direction;

        public ManagedHResultExceptionMarshallerFactory(IMarshallingGeneratorFactory inner, MarshalDirection direction)
        {
            _inner = inner;
            _direction = direction;
        }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo)
            {
                if (_direction == MarshalDirection.UnmanagedToManaged)
                {
                    return new UnmanagedToManagedMarshaller();
                }
                return new ManagedToUnmanagedMarshaller();
            }
            else
            {
                return _inner.Create(info, context);
            }
        }

        private sealed class ManagedToUnmanagedMarshaller : IMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                Debug.Assert(info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo);

                if (context.CurrentStage != StubCodeContext.Stage.Unmarshal)
                {
                    yield break;
                }

                (_, string nativeIdentifier) = context.GetIdentifiers(info);

                // Marshal.ThrowExceptionForHR(<native>);
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            ParseName(TypeNames.System_Runtime_InteropServices_Marshal),
                            IdentifierName("ThrowExceptionForHR")),
                        ArgumentList(
                            SingletonSeparatedList(Argument(IdentifierName(nativeIdentifier))))));
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
            public bool IsSupported(TargetFramework target, Version version) => true;
            public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
        }

        private sealed class UnmanagedToManagedMarshaller : IMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                Debug.Assert(info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo);

                if (context.CurrentStage != StubCodeContext.Stage.Unmarshal)
                {
                    yield break;
                }

                (_, string nativeIdentifier) = context.GetIdentifiers(info);

                //<native> = 0; // S_OK
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(nativeIdentifier),
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0))))
                .WithSemicolonToken(
                    Token(
                        TriviaList(),
                        SyntaxKind.SemicolonToken,
                        TriviaList(
                            Comment("// S_OK"))));
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
            public bool IsSupported(TargetFramework target, Version version) => true;
            public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
        }
    }
}
