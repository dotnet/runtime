// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal interface IElementsMarshallingCollectionSource
    {
        TypePositionInfo TypeInfo { get; }
        StubCodeContext CodeContext { get; }

        string GetUnmanagedValuesDestination(StubIdentifierContext context);
        string GetManagedValuesSource(StubIdentifierContext context);
        string GetUnmanagedValuesSource(StubIdentifierContext context);
        string GetManagedValuesDestination(StubIdentifierContext context);
    }

    internal abstract class ElementsMarshalling
    {
        protected const string MemoryMarshalType = TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_MemoryMarshal;
        protected const string UnsafeType = TypeNames.GlobalAlias + TypeNames.System_Runtime_CompilerServices_Unsafe;

        protected IElementsMarshallingCollectionSource CollectionSource { get; }

        protected ElementsMarshalling(IElementsMarshallingCollectionSource collectionSource)
        {
            CollectionSource = collectionSource;
        }

        public void GenerateClearUnmanagedDestination(IndentedTextWriter writer, StubIdentifierContext context)
        {
            writer.WriteLine($"{CollectionSource.GetUnmanagedValuesDestination(context)}.Clear();");
        }

        public void GenerateClearManagedValuesDestination(IndentedTextWriter writer, StubIdentifierContext context)
        {
            writer.WriteLine($"{CollectionSource.GetManagedValuesDestination(context)}.Clear();");
        }

        public static string GenerateNumElementsExpression(CountInfo count, bool countInfoRequiresCast, StubCodeContext codeContext, StubIdentifierContext context)
        {
            (string expression, bool checkedAddition) = count switch
            {
                SizeAndParamIndexInfo(int size, SizeAndParamIndexInfo.UnspecifiedParam) => (GetConstSizeExpression(size), false),
                ConstSizeCountInfo(int size) => (GetConstSizeExpression(size), false),
                SizeAndParamIndexInfo(SizeAndParamIndexInfo.UnspecifiedConstSize, TypePositionInfo param) => (GetExpressionForParam(param), false),
                SizeAndParamIndexInfo(int size, TypePositionInfo param) => ($"{GetConstSizeExpression(size)} + {GetExpressionForParam(param)}", true),
                CountElementCountInfo(TypePositionInfo elementInfo) => (GetExpressionForParam(elementInfo), false),
                _ => throw new UnreachableException("Count info should have been verified in generator resolution")
            };

            if (countInfoRequiresCast)
            {
                // Both the addition and its conversion to int must be checked before using the count.
                return $"checked((int)({expression}))";
            }

            return checkedAddition ? $"checked({expression})" : expression;

            static string GetConstSizeExpression(int size) => size.ToString(CultureInfo.InvariantCulture);

            string GetExpressionForParam(TypePositionInfo paramInfo)
                => MarshallerHelpers.GetIndexedManagedElementExpression(paramInfo, codeContext, context);
        }

        public abstract void GenerateSetupStatement(IndentedTextWriter writer, StubIdentifierContext context);
        public abstract void GenerateUnmanagedToManagedByValueOutMarshalStatement(IndentedTextWriter writer, StubIdentifierContext context);
        public abstract void GenerateMarshalStatement(IndentedTextWriter writer, StubIdentifierContext context);
        public abstract void GenerateManagedToUnmanagedByValueOutUnmarshalStatement(IndentedTextWriter writer, StubIdentifierContext context);
        public abstract void GenerateUnmarshalStatement(IndentedTextWriter writer, StubIdentifierContext context);
        public abstract void GenerateElementCleanupStatement(IndentedTextWriter writer, StubIdentifierContext context);
    }

    file static class ElementsMarshallingCollectionSourceExtensions
    {
        public static void GenerateNumElementsAssignmentFromManagedValuesSource(this IElementsMarshallingCollectionSource source, IndentedTextWriter writer, TypePositionInfo info, StubIdentifierContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            writer.WriteLine($"{numElementsIdentifier} = {source.GetManagedValuesSource(context)}.Length;");
        }

        public static void GenerateNumElementsAssignmentFromManagedValuesDestination(this IElementsMarshallingCollectionSource source, IndentedTextWriter writer, TypePositionInfo info, StubIdentifierContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            writer.WriteLine($"{numElementsIdentifier} = {source.GetManagedValuesDestination(context)}.Length;");
        }
    }

    /// <summary>
    /// Support for marshalling blittable elements.
    /// </summary>
    internal sealed class BlittableElementsMarshalling(
        string managedElementType,
        string unmanagedElementType,
        IElementsMarshallingCollectionSource collectionSource) : ElementsMarshalling(collectionSource)
    {
        public override void GenerateUnmanagedToManagedByValueOutMarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string source = CollectionSource.GetUnmanagedValuesSource(context);
            string destination = CastToManagedIfNecessary($"{MemoryMarshalType}.CreateSpan(ref {MemoryMarshalType}.GetReference({source}), {source}.Length)");
            writer.WriteLine($"{CollectionSource.GetManagedValuesDestination(context)}.CopyTo({destination});");
        }

        public override void GenerateMarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string destination = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesDestination(context));
            writer.WriteLine($"{CollectionSource.GetManagedValuesSource(context)}.CopyTo({destination});");
        }

        public override void GenerateManagedToUnmanagedByValueOutUnmarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string source = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesDestination(context));
            string managedSource = CollectionSource.GetManagedValuesSource(context);
            string destination = $"{MemoryMarshalType}.CreateSpan(ref {MemoryMarshalType}.GetReference({managedSource}), {managedSource}.Length)";
            writer.WriteLine($"{source}.CopyTo({destination});");
        }

        public override void GenerateUnmarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string source = CastToManagedIfNecessary(CollectionSource.GetUnmanagedValuesSource(context));
            writer.WriteLine($"{source}.CopyTo({CollectionSource.GetManagedValuesDestination(context)});");
        }

        private string CastToManagedIfNecessary(string expression)
        {
            return unmanagedElementType == managedElementType
                ? expression
                : $"{MemoryMarshalType}.Cast<{unmanagedElementType}, {managedElementType}>({expression})";
        }

        public override void GenerateElementCleanupStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }

        public override void GenerateSetupStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
        }
    }

    /// <summary>
    /// Support for marshalling non-blittable elements.
    /// </summary>
    internal sealed class NonBlittableElementsMarshalling(
        string unmanagedElementType,
        IBoundMarshallingGenerator elementMarshaller,
        IElementsMarshallingCollectionSource collectionSource) : ElementsMarshalling(collectionSource)
    {
        public override void GenerateMarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);

            using (writer.WriteBlock())
            {
                writer.WriteLine($"{TypeNames.System_ReadOnlySpan}<{elementMarshaller.TypeInfo.ManagedType.FullTypeName}> {managedSpanIdentifier} = {CollectionSource.GetManagedValuesSource(context)};");
                writer.WriteLine($"{TypeNames.System_Span}<{unmanagedElementType}> {nativeSpanIdentifier} = {CollectionSource.GetUnmanagedValuesDestination(context)};");

                // Nested collections clean their entire spans, including elements not reached before a failure.
                if (ShouldCleanUpAllElements(CollectionSource.TypeInfo, CollectionSource.CodeContext))
                {
                    writer.WriteLine($"{nativeSpanIdentifier}.Clear();");
                }

                GenerateContentsMarshallingStatement(writer, context, $"{managedSpanIdentifier}.Length", elementMarshaller, StubIdentifierContext.Stage.Marshal);
            }
        }

        public override void GenerateUnmarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);

            using (writer.WriteBlock())
            {
                writer.WriteLine($"{TypeNames.System_ReadOnlySpan}<{unmanagedElementType}> {nativeSpanIdentifier} = {CollectionSource.GetUnmanagedValuesSource(context)};");
                writer.WriteLine($"{TypeNames.System_Span}<{elementMarshaller.TypeInfo.ManagedType.FullTypeName}> {managedSpanIdentifier} = {CollectionSource.GetManagedValuesDestination(context)};");
                GenerateContentsMarshallingStatement(writer, context, $"{nativeSpanIdentifier}.Length", elementMarshaller,
                    StubIdentifierContext.Stage.UnmarshalCapture, StubIdentifierContext.Stage.Unmarshal);
            }
        }

        public override void GenerateManagedToUnmanagedByValueOutUnmarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            // By-value output copies into the original collection rather than replacing it.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(CollectionSource.TypeInfo, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);

            using (writer.WriteBlock())
            {
                CollectionSource.GenerateNumElementsAssignmentFromManagedValuesSource(writer, CollectionSource.TypeInfo, context);
                writer.WriteLine($"{TypeNames.System_Span}<{elementMarshaller.TypeInfo.ManagedType.FullTypeName}> {managedSpanIdentifier} = {MemoryMarshalType}.CreateSpan(ref {UnsafeType}.AsRef(in {CollectionSource.GetManagedValuesSource(context)}.GetPinnableReference()), {numElementsIdentifier});");
                writer.WriteLine($"{TypeNames.System_Span}<{unmanagedElementType}> {nativeSpanIdentifier} = {CollectionSource.GetUnmanagedValuesDestination(context)};");
                GenerateContentsMarshallingStatement(writer, context, $"{managedSpanIdentifier}.Length", elementMarshaller,
                    StubIdentifierContext.Stage.UnmarshalCapture, StubIdentifierContext.Stage.Unmarshal);
            }
        }

        public override void GenerateElementCleanupStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);
            bool usesLastIndexMarshalled = UsesLastIndexMarshalled(CollectionSource.TypeInfo, CollectionSource.CodeContext);
            string indexConstraint = usesLastIndexMarshalled
                ? MarshallerHelpers.GetLastIndexMarshalledIdentifier(CollectionSource.TypeInfo, context)
                : $"{nativeSpanIdentifier}.Length";

            var contentsWriter = new IndentedTextWriter();
            GenerateContentsMarshallingStatement(contentsWriter, context, indexConstraint, elementMarshaller, context.CurrentStage);
            if (contentsWriter.Length == 0)
            {
                if (usesLastIndexMarshalled)
                {
                    writer.WriteLine($"_ = {indexConstraint};");
                }
                return;
            }

            using (writer.WriteBlock())
            {
                string source = MarshallerHelpers.GetMarshalDirection(CollectionSource.TypeInfo, CollectionSource.CodeContext) == MarshalDirection.ManagedToUnmanaged
                    ? CollectionSource.GetUnmanagedValuesDestination(context)
                    : CollectionSource.GetUnmanagedValuesSource(context);
                writer.WriteLine($"{TypeNames.System_ReadOnlySpan}<{unmanagedElementType}> {nativeSpanIdentifier} = {source};");
                writer.Write(contentsWriter.ToString());
            }
        }

        public override void GenerateUnmanagedToManagedByValueOutMarshalStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            // By-value output reuses the caller's storage, including its original native elements.
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(CollectionSource.TypeInfo, context);
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);

            StubIdentifierContext.Stage[] stagesToGenerate;
            // Unmanaged-to-managed cleanup still combines caller- and callee-allocated resources.
            if (CollectionSource.CodeContext.Direction is MarshalDirection.UnmanagedToManaged && CollectionSource.TypeInfo.ByValueContentsMarshalKind is ByValueContentsMarshalKind.Out)
            {
                stagesToGenerate = [StubIdentifierContext.Stage.Marshal, StubIdentifierContext.Stage.PinnedMarshal];
            }
            else
            {
                stagesToGenerate = [StubIdentifierContext.Stage.Marshal, StubIdentifierContext.Stage.PinnedMarshal, StubIdentifierContext.Stage.CleanupCallerAllocated, StubIdentifierContext.Stage.CleanupCalleeAllocated];
            }

            using (writer.WriteBlock())
            {
                CollectionSource.GenerateNumElementsAssignmentFromManagedValuesDestination(writer, CollectionSource.TypeInfo, context);
                writer.WriteLine($"{TypeNames.System_Span}<{unmanagedElementType}> {nativeSpanIdentifier} = {MemoryMarshalType}.CreateSpan(ref {UnsafeType}.AsRef(in {CollectionSource.GetUnmanagedValuesSource(context)}.GetPinnableReference()), {numElementsIdentifier});");
                writer.WriteLine($"{TypeNames.System_Span}<{elementMarshaller.TypeInfo.ManagedType.FullTypeName}> {managedSpanIdentifier} = {CollectionSource.GetManagedValuesDestination(context)};");
                GenerateContentsMarshallingStatement(writer, context, $"{nativeSpanIdentifier}.Length",
                    new FreeAlwaysOwnedOriginalValueGenerator(elementMarshaller), stagesToGenerate);
            }
        }

        private void GenerateElementStages(
            IndentedTextWriter writer,
            StubIdentifierContext context,
            IBoundMarshallingGenerator elementMarshaller,
            out string indexer,
            params StubIdentifierContext.Stage[] stagesToGeneratePerElement)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(CollectionSource.TypeInfo, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(CollectionSource.TypeInfo, context);
            StubCodeContext elementCodeContext = StubCodeContext.CreateElementMarshallingContext(CollectionSource.CodeContext);
            LinearCollectionElementIdentifierContext elementSetupSubContext = new(
                context,
                elementMarshaller.TypeInfo,
                managedSpanIdentifier,
                nativeSpanIdentifier,
                elementCodeContext.ElementIndirectionLevel)
            {
                CurrentStage = StubIdentifierContext.Stage.Setup,
                CodeEmitOptions = context.CodeEmitOptions
            };

            indexer = elementSetupSubContext.IndexerIdentifier;
            StubIdentifierContext identifierContext = elementSetupSubContext;
            if (elementMarshaller.NativeType is PointerTypeInfo)
            {
                identifierContext = new GenericFriendlyPointerIdentifierContext(elementSetupSubContext, elementMarshaller.TypeInfo, $"{nativeSpanIdentifier}__{indexer}")
                {
                    CodeEmitOptions = elementSetupSubContext.CodeEmitOptions,
                };
            }

            var stagesWriter = new IndentedTextWriter();
            foreach (StubIdentifierContext.Stage stage in stagesToGeneratePerElement)
            {
                elementMarshaller.Generate(stagesWriter, identifierContext with { CurrentStage = stage });
            }
            if (stagesWriter.Length == 0)
            {
                return;
            }

            // Pointer values live in IntPtr spans, but the element marshaller must see the exact native type.
            if (identifierContext is GenericFriendlyPointerIdentifierContext)
            {
                string nativeType = elementMarshaller.NativeType.FullTypeName;
                writer.WriteLine($"{nativeType} {identifierContext.GetIdentifiers(elementMarshaller.TypeInfo).native} = ({nativeType}){elementSetupSubContext.GetIdentifiers(elementMarshaller.TypeInfo).native};");
            }

            // Setup is needed only when one of the requested stages actually emits code.
            elementMarshaller.Generate(writer, identifierContext with { CurrentStage = StubIdentifierContext.Stage.Setup });
            writer.Write(stagesWriter.ToString());

            if (identifierContext is GenericFriendlyPointerIdentifierContext
                && stagesToGeneratePerElement.Any(stage => stage is StubIdentifierContext.Stage.Marshal or StubIdentifierContext.Stage.PinnedMarshal))
            {
                writer.WriteLine($"{elementSetupSubContext.GetIdentifiers(elementMarshaller.TypeInfo).native} = ({TypeNames.GlobalAlias}{TypeNames.System_IntPtr}){identifierContext.GetIdentifiers(elementMarshaller.TypeInfo).native};");
            }
        }

        private void GenerateContentsMarshallingStatement(
            IndentedTextWriter writer,
            StubIdentifierContext context,
            string lengthExpression,
            IBoundMarshallingGenerator elementMarshaller,
            params StubIdentifierContext.Stage[] stagesToGeneratePerElement)
        {
            var elementsWriter = new IndentedTextWriter();
            GenerateElementStages(elementsWriter, context, elementMarshaller, out string indexer, stagesToGeneratePerElement);
            if (elementsWriter.Length == 0)
            {
                return;
            }

            string incrementors = $"++{indexer}";
            if (UsesLastIndexMarshalled(CollectionSource.TypeInfo, CollectionSource.CodeContext)
                && stagesToGeneratePerElement.Contains(StubIdentifierContext.Stage.Marshal))
            {
                incrementors += $", ++{MarshallerHelpers.GetLastIndexMarshalledIdentifier(CollectionSource.TypeInfo, context)}";
            }

            writer.WriteLine($"for (int {indexer} = 0; {indexer} < {lengthExpression}; {incrementors})");
            using (writer.WriteBlock())
            {
                writer.Write(elementsWriter.ToString());
            }
        }

        private static bool UsesLastIndexMarshalled(TypePositionInfo info, StubCodeContext context)
        {
            return !ShouldCleanUpAllElements(info, context)
                && MarshallerHelpers.GetMarshalDirection(info, context) != MarshalDirection.UnmanagedToManaged;
        }

        private static bool ShouldCleanUpAllElements(TypePositionInfo info, StubCodeContext context)
        {
            // Nested collections and native-produced values own every element in their native spans.
            return context.ElementIndirectionLevel != 0 || info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out || info.RefKind == RefKind.Out || info.IsNativeReturnPosition;
        }

        public override void GenerateSetupStatement(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (UsesLastIndexMarshalled(CollectionSource.TypeInfo, CollectionSource.CodeContext))
            {
                writer.WriteLine($"int {MarshallerHelpers.GetLastIndexMarshalledIdentifier(CollectionSource.TypeInfo, context)} = 0;");
            }
        }
    }
}
