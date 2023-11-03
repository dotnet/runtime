// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#if defined(FEATURE_GC_STRESS) & !defined(DACCESS_COMPILE)


#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "holder.h"
#include "Crst.h"
#include "RhConfig.h"
#include "gcrhinterface.h"
#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "forward_declarations.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "event.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "shash.h"
#include "shash.inl"
#include "GcStressControl.h"


class GcStressControl
{
public:
    static bool ShouldHijack(uintptr_t CallsiteIP, HijackType ht)
    {
        if (s_initState != isInited)
            Initialize();

        // don't hijack for GC stress if we're in a "no GC stress" region
        Thread * pCurrentThread = ThreadStore::GetCurrentThread();
        if (pCurrentThread->IsSuppressGcStressSet())
            return false;

        if (g_pRhConfig->GetGcStressThrottleMode() == 0)
        {
            return true;
        }
        if (g_pRhConfig->GetGcStressThrottleMode() & gcstm_TriggerRandom)
        {
            if (GcStressTriggerRandom(CallsiteIP, ht, pCurrentThread))
                return true;
        }
        if (g_pRhConfig->GetGcStressThrottleMode() & gcstm_TriggerOnFirstHit)
        {
            if (GcStressTriggerFirstHit(CallsiteIP, ht))
                return true;
        }
        return false;
    }

private:
    enum InitState { isNotInited, isIniting, isInited };

    static void Initialize()
    {
        volatile InitState is = (InitState) PalInterlockedCompareExchange((volatile int32_t*)(&s_initState), isIniting, isNotInited);
        if (is == isNotInited)
        {
            s_lock.InitNoThrow(CrstGcStressControl);

            if (g_pRhConfig->GetGcStressSeed())
                s_lGcStressRNGSeed = (uint32_t)g_pRhConfig->GetGcStressSeed();
            else
                s_lGcStressRNGSeed = (uint32_t)PalGetTickCount64();

            if (g_pRhConfig->GetGcStressFreqDenom())
                s_lGcStressFreqDenom = (uint32_t)g_pRhConfig->GetGcStressFreqDenom();
            else
                s_lGcStressFreqDenom = 10000;

            s_initState = isInited;
        }
        else
        {
            while (s_initState != isInited)
                ;
        }
    }

    // returns true if no entry was found for CallsiteIP, false otherwise
    static bool GcStressTrackAtIP(uintptr_t CallsiteIP, HijackType ht, bool bForceGC)
    {
        // do this under a lock, as the underlying SHash might be "grown" by
        // operations on other threads

        CrstHolder lh(&s_lock);

        const CallsiteCountEntry * pEntry = s_callsites.LookupPtr(CallsiteIP);
        size_t hits;

        if (pEntry == NULL)
        {
            hits = 1;
            CallsiteCountEntry e = {CallsiteIP, 1, 1, ht};
            s_callsites.AddOrReplace(e);
        }
        else
        {
            hits = ++(const_cast<CallsiteCountEntry*>(pEntry)->countHit);
            if (bForceGC)
            {
                ++(const_cast<CallsiteCountEntry*>(pEntry)->countForced);
            }
        }

        return pEntry == NULL;
    }

    static bool GcStressTriggerFirstHit(uintptr_t CallsiteIP, HijackType ht)
    {
        return GcStressTrackAtIP(CallsiteIP, ht, false);
    }

    static uint32_t GcStressRNG(uint32_t uMaxValue, Thread *pCurrentThread)
    {
        if (!pCurrentThread->IsRandInited())
        {
            pCurrentThread->SetRandomSeed(s_lGcStressRNGSeed);
        }

        return pCurrentThread->NextRand() % uMaxValue;
    }

    static bool GcStressTriggerRandom(uintptr_t CallsiteIP, HijackType ht, Thread *pCurrentThread)
    {
        bool bRes = false;
        if (ht == htLoop)
        {
            bRes = GcStressRNG(s_lGcStressFreqDenom , pCurrentThread) < g_pRhConfig->GetGcStressFreqLoop();
        }
        else if (ht == htCallsite)
        {
            bRes = GcStressRNG(s_lGcStressFreqDenom , pCurrentThread) < g_pRhConfig->GetGcStressFreqCallsite();
        }
        if (bRes)
        {
            // if we're about to trigger a GC, track this in s_callsites
            GcStressTrackAtIP(CallsiteIP, ht, true);
        }
        return bRes;
    }

private:
    static CrstStatic           s_lock;
    static uint32_t               s_lGcStressRNGSeed;
    static uint32_t               s_lGcStressFreqDenom;
    static volatile InitState   s_initState;

public:
    static CallsiteCountSHash   s_callsites;            // exposed to the DAC
};

// public interface:

CallsiteCountSHash GcStressControl::s_callsites;
CrstStatic GcStressControl::s_lock;
uint32_t GcStressControl::s_lGcStressRNGSeed = 0;
uint32_t GcStressControl::s_lGcStressFreqDenom = 0;
volatile GcStressControl::InitState GcStressControl::s_initState = GcStressControl::isNotInited;

GPTR_IMPL_INIT(CallsiteCountSHash, g_pCallsites, &GcStressControl::s_callsites);

bool ShouldHijackForGcStress(uintptr_t CallsiteIP, HijackType ht)
{
    return GcStressControl::ShouldHijack(CallsiteIP, ht);
}

#endif // FEATURE_GC_STRESS & !DACCESS_COMPILE


