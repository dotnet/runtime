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
    public sealed class StaticPinnableManagedValueMarshaller : IMarshallingGenerator
    {
        private readonly IMarshallingGenerator _innerMarshallingGenerator;
        private readonly TypeSyntax _getPinnableReferenceType;

        public StaticPinnableManagedValueMarshaller(IMarshallingGenerator innerMarshallingGenerator, TypeSyntax getPinnableReferenceType)
        {
            _innerMarshallingGenerator = innerMarshallingGenerator;
            _getPinnableReferenceType = getPinnableReferenceType;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                if (AsNativeType(info).Syntax is PointerTypeSyntax pointerType
                    && pointerType.ElementType is PredefinedTypeSyntax predefinedType
                    && predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
                {
                    return ValueBoundaryBehavior.NativeIdentifier;
                }

                // Cast to native type if it is not void*
                return ValueBoundaryBehavior.CastNativeIdentifier;
            }

            return _innerMarshallingGenerator.GetValueBoundaryBehavior(info, context);
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _innerMarshallingGenerator.AsNativeType(info);
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return _innerMarshallingGenerator.GetNativeSignatureBehavior(info);
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return GeneratePinningPath(info, context);
            }

            return _innerMarshallingGenerator.Generate(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return false;
            }

            return _innerMarshallingGenerator.UsesNativeIdentifier(info, context);
        }
        private static bool IsPinningPathSupported(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && !info.IsByRef && !context.IsInStubReturnPosition(info);
        }

        private IEnumerable<StatementSyntax> GeneratePinningPath(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage == StubCodeContext.Stage.Pin)
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

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
                                            _getPinnableReferenceType,
                                            IdentifierName(ShapeMemberNames.GetPinnableReference)),
                                        ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName(managedIdentifier))))))
                                ))
                        )
                    ),
                    EmptyStatement());
            }
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
        {
            return _innerMarshallingGenerator.SupportsByValueMarshalKind(marshalKind, info, context, out diagnostic);
        }
    }
}
