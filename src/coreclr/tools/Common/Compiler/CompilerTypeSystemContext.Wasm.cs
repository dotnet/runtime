// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private readonly object _structCacheLock = new object();
        private readonly Dictionary<int, TypeDesc> _structsBySize = new Dictionary<int, TypeDesc>();
        private volatile TypeDesc _cachedEmptyStruct;

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
