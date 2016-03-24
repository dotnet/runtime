// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "pal/environ.h"
#include "pal/virtual.h"
#include "pal/map.hpp"
#include "pal/stackstring.hpp"

#include <sys/param.h>
#include <errno.h>
#include <string.h>
#include <limits.h>
#if NEED_DLCOMPAT
#include "dlcompat.h"
#else   // NEED_DLCOMPAT
#include <dlfcn.h>
#endif  // NEED_DLCOMPAT
#include <stdlib.h>

#ifdef __APPLE__
#include <mach-o/dyld.h>
#include <mach-o/loader.h>
#endif // __APPLE__

#include <sys/types.h>
#include <sys/mman.h>

#if defined(__linux__)
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

/* always the first, in the in-load-order list */
MODSTRUCT exe_module; 
MODSTRUCT *pal_module = nullptr;

char * g_szCoreCLRPath = nullptr;

int MaxWCharToAcpLength = 3;

/* static function declarations ***********************************************/

template<class TChar> static bool LOADVerifyLibraryPath(const TChar *libraryPath);
static bool LOADConvertLibraryPathWideStringToMultibyteString(
    LPCWSTR wideLibraryPath,
    LPSTR multibyteLibraryPath,
    INT *multibyteLibraryPathLengthRef);
static BOOL LOADValidateModule(MODSTRUCT *module);
static LPWSTR LOADGetModuleFileName(MODSTRUCT *module);
static MODSTRUCT *LOADAddModule(void *dl_handle, LPCSTR libraryNameOrPath);
static void *LOADLoadLibraryDirect(LPCSTR libraryNameOrPath);
static BOOL LOADFreeLibrary(MODSTRUCT *module, BOOL fCallDllMain);
static HMODULE LOADRegisterLibraryDirect(void *dl_handle, LPCSTR libraryNameOrPath, BOOL fDynamic);
static HMODULE LOADLoadLibrary(LPCSTR shortAsciiName, BOOL fDynamic);
static BOOL LOADCallDllMainSafe(MODSTRUCT *module, DWORD dwReason, LPVOID lpReserved);

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
    return LoadLibraryExA(lpLibFileName, nullptr, 0);
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
    return LoadLibraryExW(lpLibFileName, nullptr, 0);
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
        return nullptr;
    }

    LPSTR lpstr = nullptr;
    HMODULE hModule = nullptr;

    PERF_ENTRY(LoadLibraryA);
    ENTRY("LoadLibraryExA (lpLibFileName=%p (%s)) \n",
          (lpLibFileName) ? lpLibFileName : "NULL",
          (lpLibFileName) ? lpLibFileName : "NULL");

    if (!LOADVerifyLibraryPath(lpLibFileName))
    {
        goto Done;
    }

    /* do the Dos/Unix conversion on our own copy of the name */
    lpstr = strdup(lpLibFileName);
    if (!lpstr)
    {
        ERROR("strdup failure!\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto Done;
    }
    FILEDosToUnixPathA(lpstr);

    hModule = LOADLoadLibrary(lpstr, TRUE);

    /* let LOADLoadLibrary call SetLastError */
 Done:
    if (lpstr != nullptr)
    {
        InternalFree(lpstr);
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
        return nullptr;
    }
    
    CHAR * lpstr;
    INT name_length;
    PathCharString pathstr;
    HMODULE hModule = nullptr;

    PERF_ENTRY(LoadLibraryExW);
    ENTRY("LoadLibraryExW (lpLibFileName=%p (%S)) \n",
          lpLibFileName ? lpLibFileName : W16_NULLSTRING,
          lpLibFileName ? lpLibFileName : W16_NULLSTRING);

    if (!LOADVerifyLibraryPath(lpLibFileName))
    {
        goto done;
    }

    lpstr = pathstr.OpenStringBuffer((PAL_wcslen(lpLibFileName)+1) * MaxWCharToAcpLength);
    if (nullptr == lpstr)
    {
        goto done;
    }
    if (!LOADConvertLibraryPathWideStringToMultibyteString(lpLibFileName, lpstr, &name_length))
    {
        goto done;
    }

    /* do the Dos/Unix conversion on our own copy of the name */
    FILEDosToUnixPathA(lpstr);
    pathstr.CloseBuffer(name_length);

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
    FARPROC ProcAddress = nullptr;
    LPCSTR symbolName = lpProcName;

    PERF_ENTRY(GetProcAddress);
    ENTRY("GetProcAddress (hModule=%p, lpProcName=%p (%s))\n",
          hModule, lpProcName ? lpProcName : "NULL", lpProcName ? lpProcName : "NULL");

    LockModuleList();

    module = (MODSTRUCT *) hModule;

    /* parameter validation */

    if ((lpProcName == nullptr) || (*lpProcName == '\0'))
    {
        TRACE("No function name given\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    if (!LOADValidateModule(module))
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
    if ((DWORD_PTR)lpProcName < VIRTUAL_PAGE_SIZE)
    {
        ASSERT("Attempt to locate symbol by ordinal?!\n");
    }

    // Get the symbol's address.
    
    // If we're looking for a symbol inside the PAL, we try the PAL_ variant
    // first because otherwise we run the risk of having the non-PAL_
    // variant preferred over the PAL's implementation.
    if (pal_module && module->dl_handle == pal_module->dl_handle)
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
    if (ProcAddress == nullptr)
    {
        ProcAddress = (FARPROC) dlsym(module->dl_handle, lpProcName);
    }

    if (ProcAddress)
    {
        TRACE("Symbol %s found at address %p in module %p (named %S)\n",
              lpProcName, ProcAddress, module, MODNAME(module));

        /* if we don't know the module's full name yet, this is our chance to obtain it */
        if (!module->lib_name && module->dl_handle)
        {
            const char* libName = PAL_dladdr((LPVOID)ProcAddress);
            if (libName)
            {
                module->lib_name = UTIL_MBToWC_Alloc(libName, -1);
                if (nullptr == module->lib_name)
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
    BOOL retval = FALSE;

    PERF_ENTRY(FreeLibrary);
    ENTRY("FreeLibrary (hLibModule=%p)\n", hLibModule);

    retval = LOADFreeLibrary((MODSTRUCT *)hLibModule, TRUE /* fCallDllMain */);

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
    DWORD retval = 0;
    LPWSTR wide_name = nullptr;

    PERF_ENTRY(GetModuleFileNameA);
    ENTRY("GetModuleFileNameA (hModule=%p, lpFileName=%p, nSize=%u)\n",
          hModule, lpFileName, nSize);

    LockModuleList();
    if (hModule && !LOADValidateModule((MODSTRUCT *)hModule))
    {
        TRACE("Can't find name for invalid module handle %p\n", hModule);
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }
    wide_name = LOADGetModuleFileName((MODSTRUCT *)hModule);

    if (!wide_name)
    {
        ASSERT("Can't find name for valid module handle %p\n", hModule);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    /* Convert module name to Ascii, place it in the supplied buffer */

    name_length = WideCharToMultiByte(CP_ACP, 0, wide_name, -1, lpFileName,
                                      nSize, nullptr, nullptr);
    if (name_length == 0)
    {
        TRACE("Buffer too small to copy module's file name.\n");
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto done;
    }

    TRACE("File name of module %p is %s\n", hModule, lpFileName);
    retval = name_length;
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
    DWORD retval = 0;
    LPWSTR wide_name = nullptr;

    PERF_ENTRY(GetModuleFileNameW);
    ENTRY("GetModuleFileNameW (hModule=%p, lpFileName=%p, nSize=%u)\n",
          hModule, lpFileName, nSize);

    LockModuleList();

    wcscpy_s(lpFileName, nSize, W(""));

    if (hModule && !LOADValidateModule((MODSTRUCT *)hModule))
    {
        TRACE("Can't find name for invalid module handle %p\n", hModule);
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }
    wide_name = LOADGetModuleFileName((MODSTRUCT *)hModule);

    if (!wide_name)
    {
        TRACE("Can't find name for valid module handle %p\n", hModule);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    /* Copy module name into supplied buffer */

    name_length = lstrlenW(wide_name);
    if (name_length >= (INT)nSize)
    {
        TRACE("Buffer too small (%u) to copy module's file name (%u).\n", nSize, name_length);
        retval = (INT)nSize;
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        goto done;
    }
    
    wcscpy_s(lpFileName, nSize, wide_name);

    TRACE("file name of module %p is %S\n", hModule, lpFileName);
    retval = name_length;
done:
    UnlockModuleList();
    LOGEXIT("GetModuleFileNameW returns DWORD %u\n", retval);
    PERF_EXIT(GetModuleFileNameW);
    return retval;
}

/*
Function:
  PAL_LoadLibraryDirect

  Loads a library using a system call, without registering the library with the module list.

  Returns the system handle to the loaded library, or nullptr upon failure (error is set via SetLastError()).
*/
void *
PALAPI
PAL_LoadLibraryDirect(
    IN LPCWSTR lpLibFileName)
{
    PathCharString pathstr;
    CHAR * lpstr = nullptr;
    INT name_length;
    void *dl_handle = nullptr;

    PERF_ENTRY(LoadLibraryDirect);
    ENTRY("LoadLibraryDirect (lpLibFileName=%p (%S)) \n",
          lpLibFileName ? lpLibFileName : W16_NULLSTRING,
          lpLibFileName ? lpLibFileName : W16_NULLSTRING);

    if (!LOADVerifyLibraryPath(lpLibFileName))
    {
        goto done;
    }
    
    lpstr = pathstr.OpenStringBuffer((PAL_wcslen(lpLibFileName)+1) * MaxWCharToAcpLength);
    if (nullptr == lpstr)
    {
        goto done;
    }
    if (!LOADConvertLibraryPathWideStringToMultibyteString(lpLibFileName, lpstr, &name_length))
    {
        goto done;
    }

    /* do the Dos/Unix conversion on our own copy of the name */
    FILEDosToUnixPathA(lpstr);
    pathstr.CloseBuffer(name_length);

    dl_handle = LOADLoadLibraryDirect(lpstr);

done:
    LOGEXIT("LoadLibraryDirect returns HMODULE %p\n", dl_handle);
    PERF_EXIT(LoadLibraryDirect);
    return dl_handle;
}

/*
Function:
  PAL_RegisterLibraryDirect

  Registers a system handle to a loaded library with the module list.

  Returns a PAL handle to the loaded library, or nullptr upon failure (error is set via SetLastError()).
*/
HMODULE
PALAPI
PAL_RegisterLibraryDirect(
    IN void *dl_handle,
    IN LPCWSTR lpLibFileName)
{
    PathCharString pathstr;
    CHAR * lpstr = nullptr;
    INT name_length;
    HMODULE hModule = nullptr;

    PERF_ENTRY(RegisterLibraryDirect);
    ENTRY("RegisterLibraryDirect (lpLibFileName=%p (%S)) \n",
        lpLibFileName ? lpLibFileName : W16_NULLSTRING,
        lpLibFileName ? lpLibFileName : W16_NULLSTRING);

    if (!LOADVerifyLibraryPath(lpLibFileName))
    {
        goto done;
    }

    lpstr = pathstr.OpenStringBuffer((PAL_wcslen(lpLibFileName)+1) * MaxWCharToAcpLength);
    if (nullptr == lpstr)
    {
        goto done;
    }
    if (!LOADConvertLibraryPathWideStringToMultibyteString(lpLibFileName, lpstr, &name_length))
    {
        goto done;
    }

    /* do the Dos/Unix conversion on our own copy of the name */
    FILEDosToUnixPathA(lpstr);
    pathstr.CloseBuffer(name_length);

    /* let LOADRegisterLibraryDirect call SetLastError in case of failure */
    LockModuleList();
    hModule = LOADRegisterLibraryDirect((void *)dl_handle, lpstr, true /* fDynamic */);
    UnlockModuleList();

done:
    LOGEXIT("RegisterLibraryDirect returns HMODULE %p\n", hModule);
    PERF_EXIT(RegisterLibraryDirect);
    return hModule;
}

/*++
Function:
  PAL_RegisterModule

  Register the module with the target module and return a module handle in
  the target module's context. Doesn't call the DllMain because it is used
  as part of calling DllMain in the calling module.

--*/
HINSTANCE
PALAPI
PAL_RegisterModule(
    IN LPCSTR lpLibFileName)
{
    HINSTANCE hinstance = nullptr;

    int err = PAL_InitializeDLL();
    if (err == 0)
    {
        PERF_ENTRY(PAL_RegisterModule);
        ENTRY("PAL_RegisterModule(%s)\n", lpLibFileName ? lpLibFileName : "");

        LockModuleList();

        void *dl_handle = LOADLoadLibraryDirect(lpLibFileName);
        if (dl_handle)
        {
            // This only creates/adds the module handle and doesn't call DllMain
            hinstance = LOADAddModule(dl_handle, lpLibFileName);
        }

        UnlockModuleList();

        LOGEXIT("PAL_RegisterModule returns HINSTANCE %p\n", hinstance);
        PERF_EXIT(PAL_RegisterModule);
    }

    return hinstance;
}

/*++
Function:
  PAL_UnregisterModule

  Used to cleanup the module HINSTANCE from PAL_RegisterModule.
--*/
VOID
PALAPI
PAL_UnregisterModule(
    IN HINSTANCE hInstance)
{
    PERF_ENTRY(PAL_UnregisterModule);
    ENTRY("PAL_UnregisterModule(hInstance=%p)\n", hInstance);

    LOADFreeLibrary((MODSTRUCT *)hInstance, FALSE /* fCallDllMain */);

    LOGEXIT("PAL_UnregisterModule returns\n");
    PERF_EXIT(PAL_UnregisterModule);
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
void *
PALAPI
PAL_LOADLoadPEFile(HANDLE hFile)
{
    ENTRY("PAL_LOADLoadPEFile (hFile=%p)\n", hFile);

    void * loadedBase = MAPMapPEFile(hFile);

#ifdef _DEBUG
    if (loadedBase != nullptr)
    {
        char* envVar = EnvironGetenv("PAL_ForcePEMapFailure");
        if (envVar)
        {
            if (strlen(envVar) > 0)
            {
                TRACE("Forcing failure of PE file map, and retry\n");
                PAL_LOADUnloadPEFile(loadedBase); // unload it
                loadedBase = MAPMapPEFile(hFile); // load it again
            }

            InternalFree(envVar);
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
BOOL 
PALAPI
PAL_LOADUnloadPEFile(void * ptr)
{
    BOOL retval = FALSE;

    ENTRY("PAL_LOADUnloadPEFile (ptr=%p)\n", ptr);

    if (nullptr == ptr)
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
    PAL_GetSymbolModuleBase

    Get base address of the module containing a given symbol 

Parameters:
    void *symbol - address of symbol

Return value:
    module base address
--*/
LPCVOID
PALAPI
PAL_GetSymbolModuleBase(void *symbol)
{
    LPCVOID retval = nullptr;

    PERF_ENTRY(PAL_GetPalModuleBase);
    ENTRY("PAL_GetPalModuleBase\n");

    if (symbol == nullptr)
    {
        TRACE("Can't get base address. Argument symbol == nullptr\n");
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

/* Internal PAL functions *****************************************************/

/*++
Function :
    LOADInitializeModules

    Initialize the process-wide list of modules

Parameters :
    None

Return value :
    TRUE  if initialization succeedded
    FALSE otherwise

--*/
extern "C"
BOOL LOADInitializeModules()
{
    _ASSERTE(exe_module.prev == nullptr);

    InternalInitializeCriticalSection(&module_critsec);

    // Initialize module for main executable
    TRACE("Initializing module for main executable\n");

    exe_module.self = (HMODULE)&exe_module;
    exe_module.dl_handle = dlopen(nullptr, RTLD_LAZY);
    if (exe_module.dl_handle == nullptr)
    {
        ERROR("Executable module will be broken : dlopen(nullptr) failed dlerror message is \"%s\" \n", dlerror());
        return FALSE;
    }
    exe_module.lib_name = nullptr;
    exe_module.refcount = -1;
    exe_module.next = &exe_module;
    exe_module.prev = &exe_module;
    exe_module.pDllMain = nullptr;
    exe_module.hinstance = nullptr;
    exe_module.threadLibCalls = TRUE;
    return TRUE;
}

/*++
Function :
    LOADSetExeName

    Set the exe name path

Parameters :
    LPWSTR man exe path and name

Return value :
    TRUE  if initialization succeedded
    FALSE otherwise

--*/
extern "C"
BOOL LOADSetExeName(LPWSTR name)
{
#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    LPSTR pszExeName = nullptr;
#endif
    BOOL result = FALSE;

    LockModuleList();

    // Save the exe path in the exe module struct
    InternalFree(exe_module.lib_name);
    exe_module.lib_name = name;

    // For platforms where we can't trust the handle to be constant, we need to 
    // store the inode/device pairs for the modules we just initialized.
#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    {
        struct stat stat_buf;
        pszExeName = UTIL_WCToMB_Alloc(name, -1);
        if (nullptr == pszExeName)
        {
            ERROR("WCToMB failure, unable to get full name of exe\n");
            goto exit;
        }
        if (-1 == stat(pszExeName, &stat_buf))
        {
            SetLastError(ERROR_MOD_NOT_FOUND);
            goto exit;
        }
        TRACE("Executable has inode %d and device %d\n", stat_buf.st_ino, stat_buf.st_dev);

        exe_module.inode = stat_buf.st_ino; 
        exe_module.device = stat_buf.st_dev;
    }
#endif
    result = TRUE;

#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
exit:
    if (pszExeName)
    {
        InternalFree(pszExeName);
    }
#endif
    UnlockModuleList();
    return result;
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
    MODSTRUCT *module = nullptr;
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

    do
    {
        if (!InLoadOrder)
            module = module->prev;

        if (module->threadLibCalls)
        {
            if (module->pDllMain)
            {
                LOADCallDllMainSafe(module, dwReason, lpReserved);
            }
        }

        if (InLoadOrder)
            module = module->next;

    } while (module != &exe_module);

    UnlockModuleList();
}

/*++
Function:
  LOADFreeLibrary

Parameters:
  MODSTRUCT * module - module to free
  BOOL fCallDllMain - if TRUE, call the DllMain function

Returns:
  TRUE if successful

--*/
static BOOL LOADFreeLibrary(MODSTRUCT *module, BOOL fCallDllMain)
{
    BOOL retval = FALSE;

    LockModuleList();

    if (terminator)
    {
        /* PAL shutdown is in progress - ignore FreeLibrary calls */
        retval = TRUE;
        goto done;
    }

    if (!LOADValidateModule(module))
    {
        TRACE("Can't free invalid module %p\n", module);
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }

    if (module->refcount == -1)
    {
        /* special module - never released */
        retval = TRUE;
        goto done;
    }

    module->refcount--;
    TRACE("Reference count for module %p (named %S) decreases to %d\n",
            module, MODNAME(module), module->refcount);

    if (module->refcount != 0)
    {
        retval = TRUE;
        goto done;
    }

    /* Releasing the last reference : call dlclose(), remove module from the
       process-wide module list */

    TRACE("Reference count for module %p (named %S) now 0; destroying module structure\n", 
        module, MODNAME(module));

    /* unlink the module structure from the list */
    module->prev->next = module->next;
    module->next->prev = module->prev;

    /* remove the circular reference so that LOADValidateModule will fail */
    module->self = nullptr;

    /* Call DllMain if the module contains one */
    if (fCallDllMain && module->pDllMain)
    {
        LOADCallDllMainSafe(module, DLL_PROCESS_DETACH, nullptr);
    }

    if (module->hinstance)
    {
        PUNREGISTER_MODULE unregisterModule = (PUNREGISTER_MODULE)dlsym(module->dl_handle, "PAL_UnregisterModule");
        if (unregisterModule != nullptr)
        {
             unregisterModule(module->hinstance);
        }
        module->hinstance = nullptr;
    }

    if (module->dl_handle && 0 != dlclose(module->dl_handle))
    {
        /* report dlclose() failure, but proceed anyway. */
        WARN("dlclose() call failed! error message is \"%s\"\n", dlerror());
    }

    /* release all memory */
    InternalFree(module->lib_name);
    InternalFree(module);

    retval = TRUE;

done:
    UnlockModuleList();
    return retval;
}

/*++
Function :
    LOADCallDllMainSafe

    Exception-safe call to DllMain.

Parameters :
    MODSTRUCT *module : module whose DllMain must be called

    DWORD dwReason : parameter to pass down to DllMain, one of DLL_PROCESS_ATTACH, DLL_PROCESS_DETACH, 
        DLL_THREAD_ATTACH, DLL_THREAD_DETACH

    LPVOID lpvReserved : parameter to pass down to DllMain,
        If dwReason is DLL_PROCESS_ATTACH, lpvReserved is NULL for dynamic loads and non-NULL for static loads. 
        If dwReason is DLL_PROCESS_DETACH, lpvReserved is NULL if DllMain has been called by using FreeLibrary 
            and non-NULL if DllMain has been called during process termination. 

Returns:
    BOOL : DllMain's return value
*/
static BOOL LOADCallDllMainSafe(MODSTRUCT *module, DWORD dwReason, LPVOID lpReserved)
{
#if _ENABLE_DEBUG_MESSAGES_
    /* reset ENTRY nesting level back to zero while inside the callback... */
    int old_level = DBG_change_entrylevel(0);
#endif /* _ENABLE_DEBUG_MESSAGES_ */
    
    struct Param
    {
        MODSTRUCT *module;
        DWORD dwReason;
        LPVOID lpReserved;
        BOOL ret;
    } param;
    param.module = module;
    param.dwReason = dwReason;
    param.lpReserved = lpReserved;
    param.ret = FALSE;

    PAL_TRY(Param *, pParam, &param)
    {
        TRACE("Calling DllMain (%p) for module %S\n",
              pParam->module->pDllMain, 
              pParam->module->lib_name ? pParam->module->lib_name : W16_NULLSTRING);
        
        {
            // This module may be foreign to our PAL, so leave our PAL.
            // If it depends on us, it will re-enter.
            PAL_LeaveHolder holder;
            pParam->ret = pParam->module->pDllMain(pParam->module->hinstance, pParam->dwReason, pParam->lpReserved);
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        WARN("Call to DllMain (%p) got an unhandled exception; ignoring.\n", module->pDllMain);
    }
    PAL_ENDTRY

#if _ENABLE_DEBUG_MESSAGES_
    /* ...and set nesting level back to what it was */
    DBG_change_entrylevel(old_level);
#endif /* _ENABLE_DEBUG_MESSAGES_ */

    return param.ret;
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

    LockModuleList();

    if (terminator)
    {
        /* PAL shutdown in progress - ignore DisableThreadLibraryCalls */
        ret = TRUE;
        goto done;
    }

    module = (MODSTRUCT *) hLibModule;

    if (!LOADValidateModule(module))
    {
        // DisableThreadLibraryCalls() does nothing when given
        // an invalid module handle. This matches the Windows
        // behavior, though it is counter to MSDN.
        WARN("Invalid module handle %p\n", hLibModule);
        ret = TRUE;
        goto done;
    }

    module->threadLibCalls = FALSE;
    ret = TRUE;

done:
    UnlockModuleList();
    LOGEXIT("DisableThreadLibraryCalls returns BOOL %d\n", ret);
    PERF_EXIT(DisableThreadLibraryCalls);
    return ret;
}

// Checks the library path for null or empty string. On error, calls SetLastError() and returns false.
template<class TChar>
static bool LOADVerifyLibraryPath(const TChar *libraryPath)
{
    if (libraryPath == nullptr)
    {
        ERROR("libraryPath is null\n");
        SetLastError(ERROR_MOD_NOT_FOUND);
        return false;
    }
    if (libraryPath[0] == '\0')
    {
        ERROR("libraryPath is empty\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        return false;
    }
    return true;
}

// Converts the wide char library path string into a multibyte-char string. On error, calls SetLastError() and returns false.
static bool LOADConvertLibraryPathWideStringToMultibyteString(
    LPCWSTR wideLibraryPath,
    LPSTR multibyteLibraryPath,
    INT *multibyteLibraryPathLengthRef)
{
    _ASSERTE(multibyteLibraryPathLengthRef != nullptr);
    _ASSERTE(wideLibraryPath != nullptr);

    size_t length = (PAL_wcslen(wideLibraryPath)+1) * MaxWCharToAcpLength;
    *multibyteLibraryPathLengthRef = WideCharToMultiByte(CP_ACP, 0, wideLibraryPath, -1, multibyteLibraryPath,
                                                        length, nullptr, nullptr);
    
    if (*multibyteLibraryPathLengthRef == 0)
    {
        DWORD dwLastError = GetLastError();
        
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        
        SetLastError(ERROR_INVALID_PARAMETER);
        return false;
    }
    return true;
}

/*++
Function :
    LOADValidateModule

    Check whether the given MODSTRUCT pointer is valid

Parameters :
    MODSTRUCT *module : module to check

Return value :
    TRUE if module is valid, FALSE otherwise

NOTE :
    The module lock MUST be owned.

--*/
static BOOL LOADValidateModule(MODSTRUCT *module)
{
    MODSTRUCT *modlist_enum = &exe_module;

    /* enumerate through the list of modules to make sure the given handle is
       really a module (HMODULEs are actually MODSTRUCT pointers) */
    do 
    {
        if (module == modlist_enum)
        {
            /* found it; check its integrity to be on the safe side */
            if (module->self != module)
            {
                ERROR("Found corrupt module %p!\n",module);
                return FALSE;
            }
            TRACE("Module %p is valid (name : %S)\n", module, MODNAME(module));
            return TRUE;
        }
        modlist_enum = modlist_enum->next;
    }
    while (modlist_enum != &exe_module);

    TRACE("Module %p is NOT valid.\n", module);
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
    if (!module)
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

/*
Function:
    LOADLoadLibraryDirect [internal]

    Loads a library using a system call, without registering the library with the module list.

Parameters:
    LPCSTR libraryNameOrPath:           The library to load.

Return value:
    System handle to the loaded library, or nullptr upon failure (error is set via SetLastError()).
*/
static void *LOADLoadLibraryDirect(LPCSTR libraryNameOrPath)
{
    _ASSERTE(libraryNameOrPath != nullptr);
    _ASSERTE(libraryNameOrPath[0] != '\0');

    void *dl_handle = dlopen(libraryNameOrPath, RTLD_LAZY);
    if (dl_handle == nullptr)
    {
        WARN("dlopen() failed; dlerror says '%s'\n", dlerror());
        SetLastError(ERROR_MOD_NOT_FOUND);
    }
    else
    {
        TRACE("dlopen() found module %s\n", libraryNameOrPath);
    }

    return dl_handle;
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

    /* no match found : try to create a new module structure */
    module = (MODSTRUCT *)InternalMalloc(sizeof(MODSTRUCT));
    if (nullptr == module)
    {
        ERROR("malloc() failed! errno is %d (%s)\n", errno, strerror(errno));
        return nullptr;
    }

    wide_name = UTIL_MBToWC_Alloc(name, -1);
    if (nullptr == wide_name)
    {
        ERROR("couldn't convert name to a wide-character string\n");
        InternalFree(module);
        return nullptr;
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
    module->hinstance = nullptr;
    module->threadLibCalls = TRUE;
    module->pDllMain = nullptr;
    module->next = nullptr;
    module->prev = nullptr;

    module->lib_name = wide_name;

    return module;
}

/*
Function:
    LOADAddModule [internal]

    Registers a system handle to a loaded library with the module list.

Parameters:
    void *dl_handle:                    System handle to the loaded library.
    LPCSTR libraryNameOrPath:           The library that was loaded.

Return value:
    PAL handle to the loaded library, or nullptr upon failure (error is set via SetLastError()).
*/
static MODSTRUCT *LOADAddModule(void *dl_handle, LPCSTR libraryNameOrPath)
{
    _ASSERTE(dl_handle != nullptr);
    _ASSERTE(libraryNameOrPath != nullptr);
    _ASSERTE(libraryNameOrPath[0] != '\0');

#if !RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    /* search module list for a match. */
    MODSTRUCT *module = &exe_module;
    do
    {
        if (dl_handle == module->dl_handle)
        {   
            /* found the handle. increment the refcount and return the 
               existing module structure */
            TRACE("Found matching module %p for module name %s\n", module, libraryNameOrPath);

            if (module->refcount != -1)
            {
                module->refcount++;
            }
            dlclose(dl_handle);
            return module;
        }
        module = module->next;

    } while (module != &exe_module);
#endif

    TRACE("Module doesn't exist : creating %s.\n", libraryNameOrPath);

    module = LOADAllocModule(dl_handle, libraryNameOrPath);
    if (nullptr == module)
    {
        ERROR("couldn't create new module\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        dlclose(dl_handle);
        return nullptr;
    }

    /* We now get the address of DllMain if the module contains one. */
    module->pDllMain = (PDLLMAIN)dlsym(module->dl_handle, "DllMain");

    /* Add the new module on to the end of the list */
    module->prev = exe_module.prev;
    module->next = &exe_module;
    exe_module.prev->next = module;
    exe_module.prev = module;

#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    module->inode = stat_buf.st_ino; 
    module->device = stat_buf.st_dev;
#endif

    return module;
}

/*
Function:
    LOADRegisterLibraryDirect [internal]

    Registers a system handle to a loaded library with the module list.

Parameters:
    void *dl_handle:                    System handle to the loaded library.
    LPCSTR libraryNameOrPath:           The library that was loaded.
    BOOL fDynamic:                      TRUE if dynamic load through LoadLibrary, FALSE if static load through RegisterLibrary.

Return value:
    PAL handle to the loaded library, or nullptr upon failure (error is set via SetLastError()).
*/
static HMODULE LOADRegisterLibraryDirect(void *dl_handle, LPCSTR libraryNameOrPath, BOOL fDynamic)
{
    MODSTRUCT *module = LOADAddModule(dl_handle, libraryNameOrPath);
    if (module == nullptr)
    {
        return nullptr;
    }

    /* If the module contains a DllMain, call it. */
    if (module->pDllMain)
    {
        TRACE("Calling DllMain (%p) for module %S\n",
            module->pDllMain,
            module->lib_name ? module->lib_name : W16_NULLSTRING);

        if (nullptr == module->hinstance)
        {
            PREGISTER_MODULE registerModule = (PREGISTER_MODULE)dlsym(module->dl_handle, "PAL_RegisterModule");
            if (registerModule != nullptr)
            {
                module->hinstance = registerModule(libraryNameOrPath);
            }
            else
            {
                // If the target module doesn't have the PAL_RegisterModule export, then use this PAL's
                // module handle assuming that the target module is referencing this PAL's exported 
                // functions on said handle.
                module->hinstance = (HINSTANCE)module;
            }
        }

        BOOL dllMainRetVal = LOADCallDllMainSafe(module, DLL_PROCESS_ATTACH, fDynamic ? nullptr : (LPVOID)-1);

        // If DlMain(DLL_PROCESS_ATTACH) returns FALSE, we must immediately unload the module
        if (!dllMainRetVal)
        {
            ERROR("DllMain returned FALSE; unloading module.\n");
            module->pDllMain = nullptr;
            FreeLibrary((HMODULE)module);
            SetLastError(ERROR_DLL_INIT_FAILED);
            module = nullptr;
        }
    }
    else
    {
        TRACE("Module does not contain a DllMain function.\n");
    }

    return module;
}

/*++
Function :
    LOADLoadLibrary [internal]

    implementation of LoadLibrary (for use by the A/W variants)

Parameters :
    LPSTR shortAsciiName : name of module as specified to LoadLibrary

    BOOL fDynamic : TRUE if dynamic load through LoadLibrary, FALSE if static load through RegisterLibrary

Return value :
    handle to loaded module

--*/
static HMODULE LOADLoadLibrary(LPCSTR shortAsciiName, BOOL fDynamic)
{
    HMODULE module = nullptr;
    void *dl_handle = nullptr;

    // Check whether we have been requested to load 'libc'. If that's the case then use the
    // full name of the library that is defined in <gnu/lib-names.h> by the LIBC_SO constant.
    // The problem is that calling dlopen("libc.so") will fail for libc even thought it works
    // for other libraries. The reason is that libc.so is just linker script (i.e. a test file).
    // As a result, we have to use the full name (i.e. lib.so.6) that is defined by LIBC_SO.
    if (strcmp(shortAsciiName, LIBC_NAME_WITHOUT_EXTENSION) == 0)
    {
#if defined(__APPLE__)
        shortAsciiName = "libc.dylib";
#elif defined(__FreeBSD__)
        shortAsciiName = FREEBSD_LIBC;
#elif defined(__NetBSD__)
        shortAsciiName = "libc.so";
#else
        shortAsciiName = LIBC_SO;
#endif
    }

    LockModuleList();

    dl_handle = LOADLoadLibraryDirect(shortAsciiName);
    if (dl_handle)
    {
        module = LOADRegisterLibraryDirect(dl_handle, shortAsciiName, fDynamic);
    }

    UnlockModuleList();

    return module;
}

/*++
    LOADInitializeCoreCLRModule

    Run the initialization methods for CoreCLR module (the module containing this PAL).

Parameters:
    None

Return value:
    TRUE if successful
    FALSE if failure
--*/
BOOL LOADInitializeCoreCLRModule()
{
    MODSTRUCT *module = LOADGetPalLibrary();
    if (!module)
    {
        ERROR("Can not load the PAL module\n");
        return FALSE;
    }
    PDLLMAIN pRuntimeDllMain = (PDLLMAIN)dlsym(module->dl_handle, "CoreDllMain");
    if (!pRuntimeDllMain)
    {
        ERROR("Can not find the CoreDllMain entry point\n");
        return FALSE;
    }
    return pRuntimeDllMain(module->hinstance, DLL_PROCESS_ATTACH, nullptr);
}

/*++
Function :
    LOADGetPalLibrary

    Load and initialize the PAL module.

Parameters :
    None

Return value :
    pointer to module struct

--*/
MODSTRUCT *LOADGetPalLibrary()
{
    if (pal_module == nullptr)
    {
        // Initialize the pal module (the module containing LOADGetPalLibrary). Assumes that 
        // the PAL is linked into the coreclr module because we use the module name containing 
        // this function for the coreclr path.
        TRACE("Loading module for PAL library\n");

        Dl_info info;
        if (dladdr((PVOID)&LOADGetPalLibrary, &info) == 0)
        {
            ERROR("LOADGetPalLibrary: dladdr() failed. dlerror message is \"%s\"\n", dlerror());
            goto exit;
        }
        // Stash a copy of the CoreCLR installation path in a global variable.
        // Make sure it's terminated with a slash.
        if (g_szCoreCLRPath == nullptr)
        {
            size_t  cbszCoreCLRPath = strlen(info.dli_fname) + 1;
            g_szCoreCLRPath = (char*) InternalMalloc(cbszCoreCLRPath);

            if (g_szCoreCLRPath == nullptr)
            {
                ERROR("LOADGetPalLibrary: InternalMalloc failed!");
                goto exit;
            }

            if (strcpy_s(g_szCoreCLRPath, cbszCoreCLRPath, info.dli_fname) != SAFECRT_SUCCESS)
            {
                ERROR("LOADGetPalLibrary: strcpy_s failed!");
                goto exit;
            }
        }
        
        pal_module = (MODSTRUCT *)LOADLoadLibrary(info.dli_fname, FALSE);
    }

exit:
    return pal_module;
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
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : nullptr);

    InternalEnterCriticalSection(pThread, &module_critsec);
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
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : nullptr);

    InternalLeaveCriticalSection(pThread, &module_critsec);
}
