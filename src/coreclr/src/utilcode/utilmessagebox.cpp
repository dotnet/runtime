// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// UtilMessageBox.cpp
//

//
// This module contains the message box utility code for the CLR. It is used
// by code in the CLR itself as well as other tools that build in the CLR tree.
// For message boxes inside the ExecutionEngine, EEMessageBox must be used
// instead of the these APIs.
//
//*****************************************************************************
#include "stdafx.h"                     // Standard header.
#include <utilcode.h>                   // Utility helpers.
#include <corerror.h>
#include "ndpversion.h"
#include "../dlls/mscorrc/resource.h"
#include "ex.h"
#if !defined(FEATURE_CORESYSTEM)
#undef NTDDI_VERSION
#define NTDDI_VERSION NTDDI_WIN7
#include "commctrl.h"
#endif

#if !defined(SELF_NO_HOST) && !defined(FEATURE_CORECLR)

//
// This should be used for runtime dialog box, because we assume the resource is from mscorrc.dll
// For tools like ildasm or Shim which uses their own resource file, you need to define IDS_RTL in 
// their resource file and define a function like this and append the style returned from the function 
// to every calls to WszMessageBox.
//
UINT GetCLRMBRTLStyle() 
{
    WRAPPER_NO_CONTRACT;

    UINT mbStyle = 0;
    WCHAR buff[MAX_LONGPATH];                        
    if(SUCCEEDED(UtilLoadStringRC(IDS_RTL, buff, MAX_LONGPATH, true))) {
        if(wcscmp(buff, W("RTL_True")) == 0) {
            mbStyle = 0x00080000 |0x00100000; // MB_RIGHT || MB_RTLREADING
        }
    }
    return mbStyle;
}

#endif //!defined(SELF_NO_HOST) && !defined(FEATURE_CORECLR)

BOOL ShouldDisplayMsgBoxOnCriticalFailure()
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    // To help find issues, we will always display dialogs for critical failures
    // under debug builds. This includes asserts and other critical issues.
   return TRUE;
#else      
    // Retrieve error mode
    UINT last = SetErrorMode(0);
    SetErrorMode(last);         //set back to previous value
                    
    // SEM_FAILCRITICALERRORS indicates that the system does not display the critical-error-handler 
    // message box. Instead, the system sends the error to the calling process.
    return !(last & SEM_FAILCRITICALERRORS);
#endif // _DEBUG
}


#if !defined(FEATURE_CORESYSTEM) && !defined(FEATURE_CORECLR)
enum ProbedTaskDialogIndirectState
{
    ProbedTaskDialogIndirectState_NotProbed = 0,
    ProbedTaskDialogIndirectState_NotAvailable = 1,
    ProbedTaskDialogIndirectState_Available = 2
};

static ProbedTaskDialogIndirectState siProbedTaskDialogIndirect = ProbedTaskDialogIndirectState_NotProbed;
#endif // !FEATURE_CORESYSTEM && !FEATURE_CORECLR


// We'd like to use TaskDialogIndirect for asserts coming from managed code in particular
// to display the detailedText in a scrollable way.  Also, we'd like to reuse the CLR's
// plumbing code for the rest of parts of the assert dialog.  Note that the simple
// Win32 MessageBox does not support the detailedText value.
// If we later refactor MessageBoxImpl into its own DLL, move the lines referencing
// "Microsoft.Windows.Common-Controls" version 6 in stdafx.h as well.  
int MessageBoxImpl(
                  HWND hWnd,            // Handle to Owner Window
                  LPCWSTR message,      // Message
                  LPCWSTR title,        // Dialog box title
                  LPCWSTR detailedText, // Details like a stack trace, etc.
                  UINT uType)
{
    CONTRACTL
    {
        // May pump messages.  Callers should be GC_TRIGGERS and MODE_PREEMPTIVE,
        // but we can't include EE contracts here.
        THROWS;
        INJECT_FAULT(return IDCANCEL;);

        // Assert if none of MB_ICON is set
        PRECONDITION((uType & MB_ICONMASK) != 0);
    }
    CONTRACTL_END;

#if defined(FEATURE_CORESYSTEM) || defined (FEATURE_CORECLR)
    return WszMessageBox(hWnd, message, title, uType);
#else
    bool mustUseMessageBox = false;  // Mac, Silverlight, pre-Vista?  Do we support this type of message box?
    decltype(TaskDialogIndirect)* pfnTaskDialogIndirect = NULL;
    ULONG_PTR cookie = NULL;  // For activation context.
    bool activatedActivationContext = false;
    HModuleHolder hmodComctl32;
    HANDLE hActCtx = INVALID_HANDLE_VALUE;

    // Note: TaskDialogIndirect is only in the v6 and above versions of comctl32.  Windows
    // stores that library in the WinSxS directory in a directory with 
    // "Microsoft.Windows.Common-Controls" in the name.  Your application can only see
    // this library if the linker has added a manifest dependency on the V6 common controls
    // to your application.  Or, you can create an activation context to make this work,
    // if your library also has the appropriate manifest dependency.
    // Also, I'm not going to leave comctl32.dll mapped, to ensure it can't somehow 
    // interfere with older versions.  Therefore, re-load comctl32.dll every time through
    // this method.  We will record whether TaskDialogIndirect is available though, so
    // we can fall back to MessageBox faster.

    // We don't yet have a perfect mapping from all MessageBox behavior to TaskDialogIndirect behavior.
    // Use MessageBox to avoid most of this complexity.
    if (((uType & MB_ICONMASK) != MB_ICONWARNING) && (uType & MB_ICONMASK) != MB_ICONERROR  ||
        (uType & MB_TYPEMASK) != MB_ABORTRETRYIGNORE ||
        (uType & MB_DEFMASK) != 0 ||
        (uType & MB_MODEMASK) != 0 ||
        (uType & MB_MISCMASK) != 0)
        mustUseMessageBox = true;
    else if (mustUseMessageBox || siProbedTaskDialogIndirect == ProbedTaskDialogIndirectState_NotAvailable)
        mustUseMessageBox = true;
    else {
        // Replace our application's ActivationContext temporarily, load comctl32
        // & look for TaskDialogIndirect.  Don't cache pointer.
        // The following code was suggested by some Windows experts.  We do not want
		// to add a manifest to our library saying we use comctl32 v6, because that
		// will mean loading a lot of extra libraries on startup (a significant perf hit).
		// We could either store the manifest as a resource, or more creatively since
		// we are effectively a Windows component, rely on %windir%\WindowsShell.manifest.
        ACTCTX ctx = { sizeof(ACTCTX) };
        ctx.dwFlags = 0;
        StackSString manifestPath;  // Point this at %windir%\WindowsShell.manifest, for comctl32 version 6.
        UINT numChars = WszGetWindowsDirectory(manifestPath.OpenUnicodeBuffer(MAX_PATH_FNAME), MAX_PATH_FNAME);
        if (numChars == 0 || numChars >= MAX_PATH_FNAME)
        {
            _ASSERTE(0);  // How did this fail?
        }
        else {
            manifestPath.CloseBuffer(numChars);
            if (manifestPath[manifestPath.GetCount() - 1] != W('\\'))
                manifestPath.Append(W('\\'));
            manifestPath.Append(W("WindowsShell.manifest"));  // Other Windows components have already loaded this.
            ctx.lpSource = manifestPath.GetUnicode();
            hActCtx = CreateActCtx(&ctx);
            if (hActCtx != INVALID_HANDLE_VALUE)
            {           
                if (!ActivateActCtx(hActCtx, &cookie))
                {
                    cookie = NULL;
                    _ASSERTE(0);  // Why did ActivateActCtx fail?  (We'll continue executing & cope with the failure.)
                }
                else {
                    activatedActivationContext = true;
                    // Activation context was replaced - now we can load comctl32 version 6.
                    hmodComctl32 = WszLoadLibrary(W("comctl32.dll"));

                    if (hmodComctl32 != INVALID_HANDLE_VALUE) {
                        pfnTaskDialogIndirect = (decltype(TaskDialogIndirect)*)GetProcAddress(hmodComctl32, "TaskDialogIndirect");
                        if (pfnTaskDialogIndirect == NULL) {
                            hmodComctl32.Release();
                        }
                    }
                }
            }
        }

        siProbedTaskDialogIndirect = (pfnTaskDialogIndirect == NULL) ? ProbedTaskDialogIndirectState_NotAvailable : ProbedTaskDialogIndirectState_Available;
        mustUseMessageBox = (pfnTaskDialogIndirect == NULL);
    }

    int result = MB_OK;
    if (mustUseMessageBox) {
        result = WszMessageBox(hWnd, message, title, uType);
    }
    else {
        _ASSERTE(pfnTaskDialogIndirect != NULL);
        int nButtonPressed                  = 0;
        TASKDIALOGCONFIG config             = {0};
        config.cbSize                       = sizeof(config);
        config.hwndParent                   = hWnd;
        config.dwCommonButtons              = 0;
        config.pszWindowTitle               = title;
        config.dwFlags                      = (uType & MB_RTLREADING) ? TDF_RTL_LAYOUT : 0;

        // Set the user-visible icon in the window.
        _ASSERTE(((uType & MB_ICONMASK) == MB_ICONWARNING) || ((uType & MB_ICONMASK) == MB_ICONERROR));
        config.pszMainIcon                  = ((uType & MB_ICONMASK) == MB_ICONWARNING) ? TD_WARNING_ICON : TD_ERROR_ICON;

        config.pszMainInstruction           = title;
        config.pszContent                   = message;
        config.pszExpandedInformation       = detailedText;

        // Set up the buttons
        // Note about button hot keys: Windows keeps track of of where the last input came from
        // (ie, mouse or keyboard).  If you use the mouse to interact w/ one dialog box and then use
        // the keyboard, the next dialog will not include hot keys.  This is a Windows feature to
        // minimize clutter on the screen for mouse users.
        _ASSERTE((uType & MB_TYPEMASK) == MB_ABORTRETRYIGNORE);
        StackSString abortLabel, debugLabel, ignoreLabel;
        const WCHAR *pAbortLabel, *pDebugLabel, *pIgnoreLabel;

        if (abortLabel.LoadResource(CCompRC::Optional, IDS_DIALOG_BOX_ABORT_BUTTON))
            pAbortLabel = abortLabel.GetUnicode();
        else
            pAbortLabel = W("&Abort");
        if (debugLabel.LoadResource(CCompRC::Optional, IDS_DIALOG_BOX_DEBUG_BUTTON))
            pDebugLabel = debugLabel.GetUnicode();
        else
            pDebugLabel = W("&Debug");
        if (ignoreLabel.LoadResource(CCompRC::Optional, IDS_DIALOG_BOX_IGNORE_BUTTON))
            pIgnoreLabel = ignoreLabel.GetUnicode();
        else
            pIgnoreLabel = W("&Ignore");

        const TASKDIALOG_BUTTON abortDebugIgnoreButtons[] = { 
            { IDOK, pAbortLabel },
            { IDRETRY, pDebugLabel },
            { IDIGNORE, pIgnoreLabel }
        };
        config.pButtons = abortDebugIgnoreButtons;
        config.cButtons = 3;

        HRESULT hr = pfnTaskDialogIndirect(&config, &nButtonPressed, NULL, NULL);
        _ASSERTE(hr == S_OK);
        if (hr == S_OK) {
            result = nButtonPressed;
        }
        else {
            result = IDOK;
        }

        _ASSERTE(result == IDOK || result == IDRETRY || result == IDIGNORE);
    }

    if (activatedActivationContext) {
        DeactivateActCtx(0, cookie);
        ReleaseActCtx(hActCtx);  // perf isn't important so we won't bother caching the actctx
    }
 
    return result;
#endif
}

int UtilMessageBoxVA(
                  HWND hWnd,        // Handle to Owner Window
                  UINT uText,       // Resource Identifier for Text message
                  UINT uTitle,      // Resource Identifier for Title
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive 
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  va_list args)     // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return IDCANCEL;);
    }
    CONTRACTL_END;

    SString text;
    SString title; 
    int result = IDCANCEL;
    
    EX_TRY
    {
        text.LoadResource(CCompRC::Error, uText);
        title.LoadResource(CCompRC::Error, uTitle);

        result = UtilMessageBoxNonLocalizedVA(hWnd, (LPWSTR)text.GetUnicode(), 
            (LPWSTR)title.GetUnicode(), uType, displayForNonInteractive, showFileNameInTitle, NULL, args);
    }
    EX_CATCH
    {
        result = IDCANCEL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return result;            
}

int UtilMessageBoxNonLocalizedVA(
                  HWND hWnd,        // Handle to Owner Window
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Title
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive 
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  BOOL * pInputFromUser,            // To distinguish between user pressing abort vs. assuming abort.
                  va_list args)     // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return IDCANCEL;);

        // Assert if none of MB_ICON is set
        PRECONDITION((uType & MB_ICONMASK) != 0);
    }
    CONTRACTL_END;

    return UtilMessageBoxNonLocalizedVA(hWnd, lpText, lpTitle, NULL, uType, displayForNonInteractive, showFileNameInTitle, pInputFromUser, args);
}

int UtilMessageBoxNonLocalizedVA(
                  HWND hWnd,        // Handle to Owner Window
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Title
                  LPCWSTR lpDetails,// Details like a stack trace, etc.
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive 
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  BOOL * pInputFromUser,            // To distinguish between user pressing abort vs. assuming abort.
                  va_list args)     // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return IDCANCEL;);

        // Assert if none of MB_ICON is set
        PRECONDITION((uType & MB_ICONMASK) != 0);
    }
    CONTRACTL_END;

    int result = IDCANCEL;
	if (pInputFromUser != NULL)
	{
        *pInputFromUser = FALSE;
	}

    EX_TRY
    {   
        StackSString formattedMessage;
        StackSString formattedTitle;
        SString details(lpDetails);
        PathString fileName;
        BOOL fDisplayMsgBox = TRUE;
        
        // Format message string using optional parameters
        formattedMessage.VPrintf(lpText, args);
       
        // Try to get filename of Module and add it to title
        if (showFileNameInTitle && WszGetModuleFileName(NULL, fileName))
        {           
            LPCWSTR wszName = NULL;
            size_t cchName = 0;

                  
            
            SplitPathInterior(fileName, NULL, NULL, NULL, NULL, &wszName, &cchName, NULL, NULL);
            formattedTitle.Printf(W("%s - %s"), wszName, lpTitle);
        }
        else
        {
            formattedTitle.Set(lpTitle);
        }

#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
        // If the current process isn't interactive (a service for example), then we report the message 
        // in the event log and via OutputDebugString. 
        // 
        // We may still however attempt to display the message box if the MB_SERVICE_NOTIFICATION
        // message box style was specified.
        if (!RunningInteractive())
        {
            HANDLE h;
            StackSString message;

            message.Printf(W(".NET Runtime version : %s - "), VER_FILEVERSION_STR_L);
            if (lpTitle)
                message.Append(lpTitle);
            if (!formattedMessage.IsEmpty())
                message.Append(formattedMessage);

            ClrReportEvent(W(".NET Runtime"),
                EVENTLOG_ERROR_TYPE,    // event type 
                0,                      // category zero
                1024,                   // event identifier
                NULL,                   // no user security identifier
                message.GetUnicode());
            
            if(lpTitle != NULL)
                WszOutputDebugString(lpTitle);
            if(!formattedMessage.IsEmpty())
                WszOutputDebugString(formattedMessage);

            // If we are running as a service and displayForNonInteractive is FALSE then IDABORT is 
            // the best value to return as it will most likely cause callers of this API to abort the process. 
            // This is the right thing to do since attaching a debugger doesn't make much sense when the process isn't
            // running in interactive mode.
            if(!displayForNonInteractive)   
            {
                fDisplayMsgBox = FALSE;
                result = IDABORT;
            }
            else
            {
                // Include in the MB_DEFAULT_DESKTOP_ONLY style.
                uType |= MB_DEFAULT_DESKTOP_ONLY;                            
            }
        }
#endif //!defined(FEATURE_UTILCODE_NO_DEPENDENCIES)

        if (fDisplayMsgBox)
        {
            // We normally want to set the reading direction (right-to-left etc.) based on the resources
            // in use.  However, outside the CLR (SELF_NO_HOST) we can't assume we have resources and
            // in CORECLR we can't even necessarily expect that our CLR callbacks have been initialized.
            // This code path is used for ASSERT dialogs.
#if !defined(SELF_NO_HOST) && !defined(FEATURE_CORECLR)
            uType |= GetCLRMBRTLStyle();
#endif
            
            result = MessageBoxImpl(hWnd, formattedMessage, formattedTitle, details, uType);
            
            if (pInputFromUser != NULL)
            {
                *pInputFromUser = TRUE;
            }
        }
    }        
    EX_CATCH
    {
        result = IDCANCEL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return result;
}

int UtilMessageBox(
                  HWND hWnd,        // Handle to Owner Window
                  UINT uText,       // Resource Identifier for Text message
                  UINT uTitle,      // Resource Identifier for Title
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive 
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  ...)              // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    va_list marker;
    va_start(marker, showFileNameInTitle);

    int result = UtilMessageBoxVA(hWnd, uText, uTitle, uType, displayForNonInteractive, showFileNameInTitle, marker);
    va_end( marker );

    return result;    
}

int UtilMessageBoxNonLocalized(
                  HWND hWnd,        // Handle to Owner Window
                  LPCWSTR lpText,   // Text message
                  LPCWSTR lpTitle,  // Title message
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive 
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  ... )             // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    va_list marker;
    va_start(marker, showFileNameInTitle);

    int result = UtilMessageBoxNonLocalizedVA(
        hWnd, lpText, lpTitle, uType, displayForNonInteractive, showFileNameInTitle, NULL, marker);
    va_end( marker );

    return result;
}

int UtilMessageBoxCatastrophic(
                  UINT uText,       // Text for MessageBox
                  UINT uTitle,      // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  ...)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    va_list marker;
    va_start(marker, showFileNameInTitle);

    int result = UtilMessageBoxCatastrophicVA(uText, uTitle, uType, showFileNameInTitle, marker);
    va_end( marker );

    return result;
}

int UtilMessageBoxCatastrophicNonLocalized(
                  LPCWSTR lpText,    // Text for MessageBox
                  LPCWSTR lpTitle,   // Title for MessageBox
                  UINT uType,        // Style of MessageBox
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  ...)
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    va_list marker;
    va_start(marker, showFileNameInTitle);

    int result = UtilMessageBoxCatastrophicNonLocalizedVA(lpText, lpTitle, uType, showFileNameInTitle, marker);
    va_end( marker );

    return result;
}

int UtilMessageBoxCatastrophicVA(
                  UINT uText,       // Text for MessageBox
                  UINT uTitle,      // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  va_list args)     // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HWND hwnd = NULL;

    // We are already in a catastrophic situation so we can tolerate faults as well as SO & GC mode violations to keep going. 
    CONTRACT_VIOLATION(FaultNotFatal | GCViolation | ModeViolation | SOToleranceViolation);

    if (!ShouldDisplayMsgBoxOnCriticalFailure())
        return IDABORT;

    // Add the MB_TASKMODAL style to indicate that the dialog should be displayed on top of the windows
    // owned by the current thread and should prevent interaction with them until dismissed.
    uType |= MB_TASKMODAL;

    return UtilMessageBoxVA(hwnd, uText, uTitle, uType, TRUE, showFileNameInTitle, args);
}

int UtilMessageBoxCatastrophicNonLocalizedVA(
                  LPCWSTR lpText,   // Text for MessageBox
                  LPCWSTR lpTitle,  // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL showFileNameInTitle, // Flag to show FileName in Caption
                  va_list args)     // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HWND hwnd = NULL;

    // We are already in a catastrophic situation so we can tolerate faults as well as SO & GC mode violations to keep going. 
    CONTRACT_VIOLATION(FaultNotFatal | GCViolation | ModeViolation | SOToleranceViolation);

    if (!ShouldDisplayMsgBoxOnCriticalFailure())
        return IDABORT;

    // Add the MB_TASKMODAL style to indicate that the dialog should be displayed on top of the windows
    // owned by the current thread and should prevent interaction with them until dismissed.
    uType |= MB_TASKMODAL;

    return UtilMessageBoxNonLocalizedVA(hwnd, lpText, lpTitle, uType, TRUE, showFileNameInTitle, NULL, args);
}

