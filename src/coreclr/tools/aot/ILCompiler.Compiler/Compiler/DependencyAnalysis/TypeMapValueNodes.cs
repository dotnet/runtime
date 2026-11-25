// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// The data of the typed value flavor of a group: the constant values of the mapped types, laid out one after the other.
    /// The type map entries refer to the values by their position.
    /// </summary>
    internal sealed class TypeMapValueArrayNode : ArrayOfEmbeddedDataNode<TypeMapValueDataNode>
    {
        public TypeMapValueArrayNode(string mangledName, TypeDesc valueType)
            : base(mangledName, new Comparer())
        {
            ValueType = valueType;
        }

        public TypeDesc ValueType { get; }

        public override int ClassCode => 1295347712;

        protected internal override int Phase => (int)ObjectNodePhase.Unordered;

        // The array starts at an address that is aligned for the value type (at least pointer aligned, as the values contain pointers),
        // and the size of a value is a multiple of its alignment. A reference is a pointer to a frozen object.
        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            ObjectData data = base.GetData(factory, relocsOnly);
            int valueAlignment = ValueType.IsValueType ? ((DefType)ValueType).InstanceFieldAlignment.AsInt : factory.Target.PointerSize;
            int alignment = Math.Max(data.Alignment, valueAlignment);
            return alignment == data.Alignment
                ? data
                : new ObjectData(data.Data, data.Relocs, alignment, data.DefinedSymbols);
        }

        // The values are constants that are copied into a location of the value type, and contain pointers (to code, strings, types, other frozen objects).
        // Windows can relocate read only data, other platforms only relocate writable data without creating text relocations.
        public override ObjectNodeSection GetSection(NodeFactory factory)
            => factory.Target.IsWindows ? ObjectNodeSection.ReadOnlyDataSection : ObjectNodeSection.DataSection;

        /// <summary>
        /// The values in the order they are in the image. The position in this list is the index of the value in the table.
        /// </summary>
        public IReadOnlyList<TypeMapValueDataNode> GetSortedNodes()
        {
            List<TypeMapValueDataNode> nodes = NodesList.ToList();
            nodes.Sort(new Comparer());
            return nodes;
        }

        private sealed class Comparer : IComparer<TypeMapValueDataNode>
        {
            public int Compare(TypeMapValueDataNode x, TypeMapValueDataNode y)
                => CompilerComparer.Instance.Compare(x.Target, y.Target);
        }
    }

    /// <summary>
    /// The data of the typed value flavor for a mapped type.
    /// </summary>
    internal sealed class TypeMapValueDataNode : EmbeddedObjectNode
    {
        private readonly TypeMapValueArrayNode _array;
        private readonly TypeMapValueEvaluation _evaluation;

        public TypeMapValueDataNode(TypeMapValueArrayNode array, TypeMapValueEvaluation evaluation)
        {
            Debug.Assert(evaluation.Kind == TypeMapValueKind.Data);
            _array = array;
            _evaluation = evaluation;
        }

        public TypeDesc Target => _evaluation.Target;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => $"Type map value data of {_evaluation.ValueType} for {_evaluation.Target} in {_array.GetMangledName(factory.NameMangler)}";

        public override void AddStaticDependencies(DependencySink sink, NodeFactory factory)
        {
            sink.Add(_array, "Array of type map values");

            // The references in the data
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly: true);
            _evaluation.Data.WriteFieldData(ref builder, factory);
            foreach (Relocation reloc in builder.ToObjectData().Relocs)
            {
                sink.Add(reloc.Target, "Reference in type map value data");
            }
        }

        protected override void OnMarked(NodeFactory factory)
        {
            _array.AddEmbeddedObject(this);
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            int start = dataBuilder.CountBytes;
            _evaluation.Data.WriteFieldData(ref dataBuilder, factory);
            Debug.Assert(dataBuilder.CountBytes - start == _evaluation.ValueType.GetElementSize().AsInt);
        }

        public override int ClassCode => 1730182549;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            TypeMapValueDataNode otherNode = (TypeMapValueDataNode)other;
            int result = comparer.Compare(_array, otherNode._array);
            return result != 0 ? result : comparer.Compare(Target, otherNode.Target);
        }
    }
}
