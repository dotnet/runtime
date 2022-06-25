// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DebuggerMessageBox.cpp
//
//*****************************************************************************

#include "stdafx.h"                     // Standard header.
#include <utilcode.h>                   // Utility helpers.
#include <corerror.h>
#include <clrversion.h>
#include "../../dlls/mscorrc/resource.h"

// Output printf-style formatted text to the debugger if it's present or stdout otherwise.
static void DbgPrintf(const LPCSTR szFormat, ...)
{
    char szBuffer[4096];

    va_list args;
    va_start(args, szFormat);

    _vsnprintf_s(szBuffer, ARRAY_SIZE(szBuffer), _TRUNCATE, szFormat, args);

    va_end(args);

    if (IsDebuggerPresent())
    {
        OutputDebugStringUtf8(szBuffer);
    }
    else
    {
        fprintf(stdout, "%s", szBuffer);
        fflush(stdout);
    }
}

typedef int (*MessageBoxWFnPtr)(HWND hWnd,
                                LPCWSTR lpText,
                                LPCWSTR lpCaption,
                                UINT uType);

static int MessageBoxImpl(
                  LPCWSTR title,        // Dialog box title
                  LPCWSTR message,      // Message
                  UINT uType)
{
    CONTRACTL
    {
        INJECT_FAULT(return IDCANCEL;);

        // Assert if none of MB_ICON is set
        PRECONDITION((uType & MB_ICONMASK) != 0);
    }
    CONTRACTL_END;

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_PREEMPTIVE; // we're in umanaged code.

#ifndef HOST_UNIX
    // User32 should exist on all systems where displaying a message box makes sense.
    HMODULE hGuiExtModule = WszLoadLibrary(W("user32"));
    if (hGuiExtModule)
    {
        int result = IDCANCEL;
        MessageBoxWFnPtr fnptr = (MessageBoxWFnPtr)GetProcAddress(hGuiExtModule, "MessageBoxW");
        if (fnptr)
            result = fnptr(NULL, message, title, uType);

        FreeLibrary(hGuiExtModule);
        return result;
    }
#endif // !HOST_UNIX

    // No luck. Output the caption and text to the debugger if present or stdout otherwise.
    if (message == NULL)
        message = W("<null>");
    if (title == NULL)
        title = W("<null>");

    MAKE_UTF8PTR_FROMWIDE_NOTHROW(titleUtf8, title);
    MAKE_UTF8PTR_FROMWIDE_NOTHROW(messageUtf8, message);

    if (titleUtf8 != NULL)
        DbgPrintf("**** '%s' ****\n", titleUtf8);
    if (messageUtf8 != NULL)
        DbgPrintf("  %s", messageUtf8);
    DbgPrintf("\n********\n\n");

    // Indicate to the caller that message box was not actually displayed
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
}

static int UtilMessageBoxNonLocalized(
                  LPCWSTR lpTitle,  // Title
                  LPCWSTR lpText,   // Text message
                  UINT uType)       // Style of MessageBox
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

    EX_TRY
    {
#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
#ifdef HOST_UNIX
        StackSString message;
        message.Printf(W(".NET Runtime version : %s - "), CLR_PRODUCT_VERSION_L);
        message.Append(lpTitle);
        message.Append(lpText);

        ClrReportEvent(W(".NET Runtime"),
            EVENTLOG_ERROR_TYPE,    // event type
            0,                      // category zero
            1024,                   // event identifier
            NULL,                   // no user security identifier
            message.GetUnicode());

        WszOutputDebugString(lpTitle);
        WszOutputDebugString(lpText);
#endif // HOST_UNIX
#endif //!defined(FEATURE_UTILCODE_NO_DEPENDENCIES)

        // This code path is used for ASSERT dialogs.
        result = MessageBoxImpl(lpTitle, lpText, uType);
    }
    EX_CATCH
    {
        result = IDCANCEL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return result;
}

int NotifyUserOfFaultMessageBox(
    LPCWSTR title,       // Title
    LPCWSTR message,     // Text message
    UINT uType)         // Style of MessageBox
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return IDCANCEL;);
    }
    CONTRACTL_END;

    int result = IDCANCEL;

    // Add the MB_TASKMODAL style to indicate that the dialog should be displayed on top of the windows
    // owned by the current thread and should prevent interaction with them until dismissed.
    // Include in the MB_DEFAULT_DESKTOP_ONLY style.
    uType |= (MB_TASKMODAL | MB_DEFAULT_DESKTOP_ONLY);

    EX_TRY
    {
        result = UtilMessageBoxNonLocalized(title, message, uType);
    }
    EX_CATCH
    {
        result = IDCANCEL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return result;
}
