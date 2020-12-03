// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntryStack
    {
        private readonly CacheEntry _entry;

        private CacheEntryStack()
        {
        }

        private CacheEntryStack(CacheEntry entry)
        {
            _entry = entry;
        }

        public static CacheEntryStack Empty { get; } = new CacheEntryStack();

        public CacheEntryStack Push(CacheEntry c)
        {
            return new CacheEntryStack(c);
        }

        public CacheEntry Peek()
        {
            return _entry;
        }
    }
}
