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
using System.Collections.Specialized;
using System.Runtime.Caching;
using System.Text;

namespace MonoTests.Common
{
    internal class PokerMemoryCache : MemoryCache
    {
        private List<string> _calls;

        public List<string> Calls
        {
            get
            {
                if (_calls == null)
                    _calls = new List<string>();
                return _calls;
            }
        }

        public override object this[string key]
        {
            get
            {
                Calls.Add("get_this [string key]");
                return base[key];
            }
            set
            {
                Calls.Add("set_this [string key]");
                base[key] = value;
            }
        }

        public PokerMemoryCache(string name, NameValueCollection config = null)
            : base(name, config)
        { }

        public override CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy)
        {
            Calls.Add("AddOrGetExisting (CacheItem item, CacheItemPolicy policy)");
            return base.AddOrGetExisting(item, policy);
        }

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            Calls.Add("AddOrGetExisting (string key, object value, CacheItemPolicy policy, string regionName = null)");
            return base.AddOrGetExisting(key, value, policy, regionName);
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            Calls.Add("AddOrGetExisting (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)");
            return base.AddOrGetExisting(key, value, absoluteExpiration, regionName);
        }

        public override object Get(string key, string regionName = null)
        {
            Calls.Add("Get (string key, string regionName = null)");
            return base.Get(key, regionName);
        }

        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            Calls.Add("GetCacheItem (string key, string regionName = null)");
            return base.GetCacheItem(key, regionName);
        }

        public override long GetCount(string regionName = null)
        {
            Calls.Add("GetCount (string regionName = null)");
            return base.GetCount(regionName);
        }

        public override bool Contains(string key, string regionName = null)
        {
            Calls.Add("Contains (string key, string regionName = null)");
            return base.Contains(key, regionName);
        }

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            Calls.Add("CreateCacheEntryChangeMonitor (IEnumerable<string> keys, string regionName = null)");
            return base.CreateCacheEntryChangeMonitor(keys, regionName);
        }

        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            Calls.Add("IEnumerator<KeyValuePair<string, object>> GetEnumerator ()");
            return base.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, object>> DoGetEnumerator()
        {
            return GetEnumerator();
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            Calls.Add("IDictionary<string, object> GetValues (IEnumerable<string> keys, string regionName = null)");
            return base.GetValues(keys, regionName);
        }

        public override IDictionary<string, object> GetValues(string regionName, params string[] keys)
        {
            Calls.Add("IDictionary<string, object> GetValues (string regionName, params string [] keys)");
            return base.GetValues(regionName, keys);
        }

        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            Calls.Add("Set (CacheItem item, CacheItemPolicy policy)");
            base.Set(item, policy);
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            Calls.Add("Set (string key, object value, CacheItemPolicy policy, string regionName = null)");
            base.Set(key, value, policy, regionName);
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            Calls.Add("Set (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)");
            base.Set(key, value, absoluteExpiration, regionName);
        }
    }
}
