// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/utils.h

Abstract:
    Miscellaneous helper functions for the PAL, which don't fit anywhere else



--*/

#ifndef _PAL_UTILS_H_
#define _PAL_UTILS_H_

#include <unistd.h>
#include <sys/types.h>
#include <sys/stat.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*++
Function:
  UTIL_inverse_wcspbrk

  Opposite of wcspbrk : searches a string for the first character NOT in the 
  given set

Parameters :
    LPWSTR lpwstr :   string to search
    LPCWSTR charset : list of characters to search for
                                      
Return value :
    pointer to first character of lpwstr that isn't in the set
    NULL if all characters are in the set                                                                 
--*/
LPWSTR UTIL_inverse_wcspbrk(LPWSTR lpwstr, LPCWSTR charset);

/*++
Function : 
    UTIL_IsReadOnlyBitsSet
    
    Takes a struct stat *
    Returns true if the file is read only,
--*/
BOOL UTIL_IsReadOnlyBitsSet( struct stat * stat_data );

/*++
Function : 
    UTIL_IsExecuteBitsSet
    
    Takes a struct stat *
    Returns true if the file is executable.
--*/
BOOL UTIL_IsExecuteBitsSet( struct stat * stat_data );


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
    performed using CP_ACP. Buffer is allocated with malloc(), release it 
    with free().
    In case if failure, LastError will be set.
--*/
LPSTR UTIL_WCToMB_Alloc(LPCWSTR lpWideCharStr, int cchWideChar);

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
    performed using CP_ACP. Buffer is allocated with malloc(), release it 
    with free().
    In case if failure, LastError will be set.
--*/
LPWSTR UTIL_MBToWC_Alloc(LPCSTR lpMultiByteStr, int cbMultiByte);

#if HAVE_VM_ALLOCATE
#include <mach/kern_return.h>

/*++
Function:
  UTIL_MachErrorToPalError

    Maps a Mach kern_return_t to a Win32 error code.
--*/
DWORD UTIL_MachErrorToPalError(kern_return_t MachReturn);

/*++
Function:
  UTIL_SetLastErrorFromMach

    Sets Win32 LastError according to the argument Mach kern_return_t value,
    provided it indicates an error.  If the argument indicates success, does
    not modify LastError.
--*/
void UTIL_SetLastErrorFromMach(kern_return_t MachReturn);

#endif //HAVE_VM_ALLOCATE

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_UTILS_H_ */
