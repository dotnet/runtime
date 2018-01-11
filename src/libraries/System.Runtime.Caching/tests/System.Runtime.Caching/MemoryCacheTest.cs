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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MonoTests.Common;

namespace MonoTests.System.Runtime.Caching
{
    public class MemoryCacheTest
    {
        [Fact]
        public void ConstructorParameters()
        {
            MemoryCache mc;
            Assert.Throws<ArgumentNullException>(() =>
            {
                mc = new MemoryCache(null);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache(String.Empty);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("default");
            });

            var config = new NameValueCollection();
            config.Add("CacheMemoryLimitMegabytes", "invalid");
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", "invalid");
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("PollingInterval", "invalid");
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("CacheMemoryLimitMegabytes", "-1");
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("CacheMemoryLimitMegabytes", UInt64.MaxValue.ToString());
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", "-1");
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", UInt64.MaxValue.ToString());
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", UInt32.MaxValue.ToString());
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", "-10");
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", "0");
            // Just make sure it doesn't throw any exception
            mc = new MemoryCache("MyCache", config);

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", "101");
            Assert.Throws<ArgumentException>(() =>
            {
                mc = new MemoryCache("MyCache", config);
            });

            // Just make sure it doesn't throw any exception
            config.Clear();
            config.Add("UnsupportedSetting", "123");
            mc = new MemoryCache("MyCache", config);
        }

        [Fact]
        public void Defaults()
        {
            var mc = new MemoryCache("MyCache");
            Assert.Equal("MyCache", mc.Name);
            Assert.Equal(TimeSpan.FromMinutes(2), mc.PollingInterval);
            Assert.Equal(
                DefaultCacheCapabilities.InMemoryProvider |
                DefaultCacheCapabilities.CacheEntryChangeMonitors |
                DefaultCacheCapabilities.AbsoluteExpirations |
                DefaultCacheCapabilities.SlidingExpirations |
                DefaultCacheCapabilities.CacheEntryRemovedCallback |
                DefaultCacheCapabilities.CacheEntryUpdateCallback,
                mc.DefaultCacheCapabilities);
        }

        [Fact]
        public void DefaultInstanceDefaults()
        {
            var mc = MemoryCache.Default;
            Assert.Equal("Default", mc.Name);
            Assert.Equal(TimeSpan.FromMinutes(2), mc.PollingInterval);
            Assert.Equal(
                DefaultCacheCapabilities.InMemoryProvider |
                DefaultCacheCapabilities.CacheEntryChangeMonitors |
                DefaultCacheCapabilities.AbsoluteExpirations |
                DefaultCacheCapabilities.SlidingExpirations |
                DefaultCacheCapabilities.CacheEntryRemovedCallback |
                DefaultCacheCapabilities.CacheEntryUpdateCallback,
                mc.DefaultCacheCapabilities);
        }

        [Fact]
        public void ConstructorValues()
        {
            var config = new NameValueCollection();
            config.Add("CacheMemoryLimitMegabytes", "1");
            config.Add("pollingInterval", "00:10:00");

            var mc = new MemoryCache("MyCache", config);
            Assert.Equal(1048576, mc.CacheMemoryLimit);
            Assert.Equal(TimeSpan.FromMinutes(10), mc.PollingInterval);

            config.Clear();
            config.Add("PhysicalMemoryLimitPercentage", "10");
            config.Add("CacheMemoryLimitMegabytes", "5");
            config.Add("PollingInterval", "01:10:00");

            mc = new MemoryCache("MyCache", config);
            Assert.Equal(10, mc.PhysicalMemoryLimit);
            Assert.Equal(5242880, mc.CacheMemoryLimit);
            Assert.Equal(TimeSpan.FromMinutes(70), mc.PollingInterval);
        }

        [Fact]
        public void Indexer()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc[null] = "value";
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                object v = mc[null];
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc["key"] = null;
            });

            mc.Calls.Clear();
            mc["key"] = "value";
            Assert.Equal(3, mc.Calls.Count);
            Assert.Equal("set_this [string key]", mc.Calls[0]);
            Assert.Equal("Set (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)", mc.Calls[1]);
            Assert.Equal("Set (string key, object value, CacheItemPolicy policy, string regionName = null)", mc.Calls[2]);
            Assert.True(mc.Contains("key"));

            mc.Calls.Clear();
            object value = mc["key"];
            Assert.Equal(1, mc.Calls.Count);
            Assert.Equal("get_this [string key]", mc.Calls[0]);
            Assert.Equal("value", value);
        }

        [Fact]
        public void Contains()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Contains(null);
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.Contains("key", "region");
            });

            mc.Set("key", "value", ObjectCache.InfiniteAbsoluteExpiration);
            Assert.True(mc.Contains("key"));

            var cip = new CacheItemPolicy();
            cip.Priority = CacheItemPriority.NotRemovable;
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(50);
            mc.Set("key", "value", cip);
            Assert.True(mc.Contains("key"));

            // wait past cip.AbsoluteExpiration
            Thread.Sleep(500);

            // Attempt to retrieve an expired entry
            Assert.False(mc.Contains("key"));
        }

        [Fact]
        public void CreateCacheEntryChangeMonitor()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.CreateCacheEntryChangeMonitor(new string[] { "key" }, "region");
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.CreateCacheEntryChangeMonitor(null);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                mc.CreateCacheEntryChangeMonitor(new string[] { });
            });

            Assert.Throws<ArgumentException>(() =>
            {
                mc.CreateCacheEntryChangeMonitor(new string[] { "key", null });
            });

            mc.Set("key1", "value1", ObjectCache.InfiniteAbsoluteExpiration);
            mc.Set("key2", "value2", ObjectCache.InfiniteAbsoluteExpiration);
            mc.Set("key3", "value3", ObjectCache.InfiniteAbsoluteExpiration);

            CacheEntryChangeMonitor monitor = mc.CreateCacheEntryChangeMonitor(new string[] { "key1", "key2" });
            Assert.NotNull(monitor);
            Assert.Equal("System.Runtime.Caching.MemoryCacheEntryChangeMonitor", monitor.GetType().ToString());
            Assert.Equal(2, monitor.CacheKeys.Count);
            Assert.Equal("key1", monitor.CacheKeys[0]);
            Assert.Equal("key2", monitor.CacheKeys[1]);
            Assert.Null(monitor.RegionName);
            Assert.False(monitor.HasChanged);

            // The actual unique id is constructed from key names followed by the hex value of ticks of their last modifed time
            Assert.False(String.IsNullOrEmpty(monitor.UniqueId));

			monitor = mc.CreateCacheEntryChangeMonitor (new string [] { "key1", "doesnotexist" });
			Assert.NotNull (monitor);
			Assert.Equal ("System.Runtime.Caching.MemoryCacheEntryChangeMonitor", monitor.GetType ().ToString ());
			Assert.Equal (2, monitor.CacheKeys.Count);
			Assert.Equal ("key1", monitor.CacheKeys [0]);
			Assert.Null (monitor.RegionName);
			Assert.True (monitor.HasChanged);
        }

        [Fact]
        public void AddOrGetExisting_String_Object_DateTimeOffset_String()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.AddOrGetExisting(null, "value", DateTimeOffset.Now);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.AddOrGetExisting("key", null, DateTimeOffset.Now);
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.AddOrGetExisting("key", "value", DateTimeOffset.Now, "region");
            });

            object value = mc.AddOrGetExisting("key3_A2-1", "value", DateTimeOffset.Now.AddMinutes(1));
            Assert.True(mc.Contains("key3_A2-1"));
            Assert.Null(value);

            mc.Calls.Clear();
            value = mc.AddOrGetExisting("key3_A2-1", "value2", DateTimeOffset.Now.AddMinutes(1));
            Assert.True(mc.Contains("key3_A2-1"));
            Assert.NotNull(value);
            Assert.Equal("value", value);
            Assert.Equal(2, mc.Calls.Count);
            Assert.Equal("AddOrGetExisting (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)", mc.Calls[0]);

            value = mc.AddOrGetExisting("key_expired", "value", DateTimeOffset.MinValue);
            Assert.False(mc.Contains("key_expired"));
            Assert.Null(value);
        }

        [Fact]
        public void AddOrGetExisting_String_Object_CacheItemPolicy_String()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.AddOrGetExisting(null, "value", null);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.AddOrGetExisting("key", null, null);
            });

            var cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
            cip.SlidingExpiration = TimeSpan.FromMinutes(1);

            Assert.Throws<ArgumentException>(() =>
            {
                mc.AddOrGetExisting("key", "value", cip);
            });

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.MinValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.AddOrGetExisting("key3", "value", cip);
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.AddOrGetExisting("key", "value", null, "region");
            });

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(500);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.AddOrGetExisting("key3", "value", cip);
            });

            cip = new CacheItemPolicy();
            cip.Priority = (CacheItemPriority)20;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.AddOrGetExisting("key3", "value", cip);
            });

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromTicks(0L);
            mc.AddOrGetExisting("key3_A2-1", "value", cip);
            Assert.True(mc.Contains("key3_A2-1"));

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(365);
            mc.AddOrGetExisting("key3_A2-2", "value", cip);
            Assert.True(mc.Contains("key3_A2-2"));

            cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments arguments) => { };
            object value = mc.AddOrGetExisting("key3_A2-3", "value", cip);
            Assert.True(mc.Contains("key3_A2-3"));
            Assert.Null(value);

            mc.Calls.Clear();
            value = mc.AddOrGetExisting("key3_A2-3", "value2", null);
            Assert.True(mc.Contains("key3_A2-3"));
            Assert.NotNull(value);
            Assert.Equal("value", value);
            Assert.Equal(2, mc.Calls.Count);
            Assert.Equal("AddOrGetExisting (string key, object value, CacheItemPolicy policy, string regionName = null)", mc.Calls[0]);

            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.MinValue;
            value = mc.AddOrGetExisting("key_expired", "value", cip);
            Assert.False(mc.Contains("key_expired"));
            Assert.Null(value);
        }

        [Fact]
        public void AddOrGetExisting_CacheItem_CacheItemPolicy()
        {
            var mc = new PokerMemoryCache("MyCache");
            CacheItem ci, ci2;

            Assert.Throws<ArgumentNullException>(() =>
            {
                ci = mc.AddOrGetExisting(null, new CacheItemPolicy());
            });

            ci = new CacheItem("key", "value");
            ci2 = mc.AddOrGetExisting(ci, null);

            Assert.NotNull(ci2);
            Assert.NotEqual(ci, ci2);
            Assert.Null(ci2.Value);
            Assert.True(mc.Contains(ci.Key));
            Assert.Equal(ci.Key, ci2.Key);

            ci = new CacheItem("key", "value");
            ci2 = mc.AddOrGetExisting(ci, null);
            Assert.NotNull(ci2);
            Assert.NotEqual(ci, ci2);
            Assert.NotNull(ci2.Value);
            Assert.Equal(ci.Value, ci2.Value);
            Assert.Equal(ci.Key, ci2.Key);

            Assert.Throws<ArgumentNullException>(() =>
            {
                ci = new CacheItem(null, "value");
                ci2 = mc.AddOrGetExisting(ci, null);
            });

            ci = new CacheItem(String.Empty, "value");
            ci2 = mc.AddOrGetExisting(ci, null);
            Assert.NotNull(ci2);
            Assert.NotEqual(ci, ci2);
            Assert.Null(ci2.Value);
            Assert.True(mc.Contains(ci.Key));
            Assert.Equal(ci.Key, ci2.Key);

            ci = new CacheItem("key2", null);

            // Thrown from:
            // at System.Runtime.Caching.MemoryCacheEntry..ctor(String key, Object value, DateTimeOffset absExp, TimeSpan slidingExp, CacheItemPriority priority, Collection`1 dependencies, CacheEntryRemovedCallback removedCallback, MemoryCache cache)
            // at System.Runtime.Caching.MemoryCache.AddOrGetExistingInternal(String key, Object value, CacheItemPolicy policy)
            // at System.Runtime.Caching.MemoryCache.AddOrGetExisting(CacheItem item, CacheItemPolicy policy)
            // at MonoTests.System.Runtime.Caching.MemoryCacheTest.AddOrGetExisting_CacheItem_CacheItemPolicy() in C:\Users\grendel\documents\visual studio 2010\Projects\System.Runtime.Caching.Test\System.Runtime.Caching.Test\System.Runtime.Caching\MemoryCacheTest.cs:line 211
            Assert.Throws<ArgumentNullException>(() =>
            {
                ci2 = mc.AddOrGetExisting(ci, null);
            });

            ci = new CacheItem("key3", "value");
            var cip = new CacheItemPolicy();
            cip.UpdateCallback = (CacheEntryUpdateArguments arguments) => { };
            Assert.Throws<ArgumentException>(() =>
            {
                ci2 = mc.AddOrGetExisting(ci, cip);
            });

            ci = new CacheItem("key3", "value");
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.Now;
            cip.SlidingExpiration = TimeSpan.FromTicks(DateTime.Now.Ticks);
            Assert.Throws<ArgumentException>(() =>
            {
                mc.AddOrGetExisting(ci, cip);
            });

            ci = new CacheItem("key3", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.MinValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.AddOrGetExisting(ci, cip);
            });

            ci = new CacheItem("key4_#B4-2", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromTicks(0L);
            mc.AddOrGetExisting(ci, cip);
            Assert.True(mc.Contains("key4_#B4-2"));

            ci = new CacheItem("key3", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(500);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.AddOrGetExisting(ci, cip);
            });

            ci = new CacheItem("key5_#B5-2", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(365);
            mc.AddOrGetExisting(ci, cip);
            Assert.True(mc.Contains("key5_#B5-2"));

            ci = new CacheItem("key3", "value");
            cip = new CacheItemPolicy();
            cip.Priority = (CacheItemPriority)20;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.AddOrGetExisting(ci, cip);
            });

            ci = new CacheItem("key3_B7", "value");
            cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments arguments) => { };
            ci2 = mc.AddOrGetExisting(ci, cip);
            Assert.True(mc.Contains("key3_B7"));

            Assert.NotNull(ci2);
            Assert.NotEqual(ci, ci2);
            Assert.Null(ci2.Value);
            Assert.True(mc.Contains(ci.Key));
            Assert.Equal(ci.Key, ci2.Key);

            // The entry is never inserted as its expiration date is before now
            ci = new CacheItem("key_D1", "value_D1");
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.MinValue;
            ci2 = mc.AddOrGetExisting(ci, cip);
            Assert.False(mc.Contains("key_D1"));
            Assert.NotNull(ci2);
            Assert.Null(ci2.Value);
            Assert.Equal("key_D1", ci2.Key);

            mc.Calls.Clear();
            ci = new CacheItem("key_D2", "value_D2");
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.MaxValue;
            mc.AddOrGetExisting(ci, cip);
            Assert.True(mc.Contains("key_D2"));
            Assert.Equal(2, mc.Calls.Count);
            Assert.Equal("AddOrGetExisting (CacheItem item, CacheItemPolicy policy)", mc.Calls[0]);
        }

        [Fact]
        public void Set_String_Object_CacheItemPolicy_String()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.Set("key", "value", new CacheItemPolicy(), "region");
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Set(null, "value", new CacheItemPolicy());
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Set("key", null, new CacheItemPolicy());
            });

            var cip = new CacheItemPolicy();
            cip.UpdateCallback = (CacheEntryUpdateArguments arguments) => { };
            cip.RemovedCallback = (CacheEntryRemovedArguments arguments) => { };
            Assert.Throws<ArgumentException>(() =>
            {
                mc.Set("key", "value", cip);
            });

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.MinValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.Set("key", "value", cip);
            });

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromTicks(0L);
            mc.Set("key_A1-6", "value", cip);
            Assert.True(mc.Contains("key_A1-6"));

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(500);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.Set("key", "value", cip);
            });

            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(365);
            mc.Set("key_A1-8", "value", cip);
            Assert.True(mc.Contains("key_A1-8"));

            cip = new CacheItemPolicy();
            cip.Priority = (CacheItemPriority)20;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.Set("key", "value", cip);
            });

            cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments arguments) => { };
            mc.Set("key_A2", "value_A2", cip);
            Assert.True(mc.Contains("key_A2"));

            mc.Set("key_A3", "value_A3", new CacheItemPolicy());
            Assert.True(mc.Contains("key_A3"));
            Assert.Equal("value_A3", mc.Get("key_A3"));

            // The entry is never inserted as its expiration date is before now
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.MinValue;
            mc.Set("key_A4", "value_A4", cip);
            Assert.False(mc.Contains("key_A4"));

            mc.Calls.Clear();
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.MaxValue;
            mc.Set("key_A5", "value_A5", cip);
            Assert.True(mc.Contains("key_A5"));
            Assert.Equal(2, mc.Calls.Count);
            Assert.Equal("Set (string key, object value, CacheItemPolicy policy, string regionName = null)", mc.Calls[0]);
        }

        [Fact]
        public void Set_String_Object_DateTimeOffset_String()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.Set("key", "value", DateTimeOffset.MaxValue, "region");
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Set(null, "value", DateTimeOffset.MaxValue);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Set("key", null, DateTimeOffset.MaxValue);
            });

            // The entry is never inserted as its expiration date is before now
            mc.Set("key_A2", "value_A2", DateTimeOffset.MinValue);
            Assert.False(mc.Contains("key_A2"));

            mc.Calls.Clear();
            mc.Set("key", "value", DateTimeOffset.MaxValue);

            Assert.Equal(2, mc.Calls.Count);
            Assert.Equal("Set (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)", mc.Calls[0]);
            Assert.Equal("Set (string key, object value, CacheItemPolicy policy, string regionName = null)", mc.Calls[1]);
        }

        [Fact]
        public void Set_CacheItem_CacheItemPolicy()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Set(null, new CacheItemPolicy());
            });

            // Actually thrown from the Set (string, object, CacheItemPolicy, string) overload
            var ci = new CacheItem(null, "value");
            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Set(ci, new CacheItemPolicy());
            });

            ci = new CacheItem("key", null);
            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Set(ci, new CacheItemPolicy());
            });

            ci = new CacheItem("key", "value");
            var cip = new CacheItemPolicy();
            cip.UpdateCallback = (CacheEntryUpdateArguments arguments) => { };
            cip.RemovedCallback = (CacheEntryRemovedArguments arguments) => { };
            Assert.Throws<ArgumentException>(() =>
            {
                mc.Set(ci, cip);
            });

            ci = new CacheItem("key", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.MinValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.Set(ci, cip);
            });

            ci = new CacheItem("key_A1-6", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromTicks(0L);
            mc.Set(ci, cip);
            Assert.True(mc.Contains("key_A1-6"));

            ci = new CacheItem("key", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(500);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.Set(ci, cip);
            });

            ci = new CacheItem("key_A1-8", "value");
            cip = new CacheItemPolicy();
            cip.SlidingExpiration = TimeSpan.FromDays(365);
            mc.Set(ci, cip);
            Assert.True(mc.Contains("key_A1-8"));

            ci = new CacheItem("key", "value");
            cip = new CacheItemPolicy();
            cip.Priority = (CacheItemPriority)20;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                mc.Set(ci, cip);
            });

            ci = new CacheItem("key_A2", "value_A2");
            cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments arguments) => { };
            mc.Set(ci, cip);
            Assert.True(mc.Contains("key_A2"));

            ci = new CacheItem("key_A3", "value_A3");
            mc.Set(ci, new CacheItemPolicy());
            Assert.True(mc.Contains("key_A3"));
            Assert.Equal("value_A3", mc.Get("key_A3"));

            // The entry is never inserted as its expiration date is before now
            ci = new CacheItem("key_A4", "value");
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.MinValue;
            mc.Set(ci, cip);
            Assert.False(mc.Contains("key_A4"));

            ci = new CacheItem("key_A5", "value");
            mc.Calls.Clear();
            mc.Set(ci, new CacheItemPolicy());

            Assert.Equal(2, mc.Calls.Count);
            Assert.Equal("Set (CacheItem item, CacheItemPolicy policy)", mc.Calls[0]);
            Assert.Equal("Set (string key, object value, CacheItemPolicy policy, string regionName = null)", mc.Calls[1]);
        }

        [Fact]
        public void Remove()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.Remove("key", "region");
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.Remove(null);
            });

            bool callbackInvoked;
            CacheEntryRemovedReason reason = (CacheEntryRemovedReason)1000;
            var cip = new CacheItemPolicy();
            cip.Priority = CacheItemPriority.NotRemovable;
            mc.Set("key2", "value1", cip);
            object value = mc.Remove("key2");

            Assert.NotNull(value);
            Assert.False(mc.Contains("key2"));

            cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                callbackInvoked = true;
                reason = args.RemovedReason;
            };

            mc.Set("key", "value", cip);
            callbackInvoked = false;
            reason = (CacheEntryRemovedReason)1000;
            value = mc.Remove("key");
            Assert.NotNull(value);
            Assert.True(callbackInvoked);
            Assert.Equal(CacheEntryRemovedReason.Removed, reason);

            cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                callbackInvoked = true;
                reason = args.RemovedReason;
                throw new ApplicationException("test");
            };

            mc.Set("key", "value", cip);
            callbackInvoked = false;
            reason = (CacheEntryRemovedReason)1000;
            value = mc.Remove("key");
            Assert.NotNull(value);
            Assert.True(callbackInvoked);
            Assert.Equal(CacheEntryRemovedReason.Removed, reason);

            cip = new CacheItemPolicy();
            cip.UpdateCallback = (CacheEntryUpdateArguments args) =>
            {
                callbackInvoked = true;
                reason = args.RemovedReason;
            };

            mc.Set("key", "value", cip);
            callbackInvoked = false;
            reason = (CacheEntryRemovedReason)1000;
            value = mc.Remove("key");
            Assert.NotNull(value);
            Assert.False(callbackInvoked);

            cip = new CacheItemPolicy();
            cip.UpdateCallback = (CacheEntryUpdateArguments args) =>
            {
                callbackInvoked = true;
                reason = args.RemovedReason;
                throw new ApplicationException("test");
            };

            mc.Set("key", "value", cip);
            callbackInvoked = false;
            reason = (CacheEntryRemovedReason)1000;
            value = mc.Remove("key");
            Assert.NotNull(value);
            Assert.False(callbackInvoked);
        }

        [Fact]
        public void GetValues()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.GetValues((string[])null);
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.GetValues(new string[] { }, "region");
            });

            Assert.Throws<ArgumentException>(() =>
            {
                mc.GetValues(new string[] { "key", null });
            });

            IDictionary<string, object> value = mc.GetValues(new string[] { });
            Assert.Null(value);

            mc.Set("key1", "value1", null);
            mc.Set("key2", "value2", null);
            mc.Set("key3", "value3", null);

            Assert.True(mc.Contains("key1"));
            Assert.True(mc.Contains("key2"));
            Assert.True(mc.Contains("key3"));

            value = mc.GetValues(new string[] { "key1", "key3" });
            Assert.NotNull(value);
            Assert.Equal(2, value.Count);
            Assert.Equal("value1", value["key1"]);
            Assert.Equal("value3", value["key3"]);
            Assert.Equal(typeof(Dictionary<string, object>), value.GetType());

            // MSDN says the number of items in the returned dictionary should be the same as in the 
            // 'keys' collection - this is not the case. The returned dictionary contains only entries for keys
            // that exist in the cache.
            value = mc.GetValues(new string[] { "key1", "key3", "nosuchkey" });
            Assert.NotNull(value);
            Assert.Equal(2, value.Count);
            Assert.Equal("value1", value["key1"]);
            Assert.Equal("value3", value["key3"]);
            Assert.False(value.ContainsKey("Key1"));
        }

        [Fact]
        public void ChangeMonitors()
        {
            bool removed = false;
            var mc = new PokerMemoryCache("MyCache");
            var cip = new CacheItemPolicy();
            var monitor = new PokerChangeMonitor();
            cip.ChangeMonitors.Add(monitor);
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                removed = true;
            };

            mc.Set("key", "value", cip);
            Assert.Equal(0, monitor.Calls.Count);

            monitor.SignalChange();
            Assert.True(removed);

            bool onChangedCalled = false;
            monitor = new PokerChangeMonitor();
            monitor.NotifyOnChanged((object state) =>
            {
                onChangedCalled = true;
            });

            cip = new CacheItemPolicy();
            cip.ChangeMonitors.Add(monitor);

            // Thrown by ChangeMonitor.NotifyOnChanged
            Assert.Throws<InvalidOperationException>(() =>
            {
                mc.Set("key1", "value1", cip);
            });

            Assert.False(onChangedCalled);
        }

        // Due to internal implementation details Trim has very few easily verifiable scenarios
        [Fact]
        public void Trim()
        {
            var config = new NameValueCollection();
            config["__MonoEmulateOneCPU"] = "true";
            var mc = new MemoryCache("MyCache", config);

            var numCpuCores = Environment.ProcessorCount;
            var numItems = numCpuCores > 1 ? numCpuCores / 2 : 1;

            for (int i = 0; i < numItems;)
            {
                var key = "key" + i*i*i + "key" + ++i;
                mc.Set(key, "value" + i.ToString(), null);
            }

            Assert.Equal(numItems, mc.GetCount());

            // Trimming 75% for such a small number of items (supposedly each in its cache store) will end up trimming all of them
            long trimmed = mc.Trim(75);
            Assert.Equal(numItems, trimmed);
            Assert.Equal(0, mc.GetCount());

            mc = new MemoryCache("MyCache", config);
            var cip = new CacheItemPolicy();
            cip.Priority = CacheItemPriority.NotRemovable;
            for (int i = 0; i < 11; i++)
            {
                mc.Set("key" + i.ToString(), "value" + i.ToString(), cip);
            }

            Assert.Equal(11, mc.GetCount());
            trimmed = mc.Trim(50);
            Assert.Equal(11, mc.GetCount());
        }

        [Fact]
        public void TestExpiredGetValues()
        {
            var config = new NameValueCollection();
            config["cacheMemoryLimitMegabytes"] = 0.ToString();
            config["physicalMemoryLimitPercentage"] = 100.ToString();
            config["pollingInterval"] = new TimeSpan(0, 0, 10).ToString();

            using (var mc = new MemoryCache("TestExpiredGetValues", config))
            {
                Assert.Equal(0, mc.GetCount());

                var keys = new List<string>();

                // add some short duration entries
                for (int i = 0; i < 10; i++)
                {
                    var key = "short-" + i;
                    var expireAt = DateTimeOffset.Now.AddMilliseconds(50);
                    mc.Add(key, i.ToString(), expireAt);

                    keys.Add(key);
                }

                Assert.Equal(10, mc.GetCount());

                // wait past expiration and call GetValues() - this does not affect the count
                Thread.Sleep(100);
                mc.GetValues(keys);
                Assert.Equal(0, mc.GetCount());
            }
        }

        [Fact]
        public void TestCacheSliding()
        {
            var config = new NameValueCollection();
            config["cacheMemoryLimitMegabytes"] = 0.ToString();
            config["physicalMemoryLimitPercentage"] = 100.ToString();
            config["pollingInterval"] = new TimeSpan(0, 0, 1).ToString();

            using (var mc = new MemoryCache("TestCacheSliding", config))
            {
                Assert.Equal(0, mc.GetCount());

                var cip = new CacheItemPolicy();
                // The sliding expiration timeout has to be greater than 1 second because
                // .NET implementation ignores timeouts updates smaller than
                // CacheExpires.MIN_UPDATE_DELTA which is equal to 1.
                cip.SlidingExpiration = TimeSpan.FromSeconds(2);
                mc.Add("slidingtest", "42", cip);

                mc.Add("expire1", "1", cip);
                mc.Add("expire2", "2", cip);
                mc.Add("expire3", "3", cip);
                mc.Add("expire4", "4", cip);
                mc.Add("expire5", "5", cip);

                Assert.Equal(6, mc.GetCount());

                for (int i = 0; i < 50; i++)
                {
                    Thread.Sleep(100);

                    var item = mc.Get("slidingtest");
                    Assert.NotEqual(null, item);
                }

                Assert.Null(mc.Get("expire1"));
                Assert.Null(mc.Get("expire2"));
                Assert.Null(mc.Get("expire3"));
                Assert.Null(mc.Get("expire4"));
                Assert.Null(mc.Get("expire5"));
                Assert.Equal(1, mc.GetCount());

                Thread.Sleep(3000);

                Assert.Null(mc.Get("slidingtest"));
                Assert.Equal(0, mc.GetCount());
            }
        }
    }

    public class MemoryCacheTestExpires1
    {
        [Fact]
        [OuterLoop] // makes long wait
        public async Task TimedExpirationAsync()
        {
            bool removed = false;
            CacheEntryRemovedReason reason = CacheEntryRemovedReason.CacheSpecificEviction;
            int sleepPeriod = 20000;

            var mc = new PokerMemoryCache("MyCache");
            var cip = new CacheItemPolicy();

            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                removed = true;
                reason = args.RemovedReason;
            };
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(50);
            mc.Set("key", "value", cip);

            // Wait past cip.AbsoluteExpiration
            Thread.Sleep(500);
            object value = mc.Get("key");
            Assert.Null(value);

            // Rather than waiting for the expiration callback to fire,
            // we replace the cache item and verify that the reason is still Expired
            mc.Set("key", "value2", cip);
            Assert.True(removed);
            Assert.Equal(CacheEntryRemovedReason.Expired, reason);

            removed = false;
            cip = new CacheItemPolicy();
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                removed = true;
                reason = args.RemovedReason;
            };
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(50);
            mc.Set("key", "value", cip);
            await Task.Delay(sleepPeriod);

            Assert.Null(mc.Get("key"));
            Assert.True(removed);
            Assert.Equal(CacheEntryRemovedReason.Expired, reason);
        }
    }

    public class MemoryCacheTestExpires11
    {
        [Fact]
        [OuterLoop] // makes long wait
        public async Task TimedExpirationAsync()
        {
            int sleepPeriod = 20000;

            var mc = new PokerMemoryCache("MyCache");
            var cip = new CacheItemPolicy();

            int expiredCount = 0;
            object expiredCountLock = new object();
            CacheEntryRemovedCallback removedCb = (CacheEntryRemovedArguments args) =>
            {
                lock (expiredCountLock)
                {
                    expiredCount++;
                }
            };

            cip = new CacheItemPolicy();
            cip.RemovedCallback = removedCb;
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(20);
            mc.Set("key1", "value1", cip);

            cip = new CacheItemPolicy();
            cip.RemovedCallback = removedCb;
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(200);
            mc.Set("key2", "value2", cip);

            cip = new CacheItemPolicy();
            cip.RemovedCallback = removedCb;
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(600);
            mc.Set("key3", "value3", cip);

            cip = new CacheItemPolicy();
            cip.RemovedCallback = removedCb;
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(sleepPeriod + 55500);
            mc.Set("key4", "value4", cip);

            await Task.Delay(sleepPeriod);
            Assert.Null(mc.Get("key1"));
            Assert.Null(mc.Get("key2"));
            Assert.Null(mc.Get("key3"));
            Assert.NotNull(mc.Get("key4"));
            Assert.Equal(3, expiredCount);
        }
    }

    public class MemoryCacheTestExpires2
    {
        [Fact]
        [OuterLoop] // makes long wait
        public async Task GetEnumeratorAsync()
        {
            var mc = new PokerMemoryCache("MyCache");

            // This one is a Hashtable enumerator
            IEnumerator enumerator = ((IEnumerable)mc).GetEnumerator();

            // This one is a Dictionary <string, object> enumerator
            IEnumerator enumerator2 = mc.DoGetEnumerator();

            Assert.NotNull(enumerator);
            Assert.NotNull(enumerator2);
            Assert.True(enumerator.GetType() != enumerator2.GetType());

            mc.Set("key1", "value1", null);
            mc.Set("key2", "value2", null);
            mc.Set("key3", "value3", null);

            bool expired4 = false;
            var cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTime.Now.AddMilliseconds(50);
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                expired4 = true;
            };

            mc.Set("key4", "value4", cip);
            // wait past "key4" AbsoluteExpiration 
            Thread.Sleep(500);

            enumerator = ((IEnumerable)mc).GetEnumerator();
            int count = 0;
            while (enumerator.MoveNext())
            {
                count++;
            }

            Assert.Equal(3, count);

            bool expired5 = false;
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTime.Now.AddMilliseconds(50);
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                expired5 = true;
            };

            mc.Set("key5", "value5", cip);
            await Task.Delay(20500);

            enumerator2 = mc.DoGetEnumerator();
            count = 0;
            while (enumerator2.MoveNext())
            {
                count++;
            }

            Assert.True(expired4);
            Assert.True(expired5);
            Assert.Equal(3, count);
        }
    }

    public class MemoryCacheTestExpires3
    {
        [Fact]
        [OuterLoop] // makes long wait
        public async Task GetCacheItem()
        {
            var mc = new PokerMemoryCache("MyCache");

            Assert.Throws<NotSupportedException>(() =>
            {
                mc.GetCacheItem("key", "region");
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                mc.GetCacheItem(null);
            });

            CacheItem value;
            mc.Set("key", "value", null);
            value = mc.GetCacheItem("key");
            Assert.NotNull(value);
            Assert.Equal("value", value.Value);
            Assert.Equal("key", value.Key);

            value = mc.GetCacheItem("doesnotexist");
            Assert.Null(value);

            var cip = new CacheItemPolicy();
            bool callbackInvoked = false;
            CacheEntryRemovedReason reason = (CacheEntryRemovedReason)1000;

            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(50);
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                callbackInvoked = true;
                reason = args.RemovedReason;
            };
            mc.Set("key", "value", cip);
            // wait past the expiration time and verify that the item is gone
            await Task.Delay(500);
            value = mc.GetCacheItem("key");
            Assert.Null(value);

            // add a new item with the same key
            cip = new CacheItemPolicy();
            cip.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(50);
            cip.RemovedCallback = (CacheEntryRemovedArguments args) =>
            {
                callbackInvoked = true;
                reason = args.RemovedReason;
                throw new ApplicationException("test");
            };
            mc.Set("key", "value", cip);

            // and verify that the old item callback is called
            Assert.True(callbackInvoked);
            Assert.Equal(CacheEntryRemovedReason.Expired, reason);

            callbackInvoked = false;
            reason = (CacheEntryRemovedReason)1000;

            // wait for both expiration and the callback of the new item
            await Task.Delay(20500);

            value = mc.GetCacheItem("key");
            Assert.Null(value);
            Assert.True(callbackInvoked);
            Assert.Equal(CacheEntryRemovedReason.Expired, reason);
        }
    }

    public class MemoryCacheTestExpires4
    {
        [Fact]
        public async Task TestCacheShrink()
        {
            const int HEAP_RESIZE_THRESHOLD = 8192 + 2;
            const int HEAP_RESIZE_SHORT_ENTRIES = 2048;
            const int HEAP_RESIZE_LONG_ENTRIES = HEAP_RESIZE_THRESHOLD - HEAP_RESIZE_SHORT_ENTRIES;

            var config = new NameValueCollection();
            config["cacheMemoryLimitMegabytes"] = 0.ToString();
            config["physicalMemoryLimitPercentage"] = 100.ToString();
            config["pollingInterval"] = new TimeSpan(0, 0, 1).ToString();

            using (var mc = new MemoryCache("TestCacheShrink", config))
            {
                Assert.Equal(0, mc.GetCount());

                // add some short duration entries
                for (int i = 0; i < HEAP_RESIZE_SHORT_ENTRIES; i++)
                {
                    var expireAt = DateTimeOffset.Now.AddSeconds(3);
                    mc.Add("short-" + i, i.ToString(), expireAt);
                }

                Assert.Equal(HEAP_RESIZE_SHORT_ENTRIES, mc.GetCount());

                // add some long duration entries				
                for (int i = 0; i < HEAP_RESIZE_LONG_ENTRIES; i++)
                {
                    var expireAt = DateTimeOffset.Now.AddSeconds(42);
                    mc.Add("long-" + i, i.ToString(), expireAt);
                }

                Assert.Equal(HEAP_RESIZE_LONG_ENTRIES + HEAP_RESIZE_SHORT_ENTRIES, mc.GetCount());

                // wait past the short duration items expiration time
                await Task.Delay(4000);

                /// the following will also shrink the size of the cache
                for (int i = 0; i < HEAP_RESIZE_SHORT_ENTRIES; i++)
                {
                    Assert.Null(mc.Get("short-" + i));
                }
                Assert.Equal(HEAP_RESIZE_LONG_ENTRIES, mc.GetCount());

                // add some new items into the cache, this will grow the cache again
                for (int i = 0; i < HEAP_RESIZE_LONG_ENTRIES; i++)
                {
                    mc.Add("final-" + i, i.ToString(), DateTimeOffset.Now.AddSeconds(4));
                }

                Assert.Equal(HEAP_RESIZE_LONG_ENTRIES + HEAP_RESIZE_LONG_ENTRIES, mc.GetCount());
            }
        }
    }

    public class MemoryCacheTestExpires5
    {
        [Fact]
        public async Task TestCacheExpiryOrdering()
        {
            var config = new NameValueCollection();
            config["cacheMemoryLimitMegabytes"] = 0.ToString();
            config["physicalMemoryLimitPercentage"] = 100.ToString();
            config["pollingInterval"] = new TimeSpan(0, 0, 1).ToString();

            using (var mc = new MemoryCache("TestCacheExpiryOrdering", config))
            {
                Assert.Equal(0, mc.GetCount());

                // add long lived items into the cache first
                for (int i = 0; i < 100; i++)
                {
                    var cip = new CacheItemPolicy();
                    cip.SlidingExpiration = new TimeSpan(0, 0, 4);
                    mc.Add("long-" + i, i, cip);
                }

                Assert.Equal(100, mc.GetCount());

                // add shorter lived items into the cache, these should expire first
                for (int i = 0; i < 100; i++)
                {
                    var cip = new CacheItemPolicy();
                    cip.SlidingExpiration = new TimeSpan(0, 0, 1);
                    mc.Add("short-" + i, i, cip);
                }

                Assert.Equal(200, mc.GetCount());

                await Task.Delay(2000);

                for (int i = 0; i < 100; i++)
                {
                    Assert.Null(mc.Get("short-" + i));
                }
                Assert.Equal(100, mc.GetCount());
            }
        }
    }
}
