// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DispatchInfo.cpp
//

//
// Implementation of helpers used to expose IDispatch
// and IDispatchEx to COM.
//


#include "common.h"

#include "dispatchinfo.h"
#include "dispex.h"
#include "object.h"
#include "field.h"
#include "method.hpp"
#include "class.h"
#include "comcallablewrapper.h"
#include "threads.h"
#include "excep.h"
#include "comutilnative.h"
#include "eeconfig.h"
#include "interoputil.h"
#include "olevariant.h"
#include "commtmemberinfomap.h"
#include "dispparammarshaler.h"
#include "reflectioninvocation.h"
#include "dbginterface.h"
#include "dllimport.h"

#define EXCEPTION_INNER_PROP                            "InnerException"

// The name of the properties accessed on the managed member infos.
#define MEMBER_INFO_NAME_PROP                           "Name"

// The initial size of the DISPID to member map.
#define DISPID_TO_MEMBER_MAP_INITIAL_SIZE        37

// The names of the properties that are accessed on the managed member info's
#define MEMBERINFO_TYPE_PROP            "MemberType"

// The names of the properties that are accessed on managed ParameterInfo.
#define PARAMETERINFO_NAME_PROP         "Name"


OBJECTHANDLE  DispatchInfo::m_hndOleAutBinder           = NULL;

MethodTable*      DispatchMemberInfo::s_pMemberTypes[NUM_MEMBER_TYPES]  = {NULL};
EnumMemberTypes DispatchMemberInfo::s_memberTypes[NUM_MEMBER_TYPES] = {Uninitted};
int           DispatchMemberInfo::s_iNumMemberTypesKnown            = 0;

// Helper function to convert between a DISPID and a hashkey.
inline UPTR DispID2HashKey(DISPID DispID)
{
    LIMITED_METHOD_CONTRACT;

    return DispID + 2;
}

// Typedef for string comparison functions.
typedef int (*UnicodeStringCompareFuncPtr)(const WCHAR *, const WCHAR *);

//--------------------------------------------------------------------------------
// The DispatchMemberInfo class implementation.

DispatchMemberInfo::DispatchMemberInfo(DispatchInfo *pDispInfo, DISPID DispID, SString& strName)
: m_DispID(DispID)
, m_hndMemberInfo(NULL)
, m_apParamMarshaler(NULL)
, m_pParamInOnly(NULL)
, m_pNext(NULL)
, m_strName(strName)
, m_enumType (Uninitted)
, m_iNumParams(-1)
, m_CultureAwareState(Unknown)
, m_bRequiresManagedCleanup(FALSE)
, m_bInitialized(FALSE)
, m_bNeutered(FALSE)
, m_pDispInfo(pDispInfo)
, m_bLastParamOleVarArg(FALSE)
{
    WRAPPER_NO_CONTRACT;
}

void DispatchMemberInfo::Neuter()
{
    WRAPPER_NO_CONTRACT;

    if (m_apParamMarshaler)
    {
        // Need to delete all individual members?
        // Can't calculate the exact number here.
        delete [] m_apParamMarshaler;
        m_apParamMarshaler = NULL;
    }

    if (m_pParamInOnly)
    {
        delete [] m_pParamInOnly;
        m_pParamInOnly = NULL;
    }

    //m_pNext = NULL;
    m_enumType = Uninitted;
    m_iNumParams = -1;
    m_CultureAwareState = Unknown;
    m_bNeutered = TRUE;
}

DispatchMemberInfo::~DispatchMemberInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Delete the parameter marshalers and then delete the array of parameter
    // marshalers itself.
    if (m_apParamMarshaler)
    {
        EnumMemberTypes MemberType = GetMemberType();
        int NumParamMarshalers = GetNumParameters() + ((MemberType == Property) ? 2 : 1);
        for (int i = 0; i < NumParamMarshalers; i++)
        {
            if (m_apParamMarshaler[i])
                delete m_apParamMarshaler[i];
        }
        delete []m_apParamMarshaler;
    }

    if (m_pParamInOnly)
        delete [] m_pParamInOnly;

    if (m_hndMemberInfo)
        m_pDispInfo->FreeHandle(m_hndMemberInfo);

    // Clear the name of the member.
    m_strName.Clear();
}

void DispatchMemberInfo::EnsureInitialized()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Initialize the entry if it hasn't been initialized yet. This must be synchronized.
    if (!m_bInitialized)
    {
        DispatchInfo::LockHolder lh(m_pDispInfo);

        if (!m_bInitialized)
            Init();
    }
}

void DispatchMemberInfo::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    EX_TRY
    {
        // Determine the type of the member.
        DetermineMemberType();

        // Determine the parameter count.
        DetermineParamCount();

        // Determine the culture awareness of the member.
        DetermineCultureAwareness();

        // Set up the parameter marshaler info.
        SetUpParamMarshalerInfo();

        // Mark the dispatch member info as having been initialized.
        m_bInitialized = TRUE;
    }
    EX_CATCH
    {
        // If we do throw an exception, then the status of the object
        // is in limbo - just neuter it.
        Neuter();
    }
    EX_END_CATCH(RethrowTerminalExceptions);
}

HRESULT DispatchMemberInfo::GetIDsOfParameters(_In_reads_(NumNames) WCHAR **astrNames, int NumNames, DISPID *aDispIds, BOOL bCaseSensitive)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());

        // The member info must have been initialized before this is called.
        PRECONDITION(TRUE == m_bInitialized);
        PRECONDITION(CheckPointer(astrNames));
        PRECONDITION(CheckPointer(aDispIds));
    }
    CONTRACTL_END;

    int NumNamesMapped = 0;
    PTRARRAYREF ParamArray = NULL;
    int cNames = 0;

    // Initialize all the ID's to DISPID_UNKNOWN.
    for (cNames = 0; cNames < NumNames; cNames++)
        aDispIds[cNames] = DISPID_UNKNOWN;

    // Retrieve the appropriate string comparation function.
    UnicodeStringCompareFuncPtr StrCompFunc = bCaseSensitive ? u16_strcmp : SString::_wcsicmp;

    GCPROTECT_BEGIN(ParamArray)
    {
        // Retrieve the member parameters.
        ParamArray = GetParameters();

        // If we managed to retrieve an non empty array of parameters then go through it and
        // map the specified names to ID's.
        if ((ParamArray != NULL) && (ParamArray->GetNumComponents() > 0))
        {
            int NumParams = ParamArray->GetNumComponents();
            int cParams = 0;
            NewArrayHolder< NewArrayHolder<WCHAR> > astrParamNames = new NewArrayHolder<WCHAR>[NumParams];

            // Go through and retrieve the names of all the components.
            for (cParams = 0; cParams < NumParams; cParams++)
            {
                OBJECTREF ParamInfoObj = ParamArray->GetAt(cParams);
                GCPROTECT_BEGIN(ParamInfoObj)
                {
                    // Retrieve the MD to use to retrieve the name of the parameter.
                    MethodDesc *pGetParamNameMD = MemberLoader::FindPropertyMethod(ParamInfoObj->GetMethodTable(), PARAMETERINFO_NAME_PROP, PropertyGet);
                    _ASSERTE(pGetParamNameMD && "Unable to find getter method for property ParameterInfo::Name");
                    MethodDescCallSite getParamName(pGetParamNameMD, &ParamInfoObj);

                    // Retrieve the name of the parameter.
                    ARG_SLOT GetNameArgs[] =
                    {
                        ObjToArgSlot(ParamInfoObj)
                    };
                    STRINGREF MemberNameObj = getParamName.Call_RetSTRINGREF(GetNameArgs);

                        // If we got a valid name back then store that in the array of names.
                    if (MemberNameObj != NULL)
                    {
                        astrParamNames[cParams] = new WCHAR[MemberNameObj->GetStringLength() + 1];
                        wcscpy_s(astrParamNames[cParams], MemberNameObj->GetStringLength() + 1, MemberNameObj->GetBuffer());
                    }
                }
                GCPROTECT_END();
            }

            // Now go through the list of specfiied names and map then to ID's.
            for (cNames = 0; cNames < NumNames; cNames++)
            {
                for (cParams = 0; cParams < NumParams; cParams++)
                {
                    if (astrParamNames[cParams] && (StrCompFunc(astrNames[cNames], astrParamNames[cParams]) == 0))
                    {
                        aDispIds[cNames] = cParams;
                        NumNamesMapped++;
                        break;
                    }
                }
            }
        }
    }
    GCPROTECT_END();

    return (NumNamesMapped == NumNames) ? S_OK : DISP_E_UNKNOWNNAME;
}

PTRARRAYREF DispatchMemberInfo::GetParameters()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PTRARRAYREF ParamArray = NULL;
    MethodDesc *pGetParamsMD = NULL;

    // Retrieve the method to use to retrieve the array of parameters.
    switch (GetMemberType())
    {
        case Method:
        {
            pGetParamsMD = DispatchInfo::GetMethodInfoMD(METHOD__METHOD__GET_PARAMETERS, GetMemberInfoObject()->GetTypeHandle());
            _ASSERTE(pGetParamsMD && "Unable to find method MemberBase::GetParameters");
            break;
        }

        case Property:
        {
            pGetParamsMD = DispatchInfo::GetPropertyInfoMD(METHOD__PROPERTY__GET_INDEX_PARAMETERS, GetMemberInfoObject()->GetTypeHandle());
            _ASSERTE(pGetParamsMD && "Unable to find method PropertyInfo::GetIndexParameters");
            break;
        }
    }

    // If the member has parameters then retrieve the array of parameters.
    if (pGetParamsMD != NULL)
    {
        OBJECTREF memberInfoObject = GetMemberInfoObject();
        GCPROTECT_BEGIN(memberInfoObject)
        MethodDescCallSite getParams(pGetParamsMD, &memberInfoObject);

        ARG_SLOT GetParamsArgs[] =
        {
            ObjToArgSlot(memberInfoObject)
        };

        ParamArray = (PTRARRAYREF) getParams.Call_RetOBJECTREF(GetParamsArgs);
        GCPROTECT_END();
    }

    return ParamArray;
}

OBJECTREF DispatchMemberInfo::GetMemberInfoObject()
{
    WRAPPER_NO_CONTRACT;
    return m_pDispInfo->GetHandleValue(m_hndMemberInfo);
}

void DispatchMemberInfo::MarshalParamNativeToManaged(int iParam, VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSrcVar));
        PRECONDITION(pDestObj != NULL);
        PRECONDITION(TRUE == m_bInitialized);
    }
    CONTRACTL_END;

    if (m_apParamMarshaler && m_apParamMarshaler[iParam + 1])
        m_apParamMarshaler[iParam + 1]->MarshalNativeToManaged(pSrcVar, pDestObj);
    else
        OleVariant::MarshalObjectForOleVariant(pSrcVar, pDestObj);
}

void DispatchMemberInfo::MarshalParamManagedToNativeRef(int iParam, OBJECTREF *pSrcObj, VARIANT *pRefVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRefVar));
        PRECONDITION(TRUE == m_bInitialized);
        PRECONDITION(pSrcObj != NULL);
    }
    CONTRACTL_END;

    if (m_apParamMarshaler && m_apParamMarshaler[iParam + 1])
        m_apParamMarshaler[iParam + 1]->MarshalManagedToNativeRef(pSrcObj, pRefVar);
    else
        OleVariant::MarshalOleRefVariantForObject(pSrcObj, pRefVar);
}

void DispatchMemberInfo::CleanUpParamManaged(int iParam, OBJECTREF *pObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pObj != NULL);
        PRECONDITION(TRUE == m_bInitialized);
    }
    CONTRACTL_END;

    if (m_apParamMarshaler && m_apParamMarshaler[iParam + 1])
        m_apParamMarshaler[iParam + 1]->CleanUpManaged(pObj);
}

void DispatchMemberInfo::MarshalReturnValueManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDestVar));
        PRECONDITION(pSrcObj != NULL);
        PRECONDITION(TRUE == m_bInitialized);
    }
    CONTRACTL_END;

    if (m_apParamMarshaler && m_apParamMarshaler[0])
        m_apParamMarshaler[0]->MarshalManagedToNative(pSrcObj, pDestVar);
    else
        OleVariant::MarshalOleVariantForObject(pSrcObj, pDestVar);
}

ComMTMethodProps * DispatchMemberInfo::GetMemberProps(OBJECTREF MemberInfoObj, ComMTMemberInfoMap *pMemberMap)
{
    CONTRACT (ComMTMethodProps*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(MemberInfoObj != NULL);
        PRECONDITION(CheckPointer(pMemberMap, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    DISPID DispId = DISPID_UNKNOWN;
    ComMTMethodProps *pMemberProps = NULL;

    // If we don't have a member map then we cannot retrieve properties for the member.
    if (!pMemberMap)
        RETURN NULL;

    // Get the member's properties.
    GCPROTECT_BEGIN(MemberInfoObj);
    {
        MethodTable *pMemberInfoClass = MemberInfoObj->GetMethodTable();
        if (CoreLibBinder::IsClass(pMemberInfoClass, CLASS__METHOD))
        {
            // Retrieve the MethodDesc from the MethodInfo.
            MethodDescCallSite getMethodHandle(METHOD__METHOD_BASE__GET_METHODDESC, &MemberInfoObj);
            ARG_SLOT GetMethodHandleArg = ObjToArgSlot(MemberInfoObj);
            MethodDesc* pMeth = (MethodDesc*) getMethodHandle.Call_RetLPVOID(&GetMethodHandleArg);
            if (pMeth)
                pMemberProps = pMemberMap->GetMethodProps(pMeth->GetMemberDef(), pMeth->GetModule());
        }
        else if (CoreLibBinder::IsClass(pMemberInfoClass, CLASS__RT_FIELD_INFO))
        {
            MethodDescCallSite getFieldHandle(METHOD__RTFIELD__GET_FIELDHANDLE, &MemberInfoObj);
            ARG_SLOT arg = ObjToArgSlot(MemberInfoObj);
            FieldDesc* pFld = (FieldDesc*) getFieldHandle.Call_RetLPVOID(&arg);
            if (pFld)
                pMemberProps = pMemberMap->GetMethodProps(pFld->GetMemberDef(), pFld->GetModule());
        }
        else if (CoreLibBinder::IsClass(pMemberInfoClass, CLASS__PROPERTY))
        {
            MethodDescCallSite getToken(METHOD__PROPERTY__GET_TOKEN, &MemberInfoObj);
            ARG_SLOT arg = ObjToArgSlot(MemberInfoObj);
            mdToken propTok = (mdToken) getToken.Call_RetArgSlot(&arg);
            MethodDescCallSite getModule(METHOD__PROPERTY__GET_MODULE, &MemberInfoObj);
            ARG_SLOT arg1 = ObjToArgSlot(MemberInfoObj);
            REFLECTMODULEBASEREF module = (REFLECTMODULEBASEREF) getModule.Call_RetOBJECTREF(&arg1);
            Module* pModule = module->GetModule();
            pMemberProps = pMemberMap->GetMethodProps(propTok, pModule);
        }
    }
    GCPROTECT_END();

    RETURN pMemberProps;
}

DISPID DispatchMemberInfo::GetMemberDispId(OBJECTREF MemberInfoObj, ComMTMemberInfoMap *pMemberMap)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMemberMap, NULL_OK));
    }
    CONTRACTL_END;

    _ASSERT(MemberInfoObj);

    DISPID DispId = DISPID_UNKNOWN;

    // Get the member's properties.
    ComMTMethodProps *pMemberProps = GetMemberProps(MemberInfoObj, pMemberMap);

    // If we managed to get the properties of the member then extract the DISPID.
    if (pMemberProps)
        DispId = pMemberProps->dispid;

    return DispId;
}

LPWSTR DispatchMemberInfo::GetMemberName(OBJECTREF MemberInfoObj, ComMTMemberInfoMap *pMemberMap)
{
    CONTRACT (LPWSTR)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(MemberInfoObj != NULL);
        PRECONDITION(CheckPointer(pMemberMap, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    NewArrayHolder<WCHAR> strMemberName = NULL;
    ComMTMethodProps *pMemberProps = NULL;

    GCPROTECT_BEGIN(MemberInfoObj);
    {
        // Get the member's properties.
        pMemberProps = GetMemberProps(MemberInfoObj, pMemberMap);

        // If we managed to get the member's properties then extract the name.
        if (pMemberProps)
        {
            int MemberNameLen = (INT)u16_strlen(pMemberProps->pName);
            strMemberName = new WCHAR[MemberNameLen + 1];

            memcpy(strMemberName, pMemberProps->pName, (MemberNameLen + 1) * sizeof(WCHAR));
        }
        else
        {
            // Retrieve the Get method for the Name property.
            MethodDesc *pMD = MemberLoader::FindPropertyMethod(MemberInfoObj->GetMethodTable(), MEMBER_INFO_NAME_PROP, PropertyGet);
            _ASSERTE(pMD && "Unable to find getter method for property MemberInfo::Name");
            MethodDescCallSite propGet(pMD, &MemberInfoObj);

            // Prepare the arguments.
            ARG_SLOT Args[] =
            {
                ObjToArgSlot(MemberInfoObj)
            };

            // Retrieve the value of the Name property.
            STRINGREF strObj = propGet.Call_RetSTRINGREF(Args);
            _ASSERTE(strObj != NULL);

            // Copy the name into the buffer we will return.
            int MemberNameLen = strObj->GetStringLength();
            strMemberName = new WCHAR[strObj->GetStringLength() + 1];
            memcpy(strMemberName, strObj->GetBuffer(), MemberNameLen * sizeof(WCHAR));
            strMemberName[MemberNameLen] = 0;
        }
    }
    GCPROTECT_END();

    strMemberName.SuppressRelease();
    RETURN strMemberName;
}

void DispatchMemberInfo::DetermineMemberType()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

    // This should not be called more than once.
        PRECONDITION(m_enumType == Uninitted);
    }
    CONTRACTL_END;

    OBJECTREF MemberInfoObj = GetMemberInfoObject();

    // Check to see if the member info is of a type we have already seen.
    TypeHandle pMemberInfoClass   = MemberInfoObj->GetTypeHandle();
    for (int i = 0 ; i < s_iNumMemberTypesKnown ; i++)
    {
        if (pMemberInfoClass.GetMethodTable() == s_pMemberTypes[i])
        {
            m_enumType = s_memberTypes[i];
            return;
        }
    }

    GCPROTECT_BEGIN(MemberInfoObj);
    {
        // Retrieve the method descriptor for the type property accessor.
        MethodDesc *pMD = MemberLoader::FindPropertyMethod(MemberInfoObj->GetMethodTable(), MEMBERINFO_TYPE_PROP, PropertyGet);
        _ASSERTE(pMD && "Unable to find getter method for property MemberInfo::Type");
        MethodDescCallSite propGet(pMD, &MemberInfoObj);

        // Prepare the arguments that will be used to retrieve the value of all the properties.
        ARG_SLOT Args[] =
        {
            ObjToArgSlot(MemberInfoObj)
        };

        // Retrieve the actual type of the member info.
        m_enumType = (EnumMemberTypes)propGet.Call_RetArgSlot(Args);
    }
    GCPROTECT_END();

    if (s_iNumMemberTypesKnown < NUM_MEMBER_TYPES)
    {
        s_pMemberTypes[s_iNumMemberTypesKnown] = MemberInfoObj->GetMethodTable();
        s_memberTypes[s_iNumMemberTypesKnown++] = m_enumType;
    }
}

void DispatchMemberInfo::DetermineParamCount()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    // This should not be called more than once.
        PRECONDITION(m_iNumParams == -1);
    }
    CONTRACTL_END;

    MethodDesc *pGetParamsMD = NULL;

    OBJECTREF MemberInfoObj = GetMemberInfoObject();
    GCPROTECT_BEGIN(MemberInfoObj);
    {
        // Retrieve the method to use to retrieve the array of parameters.
        switch (GetMemberType())
        {
            case Method:
            {
                pGetParamsMD = DispatchInfo::GetMethodInfoMD(METHOD__METHOD__GET_PARAMETERS, GetMemberInfoObject()->GetTypeHandle());
                _ASSERTE(pGetParamsMD && "Unable to find method MemberBase::GetParameters");
                break;
            }

            case Property:
            {
                pGetParamsMD = DispatchInfo::GetPropertyInfoMD(METHOD__PROPERTY__GET_INDEX_PARAMETERS, GetMemberInfoObject()->GetTypeHandle());
                _ASSERTE(pGetParamsMD && "Unable to find method PropertyInfo::GetIndexParameters");
                break;
            }
        }

        // If the member has parameters then get their count.
        if (pGetParamsMD != NULL)
        {
            MethodDescCallSite getParams(pGetParamsMD, &MemberInfoObj);

            ARG_SLOT GetParamsArgs[] =
            {
                ObjToArgSlot(GetMemberInfoObject())
            };

            PTRARRAYREF ParamArray = (PTRARRAYREF) getParams.Call_RetOBJECTREF(GetParamsArgs);
            if (ParamArray != NULL)
                m_iNumParams = ParamArray->GetNumComponents();
        }
        else
        {
            m_iNumParams = 0;
        }
    }
    GCPROTECT_END();
}

void DispatchMemberInfo::DetermineCultureAwareness()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    // This should not be called more than once.
        PRECONDITION(m_CultureAwareState == Unknown);
    }
    CONTRACTL_END;

    // Load the LCIDConversionAttribute type.
    MethodTable * pLcIdConvAttrClass = CoreLibBinder::GetClass(CLASS__LCID_CONVERSION_TYPE);

    // Check to see if the attribute is set.
    OBJECTREF MemberInfoObj = GetMemberInfoObject();
    GCPROTECT_BEGIN(MemberInfoObj);
    {
        // Retrieve the method to use to determine if the DispIdAttribute custom attribute is set.
        MethodDesc *pGetCustomAttributesMD = DispatchInfo::GetCustomAttrProviderMD(MemberInfoObj->GetTypeHandle());
        MethodDescCallSite getCustomAttributes(pGetCustomAttributesMD, &MemberInfoObj);

        // Prepare the arguments.
        ARG_SLOT GetCustomAttributesArgs[] =
        {
            0,
            ObjToArgSlot(pLcIdConvAttrClass->GetManagedClassObject()),
            0,
        };

        // Now that we have potentially triggered a GC in the GetManagedClassObject
        // call above, it is safe to set the 'this' using our properly protected
        // MemberInfoObj value.
        GetCustomAttributesArgs[0] = ObjToArgSlot(MemberInfoObj);

        // Retrieve the custom attributes of type LCIDConversionAttribute.
        PTRARRAYREF CustomAttrArray = NULL;
        EX_TRY
        {
            CustomAttrArray = (PTRARRAYREF) getCustomAttributes.Call_RetOBJECTREF(GetCustomAttributesArgs);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions)

        GCPROTECT_BEGIN(CustomAttrArray)
        {
            if ((CustomAttrArray != NULL) && (CustomAttrArray->GetNumComponents() > 0))
                m_CultureAwareState = Aware;
            else
                m_CultureAwareState = NonAware;
        }
        GCPROTECT_END();
    }
    GCPROTECT_END();
}

void DispatchMemberInfo::SetUpParamMarshalerInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BOOL bSetUpReturnValueOnly = FALSE;
    OBJECTREF SetterObj = NULL;
    OBJECTREF GetterObj = NULL;
    OBJECTREF MemberInfoObj = GetMemberInfoObject();

    GCPROTECT_BEGIN(SetterObj);
    GCPROTECT_BEGIN(GetterObj);
    GCPROTECT_BEGIN(MemberInfoObj);
    {
        MethodTable *pMemberInfoMT = MemberInfoObj->GetMethodTable();

        if (CoreLibBinder::IsClass(pMemberInfoMT, CLASS__METHOD))
        {
            MethodDescCallSite getMethodHandle(METHOD__METHOD_BASE__GET_METHODDESC, &MemberInfoObj);
            ARG_SLOT arg = ObjToArgSlot(MemberInfoObj);
            MethodDesc* pMeth = (MethodDesc*) getMethodHandle.Call_RetLPVOID(&arg);
            if (pMeth)
                SetUpMethodMarshalerInfo(pMeth, FALSE);
        }
        else if (CoreLibBinder::IsClass(pMemberInfoMT, CLASS__FIELD))
        {
            // We don't support non-default marshalling behavior for field getter/setter stubs invoked via IDispatch.
        }
        else if (CoreLibBinder::IsClass(pMemberInfoMT, CLASS__PROPERTY))
        {
            BOOL isGetter = FALSE;
            MethodDescCallSite getSetter(METHOD__PROPERTY__GET_SETTER, &MemberInfoObj);
            ARG_SLOT args[] =
            {
                ObjToArgSlot(MemberInfoObj),
                BoolToArgSlot(false)
            };
            SetterObj = getSetter.Call_RetOBJECTREF(args);

            if (SetterObj != NULL)
            {
                MethodDescCallSite getMethodHandle(METHOD__METHOD_BASE__GET_METHODDESC, &SetterObj);
                ARG_SLOT arg = ObjToArgSlot(SetterObj);
                MethodDesc* pMeth = (MethodDesc*) getMethodHandle.Call_RetLPVOID(&arg);
                if (pMeth)
                {
                    bSetUpReturnValueOnly = TRUE;
                    SetUpMethodMarshalerInfo(pMeth, FALSE);
                }
            }

            MethodDescCallSite getGetter(METHOD__PROPERTY__GET_GETTER, &MemberInfoObj);
            ARG_SLOT args1[] =
            {
                ObjToArgSlot(MemberInfoObj),
                BoolToArgSlot(false)
            };
            GetterObj = getGetter.Call_RetOBJECTREF(args1);

            if (GetterObj != NULL)
            {
                MethodDescCallSite getMethodHandle(METHOD__METHOD_BASE__GET_METHODDESC, &GetterObj);
                ARG_SLOT arg = ObjToArgSlot(GetterObj);
                MethodDesc* pMeth = (MethodDesc*) getMethodHandle.Call_RetLPVOID(&arg);
                if (pMeth)
                {
                    // Only set up the marshalling information for the parameters if we
                    // haven't done it already for the setter.
                    SetUpMethodMarshalerInfo(pMeth, bSetUpReturnValueOnly);
                }
            }
        }
        else
        {
            // @FUTURE: Add support for user defined derived classes for
            //          MethodInfo, PropertyInfo and FieldInfo.
        }
    }
    GCPROTECT_END();
    GCPROTECT_END();
    GCPROTECT_END();
}

void DispatchMemberInfo::SetUpMethodMarshalerInfo(MethodDesc *pMD, BOOL bReturnValueOnly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    GCX_PREEMP();

    MetaSig         msig(pMD);
    LPCSTR          szName;
    USHORT          usSequence;
    DWORD           dwAttr;
    mdParamDef      returnParamDef = mdParamDefNil;
    mdParamDef      currParamDef = mdParamDefNil;

    int numArgs = msig.NumFixedArgs();

    IMDInternalImport *pInternalImport = msig.GetModule()->GetMDImport();

    HENUMInternalHolder hEnumParams(pInternalImport);

    //
    // Initialize the parameter definition enum.
    //
    hEnumParams.EnumInit(mdtParamDef, pMD->GetMemberDef());

    //
    // Retrieve the paramdef for the return type and determine which is the next
    // parameter that has parameter information.
    //
    do
    {
        if (pInternalImport->EnumNext(&hEnumParams, &currParamDef))
        {
            IfFailThrow(pInternalImport->GetParamDefProps(currParamDef, &usSequence, &dwAttr, &szName));

            if (usSequence == 0)
            {
                // The first parameter, if it has sequence 0, actually describes the return type.
                returnParamDef = currParamDef;
            }
        }
        else
        {
            usSequence = (USHORT)-1;
        }
    }
    while (usSequence == 0);

    // Look up the best fit mapping info via Assembly & Interface level attributes
    BOOL BestFit = TRUE;
    BOOL ThrowOnUnmappableChar = FALSE;
    ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);

    //
    // Unless the bReturnValueOnly flag is set, set up the marshaling info for the parameters.
    //
    if (!bReturnValueOnly)
    {
        int iParam = 1;
        CorElementType  mtype;
        while (ELEMENT_TYPE_END != (mtype = msig.NextArg()))
        {
            //
            // Get the parameter token if the current parameter has one.
            //
            mdParamDef paramDef = mdParamDefNil;
            if (usSequence == iParam)
            {
                paramDef = currParamDef;

                if (pInternalImport->EnumNext(&hEnumParams, &currParamDef))
                {
                    IfFailThrow(pInternalImport->GetParamDefProps(currParamDef, &usSequence, &dwAttr, &szName));

                    // Validate that the param def tokens are in order.
                    _ASSERTE((usSequence > iParam) && "Param def tokens are not in order");
                }
                else
                {
                    usSequence = (USHORT)-1;
                }
            }


            //
            // Set up the marshaling info for the parameter.
            //

            MarshalInfo Info(msig.GetModule(), msig.GetArgProps(), msig.GetSigTypeContext(), paramDef, MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                             (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                             TRUE, iParam, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, TRUE
    #ifdef _DEBUG
                     , pMD->m_pszDebugMethodName, pMD->m_pszDebugClassName, iParam
    #endif
                );


            //
            // Based on the MarshalInfo, set up a DispParamMarshaler for the parameter.
            //
            SetUpDispParamMarshalerForMarshalInfo(iParam, &Info);

            //
            // Get the in/out/ref attributes.
            //
            SetUpDispParamAttributes(iParam, &Info);

            m_bLastParamOleVarArg |= Info.IsOleVarArgCandidate();

            //
            // Increase the argument index.
            //
            iParam++;
        }

        // Make sure that there are not more param def tokens then there are COM+ arguments.
        _ASSERTE( usSequence == (USHORT)-1 && "There are more parameter information tokens then there are COM+ arguments" );
    }

    //
    // Set up the marshaling info for the return value.
    //

    if (!msig.IsReturnTypeVoid())
    {
        MarshalInfo Info(msig.GetModule(), msig.GetReturnProps(), msig.GetSigTypeContext(), returnParamDef, MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                         (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                         FALSE, 0, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, TRUE
#ifdef _DEBUG
                         , pMD->m_pszDebugMethodName, pMD->m_pszDebugClassName, 0
#endif
                        );

        SetUpDispParamMarshalerForMarshalInfo(0, &Info);
    }
}

void DispatchMemberInfo::SetUpDispParamMarshalerForMarshalInfo(int iParam, MarshalInfo *pInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pInfo));
    }
    CONTRACTL_END;

    DispParamMarshaler *pDispParamMarshaler = pInfo->GenerateDispParamMarshaler();
    if (pDispParamMarshaler)
    {
        // If the array of marshalers hasn't been allocated yet, then allocate it.
        if (!m_apParamMarshaler)
        {
            // The array needs to be one more than the number of parameters for
            // normal methods and fields and 2 more properties.
            EnumMemberTypes MemberType = GetMemberType();
            int NumParamMarshalers = GetNumParameters() + ((MemberType == Property) ? 2 : 1);
            m_apParamMarshaler = new DispParamMarshaler*[NumParamMarshalers];
            memset(m_apParamMarshaler, 0, sizeof(DispParamMarshaler*) * NumParamMarshalers);
        }

        // Set the DispParamMarshaler in the array.
        m_apParamMarshaler[iParam] = pDispParamMarshaler;

        // If the disp param marshaler requires managed cleanup, then set
        // m_bRequiresManagedCleanup to TRUE to indicate the method requires
        // managed cleanup.
        if (pDispParamMarshaler->RequiresManagedCleanup())
            m_bRequiresManagedCleanup = TRUE;
    }
}


void DispatchMemberInfo::SetUpDispParamAttributes(int iParam, MarshalInfo* Info)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(Info));
    }
    CONTRACTL_END;

    // If the arry of In Only parameter indicators hasn't been allocated yet, then allocate it.
    if (!m_pParamInOnly)
    {
        // The array needs to be one more than the number of parameters for
        // normal methods and fields and 2 more properties.
        EnumMemberTypes MemberType = GetMemberType();
        int NumInOnlyFlags = GetNumParameters() + ((MemberType == Property) ? 2 : 1);
        m_pParamInOnly = new BOOL[NumInOnlyFlags];
        memset(m_pParamInOnly, 0, sizeof(BOOL) * NumInOnlyFlags);
    }

    m_pParamInOnly[iParam] = ( Info->IsIn() && !Info->IsOut() );
}

//--------------------------------------------------------------------------------
// The DispatchInfo class implementation.

DispatchInfo::DispatchInfo(MethodTable *pMT)
: m_pMT(pMT)
, m_pFirstMemberInfo(NULL)
, m_lock(CrstInterop, (CrstFlags)(CRST_HOST_BREAKABLE | CRST_REENTRANCY))
, m_CurrentDispID(0x10000)
, m_bAllowMembersNotInComMTMemberMap(FALSE)
, m_bInvokeUsingInvokeMember(FALSE)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    // Init the hashtable.
    m_DispIDToMemberInfoMap.Init(DISPID_TO_MEMBER_MAP_INITIAL_SIZE, NULL);
}

DispatchInfo::~DispatchInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DispatchMemberInfo* pCurrMember = m_pFirstMemberInfo;
    while (pCurrMember)
    {
        // Retrieve the next member.
        DispatchMemberInfo* pNextMember = pCurrMember->GetNext();

        // Delete the current member.
        delete pCurrMember;

        // Process the next member.
        pCurrMember = pNextMember;
    }
}

DispatchMemberInfo* DispatchInfo::FindMember(DISPID DispID)
{
    CONTRACT (DispatchMemberInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // We need to special case DISPID_UNKNOWN and -2 because the hashtable cannot handle them.
    // This is OK since these are invalid DISPID's.
    if ((DispID == DISPID_UNKNOWN) || (DispID == -2))
        RETURN NULL;

    // Lookup in the hashtable to find member with the specified DISPID. Note: this hash is unsynchronized, but Gethash
    // doesn't require synchronization.
    UPTR Data = (UPTR)m_DispIDToMemberInfoMap.Gethash(DispID2HashKey(DispID));
    if (Data != -1)
    {
        // We have found the member, so ensure it is initialized and return it.
        DispatchMemberInfo *pMemberInfo = (DispatchMemberInfo*)Data;

        pMemberInfo->EnsureInitialized();

        RETURN pMemberInfo;
    }
    else
    {
        RETURN NULL;
    }
}

DispatchMemberInfo* DispatchInfo::FindMember(SString& strName, BOOL bCaseSensitive)
{
    CONTRACT (DispatchMemberInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    BOOL fFound = FALSE;

    // Go through the list of DispatchMemberInfo's to try and find one with the
    // specified name.
    DispatchMemberInfo *pCurrMemberInfo = m_pFirstMemberInfo;
    while (pCurrMemberInfo)
    {
        if (pCurrMemberInfo->GetMemberInfoObject() != NULL)
        {
            // Compare the 2 strings.
            SString& name = pCurrMemberInfo->GetName();
            if (bCaseSensitive
                    ? name.Equals(strName)
                    : name.EqualsCaseInsensitive(strName))
            {
                // We have found the member, so ensure it is initialized and return it.
                pCurrMemberInfo->EnsureInitialized();

                RETURN pCurrMemberInfo;
            }
        }

        // Process the next member.
        pCurrMemberInfo = pCurrMemberInfo->GetNext();
    }

    // No member has been found with the corresponding name.
    RETURN NULL;
}

// Helper method used to create DispatchMemberInfo's. This is only here because
// we can't call new inside a method that has a EX_TRY statement.
DispatchMemberInfo* DispatchInfo::CreateDispatchMemberInfoInstance(DISPID dispID, SString& strMemberName, OBJECTREF memberInfoObj)
{
    CONTRACT (DispatchMemberInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    DispatchMemberInfo* pInfo = new DispatchMemberInfo(this, dispID, strMemberName);
    pInfo->SetHandle(AllocateHandle(memberInfoObj));

    RETURN pInfo;
}

// Used for cleanup of managed objects via custom marshalers. This class is stack-allocated
// in code:DispatchInfo.InvokeMemberWorker to guarantee cleanup in the face of exception.
class ManagedParamCleanupHolder
{
    DispatchMemberInfo *m_pDispMemberInfo;
    InvokeObjects      *m_pObjs;
    int                 m_CleanUpArrayArraySize;

public:
    ManagedParamCleanupHolder(DispatchMemberInfo *pDispMemberInfo, InvokeObjects *pObjs)
        : m_pDispMemberInfo(pDispMemberInfo),
          m_pObjs(pObjs),
          m_CleanUpArrayArraySize(-1)
    {
        LIMITED_METHOD_CONTRACT;
        m_pObjs->CleanUpArray = NULL;
    }

    void SetData(PTRARRAYREF pCleanUpArray, int iCleanUpArrayArraySize)
    {
        LIMITED_METHOD_CONTRACT;
        m_CleanUpArrayArraySize = iCleanUpArrayArraySize;
        m_pObjs->CleanUpArray = pCleanUpArray;
    }

    ~ManagedParamCleanupHolder()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        // If the member info requires managed object cleanup, then do it now.
        if (m_pObjs->CleanUpArray != NULL && m_pDispMemberInfo->RequiresManagedObjCleanup())
        {
            GCX_COOP();
            _ASSERTE(m_CleanUpArrayArraySize != -1);

            for (int i = 0; i < m_CleanUpArrayArraySize; i++)
            {
                // Clean up all the managed parameters that were generated.
                m_pObjs->TmpObj = m_pObjs->CleanUpArray->GetAt(i);
                m_pDispMemberInfo->CleanUpParamManaged(i, &m_pObjs->TmpObj);
            }
        }
    }
};

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void DispatchInfo::InvokeMemberWorker(DispatchMemberInfo*   pDispMemberInfo,
                                      InvokeObjects*        pObjs,
                                      int                   NumParams,
                                      int                   NumArgs,
                                      int                   NumNamedArgs,
                                      int&                  NumByrefArgs,
                                      int&                  iSrcArg,
                                      DISPID                id,
                                      DISPPARAMS*           pdp,
                                      VARIANT*              pVarRes,
                                      WORD                  wFlags,
                                      LCID                  lcid,
                                      DISPID*               pSrcArgNames,
                                      VARIANT*              pSrcArgs,
                                      OBJECTHANDLE*         aByrefStaticArrayBackupObjHandle,
                                      int*                  pManagedMethodParamIndexMap,
                                      VARIANT**             aByrefArgOleVariant)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        // there are too many fields in pObjs, here I assume once one of them is
        // protected, the whole structure is protected.
        PRECONDITION(IsProtectedByGCFrame(&pObjs->MemberInfo));
    }
    CONTRACTL_END;

    int iDestArg;
    ManagedParamCleanupHolder cleanupHolder(pDispMemberInfo, pObjs);
    BOOL bPropValIsByref = FALSE;
    EnumMemberTypes MemberType;

    Thread* pThread = GetThread();
    AppDomain* pAppDomain = pThread->GetDomain();

    SafeArrayPtrHolder pSA = NULL;
    VARIANT safeArrayVar;
    HRESULT hr;

    // Allocate the array of used flags.
    BYTE *aArgUsedFlags = (BYTE*)_alloca(NumParams * sizeof(BYTE));
    memset(aArgUsedFlags, 0, NumParams * sizeof(BYTE));

    size_t cbByrefArgMngVariantIndex;
    if (!ClrSafeInt<size_t>::multiply(sizeof(DWORD), NumArgs, cbByrefArgMngVariantIndex))
        ThrowHR(COR_E_OVERFLOW);

    DWORD *aByrefArgMngVariantIndex = (DWORD *)_alloca(cbByrefArgMngVariantIndex);


    //
    // Retrieve information required for the invoke call.
    //

    pObjs->OleAutBinder = DispatchInfo::GetOleAutBinder();


    //
    // Allocate the array of arguments
    //

    // Allocate the array that will contain the converted variants in the right order.
    // If the invoke is for a PROPUT or a PROPPUTREF and we are going to call through
    // invoke member then allocate the array one bigger to allow space for the property
    // value.
    int ArraySize = NumParams;
    if (m_bInvokeUsingInvokeMember && (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF)))
    {
        if (!ClrSafeInt<int>::addition(ArraySize, 1, ArraySize))
            ThrowHR(COR_E_OVERFLOW);
    }

    pObjs->ParamArray = (PTRARRAYREF)AllocateObjectArray(ArraySize, g_pObjectClass);


    //
    // Convert the property set argument if the invoke is a PROPERTYPUT OR PROPERTYPUTREF.
    //

    if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
    {
        // Convert the variant.
        VARIANT *pSrcOleVariant = RetrieveSrcVariant(&pdp->rgvarg[0]);
        MarshalParamNativeToManaged(pDispMemberInfo, NumArgs, pSrcOleVariant, &pObjs->PropVal);

        // Remember if the property value is byref or not.
        bPropValIsByref = V_VT(pSrcOleVariant) & VT_BYREF;

        // If the variant is a byref static array, then remember the property value.
        if (IsVariantByrefStaticArray(pSrcOleVariant))
            SetObjectReference(&pObjs->ByrefStaticArrayBackupPropVal, pObjs->PropVal);
    }


    //
    // Convert the named arguments.
    //

    if (!m_bInvokeUsingInvokeMember)
    {
        for (iSrcArg = 0; iSrcArg < NumNamedArgs; iSrcArg++)
        {
            // Determine the destination index.
            iDestArg = pSrcArgNames[iSrcArg];

            // Check for duplicate param DISPID's.
            if (aArgUsedFlags[iDestArg] != 0)
                COMPlusThrowHR(DISP_E_PARAMNOTFOUND);

            // Convert the variant.
            VARIANT *pSrcOleVariant = RetrieveSrcVariant(&pSrcArgs[iSrcArg]);
            MarshalParamNativeToManaged(pDispMemberInfo, iDestArg, pSrcOleVariant, &pObjs->TmpObj);
            pObjs->ParamArray->SetAt(iDestArg, pObjs->TmpObj);

            // If the argument is byref then add it to the array of byref arguments.
            if (V_VT(pSrcOleVariant) & VT_BYREF)
            {
                // Remember what arg this really is.
                pManagedMethodParamIndexMap[NumByrefArgs] = iDestArg;

                aByrefArgOleVariant[NumByrefArgs] = pSrcOleVariant;
                aByrefArgMngVariantIndex[NumByrefArgs] = iDestArg;

                // If the variant is a byref static array, then remember the objectref we
                // converted the variant to.
                if (IsVariantByrefStaticArray(pSrcOleVariant))
                    aByrefStaticArrayBackupObjHandle[NumByrefArgs] = pAppDomain->CreateHandle(pObjs->TmpObj);

                NumByrefArgs++;
            }

            // Mark the slot the argument is in as occupied.
            aArgUsedFlags[iDestArg] = 1;
        }
    }
    else
    {
        for (iSrcArg = 0, iDestArg = 0; iSrcArg < NumNamedArgs; iSrcArg++, iDestArg++)
        {
            // Check for duplicate param DISPID's.
            if (aArgUsedFlags[iDestArg] != 0)
                COMPlusThrowHR(DISP_E_PARAMNOTFOUND);

            // Convert the variant.
            VARIANT *pSrcOleVariant = RetrieveSrcVariant(&pSrcArgs[iSrcArg]);
            MarshalParamNativeToManaged(pDispMemberInfo, iDestArg, pSrcOleVariant, &pObjs->TmpObj);
            pObjs->ParamArray->SetAt(iDestArg, pObjs->TmpObj);

            // If the argument is byref then add it to the array of byref arguments.
            if (V_VT(pSrcOleVariant) & VT_BYREF)
            {
                // Remember what arg this really is.
                pManagedMethodParamIndexMap[NumByrefArgs] = iDestArg;

                aByrefArgOleVariant[NumByrefArgs] = pSrcOleVariant;
                aByrefArgMngVariantIndex[NumByrefArgs] = iDestArg;

                // If the variant is a byref static array, then remember the objectref we
                // converted the variant to.
                if (IsVariantByrefStaticArray(pSrcOleVariant))
                    aByrefStaticArrayBackupObjHandle[NumByrefArgs] = pAppDomain->CreateHandle(pObjs->TmpObj);

                NumByrefArgs++;
            }

            // Mark the slot the argument is in as occupied.
            aArgUsedFlags[iDestArg] = 1;
        }
    }


    //
    // Fill in the positional arguments. These are copied in reverse order and we also
    // need to skip the arguments already filled in by named arguments.
    //
    BOOL bLastParamOleVarArg = pDispMemberInfo && pDispMemberInfo->IsLastParamOleVarArg();
    BOOL bByRefArg;

    // We support VarArg by aligning with the behavior of params array in C#.
    // Here are things we do for callers depends on the arguments it passes:
    // a) NumArgs == NumParams -1:
    //     We generate a SAFEARRAY with 0 elements and pass the VARIANT
    //     wrapping it to the callee
    // b) NumArgs == NumParams && the first argument is NOT safearray:
    //     Note that arguments are passed from right to left so that the first argument
    //     passed by caller should be mapped to the last parameter of the callee
    //     We generate a SAFEARRAY to wrap the argument and pass the VARIANT
    //     wrapping the SAFEARRAY to the callee
    // c) NumArgs == NumParams && the first argument is safearray:
    //    We directly pass it to the callee. To compact with v2 behavior, we loose the
    //     conditions by checking if the VT of the safearray varaint is VT_ARRAY only
    // d) NumArgs > NumParams:
    //     We generate a SAFEARRAY to wrap then and pass the VARIANT wrapping
    //     the SAFEARRAY to the callee
    for (iSrcArg = NumArgs - 1, iDestArg = 0;
         iSrcArg >= NumNamedArgs || (iDestArg == NumParams - 1 && bLastParamOleVarArg)/* for vararg case a) */;
         iSrcArg--, iDestArg++)
    {
        // Skip the arguments already filled in by named args.
        for (; aArgUsedFlags[iDestArg] != 0; iDestArg++);
        _ASSERTE(iDestArg < NumParams);

        // Convert the variant.
        VARIANT *pSrcOleVariant = NULL;
        VARIANT *pFrstVarargOleVariant = NULL;
        BOOL bSrcOleVariantCached = FALSE;
        bByRefArg = FALSE;
        if (iDestArg == NumParams-1 && bLastParamOleVarArg)
        {
            // VarArg scenario
            BOOL bSrcArgIsSafeArray = FALSE;
            if (iSrcArg == NumNamedArgs)
            {
                pSrcOleVariant = RetrieveSrcVariant(&pSrcArgs[iSrcArg]);
                bSrcOleVariantCached = TRUE;
                if ((V_VT(pSrcOleVariant) == (VT_ARRAY | VT_VARIANT)) ||
                    (V_VT(pSrcOleVariant) == VT_ARRAY) // see the comments in case c) above
                   )
                {
                    // vararg case c)
                    bSrcArgIsSafeArray = TRUE;
                    bByRefArg = V_VT(pSrcOleVariant) & VT_BYREF;
                }
            }

            if (!bSrcArgIsSafeArray)
            {
                // vararg case a), b) and d)
                // 1. Construct a safearray
                LONG lSafeArrayArg = 0;
                bByRefArg = FALSE;
                pSA = SafeArrayCreateVector(VT_VARIANT, 0, iSrcArg - NumNamedArgs + 1);
                if (pSA.GetValue() == NULL)
                    COMPlusThrowHR(E_OUTOFMEMORY);
                V_VT(&safeArrayVar) = VT_VARIANT | VT_ARRAY;
                V_ARRAY(&safeArrayVar) = pSA;

                // 2. Put the remaining srcArg into the safearray
                for (; iSrcArg >= NumNamedArgs; iSrcArg--, lSafeArrayArg++)
                {
                    if (!bSrcOleVariantCached)
                        pSrcOleVariant = RetrieveSrcVariant(&pSrcArgs[iSrcArg]);
                    else
                        bSrcOleVariantCached = FALSE;
                    if (FAILED(hr = SafeArrayPutElement(pSA, &lSafeArrayArg, pSrcOleVariant)))
                        COMPlusThrowHR(hr);
                    // Handle the UnMarshal Scenario
                    if (lSafeArrayArg == 0)
                        pFrstVarargOleVariant = pSrcOleVariant;
                    // If any of the VARIANTS which are put into safearray is BYREF, we need marshal back it
                    bByRefArg |= V_VT(pSrcOleVariant) & VT_BYREF;
                }

                // 3. Adjust the pSrcOleVariant in order to marshal to the params array in managed side
                pSrcOleVariant = &safeArrayVar;
            }
        }
        else
        {
            pSrcOleVariant = RetrieveSrcVariant(&pSrcArgs[iSrcArg]);
            bByRefArg = V_VT(pSrcOleVariant) & VT_BYREF;
        }


        MarshalParamNativeToManaged(pDispMemberInfo, iDestArg, pSrcOleVariant, &pObjs->TmpObj);
        pObjs->ParamArray->SetAt(iDestArg, pObjs->TmpObj);

        // If the argument is byref then add it to the array of byref arguments.
        if (bByRefArg)
        {
            // Remember what arg this really is.
            pManagedMethodParamIndexMap[NumByrefArgs] = iDestArg;

            // Remember the original variant so that we can unmarshal it back
            // Note that when pSA is set, pSrcOleVaraint is re-write so that we use the first argument
            // of vararg instead
            if (pSA != NULL)
                aByrefArgOleVariant[NumByrefArgs] = pFrstVarargOleVariant;
            else
                aByrefArgOleVariant[NumByrefArgs] = pSrcOleVariant;

            aByrefArgMngVariantIndex[NumByrefArgs] = iDestArg;

            // If the variant is a byref static array, then remember the objectref we
            // converted the variant to.
            if (IsVariantByrefStaticArray(pSrcOleVariant))
                aByrefStaticArrayBackupObjHandle[NumByrefArgs] = pAppDomain->CreateHandle(pObjs->TmpObj);

            NumByrefArgs++;
        }
    }

    // Set the source arg back to -1 to indicate we are finished converting args.
    iSrcArg = -1;


    //
    // Fill in all the remaining arguments with Missing.Value.
    //

    for (; iDestArg < NumParams; iDestArg++)
    {
        if (aArgUsedFlags[iDestArg] == 0)
        {
            pObjs->ParamArray->SetAt(iDestArg, pAppDomain->GetMissingObject());
        }
    }


    //
    // Set up the binding flags to pass to reflection.
    //

    int BindingFlags = ConvertInvokeFlagsToBindingFlags(wFlags) | BINDER_OptionalParamBinding;


    //
    // Do the actual invocation on the member info.
    //

    if (!m_bInvokeUsingInvokeMember)
    {
        PREFIX_ASSUME(pDispMemberInfo != NULL);

        if (pDispMemberInfo->IsCultureAware())
        {
            // If the method is culture aware, then set the specified culture on the thread.
            GetCultureInfoForLCID(lcid, &pObjs->CultureInfo);
            pObjs->OldCultureInfo = Thread::GetCulture(FALSE);
            Thread::SetCulture(&pObjs->CultureInfo, FALSE);
        }

        // If the method has custom marshalers then we will need to call
        // the clean up method on the objects. So we need to make a copy of the
        // ParamArray since it might be changed by reflection if any of the
        // parameters are byref.
        if (pDispMemberInfo->RequiresManagedObjCleanup())
        {
            // Allocate the clean up array.
            int CleanUpArrayArraySize = NumParams;
            if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
            {
                if (!ClrSafeInt<int>::addition(CleanUpArrayArraySize, 1, CleanUpArrayArraySize))
                    ThrowHR(COR_E_OVERFLOW);
            }
            cleanupHolder.SetData((PTRARRAYREF)AllocateObjectArray(CleanUpArrayArraySize, g_pObjectClass), CleanUpArrayArraySize);
            _ASSERTE(pObjs->CleanUpArray != NULL);

            // Copy the parameters into the clean up array.
            for (int i = 0; i < ArraySize; i++)
                pObjs->CleanUpArray->SetAt(i, pObjs->ParamArray->GetAt(i));

            // If this invoke is for a PROPUT or PROPPUTREF, then add the property object to
            // the end of the clean up array.
            if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
                pObjs->CleanUpArray->SetAt(NumParams, pObjs->PropVal);
        }

        // Retrieve the member info object and the type of the member.
        pObjs->MemberInfo = pDispMemberInfo->GetMemberInfoObject();
        MemberType = pDispMemberInfo->GetMemberType();

        switch (MemberType)
        {
            case Field:
            {
                // Make sure this invoke is actually for a property put or get.
                if (wFlags & (DISPATCH_METHOD | DISPATCH_PROPERTYGET))
                {
                    // Do some more validation now that we know the type of the invocation.
                    if (NumNamedArgs != 0)
                        COMPlusThrowHR(DISP_E_NONAMEDARGS);
                    if (NumArgs != 0)
                        COMPlusThrowHR(DISP_E_BADPARAMCOUNT);

                    // Retrieve the method descriptor that will be called on.
                    MethodDesc *pMD = GetFieldInfoMD(METHOD__FIELD_INFO__GET_VALUE, pObjs->MemberInfo->GetTypeHandle());
                    MethodDescCallSite getValue(pMD, &pObjs->MemberInfo);

                    // Prepare the arguments that will be passed to Invoke.
                    ARG_SLOT Args[] =
                    {
                            ObjToArgSlot(pObjs->MemberInfo),
                            ObjToArgSlot(pObjs->Target),
                    };

                    // Do the actual method invocation.
                    pObjs->RetVal = getValue.Call_RetOBJECTREF(Args);
                }
                else if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
                {
                    // Do some more validation now that we know the type of the invocation.
                    if (NumArgs != 0)
                        COMPlusThrowHR(DISP_E_BADPARAMCOUNT);
                    if (NumNamedArgs != 0)
                        COMPlusThrowHR(DISP_E_NONAMEDARGS);

                    // Retrieve the method descriptor that will be called on.
                    MethodDesc *pMD = GetFieldInfoMD(METHOD__FIELD_INFO__SET_VALUE, pObjs->MemberInfo->GetTypeHandle());
                    MethodDescCallSite setValue(pMD, &pObjs->MemberInfo);

                    // Prepare the arguments that will be passed to Invoke.
                    ARG_SLOT Args[] =
                    {
                            ObjToArgSlot(pObjs->MemberInfo),
                            ObjToArgSlot(pObjs->Target),
                            ObjToArgSlot(pObjs->PropVal),
                            (ARG_SLOT) BindingFlags,
                            ObjToArgSlot(pObjs->OleAutBinder),
                            ObjToArgSlot(pObjs->CultureInfo),
                    };

                    // Do the actual method invocation.
                    setValue.Call(Args);
                }
                else
                {
                    COMPlusThrowHR(DISP_E_MEMBERNOTFOUND);
                }

                break;
            }

            case Property:
            {
                // Make sure this invoke is actually for a property put or get.
                if (wFlags & (DISPATCH_METHOD | DISPATCH_PROPERTYGET))
                {
                    if (!IsPropertyAccessorVisible(false, &pObjs->MemberInfo))
                        COMPlusThrowHR(DISP_E_MEMBERNOTFOUND);

                    // Retrieve the method descriptor that will be called on.
                    MethodDesc *pMD = GetPropertyInfoMD(METHOD__PROPERTY__GET_VALUE, pObjs->MemberInfo->GetTypeHandle());
                    MethodDescCallSite getValue(pMD, &pObjs->MemberInfo);

                    // Prepare the arguments that will be passed to GetValue().
                    ARG_SLOT Args[] =
                    {
                            ObjToArgSlot(pObjs->MemberInfo),
                            ObjToArgSlot(pObjs->Target),
                            (ARG_SLOT) BindingFlags,
                            ObjToArgSlot(pObjs->OleAutBinder),
                            ObjToArgSlot(pObjs->ParamArray),
                            ObjToArgSlot(pObjs->CultureInfo),
                    };

                    // Do the actual method invocation.
                    pObjs->RetVal = getValue.Call_RetOBJECTREF(Args);
                }
                else if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
                {
                    if (!IsPropertyAccessorVisible(true, &pObjs->MemberInfo))
                        COMPlusThrowHR(DISP_E_MEMBERNOTFOUND);

                    // Retrieve the method descriptor that will be called on.
                    MethodDesc *pMD = GetPropertyInfoMD(METHOD__PROPERTY__SET_VALUE, pObjs->MemberInfo->GetTypeHandle());
                    MethodDescCallSite setValue(pMD, &pObjs->MemberInfo);

                    // Prepare the arguments that will be passed to SetValue().
                    ARG_SLOT Args[] =
                    {
                            ObjToArgSlot(pObjs->MemberInfo),
                            ObjToArgSlot(pObjs->Target),
                            ObjToArgSlot(pObjs->PropVal),
                            (ARG_SLOT) BindingFlags,
                            ObjToArgSlot(pObjs->OleAutBinder),
                            ObjToArgSlot(pObjs->ParamArray),
                            ObjToArgSlot(pObjs->CultureInfo),
                    };

                    // Do the actual method invocation.
                    setValue.Call(Args);
                }
                else
                {
                    COMPlusThrowHR(DISP_E_MEMBERNOTFOUND);
                }

                break;
            }

            case Method:
            {
                // Make sure this invoke is actually for a method. We also allow
                // prop gets since it is harmless and it allows the user a bit
                // more freedom.
                if (!(wFlags & (DISPATCH_METHOD | DISPATCH_PROPERTYGET)))
                    COMPlusThrowHR(DISP_E_MEMBERNOTFOUND);

                // Retrieve the method descriptor that will be called on.
                MethodDesc *pMD = GetMethodInfoMD(METHOD__METHOD__INVOKE, pObjs->MemberInfo->GetTypeHandle());
                MethodDescCallSite invoke(pMD, &pObjs->MemberInfo);

                // Prepare the arguments that will be passed to Invoke.
                ARG_SLOT Args[] =
                {
                        ObjToArgSlot(pObjs->MemberInfo),
                        ObjToArgSlot(pObjs->Target),
                        (ARG_SLOT) BindingFlags,
                        ObjToArgSlot(pObjs->OleAutBinder),
                        ObjToArgSlot(pObjs->ParamArray),
                        ObjToArgSlot(pObjs->CultureInfo),
                };

                // Do the actual method invocation.
                pObjs->RetVal = invoke.Call_RetOBJECTREF(Args);
                break;
            }

            default:
            {
                COMPlusThrowHR(E_UNEXPECTED);
            }
        }
    }
    else
    {
        // Convert the LCID into a CultureInfo.
        GetCultureInfoForLCID(lcid, &pObjs->CultureInfo);

        pObjs->ReflectionObj = GetReflectionObject();

        // Retrieve the method descriptor that will be called on.
        MethodDesc *pMD = GetInvokeMemberMD();
        MethodDescCallSite invokeMember(pMD, &pObjs->ReflectionObj);

        // Allocate the string that will contain the name of the member.
        if (!pDispMemberInfo)
        {
            WCHAR strTmp[64];
            _snwprintf_s(strTmp, ARRAY_SIZE(strTmp), _TRUNCATE, DISPID_NAME_FORMAT_STRING, id);
            pObjs->MemberName = (OBJECTREF)StringObject::NewString(strTmp);
        }
        else
        {
            pObjs->MemberName = (OBJECTREF)StringObject::NewString(pDispMemberInfo->GetName().GetUnicode());
        }

        // If there are named arguments, then set up the array of named arguments
        // to pass to InvokeMember.
        if (NumNamedArgs > 0)
            SetUpNamedParamArray(pDispMemberInfo, pSrcArgNames, NumNamedArgs, &pObjs->NamedArgArray);

        // If this is a PROPUT or a PROPPUTREF then we need to add the value
        // being set as the last argument in the argument array.
        if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
            pObjs->ParamArray->SetAt(NumParams, pObjs->PropVal);

        // Prepare the arguments that will be passed to Invoke.
        ARG_SLOT Args[] =
        {
                ObjToArgSlot(pObjs->ReflectionObj),
                ObjToArgSlot(pObjs->MemberName),
                (ARG_SLOT) BindingFlags,
                ObjToArgSlot(pObjs->OleAutBinder),
                ObjToArgSlot(pObjs->Target),
                ObjToArgSlot(pObjs->ParamArray),
                ObjToArgSlot(NULL),       // @TODO(DM): Look into setting the byref modifiers.
                ObjToArgSlot(pObjs->CultureInfo),
                ObjToArgSlot(pObjs->NamedArgArray),
        };

        // Do the actual method invocation.
        pObjs->RetVal = invokeMember.Call_RetOBJECTREF(Args);
    }


    //
    // Convert the return value and the byref arguments.
    //

    // If the property value is byref then convert it back.
    if (bPropValIsByref)
        MarshalParamManagedToNativeRef(pDispMemberInfo, NumArgs, &pObjs->PropVal, &pObjs->ByrefStaticArrayBackupPropVal, &pdp->rgvarg[0]);

    // Convert all the ByRef arguments back.
    for (int i = 0; i < NumByrefArgs; i++)
    {
        // Get the real parameter index for this arg.
        int iParamIndex = pManagedMethodParamIndexMap[i];

        if (!pDispMemberInfo || m_bInvokeUsingInvokeMember || !pDispMemberInfo->IsParamInOnly(iParamIndex))
        {
            pObjs->TmpObj = pObjs->ParamArray->GetAt(aByrefArgMngVariantIndex[i]);
            if (pSA != NULL && iParamIndex == NumParams -1)
            {
                // VarArg scenario
                // Here we only unmarshal the object whose corresponding VARIANT is VarArg
                OleVariant::MarshalVariantArrayComToOle((BASEARRAYREF*)&pObjs->TmpObj, (void *)(aByrefArgOleVariant[i]), NULL, TRUE, FALSE, TRUE, TRUE, -1);
            }
            else
            {
                MarshalParamManagedToNativeRef(pDispMemberInfo, iParamIndex, &pObjs->TmpObj, (OBJECTREF*)aByrefStaticArrayBackupObjHandle[i], aByrefArgOleVariant[i]);
            }
        }

        if (aByrefStaticArrayBackupObjHandle[i])
        {
            DestroyHandle(aByrefStaticArrayBackupObjHandle[i]);
            aByrefStaticArrayBackupObjHandle[i] = NULL;
        }
    }

    // Convert the return COM+ object to an OLE variant.
    if (pVarRes)
        MarshalReturnValueManagedToNative(pDispMemberInfo, &pObjs->RetVal, pVarRes);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void DispatchInfo::InvokeMemberDebuggerWrapper(
                                      DispatchMemberInfo*   pDispMemberInfo,
                                      InvokeObjects*        pObjs,
                                      int                   NumParams,
                                      int                   NumArgs,
                                      int                   NumNamedArgs,
                                      int&                  NumByrefArgs,
                                      int&                  iSrcArg,
                                      DISPID                id,
                                      DISPPARAMS*           pdp,
                                      VARIANT*              pVarRes,
                                      WORD                  wFlags,
                                      LCID                  lcid,
                                      DISPID*               pSrcArgNames,
                                      VARIANT*              pSrcArgs,
                                      OBJECTHANDLE*         aByrefStaticArrayBackupObjHandle,
                                      int*                  pManagedMethodParamIndexMap,
                                      VARIANT**             aByrefArgOleVariant,
                                      Frame *               pFrame)

{
    // Use static contracts b/c we have SEH.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    // @todo - we have a PAL_TRY/PAL_EXCEPT here as a general (cross-platform) way to get a 1st-pass
    // filter. If that's bad perf, we could inline an FS:0 handler for x86-only; and then inline
    // both this wrapper and the main body.

    struct Param : public NotifyOfCHFFilterWrapperParam
    {
        DispatchInfo*         pThis;
        DispatchMemberInfo*   pDispMemberInfo;
        InvokeObjects*        pObjs;
        int                   NumParams;
        int                   NumArgs;
        int                   NumNamedArgs;
        int&                  NumByrefArgs;
        int&                  iSrcArg;
        DISPID                id;
        DISPPARAMS*           pdp;
        VARIANT*              pVarRes;
        WORD                  wFlags;
        LCID                  lcid;
        DISPID*               pSrcArgNames;
        VARIANT*              pSrcArgs;
        OBJECTHANDLE*         aByrefStaticArrayBackupObjHandle;
        int*                  pManagedMethodParamIndexMap;
        VARIANT**             aByrefArgOleVariant;

        Param(int& _NumByrefArgs, int& _iSrcArg)
            : NumByrefArgs(_NumByrefArgs), iSrcArg(_iSrcArg)
        {}
    } param(NumByrefArgs, iSrcArg);

    param.pFrame = GetThread()->GetFrame(); // Inherited from NotifyOfCHFFilterWrapperParam
    param.pThis = this;
    param.pDispMemberInfo = pDispMemberInfo;
    param.pObjs = pObjs;
    param.NumParams = NumParams;
    param.NumArgs = NumArgs;
    param.NumNamedArgs = NumNamedArgs;
    //param.NumByrefArgs = NumByrefArgs;
    //param.iSrcArg = iSrcArg;
    param.id = id;
    param.pdp = pdp;
    param.pVarRes = pVarRes;
    param.wFlags = wFlags;
    param.lcid = lcid;
    param.pSrcArgNames = pSrcArgNames;
    param.pSrcArgs = pSrcArgs;
    param.aByrefStaticArrayBackupObjHandle = aByrefStaticArrayBackupObjHandle;
    param.pManagedMethodParamIndexMap = pManagedMethodParamIndexMap;
    param.aByrefArgOleVariant = aByrefArgOleVariant;

    PAL_TRY(Param *, pParam, &param)
    {
        pParam->pThis->InvokeMemberWorker(pParam->pDispMemberInfo,
                                          pParam->pObjs,
                                          pParam->NumParams,
                                          pParam->NumArgs,
                                          pParam->NumNamedArgs,
                                          pParam->NumByrefArgs,
                                          pParam->iSrcArg,
                                          pParam->id,
                                          pParam->pdp,
                                          pParam->pVarRes,
                                          pParam->wFlags,
                                          pParam->lcid,
                                          pParam->pSrcArgNames,
                                          pParam->pSrcArgs,
                                          pParam->aByrefStaticArrayBackupObjHandle,
                                          pParam->pManagedMethodParamIndexMap,
                                          pParam->aByrefArgOleVariant);
    }
    PAL_EXCEPT_FILTER(NotifyOfCHFFilterWrapper)
    {
        // Should never reach here b/c handler should always continue search.
        _ASSERTE(false);
    }
    PAL_ENDTRY
}

// Helper method that invokes the member with the specified DISPID.
HRESULT DispatchInfo::InvokeMember(SimpleComCallWrapper *pSimpleWrap, DISPID id, LCID lcid, WORD wFlags, DISPPARAMS *pdp, VARIANT *pVarRes, EXCEPINFO *pei, IServiceProvider *pspCaller, unsigned int *puArgErr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pSimpleWrap));
        PRECONDITION(CheckPointer(pdp, NULL_OK));
        PRECONDITION(CheckPointer(pVarRes, NULL_OK));
        PRECONDITION(CheckPointer(pei, NULL_OK));
        PRECONDITION(CheckPointer(pspCaller, NULL_OK));
        PRECONDITION(CheckPointer(puArgErr, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    int iSrcArg = -1;
    int iBaseErrorArg = 0;
    int NumArgs;
    int NumNamedArgs;
    int NumParams;
    InvokeObjects Objs;
    DISPID *pSrcArgNames = NULL;
    VARIANT *pSrcArgs = NULL;
    ULONG_PTR ulActCtxCookie = 0;

    //
    // Validate the arguments.
    //

    if (!pdp)
        return E_POINTER;
    if (!pdp->rgvarg && (pdp->cArgs > 0))
        return E_INVALIDARG;
    if (!pdp->rgdispidNamedArgs && (pdp->cNamedArgs > 0))
        return E_INVALIDARG;
    if (pdp->cNamedArgs > pdp->cArgs)
        return E_INVALIDARG;
    if ((int)pdp->cArgs < 0 || (int)pdp->cNamedArgs < 0)
        return E_INVALIDARG;


    //
    // Clear the out arguments before we start.
    //

    if (pVarRes)
        SafeVariantClear(pVarRes);
    if (puArgErr)
        *puArgErr = -1;


    //
    // Convert the default LCID's to actual LCID's.
    //

    if(lcid == LOCALE_SYSTEM_DEFAULT || lcid == 0)
        lcid = GetSystemDefaultLCID();

    if(lcid == LOCALE_USER_DEFAULT)
        lcid = GetUserDefaultLCID();

    //
    // Set the value of the variables we use internally.
    //

    NumArgs = pdp->cArgs;
    NumNamedArgs = pdp->cNamedArgs;
    memset(&Objs, 0, sizeof(InvokeObjects));

    if (wFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
    {
        // Since this invoke is for a property put or put ref we need to add 1 to
        // the iSrcArg to get the argument that is in error.
        iBaseErrorArg = 1;

        if (NumArgs < 1)
        {
            return DISP_E_BADPARAMCOUNT;
        }
        else
        {
            NumArgs--;
            pSrcArgs = &pdp->rgvarg[1];
        }

        if (NumNamedArgs < 1)
        {
            if (NumNamedArgs < 0)
                return DISP_E_BADPARAMCOUNT;

            // Verify if we really want to do this or return E_INVALIDARG instead.
            _ASSERTE(NumNamedArgs == 0);
            _ASSERTE(pSrcArgNames == NULL);
        }
        else
        {
            NumNamedArgs--;
            pSrcArgNames = &pdp->rgdispidNamedArgs[1];
        }
    }
    else
    {
        pSrcArgs = pdp->rgvarg;
        pSrcArgNames = pdp->rgdispidNamedArgs;
    }

    //
    // Do a lookup in the hashtable to find the DispatchMemberInfo for the DISPID.
    //

    DispatchMemberInfo *pDispMemberInfo = FindMember(id);
    if (!pDispMemberInfo || !pDispMemberInfo->GetMemberInfoObject())
    {
        pDispMemberInfo = NULL;
    }
    else if (pDispMemberInfo->IsNeutered())
    {
        COMPlusThrow(kInvalidOperationException);
    }

    //
    // If the member is not known then make sure that the DispatchInfo we have
    // supports unknown members.
    //

    if (m_bInvokeUsingInvokeMember)
    {
        // Since we do not have any information regarding the member then we
        // must assume the number of formal parameters matches the number of args.
        NumParams = NumArgs;
    }
    else
    {
        // If we haven't found the member then fail the invoke call.
        if (!pDispMemberInfo)
            return DISP_E_MEMBERNOTFOUND;

        // DISPATCH_CONSTRUCT only works when calling InvokeMember.
        if (wFlags & DISPATCH_CONSTRUCT)
            return E_INVALIDARG;

        if ((!(wFlags & (DISPATCH_METHOD | DISPATCH_PROPERTYGET))) && pDispMemberInfo->GetMemberType() == EnumMemberTypes::Method)
        {
            return DISP_E_MEMBERNOTFOUND;
        }

        // We have the member so retrieve the number of formal parameters.
        NumParams = pDispMemberInfo->GetNumParameters();

        if (pDispMemberInfo->IsLastParamOleVarArg())
        {
            // named args aren't allowed in a vararg function,
            // unless it's a lone DISPID_PROPERTYPUT (note that we already decrement
            // the value of NumNamedArgs for DISPID_PROPERTYPUT so that no special
            // check needed be done here for it
            // the logic is borrowed from the one in OLEAUT32!CTypeInfo2::Invoke
            if (NumNamedArgs > 0)
                return DISP_E_NONAMEDARGS;
        }
        else
        {
            // Make sure the number of arguments does not exceed the number of parameters.
            if(NumArgs > NumParams)
                return DISP_E_BADPARAMCOUNT;
        }

        // Validate that all the named arguments are known.
        for (iSrcArg = 0; iSrcArg < NumNamedArgs; iSrcArg++)
        {
            // There are some members we do not know about so we will call InvokeMember()
            // passing in the DISPID's directly so the caller can try to handle them.
            if (pSrcArgNames[iSrcArg] < 0 || pSrcArgNames[iSrcArg] >= NumParams)
                return DISP_E_MEMBERNOTFOUND;
        }
    }

    OBJECTREF pThrowable = NULL;

    //
    // The member is present so we need to convert the arguments and then do the
    // actual invocation.
    //
    GCPROTECT_BEGIN(pThrowable);
    GCPROTECT_BEGIN(Objs);
    {
        //
        // Allocate information used by the method.
        //

        int NumByrefArgs = 0;

        // Allocate the array of backup byref static array objects.
        size_t cbStaticArrayBackupObjHandle;
        if (!ClrSafeInt<size_t>::multiply(sizeof(OBJECTHANDLE *), NumArgs, cbStaticArrayBackupObjHandle))
            ThrowHR(COR_E_OVERFLOW);

        OBJECTHANDLE *aByrefStaticArrayBackupObjHandle = (OBJECTHANDLE *)_alloca(cbStaticArrayBackupObjHandle);
        memset(aByrefStaticArrayBackupObjHandle, 0, cbStaticArrayBackupObjHandle);

        // Allocate the array that maps method params to their indices.
        size_t cbManagedMethodParamIndexMap;
        if (!ClrSafeInt<size_t>::multiply(sizeof(int), NumArgs, cbManagedMethodParamIndexMap))
            ThrowHR(COR_E_OVERFLOW);

        int *pManagedMethodParamIndexMap = (int *)_alloca(cbManagedMethodParamIndexMap);

        // Allocate the array of byref objects.
        size_t cbByrefArgOleVariant;
        if (!ClrSafeInt<size_t>::multiply(sizeof(VARIANT *), NumArgs, cbByrefArgOleVariant))
            ThrowHR(COR_E_OVERFLOW);

        VARIANT **aByrefArgOleVariant = (VARIANT **)_alloca(cbByrefArgOleVariant);

        Objs.Target = pSimpleWrap->GetObjectRef();

        //
        // Invoke the method.
        //

        // The sole purpose of having this frame is to tell the debugger that we have a catch handler here
        // which may swallow managed exceptions.  The debugger needs this in order to send a
        // CatchHandlerFound (CHF) notification.
        FrameWithCookie<DebuggerU2MCatchHandlerFrame> catchFrame;
        EX_TRY
        {
            InvokeMemberDebuggerWrapper(pDispMemberInfo,
                                        &Objs,
                                        NumParams,
                                        NumArgs,
                                        NumNamedArgs,
                                        NumByrefArgs,
                                        iSrcArg,
                                        id,
                                        pdp,
                                        pVarRes,
                                        wFlags,
                                        lcid,
                                        pSrcArgNames,
                                        pSrcArgs,
                                        aByrefStaticArrayBackupObjHandle,
                                        pManagedMethodParamIndexMap,
                                        aByrefArgOleVariant,
                                        &catchFrame);
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH(RethrowTerminalExceptions)
        catchFrame.Pop();

        if (pThrowable != NULL)
        {
            // Do cleanup - make sure that return value and outgoing arguments are cleared
            if (pVarRes != NULL)
                SafeVariantClear(pVarRes);

            for (int i = 0; i < NumByrefArgs; i++)
            {
                if (!pDispMemberInfo || m_bInvokeUsingInvokeMember || !pDispMemberInfo->IsParamInOnly(i))
                {
                    // Out and in/out byref arguments are outgoing and should be cleared
                    CleanUpNativeParam(pDispMemberInfo, pManagedMethodParamIndexMap[i], (OBJECTREF *)aByrefStaticArrayBackupObjHandle[i], aByrefArgOleVariant[i]);
                }

                // Destroy all the handles we allocated for the byref static safe array's.
                if (aByrefStaticArrayBackupObjHandle[i] != NULL)
                {
                    DestroyHandle(aByrefStaticArrayBackupObjHandle[i]);
                    aByrefStaticArrayBackupObjHandle[i] = NULL;
                }
            }

            // Do HR conversion.
            hr = SetupErrorInfo(pThrowable);
            if (hr == COR_E_TARGETINVOCATION)
            {
                hr = DISP_E_EXCEPTION;
                if (pei)
                {
                    // Retrieve the exception iformation.
                    GetExcepInfoForInvocationExcep(pThrowable, pei);

                    // Clear the IErrorInfo on the current thread since it does contains
                    // information on the TargetInvocationException which conflicts with
                    // the information in the returned EXCEPINFO.
                    IErrorInfo *pErrInfo = NULL;
                    HRESULT hr2 = SafeGetErrorInfo(&pErrInfo);
                    _ASSERTE(hr2 == S_OK);
                    SafeRelease(pErrInfo);
                }
            }
            else if (hr == COR_E_OVERFLOW)
            {
                hr = DISP_E_OVERFLOW;
                if (iSrcArg != -1)
                {
                    if (puArgErr)
                        *puArgErr = iSrcArg + iBaseErrorArg;
                }
            }
            else if (hr == COR_E_INVALIDOLEVARIANTTYPE)
            {
                hr = DISP_E_BADVARTYPE;
                if (iSrcArg != -1)
                {
                    if (puArgErr)
                        *puArgErr = iSrcArg + iBaseErrorArg;
                }
            }
            else if (hr == COR_E_ARGUMENT)
            {
                hr = E_INVALIDARG;
                if (iSrcArg != -1)
                {
                    if (puArgErr)
                        *puArgErr = iSrcArg + iBaseErrorArg;
                }
            }
            else if (hr == COR_E_SAFEARRAYTYPEMISMATCH)
            {
                hr = DISP_E_TYPEMISMATCH;
                if (iSrcArg != -1)
                {
                    if (puArgErr)
                        *puArgErr = iSrcArg + iBaseErrorArg;
                }
            }
            else if (hr == COR_E_MISSINGMEMBER || hr == COR_E_MISSINGMETHOD)
            {
                hr = DISP_E_MEMBERNOTFOUND;

                // This exception should never occur while we are marshaling arguments.
                _ASSERTE(iSrcArg == -1);
            }
        }

        // If the culture was changed then restore it to the old culture.
        if (Objs.OldCultureInfo != NULL)
            Thread::SetCulture(&Objs.OldCultureInfo, FALSE);
    }
    GCPROTECT_END();
    GCPROTECT_END();
    return hr;
}

// Parameter marshaling helpers.
void DispatchInfo::MarshalParamNativeToManaged(DispatchMemberInfo *pMemberInfo, int iParam, VARIANT *pSrcVar, OBJECTREF *pDestObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (pMemberInfo && !m_bInvokeUsingInvokeMember)
        pMemberInfo->MarshalParamNativeToManaged(iParam, pSrcVar, pDestObj);
    else
        OleVariant::MarshalObjectForOleVariant(pSrcVar, pDestObj);
}

void DispatchInfo::MarshalParamManagedToNativeRef(DispatchMemberInfo *pMemberInfo, int iParam, OBJECTREF *pSrcObj, OBJECTREF *pBackupStaticArray, VARIANT *pRefVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMemberInfo, NULL_OK));
        PRECONDITION(pSrcObj != NULL);
        PRECONDITION(CheckPointer(pRefVar));
    }
    CONTRACTL_END;

    if (pBackupStaticArray && (*pBackupStaticArray != NULL))
    {
        // The contents of a static array can change, but not the array itself. If
        // the array has changed, then throw an exception.
        if (*pSrcObj != *pBackupStaticArray)
            COMPlusThrow(kInvalidOperationException, IDS_INVALID_REDIM);

        // Retrieve the element VARTYPE and method table.
        VARTYPE ElementVt = V_VT(pRefVar) & ~(VT_BYREF | VT_ARRAY);
        MethodTable *pElementMT = (*(BASEARRAYREF *)pSrcObj)->GetArrayElementTypeHandle().GetMethodTable();

        PCODE pStructMarshalStubAddress = NULL;
        GCPROTECT_BEGIN(*pSrcObj);
        if (ElementVt == VT_RECORD && pElementMT->IsBlittable())
        {
            GCX_PREEMP();
            pStructMarshalStubAddress = NDirect::GetEntryPointForStructMarshalStub(pElementMT);
        }
        GCPROTECT_END();

        // Convert the contents of the managed array into the original SAFEARRAY.
        OleVariant::MarshalSafeArrayForArrayRef((BASEARRAYREF *)pSrcObj, *V_ARRAYREF(pRefVar), ElementVt, pElementMT, pStructMarshalStubAddress);
    }
    else
{
    if (pMemberInfo && !m_bInvokeUsingInvokeMember)
        pMemberInfo->MarshalParamManagedToNativeRef(iParam, pSrcObj, pRefVar);
    else
        OleVariant::MarshalOleRefVariantForObject(pSrcObj, pRefVar);
}
}

void DispatchInfo::MarshalReturnValueManagedToNative(DispatchMemberInfo *pMemberInfo, OBJECTREF *pSrcObj, VARIANT *pDestVar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (pMemberInfo && !m_bInvokeUsingInvokeMember)
        pMemberInfo->MarshalReturnValueManagedToNative(pSrcObj, pDestVar);
    else
        OleVariant::MarshalOleVariantForObject(pSrcObj, pDestVar);
}

void DispatchInfo::CleanUpNativeParam(DispatchMemberInfo *pDispMemberInfo, int iParamIndex, OBJECTREF *pBackupStaticArray, VARIANT *pArgVariant)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pArgVariant != NULL);
    }
    CONTRACTL_END;

    EX_TRY
    {
        switch (V_VT(pArgVariant) & ~VT_BYREF)
        {
            case VT_I1:    case VT_I2:    case VT_I4:    case VT_I8:
            case VT_UI1:   case VT_UI2:   case VT_UI4:   case VT_UI8:
            case VT_INT:   case VT_UINT:  case VT_PTR:
            case VT_R4:    case VT_R8:    case VT_BOOL:
            case VT_CY:    case VT_DATE:
            case VT_ERROR: case VT_HRESULT:
            case VT_DECIMAL:
            {
                // the argument type is a value type - overwrite it with zeros
                UINT uSize = OleVariant::GetElementSizeForVarType(V_VT(pArgVariant) & ~VT_BYREF, NULL);
                FillMemory(V_BYREF(pArgVariant), uSize, 0);
                break;
            }

            default:
            {
                // marshal managed null into the VARIANT which works for reference types
                OBJECTREF Null = NULL;

                GCPROTECT_BEGIN(Null); // the local stays NULL, this is just to satisfy contracts
                MarshalParamManagedToNativeRef(pDispMemberInfo, iParamIndex, &Null, pBackupStaticArray, pArgVariant);
                GCPROTECT_END();
            }
        }
    }
    EX_CATCH
    {
        // if the argument was totally corrupted and cleanup failed, just swallow it and continue
    }
    EX_END_CATCH(SwallowAllExceptions)
}

void DispatchInfo::SetUpNamedParamArray(DispatchMemberInfo *pMemberInfo, DISPID *pSrcArgNames, int NumNamedArgs, PTRARRAYREF *pNamedParamArray)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMemberInfo, NULL_OK));
        PRECONDITION(CheckPointer(pSrcArgNames));
        PRECONDITION(pNamedParamArray != NULL);
    }
    CONTRACTL_END;

    PTRARRAYREF ParamArray = NULL;
    int NumParams = pMemberInfo ? pMemberInfo->GetNumParameters() : 0;
    int iSrcArg;
    int iDestArg;
    BOOL bGotParams = FALSE;

    GCPROTECT_BEGIN(ParamArray)
    {
        // Allocate the array of named parameters.
        *pNamedParamArray = (PTRARRAYREF)AllocateObjectArray(NumNamedArgs, g_pObjectClass);
        ParamArray = pMemberInfo ? pMemberInfo->GetParameters() : NULL;
        int numArrayComponents = (pMemberInfo && ParamArray != NULL)? (int)ParamArray->GetNumComponents() : 0;

        // Convert all the named parameters from DISPID's to string.
        for (iSrcArg = 0, iDestArg = 0; iSrcArg < NumNamedArgs; iSrcArg++, iDestArg++)
        {
            BOOL bParamNameSet = FALSE;

            // Check to see if the DISPID is one that we can map to a parameter name.
            if (pMemberInfo && pSrcArgNames[iSrcArg] >= 0 && pSrcArgNames[iSrcArg] < numArrayComponents)
            {
                // The DISPID is one that we assigned, map it back to its name.

                // If we managed to get the parameters and if the current ID maps
                // to an entry in the array.
                if (ParamArray != NULL && numArrayComponents > pSrcArgNames[iSrcArg])
                {
                    OBJECTREF ParamInfoObj = ParamArray->GetAt(pSrcArgNames[iSrcArg]);
                    GCPROTECT_BEGIN(ParamInfoObj)
                    {
                        // Retrieve the MD to use to retrieve the name of the parameter.
                        MethodDesc *pGetParamNameMD = MemberLoader::FindPropertyMethod(ParamInfoObj->GetMethodTable(), PARAMETERINFO_NAME_PROP, PropertyGet);
                        _ASSERTE(pGetParamNameMD && "Unable to find getter method for property ParameterInfo::Name");
                        MethodDescCallSite getParamName(pGetParamNameMD, &ParamInfoObj);

                        // Retrieve the name of the parameter.
                        ARG_SLOT GetNameArgs[] =
                        {
                            ObjToArgSlot(ParamInfoObj)
                        };

                        STRINGREF MemberNameObj = getParamName.Call_RetSTRINGREF(GetNameArgs);

                        // If we got a valid name back then use it as the named parameter.
                        if (MemberNameObj != NULL)
                        {
                            (*pNamedParamArray)->SetAt(iDestArg, (OBJECTREF)MemberNameObj);
                            bParamNameSet = TRUE;
                        }
                    }
                    GCPROTECT_END();
                }
            }

            // If we haven't set the param name yet, then set it to [DISP=XXXX].
            if (!bParamNameSet)
            {
                WCHAR wszTmp[64];

                _snwprintf_s(wszTmp, ARRAY_SIZE(wszTmp), _TRUNCATE, DISPID_NAME_FORMAT_STRING, pSrcArgNames[iSrcArg]);
                STRINGREF strTmp = StringObject::NewString(wszTmp);
                (*pNamedParamArray)->SetAt(iDestArg, (OBJECTREF)strTmp);
            }
        }
    }
    GCPROTECT_END();
}

VARIANT *DispatchInfo::RetrieveSrcVariant(VARIANT *pDispParamsVariant)
{
    CONTRACT (VARIANT*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDispParamsVariant));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // For VB6 compatibility reasons, if the VARIANT is a VT_BYREF | VT_VARIANT that
    // contains another VARIANT with VT_BYREF | VT_VARIANT, then we need to extract the
    // inner VARIANT and use it instead of the outer one. Note that if the inner VARIANT
    // is VT_BYREF | VT_VARIANT | VT_ARRAY, it will pass the below test too.
    if (V_VT(pDispParamsVariant) == (VT_VARIANT | VT_BYREF) &&
        (V_VT(V_VARIANTREF(pDispParamsVariant)) & (VT_TYPEMASK | VT_BYREF)) == (VT_VARIANT | VT_BYREF))
    {
        RETURN (V_VARIANTREF(pDispParamsVariant));
    }
    else
    {
        RETURN pDispParamsVariant;
    }
}


bool DispatchInfo::IsPropertyAccessorVisible(bool fIsSetter, OBJECTREF* pMemberInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pMemberInfo != NULL);
        PRECONDITION (IsProtectedByGCFrame (pMemberInfo));
    }
    CONTRACTL_END;

    MethodTable *pMemberInfoClass = (*pMemberInfo)->GetMethodTable();

    if (CoreLibBinder::IsClass(pMemberInfoClass, CLASS__PROPERTY))
    {
        // Get the property's MethodDesc
        MethodDesc* pMDForProperty = NULL;
        OBJECTREF method = NULL;
        GCPROTECT_BEGIN(method)
        {
            // Get the property method token
            BinderMethodID methodID;

            if (fIsSetter)
            {
                methodID = METHOD__PROPERTY__GET_SETTER;
            }
            else
            {
                methodID = METHOD__PROPERTY__GET_GETTER;
            }

            MethodDescCallSite getMethod(methodID, pMemberInfo);
            ARG_SLOT args[] =
            {
                ObjToArgSlot(*pMemberInfo),
                BoolToArgSlot(true)
            };
            method = getMethod.Call_RetOBJECTREF(args);

            if (method != NULL)
            {
                MethodDescCallSite getMethodHandle(METHOD__METHOD_BASE__GET_METHODDESC, &method);
                ARG_SLOT arg = ObjToArgSlot(method);
                pMDForProperty = (MethodDesc*) getMethodHandle.Call_RetLPVOID(&arg);
            }
        }
        GCPROTECT_END();

        if (pMDForProperty == NULL)
            return false;

        // Check to see if the new method is a property accessor.
        mdToken tkMember = mdTokenNil;
        MethodTable *pDeclaringMT = pMDForProperty->GetMethodTable();
        if (pMDForProperty->GetModule()->GetPropertyInfoForMethodDef(pMDForProperty->GetMemberDef(), &tkMember, NULL, NULL) == S_OK)
        {
            if (IsMemberVisibleFromCom(pDeclaringMT, tkMember, pMDForProperty->GetMemberDef()))
                return true;
        }
    }

    return false;
}

MethodDesc* DispatchInfo::GetFieldInfoMD(BinderMethodID Method, TypeHandle hndFieldInfoType)
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    MethodDesc *pMD;

    // If the current class is the standard implementation then return the cached method desc
    if (CoreLibBinder::IsClass(hndFieldInfoType.GetMethodTable(), CLASS__FIELD))
    {
        pMD = CoreLibBinder::GetMethod(Method);
    }
    else
    {
        pMD = MemberLoader::FindMethod(hndFieldInfoType.GetMethodTable(),
                CoreLibBinder::GetMethodName(Method), CoreLibBinder::GetMethodSig(Method));
    }
    _ASSERTE(pMD && "Unable to find specified FieldInfo method");

    // Return the specified method desc.
    RETURN pMD;
}

MethodDesc* DispatchInfo::GetPropertyInfoMD(BinderMethodID Method, TypeHandle hndPropInfoType)
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    MethodDesc *pMD;

    // If the current class is the standard implementation then return the cached method desc if present.
    if (CoreLibBinder::IsClass(hndPropInfoType.GetMethodTable(), CLASS__PROPERTY))
    {
        pMD = CoreLibBinder::GetMethod(Method);
    }
    else
    {
        pMD = MemberLoader::FindMethod(hndPropInfoType.GetMethodTable(),
                CoreLibBinder::GetMethodName(Method), CoreLibBinder::GetMethodSig(Method));
    }
    _ASSERTE(pMD && "Unable to find specified PropertyInfo method");

    // Return the specified method desc.
    RETURN pMD;
}

MethodDesc* DispatchInfo::GetMethodInfoMD(BinderMethodID Method, TypeHandle hndMethodInfoType)
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    MethodDesc *pMD;

    // If the current class is the standard implementation then return the cached method desc.
    if (CoreLibBinder::IsClass(hndMethodInfoType.GetMethodTable(), CLASS__METHOD))
    {
        pMD = CoreLibBinder::GetMethod(Method);
    }
    else
    {
        pMD = MemberLoader::FindMethod(hndMethodInfoType.GetMethodTable(),
                CoreLibBinder::GetMethodName(Method), CoreLibBinder::GetMethodSig(Method));
    }
    _ASSERTE(pMD && "Unable to find specified MethodInfo method");

    // Return the specified method desc.
    RETURN pMD;
}

MethodDesc* DispatchInfo::GetCustomAttrProviderMD(TypeHandle hndCustomAttrProvider)
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodTable *pMT = hndCustomAttrProvider.AsMethodTable();
    MethodDesc *pMD = pMT->GetMethodDescForInterfaceMethod(CoreLibBinder::GetMethod(METHOD__ICUSTOM_ATTR_PROVIDER__GET_CUSTOM_ATTRIBUTES), TRUE /* throwOnConflict */);

    // Return the specified method desc.
    RETURN pMD;
}

// This method synchronizes the DispatchInfo's members with the ones in the method tables type.
// The return value will be set to TRUE if the object was out of synch and members where
// added and it will be set to FALSE otherwise.
BOOL DispatchInfo::SynchWithManagedView()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    NewArrayHolder<WCHAR> strMemberName = NULL;
    NewHolder<ComMTMemberInfoMap> pMemberMap = NULL;

    // This represents the new member to add and it is also used to determine if members have
    // been added or not.
    NewHolder<DispatchMemberInfo> pMemberToAdd = NULL;

    Thread* pThread = SetupThreadNoThrow();
    if (pThread == NULL)
        return FALSE;

    // Determine if this is the first time we synch.
    BOOL bFirstSynch = (m_pFirstMemberInfo == NULL);

    // This method needs to be synchronized to make sure two threads don't try and
    // add members at the same time.
    CrstHolder ch(&m_lock);
    {
        // Make sure we switch to cooperative mode before we start.
        GCX_COOP();

        // Go through the list of member info's and find the end.
        DispatchMemberInfo **ppNextMember = &m_pFirstMemberInfo;
        while (*ppNextMember)
            ppNextMember = (*ppNextMember)->GetNextPtr();

        // Retrieve the member info map.
        pMemberMap = GetMemberInfoMap();

        for (int cPhase = 0; cPhase < 3; cPhase++)
        {
            PTRARRAYREF MemberArrayObj = NULL;
            GCPROTECT_BEGIN(MemberArrayObj);

            // Retrieve the appropriate array of members for the current phase.
            switch (cPhase)
            {
                case 0:
                    // Retrieve the array of properties.
                    MemberArrayObj = RetrievePropList();
                    break;

                case 1:
                    // Retrieve the array of fields.
                    MemberArrayObj = RetrieveFieldList();
                    break;

                case 2:
                    // Retrieve the array of methods.
                    MemberArrayObj = RetrieveMethList();
                    break;
            }

            // Retrieve the number of components in the member array.
            UINT NumComponents = 0;
            if (MemberArrayObj != NULL)
                NumComponents = MemberArrayObj->GetNumComponents();

            // Go through all the member info's in the array and see if they are already
            // in the DispatchExInfo.
            for (UINT i = 0; i < NumComponents; i++)
            {
                BOOL bMatch = FALSE;

                OBJECTREF CurrMemberInfoObj = MemberArrayObj->GetAt(i);
                GCPROTECT_BEGIN(CurrMemberInfoObj)
                {
                    DispatchMemberInfo *pCurrMemberInfo = m_pFirstMemberInfo;
                    while (pCurrMemberInfo)
                    {
                        // We can simply compare the OBJECTREF's.
                        if (CurrMemberInfoObj == pCurrMemberInfo->GetMemberInfoObject())
                        {
                            // We have found a match.
                            bMatch = TRUE;
                            break;
                        }

                        // Check the next member.
                        pCurrMemberInfo = pCurrMemberInfo->GetNext();
                    }

                    // If we have not found a match then we need to add the member info to the
                    // list of member info's that will be added to the DispatchExInfo.
                    if (!bMatch)
                    {
                        DISPID MemberID = DISPID_UNKNOWN;
                        BOOL bAddMember = FALSE;


                        //
                        // Attempt to retrieve the properties of the member.
                        //

                        ComMTMethodProps *pMemberProps = DispatchMemberInfo::GetMemberProps(CurrMemberInfoObj, pMemberMap);

                        //
                        // Determine if we are to add this member or not.
                        //

                        if (pMemberProps)
                            bAddMember = pMemberProps->bMemberVisible;
                        else
                            bAddMember = m_bAllowMembersNotInComMTMemberMap;

                        if (bAddMember)
                        {
                            //
                            // Retrieve the DISPID of the member.
                            //
                            MemberID = DispatchMemberInfo::GetMemberDispId(CurrMemberInfoObj, pMemberMap);

                            //
                            // If the member does not have an explicit DISPID or if the specified DISPID
                            // is already in use then we need to generate a dynamic DISPID for the member.
                            //

                            if ((MemberID == DISPID_UNKNOWN) || (FindMember(MemberID) != NULL))
                                MemberID = GenerateDispID();

                            //
                            // Retrieve the name of the member.
                            //

                            strMemberName = DispatchMemberInfo::GetMemberName(CurrMemberInfoObj, pMemberMap);

                            //
                            // Create a DispatchInfoMemberInfo that will represent the member.
                            //

                            SString sName(strMemberName);
                            pMemberToAdd = CreateDispatchMemberInfoInstance(MemberID, sName, CurrMemberInfoObj);

                            //
                            // Add the member to the end of the list.
                            //

                            *ppNextMember = pMemberToAdd;

                            // Update ppNextMember to be ready for the next new member.
                            ppNextMember = (*ppNextMember)->GetNextPtr();

                            // Add the member to the map. Note, the hash is unsynchronized, but we already have our lock
                            // so we're okay.
                            m_DispIDToMemberInfoMap.InsertValue(DispID2HashKey(MemberID), pMemberToAdd);
                            pMemberToAdd.SuppressRelease();
                        }
                    }
                }
                GCPROTECT_END();
            }

            GCPROTECT_END();
        }
        // GC mode toggles back here
    }
    // Check to see if any new members were added to the expando object.
    return pMemberToAdd ? TRUE : FALSE;

    // locks released and memory cleaned up here
}

// This method retrieves the OleAutBinder type.
OBJECTREF DispatchInfo::GetOleAutBinder()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // If we have already create the instance of the OleAutBinder then simply return it.
    if (m_hndOleAutBinder)
        return ObjectFromHandle(m_hndOleAutBinder);

    MethodTable *pOleAutBinderClass = CoreLibBinder::GetClass(CLASS__OLE_AUT_BINDER);

    // Allocate an instance of the OleAutBinder class.
    OBJECTREF OleAutBinder = AllocateObject(pOleAutBinderClass);

    // Keep a handle to the OleAutBinder instance.
    m_hndOleAutBinder = CreateGlobalHandle(OleAutBinder);

    return OleAutBinder;
}

BOOL DispatchInfo::VariantIsMissing(VARIANT *pOle)
{
    LIMITED_METHOD_CONTRACT;

    return (V_VT(pOle) == VT_ERROR) && (V_ERROR(pOle) == DISP_E_PARAMNOTFOUND);
}

LOADERHANDLE DispatchInfo::AllocateHandle(OBJECTREF objRef)
{
    WRAPPER_NO_CONTRACT;

    return m_pMT->GetLoaderAllocator()->AllocateHandle(objRef);
}

void DispatchInfo::FreeHandle(LOADERHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(handle != NULL);
    }
    CONTRACTL_END;

    PTR_LoaderAllocator loaderAllocator = m_pMT->GetLoaderAllocator();

    // If the loader isn't alive, we can't free the handle.
    if (loaderAllocator->AddReferenceIfAlive())
    {
        loaderAllocator->FreeHandle(handle);
        loaderAllocator->Release();
    }
}

OBJECTREF DispatchInfo::GetHandleValue(LOADERHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    return m_pMT->GetLoaderAllocator()->GetHandleValue(handle);
}

PTRARRAYREF DispatchInfo::RetrievePropList()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // return value
    PTRARRAYREF orRetVal;

    // Retrieve the exposed class object.
    OBJECTREF TargetObj = GetReflectionObject();

    GCPROTECT_BEGIN(TargetObj);
    MethodDescCallSite getProperties(METHOD__CLASS__GET_PROPERTIES, &TargetObj);

    // Prepare the arguments that will be passed to the method.
    ARG_SLOT Args[] =
    {
        ObjToArgSlot(TargetObj),
        (ARG_SLOT)BINDER_DefaultLookup
    };

    // Retrieve the array of members from the type object.
    orRetVal = (PTRARRAYREF) getProperties.Call_RetOBJECTREF(Args);

    GCPROTECT_END();

    return orRetVal;
}

PTRARRAYREF DispatchInfo::RetrieveFieldList()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // return value
    PTRARRAYREF orRetVal;

    // Retrieve the exposed class object.
    OBJECTREF TargetObj = GetReflectionObject();

    GCPROTECT_BEGIN(TargetObj);
    MethodDescCallSite getFields(METHOD__CLASS__GET_FIELDS, &TargetObj);

    // Prepare the arguments that will be passed to the method.
    ARG_SLOT Args[] =
    {
        ObjToArgSlot(TargetObj),
        (ARG_SLOT)BINDER_DefaultLookup
    };

    // Retrieve the array of members from the type object.
    orRetVal = (PTRARRAYREF) getFields.Call_RetOBJECTREF(Args);

    GCPROTECT_END();

    return orRetVal;
}

PTRARRAYREF DispatchInfo::RetrieveMethList()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // return value
    PTRARRAYREF orRetVal;

    // Retrieve the exposed class object.
    OBJECTREF TargetObj = GetReflectionObject();

    GCPROTECT_BEGIN(TargetObj);
    MethodDescCallSite getMethods(METHOD__CLASS__GET_METHODS, &TargetObj);

    // Prepare the arguments that will be passed to the method.
    ARG_SLOT Args[] =
    {
        ObjToArgSlot(TargetObj),
        (ARG_SLOT)BINDER_DefaultLookup
    };

    // Retrieve the array of members from the type object.
    orRetVal = (PTRARRAYREF) getMethods.Call_RetOBJECTREF(Args);

    GCPROTECT_END();

    return orRetVal;
}

// Virtual method to retrieve the InvokeMember method desc.
MethodDesc* DispatchInfo::GetInvokeMemberMD()
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN CoreLibBinder::GetMethod(METHOD__CLASS__INVOKE_MEMBER);
}

// Virtual method to retrieve the object associated with this DispatchInfo that
// implements IReflect.
OBJECTREF DispatchInfo::GetReflectionObject()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    return m_pMT->GetManagedClassObject();
}

// Virtual method to retrieve the member info map.
ComMTMemberInfoMap *DispatchInfo::GetMemberInfoMap()
{
    CONTRACT (ComMTMemberInfoMap*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;


    // Create the member info map.
    NewHolder<ComMTMemberInfoMap> pMemberInfoMap (new ComMTMemberInfoMap(m_pMT));

    // Initialize it.
    pMemberInfoMap->Init(sizeof(void*));

    pMemberInfoMap.SuppressRelease();
    RETURN pMemberInfoMap;
}

// Helper function to fill in an EXCEPINFO for an InvocationException.
void DispatchInfo::GetExcepInfoForInvocationExcep(OBJECTREF objException, EXCEPINFO *pei)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(objException != NULL);
        PRECONDITION(CheckPointer(pei));
    }
    CONTRACTL_END;

    MethodDesc *pMD;
    ExceptionData ED;
    OBJECTREF InnerExcep = NULL;

    // Initialize the EXCEPINFO.
    memset(pei, 0, sizeof(EXCEPINFO));
    pei->scode = E_FAIL;

    GCPROTECT_BEGIN(InnerExcep)
    GCPROTECT_BEGIN(objException)
    {
        // Retrieve the method desc to access the InnerException property.
        pMD = MemberLoader::FindPropertyMethod(objException->GetMethodTable(), EXCEPTION_INNER_PROP, PropertyGet);
        _ASSERTE(pMD && "Unable to find get method for proprety Exception.InnerException");
        MethodDescCallSite propGet(pMD, &objException);

        // Retrieve the value of the InnerException property.
        ARG_SLOT GetInnerExceptionArgs[] = { ObjToArgSlot(objException) };
        InnerExcep = propGet.Call_RetOBJECTREF(GetInnerExceptionArgs);

        // If the inner exception object is null then we can't get any info.
        if (InnerExcep != NULL)
        {
            // Retrieve the exception data for the inner exception.
            ExceptionNative::GetExceptionData(InnerExcep, &ED);
            pei->bstrSource = ED.bstrSource;
            pei->bstrDescription = ED.bstrDescription;
            pei->bstrHelpFile = ED.bstrHelpFile;
            pei->dwHelpContext = ED.dwHelpContext;
            pei->scode = ED.hr;
        }
    }
    GCPROTECT_END();
    GCPROTECT_END();
}

int DispatchInfo::ConvertInvokeFlagsToBindingFlags(int InvokeFlags)
{
    LIMITED_METHOD_CONTRACT;

    int BindingFlags = 0;

    // Check to see if DISPATCH_CONSTRUCT is set.
    if (InvokeFlags & DISPATCH_CONSTRUCT)
        BindingFlags |= BINDER_CreateInstance;

    // Check to see if DISPATCH_METHOD is set.
    if (InvokeFlags & DISPATCH_METHOD)
        BindingFlags |= BINDER_InvokeMethod;

    if (InvokeFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
    {
        // We are dealing with a PROPPUT or PROPPUTREF or both.
        if ((InvokeFlags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF)) == (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF))
        {
            BindingFlags |= BINDER_SetProperty;
        }
        else if (InvokeFlags & DISPATCH_PROPERTYPUT)
        {
            BindingFlags |= BINDER_PutDispProperty;
        }
        else
        {
            BindingFlags |= BINDER_PutRefDispProperty;
        }
    }
    else
    {
        // We are dealing with a PROPGET.
        if (InvokeFlags & DISPATCH_PROPERTYGET)
            BindingFlags |= BINDER_GetProperty;
    }

    return BindingFlags;
}

BOOL DispatchInfo::IsVariantByrefStaticArray(VARIANT *pOle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOle));
    }
    CONTRACTL_END;

    if (V_VT(pOle) & VT_BYREF && V_VT(pOle) & VT_ARRAY)
    {
        SAFEARRAY *pSafeArray = *V_ARRAYREF(pOle);
        if (pSafeArray && (pSafeArray->fFeatures & FADF_STATIC))
            return TRUE;
    }

    return FALSE;
}

DISPID DispatchInfo::GenerateDispID()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Find the next unused DISPID. Note, the hash is unsynchronized, but Gethash doesn't require synchronization.
    for (; (UPTR)m_DispIDToMemberInfoMap.Gethash(DispID2HashKey(m_CurrentDispID)) != -1; m_CurrentDispID++);
    return m_CurrentDispID++;
}

//--------------------------------------------------------------------------------
// The DispatchExInfo class implementation.

DispatchExInfo::DispatchExInfo(SimpleComCallWrapper *pSimpleWrapper, MethodTable *pMT)
: DispatchInfo(pMT)
, m_pSimpleWrapperOwner(pSimpleWrapper)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pSimpleWrapper));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    // Set the flags to specify the behavior of the base DispatchInfo class.
    m_bAllowMembersNotInComMTMemberMap = TRUE;
    m_bInvokeUsingInvokeMember = TRUE;
}

DispatchExInfo::~DispatchExInfo()
{
    WRAPPER_NO_CONTRACT;
}

// Methods to lookup members. These methods synch with the managed view if they fail to
// find the method.
DispatchMemberInfo* DispatchExInfo::SynchFindMember(DISPID DispID)
{
    CONTRACT (DispatchMemberInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    DispatchMemberInfo *pMemberInfo = FindMember(DispID);

    if (!pMemberInfo && SynchWithManagedView())
        pMemberInfo = FindMember(DispID);

    RETURN pMemberInfo;
}

DispatchMemberInfo* DispatchExInfo::SynchFindMember(SString& strName, BOOL bCaseSensitive)
{
    CONTRACT (DispatchMemberInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    DispatchMemberInfo *pMemberInfo = FindMember(strName, bCaseSensitive);

    if (!pMemberInfo && SynchWithManagedView())
        pMemberInfo = FindMember(strName, bCaseSensitive);

    RETURN pMemberInfo;
}

// Helper method that invokes the member with the specified DISPID. These methods synch
// with the managed view if they fail to find the method.
HRESULT DispatchExInfo::SynchInvokeMember(SimpleComCallWrapper *pSimpleWrap, DISPID id, LCID lcid, WORD wFlags, DISPPARAMS *pdp, VARIANT *pVarRes, EXCEPINFO *pei, IServiceProvider *pspCaller, unsigned int *puArgErr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Invoke the member.
    HRESULT hr = InvokeMember(pSimpleWrap, id, lcid, wFlags, pdp, pVarRes, pei, pspCaller, puArgErr);

    // If the member was not found then we need to synch and try again if the managed view has changed.
    if ((hr == DISP_E_MEMBERNOTFOUND) && SynchWithManagedView())
        hr = InvokeMember(pSimpleWrap, id, lcid, wFlags, pdp, pVarRes, pei, pspCaller, puArgErr);

    return hr;
}

DispatchMemberInfo* DispatchExInfo::GetFirstMember()
{
    CONTRACT (DispatchMemberInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // Start with the first member.
    DispatchMemberInfo **ppNextMemberInfo = &m_pFirstMemberInfo;

    // If the next member is not set we need to sink up with the expando object
    // itself to make sure that this member is really the last member and that
    // other members have not been added without us knowing.
    if (!(*ppNextMemberInfo))
    {
        if (SynchWithManagedView())
        {
            // New members have been added to the list and since they must be added
            // to the end the next member of the previous end of the list must
            // have been updated.
            _ASSERTE(*ppNextMemberInfo);
        }
    }

    // Now we need to make sure we skip any members that are deleted.
    while ((*ppNextMemberInfo) && !(*ppNextMemberInfo)->GetMemberInfoObject())
        ppNextMemberInfo = (*ppNextMemberInfo)->GetNextPtr();

    RETURN *ppNextMemberInfo;
}

DispatchMemberInfo* DispatchExInfo::GetNextMember(DISPID CurrMemberDispID)
{
    CONTRACT (DispatchMemberInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // Do a lookup in the hashtable to find the DispatchMemberInfo for the DISPID.
    DispatchMemberInfo *pDispMemberInfo = FindMember(CurrMemberDispID);
    if (!pDispMemberInfo)
        RETURN NULL;

    // Start from the next member.
    DispatchMemberInfo **ppNextMemberInfo = pDispMemberInfo->GetNextPtr();

    // If the next member is not set we need to sink up with the expando object
    // itself to make sure that this member is really the last member and that
    // other members have not been added without us knowing.
    if (!(*ppNextMemberInfo))
    {
        if (SynchWithManagedView())
        {
            // New members have been added to the list and since they must be added
            // to the end the next member of the previous end of the list must
            // have been updated.
            _ASSERTE(*ppNextMemberInfo);
        }
    }

    // Now we need to make sure we skip any members that are deleted.
    while ((*ppNextMemberInfo) && !(*ppNextMemberInfo)->GetMemberInfoObject())
        ppNextMemberInfo = (*ppNextMemberInfo)->GetNextPtr();

    RETURN *ppNextMemberInfo;
}

MethodDesc* DispatchExInfo::GetIReflectMD(BinderMethodID Method)
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodTable *pMT = m_pSimpleWrapperOwner->GetMethodTable();
    MethodDesc *pMD = pMT->GetMethodDescForInterfaceMethod(CoreLibBinder::GetMethod(Method), TRUE /* throwOnConflict */);

    // Return the specified method desc.
    RETURN pMD;
}

PTRARRAYREF DispatchExInfo::RetrievePropList()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PTRARRAYREF oPropList;

    // Retrieve the expando OBJECTREF.
    OBJECTREF TargetObj = GetReflectionObject();
    GCPROTECT_BEGIN(TargetObj);

    // Retrieve the GetMembers MethodDesc.
    MethodDesc *pMD = GetIReflectMD(METHOD__IREFLECT__GET_PROPERTIES);
    MethodDescCallSite getProperties(pMD, &TargetObj);

    // Prepare the arguments that will be passed to the method.
    ARG_SLOT Args[] =
    {
        ObjToArgSlot(TargetObj),
        (ARG_SLOT)BINDER_DefaultLookup
    };

    // Retrieve the array of members from the expando object
    oPropList = (PTRARRAYREF) getProperties.Call_RetOBJECTREF(Args);

    GCPROTECT_END();

    return oPropList;
}

PTRARRAYREF DispatchExInfo::RetrieveFieldList()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PTRARRAYREF oFieldList;

    // Retrieve the expando OBJECTREF.
    OBJECTREF TargetObj = GetReflectionObject();
    GCPROTECT_BEGIN(TargetObj);

    // Retrieve the GetMembers MethodDesc.
    MethodDesc *pMD = GetIReflectMD(METHOD__IREFLECT__GET_FIELDS);
    MethodDescCallSite getFields(pMD, &TargetObj);

    // Prepare the arguments that will be passed to the method.
    ARG_SLOT Args[] =
    {
        ObjToArgSlot(TargetObj),
        (ARG_SLOT)BINDER_DefaultLookup
    };

    // Retrieve the array of members from the expando object
    oFieldList = (PTRARRAYREF) getFields.Call_RetOBJECTREF(Args);

    GCPROTECT_END();

    return oFieldList;
}

PTRARRAYREF DispatchExInfo::RetrieveMethList()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PTRARRAYREF oMethList;

    // Retrieve the expando OBJECTREF.
    OBJECTREF TargetObj = GetReflectionObject();
    GCPROTECT_BEGIN(TargetObj);

    // Retrieve the GetMembers MethodDesc.
    MethodDesc *pMD = GetIReflectMD(METHOD__IREFLECT__GET_METHODS);
    MethodDescCallSite getMethods(pMD, &TargetObj);

    // Prepare the arguments that will be passed to the method.
    ARG_SLOT Args[] =
    {
        ObjToArgSlot(TargetObj),
        (ARG_SLOT)BINDER_DefaultLookup
    };

    // Retrieve the array of members from the expando object
    oMethList = (PTRARRAYREF) getMethods.Call_RetOBJECTREF(Args);

    GCPROTECT_END();

    return oMethList;
}

// Virtual method to retrieve the InvokeMember method desc.
MethodDesc* DispatchExInfo::GetInvokeMemberMD()
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN GetIReflectMD(METHOD__IREFLECT__INVOKE_MEMBER);
}

// Virtual method to retrieve the object associated with this DispatchInfo that
// implements IReflect.
OBJECTREF DispatchExInfo::GetReflectionObject()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Runtime type is very special. Because of how it is implemented, calling methods
    // through IDispatch on a runtime type object doesn't work like other IReflect implementors
    // work. To be able to invoke methods on the runtime type, we need to invoke them
    // on the runtime type that represents runtime type. This is why for runtime type,
    // we get the exposed class object and not the actual objectred contained in the
    // wrapper.

    if (m_pMT == g_pRuntimeTypeClass)
        return m_pMT->GetManagedClassObject();
    else
        return m_pSimpleWrapperOwner->GetObjectRef();
}

// Virtual method to retrieve the member info map.
ComMTMemberInfoMap *DispatchExInfo::GetMemberInfoMap()
{
    LIMITED_METHOD_CONTRACT;

    // There is no member info map for IExpando objects.
    return NULL;
}
