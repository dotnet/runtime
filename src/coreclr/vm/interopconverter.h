// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _H_INTEROPCONVERTER_
#define _H_INTEROPCONVERTER_

#include "debugmacros.h"


struct ItfMarshalInfo
{
    enum ItfMarshalFlags
    {
        // unused                   = 0x01,
        // unused                   = 0x02,
        ITF_MARSHAL_CLASS_IS_HINT   = 0x04,
        ITF_MARSHAL_DISP_ITF        = 0x08,
        ITF_MARSHAL_USE_BASIC_ITF   = 0x10,
        // unused                   = 0x20,
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
        // unused               = 0x01,
        // unused               = 0x02,
        // unused               = 0x04,
        CF_NeedUniqueObject     = 0x08, // always create a new RCW/object even if we have one cached already
        // unused               = 0x10,
    };
*/


/*
04 CLASS_IS_HINT                04 ITF_MARSHAL_CLASS_IS_HINT
08 UNIQUE_OBJECT                                                08 CF_NeedUniqueObject
                                08 ITF_MARSHAL_DISP_ITF
                                10 ITF_MARSHAL_USE_BASIC_ITF
*/

struct ObjFromComIP
{
    enum flags
    {
        NONE                            = 0x00,
        // unused                       = 0x01,
        // unused                       = 0x02,
        CLASS_IS_HINT                   = 0x04, // ITF_MARSHAL_CLASS_IS_HINT   = 0x04
        UNIQUE_OBJECT                   = 0x08,                                         // CF_NeedUniqueObject      = 0x04
        // unused                       = 0x10,
    };

    static flags FromItfMarshalInfoFlags(DWORD dwFlags)
    {
        static_assert_no_msg(((DWORD)CLASS_IS_HINT)         == ((DWORD)ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT));

        DWORD dwResult = (dwFlags &
                            (ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT));
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
};


//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, ...);
// Convert ObjectRef to a COM IP, based on MethodTable* pMT.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, BOOL bEnableCustomizedQueryInterface = TRUE);


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
void GetObjectRefFromComIP(OBJECTREF* pObjOut, IUnknown **ppUnk, MethodTable *pMTClass, DWORD dwFlags); // ObjFromComIP::flags

//--------------------------------------------------------------------------------
// GetObjectRefFromComIP(IUnknown *pUnk, MethodTable *pMTClass, ...)
// Converts a COM IP to ObjectRef, pMTClass is the desired RCW type.
inline void GetObjectRefFromComIP(OBJECTREF* pObjOut, IUnknown *pUnk, MethodTable *pMTClass = NULL, DWORD dwFlags = ObjFromComIP::NONE)
{
    WRAPPER_NO_CONTRACT;
    return GetObjectRefFromComIP(pObjOut, &pUnk, pMTClass, dwFlags);
}

#endif // FEATURE_COMINTEROP

#endif // #ifndef _H_INTEROPCONVERTER_
