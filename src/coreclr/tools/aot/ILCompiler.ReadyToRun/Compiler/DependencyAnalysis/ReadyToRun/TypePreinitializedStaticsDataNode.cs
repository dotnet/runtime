// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    // Emits the preinitialized static payload for a single type:
    // - non-GC static bytes
    // - pointer-aligned GC static reference slots
    internal sealed class TypePreinitializedStaticsDataNode : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly TypePreinit.PreinitializationInfo _preinitializationInfo;

        public TypePreinitializedStaticsDataNode(TypePreinit.PreinitializationInfo preinitializationInfo)
        {
            Debug.Assert(preinitializationInfo.IsPreinitialized);
            Debug.Assert(!preinitializationInfo.Type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            _preinitializationInfo = preinitializationInfo;
        }

        public MetadataType Type => _preinitializationInfo.Type;

        private static int ComputeStaticDataSize(
            MetadataType type,
            Predicate<FieldDesc> predicate,
            Func<FieldDesc, int> getSerializedFieldSize,
            int baseOffset)
        {
            int size = 0;
            foreach (FieldDesc field in type.GetFields())
            {
                if (!predicate(field))
                    continue;

                int fieldOffset = field.Offset.AsInt - baseOffset;
                if (fieldOffset < 0)
                    throw new NotSupportedException($"Negative static field offset is not supported for preinitialized type '{type}'.");

                int fieldSize = getSerializedFieldSize(field);
                int fieldEnd = checked(fieldOffset + fieldSize);
                if (fieldEnd > size)
                    size = fieldEnd;
            }

            return size;
        }

        public static int ComputeNonGCStaticsDataSize(MetadataType type)
            => ComputeStaticDataSize(
                type,
                IsNonGCStaticField,
                static field => field.FieldType.GetElementSize().AsInt,
                baseOffset: 0);

        public int NonGCStaticsDataSize => ComputeNonGCStaticsDataSize(Type);

        public static int ComputeGCStaticsDataSize(MetadataType type)
            => ComputeStaticDataSize(
                type,
                IsGCStaticField,
                field => GetGCStaticFieldSerializedSize(field, type.Context),
                baseOffset: 0);

        public int GCStaticsDataSize(TypeSystemContext context)
            => ComputeGCStaticsDataSize(Type);

        public int AlignedNonGCStaticsDataSize(TargetDetails target)
            => AlignmentHelper.AlignUp(NonGCStaticsDataSize, target.PointerSize);

        public static bool IsNonGCStaticField(FieldDesc field)
            => field.IsStatic && !field.HasRva && !field.IsLiteral && !field.IsThreadStatic && !field.HasGCStaticBase;

        public static bool IsGCStaticField(FieldDesc field)
            => field.IsStatic && !field.HasRva && !field.IsLiteral && !field.IsThreadStatic && field.HasGCStaticBase;

        private static bool IsBoxedGCStaticField(FieldDesc field)
            => IsGCStaticField(field) && field.FieldType.IsValueType;

        private static int GetGCStaticFieldSerializedSize(FieldDesc field, TypeSystemContext context)
            => IsBoxedGCStaticField(field) ? context.Target.PointerSize : field.FieldType.GetElementSize().AsInt;

        // Synthesize a unique allocation site ID for each boxed static field based on its ordinal
        // Starting from int.MinValue to avoid collisions with allocation sites from instruction pointer
        private static int GetBoxedStaticFieldAllocationSiteId(int boxedStaticFieldOrdinal)
            => unchecked(int.MinValue + boxedStaticFieldOrdinal);

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__PreInitStaticsData_"u8);
            sb.Append(nameMangler.GetMangledTypeName(Type));
        }

        public int Offset => 0;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
                return ObjectNodeSection.ReadOnlyDataSection;

            return ObjectNodeSection.DataSection;
        }

        public override bool IsShareable => false;

        private static List<FieldDesc> GetOrderedStaticFields(MetadataType type, Predicate<FieldDesc> predicate)
        {
            List<FieldDesc> fields = new();
            foreach (FieldDesc field in type.GetFields())
            {
                if (predicate(field))
                    fields.Add(field);
            }

            fields.Sort(static (a, b) => a.Offset.AsInt.CompareTo(b.Offset.AsInt));
            return fields;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            int requiredAlignment = Type.NonGCStaticFieldAlignment.AsInt;
            if (requiredAlignment < 1)
                requiredAlignment = 1;
            if (GCStaticsDataSize(factory.TypeSystemContext) > 0 && requiredAlignment < factory.Target.PointerSize)
                requiredAlignment = factory.Target.PointerSize;
            builder.RequireInitialAlignment(requiredAlignment);
            builder.AddSymbol(this);

            int initialOffset = builder.CountBytes;
            foreach (FieldDesc field in GetOrderedStaticFields(Type, IsNonGCStaticField))
            {
                int padding = field.Offset.AsInt - builder.CountBytes + initialOffset;
                if (padding < 0)
                    throw new NotSupportedException($"Overlapping non-GC static layout is not supported for preinitialized type '{Type}'.");
                builder.EmitZeros(padding);

                TypePreinit.ISerializableValue value = _preinitializationInfo.GetFieldValue(field);
                int currentOffset = builder.CountBytes;
                value.WriteFieldData(ref builder, factory);
                Debug.Assert(builder.CountBytes - currentOffset == field.FieldType.GetElementSize().AsInt);
            }

            int nonGCPad = NonGCStaticsDataSize - builder.CountBytes + initialOffset;
            Debug.Assert(nonGCPad >= 0);
            builder.EmitZeros(nonGCPad);

            int gcDataSize = GCStaticsDataSize(factory.TypeSystemContext);
            if (gcDataSize > 0)
            {
                // GC static payload is read as pointer-sized slots at runtime.
                int alignedNonGCDataSize = AlignedNonGCStaticsDataSize(factory.Target);
                int currentNonGCDataSize = builder.CountBytes - initialOffset;
                if (currentNonGCDataSize < alignedNonGCDataSize)
                    builder.EmitZeros(alignedNonGCDataSize - currentNonGCDataSize);

                int gcInitialOffset = 0;
                int gcStartOffset = builder.CountBytes;
                int boxedStaticFieldOrdinal = 0;

                foreach (FieldDesc field in GetOrderedStaticFields(Type, IsGCStaticField))
                {
                    int padding = field.Offset.AsInt - gcInitialOffset - (builder.CountBytes - gcStartOffset);
                    if (padding < 0)
                        throw new NotSupportedException($"Overlapping GC static layout is not supported for preinitialized type '{Type}'.");
                    builder.EmitZeros(padding);

                    TypePreinit.ISerializableValue value = _preinitializationInfo.GetFieldValue(field);
                    int currentOffset = builder.CountBytes;
                    EmitGCStaticFieldData(ref builder, factory, field, value, ref boxedStaticFieldOrdinal);
                    Debug.Assert(builder.CountBytes - currentOffset == GetGCStaticFieldSerializedSize(field, factory.TypeSystemContext));
                }

                int gcPad = gcDataSize - (builder.CountBytes - gcStartOffset);
                Debug.Assert(gcPad >= 0);
                builder.EmitZeros(gcPad);
            }

            int totalSize = builder.CountBytes - initialOffset;
            Debug.Assert(totalSize >= NonGCStaticsDataSize);
            Debug.Assert(gcDataSize == 0 || totalSize >= AlignedNonGCStaticsDataSize(factory.Target) + gcDataSize);
            Debug.Assert(totalSize >= 0);
            return builder.ToObjectData();
        }

        private void EmitGCStaticFieldData(
            ref ObjectDataBuilder builder,
            NodeFactory factory,
            FieldDesc field,
            TypePreinit.ISerializableValue value,
            ref int boxedStaticFieldOrdinal)
        {
            if (IsBoxedGCStaticField(field))
            {
                if (value == null)
                    throw new NotSupportedException($"ReadyToRun preinitialized boxed static field '{field}' cannot be null.");

                if (field.FieldType is not DefType boxedValueType || !boxedValueType.IsValueType)
                    throw new NotSupportedException($"ReadyToRun preinitialized boxed static field '{field}' has unsupported type '{field.FieldType}'.");

                int allocationSiteId = GetBoxedStaticFieldAllocationSiteId(boxedStaticFieldOrdinal++);
                var boxedValue = new BoxedStaticValueTypeReference(Type, allocationSiteId, boxedValueType, value);
                boxedValue.WriteFieldData(ref builder, factory);
                return;
            }

            if (value != null)
                value.WriteFieldData(ref builder, factory);
            else
                builder.EmitZeroPointer();
        }

        private sealed class BoxedStaticValueTypeReference : TypePreinit.ISerializableReference
        {
            private readonly MetadataType _owningType;
            private readonly int _allocationSiteId;
            private readonly DefType _boxedValueType;
            private readonly TypePreinit.ISerializableValue _value;

            public BoxedStaticValueTypeReference(
                MetadataType owningType,
                int allocationSiteId,
                DefType boxedValueType,
                TypePreinit.ISerializableValue value)
            {
                Debug.Assert(boxedValueType.IsValueType);
                _owningType = owningType;
                _allocationSiteId = allocationSiteId;
                _boxedValueType = boxedValueType;
                _value = value;
            }

            public TypeDesc Type => _boxedValueType;

            public bool HasConditionalDependencies => false;

            public bool IsKnownImmutable => false;

            public int ArrayLength => throw new NotSupportedException();

            public void WriteFieldData(ref ObjectDataBuilder builder, NodeFactory factory)
            {
                builder.EmitPointerReloc(factory.SerializedFrozenObject(_owningType, _allocationSiteId, this));
            }

            public bool GetRawData(NodeFactory factory, out object data)
            {
                data = null;
                return false;
            }

            public void GetConditionalDependencies(ref CombinedDependencyList dependencies, NodeFactory factory)
            {
            }

            public void WriteContent(ref ObjectDataBuilder builder, ISymbolNode thisNode, NodeFactory factory)
            {
                builder.EmitPointerReloc(factory.ConstructedTypeSymbol(_boxedValueType));

                int dataStart = builder.CountBytes;
                _value.WriteFieldData(ref builder, factory);

                int serializedDataSize = builder.CountBytes - dataStart;
                int expectedDataSize = _boxedValueType.GetElementSize().AsInt;
                if (serializedDataSize != expectedDataSize)
                {
                    throw new NotSupportedException(
                        $"ReadyToRun preinitialized boxed static value type '{_boxedValueType}' has unsupported serialized size '{serializedDataSize}' (expected '{expectedDataSize}').");
                }
            }
        }

        public override int ClassCode => 2084515482;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
            => comparer.Compare(Type, ((TypePreinitializedStaticsDataNode)other).Type);
    }
}
