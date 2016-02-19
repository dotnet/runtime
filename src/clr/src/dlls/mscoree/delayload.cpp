// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// DelayLoad.cpp
//
// This code defines the dealy load helper notification routines that will be
// invoked when a dll marked for delay load is processed.  A DLL is marked as
// delay load by using the DELAYLOAD=foo.dll directive in your sources file.
// This tells the linker to generate helpers for the imports of this dll instead
// of loading it directly.  If your application never touches those functions,
// the the dll is never loaded.  This improves (a) startup time each time the
// app runs, and (b) overall working set size in the case you never use the
// functionality.
//
// 
//
// This module provides a hook helper and exception handler.  The hook helper
// is used primarily in debug mode right now to determine what call stacks
// force a delay load of a dll.  If these call stacks are very common, then
// you should reconsider using a delay load.
//
// The exception handler is used to catch fatal errors like library not found
// or entry point missing.  If this happens you are dead and need to fail
// gracefully.
//
//*****************************************************************************
#include "stdafx.h"                     // Standard header.

#if !defined(FEATURE_CORESYSTEM)

#include "delayimp.h"                   // Delay load header file.
#include "winwrap.h"                    // Wrappers for Win32 api's.
#include "utilcode.h"                   // Debug helpers.
#include "corerror.h"                   // Error codes from this EE.
#include "shimload.h"
#include "ex.h"
#include "strsafe.h"

//********** Locals. **********************************************************
static DWORD _FormatMessage(__out_ecount(chMsg) __out_z LPWSTR szMsg, DWORD chMsg, DWORD dwLastError, ...);
static void _FailLoadLib(unsigned dliNotify, DelayLoadInfo *pdli);
static void _FailGetProc(unsigned dliNotify, DelayLoadInfo *pdli);

#if defined (_DEBUG) || defined (__delay_load_trace__)
static void _DbgPreLoadLibrary(int bBreak,  DelayLoadInfo *pdli);
#endif


//********** Globals. *********************************************************

// Override __pfnDllFailureHook.  This will give the delay code a callback
// for when a load failure occurs.  This failure hook is implemented below.
FARPROC __stdcall CorDelayErrorHook(unsigned dliNotify, DelayLoadInfo *pdli);
ExternC extern PfnDliHook __pfnDliFailureHook = CorDelayErrorHook;

// In trace mode, override the delay load hook.  Our hook does nothing but
// provide some diagnostic information for debugging.
FARPROC __stdcall CorDelayLoadHook(unsigned dliNotify, DelayLoadInfo *pdli);
ExternC extern PfnDliHook __pfnDliNotifyHook = CorDelayLoadHook;


//********** Code. ************************************************************

#undef ExitProcess

extern void DECLSPEC_NORETURN ThrowOutOfMemory();

//*****************************************************************************
// Called for errors that might have occurred.
//*****************************************************************************
FARPROC __stdcall CorDelayErrorHook(    // Always 0.
    unsigned        dliNotify,          // What event has occurred, dli* flag.
    DelayLoadInfo   *pdli)              // Description of the event.
{

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

    // Chose operation to perform based on operation.
    switch (dliNotify)
    {
        // Failed to load the library.  Need to fail gracefully.
        case dliFailLoadLib:
        //_FailLoadLib(dliNotify, pdli);
        break;

        // Failed to get the address of the given function, fail gracefully.
        case dliFailGetProc:
#ifndef FEATURE_CORECLR
        _FailGetProc(dliNotify, pdli);
#endif // !FEATURE_CORECLR
        break;

        // Unknown failure code.
        default:
        _ASSERTE(!"Unknown delay load failure code.");
        break;
    }

#ifndef FEATURE_CORECLR
    if (_stricmp(pdli->szDll, "ole32.dll") == 0)
    {
		// TODO: after interop team fixes delayload related to ole32.dll, we can throw OOM instead.
		// For now, SQL preloads ole32.dll before starting CLR, so OOM for ole32 is not a concern.
		ExitProcess(pdli->dwLastError);
	}
	else
#endif // !FEATURE_CORECLR
#ifdef MSDIS_DLL
    // MSDIS_DLL is a macro defined in SOURCES.INC
    if (_stricmp(pdli->szDll, MSDIS_DLL) == 0)
    {
        // msdisxxx.dll is used in GCStress 4 on chk/dbg builds, if it fails to load then the
        // process will stack-overflow or terminate with no obvious reason of the root cause.
        _ASSERTE(!"Failed to delay load " MSDIS_DLL);
    }
    else
#endif // MSDIS_DLL
	{
#ifndef FEATURE_CORECLR
        // We do not own the process.  ExitProcess is bad.
    	// We will try to recover next time.	
		ThrowWin32 (pdli->dwLastError);
#endif // !FEATURE_CORECLR
	}

    return (0);
}


//*****************************************************************************
// Format an error message using a system error (supplied through GetLastError)
// and any subtitution values required.
//*****************************************************************************
DWORD _FormatMessage(                           // How many characters written.
    __out_ecount(chMsg) __out_z LPWSTR szMsg,   // Buffer for formatted data.
    DWORD       chMsg,                          // How big is the buffer.
    DWORD       dwLastError,                    // The last error code we got.
    ...)                                        // Substitution values.
{
    WRAPPER_NO_CONTRACT;

    DWORD       iRtn;
    va_list     marker;
    
    va_start(marker, dwLastError);
    iRtn = WszFormatMessage(
            FORMAT_MESSAGE_FROM_SYSTEM,                 // Flags.
            0,                                          // No source, use system.
            dwLastError,                                // Error code.
            MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),  // Use default langauge.
            szMsg,                                      // Output buffer.
            dwLastError,                                // Size of buffer.
            &marker);                                   // Substitution text.
    va_end(marker);
    return (iRtn);
}


//*****************************************************************************
// A library failed to load.  This is always a bad thing.
//*****************************************************************************
void _FailLoadLib(
    unsigned        dliNotify,          // What event has occurred, dli* flag.
    DelayLoadInfo   *pdli)              // Description of the event.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    // We're allocating strings for the purposes of putting up a critical error box.
    // Obviously, OOM's aren't going to be passed up to the caller.
    FAULT_NOT_FATAL();


    WCHAR       rcMessage[_MAX_PATH+500]; // Message for display.
    WCHAR       rcFmt[500]; // 500 is the number used by excep.cpp for mscorrc resources.
    HRESULT     hr;

    // Load a detailed error message from the resource file.    
    if (SUCCEEDED(hr = UtilLoadStringRC(MSEE_E_LOADLIBFAILED, rcFmt, NumItems(rcFmt))))
    {
        StringCchPrintf(rcMessage, COUNTOF(rcMessage), rcFmt, pdli->szDll, pdli->dwLastError);
    }
    else
    {
        // Foramt the Windows error first.
        if (!_FormatMessage(rcMessage, NumItems(rcMessage), pdli->dwLastError, pdli->szDll))
        {
            // Default to a hard coded error otherwise.
            StringCchPrintf(rcMessage, COUNTOF(rcMessage), W("ERROR!  Failed to delay load library %hs, Win32 error %d, Delay error: %d\n"), 
                    pdli->szDll, pdli->dwLastError, dliNotify);
        }
    }

#ifndef _ALPHA_
    // for some bizarre reason, calling OutputDebugString during delay load in non-debug mode on Alpha
    // kills program, so only do it when in debug mode ()
#if defined (_DEBUG) || defined (__delay_load_trace__)
    // Give some feedback to the developer.
    wprintf(W("%s\n"), rcMessage);
    WszOutputDebugString(rcMessage);
#endif
#endif

    // Inform the user that we cannot continue execution anymore.
    UtilMessageBoxCatastrophicNonLocalized(rcMessage, W("MSCOREE.DLL"), MB_ICONERROR | MB_OK, TRUE);
    _ASSERTE(!"Failed to delay load library");
}


//*****************************************************************************
// A library failed to load.  This is always a bad thing.
//*****************************************************************************
void _FailGetProc(
    unsigned        dliNotify,          // What event has occurred, dli* flag.
    DelayLoadInfo   *pdli)              // Description of the event.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;

    // We're allocating strings for the purposes of putting up a critical error box.
    // Obviously, OOM's aren't going to be passed up to the caller.
    FAULT_NOT_FATAL();

    WCHAR       rcMessage[_MAX_PATH+756]; // Message for display.
    WCHAR       rcProc[257] = {0};      // Name of procedure with error.
    WCHAR       rcFmt[500]; // 500 is the number used by excep.cpp for mscorrc resources.
    HRESULT     hr;

    // Get a display name for debugging information.
    if (pdli->dlp.fImportByName)
        Wsz_mbstowcs(rcProc, pdli->dlp.szProcName, sizeof(rcProc)/sizeof(rcProc[0])-1);
    else
        StringCchPrintf(rcProc, COUNTOF(rcProc), W("Ordinal: %d"), pdli->dlp.dwOrdinal);

    // Load a detailed error message from the resource file.    
    if (SUCCEEDED(hr = UtilLoadStringRC(MSEE_E_GETPROCFAILED, rcFmt, NumItems(rcFmt))))
    {
        StringCchPrintf(rcMessage, COUNTOF(rcMessage), rcFmt, rcProc, pdli->szDll, pdli->dwLastError);
    }
    else
    {
        if (!_FormatMessage(rcMessage, NumItems(rcMessage), pdli->dwLastError, pdli->szDll))
        {
            // Default to a hard coded error otherwise.
            StringCchPrintf(rcMessage, COUNTOF(rcMessage), W("ERROR!  Failed GetProcAddress() for %s, Win32 error %d, Delay error %d\n"), 
                    rcProc, pdli->dwLastError, dliNotify);
        }
    }

#ifndef ALPHA
    // for some bizarre reason, calling OutputDebugString during delay load in non-debug mode on Alpha
    // kills program, so only do it when in debug mode ()
#if defined (_DEBUG) || defined (__delay_load_trace__)
    // Give some feedback to the developer.
    wprintf(W("%s"),rcMessage);
    WszOutputDebugString(rcMessage);
#endif
#endif

    {
        // We are already in a catastrophic situation so we can tolerate faults as well as SO & GC mode violations to keep going. 
        CONTRACT_VIOLATION(FaultNotFatal | GCViolation | ModeViolation | SOToleranceViolation);

    // Inform the user that we cannot continue execution anymore.
    UtilMessageBoxCatastrophicNonLocalized(rcMessage, W("MSCOREE.DLL"), MB_ICONERROR | MB_OK, TRUE);
    }
    _ASSERTE(!"Failed to delay load GetProcAddress()");
}



HMODULE DoPreloadLibraryThrowing(LPCSTR szLibrary)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_SO_TOLERANT;

    HMODULE result=NULL;
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(ThrowHR(COR_E_STACKOVERFLOW));
    DWORD  dwLength = _MAX_PATH;
    WCHAR  pName[_MAX_PATH];
    IfFailThrow(GetInternalSystemDirectory(pName, &dwLength));
    
    MAKE_WIDEPTR_FROMANSI_NOTHROW(pwLibrary, szLibrary);
    if ((pwLibrary == NULL) || ovadd_ge(dwLength, __lpwLibrary, _MAX_PATH-1))
        ThrowHR(E_INVALIDARG);
    
    wcscpy_s(pName+dwLength-1, COUNTOF(pName) - dwLength + 1, pwLibrary);
    result = CLRLoadLibraryEx(pName, NULL, GetLoadWithAlteredSearchPathFlag());
    END_SO_INTOLERANT_CODE;
    return result;
}

//
//********** Tracing code. ****************************************************
//

//*****************************************************************************
// This routine is our Delay Load Helper.  It will get called for every delay
// load event that occurs while the application is running.
//*****************************************************************************
FARPROC __stdcall CorDelayLoadHook(     // Always 0.
    unsigned        dliNotify,          // What event has occurred, dli* flag.
    DelayLoadInfo   *pdli)              // Description of the event.
{
#ifdef _DEBUG
    if (dliNotify == dliStartProcessing)
    {
        BOOL fThrows = TRUE;
        if (_stricmp(pdli->szDll, "ole32.dll") == 0)
        {
            // SQL loads ole32.dll before starting CLR.  For Whidbey release, 
            // we do not have time to get ole32.dll delay load cleaned.
            fThrows = FALSE;
        }
        else if (_stricmp(pdli->szDll, "oleaut32.dll") == 0)
        {
            extern BOOL DelayLoadOleaut32CheckDisabled();
            if (DelayLoadOleaut32CheckDisabled())
            {
                fThrows = FALSE;
            }
            else if ((!pdli->dlp.fImportByName && pdli->dlp.dwOrdinal == 6) ||
				(pdli->dlp.fImportByName && strcmp(pdli->dlp.szProcName, "SysFreeString") == 0))
            {
                // BSTR has been created, which means oleaut32 should have been loaded.
                // Delay load will not fail.
                _ASSERTE (GetModuleHandleA("oleaut32.dll") != NULL);
                fThrows = FALSE;
            }
        }
        else if (_stricmp(pdli->szDll, "mscoree.dll") == 0) // If we are attempting to delay load mscoree.dll
        {
            if (GetModuleHandleA("mscoree.dll") != NULL) // and mscoree.dll has already been loaded
                fThrows = FALSE; // then the delay load will not fail (and hence will not throw).
        }
        if (fThrows)
        {
            CONTRACTL
            {
				SO_TOLERANT;
                THROWS;
            }
            CONTRACTL_END;
        }
    }
#endif

    //STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;  
    STATIC_CONTRACT_SO_TOLERANT;

    // We're allocating strings for the purposes of putting up a critical error box.
    // Obviously, OOM's aren't going to be passed up to the caller.
    HMODULE result = NULL;
    CONTRACT_VIOLATION(FaultNotFatal);


    switch(dliNotify) {
    case dliNotePreLoadLibrary:
        if(pdli->szDll) {
            result=DoPreloadLibraryThrowing(pdli->szDll);
        }
        break;
    default:
        break;
    }

#if defined (_DEBUG) || defined (__delay_load_trace__)
    SO_NOT_MAINLINE_FUNCTION;
    static int  bBreak = false;         // true to break on events.
    static int  bInit = false;          // true after we've checked environment.
    // If we've not yet looked at our environment, then do so.
    if (!bInit)
    {
        PathString  rcBreak;

        // set DelayLoadBreak=[0|1]
        if (WszGetEnvironmentVariable(W("DelayLoadBreak"), rcBreak))
        {
            // "1" means to break hard and display errors.
            if (rcBreak[0] == '1')
                bBreak = 1;
            // "2" means no break, but display errors.
            else if (rcBreak[0] == '2')
                bBreak = 2;
            else
                bBreak = false;
        }
        bInit = true;
    }

    // Chose operation to perform based on operation.
    switch (dliNotify)
    {
        // Called just before a load library takes place.  Use this opportunity
        // to display a debug trace message, and possible break if desired.
        case dliNotePreLoadLibrary:
        _DbgPreLoadLibrary(bBreak, pdli);
        break;
    }
#endif
    return (FARPROC) result;
}


#if defined (_DEBUG) || defined (__delay_load_trace__)

//*****************************************************************************
// Display a debug message so we know what's going on.  Offer to break in
// debugger if you want to see what call stack forced this library to load.
//*****************************************************************************
void _DbgPreLoadLibrary(
    int         bBreak,                 // true to break in debugger.
    DelayLoadInfo   *pdli)              // Description of the event.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    // We're allocating strings for the purposes of putting up a critical error box.
    // Obviously, OOM's aren't going to be passed up to the caller.
    FAULT_NOT_FATAL();


#ifdef _ALPHA_
    // for some bizarre reason, calling OutputDebugString during delay load in non-debug mode on Alpha
    // kills program, so only do it when in debug mode ()
    if (! IsDebuggerPresent())
        return;
#endif

    WCHAR       rcMessage[_MAX_PATH*2]; // Message for display.

    // Give some feedback to the developer.
    StringCchPrintf(rcMessage, COUNTOF(rcMessage), W("Delay loading %hs\n"), pdli->szDll);
    WszOutputDebugString(rcMessage);

    if (bBreak)
    {
        wprintf(W("%s"), rcMessage);

        if (bBreak == 1)
        {
            _ASSERTE(!"fyi - Delay loading library.  Set DelayLoadBreak=0 to disable this assert.");
        }
    }
}


#endif // _DEBUG

#endif // !FEATURE_CORESYSTEM
