// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __XPLAT_H__
#define __XPLAT_H__

#include <platformdefines.h>

#ifndef WINDOWS

#include <stddef.h>

#undef INT_MIN
#define INT_MIN	   (-2147483647 - 1)

typedef ptrdiff_t INT_PTR;
typedef size_t UINT_PTR;

typedef unsigned long long ULONG64;
typedef long long LONG64;
typedef double DOUBLE;
typedef float FLOAT;
typedef int INT, *LPINT;
typedef unsigned int UINT;
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

class IUnknown
{
public:
    virtual int QueryInterface(void* riid,void** ppvObject) = 0;
    virtual unsigned long AddRef() = 0;
    virtual unsigned long Release() = 0;
};

#define DECIMAL_NEG ((BYTE)0x80)
#define DECIMAL_SCALE(dec)       ((dec).u.u.scale)
#define DECIMAL_SIGN(dec)        ((dec).u.u.sign)
#define DECIMAL_SIGNSCALE(dec)   ((dec).u.signscale)
#define DECIMAL_LO32(dec)        ((dec).v.v.Lo32)
#define DECIMAL_MID32(dec)       ((dec).v.v.Mid32)
#define DECIMAL_HI32(dec)        ((dec).Hi32)
#define DECIMAL_LO64_GET(dec)    ((dec).v.Lo64)
#define DECIMAL_LO64_SET(dec,value)   {(dec).v.Lo64 = value; }

#define DECIMAL_SETZERO(dec) {DECIMAL_LO32(dec) = 0; DECIMAL_MID32(dec) = 0; DECIMAL_HI32(dec) = 0; DECIMAL_SIGNSCALE(dec) = 0;}

#endif //!_Win32

#endif // __XPLAT_H__
