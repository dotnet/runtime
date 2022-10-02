// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.NativeFormat;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hash table of resources within the resource blob in the image.
    /// </summary>
    internal sealed class ResourceIndexNode : ObjectNode, ISymbolDefinitionNode
    {
        private ResourceDataNode _resourceDataNode;

        public ResourceIndexNode(ResourceDataNode resourceDataNode)
        {
            _resourceDataNode = resourceDataNode;
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__embedded_resourceindex_End", true);
        }

        private ObjectAndOffsetSymbolNode _endSymbol;

        public ISymbolDefinitionNode EndSymbol => _endSymbol;

        public override bool IsShareable => false;

        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__embedded_resourceindex");
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node has no relocations.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            byte[] blob = GenerateIndexBlob(factory);
            return new ObjectData(
                blob,
                Array.Empty<Relocation>(),
                1,
                new ISymbolDefinitionNode[]
                {
                    this,
                    EndSymbol
                });
        }

        /// <summary>
        /// Builds a native hashtable containing data about each manifest resource
        /// </summary>
        /// <returns></returns>
        private byte[] GenerateIndexBlob(NodeFactory factory)
        {
            NativeWriter nativeWriter = new NativeWriter();
            Section indexHashtableSection = nativeWriter.NewSection();
            VertexHashtable indexHashtable = new VertexHashtable();
            indexHashtableSection.Place(indexHashtable);

            // Build a table with a tuple of Assembly Full Name, Resource Name, Offset within the resource data blob, Length
            // for each resource.
            // This generates a hashtable for the convenience of managed code since there's
            // a reader for VertexHashtable, but not for VertexSequence.

            foreach (ResourceIndexData indexData in _resourceDataNode.GetOrCreateIndexData(factory))
            {
                Vertex asmName = nativeWriter.GetStringConstant(indexData.AssemblyName);
                Vertex resourceName = nativeWriter.GetStringConstant(indexData.ResourceName);
                Vertex offsetVertex = nativeWriter.GetUnsignedConstant((uint)indexData.NativeOffset);
                Vertex lengthVertex = nativeWriter.GetUnsignedConstant((uint)indexData.Length);

                Vertex indexVertex = nativeWriter.GetTuple(asmName, resourceName);
                indexVertex = nativeWriter.GetTuple(indexVertex, offsetVertex);
                indexVertex = nativeWriter.GetTuple(indexVertex, lengthVertex);

                int hashCode = TypeHashingAlgorithms.ComputeNameHashCode(indexData.AssemblyName);
                indexHashtable.Append((uint)hashCode, indexHashtableSection.Place(indexVertex));
            }

            byte[] blob = nativeWriter.Save();
            _endSymbol.SetSymbolOffset(blob.Length);
            return blob;
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ResourceIndexNode;
    }
}
