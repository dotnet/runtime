//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _ICorJitInfo
#define _ICorJitInfo

#include "runtimedetails.h"
#include "ieememorymanager.h"
#include "methodcallsummarizer.h"

class interceptor_ICJI : public ICorJitInfo
{

#include "icorjitinfoimpl.h"

public:
    // Added to help us track the original icji and be able to easily indirect
    // to it.  And a simple way to keep one memory manager instance per instance.
    ICorJitInfo*          original_ICorJitInfo;
    MethodCallSummarizer* mcs;
};

#endif
