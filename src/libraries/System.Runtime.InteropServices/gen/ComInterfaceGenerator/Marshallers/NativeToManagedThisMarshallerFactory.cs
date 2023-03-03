// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed record NativeThisInfo(ManagedTypeInfo TypeKeyType) : MarshallingInfo;

    internal sealed class NativeToManagedThisMarshallerFactory : IMarshallingGeneratorFactory
    {
        private readonly IMarshallingGeneratorFactory _inner;
        public NativeToManagedThisMarshallerFactory(IMarshallingGeneratorFactory inner)
        {
            _inner = inner;
        }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
            => info.MarshallingAttributeInfo is NativeThisInfo(ManagedTypeInfo typeKeyType) ? new Marshaller(typeKeyType) : _inner.Create(info, context);

        private sealed class Marshaller : IMarshallingGenerator
        {
            private readonly ManagedTypeInfo _typeKeyType;

            public Marshaller(ManagedTypeInfo typeKeyType) => _typeKeyType = typeKeyType;

            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => new PointerTypeInfo("void*", "void*", false);
            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                if (context.CurrentStage != StubCodeContext.Stage.Unmarshal)
                {
                    yield break;
                }

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

                // <managed> = IUnmanagedVirtualMethodTableProvider.GetObjectForUnmanagedWrapper<<managedType>>(<native>);
                yield return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                GenericName(Identifier(TypeNames.IUnmanagedVirtualMethodTableProvider),
                                    TypeArgumentList(
                                        SingletonSeparatedList(
                                            _typeKeyType.Syntax))),
                                GenericName(Identifier("GetObjectForUnmanagedWrapper"),
                                    TypeArgumentList(
                                        SingletonSeparatedList(
                                            info.ManagedType.Syntax)))),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(IdentifierName(nativeIdentifier)))))));
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.NativeIdentifier;
            public bool IsSupported(TargetFramework target, Version version) => true;
            public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
