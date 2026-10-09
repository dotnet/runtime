// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    /// <summary>
    /// Implements generating code for an <see cref="ICustomTypeMarshallingStrategy"/> instance.
    /// </summary>
    internal sealed class CustomTypeMarshallingGenerator(ICustomTypeMarshallingStrategy nativeTypeMarshaller, ByValueMarshalKindSupportDescriptor byValueContentsMarshallingSupport, bool isPinned)
        : IBoundMarshallingGenerator
    {
        public ValueBoundaryBehavior ValueBoundaryBehavior => TypeInfo.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;

        public ManagedTypeInfo NativeType => nativeTypeMarshaller.NativeType;

        public SignatureBehavior NativeSignatureBehavior => TypeInfo.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;

        public TypePositionInfo TypeInfo => nativeTypeMarshaller.TypeInfo;

        public StubCodeContext CodeContext => nativeTypeMarshaller.CodeContext;

        public void Generate(IndentedTextWriter writer, StubIdentifierContext context)
        {
            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext);
            // Although custom native type marshalling doesn't support [In] or [Out] by value marshalling,
            // other marshallers that wrap this one might, so we handle the correct cases here.
            switch (context.CurrentStage)
            {
                case StubIdentifierContext.Stage.Setup:
                    nativeTypeMarshaller.GenerateSetupStatements(writer, context);
                    break;
                case StubIdentifierContext.Stage.Marshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional
                        || (CodeContext.Direction == MarshalDirection.UnmanagedToManaged && ShouldGenerateByValueOutMarshalling))
                    {
                        nativeTypeMarshaller.GenerateMarshalStatements(writer, context);
                    }
                    break;
                case StubIdentifierContext.Stage.Pin:
                    if (CodeContext.SingleFrameSpansNativeContext && elementMarshalDirection is MarshalDirection.ManagedToUnmanaged)
                    {
                        nativeTypeMarshaller.GeneratePinStatements(writer, context);
                    }
                    break;
                case StubIdentifierContext.Stage.PinnedMarshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        nativeTypeMarshaller.GeneratePinnedMarshalStatements(writer, context);
                    }
                    break;
                case StubIdentifierContext.Stage.NotifyForSuccessfulInvoke:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        nativeTypeMarshaller.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
                    }
                    break;
                case StubIdentifierContext.Stage.UnmarshalCapture:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                    {
                        nativeTypeMarshaller.GenerateUnmarshalCaptureStatements(writer, context);
                    }
                    break;
                case StubIdentifierContext.Stage.Unmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling))
                    {
                        nativeTypeMarshaller.GenerateUnmarshalStatements(writer, context);
                    }
                    break;
                case StubIdentifierContext.Stage.GuaranteedUnmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional
                        || (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && ShouldGenerateByValueOutMarshalling))
                    {
                        nativeTypeMarshaller.GenerateGuaranteedUnmarshalStatements(writer, context);
                    }
                    break;
                case StubIdentifierContext.Stage.CleanupCallerAllocated:
                    nativeTypeMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(writer, context);
                    break;
                case StubIdentifierContext.Stage.CleanupCalleeAllocated:
                    nativeTypeMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(writer, context);
                    break;
                default:
                    break;
            }
        }

        private bool ShouldGenerateByValueOutMarshalling
            => TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out)
                && byValueContentsMarshallingSupport.GetSupport(TypeInfo.ByValueContentsMarshalKind, TypeInfo, out _) != ByValueMarshalKindSupport.NotSupported
                && !TypeInfo.IsByRef
                && !isPinned;

        public bool UsesNativeIdentifier => nativeTypeMarshaller.UsesNativeIdentifier;

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
        {
            return byValueContentsMarshallingSupport.GetSupport(marshalKind, TypeInfo, out diagnostic);
        }
    }
}
