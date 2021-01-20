//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _JITHOST
#define _JITHOST

class JitHost : public ICorJitHost
{
public:
    JitHost(ICorJitHost* wrappedHost, MethodContext* methodContext);

    void setMethodContext(MethodContext* methodContext);

#include "icorjithostimpl.h"

private:
    ICorJitHost*   wrappedHost;
    MethodContext* mc;
};

extern JitHost* g_ourJitHost;

#endif
