// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DispatchInfo.h
//

//
// Definition of helpers used to expose IDispatch
// and IDispatchEx to COM.
//


#ifndef _DISPATCHINFO_H
#define _DISPATCHINFO_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "vars.hpp"
#include "mlinfo.h"

// Forward declarations.
struct ComMethodTable;
struct SimpleComCallWrapper;
class ComMTMemberInfoMap;
struct ComMTMethodProps;
class DispParamMarshaler;
class MarshalInfo;
class DispatchInfo;
enum BinderMethodID;

// An enumeration of the types of managed MemberInfo's. This must stay in synch with
// the ones defined in MemberInfo.cs.
enum EnumMemberTypes
{
    Uninitted                           = 0x00,
	Constructor							= 0x01,
	Event								= 0x02,
	Field								= 0x04,
	Method								= 0x08,
	Property							= 0x10
};

enum {NUM_MEMBER_TYPES = 5};

enum CultureAwareStates
{
    Aware,
    NonAware,
    Unknown
};

// This structure represents a dispatch member.
struct DispatchMemberInfo
{
    DispatchMemberInfo(DispatchInfo *pDispInfo, DISPID DispID, SString& strName);
    ~DispatchMemberInfo();

    // Helper method to ensure the entry is initialized.
    void EnsureInitialized();

    BOOL IsNeutered()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_bNeutered) ? TRUE : FALSE;
    }

    // This method retrieves the ID's of the specified names.
    HRESULT GetIDsOfParameters(_In_reads_(NumNames) WCHAR **astrNames, int NumNames, DISPID *aDispIds, BOOL bCaseSensitive);

    // Accessors.
    PTRARRAYREF GetParameters();

    BOOL IsParamInOnly(int iIndex)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(m_pParamInOnly));
        }
        CONTRACTL_END;

        // Add one for the return type.
        return m_pParamInOnly[iIndex + 1];
    }

    // Inline accessors.
    BOOL IsCultureAware()
    {
        CONTRACT (BOOL)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(Unknown != m_CultureAwareState);
        }
        CONTRACT_END;

        RETURN (Aware == m_CultureAwareState);
    }

    EnumMemberTypes GetMemberType()
    {
        CONTRACT (EnumMemberTypes)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(Uninitted != m_enumType);
        }
        CONTRACT_END;

        RETURN m_enumType;
    }

    int GetNumParameters()
    {
        CONTRACT (int)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(m_iNumParams != -1);
        }
        CONTRACT_END;

        RETURN m_iNumParams;
    }

    BOOL IsLastParamOleVarArg()
    {
        LIMITED_METHOD_CONTRACT;
        return m_bLastParamOleVarArg;
    }

    void SetHandle(LOADERHANDLE objhnd)
    {
        m_hndMemberInfo = objhnd;
    }

    BOOL RequiresManagedObjCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        return m_bRequiresManagedCleanup;
    }

    OBJECTREF GetMemberInfoObject();

    // Parameter marshaling methods.
    void MarshalParamNativeToManaged(int iParam, VARIANT *pSrcVar, OBJECTREF *pDestObj);
    void MarshalParamManagedToNativeRef(int iParam, OBJECTREF *pSrcObj, VARIANT *pRefVar);
    void CleanUpParamManaged(int iParam, OBJECTREF *pObj);
    void MarshalReturnValueManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);

    // Static helper methods.
    static ComMTMethodProps *GetMemberProps(OBJECTREF MemberInfoObj, ComMTMemberInfoMap *pMemberMap);
    static DISPID GetMemberDispId(OBJECTREF MemberInfoObj, ComMTMemberInfoMap *pMemberMap);
    static LPWSTR GetMemberName(OBJECTREF MemberInfoObj, ComMTMemberInfoMap *pMemberMap);

private:
    // Private helpers.
    void Neuter();
    void Init();
    void DetermineMemberType();
    void DetermineParamCount();
    void DetermineCultureAwareness();
    void SetUpParamMarshalerInfo();
    void SetUpMethodMarshalerInfo(MethodDesc *pMeth, BOOL bReturnValueOnly);
    void SetUpDispParamMarshalerForMarshalInfo(int iParam, MarshalInfo *pInfo);
    void SetUpDispParamAttributes(int iParam, MarshalInfo* Info);
public:
    DISPID                  m_DispID;
    LOADERHANDLE            m_hndMemberInfo;
    DispParamMarshaler**    m_apParamMarshaler;
    BOOL*                   m_pParamInOnly;
    DispatchMemberInfo*     m_pNext;
    SString                 m_strName;
    EnumMemberTypes         m_enumType;
    int                     m_iNumParams;
    CultureAwareStates      m_CultureAwareState;
    BOOL                    m_bRequiresManagedCleanup;
    BOOL                    m_bInitialized;
    BOOL                    m_bNeutered;
    DispatchInfo*           m_pDispInfo;
    BOOL                    m_bLastParamOleVarArg;

private:
    static MethodTable*     s_pMemberTypes[NUM_MEMBER_TYPES];
    static EnumMemberTypes  s_memberTypes[NUM_MEMBER_TYPES];
    static int              s_iNumMemberTypesKnown;
};


struct InvokeObjects
{
    PTRARRAYREF ParamArray;
    PTRARRAYREF CleanUpArray;
    OBJECTREF MemberInfo;
    OBJECTREF OleAutBinder;
    OBJECTREF Target;
    OBJECTREF PropVal;
    OBJECTREF ByrefStaticArrayBackupPropVal;
    OBJECTREF RetVal;
    OBJECTREF TmpObj;
    OBJECTREF MemberName;
    OBJECTREF CultureInfo;
    OBJECTREF OldCultureInfo;
    PTRARRAYREF NamedArgArray;
    OBJECTREF ReflectionObj;
};

class DispatchInfo
{
public:
    // Encapsulate a CrstHolder, so that clients of our lock don't have to know
    // the details of our implementation.
    class LockHolder : public CrstHolder
    {
    public:
        LockHolder(DispatchInfo *pDI)
            : CrstHolder(&pDI->m_lock)
        {
            WRAPPER_NO_CONTRACT;
        }
    };

    // Constructor and destructor.
    DispatchInfo(MethodTable *pComMTOwner);
    virtual ~DispatchInfo();

    // Methods to lookup members.
    DispatchMemberInfo*     FindMember(DISPID DispID);
    DispatchMemberInfo*     FindMember(SString& strName, BOOL bCaseSensitive);

    // Helper method that invokes the member with the specified DISPID.
    HRESULT                 InvokeMember(SimpleComCallWrapper *pSimpleWrap, DISPID id, LCID lcid, WORD wFlags, DISPPARAMS *pdp, VARIANT *pVarRes, EXCEPINFO *pei, IServiceProvider *pspCaller, unsigned int *puArgErr);

    void                    InvokeMemberDebuggerWrapper(DispatchMemberInfo*   pDispMemberInfo,
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
                                               Frame *               pFrame);

    void                    InvokeMemberWorker(DispatchMemberInfo*   pDispMemberInfo,
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
                                               VARIANT**             aByrefArgOleVariant);

    // Methods to retrieve the cached MD's
    static MethodDesc*      GetFieldInfoMD(BinderMethodID Method, TypeHandle hndFieldInfoType);
    static MethodDesc*      GetPropertyInfoMD(BinderMethodID Method, TypeHandle hndPropInfoType);
    static MethodDesc*      GetMethodInfoMD(BinderMethodID Method, TypeHandle hndMethodInfoType);
    static MethodDesc*      GetCustomAttrProviderMD(TypeHandle hndCustomAttrProvider);

    // This method synchronizes the DispatchInfo's members with the ones in managed world.
    // The return value will be set to TRUE if the object was out of synch and members where
    // added and it will be set to FALSE otherwise.
    BOOL                    SynchWithManagedView();

    // This method retrieves the OleAutBinder type.
    static OBJECTREF        GetOleAutBinder();

    // Returns TRUE if the argument is "Missing"
    static BOOL             VariantIsMissing(VARIANT *pOle);

    LoaderAllocator*        GetLoaderAllocator()
    {
        return m_pMT->GetLoaderAllocator();
    }

protected:
    // Parameter marshaling helpers.
    void                    MarshalParamNativeToManaged(DispatchMemberInfo *pMemberInfo, int iParam, VARIANT *pSrcVar, OBJECTREF *pDestObj);
    void                    MarshalParamManagedToNativeRef(DispatchMemberInfo *pMemberInfo, int iParam, OBJECTREF *pSrcObj, OBJECTREF *pBackupStaticArray, VARIANT *pRefVar);
    void                    MarshalReturnValueManagedToNative(DispatchMemberInfo *pMemberInfo, OBJECTREF *pSrcObj, VARIANT *pDestVar);
    void                    CleanUpNativeParam(DispatchMemberInfo *pDispMemberInfo, int iParam, OBJECTREF *pBackupStaticArray, VARIANT *pArgVariant);

    // DISPID to named argument conversion helper.
    void                    SetUpNamedParamArray(DispatchMemberInfo *pMemberInfo, DISPID *pSrcArgNames, int NumNamedArgs, PTRARRAYREF *pNamedParamArray);

    // Helper method to retrieve the source VARIANT from the VARIANT contained in the disp params.
    VARIANT*                RetrieveSrcVariant(VARIANT *pDispParamsVariant);

    // Helper method to determine if a member is publicly accessible.
    bool                    IsPropertyAccessorVisible(bool fIsSetter, OBJECTREF* pMemberInfo);

    // Helper methods called from SynchWithManagedView() to retrieve the lists of members.
    virtual PTRARRAYREF     RetrievePropList();
    virtual PTRARRAYREF     RetrieveFieldList();
    virtual PTRARRAYREF     RetrieveMethList();

    // Virtual method to retrieve the InvokeMember method desc.
    virtual MethodDesc*     GetInvokeMemberMD();

    // Virtual method to retrieve the reflection object associated with the DispatchInfo.
    virtual OBJECTREF       GetReflectionObject();

    // Virtual method to retrieve the member info map.
    virtual ComMTMemberInfoMap* GetMemberInfoMap();

    // This method generates a DISPID for a new member.
    DISPID                  GenerateDispID();

    // Helper method to create an instance of a DispatchMemberInfo.
    virtual DispatchMemberInfo*  CreateDispatchMemberInfoInstance(DISPID DispID, SString& strMemberName, OBJECTREF MemberInfoObj);

    // Helper function to fill in an EXCEPINFO for an InvocationException.
    static void             GetExcepInfoForInvocationExcep(OBJECTREF objException, EXCEPINFO *pei);

    // This helper method converts the IDispatch::Invoke flags to BindingFlags.
    static int              ConvertInvokeFlagsToBindingFlags(int InvokeFlags);

    // Helper function to determine if a VARIANT is a byref static safe array.
    static BOOL             IsVariantByrefStaticArray(VARIANT *pOle);

    MethodTable*            m_pMT;
    PtrHashMap              m_DispIDToMemberInfoMap;
    DispatchMemberInfo*     m_pFirstMemberInfo;
    Crst                    m_lock;
    int                     m_CurrentDispID;
    BOOL                    m_bAllowMembersNotInComMTMemberMap;
    BOOL                    m_bInvokeUsingInvokeMember;

    static OBJECTHANDLE     m_hndOleAutBinder;
};



class DispatchExInfo : public DispatchInfo
{
public:
    // Constructor and destructor.
    DispatchExInfo(SimpleComCallWrapper *pSimpleWrapper, MethodTable *pMT);
    virtual ~DispatchExInfo();

    // Methods to lookup members. These methods synch with the managed view if they fail to
    // find the method.
    DispatchMemberInfo*     SynchFindMember(DISPID DispID);
    DispatchMemberInfo*     SynchFindMember(SString& strName, BOOL bCaseSensitive);

    // Helper method that invokes the member with the specified DISPID. These methods synch
    // with the managed view if they fail to find the method.
    HRESULT                 SynchInvokeMember(SimpleComCallWrapper *pSimpleWrap, DISPID id, LCID lcid, WORD wFlags, DISPPARAMS *pdp, VARIANT *pVarRes, EXCEPINFO *pei, IServiceProvider *pspCaller, unsigned int *puArgErr);

    // Helper method to create an instance of a DispatchMemberInfo.
    virtual DispatchMemberInfo*  CreateDispatchMemberInfoInstance(DISPID DispID, SString& strMemberName, OBJECTREF MemberInfoObj);

    // These methods return the first and next non deleted members.
    DispatchMemberInfo*     GetFirstMember();
    DispatchMemberInfo*     GetNextMember(DISPID CurrMemberDispID);

    // Methods to retrieve the cached MD's
    MethodDesc*             GetIReflectMD(BinderMethodID Method);

private:
    // Helper methods called from SynchWithManagedView() to retrieve the lists of members.
    virtual PTRARRAYREF     RetrievePropList();
    virtual PTRARRAYREF     RetrieveFieldList();
    virtual PTRARRAYREF     RetrieveMethList();

    // Virtual method to retrieve the InvokeMember method desc.
    virtual MethodDesc*     GetInvokeMemberMD();

    // Virtual method to retrieve the reflection object associated with the DispatchInfo.
    virtual OBJECTREF       GetReflectionObject();

    // Virtual method to retrieve the member info map.
    virtual ComMTMemberInfoMap* GetMemberInfoMap();

    SimpleComCallWrapper*   m_pSimpleWrapperOwner;
};

#endif // _DISPATCHINFO_H
