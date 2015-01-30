//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
////////////////////////////////////////////////////////////////////////////////



#ifndef _COMMETHODRENTAL_H_
#define _COMMETHODRENTAL_H_

#include "excep.h"
#include "fcall.h"

#ifdef FEATURE_METHOD_RENTAL
// COMMethodRental
// This class implements SwapMethodBody for our MethodRenting story
class COMMethodRental
{
public:

    // COMMethodRental.SwapMethodBody -- this function will swap an existing method body with
    // a new method body
    //
    static
    void QCALLTYPE SwapMethodBody(EnregisteredTypeHandle cls, INT32 tkMethod, LPVOID rgMethod, INT32 iSize, INT32 flags, QCall::StackCrawlMarkHandle stackMark);
};
#endif // FEATURE_METHOD_RENTAL

#endif //_COMMETHODRENTAL_H_
