// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/module.h

Abstract:
    Header file for modle management utilities.



--*/

#ifndef _PAL_MODULE_H_
#define _PAL_MODULE_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef BOOL (PALAPI_NOEXPORT *PDLLMAIN)(HINSTANCE, DWORD, LPVOID);   /* entry point of module */
typedef HINSTANCE (PALAPI_NOEXPORT *PREGISTER_MODULE)(LPCSTR);           /* used to create the HINSTANCE for above DLLMain entry point */
typedef VOID (PALAPI_NOEXPORT *PUNREGISTER_MODULE)(HINSTANCE);           /* used to cleanup the HINSTANCE for above DLLMain entry point */

typedef struct _MODSTRUCT
{
    HMODULE self;                     /* circular reference to this module */
    NATIVE_LIBRARY_HANDLE dl_handle;  /* handle returned by dlopen() */
    HINSTANCE hinstance;              /* handle returned by PAL_RegisterLibrary */
    LPWSTR lib_name;                  /* full path of module */
    INT refcount;                     /* reference count */
                                      /* -1 means infinite reference count - module is never released */
    BOOL threadLibCalls;              /* TRUE for DLL_THREAD_ATTACH/DETACH notifications enabled, FALSE if they are disabled */

#if RETURNS_NEW_HANDLES_ON_REPEAT_DLOPEN
    ino_t inode;
    dev_t device;
#endif

    PDLLMAIN pDllMain;    /* entry point of module */

    /* reference to next and previous modules in list (in load order) */
    struct _MODSTRUCT *next;
    struct _MODSTRUCT *prev;
} MODSTRUCT;


/*++
Function :
    LOADInitializeModules

    Initialize the process-wide list of modules

Parameters :
    None

Return value :
    TRUE on success, FALSE on failure

--*/
BOOL LOADInitializeModules();

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
BOOL LOADSetExeName(LPWSTR name);

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
    IN offset - offset within hFile where the PE "file" is located

Return value:
    A valid base address if successful.
    0 if failure
--*/
void* PAL_LOADLoadPEFile(HANDLE hFile, size_t offset);

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

/*++
    LOADInitializeCoreCLRModule

    Run the initialization methods for CoreCLR module.

Parameters:
    None

Return value:
    TRUE if successful
    FALSE if failure
--*/
BOOL LOADInitializeCoreCLRModule();

/*++
Function :
    LOADGetPalLibrary

    Load and initialize the PAL module.

Parameters :
    None

Return value :
    handle to loaded module

--*/
MODSTRUCT *LOADGetPalLibrary();

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_MODULE_H_ */

