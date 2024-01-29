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
    internal sealed class CustomTypeMarshallingGenerator : IMarshallingGenerator
    {
        private readonly ICustomTypeMarshallingStrategy _nativeTypeMarshaller;
        private readonly ByValueMarshalKindSupportDescriptor _byValueContentsMarshallingSupport;
        private readonly bool _isPinned;

        public CustomTypeMarshallingGenerator(ICustomTypeMarshallingStrategy nativeTypeMarshaller, ByValueMarshalKindSupportDescriptor byValueContentsMarshallingSupport, bool isPinned)
        {
            _nativeTypeMarshaller = nativeTypeMarshaller;
            _byValueContentsMarshallingSupport = byValueContentsMarshallingSupport;
            _isPinned = isPinned;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeMarshaller.AsNativeType(info);
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(info, context);
            // Although custom native type marshalling doesn't support [In] or [Out] by value marshalling,
            // other marshallers that wrap this one might, so we handle the correct cases here.
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    return _nativeTypeMarshaller.GenerateSetupStatements(info, context);
                case StubCodeContext.Stage.Marshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional
                        || (context.Direction == MarshalDirection.UnmanagedToManaged && ShouldGenerateByValueOutMarshalling(info, context)))
                    {
                        return _nativeTypeMarshaller.GenerateMarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.Pin:
                    if (context.SingleFrameSpansNativeContext && elementMarshalDirection is MarshalDirection.ManagedToUnmanaged)
                    {
                        return _nativeTypeMarshaller.GeneratePinStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.PinnedMarshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        return _nativeTypeMarshaller.GeneratePinnedMarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.NotifyForSuccessfulInvoke:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        return _nativeTypeMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.UnmarshalCapture:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                    {
                        return _nativeTypeMarshaller.GenerateUnmarshalCaptureStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (context.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling(info, context)))
                    {
                        return _nativeTypeMarshaller.GenerateUnmarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.GuaranteedUnmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (context.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling(info, context)))
                    {
                        return _nativeTypeMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.CleanupCallerAllocated:
                    return _nativeTypeMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(info, context);
                case StubCodeContext.Stage.CleanupCalleeAllocated:
                    return _nativeTypeMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(info, context);
                default:
                    break;
            }

            return Array.Empty<StatementSyntax>();
        }

        private bool ShouldGenerateByValueOutMarshalling(TypePositionInfo info, StubCodeContext context)
        {
            return
            info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out)
                && _byValueContentsMarshallingSupport.GetSupport(info.ByValueContentsMarshalKind, info, context, out _) != ByValueMarshalKindSupport.NotSupported
                && !info.IsByRef
                && !_isPinned;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _nativeTypeMarshaller.UsesNativeIdentifier(info, context);
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
        {
            return _byValueContentsMarshallingSupport.GetSupport(marshalKind, info, context, out diagnostic);
        }
    }
}
