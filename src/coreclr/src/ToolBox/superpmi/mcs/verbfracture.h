//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
