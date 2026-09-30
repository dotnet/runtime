#ifndef _SRC_INTERFACES_INTERNAL_METADATAIMPORT_HPP_
#define _SRC_INTERFACES_INTERNAL_METADATAIMPORT_HPP_

#include <cstddef>
#include <cstdint>
#include <cstring>
#include <mutex>

class CQuickBytes;
class IMetaModelCommon;

#include <internal/dnmd_platform.hpp>
#include <metadata.h>
#include "tearoffbase.hpp"
#include "dnmdowner.hpp"
#include "pal.hpp"

class InternalMetadataImportRO : public TearOffBase<IMDInternalImportENC>
{
    mdhandle_view m_handle;
    pal::ReadWriteLock* m_lock;
protected:
    virtual bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
    {
        assert(riid != IID_IUnknown);
        if (riid == IID_IMDInternalImport)
        {
            *ppvObject = static_cast<IMDInternalImport*>(this);
            return true;
        }
        return false;
    }
public:

    InternalMetadataImportRO(IUnknown* controllingUnknown, mdhandle_view md_ptr, pal::ReadWriteLock* lock = nullptr)
        : TearOffBase(controllingUnknown)
        , m_handle{ md_ptr }
        , m_lock{ lock }
    { }
    mdhandle_t MetaData() const { return m_handle.get(); }
    pal::ReadWriteLock* Lock() const { return m_lock; }
public: // IMDInternalImport

    //*****************************************************************************
    // return the count of entries of a given kind in a scope
    // For example, pass in mdtMethodDef will tell you how many MethodDef
    // contained in a scope
    //*****************************************************************************
    STDMETHOD_(ULONG, GetCountWithTokenKind)(// return hresult
        DWORD       tkKind) override;           // [IN] pass in the kind of token.

    //*****************************************************************************
    // enumerator for typedef
    //*****************************************************************************
    __checkReturn
    STDMETHOD(EnumTypeDefInit)(             // return hresult
        HENUMInternal *phEnum) override;        // [OUT] buffer to fill for enumerator data

    //*****************************************************************************
    // enumerator for MethodImpl
    //*****************************************************************************
    __checkReturn
    STDMETHOD(EnumMethodImplInit)(          // return hresult
        mdTypeDef       td,                 // [IN] TypeDef over which to scope the enumeration.
        HENUMInternal   *phEnumBody,        // [OUT] buffer to fill for enumerator data for MethodBody tokens.
        HENUMInternal   *phEnumDecl) override;  // [OUT] buffer to fill for enumerator data for MethodDecl tokens.

    __checkReturn
    STDMETHOD_(ULONG, EnumMethodImplGetCount)(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl) override;  // [IN] MethodDecl enumerator.

    STDMETHOD_(void, EnumMethodImplReset)(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl) override;  // [IN] MethodDecl enumerator.

    __checkReturn
    STDMETHOD(EnumMethodImplNext)(          // return hresult (S_OK = TRUE, S_FALSE = FALSE or error code)
        HENUMInternal   *phEnumBody,        // [IN] input enum for MethodBody
        HENUMInternal   *phEnumDecl,        // [IN] input enum for MethodDecl
        mdToken         *ptkBody,           // [OUT] return token for MethodBody
        mdToken         *ptkDecl) override;     // [OUT] return token for MethodDecl

    STDMETHOD_(void, EnumMethodImplClose)(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *phEnumDecl) override;  // [IN] MethodDecl enumerator.

    //*****************************************
    // Enumerator helpers for memberdef, memberref, interfaceimp,
    // event, property, exception, param
    //*****************************************

    __checkReturn
    STDMETHOD(EnumGlobalFunctionsInit)(     // return hresult
        HENUMInternal   *phEnum) override;      // [OUT] buffer to fill for enumerator data

    __checkReturn
    STDMETHOD(EnumGlobalFieldsInit)(        // return hresult
        HENUMInternal   *phEnum) override;      // [OUT] buffer to fill for enumerator data

    __checkReturn
    STDMETHOD(EnumInit)(                    // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        mdToken     tkParent,               // [IN] token to scope the search
        HENUMInternal *phEnum) override;        // [OUT] the enumerator to fill

    __checkReturn
    STDMETHOD(EnumAllInit)(                 // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        HENUMInternal *phEnum) override;        // [OUT] the enumerator to fill

    __checkReturn
    STDMETHOD_(bool, EnumNext)(
        HENUMInternal *phEnum,              // [IN] the enumerator to retrieve information
        mdToken     *ptk) override;                   // [OUT] token to scope the search

    __checkReturn
    STDMETHOD_(ULONG, EnumGetCount)(
        HENUMInternal *phEnum) override;        // [IN] the enumerator to retrieve information

    __checkReturn
    STDMETHOD_(void, EnumReset)(
        HENUMInternal *phEnum) override;        // [IN] the enumerator to be reset

    __checkReturn
    STDMETHOD_(void, EnumClose)(
        HENUMInternal *phEnum) override;        // [IN] the enumerator to be closed

    //*****************************************
    // Enumerator helpers for CustomAttribute
    //*****************************************
    __checkReturn
    STDMETHOD(EnumCustomAttributeByNameInit)(// return S_FALSE if record not found
        mdToken     tkParent,               // [IN] token to scope the search
        LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
        HENUMInternal *phEnum) override;        // [OUT] the enumerator to fill

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
        mdToken     *ptkParent) override;       // [OUT] returning parent

    //*****************************************
    // Custom value helpers
    //*****************************************
    __checkReturn
    STDMETHOD(GetCustomAttributeProps)(     // S_OK or error.
        mdCustomAttribute at,               // [IN] The attribute.
        mdToken     *ptkType) override;         // [OUT] Put attribute type here.

    __checkReturn
    STDMETHOD(GetCustomAttributeAsBlob)(
        mdCustomAttribute cv,               // [IN] given custom value token
        void const  **ppBlob,               // [OUT] return the pointer to internal blob
        ULONG       *pcbSize) override;         // [OUT] return the size of the blob

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD (GetScopeProps)(
        LPCSTR      *pszName,               // [OUT] scope name
        GUID        *pmvid) override;           // [OUT] version id

    // finding a particular method
    __checkReturn
    STDMETHOD(FindMethodDef)(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmd) override;             // [OUT] matching memberdef

    // return a iSeq's param given a MethodDef
    __checkReturn
    STDMETHOD(FindParamOfMethod)(           // S_OK or error.
        mdMethodDef md,                     // [IN] The owning method of the param.
        ULONG       iSeq,                   // [IN] The sequence # of the param.
        mdParamDef  *pparamdef) override;       // [OUT] Put ParamDef token here.

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
        LPCSTR      *psznamespace) override;    // return the name space name

    __checkReturn
    STDMETHOD(GetIsDualOfTypeDef)(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pDual) override;           // [OUT] return dual flag here.

    __checkReturn
    STDMETHOD(GetIfaceTypeOfTypeDef)(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pIface) override;          // [OUT] 0=dual, 1=vtable, 2=dispinterface

    // get the name of either methoddef
    __checkReturn
    STDMETHOD(GetNameOfMethodDef)(  // return the name of the memberdef in UTF8
        mdMethodDef md,             // given memberdef
        LPCSTR     *pszName) override;

    __checkReturn
    STDMETHOD(GetNameAndSigOfMethodDef)(
        mdMethodDef      methoddef,         // [IN] given memberdef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName) override;

    // return the name of a FieldDef
    __checkReturn
    STDMETHOD(GetNameOfFieldDef)(
        mdFieldDef fd,              // given memberdef
        LPCSTR    *pszName) override;

    // return the name of typeref
    __checkReturn
    STDMETHOD(GetNameOfTypeRef)(
        mdTypeRef   classref,               // [IN] given typeref
        LPCSTR      *psznamespace,          // [OUT] return typeref name
        LPCSTR      *pszname) override;         // [OUT] return typeref namespace

    // return the resolutionscope of typeref
    __checkReturn
    STDMETHOD(GetResolutionScopeOfTypeRef)(
        mdTypeRef classref,                     // given classref
        mdToken  *ptkResolutionScope) override;

    // Find the type token given the name.
    __checkReturn
    STDMETHOD(FindTypeRefByName)(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
        LPCSTR      szName,                 // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef   *ptk) override;             // [OUT] TypeRef token returned.

    // return the TypeDef properties
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetTypeDefProps)(
        mdTypeDef   classdef,               // given classdef
        DWORD       *pdwAttr,               // return flags on class, tdPublic, tdAbstract
        mdToken     *ptkExtends) override;      // [OUT] Put base class TypeDef/TypeRef here

    // return the item's guid
    __checkReturn
    STDMETHOD(GetItemGuid)(
        mdToken     tkObj,                  // [IN] given item.
        CLSID       *pGuid) override;           // [out[ put guid here.

    // Get enclosing class of the NestedClass.
    __checkReturn
    STDMETHOD(GetNestedClassProps)(         // S_OK or error
        mdTypeDef   tkNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptkEnclosingClass) override; // [OUT] EnclosingClass token.

    // Get count of Nested classes given the enclosing class.
    __checkReturn
    STDMETHOD(GetCountNestedClasses)(   // return count of Nested classes.
        mdTypeDef   tkEnclosingClass,   // Enclosing class.
        ULONG      *pcNestedClassesCount) override;

    // Return array of Nested classes given the enclosing class.
    __checkReturn
    STDMETHOD(GetNestedClasses)(        // Return actual count.
        mdTypeDef   tkEnclosingClass,       // [IN] Enclosing class.
        mdTypeDef   *rNestedClasses,        // [OUT] Array of nested class tokens.
        ULONG       ulNestedClasses,        // [IN] Size of array.
        ULONG      *pcNestedClasses) override;

    // return the ModuleRef properties
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,                    // [IN] moduleref token
        LPCSTR      *pszName) override;         // [OUT] buffer to fill with the moduleref name

    //*****************************************
    //
    // GetSig* functions
    //
    //*****************************************
    __checkReturn
    STDMETHOD(GetSigOfMethodDef)(
        mdMethodDef       tkMethodDef,  // [IN] given MethodDef
        ULONG *           pcbSigBlob,   // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE * ppSig) override;

    __checkReturn
    STDMETHOD(GetSigOfFieldDef)(
        mdFieldDef        tkFieldDef,   // [IN] given FieldDef
        ULONG *           pcbSigBlob,   // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE * ppSig) override;

    __checkReturn
    STDMETHOD(GetSigFromToken)(
        mdToken           tk, // FieldDef, MethodDef, Signature or TypeSpec token
        ULONG *           pcbSig,
        PCCOR_SIGNATURE * ppSig) override;



    //*****************************************
    // get method property
    //*****************************************
    __checkReturn
    STDMETHOD(GetMethodDefProps)(
        mdMethodDef md,                 // The method for which to get props.
        DWORD      *pdwFlags) override;

    //*****************************************
    // return method implementation information, like RVA and implflags
    //*****************************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetMethodImplProps)(
        mdToken     tk,                     // [IN] MethodDef
        ULONG       *pulCodeRVA,            // [OUT] CodeRVA
        DWORD       *pdwImplFlags) override;    // [OUT] Impl. Flags

    //*****************************************
    // return method implementation information, like RVA and implflags
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldRVA)(
        mdFieldDef  fd,                     // [IN] fielddef
        ULONG       *pulCodeRVA) override;      // [OUT] CodeRVA

    //*****************************************
    // get field property
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldDefProps)(
        mdFieldDef fd,              // [IN] given fielddef
        DWORD     *pdwFlags) override;  // [OUT] return fdPublic, fdPrive, etc flags

    //*****************************************************************************
    // return default value of a token(could be paramdef, fielddef, or property
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetDefaultValue)(
        mdToken     tk,                     // [IN] given FieldDef, ParamDef, or Property
        MDDefaultValue *pDefaultValue) override;// [OUT] default value to fill


    //*****************************************
    // get dispid of a MethodDef or a FieldDef
    //*****************************************
    __checkReturn
    STDMETHOD(GetDispIdOfMemberDef)(        // return hresult
        mdToken     tk,                     // [IN] given methoddef or fielddef
        ULONG       *pDispid) override;         // [OUT] Put the dispid here.

    //*****************************************
    // return TypeRef/TypeDef given an InterfaceImpl token
    //*****************************************
    __checkReturn
    STDMETHOD(GetTypeOfInterfaceImpl)(  // return the TypeRef/typedef token for the interfaceimpl
        mdInterfaceImpl iiImpl,         // given a interfaceimpl
        mdToken        *ptkType) override;

    //*****************************************
    // look up function for TypeDef
    //*****************************************
    __checkReturn
    STDMETHOD(FindTypeDef)(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeDef.
        LPCSTR      szName,                 // [IN] Name of the TypeDef.
        mdToken     tkEnclosingClass,       // [IN] TypeRef/TypeDef Token for the enclosing class.
        mdTypeDef   *ptypedef) override;        // [IN] return typedef

    //*****************************************
    // return name and sig of a memberref
    //*****************************************
    __checkReturn
    STDMETHOD(GetNameAndSigOfMemberRef)(    // return name here
        mdMemberRef      memberref,         // given memberref
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName) override;

    //*****************************************************************************
    // Given memberref, return the parent. It can be TypeRef, ModuleRef, MethodDef
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetParentOfMemberRef)(
        mdMemberRef memberref,          // given memberref
        mdToken    *ptkParent) override;    // return the parent token

    __checkReturn
    STDMETHOD(GetParamDefProps)(
        mdParamDef paramdef,            // given a paramdef
        USHORT    *pusSequence,         // [OUT] slot number for this parameter
        DWORD     *pdwAttr,             // [OUT] flags
        LPCSTR    *pszName) override;       // [OUT] return the name of the parameter

    __checkReturn
    STDMETHOD(GetPropertyInfoForMethodDef)( // Result.
        mdMethodDef md,                     // [IN] memberdef
        mdProperty  *ppd,                   // [OUT] put property token here
        LPCSTR      *pName,                 // [OUT] put pointer to name here
        ULONG       *pSemantic) override;       // [OUT] put semantic here

    //*****************************************
    // class layout/sequence information
    //*****************************************
    __checkReturn
    STDMETHOD(GetClassPackSize)(            // return error if class doesn't have packsize
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwPackSize) override;     // [OUT] 1, 2, 4, 8, or 16

    __checkReturn
    STDMETHOD(GetClassTotalSize)(           // return error if class doesn't have total size info
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwClassSize) override;    // [OUT] return the total size of the class

    __checkReturn
    STDMETHOD(GetClassLayoutInit)(
        mdTypeDef   td,                     // [IN] give typedef
        MD_CLASS_LAYOUT *pLayout) override;     // [OUT] set up the status of query here

    __checkReturn
    STDMETHOD(GetClassLayoutNext)(
        MD_CLASS_LAYOUT *pLayout,           // [IN|OUT] set up the status of query here
        mdFieldDef  *pfd,                   // [OUT] return the fielddef
        ULONG       *pulOffset) override;       // [OUT] return the offset/ulSequence associate with it

    //*****************************************
    // marshal information of a field
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldMarshal)(             // return error if no native type associate with the token
        mdFieldDef  fd,                     // [IN] given fielddef
        PCCOR_SIGNATURE *pSigNativeType,    // [OUT] the native type signature
        ULONG       *pcbNativeType) override;   // [OUT] the count of bytes of *ppvNativeType


    //*****************************************
    // property APIs
    //*****************************************
    // find a property by name
    __checkReturn
    STDMETHOD(FindProperty)(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szPropName,             // [IN] property name
        mdProperty  *pProp) override;           // [OUT] return property token

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetPropertyProps)(
        mdProperty  prop,                   // [IN] property token
        LPCSTR      *szProperty,            // [OUT] property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
        ULONG       *pcbSig) override;          // [OUT] count of bytes in *ppvSig

    //**********************************
    // Event APIs
    //**********************************
    __checkReturn
    STDMETHOD(FindEvent)(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szEventName,            // [IN] event name
        mdEvent     *pEvent) override;          // [OUT] return event token

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetEventProps)(
        mdEvent     ev,                     // [IN] event token
        LPCSTR      *pszEvent,              // [OUT] Event name
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType) override;    // [OUT] EventType class


    //**********************************
    // find a particular associate of a property or an event
    //**********************************
    __checkReturn
    STDMETHOD(FindAssociate)(
        mdToken     evprop,                 // [IN] given a property or event token
        DWORD       associate,              // [IN] given a associate semantics(setter, getter, testdefault, reset, AddOn, RemoveOn, Fire)
        mdMethodDef *pmd) override;             // [OUT] return method def token

    // Note, void function in v1.0/v1.1
    __checkReturn
    STDMETHOD(EnumAssociateInit)(
        mdToken     evprop,                 // [IN] given a property or an event token
        HENUMInternal *phEnum) override;        // [OUT] cursor to hold the query result

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetAllAssociates)(
        HENUMInternal *phEnum,              // [IN] query result form GetPropertyAssociateCounts
        ASSOCIATE_RECORD *pAssociateRec,    // [OUT] struct to fill for output
        ULONG       cAssociateRec) override;    // [IN] size of the buffer


    //**********************************
    // Get info about a PermissionSet.
    //**********************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetPermissionSetProps)(
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission) override;   // [OUT] count of bytes of pvPermission.

    //****************************************
    // Get the String given the String token.
    // Returns a pointer to the string, or NULL in case of error.
    //****************************************
    __checkReturn
    STDMETHOD(GetUserString)(
        mdString stk,                   // [IN] the string token.
        ULONG   *pchString,             // [OUT] count of characters in the string.
        LPCWSTR *pwszUserString) override;

    //*****************************************************************************
    // p-invoke APIs.
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetPinvokeMap)(
        mdToken     tk,                     // [IN] FieldDef, MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
        LPCSTR      *pszImportName,         // [OUT] Import name.
        mdModuleRef *pmrImportDLL) override;    // [OUT] ModuleRef token for the target DLL.

    //*****************************************************************************
    // helpers to convert a text signature to a com format
    //*****************************************************************************
    __checkReturn
    STDMETHOD(ConvertTextSigToComSig)(      // Return hresult.
        BOOL        fCreateTrIfNotFound,    // [IN] create typeref if not found
        LPCSTR      pSignature,             // [IN] class file format signature
        CQuickBytes *pqbNewSig,             // [OUT] place holder for CLR signature
        ULONG       *pcbCount) override;        // [OUT] the result size of signature

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
        DWORD       *pdwAssemblyFlags) override;// [OUT] Flags.

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
        DWORD       *pdwAssemblyRefFlags) override; // [OUT] Flags.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetFileProps)(
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags) override;    // [OUT] Flags.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetExportedTypeProps)(
        mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
        LPCSTR      *pszNamespace,          // [OUT] Namespace.
        LPCSTR      *pszName,               // [OUT] Name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags) override; // [OUT] Flags.

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHOD(GetManifestResourceProps)(
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags) override;// [OUT] Flags.

    __checkReturn
    STDMETHOD(FindExportedTypeByName)(      // S_OK or error
        LPCSTR      szNamespace,            // [IN] Namespace of the ExportedType.
        LPCSTR      szName,                 // [IN] Name of the ExportedType.
        mdExportedType   tkEnclosingType,   // [IN] ExportedType for the enclosing class.
        mdExportedType   *pmct) override;       // [OUT] Put ExportedType token here.

    __checkReturn
    STDMETHOD(FindManifestResourceByName)(  // S_OK or error
        LPCSTR      szName,                 // [IN] Name of the ManifestResource.
        mdManifestResource *pmmr) override;     // [OUT] Put ManifestResource token here.

    __checkReturn
    STDMETHOD(GetAssemblyFromScope)(        // S_OK or error
        mdAssembly  *ptkAssembly) override;     // [OUT] Put token here.

    __checkReturn
    STDMETHOD(GetCustomAttributeByName)(    // S_OK or error
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCSTR     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData) override;         // [OUT] Put size of data here.

    // Note: The return type of this method was void in v1
    __checkReturn
    STDMETHOD(GetTypeSpecFromToken)(      // S_OK or error.
        mdTypeSpec typespec,                // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig) override;               // [OUT] return size of signature.

    __checkReturn
    STDMETHOD(SetUserContextData)(          // S_OK or E_NOTIMPL
        IUnknown    *pIUnk) override;           // The user context.

    __checkReturn
    STDMETHOD_(BOOL, IsValidToken)(         // True or False.
        mdToken     tk) override;               // [IN] Given token.

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
        ULONG       *pcbSig) override;          // [OUT] count of bytes in the translated signature

    STDMETHOD_(IMetaModelCommon*, GetMetaModelCommon)(  // Return MetaModelCommon interface.
        ) override;

    STDMETHOD_(IUnknown *, GetCachedPublicInterface)(BOOL fWithLock) override;   // return the cached public interface
    __checkReturn
    STDMETHOD(SetCachedPublicInterface)(IUnknown *pUnk) override;  // no return value
    STDMETHOD_(minipal_rwlock*, GetReaderWriterLock)() override;   // return the reader writer lock
    __checkReturn
    STDMETHOD(SetReaderWriterLock)(minipal_rwlock * pLock) override;

    STDMETHOD_(mdModule, GetModuleFromScope)() override;             // [OUT] Put mdModule token here.


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
        mdMethodDef *pmd) override;             // [OUT] matching memberdef

    // Additional v2 methods.

    //*****************************************
    // return a field offset for a given field
    //*****************************************
    __checkReturn
    STDMETHOD(GetFieldOffset)(
        mdFieldDef  fd,                     // [IN] fielddef
        ULONG       *pulOffset) override;       // [OUT] FieldOffset

    __checkReturn
    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec ms,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob) override;      // [OUT] actual size of signature blob

    __checkReturn
    STDMETHOD(GetTableInfoWithIndex)(
        ULONG      index,                   // [IN] pass in the table index
        void       **pTable,                // [OUT] pointer to table at index
        void       **pTableSize) override;      // [OUT] size of table at index

    __checkReturn
    STDMETHOD(ApplyEditAndContinue)(
        void        *pDeltaMD,              // [IN] the delta metadata
        ULONG       cbDeltaMD,              // [IN] length of pData
        IMDInternalImport **ppv) override;      // [OUT] the resulting metadata interface

    STDMETHOD(ApplyEditAndContinue)(MDInternalRW* pDeltaMD) override;
    STDMETHOD(EnumDeltaTokensInit)(HENUMInternal* phEnum) override;

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
        LPCSTR *szName) override;               // [OUT] The name

    __checkReturn
    STDMETHOD(GetGenericParamConstraintProps)(      // S_OK or error.
        mdGenericParamConstraint rd,            // [IN] The constraint token
        mdGenericParam *ptGenericParam,         // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType) override;  // [OUT] TypeDef/Ref/Spec constraint

    //*****************************************************************************
    // This function gets the "built for" version of a metadata scope.
    //  NOTE: if the scope has never been saved, it will not have a built-for
    //  version, and an empty string will be returned.
    //*****************************************************************************
    __checkReturn
    STDMETHOD(GetVersionString)(    // S_OK or error.
        LPCSTR      *pVer) override;       // [OUT] Put version string here.


    __checkReturn
    STDMETHOD(GetTypeDefRefTokenInTypeSpec)(// return S_FALSE if enclosing type does not have a token
        mdTypeSpec  tkTypeSpec,               // [IN] TypeSpec token to look at
        mdToken    *tkEnclosedToken) override;    // [OUT] The enclosed type token

#define MD_STREAM_VER_1X    0x10000
#define MD_STREAM_VER_2_B1  0x10001
#define MD_STREAM_VER_2     0x20000
    STDMETHOD_(DWORD, GetMetadataStreamVersion)() override;  //returns DWORD with major version of
                                // MD stream in senior word and minor version--in junior word

    __checkReturn
    STDMETHOD(GetNameOfCustomAttribute)(// S_OK or error
        mdCustomAttribute mdAttribute,      // [IN] The Custom Attribute
        LPCSTR          *pszNamespace,     // [OUT] Namespace of Custom Attribute.
        LPCSTR          *pszName) override;    // [OUT] Name of Custom Attribute.

};

class InternalMetadataImportRW final : public InternalMetadataImportRO
{
    minipal::com_ptr<IUnknown> _userContext;
    std::mutex _contextMutex;
protected:
    bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
    {
        if (InternalMetadataImportRO::TryGetInterfaceOnThis(riid, ppvObject))
            return true;
        if (riid == IID_IMDInternalImportENC)
        {
            *ppvObject = static_cast<IMDInternalImportENC*>(this);
            return true;
        }
        return false;
    }

public:
    using InternalMetadataImportRO::InternalMetadataImportRO;

    STDMETHOD(ApplyEditAndContinue)(
        void* pDeltaMD,
        ULONG cbDeltaMD,
        IMDInternalImport** ppv) override;

    STDMETHOD(EnumDeltaTokensInit)(HENUMInternal* phEnum) override;

    STDMETHOD(SetUserContextData)(IUnknown* context) override;
};

#endif // _SRC_INTERFACES_INTERNAL_METADATAIMPORT_HPP_
