// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SimpleTimer
#define _SimpleTimer

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
    LARGE_INTEGER proc_freq;
    LARGE_INTEGER start;
    LARGE_INTEGER stop;
};
#endif
