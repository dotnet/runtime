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

        public IEnumerable<StatementSyntax> Generate(StubCodeContext context)
        {
            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(info, context);
            // Although custom native type marshalling doesn't support [In] or [Out] by value marshalling,
            // other marshallers that wrap this one might, so we handle the correct cases here.
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    return nativeTypeMarshaller.GenerateSetupStatements(info, context);
                case StubCodeContext.Stage.Marshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional
                        || (context.Direction == MarshalDirection.UnmanagedToManaged && ShouldGenerateByValueOutMarshalling(context)))
                    {
                        return nativeTypeMarshaller.GenerateMarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.Pin:
                    if (context.SingleFrameSpansNativeContext && elementMarshalDirection is MarshalDirection.ManagedToUnmanaged)
                    {
                        return nativeTypeMarshaller.GeneratePinStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.PinnedMarshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        return nativeTypeMarshaller.GeneratePinnedMarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.NotifyForSuccessfulInvoke:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        return nativeTypeMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.UnmarshalCapture:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                    {
                        return nativeTypeMarshaller.GenerateUnmarshalCaptureStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (context.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling(context)))
                    {
                        return nativeTypeMarshaller.GenerateUnmarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.GuaranteedUnmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (context.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling(context)))
                    {
                        return nativeTypeMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.CleanupCallerAllocated:
                    return nativeTypeMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(info, context);
                case StubCodeContext.Stage.CleanupCalleeAllocated:
                    return nativeTypeMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(info, context);
                default:
                    break;
            }

            return Array.Empty<StatementSyntax>();
        }

        private bool ShouldGenerateByValueOutMarshalling(StubCodeContext context)
        {
            return
            info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out)
                && byValueContentsMarshallingSupport.GetSupport(info.ByValueContentsMarshalKind, info, context, out _) != ByValueMarshalKindSupport.NotSupported
                && !info.IsByRef
                && !isPinned;
        }

        public bool UsesNativeIdentifier(StubCodeContext context)
        {
            return nativeTypeMarshaller.UsesNativeIdentifier(info, context);
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
        {
            return byValueContentsMarshallingSupport.GetSupport(marshalKind, info, context, out diagnostic);
        }

        public IBoundMarshallingGenerator Rebind(TypePositionInfo newInfo) => new CustomTypeMarshallingGenerator(newInfo, nativeTypeMarshaller, byValueContentsMarshallingSupport, isPinned);
    }
}
