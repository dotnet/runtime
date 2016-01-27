// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    path.c

Abstract:

    Implementation of path functions part of Windows runtime library.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/printfcpp.hpp"

#include <string.h>
#include <stdlib.h>
#include <sys/param.h>
#include <errno.h>
#include <limits.h>

SET_DEFAULT_DEBUG_CHANNEL(CRT);


/* ON_ERROR. A Helper macro for _?splitpath functions. */
#define ON_ERROR if ( drive ) \
        {\
            drive[0] = 0;\
        }\
        if(dir)\
        {\
            dir[0] = 0;\
        }\
        if(fname)\
        {\
            fname[0] = 0;\
        }\
        if(ext)\
        {\
            ext[0] = 0;\
        }\
        goto done;\

/*++
Function:
  _wsplitpath

See MSDN doc.

Notes :
    This implementation ignores drive letters as they should not be
    present. If the drive argument is non-NULL, it always returns an empty
    string.
    File names in which the only period is at the beginning (like .bashrc, but
    not .bashrc.bak), the file is treated as having no extension
    (fname is ".bashrc", ext is "")

--*/
void
__cdecl
_wsplitpath(
            const wchar_16 *dospath,
            wchar_16 *drive,
            wchar_16 *dir,
            wchar_16 *fname,
            wchar_16 *ext)
{
    WCHAR path[_MAX_PATH+1];
    LPCWSTR slash_ptr = NULL;
    LPCWSTR period_ptr = NULL;
    INT size = 0;

    PERF_ENTRY(_wsplitpath);
    ENTRY("_wsplitpath (path=%p (%S), drive=%p, dir=%p, fname=%p, ext=%p)\n",
          dospath?dospath:W16_NULLSTRING,
          dospath?dospath:W16_NULLSTRING, drive, dir, fname, ext);

    /* Do performance intensive error checking only in debug builds.
    
    NOTE: This function must fail predictably across all platforms.
    Under Windows this function throw an access violation if NULL
    was passed in as the value for path.

    */
#if _DEBUG
    if ( !dospath )
    {
        ERROR( "path cannot be NULL!\n" );
    }
#endif
    
    if( lstrlenW( dospath ) >= _MAX_PATH )
    {
        ERROR("Path length is > _MAX_PATH (%d)!\n", _MAX_PATH);
        ON_ERROR;
    }


    PAL_wcscpy(path, dospath);
    FILEDosToUnixPathW(path);

    /* no drive letters in the PAL */
    if( drive != NULL )
    {
        drive[0] = 0;
    }

    /* find last path separator char */
    slash_ptr = PAL_wcsrchr(path, '/');

    if( slash_ptr == NULL )
    {
        TRACE("No path separator in path\n");
        slash_ptr = path - 1;
    }
    /* find extension separator, if any */
    period_ptr = PAL_wcsrchr(path, '.');

    /* make sure we only consider periods after the last path separator */
    if( period_ptr < slash_ptr )
    {
        period_ptr = NULL;
    }

    /* if the only period in the file is a leading period (denoting a hidden
       file), don't treat what follows as an extension */
    if( period_ptr == slash_ptr+1 )
    {
        period_ptr = NULL;
    }

    if( period_ptr == NULL )
    {
        TRACE("No extension in path\n");
        period_ptr = path + lstrlenW(path);
    }

    size = slash_ptr - path + 1;
    if( dir != NULL )
    {
        INT i;

        if( (size + 1 ) > _MAX_DIR )
        {
            ERROR("Directory component needs %d characters, _MAX_DIR is %d\n",
                  size+1, _MAX_DIR);
            ON_ERROR;
        }
        
        memcpy(dir, path, size*sizeof(WCHAR));
        dir[size] = 0;

        /* only allow / separators in returned path */
        i = 0;
        while( dir[ i ] )
        {
            if( dir[ i ] == '\\' )
            {
                dir[i]='/';
            }
            i++;
        }
    }

    size = period_ptr-slash_ptr-1;
    if( fname != NULL )
    {
        if( (size+1) > _MAX_FNAME )
        {
            ERROR("Filename component needs %d characters, _MAX_FNAME is %d\n",
                 size+1, _MAX_FNAME);
            ON_ERROR;
        }
        memcpy(fname, slash_ptr+1, size*sizeof(WCHAR));
        fname[size] = 0;
    }

    size = 1 + lstrlenW( period_ptr );
    if( ext != NULL )
    {
        if( size > _MAX_EXT )
        {
            ERROR("Extension component needs %d characters, _MAX_EXT is %d\n",
                 size, _MAX_EXT);
            ON_ERROR;
        }
        memcpy(ext, period_ptr, size*sizeof(WCHAR));
        ext[size-1] = 0;
    }
    
    TRACE("Path components are '%S' '%S' '%S'\n", dir, fname, ext);

done:
    
    LOGEXIT("_wsplitpath returns.\n");
    PERF_EXIT(_wsplitpath);
}


/*++
Function:
  _splitpath

See description above for _wsplitpath.

--*/
void
__cdecl
_splitpath(
           const char *path,
           char *drive,
           char *dir,
           char *fname,
           char *ext)
{
    WCHAR w_path[_MAX_PATH];
    WCHAR w_dir[_MAX_DIR];
    WCHAR w_fname[_MAX_FNAME];
    WCHAR w_ext[_MAX_EXT];

    PERF_ENTRY(_splitpath);
    ENTRY("_splitpath (path=%p (%s), drive=%p, dir=%p, fname=%p, ext=%p)\n",
          path?path:"NULL",
          path?path:"NULL", drive, dir, fname, ext);

   /* Do performance intensive error checking only in debug builds.
    
    NOTE: This function must fail predictably across all platforms.
    Under Windows this function throw an access violation if NULL
    was passed in as the value for path.
    
    */
#if _DEBUG
    if ( !path )
    {
        ERROR( "path cannot be NULL!\n" );
    }
    
    if( strlen( path ) >= _MAX_PATH )
    {
        ERROR( "Path length is > _MAX_PATH (%d)!\n", _MAX_PATH);
    }
#endif    

    /* no drive letters in the PAL */
    if(drive)
    {
        drive[0] = '\0';
    }

    if(0 == MultiByteToWideChar(CP_ACP, 0, path, -1, w_path, _MAX_PATH))
    {
        ASSERT("MultiByteToWideChar failed!\n");
        ON_ERROR;
    }

    /* Call up to Unicode version; pass NULL for parameters the caller doesn't
       care about */
    _wsplitpath(w_path, NULL, dir?w_dir:NULL, 
	                fname?w_fname:NULL, ext?w_ext:NULL);

    /* Convert result back to MultiByte; report conversion errors but don't
       stop because of them */

    if(dir)
    {
        if(0 == WideCharToMultiByte(CP_ACP, 0, w_dir, -1, dir, _MAX_DIR,
                                NULL, NULL))
        {
            ASSERT("WideCharToMultiByte failed!\n");
            ON_ERROR;
        }
    }
    if(fname)
    {
        if(0 == WideCharToMultiByte(CP_ACP, 0, w_fname, -1, fname, _MAX_FNAME,
                                    NULL, NULL))
        {
            ASSERT("WideCharToMultiByte failed!\n");
            ON_ERROR;
        }
    }
    if(ext)
    {
        if(0 == WideCharToMultiByte(CP_ACP, 0, w_ext, -1, ext, _MAX_EXT,
                                NULL, NULL))
        {
            ASSERT("WideCharToMultiByte failed!\n");
            ON_ERROR;
        }
    }

done:
    LOGEXIT("_splitpath returns.\n");
    PERF_EXIT(_splitpath);
}



/*++
Function:
  _makepath

See MSDN doc.

--*/
void   
__cdecl 
_makepath(
          char *path, 
          const char *drive, 
          const char *dir, 
          const char *fname, 
          const char *ext)
{
    UINT Length = 0;

    PERF_ENTRY(_makepath);
    ENTRY( "_makepath (path=%p, drive=%p (%s), dir=%p (%s), fname=%p (%s), ext=%p (%s))\n", 
           path, drive ? drive:"NULL", drive ? drive:"NULL", dir ? dir:"NULL", dir ? dir:"NULL", fname ? fname:"NULL", fname ? fname:"NULL", 
           ext ? ext:"NULL", 
           ext ? ext:"NULL");

    path[ 0 ] = '\0';

    /* According to the pal documentation, host operating systems that
    don't support drive letters, the "drive" parameter must always be null. */
    if ( drive != NULL  && drive[0] != '\0' )
    {
        ASSERT( "The drive parameter must always be NULL on systems that don't"
              "support drive letters. drive is being ignored!.\n" );
    }

    if ( dir != NULL && dir[ 0 ] != '\0' )
    {
        UINT DirLength = strlen( dir );
        Length += DirLength ;
        
        if ( Length < _MAX_PATH )
        {
            strncat( path, dir, DirLength );
            if ( dir[ DirLength - 1 ] != '/' && dir[ DirLength - 1 ] != '\\' )
            {
                if ( Length + 1 < _MAX_PATH )
                {
                    path[ Length ] = '/';
                    Length++;
                    path[ Length ] = '\0';
                }
                else
                {
                    goto Max_Path_Error;
                }
            }
        }
        else
        {
            goto Max_Path_Error;
        }
    }

    if ( fname != NULL && fname[ 0 ] != '\0' )
    {
        UINT fNameLength = strlen( fname );
        Length += fNameLength;
        
        if ( Length < _MAX_PATH )
        {
            strncat( path, fname, fNameLength );
        }
        else
        {
            goto Max_Path_Error;
        }
    }

    if ( ext != NULL && ext[ 0 ] != '\0' )
    {
        UINT ExtLength = strlen( ext );
        Length += ExtLength;
        
        if ( ext[ 0 ] !=  '.' )
        {
            /* Add a '.' */
            if ( Length + 1 < _MAX_PATH )
            {
                path[ Length - ExtLength ] = '.';
                Length++;
                path[ Length - ExtLength ] = '\0';
                strncat( path, ext, ExtLength );
            }
            else
            {
                goto Max_Path_Error;
            }
        }
        else
        {
            /* Already has a '.' */
            if ( Length < _MAX_PATH )
            {
                strncat( path, ext, ExtLength );    
            }
            else
            {
                goto Max_Path_Error;
            }
        }
    }

    FILEDosToUnixPathA( path );
    LOGEXIT( "_makepath returning void.\n" );
    PERF_EXIT(_makepath);
    return;

Max_Path_Error:

    ERROR( "path cannot be greater then _MAX_PATH\n" ); 
    path[ 0 ] = '\0';
    LOGEXIT( "_makepath returning void \n" );
    PERF_EXIT(_makepath);
    return;
}

/*++
Function:
  _wmakepath

See MSDN doc.

--*/
void   
__cdecl 
_wmakepath(
          wchar_16 *path, 
          const wchar_16 *drive, 
          const wchar_16 *dir, 
          const wchar_16 *fname, 
          const wchar_16 *ext)
{
    CHAR Dir[ _MAX_DIR ]={0};
    CHAR FileName[ _MAX_FNAME ]={0};
    CHAR Ext[ _MAX_EXT ]={0};
    CHAR Path[ _MAX_PATH ]={0};
    
    PERF_ENTRY(_wmakepath);
    ENTRY("_wmakepath (path=%p, drive=%p (%S), dir=%p (%S), fname=%p (%S), ext=%p (%S))\n",
          path, drive ? drive:W16_NULLSTRING, drive ? drive:W16_NULLSTRING, dir ? dir:W16_NULLSTRING, dir ? dir:W16_NULLSTRING,
          fname ? fname:W16_NULLSTRING,
          fname ? fname:W16_NULLSTRING, ext ? ext:W16_NULLSTRING, ext ? ext:W16_NULLSTRING);

    /* According to the pal documentation, host operating systems that
    don't support drive letters, the "drive" parameter must always be null. */
    if ( drive != NULL  && drive[0] != '\0' )
    {
        ASSERT( "The drive parameter must always be NULL on systems that don't"
              "support drive letters. drive is being ignored!.\n" );
    }

    if ((dir != NULL) &&  WideCharToMultiByte( CP_ACP, 0, dir, -1, Dir, 
                                               _MAX_DIR, NULL, NULL ) == 0 )
    {
        ASSERT( "An error occurred while converting dir to multibyte."
               "Possible error: Length of dir is greater than _MAX_DIR.\n" );
        goto error;
    }

    if ((fname != NULL) && WideCharToMultiByte( CP_ACP, 0, fname, -1, FileName,
                                                _MAX_FNAME, NULL, NULL ) == 0 )
    {
        ASSERT( "An error occurred while converting fname to multibyte."
               "Possible error: Length of fname is greater than _MAX_FNAME.\n" );
        goto error;
    }

    if ((ext != NULL) && WideCharToMultiByte( CP_ACP, 0, ext, -1, Ext,
                                              _MAX_EXT, NULL, NULL ) == 0 )
    {
        ASSERT( "An error occurred while converting ext to multibyte."
               "Possible error: Length of ext is greater than _MAX_EXT.\n" );
        goto error;
    }

    /* Call up to the ANSI _makepath. */
    _makepath_s( Path, sizeof(Path), NULL, Dir, FileName, Ext );

    if ( MultiByteToWideChar( CP_ACP, 0, Path, -1, path, _MAX_PATH ) == 0 )
    {
        ASSERT( "An error occurred while converting the back wide char."
               "Possible error: The length of combined path is greater "
               "than _MAX_PATH.\n" );
        goto error;
    }

    LOGEXIT("_wmakepath returns void\n");
    PERF_EXIT(_wmakepath);
    return;

error:
    *path = '\0';
    LOGEXIT("_wmakepath returns void\n");
    PERF_EXIT(_wmakepath);
}


/*++
Function:
  _fullpath

See MSDN doc.

--*/
char *   
__cdecl 
_fullpath(
          char *absPath, 
          const char *relPath, 
          size_t maxLength)
{
    char realpath_buf[PATH_MAX+1];
    char path_copy[PATH_MAX+1];
    char *retval = NULL;
    DWORD cPathCopy = sizeof(path_copy)/sizeof(path_copy[0]);
    size_t min_length;
    BOOL fBufAllocated = FALSE;

    PERF_ENTRY(_fullpath);
    ENTRY("_fullpath (absPath=%p, relPath=%p (%s), maxLength = %lu)\n",
          absPath, relPath ? relPath:"NULL", relPath ? relPath:"NULL", maxLength);
    
    if (strncpy_s(path_copy, sizeof(path_copy), relPath ? relPath : ".", cPathCopy) != SAFECRT_SUCCESS)
    {
        TRACE("_fullpath: strncpy_s failed!\n");
        goto fullpathExit;
    }

    FILEDosToUnixPathA(path_copy);

    if(NULL == realpath(path_copy, realpath_buf))
    {
        ERROR("realpath() failed; problem path is '%s'. errno is %d (%s)\n",
                realpath_buf, errno, strerror(errno));
        goto fullpathExit;
    }   

    TRACE("real path is %s\n", realpath_buf);
    min_length = strlen(realpath_buf)+1; // +1 for the NULL terminator

    if(NULL == absPath)
    {
        absPath = static_cast<char *>(
            PAL_malloc(_MAX_PATH * sizeof(char)));
        if (!absPath)
        {
            ERROR("PAL_malloc failed with error %d\n", errno);
            goto fullpathExit;
        }
        maxLength = _MAX_PATH;
        fBufAllocated = TRUE;
    }

    if(min_length > maxLength)
    {
        ERROR("maxLength is %lu, we need at least %lu\n",
                maxLength, min_length);
        if (fBufAllocated)
        {
            PAL_free(absPath);
            fBufAllocated = FALSE;
        }
        goto fullpathExit;
    }

    strcpy_s(absPath, maxLength, realpath_buf);
    retval = absPath;
    
fullpathExit:
    LOGEXIT("_fullpath returns char * %p\n", retval);
    PERF_EXIT(_fullpath);
    return retval;
}



