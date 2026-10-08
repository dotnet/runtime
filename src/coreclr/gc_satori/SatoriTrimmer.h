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
// SatoriRecycler.h
//

#ifndef __SATORI_TRIMMER_H__
#define __SATORI_TRIMMER_H__

#include "common.h"
#include "../gc.h"
#include "SatoriRegionQueue.h"

class SatoriHeap;

class SatoriTrimmer
{
public:
    SatoriTrimmer(SatoriHeap* heap);

    void SetStopSuggested();
    void SetOkToRun();
    void WaitForStop();
    bool IsActive();

private:
    static const int TRIMMER_STATE_BLOCKED = -1;
    static const int TRIMMER_STATE_STOPPED = 0;
    static const int TRIMMER_STATE_STOP_SUGGESTED = 1;
    static const int TRIMMER_STATE_OK_TO_RUN = 2;
    static const int TRIMMER_STATE_RUNNING = 3;

    SatoriHeap*  m_heap;
    GCEvent*     m_event;
    SatoriGate*  m_sleepGate;
    size_t       m_lastGen2Count;
    volatile int m_state;
    volatile bool m_paused;

    static void LoopFn(void* inst);
    void Loop();
    void StopAndWait();
    void Pause(int msec);
    void Unpause();
};

#endif
