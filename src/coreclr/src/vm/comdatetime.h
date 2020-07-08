// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _COMDATETIME_H_
#define _COMDATETIME_H_

#include <oleauto.h>
#include "fcall.h"

#include <pshpack1.h>

class COMDateTime {
    static const INT64 TicksPerMillisecond;
    static const INT64 TicksPerSecond;
    static const INT64 TicksPerMinute;
    static const INT64 TicksPerHour;
    static const INT64 TicksPerDay;

    static const INT64 MillisPerSecond;
	static const INT64 MillisPerDay;

    static const int DaysPer4Years;
    static const int DaysPer100Years;
    static const int DaysPer400Years;
    // Number of days from 1/1/0001 to 1/1/10000
    static const int DaysTo10000;

	static const int DaysTo1899;

	static const INT64 DoubleDateOffset;
	static const INT64 OADateMinAsTicks;  // in ticks
	static const double OADateMinAsDouble;
	static const double OADateMaxAsDouble;

	static const INT64 MaxTicks;
	static const INT64 MaxMillis;

public:

	// Native util functions for other classes.
	static INT64 DoubleDateToTicks(const double d);  // From OleAut Date
	static double TicksToDoubleDate(const INT64 ticks);
};

#include <poppack.h>

#endif // _COMDATETIME_H_
