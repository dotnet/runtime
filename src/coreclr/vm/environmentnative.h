// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: EnvironmentNative.h
//

//
// Purpose: Native methods on System.Environment
//

#ifndef _ENVIRONMENT_NATIVE_H_
#define _ENVIRONMENT_NATIVE_H_

#include "fcall.h"
#include "qcall.h"

class SystemNative
{
public:
    // Functions on the System.Environment class
    static FCDECL1(VOID,SetExitCode,INT32 exitcode);
    static FCDECL0(INT32, GetExitCode);
};

extern "C" void QCALLTYPE Environment_Exit(INT32 exitcode);

extern "C" void QCALLTYPE Environment_FailFast(QCall::StackCrawlMarkHandle mark, PCWSTR message, QCall::ObjectHandleOnStack exception, PCWSTR errorSource);

// Returns the number of logical processors that can be used by managed code
extern "C" INT32 QCALLTYPE Environment_GetProcessorCount();

#if defined(TARGET_X86) || defined(TARGET_AMD64)
extern "C" void QCALLTYPE X86BaseCpuId(int cpuInfo[4], int functionId, int subFunctionId);
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE GetTypeLoadExceptionMessage(UINT32 resId, QCall::StringHandleOnStack retString);

extern "C" void QCALLTYPE GetFileLoadExceptionMessage(UINT32 hr, QCall::StringHandleOnStack retString);

extern "C" void QCALLTYPE FileLoadException_GetMessageForHR(UINT32 hresult, QCall::StringHandleOnStack retString);

#endif // _ENVIRONMENT_NATIVE_H_

