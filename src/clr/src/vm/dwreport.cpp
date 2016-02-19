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

#include "imagehlp.h"

#ifdef FEATURE_UEF_CHAINMANAGER
// This is required to register our UEF callback with the UEF chain manager
#include <mscoruefwrapper.h>
#endif // FEATURE_UEF_CHAINMANAGER

EFaultRepRetVal DoReportFault(EXCEPTION_POINTERS * pExceptionInfo);

// Should the CLR use Watson to report fatal errors and unhandled exceptions?
static BOOL g_watsonErrorReportingEnabled = FALSE;

// Variables to control launching Watson only once, but making all threads wait for that single launch to finish.
LONG g_watsonAlreadyLaunched = 0; // Used to note that another thread has done Watson.

#if !defined(FEATURE_UEF_CHAINMANAGER)
HandleHolder g_hWatsonCompletionEvent = NULL; // Used to signal that Watson has finished.
#endif // FEATURE_UEF_CHAINMANAGER

const WCHAR kErrorReportingPoliciesKey[] = W("SOFTWARE\\Policies\\Microsoft\\PCHealth\\ErrorReporting");
const WCHAR kErrorReportingKey[] = W("SOFTWARE\\Microsoft\\PCHealth\\ErrorReporting");

const WCHAR kShowUIValue[] = W("ShowUI");
const WCHAR kForceQueueModeValue[] = W("ForceQueueMode");
const WCHAR kDoReportValue[] = W("DoReport");
const WCHAR kAllOrNoneValue[] = W("AllOrNone");
const WCHAR kIncludeMSAppsValue[] = W("IncludeMicrosoftApps");
const WCHAR kIncludeWindowsAppsValue[] = W("IncludeWindowsApps");
const WCHAR kExclusionListKey[] = W("SOFTWARE\\Microsoft\\PCHealth\\ErrorReporting\\ExclusionList");
const WCHAR kInclusionListKey[] = W("SOFTWARE\\Microsoft\\PCHealth\\ErrorReporting\\InclusionList");
const WCHAR kExclusionListSubKey[] = W("\\ExclusionList");
const WCHAR kInclusionListSubKey[] = W("\\InclusionList");


// Default values for various registry keys
const DWORD kDefaultShowUIValue = 1;
const DWORD kDefaultForceQueueModeValue = 0;
const DWORD kDefaultDoReportValue = 1;
const DWORD kDefaultAllOrNoneValue = 1;
const DWORD kDefaultExclusionValue = 0;
const DWORD kDefaultInclusionValue = 0;
const DWORD kDefaultIncludeMSAppsValue = 1;
const DWORD kDefaultIncludeWindowsAppsValue = 1;

// Default value for the default debugger and auto debugger attach settings.
const BOOL  kDefaultDebuggerIsWatson = FALSE;
const BOOL  kDefaultAutoValue = FALSE;

// When debugging the watson process itself, the faulting process will spin
// waiting for Watson to signal various events. If these waits time out, the
// faulting process will go ahead and exit, which is sub-optimal if you need to
// inspect the faulting process with the debugger at the same time. In debug
// builds, use a longer wait time, since watson may be stopped under the
// debugger for a while.

#ifdef _DEBUG
const DWORD kDwWaitTime = DW_TIMEOUT_VALUE * 1000;
#else
const DWORD kDwWaitTime = DW_TIMEOUT_VALUE;
#endif

#ifdef _TARGET_X86_
    const DWORD kWatsonRegKeyOptions = 0;
#else
    const DWORD kWatsonRegKeyOptions = KEY_WOW64_32KEY;
#endif

const WCHAR kWatsonPath[] = WATSON_INSTALLED_REG_SUBPATH;
#if defined(_TARGET_X86_)
const WCHAR kWatsonValue[] = WATSON_INSTALLED_REG_VAL;
#else
const WCHAR kWatsonValue[] = WATSON_INSTALLED_REG_VAL_IA64;
#endif
const WCHAR* kWatsonImageNameOnLonghorn = W("\\dw20.exe");

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
    return g_watsonErrorReportingEnabled;
}

//------------------------------------------------------------------------------
// Description
//  Initializes watson global critsec and event.  Records whether run via
//   managed .exe.
//
// Parameters
//  fFlags -- the COINITIEE flags used to start the runtime.
//
// Returns
//  TRUE -- always
//------------------------------------------------------------------------------
BOOL InitializeWatson(COINITIEE fFlags)
{
    LIMITED_METHOD_CONTRACT;

    // Watson is enabled for all SKUs
    g_watsonErrorReportingEnabled = TRUE; 

    LOG((LF_EH, LL_INFO10, "InitializeWatson: %s\n", g_watsonErrorReportingEnabled ? "enabled" : "disabled"));

    if (!IsWatsonEnabled())
    {
        return TRUE;
    }

#if defined(FEATURE_UEF_CHAINMANAGER)
    return TRUE;
#else
    // Create the event that all-but-the-first threads will wait on (the first thread
    // will set the event when Watson is done.)
    g_hWatsonCompletionEvent = WszCreateEvent(NULL, TRUE /*manual reset*/, FALSE /*initial state*/, NULL);
    return (g_hWatsonCompletionEvent != NULL);
#endif // FEATURE_UEF_CHAINMANAGER

} // BOOL InitializeWatson()

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
        PRECONDITION(RunningOnWin7());
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
// CreateWatsonSharedMemory
//
// Description
//
// Creates a shared memory block for communication with Watson
//
// Parameters
//      hWatsonSharedMemory -- [out] The handle to the watson shared memory.
//      ppWatsonSharedMemory -- [out] A pointer to the Watson shared memory.
// Returns
//      S_OK -- if the function complete normally.
//      FALSE -- otherwise
// Exceptions
//      None
//------------------------------------------------------------------------------
HRESULT CreateWatsonSharedMemory(HANDLE* hWatsonSharedMemory,
                                 DWSharedMem** ppWatsonSharedMemory);

//------------------------------------------------------------------------------
// Description
//      Alerts the host that the thread is leaving the runtime, and sleeps
//      waiting for an object to be signalled
//
// Parameters
//      handle -- the handle to wait on
//      timeout -- the length of time to wait
//
// Returns
//      DWORD -- The return value from WaitForSingleObject
//
// Exceptions
//      None
//
// Notes
//   winwrap.h prevents us from using SetEvent by including
//      #define SetEvent Dont_Use_SetEvent
//   This is because using SetEvent within the runtime will result in poor
//   interaction with any sort of host process (e.g. SQL).  We can use the
//   SetEvent/WaitForSingleObject primitives as long as we do some other work to
//   make sure the host understands.
//------------------------------------------------------------------------------
#undef SetEvent
DWORD ClrWaitForSingleObject(HANDLE handle, DWORD timeout)
{
     CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    CONTRACT_VIOLATION(ThrowsViolation);

    LeaveRuntimeHolder holder(reinterpret_cast< size_t >(WaitForSingleObject));
    return WaitForSingleObject(handle, timeout);
} // DWORD ClrWaitForSingleObject()

//------------------------------------------------------------------------------
// Helper class to set an event in destructor -- allows setting an event on the
//  way out of a function.
//
// Used to synchronize multiple threads with unhandled exceptions -- only the
//  first will run Watson, and all the rest will wait on the first one to be
//  done.
//------------------------------------------------------------------------------
class SettingEventHolder
{
public:
    SettingEventHolder(HANDLE &event) : m_event(event), m_bSetIt(FALSE) { LIMITED_METHOD_CONTRACT; }
    ~SettingEventHolder() { LIMITED_METHOD_CONTRACT; if (m_bSetIt && m_event) SetEvent(m_event); }
    void EnableSetting() { LIMITED_METHOD_CONTRACT; m_bSetIt = TRUE; }
    DWORD DoWait(DWORD timeout=INFINITE_TIMEOUT) { WRAPPER_NO_CONTRACT; return m_event ? ClrWaitForSingleObject(m_event, timeout) : 0; }

private:
    HANDLE  m_event;            // The event to set
    BOOL    m_bSetIt;           // If true, set event in destructor.
};

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

enum MicrosoftAppTypes
{
    MicrosoftAppTypesNone = 0,
    MicrosoftAppTypesWindows = 0x1,
    MicrosoftAppTypesOther = 0x2
};

inline void SetMSFTApp(DWORD &AppType)        { LIMITED_METHOD_CONTRACT; AppType |= MicrosoftAppTypesOther; }
inline void SetMSFTWindowsApp(DWORD &AppType) { LIMITED_METHOD_CONTRACT; AppType |= MicrosoftAppTypesWindows; }

inline BOOL IsMSFTApp(DWORD AppType)          { LIMITED_METHOD_CONTRACT; return (AppType & MicrosoftAppTypesOther) ? TRUE : FALSE; }
inline BOOL IsMSFTWindowsApp(DWORD AppType)   { LIMITED_METHOD_CONTRACT; return (AppType & MicrosoftAppTypesWindows) ? TRUE : FALSE; }


//------------------------------------------------------------------------------
// Description
//   Determine if the application is a Microsoft application.
//
// Parameters
//   wszFilePath         Path to a file to exctract the information from
//   pAppTypes           [out] Put MicrosoftAppTypes here.
//
// Returns
//   S_OK                If the function succeede
//   E_XXXX              Failure result.
//
// Exceptions
//   None
//------------------------------------------------------------------------------
HRESULT DwCheckCompany(                 // S_OK or error.
    __in_z LPWSTR wszFilePath,          // Path to the executable.
    DWORD* pAppTypes)                   // Non-microsoft, microsoft, microsoft windows.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    //
    // Note that this code is equivalent to FusionGetFileVersionInfo, found in fusion\asmcache\asmcache.cpp
    //

    HRESULT     hr = S_OK;              // result of some operation
    DWORD       dwHandle = 0;
    DWORD       bufSize = 0;            // Size of allocation for VersionInfo.
    DWORD       ret;

    // Avoid confusion
    *pAppTypes = MicrosoftAppTypesNone;

    // Find the buffer size for the version info structure we need to create
    EX_TRY
    {
        bufSize = GetFileVersionInfoSizeW(wszFilePath,  &dwHandle);
        if (!bufSize)
        {
            hr = HRESULT_FROM_GetLastErrorNA();
        }
    }
    EX_CATCH
    {
        hr = E_OUTOFMEMORY;
    }
    EX_END_CATCH(SwallowAllExceptions);
    if (!bufSize)
    {
        return hr;
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

        if (!ret)
        {
            return HRESULT_FROM_GetLastErrorNA();
        }
    }

    // Extract the actual CompanyName and compare it to "Microsoft" and
    // "MicrosoftWindows"

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

        if (!ret || size == 0)
        {
            return HRESULT_FROM_GetLastErrorNA();
        }
    }

    // Build the query key for the language-specific company name resource.
    WCHAR buf[64];                                                 //----+----1----+----2----+----3----+----4
    _snwprintf_s(buf, NumItems(buf), _TRUNCATE, W("\\StringFileInfo\\%04x%04x\\CompanyName"),
               translation->language, translation->codePage);

    // Get the company name.
    WCHAR *name;
    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of GetFileVersionInfoW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = VerQueryValueW(pVersionInfoBuffer, buf,
                             reinterpret_cast< void** >(&name), &size);
    }

    // If there is company name info, check it.
    if (ret != 0 && size != 0 && wcsstr(name, W("Microsoft")))
    {
        SetMSFTApp(*pAppTypes);
    }


    // Now build the query key for the language-specific product name resource.
    _snwprintf_s(buf, NumItems(buf), _TRUNCATE, W("\\StringFileInfo\\%04x%04x\\ProductName"),
               translation->language, translation->codePage);

    // Get the product name.
    {
        // If the previoud GetFileVersionInfoSizeW succeeds, version.dll has been loaded
        // in the process, and delay load of GetFileVersionInfoW will not throw.
        CONTRACT_VIOLATION(ThrowsViolation);
        ret = VerQueryValueW(pVersionInfoBuffer, buf,
                             reinterpret_cast< void** >(&name), &size);
    }

    // If there is product name info, check it.
    if (ret != 0 && size != 0 && wcsstr(name, W("Microsoft\x0ae Windows\x0ae")))
    {
        SetMSFTWindowsApp(*pAppTypes);
    }

    return S_OK;

} // HRESULT DwCheckCompany()


//------------------------------------------------------------------------------
// Description
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



//------------------------------------------------------------------------------
// CLRWatsonHelper class
//
// Certain registry keys affect the behavior of watson. In particulary, they
// control
//   o  whether or not a Watson report should result in UI popups
//   o  which debugger should be used to JIT attach to the faulting process
//   o  whether error reports should be sent at all.
// This class is a holder for static functions that access these registry keys
// to determine the proper settings.
//
//------------------------------------------------------------------------------
class CLRWatsonHelper
{
public:
    enum WHDebugAction
    {
        WHDebug_InvalidValue,
        WHDebug_AutoLaunch,
        WHDebug_AskToLaunch,
        WHDebug_DontLaunch
    } m_debugAction;

    enum WHReportAction
    {
        WHReport_InvalidValue,
        WHReport_AutoQueue,
        WHReport_AskToSend,
        WHReport_DontSend
    } m_reportAction;

    enum WHDialogAction
    {
        WHDialog_InvalidValue,
        WHDialog_OkToPopup,
        WHDialog_DontPopup
    } m_dialogAction;

    CLRWatsonHelper()
      : m_debugAction(WHDebug_InvalidValue),
        m_reportAction(WHReport_InvalidValue),
        m_dialogAction(WHDialog_InvalidValue)
    { LIMITED_METHOD_CONTRACT; }

    void Init(BOOL bIsManagedFault, TypeOfReportedError tore);

    // Does the current interactive USER have sufficient permissions to
    //  launch Watson or a debugger against this PROCESS?
    BOOL CurrentUserHasSufficientPermissions();

    // Should a debugger automatically, or should the user be queried for a debugger?
    BOOL ShouldDebug();

    // Should a managed debugger be launched, without even asking?
    BOOL ShouldAutoAttach();

    // Should Watson include a "Debug" button?
    BOOL ShouldOfferDebug();

    // Should a Watson report be generated?
    BOOL ShouldReport();

    // Should there be a popup?  Possibly with only "quit"?
    BOOL ShouldShowUI();

    // If a Watson report is generated, should it be auto-queued?
    //  (vs asking the user what to do about it)
    BOOL ShouldQueueReport();

private:
    // Looks in HKCU/Software/Policies/Microsoft/PCHealth/ErrorReporting
    //  then in HKLM/  "        "       "           "       "
    //  then in HKCU/SOftware/Microsoft/PCHealth/ErrorReporting
    //  then in HKLM/  "        "       "           "
    static int GetPCHealthConfigLong(      // Return value from registry or default.
        LPCWSTR     szName,                 // Name of value to get.
        int        iDefault);              // Default value to return if not found.

    // Like above, but searches for a subkey with the given value.
    static BOOL GetPCHealthConfigSubKeyLong(// Return value from registry or default.
        LPCWSTR     szSubKey,               // Name of the subkey.
        LPCWSTR     szName,                 // Name of value to get.
        int        iDefault,               // Default value to return if not found.
        DWORD       *pValue);               // Put value here.

    void AssertValid()
    {   
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_debugAction != WHDebug_InvalidValue);
        _ASSERTE(m_reportAction != WHReport_InvalidValue);
        _ASSERTE(m_dialogAction != WHDialog_InvalidValue);
    }

}; // class CLRWatsonHelper

//------------------------------------------------------------------------------
// Description
//   Initialization for watson helper class.
//
// Parameters
//   bIsManagedFault    - true if EXCEPTION_COMPLUS or fault from jitted code.
//                      - false otherwise
//
//
// Notes:
//   - Launches and Pops always happen to the same session in which the
//     process is running.
//   - This function computes what actions should happen, but doesn't do any.
//
//  This routine returns which actions should be taken given the current registry
//   settings and environment.  It implements the following matrix:
//
//                              <<-- AutoLaunch -->>
//                                TRUE       FALSE
//    Interactive process          A3         B2 
//    Non-interactive process      A3         C1
//
//    Action codes:
//     A - Auto attach debugger
//     B - Ask to attach debugger
//     C - Don't attach debugger
//
//     1 - Auto Queue Watson report
//     2 - Ask to Send Watson report
//     3 - Don't send Watson report
//
//
// CLRWatsonHelper::Init
//------------------------------------------------------------------------------
void CLRWatsonHelper::Init(
    BOOL        bIsManagedFault,            // Is the fault in question from managed code?
    TypeOfReportedError tore)               // What sort of error is this?
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Initialize returned values
    WHDebugAction  tmpDebugAction  = WHDebug_InvalidValue;
    WHReportAction tmpReportAction = WHReport_InvalidValue;
    WHDialogAction tmpDialogAction = WHDialog_InvalidValue;

    // First run the matrix, then later provide the over-rides
    BOOL fRunningInteractive = RunningInteractive();

    if (fRunningInteractive)
    {
        // Interactive services and interactive apps running as LocalSystem are considered non-interactive
        // so that we don't display any UI for them. Note that we should check the process token (and not the
        // thread token if the thread is impersonating a user) to determine if the app is running as LocalSystem.
        // This is because Watson displays UI for us and Watson is run by calling CreateProcess. CreateProcess
        // always creates child processes using the process token.

        BOOL fLocalSystemOrService;
        if (RunningAsLocalSystemOrService(fLocalSystemOrService) != ERROR_SUCCESS)
        {
            // Err on the side of caution; treat the app as non-interactive
            fRunningInteractive = FALSE;
        }
        else if (fLocalSystemOrService)
        {
            fRunningInteractive = FALSE;
        }
    }

    BOOL bAutoLaunch = FALSE;
    SString ssDummy;

    GetDebuggerSettingInfo(ssDummy, &bAutoLaunch);

    if (bAutoLaunch)
    {
        tmpDebugAction  = WHDebug_AutoLaunch;
        tmpReportAction = WHReport_DontSend;
        tmpDialogAction = WHDialog_DontPopup;
    }
    else
    {
        if (fRunningInteractive)
        {
            tmpDebugAction  = WHDebug_AskToLaunch;
            tmpReportAction = WHReport_AskToSend;
            tmpDialogAction = WHDialog_OkToPopup;
        }
        else
        {   
            // Non-interactive process
            tmpDebugAction  = WHDebug_DontLaunch;
            tmpReportAction = WHReport_AutoQueue;
            tmpDialogAction = WHDialog_DontPopup;
        }
    }

    // If this is a breakpoint, never send a report.
    if (tore.IsBreakpoint())
        tmpReportAction = WHReport_DontSend;

    // Store off the results.
    m_debugAction = tmpDebugAction;
    m_reportAction = tmpReportAction;
    m_dialogAction = tmpDialogAction;

    // Done.  Log some stuff in debug mode.
    #if defined(_DEBUG)
    {
    char *(rda[]) = {"InvalidValue", "AutoDebug", "AskToDebug", "DontDebug"};
    char *(rwa[]) = {"InvalidValue", "AutoQueue", "AskToSend", "DontSend"};
    char *(rdlga[]) = {"InvalidValue", "OkToPopup", "DontPopup"};
    LOG((LF_EH, LL_INFO100, "CLR Watson: debug action: %s\n", rda[m_debugAction]));
    LOG((LF_EH, LL_INFO100, "CLR Watson: report action: %s\n", rwa[m_reportAction]));
    LOG((LF_EH, LL_INFO100, "CLR Watson: dialog action: %s\n", rdlga[m_dialogAction]));
    #define LB(expr) LOG((LF_EH, LL_INFO100, "CLR Watson: " #expr ": %s\n", ((expr) ? "true" : "false") ))
    LB(CurrentUserHasSufficientPermissions());
    LB(ShouldDebug());
    LB(ShouldAutoAttach());
    LB(ShouldOfferDebug());
    LB(ShouldReport());
    LB(ShouldQueueReport());
    #undef LB
    }
    #endif

} // void CLRWatsonHelper::Init()


//------------------------------------------------------------------------------
// CurrentUserHasSufficientPermissions
//
// Determines if the user logged in has the correct permissions to launch Watson.
//
// Parameters:
//   None.
//
// Returns:
//   TRUE if the user has sufficient permissions, else FALSE
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::CurrentUserHasSufficientPermissions()
{
    // TODO! Implement!
    return TRUE;
} // BOOL CLRWatsonHelper::CurrentUserHasSufficientPermissions()



//------------------------------------------------------------------------------
// Description
//   Determines whether we will show Watson at all.
//
// Parameters
//   none
//
// Returns
//      TRUE -- If Watson should show UI.
//      FALSE -- Otherwise
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::ShouldShowUI()
{
    WRAPPER_NO_CONTRACT;

    AssertValid();

    return (m_dialogAction == WHDialog_OkToPopup);
} // BOOL CLRWatsonHelper::ShouldShowUI()

//------------------------------------------------------------------------------
// Description
//   Determines whether a debugger will (or may be) launched.  True if there
//    is an auto-launch debugger, or if we will ask the user.
//
// Parameters
//   none
//
// Returns
//      TRUE -- If a debugger might be attached.
//      FALSE -- Otherwise
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::ShouldDebug()
{
    LIMITED_METHOD_CONTRACT;

    return ShouldOfferDebug() || ShouldAutoAttach();
} // BOOL CLRWatsonHelper::ShouldDebug()

//------------------------------------------------------------------------------
// Description
//   Determines whether or not the Debug button should be present in the
//   Watson dialog
//
// Parameters
//   none
//
// Returns
//   TRUE -- if the Debug button should be displayed
//    FALSE -- otherwise
//
// Notes
//   This means "is there an appropriate debugger registered for auto attach?"
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::ShouldOfferDebug()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    AssertValid();

    // Permission check.
    if (!CurrentUserHasSufficientPermissions())
    {
        return FALSE;
    }

    // Check based on DbgJitDebugLaunchSetting & interactivity.
    if (m_debugAction != WHDebug_AskToLaunch)
    {   
        // Don't ask the user about debugging.  Do or don't debug; but don't ask.
        return FALSE;
    }

    SString ssDebuggerString;
    GetDebuggerSettingInfo(ssDebuggerString, NULL);

    // If there is no debugger installed, don't offer to debug, since we can't.
    if (ssDebuggerString.IsEmpty())
    {
        return FALSE;
    }

    return TRUE;

} // BOOL CLRWatsonHelper::ShouldOfferDebug()

//------------------------------------------------------------------------------
//
// ShouldAutoAttach
//
// Description
//      Determines whether or not a debugger should be launched
//      automatically, without prompting the user.
//
// Parameters
//   None.
//
// Returns
//      TRUE -- If a debugger should be auto-attached.
//      FALSE -- Otherwise
//
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::ShouldAutoAttach()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    AssertValid();

    // Permissions check.
    if (!CurrentUserHasSufficientPermissions())
    {
        return FALSE;
    }

    return (m_debugAction == WHDebug_AutoLaunch);
} // BOOL CLRWatsonHelper::ShouldAutoAttach()


//------------------------------------------------------------------------------
// Description
//   Returns whether a Watson report should be generated.
//
// Parameters
//   none
//
// Returns
//   TRUE - a Watson report should be generated (with a minidump).
//   FALSE - don't generate a report.
//
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::ShouldReport()
{
    WRAPPER_NO_CONTRACT;

    AssertValid();

    // If we queue or ask, we should generate.
    return (m_reportAction == WHReport_AutoQueue) || (m_reportAction == WHReport_AskToSend);

} // BOOL CLRWatsonHelper::ShouldReport()


//------------------------------------------------------------------------------
// Description
//   If a Watson report is generated, returns whether it should be auto-queued.
//   (vs asking the user what to do about it)
//
// Parameters
//   none
//
// Returns
//   TRUE - any Watson report should be be queued.
//   FALSE - any Watson report is posed to the user for "send" or "don't send".
//
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::ShouldQueueReport()
{
    WRAPPER_NO_CONTRACT;

    AssertValid();

    // If we queue a report.
    return (m_reportAction == WHReport_AutoQueue);

} // BOOL CLRWatsonHelper::ShouldQueueReport()

//------------------------------------------------------------------------------
// Description
//   Reads a PCHealth configuration LONG value from the registry.
//
// Parameters
//    szName   -- name of the value
//    iDefault -- default value, if not found
//
// Returns
//   The value read, or default if no value found.
//
// Exceptions
//   None
//
// NOtes:
// Looks in HKCU/Software/Policies/Microsoft/PCHealth/ErrorReporting
//  then in HKLM/  "        "       "           "       "
//  then in HKCU/SOftware/Microsoft/PCHealth/ErrorReporting
//  then in HKLM/  "        "       "           "
//------------------------------------------------------------------------------
int CLRWatsonHelper::GetPCHealthConfigLong(  // Return value from registry or default.
    LPCTSTR     szName,                 // Name of value to get.
    int        iDefault)               // Default value to return if not found.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    long        iValue;                 // Actual value from registry.

    // Try HKCR policy key
    if (GetRegistryLongValue(HKEY_CURRENT_USER, kErrorReportingPoliciesKey, szName, &iValue, FALSE))
        return iValue;

    // Try HKLM policy key
    if (GetRegistryLongValue(HKEY_LOCAL_MACHINE, kErrorReportingPoliciesKey, szName, &iValue, FALSE))
        return iValue;

    // Try HKCR key
    if (GetRegistryLongValue(HKEY_CURRENT_USER, kErrorReportingKey, szName, &iValue, FALSE))
        return iValue;

    // Try HKLM key
    if (GetRegistryLongValue(HKEY_LOCAL_MACHINE, kErrorReportingKey, szName, &iValue, FALSE))
        return iValue;

    // None of them had value -- return default.
    return iDefault;
} // long CLRWatsonHelper::GetPCHealthConfigLong()

//------------------------------------------------------------------------------
// Description
//   Reads a PCHealth configuration LONG value from the registry, from a
//    given subkey.
//
// Parameters
//    szSubKey -- name of the subkey.
//    szName   -- name of the value
//    iDefault -- default value, if not found
//    pValue   -- put value here.
//
// Returns
//   TRUE - a value was found in the registry
//   FALSE - no value found.
//
// Exceptions
//   None
//
// NOtes:
// Looks in HKCU/Software/Policies/Microsoft/PCHealth/ErrorReporting
//  then in HKLM/  "        "       "           "       "
//  then in HKCU/SOftware/Microsoft/PCHealth/ErrorReporting
//  then in HKLM/  "        "       "           "
//------------------------------------------------------------------------------
BOOL CLRWatsonHelper::GetPCHealthConfigSubKeyLong(  // Return value from registry or default.
    LPCWSTR     szSubKey,               // Name of the subkey.
    LPCWSTR     szName,                 // Name of value to get.
    int        iDefault,               // Default value to return if not found.
    DWORD       *pValue)                // Put the value (registry or default) here.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    long        iValue;                 // Actual value from registry.

    // Only one thread will *ever* enter this function, so it is safe to use a static
    //  buffer.  We know the the longest strings we will want to catenate.  Size
    //  the buffer appropriately, and we're set.
    static WCHAR rcBuf[lengthof(kErrorReportingPoliciesKey) + lengthof(kInclusionListSubKey) + 3];

    _ASSERT( (wcslen(kErrorReportingPoliciesKey) + wcslen(szSubKey) + 1) < lengthof(rcBuf));

    // Try HKCR policy key
    wcscpy_s(rcBuf, COUNTOF(rcBuf), kErrorReportingPoliciesKey);
    wcsncat_s(rcBuf, COUNTOF(rcBuf), szSubKey, lengthof(rcBuf)-wcslen(rcBuf)-1);

    if (GetRegistryLongValue(HKEY_CURRENT_USER, rcBuf, szName, &iValue, FALSE))
    {
        *pValue = iValue;
        return TRUE;
    }

    // Try the HKLM policy key
    if (GetRegistryLongValue(HKEY_LOCAL_MACHINE, rcBuf, szName, &iValue, FALSE))
    {
        *pValue = iValue;
        return TRUE;
    }

    // Try HKCR key
    wcscpy_s(rcBuf, COUNTOF(rcBuf), kErrorReportingKey);
    wcsncat_s(rcBuf, COUNTOF(rcBuf), szSubKey, lengthof(rcBuf)-wcslen(rcBuf)-1);

    if (GetRegistryLongValue(HKEY_CURRENT_USER, rcBuf, szName, &iValue, FALSE))
    {
        *pValue = iValue;
        return TRUE;
    }

    // Try HKLM key
    if (GetRegistryLongValue(HKEY_LOCAL_MACHINE, rcBuf, szName, &iValue, FALSE))
    {
        *pValue = iValue;
        return TRUE;
    }

    // None of them had value -- return default.
    *pValue = iDefault;
    return FALSE;
} // long CLRWatsonHelper::GetPCHealthConfigLong()


//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
HRESULT CreateWatsonSharedMemory(
    HANDLE      *hWatsonSharedMemory,
    DWSharedMem **ppWatsonSharedMemory)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Watson needs to inherit the shared memory block, so we have to set up
    // security attributes to make that happens.
    SECURITY_ATTRIBUTES securityAttributes;
    memset(&securityAttributes, 0, sizeof(securityAttributes));
    securityAttributes.nLength = sizeof(securityAttributes);
    securityAttributes.bInheritHandle = TRUE;

    _ASSERTE(NULL != hWatsonSharedMemory);
    _ASSERTE(NULL != ppWatsonSharedMemory);

    *hWatsonSharedMemory = NULL;
    *ppWatsonSharedMemory = NULL;

    // In cases where we have to return form this function with a failure, we
    // need to clean up the handle. Use a holder to take care of that for us.
    HandleHolder hTemp =
        WszCreateFileMapping(INVALID_HANDLE_VALUE,
                             &securityAttributes,
                             PAGE_READWRITE,
                             0,
                             sizeof(DWSharedMem),
                             NULL);

    if (hTemp == NULL)
    {
        return HRESULT_FROM_GetLastErrorNA();
    }

    DWSharedMem* pTemp =
        static_cast< DWSharedMem* >(CLRMapViewOfFile(hTemp,
                                                  FILE_MAP_ALL_ACCESS,
                                                  0,
                                                  0,
                                                  sizeof(DWSharedMem)));

    if (NULL == pTemp)
    {
        return HRESULT_FROM_GetLastErrorNA();
    }

    memset(pTemp, 0, sizeof(DWSharedMem));
    *hWatsonSharedMemory = hTemp;
    *ppWatsonSharedMemory = pTemp;

    // We're ready to exit normally and pass the IPC block's handle back to our
    // caller, so we don't want to close it.
    hTemp.SuppressRelease();

    return S_OK;
} // HRESULT CreateWatsonSharedMemory()



const WCHAR* kWatsonImageNameOnVista = W("\\dw20.exe");

//------------------------------------------------------------------------------
// Description
//      A helper function to launch the Watson process and wait for it to
//      complete
// Parameters
//      hWatsonSharedMemory
//          Handle to the shared memory block to pass to Watson. This handle
//          must be inheritable.
//      hEventAlive
//      hEventDone
//      hMutex
// Returns
//      true - If watson executed normally
//      false - if watson was unable to launch, reported an error, or
//              appeared to hang/crash
//------------------------------------------------------------------------------
BOOL RunWatson(
    HANDLE  hWatsonSharedMemory,
    HANDLE  hEventAlive,
    HANDLE  hEventDone,
    HANDLE  hMutex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!RunningOnWin7());
    }
    CONTRACTL_END;

    // Since we're doing our own error reporting, we don't want to pop up the
    // OS Watson dialog/GPF Dialog. Supress it now.

    PROCESS_INFORMATION processInformation;
    STARTUPINFOW startupInfo;
    memset(&startupInfo, 0, sizeof(STARTUPINFOW));
    startupInfo.cb = sizeof(STARTUPINFOW);

    HRESULT hr = S_OK;
    PathString watsonAppName;
    PathString watsonCommandLine;
    EX_TRY
    {
        do
        {


        

        {
    #if !defined(FEATURE_CORECLR)
            // Use the version of DW20.exe that lives in the system directory.
            DWORD ret;

            if (FAILED(GetCORSystemDirectoryInternaL(watsonAppName)))
            {
                hr = E_FAIL;
                break;
            }
            watsonCommandLine.Set(watsonAppName);
            watsonCommandLine.Append(kWatsonImageNameOnVista);

    #else // FEATURE_CORECLR
            HKEYHolder hKey;
            // Look for key \\HKLM\Software\Microsoft\PCHealth\ErrorReporting\DW\Installed"
            DWORD ret = WszRegOpenKeyEx(HKEY_LOCAL_MACHINE,
                                        kWatsonPath,
                                        0,
                                        KEY_READ | kWatsonRegKeyOptions,
                                        &hKey);

            if (ERROR_SUCCESS != ret)
            {
                hr = E_FAIL;
                break;
            }


            // Look in ...\DW\Installed for dw0200 (dw0201 on ia64).  This will be
            //  the full path to the executable.

            ClrRegReadString(hKey, kWatsonValue, watsonAppName);

    #endif // ! FEATURE_CORECLR

            COUNT_T len = watsonCommandLine.GetCount();
            WCHAR* buffer = watsonCommandLine.OpenUnicodeBuffer(len);
            _snwprintf_s(buffer,
                       len,
                       _TRUNCATE,
                       W("dw20.exe -x -s %lu"),
                       PtrToUlong(hWatsonSharedMemory));
            watsonCommandLine.CloseBuffer();

        }
        } while (false);
    }
    EX_CATCH_HRESULT(hr);


    if (hr != S_OK)
    {
        return false;
    }

        {
            BOOL ret = WszCreateProcess(watsonAppName,
                                        watsonCommandLine,
                                        NULL,
                                        NULL,
                                        TRUE,
                                        NULL,
                                        NULL,
                                        NULL,
                                        &startupInfo,
                                        &processInformation);

            if (FALSE == ret)
            {
                //
                // Watson failed to start up.
                //
                // This can happen if e.g. Watson wasn't installed on the machine.
                //
                 return  E_FAIL;
                 
            }

        }

    

    // Wait for watson to finish.
    //
    // This code was more-or-less pasted directly out of the test app for
    // watson, found at
    //
    // \\redist\redist\Watson\dw20_latest\neutral\retail\0\testcrash.cpp

    // These handles need to live until we're done waiting for the watson
    // process to finish execution.
    HandleHolder hProcess(processInformation.hProcess);
    HandleHolder hThread(processInformation.hThread);


    BOOL watsonSignalledCompletion = FALSE, bDWRunning = TRUE;

    while (bDWRunning)
    {
        if (WAIT_OBJECT_0 == ClrWaitForSingleObject(hEventAlive,
                                                    kDwWaitTime))
        {
            // Okay, Watson's still pinging us; see if it's finished.
            if (WAIT_OBJECT_0 == ClrWaitForSingleObject(hEventDone, 1))
            {
                bDWRunning = FALSE;
                watsonSignalledCompletion = TRUE;
            }

            // If watson is finished (i.e. has signaled hEventDone),
            // bDWRunning is false and we'll fall out of the loop. If
            // watson isn't finished, we'll go back to waiting for the
            // next ping on hEventAlive
            continue;
        }

        Thread::BeginThreadAffinity();
        // we timed-out waiting for DW to respond.
        DWORD dw = WaitForSingleObject(hMutex, DW_TIMEOUT_VALUE);

        if (WAIT_TIMEOUT == dw)
        {
            // either DW's hung or crashed, we must carry on. Let watson
            // no that we're giving up on watson, in case it comes back
            // from the hang.
            SetEvent(hEventDone);
            bDWRunning = FALSE;
        }
        else if (WAIT_ABANDONED == dw)
        {
            // The mutex was abandoned, which means Watson crashed on
            // us.
            bDWRunning = FALSE;

            ReleaseMutex(hMutex);
        }
        else
        {
            // Check one last time to see if Watson has woken up.
            if (WAIT_OBJECT_0 != ClrWaitForSingleObject(hEventAlive, 1))
            {
                // Nope. hasn't woken up. Give up on Watson
                SetEvent(hEventDone);
                bDWRunning = FALSE;
            }
            else
            {
                // Oh, it HAS woken up! See if it's finished as well.
                if (WAIT_OBJECT_0 == ClrWaitForSingleObject(hEventDone, 1))
                {
                    bDWRunning = FALSE;
                    watsonSignalledCompletion = TRUE;
                }
            }

            ReleaseMutex(hMutex);
        }
        Thread::EndThreadAffinity();
    }

    // Go ahead and bail if Watson didn't exit for some reason.
    if (!watsonSignalledCompletion)
    {
        return FALSE;
    }

    // We're now done with hProcess and hThread, it's safe to let the
    // HandleHolders destroy them now.
    //
    // We don't need to wait for the Watson process to exit; once it's signalled
    // "hEventDone" it's safe to assume that Watson will not try communicating
    // with us anymore and we have succeeded.
    return true;
} // BOOL RunWatson()


//
// Constants used to control various aspects of Watson's behavior.
//


// Flags controlling the minidump Watson creates.
const DWORD kMiniDumpType = MiniDumpNormal;
const DWORD kThreadWriteFlags = ThreadWriteThread | ThreadWriteContext | ThreadWriteStack;
const DWORD kModuleWriteFlags = ModuleWriteModule; // | ModuleWriteDataSeg ?



// Reporting. The defaults are fine here
const DWORD kReportingFlags =  0;

//
// Enable these flags if the report should be queued (i.e., if no UI should be
// shown, but a report should still be sent).
//

// Enable these flags are for bfDWRFlags
const DWORD kQueuingReportingFlags = fDwrForceToAdminQueue | fDwrIgnoreHKCU;

// Enable these flags in the bfDWUFlags field
const DWORD kQueuingUIFlags = fDwuNoEventUI;

//
// No reporting flags. Enable these flags if an error report should not be sent.
//

// Enable these flags in bfDWRFlags if a report is not to be sent.
const DWORD kNoReportFlags = fDwrNeverUpload;


// UI Flags
//
// We need to use the light plea, since we may be reporting faults for
// Non-Microsoft software (if some random 3rd party app throws an exception, we
// can't really promise that their error report will be used to fix the
// problem).
//
const DWORD kUIFlags = fDwuDenySuspend | fDwuShowFeedbackLink;

// Exception mode flags. By default, the "restart" and "recover" buttons are
//  checked. We need to turn that behavior off.  We also need to use the
//  minidump API to gather the heap dump, in order to get a managed-aware
//  minidump.  Finally, release the dumping thread before doing the cabbing
//  for performance reasons.
const DWORD kExceptionModeFlags = fDweDefaultQuit | fDweGatherHeapAsMdmp | fDweReleaseBeforeCabbing;

// "Miscellaneous" flags. These flags are only used by Office.
const DWORD kMiscFlags = 0;

// Flags to control which buttons are available on the Watson dialog.
//
// We will only display the "Send Error Report" and "Don't Send" buttons
// available -- we're not going to make the "restart" or "recover" checkboxes
// available by default.
const DWORD kOfferFlags = msoctdsQuit;

//------------------------------------------------------------------------------
// Description
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
#ifndef FEATURE_CORECLR
    if (bucketType == MoCrash)
    {
        MoCrashBucketParamsManager moCrashManager(pGenericModeBlock, tore, currentPC, pThread, pThrowable);
        moCrashManager.PopulateBucketParameters();
    }
    else
#endif // !FEATURE_CORECLR
    {
#ifdef FEATURE_WINDOWSPHONE
        _ASSERTE(bucketType == WinPhoneCrash);
        WinPhoneBucketParamsManager winphoneManager(pGenericModeBlock, tore, currentPC, pThread, pThrowable);
        winphoneManager.PopulateBucketParameters();
#else
        // if we default to CLR20r3 then let's assert that the bucketType is correct
        _ASSERTE(bucketType == CLR20r3);
        CLR20r3BucketParamsManager clr20r3Manager(pGenericModeBlock, tore, currentPC, pThread, pThrowable);
        clr20r3Manager.PopulateBucketParameters();
#endif // FEATURE_WINDOWSPHONE
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
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

#if defined(PRESERVE_WATSON_ACROSS_CONTEXTS)
    GenericModeBlock *pBuckets = NULL;

#ifdef FEATURE_CORECLR
    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this.
    if (IsWatsonEnabled())
#endif // FEATURE_CORECLR
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
        SO_NOT_MAINLINE;
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


//------------------------------------------------------------------------------
// Description
//
// Parameters
//      pExceptionInfo -- information about the exception that caused the error.
//           If the error is not the result of an exception, pass NULL for this
//           parameter
//      tore           -- Information about the fault
//      pThread        -- Thread object for faulting thread, could be NULL
//      dwThreadID     -- OS Thread ID for faulting thread
//
// Returns
//      FaultReportResult -- enumeration indicating the
//           FaultReportResultAbort -- if Watson could not execute normally
//           FaultReportResultDebug -- if Watson executed normally, and the user
//              chose to debug the process
//           FaultReportResultQuit  -- if Watson executed normally, and the user
//              chose to end the process (e.g. pressed "Send Error Report" or
//              "Don't Send").
//
// Exceptions
//      None.
//------------------------------------------------------------------------------
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
FaultReportResult DoFaultReportWorker(      // Was Watson attempted, successful?  Run debugger?
    EXCEPTION_POINTERS  *pExceptionInfo,    // Information about the fault.
    TypeOfReportedError tore,               // What sort of error is this?
    Thread              *pThread,           // Thread object for faulting thread, could be NULL
    DWORD               dwThreadID)         // OS Thread ID for faulting thread
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(!RunningOnWin7());

    LOG((LF_EH, LL_INFO100, "DoFaultReportWorker: at sp %p ...\n", GetCurrentSP()));

    if (!IsWatsonEnabled())
    {
        return FaultReportResultQuit;
    }

#if !defined(FEATURE_UEF_CHAINMANAGER)
    // If we've already tried to report a Watson crash once, we don't really
    // want to pester the user about this exception. This can occur in certain
    // pathological programs.
    // For events other than user breakpoint, we only want to report once.
    // For user breakpoints, report whenever the thread wants to.
    if (!tore.IsUserBreakpoint())
    {
        // If Watson already launched (say, on another thread)...
        if (FastInterlockCompareExchange(&g_watsonAlreadyLaunched, 1, 0) != 0)
        {
            // wait until Watson process is completed
            ClrWaitForSingleObject(g_hWatsonCompletionEvent, INFINITE_TIMEOUT);
            return FaultReportResultQuit;
        }
    }
#endif // FEATURE_UEF_CHAINMANAGER

    // Assume an unmanaged fault until we determine otherwise.
    BOOL bIsManagedFault = FALSE;

    // IF we don't have an ExceptionInfo, what does that mean?
    if (pExceptionInfo)
    {
        if (IsExceptionFromManagedCode(pExceptionInfo->ExceptionRecord))
        {
            // This is a managed fault.
            bIsManagedFault = TRUE;
        }
    }

    // Figure out what we should do.
    CLRWatsonHelper policy;
    policy.Init(bIsManagedFault, tore);

    if (policy.ShouldAutoAttach())
    {
        return FaultReportResultDebug;
    }

    // Is there anything for Watson to do?  (Either report, or ask about debugging?)
    if ((!policy.ShouldReport()) && (!policy.ShouldOfferDebug()) && (!policy.ShouldShowUI()))
    {
        // Hmm ... we're not supposed to report anything or pop up a dialog. In
        // this case, we can stop right now.
        return FaultReportResultQuit;
    }

    HANDLE hWatsonSharedMemory;
    DWSharedMem *pWatsonSharedMemory;
    {
        HRESULT hr = CreateWatsonSharedMemory(&hWatsonSharedMemory,
                                              &pWatsonSharedMemory);
        if (FAILED(hr))
        {
            return FaultReportResultAbort;
        }
    }

    // Some basic bookkeeping for Watson
    pWatsonSharedMemory->dwSize = sizeof(DWSharedMem);
    pWatsonSharedMemory->dwVersion = DW_CURRENT_VERSION;
    pWatsonSharedMemory->pid = GetCurrentProcessId();
    pWatsonSharedMemory->tid = dwThreadID;
    _snwprintf_s(pWatsonSharedMemory->wzEventLogSource, 
                 NumItems(pWatsonSharedMemory->wzEventLogSource),
                 _TRUNCATE,
                 W(".NET Runtime %0d.%0d Error Reporting"),
                 VER_MAJORVERSION,
                 VER_MINORVERSION);
    pWatsonSharedMemory->eip = (pExceptionInfo) ? reinterpret_cast< DWORD_PTR >(pExceptionInfo->ExceptionRecord->ExceptionAddress) : NULL;

    // If we set exception pointers, the debugger will automatically do a .ecxr on them.  SO,
    //  don't set the pointers unless it really is an exception and we have a
    // a good context record
    if (tore.IsException() ||
        (tore.IsFatalError() && pExceptionInfo && pExceptionInfo->ContextRecord && 
        (pExceptionInfo->ContextRecord->ContextFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
       )
    {
        pWatsonSharedMemory->pep = pExceptionInfo;
    }
    else
    {
        pWatsonSharedMemory->pep = NULL;
    }

    // Handles to kernel objects that Watson uses.
    //
    // We're expecting these handles to be valid until the Watson child process
    // has run to completion. Make sure these holders stay in scope until after
    // the call to RunWatson

    HandleHolder hEventDone(NULL),
                 hEventNotifyDone(NULL),
                 hEventAlive(NULL),
                 hMutex(NULL),
                 hProc(NULL),
                 sharedMemoryHolder(hWatsonSharedMemory);
    {
        // SECURITY_ATTRIBUTES so the handles can be inherited (by Watson).
        SECURITY_ATTRIBUTES securityAttributes =
            { sizeof(SECURITY_ATTRIBUTES), NULL, true };

        hEventDone = WszCreateEvent(&securityAttributes, FALSE, FALSE, NULL);
        if (hEventDone == NULL)
        {
            LOG((LF_EH, LL_INFO100, "CLR Watson: WszCreateEvent returned error, GetLastError(): %#x\n", GetLastError()));
            return FaultReportResultAbort;
        }
        pWatsonSharedMemory->hEventDone = hEventDone;


        hEventNotifyDone = WszCreateEvent(&securityAttributes, FALSE, FALSE, NULL);
        if (hEventNotifyDone == NULL)
        {
            LOG((LF_EH, LL_INFO100, "CLR Watson: WszCreateEvent returned error, GetLastError(): %#x\n", GetLastError()));
            return FaultReportResultAbort;
        }
        pWatsonSharedMemory->hEventNotifyDone = hEventNotifyDone;


        hEventAlive = WszCreateEvent(&securityAttributes, FALSE, FALSE, NULL);
        if (hEventAlive == NULL)
        {
            LOG((LF_EH, LL_INFO100, "CLR Watson: WszCreateEvent returned error, GetLastError(): %#x\n", GetLastError()));
            return FaultReportResultAbort;
        }
        pWatsonSharedMemory->hEventAlive = hEventAlive;


        hMutex = WszCreateMutex(&securityAttributes, FALSE, NULL);
        if (hMutex == NULL)
        {
            LOG((LF_EH, LL_INFO100, "CLR Watson: WszCreateEvent returned error, GetLastError(): %#x\n", GetLastError()));
            return FaultReportResultAbort;
        }
        pWatsonSharedMemory->hMutex = hMutex;
    }

    // During error reporting we need to do dump collection, freeze threads inside the process, read memory blocks 
    // (if you register memory), read stuff from the PEB, create remote threads for recovery. So it needs quite a 
    // lot of permissions; we end up with PROCESS_ALL_ACCESS to satisfy all required permissions.
    hProc = OpenProcess(PROCESS_ALL_ACCESS,
                        TRUE,
                        pWatsonSharedMemory->pid);
    if (hProc == NULL)
    {
        LOG((LF_EH, LL_INFO100, "CLR Watson: OpenProcess returned error, GetLastError(): %#x\n", GetLastError()));
        return FaultReportResultAbort;
    }

    pWatsonSharedMemory->hProc = hProc;


    // Flags to control reporting, queuing, etc.
    DWORD reportingFlags = kReportingFlags;     // 0
    DWORD uiFlags = kUIFlags;                   // fDwuDenySuspend | fDwuShowFeedbackLink
    DWORD dwEflags = kExceptionModeFlags;       // fDweDefaultQuit | fDweGatherHeapAsMdmp

    // Reporting flags...
    if (policy.ShouldQueueReport())
    {   // If we should queue a report,
        //  turn on kQueueingReportingFlags, which is fDwrForceToAdminQueue | fDwrIgnoreHKCU
        reportingFlags |= kQueuingReportingFlags;
    }
    else
    if (!policy.ShouldReport())
    {   // We shouldn't report at all,
        //  turn on kNoReportFlags, which is fDwrNeverUpload, which means "don't report"
        reportingFlags |= kNoReportFlags;
    }
    else
    {
        // Ask to report.
    }

    // Offer flags...
    DWORD offerFlags = kOfferFlags;             // msoctdsQuit
    if (policy.ShouldOfferDebug())
    {   // Turn on msoctdsDebug, which adds "Debug" button.
        offerFlags |= msoctdsDebug;
    }
    else
    {   // No debug, so ignore aeDebug
        dwEflags |= fDweIgnoreAeDebug;
    }

    // UI flags...
    if (policy.ShouldQueueReport() && !policy.ShouldOfferDebug())
    {   // Queue report headlessly. Turn on kQueueingUIFlags, which is fDwuNoEventUI.
        uiFlags |= kQueuingUIFlags;
    }

    pWatsonSharedMemory->bfmsoctdsOffer = offerFlags;       // From above
    pWatsonSharedMemory->bfDWRFlags = reportingFlags;       // From above
    pWatsonSharedMemory->bfDWUFlags = uiFlags;              // From above
    pWatsonSharedMemory->bfDWEFlags = dwEflags;             // From above
    pWatsonSharedMemory->bfDWMFlags = kMiscFlags;           // 0

    // We're going to rely on Watson's default localization behavior.
    pWatsonSharedMemory->lcidUI = 0;

    // By default, Watson will terminate the process after snapping a
    //  minidump.  Notify & LetRun flags disable that.
    pWatsonSharedMemory->bfmsoctdsNotify = msoctdsNull;
    pWatsonSharedMemory->bfmsoctdsLetRun = offerFlags;

    {
        PathString wzModuleFileName;
        DWORD dwRet = WszGetModuleFileName(NULL,
	                                       wzModuleFileName);
        BaseBucketParamsManager::CopyStringToBucket(pWatsonSharedMemory->wzModuleFileName, NumItems(pWatsonSharedMemory->wzModuleFileName), wzModuleFileName);
        
        _ASSERTE(0 != dwRet);
        if (0 == dwRet)
        {
            LOG((LF_EH, LL_INFO100, "CLR Watson: WszGetModuleFileName returned error, GetLastError(): %#x\n", GetLastError()));
            return FaultReportResultAbort;
        }
    }

    // We're going capture the same minidump information for all modules, so set wzDotDataDlls to "*"
    if (sizeof(DW_ALLMODULES) <= sizeof(pWatsonSharedMemory->wzDotDataDlls))
    {
        memcpy(pWatsonSharedMemory->wzDotDataDlls, DW_ALLMODULES, sizeof(DW_ALLMODULES));
    }
    else
    {
        // Assert, but go on
        _ASSERTE(sizeof(DW_ALLMODULES) <= sizeof(pWatsonSharedMemory->wzDotDataDlls));
        pWatsonSharedMemory->wzDotDataDlls[0] = 0;
    }

    // UI Customization
    //
    // The only UI customization we perform is to set the App Name. Currently we
    // do this just by using the executable name.
    //
    {
        PathString   buf;         // Buffer for path for description.
        LPCWSTR   pName ;           // Pointer to filename or description.
        int     size;                   // Size of description.
        HMODULE hModule;                // Handle to module.
        DWORD   result;                 // Return code

        // Get module name.
        hModule = WszGetModuleHandle(NULL);
        result = WszGetModuleFileName(hModule, buf);

        if (result == 0)
        {   // Couldn't get module name.  This should never happen.
            pName = W("<<unknown>>");
        }
        else
        {   // re-use the buf for pathname and description.
            size = DwGetAppDescription(buf, buf);
            pName = buf.GetUnicode();
            // If the returned size was zero, buf wasn't changed, and still contains the path.
            //  find just the filename part.
            if (size == 0)
            {   // Look for final '\'
                pName = wcsrchr(buf, W('\\'));
                // If found, skip it; if not, point to full name.
                pName = pName ? pName+1 : buf;
            }
        }

        wcsncpy_s(pWatsonSharedMemory->uib.wzGeneral_AppName,
                  COUNTOF(pWatsonSharedMemory->uib.wzGeneral_AppName),
                  pName,
                  _TRUNCATE);

        // For breakpoint, need to customize the "We're sorry..." message
        if (tore.IsBreakpoint())
        {
            LCID lcid = 0;
            // Get the message.
            StackSString sszMain_Intro_Bold;
            StackSString sszMain_Intro_Reg;
            EX_TRY
            {
                sszMain_Intro_Bold.LoadResource(CCompRC::Debugging, IDS_WATSON_DEBUG_BREAK_INTRO_BOLD);
                sszMain_Intro_Reg.LoadResource(CCompRC::Debugging, IDS_WATSON_DEBUG_BREAK_INTRO_REG);
                // Try to determine the language used for the above resources
                // At the moment this OS call is a heuristic which should match most of the time.  But the
                // CLR is starting to support languages that don't even have LCIDs, so this may not always
                // be correct (and there may be NO LCID we can pass to watson).  Long term, the correct fix
                // here is to get out of the game of making watson policy / UI decisions.  This is happening
                // for Windows 7.
                lcid = GetThreadLocale();
            }
            EX_CATCH
            {
                // Just don't customize.
            }
            EX_END_CATCH(SwallowAllExceptions)

            // If we were able to get a string, set it.
            if (sszMain_Intro_Reg.GetCount() > 0)
            {
                // Instead of "<app.exe> has encountered an error and nees to close...", say
                //  "<app.exe> has encountered a user-defined breakpoint."
                wcsncpy_s(pWatsonSharedMemory->uib.wzMain_Intro_Bold, COUNTOF(pWatsonSharedMemory->uib.wzMain_Intro_Bold), sszMain_Intro_Bold, _TRUNCATE);
                // Instead of "If you were in the middle of something...", say
                //  "A breakpoint in an application indicates a program error..."
                wcsncpy_s(pWatsonSharedMemory->uib.wzMain_Intro_Reg, COUNTOF(pWatsonSharedMemory->uib.wzMain_Intro_Reg), sszMain_Intro_Reg,  _TRUNCATE);

                pWatsonSharedMemory->bfDWUFlags = fDwuDenySuspend;

                pWatsonSharedMemory->lcidUI = lcid;
            }
        }

    }

    // Get the bucket parameters.
    switch (tore.GetType())
    {
    case TypeOfReportedError::NativeThreadUnhandledException:
        // Let Watson provide the buckets for a native thread.
        break;
    case TypeOfReportedError::UnhandledException:
    case TypeOfReportedError::FatalError:
    case TypeOfReportedError::UserBreakpoint:
    case TypeOfReportedError::NativeBreakpoint:
        // For managed exception or exceptions that come from managed code, we get the managed bucket parameters, 
        // which will be displayed in the "details" section on any UI.  
        //
        // Otherwise, use the unmanaged IP to bucket.
        if (bIsManagedFault)
        {
            RetrieveManagedBucketParameters(pExceptionInfo?pExceptionInfo->ExceptionRecord:NULL, &pWatsonSharedMemory->gmb, tore, pThread);
        }
        break;
    default:
        _ASSERTE(!"Unexpected TypeOfReportedException");
        break;
    }

    // dwThisThreadExFlags and dwOtherThreadExFlags are only used on IA64.
    CustomMinidumpBlock cmb =
    {
        TRUE,                           // fCustomMinidump
        kMiniDumpType,                  // dwMinidumpType : MiniDumpNormal
        FALSE,                          // fOnlyThisThread
        kThreadWriteFlags,              // dwThisThreadFlags : ThreadWriteThread | ThreadWriteContext | ThreadWriteStack
        kThreadWriteFlags,              // dwOtherThreadFlags
        0,                              // dwThisThreadExFlags
        0,                              // dwOtherThreadExFlags
        kModuleWriteFlags,              // dwPreferredModuleFlags
        kModuleWriteFlags               // dwOtherModuleFlags.
    };

    pWatsonSharedMemory->cmb = cmb;

    // At this point, the IPC block is all ready to go
    BOOL result = false;
    // There are two calls to RunWatson below. We want the second call to execute iff
    // secondInvocation is true.
    BOOL secondInvocation = true;


    EX_TRY
    {
        bool fRunWatson        = false;
#if defined(_TARGET_X86_)
        bool fGuardPagePresent = false;

        // There is an unfortunate side effect of calling ReadProcessMemory() out-of-process on IA64 WOW.  
        // On all platforms (IA64 native & WOW64, AMD64 native & WOW64, and x86 native), if we call 
        // ReadProcessMemory() out-of-process on a page with PAGE_GUARD protection, the read operation 
        // fails as expected.  However, on IA64 WOW64 only, the PAGE_GUARD protection is removed after 
        // the read operation.  Even IA64 native preserves the PAGE_GUARD protection.  
        // See VSW 451447 for more information.
        if ((pThread != NULL) && pThread->DetermineIfGuardPagePresent())
        {
            fGuardPagePresent = true;
        }
#endif // _TARGET_X86_

        if (secondInvocation)
        {
            fRunWatson = true;
            result = RunWatson(hWatsonSharedMemory,
                               pWatsonSharedMemory->hEventAlive,
                               pWatsonSharedMemory->hEventDone,
                               pWatsonSharedMemory->hMutex);
        }

#if defined(_TARGET_X86_)
        if (fRunWatson && fGuardPagePresent)
        {
            // This shouldn't cause a problem because guard pages are present in the first place.
            _ASSERTE(pThread != NULL);
            pThread->RestoreGuardPage();
        }
#endif // _TARGET_X86_
    }
    EX_CATCH
    {
        // We couldn't wait around for watson to execute for some reason.
        result = false;
    }
    EX_END_CATCH(SwallowAllExceptions)

    // It's now safe to close all the synchronization and process handles.

    if (!result)
    {
        // Hmmm ... watson couldn't execute correctly.
        return FaultReportResultAbort;
    }

    LOG((LF_EH, LL_INFO100, "CLR Watson: returned 0x%x\n", pWatsonSharedMemory->msoctdsResult));

    // If user clicked "Debug"
    if (msoctdsDebug == pWatsonSharedMemory->msoctdsResult)
    {
        return FaultReportResultDebug;
    }

    // No debugging, successful completion.
    return FaultReportResultQuit;
} // FaultReportResult DoFaultReportWorker()
#ifdef _PREFAST_
#pragma warning(pop)
#endif

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

class WatsonSOExceptionAddress {
    public:
        
        WatsonSOExceptionAddress() 
        {
            m_SystemMethod = NULL;
            m_UserMethod = NULL;
        }

        SLOT  m_SystemMethod;         // IP in the first method on the stack which is in a system module
        SLOT  m_UserMethod;           // IP in the first method on the stack which is in a non-system module
};

//------------------------------------------------------------------------------
// Description
//      This function is the stack walk callback for a thread that hit a soft SO (i.e., a SO caused by a
//      failed stack probe).  
//
// Parameters
//      pCf -- A pointer to the current CrawlFrame
//      data - A pointer to WatsonSOExceptionAddress instance
//
// Returns:
//     SWA_ABORT to stop the stack crawl
//     SWA_CONTINUE to continue crawling the stack
//
// Exceptions
//      None.
//------------------------------------------------------------------------------
StackWalkAction WatsonSOStackCrawlCallback(CrawlFrame* pCf, void* pParam)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pParam != NULL);
    WatsonSOExceptionAddress *pData = (WatsonSOExceptionAddress *) pParam;

    SLOT ip;

    if (pCf->IsFrameless())
    {
        ip = (PBYTE)GetControlPC(pCf->GetRegisterSet());
    }
    else
    {
        ip = (SLOT) pCf->GetFrame()->GetIP();
    }
   
    MethodDesc *pMD = pCf->GetFunction();

    if (pMD != NULL)
    {
        if (pMD->GetModule()->IsSystem())
        {
            if (pData->m_SystemMethod == NULL)
            {
                pData->m_SystemMethod = ip;
            }
            return SWA_CONTINUE;
        }
        else
        {
            _ASSERTE(pData->m_UserMethod == NULL);
            pData->m_UserMethod = ip;
            return SWA_ABORT;
        }        
    }
    else
    {
        return SWA_CONTINUE;
    }

}// WatsonSOCrawlCallBack

//------------------------------------------------------------------------------
// Description
//      Wrapper function for DoFaultReport. This function is called for SOs. 
//      It sets up the ExceptionInfo appropriately for soft SOs (caused by 
//      failed stack probes) before callign DoFaultReport.
//
// Parameters
//      pParam -- A pointer to a WatsonThreadData instance
//
// Exceptions
//      None.
//------------------------------------------------------------------------------
DWORD WINAPI DoFaultReportWorkerCallback(LPVOID pParam)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pParam != NULL);

    WatsonThreadData* pData = (WatsonThreadData*) pParam;

    EXCEPTION_POINTERS ExceptionInfo;
    EXCEPTION_RECORD  ExceptionRecord;
    PEXCEPTION_POINTERS pExceptionInfo = pData->pExceptionInfo;

    if (IsSOExceptionCode(pExceptionInfo->ExceptionRecord->ExceptionCode))
    {
        EX_TRY
        {
            if (ShouldLogInEventLog())
            {
                EventReporter reporter(EventReporter::ERT_StackOverflow);
                reporter.Report();                
            }
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    
    // The purpose of the loop below is to avoid deadlocks during the abnormal process termination.
    // We will try to acquire the lock for 100 times to see whether we can successfully grap it. If we
    // can, then we can setup the thread and report the fault without worrying about the deadlock. 
    // Otherwise we won't report the fault. It's still possible that we can enter the critical section 
    // and report the fault without having deadlocks after this spin, but compared to the risky of 
    // having deadlock, we still prefer not to report the fault if we can't get the lock after spin.
    BOOL isThreadSetup = false;
    for (int i = 0; i < 100; i++)
    {
        if (ThreadStore::CanAcquireLock())
        {
            SetupThread();
            isThreadSetup = true;
            break;
        }
        __SwitchToThread(30, CALLER_LIMITS_SPINNING);
    }
    
    if (isThreadSetup)
    {
        GCX_COOP();
    
        if (pData->pThread != NULL && pExceptionInfo != NULL && 
            pExceptionInfo->ContextRecord == NULL &&
            pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW &&
            pExceptionInfo->ExceptionRecord->ExceptionAddress == 0)
        {
            // In the case of a soft SO on a managed thread, we set the ExceptionAddress to one of the following
            // 
            // 1. The first method on the stack that is in a non-system module.
            // 2. Failing that, the first method on the stack that is in a system module

            CONTEXT ContextRecord;
            memset(&ContextRecord, 0, sizeof(CONTEXT));

            ExceptionInfo.ContextRecord = &ContextRecord; // To display the "Send" button, dw20 wants a non-NULL pointer
            ExceptionRecord = *(pExceptionInfo->ExceptionRecord);
            ExceptionInfo.ExceptionRecord = &ExceptionRecord;
            pExceptionInfo = &ExceptionInfo;

            WatsonSOExceptionAddress WatsonExceptionAddresses;
        
            pData->pThread->StackWalkFrames(
                                     WatsonSOStackCrawlCallback, 
                                     &WatsonExceptionAddresses, 
                                     FUNCTIONSONLY|ALLOW_ASYNC_STACK_WALK);
        
            if (WatsonExceptionAddresses.m_UserMethod != NULL)
            {
                pExceptionInfo->ExceptionRecord->ExceptionAddress = WatsonExceptionAddresses.m_UserMethod;
            }
            else if (WatsonExceptionAddresses.m_SystemMethod != NULL)
            {
                pExceptionInfo->ExceptionRecord->ExceptionAddress = WatsonExceptionAddresses.m_SystemMethod;
            }
            
        }

        pData->result = DoFaultReportWorker(
                            pExceptionInfo, 
                            pData->tore,
                            pData->pThread,
                            pData->dwThreadID);
    }


    return 0;

} // void DoFaultReportFavorWorker()

DWORD WINAPI ResetWatsonBucketsCallbackForStackOverflow(LPVOID pParam)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(IsWatsonEnabled());
        PRECONDITION(RunningOnWin7());
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
        PRECONDITION(RunningOnWin7());
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


//------------------------------------------------------------------------------
// Description
//      This function is called by the Debugger thread in response to a favor
//      posted to it by the faulting thread. The faulting thread uses the 
//      Debugger thread to invoke Watson in the case of stack overflows.
//      Since the debugger thread doesn't have a managed Thread object,
//      it cannot be directly used to call DoFaultReport. Instead, this function
//      spawns a worker thread and waits for it to complete.
//
// Parameters
//      pParam -- A pointer to a WatsonThreadData instance
//
// Exceptions
//      None.
//------------------------------------------------------------------------------
void DoFaultReportFavorWorker(void* pParam)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pParam != NULL);

    HANDLE hThread = NULL;
    DWORD dwThreadId;

    hThread = ::CreateThread(NULL, 0, DoFaultReportWorkerCallback, pParam, 0, &dwThreadId);
    if (hThread != NULL)
    {
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
    }

    return;

} // void DoFaultReportFavorWorker()

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



//------------------------------------------------------------------------------
// Description
//
// Parameters
//      pExceptionInfo -- information about the exception that caused the error.
//           If the error is not the result of an exception, pass NULL for this
//           parameter
//      tore           -- Information about the fault
// Returns
//      FaultReportResult -- enumeration indicating the
//           FaultReportResultAbort -- if Watson could not execute normally
//           FaultReportResultDebug -- if Watson executed normally, and the user
//              chose to debug the process
//           FaultReportResultQuit  -- if Watson executed normally, and the user
//              chose to end the process (e.g. pressed "Send Error Report" or
//              "Don't Send").
//
// Exceptions
//      None.
//------------------------------------------------------------------------------
FaultReportResult DoFaultReport(            // Was Watson attempted, successful?  Run debugger?
    EXCEPTION_POINTERS  *pExceptionInfo,    // Information about the fault.
    TypeOfReportedError tore)               // What sort of error is this?
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(!RunningOnWin7());

    LOG((LF_EH, LL_INFO100, "DoFaultReport: at sp %p ...\n", GetCurrentSP()));

    Thread *pThread = GetThread();

#ifdef FEATURE_CORECLR    
    // If watson isn't available (eg. in Silverlight), then use a simple dialog box instead
    if (!IsWatsonEnabled())
    {
        if (!pThread)
        {
            return FaultReportResultAbort;
        }

        // Since the StackOverflow handler also calls us, we must keep our stack budget
        // to a minimum. Thus, we will launch a thread to do the actual work.
        FaultReportInfo fri;
        fri.m_fDoReportFault    = FALSE;
        fri.m_pExceptionInfo    = pExceptionInfo;
        fri.m_threadid          = GetCurrentThreadId();
        // DoFaultCreateThreadReportCallback will overwrite this - if it doesn't, we'll assume it failed.
        fri.m_faultReportResult = FaultReportResultAbort;  

        GCX_PREEMP();


        if (pExceptionInfo->ExceptionRecord->ExceptionCode != STATUS_STACK_OVERFLOW)
        {
            DoFaultReportCreateThreadCallback(&fri);
        }
        else
        {
            // Stack overflow case - we don't have enough stack on our own thread so let the debugger
            // helper thread do the work.
            if (!g_pDebugInterface || FAILED(g_pDebugInterface->RequestFavor(DoFaultReportDoFavorCallback, &fri)))
            {
                // If we can't initialize the debugger helper thread or we are running on the debugger helper
                // thread, give it up. We don't have enough stack space.
            
            }
        }

        return fri.m_faultReportResult;
    } 
#endif // FEATURE_CORECLR

#ifdef FEATURE_UEF_CHAINMANAGER
    if (g_pUEFManager && !tore.IsUserBreakpoint())
    {
        IWatsonSxSManager * pWatsonSxSManager = g_pUEFManager->GetWastonSxSManagerInstance();
        
        // Has Watson report been triggered?
        if (pWatsonSxSManager->HasWatsonBeenTriggered())
        {
            LOG((LF_EH, LL_INFO100, "DoFaultReport: Watson has been triggered."));
            LeaveRuntimeHolderNoThrow holder(reinterpret_cast< size_t >(WaitForSingleObject));
            pWatsonSxSManager->WaitForWatsonSxSCompletionEvent();
            return FaultReportResultQuit;
        }
        // The unhandled exception is thrown by the current runtime.
        else if (IsExceptionFromManagedCode(pExceptionInfo->ExceptionRecord))   
        {
            // Is the current runtime allowed to report Watson?
            if (!pWatsonSxSManager->IsCurrentRuntimeAllowedToReportWatson())
            {
                LOG((LF_EH, LL_INFO100, "DoFaultReport: Watson is reported by another runtime."));
                LeaveRuntimeHolderNoThrow holder(reinterpret_cast< size_t >(WaitForSingleObject));
                pWatsonSxSManager->WaitForWatsonSxSCompletionEvent();
                return FaultReportResultQuit;
            }
        }
        // The unhandled exception is thrown by another runtime in the process.
        else if (pWatsonSxSManager->IsExceptionClaimed(pExceptionInfo->ExceptionRecord))
        {
            LOG((LF_EH, LL_INFO100, "DoFaultReport: Watson will be reported by another runtime.\n"));
            return FaultReportResultQuit;
        }
        // The unhandled exception is thrown by native code.
        else
        {
            // Is the current runtime allowed to report Watson?
            if (!pWatsonSxSManager->IsCurrentRuntimeAllowedToReportWatson())
            {
                LOG((LF_EH, LL_INFO100, "DoFaultReport: Watson is reported by another runtime."));
                LeaveRuntimeHolderNoThrow holder(reinterpret_cast< size_t >(WaitForSingleObject));
                pWatsonSxSManager->WaitForWatsonSxSCompletionEvent();
                return FaultReportResultQuit;
            }
        }
    }
#endif // FEATURE_UEF_CHAINMANAGER

    // Check if the current thread has the permission to open a process handle of the current process.
    // If not, the current thread may have been impersonated, we have to launch Watson from a new thread as in SO case.
    BOOL fOpenProcessFailed = FALSE;
    if (pExceptionInfo->ExceptionRecord->ExceptionCode != STATUS_STACK_OVERFLOW)
    {
        HandleHolder hProcess = OpenProcess(PROCESS_ALL_ACCESS, TRUE, GetCurrentProcessId());
        fOpenProcessFailed = hProcess == NULL;
    }

    if ((pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW) || fOpenProcessFailed)
    {

        WatsonThreadData* pData = new(nothrow) WatsonThreadData(
            pExceptionInfo,
            tore,
            pThread,
            GetCurrentThreadId(),
            FaultReportResultAbort); // default result

        if (pData == NULL)
        {
            return FaultReportResultAbort;
        }
        
        GCX_PREEMP();
    
        if (!g_pDebugInterface || 
            // When GC is in progress and current thread is either a GC thread or a managed 
            // thread under Coop mode, this will let the new generated DoFaultReportCallBack 
            // thread trigger a deadlock. So in this case, we should directly abort the fault 
            // report to avoid the deadlock.
            ((IsGCThread() || pThread->PreemptiveGCDisabled()) && GCHeap::IsGCInProgress()) ||
             FAILED(g_pDebugInterface->RequestFavor(DoFaultReportFavorWorker, pData)))
        {
            // If we can't initialize the debugger helper thread or we are running on the debugger helper
            // thread, return without invoking Watson. We don't have enough stack space.
            
            delete pData;
            return FaultReportResultAbort;
        }

        FaultReportResult ret = pData->result;
        delete pData;
        return ret;
    }

    return DoFaultReportWorker(pExceptionInfo, tore, GetThread(), GetCurrentThreadId());
} // FaultReportResult DoFaultReport()

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
