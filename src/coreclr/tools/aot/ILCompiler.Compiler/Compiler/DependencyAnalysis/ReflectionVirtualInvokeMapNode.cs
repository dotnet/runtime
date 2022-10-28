// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;

using VirtualInvokeTableEntry = Internal.Runtime.VirtualInvokeTableEntry;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a map containing the necessary information needed to resolve
    /// a virtual method target called through reflection.
    /// </summary>
    internal sealed class ReflectionVirtualInvokeMapNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public ReflectionVirtualInvokeMapNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__VirtualInvokeMap_End", true);
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__VirtualInvokeMap");
        }

        public ISymbolNode EndSymbol => _endSymbol;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public static bool NeedsVirtualInvokeInfo(MethodDesc method)
        {
            if (!method.IsVirtual)
                return false;

            if (method.IsFinal)
                return false;

            if (method.OwningType.IsSealed())
                return false;

            return true;
        }

        public static MethodDesc GetDeclaringVirtualMethodAndHierarchyDistance(MethodDesc method, out int parentHierarchyDistance)
        {
            parentHierarchyDistance = 0;

            MethodDesc declaringMethodForSlot = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method.GetTypicalMethodDefinition());
            TypeDesc typeOfDeclaringMethodForSlot = declaringMethodForSlot.OwningType.GetTypeDefinition();
            TypeDesc currentType = method.OwningType.GetTypeDefinition();
            TypeDesc containingTypeOfDeclaringMethodForSlot = method.OwningType;

            while (typeOfDeclaringMethodForSlot != currentType)
            {
                parentHierarchyDistance++;
                currentType = currentType.BaseType.GetTypeDefinition();
                containingTypeOfDeclaringMethodForSlot = containingTypeOfDeclaringMethodForSlot.BaseType;
            }

            if (containingTypeOfDeclaringMethodForSlot.HasInstantiation)
            {
                declaringMethodForSlot = method.Context.GetMethodForInstantiatedType(
                    declaringMethodForSlot.GetTypicalMethodDefinition(),
                    (InstantiatedType)containingTypeOfDeclaringMethodForSlot);
            }

            Debug.Assert(declaringMethodForSlot != null);

            return declaringMethodForSlot;
        }

        public static void GetVirtualInvokeMapDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (NeedsVirtualInvokeInfo(method))
            {
                dependencies ??= new DependencyList();

                dependencies.Add(
                    factory.NecessaryTypeSymbol(method.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific)),
                    "Reflection virtual invoke owning type");

                NativeLayoutMethodNameAndSignatureVertexNode nameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(method.GetTypicalMethodDefinition());
                NativeLayoutPlacedSignatureVertexNode placedNameAndSig = factory.NativeLayout.PlacedSignatureVertex(nameAndSig);
                dependencies.Add(placedNameAndSig, "Reflection virtual invoke method signature");

                if (!method.HasInstantiation)
                {
                    MethodDesc slotDefiningMethod = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method);
                    if (!factory.VTable(slotDefiningMethod.OwningType).HasFixedSlots)
                    {
                        dependencies.Add(factory.VirtualMethodUse(slotDefiningMethod), "Reflection virtual invoke method");
                    }
                }
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Ensure the native layout blob has been saved
            factory.MetadataManager.NativeLayoutInfo.SaveNativeLayoutInfoWriter(factory);

            var writer = new NativeWriter();
            var typeMapHashTable = new VertexHashtable();

            Section hashTableSection = writer.NewSection();
            hashTableSection.Place(typeMapHashTable);

            Dictionary<int, HashSet<TypeDesc>> methodsEmitted = new Dictionary<int, HashSet<TypeDesc>>();

            // Get a list of all methods that have a method body and metadata from the metadata manager.
            foreach (var mappingEntry in factory.MetadataManager.GetMethodMapping(factory))
            {
                MethodDesc method = mappingEntry.Entity;

                // The current format requires us to have an MethodTable for the owning type. We might want to lift this.
                if (!factory.MetadataManager.TypeGeneratesEEType(method.OwningType))
                    continue;

                // We have a method body, we have a metadata token, but we can't get an invoke stub. Bail.
                if (!factory.MetadataManager.IsReflectionInvokable(method))
                    continue;

                // Only virtual methods are interesting
                if (!NeedsVirtualInvokeInfo(method))
                    continue;

                //
                // The vtable entries for each instantiated type might not necessarily exist.
                // Example 1:
                //      If there's a call to Foo<string>.Method1 and a call to Foo<int>.Method2, Foo<string> will
                //      not have Method2 in its vtable and Foo<int> will not have Method1.
                // Example 2:
                //      If there's a call to Foo<string>.Method1 and a call to Foo<object>.Method2, given that both
                //      of these instantiations share the same canonical form, Foo<__Canon> will have both method
                //      entries, and therefore Foo<string> and Foo<object> will have both entries too.
                // For this reason, the entries that we write to the map in CoreRT will be based on the canonical form
                // of the method's containing type instead of the open type definition.
                //

                TypeDesc containingTypeKey = method.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);

                HashSet<TypeDesc> cache;
                if (!methodsEmitted.TryGetValue(mappingEntry.MetadataHandle, out cache))
                    methodsEmitted[mappingEntry.MetadataHandle] = cache = new HashSet<TypeDesc>();

                // Only one record is needed for any instantiation.
                if (!cache.Add(containingTypeKey))
                    continue;

                // Grammar of an entry in the hash table:
                // Virtual Method uses a normal slot
                // TypeKey + NameAndSig metadata offset into the native layout metadata + (NumberOfStepsUpParentHierarchyToType << 1) + slot
                // OR
                // Generic Virtual Method
                // TypeKey + NameAndSig metadata offset into the native layout metadata + (NumberOfStepsUpParentHierarchyToType << 1 + 1)

                int parentHierarchyDistance;
                MethodDesc declaringMethodForSlot = GetDeclaringVirtualMethodAndHierarchyDistance(method, out parentHierarchyDistance);
                ISymbolNode containingTypeKeyNode = factory.NecessaryTypeSymbol(containingTypeKey);
                NativeLayoutMethodNameAndSignatureVertexNode nameAndSig = factory.NativeLayout.MethodNameAndSignatureVertex(method.GetTypicalMethodDefinition());
                NativeLayoutPlacedSignatureVertexNode placedNameAndSig = factory.NativeLayout.PlacedSignatureVertex(nameAndSig);


                Vertex vertex;
                if (method.HasInstantiation)
                {
                    vertex = writer.GetTuple(
                        writer.GetUnsignedConstant(_externalReferences.GetIndex(containingTypeKeyNode)),
                        writer.GetUnsignedConstant((uint)placedNameAndSig.SavedVertex.VertexOffset),
                        writer.GetUnsignedConstant(((uint)parentHierarchyDistance << 1) + VirtualInvokeTableEntry.GenericVirtualMethod));
                }
                else
                {
                    // Get the declaring method for slot on the instantiated declaring type
                    int slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, declaringMethodForSlot, declaringMethodForSlot.OwningType, true);
                    Debug.Assert(slot != -1);

                    vertex = writer.GetTuple(
                        writer.GetUnsignedConstant(_externalReferences.GetIndex(containingTypeKeyNode)),
                        writer.GetUnsignedConstant((uint)placedNameAndSig.SavedVertex.VertexOffset));

                    vertex = writer.GetTuple(vertex,
                        writer.GetUnsignedConstant((uint)parentHierarchyDistance << 1),
                        writer.GetUnsignedConstant((uint)slot));
                }

                int hashCode = containingTypeKey.GetHashCode();
                typeMapHashTable.Append((uint)hashCode, hashTableSection.Place(vertex));
            }

            byte[] hashTableBytes = writer.Save();

            _endSymbol.SetSymbolOffset(hashTableBytes.Length);

            return new ObjectData(hashTableBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.ReflectionVirtualInvokeMapNode;
    }
}
