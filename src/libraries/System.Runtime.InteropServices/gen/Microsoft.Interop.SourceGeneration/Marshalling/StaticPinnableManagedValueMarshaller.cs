// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class StaticPinnableManagedValueMarshaller(IBoundMarshallingGenerator innerMarshallingGenerator, TypeSyntax getPinnableReferenceType) : IBoundMarshallingGenerator
    {
        public TypePositionInfo TypeInfo => innerMarshallingGenerator.TypeInfo;

        public StubCodeContext CodeContext => innerMarshallingGenerator.CodeContext;

        public ManagedTypeInfo NativeType => innerMarshallingGenerator.NativeType;

        public SignatureBehavior NativeSignatureBehavior => innerMarshallingGenerator.NativeSignatureBehavior;

        public ValueBoundaryBehavior ValueBoundaryBehavior
        {
            get
            {
                if (IsPinningPathSupported(CodeContext))
                {
                    if (NativeType.Syntax is PointerTypeSyntax pointerType
                        && pointerType.ElementType is PredefinedTypeSyntax predefinedType
                        && predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
                    {
                        return Interop.ValueBoundaryBehavior.NativeIdentifier;
                    }

                    // Cast to native type if it is not void*
                    return Interop.ValueBoundaryBehavior.CastNativeIdentifier;
                }

                return innerMarshallingGenerator.ValueBoundaryBehavior;
            }
        }

        public IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            if (IsPinningPathSupported(CodeContext))
            {
                return GeneratePinningPath(context);
            }

            return innerMarshallingGenerator.Generate(context);
        }

        public bool UsesNativeIdentifier
        {
            get
            {
                if (IsPinningPathSupported(CodeContext))
                {
                    return false;
                }

                return innerMarshallingGenerator.UsesNativeIdentifier;
            }
        }

        private bool IsPinningPathSupported(StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && !TypeInfo.IsByRef && !context.IsInStubReturnPosition(TypeInfo);
        }

        private IEnumerable<StatementSyntax> GeneratePinningPath(StubIdentifierContext context)
        {
            if (context.CurrentStage == StubIdentifierContext.Stage.Pin)
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(innerMarshallingGenerator.TypeInfo);

                // fixed (void* <nativeIdentifier> = &<getPinnableReferenceType>.GetPinnableReference(<managedIdentifier>))
                yield return FixedStatement(
                    VariableDeclaration(
                        PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(nativeIdentifier))
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                            getPinnableReferenceType,
                                            IdentifierName(ShapeMemberNames.GetPinnableReference)),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName(managedIdentifier))))))
                                ))
                        )
                    ),
                    EmptyStatement());
            }
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
        {
            return innerMarshallingGenerator.SupportsByValueMarshalKind(marshalKind, out diagnostic);
        }
    }
}
