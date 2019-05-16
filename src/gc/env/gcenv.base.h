// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef __GCENV_BASE_INCLUDED__
#define __GCENV_BASE_INCLUDED__
//
// Sets up basic environment for CLR GC
//

#ifdef _MSC_VER
#include <intrin.h>
#endif // _MSC_VER

#define REDHAWK_PALIMPORT extern "C"
#define REDHAWK_PALAPI __stdcall

#if !defined(_MSC_VER)
#define _alloca alloca
#endif //_MSC_VER

#ifndef _MSC_VER
#define __stdcall
#ifdef __GNUC__
#define __forceinline __attribute__((always_inline)) inline
#else // __GNUC__
#define __forceinline inline
#endif // __GNUC__
// [LOCALGC TODO] is there a better place for this?
#define NOINLINE __attribute__((noinline))
#else // !_MSC_VER
#define NOINLINE __declspec(noinline)
#endif // _MSC_VER

#ifndef SIZE_T_MAX
#define SIZE_T_MAX ((size_t)-1)
#endif
#ifndef SSIZE_T_MAX
#define SSIZE_T_MAX ((ptrdiff_t)(SIZE_T_MAX / 2))
#endif

#ifndef _INC_WINDOWS
// -----------------------------------------------------------------------------------------------------------
//
// Aliases for Win32 types
//

typedef int BOOL;
typedef uint32_t DWORD;
typedef uint64_t DWORD64;
typedef uint32_t ULONG;

// -----------------------------------------------------------------------------------------------------------
// HRESULT subset.

#ifdef PLATFORM_UNIX
typedef int32_t HRESULT;
#else
// this must exactly match the typedef used by windows.h
typedef long HRESULT;
#endif

#define SUCCEEDED(_hr)          ((HRESULT)(_hr) >= 0)
#define FAILED(_hr)             ((HRESULT)(_hr) < 0)

inline HRESULT HRESULT_FROM_WIN32(unsigned long x)
{
    return (HRESULT)(x) <= 0 ? (HRESULT)(x) : (HRESULT) (((x) & 0x0000FFFF) | (7 << 16) | 0x80000000);
}

#define S_OK                    0x0
#define E_FAIL                  0x80004005
#define E_OUTOFMEMORY           0x8007000E
#define COR_E_EXECUTIONENGINE   0x80131506
#define CLR_E_GC_BAD_AFFINITY_CONFIG 0x8013200A
#define CLR_E_GC_BAD_AFFINITY_CONFIG_FORMAT 0x8013200B

#define NOERROR                 0x0
#define ERROR_TIMEOUT           1460

#define TRUE true
#define FALSE false

#define CALLBACK __stdcall
#define FORCEINLINE __forceinline

#define INFINITE 0xFFFFFFFF

#define ZeroMemory(Destination,Length) memset((Destination),0,(Length))

#ifndef _countof
#define _countof(_array) (sizeof(_array)/sizeof(_array[0]))
#endif

#ifndef min
#define min(a,b) (((a) < (b)) ? (a) : (b))
#endif

#ifndef max
#define max(a,b) (((a) > (b)) ? (a) : (b))
#endif

#define C_ASSERT(cond) static_assert( cond, #cond )

#define UNREFERENCED_PARAMETER(P)          (void)(P)

#ifdef PLATFORM_UNIX
#define _vsnprintf_s(string, sizeInBytes, count, format, args) vsnprintf(string, sizeInBytes, format, args)
#define sprintf_s snprintf
#define swprintf_s swprintf
#define _snprintf_s(string, sizeInBytes, count, format, ...) \
  snprintf(string, sizeInBytes, format, ## __VA_ARGS__)
#endif

#ifdef UNICODE
#define _tcslen wcslen
#define _tcscpy wcscpy
#define _stprintf_s swprintf_s
#define _tfopen _wfopen
#else
#define _tcslen strlen
#define _tcscpy strcpy
#define _stprintf_s sprintf_s
#define _tfopen fopen
#endif

#define WINAPI __stdcall

typedef DWORD (WINAPI *PTHREAD_START_ROUTINE)(void* lpThreadParameter);

#define WAIT_OBJECT_0           0
#define WAIT_TIMEOUT            258
#define WAIT_FAILED             0xFFFFFFFF

#if defined(_MSC_VER) 
 #if defined(_ARM_)

  __forceinline void YieldProcessor() { }
  extern "C" void __emit(const unsigned __int32 opcode);
  #pragma intrinsic(__emit)
  #define MemoryBarrier() { __emit(0xF3BF); __emit(0x8F5F); }

 #elif defined(_ARM64_)

  extern "C" void __yield(void);
  #pragma intrinsic(__yield)
  __forceinline void YieldProcessor() { __yield();}

  extern "C" void __dmb(const unsigned __int32 _Type);
  #pragma intrinsic(__dmb)
  #define MemoryBarrier() { __dmb(_ARM64_BARRIER_SY); }

 #elif defined(_AMD64_)
  
  extern "C" void
  _mm_pause (
      void
      );
  
  extern "C" void
  _mm_mfence (
      void
      );

  #pragma intrinsic(_mm_pause)
  #pragma intrinsic(_mm_mfence)
  
  #define YieldProcessor _mm_pause
  #define MemoryBarrier _mm_mfence

 #elif defined(_X86_)
  
  #define YieldProcessor() __asm { rep nop }
  #define MemoryBarrier() MemoryBarrierImpl()
  __forceinline void MemoryBarrierImpl()
  {
      int32_t Barrier;
      __asm {
          xchg Barrier, eax
      }
  }

 #else // !_ARM_ && !_AMD64_ && !_X86_
  #error Unsupported architecture
 #endif
#else // _MSC_VER

#ifdef __llvm__
#define HAS_IA32_PAUSE __has_builtin(__builtin_ia32_pause)
#define HAS_IA32_MFENCE __has_builtin(__builtin_ia32_mfence)
#else
#define HAS_IA32_PAUSE 0
#define HAS_IA32_MFENCE 0
#endif

// Only clang defines __has_builtin, so we first test for a GCC define
// before using __has_builtin.

#if defined(__i386__) || defined(__x86_64__)

#if (__GNUC__ > 4 && __GNUC_MINOR > 7) || HAS_IA32_PAUSE
 // clang added this intrinsic in 3.8
 // gcc added this intrinsic by 4.7.1
 #define YieldProcessor __builtin_ia32_pause
#endif // __has_builtin(__builtin_ia32_pause)

#if defined(__GNUC__) || HAS_IA32_MFENCE
 // clang has had this intrinsic since at least 3.0
 // gcc has had this intrinsic since forever
 #define MemoryBarrier __builtin_ia32_mfence
#endif // __has_builtin(__builtin_ia32_mfence)

// If we don't have intrinsics, we can do some inline asm instead.
#ifndef YieldProcessor
 #define YieldProcessor() asm volatile ("pause")
#endif // YieldProcessor

#ifndef MemoryBarrier
 #define MemoryBarrier() asm volatile ("mfence")
#endif // MemoryBarrier

#endif // defined(__i386__) || defined(__x86_64__)

#ifdef __aarch64__
 #define YieldProcessor() asm volatile ("yield")
 #define MemoryBarrier __sync_synchronize
#endif // __aarch64__

#ifdef __arm__
 #define YieldProcessor()
 #define MemoryBarrier __sync_synchronize
#endif // __arm__

#endif // _MSC_VER

#ifdef _MSC_VER
#pragma intrinsic(_BitScanForward)
#pragma intrinsic(_BitScanReverse)
#if _WIN64
 #pragma intrinsic(_BitScanForward64)
 #pragma intrinsic(_BitScanReverse64)
#endif
#endif // _MSC_VER

// Cross-platform wrapper for the _BitScanForward compiler intrinsic.
// A value is unconditionally stored through the bitIndex argument,
// but callers should only rely on it when the function returns TRUE;
// otherwise, the stored value is undefined and varies by implementation
// and hardware platform.
inline uint8_t BitScanForward(uint32_t *bitIndex, uint32_t mask)
{
#ifdef _MSC_VER
    return _BitScanForward((unsigned long*)bitIndex, mask);
#else // _MSC_VER
    int iIndex = __builtin_ffs(mask);
    *bitIndex = static_cast<uint32_t>(iIndex - 1);
    // Both GCC and Clang generate better, smaller code if we check whether the
    // mask was/is zero rather than the equivalent check that iIndex is zero.
    return mask != 0 ? TRUE : FALSE;
#endif // _MSC_VER
}

// Cross-platform wrapper for the _BitScanForward64 compiler intrinsic.
// A value is unconditionally stored through the bitIndex argument,
// but callers should only rely on it when the function returns TRUE;
// otherwise, the stored value is undefined and varies by implementation
// and hardware platform.
inline uint8_t BitScanForward64(uint32_t *bitIndex, uint64_t mask)
{
#ifdef _MSC_VER
 #if _WIN64
    return _BitScanForward64((unsigned long*)bitIndex, mask);
 #else
    // MSVC targeting a 32-bit target does not support this intrinsic.
    // We can fake it using two successive invocations of _BitScanForward.
    uint32_t hi = (mask >> 32) & 0xFFFFFFFF;
    uint32_t lo = mask & 0xFFFFFFFF;
    uint32_t fakeBitIndex = 0;
    
    uint8_t result = BitScanForward(bitIndex, lo);
    if (result == 0)
    {
        result = BitScanForward(&fakeBitIndex, hi);
        if (result != 0)
        {
            *bitIndex = fakeBitIndex + 32;
        }
    }

    return result;
 #endif // _WIN64
#else
    int iIndex = __builtin_ffsll(mask);
    *bitIndex = static_cast<uint32_t>(iIndex - 1);
    // Both GCC and Clang generate better, smaller code if we check whether the
    // mask was/is zero rather than the equivalent check that iIndex is zero.
    return mask != 0 ? TRUE : FALSE;
#endif // _MSC_VER
}

// Cross-platform wrapper for the _BitScanReverse compiler intrinsic.
inline uint8_t BitScanReverse(uint32_t *bitIndex, uint32_t mask)
{
#ifdef _MSC_VER
    return _BitScanReverse((unsigned long*)bitIndex, mask);
#else // _MSC_VER
    // The result of __builtin_clzl is undefined when mask is zero,
    // but it's still OK to call the intrinsic in that case (just don't use the output).
    // Unconditionally calling the intrinsic in this way allows the compiler to
    // emit branchless code for this function when possible (depending on how the
    // intrinsic is implemented for the target platform).
    int lzcount = __builtin_clzl(mask);
    *bitIndex = static_cast<uint32_t>(31 - lzcount);
    return mask != 0 ? TRUE : FALSE;
#endif // _MSC_VER
}

// Cross-platform wrapper for the _BitScanReverse64 compiler intrinsic.
inline uint8_t BitScanReverse64(uint32_t *bitIndex, uint64_t mask)
{
#ifdef _MSC_VER
 #if _WIN64
    return _BitScanReverse64((unsigned long*)bitIndex, mask);
 #else
    // MSVC targeting a 32-bit target does not support this intrinsic.
    // We can fake it checking whether the upper 32 bits are zeros (or not)
    // then calling _BitScanReverse() on either the upper or lower 32 bits.
    uint32_t upper = static_cast<uint32_t>(mask >> 32);

    if (upper != 0)
    {
        uint8_t result = _BitScanReverse((unsigned long*)bitIndex, upper);
        *bitIndex += 32;
        return result;
    }

    return _BitScanReverse((unsigned long*)bitIndex, static_cast<uint32_t>(mask));
 #endif // _WIN64
#else
    // The result of __builtin_clzll is undefined when mask is zero,
    // but it's still OK to call the intrinsic in that case (just don't use the output).
    // Unconditionally calling the intrinsic in this way allows the compiler to
    // emit branchless code for this function when possible (depending on how the
    // intrinsic is implemented for the target platform).
    int lzcount = __builtin_clzll(mask);
    *bitIndex = static_cast<uint32_t>(63 - lzcount);
    return mask != 0 ? TRUE : FALSE;
#endif // _MSC_VER
}

// Aligns a size_t to the specified alignment. Alignment must be a power
// of two.
inline size_t ALIGN_UP(size_t val, size_t alignment)
{
    // alignment factor must be power of two
    assert((alignment & (alignment - 1)) == 0);
    size_t result = (val + (alignment - 1)) & ~(alignment - 1);
    assert(result >= val);
    return result;
}

// Aligns a pointer to the specified alignment. Alignment must be a power
// of two.
inline uint8_t* ALIGN_UP(uint8_t* ptr, size_t alignment)
{
    size_t as_size_t = reinterpret_cast<size_t>(ptr);
    return reinterpret_cast<uint8_t*>(ALIGN_UP(as_size_t, alignment));
}

// Aligns a size_t to the specified alignment by rounding down. Alignment must
// be a power of two.
inline size_t ALIGN_DOWN(size_t val, size_t alignment)
{
    // alignment factor must be power of two.
    assert((alignment & (alignment - 1)) == 0);
    size_t result = val & ~(alignment - 1);
    return result;
}

// Aligns a pointer to the specified alignment by rounding down. Alignment
// must be a power of two.
inline uint8_t* ALIGN_DOWN(uint8_t* ptr, size_t alignment)
{
    size_t as_size_t = reinterpret_cast<size_t>(ptr);
    return reinterpret_cast<uint8_t*>(ALIGN_DOWN(as_size_t, alignment));
}

// Aligns a void pointer to the specified alignment by rounding down. Alignment
// must be a power of two.
inline void* ALIGN_DOWN(void* ptr, size_t alignment)
{
    size_t as_size_t = reinterpret_cast<size_t>(ptr);
    return reinterpret_cast<void*>(ALIGN_DOWN(as_size_t, alignment));
}

inline int GetRandomInt(int max)
{
    return rand() % max;
}

typedef struct _PROCESSOR_NUMBER {
    uint16_t Group;
    uint8_t Number;
    uint8_t Reserved;
} PROCESSOR_NUMBER, *PPROCESSOR_NUMBER;

#endif // _INC_WINDOWS

// -----------------------------------------------------------------------------------------------------------
//
// The subset of the contract code required by the GC/HandleTable sources. If Redhawk moves to support
// contracts these local definitions will disappear and be replaced by real implementations.
//

#define LEAF_CONTRACT
#define LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_DAC_CONTRACT
#define WRAPPER_CONTRACT
#define WRAPPER_NO_CONTRACT
#define STATIC_CONTRACT_LEAF
#define STATIC_CONTRACT_DEBUG_ONLY
#define STATIC_CONTRACT_NOTHROW
#define STATIC_CONTRACT_CAN_TAKE_LOCK
#define STATIC_CONTRACT_GC_NOTRIGGER
#define STATIC_CONTRACT_MODE_COOPERATIVE
#define CONTRACTL
#define CONTRACT(_expr)
#define CONTRACT_VOID
#define THROWS
#define NOTHROW
#define INSTANCE_CHECK
#define MODE_COOPERATIVE
#define MODE_ANY
#define GC_TRIGGERS
#define GC_NOTRIGGER
#define CAN_TAKE_LOCK
#define SUPPORTS_DAC
#define FORBID_FAULT
#define CONTRACTL_END
#define CONTRACT_END
#define TRIGGERSGC()
#define WRAPPER(_contract)
#define DISABLED(_contract)
#define INJECT_FAULT(_expr)
#define INJECTFAULT_GCHEAP 0x2
#define FAULT_NOT_FATAL()
#define BEGIN_DEBUG_ONLY_CODE
#define END_DEBUG_ONLY_CODE
#define BEGIN_GETTHREAD_ALLOWED
#define END_GETTHREAD_ALLOWED
#define LEAF_DAC_CONTRACT
#define PRECONDITION(_expr)
#define POSTCONDITION(_expr)
#define RETURN return
#define CONDITIONAL_CONTRACT_VIOLATION(_violation, _expr)

// -----------------------------------------------------------------------------------------------------------
//
// Data access macros
//
typedef uintptr_t TADDR;
#define PTR_TO_TADDR(ptr) ((TADDR)(ptr))

#define DPTR(type) type*
#define SPTR(type) type*
typedef DPTR(size_t)    PTR_size_t;
typedef DPTR(uint8_t)   PTR_uint8_t;

// -----------------------------------------------------------------------------------------------------------

#define DATA_ALIGNMENT sizeof(uintptr_t)
#define RAW_KEYWORD(x) x

#ifdef _MSC_VER
#define DECLSPEC_ALIGN(x)   __declspec(align(x))
#else
#define DECLSPEC_ALIGN(x)   __attribute__((aligned(x)))
#endif

#ifndef _ASSERTE
#define _ASSERTE(_expr) ASSERT(_expr)
#endif
#define CONSISTENCY_CHECK(_expr) ASSERT(_expr)
#define PREFIX_ASSUME(cond) ASSERT(cond)
#define EEPOLICY_HANDLE_FATAL_ERROR(error) ASSERT(!"EEPOLICY_HANDLE_FATAL_ERROR")
#define UI64(_literal) _literal##ULL

class ObjHeader;
class MethodTable;
class Object;
class ArrayBase;

// Various types used to refer to object references or handles. This will get more complex if we decide
// Redhawk wants to wrap object references in the debug build.
typedef DPTR(Object) PTR_Object;
typedef DPTR(PTR_Object) PTR_PTR_Object;

typedef PTR_Object OBJECTREF;
typedef PTR_PTR_Object PTR_OBJECTREF;
typedef PTR_Object _UNCHECKED_OBJECTREF;
typedef PTR_PTR_Object PTR_UNCHECKED_OBJECTREF;

// With no object reference wrapping the following macros are very simple.
#define ObjectToOBJECTREF(_obj) (OBJECTREF)(_obj)
#define OBJECTREFToObject(_obj) (Object*)(_obj)

#define VALIDATEOBJECTREF(_objref) (void)_objref;

class Thread;

inline bool dbgOnly_IsSpecialEEThread()
{
    return false;
}

#define ClrFlsSetThreadType(type)

//
// Performance logging
//

//#include "etmdummy.h"
//#define ETW_EVENT_ENABLED(e,f) false

namespace ETW
{
    typedef  enum _GC_ROOT_KIND {
        GC_ROOT_STACK = 0,
        GC_ROOT_FQ = 1,
        GC_ROOT_HANDLES = 2,
        GC_ROOT_OLDER = 3,
        GC_ROOT_SIZEDREF = 4,
        GC_ROOT_OVERFLOW = 5
    } GC_ROOT_KIND;
};

inline bool FitsInU1(uint64_t val)
{
    return val == (uint64_t)(uint8_t)val;
}

#endif // __GCENV_BASE_INCLUDED__
