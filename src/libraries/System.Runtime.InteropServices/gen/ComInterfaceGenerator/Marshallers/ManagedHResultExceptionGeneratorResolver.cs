// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Interop
{
    internal sealed record ManagedHResultExceptionMarshallingInfo(Guid InterfaceId) : MarshallingInfo;

    internal sealed class ManagedHResultExceptionGeneratorResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo)
            {
                return ResolvedGenerator.Resolved(context.Direction switch
                {
                    MarshalDirection.UnmanagedToManaged => new UnmanagedToManagedMarshaller().Bind(info, context),
                    MarshalDirection.ManagedToUnmanaged => new ManagedToUnmanagedMarshaller().Bind(info, context),
                    _ => throw new UnreachableException()
                });
            }
            else
            {
                return ResolvedGenerator.UnresolvedGenerator;
            }
        }

        private sealed class ManagedToUnmanagedMarshaller : IUnboundMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
            public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
            {
                ManagedHResultExceptionMarshallingInfo marshallingInfo = (ManagedHResultExceptionMarshallingInfo)info.MarshallingAttributeInfo;

                if (context.CurrentStage != StubIdentifierContext.Stage.Unmarshal)
                {
                    return;
                }

                (string managedIdentifier, _) = context.GetIdentifiers(info);

                string interfaceId = ComInterfaceGeneratorHelpers.CreateEmbeddedDataBlobExpression(marshallingInfo.InterfaceId.ToByteArray());
                writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_Marshal}.ThrowExceptionForHR({managedIdentifier}, new({interfaceId}), (nint){VirtualMethodPointerStubGenerator.NativeThisParameterIdentifier});");
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
        }

        private sealed class UnmanagedToManagedMarshaller : IUnboundMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
            public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
            {
                Debug.Assert(info.MarshallingAttributeInfo is ManagedHResultExceptionMarshallingInfo);

                if (context.CurrentStage != StubIdentifierContext.Stage.Unmarshal)
                {
                    return;
                }

                (string managedIdentifier, _) = context.GetIdentifiers(info);

                writer.WriteLine($"{managedIdentifier} = 0; // S_OK");
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
        }
    }
}
