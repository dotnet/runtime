// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// ImportHelper.h
//

//
// contains utility code to MD directory
//
//*****************************************************************************
#ifndef __IMPORTHELPER__h__
#define __IMPORTHELPER__h__

class CMiniMdRW;
class MDTOKENMAP;

//*********************************************************************
// Class to handle merge
//*********************************************************************
class ImportHelper
{
public:
    // Options for code:FindMemberRef.
    enum HashSearchOption
    {
        DoNotCreateHash,    // Do not create hash if it does not exist (faster for isolated calls)
        CreateHash          // Create hash if it does not exist (faster for multiple calls)
    };


    static HRESULT FindMethodSpecByMethodAndInstantiation(
        CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
        /*mdMethodDefOrRef*/ mdToken tkMethod,  // [IN] MethodSpec method field
        PCCOR_SIGNATURE pInstantiation,         // [IN] MethodSpec instantiation (a signature)
        ULONG       cbInstantiation,            // [IN] Size of instantiation.
        mdMethodSpec *pMethodSpec,              // [OUT] Put the MethodSpec token here.
        RID         rid = 0);              // [IN] Optional rid to be ignored.


    static HRESULT FindGenericParamConstraintByOwnerAndConstraint(
        CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
        mdGenericParam tkOwner,                 // [IN] GenericParamConstraint Owner
        mdToken tkConstraint,                   // [IN] GenericParamConstraint Constraint
        mdGenericParamConstraint *pGenericParamConstraint, // [OUT] Put the GenericParamConstraint token here.
        RID         rid = 0);              // [IN] Optional rid to be ignored.


    static HRESULT FindGenericParamByOwner(
        CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
        mdToken     tkOwner,                    // [IN] GenericParam Owner
        LPCUTF8     szUTF8Name,                 // [IN] GeneriParam Name, may be NULL if not used for search
        ULONG       *pNumber,                   // [IN] GeneriParam Number, may be NULL if not used for search
        mdGenericParam *pGenericParam,          // [OUT] Put the GenericParam token here.
        RID         rid = 0);                   // [IN] Optional rid to be ignored.

    static HRESULT FindMethod(
        CMiniMdRW *     pMiniMd,                    // [IN] the minimd to lookup
        mdTypeDef       td,                         // [IN] parent.
        LPCUTF8         szName,                     // [IN] MethodDef name.
        PCCOR_SIGNATURE pSig,                       // [IN] Signature.
        ULONG           cbSig,                      // [IN] Size of signature.
        mdMethodDef *   pmb,                        // [OUT] Put the MethodDef token here.
        RID             rid               = 0,      // [IN] Optional rid to be ignored.
        PSIGCOMPARE     pSignatureCompare = NULL,   // [IN] Optional Routine to compare signatures
        void *          pCompareContext   = NULL);  // [IN] Optional context for the compare function

    static HRESULT FindField(
        CMiniMdRW *     pMiniMd,    // [IN] the minimd to lookup
        mdTypeDef       td,         // [IN] parent.
        LPCUTF8         szName,     // [IN] FieldDef name.
        PCCOR_SIGNATURE pSig,       // [IN] Signature.
        ULONG           cbSig,      // [IN] Size of signature.
        mdFieldDef *    pfd,        // [OUT] Put the FieldDef token here.
        RID             rid = 0);   // [IN] Optional rid to be ignored.

    static HRESULT FindMember(
        CMiniMdRW *     pMiniMd,    // [IN] the minimd to lookup
        mdTypeDef       td,         // [IN] parent.
        LPCUTF8         szName,     // [IN] Member name.
        PCCOR_SIGNATURE pSig,       // [IN] Signature.
        ULONG           cbSig,      // [IN] Size of signature.
        mdToken *       ptk);       // [OUT] Put the token here.

    static HRESULT FindMemberRef(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkParent,               // [IN] the parent token
        LPCUTF8     szName,                 // [IN] memberref name
        const COR_SIGNATURE *pSig,          // [IN] Signature.
        ULONG       cbSig,                  // [IN] Size of signature.
        mdMemberRef *pmr,                   // [OUT] Put the MemberRef token found
        RID         rid = 0,                // [IN] Optional rid to be ignored.
        HashSearchOption fCreateHash = DoNotCreateHash); // [IN] Should we create hash first? (Optimize for multiple calls vs. single isolated call)

    static HRESULT FindStandAloneSig(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        const COR_SIGNATURE *pbSig,         // [IN] Signature.
        ULONG       cbSig,                  // [IN] Size of signature.
        mdSignature *psa);                  // [OUT] Put the StandAloneSig token found

    static HRESULT FindTypeSpec(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        const COR_SIGNATURE *pbSig,         // [IN] Signature.
        ULONG       cbSig,                  // [IN] Size of signature.
        mdTypeSpec  *ptypespec);            // [OUT] Put the TypeSpec token found

    static HRESULT FindMethodImpl(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdTypeDef   tkClass,                // [IN] The parent TypeDef token.
        mdToken     tkBody,                 // [IN] Method body token.
        mdToken     tkDecl,                 // [IN] Method declaration token.
        RID         *pRid);                 // [OUT] Put the MethodImpl rid here

    static HRESULT FindCustomAttributeCtorByName(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        LPCUTF8     szAssemblyName,         // [IN] Assembly Name.
        LPCUTF8     szNamespace,            // [IN] TypeRef Namespace.
        LPCUTF8     szName,                 // [IN] TypeRef Name.
        mdTypeDef   *ptk,                   // [OUT] Put the TypeRef token here.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT FindTypeRefByName(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkResolutionScope,      // [IN] ResolutionScope, mdAssemblyRef or mdModuleRef.
        LPCUTF8     szNamespace,            // [IN] TypeRef Namespace.
        LPCUTF8     szName,                 // [IN] TypeRef Name.
        mdTypeDef   *ptk,                   // [OUT] Put the TypeRef token here.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT FindModuleRef(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        LPCUTF8     szUTF8Name,             // [IN] ModuleRef name.
        mdModuleRef *pmur,                  // [OUT] Put the ModuleRef token here.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT FindTypeDefByName(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        LPCUTF8     szNamespace,            // [IN] Namespace of the TypeDef.
        LPCUTF8     szName,                 // [IN] Name of the TypeDef.
        mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef enclosing class.
        mdTypeDef   *ptk,                   // [OUT] Put the TypeDef token here.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT FindInterfaceImpl(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkClass,                // [IN] TypeDef of the type
        mdToken     tkInterface,            // [IN] could be typedef/typeref
        mdInterfaceImpl *ptk,               // [OUT] Put the interface token here.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT FindPermission(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkParent,               // [IN] Token with the Permission
        USHORT      usAction,               // [IN] The action of the permission
        mdPermission *ppm);                 // [OUT] Put permission token here

    static HRESULT FindProperty(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkTypeDef,              // [IN] typedef token
        LPCUTF8     szName,                 // [IN] name of the property
        const COR_SIGNATURE *pbSig,         // [IN] Signature.
        ULONG       cbSig,                  // [IN] Size of signature.
        mdProperty  *ppr);                  // [OUT] Property token

    static HRESULT FindEvent(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkTypeDef,              // [IN] typedef token
        LPCUTF8     szName,                 // [IN] name of the event
        mdProperty  *pev);                  // [OUT] Event token

    static HRESULT FindCustomAttributeByToken(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkParent,               // [IN] the parent that custom value is associated with
        mdToken     tkType,                 // [IN] type of the CustomAttribute
        const void  *pCustBlob,             // [IN] custom value blob
        ULONG       cbCustBlob,             // [IN] size of the blob.
        mdCustomAttribute *pcv);            // [OUT] CustomAttribute token

    static HRESULT GetCustomAttributeByName(// S_OK or error.
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData);              // [OUT] Put size of data here.

    static HRESULT GetCustomAttributeByName(// S_OK or error.
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        mdCustomAttribute pca);             // [OUT] found CA token

    static HRESULT MergeUpdateTokenInFieldSig(
        CMiniMdRW   *pMiniMdAssemEmit,      // [IN] The assembly emit scope.
        CMiniMdRW   *pMiniMdEmit,           // [IN] The emit scope.
        IMetaModelCommon *pCommonAssemImport,   // [IN] Assembly scope where the signature is from.
        const void  *pbHashValue,           // [IN] Hash value for the import assembly.
        ULONG       cbHashValue,            // [IN] Size in bytes for the hash value.
        IMetaModelCommon *pCommonImport,    // [IN] The scope to merge into the emit scope.
        PCCOR_SIGNATURE pbSigImp,           // [IN] signature from the imported scope
        MDTOKENMAP  *ptkMap,                // [IN] Internal OID mapping structure.
        CQuickBytes *pqkSigEmit,            // [OUT] buffer for translated signature
        ULONG       cbStartEmit,            // [IN] start point of buffer to write to
        ULONG       *pcbImp,                // [OUT] total number of bytes consumed from pbSigImp
        ULONG       *pcbEmit);              // [OUT] total number of bytes write to pqkSigEmit

    static HRESULT MergeUpdateTokenInSig(   // S_OK or error.
        CMiniMdRW   *pMiniMdAssemEmit,      // [IN] The assembly emit scope.
        CMiniMdRW   *pMiniMdEmit,           // [IN] The emit scope.
        IMetaModelCommon *pCommonAssemImport,   // [IN] Assembly scope where the signature is from.
        const void  *pbHashValue,           // [IN] Hash value for the import assembly.
        ULONG       cbHashValue,            // [IN] Size in bytes for the hash value.
        IMetaModelCommon *pCommonImport,    // [IN] The scope to merge into the emit scope.
        PCCOR_SIGNATURE pbSigImp,           // [IN] signature from the imported scope
        MDTOKENMAP  *ptkMap,                // [IN] Internal OID mapping structure.
        CQuickBytes *pqkSigEmit,            // [OUT] translated signature
        ULONG       cbStartEmit,            // [IN] start point of buffer to write to
        ULONG       *pcbImp,                // [OUT] total number of bytes consumed from pbSigImp
        ULONG       *pcbEmit);              // [OUT] total number of bytes write to pqkSigEmit

    // This is implemented in a satellite lib because it is only used in emit and depends on
    // strong name support in mscorwks.dll.
    static HRESULT FindAssemblyRef(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
        LPCUTF8     szName,                 // [IN] Name.
        LPCUTF8     szLocale,               // [IN] Locale.
        const void  *pbPublicKeyOrToken,    // [IN] Public key or token (based on flags).
        ULONG       cbPublicKeyOrToken,     // [IN] Byte count of public key or token.
        USHORT      usMajorVersion,         // [IN] Major version.
        USHORT      usMinorVersion,         // [IN] Minor version.
        USHORT      usBuildNumber,          // [IN] Build number.
        USHORT      usRevisionNumber,       // [IN] Revision number.
        DWORD       dwFlags,                // [IN] Flags.
        mdAssemblyRef *pmar);               // [OUT] returned AssemblyRef token.

    static HRESULT FindFile(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
        LPCUTF8     szName,                 // [IN] name for the File.
        mdFile      *pmf,                   // [OUT] returned File token.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT FindExportedType(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
        LPCUTF8     szNamespace,            // [IN] namespace for the ExportedType.
        LPCUTF8     szName,                 // [IN] name for the ExportedType.
        mdExportedType   tkEnclosingType,   // [IN] enclosing ExportedType token.
        mdExportedType   *pmct,             // [OUT] returned ExportedType token.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT FindManifestResource(
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
        LPCUTF8     szName,                 // [IN] name for the ManifestResource.
        mdManifestResource *pmmr,           // [OUT] returned ManifestResource token.
        RID         rid = 0);               // [IN] Optional rid to be ignored.

    static HRESULT GetNesterHierarchy(
        IMetaModelCommon *pCommon,          // Scope in which to find the hierarchy.
        mdTypeDef   td,                     // TypeDef whose hierarchy is needed.
        CQuickArray<mdTypeDef> &cqaTdNesters,  // Array of Nesters.
        CQuickArray<LPCUTF8> &cqaNamespaces,    // Namespaces of the nesters.
        CQuickArray<LPCUTF8> &cqaNames);    // Names of the nesters.

    static HRESULT FindNestedTypeRef(
        CMiniMdRW   *pMiniMd,               // [IN] Scope in which to find the TypeRef.
        CQuickArray<LPCUTF8> &cqaNesterNamespaces,   // [IN] Array of Namespaces.
        CQuickArray<LPCUTF8> &cqaNesterNames,    // [IN] Array of Names.
        mdToken     tkResolutionScope,      // [IN] Resolution scope for the outermost TypeRef.
        mdTypeRef   *ptr);                  // [OUT] Inner most TypeRef token.

    static HRESULT FindNestedTypeDef(
        CMiniMdRW   *pMiniMd,               // [IN] Scope in which to find the TypeRef.
        CQuickArray<LPCUTF8> &cqaNesterNamespaces,   // [IN] Array of Namespaces.
        CQuickArray<LPCUTF8> &cqaNesterNames,    // [IN] Array of Names.
        mdTypeDef   tdNester,               // [IN] Enclosing class for the Outermost TypeDef.
        mdTypeDef   *ptd);                  // [OUT] Inner most TypeRef token.

    static HRESULT CreateNesterHierarchy(
        CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope to create the Nesters in.
        CQuickArray<LPCUTF8> &cqaNesterNamespaces,    // [IN] Array of Nester namespaces.
        CQuickArray<LPCUTF8> &cqaNesterNames,   // [IN] Array of Nester names.
        mdToken     tkResolutionScope,      // [IN] ResolutionScope for the innermost TypeRef.
        mdTypeRef   *ptr);                  // [OUT] Token for the innermost TypeRef.

    static HRESULT ImportTypeDef(
        CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Assembly emit scope.
        CMiniMdRW   *pMiniMdEmit,           // [IN] Module emit scope.
        IMetaModelCommon *pCommonAssemImport, // [IN] Assembly import scope.
        const void  *pbHashValue,           // [IN] Hash value for import assembly.
        ULONG       cbHashValue,            // [IN] Size in bytes of hash value.
        IMetaModelCommon *pCommonImport,    // [IN] Module import scope.
        mdTypeDef   tdImport,               // [IN] Imported TypeDef.
        bool        bReturnTd,              // [IN] If the import and emit scopes are identical, return the TypeDef.
        mdToken     *ptkType);              // [OUT] Output token for the imported type in the emit scope.

    static HRESULT ImportTypeRef(
        CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Assembly emit scope.
        CMiniMdRW   *pMiniMdEmit,           // [IN] Module emit scope.
        IMetaModelCommon *pCommonAssemImport, // [IN] Assembly import scope.
        const void  *pbHashValue,           // [IN] Hash value for import assembly.
        ULONG       cbHashValue,            // [IN] Size in bytes of hash value.
        IMetaModelCommon *pCommonImport,    // [IN] Module import scope.
        mdTypeRef   trImport,               // [IN] Imported TypeRef.
        mdToken     *ptkType);              // [OUT] Output token for the imported type in the emit scope.

private:
    /*
    static bool ImportHelper::CompareCustomAttribute( //
        CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        ULONG       rid);                   // [IN] the rid of the custom attribute to compare to
    */

    static HRESULT GetTDNesterHierarchy(
        IMetaModelCommon *pCommon,          // Scope in which to find the hierarchy.
        mdTypeDef     td,                   // TypeDef whose hierarchy is needed.
        CQuickArray<mdTypeDef> &cqaTdNesters,// Array of Nesters.
        CQuickArray<LPCUTF8> &cqaNamespaces,    // Namespaces of the nesters.
        CQuickArray<LPCUTF8> &cqaNames);    // Names of the nesters.

    static HRESULT GetTRNesterHierarchy(
        IMetaModelCommon *pCommon,          // Scope in which to find the hierarchy.
        mdTypeRef     tr,                   // TypeRef whose hierarchy is needed.
        CQuickArray<mdTypeRef> &cqaTrNesters,// Array of Nesters.
        CQuickArray<LPCUTF8> &cqaNamespaces,    // Namespaces of the nesters.
        CQuickArray<LPCUTF8> &cqaNames);    // Names of the nesters.

    static HRESULT CreateModuleRefFromScope(
        CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope in which the ModuleRef is to be created.
        IMetaModelCommon *pCommonImport,    // [IN] Import scope.
        mdModuleRef *ptkModuleRef);         // [OUT] Output token for ModuleRef.

    static HRESULT CreateModuleRefFromModuleRef(    // S_OK or error.
        CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope.
        IMetaModelCommon *pCommon,          // [IN] Import scope.
        mdModuleRef tkModuleRef,            // [IN] ModuleRef token.
        mdModuleRef *ptkModuleRef);         // [OUT] ModuleRef token in the emit scope.

    static HRESULT CreateModuleRefFromExportedType(  // S_OK, S_FALSE or error.
        CMiniMdRW   *pAssemEmit,            // [IN] Import assembly scope.
        CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope.
        mdExportedType   tkExportedType,    // [IN] ExportedType token in Assembly emit scope.
        mdModuleRef *ptkModuleRef);         // [OUT] ModuleRef token in the emit scope.

    // CreateAssemblyRefFromAssembly, CompareAssemblyRefToAssembly are in satellite libs because
    // they are only used in emit cases and need strong-name support in mscorwks.dll.

    static HRESULT CreateAssemblyRefFromAssembly( // S_OK or error.
        CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Emit assembly scope.
        CMiniMdRW   *pMiniMdModuleEmit,     // [IN] Emit module scope.
        IMetaModelCommon *pCommonAssemImport, // [IN] Assembly import scope.
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        mdAssemblyRef *ptkAssemblyRef);     // [OUT] AssemblyRef token.

    static HRESULT CompareAssemblyRefToAssembly(    // S_OK, S_FALSE or error.
        IMetaModelCommon *pCommonAssem1,    // [IN] Assembly that defines the AssemblyRef.
        mdAssemblyRef tkAssemRef,           // [IN] AssemblyRef.
        IMetaModelCommon *pCommonAssem2);   // [IN] Assembly against which the Ref is compared.

    static HRESULT CreateAssemblyRefFromAssemblyRef(
        CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Assembly emit scope.
        CMiniMdRW   *pMiniMdModuleEmit,     // [IN] Module emit scope
        IMetaModelCommon *pCommonImport,    // [IN] Scope to import the assembly ref from.
        mdAssemblyRef tkAssemRef,           // [IN] Assembly ref to be imported.
        mdAssemblyRef *ptkAssemblyRef);     // [OUT] AssemblyRef in the emit scope.
};

#endif // __IMPORTHELPER__h__
