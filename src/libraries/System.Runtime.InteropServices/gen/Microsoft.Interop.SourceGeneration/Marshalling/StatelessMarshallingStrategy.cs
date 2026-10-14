// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    /// <summary>
    /// Stateless marshalling support for a type that has a custom unmanaged type.
    /// </summary>
    internal sealed class StatelessValueMarshalling(TypePositionInfo info, StubCodeContext codeContext, string marshallerType, ManagedTypeInfo unmanagedType, MarshallerShape shape) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => unmanagedType;
        public bool UsesNativeIdentifier => true;
        public TypePositionInfo TypeInfo => info;
        public StubCodeContext CodeContext => codeContext;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
                writer.WriteLine($"{managedIdentifier} = {marshallerType}.{ShapeMemberNames.Value.Stateless.ConvertToManagedFinally}({nativeIdentifier});");
            }
        }

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToUnmanaged) && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
            {
                return;
            }

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string convertToUnmanaged = $"{marshallerType}.{ShapeMemberNames.Value.Stateless.ConvertToUnmanaged}({managedIdentifier})";
            if (unmanagedType == SpecialTypeInfo.Void)
            {
                // Exception marshallers can marshal to void without producing a native value.
                writer.WriteLine($"{convertToUnmanaged};");
                return;
            }

            // Some exception marshallers return nint even when the signature's native type is a pointer.
            string cast = unmanagedType is PointerTypeInfo ? $"({unmanagedType.FullTypeName})" : string.Empty;
            writer.WriteLine($"{nativeIdentifier} = {cast}{convertToUnmanaged};");
        }

        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.ToManaged))
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
                writer.WriteLine($"{managedIdentifier} = {marshallerType}.{ShapeMemberNames.Value.Stateless.ConvertToManaged}({nativeIdentifier});");
            }
        }

        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }
    }

    /// <summary>
    /// Marshaller that enables support for caller-allocated stack buffers.
    /// </summary>
    internal sealed class StatelessCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, string marshallerType, string bufferElementType, bool isLinearCollectionMarshalling) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(writer, context);
        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(writer, context);
        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(writer, context);

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (!MarshallerHelpers.CanUseCallerAllocatedBuffer(TypeInfo, CodeContext))
            {
                innerMarshaller.GenerateMarshalStatements(writer, context);
                return;
            }

            string bufferIdentifier = context.GetAdditionalIdentifier(TypeInfo, "buffer");
            writer.WriteLine($"{TypeNames.System_Span}<{bufferElementType}> {bufferIdentifier} = stackalloc {bufferElementType}[{marshallerType}.{ShapeMemberNames.BufferSize}];");

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);
            if (isLinearCollectionMarshalling)
            {
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                writer.WriteLine($"{nativeIdentifier} = {marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements}({managedIdentifier}, {bufferIdentifier}, out {numElementsIdentifier});");
                innerMarshaller.GenerateMarshalStatements(writer, context);
            }
            else
            {
                writer.WriteLine($"{nativeIdentifier} = {marshallerType}.{ShapeMemberNames.Value.Stateless.ConvertToUnmanaged}({managedIdentifier}, {bufferIdentifier});");
            }
        }

        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(writer, context);
        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(writer, context);
        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateSetupStatements(writer, context);
        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(writer, context);
        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(writer, context);
        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
    }

    internal sealed class StatelessFreeMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, string marshallerType) : ICustomTypeMarshallingStrategy
    {
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                return;
            }

            innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(writer, context);
            writer.WriteLine($"{marshallerType}.{ShapeMemberNames.Free}({context.GetIdentifiers(TypeInfo).native});");
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
            {
                return;
            }

            innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(writer, context);
            writer.WriteLine($"{marshallerType}.{ShapeMemberNames.Free}({context.GetIdentifiers(TypeInfo).native});");
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
    /// Allocates storage through a stateless linear collection marshaller.
    /// </summary>
    internal sealed class StatelessLinearCollectionSpaceAllocator(TypePositionInfo info, StubCodeContext codeContext, string marshallerType, ManagedTypeInfo unmanagedType, MarshallerShape shape, CountInfo countInfo, bool countInfoRequiresCast) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => unmanagedType;
        public TypePositionInfo TypeInfo => info;
        public StubCodeContext CodeContext => codeContext;
        public bool UsesNativeIdentifier => true;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated
                && MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext) != MarshalDirection.ManagedToUnmanaged)
            {
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                writer.WriteLine($"{numElementsIdentifier} = {ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)};");
            }
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCalleeAllocated
                && MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext) != MarshalDirection.ManagedToUnmanaged)
            {
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                writer.WriteLine($"{numElementsIdentifier} = {ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)};");
            }
        }

        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
            {
                return;
            }

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
            writer.WriteLine($"{numElementsIdentifier} = {ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)};");
            writer.WriteLine($"{managedIdentifier} = {marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElementsFinally}({nativeIdentifier}, {numElementsIdentifier});");
        }

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (shape.HasFlag(MarshallerShape.ToUnmanaged)
                && !(shape.HasFlag(MarshallerShape.CallerAllocatedBuffer)
                    && MarshallerHelpers.CanUseCallerAllocatedBuffer(TypeInfo, CodeContext)))
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                writer.WriteLine($"{nativeIdentifier} = {marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements}({managedIdentifier}, out {numElementsIdentifier});");
            }
        }

        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
            writer.WriteLine($"int {numElementsIdentifier};");
            writer.WriteLine(MarshallerHelpers.SkipInitOrDefaultInit(
                new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
                {
                    InstanceIdentifier = numElementsIdentifier
                }, context));
        }

        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                return;
            }
            if (!shape.HasFlag(MarshallerShape.ToManaged))
            {
                return;
            }

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
            writer.WriteLine($"{numElementsIdentifier} = {ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)};");
            writer.WriteLine($"{managedIdentifier} = {marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElements}({nativeIdentifier}, {numElementsIdentifier});");
        }
    }

    internal sealed class StatelessLinearCollectionSource(TypePositionInfo info, StubCodeContext codeContext, string marshallerType) : IElementsMarshallingCollectionSource
    {
        public TypePositionInfo TypeInfo => info;
        public StubCodeContext CodeContext => codeContext;

        public string GetUnmanagedValuesDestination(StubIdentifierContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string nativeIdentifier = context.GetIdentifiers(info).native;
            return $"{marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination}({nativeIdentifier}, {numElementsIdentifier})";
        }

        public string GetManagedValuesSource(StubIdentifierContext context)
        {
            string managedIdentifier = context.GetIdentifiers(info).managed;
            return $"{marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource}({managedIdentifier})";
        }

        public string GetUnmanagedValuesSource(StubIdentifierContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string nativeIdentifier = context.GetIdentifiers(info).native;
            return $"{marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesSource}({nativeIdentifier}, {numElementsIdentifier})";
        }

        public string GetManagedValuesDestination(StubIdentifierContext context)
        {
            string managedIdentifier = context.GetIdentifiers(info).managed;
            return $"{marshallerType}.{ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination}({managedIdentifier})";
        }
    }

    /// <summary>
    /// Marshals collection elements using storage supplied by a stateless collection marshaller.
    /// </summary>
    internal sealed class StatelessLinearCollectionMarshalling(
        ICustomTypeMarshallingStrategy spaceMarshallingStrategy,
        ElementsMarshalling elementsMarshalling,
        ManagedTypeInfo unmanagedType,
        MarshallerShape shape,
        CountInfo countInfo,
        bool castCountInfo,
        bool cleanupElementsAndSpace) : ICustomTypeMarshallingStrategy
    {
        public bool UsesNativeIdentifier => true;
        public TypePositionInfo TypeInfo => spaceMarshallingStrategy.TypeInfo;
        public StubCodeContext CodeContext => spaceMarshallingStrategy.CodeContext;
        public ManagedTypeInfo NativeType => unmanagedType;

        public void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (!cleanupElementsAndSpace)
            {
                return;
            }

            var cleanupWriter = new IndentedTextWriter();
            elementsMarshalling.GenerateElementCleanupStatement(cleanupWriter, context);
            if (cleanupWriter.Length != 0)
            {
                if (!CodeContext.AdditionalTemporaryStateLivesAcrossStages)
                {
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                    if (countInfo is NoCountInfo && MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext) == MarshalDirection.ManagedToUnmanaged)
                    {
                        // The count is unavailable in this nested cleanup case.
                        // See https://github.com/dotnet/runtime/issues/93423.
                        writer.WriteLine($"{numElementsIdentifier} = 0;");
                    }
                    else
                    {
                        writer.WriteLine($"{numElementsIdentifier} = {ElementsMarshalling.GenerateNumElementsExpression(countInfo, castCountInfo, CodeContext, context)};");
                    }
                }
                writer.Write(cleanupWriter.ToString());
            }

            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                spaceMarshallingStrategy.GenerateCleanupCallerAllocatedResourcesStatements(writer, context);
            }
        }

        public void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (!cleanupElementsAndSpace)
            {
                return;
            }

            var cleanupWriter = new IndentedTextWriter();
            elementsMarshalling.GenerateElementCleanupStatement(cleanupWriter, context);
            if (cleanupWriter.Length != 0)
            {
                if (!CodeContext.AdditionalTemporaryStateLivesAcrossStages)
                {
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                    writer.WriteLine($"{numElementsIdentifier} = {ElementsMarshalling.GenerateNumElementsExpression(countInfo, castCountInfo, CodeContext, context)};");
                }
                writer.Write(cleanupWriter.ToString());
            }

            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                spaceMarshallingStrategy.GenerateCleanupCalleeAllocatedResourcesStatements(writer, context);
            }
        }

        public void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => spaceMarshallingStrategy.GenerateGuaranteedUnmarshalStatements(writer, context);

        public void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                elementsMarshalling.GenerateClearUnmanagedDestination(writer, context);
                return;
            }
            if (CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                elementsMarshalling.GenerateUnmanagedToManagedByValueOutMarshalStatement(writer, context);
                return;
            }

            spaceMarshallingStrategy.GenerateMarshalStatements(writer, context);
            if (shape.HasFlag(MarshallerShape.ToUnmanaged) || shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
            {
                elementsMarshalling.GenerateMarshalStatement(writer, context);
            }
        }

        public void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context) => spaceMarshallingStrategy.GenerateNotifyForSuccessfulInvokeStatements(writer, context);
        public void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context) => spaceMarshallingStrategy.GeneratePinnedMarshalStatements(writer, context);
        public void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context) => spaceMarshallingStrategy.GeneratePinStatements(writer, context);

        public void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
            spaceMarshallingStrategy.GenerateSetupStatements(writer, context);
            elementsMarshalling.GenerateSetupStatement(writer, context);
        }

        public void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context)
        {
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
                spaceMarshallingStrategy.GenerateUnmarshalStatements(writer, context);
                return;
            }
            if (!shape.HasFlag(MarshallerShape.ToManaged))
            {
                return;
            }

            spaceMarshallingStrategy.GenerateUnmarshalStatements(writer, context);
            elementsMarshalling.GenerateUnmarshalStatement(writer, context);
        }
    }
}
