// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hash table of function pointer types generated into the image.
    /// </summary>
    internal sealed class FunctionPointerMapNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly ObjectAndOffsetSymbolNode _endSymbol;
        private readonly ExternalReferencesTableNode _externalReferences;

        public FunctionPointerMapNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__fnptr_type_map_End", true);
            _externalReferences = externalReferences;
        }

        public ISymbolDefinitionNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__fnptr_type_map");
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => _externalReferences.GetSection(factory);

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public static void GetHashtableDependencies(ref DependencyList dependencies, NodeFactory factory, FunctionPointerType type)
        {
            dependencies ??= new DependencyList();
            dependencies.Add(factory.NecessaryTypeSymbol(type.Signature.ReturnType), "Function pointer type composition");
            foreach (TypeDesc paramType in type.Signature)
                dependencies.Add(factory.NecessaryTypeSymbol(paramType), "Function pointer type composition");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var writer = new NativeWriter();
            var typeMapHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(typeMapHashTable);

            foreach (var type in factory.MetadataManager.GetTypesWithEETypes())
            {
                if (!type.IsFunctionPointer)
                    continue;

                Vertex vertex = writer.GetUnsignedConstant(_externalReferences.GetIndex(factory.NecessaryTypeSymbol(type)));

                int hashCode = type.GetHashCode();
                typeMapHashTable.Append((uint)hashCode, hashTableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.FunctionPointerMapNode;
    }
}
