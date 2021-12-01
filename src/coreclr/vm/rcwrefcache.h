// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

/*============================================================
**
** Header:  RCWRefCache.h
**
**
** Purpose: Defines RCWRefCache class
** This class maintains per-AppDomain cache that can be used
** by RCW to reference other CCWs
===========================================================*/

#ifndef _H_RCWREFCACHE_
#define _H_RCWREFCACHE_

#ifdef FEATURE_COMWRAPPERS

class RCWRefCache
{
public :
    RCWRefCache(AppDomain *pAppDomain);
    ~RCWRefCache();

    //
    // Add a reference from obj1 to obj2
    //
    HRESULT AddReferenceFromObjectToObject(OBJECTREF obj1, OBJECTREF obj2);

    //
    // Reset dependent handle cache by assigning 0 to m_dwDepHndListFreeIndex.
    //
    void ResetDependentHandles();

    //
    // Shrink the dependent handle cache if necessary (will destroy handles) and clear unused handles.
    //
    void ShrinkDependentHandles();

private :
    //
    // Add obj1 -> obj2 reference using dependent handle
    // May fail if OOM
    //
    HRESULT AddReferenceUsingDependentHandle(OBJECTREF obj1, OBJECTREF obj2);

private :
    AppDomain      *m_pAppDomain;                   // Domain

    CQuickArrayList<OBJECTHANDLE>   m_depHndList;               // Internal DependentHandle cache
                                                                // non-NULL dependent handles followed by NULL slots
    DWORD                           m_dwDepHndListFreeIndex;    // The starting index where m_depHndList has available slots
    DWORD                           m_dwShrinkHint;             // Keep track of how many times we use less than half handles
};

#endif // FEATURE_COMWRAPPERS

#endif // _H_RCWREFCACHE_
