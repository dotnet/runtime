// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a map of generic virtual method implementations.
    /// </summary>
    public sealed class GenericVirtualMethodTableNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;
        private Dictionary<MethodDesc, Dictionary<TypeDesc, MethodDesc>> _gvmImplementations;

        public GenericVirtualMethodTableNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__gvm_table_End", true);
            _externalReferences = externalReferences;
            _gvmImplementations = new Dictionary<MethodDesc, Dictionary<TypeDesc, MethodDesc>>();
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__gvm_table");
        }

        public ISymbolNode EndSymbol => _endSymbol;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        /// <summary>
        /// Helper method to compute the dependencies that would be needed by a hashtable entry for a GVM call.
        /// This helper is used by the TypeGVMEntriesNode, which is used by the dependency analysis to compute the
        /// GVM hashtable entries for the compiled types.
        /// The dependencies returned from this function will be reported as static dependencies of the TypeGVMEntriesNode,
        /// which we create for each type that has generic virtual methods.
        /// </summary>
        public static void GetGenericVirtualMethodImplementationDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc callingMethod, MethodDesc implementationMethod)
        {
            Debug.Assert(!callingMethod.OwningType.IsInterface);

            // Compute the open method signatures
            MethodDesc openCallingMethod = callingMethod.GetTypicalMethodDefinition();
            MethodDesc openImplementationMethod = implementationMethod.GetTypicalMethodDefinition();

            var openCallingMethodNameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(openCallingMethod);
            var openImplementationMethodNameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(openImplementationMethod);

            dependencies.Add(new DependencyListEntry(factory.NativeLayout.PlacedSignatureVertex(openCallingMethodNameAndSig), "gvm table calling method signature"));
            dependencies.Add(new DependencyListEntry(factory.NativeLayout.PlacedSignatureVertex(openImplementationMethodNameAndSig), "gvm table implementation method signature"));
        }

        private void AddGenericVirtualMethodImplementation(NodeFactory factory, MethodDesc callingMethod, MethodDesc implementationMethod)
        {
            Debug.Assert(!callingMethod.OwningType.IsInterface);

            // Compute the open method signatures
            MethodDesc openCallingMethod = callingMethod.GetTypicalMethodDefinition();
            MethodDesc openImplementationMethod = implementationMethod.GetTypicalMethodDefinition();

            // Insert open method signatures into the GVM map
            if (!_gvmImplementations.ContainsKey(openCallingMethod))
                _gvmImplementations[openCallingMethod] = new Dictionary<TypeDesc, MethodDesc>();

            _gvmImplementations[openCallingMethod][openImplementationMethod.OwningType] = openImplementationMethod;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Build the GVM table entries from the list of interesting GVMTableEntryNodes
            foreach (var interestingEntry in factory.MetadataManager.GetTypeGVMEntries())
            {
                foreach (var typeGVMEntryInfo in interestingEntry.ScanForGenericVirtualMethodEntries())
                {
                    AddGenericVirtualMethodImplementation(factory, typeGVMEntryInfo.CallingMethod, typeGVMEntryInfo.ImplementationMethod);
                }
            }

            // Ensure the native layout blob has been saved
            factory.MetadataManager.NativeLayoutInfo.SaveNativeLayoutInfoWriter(factory);

            NativeWriter nativeFormatWriter = new NativeWriter();
            VertexHashtable gvmHashtable = new VertexHashtable();

            Section gvmHashtableSection = nativeFormatWriter.NewSection();
            gvmHashtableSection.Place(gvmHashtable);

            // Emit the GVM target information entries
            foreach (var gvmEntry in _gvmImplementations)
            {
                Debug.Assert(!gvmEntry.Key.OwningType.IsInterface);

                foreach (var implementationEntry in gvmEntry.Value)
                {
                    MethodDesc callingMethod = gvmEntry.Key;
                    TypeDesc implementationType = implementationEntry.Key;
                    MethodDesc implementationMethod = implementationEntry.Value;

                    uint callingTypeId = _externalReferences.GetIndex(factory.NecessaryTypeSymbol(callingMethod.OwningType));
                    Vertex vertex = nativeFormatWriter.GetUnsignedConstant(callingTypeId);

                    uint targetTypeId = _externalReferences.GetIndex(factory.NecessaryTypeSymbol(implementationType));
                    vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant(targetTypeId));

                    var nameAndSig = factory.NativeLayout.PlacedSignatureVertex(factory.NativeLayout.MethodNameAndSignatureVertex(callingMethod));
                    vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant((uint)nameAndSig.SavedVertex.VertexOffset));

                    nameAndSig = factory.NativeLayout.PlacedSignatureVertex(factory.NativeLayout.MethodNameAndSignatureVertex(implementationMethod));
                    vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant((uint)nameAndSig.SavedVertex.VertexOffset));

                    int hashCode = callingMethod.OwningType.GetHashCode();
                    hashCode = ((hashCode << 13) ^ hashCode) ^ implementationType.GetHashCode();

                    gvmHashtable.Append((uint)hashCode, gvmHashtableSection.Place(vertex));
                }
            }

            // Zero out the dictionary so that we AV if someone tries to insert after we're done.
            _gvmImplementations = null;

            byte[] streamBytes = nativeFormatWriter.Save();

            _endSymbol.SetSymbolOffset(streamBytes.Length);

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.GenericVirtualMethodTableNode;
    }
}
