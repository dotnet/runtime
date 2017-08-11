// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Volatile.h
// 

// 
// Defines the Volatile<T> type, which provides uniform volatile-ness on
// Visual C++ and GNU C++.
// 
// Visual C++ treats accesses to volatile variables as follows: no read or write
// can be removed by the compiler, no global memory access can be moved backwards past
// a volatile read, and no global memory access can be moved forward past a volatile
// write.
// 
// The GCC volatile semantic is straight out of the C standard: the compiler is not 
// allowed to remove accesses to volatile variables, and it is not allowed to reorder 
// volatile accesses relative to other volatile accesses.  It is allowed to freely 
// reorder non-volatile accesses relative to volatile accesses.
//
// We have lots of code that assumes that ordering of non-volatile accesses will be 
// constrained relative to volatile accesses.  For example, this pattern appears all 
// over the place:
//
//     static volatile int lock = 0;
//
//     while (InterlockedCompareExchange(&lock, 0, 1)) 
//     {
//         //spin
//     }
//                
//     //read and write variables protected by the lock
//
//     lock = 0;
//
// This depends on the reads and writes in the critical section not moving past the 
// final statement, which releases the lock.  If this should happen, then you have an 
// unintended race.
// 
// The solution is to ban the use of the "volatile" keyword, and instead define our
// own type Volatile<T>, which acts like a variable of type T except that accesses to
// the variable are always given VC++'s volatile semantics.
// 
// (NOTE: The code above is not intended to be an example of how a spinlock should be 
// implemented; it has many flaws, and should not be used. This code is intended only 
// to illustrate where we might get into trouble with GCC's volatile semantics.)
// 
// @TODO: many of the variables marked volatile in the CLR do not actually need to be 
// volatile.  For example, if a variable is just always passed to Interlocked functions
// (such as a refcount variable), there is no need for it to be volatile.  A future 
// cleanup task should be to examine each volatile variable and make them non-volatile
// if possible.
// 
// @TODO: link to a "Memory Models for CLR Devs" doc here (this doc does not yet exist).
//

#ifndef _VOLATILE_H_
#define _VOLATILE_H_

#include "staticcontract.h"

//
// This code is extremely compiler- and CPU-specific, and will need to be altered to 
// support new compilers and/or CPUs.  Here we enforce that we can only compile using
// VC++, or GCC on x86 or AMD64.
// 
#if !defined(_MSC_VER) && !defined(__GNUC__)
#error The Volatile type is currently only defined for Visual C++ and GNU C++
#endif

#if defined(__GNUC__) && !defined(_X86_) && !defined(_AMD64_) && !defined(_ARM_) && !defined(_ARM64_)
#error The Volatile type is currently only defined for GCC when targeting x86, AMD64, ARM or ARM64 CPUs
#endif

#if defined(__GNUC__)
#if defined(_ARM_) || defined(_ARM64_)
// This is functionally equivalent to the MemoryBarrier() macro used on ARM on Windows.
#define VOLATILE_MEMORY_BARRIER() asm volatile ("dmb ish" : : : "memory")
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
#define VOLATILE_MEMORY_BARRIER() asm volatile ("" : : : "memory")
#endif // _ARM_ || _ARM64_
#elif (defined(_ARM_) || defined(_ARM64_)) && _ISO_VOLATILE
// ARM & ARM64 have a very weak memory model and very few tools to control that model. We're forced to perform a full
// memory barrier to preserve the volatile semantics. Technically this is only necessary on MP systems but we
// currently don't have a cheap way to determine the number of CPUs from this header file. Revisit this if it
// turns out to be a performance issue for the uni-proc case.
#define VOLATILE_MEMORY_BARRIER() MemoryBarrier()
#else
//
// On VC++, reorderings at the compiler and machine level are prevented by the use of the 
// "volatile" keyword in VolatileLoad and VolatileStore.  This should work on any CPU architecture
// targeted by VC++ with /iso_volatile-.
//
#define VOLATILE_MEMORY_BARRIER()
#endif // __GNUC__

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


//
// VolatileLoad loads a T from a pointer to T.  It is guaranteed that this load will not be optimized
// away by the compiler, and that any operation that occurs after this load, in program order, will
// not be moved before this load.  In general it is not guaranteed that the load will be atomic, though
// this is the case for most aligned scalar data types.  If you need atomic loads or stores, you need
// to consult the compiler and CPU manuals to find which circumstances allow atomicity.
//
// Starting at version 3.8, clang errors out on initializing of type int * to volatile int *. To fix this, we add two templates to cast away volatility
// Helper structures for casting away volatileness


template<typename T>
inline
T VolatileLoad(T const * pt)
{
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

#ifndef DACCESS_COMPILE
#if defined(_ARM64_) && defined(__GNUC__)
    T val;
    static const unsigned lockFreeAtomicSizeMask = (1 << 1) | (1 << 2) | (1 << 4) | (1 << 8);
    if((1 << sizeof(T)) & lockFreeAtomicSizeMask)
    {
        __atomic_load((T const *)pt, const_cast<typename RemoveVolatile<T>::type *>(&val), __ATOMIC_ACQUIRE);
    }
    else
    {
        val = *(T volatile const *)pt;
        asm volatile ("dmb ishld" : : : "memory");
    }
#else
    T val = *(T volatile const *)pt;
    VOLATILE_MEMORY_BARRIER();
#endif
#else
    T val = *pt;
#endif
    return val;
}

template<typename T>
inline
T VolatileLoadWithoutBarrier(T const * pt)
{
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

#ifndef DACCESS_COMPILE
    T val = *(T volatile const *)pt;
#else
    T val = *pt;
#endif
    return val;
}

template <typename T> class Volatile;

template<typename T>
inline
T VolatileLoad(Volatile<T> const * pt)
{
    STATIC_CONTRACT_SUPPORTS_DAC;
    return pt->Load();
}

//
// VolatileStore stores a T into the target of a pointer to T.  Is is guaranteed that this store will
// not be optimized away by the compiler, and that any operation that occurs before this store, in program
// order, will not be moved after this store.  In general, it is not guaranteed that the store will be
// atomic, though this is the case for most aligned scalar data types.  If you need atomic loads or stores,
// you need to consult the compiler and CPU manuals to find which circumstances allow atomicity.
//
template<typename T>
inline
void VolatileStore(T* pt, T val)
{
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

#ifndef DACCESS_COMPILE
#if defined(_ARM64_) && defined(__GNUC__)
    static const unsigned lockFreeAtomicSizeMask = (1 << 1) | (1 << 2) | (1 << 4) | (1 << 8);
    if((1 << sizeof(T)) & lockFreeAtomicSizeMask)
    {
        __atomic_store((T volatile *)pt, &val, __ATOMIC_RELEASE);
    }
    else
    {
        VOLATILE_MEMORY_BARRIER();
        *(T volatile *)pt = val;
    }
#else
    VOLATILE_MEMORY_BARRIER();
    *(T volatile *)pt = val;
#endif
#else
    *pt = val;
#endif
}

template<typename T>
inline
void VolatileStoreWithoutBarrier(T* pt, T val)
{
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

#ifndef DACCESS_COMPILE
    *(T volatile *)pt = val;
#else
    *pt = val;
#endif
}

//
// Volatile<T> implements accesses with our volatile semantics over a variable of type T.
// Wherever you would have used a "volatile Foo" or, equivalently, "Foo volatile", use Volatile<Foo> 
// instead.  If Foo is a pointer type, use VolatilePtr.
// 
// Note that there are still some things that don't work with a Volatile<T>,
// that would have worked with a "volatile T".  For example, you can't cast a Volatile<int> to a float.
// You must instead cast to an int, then to a float.  Or you can call Load on the Volatile<int>, and
// cast the result to a float.  In general, calling Load or Store explicitly will work around 
// any problems that can't be solved by operator overloading.
// 
// @TODO: it's not clear that we actually *want* any operator overloading here.  It's in here primarily
// to ease the task of converting all of the old uses of the volatile keyword, but in the long
// run it's probably better if users of this class are forced to call Load() and Store() explicitly.
// This would make it much more clear where the memory barriers are, and which operations are actually
// being performed, but it will have to wait for another cleanup effort.
//
template <typename T>
class Volatile
{
private:
    //
    // The data which we are treating as volatile
    //
    T m_val;

public:
    //
    // Default constructor.  Results in an unitialized value!
    //
    inline Volatile() 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
    }

    //
    // Allow initialization of Volatile<T> from a T
    //
    inline Volatile(const T& val) 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        ((volatile T &)m_val) = val;
    }

    //
    // Copy constructor
    //
    inline Volatile(const Volatile<T>& other)
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        ((volatile T &)m_val) = other.Load();
    }

    //
    // Loads the value of the volatile variable.  See code:VolatileLoad for the semantics of this operation.
    //
    inline T Load() const
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        return VolatileLoad(&m_val);
    }

    //
    // Loads the value of the volatile variable atomically without erecting the memory barrier.
    //
    inline T LoadWithoutBarrier() const
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        return ((volatile T &)m_val);
    }

    //
    // Stores a new value to the volatile variable.  See code:VolatileStore for the semantics of this
    // operation.
    //
    inline void Store(const T& val) 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        VolatileStore(&m_val, val);
    }


    //
    // Stores a new value to the volatile variable atomically without erecting the memory barrier.
    //
    inline void StoreWithoutBarrier(const T& val) const
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        ((volatile T &)m_val) = val;
    }


    //
    // Gets a pointer to the volatile variable.  This is dangerous, as it permits the variable to be
    // accessed without using Load and Store, but it is necessary for passing Volatile<T> to APIs like
    // InterlockedIncrement.
    //
    inline volatile T* GetPointer() { return (volatile T*)&m_val; }


    //
    // Gets the raw value of the variable.  This is dangerous, as it permits the variable to be
    // accessed without using Load and Store
    //
    inline T& RawValue() { return m_val; }

    //
    // Allow casts from Volatile<T> to T.  Note that this allows implicit casts, so you can
    // pass a Volatile<T> directly to a method that expects a T.
    //
    inline operator T() const 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        return this->Load();
    }

    //
    // Assignment from T
    //
    inline Volatile<T>& operator=(T val) {Store(val); return *this;}

    //
    // Get the address of the volatile variable.  This is dangerous, as it allows the value of the 
    // volatile variable to be accessed directly, without going through Load and Store, but it is
    // necessary for passing Volatile<T> to APIs like InterlockedIncrement.  Note that we are returning
    // a pointer to a volatile T here, so we cannot accidentally pass this pointer to an API that 
    // expects a normal pointer.
    //
    inline T volatile * operator&() {return this->GetPointer();}
    inline T volatile const * operator&() const {return this->GetPointer();}

    //
    // Comparison operators
    //
    template<typename TOther>
    inline bool operator==(const TOther& other) const {return this->Load() == other;}

    template<typename TOther>
    inline bool operator!=(const TOther& other) const {return this->Load() != other;}

    //
    // Miscellaneous operators.  Add more as necessary.
    //
	inline Volatile<T>& operator+=(T val) {Store(this->Load() + val); return *this;}
	inline Volatile<T>& operator-=(T val) {Store(this->Load() - val); return *this;}
    inline Volatile<T>& operator|=(T val) {Store(this->Load() | val); return *this;}
    inline Volatile<T>& operator&=(T val) {Store(this->Load() & val); return *this;}
    inline bool operator!() const { STATIC_CONTRACT_SUPPORTS_DAC; return !this->Load();}

    //
    // Prefix increment
    //
    inline Volatile& operator++() {this->Store(this->Load()+1); return *this;}

    //
    // Postfix increment
    //
    inline T operator++(int) {T val = this->Load(); this->Store(val+1); return val;}

    //
    // Prefix decrement
    //
    inline Volatile& operator--() {this->Store(this->Load()-1); return *this;}

    //
    // Postfix decrement
    //
    inline T operator--(int) {T val = this->Load(); this->Store(val-1); return val;}
};

//
// A VolatilePtr builds on Volatile<T> by adding operators appropriate to pointers.
// Wherever you would have used "Foo * volatile", use "VolatilePtr<Foo>" instead.
// 
// VolatilePtr also allows the substution of other types for the underlying pointer.  This
// allows you to wrap a VolatilePtr around a custom type that looks like a pointer.  For example,
// if what you want is a "volatile DPTR<Foo>", use "VolatilePtr<Foo, DPTR<Foo>>".
//
template <typename T, typename P = T*>
class VolatilePtr : public Volatile<P>
{
public:
    //
    // Default constructor.  Results in an uninitialized pointer!
    //
    inline VolatilePtr() 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
    }

    //
    // Allow assignment from the pointer type.
    //
    inline VolatilePtr(P val) : Volatile<P>(val) 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
    }

    //
    // Copy constructor
    //
    inline VolatilePtr(const VolatilePtr& other) : Volatile<P>(other) 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
    }

    //
    // Cast to the pointer type
    //
    inline operator P() const 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        return (P)this->Load();
    }

    //
    // Member access
    //
    inline P operator->() const 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        return (P)this->Load();
    }

    //
    // Dereference the pointer
    //
    inline T& operator*() const 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        return *(P)this->Load();
    }

    //
    // Access the pointer as an array
    //
    template <typename TIndex>
    inline T& operator[](TIndex index) 
    {
        STATIC_CONTRACT_SUPPORTS_DAC;
        return ((P)this->Load())[index];
    }
};


//
// Warning: workaround
// 
// At the bottom of this file, we are going to #define the "volatile" keyword such that it is illegal
// to use it.  Unfortunately, VC++ uses the volatile keyword in stddef.h, in the definition of "offsetof".
// GCC does not use volatile in its definition.
// 
// To get around this, we include stddef.h here (even if we're on GCC, for consistency).  We then need
// to redefine offsetof such that it does not use volatile, if we're building with VC++.
//
#include <stddef.h>
#ifdef _MSC_VER
#undef offsetof
#ifdef  _WIN64
#define offsetof(s,m)   (size_t)( (ptrdiff_t)&reinterpret_cast<const char&>((((s *)0)->m)) )
#else
#define offsetof(s,m)   (size_t)&reinterpret_cast<const char&>((((s *)0)->m))
#endif //_WIN64

// These also use volatile, so we'll include them here.
//#include <intrin.h>
//#include <memory>

#endif //_MSC_VER

//
// From here on out, we ban the use of the "volatile" keyword.  If you found this while trying to define
// a volatile variable, go to the top of this file and start reading.
//
#ifdef volatile
#undef volatile
#endif
// ***** Temporarily removing this to unblock integration with new VC++ bits
//#define volatile (DoNotUseVolatileKeyword) volatile

// The substitution for volatile above is defined in such a way that we can still explicitly access the
// volatile keyword without error using the macros below. Use with care.
//#define REMOVE_DONOTUSE_ERROR(x)
//#define RAW_KEYWORD(x) REMOVE_DONOTUSE_ERROR x
#define RAW_KEYWORD(x) x

#ifdef DACCESS_COMPILE
// No need to use volatile in DAC builds - DAC is single-threaded and the target
// process is suspended.
#define VOLATILE(T) T
#else

// Disable use of Volatile<T> for GC/HandleTable code except on platforms where it's absolutely necessary.
#if defined(_MSC_VER) && !defined(_ARM_) && !defined(_ARM64_)
#define VOLATILE(T) T RAW_KEYWORD(volatile)
#else
#define VOLATILE(T) Volatile<T>
#endif

#endif // DACCESS_COMPILE

// VolatilePtr-specific clr::SafeAddRef and clr::SafeRelease
namespace clr
{
    template < typename ItfT, typename PtrT > inline
    #ifdef __checkReturn // Volatile.h is used in corunix headers, which don't define/nullify SAL.
        __checkReturn
    #endif
    VolatilePtr<ItfT, PtrT>&
    SafeAddRef(VolatilePtr<ItfT, PtrT>& pItf)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        SafeAddRef(pItf.Load());
        return pItf;
    }

    template < typename ItfT, typename PtrT > inline
    ULONG
    SafeRelease(VolatilePtr<ItfT, PtrT>& pItf)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        return SafeRelease(pItf.Load());
    }
}

#endif //_VOLATILE_H_
