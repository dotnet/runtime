// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.Caching
{
    public class CacheEntryUpdateArguments
    {
        private readonly string _key;
        private readonly CacheEntryRemovedReason _reason;
        private readonly string _regionName;
        private readonly ObjectCache _source;
        private CacheItem _updatedCacheItem;
        private CacheItemPolicy _updatedCacheItemPolicy;

        public string Key
        {
            get { return _key; }
        }

        public CacheEntryRemovedReason RemovedReason
        {
            get { return _reason; }
        }

        public string RegionName
        {
            get { return _regionName; }
        }

        public ObjectCache Source
        {
            get { return _source; }
        }

        public CacheItem UpdatedCacheItem
        {
            get { return _updatedCacheItem; }
            set { _updatedCacheItem = value; }
        }

        public CacheItemPolicy UpdatedCacheItemPolicy
        {
            get { return _updatedCacheItemPolicy; }
            set { _updatedCacheItemPolicy = value; }
        }

        public CacheEntryUpdateArguments(ObjectCache source, CacheEntryRemovedReason reason, string key, string regionName)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _source = source;
            _reason = reason;
            _key = key;
            _regionName = regionName;
        }
    }
}
