// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: System.cpp
//

//
// Purpose: Native methods on System.Environment & Array
//

//

#include "common.h"

#include <object.h>

#include "ceeload.h"

#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "classnames.h"
#include "system.h"
#include "string.h"
#include "sstring.h"
#include "eeconfig.h"
#include "assemblynative.hpp"
#include "generics.h"
#include "invokeutil.h"
#include "array.h"
#include "eepolicy.h"

#if !defined(FEATURE_CORECLR)
#include "metahost.h"
#endif // !FEATURE_CORECLR

#ifdef FEATURE_WINDOWSPHONE
Volatile<BOOL> g_fGetPhoneVersionInitialized;

// This is the API to query the phone version information
typedef BOOL (*pfnGetPhoneVersion)(LPOSVERSIONINFO lpVersionInformation);

pfnGetPhoneVersion g_pfnGetPhoneVersion = NULL;
#endif


FCIMPL0(INT64, SystemNative::__GetSystemTimeAsFileTime)
{
    FCALL_CONTRACT;

    INT64 timestamp;

    ::GetSystemTimeAsFileTime((FILETIME*)&timestamp);

#if BIGENDIAN
    timestamp = (INT64)(((UINT64)timestamp >> 32) | ((UINT64)timestamp << 32));
#endif

    return timestamp;
}
FCIMPLEND;



FCIMPL0(UINT32, SystemNative::GetTickCount)
{
    FCALL_CONTRACT;
    
    return ::GetTickCount();
}
FCIMPLEND;



#ifndef FEATURE_CORECLR
INT64 QCALLTYPE SystemNative::GetWorkingSet()
{
    QCALL_CONTRACT;

    DWORD memUsage = 0;
        
    BEGIN_QCALL;
    memUsage = WszGetWorkingSet();
    END_QCALL;
    
    return memUsage;
}
#endif // !FEATURE_CORECLR

VOID QCALLTYPE SystemNative::Exit(INT32 exitcode)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via setting
    // Environment's ExitCode property.
    SetLatchedExitCode(exitcode);

    ForceEEShutdown();

    END_QCALL;
}

FCIMPL1(VOID,SystemNative::SetExitCode,INT32 exitcode)
{
    FCALL_CONTRACT;
    
    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via setting
    // Environment's ExitCode property.
    SetLatchedExitCode(exitcode);
}
FCIMPLEND

FCIMPL0(INT32, SystemNative::GetExitCode)
{
    FCALL_CONTRACT;
    
    // Return whatever has been latched so far.  This is uninitialized to 0.
    return GetLatchedExitCode();
}
FCIMPLEND

void QCALLTYPE SystemNative::_GetCommandLine(QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    LPCWSTR commandLine;

    if (g_pCachedCommandLine != NULL)
    {
        // Use the cached command line if available
        commandLine = g_pCachedCommandLine;
    }
    else
    {
        commandLine = WszGetCommandLine();
        if (commandLine==NULL)
            COMPlusThrowOM();
    }
    
    retString.Set(commandLine);

    END_QCALL;
}

FCIMPL0(Object*, SystemNative::GetCommandLineArgs)
{
    FCALL_CONTRACT;

    PTRARRAYREF strArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(strArray);

    LPWSTR commandLine;

    if (g_pCachedCommandLine != NULL)
    {
        // Use the cached command line if available
        commandLine = g_pCachedCommandLine;
    }
    else
    {
        commandLine = WszGetCommandLine();
        if (commandLine==NULL)
            COMPlusThrowOM();
    }

    DWORD numArgs = 0;
    LPWSTR* argv = SegmentCommandLine(commandLine, &numArgs);
    if (!argv)
        COMPlusThrowOM();

    _ASSERTE(numArgs > 0);
    
    strArray = (PTRARRAYREF) AllocateObjectArray(numArgs, g_pStringClass);
    // Copy each argument into new Strings.
    for(unsigned int i=0; i<numArgs; i++) 
    {
        STRINGREF str = StringObject::NewString(argv[i]);
        STRINGREF * destData = ((STRINGREF*)(strArray->GetDataPtr())) + i;
        SetObjectReference((OBJECTREF*)destData, (OBJECTREF)str, strArray->GetAppDomain());
    }
    delete [] argv;

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(strArray); 
}
FCIMPLEND


FCIMPL1(FC_BOOL_RET, SystemNative::_GetCompatibilityFlag, int flag)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(GetCompatibilityFlag((CompatibilityFlag)flag));
}
FCIMPLEND

// Note: Arguments checked in IL.
FCIMPL1(Object*, SystemNative::_GetEnvironmentVariable, StringObject* strVarUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF refRetVal;
    STRINGREF strVar;

    refRetVal   = NULL;
    strVar      = ObjectToSTRINGREF(strVarUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_2(refRetVal, strVar);

    int len;

    // Get the length of the environment variable.
    PathString envPath;    // prefix complains if pass a null ptr in, so rely on the final length parm instead
    len = WszGetEnvironmentVariable(strVar->GetBuffer(), envPath);

    if (len != 0)
    {
        // Allocate the string.
        refRetVal = StringObject::NewString(len);
 
        wcscpy_s(refRetVal->GetBuffer(), len + 1, envPath);
        
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

// Return a method info for the method were the exception was thrown
FCIMPL1(ReflectMethodObject*, SystemNative::GetMethodFromStackTrace, ArrayBase* pStackTraceUNSAFE)
{
    FCALL_CONTRACT;
    
    I1ARRAYREF pArray(static_cast<I1Array *>(pStackTraceUNSAFE));
    StackTraceArray stackArray(pArray);

    if (!stackArray.Size())
        return NULL;

    // The managed stacktrace classes always returns typical method definition, so we don't need to bother providing exact instantiation.
    // Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation(pElements[0].pFunc, pElements[0].pExactGenericArgsToken, pTypeHandle, &pMD);

    MethodDesc* pFunc = stackArray[0].pFunc;

    // Strip the instantiation to make sure that the reflection never gets a bad method desc back.
    REFLECTMETHODREF refRet = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0()
    pFunc = pFunc->LoadTypicalMethodDefinition();
    refRet = pFunc->GetStubMethodInfo();
    _ASSERTE(pFunc->IsRuntimeMethodHandle());

    HELPER_METHOD_FRAME_END();

    return (ReflectMethodObject*)OBJECTREFToObject(refRet);
}
FCIMPLEND

FCIMPL0(StringObject*, SystemNative::_GetModuleFileName)
{
    FCALL_CONTRACT;

    STRINGREF   refRetVal = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refRetVal);
    if (g_pCachedModuleFileName)
    {
        refRetVal = StringObject::NewString(g_pCachedModuleFileName);
    }
    else
    {
        PathString wszFilePathString;

       
        DWORD lgth = WszGetModuleFileName(NULL, wszFilePathString);
        if (!lgth)
        {
            COMPlusThrowWin32();
        }
       

        refRetVal = StringObject::NewString(wszFilePathString.GetUnicode());
    }
    HELPER_METHOD_FRAME_END();

    return (StringObject*)OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL0(StringObject*, SystemNative::GetDeveloperPath)
{
#ifdef FEATURE_FUSION
    FCALL_CONTRACT;

    STRINGREF   refDevPath  = NULL;
    LPWSTR pPath = NULL;
    DWORD lgth = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refDevPath);

    SystemDomain::System()->GetDevpathW(&pPath, &lgth);
    if(lgth) 
        refDevPath = StringObject::NewString(pPath, lgth);
    
    HELPER_METHOD_FRAME_END();
    return (StringObject*)OBJECTREFToObject(refDevPath);
#else
    return NULL;
#endif
}
FCIMPLEND

FCIMPL0(StringObject*, SystemNative::GetRuntimeDirectory)
{
    FCALL_CONTRACT;

    STRINGREF   refRetVal   = NULL;
    DWORD dwFile = MAX_LONGPATH+1;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refRetVal);
    SString wszFilePathString;

    WCHAR * wszFile = wszFilePathString.OpenUnicodeBuffer(dwFile);
    HRESULT hr = GetInternalSystemDirectory(wszFile, &dwFile);
    wszFilePathString.CloseBuffer(dwFile);
    
    if(FAILED(hr))
        COMPlusThrowHR(hr);

    dwFile--; // remove the trailing NULL

    if(dwFile)
        refRetVal = StringObject::NewString(wszFile, dwFile);

    HELPER_METHOD_FRAME_END();
    return (StringObject*)OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL0(StringObject*, SystemNative::GetHostBindingFile);
{
    FCALL_CONTRACT;

    STRINGREF refRetVal = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refRetVal);

    LPCWSTR wszFile = g_pConfig->GetProcessBindingFile();
    if(wszFile) 
        refRetVal = StringObject::NewString(wszFile);

    HELPER_METHOD_FRAME_END();
    return (StringObject*)OBJECTREFToObject(refRetVal);
}
FCIMPLEND

#ifndef FEATURE_CORECLR

void QCALLTYPE SystemNative::_GetSystemVersion(QCall::StringHandleOnStack retVer)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    WCHAR wszVersion[_MAX_PATH];
    DWORD dwVersion = _MAX_PATH;

    // Get the version
    IfFailThrow(g_pCLRRuntime->GetVersionString(wszVersion, &dwVersion));
    retVer.Set(wszVersion);

    END_QCALL;
}

#endif

INT32 QCALLTYPE SystemNative::GetProcessorCount()
{
    QCALL_CONTRACT;

    INT32 processorCount = 0;

    BEGIN_QCALL;

    CPUGroupInfo::EnsureInitialized();

    if(CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
    {
        processorCount = CPUGroupInfo::GetNumActiveProcessors();
    }

    // Processor count will be 0 if CPU groups are disabled/not supported
    if(processorCount == 0)
    {
        SYSTEM_INFO systemInfo;
        ZeroMemory(&systemInfo, sizeof(systemInfo));

        GetSystemInfo(&systemInfo);

        processorCount = systemInfo.dwNumberOfProcessors;
    }

    END_QCALL;

    return processorCount;
}

#ifdef FEATURE_CLASSIC_COMINTEROP

LPVOID QCALLTYPE SystemNative::GetRuntimeInterfaceImpl(
    /*in*/ REFCLSID clsid,
    /*in*/ REFIID   riid)
{
    QCALL_CONTRACT;

    LPVOID pUnk = NULL;

    BEGIN_QCALL;

#ifdef FEATURE_CORECLR
    IfFailThrow(E_NOINTERFACE);
#else
    HRESULT hr = g_pCLRRuntime->GetInterface(clsid, riid, &pUnk);

    if (FAILED(hr))
        hr = g_pCLRRuntime->QueryInterface(riid, &pUnk);

    IfFailThrow(hr);
#endif

    END_QCALL;

    return pUnk;
}

#endif

FCIMPL0(FC_BOOL_RET, SystemNative::HasShutdownStarted)
{
    FCALL_CONTRACT;

    // Return true if the EE has started to shutdown and is now going to 
    // aggressively finalize objects referred to by static variables OR
    // if someone is unloading the current AppDomain AND we have started
    // finalizing objects referred to by static variables.
    FC_RETURN_BOOL((g_fEEShutDown & ShutDown_Finalize2) || GetAppDomain()->IsFinalizing());
}
FCIMPLEND

// FailFast is supported in BCL.small as internal to support failing fast in places where EEE used to be thrown.
//
// Static message buffer used by SystemNative::FailFast to avoid reliance on a
// managed string object buffer. This buffer is not always used, see comments in
// the method below.
WCHAR g_szFailFastBuffer[256];
#define FAIL_FAST_STATIC_BUFFER_LENGTH (sizeof(g_szFailFastBuffer) / sizeof(WCHAR))

// This is the common code for FailFast processing that is wrapped by the two
// FailFast FCalls below.
void SystemNative::GenericFailFast(STRINGREF refMesgString, EXCEPTIONREF refExceptionForWatsonBucketing, UINT_PTR retAddress, UINT exitCode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }CONTRACTL_END;

    struct 
    {
        STRINGREF refMesgString;
        EXCEPTIONREF refExceptionForWatsonBucketing;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    
    gc.refMesgString = refMesgString;
    gc.refExceptionForWatsonBucketing = refExceptionForWatsonBucketing;

    // Managed code injected FailFast maps onto the unmanaged version
    // (EEPolicy::HandleFatalError) in the following manner: the exit code is
    // always set to COR_E_FAILFAST and the address passed (usually a failing
    // EIP) is in fact the address of a unicode message buffer (explaining the
    // reason for the fault).
    // The message string comes from a managed string object so we can't rely on
    // the buffer remaining in place below our feet. But equally we don't want
    // to inject failure points (by, for example, allocating a heap buffer or a
    // pinning handle) when we have a much higher chance than usual of actually
    // tripping those failure points and eradicating useful debugging info.
    // We employ various strategies to deal with this:
    //   o  If the message is small enough we copy it into a static buffer
    //      (g_szFailFastBuffer).
    //   o  Otherwise we try to allocate a buffer of the required size on the
    //      heap. This buffer will be leaked.
    //   o  If the allocation above fails we return to the static buffer and
    //      truncate the message.
    //
    // Another option would seem to be to implement a new frame type that
    // protects object references as pinned, but that seems like overkill for
    // just this problem.
    WCHAR  *pszMessage = NULL;
    DWORD   cchMessage = (gc.refMesgString == NULL) ? 0 : gc.refMesgString->GetStringLength();

    if (cchMessage < FAIL_FAST_STATIC_BUFFER_LENGTH)
    {
        pszMessage = g_szFailFastBuffer;
    }
    else
    {
        // We can fail here, but we can handle the fault.
        CONTRACT_VIOLATION(FaultViolation);
        pszMessage = new (nothrow) WCHAR[cchMessage + 1];
        if (pszMessage == NULL)
        {
            // Truncate the message to what will fit in the static buffer.
            cchMessage = FAIL_FAST_STATIC_BUFFER_LENGTH - 1;
            pszMessage = g_szFailFastBuffer;
        }
    }
    
    if (cchMessage > 0)
        memcpyNoGCRefs(pszMessage, gc.refMesgString->GetBuffer(), cchMessage * sizeof(WCHAR));
    pszMessage[cchMessage] = W('\0');

    if (cchMessage == 0) {
        WszOutputDebugString(W("CLR: Managed code called FailFast without specifying a reason.\r\n"));
    }
    else {
        WszOutputDebugString(W("CLR: Managed code called FailFast, saying \""));
        WszOutputDebugString(pszMessage);
        WszOutputDebugString(W("\"\r\n"));
    }

    Thread *pThread = GetThread();

#ifndef FEATURE_PAL    
    // If we have the exception object, then try to setup
    // the watson bucket if it has any details.
#ifdef FEATURE_CORECLR
    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this, if required.
    if (IsWatsonEnabled())
#endif // FEATURE_CORECLR
    {
        BEGIN_SO_INTOLERANT_CODE(pThread);
        if ((gc.refExceptionForWatsonBucketing == NULL) || !SetupWatsonBucketsForFailFast(gc.refExceptionForWatsonBucketing))
        {
            PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pThread->GetExceptionState()->GetUEWatsonBucketTracker();
            _ASSERTE(pUEWatsonBucketTracker != NULL);
            pUEWatsonBucketTracker->SaveIpForWatsonBucket(retAddress);
            pUEWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::FatalError, pThread, NULL);
            if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
            {
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            }
        }
        END_SO_INTOLERANT_CODE;
    }
#endif // !FEATURE_PAL

    // stash the user-provided exception object. this will be used as
    // the inner exception object to the FatalExecutionEngineException.
    if (gc.refExceptionForWatsonBucketing != NULL)
        pThread->SetLastThrownObject(gc.refExceptionForWatsonBucketing);

    EEPolicy::HandleFatalError(exitCode, retAddress, pszMessage);

    GCPROTECT_END();
}

// Note: Do not merge this FCALL method with any other FailFast overloads.
// Watson uses the managed FailFast method with one String for crash dump bucketization. 
FCIMPL1(VOID, SystemNative::FailFast, StringObject* refMessageUNSAFE)
{   
    FCALL_CONTRACT;

    STRINGREF refMessage = (STRINGREF)refMessageUNSAFE;
    
    HELPER_METHOD_FRAME_BEGIN_1(refMessage);

    // The HelperMethodFrame knows how to get the return address.
    UINT_PTR retaddr = HELPER_METHOD_FRAME_GET_RETURN_ADDRESS();
    
    // Call the actual worker to perform failfast
    GenericFailFast(refMessage, NULL, retaddr, COR_E_FAILFAST);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(VOID, SystemNative::FailFastWithExitCode, StringObject* refMessageUNSAFE, UINT exitCode)
{   
    FCALL_CONTRACT;

    STRINGREF refMessage = (STRINGREF)refMessageUNSAFE;
    
    HELPER_METHOD_FRAME_BEGIN_1(refMessage);

    // The HelperMethodFrame knows how to get the return address.
    UINT_PTR retaddr = HELPER_METHOD_FRAME_GET_RETURN_ADDRESS();
    
    // Call the actual worker to perform failfast
    GenericFailFast(refMessage, NULL, retaddr, exitCode);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(VOID, SystemNative::FailFastWithException, StringObject* refMessageUNSAFE, ExceptionObject* refExceptionUNSAFE)
{   
    FCALL_CONTRACT;

    STRINGREF refMessage = (STRINGREF)refMessageUNSAFE;
    EXCEPTIONREF refException = (EXCEPTIONREF)refExceptionUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(refMessage, refException);

    // The HelperMethodFrame knows how to get the return address.
    UINT_PTR retaddr = HELPER_METHOD_FRAME_GET_RETURN_ADDRESS();
    
    // Call the actual worker to perform failfast
    GenericFailFast(refMessage, refException, retaddr, COR_E_FAILFAST);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


#ifndef FEATURE_CORECLR
BOOL QCALLTYPE SystemNative::IsCLRHosted()
{
    QCALL_CONTRACT;

    BOOL retVal = false;
    BEGIN_QCALL;
    retVal = (CLRHosted() & CLRHOSTED) != 0;
    END_QCALL;

    return retVal;
}

void QCALLTYPE SystemNative::TriggerCodeContractFailure(ContractFailureKind failureKind, LPCWSTR pMessage, LPCWSTR pCondition, LPCWSTR exceptionAsString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    
    GCX_COOP();

    EEPolicy::HandleCodeContractFailure(pMessage, pCondition, exceptionAsString);
    // Note: if the host chose to throw an exception, we've returned from this method and
    // will throw that exception in managed code, because it's easier to pass the right parameters there.

    END_QCALL;
}
#endif // !FEATURE_CORECLR

FCIMPL0(FC_BOOL_RET, SystemNative::IsServerGC)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(GCHeap::IsServerHeap());
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP

BOOL QCALLTYPE SystemNative::WinRTSupported()
{
    QCALL_CONTRACT;

    BOOL hasWinRT = FALSE;

    BEGIN_QCALL;
    hasWinRT = ::WinRTSupported();
    END_QCALL;

    return hasWinRT;
}

#endif // FEATURE_COMINTEROP

// Helper method to retrieve OS Version based on the environment.
BOOL GetOSVersionForEnvironment(LPOSVERSIONINFO lpVersionInformation)
{
#ifdef FEATURE_WINDOWSPHONE
    // Return phone version information if it is available
    if (!g_fGetPhoneVersionInitialized)
    {
        HMODULE hPhoneInfo = WszLoadLibrary(W("phoneinfo.dll"));
        if(hPhoneInfo != NULL)
            g_pfnGetPhoneVersion = (pfnGetPhoneVersion)GetProcAddress(hPhoneInfo, "GetPhoneVersion");

        g_fGetPhoneVersionInitialized = true;
    }

    if (g_pfnGetPhoneVersion!= NULL)
        return g_pfnGetPhoneVersion(lpVersionInformation);
#endif // FEATURE_WINDOWSPHONE

    return ::GetOSVersion(lpVersionInformation);
}


/*
 * SystemNative::GetOSVersion - Fcall corresponding to System.Environment.GetVersion
 * It calls clr!GetOSVersion to get the real OS version even when running in 
 * app compat. Calling kernel32!GetVersionEx() directly will be shimmed and will return the
 * fake OS version. In order to avoid this the call to getVersionEx is made via mscoree.dll.
 * Mscoree.dll resides in system32 dir and is never lied about OS version.
 */

FCIMPL1(FC_BOOL_RET, SystemNative::GetOSVersion, OSVERSIONINFOObject *osVer)
{
    FCALL_CONTRACT;

    OSVERSIONINFO ver;    
    ver.dwOSVersionInfoSize = osVer->dwOSVersionInfoSize;

    BOOL ret = GetOSVersionForEnvironment(&ver);

    if(ret)
    {
        osVer->dwMajorVersion  = ver.dwMajorVersion;
        osVer->dwMinorVersion = ver.dwMinorVersion;
        osVer->dwBuildNumber  = ver.dwBuildNumber;
        osVer->dwPlatformId = ver.dwPlatformId;

        HELPER_METHOD_FRAME_BEGIN_RET_1(osVer);
        SetObjectReference((OBJECTREF*)&(osVer->szCSDVersion), StringObject::NewString(ver.szCSDVersion), GetAppDomain());
        HELPER_METHOD_FRAME_END();
    }

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

/*
 * SystemNative::GetOSVersionEx - Fcall implementation for System.Environment.GetVersionEx
 * Similar as above except this takes OSVERSIONINFOEX structure as input
 */

FCIMPL1(FC_BOOL_RET, SystemNative::GetOSVersionEx, OSVERSIONINFOEXObject *osVer)
{
    FCALL_CONTRACT;

    OSVERSIONINFOEX ver;
    ver.dwOSVersionInfoSize = osVer->dwOSVersionInfoSize;

    BOOL ret = GetOSVersionForEnvironment((OSVERSIONINFO *)&ver);

    if(ret)
    {
        osVer->dwMajorVersion  = ver.dwMajorVersion;
        osVer->dwMinorVersion = ver.dwMinorVersion;
        osVer->dwBuildNumber  = ver.dwBuildNumber;
        osVer->dwPlatformId = ver.dwPlatformId;
        osVer->wServicePackMajor = ver.wServicePackMajor;
        osVer->wServicePackMinor = ver.wServicePackMinor;
        osVer->wSuiteMask = ver.wSuiteMask;
        osVer->wProductType = ver.wProductType;
        osVer->wReserved = ver.wReserved;

        HELPER_METHOD_FRAME_BEGIN_RET_1(osVer);
        SetObjectReference((OBJECTREF*)&(osVer->szCSDVersion), StringObject::NewString(ver.szCSDVersion), GetAppDomain());
        HELPER_METHOD_FRAME_END();
    }

    FC_RETURN_BOOL(ret);
}
FCIMPLEND


#ifndef FEATURE_CORECLR  
//
// SystemNative::LegacyFormatMode - Fcall implementation for System.TimeSpan.LegacyFormatMode
// checks for the DWORD "TimeSpan_LegacyFormatMode" CLR config option
//
FCIMPL0(FC_BOOL_RET, SystemNative::LegacyFormatMode)
{
    FCALL_CONTRACT;

    DWORD flag = 0;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    flag = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_TimeSpan_LegacyFormatMode);
    END_SO_INTOLERANT_CODE;

    if (flag)
        FC_RETURN_BOOL(TRUE);
    else
        FC_RETURN_BOOL(FALSE);
}
FCIMPLEND
#endif // !FEATURE_CORECLR  

	
#ifndef FEATURE_CORECLR  
//
// SystemNative::CheckLegacyManagedDeflateStream - Fcall implementation for System.IO.Compression.DeflateStream
// checks for the DWORD "NetFx45_LegacyManagedDeflateStream" CLR config option
//
// Move this into a separate CLRConfigQCallWrapper class once CLRConfig has been refactored!
//
FCIMPL0(FC_BOOL_RET, SystemNative::CheckLegacyManagedDeflateStream)
{
    FCALL_CONTRACT;

    DWORD flag = 0;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    flag = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_NetFx45_LegacyManagedDeflateStream);
    END_SO_INTOLERANT_CODE;

    if (flag)
        FC_RETURN_BOOL(TRUE);
    else
        FC_RETURN_BOOL(FALSE);
}
FCIMPLEND
#endif // !FEATURE_CORECLR  

#ifndef FEATURE_CORECLR  
//
// SystemNative::CheckThrowUnobservedTaskExceptions - Fcall implementation for System.Threading.Tasks.TaskExceptionHolder
// checks for the DWORD "ThrowUnobservedTaskExceptions" CLR config option
//
FCIMPL0(FC_BOOL_RET, SystemNative::CheckThrowUnobservedTaskExceptions)
{
    FCALL_CONTRACT;

    DWORD flag = 0;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    flag = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ThrowUnobservedTaskExceptions);
    END_SO_INTOLERANT_CODE;

    if (flag)
        FC_RETURN_BOOL(TRUE);
    else
        FC_RETURN_BOOL(FALSE);
}
FCIMPLEND

BOOL QCALLTYPE SystemNative::LegacyDateTimeParseMode()
{
    QCALL_CONTRACT;

    BOOL retVal = false;
    BEGIN_QCALL;
    retVal = (BOOL) CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DateTime_NetFX35ParseMode);
    END_QCALL;

    return retVal;
}

//
// This method used with DateTimeParse to fix the parsing of AM/PM like "1/10 5 AM" case
//
BOOL QCALLTYPE SystemNative::EnableAmPmParseAdjustment()
{
    QCALL_CONTRACT;

    BOOL retVal = false;
    BEGIN_QCALL;
    retVal = (BOOL) CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DateTime_NetFX40AmPmParseAdjustment);
    END_QCALL;

    return retVal;
}


#endif // !FEATURE_CORECLR  

