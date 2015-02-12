//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
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