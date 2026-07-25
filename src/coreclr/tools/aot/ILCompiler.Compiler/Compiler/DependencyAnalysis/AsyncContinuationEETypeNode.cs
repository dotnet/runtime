// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Specialized MethodTable that describes async continuation types (<see cref="AsyncContinuationType"/>).
    /// </summary>
    public class AsyncContinuationEETypeNode : DataOnlyEETypeNode, IEETypeNode
    {
        public TypeDesc Type { get; }

        public AsyncContinuationEETypeNode(AsyncContinuationType type)
            : base("Continuation", BuildPointerMap(type), type.BaseType, false)
        {
            Type = type;
        }

        private static GCPointerMap BuildPointerMap(AsyncContinuationType type)
        {
            // The GC pointer map in the continuation type doesn't include base type. Build a GC pointer map that
            // includes the base type.
            MetadataType continuationBaseType = type.BaseType;

            GCPointerMap baseMap = GCPointerMap.FromInstanceLayout(continuationBaseType);
            GCPointerMap dataMap = type.PointerMap;

            int baseSize = baseMap.Size;
            int dataSize = dataMap.Size;

            int pointerSize = type.Context.Target.PointerSize;

            Debug.Assert(continuationBaseType.InstanceByteCountUnaligned.AsInt == baseSize * pointerSize);

#if DEBUG
            // Validate we match the expected DataOffset value.
            // DataOffset is a const int32 field in the Continuation class.
            EcmaField dataOffsetField = (EcmaField)continuationBaseType.GetField("DataOffset"u8);
            Debug.Assert(dataOffsetField.IsLiteral);

            var reader = dataOffsetField.MetadataReader;
            var constant = reader.GetConstant(reader.GetFieldDefinition(dataOffsetField.Handle).GetDefaultValue());
            Debug.Assert(constant.TypeCode == System.Reflection.Metadata.ConstantTypeCode.Int32);
            int expectedDataOffset = reader.GetBlobReader(constant.Value).ReadInt32()
                + pointerSize /* + MethodTable field */;
            Debug.Assert(baseSize * pointerSize == expectedDataOffset);
#endif

            GCPointerMapBuilder builder = new GCPointerMapBuilder((baseSize + dataSize) * pointerSize, pointerSize);
            for (int i = 0; i < baseSize + dataSize; i++)
            {
                bool isGCPointer = i < baseSize ? baseMap[i] : dataMap[i - baseSize];

                if (isGCPointer)
                    builder.MarkGCPointer(i * pointerSize);
            }

            return builder.ToGCMap();
        }

        public override int ClassCode => 9928091;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            AsyncContinuationEETypeNode otherTypeDataEETypeNode = (AsyncContinuationEETypeNode)other;
            return comparer.Compare(otherTypeDataEETypeNode.Type, Type);
        }
    }
}
