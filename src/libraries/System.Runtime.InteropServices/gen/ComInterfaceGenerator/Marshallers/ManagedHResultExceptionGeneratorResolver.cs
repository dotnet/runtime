// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    internal sealed record ManagedHResultExceptionMarshallingInfo : MarshallingInfo;

    internal sealed class ManagedHResultExceptionGeneratorResolver : IMarshallingGeneratorResolver
    {
        private readonly MarshalDirection _direction;

        public ManagedHResultExceptionGeneratorResolver(MarshalDirection direction)
        {
            if (direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.UnmanagedToManaged))
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }
            _direction = direction;
        }

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo)
            {
                return ResolvedGenerator.Resolved(_direction switch
                {
                    MarshalDirection.UnmanagedToManaged => new UnmanagedToManagedMarshaller(),
                    MarshalDirection.ManagedToUnmanaged => new ManagedToUnmanagedMarshaller(),
                    _ => throw new UnreachableException()
                });
            }
            else
            {
                return ResolvedGenerator.UnresolvedGenerator;
            }
        }

        private sealed class ManagedToUnmanagedMarshaller : IMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                Debug.Assert(info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo);

                if (context.CurrentStage != StubCodeContext.Stage.NotifyForSuccessfulInvoke)
                {
                    yield break;
                }

                (string managedIdentifier, _) = context.GetIdentifiers(info);

                // Marshal.ThrowExceptionForHR(<managed>);
                yield return MethodInvocationStatement(
                                TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                                IdentifierName("ThrowExceptionForHR"),
                                Argument(IdentifierName(managedIdentifier)));
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
        }

        private sealed class UnmanagedToManagedMarshaller : IMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                Debug.Assert(info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo);

                if (context.CurrentStage != StubCodeContext.Stage.NotifyForSuccessfulInvoke)
                {
                    yield break;
                }

                (string managedIdentifier, _) = context.GetIdentifiers(info);

                //<managed> = 0; // S_OK
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(managedIdentifier),
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
            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
        }
    }
}
