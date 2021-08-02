// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __XPLAT_H__
#define __XPLAT_H__

#include <platformdefines.h>

#ifndef WINDOWS

#define __RPC_FAR
#define DECLSPEC_UUID(x)
#define DECLSPEC_NOVTABLE
#define MIDL_INTERFACE(x)   struct DECLSPEC_UUID(x) DECLSPEC_NOVTABLE
//Check OBJC_TESTS presence to avoid interface definition on OSX (already defined)
#ifndef OBJC_TESTS
#define interface struct
#endif
#define STDMETHOD(method)       virtual HRESULT STDMETHODCALLTYPE method
#define STDMETHOD_(type,method) virtual type STDMETHODCALLTYPE method
#undef _In_
#define _In_
#undef _Outptr_
#define _Outptr_
#undef _Out_
#define _Out_
#undef _In_opt_
#define _In_opt_
#undef _COM_Outptr_
#define _COM_Outptr_
#undef _Inout_
#define _Inout_
#define __RPC__out
#define __RPC__in
#define __RPC__deref_out
#define __RPC__deref_out_opt
#define __RPC__deref_out_ecount_full_opt(x)
#define __RPC_unique_pointer


#define E_POINTER                        _HRESULT_TYPEDEF_(0x80004003L)
#define E_NOINTERFACE                    _HRESULT_TYPEDEF_(0x80004002L)
#define S_FALSE                          _HRESULT_TYPEDEF_(0x00000001L)
#define E_OUTOFMEMORY                    _HRESULT_TYPEDEF_(0x8007000EL)
#define E_NOTIMPL                        _HRESULT_TYPEDEF_(0x80004001L)

// Declaring a handle dummy struct for HSTRING the same way DECLARE_HANDLE does.
typedef struct HSTRING__{
    int unused;
} HSTRING__;

// Declare the HSTRING handle for C/C++
typedef __RPC_unique_pointer HSTRING__* HSTRING;

typedef unsigned __int64 UINT64, *PUINT64;
typedef unsigned short USHORT;
typedef USHORT *PUSHORT;
typedef unsigned char UCHAR;
typedef UCHAR *PUCHAR;

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


#ifdef __cplusplus
#define REFGUID const GUID &
#endif


#ifdef __cplusplus
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
#endif // __cplusplus


typedef GUID IID;
#ifdef __cplusplus
#define REFIID const IID &
#endif

#define IID_NULL { 0x00000000, 0x0000, 0x0000, { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}

#define IsEqualIID(riid1, riid2) IsEqualGUID(riid1, riid2)

#define __uuidof(type)      IID_##type

#ifndef assert
#define assert(e) ((void)0)
#endif  // assert

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

#ifndef __IUnknown_INTERFACE_DEFINED__
#define __IUnknown_INTERFACE_DEFINED__


//00000000-0000-0000-C000-000000000046
const IID IID_IUnknown = { 0x00000000, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000000-0000-0000-C000-000000000046")
IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void **ppvObject) = 0;

    virtual ULONG STDMETHODCALLTYPE AddRef( void) = 0;

    virtual ULONG STDMETHODCALLTYPE Release( void) = 0;

};

#endif // __IUnknown_INTERFACE_DEFINED__

struct IDispatch : public IUnknown
{

};

typedef /* [v1_enum] */ 
enum TrustLevel
    {
        BaseTrust	= 0,
        PartialTrust	= ( BaseTrust + 1 ) ,
        FullTrust	= ( PartialTrust + 1 ) 
    } 	TrustLevel;

//AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90
const IID IID_IInspectable = { 0xaf86e2e0, 0xb12d, 0x4c6a, { 0x9c, 0x5a, 0xd7, 0xaa, 0x65, 0x10, 0x1e, 0x90} };

MIDL_INTERFACE("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")
IInspectable : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetIids(
        /* [out] */ __RPC__out ULONG * iidCount,
        /* [size_is][size_is][out] */ __RPC__deref_out_ecount_full_opt(*iidCount) IID * *iids) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetRuntimeClassName(
        /* [out] */ __RPC__deref_out_opt HSTRING * className) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetTrustLevel(
        /* [out] */ __RPC__out TrustLevel * trustLevel) = 0;
};


//00000037-0000-0000-C000-000000000046
const IID IID_IWeakReference = { 0x00000037, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000037-0000-0000-C000-000000000046")
IWeakReference : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE Resolve(
        /* [in] */ __RPC__in REFIID riid,
        /* [iid_is][out] */ __RPC__deref_out IInspectable **objectReference) = 0;

};

//00000038-0000-0000-C000-000000000046
const IID IID_IWeakReferenceSource = { 0x00000038, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000038-0000-0000-C000-000000000046")
IWeakReferenceSource : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetWeakReference(
        /* [retval][out] */ __RPC__deref_out_opt IWeakReference * *weakReference) = 0;
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
