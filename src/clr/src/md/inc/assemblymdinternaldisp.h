// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// AssemblyMDInternalDispenser.h
// 

//
// Contains utility code for MD directory
//
//*****************************************************************************
#ifndef __AssemblyMDInternalDispenser__h__
#define __AssemblyMDInternalDispenser__h__

#include "../runtime/mdinternalro.h"

#ifdef FEATURE_FUSION

#include "fusionpriv.h"

struct CORCOMPILE_VERSION_INFO;
struct CORCOMPILE_DEPENDENCY;

//*****************************************************************************
// This class can support the IMetaDataAssemblyImport and some funcationalities 
// of IMetaDataImport on the internal import interface (IMDInternalImport).
//*****************************************************************************
class AssemblyMDInternalImport :
    public IMetaDataAssemblyImport,
    public IMetaDataImport2,
#ifdef FEATURE_PREJIT
    public IGetIMDInternalImport,
#endif //FEATURE_PREJIT
    public ISNAssemblySignature
#ifdef FEATURE_PREJIT
    , public INativeImageInstallInfo
#endif  // FEATURE_PREJIT
{
public:
    AssemblyMDInternalImport(IMDInternalImport *pMDInternalImport);
    ~AssemblyMDInternalImport();

    // *** IUnknown methods ***
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppUnk);
    STDMETHODIMP_(ULONG) AddRef(void);
    STDMETHODIMP_(ULONG) Release(void);

    // *** IMetaDataAssemblyImport methods ***
    STDMETHODIMP GetAssemblyProps (         // S_OK or error.
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags);         // [OUT] Flags.

    STDMETHODIMP GetAssemblyRefProps (      // S_OK or error.
        mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
        const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
        ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
        __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        const void  **ppbHashValue,         // [OUT] Hash blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
        DWORD       *pdwAssemblyRefFlags);      // [OUT] Flags.

    STDMETHODIMP GetFileProps (             // S_OK or error.
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags);         // [OUT] Flags.

    STDMETHODIMP GetExportedTypeProps (          // S_OK or error.
        mdExportedType   mdct,                   // [IN] The ExportedType for which to get the properties.
        __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef or mdExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags);      // [OUT] Flags.

    STDMETHODIMP GetManifestResourceProps ( // S_OK or error.
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        __out_ecount (cchName) LPWSTR szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ManifestResource.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags);     // [OUT] Flags.

    STDMETHODIMP EnumAssemblyRefs (         // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdAssemblyRef rAssemblyRefs[],      // [OUT] Put AssemblyRefs here.
        ULONG       cMax,                   // [IN] Max AssemblyRefs to put.
        ULONG       *pcTokens);             // [OUT] Put # put here.

    STDMETHODIMP EnumFiles (                // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdFile      rFiles[],               // [OUT] Put Files here.
        ULONG       cMax,                   // [IN] Max Files to put.
        ULONG       *pcTokens);             // [OUT] Put # put here.

    STDMETHODIMP EnumExportedTypes (        // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdExportedType   rExportedTypes[],  // [OUT] Put ExportedTypes here.
        ULONG       cMax,                   // [IN] Max ExportedTypes to put.
        ULONG       *pcTokens);             // [OUT] Put # put here.

    STDMETHODIMP EnumManifestResources (    // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdManifestResource  rManifestResources[],   // [OUT] Put ManifestResources here.
        ULONG       cMax,                   // [IN] Max Resources to put.
        ULONG       *pcTokens);             // [OUT] Put # put here.

    STDMETHODIMP GetAssemblyFromScope (     // S_OK or error
        mdAssembly  *ptkAssembly);          // [OUT] Put token here.

    STDMETHODIMP FindExportedTypeByName (   // S_OK or error
        LPCWSTR     szName,                 // [IN] Name of the ExportedType.
        mdToken     mdtExportedType,        // [IN] ExportedType for the enclosing class.
        mdExportedType   *ptkExportedType);      // [OUT] Put the ExportedType token here.

    STDMETHODIMP FindManifestResourceByName (  // S_OK or error
        LPCWSTR     szName,                 // [IN] Name of the ManifestResource.
        mdManifestResource *ptkManifestResource);       // [OUT] Put the ManifestResource token here.

    STDMETHOD_(void, CloseEnum)(
        HCORENUM hEnum);                    // Enum to be closed.

    STDMETHODIMP FindAssembliesByName (     // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        IUnknown *ppIUnk[],                 // [OUT] put IMetaDataAssemblyImport pointers here
        ULONG    cMax,                      // [IN] The max number to put
        ULONG    *pcAssemblies);            // [OUT] The number of assemblies returned.

    // *** IMetaDataImport methods ***
    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG *pulCount);
    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos);     
    STDMETHOD(EnumTypeDefs)(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs);     
    STDMETHOD(EnumInterfaceImpls)(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls);     
    STDMETHOD(EnumTypeRefs)(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs);     

    STDMETHOD(FindTypeDefByName)(           // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of the Type.
        mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef for Enclosing class.
        mdTypeDef   *ptd);                  // [OUT] Put the TypeDef token here.

    STDMETHOD(GetScopeProps)(
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR  wszName,    // [OUT] Put the name here.
        ULONG   cchName,    // [IN] Size of name buffer in wide chars.
        ULONG * pchName,    // [OUT] Put size of name (wide chars) here.
        GUID *  pMvid);     // [OUT, OPTIONAL] Put MVID here.

    STDMETHOD(GetModuleFromScope)(          // S_OK.
        mdModule    *pmd);                  // [OUT] Put mdModule token here.

    STDMETHOD(GetTypeDefProps)(
        mdTypeDef td,               // [IN] TypeDef token for inquiry.
      __out_ecount_part_opt(cchTypeDef, *pchTypeDef)
        LPWSTR    wszTypeDef,       // [OUT] Put name here.
        ULONG     cchTypeDef,       // [IN] size of name buffer in wide chars.
        ULONG *   pchTypeDef,       // [OUT] put size of name (wide chars) here.
        DWORD *   pdwTypeDefFlags,  // [OUT] Put flags here.
        mdToken * ptkExtends);      // [OUT] Put base class TypeDef/TypeRef here.

    STDMETHOD(GetInterfaceImplProps)(       // S_OK or error.
        mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
        mdTypeDef   *pClass,                // [OUT] Put implementing class token here.
        mdToken     *ptkIface);             // [OUT] Put implemented interface token here.              

    STDMETHOD(GetTypeRefProps)(
        mdTypeRef tr,                   // [IN] TypeRef token.
        mdToken * ptkResolutionScope,   // [OUT] Resolution scope, ModuleRef or AssemblyRef.
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR    wszName,              // [OUT] Name of the TypeRef.
        ULONG     cchName,              // [IN] Size of buffer.
        ULONG *   pchName);             // [OUT] Size of Name.

    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd);     

    STDMETHOD(EnumMembers)(                 // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.   
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.   
        ULONG       cMax,                   // [IN] Max MemberDefs to put.  
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumMembersWithName)(         // S_OK, S_FALSE, or error.             
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.                
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.   
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.              
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.                   
        ULONG       cMax,                   // [IN] Max MemberDefs to put.              
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumMethods)(                 // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.   
        mdMethodDef rMethods[],             // [OUT] Put MethodDefs here.   
        ULONG       cMax,                   // [IN] Max MethodDefs to put.  
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumMethodsWithName)(         // S_OK, S_FALSE, or error.             
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.                
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.   
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.              
        mdMethodDef rMethods[],             // [OU] Put MethodDefs here.    
        ULONG       cMax,                   // [IN] Max MethodDefs to put.              
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumFields)(                 // S_OK, S_FALSE, or error.  
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.   
        mdFieldDef  rFields[],              // [OUT] Put FieldDefs here.    
        ULONG       cMax,                   // [IN] Max FieldDefs to put.   
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumFieldsWithName)(         // S_OK, S_FALSE, or error.              
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.                
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.   
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.              
        mdFieldDef  rFields[],              // [OUT] Put MemberDefs here.                   
        ULONG       cMax,                   // [IN] Max MemberDefs to put.              
        ULONG       *pcTokens);             // [OUT] Put # put here.    


    STDMETHOD(EnumParams)(                  // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
        mdParamDef  rParams[],              // [OUT] Put ParamDefs here.    
        ULONG       cMax,                   // [IN] Max ParamDefs to put.   
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumMemberRefs)(              // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdToken     tkParent,               // [IN] Parent token to scope the enumeration.  
        mdMemberRef rMemberRefs[],          // [OUT] Put MemberRefs here.   
        ULONG       cMax,                   // [IN] Max MemberRefs to put.  
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumMethodImpls)(             // S_OK, S_FALSE, or error  
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.   
        mdToken     rMethodBody[],          // [OUT] Put Method Body tokens here.   
        mdToken     rMethodDecl[],          // [OUT] Put Method Declaration tokens here.
        ULONG       cMax,                   // [IN] Max tokens to put.  
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(EnumPermissionSets)(          // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdToken     tk,                     // [IN] if !NIL, token to scope the enumeration.    
        DWORD       dwActions,              // [IN] if !0, return only these actions.   
        mdPermission rPermission[],         // [OUT] Put Permissions here.  
        ULONG       cMax,                   // [IN] Max Permissions to put. 
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(FindMember)(  
        mdTypeDef   td,                     // [IN] given typedef   
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob    
        mdToken     *pmb);                  // [OUT] matching memberdef 

    STDMETHOD(FindMethod)(  
        mdTypeDef   td,                     // [IN] given typedef   
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob    
        mdMethodDef *pmb);                  // [OUT] matching memberdef 

    STDMETHOD(FindField)(   
        mdTypeDef   td,                     // [IN] given typedef   
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob    
        mdFieldDef  *pmb);                  // [OUT] matching memberdef 

    STDMETHOD(FindMemberRef)(   
        mdTypeRef   td,                     // [IN] given typeRef   
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob    
        mdMemberRef *pmr);                  // [OUT] matching memberref 

    STDMETHOD (GetMethodProps)( 
        mdMethodDef       mb,               // The method for which to get props.
        mdTypeDef   *     pClass,           // Put method's class here.
      __out_ecount_part_opt(cchMethod, *pchMethod)
        LPWSTR            wszMethod,        // Put method's name here.
        ULONG             cchMethod,        // Size of szMethod buffer in wide chars.
        ULONG *           pchMethod,        // Put actual size here.
        DWORD *           pdwAttr,          // Put flags here.
        PCCOR_SIGNATURE * ppvSigBlob,       // [OUT] point to the blob value of meta data
        ULONG *           pcbSigBlob,       // [OUT] actual size of signature blob
        ULONG *           pulCodeRVA,       // [OUT] codeRVA
        DWORD *           pdwImplFlags);    // [OUT] Impl. Flags

    STDMETHOD(GetMemberRefProps)(
        mdMemberRef       mr,           // [IN] given memberref 
        mdToken *         ptk,          // [OUT] Put classref or classdef here. 
      __out_ecount_part_opt(cchMember, *pchMember)
        LPWSTR            wszMember,    // [OUT] buffer to fill for member's name   
        ULONG             cchMember,    // [IN] the count of char of szMember   
        ULONG *           pchMember,    // [OUT] actual count of char in member name    
        PCCOR_SIGNATURE * ppvSigBlob,   // [OUT] point to meta data blob value  
        ULONG *           pbSig);       // [OUT] actual size of signature blob  

    STDMETHOD(EnumProperties)(              // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.   
        mdProperty  rProperties[],          // [OUT] Put Properties here.   
        ULONG       cMax,                   // [IN] Max properties to put.  
        ULONG       *pcProperties);         // [OUT] Put # put here.    

    STDMETHOD(EnumEvents)(                  // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.   
        mdEvent     rEvents[],              // [OUT] Put events here.   
        ULONG       cMax,                   // [IN] Max events to put.  
        ULONG       *pcEvents);             // [OUT] Put # put here.    

    STDMETHOD(GetEventProps)(               // S_OK, S_FALSE, or error. 
        mdEvent     ev,                     // [IN] event token 
        mdTypeDef   *pClass,                // [OUT] typedef containing the event declarion.    
        LPCWSTR     szEvent,                // [OUT] Event name 
        ULONG       cchEvent,               // [IN] the count of wchar of szEvent   
        ULONG       *pchEvent,              // [OUT] actual count of wchar for event's name 
        DWORD       *pdwEventFlags,         // [OUT] Event flags.   
        mdToken     *ptkEventType,          // [OUT] EventType class    
        mdMethodDef *pmdAddOn,              // [OUT] AddOn method of the event  
        mdMethodDef *pmdRemoveOn,           // [OUT] RemoveOn method of the event   
        mdMethodDef *pmdFire,               // [OUT] Fire method of the event   
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the event  
        ULONG       cMax,                   // [IN] size of rmdOtherMethod  
        ULONG       *pcOtherMethod);        // [OUT] total number of other method of this event 

    STDMETHOD(EnumMethodSemantics)(         // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
        mdToken     rEventProp[],           // [OUT] Put Event/Property here.   
        ULONG       cMax,                   // [IN] Max properties to put.  
        ULONG       *pcEventProp);          // [OUT] Put # put here.    

    STDMETHOD(GetMethodSemantics)(          // S_OK, S_FALSE, or error. 
        mdMethodDef mb,                     // [IN] method token    
        mdToken     tkEventProp,            // [IN] event/property token.   
        DWORD       *pdwSemanticsFlags);      // [OUT] the role flags for the method/propevent pair 

    STDMETHOD(GetClassLayout) ( 
        mdTypeDef   td,                     // [IN] give typedef    
        DWORD       *pdwPackSize,           // [OUT] 1, 2, 4, 8, or 16  
        COR_FIELD_OFFSET rFieldOffset[],    // [OUT] field offset array 
        ULONG       cMax,                   // [IN] size of the array   
        ULONG       *pcFieldOffset,         // [OUT] needed array size  
        ULONG       *pulClassSize);             // [OUT] the size of the class  

    STDMETHOD(GetFieldMarshal) (    
        mdToken     tk,                     // [IN] given a field's memberdef   
        PCCOR_SIGNATURE *ppvNativeType,     // [OUT] native type of this field  
        ULONG       *pcbNativeType);        // [OUT] the count of bytes of *ppvNativeType   

    STDMETHOD(GetRVA)(                      // S_OK or error.   
        mdToken     tk,                     // Member for which to set offset   
        ULONG       *pulCodeRVA,            // The offset   
        DWORD       *pdwImplFlags);         // the implementation flags 

    STDMETHOD(GetPermissionSetProps) (  
        mdPermission pm,                    // [IN] the permission token.   
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.   
        void const  **ppvPermission,        // [OUT] permission blob.   
        ULONG       *pcbPermission);        // [OUT] count of bytes of pvPermission.    

    STDMETHOD(GetSigFromToken)(             // S_OK or error.   
        mdSignature mdSig,                  // [IN] Signature token.    
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.   
        ULONG       *pcbSig);               // [OUT] return size of signature.  

    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,        // [IN] moduleref token.
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR      wszName,    // [OUT] buffer to fill with the moduleref name.
        ULONG       cchName,    // [IN] size of szName in wide characters.
        ULONG *      pchName);  // [OUT] actual count of characters in the name.

    STDMETHOD(EnumModuleRefs)(              // S_OK or error.   
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.    
        mdModuleRef rModuleRefs[],          // [OUT] put modulerefs here.   
        ULONG       cmax,                   // [IN] max memberrefs to put.  
        ULONG       *pcModuleRefs);         // [OUT] put # put here.    

    STDMETHOD(GetTypeSpecFromToken)(        // S_OK or error.   
        mdTypeSpec typespec,                // [IN] TypeSpec token.    
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to TypeSpec signature  
        ULONG       *pcbSig);               // [OUT] return size of signature.  

    STDMETHOD(GetNameFromToken)(            // <TODO>Not Recommended! May be removed!</TODO>
        mdToken     tk,                     // [IN] Token to get name from.  Must have a name.
        MDUTF8CSTR  *pszUtf8NamePtr);       // [OUT] Return pointer to UTF8 name in heap.

    STDMETHOD(EnumUnresolvedMethods)(       // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdToken     rMethods[],             // [OUT] Put MemberDefs here.   
        ULONG       cMax,                   // [IN] Max MemberDefs to put.  
        ULONG       *pcTokens);             // [OUT] Put # put here.    

    STDMETHOD(GetUserString)(
        mdString stk,           // [IN] String token.
      __out_ecount_part_opt(cchString, *pchString)
        LPWSTR   wszString,     // [OUT] Copy of string.
        ULONG    cchString,     // [IN] Max chars of room in szString.
        ULONG *  pchString);    // [OUT] How many chars in actual string.

    STDMETHOD(GetPinvokeMap)(
        mdToken       tk,               // [IN] FieldDef or MethodDef.
        DWORD *       pdwMappingFlags,  // [OUT] Flags used for mapping.
      __out_ecount_part_opt(cchImportName, *pchImportName)
        LPWSTR        wszImportName,    // [OUT] Import name.
        ULONG         cchImportName,    // [IN] Size of the name buffer.
        ULONG *       pchImportName,    // [OUT] Actual number of characters stored.
        mdModuleRef * pmrImportDLL);    // [OUT] ModuleRef token for the target DLL.

    STDMETHOD(EnumSignatures)(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.    
        mdSignature rSignatures[],          // [OUT] put signatures here.   
        ULONG       cmax,                   // [IN] max signatures to put.  
        ULONG       *pcSignatures);         // [OUT] put # put here.

    STDMETHOD(EnumTypeSpecs)(               // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.    
        mdTypeSpec  rTypeSpecs[],           // [OUT] put TypeSpecs here.   
        ULONG       cmax,                   // [IN] max TypeSpecs to put.  
        ULONG       *pcTypeSpecs);          // [OUT] put # put here.

    STDMETHOD(EnumUserStrings)(             // S_OK or error.
        HCORENUM    *phEnum,                // [IN/OUT] pointer to the enum.
        mdString    rStrings[],             // [OUT] put Strings here.
        ULONG       cmax,                   // [IN] max Strings to put.
        ULONG       *pcStrings);            // [OUT] put # put here.

    STDMETHOD(GetParamForMethodIndex)(      // S_OK or error.
        mdMethodDef md,                     // [IN] Method token.
        ULONG       ulParamSeq,             // [IN] Parameter sequence.
        mdParamDef  *ppd);                  // [IN] Put Param token here.

    STDMETHOD(EnumCustomAttributes)(        // S_OK or error.
        HCORENUM    *phEnum,                // [IN, OUT] COR enumerator.
        mdToken     tk,                     // [IN] Token to scope the enumeration, 0 for all.
        mdToken     tkType,                 // [IN] Type of interest, 0 for all.
        mdCustomAttribute rCustomAttributes[], // [OUT] Put custom attribute tokens here.
        ULONG       cMax,                   // [IN] Size of rCustomAttributes.
        ULONG       *pcCustomAttributes);       // [OUT, OPTIONAL] Put count of token values here.

    STDMETHOD(GetCustomAttributeProps)(     // S_OK or error.
        mdCustomAttribute cv,               // [IN] CustomAttribute token.
        mdToken     *ptkObj,                // [OUT, OPTIONAL] Put object token here.
        mdToken     *ptkType,               // [OUT, OPTIONAL] Put AttrType token here.
        void const  **ppBlob,               // [OUT, OPTIONAL] Put pointer to data here.
        ULONG       *pcbSize);              // [OUT, OPTIONAL] Put size of date here.

    STDMETHOD(FindTypeRef)(   
        mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
        LPCWSTR     szName,                 // [IN] TypeRef Name.
        mdTypeRef   *ptr);                  // [OUT] matching TypeRef.

    STDMETHOD(GetMemberProps)(  
        mdToken           mb,               // The member for which to get props.   
        mdTypeDef *       pClass,           // Put member's class here. 
      __out_ecount_part_opt(cchMember, *pchMember)
        LPWSTR            wszMember,        // Put member's name here.  
        ULONG             cchMember,        // Size of szMember buffer in wide chars.   
        ULONG *           pchMember,        // Put actual size here 
        DWORD *           pdwAttr,          // Put flags here.  
        PCCOR_SIGNATURE * ppvSigBlob,       // [OUT] point to the blob value of meta data   
        ULONG *           pcbSigBlob,       // [OUT] actual size of signature blob  
        ULONG *           pulCodeRVA,       // [OUT] codeRVA    
        DWORD *           pdwImplFlags,     // [OUT] Impl. Flags    
        DWORD *           pdwCPlusTypeFlag, // [OUT] flag for value type. selected ELEMENT_TYPE_*   
        UVCP_CONSTANT *   ppValue,          // [OUT] constant value 
        ULONG *           pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetFieldProps)(  
        mdFieldDef  mb,                     // The field for which to get props.
        mdTypeDef * pClass,                 // Put field's class here.
      __out_ecount_part_opt(cchField, *pchField)
        LPWSTR            szField,          // Put field's name here.
        ULONG             cchField,         // Size of szField buffer in wide chars.
        ULONG *           pchField,         // Put actual size here.
        DWORD *           pdwAttr,          // Put flags here.
        PCCOR_SIGNATURE * ppvSigBlob,       // [OUT] point to the blob value of meta data.
        ULONG *           pcbSigBlob,       // [OUT] actual size of signature blob.
        DWORD *           pdwCPlusTypeFlag, // [OUT] flag for value type. selected ELEMENT_TYPE_*.
        UVCP_CONSTANT *   ppValue,          // [OUT] constant value.
        ULONG *           pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetPropertyProps)(            // S_OK, S_FALSE, or error. 
        mdProperty  prop,                   // [IN] property token  
        mdTypeDef   *pClass,                // [OUT] typedef containing the property declarion. 
        LPCWSTR     szProperty,             // [OUT] Property name  
        ULONG       cchProperty,            // [IN] the count of wchar of szProperty    
        ULONG       *pchProperty,           // [OUT] actual count of wchar for property name    
        DWORD       *pdwPropFlags,          // [OUT] property flags.    
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob 
        ULONG       *pbSig,                 // [OUT] count of bytes in *ppvSig  
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*   
        UVCP_CONSTANT *ppDefaultValue,      // [OUT] constant value 
        ULONG       *pcchDefaultValue,      // [OUT] size of constant string in chars, 0 for non-strings.
        mdMethodDef *pmdSetter,             // [OUT] setter method of the property  
        mdMethodDef *pmdGetter,             // [OUT] getter method of the property  
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the property   
        ULONG       cMax,                   // [IN] size of rmdOtherMethod  
        ULONG       *pcOtherMethod);        // [OUT] total number of other method of this property  

    STDMETHOD(GetParamProps)(
        mdParamDef      tk,                 // [IN]The Parameter.
        mdMethodDef *   pmd,                // [OUT] Parent Method token.
        ULONG *         pulSequence,        // [OUT] Parameter sequence.
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR          wszName,            // [OUT] Put name here.
        ULONG           cchName,            // [OUT] Size of name buffer.
        ULONG *         pchName,            // [OUT] Put actual size of name here.
        DWORD *         pdwAttr,            // [OUT] Put flags here.
        DWORD *         pdwCPlusTypeFlag,   // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
        UVCP_CONSTANT * ppValue,            // [OUT] Constant value.
        ULONG *         pcchValue);         // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetCustomAttributeByName)(    // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCWSTR     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData);              // [OUT] Put size of data here.

    STDMETHOD_(BOOL, IsValidToken)(         // True or False.
        mdToken     tk);                    // [IN] Given token.

    STDMETHOD(GetNestedClassProps)(         // S_OK or error.
        mdTypeDef   tdNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptdEnclosingClass);      // [OUT] EnclosingClass token.

    STDMETHOD(GetNativeCallConvFromSig)(    // S_OK or error.
        void const  *pvSig,                 // [IN] Pointer to signature.
        ULONG       cbSig,                  // [IN] Count of signature bytes.
        ULONG       *pCallConv);            // [OUT] Put calling conv here (see CorPinvokemap).                                                                                        

    STDMETHOD(IsGlobal)(                    // S_OK or error.
        mdToken     pd,                     // [IN] Type, Field, or Method token.
        int         *pbGlobal);             // [OUT] Put 1 if global, 0 otherwise.

//*****************************************************************************
// IMetaDataImport2 methods
//*****************************************************************************
    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec mi,           // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data   
        ULONG       *pcbSigBlob);           // [OUT] actual size of signature blob 

    STDMETHOD(GetGenericParamProps)(
        mdGenericParam gp,              // [IN] GenericParam
        ULONG *        pulParamSeq,     // [OUT] Index of the type parameter
        DWORD *        pdwParamFlags,   // [OUT] Flags, for future use (e.g. variance)
        mdToken *      ptOwner,         // [OUT] Owner (TypeDef or MethodDef)
        DWORD *        pdwReserved,     // [OUT] For future use (e.g. non-type parameters)
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR         wszName,         // [OUT] Put name here
        ULONG          cchName,         // [IN] Size of buffer
        ULONG *        pchName);        // [OUT] Put size of name (wide chars) here.

    STDMETHOD(GetGenericParamConstraintProps)( // S_OK or error.
        mdGenericParamConstraint gpc,       // [IN] GenericParamConstraint
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType);   // [OUT] TypeDef/Ref/Spec constraint

    STDMETHOD(EnumGenericParams)(           // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdToken      tk,                    // [IN] TypeDef or MethodDef whose generic parameters are requested
        mdGenericParam rGenericParams[],    // [OUT] Put GenericParams here.   
        ULONG       cMax,                   // [IN] Max GenericParams to put.  
        ULONG       *pcGenericParams);      // [OUT] Put # put here.    

    STDMETHOD(EnumGenericParamConstraints)( // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdGenericParam tk,                  // [IN] GenericParam whose constraints are requested
        mdGenericParamConstraint rGenericParamConstraints[],    // [OUT] Put GenericParamConstraints here.   
        ULONG       cMax,                   // [IN] Max GenericParamConstraints to put.  
        ULONG       *pcGenericParamConstraints); // [OUT] Put # put here.
    
    STDMETHOD(EnumMethodSpecs)(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.    
        mdToken      tk,                    // [IN] MethodDef or MemberRef whose MethodSpecs are requested
        mdMethodSpec rMethodSpecs[],        // [OUT] Put MethodSpecs here.   
        ULONG       cMax,                   // [IN] Max tokens to put.  
        ULONG       *pcMethodSpecs);        // [OUT] Put actual count here.    

    STDMETHOD(GetPEKind)(            // S_OK or error.
        DWORD* pdwPEKind,            // [OUT] The kind of PE (0 - not a PE)
        DWORD* pdwMachine);          // [OUT] Machine as defined in NT header

    STDMETHOD(GetVersionString)(
      __out_ecount_part_opt(ccBufSize, *pccBufSize)
        LPWSTR  pwzBuf,         // Put version string here.
        DWORD   ccBufSize,      // [in] Size of the buffer, in wide chars.
        DWORD * pccBufSize);    // [out] Size of the version string, wide chars, including terminating nul.
    

    // *** ISNAssemblySignature methods ***
    
    STDMETHOD(GetSNAssemblySignature)(  // S_OK or error.
        BYTE        *pbSig,                 // [IN, OUT] Buffer to write signature
        DWORD       *pcbSig);               // [IN, OUT] Size of buffer, bytes written


#ifdef FEATURE_PREJIT
    // *** IGetIMDInternalImport methods ***
    
    STDMETHOD(GetIMDInternalImport) (
        IMDInternalImport ** ppIMDInternalImport); 

    // *** INativeImageInstallInfo ***

    STDMETHOD (GetSignature) (
        CORCOMPILE_NGEN_SIGNATURE * pNgenSign
        );

    STDMETHOD (GetVersionInfo) (
        CORCOMPILE_VERSION_INFO * pVersionInfo
        );
            

    STDMETHOD (GetILSignature) (
        CORCOMPILE_ASSEMBLY_SIGNATURE * pILSign
        );

    STDMETHOD (GetConfigMask) (
        DWORD * pConfigMask
        );

    STDMETHOD (EnumDependencies) (
        HCORENUM * phEnum,
        INativeImageDependency *rDeps[],
        ULONG cMax,
        DWORD * pdwCount
        );

    STDMETHOD (GetDependency) (
        const CORCOMPILE_NGEN_SIGNATURE *pcngenSign,
        CORCOMPILE_DEPENDENCY           *pDep   
        );
    

#endif  // FEATURE_PREJIT
    
    //------------ setters for privates -----------
    void SetHandle(HCORMODULE hHandle)
    {
        RuntimeAddRefHandle(hHandle);
        m_pHandle = hHandle;
    }

    void SetPEKind(DWORD dwPEKind)
    {
        m_dwPEKind = dwPEKind;
    }

    void SetMachine(DWORD dwMachine)
    {
        m_dwMachine = dwMachine;
    }

    void SetVersionString(const char* szVersionString)
    {
        m_szVersionString = szVersionString;
    }

    void SetBase(LPVOID base)
    {
        m_pBase = base;
    }

#ifdef FEATURE_PREJIT
    void SetZapVersionInfo(CORCOMPILE_VERSION_INFO * info, CORCOMPILE_DEPENDENCY * pDeps, COUNT_T cDeps)
    {
        m_pZapVersionInfo = info;
        m_pZapDependencies = pDeps;
        m_cZapDependencies = cDeps;
    }
#endif // FEATURE_PREJIT
    
private:
    LONG                                    m_cRef;
    HCORMODULE                              m_pHandle;              // Handle to a cached PE image
    LPVOID                                  m_pBase;                // File mapping (if runtime is not inited)
#ifdef FEATURE_PREJIT
    struct CORCOMPILE_VERSION_INFO        * m_pZapVersionInfo;      // Zap image information
    struct CORCOMPILE_DEPENDENCY          * m_pZapDependencies;     // Zap Dependancies directory
    COUNT_T                                 m_cZapDependencies;
#endif  // FEATURE_PREJIT
    IMDInternalImport                     * m_pMDInternalImport;
    DWORD                                   m_dwPEKind;
    DWORD                                   m_dwMachine;
    const char                            * m_szVersionString;
#ifdef _DEBUG
    IMetaDataAssemblyImport               * m_pDebugMDImport;
#endif //_DEBUG
};

#endif // FEATURE_FUSION

#endif // __AssemblyMDInternalDispenser__h__
