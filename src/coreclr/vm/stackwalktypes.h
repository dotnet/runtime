// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================================
// File: stackwalktypes.h
//

// ============================================================================
// Contains types used by stackwalk.h.


#ifndef __STACKWALKTYPES_H__
#define __STACKWALKTYPES_H__

class CrawlFrame;
struct RangeSection;

//
// This type should be used internally inside the code manager only. EECodeInfo should
// be used in general code instead. Ideally, we would replace all uses of METHODTOKEN
// with EECodeInfo.
//
struct METHODTOKEN
{
    METHODTOKEN(RangeSection * pRangeSection, TADDR pCodeHeader)
        : m_pRangeSection(pRangeSection), m_pCodeHeader(pCodeHeader)
    {
    }

    METHODTOKEN()
    {
    }

    // Cache of RangeSection containing the code to avoid redundant lookups.
    RangeSection * m_pRangeSection;

    // CodeHeader* for EEJitManager
    // PTR_RUNTIME_FUNCTION for managed native code
    TADDR m_pCodeHeader;

    BOOL IsNull() const
    {
        return m_pCodeHeader == 0;
    }
};

//************************************************************************
// Stack walking
//************************************************************************
enum StackCrawlMark
{
    LookForMe = 0,
    LookForMyCaller = 1,
    LookForMyCallersCaller = 2,
    LookForThread = 3
};

enum StackWalkAction
{
    SWA_CONTINUE    = 0,    // continue walking
    SWA_ABORT       = 1,    // stop walking, early out in "failure case"
    SWA_FAILED      = 2     // couldn't walk stack
};

#define SWA_DONE SWA_CONTINUE


// Pointer to the StackWalk callback function.
typedef StackWalkAction (*PSTACKWALKFRAMESCALLBACK)(
    CrawlFrame       *pCF,      //
    VOID*             pData     // Caller's private data

);

#endif  // __STACKWALKTYPES_H__
