//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    modulename.cpp

Abstract:

    Implementation of internal functions to get module names



--*/

#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/modulename.h"

#if NEED_DLCOMPAT
#include "dlcompat.h"
#else   // NEED_DLCOMPAT
#include <dlfcn.h>
#endif  // NEED_DLCOMPAT

#if defined(_AIX)
#include <sys/ldr.h>
#endif

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(LOADER);

#if defined(_AIX)
/*++
    GetLibRotorNameViaLoadQuery

    Retrieve the full path of the librotor_pal.so using loadquery()

Parameters:
    pszBuf - CHAR array of MAX_PATH_FNAME length

Return value:
    0 on success
    -1 on failure, with last error set
--*/
int GetLibRotorNameViaLoadQuery(LPSTR pszBuf)
{
    CHAR*               pLoadQueryBuf = NULL;
    UINT                cbBuf = 1024;
    struct ld_info *    pInfo = NULL;
    INT                 iLQRetVal = -1;
    INT                 iRetVal = -1;
    CPalThread *pThread = NULL;

    if (!pszBuf)
    {
        ASSERT("GetLibRotorNameViaLoadQuery requires non-NULL pszBuf\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto Done;
    }

    pThread = InternalGetCurrentThread();
    // Loop trying to call loadquery with enough memory until either 
    // 1) we succeed, 2) we run out of memory or 3) loadquery throws
    // an error other than ENOMEM
    while (iLQRetVal != 0)
    {
        pLoadQueryBuf = (CHAR*) InternalMalloc (pThread, cbBuf * sizeof(char));
        if (!pLoadQueryBuf)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto Done;
        }
        iLQRetVal = loadquery(L_GETINFO, pLoadQueryBuf, cbBuf);
        if (iLQRetVal < 0)
        {
            InternalFree(pThread, pLoadQueryBuf);
            pLoadQueryBuf = NULL;
            DWORD dwLastError = GetLastError();
            if (dwLastError == ERROR_NOT_ENOUGH_MEMORY)
            {
                // The buffer's too small.  Try twice as large as a guess...
                cbBuf *= 2;
            }
            else
            {
                SetLastError(ERROR_INTERNAL_ERROR);
                goto Done;
            }
        }
    }

    // We successfully called loadquery, so now see if we can find 
    // librotor_pal.a in the module list
    if (pLoadQueryBuf)
    {
        pInfo = (struct ld_info *)pLoadQueryBuf;        
        while (TRUE)
        {  
            if (strstr(pInfo->ldinfo_filename, "librotor_pal.a"))
            {
                UINT cchFileName = strlen(pInfo->ldinfo_filename);
                if (cchFileName + 1  > MAX_PATH_FNAME)
                {
                    ASSERT("Filename returned by loadquery was longer than MAX_PATH_FNAME!\n");
                    SetLastError(ERROR_INTERNAL_ERROR);
                    goto Done;
                }
                else
                {
                    // The buffer should be large enough to accomodate the filename.
                    // So, we send in the size of the filename+1
                    strcpy_s(pszBuf, MAX_PATH_FNAME, pInfo->ldinfo_filename);
                    iRetVal = 0;
                    goto Done;
                }
            }
            else
            {
                // The (wacky) design of ld_info is that the value of next is an offset in 
                // bytes rather than a pointer.  So we need this weird cast to char * to get
                // the pointer math correct.
                if (pInfo->ldinfo_next == 0)
                    break;
                else
                    pInfo = (struct ld_info *) ((char *)pInfo + pInfo->ldinfo_next);
            }
        }
    }
Done:
    if (pLoadQueryBuf)
        InternalFree(pThread, pLoadQueryBuf);
    return iRetVal;
}
#endif // defined(_AIX)

/*++
    PAL_dladdr

    Internal wrapper for dladder used only to get module name

Parameters:
    None

Return value:
    Pointer to string with the fullpath to the librotor_pal.so being
    used.

    NULL if error occurred.

Notes: 
    The string returned by this function is owned by the OS.
    If you need to keep it, strdup() it, because it is unknown how long
    this ptr will point at the string you want (over the lifetime of
    the system running)  It is only safe to use it immediately after calling
    this function.
--*/
const char *PAL_dladdr(LPVOID ProcAddress)
{
#if defined(_AIX) || defined(__hppa__)
    /* dladdr is not supported on AIX or 32-bit HPUX-PARISC */
    return (NULL);
#elif defined(_HPUX_) && defined(_IA64_)
    /* dladdr is not supported on HP-UX/IA64.  That said, PAL_dladdr just returns to module name
       and we can get that via dlgetname.  So use that for HPUX.  */
    {
        char*               pszName = NULL;
        load_module_desc    desc;
        __uint64_t          uimodret = NULL;
        uimodret = dlmodinfo((__uint64_t)ProcAddress, &desc, sizeof(desc), NULL, 0, 0);
        if (!uimodret)
        {
            WARN("dlmodinfo call failed! dlerror says '%s'\n", dlerror());
            return NULL;
        }
        pszName = dlgetname(&desc, sizeof(desc), NULL, 0, 0);
        if (!pszName)
        {
            WARN("dlgetname desc didn't describe a loaded module?! dlerror says '%s'\n", dlerror());
            return NULL;
        }
        return pszName;
    }
#else
    Dl_info dl_info;
    if (!dladdr(ProcAddress, &dl_info))
    {
        WARN("dladdr() call failed! dlerror says '%s'\n", dlerror());
        /* If we get an error, return NULL */
        return (NULL);
    }
    else 
    {
        /* Return the module name */ 
        return dl_info.dli_fname;
    }
#endif
}

