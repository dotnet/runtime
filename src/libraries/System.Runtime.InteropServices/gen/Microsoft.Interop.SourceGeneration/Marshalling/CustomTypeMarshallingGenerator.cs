// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Implements generating code for an <see cref="ICustomTypeMarshallingStrategy"/> instance.
    /// </summary>
    internal sealed class CustomTypeMarshallingGenerator(TypePositionInfo info, ICustomTypeMarshallingStrategy nativeTypeMarshaller, ByValueMarshalKindSupportDescriptor byValueContentsMarshallingSupport, bool isPinned)
        : IBoundMarshallingGenerator
    {
        public ValueBoundaryBehavior GetValueBoundaryBehavior(StubCodeContext context)
        {
            return info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;
        }

        public ManagedTypeInfo NativeType => nativeTypeMarshaller.AsNativeType(info);

        public SignatureBehavior NativeSignatureBehavior => info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;

        public TypePositionInfo TypeInfo => info;

        public IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(info, context.CodeContext);
            // Although custom native type marshalling doesn't support [In] or [Out] by value marshalling,
            // other marshallers that wrap this one might, so we handle the correct cases here.
            switch (context.CurrentStage)
            {
                case StubIdentifierContext.Stage.Setup:
                    return nativeTypeMarshaller.GenerateSetupStatements(info, context);
                case StubIdentifierContext.Stage.Marshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional
                        || (context.CodeContext.Direction == MarshalDirection.UnmanagedToManaged && ShouldGenerateByValueOutMarshalling))
                    {
                        return nativeTypeMarshaller.GenerateMarshalStatements(info, context);
                    }
                    break;
                case StubIdentifierContext.Stage.Pin:
                    if (context.CodeContext.SingleFrameSpansNativeContext && elementMarshalDirection is MarshalDirection.ManagedToUnmanaged)
                    {
                        return nativeTypeMarshaller.GeneratePinStatements(info, context);
                    }
                    break;
                case StubIdentifierContext.Stage.PinnedMarshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        return nativeTypeMarshaller.GeneratePinnedMarshalStatements(info, context);
                    }
                    break;
                case StubIdentifierContext.Stage.NotifyForSuccessfulInvoke:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        return nativeTypeMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
                    }
                    break;
                case StubIdentifierContext.Stage.UnmarshalCapture:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                    {
                        return nativeTypeMarshaller.GenerateUnmarshalCaptureStatements(info, context);
                    }
                    break;
                case StubIdentifierContext.Stage.Unmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (context.CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling))
                    {
                        return nativeTypeMarshaller.GenerateUnmarshalStatements(info, context);
                    }
                    break;
                case StubIdentifierContext.Stage.GuaranteedUnmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (context.CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling))
                    {
                        return nativeTypeMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
                    }
                    break;
                case StubIdentifierContext.Stage.CleanupCallerAllocated:
                    return nativeTypeMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(info, context);
                case StubIdentifierContext.Stage.CleanupCalleeAllocated:
                    return nativeTypeMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(info, context);
                default:
                    break;
            }

            return Array.Empty<StatementSyntax>();
        }

        private bool ShouldGenerateByValueOutMarshalling
            => info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out)
                && byValueContentsMarshallingSupport.GetSupport(info.ByValueContentsMarshalKind, info, out _) != ByValueMarshalKindSupport.NotSupported
                && !info.IsByRef
                && !isPinned;

        public bool UsesNativeIdentifier(StubCodeContext context)
        {
            return nativeTypeMarshaller.UsesNativeIdentifier(info, context);
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
        {
            return byValueContentsMarshallingSupport.GetSupport(marshalKind, info, out diagnostic);
        }

        public IBoundMarshallingGenerator Rebind(TypePositionInfo newInfo) => new CustomTypeMarshallingGenerator(newInfo, nativeTypeMarshaller, byValueContentsMarshallingSupport, isPinned);
    }
}
