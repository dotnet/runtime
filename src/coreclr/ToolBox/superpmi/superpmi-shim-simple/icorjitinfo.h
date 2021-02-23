// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ICorJitInfo
#define _ICorJitInfo

#include "runtimedetails.h"

class interceptor_ICJI : public ICorJitInfo
{

#include "icorjitinfoimpl.h"

public:
    // Added to help us track the original icji and be able to easily indirect
    // to it.  And a simple way to keep one memory manager instance per instance.
    ICorJitInfo* original_ICorJitInfo;
};

#endif
