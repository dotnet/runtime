// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//
// common.h - precompiled headers include for the COM+ Execution Engine
//

//
// Make sure _ASSERTE is defined before including this header file
// Other than that, please keep this header self-contained so that it can be included in
//  all dlls
//


#ifndef _stdmacros_h_
#define _stdmacros_h_

#include "specstrings.h"
#include "contract.h"

#ifndef _ASSERTE
#error Please define _ASSERTE before including StdMacros.h
#endif

#ifdef _DEBUG
#define     DEBUG_ARG(x)  , x
#define     DEBUG_ARG1(x)  x
#else
#define     DEBUG_ARG(x) 
#define     DEBUG_ARG1(x)
#endif

#ifdef DACCESS_COMPILE
#define     DAC_ARG(x)  , x
#else
#define     DAC_ARG(x) 
#endif


/********************************************/
/*         Portability macros               */
/********************************************/

#ifdef _TARGET_AMD64_
#define AMD64_FIRST_ARG(x)  x ,
#define AMD64_ARG(x)        , x
#define AMD64_ONLY(x)       x
#define NOT_AMD64(x)
#define NOT_AMD64_ARG(x)
#else
#define AMD64_FIRST_ARG(x)
#define AMD64_ARG(x)
#define AMD64_ONLY(x)
#define NOT_AMD64(x)        x
#define NOT_AMD64_ARG(x)    , x
#endif

#ifdef _TARGET_X86_
#define X86_FIRST_ARG(x)    x ,
#define X86_ARG(x)          , x
#define X86_ONLY(x)         x
#define NOT_X86(x)
#define NOT_X86_ARG(x)
#else
#define X86_FIRST_ARG(x)
#define X86_ARG(x)
#define X86_ONLY(x)
#define NOT_X86(x)          x
#define NOT_X86_ARG(x)      , x
#endif

#ifdef _WIN64
#define WIN64_ARG(x)  , x 
#define WIN64_ONLY(x) x 
#define NOT_WIN64(x)
#define NOT_WIN64_ARG(x)
#else
#define WIN64_ARG(x)
#define WIN64_ONLY(x) 
#define NOT_WIN64(x)    x
#define NOT_WIN64_ARG(x)    , x
#endif // _WIN64

#ifdef _TARGET_ARM_
#define ARM_FIRST_ARG(x)  x ,
#define ARM_ARG(x)        , x
#define ARM_ONLY(x)       x
#define NOT_ARM(x)
#define NOT_ARM_ARG(x)
#else
#define ARM_FIRST_ARG(x)
#define ARM_ARG(x)
#define ARM_ONLY(x)
#define NOT_ARM(x)        x
#define NOT_ARM_ARG(x)    , x
#endif

#ifdef _TARGET_ARM64_
#define ARM64_FIRST_ARG(x)  x ,
#define ARM64_ARG(x)        , x
#define ARM64_ONLY(x)       x
#define NOT_ARM64(x)
#define NOT_ARM64_ARG(x)
#else
#define ARM64_FIRST_ARG(x)
#define ARM64_ARG(x)
#define ARM64_ONLY(x)
#define NOT_ARM64(x)        x
#define NOT_ARM64_ARG(x)    , x
#endif

#ifdef _TARGET_64BIT_
#define LOG2_PTRSIZE 3
#else
#define LOG2_PTRSIZE 2
#endif

#ifdef _WIN64
    #define INVALID_POINTER_CC 0xcccccccccccccccc
    #define INVALID_POINTER_CD 0xcdcdcdcdcdcdcdcd
    #define FMT_ADDR           " %08x`%08x "
    #define LFMT_ADDR          W(" %08x`%08x ")
    #define DBG_ADDR(ptr)      (((UINT_PTR) (ptr)) >> 32), (((UINT_PTR) (ptr)) & 0xffffffff)
#else // _WIN64
    #define INVALID_POINTER_CC 0xcccccccc
    #define INVALID_POINTER_CD 0xcdcdcdcd
    #define FMT_ADDR           " %08x "
    #define LFMT_ADDR          W(" %08x ")
    #define DBG_ADDR(ptr)      ((UINT_PTR)(ptr))
#endif // _WIN64

#ifdef _TARGET_ARM_
    #define ALIGN_ACCESS        ((1<<LOG2_PTRSIZE)-1)
#endif


#ifndef ALLOC_ALIGN_CONSTANT
#define ALLOC_ALIGN_CONSTANT (sizeof(void*)-1)
#endif


inline void *GetTopMemoryAddress(void)
{
    WRAPPER_NO_CONTRACT;
    
    static void *result; // = NULL;
    if( NULL == result )
    {
        SYSTEM_INFO sysInfo;
        GetSystemInfo( &sysInfo );
        result = sysInfo.lpMaximumApplicationAddress;
    }
    return result;
}
inline void *GetBotMemoryAddress(void)
{
    WRAPPER_NO_CONTRACT;
    
    static void *result; // = NULL;
    if( NULL == result )
    {
        SYSTEM_INFO sysInfo;
        GetSystemInfo( &sysInfo );
        result = sysInfo.lpMinimumApplicationAddress;
    }
    return result;
}

#define TOP_MEMORY (GetTopMemoryAddress())
#define BOT_MEMORY (GetBotMemoryAddress())


//
// This macro returns val rounded up as necessary to be a multiple of alignment; alignment must be a power of 2
//
inline size_t ALIGN_UP( size_t val, size_t alignment )
{
    LIMITED_METHOD_DAC_CONTRACT;
    
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    _ASSERTE( 0 == (alignment & (alignment - 1)) ); 
    size_t result = (val + (alignment - 1)) & ~(alignment - 1);
    _ASSERTE( result >= val );      // check for overflow
    return result;
}
inline void* ALIGN_UP( void* val, size_t alignment )
{
    WRAPPER_NO_CONTRACT;
    
    return (void*) ALIGN_UP( (size_t)val, alignment );
}
inline uint8_t* ALIGN_UP( uint8_t* val, size_t alignment )
{
    WRAPPER_NO_CONTRACT;
    
    return (uint8_t*) ALIGN_UP( (size_t)val, alignment );
}

inline size_t ALIGN_DOWN( size_t val, size_t alignment )
{
    LIMITED_METHOD_CONTRACT;
    
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    _ASSERTE( 0 == (alignment & (alignment - 1)) );
    size_t result = val & ~(alignment - 1);
    return result;
}
inline void* ALIGN_DOWN( void* val, size_t alignment )
{
    WRAPPER_NO_CONTRACT;
    return (void*) ALIGN_DOWN( (size_t)val, alignment );
}
inline uint8_t* ALIGN_DOWN( uint8_t* val, size_t alignment )
{
    WRAPPER_NO_CONTRACT;
    return (uint8_t*) ALIGN_DOWN( (size_t)val, alignment );
}

inline BOOL IS_ALIGNED( size_t val, size_t alignment )
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    _ASSERTE( 0 == (alignment & (alignment - 1)) ); 
    return 0 == (val & (alignment - 1));
}
inline BOOL IS_ALIGNED( const void* val, size_t alignment )
{
    WRAPPER_NO_CONTRACT;
    return IS_ALIGNED( (size_t) val, alignment );
}

// Rounds a ULONG up to the nearest power of two number.
inline ULONG RoundUpToPower2(ULONG x) 
{
    if (x == 0) return 1;

    x = x - 1;
    x = x | (x >> 1);
    x = x | (x >> 2);
    x = x | (x >> 4);
    x = x | (x >> 8);
    x = x | (x >> 16);
    return x + 1;
}

#ifdef ALIGN_ACCESS

// NOTE: pSrc is evaluated three times!!!
#define MAYBE_UNALIGNED_READ(pSrc, bits)        (IS_ALIGNED((size_t)(pSrc), sizeof(UINT##bits)) ? \
                                                    (*(UINT##bits*)      (pSrc)) : \
                                                    (GET_UNALIGNED_##bits(pSrc)) )

#define MAYBE_UNALIGNED_WRITE(pDst, bits, expr) do { if (IS_ALIGNED((size_t)(pDst), sizeof(UINT##bits))) \
                                                    *(UINT##bits*)(pDst) = (UINT##bits)(expr); else \
                                                    SET_UNALIGNED_##bits(pDst, (UINT##bits)(expr)); } while (0)

// these are necessary for MAYBE_UNALIGNED_XXX to work with UINT_PTR
#define GET_UNALIGNED__PTR(x) GET_UNALIGNED_PTR(x)
#define SET_UNALIGNED__PTR(p,x) SET_UNALIGNED_PTR(p,x)

#else // ALIGN_ACCESS
#define MAYBE_UNALIGNED_READ(pSrc, bits)        (*(UINT##bits*)(pSrc))
#define MAYBE_UNALIGNED_WRITE(pDst, bits, expr) do { *(UINT##bits*)(pDst) = (UINT##bits)(expr); } while(0)
#endif // ALIGN_ACCESS

//
// define some useful macros for logging object
//

#define FMT_OBJECT  "object" FMT_ADDR
#define FMT_HANDLE  "handle" FMT_ADDR
#define FMT_CLASS   "%s"
#define FMT_REG     "r%d "
#define FMT_STK     "sp%s0x%02x "
#define FMT_PIPTR   "%s%s pointer "


#define DBG_GET_CLASS_NAME(pMT)        \
        (((pMT) == NULL)  ? NULL : (pMT)->GetClass()->GetDebugClassName())

#define DBG_CLASS_NAME_MT(pMT)         \
        (DBG_GET_CLASS_NAME(pMT) == NULL) ? "<null-class>" : DBG_GET_CLASS_NAME(pMT) 

#define DBG_GET_MT_FROM_OBJ(obj)       \
        (MethodTable*)((size_t)((Object*) (obj))->GetGCSafeMethodTable()) 

#define DBG_CLASS_NAME_OBJ(obj)        \
        ((obj) == NULL)  ? "null" : DBG_CLASS_NAME_MT(DBG_GET_MT_FROM_OBJ(obj)) 

#define DBG_CLASS_NAME_IPTR2(obj,iptr) \
        ((iptr) != 0)    ? ""     : DBG_CLASS_NAME_MT(DBG_GET_MT_FROM_OBJ(obj))

#define DBG_CLASS_NAME_IPTR(obj,iptr)  \
        ((obj)  == NULL) ? "null" : DBG_CLASS_NAME_IPTR2(obj,iptr)

#define DBG_STK(off)                   \
        (off >= 0) ? "+" : "-",        \
        (off >= 0) ? off : -off

#define DBG_PIN_NAME(pin)              \
        (pin)  ? "pinned "  : ""

#define DBG_IPTR_NAME(iptr)            \
        (iptr) ? "interior" : "base"

#define LOG_HANDLE_OBJECT_CLASS(str1, hnd, str2, obj)    \
        str1 FMT_HANDLE str2 FMT_OBJECT FMT_CLASS "\n",  \
        DBG_ADDR(hnd), DBG_ADDR(obj), DBG_CLASS_NAME_OBJ(obj)

#define LOG_OBJECT_CLASS(obj)                            \
        FMT_OBJECT FMT_CLASS "\n",                       \
        DBG_ADDR(obj), DBG_CLASS_NAME_OBJ(obj)

#define LOG_PIPTR_OBJECT_CLASS(obj, pin, iptr)           \
        FMT_PIPTR FMT_ADDR FMT_CLASS "\n",               \
        DBG_PIN_NAME(pin), DBG_IPTR_NAME(iptr),          \
        DBG_ADDR(obj), DBG_CLASS_NAME_IPTR(obj,iptr)

#define LOG_HANDLE_OBJECT(str1, hnd, str2, obj)          \
        str1 FMT_HANDLE str2 FMT_OBJECT "\n",            \
        DBG_ADDR(hnd), DBG_ADDR(obj)

#define LOG_PIPTR_OBJECT(obj, pin, iptr)                 \
        FMT_PIPTR FMT_ADDR "\n",                         \
        DBG_PIN_NAME(pin), DBG_IPTR_NAME(iptr),          \
        DBG_ADDR(obj)

#define UNIQUE_LABEL_DEF(a,x)           a##x
#define UNIQUE_LABEL_DEF_X(a,x)         UNIQUE_LABEL_DEF(a,x)
#ifdef _MSC_VER
#define UNIQUE_LABEL(a)                 UNIQUE_LABEL_DEF_X(_unique_label_##a##_, __COUNTER__)
#else
#define UNIQUE_LABEL(a)                 UNIQUE_LABEL_DEF_X(_unique_label_##a##_, __LINE__)
#endif


#ifndef _countof
#define _countof(_array) (sizeof(_array)/sizeof(_array[0]))
#endif


// This is temporary.  LKG should provide these macros and we should then 
// remove STRUNCATE and _TRUNCATE from here.

/* error codes */
#if !defined(STRUNCATE)
#define STRUNCATE       80
#endif

/* _TRUNCATE */
#if !defined(_TRUNCATE)
#define _TRUNCATE ((size_t)-1)
#endif

#endif //_stdmacros_h_
