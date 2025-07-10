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

class CLRToCOMCall
{
    public:
        //---------------------------------------------------------
        // Debugger helper function
        //---------------------------------------------------------
        static TADDR GetFrameCallIP(FramedMethodFrame *frame);

        static MethodDesc* GetILStubMethodDesc(MethodDesc* pMD, DWORD dwStubFlags);
        static PCODE       GetStubForILStub(MethodDesc* pMD, MethodDesc** ppStubMD);

        static CLRToCOMCallInfo *PopulateCLRToCOMCallMethodDesc(MethodDesc* pMD, DWORD* pdwStubFlags);
    private:
        CLRToCOMCall();     // prevent "new"'s on this class
};

#endif // __CLRTOCOMCALL_H__
