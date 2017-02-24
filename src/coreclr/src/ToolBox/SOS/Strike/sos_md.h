// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==

#ifndef __SOS_MD_H__
#define __SOS_MD_H__

#define IfErrGoTo(s, label) { \
	hresult = (s); \
	if(FAILED(hresult)){ \
	  goto label; }}

class CQuickBytes;

// TODO: Cleanup code to allow SOS to directly include the metadata header files.
//#include "MetaData.h"
//#include "corpriv.h"

/*
 *
 * Metadata definitions needed for PrettyPrint functions.
 *   The original definitions for the types and interfaces below exist in
 *   inc\MetaData.h and inc\CorPriv.h
 * TODO:
 *   Cleanup code to allow SOS to directly include the metadata header files.
 *     Currently it's extremely difficult due to symbol redefinitions.
 *   Always keep the definitions below in sync with the originals.
 * NOTES:
 *   Since SOS runs in a native debugger session, since it does not use EnC,
 *      and in order to minimize the amount of duplication we changed the
 *      method definitions that deal with UTSemReadWrite* arguments to take
 *      void* arguments instead.
 *   Also, some of the interface methods take CQuickBytes as arguments.
 *      If these methods are ever used it becomes crucial to maintain binary
 *      compatibility b/w the CQuickBytes defined in SOS and the definition
 *      from the EE.
 *
 */
typedef enum tagEnumType
{
    MDSimpleEnum        = 0x0,                  // simple enumerator that doesn't allocate memory 
    MDDynamicArrayEnum = 0x2,                   // dynamic array that holds tokens
    MDCustomEnum = 0x3,                         // Custom enumerator that doesnt work with the enum functions
} EnumType;

struct HENUMInternal
{
    DWORD       m_tkKind;                   // kind of tables that the enum is holding the result
    ULONG       m_ulCount;                  // count of total entries holding by the enumerator
    
    EnumType    m_EnumType;

    struct {
        ULONG   m_ulStart;
        ULONG   m_ulEnd;
        ULONG   m_ulCur;
    } u;

    // m_cursor will go away when we no longer support running EE with uncompressed
    // format. WHEN WE REMOVE THIS, REMOVE ITS VESTIAGES FROM ZeroEnum as well
    //
    char        m_cursor[32];               // cursor holding query result for read/write mode
};

typedef struct _MDDefaultValue
{
#if DBG_TARGET_BIGENDIAN
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

typedef struct
{
    RID         m_ridFieldCur;          // indexing to the field table
    RID         m_ridFieldEnd;          // end index to field table
} MD_CLASS_LAYOUT;

typedef struct
{
    USHORT      usMajorVersion;         // Major Version.   
    USHORT      usMinorVersion;         // Minor Version.
    USHORT      usBuildNumber;          // Build Number.
    USHORT      usRevisionNumber;       // Revision Number.
    LPCSTR      szLocale;               // Locale.
    DWORD       *rProcessor;            // Processor array.
    ULONG       ulProcessor;            // [IN/OUT] Size of the processor array/Actual # of entries filled in.
    OSINFO      *rOS;                   // OSINFO array.
    ULONG       ulOS;                   // [IN/OUT]Size of the OSINFO array/Actual # of entries filled in.
} AssemblyMetaDataInternal;

typedef struct
{
    mdMethodDef m_memberdef;
    DWORD       m_dwSemantics;
} ASSOCIATE_RECORD;

typedef BOOL (*PSIGCOMPARE)(PCCOR_SIGNATURE, DWORD, PCCOR_SIGNATURE, DWORD, void*);

EXTERN_GUID(IID_IMDInternalImport, 0xce0f34ed, 0xbbc6, 0x11d2, 0x94, 0x1e, 0x0, 0x0, 0xf8, 0x8, 0x34, 0x60);
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
    STDMETHOD(EnumTypeDefInit)(             // return hresult
        HENUMInternal *phEnum) PURE;        // [OUT] buffer to fill for enumerator data

    STDMETHOD_(ULONG, EnumTypeDefGetCount)(
        HENUMInternal *phEnum) PURE;        // [IN] the enumerator to retrieve information  

    STDMETHOD_(void, EnumTypeDefReset)(
        HENUMInternal *phEnum) PURE;        // [IN] the enumerator to retrieve information  

    STDMETHOD_(bool, EnumTypeDefNext)(      // return hresult
        HENUMInternal *phEnum,              // [IN] input enum
        mdTypeDef   *ptd) PURE;             // [OUT] return token

    STDMETHOD_(void, EnumTypeDefClose)(
        HENUMInternal *phEnum) PURE;        // [IN] the enumerator to retrieve information  

    //*****************************************************************************
    // enumerator for MethodImpl
    //*****************************************************************************
    STDMETHOD(EnumMethodImplInit)(          // return hresult
        mdTypeDef       td,                 // [IN] TypeDef over which to scope the enumeration.
        HENUMInternal   *phEnumBody,        // [OUT] buffer to fill for enumerator data for MethodBody tokens.
        HENUMInternal   *phEnumDecl) PURE;  // [OUT] buffer to fill for enumerator data for MethodDecl tokens.
    
    STDMETHOD_(ULONG, EnumMethodImplGetCount)(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.  
        HENUMInternal   *phEnumDecl) PURE;  // [IN] MethodDecl enumerator.
    
    STDMETHOD_(void, EnumMethodImplReset)(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl) PURE;  // [IN] MethodDecl enumerator.
    
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

    STDMETHOD(EnumGlobalFunctionsInit)(     // return hresult
        HENUMInternal   *phEnum) PURE;      // [OUT] buffer to fill for enumerator data

    STDMETHOD(EnumGlobalFieldsInit)(        // return hresult
        HENUMInternal   *phEnum) PURE;      // [OUT] buffer to fill for enumerator data

    STDMETHOD(EnumInit)(                    // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        mdToken     tkParent,               // [IN] token to scope the search
        HENUMInternal *phEnum) PURE;        // [OUT] the enumerator to fill 

    STDMETHOD(EnumAllInit)(                 // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        HENUMInternal *phEnum) PURE;        // [OUT] the enumerator to fill 

    STDMETHOD_(bool, EnumNext)(
        HENUMInternal *phEnum,              // [IN] the enumerator to retrieve information  
        mdToken     *ptk) PURE;             // [OUT] token to scope the search

    STDMETHOD_(ULONG, EnumGetCount)(
        HENUMInternal *phEnum) PURE;        // [IN] the enumerator to retrieve information  

    STDMETHOD_(void, EnumReset)(
        HENUMInternal *phEnum) PURE;        // [IN] the enumerator to be reset  

    STDMETHOD_(void, EnumClose)(
        HENUMInternal *phEnum) PURE;        // [IN] the enumerator to be closed

    //*****************************************
    // Enumerator helpers for declsecurity.
    //*****************************************
    STDMETHOD(EnumPermissionSetsInit)(      // return S_FALSE if record not found
        mdToken     tkParent,               // [IN] token to scope the search
        CorDeclSecurity Action,             // [IN] Action to scope the search
        HENUMInternal *phEnum) PURE;        // [OUT] the enumerator to fill 

    //*****************************************
    // Enumerator helpers for CustomAttribute
    //*****************************************
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
    STDMETHOD(GetParentToken)(
        mdToken     tkChild,                // [IN] given child token
        mdToken     *ptkParent) PURE;       // [OUT] returning parent

    //*****************************************
    // Custom value helpers
    //***************************************** 
    STDMETHOD(GetCustomAttributeProps)(  // S_OK or error.
        mdCustomAttribute at,               // [IN] The attribute.
        mdToken     *ptkType) PURE;         // [OUT] Put attribute type here.
    
    STDMETHOD(GetCustomAttributeAsBlob)(
        mdCustomAttribute cv,               // [IN] given custom value token
        void const  **ppBlob,               // [OUT] return the pointer to internal blob
        ULONG       *pcbSize) PURE;         // [OUT] return the size of the blob
    
    // returned void in v1.0/v1.1
    STDMETHOD (GetScopeProps)(
        LPCSTR      *pszName,               // [OUT] scope name
        GUID        *pmvid) PURE;           // [OUT] version id
    
    // finding a particular method 
    STDMETHOD(FindMethodDef)(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmd) PURE;             // [OUT] matching memberdef

    // return a iSeq's param given a MethodDef
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
    STDMETHOD(GetNameOfTypeDef)(
        mdTypeDef   classdef,               // given classdef
        LPCSTR      *pszname,               // return class name(unqualified)
        LPCSTR      *psznamespace) PURE;    // return the name space name

    STDMETHOD(GetIsDualOfTypeDef)(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pDual) PURE;           // [OUT] return dual flag here.

    STDMETHOD(GetIfaceTypeOfTypeDef)(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pIface) PURE;          // [OUT] 0=dual, 1=vtable, 2=dispinterface

    // get the name of either methoddef
    STDMETHOD(GetNameOfMethodDef)(  // return the name of the memberdef in UTF8
        mdMethodDef md,             // given memberdef
        LPCSTR     *pszName) PURE;
    
    STDMETHOD(GetNameAndSigOfMethodDef)(
        mdMethodDef      methoddef,         // [IN] given memberdef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName) PURE;
    
    // return the name of a FieldDef
    STDMETHOD(GetNameOfFieldDef)(
        mdFieldDef fd,              // given memberdef
        LPCSTR    *pszName) PURE;
    
    // return the name of typeref
    STDMETHOD(GetNameOfTypeRef)(
        mdTypeRef   classref,               // [IN] given typeref
        LPCSTR      *psznamespace,          // [OUT] return typeref name
        LPCSTR      *pszname) PURE;         // [OUT] return typeref namespace
    
    // return the resolutionscope of typeref
    STDMETHOD(GetResolutionScopeOfTypeRef)(
        mdTypeRef classref,                 // given classref
        mdToken  *ptkResolutionScope) PURE;
    
    // Find the type token given the name.
    STDMETHOD(FindTypeRefByName)(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
        LPCSTR      szName,                 // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef   *ptk) PURE;             // [OUT] TypeRef token returned.
    
    // return the TypeDef properties
    // returned void in v1.0/v1.1
    STDMETHOD(GetTypeDefProps)(  
        mdTypeDef   classdef,               // given classdef
        DWORD       *pdwAttr,               // return flags on class, tdPublic, tdAbstract
        mdToken     *ptkExtends) PURE;      // [OUT] Put base class TypeDef/TypeRef here
    
    // return the item's guid
    STDMETHOD(GetItemGuid)(     
        mdToken     tkObj,                  // [IN] given item.
        CLSID       *pGuid) PURE;           // [out[ put guid here.

    // Get enclosing class of the NestedClass.
    STDMETHOD(GetNestedClassProps)(         // S_OK or error
        mdTypeDef   tkNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptkEnclosingClass) PURE; // [OUT] EnclosingClass token.

    // Get count of Nested classes given the enclosing class.
    STDMETHOD(GetCountNestedClasses)(       // return count of Nested classes.
        mdTypeDef   tkEnclosingClass,       // Enclosing class.
        ULONG      *pcNestedClassesCount) PURE;
    
    // Return array of Nested classes given the enclosing class.
    STDMETHOD(GetNestedClasses)(            // Return actual count.
        mdTypeDef   tkEnclosingClass,       // [IN] Enclosing class.
        mdTypeDef   *rNestedClasses,        // [OUT] Array of nested class tokens.
        ULONG       ulNestedClasses,        // [IN] Size of array.
        ULONG      *pcNestedClasses) PURE;

    // return the ModuleRef properties
    // returned void in v1.0/v1.1
    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,                    // [IN] moduleref token
        LPCSTR      *pszName) PURE;         // [OUT] buffer to fill with the moduleref name

    //*****************************************
    //
    // GetSig* functions
    //
    //*****************************************
    STDMETHOD(GetSigOfMethodDef)(
        mdMethodDef      methoddef,     // [IN] given memberdef
        ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE *ppSig) PURE;
    
    STDMETHOD(GetSigOfFieldDef)(
        mdMethodDef      methoddef,     // [IN] given memberdef
        ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE *ppSig) PURE;
    
    STDMETHOD(GetSigFromToken)(
        mdToken           tk, 
        ULONG *           pcbSig, 
        PCCOR_SIGNATURE * ppSig) PURE;
    
    
    
    //*****************************************
    // get method property
    //*****************************************
    STDMETHOD(GetMethodDefProps)(
        mdMethodDef md,             // The method for which to get props.
        DWORD      *pdwFlags) PURE;
    
    //*****************************************
    // return method implementation informaiton, like RVA and implflags
    //*****************************************
    // returned void in v1.0/v1.1
    STDMETHOD(GetMethodImplProps)(
        mdToken     tk,                     // [IN] MethodDef
        ULONG       *pulCodeRVA,            // [OUT] CodeRVA
        DWORD       *pdwImplFlags) PURE;    // [OUT] Impl. Flags

    //*****************************************
    // return method implementation informaiton, like RVA and implflags
    //*****************************************
    STDMETHOD(GetFieldRVA)(
        mdFieldDef  fd,                     // [IN] fielddef 
        ULONG       *pulCodeRVA) PURE;      // [OUT] CodeRVA
    
    //*****************************************
    // get field property
    //*****************************************
    STDMETHOD(GetFieldDefProps)(
        mdFieldDef fd,                  // [IN] given fielddef
        DWORD     *pdwFlags) PURE;      // [OUT] return fdPublic, fdPrive, etc flags
    
    //*****************************************************************************
    // return default value of a token(could be paramdef, fielddef, or property
    //*****************************************************************************
    STDMETHOD(GetDefaultValue)(  
        mdToken     tk,                     // [IN] given FieldDef, ParamDef, or Property
        MDDefaultValue *pDefaultValue) PURE;// [OUT] default value to fill

    
    //*****************************************
    // get dispid of a MethodDef or a FieldDef
    //*****************************************
    STDMETHOD(GetDispIdOfMemberDef)(        // return hresult
        mdToken     tk,                     // [IN] given methoddef or fielddef
        ULONG       *pDispid) PURE;         // [OUT] Put the dispid here.

    //*****************************************
    // return TypeRef/TypeDef given an InterfaceImpl token
    //*****************************************
    STDMETHOD(GetTypeOfInterfaceImpl)(  // return the TypeRef/typedef token for the interfaceimpl
        mdInterfaceImpl iiImpl,         // given a interfaceimpl
        mdToken        *ptkType) PURE;
    
    //*****************************************
    // look up function for TypeDef
    //*****************************************
    STDMETHOD(FindTypeDef)(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeDef.
        LPCSTR      szName,                 // [IN] Name of the TypeDef.
        mdToken     tkEnclosingClass,       // [IN] TypeRef/TypeDef Token for the enclosing class.
        mdTypeDef   *ptypedef) PURE;        // [IN] return typedef

    //*****************************************
    // return name and sig of a memberref
    //*****************************************
    STDMETHOD(GetNameAndSigOfMemberRef)(    // return name here
        mdMemberRef      memberref,         // given memberref
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName) PURE;
    
    //*****************************************************************************
    // Given memberref, return the parent. It can be TypeRef, ModuleRef, MethodDef
    //*****************************************************************************
    STDMETHOD(GetParentOfMemberRef)(
        mdMemberRef memberref,          // given memberref
        mdToken    *ptkParent) PURE;    // return the parent token
    
    STDMETHOD(GetParamDefProps)(
        mdParamDef paramdef,        // given a paramdef
        USHORT    *pusSequence,     // [OUT] slot number for this parameter
        DWORD     *pdwAttr,         // [OUT] flags
        LPCSTR    *pszName) PURE;   // [OUT] return the name of the parameter
    
    STDMETHOD(GetPropertyInfoForMethodDef)( // Result.
        mdMethodDef md,                     // [IN] memberdef
        mdProperty  *ppd,                   // [OUT] put property token here
        LPCSTR      *pName,                 // [OUT] put pointer to name here
        ULONG       *pSemantic) PURE;       // [OUT] put semantic here
    
    //*****************************************
    // class layout/sequence information
    //*****************************************
    STDMETHOD(GetClassPackSize)(            // return error if class doesn't have packsize
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwPackSize) PURE;     // [OUT] 1, 2, 4, 8, or 16

    STDMETHOD(GetClassTotalSize)(           // return error if class doesn't have total size info
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwClassSize) PURE;    // [OUT] return the total size of the class

    STDMETHOD(GetClassLayoutInit)(
        mdTypeDef   td,                     // [IN] give typedef
        MD_CLASS_LAYOUT *pLayout) PURE;     // [OUT] set up the status of query here

    STDMETHOD(GetClassLayoutNext)(
        MD_CLASS_LAYOUT *pLayout,           // [IN|OUT] set up the status of query here
        mdFieldDef  *pfd,                   // [OUT] return the fielddef
        ULONG       *pulOffset) PURE;       // [OUT] return the offset/ulSequence associate with it

    //*****************************************
    // marshal information of a field
    //*****************************************
    STDMETHOD(GetFieldMarshal)(             // return error if no native type associate with the token
        mdFieldDef  fd,                     // [IN] given fielddef
        PCCOR_SIGNATURE *pSigNativeType,    // [OUT] the native type signature
        ULONG       *pcbNativeType) PURE;   // [OUT] the count of bytes of *ppvNativeType


    //*****************************************
    // property APIs
    //*****************************************
    // find a property by name
    STDMETHOD(FindProperty)(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szPropName,             // [IN] property name
        mdProperty  *pProp) PURE;           // [OUT] return property token

    // returned void in v1.0/v1.1
    STDMETHOD(GetPropertyProps)(
        mdProperty  prop,                   // [IN] property token
        LPCSTR      *szProperty,            // [OUT] property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
        ULONG       *pcbSig) PURE;          // [OUT] count of bytes in *ppvSig

    //**********************************
    // Event APIs
    //**********************************
    STDMETHOD(FindEvent)(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szEventName,            // [IN] event name
        mdEvent     *pEvent) PURE;          // [OUT] return event token

    // returned void in v1.0/v1.1
    STDMETHOD(GetEventProps)(
        mdEvent     ev,                     // [IN] event token
        LPCSTR      *pszEvent,              // [OUT] Event name
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType) PURE;    // [OUT] EventType class


    //**********************************
    // find a particular associate of a property or an event
    //**********************************
    STDMETHOD(FindAssociate)(
        mdToken     evprop,                 // [IN] given a property or event token
        DWORD       associate,              // [IN] given a associate semantics(setter, getter, testdefault, reset, AddOn, RemoveOn, Fire)
        mdMethodDef *pmd) PURE;             // [OUT] return method def token 

    // Note, void function in v1.0/v1.1
    STDMETHOD(EnumAssociateInit)(
        mdToken     evprop,                 // [IN] given a property or an event token
        HENUMInternal *phEnum) PURE;        // [OUT] cursor to hold the query result

    // returned void in v1.0/v1.1
    STDMETHOD(GetAllAssociates)(
        HENUMInternal *phEnum,              // [IN] query result form GetPropertyAssociateCounts
        ASSOCIATE_RECORD *pAssociateRec,    // [OUT] struct to fill for output
        ULONG       cAssociateRec) PURE;    // [IN] size of the buffer


    //**********************************
    // Get info about a PermissionSet.
    //**********************************
    // returned void in v1.0/v1.1
    STDMETHOD(GetPermissionSetProps)(
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission) PURE;   // [OUT] count of bytes of pvPermission.

    //****************************************
    // Get the String given the String token.
    // Returns a pointer to the string, or NULL in case of error.
    //****************************************
    STDMETHOD(GetUserString)(
        mdString stk,                   // [IN] the string token.
        ULONG   *pchString,             // [OUT] count of characters in the string.
        BOOL    *pbIs80Plus,            // [OUT] specifies where there are extended characters >= 0x80.
        LPCWSTR *pwszUserString) PURE;
    
    //*****************************************************************************
    // p-invoke APIs.
    //*****************************************************************************
    STDMETHOD(GetPinvokeMap)(
        mdToken     tk,                     // [IN] FieldDef, MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
        LPCSTR      *pszImportName,         // [OUT] Import name.
        mdModuleRef *pmrImportDLL) PURE;    // [OUT] ModuleRef token for the target DLL.

    //*****************************************************************************
    // helpers to convert a text signature to a com format
    //*****************************************************************************
    STDMETHOD(ConvertTextSigToComSig)(      // Return hresult.
        BOOL        fCreateTrIfNotFound,    // [IN] create typeref if not found
        LPCSTR      pSignature,             // [IN] class file format signature
        CQuickBytes *pqbNewSig,             // [OUT] place holder for CLR signature
        ULONG       *pcbCount) PURE;        // [OUT] the result size of signature

    //*****************************************************************************
    // Assembly MetaData APIs.
    //*****************************************************************************
    // returned void in v1.0/v1.1
    STDMETHOD(GetAssemblyProps)(
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags) PURE;// [OUT] Flags.

    // returned void in v1.0/v1.1
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
    STDMETHOD(GetFileProps)(
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags) PURE;    // [OUT] Flags.

    // returned void in v1.0/v1.1
    STDMETHOD(GetExportedTypeProps)(
        mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
        LPCSTR      *pszNamespace,          // [OUT] Namespace.
        LPCSTR      *pszName,               // [OUT] Name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags) PURE; // [OUT] Flags.

    // returned void in v1.0/v1.1
    STDMETHOD(GetManifestResourceProps)(
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags) PURE;// [OUT] Flags.

    STDMETHOD(FindExportedTypeByName)(      // S_OK or error
        LPCSTR      szNamespace,            // [IN] Namespace of the ExportedType.   
        LPCSTR      szName,                 // [IN] Name of the ExportedType.   
        mdExportedType   tkEnclosingType,   // [IN] ExportedType for the enclosing class.
        mdExportedType   *pmct) PURE;       // [OUT] Put ExportedType token here.

    STDMETHOD(FindManifestResourceByName)(  // S_OK or error
        LPCSTR      szName,                 // [IN] Name of the ManifestResource.   
        mdManifestResource *pmmr) PURE;     // [OUT] Put ManifestResource token here.

    STDMETHOD(GetAssemblyFromScope)(        // S_OK or error
        mdAssembly  *ptkAssembly) PURE;     // [OUT] Put token here.

    STDMETHOD(GetCustomAttributeByName)(    // S_OK or error
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData) PURE;         // [OUT] Put size of data here.

    // Note: The return type of this method was void in v1
    STDMETHOD(GetTypeSpecFromToken)(      // S_OK or error.
        mdTypeSpec typespec,                // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig) PURE;               // [OUT] return size of signature.

    STDMETHOD(SetUserContextData)(          // S_OK or E_NOTIMPL
        IUnknown    *pIUnk) PURE;           // The user context.

    STDMETHOD_(BOOL, IsValidToken)(         // True or False.
        mdToken     tk) PURE;               // [IN] Given token.

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

    // since SOS does not need to call method below, change return value to IUnknown* (from IMetaModelCommon*)
    STDMETHOD_(IUnknown*, GetMetaModelCommon)(  // Return MetaModelCommon interface.
        ) PURE;

    STDMETHOD_(IUnknown *, GetCachedPublicInterface)(BOOL fWithLock) PURE;   // return the cached public interface
    STDMETHOD(SetCachedPublicInterface)(IUnknown *pUnk) PURE;  // no return value
    // since SOS does not use the next 2 methods replace UTSemReadWrite* with void* in the signature
    STDMETHOD_(void*, GetReaderWriterLock)() PURE;   // return the reader writer lock
    STDMETHOD(SetReaderWriterLock)(void * pSem) PURE; 

    STDMETHOD_(mdModule, GetModuleFromScope)() PURE;             // [OUT] Put mdModule token here.


    //-----------------------------------------------------------------
    // Additional custom methods

    // finding a particular method 
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
    STDMETHOD(GetFieldOffset)(
        mdFieldDef  fd,                     // [IN] fielddef 
        ULONG       *pulOffset) PURE;       // [OUT] FieldOffset

    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec ms,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data   
        ULONG       *pcbSigBlob) PURE;      // [OUT] actual size of signature blob  

    STDMETHOD(GetTableInfoWithIndex)(
        ULONG      index,                   // [IN] pass in the table index
        void       **pTable,                // [OUT] pointer to table at index
        void       **pTableSize) PURE;      // [OUT] size of table at index

    STDMETHOD(ApplyEditAndContinue)(
        void        *pDeltaMD,              // [IN] the delta metadata
        ULONG       cbDeltaMD,              // [IN] length of pData
        IMDInternalImport **ppv) PURE;      // [OUT] the resulting metadata interface

    //**********************************
    // Generics APIs
    //**********************************
    STDMETHOD(GetGenericParamProps)(        // S_OK or error.
        mdGenericParam rd,                  // [IN] The type parameter
        ULONG* pulSequence,                 // [OUT] Parameter sequence number
        DWORD* pdwAttr,                     // [OUT] Type parameter flags (for future use)       
        mdToken *ptOwner,                   // [OUT] The owner (TypeDef or MethodDef) 
        DWORD *reserved,                    // [OUT] The kind (TypeDef/Ref/Spec, for future use)
        LPCSTR *szName) PURE;               // [OUT] The name

    STDMETHOD(GetGenericParamConstraintProps)(      // S_OK or error.
        mdGenericParamConstraint rd,            // [IN] The constraint token
        mdGenericParam *ptGenericParam,         // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType) PURE;  // [OUT] TypeDef/Ref/Spec constraint

    //*****************************************************************************
    // This function gets the "built for" version of a metadata scope.
    //  NOTE: if the scope has never been saved, it will not have a built-for
    //  version, and an empty string will be returned.
    //*****************************************************************************
    STDMETHOD(GetVersionString)(    // S_OK or error.
        LPCSTR      *pVer) PURE;       // [OUT] Put version string here.


    STDMETHOD(SafeAndSlowEnumCustomAttributeByNameInit)(// return S_FALSE if record not found
        mdToken     tkParent,               // [IN] token to scope the search
        LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
        HENUMInternal *phEnum) PURE;             // [OUT] The enumerator

    STDMETHOD(SafeAndSlowEnumCustomAttributeByNameNext)(// return S_FALSE if record not found
        mdToken     tkParent,               // [IN] token to scope the search
        LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
        HENUMInternal *phEnum,              // [IN] The enumerator
        mdCustomAttribute *mdAttribute) PURE;     // [OUT] The custom attribute that was found 


    STDMETHOD(GetTypeDefRefTokenInTypeSpec)(// return S_FALSE if enclosing type does not have a token
        mdTypeSpec  tkTypeSpec,               // [IN] TypeSpec token to look at
        mdToken    *tkEnclosedToken) PURE;    // [OUT] The enclosed type token

#define MD_STREAM_VER_1X    0x10000
#define MD_STREAM_VER_2_B1  0x10001
#define MD_STREAM_VER_2     0x20000
    STDMETHOD_(DWORD, GetMetadataStreamVersion)() PURE;  //returns DWORD with major version of
                                // MD stream in senior word and minor version--in junior word

    STDMETHOD(GetNameOfCustomAttribute)(// S_OK or error
        mdCustomAttribute mdAttribute,      // [IN] The Custom Attribute
        LPCUTF8          *pszNamespace,     // [OUT] Namespace of Custom Attribute.
        LPCUTF8          *pszName) PURE;    // [OUT] Name of Custom Attribute.

    STDMETHOD(SetOptimizeAccessForSpeed)(// S_OK or error
        BOOL    fOptSpeed) PURE;

    STDMETHOD(SetVerifiedByTrustedSource)(// S_OK or error
        BOOL    fVerified) PURE;

};  // IMDInternalImport

EXTERN_GUID(IID_IMetaDataHelper, 0xad93d71d, 0xe1f2, 0x11d1, 0x94, 0x9, 0x0, 0x0, 0xf8, 0x8, 0x34, 0x60);

#undef  INTERFACE
#define INTERFACE IMetaDataHelper
DECLARE_INTERFACE_(IMetaDataHelper, IUnknown)
{
    // helper functions
    // This function is exposing the ability to translate signature from a given
    // source scope to a given target scope.
    // 
    STDMETHOD(TranslateSigWithScope)(
        IMetaDataAssemblyImport *pAssemImport, // [IN] importing assembly interface
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        IMetaDataImport *import,            // [IN] importing interface
        PCCOR_SIGNATURE pbSigBlob,          // [IN] signature in the importing scope
        ULONG       cbSigBlob,              // [IN] count of bytes of signature
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] emit assembly interface
        IMetaDataEmit *emit,                // [IN] emit interface
        PCOR_SIGNATURE pvTranslatedSig,     // [OUT] buffer to hold translated signature
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig) PURE;// [OUT] count of bytes in the translated signature

    STDMETHOD(GetMetadata)(
        ULONG       ulSelect,               // [IN] Selector.
        void        **ppData) PURE;         // [OUT] Put pointer to data here.

    STDMETHOD_(IUnknown *, GetCachedInternalInterface)(BOOL fWithLock) PURE;    // S_OK or error
    STDMETHOD(SetCachedInternalInterface)(IUnknown * pUnk) PURE;    // S_OK or error
    // since SOS does not use the next 2 methods replace UTSemReadWrite* with void* in the signature
    STDMETHOD_(void*, GetReaderWriterLock)() PURE;   // return the reader writer lock
    STDMETHOD(SetReaderWriterLock)(void * pSem) PURE;
};

/********************************************************************************/

// Fine grained formatting flags used by the PrettyPrint APIs below.
// Upto FormatStubInfo they mirror the values used by TypeString, after that
// they're used to enable specifying differences between the ILDASM-style 
// output and the C#-like output prefered by the rest of SOS.
typedef enum 
{
  FormatBasic       =   0x00000000, // Not a bitmask, simply the tersest flag settings possible
  FormatNamespace   =   0x00000001, // Include namespace and/or enclosing class names in type names
  FormatFullInst    =   0x00000002, // Include namespace and assembly in generic types (regardless of other flag settings)
  FormatAssembly    =   0x00000004, // Include assembly display name in type names
  FormatSignature   =   0x00000008, // Include signature in method names
  FormatNoVersion   =   0x00000010, // Suppress version and culture information in all assembly names
  FormatDebug       =   0x00000020, // For debug printing of types only
  FormatAngleBrackets = 0x00000040, // Whether generic types are C<T> or C[T]
  FormatStubInfo    =   0x00000080, // Include stub info like {unbox-stub}
  // following flags are not present in TypeString::FormatFlags
  FormatSlashSep    =   0x00000100, // Whether nested types are NS.C1/C2 or NS.C1+C2
  FormatKwInNames   =   0x00000200, // Whether "class" and "valuetype" appear in type names in certain instances
  FormatCSharp      =   0x0000004b, // Used to generate a C#-like string representation of the token
  FormatILDasm      =   0x000003ff, // Used to generate an ILDASM-style string representation of the token
}
PPFormatFlags;

char* asString(CQuickBytes *out);

PCCOR_SIGNATURE PrettyPrintType(
    PCCOR_SIGNATURE typePtr,            // type to convert,     
    CQuickBytes *out,                   // where to put the pretty printed string   
    IMDInternalImport *pIMDI,           // ptr to IMDInternal class with ComSig
    DWORD formatFlags = FormatILDasm);

const char* PrettyPrintClass(
    CQuickBytes *out,                   // where to put the pretty printed string   
	mdToken tk,					 		// The class token to look up 
    IMDInternalImport *pIMDI,           // ptr to IMDInternalImport class with ComSig
    DWORD formatFlags = FormatILDasm);

// We have a proliferation of functions that translate a (module/token) pair to
// a string, but none of them were as complete as PrettyPrintClass. Most were
// not handling generic instantiations appropriately. PrettyPrintClassFromToken
// provides this missing functionality. If passed "FormatCSharp" it will generate
// a name fitting the format used throughout SOS, with the exception of !dumpil
// (due to its ILDASM ancestry).
// TODO: Refactor the code in PrettyPrintClassFromToken, NameForTypeDef_s,
// TODO: NameForToken_s, MDInfo::TypeDef/RefName
void PrettyPrintClassFromToken(
    TADDR moduleAddr,                   // the module containing the token
    mdToken tok,                        // the class token to look up
    __out_ecount(cbName) WCHAR* mdName, // where to put the pretty printed string
    size_t cbName,                      // the capacity of the buffer
    DWORD formatFlags = FormatCSharp);  // the format flags for the types

inline HRESULT GetMDInternalFromImport(IMetaDataImport* pIMDImport, IMDInternalImport **ppIMDI)
{
    HRESULT hresult = E_FAIL;
    IUnknown *pUnk = NULL;
    IMetaDataHelper *pIMDH = NULL;

    IfErrGoTo(pIMDImport->QueryInterface(IID_IMetaDataHelper, (void**)&pIMDH), Cleanup);
    pUnk = pIMDH->GetCachedInternalInterface(FALSE);
    if (pUnk == NULL)
        goto Cleanup;
    IfErrGoTo(pUnk->QueryInterface(IID_IMDInternalImport, (void**)ppIMDI), Cleanup);

Cleanup:
    if (pUnk)
        pUnk->Release();
    if (pIMDH != NULL)
        pIMDH->Release();

    return hresult;
}

#endif

