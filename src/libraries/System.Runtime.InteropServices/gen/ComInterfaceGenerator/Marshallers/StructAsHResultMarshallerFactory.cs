﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class StructAsHResultMarshallerFactory : IMarshallingGeneratorFactory
    {
        private static readonly Marshaller s_marshaller = new();

        private readonly IMarshallingGeneratorFactory _inner;

        public StructAsHResultMarshallerFactory(IMarshallingGeneratorFactory inner)
        {
            _inner = inner;
        }

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            // Value type with MarshalAs(UnmanagedType.Error), to be marshalled as an unmanaged HRESULT.
            if (info is { ManagedType: ValueTypeInfo, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.Error, _) })
            {
                return ResolvedGenerator.Resolved(s_marshaller);
            }

            return _inner.Create(info, context);
        }

        private sealed class Marshaller : IMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => SpecialTypeInfo.Int32;

            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
            {
                var (managed, unmanaged) = context.GetIdentifiers(info);

                switch (context.CurrentStage)
                {
                    case StubCodeContext.Stage.Marshal:
                        if (MarshallerHelpers.GetMarshalDirection(info, context) is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                        {
                            // unmanaged = Unsafe.BitCast<managedType, int>(managed);
                            yield return ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(unmanaged),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ParseTypeName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                    GenericName(Identifier("BitCast"),
                                        TypeArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                info.ManagedType.Syntax,
                                                AsNativeType(info).Syntax
                                                })))),
                                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(managed)))))));
                        }
                        break;
                    case StubCodeContext.Stage.Unmarshal:
                        if (MarshallerHelpers.GetMarshalDirection(info, context) is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                        {
                            // managed = Unsafe.BitCast<int, managedType>(unmanaged);
                            yield return ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(managed),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ParseTypeName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                    GenericName(Identifier("BitCast"),
                                        TypeArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                AsNativeType(info).Syntax,
                                                info.ManagedType.Syntax
                                                })))),
                                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(unmanaged)))))));
                        }
                        break;
                    default:
                        break;
                }
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
            {
                return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
            }

            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
            {
                if (info.IsByRef)
                {
                    return ValueBoundaryBehavior.AddressOfNativeIdentifier;
                }

                return ValueBoundaryBehavior.NativeIdentifier;
            }

            public bool IsSupported(TargetFramework target, Version version) => target == TargetFramework.Net && version.Major >= 8;

            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);

            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
