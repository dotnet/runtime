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
    /// Represents a subset of <see cref="EETypeNode"/> that is used to describe GC static field regions for
    /// types. It only fills out enough pieces of the MethodTable structure so that the GC can operate on it. Runtime should
    /// never see these.
    /// </summary>
    public class GCStaticEETypeNode : DehydratableObjectNode, ISymbolDefinitionNode
    {
        private GCPointerMap _gcMap;
        private TargetDetails _target;
        private bool _requiresAlign8;

        public GCStaticEETypeNode(TargetDetails target, GCPointerMap gcMap, bool requiresAlign8)
        {
            _gcMap = gcMap;
            _target = target;
            _requiresAlign8 = requiresAlign8;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected override ObjectNodeSection GetDehydratedSection(NodeFactory factory)
        {
            if (_target.IsWindows)
                return ObjectNodeSection.ReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__GCStaticEEType_"u8).Append(_gcMap.ToString());
        }

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                int numSeries = _gcMap.NumSeries;
                return numSeries > 0 ? ((numSeries * 2) + 1) * _target.PointerSize : 0;
            }
        }

        int ISymbolNode.Offset => 0;

        public override bool IsShareable => true;

        protected override ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder dataBuilder = new ObjectDataBuilder(factory, relocsOnly);
            dataBuilder.RequireInitialPointerAlignment();
            dataBuilder.AddSymbol(this);

            // +1 for SyncBlock (static size already includes MethodTable)
            int totalSize = (_gcMap.Size + 1) * _target.PointerSize;

            // We only need to check for containsPointers because ThreadStatics are always allocated
            // on the GC heap (no matter what "HasGCStaticBase" says).
            // If that ever changes, we can assume "true" and switch this to an assert.

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

            totalSize = Math.Max(totalSize, _target.PointerSize * 3); // minimum GC MethodTable size is 3 pointers
            dataBuilder.EmitInt(totalSize);

            // Related type: System.Object. This allows storing an instance of this type in an array of objects.
            dataBuilder.EmitPointerReloc(factory.NecessaryTypeSymbol(factory.TypeSystemContext.GetWellKnownType(WellKnownType.Object)));

            return dataBuilder.ToObjectData();
        }

        public override int ClassCode => 1304929125;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _gcMap.CompareTo(((GCStaticEETypeNode)other)._gcMap);
        }
    }
}
