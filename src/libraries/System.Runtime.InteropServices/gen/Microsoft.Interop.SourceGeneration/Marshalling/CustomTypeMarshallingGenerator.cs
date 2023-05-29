// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Implements generating code for an <see cref="ICustomTypeMarshallingStrategy"/> instance.
    /// </summary>
    internal sealed class CustomTypeMarshallingGenerator : IMarshallingGenerator
    {
        private readonly ICustomTypeMarshallingStrategy _nativeTypeMarshaller;
        private readonly bool _enableByValueContentsMarshalling;

        public CustomTypeMarshallingGenerator(ICustomTypeMarshallingStrategy nativeTypeMarshaller, bool enableByValueContentsMarshalling)
        {
            _nativeTypeMarshaller = nativeTypeMarshaller;
            _enableByValueContentsMarshalling = enableByValueContentsMarshalling;
        }

        public bool IsSupported(TargetFramework target, Version version)
        {
            return target is TargetFramework.Net && version.Major >= 6;
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
                        || (context.Direction == MarshalDirection.UnmanagedToManaged && ShouldGenerateByValueOutMarshalling(info)))
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
                        || (context.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling(info)))
                    {
                        return _nativeTypeMarshaller.GenerateUnmarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.GuaranteedUnmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (context.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling(info)))
                    {
                        return _nativeTypeMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    return _nativeTypeMarshaller.GenerateCleanupStatements(info, context);
                default:
                    break;
            }

            return Array.Empty<StatementSyntax>();
        }

        private bool ShouldGenerateByValueOutMarshalling(TypePositionInfo info)
        {
            return _enableByValueContentsMarshalling && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out);
        }

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context)
        {
            return _enableByValueContentsMarshalling;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _nativeTypeMarshaller.UsesNativeIdentifier(info, context);
        }
    }
}
