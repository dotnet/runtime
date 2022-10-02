// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.NativeFormat;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Native layout info blob.
    /// </summary>
    public sealed class NativeLayoutInfoNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;
        private ExternalReferencesTableNode _staticsReferences;

        private NativeWriter _writer;
        private byte[] _writerSavedBytes;

        private Section _signaturesSection;
        private Section _ldTokenInfoSection;
        private Section _templatesSection;

        private List<NativeLayoutVertexNode> _vertexNodesToWrite;

        public NativeLayoutInfoNode(ExternalReferencesTableNode externalReferences, ExternalReferencesTableNode staticsReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__nativelayoutinfo_End", true);
            _externalReferences = externalReferences;
            _staticsReferences = staticsReferences;

            _writer = new NativeWriter();
            _signaturesSection = _writer.NewSection();
            _ldTokenInfoSection = _writer.NewSection();
            _templatesSection = _writer.NewSection();

            _vertexNodesToWrite = new List<NativeLayoutVertexNode>();
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__nativelayoutinfo");
        }
        public ISymbolNode EndSymbol => _endSymbol;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public Section LdTokenInfoSection => _ldTokenInfoSection;
        public Section SignaturesSection => _signaturesSection;
        public Section TemplatesSection => _templatesSection;
        public ExternalReferencesTableNode ExternalReferences => _externalReferences;
        public ExternalReferencesTableNode StaticsReferences => _staticsReferences;
        public NativeWriter Writer => _writer;

        public void AddVertexNodeToNativeLayout(NativeLayoutVertexNode vertexNode)
        {
            _vertexNodesToWrite.Add(vertexNode);
        }

        public void SaveNativeLayoutInfoWriter(NodeFactory factory)
        {
            if (_writerSavedBytes != null)
                return;

            foreach (var vertexNode in _vertexNodesToWrite)
                vertexNode.WriteVertex(factory);

            _writerSavedBytes = _writer.Save();

            // Zero out the native writer and vertex list so that we AV if someone tries to insert after we're done.
            _writer = null;
            _vertexNodesToWrite = null;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // Dependencies of the NativeLayoutInfo node are tracked by the callers that emit data into the native layout writer
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            SaveNativeLayoutInfoWriter(factory);

            _endSymbol.SetSymbolOffset(_writerSavedBytes.Length);

            return new ObjectData(_writerSavedBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.NativeLayoutInfoNode;
    }
}
