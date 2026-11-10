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

private:
    // Index to start the next search for a reported metric at. See
    // MyICJI::reportMetadata.
    size_t m_metricSearchStart = 0;
};

ICorJitInfo* InitICorJitInfo(JitInstance* jitInstance);
#endif
