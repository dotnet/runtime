// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    file.c

Abstract:

    Implementation of the file functions in the C runtime library that
    are Windows specific.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/cruntime.h"

#include "pal/thread.hpp"
#include "pal/threadsusp.hpp"

#include <unistd.h>
#include <errno.h>
#include <sys/stat.h>
#include <pthread.h>

#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
    #define CLEARERR(f) clearerr((f)->bsdFilePtr)
#else
    #define CLEARERR(f)
#endif

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/* Global variables storing the std streams.*/
PAL_FILE PAL_Stdout;
PAL_FILE PAL_Stdin;
PAL_FILE PAL_Stderr;

/*++

Function:

    CRTInitStdStreams.

    Initilizes the standard streams.
    Returns TRUE on success, FALSE otherwise.
--*/
BOOL CRTInitStdStreams()
{
    /* stdout */
    PAL_Stdout.bsdFilePtr = stdout;
    PAL_Stdout.PALferrorCode = PAL_FILE_NOERROR;
    PAL_Stdout.bTextMode = TRUE;

    /* stdin */
    PAL_Stdin.bsdFilePtr = stdin;
    PAL_Stdin.PALferrorCode = PAL_FILE_NOERROR;
    PAL_Stdin.bTextMode = TRUE;

    /* stderr */
    PAL_Stderr.bsdFilePtr = stderr;
    PAL_Stderr.PALferrorCode = PAL_FILE_NOERROR;
    PAL_Stderr.bTextMode = TRUE;
    return TRUE;
}

/*++
Function :

    MapFileOpenModes

    Maps Windows file open modes to Unix fopen modes and validates.

--*/
static LPSTR MapFileOpenModes(LPSTR str , BOOL * bTextMode)
{
    LPSTR retval = NULL;
    LPSTR temp = NULL;

    if (NULL == bTextMode)
    {
        ASSERT("MapFileOpenModes called with a NULL parameter for bTextMode.\n");
        return NULL;
    }

    *bTextMode = TRUE;

    if (NULL == str)
    {
        ASSERT("MapFileOpenModes called with a NULL parameter for str.\n");
        return NULL;
    }

    /* The PAL behaves differently for some Windows file open modes:

    c, n, S, R, and T: these are all hints to the system that aren't supported
    by the PAL. Since the user cannot depend on this behavior, it's safe to
    simply ignore these modes.

    D: specifies a file as temporary. This file is expected to be deleted when
    the last file descriptor is closed. The PAL does not support this behavior
    and asserts when this mode is used.

    t: represents opening in text mode. Calls to fdopen on Unix don't accept
    't' so it is silently stripped out. However, the PAL supports the mode by
    having the PAL wrappers do the translation of CR-LF to LF and vice versa.

    t vs. b: To get binary mode, you must explicitly use 'b'. If neither mode
    is specified on Windows, the default mode is defined by the global
    variable _fmode. The PAL simply defaults to text mode. After examining
    CLR usage patterns, the PAL behavior seems acceptable. */

    /* Check if the mode specifies deleting the temporary file
    automatically when the last file descriptor is closed.
    The PAL does not support this behavior. */
    if (NULL != strchr(str,'D'))
    {
        ASSERT("The PAL doesn't support the 'D' flag for _fdopen and fopen.\n");
        return NULL;
    }

    /* Check if the mode specifies opening in binary.
    If so, set the bTextMode to false. */
    if(NULL != strchr(str,'b'))
    {
        *bTextMode = FALSE;
    }

    retval = (LPSTR)PAL_malloc( ( strlen( str ) + 1 ) * sizeof( CHAR ) );
    if (NULL == retval)
    {
        ERROR("Unable to allocate memory.\n");
        return NULL;
    }

    temp = retval;
    while ( *str )
    {
        if ( *str == 'r' || *str == 'w' || *str == 'a' )
        {
            *temp = *str;
            temp++;
            if ( ( ++str != NULL ) && *str == '+' )
            {
                *temp = *str;
                temp++;
                str++;
            }
        }
        else
        {
            str++;
        }
    }
    *temp = '\0';
    return retval;
}

#if UNGETC_NOT_RETURN_EOF
/*++
Function :

    WriteOnlyMode

    Returns TRUE to if a file is opened in write-only mode,
    Otherwise FALSE.

--*/
static BOOL WriteOnlyMode(FILE* pFile)
{
    INT fd, flags;

    if (pFile != NULL)
    {
        fd = fileno(pFile);
        if ((flags = fcntl(fd, F_GETFL)) >= 0)
        {
            if ((flags & O_ACCMODE) == O_WRONLY)
            {
                return TRUE;
            }
        }
    }
    return FALSE;
}
#endif //UNGETC_NOT_RETURN_EOF

/*++
Function:
  _fdopen

see MSDN

--*/
PAL_FILE *
__cdecl
_fdopen(
    int handle,
    const char *mode)
{
    PAL_FILE *f = NULL;
    LPSTR supported = NULL;
    BOOL bTextMode = TRUE;

    PERF_ENTRY(_fdopen);
    ENTRY("_fdopen (handle=%d mode=%p (%s))\n", handle, mode, mode);

    _ASSERTE(mode != NULL);

    f = (PAL_FILE*)PAL_malloc( sizeof( PAL_FILE ) );
    if ( f )
    {
        supported = MapFileOpenModes( (char*)mode , &bTextMode);
        if ( !supported )
        {
            PAL_free(f);
            f = NULL;
            goto EXIT;
        }

        f->bsdFilePtr = (FILE *)fdopen( handle, supported );
        f->PALferrorCode = PAL_FILE_NOERROR;
        /* Make sure fdopen did not fail. */
        if ( !f->bsdFilePtr )
        {
            PAL_free( f );
            f = NULL;
        }

        PAL_free( supported );
        supported = NULL;
    }
    else
    {
        ERROR( "Unable to allocate memory for the PAL_FILE wrapper!\n" );
    }

EXIT:
    LOGEXIT( "_fdopen returns FILE* %p\n", f );
    PERF_EXIT(_fdopen);
    return f;
}


/*++

Function :
    fopen

see MSDN doc.

--*/
PAL_FILE *
__cdecl
PAL_fopen(const char * fileName, const char * mode)
{
    PAL_FILE *f = NULL;
    LPSTR supported = NULL;
    LPSTR UnixFileName = NULL;
    struct stat stat_data;
    BOOL bTextMode = TRUE;

    PERF_ENTRY(fopen);
    ENTRY("fopen ( fileName=%p (%s) mode=%p (%s))\n", fileName, fileName, mode , mode );

    _ASSERTE(fileName != NULL);
    _ASSERTE(mode != NULL);

    if ( *mode == 'r' || *mode == 'w' || *mode == 'a' )
    {
        supported = MapFileOpenModes( (char*)mode,&bTextMode);

        if ( !supported )
        {
            goto done;
        }

        UnixFileName = PAL__strdup(fileName);
        if (UnixFileName == NULL )
        {
            ERROR("PAL__strdup() failed\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }

        FILEDosToUnixPathA( UnixFileName );

        /*I am not checking for the case where stat fails
         *as fopen will handle the error more gracefully in case
         *UnixFileName is invalid*/
        if ((stat(UnixFileName, &stat_data) == 0 ) &&
            ((stat_data.st_mode & S_IFMT) == S_IFDIR))
        {
            goto done;
        }

        f = (PAL_FILE*)PAL_malloc( sizeof( PAL_FILE ) );
        if ( f )
        {
            f->bsdFilePtr =  (FILE*)fopen( UnixFileName, supported );
            f->PALferrorCode = PAL_FILE_NOERROR;
            f->bTextMode = bTextMode;
            if ( !f->bsdFilePtr )
            {
                /* Failed */
                PAL_free( f );
                f = NULL;
            }
#if UNGETC_NOT_RETURN_EOF
            else
            {
                f->bWriteOnlyMode = WriteOnlyMode(f->bsdFilePtr);
            }
#endif //UNGETC_NOT_RETURN_EOF
        }
        else
        {
            ERROR( "Unable to allocate memory to the PAL_FILE wrapper\n" );
        }
    }
    else
    {
        ERROR( "The mode flags must start with either an a, w, or r.\n" );
    }

done:
    PAL_free( supported );
    supported = NULL;
    PAL_free( UnixFileName );

    LOGEXIT( "fopen returns FILE* %p\n", f );
    PERF_EXIT(fopen);
    return f;
}

/*++
Function:
  _wfopen

see MSDN doc.

--*/
PAL_FILE *
__cdecl
_wfopen(
    const wchar_16 *fileName,
    const wchar_16 *mode)
{
    CHAR mbFileName[ _MAX_PATH ];
    CHAR mbMode[ 10 ];
    PAL_FILE * filePtr = NULL;

    PERF_ENTRY(_wfopen);
    ENTRY("_wfopen(fileName:%p (%S), mode:%p (%S))\n", fileName, fileName, mode, mode);

    _ASSERTE(fileName != NULL);
    _ASSERTE(mode != NULL);

    /* Convert the parameters to ASCII and defer to PAL_fopen */
    if ( WideCharToMultiByte( CP_ACP, 0, fileName, -1, mbFileName,
                              sizeof mbFileName, NULL, NULL ) != 0 )
    {
        if ( WideCharToMultiByte( CP_ACP, 0, mode, -1, mbMode,
                                  sizeof mbMode, NULL, NULL ) != 0 )
        {
            filePtr = PAL_fopen(mbFileName, mbMode);
        }
        else
        {
            ERROR( "An error occurred while converting mode to ANSI.\n" );
        }
    }
    else
    {
        ERROR( "An error occurred while converting"
               " fileName to ANSI string.\n" );
    }
    LOGEXIT("_wfopen returning FILE* %p\n", filePtr);
    PERF_EXIT(_wfopen);
    return filePtr;
}

/*++
Function
    PAL_get_stdout.

    Returns the stdout stream.
--*/
PAL_FILE * __cdecl PAL_get_stdout(int caller)
{
    PERF_ENTRY(get_stdout);
    ENTRY("PAL_get_stdout\n");
    LOGEXIT("PAL_get_stdout returns PAL_FILE * %p\n", &PAL_Stdout );
    PERF_EXIT(get_stdout);
    return &PAL_Stdout;
}

/*++
Function
    PAL_get_stdin.

    Returns the stdin stream.
--*/
PAL_FILE * __cdecl PAL_get_stdin(int caller)
{
    PERF_ENTRY(get_stdin);
    ENTRY("PAL_get_stdin\n");
    LOGEXIT("PAL_get_stdin returns PAL_FILE * %p\n", &PAL_Stdin );
    PERF_EXIT(get_stdin);
    return &PAL_Stdin;
}

/*++
Function
    PAL_get_stderr.

    Returns the stderr stream.
--*/
PAL_FILE * __cdecl PAL_get_stderr(int caller)
{
    PERF_ENTRY(get_stderr);
    ENTRY("PAL_get_stderr\n");
    LOGEXIT("PAL_get_stderr returns PAL_FILE * %p\n", &PAL_Stderr );
    PERF_EXIT(get_stderr);
    return &PAL_Stderr;
}

/*++

Function:

    PAL_pread

See msdn for more details.
--*/
size_t __cdecl PAL__pread(int fd, void *buf, size_t nbytes, ULONG64 offset)
{
    return pread(fd, buf, nbytes, offset);
}

/*++

Function:

    _close

See msdn for more details.
--*/
int __cdecl PAL__close(int handle)
{
    INT nRetVal = 0;

    PERF_ENTRY(_close);
    ENTRY( "_close( handle=%d )\n", handle );

    nRetVal = close( handle );

    LOGEXIT( "_close returning %d.\n", nRetVal );
    PERF_EXIT(_close);
    return nRetVal;
}

int __cdecl PAL__flushall()
{
    return fflush(NULL);
}

int __cdecl PAL_getc(PAL_FILE *stream);

/*++
Function :

    fread

    See MSDN for more details.
--*/

size_t
__cdecl
PAL_fread(void * buffer, size_t size, size_t count, PAL_FILE * f)
{
    size_t nReadBytes = 0;

    PERF_ENTRY(fread);
    ENTRY( "fread( buffer=%p, size=%d, count=%d, f=%p )\n",
           buffer, size, count, f );

    _ASSERTE(f != NULL);

    CLEARERR(f);

    if(f->bTextMode != TRUE)
    {
        nReadBytes = fread( buffer, size, count, f->bsdFilePtr );
    }
    else
    {
        size_t i=0;
        if(size > 0)
        {
            size_t j=0;
            LPSTR temp = (LPSTR)buffer;
            int nChar = 0;
            int nCount =0;

            for(i=0; i< count; i++)
            {
                for(j=0; j< size; j++)
                {
                    if((nChar = PAL_getc(f)) == EOF)
                    {
                        nReadBytes = i;
                        goto done;
                    }
                    else
                    {
                        temp[nCount++]= (char)nChar;
                    }
                }
            }
        }
        nReadBytes = i;
    }

done:
    LOGEXIT( "fread returning size_t %d\n", nReadBytes );
    PERF_EXIT(fread);
    return nReadBytes;
}


/*++
Function :

    ferror

    See MSDN for more details.
--*/
int
_cdecl
PAL_ferror(PAL_FILE * f)
{
    INT nErrorCode = PAL_FILE_NOERROR;

    PERF_ENTRY(ferror);
    ENTRY( "ferror( f=%p )\n", f );

    _ASSERTE(f != NULL);

    nErrorCode = ferror( f->bsdFilePtr );
    if ( 0 == nErrorCode )
    {
        /* See if the PAL file error code is set. */
        nErrorCode = f->PALferrorCode;
    }

    LOGEXIT( "ferror returns %d\n", nErrorCode );
    PERF_EXIT(ferror);
    return nErrorCode;
}


/*++
Function :

    fclose

    See MSDN for more details.
--*/
int
_cdecl
PAL_fclose(PAL_FILE * f)
{
    INT nRetVal = 0;

    PERF_ENTRY(fclose);
    ENTRY( "fclose( f=%p )\n", f );

    _ASSERTE(f != NULL);

    CLEARERR(f);

    nRetVal = fclose( f->bsdFilePtr );
    PAL_free( f );

    LOGEXIT( "fclose returning %d\n", nRetVal );
    PERF_EXIT(fclose);
    return nRetVal;
}

/*++
Function :

    fputs

    See MSDN for more details.
--*/
int
_cdecl
PAL_fputs(const char * str,  PAL_FILE * f)
{
    INT nRetVal = 0;

    PERF_ENTRY(fputs);
    ENTRY( "fputs( %p (%s), %p )\n", str, str, f);

    _ASSERTE(str != NULL);
    _ASSERTE(f != NULL);

    CLEARERR(f);

    nRetVal = fputs( str, f->bsdFilePtr );

    LOGEXIT( "fputs returning %d\n", nRetVal );
    PERF_EXIT(fputs);
    return nRetVal;
}

/*++
Function :

    ftell

    See MSDN for more details.
--*/
LONG
_cdecl
PAL_ftell(PAL_FILE * f)
{
    long lRetVal = 0;

    PERF_ENTRY(ftell);
    ENTRY( "ftell( %p )\n", f );

    _ASSERTE(f != NULL);
    lRetVal = ftell( f->bsdFilePtr );

#ifdef HOST_64BIT
    /* Windows does not set an error if the file pointer's position
    is greater than _I32_MAX. It just returns -1. */
    if (lRetVal > _I32_MAX)
    {
        lRetVal = -1;
    }
#endif

    LOGEXIT( "ftell returning %ld\n", lRetVal );
    PERF_EXIT(ftell);
    /* This explicit cast to LONG is used to silence any potential warnings
    due to implicitly casting the native long lRetVal to LONG when returning. */
    return (LONG)lRetVal;
}

/*++
Function :
    getc

    See MSDN for more details.
--*/
int
_cdecl
PAL_getc(PAL_FILE * f)
{
    INT nRetVal = 0;
    INT temp =0;

    PERF_ENTRY(getc);
    ENTRY( "getc( %p )\n", f );

    _ASSERTE(f != NULL);

    CLEARERR(f);

    nRetVal = getc( f->bsdFilePtr );

    if ( (f->bTextMode) && (nRetVal == '\r') )
    {
        if ((temp = getc( f->bsdFilePtr ))== '\n')
        {
            nRetVal ='\n';
        }
        else if (EOF == ungetc( temp, f->bsdFilePtr ))
        {
            ERROR("ungetc operation failed\n");
        }
    }

    LOGEXIT( "getc returning %d\n", nRetVal );
    PERF_EXIT(getc);
    return nRetVal;
}

/*++
Function :

    setvbuf

    See MSDN for more details.
--*/
int
_cdecl
PAL_setvbuf(PAL_FILE *f, char *buf, int type, size_t size)
{
    INT nRetVal = 0;

    PERF_ENTRY(setvbuf);
    ENTRY( "setvbuf( %p, %p, %d, %ul )\n", f, buf, type, size);

    _ASSERTE(f != NULL);

    nRetVal = setvbuf(f->bsdFilePtr, buf, type, size);

    LOGEXIT( "setvbuf returning %d\n", nRetVal );
    PERF_EXIT(setvbuf);
    return nRetVal;
}
