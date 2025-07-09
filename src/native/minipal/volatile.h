// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_VOLATILE_H
#define HAVE_MINIPAL_VOLATILE_H

#include <stdint.h>

#ifdef _MSC_VER
#include <intrin.h>
#ifdef HOST_ARM64
#include <arm64intr.h>
#endif
#endif

//
// This code is extremely compiler- and CPU-specific, and will need to be altered to
// support new compilers and/or CPUs.  Here we enforce that we can only compile using
// VC++, or GCC on x86 or AMD64.
//
#if !defined(_MSC_VER) && !defined(__GNUC__)
#error The Volatile type is currently only defined for Visual C++ and GNU C++
#endif

#if defined(__GNUC__) && !defined(HOST_X86) && !defined(HOST_AMD64) && !defined(HOST_ARM) && !defined(HOST_ARM64) && !defined(HOST_LOONGARCH64) && !defined(HOST_WASM) && !defined(HOST_RISCV64)
#error The Volatile type is currently only defined for GCC when targeting x86, AMD64, ARM, ARM64, LOONGARCH64, Wasm, RISCV64
#endif

#if defined(__GNUC__)
#if defined(HOST_ARM) || defined(HOST_ARM64)
// This is functionally equivalent to the MemoryBarrier() macro used on ARM on Windows.
#define MINIPAL_VOLATILE_MEMORY_BARRIER() asm volatile ("dmb ish" : : : "memory")
#elif defined(HOST_LOONGARCH64)
#define MINIPAL_VOLATILE_MEMORY_BARRIER() asm volatile ("dbar 0 " : : : "memory")
#elif defined(HOST_RISCV64)
#define MINIPAL_VOLATILE_MEMORY_BARRIER() asm volatile ("fence rw,rw" : : : "memory")
#else
//
// For GCC, we prevent reordering by the compiler by inserting the following after a volatile
// load (to prevent subsequent operations from moving before the read), and before a volatile
// write (to prevent prior operations from moving past the write).  We don't need to do anything
// special to prevent CPU reorderings, because the x86 and AMD64 architectures are already
// sufficiently constrained for our purposes.  If we ever need to run on weaker CPU architectures
// (such as PowerPC), then we will need to do more work.
//
// Please do not use this macro outside of this file.  It is subject to change or removal without
// notice.
//
#define MINIPAL_VOLATILE_MEMORY_BARRIER() asm volatile ("" : : : "memory")
#endif // HOST_ARM || HOST_ARM64
#elif (defined(HOST_ARM) || defined(HOST_ARM64)) && _ISO_VOLATILE
// ARM & ARM64 have a very weak memory model and very few tools to control that model. We're forced to perform a full
// memory barrier to preserve the volatile semantics. Technically this is only necessary on MP systems but we
// currently don't have a cheap way to determine the number of CPUs from this header file. Revisit this if it
// turns out to be a performance issue for the uni-proc case.
#define MINIPAL_VOLATILE_MEMORY_BARRIER() MemoryBarrier()
#else
//
// On VC++, reorderings at the compiler and machine level are prevented by the use of the
// "volatile" keyword in volatile_load and volatile_store.  This should work on any CPU architecture
// targeted by VC++ with /iso_volatile-.
//
#define MINIPAL_VOLATILE_MEMORY_BARRIER()
#endif // __GNUC__

#if defined(HOST_ARM64) && defined(__GNUC__)

#ifdef __cplusplus
// Starting at version 3.8, clang errors out on initializing of type int * to volatile int *. To fix this, we add two templates to cast away volatility
// Helper structures for casting away volatileness
template<typename T>
struct RemoveVolatile
{
   typedef T type;
};

template<typename T>
struct RemoveVolatile<volatile T>
{
   typedef T type;
};

#define REMOVE_VOLATILE_T(T, val) const_cast<typename RemoveVolatile<T>::type *>(&val)
#else
#define REMOVE_VOLATILE_T(T, val) (T*)(&val)
#endif // __cplusplus

#define MINIPAL_VOLATILE_GCC_ATOMIC_LOAD(T, ptr, val) \
    do { __atomic_load((T const*)(ptr), REMOVE_VOLATILE_T(T, val), __ATOMIC_ACQUIRE); } while (0)

#define MINIPAL_VOLATILE_GCC_ATOMIC_STORE(T, ptr, val) \
    do { __atomic_store((T volatile*)(ptr), &val, __ATOMIC_RELEASE); } while (0)

#define MINIPAL_VOLATILE_LOAD_T(T, ptr, val)    do { val = *(T volatile const*)(ptr); asm volatile ("dmb ishld" : : : "memory"); } while (0)
#define MINIPAL_VOLATILE_LOAD_8(T, ptr, val)    do { MINIPAL_VOLATILE_GCC_ATOMIC_LOAD(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_LOAD_16(T, ptr, val)   do { MINIPAL_VOLATILE_GCC_ATOMIC_LOAD(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_LOAD_32(T, ptr, val)   do { MINIPAL_VOLATILE_GCC_ATOMIC_LOAD(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_LOAD_64(T, ptr, val)   do { MINIPAL_VOLATILE_GCC_ATOMIC_LOAD(T, ptr, val); } while (0)

#define MINIPAL_VOLATILE_STORE_T(T, ptr, val)   do { MINIPAL_VOLATILE_MEMORY_BARRIER(); *(T volatile*)(ptr) = val; } while (0)
#define MINIPAL_VOLATILE_STORE_8(T, ptr, val)   do { MINIPAL_VOLATILE_GCC_ATOMIC_STORE(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_STORE_16(T, ptr, val)  do { MINIPAL_VOLATILE_GCC_ATOMIC_STORE(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_STORE_32(T, ptr, val)  do { MINIPAL_VOLATILE_GCC_ATOMIC_STORE(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_STORE_64(T, ptr, val)  do { MINIPAL_VOLATILE_GCC_ATOMIC_STORE(T, ptr, val); } while (0)

#elif defined(HOST_ARM64) && defined(_MSC_VER)

#define MINIPAL_VOLATILE_LOAD_T(T, ptr, val)    do { val = *(T volatile const*)(ptr); __dmb(_ARM64_BARRIER_ISHLD); } while (0)
#define MINIPAL_VOLATILE_LOAD_8(T, ptr, val)    do { *(uint8_t*)&val = __ldar8((uint8_t volatile*)(ptr)); } while (0)
#define MINIPAL_VOLATILE_LOAD_16(T, ptr, val)   do { *(uint16_t*)&val = __ldar16((uint16_t volatile*)(ptr)); } while (0)
#define MINIPAL_VOLATILE_LOAD_32(T, ptr, val)   do { *(uint32_t*)&val = __ldar32((uint32_t volatile*)(ptr)); } while (0)
#define MINIPAL_VOLATILE_LOAD_64(T, ptr, val)   do { *(uint64_t*)&val = __ldar64((uint64_t volatile*)(ptr)); } while (0)

#define MINIPAL_VOLATILE_STORE_T(T, ptr, val)   do { __dmb(_ARM64_BARRIER_ISHLD); *(T volatile*)ptr = val; } while (0)
#define MINIPAL_VOLATILE_STORE_8(T, ptr, val)   do { __stlr8((uint8_t volatile*)(ptr), *(uint8_t*)&val); } while (0)
#define MINIPAL_VOLATILE_STORE_16(T, ptr, val)  do { __stlr16((uint16_t volatile*)(ptr), *(uint16_t*)&val); } while (0)
#define MINIPAL_VOLATILE_STORE_32(T, ptr, val)  do { __stlr32((uint32_t volatile*)(ptr), *(uint32_t*)&val); } while (0)
#define MINIPAL_VOLATILE_STORE_64(T, ptr, val)  do { __stlr64((uint64_t volatile*)(ptr), *(uint64_t*)&val); } while (0)

#else

#define MINIPAL_VOLATILE_LOAD_T(T, ptr, val)    do { val = *(T volatile const*)(ptr); MINIPAL_VOLATILE_MEMORY_BARRIER(); } while (0)
#define MINIPAL_VOLATILE_LOAD_8(T, ptr, val)    do { MINIPAL_VOLATILE_LOAD_T(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_LOAD_16(T, ptr, val)   do { MINIPAL_VOLATILE_LOAD_T(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_LOAD_32(T, ptr, val)   do { MINIPAL_VOLATILE_LOAD_T(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_LOAD_64(T, ptr, val)   do { MINIPAL_VOLATILE_LOAD_T(T, ptr, val); } while (0)

#define MINIPAL_VOLATILE_STORE_T(T, ptr, val)   do { MINIPAL_VOLATILE_MEMORY_BARRIER(); *(T volatile*)(ptr) = val; } while (0)
#define MINIPAL_VOLATILE_STORE_8(T, ptr, val)   do { MINIPAL_VOLATILE_STORE_T(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_STORE_16(T, ptr, val)  do { MINIPAL_VOLATILE_STORE_T(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_STORE_32(T, ptr, val)  do { MINIPAL_VOLATILE_STORE_T(T, ptr, val); } while (0)
#define MINIPAL_VOLATILE_STORE_64(T, ptr, val)  do { MINIPAL_VOLATILE_STORE_T(T, ptr, val); } while (0)

#endif // defined(HOST_ARM64) && defined(__GNUC__)
    
#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

// minipal_volatile_load_* loads a T from a pointer to T.  It is guaranteed that this load will not be optimized
// away by the compiler, and that any operation that occurs after this load, in program order, will
// not be moved before this load.  In general it is not guaranteed that the load will be atomic, though
// this is the case for most aligned scalar data types.  If you need atomic loads or stores, you need
// to consult the compiler and CPU manuals to find which circumstances allow atomicity.

static inline uint8_t minipal_volatile_load_uint8_t(uint8_t const* ptr)
{
    uint8_t value;
    MINIPAL_VOLATILE_LOAD_8(uint8_t, ptr, value);
    return value;
}

static inline uint16_t minipal_volatile_load_uint16_t(uint16_t const* ptr)
{
    uint16_t value;
    MINIPAL_VOLATILE_LOAD_16(uint16_t, ptr, value);
    return value;
}

static inline uint32_t minipal_volatile_load_uint32_t(uint32_t const* ptr)
{
    uint32_t value;
    MINIPAL_VOLATILE_LOAD_32(uint32_t, ptr, value);
    return value;
}

static inline uint64_t minipal_volatile_load_uint64_t(uint64_t const* ptr)
{
    uint64_t value;
    MINIPAL_VOLATILE_LOAD_64(uint64_t, ptr, value);
    return value;
}

static inline void* minipal_volatile_load_ptr(void* const* ptr)
{
    if (sizeof(uintptr_t) == 4)
    {
        uint32_t value;
        MINIPAL_VOLATILE_LOAD_32(uint32_t, ptr, value);
        return (void*)(uintptr_t)value;
    }
    else
    {
        uint64_t value;
        MINIPAL_VOLATILE_LOAD_64(uint64_t, ptr, value);
        return (void*)(uintptr_t)value;
    }
}

// minipal_volatile_store_* stores a T into the target of a pointer to T.  It is guaranteed that this store will
// not be optimized away by the compiler, and that any operation that occurs before this store, in program
// order, will not be moved after this store.  In general, it is not guaranteed that the store will be
// atomic, though this is the case for most aligned scalar data types.  If you need atomic loads or stores,
// you need to consult the compiler and CPU manuals to find which circumstances allow atomicity.

static inline void minipal_volatile_store_uint8_t(uint8_t* ptr, uint8_t value)
{
    MINIPAL_VOLATILE_STORE_8(uint8_t, ptr, value);
}

static inline void minipal_volatile_store_uint16_t(uint16_t* ptr, uint16_t value)
{
    MINIPAL_VOLATILE_STORE_16(uint16_t, ptr, value);
}

static inline void minipal_volatile_store_uint32_t(uint32_t* ptr, uint32_t value)
{
    MINIPAL_VOLATILE_STORE_32(uint32_t, ptr, value);
}

static inline void minipal_volatile_store_uint64_t(uint64_t* ptr, uint64_t value)
{
    MINIPAL_VOLATILE_STORE_64(uint64_t, ptr, value);
}

static inline void minipal_volatile_store_ptr(void** ptr, void* value)
{
    if (sizeof(uintptr_t) == 4)
    {
        uint32_t value32 = (uint32_t)(uintptr_t)value;
        MINIPAL_VOLATILE_STORE_32(uint32_t, ptr, value32);
    }
    else
    {
        uint64_t value64 = (uint64_t)(uintptr_t)value;
        MINIPAL_VOLATILE_STORE_64(uint64_t, ptr, value64);
    }
}

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_VOLATILE_H */
