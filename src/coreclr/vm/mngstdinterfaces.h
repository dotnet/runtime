// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  MngStdInterfaceMap.h
**
**
** Purpose: Contains types and method signatures for the Com wrapper class
**
**

===========================================================*/

#ifndef _MNGSTDINTERFACEMAP_H
#define _MNGSTDINTERFACEMAP_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "vars.hpp"
#include "eehash.h"
#include "class.h"
#include "mlinfo.h"

#ifndef DACCESS_COMPILE
//
// This class is used to establish a mapping between a managed standard interface and its
// unmanaged counterpart.
//

class MngStdInterfaceMap
{
public:
    // This method retrieves the native IID of the interface that the specified
    // managed type is a standard interface for. If the specified type is not
    // a standard interface then GUIDNULL is returned.
    inline static IID* GetNativeIIDForType(TypeHandle th)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            INJECT_FAULT(COMPlusThrowOM());
        }
        CONTRACTL_END

        // Only simple class types can have native IIDs
        if (th.IsTypeDesc() || th.IsArray())
            return NULL;

        HashDatum Data;

        // Retrieve the name of the type.
        LPCUTF8 ns, name;
        LPUTF8 strTypeName;
        name = th.GetMethodTable()->GetFullyQualifiedNameInfo(&ns);
        MAKE_FULL_PATH_ON_STACK_UTF8(strTypeName, ns, name);

        if (m_pMngStdItfMap == NULL) {
            MngStdInterfaceMap *tmp = new MngStdInterfaceMap;
            if (InterlockedCompareExchangeT(&m_pMngStdItfMap, tmp, NULL) != NULL) {
                tmp->m_TypeNameToNativeIIDMap.ClearHashTable();
                delete tmp;
            }
        }
        if (m_pMngStdItfMap->m_TypeNameToNativeIIDMap.GetValue(strTypeName, &Data) && (*((GUID*)Data) != GUID_NULL))
        {
            // The type is a standard interface.
            return (IID*)Data;
        }
        else
        {
            // The type is not a standard interface.
            return NULL;
        }
    }

private:
    // Disalow creation of this class by anybody outside of it.
    MngStdInterfaceMap();

    // The map of type names to native IID's.
    EEUtf8StringHashTable m_TypeNameToNativeIIDMap;

    // The one and only instance of the managed std interface map.
    static MngStdInterfaceMap *m_pMngStdItfMap;
};

#endif // DACCESS_COMPILE

//
// Base class for all the classes that contain the ECall's for the managed standard interfaces.
//

class MngStdItfBase
{
protected:
    static void InitHelper(
                    LPCUTF8 strMngItfTypeName,
                    LPCUTF8 strUComItfTypeName,
                    LPCUTF8 strCMTypeName,
                    LPCUTF8 strCookie,
                    LPCUTF8 strManagedViewName,
                    TypeHandle *pMngItfType,
                    TypeHandle *pUComItfType,
                    TypeHandle *pCustomMarshalerType,
                    TypeHandle *pManagedViewType,
                    OBJECTHANDLE *phndMarshaler);

    static LPVOID ForwardCallToManagedView(
                    OBJECTHANDLE hndMarshaler,
                    MethodDesc *pMngItfMD,
                    MethodDesc *pUComItfMD,
                    MethodDesc *pMarshalNativeToManagedMD,
                    MethodDesc *pMngViewMD,
                    IID *pMngItfIID,
                    IID *pNativeItfIID,
                    ARG_SLOT* pArgs);
};


//
// Define the enum of methods on the managed standard interface.
//

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
\
enum FriendlyName##Methods \
{  \
    FriendlyName##Methods_Dummy = -1,


#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl) \
    FriendlyName##Methods_##ECallMethName,


#define MNGSTDITF_END_INTERFACE(FriendlyName) \
    FriendlyName##Methods_LastMember \
}; \


#include "mngstditflist.h"


#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE


//
// Define the class that implements the ECall's for the managed standard interface.
//

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
\
class FriendlyName : public MngStdItfBase \
{ \
public: \
    FriendlyName() \
    { \
        CONTRACTL \
        {         \
            THROWS; \
            GC_TRIGGERS; \
            INJECT_FAULT(COMPlusThrowOM()); \
        } \
        CONTRACTL_END \
        InitHelper(strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, &m_MngItfType, &m_UComItfType, &m_CustomMarshalerType, &m_ManagedViewType, &m_hndCustomMarshaler); \
        m_NativeItfIID = NativeItfIID; \
        m_UComItfType.GetMethodTable()->GetGuid(&m_MngItfIID, TRUE); \
        memset(m_apCustomMarshalerMD, 0, CustomMarshalerMethods_LastMember * sizeof(MethodDesc *)); \
        memset(m_apManagedViewMD, 0, FriendlyName##Methods_LastMember * sizeof(MethodDesc *)); \
        memset(m_apUComItfMD, 0, FriendlyName##Methods_LastMember * sizeof(MethodDesc *)); \
        memset(m_apMngItfMD, 0, FriendlyName##Methods_LastMember * sizeof(MethodDesc *)); \
    } \
\
    OBJECTREF GetCustomMarshaler() \
    { \
        WRAPPER_NO_CONTRACT; \
        return ObjectFromHandle(m_hndCustomMarshaler); \
    } \
\
    MethodDesc* GetCustomMarshalerMD(EnumCustomMarshalerMethods Method) \
    { \
        CONTRACTL \
        {         \
            THROWS; \
            GC_TRIGGERS; \
            INJECT_FAULT(COMPlusThrowOM()); \
        } \
        CONTRACTL_END \
        MethodDesc *pMD = NULL; \
        \
        if (m_apCustomMarshalerMD[Method]) \
            return m_apCustomMarshalerMD[Method]; \
        \
        pMD = CustomMarshalerInfo::GetCustomMarshalerMD(Method, m_CustomMarshalerType); \
        _ASSERTE(pMD && "Unable to find specified method on the custom marshaler"); \
        MetaSig::EnsureSigValueTypesLoaded(pMD); \
        \
        m_apCustomMarshalerMD[Method] = pMD; \
        return pMD; \
    } \
\
    MethodDesc* GetManagedViewMD(FriendlyName##Methods Method, LPCUTF8 strMethName, LPHARDCODEDMETASIG pSig) \
    { \
        CONTRACTL \
        {         \
            THROWS; \
            GC_TRIGGERS; \
            INJECT_FAULT(COMPlusThrowOM()); \
        } \
        CONTRACTL_END \
        MethodDesc *pMD = NULL; \
        \
        if (m_apManagedViewMD[Method]) \
            return m_apManagedViewMD[Method]; \
        \
        pMD = MemberLoader::FindMethod(m_ManagedViewType.GetMethodTable(), strMethName, pSig); \
        _ASSERTE(pMD && "Unable to find specified method on the managed view"); \
        MetaSig::EnsureSigValueTypesLoaded(pMD); \
        \
        m_apManagedViewMD[Method] = pMD; \
        return pMD; \
    } \
\
    MethodDesc* GetUComItfMD(FriendlyName##Methods Method, LPCUTF8 strMethName, LPHARDCODEDMETASIG pSig) \
    { \
        CONTRACTL \
        {         \
            THROWS; \
            GC_TRIGGERS; \
            INJECT_FAULT(COMPlusThrowOM()); \
        } \
        CONTRACTL_END \
        MethodDesc *pMD = NULL; \
        \
        if (m_apUComItfMD[Method]) \
            return m_apUComItfMD[Method]; \
        \
        pMD = MemberLoader::FindMethod(m_UComItfType.GetMethodTable(), strMethName, pSig); \
        _ASSERTE(pMD && "Unable to find specified method in UCom interface"); \
        MetaSig::EnsureSigValueTypesLoaded(pMD); \
        \
        m_apUComItfMD[Method] = pMD; \
        return pMD; \
    } \
\
    MethodDesc* GetMngItfMD(FriendlyName##Methods Method, LPCUTF8 strMethName, LPHARDCODEDMETASIG pSig) \
    { \
        CONTRACTL \
        {         \
            THROWS; \
            GC_TRIGGERS; \
            INJECT_FAULT(COMPlusThrowOM()); \
        } \
        CONTRACTL_END \
        MethodDesc *pMD = NULL; \
        \
        if (m_apMngItfMD[Method]) \
            return m_apMngItfMD[Method]; \
        \
        pMD = MemberLoader::FindMethod(m_MngItfType.GetMethodTable(), strMethName, pSig); \
        _ASSERTE(pMD && "Unable to find specified method in UCom interface"); \
        MetaSig::EnsureSigValueTypesLoaded(pMD); \
        \
        m_apMngItfMD[Method] = pMD; \
        return pMD; \
    } \
\
private: \
    MethodDesc*     m_apCustomMarshalerMD[CustomMarshalerMethods_LastMember]; \
    MethodDesc*     m_apManagedViewMD[FriendlyName##Methods_LastMember]; \
    MethodDesc*     m_apUComItfMD[FriendlyName##Methods_LastMember]; \
    MethodDesc*     m_apMngItfMD[FriendlyName##Methods_LastMember]; \
    TypeHandle      m_CustomMarshalerType; \
    TypeHandle      m_ManagedViewType; \
    TypeHandle      m_UComItfType; \
    TypeHandle      m_MngItfType; \
    OBJECTHANDLE    m_hndCustomMarshaler; \
    GUID            m_MngItfIID; \
    GUID            m_NativeItfIID; \
\

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl) \
\
public: static LPVOID __stdcall ECallMethName##Worker(ARG_SLOT* pArgs); \
public: static FcallDecl; \
\

#define MNGSTDITF_END_INTERFACE(FriendlyName) \
}; \
\


#include "mngstditflist.h"


#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE


//
// App domain level information on the managed standard interfaces .
//

class MngStdInterfacesInfo
{
public:
    // Constructor and destructor.
    MngStdInterfacesInfo()
    {
        STATIC_CONTRACT_THROWS;
        STATIC_CONTRACT_FAULT;

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
\
        m_p##FriendlyName = 0; \
\

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl)
#define MNGSTDITF_END_INTERFACE(FriendlyName)


#include "mngstditflist.h"


#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE
    }

    ~MngStdInterfacesInfo()
    {
        WRAPPER_NO_CONTRACT;

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
\
        if (m_p##FriendlyName) \
            delete m_p##FriendlyName; \
\

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl)
#define MNGSTDITF_END_INTERFACE(FriendlyName)


#include "mngstditflist.h"


#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE
    }


    // Accessors for each of the managed standard interfaces.
#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
\
public: \
    FriendlyName *Get##FriendlyName() \
    { \
        CONTRACTL \
        {         \
            THROWS; \
            GC_TRIGGERS; \
            INJECT_FAULT(COMPlusThrowOM()); \
        } \
        CONTRACTL_END \
        if (!m_p##FriendlyName) \
        { \
            NewHolder<FriendlyName> pFriendlyName = new FriendlyName(); \
            if (InterlockedCompareExchangeT(&m_p##FriendlyName, pFriendlyName.GetValue(), NULL) == NULL) \
                pFriendlyName.SuppressRelease(); \
        } \
        return m_p##FriendlyName; \
    } \
\
private: \
    FriendlyName *m_p##FriendlyName; \
\

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl)
#define MNGSTDITF_END_INTERFACE(FriendlyName)


#include "mngstditflist.h"


#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE
};

#endif // _MNGSTDINTERFACEMAP_H
