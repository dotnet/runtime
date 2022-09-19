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
    /// Hashtable of all generic type templates used by the TypeLoader at runtime
    /// </summary>
    public sealed class GenericTypesTemplateMap : ObjectNode, ISymbolDefinitionNode
    {
        private ObjectAndOffsetSymbolNode _endSymbol;
        private ExternalReferencesTableNode _externalReferences;

        public GenericTypesTemplateMap(ExternalReferencesTableNode externalReferences)
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__GenericTypesTemplateMap_End", true);
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__GenericTypesTemplateMap");
        }

        public ISymbolNode EndSymbol => _endSymbol;
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

            foreach (TypeDesc type in factory.MetadataManager.GetTypesWithConstructedEETypes())
            {
                if (!IsEligibleToHaveATemplate(type))
                    continue;

                // Type's native layout info
                NativeLayoutTemplateTypeLayoutVertexNode templateNode = factory.NativeLayout.TemplateTypeLayout(type);

                // If this template isn't considered necessary, don't emit it.
                if (!templateNode.Marked)
                    continue;
                Vertex nativeLayout = templateNode.SavedVertex;

                // Hashtable Entry
                Vertex entry = nativeWriter.GetTuple(
                    nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(factory.NecessaryTypeSymbol(type))),
                    nativeWriter.GetUnsignedConstant((uint)nativeLayout.VertexOffset));

                // Add to the hash table, hashed by the containing type's hashcode
                uint hashCode = (uint)type.GetHashCode();
                hashtable.Append(hashCode, nativeSection.Place(entry));
            }

            byte[] streamBytes = nativeWriter.Save();

            _endSymbol.SetSymbolOffset(streamBytes.Length);

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this, _endSymbol });
        }

        public static void GetTemplateTypeDependencies(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            TypeDesc templateType = ConvertArrayOfTToRegularArray(factory, type);

            if (!IsEligibleToHaveATemplate(templateType))
                return;

            dependencies ??= new DependencyList();
            dependencies.Add(new DependencyListEntry(factory.NecessaryTypeSymbol(templateType), "Template type"));
            dependencies.Add(new DependencyListEntry(factory.NativeLayout.TemplateTypeLayout(templateType), "Template Type Layout"));
        }

        /// <summary>
        /// Array&lt;T&gt; should not get an MethodTable in our system. All templates should replace
        /// references to Array&lt;T&gt; types with regular arrays.
        /// </summary>
        public static TypeDesc ConvertArrayOfTToRegularArray(NodeFactory factory, TypeDesc type)
        {
            if (type.HasSameTypeDefinition(factory.ArrayOfTClass))
            {
                Debug.Assert(type.Instantiation.Length == 1);
                return factory.TypeSystemContext.GetArrayType(type.Instantiation[0]);
            }

            return type;
        }

        public static bool IsArrayTypeEligibleForTemplate(TypeDesc type)
        {
            if (!type.IsSzArray)
                return false;

            // Unmanaged Pointer and Function pointer arrays can't use the array template
            ArrayType arrayType = (ArrayType)type;
            TypeDesc elementType = arrayType.ElementType;

            return !elementType.IsPointer && !elementType.IsFunctionPointer;
        }

        public static bool IsEligibleToHaveATemplate(TypeDesc type)
        {
            if (!type.HasInstantiation && !IsArrayTypeEligibleForTemplate(type))
                return false;

            if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                // Must be fully canonical
                Debug.Assert(type == type.ConvertToCanonForm(CanonicalFormKind.Specific));
                return true;
            }

            return false;
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.GenericTypesTemplateMap;
    }
}
