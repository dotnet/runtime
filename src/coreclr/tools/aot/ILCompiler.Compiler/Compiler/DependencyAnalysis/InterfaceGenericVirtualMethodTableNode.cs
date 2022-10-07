// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;
using Internal.Runtime;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a map between reflection metadata and generated method bodies.
    /// </summary>
    public sealed class InterfaceGenericVirtualMethodTableNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;
        private Dictionary<MethodDesc, HashSet<object>> _interfaceGvmSlots;
        private Dictionary<object, Dictionary<TypeDesc, HashSet<int>>> _interfaceImpls;

        public InterfaceGenericVirtualMethodTableNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__interface_gvm_table_End", true);
            _externalReferences = externalReferences;
            _interfaceGvmSlots = new Dictionary<MethodDesc, HashSet<object>>();
            _interfaceImpls = new Dictionary<object, Dictionary<TypeDesc, HashSet<int>>>();
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__interface_gvm_table");
        }
        public ISymbolNode EndSymbol => _endSymbol;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        /// <summary>
        /// Helper method to compute the dependencies that would be needed by a hashtable entry for an interface GVM call.
        /// This helper is used by the TypeGVMEntriesNode, which is used by the dependency analysis to compute the
        /// GVM hashtable entries for the compiled types.
        /// The dependencies returned from this function will be reported as static dependencies of the TypeGVMEntriesNode,
        /// which we create for each type that has generic virtual methods.
        /// </summary>
        public static void GetGenericVirtualMethodImplementationDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc callingMethod, TypeDesc implementationType, MethodDesc implementationMethod)
        {
            Debug.Assert(callingMethod.OwningType.IsInterface);

            // Compute the open method signatures
            MethodDesc openCallingMethod = callingMethod.GetTypicalMethodDefinition();
            TypeDesc openImplementationType = implementationType.GetTypeDefinition();

            var openCallingMethodNameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(openCallingMethod);
            dependencies.Add(new DependencyListEntry(factory.NativeLayout.PlacedSignatureVertex(openCallingMethodNameAndSig), "interface gvm table calling method signature"));

            // Implementation could be null if this is a default interface method reabstraction or diamond. We need to record those.
            if (implementationMethod != null)
            {
                MethodDesc openImplementationMethod = implementationMethod.GetTypicalMethodDefinition();
                var openImplementationMethodNameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(openImplementationMethod);
                dependencies.Add(new DependencyListEntry(factory.NativeLayout.PlacedSignatureVertex(openImplementationMethodNameAndSig), "interface gvm table implementation method signature"));
            }

            if (!openImplementationType.IsInterface)
            {
                for(int index = 0; index < openImplementationType.RuntimeInterfaces.Length; index++)
                {
                    if (openImplementationType.RuntimeInterfaces[index] == callingMethod.OwningType)
                    {
                        TypeDesc currentInterface = openImplementationType.RuntimeInterfaces[index];
                        var currentInterfaceSignature = factory.NativeLayout.TypeSignatureVertex(currentInterface);
                        dependencies.Add(new DependencyListEntry(factory.NativeLayout.PlacedSignatureVertex(currentInterfaceSignature), "interface gvm table interface signature"));
                    }
                }
            }
        }

        private void AddGenericVirtualMethodImplementation(NodeFactory factory, MethodDesc callingMethod, TypeDesc implementationType, MethodDesc implementationMethod, DefaultInterfaceMethodResolution resolution)
        {
            Debug.Assert(callingMethod.OwningType.IsInterface);

            // Compute the open method signatures
            MethodDesc openCallingMethod = callingMethod.GetTypicalMethodDefinition();
            object openImplementationMethod = implementationMethod == null ? resolution : implementationMethod.GetTypicalMethodDefinition();
            TypeDesc openImplementationType = implementationType.GetTypeDefinition();

            // Add the entry to the interface GVM slots mapping table
            if (!_interfaceGvmSlots.ContainsKey(openCallingMethod))
                _interfaceGvmSlots[openCallingMethod] = new HashSet<object>();
            _interfaceGvmSlots[openCallingMethod].Add(openImplementationMethod);

            // If the implementation method is implementing some interface method, compute which
            // interface explicitly implemented on the type that the current method implements an interface method for.
            // We need this because at runtime, the interfaces explicitly implemented on the type will have
            // runtime-determined signatures that we can use to make generic substitutions and check for interface matching.
            if (!openImplementationType.IsInterface)
            {
                if (!_interfaceImpls.ContainsKey(openImplementationMethod))
                    _interfaceImpls[openImplementationMethod] = new Dictionary<TypeDesc, HashSet<int>>();
                if (!_interfaceImpls[openImplementationMethod].ContainsKey(openImplementationType))
                    _interfaceImpls[openImplementationMethod][openImplementationType] = new HashSet<int>();

                int numIfacesAdded = 0;
                for (int index = 0; index < openImplementationType.RuntimeInterfaces.Length; index++)
                {
                    if (openImplementationType.RuntimeInterfaces[index] == callingMethod.OwningType)
                    {
                        _interfaceImpls[openImplementationMethod][openImplementationType].Add(index);
                        numIfacesAdded++;
                    }
                }

                Debug.Assert(numIfacesAdded > 0);
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Build the GVM table entries from the list of interesting GVMTableEntryNodes
            foreach (var interestingEntry in factory.MetadataManager.GetTypeGVMEntries())
            {
                foreach (var typeGVMEntryInfo in interestingEntry.ScanForInterfaceGenericVirtualMethodEntries())
                {
                    AddGenericVirtualMethodImplementation(factory, typeGVMEntryInfo.CallingMethod, typeGVMEntryInfo.ImplementationType, typeGVMEntryInfo.ImplementationMethod, typeGVMEntryInfo.DefaultResolution);
                }
            }

            // Ensure the native layout blob has been saved
            factory.MetadataManager.NativeLayoutInfo.SaveNativeLayoutInfoWriter(factory);

            NativeWriter nativeFormatWriter = new NativeWriter();
            VertexHashtable gvmHashtable = new VertexHashtable();

            Section gvmHashtableSection = nativeFormatWriter.NewSection();
            gvmHashtableSection.Place(gvmHashtable);

            // Emit the interface slot resolution entries
            foreach (var gvmEntry in _interfaceGvmSlots)
            {
                Debug.Assert(gvmEntry.Key.OwningType.IsInterface);

                MethodDesc callingMethod = gvmEntry.Key;

                // Emit the method signature and containing type of the current interface method
                uint typeId = _externalReferences.GetIndex(factory.NecessaryTypeSymbol(callingMethod.OwningType));
                var nameAndSig = factory.NativeLayout.PlacedSignatureVertex(factory.NativeLayout.MethodNameAndSignatureVertex(callingMethod));
                Vertex vertex = nativeFormatWriter.GetTuple(
                    nativeFormatWriter.GetUnsignedConstant(typeId),
                    nativeFormatWriter.GetUnsignedConstant((uint)nameAndSig.SavedVertex.VertexOffset));

                // Emit the method name / sig and containing type of each GVM target method for the current interface method entry
                vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant((uint)gvmEntry.Value.Count));
                foreach (object impl in gvmEntry.Value)
                {
                    if (impl is MethodDesc implementationMethod)
                    {
                        nameAndSig = factory.NativeLayout.PlacedSignatureVertex(factory.NativeLayout.MethodNameAndSignatureVertex(implementationMethod));
                        typeId = _externalReferences.GetIndex(factory.NecessaryTypeSymbol(implementationMethod.OwningType));
                        vertex = nativeFormatWriter.GetTuple(
                            vertex,
                            nativeFormatWriter.GetUnsignedConstant((uint)nameAndSig.SavedVertex.VertexOffset),
                            nativeFormatWriter.GetUnsignedConstant(typeId));
                    }
                    else
                    {
                        Debug.Assert(impl is DefaultInterfaceMethodResolution);
                        uint constant = (DefaultInterfaceMethodResolution)impl switch
                        {
                            DefaultInterfaceMethodResolution.Diamond => SpecialGVMInterfaceEntry.Diamond,
                            DefaultInterfaceMethodResolution.Reabstraction => SpecialGVMInterfaceEntry.Reabstraction,
                            _ => throw new NotImplementedException(),
                        };
                        vertex = nativeFormatWriter.GetTuple(
                            vertex,
                            nativeFormatWriter.GetUnsignedConstant(constant));
                    }

                    // Emit the interface GVM slot details for each type that implements the interface methods
                    {
                        Debug.Assert(_interfaceImpls.ContainsKey(impl));

                        var ifaceImpls = _interfaceImpls[impl];

                        // First, emit how many types have method implementations for this interface method entry
                        vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant((uint)ifaceImpls.Count));

                        // Emit each type that implements the interface method, and the interface signatures for the interfaces implemented by the type
                        foreach (var currentImpl in ifaceImpls)
                        {
                            TypeDesc implementationType = currentImpl.Key;

                            typeId = _externalReferences.GetIndex(factory.NecessaryTypeSymbol(implementationType));
                            vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant(typeId));

                            // Emit information on which interfaces the current method entry provides implementations for
                            vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant((uint)currentImpl.Value.Count));
                            foreach (var ifaceId in currentImpl.Value)
                            {
                                // Emit the signature of the current interface implemented by the method
                                Debug.Assert(((uint)ifaceId) < implementationType.RuntimeInterfaces.Length);
                                TypeDesc currentInterface = implementationType.RuntimeInterfaces[ifaceId];
                                var typeSig = factory.NativeLayout.PlacedSignatureVertex(factory.NativeLayout.TypeSignatureVertex(currentInterface));
                                vertex = nativeFormatWriter.GetTuple(vertex, nativeFormatWriter.GetUnsignedConstant((uint)typeSig.SavedVertex.VertexOffset));
                            }
                        }
                    }
                }

                int hashCode = callingMethod.OwningType.GetHashCode();
                gvmHashtable.Append((uint)hashCode, gvmHashtableSection.Place(vertex));
            }

            // Zero out the dictionary so that we AV if someone tries to insert after we're done.
            _interfaceGvmSlots = null;

            byte[] streamBytes = nativeFormatWriter.Save();

            _endSymbol.SetSymbolOffset(streamBytes.Length);

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.InterfaceGenericVirtualMethodTableNode;
    }
}
