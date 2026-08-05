// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Wasm
{
    /// <summary>
    /// A minimal <see cref="MetadataTypeSystemContext"/> configured the way crossgen2 configures itself
    /// for a wasm target, so field layout - and therefore struct size - agrees with what the compiler
    /// will encode into wasm ABI signatures.
    /// </summary>
    /// <remarks>
    /// Assemblies are resolved from an explicit list of file paths rather than from a probing path, so
    /// callers get an error instead of a silently different answer when a reference is missing.
    /// </remarks>
    public sealed class WasmTypeSystemContext : MetadataTypeSystemContext, IWasmTypeCacheContext
    {
        private readonly Dictionary<string, string> _assemblyPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ModuleDesc> _modules = new(StringComparer.OrdinalIgnoreCase);

        private readonly WasmMetadataFieldLayoutAlgorithm _metadataFieldLayout = new();
        private readonly VectorFieldLayoutAlgorithm _vectorFieldLayoutAlgorithm;
        private readonly VectorOfTFieldLayoutAlgorithm _vectorOfTFieldLayoutAlgorithm;
        private readonly Int128FieldLayoutAlgorithm _int128FieldLayoutAlgorithm;
        private readonly DecimalFieldLayoutAlgorithm _decimalFieldLayoutAlgorithm;
        private readonly TypeWithRepeatedFieldsFieldLayoutAlgorithm _typeWithRepeatedFieldsFieldLayoutAlgorithm;
        private readonly MetadataRuntimeInterfacesAlgorithm _metadataRuntimeInterfacesAlgorithm = new();
        private readonly VirtualMethodAlgorithm _virtualMethodAlgorithm = new MetadataVirtualMethodAlgorithm();
        private ArrayOfTRuntimeInterfacesAlgorithm _arrayOfTRuntimeInterfacesAlgorithm;

        private readonly object _structCacheLock = new object();
        private readonly Dictionary<int, TypeDesc> _structsBySize = new Dictionary<int, TypeDesc>();
        private volatile TypeDesc _cachedEmptyStruct;
        private volatile TypeDesc _wasmV128Type;

        public WasmTypeSystemContext(TargetOS targetOS)
            : base(new TargetDetails(TargetArchitecture.Wasm32, targetOS, TargetAbi.NativeAot, SimdVectorLength.Vector128Bit))
        {
            _vectorFieldLayoutAlgorithm = new VectorFieldLayoutAlgorithm(_metadataFieldLayout);
            _vectorOfTFieldLayoutAlgorithm = new VectorOfTFieldLayoutAlgorithm(_metadataFieldLayout, _vectorFieldLayoutAlgorithm, "Vector128`1"u8);
            _int128FieldLayoutAlgorithm = new Int128FieldLayoutAlgorithm(_metadataFieldLayout);
            _decimalFieldLayoutAlgorithm = new DecimalFieldLayoutAlgorithm(_metadataFieldLayout);
            _typeWithRepeatedFieldsFieldLayoutAlgorithm = new TypeWithRepeatedFieldsFieldLayoutAlgorithm(_metadataFieldLayout);
        }

        /// <summary>
        /// Registers an assembly file that <see cref="ResolveAssembly"/> may load. The last registration
        /// for a given simple name wins, matching how a compiler command line treats duplicate inputs.
        /// </summary>
        public void AddAssemblyPath(string path)
        {
            _assemblyPaths[Path.GetFileNameWithoutExtension(path)] = path;
        }

        public ModuleDesc GetModuleForSimpleName(string simpleName, bool throwIfNotFound = true)
        {
            if (_modules.TryGetValue(simpleName, out ModuleDesc existingModule))
                return existingModule;

            if (!_assemblyPaths.TryGetValue(simpleName, out string filePath))
            {
                if (throwIfNotFound)
                    throw new FileNotFoundException($"Assembly '{simpleName}' was not among the assemblies provided to the wasm signature resolver.");

                return null;
            }

            var peStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            ModuleDesc module = EcmaModule.Create(this, new PEReader(peStream), containingAssembly: null);
            _modules.Add(simpleName, module);
            return module;
        }

        public override ModuleDesc ResolveAssembly(AssemblyNameInfo name, bool throwIfNotFound)
        {
            return GetModuleForSimpleName(name.Name, throwIfNotFound);
        }

        public override FieldLayoutAlgorithm GetLayoutAlgorithmForType(DefType type)
        {
            if (type == UniversalCanonType)
                return UniversalCanonLayoutAlgorithm.Instance;

            if (VectorOfTFieldLayoutAlgorithm.IsVectorOfTType(type))
                return _vectorOfTFieldLayoutAlgorithm;

            if (VectorFieldLayoutAlgorithm.IsVectorType(type))
                return _vectorFieldLayoutAlgorithm;

            if (Int128FieldLayoutAlgorithm.IsIntegerType(type))
                return _int128FieldLayoutAlgorithm;

            if (DecimalFieldLayoutAlgorithm.IsDecimalFloatingPointType(type))
                return _decimalFieldLayoutAlgorithm;

            if (type is TypeWithRepeatedFields)
                return _typeWithRepeatedFieldsFieldLayoutAlgorithm;

            return _metadataFieldLayout;
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForNonPointerArrayType(ArrayType type)
        {
            _arrayOfTRuntimeInterfacesAlgorithm ??= new ArrayOfTRuntimeInterfacesAlgorithm(SystemModule.GetType("System"u8, "Array`1"u8));
            return _arrayOfTRuntimeInterfacesAlgorithm;
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForDefType(DefType type)
        {
            return _metadataRuntimeInterfacesAlgorithm;
        }

        public override VirtualMethodAlgorithm GetVirtualMethodAlgorithmForType(TypeDesc type)
        {
            return _virtualMethodAlgorithm;
        }

        // crossgen2 always runs with SharedGenericsMode.CanonicalReferenceTypes.
        protected internal override Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind, out bool changed)
            => RuntimeDeterminedCanonicalizationAlgorithm.ConvertInstantiationToCanonForm(instantiation, kind, out changed);

        protected internal override TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
            => RuntimeDeterminedCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, kind);

        protected internal override TypeDesc ConvertToCanon(TypeDesc typeToConvert, ref CanonicalFormKind kind)
            => RuntimeDeterminedCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, ref kind);

        public override bool SupportsUniversalCanon => false;
        public override bool SupportsCanon => true;

        public TypeDesc WasmV128Type
        {
            get
            {
                TypeDesc type = _wasmV128Type;
                if (type is null)
                {
                    var vector128 = (MetadataType)SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector128`1"u8);
                    _wasmV128Type = type = vector128.MakeInstantiatedType(GetWellKnownType(WellKnownType.Byte));
                }

                return type;
            }
        }

        public TypeDesc CachedEmptyStruct => _cachedEmptyStruct;

        public void CacheEmptyStruct(TypeDesc type)
        {
            _cachedEmptyStruct ??= type;
        }

        public void CacheStructBySize(TypeDesc type)
        {
            int size = type.GetElementSize().AsInt;
            if (size <= 0)
                return;

            lock (_structCacheLock)
            {
                _structsBySize.TryAdd(size, type);
            }
        }

        public TypeDesc GetCachedStructOfSize(int size)
        {
            lock (_structCacheLock)
            {
                if (_structsBySize.TryGetValue(size, out TypeDesc result))
                    return result;
            }

            return null;
        }
    }
}
