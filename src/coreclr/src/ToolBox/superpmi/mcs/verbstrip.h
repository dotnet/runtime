//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
