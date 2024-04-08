// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    internal sealed class StructAsHResultMarshallerFactory : IMarshallingGeneratorResolver
    {
        private static readonly Marshaller s_marshaller = new();

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            // Value type with MarshalAs(UnmanagedType.Error), to be marshalled as an unmanaged HRESULT.
            if (info is { ManagedType: ValueTypeInfo, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.Error, _) })
            {
                return ResolvedGenerator.Resolved(s_marshaller);
            }

            return ResolvedGenerator.UnresolvedGenerator;
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
                            yield return AssignmentStatement(
                                IdentifierName(unmanaged),
                                MethodInvocation(
                                    ParseTypeName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                    GenericName(Identifier("BitCast"),
                                        TypeArgumentList(
                                            SeparatedList(new[]
                                                {
                                                    info.ManagedType.Syntax,
                                                    AsNativeType(info).Syntax
                                                }))),
                                    Argument(IdentifierName(managed))));
                        }
                        break;
                    case StubCodeContext.Stage.Unmarshal:
                        if (MarshallerHelpers.GetMarshalDirection(info, context) is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                        {
                            // managed = Unsafe.BitCast<int, managedType>(unmanaged);
                            yield return AssignmentStatement(
                            IdentifierName(managed),
                            MethodInvocation(
                                ParseTypeName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                GenericName(Identifier("BitCast"),
                                    TypeArgumentList(
                                        SeparatedList(new[]
                                            {
                                                AsNativeType(info).Syntax,
                                                info.ManagedType.Syntax
                                            }))),
                                Argument(IdentifierName(unmanaged))));
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

            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);

            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
