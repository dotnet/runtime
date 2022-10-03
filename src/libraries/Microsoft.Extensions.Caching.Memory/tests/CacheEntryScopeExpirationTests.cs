// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class CacheEntryScopeExpirationTests
    {
        private IMemoryCache CreateCache(bool trackLinkedCacheEntries = false)
        {
            return CreateCache(new SystemClock(), trackLinkedCacheEntries);
        }

        private IMemoryCache CreateCache(ISystemClock clock, bool trackLinkedCacheEntries = false)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
                TrackLinkedCacheEntries = trackLinkedCacheEntries
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetPopulates_ExpirationTokens_IntoScopedLink(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";

            ICacheEntry entry;
            using (entry = cache.CreateEntry(key))
            {
                VerifyCurrentEntry(trackLinkedCacheEntries, entry);

                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Equal(trackLinkedCacheEntries ? 1 : 0, entry.ExpirationTokens.Count);
            Assert.Null(entry.AbsoluteExpiration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetPopulates_AbsoluteExpiration_IntoScopeLink(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            var time = new DateTimeOffset(2051, 1, 1, 1, 1, 1, TimeSpan.Zero);

            ICacheEntry entry;
            using (entry = cache.CreateEntry(key))
            {
                VerifyCurrentEntry(trackLinkedCacheEntries, entry);

                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(time));
            }

            Assert.Empty(entry.ExpirationTokens);
            Assert.Equal(trackLinkedCacheEntries ? time : null, entry.AbsoluteExpiration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TokenExpires_LinkedEntry(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);

                cache.Set(key1, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TokenExpires_GetInLinkedEntry(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            cache.GetOrCreate(key1, e =>
            {
                e.AddExpirationToken(expirationToken);
                return obj;
            });

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(cache.Get(key1));
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TokenExpires_ParentScopeEntry(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);

                using (var entry1 = cache.CreateEntry(key1))
                {
                    entry1.SetValue(obj);
                    entry1.AddExpirationToken(expirationToken);
                }
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TokenExpires_ParentScopeEntry_WithFactory(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            cache.GetOrCreate(key, entry =>
            {
                cache.GetOrCreate(key1, entry1 =>
                {
                    entry1.AddExpirationToken(expirationToken);
                    return obj;
                });

                return obj;
            });

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TokenDoesntExpire_SiblingScopeEntry(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            string key2 = "myKey2";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);

                using (var entry1 = cache.CreateEntry(key1))
                {
                    entry1.SetValue(obj);
                    entry1.AddExpirationToken(expirationToken);
                }

                using (var entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                }
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key, out value));
            Assert.True(cache.TryGetValue(key2, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AbsoluteExpiration_WorksAcrossLink(bool trackLinkedCacheEntries)
        {
            var clock = new TestClock();
            var cache = CreateCache(clock, trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);
                cache.Set(key1, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(5)));
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            clock.Add(TimeSpan.FromSeconds(10));

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AbsoluteExpiration_WorksAcrossNestedLink(bool trackLinkedCacheEntries)
        {
            var clock = new TestClock();
            var cache = CreateCache(clock, trackLinkedCacheEntries);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry1 = cache.CreateEntry(key1))
            {
                entry1.SetValue(obj);

                using (var entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                    entry2.SetAbsoluteExpiration(TimeSpan.FromSeconds(5));
                }
            }

            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));

            clock.Add(TimeSpan.FromSeconds(10));

            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key2, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AbsoluteExpiration_DoesntAffectSiblingLink(bool trackLinkedCacheEntries)
        {
            var clock = new TestClock();
            var cache = CreateCache(clock, trackLinkedCacheEntries);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";
            string key3 = "myKey3";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry1 = cache.CreateEntry(key1))
            {
                entry1.SetValue(obj);

                using (var entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                    entry2.SetAbsoluteExpiration(TimeSpan.FromSeconds(5));
                }

                using (var entry3 = cache.CreateEntry(key3))
                {
                    entry3.SetValue(obj);
                    entry3.SetAbsoluteExpiration(TimeSpan.FromSeconds(15));
                }
            }

            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));
            Assert.Same(obj, cache.Get(key3));

            clock.Add(TimeSpan.FromSeconds(10));

            Assert.Equal(!trackLinkedCacheEntries, cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key2, out value));
            Assert.True(cache.TryGetValue(key3, out value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetWithImplicitLinkPopulatesExpirationTokens(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            Assert.Null(CacheEntry.Current);

            ICacheEntry entry;
            using (entry = cache.CreateEntry(key))
            {
                VerifyCurrentEntry(trackLinkedCacheEntries, entry);

                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key1, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Null(CacheEntry.Current);

            Assert.Equal(trackLinkedCacheEntries ? 1 : 0, entry.ExpirationTokens.Count);
            Assert.Null(entry.AbsoluteExpiration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LinkContextsCanNest(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            Assert.Null(CacheEntry.Current);

            ICacheEntry entry;
            ICacheEntry entry1;
            using (entry = cache.CreateEntry(key))
            {
                VerifyCurrentEntry(trackLinkedCacheEntries, entry);

                using (entry1 = cache.CreateEntry(key1))
                {
                    VerifyCurrentEntry(trackLinkedCacheEntries, entry1);

                    var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                    entry1.SetValue(obj);
                    entry1.AddExpirationToken(expirationToken);
                }

                VerifyCurrentEntry(trackLinkedCacheEntries, entry);
            }

            Assert.Null(CacheEntry.Current);

            Assert.Single(entry1.ExpirationTokens);
            Assert.Null(entry1.AbsoluteExpiration);
            Assert.Equal(trackLinkedCacheEntries ? 1 : 0, entry.ExpirationTokens.Count);
            Assert.Null(entry.AbsoluteExpiration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NestedLinkContextsCanAggregate(bool trackLinkedCacheEntries)
        {
            var clock = new TestClock();
            var cache = CreateCache(clock, trackLinkedCacheEntries);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";

            var expirationToken1 = new TestExpirationToken() { ActiveChangeCallbacks = true };
            var expirationToken2 = new TestExpirationToken() { ActiveChangeCallbacks = true };

            ICacheEntry entry1 = null;
            ICacheEntry entry2 = null;

            using (entry1 = cache.CreateEntry(key1))
            {
                entry1.SetValue(obj);
                entry1
                    .AddExpirationToken(expirationToken1)
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(10));

                using (entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                    entry2
                        .AddExpirationToken(expirationToken2)
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(15));
                }
            }

            Assert.Equal(trackLinkedCacheEntries ? 2 : 1, entry1.ExpirationTokens.Count());
            Assert.NotNull(entry1.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(10), entry1.AbsoluteExpiration);

            Assert.Single(entry2.ExpirationTokens);
            Assert.NotNull(entry2.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(15), entry2.AbsoluteExpiration);
        }

        [Fact]
        public async Task LinkContexts_AreThreadSafe()
        {
            var cache = CreateCache(trackLinkedCacheEntries: true);
            var key1 = new object();
            var key2 = new object();
            var key3 = new object();
            var key4 = new object();
            var value1 = Guid.NewGuid();
            var value2 = Guid.NewGuid();
            var value3 = Guid.NewGuid();
            var value4 = Guid.NewGuid();
            TestExpirationToken t3 = null;
            TestExpirationToken t4 = null;

            Func<Task> func = async () =>
            {
                t3 = new TestExpirationToken() { ActiveChangeCallbacks = true };
                t4 = new TestExpirationToken() { ActiveChangeCallbacks = true };

                value1 = await cache.GetOrCreateAsync(key1, async e1 =>
                {
                    value2 = await cache.GetOrCreateAsync(key2, async e2 =>
                    {
                        await Task.WhenAll(
                            Task.Run(() =>
                            {
                                value3 = cache.Set(key3, Guid.NewGuid(), t3);
                            }),
                            Task.Run(() =>
                            {
                                value4 = cache.Set(key4, Guid.NewGuid(), t4);
                            }));

                        return Guid.NewGuid();
                    });

                    return Guid.NewGuid();
                });
            };

            await func();

            Assert.NotNull(cache.Get(key1));
            Assert.NotNull(cache.Get(key2));
            Assert.Equal(value3, cache.Get(key3));
            Assert.Equal(value4, cache.Get(key4));
            Assert.NotEqual(value3, value4);

            t3.Fire();
            Assert.Equal(value4, cache.Get(key4));

            Assert.Null(cache.Get(key1));
            Assert.Null(cache.Get(key2));
            Assert.Null(cache.Get(key3));

            await func();

            Assert.NotNull(cache.Get(key1));
            Assert.NotNull(cache.Get(key2));
            Assert.Equal(value3, cache.Get(key3));
            Assert.Equal(value4, cache.Get(key4));
            Assert.NotEqual(value3, value4);

            t4.Fire();
            Assert.Equal(value3, cache.Get(key3));

            Assert.Null(cache.Get(key1));
            Assert.Null(cache.Get(key2));
            Assert.Null(cache.Get(key4));
        }

        [Fact]
        public async Task OnceExpiredIsSetToTrueItRemainsTrue()
        {
            var cache = CreateCache();
            var entry = (CacheEntry)cache.CreateEntry("someKey");

            await Task.WhenAll(
                Task.Run(() => SetExpiredManyTimes(entry)),
                Task.Run(() => SetExpiredManyTimes(entry)));

            Assert.True(entry.CheckExpired(DateTime.UtcNow));

            static void SetExpiredManyTimes(CacheEntry cacheEntry)
            {
                var utcNow = DateTime.UtcNow;
                for (int i = 0; i < 1_000; i++)
                {
                    cacheEntry.SetExpired(EvictionReason.Expired); // modifies CacheEntry._state
                    Assert.True(cacheEntry.CheckExpired(utcNow));
                    cacheEntry.Value = cacheEntry; // modifies CacheEntry._state
                    Assert.True(cacheEntry.CheckExpired(utcNow));

                    cacheEntry.SetExpired(EvictionReason.Expired); // modifies CacheEntry._state
                    Assert.True(cacheEntry.CheckExpired(utcNow));
                    cacheEntry.Dispose(); // might modify CacheEntry._state
                    Assert.True(cacheEntry.CheckExpired(utcNow));
                }
            }
        }

        private static void VerifyCurrentEntry(bool trackLinkedCacheEntries, ICacheEntry entry)
        {
            if (trackLinkedCacheEntries)
            {
                Assert.Same(entry, CacheEntry.Current);
            }
            else
            {
                Assert.Null(CacheEntry.Current);
            }
        }
    }
}
