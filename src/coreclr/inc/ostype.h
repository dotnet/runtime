// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "staticcontract.h"

#ifndef WRAPPER_NO_CONTRACT
#define WRAPPER_NO_CONTRACT
#endif

#ifndef LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_CONTRACT
#endif

#ifdef HOST_64BIT
inline BOOL RunningInWow64()
{
    return FALSE;
}
#else
BOOL RunningInWow64();
#endif
