// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.


//
//
// Authors:
//      Marek Habersack <mhabersack@novell.com>
//
// Copyright (C) 2010 Novell, Inc. (http://novell.com/)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Caching;

namespace MonoTests.Common
{
    internal class PokerObjectCache : ObjectCache
    {
        private Dictionary<string, object> _cache;

        private Dictionary<string, object> Cache
        {
            get
            {
                if (_cache == null)
                    _cache = new Dictionary<string, object>();
                return _cache;
            }
        }

        public string MethodCalled { get; private set; }

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            MethodCalled = "AddOrGetExisting (string key, object value, CacheItemPolicy policy, string regionName = null)";
            if (string.IsNullOrEmpty(key) || value == null)
                return null;

            object item;
            if (Cache.TryGetValue(key, out item))
                return item;

            Cache.Add(key, value);
            return null;
        }

        public override CacheItem AddOrGetExisting(CacheItem value, CacheItemPolicy policy)
        {
            MethodCalled = "AddOrGetExisting (CacheItem value, CacheItemPolicy policy)";
            if (value == null)
                return null;

            object item;
            if (Cache.TryGetValue(value.Key, out item))
                return item as CacheItem;

            Cache.Add(value.Key, value);
            return null;
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            MethodCalled = "AddOrGetExisting (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)";
            if (string.IsNullOrEmpty(key) || value == null)
                return null;

            object item;
            if (Cache.TryGetValue(key, out item))
                return item;

            Cache.Add(key, value);
            return null;
        }

        public override bool Contains(string key, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get { throw new NotImplementedException(); }
        }

        public override object Get(string key, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override long GetCount(string regionName = null)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            MethodCalled = "IDictionary<string, object> GetValues (IEnumerable<string> keys, string regionName = null)";
            var ret = new Dictionary<string, object>();
            if (keys == null)
                return ret;

            Dictionary<string, object> cache = Cache;
            if (cache.Count == 0)
                return ret;

            object value;
            foreach (string key in keys)
            {
                if (!cache.TryGetValue(key, out value))
                    continue;

                ret.Add(key, value);
            }

            return ret;
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override object Remove(string key, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            throw new NotImplementedException();
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override object this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
