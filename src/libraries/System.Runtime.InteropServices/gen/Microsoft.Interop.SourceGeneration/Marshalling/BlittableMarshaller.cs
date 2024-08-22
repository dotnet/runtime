// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class BlittableMarshaller : IUnboundMarshallingGenerator
    {
        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return info.ManagedType;
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            if (!info.IsByRef)
            {
                return ValueBoundaryBehavior.ManagedIdentifier;
            }
            else if (context.SingleFrameSpansNativeContext && !context.IsInStubReturnPosition(info))
            {
                return ValueBoundaryBehavior.NativeIdentifier;
            }
            return ValueBoundaryBehavior.AddressOfNativeIdentifier;
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubIdentifierContext context)
        {
            if (!info.IsByRef || context.CodeContext.IsInStubReturnPosition(info))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            if (context.CodeContext.SingleFrameSpansNativeContext)
            {
                if (context.CurrentStage == StubIdentifierContext.Stage.Pin)
                {
                    yield return FixedStatement(
                        VariableDeclaration(
                            PointerType(AsNativeType(info).Syntax),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(nativeIdentifier))
                                    .WithInitializer(EqualsValueClause(
                                        PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                            IdentifierName(managedIdentifier))
                                    ))
                            )
                        ),
                        EmptyStatement()
                    );
                }
                yield break;
            }

            MarshalDirection direction = MarshallerHelpers.GetMarshalDirection(info, context.CodeContext);

            switch (context.CurrentStage)
            {
                case StubIdentifierContext.Stage.Setup:
                    break;
                case StubIdentifierContext.Stage.Marshal:
                    if (direction is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional && info.IsByRef)
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(managedIdentifier)));
                    }

                    break;
                case StubIdentifierContext.Stage.Unmarshal:
                    if (direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional && info.IsByRef)
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                IdentifierName(nativeIdentifier)));
                    }
                    break;
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef && !context.IsInStubReturnPosition(info) && !context.SingleFrameSpansNativeContext;
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);
    }
}
