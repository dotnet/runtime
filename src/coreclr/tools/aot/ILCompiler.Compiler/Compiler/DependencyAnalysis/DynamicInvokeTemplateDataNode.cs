// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a map between method name / signature and CanonicalEntryPoint for the corresponding invoke stub.
    /// The first entry is the containing type of the invoke stubs.
    /// </summary>
    internal sealed class DynamicInvokeTemplateDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;
        private Dictionary<MethodDesc, int> _methodToTemplateIndex;

        public DynamicInvokeTemplateDataNode(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__dynamic_invoke_template_data_end", true);
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__dynamic_invoke_template_data");
        }

        public ISymbolNode EndSymbol => _endSymbol;
        public int Offset => 0;
        public override ObjectNodeSection Section => _externalReferences.Section;
        public override bool IsShareable => false;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => !factory.MetadataManager.GetDynamicInvokeTemplateMethods().GetEnumerator().MoveNext();

        public int GetIdForMethod(MethodDesc dynamicInvokeMethod, NodeFactory factory)
        {
            // We should only see canonical or non-shared methods here
            Debug.Assert(dynamicInvokeMethod.GetCanonMethodTarget(CanonicalFormKind.Specific) == dynamicInvokeMethod);
            Debug.Assert(!dynamicInvokeMethod.IsCanonicalMethod(CanonicalFormKind.Universal));

            if (_methodToTemplateIndex == null)
            {
                BuildMethodToIdMap(factory);
            }

            return _methodToTemplateIndex[dynamicInvokeMethod];
        }

        private void BuildMethodToIdMap(NodeFactory factory)
        {
            List<MethodDesc> methods = new List<MethodDesc>(factory.MetadataManager.GetDynamicInvokeTemplateMethods());

            // Sort the stubs
            var typeSystemComparer = new TypeSystemComparer();
            methods.Sort((first, second) => typeSystemComparer.Compare(first, second));

            // Assign each stub an ID
            var methodToTemplateIndex = new Dictionary<MethodDesc, int>();
            foreach (MethodDesc method in methods)
            {
                TypeDesc dynamicInvokeMethodContainingType = method.OwningType;

                int templateIndex = (2 * methodToTemplateIndex.Count) + 1;
                // Add 1 to the index to account for the first blob entry being the containing MethodTable RVA
                methodToTemplateIndex.Add(method, templateIndex);
            }

            _methodToTemplateIndex = methodToTemplateIndex;
        }

        internal static DependencyListEntry[] GetDependenciesDueToInvokeTemplatePresence(NodeFactory factory, MethodDesc method)
        {
            return new[]
            {
                new DependencyListEntry(factory.MethodEntrypoint(method), "Dynamic invoke stub"),
                new DependencyListEntry(factory.NativeLayout.PlacedSignatureVertex(factory.NativeLayout.MethodNameAndSignatureVertex(method)), "Dynamic invoke stub"),
                new DependencyListEntry(factory.NecessaryTypeSymbol(method.OwningType), "Dynamic invoke stub containing type"),
                new DependencyListEntry(factory.NativeLayout.TemplateMethodLayout(method), "Template"),
                new DependencyListEntry(factory.NativeLayout.TemplateMethodEntry(method), "Template"),
            };
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            // Ensure the native layout blob has been saved
            factory.MetadataManager.NativeLayoutInfo.SaveNativeLayoutInfoWriter(factory);

            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);

            if (_methodToTemplateIndex == null)
            {
                BuildMethodToIdMap(factory);
            }

            TypeDesc containerType = null;
            foreach (var method in _methodToTemplateIndex.Keys)
            {
                Debug.Assert(containerType == null || containerType == method.OwningType);
                containerType = method.OwningType;
#if !DEBUG
                break;
#endif
            }

            if (factory.Target.SupportsRelativePointers)
                objData.EmitReloc(factory.NecessaryTypeSymbol(containerType), RelocType.IMAGE_REL_BASED_RELPTR32);
            else
                objData.EmitPointerReloc(factory.NecessaryTypeSymbol(containerType));

            List<KeyValuePair<MethodDesc, int>> sortedList = new List<KeyValuePair<MethodDesc, int>>(_methodToTemplateIndex);
            sortedList.Sort((firstEntry, secondEntry) => firstEntry.Value.CompareTo(secondEntry.Value));

            for (int i = 0; i < sortedList.Count; i++)
            {
                var nameAndSig = factory.NativeLayout.PlacedSignatureVertex(factory.NativeLayout.MethodNameAndSignatureVertex(sortedList[i].Key));

                if (factory.Target.SupportsRelativePointers)
                {
                    objData.EmitInt(nameAndSig.SavedVertex.VertexOffset);
                    objData.EmitReloc(factory.MethodEntrypoint(sortedList[i].Key), RelocType.IMAGE_REL_BASED_RELPTR32);
                }
                else
                {
                    objData.EmitNaturalInt(nameAndSig.SavedVertex.VertexOffset);
                    objData.EmitPointerReloc(factory.MethodEntrypoint(sortedList[i].Key));
                }
            }

            _endSymbol.SetSymbolOffset(objData.CountBytes);
            objData.AddSymbol(_endSymbol);

            return objData.ToObjectData();
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.DynamicInvokeTemplateDataNode;
    }
}
