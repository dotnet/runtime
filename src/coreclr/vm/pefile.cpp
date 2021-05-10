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
#ifdef FEATURE_PREJIT
#include "compile.h"
#endif
#include "strongnameinternal.h"

#include "../binder/inc/applicationcontext.hpp"

#include "clrprivbinderutil.h"
#include "../binder/inc/coreclrbindercommon.h"


#ifdef FEATURE_PREJIT
#include "compile.h"

#ifdef DEBUGGING_SUPPORTED
SVAL_IMPL_INIT(DWORD, PEFile, s_NGENDebugFlags, 0);
#endif
#endif

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
#ifdef FEATURE_PREJIT
    m_nativeImage(NULL),
#endif
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

#ifdef FEATURE_PREJIT
    if (m_nativeImage != NULL)
    {
        MarkNativeImageInvalidIfOwned();

        m_nativeImage->Release();
    }
#endif //FEATURE_PREJIT


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

#ifdef FEATURE_PREJIT
    // For on-disk Dlls, we can call LoadLibrary
    if (IsDll() && !((HasNativeImage()?m_nativeImage:GetILimage())->GetPath().IsEmpty()))
    {
        // Note that we may get a DllMain notification inside here.
        if (allowNativeSkip && HasNativeImage())
        {
            m_nativeImage->Load();
            if(!m_nativeImage->IsNativeILILOnly())
                GetILimage()->Load();             // For IJW we have to load IL also...
        }
        else
            GetILimage()->Load();
    }
    else
#endif // FEATURE_PREJIT
    {

        // Since we couldn't call LoadLibrary, we must be an IL only image
        // or the image may still contain unfixed up stuff
        // Note that we make an exception for CompilationDomains, since PEImage
        // will map non-ILOnly images in a compilation domain.
        if (!GetILimage()->IsILOnly() && !GetAppDomain()->IsCompilationDomain())
        {
            if (!GetILimage()->HasV1Metadata())
                ThrowHR(COR_E_FIXUPSINEXE); // <TODO>@todo: better error</TODO>
        }



        // If we are already mapped, we can just use the current image.
#ifdef FEATURE_PREJIT
        if (allowNativeSkip && HasNativeImage())
        {
            m_nativeImage->LoadFromMapped();

            if( !m_nativeImage->IsNativeILILOnly())
                GetILimage()->LoadFromMapped();        // For IJW we have to load IL also...
        }
        else
#endif
        {
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
        }
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
        UINT_PTR fileBinderId = 0;
        if (FAILED(pFile->GetHostAssembly()->GetBinderID(&fileBinderId)))
            return FALSE;

        UINT_PTR thisBinderId = 0;
        if (FAILED(this->GetHostAssembly()->GetBinderID(&thisBinderId)))
            return FALSE;

        if (fileBinderId != thisBinderId)
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

#ifdef FEATURE_PREJIT
    if(pImage == m_nativeImage)
        return TRUE;
#endif
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

#ifdef FEATURE_PREJIT
    if (HasNativeImageMetadata())
    {
        RETURN m_nativeImage->GetMetadata(pSize);
    }
#endif

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

#ifdef FEATURE_PREJIT
    if (HasNativeImageMetadata())
    {
        RETURN GetLoadedNative()->GetMetadata(pSize);
    }
#endif

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

#ifdef FEATURE_PREJIT
    // Note it is important to get the IL from the native image if
    // available, since we are using the metadata from the native image
    // which has different IL rva's.
    if (HasNativeImageMetadata())
    {
        image = GetLoadedNative();

#ifndef DACCESS_COMPILE
        // NGen images are trusted to be well-formed.
        _ASSERTE(image->CheckILMethod(il));
#endif
    }
    else
#endif // FEATURE_PREJIT
    {
        image = GetLoadedIL();

#ifndef DACCESS_COMPILE
        // Verify that the IL blob is valid before giving it out
        if (!image->CheckILMethod(il))
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL_RANGE);
#endif
    }

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
#ifdef FEATURE_PREJIT
    if (m_nativeImage != NULL
        && m_nativeImage->GetMDImport() != NULL
        )
    {
        // Use native image for metadata
        m_flags |= PEFILE_HAS_NATIVE_IMAGE_METADATA;
        m_pMDImport=m_nativeImage->GetMDImport();
    }
    else
#endif
    {
#ifdef FEATURE_PREJIT
        m_flags &= ~PEFILE_HAS_NATIVE_IMAGE_METADATA;
#endif
        if (!IsDynamic()
           && GetILimage()->HasNTHeaders()
             && GetILimage()->HasCorHeader())
        {
            m_pMDImport=GetILimage()->GetMDImport();
        }
        else
            ThrowHR(COR_E_BADIMAGEFORMAT);

        m_bHasPersistentMDImport=TRUE;
    }
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

#ifdef FEATURE_PREJIT
#ifndef DACCESS_COMPILE
// ------------------------------------------------------------
// Native image access
// ------------------------------------------------------------

void PEFile::SetNativeImage(PEImage *image)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(!HasNativeImage());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    _ASSERTE(image != NULL);
    PREFIX_ASSUME(image != NULL);

    if (image->GetLoadedLayout()->GetBase() != image->GetLoadedLayout()->GetPreferredBase())
    {
        ExternalLog(LL_WARNING,
                    W("Native image loaded at base address") LFMT_ADDR
                    W("rather than preferred address:") LFMT_ADDR ,
                    DBG_ADDR(image->GetLoadedLayout()->GetBase()),
                    DBG_ADDR(image->GetLoadedLayout()->GetPreferredBase()));
    }

    // First ask if we're supposed to be ignoring the prejitted code &
    // structures in NGENd images. If so, bail now and do not set m_nativeImage. We've
    // already set m_identity & m_openedILimage), and will use those PEImages to find
    // and JIT IL.
    if (ShouldTreatNIAsMSIL())
        RETURN;

    m_nativeImage = image;
    m_nativeImage->AddRef();
    m_nativeImage->Load();

#if defined(TARGET_AMD64) && !defined(CROSSGEN_COMPILE)
    static ConfigDWORD configNGenReserveForJumpStubs;
    int percentReserveForJumpStubs = configNGenReserveForJumpStubs.val(CLRConfig::INTERNAL_NGenReserveForJumpStubs);
    if (percentReserveForJumpStubs != 0)
    {
        PEImageLayout * pLayout = image->GetLoadedLayout();
        ExecutionManager::GetEEJitManager()->EnsureJumpStubReserve((BYTE *)pLayout->GetBase(), pLayout->GetVirtualSize(),
            percentReserveForJumpStubs * (pLayout->GetVirtualSize() / 100));
    }
#endif

    ExternalLog(LL_INFO100, W("Attempting to use native image %s."), image->GetPath().GetUnicode());
    RETURN;
}

void PEFile::ClearNativeImage()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeImage());
        POSTCONDITION(!HasNativeImage());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    ExternalLog(LL_WARNING, "Discarding native image.");


    MarkNativeImageInvalidIfOwned();

    {
        GCX_PREEMP();
        SafeComHolderPreemp<IMDInternalImport> pOldImport=GetMDImportWithRef();
        SimpleWriteLockHolder lock(m_pMetadataLock);

        EX_TRY
        {
            ReleaseMetadataInterfaces(FALSE);
            m_flags &= ~PEFILE_HAS_NATIVE_IMAGE_METADATA;
            if (m_nativeImage)
                m_nativeImage->Release();
            m_nativeImage = NULL;
            // Make sure our normal image is open
            EnsureImageOpened();

            // Reopen metadata from normal image
            OpenMDImport();
        }
        EX_HOOK
        {
            RestoreMDImport(pOldImport);
        }
        EX_END_HOOK;
    }

    RETURN;
}


extern DWORD g_dwLogLevel;

//===========================================================================================================
// Encapsulates CLR and Fusion logging for runtime verification of native images.
//===========================================================================================================
static void RuntimeVerifyVLog(DWORD level, PEAssembly *pLogAsm, const WCHAR *fmt, va_list args)
{
    STANDARD_VM_CONTRACT;

    BOOL fOutputToDebugger = (level == LL_ERROR && IsDebuggerPresent());
    BOOL fOutputToLogging = LoggingOn(LF_ZAP, level);

    StackSString message;
    message.VPrintf(fmt, args);

    if (fOutputToLogging)
    {
        SString displayString = pLogAsm->GetPath();
        LOG((LF_ZAP, level, "%s: \"%S\"\n", "ZAP", displayString.GetUnicode()));
        LOG((LF_ZAP, level, "%S", message.GetUnicode()));
        LOG((LF_ZAP, level, "\n"));
    }

    if (fOutputToDebugger)
    {
        SString displayString = pLogAsm->GetPath();
        WszOutputDebugString(W("CLR:("));
        WszOutputDebugString(displayString.GetUnicode());
        WszOutputDebugString(W(") "));
        WszOutputDebugString(message);
        WszOutputDebugString(W("\n"));
    }

}


//===========================================================================================================
// Encapsulates CLR and Fusion logging for runtime verification of native images.
//===========================================================================================================
static void RuntimeVerifyLog(DWORD level, PEAssembly *pLogAsm, const WCHAR *fmt, ...)
{
    STANDARD_VM_CONTRACT;

    // Avoid calling RuntimeVerifyVLog unless logging is on
    if (   ((level == LL_ERROR) && IsDebuggerPresent())
        || LoggingOn(LF_ZAP, level)
       )
    {
        va_list args;
        va_start(args, fmt);

        RuntimeVerifyVLog(level, pLogAsm, fmt, args);

        va_end(args);
    }
}

//==============================================================================

static const LPCWSTR CorCompileRuntimeDllNames[NUM_RUNTIME_DLLS] =
{
    MAKEDLLNAME_W(W("coreclr")),
    MAKEDLLNAME_W(W("clrjit"))
};


LPCWSTR CorCompileGetRuntimeDllName(CorCompileRuntimeDlls id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;


    return CorCompileRuntimeDllNames[id];
}

//===========================================================================================================
// Validates that an NI matches the running CLR, OS, CPU, etc.
//
// For historial reasons, some versions of the runtime perform this check at native bind time (preferrred),
// while others check at CLR load time.
//
// This is the common funnel for both versions and is agnostic to whether the "assembly" is represented
// by a CLR object or Fusion object.
//===========================================================================================================
BOOL RuntimeVerifyNativeImageVersion(const CORCOMPILE_VERSION_INFO *info, PEAssembly *pLogAsm)
{
    STANDARD_VM_CONTRACT;

    //
    // Check that the EE version numbers are the same.
    //

    if (info->wVersionMajor != RuntimeFileMajorVersion
        || info->wVersionMinor != RuntimeFileMinorVersion
        || info->wVersionBuildNumber != RuntimeFileBuildVersion
        || info->wVersionPrivateBuildNumber != RuntimeFileRevisionVersion)
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("CLR version recorded in native image doesn't match the current CLR."));
        return FALSE;
    }

    //
    // Check checked/free status
    //

    if (info->wBuild !=
#if _DEBUG
        CORCOMPILE_BUILD_CHECKED
#else
        CORCOMPILE_BUILD_FREE
#endif
        )
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("Checked/free mismatch with native image."));
        return FALSE;
    }

    //
    // Check processor
    //

    if (info->wMachine != IMAGE_FILE_MACHINE_NATIVE_NI)
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("Processor type recorded in native image doesn't match this machine's processor."));
        return FALSE;
    }

#ifndef CROSSGEN_COMPILE
    //
    // Check the processor specific ID
    //

    CORINFO_CPU cpuInfo;
    GetSpecificCpuInfo(&cpuInfo);

    if (!IsCompatibleCpuInfo(&cpuInfo, &info->cpuInfo))
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("Required CPU features recorded in native image don't match this machine's processor."));
        return FALSE;
    }
#endif // CROSSGEN_COMPILE


    //
    // The zap is up to date.
    //

    RuntimeVerifyLog(LL_INFO100, pLogAsm, W("Native image has correct version information."));
    return TRUE;
}

//===========================================================================================================
// Validates that an NI matches the running CLR, OS, CPU, etc. This is the entrypoint used by the CLR loader.
//
//===========================================================================================================
BOOL PEAssembly::CheckNativeImageVersion(PEImage *peimage)
{
    STANDARD_VM_CONTRACT;

    //
    // Get the zap version header. Note that modules will not have version
    // headers - they add no additional versioning constraints from their
    // assemblies.
    //
    PEImageLayoutHolder image = peimage->GetLayout(PEImageLayout::LAYOUT_ANY, PEImage::LAYOUT_CREATEIFNEEDED);

    if (!image->HasNativeHeader())
        return FALSE;

    if (!image->CheckNativeHeaderVersion())
    {
        // Wrong native image version is fatal error on CoreCLR
        ThrowHR(COR_E_NI_AND_RUNTIME_VERSION_MISMATCH);
    }

    CORCOMPILE_VERSION_INFO *info = image->GetNativeVersionInfo();
    if (info == NULL)
        return FALSE;

    if (!RuntimeVerifyNativeImageVersion(info, this))
    {
        // Wrong native image version is fatal error on CoreCLR
        ThrowHR(COR_E_NI_AND_RUNTIME_VERSION_MISMATCH);
    }

    CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();

    // Otherwise, match regardless of the instrumentation flags
    configFlags = (CorCompileConfigFlags)(configFlags & ~(CORCOMPILE_CONFIG_INSTRUMENTATION_NONE | CORCOMPILE_CONFIG_INSTRUMENTATION));

    if ((info->wConfigFlags & configFlags) != configFlags)
    {
        return FALSE;
    }

    return TRUE;
}

#endif // !DACCESS_COMPILE

/* static */
CorCompileConfigFlags PEFile::GetNativeImageConfigFlags(BOOL fForceDebug/*=FALSE*/,
                                                        BOOL fForceProfiling/*=FALSE*/,
                                                        BOOL fForceInstrument/*=FALSE*/)
{
    LIMITED_METHOD_DAC_CONTRACT;

    CorCompileConfigFlags result = (CorCompileConfigFlags)0;

    // Debugging

#ifdef DEBUGGING_SUPPORTED
    // if these have been set, the take precedence over anything else
    if (s_NGENDebugFlags)
    {
        if ((s_NGENDebugFlags & CORCOMPILE_CONFIG_DEBUG_NONE) != 0)
        {
            result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG_NONE);
        }
        else
        {
            if ((s_NGENDebugFlags & CORCOMPILE_CONFIG_DEBUG) != 0)
            {
                result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG);
            }
        }
    }
    else
#endif // DEBUGGING_SUPPORTED
    {
        if (fForceDebug)
        {
            result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG);
        }
        else
        {
            result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG_DEFAULT);
        }
    }

    // Profiling

#ifdef PROFILING_SUPPORTED
    if (fForceProfiling || CORProfilerUseProfileImages())
    {
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_PROFILING);

        result = (CorCompileConfigFlags) (result & ~(CORCOMPILE_CONFIG_DEBUG_NONE|
                                                     CORCOMPILE_CONFIG_DEBUG|
                                                     CORCOMPILE_CONFIG_DEBUG_DEFAULT));
    }
    else
#endif //PROFILING_SUPPORTED
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_PROFILING_NONE);

    // Instrumentation
#ifndef DACCESS_COMPILE
    BOOL instrumented = (!IsCompilationProcess() && g_pConfig->GetZapBBInstr());
#else
    BOOL instrumented = FALSE;
#endif
    if (instrumented || fForceInstrument)
    {
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_INSTRUMENTATION);
    }
    else
    {
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_INSTRUMENTATION_NONE);
    }

    // NOTE: Right now we are not taking instrumentation into account when binding.

    return result;
}

CorCompileConfigFlags PEFile::GetNativeImageConfigFlagsWithOverrides()
{
    LIMITED_METHOD_DAC_CONTRACT;

    BOOL fForceDebug, fForceProfiling, fForceInstrument;
    SystemDomain::GetCompilationOverrides(&fForceDebug,
                                          &fForceProfiling,
                                          &fForceInstrument);
    return PEFile::GetNativeImageConfigFlags(fForceDebug,
                                             fForceProfiling,
                                             fForceInstrument);
}

#ifndef DACCESS_COMPILE



//===========================================================================================================
// Validates that a hard-dep matches the a parent NI's compile-time hard-dep.
//
// For historial reasons, some versions of the runtime perform this check at native bind time (preferrred),
// while others check at CLR load time.
//
// This is the common funnel for both versions and is agnostic to whether the "assembly" is represented
// by a CLR object or Fusion object.
//
//===========================================================================================================
BOOL RuntimeVerifyNativeImageDependency(const CORCOMPILE_NGEN_SIGNATURE &ngenSigExpected,
                                        const CORCOMPILE_VERSION_INFO *pActual,
                                        PEAssembly                    *pLogAsm)
{
    STANDARD_VM_CONTRACT;

    if (ngenSigExpected != pActual->signature)
    {
        // Signature did not match
        SString displayString = pLogAsm->GetPath();
        RuntimeVerifyLog(LL_ERROR,
                         pLogAsm,
                         W("Rejecting native image because native image dependency %s ")
                         W("had a different identity than expected"),
                         displayString.GetUnicode());

        return FALSE;
    }
    return TRUE;
}
// Wrapper function for use by parts of the runtime that actually have a CORCOMPILE_DEPENDENCY to work with.
BOOL RuntimeVerifyNativeImageDependency(const CORCOMPILE_DEPENDENCY   *pExpected,
                                        const CORCOMPILE_VERSION_INFO *pActual,
                                        PEAssembly                    *pLogAsm)
{
    WRAPPER_NO_CONTRACT;

    return RuntimeVerifyNativeImageDependency(pExpected->signNativeImage,
                                              pActual,
                                              pLogAsm);
}

#endif // !DACCESS_COMPILE

#ifdef DEBUGGING_SUPPORTED
//
// Called through ICorDebugAppDomain2::SetDesiredNGENCompilerFlags to specify
// which kinds of ngen'd images fusion should load wrt debugging support
// Overrides any previous settings
//
void PEFile::SetNGENDebugFlags(BOOL fAllowOpt)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (fAllowOpt)
        s_NGENDebugFlags = CORCOMPILE_CONFIG_DEBUG_NONE;
    else
        s_NGENDebugFlags = CORCOMPILE_CONFIG_DEBUG;
    }

//
// Called through ICorDebugAppDomain2::GetDesiredNGENCompilerFlags to determine
// which kinds of ngen'd images fusion should load wrt debugging support
//
void PEFile::GetNGENDebugFlags(BOOL *fAllowOpt)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();

    *fAllowOpt = ((configFlags & CORCOMPILE_CONFIG_DEBUG) == 0);
}
#endif // DEBUGGING_SUPPORTED



#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Used in Apollo, this method determines whether profiling or debugging has requested
// the runtime to provide debuggable / profileable code. In other CLR builds, this would
// normally result in requiring the appropriate NGEN scenario be loaded (/Debug or
// /Profile) and to JIT if unavailable. In Apollo, however, these NGEN scenarios are
// never available, and even MSIL assemblies are often not available. So this function
// tells its caller to use the NGENd assembly as if it were an MSIL assembly--ignore the
// prejitted code and prebaked structures, and just JIT code and load classes from
// scratch.
//
// Return Value:
//      nonzero iff NGENd images should be treated as MSIL images.
//

// static
BOOL PEFile::ShouldTreatNIAsMSIL()
{
    LIMITED_METHOD_CONTRACT;

    // Never use fragile native image content during ReadyToRun compilation. It would
    // produces non-version resilient images because of wrong cached values for
    // MethodTable::IsLayoutFixedInCurrentVersionBubble, etc.
    if (IsReadyToRunCompilation())
        return TRUE;

    // Ask profiling API & config vars whether NGENd images should be avoided
    // completely.
    if (!NGENImagesAllowed())
        return TRUE;

    // Ask profiling and debugging if they're requesting us to use ngen /Debug or
    // /Profile images (which aren't available under Apollo)

    CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();

    if ((configFlags & (CORCOMPILE_CONFIG_DEBUG | CORCOMPILE_CONFIG_PROFILING)) != 0)
        return TRUE;

    return FALSE;
}

#endif  //!DACCESS_COMPILE
#endif  // FEATURE_PREJIT

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

    PEImage *image;

#ifdef FEATURE_PREJIT
    if (m_nativeImage != NULL)
        image = m_nativeImage;
    else
#endif
    {
        EnsureImageOpened();
        image = GetILimage();
    }

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

// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------
#ifdef FEATURE_PREJIT
void PEFile::ExternalLog(DWORD facility, DWORD level, const WCHAR *fmt, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, fmt);

    ExternalVLog(facility, level, fmt, args);

    va_end(args);
}

void PEFile::ExternalLog(DWORD level, const WCHAR *fmt, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, fmt);

    ExternalVLog(LF_ZAP, level, fmt, args);

    va_end(args);
}

void PEFile::ExternalLog(DWORD level, const char *msg)
{
    WRAPPER_NO_CONTRACT;

    // It is OK to use %S here. We know that msg is ASCII-only.
    ExternalLog(level, W("%S"), msg);
}

void PEFile::ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    BOOL fOutputToDebugger = (level == LL_ERROR && IsDebuggerPresent());
    BOOL fOutputToLogging = LoggingOn(facility, level);

    if (!fOutputToDebugger && !fOutputToLogging)
        return;

    StackSString message;
    message.VPrintf(fmt, args);

    if (fOutputToLogging)
    {
        if (GetMDImport() != NULL)
            LOG((facility, level, "%s: \"%s\"\n", (facility == LF_ZAP ? "ZAP" : "LOADER"), GetSimpleName()));
        else
            LOG((facility, level, "%s: \"%S\"\n", (facility == LF_ZAP ? "ZAP" : "LOADER"), ((const WCHAR *)GetPath())));

        LOG((facility, level, "%S", message.GetUnicode()));
        LOG((facility, level, "\n"));
    }

    if (fOutputToDebugger)
    {
        WszOutputDebugString(W("CLR:("));

        StackSString codebase;
        GetCodeBaseOrName(codebase);
        WszOutputDebugString(codebase);

        WszOutputDebugString(W(") "));

        WszOutputDebugString(message);
        WszOutputDebugString(W("\n"));
    }

    RETURN;
}

void PEFile::FlushExternalLog()
{
    LIMITED_METHOD_CONTRACT;
}
#endif

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

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        m_nativeImage->GetNativeILPEKindAndMachine(pdwKind, pdwMachine);
        return;
    }
#ifndef DACCESS_COMPILE
    if (!HasOpenedILimage())
    {
        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage = GetNativeImageWithRef();
        if (pNativeImage)
        {
            pNativeImage->GetNativeILPEKindAndMachine(pdwKind, pdwMachine);
            return;
        }
    }
#endif // DACCESS_COMPILE
#endif // FEATURE_PREJIT

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

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        // The IL image's time stamp is copied to the native image.
        CORCOMPILE_VERSION_INFO* pVersionInfo = GetLoadedNative()->GetNativeVersionInfoMaybeNull();
        if (pVersionInfo == NULL)
        {
            return 0;
        }
        else
        {
            return pVersionInfo->sourceAssembly.timeStamp;
        }
    }
#endif // FEATURE_PREJIT

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
                ICLRPrivAssembly * pHostAssembly /*= NULL*/)

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

#ifdef FEATURE_PREJIT
    // We check the precondition above that either pBindResultInfo is null or both pPEImageIL and pPEImageNI are,
    // so we'll only get a max of one native image passed in.
    if (pPEImageNI != NULL)
    {
        SetNativeImage(pPEImageNI);
    }

    if (pBindResultInfo && pBindResultInfo->HasNativeImage())
        SetNativeImage(pBindResultInfo->GetNativeImage());
#endif

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
    ICLRPrivAssembly * pHostAssembly)
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
    ReleaseHolder<ICLRPrivAssembly> pPrivAsm;
    IfFailThrow(CCoreCLRBinderHelper::BindToSystem(&pPrivAsm, !IsCompilationProcess() || g_fAllowNativeImages));
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


#ifdef FEATURE_PREJIT


void PEAssembly::SetNativeImage(PEImage * image)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    image->Load();

    if (CheckNativeImageVersion(image))
    {
        PEFile::SetNativeImage(image);
#if 0
        //Enable this code if you want to make sure we never touch the flat layout in the presence of the
        //ngen image.
//#if defined(_DEBUG)
        //find all the layouts in the il image and make sure we never touch them.
        unsigned ignored = 0;
        PTR_PEImageLayout layout = m_ILimage->GetLayout(PEImageLayout::LAYOUT_FLAT, 0);
        if (layout != NULL)
        {
            //cache a bunch of PE metadata in the PEDecoder
            m_ILimage->CheckILFormat();

            //fudge this by a few pages to make sure we can still mess with the PE headers
            const size_t fudgeSize = 4096 * 4;
            ClrVirtualProtect((void*)(((char *)layout->GetBase()) + fudgeSize),
                              layout->GetSize() - fudgeSize, 0, &ignored);
            layout->Release();
        }
#endif
    }
    else
    {
        ExternalLog(LL_WARNING, "Native image is not correct version.");
    }
}

#endif  // FEATURE_PREJIT

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
// Logging
// ------------------------------------------------------------
#ifdef FEATURE_PREJIT
void PEAssembly::ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    PEFile::ExternalVLog(facility, level, fmt, args);


    RETURN;
}

void PEAssembly::FlushExternalLog()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;


    RETURN;
}
#endif //FEATURE_PREJIT
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
#ifdef FEATURE_PREJIT
    if(HasNativeImage())
        m_nativeImage->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED)->Release();
    else
#endif
        GetILimage()->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED)->Release();
}

void PEFile::SetupAssemblyLoadContext()
{
    PTR_ICLRPrivBinder pBindingContext = GetBindingContext();
    ICLRPrivBinder* pOpaqueBinder = NULL;

    if (pBindingContext != NULL)
    {
        UINT_PTR assemblyBinderID = 0;
        IfFailThrow(pBindingContext->GetBinderID(&assemblyBinderID));

        pOpaqueBinder = reinterpret_cast<ICLRPrivBinder*>(assemblyBinderID);
    }

    m_pAssemblyLoadContext = (pOpaqueBinder != NULL) ?
        (AssemblyLoadContext*)pOpaqueBinder :
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
#ifdef FEATURE_PREJIT
    if (m_nativeImage.IsValid())
    {
        m_nativeImage->EnumMemoryRegions(flags);
        DacEnumHostDPtrMem(m_nativeImage->GetLoadedLayout()->GetNativeVersionInfo());
    }
#endif
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

// Returns the ICLRPrivBinder* instance associated with the PEFile
PTR_ICLRPrivBinder PEFile::GetBindingContext()
{
    LIMITED_METHOD_CONTRACT;

    PTR_ICLRPrivBinder pBindingContext = NULL;

    // CoreLibrary is always bound in context of the TPA Binder. However, since it gets loaded and published
    // during EEStartup *before* DefaultContext Binder (aka TPAbinder) is initialized, we dont have a binding context to publish against.
    if (!IsSystem())
    {
        pBindingContext = dac_cast<PTR_ICLRPrivBinder>(GetHostAssembly());
        if (!pBindingContext)
        {
            // If we do not have any binding context, check if we are dealing with
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
