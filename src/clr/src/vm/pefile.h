//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// --------------------------------------------------------------------------------
// PEFile.h
// 

// --------------------------------------------------------------------------------


#ifndef PEFILE_H_
#define PEFILE_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

#include <windef.h>

#include <corpolicy.h>
#include "sstring.h"
#include "peimage.h"
#include "metadata.h"
#include "corhlpr.h"
#include "utilcode.h"
#include "loaderheap.h"
#include "sstring.h"
#include "ex.h"
#ifdef FEATURE_FUSION
#include <fusion.h>
#include <fusionbind.h>
#include "binderngen.h"
#endif
#include "assemblyspecbase.h"
#include "eecontract.h"
#include "metadatatracker.h"
#include "stackwalktypes.h"
#include <specstrings.h>
#include "slist.h"
#include "corperm.h"
#include "eventtrace.h"

#ifdef FEATURE_HOSTED_BINDER
#include "clrprivbinderutil.h"
#endif

// --------------------------------------------------------------------------------
// Forward declared classes
// --------------------------------------------------------------------------------

class Module;
class EditAndContinueModule;

class PEFile;
class PEModule;
class PEAssembly;
class SimpleRWLock;

class CLRPrivBinderLoadFile;

typedef VPTR(PEModule) PTR_PEModule;
typedef VPTR(PEAssembly) PTR_PEAssembly;

// --------------------------------------------------------------------------------
// Types
// --------------------------------------------------------------------------------

// --------------------------------------------------------------------------------
// A PEFile is an input to the CLR loader.  It is produced as a result of 
// binding, usually through fusion (although there are a few less common methods to 
// obtain one which do not go through fusion, e.g. IJW loads)
//
// Although a PEFile is usually a disk based PE file (hence the name), it is not 
// always the case. Thus it is a conscious decision to not export access to the PE
// file directly; rather the specific information required should be provided via
// individual query API.
//
// There are multiple "flavors" of PEFiles:
//
// 1. HMODULE - these PE Files are loaded in response to "spontaneous" OS callbacks.
//    These should only occur for .exe main modules and IJW dlls loaded via LoadLibrary
//    or static imports in umnanaged code.
//
// 2. Fusion loads - these are the most common case.  A path is obtained from fusion and
//    the result is loaded via PEImage.
//      a. Display name loads - these are metadata-based binds
//      b. Path loads - these are loaded from an explicit path
//
// 3. Byte arrays - loaded explicitly by user code.  These also go through PEImage.
//
// 4. Dynamic - these are not actual PE images at all, but are placeholders
//    for reflection-based modules.
//
// PEFiles are segmented into two subtypes: PEAssembly and PEModule.  The formere
// is a file to be loaded as an assembly, and the latter is to be loaded as a module.
// 
// See also file:..\inc\corhdr.h#ManagedHeader for more on the format of managed images.
// See code:Module for more on modules 
// --------------------------------------------------------------------------------

typedef VPTR(class PEFile) PTR_PEFile;

typedef ReleaseHolder<IMDInternalImport> IMDInternalImportHolder;

class PEFile
{
    // ------------------------------------------------------------
    // SOS support
    // ------------------------------------------------------------
    VPTR_BASE_CONCRETE_VTABLE_CLASS(PEFile)

public:

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

#if CHECK_INVARIANTS
    CHECK Invariant();
#endif

private:
    // ------------------------------------------------------------
    // Loader access API
    // ------------------------------------------------------------

    friend class DomainFile;
    friend class PEModule;
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    // Load actually triggers loading side effects of the module.  This should ONLY
    // be done after validation has been passed
    BOOL CanLoadLibrary();
public:
    void LoadLibrary(BOOL allowNativeSkip = TRUE);

#ifdef FEATURE_MIXEDMODE
protected:
    // Returns TRUE if this file references managed CRT (msvcmNN*).
    BOOL ReferencesManagedCRT();
    
    // Checks for unsupported loads of C++/CLI assemblies into multiple runtimes in this process.
    void CheckForDisallowedInProcSxSLoad();
#endif // FEATURE_MIXEDMODE

private:
    void CheckForDisallowedInProcSxSLoadWorker();
    void ValidateImagePlatformNeutrality();

    // For use inside LoadLibrary callback
    friend HRESULT ExecuteDLLForAttach(HINSTANCE hInst,
                                       DWORD dwReason,
                                       LPVOID lpReserved,
                                       BOOL fFromThunk);
    void SetLoadedHMODULE(HMODULE hMod);

    BOOL HasSkipVerification();
    void SetSkipVerification();

    // DO NOT USE !!! this is to be removed when we move to new fusion binding API
    friend class DomainAssembly;

    // Helper for creating metadata for CreateDynamic
    friend class Assembly;
    friend class COMDynamicWrite;
    friend class AssemblyNative;
    static void DefineEmitScope(
        GUID   iid, 
        void **ppEmit);

protected:
    IMDInternalImportHolder GetMDImport();

public:
    // ------------------------------------------------------------
    // Generic PEFile - can be used to access metadata
    // ------------------------------------------------------------
    
    static PEFile *Open(PEImage *image);

    // ------------------------------------------------------------
    // Identity
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    BOOL Equals(PEFile *pFile);
    BOOL Equals(PEImage *pImage);
#endif // DACCESS_COMPILE

#ifndef FEATURE_CORECLR
    BOOL  IsShareable();
#endif

    void GetMVID(GUID *pMvid);

    // ------------------------------------------------------------
    // Descriptive strings
    // ------------------------------------------------------------

    // Path is the file path to the file; empty if not a file
    const SString &GetPath();

#ifdef DACCESS_COMPILE
    // This is the metadata module name. Used as a hint as file name. 
    const SString &GetModuleFileNameHint();
#endif // DACCESS_COMPILE

    // Full name is the most descriptive name available (path, codebase, or name as appropriate)
    void GetCodeBaseOrName(SString &result);
    
    // Returns security information for the assembly based on the codebase
    void GetSecurityIdentity(SString &codebase, SecZone *pdwZone, DWORD dwFlags, BYTE *pbUniqueID, DWORD *pcbUniqueID);
    void InitializeSecurityManager();

#ifdef LOGGING
    // This is useful for log messages
    LPCWSTR GetDebugName();
#endif

    // ------------------------------------------------------------
    // Checks
    // ------------------------------------------------------------

    CHECK CheckLoaded(BOOL allowNativeSkip = TRUE);
    void ValidateForExecution();
    BOOL IsMarkedAsNoPlatform();
    BOOL IsMarkedAsContentTypeWindowsRuntime();


    // ------------------------------------------------------------
    // Classification
    // ------------------------------------------------------------

    BOOL IsAssembly() const;
    PTR_PEAssembly AsAssembly();
    BOOL IsModule() const;
    PTR_PEModule AsModule();
    BOOL IsSystem() const;
    BOOL IsDynamic() const;
    BOOL IsResource() const;
    BOOL IsIStream() const;
    BOOL IsIntrospectionOnly() const;
    // Returns self (if assembly) or containing assembly (if module)
    PEAssembly *GetAssembly() const;

    // ------------------------------------------------------------
    // Hash support
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    void GetImageBits(SBuffer &result);
    void GetHash(ALG_ID algorithm, SBuffer &result);
#endif // DACCESS_COMPILE

    void GetSHA1Hash(SBuffer &result);
    CHECK CheckHash(ALG_ID algorithm, const void *hash, COUNT_T size);

    // ------------------------------------------------------------
    // Metadata access
    // ------------------------------------------------------------

    BOOL HasMetadata();

    IMDInternalImport *GetPersistentMDImport();
    IMDInternalImport *GetMDImportWithRef();    
    void MakeMDImportPersistent() {m_bHasPersistentMDImport=TRUE;};

#ifndef DACCESS_COMPILE
    IMetaDataEmit *GetEmitter();
    IMetaDataAssemblyEmit *GetAssemblyEmitter();
    IMetaDataImport2 *GetRWImporter();
    IMetaDataAssemblyImport *GetAssemblyImporter();
#else
    TADDR GetMDInternalRWAddress();
#endif // DACCESS_COMPILE

    LPCUTF8 GetSimpleName();
    HRESULT GetScopeName(LPCUTF8 * pszName);
    BOOL IsStrongNameVerified();
    BOOL IsStrongNamed();
    const void *GetPublicKey(DWORD *pcbPK);
    ULONG GetHashAlgId();
    HRESULT GetVersion(USHORT *pMajor, USHORT *pMinor, USHORT *pBuild, USHORT *pRevision);
    LPCSTR GetLocale();
    DWORD GetFlags();
    HRESULT GetFlagsNoTrigger(DWORD * pdwFlags);
#ifdef FEATURE_CAS_POLICY
    COR_TRUST *GetAuthenticodeSignature();
#endif
    // ------------------------------------------------------------
    // PE file access
    // ------------------------------------------------------------

    BOOL HasSecurityDirectory();
    BOOL IsIbcOptimized();
    WORD GetSubsystem();
    mdToken GetEntryPointToken(
#ifdef _DEBUG        
        BOOL bAssumeLoaded = FALSE
#endif //_DEBUG               
        );
    BOOL IsILOnly();
    BOOL IsDll();

    TADDR GetIL(RVA il);

    PTR_VOID GetRvaField(RVA field);
    CHECK CheckRvaField(RVA field);
    CHECK CheckRvaField(RVA field, COUNT_T size);

    PCCOR_SIGNATURE GetSignature(RVA signature);
    RVA GetSignatureRva(PCCOR_SIGNATURE signature);
    CHECK CheckSignature(PCCOR_SIGNATURE signature);
    CHECK CheckSignatureRva(RVA signature);

    BOOL HasTls();
    BOOL IsRvaFieldTls(RVA field);
    UINT32 GetFieldTlsOffset(RVA field);
    UINT32 GetTlsIndex();

    const void *GetInternalPInvokeTarget(RVA target);
    CHECK CheckInternalPInvokeTarget(RVA target);

    IMAGE_COR_VTABLEFIXUP *GetVTableFixups(COUNT_T *pCount = NULL);
    void *GetVTable(RVA rva);

    BOOL GetResource(LPCSTR szName, DWORD *cbResource,
                     PBYTE *pbInMemoryResource, DomainAssembly** pAssemblyRef,
                     LPCSTR *szFileName, DWORD *dwLocation, 
                     StackCrawlMark *pStackMark, BOOL fSkipSecurityCheck,
                     BOOL fSkipRaiseResolveEvent, DomainAssembly* pDomainAssembly,
                     AppDomain* pAppDomain);
#ifndef DACCESS_COMPILE
    PTR_CVOID GetMetadata(COUNT_T *pSize);
#endif
    PTR_CVOID GetLoadedMetadata(COUNT_T *pSize);

    void GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine);

    ULONG GetILImageTimeDateStamp();

#ifdef FEATURE_CAS_POLICY
    SAFEHANDLE GetSafeHandle();
#endif // FEATURE_CAS_POLICY

    // ------------------------------------------------------------
    // Image memory access
    //
    // WARNING: do not abuse these.  There are scenarios where the image
    // is not in memory as an optimization.  
    //
    // In general, you should add an entry point to get the specific info 
    // you are interested in, rather than using these general purpose 
    // entry points.  The info can then be extracted from the native image
    // in the no-IL image case.
    // ------------------------------------------------------------

    // For IJW purposes only - this asserts that we have an IJW image.
    HMODULE GetIJWBase();

    // The debugger can tolerate a null value here for native only loading cases
    PTR_VOID GetDebuggerContents(COUNT_T *pSize = NULL);

#ifndef DACCESS_COMPILE
    // Returns the IL image range; may force a LoadLibrary
    const void *GetManagedFileContents(COUNT_T *pSize = NULL);
#endif // DACCESS_COMPILE

    PTR_CVOID GetLoadedImageContents(COUNT_T *pSize = NULL);
    
    // SetInProcSxSLoadVerified can run concurrently as we don't hold locks during LoadLibrary but
    // it is the only flag that can be set during this phase so no mutual exclusion is necessary.
    void SetInProcSxSLoadVerified() { LIMITED_METHOD_CONTRACT; m_flags |= PEFILE_SXS_LOAD_VERIFIED; }
    BOOL IsInProcSxSLoadVerified() { LIMITED_METHOD_CONTRACT; return m_flags & PEFILE_SXS_LOAD_VERIFIED; }

    // ------------------------------------------------------------
    // Native image access
    // ------------------------------------------------------------

    // Does the loader support using a native image for this file?
    // Some implementation restrictions prevent native images from being used
    // in some cases.
#ifdef FEATURE_PREJIT 
    BOOL CanUseNativeImage() { LIMITED_METHOD_CONTRACT; return m_fCanUseNativeImage; }
    void SetCannotUseNativeImage() { LIMITED_METHOD_CONTRACT; m_fCanUseNativeImage = FALSE; }
    void SetNativeImageUsedExclusively() { LIMITED_METHOD_CONTRACT; m_flags|=PEFILE_NATIVE_IMAGE_USED_EXCLUSIVELY; }
    BOOL IsNativeImageUsedExclusively() { LIMITED_METHOD_CONTRACT; return m_flags&PEFILE_NATIVE_IMAGE_USED_EXCLUSIVELY; }
    void SetSafeToHardBindTo() { LIMITED_METHOD_CONTRACT; m_flags|=PEFILE_SAFE_TO_HARDBINDTO; }
    BOOL IsSafeToHardBindTo() { LIMITED_METHOD_CONTRACT; return m_flags&PEFILE_SAFE_TO_HARDBINDTO; }

    BOOL IsNativeLoaded();
    PEImage *GetNativeImageWithRef();
    PEImage *GetPersistentNativeImage();
#endif
    BOOL HasNativeImage();
    PTR_PEImageLayout GetLoaded();
    PTR_PEImageLayout GetLoadedNative();
    PTR_PEImageLayout GetLoadedIL();    
    PTR_PEImageLayout GetAnyILWithRef();        //AddRefs!
    IStream * GetPdbStream();       
    void ClearPdbStream();
    BOOL IsLoaded(BOOL bAllowNativeSkip=TRUE) ;
    BOOL PassiveDomainOnly();
    BOOL IsPtrInILImage(PTR_CVOID data);

#ifdef DACCESS_COMPILE    
    PEImage *GetNativeImage()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_PREJIT 
        return m_nativeImage;
#else
        return NULL;
#endif
    }
#endif

#ifdef FEATURE_PREJIT 
    // ------------------------------------------------------------
    // Native image config utilities
    // ------------------------------------------------------------

    static CorCompileConfigFlags GetNativeImageConfigFlags(BOOL fForceDebug = FALSE,
                                                           BOOL fForceProfiling = FALSE,
                                                           BOOL fForceInstrument = FALSE);
    
    static CorCompileConfigFlags GetNativeImageConfigFlagsWithOverrides();

#ifdef DEBUGGING_SUPPORTED
    static void SetNGENDebugFlags(BOOL fAllowOpt);
    static void GetNGENDebugFlags(BOOL *fAllowOpt);
#endif

#ifdef FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
    static BOOL ShouldTreatNIAsMSIL();
#endif // FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
            
#endif  // FEATURE_PREJIT

    // ------------------------------------------------------------
    // Resource access
    // ------------------------------------------------------------

    void GetEmbeddedResource(DWORD dwOffset, DWORD *cbResource, PBYTE *pbInMemoryResource);

    // ------------------------------------------------------------
    // File loading
    // ------------------------------------------------------------

    PEAssembly * LoadAssembly(
            mdAssemblyRef       kAssemblyRef, 
            IMDInternalImport * pImport = NULL, 
            LPCUTF8             szWinRtTypeNamespace = NULL, 
            LPCUTF8             szWinRtTypeClassName = NULL);

    // ------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------

    // The format string is intentionally unicode to avoid globalization bugs
#ifdef FEATURE_PREJIT
    void ExternalLog(DWORD facility, DWORD level, const WCHAR *fmt, ...) DAC_EMPTY();
    void ExternalLog(DWORD level, const WCHAR *fmt, ...) DAC_EMPTY();
    void ExternalLog(DWORD level, const char *msg) DAC_EMPTY();
    virtual void ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args) DAC_EMPTY();
    virtual void FlushExternalLog() DAC_EMPTY();
#endif

protected:
    // ------------------------------------------------------------
    // Internal constants
    // ------------------------------------------------------------

    enum
    { 
        PEFILE_SYSTEM                 = 0x01,
        PEFILE_ASSEMBLY               = 0x02,
        PEFILE_MODULE                 = 0x04,
        PEFILE_SKIP_VERIFICATION      = 0x08,
        PEFILE_SKIP_MODULE_HASH_CHECKS= 0x10,
        PEFILE_ISTREAM                = 0x100,
#ifdef FEATURE_PREJIT        
        PEFILE_HAS_NATIVE_IMAGE_METADATA = 0x200,
        PEFILE_NATIVE_IMAGE_USED_EXCLUSIVELY =0x1000,
        PEFILE_SAFE_TO_HARDBINDTO     = 0x4000, // NGEN-only flag
#endif        
        PEFILE_INTROSPECTIONONLY      = 0x400,
        PEFILE_SXS_LOAD_VERIFIED      = 0x2000
    };

    // ------------------------------------------------------------
    // Internal routines
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    PEFile(PEImage *image, BOOL fCheckAuthenticodeSignature = TRUE);
    virtual ~PEFile();

    virtual void ReleaseIL();
#endif

    void OpenMDImport();
    void RestoreMDImport(IMDInternalImport* pImport);
    void OpenMDImport_Unsafe();
    void OpenImporter();
    void OpenAssemblyImporter();
    void OpenEmitter();
    void OpenAssemblyEmitter();

    void ConvertMDInternalToReadWrite();
    void ReleaseMetadataInterfaces(BOOL bDestructor, BOOL bKeepNativeData=FALSE);

#ifdef FEATURE_CAS_POLICY
    // Check the Authenticode signature of a PE file
    void CheckAuthenticodeSignature();
#endif // FEATURE_CAS_POLICY

    friend class Module;
#ifdef FEATURE_PREJIT
    void SetNativeImage(PEImage *nativeImage);
#ifndef DACCESS_COMPILE
    virtual void ClearNativeImage();
#endif
#endif

#ifndef DACCESS_COMPILE
    void EnsureImageOpened();
#endif // DACCESS_COMPILE

    friend class ClrDataAccess;
    BOOL HasNativeImageMetadata();

    // ------------------------------------------------------------
    // Instance fields
    // ------------------------------------------------------------
    
#ifdef _DEBUG
    LPCWSTR                 m_pDebugName;
    SString                 m_debugName;
#endif
    
    // Identity image
    PTR_PEImage              m_identity;
    // IL image, NULL if we didn't need to open the file
    PTR_PEImage              m_openedILimage;
#ifdef FEATURE_PREJIT
    // Native image
    PTR_PEImage              m_nativeImage;

    BOOL                     m_fCanUseNativeImage;
#endif
    // This flag is not updated atomically with m_pMDImport. Its fine for debugger usage
    // but don't rely on it in the runtime. In runtime try QI'ing the m_pMDImport for 
    // IID_IMDInternalImportENC
    BOOL                     m_MDImportIsRW_Debugger_Use_Only;
    Volatile<BOOL>           m_bHasPersistentMDImport;
    
#ifndef DACCESS_COMPILE
    IMDInternalImport       *m_pMDImport;
#else
    IMDInternalImport       *m_pMDImport_UseAccessor;
#endif
    IMetaDataImport2        *m_pImporter;
    IMetaDataEmit           *m_pEmitter;
#ifndef FEATURE_CORECLR
    IMetaDataAssemblyImport *m_pAssemblyImporter;
    IMetaDataAssemblyEmit   *m_pAssemblyEmitter;
#endif
    SimpleRWLock            *m_pMetadataLock;
    Volatile<LONG>           m_refCount;
    SBuffer                 *m_hash;                   // cached SHA1 hash value
    int                     m_flags;
    BOOL                    m_fStrongNameVerified;
#ifdef FEATURE_CAS_POLICY
    COR_TRUST               *m_certificate;
    BOOL                    m_fCheckedCertificate;
    IInternetSecurityManager    *m_pSecurityManager;
    Crst                         m_securityManagerLock;
#endif // FEATURE_CAS_POLICY

#ifdef DEBUGGING_SUPPORTED
#ifdef FEATURE_PREJIT
    SVAL_DECL(DWORD, s_NGENDebugFlags);
#endif
#endif
public:

    PTR_PEImage GetILimage()
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_TRIGGERS;
        }
        CONTRACTL_END;
#ifndef DACCESS_COMPILE
        if (m_openedILimage == NULL && m_identity != NULL)
        {
            PEImage* pOpenedILimage;
            m_identity->Clone(MDInternalImport_Default,&pOpenedILimage);
            if (InterlockedCompareExchangeT(&m_openedILimage,pOpenedILimage,NULL) != NULL)
                pOpenedILimage->Release();
        }
#endif
        return m_openedILimage;
    }

    PEImage *GetOpenedILimage()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(HasOpenedILimage());
        return m_openedILimage;
    }


    BOOL HasOpenedILimage()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_openedILimage != NULL;

    }

    BOOL HasLoadedIL()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return HasOpenedILimage() &&  GetOpenedILimage()->HasLoadedLayout();
    }

    BOOL IsDesignerBindingContext()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_HOSTED_BINDER
        DWORD binderFlags = BINDER_NONE;

        HRESULT hr = E_FAIL;
        if (HasHostAssembly())
            hr = GetHostAssembly()->GetBinderFlags(&binderFlags);

        return hr == S_OK ? binderFlags & BINDER_DESIGNER_BINDING_CONTEXT : FALSE;
#else
        return FALSE;
#endif
    }

    LPCWSTR GetPathForErrorMessages();

    static PEFile* Dummy();
    void MarkNativeImageInvalidIfOwned();
    void ConvertMetadataToRWForEnC();

#if defined(FEATURE_VERSIONING) || defined(FEATURE_HOSTED_BINDER)
protected:
    PTR_ICLRPrivAssembly m_pHostAssembly;
#endif

#ifdef FEATURE_HOSTED_BINDER
protected:

    friend class CLRPrivBinderFusion;
#ifndef DACCESS_COMPILE
    // CLRPrivBinderFusion calls this for Fusion-bound assemblies in AppX processes.
    void SetHostAssembly(ICLRPrivAssembly * pHostAssembly)
    { LIMITED_METHOD_CONTRACT; m_pHostAssembly = clr::SafeAddRef(pHostAssembly); }
#endif //DACCESS_COMPILE

public:
    // Returns a non-AddRef'ed ICLRPrivAssembly*
    PTR_ICLRPrivAssembly GetHostAssembly()
    { 
        STATIC_CONTRACT_LIMITED_METHOD; 
        return m_pHostAssembly; 
    }

    // Returns the ICLRPrivBinder* instance associated with the PEFile
    PTR_ICLRPrivBinder GetBindingContext();
    
    bool HasHostAssembly()
    { STATIC_CONTRACT_WRAPPER; return GetHostAssembly() != nullptr; }

    bool CanUseWithBindingCache()
    { LIMITED_METHOD_CONTRACT; return !HasHostAssembly(); }
#endif // FEATURE_HOSTED_BINDER
};  // class PEFile


class PEAssembly : public PEFile
{ 
    VPTR_VTABLE_CLASS(PEAssembly, PEFile)

  public:
    // ------------------------------------------------------------
    // Statics initialization.
    // ------------------------------------------------------------
    static
    void Attach();

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

#if defined(FEATURE_HOSTED_BINDER)
#if !defined(FEATURE_CORECLR)
    static PEAssembly * Open(
        PEAssembly *       pParentAssembly,
        PEImage *          pPEImageIL, 
        PEImage *          pPEImageNI, 
        ICLRPrivAssembly * pHostAssembly, 
        BOOL               fIsIntrospectionOnly);
    
    static PEAssembly * Open(
        PEAssembly * pParentAssembly, 
        PEImage *    pPEImageIL, 
        BOOL         isIntrospectionOnly = FALSE);
#else //!FEATURE_CORECLR
    // CoreCLR's PrivBinder PEAssembly creation entrypoint
    static PEAssembly * Open(
        PEAssembly *       pParent,
        PEImage *          pPEImageIL, 
        PEImage *          pPEImageNI, 
        ICLRPrivAssembly * pHostAssembly, 
        BOOL               fIsIntrospectionOnly = FALSE);
#endif //!FEATURE_CORECLR
#endif //FEATURE_HOSTED_BINDER

    // This opens the canonical mscorlib.dll
#ifdef FEATURE_FUSION
    static PEAssembly *OpenSystem(IApplicationContext *pAppCtx);
#else
    static PEAssembly *OpenSystem(IUnknown *pAppCtx);
#endif
#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

#ifdef FEATURE_FUSION
    static PEAssembly *Open(
        IAssembly *pIAssembly,
        IBindResult *pNativeFusionAssembly,
        IFusionBindLog *pFusionLog = NULL,
        BOOL isSystemAssembly = FALSE,
        BOOL isIntrospectionOnly = FALSE);

    static PEAssembly *Open(
        IHostAssembly *pIHostAssembly,
        BOOL isSystemAssembly = FALSE,
        BOOL isIntrospectionOnly = FALSE);

#ifdef FEATURE_MIXEDMODE
    // Use for main exe loading
    // NOTE: This may also be used for "spontaneous" (IJW) dll loading where
    //      we need to deliver DllMain callbacks, but we should eliminate this case

    static PEAssembly *OpenHMODULE(
        HMODULE hMod,
        IAssembly *pFusionAssembly,
        IBindResult *pNativeFusionAssembly,
        IFusionBindLog *pFusionLog = NULL,
        BOOL isIntrospectionOnly = FALSE);
#endif // FEATURE_MIXEDMODE

    static PEAssembly *DoOpen(
        IAssembly *pIAssembly,
        IBindResult *pNativeFusionAssembly,
        IFusionBindLog *pFusionLog,
        BOOL isSystemAssembly,
        BOOL isIntrospectionOnly = FALSE);

    static PEAssembly *DoOpen(
        IHostAssembly *pIHostAssembly,
        BOOL isSystemAssembly,
        BOOL isIntrospectionOnly = FALSE);
#ifdef FEATURE_MIXEDMODE
    static PEAssembly *DoOpenHMODULE(
        HMODULE hMod,
        IAssembly *pFusionAssembly,
        IBindResult *pNativeFusionAssembly,
        IFusionBindLog *pFusionLog,
        BOOL isIntrospectionOnly = FALSE);
#endif // FEATURE_MIXEDMODE
#else
    static PEAssembly *Open(
        CoreBindResult* pBindResult,
        BOOL isSystem,
        BOOL isIntrospectionOnly);
#endif // FEATURE_FUSION

    static PEAssembly *Create(
        PEAssembly *pParentAssembly,
        IMetaDataAssemblyEmit *pEmit,
        BOOL isIntrospectionOnly);

    static PEAssembly *OpenMemory(
        PEAssembly *pParentAssembly,
        const void *flat,
        COUNT_T size, 
        BOOL isIntrospectionOnly = FALSE,
        CLRPrivBinderLoadFile* pBinderToUse = NULL);

    static PEAssembly *DoOpenMemory(
        PEAssembly *pParentAssembly,
        const void *flat,
        COUNT_T size,
        BOOL isIntrospectionOnly,
        CLRPrivBinderLoadFile* pBinderToUse);

  private:
    // Private helpers for crufty exception handling reasons
#ifdef FEATURE_FUSION
    static PEAssembly *DoOpenSystem(IApplicationContext *pAppCtx);
#else
    static PEAssembly *DoOpenSystem(IUnknown *pAppCtx);
#endif

  public:

    // ------------------------------------------------------------
    // binding & source
    // ------------------------------------------------------------

    BOOL IsSourceGAC();
#ifdef FEATURE_CORECLR
    BOOL IsProfileAssembly();
    BOOL IsSilverlightPlatformStrongNameSignature();
    BOOL IsProfileTestAssembly();
    BOOL IsTestKeySignature();
#endif // FEATURE_CORECLR

    ULONG HashIdentity();
#ifdef FEATURE_FUSION

    BOOL FusionLoggingEnabled() 
    {
        LIMITED_METHOD_CONTRACT;
        return m_bFusionLogEnabled && (m_pFusionLog != NULL);
    };
    void DisableFusionLogging()
    {
        m_bFusionLogEnabled = FALSE;
    };

    IFusionBindLog *GetFusionBindLog()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_FUSION
        return (m_bFusionLogEnabled && (m_pFusionLog != NULL)) ? m_pFusionLog : NULL;
#else
        return NULL;
#endif 
    }


    BOOL IsBindingCodeBase();

    BOOL IsSourceDownloadCache();

    LOADCTX_TYPE GetLoadContext();
    BOOL IsContextLoad();

    // Can we avoid exposing these?
    IAssembly *GetFusionAssembly(); 
    IHostAssembly *GetIHostAssembly(); 
    IAssemblyName *GetFusionAssemblyName();
    IAssemblyName *GetFusionAssemblyNameNoCreate();
    IAssemblyLocation* GetNativeAssemblyLocation();
    DWORD GetLocationFlags();
    PEKIND GetFusionProcessorArchitecture();
#endif

#ifndef  DACCESS_COMPILE
    virtual void ReleaseIL();
#endif

    // ------------------------------------------------------------
    // Hash support
    // ------------------------------------------------------------

    BOOL NeedsModuleHashChecks();

    BOOL HasStrongNameSignature();
    BOOL IsFullySigned();

    void SetStrongNameBypassed();
    void VerifyStrongName();

    // ------------------------------------------------------------
    // Descriptive strings
    // ------------------------------------------------------------

    // This returns a non-empty path representing the source of the assembly; it may 
    // be the parent assembly for dynamic or memory assemblies
    const SString &GetEffectivePath();

    // Codebase is the fusion codebase or path for the assembly.  It is in URL format.
    // Note this may be obtained from the parent PEFile if we don't have a path or fusion
    // assembly.
    //
    // fCopiedName means to get the "shadow copied" path rather than the original path, if applicable
    void GetCodeBase(SString &result, BOOL fCopiedName = FALSE);
    // Get the fully qualified assembly name from its metadata token 
    static void GetFullyQualifiedAssemblyName(IMDInternalImport* pImport, mdAssembly mda, SString &result, DWORD flags = 0); 
    
    // Display name is the fusion binding name for an assembly
    void GetDisplayName(SString &result, DWORD flags = 0);

    // ------------------------------------------------------------
    // Metadata access
    // ------------------------------------------------------------

    LPCUTF8 GetSimpleName();

    // ------------------------------------------------------------
    // Utility functions
    // ------------------------------------------------------------

    static void PathToUrl(SString &string);
    static void UrlToPath(SString &string);
    static BOOL FindLastPathSeparator(const SString &path, SString::Iterator &i);

    // ------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------
#ifdef FEATURE_PREJIT
    void ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args) DAC_EMPTY();
    void FlushExternalLog() DAC_EMPTY();
#ifdef FEATURE_FUSION
    void ETWTraceLogMessage(DWORD dwETWLogCategory, PEAssembly *pAsm)
    {
        LIMITED_METHOD_CONTRACT
        if (FusionLoggingEnabled() && 
            (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEFUSION_KEYWORD)))
        { 
            m_pFusionLog->ETWTraceLogMessage(dwETWLogCategory, (pAsm?pAsm->m_pFusionAssemblyName:NULL));
        }
    }
    ULONGLONG GetBindingID()
    {
        LIMITED_METHOD_CONTRACT;
        ULONGLONG ullBindingID = 0;
        if (FusionLoggingEnabled())
            m_pFusionLog->GetBindingID(&ullBindingID);
        return ullBindingID;
    }
#endif
#endif

#ifndef FEATURE_CORECLR
    BOOL IsReportedToUsageLog();
    void SetReportedToUsageLog();
#endif // !FEATURE_CORECLR

  protected:

#ifndef DACCESS_COMPILE
#ifdef FEATURE_FUSION
    PEAssembly(
        PEImage *image,
        IMetaDataEmit *pEmit,
        IAssembly *pIAssembly,
        IBindResult *pNativeFusionAssembly,
        PEImage *pNIImage,
        IFusionBindLog *pFusionLog,
        IHostAssembly *pIHostAssembly,
        PEFile *creator,
        BOOL system,
        BOOL introspectionOnly = FALSE,
        ICLRPrivAssembly * pHostAssembly = NULL);
#else
    PEAssembly(
        CoreBindResult* pBindResultInfo, 
        IMetaDataEmit *pEmit,
        PEFile *creator, 
        BOOL system, 
        BOOL introspectionOnly = FALSE
#ifdef FEATURE_HOSTED_BINDER
        ,
        PEImage * pPEImageIL = NULL,
        PEImage * pPEImageNI = NULL,
        ICLRPrivAssembly * pHostAssembly = NULL
#endif
        );
#endif
    virtual ~PEAssembly();
#endif

    // ------------------------------------------------------------
    // Loader access API
    // ------------------------------------------------------------

    friend class DomainAssembly;
#ifdef FEATURE_PREJIT

#ifdef FEATURE_FUSION
    void SetNativeImage(IBindResult *pNativeFusionAssembly);
#else
    void SetNativeImage(PEImage *image);
#endif

    BOOL CheckNativeImageVersion(PEImage *image);


#ifdef FEATURE_FUSION
    void ClearNativeImage();    
    void SetNativeImageClosure(IAssemblyBindingClosure *pClosure)
    {
        LIMITED_METHOD_CONTRACT;
        if (m_pNativeImageClosure!=NULL)
            m_pNativeImageClosure->Release();
        if (pClosure)
            pClosure->AddRef();
        m_pNativeImageClosure=pClosure;
    };
    BOOL HasEqualNativeClosure(DomainAssembly* pDomainAssembly);
#endif //FEATURE_FUSION

#endif  // FEATURE_PREJIT

  private:
    // Check both the StrongName and Authenticode signature of an assembly. If the application is using
    // strong name bypass, then this call may not result in a strong name verificaiton. VerifyStrongName
    // should be called if a strong name must be forced to verify.
    void DoLoadSignatureChecks();


  private:
    // ------------------------------------------------------------
    // Instance fields
    // ------------------------------------------------------------

    PTR_PEFile               m_creator;
#ifdef FEATURE_FUSION
    IAssemblyName           *m_pFusionAssemblyName;
    IAssembly               *m_pFusionAssembly;
    IFusionBindLog          *m_pFusionLog;
    BOOL                     m_bFusionLogEnabled;
    IHostAssembly           *m_pIHostAssembly;
    IAssemblyLocation       *m_pNativeAssemblyLocation;
    IAssemblyBindingClosure *m_pNativeImageClosure; //present only for shared 
    LOADCTX_TYPE             m_loadContext;
    DWORD                    m_dwLocationFlags;
#else
    BOOL m_bIsFromGAC;
    BOOL m_bIsOnTpaList;
    // Using a separate entry and not m_pHostAssembly because otherwise
    // HasHostAssembly becomes true that trips various other code paths resulting in bad
    // things
    SString                  m_sTextualIdentity;
#endif
#ifdef FEATURE_CORECLR
    int                      m_fProfileAssembly; // Tri-state cache
#else
    BOOL                     m_fStrongNameBypassed;
#endif

  public:
    PTR_PEFile GetCreator()
    { LIMITED_METHOD_CONTRACT; return m_creator; }

    // Returns TRUE if the assembly is .winmd file (WinRT assembly)
    bool IsWindowsRuntime();
    
    // Used to determine if this assembly has an identity that may be used for
    // binding purposes. Currently this is true for standard .NET assemblies
    // and false for WinRT assemblies (where assemblies are identified by their
    // member types).
    bool HasBindableIdentity();

    // Indicates if the assembly can be cached in a binding cache such as AssemblySpecBindingCache.
    inline bool CanUseWithBindingCache()
    {
#if defined(FEATURE_HOSTED_BINDER)
            STATIC_CONTRACT_WRAPPER;
#if !defined(FEATURE_APPX_BINDER)
            return (HasBindableIdentity());
#else
            return (PEFile::CanUseWithBindingCache() && HasBindableIdentity());
#endif // FEATURE_CORECLR
#else
            STATIC_CONTRACT_LIMITED_METHOD;
            return true;
#endif // FEATURE_HOSTED_BINDER
    }
};

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES

class PEModule : public PEFile
{
    VPTR_VTABLE_CLASS(PEModule, PEFile)
    
  public:

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    static PEModule *Open(PEAssembly *assembly, mdFile token,
                          const SString &fileName);

    static PEModule *OpenMemory(PEAssembly *assembly, mdFile kToken,
                                const void *flat, COUNT_T size); 

    static PEModule *Create(PEAssembly *assembly, mdFile kToken, IMetaDataEmit *pEmit);

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

  private:
    // Private helpers for crufty exception handling reasons
    static PEModule *DoOpen(PEAssembly *assembly, mdFile token,
                            const SString &fileName);

    static PEModule *DoOpenMemory(PEAssembly *assembly, mdFile kToken,
                                  const void *flat, COUNT_T size); 
  public:

    // ------------------------------------------------------------
    // Metadata access
    // ------------------------------------------------------------

    PEAssembly *GetAssembly();
    mdFile GetToken();
    BOOL IsResource();
    BOOL IsIStream();
    LPCUTF8 GetSimpleName();

    // ------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------
#ifdef FEATURE_PREJIT
    void ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args) DAC_EMPTY();
    void FlushExternalLog() DAC_EMPTY();
#endif
private:
    // ------------------------------------------------------------
    // Loader access API
    // ------------------------------------------------------------

    friend class DomainModule;
#ifdef FEATURE_PREJIT
    void SetNativeImage(const SString &fullPath);
#endif  // FEATURE_PREJIT

private:

#ifndef DACCESS_COMPILE
    PEModule(PEImage *image, PEAssembly *assembly, mdFile token, IMetaDataEmit *pEmit);
    virtual ~PEModule();
#endif

    // ------------------------------------------------------------
    // Instance fields
    // ------------------------------------------------------------

    PTR_PEAssembly          m_assembly;
    mdFile                  m_token;
    BOOL                    m_bIsResource;
};
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

typedef ReleaseHolder<PEFile> PEFileHolder;

typedef ReleaseHolder<PEAssembly> PEAssemblyHolder;

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
typedef ReleaseHolder<PEModule> PEModuleHolder;
#endif // FEATURE_MULTIMODULE_ASSEMBLIES


// A small shim around PEAssemblies/IBindResult that allow us to write Fusion/CLR-agnostic
// for logging native bind failures to the Fusion log/CLR log.
//
// These structures are stack-based, non-thread-safe and created for the duration of a single RuntimeVerify call.
// The methods are expected to compute their data lazily as they are only used in bind failures or in checked builds.
class LoggablePEAssembly : public LoggableAssembly
{
  public:
    virtual SString DisplayString()
    {
        STANDARD_VM_CONTRACT;

        return m_peAssembly->GetPath();
    }

#ifdef FEATURE_FUSION
    virtual IAssemblyName* FusionAssemblyName()
    {
        STANDARD_VM_CONTRACT;

        return m_peAssembly->GetFusionAssemblyName();
    }

    virtual IFusionBindLog* FusionBindLog()
    {
        STANDARD_VM_CONTRACT;

        return m_peAssembly->GetFusionBindLog();
    }
#endif // FEATURE_FUSION

    LoggablePEAssembly(PEAssembly *peAssembly)
    {
        LIMITED_METHOD_CONTRACT;

        m_peAssembly = peAssembly;
        peAssembly->AddRef();
    }

    ~LoggablePEAssembly()
    {
        LIMITED_METHOD_CONTRACT;

        m_peAssembly->Release();
    }

  private:
    PEAssembly  *m_peAssembly;
};


// ================================================================================
// Inline definitions
// ================================================================================


#endif  // PEFILE_H_
