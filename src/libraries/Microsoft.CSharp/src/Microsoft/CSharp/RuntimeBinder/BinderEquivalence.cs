// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System;

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

        // Keep a separate cache per ALC to allow unloadability.
        private static readonly ConditionalWeakTable<AssemblyLoadContext,
            ConcurrentDictionary<ICSharpBinder, ICSharpBinder>> binderEquivalenceCache =
            new ConditionalWeakTable<AssemblyLoadContext, ConcurrentDictionary<ICSharpBinder, ICSharpBinder>>();

        internal static T TryGetExisting<T>(this T binder, Type context)
            where T : ICSharpBinder
        {
            var alc = AssemblyLoadContext.GetLoadContext(context.Assembly);
            if (alc is null)
            {
                // In the rare case the type is not a runtime type, don't cache the binder.
                return binder;
            }

            var cache = binderEquivalenceCache.GetValue(alc, _ =>
                // it is unlikely to see a lot of contention on the binder cache.
                // creating binders is not a very frequent operation.
                // typically a dynamic operation in the source will create just one binder lazily when first executed.
                new ConcurrentDictionary<ICSharpBinder, ICSharpBinder>(concurrencyLevel: 2, capacity: 32, new BinderEqualityComparer()));
            var fromCache = cache.GetOrAdd(binder, binder);
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
