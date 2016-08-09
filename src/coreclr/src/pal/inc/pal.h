// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    pal.h

Abstract:

    Rotor Platform Adaptation Layer (PAL) header file.  This file
    defines all types and API calls required by the Rotor port of
    the Microsoft Common Language Runtime.

    Defines which control the behavior of this include file:
      UNICODE - define it to set the Ansi/Unicode neutral names to
                be the ...W names.  Otherwise the neutral names default
                to be the ...A names.
      PAL_IMPLEMENTATION - define it when implementing the PAL.  Otherwise
                leave it undefined when consuming the PAL.

    Note:  some fields in structs have been renamed from the original
    SDK documentation names, with _PAL_Undefined appended.  This leaves
    the structure layout identical to its Win32 version, but prevents
    PAL consumers from inadvertently referencing undefined fields.

    If you want to add a PAL_ wrapper function to a native function in
    here, you also need to edit palinternal.h and win32pal.h.
    


--*/

#ifndef __PAL_H__
#define __PAL_H__

#ifdef PAL_STDCPP_COMPAT
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <errno.h>
#include <ctype.h>
#endif

#ifdef  __cplusplus
extern "C" {
#endif

#if defined (PLATFORM_UNIX)
// This macro is used to standardize the wide character string literals between UNIX and Windows.
// Unix L"" is UTF32, and on windows it's UTF16.  Because of built-in assumptions on the size
// of string literals, it's important to match behaviour between Unix and Windows.  Unix will be defined
// as u"" (char16_t)
#define W(str)  u##str

// Undefine the QUOTE_MACRO_L helper and redefine it in terms of u.
// The reason that we do this is that quote macro is defined in ndp\common\inc,
// not inside of coreclr sources.

#define QUOTE_MACRO_L(x) QUOTE_MACRO_u(x)
#define QUOTE_MACRO_u_HELPER(x)     u###x
#define QUOTE_MACRO_u(x)            QUOTE_MACRO_u_HELPER(x)

#endif

#include <pal_char16.h>
#include <pal_error.h>
#include <pal_mstypes.h>

/******************* Processor-specific glue  *****************************/

#ifndef _MSC_VER

#if defined(__i686__) && !defined(_M_IX86)
#define _M_IX86 600
#elif defined(__i586__) && !defined(_M_IX86)
#define _M_IX86 500
#elif defined(__i486__) && !defined(_M_IX86)
#define _M_IX86 400
#elif defined(__i386__) && !defined(_M_IX86)
#define _M_IX86 300
#elif defined(__ppc__) && !defined(_M_PPC)
#define _M_PPC 100
#elif defined(_AIX) && defined(_POWER) && !defined(_M_PPC)
#define _M_PPC 100
#elif defined(__sparc__) && !defined(_M_SPARC)
#define _M_SPARC 100
#elif defined(__hppa__) && !defined(_M_PARISC)
#define _M_PARISC 100
#elif defined(__ia64__) && !defined(_M_IA64)
#define _M_IA64 64100
#elif defined(__x86_64__) && !defined(_M_AMD64)
#define _M_AMD64 100
#elif defined(__arm__) && !defined(_M_ARM)
#define _M_ARM 7
#elif defined(__aarch64__) && !defined(_M_ARM64)
#define _M_ARM64 1
#endif

#if defined(_M_IX86) && !defined(_X86_)
#define _X86_
#elif defined(_M_ALPHA) && !defined(_ALPHA_)
#define _ALPHA_
#elif defined(_M_PPC) && !defined(_PPC_)
#define _PPC_
#elif defined(_M_SPARC) && !defined(_SPARC_)
#define _SPARC_
#elif defined(_M_PARISC) && !defined(_PARISC_)
#define _PARISC_
#elif defined(_M_MRX000) && !defined(_MIPS_)
#define _MIPS_
#elif defined(_M_M68K) && !defined(_68K_)
#define _68K_
#elif defined(_M_IA64) && !defined(_IA64_)
#define _IA64_
#elif defined(_M_AMD64) && !defined(_AMD64_)
#define _AMD64_
#elif defined(_M_ARM) && !defined(_ARM_)
#define _ARM_
#elif defined(_M_ARM64) && !defined(_ARM64_)
#define _ARM64_
#endif

#endif // !_MSC_VER

/******************* ABI-specific glue *******************************/

#if defined(_PPC_) || defined(_PPC64_) || defined(_SPARC_) || defined(_PARISC_) || defined(_IA64_)
#define BIGENDIAN 1
#endif

#ifdef __APPLE__
// Both PowerPC, i386 and x86_64 on Mac OS X use 16-byte alignment.
#define STACK_ALIGN_BITS             4
#define STACK_ALIGN_REQ             (1 << STACK_ALIGN_BITS)
#endif

#define MAX_PATH 260
#define _MAX_PATH 260
#define _MAX_DRIVE  3   /* max. length of drive component */
#define _MAX_DIR    256 /* max. length of path component */
#define _MAX_FNAME  256 /* max. length of file name component */
#define _MAX_EXT    256 /* max. length of extension component */

// In some Win32 APIs MAX_PATH is used for file names (even though 256 is the normal file system limit)
// use _MAX_PATH_FNAME to indicate these cases
#define MAX_PATH_FNAME MAX_PATH
#define MAX_LONGPATH   1024  /* max. length of full pathname */

#define MAXSHORT      0x7fff
#define MAXLONG       0x7fffffff
#define MAXCHAR       0x7f
#define MAXDWORD      0xffffffff

//  Sorting IDs.
//
//  Note that the named locale APIs (eg CompareStringExEx) are recommended.
//

#define LANG_CHINESE                     0x04
#define LANG_ENGLISH                     0x09
#define LANG_JAPANESE                    0x11
#define LANG_KOREAN                      0x12
#define LANG_THAI                        0x1e

/******************* Compiler-specific glue *******************************/

#ifndef _MSC_VER
#if defined(CORECLR)
// Define this if the underlying platform supports true 2-pass EH.
// At the same time, this enables running several PAL instances
// side-by-side.
#define FEATURE_PAL_SXS 1
#endif // CORECLR
#endif // !_MSC_VER

#if defined(_MSC_VER) || defined(__llvm__)
#define DECLSPEC_ALIGN(x)   __declspec(align(x))
#else
#define DECLSPEC_ALIGN(x) 
#endif

#define DECLSPEC_NORETURN   PAL_NORETURN

#if !defined(_MSC_VER) || defined(SOURCE_FORMATTING)
#define __assume(x) (void)0
#define __annotation(x)
#endif //!MSC_VER

#ifdef _MSC_VER

#if defined(_M_MRX000) || defined(_M_ALPHA) || defined(_M_PPC) || defined(_M_IA64)
#define UNALIGNED __unaligned
#else
#define UNALIGNED
#endif

#else // _MSC_VER

#define UNALIGNED

#endif // _MSC_VER

#ifndef FORCEINLINE
#if _MSC_VER < 1200
#define FORCEINLINE inline
#else
#define FORCEINLINE __forceinline
#endif
#endif

#ifndef PAL_STDCPP_COMPAT

#ifdef _M_ALPHA

typedef struct {
    char *a0;       /* pointer to first homed integer argument */
    int offset;     /* byte offset of next parameter */
} va_list;

#define va_start(list, v) __builtin_va_start(list, v, 1)
#define va_end(list)

#elif __GNUC__

#if defined(_AIX)

typedef __builtin_va_list __gnuc_va_list;
typedef __builtin_va_list va_list;
#define va_start(v,l)   __builtin_va_start(v,l)
#define va_end          __builtin_va_end
#define va_arg          __builtin_va_arg

#else // _AIX

#if __GNUC__ == 2
typedef void * va_list;
#else
typedef __builtin_va_list va_list;
#endif  // __GNUC__

/* We should consider if the va_arg definition here is actually necessary.
   Could we use the standard va_arg definition? */

#if __GNUC__ == 2
#if defined(_SPARC_) || defined(_PARISC_) // ToDo: is this the right thing for PARISC?
#define va_start(list, v) (__builtin_next_arg(v), list = (char *) __builtin_saveregs())
#define __va_rounded_size(TYPE)  \
  (((sizeof (TYPE) + sizeof (int) - 1) / sizeof (int)) * sizeof (int))
#define __record_type_class 12
#define __real_type_class 8
#define va_arg(pvar,TYPE)                                       \
__extension__                                                   \
(*({((__builtin_classify_type (*(TYPE*) 0) >= __record_type_class \
      || (__builtin_classify_type (*(TYPE*) 0) == __real_type_class \
          && sizeof (TYPE) == 16))                              \
    ? ((pvar) = (char *)(pvar) + __va_rounded_size (TYPE *),    \
       *(TYPE **) (void *) ((char *)(pvar) - __va_rounded_size (TYPE *))) \
    : __va_rounded_size (TYPE) == 8                             \
    ? ({ union {char __d[sizeof (TYPE)]; int __i[2];} __u;      \
         __u.__i[0] = ((int *) (void *) (pvar))[0];             \
         __u.__i[1] = ((int *) (void *) (pvar))[1];             \
         (pvar) = (char *)(pvar) + 8;                           \
         (TYPE *) (void *) __u.__d; })                          \
    : ((pvar) = (char *)(pvar) + __va_rounded_size (TYPE),      \
       ((TYPE *) (void *) ((char *)(pvar) - __va_rounded_size (TYPE)))));}))
#else   // _SPARC_ or _PARISC_
// GCC 2.95.3 on non-SPARC
#define __va_size(type) (((sizeof(type) + sizeof(int) - 1) / sizeof(int)) * sizeof(int))
#define va_start(list, v) ((list) = (va_list) __builtin_next_arg(v))
#define va_arg(ap, type) (*(type *)((ap) += __va_size(type), (ap) - __va_size(type)))
#endif  // _SPARC_ or _PARISC_
#else // __GNUC__ == 2
#define va_start    __builtin_va_start
#define va_arg      __builtin_va_arg
#endif // __GNUC__ == 2

#define va_copy     __builtin_va_copy
#define va_end      __builtin_va_end

#endif // _AIX

#define VOID void

#define PUB __attribute__((visibility("default")))

#else // __GNUC__

typedef char * va_list;

#define _INTSIZEOF(n)   ( (sizeof(n) + sizeof(int) - 1) & ~(sizeof(int) - 1) )

#if _MSC_VER >= 1400

#ifdef  __cplusplus
#define _ADDRESSOF(v)   ( &reinterpret_cast<const char &>(v) )
#else
#define _ADDRESSOF(v)   ( &(v) )
#endif

#define _crt_va_start(ap,v)  ( ap = (va_list)_ADDRESSOF(v) + _INTSIZEOF(v) )
#define _crt_va_arg(ap,t)    ( *(t *)((ap += _INTSIZEOF(t)) - _INTSIZEOF(t)) )
#define _crt_va_end(ap)      ( ap = (va_list)0 )

#define va_start _crt_va_start
#define va_arg _crt_va_arg
#define va_end _crt_va_end

#else  // _MSC_VER

#define va_start(ap,v)    (ap = (va_list) (&(v)) + _INTSIZEOF(v))
#define va_arg(ap,t)    ( *(t *)((ap += _INTSIZEOF(t)) - _INTSIZEOF(t)) )
#define va_end(ap)

#endif // _MSC_VER

#define va_copy(dest,src) (dest = src)

#endif // __GNUC__

#endif // !PAL_STDCPP_COMPAT

/******************* PAL-Specific Entrypoints *****************************/

#define IsDebuggerPresent PAL_IsDebuggerPresent

PALIMPORT
BOOL
PALAPI
PAL_IsDebuggerPresent();
 
#define MAXIMUM_SUSPEND_COUNT  MAXCHAR

#define CHAR_BIT      8

#define SCHAR_MIN   (-128)
#define SCHAR_MAX     127
#define UCHAR_MAX     0xff

#define SHRT_MIN    (-32768)
#define SHRT_MAX      32767
#define USHRT_MAX     0xffff

#define INT_MIN     (-2147483647 - 1)
#define INT_MAX       2147483647
#define UINT_MAX      0xffffffff

#define LONG_MIN    (-2147483647L - 1)
#define LONG_MAX      2147483647L
#define ULONG_MAX     0xffffffffUL

#define FLT_MAX 3.402823466e+38F
#define DBL_MAX 1.7976931348623157e+308

/* minimum signed 64 bit value */
#define _I64_MIN    (I64(-9223372036854775807) - 1)
/* maximum signed 64 bit value */
#define _I64_MAX      I64(9223372036854775807)
/* maximum unsigned 64 bit value */
#define _UI64_MAX     UI64(0xffffffffffffffff)

#define _I8_MAX   SCHAR_MAX
#define _I8_MIN   SCHAR_MIN
#define _I16_MAX  SHRT_MAX
#define _I16_MIN  SHRT_MIN
#define _I32_MAX  INT_MAX
#define _I32_MIN  INT_MIN
#define _UI8_MAX  UCHAR_MAX
#define _UI8_MIN  UCHAR_MIN
#define _UI16_MAX USHRT_MAX
#define _UI16_MIN USHRT_MIN
#define _UI32_MAX UINT_MAX
#define _UI32_MIN UINT_MIN

#ifdef PAL_STDCPP_COMPAT
#undef NULL
#endif

#ifndef NULL
#if defined(__cplusplus)
#define NULL    0
#else
#define NULL    ((void *)0)
#endif
#endif

#if defined(PAL_STDCPP_COMPAT) && !defined(__cplusplus)
#define nullptr NULL
#endif // defined(PAL_STDCPP_COMPAT) && !defined(__cplusplus)

#ifndef PAL_STDCPP_COMPAT

#if _WIN64 || _MSC_VER >= 1400
typedef __int64 time_t;
#else
typedef long time_t;
#endif
#define _TIME_T_DEFINED
#endif // !PAL_STDCPP_COMPAT

#if ENABLE_DOWNLEVEL_FOR_NLS
#define MAKELCID(lgid, srtid)  ((DWORD)((((DWORD)((WORD  )(srtid))) << 16) |  \
                                         ((DWORD)((WORD  )(lgid)))))
#define LANGIDFROMLCID(lcid)   ((WORD)(lcid))
#define SORTIDFROMLCID(lcid)   ((WORD)((((DWORD)(lcid)) >> 16) & 0xf))

#define LANG_NEUTRAL                     0x00
#define LANG_INVARIANT                   0x7f
#define SUBLANG_NEUTRAL                  0x00    // language neutral
#define SUBLANG_DEFAULT                  0x01    // user default
#define SORT_DEFAULT                     0x0     // sorting default
#define SUBLANG_SYS_DEFAULT              0x02    // system default

#define MAKELANGID(p, s)       ((((WORD  )(s)) << 10) | (WORD  )(p))
#define PRIMARYLANGID(lgid)    ((WORD  )(lgid) & 0x3ff)
#define SUBLANGID(lgid)        ((WORD  )(lgid) >> 10)

#define LANG_SYSTEM_DEFAULT    (MAKELANGID(LANG_NEUTRAL, SUBLANG_SYS_DEFAULT))
#define LANG_USER_DEFAULT      (MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT))
#define LOCALE_SYSTEM_DEFAULT  (MAKELCID(LANG_SYSTEM_DEFAULT, SORT_DEFAULT))
#define LOCALE_USER_DEFAULT    (MAKELCID(LANG_USER_DEFAULT, SORT_DEFAULT))
#define LOCALE_NEUTRAL         (MAKELCID(MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL), SORT_DEFAULT))
#define LOCALE_US_ENGLISH      (MAKELCID(MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US), SORT_DEFAULT))
#define LOCALE_INVARIANT       (MAKELCID(MAKELANGID(LANG_INVARIANT, SUBLANG_NEUTRAL), SORT_DEFAULT))

#define SUBLANG_ENGLISH_US               0x01
#define SUBLANG_CHINESE_TRADITIONAL      0x01    /* Chinese (Traditional) */

#endif // ENABLE_DOWNLEVEL_FOR_NLS


#define CT_CTYPE1                 0x00000001  /* ctype 1 information */
#define CT_CTYPE2                 0x00000002  /* ctype 2 information */
#define CT_CTYPE3                 0x00000004  /* ctype 3 information */
#define C1_UPPER                  0x0001      /* upper case */
#define C1_LOWER                  0x0002      /* lower case */
#define C1_DIGIT                  0x0004      /* decimal digits */
#define C1_SPACE                  0x0008      /* spacing characters */
#define C1_PUNCT                  0x0010      /* punctuation characters */
#define C1_CNTRL                  0x0020      /* control characters */
#define C1_BLANK                  0x0040      /* blank characters */
#define C1_XDIGIT                 0x0080      /* other digits */
#define C1_ALPHA                  0x0100      /* any linguistic character */
#define C2_LEFTTORIGHT            0x0001      /* left to right */
#define C2_RIGHTTOLEFT            0x0002      /* right to left */
#define C2_EUROPENUMBER           0x0003      /* European number, digit */
#define C2_EUROPESEPARATOR        0x0004      /* European numeric separator */
#define C2_EUROPETERMINATOR       0x0005      /* European numeric terminator */
#define C2_ARABICNUMBER           0x0006      /* Arabic number */
#define C2_COMMONSEPARATOR        0x0007      /* common numeric separator */
#define C2_BLOCKSEPARATOR         0x0008      /* block separator */
#define C2_SEGMENTSEPARATOR       0x0009      /* segment separator */
#define C2_WHITESPACE             0x000A      /* white space */
#define C2_OTHERNEUTRAL           0x000B      /* other neutrals */
#define C2_NOTAPPLICABLE          0x0000      /* no implicit directionality */
#define C3_NONSPACING             0x0001      /* nonspacing character */
#define C3_DIACRITIC              0x0002      /* diacritic mark */
#define C3_VOWELMARK              0x0004      /* vowel mark */
#define C3_SYMBOL                 0x0008      /* symbols */
#define C3_KATAKANA               0x0010      /* katakana character */
#define C3_HIRAGANA               0x0020      /* hiragana character */
#define C3_HALFWIDTH              0x0040      /* half width character */
#define C3_FULLWIDTH              0x0080      /* full width character */
#define C3_IDEOGRAPH              0x0100      /* ideographic character */
#define C3_KASHIDA                0x0200      /* Arabic kashida character */
#define C3_LEXICAL                0x0400      /* lexical character */
#define C3_ALPHA                  0x8000      /* any ling. char (C1_ALPHA) */
#define C3_NOTAPPLICABLE          0x0000      /* ctype 3 is not applicable */

#define DLL_PROCESS_ATTACH 1
#define DLL_THREAD_ATTACH  2
#define DLL_THREAD_DETACH  3
#define DLL_PROCESS_DETACH 0

#define PAL_INITIALIZE_NONE                         0x00
#define PAL_INITIALIZE_SYNC_THREAD                  0x01
#define PAL_INITIALIZE_EXEC_ALLOCATOR               0x02
#define PAL_INITIALIZE_STD_HANDLES                  0x04
#define PAL_INITIALIZE_REGISTER_SIGTERM_HANDLER     0x08
#define PAL_INITIALIZE_DEBUGGER_EXCEPTIONS          0x10

// PAL_Initialize() flags
#define PAL_INITIALIZE                 (PAL_INITIALIZE_SYNC_THREAD | PAL_INITIALIZE_STD_HANDLES)

// PAL_InitializeDLL() flags - don't start any of the helper threads
#define PAL_INITIALIZE_DLL             PAL_INITIALIZE_NONE       

// PAL_InitializeCoreCLR() flags
#define PAL_INITIALIZE_CORECLR         (PAL_INITIALIZE | PAL_INITIALIZE_EXEC_ALLOCATOR | PAL_INITIALIZE_REGISTER_SIGTERM_HANDLER | PAL_INITIALIZE_DEBUGGER_EXCEPTIONS)

typedef DWORD (PALAPI *PTHREAD_START_ROUTINE)(LPVOID lpThreadParameter);
typedef PTHREAD_START_ROUTINE LPTHREAD_START_ROUTINE;

/******************* PAL-Specific Entrypoints *****************************/

PALIMPORT
int
PALAPI
PAL_Initialize(
    int argc,
    const char * const argv[]);

PALIMPORT
int
PALAPI
PAL_InitializeDLL();

PALIMPORT
DWORD
PALAPI
PAL_InitializeCoreCLR(
    const char *szExePath);

PALIMPORT
DWORD_PTR
PALAPI
PAL_EntryPoint(
    IN LPTHREAD_START_ROUTINE lpStartAddress,
    IN LPVOID lpParameter);

/// <summary>
/// This function shuts down PAL WITHOUT exiting the current process.
/// </summary>
PALIMPORT
void
PALAPI
PAL_Shutdown(
    void);

/// <summary>
/// This function shuts down PAL and exits the current process.
/// </summary>
PALIMPORT
void
PALAPI
PAL_Terminate(
    void);

/// <summary>
/// This function shuts down PAL and exits the current process with
/// the specified exit code.
/// </summary>
PALIMPORT
void
PALAPI
PAL_TerminateEx(
    int exitCode);

typedef VOID (*PSHUTDOWN_CALLBACK)(void);

PALIMPORT
VOID
PALAPI
PAL_SetShutdownCallback(
    IN PSHUTDOWN_CALLBACK callback);

typedef VOID (*PPAL_STARTUP_CALLBACK)(
    char *modulePath,
    HMODULE hModule,
    PVOID parameter);

PALIMPORT
DWORD
PALAPI
PAL_RegisterForRuntimeStartup(
    IN DWORD dwProcessId,
    IN PPAL_STARTUP_CALLBACK pfnCallback,
    IN PVOID parameter,
    OUT PVOID *ppUnregisterToken);

PALIMPORT
DWORD
PALAPI
PAL_UnregisterForRuntimeStartup(
    IN PVOID pUnregisterToken);

PALIMPORT
BOOL
PALAPI
PAL_NotifyRuntimeStarted();

static const int MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH = 64;

PALIMPORT
void
PALAPI
PAL_GetTransportPipeName(char *name, DWORD id, const char *suffix);

PALIMPORT
void
PALAPI
PAL_InitializeDebug(
    void);

PALIMPORT
HINSTANCE
PALAPI
PAL_RegisterModule(
    IN LPCSTR lpLibFileName);

PALIMPORT
VOID 
PALAPI
PAL_UnregisterModule(
    IN HINSTANCE hInstance);

PALIMPORT
BOOL
PALAPI
PAL_GetPALDirectoryW(
    OUT LPWSTR lpDirectoryName,
    IN OUT UINT* cchDirectoryName);
#ifdef UNICODE
#define PAL_GetPALDirectory PAL_GetPALDirectoryW
#else
#define PAL_GetPALDirectory PAL_GetPALDirectoryA
#endif

PALIMPORT
BOOL
PALAPI
PAL_Random(
    IN BOOL bStrong,
    IN OUT LPVOID lpBuffer,
    IN DWORD dwLength);

#ifdef PLATFORM_UNIX

PALIMPORT
DWORD
PALAPI
PAL_CreateExecWatchpoint(
    HANDLE hThread,
    PVOID pvInstruction
    );

PALIMPORT
DWORD
PALAPI
PAL_DeleteExecWatchpoint(
    HANDLE hThread,
    PVOID pvInstruction
    );

#endif


/******************* winuser.h Entrypoints *******************************/

PALIMPORT
LPSTR
PALAPI
CharNextA(
            IN LPCSTR lpsz);

PALIMPORT
LPSTR
PALAPI
CharNextExA(
        IN WORD CodePage,
        IN LPCSTR lpCurrentChar,
        IN DWORD dwFlags);

#ifndef UNICODE
#define CharNext CharNextA
#define CharNextEx CharNextExA
#endif


PALIMPORT
int
PALAPIV
wsprintfA(
      OUT LPSTR,
      IN LPCSTR,
      ...);

PALIMPORT
int
PALAPIV
wsprintfW(
      OUT LPWSTR,
      IN LPCWSTR,
      ...);

#ifdef UNICODE
#define wsprintf wsprintfW
#else
#define wsprintf wsprintfA
#endif

#define MB_OK                   0x00000000L
#define MB_OKCANCEL             0x00000001L
#define MB_ABORTRETRYIGNORE     0x00000002L
#define MB_YESNO                0x00000004L
#define MB_RETRYCANCEL          0x00000005L

#define MB_ICONHAND             0x00000010L
#define MB_ICONQUESTION         0x00000020L
#define MB_ICONEXCLAMATION      0x00000030L
#define MB_ICONASTERISK         0x00000040L

#define MB_ICONINFORMATION      MB_ICONASTERISK
#define MB_ICONSTOP             MB_ICONHAND
#define MB_ICONERROR            MB_ICONHAND

#define MB_DEFBUTTON1           0x00000000L
#define MB_DEFBUTTON2           0x00000100L
#define MB_DEFBUTTON3           0x00000200L

#define MB_SYSTEMMODAL          0x00001000L
#define MB_TASKMODAL            0x00002000L
#define MB_SETFOREGROUND        0x00010000L
#define MB_TOPMOST              0x00040000L

#define MB_NOFOCUS                  0x00008000L
#define MB_SETFOREGROUND            0x00010000L
#define MB_DEFAULT_DESKTOP_ONLY     0x00020000L

// Note: this is the NT 4.0 and greater value.
#define MB_SERVICE_NOTIFICATION 0x00200000L

#define MB_TYPEMASK             0x0000000FL
#define MB_ICONMASK             0x000000F0L
#define MB_DEFMASK              0x00000F00L

#define IDOK                    1
#define IDCANCEL                2
#define IDABORT                 3
#define IDRETRY                 4
#define IDIGNORE                5
#define IDYES                   6
#define IDNO                    7


PALIMPORT
int
PALAPI
MessageBoxW(
        IN LPVOID hWnd,  // NOTE: diff from winuser.h
        IN LPCWSTR lpText,
        IN LPCWSTR lpCaption,
        IN UINT uType);


#ifdef UNICODE
#define MessageBox MessageBoxW
#else
#define MessageBox MessageBoxA
#endif

/***************** wincon.h Entrypoints **********************************/

#define CTRL_C_EVENT        0
#define CTRL_BREAK_EVENT    1
#define CTRL_CLOSE_EVENT    2
// 3 is reserved!
// 4 is reserved!
#define CTRL_LOGOFF_EVENT   5
#define CTRL_SHUTDOWN_EVENT 6

typedef
BOOL
(PALAPI *PHANDLER_ROUTINE)(
    DWORD CtrlType
    );

#ifndef CORECLR
PALIMPORT
BOOL
PALAPI
GenerateConsoleCtrlEvent(
    IN DWORD dwCtrlEvent,
    IN DWORD dwProcessGroupId
    );
#endif // !CORECLR

//end wincon.h Entrypoints

// From win32.h
#ifndef _CRTIMP
#ifdef __llvm__
#define _CRTIMP
#else // __llvm__
#define _CRTIMP __declspec(dllimport)
#endif // __llvm__
#endif // _CRTIMP

/******************* winbase.h Entrypoints and defines ************************/
PALIMPORT
BOOL
PALAPI
AreFileApisANSI(
        VOID);

typedef struct _SECURITY_ATTRIBUTES {
            DWORD nLength;
            LPVOID lpSecurityDescriptor;
            BOOL bInheritHandle;
} SECURITY_ATTRIBUTES, *PSECURITY_ATTRIBUTES, *LPSECURITY_ATTRIBUTES;

#define _SH_DENYWR      0x20    /* deny write mode */

#define FILE_READ_DATA            ( 0x0001 )    // file & pipe
#define FILE_APPEND_DATA          ( 0x0004 )    // file

#define GENERIC_READ               (0x80000000L)
#define GENERIC_WRITE              (0x40000000L)

#define FILE_SHARE_READ            0x00000001
#define FILE_SHARE_WRITE           0x00000002
#define FILE_SHARE_DELETE          0x00000004

#define CREATE_NEW                 1
#define CREATE_ALWAYS              2
#define OPEN_EXISTING              3
#define OPEN_ALWAYS                4
#define TRUNCATE_EXISTING          5

#define FILE_ATTRIBUTE_READONLY                 0x00000001
#define FILE_ATTRIBUTE_HIDDEN                   0x00000002
#define FILE_ATTRIBUTE_SYSTEM                   0x00000004
#define FILE_ATTRIBUTE_DIRECTORY                0x00000010
#define FILE_ATTRIBUTE_ARCHIVE                  0x00000020
#define FILE_ATTRIBUTE_DEVICE                   0x00000040
#define FILE_ATTRIBUTE_NORMAL                   0x00000080

#define FILE_FLAG_WRITE_THROUGH    0x80000000
#define FILE_FLAG_NO_BUFFERING     0x20000000
#define FILE_FLAG_RANDOM_ACCESS    0x10000000
#define FILE_FLAG_SEQUENTIAL_SCAN  0x08000000
#define FILE_FLAG_BACKUP_SEMANTICS 0x02000000

#define FILE_BEGIN                 0
#define FILE_CURRENT               1
#define FILE_END                   2

#define STILL_ACTIVE (0x00000103L)

#define INVALID_SET_FILE_POINTER   ((DWORD)-1)


PALIMPORT
HANDLE
PALAPI
CreateFileW(
        IN LPCWSTR lpFileName,
        IN DWORD dwDesiredAccess,
        IN DWORD dwShareMode,
        IN LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        IN DWORD dwCreationDisposition,
        IN DWORD dwFlagsAndAttributes,
        IN HANDLE hTemplateFile);

#ifdef UNICODE
#define CreateFile CreateFileW
#else
#define CreateFile CreateFileA
#endif

PALIMPORT
BOOL
PALAPI
LockFile(
    IN HANDLE hFile,
    IN DWORD dwFileOffsetLow,
    IN DWORD dwFileOffsetHigh,
    IN DWORD nNumberOfBytesToLockLow,
    IN DWORD nNumberOfBytesToLockHigh
    );

PALIMPORT
BOOL
PALAPI
UnlockFile(
    IN HANDLE hFile,
    IN DWORD dwFileOffsetLow,
    IN DWORD dwFileOffsetHigh,
    IN DWORD nNumberOfBytesToUnlockLow,
    IN DWORD nNumberOfBytesToUnlockHigh
    );


PALIMPORT
DWORD
PALAPI
SearchPathW(
    IN LPCWSTR lpPath,
    IN LPCWSTR lpFileName,
    IN LPCWSTR lpExtension,
    IN DWORD nBufferLength,
    OUT LPWSTR lpBuffer,
    OUT LPWSTR *lpFilePart
    );
#ifdef UNICODE
#define SearchPath  SearchPathW
#else
#define SearchPath  SearchPathA
#endif // !UNICODE



PALIMPORT
BOOL
PALAPI
CopyFileW(
      IN LPCWSTR lpExistingFileName,
      IN LPCWSTR lpNewFileName,
      IN BOOL bFailIfExists);

#ifdef UNICODE
#define CopyFile CopyFileW
#else
#define CopyFile CopyFileA
#endif


PALIMPORT
BOOL
PALAPI
DeleteFileW(
        IN LPCWSTR lpFileName);

#ifdef UNICODE
#define DeleteFile DeleteFileW
#else
#define DeleteFile DeleteFileA
#endif



PALIMPORT
BOOL
PALAPI
MoveFileW(
     IN LPCWSTR lpExistingFileName,
     IN LPCWSTR lpNewFileName);

#ifdef UNICODE
#define MoveFile MoveFileW
#else
#define MoveFile MoveFileA
#endif

#define MOVEFILE_REPLACE_EXISTING      0x00000001
#define MOVEFILE_COPY_ALLOWED          0x00000002


PALIMPORT
BOOL
PALAPI
MoveFileExW(
        IN LPCWSTR lpExistingFileName,
        IN LPCWSTR lpNewFileName,
        IN DWORD dwFlags);

#ifdef UNICODE
#define MoveFileEx MoveFileExW
#else
#define MoveFileEx MoveFileExA
#endif

PALIMPORT
BOOL
PALAPI
CreateDirectoryW(
         IN LPCWSTR lpPathName,
         IN LPSECURITY_ATTRIBUTES lpSecurityAttributes);

#ifdef UNICODE
#define CreateDirectory CreateDirectoryW
#else
#define CreateDirectory CreateDirectoryA
#endif

PALIMPORT
BOOL
PALAPI
RemoveDirectoryW(
         IN LPCWSTR lpPathName);

#ifdef UNICODE
#define RemoveDirectory RemoveDirectoryW
#else
#define RemoveDirectory RemoveDirectoryA
#endif

typedef struct _BY_HANDLE_FILE_INFORMATION {  
    DWORD dwFileAttributes;
    FILETIME ftCreationTime;
    FILETIME ftLastAccessTime;
    FILETIME ftLastWriteTime;
    DWORD dwVolumeSerialNumber;
    DWORD nFileSizeHigh;
    DWORD nFileSizeLow;
    DWORD nNumberOfLinks;
    DWORD nFileIndexHigh;
    DWORD nFileIndexLow;
} BY_HANDLE_FILE_INFORMATION, *PBY_HANDLE_FILE_INFORMATION, *LPBY_HANDLE_FILE_INFORMATION;

typedef struct _WIN32_FIND_DATAA {
    DWORD dwFileAttributes;
    FILETIME ftCreationTime;
    FILETIME ftLastAccessTime;
    FILETIME ftLastWriteTime;
    DWORD nFileSizeHigh;
    DWORD nFileSizeLow;
    DWORD dwReserved0;
    DWORD dwReserved1;
    CHAR cFileName[ MAX_PATH_FNAME ];
    CHAR cAlternateFileName[ 14 ];
} WIN32_FIND_DATAA, *PWIN32_FIND_DATAA, *LPWIN32_FIND_DATAA;

typedef struct _WIN32_FIND_DATAW {
    DWORD dwFileAttributes;
    FILETIME ftCreationTime;
    FILETIME ftLastAccessTime;
    FILETIME ftLastWriteTime;
    DWORD nFileSizeHigh;
    DWORD nFileSizeLow;
    DWORD dwReserved0;
    DWORD dwReserved1;
    WCHAR cFileName[ MAX_PATH_FNAME ];
    WCHAR cAlternateFileName[ 14 ];
} WIN32_FIND_DATAW, *PWIN32_FIND_DATAW, *LPWIN32_FIND_DATAW;

#ifdef UNICODE
typedef WIN32_FIND_DATAW WIN32_FIND_DATA;
typedef PWIN32_FIND_DATAW PWIN32_FIND_DATA;
typedef LPWIN32_FIND_DATAW LPWIN32_FIND_DATA;
#else
typedef WIN32_FIND_DATAA WIN32_FIND_DATA;
typedef PWIN32_FIND_DATAA PWIN32_FIND_DATA;
typedef LPWIN32_FIND_DATAA LPWIN32_FIND_DATA;
#endif

PALIMPORT
HANDLE
PALAPI
FindFirstFileW(
           IN LPCWSTR lpFileName,
           OUT LPWIN32_FIND_DATAW lpFindFileData);

#ifdef UNICODE
#define FindFirstFile FindFirstFileW
#else
#define FindFirstFile FindFirstFileA
#endif

PALIMPORT
BOOL
PALAPI
FindNextFileW(
          IN HANDLE hFindFile,
          OUT LPWIN32_FIND_DATAW lpFindFileData);

#ifdef UNICODE
#define FindNextFile FindNextFileW
#else
#define FindNextFile FindNextFileA
#endif

PALIMPORT
BOOL
PALAPI
FindClose(
      IN OUT HANDLE hFindFile);

PALIMPORT
DWORD
PALAPI
GetFileAttributesW(
           IN LPCWSTR lpFileName);

#ifdef UNICODE
#define GetFileAttributes GetFileAttributesW
#else
#define GetFileAttributes GetFileAttributesA
#endif

typedef enum _GET_FILEEX_INFO_LEVELS {
  GetFileExInfoStandard
} GET_FILEEX_INFO_LEVELS;

typedef enum _FINDEX_INFO_LEVELS {
    FindExInfoStandard,
    FindExInfoBasic,
    FindExInfoMaxInfoLevel
} FINDEX_INFO_LEVELS;

typedef enum _FINDEX_SEARCH_OPS {
    FindExSearchNameMatch,
    FindExSearchLimitToDirectories,
    FindExSearchLimitToDevices,
    FindExSearchMaxSearchOp
} FINDEX_SEARCH_OPS;

typedef struct _WIN32_FILE_ATTRIBUTE_DATA {
    DWORD      dwFileAttributes;
    FILETIME   ftCreationTime;
    FILETIME   ftLastAccessTime;
    FILETIME   ftLastWriteTime;
    DWORD      nFileSizeHigh;
    DWORD      nFileSizeLow;
} WIN32_FILE_ATTRIBUTE_DATA, *LPWIN32_FILE_ATTRIBUTE_DATA;

PALIMPORT
BOOL
PALAPI
GetFileAttributesExW(
             IN LPCWSTR lpFileName,
             IN GET_FILEEX_INFO_LEVELS fInfoLevelId,
             OUT LPVOID lpFileInformation);

#ifdef UNICODE
#define GetFileAttributesEx GetFileAttributesExW
#endif

PALIMPORT
BOOL
PALAPI
SetFileAttributesW(
           IN LPCWSTR lpFileName,
           IN DWORD dwFileAttributes);

#ifdef UNICODE
#define SetFileAttributes SetFileAttributesW
#else
#define SetFileAttributes SetFileAttributesA
#endif

typedef LPVOID LPOVERLAPPED;  // diff from winbase.h

PALIMPORT
BOOL
PALAPI
WriteFile(
      IN HANDLE hFile,
      IN LPCVOID lpBuffer,
      IN DWORD nNumberOfBytesToWrite,
      OUT LPDWORD lpNumberOfBytesWritten,
      IN LPOVERLAPPED lpOverlapped);

PALIMPORT
BOOL
PALAPI
ReadFile(
     IN HANDLE hFile,
     OUT LPVOID lpBuffer,
     IN DWORD nNumberOfBytesToRead,
     OUT LPDWORD lpNumberOfBytesRead,
     IN LPOVERLAPPED lpOverlapped);

#define STD_INPUT_HANDLE         ((DWORD)-10)
#define STD_OUTPUT_HANDLE        ((DWORD)-11)
#define STD_ERROR_HANDLE         ((DWORD)-12)

PALIMPORT
HANDLE
PALAPI
GetStdHandle(
         IN DWORD nStdHandle);

PALIMPORT
BOOL
PALAPI
SetEndOfFile(
         IN HANDLE hFile);

PALIMPORT
DWORD
PALAPI
SetFilePointer(
           IN HANDLE hFile,
           IN LONG lDistanceToMove,
           IN PLONG lpDistanceToMoveHigh,
           IN DWORD dwMoveMethod);

PALIMPORT
BOOL
PALAPI
SetFilePointerEx(
           IN HANDLE hFile,
           IN LARGE_INTEGER liDistanceToMove,
           OUT PLARGE_INTEGER lpNewFilePointer,
           IN DWORD dwMoveMethod);

PALIMPORT
DWORD
PALAPI
GetFileSize(
        IN HANDLE hFile,
        OUT LPDWORD lpFileSizeHigh);

PALIMPORT
BOOL
PALAPI GetFileSizeEx(
        IN   HANDLE hFile,
        OUT  PLARGE_INTEGER lpFileSize);

PALIMPORT
BOOL
PALAPI
GetFileInformationByHandle(
        IN HANDLE hFile,
        OUT BY_HANDLE_FILE_INFORMATION* lpFileInformation);

PALIMPORT
LONG
PALAPI
CompareFileTime(
        IN CONST FILETIME *lpFileTime1,
        IN CONST FILETIME *lpFileTime2);

PALIMPORT
BOOL
PALAPI
SetFileTime(
        IN HANDLE hFile,
        IN CONST FILETIME *lpCreationTime,
        IN CONST FILETIME *lpLastAccessTime,
        IN CONST FILETIME *lpLastWriteTime);

PALIMPORT
BOOL
PALAPI
GetFileTime(
        IN HANDLE hFile,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpLastAccessTime,
        OUT LPFILETIME lpLastWriteTime);

PALIMPORT
VOID
PALAPI
GetSystemTimeAsFileTime(
            OUT LPFILETIME lpSystemTimeAsFileTime);

typedef struct _SYSTEMTIME {
    WORD wYear;
    WORD wMonth;
    WORD wDayOfWeek;
    WORD wDay;
    WORD wHour;
    WORD wMinute;
    WORD wSecond;
    WORD wMilliseconds;
} SYSTEMTIME, *PSYSTEMTIME, *LPSYSTEMTIME;

PALIMPORT
VOID
PALAPI
GetSystemTime(
          OUT LPSYSTEMTIME lpSystemTime);

PALIMPORT
BOOL
PALAPI
FileTimeToSystemTime(
            IN CONST FILETIME *lpFileTime,
            OUT LPSYSTEMTIME lpSystemTime);

PALIMPORT
BOOL
PALAPI
FileTimeToDosDateTime(
    IN CONST FILETIME *lpFileTime,
    OUT LPWORD lpFatDate,
    OUT LPWORD lpFatTime
    );



PALIMPORT
BOOL
PALAPI
FlushFileBuffers(
         IN HANDLE hFile);

#define FILE_TYPE_UNKNOWN         0x0000
#define FILE_TYPE_DISK            0x0001
#define FILE_TYPE_CHAR            0x0002
#define FILE_TYPE_PIPE            0x0003
#define FILE_TYPE_REMOTE          0x8000

PALIMPORT
DWORD
PALAPI
GetFileType(
        IN HANDLE hFile);

PALIMPORT
UINT
PALAPI
GetConsoleCP(
         VOID);

PALIMPORT
UINT
PALAPI
GetConsoleOutputCP(
           VOID);

PALIMPORT
DWORD
PALAPI
GetFullPathNameW(
         IN LPCWSTR lpFileName,
         IN DWORD nBufferLength,
         OUT LPWSTR lpBuffer,
         OUT LPWSTR *lpFilePart);

#ifdef UNICODE
#define GetFullPathName GetFullPathNameW
#else
#define GetFullPathName GetFullPathNameA
#endif

PALIMPORT
DWORD
PALAPI
GetLongPathNameW(
         IN LPCWSTR lpszShortPath,
                 OUT LPWSTR lpszLongPath,
         IN DWORD cchBuffer);

#ifdef UNICODE
#define GetLongPathName GetLongPathNameW
#endif

PALIMPORT
DWORD
PALAPI
GetShortPathNameW(
         IN LPCWSTR lpszLongPath,
                 OUT LPWSTR lpszShortPath,
         IN DWORD cchBuffer);

#ifdef UNICODE
#define GetShortPathName GetShortPathNameW
#endif


PALIMPORT
UINT
PALAPI
GetTempFileNameW(
         IN LPCWSTR lpPathName,
         IN LPCWSTR lpPrefixString,
         IN UINT uUnique,
         OUT LPWSTR lpTempFileName);

#ifdef UNICODE
#define GetTempFileName GetTempFileNameW
#else
#define GetTempFileName GetTempFileNameA
#endif

PALIMPORT
DWORD
PALAPI
GetTempPathW(
         IN DWORD nBufferLength,
         OUT LPWSTR lpBuffer);

#ifdef UNICODE
#define GetTempPath GetTempPathW
#else
#define GetTempPath GetTempPathA
#endif

PALIMPORT
DWORD
PALAPI
GetCurrentDirectoryW(
             IN DWORD nBufferLength,
             OUT LPWSTR lpBuffer);

#ifdef UNICODE
#define GetCurrentDirectory GetCurrentDirectoryW
#else
#define GetCurrentDirectory GetCurrentDirectoryA
#endif

PALIMPORT
BOOL
PALAPI
SetCurrentDirectoryW(
            IN LPCWSTR lpPathName);


#ifdef UNICODE
#define SetCurrentDirectory SetCurrentDirectoryW
#else
#define SetCurrentDirectory SetCurrentDirectoryA
#endif

// maximum length of the NETBIOS name (not including NULL)
#define MAX_COMPUTERNAME_LENGTH 15

// maximum length of the username (not including NULL)
#define UNLEN   256

PALIMPORT
BOOL
PALAPI
GetUserNameW(
    OUT LPWSTR lpBuffer,      // address of name buffer
    IN OUT LPDWORD nSize );   // address of size of name buffer

PALIMPORT
BOOL
PALAPI
GetComputerNameW(
    OUT LPWSTR lpBuffer,     // address of name buffer
    IN OUT LPDWORD nSize);   // address of size of name buffer

#ifdef UNICODE
#define GetUserName GetUserNameW
#define GetComputerName GetComputerNameW
#endif // UNICODE

PALIMPORT
HANDLE
PALAPI
CreateSemaphoreW(
         IN LPSECURITY_ATTRIBUTES lpSemaphoreAttributes,
         IN LONG lInitialCount,
         IN LONG lMaximumCount,
         IN LPCWSTR lpName);

PALIMPORT
HANDLE
PALAPI
CreateSemaphoreExW(
        IN LPSECURITY_ATTRIBUTES lpSemaphoreAttributes,
        IN LONG lInitialCount,
        IN LONG lMaximumCount,
        IN LPCWSTR lpName,
        IN /*_Reserved_*/  DWORD dwFlags,
        IN DWORD dwDesiredAccess);

PALIMPORT
HANDLE
PALAPI
OpenSemaphoreW(
    IN DWORD dwDesiredAccess,
    IN BOOL bInheritHandle,
    IN LPCWSTR lpName);

#ifdef UNICODE
#define CreateSemaphore CreateSemaphoreW
#define CreateSemaphoreEx CreateSemaphoreExW
#else
#define CreateSemaphore CreateSemaphoreA
#define CreateSemaphoreEx CreateSemaphoreExA
#endif

PALIMPORT
BOOL
PALAPI
ReleaseSemaphore(
         IN HANDLE hSemaphore,
         IN LONG lReleaseCount,
         OUT LPLONG lpPreviousCount);

PALIMPORT
HANDLE
PALAPI
CreateEventW(
         IN LPSECURITY_ATTRIBUTES lpEventAttributes,
         IN BOOL bManualReset,
         IN BOOL bInitialState,
         IN LPCWSTR lpName);

#ifdef UNICODE
#define CreateEvent CreateEventW
#else
#define CreateEvent CreateEventA
#endif

PALIMPORT
BOOL
PALAPI
SetEvent(
     IN HANDLE hEvent);

PALIMPORT
BOOL
PALAPI
ResetEvent(
       IN HANDLE hEvent);

PALIMPORT
HANDLE
PALAPI
OpenEventW(
       IN DWORD dwDesiredAccess,
       IN BOOL bInheritHandle,
       IN LPCWSTR lpName);

#ifdef UNICODE
#define OpenEvent OpenEventW
#endif

PALIMPORT
HANDLE
PALAPI
CreateMutexW(
    IN LPSECURITY_ATTRIBUTES lpMutexAttributes,
    IN BOOL bInitialOwner,
    IN LPCWSTR lpName);

#ifdef UNICODE
#define CreateMutex  CreateMutexW
#else
#define CreateMutex  CreateMutexA
#endif

PALIMPORT
HANDLE
PALAPI
OpenMutexW(
       IN DWORD dwDesiredAccess,
       IN BOOL bInheritHandle,
       IN LPCWSTR lpName);


#ifdef UNICODE
#define OpenMutex  OpenMutexW
#else
#define OpenMutex  OpenMutexA
#endif // UNICODE

PALIMPORT
BOOL
PALAPI
ReleaseMutex(
    IN HANDLE hMutex);

PALIMPORT
DWORD
PALAPI
GetCurrentProcessId(
            VOID);

PALIMPORT
DWORD
PALAPI
GetCurrentSessionId(
            VOID);

PALIMPORT
HANDLE
PALAPI
GetCurrentProcess(
          VOID);

PALIMPORT
DWORD
PALAPI
GetCurrentThreadId(
           VOID);

// To work around multiply-defined symbols in the Carbon framework.
#define GetCurrentThread PAL_GetCurrentThread
PALIMPORT
HANDLE
PALAPI
GetCurrentThread(
         VOID);


#define STARTF_USESTDHANDLES       0x00000100

typedef struct _STARTUPINFOW {
    DWORD cb;
    LPWSTR lpReserved_PAL_Undefined;
    LPWSTR lpDesktop_PAL_Undefined;
    LPWSTR lpTitle_PAL_Undefined;
    DWORD dwX_PAL_Undefined;
    DWORD dwY_PAL_Undefined;
    DWORD dwXSize_PAL_Undefined;
    DWORD dwYSize_PAL_Undefined;
    DWORD dwXCountChars_PAL_Undefined;
    DWORD dwYCountChars_PAL_Undefined;
    DWORD dwFillAttribute_PAL_Undefined;
    DWORD dwFlags;
    WORD wShowWindow_PAL_Undefined;
    WORD cbReserved2_PAL_Undefined;
    LPBYTE lpReserved2_PAL_Undefined;
    HANDLE hStdInput;
    HANDLE hStdOutput;
    HANDLE hStdError;
} STARTUPINFOW, *LPSTARTUPINFOW;

typedef struct _STARTUPINFOA {
    DWORD cb;
    LPSTR lpReserved_PAL_Undefined;
    LPSTR lpDesktop_PAL_Undefined;
    LPSTR lpTitle_PAL_Undefined;
    DWORD dwX_PAL_Undefined;
    DWORD dwY_PAL_Undefined;
    DWORD dwXSize_PAL_Undefined;
    DWORD dwYSize_PAL_Undefined;
    DWORD dwXCountChars_PAL_Undefined;
    DWORD dwYCountChars_PAL_Undefined;
    DWORD dwFillAttribute_PAL_Undefined;
    DWORD dwFlags;
    WORD wShowWindow_PAL_Undefined;
    WORD cbReserved2_PAL_Undefined;
    LPBYTE lpReserved2_PAL_Undefined;
    HANDLE hStdInput;
    HANDLE hStdOutput;
    HANDLE hStdError;
} STARTUPINFOA, *LPSTARTUPINFOA;

#ifdef UNICODE
typedef STARTUPINFOW STARTUPINFO;
typedef LPSTARTUPINFOW LPSTARTUPINFO;
#else
typedef STARTUPINFOA STARTUPINFO;
typedef LPSTARTUPINFOW LPSTARTUPINFO;
#endif

#define CREATE_NEW_CONSOLE          0x00000010

#define NORMAL_PRIORITY_CLASS             0x00000020

typedef struct _PROCESS_INFORMATION {
    HANDLE hProcess;
    HANDLE hThread;
    DWORD dwProcessId;
    DWORD dwThreadId_PAL_Undefined;
} PROCESS_INFORMATION, *PPROCESS_INFORMATION, *LPPROCESS_INFORMATION;

PALIMPORT
BOOL
PALAPI
CreateProcessW(
           IN LPCWSTR lpApplicationName,
           IN LPWSTR lpCommandLine,
           IN LPSECURITY_ATTRIBUTES lpProcessAttributes,
           IN LPSECURITY_ATTRIBUTES lpThreadAttributes,
           IN BOOL bInheritHandles,
           IN DWORD dwCreationFlags,
           IN LPVOID lpEnvironment,
           IN LPCWSTR lpCurrentDirectory,
           IN LPSTARTUPINFOW lpStartupInfo,
           OUT LPPROCESS_INFORMATION lpProcessInformation);

#ifdef UNICODE
#define CreateProcess CreateProcessW
#else
#define CreateProcess CreateProcessA
#endif

PALIMPORT
PAL_NORETURN
VOID
PALAPI
ExitProcess(
        IN UINT uExitCode);

PALIMPORT
BOOL
PALAPI
TerminateProcess(
         IN HANDLE hProcess,
         IN UINT uExitCode);

PALIMPORT
BOOL
PALAPI
GetExitCodeProcess(
           IN HANDLE hProcess,
           IN LPDWORD lpExitCode);

PALIMPORT
BOOL
PALAPI
GetProcessTimes(
        IN HANDLE hProcess,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpExitTime,
        OUT LPFILETIME lpKernelTime,
        OUT LPFILETIME lpUserTime);

#define MAXIMUM_WAIT_OBJECTS  64
#define WAIT_OBJECT_0 0
#define WAIT_ABANDONED   0x00000080
#define WAIT_ABANDONED_0 0x00000080
#define WAIT_TIMEOUT 258
#define WAIT_FAILED ((DWORD)0xFFFFFFFF)

#define INFINITE 0xFFFFFFFF // Infinite timeout

PALIMPORT
DWORD
PALAPI
WaitForSingleObject(
            IN HANDLE hHandle,
            IN DWORD dwMilliseconds);

PALIMPORT
DWORD
PALAPI
WaitForSingleObjectEx(
            IN HANDLE hHandle,
            IN DWORD dwMilliseconds,
            IN BOOL bAlertable);

PALIMPORT
DWORD
PALAPI
WaitForMultipleObjects(
               IN DWORD nCount,
               IN CONST HANDLE *lpHandles,
               IN BOOL bWaitAll,
               IN DWORD dwMilliseconds);

PALIMPORT
DWORD
PALAPI
WaitForMultipleObjectsEx(
             IN DWORD nCount,
             IN CONST HANDLE *lpHandles,
             IN BOOL bWaitAll,
             IN DWORD dwMilliseconds,
             IN BOOL bAlertable);

PALIMPORT
RHANDLE
PALAPI
PAL_LocalHandleToRemote(
            IN HANDLE hLocal);

PALIMPORT
HANDLE
PALAPI
PAL_RemoteHandleToLocal(
            IN RHANDLE hRemote);


#define DUPLICATE_CLOSE_SOURCE      0x00000001
#define DUPLICATE_SAME_ACCESS       0x00000002

PALIMPORT
BOOL
PALAPI
DuplicateHandle(
        IN HANDLE hSourceProcessHandle,
        IN HANDLE hSourceHandle,
        IN HANDLE hTargetProcessHandle,
        OUT LPHANDLE lpTargetHandle,
        IN DWORD dwDesiredAccess,
        IN BOOL bInheritHandle,
        IN DWORD dwOptions);

PALIMPORT
VOID
PALAPI
Sleep(
      IN DWORD dwMilliseconds);

PALIMPORT
DWORD
PALAPI
SleepEx(
    IN DWORD dwMilliseconds,
    IN BOOL bAlertable);

PALIMPORT
BOOL
PALAPI
SwitchToThread(
    VOID);

#define DEBUG_PROCESS                     0x00000001
#define DEBUG_ONLY_THIS_PROCESS           0x00000002
#define CREATE_SUSPENDED                  0x00000004
#define STACK_SIZE_PARAM_IS_A_RESERVATION 0x00010000

PALIMPORT
HANDLE
PALAPI
CreateThread(
         IN LPSECURITY_ATTRIBUTES lpThreadAttributes,
         IN DWORD dwStackSize,
         IN LPTHREAD_START_ROUTINE lpStartAddress,
         IN LPVOID lpParameter,
         IN DWORD dwCreationFlags,
         OUT LPDWORD lpThreadId);

PALIMPORT
PAL_NORETURN
VOID
PALAPI
ExitThread(
       IN DWORD dwExitCode);

PALIMPORT
BOOL
PALAPI
GetExitCodeThread(
           IN HANDLE hThread,
           IN LPDWORD lpExitCode);

PALIMPORT
DWORD
PALAPI
ResumeThread(
         IN HANDLE hThread);

typedef VOID (PALAPI *PAPCFUNC)(ULONG_PTR dwParam);

PALIMPORT
DWORD
PALAPI
QueueUserAPC(
         IN PAPCFUNC pfnAPC,
         IN HANDLE hThread,
         IN ULONG_PTR dwData);

#ifdef _X86_

//
// ***********************************************************************************
//
// NOTE: These context definitions are replicated in ndp/clr/src/debug/inc/DbgTargetContext.h (for the
// purposes manipulating contexts from different platforms during remote debugging). Be sure to keep those
// definitions in sync if you make any changes here.
//
// ***********************************************************************************
//

#define SIZE_OF_80387_REGISTERS      80

#define CONTEXT_i386            0x00010000
#define CONTEXT_CONTROL         (CONTEXT_i386 | 0x00000001L) // SS:SP, CS:IP, FLAGS, BP
#define CONTEXT_INTEGER         (CONTEXT_i386 | 0x00000002L) // AX, BX, CX, DX, SI, DI
#define CONTEXT_SEGMENTS        (CONTEXT_i386 | 0x00000004L)
#define CONTEXT_FLOATING_POINT  (CONTEXT_i386 | 0x00000008L) // 387 state
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_i386 | 0x00000010L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS)
#define CONTEXT_EXTENDED_REGISTERS  (CONTEXT_i386 | 0x00000020L)
#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS)

#define MAXIMUM_SUPPORTED_EXTENSION     512

typedef struct _FLOATING_SAVE_AREA {
    DWORD   ControlWord;
    DWORD   StatusWord;
    DWORD   TagWord;
    DWORD   ErrorOffset;
    DWORD   ErrorSelector;
    DWORD   DataOffset;
    DWORD   DataSelector;
    BYTE    RegisterArea[SIZE_OF_80387_REGISTERS];
    DWORD   Cr0NpxState;
} FLOATING_SAVE_AREA;

typedef FLOATING_SAVE_AREA *PFLOATING_SAVE_AREA;

typedef struct _CONTEXT {
    ULONG ContextFlags;

    ULONG   Dr0_PAL_Undefined;
    ULONG   Dr1_PAL_Undefined;
    ULONG   Dr2_PAL_Undefined;
    ULONG   Dr3_PAL_Undefined;
    ULONG   Dr6_PAL_Undefined;
    ULONG   Dr7_PAL_Undefined;

    FLOATING_SAVE_AREA FloatSave;

    ULONG   SegGs_PAL_Undefined;
    ULONG   SegFs_PAL_Undefined;
    ULONG   SegEs_PAL_Undefined;
    ULONG   SegDs_PAL_Undefined;

    ULONG   Edi;
    ULONG   Esi;
    ULONG   Ebx;
    ULONG   Edx;
    ULONG   Ecx;
    ULONG   Eax;

    ULONG   Ebp;
    ULONG   Eip;
    ULONG   SegCs;
    ULONG   EFlags;
    ULONG   Esp;
    ULONG   SegSs;

    UCHAR   ExtendedRegisters[MAXIMUM_SUPPORTED_EXTENSION];

} CONTEXT, *PCONTEXT, *LPCONTEXT;

// To support saving and loading xmm register context we need to know the offset in the ExtendedRegisters
// section at which they are stored. This has been determined experimentally since I have found no
// documentation thus far but it corresponds to the offset we'd expect if a fxsave instruction was used to
// store the regular FP state along with the XMM registers at the start of the extended registers section.
// Technically the offset doesn't really matter if no code in the PAL or runtime knows what the offset should
// be either (as long as we're consistent across GetThreadContext() and SetThreadContext() and we don't
// support any other values in the ExtendedRegisters) but we might as well be as accurate as we can.
#define CONTEXT_EXREG_XMM_OFFSET 160

#elif defined(_PPC_)

//
// ***********************************************************************************
//
// NOTE: These context definitions are replicated in ndp/clr/src/debug/inc/DbgTargetContext.h (for the
// purposes manipulating contexts from different platforms during remote debugging). Be sure to keep those
// definitions in sync if you make any changes here.
//
// ***********************************************************************************
//

#define CONTEXT_CONTROL         0x00000001L
#define CONTEXT_FLOATING_POINT  0x00000002L
#define CONTEXT_INTEGER         0x00000004L

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_FLOATING_POINT | CONTEXT_INTEGER)
#define CONTEXT_ALL CONTEXT_FULL

typedef struct _CONTEXT {

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_FLOATING_POINT.
    //

    double Fpr0;                        // Floating registers 0..31
    double Fpr1;
    double Fpr2;
    double Fpr3;
    double Fpr4;
    double Fpr5;
    double Fpr6;
    double Fpr7;
    double Fpr8;
    double Fpr9;
    double Fpr10;
    double Fpr11;
    double Fpr12;
    double Fpr13;
    double Fpr14;
    double Fpr15;
    double Fpr16;
    double Fpr17;
    double Fpr18;
    double Fpr19;
    double Fpr20;
    double Fpr21;
    double Fpr22;
    double Fpr23;
    double Fpr24;
    double Fpr25;
    double Fpr26;
    double Fpr27;
    double Fpr28;
    double Fpr29;
    double Fpr30;
    double Fpr31;
    double Fpscr;                       // Floating point status/control reg

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_INTEGER.
    //

    ULONG Gpr0;                         // General registers 0..31
    ULONG Gpr1;                         // StackPointer
    ULONG Gpr2;
    ULONG Gpr3;
    ULONG Gpr4;
    ULONG Gpr5;
    ULONG Gpr6;
    ULONG Gpr7;
    ULONG Gpr8;
    ULONG Gpr9;
    ULONG Gpr10;
    ULONG Gpr11;
    ULONG Gpr12;
    ULONG Gpr13;
    ULONG Gpr14;
    ULONG Gpr15;
    ULONG Gpr16;
    ULONG Gpr17;
    ULONG Gpr18;
    ULONG Gpr19;
    ULONG Gpr20;
    ULONG Gpr21;
    ULONG Gpr22;
    ULONG Gpr23;
    ULONG Gpr24;
    ULONG Gpr25;
    ULONG Gpr26;
    ULONG Gpr27;
    ULONG Gpr28;
    ULONG Gpr29;
    ULONG Gpr30;
    ULONG Gpr31;

    ULONG Cr;                           // Condition register
    ULONG Xer;                          // Fixed point exception register

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_CONTROL.
    //

    ULONG Msr;                          // Machine status register
    ULONG Iar;                          // Instruction address register
    ULONG Lr;                           // Link register
    ULONG Ctr;                          // Count register

    //
    // The flags values within this flag control the contents of
    // a CONTEXT record.
    //
    // If the context record is used as an input parameter, then
    // for each portion of the context record controlled by a flag
    // whose value is set, it is assumed that that portion of the
    // context record contains valid context. If the context record
    // is being used to modify a thread's context, then only that
    // portion of the threads context will be modified.
    //
    // If the context record is used as an IN OUT parameter to capture
    // the context of a thread, then only those portions of the thread's
    // context corresponding to set flags will be returned.
    //
    // The context record is never used as an OUT only parameter.
    //

    ULONG ContextFlags;

    ULONG Fill[3];                      // Pad out to multiple of 16 bytes

    //
    // This section is specified/returned if CONTEXT_DEBUG_REGISTERS is
    // set in ContextFlags.  Note that CONTEXT_DEBUG_REGISTERS is NOT
    // included in CONTEXT_FULL.
    //
    ULONG Dr0;                          // Breakpoint Register 1
    ULONG Dr1;                          // Breakpoint Register 2
    ULONG Dr2;                          // Breakpoint Register 3
    ULONG Dr3;                          // Breakpoint Register 4
    ULONG Dr4;                          // Breakpoint Register 5
    ULONG Dr5;                          // Breakpoint Register 6
    ULONG Dr6;                          // Debug Status Register
    ULONG Dr7;                          // Debug Control Register

} CONTEXT, *PCONTEXT, *LPCONTEXT;

#elif defined(_SPARC_)

#define CONTEXT_CONTROL         0x00000001L
#define CONTEXT_FLOATING_POINT  0x00000002L
#define CONTEXT_INTEGER         0x00000004L

#define COUNT_FLOATING_REGISTER 32
#define COUNT_DOUBLE_REGISTER 16
#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_FLOATING_POINT | CONTEXT_INTEGER)
#define CONTEXT_ALL CONTEXT_FULL

typedef struct _CONTEXT {
    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_INTEGER.
    //
    ULONG g0;
    ULONG g1;
    ULONG g2;
    ULONG g3;
    ULONG g4;
    ULONG g5;
    ULONG g6;
    ULONG g7;
    ULONG o0;
    ULONG o1;
    ULONG o2;
    ULONG o3;
    ULONG o4;
    ULONG o5;
    ULONG sp;
    ULONG o7;
    ULONG l0;
    ULONG l1;
    ULONG l2;
    ULONG l3;
    ULONG l4;
    ULONG l5;
    ULONG l6;
    ULONG l7;
    ULONG i0;
    ULONG i1;
    ULONG i2;
    ULONG i3;
    ULONG i4;
    ULONG i5;
    ULONG fp;
    ULONG i7;

    ULONG y;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_CONTROL.
    //
#if defined(__sparcv9)
    ULONG ccr;
#else
    ULONG psr;
#endif
    ULONG pc;     // program counter
    ULONG npc;    // next address to be executed

    ULONG ContextFlags;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_FLOATING_POINT.
    //
    ULONGLONG fsr;
    union {
        float f[COUNT_FLOATING_REGISTER];
        double d[COUNT_DOUBLE_REGISTER];
        } fprs;

} CONTEXT, *PCONTEXT, *LPCONTEXT;

#elif defined(_PARISC_)

// ToDo: Get this correct for PARISC architecture
#define CONTEXT_CONTROL         0x00000001L
#define CONTEXT_FLOATING_POINT  0x00000002L
#define CONTEXT_INTEGER         0x00000004L

#define COUNT_FLOATING_REGISTER 32
#define COUNT_DOUBLE_REGISTER 16
#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_FLOATING_POINT | CONTEXT_INTEGER)
#define CONTEXT_ALL CONTEXT_FULL

typedef struct _CONTEXT {
    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_INTEGER.
    //
    ULONG g0;
    ULONG g1;
    ULONG g2;
    ULONG g3;
    ULONG g4;
    ULONG g5;
    ULONG g6;
    ULONG g7;
    ULONG o0;
    ULONG o1;
    ULONG o2;
    ULONG o3;
    ULONG o4;
    ULONG o5;
    ULONG sp;
    ULONG o7;
    ULONG l0;
    ULONG l1;
    ULONG l2;
    ULONG l3;
    ULONG l4;
    ULONG l5;
    ULONG l6;
    ULONG l7;
    ULONG i0;
    ULONG i1;
    ULONG i2;
    ULONG i3;
    ULONG i4;
    ULONG i5;
    ULONG fp;
    ULONG i7;

    ULONG y;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_CONTROL.
    //
    ULONG psr;
    ULONG pc;     // program counter
    ULONG npc;    // next address to be executed

    ULONG ContextFlags;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_FLOATING_POINT.
    //
    ULONGLONG fsr;
    union {
        float f[COUNT_FLOATING_REGISTER];
        double d[COUNT_DOUBLE_REGISTER];
        } fprs;

} CONTEXT, *PCONTEXT, *LPCONTEXT;

#elif defined(_IA64_)

// copied from winnt.h
typedef struct _FLOAT128 {
    __int64 LowPart;
    __int64 HighPart;
} FLOAT128;

typedef FLOAT128 *PFLOAT128;

// begin_ntddk begin_nthal

//
// The following flags control the contents of the CONTEXT structure.
//

#if !defined(RC_INVOKED)

#define CONTEXT_IA64                    0x00080000

#define CONTEXT_CONTROL                 (CONTEXT_IA64 | 0x00000001L)
#define CONTEXT_LOWER_FLOATING_POINT    (CONTEXT_IA64 | 0x00000002L)
#define CONTEXT_HIGHER_FLOATING_POINT   (CONTEXT_IA64 | 0x00000004L)
#define CONTEXT_INTEGER                 (CONTEXT_IA64 | 0x00000008L)
#define CONTEXT_DEBUG                   (CONTEXT_IA64 | 0x00000010L)
#define CONTEXT_IA32_CONTROL            (CONTEXT_IA64 | 0x00000020L)  // Includes StIPSR


#define CONTEXT_FLOATING_POINT          (CONTEXT_LOWER_FLOATING_POINT | CONTEXT_HIGHER_FLOATING_POINT)
#define CONTEXT_FULL                    (CONTEXT_CONTROL | CONTEXT_FLOATING_POINT | CONTEXT_INTEGER | CONTEXT_IA32_CONTROL)
#define CONTEXT_ALL                     (CONTEXT_CONTROL | CONTEXT_FLOATING_POINT | CONTEXT_INTEGER | CONTEXT_DEBUG | CONTEXT_IA32_CONTROL)

#define CONTEXT_EXCEPTION_ACTIVE        0x8000000
#define CONTEXT_SERVICE_ACTIVE          0x10000000
#define CONTEXT_EXCEPTION_REQUEST       0x40000000
#define CONTEXT_EXCEPTION_REPORTING     0x80000000

#endif // !defined(RC_INVOKED)

//
// Context Frame
//
//  This frame has a several purposes: 1) it is used as an argument to
//  NtContinue, 2) it is used to construct a call frame for APC delivery,
//  3) it is used to construct a call frame for exception dispatching
//  in user mode, 4) it is used in the user level thread creation
//  routines, and 5) it is used to to pass thread state to debuggers.
//
//  N.B. Because this record is used as a call frame, it must be EXACTLY
//  a multiple of 16 bytes in length and aligned on a 16-byte boundary.
//

typedef struct _CONTEXT {

    //
    // The flags values within this flag control the contents of
    // a CONTEXT record.
    //
    // If the context record is used as an input parameter, then
    // for each portion of the context record controlled by a flag
    // whose value is set, it is assumed that that portion of the
    // context record contains valid context. If the context record
    // is being used to modify a thread's context, then only that
    // portion of the threads context will be modified.
    //
    // If the context record is used as an IN OUT parameter to capture
    // the context of a thread, then only those portions of the thread's
    // context corresponding to set flags will be returned.
    //
    // The context record is never used as an OUT only parameter.
    //

    DWORD ContextFlags;
    DWORD Fill1[3];         // for alignment of following on 16-byte boundary

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_DEBUG.
    //
    // N.B. CONTEXT_DEBUG is *not* part of CONTEXT_FULL.
    //

    ULONGLONG DbI0;
    ULONGLONG DbI1;
    ULONGLONG DbI2;
    ULONGLONG DbI3;
    ULONGLONG DbI4;
    ULONGLONG DbI5;
    ULONGLONG DbI6;
    ULONGLONG DbI7;

    ULONGLONG DbD0;
    ULONGLONG DbD1;
    ULONGLONG DbD2;
    ULONGLONG DbD3;
    ULONGLONG DbD4;
    ULONGLONG DbD5;
    ULONGLONG DbD6;
    ULONGLONG DbD7;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_LOWER_FLOATING_POINT.
    //

    FLOAT128 FltS0;
    FLOAT128 FltS1;
    FLOAT128 FltS2;
    FLOAT128 FltS3;
    FLOAT128 FltT0;
    FLOAT128 FltT1;
    FLOAT128 FltT2;
    FLOAT128 FltT3;
    FLOAT128 FltT4;
    FLOAT128 FltT5;
    FLOAT128 FltT6;
    FLOAT128 FltT7;
    FLOAT128 FltT8;
    FLOAT128 FltT9;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_HIGHER_FLOATING_POINT.
    //

    FLOAT128 FltS4;
    FLOAT128 FltS5;
    FLOAT128 FltS6;
    FLOAT128 FltS7;
    FLOAT128 FltS8;
    FLOAT128 FltS9;
    FLOAT128 FltS10;
    FLOAT128 FltS11;
    FLOAT128 FltS12;
    FLOAT128 FltS13;
    FLOAT128 FltS14;
    FLOAT128 FltS15;
    FLOAT128 FltS16;
    FLOAT128 FltS17;
    FLOAT128 FltS18;
    FLOAT128 FltS19;

    FLOAT128 FltF32;
    FLOAT128 FltF33;
    FLOAT128 FltF34;
    FLOAT128 FltF35;
    FLOAT128 FltF36;
    FLOAT128 FltF37;
    FLOAT128 FltF38;
    FLOAT128 FltF39;

    FLOAT128 FltF40;
    FLOAT128 FltF41;
    FLOAT128 FltF42;
    FLOAT128 FltF43;
    FLOAT128 FltF44;
    FLOAT128 FltF45;
    FLOAT128 FltF46;
    FLOAT128 FltF47;
    FLOAT128 FltF48;
    FLOAT128 FltF49;

    FLOAT128 FltF50;
    FLOAT128 FltF51;
    FLOAT128 FltF52;
    FLOAT128 FltF53;
    FLOAT128 FltF54;
    FLOAT128 FltF55;
    FLOAT128 FltF56;
    FLOAT128 FltF57;
    FLOAT128 FltF58;
    FLOAT128 FltF59;

    FLOAT128 FltF60;
    FLOAT128 FltF61;
    FLOAT128 FltF62;
    FLOAT128 FltF63;
    FLOAT128 FltF64;
    FLOAT128 FltF65;
    FLOAT128 FltF66;
    FLOAT128 FltF67;
    FLOAT128 FltF68;
    FLOAT128 FltF69;

    FLOAT128 FltF70;
    FLOAT128 FltF71;
    FLOAT128 FltF72;
    FLOAT128 FltF73;
    FLOAT128 FltF74;
    FLOAT128 FltF75;
    FLOAT128 FltF76;
    FLOAT128 FltF77;
    FLOAT128 FltF78;
    FLOAT128 FltF79;

    FLOAT128 FltF80;
    FLOAT128 FltF81;
    FLOAT128 FltF82;
    FLOAT128 FltF83;
    FLOAT128 FltF84;
    FLOAT128 FltF85;
    FLOAT128 FltF86;
    FLOAT128 FltF87;
    FLOAT128 FltF88;
    FLOAT128 FltF89;

    FLOAT128 FltF90;
    FLOAT128 FltF91;
    FLOAT128 FltF92;
    FLOAT128 FltF93;
    FLOAT128 FltF94;
    FLOAT128 FltF95;
    FLOAT128 FltF96;
    FLOAT128 FltF97;
    FLOAT128 FltF98;
    FLOAT128 FltF99;

    FLOAT128 FltF100;
    FLOAT128 FltF101;
    FLOAT128 FltF102;
    FLOAT128 FltF103;
    FLOAT128 FltF104;
    FLOAT128 FltF105;
    FLOAT128 FltF106;
    FLOAT128 FltF107;
    FLOAT128 FltF108;
    FLOAT128 FltF109;

    FLOAT128 FltF110;
    FLOAT128 FltF111;
    FLOAT128 FltF112;
    FLOAT128 FltF113;
    FLOAT128 FltF114;
    FLOAT128 FltF115;
    FLOAT128 FltF116;
    FLOAT128 FltF117;
    FLOAT128 FltF118;
    FLOAT128 FltF119;

    FLOAT128 FltF120;
    FLOAT128 FltF121;
    FLOAT128 FltF122;
    FLOAT128 FltF123;
    FLOAT128 FltF124;
    FLOAT128 FltF125;
    FLOAT128 FltF126;
    FLOAT128 FltF127;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_LOWER_FLOATING_POINT | CONTEXT_HIGHER_FLOATING_POINT | CONTEXT_CONTROL.
    //

    ULONGLONG StFPSR;       //  FP status

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_INTEGER.
    //
    // N.B. The registers gp, sp, rp are part of the control context
    //

    ULONGLONG IntGp;        //  r1, volatile
    ULONGLONG IntT0;        //  r2-r3, volatile
    ULONGLONG IntT1;        //
    ULONGLONG IntS0;        //  r4-r7, preserved
    ULONGLONG IntS1;
    ULONGLONG IntS2;
    ULONGLONG IntS3;
    ULONGLONG IntV0;        //  r8, volatile
    ULONGLONG IntT2;        //  r9-r11, volatile
    ULONGLONG IntT3;
    ULONGLONG IntT4;
    ULONGLONG IntSp;        //  stack pointer (r12), special
    ULONGLONG IntTeb;       //  teb (r13), special
    ULONGLONG IntT5;        //  r14-r31, volatile
    ULONGLONG IntT6;
    ULONGLONG IntT7;
    ULONGLONG IntT8;
    ULONGLONG IntT9;
    ULONGLONG IntT10;
    ULONGLONG IntT11;
    ULONGLONG IntT12;
    ULONGLONG IntT13;
    ULONGLONG IntT14;
    ULONGLONG IntT15;
    ULONGLONG IntT16;
    ULONGLONG IntT17;
    ULONGLONG IntT18;
    ULONGLONG IntT19;
    ULONGLONG IntT20;
    ULONGLONG IntT21;
    ULONGLONG IntT22;

    ULONGLONG IntNats;      //  Nat bits for r1-r31
                            //  r1-r31 in bits 1 thru 31.
    ULONGLONG Preds;        //  predicates, preserved

    ULONGLONG BrRp;         //  return pointer, b0, preserved
    ULONGLONG BrS0;         //  b1-b5, preserved
    ULONGLONG BrS1;
    ULONGLONG BrS2;
    ULONGLONG BrS3;
    ULONGLONG BrS4;
    ULONGLONG BrT0;         //  b6-b7, volatile
    ULONGLONG BrT1;

    //
    // This section is specified/returned if the ContextFlags word contains
    // the flag CONTEXT_CONTROL.
    //

    // Other application registers
    ULONGLONG ApUNAT;       //  User Nat collection register, preserved
    ULONGLONG ApLC;         //  Loop counter register, preserved
    ULONGLONG ApEC;         //  Epilog counter register, preserved
    ULONGLONG ApCCV;        //  CMPXCHG value register, volatile
    ULONGLONG ApDCR;        //  Default control register (TBD)

    // Register stack info
    ULONGLONG RsPFS;        //  Previous function state, preserved
    ULONGLONG RsBSP;        //  Backing store pointer, preserved
    ULONGLONG RsBSPSTORE;
    ULONGLONG RsRSC;        //  RSE configuration, volatile
    ULONGLONG RsRNAT;       //  RSE Nat collection register, preserved

    // Trap Status Information
    ULONGLONG StIPSR;       //  Interruption Processor Status
    ULONGLONG StIIP;        //  Interruption IP
    ULONGLONG StIFS;        //  Interruption Function State

    // iA32 related control registers
    ULONGLONG StFCR;        //  copy of Ar21
    ULONGLONG Eflag;        //  Eflag copy of Ar24
    ULONGLONG SegCSD;       //  iA32 CSDescriptor (Ar25)
    ULONGLONG SegSSD;       //  iA32 SSDescriptor (Ar26)
    ULONGLONG Cflag;        //  Cr0+Cr4 copy of Ar27
    ULONGLONG StFSR;        //  x86 FP status (copy of AR28)
    ULONGLONG StFIR;        //  x86 FP status (copy of AR29)
    ULONGLONG StFDR;        //  x86 FP status (copy of AR30)

      ULONGLONG UNUSEDPACK;   //  added to pack StFDR to 16-bytes

} CONTEXT, *PCONTEXT, *LPCONTEXT;
#elif defined(_AMD64_)
// copied from winnt.h

#define CONTEXT_AMD64   0x100000

#define CONTEXT_CONTROL (CONTEXT_AMD64 | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_AMD64 | 0x2L)
#define CONTEXT_SEGMENTS (CONTEXT_AMD64 | 0x4L)
#define CONTEXT_FLOATING_POINT  (CONTEXT_AMD64 | 0x8L)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_AMD64 | 0x10L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS)

#define CONTEXT_XSTATE (CONTEXT_AMD64 | 0x40L)

#define CONTEXT_EXCEPTION_ACTIVE 0x8000000
#define CONTEXT_SERVICE_ACTIVE 0x10000000
#define CONTEXT_EXCEPTION_REQUEST 0x40000000
#define CONTEXT_EXCEPTION_REPORTING 0x80000000

typedef struct DECLSPEC_ALIGN(16) _M128A {
    ULONGLONG Low;
    LONGLONG High;
} M128A, *PM128A;

typedef struct _XMM_SAVE_AREA32 {
    WORD   ControlWord;
    WORD   StatusWord;
    BYTE  TagWord;
    BYTE  Reserved1;
    WORD   ErrorOpcode;
    DWORD ErrorOffset;
    WORD   ErrorSelector;
    WORD   Reserved2;
    DWORD DataOffset;
    WORD   DataSelector;
    WORD   Reserved3;
    DWORD MxCsr;
    DWORD MxCsr_Mask;
    M128A FloatRegisters[8];
    M128A XmmRegisters[16];
    BYTE  Reserved4[96];
} XMM_SAVE_AREA32, *PXMM_SAVE_AREA32;

#define LEGACY_SAVE_AREA_LENGTH sizeof(XMM_SAVE_AREA32)

//
// Context Frame
//
//  This frame has a several purposes: 1) it is used as an argument to
//  NtContinue, 2) is is used to constuct a call frame for APC delivery,
//  and 3) it is used in the user level thread creation routines.
//
//
// The flags field within this record controls the contents of a CONTEXT
// record.
//
// If the context record is used as an input parameter, then for each
// portion of the context record controlled by a flag whose value is
// set, it is assumed that that portion of the context record contains
// valid context. If the context record is being used to modify a threads
// context, then only that portion of the threads context is modified.
//
// If the context record is used as an output parameter to capture the
// context of a thread, then only those portions of the thread's context
// corresponding to set flags will be returned.
//
// CONTEXT_CONTROL specifies SegSs, Rsp, SegCs, Rip, and EFlags.
//
// CONTEXT_INTEGER specifies Rax, Rcx, Rdx, Rbx, Rbp, Rsi, Rdi, and R8-R15.
//
// CONTEXT_SEGMENTS specifies SegDs, SegEs, SegFs, and SegGs.
//
// CONTEXT_DEBUG_REGISTERS specifies Dr0-Dr3 and Dr6-Dr7.
//
// CONTEXT_MMX_REGISTERS specifies the floating point and extended registers
//     Mm0/St0-Mm7/St7 and Xmm0-Xmm15).
//

typedef struct DECLSPEC_ALIGN(16) _CONTEXT {

    //
    // Register parameter home addresses.
    //
    // N.B. These fields are for convience - they could be used to extend the
    //      context record in the future.
    //

    DWORD64 P1Home;
    DWORD64 P2Home;
    DWORD64 P3Home;
    DWORD64 P4Home;
    DWORD64 P5Home;
    DWORD64 P6Home;

    //
    // Control flags.
    //

    DWORD ContextFlags;
    DWORD MxCsr;

    //
    // Segment Registers and processor flags.
    //

    WORD   SegCs;
    WORD   SegDs;
    WORD   SegEs;
    WORD   SegFs;
    WORD   SegGs;
    WORD   SegSs;
    DWORD EFlags;

    //
    // Debug registers
    //

    DWORD64 Dr0;
    DWORD64 Dr1;
    DWORD64 Dr2;
    DWORD64 Dr3;
    DWORD64 Dr6;
    DWORD64 Dr7;

    //
    // Integer registers.
    //

    DWORD64 Rax;
    DWORD64 Rcx;
    DWORD64 Rdx;
    DWORD64 Rbx;
    DWORD64 Rsp;
    DWORD64 Rbp;
    DWORD64 Rsi;
    DWORD64 Rdi;
    DWORD64 R8;
    DWORD64 R9;
    DWORD64 R10;
    DWORD64 R11;
    DWORD64 R12;
    DWORD64 R13;
    DWORD64 R14;
    DWORD64 R15;

    //
    // Program counter.
    //

    DWORD64 Rip;

    //
    // Floating point state.
    //

    union {
        XMM_SAVE_AREA32 FltSave;
        struct {
            M128A Header[2];
            M128A Legacy[8];
            M128A Xmm0;
            M128A Xmm1;
            M128A Xmm2;
            M128A Xmm3;
            M128A Xmm4;
            M128A Xmm5;
            M128A Xmm6;
            M128A Xmm7;
            M128A Xmm8;
            M128A Xmm9;
            M128A Xmm10;
            M128A Xmm11;
            M128A Xmm12;
            M128A Xmm13;
            M128A Xmm14;
            M128A Xmm15;
        };
    };

    //
    // Vector registers.
    //

    M128A VectorRegister[26];
    DWORD64 VectorControl;

    //
    // Special debug control registers.
    //

    DWORD64 DebugControl;
    DWORD64 LastBranchToRip;
    DWORD64 LastBranchFromRip;
    DWORD64 LastExceptionToRip;
    DWORD64 LastExceptionFromRip;
} CONTEXT, *PCONTEXT, *LPCONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {
    union {
        PM128A FloatingContext[16];
        struct {
            PM128A Xmm0;
            PM128A Xmm1;
            PM128A Xmm2;
            PM128A Xmm3;
            PM128A Xmm4;
            PM128A Xmm5;
            PM128A Xmm6;
            PM128A Xmm7;
            PM128A Xmm8;
            PM128A Xmm9;
            PM128A Xmm10;
            PM128A Xmm11;
            PM128A Xmm12;
            PM128A Xmm13;
            PM128A Xmm14;
            PM128A Xmm15;
        } ;
    } ;

    union {
        PDWORD64 IntegerContext[16];
        struct {
            PDWORD64 Rax;
            PDWORD64 Rcx;
            PDWORD64 Rdx;
            PDWORD64 Rbx;
            PDWORD64 Rsp;
            PDWORD64 Rbp;
            PDWORD64 Rsi;
            PDWORD64 Rdi;
            PDWORD64 R8;
            PDWORD64 R9;
            PDWORD64 R10;
            PDWORD64 R11;
            PDWORD64 R12;
            PDWORD64 R13;
            PDWORD64 R14;
            PDWORD64 R15;
        } ;
    } ;

} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

#elif defined(_ARM_)

#define CONTEXT_ARM   0x00200000L

// end_wx86

#define CONTEXT_CONTROL (CONTEXT_ARM | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_ARM | 0x2L)
#define CONTEXT_FLOATING_POINT  (CONTEXT_ARM | 0x4L)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_ARM | 0x8L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS)

#define CONTEXT_EXCEPTION_ACTIVE 0x8000000L
#define CONTEXT_SERVICE_ACTIVE 0x10000000L
#define CONTEXT_EXCEPTION_REQUEST 0x40000000L
#define CONTEXT_EXCEPTION_REPORTING 0x80000000L

//
// This flag is set by the unwinder if it has unwound to a call
// site, and cleared whenever it unwinds through a trap frame.
// It is used by language-specific exception handlers to help
// differentiate exception scopes during dispatching.
//

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, ARM supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

#define ARM_MAX_BREAKPOINTS     8
#define ARM_MAX_WATCHPOINTS     1

typedef struct _NEON128 {
    ULONGLONG Low;
    LONGLONG High;
} NEON128, *PNEON128;

//
// Context Frame
//
//  This frame has a several purposes: 1) it is used as an argument to
//  NtContinue, 2) it is used to constuct a call frame for APC delivery,
//  and 3) it is used in the user level thread creation routines.
//
//
// The flags field within this record controls the contents of a CONTEXT
// record.
//
// If the context record is used as an input parameter, then for each
// portion of the context record controlled by a flag whose value is
// set, it is assumed that that portion of the context record contains
// valid context. If the context record is being used to modify a threads
// context, then only that portion of the threads context is modified.
//
// If the context record is used as an output parameter to capture the
// context of a thread, then only those portions of the thread's context
// corresponding to set flags will be returned.
//
// CONTEXT_CONTROL specifies Sp, Lr, Pc, and Cpsr
//
// CONTEXT_INTEGER specifies R0-R12
//
// CONTEXT_FLOATING_POINT specifies Q0-Q15 / D0-D31 / S0-S31
//
// CONTEXT_DEBUG_REGISTERS specifies up to 16 of DBGBVR, DBGBCR, DBGWVR,
//      DBGWCR.
//

typedef struct DECLSPEC_ALIGN(8) _CONTEXT {

    //
    // Control flags.
    //

    DWORD ContextFlags;

    //
    // Integer registers
    //

    DWORD R0;
    DWORD R1;
    DWORD R2;
    DWORD R3;
    DWORD R4;
    DWORD R5;
    DWORD R6;
    DWORD R7;
    DWORD R8;
    DWORD R9;
    DWORD R10;
    DWORD R11;
    DWORD R12;

    //
    // Control Registers
    //

    DWORD Sp;
    DWORD Lr;
    DWORD Pc;
    DWORD Cpsr;

    //
    // Floating Point/NEON Registers
    //

    DWORD Fpscr;
    DWORD Padding;
    union {
        NEON128 Q[16];
        ULONGLONG D[32];
        DWORD S[32];
    };

    //
    // Debug registers
    //

    DWORD Bvr[ARM_MAX_BREAKPOINTS];
    DWORD Bcr[ARM_MAX_BREAKPOINTS];
    DWORD Wvr[ARM_MAX_WATCHPOINTS];
    DWORD Wcr[ARM_MAX_WATCHPOINTS];

    DWORD Padding2[2];

} CONTEXT, *PCONTEXT, *LPCONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD R4;
    PDWORD R5;
    PDWORD R6;
    PDWORD R7;
    PDWORD R8;
    PDWORD R9;
    PDWORD R10;
    PDWORD R11;
    PDWORD Lr;

    PULONGLONG D8;
    PULONGLONG D9;
    PULONGLONG D10;
    PULONGLONG D11;
    PULONGLONG D12;
    PULONGLONG D13;
    PULONGLONG D14;
    PULONGLONG D15;

} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

typedef struct _IMAGE_ARM_RUNTIME_FUNCTION_ENTRY {
    DWORD BeginAddress;
    DWORD EndAddress;
    union {
        DWORD UnwindData;
        struct {
            DWORD Flag : 2;
            DWORD FunctionLength : 11;
            DWORD Ret : 2;
            DWORD H : 1;
            DWORD Reg : 3;
            DWORD R : 1;
            DWORD L : 1;
            DWORD C : 1;
            DWORD StackAdjust : 10;
        };
    };
} IMAGE_ARM_RUNTIME_FUNCTION_ENTRY, * PIMAGE_ARM_RUNTIME_FUNCTION_ENTRY;

#elif defined(_ARM64_)

#define CONTEXT_ARM64   0x00400000L

#define CONTEXT_CONTROL (CONTEXT_ARM64 | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_ARM64 | 0x2L)
#define CONTEXT_FLOATING_POINT  (CONTEXT_ARM64 | 0x4L)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_ARM64 | 0x8L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ALL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS)

#define CONTEXT_EXCEPTION_ACTIVE 0x8000000L
#define CONTEXT_SERVICE_ACTIVE 0x10000000L
#define CONTEXT_EXCEPTION_REQUEST 0x40000000L
#define CONTEXT_EXCEPTION_REPORTING 0x80000000L

//
// This flag is set by the unwinder if it has unwound to a call
// site, and cleared whenever it unwinds through a trap frame.
// It is used by language-specific exception handlers to help
// differentiate exception scopes during dispatching.
//

#define CONTEXT_UNWOUND_TO_CALL 0x20000000

//
// Define initial Cpsr/Fpscr value
//

#define INITIAL_CPSR 0x10
#define INITIAL_FPSCR 0

// begin_ntoshvp

//
// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, ARM64 supports up to 16. In practice,
// however, almost no one implements more than 4 of each.
//

#define ARM64_MAX_BREAKPOINTS     8
#define ARM64_MAX_WATCHPOINTS     2

//
// Context Frame
//
//  This frame has a several purposes: 1) it is used as an argument to
//  NtContinue, 2) it is used to constuct a call frame for APC delivery,
//  and 3) it is used in the user level thread creation routines.
//
//
// The flags field within this record controls the contents of a CONTEXT
// record.
//
// If the context record is used as an input parameter, then for each
// portion of the context record controlled by a flag whose value is
// set, it is assumed that that portion of the context record contains
// valid context. If the context record is being used to modify a threads
// context, then only that portion of the threads context is modified.
//
// If the context record is used as an output parameter to capture the
// context of a thread, then only those portions of the thread's context
// corresponding to set flags will be returned.
//
// CONTEXT_CONTROL specifies Sp, Lr, Pc, and Cpsr
//
// CONTEXT_INTEGER specifies R0-R12
//
// CONTEXT_FLOATING_POINT specifies Q0-Q15 / D0-D31 / S0-S31
//
// CONTEXT_DEBUG_REGISTERS specifies up to 16 of DBGBVR, DBGBCR, DBGWVR,
//      DBGWCR.
//

typedef struct _NEON128 {
    ULONGLONG Low;
    LONGLONG High;
} NEON128, *PNEON128;

typedef struct DECLSPEC_ALIGN(16) _CONTEXT {

    //
    // Control flags.
    //

    /* +0x000 */ DWORD ContextFlags;

    //
    // Integer registers
    //

    /* +0x004 */ DWORD Cpsr;       // NZVF + DAIF + CurrentEL + SPSel
    /* +0x008 */ union {
                    struct {
                        DWORD64 X0;
                        DWORD64 X1;
                        DWORD64 X2;
                        DWORD64 X3;
                        DWORD64 X4;
                        DWORD64 X5;
                        DWORD64 X6;
                        DWORD64 X7;
                        DWORD64 X8;
                        DWORD64 X9;
                        DWORD64 X10;
                        DWORD64 X11;
                        DWORD64 X12;
                        DWORD64 X13;
                        DWORD64 X14;
                        DWORD64 X15;
                        DWORD64 X16;
                        DWORD64 X17;
                        DWORD64 X18;
                        DWORD64 X19;
                        DWORD64 X20;
                        DWORD64 X21;
                        DWORD64 X22;
                        DWORD64 X23;
                        DWORD64 X24;
                        DWORD64 X25;
                        DWORD64 X26;
                        DWORD64 X27;
                        DWORD64 X28;
                    };
                    DWORD64 X[29];
                };
    /* +0x0f0 */ DWORD64 Fp;
    /* +0x0f8 */ DWORD64 Lr;
    /* +0x100 */ DWORD64 Sp;
    /* +0x108 */ DWORD64 Pc;

    //
    // Floating Point/NEON Registers
    //

    /* +0x110 */ NEON128 V[32];
    /* +0x310 */ DWORD Fpcr;
    /* +0x314 */ DWORD Fpsr;

    //
    // Debug registers
    //

    /* +0x318 */ DWORD Bcr[ARM64_MAX_BREAKPOINTS];
    /* +0x338 */ DWORD64 Bvr[ARM64_MAX_BREAKPOINTS];
    /* +0x378 */ DWORD Wcr[ARM64_MAX_WATCHPOINTS];
    /* +0x380 */ DWORD64 Wvr[ARM64_MAX_WATCHPOINTS];
    /* +0x390 */

} CONTEXT, *PCONTEXT, *LPCONTEXT;

//
// Nonvolatile context pointer record.
//

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {

    PDWORD64 X19;
    PDWORD64 X20;
    PDWORD64 X21;
    PDWORD64 X22;
    PDWORD64 X23;
    PDWORD64 X24;
    PDWORD64 X25;
    PDWORD64 X26;
    PDWORD64 X27;
    PDWORD64 X28;
    PDWORD64 Fp;
    PDWORD64 Lr;

    PDWORD64 D8;
    PDWORD64 D9;
    PDWORD64 D10;
    PDWORD64 D11;
    PDWORD64 D12;
    PDWORD64 D13;
    PDWORD64 D14;
    PDWORD64 D15;

} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

#else
#error Unknown architecture for defining CONTEXT.
#endif


PALIMPORT
BOOL
PALAPI
GetThreadContext(
         IN HANDLE hThread,
         IN OUT LPCONTEXT lpContext);

PALIMPORT
BOOL
PALAPI
SetThreadContext(
         IN HANDLE hThread,
         IN CONST CONTEXT *lpContext);

#define THREAD_BASE_PRIORITY_LOWRT    15
#define THREAD_BASE_PRIORITY_MAX      2
#define THREAD_BASE_PRIORITY_MIN      (-2)
#define THREAD_BASE_PRIORITY_IDLE     (-15)

#define THREAD_PRIORITY_LOWEST        THREAD_BASE_PRIORITY_MIN
#define THREAD_PRIORITY_BELOW_NORMAL  (THREAD_PRIORITY_LOWEST+1)
#define THREAD_PRIORITY_NORMAL        0
#define THREAD_PRIORITY_HIGHEST       THREAD_BASE_PRIORITY_MAX
#define THREAD_PRIORITY_ABOVE_NORMAL  (THREAD_PRIORITY_HIGHEST-1)
#define THREAD_PRIORITY_ERROR_RETURN  (MAXLONG)

#define THREAD_PRIORITY_TIME_CRITICAL THREAD_BASE_PRIORITY_LOWRT
#define THREAD_PRIORITY_IDLE          THREAD_BASE_PRIORITY_IDLE

PALIMPORT
int
PALAPI
GetThreadPriority(
          IN HANDLE hThread);

PALIMPORT
BOOL
PALAPI
SetThreadPriority(
          IN HANDLE hThread,
          IN int nPriority);

PALIMPORT
BOOL
PALAPI
GetThreadTimes(
        IN HANDLE hThread,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpExitTime,
        OUT LPFILETIME lpKernelTime,
        OUT LPFILETIME lpUserTime);
    
#define TLS_OUT_OF_INDEXES ((DWORD)0xFFFFFFFF)

PALIMPORT
DWORD
PALAPI
TlsAlloc(
     VOID);

PALIMPORT
LPVOID
PALAPI
TlsGetValue(
        IN DWORD dwTlsIndex);

PALIMPORT
BOOL
PALAPI
TlsSetValue(
        IN DWORD dwTlsIndex,
        IN LPVOID lpTlsValue);

PALIMPORT
BOOL
PALAPI
TlsFree(
    IN DWORD dwTlsIndex);

PALIMPORT
void *
PALAPI
PAL_GetStackBase();

PALIMPORT
void *
PALAPI
PAL_GetStackLimit();

PALIMPORT
DWORD
PALAPI
PAL_GetLogicalCpuCountFromOS();

PALIMPORT
size_t
PALAPI
PAL_GetLogicalProcessorCacheSizeFromOS();

typedef BOOL (*ReadMemoryWordCallback)(SIZE_T address, SIZE_T *value);

PALIMPORT BOOL PALAPI PAL_VirtualUnwind(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers);

PALIMPORT BOOL PALAPI PAL_VirtualUnwindOutOfProc(CONTEXT *context, 
                                                 KNONVOLATILE_CONTEXT_POINTERS *contextPointers, 
                                                 DWORD pid, 
                                                 ReadMemoryWordCallback readMemCallback);

#define GetLogicalProcessorCacheSizeFromOS PAL_GetLogicalProcessorCacheSizeFromOS

#ifdef PLATFORM_UNIX

/* PAL_CS_NATIVE_DATA_SIZE is defined as sizeof(PAL_CRITICAL_SECTION_NATIVE_DATA) */

#if defined(_AIX)
#define PAL_CS_NATIVE_DATA_SIZE 100
#elif defined(__APPLE__) && defined(__i386__)
#define PAL_CS_NATIVE_DATA_SIZE 76
#elif defined(__APPLE__) && defined(__x86_64__)
#define PAL_CS_NATIVE_DATA_SIZE 120
#elif defined(__FreeBSD__) && defined(_X86_)
#define PAL_CS_NATIVE_DATA_SIZE 12
#elif defined(__FreeBSD__) && defined(__x86_64__)
#define PAL_CS_NATIVE_DATA_SIZE 24
#elif defined(__hpux__) && (defined(__hppa__) || defined (__ia64__))
#define PAL_CS_NATIVE_DATA_SIZE 148
#elif defined(__linux__) && defined(_ARM_)
#define PAL_CS_NATIVE_DATA_SIZE 80
#elif defined(__linux__) && defined(_ARM64_)
#define PAL_CS_NATIVE_DATA_SIZE 116
#elif defined(__linux__) && defined(__x86_64__)
#define PAL_CS_NATIVE_DATA_SIZE 96
#elif defined(__NetBSD__) && defined(__amd64__)
#define PAL_CS_NATIVE_DATA_SIZE 96
#elif defined(__NetBSD__) && defined(__earm__)
#define PAL_CS_NATIVE_DATA_SIZE 56
#elif defined(__NetBSD__) && defined(__hppa__)
#define PAL_CS_NATIVE_DATA_SIZE 92
#elif defined(__NetBSD__) && defined(__i386__)
#define PAL_CS_NATIVE_DATA_SIZE 56
#elif defined(__NetBSD__) && defined(__mips__)
#define PAL_CS_NATIVE_DATA_SIZE 56
#elif defined(__NetBSD__) && (defined(__sparc__) && !defined(__sparc64__))
#define PAL_CS_NATIVE_DATA_SIZE 56
#elif defined(__NetBSD__) && defined(__sparc64__)
#define PAL_CS_NATIVE_DATA_SIZE 92
#elif defined(__sun__)
#define PAL_CS_NATIVE_DATA_SIZE 48
#else 
#warning 
#error  PAL_CS_NATIVE_DATA_SIZE is not defined for this architecture
#endif
    
#endif // PLATFORM_UNIX

// 
typedef struct _CRITICAL_SECTION {
    PVOID DebugInfo;
    LONG LockCount;
    LONG RecursionCount;
    HANDLE OwningThread;
    HANDLE LockSemaphore;
    ULONG_PTR SpinCount;

#ifdef PLATFORM_UNIX
    BOOL bInternal;
    volatile DWORD dwInitState;
    union CSNativeDataStorage
    {
        BYTE rgNativeDataStorage[PAL_CS_NATIVE_DATA_SIZE]; 
        VOID * pvAlign; // make sure the storage is machine-pointer-size aligned
    } csnds;    
#endif // PLATFORM_UNIX    
} CRITICAL_SECTION, *PCRITICAL_SECTION, *LPCRITICAL_SECTION;

PALIMPORT VOID PALAPI EnterCriticalSection(IN OUT LPCRITICAL_SECTION lpCriticalSection);
PALIMPORT VOID PALAPI LeaveCriticalSection(IN OUT LPCRITICAL_SECTION lpCriticalSection);
PALIMPORT VOID PALAPI InitializeCriticalSection(OUT LPCRITICAL_SECTION lpCriticalSection);
PALIMPORT BOOL PALAPI InitializeCriticalSectionEx(LPCRITICAL_SECTION lpCriticalSection, DWORD dwSpinCount, DWORD Flags);
PALIMPORT VOID PALAPI DeleteCriticalSection(IN OUT LPCRITICAL_SECTION lpCriticalSection);
PALIMPORT BOOL PALAPI TryEnterCriticalSection(IN OUT LPCRITICAL_SECTION lpCriticalSection);

#define SEM_FAILCRITICALERRORS          0x0001
#define SEM_NOOPENFILEERRORBOX          0x8000

PALIMPORT
UINT
PALAPI
SetErrorMode(
         IN UINT uMode);

#define PAGE_NOACCESS                   0x01
#define PAGE_READONLY                   0x02
#define PAGE_READWRITE                  0x04
#define PAGE_WRITECOPY                  0x08
#define PAGE_EXECUTE                    0x10
#define PAGE_EXECUTE_READ               0x20
#define PAGE_EXECUTE_READWRITE          0x40
#define PAGE_EXECUTE_WRITECOPY          0x80
#define MEM_COMMIT                      0x1000
#define MEM_RESERVE                     0x2000
#define MEM_DECOMMIT                    0x4000
#define MEM_RELEASE                     0x8000
#define MEM_RESET                       0x80000
#define MEM_FREE                        0x10000
#define MEM_PRIVATE                     0x20000
#define MEM_MAPPED                      0x40000
#define MEM_TOP_DOWN                    0x100000
#define MEM_WRITE_WATCH                 0x200000
#define MEM_RESERVE_EXECUTABLE          0x40000000 // reserve memory using executable memory allocator

PALIMPORT
HANDLE
PALAPI
CreateFileMappingW(
           IN HANDLE hFile,
           IN LPSECURITY_ATTRIBUTES lpFileMappingAttributes,
           IN DWORD flProtect,
           IN DWORD dwMaxmimumSizeHigh,
           IN DWORD dwMaximumSizeLow,
           IN LPCWSTR lpName);

#ifdef UNICODE
#define CreateFileMapping CreateFileMappingW
#else
#define CreateFileMapping CreateFileMappingA
#endif

#define SECTION_QUERY       0x0001
#define SECTION_MAP_WRITE   0x0002
#define SECTION_MAP_READ    0x0004
#define SECTION_ALL_ACCESS  (SECTION_MAP_READ | SECTION_MAP_WRITE) // diff from winnt.h

#define FILE_MAP_WRITE      SECTION_MAP_WRITE
#define FILE_MAP_READ       SECTION_MAP_READ
#define FILE_MAP_ALL_ACCESS SECTION_ALL_ACCESS
#define FILE_MAP_COPY       SECTION_QUERY

PALIMPORT
HANDLE
PALAPI
OpenFileMappingW(
         IN DWORD dwDesiredAccess,
         IN BOOL bInheritHandle,
         IN LPCWSTR lpName);

#ifdef UNICODE
#define OpenFileMapping OpenFileMappingW
#else
#define OpenFileMapping OpenFileMappingA
#endif

PALIMPORT
LPVOID
PALAPI
MapViewOfFile(
          IN HANDLE hFileMappingObject,
          IN DWORD dwDesiredAccess,
          IN DWORD dwFileOffsetHigh,
          IN DWORD dwFileOffsetLow,
          IN SIZE_T dwNumberOfBytesToMap);

PALIMPORT
LPVOID
PALAPI
MapViewOfFileEx(
          IN HANDLE hFileMappingObject,
          IN DWORD dwDesiredAccess,
          IN DWORD dwFileOffsetHigh,
          IN DWORD dwFileOffsetLow,
          IN SIZE_T dwNumberOfBytesToMap,
          IN LPVOID lpBaseAddress);
          
PALIMPORT
BOOL
PALAPI
FlushViewOfFile(
        IN LPVOID lpBaseAddress,
        IN SIZE_T dwNumberOfBytesToFlush);

PALIMPORT
BOOL
PALAPI
UnmapViewOfFile(
        IN LPCVOID lpBaseAddress);


PALIMPORT
HMODULE
PALAPI
LoadLibraryW(
        IN LPCWSTR lpLibFileName);

PALIMPORT
HMODULE
PALAPI
LoadLibraryExW(
        IN LPCWSTR lpLibFileName,
        IN /*Reserved*/ HANDLE hFile,
        IN DWORD dwFlags);

PALIMPORT
void *
PALAPI
PAL_LoadLibraryDirect(
        IN LPCWSTR lpLibFileName);

PALIMPORT
HMODULE
PALAPI
PAL_RegisterLibraryDirect(
        IN void *dl_handle,
        IN LPCWSTR lpLibFileName);

/*++
Function:
  PAL_LOADLoadPEFile

Abstract
  Loads a PE file into memory.  Properly maps all of the sections in the PE file.  Returns a pointer to the
  loaded base.

Parameters:
    IN hFile    - The file to load

Return value:
    A valid base address if successful.
    0 if failure
--*/
void *
PALAPI
PAL_LOADLoadPEFile(HANDLE hFile);

/*++
    PAL_LOADUnloadPEFile

    Unload a PE file that was loaded by PAL_LOADLoadPEFile().

Parameters:
    IN ptr - the file pointer returned by PAL_LOADLoadPEFile()

Return value:
    TRUE - success
    FALSE - failure (incorrect ptr, etc.)
--*/
BOOL 
PALAPI
PAL_LOADUnloadPEFile(void * ptr);

#ifdef UNICODE
#define LoadLibrary LoadLibraryW
#define LoadLibraryEx LoadLibraryExW
#else
#define LoadLibrary LoadLibraryA
#define LoadLibraryEx LoadLibraryExA
#endif

typedef INT_PTR (PALAPI *FARPROC)();

PALIMPORT
FARPROC
PALAPI
GetProcAddress(
    IN HMODULE hModule,
    IN LPCSTR lpProcName);

PALIMPORT
BOOL
PALAPI
FreeLibrary(
    IN OUT HMODULE hLibModule);

PALIMPORT
PAL_NORETURN
VOID
PALAPI
FreeLibraryAndExitThread(
    IN HMODULE hLibModule,
    IN DWORD dwExitCode);

PALIMPORT
BOOL
PALAPI
DisableThreadLibraryCalls(
    IN HMODULE hLibModule);

PALIMPORT
DWORD
PALAPI
GetModuleFileNameW(
    IN HMODULE hModule,
    OUT LPWSTR lpFileName,
    IN DWORD nSize);

#ifdef UNICODE
#define GetModuleFileName GetModuleFileNameW
#else
#define GetModuleFileName GetModuleFileNameA
#endif

PALIMPORT
DWORD
PALAPI
GetModuleFileNameExW(
    IN HANDLE hProcess,
    IN HMODULE hModule,
    OUT LPWSTR lpFilename,
    IN DWORD nSize
    );

#ifdef UNICODE
#define GetModuleFileNameEx GetModuleFileNameExW
#endif

// Get base address of the module containing a given symbol 
PALAPI
LPCVOID
PAL_GetSymbolModuleBase(void *symbol);

PALIMPORT
LPVOID
PALAPI
VirtualAlloc(
         IN LPVOID lpAddress,
         IN SIZE_T dwSize,
         IN DWORD flAllocationType,
         IN DWORD flProtect);

PALIMPORT
BOOL
PALAPI
VirtualFree(
        IN LPVOID lpAddress,
        IN SIZE_T dwSize,
        IN DWORD dwFreeType);

PALIMPORT
BOOL
PALAPI
VirtualProtect(
           IN LPVOID lpAddress,
           IN SIZE_T dwSize,
           IN DWORD flNewProtect,
           OUT PDWORD lpflOldProtect);

typedef struct _MEMORYSTATUSEX {
  DWORD     dwLength;
  DWORD     dwMemoryLoad;
  DWORDLONG ullTotalPhys;
  DWORDLONG ullAvailPhys;
  DWORDLONG ullTotalPageFile;
  DWORDLONG ullAvailPageFile;
  DWORDLONG ullTotalVirtual;
  DWORDLONG ullAvailVirtual;
  DWORDLONG ullAvailExtendedVirtual;
} MEMORYSTATUSEX, *LPMEMORYSTATUSEX;

PALIMPORT 
BOOL
PALAPI
GlobalMemoryStatusEx(
            IN OUT LPMEMORYSTATUSEX lpBuffer);

typedef struct _MEMORY_BASIC_INFORMATION {
    PVOID BaseAddress;
    PVOID AllocationBase_PAL_Undefined;
    DWORD AllocationProtect;
    SIZE_T RegionSize;
    DWORD State;
    DWORD Protect;
    DWORD Type;
} MEMORY_BASIC_INFORMATION, *PMEMORY_BASIC_INFORMATION;

PALIMPORT
SIZE_T
PALAPI
VirtualQuery(
         IN LPCVOID lpAddress,
         OUT PMEMORY_BASIC_INFORMATION lpBuffer,
         IN SIZE_T dwLength);

PALIMPORT
BOOL
PALAPI
ReadProcessMemory(
          IN HANDLE hProcess,
          IN LPCVOID lpBaseAddress,
          OUT LPVOID lpBuffer,
          IN SIZE_T nSize,
          OUT SIZE_T * lpNumberOfBytesRead);

PALIMPORT
VOID
PALAPI
RtlMoveMemory(
          IN PVOID Destination,
          IN CONST VOID *Source,
          IN SIZE_T Length);

PALIMPORT
VOID
PALAPI
RtlZeroMemory(
    IN PVOID Destination,
    IN SIZE_T Length);

#define MoveMemory memmove
#define CopyMemory memcpy
#define FillMemory(Destination,Length,Fill) memset((Destination),(Fill),(Length))
#define ZeroMemory(Destination,Length) memset((Destination),0,(Length))


PALIMPORT
HANDLE
PALAPI
GetProcessHeap(
           VOID);

#define HEAP_ZERO_MEMORY 0x00000008

PALIMPORT
HANDLE
PALAPI
HeapCreate(
         IN DWORD flOptions,
         IN SIZE_T dwInitialSize,
         IN SIZE_T dwMaximumSize);

PALIMPORT
LPVOID
PALAPI
HeapAlloc(
      IN HANDLE hHeap,
      IN DWORD dwFlags,
      IN SIZE_T dwBytes);

PALIMPORT
LPVOID
PALAPI
HeapReAlloc(
    IN HANDLE hHeap,
    IN DWORD dwFlags,
    IN LPVOID lpMem,
    IN SIZE_T dwBytes
    );

PALIMPORT
BOOL
PALAPI
HeapFree(
     IN HANDLE hHeap,
     IN DWORD dwFlags,
     IN LPVOID lpMem);

typedef enum _HEAP_INFORMATION_CLASS {
    HeapCompatibilityInformation,
    HeapEnableTerminationOnCorruption
} HEAP_INFORMATION_CLASS;

PALIMPORT
BOOL
PALAPI
HeapSetInformation(
        IN OPTIONAL HANDLE HeapHandle,
        IN HEAP_INFORMATION_CLASS HeapInformationClass,
        IN PVOID HeapInformation,
        IN SIZE_T HeapInformationLength);

#define LMEM_FIXED          0x0000
#define LMEM_MOVEABLE       0x0002
#define LMEM_ZEROINIT       0x0040
#define LPTR                (LMEM_FIXED | LMEM_ZEROINIT)

PALIMPORT
HLOCAL
PALAPI
LocalAlloc(
       IN UINT uFlags,
       IN SIZE_T uBytes);

PALIMPORT
HLOCAL
PALAPI
LocalReAlloc(
       IN HLOCAL hMem,
       IN SIZE_T uBytes,
       IN UINT   uFlags);

PALIMPORT
HLOCAL
PALAPI
LocalFree(
      IN HLOCAL hMem);

PALIMPORT
BOOL
PALAPI
FlushInstructionCache(
              IN HANDLE hProcess,
              IN LPCVOID lpBaseAddress,
              IN SIZE_T dwSize);

#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
BOOL
PALAPI
GetStringTypeExW(
         IN LCID Locale,
         IN DWORD dwInfoType,
         IN LPCWSTR lpSrcStr,
         IN int cchSrc,
         OUT LPWORD lpCharType);

#ifdef UNICODE
#define GetStringTypeEx GetStringTypeExW
#endif

#endif // ENABLE_DOWNLEVEL_FOR_NLS


#define NORM_IGNORECASE           0x00000001  // ignore case
#define NORM_IGNOREWIDTH          0x00020000  // ignore width

#define NORM_LINGUISTIC_CASING    0x08000000  // use linguistic rules for casing

#ifdef __APPLE__
#define NORM_IGNORENONSPACE       0x00000002  // ignore nonspacing chars
#define NORM_IGNORESYMBOLS        0x00000004  // ignore symbols
#define NORM_IGNOREKANATYPE       0x00010000  // ignore kanatype
#define SORT_STRINGSORT           0x00001000  // use string sort method
#endif // __APPLE__


typedef struct nlsversioninfo { 
  DWORD     dwNLSVersionInfoSize; 
  DWORD     dwNLSVersion; 
  DWORD     dwDefinedVersion; 
} NLSVERSIONINFO, *LPNLSVERSIONINFO; 

#define CSTR_LESS_THAN     1
#define CSTR_EQUAL         2
#define CSTR_GREATER_THAN  3

#if ENABLE_DOWNLEVEL_FOR_NLS


PALIMPORT
int
PALAPI
CompareStringW(
    IN LCID     Locale,
    IN DWORD    dwCmpFlags,
    IN LPCWSTR  lpString1,
    IN int      cchCount1,
    IN LPCWSTR  lpString2,
    IN int      cchCount2);

#endif // ENABLE_DOWNLEVEL_FOR_NLS


PALIMPORT
int
PALAPI
CompareStringEx(
    IN LPCWSTR lpLocaleName,
    IN DWORD    dwCmpFlags,
    IN LPCWSTR  lpString1,
    IN int      cchCount1,
    IN LPCWSTR  lpString2,
    IN int      cchCount2,
    IN LPNLSVERSIONINFO lpVersionInformation,
    IN LPVOID lpReserved,
    IN LPARAM lParam);


#ifdef UNICODE
#define CompareString  CompareStringW
#endif

#define MAX_LEADBYTES         12
#define MAX_DEFAULTCHAR       2

PALIMPORT
UINT
PALAPI
GetACP(void);

typedef struct _cpinfo {
    UINT MaxCharSize;
    BYTE DefaultChar[MAX_DEFAULTCHAR];
    BYTE LeadByte[MAX_LEADBYTES];
} CPINFO, *LPCPINFO;

PALIMPORT
BOOL
PALAPI
GetCPInfo(
      IN UINT CodePage,
      OUT LPCPINFO lpCPInfo);

PALIMPORT
BOOL
PALAPI
IsDBCSLeadByteEx(
         IN UINT CodePage,
         IN BYTE TestChar);

PALIMPORT
BOOL
PALAPI
IsDBCSLeadByte(
        IN BYTE TestChar);

PALIMPORT
BOOL
PALAPI
IsValidCodePage(
        IN UINT CodePage);
        

#define MB_PRECOMPOSED            0x00000001
#define MB_ERR_INVALID_CHARS      0x00000008

PALIMPORT
int
PALAPI
MultiByteToWideChar(
            IN UINT CodePage,
            IN DWORD dwFlags,
            IN LPCSTR lpMultiByteStr,
            IN int cbMultiByte,
            OUT LPWSTR lpWideCharStr,
            IN int cchWideChar);

#define WC_NO_BEST_FIT_CHARS      0x00000400

PALIMPORT
int
PALAPI
WideCharToMultiByte(
            IN UINT CodePage,
            IN DWORD dwFlags,
            IN LPCWSTR lpWideCharStr,
            IN int cchWideChar,
            OUT LPSTR lpMultiByteStr,
            IN int cbMultyByte,
            IN LPCSTR lpDefaultChar,
            OUT LPBOOL lpUsedDefaultChar);

#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
LANGID
PALAPI
GetSystemDefaultLangID(
               void);

PALIMPORT
LANGID
PALAPI
GetUserDefaultLangID(
             void);

PALIMPORT
BOOL
PALAPI
SetThreadLocale(
        IN LCID Locale);

PALIMPORT
LCID
PALAPI
GetThreadLocale(
        void);

#endif //ENABLE_DOWNLEVEL_FOR_NLS

//
//  Locale Types.
//
//  These types are used for the GetLocaleInfo NLS API routine.
//

#ifdef __APPLE__

//
//  The following LCTypes may be used in combination with any other LCTypes.
//
//    LOCALE_NOUSEROVERRIDE is also used in GetTimeFormat and
//    GetDateFormat.
//
//    LOCALE_RETURN_NUMBER will return the result from GetLocaleInfo as a
//    number instead of a string.  This flag is only valid for the LCTypes
//    beginning with LOCALE_I.
//
#define LOCALE_NOUSEROVERRIDE         0x80000000    /* do not use user overrides */
#define LOCALE_RETURN_NUMBER          0x20000000    /* return number instead of string */
#define LOCALE_RETURN_GENITIVE_NAMES  0x10000000   //Flag to return the Genitive forms of month names

#define LOCALE_SLOCALIZEDDISPLAYNAME  0x00000002   // localized name of locale, eg "German (Germany)" in UI language
#define LOCALE_SENGLISHDISPLAYNAME    0x00000072   // Display name (language + country usually) in English, eg "German (Germany)"
#define LOCALE_SNATIVEDISPLAYNAME     0x00000073   // Display name in native locale language, eg "Deutsch (Deutschland)

#define LOCALE_SLOCALIZEDLANGUAGENAME 0x0000006f   // Language Display Name for a language, eg "German" in UI language
#define LOCALE_SENGLISHLANGUAGENAME   0x00001001   // English name of language, eg "German"
#define LOCALE_SNATIVELANGUAGENAME    0x00000004   // native name of language, eg "Deutsch"

#define LOCALE_SLOCALIZEDCOUNTRYNAME  0x00000006   // localized name of country, eg "Germany" in UI language
#define LOCALE_SENGLISHCOUNTRYNAME    0x00001002   // English name of country, eg "Germany"
#define LOCALE_SNATIVECOUNTRYNAME     0x00000008   // native name of country, eg "Deutschland"

//
//  The following LCTypes are mutually exclusive in that they may NOT
//  be used in combination with each other.
//
#define LOCALE_ILANGUAGE              0x00000001    /* language id */
#define LOCALE_SLANGUAGE              0x00000002    /* localized name of language */
#define LOCALE_SENGLANGUAGE           0x00001001    /* English name of language */
#define LOCALE_SABBREVLANGNAME        0x00000003    /* abbreviated language name */
#define LOCALE_SNATIVELANGNAME        0x00000004    /* native name of language */
#define LOCALE_ICOUNTRY               0x00000005    /* country code */
#define LOCALE_SCOUNTRY               0x00000006    /* localized name of country */

#define LOCALE_SENGCOUNTRY            0x00001002    /* English name of country */
#define LOCALE_SABBREVCTRYNAME        0x00000007    /* abbreviated country name */
#define LOCALE_SNATIVECTRYNAME        0x00000008    /* native name of country */

#define LOCALE_SLIST                  0x0000000C    /* list item separator */
#define LOCALE_IMEASURE               0x0000000D    /* 0 = metric, 1 = US */

#define LOCALE_SDECIMAL               0x0000000E    /* decimal separator */
#define LOCALE_STHOUSAND              0x0000000F    /* thousand separator */
#define LOCALE_SGROUPING              0x00000010    /* digit grouping */
#define LOCALE_IDIGITS                0x00000011    /* number of fractional digits */
#define LOCALE_ILZERO                 0x00000012    /* leading zeros for decimal */
#define LOCALE_INEGNUMBER             0x00001010    /* negative number mode */
#define LOCALE_SNATIVEDIGITS          0x00000013    /* native ascii 0-9 */

#define LOCALE_SCURRENCY              0x00000014    /* local monetary symbol */
#define LOCALE_SINTLSYMBOL            0x00000015    /* intl monetary symbol */
#define LOCALE_SMONDECIMALSEP         0x00000016    /* monetary decimal separator */
#define LOCALE_SMONTHOUSANDSEP        0x00000017    /* monetary thousand separator */
#define LOCALE_SMONGROUPING           0x00000018    /* monetary grouping */
#define LOCALE_ICURRDIGITS            0x00000019    /* # local monetary digits */
#define LOCALE_IINTLCURRDIGITS        0x0000001A    /* # intl monetary digits */
#define LOCALE_ICURRENCY              0x0000001B    /* positive currency mode */
#define LOCALE_INEGCURR               0x0000001C    /* negative currency mode */

#define LOCALE_SSHORTDATE             0x0000001F    /* short date format string */
#define LOCALE_SLONGDATE              0x00000020    /* long date format string */
#define LOCALE_STIMEFORMAT            0x00001003    /* time format string */
#define LOCALE_S1159                  0x00000028    /* AM designator */
#define LOCALE_S2359                  0x00000029    /* PM designator */

#define LOCALE_ICALENDARTYPE          0x00001009    /* type of calendar specifier */
#define LOCALE_IFIRSTDAYOFWEEK        0x0000100C    /* first day of week specifier */
#define LOCALE_IFIRSTWEEKOFYEAR       0x0000100D    /* first week of year specifier */

#define LOCALE_SDAYNAME1              0x0000002A    /* long name for Monday */
#define LOCALE_SDAYNAME2              0x0000002B    /* long name for Tuesday */
#define LOCALE_SDAYNAME3              0x0000002C    /* long name for Wednesday */
#define LOCALE_SDAYNAME4              0x0000002D    /* long name for Thursday */
#define LOCALE_SDAYNAME5              0x0000002E    /* long name for Friday */
#define LOCALE_SDAYNAME6              0x0000002F    /* long name for Saturday */
#define LOCALE_SDAYNAME7              0x00000030    /* long name for Sunday */
#define LOCALE_SABBREVDAYNAME1        0x00000031    /* abbreviated name for Monday */
#define LOCALE_SABBREVDAYNAME2        0x00000032    /* abbreviated name for Tuesday */
#define LOCALE_SABBREVDAYNAME3        0x00000033    /* abbreviated name for Wednesday */
#define LOCALE_SABBREVDAYNAME4        0x00000034    /* abbreviated name for Thursday */
#define LOCALE_SABBREVDAYNAME5        0x00000035    /* abbreviated name for Friday */
#define LOCALE_SABBREVDAYNAME6        0x00000036    /* abbreviated name for Saturday */
#define LOCALE_SABBREVDAYNAME7        0x00000037    /* abbreviated name for Sunday */
#define LOCALE_SMONTHNAME1            0x00000038    /* long name for January */
#define LOCALE_SMONTHNAME2            0x00000039    /* long name for February */
#define LOCALE_SMONTHNAME3            0x0000003A    /* long name for March */
#define LOCALE_SMONTHNAME4            0x0000003B    /* long name for April */
#define LOCALE_SMONTHNAME5            0x0000003C    /* long name for May */
#define LOCALE_SMONTHNAME6            0x0000003D    /* long name for June */
#define LOCALE_SMONTHNAME7            0x0000003E    /* long name for July */
#define LOCALE_SMONTHNAME8            0x0000003F    /* long name for August */
#define LOCALE_SMONTHNAME9            0x00000040    /* long name for September */
#define LOCALE_SMONTHNAME10           0x00000041    /* long name for October */
#define LOCALE_SMONTHNAME11           0x00000042    /* long name for November */
#define LOCALE_SMONTHNAME12           0x00000043    /* long name for December */
#define LOCALE_SMONTHNAME13           0x0000100E    /* long name for 13th month (if exists) */
#define LOCALE_SABBREVMONTHNAME1      0x00000044    /* abbreviated name for January */
#define LOCALE_SABBREVMONTHNAME2      0x00000045    /* abbreviated name for February */
#define LOCALE_SABBREVMONTHNAME3      0x00000046    /* abbreviated name for March */
#define LOCALE_SABBREVMONTHNAME4      0x00000047    /* abbreviated name for April */
#define LOCALE_SABBREVMONTHNAME5      0x00000048    /* abbreviated name for May */
#define LOCALE_SABBREVMONTHNAME6      0x00000049    /* abbreviated name for June */
#define LOCALE_SABBREVMONTHNAME7      0x0000004A    /* abbreviated name for July */
#define LOCALE_SABBREVMONTHNAME8      0x0000004B    /* abbreviated name for August */
#define LOCALE_SABBREVMONTHNAME9      0x0000004C    /* abbreviated name for September */
#define LOCALE_SABBREVMONTHNAME10     0x0000004D    /* abbreviated name for October */
#define LOCALE_SABBREVMONTHNAME11     0x0000004E    /* abbreviated name for November */
#define LOCALE_SABBREVMONTHNAME12     0x0000004F    /* abbreviated name for December */
#define LOCALE_SABBREVMONTHNAME13     0x0000100F    /* abbreviated name for 13th month (if exists) */

#define LOCALE_SPOSITIVESIGN          0x00000050    /* positive sign */
#define LOCALE_SNEGATIVESIGN          0x00000051    /* negative sign */

#define LOCALE_FONTSIGNATURE          0x00000058    /* font signature */
#define LOCALE_SISO639LANGNAME        0x00000059    /* ISO abbreviated language name */
#define LOCALE_SISO3166CTRYNAME       0x0000005A    /* ISO abbreviated country name */

#define LOCALE_SENGCURRNAME           0x00001007    /* english name of currency */
#define LOCALE_SNATIVECURRNAME        0x00001008    /* native name of currency */
#define LOCALE_SYEARMONTH             0x00001006    /* year month format string */
#define LOCALE_IDIGITSUBSTITUTION     0x00001014    /* 0 = context, 1 = none, 2 = national */

#define LOCALE_SNAME                  0x0000005C    /* locale name <language>[-<Script>][-<REGION>[_<sort order>]] */
#define LOCALE_SDURATION              0x0000005d    /* time duration format */
#define LOCALE_SKEYBOARDSTOINSTALL    0x0000005e    /* (windows only) keyboards to install */
#define LOCALE_SSHORTESTDAYNAME1      0x00000060    /* Shortest day name for Monday */
#define LOCALE_SSHORTESTDAYNAME2      0x00000061    /* Shortest day name for Tuesday */
#define LOCALE_SSHORTESTDAYNAME3      0x00000062    /* Shortest day name for Wednesday */
#define LOCALE_SSHORTESTDAYNAME4      0x00000063    /* Shortest day name for Thursday */
#define LOCALE_SSHORTESTDAYNAME5      0x00000064    /* Shortest day name for Friday */
#define LOCALE_SSHORTESTDAYNAME6      0x00000065    /* Shortest day name for Saturday */
#define LOCALE_SSHORTESTDAYNAME7      0x00000066    /* Shortest day name for Sunday */
#define LOCALE_SISO639LANGNAME2       0x00000067    /* 3 character ISO abbreviated language name */
#define LOCALE_SISO3166CTRYNAME2      0x00000068    /* 3 character ISO country name */
#define LOCALE_SNAN                   0x00000069    /* Not a Number */
#define LOCALE_SPOSINFINITY           0x0000006a    /* + Infinity */
#define LOCALE_SNEGINFINITY           0x0000006b    /* - Infinity */
#define LOCALE_SSCRIPTS               0x0000006c    /* Typical scripts in the locale */
#define LOCALE_SPARENT                0x0000006d    /* Fallback name for resources */
#define LOCALE_SCONSOLEFALLBACKNAME   0x0000006e    /* Fallback name for within the console */
#define LOCALE_SLANGDISPLAYNAME       0x0000006f    /* Language Display Name for a language */ 
#define LOCALE_IREADINGLAYOUT         0x00000070   // Returns one of the following 4 reading layout values:
                                                   // 0 - Left to right (eg en-US)
                                                   // 1 - Right to left (eg arabic locales)
                                                   // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
                                                   // 3 - Vertical top to bottom with columns proceeding to the right
#define LOCALE_INEUTRAL               0x00000071   // Returns 0 for specific cultures, 1 for neutral cultures.
#define LOCALE_INEGATIVEPERCENT       0x00000074   // Returns 0-11 for the negative percent format
#define LOCALE_IPOSITIVEPERCENT       0x00000075   // Returns 0-3 for the positive percent formatIPOSITIVEPERCENT
#define LOCALE_SPERCENT               0x00000076   // Returns the percent symbol
#define LOCALE_SPERMILLE              0x00000077   // Returns the permille (U+2030) symbol
#define LOCALE_SMONTHDAY              0x00000078   // Returns the preferred month/day format
#define LOCALE_SSHORTTIME             0x00000079   // Returns the preferred short time format (ie: no seconds, just h:mm)
#define LOCALE_SOPENTYPELANGUAGETAG   0x0000007a   // Open type language tag, eg: "latn" or "dflt"
#define LOCALE_SSORTLOCALE            0x0000007b   // Name of locale to use for sorting/collation/casing behavior.

#define LCMAP_LINGUISTIC_CASING       0x01000000    /* Use linguistic casing */

#define CAL_RETURN_GENITIVE_NAMES       LOCALE_RETURN_GENITIVE_NAMES  // return genitive forms of month names

#define CAL_SSHORTESTDAYNAME1         0x00000031
#define CAL_SSHORTESTDAYNAME2         0x00000032
#define CAL_SSHORTESTDAYNAME3         0x00000033
#define CAL_SSHORTESTDAYNAME4         0x00000034
#define CAL_SSHORTESTDAYNAME5         0x00000035
#define CAL_SSHORTESTDAYNAME6         0x00000036
#define CAL_SSHORTESTDAYNAME7         0x00000037

#define CAL_SMONTHDAY                   0x00000038  // Month/day pattern (reserve for potential inclusion in a future version)
#define CAL_SERASTRING                  0x00000004  // era name for IYearOffsetRanges, eg A.D.
#define CAL_SABBREVERASTRING            0x00000039  // Abbreviated era string (eg: AD)

#define CAL_SSHORTDATE            0x00000005  /* short date format string */
#define CAL_SLONGDATE             0x00000006  /* long date format string */
#define CAL_SDAYNAME1             0x00000007  /* native name for Monday */
#define CAL_SDAYNAME2             0x00000008  /* native name for Tuesday */
#define CAL_SDAYNAME3             0x00000009  /* native name for Wednesday */
#define CAL_SDAYNAME4             0x0000000a  /* native name for Thursday */
#define CAL_SDAYNAME5             0x0000000b  /* native name for Friday */
#define CAL_SDAYNAME6             0x0000000c  /* native name for Saturday */
#define CAL_SDAYNAME7             0x0000000d  /* native name for Sunday */
#define CAL_SABBREVDAYNAME1       0x0000000e  /* abbreviated name for Monday */
#define CAL_SABBREVDAYNAME2       0x0000000f  /* abbreviated name for Tuesday */
#define CAL_SABBREVDAYNAME3       0x00000010  /* abbreviated name for Wednesday */
#define CAL_SABBREVDAYNAME4       0x00000011  /* abbreviated name for Thursday */
#define CAL_SABBREVDAYNAME5       0x00000012  /* abbreviated name for Friday */
#define CAL_SABBREVDAYNAME6       0x00000013  /* abbreviated name for Saturday */
#define CAL_SABBREVDAYNAME7       0x00000014  /* abbreviated name for Sunday */
#define CAL_SMONTHNAME1           0x00000015  /* native name for January */
#define CAL_SMONTHNAME2           0x00000016  /* native name for February */
#define CAL_SMONTHNAME3           0x00000017  /* native name for March */
#define CAL_SMONTHNAME4           0x00000018  /* native name for April */
#define CAL_SMONTHNAME5           0x00000019  /* native name for May */
#define CAL_SMONTHNAME6           0x0000001a  /* native name for June */
#define CAL_SMONTHNAME7           0x0000001b  /* native name for July */
#define CAL_SMONTHNAME8           0x0000001c  /* native name for August */
#define CAL_SMONTHNAME9           0x0000001d  /* native name for September */
#define CAL_SMONTHNAME10          0x0000001e  /* native name for October */
#define CAL_SMONTHNAME11          0x0000001f  /* native name for November */
#define CAL_SMONTHNAME12          0x00000020  /* native name for December */
#define CAL_SMONTHNAME13          0x00000021  /* native name for 13th month (if any) */
#define CAL_SABBREVMONTHNAME1     0x00000022  /* abbreviated name for January */
#define CAL_SABBREVMONTHNAME2     0x00000023  /* abbreviated name for February */
#define CAL_SABBREVMONTHNAME3     0x00000024  /* abbreviated name for March */
#define CAL_SABBREVMONTHNAME4     0x00000025  /* abbreviated name for April */
#define CAL_SABBREVMONTHNAME5     0x00000026  /* abbreviated name for May */
#define CAL_SABBREVMONTHNAME6     0x00000027  /* abbreviated name for June */
#define CAL_SABBREVMONTHNAME7     0x00000028  /* abbreviated name for July */
#define CAL_SABBREVMONTHNAME8     0x00000029  /* abbreviated name for August */
#define CAL_SABBREVMONTHNAME9     0x0000002a  /* abbreviated name for September */
#define CAL_SABBREVMONTHNAME10    0x0000002b  /* abbreviated name for October */
#define CAL_SABBREVMONTHNAME11    0x0000002c  /* abbreviated name for November */
#define CAL_SABBREVMONTHNAME12    0x0000002d  /* abbreviated name for December */
#define CAL_SABBREVMONTHNAME13    0x0000002e  /* abbreviated name for 13th month (if any) */
#define CAL_SYEARMONTH            0x0000002f  /* year month format string */


#else // __APPLE__

#define LOCALE_SDECIMAL               0x0000000E    /* decimal separator */
#define LOCALE_STHOUSAND              0x0000000F    /* thousand separator */
#define LOCALE_ILZERO                 0x00000012    /* leading zeros for decimal */
#define LOCALE_SCURRENCY              0x00000014    /* local monetary symbol */
#define LOCALE_SMONDECIMALSEP         0x00000016    /* monetary decimal separator */
#define LOCALE_SMONTHOUSANDSEP        0x00000017    /* monetary thousand separator */

#endif // __APPLE__


#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
int
PALAPI
GetLocaleInfoW(
    IN LCID     Locale,
    IN LCTYPE   LCType,
    OUT LPWSTR  lpLCData,
    IN int      cchData);

#endif // ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
int
PALAPI
GetLocaleInfoEx(
    IN LPCWSTR  lpLocaleName,
    IN LCTYPE   LCType,
    OUT LPWSTR  lpLCData,
    IN int      cchData);


PALIMPORT
int 
PALAPI
CompareStringOrdinal(
    IN LPCWSTR lpString1, 
  IN int cchCount1, 
  IN LPCWSTR lpString2, 
  IN int cchCount2, 
  IN BOOL bIgnoreCase);

typedef struct _nlsversioninfoex { 
  DWORD  dwNLSVersionInfoSize; 
  DWORD  dwNLSVersion; 
  DWORD  dwDefinedVersion; 
  DWORD  dwEffectiveId;   
  GUID  guidCustomVersion; 
  } NLSVERSIONINFOEX, *LPNLSVERSIONINFOEX; 

PALIMPORT
int 
PALAPI
FindNLSStringEx(
    IN LPCWSTR lpLocaleName, 
  IN DWORD dwFindNLSStringFlags, 
  IN LPCWSTR lpStringSource, 
  IN int cchSource, 
    IN LPCWSTR lpStringValue, 
  IN int cchValue, 
  OUT LPINT pcchFound, 
  IN LPNLSVERSIONINFOEX lpVersionInformation, 
  IN LPVOID lpReserved, 
  IN LPARAM lParam );

typedef enum {
    COMPARE_STRING = 0x0001,
} NLS_FUNCTION;

PALIMPORT
BOOL 
PALAPI
IsNLSDefinedString(
    IN NLS_FUNCTION Function, 
  IN DWORD dwFlags, 
  IN LPNLSVERSIONINFOEX lpVersionInfo, 
  IN LPCWSTR lpString, 
  IN int cchStr );


PALIMPORT
int
PALAPI
ResolveLocaleName(
    IN LPCWSTR lpNameToResolve,
        OUT LPWSTR lpLocaleName,
        IN int cchLocaleName );

PALIMPORT
BOOL 
PALAPI
GetThreadPreferredUILanguages(
    IN DWORD  dwFlags,
    OUT PULONG  pulNumLanguages,
    OUT PWSTR  pwszLanguagesBuffer,
    IN OUT PULONG  pcchLanguagesBuffer);


PALIMPORT
int 
PALAPI
GetSystemDefaultLocaleName(
    OUT LPWSTR lpLocaleName, 
  IN int cchLocaleName);

#ifdef UNICODE
#define GetLocaleInfo GetLocaleInfoW
#endif

#if ENABLE_DOWNLEVEL_FOR_NLS
PALIMPORT
LCID
PALAPI
GetUserDefaultLCID(
           void);
#endif


PALIMPORT
int
PALAPI
GetUserDefaultLocaleName(
           OUT LPWSTR lpLocaleName,
           IN int cchLocaleName);


#define LCID_INSTALLED            0x00000001  // installed locale ids
#define LCID_SUPPORTED            0x00000002  // supported locale ids
#ifdef __APPLE__
#define LCID_ALTERNATE_SORTS      0x00000004  // alternate sort locale ids
#endif // __APPLE__

#if ENABLE_DOWNLEVEL_FOR_NLS
PALIMPORT
BOOL
PALAPI
IsValidLocale(
          IN LCID Locale,
          IN DWORD dwFlags);
#endif // ENABLE_DOWNLEVEL_FOR_NLS


typedef DWORD CALID;
typedef DWORD CALTYPE;

#define CAL_ITWODIGITYEARMAX 0x00000030 // two digit year max
#define CAL_RETURN_NUMBER    0x20000000 // return number instead of string

#define CAL_GREGORIAN                 1 // Gregorian (localized) calendar
#define CAL_GREGORIAN_US              2 // Gregorian (U.S.) calendar
#define CAL_JAPAN                     3 // Japanese Emperor Era calendar
#define CAL_TAIWAN                    4 // Taiwan Era calendar
#define CAL_KOREA                     5 // Korean Tangun Era calendar
#define CAL_HIJRI                     6 // Hijri (Arabic Lunar) calendar
#define CAL_THAI                      7 // Thai calendar
#define CAL_HEBREW                    8 // Hebrew (Lunar) calendar
#define CAL_GREGORIAN_ME_FRENCH       9 // Gregorian Middle East French calendar
#define CAL_GREGORIAN_ARABIC         10 // Gregorian Arabic calendar
#define CAL_GREGORIAN_XLIT_ENGLISH   11 // Gregorian Transliterated English calendar
#define CAL_GREGORIAN_XLIT_FRENCH    12 // Gregorian Transliterated French calendar
#define CAL_JULIAN                   13

#if ENABLE_DOWNLEVEL_FOR_NLS
PALIMPORT
int
PALAPI
GetCalendarInfoW(
         IN LCID Locale,
         IN CALID Calendar,
         IN CALTYPE CalType,
         OUT LPWSTR lpCalData,
         IN int cchData,
         OUT LPDWORD lpValue);

#ifdef UNICODE
#define GetCalendarInfo GetCalendarInfoW
#endif

#endif // ENABLE_DOWNLEVEL_FOR_NLS


PALIMPORT
int
PALAPI
GetCalendarInfoEx(
         IN LPCWSTR lpLocaleName,
         IN CALID Calendar,
         IN LPCWSTR lpReserved,
         IN CALTYPE CalType,
         OUT LPWSTR lpCalData,
         IN int cchData,
         OUT LPDWORD lpValue);

#if ENABLE_DOWNLEVEL_FOR_NLS
typedef BOOL (CALLBACK* LOCALE_ENUMPROCW)(LPWSTR);

PALIMPORT
BOOL
PALAPI
EnumSystemLocalesW(
    IN LOCALE_ENUMPROCW lpLocaleEnumProc,
    IN DWORD            dwFlags);
#endif //  ENABLE_DOWNLEVEL_FOR_NLS

#define DATE_SHORTDATE            0x00000001  // use short date picture
#define DATE_LONGDATE             0x00000002  // use long date picture
#define DATE_YEARMONTH            0x00000008  // use year month picture

typedef BOOL (CALLBACK* DATEFMT_ENUMPROCEXW)(LPWSTR, CALID);

#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
BOOL
PALAPI
EnumDateFormatsExW(
    IN DATEFMT_ENUMPROCEXW lpDateFmtEnumProcEx,
    IN LCID                Locale,
    IN DWORD               dwFlags);

#else // ENABLE_DOWNLEVEL_FOR_NLS

typedef BOOL (CALLBACK* DATEFMT_ENUMPROCEXEXW)(LPWSTR, CALID, LPARAM);

PALIMPORT
BOOL
PALAPI
EnumDateFormatsExEx(
    IN DATEFMT_ENUMPROCEXEXW lpDateFmtEnumProcEx,
    IN LPCWSTR          lpLocaleName,
    IN DWORD               dwFlags,
    IN LPARAM      lParam);

#endif // ENABLE_DOWNLEVEL_FOR_NLS

typedef BOOL (CALLBACK* TIMEFMT_ENUMPROCW)(LPWSTR);

#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
BOOL
PALAPI
EnumTimeFormatsW(
    IN TIMEFMT_ENUMPROCW lpTimeFmtEnumProc,
    IN LCID              Locale,
    IN DWORD             dwFlags);

#else // ENABLE_DOWNLEVEL_FOR_NLS

typedef BOOL (CALLBACK* TIMEFMT_ENUMPROCEXW)(LPWSTR, LPARAM);

PALIMPORT
BOOL
PALAPI
EnumTimeFormatsEx(
    IN TIMEFMT_ENUMPROCEXW lpTimeFmtEnumProc,
    IN LPCWSTR          lpLocaleName,
    IN DWORD             dwFlags,
    IN LPARAM    lParam);

#endif // ENABLE_DOWNLEVEL_FOR_NLS

#define ENUM_ALL_CALENDARS        0xffffffff  // enumerate all calendars
#define CAL_ICALINTVALUE          0x00000001  // calendar type
#define CAL_NOUSEROVERRIDE        LOCALE_NOUSEROVERRIDE  // do not use user overrides
#define CAL_SCALNAME              0x00000002  // native name of calendar

typedef BOOL (CALLBACK* CALINFO_ENUMPROCEXW)(LPWSTR,CALID);

#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
BOOL
PALAPI
EnumCalendarInfoExW(
    IN CALINFO_ENUMPROCEXW lpCalInfoEnumProc,
    IN LCID              Locale,
    IN CALID             Calendar,
    IN CALTYPE           CalType);

#else // ENABLE_DOWNLEVEL_FOR_NLS

typedef BOOL (CALLBACK* CALINFO_ENUMPROCEXEXW)(LPWSTR, CALID, LPWSTR, LPARAM);

PALIMPORT
BOOL
PALAPI
EnumCalendarInfoExEx(
    IN CALINFO_ENUMPROCEXEXW lpCalInfoEnumProc,
    IN LPCWSTR          lpLocaleName,
    IN CALID             Calendar,
    IN LPCWSTR           lpReserved,
    IN CALTYPE           CalType,
    IN LPARAM        lParam);

#endif // ENABLE_DOWNLEVEL_FOR_NLS

#define LCMAP_LOWERCASE  0x00000100
#define LCMAP_UPPERCASE  0x00000200

#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
int
PALAPI
LCMapStringW(
    IN LCID    Locale,
    IN DWORD   dwMapFlags,
    IN LPCWSTR lpSrcStr,
    IN int     cchSrc,
    OUT LPWSTR lpDestStr,
    IN int     cchDest);

#ifdef UNICODE
#define LCMapString LCMapStringW
#endif


#endif // ENABLE_DOWNLEVEL_FOR_NLS


PALIMPORT
int
PALAPI
LCMapStringEx(
    IN LPCWSTR    lpLocaleName,
    IN DWORD   dwMapFlags,
    IN LPCWSTR lpSrcStr,
    IN int     cchSrc,
    OUT LPWSTR lpDestStr,
    IN int     cchDest,
    IN LPNLSVERSIONINFO lpVersionInformation, 
    IN LPVOID lpReserved, 
    IN LPARAM lParam );

PALIMPORT
int
PALAPI
PAL_LCMapCharW(
    IN LPCWSTR    lpLocaleName,
    IN DWORD   dwMapFlags,
    IN WCHAR   srcChar,
    OUT WCHAR  *destChar,
    LPNLSVERSIONINFO lpVersionInformation,
    LPVOID lpReserved,
    LPARAM lParam );

PALIMPORT
int
PALAPI
PAL_NormalizeStringExW(
    IN LPCWSTR    lpLocaleName,
    IN DWORD   dwMapFlags,
    IN LPCWSTR lpSrcStr,
    IN int     cchSrc,
    OUT LPWSTR lpDestStr,
    IN int     cchDest);

PALIMPORT
int
PALAPI
PAL_ParseDateW(
    IN LPCWSTR   lpLocaleName,
    IN LPCWSTR   lpFormat,
    IN LPCWSTR   lpString,
    OUT LPSYSTEMTIME lpTime);

PALIMPORT
int
PALAPI
PAL_GetCalendar(
    IN LPCWSTR   lpLocaleName,
    OUT CALID*   pCalendar);

#define GEOID_NOT_AVAILABLE -1

// "a number", might represent different types
typedef struct PALNUMBER__* PALNUMBER;

// return NULL on OOM
PALIMPORT PALNUMBER PALAPI PAL_DoubleToNumber(double);
PALIMPORT PALNUMBER PALAPI PAL_Int64ToNumber(INT64);
PALIMPORT PALNUMBER PALAPI PAL_UInt64ToNumber(UINT64);
PALIMPORT PALNUMBER PALAPI PAL_IntToNumber(int);
PALIMPORT PALNUMBER PALAPI PAL_UIntToNumber(unsigned int);

PALIMPORT void PALAPI PAL_ReleaseNumber(PALNUMBER);


// return string length if Buffer is NULL or the result fits in cchBuffer, otherwise -1
PALIMPORT int PALAPI PAL_FormatScientific(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number, int nMinDigits, int nMaxDigits,
                                                                      LPCWSTR sExponent, LPCWSTR sNumberDecimal, LPCWSTR sPositive, LPCWSTR sNegative, LPCWSTR sZero);

PALIMPORT int PALAPI  PAL_FormatCurrency(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number, int nMinDigits, int nMaxDigits, int iNegativeFormat, int iPositiveFormat,
                      int iPrimaryGroup, int iSecondaryGroup, LPCWSTR sCurrencyDecimal, LPCWSTR sCurrencyGroup, LPCWSTR sNegative, LPCWSTR sCurrency, LPCWSTR sZero);

PALIMPORT int PALAPI  PAL_FormatPercent(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number,  int nMinDigits, int nMaxDigits,int iNegativeFormat, int iPositiveFormat, 
                      int iPrimaryGroup, int iSecondaryGroup, LPCWSTR sPercentDecimal, LPCWSTR sPercentGroup, LPCWSTR sNegative, LPCWSTR sPercent, LPCWSTR sZero);

PALIMPORT int PALAPI  PAL_FormatDecimal(LPCWSTR sLocale, LPWSTR pBuffer, SIZE_T cchBuffer, PALNUMBER number, int nMinDigits, int nMaxDigits, int iNegativeFormat,
                                    int iPrimaryGroup, int iSecondaryGroup,  LPCWSTR sDecimal, LPCWSTR sGroup, LPCWSTR sNegative, LPCWSTR sZero);


#define DATE_USE_ALT_CALENDAR 0x00000004

#if ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
int
PALAPI
GetDateFormatW(
           IN LCID Locale,
           IN DWORD dwFlags,
           IN CONST SYSTEMTIME *lpDate,
           IN LPCWSTR lpFormat,
           OUT LPWSTR lpDateStr,
           IN int cchDate);

#else

PALIMPORT
int
PALAPI
GetDateFormatEx(
           IN LPCWSTR Locale,
           IN DWORD dwFlags,
           IN CONST SYSTEMTIME *lpDate,
           IN LPCWSTR lpFormat,
           OUT LPWSTR lpDateStr,
           IN int cchDate,
           IN LPCWSTR lpCalendar);


#endif // ENABLE_DOWNLEVEL_FOR_NLS

PALIMPORT
int
PALAPI
GetDateFormatEx(
           IN LPCWSTR lpLocaleName,
           IN DWORD dwFlags,
           IN CONST SYSTEMTIME *lpDate,
           IN LPCWSTR lpFormat,
           OUT LPWSTR lpDateStr,
           IN int cchDate,
           LPCWSTR lpCalendar);


#ifdef UNICODE
#define GetDateFormat GetDateFormatW
#endif


PALIMPORT
int
PALAPI
PAL_GetResourceString(
        IN LPCSTR lpDomain,
        IN LPCSTR lpResourceStr,
        OUT LPWSTR lpWideCharStr,
        IN int cchWideChar);

PALIMPORT
BOOL
PALAPI
PAL_BindResources(IN LPCSTR lpDomain);

#define EXCEPTION_NONCONTINUABLE 0x1
#define EXCEPTION_UNWINDING 0x2

#ifdef FEATURE_PAL_SXS

#define EXCEPTION_EXIT_UNWIND 0x4       // Exit unwind is in progress (not used by PAL SEH)
#define EXCEPTION_NESTED_CALL 0x10      // Nested exception handler call
#define EXCEPTION_TARGET_UNWIND 0x20    // Target unwind in progress
#define EXCEPTION_COLLIDED_UNWIND 0x40  // Collided exception handler call
#define EXCEPTION_SKIP_VEH 0x200

#define EXCEPTION_UNWIND (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND | \
                          EXCEPTION_TARGET_UNWIND | EXCEPTION_COLLIDED_UNWIND)

#define IS_DISPATCHING(Flag) ((Flag & EXCEPTION_UNWIND) == 0)
#define IS_UNWINDING(Flag) ((Flag & EXCEPTION_UNWIND) != 0)
#define IS_TARGET_UNWIND(Flag) (Flag & EXCEPTION_TARGET_UNWIND)

#endif // FEATURE_PAL_SXS

#define EXCEPTION_IS_SIGNAL 0x100

#define EXCEPTION_MAXIMUM_PARAMETERS 15

// Index in the ExceptionInformation array where we will keep the reference
// to the native exception that needs to be deleted when dispatching
// exception in managed code.
#define NATIVE_EXCEPTION_ASYNC_SLOT (EXCEPTION_MAXIMUM_PARAMETERS-1)

typedef struct _EXCEPTION_RECORD {
    DWORD ExceptionCode;
    DWORD ExceptionFlags;
    struct _EXCEPTION_RECORD *ExceptionRecord;
    PVOID ExceptionAddress;
    DWORD NumberParameters;
    ULONG_PTR ExceptionInformation[EXCEPTION_MAXIMUM_PARAMETERS];
} EXCEPTION_RECORD, *PEXCEPTION_RECORD;

typedef struct _EXCEPTION_POINTERS {
    PEXCEPTION_RECORD ExceptionRecord;
    PCONTEXT ContextRecord;
} EXCEPTION_POINTERS, *PEXCEPTION_POINTERS, *LPEXCEPTION_POINTERS;

#ifdef FEATURE_PAL_SXS

typedef LONG EXCEPTION_DISPOSITION;

enum {
    ExceptionContinueExecution,
    ExceptionContinueSearch,
    ExceptionNestedException,
    ExceptionCollidedUnwind,
};

#endif // FEATURE_PAL_SXS

//
// A function table entry is generated for each frame function.
//
typedef struct _RUNTIME_FUNCTION {
    DWORD BeginAddress;
    DWORD EndAddress;
    DWORD UnwindData;
} RUNTIME_FUNCTION, *PRUNTIME_FUNCTION;

PALIMPORT
BOOL
PALAPI
WriteProcessMemory(IN HANDLE hProcess,
                   IN LPVOID lpBaseAddress,
                   IN LPCVOID lpBuffer,
                   IN SIZE_T nSize,
                   OUT SIZE_T * lpNumberOfBytesWritten);

#define STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
#define SYNCHRONIZE               (0x00100000L)
#define READ_CONTROL              (0x00020000L)

#define EVENT_MODIFY_STATE        (0x0002)
#define EVENT_ALL_ACCESS          (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | \
                                   0x3) 

#define MUTANT_QUERY_STATE        (0x0001)
#define MUTANT_ALL_ACCESS         (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | \
                                   MUTANT_QUERY_STATE)
#define MUTEX_ALL_ACCESS          MUTANT_ALL_ACCESS

#define SEMAPHORE_MODIFY_STATE    (0x0002)
#define SEMAPHORE_ALL_ACCESS      (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | \
                                   0x3)

#define PROCESS_TERMINATE         (0x0001)  
#define PROCESS_CREATE_THREAD     (0x0002)  
#define PROCESS_SET_SESSIONID     (0x0004)  
#define PROCESS_VM_OPERATION      (0x0008)  
#define PROCESS_VM_READ           (0x0010)  
#define PROCESS_VM_WRITE          (0x0020)  
#define PROCESS_DUP_HANDLE        (0x0040)  
#define PROCESS_CREATE_PROCESS    (0x0080)  
#define PROCESS_SET_QUOTA         (0x0100)  
#define PROCESS_SET_INFORMATION   (0x0200)  
#define PROCESS_QUERY_INFORMATION (0x0400)  
#define PROCESS_SUSPEND_RESUME    (0x0800)  
#define PROCESS_ALL_ACCESS        (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | \
                                   0xFFF)

PALIMPORT
HANDLE
PALAPI
OpenProcess(
    IN DWORD dwDesiredAccess, /* PROCESS_DUP_HANDLE or PROCESS_ALL_ACCESS */
    IN BOOL bInheritHandle,
    IN DWORD dwProcessId
    );

PALIMPORT
BOOL
PALAPI
EnumProcessModules(
    IN HANDLE hProcess,
    OUT HMODULE *lphModule,
    IN DWORD cb,
    OUT LPDWORD lpcbNeeded
    );

PALIMPORT
VOID
PALAPI
OutputDebugStringA(
    IN LPCSTR lpOutputString);

PALIMPORT
VOID
PALAPI
OutputDebugStringW(
    IN LPCWSTR lpOutputStrig);

#ifdef UNICODE
#define OutputDebugString OutputDebugStringW
#else
#define OutputDebugString OutputDebugStringA
#endif

PALIMPORT
VOID
PALAPI
DebugBreak(
       VOID);

PALIMPORT
LPWSTR
PALAPI
lstrcatW(
     IN OUT LPWSTR lpString1,
     IN LPCWSTR lpString2);

#ifdef UNICODE
#define lstrcat lstrcatW
#endif

PALIMPORT
LPWSTR
PALAPI
lstrcpyW(
     OUT LPWSTR lpString1,
     IN LPCWSTR lpString2);

#ifdef UNICODE
#define lstrcpy lstrcpyW
#endif

PALIMPORT
int
PALAPI
lstrlenA(
     IN LPCSTR lpString);

PALIMPORT
int
PALAPI
lstrlenW(
     IN LPCWSTR lpString);

#ifdef UNICODE
#define lstrlen lstrlenW
#else
#define lstrlen lstrlenA
#endif

PALIMPORT
LPWSTR
PALAPI
lstrcpynW(
      OUT LPWSTR lpString1,
      IN LPCWSTR lpString2,
      IN int iMaxLength);

#ifdef UNICODE
#define lstrcpyn lstrcpynW
#endif


PALIMPORT
DWORD
PALAPI
GetEnvironmentVariableW(
            IN LPCWSTR lpName,
            OUT LPWSTR lpBuffer,
            IN DWORD nSize);

#ifdef UNICODE
#define GetEnvironmentVariable GetEnvironmentVariableW
#else
#define GetEnvironmentVariable GetEnvironmentVariableA
#endif

PALIMPORT
BOOL
PALAPI
SetEnvironmentVariableW(
            IN LPCWSTR lpName,
            IN LPCWSTR lpValue);

#ifdef UNICODE
#define SetEnvironmentVariable SetEnvironmentVariableW
#else
#define SetEnvironmentVariable SetEnvironmentVariableA
#endif

PALIMPORT
LPWSTR
PALAPI
GetEnvironmentStringsW(
               VOID);

#ifdef UNICODE
#define GetEnvironmentStrings GetEnvironmentStringsW
#else
#define GetEnvironmentStrings GetEnvironmentStringsA
#endif

PALIMPORT
BOOL
PALAPI
FreeEnvironmentStringsW(
            IN LPWSTR);

#ifdef UNICODE
#define FreeEnvironmentStrings FreeEnvironmentStringsW
#else
#define FreeEnvironmentStrings FreeEnvironmentStringsA
#endif

PALIMPORT
BOOL
PALAPI
CloseHandle(
        IN OUT HANDLE hObject);

PALIMPORT
VOID
PALAPI
RaiseException(
           IN DWORD dwExceptionCode,
           IN DWORD dwExceptionFlags,
           IN DWORD nNumberOfArguments,
           IN CONST ULONG_PTR *lpArguments);

#ifdef FEATURE_PAL_SXS
PALIMPORT
PAL_NORETURN
VOID
PALAPI
PAL_RaiseException(
           IN PEXCEPTION_POINTERS ExceptionPointers);
#endif // FEATURE_PAL_SXS

PALIMPORT
DWORD
PALAPI
GetTickCount(
         VOID);

PALIMPORT
ULONGLONG
PALAPI
GetTickCount64();

PALIMPORT
BOOL
PALAPI
QueryPerformanceCounter(
    OUT LARGE_INTEGER *lpPerformanceCount
    );

PALIMPORT
BOOL
PALAPI
QueryPerformanceFrequency(
    OUT LARGE_INTEGER *lpFrequency
    );

PALIMPORT
BOOL
PALAPI
QueryThreadCycleTime(
    IN HANDLE ThreadHandle,
    OUT PULONG64 CycleTime);

#ifndef FEATURE_PAL_SXS

typedef LONG (PALAPI *PTOP_LEVEL_EXCEPTION_FILTER)(
                           struct _EXCEPTION_POINTERS *ExceptionInfo);
typedef PTOP_LEVEL_EXCEPTION_FILTER LPTOP_LEVEL_EXCEPTION_FILTER;

PALIMPORT
LPTOP_LEVEL_EXCEPTION_FILTER
PALAPI
SetUnhandledExceptionFilter(
                IN LPTOP_LEVEL_EXCEPTION_FILTER lpTopLevelExceptionFilter);

#else // FEATURE_PAL_SXS

typedef EXCEPTION_DISPOSITION (PALAPI *PVECTORED_EXCEPTION_HANDLER)(
                           struct _EXCEPTION_POINTERS *ExceptionPointers);

#endif // FEATURE_PAL_SXS

// Define BitScanForward64 and BitScanForward
// Per MSDN, BitScanForward64 will search the mask data from LSB to MSB for a set bit.
// If one is found, its bit position is returned in the out PDWORD argument and 1 is returned.
// Otherwise, 0 is returned.
//
// On GCC, the equivalent function is __builtin_ffsl. It returns 1+index of the least
// significant set bit, or 0 if if mask is zero.
//
// The same is true for BitScanForward, except that the GCC function is __builtin_ffs.
EXTERN_C
PALIMPORT
inline
unsigned char
PALAPI
BitScanForward(
    IN OUT PDWORD Index,
    IN UINT qwMask)
{
    unsigned char bRet = FALSE;
    int iIndex = __builtin_ffsl(qwMask);
    if (iIndex != 0)
    {
        // Set the Index after deducting unity
        *Index = (DWORD)(iIndex - 1);
        bRet = TRUE;
    }

    return bRet;
}

EXTERN_C
PALIMPORT
inline
unsigned char
PALAPI
BitScanForward64(
    IN OUT PDWORD Index,
    IN UINT64 qwMask)
{
    unsigned char bRet = FALSE;
    int iIndex = __builtin_ffsl(qwMask);
    if (iIndex != 0)
    {
        // Set the Index after deducting unity
        *Index = (DWORD)(iIndex - 1);
        bRet = TRUE;
    }

    return bRet;
}

/*++
Function:
InterlockedIncrement

The InterlockedIncrement function increments (increases by one) the
value of the specified variable and checks the resulting value. The
function prevents more than one thread from using the same variable
simultaneously.

Parameters

lpAddend 
[in/out] Pointer to the variable to increment. 

Return Values

The return value is the resulting incremented value. 

--*/
EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedIncrement(
    IN OUT LONG volatile *lpAddend)
{
    return __sync_add_and_fetch(lpAddend, (LONG)1);
}

EXTERN_C
PALIMPORT
inline
LONGLONG
PALAPI
InterlockedIncrement64(
    IN OUT LONGLONG volatile *lpAddend)
{
    return __sync_add_and_fetch(lpAddend, (LONGLONG)1);
}

/*++
Function:
InterlockedDecrement

The InterlockedDecrement function decrements (decreases by one) the
value of the specified variable and checks the resulting value. The
function prevents more than one thread from using the same variable
simultaneously.

Parameters

lpAddend 
[in/out] Pointer to the variable to decrement. 

Return Values

The return value is the resulting decremented value.

--*/
EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedDecrement(
    IN OUT LONG volatile *lpAddend)
{
    return __sync_sub_and_fetch(lpAddend, (LONG)1);
}

EXTERN_C
PALIMPORT
inline
LONGLONG
PALAPI
InterlockedDecrement64(
    IN OUT LONGLONG volatile *lpAddend)
{
    return __sync_sub_and_fetch(lpAddend, (LONGLONG)1);
}

/*++
Function:
InterlockedExchange

The InterlockedExchange function atomically exchanges a pair of
values. The function prevents more than one thread from using the same
variable simultaneously.

Parameters

Target 
[in/out] Pointer to the value to exchange. The function sets
this variable to Value, and returns its prior value.
Value 
[in] Specifies a new value for the variable pointed to by Target. 

Return Values

The function returns the initial value pointed to by Target. 

--*/
EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedExchange(
    IN OUT LONG volatile *Target,
    IN LONG Value)
{
    return __sync_swap(Target, Value);
}

EXTERN_C
PALIMPORT
inline
LONGLONG
PALAPI
InterlockedExchange64(
    IN OUT LONGLONG volatile *Target,
    IN LONGLONG Value)
{
    return __sync_swap(Target, Value);
}

/*++
Function:
InterlockedCompareExchange

The InterlockedCompareExchange function performs an atomic comparison
of the specified values and exchanges the values, based on the outcome
of the comparison. The function prevents more than one thread from
using the same variable simultaneously.

If you are exchanging pointer values, this function has been
superseded by the InterlockedCompareExchangePointer function.

Parameters

Destination     [in/out] Specifies the address of the destination value. The sign is ignored.
Exchange        [in]     Specifies the exchange value. The sign is ignored.
Comperand       [in]     Specifies the value to compare to Destination. The sign is ignored.

Return Values

The return value is the initial value of the destination.

--*/
EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedCompareExchange(
    IN OUT LONG volatile *Destination,
    IN LONG Exchange,
    IN LONG Comperand)
{
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}

EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedCompareExchangeAcquire(
    IN OUT LONG volatile *Destination,
    IN LONG Exchange,
    IN LONG Comperand)
{
    // TODO: implement the version with only the acquire semantics
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}

EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedCompareExchangeRelease(
    IN OUT LONG volatile *Destination,
    IN LONG Exchange,
    IN LONG Comperand)
{
    // TODO: implement the version with only the release semantics
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}

// See the 32-bit variant in interlock2.s
EXTERN_C
PALIMPORT
inline
LONGLONG
PALAPI
InterlockedCompareExchange64(
    IN OUT LONGLONG volatile *Destination,
    IN LONGLONG Exchange,
    IN LONGLONG Comperand)
{
    return __sync_val_compare_and_swap(
        Destination, /* The pointer to a variable whose value is to be compared with. */
        Comperand, /* The value to be compared */
        Exchange /* The value to be stored */);
}

/*++
Function:
InterlockedExchangeAdd

The InterlockedExchangeAdd function atomically adds the value of 'Value'
to the variable that 'Addend' points to.

Parameters

lpAddend
[in/out] Pointer to the variable to to added.

Return Values

The return value is the original value that 'Addend' pointed to.

--*/
EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedExchangeAdd(
    IN OUT LONG volatile *Addend,
    IN LONG Value)
{
    return __sync_fetch_and_add(Addend, Value);
}

EXTERN_C
PALIMPORT
inline
LONGLONG
PALAPI
InterlockedExchangeAdd64(
    IN OUT LONGLONG volatile *Addend,
    IN LONGLONG Value)
{
    return __sync_fetch_and_add(Addend, Value);
}

EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedAnd(
    IN OUT LONG volatile *Destination,
    IN LONG Value)
{
    return __sync_fetch_and_and(Destination, Value);
}

EXTERN_C
PALIMPORT
inline
LONG
PALAPI
InterlockedOr(
    IN OUT LONG volatile *Destination,
    IN LONG Value)
{
    return __sync_fetch_and_or(Destination, Value);
}

EXTERN_C
PALIMPORT
inline
UCHAR
PALAPI
InterlockedBitTestAndReset(
    IN OUT LONG volatile *Base,
    IN LONG Bit)
{
    return (InterlockedAnd(Base, ~(1 << Bit)) & (1 << Bit)) != 0;
}

EXTERN_C
PALIMPORT
inline
UCHAR
PALAPI
InterlockedBitTestAndSet(
    IN OUT LONG volatile *Base,
    IN LONG Bit)
{
    return (InterlockedOr(Base, (1 << Bit)) & (1 << Bit)) != 0;
}

#if defined(BIT64)
#define InterlockedExchangePointer(Target, Value) \
    ((PVOID)InterlockedExchange64((PLONG64)(Target), (LONGLONG)(Value)))

#define InterlockedCompareExchangePointer(Destination, ExChange, Comperand) \
    ((PVOID)InterlockedCompareExchange64((PLONG64)(Destination), (LONGLONG)(ExChange), (LONGLONG)(Comperand)))
#else
#define InterlockedExchangePointer(Target, Value) \
    ((PVOID)(UINT_PTR)InterlockedExchange((PLONG)(UINT_PTR)(Target), (LONG)(UINT_PTR)(Value)))

#define InterlockedCompareExchangePointer(Destination, ExChange, Comperand) \
    ((PVOID)(UINT_PTR)InterlockedCompareExchange((PLONG)(UINT_PTR)(Destination), (LONG)(UINT_PTR)(ExChange), (LONG)(UINT_PTR)(Comperand)))
#endif

/*++
Function:
MemoryBarrier

The MemoryBarrier function creates a full memory barrier.

--*/
EXTERN_C
PALIMPORT
inline
VOID
PALAPI
MemoryBarrier(
    VOID)
{
    __sync_synchronize();
}

PALIMPORT
VOID
PALAPI
YieldProcessor(
    VOID);

PALIMPORT
DWORD
PALAPI
GetCurrentProcessorNumber();

/*++
Function:
PAL_HasGetCurrentProcessorNumber

Checks if GetCurrentProcessorNumber is available in the current environment

--*/
PALIMPORT
BOOL
PALAPI
PAL_HasGetCurrentProcessorNumber();
    
#define FORMAT_MESSAGE_ALLOCATE_BUFFER 0x00000100
#define FORMAT_MESSAGE_IGNORE_INSERTS  0x00000200
#define FORMAT_MESSAGE_FROM_STRING     0x00000400
#define FORMAT_MESSAGE_FROM_SYSTEM     0x00001000
#define FORMAT_MESSAGE_ARGUMENT_ARRAY  0x00002000
#define FORMAT_MESSAGE_MAX_WIDTH_MASK  0x000000FF

PALIMPORT
DWORD
PALAPI
FormatMessageW(
           IN DWORD dwFlags,
           IN LPCVOID lpSource,
           IN DWORD dwMessageId,
           IN DWORD dwLanguageId,
           OUT LPWSTR lpBffer,
           IN DWORD nSize,
           IN va_list *Arguments);

#ifdef UNICODE
#define FormatMessage FormatMessageW
#endif


PALIMPORT
DWORD
PALAPI
GetLastError(
         VOID);

PALIMPORT
VOID
PALAPI
SetLastError(
         IN DWORD dwErrCode);

PALIMPORT
LPWSTR
PALAPI
GetCommandLineW(
        VOID);

#ifdef UNICODE
#define GetCommandLine GetCommandLineW
#endif

PALIMPORT
VOID 
PALAPI 
RtlRestoreContext(
  IN PCONTEXT ContextRecord,
  IN PEXCEPTION_RECORD ExceptionRecord
);

PALIMPORT
VOID 
PALAPI 
RtlCaptureContext(
  OUT PCONTEXT ContextRecord
);

PALIMPORT
UINT 
PALAPI 
GetWriteWatch(
  IN DWORD dwFlags,
  IN PVOID lpBaseAddress,
  IN SIZE_T dwRegionSize,
  OUT PVOID *lpAddresses,
  IN OUT PULONG_PTR lpdwCount,
  OUT PULONG lpdwGranularity
);

PALIMPORT
UINT 
PALAPI 
ResetWriteWatch(
  IN LPVOID lpBaseAddress,
  IN SIZE_T dwRegionSize
);

PALIMPORT
VOID 
PALAPI 
FlushProcessWriteBuffers();

typedef void (*PAL_ActivationFunction)(CONTEXT *context);
typedef BOOL (*PAL_SafeActivationCheckFunction)(SIZE_T ip, BOOL checkingCurrentThread);

PALIMPORT
VOID
PALAPI
PAL_SetActivationFunction(
    IN PAL_ActivationFunction pActivationFunction,
    IN PAL_SafeActivationCheckFunction pSafeActivationCheckFunction);

PALIMPORT
BOOL
PALAPI
PAL_InjectActivation(
    IN HANDLE hThread
);

#define VER_PLATFORM_WIN32_WINDOWS        1
#define VER_PLATFORM_WIN32_NT        2
#define VER_PLATFORM_UNIX            10
#define VER_PLATFORM_MACOSX          11

typedef struct _OSVERSIONINFOA {
    DWORD dwOSVersionInfoSize;
    DWORD dwMajorVersion;
    DWORD dwMinorVersion;
    DWORD dwBuildNumber;
    DWORD dwPlatformId;
    CHAR szCSDVersion[ 128 ];
} OSVERSIONINFOA, *POSVERSIONINFOA, *LPOSVERSIONINFOA;

typedef struct _OSVERSIONINFOW {
    DWORD dwOSVersionInfoSize;
    DWORD dwMajorVersion;
    DWORD dwMinorVersion;
    DWORD dwBuildNumber;
    DWORD dwPlatformId;
    WCHAR szCSDVersion[ 128 ];
} OSVERSIONINFOW, *POSVERSIONINFOW, *LPOSVERSIONINFOW;

#ifdef UNICODE
typedef OSVERSIONINFOW OSVERSIONINFO;
typedef POSVERSIONINFOW POSVERSIONINFO;
typedef LPOSVERSIONINFOW LPOSVERSIONINFO;
#else
typedef OSVERSIONINFOA OSVERSIONINFO;
typedef POSVERSIONINFOA POSVERSIONINFO;
typedef LPOSVERSIONINFOA LPOSVERSIONINFO;
#endif

typedef struct _OSVERSIONINFOEXA {
    DWORD dwOSVersionInfoSize;
    DWORD dwMajorVersion;
    DWORD dwMinorVersion;
    DWORD dwBuildNumber;
    DWORD dwPlatformId;
    CHAR szCSDVersion[ 128 ];
    WORD  wServicePackMajor;
    WORD  wServicePackMinor;
    WORD  wSuiteMask;
    BYTE  wProductType;
    BYTE  wReserved;
} OSVERSIONINFOEXA, *POSVERSIONINFOEXA, *LPOSVERSIONINFOEXA;

typedef struct _OSVERSIONINFOEXW {
    DWORD dwOSVersionInfoSize;
    DWORD dwMajorVersion;
    DWORD dwMinorVersion;
    DWORD dwBuildNumber;
    DWORD dwPlatformId;
    WCHAR szCSDVersion[ 128 ];
    WORD  wServicePackMajor;
    WORD  wServicePackMinor;
    WORD  wSuiteMask;
    BYTE  wProductType;
    BYTE  wReserved;
} OSVERSIONINFOEXW, *POSVERSIONINFOEXW, *LPOSVERSIONINFOEXW;

#ifdef UNICODE
typedef OSVERSIONINFOEXW OSVERSIONINFOEX;
typedef POSVERSIONINFOEXW POSVERSIONINFOEX;
typedef LPOSVERSIONINFOEXW LPOSVERSIONINFOEX;
#else
typedef OSVERSIONINFOEXA OSVERSIONINFOEX;
typedef POSVERSIONINFOEXA POSVERSIONINFOEX;
typedef LPOSVERSIONINFOEXA LPOSVERSIONINFOEX;
#endif

PALIMPORT
BOOL
PALAPI
GetVersionExW(
          IN OUT LPOSVERSIONINFOW lpVersionInformation);

#ifdef UNICODE
#define GetVersionEx GetVersionExW
#else
#define GetVersionEx GetVersionExA
#endif

#define IMAGE_FILE_MACHINE_I386              0x014c
#define IMAGE_FILE_MACHINE_ARM64             0xAA64  // ARM64 Little-Endian

typedef struct _SYSTEM_INFO {
    WORD wProcessorArchitecture_PAL_Undefined;
    WORD wReserved_PAL_Undefined; // NOTE: diff from winbase.h - no obsolete dwOemId union
    DWORD dwPageSize;
    LPVOID lpMinimumApplicationAddress;
    LPVOID lpMaximumApplicationAddress;
    DWORD_PTR dwActiveProcessorMask_PAL_Undefined;
    DWORD dwNumberOfProcessors;
    DWORD dwProcessorType_PAL_Undefined;
    DWORD dwAllocationGranularity;
    WORD wProcessorLevel_PAL_Undefined;
    WORD wProcessorRevision_PAL_Undefined;
} SYSTEM_INFO, *LPSYSTEM_INFO;

PALIMPORT
VOID
PALAPI
GetSystemInfo(
          OUT LPSYSTEM_INFO lpSystemInfo);

PALIMPORT
BOOL
PALAPI
GetDiskFreeSpaceW(
          LPCWSTR lpDirectoryName,
          LPDWORD lpSectorsPerCluster,
          LPDWORD lpBytesPerSector,
          LPDWORD lpNumberOfFreeClusters,
          LPDWORD lpTotalNumberOfClusters);

#ifdef UNICODE
#define GetDiskFreeSpace GetDiskFreeSpaceW
#endif

PALIMPORT
BOOL
PALAPI
CreatePipe(
    OUT PHANDLE hReadPipe,
    OUT PHANDLE hWritePipe,
    IN LPSECURITY_ATTRIBUTES lpPipeAttributes,
    IN DWORD nSize
    );

PALIMPORT
BOOL
PALAPI
DeregisterEventSource (
    IN HANDLE hEventLog
    );

PALIMPORT
HANDLE
PALAPI
RegisterEventSourceA (
    IN OPTIONAL LPCSTR lpUNCServerName,
    IN     LPCSTR lpSourceName
    );
PALIMPORT
HANDLE
PALAPI
RegisterEventSourceW (
    IN OPTIONAL LPCWSTR lpUNCServerName,
    IN     LPCWSTR lpSourceName
    );
#ifdef UNICODE
#define RegisterEventSource  RegisterEventSourceW
#else
#define RegisterEventSource  RegisterEventSourceA
#endif // !UNICODE

//
// The types of events that can be logged.
//
#define EVENTLOG_SUCCESS                0x0000
#define EVENTLOG_ERROR_TYPE             0x0001
#define EVENTLOG_WARNING_TYPE           0x0002
#define EVENTLOG_INFORMATION_TYPE       0x0004
#define EVENTLOG_AUDIT_SUCCESS          0x0008
#define EVENTLOG_AUDIT_FAILURE          0x0010

PALIMPORT
BOOL
PALAPI
ReportEventA (
    IN     HANDLE     hEventLog,
    IN     WORD       wType,
    IN     WORD       wCategory,
    IN     DWORD      dwEventID,
    IN OPTIONAL PSID       lpUserSid,
    IN     WORD       wNumStrings,
    IN     DWORD      dwDataSize,
    IN OPTIONAL LPCSTR *lpStrings,
    IN OPTIONAL LPVOID lpRawData
    );
PALIMPORT
BOOL
PALAPI
ReportEventW (
    IN     HANDLE     hEventLog,
    IN     WORD       wType,
    IN     WORD       wCategory,
    IN     DWORD      dwEventID,
    IN OPTIONAL PSID       lpUserSid,
    IN     WORD       wNumStrings,
    IN     DWORD      dwDataSize,
    IN OPTIONAL LPCWSTR *lpStrings,
    IN OPTIONAL LPVOID lpRawData
    );
#ifdef UNICODE
#define ReportEvent  ReportEventW
#else
#define ReportEvent  ReportEventA
#endif // !UNICODE

PALIMPORT
HRESULT
PALAPI
CoCreateGuid(OUT GUID * pguid);

#if defined FEATURE_PAL_ANSI
#include "palprivate.h"
#endif //FEATURE_PAL_ANSI
/******************* C Runtime Entrypoints *******************************/

/* Some C runtime functions needs to be reimplemented by the PAL.
   To avoid name collisions, those functions have been renamed using
   defines */
#ifdef PLATFORM_UNIX
#ifndef PAL_STDCPP_COMPAT
#define exit          PAL_exit
#define atexit        PAL_atexit
#define printf        PAL_printf
#define vprintf       PAL_vprintf
#define wprintf       PAL_wprintf
#define sprintf       PAL_sprintf
#define swprintf      PAL_swprintf
#define sscanf        PAL_sscanf
#define wcsspn        PAL_wcsspn
#define wcstod        PAL_wcstod
#define wcstol        PAL_wcstol
#define wcstoul       PAL_wcstoul
#define wcscat        PAL_wcscat
#define wcscpy        PAL_wcscpy
#define wcslen        PAL_wcslen
#define wcsncmp       PAL_wcsncmp
#define wcschr        PAL_wcschr
#define wcsrchr       PAL_wcsrchr
#define wcsstr        PAL_wcsstr
#define swscanf       PAL_swscanf
#define wcspbrk       PAL_wcspbrk
#define wcscmp        PAL_wcscmp
#define wcsncat       PAL_wcsncat
#define wcsncpy       PAL_wcsncpy
#define wcstok        PAL_wcstok
#define wcscspn       PAL_wcscspn
#define iswprint      PAL_iswprint
#define iswalpha      PAL_iswalpha
#define iswdigit      PAL_iswdigit
#define iswspace      PAL_iswspace
#define iswupper      PAL_iswupper
#define iswxdigit     PAL_iswxdigit
#define towlower      PAL_towlower
#define towupper      PAL_towupper
#define vsprintf      PAL_vsprintf
#define vswprintf     PAL_vswprintf
#define realloc       PAL_realloc
#define fopen         PAL_fopen
#define strtok        PAL_strtok
#define strtoul       PAL_strtoul
#define fprintf       PAL_fprintf
#define fwprintf      PAL_fwprintf
#define vfprintf      PAL_vfprintf
#define vfwprintf     PAL_vfwprintf
#define ctime         PAL_ctime
#define localtime     PAL_localtime
#define mktime        PAL_mktime
#define rand          PAL_rand
#define time          PAL_time
#define getenv        PAL_getenv
#define fgets         PAL_fgets
#define fgetws        PAL_fgetws
#define fputc         PAL_fputc
#define putchar       PAL_putchar
#define qsort         PAL_qsort
#define bsearch       PAL_bsearch
#define ferror        PAL_ferror
#define fread         PAL_fread
#define fwrite        PAL_fwrite
#define feof          PAL_feof
#define ftell         PAL_ftell
#define fclose        PAL_fclose
#define setbuf        PAL_setbuf
#define fflush        PAL_fflush
#define fputs         PAL_fputs
#define fseek         PAL_fseek
#define fgetpos       PAL_fgetpos
#define fsetpos       PAL_fsetpos
#define getc          PAL_getc
#define fgetc         PAL_getc // not a typo
#define ungetc        PAL_ungetc
#define setvbuf       PAL_setvbuf
#define atol          PAL_atol
#define labs          PAL_labs
#define acos          PAL_acos
#define asin          PAL_asin
#define atan2         PAL_atan2
#define exp           PAL_exp
#define log           PAL_log
#define log10         PAL_log10
#define pow           PAL_pow
#define malloc        PAL_malloc
#define free          PAL_free
#define mkstemp       PAL_mkstemp
#define rename        PAL_rename
#define _strdup       PAL__strdup
#define _getcwd       PAL__getcwd
#define _open         PAL__open
#define _close        PAL__close
#define _wcstoui64    PAL__wcstoui64
#define _flushall     PAL__flushall
#define _vsnprintf    PAL__vsnprintf

#ifdef _AMD64_ 
#define _mm_getcsr    PAL__mm_getcsr
#define _mm_setcsr    PAL__mm_setcsr
#endif // _AMD64_

#endif // !PAL_STDCPP_COMPAT
#endif // PLATFORM_UNIX

#ifndef _CONST_RETURN
#ifdef  __cplusplus
#define _CONST_RETURN  const
#define _CRT_CONST_CORRECT_OVERLOADS
#else
#define _CONST_RETURN
#endif
#endif

/* For backwards compatibility */
#define _WConst_return _CONST_RETURN

#define EOF     (-1)

typedef int errno_t;

#ifndef PAL_STDCPP_COMPAT

typedef struct {
    int quot;
    int rem;
} div_t;

PALIMPORT div_t div(int numer, int denom);

#if defined(_DEBUG)

/*++
Function:
PAL_memcpy

Overlapping buffer-safe version of memcpy.
See MSDN doc for memcpy
--*/
EXTERN_C
PALIMPORT
void *PAL_memcpy (void *dest, const void *src, size_t count);

PALIMPORT void * __cdecl memcpy(void *, const void *, size_t);

#define memcpy PAL_memcpy
#define IS_PAL_memcpy 1
#define TEST_PAL_DEFERRED(def) IS_##def
#define IS_REDEFINED_IN_PAL(def) TEST_PAL_DEFERRED(def)
#else //defined(_DEBUG)
PALIMPORT void * __cdecl memcpy(void *, const void *, size_t);
#endif //defined(_DEBUG)
PALIMPORT int    __cdecl memcmp(const void *, const void *, size_t);
PALIMPORT void * __cdecl memset(void *, int, size_t);
PALIMPORT void * __cdecl memmove(void *, const void *, size_t);
PALIMPORT void * __cdecl memchr(const void *, int, size_t);
PALIMPORT long long int __cdecl atoll(const char *);
PALIMPORT size_t __cdecl strlen(const char *);
PALIMPORT int __cdecl strcmp(const char*, const char *);
PALIMPORT int __cdecl strncmp(const char*, const char *, size_t);
PALIMPORT int __cdecl _strnicmp(const char *, const char *, size_t);
PALIMPORT char * __cdecl strcat(char *, const char *);
PALIMPORT char * __cdecl strncat(char *, const char *, size_t);
PALIMPORT char * __cdecl strcpy(char *, const char *);
PALIMPORT char * __cdecl strncpy(char *, const char *, size_t);
PALIMPORT char * __cdecl strchr(const char *, int);
PALIMPORT char * __cdecl strrchr(const char *, int);
PALIMPORT char * __cdecl strpbrk(const char *, const char *);
PALIMPORT char * __cdecl strstr(const char *, const char *);
PALIMPORT char * __cdecl strtok(char *, const char *);
PALIMPORT size_t __cdecl strspn(const char *, const char *);
PALIMPORT size_t  __cdecl strcspn(const char *, const char *);
PALIMPORT int __cdecl sprintf(char *, const char *, ...);
PALIMPORT int __cdecl vsprintf(char *, const char *, va_list);
PALIMPORT int __cdecl sscanf(const char *, const char *, ...);
PALIMPORT int __cdecl atoi(const char *);
PALIMPORT LONG __cdecl atol(const char *);
PALIMPORT ULONG __cdecl strtoul(const char *, char **, int);
PALIMPORT double __cdecl atof(const char *);
PALIMPORT double __cdecl strtod(const char *, char **);
PALIMPORT int __cdecl isprint(int);
PALIMPORT int __cdecl isspace(int);
PALIMPORT int __cdecl isalpha(int);
PALIMPORT int __cdecl isalnum(int);
PALIMPORT int __cdecl isdigit(int);
PALIMPORT int __cdecl isxdigit(int);
PALIMPORT int __cdecl isupper(int);
PALIMPORT int __cdecl islower(int);
PALIMPORT int __cdecl tolower(int);
PALIMPORT int __cdecl toupper(int);

#endif // PAL_STDCPP_COMPAT

PALIMPORT errno_t __cdecl memcpy_s(void *, size_t, const void *, size_t);
PALIMPORT errno_t __cdecl memmove_s(void *, size_t, const void *, size_t);
PALIMPORT char * __cdecl _strlwr(char *);
PALIMPORT int __cdecl _stricmp(const char *, const char *);
PALIMPORT int __cdecl _snprintf(char *, size_t, const char *, ...);
PALIMPORT char * __cdecl _gcvt_s(char *, int, double, int);
PALIMPORT char * __cdecl _ecvt(double, int, int *, int *);
PALIMPORT int __cdecl __iscsym(int);
PALIMPORT size_t __cdecl _mbslen(const unsigned char *);
PALIMPORT unsigned char * __cdecl _mbsinc(const unsigned char *);
PALIMPORT unsigned char * __cdecl _mbsninc(const unsigned char *, size_t);
PALIMPORT unsigned char * __cdecl _mbsdec(const unsigned char *, const unsigned char *);
PALIMPORT int __cdecl _wcsicmp(const WCHAR *, const WCHAR*);
PALIMPORT int __cdecl _wcsnicmp(const WCHAR *, const WCHAR *, size_t);
PALIMPORT int __cdecl _vsnprintf(char *, size_t, const char *, va_list);
PALIMPORT int __cdecl _vsnwprintf(WCHAR *, size_t, const WCHAR *, va_list);
PALIMPORT WCHAR * __cdecl _itow(int, WCHAR *, int);

PALIMPORT size_t __cdecl PAL_wcslen(const WCHAR *);
PALIMPORT int __cdecl PAL_wcscmp(const WCHAR*, const WCHAR*);
PALIMPORT int __cdecl PAL_wcsncmp(const WCHAR *, const WCHAR *, size_t);
PALIMPORT WCHAR * __cdecl PAL_wcscat(WCHAR *, const WCHAR *);
PALIMPORT WCHAR * __cdecl PAL_wcsncat(WCHAR *, const WCHAR *, size_t);
PALIMPORT WCHAR * __cdecl PAL_wcscpy(WCHAR *, const WCHAR *);
PALIMPORT WCHAR * __cdecl PAL_wcsncpy(WCHAR *, const WCHAR *, size_t);
PALIMPORT const WCHAR * __cdecl PAL_wcschr(const WCHAR *, WCHAR);
PALIMPORT const WCHAR * __cdecl PAL_wcsrchr(const WCHAR *, WCHAR);
PALIMPORT WCHAR _WConst_return * __cdecl PAL_wcspbrk(const WCHAR *, const WCHAR *);
PALIMPORT WCHAR _WConst_return * __cdecl PAL_wcsstr(const WCHAR *, const WCHAR *);
PALIMPORT WCHAR * __cdecl PAL_wcstok(WCHAR *, const WCHAR *);
PALIMPORT size_t __cdecl PAL_wcscspn(const WCHAR *, const WCHAR *);
PALIMPORT int __cdecl PAL_swprintf(WCHAR *, const WCHAR *, ...);
PALIMPORT int __cdecl PAL_vswprintf(WCHAR *, const WCHAR *, va_list);
PALIMPORT int __cdecl PAL__vsnprintf(LPSTR Buffer, size_t Count, LPCSTR Format, va_list ap);
PALIMPORT int __cdecl _snwprintf(WCHAR *, size_t, const WCHAR *, ...);
PALIMPORT int __cdecl PAL_swscanf(const WCHAR *, const WCHAR *, ...);
PALIMPORT LONG __cdecl PAL_wcstol(const WCHAR *, WCHAR **, int);
PALIMPORT ULONG __cdecl PAL_wcstoul(const WCHAR *, WCHAR **, int);
PALIMPORT size_t __cdecl PAL_wcsspn (const WCHAR *, const WCHAR *);
PALIMPORT double __cdecl PAL_wcstod(const WCHAR *, WCHAR **);
PALIMPORT int __cdecl PAL_iswalpha(WCHAR);
PALIMPORT int __cdecl PAL_iswprint(WCHAR);
PALIMPORT int __cdecl PAL_iswupper(WCHAR);
PALIMPORT int __cdecl PAL_iswspace(WCHAR);
PALIMPORT int __cdecl PAL_iswdigit(WCHAR);
PALIMPORT int __cdecl PAL_iswxdigit(WCHAR);
PALIMPORT WCHAR __cdecl PAL_towlower(WCHAR);
PALIMPORT WCHAR __cdecl PAL_towupper(WCHAR);

PALIMPORT WCHAR * __cdecl _wcslwr(WCHAR *);
PALIMPORT ULONGLONG _wcstoui64(const WCHAR *, WCHAR **, int);
PALIMPORT WCHAR * __cdecl _i64tow(__int64, WCHAR *, int);
PALIMPORT WCHAR * __cdecl _ui64tow(unsigned __int64, WCHAR *, int);
PALIMPORT int __cdecl _wtoi(const WCHAR *);

#ifdef __cplusplus
extern "C++" {
inline WCHAR *PAL_wcschr(WCHAR *_S, WCHAR _C)
        {return ((WCHAR *)PAL_wcschr((const WCHAR *)_S, _C)); }
inline WCHAR *PAL_wcsrchr(WCHAR *_S, WCHAR _C)
        {return ((WCHAR *)PAL_wcsrchr((const WCHAR *)_S, _C)); }
inline WCHAR *PAL_wcspbrk(WCHAR *_S, const WCHAR *_P)
        {return ((WCHAR *)PAL_wcspbrk((const WCHAR *)_S, _P)); }
inline WCHAR *PAL_wcsstr(WCHAR *_S, const WCHAR *_P)
        {return ((WCHAR *)PAL_wcsstr((const WCHAR *)_S, _P)); }
}
#endif

/*++
Function:
_rotl

See MSDN doc.
--*/
EXTERN_C
PALIMPORT
inline
unsigned int __cdecl _rotl(unsigned int value, int shift)
{
    unsigned int retval = 0;

    shift &= 0x1f;
    retval = (value << shift) | (value >> (sizeof(int) * CHAR_BIT - shift));
    return retval;
}

// On 64 bit unix, make the long an int.
#ifdef BIT64
#define _lrotl _rotl
#endif // BIT64

/*++
Function:
_rotr

See MSDN doc.
--*/
EXTERN_C
PALIMPORT
inline
unsigned int __cdecl _rotr(unsigned int value, int shift)
{
    unsigned int retval;

    shift &= 0x1f;
    retval = (value >> shift) | (value << (sizeof(int) * CHAR_BIT - shift));
    return retval;
}

PALIMPORT int __cdecl abs(int);
#ifndef PAL_STDCPP_COMPAT
PALIMPORT LONG __cdecl labs(LONG);
#endif // !PAL_STDCPP_COMPAT
// clang complains if this is declared with __int64
PALIMPORT long long __cdecl llabs(long long);

PALIMPORT int __cdecl _finite(double);
PALIMPORT int __cdecl _isnan(double);
PALIMPORT double __cdecl _copysign(double, double);
PALIMPORT double __cdecl acos(double);
PALIMPORT double __cdecl asin(double);
PALIMPORT double __cdecl atan(double);
PALIMPORT double __cdecl atan2(double, double);
PALIMPORT double __cdecl ceil(double);
PALIMPORT double __cdecl cos(double);
PALIMPORT double __cdecl cosh(double);
PALIMPORT double __cdecl exp(double);
PALIMPORT double __cdecl fabs(double);
PALIMPORT double __cdecl floor(double);
PALIMPORT double __cdecl fmod(double, double); 
PALIMPORT double __cdecl log(double);
PALIMPORT double __cdecl log10(double);
PALIMPORT double __cdecl modf(double, double*);
PALIMPORT double __cdecl pow(double, double);
PALIMPORT double __cdecl sin(double);
PALIMPORT double __cdecl sinh(double);
PALIMPORT double __cdecl sqrt(double);
PALIMPORT double __cdecl tan(double);
PALIMPORT double __cdecl tanh(double);

PALIMPORT float __cdecl fabsf(float);
PALIMPORT float __cdecl fmodf(float, float); 
PALIMPORT float __cdecl modff(float, float*);

#ifndef PAL_STDCPP_COMPAT

#ifdef __cplusplus
extern "C++" {

inline __int64 abs(__int64 _X) {
    return llabs(_X);
}

}
#endif

PALIMPORT void * __cdecl malloc(size_t);
PALIMPORT void   __cdecl free(void *);
PALIMPORT void * __cdecl realloc(void *, size_t);
PALIMPORT char * __cdecl _strdup(const char *);

#if defined(_MSC_VER)
#define alloca _alloca
#elif defined(PLATFORM_UNIX)
#define _alloca alloca
#else
// MingW
#define _alloca __builtin_alloca
#define alloca __builtin_alloca
#endif //_MSC_VER

#if defined(__GNUC__) && defined(PLATFORM_UNIX)
#define alloca  __builtin_alloca
#endif // __GNUC__

#define max(a, b) (((a) > (b)) ? (a) : (b))
#define min(a, b) (((a) < (b)) ? (a) : (b))

#endif // !PAL_STDCPP_COMPAT

PALIMPORT PAL_NORETURN void __cdecl exit(int);
int __cdecl atexit(void (__cdecl *function)(void));

PALIMPORT void __cdecl qsort(void *, size_t, size_t, int (__cdecl *)(const void *, const void *));
PALIMPORT void * __cdecl bsearch(const void *, const void *, size_t, size_t,
int (__cdecl *)(const void *, const void *));

PALIMPORT void __cdecl _splitpath(const char *, char *, char *, char *, char *);
PALIMPORT void __cdecl _wsplitpath(const WCHAR *, WCHAR *, WCHAR *, WCHAR *, WCHAR *);
PALIMPORT void __cdecl _makepath(char *, const char *, const char *, const char *, const char *);
PALIMPORT void __cdecl _wmakepath(WCHAR *, const WCHAR *, const WCHAR *, const WCHAR *, const WCHAR *);
PALIMPORT char * __cdecl _fullpath(char *, const char *, size_t);

PALIMPORT void __cdecl _swab(char *, char *, int);

#ifndef PAL_STDCPP_COMPAT
PALIMPORT time_t __cdecl time(time_t *);

struct tm {
        int tm_sec;     /* seconds after the minute - [0,59] */
        int tm_min;     /* minutes after the hour - [0,59] */
        int tm_hour;    /* hours since midnight - [0,23] */
        int tm_mday;    /* day of the month - [1,31] */
        int tm_mon;     /* months since January - [0,11] */
        int tm_year;    /* years since 1900 */
        int tm_wday;    /* days since Sunday - [0,6] */
        int tm_yday;    /* days since January 1 - [0,365] */
        int tm_isdst;   /* daylight savings time flag */
        };

PALIMPORT struct tm * __cdecl localtime(const time_t *);
PALIMPORT time_t __cdecl mktime(struct tm *);
PALIMPORT char * __cdecl ctime(const time_t *);
#endif // !PAL_STDCPP_COMPAT

PALIMPORT int __cdecl _open_osfhandle(INT_PTR, int);
PALIMPORT int __cdecl _close(int);
PALIMPORT int __cdecl _flushall();

#ifdef PAL_STDCPP_COMPAT

struct _PAL_FILE;
typedef struct _PAL_FILE PAL_FILE;

#else // PAL_STDCPP_COMPAT

struct _FILE;
typedef struct _FILE FILE;
typedef struct _FILE PAL_FILE;

#define SEEK_SET    0
#define SEEK_CUR    1
#define SEEK_END    2

/* Locale categories */
#define LC_ALL          0
#define LC_COLLATE      1
#define LC_CTYPE        2
#define LC_MONETARY     3
#define LC_NUMERIC      4
#define LC_TIME         5

#define _IOFBF  0       /* setvbuf should set fully buffered */
#define _IOLBF  1       /* setvbuf should set line buffered */
#define _IONBF  2       /* setvbuf should set unbuffered */

#endif // PAL_STDCPP_COMPAT

PALIMPORT int __cdecl PAL_fclose(PAL_FILE *);
PALIMPORT void __cdecl PAL_setbuf(PAL_FILE *, char*);
PALIMPORT int __cdecl PAL_fflush(PAL_FILE *);
PALIMPORT size_t __cdecl PAL_fwrite(const void *, size_t, size_t, PAL_FILE *);
PALIMPORT size_t __cdecl PAL_fread(void *, size_t, size_t, PAL_FILE *);
PALIMPORT char * __cdecl PAL_fgets(char *, int, PAL_FILE *);
PALIMPORT int __cdecl PAL_fputs(const char *, PAL_FILE *);
PALIMPORT int __cdecl PAL_fputc(int c, PAL_FILE *stream);
PALIMPORT int __cdecl PAL_putchar(int c);
PALIMPORT int __cdecl PAL_fprintf(PAL_FILE *, const char *, ...);
PALIMPORT int __cdecl PAL_vfprintf(PAL_FILE *, const char *, va_list);
PALIMPORT int __cdecl PAL_fseek(PAL_FILE *, LONG, int);
PALIMPORT LONG __cdecl PAL_ftell(PAL_FILE *);
PALIMPORT int __cdecl PAL_feof(PAL_FILE *);
PALIMPORT int __cdecl PAL_ferror(PAL_FILE *);
PALIMPORT PAL_FILE * __cdecl PAL_fopen(const char *, const char *);
PALIMPORT int __cdecl PAL_getc(PAL_FILE *stream);
PALIMPORT int __cdecl PAL_fgetc(PAL_FILE *stream);
PALIMPORT int __cdecl PAL_ungetc(int c, PAL_FILE *stream);
PALIMPORT int __cdecl PAL_setvbuf(PAL_FILE *stream, char *, int, size_t);
PALIMPORT WCHAR * __cdecl PAL_fgetws(WCHAR *, int, PAL_FILE *);
PALIMPORT int __cdecl PAL_fwprintf(PAL_FILE *, const WCHAR *, ...);
PALIMPORT int __cdecl PAL_vfwprintf(PAL_FILE *, const WCHAR *, va_list);
PALIMPORT int __cdecl PAL_wprintf(const WCHAR*, ...);

PALIMPORT int __cdecl _getw(PAL_FILE *);
PALIMPORT int __cdecl _putw(int, PAL_FILE *);
PALIMPORT PAL_FILE * __cdecl _fdopen(int, const char *);
PALIMPORT PAL_FILE * __cdecl _wfopen(const WCHAR *, const WCHAR *);
PALIMPORT PAL_FILE * __cdecl _wfsopen(const WCHAR *, const WCHAR *, int);

/* Maximum value that can be returned by the rand function. */

#ifndef PAL_STDCPP_COMPAT
#define RAND_MAX 0x7fff
#endif // !PAL_STDCPP_COMPAT

PALIMPORT int __cdecl rand(void);
PALIMPORT void __cdecl srand(unsigned int);

PALIMPORT int __cdecl printf(const char *, ...);
PALIMPORT int __cdecl vprintf(const char *, va_list);

#ifdef _MSC_VER
#define PAL_get_caller _MSC_VER
#else
#define PAL_get_caller 0
#endif

PALIMPORT PAL_FILE * __cdecl PAL_get_stdout(int caller);
PALIMPORT PAL_FILE * __cdecl PAL_get_stdin(int caller);
PALIMPORT PAL_FILE * __cdecl PAL_get_stderr(int caller);
PALIMPORT int * __cdecl PAL_errno(int caller);

#ifdef PAL_STDCPP_COMPAT
#define PAL_stdout (PAL_get_stdout(PAL_get_caller))
#define PAL_stdin  (PAL_get_stdin(PAL_get_caller))
#define PAL_stderr (PAL_get_stderr(PAL_get_caller))
#define PAL_errno   (*PAL_errno(PAL_get_caller))
#else // PAL_STDCPP_COMPAT
#define stdout (PAL_get_stdout(PAL_get_caller))
#define stdin  (PAL_get_stdin(PAL_get_caller))
#define stderr (PAL_get_stderr(PAL_get_caller))
#define errno  (*PAL_errno(PAL_get_caller))
#endif // PAL_STDCPP_COMPAT

PALIMPORT char * __cdecl getenv(const char *);
PALIMPORT int __cdecl _putenv(const char *);

#define ERANGE          34

/******************* PAL-specific I/O completion port *****************/

typedef struct _PAL_IOCP_CPU_INFORMATION {
    union {
        FILETIME ftLastRecordedIdleTime;
        FILETIME ftLastRecordedCurrentTime;
    } LastRecordedTime;
    FILETIME ftLastRecordedKernelTime;
    FILETIME ftLastRecordedUserTime;
} PAL_IOCP_CPU_INFORMATION;

PALIMPORT
INT
PALAPI
PAL_GetCPUBusyTime(
    IN OUT PAL_IOCP_CPU_INFORMATION *lpPrevCPUInfo);

/****************PAL Perf functions for PInvoke*********************/
#if PAL_PERF
PALIMPORT
VOID
PALAPI
PAL_EnableProcessProfile(VOID);

PALIMPORT
VOID
PALAPI
PAL_DisableProcessProfile(VOID);

PALIMPORT
BOOL
PALAPI
PAL_IsProcessProfileEnabled(VOID);

PALIMPORT
INT64
PALAPI
PAL_GetCpuTickCount(VOID);
#endif // PAL_PERF

/******************* PAL functions for SIMD extensions *****************/

PALIMPORT
unsigned int _mm_getcsr(void);

PALIMPORT
void _mm_setcsr(unsigned int i);

/******************* PAL side-by-side support  ************************/

#ifdef FEATURE_PAL_SXS
//
// Some versions of the PAL support several PALs side-by-side
// in the process.  To avoid those PALs interfering with one
// another, they need to be told by clients when they are active
// and when they are not.
//

// To avoid performance problems incurred by swapping thread
// exception ports every time we leave the PAL, there's also
// the concept of entering/leaving the PAL at its top boundary
// (entering down/leaving up) or at the bottom boundary
// (leaving down/entering up).

typedef enum _PAL_Boundary {
    PAL_BoundaryTop,            // closer to main()
    PAL_BoundaryBottom,         // closer to execution
    PAL_BoundaryEH,             // out-of-band during EH
    
    PAL_BoundaryMax = PAL_BoundaryEH
} PAL_Boundary;

// This function needs to be called on a thread when it enters
// a region of code that depends on this instance of the PAL
// in the process, and the current thread may or may not be
// known to the PAL.  This function can fail (for something else
// than an internal error) if this is the first time that the
// current thread entered this PAL.  Note that PAL_Initialize
// implies a call to this function.  Does not modify LastError.
PALIMPORT
DWORD
PALAPI
PAL_Enter(PAL_Boundary boundary);

// Returns TRUE if we this thread has already entered the PAL,
// returns FALSE if we have not entered the PAL.
PALIMPORT
BOOL
PALAPI
PAL_HasEntered();

// Equivalent to PAL_Enter(PAL_BoundaryTop) and is for stub
// code generation use.
PALIMPORT
DWORD
PALAPI
PAL_EnterTop();

// This function needs to be called on a thread when it enters
// a region of code that depends on this instance of the PAL
// in the process, and the current thread is already known to
// the PAL.  Does not modify LastError.
PALIMPORT
VOID
PALAPI
PAL_Reenter(PAL_Boundary boundary);

// This function needs to be called on a thread when it enters
// a region of code that depends on this instance of the PAL
// in the process, and it is unknown whether the current thread
// is already running in the PAL.  Returns TRUE if and only if
// the thread was not running in the PAL previously.  Does not
// modify LastError.
PALIMPORT
BOOL
PALAPI
PAL_ReenterForEH(VOID);

// This function needs to be called on a thread when it leaves
// a region of code that depends on this instance of the PAL
// in the process.  Does not modify LastError.
PALIMPORT
VOID
PALAPI
PAL_Leave(PAL_Boundary boundary);

// This function is equivalent to PAL_Leave(PAL_BoundaryBottom)
// and is available to limit the creation of stub code.
PALIMPORT
VOID
PALAPI
PAL_LeaveBottom();

// This function is equivalent to PAL_Leave(PAL_BoundaryTop)
// and is available to limit the creation of stub code.
PALIMPORT
VOID
PALAPI
PAL_LeaveTop();

#ifdef  __cplusplus
//
// A holder to enter the PAL for a specific region of code.
// Previously, we must have been executing outside the PAL
// (unless fEnter is set to FALSE).
//
class PAL_EnterHolder
{
private:
    BOOL m_fEntered;
    DWORD m_palError;
public:
    PAL_EnterHolder(BOOL fEnter = TRUE) : m_palError(ERROR_SUCCESS)
    {
        if (fEnter)
        {
            m_palError = PAL_Enter(PAL_BoundaryTop);
            m_fEntered = m_palError == ERROR_SUCCESS;
        }
        else
        {
            m_fEntered = FALSE;
        }
    }

    ~PAL_EnterHolder()
    {
        if (m_fEntered)
        {
            PAL_Leave(PAL_BoundaryTop);
        }
    }

    DWORD GetError()
    {
        return m_palError;
    }
    
    void SuppressRelease()
    {
        // Used to avoid calling PAL_Leave() when 
        // another code path will explicitly do so.
        m_fEntered = FALSE;
    }
};

class PAL_LeaveHolder
{
public:
    PAL_LeaveHolder()
    {
        PAL_Leave(PAL_BoundaryBottom);
    }

    ~PAL_LeaveHolder()
    {
        PAL_Reenter(PAL_BoundaryBottom);
    }
};
#endif // __cplusplus

#else // FEATURE_PAL_SXS

#define PAL_Enter(boundary) ERROR_SUCCESS
#define PAL_Reenter(boundary)
#define PAL_Leave(boundary)

#ifdef __cplusplus
class PAL_EnterHolder {
public:
    // using constructor to suppress the "unused variable" warnings
    PAL_EnterHolder() {}
};
class PAL_LeaveHolder {
public:
    // using constructor to suppress the "unused variable" warnings
    PAL_LeaveHolder() {}
};
#endif // __cplusplus

#endif // FEATURE_PAL_SXS

#ifdef __cplusplus

#include "pal_unwind.h"

PALIMPORT
VOID
PALAPI
PAL_FreeExceptionRecords(
  IN EXCEPTION_RECORD *exceptionRecord, 
  IN CONTEXT *contextRecord);

#define EXCEPTION_CONTINUE_SEARCH   0
#define EXCEPTION_EXECUTE_HANDLER   1
#define EXCEPTION_CONTINUE_EXECUTION -1

struct PAL_SEHException
{
private:
    static const SIZE_T NoTargetFrameSp = SIZE_MAX;

    void Move(PAL_SEHException& ex)
    {
        ExceptionPointers.ExceptionRecord = ex.ExceptionPointers.ExceptionRecord;
        ExceptionPointers.ContextRecord = ex.ExceptionPointers.ContextRecord;
        TargetFrameSp = ex.TargetFrameSp;

        ex.Clear();
    }

    void FreeRecords()
    {
        if (ExceptionPointers.ExceptionRecord != NULL)
        {
            PAL_FreeExceptionRecords(ExceptionPointers.ExceptionRecord, ExceptionPointers.ContextRecord);
            ExceptionPointers.ExceptionRecord = NULL;
            ExceptionPointers.ContextRecord = NULL;
        }
    }

public:
    EXCEPTION_POINTERS ExceptionPointers;
    // Target frame stack pointer set before the 2nd pass.
    SIZE_T TargetFrameSp;

    PAL_SEHException(EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pContextRecord)
    {
        ExceptionPointers.ExceptionRecord = pExceptionRecord;
        ExceptionPointers.ContextRecord = pContextRecord;
        TargetFrameSp = NoTargetFrameSp;
    }

    PAL_SEHException()
    {
        Clear();
    }    

    // The copy constructor and copy assignment operators are deleted so that the PAL_SEHException
    // can never be copied, only moved. This enables simple lifetime management of the exception and
    // context records, since there is always just one PAL_SEHException instance referring to the same records.
    PAL_SEHException(const PAL_SEHException& ex) = delete;
    PAL_SEHException& operator=(const PAL_SEHException& ex) = delete;

    PAL_SEHException(PAL_SEHException&& ex)
    {
        Move(ex);
    }    

    PAL_SEHException& operator=(PAL_SEHException&& ex)
    {
        FreeRecords();
        Move(ex);
        return *this;
    }

    ~PAL_SEHException()
    {
        FreeRecords();
    }

    void Clear()
    {
        ExceptionPointers.ExceptionRecord = NULL;
        ExceptionPointers.ContextRecord = NULL;
        TargetFrameSp = NoTargetFrameSp;
    }

    CONTEXT* GetContextRecord()
    {
        return ExceptionPointers.ContextRecord;
    }

    EXCEPTION_RECORD* GetExceptionRecord()
    {
        return ExceptionPointers.ExceptionRecord;
    }

    bool IsFirstPass()
    {
        return (TargetFrameSp == NoTargetFrameSp);
    }

    void SecondPassDone()
    {
        TargetFrameSp = NoTargetFrameSp;
    }
};

typedef BOOL (PALAPI *PHARDWARE_EXCEPTION_HANDLER)(PAL_SEHException* ex);
typedef BOOL (PALAPI *PHARDWARE_EXCEPTION_SAFETY_CHECK_FUNCTION)(PCONTEXT contextRecord, PEXCEPTION_RECORD exceptionRecord);
typedef VOID (PALAPI *PTERMINATION_REQUEST_HANDLER)();
typedef DWORD (PALAPI *PGET_GCMARKER_EXCEPTION_CODE)(LPVOID ip);

PALIMPORT
VOID
PALAPI
PAL_SetHardwareExceptionHandler(
    IN PHARDWARE_EXCEPTION_HANDLER exceptionHandler,
    IN PHARDWARE_EXCEPTION_SAFETY_CHECK_FUNCTION exceptionCheckFunction);

PALIMPORT
VOID
PALAPI
PAL_SetGetGcMarkerExceptionCode(
    IN PGET_GCMARKER_EXCEPTION_CODE getGcMarkerExceptionCode);

PALIMPORT
VOID
PALAPI
PAL_ThrowExceptionFromContext(
    IN CONTEXT* context,
    IN PAL_SEHException* ex);

PALIMPORT
VOID
PALAPI
PAL_SetTerminationRequestHandler(
    IN PTERMINATION_REQUEST_HANDLER terminationRequestHandler);

//
// This holder is used to indicate that a hardware
// exception should be raised as a C++ exception
// to better emulate SEH on the xplat platforms.
//
class CatchHardwareExceptionHolder
{
public:
    CatchHardwareExceptionHolder();

    ~CatchHardwareExceptionHolder();

    static bool IsEnabled();
};

//
// NOTE: Catching hardware exceptions are only enabled in the DAC and SOS 
// builds. A hardware exception in coreclr code will fail fast/terminate
// the process.
//
#ifdef FEATURE_ENABLE_HARDWARE_EXCEPTIONS
#define HardwareExceptionHolder CatchHardwareExceptionHolder __catchHardwareException;
#else
#define HardwareExceptionHolder
#endif // FEATURE_ENABLE_HARDWARE_EXCEPTIONS

#ifdef FEATURE_PAL_SXS

extern "C++" {

//
// This is the base class of native exception holder used to provide 
// the filter function to the exception dispatcher. This allows the
// filter to be called during the first pass to better emulate SEH
// the xplat platforms that only have C++ exception support.
//
class NativeExceptionHolderBase
{
    // Save the address of the holder head so the destructor 
    // doesn't have access the slow (on Linux) TLS value again.
    NativeExceptionHolderBase **m_head;

    // The next holder on the stack
    NativeExceptionHolderBase *m_next;

protected:
    NativeExceptionHolderBase();

    ~NativeExceptionHolderBase();

public:
    // Calls the holder's filter handler.
    virtual EXCEPTION_DISPOSITION InvokeFilter(PAL_SEHException& ex) = 0;

    // Adds the holder to the "stack" of holders. This is done explicitly instead
    // of in the constructor was to avoid the mess of move constructors combined
    // with return value optimization (in CreateHolder).
    void Push();

    // Given the currentHolder and locals stack range find the next holder starting with this one
    // To find the first holder, pass nullptr as the currentHolder.
    static NativeExceptionHolderBase *FindNextHolder(NativeExceptionHolderBase *currentHolder, void *frameLowAddress, void *frameHighAddress);
};

//
// This is the second part of the native exception filter holder. It is
// templated because the lambda used to wrap the exception filter is a
// unknown type. 
//
template<class FilterType>
class NativeExceptionHolder : public NativeExceptionHolderBase
{
    FilterType* m_exceptionFilter;

public:
    NativeExceptionHolder(FilterType* exceptionFilter)
        : NativeExceptionHolderBase()
    {
        m_exceptionFilter = exceptionFilter;
    }

    virtual EXCEPTION_DISPOSITION InvokeFilter(PAL_SEHException& ex)
    {
        return (*m_exceptionFilter)(ex);
    }
};

//
// This is a native exception holder that is used when the catch catches
// all exceptions.
//
class NativeExceptionHolderCatchAll : public NativeExceptionHolderBase
{

public:
    NativeExceptionHolderCatchAll()
        : NativeExceptionHolderBase()
    {
    }

    virtual EXCEPTION_DISPOSITION InvokeFilter(PAL_SEHException& ex)
    {
        return EXCEPTION_EXECUTE_HANDLER;
    }
};

// This is a native exception holder that doesn't catch any exceptions.
class NativeExceptionHolderNoCatch : public NativeExceptionHolderBase
{

public:
    NativeExceptionHolderNoCatch()
        : NativeExceptionHolderBase()
    {
    }

    virtual EXCEPTION_DISPOSITION InvokeFilter(PAL_SEHException& ex)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }
};

//
// This factory class for the native exception holder is necessary because
// templated functions don't need the explicit type parameter and can infer
// the template type from the parameter.
//
class NativeExceptionHolderFactory
{
public:
    template<class FilterType>
    static NativeExceptionHolder<FilterType> CreateHolder(FilterType* exceptionFilter)
    {
        return NativeExceptionHolder<FilterType>(exceptionFilter);
    }
};

// Start of a try block for exceptions raised by RaiseException
#define PAL_TRY(__ParamType, __paramDef, __paramRef)                            \
{                                                                               \
    __ParamType __param = __paramRef;                                           \
    auto tryBlock = [](__ParamType __paramDef)                                  \
    {

// Start of an exception handler. If an exception raised by the RaiseException 
// occurs in the try block and the disposition is EXCEPTION_EXECUTE_HANDLER, 
// the handler code is executed. If the disposition is EXCEPTION_CONTINUE_SEARCH,
// the exception is rethrown. The EXCEPTION_CONTINUE_EXECUTION disposition is
// not supported.
#define PAL_EXCEPT(dispositionExpression)                                       \
    };                                                                          \
    const bool isFinally = false;                                               \
    auto finallyBlock = []() {};                                                \
    EXCEPTION_DISPOSITION disposition = EXCEPTION_CONTINUE_EXECUTION;           \
    auto exceptionFilter = [&disposition, &__param](PAL_SEHException& ex)       \
    {                                                                           \
        disposition = dispositionExpression;                                    \
        _ASSERTE(disposition != EXCEPTION_CONTINUE_EXECUTION);                  \
        return disposition;                                                     \
    };                                                                          \
    try                                                                         \
    {                                                                           \
        HardwareExceptionHolder                                                 \
        auto __exceptionHolder = NativeExceptionHolderFactory::CreateHolder(&exceptionFilter); \
        __exceptionHolder.Push();                                               \
        tryBlock(__param);                                                      \
    }                                                                           \
    catch (PAL_SEHException& ex)                                                \
    {                                                                           \
        if (disposition == EXCEPTION_CONTINUE_EXECUTION)                        \
        {                                                                       \
            exceptionFilter(ex);                                                \
        }                                                                       \
        if (disposition == EXCEPTION_CONTINUE_SEARCH)                           \
        {                                                                       \
            throw;                                                              \
        }                                                                       \
        ex.SecondPassDone();

// Start of an exception handler. It works the same way as the PAL_EXCEPT except
// that the disposition is obtained by calling the specified filter.
#define PAL_EXCEPT_FILTER(filter) PAL_EXCEPT(filter(&ex.ExceptionPointers, __param))

// Start of a finally block. The finally block is executed both when the try block
// finishes or when an exception is raised using the RaiseException in it.
#define PAL_FINALLY                     \
    };                                  \
    const bool isFinally = true;        \
    auto finallyBlock = [&]()           \
    {                       

// End of an except or a finally block.
#define PAL_ENDTRY                      \
    };                                  \
    if (isFinally)                      \
    {                                   \
        try                             \
        {                               \
            tryBlock(__param);          \
        }                               \
        catch (...)                     \
        {                               \
            finallyBlock();             \
            throw;                      \
        }                               \
        finallyBlock();                 \
    }                                   \
}

} // extern "C++"

#endif // FEATURE_PAL_SXS

#define PAL_CPP_THROW(type, obj) { throw obj; }
#define PAL_CPP_RETHROW { throw; }
#define PAL_CPP_TRY                     try { HardwareExceptionHolder
#define PAL_CPP_CATCH_EXCEPTION(ident)  } catch (Exception *ident) { PAL_Reenter(PAL_BoundaryBottom);
#define PAL_CPP_CATCH_EXCEPTION_NOARG   } catch (Exception *) { PAL_Reenter(PAL_BoundaryBottom);
#define PAL_CPP_CATCH_DERIVED(type, ident) } catch (type *ident) { PAL_Reenter(PAL_BoundaryBottom);
#define PAL_CPP_CATCH_ALL               } catch (...) {                                           \
                                            PAL_Reenter(PAL_BoundaryBottom);                      \
                                            try { throw; }                                        \
                                            catch (PAL_SEHException& ex) { ex.SecondPassDone(); } \
                                            catch (...) {}

#define PAL_CPP_ENDTRY                  }

#ifdef _MSC_VER
#pragma warning(disable:4611) // interaction between '_setjmp' and C++ object destruction is non-portable
#endif

#ifdef FEATURE_PAL_SXS

#define PAL_TRY_FOR_DLLMAIN(ParamType, paramDef, paramRef, _reason) PAL_TRY(ParamType, paramDef, paramRef)

#else // FEATURE_PAL_SXS

#define PAL_TRY(ParamType, paramDef, paramRef)                          \
    {                                                                   \
        ParamType __param = paramRef;                                   \
        ParamType paramDef; paramDef = __param;                         \
        try {                                                           \
            HardwareExceptionHolder

#define PAL_TRY_FOR_DLLMAIN(ParamType, paramDef, paramRef, _reason)     \
    {                                                                   \
        ParamType __param = paramRef;                                   \
        ParamType paramDef; paramDef = __param;                         \
        try {                                                           \
            HardwareExceptionHolder

#define PAL_ENDTRY                                                      \
        }                                                               \
    }

#endif // FEATURE_PAL_SXS

#endif // __cplusplus

// Platform-specific library naming
// 
#ifdef PLATFORM_UNIX
#ifdef __APPLE__
#define MAKEDLLNAME_W(name) u"lib" name u".dylib"
#define MAKEDLLNAME_A(name)  "lib" name  ".dylib"
#elif defined(_AIX)
#define MAKEDLLNAME_W(name) L"lib" name L".a"
#define MAKEDLLNAME_A(name)  "lib" name  ".a"
#elif defined(__hppa__) || defined(_IA64_)
#define MAKEDLLNAME_W(name) L"lib" name L".sl"
#define MAKEDLLNAME_A(name)  "lib" name  ".sl"
#else
#define MAKEDLLNAME_W(name) u"lib" name u".so"
#define MAKEDLLNAME_A(name)  "lib" name  ".so"
#endif
#else
#define MAKEDLLNAME_W(name) name L".dll"
#define MAKEDLLNAME_A(name) name  ".dll"
#endif

#ifdef UNICODE
#define MAKEDLLNAME(x) MAKEDLLNAME_W(x)
#else
#define MAKEDLLNAME(x) MAKEDLLNAME_A(x)
#endif

#define PAL_SHLIB_PREFIX "lib"

#if __APPLE__
#define PAL_SHLIB_SUFFIX ".dylib"
#elif _AIX
#define PAL_SHLIB_SUFFIX ".a"
#elif _HPUX_
#define PAL_SHLIB_SUFFIX ".sl"
#else
#define PAL_SHLIB_SUFFIX ".so"
#endif

#define DBG_EXCEPTION_HANDLED            ((DWORD   )0x00010001L)    
#define DBG_CONTINUE                     ((DWORD   )0x00010002L)    
#define DBG_EXCEPTION_NOT_HANDLED        ((DWORD   )0x80010001L)    

#define DBG_TERMINATE_THREAD             ((DWORD   )0x40010003L)    
#define DBG_TERMINATE_PROCESS            ((DWORD   )0x40010004L)    
#define DBG_CONTROL_C                    ((DWORD   )0x40010005L)    
#define DBG_RIPEXCEPTION                 ((DWORD   )0x40010007L)    
#define DBG_CONTROL_BREAK                ((DWORD   )0x40010008L)    
#define DBG_COMMAND_EXCEPTION            ((DWORD   )0x40010009L)    

#define STATUS_USER_APC                  ((DWORD   )0x000000C0L)
#define STATUS_GUARD_PAGE_VIOLATION      ((DWORD   )0x80000001L)
#define STATUS_DATATYPE_MISALIGNMENT     ((DWORD   )0x80000002L)
#define STATUS_BREAKPOINT                ((DWORD   )0x80000003L)
#define STATUS_SINGLE_STEP               ((DWORD   )0x80000004L)
#define STATUS_LONGJUMP                  ((DWORD   )0x80000026L)
#define STATUS_UNWIND_CONSOLIDATE        ((DWORD   )0x80000029L)
#define STATUS_ACCESS_VIOLATION          ((DWORD   )0xC0000005L)
#define STATUS_IN_PAGE_ERROR             ((DWORD   )0xC0000006L)
#define STATUS_INVALID_HANDLE            ((DWORD   )0xC0000008L)
#define STATUS_NO_MEMORY                 ((DWORD   )0xC0000017L)
#define STATUS_ILLEGAL_INSTRUCTION       ((DWORD   )0xC000001DL)
#define STATUS_NONCONTINUABLE_EXCEPTION  ((DWORD   )0xC0000025L)
#define STATUS_INVALID_DISPOSITION       ((DWORD   )0xC0000026L)
#define STATUS_ARRAY_BOUNDS_EXCEEDED     ((DWORD   )0xC000008CL)
#define STATUS_FLOAT_DENORMAL_OPERAND    ((DWORD   )0xC000008DL)
#define STATUS_FLOAT_DIVIDE_BY_ZERO      ((DWORD   )0xC000008EL)
#define STATUS_FLOAT_INEXACT_RESULT      ((DWORD   )0xC000008FL)
#define STATUS_FLOAT_INVALID_OPERATION   ((DWORD   )0xC0000090L)
#define STATUS_FLOAT_OVERFLOW            ((DWORD   )0xC0000091L)
#define STATUS_FLOAT_STACK_CHECK         ((DWORD   )0xC0000092L)
#define STATUS_FLOAT_UNDERFLOW           ((DWORD   )0xC0000093L)
#define STATUS_INTEGER_DIVIDE_BY_ZERO    ((DWORD   )0xC0000094L)
#define STATUS_INTEGER_OVERFLOW          ((DWORD   )0xC0000095L)
#define STATUS_PRIVILEGED_INSTRUCTION    ((DWORD   )0xC0000096L)
#define STATUS_STACK_OVERFLOW            ((DWORD   )0xC00000FDL)
#define STATUS_CONTROL_C_EXIT            ((DWORD   )0xC000013AL)

#define WAIT_IO_COMPLETION                  STATUS_USER_APC

#define EXCEPTION_ACCESS_VIOLATION          STATUS_ACCESS_VIOLATION
#define EXCEPTION_DATATYPE_MISALIGNMENT     STATUS_DATATYPE_MISALIGNMENT
#define EXCEPTION_BREAKPOINT                STATUS_BREAKPOINT
#define EXCEPTION_SINGLE_STEP               STATUS_SINGLE_STEP
#define EXCEPTION_ARRAY_BOUNDS_EXCEEDED     STATUS_ARRAY_BOUNDS_EXCEEDED
#define EXCEPTION_FLT_DENORMAL_OPERAND      STATUS_FLOAT_DENORMAL_OPERAND
#define EXCEPTION_FLT_DIVIDE_BY_ZERO        STATUS_FLOAT_DIVIDE_BY_ZERO
#define EXCEPTION_FLT_INEXACT_RESULT        STATUS_FLOAT_INEXACT_RESULT
#define EXCEPTION_FLT_INVALID_OPERATION     STATUS_FLOAT_INVALID_OPERATION
#define EXCEPTION_FLT_OVERFLOW              STATUS_FLOAT_OVERFLOW
#define EXCEPTION_FLT_STACK_CHECK           STATUS_FLOAT_STACK_CHECK
#define EXCEPTION_FLT_UNDERFLOW             STATUS_FLOAT_UNDERFLOW
#define EXCEPTION_INT_DIVIDE_BY_ZERO        STATUS_INTEGER_DIVIDE_BY_ZERO
#define EXCEPTION_INT_OVERFLOW              STATUS_INTEGER_OVERFLOW
#define EXCEPTION_PRIV_INSTRUCTION          STATUS_PRIVILEGED_INSTRUCTION
#define EXCEPTION_IN_PAGE_ERROR             STATUS_IN_PAGE_ERROR
#define EXCEPTION_ILLEGAL_INSTRUCTION       STATUS_ILLEGAL_INSTRUCTION
#define EXCEPTION_NONCONTINUABLE_EXCEPTION  STATUS_NONCONTINUABLE_EXCEPTION
#define EXCEPTION_STACK_OVERFLOW            STATUS_STACK_OVERFLOW
#define EXCEPTION_INVALID_DISPOSITION       STATUS_INVALID_DISPOSITION
#define EXCEPTION_GUARD_PAGE                STATUS_GUARD_PAGE_VIOLATION
#define EXCEPTION_INVALID_HANDLE            STATUS_INVALID_HANDLE

#define CONTROL_C_EXIT                      STATUS_CONTROL_C_EXIT

/*  These are from the <FCNTL.H> file in windows.
    They are needed for _open_osfhandle.*/
#define _O_RDONLY   0x0000
#define _O_APPEND   0x0008
#define _O_TEXT     0x4000
#define _O_BINARY   0x8000

#ifdef  __cplusplus
}
#endif

#endif // __PAL_H__
