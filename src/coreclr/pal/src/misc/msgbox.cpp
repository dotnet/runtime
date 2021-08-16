// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    msgbox.c

Abstract:

    Implementation of Message Box.



--*/

#include "pal/palinternal.h"
#include "pal/critsect.h"
#include "pal/dbgmsg.h"
#include "pal/misc.h"

#include <syslog.h>

SET_DEFAULT_DEBUG_CHANNEL(MISC);

CRITICAL_SECTION msgbox_critsec;


/*++
Function :
    MsgBoxInitialize

    Initialize the critical sections.

Return value:
    TRUE if initialize succeeded
    FALSE otherwise

--*/
BOOL
MsgBoxInitialize( void )
{
    TRACE( "Initialising the critical section.\n" );
    InternalInitializeCriticalSection(&msgbox_critsec);

    return TRUE;
}

/*++
Function :
    MsgBoxCleanup

    Deletes the critical sections.

--*/
void MsgBoxCleanup( void )
{
    TRACE( "Deleting the critical section.\n" );
    DeleteCriticalSection( &msgbox_critsec );
}



#ifdef __APPLE__
#include "CoreFoundation/CFUserNotification.h"
#include "CoreFoundation/CFString.h"
#include "Security/AuthSession.h"
#endif // __APPLE__


/*++
Function:
  MessageBoxW

This is a small subset of MessageBox that simply logs a message to the
system logging facility and returns. A typical log entry will look
like:

May 23 15:48:10 rice example1: MessageBox: Caption: Error Text

Note:
  hWnd should always be NULL.

See MSDN doc.
--*/
int
PALAPI
MessageBoxW(
	    IN LPVOID hWnd,
	    IN LPCWSTR lpText,
	    IN LPCWSTR lpCaption,
	    IN UINT uType)
{
    CHAR *text = NULL;
    CHAR *caption = NULL;
    INT len = 0;
    INT rc = 0;

    PERF_ENTRY(MessageBoxW);
    ENTRY( "MessageBoxW (hWnd=%p, lpText=%p (%S), lpCaption=%p (%S), uType=%#x)\n",
           hWnd, lpText?lpText:W16_NULLSTRING, lpText?lpText:W16_NULLSTRING,
           lpCaption?lpCaption:W16_NULLSTRING,
           lpCaption?lpCaption:W16_NULLSTRING, uType );

    if (hWnd != NULL)
    {
        ASSERT("hWnd != NULL");
    }

    if(lpText)
    {
        len = WideCharToMultiByte(CP_ACP, 0, lpText, -1, NULL, 0, NULL, NULL);
        if(len)
        {
            text = (LPSTR)PAL_malloc(len);
            if(!text)
            {
                ERROR("malloc() failed!\n");
                SetLastError( ERROR_NOT_ENOUGH_MEMORY );
                goto error;
            }
            if( !WideCharToMultiByte( CP_ACP, 0, lpText, -1, text, len,
                                      NULL, NULL))
            {
                ASSERT("WideCharToMultiByte failure\n");
                SetLastError( ERROR_INTERNAL_ERROR );
                goto error;
            }
        }
        else
        {
            ASSERT("WideCharToMultiByte failure\n");
            SetLastError( ERROR_INTERNAL_ERROR );
            goto error;
        }
    }
    else
    {
        WARN("No message text\n");

        if (NULL == (text = PAL__strdup("(no message text)")))
        {
            ASSERT("strdup() failed\n");
            SetLastError( ERROR_INTERNAL_ERROR );
            goto error;
        }
    }
    if (lpCaption)
    {
        len = WideCharToMultiByte( CP_ACP, 0, lpCaption, -1, NULL, 0,
                                   NULL, NULL);
        if(len)
        {
            caption = (CHAR*)PAL_malloc(len);
            if(!caption)
            {
                ERROR("malloc() failed!\n");
                SetLastError( ERROR_NOT_ENOUGH_MEMORY );
                goto error;
            }
            if( !WideCharToMultiByte( CP_ACP, 0, lpCaption, -1, caption, len,
                                      NULL, NULL))
            {
                ASSERT("WideCharToMultiByte failure\n");
                SetLastError( ERROR_INTERNAL_ERROR );
                goto error;
            }
        }
        else
        {
            ASSERT("WideCharToMultiByte failure\n");
            SetLastError( ERROR_INTERNAL_ERROR );
            goto error;
        }
    }
    else
    {
        if (NULL == (caption = PAL__strdup("Error")))
        {
            ERROR("strdup() failed\n");
            SetLastError( ERROR_NOT_ENOUGH_MEMORY );
            goto error;
        }
    }

    rc = MessageBoxA(hWnd, text, caption, uType);

error:
    PAL_free(caption);
    PAL_free(text);


    LOGEXIT("MessageBoxW returns %d\n", rc);
    PERF_EXIT(MessageBoxW);
    return rc;
}


/*++
Function:
  MessageBoxA

This is a small subset of MessageBox that simply logs a message to the
system logging facility and returns. A typical log entry will look
like:

May 23 15:48:10 rice example1: MessageBox: Caption: Error Text

Note:
  hWnd should always be NULL.

See MSDN doc.
--*/
int
PALAPI
MessageBoxA(
	    IN LPVOID hWnd,
	    IN LPCSTR lpText,
	    IN LPCSTR lpCaption,
	    IN UINT uType)
{
    INT rc = 0;

    PERF_ENTRY(MessageBoxA);
    ENTRY( "MessageBoxA (hWnd=%p, lpText=%p (%s), lpCaption=%p (%s), uType=%#x)\n",
           hWnd, lpText?lpText:"NULL", lpText?lpText:"NULL",
           lpCaption?lpCaption:"NULL",
           lpCaption?lpCaption:"NULL", uType );

    if (hWnd != NULL)
    {
        ASSERT("hWnd != NULL");
    }

    if (lpText == NULL)
    {
        WARN("No message text\n");

        lpText = "(no message text)";
    }

    if (lpCaption == NULL)
    {
        lpCaption = "Error";
    }

    if (uType & MB_DEFMASK)
    {
        WARN("No support for alternate default buttons.\n");
    }

    /* set default status based on the type of button */
    switch(uType & MB_TYPEMASK)
    {
    case MB_OK:
        rc = IDOK;
        break;

    case MB_ABORTRETRYIGNORE:
        rc = IDABORT;
        break;

    case MB_YESNO:
        rc = IDNO;
        break;

    case MB_OKCANCEL :
        rc = IDCANCEL;
        break;

    case MB_RETRYCANCEL :
        rc = IDCANCEL;
        break;

    default:
        ASSERT("Bad uType");
        rc = IDOK;
        break;
    }

    PALCEnterCriticalSection( &msgbox_critsec);

#ifdef __APPLE__
    OSStatus osstatus;

    SecuritySessionId secSession;
    SessionAttributeBits secSessionInfo;

    osstatus = SessionGetInfo(callerSecuritySession, &secSession, &secSessionInfo);
    if (noErr == osstatus && (secSessionInfo & sessionHasGraphicAccess) != 0)
    {
        CFStringRef cfsTitle = CFStringCreateWithCString(kCFAllocatorDefault, lpCaption, kCFStringEncodingUTF8);
        CFStringRef cfsText = CFStringCreateWithCString(kCFAllocatorDefault, lpText, kCFStringEncodingUTF8);
        CFStringRef cfsButton1 = NULL;
        CFStringRef cfsButton2 = NULL;
        CFStringRef cfsButton3 = NULL;
        CFOptionFlags alertFlags = 0;
        CFOptionFlags response;

        switch (uType & MB_TYPEMASK)
        {
        case MB_OK:
            // Nothing needed; since if all the buttons are null, a stock "OK" is used.
            break;

        case MB_ABORTRETRYIGNORE:
            // Localization? Would be needed if this were used outside of debugging.
            cfsButton1 = CFSTR("Abort");
            cfsButton2 = CFSTR("Retry");
            cfsButton3 = CFSTR("Ignore");
            alertFlags = kCFUserNotificationCautionAlertLevel;
            break;

        case MB_YESNO:
            cfsButton1 = CFSTR("Yes");
            cfsButton2 = CFSTR("No");
            break;

        case MB_OKCANCEL:
            cfsButton1 = CFSTR("OK");
            cfsButton2 = CFSTR("Cancel");
            break;

        case MB_RETRYCANCEL:
            cfsButton1 = CFSTR("Retry");
            cfsButton2 = CFSTR("Cancel");
            break;
        }

        CFUserNotificationDisplayAlert(0 /* no time out */, alertFlags, NULL /* iconURL */,
            NULL /* soundURL */, NULL /* localizationURL */, cfsTitle, cfsText, cfsButton1,
            cfsButton2, cfsButton3, &response);

        switch (uType & MB_TYPEMASK)
        {
        case MB_OK:
            break;

        case MB_ABORTRETRYIGNORE:
            switch (response)
            {
            case kCFUserNotificationDefaultResponse:
                rc = IDABORT;
                break;
            case kCFUserNotificationAlternateResponse:
                rc = IDRETRY;
                break;
            case kCFUserNotificationOtherResponse:
                rc = IDIGNORE;
                break;
            }
            break;

        case MB_YESNO:
            switch (response)
            {
            case kCFUserNotificationDefaultResponse:
                rc = IDYES;
                break;
            case kCFUserNotificationAlternateResponse:
                rc = IDNO;
                break;
            }
            break;

        case MB_OKCANCEL:
            switch (response)
            {
            case kCFUserNotificationDefaultResponse:
                rc = IDOK;
                break;
            case kCFUserNotificationAlternateResponse:
                rc = IDCANCEL;
                break;
            }
            break;

        case MB_RETRYCANCEL:
            switch (response)
            {
            case kCFUserNotificationDefaultResponse:
                rc = IDRETRY;
                break;
            case kCFUserNotificationAlternateResponse:
                rc = IDCANCEL;
                break;
            }
            break;
        }
    }
    else
    {
        // We're not in a login session, e.g., running via ssh, and so bringing
        // up a message box would be bad form.
        fprintf ( stderr, "MessageBox: %s: %s", lpCaption, lpText );
        syslog(LOG_USER|LOG_ERR, "MessageBox: %s: %s", lpCaption, lpText);
    }
#else // __APPLE__
    fprintf ( stderr, "MessageBox: %s: %s", lpCaption, lpText );
    syslog(LOG_USER|LOG_ERR, "MessageBox: %s: %s", lpCaption, lpText);

    // Some systems support displaying a GUI dialog. (This will suspend the current thread until they hit the
    // 'OK' button and allow a debugger to be attached).
    PAL_DisplayDialog(lpCaption, lpText);
#endif // __APPLE__ else

    PALCLeaveCriticalSection( &msgbox_critsec);

    LOGEXIT("MessageBoxA returns %d\n", rc);
    PERF_EXIT(MessageBoxA);
    return rc;
}
