// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File: convert.h
// 
// ===========================================================================

/***
*Purpose:
*  Common header (shared by convert.cpp and decimal.cpp) for numeric
*  conversions and other math stuff.
*
*Revision History:
*
*  
*
*Implementation Notes:
*
*****************************************************************************/

#ifndef _CONVERT_H_ /* { */
#define _CONVERT_H_

//***********************************************************************
//
// Structures
//

typedef union{
    struct {
#if BIGENDIAN
      ULONG sign:1;
      ULONG exp:11;
      ULONG mantHi:20;
      ULONG mantLo;
#else // BIGENDIAN
      ULONG mantLo;
      ULONG mantHi:20;
      ULONG exp:11;
      ULONG sign:1;
#endif
    } u;
    double dbl;
} DBLSTRUCT;

// Intializer for a DBLSTRUCT
#if BIGENDIAN
#define DEFDS(Lo, Hi, exp, sign) { {sign, exp, Hi, Lo } }
#else
#define DEFDS(Lo, Hi, exp, sign) { {Lo, Hi, exp, sign} }
#endif


typedef struct {
#if BIGENDIAN
    ULONG sign:1;
    ULONG exp:8;
    ULONG mant:23;
#else
    ULONG mant:23;
    ULONG exp:8;
    ULONG sign:1;
#endif
} SNGSTRUCT;



typedef union {
    DWORDLONG int64;
    struct {
#ifdef BIGENDIAN
        ULONG Hi;
        ULONG Lo;
#else
        ULONG Lo;
        ULONG Hi;
#endif
    } u;
} SPLIT64;



//***********************************************************************
//
// Constants
//

static const ULONG ulTenToTenDiv4 = 2500000000U;
static const ULONG ulTenToNine    = 1000000000U;

//***********************************************************************
//
// Inlines for Decimal
//


#ifndef UInt32x32To64
#define UInt32x32To64(a, b) ((DWORDLONG)((DWORD)(a)) * (DWORDLONG)((DWORD)(b)))
#endif

#define Div64by32(num, den) ((ULONG)((DWORDLONG)(num) / (ULONG)(den)))
#define Mod64by32(num, den) ((ULONG)((DWORDLONG)(num) % (ULONG)(den)))

inline DWORDLONG DivMod32by32(ULONG num, ULONG den)
{
    SPLIT64  sdl;

    sdl.u.Lo = num / den;
    sdl.u.Hi = num % den;
    return sdl.int64;
}

inline DWORDLONG DivMod64by32(DWORDLONG num, ULONG den)
{
    SPLIT64  sdl;

    sdl.u.Lo = Div64by32(num, den);
    sdl.u.Hi = Mod64by32(num, den);
    return sdl.int64;
}

#endif /* } _CONVERT_H_ */
