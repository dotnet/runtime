//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _ICorJitInfo
#define _ICorJitInfo

#include "runtimedetails.h"
#include "jitinstance.h"

extern ICorJitInfo* pICJI;

class MyICJI : public ICorJitInfo
{

#include "icorjitinfoimpl.h"

public:
    // Added extras... todo add padding to detect corruption?
    JitInstance* jitInstance;
};

ICorJitInfo* InitICorJitInfo(JitInstance* jitInstance);
#endif
