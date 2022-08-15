// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Microsoft.Diagnostics.Tracing.Etlx;
using System.IO;
using System.IO.MemoryMappedFiles;

using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Reflection.Metadata;
using ILCompiler.Reflection.ReadyToRun;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    class TraceTypeSystemContext : MetadataTypeSystemContext, IMetadataStringDecoderProvider, IAssemblyResolver
    {
        private readonly PgoTraceProcess _pgoTraceProcess;
        private readonly ModuleLoadLogger _moduleLoadLogger;
        private int _clrInstanceID;
        private bool _automaticReferences;

        public readonly Dictionary<string,string> _normalizedFilePathToFilePath = new Dictionary<string,string> (StringComparer.OrdinalIgnoreCase);

        public TraceTypeSystemContext(PgoTraceProcess traceProcess, int clrInstanceID, Logger logger, bool automaticReferences)
        {
            _automaticReferences = automaticReferences;
            foreach (var traceData in traceProcess.TraceProcess.EventsInProcess.ByEventType<ModuleLoadUnloadTraceData>())
            {
                if (traceData.ModuleILPath != null)
                {
                    _normalizedFilePathToFilePath[traceData.ModuleILPath] = traceData.ModuleILPath;
                }
            }

            _pgoTraceProcess = traceProcess;
            _clrInstanceID = clrInstanceID;
            _moduleLoadLogger = new ModuleLoadLogger(logger);
        }

        public bool Initialize()
        {
            ModuleDesc systemModule = GetModuleForSimpleName("System.Private.CoreLib", false);
            if (systemModule == null)
                return false;
            SetSystemModule(systemModule);
            return true;
        }

        public override bool SupportsCanon => true;

        public override bool SupportsUniversalCanon => false;

        private class ModuleData
        {
            public string SimpleName;
            public string FilePath;

            public EcmaModule Module;
            public MemoryMappedViewAccessor MappedViewAccessor;
        }

        private class ModuleHashtable : LockFreeReaderHashtable<EcmaModule, ModuleData>
        {
            protected override int GetKeyHashCode(EcmaModule key)
            {
                return key.GetHashCode();
            }
            protected override int GetValueHashCode(ModuleData value)
            {
                return value.Module.GetHashCode();
            }
            protected override bool CompareKeyToValue(EcmaModule key, ModuleData value)
            {
                return Object.ReferenceEquals(key, value.Module);
            }
            protected override bool CompareValueToValue(ModuleData value1, ModuleData value2)
            {
                return Object.ReferenceEquals(value1.Module, value2.Module);
            }
            protected override ModuleData CreateValueFromKey(EcmaModule key)
            {
                Debug.Fail("CreateValueFromKey not supported");
                return null;
            }
        }
        private readonly ModuleHashtable _moduleHashtable = new ModuleHashtable();

        private readonly Dictionary<string, ModuleData> _simpleNameHashtable = new Dictionary<string, ModuleData>(StringComparer.OrdinalIgnoreCase);

        public override ModuleDesc ResolveAssembly(System.Reflection.AssemblyName name, bool throwIfNotFound)
        {
            // TODO: catch typesystem BadImageFormatException and throw a new one that also captures the
            // assembly name that caused the failure. (Along with the reason, which makes this rather annoying).
            return GetModuleForSimpleName(name.Name, throwIfNotFound);
        }

        public ModuleDesc GetModuleForSimpleName(string simpleName, bool throwIfNotFound = true)
        {
            lock (this)
            {
                ModuleData existing;
                if (_simpleNameHashtable.TryGetValue(simpleName, out existing))
                {
                    if (existing == null)
                    {
                        if (throwIfNotFound)
                        {
                            ThrowHelper.ThrowFileNotFoundException(ExceptionStringID.FileLoadErrorGeneric, simpleName);
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return existing.Module;
                }

                string filePath = null;

                if (_automaticReferences)
                {
                    foreach (var module in _pgoTraceProcess.EnumerateLoadedManagedModules())
                    {
                        var managedModule = module.ManagedModule;

                        if (module.ClrInstanceID != _clrInstanceID)
                            continue;

                        if (PgoTraceProcess.CompareModuleAgainstSimpleName(simpleName, managedModule))
                        {
                            string filePathTemp = PgoTraceProcess.ComputeFilePathOnDiskForModule(managedModule);

                            // This path may be normalized
                            if (File.Exists(filePathTemp) || !_normalizedFilePathToFilePath.TryGetValue(filePathTemp, out filePath))
                                filePath = filePathTemp;
                            break;
                        }
                    }
                }

                if (filePath == null)
                {
                    // TODO: the exception is wrong for two reasons: for one, this should be assembly full name, not simple name.
                    // The other reason is that on CoreCLR, the exception also captures the reason. We should be passing two
                    // string IDs. This makes this rather annoying.

                    _moduleLoadLogger.LogModuleLoadFailure(simpleName);

                    if (throwIfNotFound)
                        ThrowHelper.ThrowFileNotFoundException(ExceptionStringID.FileLoadErrorGeneric, simpleName);

                    return null;
                }

                bool succeededOrReportedError = false;
                try
                {
                    ModuleDesc returnValue = AddModule(filePath, simpleName, null, true);
                    _moduleLoadLogger.LogModuleLoadSuccess(simpleName, filePath);
                    succeededOrReportedError = true;
                    return returnValue;
                }
                catch (Exception) when (!throwIfNotFound)
                {
                    _moduleLoadLogger.LogModuleLoadFailure(simpleName, filePath);
                    succeededOrReportedError = true;
                    _simpleNameHashtable.Add(simpleName, null);
                    return null;
                }
                finally
                {
                    if (!succeededOrReportedError)
                    {
                        _moduleLoadLogger.LogModuleLoadFailure(simpleName, filePath);
                        _simpleNameHashtable.Add(simpleName, null);
                    }
                }
            }
        }

        public EcmaModule GetModuleFromPath(string filePath, bool throwIfNotLoadable = true)
        {
            return GetOrAddModuleFromPath(filePath, null, true, throwIfNotLoadable: throwIfNotLoadable);
        }

        public EcmaModule GetMetadataOnlyModuleFromPath(string filePath)
        {
            return GetOrAddModuleFromPath(filePath, null, false);
        }

        public EcmaModule GetMetadataOnlyModuleFromMemory(string filePath, byte[] moduleData)
        {
            return GetOrAddModuleFromPath(filePath, moduleData, false);
        }

        private EcmaModule GetOrAddModuleFromPath(string filePath, byte[] moduleData, bool useForBinding, bool throwIfNotLoadable = true)
        {
            // This method is not expected to be called frequently. Linear search is acceptable.
            foreach (var entry in ModuleHashtable.Enumerator.Get(_moduleHashtable))
            {
                if (entry.FilePath == filePath)
                    return entry.Module;
            }

            bool succeeded = false;
            try
            {
                EcmaModule returnValue = AddModule(filePath, null, moduleData, useForBinding, throwIfNotLoadable: throwIfNotLoadable);
                if (returnValue != null)
                {
                    _moduleLoadLogger.LogModuleLoadSuccess(returnValue.Assembly.GetName().Name, filePath);
                    succeeded = true;
                    return returnValue;
                }
            }
            finally
            {
                if (!succeeded)
                {
                    _moduleLoadLogger.LogModuleLoadFailure(Path.GetFileNameWithoutExtension(filePath), filePath);
                }
            }
            return null;
        }

        private static ConditionalWeakTable<PEReader, string> s_peReaderToPath = new ConditionalWeakTable<PEReader, string>();

        // Get the file path used to load a PEReader or "Memory" if it wasn't loaded from a file
        public string PEReaderToFilePath(PEReader reader)
        {
            if (!s_peReaderToPath.TryGetValue(reader, out string filepath))
            {
                filepath = "Memory";
            }

            return filepath;
        }

        public static unsafe PEReader OpenPEFile(string filePath, byte[] moduleBytes, out MemoryMappedViewAccessor mappedViewAccessor)
        {
            // If moduleBytes is specified create PEReader from the in memory array, not from a file on disk
            if (moduleBytes != null)
            {
                var peReader = new PEReader(ImmutableArray.Create<byte>(moduleBytes));
                mappedViewAccessor = null;
                return peReader;
            }

            // System.Reflection.Metadata has heuristic that tries to save virtual address space. This heuristic does not work
            // well for us since it can make IL access very slow (call to OS for each method IL query). We will map the file
            // ourselves to get the desired performance characteristics reliably.

            FileStream fileStream = null;
            MemoryMappedFile mappedFile = null;
            MemoryMappedViewAccessor accessor = null;
            try
            {
                // Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
                mappedFile = MemoryMappedFile.CreateFromFile(
                    fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                var safeBuffer = accessor.SafeMemoryMappedViewHandle;
                var peReader = new PEReader((byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength);
                s_peReaderToPath.Add(peReader, filePath);

                // MemoryMappedFile does not need to be kept around. MemoryMappedViewAccessor is enough.

                mappedViewAccessor = accessor;
                accessor = null;

                return peReader;
            }
            finally
            {
                accessor?.Dispose();
                mappedFile?.Dispose();
                fileStream?.Dispose();
            }
        }

        private EcmaModule AddModule(string filePath, string expectedSimpleName, byte[] moduleDataBytes, bool useForBinding, bool throwIfNotLoadable = true)
        {
            MemoryMappedViewAccessor mappedViewAccessor = null;
            PdbSymbolReader pdbReader = null;
            try
            {
                PEReader peReader = OpenPEFile(filePath, moduleDataBytes, out mappedViewAccessor);
                if ((!peReader.HasMetadata) && !throwIfNotLoadable)
                {
                    return null;
                }
                pdbReader = OpenAssociatedSymbolFile(filePath, peReader);

                EcmaModule module = EcmaModule.Create(this, peReader, containingAssembly: null, pdbReader);

                MetadataReader metadataReader = module.MetadataReader;
                string simpleName = metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name);

                ModuleData moduleData = new ModuleData()
                {
                    SimpleName = simpleName,
                    FilePath = filePath,
                    Module = module,
                    MappedViewAccessor = mappedViewAccessor
                };

                lock (this)
                {
                    if (useForBinding)
                    {
                        ModuleData actualModuleData;

                        if (!_simpleNameHashtable.TryGetValue(moduleData.SimpleName, out actualModuleData))
                        {
                            _simpleNameHashtable.Add(moduleData.SimpleName, moduleData);
                            actualModuleData = moduleData;
                        }
                        if (actualModuleData != moduleData)
                        {
                            if (actualModuleData.FilePath != filePath)
                                throw new FileNotFoundException("Module with same simple name already exists " + filePath);
                            return actualModuleData.Module;
                        }
                    }
                    mappedViewAccessor = null; // Ownership has been transferred
                    pdbReader = null; // Ownership has been transferred

                    _moduleHashtable.AddOrGetExisting(moduleData);
                }

                return module;
            }
            catch when (!throwIfNotLoadable)
            {
                return null;
            }
            finally
            {
                if (mappedViewAccessor != null)
                    mappedViewAccessor.Dispose();
                if (pdbReader != null)
                    pdbReader.Dispose();
            }
        }


        //
        // Symbols
        //

        private PdbSymbolReader OpenAssociatedSymbolFile(string peFilePath, PEReader peReader)
        {
            string pdbFileName = null;
            BlobContentId pdbContentId = default;

            foreach (DebugDirectoryEntry debugEntry in peReader.SafeReadDebugDirectory())
            {
                if (debugEntry.Type != DebugDirectoryEntryType.CodeView)
                    continue;

                CodeViewDebugDirectoryData debugDirectoryData = peReader.ReadCodeViewDebugDirectoryData(debugEntry);

                string candidatePath = debugDirectoryData.Path;
                if (!Path.IsPathRooted(candidatePath) || !File.Exists(candidatePath))
                {
                    // Also check next to the PE file
                    candidatePath = Path.Combine(Path.GetDirectoryName(peFilePath), Path.GetFileName(candidatePath));
                    if (!File.Exists(candidatePath))
                        continue;
                }

                pdbFileName = candidatePath;
                pdbContentId = new BlobContentId(debugDirectoryData.Guid, debugEntry.Stamp);
                break;
            }

            if (pdbFileName == null)
                return null;

            // Try to open the symbol file as portable pdb first
            PdbSymbolReader reader = PortablePdbSymbolReader.TryOpen(pdbFileName, GetMetadataStringDecoder(), pdbContentId);

            return reader;
        }


        private MetadataStringDecoder _metadataStringDecoder;

        public MetadataStringDecoder GetMetadataStringDecoder()
        {
            if (_metadataStringDecoder == null)
                _metadataStringDecoder = new CachingMetadataStringDecoder(0x10000); // TODO: Tune the size
            return _metadataStringDecoder;
        }

        IAssemblyMetadata IAssemblyResolver.FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile)
        {
            using var triggerErrors = new ModuleLoadLogger.LoadFailuresAsErrors();
            EcmaAssembly ecmaAssembly = (EcmaAssembly)this.GetModuleForSimpleName(metadataReader.GetString(metadataReader.GetAssemblyReference(assemblyReferenceHandle).Name), false);
            return new StandaloneAssemblyMetadata(ecmaAssembly.PEReader);
        }

        IAssemblyMetadata IAssemblyResolver.FindAssembly(string simpleName, string parentFile)
        {
            using var triggerErrors = new ModuleLoadLogger.LoadFailuresAsErrors();
            EcmaAssembly ecmaAssembly = (EcmaAssembly)this.GetModuleForSimpleName(simpleName, false);
            return new StandaloneAssemblyMetadata(ecmaAssembly.PEReader);
        }
    }
}
