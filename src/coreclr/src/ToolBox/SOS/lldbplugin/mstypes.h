// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Contains some definitions duplicated from pal.h, etc. because pal.h
// has various conflicits with the linux standard runtime h files.
//
typedef void VOID;
typedef VOID *PVOID;

typedef int BOOL;
typedef int LONG;   
typedef unsigned int ULONG;
typedef ULONG ULONG32;
typedef ULONG *PULONG;
typedef unsigned char BYTE;
typedef unsigned char UCHAR;
typedef BYTE *PBYTE;
typedef unsigned short WORD;
typedef unsigned short USHORT;
typedef unsigned int DWORD;

typedef long long LONG64;
typedef unsigned long long ULONG64;
typedef ULONG64 *PULONG64;

typedef long long LONGLONG;
typedef unsigned long long ULONGLONG;
typedef ULONGLONG DWORD64;

#ifdef DBG_TARGET_64BIT
typedef ULONG64 ULONG_PTR, *PULONG_PTR;
typedef ULONG64 DWORD_PTR, *PDWORD_PTR;
#else
typedef ULONG32 ULONG_PTR, *PULONG_PTR;
typedef ULONG32 DWORD_PTR, *PDWORD_PTR;
#endif

typedef wchar_t WCHAR;
typedef WCHAR *PWCHAR;
typedef const WCHAR *PCWSTR;

typedef const char *PCSTR;
typedef char *PSTR;

typedef int HRESULT;

#define S_OK                             (HRESULT)0x00000000
#define S_FALSE                          (HRESULT)0x00000001
#define E_NOTIMPL                        (HRESULT)0x80004001
#define E_FAIL                           (HRESULT)0x80004005
#define E_INVALIDARG                     (HRESULT)0x80070057

#define MAX_PATH                         260 

#if defined(_MSC_VER) || defined(__llvm__)
#define DECLSPEC_ALIGN(x)   __declspec(align(x))
#else
#define DECLSPEC_ALIGN(x) 
#endif

// Platform-specific library naming
// 
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
