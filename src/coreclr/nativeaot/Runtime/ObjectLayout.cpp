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
#include "RedhawkWarnings.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "TargetPtrs.h"
#include "MethodTable.h"
#include "ObjectLayout.h"
#include "MethodTable.inl"

#ifndef DACCESS_COMPILE
void Object::InitEEType(MethodTable * pEEType)
{
    ASSERT(NULL == m_pEEType);
    m_pEEType = pEEType;
}
#endif

uint32_t Array::GetArrayLength()
{
    return m_Length;
}

void* Array::GetArrayData()
{
    uint8_t* pData = (uint8_t*)this;
    pData += (get_EEType()->get_BaseSize() - sizeof(ObjHeader));
    return pData;
}

#ifndef DACCESS_COMPILE
void Array::InitArrayLength(uint32_t length)
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

size_t Object::GetSize()
{
    MethodTable * pEEType = get_EEType();

    // strings have component size2, all other non-arrays should have 0
    ASSERT(( pEEType->get_ComponentSize() <= 2) || pEEType->IsArray());

    size_t s = pEEType->get_BaseSize();
    uint16_t componentSize = pEEType->get_ComponentSize();
    if (componentSize > 0)
        s += ((Array*)this)->GetArrayLength() * componentSize;
    return s;
}

#endif
