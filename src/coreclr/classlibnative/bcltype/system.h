// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: System.h
//

//
// Purpose: Native methods on System.System
//

#ifndef _SYSTEM_H_
#define _SYSTEM_H_

#include "fcall.h"
#include "qcall.h"

class SystemNative
{
    friend class DebugStackTrace;

private:
    struct CaptureStackTraceData
    {
        // Used for the integer-skip version
        INT32   skip;

        INT32   cElementsAllocated;
        INT32   cElements;
        StackTraceElement* pElements;
        void*   pStopStack;   // use to limit the crawl

        CaptureStackTraceData() : skip(0), cElementsAllocated(0), cElements(0), pElements(NULL), pStopStack((void*)-1)
        {
            LIMITED_METHOD_CONTRACT;
        }
    };

public:
    // Functions on the System.Environment class
    static FCDECL0(UINT32, GetTickCount);
    static FCDECL0(UINT64, GetTickCount64);

    static FCDECL1(VOID,SetExitCode,INT32 exitcode);
    static FCDECL0(INT32, GetExitCode);

    static FCDECL0(FC_BOOL_RET, IsServerGC);

    // Return a method info for the method were the exception was thrown
    static FCDECL1(ReflectMethodObject*, GetMethodFromStackTrace, ArrayBase* pStackTraceUNSAFE);
    
    // Common processing code for FailFast
    static void GenericFailFast(STRINGREF refMesgString, EXCEPTIONREF refExceptionForWatsonBucketing, StackCrawlMark* stackCrawlMark, STRINGREF errorSource);
};

extern "C" void QCALLTYPE Environment_Exit(INT32 exitcode);

extern "C" void QCALLTYPE Environment_FailFast(QCall::StackCrawlMarkHandle mark, QCall::StringHandleOnStack message, QCall::ObjectHandleOnStack exception, QCall::StringHandleOnStack errorSource);

// Returns the number of logical processors that can be used by managed code
extern "C" INT32 QCALLTYPE Environment_GetProcessorCount();

#if defined(TARGET_X86) || defined(TARGET_AMD64)
extern "C" void QCALLTYPE X86BaseCpuId(int cpuInfo[4], int functionId, int subFunctionId);
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE GetTypeLoadExceptionMessage(UINT32 resId, QCall::StringHandleOnStack retString);

extern "C" void QCALLTYPE GetFileLoadExceptionMessage(UINT32 hr, QCall::StringHandleOnStack retString);

extern "C" void QCALLTYPE FileLoadException_GetMessageForHR(UINT32 hresult, QCall::StringHandleOnStack retString);

#endif // _SYSTEM_H_

