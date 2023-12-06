// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __XPLAT_H__
#define __XPLAT_H__

#include <platformdefines.h>

#ifndef WINDOWS

#define DECLSPEC_UUID(x)
#define DECLSPEC_NOVTABLE
#define MIDL_INTERFACE(x)   struct DECLSPEC_UUID(x) DECLSPEC_NOVTABLE

#define STDMETHOD(method)       virtual HRESULT STDMETHODCALLTYPE method
#define STDMETHOD_(type,method) virtual type STDMETHODCALLTYPE method

// SAL
#undef _In_
#define _In_
#undef _Outptr_
#define _Outptr_
#undef _Out_
#define _Out_
#undef _In_opt_
#define _In_opt_
#undef _Inout_
#define _Inout_

// HRESULT values
#define E_POINTER       _HRESULT_TYPEDEF_(0x80004003L)
#define E_NOINTERFACE   _HRESULT_TYPEDEF_(0x80004002L)
#define S_FALSE         _HRESULT_TYPEDEF_(0x00000001L)
#define E_OUTOFMEMORY   _HRESULT_TYPEDEF_(0x8007000EL)
#define E_NOTIMPL       _HRESULT_TYPEDEF_(0x80004001L)
#define E_UNEXPECTED    _HRESULT_TYPEDEF_(0x8000FFFFL)

// Declaring a handle dummy struct for HSTRING the same way DECLARE_HANDLE does.
typedef struct HSTRING__{
    int unused;
} HSTRING__;

// Declare the HSTRING handle for C/C++
typedef HSTRING__* HSTRING;

#ifndef GUID_DEFINED
typedef struct _GUID {
    uint32_t    Data1;    // NOTE: diff from Win32, for LP64
    uint16_t    Data2;
    uint16_t    Data3;
    uint8_t     Data4[8];
} GUID;
typedef const GUID *LPCGUID;
#define GUID_DEFINED
#endif // !GUID_DEFINED

#define REFGUID const GUID &

extern "C++" {
#if !defined _SYS_GUID_OPERATOR_EQ_ && !defined _NO_SYS_GUID_OPERATOR_EQ_
#define _SYS_GUID_OPERATOR_EQ_
inline int IsEqualGUID(REFGUID rguid1, REFGUID rguid2)
    { return !memcmp(&rguid1, &rguid2, sizeof(GUID)); }
inline int operator==(REFGUID guidOne, REFGUID guidOther)
    { return IsEqualGUID(guidOne,guidOther); }
inline int operator!=(REFGUID guidOne, REFGUID guidOther)
    { return !IsEqualGUID(guidOne,guidOther); }
#endif
};

typedef GUID IID;
#define REFIID const IID &
#define IsEqualIID(riid1, riid2) IsEqualGUID(riid1, riid2)

#define __uuidof(type)      IID_##type

#include <stddef.h>

#undef INT_MIN
#define INT_MIN	   (-2147483647 - 1)

typedef ptrdiff_t INT_PTR;
typedef size_t UINT_PTR;

typedef double DOUBLE;
typedef float FLOAT;
typedef char CHAR;

typedef size_t SIZE_T;

typedef union tagCY {
    struct {
#if BIGENDIAN
        int32_t Hi;
        int32_t Lo;
#else
        int32_t Lo;
        int32_t Hi;
#endif
    };
    int64_t int64;
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
            uint8_t sign;
            uint8_t scale;
        };
        uint16_t signscale;
    };
    uint16_t wReserved;
#else
    uint16_t wReserved;
    union {
        struct {
            uint8_t scale;
            uint8_t sign;
        };
        uint16_t signscale;
    };
#endif
    int32_t Hi32;
    union {
        struct {
            int32_t Lo32;
            int32_t Mid32;
        };
        uint64_t Lo64;
    };
} DECIMAL, *LPDECIMAL;


#ifndef TRUE
#define TRUE 1
#endif

#ifndef FALSE
#define FALSE 0
#endif

typedef uint16_t VARIANT_BOOL;

#ifndef VARIANT_TRUE
#define VARIANT_TRUE -1
#endif

#ifndef VARIANT_FALSE
#define VARIANT_FALSE 0
#endif

#ifndef __IUnknown_INTERFACE_DEFINED__
#define __IUnknown_INTERFACE_DEFINED__

// 00000000-0000-0000-C000-000000000046
const IID IID_IUnknown = { 0x00000000, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000000-0000-0000-C000-000000000046")
IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void **ppvObject) = 0;

    virtual uint32_t STDMETHODCALLTYPE AddRef(void) = 0;

    virtual uint32_t STDMETHODCALLTYPE Release(void) = 0;
};

#endif // __IUnknown_INTERFACE_DEFINED__

typedef /* [v1_enum] */
enum TrustLevel
{
    BaseTrust	= 0,
    PartialTrust	= ( BaseTrust + 1 ) ,
    FullTrust	= ( PartialTrust + 1 )
} 	TrustLevel;

// AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90
const IID IID_IInspectable = { 0xaf86e2e0, 0xb12d, 0x4c6a, { 0x9c, 0x5a, 0xd7, 0xaa, 0x65, 0x10, 0x1e, 0x90} };

MIDL_INTERFACE("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")
IInspectable : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetIids(
        /* [out] */ uint32_t * iidCount,
        /* [size_is][size_is][out] */ IID * *iids) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetRuntimeClassName(
        /* [out] */ HSTRING * className) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetTrustLevel(
        /* [out] */ TrustLevel * trustLevel) = 0;
};

// 00000037-0000-0000-C000-000000000046
const IID IID_IWeakReference = { 0x00000037, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000037-0000-0000-C000-000000000046")
IWeakReference : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE Resolve(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ IInspectable **objectReference) = 0;
};

// 00000038-0000-0000-C000-000000000046
const IID IID_IWeakReferenceSource = { 0x00000038, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000038-0000-0000-C000-000000000046")
IWeakReferenceSource : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetWeakReference(
        /* [retval][out] */ IWeakReference * *weakReference) = 0;
};

#define DECIMAL_NEG ((uint8_t)0x80)
#define DECIMAL_SCALE(dec)       ((dec).u.u.scale)
#define DECIMAL_SIGN(dec)        ((dec).u.u.sign)
#define DECIMAL_SIGNSCALE(dec)   ((dec).u.signscale)
#define DECIMAL_LO32(dec)        ((dec).v.v.Lo32)
#define DECIMAL_MID32(dec)       ((dec).v.v.Mid32)
#define DECIMAL_HI32(dec)        ((dec).Hi32)
#define DECIMAL_LO64_GET(dec)    ((dec).v.Lo64)
#define DECIMAL_LO64_SET(dec,value)   {(dec).v.Lo64 = value; }

#define DECIMAL_SETZERO(dec) {DECIMAL_LO32(dec) = 0; DECIMAL_MID32(dec) = 0; DECIMAL_HI32(dec) = 0; DECIMAL_SIGNSCALE(dec) = 0;}

#endif // !WINDOWS

#endif // __XPLAT_H__
