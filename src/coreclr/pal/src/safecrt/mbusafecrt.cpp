// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*   mbusafecrt.c - implementation of support functions and data for MBUSafeCRT
*

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

/* global data */
tSafeCRT_AssertFuncPtr sMBUSafeCRTAssertFunc = NULL;

/***
*   MBUSafeCRTSetAssertFunc - Set the function called when an assert fails.
****/

void MBUSafeCRTSetAssertFunc( tSafeCRT_AssertFuncPtr inAssertFuncPtr )
{
    /* set it */
    sMBUSafeCRTAssertFunc = inAssertFuncPtr;
}

/***
*   _putc_nolock - putc for the miniFILE stream.
****/

int _putc_nolock( char inChar, miniFILE* inStream )
{
    int returnValue = EOF;

        inStream->_cnt -= sizeof( char );

    if ( ( inStream->_cnt ) >= 0 )
    {
        *( inStream->_ptr ) = inChar;
        inStream->_ptr += sizeof( char );
        returnValue = ( int )inChar;
    }

    return returnValue;
}

/***
*   _putwc_nolock - putwc for the miniFILE stream.
****/

int _putwc_nolock( char16_t inChar, miniFILE* inStream )
{
    int returnValue = WEOF;

        inStream->_cnt -= sizeof( char16_t );

    if ( ( inStream->_cnt ) >= 0 )
    {
        *( ( char16_t* )( inStream->_ptr ) ) = inChar;
        inStream->_ptr += sizeof( char16_t );
        returnValue = ( int )inChar;
    }

    return returnValue;
}

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
*   _safecrt_cfltcvt - convert a float to an ascii string.
****/

/* routine used for floating-point output */
#define FORMATSIZE 30

// taken from output.inl
#define FL_ALTERNATE  0x00080   /* alternate form requested */

errno_t _safecrt_cfltcvt(double *arg, char *buffer, size_t sizeInBytes, int type, int precision, int flags)
{
    char format[FORMATSIZE];
    size_t formatlen = 0;
    int retvalue;

    if (flags & 1)
    {
        type -= 'a' - 'A';
    }
    formatlen = 0;
    format[formatlen++] = '%';
    if (flags & FL_ALTERNATE)
    {
        format[formatlen++] = '#';
    }
    format[formatlen++] = '.';
    _itoa_s(precision, format + formatlen, FORMATSIZE - formatlen, 10);
    formatlen = strlen(format);
    format[formatlen++] = (char)type;
    format[formatlen] = 0;

    buffer[sizeInBytes - 1] = 0;
    retvalue = snprintf(buffer, sizeInBytes, format, *arg);
    if (buffer[sizeInBytes - 1] != 0 || retvalue <= 0)
    {
        buffer[0] = 0;
        return EINVAL;
    }
    return 0;
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
*   _safecrt_wfassign - convert a char16_t string into a float or double.
****/

void _safecrt_wfassign(int flag, void* argument, char16_t* number )
{
    // We cannot use system functions for this - they
    // assume that char16_t is four bytes, while we assume
    // two. So, we need to convert to a regular char string
    // without using any system functions. To do this,
    // we'll assume that the numbers are in the 0-9 range and
    // do a simple conversion.

    char* numberAsChars = ( char* )number;
    int position = 0;

    // do the convert
    while ( number[ position ] != 0 )
    {
        numberAsChars[ position ] = ( char )( number[ position ] & 0x00FF );
        position++;
    }
    numberAsChars[ position ] = ( char )( number[ position ] & 0x00FF );

    // call the normal char version
    _safecrt_fassign( flag, argument, numberAsChars );
}


/***
*   _minimal_chartowchar - do a simple char to wchar conversion.
****/

int _minimal_chartowchar( char16_t* outWChar, const char* inChar )
{
    *outWChar = ( char16_t )( ( unsigned short )( ( unsigned char )( *inChar ) ) );
    return 1;
}


