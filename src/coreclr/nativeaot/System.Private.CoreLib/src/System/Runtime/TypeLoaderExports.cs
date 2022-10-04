// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Runtime.Augments;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime
{
    [ReflectionBlocked]
    public static class TypeLoaderExports
    {
        public static unsafe void ActivatorCreateInstanceAny(ref object ptrToData, IntPtr pEETypePtr)
        {
            EETypePtr pEEType = new EETypePtr(pEETypePtr);

            if (pEEType.IsValueType)
            {
                // Nothing else to do for value types.
                return;
            }

            // For reference types, we need to:
            //  1- Allocate the new object
            //  2- Call its default ctor
            //  3- Update ptrToData to point to that newly allocated object
            ptrToData = RuntimeImports.RhNewObject(pEEType);

            Entry entry = LookupInCache(s_cache, pEETypePtr, pEETypePtr);
            entry ??= CacheMiss(pEETypePtr, pEETypePtr,
                    (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult) =>
                    {
                        IntPtr result = RuntimeAugments.TypeLoaderCallbacks.TryGetDefaultConstructorForType(new RuntimeTypeHandle(new EETypePtr(context)));
                        if (result == IntPtr.Zero)
                            result = RuntimeAugments.GetFallbackDefaultConstructor();
                        return result;
                    });
            RawCalliHelper.Call(entry.Result, ptrToData);
        }

        //
        // Generic lookup cache
        //

        private class Entry
        {
            public IntPtr Context;
            public IntPtr Signature;
            public IntPtr Result;
            public IntPtr AuxResult;
            public Entry Next;
        }

        // Initialize the cache eagerly to avoid null checks.
        // Use array with just single element to make this pay-for-play. The actual cache will be allocated only
        // once the lazy lookups are actually needed.
        private static Entry[] s_cache;

        private static Lock s_lock;
        private static GCHandle s_previousCache;

        internal static void Initialize()
        {
            s_cache = new Entry[1];
        }

        public static IntPtr GenericLookup(IntPtr context, IntPtr signature)
        {
            Entry entry = LookupInCache(s_cache, context, signature);
            entry ??= CacheMiss(context, signature);
            return entry.Result;
        }

        public static void GenericLookupAndCallCtor(object arg, IntPtr context, IntPtr signature)
        {
            Entry entry = LookupInCache(s_cache, context, signature);
            entry ??= CacheMiss(context, signature);
            RawCalliHelper.Call(entry.Result, arg);
        }

        public static object GenericLookupAndAllocObject(IntPtr context, IntPtr signature)
        {
            Entry entry = LookupInCache(s_cache, context, signature);
            entry ??= CacheMiss(context, signature);
            return RawCalliHelper.Call<object>(entry.Result, entry.AuxResult);
        }

        public static object GenericLookupAndAllocArray(IntPtr context, IntPtr arg, IntPtr signature)
        {
            Entry entry = LookupInCache(s_cache, context, signature);
            entry ??= CacheMiss(context, signature);
            return RawCalliHelper.Call<object>(entry.Result, entry.AuxResult, arg);
        }

        public static void GenericLookupAndCheckArrayElemType(IntPtr context, object arg, IntPtr signature)
        {
            Entry entry = LookupInCache(s_cache, context, signature);
            entry ??= CacheMiss(context, signature);
            RawCalliHelper.Call(entry.Result, entry.AuxResult, arg);
        }

        public static object GenericLookupAndCast(object arg, IntPtr context, IntPtr signature)
        {
            Entry entry = LookupInCache(s_cache, context, signature);
            entry ??= CacheMiss(context, signature);
            return RawCalliHelper.Call<object>(entry.Result, arg, entry.AuxResult);
        }

        public static IntPtr UpdateTypeFloatingDictionary(IntPtr eetypePtr, IntPtr dictionaryPtr)
        {
            // No caching needed. Update is in-place, and happens once per dictionary
            return RuntimeAugments.TypeLoaderCallbacks.UpdateFloatingDictionary(eetypePtr, dictionaryPtr);
        }

        public static IntPtr UpdateMethodFloatingDictionary(IntPtr dictionaryPtr)
        {
            // No caching needed. Update is in-place, and happens once per dictionary
            return RuntimeAugments.TypeLoaderCallbacks.UpdateFloatingDictionary(dictionaryPtr, dictionaryPtr);
        }

#if FEATURE_UNIVERSAL_GENERICS
        public static unsafe IntPtr GetDelegateThunk(object delegateObj, int whichThunk)
        {
            Entry entry = LookupInCache(s_cache, (IntPtr)delegateObj.GetMethodTable(), new IntPtr(whichThunk));
            if (entry == null)
            {
                entry = CacheMiss((IntPtr)delegateObj.GetMethodTable(), new IntPtr(whichThunk),
                    (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult)
                        => RuntimeAugments.TypeLoaderCallbacks.GetDelegateThunk((Delegate)contextObject, (int)signature),
                    delegateObj);
            }
            return entry.Result;
        }
#endif

        public static unsafe IntPtr GVMLookupForSlot(object obj, RuntimeMethodHandle slot)
        {
            Entry entry = LookupInCache(s_cache, (IntPtr)obj.GetMethodTable(), *(IntPtr*)&slot);
            entry ??= CacheMiss((IntPtr)obj.GetMethodTable(), *(IntPtr*)&slot,
                    (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult)
                        => Internal.Runtime.CompilerServices.GenericVirtualMethodSupport.GVMLookupForSlot(new RuntimeTypeHandle(new EETypePtr(context)), *(RuntimeMethodHandle*)&signature));
            return entry.Result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe IntPtr OpenInstanceMethodLookup(IntPtr openResolver, object obj)
        {
            Entry entry = LookupInCache(s_cache, (IntPtr)obj.GetMethodTable(), openResolver);
            entry ??= CacheMiss((IntPtr)obj.GetMethodTable(), openResolver,
                    (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult)
                        => Internal.Runtime.CompilerServices.OpenMethodResolver.ResolveMethodWorker(signature, contextObject),
                    obj);
            return entry.Result;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static Entry LookupInCache(Entry[] cache, IntPtr context, IntPtr signature)
        {
            int key = ((context.GetHashCode() >> 4) ^ signature.GetHashCode()) & (cache.Length - 1);
            Entry entry = cache[key];
            while (entry != null)
            {
                if (entry.Context == context && entry.Signature == signature)
                    break;
                entry = entry.Next;
            }
            return entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IntPtr RuntimeCacheLookupInCache(IntPtr context, IntPtr signature, RuntimeObjectFactory factory, object contextObject, out IntPtr auxResult)
        {
            Entry entry = LookupInCache(s_cache, context, signature);
            entry ??= CacheMiss(context, signature, factory, contextObject);
            auxResult = entry.AuxResult;
            return entry.Result;
        }

        private static Entry CacheMiss(IntPtr ctx, IntPtr sig)
        {
            return CacheMiss(ctx, sig,
                (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult) =>
                    RuntimeAugments.TypeLoaderCallbacks.GenericLookupFromContextAndSignature(context, signature, out auxResult)
                );
        }

        private static unsafe Entry CacheMiss(IntPtr context, IntPtr signature, RuntimeObjectFactory factory, object contextObject = null)
        {
            IntPtr result = IntPtr.Zero, auxResult = IntPtr.Zero;
            bool previouslyCached = false;

            //
            // Try to find the entry in the previous version of the cache that is kept alive by weak reference
            //
            if (s_previousCache.IsAllocated)
            {
                Entry[]? previousCache = (Entry[]?)s_previousCache.Target;
                if (previousCache != null)
                {
                    Entry previousEntry = LookupInCache(previousCache, context, signature);
                    if (previousEntry != null)
                    {
                        result = previousEntry.Result;
                        auxResult = previousEntry.AuxResult;
                        previouslyCached = true;
                    }
                }
            }

            //
            // Call into the type loader to compute the target
            //
            if (!previouslyCached)
            {
                result = factory(context, signature, contextObject, ref auxResult);
            }

            //
            // Update the cache under the lock
            //
            if (s_lock == null)
                Interlocked.CompareExchange(ref s_lock, new Lock(), null);

            s_lock.Acquire();
            try
            {
                // Avoid duplicate entries
                Entry existingEntry = LookupInCache(s_cache, context, signature);
                if (existingEntry != null)
                    return existingEntry;

                // Resize cache as necessary
                Entry[] cache = ResizeCacheForNewEntryAsNecessary();

                int key = ((context.GetHashCode() >> 4) ^ signature.GetHashCode()) & (cache.Length - 1);

                Entry newEntry = new Entry() { Context = context, Signature = signature, Result = result, AuxResult = auxResult, Next = cache[key] };
                cache[key] = newEntry;
                return newEntry;
            }
            finally
            {
                s_lock.Release();
            }
        }

        //
        // Parameters and state used by generic lookup cache resizing algorithm
        //

        private const int InitialCacheSize = 128; // MUST BE A POWER OF TWO
        private const int DefaultCacheSize = 1024;
        private const int MaximumCacheSize = 128 * 1024;

        private static long s_tickCountOfLastOverflow;
        private static int s_entries;
        private static bool s_roundRobinFlushing;

        private static Entry[] ResizeCacheForNewEntryAsNecessary()
        {
            Entry[] cache = s_cache;

            if (cache.Length < InitialCacheSize)
            {
                // Start with small cache size so that the cache entries used by startup one-time only initialization will get flushed soon
                return s_cache = new Entry[InitialCacheSize];
            }

            int entries = s_entries++;

            // If the cache has spare space, we are done
            if (2 * entries < cache.Length)
            {
                if (s_roundRobinFlushing)
                {
                    cache[2 * entries] = null;
                    cache[2 * entries + 1] = null;
                }
                return cache;
            }

            //
            // Now, we have cache that is overflowing with the stuff. We need to decide whether to resize it or start flushing the old entries instead
            //

            // Start over counting the entries
            s_entries = 0;

            // See how long it has been since the last time the cache was overflowing
            long tickCount = Environment.TickCount64;
            long tickCountSinceLastOverflow = tickCount - s_tickCountOfLastOverflow;
            s_tickCountOfLastOverflow = tickCount;

            bool shrinkCache = false;
            bool growCache = false;

            if (cache.Length < DefaultCacheSize)
            {
                // If the cache have not reached the default size, just grow it without thinking about it much
                growCache = true;
            }
            else
            {
                if (tickCountSinceLastOverflow < cache.Length / 128)
                {
                    // If the fill rate of the cache is faster than ~0.01ms per entry, grow it
                    if (cache.Length < MaximumCacheSize)
                        growCache = true;
                }
                else
                if (tickCountSinceLastOverflow > cache.Length * 16)
                {
                    // If the fill rate of the cache is slower than 16ms per entry, shrink it
                    if (cache.Length > DefaultCacheSize)
                        shrinkCache = true;
                }
                // Otherwise, keep the current size and just keep flushing the entries round robin
            }

            if (growCache || shrinkCache)
            {
                s_roundRobinFlushing = false;

                // Keep the reference to the old cache in a weak handle. We will try to use to avoid
                // hitting the type loader until GC collects it.
                if (s_previousCache.IsAllocated)
                {
                    s_previousCache.Target = cache;
                }
                else
                {
                    s_previousCache = GCHandle.Alloc(cache, GCHandleType.Weak);
                }

                return s_cache = new Entry[shrinkCache ? (cache.Length / 2) : (cache.Length * 2)];
            }
            else
            {
                s_roundRobinFlushing = true;
                return cache;
            }
        }
    }

    [ReflectionBlocked]
    public delegate IntPtr RuntimeObjectFactory(IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult);

    internal static unsafe class RawCalliHelper
    {
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void Call(System.IntPtr pfn, ref byte data)
            => ((delegate*<ref byte, void>)pfn)(ref data);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, IntPtr arg)
            => ((delegate*<IntPtr, T>)pfn)(arg);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void Call(System.IntPtr pfn, object arg)
            => ((delegate*<object, void>)pfn)(arg);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, IntPtr arg1, IntPtr arg2)
            => ((delegate*<IntPtr, IntPtr, T>)pfn)(arg1, arg2);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, IntPtr arg1, IntPtr arg2, object arg3, out IntPtr arg4)
            => ((delegate*<IntPtr, IntPtr, object, out IntPtr, T>)pfn)(arg1, arg2, arg3, out arg4);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void Call(System.IntPtr pfn, IntPtr arg1, object arg2)
            => ((delegate*<IntPtr, object, void>)pfn)(arg1, arg2);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, object arg1, IntPtr arg2)
            => ((delegate*<object, IntPtr, T>)pfn)(arg1, arg2);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(IntPtr pfn, string[] arg0)
            => ((delegate*<string[], T>)pfn)(arg0);

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ref byte Call(IntPtr pfn, void* arg1, ref byte arg2, ref byte arg3, void* arg4)
            => ref ((delegate*<void*, ref byte, ref byte, void*, ref byte>)pfn)(arg1, ref arg2, ref arg3, arg4);
    }
}
