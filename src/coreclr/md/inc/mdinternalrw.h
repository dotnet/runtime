// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDInternalRW.h
//

//
// Contains utility code for MD directory
//
//*****************************************************************************
#ifndef __MDInternalRW__h__
#define __MDInternalRW__h__

#ifdef FEATURE_METADATA_INTERNAL_APIS

#include "../inc/mdlog.h"

class UTSemReadWrite;

class MDInternalRW : public IMDInternalImportENC, public IMDCommon
{
    friend class VerifyLayoutsMD;
public:


    MDInternalRW();
    virtual ~MDInternalRW();
    __checkReturn
    HRESULT Init(LPVOID pData, ULONG cbData, int bReadOnly);
    __checkReturn
    HRESULT InitWithStgdb(IUnknown *pUnk, CLiteWeightStgdbRW *pStgdb);
    __checkReturn
    HRESULT InitWithRO(MDInternalRO *pRO, int bReadOnly);

    // *** IUnknown methods ***
    __checkReturn
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void);
    STDMETHODIMP_(ULONG) Release(void);

    __checkReturn
    STDMETHODIMP TranslateSigWithScope(
        IMDInternalImport *pAssemImport,    // [IN] import assembly scope.
        const void  *pbHashValue,           // [IN] hash value for the import assembly.
        ULONG       cbHashValue,            // [IN] count of bytes in the hash value.
        PCCOR_SIGNATURE pbSigBlob,          // [IN] signature in the importing scope
        ULONG       cbSigBlob,              // [IN] count of bytes of signature
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] assembly emit scope.
        IMetaDataEmit *emit,                // [IN] emit interface
        CQuickBytes *pqkSigEmit,            // [OUT] buffer to hold translated signature
        ULONG       *pcbSig)                // [OUT] count of bytes in the translated signature
        DAC_UNEXPECTED();

    __checkReturn
    STDMETHODIMP GetTypeDefRefTokenInTypeSpec(// return S_FALSE if enclosing type does not have a token
        mdTypeSpec  tkTypeSpec,             // [IN] TypeSpec token to look at
        mdToken    *tkEnclosedToken)        // [OUT] The enclosed type token
        DAC_UNEXPECTED();

    STDMETHODIMP_(IMetaModelCommon*) GetMetaModelCommon()
    {
        return static_cast<IMetaModelCommon*>(&m_pStgdb->m_MiniMd);
    }

    STDMETHODIMP_(IMetaModelCommonRO*) GetMetaModelCommonRO()
    {
        if (m_pStgdb->m_MiniMd.IsWritable())
        {
            _ASSERTE(!"IMetaModelCommonRO methods cannot be used because this importer is writable.");
            return NULL;
        }

        return static_cast<IMetaModelCommonRO*>(&m_pStgdb->m_MiniMd);
    }

    __checkReturn
    STDMETHODIMP SetOptimizeAccessForSpeed(// return hresult
        BOOL    fOptSpeed)
    {
        // If there is any optional work we can avoid (for example, because we have
        // traded space for speed) this is the place to turn it off or on.

        return S_OK;
    }

    //*****************************************************************************
    // return the count of entries of a given kind in a scope
    // For example, pass in mdtMethodDef will tell you how many MethodDef
    // contained in a scope
    //*****************************************************************************
    STDMETHODIMP_(ULONG) GetCountWithTokenKind(// return hresult
        DWORD       tkKind)                 // [IN] pass in the kind of token.
        DAC_UNEXPECTED();

    //*****************************************************************************
    // enumerator for typedef
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP EnumTypeDefInit(           // return hresult
        HENUMInternal *phEnum);             // [OUT] buffer to fill for enumerator data

    //*****************************************************************************
    // enumerator for MethodImpl
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP EnumMethodImplInit(        // return hresult
        mdTypeDef       td,                 // [IN] TypeDef over which to scope the enumeration.
        HENUMInternal   *phEnumBody,        // [OUT] buffer to fill for enumerator data for MethodBody tokens.
        HENUMInternal   *phEnumDecl)        // [OUT] buffer to fill for enumerator data for MethodDecl tokens.
        DAC_UNEXPECTED();

    STDMETHODIMP_(ULONG) EnumMethodImplGetCount(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
        DAC_UNEXPECTED();

    STDMETHODIMP_(void) EnumMethodImplReset(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
        DAC_UNEXPECTED();

    __checkReturn
    STDMETHODIMP EnumMethodImplNext(        // return hresult (S_OK = TRUE, S_FALSE = FALSE or error code)
        HENUMInternal   *phEnumBody,        // [IN] input enum for MethodBody
        HENUMInternal   *phEnumDecl,        // [IN] input enum for MethodDecl
        mdToken         *ptkBody,           // [OUT] return token for MethodBody
        mdToken         *ptkDecl)           // [OUT] return token for MethodDecl
        DAC_UNEXPECTED();

    STDMETHODIMP_(void) EnumMethodImplClose(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
        DAC_UNEXPECTED();

    //*****************************************
    // Enumerator helpers for memberdef, memberref, interfaceimp,
    // event, property, param, methodimpl
    //*****************************************

    __checkReturn
    STDMETHODIMP EnumGlobalFunctionsInit(   // return hresult
        HENUMInternal   *phEnum);           // [OUT] buffer to fill for enumerator data

    __checkReturn
    STDMETHODIMP EnumGlobalFieldsInit(      // return hresult
        HENUMInternal   *phEnum);           // [OUT] buffer to fill for enumerator data


    __checkReturn
    STDMETHODIMP EnumInit(                  // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        mdToken     tkParent,               // [IN] token to scope the search
        HENUMInternal *phEnum);             // [OUT] the enumerator to fill

    __checkReturn
    STDMETHODIMP EnumAllInit(               // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        HENUMInternal *phEnum);             // [OUT] the enumerator to fill

    __checkReturn
    STDMETHODIMP EnumCustomAttributeByNameInit(// return S_FALSE if record not found
        mdToken     tkParent,               // [IN] token to scope the search
        LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
        HENUMInternal *phEnum);             // [OUT] the enumerator to fill

    __checkReturn
    STDMETHODIMP GetParentToken(
        mdToken     tkChild,                // [IN] given child token
        mdToken     *ptkParent);            // [OUT] returning parent

    __checkReturn
    STDMETHODIMP GetCustomAttributeProps(
        mdCustomAttribute at,               // The attribute.
        mdToken     *ptkType);              // Put attribute type here.

    __checkReturn
    STDMETHODIMP GetCustomAttributeAsBlob(
        mdCustomAttribute cv,               // [IN] given custom attribute token
        void const  **ppBlob,               // [OUT] return the pointer to internal blob
        ULONG       *pcbSize);              // [OUT] return the size of the blob

    __checkReturn
    STDMETHODIMP GetCustomAttributeByName(  // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData);              // [OUT] Put size of data here.

    __checkReturn
    STDMETHODIMP GetNameOfCustomAttribute(  // S_OK or error.
        mdCustomAttribute mdAttribute,      // [IN] The Custom Attribute
        LPCUTF8          *pszNamespace,     // [OUT] Namespace of Custom Attribute.
        LPCUTF8          *pszName);         // [OUT] Name of Custom Attribute.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetScopeProps(
        LPCSTR      *pszName,               // [OUT] scope name
        GUID        *pmvid);                // [OUT] version id

    // finding a particular method
    __checkReturn
    STDMETHODIMP FindMethodDef(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmd);                  // [OUT] matching memberdef

    // return a iSeq's param given a MethodDef
    __checkReturn
    STDMETHODIMP FindParamOfMethod(         // S_OK or error.
        mdMethodDef md,                     // [IN] The owning method of the param.
        ULONG       iSeq,                   // [IN} The sequence # of the param.
        mdParamDef  *pparamdef);            // [OUT] Put ParamDef token here.

    //*****************************************
    //
    // GetName* functions
    //
    //*****************************************

    // return the name and namespace of typedef
    __checkReturn
    STDMETHODIMP GetNameOfTypeDef(
        mdTypeDef   classdef,               // given classdef
        LPCSTR      *pszname,               // return class name(unqualified)
        LPCSTR      *psznamespace);         // return the name space name

    __checkReturn
    STDMETHODIMP GetIsDualOfTypeDef(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pDual);                // [OUT] return dual flag here.

    __checkReturn
    STDMETHODIMP GetIfaceTypeOfTypeDef(
        mdTypeDef tkTypeDef,
        ULONG *   pIface);  // [OUT] 0=dual, 1=vtable, 2=dispinterface

    __checkReturn
    STDMETHODIMP GetNameOfMethodDef(
        mdMethodDef tkMethodDef,
        LPCSTR *    pszName);

    __checkReturn
    STDMETHODIMP GetNameAndSigOfMethodDef(
        mdMethodDef      methoddef,     // [IN] given memberdef
        PCCOR_SIGNATURE *ppvSigBlob,    // [OUT] point to a blob value of COM+ signature
        ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName);

    // return the name of a FieldDef
    __checkReturn
    STDMETHODIMP GetNameOfFieldDef(
        mdFieldDef fd,          // given memberdef
        LPCSTR    *pszName);

    // return the name of typeref
    __checkReturn
    STDMETHODIMP GetNameOfTypeRef(
        mdTypeRef   classref,               // [IN] given typeref
        LPCSTR      *psznamespace,          // [OUT] return typeref name
        LPCSTR      *pszname);              // [OUT] return typeref namespace

    // return the resolutionscope of typeref
    __checkReturn
    STDMETHODIMP GetResolutionScopeOfTypeRef(
        mdTypeRef classref,                 // given classref
        mdToken  *ptkResolutionScope);

    // return the typeref token given the name.
    __checkReturn
    STDMETHODIMP FindTypeRefByName(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
        LPCSTR      szName,                 // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef   *ptk);                  // [OUT] TypeRef token returned.

    // return the TypeDef properties
    __checkReturn
    STDMETHODIMP GetTypeDefProps(    // return hresult
        mdTypeDef   classdef,               // given classdef
        DWORD       *pdwAttr,               // return flags on class, tdPublic, tdAbstract
        mdToken     *ptkExtends);           // [OUT] Put base class TypeDef/TypeRef here.

    // return the item's guid
    __checkReturn
    STDMETHODIMP GetItemGuid(               // return hresult
        mdToken     tkObj,                  // [IN] given item.
        CLSID       *pGuid);                // [OUT] Put guid here.

    // get enclosing class of NestedClass.
    __checkReturn
    STDMETHODIMP GetNestedClassProps(       // S_OK or error
        mdTypeDef   tkNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptkEnclosingClass);    // [OUT] EnclosingClass token.

    // Get count of Nested classes given the enclosing class.
    __checkReturn
    STDMETHODIMP GetCountNestedClasses(     // return count of Nested classes.
        mdTypeDef   tkEnclosingClass,       // [IN]Enclosing class.
        ULONG      *pcNestedClassesCount);

    // Return array of Nested classes given the enclosing class.
    __checkReturn
    STDMETHODIMP GetNestedClasses(      // Return actual count.
        mdTypeDef   tkEnclosingClass,       // [IN] Enclosing class.
        mdTypeDef   *rNestedClasses,        // [OUT] Array of nested class tokens.
        ULONG       ulNestedClasses,        // [IN] Size of array.
        ULONG      *pcNestedClasses);

    // return the ModuleRef properties
    __checkReturn
    STDMETHODIMP GetModuleRefProps(
        mdModuleRef mur,                    // [IN] moduleref token
        LPCSTR      *pszName);              // [OUT] buffer to fill with the moduleref name


    //*****************************************
    //
    // GetSig* functions
    //
    //*****************************************
    __checkReturn
    STDMETHODIMP GetSigOfMethodDef(
        mdMethodDef      methoddef,         // [IN] given memberdef
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE *ppSig);

    __checkReturn
    STDMETHODIMP GetSigOfFieldDef(
        mdMethodDef      methoddef,         // [IN] given memberdef
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE *ppSig);

    __checkReturn
    STDMETHODIMP GetSigFromToken(
        mdToken           tk, // FieldDef, MethodDef, Signature or TypeSpec token
        ULONG *           pcbSig,
        PCCOR_SIGNATURE * ppSig);



    //*****************************************
    // get method property
    //*****************************************
    __checkReturn
    STDMETHODIMP GetMethodDefProps(
        mdMethodDef md,             // The method for which to get props.
        DWORD      *pdwFlags);

    STDMETHODIMP_(ULONG) GetMethodDefSlot(
        mdMethodDef mb);                    // The method for which to get props.

    //*****************************************
    // return method implementation information, like RVA and implflags
    //*****************************************
    __checkReturn
    STDMETHODIMP GetMethodImplProps(
        mdToken     tk,                     // [IN] MethodDef or MethodImpl
        DWORD       *pulCodeRVA,            // [OUT] CodeRVA
        DWORD       *pdwImplFlags);         // [OUT] Impl. Flags

    //*****************************************************************************
    // return the field RVA
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetFieldRVA(
        mdToken     fd,                     // [IN] FieldDef
        ULONG       *pulCodeRVA);           // [OUT] CodeRVA

    //*****************************************************************************
    // return the field offset for a given field
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetFieldOffset(
        mdFieldDef  fd,                     // [IN] fielddef
        ULONG       *pulOffset);            // [OUT] FieldOffset

    //*****************************************
    // get field property
    //*****************************************
    __checkReturn
    STDMETHODIMP GetFieldDefProps(
        mdFieldDef fd,              // [IN] given fielddef
        DWORD     *pdwFlags);       // [OUT] return fdPublic, fdPrive, etc flags

    //*****************************************************************************
    // return default value of a token(could be paramdef, fielddef, or property
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetDefaultValue(
        mdToken     tk,                     // [IN] given FieldDef, ParamDef, or Property
        MDDefaultValue *pDefaultValue);     // [OUT] default value to fill


    //*****************************************
    // get dispid of a MethodDef or a FieldDef
    //*****************************************
    __checkReturn
    STDMETHODIMP GetDispIdOfMemberDef(      // return hresult
        mdToken     tk,                     // [IN] given methoddef or fielddef
        ULONG       *pDispid);              // [OUT] Put the dispid here.

    //*****************************************
    // return TypeRef/TypeDef given an InterfaceImpl token
    //*****************************************
    __checkReturn
    STDMETHODIMP GetTypeOfInterfaceImpl(    // return the TypeRef/typedef token for the interfaceimpl
        mdInterfaceImpl iiImpl,             // given a interfaceimpl
        mdToken        *ptkType);

    __checkReturn
    STDMETHODIMP GetMethodSpecProps(
        mdMethodSpec mi,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob);           // [OUT] actual size of signature blob

    //*****************************************
    // look up function for TypeDef
    //*****************************************
    __checkReturn
    STDMETHODIMP FindTypeDef(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeDef.
        LPCSTR      szName,                 // [IN] Name of the TypeDef.
        mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef of enclosing class.
        mdTypeDef   *ptypedef);             // [OUT] return typedef

    __checkReturn
    STDMETHODIMP FindTypeDefByGUID(
        REFGUID     guid,                   // guid to look up
        mdTypeDef   *ptypedef);             // return typedef



    //*****************************************
    // return name and sig of a memberref
    //*****************************************
    __checkReturn
    STDMETHODIMP GetNameAndSigOfMemberRef(  // return name here
        mdMemberRef      memberref,         // given memberref
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of COM+ signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName);

    //*****************************************************************************
    // Given memberref, return the parent. It can be TypeRef, ModuleRef, MethodDef
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetParentOfMemberRef(
        mdMemberRef memberref,      // given memberref
        mdToken    *ptkParent);     // return the parent token

    __checkReturn
    STDMETHODIMP GetParamDefProps(
        mdParamDef  paramdef,       // given a paramdef
        USHORT      *pusSequence,   // [OUT] slot number for this parameter
        DWORD       *pdwAttr,       // [OUT] flags
        LPCSTR    *pszName);        // [OUT] return the name of the parameter

    //******************************************
    // property info for method.
    //******************************************
    __checkReturn
    STDMETHODIMP GetPropertyInfoForMethodDef(   // Result.
        mdMethodDef md,                     // [IN] memberdef
        mdProperty  *ppd,                   // [OUT] put property token here
        LPCSTR      *pName,                 // [OUT] put pointer to name here
        ULONG       *pSemantic);            // [OUT] put semantic here

    //*****************************************
    // class layout/sequence information
    //*****************************************
    __checkReturn
    STDMETHODIMP GetClassPackSize(          // [OUT] return error if a class doesn't have packsize info
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwPackSize);          // [OUT] return the pack size of the class. 1, 2, 4, 8 or 16

    __checkReturn
    STDMETHODIMP GetClassTotalSize(         // [OUT] return error if a class doesn't have total size info
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwClassSize);         // [OUT] return the total size of the class

    __checkReturn
    STDMETHODIMP GetClassLayoutInit(
        mdTypeDef   td,                     // [IN] give typedef
        MD_CLASS_LAYOUT *pLayout);          // [OUT] set up the status of query here

    __checkReturn
    STDMETHODIMP GetClassLayoutNext(
        MD_CLASS_LAYOUT *pLayout,           // [IN|OUT] set up the status of query here
        mdFieldDef  *pfd,                   // [OUT] return the fielddef
        ULONG       *pulOffset);            // [OUT] return the offset/ulSequence associate with it

    //*****************************************
    // marshal information of a field
    //*****************************************
    __checkReturn
    STDMETHODIMP GetFieldMarshal(           // return error if no native type associate with the token
        mdFieldDef  fd,                     // [IN] given fielddef
        PCCOR_SIGNATURE *pSigNativeType,    // [OUT] the native type signature
        ULONG       *pcbNativeType);        // [OUT] the count of bytes of *ppvNativeType


    //*****************************************
    // property APIs
    //*****************************************
    // find a property by name
    __checkReturn
    STDMETHODIMP FindProperty(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szPropName,             // [IN] property name
        mdProperty  *pProp);                // [OUT] return property token

    __checkReturn
    STDMETHODIMP GetPropertyProps(
        mdProperty  prop,                   // [IN] property token
        LPCSTR      *szProperty,            // [OUT] property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
        ULONG       *pcbSig);               // [OUT] count of bytes in *ppvSig

    //**********************************
    // Event APIs
    //**********************************
    __checkReturn
    STDMETHODIMP FindEvent(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szEventName,            // [IN] event name
        mdEvent     *pEvent);               // [OUT] return event token

    __checkReturn
    STDMETHODIMP GetEventProps(           // S_OK, S_FALSE, or error.
        mdEvent     ev,                     // [IN] event token
        LPCSTR      *pszEvent,              // [OUT] Event name
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType);         // [OUT] EventType class


    //**********************************
    // Generics APIs
    //**********************************
    __checkReturn
    STDMETHODIMP GetGenericParamProps(        // S_OK or error.
        mdGenericParam rd,                  // [IN] The type parameter
        ULONG* pulSequence,                 // [OUT] Parameter sequence number
        DWORD* pdwAttr,                     // [OUT] Type parameter flags (for future use)
        mdToken *ptOwner,                   // [OUT] The owner (TypeDef or MethodDef)
        DWORD *reserved,                    // [OUT] The kind (TypeDef/Ref/Spec, for future use)
        LPCSTR *szName);                    // [OUT] The name

    __checkReturn
    STDMETHODIMP GetGenericParamConstraintProps(      // S_OK or error.
        mdGenericParamConstraint rd,        // [IN] The constraint token
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType);    // [OUT] TypeDef/Ref/Spec constraint

    //**********************************
    // find a particular associate of a property or an event
    //**********************************
    __checkReturn
    STDMETHODIMP FindAssociate(
        mdToken     evprop,                 // [IN] given a property or event token
        DWORD       associate,              // [IN] given a associate semantics(setter, getter, testdefault, reset, AddOn, RemoveOn, Fire)
        mdMethodDef *pmd);                  // [OUT] return method def token

    __checkReturn
    STDMETHODIMP EnumAssociateInit(
        mdToken     evprop,                 // [IN] given a property or an event token
        HENUMInternal *phEnum);             // [OUT] cursor to hold the query result

    __checkReturn
    STDMETHODIMP GetAllAssociates(
        HENUMInternal *phEnum,              // [IN] query result form GetPropertyAssociateCounts
        ASSOCIATE_RECORD *pAssociateRec,    // [OUT] struct to fill for output
        ULONG       cAssociateRec);         // [IN] size of the buffer


    //**********************************
    // Get info about a PermissionSet.
    //**********************************
    __checkReturn
    STDMETHODIMP GetPermissionSetProps(
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission);        // [OUT] count of bytes of pvPermission.

    //****************************************
    // Get the String given the String token.
    // Returns a pointer to the string, or NULL in case of error.
    //****************************************
    __checkReturn
    STDMETHODIMP GetUserString(
        mdString stk,                   // [IN] the string token.
        ULONG   *pchString,             // [OUT] count of characters in the string.
        BOOL    *pbIs80Plus,            // [OUT] specifies where there are extended characters >= 0x80.
        LPCWSTR *pwszUserString);

    //*****************************************************************************
    // p-invoke APIs.
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetPinvokeMap(
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
        LPCSTR      *pszImportName,         // [OUT] Import name.
        mdModuleRef *pmrImportDLL);         // [OUT] ModuleRef token for the target DLL.

    //*****************************************************************************
    // Assembly MetaData APIs.
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetAssemblyProps(
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,                 // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,                  // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags);     // [OUT] Flags.

    __checkReturn
    STDMETHODIMP GetAssemblyRefProps(
        mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
        const void  **ppbPublicKeyOrToken,          // [OUT] Pointer to the public key or token.
        ULONG       *pcbPublicKeyOrToken,           // [OUT] Count of bytes in the public key or token.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
        const void  **ppbHashValue,         // [OUT] Hash blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
        DWORD       *pdwAssemblyRefFlags);  // [OUT] Flags.

    __checkReturn
    STDMETHODIMP GetFileProps(
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags);         // [OUT] Flags.

    __checkReturn
    STDMETHODIMP GetExportedTypeProps(
        mdExportedType  mdct,                   // [IN] The ExportedType for which to get the properties.
        LPCSTR      *pszNamespace,          // [OUT] Buffer to fill with namespace.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags);     // [OUT] Flags.

    __checkReturn
    STDMETHODIMP GetManifestResourceProps(
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags);     // [OUT] Flags.

    __checkReturn
    STDMETHODIMP FindExportedTypeByName(        // S_OK or error
        LPCSTR      szNamespace,            // [IN] Namespace of the ExportedType.
        LPCSTR      szName,                 // [IN] Name of the ExportedType.
        mdExportedType   tkEnclosingType,        // [IN] Token for the enclosing Type.
        mdExportedType  *pmct);                 // [OUT] Put ExportedType token here.

    __checkReturn
    STDMETHODIMP FindManifestResourceByName(// S_OK or error
        LPCSTR      szName,                 // [IN] Name of the resource.
        mdManifestResource *pmmr);          // [OUT] Put ManifestResource token here.

    __checkReturn
    STDMETHODIMP GetAssemblyFromScope(      // S_OK or error
        mdAssembly  *ptkAssembly);          // [OUT] Put token here.

    //***************************************************************************
    // return properties regarding a TypeSpec
    //***************************************************************************
    __checkReturn
    STDMETHODIMP GetTypeSpecFromToken(   // S_OK or error.
        mdTypeSpec typespec,                // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig);                // [OUT] return size of signature.


    //*****************************************************************************
    // This function gets the "built for" version of a metadata scope.
    //  NOTE: if the scope has never been saved, it will not have a built-for
    //  version, and an empty string will be returned.
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetVersionString(    // S_OK or error.
        LPCSTR      *pVer);               // [OUT] Put version string here.


    //*****************************************************************************
    // helpers to convert a text signature to a com format
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP ConvertTextSigToComSig(    // Return hresult.
        BOOL        fCreateTrIfNotFound,    // [IN] create typeref if not found
        LPCSTR      pSignature,             // [IN] class file format signature
        CQuickBytes *pqbNewSig,             // [OUT] place holder for COM+ signature
        ULONG       *pcbCount);             // [OUT] the result size of signature

    __checkReturn
    STDMETHODIMP SetUserContextData(        // S_OK or E_NOTIMPL
        IUnknown    *pIUnk);                // The user context.

    STDMETHODIMP_(BOOL) IsValidToken(       // True or False.
        mdToken     tk);                    // [IN] Given token.

    STDMETHODIMP_(IUnknown *) GetCachedPublicInterface(BOOL fWithLock);       // return the cached public interface
    __checkReturn
    STDMETHODIMP SetCachedPublicInterface(IUnknown *pUnk);      // return hresult
    STDMETHODIMP_(UTSemReadWrite*) GetReaderWriterLock();       // return the reader writer lock
    __checkReturn
    STDMETHODIMP SetReaderWriterLock(UTSemReadWrite *pSem)
    {
        _ASSERTE(m_pSemReadWrite == NULL);
        m_pSemReadWrite = pSem;
        INDEBUG(m_pStgdb->m_MiniMd.Debug_SetLock(m_pSemReadWrite);)
        return NOERROR;
    }

    // *** IMDInternalImportENC methods ***
    __checkReturn
    STDMETHODIMP ApplyEditAndContinue(      // S_OK or error.
        MDInternalRW *pDelta);              // MD with the ENC delta.

    __checkReturn
    STDMETHODIMP EnumDeltaTokensInit(       // return hresult
        HENUMInternal   *phEnum);           // [OUT] buffer to fill for enumerator data

    STDMETHODIMP_(mdModule) GetModuleFromScope(void);

    // finding a particular method
    __checkReturn
    STDMETHODIMP FindMethodDefUsingCompare(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        PSIGCOMPARE pSignatureCompare,      // [IN] Routine to compare signatures
        void*       pSignatureArgs,         // [IN] Additional info to supply the compare function
        mdMethodDef *pmd);                  // [OUT] matching memberdef

    //*****************************************************************************
    // return the table pointer and size for a given table index
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetTableInfoWithIndex(
        ULONG  index,                       // [IN] pass in the index
        void **pTable,                      // [OUT] pointer to table at index
        void **pTableSize);                 // [OUT] size of table at index

    __checkReturn
    STDMETHODIMP ApplyEditAndContinue(
        void        *pData,                 // [IN] the delta metadata
        ULONG       cbData,                 // [IN] length of pData
        IMDInternalImport **ppv);            // [OUT] the resulting metadata interface


    FORCEINLINE CLiteWeightStgdbRW* GetMiniStgdb() { return m_pStgdb; }
    FORCEINLINE UTSemReadWrite *getReaderWriterLock() { return m_pSemReadWrite; }


    CLiteWeightStgdbRW  *m_pStgdb;

private:
    mdTypeDef           m_tdModule;         // <Module> typedef value.
    LONG                m_cRefs;            // Ref count.
    bool                m_fOwnStgdb;
    IUnknown            *m_pUnk;
    IUnknown            *m_pUserUnk;        // Release at shutdown.
    IMetaDataHelper     *m_pIMetaDataHelper;// pointer to cached public interface
    UTSemReadWrite      *m_pSemReadWrite;   // read write lock for multi-threading.
    bool                m_fOwnSem;          // Does MDInternalRW own this read write lock object?

public:
    STDMETHODIMP_(DWORD) GetMetadataStreamVersion()
    {
        return (DWORD)m_pStgdb->m_MiniMd.m_Schema.m_minor |
               ((DWORD)m_pStgdb->m_MiniMd.m_Schema.m_major << 16);
    };

    __checkReturn
    STDMETHODIMP SetVerifiedByTrustedSource(// return hresult
        BOOL    fVerified)
    {
        m_pStgdb->m_MiniMd.SetVerifiedByTrustedSource(fVerified);
        return S_OK;
    }

    STDMETHODIMP GetRvaOffsetData(// S_OK or error
        DWORD   *pFirstMethodRvaOffset,     // [OUT] Offset (from start of metadata) to the first RVA field in MethodDef table.
        DWORD   *pMethodDefRecordSize,      // [OUT] Size of each record in MethodDef table.
        DWORD   *pMethodDefCount,           // [OUT] Number of records in MethodDef table.
        DWORD   *pFirstFieldRvaOffset,      // [OUT] Offset (from start of metadata) to the first RVA field in FieldRVA table.
        DWORD   *pFieldRvaRecordSize,       // [OUT] Size of each record in FieldRVA table.
        DWORD   *pFieldRvaCount)            // [OUT] Number of records in FieldRVA table.
    {
        return m_pStgdb->m_MiniMd.GetRvaOffsetData(
            pFirstMethodRvaOffset,
            pMethodDefRecordSize,
            pMethodDefCount,
            pFirstFieldRvaOffset,
            pFieldRvaRecordSize,
            pFieldRvaCount);
    }
};  // class MDInternalRW

#endif //FEATURE_METADATA_INTERNAL_APIS

#endif // __MDInternalRW__h__
