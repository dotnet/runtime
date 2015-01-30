//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef CompilerBitSetTraits_HPP_DEFINED
#define CompilerBitSetTraits_HPP_DEFINED 1

#include "compilerbitsettraits.h"
#include "compiler.h"

///////////////////////////////////////////////////////////////////////////////
// 
// CompAllocBitSetTraits
// 
///////////////////////////////////////////////////////////////////////////////

// static 
IAllocator* CompAllocBitSetTraits::GetAllocator(Compiler* comp)
{
    return comp->getAllocatorBitset();
}

#ifdef DEBUG
// static 
IAllocator* CompAllocBitSetTraits::GetDebugOnlyAllocator(Compiler* comp)
{
    return comp->getAllocatorDebugOnly();
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
// 
// TrackedVarBitSetTraits
// 
///////////////////////////////////////////////////////////////////////////////

// static
unsigned TrackedVarBitSetTraits::GetSize(Compiler* comp)
{
    return comp->lvaTrackedCount;
}

// static
unsigned TrackedVarBitSetTraits::GetArrSize(Compiler* comp, unsigned elemSize)
{
    assert(elemSize == sizeof(size_t));
    return comp->lvaTrackedCountInSizeTUnits;
}

// static 
unsigned TrackedVarBitSetTraits::GetEpoch(Compiler* comp)
{
    return comp->GetCurLVEpoch();
}

// static
BitSetSupport::BitSetOpCounter* TrackedVarBitSetTraits::GetOpCounter(Compiler* comp)
{
#if VARSET_COUNTOPS
    return &Compiler::m_varsetOpCounter;
#else
    return NULL;
#endif
}

///////////////////////////////////////////////////////////////////////////////
// 
// AllVarBitSetTraits
// 
///////////////////////////////////////////////////////////////////////////////

// static
unsigned AllVarBitSetTraits::GetSize(Compiler* comp)
{
    return min(comp->lvaCount, lclMAX_ALLSET_TRACKED);
}

// static
unsigned AllVarBitSetTraits::GetArrSize(Compiler* comp, unsigned elemSize)
{
    return unsigned(roundUp(GetSize(comp), elemSize));
}

// static 
unsigned AllVarBitSetTraits::GetEpoch(Compiler* comp)
{
    return GetSize(comp);
}

// static
BitSetSupport::BitSetOpCounter* AllVarBitSetTraits::GetOpCounter(Compiler* comp)
{
#if ALLVARSET_COUNTOPS
    return &Compiler::m_allvarsetOpCounter;
#else
    return NULL;
#endif
}

///////////////////////////////////////////////////////////////////////////////
// 
// BasicBlockBitSetTraits
// 
///////////////////////////////////////////////////////////////////////////////

// static
unsigned BasicBlockBitSetTraits::GetSize(Compiler* comp)
{
    return comp->fgCurBBEpochSize;
}

// static
unsigned BasicBlockBitSetTraits::GetArrSize(Compiler* comp, unsigned elemSize)
{
    // Assert that the epoch has been initialized. This is a convenient place to assert this because
    // GetArrSize() is called for every function, via IsShort().
    assert(GetEpoch(comp) != 0);

    assert(elemSize == sizeof(size_t));
    return comp->fgBBSetCountInSizeTUnits;      // This is precomputed to avoid doing math every time this function is called
}

// static 
unsigned BasicBlockBitSetTraits::GetEpoch(Compiler* comp)
{
    return comp->GetCurBasicBlockEpoch();
}

// static
BitSetSupport::BitSetOpCounter* BasicBlockBitSetTraits::GetOpCounter(Compiler* comp)
{
    return NULL;
}

///////////////////////////////////////////////////////////////////////////////
// 
// BitVecTraits
// 
///////////////////////////////////////////////////////////////////////////////

// static
IAllocator* BitVecTraits::GetAllocator(BitVecTraits* b)
{
    return b->comp->getAllocatorBitset();
}

#ifdef DEBUG
// static
IAllocator* BitVecTraits::GetDebugOnlyAllocator(BitVecTraits* b)
{
    return b->comp->getAllocatorDebugOnly();
}
#endif // DEBUG

// static
unsigned BitVecTraits::GetSize(BitVecTraits* b)
{
    return b->size;
}

// static
unsigned BitVecTraits::GetArrSize(BitVecTraits* b, unsigned elemSize)
{
    assert(elemSize == sizeof(size_t));
    unsigned elemBits = 8 * elemSize;
    return (unsigned) roundUp(b->size, elemBits)/elemBits;
}

// static
unsigned BitVecTraits::GetEpoch(BitVecTraits* b)
{
    return b->size;
}

// static
BitSetSupport::BitSetOpCounter* BitVecTraits::GetOpCounter(BitVecTraits* b)
{
    return NULL;
}

#endif // CompilerBitSetTraits_HPP_DEFINED
