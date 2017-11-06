// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Caching.Resources;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Caching
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "The class represents a type of cache")]
    public abstract class ObjectCache : IEnumerable<KeyValuePair<string, object>>
    {
        private static IServiceProvider s_host;

        public static readonly DateTimeOffset InfiniteAbsoluteExpiration = DateTimeOffset.MaxValue;
        public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;

        public static IServiceProvider Host
        {
            get
            {
                return s_host;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (Interlocked.CompareExchange(ref s_host, value, null) != null)
                {
                    throw new InvalidOperationException(SR.Property_already_set);
                }
            }
        }

        public abstract DefaultCacheCapabilities DefaultCacheCapabilities { get; }

        public abstract string Name { get; }

        //Default indexer property
        public abstract object this[string key] { get; set; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
        }

        public abstract CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<String> keys, String regionName = null);

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract IEnumerator<KeyValuePair<string, object>> GetEnumerator();

        //Existence check for a single item
        public abstract bool Contains(string key, string regionName = null);

        //The Add overloads are for adding an item without requiring the existing item to be returned.  This was requested for Velocity.
        public virtual bool Add(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            return (AddOrGetExisting(key, value, absoluteExpiration, regionName) == null);
        }

        public virtual bool Add(CacheItem item, CacheItemPolicy policy)
        {
            return (AddOrGetExisting(item, policy) == null);
        }

        public virtual bool Add(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            return (AddOrGetExisting(key, value, policy, regionName) == null);
        }

        public abstract object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null);
        public abstract CacheItem AddOrGetExisting(CacheItem value, CacheItemPolicy policy);

        public abstract object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "The name best represents an operation on a cache")]
        public abstract object Get(string key, string regionName = null);

        public abstract CacheItem GetCacheItem(string key, string regionName = null);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "The name best represents an operation on a cache")]
        public abstract void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "The name best represents an operation on a cache")]
        public abstract void Set(CacheItem item, CacheItemPolicy policy);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "The name best represents an operation on a cache")]
        public abstract void Set(string key, object value, CacheItemPolicy policy, string regionName = null);

        //Get multiple items by keys
        public abstract IDictionary<string, object> GetValues(IEnumerable<String> keys, string regionName = null);

        public virtual IDictionary<string, object> GetValues(string regionName, params string[] keys)
        {
            return GetValues(keys, regionName);
        }

        public abstract object Remove(string key, string regionName = null);

        public abstract long GetCount(string regionName = null);
    }
}
