// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#ifdef FEATURE_COMINTEROP

class RCWRefCache
{
public :
    RCWRefCache(AppDomain *pAppDomain);
    ~RCWRefCache();

    //
    // Add a reference from RCW to CCW
    //
    HRESULT AddReferenceFromRCWToCCW(RCW *pRCW, ComCallWrapper *pCCW);

    //
    // Enumerate all Jupiter RCWs in the RCW cache and do the callback
    // I'm using template here so there is no perf penality
    //
    template<class Function, class T> HRESULT EnumerateAllJupiterRCWs(Function fn, T userData)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        HRESULT hr;

        // Go through the RCW cache and call the callback for all Jupiter objects
        RCWCache *pRCWCache = m_pAppDomain->GetRCWCacheNoCreate();
        if (pRCWCache != NULL)
        {
            SHash<RCWCache::RCWCacheTraits> *pHashMap = &pRCWCache->m_HashMap;

            for (SHash<RCWCache::RCWCacheTraits>::Iterator it = pHashMap->Begin(); it != pHashMap->End(); it++)
            {
                RCW *pRCW = *it;
                _ASSERTE(pRCW != NULL);

                if (pRCW->IsJupiterObject())
                {
                    hr = fn(pRCW, userData);
                    if (FAILED(hr))
                    {
                        return hr;
                    }
                }
            }
        }

        return S_OK;
    }

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
    // Add RCW -> CCW reference using dependent handle
    // May fail if OOM
    //
    HRESULT AddReferenceUsingDependentHandle(RCW *pRCW, ComCallWrapper *pCCW);

private :
    AppDomain      *m_pAppDomain;                   // Domain

    CQuickArrayList<OBJECTHANDLE>   m_depHndList;               // Internal DependentHandle cache
                                                                // non-NULL dependent handles followed by NULL slots
    DWORD                           m_dwDepHndListFreeIndex;    // The starting index where m_depHndList has available slots
    DWORD                           m_dwShrinkHint;             // Keep track of how many times we use less than half handles
};

#endif // FEATURE_COMINTEROP

#endif // _H_RCWREFCACHE_
