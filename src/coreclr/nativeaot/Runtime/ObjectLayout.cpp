// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementations of functions dealing with object layout related types.
//
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "rhassert.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "TargetPtrs.h"
#include "MethodTable.h"
#include "ObjectLayout.h"
#include "MethodTable.inl"
#include "volatile.h"

uint32_t Array::GetArrayLength()
{
    return m_Length;
}

void* Array::GetArrayData()
{
    uint8_t* pData = (uint8_t*)this;
    pData += (GetMethodTable()->GetBaseSize() - sizeof(ObjHeader));
    return pData;
}

#ifndef DACCESS_COMPILE
void Array::SetNumComponents(uint32_t length)
{
    m_Length = length;
}

void ObjHeader::SetBit(uint32_t uBit)
{
    PalInterlockedOr(&m_uSyncBlockValue, uBit);
}

void ObjHeader::ClrBit(uint32_t uBit)
{
    PalInterlockedAnd(&m_uSyncBlockValue, ~uBit);
}

uint32_t ObjHeader::GetGCReservedBits(uint32_t mask, uint32_t shift)
{
#ifdef HOST_64BIT
    ASSERT(mask != 0);
    ASSERT(shift < 32);
    uint32_t value = static_cast<uint32_t>(PalInterlockedCompareExchange(
        reinterpret_cast<volatile int32_t*>(&m_uGCReservedBits),
        0,
        0));
    return (value & mask) >> shift;
#else
    UNREFERENCED_PARAMETER(mask);
    UNREFERENCED_PARAMETER(shift);
    return 0;
#endif
}

uint32_t ObjHeader::CompareExchangeGCReservedBits(
    uint32_t mask,
    uint32_t shift,
    uint32_t value,
    uint32_t comparand)
{
#ifdef HOST_64BIT
    ASSERT(mask != 0);
    ASSERT(shift < 32);
    ASSERT((value & ~(mask >> shift)) == 0);
    ASSERT((comparand & ~(mask >> shift)) == 0);

    uint32_t oldWord = static_cast<uint32_t>(PalInterlockedCompareExchange(
        reinterpret_cast<volatile int32_t*>(&m_uGCReservedBits),
        0,
        0));
    while (true)
    {
        uint32_t oldValue = (oldWord & mask) >> shift;
        if (oldValue != comparand)
        {
            return oldValue;
        }

        uint32_t newWord = (oldWord & ~mask) | ((value << shift) & mask);
        uint32_t observed = static_cast<uint32_t>(PalInterlockedCompareExchange(
                reinterpret_cast<volatile int32_t*>(&m_uGCReservedBits),
                static_cast<int32_t>(newWord),
                static_cast<int32_t>(oldWord)));
        if (observed == oldWord)
        {
            return comparand;
        }
        oldWord = observed;
    }
#else
    UNREFERENCED_PARAMETER(mask);
    UNREFERENCED_PARAMETER(shift);
    UNREFERENCED_PARAMETER(value);
    UNREFERENCED_PARAMETER(comparand);
    return 0;
#endif
}

uint32_t ObjHeader::SetGCReservedBits(uint32_t mask, uint32_t shift, uint32_t value)
{
#ifdef HOST_64BIT
    ASSERT(mask != 0);
    ASSERT(shift < 32);
    ASSERT((value & ~(mask >> shift)) == 0);

    uint32_t oldWord = static_cast<uint32_t>(PalInterlockedCompareExchange(
        reinterpret_cast<volatile int32_t*>(&m_uGCReservedBits),
        0,
        0));
    while (true)
    {
        uint32_t oldValue = (oldWord & mask) >> shift;
        uint32_t newWord = (oldWord & ~mask) | ((value << shift) & mask);
        uint32_t observed = static_cast<uint32_t>(PalInterlockedCompareExchange(
            reinterpret_cast<volatile int32_t*>(&m_uGCReservedBits),
            static_cast<int32_t>(newWord),
            static_cast<int32_t>(oldWord)));
        if (observed == oldWord)
        {
            return oldValue;
        }
        oldWord = observed;
    }
#else
    UNREFERENCED_PARAMETER(mask);
    UNREFERENCED_PARAMETER(shift);
    UNREFERENCED_PARAMETER(value);
    return 0;
#endif
}

uint32_t ObjHeader::WaitWhileGCReservedBits(uint32_t mask, uint32_t shift, uint32_t value)
{
#ifdef HOST_64BIT
    uint32_t iteration = 0;
    uint32_t current;
    while ((current = GetGCReservedBits(mask, shift)) == value)
    {
        if ((++iteration % 1024) != 0)
        {
            PalYieldProcessor();
        }
        else
        {
            PalSwitchToThread();
        }
    }
    return current;
#else
    UNREFERENCED_PARAMETER(mask);
    UNREFERENCED_PARAMETER(shift);
    UNREFERENCED_PARAMETER(value);
    return 0;
#endif
}

size_t Object::GetSize()
{
    MethodTable * pEEType = GetMethodTable();

    // strings have component size2, all other non-arrays should have 0
    ASSERT(( pEEType->GetComponentSize() <= 2) || pEEType->IsArray());

    size_t s = pEEType->GetBaseSize();
    if (pEEType->HasComponentSize())
        s += (size_t)((Array*)this)->GetArrayLength() * pEEType->RawGetComponentSize();

    return s;
}

#endif
