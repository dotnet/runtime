// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CompilerBitSetTraits_DEFINED
#define CompilerBitSetTraits_DEFINED 1

#include "bitset.h"
#include "compiler.h"
#include "bitsetasshortlong.h"

///////////////////////////////////////////////////////////////////////////////
//
// CompAllocBitSetTraits: a base class for other BitSet traits classes.
//
// The classes in this file define "BitSetTraits" arguments to the "BitSetOps" type, ones that assume that
// Compiler* is the "Env" type.
//
// This class just wraps the compiler's allocator.
//
class CompAllocBitSetTraits
{
public:
    static inline void* Alloc(Compiler* comp, size_t byteSize);

#ifdef DEBUG
    static inline void* DebugAlloc(Compiler* comp, size_t byteSize);
#endif // DEBUG
};

///////////////////////////////////////////////////////////////////////////////
//
// TrackedVarBitSetTraits
//
// This class is customizes the bit set to represent sets of tracked local vars.
// The size of the bitset is determined by the # of tracked locals (up to some internal
// maximum), and the Compiler* tracks the tracked local epochs.
//
class TrackedVarBitSetTraits : public CompAllocBitSetTraits
{
public:
    static inline unsigned GetSize(Compiler* comp);

    static inline unsigned GetArrSize(Compiler* comp);

    static inline unsigned GetEpoch(class Compiler* comp);

    static inline BitSetSupport::BitSetOpCounter* GetOpCounter(Compiler* comp);
};

///////////////////////////////////////////////////////////////////////////////
//
// AllVarBitSetTraits
//
// This class is customizes the bit set to represent sets of all local vars (tracked or not) --
// at least up to some maximum index.  (This index is private to the Compiler, and it is
// the responsibility of the compiler not to use indices >= this maximum.)
// We rely on the fact that variables are never deleted, and therefore use the
// total # of locals as the epoch number (up to the maximum).
//
class AllVarBitSetTraits : public CompAllocBitSetTraits
{
public:
    static inline unsigned GetSize(Compiler* comp);

    static inline unsigned GetArrSize(Compiler* comp);

    static inline unsigned GetEpoch(class Compiler* comp);

    static inline BitSetSupport::BitSetOpCounter* GetOpCounter(Compiler* comp);
};

///////////////////////////////////////////////////////////////////////////////
//
// BasicBlockBitSetTraits
//
// This class is customizes the bit set to represent sets of BasicBlocks.
// The size of the bitset is determined by maximum assigned BasicBlock number
// (Compiler::fgBBNumMax) (Note that fgBBcount is not equal to this during inlining,
// when fgBBcount is the number of blocks in the inlined function, but the assigned
// block numbers are higher than the inliner function. fgBBNumMax counts both.
// Thus, if you only care about the inlinee, during inlining, this bit set will waste
// the lower numbered block bits.) The Compiler* tracks the BasicBlock epochs.
//
class BasicBlockBitSetTraits : public CompAllocBitSetTraits
{
public:
    static inline unsigned GetSize(Compiler* comp);

    static inline unsigned GetArrSize(Compiler* comp);

    static inline unsigned GetEpoch(class Compiler* comp);

    static inline BitSetSupport::BitSetOpCounter* GetOpCounter(Compiler* comp);
};

///////////////////////////////////////////////////////////////////////////////
//
// BitVecTraits
//
// This class simplifies creation and usage of "ShortLong" bitsets.
//
struct BitVecTraits
{
private:
    unsigned  size;
    unsigned  arraySize; // pre-computed to avoid computation in GetArrSize
    Compiler* comp;

public:
    BitVecTraits(unsigned size, Compiler* comp) : size(size), comp(comp)
    {
        const unsigned elemBits = 8 * sizeof(size_t);
        arraySize               = roundUp(size, elemBits) / elemBits;
    }

    static inline void* Alloc(BitVecTraits* b, size_t byteSize);

#ifdef DEBUG
    static inline void* DebugAlloc(BitVecTraits* b, size_t byteSize);
#endif // DEBUG

    static inline unsigned GetSize(BitVecTraits* b);

    static inline unsigned GetArrSize(BitVecTraits* b);

    static inline unsigned GetEpoch(BitVecTraits* b);

    static inline BitSetSupport::BitSetOpCounter* GetOpCounter(BitVecTraits* b);
};

#endif // CompilerBitSetTraits_DEFINED
