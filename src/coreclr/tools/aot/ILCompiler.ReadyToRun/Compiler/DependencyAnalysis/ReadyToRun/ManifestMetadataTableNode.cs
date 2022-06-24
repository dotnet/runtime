// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        /// Modules which need to exist in set of modules visible
        /// </summary>
        private ConcurrentBag<EcmaModule> _modulesWhichMustBeIndexable = new ConcurrentBag<EcmaModule>();

        /// <summary>
        /// Set to true after GetData has been called. After that, ModuleToIndex may be called no more.
        /// </summary>
        private bool _emissionCompleted;

        /// <summary>
        /// Node factory for the compilation
        /// </summary>
        private readonly NodeFactory _nodeFactory;

        public readonly MutableModule _mutableModule;

        public ManifestMetadataTableNode(NodeFactory nodeFactory)
            : base(nodeFactory.Target)
        {
            _assemblyRefToModuleIdMap = new Dictionary<string, int>();
            _moduleIdToAssemblyNameMap = new Dictionary<int, AssemblyName>();
            _manifestAssemblyMvids = new List<Guid>();
            _signatureEmitters = new List<ISignatureEmitter>();
            _nodeFactory = nodeFactory;
            _nextModuleId = 2;

            AssemblyHashAlgorithm hashAlgorithm = AssemblyHashAlgorithm.None;
            byte[] publicKeyBlob = null;
            AssemblyFlags manifestAssemblyFlags = default(AssemblyFlags);
            Version manifestAssemblyVersion = new Version(0, 0, 0, 0);

            if ((nodeFactory.CompositeImageSettings != null) && nodeFactory.CompilationModuleGroup.IsCompositeBuildMode)
            {
                if (nodeFactory.CompositeImageSettings.PublicKey != null)
                {
                    hashAlgorithm = AssemblyHashAlgorithm.Sha1;
                    publicKeyBlob = nodeFactory.CompositeImageSettings.PublicKey.ToArray();
                    manifestAssemblyFlags |= AssemblyFlags.PublicKey;
                }

                if (nodeFactory.CompositeImageSettings.AssemblyVersion != null)
                {
                    manifestAssemblyVersion = nodeFactory.CompositeImageSettings.AssemblyVersion;
                }
            }

            _mutableModule = new MutableModule(nodeFactory.TypeSystemContext,
                                               "ManifestMetadata",
                                               manifestAssemblyFlags,
                                               publicKeyBlob,
                                               manifestAssemblyVersion,
                                               hashAlgorithm,
                                               ModuleToIndexSingleThreadedAndSorted,
                                               nodeFactory.CompilationModuleGroup);

            if (!_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode)
            {
                MetadataReader mdReader = _nodeFactory.CompilationModuleGroup.CompilationModuleSet.Single().MetadataReader;
                _assemblyRefCount = mdReader.GetTableRowCount(TableIndex.AssemblyRef);

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

                // AssemblyRefCount + 1 corresponds to rid 0 in the manifest metadata which indicates to use the manifest metadata itself
                // AssemblyRefCount + 2 corresponds to ROWID 1 in the manifest metadata
                _nextModuleId += _assemblyRefCount;
            }

            if (_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode)
            {
                // Fill in entries for all input modules right away to make sure they have parallel indices
                int nextExpectedId = 2;
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

        private int ModuleToIndexForInputModulesOnly(ModuleDesc module)
        {
            if (!(module is EcmaModule ecmaModule))
            {
                return -1;
            }

            if (!_nodeFactory.CompilationModuleGroup.IsModuleInCompilationGroup(ecmaModule))
            {
                return -1;
            }

#if DEBUG
            int oldModuleToIndexCount = _assemblyRefToModuleIdMap.Count;
#endif
            int index = ModuleToIndexInternal(ecmaModule);
#if DEBUG
            Debug.Assert(oldModuleToIndexCount == _assemblyRefToModuleIdMap.Count);
#endif
            return index;
        }

        public void RegisterEmitter(ISignatureEmitter emitter)
        {
            _signatureEmitters.Add(emitter);
        }

        public int ModuleToIndex(IEcmaModule module)
        {
            if (!_nodeFactory.MarkingComplete)
            {
                // If we call this function before sorting is complete, we might have a determinism bug caused by
                // compiling two functions in an arbitrary order and hence getting different module IDs.
                throw new InvalidOperationException("Cannot get ModuleToIndex mapping until marking is complete.");
            }

            return ModuleToIndexInternal(module);
        }

        // This function may only be called when all multithreading is disabled, and must only be called in a deterministic fashion.
        private int ModuleToIndexSingleThreadedAndSorted(ModuleDesc module)
        {
            return ModuleToIndexInternal((IEcmaModule)module);
        }

        public void EnsureModuleIndexable(ModuleDesc module)
        {
            if (_emissionCompleted)
            {
                throw new InvalidOperationException("Adding a new assembly after signatures have been materialized.");
            }

            if (module is EcmaModule ecmaModule && _nodeFactory.CompilationModuleGroup.VersionsWithModule(ecmaModule))
            {
                _modulesWhichMustBeIndexable.Add(ecmaModule);
            }
        }

        private int ModuleToIndexInternal(IEcmaModule module)
        {
            Debug.Assert(module != null);
            EcmaModule emodule = module as EcmaModule;
            if (emodule == null)
            {
                Debug.Assert(module == _mutableModule);
                return _assemblyRefCount + 1;
            }

            if (!_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode && (_nodeFactory.CompilationModuleGroup.CompilationModuleSet.Single() == module))
            {
                // Must be a reference to the only module being compiled
                return 0;
            }

            AssemblyName assemblyName = emodule.Assembly.GetName();
            int assemblyRefIndex;
            if (!_assemblyRefToModuleIdMap.TryGetValue(assemblyName.Name, out assemblyRefIndex))
            {
                assemblyRefIndex = _nextModuleId++;
                _assemblyRefToModuleIdMap.Add(assemblyName.Name, assemblyRefIndex);
            }

            if (assemblyRefIndex > _assemblyRefCount && !_moduleIdToAssemblyNameMap.ContainsKey(assemblyRefIndex))
            {
                if (_emissionCompleted)
                {
                    throw new InvalidOperationException("Adding a new assembly after signatures have been materialized.");
                }

                _moduleIdToAssemblyNameMap.Add(assemblyRefIndex, assemblyName);
                if (_nodeFactory.CompilationModuleGroup.VersionsWithModule(emodule))
                {
                    _manifestAssemblyMvids.Add(module.MetadataReader.GetGuid(module.MetadataReader.GetModuleDefinition().Mvid));
                }
                else
                {
                    Debug.Assert(_nodeFactory.CompilationModuleGroup.CrossModuleInlineableModule(emodule));
                    _manifestAssemblyMvids.Add(default(Guid));
                }
            }
            return assemblyRefIndex;
        }

        public override int ClassCode => 791828335;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ManifestMetadataTableNode");
        }

        private void ComputeLastSetOfModuleIndices()
        {
            if (!_emissionCompleted)
            {
                foreach (ISignatureEmitter emitter in _signatureEmitters)
                {
                    emitter.MaterializeSignature();
                }

                EcmaModule [] moduleArray = _modulesWhichMustBeIndexable.ToArray();
                Array.Sort(moduleArray, (EcmaModule moduleA, EcmaModule moduleB) => moduleA.CompareTo(moduleB));
                foreach (var module in moduleArray)
                {
                    ModuleToIndex(module);
                }

                _emissionCompleted = true;
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), null, 1, null);
            }

            ComputeLastSetOfModuleIndices();

            foreach (var idAndAssemblyName in _moduleIdToAssemblyNameMap.OrderBy(x => x.Key))
            {
                AssemblyName assemblyName = idAndAssemblyName.Value;
                var handle = _mutableModule.TryGetAssemblyRefHandle(assemblyName);
                Debug.Assert(handle.HasValue);
                Debug.Assert(((handle.Value & 0xFFFFFF) + (_assemblyRefCount)) == (idAndAssemblyName.Key - 1));
            }

            // After this point new tokens will not be embedded in the final image
            _mutableModule.DisableNewTokens = true;

            return new ObjectData(
                data: _mutableModule.MetadataBlob,
                relocs: Array.Empty<Relocation>(),
                alignment: 1,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        private const int GuidByteSize = 16;

        public int ManifestAssemblyMvidTableSize => GuidByteSize * _manifestAssemblyMvids.Count;

        internal byte[] GetManifestAssemblyMvidTableData()
        {
            ComputeLastSetOfModuleIndices();

            byte[] manifestAssemblyMvidTable = new byte[ManifestAssemblyMvidTableSize];
            for (int i = 0; i < _manifestAssemblyMvids.Count; i++)
            {
                _manifestAssemblyMvids[i].TryWriteBytes(new Span<byte>(manifestAssemblyMvidTable, GuidByteSize * i, GuidByteSize));
            }
            return manifestAssemblyMvidTable;
        }
    }
}
