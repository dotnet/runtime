// Copyright 2022 Aaron R Robinson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#ifndef MINIPAL_COM_COMTYPES_H
#define MINIPAL_COM_COMTYPES_H

// Perform platform check
#ifdef _MSC_VER
    #define DNCP_WINDOWS
#endif

// Typedefs typically provided by Windows' headers
#ifdef MINIPAL_COM_TYPEDEFS
    typedef void* PVOID;
    typedef void* LPVOID;
    typedef void const* LPCVOID;
    typedef uintptr_t UINT_PTR;
    typedef size_t SIZE_T;

    typedef uint8_t BYTE;
    typedef char CHAR;
    typedef int16_t SHORT;
    typedef uint16_t USHORT;
    typedef int32_t INT;
    typedef int32_t INT32;
    typedef uint32_t UINT;
    typedef int32_t LONG;
    typedef uint32_t ULONG;
    typedef uint32_t ULONG32;
    typedef uint32_t DWORD;
    typedef int64_t LONGLONG;
    typedef uint64_t UINT64;
    typedef uint64_t ULONG64;
    typedef uint64_t ULONGLONG;
    typedef float FLOAT;
    typedef double DOUBLE;
    typedef int32_t SCODE;
    typedef int32_t DATE;

#ifdef DNCP_WINDOWS
    typedef wchar_t WCHAR;
#elif defined(__cplusplus)
    typedef char16_t WCHAR;
#else
    typedef uint16_t WCHAR;
#endif // __cplusplus

    typedef WCHAR* LPWSTR;
    typedef WCHAR const* LPCWSTR;

    typedef WCHAR OLECHAR;
    typedef OLECHAR* LPOLESTR;
    typedef OLECHAR const* LPCOLESTR;
    typedef OLECHAR* BSTR;

    typedef int32_t BOOL;
    #define TRUE ((BOOL)1)
    #define FALSE ((BOOL)0)

    typedef int16_t VARIANT_BOOL;
    #define VARIANT_TRUE ((VARIANT_BOOL)-1)
    #define VARIANT_FALSE ((VARIANT_BOOL)0)

    typedef unsigned short VARTYPE;

    typedef int32_t HRESULT;
    typedef void* HANDLE;

    typedef struct
    {
        uint32_t  Data1;
        uint16_t  Data2;
        uint16_t  Data3;
        uint8_t   Data4[8];
    } GUID;

    typedef GUID IID;

    // 00000000-0000-0000-0000-000000000000
    extern IID const GUID_NULL;

    typedef union {
        struct {
#ifdef DNCP_BIG_ENDIAN
            LONG HighPart;
            DWORD LowPart;
#else
            DWORD LowPart;
            LONG HighPart;
#endif
        } u;
        LONGLONG QuadPart;
    } LARGE_INTEGER;

    typedef union {
        struct {
#ifdef DNCP_BIG_ENDIAN
            DWORD HighPart;
            DWORD LowPart;
#else
            DWORD LowPart;
            DWORD HighPart;
#endif
        } u;
        ULONGLONG QuadPart;
    } ULARGE_INTEGER;
#endif // MINIPAL_COM_TYPEDEFS

//
// Windows headers
//

#ifdef MINIPAL_COM_WINHDRS
    #include <winerror.h>

    #if defined(__cplusplus)

        #define EXTERN_C extern "C"

        using REFGUID = GUID const&;
        using IID = GUID;
        using REFIID = IID const&;
        using CLSID = GUID;
        using REFCLSID = CLSID const&;

        // The DNCP_DEFINE_GUID should only be set in a compilation unit
        // to avoid duplicate symbol problems during linking.
        #if defined(DNCP_DEFINE_GUID)
            #define EXTERN_GUID(itf,l1,s1,s2,c1,c2,c3,c4,c5,c6,c7,c8) \
                EXTERN_C constexpr IID itf = {l1,s1,s2,{c1,c2,c3,c4,c5,c6,c7,c8}}
        #else
            #define EXTERN_GUID(itf,l1,s1,s2,c1,c2,c3,c4,c5,c6,c7,c8) \
                EXTERN_C const IID itf
        #endif // !DNCP_DEFINE_GUID

        // sal
        #define _In_
        #define _In_z_
        #define _In_opt_
        #define _In_reads_bytes_(x)
        #define _Inout_
        #define _Out_
        #define _Out_opt_
        #define _Out_writes_to_opt_(x,y)
        #define _Out_writes_bytes_to_(x, y)
        #define _Out_writes_to_(x,y)
        #define _COM_Outptr_
        #define __RPC_FAR
        #define __RPC_USER
        #define __RPC__in
        #define __RPC__in_xcount(x)
        #define __RPC__in_ecount_full(x)
        #define __RPC__in_opt
        #define __RPC__inout
        #define __RPC__inout_xcount(x)
        #define __RPC__out
        #define __RPC__out_ecount_part(x,y)
        #define __RPC__deref_out_ecount_full_opt(x)
        #define __RPC__deref_out_opt
        #define __RPC__out

        // COM Interface definitions
        #define __uuidof(type) IID_##type
        #define interface struct
        #define DECLSPEC_UUID(x)
        #define DECLSPEC_NOVTABLE
        #define MIDL_INTERFACE(x)                       struct DECLSPEC_UUID(x) DECLSPEC_NOVTABLE
        #define DECLARE_INTERFACE_(iface, baseiface)    interface DECLSPEC_NOVTABLE iface : public baseiface
        #define STDMETHODCALLTYPE
        #define STDMETHOD(method)       virtual HRESULT STDMETHODCALLTYPE method
        #define STDMETHOD_(type,method) virtual type STDMETHODCALLTYPE method

        #define UNALIGNED
        #define PURE = 0

        #define UNREFERENCED_PARAMETER(p) (void)p

        #include <unknwn.h>

        // Unusable COM and RPC types
        struct tagSTATSTG;
        using STATSTG = tagSTATSTG;
        interface ITypeInfo;
        interface IDispatch;
        interface IRpcChannelBuffer;
        interface IRecordInfo;
        using RPC_IF_HANDLE = void*;
        struct SAFEARRAY;

        // Unusable Win32 types
        using LPDEBUG_EVENT = SIZE_T;
        using LPSTARTUPINFOW = SIZE_T;
        using LPPROCESS_INFORMATION = SIZE_T;
        using LPSECURITY_ATTRIBUTES = SIZE_T;

        // Other COM interfaces
        #include <objidl.h>

        // OLE VARIANT types
        typedef struct {
            struct {
                ULONG Lo;
                LONG Hi;
            } DUMMYSTRUCTNAME;
            LONGLONG int64;
        } CY;
        typedef struct {
            USHORT wReserved;
            union {
                struct {
                    BYTE scale;
                    BYTE sign;
                } DUMMYSTRUCTNAME;
                USHORT signscale;
            } DUMMYUNIONNAME;
            ULONG Hi32;
            union {
                struct {
                    ULONG Lo32;
                    ULONG Mid32;
                } DUMMYSTRUCTNAME2;
                ULONGLONG Lo64;
            } DUMMYUNIONNAME2;
        } DECIMAL;

        enum VARENUM
        {
            VT_EMPTY = 0,
            VT_NULL = 1,
            VT_I2 = 2,
            VT_I4 = 3,
            VT_R4 = 4,
            VT_R8 = 5,
            VT_CY = 6,
            VT_DATE = 7,
            VT_BSTR = 8,
            VT_DISPATCH = 9,
            VT_ERROR = 10,
            VT_BOOL = 11,
            VT_VARIANT = 12,
            VT_UNKNOWN = 13,
            VT_DECIMAL = 14,
            VT_I1 = 16,
            VT_UI1 = 17,
            VT_UI2 = 18,
            VT_UI4 = 19,
            VT_I8 = 20,
            VT_UI8 = 21,
            VT_INT = 22,
            VT_UINT = 23,
            VT_VOID = 24,
            VT_HRESULT = 25,
            VT_PTR = 26,
            VT_SAFEARRAY = 27,
            VT_CARRAY = 28,
            VT_USERDEFINED = 29,
            VT_LPSTR = 30,
            VT_LPWSTR = 31,
            VT_RECORD = 36,
            VT_INT_PTR = 37,
            VT_UINT_PTR = 38,
            VT_FILETIME = 64,
            VT_BLOB = 65,
            VT_STREAM = 66,
            VT_STORAGE = 67,
            VT_STREAMED_OBJECT = 68,
            VT_STORED_OBJECT = 69,
            VT_BLOB_OBJECT = 70,
            VT_CF = 71,
            VT_CLSID = 72,
            VT_VERSIONED_STREAM = 73,
            VT_BSTR_BLOB = 0xfff,
            VT_VECTOR = 0x1000,
            VT_ARRAY = 0x2000,
            VT_BYREF = 0x4000,
            VT_RESERVED = 0x8000,
            VT_ILLEGAL = 0xffff,
            VT_ILLEGALMASKED = 0xfff,
            VT_TYPEMASK = 0xfff
        };

        typedef struct tagVARIANT {
            union {
                struct __tagVARIANT {
                    VARTYPE vt;
                    uint16_t wReserved1;
                    uint16_t wReserved2;
                    uint16_t wReserved3;
                    union {
                        LONGLONG llVal;
                        LONG lVal;
                        BYTE bVal;
                        SHORT iVal;
                        FLOAT fltVal;
                        DOUBLE dblVal;
                        VARIANT_BOOL boolVal;
                        VARIANT_BOOL __OBSOLETE__VARIANT_BOOL;
                        SCODE scode;
                        CY cyVal;
                        DATE date;
                        BSTR bstrVal;
                        IUnknown *punkVal;
                        IDispatch *pdispVal;
                        SAFEARRAY *parray;
                        BYTE *pbVal;
                        SHORT *piVal;
                        LONG *plVal;
                        LONGLONG *pllVal;
                        FLOAT *pfltVal;
                        DOUBLE *pdblVal;
                        VARIANT_BOOL *pboolVal;
                        VARIANT_BOOL *__OBSOLETE__VARIANT_PBOOL;
                        SCODE *pscode;
                        CY *pcyVal;
                        DATE *pdate;
                        BSTR *pbstrVal;
                        IUnknown **ppunkVal;
                        IDispatch **ppdispVal;
                        SAFEARRAY **pparray;
                        struct tagVARIANT *pvarVal;
                        PVOID byref;
                        CHAR cVal;
                        USHORT uiVal;
                        ULONG ulVal;
                        ULONGLONG ullVal;
                        INT intVal;
                        UINT uintVal;
                        DECIMAL *pdecVal;
                        CHAR *pcVal;
                        USHORT *puiVal;
                        ULONG *pulVal;
                        ULONGLONG *pullVal;
                        INT *pintVal;
                        UINT *puintVal;
                        struct __tagBRECORD {
                            PVOID pvRecord;
                            IRecordInfo *pRecInfo;
                        } n4;
                    } n3;
                } n2;
                DECIMAL decVal;
            } n1;
        } VARIANT;

        #define V_UNION(X, Y)   ((X)->n1.n2.n3.Y)
        #define V_VT(X)         ((X)->n1.n2.vt)
        #define V_RECORDINFO(X) ((X)->n1.n2.n3.brecVal.pRecInfo)
        #define V_RECORD(X)     ((X)->n1.n2.n3.brecVal.pvRecord)
        #define V_DECIMAL(X)    ((X)->n1.decVal)

        // VARIANT access macros
        #define V_ISBYREF(X)     (V_VT(X)&VT_BYREF)
        #define V_ISARRAY(X)     (V_VT(X)&VT_ARRAY)
        #define V_ISVECTOR(X)    (V_VT(X)&VT_VECTOR)
        #define V_NONE(X)        V_I2(X)

        #define V_UI1(X)         V_UNION(X, bVal)
        #define V_UI1REF(X)      V_UNION(X, pbVal)
        #define V_I2(X)          V_UNION(X, iVal)
        #define V_I2REF(X)       V_UNION(X, piVal)
        #define V_I4(X)          V_UNION(X, lVal)
        #define V_I4REF(X)       V_UNION(X, plVal)
        #define V_I8(X)          V_UNION(X, llVal)
        #define V_I8REF(X)       V_UNION(X, pllVal)
        #define V_R4(X)          V_UNION(X, fltVal)
        #define V_R4REF(X)       V_UNION(X, pfltVal)
        #define V_R8(X)          V_UNION(X, dblVal)
        #define V_R8REF(X)       V_UNION(X, pdblVal)
        #define V_I1(X)          V_UNION(X, cVal)
        #define V_I1REF(X)       V_UNION(X, pcVal)
        #define V_UI2(X)         V_UNION(X, uiVal)
        #define V_UI2REF(X)      V_UNION(X, puiVal)
        #define V_UI4(X)         V_UNION(X, ulVal)
        #define V_UI4REF(X)      V_UNION(X, pulVal)
        #define V_UI8(X)         V_UNION(X, ullVal)
        #define V_UI8REF(X)      V_UNION(X, pullVal)
        #define V_INT(X)         V_UNION(X, intVal)
        #define V_INTREF(X)      V_UNION(X, pintVal)
        #define V_UINT(X)        V_UNION(X, uintVal)
        #define V_UINTREF(X)     V_UNION(X, puintVal)

        #if INTPTR_MAX == INT64_MAX
            #define V_INT_PTR(X)        V_UNION(X, llVal)
            #define V_UINT_PTR(X)       V_UNION(X, ullVal)
            #define V_INT_PTRREF(X)     V_UNION(X, pllVal)
            #define V_UINT_PTRREF(X)    V_UNION(X, pullVal)
        #elif INTPTR_MAX == INT32_MAX
            #define V_INT_PTR(X)        V_UNION(X, lVal)
            #define V_UINT_PTR(X)       V_UNION(X, ulVal)
            #define V_INT_PTRREF(X)     V_UNION(X, plVal)
            #define V_UINT_PTRREF(X)    V_UNION(X, pulVal)
        #else
            #error "Unknown pointer size"
        #endif

        #define V_CY(X)          V_UNION(X, cyVal)
        #define V_CYREF(X)       V_UNION(X, pcyVal)
        #define V_DATE(X)        V_UNION(X, date)
        #define V_DATEREF(X)     V_UNION(X, pdate)
        #define V_BSTR(X)        V_UNION(X, bstrVal)
        #define V_BSTRREF(X)     V_UNION(X, pbstrVal)
        #define V_DISPATCH(X)    V_UNION(X, pdispVal)
        #define V_DISPATCHREF(X) V_UNION(X, ppdispVal)
        #define V_ERROR(X)       V_UNION(X, scode)
        #define V_ERRORREF(X)    V_UNION(X, pscode)
        #define V_BOOL(X)        V_UNION(X, boolVal)
        #define V_BOOLREF(X)     V_UNION(X, pboolVal)
        #define V_UNKNOWN(X)     V_UNION(X, punkVal)
        #define V_UNKNOWNREF(X)  V_UNION(X, ppunkVal)
        #define V_VARIANTREF(X)  V_UNION(X, pvarVal)
        #define V_ARRAY(X)       V_UNION(X, parray)
        #define V_ARRAYREF(X)    V_UNION(X, pparray)
        #define V_BYREF(X)       V_UNION(X, byref)

        #define V_DECIMALREF(X)  V_UNION(X, pdecVal)
    #endif // __cplusplus
#endif // DNCP_INTERFACES

#endif // MINIPAL_COM_COMTYPES_H
