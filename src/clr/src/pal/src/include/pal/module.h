//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    include/pal/module.h

Abstract:
    Header file for modle management utilities.



--*/

#ifndef _PAL_MODULE_H_
#define _PAL_MODULE_H_

#if defined(CORECLR) && defined(__APPLE__)

#include <CoreFoundation/CFBundle.h>

// Name of the CoreCLR bundle executable
#define CORECLR_BUNDLE_NAME "coreclr"

// Name of the CoreCLR bundle root directory.
#define CORECLR_BUNDLE_DIR "CoreCLR.bundle"

// Directory components between the bundle root and the executable.
#define CORECLR_BUNDLE_PATH "Contents/MacOS/"

// Abstract the API used to load and query for functions in the CoreCLR binary to make it easier to change the
// underlying implementation.
typedef CFBundleRef CORECLRHANDLE;
#endif // CORECLR && __APPLE__

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#define PAL_SHLIB_PREFIX "lib"

#if __APPLE__
#define PAL_SHLIB_SUFFIX ".dylib"
#elif _AIX
#define PAL_SHLIB_SUFFIX ".a"
#elif _HPUX_
#define PAL_SHLIB_SUFFIX ".sl"
#else
#define PAL_SHLIB_SUFFIX ".so"
#endif

typedef BOOL (__stdcall *PDLLMAIN)(HINSTANCE, DWORD, LPVOID);   /* entry point of module */
typedef HINSTANCE (PALAPI *PREGISTER_MODULE)(LPCSTR);           /* used to create the HINSTANCE for above DLLMain entry point */
typedef VOID (PALAPI *PUNREGISTER_MODULE)(HINSTANCE);           /* used to cleanup the HINSTANCE for above DLLMain entry point */

typedef struct _MODSTRUCT
{
    HMODULE self;         /* circular reference to this module */
    void *dl_handle;      /* handle returned by dlopen() */
    HINSTANCE hinstance;  /* handle returned by PAL_RegisterLibrary */
#if defined(CORECLR) && defined(__APPLE__)
    CORECLRHANDLE sys_module; /* System modules can be loaded via mechanisms other than dlopen() under
                               * CoreCLR/Mac */
#endif // CORECLR && __APPLE__
    LPWSTR lib_name;      /* full path of module */
    INT refcount;         /* reference count */
                          /* -1 means infinite reference count - module is never released */
    BOOL ThreadLibCalls;  /* TRUE for DLL_THREAD_ATTACH/DETACH notifications 
                              enabled, FALSE if they are disabled */

#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    ino_t inode;
    dev_t device;
#endif

    PDLLMAIN pDllMain; /* entry point of module */

    /* reference to next and previous modules in list (in load order) */
    struct _MODSTRUCT *next;
    struct _MODSTRUCT *prev;
} MODSTRUCT;

extern MODSTRUCT pal_module;


/*++
Function :
    LoadInitializeModules

    Initialize the process-wide list of modules (2 initial modules : 1 for
    the executable and 1 for the PAL)

Parameters :
    LPWSTR exe_name : full path to executable

Return value :
    TRUE on success, FALSE on failure

Notes :
    the module manager takes ownership of the string
--*/
BOOL LOADInitializeModules(LPWSTR exe_name);

/*++
Function :
    LOADFreeModules

    Release all resources held by the module manager (including dlopen handles)

Parameters:
    BOOL bTerminateUnconditionally: If TRUE, this will avoid calling any DllMains

    (no return value)
--*/
void LOADFreeModules(BOOL bTerminateUnconditionally);

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
void LOADCallDllMain(DWORD dwReason, LPVOID lpReserved);

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
void LockModuleList();

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
void UnlockModuleList();

/*++
Function:
  PAL_LOADLoadPEFile

Abstract
  Loads a PE file into memory.  Properly maps all of the sections in the PE file.  Returns a pointer to the
  loaded base.

Parameters:
    IN hFile    - The file to load

Return value:
    A valid base address if successful.
    0 if failure
--*/
void * PAL_LOADLoadPEFile(HANDLE hFile);

/*++
    PAL_LOADUnloadPEFile

    Unload a PE file that was loaded by PAL_LOADLoadPEFile().

Parameters:
    IN ptr - the file pointer returned by PAL_LOADLoadPEFile()

Return value:
    TRUE - success
    FALSE - failure (incorrect ptr, etc.)
--*/
BOOL PAL_LOADUnloadPEFile(void * ptr);


#if !defined(CORECLR) || !defined(__APPLE__)
/*++
    LOADGetLibRotorPalSoFileName

    Retrieve the full path of the librotor_pal.so being used.

Parameters:
    OUT pwzBuf - WCHAR buffer of MAX_PATH length to receive file name

Return value:
    0 if successful
    -1 if failure, with last error set.
--*/
int LOADGetLibRotorPalSoFileName(LPSTR pszBuf);
#endif // !CORECLR || !__APPLE__

/*++
    LOADInitCoreCLRModules

    Run the initialization methods for CoreCLR modules that used to be standalone dynamic libraries (PALRT and
    mscorwks).

Parameters:
    Core CLR path

Return value:
    TRUE if successful
    FALSE if failure
--*/
BOOL LOADInitCoreCLRModules(const char *szCoreCLRPath);

#if defined(CORECLR) && defined(__APPLE__)
// Abstract the API used to load and query for functions in the CoreCLR binary to make it easier to change the
// underlying implementation.

// Load the CoreCLR module into memory given the directory in which it resides. Returns NULL on failure.
CORECLRHANDLE LoadCoreCLR(const char *szPath);

// Lookup the named function in the given CoreCLR image. Returns NULL on failure.
void *LookupFunctionInCoreCLR(CORECLRHANDLE hCoreCLR, const char *szFunction);

// Locate the CoreCLR module handle associated with the code currently executing. Returns NULL on failure.
CORECLRHANDLE FindCoreCLRHandle();

#endif // CORECLR && __APPLE__

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_MODULE_H_ */

