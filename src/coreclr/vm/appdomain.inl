// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*============================================================
**
** Header:  AppDomain.i
**

**
** Purpose: Implements AppDomain (loader domain) architecture
** inline functions
**
**
===========================================================*/
#ifndef _APPDOMAIN_I
#define _APPDOMAIN_I

#include "appdomain.hpp"

inline AppDomain::PathIterator AppDomain::IterateNativeDllSearchDirectories()
{
    WRAPPER_NO_CONTRACT;
    PathIterator i;
    i.m_i = m_NativeDllSearchDirectories.Iterate();
    return i;
}

inline BOOL AppDomain::HasNativeDllSearchDirectories()
{
    WRAPPER_NO_CONTRACT;
    return m_NativeDllSearchDirectories.GetCount() !=0;
}

inline bool AppDomain::MustForceTrivialWaitOperations()
{
    LIMITED_METHOD_CONTRACT;
    return m_ForceTrivialWaitOperations;
}

inline void AppDomain::SetForceTrivialWaitOperations()
{
    LIMITED_METHOD_CONTRACT;
    m_ForceTrivialWaitOperations = true;
}

inline PTR_LoaderHeap AppDomain::GetHighFrequencyHeap()
{
    WRAPPER_NO_CONTRACT;
    return GetLoaderAllocator()->GetHighFrequencyHeap();
}

inline PTR_LoaderHeap AppDomain::GetLowFrequencyHeap()
{
    WRAPPER_NO_CONTRACT;
    return GetLoaderAllocator()->GetLowFrequencyHeap();
}

inline PTR_LoaderHeap AppDomain::GetStubHeap()
{
    WRAPPER_NO_CONTRACT;
    return GetLoaderAllocator()->GetStubHeap();
}

#endif  // _APPDOMAIN_I

