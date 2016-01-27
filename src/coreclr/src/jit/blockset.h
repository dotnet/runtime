// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

class BlockSetOps : public BitSetOps</*BitSetType*/BitSetShortLongRep, 
                  /*Brand*/BSShortLong,
                  /*Env*/Compiler*,
                  /*BitSetTraits*/BasicBlockBitSetTraits>
{
public:
    // Specialize BlockSetOps::MakeFull(). Since we number basic blocks from one, we remove bit zero from
    // the block set. Otherwise, IsEmpty() would never return true.
    static
    BitSetShortLongRep
    MakeFull(Compiler* env)
    {
        BitSetShortLongRep retval;

        // First, make a full set using the BitSetOps::MakeFull

        retval = BitSetOps</*BitSetType*/BitSetShortLongRep, 
                      /*Brand*/BSShortLong,
                      /*Env*/Compiler*,
                      /*BitSetTraits*/BasicBlockBitSetTraits>::MakeFull(env);
                      
        // Now, remove element zero, since we number basic blocks starting at one, and index the set with the
        // basic block number. If we left this, then IsEmpty() would never return true.
        BlockSetOps::RemoveElemD(env, retval, 0);

        return retval;
    }
};

typedef  BitSetShortLongRep BlockSet;

// These types should be used as the types for BlockSet arguments and return values, respectively.
typedef   BlockSetOps::ValArgType BlockSet_ValArg_T;
typedef   BlockSetOps::RetValType BlockSet_ValRet_T;

// Initialize "_varName" to "_initVal."  Copies contents, not references; if "_varName" is uninitialized, allocates a var set
// for it (using "_comp" for any necessary allocation), and copies the contents of "_initVal" into it.
#define BLOCKSET_INIT(_comp, _varName, _initVal) _varName(BlockSetOps::MakeCopy(_comp, _initVal))

// Initializes "_varName" to "_initVal", without copying: if "_initVal" is an indirect representation, copies its
// pointer into "_varName".
#define BLOCKSET_INIT_NOCOPY(_varName, _initVal) _varName(_initVal)

// The iterator pattern.

// Use this to initialize an iterator "_iterName" to iterate over a BlockSet "_blockSet".
// "_blockNum" will be an unsigned variable to which we assign the elements of "_blockSet".
#define BLOCKSET_ITER_INIT(_comp, _iterName, _blockSet, _blockNum) \
    unsigned _blockNum = 0; \
    BlockSetOps::Iter _iterName(_comp, _blockSet)

#endif // _BLOCKSET_INCLUDED_
