// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbRemoveDup.h - verb that attempts to remove dups
//----------------------------------------------------------
#ifndef _verbRemoveDup
#define _verbRemoveDup

class verbRemoveDup
{
public:
    static int DoWork(const char* nameOfInput1, const char* nameOfOutput, bool stripCR, bool legacyCompare);
};
#endif
