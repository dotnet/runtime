// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _JITHOST
#define _JITHOST

class JitHost : public ICorJitHost
{
public:
    JitHost(ICorJitHost* wrappedHost);

    void setMethodCallSummarizer(MethodCallSummarizer* methodCallSummarizer);

#include "icorjithostimpl.h"

private:
    ICorJitHost*          wrappedHost;
    MethodCallSummarizer* mcs;
};

extern JitHost* g_ourJitHost;

#endif
