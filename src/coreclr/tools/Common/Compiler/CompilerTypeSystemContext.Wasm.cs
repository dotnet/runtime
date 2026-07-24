// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private readonly object _structCacheLock = new object();
        private readonly Dictionary<int, TypeDesc> _structsBySize = new Dictionary<int, TypeDesc>();
        private volatile TypeDesc _cachedEmptyStruct;
        private volatile TypeDesc _cachedV128Type;

        /// <summary>
        /// Gets the first SIMD v128 type encountered during lowering, or null if none has been seen.
        /// Used by RaiseSignature to produce a roundtrippable type for the 'V' encoding. Any v128
        /// type is usable there because all of them share the same wasm ABI (see CacheV128Type).
        /// </summary>
        public TypeDesc CachedV128Type => _cachedV128Type;

        /// <summary>
        /// Caches a SIMD v128 type discovered during lowering. Only the first one is retained:
        /// every type that lowers to a wasm <c>v128</c> is 16 bytes with 16-byte alignment, so they
        /// are interchangeable for the signature round-trip that RaiseSignature performs. The assert
        /// guards that invariant, since a v128 type with a smaller alignment would silently give
        /// raised signatures a different argument layout depending on which type was lowered first.
        /// </summary>
        public void CacheV128Type(TypeDesc type)
        {
            Debug.Assert(((DefType)type).InstanceFieldAlignment.AsInt == 16,
                $"v128 type {type} must be 16-byte aligned to be interchangeable in raised signatures");

            _cachedV128Type ??= type;
        }

        /// <summary>
        /// Gets the first empty struct type encountered during lowering, or null if none has been seen.
        /// Used by RaiseSignature to produce a roundtrippable type for the 'e' encoding.
        /// </summary>
        public TypeDesc CachedEmptyStruct => _cachedEmptyStruct;

        /// <summary>
        /// Caches an empty struct type discovered during lowering. Only the first one is retained.
        /// </summary>
        public void CacheEmptyStruct(TypeDesc type)
        {
            _cachedEmptyStruct ??= type;
        }

        /// <summary>
        /// Caches a struct type by its element size, so RaiseSignature can retrieve a real
        /// type of that size. Only the first struct encountered for a given size is retained.
        /// </summary>
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

        /// <summary>
        /// Gets a previously cached struct type of the specified byte size.
        /// Returns null if no struct of that size has been cached.
        /// Used by RaiseSignature to produce a roundtrippable type for the 'S&lt;N&gt;' encoding.
        /// </summary>
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
