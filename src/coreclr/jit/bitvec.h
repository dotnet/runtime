// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This include file determines how BitVec is implemented.
//
#ifndef _BITVEC_INCLUDED_
#define _BITVEC_INCLUDED_ 1

// This class simplifies creation and usage of "ShortLong" bitsets.
//
// Create new bitsets like so:
//
//   BitVecTraits traits(size, pCompiler);
//   BitVec bitvec = BitVecOps::MakeEmpty(&traits);
//
// and call functions like so:
//
//   BitVecOps::AddElemD(&traits, bitvec, 10);
//   BitVecOps::IsMember(&traits, bitvec, 10));
//

#include "bitset.h"
#include "compilerbitsettraits.h"
#include "bitsetasshortlong.h"

typedef BitSetOps</*BitSetType*/ BitSetShortLongRep,
                  /*Brand*/ BSShortLong,
                  /*Env*/ BitVecTraits*,
                  /*BitSetTraits*/ BitVecTraits>
    BitVecOps;

typedef BitSetShortLongRep BitVec;

// These types should be used as the types for BitVec arguments and return values, respectively.
typedef BitVecOps::ValArgType BitVec_ValArg_T;
typedef BitVecOps::RetValType BitVec_ValRet_T;

#endif // _BITVEC_INCLUDED_
