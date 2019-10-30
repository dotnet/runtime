// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Signature emitters need to register themselves with the manifest metadata table;
    /// this is needed so that the manifest metadata can force all signatures to materialize,
    /// and, in doing so, all extra reference modules to be emitted to the manifest metadata.
    /// </summary>
    public interface ISignatureEmitter
    {
        void MaterializeSignature();
    }

    public class ManifestMetadataTableNode : HeaderTableNode
    {
        /// <summary>
        /// Map from simple assembly names to their module indices. The map gets prepopulated
        /// with AssemblyRef's from the input module and subsequently expanded by adding entries
        /// recorded in the manifest metadata.
        /// </summary>
        private readonly Dictionary<string, int> _assemblyRefToModuleIdMap;

        /// <summary>
        /// Assembly references to store in the manifest metadata.
        /// </summary>
        private readonly List<AssemblyName> _manifestAssemblies;

        /// <summary>
        /// Registered signature emitters.
        /// </summary>
        private readonly List<ISignatureEmitter> _signatureEmitters;

        /// <summary>
        /// ID corresponding to the next manifest metadata assemblyref entry.
        /// </summary>
        private int _nextModuleId;

        /// <summary>
        /// Set to true after GetData has been called. After that, ModuleToIndex may be called no more.
        /// </summary>
        private bool _emissionCompleted;

        /// <summary>
        /// Name of the input assembly.
        /// </summary>
        private string _inputModuleName;

        public ManifestMetadataTableNode(EcmaModule inputModule)
            : base(inputModule.Context.Target)
        {
            _assemblyRefToModuleIdMap = new Dictionary<string, int>();
            _manifestAssemblies = new List<AssemblyName>();
            _signatureEmitters = new List<ISignatureEmitter>();

            _inputModuleName = inputModule.Assembly.GetName().Name;

            int assemblyRefCount = inputModule.MetadataReader.GetTableRowCount(TableIndex.AssemblyRef);
            for (int assemblyRefIndex = 1; assemblyRefIndex <= assemblyRefCount; assemblyRefIndex++)
            {
                AssemblyReferenceHandle assemblyRefHandle = MetadataTokens.AssemblyReferenceHandle(assemblyRefIndex);
                AssemblyReference assemblyRef = inputModule.MetadataReader.GetAssemblyReference(assemblyRefHandle);
                string assemblyName = inputModule.MetadataReader.GetString(assemblyRef.Name);
                _assemblyRefToModuleIdMap[assemblyName] = assemblyRefIndex;
            }

            // AssemblyRefCount + 1 corresponds to ROWID 0 in the manifest metadata
            _nextModuleId = assemblyRefCount + 2;
        }

        public void RegisterEmitter(ISignatureEmitter emitter)
        {
            _signatureEmitters.Add(emitter);
        }

        public int ModuleToIndex(EcmaModule module)
        {
            AssemblyName assemblyName = module.Assembly.GetName();
            int assemblyRefIndex;
            if (!_assemblyRefToModuleIdMap.TryGetValue(assemblyName.Name, out assemblyRefIndex))
            {
                if (_emissionCompleted)
                {
                    throw new Exception("mustn't add new assemblies after signatures have been materialized");
                }

                assemblyRefIndex = _nextModuleId++;
                _manifestAssemblies.Add(assemblyName);
                _assemblyRefToModuleIdMap.Add(assemblyName.Name, assemblyRefIndex);
            }
            return assemblyRefIndex;
        }

        public override int ClassCode => 791828335;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ManifestMetadataTableNode");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), null, 1, null);
            }

            if (!_emissionCompleted)
            {
                foreach (ISignatureEmitter emitter in _signatureEmitters)
                {
                    emitter.MaterializeSignature();
                }

                _emissionCompleted = true;
            }

            MetadataBuilder metadataBuilder = new MetadataBuilder();

            metadataBuilder.AddAssembly(
                metadataBuilder.GetOrAddString(_inputModuleName),
                new Version(0, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashAlgorithm: AssemblyHashAlgorithm.None);

            metadataBuilder.AddModule(
                0,
                metadataBuilder.GetOrAddString(_inputModuleName),
                default(GuidHandle), default(GuidHandle), default(GuidHandle));

            // Module type
            metadataBuilder.AddTypeDefinition(
               default(TypeAttributes),
               default(StringHandle),
               metadataBuilder.GetOrAddString("<Module>"),
               baseType: default(EntityHandle),
               fieldList: MetadataTokens.FieldDefinitionHandle(1),
               methodList: MetadataTokens.MethodDefinitionHandle(1));

            foreach (AssemblyName assemblyName in _manifestAssemblies)
            {
                AssemblyFlags assemblyFlags = 0;
                byte[] publicKeyOrToken;
                if ((assemblyName.Flags & AssemblyNameFlags.PublicKey) != 0)
                {
                    assemblyFlags |= AssemblyFlags.PublicKey;
                    publicKeyOrToken = assemblyName.GetPublicKey();
                }
                else
                {
                    publicKeyOrToken = assemblyName.GetPublicKeyToken();
                }
                if ((assemblyName.Flags & AssemblyNameFlags.Retargetable) != 0)
                {
                    assemblyFlags |= AssemblyFlags.Retargetable;
                }

                AssemblyReferenceHandle newHandle = metadataBuilder.AddAssemblyReference(
                    name: metadataBuilder.GetOrAddString(assemblyName.Name),
                    version: assemblyName.Version,
                    culture: metadataBuilder.GetOrAddString(assemblyName.CultureName),
                    publicKeyOrToken: metadataBuilder.GetOrAddBlob(publicKeyOrToken),
                    flags: assemblyFlags,
                    hashValue: default(BlobHandle) /* TODO */);
            }

            MetadataRootBuilder metadataRootBuilder = new MetadataRootBuilder(metadataBuilder);
            BlobBuilder metadataBlobBuilder = new BlobBuilder();
            metadataRootBuilder.Serialize(metadataBlobBuilder, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);

            return new ObjectData(
                data: metadataBlobBuilder.ToArray(),
                relocs: Array.Empty<Relocation>(),
                alignment: 1,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }
    }
}
