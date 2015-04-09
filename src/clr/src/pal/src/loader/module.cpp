//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    module.c

Abstract:

    Implementation of module related functions in the Win32 API



--*/

#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/file.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/module.h"
#include "pal/cs.hpp"
#include "pal/process.h"
#include "pal/file.h"
#include "pal/utils.h"
#include "pal/init.h"
#include "pal/modulename.h"
#include "pal/misc.h"
#include "pal/virtual.h"
#include "pal/map.hpp"

#include <sys/param.h>
#include <errno.h>
#include <string.h>
#include <limits.h>
#if NEED_DLCOMPAT
#include "dlcompat.h"
#else   // NEED_DLCOMPAT
#include <dlfcn.h>
#endif  // NEED_DLCOMPAT
#if HAVE_ALLOCA_H
#include <alloca.h>
#endif  // HAVE_ALLOCA_H

#ifdef __APPLE__
#include <mach-o/dyld.h>
#include <mach-o/loader.h>
#endif // __APPLE__

#include <sys/types.h>
#include <sys/mman.h>

#if defined(__LINUX__)
#include <gnu/lib-names.h>
#endif

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(LOADER);

// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(LOADER)
#include <safemath.h>

/* macro definitions **********************************************************/

/* get the full name of a module if available, and the short name otherwise*/
#define MODNAME(x) ((x)->lib_name)

/* Which path should FindLibrary search? */
#if defined(__APPLE__)
#define LIBSEARCHPATH "DYLD_LIBRARY_PATH"
#else
#define LIBSEARCHPATH "LD_LIBRARY_PATH"
#endif

#define LIBC_NAME_WITHOUT_EXTENSION "libc"

/* static variables ***********************************************************/

/* critical section that regulates access to the module list */
CRITICAL_SECTION module_critsec;

MODSTRUCT exe_module; /* always the first, in the in-load-order list */
MODSTRUCT pal_module; /* always the second, in the in-load-order list */

PDLLMAIN g_pRuntimeDllMain = NULL;
// Use the g_szCoreCLRPath global to determine whether we're really part of CoreCLR or just a standalone PAL
// linked into some utility.
extern char g_szCoreCLRPath[MAX_PATH];

/* static function declarations ***********************************************/

static BOOL LOADValidateModule(MODSTRUCT *module);
static LPWSTR LOADGetModuleFileName(MODSTRUCT *module);
static HMODULE LOADLoadLibrary(LPCSTR ShortAsciiName, BOOL fDynamic);
static void LOAD_SEH_CallDllMain(MODSTRUCT *module, DWORD dwReason, LPVOID lpReserved);
static MODSTRUCT *LOADAllocModule(void *dl_handle, LPCSTR name);
static INT FindLibrary(CHAR* pszRelName, CHAR** ppszFullName);

/* API function definitions ***************************************************/

/*++
Function:
  LoadLibraryA

See MSDN doc.
--*/
HMODULE
PALAPI
LoadLibraryA(
         IN LPCSTR lpLibFileName)
{
    return LoadLibraryExA(lpLibFileName, NULL, 0);
}

/*++
Function:
  LoadLibraryW

See MSDN doc.
--*/
HMODULE
PALAPI
LoadLibraryW(
         IN LPCWSTR lpLibFileName)
{
    return LoadLibraryExW(lpLibFileName, NULL, 0);
}

/*++
Function:
LoadLibraryExA

See MSDN doc.
--*/
HMODULE
PALAPI
LoadLibraryExA(
        IN LPCSTR lpLibFileName,
        IN /*Reserved*/ HANDLE hFile,
        IN DWORD dwFlags)
{
    if (dwFlags != 0) 
    {
        // UNIXTODO: Implement this!
        ASSERT("Needs Implementation!!!");
        return NULL;
    }

    LPSTR lpstr = NULL;
    HMODULE hModule = NULL;
    CPalThread *pThread = NULL;

    PERF_ENTRY(LoadLibraryA);
    ENTRY("LoadLibraryExA (lpLibFileName=%p (%s)) \n",
          (lpLibFileName)?lpLibFileName:"NULL",
          (lpLibFileName)?lpLibFileName:"NULL");

    if(NULL == lpLibFileName)
    {
        ERROR("lpLibFileName is NULL;Exit.\n");
        SetLastError(ERROR_MOD_NOT_FOUND);
        goto Done;
    }

    if(lpLibFileName[0]=='\0')
    {
        ERROR("can't load library with NULL file name...\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto Done;
    }

    pThread = InternalGetCurrentThread();
    /* do the Dos/Unix conversion on our own copy of the name */
    lpstr = InternalStrdup(pThread, lpLibFileName);
    if(!lpstr)
    {
        ERROR("InternalStrdup failure!\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto Done;
    }
    FILEDosToUnixPathA(lpstr);

    hModule = LOADLoadLibrary(lpstr, TRUE);

    /* let LOADLoadLibrary call SetLastError */
 Done:
    if (lpstr != NULL)
    {
        InternalFree(pThread, lpstr);
    }

    LOGEXIT("LoadLibraryExA returns HMODULE %p\n", hModule);
    PERF_EXIT(LoadLibraryExA);
    return hModule;
    
}

/*++
Function:
LoadLibraryExW

See MSDN doc.
--*/
HMODULE
PALAPI
LoadLibraryExW(
        IN LPCWSTR lpLibFileName,
        IN /*Reserved*/ HANDLE hFile,
        IN DWORD dwFlags)
{
    if (dwFlags != 0) 
    {
        // UNIXTODO: Implement this!
        ASSERT("Needs Implementation!!!");
        return NULL;
    }
    
    CHAR lpstr[MAX_PATH];
    INT name_length;
    HMODULE hModule = NULL;

    PERF_ENTRY(LoadLibraryExW);
    ENTRY("LoadLibraryExW (lpLibFileName=%p (%S)) \n",
          lpLibFileName?lpLibFileName:W16_NULLSTRING,
          lpLibFileName?lpLibFileName:W16_NULLSTRING);

    if(NULL == lpLibFileName)
    {
        ERROR("lpLibFileName is NULL;Exit.\n");
        SetLastError(ERROR_MOD_NOT_FOUND);
        goto done;
    }

    if(lpLibFileName[0]==0)
    {
        ERROR("Can't load library with NULL file name...\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    /* do the Dos/Unix conversion on our own copy of the name */

    name_length = WideCharToMultiByte(CP_ACP, 0, lpLibFileName, -1, lpstr,
                                      MAX_PATH, NULL, NULL);
    if( name_length == 0 )
    {
        DWORD dwLastError = GetLastError();
        if( dwLastError == ERROR_INSUFFICIENT_BUFFER )
        {
            ERROR("lpLibFileName is larger than MAX_PATH (%d)!\n", MAX_PATH);
        }
        else
        {
            ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        }
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    FILEDosToUnixPathA(lpstr);

    /* let LOADLoadLibrary call SetLastError in case of failure */
    hModule = LOADLoadLibrary(lpstr, TRUE);

done:
    LOGEXIT("LoadLibraryExW returns HMODULE %p\n", hModule);
    PERF_EXIT(LoadLibraryExW);
    return hModule;
}

/*++
Function:
  GetProcAddress

See MSDN doc.
--*/
FARPROC
PALAPI
GetProcAddress(
           IN HMODULE hModule,
           IN LPCSTR lpProcName)
{
    MODSTRUCT *module;
    FARPROC ProcAddress = NULL;
    LPCSTR symbolName = lpProcName;

    PERF_ENTRY(GetProcAddress);
    ENTRY("GetProcAddress (hModule=%p, lpProcName=%p (%s))\n",
          hModule, lpProcName?lpProcName:"NULL", lpProcName?lpProcName:"NULL");

    LockModuleList();

    module = (MODSTRUCT *) hModule;

    /* parameter validation */

    if( (lpProcName == NULL) || (*lpProcName == '\0') )
    {
        TRACE("No function name given\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    if( !LOADValidateModule( module ) )
    {
        TRACE("Invalid module handle %p\n", hModule);
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }
    
    /* try to assert on attempt to locate symbol by ordinal */
    /* this can't be an exact test for HIWORD((DWORD)lpProcName) == 0
       because of the address range reserved for ordinals contain can
       be a valid string address on non-Windows systems
    */
    if( (DWORD_PTR)lpProcName < VIRTUAL_PAGE_SIZE )
    {
        ASSERT("Attempt to locate symbol by ordinal?!\n");
    }

    // Get the symbol's address.
    
    // If we're looking for a symbol inside the PAL, we try the PAL_ variant
    // first because otherwise we run the risk of having the non-PAL_
    // variant preferred over the PAL's implementation.
    if (module->dl_handle == pal_module.dl_handle)
    {
        int iLen = 4 + strlen(lpProcName) + 1;
        LPSTR lpPALProcName = (LPSTR) alloca(iLen);

        if (strcpy_s(lpPALProcName, iLen, "PAL_") != SAFECRT_SUCCESS)
        {
            ERROR("strcpy_s failed!\n");
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            goto done;
        }

        if (strcat_s(lpPALProcName, iLen, lpProcName) != SAFECRT_SUCCESS)
        {
            ERROR("strcat_s failed!\n");
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            goto done;
        }

        ProcAddress = (FARPROC) dlsym(module->dl_handle, lpPALProcName);
        symbolName = lpPALProcName;
    }

    // If we aren't looking inside the PAL or we didn't find a PAL_ variant
    // inside the PAL, fall back to a normal search.
    if (ProcAddress == NULL)
    {
            ProcAddress = (FARPROC) dlsym(module->dl_handle, lpProcName);
    }

    if (ProcAddress)
    {
        TRACE("Symbol %s found at address %p in module %p (named %S)\n",
              lpProcName, ProcAddress, module, MODNAME(module));

        /* if we don't know the module's full name yet, this is our chance to
           obtain it */
        if(!module->lib_name && module->dl_handle)
        {
            const char* libName = PAL_dladdr((LPVOID)ProcAddress);
            if (libName)
            {
                module->lib_name = UTIL_MBToWC_Alloc(libName, -1);
                if(NULL == module->lib_name)
                {
                    ERROR("MBToWC failure; can't save module's full name\n");
                }
                else
                {
                    TRACE("Saving full path of module %p as %s\n",
                          module, libName);
                }
            }
        }
    }
    else
    {
        TRACE("Symbol %s not found in module %p (named %S), dlerror message is \"%s\"\n",
              lpProcName, module, MODNAME(module), dlerror());
        SetLastError(ERROR_PROC_NOT_FOUND);
    }
done:
    UnlockModuleList();
    LOGEXIT("GetProcAddress returns FARPROC %p\n", ProcAddress);
    PERF_EXIT(GetProcAddress);
    return ProcAddress;
}


/*++
Function:
  FreeLibrary

See MSDN doc.
--*/
BOOL
PALAPI
FreeLibrary(
        IN OUT HMODULE hLibModule)
{
    MODSTRUCT *module;
    BOOL retval = FALSE;
    CPalThread *pThread;

    PERF_ENTRY(FreeLibrary);
    ENTRY("FreeLibrary (hLibModule=%p)\n", hLibModule);

    LockModuleList();

    module = (MODSTRUCT *) hLibModule;

    if (terminator)
    {
        /* PAL shutdown is in progress - ignore FreeLibrary calls */
        retval = TRUE;
        goto done;
    }

    if( !LOADValidateModule( module ) )
    {
        TRACE("Can't free invalid module handle %p\n", hLibModule);
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }

    if( module->refcount == -1 )
    {
        /* special module - never released */
        retval=TRUE;
        goto done;
    }

    module->refcount--;
    TRACE("Reference count for module %p (named %S) decreases to %d\n",
            module, MODNAME(module), module->refcount);

    if( module->refcount != 0 )
    {
        retval=TRUE;
        goto done;
    }

    /* Releasing the last reference : call dlclose(), remove module from the
       process-wide module list */

    TRACE("Reference count for module %p (named %S) now 0; destroying "
            "module structure.\n", module, MODNAME(module));

    /* unlink the module structure from the list */
    module->prev->next = module->next;
    module->next->prev = module->prev;

    /* remove the circular reference so that LOADValidateModule will fail */
    module->self=NULL;

    /* Call DllMain if the module contains one */
    if(module->pDllMain)
    {
        TRACE("Calling DllMain (%p) for module %S\n",
                module->pDllMain, 
                module->lib_name ? module->lib_name : W16_NULLSTRING);

/* reset ENTRY nesting level back to zero while inside the callback... */
#if !_NO_DEBUG_MESSAGES_
    {
        int old_level;
        old_level = DBG_change_entrylevel(0);
#endif /* !_NO_DEBUG_MESSAGES_ */
    
        {
            // This module may be foreign to our PAL, so leave our PAL.
            // If it depends on us, it will re-enter.
            PAL_LeaveHolder holder;
            module->pDllMain((HMODULE)module, DLL_PROCESS_DETACH, NULL);
        }

/* ...and set nesting level back to what it was */
#if !_NO_DEBUG_MESSAGES_
        DBG_change_entrylevel(old_level);
    }
#endif /* !_NO_DEBUG_MESSAGES_ */
    }

    if(module->dl_handle && 0!=dlclose(module->dl_handle))
    {
        /* report dlclose() failure, but proceed anyway. */
        WARN("dlclose() call failed! error message is \"%s\"\n", dlerror());
    }

    pThread = InternalGetCurrentThread();
    /* release all memory */
    InternalFree(pThread, module->lib_name);
    InternalFree(pThread, module);

    retval=TRUE;

done:
    UnlockModuleList();
    LOGEXIT("FreeLibrary returns BOOL %d\n", retval);
    PERF_EXIT(FreeLibrary);
    return retval;
}


/*++
Function:
  FreeLibraryAndExitThread

See MSDN doc.

--*/
PALIMPORT
VOID
PALAPI
FreeLibraryAndExitThread(
             IN HMODULE hLibModule,
             IN DWORD dwExitCode)
{
    PERF_ENTRY(FreeLibraryAndExitThread);
    ENTRY("FreeLibraryAndExitThread()\n"); 
    FreeLibrary(hLibModule);
    ExitThread(dwExitCode);
    LOGEXIT("FreeLibraryAndExitThread\n");
    PERF_EXIT(FreeLibraryAndExitThread);
}


/*++
Function:
  GetModuleFileNameA

See MSDN doc.

Notes :
    because of limitations in the dlopen() mechanism, this will only return the
    full path name if a relative or absolute path was given to LoadLibrary, or
    if the module was used in a GetProcAddress call. otherwise, this will return
    the short name as given to LoadLibrary. The exception is if hModule is
    NULL : in this case, the full path of the executable is always returned.
--*/
DWORD
PALAPI
GetModuleFileNameA(
           IN HMODULE hModule,
           OUT LPSTR lpFileName,
           IN DWORD nSize)
{
    INT name_length;
    DWORD retval=0;
    LPWSTR wide_name = NULL;

    PERF_ENTRY(GetModuleFileNameA);
    ENTRY("GetModuleFileNameA (hModule=%p, lpFileName=%p, nSize=%u)\n",
          hModule, lpFileName, nSize);

    LockModuleList();
    if(hModule && !LOADValidateModule((MODSTRUCT *)hModule))
    {
        TRACE("Can't find name for invalid module handle %p\n", hModule);
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }
    wide_name=LOADGetModuleFileName((MODSTRUCT *)hModule);

    if(!wide_name)
    {
        ASSERT("Can't find name for valid module handle %p\n", hModule);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    /* Convert module name to Ascii, place it in the supplied buffer */

    name_length = WideCharToMultiByte(CP_ACP, 0, wide_name, -1, lpFileName,
                                      nSize, NULL, NULL);
    if( name_length==0 )
    {
        TRACE("Buffer too small to copy module's file name.\n");
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto done;
    }

    TRACE("File name of module %p is %s\n", hModule, lpFileName);
    retval=name_length;
done:
    UnlockModuleList();
    LOGEXIT("GetModuleFileNameA returns DWORD %d\n", retval);
    PERF_EXIT(GetModuleFileNameA);
    return retval;
}


/*++
Function:
  GetModuleFileNameW

See MSDN doc.

Notes :
    because of limitations in the dlopen() mechanism, this will only return the
    full path name if a relative or absolute path was given to LoadLibrary, or
    if the module was used in a GetProcAddress call. otherwise, this will return
    the short name as given to LoadLibrary. The exception is if hModule is
    NULL : in this case, the full path of the executable is always returned.
--*/
DWORD
PALAPI
GetModuleFileNameW(
           IN HMODULE hModule,
           OUT LPWSTR lpFileName,
           IN DWORD nSize)
{
    INT name_length;
    DWORD retval=0;
    LPWSTR wide_name = NULL;

    PERF_ENTRY(GetModuleFileNameW);
    ENTRY("GetModuleFileNameW (hModule=%p, lpFileName=%p, nSize=%u)\n",
          hModule, lpFileName, nSize);

    LockModuleList();

    if(hModule && !LOADValidateModule((MODSTRUCT *)hModule))
    {
        TRACE("Can't find name for invalid module handle %p\n", hModule);
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }
    wide_name=LOADGetModuleFileName((MODSTRUCT *)hModule);

    if(!wide_name)
    {
        ASSERT("Can't find name for valid module handle %p\n", hModule);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    /* Copy module name into supplied buffer */

    name_length = lstrlenW(wide_name);
    if(name_length>=(INT)nSize)
    {
        TRACE("Buffer too small to copy module's file name.\n");
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto done;
    }
    
    wcscpy_s(lpFileName, nSize, wide_name);

    TRACE("file name of module %p is %S\n", hModule, lpFileName);
    retval=name_length;
done:
    UnlockModuleList();
    LOGEXIT("GetModuleFileNameW returns DWORD %u\n", retval);
    PERF_EXIT(GetModuleFileNameW);
    return retval;
}

/*++
Function:
  PAL_RegisterLibraryW

  Same as LoadLibraryW, but with only the base name of
  the library instead of a full filename.
--*/

HMODULE
PALAPI
PAL_RegisterLibraryW(
         IN LPCWSTR lpLibFileName)
{
    HMODULE hModule = NULL;
    CHAR    lpstr[MAX_PATH];
    INT     cbMultiByteShortName = 0;

    static const char LIB_PREFIX[] = PAL_SHLIB_PREFIX;
    static const char LIB_SUFFIX[] = PAL_SHLIB_SUFFIX;
    static const int LIB_PREFIX_LENGTH = sizeof(LIB_PREFIX) - 1;
    static const int LIB_SUFFIX_LENGTH = sizeof(LIB_SUFFIX) - 1;

    PERF_ENTRY(PAL_RegisterLibraryW);
    ENTRY("PAL_RegisterLibraryW (lpLibFileName=%p (%S)) \n",
          lpLibFileName?lpLibFileName:W16_NULLSTRING,
          lpLibFileName?lpLibFileName:W16_NULLSTRING);

    // First, copy the prefix into the buffer
    if (strcpy_s(lpstr, sizeof(lpstr), LIB_PREFIX) != SAFECRT_SUCCESS)
    {
        ERROR("strcpy_s failed!\n");
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto Done;
    }

    // Second, copy the file name, converting to multibyte along the way
    cbMultiByteShortName = WideCharToMultiByte(CP_ACP, 0, lpLibFileName, -1, 
                                               lpstr + LIB_PREFIX_LENGTH, 
                                               MAX_PATH - (LIB_PREFIX_LENGTH + LIB_SUFFIX_LENGTH),
                                               NULL, NULL);

    if (cbMultiByteShortName == 0)
    {
        DWORD dwLastError = GetLastError();
        if (dwLastError == ERROR_INSUFFICIENT_BUFFER)
        {
            if (lstrlenW(lpLibFileName) + LIB_PREFIX_LENGTH + LIB_SUFFIX_LENGTH < MAX_PATH)
            {
                ASSERT("Insufficient buffer error returned incorrectly from WideCharToMultiByte!\n");
            }
            ERROR("lpLibFileName is larger than MAX_PATH (%d)!\n", MAX_PATH);
        }
        else
        {
            ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        }
        SetLastError(ERROR_INVALID_PARAMETER);
        goto Done;
    }

    // Last, add the suffix
    if (strcat_s(lpstr, sizeof(lpstr), LIB_SUFFIX) != SAFECRT_SUCCESS)
    {
        ERROR("strcat_s failed!\n");
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto Done;
    }

    FILEDosToUnixPathA(lpstr);

    hModule = LOADLoadLibrary(lpstr, FALSE);

Done:
    LOGEXIT("PAL_RegisterLibraryW returns HMODULE %p\n", hModule);
    PERF_EXIT(PAL_RegisterLibraryW);
    return hModule;
}


/*++
Function:
  PAL_UnregisterLibraryW

  Same as FreeLibrary.
--*/
BOOL
PALAPI
PAL_UnregisterLibraryW(
        IN OUT HMODULE hLibModule)
{
    BOOL retval;

    PERF_ENTRY(PAL_UnregisterLibraryW);
    ENTRY("PAL_UnregisterLibraryW (hLibModule=%p)\n", hLibModule);

    retval = FreeLibrary(hLibModule);

    LOGEXIT("PAL_UnregisterLibraryW returns BOOL %d\n", retval);
    PERF_EXIT(PAL_UnregisterLibraryW);
    return retval;
}

/* Internal PAL functions *****************************************************/

/*++
    LOADGetLibRotorPalSoFileName

    Search LD_LIBRARY_PATH (or DYLD_LIBRARY_PATH) for LibRotorPal.  This 
    defines the working directory for PAL.

Parameters:
    OUT LPSTR pszBuf - WCHAR buffer of MAX_PATH length to receive file name

Return value:
    0 if successful
    -1 if failure, with last error set.
--*/
extern "C"
int LOADGetLibRotorPalSoFileName(LPSTR pszBuf)
{
    INT     iRetVal = -1;
    CHAR*   pszFileName = NULL;
    CPalThread *pthrThread = InternalGetCurrentThread();

    if (!pszBuf)
    {
        ASSERT("LOADGetLibRotorPalSoFileName requires non-NULL pszBuf\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto Done;
    }
    iRetVal = FindLibrary((CHAR*)MAKEDLLNAME_A("CoreClrPal"), &pszFileName);
    if (pszFileName)
    {
        UINT cchFileName = strlen(pszFileName);
        if (cchFileName + 1  > MAX_PATH)
        {
            ASSERT("Filename returned by FindLibrary was longer than"
                "MAX_PATH!\n");
            SetLastError(ERROR_FILENAME_EXCED_RANGE);
            goto Done;
        }
        // If the path is relative, get current working directory and prepend 
        // it (Note that this function is called only on PAL startup, so 
        // current working directory should still be correct)
        if (pszFileName[0] != '/')
        {
            CHAR    szCurDir[MAX_PATH];
            CHAR*   pszRetVal = NULL;
            if ((pszRetVal = InternalGetcwd(pthrThread, szCurDir, MAX_PATH)) == NULL)
            {
                SetLastError(DIRGetLastErrorFromErrno());
                goto Done;
            }
            // If the strings would overflow (note that if the sum of the 
            // lengths == MAX_PATH, the string would overflow b/c of the null
            // terminator -- the 1 is added to account for the /)
            if ((strlen(szCurDir) + strlen(pszFileName) + 1) >= MAX_PATH)
            {
                SetLastError(ERROR_FILENAME_EXCED_RANGE);
                goto Done;
            }
            strcat_s(pszBuf, MAX_PATH, szCurDir);
            strcat_s(pszBuf, MAX_PATH,  "/");
            strcat_s(pszBuf, MAX_PATH,  pszFileName);
        }
        else
        {
            strcpy_s(pszBuf, MAX_PATH, pszFileName);
        }
        iRetVal = 0;        
    }
Done:
    if (pszFileName)
    {
        InternalFree(pthrThread, pszFileName);
    }
    return iRetVal;
}

/*++
Function :
    LOADInitializeModules

    Initialize the process-wide list of modules (2 initial modules : 1 for
    the executable and 1 for the PAL)

Parameters :
    LPWSTR exe_name : full path to executable

Return value:
    TRUE  if initialization succeedded
    FALSE otherwise

Notes :
    the module manager takes ownership of the exe_name string
--*/
extern "C"
BOOL LOADInitializeModules(LPWSTR exe_name)
{
    LPWSTR  lpwstr = NULL;

#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    LPSTR   pszExeName = NULL;
    CPalThread *pThread = NULL;
#endif

    BOOL    fRetCode = FALSE;

    if(exe_module.prev)
    {
        ERROR("Module manager already initialized!\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto Done;
    }

    InternalInitializeCriticalSection(&module_critsec);

    /* initialize module for main executable */
    TRACE("Initializing module for main executable\n");
    exe_module.self=(HMODULE)&exe_module;
    exe_module.dl_handle=dlopen(NULL, RTLD_LAZY);
    if(!exe_module.dl_handle)
    {
        ASSERT("Main executable module will be broken : dlopen(NULL) failed. "
             "dlerror message is \"%s\" \n", dlerror());
    }
    exe_module.lib_name = exe_name;
    exe_module.refcount=-1;
    exe_module.next=&pal_module;
    exe_module.prev=&pal_module;
    exe_module.pDllMain = NULL;
    exe_module.ThreadLibCalls = TRUE;
    
    TRACE("Initializing module for PAL library\n");
    pal_module.self=(HANDLE)&pal_module;

    if (g_szCoreCLRPath[0] == '\0')
    {
        pal_module.lib_name=NULL;
        pal_module.dl_handle=NULL;
    } else
    {
        TRACE("PAL library is %s\n", g_szCoreCLRPath);
        lpwstr = UTIL_MBToWC_Alloc(g_szCoreCLRPath, -1);
        if(NULL == lpwstr)
        {
            ERROR("MBToWC failure, unable to save full name of PAL module\n");
            goto Done;
        }
        pal_module.lib_name=lpwstr;
        pal_module.dl_handle=dlopen(g_szCoreCLRPath, RTLD_LAZY);

        if(pal_module.dl_handle)
        {
            g_pRuntimeDllMain = (PDLLMAIN)dlsym(pal_module.dl_handle, "CoreDllMain");
        }
        else
        {
#if !defined(__hppa__)
            ASSERT("PAL module will be broken : dlopen(%s) failed. dlerror "
                 "message is \"%s\"\n ", g_szCoreCLRPath, dlerror());
#endif
        }
    }

    pal_module.refcount=-1;
    pal_module.next=&exe_module;
    pal_module.prev=&exe_module;
    pal_module.pDllMain = NULL;
    pal_module.ThreadLibCalls = TRUE;

    // For platforms where we can't trust the handle to be constant, we need to 
    // store the inode/device pairs for the modules we just initialized.
#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    {
        struct stat stat_buf;
        pszExeName = UTIL_WCToMB_Alloc(exe_name, -1);
        if (NULL == pszExeName)
        {
            ERROR("WCToMB failure, unable to get full name of exe\n");
            goto Done;
        }
        if ( -1 == stat(pszExeName, &stat_buf))
        {
            SetLastError(ERROR_MOD_NOT_FOUND);
            goto Done;
        }

        TRACE("Executable has inode %d and device %d\n", 
            stat_buf.st_ino, stat_buf.st_dev);

        exe_module.inode = stat_buf.st_ino; 
        exe_module.device = stat_buf.st_dev;
        if ( -1 == stat(librotor_fname, &stat_buf))
        {
            SetLastError(ERROR_MOD_NOT_FOUND);
            goto Done;
        }

        TRACE("PAL Library has inode %d and device %d\n", 
            stat_buf.st_ino, stat_buf.st_dev);

        pal_module.inode = stat_buf.st_ino; 
        pal_module.device = stat_buf.st_dev;
    }
#endif

    // If we got here, init succeeded.
    fRetCode = TRUE;
 Done:
    if (!fRetCode && GetLastError() == ERROR_SUCCESS)
    {
        ASSERT("returning failure, but last error not set\n");
    }

#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    pThread = InternalGetCurrentThread();
    if (pszExeName)
        InternalFree(pThread, pszExeName);
#endif
    TRACE("Module manager initialization returning %d.\n", fRetCode);
    return fRetCode;
}

/*++
Function :
    LOADFreeModules

    Release all resources held by the module manager (including dlopen handles)

Parameters:
    BOOL bTerminateUnconditionally: If TRUE, this will avoid calling any DllMains

    (no return value)
--*/
extern "C"
void LOADFreeModules(BOOL bTerminateUnconditionally)
{
    MODSTRUCT *module;
    CPalThread *pThread = InternalGetCurrentThread();

    if(!exe_module.prev)
    {
        ERROR("Module manager not initialized!\n");
        return;
    }

    LockModuleList();

    /* Go through the list of modules, release any references we still hold.
       The list is traversed from newest module to oldest */
    do
    {
        module = exe_module.prev;

        // Call DllMain if the module contains one and if we're supposed
        // to call DllMains.
        if( !bTerminateUnconditionally && module->pDllMain )
        {
           /* Exception-safe call to DllMain */
           LOAD_SEH_CallDllMain( module, DLL_PROCESS_DETACH, (LPVOID)-1 );
        }

        /* Remove the current MODSTRUCT from the list, then free its memory */
        module->prev->next = module->next;
        module->next->prev = module->prev;
        module->self = NULL;

        if (module->dl_handle)
            dlclose( module->dl_handle );

        InternalFree( pThread, module->lib_name );
        module->lib_name = NULL;
        if (module != &exe_module && module != &pal_module)
        {
            InternalFree( pThread, module );
        }
    }
    while( module != &exe_module );

    /* Flag the module manager as uninitialized */
    exe_module.prev = NULL;

    TRACE("Module manager stopped.\n");

    UnlockModuleList();
    DeleteCriticalSection(&module_critsec);
}

/*++
Function :
    LOADCallDllMain

    Call DllMain for all modules (that have one) with the given "fwReason"

Parameters :
    DWORD dwReason : parameter to pass down to DllMain, one of DLL_PROCESS_ATTACH, DLL_PROCESS_DETACH, 
        DLL_THREAD_ATTACH, DLL_THREAD_DETACH

    LPVOID lpReserved : parameter to pass down to DllMain
        If dwReason is DLL_PROCESS_ATTACH, lpvReserved is NULL for dynamic loads and non-NULL for static loads.
        If dwReason is DLL_PROCESS_DETACH, lpvReserved is NULL if DllMain has been called by using FreeLibrary 
            and non-NULL if DllMain has been called during process termination. 

(no return value)

Notes :
    This is used to send DLL_THREAD_*TACH messages to modules
--*/
extern "C"
void LOADCallDllMain(DWORD dwReason, LPVOID lpReserved)
{
    MODSTRUCT *module = NULL;
    BOOL InLoadOrder = TRUE; /* true if in load order, false for reverse */
    CPalThread *pThread;
    
    pThread = InternalGetCurrentThread();
    if (UserCreatedThread != pThread->GetThreadType())
    {
        return;
    }

    /* Validate dwReason */
    switch(dwReason)
    {
    case DLL_PROCESS_ATTACH: 
        ASSERT("got called with DLL_PROCESS_ATTACH parameter! Why?\n");
        break;
    case DLL_PROCESS_DETACH:
        ASSERT("got called with DLL_PROCESS_DETACH parameter! Why?\n");
        InLoadOrder = FALSE;
        break;
    case DLL_THREAD_ATTACH:
        TRACE("Calling DllMain(DLL_THREAD_ATTACH) on all known modules.\n");
        break;
    case DLL_THREAD_DETACH:
        TRACE("Calling DllMain(DLL_THREAD_DETACH) on all known modules.\n");
        InLoadOrder = FALSE;
        break;
    default:
        ASSERT("LOADCallDllMain called with unknown parameter %d!\n", dwReason);
        return;
    }

    LockModuleList();

    module = &exe_module;
    do {
        if (!InLoadOrder)
            module = module->prev;

        if (module->ThreadLibCalls)
        {
            if(module->pDllMain)
            {
#if !_NO_DEBUG_MESSAGES_
                /* reset ENTRY nesting level back to zero while inside the callback... */
                int old_level;
                old_level = DBG_change_entrylevel(0);
#endif /* !_NO_DEBUG_MESSAGES_ */

                {
                    // This module may be foreign to our PAL, so leave our PAL.
                    // If it depends on us, it will re-enter.
                    PAL_LeaveHolder holder;
                    module->pDllMain((HMODULE) module, dwReason, lpReserved);
                }

#if !_NO_DEBUG_MESSAGES_
                /* ...and set nesting level back to what it was */
                DBG_change_entrylevel(old_level);
#endif /* !_NO_DEBUG_MESSAGES_ */
            }
        }

        if (InLoadOrder)
            module = module->next;
    } while (module != &exe_module);

    UnlockModuleList();
}


/*++
Function:
    DisableThreadLibraryCalls

See MSDN doc.
--*/
BOOL
PALAPI
DisableThreadLibraryCalls(
    IN HMODULE hLibModule)
{
    BOOL ret = FALSE;
    MODSTRUCT *module;
    PERF_ENTRY(DisableThreadLibraryCalls);
    ENTRY("DisableThreadLibraryCalls(hLibModule=%p)\n", hLibModule);

    if (terminator)
    {
        /* PAL shutdown in progress - ignore DisableThreadLibraryCalls */
        ret = TRUE;
        goto done_nolock;
    }

    LockModuleList();
    module = (MODSTRUCT *) hLibModule;

    if(!module || !LOADValidateModule(module))
    {
        // DisableThreadLibraryCalls() does nothing when given
        // an invalid module handle. This matches the Windows
        // behavior, though it is counter to MSDN.
        WARN("Invalid module handle %p\n", hLibModule);
        ret = TRUE;
        goto done;
    }

    module->ThreadLibCalls = FALSE;
    ret = TRUE;

done:
    UnlockModuleList();
done_nolock:
    LOGEXIT("DisableThreadLibraryCalls returns BOOL %d\n", ret);
    PERF_EXIT(DisableThreadLibraryCalls);
    return ret;
}


/* Static function definitions ************************************************/

/*++
Function :
    LOADValidateModule

    Check whether the given MODSTRUCT pointer is valid

Parameters :
    MODSTRUCT *module : module to check

Return value :
    TRUE if module is valid, FALSE otherwise

--*/
static BOOL LOADValidateModule(MODSTRUCT *module)
{
    MODSTRUCT *modlist_enum;

    LockModuleList();

    modlist_enum=&exe_module;

    /* enumerate through the list of modules to make sure the given handle is
       really a module (HMODULEs are actually MODSTRUCT pointers) */
    do 
    {
        if(module==modlist_enum)
        {
            /* found it; check its integrity to be on the safe side */
            if(module->self!=module)
            {
                ERROR("Found corrupt module %p!\n",module);
                UnlockModuleList();
                return FALSE;
            }
            UnlockModuleList();
            TRACE("Module %p is valid (name : %S)\n", module,
                  MODNAME(module));
            return TRUE;
        }
        modlist_enum = modlist_enum->next;
    }
    while (modlist_enum != &exe_module);

    TRACE("Module %p is NOT valid.\n", module);
    UnlockModuleList();
    return FALSE;
}

/*++
Function :
    LOADGetModuleFileName [internal]

    Retrieve the module's full path if it is known, the short name given to
    LoadLibrary otherwise.

Parameters :
    MODSTRUCT *module : module to check

Return value :
    pointer to internal buffer with name of module (Unicode)

Notes :
    this function assumes that the module critical section is held, and that
    the module has already been validated.
--*/
static LPWSTR LOADGetModuleFileName(MODSTRUCT *module)
{
    LPWSTR module_name;
    /* special case : if module is NULL, we want the name of the executable */
    if(!module)
    {
        module_name = exe_module.lib_name;
        TRACE("Returning name of main executable\n");
        return module_name;
    }

    /* return "real" name of module if it is known. we have this if LoadLibrary
       was given an absolute or relative path; we can also determine it at the
       first GetProcAdress call. */
    TRACE("Returning full path name of module\n");
    return module->lib_name;
}

/*++
Function :
    LOADAllocModule

    Allocate and initialize a new MODSTRUCT structure

Parameters :
    void *dl_handle :   handle returned by dl_open, goes in MODSTRUCT::dl_handle
    
    char *name :        name of new module. after conversion to widechar, 
                        goes in MODSTRUCT::lib_name
                        
Return value:
    a pointer to a new, initialized MODSTRUCT strucutre, or NULL on failure.
    
Notes :
    'name' is used to initialize MODSTRUCT::lib_name. The other member is set to NULL
    In case of failure (in malloc or MBToWC), this function sets LastError.
--*/
static MODSTRUCT *LOADAllocModule(void *dl_handle, LPCSTR name)
{   
    MODSTRUCT *module;
    LPWSTR wide_name;
    CPalThread* pThread = NULL;

    pThread = InternalGetCurrentThread();	
    /* no match found : try to create a new module structure */
    module=(MODSTRUCT *) InternalMalloc(pThread, sizeof(MODSTRUCT));
    if(!module)
    {
        ERROR("malloc() failed! errno is %d (%s)\n", errno, strerror(errno));
        return NULL;
    }

    wide_name = UTIL_MBToWC_Alloc(name, -1);
    if(NULL == wide_name)
    {
        ERROR("couldn't convert name to a wide-character string\n");
        InternalFree(pThread, module);
        return NULL;
    }

    module->dl_handle = dl_handle;
#if NEED_DLCOMPAT
    if (isdylib(module))
    {
        module->refcount = -1;
    }
    else
    {
        module->refcount = 1;
    }
#else   // NEED_DLCOMPAT
    module->refcount = 1;
#endif  // NEED_DLCOMPAT
    module->self = module;
    module->ThreadLibCalls = TRUE;
    module->next = NULL;
    module->prev = NULL;

    module->lib_name = wide_name;

    return module;
}

/*++
Function :
    LOADLoadLibrary [internal]

    implementation of LoadLibrary (for use by the A/W variants)

Parameters :
    LPSTR ShortAsciiName : name of module as specified to LoadLibrary

    BOOL fDynamic : TRUE if dynamic load through LoadLibrary, FALSE if static load through RegisterLibrary

Return value :
    handle to loaded module

--*/
static HMODULE LOADLoadLibrary(LPCSTR ShortAsciiName, BOOL fDynamic)
{
    CHAR fullLibraryName[MAX_PATH];
    MODSTRUCT *module = NULL;
    void *dl_handle;
    DWORD dwError;

    // Check whether we have been requested to load 'libc'. If that's the case then use the
    // full name of the library that is defined in <gnu/lib-names.h> by the LIBC_SO constant.
    // The problem is that calling dlopen("libc.so") will fail for libc even thought it works
    // for other libraries. The reason is that libc.so is just linker script (i.e. a test file).
    // As a result, we have to use the full name (i.e. lib.so.6) that is defined by LIBC_SO.
    if (strcmp(ShortAsciiName, LIBC_NAME_WITHOUT_EXTENSION) == 0)
    {
#if defined(__APPLE__)
        ShortAsciiName = "libc.dylib";
#elif defined(__FreeBSD__)
        ShortAsciiName = "libc.so";
#else
        ShortAsciiName = LIBC_SO;
#endif
    }

    LockModuleList();

    /* see if file can be dlopen()ed; this should work even if it's already
        loaded */

    {
        // See GetProcAddress for an explanation why we leave the PAL.
        PAL_LeaveHolder holder;
        dl_handle = dlopen(ShortAsciiName, RTLD_LAZY);

        // P/Invoke calls are often defined without an extension in the name of the 
        // target library. So if we failed to load the specified library, try adding
        // a proper extension and load the library again.
        if (!dl_handle)
        {
            if (snprintf(fullLibraryName, MAX_PATH, "%s%s", ShortAsciiName, PAL_SHLIB_SUFFIX) < MAX_PATH)
            {
                dl_handle = dlopen(fullLibraryName, RTLD_LAZY);
                if (dl_handle)
                {
                    ShortAsciiName = fullLibraryName;
                }
            }
        }
    }

    if (!dl_handle)
    {
        WARN("dlopen() failed; dlerror says '%s'\n", dlerror()); 
        SetLastError(ERROR_MOD_NOT_FOUND);
        goto done;
    }
    TRACE("dlopen() found module %s\n", ShortAsciiName);


#if !RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    /* search module list for a match. */
    module = &exe_module;
    do
    {
        if (dl_handle == module->dl_handle)
        {   
            /* found the handle. increment the refcount and return the 
               existing module structure */
            TRACE("Found matching module %p for module name %s\n",
                 module, ShortAsciiName);
            if (module->refcount != -1)
                module->refcount++;
            dlclose(dl_handle);
            goto done;
        }
        module = module->next;
    } while (module != &exe_module);
#endif

    TRACE("Module doesn't exist : creating %s.\n", ShortAsciiName);
    module = LOADAllocModule(dl_handle, ShortAsciiName);

    if(NULL == module)
    {
        ERROR("couldn't create new module\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        dlclose(dl_handle);
        goto done;
    }

    /* Add the new module on to the end of the list */
    module->prev = exe_module.prev;
    module->next = &exe_module;
    exe_module.prev->next = module;
    exe_module.prev = module;

#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    module->inode = stat_buf.st_ino; 
    module->device = stat_buf.st_dev;
#endif

    /* If we get here, then we have created a new module structure. We can now
       get the address of DllMain if the module contains one. We save
       the last error and restore it afterward, because our caller doesn't
       care about GetProcAddress failures. */
    dwError = GetLastError();

    module->pDllMain = (PDLLMAIN)GetProcAddress((HMODULE)module, "DllMain");

    SetLastError(dwError);

    /* If it did contain a DllMain, call it. */
    if(module->pDllMain)
    {
        DWORD dllmain_retval;

        TRACE("Calling DllMain (%p) for module %S\n", 
              module->pDllMain, 
              module->lib_name ? module->lib_name : W16_NULLSTRING);

        {
#if !_NO_DEBUG_MESSAGES_
            /* reset ENTRY nesting level back to zero while inside the callback... */
            int old_level;
            old_level = DBG_change_entrylevel(0);
#endif /* !_NO_DEBUG_MESSAGES_ */

            {
                // This module may be foreign to our PAL, so leave our PAL.
                // If it depends on us, it will re-enter.
                PAL_LeaveHolder holder;
                dllmain_retval = module->pDllMain((HINSTANCE) module,
                    DLL_PROCESS_ATTACH, fDynamic ? NULL : (LPVOID)-1);
            }

#if !_NO_DEBUG_MESSAGES_
            /* ...and set nesting level back to what it was */
            DBG_change_entrylevel(old_level);
#endif /* !_NO_DEBUG_MESSAGES_ */
        }

        /* If DlMain(DLL_PROCESS_ATTACH) returns FALSE, we must immediately
           unload the module.*/
        if(FALSE == dllmain_retval)
        {
            TRACE("DllMain returned FALSE; unloading module.\n");
            module->pDllMain = NULL;
            FreeLibrary((HMODULE) module);
            ERROR("DllMain failed and returned NULL. \n");
            SetLastError(ERROR_DLL_INIT_FAILED);
            module = NULL;
        }
    }
    else
    {
        TRACE("Module does not contain a DllMain function.\n");
    }

done:
    UnlockModuleList();
    return (HMODULE)module;
}

/*++
Function :
    LOAD_SEH_CallDllMain

    Exception-safe call to DllMain.

Parameters :
    MODSTRUCT *module : module whose DllMain must be called

    DWORD dwReason : parameter to pass down to DllMain, one of DLL_PROCESS_ATTACH, DLL_PROCESS_DETACH, 
        DLL_THREAD_ATTACH, DLL_THREAD_DETACH

    LPVOID lpvReserved : parameter to pass down to DllMain,
        If dwReason is DLL_PROCESS_ATTACH, lpvReserved is NULL for dynamic loads and non-NULL for static loads. 
        If dwReason is DLL_PROCESS_DETACH, lpvReserved is NULL if DllMain has been called by using FreeLibrary 
            and non-NULL if DllMain has been called during process termination. 

(no return value)

Notes :
This function is called from LOADFreeModules. Since we get there from
PAL_Terminate, we can't let exceptions in DllMain go unhandled :
TerminateProcess would be called, and would have to abort uncleanly because
termination was already started. So we catch the exception and ignore it;
we're terminating anyway.
*/
static void LOAD_SEH_CallDllMain(MODSTRUCT *module, DWORD dwReason, LPVOID lpReserved)
{
#if !_NO_DEBUG_MESSAGES_
    /* reset ENTRY nesting level back to zero while inside the callback... */
    int old_level = DBG_change_entrylevel(0);
#endif /* !_NO_DEBUG_MESSAGES_ */
    
    struct Param
    {
        MODSTRUCT *module;
        DWORD dwReason;
        LPVOID lpReserved;
    } param;
    param.module = module;
    param.dwReason = dwReason;
    param.lpReserved = lpReserved;

    PAL_TRY(Param *, pParam, &param)
    {
        TRACE("Calling DllMain (%p) for module %S\n",
              pParam->module->pDllMain, 
              pParam->module->lib_name ? pParam->module->lib_name : W16_NULLSTRING);
        
        {
            // This module may be foreign to our PAL, so leave our PAL.
            // If it depends on us, it will re-enter.
            PAL_LeaveHolder holder;
            pParam->module->pDllMain((HMODULE)pParam->module, pParam->dwReason, pParam->lpReserved);
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        WARN("Call to DllMain (%p) got an unhandled exception; "
              "ignoring.\n", module->pDllMain);
    }
    PAL_ENDTRY

#if !_NO_DEBUG_MESSAGES_
    /* ...and set nesting level back to what it was */
    DBG_change_entrylevel(old_level);
#endif /* !_NO_DEBUG_MESSAGES_ */
}

/*++
Function:
  LockModuleList

Abstract
  Enter the critical section associated to the module list

Parameter
  void

Return
  void
--*/
extern "C"
void LockModuleList()
{
    CPalThread * pThread = 
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : NULL);

    InternalEnterCriticalSection(pThread , &module_critsec);
}

/*++
Function:
  UnlockModuleList

Abstract
  Leave the critical section associated to the module list

Parameter
  void

Return
  void
--*/
extern "C"
void UnlockModuleList()
{
    CPalThread * pThread = 
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : NULL);

    InternalLeaveCriticalSection(pThread , &module_critsec);
}

/*++
    PAL_LOADLoadPEFile

    Map a PE format file into memory like Windows LoadLibrary() would do.
    Doesn't apply base relocations if the function is relocated.

Parameters:
    IN hFile - file to map

Return value:
    non-NULL - the base address of the mapped image
    NULL - error, with last error set.
--*/

void * PAL_LOADLoadPEFile(HANDLE hFile)
{
    ENTRY("PAL_LOADLoadPEFile (hFile=%p)\n", hFile);

    void * loadedBase = MAPMapPEFile(hFile);

#ifdef _DEBUG
    if (loadedBase != NULL)
    {
        char* envVar = getenv("PAL_ForcePEMapFailure");
        if (envVar && strlen(envVar) > 0)
        {
            TRACE("Forcing failure of PE file map, and retry\n");
            PAL_LOADUnloadPEFile(loadedBase); // unload it
            loadedBase = MAPMapPEFile(hFile); // load it again
        }
    }
#endif // _DEBUG

    LOGEXIT("PAL_LOADLoadPEFile returns %p\n", loadedBase);
    return loadedBase;
}


/*++
    PAL_LOADUnloadPEFile

    Unload a PE file that was loaded by PAL_LOADLoadPEFile().

Parameters:
    IN ptr - the file pointer returned by PAL_LOADLoadPEFile()

Return value:
    TRUE - success
    FALSE - failure (incorrect ptr, etc.)
--*/

BOOL PAL_LOADUnloadPEFile(void * ptr)
{
    BOOL retval = FALSE;

    ENTRY("PAL_LOADUnloadPEFile (ptr=%p)\n", ptr);

    if (NULL == ptr)
    {
        ERROR( "Invalid pointer value\n" );
    }
    else
    {
        retval = MAPUnmapPEFile(ptr);
    }

    LOGEXIT("PAL_LOADUnloadPEFile returns %d\n", retval);
    return retval;
}

/*++
Function:
  FindLibrary

Abstract
    Search LD_LIBRARY_PATH/DYLD_LIBRARY_PATH for a file named pszRelName

Parameter
    pszRelName: The relative name of the file sought
    ppszFullName: A pointer that will be filled in with the full filename if
        we find it

Return
    0 if completed successfully, even if library not found
    -1 on error
--*/
INT FindLibrary(CHAR* pszRelName, CHAR** ppszFullName)
{
    CPalThread *pThread = NULL;
    CHAR*   pszLibPath = NULL;
    CHAR*   pszNext = NULL;
    CHAR**  rgpLibDirSeparators = NULL;
    UINT    cSeparators = 0;
    UINT    iSeparator = 0;
    UINT    iStringLen = 0;
    INT     iRetVal = 0;
    CHAR*   pszSearchPath = NULL;
    BOOL    fSearchPathNeedsFreeing = FALSE;

    if (!ppszFullName)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        iRetVal = -1;
        goto Done;
    }
    *ppszFullName = NULL;

    // First, get the LD_LIBRARY_PATH to figure out where to look
    // Note that pszLibPath points to system memory -- don't free.
    pszLibPath = MiscGetenv(LIBSEARCHPATH);
    if (!pszLibPath)
    {
        TRACE("FindLibrary: " LIBSEARCHPATH " not set\n");
        pszLibPath = (char*)".";
    }
    else
    {
        TRACE("FindLibrary: " LIBSEARCHPATH " is %s\n", pszLibPath);
    }

    pThread = InternalGetCurrentThread();
    iStringLen = strlen(pszLibPath);

    // We want to make sure that we always search the current directory,
    // regardless of whether LD_LIBRARY_PATH includes it (this mimics
    // Windows behavior)
    if ( (!(strstr(pszLibPath, ":.:"))) && // if you don't find '.' in the middle
         (!(iStringLen == 1 && pszLibPath[0] == '.')) && // if it's not just equal to '.'
         (!(iStringLen >= 2 && pszLibPath[0] == '.' 
                            && pszLibPath[1] == ':')) && // if it doesn't start with ".:"
         (!(iStringLen >= 2 && pszLibPath[iStringLen-2] == ':' 
                            && pszLibPath[iStringLen-1] == '.')) ) // if it doesn't end with ":."
    {
        // 3 is hardcoded here for :. and null
        int iLen = sizeof(pszSearchPath[0]) * (iStringLen + 3);
        pszSearchPath = (char*) InternalMalloc (pThread, iLen);
        if (!pszSearchPath)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            iRetVal = -1;
            goto Done;
        }
        iStringLen += 3; // This 3 is hard coded for :. and null
        fSearchPathNeedsFreeing = TRUE;
        if (strcpy_s(pszSearchPath, iLen, pszLibPath) != SAFECRT_SUCCESS)
        {
            ERROR("strcpy_s failed!\n");
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            goto Done;
        }

        if (strcat_s(pszSearchPath, iLen, ":.") != SAFECRT_SUCCESS)
        {
            ERROR("strcat_s failed!\n");
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            goto Done;
        }
    }
    // If LD_LIBRARY_PATH already includes a reference to the current
    // directory, we'll search it in the right order.
    else
    {
        pszSearchPath = pszLibPath;
    }
      
    _ASSERTE(strchr(pszSearchPath, '.'));

    // Allocate an array for pointers to separators -- there can't be more than
    // the length of LD_LIBRARY_PATH - 1 separators (since we always have atleast a '.' in it )
    //                      + 2 implicit seperators...
    rgpLibDirSeparators = (char **) 
                InternalMalloc(pThread, sizeof(rgpLibDirSeparators[0]) * (iStringLen+1));
    if (!rgpLibDirSeparators)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        iRetVal = -1;
        goto Done;
    }

    // Now, find the separators in LD_LIBRARY_PATH.  Set a pointer to each :
    pszNext = pszSearchPath;
    // There's an implicit separator at the start...
    rgpLibDirSeparators[0] = pszNext - 1;
    cSeparators = 1;
    while (*pszNext != '\0')
    {
        if (*pszNext == ':')
        {
            _ASSERTE(cSeparators < iStringLen);
            rgpLibDirSeparators[cSeparators] = pszNext;
            cSeparators++;
        }
        pszNext++;
    }

    _ASSERTE(cSeparators <= iStringLen);
    // And there's an implicit separator at the end.
    rgpLibDirSeparators[cSeparators] = pszNext;
    cSeparators++;

    // Now, check each path for the File
    // Note that cSeparators is always >= 2, so the < -1 check is safe
    for (iSeparator = 0; iSeparator < (cSeparators-1); iSeparator++)
    {
        CHAR        szFileName[MAX_PATH + 1];
        CHAR        szDirName[MAX_PATH + 1];
        struct stat stat_buf;
        UINT        cchDirName = 0;

        // length of DirName is number of chars between the first char after 
        // the colon and the next colon
        cchDirName = rgpLibDirSeparators[iSeparator + 1] - 
                        (rgpLibDirSeparators[iSeparator] + 1);
        memcpy(szDirName, rgpLibDirSeparators[iSeparator] + 1, cchDirName);
        szDirName[cchDirName] = '\0';
        snprintf(szFileName, MAX_PATH, "%s/%s", szDirName, pszRelName);
        if (0 == stat(szFileName, &stat_buf))
        {
            // First, make sure we've got the canonical path
            CHAR   szRealPath[PATH_MAX + 1];

            if(!realpath(szFileName, szRealPath))
            {
                ASSERT("realpath() failed! problem path is %s\n", szFileName);
                SetLastError(ERROR_INTERNAL_ERROR);
                goto Done;
            }
            // We've found it.  Rejoice!
            TRACE("FindLibrary: found file: %s\n", szRealPath);
            *ppszFullName = InternalStrdup(pThread, szRealPath);
            if (!*ppszFullName)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                iRetVal = -1;
            }
            goto Done;
        }
    }

Done:
    if (rgpLibDirSeparators)
        InternalFree(pThread, rgpLibDirSeparators);
    if (fSearchPathNeedsFreeing)
        InternalFree(pThread, pszSearchPath);
    // Don't treat it as an error if the library's not found -- just set
    // *ppszFullName to NULL.
    return iRetVal;
}

/*++
    LOADInitCoreCLRModules

    Run the initialization methods for CoreCLR modules that used to be standalone dynamic libraries (PALRT and
    mscorwks).

Parameters:
    void

Return value:
    TRUE if successful
    FALSE if failure
--*/
BOOL LOADInitCoreCLRModules()
{
    return g_pRuntimeDllMain((HMODULE)&pal_module, DLL_PROCESS_ATTACH, NULL);
}

// Get base address of the module containing a given symbol 
PALAPI
LPCVOID
PAL_GetSymbolModuleBase(void *symbol)
{
    LPCVOID retval = NULL;

    PERF_ENTRY(PAL_GetPalModuleBase);
    ENTRY("PAL_GetPalModuleBase\n");

    if (symbol == NULL)
    {
        TRACE("Can't get base address. Argument symbol == NULL\n");
        SetLastError(ERROR_INVALID_DATA);
    }
    else 
    {
        Dl_info info;
        if (dladdr(symbol, &info) != 0)
        {
            retval = info.dli_fbase;
        }
        else 
        {
            TRACE("Can't get base address of the current module\n");
            SetLastError(ERROR_INVALID_DATA);
        }        
    }

    LOGEXIT("PAL_GetPalModuleBase returns %p\n", retval);
    PERF_EXIT(PAL_GetPalModuleBase);
    return retval;

}
