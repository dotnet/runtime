// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    internal sealed class StatefulValueMarshalling(TypePositionInfo info, StubCodeContext stubContext, ManagedTypeInfo marshallerType, ManagedTypeInfo unmanagedType, MarshallerShape shape) : ICustomTypeMarshallingStrategy
    {
        internal const string MarshallerIdentifier = "marshaller";

        public ManagedTypeInfo NativeType => unmanagedType;
        public bool UsesNativeIdentifier => true;
        public TypePositionInfo TypeInfo => info;
        public StubCodeContext CodeContext => stubContext;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, stubContext) is StubIdentifierContext.Stage.CleanupCallerAllocated
                && shape.HasFlag(MarshallerShape.Free))
            {
                writer.WriteLine($"{GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Free}();");
            }
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, stubContext) is StubIdentifierContext.Stage.CleanupCalleeAllocated
                && shape.HasFlag(MarshallerShape.Free))
            {
                writer.WriteLine($"{GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Free}();");
            }
        }

        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
            {
                writer.WriteLine($"{context.GetIdentifiers(info).managed} = {GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Value.Stateful.ToManagedFinally}();");
            }
        }

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.ToUnmanaged))
            {
                writer.WriteLine($"{GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Value.Stateful.FromManaged}({context.GetIdentifiers(info).managed});");
            }
        }

        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.ToUnmanaged) || shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
            {
                writer.WriteLine($"{context.GetIdentifiers(info).native} = {GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Value.Stateful.ToUnmanaged}();");
            }
        }

        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.ToManaged))
            {
                writer.WriteLine($"{context.GetIdentifiers(info).managed} = {GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Value.Stateful.ToManaged}();");
            }
        }

        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.ToManaged) || shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
            {
                writer.WriteLine($"{GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Value.Stateful.FromUnmanaged}({context.GetIdentifiers(info).native});");
            }
        }

        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            // Scoped marshaller locals prevent caller-allocated buffers from escaping through ref-like state.
            string scoped = marshallerType is ValueTypeInfo { IsByRefLike: true } ? "scoped " : string.Empty;
            writer.WriteLine($"{scoped}{marshallerType.FullTypeName} {GetMarshallerIdentifier(info, context)} = new();");
        }

        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.StatefulPinnableReference))
            {
                string unusedIdentifier = context.GetAdditionalIdentifier(info, "unused");
                writer.WriteLine($"fixed (void* {unusedIdentifier} = {GetMarshallerIdentifier(info, context)})");
            }
        }

        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.OnInvoked))
            {
                writer.WriteLine($"{GetMarshallerIdentifier(info, context)}.{ShapeMemberNames.Value.Stateful.OnInvoked}();");
            }
        }

        public static string GetMarshallerIdentifier(TypePositionInfo info, StubIdentifierContext context)
        {
            return context.GetAdditionalIdentifier(info, MarshallerIdentifier);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StatefulCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, string marshallerType, string bufferElementType) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(writer, context);
        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(writer, context);

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.CanUseCallerAllocatedBuffer(TypeInfo, CodeContext))
            {
                string managedIdentifier = context.GetIdentifiers(TypeInfo).managed;
                string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(TypeInfo, context);
                writer.WriteLine($"{marshaller}.{ShapeMemberNames.Value.Stateful.FromManaged}({managedIdentifier}, stackalloc {bufferElementType}[{marshallerType}.{ShapeMemberNames.BufferSize}]);");
                return;
            }

            innerMarshaller.GenerateMarshalStatements(writer, context);
        }

        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(writer, context);
        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(writer, context);
        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateSetupStatements(writer, context);
        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(writer, context);
        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(writer, context);
        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(writer, context);
        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
    }

    internal sealed class StatefulLinearCollectionSource(TypePositionInfo info, StubCodeContext codeContext) : IElementsMarshallingCollectionSource
    {
        public TypePositionInfo TypeInfo => info;
        public StubCodeContext CodeContext => codeContext;

        public string GetUnmanagedValuesDestination(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            return $"{marshaller}.{ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesDestination}()";
        }

        public string GetManagedValuesSource(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            return $"{marshaller}.{ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesSource}()";
        }

        public string GetUnmanagedValuesSource(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            return $"{marshaller}.{ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesSource}({numElementsIdentifier})";
        }

        public string GetManagedValuesDestination(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            return $"{marshaller}.{ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesDestination}({numElementsIdentifier})";
        }
    }

    /// <summary>
    /// Marshaller that marshals elements through a stateful linear collection marshaller.
    /// </summary>
    internal sealed class StatefulLinearCollectionMarshalling(
        ICustomTypeMarshallingStrategy innerMarshaller,
        MarshallerShape shape,
        CountInfo countInfo,
        bool castCountInfo,
        ElementsMarshalling elementsMarshalling,
        bool cleanupElements) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;
        public bool UsesNativeIdentifier => true;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            // The element marshaller chooses its cleanup stage. StatefulFreeMarshalling frees the container.
            if (cleanupElements)
            {
                elementsMarshalling.GenerateElementCleanupStatement(writer, context);
            }
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (cleanupElements)
            {
                elementsMarshalling.GenerateElementCleanupStatement(writer, context);
            }
        }

        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(writer, context);

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            innerMarshaller.GenerateMarshalStatements(writer, context);

            if (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // Clear output-only storage so cleanup cannot observe uninitialized elements.
                elementsMarshalling.GenerateClearUnmanagedDestination(writer, context);
                return;
            }
            if (CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                elementsMarshalling.GenerateUnmanagedToManagedByValueOutMarshalStatement(writer, context);
                return;
            }
            if (shape.HasFlag(MarshallerShape.ToUnmanaged) || shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
            {
                elementsMarshalling.GenerateMarshalStatement(writer, context);
            }
        }

        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(writer, context);
        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(writer, context);

        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            innerMarshaller.GenerateSetupStatements(writer, context);

            // A managed-to-unmanaged-only collection does not otherwise use a count local.
            if (MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext) is not MarshalDirection.ManagedToUnmanaged
                || TypeInfo.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default)
            {
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                writer.WriteLine($"int {numElementsIdentifier};");
                writer.WriteLine(MarshallerHelpers.SkipInitOrDefaultInit(
                    new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
                    {
                        InstanceIdentifier = numElementsIdentifier
                    }, context));
            }

            elementsMarshalling.GenerateSetupStatement(writer, context);
        }

        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                elementsMarshalling.GenerateManagedToUnmanagedByValueOutUnmarshalStatement(writer, context);
                return;
            }
            if (CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                elementsMarshalling.GenerateClearManagedValuesDestination(writer, context);
                return;
            }
            if (!shape.HasFlag(MarshallerShape.ToManaged))
            {
                return;
            }

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
            writer.WriteLine($"{numElementsIdentifier} = {ElementsMarshalling.GenerateNumElementsExpression(countInfo, castCountInfo, CodeContext, context)};");
            elementsMarshalling.GenerateUnmarshalStatement(writer, context);
            innerMarshaller.GenerateUnmarshalStatements(writer, context);
        }

        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(writer, context);
    }

    /// <summary>
    /// Marshaller that enables calling the Free method on a stateful marshaller.
    /// </summary>
    internal sealed class StatefulFreeMarshalling(ICustomTypeMarshallingStrategy innerMarshaller) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(writer, context);
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                writer.WriteLine($"{StatefulValueMarshalling.GetMarshallerIdentifier(TypeInfo, context)}.{ShapeMemberNames.Free}();");
            }
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(writer, context);
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCalleeAllocated)
            {
                writer.WriteLine($"{StatefulValueMarshalling.GetMarshallerIdentifier(TypeInfo, context)}.{ShapeMemberNames.Free}();");
            }
        }

        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(writer, context);
        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateMarshalStatements(writer, context);
        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(writer, context);
        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(writer, context);
        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateSetupStatements(writer, context);
        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(writer, context);
        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(writer, context);
    }
}
