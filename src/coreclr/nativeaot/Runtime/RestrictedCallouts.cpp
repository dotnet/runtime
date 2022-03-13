// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Callouts from the unmanaged portion of the runtime to C# helpers made during garbage collections. See
// RestrictedCallouts.h for more detail.
//

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "gcrhinterface.h"
#include "shash.h"
#include "RWLock.h"
#include "rhbinder.h"
#include "Crst.h"
#include "RuntimeInstance.h"
#include "MethodTable.h"
#include "ObjectLayout.h"
#include "event.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "RestrictedCallouts.h"
#include "MethodTable.inl"

// The head of the chains of GC callouts, one per callout type.
RestrictedCallouts::GcRestrictedCallout * RestrictedCallouts::s_rgGcRestrictedCallouts[GCRC_Count] = { 0 };

// The head of the chain of HandleTable callouts.
RestrictedCallouts::HandleTableRestrictedCallout * RestrictedCallouts::s_pHandleTableRestrictedCallouts = NULL;

// Lock protecting access to s_rgGcRestrictedCallouts and s_pHandleTableRestrictedCallouts during registration
// and unregistration (not used during actual callbacks since everything is single threaded then).
CrstStatic RestrictedCallouts::s_sLock;

// One time startup initialization.
bool RestrictedCallouts::Initialize()
{
    s_sLock.Init(CrstRestrictedCallouts, CRST_DEFAULT);

    return true;
}

// Register callback of the given type to the method with the given address. The most recently registered
// callbacks are called first. Returns true on success, false if insufficient memory was available for the
// registration.
bool RestrictedCallouts::RegisterGcCallout(GcRestrictedCalloutKind eKind, void * pCalloutMethod)
{
    // Validate callout kind.
    if (eKind >= GCRC_Count)
    {
        ASSERT_UNCONDITIONALLY("Invalid GC restricted callout kind.");
        RhFailFast();
    }

    GcRestrictedCallout * pCallout = new (nothrow) GcRestrictedCallout();
    if (pCallout == NULL)
        return false;

    pCallout->m_pCalloutMethod = pCalloutMethod;

    CrstHolder lh(&s_sLock);

    // Link new callout to head of the chain according to its type.
    pCallout->m_pNext = s_rgGcRestrictedCallouts[eKind];
    s_rgGcRestrictedCallouts[eKind] = pCallout;

    return true;
}

// Unregister a previously registered callout. Removes the first registration that matches on both callout
// kind and address. Causes a fail fast if the registration doesn't exist.
void RestrictedCallouts::UnregisterGcCallout(GcRestrictedCalloutKind eKind, void * pCalloutMethod)
{
    // Validate callout kind.
    if (eKind >= GCRC_Count)
    {
        ASSERT_UNCONDITIONALLY("Invalid GC restricted callout kind.");
        RhFailFast();
    }

    CrstHolder lh(&s_sLock);

    GcRestrictedCallout * pCurrCallout = s_rgGcRestrictedCallouts[eKind];
    GcRestrictedCallout * pPrevCallout = NULL;

    while (pCurrCallout)
    {
        if (pCurrCallout->m_pCalloutMethod == pCalloutMethod)
        {
            // Found a matching entry, remove it from the chain.
            if (pPrevCallout)
                pPrevCallout->m_pNext = pCurrCallout->m_pNext;
            else
                s_rgGcRestrictedCallouts[eKind] = pCurrCallout->m_pNext;

            delete pCurrCallout;

            return;
        }

        pPrevCallout = pCurrCallout;
        pCurrCallout = pCurrCallout->m_pNext;
    }

    // If we get here we didn't find a matching registration, indicating a bug on the part of the caller.
    ASSERT_UNCONDITIONALLY("Attempted to unregister restricted callout that wasn't registered.");
    RhFailFast();
}

// Register callback for the "is alive" property of ref counted handles with objects of the given type (the
// type match must be exact). The most recently registered callbacks are called first. Returns true on
// success, false if insufficient memory was available for the registration.
bool RestrictedCallouts::RegisterRefCountedHandleCallback(void * pCalloutMethod, MethodTable * pTypeFilter)
{
    HandleTableRestrictedCallout * pCallout = new (nothrow) HandleTableRestrictedCallout();
    if (pCallout == NULL)
        return false;

    pCallout->m_pCalloutMethod = pCalloutMethod;
    pCallout->m_pTypeFilter = pTypeFilter;

    CrstHolder lh(&s_sLock);

    // Link new callout to head of the chain.
    pCallout->m_pNext = s_pHandleTableRestrictedCallouts;
    s_pHandleTableRestrictedCallouts = pCallout;

    return true;
}

// Unregister a previously registered callout. Removes the first registration that matches on both callout
// address and filter type. Causes a fail fast if the registration doesn't exist.
void RestrictedCallouts::UnregisterRefCountedHandleCallback(void * pCalloutMethod, MethodTable * pTypeFilter)
{
    CrstHolder lh(&s_sLock);

    HandleTableRestrictedCallout * pCurrCallout = s_pHandleTableRestrictedCallouts;
    HandleTableRestrictedCallout * pPrevCallout = NULL;

    while (pCurrCallout)
    {
        if ((pCurrCallout->m_pCalloutMethod == pCalloutMethod) &&
            (pCurrCallout->m_pTypeFilter == pTypeFilter))
        {
            // Found a matching entry, remove it from the chain.
            if (pPrevCallout)
                pPrevCallout->m_pNext = pCurrCallout->m_pNext;
            else
                s_pHandleTableRestrictedCallouts = pCurrCallout->m_pNext;

            delete pCurrCallout;

            return;
        }

        pPrevCallout = pCurrCallout;
        pCurrCallout = pCurrCallout->m_pNext;
    }

    // If we get here we didn't find a matching registration, indicating a bug on the part of the caller.
    ASSERT_UNCONDITIONALLY("Attempted to unregister restricted callout that wasn't registered.");
    RhFailFast();
}

// Invoke all the registered GC callouts of the given kind. The condemned generation of the current collection
// is passed along to the callouts.
void RestrictedCallouts::InvokeGcCallouts(GcRestrictedCalloutKind eKind, uint32_t uiCondemnedGeneration)
{
    ASSERT(eKind < GCRC_Count);

    // It is illegal for any of the callouts to trigger a GC.
    Thread * pThread = ThreadStore::GetCurrentThread();
    pThread->SetDoNotTriggerGc();

    // Due to the above we have better suppress GC stress.
    bool fGcStressWasSuppressed = pThread->IsSuppressGcStressSet();
    if (!fGcStressWasSuppressed)
        pThread->SetSuppressGcStress();

    GcRestrictedCallout * pCurrCallout = s_rgGcRestrictedCallouts[eKind];
    while (pCurrCallout)
    {
        // Make the callout.
        ((GcRestrictedCallbackFunction)pCurrCallout->m_pCalloutMethod)(uiCondemnedGeneration);

        pCurrCallout = pCurrCallout->m_pNext;
    }

    // Revert GC stress mode if we changed it.
    if (!fGcStressWasSuppressed)
        pThread->ClearSuppressGcStress();

    pThread->ClearDoNotTriggerGc();
}

// Invoke all the registered ref counted handle callouts for the given object extracted from the handle. The
// result is the union of the results for all the handlers that matched the object type (i.e. if one of them
// returned true the overall result is true otherwise false is returned (which includes the case where no
// handlers matched)). Since there should be no other side-effects of the callout, the invocations cease as
// soon as a handler returns true.
bool RestrictedCallouts::InvokeRefCountedHandleCallbacks(Object * pObject)
{
    bool fResult = false;

    // It is illegal for any of the callouts to trigger a GC.
    Thread * pThread = ThreadStore::GetCurrentThread();
    pThread->SetDoNotTriggerGc();

    // Due to the above we have better suppress GC stress.
    bool fGcStressWasSuppressed = pThread->IsSuppressGcStressSet();
    if (!fGcStressWasSuppressed)
        pThread->SetSuppressGcStress();

    HandleTableRestrictedCallout * pCurrCallout = s_pHandleTableRestrictedCallouts;
    while (pCurrCallout)
    {
        if (pObject->get_SafeEEType() == pCurrCallout->m_pTypeFilter)
        {
            // Make the callout. Return true to our caller as soon as we see a true result here.
            if (((HandleTableRestrictedCallbackFunction)pCurrCallout->m_pCalloutMethod)(pObject))
            {
                fResult = true;
                goto Done;
            }
        }

        pCurrCallout = pCurrCallout->m_pNext;
    }

  Done:
    // Revert GC stress mode if we changed it.
    if (!fGcStressWasSuppressed)
        pThread->ClearSuppressGcStress();

    pThread->ClearDoNotTriggerGc();

    return fResult;
}
