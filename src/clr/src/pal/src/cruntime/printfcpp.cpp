//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    printfcpp.cpp

Abstract:

    Implementation of suspension safe printf functions.

Revision History:



--*/

#include "pal/corunix.hpp"
#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/file.hpp"
#include "pal/printfcpp.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/cruntime.h"

#include <errno.h>

SET_DEFAULT_DEBUG_CHANNEL(CRT);

using namespace CorUnix;

int CoreWvsnprintf(CPalThread *pthrCurrent, LPWSTR Buffer, size_t Count, LPCWSTR Format, va_list ap);
int CoreVsnprintf(CPalThread *pthrCurrent, LPSTR Buffer, size_t Count, LPCSTR Format, va_list ap);
int CoreVfprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const char *format, va_list ap);
int CoreVfwprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const wchar_16 *format, va_list ap);

extern "C"
{

/*******************************************************************************
Function:
  Internal_Convertfwrite
  This function is a wrapper around fwrite for cases where the buffer has
  to be converted from WideChar to MultiByte
*******************************************************************************/

static int Internal_Convertfwrite(CPalThread *pthrCurrent, const void *buffer, size_t size, size_t count, FILE *stream, BOOL convert)
{
    int ret;
    int iError = 0;

#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
    clearerr (stream);
#endif

    if(convert)
    {
        int nsize;
        LPSTR newBuff = 0;
        nsize = WideCharToMultiByte(CP_ACP, 0,(LPCWSTR)buffer, count, 0, 0, 0, 0);
        if (!nsize)
        {
            ASSERT("WideCharToMultiByte failed.  Error is %d\n", GetLastError());
            return -1;
        }
        newBuff = (LPSTR) InternalMalloc(pthrCurrent, nsize);
        if (!newBuff)
        {
            ERROR("InternalMalloc failed\n");
            pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            return -1;
        }
        nsize = WideCharToMultiByte(CP_ACP, 0, (LPCWSTR)buffer, count, newBuff, nsize, 0, 0);
        if (!nsize)
        {
            ASSERT("WideCharToMultiByte failed.  Error is %d\n", GetLastError());
            InternalFree(pthrCurrent, newBuff);
            return -1;
        }
        ret = InternalFwrite(pthrCurrent, newBuff, 1, count, stream, &iError);
        if (iError != 0)
        {
            ERROR("InternalFwrite did not write the whole buffer. Error is %d\n", iError);
            InternalFree(pthrCurrent, newBuff);
            return -1;
        }
        InternalFree(pthrCurrent, newBuff);
   }
   else
   {
        ret = InternalFwrite(pthrCurrent, buffer, size, count, stream, &iError);
        if (iError != 0)
        {
            ERROR("InternalFwrite did not write the whole buffer. Error is %d\n", iError);
            return -1;
        }
   }
   return ret;

}    
    
/*******************************************************************************
Function:
  Internal_ExtractFormatA

Paramaters:
  Fmt
    - format string to parse
    - first character must be a '%'
    - paramater gets updated to point to the character after
      the %<foo> format string
  Out
    - buffer will contain the %<foo> format string
  Flags
    - paramater will be set with the PRINTF_FORMAT_FLAGS defined above
  Width
    - will contain the width specified by the format string
    - -1 if none given
  Precision
    - will contain the precision specified in the format string
    - -1 if none given
  Prefix
    - an enumeration of the type prefix
  Type
    - an enumeration of the type value

Notes:
  - I'm also handling the undocumented %ws, %wc, %w...
  - %#10x, when we have a width greater than the length (i.e padding) the
    length of the padding is not consistent with MS's wsprintf
    (MS adds an extra 2 padding chars, length of "0x")
  - MS's wsprintf seems to ingore a 'h' prefix for number types
  - MS's "%p" is different than gcc's
    e.g. printf("%p", NULL);
        MS  -->  00000000
        gcc -->  0x0
  - the length of the exponent (precision) for floating types is different
    between MS and gcc
    e.g. printf("%E", 256.0);
        MS  -->  2.560000E+002
        gcc -->  2.560000E+02
*******************************************************************************/
BOOL Internal_ExtractFormatA(CPalThread *pthrCurrent, LPCSTR *Fmt, LPSTR Out, LPINT Flags,
    LPINT Width, LPINT Precision, LPINT Prefix, LPINT Type)
{
    BOOL Result = FALSE;
    LPSTR TempStr;
    LPSTR TempStrPtr;

    *Width = WIDTH_DEFAULT;
    *Precision = PRECISION_DEFAULT;
    *Flags = PFF_NONE;
    *Prefix = PFF_PREFIX_DEFAULT;
    *Type = PFF_TYPE_DEFAULT;

    if (*Fmt && **Fmt == '%')
    {
        *Out++ = *(*Fmt)++;
    }
    else
    {
        return Result;
    }

    /* we'll never need a temp string longer than the original */
    TempStrPtr = TempStr = (LPSTR) InternalMalloc(pthrCurrent, strlen(*Fmt)+1);
    if (!TempStr)
    {
        ERROR("InternalMalloc failed\n");
        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return Result;
    }

    /* parse flags */
    while (**Fmt && (**Fmt == '-' || **Fmt == '+' ||
           **Fmt == '0' || **Fmt == ' ' || **Fmt == '#'))
    {
        switch (**Fmt)
        {
        case '-':
            *Flags |= PFF_MINUS; break;
        case '+':
            *Flags |= PFF_PLUS; break;
        case '0':
            *Flags |= PFF_ZERO; break;
        case ' ':
            *Flags |= PFF_SPACE; break;
        case '#':
            *Flags |= PFF_POUND; break;
        }
            *Out++ = *(*Fmt)++;
    }
    /* '-' flag negates '0' flag */
    if ((*Flags & PFF_MINUS) && (*Flags & PFF_ZERO))
    {
        *Flags -= PFF_ZERO;
    }

    /* grab width specifier */
    if (isdigit((unsigned char) **Fmt))
    {
        TempStrPtr = TempStr;
        while (isdigit((unsigned char) **Fmt))
        {
            *TempStrPtr++ = **Fmt;
            *Out++ = *(*Fmt)++;
        }
        *TempStrPtr = 0; /* end string */
        *Width = atoi(TempStr);
        if (*Width < 0)
        {
            ERROR("atoi returned a negative value indicative of an overflow.\n");
            pthrCurrent->SetLastError(ERROR_INTERNAL_ERROR);
            return Result;
        }
    }
    else if (**Fmt == '*')
    {
        *Width = WIDTH_STAR;
        *Out++ = *(*Fmt)++;
        if (isdigit((unsigned char) **Fmt))
        {
            /* this is an invalid width because we have a * then a number */
            /* printf handles this by just printing the whole string */
            *Width = WIDTH_INVALID;
            while (isdigit((unsigned char) **Fmt))
            {
               *Out++ = *(*Fmt)++;
            }
        }
    }


    /* grab precision specifier */
    if (**Fmt == '.')
    {
        *Out++ = *(*Fmt)++;
        if (isdigit((unsigned char) **Fmt))
        {
            TempStrPtr = TempStr;
            while (isdigit((unsigned char) **Fmt))
            {
                *TempStrPtr++ = **Fmt;
                *Out++ = *(*Fmt)++;
            }
            *TempStrPtr = 0; /* end string */
            *Precision = atoi(TempStr);
            if (*Precision < 0)
            {
                ERROR("atoi returned a negative value indicative of an overflow.\n");
                pthrCurrent->SetLastError(ERROR_INTERNAL_ERROR);
                return Result;
            }
        }
        else if (**Fmt == '*')
        {
            *Precision = PRECISION_STAR;
            *Out++ = *(*Fmt)++;
            if (isdigit((unsigned char) **Fmt))
            {
                /* this is an invalid precision because we have a .* then a number */
                /* printf handles this by just printing the whole string */
                *Precision = PRECISION_INVALID;
                while (isdigit((unsigned char) **Fmt))
                {
                    *Out++ = *(*Fmt)++;
                }
            }
        }
        else
        {
            *Precision = PRECISION_DOT;
        }
    }

#ifdef BIT64
    if (**Fmt == 'p')
    {
        *Prefix = PFF_PREFIX_LONGLONG;
    }
#endif
    if ((*Fmt)[0] == 'I')
    {
        /* grab prefix of 'I64' for __int64 */
        if ((*Fmt)[1] == '6' && (*Fmt)[2] == '4')
        {
            /* convert to 'll' so that Unix snprintf can handle it */
            *Fmt += 3;
            *Prefix = PFF_PREFIX_LONGLONG;
        }
        /* grab prefix of 'I32' for __int32 */
        else if ((*Fmt)[1] == '3' && (*Fmt)[2] == '2')
        {
            *Fmt += 3;
        }
        else
        {
            ++(*Fmt);
    #ifdef BIT64
            /* convert to 'll' so that Unix snprintf can handle it */
            *Prefix = PFF_PREFIX_LONGLONG;
    #endif
        }
    }
    /* grab a prefix of 'h' */
    else if (**Fmt == 'h')
    {
        *Prefix = PFF_PREFIX_SHORT;
        ++(*Fmt);
    }
    /* grab prefix of 'l' or the undocumented 'w' (at least in MSDN) */
    else if (**Fmt == 'l' || **Fmt == 'w')
    {
        ++(*Fmt);
#ifdef BIT64
        // Only want to change the prefix on 64 bit when printing characters.
        if (**Fmt == 'c' || **Fmt == 's')
#endif       
        {
            *Prefix = PFF_PREFIX_LONG;
        }
        else if (**Fmt == 'l')
        {
            *Prefix = PFF_PREFIX_LONGLONG;   
            ++(*Fmt);
        }
    }
    else if (**Fmt == 'L')
    {
        /* a prefix of 'L' seems to be ignored */
        ++(*Fmt);
    }

    /* grab type 'c' */
    if (**Fmt == 'c' || **Fmt == 'C')
    {
        *Type = PFF_TYPE_CHAR;
        if (*Prefix != PFF_PREFIX_SHORT && **Fmt == 'C')
        {
            *Prefix = PFF_PREFIX_LONG; /* give it a wide prefix */
        }
        if (*Prefix == PFF_PREFIX_LONG)
        {
            *Out++ = 'l';
        }
        *Out++ = 'c';
        ++(*Fmt);
        Result = TRUE;
    }
    /* grab type 's' */
    else if (**Fmt == 's' || **Fmt == 'S')
    {
        *Type = PFF_TYPE_STRING;
        if (*Prefix != PFF_PREFIX_SHORT && **Fmt == 'S')
        {
            *Prefix = PFF_PREFIX_LONG; /* give it a wide prefix */
        }
        if (*Prefix == PFF_PREFIX_LONG)
        {
            *Out++ = 'l';
        }
        *Out++ = 's';
        ++(*Fmt);
        Result = TRUE;
    }
    /* grab int types */
    else if (**Fmt == 'd' || **Fmt == 'i' || **Fmt == 'o' ||
             **Fmt == 'u' || **Fmt == 'x' || **Fmt == 'X')
    {
        *Type = PFF_TYPE_INT;
        if (*Prefix == PFF_PREFIX_SHORT)
        {
            *Out++ = 'h';
        }
        else if (*Prefix == PFF_PREFIX_LONG)
        {
            *Out++ = 'l';
        }
        else if (*Prefix == PFF_PREFIX_LONGLONG)
        {
            *Out++ = 'l';
            *Out++ = 'l';
        }
        *Out++ = *(*Fmt)++;
        Result = TRUE;
    }
    else if (**Fmt == 'e' || **Fmt == 'E' || **Fmt == 'f' ||
             **Fmt == 'g' || **Fmt == 'G')
    {
        /* we can safely ignore the prefixes and only add the type*/
        *Type = PFF_TYPE_FLOAT;
        *Out++ = *(*Fmt)++;
        Result = TRUE;
    }
    else if (**Fmt == 'n')
    {
        if (*Prefix == PFF_PREFIX_SHORT)
        {
            *Out++ = 'h';
        }
        *Out++ = *(*Fmt)++;
        *Type = PFF_TYPE_N;
        Result = TRUE;
    }
    else if (**Fmt == 'p')
    {
        *Type = PFF_TYPE_P;
        (*Fmt)++;

        if (*Prefix == PFF_PREFIX_LONGLONG)
        {
            if (*Precision == PRECISION_DEFAULT)
            {
                *Precision = 16;
                *Out++ = '.';
                *Out++ = '1';
                *Out++ = '6';
            }
            /* native *printf does not support %I64p
               (actually %llp), so we need to cheat a little bit */
            *Out++ = 'l';
            *Out++ = 'l';
        }
        else
        {
            if (*Precision == PRECISION_DEFAULT)
            {
                *Precision = 8;
                *Out++ = '.';
                *Out++ = '8';
            }
        }
        *Out++ = 'X';
        Result = TRUE;
    }

    *Out = 0;  /* end the string */
    InternalFree(pthrCurrent, TempStr);
    return Result;
}

/*******************************************************************************
Function:
  Internal_ExtractFormatW

  -- see Internal_ExtractFormatA above
*******************************************************************************/
BOOL Internal_ExtractFormatW(CPalThread *pthrCurrent, LPCWSTR *Fmt, LPSTR Out, LPINT Flags,
    LPINT Width, LPINT Precision, LPINT Prefix, LPINT Type)
{
    BOOL Result = FALSE;
    LPSTR TempStr;
    LPSTR TempStrPtr;

    *Width = WIDTH_DEFAULT;
    *Precision = PRECISION_DEFAULT;
    *Flags = PFF_NONE;
    *Prefix = PFF_PREFIX_DEFAULT;
    *Type = PFF_TYPE_DEFAULT;

    if (*Fmt && **Fmt == '%')
    {
        *Out++ = (CHAR) *(*Fmt)++;
    }
    else
    {
        return Result;
    }

    /* we'll never need a temp string longer than the original */
    TempStrPtr = TempStr = (LPSTR) InternalMalloc(pthrCurrent, PAL_wcslen(*Fmt)+1);
    if (!TempStr)
    {
        ERROR("InternalMalloc failed\n");
        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return Result;
    }

    /* parse flags */
    while (**Fmt && (**Fmt == '-' || **Fmt == '+' ||
           **Fmt == '0' || **Fmt == ' ' || **Fmt == '#'))
    {
        switch (**Fmt)
        {
            case '-':
                *Flags |= PFF_MINUS; break;
            case '+':
                *Flags |= PFF_PLUS; break;
            case '0':
                *Flags |= PFF_ZERO; break;
            case ' ':
                *Flags |= PFF_SPACE; break;
            case '#':
                *Flags |= PFF_POUND; break;
        }
            *Out++ = (CHAR) *(*Fmt)++;
    }
    /* '-' flag negates '0' flag */
    if ((*Flags & PFF_MINUS) && (*Flags & PFF_ZERO))
    {
        *Flags -= PFF_ZERO;
    }

    /* grab width specifier */
    if (isdigit(**Fmt))
    {
        TempStrPtr = TempStr;
        while (isdigit(**Fmt))
        {
            *TempStrPtr++ = (CHAR) **Fmt;
            *Out++ = (CHAR) *(*Fmt)++;
        }
        *TempStrPtr = 0; /* end string */
        *Width = atoi(TempStr);
        if (*Width < 0)
        {
            ERROR("atoi returned a negative value indicative of an overflow.\n");
            pthrCurrent->SetLastError(ERROR_INTERNAL_ERROR);
            return Result;
        }
    }
    else if (**Fmt == '*')
    {
        *Width = WIDTH_STAR;
        *Out++ = (CHAR) *(*Fmt)++;
        if (isdigit(**Fmt))
        {
            /* this is an invalid width because we have a * then a number */
            /* printf handles this by just printing the whole string */
            *Width = WIDTH_INVALID;
            while (isdigit(**Fmt))
            {
                *Out++ = (CHAR) *(*Fmt)++;
            }
        }
    }

    /* grab precision specifier */
    if (**Fmt == '.')
    {
        *Out++ = (CHAR) *(*Fmt)++;
        if (isdigit(**Fmt))
        {
            TempStrPtr = TempStr;
            while (isdigit(**Fmt))
            {
                *TempStrPtr++ = (CHAR) **Fmt;
                *Out++ = (CHAR) *(*Fmt)++;
            }
            *TempStrPtr = 0; /* end string */
            *Precision = atoi(TempStr);
            if (*Precision < 0)
            {
                ERROR("atoi returned a negative value indicative of an overflow.\n");
                pthrCurrent->SetLastError(ERROR_INTERNAL_ERROR);
                return Result;
            }
        }
        else if (**Fmt == '*')
        {
            *Precision = PRECISION_STAR;
            *Out++ = (CHAR) *(*Fmt)++;
            if (isdigit(**Fmt))
            {
                /* this is an invalid precision because we have a .* then a number */
                /* printf handles this by just printing the whole string */
                *Precision = PRECISION_INVALID;
                while (isdigit(**Fmt))
                {
                    *Out++ = (CHAR) *(*Fmt)++;
                }
            }
        }
        else
        {
            *Precision = PRECISION_DOT;
        }
    }

#ifdef BIT64
    if (**Fmt == 'p')
    {
        *Prefix = PFF_PREFIX_LONGLONG;
    }
#endif
    /* grab prefix of 'I64' for __int64 */
    if ((*Fmt)[0] == 'I' && (*Fmt)[1] == '6' && (*Fmt)[2] == '4')
    {
        /* convert to 'll' so BSD's snprintf can handle it */
        *Fmt += 3;
        *Prefix = PFF_PREFIX_LONGLONG;
    }
    /* grab a prefix of 'h' */
    else if (**Fmt == 'h')
    {
        *Prefix = PFF_PREFIX_SHORT;
        ++(*Fmt);
    }
    else if (**Fmt == 'l' || **Fmt == 'w')
    {
        ++(*Fmt);
 #ifdef BIT64
        // Only want to change the prefix on 64 bit when printing characters.
        if (**Fmt == 'C' || **Fmt == 'S')
#endif   
        {
            *Prefix = PFF_PREFIX_LONG_W;
        }
    }
    else if (**Fmt == 'L')
    {
        /* a prefix of 'L' seems to be ignored */
        ++(*Fmt);
    }


    /* grab type 'c' */
    if (**Fmt == 'c' || **Fmt == 'C')
    {
        *Type = PFF_TYPE_CHAR;
        if (*Prefix != PFF_PREFIX_SHORT && **Fmt == 'c')
        {
            *Prefix = PFF_PREFIX_LONG; /* give it a wide prefix */
        }
        if (*Prefix == PFF_PREFIX_LONG || *Prefix == PFF_PREFIX_LONG_W)
        {
            *Out++ = 'l';
            *Prefix = PFF_PREFIX_LONG;
        }
        *Out++ = 'c';
        ++(*Fmt);
        Result = TRUE;
    }
    /* grab type 's' */
    else if (**Fmt == 's' || **Fmt == 'S' )
    {
        if ( **Fmt == 'S' )
        {
           *Type = PFF_TYPE_WSTRING;
        }
        else
        {
            *Type = PFF_TYPE_STRING;
        }
        if (*Prefix != PFF_PREFIX_SHORT && **Fmt == 's')
        {
            *Prefix = PFF_PREFIX_LONG; /* give it a wide prefix */
        }
        if (*Prefix == PFF_PREFIX_LONG)
        {
            *Out++ = 'l';
        }

        *Out++ = 's';
        ++(*Fmt);
        Result = TRUE;
    }
    /* grab int types */
    else if (**Fmt == 'd' || **Fmt == 'i' || **Fmt == 'o' ||
             **Fmt == 'u' || **Fmt == 'x' || **Fmt == 'X')
    {
        *Type = PFF_TYPE_INT;
        if (*Prefix == PFF_PREFIX_SHORT)
        {
            *Out++ = 'h';
        }
        else if (*Prefix == PFF_PREFIX_LONG || *Prefix == PFF_PREFIX_LONG_W)
        {
            *Out++ = 'l';
            *Prefix = PFF_PREFIX_LONG;
        }
        else if (*Prefix == PFF_PREFIX_LONGLONG)
        {
            *Out++ = 'l';
            *Out++ = 'l';
        }
        *Out++ = *(*Fmt)++;
        Result = TRUE;
    }
    else if (**Fmt == 'e' || **Fmt == 'E' || **Fmt == 'f' ||
             **Fmt == 'g' || **Fmt == 'G')
    {
        /* we can safely ignore the prefixes and only add the type*/
        if (*Prefix == PFF_PREFIX_LONG_W)
        {
            *Prefix = PFF_PREFIX_LONG;
        }

        *Type = PFF_TYPE_FLOAT;
        *Out++ = *(*Fmt)++;
        Result = TRUE;
    }
    else if (**Fmt == 'n')
    {
        if (*Prefix == PFF_PREFIX_LONG_W)
        {
            *Prefix = PFF_PREFIX_LONG;
        }

        if (*Prefix == PFF_PREFIX_SHORT)
        {
            *Out++ = 'h';
        }
        *Out++ = *(*Fmt)++;
        *Type = PFF_TYPE_N;
        Result = TRUE;
    }
    else if (**Fmt == 'p')
    {
        *Type = PFF_TYPE_P;
        (*Fmt)++;

        if (*Prefix == PFF_PREFIX_LONGLONG)
        {
            if (*Precision == PRECISION_DEFAULT)
            {
                *Precision = 16;
                *Out++ = '.';
                *Out++ = '1';
                *Out++ = '6';
            }
            /* native *printf does not support %I64p
               (actually %llp), so we need to cheat a little bit */
            *Out++ = 'l';
            *Out++ = 'l';
        }
        else
        {
            if (*Precision == PRECISION_DEFAULT)
            {
                *Precision = 8;
                *Out++ = '.';
                *Out++ = '8';
            }
            if (*Prefix == PFF_PREFIX_LONG_W)
            {
                *Prefix = PFF_PREFIX_LONG;
            }
        }
        *Out++ = 'X';
        Result = TRUE;
    }

    *Out = 0;  /* end the string */
    InternalFree(pthrCurrent, TempStr);
    return Result;
}

/*******************************************************************************
Function:
  Internal_AddPaddingW

Parameters:
  Out
    - buffer to place padding and given string (In)
  Count
    - maximum chars to be copied so as not to overrun given buffer
  In
    - string to place into (Out) accompanied with padding
  Padding
    - number of padding chars to add
  Flags
    - padding style flags (PRINTF_FORMAT_FLAGS)
*******************************************************************************/

BOOL Internal_AddPaddingW(LPWSTR *Out, INT Count, LPWSTR In, INT Padding, INT Flags)
{
    LPWSTR OutOriginal = *Out;
    INT PaddingOriginal = Padding;
    INT LengthInStr;
    LengthInStr = PAL_wcslen(In);
    

    if (Padding < 0)
    {
        /* this is used at the bottom to determine if the buffer ran out */
        PaddingOriginal = 0;
    }
    if (Flags & PFF_MINUS) /* pad on right */
    {
        if (wcsncpy_s(*Out, Count, In, min(LengthInStr + 1, Count - 1)) != SAFECRT_SUCCESS)
        {
            return FALSE;
        }

        *Out += min(LengthInStr, Count - 1);
    }
    if (Padding > 0)
    {
        if (Flags & PFF_ZERO) /* '0', pad with zeros */
        {
            while (Padding-- && Count > *Out - OutOriginal)
            {
                *(*Out)++ = '0';
            }
        }
        else /* pad left with spaces */
        {
            while (Padding-- && Count > *Out - OutOriginal)
            {
                *(*Out)++ = ' ';
            }
        }
    }
    if (!(Flags & PFF_MINUS)) /* put 'In' after padding */
    {
        if (wcsncpy_s(*Out, Count - (*Out - OutOriginal), In,
            min(LengthInStr, Count - (*Out - OutOriginal) - 1)) != SAFECRT_SUCCESS)
        {
            return FALSE;
        }

        *Out += min(LengthInStr, Count - (*Out - OutOriginal) - 1);
    }

    if (LengthInStr + PaddingOriginal > Count - 1)
    {
        return FALSE;
    }
    else
    {
        return TRUE;
    }
}

/*******************************************************************************
Function:
  Internal_AddPaddingVfprintf

Parameters:
  stream
    - file stream to place padding and given string (In)
  In
    - string to place into (Out) accompanied with padding
  Padding
    - number of padding chars to add
  Flags
    - padding style flags (PRINTF_FORMAT_FLAGS)
*******************************************************************************/

INT Internal_AddPaddingVfprintf(CPalThread *pthrCurrent, PAL_FILE *stream, LPSTR In,
                                       INT Padding, INT Flags)
{
    LPSTR Out;
    INT LengthInStr;
    INT Length;
    LPSTR OutOriginal;
    INT Written;

    LengthInStr = strlen(In);
    Length = LengthInStr;

    if (Padding > 0)
    {
        Length += Padding;
    }
    Out = (LPSTR) InternalMalloc(pthrCurrent, Length+1);
    int iLength = Length+1;
    if (!Out)
    {
        ERROR("InternalMalloc failed\n");
        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return -1;
    }
    OutOriginal = Out;

    if (Flags & PFF_MINUS) /* pad on right */
    {
        if (strcpy_s(Out, iLength, In) != SAFECRT_SUCCESS)
        {
            ERROR("strcpy_s failed\n");
            pthrCurrent->SetLastError(ERROR_INSUFFICIENT_BUFFER);
            Written = -1;
            goto Done;
        }

        Out += LengthInStr;
        iLength -= LengthInStr;
    }
    if (Padding > 0)
    {
        iLength -= Padding;
        if (Flags & PFF_ZERO) /* '0', pad with zeros */
        {
            while (Padding--)
            {
                *Out++ = '0';
            }
        }
        else /* pad with spaces */
        {
            while (Padding--)
            {
                *Out++ = ' ';
            }
        }
    }
    if (!(Flags & PFF_MINUS)) /* put 'In' after padding */
    {
        if (strcpy_s(Out, iLength, In) != SAFECRT_SUCCESS)
        {
            ERROR("strcpy_s failed\n");
            pthrCurrent->SetLastError(ERROR_INSUFFICIENT_BUFFER);
            Written = -1;
            goto Done;
        }

        Out += LengthInStr;
        iLength -= LengthInStr;
    }

#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
    clearerr (stream->bsdFilePtr);
#endif
    
    Written = InternalFwrite(pthrCurrent, OutOriginal, 1, Length, stream->bsdFilePtr, &stream->PALferrorCode);
    if (stream->PALferrorCode == PAL_FILE_ERROR)
    {
        ERROR("fwrite() failed with errno == %d\n", errno);
    }

Done:
    InternalFree(pthrCurrent, OutOriginal);

    return Written;
}

/*******************************************************************************
Function:
  Internal_AddPaddingVfwprintf

Parameters:
  stream
    - file stream to place padding and given string (In)
  In
    - string to place into (Out) accompanied with padding
  Padding
    - number of padding chars to add
  Flags
    - padding style flags (PRINTF_FORMAT_FLAGS)
*******************************************************************************/
static INT Internal_AddPaddingVfwprintf(CPalThread *pthrCurrent, PAL_FILE *stream, LPWSTR In,
                                       INT Padding, INT Flags,BOOL convert)
{
    LPWSTR Out;
    LPWSTR OutOriginal;
    INT LengthInStr;
    INT Length;
    INT Written = 0;

    LengthInStr = PAL_wcslen(In);
    Length = LengthInStr;


    if (Padding > 0)
    {
        Length += Padding;
    }

    int iLen = (Length+1);	
    Out = (LPWSTR) InternalMalloc(pthrCurrent, iLen * sizeof(WCHAR));
    if (!Out)
    {
        ERROR("InternalMalloc failed\n");
        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return -1;
    }
    OutOriginal = Out;

    if (Flags & PFF_MINUS) /* pad on right */
    {
        if (wcscpy_s(Out, iLen, In) != SAFECRT_SUCCESS)
        {
            ERROR("wcscpy_s failed!\n");
            InternalFree(pthrCurrent, OutOriginal);
            pthrCurrent->SetLastError(ERROR_INSUFFICIENT_BUFFER);
            return -1;
        }
        Out += LengthInStr;
        iLen -= LengthInStr;
    }
    if (Padding > 0)
    {
        iLen -= Padding;
        if (Flags & PFF_ZERO) /* '0', pad with zeros */
        {
            while (Padding--)
            {
                *Out++ = '0';
            }
        }
        else /* pad with spaces */
        {
            while (Padding--)
            {
                *Out++ = ' ';
            }
        }
    }
    if (!(Flags & PFF_MINUS)) /* put 'In' after padding */
    {
        if (wcscpy_s(Out, iLen, In) != SAFECRT_SUCCESS)
        {
            ERROR("wcscpy_s failed!\n");
            InternalFree(pthrCurrent, OutOriginal);
            pthrCurrent->SetLastError(ERROR_INSUFFICIENT_BUFFER);
            return -1;
        }

        Out += LengthInStr;
        iLen -= LengthInStr;
    }

    if (Length > 0) {
        Written = Internal_Convertfwrite(pthrCurrent, OutOriginal, sizeof(wchar_16), Length, 
            (FILE*)(stream->bsdFilePtr), convert);

        if (-1 == Written)
        {
            ERROR("fwrite() failed with errno == %d\n", errno);
        }
        InternalFree(pthrCurrent, OutOriginal);
    }

    return Written;
}

/*******************************************************************************
Function:
  PAL_vsnprintf

Parameters:
  Buffer
    - out buffer
  Count
    - buffer size
  Format
    - format string
  ap
    - stdarg parameter list
*******************************************************************************/

int __cdecl PAL__vsnprintf(LPSTR Buffer, size_t Count, LPCSTR Format, va_list ap)
{
    return CoreVsnprintf(InternalGetCurrentThread(), Buffer, Count, Format, ap);
}

/*******************************************************************************
Function:
  PAL_wvsnprintf

  -- see PAL_vsnprintf above
*******************************************************************************/

int __cdecl PAL__wvsnprintf(LPWSTR Buffer, size_t Count, LPCWSTR Format, va_list ap)
{
    return CoreWvsnprintf(InternalGetCurrentThread(), Buffer, Count, Format, ap);
}

/*******************************************************************************
Function:
  PAL_vfprintf

Parameters:
  stream
    - out stream
  Format
    - format string
  ap
    - stdarg parameter list
*******************************************************************************/

int __cdecl PAL_vfprintf(PAL_FILE *stream, const char *format, va_list ap)
{
    return CoreVfprintf(InternalGetCurrentThread(), stream, format, ap);
}

/*******************************************************************************
Function:
  PAL_vfwprintf

Parameters:
  stream
    - out stream
  Format
    - format string
  ap
    - stdarg parameter list
*******************************************************************************/

int __cdecl PAL_vfwprintf(PAL_FILE *stream, const wchar_16 *format, va_list ap)
{
    return CoreVfwprintf(InternalGetCurrentThread(), stream, format, ap);
}

} // end extern "C"

int CorUnix::InternalWvsnprintf(CPalThread *pthrCurrent, LPWSTR Buffer, size_t Count, LPCWSTR Format, va_list ap)
{
    return CoreWvsnprintf(pthrCurrent, Buffer, Count, Format, ap);
}

int CorUnix::InternalVsnprintf(CPalThread *pthrCurrent, LPSTR Buffer, size_t Count, LPCSTR Format, va_list ap)
{
    return CoreVsnprintf(pthrCurrent, Buffer, Count, Format, ap);
}

int CorUnix::InternalVfprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const char *format, va_list ap)
{
    return CoreVfprintf(pthrCurrent, stream, format, ap);
}

int CorUnix::InternalVfwprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const wchar_16 *format, va_list ap)
{
    return CoreVfwprintf(pthrCurrent, stream, format, ap);
}

int NativeVsnprintf(CPalThread *pthrCurrent, LPSTR Buffer, size_t Count, LPCSTR Format, va_list ap)
{
    int retVal = 0;
    pthrCurrent->suspensionInfo.EnterUnsafeRegion();
    retVal = vsnprintf(Buffer, Count, Format, ap);
    pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
    return retVal;
}

int NativeVfprintf(CPalThread *pthrCurrent, FILE *filePtr, const char *format, va_list ap)
{
    int retVal = 0;
    pthrCurrent->suspensionInfo.EnterUnsafeRegion();
    retVal = vfprintf(filePtr, format, ap);
    pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
    return retVal;
}

int CoreVfwprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const wchar_16 *format, va_list aparg)
{
    CHAR TempBuff[1024]; /* used to hold a single %<foo> format string */
    LPCWSTR Fmt = format;
    LPWSTR TempWStr = NULL;
    LPWSTR WorkingWStr = NULL;
    WCHAR TempWChar[2];
    INT Flags;
    INT Width;
    INT Precision;
    INT Prefix;
    INT Type;
    INT TempInt;
    BOOL WStrWasMalloced = FALSE;
    int mbtowcResult;
    int written=0;
    int paddingReturnValue;
    int ret;
    va_list ap;

    /* fwprintf for now in the PAL is always used on file opened
       in text mode. In those case the output should be ANSI not Unicode */
    BOOL textMode = TRUE;

    PERF_ENTRY(vfwprintf);
    ENTRY("vfwprintf (stream=%p, format=%p (%S))\n",
          stream, format, format);
    
    va_copy(ap, aparg);

    while (*Fmt)
    {
        if(*Fmt == '%' &&
                TRUE == Internal_ExtractFormatW(pthrCurrent, &Fmt, TempBuff, &Flags,
                                                &Width, &Precision,
                                                &Prefix, &Type))
        {
            if (((Prefix == PFF_PREFIX_LONG || Prefix == PFF_PREFIX_LONG_W) &&
                 (Type == PFF_TYPE_STRING || Type == PFF_TYPE_WSTRING)) ||
                 (Type == PFF_TYPE_WSTRING && (Flags & PFF_ZERO) != 0))
            {
                WStrWasMalloced = FALSE;

                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }
                else if (WIDTH_INVALID == Width)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }
                else if (PRECISION_INVALID == Precision)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if (Type == PFF_TYPE_STRING || Prefix == PFF_PREFIX_LONG_W)
                {
                    TempWStr = va_arg(ap, LPWSTR);
                }
                else
                {
                    /* %lS assumes a LPSTR argument. */
                    LPSTR s = va_arg(ap, LPSTR );
                    UINT Length = 0;
                    Length = MultiByteToWideChar( CP_ACP, 0, s, -1, NULL, 0 );
                    if ( Length != 0 )
                    {
                        TempWStr =
                            (LPWSTR)InternalMalloc( pthrCurrent, (Length) * sizeof( WCHAR ) );
                        if ( TempWStr )
                        {
                            WStrWasMalloced = TRUE;
                            MultiByteToWideChar( CP_ACP, 0, s, -1,
                                                 TempWStr, Length );
                        }
                        else
                        {
                            ERROR( "InternalMalloc failed.\n" );
                            LOGEXIT("vfwprintf returns int -1\n");
                            PERF_EXIT(vfwprintf);
                            va_end(ap);
                            return -1;
                        }
                    }
                    else
                    {
                        ASSERT( "Unable to convert from multibyte "
                               " to wide char.\n" );
                        LOGEXIT("vfwprintf returns int -1\n");
                        PERF_EXIT(vfwprintf);
                        va_end(ap);
                        return -1;
                    }
                }

                INT Length = PAL_wcslen(TempWStr);
                WorkingWStr = (LPWSTR) InternalMalloc(pthrCurrent, (sizeof(WCHAR) * (Length + 1)));
                if (!WorkingWStr)
                {
                    ERROR("InternalMalloc failed\n");
                    LOGEXIT("vfwprintf returns int -1\n");
                    PERF_EXIT(vfwprintf);
                    pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    if (WStrWasMalloced)
                    {
                        InternalFree(pthrCurrent, TempWStr);
                    }
                    va_end(ap);
                    return -1;
                }
                if (PRECISION_DOT == Precision)
                {
                    /* copy nothing */
                    *WorkingWStr = 0;
                    Length = 0;
                }
                else if (Precision > 0 && Precision < Length)
                {
                    if (wcsncpy_s(WorkingWStr, (Length + 1), TempWStr, Precision+1) != SAFECRT_SUCCESS)
                    {
                        ERROR("Internal_AddPaddingVfwprintf failed\n");
                        if (WStrWasMalloced)
                        {
                            InternalFree(pthrCurrent, TempWStr);
                        }
                        InternalFree(pthrCurrent, WorkingWStr);
                        LOGEXIT("wcsncpy_s failed!\n");
                        PERF_EXIT(vfwprintf);	
                        va_end(ap);
                        return (-1);
                    }

                    Length = Precision;
                }
                /* copy everything */
                else
                {
                    PAL_wcscpy(WorkingWStr, TempWStr);
                }


                /* do the padding (if needed)*/
                paddingReturnValue =
                    Internal_AddPaddingVfwprintf( pthrCurrent, stream, WorkingWStr,
                                                 Width - Length,
                                                 Flags,textMode);

                if (paddingReturnValue == -1)
                {
                    ERROR("Internal_AddPaddingVfwprintf failed\n");
                    if (WStrWasMalloced)
                    {
                        InternalFree(pthrCurrent, TempWStr);
                    }
                    InternalFree(pthrCurrent, WorkingWStr);
                    LOGEXIT("vfwprintf returns int -1\n");
                    PERF_EXIT(vfwprintf);	
                    va_end(ap);
                    return (-1);
                }
                written += paddingReturnValue;

                InternalFree(pthrCurrent, WorkingWStr);
                if (WStrWasMalloced)
                {
                    InternalFree(pthrCurrent, TempWStr);
                }
            }
            else if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_CHAR)
            {
                if (WIDTH_STAR == Width ||
                    WIDTH_INVALID == Width)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if (PRECISION_STAR == Precision ||
                    PRECISION_INVALID == Precision)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                TempWChar[0] = va_arg(ap, int);
                TempWChar[1] = 0;

               /* do the padding (if needed)*/
                paddingReturnValue =
                    Internal_AddPaddingVfwprintf(pthrCurrent, stream, TempWChar,
                                                 Width - 1,
                                                 Flags,textMode);
                if (paddingReturnValue == -1)
                {
                    ERROR("Internal_AddPaddingVfwprintf failed\n");
                    LOGEXIT("vfwprintf returns int -1\n");
                    PERF_EXIT(vfwprintf);
                    va_end(ap);
                    return(-1);
                }
                written += paddingReturnValue;
            }
            /* this places the number of bytes written to the buffer in the
               next arg */
            else if (Type == PFF_TYPE_N)
            {
                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }

                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }

                if (Prefix == PFF_PREFIX_SHORT)
                {
                    *(va_arg(ap, short *)) = written;
                }
                else
                {
                    *(va_arg(ap, LPLONG)) = written;
                }
            }
            else
            {
                // Types that sprintf can handle.

                /* note: I'm using the wide buffer as a (char *) buffer when I
                   pass it to sprintf().  After I get the buffer back I make a
                   backup of the chars copied and then convert them to wide
                   and place them in the buffer (BufferPtr) */

                // This argument will be limited to 1024 characters.
                // It should be enough.
                size_t TEMP_COUNT = 1024;
                char TempSprintfStrBuffer[1024];
                char *TempSprintfStrPtr = NULL;
                char *TempSprintfStr = TempSprintfStrBuffer;
                LPWSTR TempWideBuffer;

                TempInt = 0;
                // %h (short) doesn't seem to be handled properly by local sprintf,
                // so we do the truncation ourselves for some cases.
                if (Type == PFF_TYPE_P && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert from pointer -> int -> short to avoid warnings.
                    long trunc1;
                    short trunc2;

                    trunc1 = va_arg(ap, LONG);
                    trunc2 = (short)trunc1;
                    trunc1 = trunc2;
                    
                    TempInt = snprintf(TempSprintfStr, TEMP_COUNT, TempBuff, trunc1);
                    
                    if (TempInt < 0 || static_cast<size_t>(TempInt) >= TEMP_COUNT)
                    {
                        if (NULL == (TempSprintfStrPtr = (char*)InternalMalloc(pthrCurrent, ++TempInt)))
                        {
                            ERROR("InternalMalloc failed\n");
                            LOGEXIT("vfwprintf returns int -1\n");
                            PERF_EXIT(vfwprintf);
                            pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);	
                            va_end(ap);
                            return -1;
                        }
                        
                        TempSprintfStr = TempSprintfStrPtr;
                        snprintf(TempSprintfStr, TempInt, TempBuff, trunc2);
                    }
                }
                else if (Type == PFF_TYPE_INT && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert explicitly from int to short to get
                    // correct sign extension for shorts on all systems.
                    int n;
                    short s;

                    n = va_arg(ap, int);
                    s = (short) n;

                    TempInt = snprintf(TempSprintfStr, TEMP_COUNT, TempBuff, s);

                    if (TempInt < 0 || static_cast<size_t>(TempInt) >= TEMP_COUNT)
                    {
                        if (NULL == (TempSprintfStrPtr = (char*)InternalMalloc(pthrCurrent, ++TempInt)))
                        {
                            ERROR("InternalMalloc failed\n");
                            LOGEXIT("vfwprintf returns int -1\n");
                            PERF_EXIT(vfwprintf);
                            pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);	
                            va_end(ap);
                            return -1;
                        }
                        
                        TempSprintfStr = TempSprintfStrPtr;
                        snprintf(TempSprintfStr, TempInt, TempBuff, s);
                    }
                }
                else
                {
                    va_list apcopy;

                    va_copy(apcopy, ap);
                    TempInt = NativeVsnprintf(pthrCurrent, TempSprintfStr, TEMP_COUNT, TempBuff, apcopy);
                    va_end(apcopy);
                    PAL_printf_arg_remover(&ap, Width, Precision, Type, Prefix);

                    if (TempInt < 0 || static_cast<size_t>(TempInt) >= TEMP_COUNT)
                    {
                        if (NULL == (TempSprintfStrPtr = (char*)InternalMalloc(pthrCurrent, ++TempInt)))
                        {
                            ERROR("InternalMalloc failed\n");
                            LOGEXIT("vfwprintf returns int -1\n");
                            PERF_EXIT(vfwprintf);
                            pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);		
                            va_end(ap);
                            return -1;
                        }
                        
                        TempSprintfStr = TempSprintfStrPtr;
                        va_copy(apcopy, ap);
                        NativeVsnprintf(pthrCurrent, TempSprintfStr, TempInt, TempBuff, apcopy);
                        va_end(apcopy);
                        PAL_printf_arg_remover(&ap, Width, Precision, Type, Prefix);
                    }
                }

                mbtowcResult = MultiByteToWideChar(CP_ACP, 0,
                                                   TempSprintfStr, -1,
                                                   NULL, 0);
 
                if (mbtowcResult == 0)
                {
                    ERROR("MultiByteToWideChar failed\n");
                    if(TempSprintfStrPtr)
                    {
                        InternalFree(pthrCurrent, TempSprintfStrPtr);
                    }
                    LOGEXIT("vfwprintf returns int -1\n");
                    PERF_EXIT(vfwprintf);	
                    va_end(ap);
                    return -1;
                }

                TempWideBuffer = (LPWSTR) InternalMalloc(pthrCurrent, mbtowcResult*sizeof(WCHAR));
                if (!TempWideBuffer)
                {
                    ERROR("InternalMalloc failed\n");
                    LOGEXIT("vfwprintf returns int -1\n");
                    PERF_EXIT(vfwprintf);
                    pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    if(TempSprintfStrPtr)
                    {
                        InternalFree(pthrCurrent, TempSprintfStrPtr);
                    }	
                    va_end(ap);
                    return -1;
                }

                MultiByteToWideChar(CP_ACP, 0, TempSprintfStr, -1,
                                    TempWideBuffer, mbtowcResult);
 
                ret = Internal_Convertfwrite(
                                    pthrCurrent, 
                                    TempWideBuffer, 
                                    sizeof(wchar_16), 
                                    mbtowcResult-1, 
                                    (FILE*)stream->bsdFilePtr,
                                    textMode);

                if (-1 == ret)
                {
                    ERROR("fwrite() failed with errno == %d (%s)\n", errno, strerror(errno));
                    LOGEXIT("vfwprintf returns int -1\n");
                    PERF_EXIT(vfwprintf);
                    InternalFree(pthrCurrent, TempWideBuffer);
                    if(TempSprintfStrPtr)
                    {
                        InternalFree(pthrCurrent, TempSprintfStrPtr);
                    }
                    va_end(ap);
                    return -1;
                }
                if(TempSprintfStrPtr)
                {
                    InternalFree(pthrCurrent, TempSprintfStrPtr);
                }
                InternalFree(pthrCurrent, TempWideBuffer);
            }
        }
        else
        {
            ret = Internal_Convertfwrite(
                                    pthrCurrent, 
                                    Fmt++, 
                                    sizeof(wchar_16), 
                                    1, 
                                    (FILE*)stream->bsdFilePtr,
                                    textMode); /* copy regular chars into buffer */
            
            if (-1 == ret)
            {
                ERROR("fwrite() failed with errno == %d\n", errno);
                LOGEXIT("vfwprintf returns int -1\n");
                PERF_EXIT(vfwprintf);
                va_end(ap);
                return -1;
            }
            ++written;
       }
    }
	
    LOGEXIT("vfwprintf returns int %d\n", written);
    PERF_EXIT(vfwprintf);
    va_end(ap);
    return (written);
}

int CoreVsnprintf(CPalThread *pthrCurrent, LPSTR Buffer, size_t Count, LPCSTR Format, va_list aparg)
{
    BOOL BufferRanOut = FALSE;
    CHAR TempBuff[1024]; /* used to hold a single %<foo> format string */
    LPSTR BufferPtr = Buffer;
    LPCSTR Fmt = Format;
    LPWSTR TempWStr;
    LPSTR TempStr;
    WCHAR TempWChar;
    INT Flags;
    INT Width;
    INT Precision;
    INT Prefix;
    INT Type;
    INT Length;
    INT TempInt;
    int wctombResult;
    va_list ap;
    
    va_copy(ap, aparg);

    while (*Fmt)
    {
        if (BufferRanOut || (BufferPtr - Buffer) >= static_cast<int>(Count)) //Count is assumed to be in the range of int
        {
            BufferRanOut = TRUE;
            break;
        }
        else if(*Fmt == '%' &&
                TRUE == Internal_ExtractFormatA(pthrCurrent, &Fmt, TempBuff, &Flags,
                                                &Width, &Precision,
                                                &Prefix, &Type))
        {
            if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_STRING)
            {
                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }
                else if (WIDTH_INVALID == Width)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }
                else if (PRECISION_INVALID == Precision)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                TempWStr = va_arg(ap, LPWSTR);
                Length = WideCharToMultiByte(CP_ACP, 0, TempWStr, -1, 0,
                                             0, 0, 0);
                if (!Length)
                {              
                    ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                          GetLastError());
                    va_end(ap);
                    return -1;
                }
                TempStr = (LPSTR) InternalMalloc(pthrCurrent, Length);
                if (!TempStr)
                {        
                    ERROR("InternalMalloc failed\n");
                    pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    va_end(ap);
                    return -1;
                }
                if (PRECISION_DOT == Precision)
                {
                    /* copy nothing */
                    *TempStr = 0;
                    Length = 0;
                }
                else if (Precision > 0 && Precision < Length - 1)
                {
                    Length = WideCharToMultiByte(CP_ACP, 0, TempWStr,
                                                 Precision, TempStr, Length,
                                                 0, 0);
                    if (!Length)
                    {               
                        ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                              GetLastError());
                        InternalFree(pthrCurrent, TempStr);  
                        va_end(ap);
                        return -1;
                    }
                    TempStr[Length] = 0;
                    Length = Precision;
                }
                /* copy everything */
                else
                {
                    wctombResult = WideCharToMultiByte(CP_ACP, 0, TempWStr, -1,
                                                       TempStr, Length, 0, 0);
                    if (!wctombResult)
                    {               
                        ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                              GetLastError());
                        InternalFree(pthrCurrent, TempStr);  
                        va_end(ap);
                        return -1;
                    }
                    --Length; /* exclude null char */
                }

                /* do the padding (if needed)*/
                BufferRanOut = !Internal_AddPaddingA(&BufferPtr,
                                                   Count - (BufferPtr - Buffer),
                                                   TempStr,
                                                   Width - Length,
                                                   Flags);

                InternalFree(pthrCurrent, TempStr);
            }
            else if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_CHAR)
            {
                CHAR TempBuffer[5];

                if (WIDTH_STAR == Width ||
                    WIDTH_INVALID == Width)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }
                if (PRECISION_STAR == Precision ||
                    PRECISION_INVALID == Precision)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                TempWChar = va_arg(ap, int);
                Length = WideCharToMultiByte(CP_ACP, 0, &TempWChar, 1,
                                             TempBuffer, sizeof(TempBuffer),
                                             0, 0);
                if (!Length)
                {                
                    ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                          GetLastError());
                    va_end(ap);
                    return -1;
                }
                TempBuffer[Length] = 0;

                /* do the padding (if needed)*/
                BufferRanOut = !Internal_AddPaddingA(&BufferPtr,
                                                   Count - (BufferPtr - Buffer),
                                                   TempBuffer,
                                                   Width - Length,
                                                   Flags);

            }
            /* this places the number of bytes written to the buffer in the
               next arg */
            else if (Type == PFF_TYPE_N)
            {
                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }
                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }

                if (Prefix == PFF_PREFIX_SHORT)
                {
                    *(va_arg(ap, short *)) = BufferPtr - Buffer;
                }
                else
                {
                    *(va_arg(ap, LPLONG)) = BufferPtr - Buffer;
                }
            }
            else if (Type == PFF_TYPE_CHAR && (Flags & PFF_ZERO) != 0)
            {
                // Some versions of sprintf don't support 0-padded chars,
                // so we handle them here.
                char ch[2];

                ch[0] = (char) va_arg(ap, int);
                ch[1] = '\0';
                Length = 1;
                BufferRanOut = !Internal_AddPaddingA(&BufferPtr,
                                           Count - (BufferPtr - Buffer),
                                           ch,
                                           Width - Length,
                                           Flags);
            }
            else if (Type == PFF_TYPE_STRING && (Flags & PFF_ZERO) != 0)
            {
                // Some versions of sprintf don't support 0-padded strings,
                // so we handle them here.
                char *tempStr;

                tempStr = va_arg(ap, char *);
                Length = strlen(tempStr);
                BufferRanOut = !Internal_AddPaddingA(&BufferPtr,
                                           Count - (BufferPtr - Buffer),
                                           tempStr,
                                           Width - Length,
                                           Flags);
            }
            else
            {
                // Types that sprintf can handle
                size_t TempCount = Count - (BufferPtr - Buffer);

#if !HAVE_LARGE_SNPRINTF_SUPPORT
                // Limit TempCount to 0x40000000, which is sufficient
                // for platforms on which snprintf fails for very large
                // sizes.
                if (TempCount > 0x40000000)
                {
                    TempCount = 0x40000000;
                }
#endif  // HAVE_LARGE_SNPRINTF_SUPPORT

                TempInt = 0;
                // %h (short) doesn't seem to be handled properly by local sprintf,
                // so we do the truncation ourselves for some cases.
                if (Type == PFF_TYPE_P && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert from pointer -> int -> short to avoid warnings.
                    long trunc1;
                    short trunc2;

                    trunc1 = va_arg(ap, LONG);
                    trunc2 = (short) trunc1;
                    trunc1 = trunc2;

                    TempInt = snprintf(BufferPtr, TempCount, TempBuff, trunc1);
                }
                else if (Type == PFF_TYPE_INT && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert explicitly from int to short to get
                    // correct sign extension for shorts on all systems.
                    int n;
                    short s;

                    n = va_arg(ap, int);
                    s = (short) n;

                    TempInt = snprintf(BufferPtr, TempCount, TempBuff, s);
                }
                else
                {
                    va_list apcopy;
                    va_copy(apcopy, ap);
                    TempInt = NativeVsnprintf(pthrCurrent, BufferPtr, TempCount, TempBuff, apcopy);
                    va_end(apcopy);
                    PAL_printf_arg_remover(&ap, Width, Precision, Type, Prefix);
                }

                if (TempInt < 0 || static_cast<size_t>(TempInt) >= TempCount) /* buffer not long enough */
                {
                    BufferPtr += TempCount;
                    BufferRanOut = TRUE;
                }
                else
                {
                    BufferPtr += TempInt;
                }
            }
        }
        else
        {
            *BufferPtr++ = *Fmt++; /* copy regular chars into buffer */
        }
    }

    if (static_cast<int>(Count) > (BufferPtr - Buffer)) //Count is assumed to be in the range of int
    {
        *BufferPtr = 0; /* end the string */
    }

    va_end(ap);

    if (BufferRanOut)
    {
        errno = ERANGE;
        return -1;
    }
    else
    {
        return BufferPtr - Buffer;
    }
}

int CoreWvsnprintf(CPalThread *pthrCurrent, LPWSTR Buffer, size_t Count, LPCWSTR Format, va_list aparg)
{
    BOOL BufferRanOut = FALSE;
    CHAR TempBuff[1024]; /* used to hold a single %<foo> format string */
    LPWSTR BufferPtr = Buffer;
    LPCWSTR Fmt = Format;
    LPWSTR TempWStr = NULL;
    LPWSTR WorkingWStr = NULL;
    WCHAR TempWChar[2];
    INT Flags;
    INT Width;
    INT Precision;
    INT Prefix;
    INT Type;
    INT TempInt;
    LPSTR TempNumberBuffer;
    int mbtowcResult;
    va_list(ap);

    PERF_ENTRY(wvsnprintf);
    ENTRY("wvsnprintf (buffer=%p, count=%u, format=%p (%S))\n",
          Buffer, Count, Format, Format);
    
    va_copy(ap, aparg);

    while (*Fmt)
    {
        if (BufferRanOut || (BufferPtr - Buffer) >= static_cast<int>(Count)) //Count is assumed to be in the range of int
        {
            BufferRanOut = TRUE;
            break;
        }
        else if(*Fmt == '%' &&
                TRUE == Internal_ExtractFormatW(pthrCurrent, &Fmt, TempBuff, &Flags,
                                                &Width, &Precision,
                                                &Prefix, &Type))
        {
            if (((Prefix == PFF_PREFIX_LONG || Prefix == PFF_PREFIX_LONG_W) &&
                (Type == PFF_TYPE_STRING || Type == PFF_TYPE_WSTRING)) ||
                (Prefix == PFF_PREFIX_SHORT && Type == PFF_TYPE_STRING) ||
                (Type == PFF_TYPE_WSTRING && (Flags & PFF_ZERO) != 0))
            {             
                BOOL needToFree = FALSE;

                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }
                else if (WIDTH_INVALID == Width)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }
                else if (PRECISION_INVALID == Precision)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if ((Type == PFF_TYPE_STRING && Prefix == PFF_PREFIX_LONG) ||
                    Prefix == PFF_PREFIX_LONG_W)
                {
                    TempWStr = va_arg(ap, LPWSTR);
                }
                else
                {
                    // %lS and %hs assume an LPSTR argument.
                    LPSTR s = va_arg(ap, LPSTR );
                    UINT Length = 0;
                    Length = MultiByteToWideChar( CP_ACP, 0, s, -1, NULL, 0 );
                    if ( Length != 0 )
                    {
                        TempWStr =
                            (LPWSTR)InternalMalloc(pthrCurrent, (Length + 1 ) * sizeof( WCHAR ) );
                        if ( TempWStr )
                        {
                            needToFree = TRUE;
                            MultiByteToWideChar( CP_ACP, 0, s, -1,
                                                 TempWStr, Length );
                        }
                        else
                        {                           
                            ERROR( "InternalMalloc failed.\n" );
                            va_end(ap);
                            return -1;
                        }
                    }
                    else
                    {                  
                        ASSERT( "Unable to convert from multibyte "
                               " to wide char.\n" );    
                        va_end(ap);
                        return -1;
                    }

                }
                
                INT Length = PAL_wcslen(TempWStr);
                WorkingWStr = (LPWSTR) InternalMalloc(pthrCurrent, sizeof(WCHAR) * (Length + 1));
                if (!WorkingWStr)
                {                
                    ERROR("InternalMalloc failed\n");
                    pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    if (needToFree)
                    {
                        InternalFree(pthrCurrent, TempWStr);
                    }
                    va_end(ap);
                    return -1;
                }
                if (PRECISION_DOT == Precision)
                {
                    // Copy nothing
                    *WorkingWStr = 0;
                    Length = 0;
                }
                else if (Precision > 0 && Precision < Length)
                {
                    if (wcsncpy_s(WorkingWStr, (Length + 1), TempWStr, Precision+1) != SAFECRT_SUCCESS)
                    {
                        ERROR("CoreWvsnprintf failed\n");
                        if (needToFree)
                        {
                            InternalFree(pthrCurrent, TempWStr);
                        }
                        InternalFree(pthrCurrent, WorkingWStr);
                        LOGEXIT("wcsncpy_s failed!\n");
                        PERF_EXIT(wvsnprintf);	
                        va_end(ap);
                        return (-1);
                    }

                    Length = Precision;
                }
                else
                {
                    // Copy everything
                    PAL_wcscpy(WorkingWStr, TempWStr);
                }

                // Add padding if needed.
                BufferRanOut = !Internal_AddPaddingW(&BufferPtr,
                                                   Count - (BufferPtr - Buffer),
                                                   WorkingWStr,
                                                   Width - Length,
                                                   Flags);

                if (needToFree)
                {
                    InternalFree(pthrCurrent, TempWStr);
                }
                InternalFree(pthrCurrent, WorkingWStr);
            }
            else if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_CHAR)
            {
                if (WIDTH_STAR == Width ||
                    WIDTH_INVALID == Width)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if (PRECISION_STAR == Precision ||
                    PRECISION_INVALID == Precision)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                TempWChar[0] = va_arg(ap, int);
                TempWChar[1] = 0;

                /* do the padding (if needed)*/
                BufferRanOut = !Internal_AddPaddingW(&BufferPtr,
                                                   Count - (BufferPtr - Buffer),
                                                   TempWChar,
                                                   Width - 1,
                                                   Flags);

            }
            /* this places the number of bytes written to the buffer in the
               next arg */
            else if (Type == PFF_TYPE_N)
            {
                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }
                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }

                if (Prefix == PFF_PREFIX_SHORT)
                {
                    *(va_arg(ap, short *)) = BufferPtr - Buffer;
                }
                else
                {
                    *(va_arg(ap, LPLONG)) = BufferPtr - Buffer;
                }
            }
            else
            {
                // Types that sprintf can handle

                /* note: I'm using the wide buffer as a (char *) buffer when I
                   pass it to sprintf().  After I get the buffer back I make a
                   backup of the chars copied and then convert them to wide
                   and place them in the buffer (BufferPtr) */
                size_t TempCount = Count - (BufferPtr - Buffer);
                TempInt = 0;

#if !HAVE_LARGE_SNPRINTF_SUPPORT
                // Limit TempCount to 0x40000000, which is sufficient
                // for platforms on which snprintf fails for very large
                // sizes.
                if (TempCount > 0x40000000)
                {
                    TempCount = 0x40000000;
                }
#endif  // HAVE_LARGE_SNPRINTF_SUPPORT

                // %h (short) doesn't seem to be handled properly by local sprintf,
                // so we do the truncation ourselves for some cases.
                if (Type == PFF_TYPE_P && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert from pointer -> int -> short to avoid warnings.
                    long trunc1;
                    short trunc2;

                    trunc1 = va_arg(ap, LONG);
                    trunc2 = (short)trunc1;
                    trunc1 = trunc2;

                    TempInt = snprintf((LPSTR)BufferPtr, TempCount, TempBuff, trunc1);
                }
                else if (Type == PFF_TYPE_INT && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert explicitly from int to short to get
                    // correct sign extension for shorts on all systems.
                    int n;
                    short s;

                    n = va_arg(ap, int);
                    s = (short) n;

                    TempInt = snprintf((LPSTR)BufferPtr, TempCount, TempBuff, s);
                }
                else
                {
                    va_list apcopy;
                    va_copy(apcopy, ap);
                    TempInt = NativeVsnprintf(pthrCurrent, (LPSTR) BufferPtr, TempCount, TempBuff, apcopy);
                    va_end(apcopy);
                    PAL_printf_arg_remover(&ap, Width, Precision, Type, Prefix);
                }

                if (TempInt == 0)
                {
                    // The argument is "".
                    continue;
                }
                if (TempInt < 0 || static_cast<size_t>(TempInt) >= TempCount) /* buffer not long enough */
                {
                    TempNumberBuffer = (LPSTR) InternalMalloc(pthrCurrent, TempCount+1);
                    if (!TempNumberBuffer)
                    {                         
                        ERROR("InternalMalloc failed\n");
                        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                        errno = ENOMEM;
                        va_end(ap);
                        return -1;
                    }
                    
                    if (strncpy_s(TempNumberBuffer, TempCount+1, (LPSTR) BufferPtr, TempCount) != SAFECRT_SUCCESS)
                    {
                        ASSERT("strncpy_s failed!\n");
                        InternalFree(pthrCurrent, TempNumberBuffer);
                        va_end(ap);
                        return -1;
                    }

                    mbtowcResult = MultiByteToWideChar(CP_ACP, 0,
                                                       TempNumberBuffer,
                                                       TempCount,
                                                       BufferPtr, TempCount);
                    if (!mbtowcResult)
                    {                 
                        ASSERT("MultiByteToWideChar failed.  Error is %d\n",
                              GetLastError());
                        InternalFree(pthrCurrent, TempNumberBuffer);
                        va_end(ap);
                        return -1;
                    }
                    BufferPtr += TempCount;
                    BufferRanOut = TRUE;
                }
                else
                {
                    TempNumberBuffer = (LPSTR) InternalMalloc(pthrCurrent, TempInt+1);
                    if (!TempNumberBuffer)
                    {          
                        ERROR("InternalMalloc failed\n");
                        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);  
                        va_end(ap);
                        return -1;
                    }
                    
                    if (strncpy_s(TempNumberBuffer, TempInt+1, (LPSTR) BufferPtr, TempInt) != SAFECRT_SUCCESS)
                    {
                        ASSERT("strncpy_s failed!\n");
                        InternalFree(pthrCurrent, TempNumberBuffer); 
                        va_end(ap);
                        return -1;
                    }

                    mbtowcResult = MultiByteToWideChar(CP_ACP, 0,
                                                       TempNumberBuffer,
                                                       TempInt,
                                                       BufferPtr, TempInt);
                    if (!mbtowcResult)
                    {          
                        ASSERT("MultiByteToWideChar failed.  Error is %d\n",
                              GetLastError());
                        InternalFree(pthrCurrent, TempNumberBuffer); 
                        va_end(ap);
                        return -1;
                    }
                    BufferPtr += TempInt;
                }
                InternalFree(pthrCurrent, TempNumberBuffer);
            }
        }
        else
        {
            *BufferPtr++ = *Fmt++; /* copy regular chars into buffer */
        }
    }

    if (static_cast<int>(Count) > (BufferPtr - Buffer)) //Count is assumed to be in the range of int
    {
        *BufferPtr = 0; /* end the string */
    }
    
    va_end(ap);

    if (BufferRanOut)
    {
        errno = ERANGE;
        return -1;
    }
    else
    {
        return BufferPtr - Buffer;
    }
}

int CoreVfprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const char *format, va_list aparg)
{
    CHAR TempBuff[1024]; /* used to hold a single %<foo> format string */
    LPCSTR Fmt = format;
    LPWSTR TempWStr;
    LPSTR TempStr;
    WCHAR TempWChar;
    INT Flags;
    INT Width;
    INT Precision;
    INT Prefix;
    INT Type;
    INT Length;
    INT TempInt;
    int wctombResult;
    int written = 0;
    int paddingReturnValue;
    va_list ap;
    
    PERF_ENTRY(vfprintf);
    
    va_copy(ap, aparg);
        
    while (*Fmt)
    {
        if (*Fmt == '%' &&
            TRUE == Internal_ExtractFormatA(pthrCurrent, &Fmt, TempBuff, &Flags,
                                            &Width, &Precision,
                                            &Prefix, &Type))
        {
            if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_STRING)
            {
                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }
                else if (WIDTH_INVALID == Width)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }
                else if (PRECISION_INVALID == Precision)
                {
                    /* both a '*' and a number, ignore, but remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                TempWStr = va_arg(ap, LPWSTR);
                Length = WideCharToMultiByte(CP_ACP, 0, TempWStr, -1, 0,
                                             0, 0, 0);
                if (!Length)
                {
                    ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                          GetLastError());
                    PERF_EXIT(vfprintf);
                    va_end(ap);
                    return -1;
                }
                TempStr = (LPSTR) InternalMalloc(pthrCurrent, Length);
                if (!TempStr)
                {
                    ERROR("InternalMalloc failed\n");
                    pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    PERF_EXIT(vfprintf);  
                    va_end(ap);
                    return -1;
                }
                if (PRECISION_DOT == Precision)
                {
                    /* copy nothing */
                    *TempStr = 0;
                    Length = 0;
                }
                else if (Precision > 0 && Precision < Length - 1)
                {
                    Length = WideCharToMultiByte(CP_ACP, 0, TempWStr,
                                                 Precision, TempStr, Length,
                                                 0, 0);
                    if (!Length)
                    {
                        ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                              GetLastError());
                        InternalFree(pthrCurrent, TempStr);
                        PERF_EXIT(vfprintf);        
                        va_end(ap);
                        return -1;
                    }
                    TempStr[Length] = 0;
                    Length = Precision;
                }
                /* copy everything */
                else
                {
                    wctombResult = WideCharToMultiByte(CP_ACP, 0, TempWStr, -1,
                                                       TempStr, Length, 0, 0);
                    if (!wctombResult)
                    {
                        ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                              GetLastError());
                        InternalFree(pthrCurrent, TempStr);
                        PERF_EXIT(vfprintf);      
                        va_end(ap);
                        return -1;
                    }
                    --Length; /* exclude null char */
                }

                /* do the padding (if needed)*/
                paddingReturnValue =
                  Internal_AddPaddingVfprintf(pthrCurrent, stream, TempStr,
                                              Width - Length, Flags);
                if (-1 == paddingReturnValue)
                {
                    ERROR("Internal_AddPaddingVfprintf failed\n");
                    InternalFree(pthrCurrent, TempStr);
                    PERF_EXIT(vfprintf);  
                    va_end(ap);
                    return -1;
                }
                written += paddingReturnValue;

                InternalFree(pthrCurrent, TempStr);
            }
            else if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_CHAR)
            {
                CHAR TempBuffer[5];
                if (WIDTH_STAR == Width ||
                    WIDTH_INVALID == Width)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }
                if (PRECISION_STAR == Precision ||
                    PRECISION_INVALID == Precision)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }
                
                TempWChar = va_arg(ap, int);
                Length = WideCharToMultiByte(CP_ACP, 0, &TempWChar, 1,
                                             TempBuffer, sizeof(TempBuffer),
                                             0, 0);
                if (!Length)
                {
                    ASSERT("WideCharToMultiByte failed.  Error is %d\n",
                          GetLastError());
                    PERF_EXIT(vfprintf);
                    va_end(ap);
                    return -1;
                }
                TempBuffer[Length] = 0;

                /* do the padding (if needed)*/
                paddingReturnValue =
                  Internal_AddPaddingVfprintf(pthrCurrent, stream, TempBuffer,
                                              Width - Length, Flags);
                if (-1 == paddingReturnValue)
                {
                    ERROR("Internal_AddPaddingVfprintf failed\n");
                    PERF_EXIT(vfprintf);
                    va_end(ap);
                    return -1;
                }
                written += paddingReturnValue;

            }
            /* this places the number of bytes written to the buffer in the
               next arg */
            else if (Type == PFF_TYPE_N)
            {
                if (WIDTH_STAR == Width)
                {
                    Width = va_arg(ap, INT);
                }
                if (PRECISION_STAR == Precision)
                {
                    Precision = va_arg(ap, INT);
                }

                if (Prefix == PFF_PREFIX_SHORT)
                {
                    *(va_arg(ap, short *)) = written;
                }
                else
                {
                    *(va_arg(ap, LPLONG)) = written;
                }
            }
            else if (Type == PFF_TYPE_CHAR && (Flags & PFF_ZERO) != 0)
            {
                // Some versions of fprintf don't support 0-padded chars,
                // so we handle them here.
                char ch[2];

                ch[0] = (char) va_arg(ap, int);
                ch[1] = '\0';
                Length = 1;
                paddingReturnValue = Internal_AddPaddingVfprintf(
                                                pthrCurrent, 
                                                stream,
                                                ch,
                                                Width - Length,
                                                Flags);
                if (-1 == paddingReturnValue)
                {
                    ERROR("Internal_AddPaddingVfprintf failed\n");
                    PERF_EXIT(vfprintf);    
                    va_end(ap);
                    return -1;
                }
                written += paddingReturnValue;
            }
            else if (Type == PFF_TYPE_STRING && (Flags & PFF_ZERO) != 0)
            {
                // Some versions of fprintf don't support 0-padded strings,
                // so we handle them here.
                char *tempStr;

                tempStr = va_arg(ap, char *);
                Length = strlen(tempStr);
                paddingReturnValue = Internal_AddPaddingVfprintf(
                                                pthrCurrent,
                                                stream,
                                                tempStr,
                                                Width - Length,
                                                Flags);
                if (-1 == paddingReturnValue)
                {
                    ERROR("Internal_AddPaddingVfprintf failed\n");
                    PERF_EXIT(vfprintf);    
                    va_end(ap);
                    return -1;
                }
                written += paddingReturnValue;
            }
            else
            {
                // Types that fprintf can handle.
                TempInt = 0;

                // %h (short) doesn't seem to be handled properly by local sprintf,
                // so we do the truncation ourselves for some cases.
                if (Type == PFF_TYPE_P && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert from pointer -> int -> short to avoid warnings.
                    long trunc1;
                    short trunc2;

                    trunc1 = va_arg(ap, LONG);
                    trunc2 = (short)trunc1;
                    trunc1 = trunc2;

                    TempInt = fprintf( stream->bsdFilePtr, TempBuff, trunc1);
                }
                else if (Type == PFF_TYPE_INT && Prefix == PFF_PREFIX_SHORT)
                {
                    // Convert explicitly from int to short to get
                    // correct sign extension for shorts on all systems.
                    int n;
                    short s;

                    n = va_arg(ap, int);
                    s = (short) n;

                    TempInt = fprintf( stream->bsdFilePtr, TempBuff, s);
                }
                else
                {
                    va_list apcopy;
                    va_copy(apcopy, ap);
                    TempInt = NativeVfprintf(pthrCurrent, stream->bsdFilePtr, TempBuff, apcopy);
                    va_end(apcopy);
                    PAL_printf_arg_remover(&ap, Width, Precision, Type, Prefix);
                }

                if (-1 == TempInt)
                {
                    ERROR("vfprintf returned an error\n");
                }
                else
                {
                    written += TempInt;
                }
            }
        }
        else
        {

#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
            clearerr (stream->bsdFilePtr);
#endif

            InternalFwrite(pthrCurrent, Fmt++, 1, 1, stream->bsdFilePtr, &stream->PALferrorCode); /* copy regular chars into buffer */
            if (stream->PALferrorCode == PAL_FILE_ERROR)
            {
                ERROR("fwrite() failed with errno == %d\n", errno);
                PERF_EXIT(vfprintf);    
                va_end(ap);
                return -1;
            }
            ++written;
        }
    }
    
    va_end(ap);
    
    PERF_EXIT(vfprintf);
    return written;
}

