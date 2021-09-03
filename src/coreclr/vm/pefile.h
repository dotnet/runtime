// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

#include "sstring.h"
#include "peimage.h"
#include "metadata.h"
#include "corhlpr.h"
#include "utilcode.h"
#include "loaderheap.h"
#include "sstring.h"
#include "ex.h"
#include "assemblyspecbase.h"
#include "eecontract.h"
#include "metadatatracker.h"
#include "stackwalktypes.h"
#include <specstrings.h>
#include "slist.h"
#include "eventtrace.h"

#include "assemblybinderutil.h"

// --------------------------------------------------------------------------------
// Forward declared classes
// --------------------------------------------------------------------------------

class Module;
class EditAndContinueModule;

class PEFile;
class PEModule;
class PEAssembly;
class SimpleRWLock;
class AssemblyLoadContext;

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

public:
    void LoadLibrary(BOOL allowNativeSkip = TRUE);


private:
    // For use inside LoadLibrary callback
    void SetLoadedHMODULE(HMODULE hMod);

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


    void GetMVID(GUID *pMvid);

    // ------------------------------------------------------------
    // Descriptive strings
    // ------------------------------------------------------------

    // Path is the file path to the file; empty if not a file
    const SString& GetPath();
    const SString& GetIdentityPath();

#ifdef DACCESS_COMPILE
    // This is the metadata module name. Used as a hint as file name.
    const SString &GetModuleFileNameHint();
#endif // DACCESS_COMPILE

    // Full name is the most descriptive name available (path, codebase, or name as appropriate)
    void GetCodeBaseOrName(SString &result);

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
    // Returns self (if assembly) or containing assembly (if module)
    PEAssembly *GetAssembly() const;

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
    // ------------------------------------------------------------
    // PE file access
    // ------------------------------------------------------------

    BOOL IsIbcOptimized();
    BOOL IsReadyToRun();
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
                     BOOL fSkipRaiseResolveEvent, DomainAssembly* pDomainAssembly,
                     AppDomain* pAppDomain);
#ifndef DACCESS_COMPILE
    PTR_CVOID GetMetadata(COUNT_T *pSize);
#endif
    PTR_CVOID GetLoadedMetadata(COUNT_T *pSize);

    void GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine);

    ULONG GetILImageTimeDateStamp();


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

    // ------------------------------------------------------------
    // Native image access
    // ------------------------------------------------------------

    // Does the loader support using a native image for this file?
    // Some implementation restrictions prevent native images from being used
    // in some cases.
    PTR_PEImageLayout GetLoaded();
    PTR_PEImageLayout GetLoadedIL();
    IStream * GetPdbStream();
    void ClearPdbStream();
    BOOL IsLoaded(BOOL bAllowNativeSkip=TRUE) ;
    BOOL IsPtrInILImage(PTR_CVOID data);

    // ------------------------------------------------------------
    // Resource access
    // ------------------------------------------------------------

    void GetEmbeddedResource(DWORD dwOffset, DWORD *cbResource, PBYTE *pbInMemoryResource);

    // ------------------------------------------------------------
    // File loading
    // ------------------------------------------------------------

    PEAssembly * LoadAssembly(
            mdAssemblyRef       kAssemblyRef,
            IMDInternalImport * pImport = NULL);

protected:
    // ------------------------------------------------------------
    // Internal constants
    // ------------------------------------------------------------

    enum
    {
        PEFILE_SYSTEM                 = 0x01,
        PEFILE_ASSEMBLY               = 0x02,
        PEFILE_MODULE                 = 0x04,
    };

    // ------------------------------------------------------------
    // Internal routines
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    PEFile(PEImage *image);
    virtual ~PEFile();
#else
    virtual ~PEFile() {}
#endif

    void OpenMDImport();
    void OpenMDImport_Unsafe();
    void OpenImporter();
    void OpenEmitter();

    void ReleaseMetadataInterfaces(BOOL bDestructor);


    friend class Module;

#ifndef DACCESS_COMPILE
    void EnsureImageOpened();
#endif // DACCESS_COMPILE

    friend class ClrDataAccess;

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
    SimpleRWLock            *m_pMetadataLock;
    Volatile<LONG>           m_refCount;
    int                      m_flags;

    // AssemblyLoadContext that this PEFile is associated with
    PTR_AssemblyLoadContext  m_pAssemblyLoadContext;

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

    LPCWSTR GetPathForErrorMessages();

    static PEFile* Dummy();

    void ConvertMDInternalToReadWrite();

protected:
    PTR_BINDER_SPACE_Assembly m_pHostAssembly;

    // For certain assemblies, we do not have m_pHostAssembly since they are not bound using an actual binder.
    // An example is Ref-Emitted assemblies. Thus, when such assemblies trigger load of their dependencies,
    // we need to ensure they are loaded in appropriate load context.
    //
    // To enable this, we maintain a concept of "Fallback LoadContext", which will be set to the Binder of the
    // assembly that created the dynamic assembly. If the creator assembly is dynamic itself, then its fallback
    // load context would be propagated to the assembly being dynamically generated.
    PTR_AssemblyBinder m_pFallbackLoadContextBinder;

public:
    // Returns a non-AddRef'ed BINDER_SPACE::Assembly*
    PTR_BINDER_SPACE_Assembly GetHostAssembly()
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        return m_pHostAssembly;
    }

    // Returns the AssemblyBinder* instance associated with the PEFile
    PTR_AssemblyBinder GetBindingContext();

#ifndef DACCESS_COMPILE
    void SetupAssemblyLoadContext();

    void SetFallbackLoadContextBinder(PTR_AssemblyBinder pFallbackLoadContextBinder)
    {
        LIMITED_METHOD_CONTRACT;
        m_pFallbackLoadContextBinder = pFallbackLoadContextBinder;
        SetupAssemblyLoadContext();
    }

#endif //!DACCESS_COMPILE

    // Returns AssemblyLoadContext into which the current PEFile was loaded.
    PTR_AssemblyLoadContext GetAssemblyLoadContext()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_pAssemblyLoadContext != NULL);
        return m_pAssemblyLoadContext;
    }

    bool HasHostAssembly()
    { STATIC_CONTRACT_WRAPPER; return GetHostAssembly() != nullptr; }

    PTR_AssemblyBinder GetFallbackLoadContextBinder()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pFallbackLoadContextBinder;
    }
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

    // CoreCLR's PrivBinder PEAssembly creation entrypoint
    static PEAssembly * Open(
        PEAssembly *       pParent,
        PEImage *          pPEImageIL,
        BINDER_SPACE::Assembly * pHostAssembly);

    // This opens the canonical System.Private.CoreLib.dll
    static PEAssembly *OpenSystem();
#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    static PEAssembly *Open(
        CoreBindResult* pBindResult,
        BOOL isSystem);

    static PEAssembly *Create(
        PEAssembly *pParentAssembly,
        IMetaDataAssemblyEmit *pEmit);

  private:
    // Private helpers for crufty exception handling reasons
    static PEAssembly *DoOpenSystem();

  public:

    // ------------------------------------------------------------
    // binding & source
    // ------------------------------------------------------------

    ULONG HashIdentity();

    // ------------------------------------------------------------
    // Descriptive strings
    // ------------------------------------------------------------

    // This returns a non-empty path representing the source of the assembly; it may
    // be the parent assembly for dynamic or memory assemblies
    const SString &GetEffectivePath();

    // Codebase is the fusion codebase or path for the assembly.  It is in URL format.
    // Note this may be obtained from the parent PEFile if we don't have a path or fusion
    // assembly.
    BOOL GetCodeBase(SString &result);

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

  protected:

#ifndef DACCESS_COMPILE
    PEAssembly(
        CoreBindResult* pBindResultInfo,
        IMetaDataEmit *pEmit,
        PEFile *creator,
        BOOL system,
        PEImage * pPEImageIL = NULL,
        BINDER_SPACE::Assembly * pHostAssembly = NULL
        );
    virtual ~PEAssembly();
#endif

  private:
    // ------------------------------------------------------------
    // Instance fields
    // ------------------------------------------------------------

    PTR_PEFile               m_creator;
};


typedef ReleaseHolder<PEFile> PEFileHolder;

typedef ReleaseHolder<PEAssembly> PEAssemblyHolder;

#endif  // PEFILE_H_
