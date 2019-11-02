// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _H_INTEROPCONVERTER_
#define _H_INTEROPCONVERTER_

#include "debugmacros.h"


struct ItfMarshalInfo
{
    enum ItfMarshalFlags
    {
        ITF_MARSHAL_INSP_ITF        = 0x01,  // IInspectable-based interface
        ITF_MARSHAL_SUPPRESS_ADDREF = 0x02,
        ITF_MARSHAL_CLASS_IS_HINT   = 0x04,
        ITF_MARSHAL_DISP_ITF        = 0x08,
        ITF_MARSHAL_USE_BASIC_ITF   = 0x10,
        ITF_MARSHAL_WINRT_SCENARIO  = 0x20,  // WinRT scenario only
    };

    TypeHandle      thClass;
    TypeHandle      thItf;
    TypeHandle      thNativeItf;
    DWORD           dwFlags;
};

/*
    enum CreationFlags  // member of RCW struct
    {
        CF_None                 = 0x00,
        CF_SupportsIInspectable = 0x01, // the underlying object supports IInspectable
        CF_SuppressAddRef       = 0x02, // do not AddRef the underlying interface pointer
        CF_IsWeakReference      = 0x04, // mark the RCW as "weak"
        CF_NeedUniqueObject     = 0x08, // always create a new RCW/object even if we have one cached already
        CF_DontResolveClass     = 0x10, // don't attempt to create a strongly typed RCW
    };
*/


/*
01 REQUIRE_IINSPECTABLE         01 ITF_MARSHAL_INSP_ITF         01 CF_SupportsIInspectable
02 SUPPRESS_ADDREF              02 ITF_MARSHAL_SUPPRESS_ADDREF
                                                                04 CF_IsWeakReference
04 CLASS_IS_HINT                04 ITF_MARSHAL_CLASS_IS_HINT
08 UNIQUE_OBJECT                                                08 CF_NeedUniqueObject
                                08 ITF_MARSHAL_DISP_ITF
10 IGNORE_WINRT_AND_SKIP_UNBOXING                               10 CF_DontResolveClass
                                10 ITF_MARSHAL_USE_BASIC_ITF
                                20 ITF_MARSHAL_WINRT_SCENARIO
*/

struct ObjFromComIP
{
    enum flags
    {
        NONE                            = 0x00,
        REQUIRE_IINSPECTABLE            = 0x01, // ITF_MARSHAL_INSP_ITF        = 0x01   // CF_SupportsIInspectable  = 0x01
        SUPPRESS_ADDREF                 = 0x02, // ITF_MARSHAL_SUPPRESS_ADDREF = 0x02   // CF_SuppressAddRef        = 0x02
        CLASS_IS_HINT                   = 0x04, // ITF_MARSHAL_CLASS_IS_HINT   = 0x04
        UNIQUE_OBJECT                   = 0x08,                                         // CF_NeedUniqueObject      = 0x04
        IGNORE_WINRT_AND_SKIP_UNBOXING  = 0x10,                                         // CF_DontResolveClass      = 0x10
    };

    static flags FromItfMarshalInfoFlags(DWORD dwFlags)
    {
        static_assert_no_msg(((DWORD)CLASS_IS_HINT)         == ((DWORD)ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT));
        static_assert_no_msg(((DWORD)REQUIRE_IINSPECTABLE)  == ((DWORD)ItfMarshalInfo::ITF_MARSHAL_INSP_ITF));
        static_assert_no_msg(((DWORD)SUPPRESS_ADDREF)       == ((DWORD)ItfMarshalInfo::ITF_MARSHAL_SUPPRESS_ADDREF));

        DWORD dwResult = (dwFlags &
                            (ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT|
                             ItfMarshalInfo::ITF_MARSHAL_INSP_ITF|
                             ItfMarshalInfo::ITF_MARSHAL_SUPPRESS_ADDREF));
        return (flags)dwResult;
    }
};

inline ObjFromComIP::flags operator|(ObjFromComIP::flags lhs, ObjFromComIP::flags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<ObjFromComIP::flags>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
}
inline ObjFromComIP::flags operator|=(ObjFromComIP::flags & lhs, ObjFromComIP::flags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<ObjFromComIP::flags>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
    return lhs;
}


//
// THE FOLLOWING ARE THE MAIN APIS USED BY EVERYONE TO CONVERT BETWEEN
// OBJECTREFs AND COM IPs

#ifdef FEATURE_COMINTEROP

//--------------------------------------------------------------------------------
// The type of COM IP to convert the OBJECTREF to.
enum ComIpType
{
    ComIpType_None          = 0x0,
    ComIpType_Unknown       = 0x1,
    ComIpType_Dispatch      = 0x2,
    ComIpType_Both          = 0x3,
    ComIpType_OuterUnknown  = 0x5,
    ComIpType_Inspectable   = 0x8,
};


//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, ...);
// Convert ObjectRef to a COM IP, based on MethodTable* pMT.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, BOOL bSecurityCheck = TRUE, BOOL bEnableCustomizedQueryInterface = TRUE);


//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT);
// Convert ObjectRef to a COM IP of the requested type.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref,
    ComIpType ReqIpType = ComIpType_Unknown, ComIpType *pFetchedIpType = NULL);


//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, REFIID iid);
// Convert ObjectRef to a COM IP, based on riid.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, REFIID iid, bool throwIfNoComIP = true);


//--------------------------------------------------------------------------------
// GetObjectRefFromComIP(IUnknown **ppUnk, MethodTable *pMTClass, ...)
// Converts a COM IP to ObjectRef, pMTClass is the desired RCW type. If bSuppressAddRef and a new RCW is created,
// *ppUnk will be assigned NULL to signal to the caller that it is no longer responsible for releasing the IP.
void GetObjectRefFromComIP(OBJECTREF* pObjOut, IUnknown **ppUnk, MethodTable *pMTClass, MethodTable *pItfMT, DWORD dwFlags); // ObjFromComIP::flags

//--------------------------------------------------------------------------------
// GetObjectRefFromComIP(IUnknown *pUnk, MethodTable *pMTClass, ...)
// Converts a COM IP to ObjectRef, pMTClass is the desired RCW type.
inline void GetObjectRefFromComIP(OBJECTREF* pObjOut, IUnknown *pUnk, MethodTable *pMTClass = NULL, MethodTable *pItfMT = NULL, DWORD dwFlags = ObjFromComIP::NONE)
{
    WRAPPER_NO_CONTRACT;
    return GetObjectRefFromComIP(pObjOut, &pUnk, pMTClass, pItfMT, dwFlags);
}

#endif // FEATURE_COMINTEROP

#endif // #ifndef _H_INTEROPCONVERTER_
