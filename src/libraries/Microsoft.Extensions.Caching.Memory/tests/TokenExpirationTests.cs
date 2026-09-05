// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class TokenExpirationTests
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
                TrackLinkedCacheEntries = trackLinkedCacheEntries,
            });
        }

        [Fact]
        public void SetWithTokenRegistersForNotification()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, expirationToken);

            Assert.True(expirationToken.HasChangedWasCalled);
            Assert.True(expirationToken.ActiveChangeCallbacksWasCalled);
            Assert.NotNull(expirationToken.Registration);
            Assert.NotNull(expirationToken.Registration.RegisteredCallback);
            Assert.NotNull(expirationToken.Registration.RegisteredState);
            Assert.False(expirationToken.Registration.Disposed);
        }

        [Fact]
        public void SetWithLazyTokenDoesntRegisterForNotification()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));

            Assert.True(expirationToken.HasChangedWasCalled);
            Assert.True(expirationToken.ActiveChangeCallbacksWasCalled);
            Assert.Null(expirationToken.Registration);
        }

        [Fact]
        public void FireTokenRemovesItem()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            expirationToken.Fire();

            var found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void ExpiredLazyTokenRemovesItemOnNextAccess()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            expirationToken.HasChanged = true;

            found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void ExpiredLazyTokenRemovesItemInBackground()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            clock.Add(TimeSpan.FromMinutes(2));
            expirationToken.HasChanged = true;
            var ignored = cache.Get("otherKey"); // Background expiration checks are triggered by misc cache activity.
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            found = cache.TryGetValue(key, out value);
            Assert.False(found);
        }

        [Fact]
        public void RemoveItemDisposesTokenRegistration()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            cache.Remove(key);

            Assert.NotNull(expirationToken.Registration);
            Assert.True(expirationToken.Registration.Disposed);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void ClearingCacheDisposesTokenRegistration()
        {
            var cache = (MemoryCache)CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));
            cache.Clear();

            Assert.Equal(0, cache.Count);
            Assert.NotNull(expirationToken.Registration);
            Assert.True(expirationToken.Registration.Disposed);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void AddExpiredTokenPreventsCaching()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { HasChanged = true };
            var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            Assert.Same(value, result); // The created item should be returned, but not cached.

            Assert.True(expirationToken.HasChangedWasCalled);
            Assert.False(expirationToken.ActiveChangeCallbacksWasCalled);
            Assert.Null(expirationToken.Registration);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            result = cache.Get(key);
            Assert.Null(result); // It wasn't cached
        }

        [Fact]
        public void TokenExpiresOnRegister()
        {
            var cache = CreateCache();
            var key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestToken(callbackInvoked);
            var task = Task.Run(() => cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)));
            callbackInvoked.WaitOne(TimeSpan.FromSeconds(30));
            var result = task.Result;

            Assert.Same(value, result);
            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void PostEvictionCallbacksGetInvokedWhenMemoryCacheEntriesExpireWithAnActiveChangeToken()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var key = new object();

            var cts = new CancellationTokenSource();
            var callbackInvoked = new ManualResetEvent(false);

            cache.Set(key, new object(), new MemoryCacheEntryOptions
            {
                ExpirationTokens = { new CancellationChangeToken(cts.Token) },
                PostEvictionCallbacks =
                {
                    new PostEvictionCallbackRegistration()
                    {
                        EvictionCallback = (key, value, reason, state) => ((ManualResetEvent)state).Set(),
                        State = callbackInvoked
                    }
                }
            });

            Assert.True(cache.TryGetValue(key, out _));

            cts.Cancel();
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.False(cache.TryGetValue(key, out _));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExpirationTokensBehaveLikeAList(bool concurrentReadsEnabled)
        {
            var cache = CreateCache(trackLinkedCacheEntries: concurrentReadsEnabled);
            using ICacheEntry entry = cache.CreateEntry("myKey");
            IList<IChangeToken> tokens = entry.ExpirationTokens;
            entry.SetValue(new object());
            if (concurrentReadsEnabled)
            {
                entry.Dispose();
            }

            var first = new TestExpirationToken();
            var second = new TestExpirationToken();
            var third = new TestExpirationToken();

            Assert.Empty(tokens);
            Assert.False(tokens.IsReadOnly);

            tokens.Add(first);
            tokens.Add(third);
            tokens.Insert(1, second);
            Assert.Equal(new[] { first, second, third }, tokens);
            Assert.Equal(3, tokens.Count);

            Assert.Same(second, tokens[1]);
            Assert.Equal(2, tokens.IndexOf(third));
            Assert.True(tokens.Contains(second));
            Assert.Throws<ArgumentOutOfRangeException>(() => tokens[3]);
            Assert.Throws<ArgumentOutOfRangeException>(() => tokens.Insert(4, first));
            Assert.Throws<ArgumentOutOfRangeException>(() => tokens.RemoveAt(3));

            var target = new IChangeToken[4];
            tokens.CopyTo(target, 1);
            Assert.Equal(new IChangeToken[] { null, first, second, third }, target);

            tokens[0] = third;
            Assert.Same(third, tokens[0]);

            Assert.True(tokens.Remove(second));
            Assert.False(tokens.Remove(second));
            Assert.Equal(new[] { third, third }, tokens);

            tokens.RemoveAt(0);
            Assert.Same(third, Assert.Single(tokens));

            tokens.Clear();
            Assert.Empty(tokens);

            tokens.Add(first);
            Assert.Same(first, Assert.Single(tokens));
            tokens.Clear();
            Assert.Empty(tokens);
        }

        [Theory]
        [InlineData(1, false)] // append into spare capacity while building
        [InlineData(4, false)] // grow while building
        [InlineData(1, true)] // append into spare capacity with concurrent reads
        [InlineData(4, true)] // grow with concurrent reads
        public void ExpirationTokensEnumeratorUsesSnapshotWhileAdding(int initialCount, bool concurrentReadsEnabled)
        {
            var cache = CreateCache(trackLinkedCacheEntries: concurrentReadsEnabled);
            using ICacheEntry entry = cache.CreateEntry("myKey");
            var expected = new List<IChangeToken>(initialCount);

            for (int i = 0; i < initialCount; i++)
            {
                var token = new TestExpirationToken();
                expected.Add(token);
                entry.AddExpirationToken(token);
            }

            entry.SetValue(new object());
            if (concurrentReadsEnabled)
            {
                entry.Dispose();
            }

            using IEnumerator<IChangeToken> enumerator = entry.ExpirationTokens.GetEnumerator();
            Assert.True(enumerator.MoveNext()); // captures the current state and visible count

            var actual = new List<IChangeToken>(initialCount) { enumerator.Current };
            entry.AddExpirationToken(new TestExpirationToken());

            while (enumerator.MoveNext())
            {
                actual.Add(enumerator.Current);
            }

            Assert.Equal(expected, actual);
            Assert.Equal(initialCount + 1, entry.ExpirationTokens.Count);
        }

        internal class TestToken : IChangeToken
        {
            private bool _hasChanged;
            private ManualResetEvent _event;

            public TestToken(ManualResetEvent mre)
            {
                _event = mre;
            }

            public bool ActiveChangeCallbacks
            {
                get
                {
                    return true;
                }
            }

            public bool HasChanged
            {
                get
                {
                    return _hasChanged;
                }
            }

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                _hasChanged = true;
                callback(state);
                _event.Set();
                return new TestDisposable();
            }
        }

        internal class TestDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
