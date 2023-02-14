// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementations of the COM strategy interfaces defined in Com.cs that we would want to ship (can be internal only if we don't want to allow users to provide their own implementations in v1).
using System.Collections.Generic;

namespace System.Runtime.InteropServices.Marshalling
{
    internal sealed unsafe class DefaultCaching : IIUnknownCacheStrategy
    {
        // [TODO] Implement some smart/thread-safe caching
        private readonly Dictionary<RuntimeTypeHandle, IIUnknownCacheStrategy.TableInfo> _cache = new();

        IIUnknownCacheStrategy.TableInfo IIUnknownCacheStrategy.ConstructTableInfo(RuntimeTypeHandle handle, IUnknownDerivedDetails details, void* ptr)
        {
            var obj = (void***)ptr;
            return new IIUnknownCacheStrategy.TableInfo()
            {
                ThisPtr = obj,
                Table = *obj,
                ManagedType = details.Implementation.TypeHandle
            };
        }

        bool IIUnknownCacheStrategy.TryGetTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo info)
        {
            return _cache.TryGetValue(handle, out info);
        }

        bool IIUnknownCacheStrategy.TrySetTableInfo(RuntimeTypeHandle handle, IIUnknownCacheStrategy.TableInfo info)
        {
            return _cache.TryAdd(handle, info);
        }

        void IIUnknownCacheStrategy.Clear(IIUnknownStrategy unknownStrategy)
        {
            foreach (var info in _cache.Values)
            {
                _ = unknownStrategy.Release(info.ThisPtr);
            }
            _cache.Clear();
        }
    }
}
