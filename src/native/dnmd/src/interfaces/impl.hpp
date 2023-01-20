#ifndef _SRC_INTERFACES_IMPL_HPP_
#define _SRC_INTERFACES_IMPL_HPP_

#include <cstdint>
#include <atomic>

#include <external/cor.h>
#include <external/corhdr.h>

#include <dnmd.hpp>

class MetadataImportRO final : public IMetaDataImport2
{
    std::atomic_uint32_t _refCount;
    mdhandle_ptr _md_ptr;
    malloc_ptr<void> _malloc_to_free;
    cotaskmem_ptr _cotaskmem_to_free;

public:
    MetadataImportRO(mdhandle_ptr md_ptr, malloc_ptr<void> mallocMem, cotaskmem_ptr cotaskmemMem)
        : _refCount{ 1 }
        , _md_ptr{ std::move(md_ptr) }
        , _malloc_to_free{ std::move(mallocMem) }
        , _cotaskmem_to_free{ std::move(cotaskmemMem) }
    { }

    virtual ~MetadataImportRO() = default;

    mdhandle_t MetaData() const;

public: // IMetaDataImport
    STDMETHOD_(void, CloseEnum)(HCORENUM hEnum) override;
    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG *pulCount) override;
    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos) override;
    STDMETHOD(EnumTypeDefs)(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs) override;
    STDMETHOD(EnumInterfaceImpls)(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls) override;
    STDMETHOD(EnumTypeRefs)(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs) override;

    STDMETHOD(FindTypeDefByName)(
        LPCWSTR     szTypeDef,
        mdToken     tkEnclosingClass,
        mdTypeDef   *ptd) override;

    STDMETHOD(GetScopeProps)(
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName,
        GUID        *pmvid) override;

    STDMETHOD(GetModuleFromScope)(
        mdModule    *pmd) override;

    STDMETHOD(GetTypeDefProps)(
        mdTypeDef   td,
      _Out_writes_to_opt_(cchTypeDef, *pchTypeDef)
        LPWSTR      szTypeDef,
        ULONG       cchTypeDef,
        ULONG       *pchTypeDef,
        DWORD       *pdwTypeDefFlags,
        mdToken     *ptkExtends) override;

    STDMETHOD(GetInterfaceImplProps)(
        mdInterfaceImpl iiImpl,
        mdTypeDef   *pClass,
        mdToken     *ptkIface) override;

    STDMETHOD(GetTypeRefProps)(
        mdTypeRef   tr,
        mdToken     *ptkResolutionScope,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName) override;

    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd) override;

    STDMETHOD(EnumMembers)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMembersWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMethods)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMethodsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumFields)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumFieldsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens) override;


    STDMETHOD(EnumParams)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdParamDef  rParams[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMemberRefs)(
        HCORENUM    *phEnum,
        mdToken     tkParent,
        mdMemberRef rMemberRefs[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumMethodImpls)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdToken     rMethodBody[],
        mdToken     rMethodDecl[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(EnumPermissionSets)(
        HCORENUM    *phEnum,
        mdToken     tk,
        DWORD       dwActions,
        mdPermission rPermission[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(FindMember)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdToken     *pmb) override;

    STDMETHOD(FindMethod)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodDef *pmb) override;

    STDMETHOD(FindField)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdFieldDef  *pmb) override;

    STDMETHOD(FindMemberRef)(
        mdTypeRef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr) override;

    STDMETHOD (GetMethodProps)(
        mdMethodDef mb,
        mdTypeDef   *pClass,
      _Out_writes_to_opt_(cchMethod, *pchMethod)
        LPWSTR      szMethod,
        ULONG       cchMethod,
        ULONG       *pchMethod,
        DWORD       *pdwAttr,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags) override;

    STDMETHOD(GetMemberRefProps)(
        mdMemberRef mr,
        mdToken     *ptk,
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,
        ULONG       cchMember,
        ULONG       *pchMember,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pbSig) override;

    STDMETHOD(EnumProperties)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdProperty  rProperties[],
        ULONG       cMax,
        ULONG       *pcProperties) override;

    STDMETHOD(EnumEvents)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdEvent     rEvents[],
        ULONG       cMax,
        ULONG       *pcEvents) override;

    STDMETHOD(GetEventProps)(
        mdEvent     ev,
        mdTypeDef   *pClass,
        LPCWSTR     szEvent,
        ULONG       cchEvent,
        ULONG       *pchEvent,
        DWORD       *pdwEventFlags,
        mdToken     *ptkEventType,
        mdMethodDef *pmdAddOn,
        mdMethodDef *pmdRemoveOn,
        mdMethodDef *pmdFire,
        mdMethodDef rmdOtherMethod[],
        ULONG       cMax,
        ULONG       *pcOtherMethod) override;

    STDMETHOD(EnumMethodSemantics)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdToken     rEventProp[],
        ULONG       cMax,
        ULONG       *pcEventProp) override;

    STDMETHOD(GetMethodSemantics)(
        mdMethodDef mb,
        mdToken     tkEventProp,
        DWORD       *pdwSemanticsFlags) override;

    STDMETHOD(GetClassLayout) (
        mdTypeDef   td,
        DWORD       *pdwPackSize,
        COR_FIELD_OFFSET rFieldOffset[],
        ULONG       cMax,
        ULONG       *pcFieldOffset,
        ULONG       *pulClassSize) override;

    STDMETHOD(GetFieldMarshal) (
        mdToken     tk,
        PCCOR_SIGNATURE *ppvNativeType,
        ULONG       *pcbNativeType) override;

    STDMETHOD(GetRVA)(
        mdToken     tk,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags) override;

    STDMETHOD(GetPermissionSetProps) (
        mdPermission pm,
        DWORD       *pdwAction,
        void const  **ppvPermission,
        ULONG       *pcbPermission) override;

    STDMETHOD(GetSigFromToken)(
        mdSignature mdSig,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig) override;

    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName) override;

    STDMETHOD(EnumModuleRefs)(
        HCORENUM    *phEnum,
        mdModuleRef rModuleRefs[],
        ULONG       cMax,
        ULONG       *pcModuleRefs) override;

    STDMETHOD(GetTypeSpecFromToken)(
        mdTypeSpec typespec,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig) override;

    STDMETHOD(GetNameFromToken)(            // Not Recommended! May be removed!
        mdToken     tk,
        MDUTF8CSTR  *pszUtf8NamePtr) override;

    STDMETHOD(EnumUnresolvedMethods)(
        HCORENUM    *phEnum,
        mdToken     rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override;

    STDMETHOD(GetUserString)(
        mdString    stk,
      _Out_writes_to_opt_(cchString, *pchString)
        LPWSTR      szString,
        ULONG       cchString,
        ULONG       *pchString) override;

    STDMETHOD(GetPinvokeMap)(
        mdToken     tk,
        DWORD       *pdwMappingFlags,
      _Out_writes_to_opt_(cchImportName, *pchImportName)
        LPWSTR      szImportName,
        ULONG       cchImportName,
        ULONG       *pchImportName,
        mdModuleRef *pmrImportDLL) override;

    STDMETHOD(EnumSignatures)(
        HCORENUM    *phEnum,
        mdSignature rSignatures[],
        ULONG       cMax,
        ULONG       *pcSignatures) override;

    STDMETHOD(EnumTypeSpecs)(
        HCORENUM    *phEnum,
        mdTypeSpec  rTypeSpecs[],
        ULONG       cMax,
        ULONG       *pcTypeSpecs) override;

    STDMETHOD(EnumUserStrings)(
        HCORENUM    *phEnum,
        mdString    rStrings[],
        ULONG       cMax,
        ULONG       *pcStrings) override;

    STDMETHOD(GetParamForMethodIndex)(
        mdMethodDef md,
        ULONG       ulParamSeq,
        mdParamDef  *ppd) override;

    STDMETHOD(EnumCustomAttributes)(
        HCORENUM    *phEnum,
        mdToken     tk,
        mdToken     tkType,
        mdCustomAttribute rCustomAttributes[],
        ULONG       cMax,
        ULONG       *pcCustomAttributes) override;

    STDMETHOD(GetCustomAttributeProps)(
        mdCustomAttribute cv,
        mdToken     *ptkObj,
        mdToken     *ptkType,
        void const  **ppBlob,
        ULONG       *pcbSize) override;

    STDMETHOD(FindTypeRef)(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr) override;

    STDMETHOD(GetMemberProps)(
        mdToken     mb,
        mdTypeDef   *pClass,
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,
        ULONG       cchMember,
        ULONG       *pchMember,
        DWORD       *pdwAttr,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppValue,
        ULONG       *pcchValue) override;

    STDMETHOD(GetFieldProps)(
        mdFieldDef  mb,
        mdTypeDef   *pClass,
      _Out_writes_to_opt_(cchField, *pchField)
        LPWSTR      szField,
        ULONG       cchField,
        ULONG       *pchField,
        DWORD       *pdwAttr,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppValue,
        ULONG       *pcchValue) override;

    STDMETHOD(GetPropertyProps)(
        mdProperty  prop,
        mdTypeDef   *pClass,
        LPCWSTR     szProperty,
        ULONG       cchProperty,
        ULONG       *pchProperty,
        DWORD       *pdwPropFlags,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pbSig,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppDefaultValue,
        ULONG       *pcchDefaultValue,
        mdMethodDef *pmdSetter,
        mdMethodDef *pmdGetter,
        mdMethodDef rmdOtherMethod[],
        ULONG       cMax,
        ULONG       *pcOtherMethod) override;

    STDMETHOD(GetParamProps)(
        mdParamDef  tk,
        mdMethodDef *pmd,
        ULONG       *pulSequence,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName,
        DWORD       *pdwAttr,
        DWORD       *pdwCPlusTypeFlag,
        UVCP_CONSTANT *ppValue,
        ULONG       *pcchValue) override;

    STDMETHOD(GetCustomAttributeByName)(
        mdToken     tkObj,
        LPCWSTR     szName,
        const void  **ppData,
        ULONG       *pcbData) override;

    STDMETHOD_(BOOL, IsValidToken)(
        mdToken     tk) override;

    STDMETHOD(GetNestedClassProps)(
        mdTypeDef   tdNestedClass,
        mdTypeDef   *ptdEnclosingClass) override;

    STDMETHOD(GetNativeCallConvFromSig)(
        void const  *pvSig,
        ULONG       cbSig,
        ULONG       *pCallConv) override;

    STDMETHOD(IsGlobal)(
        mdToken     pd,
        int         *pbGlobal) override;

public: // IMetaDataImport2
    STDMETHOD(EnumGenericParams)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdGenericParam rGenericParams[],
        ULONG       cMax,
        ULONG       *pcGenericParams) override;

    STDMETHOD(GetGenericParamProps)(
        mdGenericParam gp,
        ULONG        *pulParamSeq,
        DWORD        *pdwParamFlags,
        mdToken      *ptOwner,
        DWORD       *reserved,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR       wzname,
        ULONG        cchName,
        ULONG        *pchName) override;

    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec mi,
        mdToken *tkParent,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob) override;

    STDMETHOD(EnumGenericParamConstraints)(
        HCORENUM    *phEnum,
        mdGenericParam tk,
        mdGenericParamConstraint rGenericParamConstraints[],
        ULONG       cMax,
        ULONG       *pcGenericParamConstraints) override;

    STDMETHOD(GetGenericParamConstraintProps)(
        mdGenericParamConstraint gpc,
        mdGenericParam *ptGenericParam,
        mdToken      *ptkConstraintType) override;

    STDMETHOD(GetPEKind)(
        DWORD* pdwPEKind,
        DWORD* pdwMAchine) override;

    STDMETHOD(GetVersionString)(
      _Out_writes_to_opt_(ccBufSize, *pccBufSize)
        LPWSTR      pwzBuf,
        DWORD       ccBufSize,
        DWORD       *pccBufSize) override;

    STDMETHOD(EnumMethodSpecs)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdMethodSpec rMethodSpecs[],
        ULONG       cMax,
        ULONG       *pcMethodSpecs) override;

public: // IUnknown
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject) override
    {
        if (ppvObject == nullptr)
            return E_POINTER;

        if (riid == IID_IUnknown)
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else if (riid == IID_IMetaDataImport ||riid == IID_IMetaDataImport2)
        {
            *ppvObject = static_cast<IMetaDataImport2*>(this);
        }
        else
        {
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        (void)AddRef();
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void) override
    {
        return ++_refCount;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void) override
    {
        uint32_t c = --_refCount;
        if (c == 0)
            delete this;
        return c;
    }
};

enum class HCORENUMType : uint32_t
{
    Table = 1, Dynamic
};

// Represents a singly linked list or dynamic uint32_t array enumerator
class HCORENUMImpl final
{
    HCORENUMType _type;
    uint32_t _entrySpan; // The number of entries equal to a single unit.

    struct EnumData final
    {
        union
        {
            // Enumerate for tables
            struct
            {
                mdcursor_t Current;
                mdcursor_t Start;
            } Table;

            // Enumerate for dynamic uint32_t array
            struct
            {
                uint32_t Page[16];
            } Dynamic;
        };

        uint32_t ReadIn;
        uint32_t Total;
        EnumData* Next;
    };

    EnumData _data;
    EnumData* _curr;
    EnumData* _last;

public: // static
    // Lifetime operations
    static HRESULT CreateTableEnum(_In_ uint32_t count, _Out_ HCORENUMImpl** impl) noexcept;
    static void InitTableEnum(_Inout_ HCORENUMImpl& impl, _In_ uint32_t index, _In_ mdcursor_t cursor, _In_ uint32_t rows) noexcept;

    // If multiple values represent a single entry, the "entrySpan" argument
    // can be used to indicate the count for a single entry.
    static HRESULT CreateDynamicEnum(_Out_ HCORENUMImpl** impl, _In_ uint32_t entrySpan = 1) noexcept;
    static HRESULT AddToDynamicEnum(_Inout_ HCORENUMImpl& impl, uint32_t value) noexcept;

    static void Destroy(_In_ HCORENUMImpl* impl) noexcept;

public: // instance
    // Get the total items for this enumeration
    uint32_t Count() const noexcept;

    // Read in the tokens for this enumeration
    HRESULT ReadTokens(
        mdToken rTokens[],
        ULONG cMax,
        ULONG* pcTokens) noexcept;

    HRESULT ReadTokenPairs(
        mdToken rTokens1[],
        mdToken rTokens2[],
        ULONG cMax,
        ULONG* pcTokens) noexcept;

    // Reset the enumeration to a specific position
    HRESULT Reset(_In_ ULONG position) noexcept;

private:
    HRESULT ReadOneToken(mdToken& rToken, uint32_t& count) noexcept;
    HRESULT ReadTableTokens(
        mdToken rTokens[],
        uint32_t cMax,
        uint32_t& tokenCount) noexcept;
    HRESULT ReadDynamicTokens(
        mdToken rTokens[],
        uint32_t cMax,
        uint32_t& tokenCount) noexcept;

    HRESULT ResetTableEnum(_In_ uint32_t position) noexcept;
    HRESULT ResetDynamicEnum(_In_ uint32_t position) noexcept;
};

struct HCORENUMImplDeleter
{
    using pointer = HCORENUMImpl*;
    void operator()(HCORENUMImpl* mem)
    {
        HCORENUMImpl::Destroy(mem);
    }
};

// C++ lifetime wrapper for HCORENUMImpl memory
using HCORENUMImpl_ptr = std::unique_ptr<HCORENUMImpl, HCORENUMImplDeleter>;

#endif // _SRC_INTERFACES_IMPL_HPP_