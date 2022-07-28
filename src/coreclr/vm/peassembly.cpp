// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEAssembly.cpp
//

// --------------------------------------------------------------------------------


#include "common.h"
#include "peassembly.h"
#include "eecontract.h"
#include "eeconfig.h"
#include "eventtrace.h"
#include "dbginterface.h"
#include "peimagelayout.inl"
#include "dlwrap.h"
#include "invokeutil.h"
#include "strongnameinternal.h"

#include "../binder/inc/applicationcontext.hpp"

#include "assemblybinderutil.h"
#include "../binder/inc/assemblybindercommon.hpp"

#include "sha1.h"


#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------------------------------
// Catch attempts to load x64 assemblies on x86, etc.
//-----------------------------------------------------------------------------------------------------
static void ValidatePEFileMachineType(PEAssembly *pPEAssembly)
{
    STANDARD_VM_CONTRACT;

    if (pPEAssembly->IsDynamic())
        return;    // PEFiles for ReflectionEmit assemblies don't cache the machine type.

    DWORD peKind;
    DWORD actualMachineType;
    pPEAssembly->GetPEKindAndMachine(&peKind, &actualMachineType);

    if (actualMachineType == IMAGE_FILE_MACHINE_I386 && ((peKind & (peILonly | pe32BitRequired)) == peILonly))
        return;    // Image is marked CPU-agnostic.

    if (actualMachineType != IMAGE_FILE_MACHINE_NATIVE && actualMachineType != IMAGE_FILE_MACHINE_NATIVE_NI)
    {
#ifdef TARGET_AMD64
        // v4.0 64-bit compatibility workaround. The 64-bit v4.0 CLR's Reflection.Load(byte[]) api does not detect cpu-matches. We should consider fixing that in
        // the next SxS release. In the meantime, this bypass will retain compat for 64-bit v4.0 CLR for target platforms that existed at the time.
        //
        // Though this bypass kicks in for all Load() flavors, the other Load() flavors did detect cpu-matches through various other code paths that still exist.
        // Or to put it another way, this #ifdef makes the (4.5 only) ValidatePEFileMachineType() a NOP for x64, hence preserving 4.0 compatibility.
        if (actualMachineType == IMAGE_FILE_MACHINE_I386 || actualMachineType == IMAGE_FILE_MACHINE_IA64)
            return;
#endif // BIT64_

        // Image has required machine that doesn't match the CLR.
        StackSString name;
        pPEAssembly->GetDisplayName(name);

        COMPlusThrow(kBadImageFormatException, IDS_CLASSLOAD_WRONGCPU, name.GetUnicode());
    }

    return;   // If we got here, all is good.
}

void PEAssembly::EnsureLoaded()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        POSTCONDITION(IsLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    if (IsDynamic())
        RETURN;

    // Ensure that loaded layout is available.
    PEImageLayout* pLayout = GetPEImage()->GetOrCreateLayout(PEImageLayout::LAYOUT_LOADED);
    if (pLayout == NULL)
    {
        EEFileLoadException::Throw(this, COR_E_BADIMAGEFORMAT, NULL);
    }

    // Catch attempts to load x64 assemblies on x86, etc.
    ValidatePEFileMachineType(this);

#if !defined(TARGET_64BIT)
    if (!GetPEImage()->Has32BitNTHeaders())
    {
        // Tried to load 64-bit assembly on 32-bit platform.
        EEFileLoadException::Throw(this, COR_E_BADIMAGEFORMAT, NULL);
    }
#endif

    RETURN;
}

// ------------------------------------------------------------
// Identity
// ------------------------------------------------------------

BOOL PEAssembly::Equals(PEAssembly *pPEAssembly)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pPEAssembly));
        GC_NOTRIGGER;
        NOTHROW;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Same object is equal
    if (pPEAssembly == this)
        return TRUE;

    // Different host assemblies cannot be equal unless they are associated with the same host binder
    // It's ok if only one has a host binder because multiple threads can race to load the same assembly
    // and that may cause temporary candidate PEAssembly objects that never get bound to a host assembly
    // because another thread beats it; the losing thread will pick up the PEAssembly in the cache.
    if (pPEAssembly->HasHostAssembly() && this->HasHostAssembly())
    {
        AssemblyBinder* otherBinder = pPEAssembly->GetHostAssembly()->GetBinder();
        AssemblyBinder* thisBinder = this->GetHostAssembly()->GetBinder();

        if (otherBinder != thisBinder || otherBinder == NULL)
            return FALSE;
    }

    // Same image is equal
    if (m_PEImage != NULL && pPEAssembly->m_PEImage != NULL
        && m_PEImage->Equals(pPEAssembly->m_PEImage))
        return TRUE;

    return FALSE;
}

BOOL PEAssembly::Equals(PEImage *pImage)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pImage));
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Same image ==> equal
    if (pImage == m_PEImage)
        return TRUE;

    // Equal image ==> equal
    if (m_PEImage != NULL
        && m_PEImage->Equals(pImage))
        return TRUE;

    return FALSE;
}

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

void PEAssembly::GetPathOrCodeBase(SString &result)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_PEImage != NULL && !m_PEImage->GetPath().IsEmpty())
    {
        result.Set(m_PEImage->GetPath());
    }
    else
    {
        GetCodeBase(result);
    }
}

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

PTR_CVOID PEAssembly::GetMetadata(COUNT_T *pSize)
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(pSize, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (IsDynamic()
         || !GetPEImage()->HasNTHeaders()
         || !GetPEImage()->HasCorHeader())
    {
        if (pSize != NULL)
            *pSize = 0;
        RETURN NULL;
    }
    else
    {
        RETURN GetPEImage()->GetMetadata(pSize);
    }
}
#endif // #ifndef DACCESS_COMPILE

PTR_CVOID PEAssembly::GetLoadedMetadata(COUNT_T *pSize)
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(pSize, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (!HasLoadedPEImage()
         || !GetLoadedLayout()->HasNTHeaders()
         || !GetLoadedLayout()->HasCorHeader())
    {
        if (pSize != NULL)
            *pSize = 0;
        RETURN NULL;
    }
    else
    {
        RETURN GetLoadedLayout()->GetMetadata(pSize);
    }
}

TADDR PEAssembly::GetIL(RVA il)
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(il != 0);
        PRECONDITION(!IsDynamic());
#ifndef DACCESS_COMPILE
        PRECONDITION(HasLoadedPEImage());
#endif
        POSTCONDITION(RETVAL != NULL);
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    PEImageLayout *image = NULL;
    image = GetLoadedLayout();

#ifndef DACCESS_COMPILE
    // Verify that the IL blob is valid before giving it out
    if (!image->CheckILMethod(il))
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL_RANGE);
#endif

    RETURN image->GetRvaData(il);
}

#ifndef DACCESS_COMPILE

void PEAssembly::OpenImporter()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Make sure internal MD is in RW format.
    ConvertMDInternalToReadWrite();

    IMetaDataImport2 *pIMDImport = NULL;
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetMDImport(),
                                                       IID_IMetaDataImport2,
                                                       (void **)&pIMDImport));

    // Atomically swap it into the field (release it if we lose the race)
    if (InterlockedCompareExchangeT(&m_pImporter, pIMDImport, NULL) != NULL)
        pIMDImport->Release();
}

void PEAssembly::ConvertMDInternalToReadWrite()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(EX_THROW(EEMessageException, (E_OUTOFMEMORY)););
    }
    CONTRACTL_END;

    IMDInternalImport *pOld;            // Old (current RO) value of internal import.
    IMDInternalImport *pNew = NULL;     // New (RW) value of internal import.

    // Take a local copy of *ppImport.  This may be a pointer to an RO
    //  or to an RW MDInternalXX.
    pOld = m_pMDImport;
    IMetaDataImport *pIMDImport = m_pImporter;
    if (pIMDImport != NULL)
    {
        HRESULT hr = GetMetaDataInternalInterfaceFromPublic(pIMDImport, IID_IMDInternalImport, (void **)&pNew);
        if (FAILED(hr))
        {
            EX_THROW(EEMessageException, (hr));
        }
        if (pNew == pOld)
        {
            pNew->Release();
            return;
        }
    }
    else
    {
        // If an RO, convert to an RW, return S_OK.  If already RW, no conversion
        //  needed, return S_FALSE.
        HRESULT hr = ConvertMDInternalImport(pOld, &pNew);

        if (FAILED(hr))
        {
            EX_THROW(EEMessageException, (hr));
        }

        // If no conversion took place, don't change pointers.
        if (hr == S_FALSE)
            return;
    }

    // Swap the pointers in a thread safe manner.  If the contents of *ppImport
    //  equals pOld then no other thread got here first, and the old contents are
    //  replaced with pNew.  The old contents are returned.
    if (InterlockedCompareExchangeT(&m_pMDImport, pNew, pOld) == pOld)
    {
        //if the debugger queries, it will now see that we have RW metadata
        m_MDImportIsRW_Debugger_Use_Only = TRUE;

        // Swapped -- get the metadata to hang onto the old Internal import.
        HRESULT hr=m_pMDImport->SetUserContextData(pOld);
        _ASSERTE(SUCCEEDED(hr)||!"Leaking old MDImport");
        IfFailThrow(hr);
    }
    else
    {   // Some other thread finished first.  Just free the results of this conversion.
        pNew->Release();
    }
}

void PEAssembly::OpenMDImport()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_pMDImport != NULL)
        return;
    if (!IsDynamic()
        && GetPEImage()->HasNTHeaders()
            && GetPEImage()->HasCorHeader())
    {
        m_pMDImport=GetPEImage()->GetMDImport();
    }
    else
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    _ASSERTE(m_pMDImport);
    m_pMDImport->AddRef();
}

void PEAssembly::OpenEmitter()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Make sure internal MD is in RW format.
    ConvertMDInternalToReadWrite();

    IMetaDataEmit *pIMDEmit = NULL;
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetMDImport(),
                                                       IID_IMetaDataEmit,
                                                       (void **)&pIMDEmit));

    // Atomically swap it into the field (release it if we lose the race)
    if (InterlockedCompareExchangeT(&m_pEmitter, pIMDEmit, NULL) != NULL)
        pIMDEmit->Release();
}

// ------------------------------------------------------------
// PE file access
// ------------------------------------------------------------

// Note that most of these APIs are currently passed through
// to the main image.  However, in the near future they will
// be rerouted to the native image in the prejitted case so
// we can avoid using the original IL image.

#endif //!DACCESS_COMPILE

#ifndef DACCESS_COMPILE

// ------------------------------------------------------------
// Resource access
// ------------------------------------------------------------

void PEAssembly::GetEmbeddedResource(DWORD dwOffset, DWORD *cbResource, PBYTE *pbInMemoryResource)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END;

    PEImage* image = GetPEImage();
    PEImageLayout* theImage = image->GetOrCreateLayout(PEImageLayout::LAYOUT_ANY);
    if (!theImage->CheckResource(dwOffset))
        ThrowHR(COR_E_BADIMAGEFORMAT);

    COUNT_T size;
    const void *resource = theImage->GetResource(dwOffset, &size);

    *cbResource = size;
    *pbInMemoryResource = (PBYTE) resource;
}

// ------------------------------------------------------------
// File loading
// ------------------------------------------------------------

PEAssembly* PEAssembly::LoadAssembly(mdAssemblyRef kAssemblyRef)
{
    CONTRACT(PEAssembly *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    IMDInternalImport* pImport = GetMDImport();
    if (((TypeFromToken(kAssemblyRef) != mdtAssembly) &&
         (TypeFromToken(kAssemblyRef) != mdtAssemblyRef)) ||
        (!pImport->IsValidToken(kAssemblyRef)))
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    AssemblySpec spec;

    spec.InitializeSpec(kAssemblyRef, pImport, GetAppDomain()->FindAssembly(this));

    RETURN GetAppDomain()->BindAssemblySpec(&spec, TRUE);
}


BOOL PEAssembly::GetResource(LPCSTR szName, DWORD *cbResource,
                             PBYTE *pbInMemoryResource, DomainAssembly** pAssemblyRef,
                             LPCSTR *szFileName, DWORD *dwLocation,
                             BOOL fSkipRaiseResolveEvent, DomainAssembly* pDomainAssembly, AppDomain* pAppDomain)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;


    mdToken            mdLinkRef;
    DWORD              dwResourceFlags;
    DWORD              dwOffset;
    mdManifestResource mdResource;
    Assembly*          pAssembly = NULL;
    PEAssembly*        pPEAssembly = NULL;
    IMDInternalImport* pImport = GetMDImport();
    if (SUCCEEDED(pImport->FindManifestResourceByName(szName, &mdResource)))
    {
        pPEAssembly = this;
        IfFailThrow(pImport->GetManifestResourceProps(
            mdResource,
            NULL,           //&szName,
            &mdLinkRef,
            &dwOffset,
            &dwResourceFlags));
    }
    else
    {
        if (fSkipRaiseResolveEvent || pAppDomain == NULL)
            return FALSE;

        DomainAssembly* pParentAssembly = GetAppDomain()->FindAssembly(this);
        pAssembly = pAppDomain->RaiseResourceResolveEvent(pParentAssembly, szName);
        if (pAssembly == NULL)
            return FALSE;

        pDomainAssembly = pAssembly->GetDomainAssembly();
        pPEAssembly = pDomainAssembly->GetPEAssembly();

        if (FAILED(pAssembly->GetMDImport()->FindManifestResourceByName(
            szName,
            &mdResource)))
        {
            return FALSE;
        }

        if (dwLocation != 0)
        {
            if (pAssemblyRef != NULL)
                *pAssemblyRef = pDomainAssembly;

            *dwLocation = *dwLocation | 2; // ResourceLocation.containedInAnotherAssembly
        }
        IfFailThrow(pPEAssembly->GetMDImport()->GetManifestResourceProps(
            mdResource,
            NULL,           //&szName,
            &mdLinkRef,
            &dwOffset,
            &dwResourceFlags));
    }


    switch(TypeFromToken(mdLinkRef)) {
    case mdtAssemblyRef:
        {
            if (pDomainAssembly == NULL)
                return FALSE;

            AssemblySpec spec;
            spec.InitializeSpec(mdLinkRef, GetMDImport(), pDomainAssembly);
            pDomainAssembly = spec.LoadDomainAssembly(FILE_LOADED);

            if (dwLocation) {
                if (pAssemblyRef)
                    *pAssemblyRef = pDomainAssembly;

                *dwLocation = *dwLocation | 2; // ResourceLocation.containedInAnotherAssembly
            }

            return pDomainAssembly->GetResource(szName,
                                                cbResource,
                                                pbInMemoryResource,
                                                pAssemblyRef,
                                                szFileName,
                                                dwLocation,
                                                fSkipRaiseResolveEvent);
        }

    case mdtFile:
        if (mdLinkRef == mdFileNil)
        {
            // The resource is embedded in the manifest file

            if (dwLocation) {
                *dwLocation = *dwLocation | 5; // ResourceLocation.embedded |

                                               // ResourceLocation.containedInManifestFile
                return TRUE;
            }

            pPEAssembly->GetEmbeddedResource(dwOffset, cbResource, pbInMemoryResource);

            return TRUE;
        }
        return FALSE;

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_IN_MANIFESTRES);
    }
}

void PEAssembly::GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pdwKind != NULL && pdwMachine != NULL);
    if (IsDynamic())
    {
        *pdwKind = 0;
        *pdwMachine = 0;
        return;
    }

    GetPEImage()->GetPEKindAndMachine(pdwKind, pdwMachine);
    return;
}

ULONG PEAssembly::GetPEImageTimeDateStamp()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return GetLoadedLayout()->GetTimeDateStamp();
}

#ifndef DACCESS_COMPILE

PEAssembly::PEAssembly(
                BINDER_SPACE::Assembly* pBindResultInfo,
                IMetaDataEmit* pEmit,
                BOOL isSystem,
                PEImage * pPEImage /*= NULL*/,
                BINDER_SPACE::Assembly * pHostAssembly /*= NULL*/)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(pEmit, NULL_OK));
        PRECONDITION(pBindResultInfo == NULL || pPEImage == NULL);
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

#if _DEBUG
    m_pDebugName = NULL;
#endif
    m_PEImage = NULL;
    m_MDImportIsRW_Debugger_Use_Only = FALSE;
    m_pMDImport = NULL;
    m_pImporter = NULL;
    m_pEmitter = NULL;
    m_refCount = 1;
    m_isSystem = isSystem;
    m_pHostAssembly = nullptr;
    m_pFallbackBinder = nullptr;

    pPEImage = pBindResultInfo ? pBindResultInfo->GetPEImage() : pPEImage;
    if (pPEImage)
    {
        _ASSERTE(pPEImage->CheckUniqueInstance());
        pPEImage->AddRef();
        // We require an open layout for the file.
        // Most likely we have one already, just make sure we have one.
        pPEImage->GetOrCreateLayout(PEImageLayout::LAYOUT_ANY);
        m_PEImage = pPEImage;
    }

    // Open metadata eagerly to minimize failure windows
    if (pEmit == NULL)
        OpenMDImport(); //constructor, cannot race with anything
    else
    {
        IfFailThrow(GetMetaDataInternalInterfaceFromPublic(pEmit, IID_IMDInternalImport,
                                                           (void **)&m_pMDImport));
        m_pEmitter = pEmit;
        pEmit->AddRef();
        m_MDImportIsRW_Debugger_Use_Only = TRUE;
    }

    // m_pMDImport can be external
    // Make sure this is an assembly
    if (!m_pMDImport->IsValidToken(TokenFromRid(1, mdtAssembly)))
        ThrowHR(COR_E_ASSEMBLYEXPECTED);

    // Verify name eagerly
    LPCUTF8 szName = GetSimpleName();
    if (!*szName)
    {
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_EMPTY_ASSEMDEF_NAME);
    }

    // Set the host assembly and binding context as the AssemblySpec initialization
    // for CoreCLR will expect to have it set.
    if (pHostAssembly != nullptr)
    {
        m_pHostAssembly = clr::SafeAddRef(pHostAssembly);
    }

    if(pBindResultInfo != nullptr)
    {
        // Cannot have both pHostAssembly and a coreclr based bind
        _ASSERTE(pHostAssembly == nullptr);
        pBindResultInfo = clr::SafeAddRef(pBindResultInfo);
        m_pHostAssembly = pBindResultInfo;
    }

#if _DEBUG
    GetPathOrCodeBase(m_debugName);
    m_debugName.Normalize();
    m_pDebugName = m_debugName;
#endif
}
#endif // !DACCESS_COMPILE


PEAssembly *PEAssembly::Open(
    PEImage *          pPEImageIL,
    BINDER_SPACE::Assembly * pHostAssembly)
{
    STANDARD_VM_CONTRACT;

    PEAssembly * pPEAssembly = new PEAssembly(
        nullptr,        // BindResult
        nullptr,        // IMetaDataEmit
        FALSE,          // isSystem
        pPEImageIL,
        pHostAssembly);

    return pPEAssembly;
}


PEAssembly::~PEAssembly()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS; // Fusion uses crsts on AddRef/Release
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    if (m_pImporter != NULL)
    {
        m_pImporter->Release();
        m_pImporter = NULL;
    }

    if (m_pEmitter != NULL)
    {
        m_pEmitter->Release();
        m_pEmitter = NULL;
    }

    if (m_pMDImport != NULL)
    {
        m_pMDImport->Release();
        m_pMDImport = NULL;
    }

    if (m_PEImage != NULL)
        m_PEImage->Release();

    if (m_pHostAssembly != NULL)
        m_pHostAssembly->Release();
}

/* static */
PEAssembly *PEAssembly::OpenSystem()
{
    STANDARD_VM_CONTRACT;

    PEAssembly *result = NULL;

    EX_TRY
    {
        result = DoOpenSystem();
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context

        if (!ex->IsTransient())
            EEFileLoadException::Throw(SystemDomain::System()->BaseLibrary(), ex->GetHR(), ex);
    }
    EX_END_HOOK;
    return result;
}

/* static */
PEAssembly *PEAssembly::DoOpenSystem()
{
    CONTRACT(PEAssembly *)
    {
        POSTCONDITION(CheckPointer(RETVAL));
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    ETWOnStartup (FusionBinding_V1, FusionBindingEnd_V1);
    ReleaseHolder<BINDER_SPACE::Assembly> pBoundAssembly;
    IfFailThrow(GetAppDomain()->GetDefaultBinder()->BindToSystem(&pBoundAssembly));

    RETURN new PEAssembly(pBoundAssembly, NULL, TRUE);
}

PEAssembly* PEAssembly::Open(BINDER_SPACE::Assembly* pBindResult)
{
    return new PEAssembly(pBindResult,NULL,/*isSystem*/ false);
};

/* static */
PEAssembly *PEAssembly::Create(IMetaDataAssemblyEmit *pAssemblyEmit)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(pAssemblyEmit));
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Set up the metadata pointers in the PEAssembly. (This is the only identity
    // we have.)
    SafeComHolder<IMetaDataEmit> pEmit;
    pAssemblyEmit->QueryInterface(IID_IMetaDataEmit, (void **)&pEmit);
    RETURN new PEAssembly(NULL, pEmit, FALSE);
}

#endif // #ifndef DACCESS_COMPILE


#ifndef DACCESS_COMPILE

// Supports implementation of the legacy Assembly.CodeBase property.
// Returns false if the assembly was loaded from a bundle, true otherwise
BOOL PEAssembly::GetCodeBase(SString &result)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    PEImage* ilImage = GetPEImage();
    if (ilImage != NULL && !ilImage->IsInBundle())
    {
        // All other cases use the file path.
        result.Set(ilImage->GetPath());
        if (!result.IsEmpty())
            PathToUrl(result);

        return TRUE;
    }
    else
    {
        result.Set(SString::Empty());
        return FALSE;
    }
}

/* static */
void PEAssembly::PathToUrl(SString &string)
{
    CONTRACTL
    {
        PRECONDITION(PEImage::CheckCanonicalFullPath(string));
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    SString::Iterator i = string.Begin();

#if !defined(TARGET_UNIX)
    if (i[0] == W('\\'))
    {
        // Network path
        string.Insert(i, SL("file://"));
        string.Skip(i, SL("file://"));
    }
    else
    {
        // Disk path
        string.Insert(i, SL("file:///"));
        string.Skip(i, SL("file:///"));
    }
#else
    // Unix doesn't have a distinction between a network or a local path
    _ASSERTE( i[0] == W('\\') || i[0] == W('/'));
    SString sss(SString::Literal, W("file://"));
    string.Insert(i, sss);
    string.Skip(i, sss);
#endif

    while (string.Find(i, W('\\')))
    {
        string.Replace(i, W('/'));
    }
}

void PEAssembly::UrlToPath(SString &string)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    SString::Iterator i = string.Begin();

    SString sss2(SString::Literal, W("file://"));
#if !defined(TARGET_UNIX)
    SString sss3(SString::Literal, W("file:///"));
    if (string.MatchCaseInsensitive(i, sss3))
        string.Delete(i, 8);
    else
#endif
    if (string.MatchCaseInsensitive(i, sss2))
        string.Delete(i, 7);

    while (string.Find(i, W('/')))
    {
        string.Replace(i, W('\\'));
    }

    RETURN;
}

BOOL PEAssembly::FindLastPathSeparator(const SString &path, SString::Iterator &i)
{
#ifdef TARGET_UNIX
    SString::Iterator slash = i;
    SString::Iterator backSlash = i;
    BOOL foundSlash = path.FindBack(slash, '/');
    BOOL foundBackSlash = path.FindBack(backSlash, '\\');
    if (!foundSlash && !foundBackSlash)
        return FALSE;
    else if (foundSlash && !foundBackSlash)
        i = slash;
    else if (!foundSlash && foundBackSlash)
        i = backSlash;
    else
        i = (backSlash > slash) ? backSlash : slash;
    return TRUE;
#else
    return path.FindBack(i, '\\');
#endif //TARGET_UNIX
}

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

HRESULT PEAssembly::GetVersion(USHORT *pMajor, USHORT *pMinor, USHORT *pBuild, USHORT *pRevision)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMajor, NULL_OK));
        PRECONDITION(CheckPointer(pMinor, NULL_OK));
        PRECONDITION(CheckPointer(pBuild, NULL_OK));
        PRECONDITION(CheckPointer(pRevision, NULL_OK));
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(GetMDImport()->IsValidToken(TokenFromRid(1, mdtAssembly)));

    HRESULT hr = S_OK;;
    AssemblyMetaDataInternal md;
    IfFailRet(GetMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, &md, NULL));

    if (pMajor != NULL)
        *pMajor = md.usMajorVersion;
    if (pMinor != NULL)
        *pMinor = md.usMinorVersion;
    if (pBuild != NULL)
        *pBuild = md.usBuildNumber;
    if (pRevision != NULL)
        *pRevision = md.usRevisionNumber;

    return S_OK;
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void PEAssembly::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    DAC_ENUM_DTHIS();
    EMEM_OUT(("MEM: %p PEAssembly\n", dac_cast<TADDR>(this)));

#ifdef _DEBUG
    // Not a big deal if it's NULL or fails.
    m_debugName.EnumMemoryRegions(flags);
#endif

    if (m_PEImage.IsValid())
    {
        m_PEImage->EnumMemoryRegions(flags);
    }
}

#endif // #ifdef DACCESS_COMPILE


//-------------------------------------------------------------------------------
// Make best-case effort to obtain an image name for use in an error message.
//
// This routine must expect to be called before the this object is fully loaded.
// It can return an empty if the name isn't available or the object isn't initialized
// enough to get a name, but it mustn't crash.
//-------------------------------------------------------------------------------
LPCWSTR PEAssembly::GetPathForErrorMessages()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END

    if (!IsDynamic())
    {
        return m_PEImage->GetPathForErrorMessages();
    }
    else
    {
        return W("");
    }
}


#ifdef DACCESS_COMPILE
TADDR PEAssembly::GetMDInternalRWAddress()
{
    if (!m_MDImportIsRW_Debugger_Use_Only)
        return 0;
    else
    {
        // This line of code is a bit scary, but it is correct for now at least...
        // 1) We are using 'm_pMDImport_Use_Accessor' directly, and not the accessor. The field is
        //    named this way to prevent debugger code that wants a host implementation of IMDInternalImport
        //    from accidentally trying to use this pointer. This pointer is a target pointer, not
        //    a host pointer. However in this function we do want the target pointer, so the usage is
        //    accurate.
        // 2) ASSUMPTION: We are assuming that the only valid implementation of RW metadata is
        //    MDInternalRW. If that ever changes we would need some way to disambiguate, and
        //    probably this entire code path would need to be redesigned.
        // 3) ASSUMPTION: We are assuming that no pointer adjustment is required to convert between
        //    IMDInternalImport*, IMDInternalImportENC* and MDInternalRW*. Ideally I was hoping to do this with a
        //    static_cast<> but the compiler complains that the ENC<->RW is an unrelated conversion.
        return (TADDR)m_pMDImport_UseAccessor;
    }
}
#endif

// Returns the AssemblyBinder* instance associated with the PEAssembly
PTR_AssemblyBinder PEAssembly::GetAssemblyBinder()
{
    LIMITED_METHOD_CONTRACT;

    PTR_AssemblyBinder pBinder = NULL;

    BINDER_SPACE::Assembly* pHostAssembly = GetHostAssembly();
    if (pHostAssembly)
    {
        pBinder = dac_cast<PTR_AssemblyBinder>(pHostAssembly->GetBinder());
    }
    else
    {
        // If we do not have a host assembly, check if we are dealing with
        // a dynamically emitted assembly and if so, use its fallback load context
        // binder reference.
        if (IsDynamic())
        {
            pBinder = GetFallbackBinder();
        }
    }

    return pBinder;
}
