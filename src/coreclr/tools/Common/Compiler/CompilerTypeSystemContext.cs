// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext : MetadataTypeSystemContext, IMetadataStringDecoderProvider
    {
        private readonly MetadataRuntimeInterfacesAlgorithm _metadataRuntimeInterfacesAlgorithm = new MetadataRuntimeInterfacesAlgorithm();
        private readonly MetadataVirtualMethodAlgorithm _virtualMethodAlgorithm = new MetadataVirtualMethodAlgorithm();

        private MetadataStringDecoder _metadataStringDecoder;

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

        private class SimpleNameHashtable : LockFreeReaderHashtable<string, ModuleData>
        {
            StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

            protected override int GetKeyHashCode(string key)
            {
                return _comparer.GetHashCode(key);
            }
            protected override int GetValueHashCode(ModuleData value)
            {
                return _comparer.GetHashCode(value.SimpleName);
            }
            protected override bool CompareKeyToValue(string key, ModuleData value)
            {
                return _comparer.Equals(key, value.SimpleName);
            }
            protected override bool CompareValueToValue(ModuleData value1, ModuleData value2)
            {
                return _comparer.Equals(value1.SimpleName, value2.SimpleName);
            }
            protected override ModuleData CreateValueFromKey(string key)
            {
                Debug.Fail("CreateValueFromKey not supported");
                return null;
            }
        }
        private readonly SimpleNameHashtable _simpleNameHashtable = new SimpleNameHashtable();

        private readonly SharedGenericsMode _genericsMode;

        public IReadOnlyDictionary<string, string> InputFilePaths
        {
            get;
            set;
        }

        public IReadOnlyDictionary<string, string> ReferenceFilePaths
        {
            get;
            set;
        }

        public override ModuleDesc ResolveAssembly(System.Reflection.AssemblyName name, bool throwIfNotFound)
        {
            // TODO: catch typesystem BadImageFormatException and throw a new one that also captures the
            // assembly name that caused the failure. (Along with the reason, which makes this rather annoying).
            return GetModuleForSimpleName(name.Name, throwIfNotFound);
        }

        public EcmaModule GetModuleForSimpleName(string simpleName, bool throwIfNotFound = true)
        {
            if (_simpleNameHashtable.TryGetValue(simpleName, out ModuleData existing))
                return existing.Module;

            if (InputFilePaths.TryGetValue(simpleName, out string filePath)
                || ReferenceFilePaths.TryGetValue(simpleName, out filePath))
                return AddModule(filePath, simpleName, true);

            // TODO: the exception is wrong for two reasons: for one, this should be assembly full name, not simple name.
            // The other reason is that on CoreCLR, the exception also captures the reason. We should be passing two
            // string IDs. This makes this rather annoying.
            if (throwIfNotFound)
                ThrowHelper.ThrowFileNotFoundException(ExceptionStringID.FileLoadErrorGeneric, simpleName);

            return null;
        }

        public EcmaModule GetModuleFromPath(string filePath, bool throwOnFailureToLoad = true)
        {
            return GetOrAddModuleFromPath(filePath, true, throwOnFailureToLoad: throwOnFailureToLoad);
        }

        public EcmaModule GetMetadataOnlyModuleFromPath(string filePath)
        {
            return GetOrAddModuleFromPath(filePath, false);
        }

        private EcmaModule GetOrAddModuleFromPath(string filePath, bool useForBinding, bool throwOnFailureToLoad = true)
        {
            filePath = Path.GetFullPath(filePath);

            // This method is not expected to be called frequently. Linear search is acceptable.
            foreach (var entry in ModuleHashtable.Enumerator.Get(_moduleHashtable))
            {
                if (entry.FilePath == filePath)
                    return entry.Module;
            }

            return AddModule(filePath, null, useForBinding, throwOnFailureToLoad: throwOnFailureToLoad);
        }

        public static unsafe PEReader OpenPEFile(string filePath, out MemoryMappedViewAccessor mappedViewAccessor)
        {
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

        private EcmaModule AddModule(string filePath, string expectedSimpleName, bool useForBinding, ModuleData oldModuleData = null, bool throwOnFailureToLoad = true)
        {
            filePath = Path.GetFullPath(filePath);

            PEReader peReader = null;
            MemoryMappedViewAccessor mappedViewAccessor = null;
            PdbSymbolReader pdbReader = null;
            try
            {
                if (oldModuleData == null)
                {
                    peReader = OpenPEFile(filePath, out mappedViewAccessor);

#if !READYTORUN
                if (peReader.HasMetadata && (peReader.PEHeaders.CorHeader.Flags & (CorFlags.ILLibrary | CorFlags.ILOnly)) == 0)
                    throw new NotSupportedException($"Error: C++/CLI is not supported: '{filePath}'");
#endif

                    pdbReader = PortablePdbSymbolReader.TryOpenEmbedded(peReader, GetMetadataStringDecoder()) ?? OpenAssociatedSymbolFile(filePath, peReader);
                }
                else
                {
                    filePath = oldModuleData.FilePath;
                    peReader = oldModuleData.Module.PEReader;
                    mappedViewAccessor = oldModuleData.MappedViewAccessor;
                    pdbReader = oldModuleData.Module.PdbReader;
                }

                if (!peReader.HasMetadata && !throwOnFailureToLoad)
                {
                    return null;
                }

                EcmaModule module = EcmaModule.Create(this, peReader, containingAssembly: null, pdbReader);

                MetadataReader metadataReader = module.MetadataReader;
                string simpleName = metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name);

                if (expectedSimpleName != null && !simpleName.Equals(expectedSimpleName, StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Assembly name does not match filename " + filePath);

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
                        ModuleData actualModuleData = _simpleNameHashtable.AddOrGetExisting(moduleData);
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
            finally
            {
                if (mappedViewAccessor != null)
                    mappedViewAccessor.Dispose();
                if (pdbReader != null)
                    pdbReader.Dispose();
            }
        }

        protected void InheritOpenModules(CompilerTypeSystemContext oldTypeSystemContext)
        {
            foreach (ModuleData oldModuleData in ModuleHashtable.Enumerator.Get(oldTypeSystemContext._moduleHashtable))
            {
                AddModule(null, null, true, oldModuleData);
            }
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForDefType(DefType type)
        {
            return _metadataRuntimeInterfacesAlgorithm;
        }

        public override VirtualMethodAlgorithm GetVirtualMethodAlgorithmForType(TypeDesc type)
        {
            Debug.Assert(!type.IsArray, "Wanted to call GetClosestMetadataType?");

            return _virtualMethodAlgorithm;
        }

        protected override Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind, out bool changed)
        {
            if (_genericsMode == SharedGenericsMode.CanonicalReferenceTypes)
                return RuntimeDeterminedCanonicalizationAlgorithm.ConvertInstantiationToCanonForm(instantiation, kind, out changed);

            Debug.Assert(_genericsMode == SharedGenericsMode.Disabled);
            changed = false;
            return instantiation;
        }

        protected override TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
        {
            if (_genericsMode == SharedGenericsMode.CanonicalReferenceTypes)
                return RuntimeDeterminedCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, kind);

            Debug.Assert(_genericsMode == SharedGenericsMode.Disabled);
            return typeToConvert;
        }

        protected override TypeDesc ConvertToCanon(TypeDesc typeToConvert, ref CanonicalFormKind kind)
        {
            if (_genericsMode == SharedGenericsMode.CanonicalReferenceTypes)
                return RuntimeDeterminedCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, ref kind);

            Debug.Assert(_genericsMode == SharedGenericsMode.Disabled);
            return typeToConvert;
        }

        public override bool SupportsUniversalCanon => false;
        public override bool SupportsCanon => _genericsMode != SharedGenericsMode.Disabled;

        public MetadataStringDecoder GetMetadataStringDecoder()
        {
            if (_metadataStringDecoder == null)
                _metadataStringDecoder = new CachingMetadataStringDecoder(0x10000); // TODO: Tune the size
            return _metadataStringDecoder;
        }

        //
        // Symbols
        //

        private PdbSymbolReader OpenAssociatedSymbolFile(string peFilePath, PEReader peReader)
        {
            string pdbFileName = null;
            BlobContentId pdbContentId = default;

            foreach (DebugDirectoryEntry debugEntry in peReader.ReadDebugDirectory())
            {
                if (debugEntry.Type != DebugDirectoryEntryType.CodeView)
                    continue;

                CodeViewDebugDirectoryData debugDirectoryData = peReader.ReadCodeViewDebugDirectoryData(debugEntry);

                string candidatePath  = debugDirectoryData.Path;
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
            if (reader == null)
            {
                // Fallback to the diasymreader for non-portable pdbs
                reader = UnmanagedPdbSymbolReader.TryOpenSymbolReaderForMetadataFile(peFilePath, Path.GetDirectoryName(pdbFileName));
            }

            return reader;
        }
    }

    /// <summary>
    /// Specifies the mode in which canonicalization should occur.
    /// </summary>
    public enum SharedGenericsMode
    {
        Disabled,
        CanonicalReferenceTypes,
    }
}
