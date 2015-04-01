//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    fmtmessage.c

Abstract:

    Implementation of FormatMessage function.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/unicode_data.h"
#include "pal/critsect.h"
#include "pal/module.h"
#include "pal/misc.h"

#include "pal/printfcpp.hpp"

#include <stdarg.h>
#if NEED_DLCOMPAT
#include "dlcompat.h"
#else   // NEED_DLCOMPAT
#include <dlfcn.h>
#endif  // NEED_DLCOMPAT
#include <errno.h>

SET_DEFAULT_DEBUG_CHANNEL(MISC);

/* Defines */

/* The PAL_GetSatelliteStringW function assumes the buffer is going to be
   big enough. */
#define MAX_SAT_STRING_LENGTH 511

/* Function pointers and handles. */
typedef VOID * HSATELLITE;
static HSATELLITE s_hSatellite = NULL;
static LPVOID s_lpLibRotorPalRt = NULL;
typedef HSATELLITE (__stdcall *FnLoadSatelliteResource)(LPCWSTR);
typedef BOOL (__stdcall *FnFreeSatelliteResource)(HSATELLITE);
static FnFreeSatelliteResource FreeSatelliteResource;
#if 0
static FnLoadSatelliteResource LoadSatelliteResource;
typedef UINT (__stdcall *FnLoadSatelliteStringW)(HSATELLITE, UINT, LPWSTR, UINT);
static FnLoadSatelliteStringW LoadSatelliteStringW;

/*++
Function :

    FMTMSG_LoadLibrary
    
    Loads the dynamic library
--*/
static LPVOID FMTMSG_LoadLibrary( )
{
    CHAR PathAndFileName[ MAX_PATH ];
    LPVOID lpLibRotorPalRt;
    
    if ( !PAL_GetPALDirectoryA( PathAndFileName, MAX_PATH ) )
    {
        ERROR( "Unable to retrieve the path.\n" );
        goto error;
    }

#define ROTOR_PALRT PAL_SHLIB_PREFIX "rotor_palrt" PAL_SHLIB_SUFFIX

    if (strncat_s( PathAndFileName, sizeof(PathAndFileName), ROTOR_PALRT, MAX_PATH ) != SAFECRT_SUCCESS)
    {
        ERROR( "strncat_s failed!\n" );
        goto error;
    }

    TRACE( "%s'\n",PathAndFileName ); 

    /* the refcounting in dlopen / dlclose calls does not tend to be threadsafe - take 
       the modulelist lock to avoid a potential race condition from the PAL side */
    LockModuleList();
    lpLibRotorPalRt = dlopen( PathAndFileName, RTLD_LAZY );
    if ( lpLibRotorPalRt )
    {
        if ( InterlockedCompareExchangePointer(&s_lpLibRotorPalRt, lpLibRotorPalRt, NULL) != NULL )
        {
            /* somebody beat us to it */
            dlclose( lpLibRotorPalRt );
        }
    }
    else
    {
        ERROR( "%s\n", strerror( errno ) );
    }
    UnlockModuleList();

error:
    return s_lpLibRotorPalRt;
}

/*++
Function :

    FMTMSG_FormatMessageInit
    
    Loads the dynamic library, resolves symbols.
    
    Loads the satellite file into memory.
--*/
static HSATELLITE FMTMSG_FormatMessageInit( void )
{
    static const WCHAR ROTORPALSATFILE[] = {
        'p','a','l','.','s','a','t','e','l','l','i','t','e', '\0'
    };
    
    WCHAR SatPathAndFile[ MAX_PATH ];

    HSATELLITE hSatellite;

    LPVOID lpLibRotorPalRt;

    TRACE( "Initilizing the dynamic library and the satellite files.\n" );

    lpLibRotorPalRt = s_lpLibRotorPalRt;

    if ( !lpLibRotorPalRt )
    {
        lpLibRotorPalRt = FMTMSG_LoadLibrary( );
        if ( !lpLibRotorPalRt )
        {
            ERROR( "Unable to load the shared library. Reason %s.\n", dlerror() );
            goto error;
        }
    }

    /* Get the symbols. */
    LoadSatelliteResource = reinterpret_cast<FnLoadSatelliteResource>(
        dlsym( lpLibRotorPalRt, "PAL_LoadSatelliteResourceW" ));
    FreeSatelliteResource = reinterpret_cast<FnFreeSatelliteResource>(
        dlsym( lpLibRotorPalRt, "PAL_FreeSatelliteResource" ));
    LoadSatelliteStringW = reinterpret_cast<FnLoadSatelliteStringW>(
        dlsym( lpLibRotorPalRt, "PAL_LoadSatelliteStringW" ));

    if ( !LoadSatelliteResource || !FreeSatelliteResource || 
            !LoadSatelliteStringW )
    {
        ERROR( "Unable to load the shared library symbols. "
                "Reason %s.\n", dlerror() );
        goto error;
    }

    /* Load the satellite file. */
    if ( !PAL_GetPALDirectoryW( SatPathAndFile, MAX_PATH ) )
    {
        ERROR( "Unable to retrieve the path.\n" );
        goto error;
    }
   
    PAL_wcsncat( SatPathAndFile, ROTORPALSATFILE, MAX_PATH );
    hSatellite = ((*LoadSatelliteResource)( SatPathAndFile ));

    if ( !hSatellite )
    {
        ERROR( "Unable to load the satellite file\n" );
        goto error;
    }

    if ( InterlockedCompareExchangePointer(&s_hSatellite, hSatellite, NULL) != NULL )
    {
        /* somebody beat us to it */
        (*FreeSatelliteResource)(hSatellite);
    }

error:
    return s_hSatellite;
}

#endif // 0

/*++
Function :

    FMTMSG_FormatMessageCleanUp
    
    Frees the satellite file from memory.
    Closes the dynamic library.
    Releases all resources used by FormatMessage,
    including the satellite file and critical section.
    
--*/
BOOL FMTMSG_FormatMessageCleanUp( void )
{    
    TRACE( "Cleaning up the dynamic library and the satellite files.\n" );
    if ( s_lpLibRotorPalRt )
    {
        if (s_hSatellite)
        {
            (*FreeSatelliteResource)(s_hSatellite);
            s_hSatellite = NULL;
        }
        if ( dlclose( s_lpLibRotorPalRt ) != 0 )
        {
            ASSERT( "Unable to close the dynamic library\n" );
        }
        s_lpLibRotorPalRt = NULL;
    }
    return TRUE;
}

/*++
Function:
    
    FMTMSG_GetMessageString
    
Returns the message as a wide string.
--*/
static LPWSTR FMTMSG_GetMessageString( DWORD dwErrCode )
{
    TRACE("Entered FMTMSG_GetMessageString\n");

    LPWSTR lpRetVal = NULL;
#if 0
    HSATELLITE hSatellite;

    hSatellite = s_hSatellite;
    if ( hSatellite == NULL )
    {
        hSatellite = FMTMSG_FormatMessageInit();
        if ( !hSatellite )
        {
            ASSERT( "Unable to continue due to missing library.\n" );
            SetLastError( ERROR_INTERNAL_ERROR );
            goto error;
        }
    }
        
    lpRetVal = 
        (LPWSTR)LocalAlloc( LMEM_FIXED, (MAX_SAT_STRING_LENGTH + 1 ) 
                            * sizeof( WCHAR ) );
    if ( lpRetVal )
    {
        if ( ((*LoadSatelliteStringW)( hSatellite, dwErrCode, lpRetVal, 
                                        MAX_SAT_STRING_LENGTH ) ) != 0 )
        {
            /* Lets see if we can save memory here. */
            UINT Length;
            LPWSTR temp;
            Length = PAL_wcslen( lpRetVal ) + 1; 
            temp = static_cast<WCHAR *>(
                LocalAlloc( LMEM_FIXED, Length * sizeof( WCHAR ) ) );
            
            if ( temp )
            {
                memcpy( temp, lpRetVal, Length*sizeof(WCHAR) );
                LocalFree( lpRetVal );
                lpRetVal = temp;
            }
            else
            {
                WARN( "Memory is running low. Continuing "
                        "with original memory allocation.\n" );
            }
        }
        else
        {
            ERROR( "LoadSatelliteStringW failed!\n" );
            LocalFree( lpRetVal );
            lpRetVal = NULL;
        }
    }
    else
    {
        ERROR( "Unable to allocate memory.\n" );
    }

error:

#else // 0

    // UNIXTODO: Implement real string loading from resources

    lpRetVal =
        (LPWSTR)LocalAlloc(LMEM_FIXED, (MAX_SAT_STRING_LENGTH + 1)
        * sizeof(WCHAR));

    if (lpRetVal)
    {
        _snwprintf(lpRetVal, MAX_SAT_STRING_LENGTH, W("Error %u"), dwErrCode);
    }
    else
    {
        ERROR("Unable to allocate memory.\n");
    }

#endif // 0

    return lpRetVal;
}

/*++

Function :

    FMTMSG__watoi
    
    Converts a wide string repersentation of an integer number 
    into a interger number.

    Returns a integer number, or 0 on failure. 0 is not a valid number
    for FormatMessage inserts.
    
--*/
static INT FMTMSG__watoi( LPWSTR str )
{
    CONST UINT MAX_NUMBER_LENGTH = 3;
    CHAR buf[ MAX_NUMBER_LENGTH ];
    INT nRetVal = 0;
    
    nRetVal = WideCharToMultiByte( CP_ACP, 0, str, -1, buf, 
                                   MAX_NUMBER_LENGTH, NULL, 0 );

    if ( nRetVal != 0 )
    {
        return atoi( buf );
    }
    else
    {
        ERROR( "Unable to convert the string to a number.\n" );
        return 0;
    }
}
            
/* Adds the character to the working string. */
#define _ADD_TO_STRING( c ) \
{\
   TRACE( "Adding %c to the string.\n", (CHAR)c );\
   *lpWorkingString = c;\
    lpWorkingString++;\
    nCount++;\
}

/* Grows the buffer. */
#define _GROW_BUFFER() \
{\
    if ( bIsLocalAlloced ) \
    { \
        LPWSTR lpTemp = NULL; \
        UINT NumOfBytes = 0; \
        nSize *= 2; \
        NumOfBytes = nSize * sizeof( WCHAR ); \
        lpTemp = static_cast<WCHAR *>( LocalAlloc( LMEM_FIXED, NumOfBytes ) ); \
        TRACE( "Growing the buffer.\n" );\
        \
        if ( !lpTemp ) \
        { \
            ERROR( "Out of buffer\n" ); \
            SetLastError( ERROR_NOT_ENOUGH_MEMORY ); \
            nCount = 0; \
            lpWorkingString = NULL; \
            goto exit; \
        } \
        \
        *lpWorkingString = '\0';\
        PAL_wcscpy( lpTemp, lpReturnString );\
        LocalFree( lpReturnString ); \
        lpWorkingString = lpReturnString = lpTemp; \
        lpWorkingString += nCount; \
    } \
    else \
    { \
        WARN( "Out of buffer.\n" ); \
        SetLastError( ERROR_INSUFFICIENT_BUFFER ); \
        nCount = 0; \
        lpWorkingString = NULL; \
        goto exit; \
    } \
}
/* Adds a character to the working string.  This is a safer version
of _ADD_TO_STRING, as we will resize the buffer if necessary. */
#define _CHECKED_ADD_TO_STRING( c ) \
{\
    if ( nCount+1 == nSize ) \
    {\
        _GROW_BUFFER();\
    } \
    _ADD_TO_STRING( c );\
}


/*++
Function :

    FMTMSG_ProcessPrintf
    
    Processes the printf formatters based on the format.
        
    Returns the LPWSTR string, or NULL on failure.
*/
    
static LPWSTR FMTMSG_ProcessPrintf( wchar_t c , 
                                 LPWSTR lpPrintfString,
                                 LPWSTR lpInsertString)
{
    LPWSTR lpBuffer = NULL;
    LPWSTR lpBuffer2 = NULL;
    LPWSTR lpFormat = NULL;
#if _DEBUG
    // small size for _DEBUG to exercise buffer reallocation logic
    int tmpSize = 4;
#else
    int tmpSize = 64;
#endif
    UINT nFormatLength = 0;
    int nBufferLength = 0;

    TRACE( "FMTMSG_ProcessPrintf( %C, %S, %S )\n", c, 
           lpPrintfString, lpInsertString );

    switch ( c )
    {
    case 'e' :
        /* Fall through */
    case 'E' :
        /* Fall through */
    case 'f' :
        /* Fall through */
    case 'g' :
        /* Fall through */
    case 'G' : 
        ERROR( "%%%c is not supported by FormatMessage.\n", c );
        SetLastError( ERROR_INVALID_PARAMETER );
        return NULL;
    }

    nFormatLength = PAL_wcslen( lpPrintfString ) + 2; /* Need to count % AND NULL */
    lpFormat = (LPWSTR)PAL_malloc( nFormatLength * sizeof( WCHAR ) );
    if ( !lpFormat )
    {
        ERROR( "Unable to allocate memory.\n" );
        SetLastError( ERROR_NOT_ENOUGH_MEMORY );
        return NULL;
    }
    /* Create the format string. */
    memset( lpFormat, 0, nFormatLength * sizeof(WCHAR) );
    *lpFormat = '%';
    
    PAL_wcscat( lpFormat, lpPrintfString );
 
    lpBuffer = (LPWSTR) PAL_malloc(tmpSize*sizeof(WCHAR));
        
    /* try until the buffer is big enough */
    while (TRUE)
    {
        if (!lpBuffer)
        {
            ERROR("Unable to allocate memory\n");
            SetLastError( ERROR_NOT_ENOUGH_MEMORY );
            PAL_free(lpFormat);
            return NULL;
        }
        nBufferLength = _snwprintf_s( lpBuffer, tmpSize,  tmpSize, 
                                    lpFormat, lpInsertString);

        if ((nBufferLength >= 0) && (nBufferLength != tmpSize))
        {
            break; /* succeeded */
        }
        else
        {
            tmpSize *= 2;
            lpBuffer2 = static_cast<WCHAR *>(
                PAL_realloc(lpBuffer, tmpSize*sizeof(WCHAR)));
            if (lpBuffer2 == NULL)
                PAL_free(lpBuffer);
            lpBuffer = lpBuffer2;
        }
    }

    PAL_free( lpFormat );
    lpFormat = NULL;

    return lpBuffer;
}

/*++
Function:
  FormatMessageW

See MSDN doc.
--*/
DWORD
PALAPI
FormatMessageW(
           IN DWORD dwFlags,
           IN LPCVOID lpSource,
           IN DWORD dwMessageId,
           IN DWORD dwLanguageId,
           OUT LPWSTR lpBuffer,
           IN DWORD nSize,
           IN va_list *Arguments)
{
    BOOL bIgnoreInserts = FALSE;
    BOOL bIsVaList = TRUE;
    BOOL bIsLocalAlloced = FALSE;
    LPWSTR lpSourceString = NULL;
    UINT nCount = 0;
    LPWSTR lpReturnString = NULL;
    LPWSTR lpWorkingString = NULL; 
    

    PERF_ENTRY(FormatMessageW);
    ENTRY( "FormatMessageW(dwFlags=%#x, lpSource=%p, dwMessageId=%#x, "
           "dwLanguageId=%#x, lpBuffer=%p, nSize=%u, va_list=%p)\n", 
           dwFlags, lpSource, dwMessageId, dwLanguageId, lpBuffer, nSize,
           Arguments);
    
    /* Sanity checks. */
    if ( dwFlags & FORMAT_MESSAGE_FROM_STRING && !lpSource )
    {
        /* This behavior is different then in Windows.  
           Windows would just crash.*/
        ERROR( "lpSource cannot be NULL.\n" );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto exit;
    }

    if ( !(dwFlags & FORMAT_MESSAGE_ALLOCATE_BUFFER ) && !lpBuffer )
    {
        /* This behavior is different then in Windows.  
           Windows would just crash.*/
        ERROR( "lpBuffer cannot be NULL, if "
               " FORMAT_MESSAGE_ALLOCATE_BUFFER is not specified.\n" );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto exit;
    }
    
    if ( ( dwFlags & FORMAT_MESSAGE_FROM_STRING ) && 
         ( dwFlags & FORMAT_MESSAGE_FROM_SYSTEM ) )
    {
        ERROR( "These flags cannot co-exist. You can either "
               "specify FORMAT_MESSAGE_FROM_STRING, or "
               "FORMAT_MESSAGE_FROM_SYSTEM.\n" );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto exit;
    }
    
    if ( !( dwFlags & FORMAT_MESSAGE_FROM_STRING ) && 
         ( dwLanguageId != 0
#if ENABLE_DOWNLEVEL_FOR_NLS         
         && dwLanguageId != MAKELANGID( LANG_NEUTRAL, SUBLANG_DEFAULT ) 
#endif
         ) )
    {
        ERROR( "Invalid language indentifier.\n" );
        SetLastError( ERROR_RESOURCE_LANG_NOT_FOUND );
        goto exit;
    }

    /* Parameter processing. */
    if ( dwFlags & FORMAT_MESSAGE_ALLOCATE_BUFFER )
    {
        TRACE( "Allocated %d TCHARs. Don't forget to call LocalFree to "
               "free the memory when done.\n", nSize );
        bIsLocalAlloced = TRUE;
    }
    
    if ( dwFlags & FORMAT_MESSAGE_IGNORE_INSERTS )
    {
        bIgnoreInserts = TRUE;
    }

    if ( dwFlags & FORMAT_MESSAGE_ARGUMENT_ARRAY )
    {
        if ( !Arguments && !bIgnoreInserts )
        {
            ERROR( "The va_list cannot be NULL.\n" );
            SetLastError( ERROR_INVALID_PARAMETER );
            goto exit;
        }
        else
        {
            bIsVaList = FALSE;
        }
    }
    
    if ( dwFlags & FORMAT_MESSAGE_FROM_STRING )
    {
        lpSourceString = (LPWSTR)lpSource;
    }
    else if ( dwFlags & FORMAT_MESSAGE_FROM_SYSTEM ) 
    {
        if ((dwMessageId & 0xFFFF0000) == 0x80070000)
        {
            // This message has been produced by HRESULT_FROM_WIN32.  Undo its work.
            dwMessageId &= 0xFFFF;
        }

        lpWorkingString = lpReturnString = 
            FMTMSG_GetMessageString( dwMessageId );
        
        if ( !lpWorkingString )
        {
            ERROR( "Unable to find the message %d.\n", dwMessageId );
            SetLastError( ERROR_INTERNAL_ERROR );
            nCount = 0;
            goto exit;
        }

        nCount = PAL_wcslen( lpWorkingString );
        
        if ( !bIsLocalAlloced && nCount > nSize )
        {
            ERROR( "Insufficient buffer.\n" );
            SetLastError( ERROR_INSUFFICIENT_BUFFER );
            lpWorkingString = NULL;
            nCount = 0;
            goto exit;
        }
        if ( !lpWorkingString )
        {
            ERROR( "Invalid error indentifier.\n" );
            SetLastError( ERROR_INVALID_ADDRESS );
        }
        goto exit;
    }
    else
    {
        ERROR( "Unknown flag.\n" );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto exit;
    }

    if ( nSize == 0 && bIsLocalAlloced )
    {
        nSize = 1;
    }

    lpWorkingString = static_cast<WCHAR *>(
        LocalAlloc( LMEM_FIXED, nSize * sizeof( WCHAR ) ) );
    if ( !lpWorkingString )
    {
        ERROR( "Unable to allocate memory for the working string.\n" );
        SetLastError( ERROR_INSUFFICIENT_BUFFER );
        goto exit;
    }


    /* Process the string. */
    lpReturnString = lpWorkingString;
    while ( *lpSourceString )
    {
        if ( *lpSourceString == '%' && !bIgnoreInserts )
        {
            lpSourceString++;
            /* Escape sequences. */
            if ( *lpSourceString == '0' )
            {
                /* Terminates a message without a newline character. */
                *lpWorkingString = '\0';
                goto exit;
            }
            else if ( PAL_iswdigit( *lpSourceString ) )
            {
                /* Get the insert number. */
                WCHAR Number[] = { '\0', '\0', '\0' };
                SIZE_T Index = 0;

                Number[ 0 ] = *lpSourceString;
                lpSourceString++;
                
                if ( PAL_iswdigit( *lpSourceString ) )
                {
                    Number[ 1 ] = *lpSourceString;
                    lpSourceString++;
                    if ( PAL_iswdigit( *lpSourceString ) )
                    {
                        ERROR( "Invalid insert indentifier.\n" );
                        SetLastError( ERROR_INVALID_PARAMETER );
                        lpWorkingString = NULL;
                        nCount = 0;
                        goto exit;
                    }
                }
                Index = FMTMSG__watoi( Number );
                if ( Index == 0 )
                {
                    ERROR( "Invalid insert indentifier.\n" );
                    SetLastError( ERROR_INVALID_PARAMETER );
                    lpWorkingString = NULL;
                    nCount = 0;
                    goto exit;
                }
                if ( *lpSourceString == '!' )
                {
                    LPWSTR lpInsertString = NULL;
                    LPWSTR lpPrintfString = NULL;
                    LPWSTR lpStartOfFormattedString = NULL;
                    UINT nPrintfLength = 0;
                    LPWSTR lpFormattedString = NULL;
                    UINT nFormattedLength = 0;

                    if ( !bIsVaList )
                    {
                        lpInsertString = ((LPWSTR*)Arguments)[ Index - 1 ];
                    }
                    else
                    {
                        va_list TheArgs;
                        
                        va_copy(TheArgs, *Arguments);
                        UINT i = 0;
                        for ( ; i < Index; i++ )
                        {
                            lpInsertString = va_arg( TheArgs, LPWSTR );
                        }
                    }

                    /* Calculate the length, and extract the printf string.*/
                    lpSourceString++;
                    {
                        LPWSTR p = PAL_wcschr( lpSourceString, '!' );

                        if ( NULL == p )
                        {
                            nPrintfLength = 0;
                        }
                        else
                        {
                            nPrintfLength = p - lpSourceString;
                        }
                    }
                                        
                    lpPrintfString = 
                        (LPWSTR)PAL_malloc( ( nPrintfLength + 1 ) * sizeof( WCHAR ) );
                    
                    if ( !lpPrintfString )
                    {
                        ERROR( "Unable to allocate memory.\n" );
                        SetLastError( ERROR_NOT_ENOUGH_MEMORY );
                        lpWorkingString = NULL;
                        nCount = 0;
                        goto exit;
                    }
                    
                    PAL_wcsncpy( lpPrintfString, lpSourceString, nPrintfLength );
                    *( lpPrintfString + nPrintfLength ) = '\0';

                    lpStartOfFormattedString = lpFormattedString = 
                           FMTMSG_ProcessPrintf( *lpPrintfString, 
                                                 lpPrintfString, 
                                                 lpInsertString);

                    if ( !lpFormattedString )
                    {
                        ERROR( "Unable to process the format string.\n" );
                        /* Function will set the error code. */
                        PAL_free( lpPrintfString );
                        lpWorkingString = NULL;
                        goto exit;
                    }
                     

                    nFormattedLength = PAL_wcslen( lpFormattedString );
                    
                    /* Append the processed printf string into the working string */
                    while ( *lpFormattedString )
                    {
                        _CHECKED_ADD_TO_STRING( *lpFormattedString );
                        lpFormattedString++;
                    }
                    
                    lpSourceString += nPrintfLength + 1;
                    PAL_free( lpPrintfString );
                    PAL_free( lpStartOfFormattedString );
                    lpPrintfString = lpFormattedString = NULL;
                }
                else
                {
                    /* The printf format string defaults to 's'.*/
                    LPWSTR lpInsert = NULL;

                    if ( !bIsVaList )
                    {
                        lpInsert = ((LPWSTR*)Arguments)[Index - 1];
                    }
                    else
                    {
                        va_list TheArgs;
                        va_copy(TheArgs, *Arguments);
                        UINT i = 0;
                        for ( ; i < Index; i++ )
                        {
                            lpInsert = va_arg( TheArgs, LPWSTR );
                        }
                    }

                    while ( *lpInsert )
                    {
                        _CHECKED_ADD_TO_STRING( *lpInsert );
                        lpInsert++;
                    }
                }
            }
            /* Format specifiers. */
            else if ( *lpSourceString == '%' )
            {
                _CHECKED_ADD_TO_STRING( '%' );
                lpSourceString++;
            }
            else if ( *lpSourceString == 'n' )
            {
                /* Hard line break. */
                _CHECKED_ADD_TO_STRING( '\n' );
                lpSourceString++;
            }
            else if ( *lpSourceString == '.' )
            {
                _CHECKED_ADD_TO_STRING( '.' );
                lpSourceString++;
            }
            else if ( *lpSourceString == '!' )
            {
                _CHECKED_ADD_TO_STRING( '!' );
                lpSourceString++;
            }
            else if ( !*lpSourceString )
            {
                ERROR( "Invalid parameter.\n" );
                SetLastError( ERROR_INVALID_PARAMETER );
                lpWorkingString = NULL;
                nCount = 0;
                goto exit;
            }
            else /* Append the character. */
            {
                _CHECKED_ADD_TO_STRING( *lpSourceString );
                lpSourceString++;
            }
        }/* END if ( *lpSourceString == '%' ) */
        else
        {
            /* In Windows if FormatMessage is called with ignore inserts,
            then FormatMessage strips %1!s! down to %1, since string is the
            default. */
            if ( bIgnoreInserts && *lpSourceString == '!' && 
                 *( lpSourceString + 1 ) == 's' )
            {
                LPWSTR lpLastBang = PAL_wcschr( lpSourceString + 1, '!' );

                if ( lpLastBang && ( 2 == lpLastBang - lpSourceString ) )
                {
                    lpSourceString = lpLastBang + 1;
                }
                else
                {
                    ERROR( "Mal-formed string\n" );
                    SetLastError( ERROR_INVALID_PARAMETER );
                    lpWorkingString = NULL;
                    nCount = 0;
                    goto exit;
                }
            }
            else
            {
                /* Append to the string. */
                _CHECKED_ADD_TO_STRING( *lpSourceString );
                lpSourceString++;
            }
        }
    }
    
    /* Terminate the message. */
    _CHECKED_ADD_TO_STRING( '\0' );
    /* NULL does not count. */
    nCount--;

exit: /* Function clean-up and exit. */
    if ( lpWorkingString )
    {
        if ( bIsLocalAlloced )
        {
            TRACE( "Assigning the buffer to the pointer.\n" );
            // when FORMAT_MESSAGE_ALLOCATE_BUFFER is specified, nSize
            // does not specify the size of lpBuffer, rather it specifies
            // the minimum size of the string
            // as such we have to blindly assume that lpBuffer has enough space to
            // store PVOID
            // might cause a prefast warning, but there is no good way to suppress it yet
            _ASSERTE(dwFlags & FORMAT_MESSAGE_ALLOCATE_BUFFER);
            *((LPVOID*)lpBuffer) = (LPVOID)lpReturnString;
        }
        else /* Only delete lpReturnString if the caller has their own buffer.*/
        {
            TRACE( "Copying the string into the buffer.\n" );
            PAL_wcsncpy( lpBuffer, lpReturnString, nCount + 1 );
            LocalFree( lpReturnString );
        }
    }
    else /* Error, something occured. */
    {
        if ( lpReturnString )
        {
            LocalFree( lpReturnString );
        }
    }
    LOGEXIT( "FormatMessageW returns %d.\n", nCount );
    PERF_EXIT(FormatMessageW);
    return nCount;
}
