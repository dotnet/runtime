// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILTrim
{
    public class ILTrimTypeSystemContext : MetadataTypeSystemContext
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

            if (ReferenceFilePaths.TryGetValue(simpleName, out string filePath))
                return AddModule(filePath, simpleName);

            // TODO: the exception is wrong for two reasons: for one, this should be assembly full name, not simple name.
            // The other reason is that on CoreCLR, the exception also captures the reason. We should be passing two
            // string IDs. This makes this rather annoying.
            if (throwIfNotFound)
                ThrowHelper.ThrowFileNotFoundException(ExceptionStringID.FileLoadErrorGeneric, simpleName);

            return null;
        }

        public EcmaModule GetModuleFromPath(string filePath)
        {
            return GetOrAddModuleFromPath(filePath);
        }

        private EcmaModule GetOrAddModuleFromPath(string filePath)
        {
            // This method is not expected to be called frequently. Linear search is acceptable.
            foreach (var entry in ModuleHashtable.Enumerator.Get(_moduleHashtable))
            {
                if (entry.FilePath == filePath)
                    return entry.Module;
            }

            return AddModule(filePath, null);
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
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
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
                if (accessor != null)
                    accessor.Dispose();
                if (mappedFile != null)
                    mappedFile.Dispose();
                if (fileStream != null)
                    fileStream.Dispose();
            }
        }

        private EcmaModule AddModule(string filePath, string expectedSimpleName)
        {
            PEReader peReader = null;
            MemoryMappedViewAccessor mappedViewAccessor = null;
            try
            {
                peReader = OpenPEFile(filePath, out mappedViewAccessor);

                EcmaModule module = EcmaModule.Create(this, peReader, containingAssembly: null);

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
                    ModuleData actualModuleData = _simpleNameHashtable.AddOrGetExisting(moduleData);
                    if (actualModuleData != moduleData)
                    {
                        if (actualModuleData.FilePath != filePath)
                            throw new FileNotFoundException("Module with same simple name already exists " + filePath);
                        return actualModuleData.Module;
                    }
                    mappedViewAccessor = null; // Ownership has been transfered

                    _moduleHashtable.AddOrGetExisting(moduleData);
                }

                return module;
            }
            finally
            {
                if (mappedViewAccessor != null)
                    mappedViewAccessor.Dispose();
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

        public MetadataStringDecoder GetMetadataStringDecoder()
        {
            if (_metadataStringDecoder == null)
                _metadataStringDecoder = new CachingMetadataStringDecoder(0x10000); // TODO: Tune the size
            return _metadataStringDecoder;
        }

    }
}
