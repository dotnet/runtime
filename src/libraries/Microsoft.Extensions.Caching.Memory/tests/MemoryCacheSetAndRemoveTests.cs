// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheSetAndRemoveTests
    {
        private static IMemoryCache CreateCache(bool trackLinkedCacheEntries = false)
        {
            return new MemoryCache(new MemoryCacheOptions { TrackLinkedCacheEntries = trackLinkedCacheEntries });
        }

        [Fact]
        public void GetMissingKeyReturnsFalseOrNull()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Get(key);
            Assert.Null(result);

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
        }

        [Fact]
        public void SetAndGetReturnsObject()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            result = cache.Get(key);
            Assert.Same(obj, result);
        }

        [Fact]
        public void SetAndGetWorksWithCaseSensitiveKeys()
        {
            var cache = CreateCache();
            var obj = new object();
            string key1 = "myKey";
            string key2 = "Mykey";

            var result = cache.Set(key1, obj);
            Assert.Same(obj, result);

            result = cache.Get(key1);
            Assert.Same(obj, result);

            result = cache.Get(key2);
            Assert.Null(result);
        }

        [Fact]
        public void SetAlwaysOverwrites()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            var obj2 = new object();
            result = cache.Set(key, obj2);
            Assert.Same(obj2, result);

            result = cache.Get(key);
            Assert.Same(obj2, result);
        }

        [Fact]
        public void GetOrCreate_AddsNewValue()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            bool invoked = false;

            var result = cache.GetOrCreate(key, e =>
            {
                invoked = true;
                return obj;
            });

            Assert.Same(obj, result);
            Assert.True(invoked);

            result = cache.Get(key);
            Assert.Same(obj, result);
        }

        [Fact]
        public async Task GetOrCreateAsync_AddsNewValue()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            bool invoked = false;

            var result = await cache.GetOrCreateAsync(key, e =>
            {
                invoked = true;
                return Task.FromResult(obj);
            });

            Assert.Same(obj, result);
            Assert.True(invoked);

            result = cache.Get(key);
            Assert.Same(obj, result);
        }

        [Fact]
        public void GetOrCreate_ReturnExistingValue()
        {
            var cache = CreateCache();
            var obj = new object();
            var obj1 = new object();
            string key = "myKey";
            bool invoked = false;

            cache.Set(key, obj);

            var result = cache.GetOrCreate(key, e =>
            {
                invoked = true;
                return obj1;
            });

            Assert.False(invoked);
            Assert.Same(obj, result);
        }

        [Fact]
        public async Task GetOrCreateAsync_ReturnExistingValue()
        {
            var cache = CreateCache();
            var obj = new object();
            var obj1 = new object();
            string key = "myKey";
            bool invoked = false;

            cache.Set(key, obj);

            var result = await cache.GetOrCreateAsync(key, e =>
            {
                invoked = true;
                return Task.FromResult(obj1);
            });

            Assert.False(invoked);
            Assert.Same(obj, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetOrCreate_WillNotCreateEmptyValue_WhenFactoryThrows(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            string key = "myKey";
            try
            {
                cache.GetOrCreate<int>(key, entry =>
                {
                    throw new Exception();
                });
            }
            catch (Exception)
            {
            }

            Assert.False(cache.TryGetValue(key, out int obj));

            // verify that throwing an exception doesn't leak CacheEntry objects
            Assert.Null(CacheEntry.Current);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetOrCreateAsync_WillNotCreateEmptyValue_WhenFactoryThrows(bool trackLinkedCacheEntries)
        {
            var cache = CreateCache(trackLinkedCacheEntries);
            string key = "myKey";
            try
            {
                await cache.GetOrCreateAsync<int>(key, entry =>
                {
                    throw new Exception();
                });
            }
            catch (Exception)
            {
            }

            Assert.False(cache.TryGetValue(key, out int obj));

            // verify that throwing an exception doesn't leak CacheEntry objects
            Assert.Null(CacheEntry.Current);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DisposingCacheEntryReleasesScope(bool trackLinkedCacheEntries)
        {
            object GetScope(ICacheEntry entry)
            {
                // Use Type.GetType so that trimming can know what type we operate on
                Type cacheEntryType = Type.GetType("Microsoft.Extensions.Caching.Memory.CacheEntry, Microsoft.Extensions.Caching.Memory");
                Assert.Equal(cacheEntryType, entry.GetType());
                return cacheEntryType
                    .GetField("_previous", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(entry);
            }

            var cache = CreateCache(trackLinkedCacheEntries);

            ICacheEntry first = cache.CreateEntry("myKey1");
            Assert.Null(GetScope(first)); // it's the first entry, so it has no previous cache entry set

            ICacheEntry second = cache.CreateEntry("myKey2");

            if (trackLinkedCacheEntries)
            {
                Assert.NotNull(GetScope(second)); // it's not first, so it has previous set
                Assert.Same(first, GetScope(second)); // second.previous is set to first

                second.Dispose();
                Assert.Null(GetScope(second));
                first.Dispose();
                Assert.Null(GetScope(first));
            }
            else
            {
                Assert.Null(GetScope(second)); // tracking not enabled, the scope is null
            }
        }

        [Fact]
        public void TryGetValue_WillCreateDefaultValue_WhenGenericTypeIsIncompatible()
        {
            var cache = CreateCache();
            string key = "myKey";
            int value = 42;

            cache.Set(key, value);

            Assert.False(cache.TryGetValue(key, out string obj));
        }


        [Fact]
        public void TryGetValue_WillCreateDefaultValueAndSucceed_WhenValueNull()
        {
            var cache = CreateCache();
            string key = "myKey";
            string value = null;

            cache.Set(key, value);

            Assert.True(cache.TryGetValue(key, out string obj));
        }

        [Fact]
        public void TryGetValue_WillCreateDefaultValueAndSucceed_WhenValueNullForValueType()
        {
            var cache = CreateCache();
            string key = "myKey";
            string value = null;

            cache.Set(key, value);

            Assert.True(cache.TryGetValue(key, out int obj));
            Assert.Equal(default, obj);
        }

        [Fact]
        public void SetOverwritesAndInvokesCallbacks()
        {
            var cache = CreateCache();
            var value1 = new object();
            string key = "myKey";
            var callback1Invoked = new ManualResetEvent(false);
            var callback2Invoked = new ManualResetEvent(false);

            var options1 = new MemoryCacheEntryOptions();
            options1.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(subValue, value1);
                    Assert.Equal(EvictionReason.Replaced, reason);
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                },
                State = callback1Invoked
            });

            var result = cache.Set(key, value1, options1);
            Assert.Same(value1, result);

            var value2 = new object();
            var options2 = new MemoryCacheEntryOptions();
            options2.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    // Shouldn't be invoked.
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                },
                State = callback2Invoked
            });
            result = cache.Set(key, value2, options2);
            Assert.Same(value2, result);
            Assert.True(callback1Invoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback1");
            Assert.False(callback2Invoked.WaitOne(TimeSpan.FromSeconds(1)), "Callback2");

            result = cache.Get(key);
            Assert.Same(value2, result);

            Assert.False(callback2Invoked.WaitOne(TimeSpan.FromSeconds(1)), "Callback2");
        }

        [Fact]
        public void SetOverwritesWithReplacedReason()
        {
            var cache = CreateCache();
            var value1 = new object();
            string key = "myKey";
            var callback1Invoked = new ManualResetEvent(false);
            EvictionReason actualReason = EvictionReason.None;

            var options1 = new MemoryCacheEntryOptions();
            options1.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    actualReason = reason;
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                },
                State = callback1Invoked
            });

            var result = cache.Set(key, value1, options1);
            Assert.Same(value1, result);

            var value2 = new object();
            result = cache.Set(key, value2);

            Assert.True(callback1Invoked.WaitOne(TimeSpan.FromSeconds(3)), "Callback1");
            Assert.Equal(EvictionReason.Replaced, actualReason);
        }

        [Fact]
        public void RemoveRemoves()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            cache.Remove(key);
            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void ClearClears()
        {
            var cache = (MemoryCache)CreateCache();
            var obj = new object();
            string[] keys = new string[] { "key1", "key2", "key3", "key4" };

            foreach (string key in keys)
            {
                var result = cache.Set(key, obj);
                Assert.Same(obj, result);
                Assert.Same(obj, cache.Get(key));
            }

            cache.Clear();

            Assert.Equal(0, cache.Count);
            foreach (string key in keys)
            {
                Assert.Null(cache.Get(key));
            }
        }

        [Fact]
        public void SetNullCallback_NotAllowed_ArgumentException()
        {
            var cache = CreateCache();
            const string someKey = "test";
            var entry = cache.CreateEntry(someKey);

            var options = new MemoryCacheEntryOptions();

            var notNullCallback = new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (_, _, _, _) => {}
            };

            options.PostEvictionCallbacks.Add(notNullCallback);

            var nullCallback = new PostEvictionCallbackRegistration();

            options.PostEvictionCallbacks.Add(nullCallback);

            Assert.Throws<ArgumentException>(() => entry.SetOptions(options));
        }

        [Fact]
        public void RemoveRemovesAndInvokesCallback()
        {
            var cache = CreateCache();
            var value = new object();
            string key = "myKey";
            var callbackInvoked = new ManualResetEvent(false);

            var options = new MemoryCacheEntryOptions();
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(value, subValue);
                    Assert.Equal(EvictionReason.Removed, reason);
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                },
                State = callbackInvoked
            });
            var result = cache.Set(key, value, options);
            Assert.Same(value, result);

            cache.Remove(key);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void ClearClearsAndInvokesCallback()
        {
            var cache = (MemoryCache)CreateCache();
            var value = new object();
            string key = "myKey";
            var callbackInvoked = new ManualResetEvent(false);

            var options = new MemoryCacheEntryOptions();
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(value, subValue);
                    Assert.Equal(EvictionReason.Removed, reason);
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                },
                State = callbackInvoked
            });
            var result = cache.Set(key, value, options);
            Assert.Same(value, result);

            cache.Clear();
            Assert.Equal(0, cache.Count);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void RemoveAndReAddFromCallbackWorks()
        {
            var cache = CreateCache();
            var value = new object();
            var obj2 = new object();
            string key = "myKey";
            var callbackInvoked = new ManualResetEvent(false);

            var options = new MemoryCacheEntryOptions();
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (subkey, subValue, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(subValue, value);
                    Assert.Equal(EvictionReason.Removed, reason);
                    var localCallbackInvoked = (ManualResetEvent)state;
                    cache.Set(key, obj2);
                    localCallbackInvoked.Set();
                },
                State = callbackInvoked
            });

            var result = cache.Set(key, value, options);
            Assert.Same(value, result);

            cache.Remove(key);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            result = cache.Get(key);
            Assert.Same(obj2, result);
        }

        [Fact]
        public void SetGetAndRemoveWorksWithNonStringKeys()
        {
            var cache = CreateCache();
            var obj = new object();
            var key = new Person { Id = 10, Name = "Mike" };

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            result = cache.Get(key);
            Assert.Same(obj, result);

            cache.Remove(key);
            result = cache.Get(key);
            Assert.Null(result);
        }

        private class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void SetGetAndRemoveWorksWithObjectKeysWhenDifferentReferences()
        {
            var cache = CreateCache();
            var obj = new object();

            var result = cache.Set(new TestKey(), obj);
            Assert.Same(obj, result);

            result = cache.Get(new TestKey());
            Assert.Same(obj, result);

            var key = new TestKey();
            cache.Remove(key);
            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72879")] // issue in cache
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72890")] // issue in test
        public void GetAndSet_AreThreadSafe_AndUpdatesNeverLeavesNullValues()
        {
            var cache = CreateCache();
            string key = "myKey";
            var cts = new CancellationTokenSource();
            var readValueIsNull = false;

            cache.Set(key, new Guid());

            var task0 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    cache.Set(key, Guid.NewGuid());
                }
            });

            var task1 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    cache.Set(key, Guid.NewGuid());
                }
            });

            var task2 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (cache.Get(key) == null)
                    {
                        // Stop this task and update flag for assertion
                        readValueIsNull = true;
                        break;
                    }
                }
            });

            var task3 = Task.Delay(TimeSpan.FromSeconds(7));

            Task.WaitAny(task0, task1, task2, task3);

            Assert.False(readValueIsNull);
            Assert.Equal(TaskStatus.Running, task0.Status);
            Assert.Equal(TaskStatus.Running, task1.Status);
            Assert.Equal(TaskStatus.Running, task2.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task3.Status);

            cts.Cancel();
            Task.WaitAll(task0, task1, task2, task3);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72890")]
        public void OvercapacityPurge_AreThreadSafe()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.Zero,
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });
            var cts = new CancellationTokenSource();
            var limitExceeded = false;

            var task0 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (cache.Size > 10)
                    {
                        limitExceeded = true;
                        break;
                    }
                    cache.Set(Guid.NewGuid(), Guid.NewGuid(), new MemoryCacheEntryOptions { Size = 1 });
                }
            }, cts.Token);

            var task1 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (cache.Size > 10)
                    {
                        limitExceeded = true;
                        break;
                    }
                    cache.Set(Guid.NewGuid(), Guid.NewGuid(), new MemoryCacheEntryOptions { Size = 1 });
                }
            }, cts.Token);

            var task2 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (cache.Size > 10)
                    {
                        limitExceeded = true;
                        break;
                    }
                    cache.Set(Guid.NewGuid(), Guid.NewGuid(), new MemoryCacheEntryOptions { Size = 1 });
                }
            }, cts.Token);

            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var task3 = Task.Delay(TimeSpan.FromSeconds(7));

            Task.WaitAll(task0, task1, task2, task3);

            Assert.Equal(TaskStatus.RanToCompletion, task0.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task2.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task3.Status);
            CapacityTests.AssertCacheSize(cache.Count, cache);
            Assert.InRange(cache.Count, 0, 10);
            Assert.False(limitExceeded);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72890")]
        public void AddAndReplaceEntries_AreThreadSafe()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.Zero,
                SizeLimit = 20,
                CompactionPercentage = 0.5
            });
            var cts = new CancellationTokenSource();

            var random = new Random();

            var task0 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var entrySize = random.Next(0, 5);
                    cache.Set(random.Next(0, 10), entrySize, new MemoryCacheEntryOptions { Size = entrySize });
                }
            });

            var task1 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var entrySize = random.Next(0, 5);
                    cache.Set(random.Next(0, 10), entrySize, new MemoryCacheEntryOptions { Size = entrySize });
                }
            });

            var task2 = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var entrySize = random.Next(0, 5);
                    cache.Set(random.Next(0, 10), entrySize, new MemoryCacheEntryOptions { Size = entrySize });
                }
            });

            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var task3 = Task.Delay(TimeSpan.FromSeconds(7));

            Task.WaitAll(task0, task1, task2, task3);

            Assert.Equal(TaskStatus.RanToCompletion, task0.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task2.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task3.Status);

            var cacheSize = 0;
            for (var i = 0; i < 10; i++)
            {
                cacheSize += cache.Get<int>(i);
            }

            CapacityTests.AssertCacheSize(cacheSize, cache);
            Assert.InRange(cache.Count, 0, 20);
        }

        [Fact]
        public void GetDataFromCacheWithNullKeyThrows()
        {
            var cache = CreateCache();
            Assert.Throws<ArgumentNullException>(() => cache.Get(null));
        }

        [Fact]
        public void SetDataToCacheWithNullKeyThrows()
        {
            var cache = CreateCache();
            var value = new object();
            Assert.Throws<ArgumentNullException>(() => cache.Set(null, value));
        }

        [Fact]
        public void SetDataToCacheWithNullKeyAndChangeTokenThrows()
        {
            var cache = CreateCache();
            var value = new object();
            Assert.Throws<ArgumentNullException>(() => cache.Set(null, value, expirationToken: null));
        }

        [Fact]
        public void TryGetValueFromCacheWithNullKeyThrows()
        {
            var cache = CreateCache();
            Assert.Throws<ArgumentNullException>(() => cache.TryGetValue(null,out long result));
        }

        [Fact]
        public void GetOrCreateFromCacheWithNullKeyThrows()
        {
            var cache = CreateCache();
            Assert.Throws<ArgumentNullException>(() => cache.GetOrCreate<object>(null, null))
;       }

        [Fact]
        public async Task GetOrCreateAsyncFromCacheWithNullKeyThrows()
        {
            var cache = CreateCache();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.GetOrCreateAsync<object>(null, null));
        }

        [Fact]
        public void GetOrCreateWithCacheEntryOptions()
        {
            var cacheKey = "test";
            var cache = CreateCache();
            ManualResetEvent mre = new ManualResetEvent(false);

            var options = new MemoryCacheEntryOptions();
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (key, value, reason, state) =>
                {
                    Assert.Equal(cacheKey, key);
                    Assert.Equal(cacheKey, value);
                    Assert.Equal(EvictionReason.Removed, reason);
                    mre.Set();
                }
            });

            var value = cache.GetOrCreate<string>(cacheKey, _ => cacheKey, options);
            Assert.Equal(cacheKey, value);
            Assert.True(cache.TryGetValue(cacheKey, out _));

            cache.Remove(cacheKey);
            Assert.True(mre.WaitOne(TimeSpan.FromSeconds(30)));
            Assert.False(cache.TryGetValue(cacheKey, out _));
        }

        [Fact]
        public async Task GetOrCreateAsyncWithCacheEntryOptions()
        {
            var cacheKey = "test";
            var cache = CreateCache();
            ManualResetEvent mre = new ManualResetEvent(false);

            var options = new MemoryCacheEntryOptions();
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = (key, value, reason, state) =>
                {
                    Assert.Equal(cacheKey, key);
                    Assert.Equal(cacheKey, value);
                    Assert.Equal(EvictionReason.Removed, reason);
                    mre.Set();
                }
            });

            var value = await cache.GetOrCreateAsync<string>(cacheKey, _ => Task.FromResult(cacheKey), options);
            Assert.Equal(cacheKey, value);
            Assert.True(cache.TryGetValue(cacheKey, out _));

            cache.Remove(cacheKey);
            Assert.True(mre.WaitOne(TimeSpan.FromSeconds(30)));
            Assert.False(cache.TryGetValue(cacheKey, out _));
        }

        private class TestKey
        {
            public override bool Equals(object obj) => true;
            public override int GetHashCode() => 0;
        }
    }
}
