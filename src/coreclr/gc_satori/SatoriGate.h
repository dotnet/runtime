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
// SatoriGate.h
//

#ifndef __SATORI_GATE_H__
#define __SATORI_GATE_H__

#include <stdint.h>

class SatoriGate
{
private:
    static const uint32_t s_open = 1;
    static const uint32_t s_blocking = 0;

    volatile uint32_t m_state;

#if defined(_INC_PTHREADS)
    pthread_mutex_t* m_cs;
    pthread_cond_t* m_cv;
#else
    size_t* dummy1;
    size_t* dummy2;
#endif

public:
    SatoriGate();

    void Wait();

    bool TimedWait(int timeout);

    void WakeOne();

    void WakeAll();
};

#endif
