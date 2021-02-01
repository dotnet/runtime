//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _ICorJitInfo
#define _ICorJitInfo

#include "runtimedetails.h"
#include "methodcontext.h"

class interceptor_ICJI : public ICorJitInfo
{

#include "icorjitinfoimpl.h"

private:
    void makeFatMC_ClassHandle(CORINFO_CLASS_HANDLE cls, bool getAttribs);

public:
    // Added to help us track the original icji and be able to easily indirect
    // to it.  And a simple way to keep one memory manager instance per instance.
    ICorJitInfo*   original_ICorJitInfo;
    MethodContext* mc;
};

#endif
