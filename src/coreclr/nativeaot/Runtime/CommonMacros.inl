// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCENV_BASE_INCLUDED__

//
// This macro returns val rounded up as necessary to be a multiple of alignment; alignment must be a power of 2
//
inline uintptr_t ALIGN_UP( uintptr_t val, uintptr_t alignment )
{
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    ASSERT( 0 == (alignment & (alignment - 1)) );
    uintptr_t result = (val + (alignment - 1)) & ~(alignment - 1);
    ASSERT( result >= val );      // check for overflow

    return result;
}

template <typename T>
inline T* ALIGN_UP(T* val, uintptr_t alignment)
{
    return reinterpret_cast<T*>(ALIGN_UP(reinterpret_cast<uintptr_t>(val), alignment));
}

inline uintptr_t ALIGN_DOWN( uintptr_t val, uintptr_t alignment )
{
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    ASSERT( 0 == (alignment & (alignment - 1)) );
    uintptr_t result = val & ~(alignment - 1);
    return result;
}

template <typename T>
inline T* ALIGN_DOWN(T* val, uintptr_t alignment)
{
    return reinterpret_cast<T*>(ALIGN_DOWN(reinterpret_cast<uintptr_t>(val), alignment));
}

#endif // !__GCENV_BASE_INCLUDED__

inline bool IS_ALIGNED(uintptr_t val, uintptr_t alignment)
{
    ASSERT(0 == (alignment & (alignment - 1)));
    return 0 == (val & (alignment - 1));
}

template <typename T>
inline bool IS_ALIGNED(T* val, uintptr_t alignment)
{
    ASSERT(0 == (alignment & (alignment - 1)));
    return IS_ALIGNED(reinterpret_cast<uintptr_t>(val), alignment);
}

// Convert from a PCODE to the corresponding PINSTR.  On many architectures this will be the identity function;
// on ARM, this will mask off the THUMB bit.
inline TADDR PCODEToPINSTR(PCODE pc)
{
#ifdef TARGET_ARM
    return dac_cast<TADDR>(pc & ~THUMB_CODE);
#else
    return dac_cast<TADDR>(pc);
#endif
}

// Convert from a PINSTR to the corresponding PCODE.  On many architectures this will be the identity function;
// on ARM, this will raise the THUMB bit.
inline PCODE PINSTRToPCODE(TADDR addr)
{
#ifdef TARGET_ARM
    return dac_cast<PCODE>(addr | THUMB_CODE);
#else
    return dac_cast<PCODE>(addr);
#endif
}
