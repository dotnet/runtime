// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_TYPES__H
#define _INTEROP_TYPES__H

#include <stddef.h>

#undef INT_MIN
#define INT_MIN	   (-2147483647 - 1)

typedef char16_t WCHAR;
typedef int BOOL;
typedef WCHAR *LPWSTR, *PWSTR;
typedef const WCHAR *LPCWSTR, *PCWSTR;

typedef char* LPSTR;
typedef const char* LPCSTR;
typedef void* FARPROC;
typedef void* HANDLE;
typedef void* HMODULE;
typedef int error_t;
typedef void* LPVOID;
typedef unsigned char BYTE;
typedef WCHAR OLECHAR;
typedef ptrdiff_t INT_PTR;
typedef size_t UINT_PTR;

typedef unsigned long long ULONG64;
typedef unsigned long long LONG64;
typedef double DOUBLE;
typedef float FLOAT;
typedef int INT, *LPINT;
typedef unsigned int UINT;
typedef int LONG;
typedef char CHAR, *PCHAR;
typedef unsigned short USHORT;
typedef signed short SHORT;
typedef unsigned short WORD, *PWORD, *LPWORD;
typedef int LONG;

typedef size_t SIZE_T;

typedef union tagCY {
    struct {
#if BIGENDIAN
        LONG    Hi;
        LONG   Lo;
#else
        LONG   Lo;
        LONG    Hi;
#endif
    };
    LONG64 int64;
} CY, *LPCY;

typedef CY CURRENCY;

typedef struct tagDEC {
    // Decimal.cs treats the first two shorts as one long
    // And they seriable the data so we need to little endian
    // seriliazation
    // The wReserved overlaps with Variant's vt member
#if BIGENDIAN
    union {
        struct {
            BYTE sign;
            BYTE scale;
        };
        USHORT signscale;
    };
    USHORT wReserved;
#else
    USHORT wReserved;
    union {
        struct {
            BYTE scale;
            BYTE sign;
        };
        USHORT signscale;
    };
#endif
    LONG Hi32;
    union {
        struct {
            LONG Lo32;
            LONG Mid32;
        };
        ULONG64 Lo64;
    };
} DECIMAL, *LPDECIMAL;


#ifndef TRUE
#define TRUE 1
#endif

#ifndef FALSE
#define FALSE 0
#endif

#endif //_INTEROP_TYPES__H
