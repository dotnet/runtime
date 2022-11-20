// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

static const char __nullstring[] = "(null)";  /* string to print on null ptr */
static const WCHAR __wnullstring[] = W("(null)"); /* string to print on null ptr */

int CoreVfprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const char *format, va_list ap);

extern "C"
{

/*******************************************************************************
Function:
  Internal_ExtractFormatA

Parameters:
  Fmt
    - format string to parse
    - first character must be a '%'
    - parameter gets updated to point to the character after
      the %<foo> format string
  Out
    - buffer will contain the %<foo> format string
  Flags
    - parameter will be set with the PRINTF_FORMAT_FLAGS defined above
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
  - MS's wsprintf seems to ignore a 'h' prefix for number types
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
    TempStrPtr = TempStr = (LPSTR) InternalMalloc(strlen(*Fmt)+1);
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
            goto EXIT;
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
                goto EXIT;
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

#ifdef HOST_64BIT
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
    #ifdef HOST_64BIT
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
#ifdef HOST_64BIT
        // Only want to change the prefix on 64 bit when printing characters.
        if (**Fmt == 'c' || **Fmt == 's')
#endif
        {
            *Prefix = PFF_PREFIX_LONG;
        }
        if (**Fmt == 'l')
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

EXIT:
    free(TempStr);
    return Result;
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

INT Internal_AddPaddingVfprintf(CPalThread *pthrCurrent, PAL_FILE *stream, LPCSTR In,
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
    Out = (LPSTR) InternalMalloc(Length+1);
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

    Written = InternalFwrite(OutOriginal, 1, Length, stream->bsdFilePtr, &stream->PALferrorCode);
    if (stream->PALferrorCode == PAL_FILE_ERROR)
    {
        ERROR("fwrite() failed with errno == %d\n", errno);
    }

Done:
    free(OutOriginal);

    return Written;
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

} // end extern "C"

int CoreVfprintf(CPalThread *pthrCurrent, PAL_FILE *stream, const char *format, va_list aparg)
{
    CHAR TempBuff[1024]; /* used to hold a single %<foo> format string */
    LPCSTR Fmt = format;
    LPCWSTR TempWStr;
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
                if (TempWStr == NULL)\
                {
                    TempWStr = __wnullstring;
                }
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
                TempStr = (LPSTR) InternalMalloc(Length);
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
                        free(TempStr);
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
                        free(TempStr);
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
                    free(TempStr);
                    PERF_EXIT(vfprintf);
                    va_end(ap);
                    return -1;
                }
                written += paddingReturnValue;

                free(TempStr);
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

                TempWChar = (WCHAR)va_arg(ap, int);
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
                TempBuffer[Length] = W('\0');

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
                    *(va_arg(ap, short *)) = (short)written;
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
                const char *tempStr;

                tempStr = va_arg(ap, char *);
                if (tempStr == NULL)
                {
                    tempStr = __nullstring;
                }
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

                    TempInt = fprintf(stream->bsdFilePtr, TempBuff, trunc1);
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
                    TempInt = vfprintf(stream->bsdFilePtr, TempBuff, apcopy);
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

            InternalFwrite(Fmt++, 1, 1, stream->bsdFilePtr, &stream->PALferrorCode); /* copy regular chars into buffer */
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
