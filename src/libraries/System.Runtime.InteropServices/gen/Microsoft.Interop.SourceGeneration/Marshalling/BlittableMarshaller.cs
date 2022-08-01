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
    public sealed class BlittableMarshaller : IMarshallingGenerator
    {
        public bool IsSupported(TargetFramework target, Version version) => true;

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return info.ManagedType.Syntax;
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
            else if (context.SingleFrameSpansNativeContext && !info.IsManagedReturnPosition)
            {
                return ValueBoundaryBehavior.NativeIdentifier;
            }
            return ValueBoundaryBehavior.AddressOfNativeIdentifier;
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (!info.IsByRef || info.IsManagedReturnPosition)
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            if (context.SingleFrameSpansNativeContext)
            {
                if (context.CurrentStage == StubCodeContext.Stage.Pin)
                {
                    yield return FixedStatement(
                        VariableDeclaration(
                            PointerType(AsNativeType(info)),
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

            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    break;
                case StubCodeContext.Stage.Marshal:
                    if (info.RefKind == RefKind.Ref)
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(managedIdentifier)));
                    }

                    break;
                case StubCodeContext.Stage.Unmarshal:
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(managedIdentifier),
                            IdentifierName(nativeIdentifier)));
                    break;
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef && !info.IsManagedReturnPosition && !context.SingleFrameSpansNativeContext;
        }

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
    }

}
