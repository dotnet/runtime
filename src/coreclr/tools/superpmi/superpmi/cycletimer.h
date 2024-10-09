// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _CycleTimer
#define _CycleTimer

#include "errorhandling.h"

class CycleTimer
{
public:
    CycleTimer();
    ~CycleTimer();

    void             Start();
    void             Stop();
    uint64_t GetCycles();
    uint64_t QueryOverhead();

private:
    // Cycles
    uint64_t start;
    uint64_t stop;
    uint64_t overhead;
};
#endif
