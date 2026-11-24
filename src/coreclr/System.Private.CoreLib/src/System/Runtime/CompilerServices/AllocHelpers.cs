// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    // Managed implementation of the allocation fast paths (CORINFO_HELP_NEWSFAST, CORINFO_HELP_NEWARR_1_VC,
    // CORINFO_HELP_NEWARR_1_PTR and String.FastAllocateString). Opt-in via DOTNET_ManagedAllocHelpers=1;
    // the runtime only installs them on linux arm64/loongarch64/riscv64 when the JIT can inline thread-static access
    // (see InitJITAllocationHelpers). Not compiled for WebAssembly.
    //
    // The fast paths bump-allocate from the current thread's allocation context (ee_alloc_context), which the runtime
    // embeds in ThreadLocalData (t_ThreadStatics, the native block that also holds the managed thread statics). The
    // t_allocContext thread static below is mapped onto its first field (the same mechanism as
    // Thread.DirectOnThreadLocalData and ManagedThreadId), so the address of the allocation context is the address of
    // that thread static: an inlined TLS base + constant, with no load and no call into native code. If the
    // allocation does not fit (including a not yet initialized, all-zero context), the native slow helpers are called.
    //
    // GC SAFETY: between the update of alloc_ptr and the store of the MethodTable pointer (i.e. while the new object
    // is not yet a valid object), the thread must not be suspended for GC. This holds because:
    //  - the fast paths contain no calls, loops or GC polls in that region;
    //  - the methods are marked [Intrinsic]: the JIT recognizes them and never makes them fully interruptible
    //    (not even for JitStress/JitFullyInt/debuggable code), so the only GC safe points are the call sites
    //    of the slow paths (which are outside the critical region) and the return-address hijack after return.
    //    (The JIT also does not treat their tail calls to the [NoInlining] slow paths as potential GC-poll-free
    //    loops, which would otherwise force full interruptibility.) Verified with JitGCDump/JitDisasm: the only
    //    GC safe point is the inlined thread-static access' fallback/TLS call before the critical region;

    //  - the critical region stays inside these partially interruptible methods: NewString (the only one callable
    //    from IL) is [NoInlining]; NewSFast/NewArr1 are only invoked as JIT helpers.
    //
    // MEMORY ORDERING: same as the native fast helpers (runtime/<arch>/AllocFast.S), which use plain stores with no
    // barrier. Publishing the new reference to other threads goes through a reference store, which is a release
    // per the .NET memory model (docs/design/specs/Memory-model.md, "Object assignment"), so other threads (incl.
    // background GC threads) cannot observe the object before its MethodTable/length. The allocation context itself
    // is only read by the GC/other threads while this thread is suspended.
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe class AllocHelpers
    {
        // Mirrors the start of the native ee_alloc_context (vm/gcheaputilities.h):
        //   uint8_t* m_CombinedLimit; gc_alloc_context { uint8_t* alloc_ptr; uint8_t* alloc_limit; ... }
        [StructLayout(LayoutKind.Sequential)]
        private struct EEAllocContext
        {
            public byte* CombinedLimit;
            public nint AllocPtr;
        }

        // Must be the only thread static of this class: the runtime maps it onto the first field (m_CombinedLimit) of
        // the ee_alloc_context embedded in ThreadLocalData (see the TLSIndexType::DirectOnThreadLocalData handling in
        // threadstatics.cpp). Only its address is used.
        [ThreadStatic]
        private static nint t_allocContext;

        private static EEAllocContext* AllocContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (EEAllocContext*)Unsafe.AsPointer(ref t_allocContext);
        }

        // Must match STRING_BASE_SIZE / SZARRAY_BASE_SIZE: object header + MethodTable* + length (+ terminator).
#if TARGET_64BIT
        private const int StringBaseSize = 8 + 8 + sizeof(int) + sizeof(char);
        private const int ArrayLengthOffset = 8; // Offset of the length field from the object start
#else
        private const int StringBaseSize = 4 + 4 + sizeof(int) + sizeof(char);
        private const int ArrayLengthOffset = 4;
#endif

        /// <summary>Native slow path for non-array objects (RhpNew).</summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object NewObjectSlow(MethodTable* pMT);

        /// <summary>Native slow path for arrays and strings (RhpNewVariableSizeObject).</summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object NewVariableSizeObjectSlow(MethodTable* pMT, nint length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object NewSFastSlow(MethodTable* pMT) => NewObjectSlow(pMT);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object NewVariableSizeSlow(MethodTable* pMT, nint length) => NewVariableSizeObjectSlow(pMT, length);

        // CORINFO_HELP_NEWSFAST: non-finalizable, non-array object.
        // Not [NoInlining]: only ever called as a JIT helper (never from IL), and NoInlining would block the
        // opportunistic tail call to the slow path.
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static object NewSFast(MethodTable* pMT)
        {
            EEAllocContext* ctx = AllocContext;
            nuint size = pMT->BaseSize;
            byte* p = (byte*)ctx->AllocPtr;
            if (size <= (nuint)(ctx->CombinedLimit - p))
            {
                // No GC safe point from here until return.
                *(MethodTable**)p = pMT;
                // Re-read the object reference from the allocation context instead of reinterpreting `p`:
                // reinterpreting a local makes it address-exposed and forces a stack spill. The intervening
                // store keeps the JIT from CSE-ing this load with the load of `p` above.
                object obj = Unsafe.As<nint, object>(ref ctx->AllocPtr);
                ctx->AllocPtr = (nint)(p + size);
                return obj;
            }

            return NewSFastSlow(pMT);
        }

        // CORINFO_HELP_NEWARR_1_VC / CORINFO_HELP_NEWARR_1_PTR: single-dimensional, zero-based array.
        // Not [NoInlining]: only ever called as a JIT helper (never from IL), and NoInlining would block the
        // opportunistic tail call to the slow path.
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static object NewArr1(MethodTable* pMT, nint length)
        {
            EEAllocContext* ctx = AllocContext;
            // Limit the element count to the non-negative int range (as the native helper does); the slow
            // path throws OverflowException/OutOfMemoryException as appropriate.
            if ((nuint)length <= int.MaxValue)
            {
                // Cannot overflow on 64-bit: BaseSize + 0xFFFF * 0x7FFFFFFF + 7 < 2^48.
                ulong size = ((ulong)pMT->BaseSize + (ulong)pMT->ComponentSize * (ulong)length + (ulong)(sizeof(nint) - 1))
                    & ~(ulong)(sizeof(nint) - 1);
                byte* p = (byte*)ctx->AllocPtr;
                if (size <= (ulong)(nuint)(ctx->CombinedLimit - p))
                {
                    // No GC safe point from here until return.
                    *(MethodTable**)p = pMT;
                    *(int*)(p + ArrayLengthOffset) = (int)length;
                    object obj = Unsafe.As<nint, object>(ref ctx->AllocPtr);
                    ctx->AllocPtr = (nint)(p + (nuint)size);
                    return obj;
                }
            }

            return NewVariableSizeSlow(pMT, length);
        }

        // String.FastAllocateString.
        [Intrinsic]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static string NewString(MethodTable* pMT, nint length)
        {
            EEAllocContext* ctx = AllocContext;
            if ((nuint)length <= (nuint)string.MaxLength)
            {
                nuint size = ((nuint)StringBaseSize + (nuint)length * sizeof(char) + (nuint)(sizeof(nint) - 1))
                    & ~(nuint)(sizeof(nint) - 1);
                byte* p = (byte*)ctx->AllocPtr;
                if (size <= (nuint)(ctx->CombinedLimit - p))
                {
                    // No GC safe point from here until return.
                    *(MethodTable**)p = pMT;
                    *(int*)(p + ArrayLengthOffset) = (int)length;
                    string obj = Unsafe.As<nint, string>(ref ctx->AllocPtr);
                    ctx->AllocPtr = (nint)(p + size);
                    return obj;
                }
            }

            return Unsafe.As<string>(NewVariableSizeSlow(pMT, length));
        }
    }

    // Kept separate from AllocHelpers so that AllocHelpers has no static constructor: the fast paths must not
    // need a class initialization check.
    internal static class AllocHelpersConfig
    {
        /// <summary>Returns true when the runtime installed the managed allocation helpers.</summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsEnabled();

        internal static readonly bool s_enabled = IsEnabled();
    }
}
