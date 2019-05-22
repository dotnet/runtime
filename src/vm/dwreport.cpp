// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// FILE: dwreport.cpp
//

//

//
// ============================================================================

#include "common.h"

#include "dwreport.h"
#include "dwbucketmanager.hpp"
#include <cordbpriv.h>
#include "field.h"
#include <msodwwrap.h>
#include <shlobj.h>
#include "dbginterface.h"
#include <sha1.h>
#include <winver.h>
#include "dlwrap.h"
#include "eemessagebox.h"
#include "eventreporter.h"
#include "utilcode.h"
#include "../dlls/mscorrc/resource.h"   // for resource ids


EFaultRepRetVal DoReportFault(EXCEPTION_POINTERS * pExceptionInfo);


// Variables to control launching Watson only once, but making all threads wait for that single launch to finish.
LONG g_watsonAlreadyLaunched = 0; // Used to note that another thread has done Watson.

typedef HMODULE (*AcquireLibraryHandleFn)(LPCWSTR);

template <AcquireLibraryHandleFn AcquireLibraryHandleFnPtr, bool RequiresFree>
class SimpleModuleHolder
{
private:
    HMODULE hModule;

public:
    SimpleModuleHolder(LPCWSTR moduleName)
    {
        hModule = AcquireLibraryHandleFnPtr(moduleName);
    }
    
    ~SimpleModuleHolder()
    {
        if (RequiresFree && hModule)
        {
            CLRFreeLibrary(hModule);
        }
    }
    
    operator HMODULE() { return hModule; }
};

#ifndef FEATURE_CORESYSTEM
#define WER_MODULE_NAME_W WINDOWS_KERNEL32_DLLNAME_W
typedef SimpleModuleHolder<WszGetModuleHandle, false> WerModuleHolder;
#else
#define WER_MODULE_NAME_W W("api-ms-win-core-windowserrorreporting-l1-1-0.dll")
typedef SimpleModuleHolder<CLRLoadLibrary, true> WerModuleHolder;
#endif

//------------------------------------------------------------------------------
// Description
//  Indicate if Watson is enabled
//
// Parameters
//  None
//
// Returns
//  TRUE  -- Yes, Watson is enabled.
//  FALSE -- No, it's not.
//------------------------------------------------------------------------------
BOOL IsWatsonEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return TRUE;
}

//------------------------------------------------------------------------------
// Description
//  Register out-of-process Watson callbacks provided in DAC dll for WIN7 or later
//
// Parameters
//  None
//
// Returns
//  None
//
// Note: In Windows 7, the OS will take over the job of error reporting, and so most 
// of our watson code should not be used.  In such cases, we will however still need 
// to provide some services to windows error reporting, such as computing bucket 
// parameters for a managed unhandled exception.  
//------------------------------------------------------------------------------
BOOL RegisterOutOfProcessWatsonCallbacks()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    WCHAR wszDACName[] = MAIN_DAC_MODULE_NAME_W W(".dll");
    WerModuleHolder hWerModule(WER_MODULE_NAME_W);

#ifdef FEATURE_CORESYSTEM
    if ((hWerModule == NULL) && !RunningOnWin8())
    {
        // If we are built for CoreSystemServer, but are running on Windows 7, we need to look elsewhere
        hWerModule = WerModuleHolder(W("Kernel32.dll"));
    }
#endif

    if (hWerModule == NULL)
    {
        _ASSERTE(!"failed to get WER module handle");
        return FALSE;
    }

    typedef HRESULT (WINAPI * WerRegisterRuntimeExceptionModuleFnPtr)(PCWSTR, PDWORD);
    WerRegisterRuntimeExceptionModuleFnPtr pFnWerRegisterRuntimeExceptionModule;

    pFnWerRegisterRuntimeExceptionModule = (WerRegisterRuntimeExceptionModuleFnPtr)
                                        GetProcAddress(hWerModule, "WerRegisterRuntimeExceptionModule"); 

    _ASSERTE(pFnWerRegisterRuntimeExceptionModule != NULL);
    if (pFnWerRegisterRuntimeExceptionModule == NULL)
    {
       return FALSE;
    }
    HRESULT hr = S_OK;

    EX_TRY
    {
        PathString wszDACPath;
        if (SUCCEEDED(::GetCORSystemDirectoryInternaL(wszDACPath)))
        {
            wszDACPath.Append(wszDACName);
            hr = (*pFnWerRegisterRuntimeExceptionModule)(wszDACPath, (PDWORD)g_pMSCorEE);
        }
        else {
            hr = E_FAIL;
        }

    }
    EX_CATCH_HRESULT(hr);
    
    if (FAILED(hr))
    {
        STRESS_LOG0(LF_STARTUP, 
                    LL_ERROR, 
                    "WATSON support: failed to register DAC dll with WerRegisterRuntimeExceptionModule");

#ifdef FEATURE_CORESYSTEM
        // For CoreSys we *could* be running on a platform that doesn't have Watson proper 
        // (the APIs might exist but they just fail).  
        // WerRegisterRuntimeExceptionModule may return E_NOIMPL.
        return TRUE;
#else // FEATURE_CORESYSTEM
       _ASSERTE(! "WATSON support: failed to register DAC dll with WerRegisterRuntimeExceptionModule");
        return FALSE;
#endif // FEATURE_CORESYSTEM
    }

    STRESS_LOG0(LF_STARTUP, 
                LL_INFO100, 
                "WATSON support: registered DAC dll with WerRegisterRuntimeExceptionModule");
    return TRUE;
}

//------------------------------------------------------------------------------
// Description
//------------------------------------------------------------------------------
// Description
HRESULT DwGetFileVersionInfo(
    __in_z LPCWSTR wszFilePath,
    USHORT& major,
    USHORT& minor,
    USHORT& build,
    USHORT& revision)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    major = minor = build = revision = 0;
    ULARGE_INTEGER appVersion = { 0, 0 };

    HRESULT result = GetFileVersion(wszFilePath, &appVersion);
    if (SUCCEEDED(result))
    {
        major = (appVersion.HighPart & 0xFFFF0000) >> 16;
        minor = appVersion.HighPart & 0x0000FFFF;
        build = (appVersion.LowPart & 0xFFFF0000) >> 16;
        revision = appVersion.LowPart & 0x0000FFFF;
    }

    return result;
}

//   Read the description from the resource section.
//
// Parameters
//   wszFilePath         Path to a file from which to extract the description
//   pBuf                [out] Put description here.
//   cchBuf              [in] Size of buf, wide chars.
//
// Returns
//   The number of characters stored.  Zero if error or no description.
//
// Exceptions
//   None
//------------------------------------------------------------------------------
int DwGetAppDescription(                // Number of characters written.
    __in_z LPCWSTR wszFilePath,          // Path to the executable.
    SString& pBuf // Put description here.
    )                     // Size of buf, wide chars.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD       dwHandle = 0;
    DWORD       bufSize = 0;                // Size of allocation for VersionInfo.
    DWORD       ret;

    // Find the buffer size for the version info structure we need to create
    EX_TRY
    {
        bufSize = GetFileVersionInfoSizeW(wszFilePath,  &dwHandle);
    }
    EX_CATCH
    {
        bufSize = 0;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!bufSize)
    {
        return 0;
    }

    // Allocate the buffer for the version info structure
    // _alloca() can't return NULL -- raises STATUS_STACK_OVERFLOW.
    BYTE* pVersionInfoBuffer = reinterpret_cast< BYTE* >(_alloca(bufSize));

    // Extract the version information blob. The version information
    // contains much more than the actual item of interest.
    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of GetFileVersionInfoW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = GetFileVersionInfoW(wszFilePath, dwHandle, bufSize, pVersionInfoBuffer);
    }

    if (!ret)
    {
        return 0;
    }

    // Extract the description.

    // Get the language and codepage for the version info.
    UINT size = 0;
    struct
    {
        WORD language;
        WORD codePage;
    }* translation;

    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of GetFileVersionInfoW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = VerQueryValueW(pVersionInfoBuffer, W("\\VarFileInfo\\Translation"),
                             reinterpret_cast< void **>(&translation), &size);
    }

    if (!ret || size == 0)
    {
        return 0;
    }

    // Build the query key for the language-specific file description resource.
    WCHAR buf[64];                 //----+----1----+----2----+----3----+----4----+
    _snwprintf_s(buf, NumItems(buf), _TRUNCATE, W("\\StringFileInfo\\%04x%04x\\FileDescription"),
               translation->language, translation->codePage);

    // Get the file description.
    WCHAR* fileDescription;
    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of GetFileVersionInfoW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = VerQueryValueW(pVersionInfoBuffer, buf,
                             reinterpret_cast< void** >(&fileDescription), &size);
    }

    // If the call failed, or there is no file description, done.
    if (!ret || size == 0)
    {
        return 0;
    }

    // If the description is a single space, ignore it.
    if (wcscmp(fileDescription, W(" ")) == 0)
    {
        return 0;
    }

    // Copy back the description.
    EX_TRY
    {
        wcsncpy_s(pBuf.OpenUnicodeBuffer(size), size, fileDescription, size);
        pBuf.CloseBuffer(size);
    }
        EX_CATCH
    {
        size = 0;
    }
    EX_END_CATCH(SwallowAllExceptions);
    

    return size;
} // int DwGetAppDescription()

//------------------------------------------------------------------------------
// Description
//   Extract the assembly version from an executable.
//
// Parameters
//   wszFilePath         Path to a file to exctract the version information from
//   pBuf                [out] Put version here.
//   cchBuf              Size of pBuf, in wide chars.
//
// Returns
//   Count of characters stored.
//
// Exceptions
//   None
//------------------------------------------------------------------------------
int DwGetAssemblyVersion(               // Number of characters written.
    __in_z LPCWSTR  wszFilePath,         // Path to the executable.
    __inout_ecount(cchBuf) WCHAR *pBuf, // Put description here.
    int cchBuf)                     // Size of buf, wide chars.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD       dwHandle = 0;
    DWORD       bufSize = 01;                // Size of allocation for VersionInfo.
    DWORD       ret;

    // Find the buffer size for the version info structure we need to create
    EX_TRY
    {
        bufSize = GetFileVersionInfoSizeW(wszFilePath,  &dwHandle);
    }
    EX_CATCH
    {
        bufSize = 0;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!bufSize)
    {
        return 0;
    }

    // Allocate the buffer for the version info structure
    // _alloca() can't return NULL -- raises STATUS_STACK_OVERFLOW.
    BYTE* pVersionInfoBuffer = reinterpret_cast< BYTE* >(_alloca(bufSize));

    // Extract the version information blob. The version information
    // contains much more than the actual item of interest.
    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of GetFileVersionInfoW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = GetFileVersionInfoW(wszFilePath, dwHandle, bufSize, pVersionInfoBuffer);
    }

    if (!ret)
    {
        return 0;
    }

    // Extract the description.

    // Get the language and codepage for the version info.
    UINT size = 0;
    struct
    {
        WORD language;
        WORD codePage;
    }* translation;

    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of VerQueryValueW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = VerQueryValueW(pVersionInfoBuffer, W("\\VarFileInfo\\Translation"),
                             reinterpret_cast< void **>(&translation), &size);
    }

    if (ret == 0 || size == 0)
    {
        return 0;
    }

    // Build the query key for the language-specific assembly version resource.
    WCHAR buf[64];                                                 //----+----1----+----2----+----3----+----4----+
    _snwprintf_s(buf, NumItems(buf), _TRUNCATE, W("\\StringFileInfo\\%04x%04x\\Assembly Version"),
               translation->language, translation->codePage);

    // Get the assembly version.
    WCHAR* assemblyVersion;
    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of VerQueryValueW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = VerQueryValueW(pVersionInfoBuffer, buf,
                             reinterpret_cast< void** >(&assemblyVersion), &size);
    }

    // If the call failed, or there is no assembly version, done.
    if (ret == 0 || size == 0)
    {
        return 0;
    }

    // If the assembly version is a single space, ignore it.
    if (wcscmp(assemblyVersion, W(" ")) == 0)
    {
        return 0;
    }

    // Copy back the assembly version.
    size = (int)size > cchBuf-1 ? cchBuf-1 : size;
    wcsncpy_s(pBuf, cchBuf, assemblyVersion, size);

    return size;
} // int DwGetAssemblyVersion()


//   Returns the IP of the instruction that caused the exception to occur.
//    For managed exceptions this may not match the Exceptions contained in
//    the exception record.
//
// Parameters
//   pExceptionRecord -- the SEH exception for the current exception
//   pThread           -- Pointer to Thread object of faulting thread
//
// Returns
//   The IP that caused the exception to occur
//
// Exceptions
//   None
//------------------------------------------------------------------------------
UINT_PTR GetIPOfThrowSite(
    EXCEPTION_RECORD*   pExceptionRecord,
    Thread              *pThread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If we can't determine a better value, use the exception record's exception address.
    UINT_PTR rslt = reinterpret_cast< UINT_PTR >(pExceptionRecord->ExceptionAddress);

    // If it is not a managed exception, use the IP from the exception.
    if (!IsComPlusException(pExceptionRecord))
        return rslt;

    // Get the thread object, from which we'll try to get the managed exception object.
    if (NULL == pThread)
    {   // If there's no managed thread, use the IP from the exception.
        return rslt;
    }

    // Retrieve any stack trace from the managed exception.  If there is a stack
    //  trace, it will start with the topmost (lowest address, newest) managed
    //  code, which is what we want.
    GCX_COOP();
    OBJECTREF throwable = pThread->GetThrowable();

    // If there was no managed code on the stack and we are on 64-bit, then we won't have propagated
    // the LastThrownObject into the Throwable yet.
    if (throwable == NULL)
        throwable = pThread->LastThrownObject();

    _ASSERTE(throwable != NULL);
    _ASSERTE(IsException(throwable->GetMethodTable()));

    // If the last thrown object is of type Exception, get the stack trace.
    if (throwable != NULL)
    {
        // Get the BYTE[] containing the stack trace.
        StackTraceArray traceData;
        ((EXCEPTIONREF)throwable)->GetStackTrace(traceData);

        GCPROTECT_BEGIN(traceData);
            // Grab the first non-zero, if there is one.
            for (size_t ix = 0; ix < traceData.Size(); ++ix)
            {
                if (traceData[ix].ip)
                {
                    rslt = traceData[ix].ip;
                    break;
                }
            }
        GCPROTECT_END();
    }

    return rslt;
} // UINT_PTR GetIPOfThrowSite()

//------------------------------------------------------------------------------
// Description
// Given a wchar string, returns true if any of the individual characters are unicode.
//  Else returns false (which implies the string could be losslessly converted to ascii)
//
// Input
//   wsz     -- The string to check.
//
// Returns
//   true    -- if the string contained any non-ascii characters,
//   false   -- otherwise.
//
//------------------------------------------------------------------------------
BOOL ContainsUnicodeChars(__in_z LPCWSTR wsz)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(wsz != NULL);

    while (NULL != *wsz)
    {
        if (!iswascii(*wsz))
        {
            return TRUE;
        }
        ++wsz;
    }
    return FALSE;
} // BOOL ContainsUnicodeChars()

//------------------------------------------------------------------------------
// Description
//   Builds the GenericMode bucket parameters for a managed Watson dump.
//
// Parameters
//   tore              -- type of error being reported
//   pThread           -- Pointer to Thread object of faulting thread
//   ip                -- Where the exception was thrown.
//   pGenericModeBlock -- Where to build the buckets
//   exception         -- the throwable
//
// Returns
//   S_OK if all there is a valid managed exception to report on and
//        Watson buckets were initialized successfully
//   S_FALSE if there is no managed exception to report on
//   E_OUTOFMEMORY if we ran out of memory while filling out the buckets
//
// Notes
//   (pGenericModeBlock->fInited == TRUE)  <=> (result = S_OK)
//   The original contract of this method required that both of these conditions
//   had to be checked independently and it has caused us some grief.
//   See Dev10 bug 833350.
//------------------------------------------------------------------------------
HRESULT GetManagedBucketParametersForIp(
    TypeOfReportedError tore,
    Thread *            pThread,
    UINT_PTR            ip,
    GenericModeBlock *  pGenericModeBlock,
    OBJECTREF *         pThrowable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Avoid confusion and stale data.
    memset(pGenericModeBlock, 0, sizeof(GenericModeBlock));

    // If the exception is not from managed code, then return S_FALSE.  There is
    // no more bucket data we can fill out.
    if (ip == 0)
    {
        LOG((LF_EH, LL_INFO1000, "GetManagedBucketParametersForIP: ip == 0, returning\n"));
        return S_FALSE;
    }

    PCODE currentPC = PCODE(ip);

    if (!ExecutionManager::IsManagedCode(currentPC))
    {
        // If there's no code manager for the location of the exception, then we
        // should just treat this exception like an unmanaged exception. We are
        // probably inside of mscorwks
        //
        // Note that while there may be an actual managed exception that
        // occurred, we can live without the managed bucket parameters. For
        // exceptions coming from within mscorwks.dll, the native bucket
        // parameters will do just fine.

        LOG((LF_EH, LL_INFO1000, "GetManagedBucketParametersForIP: IsManagedCode(%p) == FALSE, returning\n", currentPC));
        return S_FALSE;
    }

    WatsonBucketType bucketType = GetWatsonBucketType();

    {
        // if we default to CLR20r3 then let's assert that the bucketType is correct
        _ASSERTE(bucketType == CLR20r3);
        CLR20r3BucketParamsManager clr20r3Manager(pGenericModeBlock, tore, currentPC, pThread, pThrowable);
        clr20r3Manager.PopulateBucketParameters();
    }

    // At this point we have a valid managed exception, so the GMB should get
    // filled out.  If we set this to TRUE and there isn't a managed exception,
    // Watson will get confused and not report the full unmanaged data.
    pGenericModeBlock->fInited = TRUE;

    return S_OK;
} // HRESULT GetManagedBucketParametersForIp()

//------------------------------------------------------------------------------
// Description
//   Builds the GenericMode bucket parameters for a managed Watson dump.
//
// Parameters
//   ip -- the managed ip where the fault occurred.
//   tore -- the type of reportederror
//   pThread -- the thread point with the exception
//
// Returns
//   Allocated GenericModeBlock or null.
//
// Notes
//   This will attempt to allocate a new GenericModeBlock, and, if
//    successful, will fill it with the GenericMode parameters for
//    a managed Watson dump.  This is intended to be used in places where
//    the information is about to be lost.
//
//   This function is called from elsewhere in the runtime.
//------------------------------------------------------------------------------
void* GetBucketParametersForManagedException(UINT_PTR ip, TypeOfReportedError tore, Thread * pThread, OBJECTREF * pThrowable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!IsWatsonEnabled())
    {
        return NULL;
    }

    // Set up an empty GenericModeBlock to hold the bucket parameters.
    GenericModeBlock *pgmb = new (nothrow) GenericModeBlock;
    if (pgmb == NULL)
        return NULL;

    // Try to get BucketParameters.
    HRESULT hr = GetManagedBucketParametersForIp(tore, pThread, ip, pgmb, pThrowable);

    // If it didn't succeed, delete the GenericModeBlock. Note that hr could be S_FALSE, and that still
    // means the buckets aren't initialized.
    if (hr != S_OK)
    {
        delete pgmb;
        pgmb = NULL;
    }

    return pgmb;
} // void* GetBucketParametersForManagedException()

//------------------------------------------------------------------------------
// Description
//   Frees the GenericModeBlock allocated by GetBucketParametersForManagedException.
//
// Parameters
//   pgmb -- the allocated GenericModeBlock.
//
// Returns
//   nothing.
//------------------------------------------------------------------------------
void FreeBucketParametersForManagedException(void *pgmb)
{
    WRAPPER_NO_CONTRACT;

    if (!IsWatsonEnabled())
    {
        _ASSERTE(pgmb == NULL);
        return;
    }

    if (pgmb)
        delete pgmb;
} // void FreeBucketParametersForManagedException()


//------------------------------------------------------------------------------
// Description
//   Retrieves or builds the GenericMode bucket parameters for a managed
//    Watson dump.
//
// Parameters
//   pExceptionRecord  -- Information regarding the exception
//   pGenericModeBlock -- Where to build the buckets
//   tore              -- type of error being reported
//   pThread           -- Pointer to Thread object of faulting thread
//
// Returns
//   S_OK or error code.
//
// Notes
//   If there is a saved GenericModeBlock on the thread object's ExceptionState
//    that will be used.  Otherwise, a new block is created.
//------------------------------------------------------------------------------
HRESULT RetrieveManagedBucketParameters(
    EXCEPTION_RECORD    *pExceptionRecord,
    GenericModeBlock    *pGenericModeBlock,
    TypeOfReportedError tore,
    Thread              *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

#if defined(PRESERVE_WATSON_ACROSS_CONTEXTS)
    GenericModeBlock *pBuckets = NULL;

    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this.
    if (IsWatsonEnabled())
    {
        if (pThread != NULL)
        {
            // Try to get the buckets from the UE Watson Bucket Tracker
            pBuckets = reinterpret_cast<GenericModeBlock*>(pThread->GetExceptionState()->GetUEWatsonBucketTracker()->RetrieveWatsonBuckets());
            if ((pBuckets == NULL) && (pThread->GetExceptionState()->GetCurrentExceptionTracker() != NULL))
            {
                // If we didnt find the buckets in the UE Watson bucket tracker, then
                // try to look them up in the current exception's watson tracker if
                // an exception tracker exists.
                pBuckets = reinterpret_cast<GenericModeBlock*>(pThread->GetExceptionState()->GetCurrentExceptionTracker()->GetWatsonBucketTracker()->RetrieveWatsonBuckets());
            }
        }
    }

    // See if the thread has some managed bucket parameters stashed away...
    if (pBuckets != NULL)
    {   // Yes it does, so copy them to the output buffer.
        LOG((LF_EH, LL_INFO100, "Watson: RetrieveManagedBucketParameters returning stashed parameters (%p)\n", pBuckets));
        *pGenericModeBlock = *pBuckets;

#if defined(_DEBUG)
        LOG((LF_EH, LL_INFO100, "Watson b 1: %S\n", pGenericModeBlock->wzP1));
        LOG((LF_EH, LL_INFO100, "       b 2: %S\n", pGenericModeBlock->wzP2));
        LOG((LF_EH, LL_INFO100, "       b 3: %S\n", pGenericModeBlock->wzP3));
        LOG((LF_EH, LL_INFO100, "       b 4: %S\n", pGenericModeBlock->wzP4));
        LOG((LF_EH, LL_INFO100, "       b 5: %S\n", pGenericModeBlock->wzP5));
        LOG((LF_EH, LL_INFO100, "       b 6: %S\n", pGenericModeBlock->wzP6));
        LOG((LF_EH, LL_INFO100, "       b 7: %S\n", pGenericModeBlock->wzP7));
        LOG((LF_EH, LL_INFO100, "       b 8: %S\n", pGenericModeBlock->wzP8));
        LOG((LF_EH, LL_INFO100, "       b 9: %S\n", pGenericModeBlock->wzP9));
#endif
    }
    else
#endif
    {   // No stashed bucket parameters, so get them from the exception.
        UINT_PTR ip = 0;
        if (pExceptionRecord != NULL)
        {
            // This function is called from functions that have NOTHROW/GC_NOTRIGGER
            // contracts (in particular EEPolicy::HandleFatalError). Because that
            // function always passes pExceptionInfo as NULL, we will never actually
            // reach the potentially throwing code.
            //
            CONTRACT_VIOLATION(ThrowsViolation | GCViolation);

            ip = GetIPOfThrowSite(pExceptionRecord, pThread);
        }

        hr = GetManagedBucketParametersForIp(tore, pThread, ip, pGenericModeBlock, NULL);
    }

    return hr;
} // HRESULT RetrieveManagedBucketParameters()

//------------------------------------------------------------------------------
// Description
//  Helper to get Watson bucket parameters, for the DebugManager interface.
//
// Parameters
//  pParams -- Fill the parameters here.
//
// Returns
//  S_OK    -- Parameters filled in.
//  S_FALSE -- No current exception.
//  error   -- Some error occurred.
//
// Note:
//  This function is exposed via the hosting interface.
//------------------------------------------------------------------------------
HRESULT GetBucketParametersForCurrentException(
    BucketParameters *pParams)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr;
    GenericModeBlock gmb;

    // Make sure this is (or at least has been) a managed thread.
    Thread *pThread = GetThread();
    if (pThread == NULL)
    {   // Not the greatest error, but we don't expect to be called on a unmanaged thread.
        return E_UNEXPECTED;
    }

    if (!IsWatsonEnabled())
    {
        return E_NOTIMPL;
    }

    // And make sure there is a current exception.
    ThreadExceptionState* pExState = pThread->GetExceptionState();
    if (!pExState->IsExceptionInProgress())
        return S_FALSE;

    // Make sure we're not in the second pass.
    if (pExState->GetFlags()->UnwindHasStarted())
    {   // unwind indicates the second pass, so quit
        return S_FALSE;
    }

    EXCEPTION_RECORD *pExceptionRecord = pExState->GetExceptionRecord();

    // Try to get the parameters...
    hr = RetrieveManagedBucketParameters(pExceptionRecord, &gmb, TypeOfReportedError::UnhandledException, pThread);

    // ... and if successful, copy to the output block.  If the return value is
    // S_FALSE then it wasn't a managed exception and we should not copy the data in
    // S_OK is the only success value that has inited the data
    if (hr == S_OK)
    {
        // Event type name.
        wcsncpy_s(pParams->pszEventTypeName, COUNTOF(pParams->pszEventTypeName), gmb.wzEventTypeName, _TRUNCATE);

        // Buckets.  Mind the 1-based vs 0-based.
        wcsncpy_s(pParams->pszParams[0], COUNTOF(pParams->pszParams[0]), gmb.wzP1, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[1], COUNTOF(pParams->pszParams[1]), gmb.wzP2, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[2], COUNTOF(pParams->pszParams[2]), gmb.wzP3, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[3], COUNTOF(pParams->pszParams[3]), gmb.wzP4, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[4], COUNTOF(pParams->pszParams[4]), gmb.wzP5, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[5], COUNTOF(pParams->pszParams[5]), gmb.wzP6, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[6], COUNTOF(pParams->pszParams[6]), gmb.wzP7, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[7], COUNTOF(pParams->pszParams[7]), gmb.wzP8, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[8], COUNTOF(pParams->pszParams[8]), gmb.wzP9, _TRUNCATE);
        wcsncpy_s(pParams->pszParams[9], COUNTOF(pParams->pszParams[9]), gmb.wzP10, _TRUNCATE);

        // All good.
        pParams->fInited = TRUE;
    }

    return hr;
} // HRESULT GetBucketParametersForCurrentException()


class WatsonThreadData {
 public:

    WatsonThreadData(EXCEPTION_POINTERS *pExc, TypeOfReportedError t, Thread* pThr, DWORD dwID, FaultReportResult res) 
       : pExceptionInfo(pExc)
       , tore(t)
       , pThread(pThr)
       , dwThreadID(dwID)
       , result(res)
    {
    }
        
    EXCEPTION_POINTERS  *pExceptionInfo; // Information about the exception, NULL if the error is not caused by an exception
    TypeOfReportedError tore;            // Information about the fault
    Thread*             pThread;         // Thread object for faulting thread, could be NULL
    DWORD               dwThreadID;      // OS Thread ID for faulting thread
    FaultReportResult   result;          // Result of invoking Watson
};



DWORD WINAPI ResetWatsonBucketsCallbackForStackOverflow(LPVOID pParam)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(IsWatsonEnabled());
        PRECONDITION(pParam != NULL);
    }
    CONTRACTL_END;

    // ThreadStore lock could be already taken (SO during GC) so we skip creating a managed thread and get a hardcoded exception name.
    // If we wanted to get the exception name from OBJECTREF we would have to switch to GC_COOP mode and be on a managed thread.

    ResetWatsonBucketsParams * pRWBP = reinterpret_cast<ResetWatsonBucketsParams *>(pParam);
    Thread * pThread = pRWBP->m_pThread;
    PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pThread->GetExceptionState()->GetUEWatsonBucketTracker();
    _ASSERTE(pUEWatsonBucketTracker != NULL);

    UINT_PTR ip = reinterpret_cast<UINT_PTR>(pRWBP->pExceptionRecord->ExceptionAddress);
    pUEWatsonBucketTracker->SaveIpForWatsonBucket(ip);
    pUEWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::StackOverflowException, pThread, NULL);
    if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
    {
        pUEWatsonBucketTracker->ClearWatsonBucketDetails();
    }

    return 0;
}

//------------------------------------------------------------------------------
// Description
//      This function is called by the Debugger thread in response to a favor
//      posted to it by the faulting thread. The faulting thread uses the 
//      Debugger thread to reset Watson buckets in the case of stack overflows.
//      Since the debugger thread doesn't have a managed Thread object,
//      it cannot be directly used to call ResetWatsonBucketsFavorWorker. 
//      Instead, this function spawns a worker thread and waits for it to complete.
//
// Parameters
//      pParam -- A pointer to a ResetWatsonBucketsParams instance
//
// Exceptions
//      None.
//------------------------------------------------------------------------------
void ResetWatsonBucketsFavorWorker(void * pParam)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(IsWatsonEnabled());
        PRECONDITION(pParam != NULL);
    }
    CONTRACTL_END;

    HANDLE hThread = NULL;
    DWORD dwThreadId;

    hThread = ::CreateThread(NULL, 0, ResetWatsonBucketsCallbackForStackOverflow, pParam, 0, &dwThreadId);
    if (hThread != NULL)
    {
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
    }

    return;
}


//----------------------------------------------------------------------------
// CreateThread() callback to invoke native Watson or put up our fake Watson 
// dialog depending on m_fDoReportFault value.
//
// The output is a FaultReport* value communicated by setting
// pFaultReportInfo->m_result. The DWORD function return value
// is unused.
//----------------------------------------------------------------------------
static DWORD WINAPI DoFaultReportCreateThreadCallback(LPVOID pFaultReportInfoAsVoid)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;


    // We are allowed to ignore OOM's here as FaultReport() is merely a notification of
    // an unhandled exception. If we can't do the report, that's just too bad.
    FAULT_NOT_FATAL();

    LOG((LF_EH, LL_INFO100, "DoFaultReport: at sp %p ...\n", GetCurrentSP()));

    FaultReportInfo *pFaultReportInfo = (FaultReportInfo*)pFaultReportInfoAsVoid;
    EXCEPTION_POINTERS *pExceptionInfo = pFaultReportInfo->m_pExceptionInfo;

    if (pFaultReportInfo->m_fDoReportFault)
    {
        pFaultReportInfo->m_faultRepRetValResult = DoReportFault(pExceptionInfo);
    }
    else
    {
        int res = EEMessageBoxCatastrophicWithCustomizedStyle(
                               IDS_DEBUG_UNHANDLEDEXCEPTION,
                               IDS_DEBUG_SERVICE_CAPTION,
                               MB_OKCANCEL | MB_ICONEXCLAMATION,
                               TRUE,
                               GetCurrentProcessId(),
                               GetCurrentProcessId(),
                               pFaultReportInfo->m_threadid,
                               pFaultReportInfo->m_threadid
                              );
        if (res == IDOK)
        {
            pFaultReportInfo->m_faultReportResult = FaultReportResultQuit;
        }
        else
        {
            pFaultReportInfo->m_faultReportResult = FaultReportResultDebug;
        }
    }

    return 0;
}


//----------------------------------------------------------------------------
// Favor callback for the debugger thread.
//----------------------------------------------------------------------------
VOID WINAPI DoFaultReportDoFavorCallback(LPVOID pFaultReportInfoAsVoid)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;


    // Since the debugger thread doesn't allow ordinary New's which our stuff
    // indirectly calls, it cannot be directly used to call DoFaultReport. Instead, this function
    // spawns a worker thread and waits for it to complete.
    
    HANDLE hThread = NULL;
    DWORD dwThreadId;

    hThread = ::CreateThread(NULL, 0, DoFaultReportCreateThreadCallback, pFaultReportInfoAsVoid, 0, &dwThreadId);
    if (hThread != NULL)
    {
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
    }
}

// look at the type of the contract failure.  if it's a precondition then we want to blame the caller
// of the method that originated the ContractException not just the first non-contract runtime frame.
// if this isn't a ContractException then we default to Invariant which won't skip the extra frame.
ContractFailureKind GetContractFailureKind(OBJECTREF obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(obj != NULL);
    }
    CONTRACTL_END;

    PTR_MethodTable pMT = obj->GetMethodTable();

    if (MscorlibBinder::IsException(pMT, kContractException))
        return CONTRACTEXCEPTIONREF(obj)->GetContractFailureKind();

    // there are cases where the code contracts rewriter will use a ContractException
    // type that's compiled into the user's assembly.  if we get here then this is
    // one of those cases.  we will make a best guess if this is a ContractException
    // so that we can return the value in the _Kind field.

    // NOTE: this really isn't meant to be a general-purpose solution for identifying ContractException types.
    // we're making a few assumptions here since we're being called in context of WER bucket parameter generation.

    // just return anything that isn't precondition so that an extra frame won't be skipped.
    ContractFailureKind result = CONTRACT_FAILURE_INVARIANT;

    // first compare the exception name.
    PTR_MethodTable pContractExceptionMT = MscorlibBinder::GetClassIfExist(CLASS__CONTRACTEXCEPTION);
    _ASSERTE(pContractExceptionMT);

    if (pContractExceptionMT)
    {
        LPCUTF8 contractExceptionNamespace = NULL;
        LPCUTF8 contractExceptionName = pContractExceptionMT->GetFullyQualifiedNameInfo(&contractExceptionNamespace);
        _ASSERTE(contractExceptionName);

        LPCUTF8 incomingExceptionNamespace = NULL;
        LPCUTF8 incomingExceptionName = pMT->GetFullyQualifiedNameInfo(&incomingExceptionNamespace);
        _ASSERTE(incomingExceptionName);

        // NOTE: we can't compare the namespaces since sometimes it comes back as an empty string
        if (contractExceptionName && incomingExceptionName && strcmp(incomingExceptionName, contractExceptionName) == 0)
        {
            WORD requiredNumFields = pContractExceptionMT->GetNumInstanceFields();
            WORD numFields = pMT->GetNumInstanceFields();

            // now see if this exception object has the required number of fields
            if (numFields == requiredNumFields)
            {
                // getting closer, now look for all three fields on ContractException
                const int requiredFieldMatches = 3;

                PTR_EEClass pEEClass = pMT->GetClass_NoLogging();

                PTR_FieldDesc pFD = pEEClass->GetFieldDescList();
                PTR_FieldDesc pFDEnd = pFD + numFields;
                PTR_FieldDesc pKindFD = NULL;

                int numMatchedFields = 0;
                while ((pFD < pFDEnd) && (numMatchedFields != requiredFieldMatches))
                {
                    CorElementType fieldType = pFD->GetFieldType();
                    if (fieldType == ELEMENT_TYPE_I4)
                    {
                        // found the _Kind field
                        LPCUTF8 name = NULL;
                        HRESULT hr = pFD->GetName_NoThrow(&name);
                        if (SUCCEEDED(hr) && name && (strcmp(name, "_Kind") == 0))
                        {
                            // found the _Kind field, remember this FieldDesc in case we have a match
                            pKindFD = pFD;
                            ++numMatchedFields;
                        }
                    }
                    else if (fieldType == ELEMENT_TYPE_CLASS)
                    {
                        LPCUTF8 name = NULL;
                        HRESULT hr = pFD->GetName_NoThrow(&name);
                        if (SUCCEEDED(hr) && name && ((strcmp(name, "_UserMessage") == 0) || (strcmp(name, "_Condition") == 0)))
                        {
                            // found another matching field
                            ++numMatchedFields;
                        }
                    }

                    ++pFD;
                }

                if (numMatchedFields == requiredFieldMatches)
                {
                    _ASSERTE(pKindFD != NULL);
                    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();
                    pKindFD->GetInstanceField(obj, reinterpret_cast<void*>(&result));
                }
            }
        }
    }

    return result;
}
