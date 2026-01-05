// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// EXCEPX86.H -
//
// This header file is optionally included from Excep.h if the target platform is x86
//


#ifndef __excepx86_h__
#define __excepx86_h__

#include "corerror.h"

#include "../dlls/mscorrc/resource.h"

#define THROW_CONTROL_FOR_THREAD_FUNCTION  ThrowControlForThread

#define STATUS_CLR_GCCOVER_CODE         STATUS_PRIVILEGED_INSTRUCTION

#ifdef TARGET_WINDOWS
#define INSTALL_SEH_RECORD(record)                                        \
    {                                                                     \
       (record)->Next = (PEXCEPTION_REGISTRATION_RECORD)__readfsdword(0); \
       __writefsdword(0, (DWORD) (record));                               \
    }

#define UNINSTALL_SEH_RECORD(record)                                      \
    {                                                                     \
        __writefsdword(0, (DWORD) ((record)->Next));                      \
    }
#endif // TARGET_WINDOWS

//
// Retrieves the redirected CONTEXT* from the stack frame of one of the
// RedirectedHandledJITCaseForXXX_Stub's.
//
PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(CONTEXT * pContext);

// Determine the address of the instruction that made the current call.
inline
PCODE GetAdjustedCallAddress(PCODE returnAddress)
{
    LIMITED_METHOD_CONTRACT;
    return returnAddress - 5;
}

BOOL AdjustContextForVirtualStub(EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pContext);

#endif // __excepx86_h__
