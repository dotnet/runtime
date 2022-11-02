// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEAssembly.h
//

// --------------------------------------------------------------------------------


#ifndef PEASSEMBLY_H_
#define PEASSEMBLY_H_

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

class PEAssembly;
class SimpleRWLock;

typedef DPTR(PEAssembly) PTR_PEAssembly;

// --------------------------------------------------------------------------------
// Types
// --------------------------------------------------------------------------------

// --------------------------------------------------------------------------------
// A PEAssembly is an input to the CLR loader.  It is produced as a result of
// binding, usually through fusion (although there are a few less common methods to
// obtain one which do not go through fusion, e.g. IJW loads)
//
// Although a PEAssembly is usually a disk based PE file, it is not
// always the case. Thus it is a conscious decision to not export access to the PE
// file directly; rather the specific information required should be provided via
// individual query API.
//
// There are multiple "flavors" of PEAssemblies:
//
// 1. HMODULE - these PE Files are loaded in response to "spontaneous" OS callbacks.
//    These should only occur for .exe main modules and IJW dlls loaded via LoadLibrary
//    or static imports in umnanaged code.
//    These get their PEImage loaded directly in PEImage::CreateFromHMODULE(HMODULE hMod)
//
// 2. Assemblies loaded directly or indirectly by the managed code - these are the most
//    common case.  A path is obtained from assembly binding and the result is loaded
//    via PEImage:
//      a. Display name loads - these are metadata-based binds
//      b. Path loads - these are loaded from an explicit path
//
// 3. Byte arrays - loaded explicitly by user code.  These also go through PEImage.
//
// 4. Dynamic - these are not actual PE images at all, but are placeholders
//    for reflection-based modules.
//
// See also file:..\inc\corhdr.h#ManagedHeader for more on the format of managed images.
// --------------------------------------------------------------------------------

class PEAssembly final
{
public:

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

#if CHECK_INVARIANTS
    CHECK Invariant();
#endif

    // ------------------------------------------------------------
    // Identity
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    BOOL Equals(PEAssembly *pPEAssembly);
    BOOL Equals(PEImage *pImage);
#endif // DACCESS_COMPILE

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

    LPCWSTR GetPathForErrorMessages();

    // Codebase is the fusion codebase or path for the assembly.  It is in URL format.
    // Note this may be obtained from the parent PEAssembly if we don't have a path or fusion
    // assembly.
    BOOL GetCodeBase(SString& result);

    // Full name is the most descriptive name available (path, codebase, or name as appropriate)
    void GetPathOrCodeBase(SString& result);

    // Display name is the fusion binding name for an assembly
    void GetDisplayName(SString& result, DWORD flags = 0);

#ifdef LOGGING
    // This is useful for log messages
    LPCWSTR GetDebugName();
#endif

    // ------------------------------------------------------------
    // Checks
    // ------------------------------------------------------------

    void ValidateForExecution();
    BOOL IsMarkedAsNoPlatform();

    // ------------------------------------------------------------
    // Classification
    // ------------------------------------------------------------

    BOOL IsSystem() const;
    BOOL IsDynamic() const;

    // ------------------------------------------------------------
    // Metadata access
    // ------------------------------------------------------------

    IMDInternalImport *GetMDImport();

#ifndef DACCESS_COMPILE
    IMetaDataEmit *GetEmitter();
    IMetaDataImport2 *GetRWImporter();
#else
    TADDR GetMDInternalRWAddress();
#endif // DACCESS_COMPILE

    void ConvertMDInternalToReadWrite();

    void GetMVID(GUID* pMvid);
    ULONG GetHashAlgId();
    HRESULT GetVersion(USHORT* pMajor, USHORT* pMinor, USHORT* pBuild, USHORT* pRevision);
    BOOL IsStrongNamed();
    LPCUTF8 GetSimpleName();
    HRESULT GetScopeName(LPCUTF8 * pszName);
    const void *GetPublicKey(DWORD *pcbPK);
    LPCSTR GetLocale();
    DWORD GetFlags();

    // ------------------------------------------------------------
    // PE file access
    // ------------------------------------------------------------

    BOOL IsReadyToRun();
    mdToken GetEntryPointToken();

    BOOL IsILOnly();
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
    ULONG GetPEImageTimeDateStamp();

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

    BOOL HasPEImage()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_PEImage != NULL;
    }

    PEImage* GetPEImage()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_PEImage;
    }

    void EnsureLoaded();

    BOOL HasLoadedPEImage()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return HasPEImage() && GetPEImage()->HasLoadedLayout();
    }

    PTR_PEImageLayout GetLoadedLayout()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        _ASSERTE(HasPEImage());
        return GetPEImage()->GetLoadedLayout();
    };

    BOOL IsLoaded()
    {
        return IsDynamic() || HasLoadedPEImage();
    }

    BOOL IsPtrInPEImage(PTR_CVOID data);

    // For IJW purposes only - this asserts that we have an IJW image.
    HMODULE GetIJWBase();

    // The debugger can tolerate a null value here for native only loading cases
    PTR_VOID GetDebuggerContents(COUNT_T* pSize = NULL);

#ifndef DACCESS_COMPILE
    // Returns the IL image range; may force a LoadLibrary
    const void* GetManagedFileContents(COUNT_T* pSize = NULL);
#endif // DACCESS_COMPILE

    PTR_CVOID GetLoadedImageContents(COUNT_T* pSize = NULL);

    // ------------------------------------------------------------
    // Resource access
    // ------------------------------------------------------------

    void GetEmbeddedResource(DWORD dwOffset, DWORD *cbResource, PBYTE *pbInMemoryResource);

    // ------------------------------------------------------------
    // File loading
    // ------------------------------------------------------------

    PEAssembly * LoadAssembly(mdAssemblyRef kAssemblyRef);

    // ------------------------------------------------------------
    // Assembly Binder and host assembly (BINDER_SPACE::Assembly)
    // ------------------------------------------------------------

    bool HasHostAssembly()
    {
        STATIC_CONTRACT_WRAPPER;
        return GetHostAssembly() != NULL;
    }

    // Returns a non-AddRef'ed BINDER_SPACE::Assembly*
    PTR_BINDER_SPACE_Assembly GetHostAssembly()
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        return m_pHostAssembly;
    }

    // Returns the AssemblyBinder* instance associated with the PEAssembly
    // which owns the context into which the current PEAssembly was loaded.
    // For Dynamic assemblies this is the fallback binder.
    PTR_AssemblyBinder GetAssemblyBinder();

#ifndef DACCESS_COMPILE
    void SetFallbackBinder(PTR_AssemblyBinder pFallbackBinder)
    {
        LIMITED_METHOD_CONTRACT;
        m_pFallbackBinder = pFallbackBinder;
    }

#endif //!DACCESS_COMPILE

    ULONG HashIdentity();

    PTR_AssemblyBinder GetFallbackBinder()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pFallbackBinder;
    }

    // ------------------------------------------------------------
    // Creation entry points
    // ------------------------------------------------------------

    static PEAssembly* Open(
        PEImage* pPEImageIL,
        BINDER_SPACE::Assembly* pHostAssembly);

    // This opens the canonical System.Private.CoreLib.dll
    static PEAssembly* OpenSystem();

    static PEAssembly* Open(BINDER_SPACE::Assembly* pBindResult);

    static PEAssembly* Create(IMetaDataAssemblyEmit* pEmit);

      // ------------------------------------------------------------
      // Utility functions
      // ------------------------------------------------------------

      static void PathToUrl(SString& string);
      static void UrlToPath(SString& string);
      static BOOL FindLastPathSeparator(const SString& path, SString::Iterator& i);

private:
    // ------------------------------------------------------------
    // Loader access API
    // ------------------------------------------------------------

    // Private helper for crufty exception handling reasons
    static PEAssembly* DoOpenSystem();

    // ------------------------------------------------------------
    // Internal routines
    // ------------------------------------------------------------

#ifdef DACCESS_COMPILE
    // just to make the DAC and GCC happy.
    ~PEAssembly() {};
    PEAssembly() = default;
#else
    PEAssembly(
        BINDER_SPACE::Assembly* pBindResultInfo,
        IMetaDataEmit* pEmit,
        BOOL isSystem,
        PEImage* pPEImageIL = NULL,
        BINDER_SPACE::Assembly* pHostAssembly = NULL
    );

    ~PEAssembly();
#endif

    void OpenMDImport();
    void OpenImporter();
    void OpenEmitter();

private:

 // ------------------------------------------------------------
 // Instance fields
 // ------------------------------------------------------------

#ifdef _DEBUG
    LPCWSTR                 m_pDebugName;
    SString                 m_debugName;
#endif

    // IL image, NULL if dynamic
    PTR_PEImage              m_PEImage;

    // This flag is not updated atomically with m_pMDImport. Its fine for debugger usage
    // but don't rely on it in the runtime. In runtime try QI'ing the m_pMDImport for
    // IID_IMDInternalImportENC
    BOOL                     m_MDImportIsRW_Debugger_Use_Only;

    union
    {
#ifndef DACCESS_COMPILE
        IMDInternalImport* m_pMDImport;
#else
        // NB: m_pMDImport_UseAccessor appears to be never assigned a value, but its purpose is just
        //     to be a placeholder that has the same type and offset as m_pMDImport.
        //
        //     The field has a different name so it would be an error to use directly.
        //     Only GetMDInternalRWAddress is supposed to use it via (TADDR)m_pMDImport_UseAccessor,
        //     which at that point will match the m_pMDImport on the debuggee side.
        //     See more scary comments in GetMDInternalRWAddress.
        IMDInternalImport* m_pMDImport_UseAccessor;
#endif
    };

    IMetaDataImport2* m_pImporter;
    IMetaDataEmit* m_pEmitter;

    Volatile<LONG>           m_refCount;
    bool                     m_isSystem;

    PTR_BINDER_SPACE_Assembly m_pHostAssembly;

    // For certain assemblies, we do not have m_pHostAssembly since they are not bound using an actual binder.
    // An example is Ref-Emitted assemblies. Thus, when such assemblies trigger load of their dependencies,
    // we need to ensure they are loaded in appropriate load context.
    //
    // To enable this, we maintain a concept of "FallbackBinder", which will be set to the Binder of the
    // assembly that created the dynamic assembly. If the creator assembly is dynamic itself, then its fallback
    // load context would be propagated to the assembly being dynamically generated.
    PTR_AssemblyBinder m_pFallbackBinder;

};  // class PEAssembly

typedef ReleaseHolder<PEAssembly> PEAssemblyHolder;

#endif  // PEASSEMBLY_H_
