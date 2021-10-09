// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _JITHOST
#define _JITHOST

class JitHost final : public ICorJitHost
{
public:
    JitHost(JitInstance& jitInstance);

#include "icorjithostimpl.h"

private:
    bool convertStringValueToInt(const WCHAR* key, const WCHAR* stringValue, int& result);

    JitInstance& jitInstance;
};

#endif
