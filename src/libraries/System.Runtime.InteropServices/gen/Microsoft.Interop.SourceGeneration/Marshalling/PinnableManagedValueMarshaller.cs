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
    public sealed class PinnableManagedValueMarshaller : IMarshallingGenerator
    {
        private readonly IMarshallingGenerator _innerMarshallingGenerator;

        public PinnableManagedValueMarshaller(IMarshallingGenerator innerMarshallingGenerator)
        {
            _innerMarshallingGenerator = innerMarshallingGenerator;
        }

        public bool IsSupported(TargetFramework target, Version version)
            => _innerMarshallingGenerator.IsSupported(target, version);

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                if (AsNativeType(info) is PointerTypeSyntax pointerType
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

        public TypeSyntax AsNativeType(TypePositionInfo info)
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

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context)
        {
            return _innerMarshallingGenerator.SupportsByValueMarshalKind(marshalKind, context);
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
            return context.SingleFrameSpansNativeContext && !info.IsByRef && !info.IsManagedReturnPosition;
        }

        private static IEnumerable<StatementSyntax> GeneratePinningPath(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage == StubCodeContext.Stage.Pin)
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

                // fixed (void* <nativeIdentifier> = <managedIdentifier>)
                yield return FixedStatement(
                    VariableDeclaration(
                        PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(nativeIdentifier))
                                .WithInitializer(EqualsValueClause(
                                    IdentifierName(managedIdentifier)
                                ))
                        )
                    ),
                    EmptyStatement());
            }
        }
    }
}
