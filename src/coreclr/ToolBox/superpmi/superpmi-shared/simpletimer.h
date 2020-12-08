//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
