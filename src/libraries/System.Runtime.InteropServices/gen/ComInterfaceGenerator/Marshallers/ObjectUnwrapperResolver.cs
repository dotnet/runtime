// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    internal sealed record ObjectUnwrapperInfo(TypeSyntax UnwrapperType) : MarshallingInfo;

    internal sealed class ObjectUnwrapperResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
            => info.MarshallingAttributeInfo is ObjectUnwrapperInfo ? ResolvedGenerator.Resolved(new Marshaller()) : ResolvedGenerator.UnresolvedGenerator;

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
                yield return AssignmentStatement(
                        IdentifierName(managedIdentifier),
                        CastExpression(
                            info.ManagedType.Syntax,
                            MethodInvocation(
                                    TypeSyntaxes.UnmanagedObjectUnwrapper,
                                    GenericName(Identifier("GetObjectForUnmanagedWrapper"))
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList(
                                                    unwrapperType))),
                                    Argument(IdentifierName(nativeIdentifier)))));
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.NativeIdentifier;
            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
