using System;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory.Tests
{
    public class AltLookupTests
    {
        [Fact]
        public void SimpleStringAccess()
        {
            // create a cache with unrelated other values
            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set("unrelated key", new object());
            cache.Set(Guid.NewGuid(), new object());

            // our key, in various disguises
            string stringKey = "my_key";
            object objKey = stringKey;
            ReadOnlySpan<char> stringSpanKey = stringKey.AsSpan();
            // in the case of span, just to be super rigorous, we
            // will also check an isolated span that isn't an interior pointer to a string
            Span<char> isolated = stackalloc char[stringKey.Length];
            stringKey.AsSpan().CopyTo(isolated);
            ReadOnlySpan<char> isolatedSpanKey = isolated;

            // not found initially (before added)
            Assert.False(cache.TryGetValue(stringKey, out object? result));
            Assert.Null(result);

            Assert.False(cache.TryGetValue(objKey, out result));
            Assert.Null(result);

#if NET9_0_OR_GREATER
            Assert.False(cache.TryGetValue(stringSpanKey, out result));
            Assert.Null(result);

            Assert.False(cache.TryGetValue(isolatedSpanKey, out result));
            Assert.Null(result);
#endif

            // found after adding
            object cachedValue = new();
            cache.Set(stringKey, cachedValue);

            Assert.True(cache.TryGetValue(stringKey, out result));
            Assert.Same(cachedValue, result);

            Assert.True(cache.TryGetValue(objKey, out result));
            Assert.Same(cachedValue, result);

#if NET9_0_OR_GREATER
            Assert.True(cache.TryGetValue(stringSpanKey, out result));
            Assert.Same(cachedValue, result);

            Assert.True(cache.TryGetValue(isolatedSpanKey, out result));
            Assert.Same(cachedValue, result);
#endif

            // not found after removing
            cache.Remove(stringKey);

            Assert.False(cache.TryGetValue(stringKey, out result));
            Assert.Null(result);

            Assert.False(cache.TryGetValue(objKey, out result));
            Assert.Null(result);

#if NET9_0_OR_GREATER
            Assert.False(cache.TryGetValue(stringSpanKey, out result));
            Assert.Null(result);

            Assert.False(cache.TryGetValue(isolatedSpanKey, out result));
            Assert.Null(result);
#endif
        }
    }
}
