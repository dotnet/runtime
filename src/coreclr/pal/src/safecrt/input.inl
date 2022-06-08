// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*input.c - C formatted input, used by scanf, etc.
*

*
*Purpose:
*       defines _input() to do formatted input; called from scanf(),
*       etc. functions.  This module defines _cscanf() instead when
*       CPRFLAG is defined.  The file cscanf.c defines that symbol
*       and then includes this file in order to implement _cscanf().
*
*Note:
*       this file is included in safecrt.lib build directly, plese refer
*       to safecrt_[w]input_s.c
*
*******************************************************************************/


#define ALLOW_RANGE /* enable "%[a-z]"-style scansets */


/* temporary work-around for compiler without 64-bit support */

#ifndef _INTEGRAL_MAX_BITS
#define _INTEGRAL_MAX_BITS  64
#endif  /* _INTEGRAL_MAX_BITS */

// typedef __int64_t __int64;

#ifndef FALSE
#define FALSE 0
#endif

#ifndef TRUE
#define TRUE 1
#endif

#define UNALIGNED

#define _CVTBUFSIZE (309+40) /* # of digits in max. dp value + slop */

#define _MBTOWC(x,y,z) _minimal_chartowchar( x, y )

#define _istspace(x)    isspace((unsigned char)x)

#define _malloc_crt PAL_malloc
#define _realloc_crt PAL_realloc
#define _free_crt PAL_free

#define _FASSIGN(flag, argument, number, dec_point, locale) _safecrt_fassign((flag), (argument), (number))
#define _WFASSIGN(flag, argument, number, dec_point, locale) _safecrt_wfassign((flag), (argument), (number))

#if defined (UNICODE)
#define ALLOC_TABLE 1
#else  /* defined (UNICODE) */
#define ALLOC_TABLE 0
#endif  /* defined (UNICODE) */

#define HEXTODEC(chr)   _hextodec(chr)

#define LEFT_BRACKET    ('[' | ('a' - 'A')) /* 'lowercase' version */

static int __cdecl _hextodec(_TCHAR);
#ifdef CPRFLAG

#define INC()           (++charcount, _inc())
#define UN_INC(chr)     (--charcount, _un_inc(chr))
#define EAT_WHITE()     _whiteout(&charcount)

static int __cdecl _inc(void);
static void __cdecl _un_inc(int);
static int __cdecl _whiteout(int *);

#else  /* CPRFLAG */

#define INC()           (++charcount, _inc(stream))
#define UN_INC(chr)     (--charcount, _un_inc(chr, stream))
#define EAT_WHITE()     _whiteout(&charcount, stream)

static int __cdecl _inc(miniFILE *);
static void __cdecl _un_inc(int, miniFILE *);
static int __cdecl _whiteout(int *, miniFILE *);

#endif  /* CPRFLAG */

#undef _ISDIGIT
#undef _ISXDIGIT

#ifndef _UNICODE
#define _ISDIGIT(chr)   isdigit((unsigned char)chr)
#define _ISXDIGIT(chr)  isxdigit((unsigned char)chr)
#else  /* _UNICODE */
#define _ISDIGIT(chr)   ( !(chr & 0xff00) && isdigit( ((chr) & 0x00ff) ) )
#define _ISXDIGIT(chr)  ( !(chr & 0xff00) && isxdigit( ((chr) & 0x00ff) ) )
#endif  /* _UNICODE */

#define MUL10(x)        ( (((x)<<2) + (x))<<1 )


#define LONGLONG_IS_INT64 1     /* 1 means long long is same as int64
                                   0 means long long is same as long */

/***
*  int __check_float_string(size_t,size_t *, _TCHAR**, _TCHAR*, int*)
*
*  Purpose:
*       Check if there is enough space insert onemore character in the given
*       block, if not then allocate more memory.
*
*  Return:
*       FALSE if more memory needed and the reallocation failed.
*
*******************************************************************************/

static int __check_float_string(size_t nFloatStrUsed,
                                size_t *pnFloatStrSz,
                                _TCHAR **pFloatStr,
                                _TCHAR *floatstring,
                                int *pmalloc_FloatStrFlag)
{
    void *tmpPointer;
    _ASSERTE(nFloatStrUsed<=(*pnFloatStrSz));
    if (nFloatStrUsed==(*pnFloatStrSz))
    {
        size_t newSize;

        // Will (*pnFloatStrSz) * 2 * sizeof(_TCHAR) overflow?
        if ( *pnFloatStrSz > (SIZE_T_MAX / 2 / sizeof(_TCHAR)))
        {
            return FALSE;
        }

        newSize = *pnFloatStrSz * 2 * sizeof(_TCHAR);

        if ((*pFloatStr)==floatstring)
        {
            if (((*pFloatStr)=(_TCHAR *)_malloc_crt(newSize))==NULL)
            {
                return FALSE;
            }

            (*pmalloc_FloatStrFlag)=1;

            memcpy((*pFloatStr),floatstring,(*pnFloatStrSz)*sizeof(_TCHAR));
            (*pnFloatStrSz)*=2;
        }
        else
        {
            if ((tmpPointer=(_TCHAR *)_realloc_crt((*pFloatStr), newSize))==NULL)
            {
                return FALSE;
            }
            (*pFloatStr)=(_TCHAR *)(tmpPointer);
            (*pnFloatStrSz)*=2;
        }
    }
    return TRUE;
}


#define ASCII       32           /* # of bytes needed to hold 256 bits */

#define SCAN_SHORT     0         /* also for FLOAT */
#define SCAN_LONG      1         /* also for DOUBLE */
#define SCAN_L_DOUBLE  2         /* only for LONG DOUBLE */

#define SCAN_NEAR    0
#define SCAN_FAR     1

#ifndef _UNICODE
#define TABLESIZE    ASCII
#else  /* _UNICODE */
#define TABLESIZE    (ASCII * 256)
#endif  /* _UNICODE */


/***
*int _input(stream, format, arglist), static int input(format, arglist)
*
*Purpose:
*   get input items (data items or literal matches) from the input stream
*   and assign them if appropriate to the items thru the arglist. this
*   function is intended for internal library use only, not for the user
*
*   The _input entry point is for the normal scanf() functions
*   The input entry point is used when compiling for _cscanf() [CPRFLAF
*   defined] and is a static function called only by _cscanf() -- reads from
*   console.
*
*   This code also defines _input_s, which works differently for %c, %s & %[.
*   For these, _input_s first picks up the next argument from the variable
*   argument list & uses it as the maximum size of the character array pointed
*   to by the next argument in the list.
*
*Entry:
*   FILE *stream - file to read from
*   char *format - format string to determine the data to read
*   arglist - list of pointer to data items
*
*Exit:
*   returns number of items assigned and fills in data items
*   returns EOF if error or EOF found on stream before 1st data item matched
*
*Exceptions:
*
*******************************************************************************/

    #define _INTRN_LOCALE_CONV( x ) localeconv()

#ifndef _UNICODE
        int __cdecl __tinput_s (miniFILE* stream, const _TUCHAR* format, va_list arglist)
#else
        int __cdecl __twinput_s (miniFILE* stream, const _TUCHAR* format, va_list arglist)
#endif  /* _UNICODE */
{
    _TCHAR floatstring[_CVTBUFSIZE + 1];
    _TCHAR *pFloatStr=floatstring;
    size_t nFloatStrUsed=0;
    size_t nFloatStrSz=sizeof(floatstring)/sizeof(floatstring[0]);
    int malloc_FloatStrFlag=0;

    unsigned long number;               /* temp hold-value                   */
#if ALLOC_TABLE
    char *table = NULL;                 /* which chars allowed for %[]       */
    int malloc_flag = 0;                /* is "table" allocated on the heap? */
#else  /* ALLOC_TABLE */
    char AsciiTable[TABLESIZE];
    char *table = AsciiTable;
#endif  /* ALLOC_TABLE */

#if _INTEGRAL_MAX_BITS >= 64
    uint64_t num64 = 0LL;             /* temp for 64-bit integers          */
#endif  /* _INTEGRAL_MAX_BITS >= 64    */
    void *pointer=NULL;                 /* points to user data receptacle    */
    void *start;                        /* indicate non-empty string         */


#ifndef _UNICODE
    char16_t wctemp=L'\0';
#endif  /* _UNICODE */
    _TUCHAR *scanptr;                   /* for building "table" data         */
    int ch = 0;
    int charcount;                      /* total number of chars read        */
    int comchr;                        /* holds designator type             */
    int count;                          /* return value.  # of assignments   */

    int started;                        /* indicate good number              */
    int width;                          /* width of field                    */
    int widthset;                       /* user has specified width          */
#ifdef _SECURE_SCANF
    size_t array_width = 0;
    size_t original_array_width = 0;
    int enomem = 0;
    int format_error = FALSE;
#endif  /* _SECURE_SCANF */

/* Neither coerceshort nor farone are need for the 386 */


    char done_flag;                     /* general purpose loop monitor      */
    char longone;                       /* 0 = SHORT, 1 = LONG, 2 = L_DOUBLE */
#if _INTEGRAL_MAX_BITS >= 64
    int integer64;                      /* 1 for 64-bit integer, 0 otherwise */
#endif  /* _INTEGRAL_MAX_BITS >= 64    */
    signed char widechar;               /* -1 = char, 0 = ????, 1 = char16_t  */
    char reject;                        /* %[^ABC] instead of %[ABC]         */
    char negative;                      /* flag for '-' detected             */
    char suppress;                      /* don't assign anything             */
    char match;                         /* flag: !0 if any fields matched    */
    va_list arglistsave;                /* save arglist value                */

    char fl_wchar_arg;                  /* flags wide char/string argument   */

    _TCHAR decimal;


    _TUCHAR rngch;
    _TUCHAR last;
    _TUCHAR prevchar;
    _TCHAR tch;

    _VALIDATE_RETURN( (format != NULL), EINVAL, EOF);

#ifndef CPRFLAG
    _VALIDATE_RETURN( (stream != NULL), EINVAL, EOF);
#endif  /* CPRFLAG */

    /*
    count = # fields assigned
    charcount = # chars read
    match = flag indicating if any fields were matched

    [Note that we need both count and match.  For example, a field
    may match a format but have assignments suppressed.  In this case,
    match will get set, but 'count' will still equal 0.  We need to
    distinguish 'match vs no-match' when terminating due to EOF.]
    */

    count = charcount = match = 0;

    while (*format) {

        if (_istspace((_TUCHAR)*format)) {

            UN_INC(EAT_WHITE()); /* put first non-space char back */

            do {
                tch = *++format;
            } while (_istspace((_TUCHAR)tch));

            continue;

        }

        if (_T('%') == *format) {

            number = 0;
            prevchar = 0;
            width = widthset = started = 0;
#ifdef _SECURE_SCANF
            original_array_width = array_width = 0;
            enomem = 0;
#endif  /* _SECURE_SCANF */
            fl_wchar_arg = done_flag = suppress = negative = reject = 0;
            widechar = 0;

            longone = 1;

#if _INTEGRAL_MAX_BITS >= 64
            integer64 = 0;
#endif  /* _INTEGRAL_MAX_BITS >= 64    */

            while (!done_flag) {

                comchr = *++format;
                if (_ISDIGIT((_TUCHAR)comchr)) {
                    ++widthset;
                    width = MUL10(width) + (comchr - _T('0'));
                } else
                    switch (comchr) {
                        case _T('F') :
                        case _T('N') :   /* no way to push NEAR in large model */
                            break;  /* NEAR is default in small model */
                        case _T('h') :
                            /* set longone to 0 */
                            --longone;
                            --widechar;         /* set widechar = -1 */
                            break;

#if _INTEGRAL_MAX_BITS >= 64
                        case _T('I'):
                            if ( (*(format + 1) == _T('6')) &&
                                 (*(format + 2) == _T('4')) )
                            {
                                format += 2;
                                ++integer64;
                                num64 = 0;
                                break;
                            }
                            else if ( (*(format + 1) == _T('3')) &&
                                      (*(format + 2) == _T('2')) )
                            {
                                format += 2;
                                break;
                            }
                            else if ( (*(format + 1) == _T('d')) ||
                                      (*(format + 1) == _T('i')) ||
                                      (*(format + 1) == _T('o')) ||
                                      (*(format + 1) == _T('x')) ||
                                      (*(format + 1) == _T('X')) )
                            {
                                if (sizeof(void*) == sizeof(__int64))
                                {
                                    ++integer64;
                                    num64 = 0;
                                }
                                break;
                            }
                            if (sizeof(void*) == sizeof(__int64))
                            {
                                    ++integer64;
                                    num64 = 0;
                            }
                            goto DEFAULT_LABEL;
#endif  /* _INTEGRAL_MAX_BITS >= 64    */

                        case _T('L') :
                        /*  ++longone;  */
                            ++longone;
                            break;

                        case _T('q'):
                            ++integer64;
                            num64 = 0;
                            break;

                        case _T('l') :
                            if (*(format + 1) == _T('l'))
                            {
                                ++format;
#ifdef LONGLONG_IS_INT64
                                ++integer64;
                                num64 = 0;
                                break;
#else  /* LONGLONG_IS_INT64 */
                                ++longone;
                                /* NOBREAK */
#endif  /* LONGLONG_IS_INT64 */
                            }
                            else
                            {
                                ++longone;
                                /* NOBREAK */
                            }
                            FALLTHROUGH;
                        case _T('w') :
                            ++widechar;         /* set widechar = 1 */
                            break;

                        case _T('*') :
                            ++suppress;
                            break;

                        default:
DEFAULT_LABEL:
                            ++done_flag;
                            break;
                    }
            }

            if (!suppress) {
                va_copy(arglistsave, arglist);
                pointer = va_arg(arglist,void *);
            } else {
                pointer = NULL;         // doesn't matter what value we use here - we're only using it as a flag
            }

            done_flag = 0;

            if (!widechar) {    /* use case if not explicitly specified */
                if ((*format == _T('S')) || (*format == _T('C')))
#ifdef _UNICODE
                    --widechar;
                else
                    ++widechar;
#else  /* _UNICODE */
                    ++widechar;
                else
                    --widechar;
#endif  /* _UNICODE */
            }

            /* switch to lowercase to allow %E,%G, and to
               keep the switch table small */

            comchr = *format | (_T('a') - _T('A'));

            if (_T('n') != comchr)
            {
                if (_T('c') != comchr && LEFT_BRACKET != comchr)
                    ch = EAT_WHITE();
                else
                    ch = INC();
            }

            if (_T('n') != comchr)
            {
                if (_TEOF == ch)
                    goto error_return;
            }

            if (!widthset || width) {

#ifdef _SECURE_SCANF
                if(!suppress && (comchr == _T('c') || comchr == _T('s') || comchr == LEFT_BRACKET)) {

                    va_copy(arglist, arglistsave);

                    /* Reinitialize pointer to point to the array to which we write the input */
                    pointer = va_arg(arglist, void*);

                    va_copy(arglistsave, arglist);

                    /* Get the next argument - size of the array in characters */
#ifdef HOST_64BIT
                    original_array_width = array_width = (size_t)(va_arg(arglist, unsigned int));
#else  /* HOST_64BIT */
                    original_array_width = array_width = va_arg(arglist, size_t);
#endif  /* HOST_64BIT */

                    if(array_width < 1) {
                        if (widechar > 0)
                            *(char16_t UNALIGNED *)pointer = L'\0';
                        else
                            *(char *)pointer = '\0';

                        errno = ENOMEM;

                        goto error_return;
                    }
                }
#endif  /* _SECURE_SCANF */
                switch(comchr) {

                    case _T('c'):
                /*  case _T('C'):  */
                        if (!widthset) {
                            ++widthset;
                            ++width;
                        }
                        if (widechar > 0)
                            fl_wchar_arg++;
                        goto scanit;


                    case _T('s'):
                /*  case _T('S'):  */
                        if(widechar > 0)
                            fl_wchar_arg++;
                        goto scanit;


                    case LEFT_BRACKET :   /* scanset */
                        if (widechar>0)
                            fl_wchar_arg++;
                        scanptr = (_TUCHAR *)(++format);

                        if (_T('^') == *scanptr) {
                            ++scanptr;
                            --reject; /* set reject to 255 */
                        }

                        /* Allocate "table" on first %[] spec */
#if ALLOC_TABLE
                        if (table == NULL) {
                            table = (char*)_malloc_crt(TABLESIZE);
                            if ( table == NULL)
                                goto error_return;
                            malloc_flag = 1;
                        }
#endif  /* ALLOC_TABLE */
                        memset(table, 0, TABLESIZE);


                        if (LEFT_BRACKET == comchr)
                            if (_T(']') == *scanptr) {
                                prevchar = _T(']');
                                ++scanptr;

                                table[ _T(']') >> 3] = 1 << (_T(']') & 7);

                            }

                        while (_T(']') != *scanptr) {

                            rngch = *scanptr++;

                            if (_T('-') != rngch ||
                                 !prevchar ||           /* first char */
                                 _T(']') == *scanptr) /* last char */

                                table[(prevchar = rngch) >> 3] |= 1 << (rngch & 7);

                            else {  /* handle a-z type set */

                                rngch = *scanptr++; /* get end of range */

                                if (prevchar < rngch)  /* %[a-z] */
                                    last = rngch;
                                else {              /* %[z-a] */
                                    last = prevchar;
                                    prevchar = rngch;
                                }
                                for (rngch = prevchar; rngch <= last; ++rngch)
                                    table[rngch >> 3] |= 1 << (rngch & 7);

                                prevchar = 0;

                            }
                        }


                        if (!*scanptr)
                            goto error_return;      /* trunc'd format string */

                        /* scanset completed.  Now read string */

                        if (LEFT_BRACKET == comchr)
                            format = scanptr;

scanit:
                        start = pointer;

                        /*
                         * execute the format directive. that is, scan input
                         * characters until the directive is fulfilled, eof
                         * is reached, or a non-matching character is
                         * encountered.
                         *
                         * it is important not to get the next character
                         * unless that character needs to be tested! other-
                         * wise, reads from line-buffered devices (e.g.,
                         * scanf()) would require an extra, spurious, newline
                         * if the first newline completes the current format
                         * directive.
                         */
                        UN_INC(ch);

#ifdef _SECURE_SCANF
                        /* One element is needed for '\0' for %s & %[ */
                        if(comchr != _T('c')) {
                            --array_width;
                        }
#endif  /* _SECURE_SCANF */
                        while ( !widthset || width-- ) {

                            ch = INC();
                            if (
#ifndef CPRFLAG
                                 (_TEOF != ch) &&
#endif  /* CPRFLAG */
                                   // char conditions
                                 ( ( comchr == _T('c')) ||
                                   // string conditions !isspace()
                                   ( ( comchr == _T('s') &&
                                       (!(ch >= _T('\t') && ch <= _T('\r')) &&
                                       ch != _T(' ')))) ||
                                   // BRACKET conditions
                                   ( (comchr == LEFT_BRACKET) &&
                                     ((table[ch >> 3] ^ reject) & (1 << (ch & 7)))
                                     )
                                   )
                                )
                            {
                                if (!suppress) {
#ifdef _SECURE_SCANF
                                    if(!array_width) {
                                        /* We have exhausted the user's buffer */

                                        enomem = 1;
                                        break;
                                    }
#endif  /* _SECURE_SCANF */
#ifndef _UNICODE
                                    if (fl_wchar_arg) {
                                        wctemp = W('?');
                                        char temp[2];
                                        temp[0] = (char) ch;
#if 0       // we are not supporting multibyte input strings
                                        if (isleadbyte((unsigned char)ch))
                                        {
                                            temp[1] = (char) INC();
                                        }
#endif  /* 0 */
                                        _MBTOWC(&wctemp, temp, MB_CUR_MAX);
                                        *(char16_t UNALIGNED *)pointer = wctemp;
                                        /* just copy W('?') if mbtowc fails, errno is set by mbtowc */
                                        pointer = (char16_t *)pointer + 1;
#ifdef _SECURE_SCANF
                                        --array_width;
#endif  /* _SECURE_SCANF */
                                    } else
#else  /* _UNICODE */
                                    if (fl_wchar_arg) {
                                        *(char16_t UNALIGNED *)pointer = (char16_t)ch;
                                        pointer = (char16_t *)pointer + 1;
#ifdef _SECURE_SCANF
                                        --array_width;
#endif  /* _SECURE_SCANF */
                                    } else
#endif  /* _UNICODE */
                                    {
#ifndef _UNICODE
                                    *(char *)pointer = (char)ch;
                                    pointer = (char *)pointer + 1;
#ifdef _SECURE_SCANF
                                    --array_width;
#endif  /* _SECURE_SCANF */
#else  /* _UNICODE */
                                    int temp = 0;
#ifndef _SECURE_SCANF
                                    /* convert wide to multibyte */
                                    if (_ERRCHECK_EINVAL_ERANGE(wctomb_s(&temp, (char *)pointer, MB_LEN_MAX, ch)) == 0)
                                    {
                                        /* do nothing if wctomb fails, errno will be set to EILSEQ */
                                        pointer = (char *)pointer + temp;
                                    }
#else  /* _SECURE_SCANF */
                                    /* convert wide to multibyte */
                                    if (array_width >= ((size_t)MB_CUR_MAX))
                                    {
                                        temp = wctomb((char *)pointer, ch);
                                    }
                                    else
                                    {
                                        char tmpbuf[MB_LEN_MAX];
                                        temp = wctomb(tmpbuf, ch);
                                        if (temp > 0 && ((size_t)temp) > array_width)
                                        {
                                            /* We have exhausted the user's buffer */
                                            enomem = 1;
                                            break;
                                        }
                                        memcpy(pointer, tmpbuf, temp);
                                    }
                                    if (temp > 0)
                                    {
                                        /* do nothing if wctomb fails, errno will be set to EILSEQ */
                                        pointer = (char *)pointer + temp;
                                        array_width -= temp;
                                    }
#endif  /* _SECURE_SCANF */
#endif  /* _UNICODE */
                                    }
                                } /* suppress */
                                else {
                                    /* just indicate a match */
                                    start = (_TCHAR *)start + 1;
                                }
                            }
                            else  {
                                UN_INC(ch);
                                break;
                            }
                        }

                        /* make sure something has been matched and, if
                           assignment is not suppressed, null-terminate
                           output string if comchr != c */

#ifdef _SECURE_SCANF
                        if(enomem) {
                            errno = ENOMEM;
                            /* In case of error, blank out the input buffer */
                            if (fl_wchar_arg)
                            {
                                _RESET_STRING(((char16_t UNALIGNED *)start), original_array_width);
                            }
                            else
                            {
                                _RESET_STRING(((char *)start), original_array_width);
                            }

                            goto error_return;
                        }
#endif  /* _SECURE_SCANF */

                        if (start != pointer) {
                            if (!suppress) {
                                ++count;
                                if ('c' != comchr) /* null-terminate strings */
                                {
                                    if (fl_wchar_arg)
                                    {
                                        *(char16_t UNALIGNED *)pointer = L'\0';
#ifdef _SECURE_SCANF
                                        _FILL_STRING(((char16_t UNALIGNED *)start), original_array_width,
                                            ((char16_t UNALIGNED *)pointer - (char16_t UNALIGNED *)start + 1))
#endif  /* _SECURE_SCANF */
                                    }
                                    else
                                    {
                                        *(char *)pointer = '\0';
#ifdef _SECURE_SCANF
                                        _FILL_STRING(((char *)start), original_array_width,
                                            ((char *)pointer - (char *)start + 1))
#endif  /* _SECURE_SCANF */
                                    }
                                }
                            }
                            else
                            {
                                // supress set, do nothing
                            }
                        }
                        else
                            goto error_return;

                        break;

                    case _T('i') :      /* could be d, o, or x */

                        comchr = _T('d'); /* use as default */
                        FALLTHROUGH;

                    case _T('x'):

                        if (_T('-') == ch) {
                            ++negative;

                            goto x_incwidth;

                        } else if (_T('+') == ch) {
x_incwidth:
                            if (!--width && widthset)
                                ++done_flag;
                            else
                                ch = INC();
                        }

                        if (_T('0') == ch) {

                            if (_T('x') == (_TCHAR)(ch = INC()) || _T('X') == (_TCHAR)ch) {
                                ch = INC();
                                if (widthset) {
                                    width -= 2;
                                    if (width < 1)
                                        ++done_flag;
                                }
                                comchr = _T('x');
                            } else {
                                ++started;
                                if (_T('x') != comchr) {
                                    if (widthset && !--width)
                                        ++done_flag;
                                    comchr = _T('o');
                                }
                                else {
                                    /* scanning a hex number that starts */
                                    /* with a 0. push back the character */
                                    /* currently in ch and restore the 0 */
                                    UN_INC(ch);
                                    ch = _T('0');
                                }
                            }
                        }
                        goto getnum;

                        /* NOTREACHED */

                    case _T('p') :
                        /* force %hp to be treated as %p */
                        longone = 1;
#ifdef HOST_64BIT
                        /* force %p to be 64 bit in WIN64 */
                        ++integer64;
                        num64 = 0;
#endif  /* HOST_64BIT */
                        FALLTHROUGH;
                    case _T('o') :
                    case _T('u') :
                    case _T('d') :

                        if (_T('-') == ch) {
                            ++negative;

                            goto d_incwidth;

                        } else if (_T('+') == ch) {
d_incwidth:
                            if (!--width && widthset)
                                ++done_flag;
                            else
                                ch = INC();
                        }

getnum:
#if _INTEGRAL_MAX_BITS >= 64
                        if ( integer64 ) {

                            while (!done_flag) {

                                if (_T('x') == comchr || _T('p') == comchr)

                                    if (_ISXDIGIT(ch)) {
                                        num64 <<= 4;
                                        ch = _hextodec((_TCHAR)ch);
                                    }
                                    else
                                        ++done_flag;

                                else if (_ISDIGIT(ch))

                                    if (_T('o') == comchr)
                                        if (_T('8') > ch)
                                                num64 <<= 3;
                                        else {
                                                ++done_flag;
                                        }
                                    else /* _T('d') == comchr */
                                        num64 = MUL10(num64);

                                else
                                    ++done_flag;

                                if (!done_flag) {
                                    ++started;
                                    num64 += ch - _T('0');

                                    if (widthset && !--width)
                                        ++done_flag;
                                    else
                                        ch = INC();
                                } else
                                    UN_INC(ch);

                            } /* end of WHILE loop */

                            if (negative)
                                num64 = (uint64_t )(-(__int64)num64);
                        }
                        else {
#endif  /* _INTEGRAL_MAX_BITS >= 64    */
                            while (!done_flag) {

                                if (_T('x') == comchr || _T('p') == comchr)

                                    if (_ISXDIGIT(ch)) {
                                        number = (number << 4);
                                        ch = _hextodec((_TCHAR)ch);
                                    }
                                    else
                                        ++done_flag;

                                else if (_ISDIGIT(ch))

                                    if (_T('o') == comchr)
                                        if (_T('8') > ch)
                                            number = (number << 3);
                                        else {
                                            ++done_flag;
                                        }
                                    else /* _T('d') == comchr */
                                        number = MUL10(number);

                                else
                                    ++done_flag;

                                if (!done_flag) {
                                    ++started;
                                    number += ch - _T('0');

                                    if (widthset && !--width)
                                        ++done_flag;
                                    else
                                        ch = INC();
                                } else
                                    UN_INC(ch);

                            } /* end of WHILE loop */

                            if (negative)
                                number = (unsigned long)(-(long)number);
#if _INTEGRAL_MAX_BITS >= 64
                        }
#endif  /* _INTEGRAL_MAX_BITS >= 64    */
                        if (_T('F')==comchr) /* expected ':' in long pointer */
                            started = 0;

                        if (started)
                            if (!suppress) {

                                ++count;
assign_num:
#if _INTEGRAL_MAX_BITS >= 64
                                if ( integer64 )
                                    *(__int64 UNALIGNED *)pointer = ( uint64_t )num64;
                                else
#endif  /* _INTEGRAL_MAX_BITS >= 64    */
                                if (longone)
                                    *(int UNALIGNED *)pointer = (unsigned int)number;
                                else
                                    *(short UNALIGNED *)pointer = (unsigned short)number;

                            } else /*NULL*/;
                        else
                            goto error_return;

                        break;

                    case _T('n') :      /* char count, don't inc return value */
                        number = charcount;
                        if(!suppress)
                            goto assign_num; /* found in number code above */
                        break;


                    case _T('e') :
                 /* case _T('E') : */
                    case _T('f') :
                    case _T('g') : /* scan a float */
                 /* case _T('G') : */
                        nFloatStrUsed=0;

                        if (_T('-') == ch) {
                            pFloatStr[nFloatStrUsed++] = _T('-');
                            goto f_incwidth;

                        } else if (_T('+') == ch) {
f_incwidth:
                            --width;
                            ch = INC();
                        }

                        if (!widthset)              /* must watch width */
                            width = -1;


                        /* now get integral part */

                        while (_ISDIGIT(ch) && width--) {
                            ++started;
                            pFloatStr[nFloatStrUsed++] = (char)ch;
                            if (__check_float_string(nFloatStrUsed,
                                                     &nFloatStrSz,
                                                     &pFloatStr,
                                                     floatstring,
                                                     &malloc_FloatStrFlag
                                                     )==FALSE) {
                                goto error_return;
                            }
                            ch = INC();
                        }

#ifdef _UNICODE
                        /* convert decimal point to wide-char */
                        /* if mbtowc fails (should never happen), we use L'.' */
                        decimal = L'.';
                        _MBTOWC(&decimal, _INTRN_LOCALE_CONV(_loc_update)->decimal_point, MB_CUR_MAX);
#else  /* _UNICODE */

                        decimal=*((_INTRN_LOCALE_CONV(_loc_update))->decimal_point);
#endif  /* _UNICODE */

                        /* now check for decimal */
                        if (decimal == (char)ch && width--) {
                            ch = INC();
                            pFloatStr[nFloatStrUsed++] = decimal;
                            if (__check_float_string(nFloatStrUsed,
                                                     &nFloatStrSz,
                                                     &pFloatStr,
                                                     floatstring,
                                                     &malloc_FloatStrFlag
                                                     )==FALSE) {
                                goto error_return;
                            }

                            while (_ISDIGIT(ch) && width--) {
                                ++started;
                                pFloatStr[nFloatStrUsed++] = (_TCHAR)ch;
                                if (__check_float_string(nFloatStrUsed,
                                                         &nFloatStrSz,
                                                         &pFloatStr,
                                                         floatstring,
                                                         &malloc_FloatStrFlag
                                                         )==FALSE) {
                                    goto error_return;
                                }
                                ch = INC();
                            }
                        }

                        /* now check for exponent */

                        if (started && (_T('e') == ch || _T('E') == ch) && width--) {
                            pFloatStr[nFloatStrUsed++] = _T('e');
                            if (__check_float_string(nFloatStrUsed,
                                                     &nFloatStrSz,
                                                     &pFloatStr,
                                                     floatstring,
                                                     &malloc_FloatStrFlag
                                                     )==FALSE) {
                                goto error_return;
                            }

                            if (_T('-') == (ch = INC())) {

                                pFloatStr[nFloatStrUsed++] = _T('-');
                                if (__check_float_string(nFloatStrUsed,
                                                         &nFloatStrSz,
                                                         &pFloatStr,
                                                         floatstring,
                                                         &malloc_FloatStrFlag
                                                         )==FALSE) {
                                    goto error_return;
                                }
                                goto f_incwidth2;

                            } else if (_T('+') == ch) {
f_incwidth2:
                                if (!width--)
                                    ++width;
                                else
                                    ch = INC();
                            }


                            while (_ISDIGIT(ch) && width--) {
                                ++started;
                                pFloatStr[nFloatStrUsed++] = (_TCHAR)ch;
                                if (__check_float_string(nFloatStrUsed,
                                                         &nFloatStrSz,
                                                         &pFloatStr,
                                                         floatstring,
                                                         &malloc_FloatStrFlag
                                                         )==FALSE) {
                                    goto error_return;
                                }
                                ch = INC();
                            }

                        }

                        UN_INC(ch);

                        if (started)
                            if (!suppress) {
                                ++count;
                                pFloatStr[nFloatStrUsed]= _T('\0');
#ifdef _UNICODE
                                _WFASSIGN( longone-1, pointer, pFloatStr, (char)decimal, _loc_update.GetLocaleT());
#else  /* _UNICODE */
                                _FASSIGN( longone-1, pointer, pFloatStr, (char)decimal, _loc_update.GetLocaleT());
#endif  /* _UNICODE */
                            } else /*NULL */;
                        else
                            goto error_return;

                        break;


                    default:    /* either found '%' or something else */

                        if ((int)*format != (int)ch) {
                            UN_INC(ch);
#ifdef _SECURE_SCANF
                            /* error_return ASSERT's if format_error is true */
                                format_error = TRUE;
#endif  /* _SECURE_SCANF */
                            goto error_return;
                            }
                        else
                            match--; /* % found, compensate for inc below */

                        if (!suppress)
                            va_copy(arglist, arglistsave);

                } /* SWITCH */

                match++;        /* matched a format field - set flag */

            } /* WHILE (width) */

            else {  /* zero-width field in format string */
                UN_INC(ch);  /* check for input error */
                goto error_return;
            }

            ++format;  /* skip to next char */

        } else  /*  ('%' != *format) */
            {

            if ((int)*format++ != (int)(ch = INC()))
                {
                UN_INC(ch);
                goto error_return;
                }
#if 0       // we are not supporting multibyte input strings
#ifndef _UNICODE
            if (isleadbyte((unsigned char)ch))
                {
                int ch2;
                if ((int)*format++ != (ch2=INC()))
                    {
                    UN_INC(ch2);
                    UN_INC(ch);
                    goto error_return;
                    }

                    --charcount; /* only count as one character read */
                }
#endif  /* _UNICODE */
#endif
            }

#ifndef CPRFLAG
        if ( (_TEOF == ch) && ((*format != _T('%')) || (*(format + 1) != _T('n'))) )
            break;
#endif  /* CPRFLAG */

    }  /* WHILE (*format) */

error_return:
#if ALLOC_TABLE
    if (malloc_flag == 1)
    {
        _free_crt(table);
    }
#endif  /* ALLOC_TABLE */
    if (malloc_FloatStrFlag == 1)
    {
        _free_crt(pFloatStr);
    }

#ifndef CPRFLAG
    if (_TEOF == ch)
        /* If any fields were matched or assigned, return count */
        return ( (count || match) ? count : EOF);
    else
#endif  /* CPRFLAG */
#ifdef _SECURE_SCANF
        if(format_error == TRUE) {
            _VALIDATE_RETURN( ("Invalid Input Format" && 0), EINVAL, count);
        }
#endif  /* _SECURE_SCANF */
        return count;

}

/* _hextodec() returns a value of 0-15 and expects a char 0-9, a-f, A-F */
/* _inc() is the one place where we put the actual getc code. */
/* _whiteout() returns the first non-blank character, as defined by isspace() */

static int __cdecl _hextodec ( _TCHAR chr)
{
    return _ISDIGIT(chr) ? chr : (chr & ~(_T('a') - _T('A'))) - _T('A') + 10 + _T('0');
}

#ifdef CPRFLAG

static int __cdecl _inc(void)
{
    return (_gettche_nolock());
}

static void __cdecl _un_inc(int chr)
{
    if (_TEOF != chr) {
        _ungettch_nolock(chr);
    }
}

static int __cdecl _whiteout(REG1 int* counter)
{
    REG2 int ch;

    do
    {
        ++*counter;
        ch = _inc();

        if (ch == _TEOF)
        {
            break;
        }
    }
    while(_istspace((_TUCHAR)ch));
    return ch;
}

#else  /* CPRFLAG */

static int __cdecl _inc(miniFILE* fileptr)
{
    return (_gettc_nolock(fileptr));
}

static void __cdecl _un_inc(int chr, miniFILE* fileptr)
{
    if (_TEOF != chr) {
        _ungettc_nolock((char)chr,fileptr);
    }
}

static int __cdecl _whiteout(int* counter, miniFILE* fileptr)
{
    int ch;

    do
    {
        ++*counter;
        ch = _inc(fileptr);

        if (ch == _TEOF)
        {
            break;
        }
    }
    while(_istspace((_TUCHAR)ch));
    return ch;
}

#endif  /* CPRFLAG */
