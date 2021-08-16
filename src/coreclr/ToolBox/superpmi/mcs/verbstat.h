// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbStat.h - verb that
//----------------------------------------------------------
#ifndef _verbStat
#define _verbStat

class verbStat
{
public:
    static int DoWork(const char* nameOfInput1, const char* nameOfOutput, int indexCount, const int* indexes);
};
#endif
