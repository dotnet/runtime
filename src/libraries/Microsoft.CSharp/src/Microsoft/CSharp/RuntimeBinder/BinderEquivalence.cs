// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class BinderEquivalence
    {
        // An upper bound on the size of binder cache.
        // We do not need to cache all the binders, 4K should be enough for most cases.
        //
        // For the perspective: the dynamic testsuite has a lot of dynamic operations,
        // but ends up needing only ~500 binders once caching is enabled.
        //
        // NOTE:
        //    typical C# binders, once created, are rooted to their callsites, which are stored in static fields.
        //    the cache is unlikely to extend the life time of the binders.
        //    the limit here is just to have assurance on how large the cache may get.
        private const uint BINDER_CACHE_LIMIT = 4096;

        // keep a separate count because it is cheaper than calling CD.Count()
        // it does not need to be very precise either
        private static int cachedBinderCount;

        // it is unlikely to see a lot of contention on the binder cache.
        // creating binders is not a very frequent operation.
        // typically a dynamic operation in the source will create just one binder lazily when first executed.
        private static readonly ConcurrentDictionary<ICSharpBinder, ICSharpBinder> binderEquivalenceCache =
            new ConcurrentDictionary<ICSharpBinder, ICSharpBinder>(concurrencyLevel: 2, capacity: 32, new BinderEqualityComparer());

        internal static T TryGetExisting<T>(this T binder)
            where T : ICSharpBinder
        {
            var fromCache = binderEquivalenceCache.GetOrAdd(binder, binder);
            if (fromCache == (object)binder)
            {
                var count = Interlocked.Increment(ref cachedBinderCount);

                // a simple eviction policy -
                // if cache grows too big, just flush it and start over.
                if ((uint)count > BINDER_CACHE_LIMIT)
                {
                    binderEquivalenceCache.Clear();
                    cachedBinderCount = 0;
                }
            }

            return (T)fromCache;
        }

        internal sealed class BinderEqualityComparer : IEqualityComparer<ICSharpBinder>
        {
            public bool Equals(ICSharpBinder x, ICSharpBinder y) => x.IsEquivalentTo(y);
            public int GetHashCode(ICSharpBinder obj) => obj.GetGetBinderEquivalenceHash();
        }
    }
}
