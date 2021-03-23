// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This include file determines how BlockSet is implemented.
//
#ifndef _BLOCKSET_INCLUDED_
#define _BLOCKSET_INCLUDED_ 1

// A BlockSet is a set of BasicBlocks, represented by the BasicBlock number (bbNum).
// Unlike VARSET_TP, we only support a single implementation: the bitset "shortlong"
// implementation.
//
// Note that BasicBlocks in the JIT are numbered starting at 1. We always just waste the
// 0th bit to avoid having to do "bbNum - 1" calculations everywhere (at the BlockSet call
// sites). This makes reading the code easier, and avoids potential problems of forgetting
// to do a "- 1" somewhere.
//
// Basic blocks can be renumbered during compilation, so it is important to not mix
// BlockSets created before and after a renumbering. Every time the blocks are renumbered
// creates a different "epoch", during which the basic block numbers are stable.

#include "bitset.h"
#include "compilerbitsettraits.h"
#include "bitsetasshortlong.h"

class BlockSetOps : public BitSetOps</*BitSetType*/ BitSetShortLongRep,
                                     /*Brand*/ BSShortLong,
                                     /*Env*/ Compiler*,
                                     /*BitSetTraits*/ BasicBlockBitSetTraits>
{
public:
    // Specialize BlockSetOps::MakeFull(). Since we number basic blocks from one, we remove bit zero from
    // the block set. Otherwise, IsEmpty() would never return true.
    static BitSetShortLongRep MakeFull(Compiler* env)
    {
        BitSetShortLongRep retval;

        // First, make a full set using the BitSetOps::MakeFull

        retval = BitSetOps</*BitSetType*/ BitSetShortLongRep,
                           /*Brand*/ BSShortLong,
                           /*Env*/ Compiler*,
                           /*BitSetTraits*/ BasicBlockBitSetTraits>::MakeFull(env);

        // Now, remove element zero, since we number basic blocks starting at one, and index the set with the
        // basic block number. If we left this, then IsEmpty() would never return true.
        BlockSetOps::RemoveElemD(env, retval, 0);

        return retval;
    }
};

typedef BitSetShortLongRep BlockSet;

// These types should be used as the types for BlockSet arguments and return values, respectively.
typedef BlockSetOps::ValArgType BlockSet_ValArg_T;
typedef BlockSetOps::RetValType BlockSet_ValRet_T;

#endif // _BLOCKSET_INCLUDED_
