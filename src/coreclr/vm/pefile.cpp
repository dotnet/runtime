// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEFile.cpp
//

// --------------------------------------------------------------------------------


#include "common.h"
#include "pefile.h"
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

// ================================================================================
// PEFile class - this is an abstract base class for PEModule and PEAssembly
// <TODO>@todo: rename TargetFile</TODO>
// ================================================================================

PEFile::PEFile(PEImage *identity) :
#if _DEBUG
    m_pDebugName(NULL),
#endif
    m_identity(NULL),
    m_openedILimage(NULL),
    m_MDImportIsRW_Debugger_Use_Only(FALSE),
    m_bHasPersistentMDImport(FALSE),
    m_pMDImport(NULL),
    m_pImporter(NULL),
    m_pEmitter(NULL),
    m_pMetadataLock(::new SimpleRWLock(PREEMPTIVE, LOCK_TYPE_DEFAULT)),
    m_refCount(1),
    m_flags(0),
    m_pAssemblyLoadContext(nullptr),
    m_pHostAssembly(nullptr),
    m_pFallbackLoadContextBinder(nullptr)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (identity)
    {
        identity->AddRef();
        m_identity = identity;

        if(identity->IsOpened())
        {
            //already opened, prepopulate
            identity->AddRef();
            m_openedILimage = identity;
        }
    }


}



PEFile::~PEFile()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ReleaseMetadataInterfaces(TRUE);


    if (m_openedILimage != NULL)
        m_openedILimage->Release();
    if (m_identity != NULL)
        m_identity->Release();
    if (m_pMetadataLock)
        delete m_pMetadataLock;

    if (m_pHostAssembly != NULL)
    {
        m_pHostAssembly->Release();
    }
}

/* static */
PEFile *PEFile::Open(PEImage *image)
{
    CONTRACT(PEFile *)
    {
        PRECONDITION(image != NULL);
        PRECONDITION(image->CheckFormat());
        POSTCONDITION(RETVAL != NULL);
        POSTCONDITION(!RETVAL->IsModule());
        POSTCONDITION(!RETVAL->IsAssembly());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    PEFile *pFile = new PEFile(image);

    if (image->HasNTHeaders() && image->HasCorHeader())
        pFile->OpenMDImport_Unsafe(); //no one else can see the object yet

#if _DEBUG
    pFile->m_debugName = image->GetPath();
    pFile->m_debugName.Normalize();
    pFile->m_pDebugName = pFile->m_debugName;
#endif

    RETURN pFile;
}

//-----------------------------------------------------------------------------------------------------
// Catch attempts to load x64 assemblies on x86, etc.
//-----------------------------------------------------------------------------------------------------
static void ValidatePEFileMachineType(PEFile *peFile)
{
    STANDARD_VM_CONTRACT;

    if (peFile->IsDynamic())
        return;    // PEFiles for ReflectionEmit assemblies don't cache the machine type.

    if (peFile->IsResource())
        return;    // PEFiles for resource assemblies don't cache the machine type.

    if (peFile->HasNativeImage())
        return;    // If it passed the native binder, no need to do the check again esp. at the risk of inviting an IL page-in.

    DWORD peKind;
    DWORD actualMachineType;
    peFile->GetPEKindAndMachine(&peKind, &actualMachineType);

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
        if (peFile->IsAssembly())
            ((PEAssembly*)peFile)->GetDisplayName(name);
        else
            name = StackSString(SString::Utf8, peFile->GetSimpleName());

        COMPlusThrow(kBadImageFormatException, IDS_CLASSLOAD_WRONGCPU, name.GetUnicode());
    }

    return;   // If we got here, all is good.
}

void PEFile::LoadLibrary(BOOL allowNativeSkip/*=TRUE*/) // if allowNativeSkip==FALSE force IL image load
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    // Catch attempts to load x64 assemblies on x86, etc.
    ValidatePEFileMachineType(this);

    // See if we've already loaded it.
    if (CheckLoaded(allowNativeSkip))
    {
        RETURN;
    }

    // Note that we may be racing other threads here, in the case of domain neutral files

    // Resource images are always flat.
    if (IsResource())
    {
        GetILimage()->LoadNoMetaData();
        RETURN;
    }

#if !defined(TARGET_64BIT)
    if (!HasNativeImage() && !GetILimage()->Has32BitNTHeaders())
    {
        // Tried to load 64-bit assembly on 32-bit platform.
        EEFileLoadException::Throw(this, COR_E_BADIMAGEFORMAT, NULL);
    }
#endif

    // We need contents now
    if (!HasNativeImage())
    {
        EnsureImageOpened();
    }

    // Since we couldn't call LoadLibrary, we must be an IL only image
    // or the image may still contain unfixed up stuff
    if (!GetILimage()->IsILOnly())
    {
        if (!GetILimage()->HasV1Metadata())
            ThrowHR(COR_E_FIXUPSINEXE); // <TODO>@todo: better error</TODO>
    }

    if (GetILimage()->IsFile())
    {
#ifdef TARGET_UNIX
        bool loadILImage = GetILimage()->IsILOnly();
#else // TARGET_UNIX
        bool loadILImage = GetILimage()->IsILOnly() && GetILimage()->IsInBundle();
#endif // TARGET_UNIX
        if (loadILImage)
        {
            GetILimage()->Load();
        }
        else
        {
            GetILimage()->LoadFromMapped();
        }
    }
    else
    {
        GetILimage()->LoadNoFile();
    }

    RETURN;
}

void PEFile::SetLoadedHMODULE(HMODULE hMod)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(hMod));
        POSTCONDITION(CheckLoaded());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    // See if the image is an internal PEImage.
    GetILimage()->SetLoadedHMODULE(hMod);

    RETURN;
}

/* static */
void PEFile::DefineEmitScope(
    GUID   iid,
    void **ppEmit)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(ppEmit));
        POSTCONDITION(CheckPointer(*ppEmit));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    SafeComHolder<IMetaDataDispenserEx> pDispenser;

    // Get the Dispenser interface.
    MetaDataGetDispenser(
        CLSID_CorMetaDataDispenser,
        IID_IMetaDataDispenserEx,
        (void **)&pDispenser);
    if (pDispenser == NULL)
    {
        ThrowOutOfMemory();
    }

    // Set the option on the dispenser turn on duplicate check for TypeDef and moduleRef
    VARIANT varOption;
    V_VT(&varOption) = VT_UI4;
    V_I4(&varOption) = MDDupDefault | MDDupTypeDef | MDDupModuleRef | MDDupExportedType | MDDupAssemblyRef | MDDupPermission | MDDupFile;
    IfFailThrow(pDispenser->SetOption(MetaDataCheckDuplicatesFor, &varOption));

    // Set minimal MetaData size
    V_VT(&varOption) = VT_UI4;
    V_I4(&varOption) = MDInitialSizeMinimal;
    IfFailThrow(pDispenser->SetOption(MetaDataInitialSize, &varOption));

    // turn on the thread safety!
    V_I4(&varOption) = MDThreadSafetyOn;
    IfFailThrow(pDispenser->SetOption(MetaDataThreadSafetyOptions, &varOption));

    IfFailThrow(pDispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, iid, (IUnknown **)ppEmit));

    RETURN;
} // PEFile::DefineEmitScope

// ------------------------------------------------------------
// Identity
// ------------------------------------------------------------

BOOL PEFile::Equals(PEFile *pFile)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pFile));
        GC_NOTRIGGER;
        NOTHROW;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Same object is equal
    if (pFile == this)
        return TRUE;

    // Different host assemblies cannot be equal unless they are associated with the same host binder
    // It's ok if only one has a host binder because multiple threads can race to load the same assembly
    // and that may cause temporary candidate PEAssembly objects that never get bound to a host assembly
    // because another thread beats it; the losing thread will pick up the PEAssembly in the cache.
    if (pFile->HasHostAssembly() && this->HasHostAssembly())
    {
        AssemblyBinder* fileBinderId = pFile->GetHostAssembly()->GetBinder();
        AssemblyBinder* thisBinderId = this->GetHostAssembly()->GetBinder();

        if (fileBinderId != thisBinderId || fileBinderId == NULL)
            return FALSE;
    }

    // Same identity is equal
    if (m_identity != NULL && pFile->m_identity != NULL
        && m_identity->Equals(pFile->m_identity))
        return TRUE;

    // Same image is equal
    if (m_openedILimage != NULL && pFile->m_openedILimage != NULL
        && m_openedILimage->Equals(pFile->m_openedILimage))
        return TRUE;

    return FALSE;
}

BOOL PEFile::Equals(PEImage *pImage)
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

    // Same object is equal
    if (pImage == m_identity || pImage == m_openedILimage)
        return TRUE;

    // Same identity is equal
    if (m_identity != NULL
        && m_identity->Equals(pImage))
        return TRUE;

    // Same image is equal
    if (m_openedILimage != NULL
        && m_openedILimage->Equals(pImage))
        return TRUE;


    return FALSE;
}

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

void PEFile::GetCodeBaseOrName(SString &result)
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

    if (m_identity != NULL && !m_identity->GetPath().IsEmpty())
    {
        result.Set(m_identity->GetPath());
    }
    else if (IsAssembly())
    {
        ((PEAssembly*)this)->GetCodeBase(result);
    }
    else
        result.SetUTF8(GetSimpleName());
}


// ------------------------------------------------------------
// Checks
// ------------------------------------------------------------



CHECK PEFile::CheckLoaded(BOOL bAllowNativeSkip/*=TRUE*/)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    CHECK(IsLoaded(bAllowNativeSkip)
          // We are allowed to skip LoadLibrary in most cases for ngen'ed IL only images
          || (bAllowNativeSkip && HasNativeImage() && IsILOnly()));

    CHECK_OK;
}


// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

PTR_CVOID PEFile::GetMetadata(COUNT_T *pSize)
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
         || !GetILimage()->HasNTHeaders()
         || !GetILimage()->HasCorHeader())
    {
        if (pSize != NULL)
            *pSize = 0;
        RETURN NULL;
    }
    else
    {
        RETURN GetILimage()->GetMetadata(pSize);
    }
}
#endif // #ifndef DACCESS_COMPILE

PTR_CVOID PEFile::GetLoadedMetadata(COUNT_T *pSize)
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

    if (!HasLoadedIL()
         || !GetLoadedIL()->HasNTHeaders()
         || !GetLoadedIL()->HasCorHeader())
    {
        if (pSize != NULL)
            *pSize = 0;
        RETURN NULL;
    }
    else
    {
        RETURN GetLoadedIL()->GetMetadata(pSize);
    }
}

TADDR PEFile::GetIL(RVA il)
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(il != 0);
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
#ifndef DACCESS_COMPILE
        PRECONDITION(CheckLoaded());
#endif
        POSTCONDITION(RETVAL != NULL);
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    PEImageLayout *image = NULL;

    image = GetLoadedIL();

#ifndef DACCESS_COMPILE
    // Verify that the IL blob is valid before giving it out
    if (!image->CheckILMethod(il))
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL_RANGE);
#endif

    RETURN image->GetRvaData(il);
}

#ifndef DACCESS_COMPILE

void PEFile::OpenImporter()
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
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetPersistentMDImport(),
                                                       IID_IMetaDataImport2,
                                                       (void **)&pIMDImport));

    // Atomically swap it into the field (release it if we lose the race)
    if (FastInterlockCompareExchangePointer(&m_pImporter, pIMDImport, NULL) != NULL)
        pIMDImport->Release();
}

void PEFile::ConvertMDInternalToReadWrite()
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
    _ASSERTE(m_bHasPersistentMDImport);
    if (FastInterlockCompareExchangePointer(&m_pMDImport, pNew, pOld) == pOld)
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

void PEFile::OpenMDImport_Unsafe()
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
        && GetILimage()->HasNTHeaders()
            && GetILimage()->HasCorHeader())
    {
        m_pMDImport=GetILimage()->GetMDImport();
    }
    else
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    m_bHasPersistentMDImport=TRUE;

    _ASSERTE(m_pMDImport);
    m_pMDImport->AddRef();
}

void PEFile::OpenEmitter()
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
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetPersistentMDImport(),
                                                       IID_IMetaDataEmit,
                                                       (void **)&pIMDEmit));

    // Atomically swap it into the field (release it if we lose the race)
    if (FastInterlockCompareExchangePointer(&m_pEmitter, pIMDEmit, NULL) != NULL)
        pIMDEmit->Release();
}


void PEFile::ReleaseMetadataInterfaces(BOOL bDestructor, BOOL bKeepNativeData/*=FALSE*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(bDestructor||m_pMetadataLock->IsWriterLock());
    }
    CONTRACTL_END;
    _ASSERTE(bDestructor || !m_bHasPersistentMDImport);

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

    if (m_pMDImport != NULL && (!bKeepNativeData || !HasNativeImage()))
    {
        m_pMDImport->Release();
        m_pMDImport=NULL;
     }
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

void PEFile::GetEmbeddedResource(DWORD dwOffset, DWORD *cbResource, PBYTE *pbInMemoryResource)
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

    // NOTE: it's not clear whether to load this from m_image or m_loadedImage.
    // m_loadedImage is probably preferable, but this may be called by security
    // before the image is loaded.

    EnsureImageOpened();
    PEImage* image = GetILimage();

    PEImageLayoutHolder theImage(image->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED));
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

PEAssembly *
PEFile::LoadAssembly(
    mdAssemblyRef       kAssemblyRef,
    IMDInternalImport * pImport)
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

    if (pImport == NULL)
        pImport = GetPersistentMDImport();

    if (((TypeFromToken(kAssemblyRef) != mdtAssembly) &&
         (TypeFromToken(kAssemblyRef) != mdtAssemblyRef)) ||
        (!pImport->IsValidToken(kAssemblyRef)))
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    AssemblySpec spec;

    spec.InitializeSpec(kAssemblyRef, pImport, GetAppDomain()->FindAssembly(GetAssembly()));

    RETURN GetAppDomain()->BindAssemblySpec(&spec, TRUE);
}


BOOL PEFile::GetResource(LPCSTR szName, DWORD *cbResource,
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
    PEFile*            pPEFile = NULL;
    ReleaseHolder<IMDInternalImport> pImport (GetMDImportWithRef());
    if (SUCCEEDED(pImport->FindManifestResourceByName(szName, &mdResource)))
    {
        pPEFile = this;
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

        DomainAssembly* pParentAssembly = GetAppDomain()->FindAssembly(GetAssembly());
        pAssembly = pAppDomain->RaiseResourceResolveEvent(pParentAssembly, szName);
        if (pAssembly == NULL)
            return FALSE;

        pDomainAssembly = pAssembly->GetDomainAssembly();
        pPEFile = pDomainAssembly->GetFile();

        if (FAILED(pAssembly->GetManifestImport()->FindManifestResourceByName(
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
        IfFailThrow(pPEFile->GetPersistentMDImport()->GetManifestResourceProps(
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
            spec.InitializeSpec(mdLinkRef, GetPersistentMDImport(), pDomainAssembly);
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

            pPEFile->GetEmbeddedResource(dwOffset, cbResource, pbInMemoryResource);

            return TRUE;
        }
        return FALSE;

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_IN_MANIFESTRES);
    }
}

void PEFile::GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine)
{
    WRAPPER_NO_CONTRACT;

    if (IsResource() || IsDynamic())
    {
        if (pdwKind)
            *pdwKind = 0;
        if (pdwMachine)
            *pdwMachine = 0;
        return;
    }

    GetILimage()->GetPEKindAndMachine(pdwKind, pdwMachine);
    return;
}

ULONG PEFile::GetILImageTimeDateStamp()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return GetLoadedIL()->GetTimeDateStamp();
}


// ================================================================================
// PEAssembly class - a PEFile which represents an assembly
// ================================================================================

// Statics initialization.
/* static */
void PEAssembly::Attach()
{
    STANDARD_VM_CONTRACT;
}

#ifndef DACCESS_COMPILE
PEAssembly::PEAssembly(
                CoreBindResult* pBindResultInfo,
                IMetaDataEmit* pEmit,
                PEFile *creator,
                BOOL system,
                PEImage * pPEImageIL /*= NULL*/,
                PEImage * pPEImageNI /*= NULL*/,
                BINDER_SPACE::Assembly * pHostAssembly /*= NULL*/)

  : PEFile(pBindResultInfo ? (pBindResultInfo->GetPEImage() ? pBindResultInfo->GetPEImage() :
                                                              (pBindResultInfo->HasNativeImage() ? pBindResultInfo->GetNativeImage() : NULL)
                              ): pPEImageIL? pPEImageIL:(pPEImageNI? pPEImageNI:NULL)),
    m_creator(clr::SafeAddRef(creator))
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(pEmit, NULL_OK));
        PRECONDITION(CheckPointer(creator, NULL_OK));
        PRECONDITION(pBindResultInfo == NULL || (pPEImageIL == NULL && pPEImageNI == NULL));
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    m_flags |= PEFILE_ASSEMBLY;
    if (system)
        m_flags |= PEFILE_SYSTEM;

    // If we have no native image, we require a mapping for the file.
    if (!HasNativeImage() || !IsILOnly())
        EnsureImageOpened();

    // Open metadata eagerly to minimize failure windows
    if (pEmit == NULL)
        OpenMDImport_Unsafe(); //constructor, cannot race with anything
    else
    {
        _ASSERTE(!m_bHasPersistentMDImport);
        IfFailThrow(GetMetaDataInternalInterfaceFromPublic(pEmit, IID_IMDInternalImport,
                                                           (void **)&m_pMDImport));
        m_pEmitter = pEmit;
        pEmit->AddRef();
        m_bHasPersistentMDImport=TRUE;
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
        pBindResultInfo->GetBindAssembly(&m_pHostAssembly);
    }

#if _DEBUG
    GetCodeBaseOrName(m_debugName);
    m_debugName.Normalize();
    m_pDebugName = m_debugName;
#endif

    SetupAssemblyLoadContext();
}
#endif // !DACCESS_COMPILE


PEAssembly *PEAssembly::Open(
    PEAssembly *       pParent,
    PEImage *          pPEImageIL,
    PEImage *          pPEImageNI,
    BINDER_SPACE::Assembly * pHostAssembly)
{
    STANDARD_VM_CONTRACT;

    PEAssembly * pPEAssembly = new PEAssembly(
        nullptr,        // BindResult
        nullptr,        // IMetaDataEmit
        pParent,        // PEFile creator
        FALSE,          // isSystem
        pPEImageIL,
        pPEImageNI,
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
    if (m_creator != NULL)
        m_creator->Release();

}

/* static */
PEAssembly *PEAssembly::OpenSystem(IUnknown * pAppCtx)
{
    STANDARD_VM_CONTRACT;

    PEAssembly *result = NULL;

    EX_TRY
    {
        result = DoOpenSystem(pAppCtx);
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
PEAssembly *PEAssembly::DoOpenSystem(IUnknown * pAppCtx)
{
    CONTRACT(PEAssembly *)
    {
        POSTCONDITION(CheckPointer(RETVAL));
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    ETWOnStartup (FusionBinding_V1, FusionBindingEnd_V1);
    CoreBindResult bindResult;
    ReleaseHolder<BINDER_SPACE::Assembly> pPrivAsm;
    IfFailThrow(BINDER_SPACE::AssemblyBinderCommon::BindToSystem(&pPrivAsm, !IsCompilationProcess() || g_fAllowNativeImages));
    if(pPrivAsm != NULL)
    {
        bindResult.Init(pPrivAsm);
    }

    RETURN new PEAssembly(&bindResult, NULL, NULL, TRUE, FALSE);
}

PEAssembly* PEAssembly::Open(CoreBindResult* pBindResult,
                                   BOOL isSystem)
{

    return new PEAssembly(pBindResult,NULL,NULL,isSystem);

};

/* static */
PEAssembly *PEAssembly::Create(PEAssembly *pParentAssembly,
                               IMetaDataAssemblyEmit *pAssemblyEmit)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(pParentAssembly));
        PRECONDITION(CheckPointer(pAssemblyEmit));
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Set up the metadata pointers in the PEAssembly. (This is the only identity
    // we have.)
    SafeComHolder<IMetaDataEmit> pEmit;
    pAssemblyEmit->QueryInterface(IID_IMetaDataEmit, (void **)&pEmit);
    PEAssemblyHolder pFile(new PEAssembly(NULL, pEmit, pParentAssembly, FALSE));
    RETURN pFile.Extract();
}


#endif // #ifndef DACCESS_COMPILE



#ifndef DACCESS_COMPILE

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

// Effective path is the path of nearest parent (creator) assembly which has a nonempty path.

const SString &PEAssembly::GetEffectivePath()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PEAssembly *pAssembly = this;

    while (pAssembly->m_identity == NULL
           || pAssembly->m_identity->GetPath().IsEmpty())
    {
        if (pAssembly->m_creator)
            pAssembly = pAssembly->m_creator->GetAssembly();
        else // Unmanaged exe which loads byte[]/IStream assemblies
            return SString::Empty();
    }

    return pAssembly->m_identity->GetPath();
}


// Codebase is the fusion codebase or path for the assembly.  It is in URL format.
// Note this may be obtained from the parent PEFile if we don't have a path or fusion
// assembly.
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

    auto ilImage = GetILimage();
    if (ilImage == nullptr || !ilImage->IsInBundle())
    {
        // All other cases use the file path.
        result.Set(GetEffectivePath());
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

HRESULT PEFile::GetVersion(USHORT *pMajor, USHORT *pMinor, USHORT *pBuild, USHORT *pRevision)
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

    AssemblyMetaDataInternal md;
    HRESULT hr = S_OK;;
    if (m_bHasPersistentMDImport)
    {
        _ASSERTE(GetPersistentMDImport()->IsValidToken(TokenFromRid(1, mdtAssembly)));
        IfFailRet(GetPersistentMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, &md, NULL));
    }
    else
    {
        ReleaseHolder<IMDInternalImport> pImport(GetMDImportWithRef());
        _ASSERTE(pImport->IsValidToken(TokenFromRid(1, mdtAssembly)));
        IfFailRet(pImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, &md, NULL));
    }

    if (pMajor != NULL)
        *pMajor = md.usMajorVersion;
    if (pMinor != NULL)
        *pMinor = md.usMinorVersion;
    if (pBuild != NULL)
        *pBuild = md.usBuildNumber;
    if (pRevision != NULL)
        *pRevision = md.usRevisionNumber;

    return hr;
}



void PEFile::EnsureImageOpened()
{
    WRAPPER_NO_CONTRACT;
    if (IsDynamic())
        return;

    GetILimage()->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED)->Release();
}

void PEFile::SetupAssemblyLoadContext()
{
    PTR_AssemblyBinder pBindingContext = GetBindingContext();

    m_pAssemblyLoadContext = (pBindingContext != NULL) ?
        (AssemblyLoadContext*)pBindingContext :
        AppDomain::GetCurrentDomain()->CreateBinderContext();
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
PEFile::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // sizeof(PEFile) == 0xb8
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p PEFile\n", dac_cast<TADDR>(this)));

#ifdef _DEBUG
    // Not a big deal if it's NULL or fails.
    m_debugName.EnumMemoryRegions(flags);
#endif

    if (m_identity.IsValid())
    {
        m_identity->EnumMemoryRegions(flags);
    }
    if (GetILimage().IsValid())
    {
        GetILimage()->EnumMemoryRegions(flags);
    }
}

void
PEAssembly::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    PEFile::EnumMemoryRegions(flags);

    if (m_creator.IsValid())
    {
        m_creator->EnumMemoryRegions(flags);
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
LPCWSTR PEFile::GetPathForErrorMessages()
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
        return m_identity->GetPathForErrorMessages();
    }
    else
    {
        return W("");
    }
}


#ifdef DACCESS_COMPILE
TADDR PEFile::GetMDInternalRWAddress()
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
        return (TADDR) m_pMDImport_UseAccessor;
    }
}
#endif

// Returns the AssemblyBinder* instance associated with the PEFile
PTR_AssemblyBinder PEFile::GetBindingContext()
{
    LIMITED_METHOD_CONTRACT;

    PTR_AssemblyBinder pBindingContext = NULL;

    // CoreLibrary is always bound in context of the TPA Binder. However, since it gets loaded and published
    // during EEStartup *before* DefaultContext Binder (aka TPAbinder) is initialized, we dont have a binding context to publish against.
    if (!IsSystem())
    {
        BINDER_SPACE::Assembly* pHostAssembly = GetHostAssembly();
        if (pHostAssembly)
        {
            pBindingContext = dac_cast<PTR_AssemblyBinder>(pHostAssembly->GetBinder());
        }
        else
        {
            // If we do not have a host assembly, check if we are dealing with
            // a dynamically emitted assembly and if so, use its fallback load context
            // binder reference.
            if (IsDynamic())
            {
                pBindingContext = GetFallbackLoadContextBinder();
            }
        }
    }

    return pBindingContext;
}
