//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    console.cpp

Abstract:

    Implementation of console ctrl API functions.



--*/

#include "pal/thread.hpp"
#include "pal/dbgmsg.h"
#include "pal/malloc.hpp"
#include "pal/process.h"
#include <errno.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT);

/* Constant and type definitions **********************************************/

typedef struct CTRL_HANDLER_LIST
{
    PHANDLER_ROUTINE handler;
    struct CTRL_HANDLER_LIST *next;
} CTRL_HANDLER_LIST;

/* Static variables ***********************************************************/

/* for manipulating process control-handler list, etc */
CRITICAL_SECTION exception_critsec;

static CTRL_HANDLER_LIST * pCtrlHandler;
static int nCtrlHandlerListLength;

/* Internal function definitions **********************************************/

/*++
Function :
    SEHInitializeConsole

    Initialize stuff related to console ctrl events

    (no parameters)

Return value :
    TRUE  if initialization succeeded
    FALSE otherwise
--*/
BOOL SEHInitializeConsole()
{
    pCtrlHandler = NULL;
    nCtrlHandlerListLength = 0;
    InternalInitializeCriticalSection(&exception_critsec);
    return TRUE;
}

/* PAL function definitions ***************************************************/

/*++
Function:
  SetConsoleCtrlHandler

See MSDN doc.

--*/
BOOL
PALAPI
SetConsoleCtrlHandler(
              IN PHANDLER_ROUTINE HandlerRoutine,
              IN BOOL Add)
{
    BOOL retval = FALSE;
    CTRL_HANDLER_LIST *handler;
    CPalThread * pThread;

    PERF_ENTRY(SetConsoleCtrlHandler);
    ENTRY("SetConsoleCtrlHandler(HandlerRoutine=%p, Add=%d)\n",
          HandlerRoutine, Add);

    pThread = InternalGetCurrentThread();
    InternalEnterCriticalSection(pThread, &exception_critsec);
    
    if(NULL == HandlerRoutine)
    {
        ASSERT("HandlerRoutine may not be NULL, control-c-ignoration is not "
               "supported\n");
        goto done;
    }

    if(Add)
    {
        handler = (CTRL_HANDLER_LIST *)InternalMalloc(pThread, sizeof(CTRL_HANDLER_LIST));
        if(!handler)
        {
            ERROR("PAL_malloc failed! error is %d (%s)\n", errno, strerror(errno));
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }
        handler->handler = HandlerRoutine;
        /* From MSDN : 
           "handler functions are called on a last-registered, first-called 
           basis". So we can add the new handler at the head of the list. */
        handler->next = pCtrlHandler;
        pCtrlHandler = handler;
        nCtrlHandlerListLength++;

        TRACE("Adding Control Handler %p\n", HandlerRoutine);
        retval = TRUE;
    }
    else
    {
        CTRL_HANDLER_LIST *temp_handler;

        handler = pCtrlHandler;
        temp_handler = handler;
        while(handler)
        {
            if(handler->handler == HandlerRoutine)
            {
                break;
            }
            temp_handler = handler;
            handler = handler->next;
        }
        if(handler)
        {
            /* temp_handler it the item before the one to remove, unless it was
               first in the list, in which case handler == temp_handler */
            if(handler == temp_handler)
            {
                /* handler to remove was first in the list... */
                nCtrlHandlerListLength--;
                pCtrlHandler = handler->next;

                InternalFree(pThread, handler);
                TRACE("Removing Control Handler %p from head of list\n", 
                      HandlerRoutine );
            }
            else
            {
                /* handler was not first in the list... */
                nCtrlHandlerListLength--;
                temp_handler->next = handler->next;
                InternalFree(pThread, handler);
                TRACE("Removing Control Handler %p (not head of list)\n", 
                      HandlerRoutine );                 
            }                 
            retval = TRUE;
        }
        else
        {
            WARN("Trying to remove unknown Control Handler %p\n", 
                 HandlerRoutine);
            SetLastError(ERROR_INVALID_PARAMETER);
        }
    }
done:
    InternalLeaveCriticalSection(pThread, &exception_critsec);

    LOGEXIT("SetConsoleCtrlHandler returns BOOL %d\n", retval);
    PERF_EXIT(SetConsoleCtrlHandler);
    return retval;
}

#if !HAVE_MACH_EXCEPTIONS
// TODO: Implement for Mach exceptions.  Not in CoreCLR surface area.
/*++
Function:
  GenerateConsoleCtrlEvent

See MSDN doc.

PAL specifics :
    dwProcessGroupId must be zero
              
--*/
BOOL
PALAPI
GenerateConsoleCtrlEvent(
    IN DWORD dwCtrlEvent,
    IN DWORD dwProcessGroupId
    )
{
    int sig;
    BOOL retval = FALSE;

    PERF_ENTRY(GenerateConsoleCtrlEvent);
    ENTRY("GenerateConsoleCtrlEvent(dwCtrlEvent=%d, dwProcessGroupId=%#x)\n",
        dwCtrlEvent, dwProcessGroupId);

    if(0!=dwProcessGroupId)
    {
        ASSERT("dwProcessGroupId is not 0, this is not supported by the PAL\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }
    switch(dwCtrlEvent)
    {
    case CTRL_C_EVENT :
        sig = SIGINT;
        break;
    case CTRL_BREAK_EVENT:
        /* Map control-break on SIGQUIT */
        sig = SIGQUIT;
        break;
    default:
        TRACE("got unknown control event\n");
        goto done;
    }

    TRACE("sending signal %d to process %d\n", sig, gPID);
    if(-1 == kill(gPID, sig))
    {
        ASSERT("kill() failed; errno is %d (%s)\n",errno, strerror(errno));
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }
    retval = TRUE;
done:
    LOGEXIT("GenerateConsoleCtrlEvent returns BOOL %d\n",retval);
    PERF_EXIT(GenerateConsoleCtrlEvent);
    return retval;
}
#endif // !HAVE_MACH_EXCEPTIONS

#if !HAVE_MACH_EXCEPTIONS
// TODO: Implement for Mach exceptions.  Not in CoreCLR surface area.
/*++
Function :
    SEHHandleControlEvent

    handle Control-C and Control-Break events (call handler routines, 
    notify debugger)

Parameters :
    DWORD event : event that occurred
    LPVOID eip  : instruction pointer when exception occurred                                 

(no return value)

Notes :
    Handlers are called on a last-installed, first called basis, until a
    handler returns TRUE. If no handler returns TRUE (or no handler is
    installed), the default behavior is to call the default handler of
    the corresponding signal.
--*/
void SEHHandleControlEvent(DWORD event, LPVOID eip)
{
    /* handler is actually a copy of the original list */
    CTRL_HANDLER_LIST *handler=NULL, *handlertail=NULL, *handlertmp, *newelem;
    BOOL fHandled = FALSE;
#ifdef _DEBUG
    BOOL fHoldingCritsec = TRUE;
#endif

    CPalThread *pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &exception_critsec);
    handlertmp = pCtrlHandler;

    /* nCtrlHandlerListLength is guaranteed to be at most 1 less than,
     * and not greater than, actual length.
     * We might get a stack overflow here, if the list is too large
     * However, that will lead us to terminate with an error, which is
     * the default behavior anyway.
     */
    newelem = reinterpret_cast<CTRL_HANDLER_LIST *>(
        alloca(sizeof(CTRL_HANDLER_LIST)*(nCtrlHandlerListLength+1))
        );

    // If alloca failed, we terminate
    if (newelem == NULL) {
        ERROR("alloca failed!");
        InternalLeaveCriticalSection(pthrCurrent, &exception_critsec);
#ifdef _DEBUG       
        fHoldingCritsec = FALSE;
#endif
        goto done;
    }

    /* list copying */
    while(NULL!=handlertmp)
    {
        newelem->handler = handlertmp->handler;
        newelem->next = NULL;
 
        /* add the new element to the list */
        if (handler == NULL)
        {
            handler = newelem;
        }
        else
        {
            handlertail->next = newelem;
        }
        handlertail = newelem;
 
        handlertmp = handlertmp->next;

        newelem++;
    }

    //
    // Once we've copied the handler list it's safe to release this
    // critical section. We cannot call the user handler routines
    // while holding this critsec -- a poorly written control handler
    // may block indefinitely, which, in turn, would prevent graceful
    // shutdown from occurring (as it would not be possible to
    // suspend this thread due to its holding of an internal critical
    // section).
    //
    // Note that this behavior is somewhat different than Windows --
    // where only a single control handler can run at a time. If we
    // need to replicate that behavior we would need to enter a
    // separate, non-internal critical section before calling the
    // control handlers.
    //

    InternalLeaveCriticalSection(pthrCurrent, &exception_critsec);
#ifdef _DEBUG
    fHoldingCritsec = FALSE;
#endif

    /* second, call handler routines until one handles the event */   

    while(NULL!=handler)
    {
        BOOL handler_retval;

/* reset ENTRY nesting level back to zero while inside the callback... */
#if !_NO_DEBUG_MESSAGES_
    {
        int old_level;
        old_level = DBG_change_entrylevel(0);
#endif /* !_NO_DEBUG_MESSAGES_ */

        handler_retval = handler->handler(event);

/* ...and set nesting level back to what it was */
#if !_NO_DEBUG_MESSAGES_
        DBG_change_entrylevel(old_level);
    }
#endif /* !_NO_DEBUG_MESSAGES_ */

        if(handler_retval)
        {
            TRACE("Console Control handler %p has handled event\n",
                  handler->handler);
            fHandled = TRUE;
            break;
        }
        handler = handler->next;
    }

done:
#ifdef _DEBUG
    _ASSERT_MSG(!fHoldingCritsec, "Exiting SEHHandleControlEvent while still holding a critical section.\n");
#endif
    if(!fHandled)
    {
        int signalCode;

        if(CTRL_C_EVENT == event)
        {
            TRACE("Control-C not handled; terminating.\n");
            signalCode = SIGINT;
        }
        else
        {
            TRACE("Control-Break not handled; terminating.\n");
            signalCode = SIGQUIT;
        }
        
        // The proper behavior for unhandled SIGINT/SIGQUIT is to set the signal handler to the default one
        // and then send the SIGINT/SIGQUIT to self and let the default handler do its work.
        struct sigaction action;
        action.sa_handler = SIG_DFL;
        action.sa_flags = 0;
        sigemptyset(&action.sa_mask);
        sigaction(signalCode, &action, NULL);

        kill(getpid(), signalCode);    
    }
}

#endif // !HAVE_MACH_EXCEPTIONS
