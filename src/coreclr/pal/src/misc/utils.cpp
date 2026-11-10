// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
Module Name:
    misc/utils.c

Abstract:
    Miscellaneous helper functions for the PAL, which don't fit anywhere else
--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(MISC); // some headers have code with asserts, so do this first

#include "pal/palinternal.h"

#if defined(TARGET_WASI)
#include "pal/wasi/pal_wasi_missing.h"
#endif

#if HAVE_VM_ALLOCATE
#include <mach/message.h>
#endif //HAVE_VM_ALLOCATE

#include "pal/utils.h"

#include <errno.h>
#include <string.h>
#include <minipal/utf8.h>


// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(MISC)
#include <safemath.h>

/*++
Function :
    UTIL_WCToMB_Alloc

    Converts a wide string to a multibyte string, allocating the required buffer

Parameters :
    LPCWSTR lpWideCharStr : string to convert
    int cchWideChar : number of wide characters to convert
                      (-1 to convert a complete null-termnated string)

Return Value :
    newly allocated buffer containing the converted string. Conversion is
    performed using UTF-8. Buffer is allocated with malloc(), release it
    with free().
    In case if failure, LastError will be set.
--*/
LPSTR UTIL_WCToMB_Alloc(LPCWSTR lpWideCharStr, int cchWideChar)
{
    size_t cchSource = cchWideChar < 0 ? minipal_u16_strlen((const CHAR16_T*)lpWideCharStr) + 1 : (size_t)cchWideChar;
    size_t length;
    LPSTR lpMultiByteStr;

    /* get required buffer length */
    length = minipal_get_length_utf16_to_utf8((const CHAR16_T*)lpWideCharStr, cchSource, 0);
    if(0 == length)
    {
        ERROR("WCToMB error; GetLastError returns %#x", GetLastError());
        return NULL;
    }

    /* allocate required buffer */
    lpMultiByteStr = (LPSTR)malloc(length);
    if(NULL == lpMultiByteStr)
    {
        ERROR("malloc() failed! errno is %d (%s)\n", errno,strerror(errno));
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }

    /* convert into allocated buffer */
    length = minipal_convert_utf16_to_utf8((const CHAR16_T*)lpWideCharStr, cchSource, lpMultiByteStr, length, 0);
    if(0 == length)
    {
        ASSERT("WCToMB error; GetLastError returns %#x\n", GetLastError());
        free(lpMultiByteStr);
        return NULL;
    }
    return lpMultiByteStr;
}

/*++
Function :
    UTIL_MBToWC_Alloc

    Converts a multibyte string to a wide string, allocating the required buffer

Parameters :
    LPCSTR lpMultiByteStr : string to convert
    int cbMultiByte : number of bytes to convert
                      (-1 to convert a complete null-termnated string)

Return Value :
    newly allocated buffer containing the converted string. Conversion is
    performed using UTF-8. Buffer is allocated with malloc(), release it
    with free().
    In case if failure, LastError will be set.
--*/
LPWSTR UTIL_MBToWC_Alloc(LPCSTR lpMultiByteStr, int cbMultiByte)
{
    size_t cbSource = cbMultiByte < 0 ? strlen(lpMultiByteStr) + 1 : (size_t)cbMultiByte;
    size_t length;
    LPWSTR lpWideCharStr;

    /* get required buffer length */
    length = minipal_get_length_utf8_to_utf16(lpMultiByteStr, cbSource, 0);
    if(0 == length)
    {
        ERROR("MBToWC error; GetLastError returns %#x", GetLastError());
        return NULL;
    }

    /* allocate required buffer */
    size_t fullsize;
    if (!ClrSafeInt<size_t>::multiply(length,sizeof(WCHAR),fullsize))
    {
        ERROR("integer overflow! length = %zu , sizeof(WCHAR) = (%zu)\n", length,sizeof(WCHAR) );
        SetLastError(ERROR_ARITHMETIC_OVERFLOW);
        return NULL;
    }

    lpWideCharStr = (LPWSTR)malloc(fullsize);
    if(NULL == lpWideCharStr)
    {
        ERROR("malloc() failed! errno is %d (%s)\n", errno,strerror(errno));
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return NULL;
    }

    /* convert into allocated buffer */
    length = minipal_convert_utf8_to_utf16(lpMultiByteStr, cbSource, (CHAR16_T*)lpWideCharStr, length, 0);
    if(0 == length)
    {
        ASSERT("MCToMB error; GetLastError returns %#x\n", GetLastError());
        free(lpWideCharStr);
        return NULL;
    }
    return lpWideCharStr;
}

#if HAVE_VM_ALLOCATE
/*++
Function:
  UTIL_MachErrorToPalError

    Maps a Mach kern_return_t to a Win32 error code.
--*/
DWORD UTIL_MachErrorToPalError(kern_return_t MachReturn)
{
    switch (MachReturn)
    {
    case KERN_SUCCESS:
        return ERROR_SUCCESS;

    case KERN_NO_ACCESS:
    case KERN_INVALID_CAPABILITY:
        return ERROR_ACCESS_DENIED;

    case KERN_TERMINATED:
        return ERROR_INVALID_HANDLE;

    case KERN_INVALID_ADDRESS:
        return ERROR_INVALID_ADDRESS;

    case KERN_NO_SPACE:
        return ERROR_NOT_ENOUGH_MEMORY;

    case KERN_INVALID_ARGUMENT:
        return ERROR_INVALID_PARAMETER;

    default:
        ASSERT("Unknown kern_return_t value %d - reporting ERROR_INTERNAL_ERROR\n", MachReturn);
        return ERROR_INTERNAL_ERROR;
    }
}

/*++
Function:
  UTIL_SetLastErrorFromMach

    Sets Win32 LastError according to the argument Mach kern_return_t value,
    provided it indicates an error.  If the argument indicates success, does
    not modify LastError.
--*/
void UTIL_SetLastErrorFromMach(kern_return_t MachReturn)
{
    DWORD palError = UTIL_MachErrorToPalError(MachReturn);
    if (palError != ERROR_SUCCESS)
    {
        SetLastError(palError);
    }
}
#endif //HAVE_VM_ALLOCATE
