// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

////////////////////////////////////////////////////////////////////////
// Extensions to the usual posix header files
////////////////////////////////////////////////////////////////////////

#ifndef __PAL_MSTYPES_H__
#define __PAL_MSTYPES_H__

#include <stdint.h>

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

// Some platforms (such as FreeBSD) define the __fastcall macro
// on all targets, even when using it will fail.
// Undefine it here so we can use it on all platforms without error.
#ifdef __fastcall
#undef __fastcall
#endif

#define __fastcall
#define _fastcall

#endif  // !defined(__i386__)

#define CALLBACK __cdecl

#if !defined(_declspec)
#define _declspec(e)  __declspec(e)
#endif

#if defined(_VAC_) && defined(__cplusplus)
#define __inline        inline
#endif

#define __forceinline   inline

#define PALIMPORT

#ifndef DLLEXPORT
#define DLLEXPORT __attribute__((visibility("default")))
#endif

#define PAL_NORETURN    __attribute__((noreturn))

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

// GCC's way of declaring large integer constants
// If you define these in one step, without the _HELPER macros, you
// get extra whitespace when composing these with other concatenating macros.
#define I64_HELPER(x) x ## LL
#define I64(x)        I64_HELPER(x)

#define UI64_HELPER(x) x ## ULL
#define UI64(x)        UI64_HELPER(x)

////////////////////////////////////////////////////////////////////////
// Misc. types
////////////////////////////////////////////////////////////////////////

#if HOST_64BIT
typedef long double LONG_DOUBLE;
#endif

typedef void VOID;

typedef int LONG;       // NOTE: diff from windows.h, for LP64 compat
typedef unsigned int ULONG; // NOTE: diff from windows.h, for LP64 compat

typedef int64_t LONGLONG;
typedef uint64_t ULONGLONG;
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

typedef uint8_t UINT8;
typedef int8_t INT8;
typedef uint16_t UINT16;
typedef int16_t INT16;
typedef uint32_t UINT32, *PUINT32;
typedef int32_t INT32, *PINT32;
typedef uint64_t UINT64, *PUINT64;
typedef int64_t INT64, *PINT64;

typedef uint32_t ULONG32, *PULONG32;
typedef int32_t LONG32, *PLONG32;
typedef uint64_t ULONG64;
typedef int64_t LONG64;

typedef intptr_t INT_PTR, *PINT_PTR;
typedef uintptr_t UINT_PTR, *PUINT_PTR;

#ifdef HOST_64BIT

#define _atoi64 (int64_t)atoll

typedef int64_t LONG_PTR, *PLONG_PTR;
typedef uint64_t ULONG_PTR, *PULONG_PTR;
typedef uint64_t DWORD_PTR, *PDWORD_PTR;

#else

typedef int32_t LONG_PTR, *PLONG_PTR;
typedef uint32_t ULONG_PTR, *PULONG_PTR;
typedef uint32_t DWORD_PTR, *PDWORD_PTR;

#endif

typedef uintptr_t SIZE_T;
typedef intptr_t SSIZE_T;

#ifndef SIZE_T_MAX
#define SIZE_T_MAX UINTPTR_MAX
#endif // SIZE_T_MAX

#ifndef SSIZE_T_MAX
#define SSIZE_T_MAX INTPTR_MAX
#endif

#ifndef SSIZE_T_MIN
#define SSIZE_T_MIN INTPTR_MIN
#endif

typedef LONG_PTR LPARAM;

typedef char16_t WCHAR;

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
