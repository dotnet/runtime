//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

// Contains some definitions duplicated from pal.h, etc. because pal.h
// has various conflicits with the linux standard runtime h files.
//
typedef void VOID;
typedef VOID *PVOID;

typedef int BOOL;
typedef int LONG;   
typedef unsigned int ULONG;
typedef ULONG *PULONG;

typedef unsigned long long ULONG64;
typedef ULONG64 *PULONG64;

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
