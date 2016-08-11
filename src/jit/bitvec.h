// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

// Initialize "_varName" to "_initVal."  Copies contents, not references; if "_varName" is uninitialized, allocates a
// set for it (using "_traits" for any necessary allocation), and copies the contents of "_initVal" into it.
#define BITVEC_INIT(_traits, _varName, _initVal) _varName(BitVecOps::MakeCopy(_traits, _initVal))

// Initializes "_varName" to "_initVal", without copying: if "_initVal" is an indirect representation, copies its
// pointer into "_varName".
#define BITVEC_INIT_NOCOPY(_varName, _initVal) _varName(_initVal)

// The iterator pattern.

// Use this to initialize an iterator "_iterName" to iterate over a BitVec "_bitVec".
// "_bitNum" will be an unsigned variable to which we assign the elements of "_bitVec".
#define BITVEC_ITER_INIT(_traits, _iterName, _bitVec, _bitNum)                                                         \
    unsigned        _bitNum = 0;                                                                                       \
    BitVecOps::Iter _iterName(_traits, _bitVec)

#endif // _BITVEC_INCLUDED_
