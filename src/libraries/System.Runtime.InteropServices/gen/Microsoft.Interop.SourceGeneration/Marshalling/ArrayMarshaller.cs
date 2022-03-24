// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class ArrayMarshaller : IMarshallingGenerator
    {
        private readonly IMarshallingGenerator _manualMarshallingGenerator;
        private readonly TypeSyntax _elementType;
        private readonly bool _enablePinning;

        public ArrayMarshaller(IMarshallingGenerator manualMarshallingGenerator, TypeSyntax elementType, bool enablePinning)
        {
            _manualMarshallingGenerator = manualMarshallingGenerator;
            _elementType = elementType;
            _enablePinning = enablePinning;
        }

        public bool IsSupported(TargetFramework target, Version version)
        {
            return target is TargetFramework.Net && version.Major >= 7;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return ValueBoundaryBehavior.NativeIdentifier;
            }
            return _manualMarshallingGenerator.GetValueBoundaryBehavior(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _manualMarshallingGenerator.AsNativeType(info);
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return _manualMarshallingGenerator.GetNativeSignatureBehavior(info);
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return GeneratePinningPath(info, context);
            }
            return _manualMarshallingGenerator.Generate(info, context);
        }

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context)
        {
            if (context.SingleFrameSpansNativeContext && _enablePinning)
            {
                return false;
            }
            return marshalKind.HasFlag(ByValueContentsMarshalKind.Out);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return false;
            }
            return _manualMarshallingGenerator.UsesNativeIdentifier(info, context);
        }

        private bool IsPinningPathSupported(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && _enablePinning && !info.IsByRef && !info.IsManagedReturnPosition;
        }

        private IEnumerable<StatementSyntax> GeneratePinningPath(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifer, string nativeIdentifier) = context.GetIdentifiers(info);
            string byRefIdentifier = $"__byref_{managedIdentifer}";
            TypeSyntax arrayElementType = _elementType;
            if (context.CurrentStage == StubCodeContext.Stage.Marshal)
            {
                // [COMPAT] We use explicit byref calculations here instead of just using a fixed statement
                // since a fixed statement converts a zero-length array to a null pointer.
                // Many native APIs, such as GDI+, ICU, etc. validate that an array parameter is non-null
                // even when the passed in array length is zero. To avoid breaking customers that want to move
                // to source-generated interop in subtle ways, we explicitly pass a reference to the 0-th element
                // of an array as long as it is non-null, matching the behavior of the built-in interop system
                // for single-dimensional zero-based arrays.

                // ref <elementType> <byRefIdentifier> = ref <managedIdentifer> == null ? ref *(<elementType>*)0 : ref MemoryMarshal.GetArrayDataReference(<managedIdentifer>);
                PrefixUnaryExpressionSyntax nullRef =
                    PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression,
                        CastExpression(
                            PointerType(arrayElementType),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))));

                InvocationExpressionSyntax getArrayDataReference =
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                            IdentifierName("GetArrayDataReference")),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifer)))));

                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        RefType(arrayElementType))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier(byRefIdentifier))
                        .WithInitializer(EqualsValueClause(
                            RefExpression(ParenthesizedExpression(
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        IdentifierName(managedIdentifer),
                                        LiteralExpression(
                                            SyntaxKind.NullLiteralExpression)),
                                    RefExpression(nullRef),
                                    RefExpression(getArrayDataReference)))))))));
            }
            if (context.CurrentStage == StubCodeContext.Stage.Pin)
            {
                // fixed (<nativeType> <nativeIdentifier> = &Unsafe.As<elementType, byte>(ref <byrefIdentifier>))
                yield return FixedStatement(
                    VariableDeclaration(AsNativeType(info), SingletonSeparatedList(
                        VariableDeclarator(nativeIdentifier)
                            .WithInitializer(EqualsValueClause(
                                PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ParseTypeName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                        GenericName("As").AddTypeArgumentListArguments(
                                            arrayElementType,
                                            PredefinedType(Token(SyntaxKind.ByteKeyword)))))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName(byRefIdentifier))
                                        .WithRefKindKeyword(Token(SyntaxKind.RefKeyword)))))))),
                    EmptyStatement());
            }
        }
    }
}
