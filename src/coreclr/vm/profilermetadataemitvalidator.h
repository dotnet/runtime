// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef _PROFILER_METADATA_EMIT_VALIDATOR_H_
#define _PROFILER_METADATA_EMIT_VALIDATOR_H_

// This is a wrapper over IMetaDataEmit interfaces to prevent profilers from making changes the runtime can't support
// Annoyingly, it is legal to QI IMetaDataImport from the emitter and vice-versa, so the wrapper also proxies those interfaces

class ProfilerMetadataEmitValidator : public IMetaDataEmit2, public IMetaDataAssemblyEmit, public IMetaDataImport2, public IMetaDataAssemblyImport
{
public:
    ProfilerMetadataEmitValidator(IMetaDataEmit* pInnerEmit);
    virtual ~ProfilerMetadataEmitValidator();

    //IUnknown
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppInterface);
    virtual ULONG   STDMETHODCALLTYPE AddRef();
    virtual ULONG   STDMETHODCALLTYPE Release();

    //IMetaDataEmit
    virtual HRESULT STDMETHODCALLTYPE SetModuleProps(
        LPCWSTR     szName);

    virtual HRESULT STDMETHODCALLTYPE Save(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags);

    virtual HRESULT STDMETHODCALLTYPE SaveToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags);

    virtual HRESULT STDMETHODCALLTYPE GetSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize);

    virtual HRESULT STDMETHODCALLTYPE DefineTypeDef(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   *ptd);

    virtual HRESULT STDMETHODCALLTYPE DefineNestedType(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   tdEncloser,
        mdTypeDef   *ptd);

    virtual HRESULT STDMETHODCALLTYPE SetHandler(
        IUnknown    *pUnk);

    virtual HRESULT STDMETHODCALLTYPE DefineMethod(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwMethodFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags,
        mdMethodDef *pmd);

    virtual HRESULT STDMETHODCALLTYPE DefineMethodImpl(
        mdTypeDef   td,
        mdToken     tkBody,
        mdToken     tkDecl);

    virtual HRESULT STDMETHODCALLTYPE DefineTypeRefByName(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr);

    virtual HRESULT STDMETHODCALLTYPE DefineImportType(
        IMetaDataAssemblyImport *pAssemImport,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdTypeDef   tdImport,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdTypeRef   *ptr);

    virtual HRESULT STDMETHODCALLTYPE DefineMemberRef(
        mdToken     tkImport,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr);

    virtual HRESULT STDMETHODCALLTYPE DefineImportMember(
        IMetaDataAssemblyImport *pAssemImport,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdToken     mbMember,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdToken     tkParent,
        mdMemberRef *pmr);

    virtual HRESULT STDMETHODCALLTYPE DefineEvent(
        mdTypeDef   td,
        LPCWSTR     szEvent,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[],
        mdEvent     *pmdEvent);

    virtual HRESULT STDMETHODCALLTYPE SetClassLayout(
        mdTypeDef   td,
        DWORD       dwPackSize,
        COR_FIELD_OFFSET rFieldOffsets[],
        ULONG       ulClassSize);

    virtual HRESULT STDMETHODCALLTYPE DeleteClassLayout(
        mdTypeDef   td);

    virtual HRESULT STDMETHODCALLTYPE SetFieldMarshal(
        mdToken     tk,
        PCCOR_SIGNATURE pvNativeType,
        ULONG       cbNativeType);

    virtual HRESULT STDMETHODCALLTYPE DeleteFieldMarshal(
        mdToken     tk);

    virtual HRESULT STDMETHODCALLTYPE DefinePermissionSet(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm);

    virtual HRESULT STDMETHODCALLTYPE SetRVA(
        mdMethodDef md,
        ULONG       ulRVA);

    virtual HRESULT STDMETHODCALLTYPE GetTokenFromSig(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdSignature *pmsig);

    virtual HRESULT STDMETHODCALLTYPE DefineModuleRef(
        LPCWSTR     szName,
        mdModuleRef *pmur);

    virtual HRESULT STDMETHODCALLTYPE SetParent(
        mdMemberRef mr,
        mdToken     tk);

    virtual HRESULT STDMETHODCALLTYPE GetTokenFromTypeSpec(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdTypeSpec *ptypespec);

    virtual HRESULT STDMETHODCALLTYPE SaveToMemory(
        void        *pbData,
        ULONG       cbData);

    virtual HRESULT STDMETHODCALLTYPE DefineUserString(
        LPCWSTR szString,
        ULONG       cchString,
        mdString    *pstk);

    virtual HRESULT STDMETHODCALLTYPE DeleteToken(
        mdToken     tkObj);

    virtual HRESULT STDMETHODCALLTYPE SetMethodProps(
        mdMethodDef md,
        DWORD       dwMethodFlags,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags);

    virtual HRESULT STDMETHODCALLTYPE SetTypeDefProps(
        mdTypeDef   td,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[]);

    virtual HRESULT STDMETHODCALLTYPE SetEventProps(
        mdEvent     ev,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[]);

    virtual HRESULT STDMETHODCALLTYPE SetPermissionSetProps(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm);

    virtual HRESULT STDMETHODCALLTYPE DefinePinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL);

    virtual HRESULT STDMETHODCALLTYPE SetPinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL);

    virtual HRESULT STDMETHODCALLTYPE DeletePinvokeMap(
        mdToken     tk);

    // New CustomAttribute functions.
    virtual HRESULT STDMETHODCALLTYPE DefineCustomAttribute(
        mdToken     tkOwner,
        mdToken     tkCtor,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute,
        mdCustomAttribute *pcv);

    virtual HRESULT STDMETHODCALLTYPE SetCustomAttributeValue(
        mdCustomAttribute pcv,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute);

    virtual HRESULT STDMETHODCALLTYPE DefineField(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwFieldFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdFieldDef  *pmd);

    virtual HRESULT STDMETHODCALLTYPE DefineProperty(
        mdTypeDef   td,
        LPCWSTR     szProperty,
        DWORD       dwPropFlags,
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[],
        mdProperty  *pmdProp);

    virtual HRESULT STDMETHODCALLTYPE DefineParam(
        mdMethodDef md,
        ULONG       ulParamSeq,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdParamDef  *ppd);

    virtual HRESULT STDMETHODCALLTYPE SetFieldProps(
        mdFieldDef  fd,
        DWORD       dwFieldFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue);

    virtual HRESULT STDMETHODCALLTYPE SetPropertyProps(
        mdProperty  pr,
        DWORD       dwPropFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[]);

    virtual HRESULT STDMETHODCALLTYPE SetParamProps(
        mdParamDef  pd,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue);

    virtual HRESULT STDMETHODCALLTYPE DefineSecurityAttributeSet(
        mdToken     tkObj,
        COR_SECATTR rSecAttrs[],
        ULONG       cSecAttrs,
        ULONG       *pulErrorAttr);

    virtual HRESULT STDMETHODCALLTYPE ApplyEditAndContinue(
        IUnknown    *pImport);

    virtual HRESULT STDMETHODCALLTYPE TranslateSigWithScope(
        IMetaDataAssemblyImport *pAssemImport,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *import,
        PCCOR_SIGNATURE pbSigBlob,
        ULONG       cbSigBlob,
        IMetaDataAssemblyEmit *pAssemEmit,
        IMetaDataEmit *emit,
        PCOR_SIGNATURE pvTranslatedSig,
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig);

    virtual HRESULT STDMETHODCALLTYPE SetMethodImplFlags(
        mdMethodDef md,
        DWORD       dwImplFlags);

    virtual HRESULT STDMETHODCALLTYPE SetFieldRVA(
        mdFieldDef  fd,
        ULONG       ulRVA);

    virtual HRESULT STDMETHODCALLTYPE Merge(
        IMetaDataImport *pImport,
        IMapToken   *pHostMapToken,
        IUnknown    *pHandler);

    virtual HRESULT STDMETHODCALLTYPE MergeEnd();

    // IMetaDataEmit2
     virtual HRESULT STDMETHODCALLTYPE DefineMethodSpec(
        mdToken     tkParent,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodSpec *pmi);

    virtual HRESULT STDMETHODCALLTYPE GetDeltaSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize);

    virtual HRESULT STDMETHODCALLTYPE SaveDelta(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags);

    virtual HRESULT STDMETHODCALLTYPE SaveDeltaToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags);

    virtual HRESULT STDMETHODCALLTYPE SaveDeltaToMemory(
        void        *pbData,
        ULONG       cbData);

    virtual HRESULT STDMETHODCALLTYPE DefineGenericParam(
        mdToken      tk,
        ULONG        ulParamSeq,
        DWORD        dwParamFlags,
        LPCWSTR      szname,
        DWORD        reserved,
        mdToken      rtkConstraints[],
        mdGenericParam *pgp);

    virtual HRESULT STDMETHODCALLTYPE SetGenericParamProps(
        mdGenericParam gp,
        DWORD        dwParamFlags,
        LPCWSTR      szName,
        DWORD        reserved,
        mdToken      rtkConstraints[]);

    virtual HRESULT STDMETHODCALLTYPE ResetENCLog();

    //IMetaDataAssemblyEmit
    virtual HRESULT STDMETHODCALLTYPE DefineAssembly(
        const void  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        DWORD       dwAssemblyFlags,
        mdAssembly  *pma);

    virtual HRESULT STDMETHODCALLTYPE DefineAssemblyRef(
        const void  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags,
        mdAssemblyRef *pmdar);

    virtual HRESULT STDMETHODCALLTYPE DefineFile(
        LPCWSTR     szName,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags,
        mdFile      *pmdf);

    virtual HRESULT STDMETHODCALLTYPE DefineExportedType(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags,
        mdExportedType   *pmdct);

    virtual HRESULT STDMETHODCALLTYPE DefineManifestResource(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags,
        mdManifestResource  *pmdmr);

    virtual HRESULT STDMETHODCALLTYPE SetAssemblyProps(
        mdAssembly  pma,
        const void  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        DWORD       dwAssemblyFlags);

    virtual HRESULT STDMETHODCALLTYPE SetAssemblyRefProps(
        mdAssemblyRef ar,
        const void  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags);

    virtual HRESULT STDMETHODCALLTYPE SetFileProps(
        mdFile      file,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags);

    virtual HRESULT STDMETHODCALLTYPE SetExportedTypeProps(
        mdExportedType   ct,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags);

    virtual HRESULT STDMETHODCALLTYPE SetManifestResourceProps(
        mdManifestResource  mr,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags);

    //IMetaDataImport
    virtual void STDMETHODCALLTYPE CloseEnum(HCORENUM hEnum);
    virtual HRESULT STDMETHODCALLTYPE CountEnum(HCORENUM hEnum, ULONG *pulCount);
    virtual HRESULT STDMETHODCALLTYPE ResetEnum(HCORENUM hEnum, ULONG ulPos);
    virtual HRESULT STDMETHODCALLTYPE EnumTypeDefs(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
        ULONG cMax, ULONG *pcTypeDefs);
    virtual HRESULT STDMETHODCALLTYPE EnumInterfaceImpls(HCORENUM *phEnum, mdTypeDef td,
        mdInterfaceImpl rImpls[], ULONG cMax,
        ULONG* pcImpls);
    virtual HRESULT STDMETHODCALLTYPE EnumTypeRefs(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
        ULONG cMax, ULONG* pcTypeRefs);

    virtual HRESULT STDMETHODCALLTYPE FindTypeDefByName(           // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of the Type.
        mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef for Enclosing class.
        mdTypeDef   *ptd);             // [OUT] Put the TypeDef token here.

    virtual HRESULT STDMETHODCALLTYPE GetScopeProps(               // S_OK or error.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Put the name here.
        ULONG       cchName,                // [IN] Size of name buffer in wide chars.
        ULONG       *pchName,               // [OUT] Put size of name (wide chars) here.
        GUID        *pmvid);           // [OUT, OPTIONAL] Put MVID here.

    virtual HRESULT STDMETHODCALLTYPE GetModuleFromScope(          // S_OK.
        mdModule    *pmd);             // [OUT] Put mdModule token here.

    virtual HRESULT STDMETHODCALLTYPE GetTypeDefProps(             // S_OK or error.
        mdTypeDef   td,                     // [IN] TypeDef token for inquiry.
        __out_ecount(cchTypeDef)
        LPWSTR      szTypeDef,              // [OUT] Put name here.
        ULONG       cchTypeDef,             // [IN] size of name buffer in wide chars.
        ULONG       *pchTypeDef,            // [OUT] put size of name (wide chars) here.
        DWORD       *pdwTypeDefFlags,       // [OUT] Put flags here.
        mdToken     *ptkExtends);      // [OUT] Put base class TypeDef/TypeRef here.

    virtual HRESULT STDMETHODCALLTYPE GetInterfaceImplProps(       // S_OK or error.
        mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
        mdTypeDef   *pClass,                // [OUT] Put implementing class token here.
        mdToken     *ptkIface);        // [OUT] Put implemented interface token here.

    virtual HRESULT STDMETHODCALLTYPE GetTypeRefProps(             // S_OK or error.
        mdTypeRef   tr,                     // [IN] TypeRef token.
        mdToken     *ptkResolutionScope,    // [OUT] Resolution scope, ModuleRef or AssemblyRef.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Name of the TypeRef.
        ULONG       cchName,                // [IN] Size of buffer.
        ULONG       *pchName);         // [OUT] Size of Name.

    virtual HRESULT STDMETHODCALLTYPE ResolveTypeRef(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd);

    virtual HRESULT STDMETHODCALLTYPE EnumMembers(                 // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumMembersWithName(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumMethods(                 // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdMethodDef rMethods[],             // [OUT] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumMethodsWithName(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdMethodDef rMethods[],             // [OU] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumFields(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdFieldDef  rFields[],              // [OUT] Put FieldDefs here.
        ULONG       cMax,                   // [IN] Max FieldDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumFieldsWithName(          // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdFieldDef  rFields[],              // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.


    virtual HRESULT STDMETHODCALLTYPE EnumParams(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration.
        mdParamDef  rParams[],              // [OUT] Put ParamDefs here.
        ULONG       cMax,                   // [IN] Max ParamDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumMemberRefs(              // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tkParent,               // [IN] Parent token to scope the enumeration.
        mdMemberRef rMemberRefs[],          // [OUT] Put MemberRefs here.
        ULONG       cMax,                   // [IN] Max MemberRefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumMethodImpls(             // S_OK, S_FALSE, or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMethodBody[],          // [OUT] Put Method Body tokens here.
        mdToken     rMethodDecl[],          // [OUT] Put Method Declaration tokens here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumPermissionSets(          // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tk,                     // [IN] if !NIL, token to scope the enumeration.
        DWORD       dwActions,              // [IN] if !0, return only these actions.
        mdPermission rPermission[],         // [OUT] Put Permissions here.
        ULONG       cMax,                   // [IN] Max Permissions to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE FindMember(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdToken     *pmb);             // [OUT] matching memberdef

    virtual HRESULT STDMETHODCALLTYPE FindMethod(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmb);             // [OUT] matching memberdef

    virtual HRESULT STDMETHODCALLTYPE FindField(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdFieldDef  *pmb);             // [OUT] matching memberdef

    virtual HRESULT STDMETHODCALLTYPE FindMemberRef(
        mdTypeRef   td,                     // [IN] given typeRef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMemberRef *pmr);             // [OUT] matching memberref

    virtual HRESULT STDMETHODCALLTYPE GetMethodProps(
        mdMethodDef mb,                     // The method for which to get props.
        mdTypeDef   *pClass,                // Put method's class here.
        __out_ecount(cchMethod)
        LPWSTR      szMethod,               // Put method's name here.
        ULONG       cchMethod,              // Size of szMethod buffer in wide chars.
        ULONG       *pchMethod,             // Put actual size here
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags);    // [OUT] Impl. Flags

    virtual HRESULT STDMETHODCALLTYPE GetMemberRefProps(           // S_OK or error.
        mdMemberRef mr,                     // [IN] given memberref
        mdToken     *ptk,                   // [OUT] Put classref or classdef here.
        __out_ecount(cchMember)
        LPWSTR      szMember,               // [OUT] buffer to fill for member's name
        ULONG       cchMember,              // [IN] the count of char of szMember
        ULONG       *pchMember,             // [OUT] actual count of char in member name
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to meta data blob value
        ULONG       *pbSig);           // [OUT] actual size of signature blob

    virtual HRESULT STDMETHODCALLTYPE EnumProperties(              // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdProperty  rProperties[],          // [OUT] Put Properties here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcProperties);    // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumEvents(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdEvent     rEvents[],              // [OUT] Put events here.
        ULONG       cMax,                   // [IN] Max events to put.
        ULONG       *pcEvents);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetEventProps(               // S_OK, S_FALSE, or error.
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
        ULONG       *pcOtherMethod);   // [OUT] total number of other method of this event

    virtual HRESULT STDMETHODCALLTYPE EnumMethodSemantics(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration.
        mdToken     rEventProp[],           // [OUT] Put Event/Property here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcEventProp);     // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetMethodSemantics(          // S_OK, S_FALSE, or error.
        mdMethodDef mb,                     // [IN] method token
        mdToken     tkEventProp,            // [IN] event/property token.
        DWORD       *pdwSemanticsFlags); // [OUT] the role flags for the method/propevent pair

    virtual HRESULT STDMETHODCALLTYPE GetClassLayout(
        mdTypeDef   td,                     // [IN] give typedef
        DWORD       *pdwPackSize,           // [OUT] 1, 2, 4, 8, or 16
        COR_FIELD_OFFSET rFieldOffset[],    // [OUT] field offset array
        ULONG       cMax,                   // [IN] size of the array
        ULONG       *pcFieldOffset,         // [OUT] needed array size
        ULONG       *pulClassSize);        // [OUT] the size of the class

    virtual HRESULT STDMETHODCALLTYPE GetFieldMarshal(
        mdToken     tk,                     // [IN] given a field's memberdef
        PCCOR_SIGNATURE *ppvNativeType,     // [OUT] native type of this field
        ULONG       *pcbNativeType);   // [OUT] the count of bytes of *ppvNativeType

    virtual HRESULT STDMETHODCALLTYPE GetRVA(                      // S_OK or error.
        mdToken     tk,                     // Member for which to set offset
        ULONG       *pulCodeRVA,            // The offset
        DWORD       *pdwImplFlags);    // the implementation flags

    virtual HRESULT STDMETHODCALLTYPE GetPermissionSetProps(
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission);   // [OUT] count of bytes of pvPermission.

    virtual HRESULT STDMETHODCALLTYPE GetSigFromToken(             // S_OK or error.
        mdSignature mdSig,                  // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig);          // [OUT] return size of signature.

    virtual HRESULT STDMETHODCALLTYPE GetModuleRefProps(           // S_OK or error.
        mdModuleRef mur,                    // [IN] moduleref token.
        __out_ecount_part_opt(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] buffer to fill with the moduleref name.
        ULONG       cchName,                // [IN] size of szName in wide characters.
        ULONG       *pchName);         // [OUT] actual count of characters in the name.

    virtual HRESULT STDMETHODCALLTYPE EnumModuleRefs(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdModuleRef rModuleRefs[],          // [OUT] put modulerefs here.
        ULONG       cmax,                   // [IN] max memberrefs to put.
        ULONG       *pcModuleRefs);    // [OUT] put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetTypeSpecFromToken(        // S_OK or error.
        mdTypeSpec typespec,                // [IN] TypeSpec token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to TypeSpec signature
        ULONG       *pcbSig);          // [OUT] return size of signature.

    virtual HRESULT STDMETHODCALLTYPE GetNameFromToken(            // Not Recommended! May be removed!
        mdToken     tk,                     // [IN] Token to get name from.  Must have a name.
        MDUTF8CSTR  *pszUtf8NamePtr);  // [OUT] Return pointer to UTF8 name in heap.

    virtual HRESULT STDMETHODCALLTYPE EnumUnresolvedMethods(       // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     rMethods[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetUserString(               // S_OK or error.
        mdString    stk,                    // [IN] String token.
        __out_ecount(cchString)
        LPWSTR      szString,               // [OUT] Copy of string.
        ULONG       cchString,              // [IN] Max chars of room in szString.
        ULONG       *pchString);       // [OUT] How many chars in actual string.

    virtual HRESULT STDMETHODCALLTYPE GetPinvokeMap(               // S_OK or error.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
        __out_ecount(cchImportName)
        LPWSTR      szImportName,           // [OUT] Import name.
        ULONG       cchImportName,          // [IN] Size of the name buffer.
        ULONG       *pchImportName,         // [OUT] Actual number of characters stored.
        mdModuleRef *pmrImportDLL);    // [OUT] ModuleRef token for the target DLL.

    virtual HRESULT STDMETHODCALLTYPE EnumSignatures(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdSignature rSignatures[],          // [OUT] put signatures here.
        ULONG       cmax,                   // [IN] max signatures to put.
        ULONG       *pcSignatures);    // [OUT] put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumTypeSpecs(               // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdTypeSpec  rTypeSpecs[],           // [OUT] put TypeSpecs here.
        ULONG       cmax,                   // [IN] max TypeSpecs to put.
        ULONG       *pcTypeSpecs);     // [OUT] put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumUserStrings(             // S_OK or error.
        HCORENUM    *phEnum,                // [IN/OUT] pointer to the enum.
        mdString    rStrings[],             // [OUT] put Strings here.
        ULONG       cmax,                   // [IN] max Strings to put.
        ULONG       *pcStrings);       // [OUT] put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetParamForMethodIndex(      // S_OK or error.
        mdMethodDef md,                     // [IN] Method token.
        ULONG       ulParamSeq,             // [IN] Parameter sequence.
        mdParamDef  *ppd);             // [IN] Put Param token here.

    virtual HRESULT STDMETHODCALLTYPE EnumCustomAttributes(        // S_OK or error.
        HCORENUM    *phEnum,                // [IN, OUT] COR enumerator.
        mdToken     tk,                     // [IN] Token to scope the enumeration, 0 for all.
        mdToken     tkType,                 // [IN] Type of interest, 0 for all.
        mdCustomAttribute rCustomAttributes[], // [OUT] Put custom attribute tokens here.
        ULONG       cMax,                   // [IN] Size of rCustomAttributes.
        ULONG       *pcCustomAttributes);  // [OUT, OPTIONAL] Put count of token values here.

    virtual HRESULT STDMETHODCALLTYPE GetCustomAttributeProps(     // S_OK or error.
        mdCustomAttribute cv,               // [IN] CustomAttribute token.
        mdToken     *ptkObj,                // [OUT, OPTIONAL] Put object token here.
        mdToken     *ptkType,               // [OUT, OPTIONAL] Put AttrType token here.
        void const  **ppBlob,               // [OUT, OPTIONAL] Put pointer to data here.
        ULONG       *pcbSize);         // [OUT, OPTIONAL] Put size of date here.

    virtual HRESULT STDMETHODCALLTYPE FindTypeRef(
        mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
        LPCWSTR     szName,                 // [IN] TypeRef Name.
        mdTypeRef   *ptr);             // [OUT] matching TypeRef.

    virtual HRESULT STDMETHODCALLTYPE GetMemberProps(
        mdToken     mb,                     // The member for which to get props.
        mdTypeDef   *pClass,                // Put member's class here.
        __out_ecount(cchMember)
        LPWSTR      szMember,               // Put member's name here.
        ULONG       cchMember,              // Size of szMember buffer in wide chars.
        ULONG       *pchMember,             // Put actual size here
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags,          // [OUT] Impl. Flags
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value
        ULONG       *pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    virtual HRESULT STDMETHODCALLTYPE GetFieldProps(
        mdFieldDef  mb,                     // The field for which to get props.
        mdTypeDef   *pClass,                // Put field's class here.
        __out_ecount(cchField)
        LPWSTR      szField,                // Put field's name here.
        ULONG       cchField,               // Size of szField buffer in wide chars.
        ULONG       *pchField,              // Put actual size here
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value
        ULONG       *pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    virtual HRESULT STDMETHODCALLTYPE GetPropertyProps(            // S_OK, S_FALSE, or error.
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
        ULONG       *pcOtherMethod);   // [OUT] total number of other method of this property

    virtual HRESULT STDMETHODCALLTYPE GetParamProps(               // S_OK or error.
        mdParamDef  tk,                     // [IN]The Parameter.
        mdMethodDef *pmd,                   // [OUT] Parent Method token.
        ULONG       *pulSequence,           // [OUT] Parameter sequence.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Put name here.
        ULONG       cchName,                // [OUT] Size of name buffer.
        ULONG       *pchName,               // [OUT] Put actual size of name here.
        DWORD       *pdwAttr,               // [OUT] Put flags here.
        DWORD       *pdwCPlusTypeFlag,      // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
        UVCP_CONSTANT *ppValue,             // [OUT] Constant value.
        ULONG       *pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    virtual HRESULT STDMETHODCALLTYPE GetCustomAttributeByName(    // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCWSTR     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData);         // [OUT] Put size of data here.

    virtual BOOL STDMETHODCALLTYPE IsValidToken(         // True or False.
        mdToken     tk);               // [IN] Given token.

    virtual HRESULT STDMETHODCALLTYPE GetNestedClassProps(         // S_OK or error.
        mdTypeDef   tdNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptdEnclosingClass); // [OUT] EnclosingClass token.

    virtual HRESULT STDMETHODCALLTYPE GetNativeCallConvFromSig(    // S_OK or error.
        void const  *pvSig,                 // [IN] Pointer to signature.
        ULONG       cbSig,                  // [IN] Count of signature bytes.
        ULONG       *pCallConv);       // [OUT] Put calling conv here (see CorPinvokemap).

    virtual HRESULT STDMETHODCALLTYPE IsGlobal(                    // S_OK or error.
        mdToken     pd,                     // [IN] Type, Field, or Method token.
        int         *pbGlobal);        // [OUT] Put 1 if global, 0 otherwise.

    //IMetaDataImport2
    virtual HRESULT STDMETHODCALLTYPE EnumGenericParams(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] TypeDef or MethodDef whose generic parameters are requested
        mdGenericParam rGenericParams[],    // [OUT] Put GenericParams here.
        ULONG       cMax,                   // [IN] Max GenericParams to put.
        ULONG       *pcGenericParams); // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetGenericParamProps(        // S_OK or error.
        mdGenericParam gp,                  // [IN] GenericParam
        ULONG        *pulParamSeq,          // [OUT] Index of the type parameter
        DWORD        *pdwParamFlags,        // [OUT] Flags, for future use (e.g. variance)
        mdToken      *ptOwner,              // [OUT] Owner (TypeDef or MethodDef)
        DWORD       *reserved,              // [OUT] For future use (e.g. non-type parameters)
        __out_ecount(cchName)
        LPWSTR       wzname,                // [OUT] Put name here
        ULONG        cchName,               // [IN] Size of buffer
        ULONG        *pchName);        // [OUT] Put size of name (wide chars) here.

    virtual HRESULT STDMETHODCALLTYPE GetMethodSpecProps(
        mdMethodSpec mi,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob);      // [OUT] actual size of signature blob

    virtual HRESULT STDMETHODCALLTYPE EnumGenericParamConstraints(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdGenericParam tk,                  // [IN] GenericParam whose constraints are requested
        mdGenericParamConstraint rGenericParamConstraints[],    // [OUT] Put GenericParamConstraints here.
        ULONG       cMax,                   // [IN] Max GenericParamConstraints to put.
        ULONG       *pcGenericParamConstraints); // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetGenericParamConstraintProps( // S_OK or error.
        mdGenericParamConstraint gpc,       // [IN] GenericParamConstraint
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType); // [OUT] TypeDef/Ref/Spec constraint

    virtual HRESULT STDMETHODCALLTYPE GetPEKind(                   // S_OK or error.
        DWORD* pdwPEKind,                   // [OUT] The kind of PE (0 - not a PE)
        DWORD* pdwMAchine);            // [OUT] Machine as defined in NT header

    virtual HRESULT STDMETHODCALLTYPE GetVersionString(            // S_OK or error.
        __out_ecount(ccBufSize)
        LPWSTR      pwzBuf,                 // [OUT] Put version string here.
        DWORD       ccBufSize,              // [IN] size of the buffer, in wide chars
        DWORD       *pccBufSize);      // [OUT] Size of the version string, wide chars, including terminating nul.

    virtual HRESULT STDMETHODCALLTYPE EnumMethodSpecs(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] MethodDef or MemberRef whose MethodSpecs are requested
        mdMethodSpec rMethodSpecs[],        // [OUT] Put MethodSpecs here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcMethodSpecs);   // [OUT] Put actual count here.


    // IMetaDataAssemblyImport
    virtual HRESULT STDMETHODCALLTYPE GetAssemblyProps(            // S_OK or error.
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Buffer to fill with assembly's simply name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags);    // [OUT] Flags.

    virtual HRESULT STDMETHODCALLTYPE GetAssemblyRefProps(         // S_OK or error.
        mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
        const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
        ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        const void  **ppbHashValue,         // [OUT] Hash blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
        DWORD       *pdwAssemblyRefFlags); // [OUT] Flags.

    virtual HRESULT STDMETHODCALLTYPE GetFileProps(                // S_OK or error.
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags);    // [OUT] Flags.

    virtual HRESULT STDMETHODCALLTYPE GetExportedTypeProps(        // S_OK or error.
        mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef or mdExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags); // [OUT] Flags.

    virtual HRESULT STDMETHODCALLTYPE GetManifestResourceProps(    // S_OK or error.
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        __out_ecount(cchName)
        LPWSTR      szName,                 // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ManifestResource.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags);// [OUT] Flags.

    virtual HRESULT STDMETHODCALLTYPE EnumAssemblyRefs(            // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdAssemblyRef rAssemblyRefs[],      // [OUT] Put AssemblyRefs here.
        ULONG       cMax,                   // [IN] Max AssemblyRefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumFiles(                   // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdFile      rFiles[],               // [OUT] Put Files here.
        ULONG       cMax,                   // [IN] Max Files to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumExportedTypes(           // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdExportedType   rExportedTypes[],  // [OUT] Put ExportedTypes here.
        ULONG       cMax,                   // [IN] Max ExportedTypes to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE EnumManifestResources(       // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdManifestResource  rManifestResources[],   // [OUT] Put ManifestResources here.
        ULONG       cMax,                   // [IN] Max Resources to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    virtual HRESULT STDMETHODCALLTYPE GetAssemblyFromScope(        // S_OK or error
        mdAssembly  *ptkAssembly);     // [OUT] Put token here.

    virtual HRESULT STDMETHODCALLTYPE FindExportedTypeByName(      // S_OK or error
        LPCWSTR     szName,                 // [IN] Name of the ExportedType.
        mdToken     mdtExportedType,        // [IN] ExportedType for the enclosing class.
        mdExportedType   *ptkExportedType); // [OUT] Put the ExportedType token here.

    virtual HRESULT STDMETHODCALLTYPE FindManifestResourceByName(  // S_OK or error
        LPCWSTR     szName,                 // [IN] Name of the ManifestResource.
        mdManifestResource *ptkManifestResource);  // [OUT] Put the ManifestResource token here.

    //STDMETHOD_(void, CloseEnum(
    //    HCORENUM hEnum);               // Enum to be closed.

    virtual HRESULT STDMETHODCALLTYPE FindAssembliesByName(        // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        IUnknown *ppIUnk[],                 // [OUT] put IMetaDataAssemblyImport pointers here
        ULONG    cMax,                      // [IN] The max number to put
        ULONG    *pcAssemblies);       // [OUT] The number of assemblies returned.

private:
  Volatile<LONG> m_cRefCount;
  ReleaseHolder<IMetaDataImport2> m_pInnerImport;
  ReleaseHolder<IMDInternalImport> m_pInnerInternalImport;
  ReleaseHolder<IMetaDataAssemblyImport> m_pInnerAssemblyImport;
  ReleaseHolder<IMetaDataEmit2> m_pInner;
  ReleaseHolder<IMetaDataAssemblyEmit> m_pInnerAssembly;

  // all tokens with values <= these maximums are considered pre-existing content
  // and can not be altered using this emitter
  mdTypeDef maxInitialTypeDef;
  mdMethodDef maxInitialMethodDef;
  mdFieldDef maxInitialFieldDef;
  mdMemberRef maxInitialMemberRef;
  mdParamDef maxInitialParamDef;
  mdCustomAttribute maxInitialCustomAttribute;
  mdEvent maxInitialEvent;
  mdProperty maxInitialProperty;
  mdGenericParam maxInitialGenericParam;
};



#endif
