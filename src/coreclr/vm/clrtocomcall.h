// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CLRtoCOMCall.h
//

//
// Used to handle stub creation for managed to unmanaged transitions.
//


#ifndef __CLRTOCOMCALL_H__
#define __CLRTOCOMCALL_H__

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "util.hpp"

class ILStubResolver;

class CLRToCOMCall
{
    public:
        // Generates the transient IL implementation for a CLR->COM call.
        static COR_ILMETHOD_DECODER* CreateCLRToCOMCallMethodIL(MethodDesc* pMD, DynamicResolver** ppResolver);

        // Returns the user-provided IL stub method for this CLR->COM call, or NULL if there isn't one.
        static MethodDesc* GetPredefinedILStubMethod(MethodDesc* pMD);

        static CLRToCOMCallInfo *PopulateCLRToCOMCallMethodDesc(MethodDesc* pMD, DWORD* pdwStubFlags);
    private:
        CLRToCOMCall();     // prevent "new"'s on this class
};

#endif // __CLRTOCOMCALL_H__
