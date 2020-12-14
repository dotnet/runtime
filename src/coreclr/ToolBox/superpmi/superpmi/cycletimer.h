//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
