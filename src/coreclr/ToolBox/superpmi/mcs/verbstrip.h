// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbStrip.h - verb that removes a list of mc's from an MCH file
//----------------------------------------------------------
#ifndef _verbStrip
#define _verbStrip

class verbStrip
{
public:
    static int DoWork(const char* nameOfInput1,
                      const char* nameOfOutput,
                      int         indexCount,
                      const int*  indexes,
                      bool        strip,
                      bool        stripCR);
    static int DoWorkTheOldWay(
        const char* nameOfInput, const char* nameOfOutput, int indexCount, const int* indexes, bool stripCR);
};
#endif
