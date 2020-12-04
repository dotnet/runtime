// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntryHelper
    {
        private static readonly AsyncLocal<CacheEntryStack> _scopes = new AsyncLocal<CacheEntryStack>();

        internal static CacheEntryStack Scopes
        {
            get { return _scopes.Value; }
            set { _scopes.Value = value; }
        }

        internal static CacheEntry Current
        {
            get
            {
                CacheEntryStack scopes = GetOrCreateScopes();
                return scopes.Peek();
            }
        }

        internal static IDisposable EnterScope(CacheEntry entry)
        {
            CacheEntryStack scopes = GetOrCreateScopes();

            var scopeLease = new ScopeLease(scopes);
            Scopes = scopes.Push(entry);

            return scopeLease;
        }

        private static CacheEntryStack GetOrCreateScopes()
        {
            CacheEntryStack scopes = Scopes;
            if (scopes == null)
            {
                scopes = CacheEntryStack.Empty;
                Scopes = scopes;
            }

            return scopes;
        }

        private sealed class ScopeLease : IDisposable
        {
            private readonly CacheEntryStack _cacheEntryStack;

            public ScopeLease(CacheEntryStack cacheEntryStack)
            {
                _cacheEntryStack = cacheEntryStack;
            }

            public void Dispose()
            {
                Scopes = _cacheEntryStack;
            }
        }
    }
}
