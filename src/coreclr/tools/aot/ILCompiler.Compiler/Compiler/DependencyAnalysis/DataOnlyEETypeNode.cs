// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// MethodTable for a piece of data that can be allocated on the GC heap, with GC-reported references in it.
    /// The shape of the data is described using a <see cref="GCPointerMap"/>. The generated data structure
    /// is a MethodTable that is valid for use with GC, however not all data on it is present.
    /// A lot of the MethodTable API surface on this will not work at runtime .
    /// </summary>
    public class DataOnlyEETypeNode : DehydratableObjectNode, ISymbolDefinitionNode
    {
        private readonly string _prefix;
        protected readonly GCPointerMap _gcMap;
        private readonly TypeDesc _baseType;
        protected readonly bool _requiresAlign8;

        public DataOnlyEETypeNode(string prefix, GCPointerMap gcMap, TypeDesc baseType, bool requiresAlign8)
        {
            _prefix = prefix;
            _gcMap = gcMap;
            _baseType = baseType;
            _requiresAlign8 = requiresAlign8;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected override ObjectNodeSection GetDehydratedSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
                return ObjectNodeSection.ReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append($"__{_prefix}_").Append(_gcMap.ToString());
            if (_requiresAlign8)
            {
                sb.Append("_align8"u8);
            }
        }

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                int numSeries = _gcMap.NumSeries;
                return numSeries > 0 ? ((numSeries * 2) + 1) * _baseType.Context.Target.PointerSize : 0;
            }
        }

        int ISymbolNode.Offset => 0;

        public override bool IsShareable => true;

        protected override ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder dataBuilder = new ObjectDataBuilder(factory, relocsOnly);
            dataBuilder.RequireInitialPointerAlignment();
            dataBuilder.AddSymbol(this);

            // +1 for SyncBlock (GCMap size already includes MethodTable field)
            int totalSize = (_gcMap.Size + 1) * factory.Target.PointerSize;

            bool containsPointers = _gcMap.NumSeries > 0;
            if (containsPointers)
            {
                GCDescEncoder.EncodeStandardGCDesc(ref dataBuilder, _gcMap, totalSize, 0);
            }

            Debug.Assert(dataBuilder.CountBytes == ((ISymbolDefinitionNode)this).Offset);

            // ComponentSize is always 0
            uint flags = 0;
            if (containsPointers)
                flags |= (uint)EETypeFlags.HasPointersFlag;

            if (_requiresAlign8)
            {
                // Mark the method table as non-value type that requires 8-byte alignment
                flags |= (uint)EETypeFlagsEx.RequiresAlign8Flag;
                flags |= (uint)EETypeElementType.Class << (byte)EETypeFlags.ElementTypeShift;
            }

            dataBuilder.EmitUInt(flags);

            totalSize = Math.Max(totalSize, factory.Target.PointerSize * 3); // minimum GC MethodTable size is 3 pointers
            dataBuilder.EmitInt(totalSize);

            dataBuilder.EmitPointerReloc(factory.NecessaryTypeSymbol(_baseType));

            return dataBuilder.ToObjectData();
        }

        public override int ClassCode => 1304929125;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            DataOnlyEETypeNode otherNode = (DataOnlyEETypeNode)other;
            int mapCompare = _gcMap.CompareTo(otherNode._gcMap);
            if (mapCompare == 0)
            {
                return _requiresAlign8.CompareTo(otherNode._requiresAlign8);
            }

            return mapCompare;
        }
    }
}
