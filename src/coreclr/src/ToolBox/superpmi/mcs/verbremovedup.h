//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
