// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.Augments;

namespace System.Runtime
{
    // Initialize the cache eagerly to avoid null checks.
    [EagerStaticClassConstruction]
    public static class TypeLoaderExports
    {
        //
        // Generic lookup cache
        //

#if DEBUG
        // use smaller numbers to hit resizing/preempting logic in debug
        private const int InitialCacheSize = 8; // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 512;
#else
        private const int InitialCacheSize = 128; // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 128 * 1024;
#endif // DEBUG

        private static GenericCache<Key, Value> s_cache =
            new GenericCache<Key, Value>(InitialCacheSize, MaximumCacheSize);

        private struct Key : IEquatable<Key>
        {
            public IntPtr _context;
            public IntPtr _signature;

            public Key(nint context, nint signature)
            {
                _context = context;
                _signature = signature;
            }

            public bool Equals(Key other)
            {
                return _context == other._context && _signature == other._signature;
            }

            public override int GetHashCode()
            {
                // pointers will likely match and cancel out in the upper bits
                // we will rotate context by 16 bit to keep more varying bits in the hash
                IntPtr context = (IntPtr)BitOperations.RotateLeft((nuint)_context, 16);
                return (context ^ _signature).GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is Key && Equals((Key)obj);
            }
        }

        private struct Value
        {
            public IntPtr _result;
            public IntPtr _auxResult;

            public Value(IntPtr result, IntPtr auxResult)
            {
                _result = result;
                _auxResult = auxResult;
            }
        }

        private static Value LookupOrAdd(IntPtr context, IntPtr signature)
        {
            if (!TryGetFromCache(context, signature, out var v))
            {
                v = CacheMiss(context, signature);
            }

            return v;
        }

        public static IntPtr GenericLookup(IntPtr context, IntPtr signature)
        {
            if (!TryGetFromCache(context, signature, out var v))
            {
                v = CacheMiss(context, signature);
            }

            return v._result;
        }

        public static unsafe IntPtr GVMLookupForSlot(object obj, RuntimeMethodHandle slot)
        {
            if (TryGetFromCache((IntPtr)obj.GetMethodTable(), RuntimeMethodHandle.ToIntPtr(slot), out var v))
                return v._result;

            return GVMLookupForSlotSlow(obj, slot);
        }

        private static unsafe IntPtr GVMLookupForSlotSlow(object obj, RuntimeMethodHandle slot)
        {
            Value v = CacheMiss((IntPtr)obj.GetMethodTable(), RuntimeMethodHandle.ToIntPtr(slot),
                    (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult)
                        => RuntimeAugments.TypeLoaderCallbacks.ResolveGenericVirtualMethodTarget(new RuntimeTypeHandle((MethodTable*)context), *(RuntimeMethodHandle*)&signature));

            return v._result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe IntPtr OpenInstanceMethodLookup(IntPtr openResolver, object obj)
        {
            if (!TryGetFromCache((IntPtr)obj.GetMethodTable(), openResolver, out var v))
            {
                v = CacheMiss((IntPtr)obj.GetMethodTable(), openResolver,
                        (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult)
                            => Internal.Runtime.CompilerServices.OpenMethodResolver.ResolveMethodWorker(signature, contextObject),
                        obj);
            }

            return v._result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetFromCache(IntPtr context, IntPtr signature, out Value entry)
        {
            Key k = new Key(context, signature);
            return s_cache.TryGet(k, out entry);
        }

        private static Value CacheMiss(IntPtr ctx, IntPtr sig)
        {
            return CacheMiss(ctx, sig,
                (IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult) =>
                    RuntimeAugments.TypeLoaderCallbacks.GenericLookupFromContextAndSignature(context, signature, out auxResult)
                );
        }

        private static unsafe Value CacheMiss(IntPtr context, IntPtr signature, RuntimeObjectFactory factory, object contextObject = null)
        {
            //
            // Call into the type loader to compute the target
            //
            IntPtr auxResult = default;
            IntPtr result = factory(context, signature, contextObject, ref auxResult);

            Key k = new Key(context, signature);
            Value v = new Value(result, auxResult);

            s_cache.TrySet(k, v);
            return v;
        }
    }

    internal delegate IntPtr RuntimeObjectFactory(IntPtr context, IntPtr signature, object contextObject, ref IntPtr auxResult);

    internal static unsafe class RawCalliHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Call(System.IntPtr pfn, ref byte data)
            => ((delegate*<ref byte, void>)pfn)(ref data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, IntPtr arg)
            => ((delegate*<IntPtr, T>)pfn)(arg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Call(System.IntPtr pfn, object arg)
            => ((delegate*<object, void>)pfn)(arg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, IntPtr arg1, IntPtr arg2)
            => ((delegate*<IntPtr, IntPtr, T>)pfn)(arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, IntPtr arg1, IntPtr arg2, object arg3, out IntPtr arg4)
            => ((delegate*<IntPtr, IntPtr, object, out IntPtr, T>)pfn)(arg1, arg2, arg3, out arg4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Call(System.IntPtr pfn, IntPtr arg1, object arg2)
            => ((delegate*<IntPtr, object, void>)pfn)(arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(System.IntPtr pfn, object arg1, IntPtr arg2)
            => ((delegate*<object, IntPtr, T>)pfn)(arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Call<T>(IntPtr pfn, string[] arg0)
            => ((delegate*<string[], T>)pfn)(arg0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte Call(IntPtr pfn, void* arg1, ref byte arg2, ref byte arg3, void* arg4)
            => ref ((delegate*<void*, ref byte, ref byte, void*, ref byte>)pfn)(arg1, ref arg2, ref arg3, arg4);
    }
}
