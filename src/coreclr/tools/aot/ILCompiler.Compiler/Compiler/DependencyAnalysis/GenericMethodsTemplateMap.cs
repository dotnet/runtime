// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Hashtable of all generic method templates used by the TypeLoader at runtime
    /// </summary>
    public sealed class GenericMethodsTemplateMap : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;
        private ExternalReferencesTableNode _externalReferences;

        public GenericMethodsTemplateMap(ExternalReferencesTableNode externalReferences)
        {
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__GenericMethodsTemplateMap");
        }

        int INodeWithSize.Size => _size.Value;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection GetSection(NodeFactory factory) => _externalReferences.GetSection(factory);
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // Dependencies for this node are tracked by the method code nodes
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Ensure the native layout data has been saved, in order to get valid Vertex offsets for the signature Vertices
            factory.MetadataManager.NativeLayoutInfo.SaveNativeLayoutInfoWriter(factory);

            NativeWriter nativeWriter = new NativeWriter();
            VertexHashtable hashtable = new VertexHashtable();
            Section nativeSection = nativeWriter.NewSection();
            nativeSection.Place(hashtable);


            foreach (var methodEntryNode in factory.MetadataManager.GetTemplateMethodEntries())
            {
                // Method entry
                Vertex methodEntry = methodEntryNode.SavedVertex;

                // Method's native layout info
                var layoutNode = factory.NativeLayout.TemplateMethodLayout(methodEntryNode.Method);
                Debug.Assert(layoutNode.Marked);
                Vertex nativeLayout = layoutNode.SavedVertex;

                // Hashtable Entry
                Vertex entry = nativeWriter.GetTuple(
                    nativeWriter.GetUnsignedConstant((uint)methodEntry.VertexOffset),
                    nativeWriter.GetUnsignedConstant((uint)nativeLayout.VertexOffset));

                // Add to the hash table, hashed by the containing type's hashcode
                uint hashCode = (uint)methodEntryNode.Method.GetHashCode();
                hashtable.Append(hashCode, nativeSection.Place(entry));
            }

            byte[] streamBytes = nativeWriter.Save();

            _size = streamBytes.Length;

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
        }

        public static void GetTemplateMethodDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (!IsEligibleToBeATemplate(method))
                return;

            dependencies ??= new DependencyList();
            dependencies.Add(new DependencyListEntry(factory.NativeLayout.TemplateMethodEntry(method), "Template Method Entry"));
            dependencies.Add(new DependencyListEntry(factory.NativeLayout.TemplateMethodLayout(method), "Template Method Layout"));
        }

        private static bool IsEligibleToBeATemplate(MethodDesc method)
        {
            if (!method.HasInstantiation)
                return false;

            if (method.IsAbstract)
                return false;

            if (method.IsCanonicalMethod(CanonicalFormKind.Specific))
            {
                // Must be fully canonical
                Debug.Assert(method == method.GetCanonMethodTarget(CanonicalFormKind.Specific));
                return true;
            }
            else if (method.IsCanonicalMethod(CanonicalFormKind.Universal))
            {
                // Must be fully canonical
                if (method == method.GetCanonMethodTarget(CanonicalFormKind.Universal))
                    return true;
            }

            return false;
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.GenericMethodsTemplateMap;
    }
}
