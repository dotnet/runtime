// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;
using Internal;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Contains information about mapping native code offsets to line numbers.
    /// </summary>
    public sealed class StackTraceLineNumbersNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;
        private readonly ExternalReferencesTableNode _externalReferences;
        private readonly StackTraceDocumentsNode _documents;

        public StackTraceLineNumbersNode(ExternalReferencesTableNode externalReferences, StackTraceDocumentsNode documents)
        {
            _externalReferences = externalReferences;
            _documents = documents;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__stacktrace_line_numbers"u8);
        }

        int INodeWithSize.Size => _size.Value;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => _externalReferences.GetSection(factory);
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            NativeWriter nativeWriter = new NativeWriter();
            VertexHashtable hashtable = new VertexHashtable();
            Section nativeSection = nativeWriter.NewSection();
            nativeSection.Place(hashtable);

            foreach (StackTraceMapping mapping in factory.MetadataManager.GetStackTraceMapping(factory))
            {
                if ((factory.MetadataManager.StackTracePolicy.GetMethodVisibility(mapping.Method) & MethodStackTraceVisibilityFlags.HasLineNumbers) == 0)
                    continue;

                var entrypointSymbol = factory.MethodEntrypoint(mapping.Method);
                if (entrypointSymbol is not INodeWithDebugInfo debugInfo)
                    continue;

                Vertex blob = CreateLineNumbersBlob(nativeWriter, _documents, debugInfo);
                if (blob == null)
                    continue;

                Vertex methodPointer = nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(entrypointSymbol));
                var hashtableEntry = nativeWriter.GetTuple(methodPointer, blob);

                uint hashcode = VersionResilientHashCode.CombineThreeValuesIntoHash((uint)mapping.OwningTypeHandle, (uint)mapping.MethodNameHandle, (uint)mapping.MethodSignatureHandle);
                hashtable.Append(hashcode, nativeSection.Place(hashtableEntry));
            }

            foreach (ReflectionStackTraceMapping mapping in factory.MetadataManager.GetReflectionStackTraceMappings(factory))
            {
                Debug.Assert((factory.MetadataManager.StackTracePolicy.GetMethodVisibility(mapping.Method) & MethodStackTraceVisibilityFlags.HasLineNumbers) != 0);

                var entrypointSymbol = factory.MethodEntrypoint(mapping.Method);
                if (entrypointSymbol is not INodeWithDebugInfo debugInfo)
                    continue;

                Vertex blob = CreateLineNumbersBlob(nativeWriter, _documents, debugInfo);
                if (blob == null)
                    continue;

                Vertex methodPointer = nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(entrypointSymbol));
                var hashtableEntry = nativeWriter.GetTuple(methodPointer, blob);

                uint hashcode = VersionResilientHashCode.CombineTwoValuesIntoHash((uint)mapping.OwningTypeHandle, (uint)mapping.MethodHandle);
                hashtable.Append(hashcode, nativeSection.Place(hashtableEntry));
            }

            static Vertex CreateLineNumbersBlob(NativeWriter writer, StackTraceDocumentsNode documents, INodeWithDebugInfo debugInfoNode)
            {
                var encoder = default(NativePrimitiveEncoder);
                encoder.Init();

                int currentNativeOffset = 0;
                int currentLineNumber = 0;
                string currentDocument = null;

                uint numEntries = 0;

                foreach (NativeSequencePoint sequencePoint in debugInfoNode.GetNativeSequencePoints())
                {
                    if (currentLineNumber == sequencePoint.LineNumber && currentDocument == sequencePoint.FileName)
                        continue;

                    // Make sure a zero native offset delta is not possible because we use it below
                    // to indicate an update to the current document.
                    if (currentDocument != null && currentNativeOffset == sequencePoint.NativeOffset)
                        continue;

                    if (currentDocument != sequencePoint.FileName)
                    {
                        // We start with currentDocument == null, so the reader knows the first byte of the output
                        // is a document number. Otherwise we use NativeOffsetDelta == 0 as a marker that the next
                        // byte is a document number and not a native offset delta.
                        if (currentDocument != null)
                            encoder.WriteSigned(0);
                        encoder.WriteSigned(documents.GetDocumentId(sequencePoint.FileName));
                    }

                    int nativeOffsetDelta = sequencePoint.NativeOffset - currentNativeOffset;
                    encoder.WriteSigned(nativeOffsetDelta);

                    int lineNumberDelta = sequencePoint.LineNumber - currentLineNumber;
                    encoder.WriteSigned(lineNumberDelta);

                    numEntries++;

                    currentLineNumber = sequencePoint.LineNumber;
                    currentNativeOffset = sequencePoint.NativeOffset;
                    currentDocument = sequencePoint.FileName;
                }

                var ms = new MemoryStream();
                encoder.Save(ms);

                return numEntries == 0 ? null : writer.GetTuple(writer.GetUnsignedConstant(numEntries), new BlobVertex(ms.ToArray()));
            }

            byte[] streamBytes = nativeWriter.Save();

            _size = streamBytes.Length;

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.StackTraceLineNumbersNode;
    }
}
