// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Map from module index to the AssemblyName for the module. This only contains modules
        /// that were actually loaded and is populated by ModuleToIndex.
        /// </summary>
        private readonly Dictionary<int, AssemblyName> _moduleIdToAssemblyNameMap;

        /// <summary>
        /// MVIDs of the assemblies included in manifest metadata to be emitted as the
        /// ManifestAssemblyMvid R2R header table used by the runtime to check loaded assemblies
        /// and fail fast in case of mismatch.
        /// </summary>
        private readonly List<Guid> _manifestAssemblyMvids;

        /// <summary>
        /// Registered signature emitters.
        /// </summary>
        private readonly List<ISignatureEmitter> _signatureEmitters;

        /// <summary>
        /// Number of assembly references in the input module
        /// </summary>
        private int _assemblyRefCount;

        /// <summary>
        /// ID corresponding to the next manifest metadata assemblyref entry.
        /// </summary>
        private int _nextModuleId;

        /// <summary>
        /// Set to true after GetData has been called. After that, ModuleToIndex may be called no more.
        /// </summary>
        private bool _emissionCompleted;

        /// <summary>
        /// Node factory for the compilation
        /// </summary>
        private readonly NodeFactory _nodeFactory;

        public ManifestMetadataTableNode(NodeFactory nodeFactory)
            : base(nodeFactory.Target)
        {
            _assemblyRefToModuleIdMap = new Dictionary<string, int>();
            _moduleIdToAssemblyNameMap = new Dictionary<int, AssemblyName>();
            _manifestAssemblyMvids = new List<Guid>();
            _signatureEmitters = new List<ISignatureEmitter>();
            _nodeFactory = nodeFactory;
            _nextModuleId = 1;

            if (!_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode)
            {
                MetadataReader mdReader = _nodeFactory.CompilationModuleGroup.CompilationModuleSet.Single().MetadataReader;
                _assemblyRefCount = mdReader.GetTableRowCount(TableIndex.AssemblyRef) + 1;

                if (!_nodeFactory.CompilationModuleGroup.IsInputBubble)
                {
                    for (int assemblyRefIndex = 1; assemblyRefIndex < _assemblyRefCount; assemblyRefIndex++)
                    {
                        AssemblyReferenceHandle assemblyRefHandle = MetadataTokens.AssemblyReferenceHandle(assemblyRefIndex);
                        AssemblyReference assemblyRef = mdReader.GetAssemblyReference(assemblyRefHandle);
                        string assemblyName = mdReader.GetString(assemblyRef.Name);
                        _assemblyRefToModuleIdMap[assemblyName] = assemblyRefIndex;
                    }
                }

                // AssemblyRefCount + 1 corresponds to ROWID 0 in the manifest metadata
                _nextModuleId += _assemblyRefCount;
            }

            if (_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode)
            {
                // Fill in entries for all input modules right away to make sure they have parallel indices
                int nextExpectedId = 1;
                foreach (EcmaModule inputModule in _nodeFactory.CompilationModuleGroup.CompilationModuleSet)
                {
                    int acquiredId = ModuleToIndexInternal(inputModule);
                    if (acquiredId != nextExpectedId)
                    {
                        throw new InternalCompilerErrorException($"Manifest metadata consistency error - acquired ID {acquiredId}, expected {nextExpectedId}");
                    }
                    nextExpectedId++;
                }
            }
        }

        public void RegisterEmitter(ISignatureEmitter emitter)
        {
            _signatureEmitters.Add(emitter);
        }

        public int ModuleToIndex(EcmaModule module)
        {
            if (!_nodeFactory.MarkingComplete)
            {
                // If we call this function before sorting is complete, we might have a determinism bug caused by
                // compiling two functions in an arbitrary order and hence getting different module IDs.
                throw new InvalidOperationException("Cannot get ModuleToIndex mapping until marking is complete.");
            }

            return ModuleToIndexInternal(module);
        }

        private int ModuleToIndexInternal(EcmaModule module)
        {
            AssemblyName assemblyName = module.Assembly.GetName();
            int assemblyRefIndex;
            if (!_assemblyRefToModuleIdMap.TryGetValue(assemblyName.Name, out assemblyRefIndex))
            {
                assemblyRefIndex = _nextModuleId++;
                _assemblyRefToModuleIdMap.Add(assemblyName.Name, assemblyRefIndex);
            }

            if (assemblyRefIndex >= _assemblyRefCount && !_moduleIdToAssemblyNameMap.ContainsKey(assemblyRefIndex))
            {
                if (_emissionCompleted)
                {
                    throw new InvalidOperationException("Adding a new assembly after signatures have been materialized.");
                }

                // If we're going to add a module to the manifest, it has to be part of the version bubble, otherwise
                // the verification logic would be broken at runtime.
                Debug.Assert(_nodeFactory.CompilationModuleGroup.VersionsWithModule(module));

                _moduleIdToAssemblyNameMap.Add(assemblyRefIndex, assemblyName);
                _manifestAssemblyMvids.Add(module.MetadataReader.GetGuid(module.MetadataReader.GetModuleDefinition().Mvid));
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

            AssemblyHashAlgorithm hashAlgorithm = AssemblyHashAlgorithm.None;
            BlobHandle publicKeyBlob = default(BlobHandle);
            AssemblyFlags manifestAssemblyFlags = default(AssemblyFlags);
            Version manifestAssemblyVersion = new Version(0, 0, 0, 0);

            if ((factory.CompositeImageSettings != null) && factory.CompilationModuleGroup.IsCompositeBuildMode)
            {
                if (factory.CompositeImageSettings.PublicKey != null)
                {
                    hashAlgorithm = AssemblyHashAlgorithm.Sha1;
                    publicKeyBlob = metadataBuilder.GetOrAddBlob(factory.CompositeImageSettings.PublicKey);
                    manifestAssemblyFlags |= AssemblyFlags.PublicKey;
                }

                if (factory.CompositeImageSettings.AssemblyVersion != null)
                {
                    manifestAssemblyVersion = factory.CompositeImageSettings.AssemblyVersion;
                }
            }

            string manifestMetadataAssemblyName = "ManifestMetadata";
            metadataBuilder.AddAssembly(
                metadataBuilder.GetOrAddString(manifestMetadataAssemblyName),
                manifestAssemblyVersion,
                culture: default(StringHandle),
                publicKey: publicKeyBlob,
                flags: manifestAssemblyFlags,
                hashAlgorithm: hashAlgorithm);

            metadataBuilder.AddModule(
                0,
                metadataBuilder.GetOrAddString(manifestMetadataAssemblyName),
                default(GuidHandle), default(GuidHandle), default(GuidHandle));

            // Module type
            metadataBuilder.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadataBuilder.GetOrAddString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            foreach (var idAndAssemblyName in _moduleIdToAssemblyNameMap.OrderBy(x => x.Key))
            {
                AssemblyName assemblyName = idAndAssemblyName.Value;
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

        private const int GuidByteSize = 16;

        public int ManifestAssemblyMvidTableSize => GuidByteSize * _manifestAssemblyMvids.Count;

        internal byte[] GetManifestAssemblyMvidTableData()
        {
            byte[] manifestAssemblyMvidTable = new byte[ManifestAssemblyMvidTableSize];
            for (int i = 0; i < _manifestAssemblyMvids.Count; i++)
            {
                _manifestAssemblyMvids[i].TryWriteBytes(new Span<byte>(manifestAssemblyMvidTable, GuidByteSize * i, GuidByteSize));
            }
            return manifestAssemblyMvidTable;
        }
    }
}
