// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    palinternal.h

Abstract:

    CoreCLR Platform Adaptation Layer (PAL) header file used by source
    file part of the PAL implementation. This is a wrapper over 
    pal/inc/pal.h. It allows avoiding name collisions when including
    system header files, and it allows redirecting calls to 'standard' functions
    to their PAL counterpart

Details :

A] Rationale (see B] for the quick recipe)
There are 2 types of namespace collisions that must be handled.

1) standard functions declared in pal.h, which do not need to be 
   implemented in the PAL because the system's implementation is sufficient.

   (examples : memcpy, strlen, fclose)

   The problem with these is that a prototype for them is provided both in 
   pal.h and in a system header (stdio.h, etc). If a PAL file needs to 
   include the files containing both prototypes, the compiler may complain 
   about the multiple declarations.

   To avoid this, the inclusion of pal.h must be wrapped in a 
   #define/#undef pair, which will effectiveily "hide" the pal.h 
   declaration by renaming it to something else. this is done by palinternal.h
   in this way :

   #define some_function DUMMY_some_function
   #include <pal.h>
   #undef some_function

   when a PAL source file includes palinternal.h, it will see a prototype for 
   DUMMY_some_function instead of some_function; so when it includes the 
   system header with the "real" prototype, no collision occurs.

   (note : technically, no functions should ever be treated this way, all 
   system functions should be wrapped according to method 2, so that call 
   logging through ENTRY macros is done for all functions n the PAL. However 
   this reason alone is not currently considered enough to warrant a wrapper)

2) standard functions which must be reimplemented by the PAL, because the 
   system's implementation does not offer suitable functionnality.
   
   (examples : widestring functions, networking)
   
   Here, the problem is more complex. The PAL must provide functions with the 
   same name as system functions. Due to the nature of Unix dynamic linking, 
   if this is done, the PAL's implementation will effectively mask the "real" 
   function, so that all calls are directed to it. This makes it impossible for
   a function to be implemented as calling its counterpart in the system, plus 
   some extra work, because instead of calling the system's implementation, the
   function would only call itself in an infinitely recursing nightmare. Even 
   worse, if by bad luck the system libraries attempt to call the function for 
   which the PAL provides an implementation, it is the PAL's version that will 
   be called.
   It is therefore necessary to give the PAL's implementation of such functions
   a different name. However, PAL consumers (applications built on top of the 
   PAL) must be able to call the function by its 'official' name, not the PAL's 
   internal name. 
   This can be done with some more macro magic, by #defining the official name 
   to the internal name *in pal.h*. :

   #define some_function PAL_some_function

   This way, while PAL consumer code can use the official name, it is the 
   internal name that wil be seen at compile time.
   However, one extra step is needed. While PAL consumers must use the PAL's 
   implementation of these functions, the PAL itself must still have access to
   the "real" functions. This is done by #undefining in palinternal.h the names
   #defined in pal.h :

   #include <pal.h>
   #undef some_function.

   At this point, code in the PAL implementation can access *both* its own 
   implementation of the function (with PAL_some_function) *and* the system's 
   implementation (with some_function)

    [side note : for the Win32 PAL, this can be accomplished without touching 
    pal.h. In Windows, symbols in in dynamic libraries are resolved at 
    compile time. if an application that uses some_function is only linked to 
    pal.dll, some_function will be resolved to the version in that DLL, 
    even if other DLLs in the system provide other implementations. In addition,
    the function in the DLL can actually have a different name (e.g. 
    PAL_some_function), to which the 'official' name is aliased when the DLL 
    is compiled. All this is not possible with Unix dynamic linking, where 
    symbols are resolved at run-time in a first-found-first-used order. A 
    module may end up using the symbols from a module it was never linked with,
    simply because that module was located somewhere in the dependency chain. ]

    It should be mentionned that even if a function name is not documented as 
    being implemented in the system, it can still cause problems if it exists. 
    This is especially a problem for functions in the "reserved" namespace 
    (names starting with an underscore : _exit, etc). (We shouldn't really be 
    implementing functions with such a name, but we don't really have a choice)
    If such a case is detected, it should be wrapped according to method 2

    Note that for all this to work, it is important for the PAL's implementation
    files to #include palinternal.h *before* any system files, and to never 
    include pal.h directly.

B] Procedure for name conflict resolution :

When adding a function to pal.h, which is implemented by the system and 
which does not need a different implementation :

- add a #define function_name DUMMY_function_name to palinternal.h, after all 
  the other DUMMY_ #defines (above the #include <pal.h> line)
- add the function's prototype to pal.h (if that isn't already done)
- add a #undef function_name to palinternal.h near all the other #undefs 
  (after the #include <pal.h> line)
  
When overriding a system function with the PAL's own implementation :

- add a #define function_name PAL_function_name to pal.h, somewhere 
  before the function's prototype, inside a #ifndef _MSCVER/#endif pair 
  (to avoid affecting the Win32 build)
- add a #undef function_name to palinternal.h near all the other #undefs 
  (after the #include <pal.h> line)
- implement the function in the pal, naming it PAL_function_name
- within the PAL, call PAL_function_name() to call the PAL's implementation, 
function_name() to call the system's implementation



--*/

#ifndef _PAL_INTERNAL_H_
#define _PAL_INTERNAL_H_

#define PAL_IMPLEMENTATION

/* Include our configuration information so it's always present when
   compiling PAL implementation files. */
#include "config.h"

#ifdef DEBUG
#define _ENABLE_DEBUG_MESSAGES_ 1
#else
#define _ENABLE_DEBUG_MESSAGES_ 0
#endif

/* Include type_traits before including the pal.h. On newer glibcxx versions,
   the type_traits fail to compile if we redefine the wchar_t before including 
   the header */
#include <type_traits>

#ifdef PAL_PERF
#include "pal_perf.h"
#endif

/* C runtime functions needed to be renamed to avoid duplicate definition
   of those functions when including standard C header files */
#define div DUMMY_div
#define div_t DUMMY_div_t
#if !defined(_DEBUG)
#define memcpy DUMMY_memcpy 
#endif //!defined(_DEBUG)
#define memcmp DUMMY_memcmp 
#define memset DUMMY_memset 
#define memmove DUMMY_memmove 
#define memchr DUMMY_memchr
#define strlen DUMMY_strlen
#define stricmp DUMMY_stricmp 
#define strstr DUMMY_strstr 
#define strcmp DUMMY_strcmp 
#define strcat DUMMY_strcat
#define strncat DUMMY_strncat
#define strcpy DUMMY_strcpy
#define strcspn DUMMY_strcspn
#define strncmp DUMMY_strncmp
#define strncpy DUMMY_strncpy
#define strchr DUMMY_strchr
#define strrchr DUMMY_strrchr 
#define strpbrk DUMMY_strpbrk
#define strtod DUMMY_strtod
#define strspn DUMMY_strspn
#define tolower DUMMY_tolower
#define toupper DUMMY_toupper
#define islower DUMMY_islower
#define isupper DUMMY_isupper
#define isprint DUMMY_isprint
#define isdigit DUMMY_isdigit
#define iswalpha DUMMY_iswalpha
#define iswdigit DUMMY_iswdigit
#define iswupper DUMMY_iswupper
#define towupper DUMMY_towupper
#define towlower DUMMY_towlower
#define iswprint DUMMY_iswprint
#define iswspace DUMMY_iswspace
#define iswxdigit DUMMY_iswxdigit
#define wint_t DUMMY_wint_t
#define srand DUMMY_srand
#define atoi DUMMY_atoi
#define atof DUMMY_atof
#define tm PAL_tm
#define size_t DUMMY_size_t
#define time_t PAL_time_t
#define va_list DUMMY_va_list
#define abs DUMMY_abs
#define llabs DUMMY_llabs
#define ceil DUMMY_ceil
#define cos DUMMY_cos
#define cosh DUMMY_cosh
#define fabs DUMMY_fabs
#define floor DUMMY_floor
#define fmod DUMMY_fmod
#define modf DUMMY_modf
#define sin DUMMY_sin
#define sinh DUMMY_sinh
#define sqrt DUMMY_sqrt
#define tan DUMMY_tan
#define tanh DUMMY_tanh
#define ceilf DUMMY_ceilf
#define cosf DUMMY_cosf
#define coshf DUMMY_coshf
#define fabsf DUMMY_fabsf
#define floorf DUMMY_floorf
#define fmodf DUMMY_fmodf
#define modff DUMMY_modff
#define sinf DUMMY_sinf
#define sinhf DUMMY_sinhf
#define sqrtf DUMMY_sqrtf
#define tanf DUMMY_tanf
#define tanhf DUMMY_tanhf

/* RAND_MAX needed to be renamed to avoid duplicate definition when including 
   stdlib.h header files. PAL_RAND_MAX should have the same value as RAND_MAX 
   defined in pal.h  */
#define PAL_RAND_MAX 0x7fff

/* The standard headers define isspace and isxdigit as macros and functions,
   To avoid redefinition problems, undefine those macros. */
#ifdef isspace
#undef isspace
#endif
#ifdef isxdigit
#undef isxdigit
#endif
#ifdef isalpha
#undef isalpha
#endif
#ifdef isalnum
#undef isalnum
#endif
#define isspace DUMMY_isspace 
#define isxdigit DUMMY_isxdigit
#define isalpha DUMMY_isalpha
#define isalnum DUMMY_isalnum

#ifdef stdin
#undef stdin
#endif
#ifdef stdout
#undef stdout
#endif
#ifdef stderr
#undef stderr
#endif

#ifdef SCHAR_MIN
#undef SCHAR_MIN
#endif
#ifdef SCHAR_MAX
#undef SCHAR_MAX
#endif
#ifdef SHRT_MIN
#undef SHRT_MIN
#endif
#ifdef SHRT_MAX
#undef SHRT_MAX
#endif
#ifdef UCHAR_MAX
#undef UCHAR_MAX
#endif
#ifdef USHRT_MAX
#undef USHRT_MAX
#endif
#ifdef ULONG_MAX
#undef ULONG_MAX
#endif
#ifdef LONG_MIN
#undef LONG_MIN
#endif
#ifdef LONG_MAX
#undef LONG_MAX
#endif
#ifdef RAND_MAX
#undef RAND_MAX
#endif
#ifdef DBL_MAX
#undef DBL_MAX
#endif
#ifdef FLT_MAX
#undef FLT_MAX
#endif
#ifdef __record_type_class
#undef __record_type_class
#endif
#ifdef __real_type_class
#undef __real_type_class
#endif

// The standard headers define va_start and va_end as macros,
// To avoid redefinition problems, undefine those macros.
#ifdef va_start
#undef va_start
#endif
#ifdef va_end
#undef va_end
#endif
#ifdef va_copy
#undef va_copy
#endif


#ifdef _VAC_
#define wchar_16 wchar_t
#else
#define wchar_t wchar_16
#endif // _VAC_

#define ptrdiff_t PAL_ptrdiff_t
#define intptr_t PAL_intptr_t
#define uintptr_t PAL_uintptr_t
#define timeval PAL_timeval
#define FILE PAL_FILE

#include "pal.h"
#include "palprivate.h"

#include "mbusafecrt.h"

#ifdef _VAC_
#undef CHAR_BIT
#undef va_arg
#endif

#if !defined(_MSC_VER) && defined(_WIN64)
#undef _BitScanForward64
#undef _BitScanReverse64
#endif 

/* pal.h defines alloca(3) as a compiler builtin.
   Redefining it to native libc will result in undefined breakage because
   a compiler is allowed to make assumptions about the stack and frame
   pointers. */

/* Undef all functions and types previously defined so those functions and
   types could be mapped to the C runtime and socket implementation of the 
   native OS */
#undef exit
#undef atexit
#undef div
#undef div_t
#undef memcpy
#undef memcmp
#undef memset
#undef memmove
#undef memchr
#undef strlen
#undef strnlen
#undef wcsnlen
#undef stricmp
#undef strstr
#undef strcmp
#undef strcat
#undef strcspn
#undef strncat
#undef strcpy
#undef strncmp
#undef strncpy
#undef strchr
#undef strrchr
#undef strpbrk
#undef strtoul
#undef strtod
#undef strspn
#undef strtok
#undef strdup
#undef tolower
#undef toupper
#undef islower
#undef isupper
#undef isprint
#undef isdigit
#undef isspace
#undef iswdigit
#undef iswxdigit
#undef iswalpha
#undef iswprint
#undef isxdigit
#undef isalpha
#undef isalnum
#undef iswalpha
#undef iswdigit
#undef iswupper
#undef towupper
#undef towlower
#undef wint_t
#undef atoi
#undef atol
#undef atof
#undef malloc
#undef realloc
#undef free
#undef qsort
#undef bsearch
#undef time
#undef tm
#undef localtime
#undef mktime
#undef FILE
#undef fclose
#undef setbuf
#undef fopen
#undef fread
#undef feof
#undef ferror
#undef ftell
#undef fflush
#undef fwrite
#undef fgets
#undef fgetws
#undef fputc
#undef putchar
#undef fputs
#undef fseek
#undef fgetpos
#undef fsetpos
#undef getcwd
#undef getc
#undef fgetc
#undef ungetc
#undef _flushall
#undef setvbuf
#undef mkstemp
#undef rename
#undef unlink
#undef size_t
#undef time_t
#undef va_list
#undef va_start
#undef va_end
#undef va_copy
#undef stdin
#undef stdout
#undef stderr
#undef abs
#undef labs
#undef llabs
#undef acos
#undef acosh
#undef asin
#undef asinh
#undef atan
#undef atanh
#undef atan2
#undef cbrt
#undef ceil
#undef cos
#undef cosh
#undef exp
#undef fabs
#undef floor
#undef fmod
#undef fma
#undef ilogb
#undef log
#undef log2
#undef log10
#undef modf
#undef pow
#undef scalbn
#undef sin
#undef sinh
#undef sqrt
#undef tan
#undef tanh
#undef acosf
#undef acoshf
#undef asinf
#undef asinhf
#undef atanf
#undef atanhf
#undef atan2f
#undef cbrtf
#undef ceilf
#undef cosf
#undef coshf
#undef expf
#undef fabsf
#undef floorf
#undef fmodf
#undef fmaf
#undef ilogbf
#undef logf
#undef log2f
#undef log10f
#undef modff
#undef powf
#undef scalbnf
#undef sinf
#undef sinhf
#undef sqrtf
#undef tanf
#undef tanhf
#undef rand
#undef srand
#undef errno
#undef getenv 
#undef wcsspn
#undef open
#undef glob

#undef wchar_t
#undef ptrdiff_t
#undef intptr_t
#undef uintptr_t
#undef timeval


#undef printf
#undef fprintf
#undef fwprintf
#undef vfprintf
#undef vfwprintf
#undef vprintf
#undef wprintf
#undef wcstod
#undef wcstol
#undef wcstoul
#undef _wcstoui64
#undef wcscat
#undef wcscpy
#undef wcslen
#undef wcsncmp
#undef wcschr
#undef wcsrchr
#undef swscanf
#undef wcspbrk
#undef wcsstr
#undef wcscmp
#undef wcsncat
#undef wcsncpy
#undef wcstok
#undef wcscspn
#undef iswupper
#undef iswspace
#undef towlower
#undef towupper
#undef wvsnprintf

#ifdef _AMD64_ 
#undef _mm_getcsr
#undef _mm_setcsr
#endif // _AMD64_

#undef ctime

#undef min
#undef max

#undef SCHAR_MIN
#undef SCHAR_MAX
#undef UCHAR_MAX
#undef SHRT_MIN
#undef SHRT_MAX
#undef USHRT_MAX
#undef LONG_MIN
#undef LONG_MAX
#undef ULONG_MAX
#undef RAND_MAX
#undef DBL_MAX
#undef FLT_MAX
#undef __record_type_class
#undef __real_type_class

#if HAVE_CHAR_BIT
#undef CHAR_BIT
#endif

// We need a sigsetjmp prototype in pal.h for the SEH macros, but we
// can't use the "real" prototype (because we don't want to define sigjmp_buf).
// So we must rename the "real" sigsetjmp to avoid redefinition errors.
#define sigsetjmp REAL_sigsetjmp
#define siglongjmp REAL_siglongjmp
#include <setjmp.h>
#undef sigsetjmp
#undef siglongjmp

#undef _SIZE_T_DEFINED
#undef _WCHAR_T_DEFINED

#define _DONT_USE_CTYPE_INLINE_
#if HAVE_RUNETYPE_H
#include <runetype.h>
#endif
#include <ctype.h>

// Don't use C++ wrappers for stdlib.h
// https://gcc.gnu.org/ml/libstdc++/2016-01/msg00025.html 
#define _GLIBCXX_INCLUDE_NEXT_C_HEADERS 1

#define _WITH_GETLINE
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
#include <pwd.h>
#include <unistd.h>
#include <fcntl.h>
#include <glob.h>

#ifdef __APPLE__

#undef GetCurrentThread
#include <CoreServices/CoreServices.h>

#include <malloc/malloc.h>

#endif // __APPLE__

/* we don't really need this header here, but by including it we make sure
   we'll catch any definition conflicts */
#include <sys/socket.h>

#include <pal/stackstring.hpp>

#if !HAVE_INFTIM
#define INFTIM  -1
#endif // !HAVE_INFTIM

#define OffsetOf(TYPE, MEMBER) __builtin_offsetof(TYPE, MEMBER)

#undef assert
#define assert (Use__ASSERTE_instead_of_assert) assert

#define string_countof(a) (sizeof(a) / sizeof(a[0]) - 1)

#ifndef __ANDROID__
#define TEMP_DIRECTORY_PATH "/tmp/"
#else
// On Android, "/tmp/" doesn't exist; temporary files should go to
// /data/local/tmp/
#define TEMP_DIRECTORY_PATH "/data/local/tmp/"
#endif

#define PROCESS_PIPE_NAME_PREFIX ".dotnet-pal-processpipe"

#ifdef __APPLE__
#define APPLICATION_CONTAINER_BASE_PATH_SUFFIX "/Library/Group Containers/"

// Not much to go with, but Max semaphore length on Mac is 31 characters. In a sandbox, the semaphore name
// must be prefixed with an application group ID. This will be 10 characters for developer ID and extra 2 
// characters for group name. For example ABCDEFGHIJ.MS. We still need some characters left
// for the actual semaphore names.
#define MAX_APPLICATION_GROUP_ID_LENGTH 13
#endif // __APPLE__

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef enum _TimeConversionConstants
{
    tccSecondsToMillieSeconds       = 1000,         // 10^3
    tccSecondsToMicroSeconds        = 1000000,      // 10^6
    tccSecondsToNanoSeconds         = 1000000000,   // 10^9
    tccMillieSecondsToMicroSeconds  = 1000,         // 10^3
    tccMillieSecondsToNanoSeconds   = 1000000,      // 10^6
    tccMicroSecondsToNanoSeconds    = 1000,         // 10^3
    tccSecondsTo100NanoSeconds      = 10000000,     // 10^7
    tccMicroSecondsTo100NanoSeconds = 10            // 10^1
} TimeConversionConstants;

#ifdef __cplusplus
}

bool
ReadMemoryValueFromFile(const char* filename, size_t* val);

#ifdef __APPLE__
bool
GetApplicationContainerFolder(PathCharString& buffer, const char *applicationGroupId, int applicationGroupIdLength);
#endif // __APPLE__

/* This is duplicated in utilcode.h for CLR, with cooler type-traits */
template <typename T>
inline
T* InterlockedExchangePointerT(
    T* volatile *Target,
    T* Value)
{
    return (T*)(InterlockedExchangePointer(
        (PVOID volatile*)Target,
        (PVOID)Value));
}

template <typename T>
inline
T* InterlockedCompareExchangePointerT(
    T* volatile *destination,
    T* exchange,
    T* comparand)
{
    return (T*)(InterlockedCompareExchangePointer(
        (PVOID volatile*)destination,
        (PVOID)exchange,
        (PVOID)comparand));
}

template <typename T>
inline T* InterlockedExchangePointerT(
    T* volatile * target,
    int           value) // When NULL is provided as argument.
{
    //STATIC_ASSERT(value == 0);
    return InterlockedExchangePointerT(target, reinterpret_cast<T*>(value));
}

template <typename T>
inline T* InterlockedCompareExchangePointerT(
    T* volatile * destination,
    int           exchange,  // When NULL is provided as argument.
    T*            comparand)
{
    //STATIC_ASSERT(exchange == 0);
    return InterlockedCompareExchangePointerT(destination, reinterpret_cast<T*>(exchange), comparand);
}

template <typename T>
inline T* InterlockedCompareExchangePointerT(
    T* volatile * destination,
    T*            exchange,
    int           comparand) // When NULL is provided as argument.
{
    //STATIC_ASSERT(comparand == 0);
    return InterlockedCompareExchangePointerT(destination, exchange, reinterpret_cast<T*>(comparand));
}

#undef InterlockedExchangePointer
#define InterlockedExchangePointer InterlockedExchangePointerT
#undef InterlockedCompareExchangePointer
#define InterlockedCompareExchangePointer InterlockedCompareExchangePointerT

#include "volatile.h"

const char StackOverflowMessage[] = "Stack overflow.\n";

#endif // __cplusplus

#endif /* _PAL_INTERNAL_H_ */
