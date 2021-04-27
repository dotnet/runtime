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
    unsigned __int64 GetCycles();
    unsigned __int64 QueryOverhead();

private:
    // Cycles
    unsigned __int64 start;
    unsigned __int64 stop;
    unsigned __int64 overhead;
};
#endif
