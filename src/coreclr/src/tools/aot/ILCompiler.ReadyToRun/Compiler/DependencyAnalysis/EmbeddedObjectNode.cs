// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public abstract class EmbeddedObjectNode : SortableDependencyNode
    {
        private const int InvalidOffset = int.MinValue;

        private int _offset;
        private int _index;

        public IHasStartSymbol ContainingNode { get; set; }

        public EmbeddedObjectNode()
        {
            _offset = InvalidOffset;
            _index = InvalidOffset;
        }

        public int OffsetFromBeginningOfArray
        {
            get
            {
                Debug.Assert(_offset != InvalidOffset);
                return _offset;
            }
        }

        public int IndexFromBeginningOfArray
        {
            get
            {
                Debug.Assert(_index != InvalidOffset);
                return _index;
            }
        }

        public void InitializeOffsetFromBeginningOfArray(int offset)
        {
            Debug.Assert(_offset == InvalidOffset || _offset == offset);
            _offset = offset;
        }

        public void InitializeIndexFromBeginningOfArray(int index)
        {
            Debug.Assert(_index == InvalidOffset || _index == index);
            _index = index;
        }

        public virtual bool IsShareable => false;
        public virtual bool RepresentsIndirectionCell => false;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

        public abstract void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly);
    }
}
