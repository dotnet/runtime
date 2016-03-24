// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Contains some definitions duplicated from pal.h, palrt.h, rpc.h, 
// etc. because they have various conflicits with the linux standard
// runtime h files like wchar_t, memcpy, etc.

#include <../../../pal/inc/pal_mstypes.h>

#define S_OK                             (HRESULT)0x00000000
#define S_FALSE                          (HRESULT)0x00000001
#define E_NOTIMPL                        (HRESULT)0x80004001
#define E_FAIL                           (HRESULT)0x80004005
#define E_INVALIDARG                     (HRESULT)0x80070057
#define E_NOINTERFACE                    (HRESULT)0x80004002

#define MAX_PATH                         260 

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

#if defined(_MSC_VER) || defined(__llvm__)
#define DECLSPEC_ALIGN(x)   __declspec(align(x))
#else
#define DECLSPEC_ALIGN(x) 
#endif

#define interface struct
#define DECLSPEC_UUID(x)    __declspec(uuid(x))
#define DECLSPEC_NOVTABLE
#define MIDL_INTERFACE(x)   struct DECLSPEC_UUID(x) DECLSPEC_NOVTABLE

#ifdef __cplusplus
#define REFGUID const GUID &
#else
#define REFGUID const GUID *
#endif

#ifdef __cplusplus
extern "C++" {
#include "string.h"
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
#define IID_NULL GUID_NULL
#define IsEqualIID(riid1, riid2) IsEqualGUID(riid1, riid2)

MIDL_INTERFACE("00000000-0000-0000-C000-000000000046")
IUnknown
{
public:
    virtual HRESULT QueryInterface( 
        REFIID riid,
        void **ppvObject) = 0;
        
    virtual ULONG AddRef( void) = 0;
        
    virtual ULONG Release( void) = 0;
};

EXTERN_C
inline
LONG
InterlockedIncrement(
    LONG volatile *lpAddend)
{
    return __sync_add_and_fetch(lpAddend, (LONG)1);
}

EXTERN_C
inline
LONG
InterlockedDecrement(
    LONG volatile *lpAddend)
{
    return __sync_sub_and_fetch(lpAddend, (LONG)1);
}