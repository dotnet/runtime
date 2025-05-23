// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SimpleTimer
#define _SimpleTimer

#include <stdint.h>

class SimpleTimer
{
public:
    SimpleTimer();
    ~SimpleTimer();

    void   Start();
    void   Stop();
    double GetMilliseconds();
    double GetSeconds();

private:
    int64_t proc_freq;
    int64_t start;
    int64_t stop;
};
#endif
