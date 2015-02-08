//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

///////////////////////////////////////////////////////////////////////////////
// 
// Copyright (c) 2004 Microsoft Corporation.  All Rights Reserved.
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

    bool InternalTryEnterCriticalSection(
        CPalThread * pThread,
        PCRITICAL_SECTION pCriticalSection);

#ifdef _DEBUG
    void PALCS_ReportStatisticalData(void);
    void PALCS_DumpCSList();
#endif // _DEBUG
    
}

#endif // _PAL_CS_HPP

