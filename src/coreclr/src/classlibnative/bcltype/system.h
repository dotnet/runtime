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

struct FullSystemTime
{
    SYSTEMTIME systemTime;
    INT64 hundredNanoSecond;
};

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
#ifndef TARGET_UNIX
    static FCDECL1(VOID, GetSystemTimeWithLeapSecondsHandling, FullSystemTime *time);
    static FCDECL2(FC_BOOL_RET, ValidateSystemTime, SYSTEMTIME *time, CLR_BOOL localTime);
    static FCDECL2(FC_BOOL_RET, FileTimeToSystemTime, INT64 fileTime, FullSystemTime *time);
    static FCDECL2(FC_BOOL_RET, SystemTimeToFileTime, SYSTEMTIME *time, INT64 *pFileTime);
#endif // TARGET_UNIX
    static FCDECL0(INT64, __GetSystemTimeAsFileTime);
    static FCDECL0(UINT32, GetTickCount);
    static FCDECL0(UINT64, GetTickCount64);

    static
    void QCALLTYPE Exit(INT32 exitcode);

    static FCDECL1(VOID,SetExitCode,INT32 exitcode);
    static FCDECL0(INT32, GetExitCode);

    static
    void QCALLTYPE _GetCommandLine(QCall::StringHandleOnStack retString);

    static FCDECL0(Object*, GetCommandLineArgs);
    static FCDECL1(VOID, FailFast, StringObject* refMessageUNSAFE);
    static FCDECL2(VOID, FailFastWithExitCode, StringObject* refMessageUNSAFE, UINT exitCode);
    static FCDECL2(VOID, FailFastWithException, StringObject* refMessageUNSAFE, ExceptionObject* refExceptionUNSAFE);
    static FCDECL3(VOID, FailFastWithExceptionAndSource, StringObject* refMessageUNSAFE, ExceptionObject* refExceptionUNSAFE, StringObject* errorSourceUNSAFE);

    // Returns the number of logical processors that can be used by managed code
    static INT32 QCALLTYPE GetProcessorCount();

    static FCDECL0(FC_BOOL_RET, IsServerGC);

#ifdef FEATURE_COMINTEROP
    static
    BOOL QCALLTYPE WinRTSupported();
#endif // FEATURE_COMINTEROP

    // Return a method info for the method were the exception was thrown
    static FCDECL1(ReflectMethodObject*, GetMethodFromStackTrace, ArrayBase* pStackTraceUNSAFE);

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    static void QCALLTYPE X86BaseCpuId(int cpuInfo[4], int functionId, int subFunctionId);
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

private:
    // Common processing code for FailFast
    static void GenericFailFast(STRINGREF refMesgString, EXCEPTIONREF refExceptionForWatsonBucketing, UINT_PTR retAddress, UINT exitCode, STRINGREF errorSource);
};

/* static */
void QCALLTYPE GetTypeLoadExceptionMessage(UINT32 resId, QCall::StringHandleOnStack retString);

/* static */
void QCALLTYPE GetFileLoadExceptionMessage(UINT32 hr, QCall::StringHandleOnStack retString);

/* static */
void QCALLTYPE FileLoadException_GetMessageForHR(UINT32 hresult, QCall::StringHandleOnStack retString);

#endif // _SYSTEM_H_

