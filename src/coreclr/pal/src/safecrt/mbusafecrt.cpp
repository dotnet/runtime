// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*   mbusafecrt.c - implementation of support functions and data for MBUSafeCRT
*
*   Purpose:
*       This file contains the implementation of support functions and
*       data for MBUSafeCRT declared in mbusafecrt.h and mbusafecrt_internal.h.
****/

#include "pal/palinternal.h"
#include <string.h>
#include <errno.h>
#include <limits.h>

#include "mbusafecrt_internal.h"

/***
*   _getc_nolock - getc for the miniFILE stream.
****/

int _getc_nolock( miniFILE* inStream )
{
    int returnValue = EOF;

    if ( ( inStream->_cnt ) >= ( int )( sizeof( char ) ) )
    {
        inStream->_cnt -= sizeof( char );
        returnValue = ( int )( *( inStream->_ptr ) );
        inStream->_ptr += sizeof( char );
    }

    return returnValue;
}

/***
*   _getwc_nolock - getc for the miniFILE stream.
****/

int _getwc_nolock( miniFILE* inStream )
{
    int returnValue = EOF;

    if ( ( inStream->_cnt ) >= ( int )( sizeof( char16_t ) ) )
    {
        inStream->_cnt -= sizeof( char16_t );
        returnValue = ( int )( *( ( char16_t* )( inStream->_ptr ) ) );
        inStream->_ptr += sizeof( char16_t );
    }

    return returnValue;
}

/***
*   _ungetc_nolock - ungetc for the miniFILE stream.
****/

int _ungetc_nolock( char inChar, miniFILE* inStream )
{
    int returnValue = EOF;

    if ( ( size_t )( ( inStream->_ptr ) - ( inStream->_base ) ) >= ( sizeof( char ) ) )
    {
        inStream->_cnt += sizeof( char );
        inStream->_ptr -= sizeof( char );
        return ( int )inChar;
    }

    return returnValue;
}

/***
*   _ungetwc_nolock - ungetwc for the miniFILE stream.
****/

int _ungetwc_nolock( char16_t inChar, miniFILE* inStream )
{
    int returnValue = WEOF;

    if ( ( size_t )( ( inStream->_ptr ) - ( inStream->_base ) ) >= ( sizeof( char16_t ) ) )
    {
        inStream->_cnt += sizeof( char16_t );
        inStream->_ptr -= sizeof( char16_t );
        returnValue = ( unsigned short )inChar;
    }

    return returnValue;
}


/***
*   _safecrt_fassign - convert a string into a float or double.
****/

void _safecrt_fassign(int flag, void* argument, char* number )
{
    if ( flag != 0 )    // double
    {
        double dblValue = strtod(number, NULL);
        *( ( double* )argument ) = dblValue;
    }
    else                // float
    {
        float fltValue = strtof(number, NULL);
        *( ( float* )argument ) = fltValue;
    }
}

/***
*   _minimal_chartowchar - do a simple char to wchar conversion.
****/

int _minimal_chartowchar( char16_t* outWChar, const char* inChar )
{
    *outWChar = ( char16_t )( ( unsigned short )( ( unsigned char )( *inChar ) ) );
    return 1;
}
