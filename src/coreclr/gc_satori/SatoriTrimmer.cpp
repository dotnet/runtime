// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriRecycler.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"

#include "SatoriUtil.h"
#include "SatoriTrimmer.h"
#include "SatoriHeap.h"
#include "SatoriPage.h"
#include "SatoriPage.inl"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"

SatoriTrimmer::SatoriTrimmer(SatoriHeap* heap)
{
    m_lastGen2Count = 0;
    m_heap  = heap;
    m_state = TRIMMER_STATE_STOPPED;

    m_event = new (nothrow) GCEvent;
    m_event->CreateAutoEventNoThrow(false);

    m_sleepGate = new (nothrow) SatoriGate();

    if (SatoriUtil::IsTrimmingEnabled())
    {
        GCToEEInterface::CreateThread(LoopFn, this, false, "Satori GC Trimmer Thread");
    }
}

void SatoriTrimmer::Pause(int msec)
{
    m_paused = true;
    m_sleepGate->TimedWait(msec);
    m_paused = false;
}

void SatoriTrimmer::Unpause()
{
    m_sleepGate->WakeOne();
}

void SatoriTrimmer::LoopFn(void* inst)
{
    SatoriTrimmer* ths = (SatoriTrimmer*)inst;
    ths->Loop();
}

void SatoriTrimmer::Loop()
{
    int64_t lastGen2 = m_heap->Recycler()->GetCollectionCount(2);
    while (true)
    {
        // limit the re-trim rate to once per 5 sec.
        // we would also require that gen2 gc happened since the last round.
        while (true)
        {
            int64_t newGen2 = m_heap->Recycler()->GetCollectionCount(2);
            if (lastGen2 != newGen2)
            {
                lastGen2 = newGen2;
                break;
            }

            Interlocked::CompareExchange(&m_state, TRIMMER_STATE_STOPPED, TRIMMER_STATE_RUNNING);
            // we are not running here, so we can sleep a bit before continuing.
            Pause(5000);
            StopAndWait();
        }

        m_heap->ForEachPage(
            [&](SatoriPage* page)
            {
                // limit the rate of scanning to 1 page/msec.
                Pause(1);
                if (m_state != TRIMMER_STATE_RUNNING)
                {
                    StopAndWait();
                }

                int64_t lastGen1 = m_heap->Recycler()->GetCollectionCount(1);

                page->ForEachRegion(
                    [&](SatoriRegion* region)
                    {
                        SatoriQueue<SatoriRegion>* queue = region->ContainingQueue();
                        if (queue && queue->Kind() == QueueKind::Allocator)
                        {
                            if (region->CanDecommit() || region->CanCoalesceWithNext())
                            {
                                if (queue->TryRemove(region))
                                {
                                    bool didSomeWork = region->TryCoalesceWithNext();
                                    if (region->TryDecommit())
                                    {
                                        m_heap->Allocator()->AddRegion(region);
                                        didSomeWork = true;
                                    }
                                    else
                                    {
                                        m_heap->Allocator()->ReturnRegion(region);
                                    }

                                    if (didSomeWork)
                                    {
                                        // limit the decommit/coalesce rate to 1 region/10 msec.
                                        Pause(10);
                                        if (m_state != TRIMMER_STATE_RUNNING)
                                        {
                                            StopAndWait();
                                        }
                                    }
                                }
                            }
                        }

                        // also we will pause for 1 sec if there was a GC - to further reduce the churn
                        // if the app is allocation-active.
                        int64_t newGen1 = m_heap->Recycler()->GetCollectionCount(1);
                        if (newGen1 != lastGen1)
                        {
                            lastGen1 = newGen1;
                            Pause(1000);
                        }

                        if (m_state != TRIMMER_STATE_RUNNING)
                        {
                            StopAndWait();
                        }
                    }
                );
            }
        );
    }
}

void SatoriTrimmer::StopAndWait()
{
    while (true)
    {
        tryAgain:

        int state = m_state;
        switch (state)
        {
        case TRIMMER_STATE_STOP_SUGGESTED:
            Interlocked::CompareExchange(&m_state, TRIMMER_STATE_STOPPED, state);
            continue;
        case TRIMMER_STATE_OK_TO_RUN:
            Interlocked::CompareExchange(&m_state, TRIMMER_STATE_RUNNING, state);
            continue;
        case TRIMMER_STATE_STOPPED:
            for (int i = 0; i < 10; i++)
            {
                Pause(100);
                if (m_state != state)
                {
                    goto tryAgain;
                }
            }

            if (Interlocked::CompareExchange(&m_state, TRIMMER_STATE_BLOCKED, state) == state)
            {
                m_event->Wait(INFINITE, false);
            }
            continue;
        case TRIMMER_STATE_RUNNING:
            return;
        default:
            UNREACHABLE();
            return;
        }
    }
}

void SatoriTrimmer::SetOkToRun()
{
    int state = m_state;
    switch (state)
    {
    case TRIMMER_STATE_BLOCKED:
        // trimmer can't get out of BlOCKED by itself, ordinary assignment is ok
        m_state = TRIMMER_STATE_OK_TO_RUN;
        m_event->Set();
        break;
    case TRIMMER_STATE_STOPPED:
        Interlocked::CompareExchange(&m_state, TRIMMER_STATE_OK_TO_RUN, state);
        break;
    case TRIMMER_STATE_STOP_SUGGESTED:
        Interlocked::CompareExchange(&m_state, TRIMMER_STATE_RUNNING, state);
        break;
    }
}

void SatoriTrimmer::SetStopSuggested()
{
    while (true)
    {
        int state = m_state;
        switch (state)
        {
        case TRIMMER_STATE_OK_TO_RUN:
            if (Interlocked::CompareExchange(&m_state, TRIMMER_STATE_STOPPED, state) == state)
            {
                Unpause();
                return;
            }
            break;
        case TRIMMER_STATE_RUNNING:
            if (Interlocked::CompareExchange(&m_state, TRIMMER_STATE_STOP_SUGGESTED, state) == state)
            {
                Unpause();
                return;
            }
            break;
        default:
            _ASSERTE(m_state <= TRIMMER_STATE_STOP_SUGGESTED);
            Unpause();
            return;
        }
    }
}

void SatoriTrimmer::WaitForStop()
{
    _ASSERTE(m_state <= TRIMMER_STATE_STOP_SUGGESTED);

    int cycles = 0;
    while (m_state == TRIMMER_STATE_STOP_SUGGESTED)
    {
        if (m_paused)
        {
            Unpause();
        }

        YieldProcessor();
        if ((++cycles & 127) == 0)
        {
            GCToOSInterface::YieldThread(0);
        }
    }

    _ASSERTE(!IsActive());
}

bool SatoriTrimmer::IsActive()
{
    return m_state > TRIMMER_STATE_STOPPED;
}
