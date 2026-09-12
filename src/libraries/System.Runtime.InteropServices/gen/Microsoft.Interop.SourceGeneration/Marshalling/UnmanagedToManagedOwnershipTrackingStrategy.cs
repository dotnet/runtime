// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    /// <summary>
    /// Tracks the original unmanaged value and whether it has been replaced with a new value.
    /// </summary>
    /// <seealso cref="CleanupOwnedOriginalValueMarshalling"/>
    internal sealed class UnmanagedToManagedOwnershipTrackingStrategy(ICustomTypeMarshallingStrategy innerMarshaller) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(writer, context);
        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(writer, context);
        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(writer, context);

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            innerMarshaller.GenerateMarshalStatements(writer, context);
            // Only take ownership after the replacement has been marshalled successfully.
            writer.WriteLine($"{context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)} = true;");
        }

        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(writer, context);
        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(writer, context);

        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            innerMarshaller.GenerateSetupStatements(writer, context);
            writer.WriteLine($"bool {context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)} = false;");
            OwnershipTrackingHelpers.DeclareOriginalValueIdentifier(writer, TypeInfo, context, NativeType);
        }

        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(writer, context);
        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(writer, context);
    }

    /// <summary>
    /// Cleans the original unmanaged value once the ownership-tracking strategy has replaced it.
    /// </summary>
    internal sealed class CleanupOwnedOriginalValueMarshalling(ICustomTypeMarshallingStrategy innerMarshaller) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                return;
            }

            writer.WriteLine($"if ({context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)})");
            using (writer.WriteBlock())
            {
                innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(writer, new OwnedValueCodeContext(context));
            }
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
            {
                return;
            }

            writer.WriteLine($"if ({context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)})");
            using (writer.WriteBlock())
            {
                innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(writer, new OwnedValueCodeContext(context));
            }
        }

        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(writer, context);
        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateMarshalStatements(writer, context);
        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(writer, context);
        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(writer, context);
        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateSetupStatements(writer, context);
        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(writer, context);
        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(writer, context);
    }

    /// <summary>
    /// Caches and cleans the original native value when every path reaching cleanup owns that value.
    /// </summary>
    internal sealed class FreeAlwaysOwnedOriginalValueGenerator(IBoundMarshallingGenerator inner) : IBoundMarshallingGenerator
    {
        public ManagedTypeInfo NativeType => inner.NativeType;
        public TypePositionInfo TypeInfo => inner.TypeInfo;
        public StubCodeContext CodeContext => inner.CodeContext;
        public SignatureBehavior NativeSignatureBehavior => inner.NativeSignatureBehavior;
        public bool UsesNativeIdentifier => inner.UsesNativeIdentifier;
        public ValueBoundaryBehavior ValueBoundaryBehavior => inner.ValueBoundaryBehavior;

        public void Generate(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (context.CurrentStage == StubIdentifierContext.Stage.Setup)
            {
                inner.Generate(writer, new OwnedValueCodeContext(context));
                OwnershipTrackingHelpers.DeclareOriginalValueIdentifier(writer, TypeInfo, context, NativeType);
                return;
            }

            inner.Generate(writer, context.CurrentStage == StubIdentifierContext.Stage.CleanupCallerAllocated
                ? new OwnedValueCodeContext(context)
                : context);
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
            => inner.SupportsByValueMarshalKind(marshalKind, out diagnostic);
    }

    file sealed record OwnedValueCodeContext : StubIdentifierContext
    {
        private readonly StubIdentifierContext _innerContext;

        public OwnedValueCodeContext(StubIdentifierContext innerContext)
        {
            _innerContext = innerContext;
            CurrentStage = innerContext.CurrentStage;
        }

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            string managed = _innerContext.GetIdentifiers(info).managed;
            return (managed, _innerContext.GetAdditionalIdentifier(info, OwnershipTrackingHelpers.OriginalValueIdentifier));
        }

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name) => _innerContext.GetAdditionalIdentifier(info, name);
    }

    file static class OwnershipTrackingHelpers
    {
        public const string OwnOriginalValueIdentifier = "ownOriginal";
        public const string OriginalValueIdentifier = "original";

        public static void DeclareOriginalValueIdentifier(IndentedTextWriter writer, TypePositionInfo info, StubIdentifierContext context, ManagedTypeInfo nativeType)
        {
            writer.WriteLine($"{nativeType.FullTypeName} {context.GetAdditionalIdentifier(info, OriginalValueIdentifier)} = {context.GetIdentifiers(info).native};");
        }
    }
}
