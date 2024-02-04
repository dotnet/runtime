// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Diagnostics;
using System.IO;

using ILCompiler;
using System.Runtime.CompilerServices;

namespace Internal.TypeSystem.Ecma
{
    public partial class MutableModule : ModuleDesc, IEcmaModule
    {
        private class ManagedBinaryEmitterForInternalUse : TypeSystemMetadataEmitter
        {
            Dictionary<ModuleDesc, EntityHandle> _moduleRefs = new Dictionary<ModuleDesc, EntityHandle>();
            List<string> _moduleRefStrings = new List<string>();
            readonly Func<ModuleDesc, int> _moduleToIndex;

            MutableModule _mutableModule;

            protected override EntityHandle GetNonNestedResolutionScope(MetadataType metadataType)
            {
                var module = metadataType.Module;

                EntityHandle result;
                if (_moduleRefs.TryGetValue(module, out result))
                {
                    return result;
                }

                string moduleRefString;
                if (!_mutableModule._moduleToModuleRefString.TryGetValue(module, out moduleRefString))
                {
                    Debug.Assert(_mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences != null &&
                        _mutableModule._compilationGroup.CrossModuleInlineableModule(_mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences));

                    if (module == _typeSystemContext.SystemModule)
                    {
                        moduleRefString = "System.Private.CoreLib";
                    }
                    else
                    {
                        if (_mutableModule._compilationGroup.CrossModuleInlineableModule(module) || _mutableModule._compilationGroup.VersionsWithModule(module))
                        {
                            // References to modules that are explicitly permitted are done via ModuleIndex
                            int index = _moduleToIndex(module);
                            Debug.Assert(index != -1);
                            moduleRefString = $"#:{index.ToStringInvariant()}";
                        }
                        else
                        {
                            // Further dependencies are handled by specifying a module which has a further assembly dependency on the correct module
                            string asmReferenceName = GetNameOfAssemblyRefWhichResolvesToType(_mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences, metadataType);
                            int index = _moduleToIndex(_mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences);
                            Debug.Assert(index != -1);
                            moduleRefString = $"#{asmReferenceName}:{index.ToStringInvariant()}";
                        }
                    }

                    _mutableModule._moduleToModuleRefString.Add(module, moduleRefString);
                }

                _moduleRefStrings.Add(moduleRefString);
                result = MetadataTokens.ModuleReferenceHandle(_moduleRefStrings.Count);
                result = Builder.AddModuleReference(Builder.GetOrAddString(moduleRefString));
                _moduleRefs.Add(module, result);
                return result;
            }

            public ManagedBinaryEmitterForInternalUse(AssemblyName assemblyName,
                                                      TypeSystemContext typeSystemContext,
                                                      AssemblyFlags assemblyFlags,
                                                      byte[] publicKeyArray,
                                                      AssemblyHashAlgorithm hashAlgorithm,
                                                      Func<ModuleDesc, int> moduleToIndex,
                                                      MutableModule mutableModule)
                : base(assemblyName, typeSystemContext, assemblyFlags, publicKeyArray)
            {
                _moduleToIndex = moduleToIndex;
                _mutableModule = mutableModule;
            }

            static ConditionalWeakTable<ModuleDesc, Dictionary<MetadataType, string>> s_assemblyNameFromTypeLookups = new ConditionalWeakTable<ModuleDesc, Dictionary<MetadataType, string>>();

            static Dictionary<MetadataType, string> ComputeTypeLookupTable(ModuleDesc module)
            {
                Dictionary<MetadataType, string> result = new Dictionary<MetadataType, string>();
                if (!(module is EcmaModule ecmaModule))
                {
                    return result;
                }

                foreach (var typeRefHandle in ecmaModule.MetadataReader.TypeReferences)
                {
                    try
                    {
                        MetadataType typeFromTypeRef = ecmaModule.GetType(typeRefHandle) as MetadataType;
                        if (typeFromTypeRef == null)
                            continue;
                        if (!result.ContainsKey(typeFromTypeRef))
                        {
                            var reader = ecmaModule.MetadataReader;
                            var resolutionScope = reader.GetTypeReference(typeRefHandle).ResolutionScope;
                            if (resolutionScope.Kind == HandleKind.AssemblyReference)
                            {
                                var assemblyName = reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)resolutionScope).Name);

                                result.Add(typeFromTypeRef, assemblyName);
                            }
                        }
                    }
                    catch (TypeSystemException) { }

                }
                return result;
            }

            static string GetNameOfAssemblyRefWhichResolvesToType(ModuleDesc module, MetadataType type)
            {
                if (!s_assemblyNameFromTypeLookups.TryGetValue(module, out var lookupTable))
                {
                    lookupTable = ComputeTypeLookupTable(module);
                    s_assemblyNameFromTypeLookups.AddOrUpdate(module, lookupTable);
                }

                return lookupTable[type];
            }
        }

        class Cache
        {
            private List<ValueTuple<int, object>> _values = new List<ValueTuple<int, object>>();
            private List<PerMetadataFormCache> _perMetadata = new List<PerMetadataFormCache>();
            TypeSystemMetadataEmitter _currentBinaryEmitter;
            MetadataReader _reader;
            MutableModule _module;
            List<byte[]> _readers = new List<byte[]> (); // For now, as we don't maintain knowledge of how long these live, keep them around forever
            public Dictionary<int, object> Entities = new Dictionary<int, object>();
            public Dictionary<object, int> ExistingEntities = new Dictionary<object, int>();
            string _assemblyName;
            AssemblyFlags _assemblyFlags;
            byte[] _publicKeyArray;
            Version _version;
            AssemblyHashAlgorithm _hashAlgorithm;
            Func<ModuleDesc, int> _moduleToIndex;

            public Cache(MutableModule module, string assemblyName, AssemblyFlags assemblyFlags, byte[] publicKeyArray, Version version, AssemblyHashAlgorithm hashAlgorithm, Func<ModuleDesc, int> moduleToIndex)
            {
                _module = module;
                _assemblyName = assemblyName;
                _assemblyFlags = assemblyFlags;
                _publicKeyArray = publicKeyArray;
                _version = version;
                _hashAlgorithm = hashAlgorithm;
                _moduleToIndex = moduleToIndex;
                ResetEmitter();
            }

            private void ResetEmitter()
            {
                _reader = null;
                AssemblyName assemblyName = new AssemblyName();
                assemblyName.Name = _assemblyName;
                assemblyName.Version = _version;

                _currentBinaryEmitter = new ManagedBinaryEmitterForInternalUse(assemblyName, _module.Context, _assemblyFlags, _publicKeyArray, _hashAlgorithm, _moduleToIndex, _module);
                foreach (var entry in _values)
                {
                    var perMetadata = _perMetadata[entry.Item1];
                    var handle = perMetadata.HandleGenerationFunction(_currentBinaryEmitter, entry.Item2);
                    Debug.Assert(handle == ExistingEntities[entry.Item2]);
                }
            }

            class PerMetadataFormCache
            {
                public Func<TypeSystemMetadataEmitter, object, int> HandleGenerationFunction;
                public MutableModule _mutableModule;
                public int _cacheIndex;
            }

            public Func<T, int?> CreateCacheFunc<T>(Func<TypeSystemMetadataEmitter, object, int> handleFunc)
            {
                var perMetadataTypeCache = new PerMetadataFormCache<T>(_module, handleFunc, _perMetadata.Count);
                _perMetadata.Add(perMetadataTypeCache);
                return perMetadataTypeCache.TryGet;
            }

            public MetadataReader Reader
            {
                get
                {
                    lock (this)
                    {
                        if (_reader != null)
                            return _reader;

                        foreach (var item in _currentBinaryEmitter.TypeSystemEntitiesKnown)
                        {
                            if (!Entities.ContainsKey(MetadataTokens.GetToken(item.Value)))
                            {
                                Entities.Add(MetadataTokens.GetToken(item.Value), item.Key);
                                ExistingEntities.Add(item.Key, MetadataTokens.GetToken(item.Value));
                            }
                        }

                        byte[] metadataArrayTemp = _currentBinaryEmitter.EmitToMetadataBlob();
                        byte[] metadataArray = GC.AllocateArray<byte>(metadataArrayTemp.Length, pinned: true);
                        System.Runtime.InteropServices.GCHandle.Alloc(metadataArray, System.Runtime.InteropServices.GCHandleType.Pinned);
                        Array.Copy(metadataArrayTemp, metadataArray, metadataArray.Length);
                        _readers.Add(metadataArray);
                        unsafe
                        {
                            fixed (byte* pb = metadataArray)
                            {
                                _reader = new MetadataReader(pb, metadataArray.Length);
                            }
                        }
                        return _reader;
                    }
                }
            }

            public byte[] MetadataBlob
            {
                get
                {
                    lock(this)
                    {
                        // Ensure the latest metadata blob is up to date which will have the side-effect of ensuring that the metadata blob is accessible
                        var reader = Reader;
                        return _readers[_readers.Count - 1];
                    }
                }
            }

            class PerMetadataFormCache<T> : PerMetadataFormCache
            {
                public PerMetadataFormCache(MutableModule module, Func<TypeSystemMetadataEmitter, object, int> handleFunc, int cacheIndex)
                {
                    _cacheIndex = cacheIndex;
                    _mutableModule = module;
                    HandleGenerationFunction = handleFunc;
                }

                public int? TryGet(T value)
                {
                    lock (_mutableModule._cache)
                    {
                        try
                        {
                            int result;
                            if (_mutableModule._cache.ExistingEntities.TryGetValue(value, out result))
                            {
                                return result;
                            }

                            if (_mutableModule._cache._reader != null)
                            {
                                _mutableModule._cache.ResetEmitter();
                            }

                            if (_mutableModule.DisableNewTokens)
                                throw new DisableNewTokensException();

                            var handle = HandleGenerationFunction(_mutableModule._cache._currentBinaryEmitter, value);
                            _mutableModule._cache.ExistingEntities.Add(value, handle);
                            _mutableModule._cache.Entities.Add(handle, value);
                            _mutableModule._cache._values.Add((_cacheIndex, value));
                            return handle;
                        }
                        catch (NotImplementedException)
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public MutableModule(TypeSystemContext context,
                             string assemblyName,
                             AssemblyFlags assemblyFlags,
                             byte[] publicKeyArray,
                             Version version,
                             AssemblyHashAlgorithm hashAlgorithm,
                             Func<ModuleDesc, int> moduleToIndex,
                             ReadyToRunCompilationModuleGroupBase compilationGroup) : base(context, null)
        {
            _compilationGroup = compilationGroup;
            _cache = new Cache(this, assemblyName, assemblyFlags, publicKeyArray, version, hashAlgorithm, moduleToIndex);
            TryGetHandle = _cache.CreateCacheFunc<TypeSystemEntity>(GetHandleForTypeSystemEntity);
            TryGetStringHandle = _cache.CreateCacheFunc<string>(GetUserStringHandle);
            TryGetAssemblyRefHandle = _cache.CreateCacheFunc<AssemblyName>(GetAssemblyRefHandle);
        }

        class DisableNewTokensException : Exception { }

        public bool DisableNewTokens;
        public ModuleDesc ModuleThatIsCurrentlyTheSourceOfNewReferences;
        private ReadyToRunCompilationModuleGroupBase _compilationGroup;
        private Dictionary<ModuleDesc, string> _moduleToModuleRefString = new Dictionary<ModuleDesc, string>();

        private int GetHandleForTypeSystemEntity(TypeSystemMetadataEmitter emitter, object type)
        {
            return MetadataTokens.GetToken(emitter.EmitMetadataHandleForTypeSystemEntity((TypeSystemEntity)type));
        }

        private int GetUserStringHandle(TypeSystemMetadataEmitter emitter, object str)
        {
            return MetadataTokens.GetToken(emitter.GetUserStringHandle((string)str));
        }

        private int GetAssemblyRefHandle(TypeSystemMetadataEmitter emitter, object name)
        {
            return MetadataTokens.GetToken(emitter.GetAssemblyRef((AssemblyName)name));
        }

        public Func<TypeSystemEntity, int?> TryGetHandle { get; }
        public Func<string, int?> TryGetStringHandle { get; }
        public Func<AssemblyName, int?> TryGetAssemblyRefHandle { get; }
        public EntityHandle? TryGetEntityHandle(TypeSystemEntity tse)
        {
            var handle = TryGetHandle(tse);
            if (handle.HasValue)
                return MetadataTokens.EntityHandle(handle.Value);
            else
                return null;
        }
        public EntityHandle? TryGetExistingEntityHandle(TypeSystemEntity tse)
        {
            lock (_cache)
            {
                if (_cache.ExistingEntities.TryGetValue(tse, out var handle))
                    return MetadataTokens.EntityHandle(handle);
                var blob = _cache.MetadataBlob;
                if (_cache.ExistingEntities.TryGetValue(tse, out handle))
                    return MetadataTokens.EntityHandle(handle);
            }

            return null;
        }

        Cache _cache;

        public MetadataReader MetadataReader => _cache.Reader;
        public byte[] MetadataBlob => _cache.MetadataBlob;

        public int ModuleTypeSort => 1;

        public int CompareTo(IEcmaModule other)
        {
            if (other == this)
                return 0;

            if (other is MutableModule mutableModule)
            {
                return CompareTo(mutableModule);
            }

            return ModuleTypeSort.CompareTo(other.ModuleTypeSort);
        }

        public override IEnumerable<MetadataType> GetAllTypes() => Array.Empty<MetadataType>();
        public override MetadataType GetGlobalModuleType() => null;

        public object GetObject(EntityHandle handle, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw)
        {
            lock(_cache)
            {
                if (_cache.Entities.TryGetValue(MetadataTokens.GetToken(handle), out var result))
                {
                    return result;
                }

                var reader = MetadataReader;

                if (_cache.Entities.TryGetValue(MetadataTokens.GetToken(handle), out result))
                {
                    return result;
                }
            }

            throw new ArgumentException($"Invalid EntityHandle {MetadataTokens.GetToken(handle):X}  passed to MutableModule.GetObject");
        }

        public string GetUserString(UserStringHandle handle)
        {
            lock(_cache)
            {
                if (_cache.Entities.TryGetValue(MetadataTokens.GetToken(handle), out var result))
                {
                    return (string)result;
                }
            }
            throw new ArgumentException("Invalid UserStringHandle passed to MutableModule.GetObject");
        }
        public override object GetType(string nameSpace, string name, NotFoundBehavior notFoundBehavior) => throw new NotImplementedException();
        public TypeDesc GetType(EntityHandle handle)
        {
            TypeDesc type = GetObject(handle, NotFoundBehavior.Throw) as TypeDesc;
            if (type == null)
                ThrowHelper.ThrowBadImageFormatException();
            return type;
        }
    }
}
