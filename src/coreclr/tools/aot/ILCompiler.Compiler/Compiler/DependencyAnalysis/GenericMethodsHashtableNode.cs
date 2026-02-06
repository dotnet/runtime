// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;
using Internal.NativeFormat;
using Internal.Runtime;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a hashtable of all compiled generic method instantiations
    /// </summary>
    public sealed class GenericMethodsHashtableNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int? _size;
        private ExternalReferencesTableNode _externalReferences;

        public GenericMethodsHashtableNode(ExternalReferencesTableNode externalReferences)
        {
            _externalReferences = externalReferences;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__generic_methods_hashtable"u8);
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

            // Ensure the native layout data has been saved, in order to get valid Vertex offsets for the signature Vertices
            factory.MetadataManager.NativeLayoutInfo.SaveNativeLayoutInfoWriter(factory);

            NativeWriter nativeWriter = new NativeWriter();
            VertexHashtable hashtable = new VertexHashtable();
            Section nativeSection = nativeWriter.NewSection();
            nativeSection.Place(hashtable);

            foreach (MethodDesc method in factory.MetadataManager.GetGenericMethodHashtableEntries())
            {
                Debug.Assert(method.HasInstantiation && !method.IsCanonicalMethod(CanonicalFormKind.Any));

                Vertex fullMethodSignature;
                {
                    // Method's containing type
                    IEETypeNode containingTypeNode = factory.NecessaryTypeSymbol(method.OwningType);
                    Vertex containingType = nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(containingTypeNode));

                    // Method's instantiation arguments
                    VertexSequence arguments = new VertexSequence();
                    for (int i = 0; i < method.Instantiation.Length; i++)
                    {
                        IEETypeNode argNode = factory.NecessaryTypeSymbol(method.Instantiation[i]);
                        arguments.Append(nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(argNode)));
                    }

                    int flags = 0;
                    MethodDesc methodForMetadata = GetMethodForMetadata(method, out bool isAsyncVariant);
                    if (isAsyncVariant)
                        flags |= GenericMethodsHashtableConstants.IsAsyncVariant;

                    int token = factory.MetadataManager.GetMetadataHandleForMethod(factory, methodForMetadata);

                    int flagsAndToken = (token & MetadataManager.MetadataOffsetMask) | flags;
                    fullMethodSignature = nativeWriter.GetTuple(containingType, nativeWriter.GetUnsignedConstant((uint)flagsAndToken), arguments);
                }

                // Method's dictionary pointer
                var dictionaryNode = factory.MethodGenericDictionary(method);
                Vertex dictionaryVertex = nativeWriter.GetUnsignedConstant(_externalReferences.GetIndex(dictionaryNode));

                Vertex entry = nativeWriter.GetTuple(dictionaryVertex, fullMethodSignature);

                hashtable.Append((uint)method.GetHashCode(), nativeSection.Place(entry));
            }

            byte[] streamBytes = nativeWriter.Save();

            _size = streamBytes.Length;

            return new ObjectData(streamBytes, Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
        }

        public static void GetGenericMethodsHashtableDependenciesForMethod(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            dependencies ??= new DependencyList();

            Debug.Assert(method.HasInstantiation && !method.IsCanonicalMethod(CanonicalFormKind.Any));

            // Method's containing type
            IEETypeNode containingTypeNode = factory.NecessaryTypeSymbol(method.OwningType);
            dependencies.Add(new DependencyListEntry(containingTypeNode, "GenericMethodsHashtable entry containing type"));

            // Method's instantiation arguments
            for (int i = 0; i < method.Instantiation.Length; i++)
            {
                IEETypeNode argNode = factory.NecessaryTypeSymbol(method.Instantiation[i]);
                dependencies.Add(new DependencyListEntry(argNode, "GenericMethodsHashtable entry instantiation argument"));
            }

            factory.MetadataManager.GetNativeLayoutMetadataDependencies(ref dependencies, factory, GetMethodForMetadata(method, out _));
        }

        private static MethodDesc GetMethodForMetadata(MethodDesc method, out bool isAsyncVariant)
        {
            MethodDesc result = method.GetTypicalMethodDefinition();
            if (result is AsyncMethodVariant asyncVariant)
            {
                isAsyncVariant = true;
                return asyncVariant.Target;
            }
            isAsyncVariant = false;
            return result;
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.GenericMethodsHashtableNode;
    }
}
