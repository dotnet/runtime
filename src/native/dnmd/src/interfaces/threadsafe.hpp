#ifndef _SRC_INTERFACES_THREADSAFE_HPP_
#define _SRC_INTERFACES_THREADSAFE_HPP_

#include "internal/dnmd_platform.hpp"
#include "tearoffbase.hpp"
#include "controllingiunknown.hpp"
#include "dnmdowner.hpp"
#include "pal.hpp"
#include "importhelpers.hpp"

#include <external/cor.h>
#include <external/corhdr.h>

#include <cstdint>
#include <mutex>

// A tear-off that re-exposes an mdhandle_view as an IDNMDOwner*.
class DelegatingDNMDOwner final : public TearOffBase<IDNMDOwner>
{
    mdhandle_view _inner;

protected:
    virtual bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
    {
        if (riid == IID_IDNMDOwner)
        {
            *ppvObject = static_cast<IDNMDOwner*>(this);
            return true;
        }
        return false;
    }

public:
    DelegatingDNMDOwner(IUnknown* controllingUnknown, mdhandle_view inner)
        : TearOffBase(controllingUnknown)
        , _inner{ inner }
    { }

    virtual ~DelegatingDNMDOwner() = default;

    mdhandle_t MetaData() override
    {
        return _inner.get();
    }
};

template<typename TImport, typename TEmit>
class ThreadSafeImportEmit : public TearOffBase<IMetaDataImport2, IMetaDataEmit2, IMetaDataAssemblyImport, IMetaDataAssemblyEmit>
{
    pal::ReadWriteLock _lock;
    // owning reference to the thread-unsafe object that provides the underlying implementation.
    dncp::com_ptr<ControllingIUnknown> _threadUnsafe;
    // non-owning reference to the concrete non-locking implementations
    TImport* _import;
    TEmit* _emit;

protected:
    virtual bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
    {
        assert(riid != IID_IUnknown);
        if (riid == IID_IMetaDataImport || riid == IID_IMetaDataImport2)
        {
            *ppvObject = static_cast<IMetaDataImport2*>(this);
            return true;
        }
        if (riid == IID_IMetaDataAssemblyImport)
        {
            *ppvObject = static_cast<IMetaDataAssemblyImport*>(this);
            return true;
        }
        if (riid == IID_IMetaDataEmit || riid == IID_IMetaDataEmit2)
        {
            *ppvObject = static_cast<IMetaDataEmit2*>(this);
            return true;
        }
        if (riid == IID_IMetaDataAssemblyEmit)
        {
            *ppvObject = static_cast<IMetaDataAssemblyEmit*>(this);
            return true;
        }
        return false;
    }

public:
    ThreadSafeImportEmit(IUnknown* controllingUnknown, dncp::com_ptr<ControllingIUnknown>&& threadUnsafe, TImport* import, TEmit* emit)
        : TearOffBase(controllingUnknown)
        , _lock { }
        , _threadUnsafe{ std::move(threadUnsafe) }
        , _import{ import }
        , _emit{ emit }
    {
        assert(_threadUnsafe.p != nullptr);
        assert(_import != nullptr);
        assert(_emit != nullptr);
    }

    virtual ~ThreadSafeImportEmit() = default;

public: // IMetaDataImport
    STDMETHOD_(void, CloseEnum)(HCORENUM hEnum) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->CloseEnum(hEnum);
    }

    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG *pulCount) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->CountEnum(hEnum, pulCount);
    }

    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->ResetEnum(hEnum, ulPos);
    }

    STDMETHOD(EnumTypeDefs)(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumTypeDefs(phEnum, rTypeDefs, cMax, pcTypeDefs);
    }

    STDMETHOD(EnumInterfaceImpls)(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumInterfaceImpls(phEnum, td, rImpls, cMax, pcImpls);
    }

    STDMETHOD(EnumTypeRefs)(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumTypeRefs(phEnum, rTypeRefs, cMax, pcTypeRefs);
    }

    STDMETHOD(FindTypeDefByName)(
        LPCWSTR     szTypeDef,
        mdToken     tkEnclosingClass,
        mdTypeDef   *ptd) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindTypeDefByName(szTypeDef, tkEnclosingClass, ptd);
    }

    STDMETHOD(GetScopeProps)(
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName,
        GUID        *pmvid) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetScopeProps(szName, cchName, pchName, pmvid);
    }

    STDMETHOD(GetModuleFromScope)(
        mdModule    *pmd) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetModuleFromScope(pmd);
    }

    STDMETHOD(GetTypeDefProps)(
        mdTypeDef   td,
      _Out_writes_to_opt_(cchTypeDef, *pchTypeDef)
        LPWSTR      szTypeDef,
        ULONG       cchTypeDef,
        ULONG       *pchTypeDef,
        DWORD       *pdwTypeDefFlags,
        mdToken     *ptkExtends) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetTypeDefProps(td, szTypeDef, cchTypeDef, pchTypeDef, pdwTypeDefFlags, ptkExtends);
    }

    STDMETHOD(GetInterfaceImplProps)(
        mdInterfaceImpl iiImpl,
        mdTypeDef   *pClass,
        mdToken     *ptkIface) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetInterfaceImplProps(iiImpl, pClass, ptkIface);
    }

    STDMETHOD(GetTypeRefProps)(
        mdTypeRef   tr,
        mdToken     *ptkResolutionScope,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetTypeRefProps(tr, ptkResolutionScope, szName, cchName, pchName);
    }

    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->ResolveTypeRef(tr, riid, ppIScope, ptd);
    }

    STDMETHOD(EnumMembers)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMembers(phEnum, cl, rMembers, cMax, pcTokens);
    }

    STDMETHOD(EnumMembersWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdToken     rMembers[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMembersWithName(phEnum, cl, szName, rMembers, cMax, pcTokens);
    }

    STDMETHOD(EnumMethods)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMethods(phEnum, cl, rMethods, cMax, pcTokens);
    }

    STDMETHOD(EnumMethodsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdMethodDef rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMethodsWithName(phEnum, cl, szName, rMethods, cMax, pcTokens);
    }

    STDMETHOD(EnumFields)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumFields(phEnum, cl, rFields, cMax, pcTokens);
    }

    STDMETHOD(EnumFieldsWithName)(
        HCORENUM    *phEnum,
        mdTypeDef   cl,
        LPCWSTR     szName,
        mdFieldDef  rFields[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumFieldsWithName(phEnum, cl, szName, rFields, cMax, pcTokens);
    }

    STDMETHOD(EnumParams)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdParamDef  rParams[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumParams(phEnum, mb, rParams, cMax, pcTokens);
    }

    STDMETHOD(EnumMemberRefs)(
        HCORENUM    *phEnum,
        mdToken     tkParent,
        mdMemberRef rMemberRefs[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMemberRefs(phEnum, tkParent, rMemberRefs, cMax, pcTokens);
    }

    STDMETHOD(EnumMethodImpls)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdToken     rMethodBody[],
        mdToken     rMethodDecl[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMethodImpls(phEnum, td, rMethodBody, rMethodDecl, cMax, pcTokens);
    }

    STDMETHOD(EnumPermissionSets)(
        HCORENUM    *phEnum,
        mdToken     tk,
        DWORD       dwActions,
        mdPermission rPermission[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumPermissionSets(phEnum, tk, dwActions, rPermission, cMax, pcTokens);
    }

    STDMETHOD(FindMember)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdToken     *pmb) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindMember(td, szName, pvSigBlob, cbSigBlob, pmb);
    }

    STDMETHOD(FindMethod)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodDef *pmb) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindMethod(td, szName, pvSigBlob, cbSigBlob, pmb);
    }

    STDMETHOD(FindField)(
        mdTypeDef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdFieldDef  *pmb) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindField(td, szName, pvSigBlob, cbSigBlob, pmb);
    }

    STDMETHOD(FindMemberRef)(
        mdTypeRef   td,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindMemberRef(td, szName, pvSigBlob, cbSigBlob, pmr);
    }

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
        DWORD       *pdwImplFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetMethodProps(mb, pClass, szMethod, cchMethod, pchMethod, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags);
    }

    STDMETHOD(GetMemberRefProps)(
        mdMemberRef mr,
        mdToken     *ptk,
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,
        ULONG       cchMember,
        ULONG       *pchMember,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pbSig) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetMemberRefProps(mr, ptk, szMember, cchMember, pchMember, ppvSigBlob, pbSig);
    }

    STDMETHOD(EnumProperties)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdProperty  rProperties[],
        ULONG       cMax,
        ULONG       *pcProperties) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumProperties(phEnum, td, rProperties, cMax, pcProperties);
    }

    STDMETHOD(EnumEvents)(
        HCORENUM    *phEnum,
        mdTypeDef   td,
        mdEvent     rEvents[],
        ULONG       cMax,
        ULONG       *pcEvents) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumEvents(phEnum, td, rEvents, cMax, pcEvents);
    }

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
        ULONG       *pcOtherMethod) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetEventProps(ev, pClass, szEvent, cchEvent, pchEvent, pdwEventFlags, ptkEventType, pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod);
    }

    STDMETHOD(EnumMethodSemantics)(
        HCORENUM    *phEnum,
        mdMethodDef mb,
        mdToken     rEventProp[],
        ULONG       cMax,
        ULONG       *pcEventProp) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMethodSemantics(phEnum, mb, rEventProp, cMax, pcEventProp);
    }

    STDMETHOD(GetMethodSemantics)(
        mdMethodDef mb,
        mdToken     tkEventProp,
        DWORD       *pdwSemanticsFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetMethodSemantics(mb, tkEventProp, pdwSemanticsFlags);
    }

    STDMETHOD(GetClassLayout) (
        mdTypeDef   td,
        DWORD       *pdwPackSize,
        COR_FIELD_OFFSET rFieldOffset[],
        ULONG       cMax,
        ULONG       *pcFieldOffset,
        ULONG       *pulClassSize) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetClassLayout(td, pdwPackSize, rFieldOffset, cMax, pcFieldOffset, pulClassSize);
    }

    STDMETHOD(GetFieldMarshal) (
        mdToken     tk,
        PCCOR_SIGNATURE *ppvNativeType,
        ULONG       *pcbNativeType) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetFieldMarshal(tk, ppvNativeType, pcbNativeType);
    }

    STDMETHOD(GetRVA)(
        mdToken     tk,
        ULONG       *pulCodeRVA,
        DWORD       *pdwImplFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetRVA(tk, pulCodeRVA, pdwImplFlags);
    }

    STDMETHOD(GetPermissionSetProps) (
        mdPermission pm,
        DWORD       *pdwAction,
        void const  **ppvPermission,
        ULONG       *pcbPermission) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission);
    }

    STDMETHOD(GetSigFromToken)(
        mdSignature mdSig,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetSigFromToken(mdSig, ppvSig, pcbSig);
    }

    STDMETHOD(GetModuleRefProps)(
        mdModuleRef mur,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,
        ULONG       cchName,
        ULONG       *pchName) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetModuleRefProps(mur, szName, cchName, pchName);
    }

    STDMETHOD(EnumModuleRefs)(
        HCORENUM    *phEnum,
        mdModuleRef rModuleRefs[],
        ULONG       cMax,
        ULONG       *pcModuleRefs) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumModuleRefs(phEnum, rModuleRefs, cMax, pcModuleRefs);
    }

    STDMETHOD(GetTypeSpecFromToken)(
        mdTypeSpec typespec,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetTypeSpecFromToken(typespec, ppvSig, pcbSig);
    }

    STDMETHOD(GetNameFromToken)(            // Not Recommended! May be removed!
        mdToken     tk,
        MDUTF8CSTR  *pszUtf8NamePtr) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetNameFromToken(tk, pszUtf8NamePtr);
    }

    STDMETHOD(EnumUnresolvedMethods)(
        HCORENUM    *phEnum,
        mdToken     rMethods[],
        ULONG       cMax,
        ULONG       *pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumUnresolvedMethods(phEnum, rMethods, cMax, pcTokens);
    }

    STDMETHOD(GetUserString)(
        mdString    stk,
      _Out_writes_to_opt_(cchString, *pchString)
        LPWSTR      szString,
        ULONG       cchString,
        ULONG       *pchString) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetUserString(stk, szString, cchString, pchString);
    }

    STDMETHOD(GetPinvokeMap)(
        mdToken     tk,
        DWORD       *pdwMappingFlags,
      _Out_writes_to_opt_(cchImportName, *pchImportName)
        LPWSTR      szImportName,
        ULONG       cchImportName,
        ULONG       *pchImportName,
        mdModuleRef *pmrImportDLL) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetPinvokeMap(tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL);
    }

    STDMETHOD(EnumSignatures)(
        HCORENUM    *phEnum,
        mdSignature rSignatures[],
        ULONG       cMax,
        ULONG       *pcSignatures) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumSignatures(phEnum, rSignatures, cMax, pcSignatures);
    }

    STDMETHOD(EnumTypeSpecs)(
        HCORENUM    *phEnum,
        mdTypeSpec  rTypeSpecs[],
        ULONG       cMax,
        ULONG       *pcTypeSpecs) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumTypeSpecs(phEnum, rTypeSpecs, cMax, pcTypeSpecs);
    }

    STDMETHOD(EnumUserStrings)(
        HCORENUM    *phEnum,
        mdString    rStrings[],
        ULONG       cMax,
        ULONG       *pcStrings) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumUserStrings(phEnum, rStrings, cMax, pcStrings);
    }

    STDMETHOD(GetParamForMethodIndex)(
        mdMethodDef md,
        ULONG       ulParamSeq,
        mdParamDef  *ppd) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetParamForMethodIndex(md, ulParamSeq, ppd);
    }

    STDMETHOD(EnumCustomAttributes)(
        HCORENUM    *phEnum,
        mdToken     tk,
        mdToken     tkType,
        mdCustomAttribute rCustomAttributes[],
        ULONG       cMax,
        ULONG       *pcCustomAttributes) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumCustomAttributes(phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes);
    }

    STDMETHOD(GetCustomAttributeProps)(
        mdCustomAttribute cv,
        mdToken     *ptkObj,
        mdToken     *ptkType,
        void const  **ppBlob,
        ULONG       *pcbSize) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetCustomAttributeProps(cv, ptkObj, ptkType, ppBlob, pcbSize);
    }

    STDMETHOD(FindTypeRef)(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindTypeRef(tkResolutionScope, szName, ptr);
    }

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
        ULONG       *pcchValue) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetMemberProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags, pdwCPlusTypeFlag, ppValue, pcchValue);
    }

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
        ULONG       *pcchValue) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetFieldProps(mb, pClass, szField, cchField, pchField, pdwAttr, ppvSigBlob, pcbSigBlob, pdwCPlusTypeFlag, ppValue, pcchValue);
    }

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
        ULONG       *pcOtherMethod) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetPropertyProps(prop, pClass, szProperty, cchProperty, pchProperty, pdwPropFlags, ppvSig, pbSig, pdwCPlusTypeFlag, ppDefaultValue, pcchDefaultValue, pmdSetter, pmdGetter, rmdOtherMethod, cMax, pcOtherMethod);
    }

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
        ULONG       *pcchValue) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetParamProps(tk, pmd, pulSequence, szName, cchName, pchName, pdwAttr, pdwCPlusTypeFlag, ppValue, pcchValue);
    }

    STDMETHOD(GetCustomAttributeByName)(
        mdToken     tkObj,
        LPCWSTR     szName,
        void const**  ppData,
        ULONG       *pcbData) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetCustomAttributeByName(tkObj, szName, ppData, pcbData);
    }

    STDMETHOD_(BOOL, IsValidToken)(
        mdToken     tk) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->IsValidToken(tk);
    }

    STDMETHOD(GetNestedClassProps)(
        mdTypeDef   tdNestedClass,
        mdTypeDef   *ptdEnclosingClass) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetNestedClassProps(tdNestedClass, ptdEnclosingClass);
    }

    STDMETHOD(GetNativeCallConvFromSig)(
        void const  *pvSig,
        ULONG       cbSig,
        ULONG       *pCallConv) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetNativeCallConvFromSig(pvSig, cbSig, pCallConv);
    }

    STDMETHOD(IsGlobal)(
        mdToken     pd,
        int         *pbGlobal) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->IsGlobal(pd, pbGlobal);
    }

public: // IMetaDataImport2
    STDMETHOD(EnumGenericParams)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdGenericParam rGenericParams[],
        ULONG       cMax,
        ULONG       *pcGenericParams) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumGenericParams(phEnum, tk, rGenericParams, cMax, pcGenericParams);
    }

    STDMETHOD(GetGenericParamProps)(
        mdGenericParam gp,
        ULONG        *pulParamSeq,
        DWORD        *pdwParamFlags,
        mdToken      *ptOwner,
        DWORD       *reserved,
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR       wzname,
        ULONG        cchName,
        ULONG        *pchName) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetGenericParamProps(gp, pulParamSeq, pdwParamFlags, ptOwner, reserved, wzname, cchName, pchName);
    }

    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec mi,
        mdToken *tkParent,
        PCCOR_SIGNATURE *ppvSigBlob,
        ULONG       *pcbSigBlob) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetMethodSpecProps(mi, tkParent, ppvSigBlob, pcbSigBlob);
    }

    STDMETHOD(EnumGenericParamConstraints)(
        HCORENUM    *phEnum,
        mdGenericParam tk,
        mdGenericParamConstraint rGenericParamConstraints[],
        ULONG       cMax,
        ULONG       *pcGenericParamConstraints) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumGenericParamConstraints(phEnum, tk, rGenericParamConstraints, cMax, pcGenericParamConstraints);
    }

    STDMETHOD(GetGenericParamConstraintProps)(
        mdGenericParamConstraint gpc,
        mdGenericParam *ptGenericParam,
        mdToken      *ptkConstraintType) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetGenericParamConstraintProps(gpc, ptGenericParam, ptkConstraintType);
    }

    STDMETHOD(GetPEKind)(
        DWORD* pdwPEKind,
        DWORD* pdwMAchine) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetPEKind(pdwPEKind, pdwMAchine);
    }

    STDMETHOD(GetVersionString)(
      _Out_writes_to_opt_(ccBufSize, *pccBufSize)
        LPWSTR      pwzBuf,
        DWORD       ccBufSize,
        DWORD       *pccBufSize) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetVersionString(pwzBuf, ccBufSize, pccBufSize);
    }

    STDMETHOD(EnumMethodSpecs)(
        HCORENUM    *phEnum,
        mdToken      tk,
        mdMethodSpec rMethodSpecs[],
        ULONG       cMax,
        ULONG       *pcMethodSpecs) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumMethodSpecs(phEnum, tk, rMethodSpecs, cMax, pcMethodSpecs);
    }

public: // IMetaDataAssemblyImport
    STDMETHOD(GetAssemblyProps)(
        mdAssembly  mda,
        void const** ppbPublicKey,
        ULONG* pcbPublicKey,
        ULONG* pulHashAlgId,
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR  szName,
        ULONG       cchName,
        ULONG* pchName,
        ASSEMBLYMETADATA* pMetaData,
        DWORD* pdwAssemblyFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetAssemblyProps(mda, ppbPublicKey, pcbPublicKey, pulHashAlgId, szName, cchName, pchName, pMetaData, pdwAssemblyFlags);
    }

    STDMETHOD(GetAssemblyRefProps)(
        mdAssemblyRef mdar,
        void const** ppbPublicKeyOrToken,
        ULONG* pcbPublicKeyOrToken,
        _Out_writes_to_opt_(cchName, *pchName)LPWSTR szName,
        ULONG       cchName,
        ULONG* pchName,
        ASSEMBLYMETADATA* pMetaData,
        void const** ppbHashValue,
        ULONG* pcbHashValue,
        DWORD* pdwAssemblyRefFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetAssemblyRefProps(mdar, ppbPublicKeyOrToken, pcbPublicKeyOrToken, szName, cchName, pchName, pMetaData, ppbHashValue, pcbHashValue, pdwAssemblyRefFlags);
    }

    STDMETHOD(GetFileProps)(
        mdFile      mdf,
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR      szName,
        ULONG       cchName,
        ULONG* pchName,
        void const** ppbHashValue,
        ULONG* pcbHashValue,
        DWORD* pdwFileFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetFileProps(mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags);
    }

    STDMETHOD(GetExportedTypeProps)(
        mdExportedType   mdct,
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR      szName,
        ULONG       cchName,
        ULONG* pchName,
        mdToken* ptkImplementation,
        mdTypeDef* ptkTypeDef,
        DWORD* pdwExportedTypeFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetExportedTypeProps(mdct, szName, cchName, pchName, ptkImplementation, ptkTypeDef, pdwExportedTypeFlags);
    }

    STDMETHOD(GetManifestResourceProps)(
        mdManifestResource  mdmr,
        _Out_writes_to_opt_(cchName, *pchName)LPWSTR      szName,
        ULONG       cchName,
        ULONG* pchName,
        mdToken* ptkImplementation,
        DWORD* pdwOffset,
        DWORD* pdwResourceFlags) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetManifestResourceProps(mdmr, szName, cchName, pchName, ptkImplementation, pdwOffset, pdwResourceFlags);
    }

    STDMETHOD(EnumAssemblyRefs)(
        HCORENUM* phEnum,
        mdAssemblyRef rAssemblyRefs[],
        ULONG       cMax,
        ULONG* pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumAssemblyRefs(phEnum, rAssemblyRefs, cMax, pcTokens);
    }

    STDMETHOD(EnumFiles)(
        HCORENUM* phEnum,
        mdFile      rFiles[],
        ULONG       cMax,
        ULONG* pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumFiles(phEnum, rFiles, cMax, pcTokens);
    }

    STDMETHOD(EnumExportedTypes)(
        HCORENUM* phEnum,
        mdExportedType   rExportedTypes[],
        ULONG       cMax,
        ULONG* pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumExportedTypes(phEnum, rExportedTypes, cMax, pcTokens);
    }

    STDMETHOD(EnumManifestResources)(
        HCORENUM* phEnum,
        mdManifestResource  rManifestResources[],
        ULONG       cMax,
        ULONG* pcTokens) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->EnumManifestResources(phEnum, rManifestResources, cMax, pcTokens);
    }

    STDMETHOD(GetAssemblyFromScope)(
        mdAssembly* ptkAssembly) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->GetAssemblyFromScope(ptkAssembly);
    }

    STDMETHOD(FindExportedTypeByName)(
        LPCWSTR     szName,
        mdToken     mdtExportedType,
        mdExportedType* ptkExportedType) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindExportedTypeByName(szName, mdtExportedType, ptkExportedType);
    }

    STDMETHOD(FindManifestResourceByName)(
        LPCWSTR     szName,
        mdManifestResource* ptkManifestResource) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindManifestResourceByName(szName, ptkManifestResource);
    
    }

    STDMETHOD(FindAssembliesByName)(
        LPCWSTR  szAppBase,
        LPCWSTR  szPrivateBin,
        LPCWSTR  szAssemblyName,
        IUnknown* ppIUnk[],
        ULONG    cMax,
        ULONG* pcAssemblies) override
    {
        std::lock_guard<pal::ReadLock> lock { this->_lock.GetReadLock() };
        return _import->FindAssembliesByName(szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies);
    }

public: // IMetaDataEmit
    STDMETHOD(SetModuleProps)(
        LPCWSTR     szName) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetModuleProps(szName);
    }

    STDMETHOD(Save)(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->Save(szFile, dwSaveFlags);
    }

    STDMETHOD(SaveToStream)(
        IStream     *pIStream,
        DWORD       dwSaveFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SaveToStream(pIStream, dwSaveFlags);
    
    }

    STDMETHOD(GetSaveSize)(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->GetSaveSize(fSave, pdwSaveSize);
    }

    STDMETHOD(DefineTypeDef)(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   *ptd) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineTypeDef(szTypeDef, dwTypeDefFlags, tkExtends, rtkImplements, ptd);
    }

    STDMETHOD(DefineNestedType)(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   tdEncloser,
        mdTypeDef   *ptd) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineNestedType(szTypeDef, dwTypeDefFlags, tkExtends, rtkImplements, tdEncloser, ptd);
    }

    STDMETHOD(SetHandler)(
        IUnknown    *pUnk) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetHandler(pUnk);
    }

    STDMETHOD(DefineMethod)(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwMethodFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags,
        mdMethodDef *pmd) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineMethod(td, szName, dwMethodFlags, pvSigBlob, cbSigBlob, ulCodeRVA, dwImplFlags, pmd);
    }

    STDMETHOD(DefineMethodImpl)(
        mdTypeDef   td,
        mdToken     tkBody,
        mdToken     tkDecl) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineMethodImpl(td, tkBody, tkDecl);
    }

    STDMETHOD(DefineTypeRefByName)(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineTypeRefByName(tkResolutionScope, szName, ptr);
    }

    STDMETHOD(DefineImportType)(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdTypeDef   tdImport,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdTypeRef   *ptr) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineImportType(pAssemImport, pbHashValue, cbHashValue, pImport, tdImport, pAssemEmit, ptr);
    }

    STDMETHOD(DefineMemberRef)(
        mdToken     tkImport,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineMemberRef(tkImport, szName, pvSigBlob, cbSigBlob, pmr);
    }

    STDMETHOD(DefineImportMember)(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdToken     mbMember,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdToken     tkParent,
        mdMemberRef *pmr) override
    {
        // No need to lock this function. The implementation will lock when necessary
        return ::DefineImportMember(this, pAssemImport, pbHashValue, cbHashValue, pImport, mbMember, pAssemEmit, tkParent, pmr);
    }

    STDMETHOD(DefineEvent) (
        mdTypeDef   td,
        LPCWSTR     szEvent,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[],
        mdEvent     *pmdEvent) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineEvent(td, szEvent, dwEventFlags, tkEventType, mdAddOn, mdRemoveOn, mdFire, rmdOtherMethods, pmdEvent);
    }

    STDMETHOD(SetClassLayout) (
        mdTypeDef   td,
        DWORD       dwPackSize,
        COR_FIELD_OFFSET rFieldOffsets[],
        ULONG       ulClassSize) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetClassLayout(td, dwPackSize, rFieldOffsets, ulClassSize);
    }

    STDMETHOD(DeleteClassLayout) (
        mdTypeDef   td) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DeleteClassLayout(td);
    }

    STDMETHOD(SetFieldMarshal) (
        mdToken     tk,
        PCCOR_SIGNATURE pvNativeType,
        ULONG       cbNativeType) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetFieldMarshal(tk, pvNativeType, cbNativeType);
    }

    STDMETHOD(DeleteFieldMarshal) (
        mdToken     tk) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DeleteFieldMarshal(tk);
    }

    STDMETHOD(DefinePermissionSet) (
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefinePermissionSet(tk, dwAction, pvPermission, cbPermission, ppm);
    }

    STDMETHOD(SetRVA)(
        mdMethodDef md,
        ULONG       ulRVA) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetRVA(md, ulRVA);
    }

    STDMETHOD(GetTokenFromSig)(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdSignature *pmsig) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->GetTokenFromSig(pvSig, cbSig, pmsig);
    }

    STDMETHOD(DefineModuleRef)(
        LPCWSTR     szName,
        mdModuleRef *pmur) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineModuleRef(szName, pmur);
    }

    STDMETHOD(SetParent)(
        mdMemberRef mr,
        mdToken     tk) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetParent(mr, tk);
    }

    STDMETHOD(GetTokenFromTypeSpec)(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdTypeSpec *ptypespec) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->GetTokenFromTypeSpec(pvSig, cbSig, ptypespec);
    }

    STDMETHOD(SaveToMemory)(
        void        *pbData,
        ULONG       cbData) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SaveToMemory(pbData, cbData);
    }

    STDMETHOD(DefineUserString)(
        LPCWSTR szString,
        ULONG       cchString,
        mdString    *pstk) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineUserString(szString, cchString, pstk);
    }

    STDMETHOD(DeleteToken)(
        mdToken     tkObj) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DeleteToken(tkObj);
    }

    STDMETHOD(SetMethodProps)(
        mdMethodDef md,
        DWORD       dwMethodFlags,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetMethodProps(md, dwMethodFlags, ulCodeRVA, dwImplFlags);
    }

    STDMETHOD(SetTypeDefProps)(
        mdTypeDef   td,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[]) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetTypeDefProps(td, dwTypeDefFlags, tkExtends, rtkImplements);
    }

    STDMETHOD(SetEventProps)(
        mdEvent     ev,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[]) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetEventProps(ev, dwEventFlags, tkEventType, mdAddOn, mdRemoveOn, mdFire, rmdOtherMethods);
    }

    STDMETHOD(SetPermissionSetProps)(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetPermissionSetProps(tk, dwAction, pvPermission, cbPermission, ppm);
    }

    STDMETHOD(DefinePinvokeMap)(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefinePinvokeMap(tk, dwMappingFlags, szImportName, mrImportDLL);
    }

    STDMETHOD(SetPinvokeMap)(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetPinvokeMap(tk, dwMappingFlags, szImportName, mrImportDLL);
    }

    STDMETHOD(DeletePinvokeMap)(
        mdToken     tk) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DeletePinvokeMap(tk);
    }


    STDMETHOD(DefineCustomAttribute)(
        mdToken     tkOwner,
        mdToken     tkCtor,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute,
        mdCustomAttribute *pcv) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineCustomAttribute(tkOwner, tkCtor, pCustomAttribute, cbCustomAttribute, pcv);
    }

    STDMETHOD(SetCustomAttributeValue)(
        mdCustomAttribute pcv,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetCustomAttributeValue(pcv, pCustomAttribute, cbCustomAttribute);
    }

    STDMETHOD(DefineField)(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwFieldFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdFieldDef  *pmd) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineField(td, szName, dwFieldFlags, pvSigBlob, cbSigBlob, dwCPlusTypeFlag, pValue, cchValue, pmd);
    }

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
        mdProperty  *pmdProp) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineProperty(td, szProperty, dwPropFlags, pvSig, cbSig, dwCPlusTypeFlag, pValue, cchValue, mdSetter, mdGetter, rmdOtherMethods, pmdProp);
    }

    STDMETHOD(DefineParam)(
        mdMethodDef md,
        ULONG       ulParamSeq,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdParamDef  *ppd) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineParam(md, ulParamSeq, szName, dwParamFlags, dwCPlusTypeFlag, pValue, cchValue, ppd);
    }

    STDMETHOD(SetFieldProps)(
        mdFieldDef  fd,
        DWORD       dwFieldFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetFieldProps(fd, dwFieldFlags, dwCPlusTypeFlag, pValue, cchValue);
    }

    STDMETHOD(SetPropertyProps)(
        mdProperty  pr,
        DWORD       dwPropFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[]) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetPropertyProps(pr, dwPropFlags, dwCPlusTypeFlag, pValue, cchValue, mdSetter, mdGetter, rmdOtherMethods);
    }

    STDMETHOD(SetParamProps)(
        mdParamDef  pd,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetParamProps(pd, szName, dwParamFlags, dwCPlusTypeFlag, pValue, cchValue);
    }


    STDMETHOD(DefineSecurityAttributeSet)(
        mdToken     tkObj,
        COR_SECATTR rSecAttrs[],
        ULONG       cSecAttrs,
        ULONG       *pulErrorAttr) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineSecurityAttributeSet(tkObj, rSecAttrs, cSecAttrs, pulErrorAttr);
    }

    STDMETHOD(ApplyEditAndContinue)(
        IUnknown    *pImport) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->ApplyEditAndContinue(pImport);
    }

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
        ULONG       *pcbTranslatedSig) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->TranslateSigWithScope(pAssemImport, pbHashValue, cbHashValue, import, pbSigBlob, cbSigBlob, pAssemEmit, emit, pvTranslatedSig, cbTranslatedSigMax, pcbTranslatedSig);
    }

    STDMETHOD(SetMethodImplFlags)(
        mdMethodDef md,
        DWORD       dwImplFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetMethodImplFlags(md, dwImplFlags);
    }

    STDMETHOD(SetFieldRVA)(
        mdFieldDef  fd,
        ULONG       ulRVA) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetFieldRVA(fd, ulRVA);
    }

    STDMETHOD(Merge)(
        IMetaDataImport *pImport,
        IMapToken   *pHostMapToken,
        IUnknown    *pHandler) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->Merge(pImport, pHostMapToken, pHandler);
    }

    STDMETHOD(MergeEnd)() override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->MergeEnd();
    }

public: // IMetaDataEmit2
    STDMETHOD(DefineMethodSpec)(
        mdToken     tkParent,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodSpec *pmi) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineMethodSpec(tkParent, pvSigBlob, cbSigBlob, pmi);
    }

    STDMETHOD(GetDeltaSaveSize)(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->GetDeltaSaveSize(fSave, pdwSaveSize);
    }

    STDMETHOD(SaveDelta)(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SaveDelta(szFile, dwSaveFlags);
    }

    STDMETHOD(SaveDeltaToStream)(
        IStream     *pIStream,
        DWORD       dwSaveFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SaveDeltaToStream(pIStream, dwSaveFlags);
    }

    STDMETHOD(SaveDeltaToMemory)(
        void        *pbData,
        ULONG       cbData) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SaveDeltaToMemory(pbData, cbData);
    }

    STDMETHOD(DefineGenericParam)(
        mdToken      tk,
        ULONG        ulParamSeq,
        DWORD        dwParamFlags,
        LPCWSTR      szname,
        DWORD        reserved,
        mdToken      rtkConstraints[],
        mdGenericParam *pgp) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineGenericParam(tk, ulParamSeq, dwParamFlags, szname, reserved, rtkConstraints, pgp);
    }

    STDMETHOD(SetGenericParamProps)(
        mdGenericParam gp,
        DWORD        dwParamFlags,
        LPCWSTR      szName,
        DWORD        reserved,
        mdToken      rtkConstraints[]) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetGenericParamProps(gp, dwParamFlags, szName, reserved, rtkConstraints);
    }

    STDMETHOD(ResetENCLog)() override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->ResetENCLog();
    }

public: // IMetaDataAssemblyEmit
    STDMETHOD(DefineAssembly)(
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags,
        mdAssembly  *pma) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineAssembly(pbPublicKey, cbPublicKey, ulHashAlgId, szName, pMetaData, dwAssemblyFlags, pma);
    }

    STDMETHOD(DefineAssemblyRef)(
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags,
        mdAssemblyRef *pmdar) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineAssemblyRef(pbPublicKeyOrToken, cbPublicKeyOrToken, szName, pMetaData, pbHashValue, cbHashValue, dwAssemblyRefFlags, pmdar);
    }

    STDMETHOD(DefineFile)(
        LPCWSTR     szName,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags,
        mdFile      *pmdf) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineFile(szName, pbHashValue, cbHashValue, dwFileFlags, pmdf);
    }

    STDMETHOD(DefineExportedType)(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags,
        mdExportedType   *pmdct) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineExportedType(szName, tkImplementation, tkTypeDef, dwExportedTypeFlags, pmdct);
    }

    STDMETHOD(DefineManifestResource)(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags,
        mdManifestResource  *pmdmr) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->DefineManifestResource(szName, tkImplementation, dwOffset, dwResourceFlags, pmdmr);
    }

    STDMETHOD(SetAssemblyProps)(
        mdAssembly  pma,
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetAssemblyProps(pma, pbPublicKey, cbPublicKey, ulHashAlgId, szName, pMetaData, dwAssemblyFlags);
    }

    STDMETHOD(SetAssemblyRefProps)(
        mdAssemblyRef ar,
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetAssemblyRefProps(ar, pbPublicKeyOrToken, cbPublicKeyOrToken, szName, pMetaData, pbHashValue, cbHashValue, dwAssemblyRefFlags);
    }

    STDMETHOD(SetFileProps)(
        mdFile      file,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetFileProps(file, pbHashValue, cbHashValue, dwFileFlags);
    }

    STDMETHOD(SetExportedTypeProps)(
        mdExportedType   ct,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetExportedTypeProps(ct, tkImplementation, tkTypeDef, dwExportedTypeFlags);
    }

    STDMETHOD(SetManifestResourceProps)(
        mdManifestResource  mr,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags) override
    {
        std::lock_guard<pal::WriteLock> lock { this->_lock.GetWriteLock() };
        return _emit->SetManifestResourceProps(mr, tkImplementation, dwOffset, dwResourceFlags);
    }
};

#endif // _SRC_INTERFACES_THREADSAFE_HPP_