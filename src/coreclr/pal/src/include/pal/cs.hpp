// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

///////////////////////////////////////////////////////////////////////////////
//
// File:
//     cs.cpp
//
// Purpose:
//     Header file for critical sections implementation
//

//
///////////////////////////////////////////////////////////////////////////////

#ifndef _PAL_CS_HPP
#define _PAL_CS_HPP

#include "corunix.hpp"
#include "critsect.h"

namespace CorUnix
{
    void CriticalSectionSubSysInitialize(void);

    void InternalInitializeCriticalSectionAndSpinCount(
        PCRITICAL_SECTION pCriticalSection,
        DWORD dwSpinCount,
        bool fInternal);

    void InternalEnterCriticalSection(
        CPalThread *pThread,
        CRITICAL_SECTION *pcs
        );

    void InternalLeaveCriticalSection(
        CPalThread *pThread,
        CRITICAL_SECTION *pcs
        );

#ifdef _DEBUG
    void PALCS_ReportStatisticalData(void);
    void PALCS_DumpCSList();
#endif // _DEBUG

}

#endif // _PAL_CS_HPP

