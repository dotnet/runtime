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
    public sealed class ExactMethodInstantiationsNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public ExactMethodInstantiationsNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__exact_method_instantiations_End", true);
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__exact_method_instantiations");
        }

        public ISymbolDefinitionNode EndSymbol => _endSymbol;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
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


            foreach (MethodDesc method in factory.MetadataManager.GetCompiledMethods())
            {
                if (!IsMethodEligibleForTracking(factory, method))
                    continue;

                // Get the method pointer vertex

                bool getUnboxingStub = method.OwningType.IsValueType && !method.Signature.IsStatic;
                IMethodNode methodEntryPointNode = factory.MethodEntrypoint(method, getUnboxingStub);
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

            _endSymbol.SetSymbolOffset(streamBytes.Length);

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        public static void GetExactMethodInstantiationDependenciesForMethod(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (!IsMethodEligibleForTracking(factory, method))
                return;

            dependencies ??= new DependencyList();

            // Method entry point dependency
            bool getUnboxingStub = method.OwningType.IsValueType && !method.Signature.IsStatic;
            IMethodNode methodEntryPointNode = factory.MethodEntrypoint(method, getUnboxingStub);
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

        private static bool IsMethodEligibleForTracking(NodeFactory factory, MethodDesc method)
        {
            // Runtime determined methods should never show up here.
            Debug.Assert(!method.IsRuntimeDeterminedExactMethod);

            if (method.IsAbstract)
                return false;

            if (!method.HasInstantiation)
                return false;

            // This hashtable is only for method instantiations that don't use generic dictionaries,
            // so check if the given method is shared before proceeding
            if (method.IsSharedByGenericInstantiations || method.GetCanonMethodTarget(CanonicalFormKind.Specific) != method)
                return false;

            // The hashtable is used to find implementations of generic virtual methods at runtime
            if (method.IsVirtual)
                return true;

            // The hashtable is also used for reflection
            if (!factory.MetadataManager.IsReflectionBlocked(method))
                return true;

            // The rest of the entries are potentially only useful for the universal
            // canonical type loader.
            return factory.TypeSystemContext.SupportsUniversalCanon;
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ExactMethodInstantiationsNode;
    }
}
