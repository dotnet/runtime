// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    filecrt.cpp

Abstract:

    Implementation of the file functions in the C runtime library that
    are Windows specific.



--*/

#include "pal/thread.hpp"
#include "pal/file.hpp"

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/cruntime.h"

#include <unistd.h>
#include <errno.h>
#include <sys/stat.h>

#ifdef __APPLE__
#include <sys/syscall.h>
#endif // __APPLE__

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:
  _open_osfhandle

See MSDN doc.
--*/
int
__cdecl
_open_osfhandle( INT_PTR osfhandle, int flags )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthrCurrent = NULL;
    IPalObject *pobjFile = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pDataLock = NULL;
    INT nRetVal = -1;  
    INT openFlags = 0;

    PERF_ENTRY(_open_osfhandle);
    ENTRY( "_open_osfhandle (osfhandle=%#x, flags=%#x)\n", osfhandle, flags );

    pthrCurrent = InternalGetCurrentThread();

    if (flags != _O_RDONLY)
    {
        ASSERT("flag(%#x) not supported\n", flags);
        goto EXIT;
    }

    openFlags |= O_RDONLY;

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pthrCurrent,
        reinterpret_cast<HANDLE>(osfhandle),
        &aotFile,
        0,
        &pobjFile
        );

    if (NO_ERROR != palError)
    {
        ERROR("Error dereferencing file handle\n");
        goto EXIT;
    }

    palError = pobjFile->GetProcessLocalData(
        pthrCurrent,
        ReadLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR == palError)
    {
        if ('\0' != pLocalData->unix_filename[0])
        {
            nRetVal = InternalOpen(pLocalData->unix_filename, openFlags);
        }
        else /* the only file object with no unix_filename is a pipe */
        {
            /* check if the file pipe descrptor is for read or write */
            if (pLocalData->open_flags == O_WRONLY)
            {
                ERROR( "Couldn't open a write pipe on read mode\n");
                goto EXIT;
            }

            nRetVal = pLocalData->unix_fd;
        }
        
        if ( nRetVal == -1 )
        {
            ERROR( "Error: %s.\n", strerror( errno ) );
        }
    }
    else
    {
        ASSERT("Unable to access file data");
    }

EXIT:

    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pthrCurrent, FALSE);
    }

    if (NULL != pobjFile)
    {
        pobjFile->ReleaseReference(pthrCurrent);
    }
 
    LOGEXIT( "_open_osfhandle return nRetVal:%d\n", nRetVal);
    PERF_EXIT(_open_osfhandle);
    return nRetVal;
}


/*++
Function:
    PAL_fflush

See MSDN for more details.
--*/
int
_cdecl
PAL_fflush( PAL_FILE *stream )
{
    int nRetVal = 0;

    PERF_ENTRY(fflush);
    ENTRY( "fflush( %p )\n", stream );

    nRetVal = fflush(stream ? stream->bsdFilePtr : NULL);

    LOGEXIT( "fflush returning %d\n", nRetVal );
    PERF_EXIT(fflush);
    return nRetVal;
}


/*++
PAL__getcwd

Wrapper function for getcwd.

Input parameters:

szBuf = a copy of the absolute pathname of the current working directory
is copied into szBuf.
nSize = size, in bytes, of the array referenced by szBuf.

Return value:
    A pointer to the pathname if successful, otherwise NULL is returned 
--*/
char * 
__cdecl 
PAL__getcwd(
    char *szBuf, 
    size_t nSize
    )
{
    return (char *)getcwd(szBuf, nSize);
}


/*++
PAL_mkstemp

Wrapper function for InternalMkstemp.

Input parameters:

szNameTemplate = template to follow when naming the created file

Return value:
    Open file descriptor on success, -1 if file could not be created
--*/
int 
__cdecl 
PAL_mkstemp(char *szNameTemplate)
{
    return InternalMkstemp(szNameTemplate);
}

/*++
InternalMkstemp

Wrapper for mkstemp.

Input parameters:

szNameTemplate = template to follow when naming the created file

Return value:
    Open file descriptor on success, -1 if file could not be created
--*/
int 
CorUnix::InternalMkstemp(
    char *szNameTemplate
    )
{
    int nRet = -1;
#if MKSTEMP64_IS_USED_INSTEAD_OF_MKSTEMP
    nRet = mkstemp64(szNameTemplate);
#else
    nRet = mkstemp(szNameTemplate);
#endif
    return nRet;
}


/*++
PAL__open

Wrapper function for InternalOpen.

Input parameters:

szPath = pointer to a pathname of a file to be opened
nFlags = arguments that control how the file should be accessed
mode = file permission settings that are used only when a file is created

Return value:
    File descriptor on success, -1 on failure
--*/
int
__cdecl
PAL__open(
    const char *szPath, 
    int nFlags, 
    ...
    )
{
    int nRet = -1;
    int mode = 0;
    va_list ap;
    
    // If nFlags does not contain O_CREAT, the mode parameter will be ignored.
    if (nFlags & O_CREAT)
    {
        va_start(ap, nFlags);
        mode = va_arg(ap, int);
        va_end(ap);
    }
    
    nRet = InternalOpen(szPath, nFlags, mode);
    return nRet;
}

/*++
InternalOpen

Wrapper for open.

Input parameters:

szPath = pointer to a pathname of a file to be opened
nFlags = arguments that control how the file should be accessed
mode = file permission settings that are used only when a file is created

Return value:
    File descriptor on success, -1 on failure
--*/
int
CorUnix::InternalOpen(
    const char *szPath,
    int nFlags,
    ...
    )
{
    int nRet = -1;
    int mode = 0;
    va_list ap;

    // If nFlags does not contain O_CREAT, the mode parameter will be ignored.
    if (nFlags & O_CREAT)
    {
        va_start(ap, nFlags);
        mode = va_arg(ap, int);
        va_end(ap);
    }

#if OPEN64_IS_USED_INSTEAD_OF_OPEN
        nRet = open64(szPath, nFlags, mode);
#else
        nRet = open(szPath, nFlags, mode);
#endif
    return nRet;
}


/*++
PAL_rename

Wrapper function for rename.

Input parameters:

szOldName = pointer to the pathname of the file to be renamed
szNewName = pointer to the new pathname of the file

Return value:
    Returns 0 on success and -1 on failure
--*/
int
__cdecl
PAL_rename(
    const char *szOldName, 
    const char *szNewName
    )
{
    return rename(szOldName, szNewName);
}


/*++
PAL_fgets

Wrapper function for InternalFgets.

Input parameters:

sz = stores characters read from the given file stream
nSize = number of characters to be read
pf = stream to read characters from

Return value:
    Returns a pointer to the string storing the characters on success
    and NULL on failure.
--*/
char * 
__cdecl 
PAL_fgets(
    char *sz, 
    int nSize, 
    PAL_FILE *pf
    )
{
    char * szBuf;
    
    PERF_ENTRY(fgets);
    ENTRY( "fgets(sz=%p (%s) nSize=%d pf=%p)\n", sz, sz, nSize, pf);
    
    if (pf != NULL)
    {
        szBuf = InternalFgets(sz, nSize, pf->bsdFilePtr, pf->bTextMode);
    }
    else
    {
        szBuf = NULL;
    }
    
    LOGEXIT("fgets() returns %p\n", szBuf);
    PERF_EXIT(fgets);

    return szBuf;
}

/*++
InternalFgets

Wrapper for fgets.

Input parameters:

sz = stores characters read from the given file stream
nSize = number of characters to be read
f = stream to read characters from
fTextMode = flag that indicates if file contents are text or binary

Return value:
    Returns a pointer to the string storing the characters on success
    and NULL on failure.

Notes:
In Unix systems, fgets() can return an error if it gets interrupted by a
signal before reading anything, and errno is set to EINTR. When this
happens, it is SOP to call fgets again.
--*/
char *
CorUnix::InternalFgets(
    char *sz,
    int nSize,
    FILE *f,
    bool fTextMode
    )
{
    char *retval = NULL;

    _ASSERTE(sz != NULL);
    _ASSERTE(f != NULL);

#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
    clearerr(f);
#endif

    do
    {
        retval =  fgets(sz, nSize, f);
        if (NULL==retval)
        {
            if (feof(f))
            {
                TRACE("Reached EOF\n");
                break;
            }
            /* The man page suggests using ferror and feof to distinguish
               between error and EOF, but feof and errno is sufficient.
               Not all cases that set errno also flag ferror, so just
               checking errno is the best solution. */
            if (EINTR != errno)
            {
                WARN("got error; errno is %d (%s)\n",errno, strerror(errno));
                break;
            }
            /* we ignored a EINTR error, reset the stream's error state */
            clearerr(f);
            TRACE("call got interrupted (EINTR), trying again\n");
        }
        if (fTextMode)
        {
            int len = strlen(sz);
            if ((len>=2) && (sz[len-1]=='\n') && (sz[len-2]=='\r'))
            {
                sz[len-2]='\n';
                sz[len-1]='\0';
            }
        }
    } while(NULL == retval);

    return retval;
}

/*++
PAL_fwrite

Wrapper function for InternalFwrite.

Input parameters:

pvBuffer = array of objects to write to the given file stream
nSize = size of a object in bytes
nCount = number of objects to write
pf = stream to write characters to

Return value:
    Returns the number of objects written.
--*/
size_t 
__cdecl 
PAL_fwrite(
    const void *pvBuffer, 
    size_t nSize, 
    size_t nCount, 
    PAL_FILE *pf
    )
{
    size_t nWrittenBytes = 0;

    PERF_ENTRY(fwrite);
    ENTRY( "fwrite( pvBuffer=%p, nSize=%d, nCount=%d, pf=%p )\n",
           pvBuffer, nSize, nCount, pf);
    _ASSERTE(pf != NULL);

    nWrittenBytes = InternalFwrite(pvBuffer, nSize, nCount, pf->bsdFilePtr, &pf->PALferrorCode);

    LOGEXIT( "fwrite returning size_t %d\n", nWrittenBytes );
    PERF_EXIT(fwrite);
    return nWrittenBytes;
}

/*++
InternalFwrite

Wrapper for fwrite.

Input parameters:

pvBuffer = array of objects to write to the given file stream
nSize = size of a object in bytes
nCount = number of objects to write
f = stream to write characters to
pnErrorCode = reference to a PAL_FILE's fwrite error code field

Return value:
    Returns the number of objects written.
--*/
size_t
CorUnix::InternalFwrite(
    const void *pvBuffer,
    size_t nSize,
    size_t nCount,
    FILE *f,
    INT *pnErrorCode
    )
{
    size_t nWrittenBytes = 0;
    _ASSERTE(f != NULL);

#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
    clearerr(f);
#endif

    nWrittenBytes = fwrite(pvBuffer, nSize, nCount, f);

    // Make sure no error ocurred. 
    if ( nWrittenBytes < nCount )
    {
        // Set the FILE* error code 
        *pnErrorCode = PAL_FILE_ERROR;
    }

    return nWrittenBytes;
}


/*++
PAL_fseek

Wrapper function for fseek.

Input parameters:

pf = a given file stream
lOffset = distance from position to set file-position indicator
nWhence = method used to determine the file_position indicator location relative to lOffset

Return value:
    0 on success, -1 on failure.
--*/
int
_cdecl
PAL_fseek(
    PAL_FILE * pf, 
    LONG lOffset, 
    int nWhence
    )
{
    int nRet = 0;

    PERF_ENTRY(fseek);
    ENTRY( "fseek( %p, %ld, %d )\n", pf, lOffset, nWhence );

    nRet = fseek(pf ? pf->bsdFilePtr : NULL, lOffset, nWhence);

    LOGEXIT("fseek returning %d\n", nRet);
    PERF_EXIT(fseek);
    return nRet;
}
