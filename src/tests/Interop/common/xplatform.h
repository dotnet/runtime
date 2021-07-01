// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __XPLAT_H__
#define __XPLAT_H__

#include <platformdefines.h>

#ifndef WINDOWS

#include <atomic>
#define __RPC_FAR
#define DECLSPEC_UUID(x)
#define DECLSPEC_NOVTABLE
#define MIDL_INTERFACE(x)   struct DECLSPEC_UUID(x) DECLSPEC_NOVTABLE
#define interface struct
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


#define E_POINTER                        _HRESULT_TYPEDEF_(0x80004003L)
#define E_NOINTERFACE                    _HRESULT_TYPEDEF_(0x80004002L)
#define S_FALSE                          _HRESULT_TYPEDEF_(0x00000001L)
#define E_OUTOFMEMORY                    _HRESULT_TYPEDEF_(0x8007000EL)
#define E_NOTIMPL   0x80004001

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
#else
#define REFGUID const GUID *
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
#else
#define REFIID const IID *
#endif

#define IID_NULL { 0x00000000, 0x0000, 0x0000, { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}

#define IsEqualIID(riid1, riid2) IsEqualGUID(riid1, riid2)

// Common macro for working in COM
#define RETURN_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { return hr; } }

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

// Implementation of IUnknown operations
class UnknownImpl
{
public:
    UnknownImpl() : _refCount{ 1 } {};
    virtual ~UnknownImpl() = default;

    UnknownImpl(const UnknownImpl&) = delete;
    UnknownImpl& operator=(const UnknownImpl&) = delete;

    UnknownImpl(UnknownImpl&&)  : _refCount{ 1 } {};
    UnknownImpl& operator=(UnknownImpl&&) = delete;

    template<typename I1, typename ...IR>
    HRESULT DoQueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void **ppvObject,
        /* [in] */ I1 i1,
        /* [in] */ IR... remain)
    {
        if (ppvObject == nullptr)
            return E_POINTER;

        if (riid == __uuidof(IUnknown))
        {
            *ppvObject = static_cast<IUnknown *>(i1);
        }
        else
        {
            //Temporary commented: to be managed with templates (_uuidof(T) needed) or with 
            // dedicated if-logic over well-know types
            //HRESULT hr = Internal::__QueryInterfaceImpl(riid, ppvObject, i1, remain...);
            //if (hr != S_OK)
            //    return hr;
        }

        DoAddRef();
        return S_OK;
    }

    ULONG DoAddRef()
    {
        assert(_refCount > 0);
        return (++_refCount);
    }

    ULONG DoRelease()
    {
        assert(_refCount > 0);
        ULONG c = (--_refCount);
        if (c == 0)
            delete this;
        return c;
    }

protected:
    ULONG GetRefCount()
    {
        return _refCount;
    }

private:
    //std::atomic<ULONG> _refCount = 1;
    //Initizialization moved ad constructor level
    std::atomic<ULONG> _refCount;
};


// Macro to use for defining ref counting impls
#define DEFINE_REF_COUNTING() \
    STDMETHOD_(ULONG, AddRef)(void) { return UnknownImpl::DoAddRef(); } \
    STDMETHOD_(ULONG, Release)(void) { return UnknownImpl::DoRelease(); }

template<typename T>
struct ComSmartPtr
{
    T* p;

    ComSmartPtr()
        : p{ nullptr }
    { }

    ComSmartPtr(_In_ T* t)
        : p{ t }
    {
        if (p != nullptr)
            (void)p->AddRef();
    }

    ComSmartPtr(_In_ const ComSmartPtr&) = delete;

    ComSmartPtr(_Inout_ ComSmartPtr&& other)
        : p{ other.Detach() }
    { }

    ~ComSmartPtr()
    {
        Release();
    }

    ComSmartPtr& operator=(_In_ const ComSmartPtr&) = delete;

    ComSmartPtr& operator=(_Inout_ ComSmartPtr&& other)
    {
        Attach(other.Detach());
        return (*this);
    }

    operator T*()
    {
        return p;
    }

    T** operator&()
    {
        return &p;
    }

    T* operator->()
    {
        return p;
    }

    void Attach(_In_opt_ T* t) noexcept
    {
        Release();
        p = t;
    }

    T* Detach() noexcept
    {
        T* tmp = p;
        p = nullptr;
        return tmp;
    }

    void Release() noexcept
    {
        if (p != nullptr)
        {
            (void)p->Release();
            p = nullptr;
        }
    }
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
