// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*internal.h - contains declarations of internal routines and variables
*

*
*Purpose:
*       Declares routines and variables used internally by the C run-time.
*
*       [Internal]
*
****/

#if _MSC_VER > 1000
#pragma once
#endif  /* _MSC_VER > 1000 */

#ifndef _INC_INTERNAL
#define _INC_INTERNAL

#include <crtdefs.h>

#ifdef __cplusplus
extern "C" {
#endif  /* __cplusplus */

#include <cruntime.h>
#include <limits.h>

/*
 * Conditionally include windows.h to pick up the definition of
 * CRITICAL_SECTION.
 */
#include <windows.h>

#ifdef _MSC_VER
#pragma pack(push,_CRT_PACKING)
#endif  /* _MSC_VER */

/* Define function types used in several startup sources */

typedef void (__cdecl *_PVFV)(void);
typedef int  (__cdecl *_PIFV)(void);
typedef void (__cdecl *_PVFI)(int);

#if _MSC_VER >= 1400 && defined(_M_CEE)
typedef const void* (__clrcall *_PVFVM)(void);
typedef int (__clrcall *_PIFVM)(void);
typedef void (__clrcall *_CPVFV)(void);
#endif  /* _MSC_VER >= 1400 && defined(_M_CEE) */

#if defined (_M_CEE_PURE) || (defined (_DLL) && defined (_M_IX86))
/* Retained for compatibility with VC++ 5.0 and earlier versions */
_CRTIMP int * __cdecl __p__commode(void);
#endif  /* defined (_M_CEE_PURE) || (defined (_DLL) && defined (_M_IX86)) */
#if defined (SPECIAL_CRTEXE) && defined (_DLL)
        extern int _commode;
#else  /* defined (SPECIAL_CRTEXE) && defined (_DLL) */
#ifndef _M_CEE_PURE
_CRTIMP extern int _commode;
#else  /* _M_CEE_PURE */
#define _commode (*__p___commode())
#endif  /* _M_CEE_PURE */
#endif  /* defined (SPECIAL_CRTEXE) && defined (_DLL) */

#define __IOINFO_TM_ANSI    0   /* Regular Text */
#define __IOINFO_TM_UTF8    1   /* UTF8 Encoded */
#define __IOINFO_TM_UTF16LE 2   /* UTF16 Little Endian Encoded */

/*
 * Control structure for lowio file handles
 */
typedef struct {
        intptr_t osfhnd;    /* underlying OS file HANDLE */
        char osfile;        /* attributes of file (e.g., open in text mode?) */
        char pipech;        /* one char buffer for handles opened on pipes */
        int lockinitflag;
        CRITICAL_SECTION lock;
#ifndef _SAFECRT_IMPL
        /* Not used in the safecrt downlevel. We do not define them, so we cannot use them accidentally */
        char textmode : 7;     /* __IOINFO_TM_ANSI or __IOINFO_TM_UTF8 or __IOINFO_TM_UTF16LE */
        char unicode : 1;      /* Was the file opened as unicode? */
        char pipech2[2];       /* 2 more peak ahead chars for UNICODE mode */
#endif  /* _SAFECRT_IMPL */
    }   ioinfo;

/*
 * Definition of IOINFO_L2E, the log base 2 of the number of elements in each
 * array of ioinfo structs.
 */
#define IOINFO_L2E          5

/*
 * Definition of IOINFO_ARRAY_ELTS, the number of elements in ioinfo array
 */
#define IOINFO_ARRAY_ELTS   (1 << IOINFO_L2E)

/*
 * Definition of IOINFO_ARRAYS, maximum number of supported ioinfo arrays.
 */
#define IOINFO_ARRAYS       64

#define _NHANDLE_           (IOINFO_ARRAYS * IOINFO_ARRAY_ELTS)

#define _TZ_STRINGS_SIZE    64

/*
 * Access macros for getting at an ioinfo struct and its fields from a
 * file handle
 */
#define _pioinfo(i) ( __pioinfo[(i) >> IOINFO_L2E] + ((i) & (IOINFO_ARRAY_ELTS - \
                              1)) )
#define _osfhnd(i)  ( _pioinfo(i)->osfhnd )

#define _osfile(i)  ( _pioinfo(i)->osfile )

#define _pipech(i)  ( _pioinfo(i)->pipech )

#define _pipech2(i)  ( _pioinfo(i)->pipech2 )

#define _textmode(i) ( _pioinfo(i)->textmode )

#define _tm_unicode(i) ( _pioinfo(i)->unicode )

/*
 * Safer versions of the above macros. Currently, only _osfile_safe is
 * used.
 */
#define _pioinfo_safe(i)    ( (((i) != -1) && ((i) != -2)) ? _pioinfo(i) : &__badioinfo )

#define _osfhnd_safe(i)     ( _pioinfo_safe(i)->osfhnd )

#define _osfile_safe(i)     ( _pioinfo_safe(i)->osfile )

#define _pipech_safe(i)     ( _pioinfo_safe(i)->pipech )

#define _pipech2_safe(i)    ( _pioinfo_safe(i)->pipech2 )

#ifdef _SAFECRT_IMPL
/* safecrt does not have support for textmode, so we always return __IOINFO_TM_ANSI */
#define _textmode_safe(i)   __IOINFO_TM_ANSI
#define _tm_unicode_safe(i)  0
#else  /* _SAFECRT_IMPL */
#define _textmode_safe(i)   ( _pioinfo_safe(i)->textmode )
#define _tm_unicode_safe(i) ( _pioinfo_safe(i)->unicode )
#endif  /* _SAFECRT_IMPL */

#ifndef _M_CEE_PURE
#ifdef _SAFECRT_IMPL
/* We need to get this from the downlevel DLL, even when we build safecrt.lib */
extern __declspec(dllimport) ioinfo __badioinfo;
extern __declspec(dllimport) ioinfo * __pioinfo[];
#else  /* _SAFECRT_IMPL */
/*
 * Special, static ioinfo structure used only for more graceful handling
 * of a C file handle value of -1 (results from common errors at the stdio
 * level).
 */
extern _CRTIMP ioinfo __badioinfo;

/*
 * Array of arrays of control structures for lowio files.
 */
extern _CRTIMP ioinfo * __pioinfo[];
#endif  /* _SAFECRT_IMPL */
#endif  /* _M_CEE_PURE */

/*
 * Current number of allocated ioinfo structures (_NHANDLE_ is the upper
 * limit).
 */
extern int _nhandle;

int __cdecl _alloc_osfhnd(void);
int __cdecl _free_osfhnd(int);
int __cdecl _set_osfhnd(int, intptr_t);

/*
    fileno for stdout, stdin & stderr when there is no console
*/
#define _NO_CONSOLE_FILENO (intptr_t)-2


extern const char __dnames[];
extern const char __mnames[];

extern int _days[];
extern int _lpdays[];

extern __time32_t __cdecl __loctotime32_t(int, int, int, int, int, int, int);
extern __time64_t __cdecl __loctotime64_t(int, int, int, int, int, int, int);

extern void __cdecl __tzset(void);

extern int __cdecl _validdrive(unsigned);

/*
 * If we are only interested in years between 1901 and 2099, we could use this:
 *
 *      #define IS_LEAP_YEAR(y)  (y % 4 == 0)
 */

#define IS_LEAP_YEAR(y)  (((y) % 4 == 0 && (y) % 100 != 0) || (y) % 400 == 0)

/*
 * This variable is in the C start-up; the length must be kept synchronized
 * It is used by the *cenvarg.c modules
 */

extern char _acfinfo[]; /* "_C_FILE_INFO=" */

#define CFI_LENGTH  12  /* "_C_FILE_INFO" is 12 bytes long */


/*
 * stdio internals
 */
#ifndef _FILE_DEFINED
struct _iobuf {
        char *_ptr;
        int   _cnt;
        char *_base;
        int   _flag;
        int   _file;
        int   _charbuf;
        int   _bufsiz;
        char *_tmpfname;
        };
typedef struct _iobuf FILE;
#define _FILE_DEFINED
#endif  /* _FILE_DEFINED */

#if !defined (_FILEX_DEFINED) && defined (_WINDOWS_)

/*
 * Variation of FILE type used for the dynamically allocated portion of
 * __piob[]. For single thread, _FILEX is the same as FILE. For multithread
 * models, _FILEX has two fields: the FILE struct and the CRITICAL_SECTION
 * struct used to serialize access to the FILE.
 */

typedef struct {
        FILE f;
        CRITICAL_SECTION lock;
        }   _FILEX;


#define _FILEX_DEFINED
#endif  /* !defined (_FILEX_DEFINED) && defined (_WINDOWS_) */

/*
 * Number of entries supported in the array pointed to by __piob[]. That is,
 * the number of stdio-level files which may be open simultaneously. This
 * is normally set to _NSTREAM_ by the stdio initialization code.
 */
extern int _nstream;

/*
 * Pointer to the array of pointers to FILE/_FILEX structures that are used
 * to manage stdio-level files.
 */
extern void **__piob;

FILE * __cdecl _getstream(void);
FILE * __cdecl _openfile(_In_z_ const char * _Filename, _In_z_ const char * _Mode, _In_ int _ShFlag, _Out_ FILE * _File);
FILE * __cdecl _wopenfile(_In_z_ const char16_t * _Filename, _In_z_ const char16_t * _Mode, _In_ int _ShFlag, _Out_ FILE * _File);
void __cdecl _getbuf(_Out_ FILE * _File);
int __cdecl _filwbuf (__inout FILE * _File);
int __cdecl _flswbuf(_In_ int _Ch, __inout FILE * _File);
void __cdecl _freebuf(__inout FILE * _File);
int __cdecl _stbuf(__inout FILE * _File);
void __cdecl _ftbuf(int _Flag, __inout FILE * _File);

#ifdef _SAFECRT_IMPL

int __cdecl _output(__inout FILE * _File, _In_z_ __format_string const char *_Format, va_list _ArgList);
int __cdecl _woutput(__inout FILE * _File, _In_z_ __format_string const char16_t *_Format, va_list _ArgList);
int __cdecl _output_s(__inout FILE * _File, _In_z_ __format_string const char *_Format, va_list _ArgList);
int __cdecl _output_p(__inout FILE * _File, _In_z_ __format_string const char *_Format, va_list _ArgList);
int __cdecl _woutput_s(__inout FILE * _File, _In_z_ __format_string const char16_t *_Format, va_list _ArgList);
int __cdecl _woutput_p(__inout FILE * _File, _In_z_ __format_string const char16_t *_Format, va_list _ArgList);
typedef int (*OUTPUTFN)(FILE *, const char *, va_list);
typedef int (*WOUTPUTFN)(FILE *, const char16_t *, va_list);

#else  /* _SAFECRT_IMPL */

int __cdecl _output_l(__inout FILE * _File, _In_z_ __format_string const char *_Format, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _woutput_l(__inout FILE * _File, _In_z_ __format_string const char16_t *_Format, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _output_s_l(__inout FILE * _File, _In_z_ __format_string const char *_Format, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _output_p_l(__inout FILE * _File, _In_z_ __format_string const char *_Format, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _woutput_s_l(__inout FILE * _File, _In_z_ __format_string const char16_t *_Format, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _woutput_p_l(__inout FILE * _File, _In_z_ __format_string const char16_t *_Format, _In_opt_ _locale_t _Locale, va_list _ArgList);
typedef int (*OUTPUTFN)(__inout FILE * _File, const char *, _locale_t, va_list);
typedef int (*WOUTPUTFN)(__inout FILE * _File, const char16_t *, _locale_t, va_list);

#endif  /* _SAFECRT_IMPL */

#ifdef _SAFECRT_IMPL

int __cdecl _input(_In_ FILE * _File, _In_z_ __format_string const unsigned char * _Format, va_list _ArgList);
int __cdecl _winput(_In_ FILE * _File, _In_z_ __format_string const char16_t * _Format, va_list _ArgList);
int __cdecl _input_s(_In_ FILE * _File, _In_z_ __format_string const unsigned char * _Format, va_list _ArgList);
int __cdecl _winput_s(_In_ FILE * _File, _In_z_ __format_string const char16_t * _Format, va_list _ArgList);
typedef int (*INPUTFN)(FILE *, const unsigned char *, va_list);
typedef int (*WINPUTFN)(FILE *, const char16_t *, va_list);

#else  /* _SAFECRT_IMPL */

int __cdecl _input_l(__inout FILE * _File, _In_z_ __format_string const unsigned char *, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _winput_l(__inout FILE * _File, _In_z_ __format_string const char16_t *, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _input_s_l(__inout FILE * _File, _In_z_ __format_string const unsigned char *, _In_opt_ _locale_t _Locale, va_list _ArgList);
int __cdecl _winput_s_l(__inout FILE * _File, _In_z_ __format_string const char16_t *, _In_opt_ _locale_t _Locale, va_list _ArgList);
typedef int (*INPUTFN)(FILE *, const unsigned char *, _locale_t, va_list);
typedef int (*WINPUTFN)(FILE *, const char16_t *, _locale_t, va_list);

#ifdef _UNICODE
#define TINPUTFN WINPUTFN
#else  /* _UNICODE */
#define TINPUTFN INPUTFN
#endif  /* _UNICODE */

#endif  /* _SAFECRT_IMPL */

int __cdecl _flush(__inout FILE * _File);
void __cdecl _endstdio(void);

errno_t __cdecl _sopen_helper(_In_z_ const char * _Filename,
    _In_ int _OFlag, _In_ int _ShFlag, _In_ int _PMode,
    _Out_ int * _PFileHandle, int _BSecure);
errno_t __cdecl _wsopen_helper(_In_z_ const char16_t * _Filename,
    _In_ int _OFlag, _In_ int _ShFlag, _In_ int _PMode,
    _Out_ int * _PFileHandle, int _BSecure);

#ifndef CRTDLL
extern int _cflush;
#endif  /* CRTDLL */

extern unsigned int _tempoff;

extern unsigned int _old_pfxlen;

extern int _umaskval;       /* the umask value */

extern char _pipech[];      /* pipe lookahead */

extern char _exitflag;      /* callable termination flag */

extern int _C_Termination_Done; /* termination done flag */

char * __cdecl _getpath(_In_z_ const char * _Src, _Out_writes_z_(_SizeInChars) char * _Dst, _In_ size_t _SizeInChars);
char16_t * __cdecl _wgetpath(_In_z_ const char16_t * _Src, _Out_writes_z_(_SizeInWords) char16_t * _Dst, _In_ size_t _SizeInWords);

extern int _dowildcard;     /* flag to enable argv[] wildcard expansion */

#ifndef _PNH_DEFINED
typedef int (__cdecl * _PNH)( size_t );
#define _PNH_DEFINED
#endif  /* _PNH_DEFINED */

#if _MSC_VER >= 1400 && defined(_M_CEE)
#ifndef __MPNH_DEFINED
typedef int (__clrcall * __MPNH)( size_t );
#define __MPNH_DEFINED
#endif  /* __MPNH_DEFINED */
#endif  /* _MSC_VER >= 1400 && defined(_M_CEE) */


/* calls the currently installed new handler */
int __cdecl _callnewh(_In_ size_t _Size);

extern int _newmode;    /* malloc new() handler mode */

/* pointer to initial environment block that is passed to [w]main */
#ifndef _M_CEE_PURE
extern _CRTIMP char16_t **__winitenv;
extern _CRTIMP char **__initenv;
#endif  /* _M_CEE_PURE */

/* _calloca helper */
#define _calloca(count, size)  ((count<=0 || size<=0 || ((((size_t)_HEAP_MAXREQ) / ((size_t)count)) < ((size_t)size)))? NULL : _malloca(count * size))

/* startup set values */
extern char *_aenvptr;      /* environment ptr */
extern char16_t *_wenvptr;   /* wide environment ptr */

/* command line */

#if defined (_DLL)
_CRTIMP char ** __cdecl __p__acmdln(void);
_CRTIMP char16_t ** __cdecl __p__wcmdln(void);
#endif  /* defined (_DLL) */
#ifndef _M_CEE_PURE
_CRTIMP extern char *_acmdln;
_CRTIMP extern char16_t *_wcmdln;
#else  /* _M_CEE_PURE */
#define _acmdln (*__p__acmdln())
#define _wcmdln (*__p__wcmdln())
#endif  /* _M_CEE_PURE */

/*
 * prototypes for internal startup functions
 */
int __cdecl _cwild(void);           /* wild.c */
int __cdecl _wcwild(void);          /* wwild.c */
int  __cdecl _mtinit(void);         /* tidtable.c */
void __cdecl _mtterm(void);         /* tidtable.c */
int  __cdecl _mtinitlocks(void);    /* mlock.c */
void __cdecl _mtdeletelocks(void);  /* mlock.c */
int  __cdecl _mtinitlocknum(int);   /* mlock.c */

/* Wrapper for InitializeCriticalSection API, with default spin count */
int __cdecl __crtInitCritSecAndSpinCount(PCRITICAL_SECTION, DWORD);
#define _CRT_SPINCOUNT  4000

/*
 * C source build only!!!!
 *
 * more prototypes for internal startup functions
 */
void __cdecl _amsg_exit(int);           /* crt0.c */
void __cdecl __crtExitProcess(int);     /* crt0dat.c */
void __cdecl __crtCorExitProcess(int);  /* crt0dat.c */
void __cdecl __crtdll_callstaticterminators(void); /* crt0dat.c */

/*
_cinit now allows the caller to suppress floating point precision init
This allows the DLLs that use the CRT to not initialise FP precision,
allowing the EXE's setting to persist even when a DLL is loaded
*/
int  __cdecl _cinit(int /* initFloatingPrecision */);   /* crt0dat.c */
void __cdecl __doinits(void);           /* astart.asm */
void __cdecl __doterms(void);           /* astart.asm */
void __cdecl __dopreterms(void);        /* astart.asm */
void __cdecl _FF_MSGBANNER(void);
void __cdecl _fpmath(int /*initPrecision*/);
void __cdecl _fpclear(void);
void __cdecl _fptrap(void);             /* crt0fp.c */
int  __cdecl _heap_init(int);
void __cdecl _heap_term(void);
void __cdecl _heap_abort(void);
void __cdecl __initconin(void);         /* initcon.c */
void __cdecl __initconout(void);        /* initcon.c */
int  __cdecl _ioinit(void);             /* crt0.c, crtlib.c */
void __cdecl _ioterm(void);             /* crt0.c, crtlib.c */
char * __cdecl _GET_RTERRMSG(int);
void __cdecl _NMSG_WRITE(int);
int  __CRTDECL _setargv(void);            /* setargv.c, stdargv.c */
int  __CRTDECL __setargv(void);           /* stdargv.c */
int  __CRTDECL _wsetargv(void);           /* wsetargv.c, wstdargv.c */
int  __CRTDECL __wsetargv(void);          /* wstdargv.c */
int  __cdecl _setenvp(void);            /* stdenvp.c */
int  __cdecl _wsetenvp(void);           /* wstdenvp.c */
void __cdecl __setmbctable(unsigned int);   /* mbctype.c */

#ifdef MRTDLL
_MRTIMP int __cdecl _onexit_process(_CPVFV);
_MRTIMP int __cdecl _onexit_app_domain(_CPVFV);
#endif  /* MRTDLL */

#ifndef _MANAGED_MAIN
int __CRTDECL main(_In_ int _Argc, _In_reads_z_(_Argc) char ** _Argv, _In_z_ char ** _Env);
int __CRTDECL wmain(_In_ int _Argc, _In_reads_z_(_Argc) char16_t ** _Argv, _In_z_ char16_t ** _Env);
#endif  /* _MANAGED_MAIN */

/* helper functions for wide/multibyte environment conversion */
int __cdecl __mbtow_environ (void);
int __cdecl __wtomb_environ (void);

/* These two functions take a char ** for the environment option
   At some point during their execution, they take ownership of the
   memory block passed in using option. At this point, they
   NULL out the incoming char * / char16_t * to ensure there is no
   double-free
*/
int __cdecl __crtsetenv(_Outptr_opt_ char ** _POption, _In_ const int _Primary);
int __cdecl __crtwsetenv(_Outptr_opt_ char16_t ** _POption, _In_ const int _Primary);

#ifndef _M_CEE_PURE
_CRTIMP extern void (__cdecl * _aexit_rtn)(int);
#endif  /* _M_CEE_PURE */

#if defined (_DLL) || defined (CRTDLL)

#ifndef _STARTUP_INFO_DEFINED
typedef struct
{
        int newmode;
} _startupinfo;
#define _STARTUP_INFO_DEFINED
#endif  /* _STARTUP_INFO_DEFINED */

_CRTIMP int __cdecl __getmainargs(_Out_ int * _Argc, _Outptr_result_buffer_(*_Argc) char *** _Argv,
                                  _Outptr_opt_ char *** _Env, _In_ int _DoWildCard,
                                  _In_ _startupinfo * _StartInfo);

_CRTIMP int __cdecl __wgetmainargs(_Out_ int * _Argc, _Outptr_result_buffer_(*_Argc)char16_t *** _Argv,
                                   _Outptr_opt_ char16_t *** _Env, _In_ int _DoWildCard,
                                                                   _In_ _startupinfo * _StartInfo);

#endif  /* defined (_DLL) || defined (CRTDLL) */

/*
 * Prototype, variables and constants which determine how error messages are
 * written out.
 */
#define _UNKNOWN_APP    0
#define _CONSOLE_APP    1
#define _GUI_APP        2

extern int __app_type;

#if !defined (_M_CEE_PURE)

extern Volatile<void*> __native_startup_lock;

#define __NO_REASON UINT_MAX
extern Volatile<unsigned int> __native_dllmain_reason;
extern Volatile<unsigned int> __native_vcclrit_reason;

#if defined (__cplusplus)

#pragma warning(push)
#pragma warning(disable: 4483)
#if _MSC_FULL_VER >= 140050415
#define _NATIVE_STARTUP_NAMESPACE  __identifier("<CrtImplementationDetails>")
#else  /* _MSC_FULL_VER >= 140050415 */
#define _NATIVE_STARTUP_NAMESPACE __CrtImplementationDetails
#endif  /* _MSC_FULL_VER >= 140050415 */

namespace _NATIVE_STARTUP_NAMESPACE
{
    class NativeDll
    {
    private:
        static const unsigned int ProcessDetach   = 0;
        static const unsigned int ProcessAttach   = 1;
        static const unsigned int ThreadAttach    = 2;
        static const unsigned int ThreadDetach    = 3;
        static const unsigned int ProcessVerifier = 4;

    public:

        inline static bool IsInDllMain()
        {
            return (__native_dllmain_reason != __NO_REASON);
        }

        inline static bool IsInProcessAttach()
        {
            return (__native_dllmain_reason == ProcessAttach);
        }

        inline static bool IsInProcessDetach()
        {
            return (__native_dllmain_reason == ProcessDetach);
        }

        inline static bool IsInVcclrit()
        {
            return (__native_vcclrit_reason != __NO_REASON);
        }

        inline static bool IsSafeForManagedCode()
        {
            if (!IsInDllMain())
            {
                return true;
            }

            if (IsInVcclrit())
            {
                return true;
            }

            return !IsInProcessAttach() && !IsInProcessDetach();
        }
    };
}
#pragma warning(pop)

#endif  /* defined (__cplusplus) */

#endif  /* !defined (_M_CEE_PURE) */

extern int __error_mode;

_CRTIMP void __cdecl __set_app_type(int);
#if defined (CRTDLL) && !defined (_SYSCRT)
/*
 * All these function pointer are used for creating global state of CRT
 * functions. Either all of them will be set or all of them will be NULL
 */
typedef void (__cdecl *_set_app_type_function)(int);
typedef int (__cdecl *_get_app_type_function)();
extern _set_app_type_function __set_app_type_server;
extern _get_app_type_function __get_app_type_server;
#endif  /* defined (CRTDLL) && !defined (_SYSCRT) */

/*
 * C source build only!!!!
 *
 * map Win32 errors into Xenix errno values -- for modules written in C
 */
_CRTIMP void __cdecl _dosmaperr(unsigned long);
extern int __cdecl _get_errno_from_oserr(unsigned long);

/*
 * internal routines used by the exec/spawn functions
 */

extern intptr_t __cdecl _dospawn(_In_ int _Mode, _In_opt_z_ const char * _Name, __inout_z char * _Cmd, _In_opt_z_ char * _Env);
extern intptr_t __cdecl _wdospawn(_In_ int _Mode, _In_opt_z_ const char16_t * _Name, __inout_z char16_t * _Cmd, _In_opt_z_ char16_t * _Env);
extern int __cdecl _cenvarg(_In_z_ const char * const * _Argv, _In_opt_z_ const char * const * _Env,
        _Outptr_opt_ char ** _ArgBlk, _Outptr_opt_ char ** _EnvBlk, _In_z_ const char *_Name);
extern int __cdecl _wcenvarg(_In_z_ const char16_t * const * _Argv, _In_opt_z_ const char16_t * const * _Env,
        _Outptr_opt_ char16_t ** _ArgBlk, _Outptr_opt_ char16_t ** _EnvBlk, _In_z_ const char16_t * _Name);
#ifndef _M_IX86
extern char ** _capture_argv(_In_ va_list *, _In_z_ const char * _FirstArg, _Out_writes_z_(_MaxCount) char ** _Static_argv, _In_ size_t _MaxCount);
extern char16_t ** _wcapture_argv(_In_ va_list *, _In_z_ const char16_t * _FirstArg, _Out_writes_z_(_MaxCount) char16_t ** _Static_argv, _In_ size_t _MaxCount);
#endif  /* _M_IX86 */

/*
 * internal routine used by the abort
 */

extern _PHNDLR __cdecl __get_sigabrt(void);

/*
 * Type from ntdef.h
 */

typedef LONG NTSTATUS;

/*
 * Exception code used in _invalid_parameter
 */

#ifndef STATUS_INVALID_PARAMETER
#define STATUS_INVALID_PARAMETER         ((NTSTATUS)0xC000000DL)
#endif  /* STATUS_INVALID_PARAMETER */

/*
 * Exception code used for abort and _CALL_REPORTFAULT
 */

#ifndef STATUS_FATAL_APP_EXIT
#define STATUS_FATAL_APP_EXIT            ((NTSTATUS)0x40000015L)
#endif  /* STATUS_FATAL_APP_EXIT */

/*
 * Validate functions
 */
#include <crtdbg.h> /* _ASSERTE */
#include <errno.h>

#define __STR2WSTR(str)    L##str

#define _STR2WSTR(str)     __STR2WSTR(str)

#define __FILEW__          _STR2WSTR(__FILE__)
#define __FUNCTIONW__      _STR2WSTR(__FUNCTION__)

/* We completely fill the buffer only in debug (see _SECURECRT__FILL_STRING
 * and _SECURECRT__FILL_BYTE macros).
 */
#if !defined (_SECURECRT_FILL_BUFFER)
#ifdef _DEBUG
#define _SECURECRT_FILL_BUFFER 1
#else  /* _DEBUG */
#define _SECURECRT_FILL_BUFFER 0
#endif  /* _DEBUG */
#endif  /* !defined (_SECURECRT_FILL_BUFFER) */

#ifndef _SAFECRT_IMPL
/* _invalid_parameter is already defined in safecrt.h and safecrt.lib */
#if !defined (_NATIVE_char16_t_DEFINED) && defined (_M_CEE_PURE)
extern "C++"
#endif  /* !defined (_NATIVE_char16_t_DEFINED) && defined (_M_CEE_PURE) */
_CRTIMP
#endif  /* _SAFECRT_IMPL */
void __cdecl _invalid_parameter(_In_opt_z_ const char16_t *, _In_opt_z_ const char16_t *, _In_opt_z_ const char16_t *, unsigned int, uintptr_t);

#if !defined (_NATIVE_char16_t_DEFINED) && defined (_M_CEE_PURE)
extern "C++"
#endif  /* !defined (_NATIVE_char16_t_DEFINED) && defined (_M_CEE_PURE) */
_CRTIMP
void __cdecl _invoke_watson(_In_opt_z_ const char16_t *, _In_opt_z_ const char16_t *, _In_opt_z_ const char16_t *, unsigned int, uintptr_t);

#ifndef _DEBUG
#if !defined (_NATIVE_char16_t_DEFINED) && defined (_M_CEE_PURE)
extern "C++"
#endif  /* !defined (_NATIVE_char16_t_DEFINED) && defined (_M_CEE_PURE) */
_CRTIMP
void __cdecl _invalid_parameter_noinfo(void);
#endif  /* _DEBUG */

/* Invoke Watson if _ExpressionError is not 0; otherwise simply return _ExpressionError */
__forceinline
void _invoke_watson_if_error(
    errno_t _ExpressionError,
    const char16_t *_Expression,
    const char16_t *_Function,
    const char16_t *_File,
    unsigned int _Line,
    uintptr_t _Reserved
    )
{
    if (_ExpressionError == 0)
    {
        return;
    }
    _invoke_watson(_Expression, _Function, _File, _Line, _Reserved);
}

/* Invoke Watson if _ExpressionError is not 0 and equal to _ErrorValue1 or _ErrorValue2; otherwise simply return _ExpressionError */
__forceinline
errno_t _invoke_watson_if_oneof(
    errno_t _ExpressionError,
    errno_t _ErrorValue1,
    errno_t _ErrorValue2,
    const char16_t *_Expression,
    const char16_t *_Function,
    const char16_t *_File,
    unsigned int _Line,
    uintptr_t _Reserved
    )
{
    if (_ExpressionError == 0 || (_ExpressionError != _ErrorValue1 && _ExpressionError != _ErrorValue2))
    {
        return _ExpressionError;
    }
    _invoke_watson(_Expression, _Function, _File, _Line, _Reserved);
    return _ExpressionError;
}

/*
 * Assert in debug builds.
 * set errno and return
 *
 */
#ifdef _DEBUG
#define _CALL_INVALID_PARAMETER_FUNC(funcname, expr) funcname(expr, __FUNCTIONW__, __FILEW__, __LINE__, 0)
#define _INVOKE_WATSON_IF_ERROR(expr) _invoke_watson_if_error((expr), __STR2WSTR(#expr), __FUNCTIONW__, __FILEW__, __LINE__, 0)
#define _INVOKE_WATSON_IF_ONEOF(expr, errvalue1, errvalue2) _invoke_watson_if_oneof(expr, (errvalue1), (errvalue2), __STR2WSTR(#expr), __FUNCTIONW__, __FILEW__, __LINE__, 0)
#else  /* _DEBUG */
#define _CALL_INVALID_PARAMETER_FUNC(funcname, expr) funcname(NULL, NULL, NULL, 0, 0)
#define _INVOKE_WATSON_IF_ERROR(expr) _invoke_watson_if_error(expr, NULL, NULL, NULL, 0, 0)
#define _INVOKE_WATSON_IF_ONEOF(expr, errvalue1, errvalue2) _invoke_watson_if_oneof((expr), (errvalue1), (errvalue2), NULL, NULL, NULL, 0, 0)
#endif  /* _DEBUG */

#define _INVALID_PARAMETER(expr) _CALL_INVALID_PARAMETER_FUNC(_invalid_parameter, expr)

#define _VALIDATE_RETURN_VOID( expr, errorcode )                               \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(_CRT_WIDE(#expr));                              \
            return;                                                            \
        }                                                                      \
    }

/*
 * Assert in debug builds.
 * set errno and return value
 */

#ifndef _VALIDATE_RETURN
#define _VALIDATE_RETURN( expr, errorcode, retexpr )                           \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(_CRT_WIDE(#expr) );                             \
            return ( retexpr );                                                \
        }                                                                      \
    }
#endif  /* _VALIDATE_RETURN */

#ifndef _VALIDATE_RETURN_NOEXC
#define _VALIDATE_RETURN_NOEXC( expr, errorcode, retexpr )                     \
    {                                                                          \
        if ( !(expr) )                                                         \
        {                                                                      \
            errno = errorcode;                                                 \
            return ( retexpr );                                                \
        }                                                                      \
    }
#endif  /* _VALIDATE_RETURN_NOEXC */

/*
 * Assert in debug builds.
 * set errno and set retval for later usage
 */

#define _VALIDATE_SETRET( expr, errorcode, retval, retexpr )                   \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(_CRT_WIDE(#expr));                              \
            retval=( retexpr );                                                \
        }                                                                      \
    }

#define _CHECK_FH_RETURN( handle, errorcode, retexpr )                         \
    {                                                                          \
        if(handle == _NO_CONSOLE_FILENO)                                       \
        {                                                                      \
            errno = errorcode;                                                 \
            return ( retexpr );                                                \
        }                                                                      \
    }

/*
    We use _VALIDATE_STREAM_ANSI_RETURN to ensure that ANSI file operations(
    fprintf etc) aren't called on files opened as UNICODE. We do this check
    only if it's an actual FILE pointer & not a string
*/

#define _VALIDATE_STREAM_ANSI_RETURN( stream, errorcode, retexpr )                   \
    {                                                                                \
        FILE *_Stream=stream;                                                        \
        _VALIDATE_RETURN(( (_Stream->_flag & _IOSTRG) ||                             \
                       (   (_textmode_safe(_fileno(_Stream)) == __IOINFO_TM_ANSI) && \
                            !_tm_unicode_safe(_fileno(_Stream)))),                   \
                        errorcode, retexpr)                                          \
    }

/*
    We use _VALIDATE_STREAM_ANSI_SETRET to ensure that ANSI file operations(
    fprintf etc) aren't called on files opened as UNICODE. We do this check
    only if it's an actual FILE pointer & not a string. It doesn't actually return
        immediately
*/

#define _VALIDATE_STREAM_ANSI_SETRET( stream, errorcode, retval, retexpr)            \
    {                                                                                \
        FILE *_Stream=stream;                                                        \
        _VALIDATE_SETRET(( (_Stream->_flag & _IOSTRG) ||                             \
                       (   (_textmode_safe(_fileno(_Stream)) == __IOINFO_TM_ANSI) && \
                            !_tm_unicode_safe(_fileno(_Stream)))),                   \
                            errorcode, retval, retexpr)                              \
    }

/*
 * Assert in debug builds.
 * Return value (do not set errno)
 */

#define _VALIDATE_RETURN_NOERRNO( expr, retexpr )                              \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            _INVALID_PARAMETER(_CRT_WIDE(#expr));                              \
            return ( retexpr );                                                \
        }                                                                      \
    }

/*
 * Assert in debug builds.
 * set errno and return errorcode
 */

#define _VALIDATE_RETURN_ERRCODE( expr, errorcode )                            \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(_CRT_WIDE(#expr));                              \
            return ( errorcode );                                              \
        }                                                                      \
    }

#define _VALIDATE_RETURN_ERRCODE_NOEXC( expr, errorcode )                      \
    {                                                                          \
        if (!(expr))                                                           \
        {                                                                      \
            errno = errorcode;                                                 \
            return ( errorcode );                                              \
        }                                                                      \
    }

#define _VALIDATE_CLEAR_OSSERR_RETURN( expr, errorcode, retexpr )              \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            _doserrno = 0L;                                                    \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(_CRT_WIDE(#expr) );                             \
            return ( retexpr );                                                \
        }                                                                      \
    }

#define _CHECK_FH_CLEAR_OSSERR_RETURN( handle, errorcode, retexpr )            \
    {                                                                          \
        if(handle == _NO_CONSOLE_FILENO)                                       \
        {                                                                      \
            _doserrno = 0L;                                                    \
            errno = errorcode;                                                 \
            return ( retexpr );                                                \
        }                                                                      \
    }

#define _VALIDATE_CLEAR_OSSERR_RETURN_ERRCODE( expr, errorcode )               \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            _doserrno = 0L;                                                    \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(_CRT_WIDE(#expr));                              \
            return ( errorcode );                                              \
        }                                                                      \
    }

#define _CHECK_FH_CLEAR_OSSERR_RETURN_ERRCODE( handle, retexpr )               \
    {                                                                          \
        if(handle == _NO_CONSOLE_FILENO)                                       \
        {                                                                      \
            _doserrno = 0L;                                                    \
            return ( retexpr );                                                \
        }                                                                      \
    }

#ifdef _DEBUG
extern size_t __crtDebugFillThreshold;
#endif  /* _DEBUG */

#if !defined (_SECURECRT_FILL_BUFFER_THRESHOLD)
#ifdef _DEBUG
#define _SECURECRT_FILL_BUFFER_THRESHOLD __crtDebugFillThreshold
#else  /* _DEBUG */
#define _SECURECRT_FILL_BUFFER_THRESHOLD ((size_t)0)
#endif  /* _DEBUG */
#endif  /* !defined (_SECURECRT_FILL_BUFFER_THRESHOLD) */

#if _SECURECRT_FILL_BUFFER
#define _SECURECRT__FILL_STRING(_String, _Size, _Offset)                            \
    if ((_Size) != ((size_t)-1) && (_Size) != INT_MAX &&                            \
        ((size_t)(_Offset)) < (_Size))                                              \
    {                                                                               \
        memset((_String) + (_Offset),                                               \
            _SECURECRT_FILL_BUFFER_PATTERN,                                         \
            (_SECURECRT_FILL_BUFFER_THRESHOLD < ((size_t)((_Size) - (_Offset))) ?   \
                _SECURECRT_FILL_BUFFER_THRESHOLD :                                  \
                ((_Size) - (_Offset))) * sizeof(*(_String)));                       \
    }
#else  /* _SECURECRT_FILL_BUFFER */
#define _SECURECRT__FILL_STRING(_String, _Size, _Offset)
#endif  /* _SECURECRT_FILL_BUFFER */

#if _SECURECRT_FILL_BUFFER
#define _SECURECRT__FILL_BYTE(_Position)                \
    if (_SECURECRT_FILL_BUFFER_THRESHOLD > 0)           \
    {                                                   \
        (_Position) = _SECURECRT_FILL_BUFFER_PATTERN;   \
    }
#else  /* _SECURECRT_FILL_BUFFER */
#define _SECURECRT__FILL_BYTE(_Position)
#endif  /* _SECURECRT_FILL_BUFFER */

#ifdef __cplusplus
#define _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE extern "C"
#else  /* __cplusplus */
#define _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE
#endif  /* __cplusplus */

/* helper macros to redirect an mbs function to the corresponding _l version */
#define _REDIRECT_TO_L_VERSION_1(_ReturnType, _FunctionName, _Type1) \
    _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE \
    _ReturnType __cdecl _FunctionName(_Type1 _Arg1) \
    { \
        return _FunctionName##_l(_Arg1, NULL); \
    }

#define _REDIRECT_TO_L_VERSION_2(_ReturnType, _FunctionName, _Type1, _Type2) \
    _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE \
    _ReturnType __cdecl _FunctionName(_Type1 _Arg1, _Type2 _Arg2) \
    { \
        return _FunctionName##_l(_Arg1, _Arg2, NULL); \
    }

#define _REDIRECT_TO_L_VERSION_3(_ReturnType, _FunctionName, _Type1, _Type2, _Type3) \
    _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE \
    _ReturnType __cdecl _FunctionName(_Type1 _Arg1, _Type2 _Arg2, _Type3 _Arg3) \
    { \
        return _FunctionName##_l(_Arg1, _Arg2, _Arg3, NULL); \
    }

#define _REDIRECT_TO_L_VERSION_4(_ReturnType, _FunctionName, _Type1, _Type2, _Type3, _Type4) \
    _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE \
    _ReturnType __cdecl _FunctionName(_Type1 _Arg1, _Type2 _Arg2, _Type3 _Arg3, _Type4 _Arg4) \
    { \
        return _FunctionName##_l(_Arg1, _Arg2, _Arg3, _Arg4, NULL); \
    }

#define _REDIRECT_TO_L_VERSION_5(_ReturnType, _FunctionName, _Type1, _Type2, _Type3, _Type4, _Type5) \
    _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE \
    _ReturnType __cdecl _FunctionName(_Type1 _Arg1, _Type2 _Arg2, _Type3 _Arg3, _Type4 _Arg4, _Type5 _Arg5) \
    { \
        return _FunctionName##_l(_Arg1, _Arg2, _Arg3, _Arg4, _Arg5, NULL); \
    }

#define _REDIRECT_TO_L_VERSION_6(_ReturnType, _FunctionName, _Type1, _Type2, _Type3, _Type4, _Type5, _Type6) \
    _REDIRECT_TO_L_VERSION_FUNC_PROLOGUE \
    _ReturnType __cdecl _FunctionName(_Type1 _Arg1, _Type2 _Arg2, _Type3 _Arg3, _Type4 _Arg4, _Type5 _Arg5, _Type6 _Arg6) \
    { \
        return _FunctionName##_l(_Arg1, _Arg2, _Arg3, _Arg4, _Arg5, _Arg6, NULL); \
    }

/* internal helper functions for encoding and decoding pointers */
void __cdecl _init_pointers();
_CRTIMP void * __cdecl _encode_pointer(void *);
_CRTIMP void * __cdecl _encoded_null();
_CRTIMP void * __cdecl _decode_pointer(void *);

/* internal helper function for communicating with the debugger */
BOOL DebuggerKnownHandle();

#define _ERRCHECK(e) \
    _INVOKE_WATSON_IF_ERROR(e)

#define _ERRCHECK_EINVAL(e) \
    _INVOKE_WATSON_IF_ONEOF(e, EINVAL, EINVAL)

#define _ERRCHECK_EINVAL_ERANGE(e) \
    _INVOKE_WATSON_IF_ONEOF(e, EINVAL, ERANGE)

#define _ERRCHECK_SPRINTF(_PrintfCall) \
    { \
        errno_t _SaveErrno = errno; \
        errno = 0; \
        if ( ( _PrintfCall ) < 0) \
        { \
            _ERRCHECK_EINVAL_ERANGE(errno); \
        } \
        errno = _SaveErrno; \
    }

/* internal helper function to access environment variable in read-only mode */
const char16_t * __cdecl _wgetenv_helper_nolock(const char16_t *);
const char * __cdecl _getenv_helper_nolock(const char *);

/* internal helper routines used to query a PE image header. */
BOOL __cdecl _ValidateImageBase(PBYTE pImageBase);
PIMAGE_SECTION_HEADER __cdecl _FindPESection(PBYTE pImageBase, DWORD_PTR rva);
BOOL __cdecl _IsNonwritableInCurrentImage(PBYTE pTarget);

#ifdef __cplusplus
}
#endif  /* __cplusplus */

#ifdef _MSC_VER
#pragma pack(pop)
#endif  /* _MSC_VER */

#endif  /* _INC_INTERNAL */
