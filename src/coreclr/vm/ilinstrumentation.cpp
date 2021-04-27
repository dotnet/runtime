// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: ILInstrumentation.cpp
//
// ===========================================================================


#include "common.h"
#include "ilinstrumentation.h"


//---------------------------------------------------------------------------------------
InstrumentedILOffsetMapping::InstrumentedILOffsetMapping()
{
    LIMITED_METHOD_DAC_CONTRACT;

    m_cMap = 0;
    m_rgMap = NULL;
    _ASSERTE(IsNull());
}

//---------------------------------------------------------------------------------------
//
// Check whether there is any mapping information stored in this object.
//
// Notes:
//    The memory should be alive throughout the process lifetime until
//    the Module containing the instrumented method is destructed.
//

BOOL InstrumentedILOffsetMapping::IsNull() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE((m_cMap == 0) == (m_rgMap == NULL));
    return (m_cMap == 0);
}

#if !defined(DACCESS_COMPILE)
//---------------------------------------------------------------------------------------
//
// Release the memory used by the array of COR_IL_MAPs.
//
// Notes:
//    * The memory should be alive throughout the process lifetime until the Module containing
//      the instrumented method is destructed.
//    * This struct should be read-only in DAC builds.
//

void InstrumentedILOffsetMapping::Clear()
{
    LIMITED_METHOD_CONTRACT;

    if (m_rgMap != NULL)
    {
        delete[] m_rgMap;
    }

    m_cMap = 0;
    m_rgMap = NULL;
}
#endif // !DACCESS_COMPILE

#if !defined(DACCESS_COMPILE)
void InstrumentedILOffsetMapping::SetMappingInfo(SIZE_T cMap, COR_IL_MAP * rgMap)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE((cMap == 0) == (rgMap == NULL));
    m_cMap = cMap;
    m_rgMap = ARRAY_PTR_COR_IL_MAP(rgMap);
}
#endif // !DACCESS_COMPILE

SIZE_T InstrumentedILOffsetMapping::GetCount() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE((m_cMap == 0) == (m_rgMap == NULL));
    return m_cMap;
}

ARRAY_PTR_COR_IL_MAP InstrumentedILOffsetMapping::GetOffsets() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE((m_cMap == 0) == (m_rgMap == NULL));
    return m_rgMap;
}
