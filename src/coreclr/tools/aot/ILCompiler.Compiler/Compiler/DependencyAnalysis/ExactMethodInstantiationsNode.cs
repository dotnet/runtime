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
    /// Hashtable of all exact (non-canonical) generic method instantiations compiled in the module.
    /// </summary>
    public sealed class ExactMethodInstantiationsNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;
        private ExternalReferencesTableNode _externalReferences;

        public ExactMethodInstantiationsNode(ExternalReferencesTableNode externalReferences)
        {
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__exact_method_instantiations"u8);
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


            foreach (MethodDesc method in factory.MetadataManager.GetExactMethodHashtableEntries())
            {
                // Get the method pointer vertex

                bool getUnboxingStub = method.OwningType.IsValueType && !method.Signature.IsStatic;
                // TODO-SIZE: we need address taken entrypoint only if this was a target of a delegate
                IMethodNode methodEntryPointNode = factory.AddressTakenMethodEntrypoint(method, getUnboxingStub);
                Vertex methodPointer = nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(methodEntryPointNode));

                // Get native layout vertices for the declaring type

                ISymbolNode declaringTypeNode = factory.NecessaryTypeSymbol(method.OwningType);
                Vertex declaringType = nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(declaringTypeNode));

                // Get a vertex sequence for the method instantiation args if any

                VertexSequence arguments = new VertexSequence();
                foreach (var arg in method.Instantiation)
                {
                    ISymbolNode argNode = factory.NecessaryTypeSymbol(arg);
                    arguments.Append(nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(argNode)));
                }

                // Get the name and sig of the method.
                // Note: the method name and signature are stored in the NativeLayoutInfo blob, not in the hashtable we build here.

                NativeLayoutMethodNameAndSignatureVertexNode nameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(method.GetTypicalMethodDefinition());
                NativeLayoutPlacedSignatureVertexNode placedNameAndSig = factory.NativeLayout.PlacedSignatureVertex(nameAndSig);
                Debug.Assert(placedNameAndSig.SavedVertex != null);
                Vertex placedNameAndSigOffsetSig = nativeWriter.GetOffsetSignature(placedNameAndSig.SavedVertex);

                // Get the vertex for the completed method signature

                Vertex methodSignature = nativeWriter.GetTuple(declaringType, placedNameAndSigOffsetSig, arguments);

                // Make the generic method entry vertex

                Vertex entry = nativeWriter.GetTuple(methodSignature, methodPointer);

                // Add to the hash table, hashed by the containing type's hashcode
                uint hashCode = (uint)method.OwningType.GetHashCode();
                hashtable.Append(hashCode, nativeSection.Place(entry));
            }

            byte[] streamBytes = nativeWriter.Save();

            _size = streamBytes.Length;

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
        }

        public static void GetExactMethodInstantiationDependenciesForMethod(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            dependencies ??= new DependencyList();

            // Method entry point dependency
            bool getUnboxingStub = method.OwningType.IsValueType && !method.Signature.IsStatic;
            // TODO-SIZE: we need address taken entrypoint only if this was a target of a delegate
            IMethodNode methodEntryPointNode = factory.AddressTakenMethodEntrypoint(method, getUnboxingStub);
            dependencies.Add(new DependencyListEntry(methodEntryPointNode, "Exact method instantiation entry"));

            // Get native layout dependencies for the declaring type
            dependencies.Add(new DependencyListEntry(factory.NecessaryTypeSymbol(method.OwningType), "Exact method instantiation entry"));

            // Get native layout dependencies for the method instantiation args
            foreach (var arg in method.Instantiation)
                dependencies.Add(new DependencyListEntry(factory.NecessaryTypeSymbol(arg), "Exact method instantiation entry"));

            // Get native layout dependencies for the method signature.
            NativeLayoutMethodNameAndSignatureVertexNode nameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(method.GetTypicalMethodDefinition());
            dependencies.Add(new DependencyListEntry(factory.NativeLayout.PlacedSignatureVertex(nameAndSig), "Exact method instantiation entry"));
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ExactMethodInstantiationsNode;
    }
}
