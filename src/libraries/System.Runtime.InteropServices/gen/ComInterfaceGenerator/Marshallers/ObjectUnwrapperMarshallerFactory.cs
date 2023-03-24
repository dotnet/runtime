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
    internal sealed record ObjectUnwrapperInfo(TypeSyntax UnwrapperType) : MarshallingInfo;

    internal sealed class ObjectUnwrapperMarshallerFactory : IMarshallingGeneratorFactory
    {
        private readonly IMarshallingGeneratorFactory _inner;
        public ObjectUnwrapperMarshallerFactory(IMarshallingGeneratorFactory inner)
        {
            _inner = inner;
        }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
            => info.MarshallingAttributeInfo is ObjectUnwrapperInfo ? new Marshaller() : _inner.Create(info, context);

        private sealed class Marshaller : IMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => new PointerTypeInfo("void*", "void*", false);
            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                Debug.Assert(info.MarshallingAttributeInfo is ObjectUnwrapperInfo);
                TypeSyntax unwrapperType = ((ObjectUnwrapperInfo)info.MarshallingAttributeInfo).UnwrapperType;
                if (context.CurrentStage != StubCodeContext.Stage.Unmarshal)
                {
                    yield break;
                }

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

                // <managed> = (<managedType>)UnmanagedObjectUnwrapper.GetObjectFormUnmanagedWrapper<TUnmanagedUnwrapper>(<native>);
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(managedIdentifier),
                        CastExpression(
                            info.ManagedType.Syntax,
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ParseTypeName(TypeNames.UnmanagedObjectUnwrapper),
                                    GenericName(Identifier("GetObjectForUnmanagedWrapper"))
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList(
                                                    unwrapperType)))),
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(IdentifierName(nativeIdentifier))))))));
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.NativeIdentifier;
            public bool IsSupported(TargetFramework target, Version version) => true;
            public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
