// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++





--*/

////////////////////////////////////////////////////////////////////////
// Extensions to the usual posix header files
////////////////////////////////////////////////////////////////////////

#ifndef __PAL_MSTYPES_H__
#define __PAL_MSTYPES_H__

#ifdef  __cplusplus
extern "C" {
#endif

////////////////////////////////////////////////////////////////////////
// calling convention stuff
////////////////////////////////////////////////////////////////////////


#ifdef __cplusplus
#define EXTERN_C extern "C"
#else
#define EXTERN_C
#endif // __cplusplus

#ifndef _MSC_VER

// Note:  Win32-hosted GCC predefines __stdcall and __cdecl, but Unix-
// hosted GCC does not.

#ifdef __i386__

#if !defined(__stdcall)
#define __stdcall      __attribute__((stdcall))
#endif
#if !defined(_stdcall)
#define _stdcall       __stdcall
#endif

#if !defined(__cdecl)
#define __cdecl        __attribute__((cdecl))
#endif
#if !defined(_cdecl)
#define _cdecl         __cdecl
#endif
#if !defined(CDECL)
#define CDECL          __cdecl
#endif


#else   // !defined(__i386__)

#define __stdcall
#define _stdcall
#define __cdecl
#define _cdecl
#define CDECL

// On ARM __fastcall is ignored and causes a compile error
#if !defined(PAL_STDCPP_COMPAT) || defined(__arm__)
#  undef __fastcall
#  undef _fastcall
#  define __fastcall
#  define _fastcall
#endif // !defined(PAL_STDCPP_COMPAT) || defined(__arm__)

#endif  // !defined(__i386__)

#define CALLBACK __cdecl

#if !defined(_declspec)
#define _declspec(e)  __declspec(e)
#endif

#if defined(_VAC_) && defined(__cplusplus)
#define __inline        inline
#endif

#define __forceinline   inline

#endif // !_MSC_VER

#ifdef _MSC_VER

#if defined(PAL_IMPLEMENTATION)
#define PALIMPORT
#else
#define PALIMPORT   __declspec(dllimport)
#endif
#define DLLEXPORT __declspec(dllexport)
#define PAL_NORETURN __declspec(noreturn)

#else

#define PALIMPORT
#ifndef DLLEXPORT
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#define PAL_NORETURN    __attribute__((noreturn))

#endif

#define PALAPI             DLLEXPORT __cdecl
#define PALAPI_NOEXPORT    __cdecl
#define PALAPIV            __cdecl

////////////////////////////////////////////////////////////////////////
// Type attribute stuff
////////////////////////////////////////////////////////////////////////

#define CONST const
#define IN
#define OUT
#define OPTIONAL
#define FAR

#ifdef UNICODE
#define __TEXT(x) L##x
#else
#define __TEXT(x) x
#endif
#define TEXT(x) __TEXT(x)

////////////////////////////////////////////////////////////////////////
// Some special values
////////////////////////////////////////////////////////////////////////

#ifndef TRUE
#define TRUE 1
#endif

#ifndef FALSE
#define FALSE 0
#endif

////////////////////////////////////////////////////////////////////////
// Misc. type helpers
////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER

// MSVC's way of declaring large integer constants
// If you define these in one step, without the _HELPER macros, you
// get extra whitespace when composing these with other concatenating macros.
#define I64_HELPER(x) x ## i64
#define I64(x)        I64_HELPER(x)

#define UI64_HELPER(x) x ## ui64
#define UI64(x)        UI64_HELPER(x)

#else // _MSC_VER

// GCC's way of declaring large integer constants
// If you define these in one step, without the _HELPER macros, you
// get extra whitespace when composing these with other concatenating macros.
#define I64_HELPER(x) x ## LL
#define I64(x)        I64_HELPER(x)

#define UI64_HELPER(x) x ## ULL
#define UI64(x)        UI64_HELPER(x)

#endif // _MSC_VER

////////////////////////////////////////////////////////////////////////
// Misc. types
////////////////////////////////////////////////////////////////////////

#ifndef _MSC_VER

// A bunch of source files (e.g. most of the ndp tree) include pal.h
// but are written to be LLP64, not LP64.  (LP64 => long = 64 bits
// LLP64 => longs = 32 bits, long long = 64 bits)
//
// To handle this difference, we #define long to be int (and thus 32 bits) when
// compiling those files.  (See the bottom of this file or search for
// #define long to see where we do this.)
//
// But this fix is more complicated than it seems, because we also use the
// preprocessor to #define __int64 to long for LP64 architectures (__int64
// isn't a builtin in gcc).   We don't want __int64 to be an int (by cascading
// macro rules).  So we play this little trick below where we add
// __cppmungestrip before "long", which is what we're really #defining __int64
// to.  The preprocessor sees __cppmungestriplong as something different than
// long, so it doesn't replace it with int.  The during the cppmunge phase, we
// remove the __cppmungestrip part, leaving long for the compiler to see.
//
// Note that we can't just use a typedef to define __int64 as long before
// #defining long because typedefed types can't be signedness-agnostic (i.e.
// they must be either signed or unsigned) and we want to be able to use
// __int64 as though it were intrinsic

#if defined(HOST_64BIT) && !defined(__APPLE__)
#define __int64     long
#else // HOST_64BIT && !__APPLE__
#define __int64     long long
#endif // HOST_64BIT && !__APPLE__
#define __int32     int
#define __int16     short int
#define __int8      char        // assumes char is signed

#endif // _MSC_VER

#ifndef PAL_STDCPP_COMPAT

#ifndef _MSC_VER

#if HOST_64BIT
typedef long double LONG_DOUBLE;
#endif

#endif // _MSC_VER
#endif // !PAL_STDCPP_COMPAT

typedef void VOID;

typedef int LONG;       // NOTE: diff from windows.h, for LP64 compat
typedef unsigned int ULONG; // NOTE: diff from windows.h, for LP64 compat

typedef __int64 LONGLONG;
typedef unsigned __int64 ULONGLONG;
typedef ULONGLONG DWORD64;
typedef DWORD64 *PDWORD64;
typedef LONGLONG *PLONG64;
typedef ULONGLONG *PULONG64;
typedef ULONGLONG *PULONGLONG;
typedef ULONG *PULONG;
typedef short SHORT;
typedef SHORT *PSHORT;
typedef unsigned short USHORT;
typedef USHORT *PUSHORT;
typedef unsigned char UCHAR;
typedef UCHAR *PUCHAR;
typedef char *PSZ;
typedef ULONGLONG DWORDLONG;

typedef unsigned int DWORD; // NOTE: diff from  windows.h, for LP64 compat
typedef unsigned int DWORD32, *PDWORD32;

typedef int BOOL;
typedef unsigned char BYTE;
typedef unsigned short WORD;
typedef float FLOAT;
typedef double DOUBLE;
typedef BOOL *PBOOL;
typedef BOOL *LPBOOL;
typedef BYTE *PBYTE;
typedef BYTE *LPBYTE;
typedef const BYTE *LPCBYTE;
typedef int *PINT;
typedef int *LPINT;
typedef WORD *PWORD;
typedef WORD *LPWORD;
typedef LONG *LPLONG;
typedef LPLONG PLONG;
typedef DWORD *PDWORD;
typedef DWORD *LPDWORD;
typedef void *PVOID;
typedef void *LPVOID;
typedef CONST void *LPCVOID;
typedef int INT;
typedef unsigned int UINT;
typedef unsigned int *PUINT;
typedef BYTE BOOLEAN;
typedef BOOLEAN *PBOOLEAN;

typedef unsigned __int8 UINT8;
typedef signed __int8 INT8;
typedef unsigned __int16 UINT16;
typedef signed __int16 INT16;
typedef unsigned __int32 UINT32, *PUINT32;
typedef signed __int32 INT32, *PINT32;
typedef unsigned __int64 UINT64, *PUINT64;
typedef signed __int64 INT64, *PINT64;

typedef unsigned __int32 ULONG32, *PULONG32;
typedef signed __int32 LONG32, *PLONG32;
typedef unsigned __int64 ULONG64;
typedef signed __int64 LONG64;

#if defined(HOST_X86) && _MSC_VER >= 1300
#define _W64 __w64
#else
#define _W64
#endif

#ifdef HOST_64BIT

#define _atoi64 (__int64)atoll

typedef __int64 INT_PTR, *PINT_PTR;
typedef unsigned __int64 UINT_PTR, *PUINT_PTR;
typedef __int64 LONG_PTR, *PLONG_PTR;
typedef unsigned __int64 ULONG_PTR, *PULONG_PTR;
typedef unsigned __int64 DWORD_PTR, *PDWORD_PTR;

/* maximum signed 64 bit value */
#define LONG_PTR_MAX      I64(9223372036854775807)
/* maximum unsigned 64 bit value */
#define ULONG_PTR_MAX     UI64(0xffffffffffffffff)

#ifndef SIZE_MAX
#define SIZE_MAX _UI64_MAX
#endif

#define __int3264   __int64

#if !defined(HOST_64BIT)
__inline
unsigned long
HandleToULong(
    const void *h
    )
{
    return((unsigned long) (ULONG_PTR) h );
}

__inline
long
HandleToLong(
    const void *h
    )
{
    return((long) (LONG_PTR) h );
}

__inline
void *
ULongToHandle(
    const unsigned long h
    )
{
    return((void *) (UINT_PTR) h );
}


__inline
void *
LongToHandle(
    const long h
    )
{
    return((void *) (INT_PTR) h );
}


__inline
unsigned long
PtrToUlong(
    const void  *p
    )
{
    return((unsigned long) (ULONG_PTR) p );
}

__inline
unsigned int
PtrToUint(
    const void  *p
    )
{
    return((unsigned int) (UINT_PTR) p );
}

__inline
unsigned short
PtrToUshort(
    const void  *p
    )
{
    return((unsigned short) (unsigned long) (ULONG_PTR) p );
}

__inline
long
PtrToLong(
    const void  *p
    )
{
    return((long) (LONG_PTR) p );
}

__inline
int
PtrToInt(
    const void  *p
    )
{
    return((int) (INT_PTR) p );
}

__inline
short
PtrToShort(
    const void  *p
    )
{
    return((short) (long) (LONG_PTR) p );
}

__inline
void *
IntToPtr(
    const int i
    )
// Caution: IntToPtr() sign-extends the int value.
{
    return( (void *)(INT_PTR)i );
}

__inline
void *
UIntToPtr(
    const unsigned int ui
    )
// Caution: UIntToPtr() zero-extends the unsigned int value.
{
    return( (void *)(UINT_PTR)ui );
}

__inline
void *
LongToPtr(
    const long l
    )
// Caution: LongToPtr() sign-extends the long value.
{
    return( (void *)(LONG_PTR)l );
}

__inline
void *
ULongToPtr(
    const unsigned long ul
    )
// Caution: ULongToPtr() zero-extends the unsigned long value.
{
    return( (void *)(ULONG_PTR)ul );
}

__inline
void *
ShortToPtr(
    const short s
    )
// Caution: ShortToPtr() sign-extends the short value.
{
    return( (void *)(INT_PTR)s );
}

__inline
void *
UShortToPtr(
    const unsigned short us
    )
// Caution: UShortToPtr() zero-extends the unsigned short value.
{
    return( (void *)(UINT_PTR)us );
}

#else // !defined(HOST_64BIT)
#define HandleToULong( h ) ((ULONG)(ULONG_PTR)(h) )
#define HandleToLong( h )  ((LONG)(LONG_PTR) (h) )
#define ULongToHandle( ul ) ((HANDLE)(ULONG_PTR) (ul) )
#define LongToHandle( h )   ((HANDLE)(LONG_PTR) (h) )
#define PtrToUlong( p ) ((ULONG)(ULONG_PTR) (p) )
#define PtrToLong( p )  ((LONG)(LONG_PTR) (p) )
#define PtrToUint( p ) ((UINT)(UINT_PTR) (p) )
#define PtrToInt( p )  ((INT)(INT_PTR) (p) )
#define PtrToUshort( p ) ((unsigned short)(ULONG_PTR)(p) )
#define PtrToShort( p )  ((short)(LONG_PTR)(p) )
#define IntToPtr( i )    ((VOID *)(INT_PTR)((int)(i)))
#define UIntToPtr( ui )  ((VOID *)(UINT_PTR)((unsigned int)(ui)))
#define LongToPtr( l )   ((VOID *)(LONG_PTR)((long)(l)))
#define ULongToPtr( ul ) ((VOID *)(ULONG_PTR)((unsigned long)(ul)))
#define ShortToPtr( s )  ((VOID *)(INT_PTR)((short)(s)))
#define UShortToPtr( us )  ((VOID *)(UINT_PTR)((unsigned short)(s)))
#endif // !defined(HOST_64BIT)



#else

typedef _W64 __int32 INT_PTR;
typedef _W64 unsigned __int32 UINT_PTR;

typedef _W64 __int32 LONG_PTR;
typedef _W64 unsigned __int32 ULONG_PTR, *PULONG_PTR;
typedef _W64 unsigned __int32 DWORD_PTR, *PDWORD_PTR;

/* maximum signed 32 bit value */
#define LONG_PTR_MAX      2147483647L
/* maximum unsigned 32 bit value */
#define ULONG_PTR_MAX     0xffffffffUL

#ifndef SIZE_MAX
#define SIZE_MAX UINT_MAX
#endif

#define __int3264   __int32

#define HandleToULong( h ) ((ULONG)(ULONG_PTR)(h) )
#define HandleToLong( h )  ((LONG)(LONG_PTR) (h) )
#define ULongToHandle( ul ) ((HANDLE)(ULONG_PTR) (ul) )
#define LongToHandle( h )   ((HANDLE)(LONG_PTR) (h) )
#define PtrToUlong( p ) ((ULONG)(ULONG_PTR) (p) )
#define PtrToLong( p )  ((LONG)(LONG_PTR) (p) )
#define PtrToUint( p ) ((UINT)(UINT_PTR) (p) )
#define PtrToInt( p )  ((INT)(INT_PTR) (p) )
#define PtrToUshort( p ) ((unsigned short)(ULONG_PTR)(p) )
#define PtrToShort( p )  ((short)(LONG_PTR)(p) )
#define IntToPtr( i )    ((VOID *)(INT_PTR)((int)i))
#define UIntToPtr( ui )  ((VOID *)(UINT_PTR)((unsigned int)ui))
#define LongToPtr( l )   ((VOID *)(LONG_PTR)((long)l))
#define ULongToPtr( ul ) ((VOID *)(ULONG_PTR)((unsigned long)ul))
#define ShortToPtr( s )  ((VOID *)(INT_PTR)((short)s))
#define UShortToPtr( us )  ((VOID *)(UINT_PTR)((unsigned short)s))

#endif

#define HandleToUlong(h)  HandleToULong(h)
#define UlongToHandle(ul) ULongToHandle(ul)
#define UlongToPtr(ul) ULongToPtr(ul)
#define UintToPtr(ui)  UIntToPtr(ui)

#ifdef HOST_64BIT
typedef unsigned long SIZE_T;
typedef long SSIZE_T;
#else
typedef unsigned int SIZE_T;
typedef int SSIZE_T;
#endif

static_assert(sizeof(SIZE_T) == sizeof(void*), "SIZE_T should be pointer sized");
static_assert(sizeof(SSIZE_T) == sizeof(void*), "SSIZE_T should be pointer sized");

#ifndef SIZE_T_MAX
#define SIZE_T_MAX ULONG_PTR_MAX
#endif // SIZE_T_MAX

#ifndef SSIZE_T_MAX
#define SSIZE_T_MAX LONG_PTR_MAX
#endif

#ifndef SSIZE_T_MIN
#define SSIZE_T_MIN (ssize_t)I64(0x8000000000000000)
#endif

#ifndef PAL_STDCPP_COMPAT
#ifdef HOST_64BIT
typedef unsigned long size_t;
typedef long ssize_t;
typedef long ptrdiff_t;
#else // !HOST_64BIT
typedef unsigned int size_t;
typedef int ptrdiff_t;
#endif // !HOST_64BIT
#endif // !PAL_STDCPP_COMPAT
#define _SIZE_T_DEFINED

typedef LONG_PTR LPARAM;

#define _PTRDIFF_T_DEFINED
#ifdef _MINGW_
// We need to define _PTRDIFF_T to make sure ptrdiff_t doesn't get defined
// again by system headers - but only for MinGW.
#define _PTRDIFF_T
#endif

typedef char16_t WCHAR;

#ifndef PAL_STDCPP_COMPAT

#if defined(__linux__)
#ifdef HOST_64BIT
typedef long int intptr_t;
typedef unsigned long int uintptr_t;
#else // !HOST_64BIT
typedef int intptr_t;
typedef unsigned int uintptr_t;
#endif // !HOST_64BIT
#else
typedef long int intptr_t;
typedef unsigned long int uintptr_t;
#endif

#endif // PAL_STDCPP_COMPAT

#define _INTPTR_T_DEFINED
#define _UINTPTR_T_DEFINED

typedef DWORD LCID;
typedef PDWORD PLCID;
typedef WORD LANGID;

typedef DWORD LCTYPE;

typedef WCHAR *PWCHAR;
typedef WCHAR *LPWCH, *PWCH;
typedef CONST WCHAR *LPCWCH, *PCWCH;
typedef WCHAR *NWPSTR;
typedef WCHAR *LPWSTR, *PWSTR;

typedef CONST WCHAR *LPCWSTR, *PCWSTR;

typedef char CHAR;
typedef CHAR *PCHAR;
typedef CHAR *LPCH, *PCH;
typedef CONST CHAR *LPCCH, *PCCH;
typedef CHAR *NPSTR;
typedef CHAR *LPSTR, *PSTR;
typedef CONST CHAR *LPCSTR, *PCSTR;

#ifdef UNICODE
typedef WCHAR TCHAR;
typedef WCHAR _TCHAR;
#else
typedef CHAR TCHAR;
typedef CHAR _TCHAR;
#endif
typedef TCHAR *PTCHAR;
typedef TCHAR *LPTSTR, *PTSTR;
typedef CONST TCHAR *LPCTSTR;

#define MAKEWORD(a, b)      ((WORD)(((BYTE)((DWORD_PTR)(a) & 0xff)) | ((WORD)((BYTE)((DWORD_PTR)(b) & 0xff))) << 8))
#define MAKELONG(a, b)      ((LONG)(((WORD)((DWORD_PTR)(a) & 0xffff)) | ((DWORD)((WORD)((DWORD_PTR)(b) & 0xffff))) << 16))
#define LOWORD(l)           ((WORD)((DWORD_PTR)(l) & 0xffff))
#define HIWORD(l)           ((WORD)((DWORD_PTR)(l) >> 16))
#define LOBYTE(w)           ((BYTE)((DWORD_PTR)(w) & 0xff))
#define HIBYTE(w)           ((BYTE)((DWORD_PTR)(w) >> 8))

typedef VOID *HANDLE;
typedef HANDLE HWND;
typedef struct __PAL_RemoteHandle__ { HANDLE h; } *RHANDLE;
typedef HANDLE *PHANDLE;
typedef HANDLE *LPHANDLE;
#define INVALID_HANDLE_VALUE ((VOID *)(-1))
#define INVALID_FILE_SIZE ((DWORD)0xFFFFFFFF)
#define INVALID_FILE_ATTRIBUTES ((DWORD) -1)
typedef HANDLE HMODULE;
typedef HANDLE HINSTANCE;
typedef HANDLE HGLOBAL;
typedef HANDLE HLOCAL;
typedef HANDLE HRSRC;

typedef LONG HRESULT;
typedef LONG NTSTATUS;

typedef union _LARGE_INTEGER {
    struct {
#if BIGENDIAN
        LONG HighPart;
        DWORD LowPart;
#else
        DWORD LowPart;
        LONG HighPart;
#endif
    } u;
    LONGLONG QuadPart;
} LARGE_INTEGER, *PLARGE_INTEGER;

#ifndef GUID_DEFINED
typedef struct _GUID {
    ULONG   Data1;    // NOTE: diff from Win32, for LP64
    USHORT  Data2;
    USHORT  Data3;
    UCHAR   Data4[ 8 ];
} GUID;
typedef const GUID *LPCGUID;
#define GUID_DEFINED
#endif // !GUID_DEFINED

typedef struct _FILETIME {
    DWORD dwLowDateTime;
    DWORD dwHighDateTime;
} FILETIME, *PFILETIME, *LPFILETIME;

/* Code Page Default Values */
#define CP_ACP          0   /* default to ANSI code page */
#define CP_UTF8     65001   /* UTF-8 translation */

typedef PVOID PSID;

#ifdef  __cplusplus
}
#endif

#endif // __PAL_MSTYPES_H__
