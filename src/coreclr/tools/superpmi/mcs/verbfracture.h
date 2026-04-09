// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbFracture.h - verb that copies N items into each child file.
//----------------------------------------------------------
#ifndef _verbFracture
#define _verbFracture

class verbFracture
{
public:
    static int DoWork(
        const char* nameOfInput1, const char* nameOfOutput, int indexCount, const int* indexes, bool stripCR);
};
#endif
