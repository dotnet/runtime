// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This include file determines how VARSET_TP is implemented.
//
#ifndef _VARSET_INCLUDED_
#define _VARSET_INCLUDED_ 1

// A VARSET_TP is a set of (small) integers representing local variables.
// We implement varsets using the BitSet abstraction, which supports
// several different implementations.
// 
// The set of tracked variables may change during a compilation, and variables may be
// re-sorted, so the tracked variable index of a variable is decidedly *not* stable.  The
// bitset abstraction supports labeling of bitsets with "epochs", and supports a
// debugging mode in which live bitsets must have the current epoch.  To use this feature,
// divide a compilation up into epochs, during which tracked variable indices are
// stable.

// Some implementations of BitSet may use a level of indirection.  Therefore, we
// must be careful about about assignment and initialization.  We often want to
// reason about VARSET_TP as immutable values, and just copying the contents would
// introduce sharing in the indirect case, which is usually not what's desired.  On
// the other hand, there are many cases in which the RHS value has just been
// created functionally, and the intialization/assignment is obviously its last
// use.  In these cases, allocating a new indirect representation for the lhs (if
// it does not already have one) would be unnecessary and wasteful.  Thus, for both
// initialization and assignment, we have normal versions, which do make copies to
// prevent sharing and definitely preserve value semantics, and "NOCOPY" versions,
// which do not.  Obviously, the latter should be used with care.

#include "bitset.h"
#include "compilerbitsettraits.h"

const unsigned UInt64Bits = sizeof(UINT64) * 8;

// This #define chooses the BitSet representation used for VARSET.
// The choices are defined in "bitset.h"; they currently include
// BSUInt64, BSShortLong, and BSUInt64Class.
#define VARSET_REP BSShortLong

#if VARSET_REP == BSUInt64

#include "bitsetasuint64.h"

typedef BitSetOps</*BitSetType*/UINT64, 
                  /*Brand*/VARSET_REP,
                  /*Env*/Compiler*,
                  /*BitSetTraits*/TrackedVarBitSetTraits>
        VarSetOpsRaw;

typedef UINT64 VARSET_TP;

const unsigned lclMAX_TRACKED = UInt64Bits;

#define VARSET_REP_IS_CLASS 0

#elif VARSET_REP == BSShortLong

#include "bitsetasshortlong.h"

typedef BitSetOps</*BitSetType*/BitSetShortLongRep, 
                  /*Brand*/VARSET_REP,
                  /*Env*/Compiler*,
                  /*BitSetTraits*/TrackedVarBitSetTraits>
        VarSetOpsRaw;

typedef  BitSetShortLongRep VARSET_TP;

// Tested various sizes for max tracked locals. The largest value for which no throughput regression
// could be measured was 512. Going to 1024 showed the first throughput regressions.
// We anticipate the larger size will be needed to support better inlining.
// There were a number of failures when 512 was used for legacy, so we just retain the 128 value
// for legacy backend.
 
#if !defined(LEGACY_BACKEND)
const unsigned lclMAX_TRACKED = 512;
#else
const unsigned lclMAX_TRACKED = 128;
#endif


#define VARSET_REP_IS_CLASS 0

#elif VARSET_REP == BSUInt64Class

#include "bitsetasuint64inclass.h"

typedef BitSetOps</*BitSetType*/BitSetUint64<Compiler*, TrackedVarBitSetTraits>, 
                  /*Brand*/VARSET_REP,
                  /*Env*/Compiler*,
                  /*BitSetTraits*/TrackedVarBitSetTraits>
        VarSetOpsRaw;

typedef   BitSetUint64<Compiler*, TrackedVarBitSetTraits> VARSET_TP;

const unsigned lclMAX_TRACKED = UInt64Bits;

#define VARSET_REP_IS_CLASS 1

#else

#error "Unrecognized BitSet implemention for VarSet."

#endif

// These types should be used as the types for VARSET_TP arguments and return values, respectively.
typedef   VarSetOpsRaw::ValArgType VARSET_VALARG_TP;
typedef   VarSetOpsRaw::RetValType VARSET_VALRET_TP;

#define VARSET_COUNTOPS 0
#if VARSET_COUNTOPS
typedef BitSetOpsWithCounter<VARSET_TP, VARSET_REP, Compiler*, TrackedVarBitSetTraits, VARSET_VALARG_TP, VARSET_VALRET_TP, VarSetOpsRaw::Iter> VarSetOps;
#else
typedef VarSetOpsRaw VarSetOps;
#endif

#define ALLVARSET_REP BSUInt64

#if ALLVARSET_REP == BSUInt64

#include "bitsetasuint64.h"

typedef BitSetOps</*BitSetType*/UINT64, 
                  /*Brand*/ALLVARSET_REP,
                  /*Env*/Compiler*,
                  /*BitSetTraits*/AllVarBitSetTraits>
        AllVarSetOps;

typedef   UINT64   ALLVARSET_TP;

const unsigned lclMAX_ALLSET_TRACKED = UInt64Bits; 

#define ALLVARSET_REP_IS_CLASS 0

#elif ALLVARSET_REP == BSShortLong

#include "bitsetasshortlong.h"

typedef BitSetOps</*BitSetType*/BitSetShortLongRep,
                  /*Brand*/ALLVARSET_REP,
                  /*Env*/Compiler*,
                  /*BitSetTraits*/AllVarBitSetTraits>
        AllVarSetOps;

typedef  BitSetShortLongRep ALLVARSET_TP;

const unsigned lclMAX_ALLSET_TRACKED = lclMAX_TRACKED; 

#define ALLVARSET_REP_IS_CLASS 0

#elif ALLVARSET_REP == BSUInt64Class

#include "bitsetasuint64inclass.h"

typedef BitSetOps</*BitSetType*/BitSetUint64<Compiler*, AllVarBitSetTraits>,
                  /*Brand*/ALLVARSET_REP,
                  /*Env*/Compiler*,
                  /*BitSetTraits*/AllVarBitSetTraits>
        AllVarSetOps;

typedef  BitSetUint64<Compiler*, AllVarBitSetTraits> ALLVARSET_TP;

const unsigned lclMAX_ALLSET_TRACKED = UInt64Bits; 

#define ALLVARSET_REP_IS_CLASS 1

#else
#error "Unrecognized BitSet implemention for AllVarSet."
#endif

// These types should be used as the types for VARSET_TP arguments and return values, respectively.
typedef   AllVarSetOps::ValArgType ALLVARSET_VALARG_TP;
typedef   AllVarSetOps::RetValType ALLVARSET_VALRET_TP;


// Initialize "varName" to "initVal."  Copies contents, not references; if "varName" is uninitialized, allocates a var set
// for it (using "comp" for any necessary allocation), and copies the contents of "initVal" into it.
#define VARSET_INIT(comp, varName, initVal) varName(VarSetOps::MakeCopy(comp, initVal))
#define ALLVARSET_INIT(comp, varName, initVal) varName(AllVarSetOps::MakeCopy(comp, initVal))

// Initializes "varName" to "initVal", without copying: if "initVal" is an indirect representation, copies its
// pointer into "varName".
#if defined(DEBUG) && VARSET_REP_IS_CLASS
#define VARSET_INIT_NOCOPY(varName, initVal) varName(initVal, 0)
#else
#define VARSET_INIT_NOCOPY(varName, initVal) varName(initVal)
#endif

#if defined(DEBUG) && ALLVARSET_REP_IS_CLASS
#define ALLVARSET_INIT_NOCOPY(varName, initVal) varName(initVal, 0)
#else
#define ALLVARSET_INIT_NOCOPY(varName, initVal) varName(initVal)
#endif


// The iterator pattern.

// Use this to initialize an iterator "iterName" to iterate over a VARSET_TP "vs".
// "varIndex" will be an unsigned variable to which we assign the elements of "vs".
#define VARSET_ITER_INIT(comp, iterName, vs, varIndex) \
    unsigned varIndex = 0; \
    VarSetOps::Iter iterName(comp, vs)

#endif // _VARSET_INCLUDED_
