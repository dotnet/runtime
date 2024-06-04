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
#if HAVE_VM_ALLOCATE
#include <mach/message.h>
#endif //HAVE_VM_ALLOCATE

#include <sys/mman.h>

#include "pal/utils.h"
#include "pal/file.h"

#include <errno.h>
#include <string.h>


// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(MISC)
#include <safemath.h>

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
LPWSTR UTIL_inverse_wcspbrk(LPWSTR lpwstr, LPCWSTR charset)
{
    while(*lpwstr)
    {
        if(NULL == PAL_wcschr(charset,*lpwstr))
        {
            return lpwstr;
        }
        lpwstr++;
    }
    return NULL;
}


/*++
Function :
    UTIL_IsReadOnlyBitsSet

    Takes a struct stat *
    Returns true if the file is read only,
--*/
BOOL UTIL_IsReadOnlyBitsSet( struct stat * stat_data )
{
    BOOL bRetVal = FALSE;

    /* Check for read permissions. */
    if ( stat_data->st_uid == geteuid() )
    {
        /* The process owner is the file owner as well. */
        if ( ( stat_data->st_mode & S_IRUSR ) && !( stat_data->st_mode & S_IWUSR ) )
        {
            bRetVal = TRUE;
        }
    }
    else if ( stat_data->st_gid == getegid() )
    {
        /* The process's owner is in the same group as the file's owner. */
        if ( ( stat_data->st_mode & S_IRGRP ) && !( stat_data->st_mode & S_IWGRP ) )
        {
            bRetVal = TRUE;
        }
    }
    else
    {
        /* Check the other bits to see who can access the file. */
        if ( ( stat_data->st_mode & S_IROTH ) && !( stat_data->st_mode & S_IWOTH ) )
        {
            bRetVal = TRUE;
        }
    }

    return bRetVal;
}

/*++
Function :
    UTIL_IsExecuteBitsSet

    Takes a struct stat *
    Returns true if the file is executable,
--*/
BOOL UTIL_IsExecuteBitsSet( struct stat * stat_data )
{
    BOOL bRetVal = FALSE;

    if ( (stat_data->st_mode & S_IFMT) == S_IFDIR )
    {
        return FALSE;
    }

    /* Check for read permissions. */
    if ( 0 == geteuid() )
    {
        /* The process owner is root */
        bRetVal = TRUE;
    }
    else if ( stat_data->st_uid == geteuid() )
    {
        /* The process owner is the file owner as well. */
        if ( ( stat_data->st_mode & S_IXUSR ) )
        {
            bRetVal = TRUE;
        }
    }
    else if ( stat_data->st_gid == getegid() )
    {
        /* The process's owner is in the same group as the file's owner. */
        if ( ( stat_data->st_mode & S_IXGRP ) )
        {
            bRetVal = TRUE;
        }
    }
    else
    {
        /* Check the other bits to see who can access the file. */
        if ( ( stat_data->st_mode & S_IXOTH ) )
        {
            bRetVal = TRUE;
        }
    }

    return bRetVal;
}

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
LPSTR UTIL_WCToMB_Alloc(LPCWSTR lpWideCharStr, int cchWideChar)
{
    int length;
    LPSTR lpMultiByteStr;

    /* get required buffer length */
    length = WideCharToMultiByte(CP_ACP, 0, lpWideCharStr, cchWideChar,
                                 NULL, 0, NULL, NULL);
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
    length = WideCharToMultiByte(CP_ACP, 0, lpWideCharStr, cchWideChar,
                                 lpMultiByteStr, length, NULL, NULL);
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
    performed using CP_ACP. Buffer is allocated with malloc(), release it
    with free().
    In case if failure, LastError will be set.
--*/
LPWSTR UTIL_MBToWC_Alloc(LPCSTR lpMultiByteStr, int cbMultiByte)
{
    int length;
    LPWSTR lpWideCharStr;

    /* get required buffer length */
    length = MultiByteToWideChar(CP_ACP, 0, lpMultiByteStr, cbMultiByte,
                                      NULL, 0);
    if(0 == length)
    {
        ERROR("MBToWC error; GetLastError returns %#x", GetLastError());
        return NULL;
    }

    /* allocate required buffer */
    size_t fullsize;
    if (!ClrSafeInt<size_t>::multiply(length,sizeof(WCHAR),fullsize))
    {
        ERROR("integer overflow! length = %d , sizeof(WCHAR) = (%d)\n", length,sizeof(WCHAR) );
        SetLastError(ERROR_ARITHMETIC_OVERFLOW);
        return NULL;
    }

    lpWideCharStr = (LPWSTR)malloc(fullsize);
    if(NULL == lpWideCharStr)
    {
        ERROR("malloc() failed! errno is %d (%s)\n", errno,strerror(errno));
        SetLastError(FILEGetLastErrorFromErrno());
        return NULL;
    }

    /* convert into allocated buffer */
    length = MultiByteToWideChar(CP_ACP, 0, lpMultiByteStr, cbMultiByte,
                                      lpWideCharStr, length);
    if(0 >= length)
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

#ifdef __APPLE__

/*++
Function:
  IsRunningOnMojaveHardenedRuntime() - Test if the current process is running on Mojave hardened runtime
--*/
BOOL IsRunningOnMojaveHardenedRuntime()
{
#if defined(TARGET_ARM64)
    return true;
#else //  defined(TARGET_ARM64)
    static volatile int isRunningOnMojaveHardenedRuntime = -1;

    if (isRunningOnMojaveHardenedRuntime == -1)
    {
        BOOL mhrDetected = FALSE;
        int pageSize = sysconf(_SC_PAGE_SIZE);
        // Try to map a page with read-write-execute protection. It should fail on Mojave hardened runtime.
        void* testPage = mmap(NULL, pageSize, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0);
        if (testPage == MAP_FAILED && (errno == EACCES))
        {
            // The mapping has failed with EACCES, check if making the same mapping with MAP_JIT flag works
            testPage = mmap(NULL, pageSize, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_ANONYMOUS | MAP_PRIVATE | MAP_JIT, -1, 0);
            if (testPage != MAP_FAILED)
            {
                mhrDetected = TRUE;
            }
        }

        if (testPage != MAP_FAILED)
        {
            munmap(testPage, pageSize);
        }

        isRunningOnMojaveHardenedRuntime = (int)mhrDetected;
    }

    return (BOOL)isRunningOnMojaveHardenedRuntime;
#endif  // defined(TARGET_ARM64)
}

#endif // __APPLE__

const char *GetFriendlyErrorCodeString(int errorCode)
{
#if HAVE_STRERRORNAME_NP
    const char *error = strerrorname_np(errorCode);
    if (error != nullptr)
    {
        return error;
    }
#else // !HAVE_STRERRORNAME_NP
    switch (errorCode)
    {
        case EACCES: return "EACCES";
    #if EAGAIN == EWOULDBLOCK
        case EAGAIN: return "EAGAIN/EWOULDBLOCK";
    #else
        case EAGAIN: return "EAGAIN";
        case EWOULDBLOCK: return "EWOULDBLOCK";
    #endif
        case EBADF: return "EBADF";
        case EBUSY: return "EBUSY";
        case EDQUOT: return "EDQUOT";
        case EEXIST: return "EEXIST";
        case EFAULT: return "EFAULT";
        case EFBIG: return "EFBIG";
        case EINVAL: return "EINVAL";
        case EINTR: return "EINTR";
        case EIO: return "EIO";
        case EISDIR: return "EISDIR";
        case ELOOP: return "ELOOP";
        case EMFILE: return "EMFILE";
        case EMLINK: return "EMLINK";
        case ENAMETOOLONG: return "ENAMETOOLONG";
        case ENFILE: return "ENFILE";
        case ENODEV: return "ENODEV";
        case ENOENT: return "ENOENT";
        case ENOLCK: return "ENOLCK";
        case ENOMEM: return "ENOMEM";
        case ENOSPC: return "ENOSPC";
    #if ENOTSUP == EOPNOTSUPP
        case ENOTSUP: return "ENOTSUP/EOPNOTSUPP";
    #else
        case ENOTSUP: return "ENOTSUP";
        case EOPNOTSUPP: return "EOPNOTSUPP";
    #endif
        case ENOTDIR: return "ENOTDIR";
        case ENOTEMPTY: return "ENOTEMPTY";
        case ENXIO: return "ENXIO";
        case EOVERFLOW: return "EOVERFLOW";
        case EPERM: return "EPERM";
        case EROFS: return "EROFS";
        case ETXTBSY: return "ETXTBSY";
        case EXDEV: return "EXDEV";
    }
#endif // HAVE_STRERRORNAME_NP

    return strerror(errorCode);
}
