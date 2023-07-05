// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime.Augments;
using Internal.Metadata.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// This class represents basic information about a native binary module including its
    /// metadata.
    /// </summary>
    public unsafe class ModuleInfo
    {
        /// <summary>
        /// Module handle is the TypeManager associated with this module.
        /// </summary>
        public TypeManagerHandle Handle { get; private set; }

        /// <summary>
        /// Initialize module info and construct per-module metadata reader.
        /// </summary>
        /// <param name="moduleHandle">Handle (address) of module to initialize</param>
        internal ModuleInfo(TypeManagerHandle moduleHandle)
        {
            Handle = moduleHandle;
        }

    }

    public class NativeFormatModuleInfo : ModuleInfo
    {
        /// <summary>
        /// Initialize module info and construct per-module metadata reader.
        /// </summary>
        /// <param name="moduleHandle">Handle (address) of module to initialize</param>
        /// <param name="pBlob">Module blob start address</param>
        /// <param name="cbBlob">Module blob length</param>
        internal NativeFormatModuleInfo(TypeManagerHandle moduleHandle, IntPtr pBlob, int cbBlob) : base(moduleHandle)
        {
            MetadataReader = new MetadataReader((IntPtr)pBlob, (int)cbBlob);
        }

        /// <summary>
        /// Module metadata reader for NativeFormat metadata
        /// </summary>
        public MetadataReader MetadataReader { get; private set; }

        internal unsafe bool TryFindBlob(ReflectionMapBlob blobId, out byte* pBlob, out uint cbBlob)
        {
            pBlob = null;
            cbBlob = 0;
            fixed (byte** ppBlob = &pBlob)
            {
                fixed (uint* pcbBlob = &cbBlob)
                {
                    return RuntimeAugments.FindBlob(Handle, (int)blobId, new IntPtr(ppBlob), new IntPtr(pcbBlob));
                }
            }
        }

        public unsafe bool TryFindBlob(int blobId, out byte* pBlob, out uint cbBlob)
        {
            pBlob = null;
            cbBlob = 0;
            fixed (byte** ppBlob = &pBlob)
            {
                fixed (uint* pcbBlob = &cbBlob)
                {
                    return RuntimeAugments.FindBlob(Handle, (int)blobId, new IntPtr(ppBlob), new IntPtr(pcbBlob));
                }
            }
        }
    }

    /// <summary>
    /// This class represents a linear module list and a dictionary mapping module handles
    /// to its indices. When a new module is registered, a new instance of this class gets
    /// constructed and atomically updates the _loadedModuleMap so that at any point in time
    /// all threads see the map as consistent.
    /// </summary>
    internal sealed class ModuleMap
    {
        /// <summary>
        /// Array of loaded binary modules.
        /// </summary>
        public readonly ModuleInfo[] Modules;

        /// <summary>
        /// Map of module handles to indices within the Modules array.
        /// </summary>
        public readonly LowLevelDictionary<TypeManagerHandle, int> HandleToModuleIndex;

        internal ModuleMap(ModuleInfo[] modules)
        {
            Modules = modules;
            HandleToModuleIndex = new LowLevelDictionary<TypeManagerHandle, int>();
            for (int moduleIndex = 0; moduleIndex < Modules.Length; moduleIndex++)
            {
                HandleToModuleIndex.Add(Modules[moduleIndex].Handle, moduleIndex);
            }
        }
    }

    /// <summary>
    /// This enumerator iterates the module map, possibly adjusting the order to make a given
    /// module go first in the enumeration.
    /// </summary>
    public struct ModuleInfoEnumerator
    {
        /// <summary>
        /// Array of modules to enumerate.
        /// </summary>
        private readonly ModuleInfo[] _modules;

        /// <summary>
        /// Preferred module index in the array, -1 when none (in such case the array is enumerated
        /// in its natural order).
        /// </summary>
        private int _preferredIndex;

        /// <summary>
        /// Enumeration step index initially set to -1 (so that the first MoveNext increments it to 0).
        /// </summary>
        private int _iterationIndex;

        /// <summary>
        /// Current _modules element that should be returned by Current (updated in MoveNext).
        /// </summary>
        private ModuleInfo _currentModule;

        /// <summary>
        /// Initialize the module enumerator state machine and locate the preferred module index.
        /// </summary>
        /// <param name="moduleMap">Module map to enumerate</param>
        /// <param name="preferredModuleHandle">Optional module handle to enumerate first</param>
        internal ModuleInfoEnumerator(ModuleMap moduleMap, TypeManagerHandle preferredModuleHandle)
        {
            _modules = moduleMap.Modules;
            _preferredIndex = -1;
            _iterationIndex = -1;
            _currentModule = null;

            if (!preferredModuleHandle.IsNull &&
                !moduleMap.HandleToModuleIndex.TryGetValue(preferredModuleHandle, out _preferredIndex))
            {
                Environment.FailFast("Invalid module requested in enumeration: " + preferredModuleHandle.LowLevelToString());
            }
        }

        /// <summary>
        /// Move the enumerator state machine to the next element in the module map.
        /// </summary>
        /// <returns>true when [another] module is available, false when the enumeration is finished</returns>
        public bool MoveNext()
        {
            if (_iterationIndex + 1 >= _modules.Length)
            {
                _currentModule = null;
                return false;
            }

            _iterationIndex++;
            int moduleIndex = _iterationIndex;
            if (moduleIndex <= _preferredIndex)
            {
                // Transform the index so that the _preferredIndex is returned in first iteration
                moduleIndex = (moduleIndex == 0 ? _preferredIndex : moduleIndex - 1);
            }

            _currentModule = _modules[moduleIndex];

            return true;
        }

        /// <summary>
        /// Look up the "current" module corresponding to the previous call to MoveNext.
        /// </summary>
        public ModuleInfo Current
        {
            get
            {
                if (_currentModule == null)
                {
                    Environment.FailFast("Current module queried in wrong enumerator state");
                }
                return _currentModule;
            }
        }
    }

    /// <summary>
    /// Helper class that can construct an enumerator for the module info map, possibly adjusting
    /// the module order so that a given explicitly specified module goes first - this is used
    /// as optimization in cases where a certain module is most likely to contain some metadata.
    /// </summary>
    public struct NativeFormatModuleInfoEnumerable
    {
        /// <summary>
        /// Module map to enumerate
        /// </summary>
        private readonly ModuleMap _moduleMap;

        /// <summary>
        /// Module handle that should be enumerated first, default(IntPtr) when not used.
        /// </summary>
        private readonly TypeManagerHandle _preferredModuleHandle;

        /// <summary>
        /// Store module map and preferred module to pass to the enumerator upon construction.
        /// </summary>
        /// <param name="moduleMap">Module map to enumerate</param>
        /// <param name="preferredModuleHandle">Optional module handle to enumerate first</param>
        internal NativeFormatModuleInfoEnumerable(ModuleMap moduleMap, TypeManagerHandle preferredModuleHandle)
        {
            _moduleMap = moduleMap;
            _preferredModuleHandle = preferredModuleHandle;
        }

        /// <summary>
        /// Construct the actual module info enumerator.
        /// </summary>
        public NativeFormatModuleInfoEnumerator GetEnumerator()
        {
            return new NativeFormatModuleInfoEnumerator(_moduleMap, _preferredModuleHandle);
        }
    }

    /// <summary>
    /// This enumerator iterates the module map, possibly adjusting the order to make a given
    /// module go first in the enumeration.
    /// </summary>
    public struct NativeFormatModuleInfoEnumerator
    {
        /// <summary>
        /// Array of modules to enumerate.
        /// </summary>
        private readonly ModuleInfo[] _modules;

        /// <summary>
        /// Preferred module index in the array, -1 when none (in such case the array is enumerated
        /// in its natural order).
        /// </summary>
        private int _preferredIndex;

        /// <summary>
        /// Enumeration step index initially set to -1 (so that the first MoveNext increments it to 0).
        /// </summary>
        private int _iterationIndex;

        /// <summary>
        /// Current _modules element that should be returned by Current (updated in MoveNext).
        /// </summary>
        private NativeFormatModuleInfo _currentModule;

        /// <summary>
        /// Initialize the module enumerator state machine and locate the preferred module index.
        /// </summary>
        /// <param name="moduleMap">Module map to enumerate</param>
        /// <param name="preferredModuleHandle">Optional module handle to enumerate first</param>
        internal NativeFormatModuleInfoEnumerator(ModuleMap moduleMap, TypeManagerHandle preferredModuleHandle)
        {
            _modules = moduleMap.Modules;
            _preferredIndex = -1;
            _iterationIndex = -1;
            _currentModule = null;

            if (!preferredModuleHandle.IsNull &&
                !moduleMap.HandleToModuleIndex.TryGetValue(preferredModuleHandle, out _preferredIndex))
            {
                Environment.FailFast("Invalid module requested in enumeration: " + preferredModuleHandle.LowLevelToString());
            }
        }

        /// <summary>
        /// Move the enumerator state machine to the next element in the module map.
        /// </summary>
        /// <returns>true when [another] module is available, false when the enumeration is finished</returns>
        public bool MoveNext()
        {
            do
            {
                if (_iterationIndex + 1 >= _modules.Length)
                {
                    _currentModule = null;
                    return false;
                }

                _iterationIndex++;
                int moduleIndex = _iterationIndex;
                if (moduleIndex <= _preferredIndex)
                {
                    // Transform the index so that the _preferredIndex is returned in first iteration
                    moduleIndex = (moduleIndex == 0 ? _preferredIndex : moduleIndex - 1);
                }

                _currentModule = _modules[moduleIndex] as NativeFormatModuleInfo;
            } while (_currentModule == null);

            return true;
        }

        /// <summary>
        /// Look up the "current" module corresponding to the previous call to MoveNext.
        /// </summary>
        public NativeFormatModuleInfo Current
        {
            get
            {
                if (_currentModule == null)
                {
                    Environment.FailFast("Current module queried in wrong enumerator state");
                }
                return _currentModule;
            }
        }
    }

    /// <summary>
    /// Utilities for manipulating module list and metadata readers.
    /// </summary>
    public sealed class ModuleList
    {
        private ModuleMap _loadedModuleMap;

        /// <summary>
        /// Module list is a process-wide singleton that physically lives in the TypeLoaderEnvironment instance.
        /// </summary>
        public static ModuleList Instance { get; } = new ModuleList();

        /// <summary>
        /// Register initially (eagerly) loaded modules.
        /// </summary>
        internal ModuleList()
        {
            // Fetch modules that have already been registered with the runtime
            int loadedModuleCount = RuntimeAugments.GetLoadedModules(null);
            TypeManagerHandle[] loadedModuleHandles = new TypeManagerHandle[loadedModuleCount];
            int loadedModuleCountUpdated = RuntimeAugments.GetLoadedModules(loadedModuleHandles);
            Debug.Assert(loadedModuleCount == loadedModuleCountUpdated);

            ModuleInfo[] updatedModules = new ModuleInfo[loadedModuleHandles.Length];

            for (int newModuleIndex = 0; newModuleIndex < loadedModuleHandles.Length; newModuleIndex++)
            {
                ModuleInfo newModuleInfo;

                unsafe
                {
                    byte* pBlob;
                    uint cbBlob;

                    if (RuntimeAugments.FindBlob(loadedModuleHandles[newModuleIndex], (int)ReflectionMapBlob.EmbeddedMetadata, new IntPtr(&pBlob), new IntPtr(&cbBlob)))
                    {
                        newModuleInfo = new NativeFormatModuleInfo(loadedModuleHandles[newModuleIndex], (IntPtr)pBlob, (int)cbBlob);
                    }
                    else
                    {
                        newModuleInfo = new ModuleInfo(loadedModuleHandles[newModuleIndex]);
                    }
                }

                updatedModules[newModuleIndex] = newModuleInfo;
            }

            _loadedModuleMap = new ModuleMap(updatedModules);
        }

        /// <summary>
        /// Locate module info for a given module. Fail if not found or before the module registry
        /// gets initialized. Must only be called for modules described as native format (not the mrt module, or an ECMA module)
        /// </summary>
        /// <param name="moduleHandle">Handle of module to look up</param>
        public NativeFormatModuleInfo GetModuleInfoByHandle(TypeManagerHandle moduleHandle)
        {
            ModuleMap moduleMap = _loadedModuleMap;
            return (NativeFormatModuleInfo)moduleMap.Modules[moduleMap.HandleToModuleIndex[moduleHandle]];
        }

        /// <summary>
        /// Try to Locate module info for a given module. Returns false when not found.
        /// gets initialized.
        /// </summary>
        /// <param name="moduleHandle">Handle of module to look up</param>
        /// <param name="moduleInfo">Found module info</param>
        public bool TryGetModuleInfoByHandle(TypeManagerHandle moduleHandle, out ModuleInfo moduleInfo)
        {
            ModuleMap moduleMap = _loadedModuleMap;
            int moduleIndex;
            if (moduleMap.HandleToModuleIndex.TryGetValue(moduleHandle, out moduleIndex))
            {
                moduleInfo = moduleMap.Modules[moduleIndex];
                return true;
            }
            moduleInfo = null;
            return false;
        }

        /// <summary>
        /// Given module handle, locate the metadata reader. Return null when not found.
        /// </summary>
        /// <param name="moduleHandle">Handle of module to look up</param>
        /// <returns>Reader for the embedded metadata blob in the module, null when not found</returns>
        public MetadataReader GetMetadataReaderForModule(TypeManagerHandle moduleHandle)
        {
            ModuleMap moduleMap = _loadedModuleMap;
            int moduleIndex;
            if (moduleMap.HandleToModuleIndex.TryGetValue(moduleHandle, out moduleIndex))
            {
                NativeFormatModuleInfo moduleInfo = moduleMap.Modules[moduleIndex] as NativeFormatModuleInfo;
                if (moduleInfo != null)
                    return moduleInfo.MetadataReader;
                else
                    return null;
            }
            return null;
        }

        /// <summary>
        /// Locate the containing module for a given metadata reader. Assert when not found.
        /// </summary>
        /// <param name="reader">Metadata reader to look up</param>
        /// <returns>Module handle of the module containing the given reader</returns>
        public NativeFormatModuleInfo GetModuleInfoForMetadataReader(MetadataReader reader)
        {
            foreach (ModuleInfo moduleInfo in _loadedModuleMap.Modules)
            {
                NativeFormatModuleInfo nativeFormatModuleInfo = moduleInfo as NativeFormatModuleInfo;
                if (nativeFormatModuleInfo != null && nativeFormatModuleInfo.MetadataReader == reader)
                {
                    return nativeFormatModuleInfo;
                }
            }

            // We should never have a reader that is not associated with a module (where does it come from?!)
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Locate the containing module for a given metadata reader. Assert when not found.
        /// </summary>
        /// <param name="reader">Metadata reader to look up</param>
        /// <returns>Module handle of the module containing the given reader</returns>
        public TypeManagerHandle GetModuleForMetadataReader(MetadataReader reader)
        {
            foreach (ModuleInfo moduleInfo in _loadedModuleMap.Modules)
            {
                NativeFormatModuleInfo nativeFormatModuleInfo = moduleInfo as NativeFormatModuleInfo;
                if (nativeFormatModuleInfo != null && nativeFormatModuleInfo.MetadataReader == reader)
                {
                    return moduleInfo.Handle;
                }
            }

            // We should never have a reader that is not associated with a module (where does it come from?!)
            Debug.Assert(false);
            return default(TypeManagerHandle);
        }

        /// <summary>
        /// Enumerate modules.
        /// </summary>
        public static NativeFormatModuleInfoEnumerable EnumerateModules()
        {
            return new NativeFormatModuleInfoEnumerable(Instance._loadedModuleMap, default(TypeManagerHandle));
        }

        /// <summary>
        /// Enumerate modules. Specify a module that should be enumerated first
        /// - this is used as an optimization in cases when a certain binary module is more probable
        /// to contain a certain information.
        /// </summary>
        /// <param name="preferredModule">Handle to the module which should be enumerated first</param>
        public static NativeFormatModuleInfoEnumerable EnumerateModules(TypeManagerHandle preferredModule)
        {
            return new NativeFormatModuleInfoEnumerable(Instance._loadedModuleMap, preferredModule);
        }
    }

    public static partial class RuntimeSignatureHelper
    {
        public static ModuleInfo GetModuleInfo(this Internal.Runtime.CompilerServices.RuntimeSignature methodSignature)
        {
            if (methodSignature.IsNativeLayoutSignature)
            {
                return ModuleList.Instance.GetModuleInfoByHandle(new TypeManagerHandle(methodSignature.ModuleHandle));
            }
            else
            {
                ModuleInfo moduleInfo;
                bool success = ModuleList.Instance.TryGetModuleInfoByHandle(new TypeManagerHandle(methodSignature.ModuleHandle), out moduleInfo);
                Debug.Assert(success);
                return moduleInfo;
            }
        }
    }
}
