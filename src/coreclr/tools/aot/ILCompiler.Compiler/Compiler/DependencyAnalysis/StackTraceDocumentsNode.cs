// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Contains information about source files in this compilation.
    /// </summary>
    public sealed class StackTraceDocumentsNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private Dictionary<string, int> _documentToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        private List<string> _documents = new List<string>();
        private int? _size;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__stacktrace_documents"u8);
        }

        int INodeWithSize.Size => _size.Value;
        public int Offset => 0;
        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows || factory.Target.SupportsRelativePointers)
                return ObjectNodeSection.ReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public int GetDocumentId(string documentName)
        {
            if (!_documentToIndex.TryGetValue(documentName, out int index))
            {
                index = _documents.Count;
                _documents.Add(documentName);
                _documentToIndex.Add(documentName, index);
            }

            return index;
        }

        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Zero out the hashtable so that we crash if someone tries to use this after emission
            _documentToIndex = null;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            // We write out:
            // (Int32) Offset of document1 from beginning of blob
            // (Int32) Offset of document2 from beginning of blob
            // ...
            // (Int32) Offset of documentN from beginning of blob
            // Null-terminated UTF-8 bytes of document1
            // Null-terminated UTF-8 bytes of document2
            // ...
            // Null-terminated UTF-8 bytes of documentN

            int position = _documents.Count * sizeof(int);
            for (int i = 0; i < _documents.Count; i++)
            {
                bw.Write(position);
                position += Encoding.UTF8.GetByteCount(_documents[i]) + 1;
            }

            for (int i = 0; i < _documents.Count; i++)
            {
                bw.Write(Encoding.UTF8.GetBytes(_documents[i]));
                bw.Write((byte)0);
            }

            _size = checked((int)ms.Length);

            return new ObjectData(ms.ToArray(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.StackTraceDocumentsNode;
    }
}
