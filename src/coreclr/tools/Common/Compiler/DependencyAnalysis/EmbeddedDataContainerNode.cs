// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a node that contains a set of embedded objects. The main function is
    /// to serve as a base class, providing symbol name boundaries and node ordering.
    /// </summary>
    public abstract class EmbeddedDataContainerNode : ObjectNode, ISymbolDefinitionNode
    {
        private string _mangledName;

        protected EmbeddedDataContainerNode(string mangledName)
        {
            _mangledName = mangledName;
        }

        public int Offset => 0;

        public override int ClassCode => -1410622237;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            => sb.Append(nameMangler.CompilationUnitPrefix).Append(_mangledName);

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _mangledName.CompareTo(((EmbeddedDataContainerNode)other)._mangledName);
        }
    }
}
