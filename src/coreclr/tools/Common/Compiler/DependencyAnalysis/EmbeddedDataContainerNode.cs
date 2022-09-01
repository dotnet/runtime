// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a node that contains a set of embedded objects. The main function is
    /// to serve as a base class, providing symbol name boundaries and node ordering.
    /// </summary>
    public abstract class EmbeddedDataContainerNode : ObjectNode
    {
        private ObjectAndOffsetSymbolNode _startSymbol;
        private ObjectAndOffsetSymbolNode _endSymbol;
        private string _startSymbolMangledName;

        public ObjectAndOffsetSymbolNode StartSymbol => _startSymbol;
        public ObjectAndOffsetSymbolNode EndSymbol => _endSymbol;

        protected EmbeddedDataContainerNode(string startSymbolMangledName, string endSymbolMangledName)
        {
            _startSymbolMangledName = startSymbolMangledName;
            _startSymbol = new ObjectAndOffsetSymbolNode(this, 0, startSymbolMangledName, true);
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, endSymbolMangledName, true);
        }

        public override int ClassCode => -1410622237;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _startSymbolMangledName.CompareTo(((EmbeddedDataContainerNode)other)._startSymbolMangledName);
        }
    }
}
