// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed record ComInterfaceDispatchMarshallingInfo : MarshallingInfo
    {
        public static readonly ComInterfaceDispatchMarshallingInfo Instance = new();
    }

    internal sealed class ComInterfaceDispatchMarshallerFactory : IMarshallingGeneratorFactory
    {
        private readonly IMarshallingGeneratorFactory _inner;
        public ComInterfaceDispatchMarshallerFactory(IMarshallingGeneratorFactory inner)
        {
            _inner = inner;
        }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
            => info.MarshallingAttributeInfo is ComInterfaceDispatchMarshallingInfo ? new Marshaller() : _inner.Create(info, context);

        private sealed class Marshaller : IMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) =>
                new PointerTypeInfo(
                    $"{TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch}*",
                    $"{TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch}*",
                    IsFunctionPointer: false);
            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                if (context.CurrentStage != StubCodeContext.Stage.Unmarshal)
                {
                    yield break;
                }

                var (managed, native) = context.GetIdentifiers(info);

                // <managed> = ComWrappers.ComInterfaceDispatch.GetInstance<<managedType>>(<native>);
                yield return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(managed),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                ParseName(TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch),
                                GenericName(
                                    Identifier("GetInstance"),
                                    TypeArgumentList(SingletonSeparatedList(info.ManagedType.Syntax)))),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        IdentifierName(native)))))));
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.NativeIdentifier;
            public bool IsSupported(TargetFramework target, Version version)
                => target == TargetFramework.Net && version >= new Version(5, 0);
            public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
