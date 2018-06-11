// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Caching
{
    public class CacheEntryRemovedArguments
    {
        private CacheItem _cacheItem;
        private ObjectCache _source;
        private CacheEntryRemovedReason _reason;

        public CacheItem CacheItem
        {
            get { return _cacheItem; }
        }

        public CacheEntryRemovedReason RemovedReason
        {
            get { return _reason; }
        }

        public ObjectCache Source
        {
            get { return _source; }
        }

        public CacheEntryRemovedArguments(ObjectCache source, CacheEntryRemovedReason reason, CacheItem cacheItem)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (cacheItem == null)
            {
                throw new ArgumentNullException(nameof(cacheItem));
            }
            _source = source;
            _reason = reason;
            _cacheItem = cacheItem;
        }
    }
}
