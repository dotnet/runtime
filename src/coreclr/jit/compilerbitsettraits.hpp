// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
void* CompAllocBitSetTraits::Alloc(Compiler* comp, size_t byteSize)
{
    return comp->getAllocator(CMK_bitset).allocate<char>(byteSize);
}

#ifdef DEBUG
// static
void* CompAllocBitSetTraits::DebugAlloc(Compiler* comp, size_t byteSize)
{
    return comp->getAllocator(CMK_DebugOnly).allocate<char>(byteSize);
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
unsigned TrackedVarBitSetTraits::GetArrSize(Compiler* comp)
{
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
    return nullptr;
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
unsigned AllVarBitSetTraits::GetArrSize(Compiler* comp)
{
    const unsigned elemBits = 8 * sizeof(size_t);
    return roundUp(GetSize(comp), elemBits) / elemBits;
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
    return nullptr;
#endif
}

///////////////////////////////////////////////////////////////////////////////
//
// BitVecTraits
//
///////////////////////////////////////////////////////////////////////////////

// static
void* BitVecTraits::Alloc(BitVecTraits* b, size_t byteSize)
{
    return b->comp->getAllocator(CMK_bitset).allocate<char>(byteSize);
}

#ifdef DEBUG
// static
void* BitVecTraits::DebugAlloc(BitVecTraits* b, size_t byteSize)
{
    return b->comp->getAllocator(CMK_DebugOnly).allocate<char>(byteSize);
}
#endif // DEBUG

// static
unsigned BitVecTraits::GetSize(BitVecTraits* b)
{
    return b->size;
}

// static
unsigned BitVecTraits::GetArrSize(BitVecTraits* b)
{
    return b->arraySize;
}

// static
unsigned BitVecTraits::GetEpoch(BitVecTraits* b)
{
    return b->size;
}

// static
BitSetSupport::BitSetOpCounter* BitVecTraits::GetOpCounter(BitVecTraits* b)
{
    return nullptr;
}

#endif // CompilerBitSetTraits_HPP_DEFINED
