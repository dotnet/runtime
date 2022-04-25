// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "clrversion.h"
#include "../dlls/mscorrc/resource.h"
#include "ex.h"

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

// Output printf-style formatted text to the debugger if it's present or stdout otherwise.
static void DbgWPrintf(const LPCWSTR wszFormat, ...)
{
    WCHAR wszBuffer[4096];

    va_list args;
    va_start(args, wszFormat);

    _vsnwprintf_s(wszBuffer, sizeof(wszBuffer) / sizeof(WCHAR), _TRUNCATE, wszFormat, args);

    va_end(args);

    if (IsDebuggerPresent())
    {
        OutputDebugStringW(wszBuffer);
    }
    else
    {
        fwprintf(stdout, W("%s"), wszBuffer);
        fflush(stdout);
    }
}

typedef int (*MessageBoxWFnPtr)(HWND hWnd,
                                LPCWSTR lpText,
                                LPCWSTR lpCaption,
                                UINT uType);

// We'd like to use TaskDialogIndirect for asserts coming from managed code in particular
// to display the detailedText in a scrollable way.  Also, we'd like to reuse the CLR's
// plumbing code for the rest of parts of the assert dialog.  Note that the simple
// Win32 MessageBox does not support the detailedText value.
// If we later refactor MessageBoxImpl into its own DLL, move the lines referencing
// "Microsoft.Windows.Common-Controls" version 6 in stdafx.h as well.
static int MessageBoxImpl(
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

#ifndef HOST_UNIX
    // User32 should exist on all systems where displaying a message box makes sense.
    HMODULE hGuiExtModule = WszLoadLibrary(W("user32"));
    if (hGuiExtModule)
    {
        int result = IDCANCEL;
        MessageBoxWFnPtr fnptr = (MessageBoxWFnPtr)GetProcAddress(hGuiExtModule, "MessageBoxW");
        if (fnptr)
            result = fnptr(hWnd, message, title, uType);

        FreeLibrary(hGuiExtModule);
        return result;
    }
#endif // !HOST_UNIX

    // No luck. Output the caption and text to the debugger if present or stdout otherwise.
    if (message == NULL)
        message = W("<null>");
    if (title == NULL)
        title = W("<null>");
    DbgWPrintf(W("**** '%s' ****\n"), title);
    DbgWPrintf(W("  %s\n"), message);
    DbgWPrintf(W("********\n"));
    DbgWPrintf(W("\n"));

    // Indicate to the caller that message box was not actually displayed
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
}

static int UtilMessageBoxNonLocalizedVA(
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
#ifdef HOST_UNIX
        // For non-interactive processes, we report the message in the event log and via OutputDebugString.
        //
        // We may still however attempt to display the message box if the MB_SERVICE_NOTIFICATION
        // message box style was specified.
        StackSString message;

        message.Printf(W(".NET Runtime version : %s - "), CLR_PRODUCT_VERSION_L);
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
#endif // HOST_UNIX
#endif //!defined(FEATURE_UTILCODE_NO_DEPENDENCIES)

        if (fDisplayMsgBox)
        {
            // We normally want to set the reading direction (right-to-left etc.) based on the resources
            // in use.  However, outside the CLR (SELF_NO_HOST) we can't assume we have resources and
            // in CORECLR we can't even necessarily expect that our CLR callbacks have been initialized.
            // This code path is used for ASSERT dialogs.

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
            (LPWSTR)title.GetUnicode(), NULL, uType, displayForNonInteractive, showFileNameInTitle, NULL, args);
    }
    EX_CATCH
    {
        result = IDCANCEL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return result;
}

int UtilMessageBoxCatastrophic(
                  UINT uText,       // Text for MessageBox
                  UINT uTitle,      // Title for MessageBox
                  UINT uType,       // Style of MessageBox
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  ...)     // Additional Arguments
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    HWND hwnd = NULL;

    // We are already in a catastrophic situation so we can tolerate faults as well as GC mode violations to keep going.
    CONTRACT_VIOLATION(FaultNotFatal | GCViolation | ModeViolation);

    if (!ShouldDisplayMsgBoxOnCriticalFailure())
        return IDABORT;

    // Add the MB_TASKMODAL style to indicate that the dialog should be displayed on top of the windows
    // owned by the current thread and should prevent interaction with them until dismissed.
    uType |= MB_TASKMODAL;


    va_list args;
    va_start(args, showFileNameInTitle);

    int result = UtilMessageBoxVA(hwnd, uText, uTitle, uType, TRUE, showFileNameInTitle, args);

    va_end(args);

    return result;
}
