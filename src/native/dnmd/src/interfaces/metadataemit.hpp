#ifndef _SRC_INTERFACES_METADATAEMIT_HPP_
#define _SRC_INTERFACES_METADATAEMIT_HPP_

#include "internal/dnmd_platform.hpp"
#include "tearoffbase.hpp"
#include "controllingiunknown.hpp"
#include "dnmdowner.hpp"

#include <external/cor.h>
#include <external/corhdr.h>

#include <cstdint>
#include <atomic>

class MetadataEmit final : public TearOffBase<IMetaDataEmit2, IMetaDataAssemblyEmit>
{
    mdhandle_view _md_ptr;

protected:
    bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
    {
        if (riid == IID_IMetaDataEmit || riid == IID_IMetaDataEmit)
        {
            *ppvObject = static_cast<IMetaDataEmit2*>(this);
            return true;
        }
        else if (riid == IID_IMetaDataAssemblyEmit)
        {
            *ppvObject = static_cast<IMetaDataAssemblyEmit*>(this);
            return true;
        }
        return false;
    }

public:
    MetadataEmit(IUnknown* controllingUnknown, mdhandle_view md_ptr)
        : TearOffBase(controllingUnknown)
        , _md_ptr{ std::move(md_ptr) }
    { }

    virtual ~MetadataEmit() = default;

    mdhandle_t MetaData()
    {
        return _md_ptr.get();
    }

public: // IMetaDataEmit
    STDMETHOD(SetModuleProps)(
        LPCWSTR     szName) override;

    STDMETHOD(Save)(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags) override;

    STDMETHOD(SaveToStream)(
        IStream     *pIStream,
        DWORD       dwSaveFlags) override;

    STDMETHOD(GetSaveSize)(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize) override;

    STDMETHOD(DefineTypeDef)(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   *ptd) override;

    STDMETHOD(DefineNestedType)(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   tdEncloser,
        mdTypeDef   *ptd) override;

    STDMETHOD(SetHandler)(
        IUnknown    *pUnk) override;

    STDMETHOD(DefineMethod)(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwMethodFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags,
        mdMethodDef *pmd) override;

    STDMETHOD(DefineMethodImpl)(
        mdTypeDef   td,
        mdToken     tkBody,
        mdToken     tkDecl) override;

    STDMETHOD(DefineTypeRefByName)(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr) override;

    STDMETHOD(DefineImportType)(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdTypeDef   tdImport,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdTypeRef   *ptr) override;

    STDMETHOD(DefineMemberRef)(
        mdToken     tkImport,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr) override;

    STDMETHOD(DefineImportMember)(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdToken     mbMember,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdToken     tkParent,
        mdMemberRef *pmr) override;

    STDMETHOD(DefineEvent) (
        mdTypeDef   td,
        LPCWSTR     szEvent,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[],
        mdEvent     *pmdEvent) override;

    STDMETHOD(SetClassLayout) (
        mdTypeDef   td,
        DWORD       dwPackSize,
        COR_FIELD_OFFSET rFieldOffsets[],
        ULONG       ulClassSize) override;

    STDMETHOD(DeleteClassLayout) (
        mdTypeDef   td) override;

    STDMETHOD(SetFieldMarshal) (
        mdToken     tk,
        PCCOR_SIGNATURE pvNativeType,
        ULONG       cbNativeType) override;

    STDMETHOD(DeleteFieldMarshal) (
        mdToken     tk) override;

    STDMETHOD(DefinePermissionSet) (
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm) override;

    STDMETHOD(SetRVA)(
        mdMethodDef md,
        ULONG       ulRVA) override;

    STDMETHOD(GetTokenFromSig)(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdSignature *pmsig) override;

    STDMETHOD(DefineModuleRef)(
        LPCWSTR     szName,
        mdModuleRef *pmur) override;


    STDMETHOD(SetParent)(
        mdMemberRef mr,
        mdToken     tk) override;

    STDMETHOD(GetTokenFromTypeSpec)(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdTypeSpec *ptypespec) override;

    STDMETHOD(SaveToMemory)(
        void        *pbData,
        ULONG       cbData) override;

    STDMETHOD(DefineUserString)(
        LPCWSTR szString,
        ULONG       cchString,
        mdString    *pstk) override;

    STDMETHOD(DeleteToken)(
        mdToken     tkObj) override;

    STDMETHOD(SetMethodProps)(
        mdMethodDef md,
        DWORD       dwMethodFlags,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags) override;

    STDMETHOD(SetTypeDefProps)(
        mdTypeDef   td,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[]) override;

    STDMETHOD(SetEventProps)(
        mdEvent     ev,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[]) override;

    STDMETHOD(SetPermissionSetProps)(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm) override;

    STDMETHOD(DefinePinvokeMap)(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL) override;

    STDMETHOD(SetPinvokeMap)(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL) override;

    STDMETHOD(DeletePinvokeMap)(
        mdToken     tk) override;


    STDMETHOD(DefineCustomAttribute)(
        mdToken     tkOwner,
        mdToken     tkCtor,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute,
        mdCustomAttribute *pcv) override;

    STDMETHOD(SetCustomAttributeValue)(
        mdCustomAttribute pcv,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute) override;

    STDMETHOD(DefineField)(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwFieldFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdFieldDef  *pmd) override;

    STDMETHOD(DefineProperty)(
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
        mdProperty  *pmdProp) override;

    STDMETHOD(DefineParam)(
        mdMethodDef md,
        ULONG       ulParamSeq,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdParamDef  *ppd) override;

    STDMETHOD(SetFieldProps)(
        mdFieldDef  fd,
        DWORD       dwFieldFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue) override;

    STDMETHOD(SetPropertyProps)(
        mdProperty  pr,
        DWORD       dwPropFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[]) override;

    STDMETHOD(SetParamProps)(
        mdParamDef  pd,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue) override;


    STDMETHOD(DefineSecurityAttributeSet)(
        mdToken     tkObj,
        COR_SECATTR rSecAttrs[],
        ULONG       cSecAttrs,
        ULONG       *pulErrorAttr) override;

    STDMETHOD(ApplyEditAndContinue)(
        IUnknown    *pImport) override;

    STDMETHOD(TranslateSigWithScope)(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *import,
        PCCOR_SIGNATURE pbSigBlob,
        ULONG       cbSigBlob,
        IMetaDataAssemblyEmit *pAssemEmit,
        IMetaDataEmit *emit,
        PCOR_SIGNATURE pvTranslatedSig,
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig) override;

    STDMETHOD(SetMethodImplFlags)(
        mdMethodDef md,
        DWORD       dwImplFlags) override;

    STDMETHOD(SetFieldRVA)(
        mdFieldDef  fd,
        ULONG       ulRVA) override;

    STDMETHOD(Merge)(
        IMetaDataImport *pImport,
        IMapToken   *pHostMapToken,
        IUnknown    *pHandler) override;

    STDMETHOD(MergeEnd)() override;

public: // IMetaDataEmit2
    STDMETHOD(DefineMethodSpec)(
        mdToken     tkParent,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodSpec *pmi) override;

    STDMETHOD(GetDeltaSaveSize)(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize) override;

    STDMETHOD(SaveDelta)(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags) override;

    STDMETHOD(SaveDeltaToStream)(
        IStream     *pIStream,
        DWORD       dwSaveFlags) override;

    STDMETHOD(SaveDeltaToMemory)(
        void        *pbData,
        ULONG       cbData) override;

    STDMETHOD(DefineGenericParam)(
        mdToken      tk,
        ULONG        ulParamSeq,
        DWORD        dwParamFlags,
        LPCWSTR      szname,
        DWORD        reserved,
        mdToken      rtkConstraints[],
        mdGenericParam *pgp) override;

    STDMETHOD(SetGenericParamProps)(
        mdGenericParam gp,
        DWORD        dwParamFlags,
        LPCWSTR      szName,
        DWORD        reserved,
        mdToken      rtkConstraints[]) override;

    STDMETHOD(ResetENCLog)() override;

public: // IMetaDataAssemblyEmit
    STDMETHOD(DefineAssembly)(
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags,
        mdAssembly  *pma) override;

    STDMETHOD(DefineAssemblyRef)(
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags,
        mdAssemblyRef *pmdar) override;

    STDMETHOD(DefineFile)(
        LPCWSTR     szName,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags,
        mdFile      *pmdf) override;

    STDMETHOD(DefineExportedType)(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags,
        mdExportedType   *pmdct) override;

    STDMETHOD(DefineManifestResource)(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags,
        mdManifestResource  *pmdmr) override;

    STDMETHOD(SetAssemblyProps)(
        mdAssembly  pma,
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags) override;

    STDMETHOD(SetAssemblyRefProps)(
        mdAssemblyRef ar,
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags) override;

    STDMETHOD(SetFileProps)(
        mdFile      file,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags) override;

    STDMETHOD(SetExportedTypeProps)(
        mdExportedType   ct,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags) override;

    STDMETHOD(SetManifestResourceProps)(
        mdManifestResource  mr,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags) override;
};

#endif