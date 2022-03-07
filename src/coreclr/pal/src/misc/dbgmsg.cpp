// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    misc/dbgmsg.cpp

Abstract:
    Implementation of Debug Message utilies. Relay channel information,
    output functions, etc.

--*/

/* PAL headers */

#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/file.hpp"

#include "config.h"
#include "pal/dbgmsg.h"
#include "pal/cruntime.h"
#include "pal/critsect.h"
#include "pal/file.h"
#include "pal/environ.h"

/* standard headers */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h> /* for pthread_self */
#include <errno.h>
#include <dirent.h>
#include <dlfcn.h>

/* <stdarg.h> needs to be included after "palinternal.h" to avoid name
   collision for va_start and va_end */
#include <stdarg.h>

using namespace CorUnix;

/* append mode file I/O is safer */
#define _PAL_APPEND_DBG_OUTPUT_

static const char FOPEN_FLAGS[] = "at";

/* number of ENTRY nesting levels to indicate with a '.' */
#define MAX_NESTING 50

/* size of output buffer (arbitrary) */
#define DBG_BUFFER_SIZE 20000

/* global and static variables */

LPCWSTR W16_NULLSTRING = (LPCWSTR) "N\0U\0L\0L\0\0";

DWORD dbg_channel_flags[DCI_LAST];
BOOL g_Dbg_asserts_enabled;

/* we must use stdio functions directly rather that rely on PAL functions for
  output, because those functions do tracing and we need to avoid recursion */
FILE *output_file = NULL;

/* master switch for debug channel enablement, to be modified by debugger */
Volatile<BOOL> dbg_master_switch = TRUE;


static const char *dbg_channel_names[]=
{
    "PAL",
    "LOADER",
    "HANDLE",
    "SHMEM",
    "PROCESS",
    "THREAD",
    "EXCEPT",
    "CRT",
    "UNICODE",
    "ARCH",
    "SYNC",
    "FILE",
    "VIRTUAL",
    "MEM",
    "SOCKET",
    "DEBUG",
    "LOCALE",
    "MISC",
    "MUTEX",
    "CRITSEC",
    "POLL",
    "CRYPT",
    "SHFOLDER"
  , "SXS"
  , "DCI_NUMA"
};

// Verify the number of elements in dbg_channel_names
static_assert_no_msg(ARRAY_SIZE(dbg_channel_names) == DCI_LAST);

static const char *dbg_level_names[]=
{
    "ENTRY",
    "TRACE",
    "WARN",
    "ERROR",
    "ASSERT",
    "EXIT"
};

static const char ENV_FILE[]="PAL_API_TRACING";
static const char ENV_CHANNELS[]="PAL_DBG_CHANNELS";
static const char ENV_ASSERTS[]="PAL_DISABLE_ASSERTS";
static const char ENV_ENTRY_LEVELS[]="PAL_API_LEVELS";

/* per-thread storage for ENTRY tracing level */
static pthread_key_t entry_level_key;

/* entry level limitation */
static int max_entry_level;

/* character to use for ENTRY indentation */
static const char INDENT_CHAR = '.';

static BOOL DBG_get_indent(DBG_LEVEL_ID level, const char *format,
                           char *indent_string);

static CRITICAL_SECTION fprintf_crit_section;

/* Function definitions */

/*++
Function :
    DBG_init_channels

    Parse environment variables PAL_DBG_CHANNELS and PAL_API_TRACING for debug
    channel settings; initialize static variables.

    (no parameters, no return value)
--*/
BOOL DBG_init_channels(void)
{
    INT i;
    LPSTR env_string;
    LPSTR env_workstring;
    LPSTR env_pcache;
    LPSTR entry_ptr;
    LPSTR level_ptr;
    CHAR plus_or_minus;
    DWORD flag_mask = 0;
    int ret;

    /* output only asserts by default [only affects no-vararg-support case; if
       we have varargs, these flags aren't even checked for ASSERTs] */
    for(i=0;i<DCI_LAST;i++)
        dbg_channel_flags[i]=1<<DLI_ASSERT;

    /* parse PAL_DBG_CHANNELS environment variable */

    env_string = getenv(ENV_CHANNELS);
    if (env_string != NULL)
    {
        env_string = strdup(env_string);
    }
    env_pcache = env_workstring = env_string;

    while(env_workstring)
    {
        entry_ptr=env_workstring;

        /* find beginning of next entry */
        while((*entry_ptr != '\0') &&(*entry_ptr != '+') && (*entry_ptr != '-'))
        {
            entry_ptr++;
        }

        /* break if end of string is reached */
        if(*entry_ptr == '\0')
        {
           break;
        }

        plus_or_minus=*entry_ptr++;

        /* find end of entry; if strchr returns NULL, we have reached the end
           of the string and we will leave the loop at the end of this pass. */
        env_workstring=strchr(entry_ptr,':');

        /* NULL-terminate entry, make env_string point to rest of string */
        if(env_workstring)
        {
            *env_workstring++='\0';
        }

        /* find period that separates channel name from level name */
        level_ptr=strchr(entry_ptr,'.');

        /* an entry with no period is illegal : ignore it */
        if(!level_ptr)
        {
            continue;
        }
        /* NULL-terminate channel name, make level_ptr point to the level name */
        *level_ptr++='\0';

        /* build the flag mask based on requested level */

        /* if "all" level is specified, we want to open/close all levels at
           once, so mask is either all ones or all zeroes */
        if(!strcmp(level_ptr,"all"))
        {
            if(plus_or_minus=='+')
            {
                flag_mask=0xFFFF;  /* OR this to open all levels */
            }
            else
            {
                flag_mask=0;       /* AND this to close all levels*/
            }
        }
        else
        {
            for(i=0;i<DLI_LAST;i++)
            {
                if(!strcmp(level_ptr,dbg_level_names[i]))
                {
                    if(plus_or_minus=='+')
                    {
                        flag_mask=1<<i;     /* OR this to open the level */
                    }
                    else
                    {
                        flag_mask=~(1<<i);  /* AND this to close the level */
                    }
                    break;
                }
            }
            /* didn't find a matching level : skip it. */
            if(i==DLI_LAST)
            {
                continue;
            }
        }

        /* Set EXIT and ENTRY channels to be identical */
        if(!(flag_mask & (1<<DLI_ENTRY)))
        {
            flag_mask = flag_mask & (~(1<<DLI_EXIT));
        }
        else
        {
            flag_mask = flag_mask | (1<<DLI_EXIT);
        }

        /* apply the flag mask to the specified channel */

        /* if "all" channel is specified, apply mask to all channels */
        if(!strcmp(entry_ptr,"all"))
        {
            if(plus_or_minus=='+')
            {
                for(i=0;i<DCI_LAST;i++)
                {
                    dbg_channel_flags[i] |= flag_mask; /* OR to open levels*/
                }
            }
            else
            {
                for(i=0;i<DCI_LAST;i++)
                {
                    dbg_channel_flags[i] &= flag_mask; /* AND to close levels */
                }
            }
        }
        else
        {
            for(i=0;i<DCI_LAST;i++)
            {
                if(!strcmp(entry_ptr,dbg_channel_names[i]))
                {
                    if(plus_or_minus=='+')
                    {
                        dbg_channel_flags[i] |= flag_mask;
                    }
                    else
                    {
                        dbg_channel_flags[i] &= flag_mask;
                    }

                    break;
                }
            }
            /* ignore the entry if the channel name is unknown */
        }
        /* done processing this entry; on to the next. */
    }
    free(env_pcache);

    /* select output file */
    env_string = getenv(ENV_FILE);
    if(env_string && *env_string!='\0')
    {
        if(!strcmp(env_string, "stderr"))
        {
            output_file = stderr;
        }
        else if(!strcmp(env_string, "stdout"))
        {
            output_file = stdout;
        }
        else
        {
            output_file = fopen(env_string,FOPEN_FLAGS);

            /* if file can't be opened, default to stderr */
            if(!output_file)
            {
                output_file = stderr;
                fprintf(stderr, "Can't open %s for writing : debug messages "
                        "will go to stderr. Check your PAL_API_TRACING "
                        "variable!\n", env_string);
            }
        }
    }
    else
    {
        output_file = stderr; /* output to stderr by default */
    }

    /* see if we need to disable assertions */
    env_string = getenv(ENV_ASSERTS);
    if(env_string && 0 == strcmp(env_string,"1"))
    {
        g_Dbg_asserts_enabled = FALSE;
    }
    else
    {
        g_Dbg_asserts_enabled = TRUE;
    }

    /* select ENTRY level limitation */
    env_string = getenv(ENV_ENTRY_LEVELS);
    if(env_string)
    {
        max_entry_level = atoi(env_string);
    }
    else
    {
        max_entry_level = 1;
    }

    /* if necessary, allocate TLS key for entry nesting level */
    if(0 != max_entry_level)
    {
        if ((ret = pthread_key_create(&entry_level_key,NULL)) != 0)
        {
            fprintf(stderr, "ERROR : pthread_key_create() failed error:%d (%s)\n",
                   ret, strerror(ret));
            return FALSE;
        }
    }

    InternalInitializeCriticalSection(&fprintf_crit_section);

    return TRUE;
}

/*++
Function :
    DBG_close_channels

    Stop outputting debug messages by closing the associated file.

    (no parameters, no return value)
--*/
void DBG_close_channels()
{
    if(output_file && output_file != stderr && output_file != stdout)
    {
        if (fclose(output_file) != 0)
        {
            fprintf(stderr, "ERROR : fclose() failed errno:%d (%s)\n",
                   errno, strerror(errno));
        }
    }

    output_file = NULL;

    DeleteCriticalSection(&fprintf_crit_section);

    /* if necessary, release TLS key for entry nesting level */
    if(0 != max_entry_level)
    {
        int retval;

        retval = pthread_key_delete(entry_level_key);
        if(0 != retval)
        {
            fprintf(stderr, "ERROR : pthread_key_delete() returned %d! (%s)\n",
                    retval, strerror(retval));
        }
    }
}


static const void *DBG_get_module_id()
{
    static const void *s_module_id = NULL;
    if (s_module_id == NULL)
    {
        Dl_info dl_info;
        if (dladdr((void *) DBG_get_module_id, &dl_info) == 0 || dl_info.dli_sname == NULL)
        {
            s_module_id = (void *) -1;
        }
        else
        {
            s_module_id = dl_info.dli_fbase;
        }
    }
    return s_module_id;
}

#define MODULE_ID DBG_get_module_id,
#define MODULE_FORMAT "-%p"

/*++
Function :
    DBG_printf

    Internal function for debug channels; don't use.
    This function outputs a complete debug message, including the function name.

Parameters :
    DBG_CHANNEL_ID channel : debug channel to use
    DBG_LEVEL_ID level : debug message level
    BOOL bHeader : whether or not to output message header (thread id, etc)
    LPCSTR function : current function
    LPCSTR file : current file
    INT line : line number
    LPCSTR format, ... : standard printf parameter list.

Return Value :
    always 1.

Notes :
    This version is for compilers that support the C99 flavor of
    variable-argument macros but not the gnu flavor, and do not support the
    __FUNCTION__ pseudo-macro.

--*/
int DBG_printf(DBG_CHANNEL_ID channel, DBG_LEVEL_ID level, BOOL bHeader,
               LPCSTR function, LPCSTR file, INT line, LPCSTR format, ...)
{
    struct ErrnoHolder
    {
        int value;
        ErrnoHolder() : value(errno) { }
        ~ErrnoHolder()
        {
            errno = value;
        }
    } errno_holder;

    CHAR indent[MAX_NESTING+1];
    if(!DBG_get_indent(level, format, indent))
    {
        // Note: we will drop log messages here if the indent gets too high, and we won't print
        //       an error when this occurs.
        return 1;
    }

    void *thread_id = (void *)THREADSilentGetCurrentThreadId();

    CHAR buffer[DBG_BUFFER_SIZE];
    INT output_size;
    if(bHeader)
    {
        /* Print file instead of function name for ENTRY messages, because those
           already include the function name */
        /* also print file name for ASSERTs, to match Win32 behavior */
        LPCSTR location;
        if( DLI_ENTRY == level || DLI_ASSERT == level || DLI_EXIT == level)
            location = file;
        else
            location = function;
        output_size=snprintf(buffer, DBG_BUFFER_SIZE,
                             "{%p" MODULE_FORMAT "} %-5s [%-7s] at %s.%d: ",
                             thread_id, MODULE_ID
                             dbg_level_names[level], dbg_channel_names[channel], location, line);
        if( output_size < 0)
        {
            fprintf(stderr, "ERROR : DBG_printf: snprintf header failed errno:%d (%s)\n", errno, strerror(errno));
            output_size = 0; // don't return, just drop the header from the log message
        }
        else if (output_size > DBG_BUFFER_SIZE)
        {
            output_size = DBG_BUFFER_SIZE;
        }
    }
    else
    {
        output_size = 0;
    }

    {
        va_list args;
        va_start(args, format);
        INT result = _vsnprintf_s(buffer+output_size, DBG_BUFFER_SIZE-output_size, _TRUNCATE,
                                  format, args);
        va_end(args);
        if( result < 0 )
        {
            // if we didn't get data from _vsnprintf_s, print an error and exit
            if ( output_size == 0 || buffer[output_size] == '\0' )
            {
                fprintf(stderr, "ERROR : DBG_printf: vsnprintf_s failed errno:%d (%s)\n", errno, strerror(errno));
                return 1;
            }
            else if (output_size < DBG_BUFFER_SIZE)
            {
                fprintf(stderr, "ERROR : DBG_printf: message truncated, vsnprintf_s failed errno:%d (%s)\n", errno, strerror(errno));
                // do not return, print what we have
            }
        }
        else
        {
            output_size+=result;
        }
    }

    if( output_size >= DBG_BUFFER_SIZE )
    {
        fprintf(stderr, "ERROR : DBG_printf: message truncated");
    }

    /* Use a Critical section before calling printf code to
       avoid holding a libc lock while another thread is calling
       SuspendThread on this one. */

    InternalEnterCriticalSection(NULL, &fprintf_crit_section);
    fprintf( output_file, "%s%s", indent, buffer );
    InternalLeaveCriticalSection(NULL, &fprintf_crit_section);

    /* flush the output to file */
    if ( fflush(output_file) != 0 )
    {
        fprintf(stderr, "ERROR : fflush() failed errno:%d (%s)\n",
                errno, strerror(errno));
    }

    // Some systems support displaying a GUI dialog. We attempt this only for asserts.
    if ( level == DLI_ASSERT )
        PAL_DisplayDialog("PAL ASSERT", buffer);

    return 1;
}

/*++
Function :
    DBG_get_indent

    generate an indentation string to be used for message output

Parameters :
    DBG_LEVEL_ID level  : level of message (DLI_ENTRY, etc)
    const char *format  : printf format string of message
    char *indent_string : destination for indentation string

Return value :
    TRUE if output can proceed, FALSE otherwise

Notes:
As a side-effect, this function updates the ENTRY nesting level for the current
thread : it decrements it if 'format' contains the string 'return', increments
it otherwise (but only if 'level' is DLI_ENTRY). The function will return
FALSE if the current nesting level is beyond our treshold (max_nesting_level);
it always returns TRUE for other message levels
--*/
static BOOL DBG_get_indent(DBG_LEVEL_ID level, const char *format,
                           char *indent_string)
{
    int ret;

    /* determine whether to output an ENTRY line */
    if(DLI_ENTRY == level||DLI_EXIT == level)
    {
        if(0 != max_entry_level)
        {
            INT_PTR nesting;

            /* Determine if this is an entry or an
               exit */
            if(DLI_EXIT == level)
            {
                nesting = (INT_PTR) pthread_getspecific(entry_level_key);
                /* avoid going negative */
                if(nesting != 0)
                {
                    nesting--;
                    if ((ret = pthread_setspecific(entry_level_key,
                                                     (LPVOID)nesting)) != 0)
                    {
                        fprintf(stderr, "ERROR : pthread_setspecific() failed "
                                "error:%d (%s)\n", ret, strerror(ret));
                    }
                }
            }
            else
            {
                nesting = (INT_PTR) pthread_getspecific(entry_level_key);

                if ((ret = pthread_setspecific(entry_level_key,
                                                 (LPVOID)(nesting+1))) != 0)
                {
                    fprintf(stderr, "ERROR : pthread_setspecific() failed "
                            "error:%d (%s)\n", ret, strerror(ret));
                }
            }

            /* see if we're past the level treshold */
            if(nesting >= max_entry_level)
            {
                return FALSE;
            }

            /* generate indentation string */
            if(MAX_NESTING < nesting)
            {
                nesting = MAX_NESTING;
            }
            memset(indent_string,INDENT_CHAR ,nesting);
            indent_string[nesting] = '\0';
        }
        else
        {
            indent_string[0] = '\0';
        }
    }
    else
    {
        indent_string[0] = '\0';
    }
    return TRUE;
}

/*++
Function :
    DBG_change_entrylevel

    retrieve current ENTRY nesting level and [optionnally] modify it

Parameters :
    int new_level : value to which the nesting level must be set, or -1

Return value :
    nesting level at the time the function was called

Notes:
if new_level is -1, the nesting level will not be modified
--*/
int DBG_change_entrylevel(int new_level)
{
    int old_level;
    int ret;

    if(0 == max_entry_level)
    {
        return 0;
    }
    old_level = PtrToInt(pthread_getspecific(entry_level_key));
    if(-1 != new_level)
    {
        if ((ret = pthread_setspecific(entry_level_key,(LPVOID)(IntToPtr(new_level)))) != 0)
        {
            fprintf(stderr, "ERROR : pthread_setspecific() failed "
                    "error:%d (%s)\n", ret, strerror(ret));
        }
    }
    return old_level;
}

#if _DEBUG && defined(__APPLE__)
/*++
Function:
    DBG_ShouldCheckStackAlignment

    Wires up stack alignment checks (debug builds only)
--*/
static const char * PAL_CHECK_ALIGNMENT_MODE = "PAL_CheckAlignmentMode";
enum CheckAlignmentMode
{
    // special value to indicate we've not initialized yet
    CheckAlignment_Uninitialized    = -1,

    CheckAlignment_Off              = 0,
    CheckAlignment_On               = 1,

    CheckAlignment_Default          = CheckAlignment_On
};

bool DBG_ShouldCheckStackAlignment()
{
    static CheckAlignmentMode caMode = CheckAlignment_Uninitialized;

    if (caMode == CheckAlignment_Uninitialized)
    {
        char* checkAlignmentSettings;
        bool shouldFreeCheckAlignmentSettings = false;
        if (palEnvironment == nullptr)
        {
            // This function might be called before the PAL environment is initialized.
            // In this case, use the system getenv instead.
            checkAlignmentSettings = ::getenv(PAL_CHECK_ALIGNMENT_MODE);
        }
        else
        {
            checkAlignmentSettings = EnvironGetenv(PAL_CHECK_ALIGNMENT_MODE);
            shouldFreeCheckAlignmentSettings = true;
        }

        caMode = checkAlignmentSettings ?
            (CheckAlignmentMode)atoi(checkAlignmentSettings) : CheckAlignment_Default;

        if (checkAlignmentSettings && shouldFreeCheckAlignmentSettings)
        {
            free(checkAlignmentSettings);
        }
    }

    return caMode == CheckAlignment_On;
}
#endif // _DEBUG && __APPLE__

#ifdef __APPLE__
#include "CoreFoundation/CFUserNotification.h"
#include "CoreFoundation/CFString.h"
#include "Security/AuthSession.h"

static const char * PAL_DISPLAY_DIALOG = "PAL_DisplayDialog";
enum DisplayDialogMode
{
    DisplayDialog_Uninitialized = -1,

    DisplayDialog_Suppress = 0,
    DisplayDialog_Show = 1,

    DisplayDialog_Default = DisplayDialog_Suppress,
};

/*++
Function :
    PAL_DisplayDialog

    Display a simple modal dialog with an alert icon and a single OK button. Caller supplies the title of the
    dialog and the main text. The dialog is displayed only if the PAL_DisplayDialog environment
    variable is set to the value "1" and the session has access to the display.

--*/
void PAL_DisplayDialog(const char *szTitle, const char *szText)
{
    static DisplayDialogMode dispDialog = DisplayDialog_Uninitialized;

    if (dispDialog == DisplayDialog_Uninitialized)
    {
        char* displayDialog = EnvironGetenv(PAL_DISPLAY_DIALOG);
        if (displayDialog)
        {
            int i = atoi(displayDialog);
            free(displayDialog);

            switch (i)
            {
            case 0:
                dispDialog = DisplayDialog_Suppress;
                break;

            case 1:
                dispDialog = DisplayDialog_Show;
                break;

            default:
                // Asserting here would just be re-entrant. :/
                dispDialog = DisplayDialog_Default;
                break;
            }
        }
        else
            dispDialog = DisplayDialog_Default;

        if (dispDialog == DisplayDialog_Show)
        {
            // We may not be allowed to show.
            OSStatus osstatus;
            SecuritySessionId secSession;
            SessionAttributeBits secSessionInfo;

            osstatus = SessionGetInfo(callerSecuritySession, &secSession, &secSessionInfo);
            if (noErr != osstatus || (secSessionInfo & sessionHasGraphicAccess) == 0)
                dispDialog = DisplayDialog_Suppress;
        }
    }

    if (dispDialog == DisplayDialog_Suppress)
        return;

    CFStringRef cfsTitle = CFStringCreateWithCString(kCFAllocatorDefault,
                                                     szTitle,
                                                     kCFStringEncodingUTF8);
    if (cfsTitle != NULL)
    {
        CFStringRef cfsText = CFStringCreateWithCString(kCFAllocatorDefault,
                                                        szText,
                                                        kCFStringEncodingUTF8);
        if (cfsText != NULL)
        {
            CFOptionFlags response;
            CFUserNotificationDisplayAlert(0,               // Never time-out, wait for user to hit 'OK'
                                           0,               // No flags
                                           NULL,            // Default icon
                                           NULL,            // Default sound
                                           NULL,            // No-localization support for text
                                           cfsTitle,        // Title for dialog
                                           cfsText,         // The actual alert text
                                           NULL,            // Default default button title ('OK')
                                           NULL,            // No alternate button
                                           NULL,            // No third button
                                           &response);      // User's response (discarded)
            CFRelease(cfsText);
        }
        CFRelease(cfsTitle);
    }
}

/*++
Function :
    PAL_DisplayDialogFormatted

    As above but takes a printf-style format string and insertion values to form the main text.

--*/
void PAL_DisplayDialogFormatted(const char *szTitle, const char *szTextFormat, ...)
{
    va_list args;

    va_start(args, szTextFormat);

    const int cchBuffer = 4096;
    char *szBuffer = (char*)alloca(cchBuffer);
    _vsnprintf_s(szBuffer, cchBuffer, _TRUNCATE, szTextFormat, args);
    PAL_DisplayDialog(szTitle, szBuffer);

    va_end(args);
}
#endif // __APPLE__
