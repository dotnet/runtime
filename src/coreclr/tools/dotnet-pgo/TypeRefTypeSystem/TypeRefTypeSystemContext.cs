// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Pgo.TypeRefTypeSystem
{
    partial class TypeRefTypeSystemContext : MetadataTypeSystemContext, IMetadataStringDecoderProvider
    {
        PEReader[] _refReaders;
        Dictionary<string, TypeRefTypeSystemModule> _typeRefModules = new Dictionary<string, TypeRefTypeSystemModule>();

        class PEInfo
        {
            public PEInfo(PEReader pe)
            {
                this.pe = pe;
                reader = pe.GetMetadataReader();
                Name = reader.GetAssemblyDefinition().GetAssemblyName().Name;
            }

            public readonly string Name;
            public readonly PEReader pe;
            public readonly MetadataReader reader;
            public readonly Dictionary<TypeReferenceHandle, TypeRefTypeSystemType> handleLookup = new Dictionary<TypeReferenceHandle, TypeRefTypeSystemType>();
            public readonly Dictionary<AssemblyReferenceHandle, TypeRefTypeSystemModule> assemblyLookup = new Dictionary<AssemblyReferenceHandle, TypeRefTypeSystemModule>();
        }

        public TypeRefTypeSystemContext(IEnumerable<PEReader> refReaders)
        {
            _refReaders = refReaders.ToArray();

            TypeRefTypeSystemModule coreLibModule = new TypeRefTypeSystemModule(this, new System.Reflection.AssemblyName("System.Private.CoreLib"));
            foreach (string name in MetadataTypeSystemContext.WellKnownTypeNames)
            {
                coreLibModule.GetOrAddType("System", name);
            }

            _typeRefModules.Add(coreLibModule.Assembly.GetName().Name, coreLibModule);

            SetSystemModule(coreLibModule);

            // Read every signature and hydrate the types that are possible
            List<PEInfo> peInfos = new List<PEInfo>();
            foreach (PEReader pe in refReaders)
            {
                peInfos.Add(new PEInfo(pe));
            }

            // Walk all signature blobs and find out which types are valuetypes/generic
            foreach (PEInfo peInfo in peInfos)
            {
                TypeRefSignatureParserProvider parserHelper = new TypeRefSignatureParserProvider(this, peInfo.handleLookup);
                // Resolve every type ref, so that the full set of type refs is known
                foreach (var typeRefHandle in peInfo.reader.TypeReferences)
                {
                    ResolveTypeRef(peInfo, typeRefHandle);
                }

                int typeSpecRowCount = peInfo.reader.GetTableRowCount(TableIndex.TypeSpec);
                for (int row = 1; row <= typeSpecRowCount; row++)
                {
                    var handle = MetadataTokens.TypeSpecificationHandle(row);
                    var typeSpec = peInfo.reader.GetTypeSpecification(handle);
                    typeSpec.DecodeSignature(parserHelper, null);
                }

                int standAloneSigRowCount = peInfo.reader.GetTableRowCount(TableIndex.StandAloneSig);
                for (int row = 1; row <= standAloneSigRowCount; row++)
                {
                    var handle = MetadataTokens.StandaloneSignatureHandle(row);
                    var standaloneSig = peInfo.reader.GetStandaloneSignature(handle);
                    if (standaloneSig.GetKind() == StandaloneSignatureKind.LocalVariables)
                    {
                        standaloneSig.DecodeLocalSignature(parserHelper, null);
                    }
                    else
                    {
                        standaloneSig.DecodeMethodSignature(parserHelper, null);
                    }
                }

                int memberRefRowCount = peInfo.reader.GetTableRowCount(TableIndex.MemberRef);
                for (int row = 1; row <= memberRefRowCount; row++)
                {
                    var handle = MetadataTokens.MemberReferenceHandle(row);
                    var memberRef = peInfo.reader.GetMemberReference(handle);
                    if (memberRef.GetKind() == MemberReferenceKind.Method)
                    {
                        memberRef.DecodeMethodSignature(parserHelper, null);
                    }
                    else
                    {
                        memberRef.DecodeFieldSignature(parserHelper, null);
                    }
                }

                int methodSpecRowCount = peInfo.reader.GetTableRowCount(TableIndex.MethodSpec);
                for (int row = 1; row <= methodSpecRowCount; row++)
                {
                    var handle = MetadataTokens.MethodSpecificationHandle(row);
                    var methodSpec = peInfo.reader.GetMethodSpecification(handle);
                    methodSpec.DecodeSignature(parserHelper, null);
                }
            }

            // Walk all memberrefs, and attach fields/methods to types
            foreach (PEInfo peInfo in peInfos)
            {
                TypeRefSignatureParserProvider parserHelper = new TypeRefSignatureParserProvider(this, peInfo.handleLookup);

                Func<EntityHandle, NotFoundBehavior, TypeDesc> resolverFunc = ResolveTypeRefForPeInfo;
                int memberRefRowCount = peInfo.reader.GetTableRowCount(TableIndex.MemberRef);
                for (int row = 1; row <= memberRefRowCount; row++)
                {
                    var handle = MetadataTokens.MemberReferenceHandle(row);
                    var memberRef = peInfo.reader.GetMemberReference(handle);

                    TypeRefTypeSystemType ownerType = null;
                    if (memberRef.Parent.Kind == HandleKind.TypeReference)
                    {
                        ownerType = ResolveTypeRef(peInfo, (TypeReferenceHandle)memberRef.Parent);
                    }
                    else if (memberRef.Parent.Kind == HandleKind.TypeSpecification)
                    {
                        TypeDesc t = parserHelper.GetTypeFromSpecification(peInfo.reader, null, (TypeSpecificationHandle)memberRef.Parent, 0);
                        ownerType = t.GetTypeDefinition() as TypeRefTypeSystemType;
                    }
                    if (ownerType == null)
                    {
                        continue;
                    }

                    EcmaSignatureParser ecmaSigParse = new EcmaSignatureParser(this, ResolveTypeRefForPeInfo, peInfo.reader.GetBlobReader(memberRef.Signature), NotFoundBehavior.ReturnNull);
                    string name = peInfo.reader.GetString(memberRef.Name);

                    if (memberRef.GetKind() == MemberReferenceKind.Method)
                    {
                        var methodSig = ecmaSigParse.ParseMethodSignature();
                        ownerType.GetOrAddMethod(name, methodSig);
                    }
                    else
                    {
                        var fieldType = ecmaSigParse.ParseFieldSignature();
                        ownerType.GetOrAddField(name, fieldType);
                    }
                }

                TypeDesc ResolveTypeRefForPeInfo(EntityHandle handle, NotFoundBehavior notFoundBehavior)
                {
                    Debug.Assert(notFoundBehavior == NotFoundBehavior.ReturnNull);
                    TypeRefTypeSystemType type = null;
                    if (handle.Kind == HandleKind.TypeReference)
                    {
                        peInfo.handleLookup.TryGetValue((TypeReferenceHandle)handle, out type);
                    }
                    return type;
                }
            }
        }

        private TypeRefTypeSystemType ResolveTypeRef(PEInfo peInfo, TypeReferenceHandle handle)
        {
            if (peInfo.handleLookup.TryGetValue(handle, out TypeRefTypeSystemType type))
            {
                return type;
            }
            var typeReference = peInfo.reader.GetTypeReference(handle);
            if (typeReference.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                var containingType = ResolveTypeRef(peInfo, (TypeReferenceHandle)typeReference.ResolutionScope);

                string typeName = peInfo.reader.GetString(typeReference.Name);
                if (!typeReference.Namespace.IsNil)
                    typeName = peInfo.reader.GetString(typeReference.Namespace) + "." + typeName;
                type = containingType.GetOrAddNestedType(typeName);
            }
            else if (typeReference.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                AssemblyReferenceHandle asmRefHandle = (AssemblyReferenceHandle)typeReference.ResolutionScope;
                TypeRefTypeSystemModule module;

                if (!peInfo.assemblyLookup.TryGetValue(asmRefHandle, out module))
                {
                    var assemblyReference = peInfo.reader.GetAssemblyReference(asmRefHandle);
                    string assemblyName = peInfo.reader.GetString(assemblyReference.Name);
                    if (!_typeRefModules.TryGetValue(assemblyName, out module))
                    {
                        module = new TypeRefTypeSystemModule(this, new System.Reflection.AssemblyName(assemblyName));
                        _typeRefModules.Add(module.Assembly.GetName().Name, module);
                    }
                    peInfo.assemblyLookup.Add(asmRefHandle, module);
                }
                
                type = module.GetOrAddType(!typeReference.Namespace.IsNil ? peInfo.reader.GetString(typeReference.Namespace) : null, peInfo.reader.GetString(typeReference.Name));
            }
            else
            {
                type = null;
            }

            if (type != null)
            {
                peInfo.handleLookup.Add(handle, type);
            }

            return type;
        }

        public override ModuleDesc ResolveAssembly(AssemblyName name, bool throwIfNotFound = true)
        {
            if (_typeRefModules.TryGetValue(name.Name, out TypeRefTypeSystemModule foundModule))
            {
                return foundModule;
            }

            return base.ResolveAssembly(name, throwIfNotFound);
        }

        MetadataStringDecoder _metadataStringDecoder;

        public override bool SupportsCanon => true;

        public override bool SupportsUniversalCanon => false;

        public MetadataStringDecoder GetMetadataStringDecoder()
        {
            if (_metadataStringDecoder == null)
                _metadataStringDecoder = new CachingMetadataStringDecoder(0x10000); // TODO: Tune the size
            return _metadataStringDecoder;
        }
    }
}
