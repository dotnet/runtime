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
using System.Runtime.Caching;

using Xunit;
using MonoTests.Common;

namespace MonoTests.System.Runtime.Caching
{
    public class ObjectCacheTest
    {
        [Fact]
        [ActiveIssue(25168)]
        private static void Host_SetToProvider()
        {
            var tns1 = new TestNotificationSystem();
            var tns2 = new TestNotificationSystem();
            ObjectCache.Host = tns1;
            Assert.NotNull(ObjectCache.Host);
            Assert.Equal(tns1, ObjectCache.Host);

            Assert.Throws<InvalidOperationException>(() =>
            {
                ObjectCache.Host = tns2;
            });
        }

        [Fact]
        public void Add_CacheItem_CacheItemPolicy()
        {
            var poker = new PokerObjectCache();
            bool ret;

            ret = poker.Add(null, null);
            Assert.True(ret);
            Assert.Equal("AddOrGetExisting (CacheItem value, CacheItemPolicy policy)", poker.MethodCalled);

            var item = new CacheItem("key", 1234);
            ret = poker.Add(item, null);
            Assert.True(ret);
            Assert.Equal("AddOrGetExisting (CacheItem value, CacheItemPolicy policy)", poker.MethodCalled);

            ret = poker.Add(item, null);
            Assert.False(ret);
            Assert.Equal("AddOrGetExisting (CacheItem value, CacheItemPolicy policy)", poker.MethodCalled);
        }

        [Fact]
        public void Add_String_Object_CacheItemPolicy_String()
        {
            var poker = new PokerObjectCache();
            bool ret;

            ret = poker.Add(null, null, null, null);
            Assert.True(ret);
            Assert.Equal("AddOrGetExisting (string key, object value, CacheItemPolicy policy, string regionName = null)", poker.MethodCalled);

            ret = poker.Add("key", 1234, null, null);
            Assert.True(ret);
            Assert.Equal("AddOrGetExisting (string key, object value, CacheItemPolicy policy, string regionName = null)", poker.MethodCalled);

            ret = poker.Add("key", 1234, null, null);
            Assert.False(ret);
            Assert.Equal("AddOrGetExisting (string key, object value, CacheItemPolicy policy, string regionName = null)", poker.MethodCalled);
        }

        [Fact]
        public void Add_String_Object_DateTimeOffset_String()
        {
            var poker = new PokerObjectCache();
            bool ret;

            ret = poker.Add(null, null, DateTimeOffset.Now, null);
            Assert.True(ret);
            Assert.Equal("AddOrGetExisting (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)", poker.MethodCalled);

            ret = poker.Add("key", 1234, DateTimeOffset.Now, null);
            Assert.True(ret);
            Assert.Equal("AddOrGetExisting (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)", poker.MethodCalled);

            ret = poker.Add("key", 1234, DateTimeOffset.Now, null);
            Assert.False(ret);
            Assert.Equal("AddOrGetExisting (string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)", poker.MethodCalled);
        }

        [Fact]
        public void GetValues()
        {
            var poker = new PokerObjectCache();

            IDictionary<string, object> values = poker.GetValues(null, (string[])null);
            Assert.NotNull(values);
            Assert.Equal(0, values.Count);
            Assert.Equal("IDictionary<string, object> GetValues (IEnumerable<string> keys, string regionName = null)", poker.MethodCalled);

            poker.Add("key1", 1, null);
            poker.Add("key2", 2, null);
            poker.Add("key3", 3, null);

            values = poker.GetValues(new string[] { "key1", "key2", "key3" });
            Assert.NotNull(values);
            Assert.Equal(3, values.Count);
            Assert.Equal("IDictionary<string, object> GetValues (IEnumerable<string> keys, string regionName = null)", poker.MethodCalled);

            values = poker.GetValues(new string[] { "key1", "key22", "key3" });
            Assert.NotNull(values);
            Assert.Equal(2, values.Count);
            Assert.Equal("IDictionary<string, object> GetValues (IEnumerable<string> keys, string regionName = null)", poker.MethodCalled);
        }

        [Fact]
        public void Defaults()
        {
            Assert.Equal(DateTimeOffset.MaxValue, ObjectCache.InfiniteAbsoluteExpiration);
            Assert.Equal(TimeSpan.Zero, ObjectCache.NoSlidingExpiration);
        }
    }
}
