// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//****************************************************************************
//  File: metadata.h
//

//
//  Notes:
//   Common includes for EE & metadata internal. This file contains
//   definition of CorMetaDataScope
//****************************************************************************

#ifndef _METADATA_H_
#define _METADATA_H_

#include "ex.h"

class CorProfileData;
class IMetaModelCommon;
class MDInternalRW;
class UTSemReadWrite;

inline int IsGlobalMethodParentTk(mdTypeDef td)
{
    LIMITED_METHOD_CONTRACT;

    return (td == mdTypeDefNil || td == mdTokenNil);
}

typedef enum CorInternalStates
{
    tdNoTypes               = 0x00000000,
    tdAllAssemblies         = 0x00000001,
    tdAllTypes              = 0xffffffff,
} CorInternalStates;

//
// MetaData custom value names.
//
enum CorIfaceAttr
{
    ifDual        = 0,            // Interface derives from IDispatch.
    ifVtable      = 1,            // Interface derives from IUnknown.
    ifDispatch    = 2,            // Interface is a dispinterface.
    ifInspectable = 3,            // Interface derives from IInspectable.
    ifLast        = 4,            // The last member of the enum.
};

inline BOOL IsDispatchBasedItf(CorIfaceAttr ifaceAttr)
{
    return (ifaceAttr == ifDual || ifaceAttr == ifDispatch);
}

enum CorClassIfaceAttr
{
    clsIfNone      = 0,                 // No class interface is generated.
    clsIfAutoDisp  = 1,                 // A dispatch only class interface is generated.
    clsIfAutoDual  = 2,                 // A dual class interface is generated.
    clsIfLast      = 3,                 // The last member of the enum.
};

//
// The default values for the COM interface and class interface types.
//
#define DEFAULT_COM_INTERFACE_TYPE ifDual
#define DEFAULT_CLASS_INTERFACE_TYPE clsIfAutoDisp

#define HANDLE_UNCOMPRESSED(func) (E_FAIL)
#define HANDLE_UNCOMPRESSED_BOOL(func) (false)

class TOKENLIST : public CDynArray<mdToken>
{
};


typedef enum tagEnumType
{
    MDSimpleEnum        = 0x0,                  // simple enumerator that doesn't allocate memory

    // You could get this kind of enum if you perform a non-simple query (such as EnumMethodWithName).
    //
    MDDynamicArrayEnum = 0x2,                   // dynamic array that holds tokens
} EnumType;

//*****************************************
// Enumerator used by MetaDataInternal
//*****************************************
struct HENUMInternal
{
    DWORD       m_tkKind;                   // kind of tables that the enum is holding the result
    uint32_t    m_ulCount;                  // count of total entries holding by the enumerator

    EnumType    m_EnumType;

    struct {
        uint32_t   m_ulStart;
        uint32_t   m_ulEnd;
        uint32_t   m_ulCur;
    } u;

    // m_cursor will go away when we no longer support running EE with uncompressed
    // format. WHEN WE REMOVE THIS, REMOVE ITS VESTIAGES FROM ZeroEnum as well
    //
    union {
        void*       m_alignpad;                 // The first item is m_cursor[] is a pointer
        char        m_cursor[32];               // cursor holding query result for read/write mode
    };

    // TOKENLIST    daTKList;               // dynamic arrays of token list
    HENUMInternal() : m_EnumType(MDSimpleEnum) { LIMITED_METHOD_DAC_CONTRACT; }

    // in-place initialization
    static void InitDynamicArrayEnum(
        HENUMInternal   *pEnum);            // HENUMInternal to be initialized

    static void InitSimpleEnum(
        DWORD           tkKind,             // kind of token that we are iterating
        ULONG           ridStart,           // starting rid
        ULONG           ridEnd,             // end rid
        HENUMInternal   *pEnum);            // HENUMInternal to be initialized

    // Specialized helper which should be better than always calling memset
    inline
    static void ZeroEnum(
        HENUMInternal   *pEnum)
    {
        // we use this to avoid the memset that will happen otherwise.
        // this should be inlined in its caller. we are seeing a large
        // number of calls to memset from MDInternalRO::EnumPermissionSetsInit
        // on x64 which we can eliminate with this code.
        pEnum->m_tkKind = 0;
        pEnum->m_ulCount = 0;
        pEnum->m_EnumType =  MDSimpleEnum;
        pEnum->u.m_ulStart = 0;
        pEnum->u.m_ulEnd = 0;
        pEnum->u.m_ulCur = 0;

        // TODO: remove this when we remove m_cursor from the HENUMInternal structure
        _ASSERTE(IS_ALIGNED(pEnum->m_cursor, sizeof(DWORD)));
        _ASSERTE((sizeof(HENUMInternal) - offsetof(HENUMInternal, m_cursor)) == (8 * sizeof(DWORD)));

        DWORD* pBuffer = (DWORD*)pEnum->m_cursor;
        pBuffer[0] = 0;
        pBuffer[1] = 0;
        pBuffer[2] = 0;
        pBuffer[3] = 0;
        pBuffer[4] = 0;
        pBuffer[5] = 0;
        pBuffer[6] = 0;
        pBuffer[7] = 0;
    }

    // This will only clear the content of enum and will not free the memory of enum
    static void ClearEnum(
        HENUMInternal   *pmdEnum);

    // create a HENUMInternal. This will allocate the memory
    __checkReturn
    static HRESULT CreateSimpleEnum(
        DWORD           tkKind,             // kind of token that we are iterating
        ULONG           ridStart,           // starting rid
        ULONG           ridEnd,             // end rid
        HENUMInternal   **ppEnum);          // return the created HENUMInternal

    __checkReturn
    static HRESULT CreateDynamicArrayEnum(
        DWORD           tkKind,             // kind of token that we are iterating
        HENUMInternal   **ppEnum);          // return the created HENUMInternal

    // Destory Enum. This will free the memory
    static void DestroyEnum(
        HENUMInternal   *pmdEnum);

    static void DestroyEnumIfEmpty(
        HENUMInternal   **ppEnum);          // reset the enumerator pointer to NULL if empty

    __checkReturn
    static HRESULT EnumWithCount(
        HENUMInternal   *pEnum,             // enumerator
        ULONG           cMax,               // max tokens that caller wants
        mdToken         rTokens[],          // output buffer to fill the tokens
        ULONG           *pcTokens);         // number of tokens fill to the buffer upon return

    __checkReturn
    static HRESULT EnumWithCount(
        HENUMInternal   *pEnum,             // enumerator
        ULONG           cMax,               // max tokens that caller wants
        mdToken         rTokens1[],         // first output buffer to fill the tokens
        mdToken         rTokens2[],         // second output buffer to fill the tokens
        ULONG           *pcTokens);         // number of tokens fill to the buffer upon return

    __checkReturn
    static HRESULT AddElementToEnum(
        HENUMInternal   *pEnum,             // return the created HENUMInternal
        mdToken         tk);                // token to fill

    //*****************************************
    // Get next value contained in the enumerator
    //*****************************************
    static bool EnumNext(
        HENUMInternal   *phEnum,            // [IN] the enumerator to retrieve information
        mdToken         *ptk);              // [OUT] token to scope the search

    __checkReturn
    static HRESULT GetCount(
        HENUMInternal   *phEnum,            // [IN] the enumerator to retrieve information
        ULONG           *pCount);           // ]OUT] the index of the desired item

    __checkReturn
    static HRESULT GetElement(
        HENUMInternal   *phEnum,            // [IN] the enumerator to retrieve information
        ULONG           ix,                 // ]IN] the index of the desired item
        mdToken         *ptk);              // [OUT] token to fill

};



//*****************************************
// Default Value for field, param or property. Returned by GetDefaultValue
//*****************************************
typedef struct _MDDefaultValue
{
#if BIGENDIAN
    _MDDefaultValue(void)
    {
        m_bType = ELEMENT_TYPE_END;
    }
    ~_MDDefaultValue(void)
    {
        if (m_bType == ELEMENT_TYPE_STRING)
        {
            delete[] m_wzValue;
        }
    }
#endif

    // type of default value
    BYTE            m_bType;                // CorElementType for the default value

    // the default value
    union
    {
        BOOL        m_bValue;               // ELEMENT_TYPE_BOOLEAN
        CHAR        m_cValue;               // ELEMENT_TYPE_I1
        BYTE        m_byteValue;            // ELEMENT_TYPE_UI1
        SHORT       m_sValue;               // ELEMENT_TYPE_I2
        USHORT      m_usValue;              // ELEMENT_TYPE_UI2
        LONG        m_lValue;               // ELEMENT_TYPE_I4
        ULONG       m_ulValue;              // ELEMENT_TYPE_UI4
        LONGLONG    m_llValue;              // ELEMENT_TYPE_I8
        ULONGLONG   m_ullValue;             // ELEMENT_TYPE_UI8
        FLOAT       m_fltValue;             // ELEMENT_TYPE_R4
        DOUBLE      m_dblValue;             // ELEMENT_TYPE_R8
        LPCWSTR     m_wzValue;              // ELEMENT_TYPE_STRING
        IUnknown    *m_unkValue;            // ELEMENT_TYPE_CLASS
    };
    ULONG   m_cbSize;   // default value size (for blob)

} MDDefaultValue;



//*****************************************
// structure use to in GetAllEventAssociates and GetAllPropertyAssociates
//*****************************************
typedef struct
{
    mdMethodDef m_memberdef;
    DWORD       m_dwSemantics;
} ASSOCIATE_RECORD;


//
// structure use to retrieve class layout informaiton
//
typedef struct
{
    RID         m_ridFieldCur;          // indexing to the field table
    RID         m_ridFieldEnd;          // end index to field table
} MD_CLASS_LAYOUT;


// Structure for describing the Assembly MetaData.
typedef struct
{
    USHORT      usMajorVersion;         // Major Version.
    USHORT      usMinorVersion;         // Minor Version.
    USHORT      usBuildNumber;          // Build Number.
    USHORT      usRevisionNumber;       // Revision Number.
    LPCSTR      szLocale;               // Locale.
} AssemblyMetaDataInternal;



// Callback definition for comparing signatures.
// (*PSIGCOMPARE) (BYTE ScopeSignature[], DWORD ScopeSignatureLength,
//                 BYTE ExternalSignature[], DWORD ExternalSignatureLength,
//                 void* SignatureData);
typedef BOOL (*PSIGCOMPARE)(PCCOR_SIGNATURE, DWORD, PCCOR_SIGNATURE, DWORD, void*);


// {1B119F60-C507-4024-BB39-F8223FB3E1FD}
EXTERN_GUID(IID_IMDInternalImport, 0x1b119f60, 0xc507, 0x4024, 0xbb, 0x39, 0xf8, 0x22, 0x3f, 0xb3, 0xe1, 0xfd);

#undef  INTERFACE
#define INTERFACE IMDInternalImport
DECLARE_INTERFACE_(IMDInternalImport, IUnknown)
{
    //*****************************************************************************
    // return the count of entries of a given kind in a scope
    // For example, pass in mdtMethodDef will tell you how many MethodDef
    // contained in a scope
    //*****************************************************************************
    STDMETHOD_(ULONG, GetCountWithTokenKind)(// return hresult
        DWORD       tkKind) PURE;           // [IN] pass in the kind of token.

    //*****************************************************************************
    // enumerator for typedef
    //*****************************************************************************
    __checkReturn
    STDMETHOD(EnumTypeDefInit)(             // return hresult
        HENUMInternal *phEnum) PURE;        // [OUT] buffer to fill for enumerator data

    //*****************************************************************************
    // enumerator for MethodImpl
    //*****************************************************************************
    __checkReturn
    STDMETHOD(EnumMethodImplInit)(          // return hresult
        mdTypeDef       td,                 // [IN] TypeDef over which to scope the enumeration.
        HENUMInternal   *phEnumBody,        // [OUT] buffer to fill for enumerator data for MethodBody tokens.
        HENUMInternal   *phEnumDecl) PURE;  // [OUT] buffer to fill for enumerator data for MethodDecl tokens.

    ULONG EnumMethodImplGetCount(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
    {
        return phEnumBody->m_ulCount;
    }

    STDMETHOD_(void, EnumMethodImplReset)(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl) PURE;  // [IN] MethodDecl enumerator.

    __checkReturn
    STDMETHOD(EnumMethodImplNext)(          // return hresult (S_OK = TRUE, S_FALSE = FALSE or error code)
        HENUMInternal   *phEnumBody,        // [IN] input enum for MethodBody
        HENUMInternal   *phEnumDecl,        // [IN] input enum for MethodDecl
        mdToken         *ptkBody,           // [OUT] return token for MethodBody
        mdToken         *ptkDecl) PURE;     // [OUT] return token for MethodDecl

    STDMETHOD_(void, EnumMethodImplClose)(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl) PURE;  // [IN] MethodDecl enumerator.

    //*****************************************
    // Enumerator helpers for memberdef, memberref, interfaceimp,
    // event, property, exception, param
    //*****************************************

    __checkReturn
    STDMETHOD(EnumGlobalFunctionsInit)(     // return hresult
        HENUMInternal   *phEnum) PURE;      // [OUT] buffer to fill for enumerator data

    __checkReturn
    STDMETHOD(EnumGlobalFieldsInit)(        // return hresult
        HENUMInternal   *phEnum) PURE;      // [OUT] buffer to fill for enumerator data

    __checkReturn
    STDMETHOD(EnumInit)(                    // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        mdToken     tkParent,               // [IN] token to scope the search
        HENUMInternal *phEnum) PURE;        // [OUT] the enumerator to fill

    __checkReturn
    STDMETHOD(EnumAllInit)(                 // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        HENUMInternal *phEnum) PURE;        // [OUT] the enumerator to fill

    bool EnumNext(
        HENUMInternal *phEnum,              // [IN] the enumerator to retrieve information
        mdToken     *ptk)                   // [OUT] token to scope the search
    {
        _ASSERTE(phEnum && ptk);
        if (phEnum->u.m_ulCur >= phEnum->u.m_ulEnd)
            return false;

        if ( phEnum->m_EnumType == MDSimpleEnum )
        {
            *ptk = phEnum->u.m_ulCur | phEnum->m_tkKind;
            phEnum->u.m_ulCur++;
        }
        else
        {
            TOKENLIST       *pdalist = (TOKENLIST *)&(phEnum->m_cursor);

            _ASSERTE( phEnum->m_EnumType == MDDynamicArrayEnum );
            *ptk = *( pdalist->Get(phEnum->u.m_ulCur++) );
        }
        return true;
    }

    ULONG EnumGetCount(
        HENUMInternal *phEnum)        // [IN] the enumerator to retrieve information
    {
        _ASSERTE(phEnum);
        return phEnum->m_ulCount;
    }

    void EnumReset(
        HENUMInternal *phEnum)        // [IN] the enumerator to be reset
    {
        _ASSERTE(phEnum);
        _ASSERTE( phEnum->m_EnumType == MDSimpleEnum || phEnum->m_EnumType == MDDynamicArrayEnum);

        phEnum->u.m_ulCur = phEnum->u.m_ulStart;
    } // MDInternalRW::EnumReset

    void EnumClose(
        HENUMInternal *phEnum)        // [IN] the enumerator to be closed
    {
        _ASSERTE( phEnum->m_EnumType == MDSimpleEnum ||
            phEnum->m_EnumType == MDDynamicArrayEnum);
        if (phEnum->m_EnumType == MDDynamicArrayEnum)
            HENUMInternal::ClearEnum(phEnum);
    }

    //*****************************************
    // Enumerator helpers for CustomAttribute
    //*****************************************
    __checkReturn
    STDMETHOD(EnumCustomAttributeByNameInit)(// return S_FALSE if record not found
        mdToken     tkParent,               // [IN] token to scope the search
        LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
        HENUMInternal *phEnum) PURE;        // [OUT] the enumerator to fill

    //*****************************************
    // Nagivator helper to navigate back to the parent token given a token.
    // For example, given a memberdef token, it will return the containing typedef.
    //
    // the mapping is as following:
    //  ---given child type---------parent type
    //  mdMethodDef                 mdTypeDef
    //  mdFieldDef                  mdTypeDef
    //  mdInterfaceImpl             mdTypeDef
    //  mdParam                     mdMethodDef
    //  mdProperty                  mdTypeDef
    //  mdEvent                     mdTypeDef
    //
    //*****************************************
    __checkReturn
    STDMETHOD(GetParentToken)(
        mdToken     tkChild,                // [IN] given child token
        mdToken     *ptkParent) PURE;       // [OUT] returning parent

    //*****************************************
    // Custom value helpers
    //*****************************************
    __checkReturn
    STDMETHOD(GetCustomAttributeProps)(     // S_OK or error.
        mdCustomAttribute at,               // [IN] The attribute.
        mdToken     *ptkType) PURE;         // [OUT] Put attribute type here.

    __checkReturn
    STDMETHOD(GetCustomAttributeAsBlob)(
        mdCustomAttribute cv,               // [IN] given custom value token
        void const  **ppBlob,               // [OUT] return the pointer to internal blob
        ULONG       *pcbSize) PURE;         // [OUT] return the size of the blob

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD (GetScopeProps)(
        LPCSTR      *pszName,               // [OUT] scope name
        GUID        *pmvid) PURE;           // [OUT] version id

    // finding a particular method
    __checkReturn
    STDMETHOD(FindMethodDef)(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmd) PURE;             // [OUT] matching memberdef

    // return a iSeq's param given a MethodDef
    __checkReturn
    STDMETHOD(FindParamOfMethod)(           // S_OK or error.
        mdMethodDef md,                     // [IN] The owning method of the param.
        ULONG       iSeq,                   // [IN] The sequence # of the param.
        mdParamDef  *pparamdef) PURE;       // [OUT] Put ParamDef token here.

    //*****************************************
    //
    // GetName* functions
    //
    //*****************************************

    // return the name and namespace of typedef
    __checkReturn
    STDMETHOD(GetNameOfTypeDef)(
        mdTypeDef   classdef,               // given classdef
        LPCSTR      *pszname,               // return class name(unqualified)
        LPCSTR      *psznamespace) PURE;    // return the name space name

    __checkReturn
    STDMETHOD(GetIsDualOfTypeDef)(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pDual) PURE;           // [OUT] return dual flag here.

    __checkReturn
    STDMETHOD(GetIfaceTypeOfTypeDef)(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pIface) PURE;          // [OUT] 0=dual, 1=vtable, 2=dispinterface

    // get the name of either methoddef
    __checkReturn
    STDMETHOD(GetNameOfMethodDef)(  // return the name of the memberdef in UTF8
        mdMethodDef md,             // given memberdef
        LPCSTR     *pszName) PURE;

    __checkReturn
    STDMETHOD(GetNameAndSigOfMethodDef)(
        mdMethodDef      methoddef,         // [IN] given memberdef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName) PURE;

    // return the name of a FieldDef
    __checkReturn
    STDMETHOD(GetNameOfFieldDef)(
        mdFieldDef fd,              // given memberdef
        LPCSTR    *pszName) PURE;

    // return the name of typeref
    __checkReturn
    STDMETHOD(GetNameOfTypeRef)(
        mdTypeRef   classref,               // [IN] given typeref
        LPCSTR      *psznamespace,          // [OUT] return typeref name
        LPCSTR      *pszname) PURE;         // [OUT] return typeref namespace

    // return the resolutionscope of typeref
    __checkReturn
    STDMETHOD(GetResolutionScopeOfTypeRef)(
        mdTypeRef classref,                     // given classref
        mdToken  *ptkResolutionScope) PURE;

    // Find the type token given the name.
    __checkReturn
    STDMETHOD(FindTypeRefByName)(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
        LPCSTR      szName,                 // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef   *ptk) PURE;             // [OUT] TypeRef token returned.

    // return the TypeDef properties
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetTypeDefProps)(
        mdTypeDef   classdef,               // given classdef
        DWORD       *pdwAttr,               // return flags on class, tdPublic, tdAbstract
        mdToken     *ptkExtends) PURE;      // [OUT] Put base class TypeDef/TypeRef here

    // return the item's guid
    __checkReturn
    STDMETHOD(GetItemGuid)(
        mdToken     tkObj,                  // [IN] given item.
        CLSID       *pGuid) PURE;           // [out[ put guid here.

    // Get enclosing class of the NestedClass.
    __checkReturn
    STDMETHOD(GetNestedClassProps)(         // S_OK or error
        mdTypeDef   tkNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptkEnclosingClass) PURE; // [OUT] EnclosingClass token.

    // Get count of Nested classes given the enclosing class.
    __checkReturn
    STDMETHOD(GetCountNestedClasses)(   // return count of Nested classes.
        mdTypeDef   tkEnclosingClass,   // Enclosing class.
        ULONG      *pcNestedClassesCount) PURE;

    // Return array of Nested classes given the enclosing class.
    __checkReturn
    STDMETHOD(GetNestedClasses)(        // Return actual count.
        mdTypeDef   tkEnclosingClass,       // [IN] Enclosing class.
        mdTypeDef   *rNestedClasses,        // [OUT] Array of nested class tokens.
        ULONG       ulNestedClasses,        // [IN] Size of array.
        ULONG      *pcNestedClasses) PURE;

    // return the ModuleRef properties
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,                    // [IN] moduleref token
        LPCSTR      *pszName) PURE;         // [OUT] buffer to fill with the moduleref name

    //*****************************************
    //
    // GetSig* functions
    //
    //*****************************************
    __checkReturn
    STDMETHOD(GetSigOfMethodDef)(
        mdMethodDef       tkMethodDef,  // [IN] given MethodDef
        ULONG *           pcbSigBlob,   // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE * ppSig) PURE;

    __checkReturn
    STDMETHOD(GetSigOfFieldDef)(
        mdFieldDef        tkFieldDef,   // [IN] given FieldDef
        ULONG *           pcbSigBlob,   // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE * ppSig) PURE;

    __checkReturn
    STDMETHOD(GetSigFromToken)(
        mdToken           tk, // FieldDef, MethodDef, Signature or TypeSpec token
        ULONG *           pcbSig,
        PCCOR_SIGNATURE * ppSig) PURE;



    //*****************************************
    // get method property
    //*****************************************
    __checkReturn
    STDMETHOD(GetMethodDefProps)(
        mdMethodDef md,                 // The method for which to get props.
        DWORD      *pdwFlags) PURE;

    //*****************************************
    // return method implementation informaiton, like RVA and implflags
    //*****************************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetMethodImplProps)(
        mdToken     tk,                     // [IN] MethodDef
        ULONG       *pulCodeRVA,            // [OUT] CodeRVA
        DWORD       *pdwImplFlags) PURE;    // [OUT] Impl. Flags

    //*****************************************
    // return method implementation informaiton, like RVA and implflags
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldRVA)(
        mdFieldDef  fd,                     // [IN] fielddef
        ULONG       *pulCodeRVA) PURE;      // [OUT] CodeRVA

    //*****************************************
    // get field property
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldDefProps)(
        mdFieldDef fd,              // [IN] given fielddef
        DWORD     *pdwFlags) PURE;  // [OUT] return fdPublic, fdPrive, etc flags

    //*****************************************************************************
    // return default value of a token(could be paramdef, fielddef, or property
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetDefaultValue)(
        mdToken     tk,                     // [IN] given FieldDef, ParamDef, or Property
        MDDefaultValue *pDefaultValue) PURE;// [OUT] default value to fill


    //*****************************************
    // get dispid of a MethodDef or a FieldDef
    //*****************************************
    __checkReturn
    STDMETHOD(GetDispIdOfMemberDef)(        // return hresult
        mdToken     tk,                     // [IN] given methoddef or fielddef
        ULONG       *pDispid) PURE;         // [OUT] Put the dispid here.

    //*****************************************
    // return TypeRef/TypeDef given an InterfaceImpl token
    //*****************************************
    __checkReturn
    STDMETHOD(GetTypeOfInterfaceImpl)(  // return the TypeRef/typedef token for the interfaceimpl
        mdInterfaceImpl iiImpl,         // given a interfaceimpl
        mdToken        *ptkType) PURE;

    //*****************************************
    // look up function for TypeDef
    //*****************************************
    __checkReturn
    STDMETHOD(FindTypeDef)(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeDef.
        LPCSTR      szName,                 // [IN] Name of the TypeDef.
        mdToken     tkEnclosingClass,       // [IN] TypeRef/TypeDef Token for the enclosing class.
        mdTypeDef   *ptypedef) PURE;        // [IN] return typedef

    //*****************************************
    // return name and sig of a memberref
    //*****************************************
    __checkReturn
    STDMETHOD(GetNameAndSigOfMemberRef)(    // return name here
        mdMemberRef      memberref,         // given memberref
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName) PURE;

    //*****************************************************************************
    // Given memberref, return the parent. It can be TypeRef, ModuleRef, MethodDef
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetParentOfMemberRef)(
        mdMemberRef memberref,          // given memberref
        mdToken    *ptkParent) PURE;    // return the parent token

    __checkReturn
    STDMETHOD(GetParamDefProps)(
        mdParamDef paramdef,            // given a paramdef
        USHORT    *pusSequence,         // [OUT] slot number for this parameter
        DWORD     *pdwAttr,             // [OUT] flags
        LPCSTR    *pszName) PURE;       // [OUT] return the name of the parameter

    __checkReturn
    STDMETHOD(GetPropertyInfoForMethodDef)( // Result.
        mdMethodDef md,                     // [IN] memberdef
        mdProperty  *ppd,                   // [OUT] put property token here
        LPCSTR      *pName,                 // [OUT] put pointer to name here
        ULONG       *pSemantic) PURE;       // [OUT] put semantic here

    //*****************************************
    // class layout/sequence information
    //*****************************************
    __checkReturn
    STDMETHOD(GetClassPackSize)(            // return error if class doesn't have packsize
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwPackSize) PURE;     // [OUT] 1, 2, 4, 8, or 16

    __checkReturn
    STDMETHOD(GetClassTotalSize)(           // return error if class doesn't have total size info
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwClassSize) PURE;    // [OUT] return the total size of the class

    __checkReturn
    STDMETHOD(GetClassLayoutInit)(
        mdTypeDef   td,                     // [IN] give typedef
        MD_CLASS_LAYOUT *pLayout) PURE;     // [OUT] set up the status of query here

    __checkReturn
    STDMETHOD(GetClassLayoutNext)(
        MD_CLASS_LAYOUT *pLayout,           // [IN|OUT] set up the status of query here
        mdFieldDef  *pfd,                   // [OUT] return the fielddef
        ULONG       *pulOffset) PURE;       // [OUT] return the offset/ulSequence associate with it

    //*****************************************
    // marshal information of a field
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldMarshal)(             // return error if no native type associate with the token
        mdFieldDef  fd,                     // [IN] given fielddef
        PCCOR_SIGNATURE *pSigNativeType,    // [OUT] the native type signature
        ULONG       *pcbNativeType) PURE;   // [OUT] the count of bytes of *ppvNativeType


    //*****************************************
    // property APIs
    //*****************************************
    // find a property by name
    __checkReturn
    STDMETHOD(FindProperty)(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szPropName,             // [IN] property name
        mdProperty  *pProp) PURE;           // [OUT] return property token

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetPropertyProps)(
        mdProperty  prop,                   // [IN] property token
        LPCSTR      *szProperty,            // [OUT] property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
        ULONG       *pcbSig) PURE;          // [OUT] count of bytes in *ppvSig

    //**********************************
    // Event APIs
    //**********************************
    __checkReturn
    STDMETHOD(FindEvent)(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szEventName,            // [IN] event name
        mdEvent     *pEvent) PURE;          // [OUT] return event token

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetEventProps)(
        mdEvent     ev,                     // [IN] event token
        LPCSTR      *pszEvent,              // [OUT] Event name
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType) PURE;    // [OUT] EventType class


    //**********************************
    // find a particular associate of a property or an event
    //**********************************
    __checkReturn
    STDMETHOD(FindAssociate)(
        mdToken     evprop,                 // [IN] given a property or event token
        DWORD       associate,              // [IN] given a associate semantics(setter, getter, testdefault, reset, AddOn, RemoveOn, Fire)
        mdMethodDef *pmd) PURE;             // [OUT] return method def token

    // Note, void function in v1.0/v1.1
    __checkReturn
    STDMETHOD(EnumAssociateInit)(
        mdToken     evprop,                 // [IN] given a property or an event token
        HENUMInternal *phEnum) PURE;        // [OUT] cursor to hold the query result

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetAllAssociates)(
        HENUMInternal *phEnum,              // [IN] query result form GetPropertyAssociateCounts
        ASSOCIATE_RECORD *pAssociateRec,    // [OUT] struct to fill for output
        ULONG       cAssociateRec) PURE;    // [IN] size of the buffer


    //**********************************
    // Get info about a PermissionSet.
    //**********************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetPermissionSetProps)(
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission) PURE;   // [OUT] count of bytes of pvPermission.

    //****************************************
    // Get the String given the String token.
    // Returns a pointer to the string, or NULL in case of error.
    //****************************************
    __checkReturn
    STDMETHOD(GetUserString)(
        mdString stk,                   // [IN] the string token.
        ULONG   *pchString,             // [OUT] count of characters in the string.
        BOOL    *pbIs80Plus,            // [OUT] specifies where there are extended characters >= 0x80.
        LPCWSTR *pwszUserString) PURE;

    //*****************************************************************************
    // p-invoke APIs.
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetPinvokeMap)(
        mdToken     tk,                     // [IN] FieldDef, MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
        LPCSTR      *pszImportName,         // [OUT] Import name.
        mdModuleRef *pmrImportDLL) PURE;    // [OUT] ModuleRef token for the target DLL.

    //*****************************************************************************
    // helpers to convert a text signature to a com format
    //*****************************************************************************
    __checkReturn
    STDMETHOD(ConvertTextSigToComSig)(      // Return hresult.
        BOOL        fCreateTrIfNotFound,    // [IN] create typeref if not found
        LPCSTR      pSignature,             // [IN] class file format signature
        CQuickBytes *pqbNewSig,             // [OUT] place holder for CLR signature
        ULONG       *pcbCount) PURE;        // [OUT] the result size of signature

    //*****************************************************************************
    // Assembly MetaData APIs.
    //*****************************************************************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetAssemblyProps)(
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags) PURE;// [OUT] Flags.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetAssemblyRefProps)(
        mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
        const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
        ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
        const void  **ppbHashValue,         // [OUT] Hash blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
        DWORD       *pdwAssemblyRefFlags) PURE; // [OUT] Flags.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetFileProps)(
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags) PURE;    // [OUT] Flags.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetExportedTypeProps)(
        mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
        LPCSTR      *pszNamespace,          // [OUT] Namespace.
        LPCSTR      *pszName,               // [OUT] Name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags) PURE; // [OUT] Flags.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetManifestResourceProps)(
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags) PURE;// [OUT] Flags.

    __checkReturn
    STDMETHOD(FindExportedTypeByName)(      // S_OK or error
        LPCSTR      szNamespace,            // [IN] Namespace of the ExportedType.
        LPCSTR      szName,                 // [IN] Name of the ExportedType.
        mdExportedType   tkEnclosingType,   // [IN] ExportedType for the enclosing class.
        mdExportedType   *pmct) PURE;       // [OUT] Put ExportedType token here.

    __checkReturn
    STDMETHOD(FindManifestResourceByName)(  // S_OK or error
        LPCSTR      szName,                 // [IN] Name of the ManifestResource.
        mdManifestResource *pmmr) PURE;     // [OUT] Put ManifestResource token here.

    __checkReturn
    STDMETHOD(GetAssemblyFromScope)(        // S_OK or error
        mdAssembly  *ptkAssembly) PURE;     // [OUT] Put token here.

    __checkReturn
    STDMETHOD(GetCustomAttributeByName)(    // S_OK or error
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData) PURE;         // [OUT] Put size of data here.

    // Note: The return type of this method was void in v1
    __checkReturn
    STDMETHOD(GetTypeSpecFromToken)(      // S_OK or error.
        mdTypeSpec typespec,                // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig) PURE;               // [OUT] return size of signature.

    __checkReturn
    STDMETHOD(SetUserContextData)(          // S_OK or E_NOTIMPL
        IUnknown    *pIUnk) PURE;           // The user context.

    __checkReturn
    STDMETHOD_(BOOL, IsValidToken)(         // True or False.
        mdToken     tk) PURE;               // [IN] Given token.

    __checkReturn
    STDMETHOD(TranslateSigWithScope)(
        IMDInternalImport *pAssemImport,    // [IN] import assembly scope.
        const void  *pbHashValue,           // [IN] hash value for the import assembly.
        ULONG       cbHashValue,            // [IN] count of bytes in the hash value.
        PCCOR_SIGNATURE pbSigBlob,          // [IN] signature in the importing scope
        ULONG       cbSigBlob,              // [IN] count of bytes of signature
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] assembly emit scope.
        IMetaDataEmit *emit,                // [IN] emit interface
        CQuickBytes *pqkSigEmit,            // [OUT] buffer to hold translated signature
        ULONG       *pcbSig) PURE;          // [OUT] count of bytes in the translated signature

    STDMETHOD_(IMetaModelCommon*, GetMetaModelCommon)(  // Return MetaModelCommon interface.
        ) PURE;

    STDMETHOD_(IUnknown *, GetCachedPublicInterface)(BOOL fWithLock) PURE;   // return the cached public interface
    __checkReturn
    STDMETHOD(SetCachedPublicInterface)(IUnknown *pUnk) PURE;  // no return value
    STDMETHOD_(UTSemReadWrite*, GetReaderWriterLock)() PURE;   // return the reader writer lock
    __checkReturn
    STDMETHOD(SetReaderWriterLock)(UTSemReadWrite * pSem) PURE;

    STDMETHOD_(mdModule, GetModuleFromScope)() PURE;             // [OUT] Put mdModule token here.


    //-----------------------------------------------------------------
    // Additional custom methods

    // finding a particular method
    __checkReturn
    STDMETHOD(FindMethodDefUsingCompare)(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        PSIGCOMPARE pSignatureCompare,      // [IN] Routine to compare signatures
        void*       pSignatureArgs,         // [IN] Additional info to supply the compare function
        mdMethodDef *pmd) PURE;             // [OUT] matching memberdef

    // Additional v2 methods.

    //*****************************************
    // return a field offset for a given field
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldOffset)(
        mdFieldDef  fd,                     // [IN] fielddef
        ULONG       *pulOffset) PURE;       // [OUT] FieldOffset

    __checkReturn
    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec ms,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob) PURE;      // [OUT] actual size of signature blob

    __checkReturn
    STDMETHOD(GetTableInfoWithIndex)(
        ULONG      index,                   // [IN] pass in the table index
        void       **pTable,                // [OUT] pointer to table at index
        void       **pTableSize) PURE;      // [OUT] size of table at index

    __checkReturn
    STDMETHOD(ApplyEditAndContinue)(
        void        *pDeltaMD,              // [IN] the delta metadata
        ULONG       cbDeltaMD,              // [IN] length of pData
        IMDInternalImport **ppv) PURE;      // [OUT] the resulting metadata interface

    //**********************************
    // Generics APIs
    //**********************************
    __checkReturn
    STDMETHOD(GetGenericParamProps)(        // S_OK or error.
        mdGenericParam rd,                  // [IN] The type parameter
        ULONG* pulSequence,                 // [OUT] Parameter sequence number
        DWORD* pdwAttr,                     // [OUT] Type parameter flags (for future use)
        mdToken *ptOwner,                   // [OUT] The owner (TypeDef or MethodDef)
        DWORD *reserved,                    // [OUT] The kind (TypeDef/Ref/Spec, for future use)
        LPCSTR *szName) PURE;               // [OUT] The name

    __checkReturn
    STDMETHOD(GetGenericParamConstraintProps)(      // S_OK or error.
        mdGenericParamConstraint rd,            // [IN] The constraint token
        mdGenericParam *ptGenericParam,         // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType) PURE;  // [OUT] TypeDef/Ref/Spec constraint

    //*****************************************************************************
    // This function gets the "built for" version of a metadata scope.
    //  NOTE: if the scope has never been saved, it will not have a built-for
    //  version, and an empty string will be returned.
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetVersionString)(    // S_OK or error.
        LPCSTR      *pVer) PURE;       // [OUT] Put version string here.


    __checkReturn
    STDMETHOD(GetTypeDefRefTokenInTypeSpec)(// return S_FALSE if enclosing type does not have a token
        mdTypeSpec  tkTypeSpec,               // [IN] TypeSpec token to look at
        mdToken    *tkEnclosedToken) PURE;    // [OUT] The enclosed type token

#define MD_STREAM_VER_1X    0x10000
#define MD_STREAM_VER_2_B1  0x10001
#define MD_STREAM_VER_2     0x20000
    STDMETHOD_(DWORD, GetMetadataStreamVersion)() PURE;  //returns DWORD with major version of
                                // MD stream in senior word and minor version--in junior word

    __checkReturn
    STDMETHOD(GetNameOfCustomAttribute)(// S_OK or error
        mdCustomAttribute mdAttribute,      // [IN] The Custom Attribute
        LPCUTF8          *pszNamespace,     // [OUT] Namespace of Custom Attribute.
        LPCUTF8          *pszName) PURE;    // [OUT] Name of Custom Attribute.

    STDMETHOD(SetOptimizeAccessForSpeed)(// S_OK or error
        BOOL    fOptSpeed) PURE;

    STDMETHOD(SetVerifiedByTrustedSource)(// S_OK or error
        BOOL    fVerified) PURE;

    STDMETHOD(GetRvaOffsetData)(
        DWORD   *pFirstMethodRvaOffset,     // [OUT] Offset (from start of metadata) to the first RVA field in MethodDef table.
        DWORD   *pMethodDefRecordSize,      // [OUT] Size of each record in MethodDef table.
        DWORD   *pMethodDefCount,           // [OUT] Number of records in MethodDef table.
        DWORD   *pFirstFieldRvaOffset,      // [OUT] Offset (from start of metadata) to the first RVA field in FieldRVA table.
        DWORD   *pFieldRvaRecordSize,       // [OUT] Size of each record in FieldRVA table.
        DWORD   *pFieldRvaCount             // [OUT] Number of records in FieldRVA table.
        ) PURE;

    //----------------------------------------------------------------------------------------
    // !!! READ THIS !!!
    //
    // New methods have to be added at the end. The order and signatures of the existing methods
    // have to be preserved. We need to maintain a backward compatibility for this interface to
    // allow ildasm to work on SingleCLR.
    //
    //----------------------------------------------------------------------------------------

};  // IMDInternalImport


// {E03D7730-D7E3-11d2-8C0D-00C04FF7431A}
EXTERN_GUID(IID_IMDInternalImportENC, 0xe03d7730, 0xd7e3, 0x11d2, 0x8c, 0xd, 0x0, 0xc0, 0x4f, 0xf7, 0x43, 0x1a);

#undef  INTERFACE
#define INTERFACE IMDInternalImportENC
DECLARE_INTERFACE_(IMDInternalImportENC, IMDInternalImport)
{
private:
    using IMDInternalImport::ApplyEditAndContinue;
public:
    // ENC only methods here.
    STDMETHOD(ApplyEditAndContinue)(        // S_OK or error.
        MDInternalRW *pDelta) PURE;         // Interface to MD with the ENC delta.

    STDMETHOD(EnumDeltaTokensInit)(         // return hresult
        HENUMInternal *phEnum) PURE;        // [OUT] buffer to fill for enumerator data

}; // IMDInternalImportENC

// {F102C526-38CB-49ed-9B5F-498816AE36E0}
EXTERN_GUID(IID_IMDInternalEmit, 0xf102c526, 0x38cb, 0x49ed, 0x9b, 0x5f, 0x49, 0x88, 0x16, 0xae, 0x36, 0xe0);

#undef  INTERFACE
#define INTERFACE IMDInternalEmit
DECLARE_INTERFACE_(IMDInternalEmit, IUnknown)
{
    STDMETHOD(ChangeMvid)(                  // S_OK or error.
        REFGUID newMvid) PURE;              // GUID to use as the MVID

    STDMETHOD(SetMDUpdateMode)(
        ULONG updateMode, ULONG *pPreviousUpdateMode) PURE;

}; // IMDInternalEmit

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE

struct IMDCustomDataSource;
class CMiniMdSchema;
struct CMiniTableDef;
namespace MetaData
{
    class DataBlob;
}

// {CC0C8F7A-A00B-493D-80B6-CE0C92491670}
EXTERN_GUID(IID_IMDCustomDataSource, 0xcc0c8f7a, 0xa00b, 0x493d, 0x80, 0xb6, 0xce, 0xc, 0x92, 0x49, 0x16, 0x70);

#undef  INTERFACE
#define INTERFACE IMDCustomDataSource
DECLARE_INTERFACE_(IMDCustomDataSource, IUnknown)
{
    STDMETHOD(GetSchema)(CMiniMdSchema* pSchema) PURE;
    STDMETHOD(GetTableDef)(ULONG32 tableIndex, CMiniTableDef* pTableDef) PURE;
    STDMETHOD(GetBlobHeap)(MetaData::DataBlob* pBlobHeapData) PURE;
    STDMETHOD(GetGuidHeap)(MetaData::DataBlob* pGuidHeapData) PURE;
    STDMETHOD(GetStringHeap)(MetaData::DataBlob* pStringHeapData) PURE;
    STDMETHOD(GetUserStringHeap)(MetaData::DataBlob* pUserStringHeapData) PURE;
    STDMETHOD(GetTableRecords)(ULONG32 tableIndex, MetaData::DataBlob* pTableRecordData) PURE;
    STDMETHOD(GetTableSortable)(ULONG32 tableIndex, BOOL* pSortable) PURE;
    STDMETHOD(GetStorageSignature)(MetaData::DataBlob* pStorageSignature) PURE;

}; // IMDCustomDataSource

// {503F79FB-7AAE-4364-BDA6-8E235D173AEC}
EXTERN_GUID(IID_IMetaDataDispenserCustom,
    0x503f79fb, 0x7aae, 0x4364, 0xbd, 0xa6, 0x8e, 0x23, 0x5d, 0x17, 0x3a, 0xec);

#undef  INTERFACE
#define INTERFACE IMetaDataDispenserCustom
DECLARE_INTERFACE_(IMetaDataDispenserCustom, IUnknown)
{
    STDMETHOD(OpenScopeOnCustomDataSource)(    // Return code.
        IMDCustomDataSource  *pCustomSource,  // [in] The scope to open.
        DWORD                dwOpenFlags,    // [in] Open mode flags.
        REFIID               riid,           // [in] The interface desired.
        IUnknown             **ppIUnk) PURE; // [out] Return interface on success.

}; // IMetaDataDispenserCustom

#endif // FEATURE_METADATA_CUSTOM_DATA_SOURCE

#ifdef FEATURE_METADATA_DEBUGGEE_DATA_SOURCE
struct ICorDebugDataTarget;
HRESULT CreateRemoteMDInternalRWSource(TADDR mdInternalRWRemoteAddress, ICorDebugDataTarget* pDataTarget, DWORD defines, DWORD dataStructureVersion, IMDCustomDataSource** ppDataSource);
#endif

enum MetaDataReorderingOptions {
    NoReordering=0x0,
    ReArrangeStringPool=0x1
};

#ifdef FEATURE_PREJIT

// {0702E333-8D64-4ca7-B564-4AA56B1FCEA3}
EXTERN_GUID(IID_IMetaDataCorProfileData, 0x702e333, 0x8d64, 0x4ca7, 0xb5, 0x64, 0x4a, 0xa5, 0x6b, 0x1f, 0xce, 0xa3 );

#undef  INTERFACE
#define INTERFACE IMetaDataCorProfileData
DECLARE_INTERFACE_(IMetaDataCorProfileData, IUnknown)
{
    STDMETHOD(SetCorProfileData)(
        CorProfileData *pProfileData) PURE;         // [IN] Pointer to profile data
};

// {2B464817-C0F6-454e-99E7-C352D8384D7B}
EXTERN_GUID(IID_IMDInternalMetadataReorderingOptions, 0x2B464817, 0xC0F6, 0x454e, 0x99, 0xE7, 0xC3, 0x52, 0xD8, 0x38, 0x4D, 0x7B );

#undef  INTERFACE
#define INTERFACE IMDInternalMetadataReorderingOptions
DECLARE_INTERFACE_(IMDInternalMetadataReorderingOptions, IUnknown)
{
    STDMETHOD(SetMetaDataReorderingOptions)(
        MetaDataReorderingOptions options) PURE;         // [IN] metadata reordering options
};

#endif //FEATURE_PREJIT

#ifdef __HOLDER_H_

void DECLSPEC_NORETURN ThrowHR(HRESULT hr);

// This wrapper class ensures that the HENUMInternal is EnumClose'd no matter how the scope is exited.
class HENUMTypeDefInternalHolder
{
public:
    FORCEINLINE HENUMTypeDefInternalHolder(IMDInternalImport *pInternalImport)
    {
        WRAPPER_NO_CONTRACT;

        m_pInternalImport = pInternalImport;
        m_fAcquired       = FALSE;
    }

    FORCEINLINE VOID EnumTypeDefInit()
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        _ASSERTE(!m_fAcquired);
        HRESULT hr = m_pInternalImport->EnumTypeDefInit(&m_hEnum);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
        m_fAcquired = TRUE;

    }

    FORCEINLINE HRESULT EnumTypeDefInitNoThrow()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(!m_fAcquired);
        HRESULT hr = m_pInternalImport->EnumTypeDefInit(&m_hEnum);
        if (FAILED(hr))
        {
            return hr;
        }
        m_fAcquired = TRUE;

        return hr;
    }

    FORCEINLINE ~HENUMTypeDefInternalHolder()
    {
        WRAPPER_NO_CONTRACT;

        if (m_fAcquired)
        {
            m_pInternalImport->EnumClose(&m_hEnum);
        }
    }

    FORCEINLINE HENUMInternal* operator& ()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_fAcquired);
        return &m_hEnum;
    }

private:
    FORCEINLINE HENUMTypeDefInternalHolder(const HENUMTypeDefInternalHolder &)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(!"Don't try to assign this class.");
    }

private:
    IMDInternalImport *m_pInternalImport;
    HENUMInternal      m_hEnum;
    BOOL               m_fAcquired;
};


// This wrapper class ensures that the HENUMInternal is EnumClose'd no matter how the scope is exited.
class HENUMInternalHolder
{
public:
    FORCEINLINE HENUMInternalHolder(IMDInternalImport *pInternalImport)
    {
        WRAPPER_NO_CONTRACT;

        m_pInternalImport = pInternalImport;
        m_fAcquired       = FALSE;
    }

    FORCEINLINE VOID EnumGlobalFunctionsInit()
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        _ASSERTE(!m_fAcquired);
        HRESULT hr = m_pInternalImport->EnumGlobalFunctionsInit(&m_hEnum);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
        m_fAcquired = TRUE;

    }


    FORCEINLINE VOID EnumGlobalFieldsInit()
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        _ASSERTE(!m_fAcquired);
        HRESULT hr = m_pInternalImport->EnumGlobalFieldsInit(&m_hEnum);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
        m_fAcquired = TRUE;

    }

    FORCEINLINE VOID EnumTypeDefInit()
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        _ASSERTE(!m_fAcquired);
        HRESULT hr = m_pInternalImport->EnumTypeDefInit(&m_hEnum);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
        m_fAcquired = TRUE;
    }

    FORCEINLINE VOID EnumAssociateInit(mdToken tkParent)        // [IN] token to scope the search
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        _ASSERTE(!m_fAcquired);
        IfFailThrow(m_pInternalImport->EnumAssociateInit(tkParent, &m_hEnum));
        m_fAcquired = TRUE;
    }

    FORCEINLINE VOID EnumInit(DWORD       tkKind,                 // [IN] which table to work on
                              mdToken     tkParent                // [IN] token to scope the search
                             )
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        HRESULT hr = EnumInitNoThrow(tkKind, tkParent);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
    }

    __checkReturn
    FORCEINLINE HRESULT EnumInitNoThrow(DWORD       tkKind,       // [IN] which table to work on
                                        mdToken     tkParent      // [IN] token to scope the search
                                       )
    {
        CONTRACTL {
            NOTHROW;
        } CONTRACTL_END;

        _ASSERTE(!m_fAcquired);
        HRESULT hr = m_pInternalImport->EnumInit(tkKind, tkParent, &m_hEnum);
        if (SUCCEEDED(hr))
        {
            m_fAcquired = TRUE;
        }
        return hr;
    }

    FORCEINLINE VOID EnumAllInit(DWORD       tkKind                 // [IN] which table to work on
                             )
    {
        CONTRACTL {
            THROWS;
        } CONTRACTL_END;

        _ASSERTE(!m_fAcquired);
        HRESULT hr = m_pInternalImport->EnumAllInit(tkKind, &m_hEnum);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
        m_fAcquired = TRUE;

    }

    FORCEINLINE ULONG EnumGetCount()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            CONSISTENCY_CHECK(m_fAcquired);
        } CONTRACTL_END;

        return m_pInternalImport->EnumGetCount(&m_hEnum);
    }

    FORCEINLINE bool EnumNext(mdToken * pTok)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            CONSISTENCY_CHECK(m_fAcquired);
        } CONTRACTL_END;

        return m_pInternalImport->EnumNext(&m_hEnum, pTok);
    }

    FORCEINLINE void EnumReset()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            CONSISTENCY_CHECK(m_fAcquired);
        } CONTRACTL_END;

        return m_pInternalImport->EnumReset(&m_hEnum);
    }

    FORCEINLINE ~HENUMInternalHolder()
    {
        WRAPPER_NO_CONTRACT;

        if (m_fAcquired)
        {
            // Ignore the error
            (void)m_pInternalImport->EnumClose(&m_hEnum);
        }
    }

    FORCEINLINE HENUMInternal* operator& ()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_fAcquired);
        return &m_hEnum;
    }

private:
    FORCEINLINE HENUMInternalHolder(const HENUMInternalHolder &)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(!"Don't try to assign this class.");
    }


protected:
    IMDInternalImport *m_pInternalImport;
    HENUMInternal      m_hEnum;
    BOOL               m_fAcquired;
};

class HENUMInternalMethodImplHolder : protected HENUMInternalHolder
{
    public:
        FORCEINLINE HENUMInternalMethodImplHolder(IMDInternalImport *pInternalImport)
            : HENUMInternalHolder(pInternalImport)
        {
            WRAPPER_NO_CONTRACT;
        }

        FORCEINLINE ~HENUMInternalMethodImplHolder()
        {
            WRAPPER_NO_CONTRACT;

            if (m_fAcquired)
            {
                // Ignore the error
                (void)m_pInternalImport->EnumClose(&m_hEnum2);
            }
        }

        FORCEINLINE void EnumMethodImplInit(mdToken     tkParent                   // [IN] token to scope the search
                                 )
        {
            CONTRACTL {
                THROWS;
            } CONTRACTL_END;

            HRESULT hr = EnumMethodImplInitNoThrow(tkParent);
            if (FAILED(hr))
            {
                ThrowHR(hr);
            }
        }

        __checkReturn
        FORCEINLINE HRESULT EnumMethodImplInitNoThrow(mdToken     tkParent         // [IN] token to scope the search
                                 )
        {
            CONTRACTL {
                NOTHROW;
            } CONTRACTL_END;

            _ASSERTE(!m_fAcquired);
            HRESULT hr = m_pInternalImport->EnumMethodImplInit(tkParent, &m_hEnum, &m_hEnum2);
            if (SUCCEEDED(hr))
            {
                m_fAcquired = TRUE;
            }
            return hr;
        }

        __checkReturn
        FORCEINLINE HRESULT EnumMethodImplNext(
            mdToken *ptkImpl,
            mdToken *ptkDecl)
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                CONSISTENCY_CHECK(m_fAcquired);
            } CONTRACTL_END;

            return m_pInternalImport->EnumMethodImplNext(&m_hEnum, &m_hEnum2, ptkImpl, ptkDecl);
        }

        FORCEINLINE ULONG EnumMethodImplGetCount()
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                CONSISTENCY_CHECK(m_fAcquired);
            } CONTRACTL_END;

            return m_pInternalImport->EnumMethodImplGetCount(&m_hEnum, &m_hEnum2);
        }

    protected:
        HENUMInternal      m_hEnum2;
};

#endif //__HOLDER_H_

#endif // _METADATA_H_
