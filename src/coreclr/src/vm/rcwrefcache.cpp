// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

/*============================================================
**
** Class:  RCWRefCache
**
**
** Purpose: The implementation of RCWRefCache class
** This class maintains per-AppDomain cache that can be used 
** by RCW to reference other CCWs
===========================================================*/

#include "common.h"

#include "objecthandle.h"
#include "rcwrefcache.h"
#include "comcallablewrapper.h"
#include "runtimecallablewrapper.h"

// SHRINK_TOTAL_THRESHOLD - only shrink when the total number of handles exceed this number
#ifdef _DEBUG
// Exercise the shrink path more often
#define SHRINK_TOTAL_THRESHOLD            4
#else
#define SHRINK_TOTAL_THRESHOLD            100
#endif // _DEBUG

// SHRINK_HINT_THRESHOLD - only shrink when we consistently see a hint that we probably need to shrink
#ifdef _DEBUG
// Exercise the shrink path more often
#define SHRINK_HINT_THRESHOLD            0
#else
#define SHRINK_HINT_THRESHOLD            10
#endif // _DEBUG

RCWRefCache::RCWRefCache(AppDomain *pAppDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pAppDomain != NULL);
    
    m_pAppDomain = pAppDomain;
    
    m_dwDepHndListFreeIndex = 0;
}

RCWRefCache::~RCWRefCache()
{

    LIMITED_METHOD_CONTRACT;

    // We don't need to clear the handles because this AppDomain will go away
    // and GC will clean everything for us
}

//
// Reset dependent handle cache by assigning 0 to m_dwDepHndListFreeIndex.
//
void RCWRefCache::ResetDependentHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO100, "\t[RCWRefCache 0x%p] ----- RCWRefCache::ResetDependentHandles BEGINS -----\n", this));
    LOG((LF_INTEROP, LL_INFO100, 
        "\t[RCWRefCache 0x%p] Dependent handle cache status: Total SLOTs = %d, Next free SLOT index = %d\n", 
        this, (ULONG) m_depHndList.Size(), m_dwDepHndListFreeIndex
        ));

    // Reset the index - now every handle in the cache can be reused
    m_dwDepHndListFreeIndex = 0;    

    LOG((LF_INTEROP, LL_INFO100, "\t[RCWRefCache 0x%p] ----- RCWRefCache::ResetDependentHandles ENDS   -----\n", this));
}

//
// Shrink the dependent handle cache if necessary (will destroy handles) and clear unused handles.
//
void RCWRefCache::ShrinkDependentHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(m_dwDepHndListFreeIndex <= m_depHndList.Size());
    
    LOG((LF_INTEROP, LL_INFO100, "\t[RCWRefCache 0x%p] ----- RCWRefCache::ShrinkDependentHandles BEGINS -----\n", this));
    LOG((LF_INTEROP, LL_INFO100, 
        "\t[RCWRefCache 0x%p] Dependent handle cache status: Total SLOTs = %d, Next free SLOT index = %d\n", 
        this, (ULONG) m_depHndList.Size(), m_dwDepHndListFreeIndex
        ));

    SIZE_T depHndListSize = m_depHndList.Size();

    
    //
    // If we are not using half of the handles last time, it is a hint that probably we need to shrink
    //
    _ASSERTE(SHRINK_TOTAL_THRESHOLD / 2 > 0);
    if (m_dwDepHndListFreeIndex < depHndListSize / 2 && depHndListSize > SHRINK_TOTAL_THRESHOLD)
    {
        m_dwShrinkHint++;
        LOG((LF_INTEROP, LL_INFO100, "\t[RCWRefCache 0x%p] m_dwShrinkHint = %d\n", this, m_dwShrinkHint));

        //
        // Only shrink if we consistently seen such hint more than SHRINK_TOTAL_THRESHOLD times
        //
        if (m_dwShrinkHint > SHRINK_HINT_THRESHOLD)
        {
        
            LOG((LF_INTEROP, LL_INFO100, 
                "\t[RCWRefCache 0x%p] Shrinking dependent handle cache. Total SLOTS = %d\n", 
                this, m_depHndList.Size()
                ));
            
            //
            // Destroy the handles we don't need and resize 
            //
            SIZE_T newSize = depHndListSize / 2;

            _ASSERTE(newSize > 0);
            
            for (SIZE_T i = newSize; i < depHndListSize; ++i)
            {   
                OBJECTHANDLE hnd = m_depHndList.Pop();
                DestroyDependentHandle(hnd);
                LOG((LF_INTEROP, LL_INFO1000, 
                    "\t[RCWRefCache 0x%p] DependentHandle 0x%p destroyed @ index %d\n", 
                    this, hnd, (ULONG)(depHndListSize - (i - newSize + 1))));
            }

            // Try realloc - I don't expect this to fail but who knows
            // If it fails, we just don't care
            m_depHndList.ReSizeNoThrow(newSize);            

            //
            // Reset shrink hint as we've just shrinked
            //
            m_dwShrinkHint = 0;
            LOG((LF_INTEROP, LL_INFO100, "\t[RCWRefCache 0x%p] Reset m_dwShrinkHint = 0\n", this));
        }
    }
    else
    {
        //
        // Reset shrink hint and start over
        //
        m_dwShrinkHint = 0;
        LOG((LF_INTEROP, LL_INFO100, "\t[RCWRefCache 0x%p] Reset m_dwShrinkCount = 0\n", this));    
    }

    //
    // Iterate through the list and clear the unused handles
    //
    for (SIZE_T i = m_dwDepHndListFreeIndex; i < m_depHndList.Size(); ++i)
    {
        OBJECTHANDLE depHnd = m_depHndList[i];
        
        HndAssignHandle(depHnd, NULL);
        SetDependentHandleSecondary(depHnd, NULL);        
            
        LOG((LF_INTEROP, LL_INFO1000, "\t[RCWRefCache 0x%p] DependentHandle 0x%p cleared @ index %d\n", this, depHnd, (ULONG) i));
    }

    LOG((LF_INTEROP, LL_INFO100, 
        "\t[RCWRefCache 0x%p] Dependent handle cache status: Total SLOTs = %d, Next free SLOT index = %d\n", 
        this, (ULONG) m_depHndList.Size(), m_dwDepHndListFreeIndex
        ));
    
    LOG((LF_INTEROP, LL_INFO100, "\t[RCWRefCache 0x%p] ----- RCWRefCache::ShrinkDependentHandles ENDS   -----\n", this));
}

//
// Add a reference from RCW to CCW
//
HRESULT RCWRefCache::AddReferenceFromRCWToCCW(RCW *pRCW, ComCallWrapper *pCCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRCW));
        PRECONDITION(CheckPointer(pCCW));
        PRECONDITION(CheckPointer(OBJECTREFToObject(pCCW->GetObjectRef())));
        PRECONDITION(pRCW->IsJupiterObject());
        PRECONDITION(pCCW->GetJupiterRefCount() > 0);
    }
    CONTRACTL_END;
    
    // Try adding reference using dependent handles
    return AddReferenceUsingDependentHandle(pRCW, pCCW);
}

//
// Add RCW -> CCW reference using dependent handle
// May fail if OOM
//
HRESULT RCWRefCache::AddReferenceUsingDependentHandle(RCW *pRCW, ComCallWrapper *pCCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRCW));
        PRECONDITION(CheckPointer(pCCW));
        PRECONDITION(CheckPointer(OBJECTREFToObject(pCCW->GetObjectRef())));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    LOG((LF_INTEROP, LL_INFO1000, 
        "\t[RCWRefCache 0x%p] Dependent handle cache status: Total SLOTs = %d, Next free SLOT index = %d\n", 
        this, (ULONG) m_depHndList.Size(), m_dwDepHndListFreeIndex
        ));
    
    // Is there an valid DependentHandle in the array?
    if (m_dwDepHndListFreeIndex >= m_depHndList.Size())
    {
        // No, we need to create a new handle
        EX_TRY
        {
            OBJECTHANDLE depHnd = m_pAppDomain->CreateDependentHandle(pRCW->GetExposedObject(), pCCW->GetObjectRef());
            m_depHndList.Push(depHnd);

            STRESS_LOG2(
                LF_INTEROP, LL_INFO1000, 
                "\t[RCWRefCache] Created DependentHandle 0x%p @ appended SLOT %d\n", 
                depHnd,
                m_dwDepHndListFreeIndex
                );

            // Increment the index so that next time we still need to append
            m_dwDepHndListFreeIndex++;
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions)
    }
    else
    {
        // Yes, there is a valid DependentHandle entry on the list, use that
        OBJECTHANDLE depHnd = (OBJECTHANDLE) m_depHndList[m_dwDepHndListFreeIndex];

        HndAssignHandle(depHnd, pRCW->GetExposedObject());
        SetDependentHandleSecondary(depHnd, pCCW->GetObjectRef());

        STRESS_LOG3(
            LF_INTEROP, LL_INFO1000, 
            "\t[RCWRefCache 0x%p] Reused DependentHandle 0x%p @ valid SLOT %d\n", 
            this, depHnd, m_dwDepHndListFreeIndex);
        
        // Increment the index and the next one will be used
        m_dwDepHndListFreeIndex++;
    }

    if (SUCCEEDED(hr))
    {
        LOG((LF_INTEROP, LL_INFO1000, 
            "\t[RCWRefCache 0x%p] Dependent handle cache status: Total SLOTs = %d, Next free SLOT index = %d\n", 
            this, (ULONG) m_depHndList.Size(), m_dwDepHndListFreeIndex
            ));
    }
   
    return hr;    
}
