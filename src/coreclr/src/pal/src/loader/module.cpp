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

#include <gnu/lib-names.h>

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

#if defined(CORECLR) && defined(__APPLE__)
// Under CoreCLR/Mac the pal_module above actually represents the PAL, the PALRT and mscorwks (they're all
// linked into one binary). The PAL has no DllMain, but the other two do. Cache their DllMain entrypoints here
// so we can call them properly (e.g. thread attaches).
PDLLMAIN g_pPalRTDllMain = NULL;
#endif // CORECLR && __APPLE__

/* static function declarations ***********************************************/

static BOOL LOADValidateModule(MODSTRUCT *module);
static LPWSTR LOADGetModuleFileName(MODSTRUCT *module);
static HMODULE LOADLoadLibrary(LPCSTR ShortAsciiName, BOOL fDynamic);
static void LOAD_SEH_CallDllMain(MODSTRUCT *module, DWORD dwReason, LPVOID lpReserved);
static MODSTRUCT *LOADAllocModule(void *dl_handle, LPCSTR name);
#if !defined(CORECLR) || !defined(__APPLE__)
static INT FindLibrary(CHAR* pszRelName, CHAR** ppszFullName);
#endif // !CORECLR || !__APPLE__

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
#if !defined(CORECLR) || !defined(__APPLE__)
    LPCSTR symbolName = lpProcName;
#endif // !defined(CORECLR) || !defined(__APPLE__)

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
#if !defined(CORECLR) || !defined(__APPLE__)
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
#else // !CORECLR || !__APPLE__
    if (module == &pal_module)
    {
        // Attempting to lookup a symbol exported by the PAL/runtime itself.

        // Under CoreCLR/Mac the PAL "module" represents either the entire CoreCLR binary (including PAL,
        // PALRT and mscorwks) or just the PAL in the (uncommon) case of a standalone PAL. We can tell the
        // difference in these cases by whether the sys_module field of pal_module was initialized to contain
        // a non-NULL value: this is only done in the CoreCLR case.
        if (pal_module.sys_module)
        {
            // Trying to locate a symbol in the PAL, PALRT or mscorwks.
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

            ProcAddress = (FARPROC)LookupFunctionInCoreCLR(pal_module.sys_module, lpPALProcName);
        }
        else
        {
            // Trying to locate a symbol in the standalone PAL. We don't support this (it's brittle to lump
            // the PAL namespace in with some random host code). Just fall through to the failure case.
            ASSERT("Attempted to lookup proc address in a standalone PAL");
        }
    }
#endif // !CORECLR || !__APPLE__

    // If we aren't looking inside the PAL or we didn't find a PAL_ variant
    // inside the PAL, fall back to a normal search.
    if (ProcAddress == NULL)
    {
#if defined(CORECLR) && defined(__APPLE__)
        if (module->dl_handle)
        {
#endif // CORECLR && __APPLE__
            ProcAddress = (FARPROC) dlsym(module->dl_handle, lpProcName);
#if defined(CORECLR) && defined(__APPLE__)
        }
        else if (module->sys_module)
        {
            ProcAddress = (FARPROC)LookupFunctionInCoreCLR(module->sys_module, lpProcName);
        }
#endif // CORECLR && __APPLE__
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

#if !defined(CORECLR) || !defined(__APPLE__)
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
#endif // !CORECLR || !__APPLE__

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
#if !defined(CORECLR) || !defined(__APPLE__)
    LPWSTR  lpwstr = NULL;
#endif // !defined(CORECLR) || !defined(__APPLE__)

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
#if defined(CORECLR) && defined(__APPLE__)
    exe_module.sys_module = NULL;
#endif // CORECLR && __APPLE__
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

#if !defined(CORECLR) || !defined(__APPLE__)
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
#else // !CORECLR || !__APPLE__
    // Under CoreCLR/Mac we have a single binary instead of separate dynamic libraries. Here pal_module
    // represents all of that dylib (with dl_handle == NULL and sys_module != NULL). We still support some
    // scenarios with a standalone PAL statically linked into host code. These cases are differented by
    // sys_module being NULL (and GetProcAddress() will not work on such a module).
    pal_module.lib_name = UTIL_MBToWC_Alloc("CoreCLR", -1);
    if(NULL == pal_module.lib_name)
    {
        ERROR("MBToWC failure, unable to save full name of PAL module\n");
        goto Done;
    }
    pal_module.dl_handle = NULL;

    // Determine whether we're part of CoreCLR or a standalone PAL. Do this by looking at the g_szCoreCLRPath
    // global: this is set to a non-zero length string by PAL initialization in the CoreCLR case.
    if (g_szCoreCLRPath[0] != '\0')
    {
        // We're part of a full CoreCLR. Determine our module's handle and cache it for future
        // GetProcAddress() operations).
        pal_module.sys_module = FindCoreCLRHandle();
        if (pal_module.sys_module == NULL)
        {
            ASSERT("FindCoreCLRHandle() failure");
            goto Done;
        }
    }
    else
    {
        // We're just a standalone PAL. Disable any functionality that needs to peek into the containing
        // module (since we know nothing about that module).
        pal_module.sys_module = NULL;
    }

    // If we really are running in CoreCLR then we need to locate and remember the DllMain routines for the
    // PalRT and mscorwks (the PAL itself doesn't have one). We use these to keep the components up to date
    // with thread attaches and detaches. We can't call them here for the process attach, however, since we
    // are still partway through PAL initialization. We rely on PAL_InitializeCoreCLR to call us back on
    // LOADInitCoreCLRModules once PAL initialization is complete.
    if (pal_module.sys_module)
    {
        g_pPalRTDllMain = (PDLLMAIN)LookupFunctionInCoreCLR(pal_module.sys_module, "PalRtDllMain");
        if (g_pPalRTDllMain == NULL)
        {
            ERROR("Failed to locate PalRT DllMain\n");
            SetLastError(ERROR_INVALID_DLL);
            goto Done;
        }

        g_pRuntimeDllMain = (PDLLMAIN)LookupFunctionInCoreCLR(pal_module.sys_module, "CoreDllMain");
        if (g_pRuntimeDllMain == NULL)
        {
            ERROR("Failed to locate Mscorwks DllMain\n");
            SetLastError(ERROR_INVALID_DLL);
            goto Done;
        }
    }
#endif // !CORECLR || !__APPLE__

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

#if defined(CORECLR) && defined(__APPLE__)
    // The CoreCLR needs to simulate PalRT and mscorwks being separate libraries rather
    // than a single binary.
    if (InLoadOrder && g_pPalRTDllMain)
    {
#if !_NO_DEBUG_MESSAGES_
        /* reset ENTRY nesting level back to zero while inside the callback... */
        int old_level;
        old_level = DBG_change_entrylevel(0);
#endif /* !_NO_DEBUG_MESSAGES_ */

        {
            PAL_LeaveHolder holder;
            g_pPalRTDllMain((HMODULE) module, dwReason, lpReserved);
        }
        g_pRuntimeDllMain((HMODULE) module, dwReason, lpReserved);

#if !_NO_DEBUG_MESSAGES_
        /* ...and set nesting level back to what it was */
        DBG_change_entrylevel(old_level);
#endif /* !_NO_DEBUG_MESSAGES_ */
    }
#endif // CORECLR && __APPLE__

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

#if defined(CORECLR) && defined(__APPLE__)
    // The CoreCLR needs to simulate PalRT and CoreCLR being separate libraries rather
    // than a single binary.
    if (!InLoadOrder && g_pPalRTDllMain)
    {
#if !_NO_DEBUG_MESSAGES_
        /* reset ENTRY nesting level back to zero while inside the callback... */
        int old_level;
        old_level = DBG_change_entrylevel(0);
#endif /* !_NO_DEBUG_MESSAGES_ */

        g_pRuntimeDllMain((HMODULE) module, dwReason, lpReserved);
        {
            PAL_LeaveHolder holder;
            g_pPalRTDllMain((HMODULE) module, dwReason, lpReserved);
        }

#if !_NO_DEBUG_MESSAGES_
        /* ...and set nesting level back to what it was */
        DBG_change_entrylevel(old_level);
#endif /* !_NO_DEBUG_MESSAGES_ */
    }
#endif // CORECLR && __APPLE__

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
#if defined(CORECLR) && defined(__APPLE__)
    module->sys_module = NULL;
#endif // CORECLR && __APPLE__
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
        ShortAsciiName = LIBC_SO;
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

#if !defined(CORECLR) || !defined(__APPLE__)
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
#endif // !CORECLR || !__APPLE__

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
#ifdef __APPLE__
    {
        PAL_LeaveHolder holder;
        if (!g_pPalRTDllMain((HMODULE)&pal_module, DLL_PROCESS_ATTACH, NULL))
            return FALSE;
    }
#endif // __APPLE__
    return g_pRuntimeDllMain((HMODULE)&pal_module, DLL_PROCESS_ATTACH, NULL);
}

#if defined(CORECLR) && defined(__APPLE__)
// Abstract the API used to load and query for functions in the CoreCLR binary to make it easier to change the
// underlying implementation.

// Load the CoreCLR module into memory given the directory in which it resides. Returns NULL on failure.
CORECLRHANDLE LoadCoreCLR(const char *szPath)
{
    CFStringRef hPath = NULL;
    CFURLRef    hUrl = NULL;
    CFBundleRef hBundle = NULL;

    // We're handed the full path to the CoreCLR directory but CFBundleCreate wants the path of the bundle
    // directory that contains it. So we have to strip two directory components off.
    int iLen = strlen(szPath) + 1;
    char *szBundlePath = (char*)alloca(iLen);
    
    if (strcpy_s(szBundlePath, iLen, szPath) != SAFECRT_SUCCESS)
    {
        ERROR("strcpy_s failed!\n");
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto done;
    }

    // Null out the last three slashes:
    //      <foo>/CoreCLR.bundle/Contents/MacOS/ -> <foo>/CoreCLR.bundle/Contents/MacOS
    //      <foo>/CoreCLR.bundle/Contents/MacOS -> <foo>/CoreCLR.bundle/Contents
    //      <foo>/CoreCLR.bundle/Contents -> <foo>/CoreCLR.bundle
    TRACE("LoadCoreCLR: szPath = \"%s\"\n", szPath);
    for (int i = 0; i < 3; i++)
    {
        char *szLastSlash = rindex(szBundlePath, '/');
        if (szLastSlash == NULL)
        {
            ERROR("Got invalid bundle path \"%s\"\n", szPath);
            SetLastError(ERROR_INVALID_PARAMETER);
            goto done;
        }
        *szLastSlash = '\0';
    }

    // Convert the pathname provided as a cstring to a CFString.
    hPath = CFStringCreateWithCString(kCFAllocatorDefault, szBundlePath, kCFStringEncodingUTF8);
    if (hPath == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

    // Convert the path into a URL.
    hUrl = CFURLCreateWithFileSystemPath(kCFAllocatorDefault, hPath, kCFURLPOSIXPathStyle, TRUE);
    if (hUrl == NULL)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

    // Load the bundle from the URL.
    hBundle = CFBundleCreate(kCFAllocatorDefault, hUrl);

  done:
    if (hUrl)
        CFRelease(hUrl);
    if (hPath)
        CFRelease(hPath);

    return hBundle;
}

// Lookup the named function in the given CoreCLR image. Returns NULL on failure.
void *LookupFunctionInCoreCLR(CORECLRHANDLE hCoreCLR, const char *szFunction)
{
    CFStringRef hFunction = NULL;
    void       *pFunction = NULL;

    // Convert the function name provided as a cstring to a CFString.
    hFunction = CFStringCreateWithCString(kCFAllocatorDefault, szFunction, kCFStringEncodingUTF8);
    if (hFunction == NULL)
        goto done;

    // Look up the function name in the bundle.
    {
        // We temporarily leave PAL as a workaround for what is presumably a problem in gdb (as of version 477).
        // The function we call here may call into dyld for linking new images, and gdb sets a breakpoint deep
        // in there so that it knows about it, and can load new symbol files. We leave the PAL so that we
        // unhook the exception port for hardware breakpoints.
        //
        // Strictly speaking, we'd expect this to work without leaving the PAL: For a breakpoint exception, if
        // no managed debugger is attached, our thread-level handler sends back a message to the system that
        // we do not with to handle it. This causes the system to forward the exception message to the task
        // and host-level handlers. However, gdb's host-level handler seems to hang in this case.
        PAL_LeaveHolder holder;
        pFunction = CFBundleGetFunctionPointerForName(hCoreCLR, hFunction);
    }

  done:
    if (hFunction)
        CFRelease(hFunction);

    return pFunction;
}

// Locate the CoreCLR module handle associated with the code currently executing. Returns NULL on failure.
CORECLRHANDLE FindCoreCLRHandle()
{
    // Return NULL when we're not really part of CoreCLR (i.e. we're a standalone PAL).
    if (g_szCoreCLRPath[0] == '\0')
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return NULL;
    }

    // Reloading the same bundle will just return a reference to the exiting copy and we know the path from
    // which the host originally loaded us.
    return LoadCoreCLR(g_szCoreCLRPath);
}
#endif // CORECLR && __APPLE__

/*++
Function:
  PAL_GetModuleBaseFromAddress

  Given an address, returns the base address of the dynamic module which contains that address, 
  or NULL if none.

  Notes:
    This is a replacement for code that casts HMODULEs to pointers on Windows.
    Ideally this would take an HMODULE instead of an address, but that is harder - we don't seem to
    have a way to map it directly to a dyld index or to get an address from it. Eg., we're not 
    guaranteed toh ave a module name, dllMain or dyld handle.
 */
#ifdef __APPLE__
PALAPI
LPCVOID
PAL_GetModuleBaseFromAddress(LPCVOID pAddress)
{
    LPCVOID retval = NULL;

    PERF_ENTRY(PAL_GetModuleBaseFromAddress);
    ENTRY("PAL_GetModuleBaseFromAddress (pAddress=%p)\n", pAddress);

    // Given a pointer into the module, get the header at the start of the module
    retval = _dyld_get_image_header_containing_address(pAddress);
    if (retval == NULL)
    {
        // All modules we load use dyld (even bundles are implemented using this in the OS)
        TRACE("Address isn't recognized as being in a dyld module: %p\n", pAddress);
        goto done;
    }

    TRACE("base address of module with address %p is %p\n", pAddress, retval);

done:
    LOGEXIT("PAL_GetModuleBaseFromAddress returns %p\n", retval);
    PERF_EXIT(PAL_GetModuleBaseFromAddress);
    return retval;
}

//---------------------------------------------------------------------------------------
//
// Retrieve the UUID in the image.
//
// Arguments:
//    pImageBase - the base address of where an image is loaded into memory
//    pUUID      - out parameter; return the UUID in the image
//
// Assumptions:
//    The buffer pointed to by pUUID must have at least 16 bytes.
//
// Return Value:
//    TRUE if this function successfully retrieves the UUID from the specified image
//

PALAPI
BOOL
PAL_GetUUIDOfImage(LPCVOID pImageBase, BYTE * pUUID)
{
        PERF_ENTRY(PAL_GetUUIDOfImage);
    ENTRY("PAL_GetUUIDOfImage (pImageBase=%p, pUUID=%p)\n", pImageBase, pUUID);

    // There should be a Mach-O header at the image base.
    const mach_header * pHeader;
    pHeader = reinterpret_cast<const mach_header *>(pImageBase);

    const load_command * pCurCommand = NULL;
    UINT32 cLoadCommands = 0;
    BOOL fFoundUUID = FALSE;
    
    // The offset to the magic number is the same for both mach_header and for
    // mach_header_64 (same size too), so it's safe to use it to check for
    // MH_MAGIC_64.
    if (pHeader->magic == MH_MAGIC)
    {
        // Immediately following the header are the load commands.
        cLoadCommands = pHeader->ncmds;
        pCurCommand = reinterpret_cast<const load_command *>(pHeader + 1);

    }
    else if (pHeader->magic == MH_MAGIC_64)
    {
        const mach_header_64 * pHeader64;
	pHeader64 = reinterpret_cast<const mach_header_64 *>(pImageBase);
	cLoadCommands = pHeader64->ncmds;
	pCurCommand = reinterpret_cast<const load_command *>(pHeader64 + 1);
    }

    if (pCurCommand)
    {
        // Loop through the load commmands to find the LC_UUID load command.
        for (UINT32 i = 0; i < cLoadCommands; i++)
	{
            if (pCurCommand->cmd == LC_UUID)
            {
                const uuid_command * pUUIDCommand = reinterpret_cast<const uuid_command *>(pCurCommand);

                // sanity check
                if (pUUIDCommand->cmdsize == sizeof(uuid_command))
                {
		    // Copy the 16-byte UUID into the out buffer.
                    memcpy(pUUID, pUUIDCommand->uuid, sizeof(pUUIDCommand->uuid));
                    fFoundUUID = TRUE;
                    break;
                }
            }
            pCurCommand = reinterpret_cast<const load_command *>((SIZE_T)pCurCommand + pCurCommand->cmdsize);
        }
    }

    LOGEXIT("PAL_GetUUIDOfImage\n");
    PERF_EXIT(PAL_GetUUIDOfImage);
    return fFoundUUID;
}

//---------------------------------------------------------------------------------------
// Retrieve the version stored in the Info.plist file in a bundle.
//
// Arguments:
//    bundle                 - Target bundle.
//    pwszVersionString      - out parameter; buffer to be filled with the version string
//    cchVersionStringBuffer - size of the buffer in # of characters pointed to by pwszVersionString
//    pcchVersionStringBufferRequired - required size in characters, including NULL.
//
// Return Value:
//    Return the number of characters in the version string (excluding NULL) or 0 if the operation fails.
//
// Notes:
//    Call GetLastError() to retrieve more information if the function fails.
//
static DWORD GetBundleVersionString(IN CFBundleRef bundle,
                                    IN WCHAR *pwszVersionString,
                                    IN DWORD cchVersionStringBuffer, 
                                    IN DWORD *pcchVersionStringBufferRequired)
{
    CFTypeRef   hVersionString     = NULL;
    CFStringRef hRealVersionString = NULL;

    *pcchVersionStringBufferRequired = 0;

    // Get a CFTypeRef to the version string stored in the Info.plist file in the CoreCLR bundle.
    // CFTypeRef is like an System.Object.  It's the base class in CoreFoundation.
    hVersionString = CFBundleGetValueForInfoDictionaryKey(bundle, kCFBundleVersionKey);
    if (hVersionString == NULL || CFGetTypeID(hVersionString) != CFStringGetTypeID())
    {
        SetLastError(ERROR_INVALID_DATA);
        return 0;
    }
    hRealVersionString = static_cast<CFStringRef>(hVersionString);

    // Get the length of the version string.
    S_UINT32 cchRealVersionString(ClrSafeInt<CFIndex>(CFStringGetLength(hRealVersionString)));
    if (cchRealVersionString.IsOverflow() || 
        !cchRealVersionString.addition(cchRealVersionString.Value(), 1ul, *pcchVersionStringBufferRequired))
    {
        SetLastError(ERROR_INVALID_DATA);
        return 0;
    }

    if (*pcchVersionStringBufferRequired > cchVersionStringBuffer)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return 0;
    }

    // Copy the version string into the output buffer and make sure we put the NULL character at the end.
    CFStringGetCharacters(hRealVersionString, CFRangeMake(0, cchRealVersionString.Value()), pwszVersionString);
    pwszVersionString[cchRealVersionString.Value()] = L'\0';

    return cchRealVersionString.Value();
}

//---------------------------------------------------------------------------------------
//
// Retrieve the version stored in the Info.plist file in a bundle containing the passed in executable path.
//
// Arguments:
//    pwszCoreClrFullPath       - full path to CoreCLR
//    pwszVersionString         - out parameter; buffer to be filled with the version string
//    cchVersionStringBuffer    - size of the buffer in # of characters pointed to by pwszVersionString
//    pcchVersionStringBufferRequired - required size in characters, including NULL.
//
// Return Value:
//    Return the number of characters in the version string (excluding NULL) or 0 if the operation fails.
//
// Notes:
//    Call GetLastError() to retrieve more information if the function fails.
//

PALAPI
DWORD
PAL_GetVersionString(IN WCHAR * pwszCoreClrFullPath, 
                     IN OUT WCHAR * pwszVersionString, 
                     IN DWORD cchVersionStringBuffer,
                     OUT DWORD *pcchVersionStringBufferRequired)
{
    PERF_ENTRY(PAL_GetVersionString);
    ENTRY("PAL_GetVersionString (pwszCoreClrFullPath=%p (%S), pwszVersionString=%p, "
          "cchVersionStringBuffer=%u, pcchVersionStringBufferRequired=%p)\n", 
          (pwszCoreClrFullPath ? pwszCoreClrFullPath : W16_NULLSTRING),
          (pwszCoreClrFullPath ? pwszCoreClrFullPath : W16_NULLSTRING),
          pwszVersionString, cchVersionStringBuffer, pcchVersionStringBufferRequired);

    // various handles for dealing with the Core Foundation APIs
    CFStringRef hPath   = NULL;
    CFURLRef    hURL    = NULL;
    CFBundleRef hBundle = NULL;

    DWORD cchFullPath = PAL_wcslen(pwszCoreClrFullPath);
    DWORD cchVersionString = 0;

    // Make sure the full path is not too long.
    if (cchFullPath > MAX_PATH)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto LExit;
    }

    // Include an extra space for the NULL character.
    WCHAR wszBundlePath[MAX_PATH + 1];
    if (wcscpy_s(wszBundlePath, cchFullPath + 1, pwszCoreClrFullPath) != SAFECRT_SUCCESS)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto LExit;
    }

    // Null out the last three slashes:
    //      <foo>/CoreCLR.bundle/Contents/MacOS/ -> <foo>/CoreCLR.bundle/Contents/MacOS
    //      <foo>/CoreCLR.bundle/Contents/MacOS -> <foo>/CoreCLR.bundle/Contents
    //      <foo>/CoreCLR.bundle/Contents -> <foo>/CoreCLR.bundle
    for (int i = 0; i < 3; i++)
    {
        WCHAR * pwszLastSlash = PAL_wcsrchr(wszBundlePath, L'/');
        if (pwszLastSlash == NULL)
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            goto LExit;
        }

        *pwszLastSlash = '\0';
    }

    // Create a CFStringRef representation of the bundle path.
    hPath = CFStringCreateWithCharacters(kCFAllocatorDefault, wszBundlePath, (CFIndex)PAL_wcslen(wszBundlePath));
    if (hPath == NULL)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto LExit;
    }

    // Create a CFURLRef representation of the bundle path.
    hURL = CFURLCreateWithFileSystemPath(kCFAllocatorDefault, hPath, kCFURLPOSIXPathStyle, true);
    if (hURL == NULL)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto LExit;
    }

    // Create a handle to the CoreCLR bundle.
    hBundle = CFBundleCreate(kCFAllocatorDefault, hURL);
    if (hBundle == NULL)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto LExit;
    }

    cchVersionString = GetBundleVersionString(hBundle, pwszVersionString, cchVersionStringBuffer, 
        pcchVersionStringBufferRequired);

LExit:
    if (hURL != NULL)
    {
        CFRelease(hURL);
    }

    if (hPath != NULL)
    {
        CFRelease(hPath);
    }

    if (hBundle != NULL)
    {
        CFRelease(hBundle);
    }

    LOGEXIT("PAL_GetVersionString returns %u, pwszVersionString=\"%S\", *pcchVersionStringBufferRequired=%u\n",
        cchVersionString, (pwszVersionString ? pwszVersionString : W16_NULLSTRING),
        (pcchVersionStringBufferRequired ? *pcchVersionStringBufferRequired : 0));
    PERF_EXIT(PAL_GetVersionString);
    return cchVersionString;
}

//---------------------------------------------------------------------------------------
//
// Retrieve the version stored in the Info.plist file in the CoreCLR bundle.
//
// Arguments:
//    pwszVersionString         - out parameter; buffer to be filled with the version string
//    cchVersionStringBuffer    - size of the buffer in # of characters pointed to by pwszVersionString
//    pcchVersionStringBufferRequired - required size in characters, including NULL.
//
// Return Value:
//    Return the number of characters in the version string (excluding NULL) or 0 if the operation fails.
//
// Notes:
//    Call GetLastError() to retrieve more information if the function fails.
//

PALAPI
DWORD
PAL_GetCoreCLRVersionString(
                     IN OUT WCHAR * pwszVersionString, 
                     IN DWORD cchVersionStringBuffer,
                     IN DWORD *pcchVersionStringBufferRequired)
{
    PERF_ENTRY(PAL_GetCoreCLRVersionString);
    ENTRY("PAL_GetCoreCLRVersionString (pwszVersionString=%p, cchVersionStringBuffer=%u, "
          "pcchVersionStringBufferRequired=%p)\n", 
          pwszVersionString, cchVersionStringBuffer, pcchVersionStringBufferRequired);

    // various handles for dealing with the Core Foundation APIs
    CFBundleRef hBundle   = NULL;

    DWORD cchVersionString = 0;

    // NOTE: This code knows that CORECLRHANDLE is actually a CFBundleRef
    hBundle = (CFBundleRef)FindCoreCLRHandle();
    if (hBundle == NULL)
    {
        SetLastError(ERROR_INVALID_DATA);
        goto LExit;
    }

    cchVersionString = GetBundleVersionString(hBundle, pwszVersionString, cchVersionStringBuffer, 
        pcchVersionStringBufferRequired);

LExit:
    if (hBundle != NULL)
    {
        CFRelease(hBundle);
    }

    LOGEXIT("PAL_GetCoreCLRVersionString returns %u, pwszVersionString=\"%S\", *pcchVersionStringBufferRequired=%u\n",
        cchVersionString, (pwszVersionString ? pwszVersionString : W16_NULLSTRING),
        (pcchVersionStringBufferRequired ? *pcchVersionStringBufferRequired : 0));
    PERF_EXIT(PAL_GetCoreCLRVersionString);
    return cchVersionString;
}
#else // __APPLE__

// Get base address of the coreclr module
PALAPI
LPCVOID
PAL_GetCoreClrModuleBase()
{
    LPCVOID retval = NULL;

    PERF_ENTRY(PAL_GetModuleBaseFromHModule);
    ENTRY("PAL_GetCoreClrModuleBase\n");

    if(pal_module.dl_handle != NULL)
    {
        // To lookup module base address, we need an address inside of the module.
        // The coreclr.so contains the DllMain function, so we use it here.
        void* dllMain = dlsym(pal_module.dl_handle, "DllMain");
        if (dllMain != NULL)
        {
            Dl_info info;
            if (dladdr(dllMain, &info) != 0)
            {
                retval = info.dli_fbase;
            }
            else 
            {
                TRACE("Can't get base address of the libcoreclr.so\n");
                SetLastError(ERROR_INVALID_DATA);
            }
        }
        else
        {
            TRACE("Can't find DllMain in libcoreclr.so\n");
            SetLastError(ERROR_INVALID_DATA);
        }
    }
    else 
    {
        TRACE("Can't get libcoreclr.so base - the pal_module is not initialized\n");
        SetLastError(ERROR_MOD_NOT_FOUND);
    }

    LOGEXIT("PAL_GetCoreClrModuleBase returns %p\n", retval);
    PERF_EXIT(PAL_GetCoreClrModuleBase);
    return retval;
}

#endif // __APPLE__
