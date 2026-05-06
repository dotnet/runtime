// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ICorJitInfo
#define _ICorJitInfo

#include "runtimedetails.h"
#include "methodcontext.h"

class interceptor_ICJC;

class interceptor_ICJI : public ICorJitInfo
{

#include "icorjitinfoimpl.h"

private:
    interceptor_ICJC* m_compiler;
    ICorJitInfo* original_ICorJitInfo;
    MethodContext* mc;
    bool m_savedCollectionEarly;

public:
    interceptor_ICJI(interceptor_ICJC* compiler, ICorJitInfo* original, MethodContext* mc)
        : m_compiler(compiler)
        , original_ICorJitInfo(original)
        , mc(mc)
        , m_savedCollectionEarly(false)
    {
    }

    bool SavedCollectionEarly() const { return m_savedCollectionEarly; }
};

#endif
