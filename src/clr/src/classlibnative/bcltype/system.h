// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: System.h
//

//
// Purpose: Native methods on System.System
//

//

#ifndef _SYSTEM_H_
#define _SYSTEM_H_

#include "fcall.h"
#include "qcall.h"

// Corresponding to managed class Microsoft.Win32.OSVERSIONINFO
class OSVERSIONINFOObject : public Object
{
   public:
  STRINGREF szCSDVersion;
  DWORD dwOSVersionInfoSize;
  DWORD dwMajorVersion;
  DWORD dwMinorVersion;
  DWORD dwBuildNumber;
  DWORD dwPlatformId;
};

//Corresponding to managed class Microsoft.Win32.OSVERSIONINFOEX
class OSVERSIONINFOEXObject : public Object
{
   public:
  STRINGREF szCSDVersion;
  DWORD dwOSVersionInfoSize;
  DWORD dwMajorVersion;
  DWORD dwMinorVersion;
  DWORD dwBuildNumber;
  DWORD dwPlatformId;
  WORD  wServicePackMajor;
  WORD  wServicePackMinor;
  WORD  wSuiteMask;
  BYTE  wProductType;
  BYTE  wReserved;
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
    static FCDECL0(INT64, __GetSystemTimeAsFileTime);
    static FCDECL0(UINT32, GetTickCount);
    static FCDECL1(FC_BOOL_RET, GetOSVersion, OSVERSIONINFOObject *osVer);
    static FCDECL1(FC_BOOL_RET, GetOSVersionEx, OSVERSIONINFOEXObject *osVer);

#ifndef FEATURE_CORECLR
    static
    INT64 QCALLTYPE GetWorkingSet();
#endif // !FEATURE_CORECLR

    static
    void QCALLTYPE Exit(INT32 exitcode);

    static FCDECL1(VOID,SetExitCode,INT32 exitcode);
    static FCDECL0(INT32, GetExitCode);

    static
    void QCALLTYPE _GetCommandLine(QCall::StringHandleOnStack retString);

    static FCDECL0(Object*, GetCommandLineArgs);
    static FCDECL1(FC_BOOL_RET, _GetCompatibilityFlag, int flag);
    static FCDECL1(VOID, FailFast, StringObject* refMessageUNSAFE);
    static FCDECL2(VOID, FailFastWithExitCode, StringObject* refMessageUNSAFE, UINT exitCode);
    static FCDECL2(VOID, FailFastWithException, StringObject* refMessageUNSAFE, ExceptionObject* refExceptionUNSAFE);
#ifndef FEATURE_CORECLR
    static void QCALLTYPE TriggerCodeContractFailure(ContractFailureKind failureKind, LPCWSTR pMessage, LPCWSTR pCondition, LPCWSTR exceptionAsText);
    static BOOL QCALLTYPE IsCLRHosted();
#endif // !FEATURE_CORECLR

    static FCDECL0(StringObject*, GetDeveloperPath);
    static FCDECL1(Object*,       _GetEnvironmentVariable, StringObject* strVar);
    static FCDECL0(StringObject*, _GetModuleFileName);
    static FCDECL0(StringObject*, GetRuntimeDirectory);
    static FCDECL0(StringObject*, GetHostBindingFile);
    static LPVOID QCALLTYPE GetRuntimeInterfaceImpl(REFCLSID clsid, REFIID   riid);
    static void QCALLTYPE _GetSystemVersion(QCall::StringHandleOnStack retVer);

    // Returns the number of logical processors that can be used by managed code
	static INT32 QCALLTYPE GetProcessorCount();

    static FCDECL0(FC_BOOL_RET, HasShutdownStarted);
    static FCDECL0(FC_BOOL_RET, IsServerGC);

#ifdef FEATURE_COMINTEROP
    static
    BOOL QCALLTYPE WinRTSupported();
#endif // FEATURE_COMINTEROP

    // Return a method info for the method were the exception was thrown
    static FCDECL1(ReflectMethodObject*, GetMethodFromStackTrace, ArrayBase* pStackTraceUNSAFE);

#ifndef FEATURE_CORECLR    
    // Functions on the System.TimeSpan class
    static FCDECL0(FC_BOOL_RET, LegacyFormatMode);
	// Function on the DateTime
    static BOOL QCALLTYPE EnableAmPmParseAdjustment();
	static BOOL QCALLTYPE LegacyDateTimeParseMode();
#endif // !FEATURE_CORECLR

	
// Move this into a separate CLRConfigQCallWrapper class once CLRConfif has been refactored:
#ifndef FEATURE_CORECLR        
    static FCDECL0(FC_BOOL_RET, CheckLegacyManagedDeflateStream);
#endif // !FEATURE_CORECLR
	
#ifndef FEATURE_CORECLR        
    static FCDECL0(FC_BOOL_RET, CheckThrowUnobservedTaskExceptions);
#endif // !FEATURE_CORECLR

private:
    // Common processing code for FailFast
    static void GenericFailFast(STRINGREF refMesgString, EXCEPTIONREF refExceptionForWatsonBucketing, UINT_PTR retAddress, UINT exitCode);
};

/* static */
void QCALLTYPE GetTypeLoadExceptionMessage(UINT32 resId, QCall::StringHandleOnStack retString);

/* static */
void QCALLTYPE GetFileLoadExceptionMessage(UINT32 hr, QCall::StringHandleOnStack retString);

/* static */
void QCALLTYPE FileLoadException_GetMessageForHR(UINT32 hresult, QCall::StringHandleOnStack retString);

#endif // _SYSTEM_H_

