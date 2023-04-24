// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
