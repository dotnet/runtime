//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    silent_printf.c

Abstract:

    Implementation of a silent version of PAL_vsprintf and PAL_vfprintf function.
    (without any reference to TRACE/ERROR/... macros, needed by the tracing macros) 

Revision History:



--*/


#include "pal/palinternal.h"
#include "pal/cruntime.h"
#include "pal/locale.h"

/* clip strings (%s, %S) at this number of characters */
#define MAX_STR_LEN 300

static int Silent_WideCharToMultiByte(LPCWSTR lpWideCharStr, int cchWideChar, 
                                      LPSTR lpMultiByteStr, int cbMultiByte);
static BOOL Silent_ExtractFormatA(LPCSTR *Fmt, LPSTR Out, LPINT Flags, LPINT Width,
                                  LPINT Precision, LPINT Prefix, LPINT Type);
static INT Silent_AddPaddingVfprintf(PAL_FILE *stream, LPSTR In, INT Padding, 
                                     INT Flags);

static size_t Silent_PAL_wcslen(const wchar_16 *string);

/*******************************************************************************
Function:
  PAL_vsnprintf (silent version)
  for more details, see PAL_vsnprintf in printf.c
*******************************************************************************/
INT Silent_PAL_vsnprintf(LPSTR Buffer, INT Count, LPCSTR Format, va_list ap)
{
    BOOL BufferRanOut = FALSE;
    CHAR TempBuff[1024]; /* used to hold a single %<foo> format string */
    LPSTR BufferPtr = Buffer;
    LPCSTR Fmt = Format;
    LPWSTR TempWStr;
    CHAR TempStr[MAX_STR_LEN+1];
    WCHAR TempWChar;
    INT Flags;
    INT Width;
    INT Precision;
    INT Prefix;
    INT Type;
    INT Length;
    INT TempInt;
    int wctombResult;

    while (*Fmt)
    {
        if ((BufferPtr - Buffer) >= Count)
        {
            BufferRanOut = TRUE;
            break;
        }
        else if(*Fmt == '%' &&
                TRUE == Silent_ExtractFormatA(&Fmt, TempBuff, &Flags,
                                                &Width, &Precision,
                                                &Prefix, &Type))
        {
            if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_STRING)
            {
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
                Length = Silent_WideCharToMultiByte(TempWStr, -1, 0, 0);
                if (!Length)
                {
                    return -1;
                }

                /* clip string output to MAX_STR_LEN characters */
                if(-1 == Precision)
                {
                    Precision = MAX_STR_LEN;
                }

                if (PRECISION_DOT == Precision)
                {
                    /* copy nothing */
                    *TempStr = 0;
                    Length = 0;
                }
                else if (Precision > 0 && Precision < Length - 1)
                {
                    Length = Silent_WideCharToMultiByte(TempWStr, Precision, 
                                                        TempStr, Length);
                    if (!Length)
                    {
                        return -1;
                    }
                    TempStr[Length] = 0;
                    Length = Precision;
                }
                /* copy everything */
                else
                {
                    wctombResult = Silent_WideCharToMultiByte(TempWStr, -1,
                                                              TempStr, Length);
                    if (!wctombResult)
                    {
                        PAL_free(TempStr);
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
            }
            else if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_CHAR)
            {
                CHAR TempBuffer[4];
                if (PRECISION_STAR == Precision ||
                    PRECISION_INVALID == Precision)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                TempWChar = va_arg(ap, int);
                Length = Silent_WideCharToMultiByte(&TempWChar, 1, TempBuffer, 4);
                if (!Length)
                {
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
            /* types that sprintf can handle */
            else
            {
                size_t TempCount = Count - (BufferPtr - Buffer);

                TempInt = 0;
                /* %h (short) doesn't seem to be handled properly by local sprintf,
                   so lets do the truncation ourselves.  (ptr -> int -> short to avoid
                   warnings */
                if (Type == PFF_TYPE_P && Prefix == PFF_PREFIX_SHORT)
                {
                    long trunc1;
                    short trunc2;

                    trunc1 = va_arg(ap, LONG);
                    trunc2 = (short)trunc1;

                    TempInt = snprintf(BufferPtr, TempCount, TempBuff, trunc2);
                }
                else
                {
                    /* limit string output (%s) to 300 characters */
                    if(TempBuff[0]=='%' && TempBuff[1]=='s')
                    {
                        if (strcpy_s(TempBuff, sizeof(TempBuff), "%.300s") != SAFECRT_SUCCESS)
                        {
                            return -1;
                        }
                    }                                     

                     TempInt = PAL__vsnprintf(BufferPtr, TempCount, TempBuff, ap);
                     PAL_printf_arg_remover((va_list *)&ap, Precision, Type, Prefix);
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

    if (Count > (BufferPtr - Buffer))
    {
        *BufferPtr = 0; /* end the string */
    }

    if (BufferRanOut)
    {
        return -1;
    }
    else
    {

        return BufferPtr - Buffer;
    }
}

/*++
Function:
  PAL_vfprintf (silent version)

  for more details, see PAL_vfprintf in printf.c
--*/
int Silent_PAL_vfprintf(PAL_FILE *stream, const char *format, va_list ap)
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

    while (*Fmt)
    {
        if (*Fmt == '%' &&
            TRUE == Silent_ExtractFormatA(&Fmt, TempBuff, &Flags, &Width, 
                                          &Precision, &Prefix, &Type))
        {
            if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_STRING)
            {
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
                Length = Silent_WideCharToMultiByte(TempWStr, -1, 0, 0);
                if (!Length)
                {
                    return -1;
                }
                TempStr = (LPSTR) PAL_malloc(Length);
                if (!TempStr)
                {
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
                    Length = Silent_WideCharToMultiByte(TempWStr, Precision, 
                                                        TempStr, Length);
                    if (!Length)
                    {
                        PAL_free(TempStr);
                        return -1;
                    }
                    TempStr[Length] = 0;
                    Length = Precision;
                }
                /* copy everything */
                else
                {
                    wctombResult = Silent_WideCharToMultiByte(TempWStr, -1, 
                                                              TempStr, Length);
                    if (!wctombResult)
                    {
                        PAL_free(TempStr);
                        return -1;
                    }
                    --Length; /* exclude null char */
                }

                /* do the padding (if needed)*/
                paddingReturnValue =
                  Silent_AddPaddingVfprintf(stream, TempStr, Width - Length, Flags);
                if (-1 == paddingReturnValue)
                {
                    PAL_free(TempStr);
                    return -1;
                }
                written += paddingReturnValue;

                PAL_free(TempStr);
            }
            else if (Prefix == PFF_PREFIX_LONG && Type == PFF_TYPE_CHAR)
            {
                CHAR TempBuffer[4];
                if (PRECISION_STAR == Precision ||
                    PRECISION_INVALID == Precision)
                {
                    /* ignore (because it's a char), and remove arg */
                    TempInt = va_arg(ap, INT); /* value not used */
                }

                TempWChar = va_arg(ap, int);
                Length = Silent_WideCharToMultiByte(&TempWChar, 1, TempBuffer, 4);
                if (!Length)
                {
                    return -1;
                }
                TempBuffer[Length] = 0;

                /* do the padding (if needed)*/
                paddingReturnValue =
                  Silent_AddPaddingVfprintf(stream, TempBuffer,
                                              Width - Length, Flags);
                if (-1 == paddingReturnValue)
                {
                    return -1;
                }
                written += paddingReturnValue;

            }
            /* this places the number of bytes written to the buffer in the
               next arg */
            else if (Type == PFF_TYPE_N)
            {
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
            /* types that sprintf can handle */
            else
            {
                TempInt = 0;

                /* %h (short) doesn't seem to be handled properly by local sprintf,
                   so lets do the truncation ourselves.  (ptr -> int -> short to avoid
                   warnings */
                if (Type == PFF_TYPE_P && Prefix == PFF_PREFIX_SHORT)
                {
                    long trunc1;
                    short trunc2;

                    trunc1 = va_arg(ap, LONG);
                    trunc2 = (short)trunc1;

                    TempInt = fprintf((FILE*)stream, TempBuff, trunc2);
                }
                else
                {
                    TempInt = PAL_vfprintf(stream, TempBuff, ap);
                    PAL_printf_arg_remover((va_list *)&ap, Precision, Type, Prefix);
                }

                if (-1 != TempInt)
                {
                    written += TempInt;
                }
            }
        }
        else
        {
#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
            clearerr((FILE*)stream);
#endif

            PAL_fwrite(Fmt++, 1, 1, stream); /* copy regular chars into buffer */
            if (stream->PALferrorCode == PAL_FILE_ERROR)
            {
                return -1;
            }
            ++written;
        }
    }

    return written;
}

/*++
Function:
  WideCharToMultiByte (reduced and silent version)

See MSDN doc.
--*/
int Silent_WideCharToMultiByte(LPCWSTR lpWideCharStr, int cchWideChar,
                               LPSTR lpMultiByteStr, int cbMultiByte)
{
    INT retval =0;

    if ((lpWideCharStr == NULL)||
        (lpWideCharStr == (LPCWSTR) lpMultiByteStr))
    {
        goto EXIT;
    }

    if (cchWideChar == -1)
    {
        cchWideChar = Silent_PAL_wcslen(lpWideCharStr) + 1;
    }

    if (cbMultiByte == 0)
    {
        retval = cchWideChar;
        goto EXIT;
    }
    else if(cbMultiByte < cchWideChar)
    {
        retval = 0;
        goto EXIT;
    }
    
    retval = cchWideChar;
    while(cchWideChar > 0)
    {
        if(*lpWideCharStr > 255)
        {
            *lpMultiByteStr = '?';
        }
        else
        {
            *lpMultiByteStr = (unsigned char)*lpWideCharStr;
        }
        lpMultiByteStr++;
        lpWideCharStr++;
        cchWideChar--;
    }

EXIT:
    return retval;
}

/*******************************************************************************
Function:
  Internal_ExtractFormatA (silent version)
  
  see Internal_ExtractFormatA function in printf.c
*******************************************************************************/
BOOL Silent_ExtractFormatA(LPCSTR *Fmt, LPSTR Out, LPINT Flags,
                                    LPINT Width, LPINT Precision,
                                    LPINT Prefix, LPINT Type)
{
    BOOL Result = FALSE;
    LPSTR TempStr;
    LPSTR TempStrPtr;

    *Width = -1;
    *Precision = -1;
    *Flags = 0;
    *Prefix = -1;
    *Type = -1;

    if (*Fmt && **Fmt == '%')
    {
        *Out++ = *(*Fmt)++;
    }
    else
    {
        return Result;
    }

    /* we'll never need a temp string longer than the original */
    TempStrPtr = TempStr = (LPSTR) PAL_malloc(strlen(*Fmt)+1);
    if (!TempStr)
    {
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
            SetLastError(ERROR_INTERNAL_ERROR);
            return Result;
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
                SetLastError(ERROR_INTERNAL_ERROR);
                return Result;
            }
        }
        else if (**Fmt == '*')
        {
            *Precision = PRECISION_STAR;
            *Out++ = *(*Fmt)++;
            if (isdigit((unsigned char) **Fmt))
            {
                /* this is an invalid precision because we have a .* then a
                   number */
                /* printf handles this by just printing the whole string */
                *Precision = PRECISION_INVALID;
                while (isdigit((unsigned char) **Fmt))
                {
                    *Out++ = *(*Fmt)++;
                }
            }
            else
            {
                *Precision = PRECISION_STAR;
            }
        }
        else
        {
            *Precision = PRECISION_DOT;
        }
    }

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
    /* grab prefix of 'l' or the undocumented 'w' (at least in MSDN) */
    else if (**Fmt == 'l' || **Fmt == 'w')
    {
        *Prefix = PFF_PREFIX_LONG;
        ++(*Fmt);
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
    /* grab int types types */
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
            /* native *printf does not support %I64p
               (actually %llp), so we need to cheat a little bit */
            *Out++ = 'l';
            *Out++ = 'l';
        }
        (*Fmt)++;
        *Out++ = '.';
        *Out++ = '8';
        *Out++ = 'X';
        Result = TRUE;
    }

    *Out = 0;  /* end the string */
    PAL_free(TempStr);
    return Result;
}

/*******************************************************************************
Function:
  AddPaddingVfprintf (silent version)
    see Internal_AddPaddingVfprintf in printf.c
*******************************************************************************/
INT Silent_AddPaddingVfprintf(PAL_FILE *stream, LPSTR In, INT Padding, INT Flags)
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
    Out = (LPSTR) PAL_malloc(Length+1);
    int iLen = Length+1;
    if (!Out)
    {
        return -1;
    }
    OutOriginal = Out;

    if (Flags & PFF_MINUS) /* pad on right */
    {
        if (strcpy_s(Out, iLen, In) != SAFECRT_SUCCESS)
        {
            Written = -1;
            goto Done;
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
        if (strcpy_s(Out, Length+1, In) != SAFECRT_SUCCESS)
        {
            Written = -1;
            goto Done;
        }

        Out += LengthInStr;
        iLen -= LengthInStr;
    }

#if FILE_OPS_CHECK_FERROR_OF_PREVIOUS_CALL
    clearerr((FILE*)stream);
#endif

    Written = PAL_fwrite(OutOriginal, 1, Length, stream);
    if (stream->PALferrorCode == PAL_FILE_ERROR)
    {
        Written = -1;
    }

Done:
    PAL_free(OutOriginal);
    return Written;
}

/*++
Function:
  PAL_wcslen (silent version)

See MSDN or the man page for wcslen.

--*/
size_t Silent_PAL_wcslen(const wchar_16 *string)
{
    size_t nChar = 0;

    if ( !string )
    {
        return 0;
    }
    while (*string++)
    {
        nChar++;
    }    
    
    return nChar;
}

