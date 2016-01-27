// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEImage.h
// 

// --------------------------------------------------------------------------------


#ifndef PEIMAGE_H_
#define PEIMAGE_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

#include "clrtypes.h"
#include "peimagelayout.h"
#include "sstring.h"
#include "holder.h"
#include "pefingerprint.h"

class SimpleRWLock;
// --------------------------------------------------------------------------------
// Forward declarations
// --------------------------------------------------------------------------------

class Crst;
class Thread;

Thread* GetThreadNULLOk();

// --------------------------------------------------------------------------------
// PEImage is a PE file loaded by our "simulated LoadLibrary" mechanism.  A PEImage 
// can be loaded either FLAT (same layout as on disk) or MAPPED (PE sections 
// mapped into virtual addresses.)
// 
// The MAPPED format is currently limited to "IL only" images - this can be checked
// for via PEDecoder::IsILOnlyImage.
//
// NOTE: PEImage will NEVER call LoadLibrary.
// --------------------------------------------------------------------------------



#define CV_SIGNATURE_RSDS   0x53445352

// CodeView RSDS debug information -> PDB 7.00
struct CV_INFO_PDB70 
{
    DWORD      magic; 
    GUID       signature;       // unique identifier 
    DWORD      age;             // an always-incrementing value 
    char       path[MAX_LONGPATH];  // zero terminated string with the name of the PDB file 
};

typedef DPTR(class PEImage)                PTR_PEImage;

class PEImage 
{
    friend class PEModule;
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    friend class CCLRDebugManager;
#endif // FEATURE_INCLUDE_ALL_INTERFACES
public:
    // ------------------------------------------------------------
    // Public constants
    // ------------------------------------------------------------

    enum
    {
        LAYOUT_CREATEIFNEEDED=1
    };
    PTR_PEImageLayout GetLayout(DWORD imageLayoutMask,DWORD flags); //with ref
    PTR_PEImageLayout GetLoadedLayout(); //no ref
    PTR_PEImageLayout GetLoadedIntrospectionLayout(); //no ref, introspection only
    BOOL IsOpened();
    BOOL HasLoadedLayout();
    BOOL HasLoadedIntrospectionLayout();
    

public:
    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    static void Startup();

    // Normal constructed PEImages do NOT share images between calls and
    // cannot be accessed by Get methods.
    //
    // DO NOT USE these unless you want a private copy-on-write mapping of
    // the file.



public:
    ~PEImage();
    PEImage();

#ifndef DACCESS_COMPILE
    static PTR_PEImage LoadFlat(
        const void *flat,
        COUNT_T size);
#ifndef FEATURE_PAL
    static PTR_PEImage LoadImage(
        HMODULE hMod);
#endif // !FEATURE_PAL        
    static PTR_PEImage OpenImage(
        LPCWSTR pPath,
        MDInternalImportFlags flags = MDInternalImport_Default);

#ifdef FEATURE_FUSION
    static PTR_PEImage OpenImage(
        IStream *pIStream,
        UINT64 uStreamAsmId,
        DWORD dwModuleId,
        BOOL resourceFile,
        MDInternalImportFlags flags = MDInternalImport_Default);
#endif

    // clones the image with new flags (this is pretty much about cached / noncached difference)
    void Clone(MDInternalImportFlags flags, PTR_PEImage* ppImage)
    {
        if (GetPath().IsEmpty())
        {
            AddRef();
            *ppImage = this;
        }
        else
            *ppImage = PEImage::OpenImage(GetPath(), flags);

    };

#ifdef FEATURE_HOSTED_BINDER
    // pUnkResource must be one of the ICLRPrivResource* interfaces defined in CLRPrivBinding.IDL.
    // pUnkResource will be queried for each of these to find a match and 
    static PEImage * OpenImage(
        ICLRPrivResource * pIResource,
        MDInternalImportFlags flags = MDInternalImport_Default);
#endif

    static PTR_PEImage FindById(UINT64 uStreamAsmId, DWORD dwModuleId);
    static PTR_PEImage FindByPath(LPCWSTR pPath);    
    static PTR_PEImage FindByShortPath(LPCWSTR pPath);
    static PTR_PEImage FindByLongPath(LPCWSTR pPath);
    void AddToHashMap();

    void   Load();
    void   SetLoadedHMODULE(HMODULE hMod);
    void   LoadNoMetaData(BOOL bIntrospection);
    void   LoadNoFile();
    void   LoadFromMapped();  
    void   LoadForIntrospection();

    void AllocateLazyCOWPages();
#endif
    
    BOOL   HasID();
    ULONG GetIDHash();
    
    PTR_CVOID GetStrongNameSignature(COUNT_T *pSize = NULL);
    

    // Refcount above images.
    ULONG AddRef();
    ULONG Release();

    // Accessors
    const SString &GetPath();
    BOOL IsFile();
    HANDLE GetFileHandle();
    HANDLE GetFileHandleLocking();
    void SetFileHandle(HANDLE hFile);
    HRESULT TryOpenFile();    

    HANDLE GetProtectingFileHandle(BOOL bProtectIfNotOpenedYet);

    LPCWSTR GetPathForErrorMessages();

    // Equality
    BOOL Equals(PEImage *pImage);
    static ULONG HashStreamIds(UINT64 id1, DWORD id2);

    // Hashing utilities.  (These require a flat version of the file, and 
    // will open one if necessary.)

#ifndef DACCESS_COMPILE
    void GetImageBits(DWORD layout, SBuffer &result);
#endif

    void ComputeHash(ALG_ID algorithm, SBuffer &result);
    CHECK CheckHash(ALG_ID algorithm, const void *pbHash, COUNT_T cbHash);

    void GetMVID(GUID *pMvid);
    const BOOL HasV1Metadata();
    IMDInternalImport* GetMDImport();
    BOOL MDImportLoaded();
    IMDInternalImport* GetNativeMDImport(BOOL loadAllowed = TRUE);    

    BOOL HasSecurityDirectory();
    BOOL HasContents() ;
    BOOL HasNativeHeader() ;
    BOOL IsPtrInImage(PTR_CVOID data);
    CHECK CheckFormat();

    // Check utilites
    CHECK CheckILFormat();
#ifdef FEATURE_PREJIT    
    CHECK CheckNativeFormat();    
#endif // FEATURE_PREJIT
    static CHECK CheckCanonicalFullPath(const SString &path);
    static CHECK CheckStartup();
    PTR_CVOID GetMetadata(COUNT_T *pSize = NULL);
    void GetHashedStrongNameSignature(SBuffer &result);

#ifndef FEATURE_PAL
    static void GetPathFromDll(HINSTANCE hMod, SString &result);
#endif // !FEATURE_PAL    
    static LocaleID GetFileSystemLocale();
    static BOOL PathEquals(const SString &p1, const SString &p2);
    BOOL IsTrustedNativeImage(){LIMITED_METHOD_CONTRACT; return m_bIsTrustedNativeImage;};
    void SetIsTrustedNativeImage(){LIMITED_METHOD_CONTRACT; m_bIsTrustedNativeImage=TRUE;};
    BOOL IsNativeImageInstall(){LIMITED_METHOD_CONTRACT; return m_bIsNativeImageInstall;}
    void SetIsNativeImageInstall(){LIMITED_METHOD_CONTRACT; m_bIsNativeImageInstall=TRUE;};

    void SetModuleFileNameHintForDAC();
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    const SString &GetModuleFileNameHintForDAC();
#endif

    const BOOL HasNTHeaders();
    const BOOL HasCorHeader(); 
    const BOOL HasReadyToRunHeader();
    void SetPassiveDomainOnly();
    BOOL PassiveDomainOnly();
    BOOL IsReferenceAssembly();
#ifdef FEATURE_PREJIT  
    const BOOL GetNativeILHasSecurityDirectory();
    const BOOL IsNativeILILOnly();
    const BOOL IsNativeILDll();
    void GetNativeILPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine);
    PTR_CVOID GetNativeManifestMetadata(COUNT_T *pSize = NULL);
#endif
    const BOOL HasDirectoryEntry(int entry);
    const mdToken GetEntryPointToken();
    const DWORD GetCorHeaderFlags(); 
    const BOOL IsILOnly();
    const BOOL IsDll();
    const WORD GetSubsystem();
    BOOL  IsFileLocked();
    const BOOL HasStrongNameSignature();
#ifndef DACCESS_COMPILE
    const HRESULT VerifyStrongName(DWORD* verifyOutputFlags);    
#endif

    BOOL IsStrongNameSigned();
    BOOL IsIbcOptimized();
    BOOL Has32BitNTHeaders();

    void VerifyIsAssembly();
    void VerifyIsNIAssembly();

#ifndef FEATURE_CORECLR
    BOOL IsReportedToUsageLog();
    void SetReportedToUsageLog();
#ifndef DACCESS_COMPILE
    HRESULT GetILFingerprint(IILFingerprint **ppFingerprint);
#endif //!DACCESS_COMPILE
#endif //!FEATURE_CORECLR

    static void GetAll(SArray<PEImage*> &images);

private:
#ifndef DACCESS_COMPILE
    // Get or create the layout corresponding to the mask, with an AddRef
    PTR_PEImageLayout GetLayoutInternal(DWORD imageLayoutMask, DWORD flags); 
#endif
    // Get an existing layout corresponding to the mask, no AddRef
    PTR_PEImageLayout GetExistingLayoutInternal(DWORD imageLayoutMask);

    void OpenMDImport();
    void OpenNativeMDImport();    
    // ------------------------------------------------------------
    // Private routines
    // ------------------------------------------------------------

    void  Init(LPCWSTR pPath);
    void  Init(IStream* pStream, UINT64 uStreamAsmId,
               DWORD dwModuleId, BOOL resourceFile);

    void VerifyIsILOrNIAssembly(BOOL fIL);

    struct PEImageLocator
    {
#ifdef FEATURE_FUSION
        BOOL m_fIsIStream;
        DWORD m_dwStreamModuleId;
        UINT64 m_StreamAsmId;
#endif

        LPCWSTR m_pPath;

#ifdef FEATURE_FUSION
        PEImageLocator(LPCWSTR pPath)
            : m_fIsIStream(FALSE), m_pPath(pPath)
        {
        }

        PEImageLocator(UINT64 uStreamAsmId, DWORD dwModuleId)
            : m_fIsIStream(TRUE), m_dwStreamModuleId(dwModuleId), m_StreamAsmId(uStreamAsmId)
        {
        }

        PEImageLocator(PEImage * pImage)
            : m_fIsIStream(pImage->m_fIsIStream),
              m_dwStreamModuleId(pImage->m_dwStreamModuleId),
              m_StreamAsmId(pImage->m_StreamAsmId),
              m_pPath(pImage->m_path.GetUnicode())
        {
        }
#else // FEATURE_FUSION
        PEImageLocator(LPCWSTR pPath)
            : m_pPath(pPath)
        {
        }

        PEImageLocator(PEImage * pImage)
            : m_pPath(pImage->m_path.GetUnicode())
        {
        }
#endif // FEATURE_FUSION
    };

    static BOOL CompareImage(UPTR image1, UPTR image2);
#ifdef FEATURE_MIXEDMODE
    static BOOL CompareIJWDataBase(UPTR base, UPTR mapping);
#endif // FEATURE_MIXEDMODE

    void DECLSPEC_NORETURN ThrowFormat(HRESULT hr);

    static CHECK CheckLayoutFormat(PEDecoder *pe);

    // ------------------------------------------------------------
    // Instance members
    // ------------------------------------------------------------

    SString     m_path;
    LONG        m_refCount;

    // This variable will have the data of module name. 
    // It is only used by DAC to remap fusion loaded modules back to 
    // disk IL. This really is a workaround. The real fix is for fusion loader
    // hook (public API on hosting) to take an additional file name hint.
    // We are piggy backing on the fact that module name is the same as file name!!!
    //
    SString     m_sModuleFileNameHintUsedByDac; // This is only used by DAC
private:
    BOOL        m_bIsTrustedNativeImage;
    BOOL        m_bIsNativeImageInstall;
    BOOL        m_bPassiveDomainOnly;
#ifndef FEATURE_CORECLR
    BOOL        m_fReportedToUsageLog;
#endif // !FEATURE_CORECLR
#ifdef FEATURE_LAZY_COW_PAGES
    BOOL        m_bAllocatedLazyCOWPages;
#endif // FEATURE_LAZY_COW_PAGES

protected:

    enum 
    {
        IMAGE_FLAT=0,
        IMAGE_MAPPED=1,
        IMAGE_LOADED=2,
        IMAGE_LOADED_FOR_INTROSPECTION=3,
        IMAGE_COUNT=4
    };
    
    SimpleRWLock *m_pLayoutLock;
    PTR_PEImageLayout m_pLayouts[IMAGE_COUNT] ;
    BOOL      m_bInHashMap;
#ifndef DACCESS_COMPILE    
    void   SetLayout(DWORD dwLayout, PTR_PEImageLayout pLayout);
#endif // DACCESS_COMPILE


#ifdef METADATATRACKER_DATA
    class MetaDataTracker   *m_pMDTracker;
#endif // METADATATRACKER_DATA

    IMDInternalImport* m_pMDImport;
    IMDInternalImport* m_pNativeMDImport;

#ifdef FEATURE_FUSION
    UINT64      m_StreamAsmId;
    DWORD       m_dwStreamModuleId;
    BOOL        m_fIsIStream;
#endif

private:


    // ------------------------------------------------------------
    // Static members
    // ------------------------------------------------------------

    static CrstStatic   s_hashLock;

    static PtrHashMap   *s_Images;

    HANDLE m_hFile;
    bool   m_bOwnHandle;

    BOOL        m_bSignatureInfoCached;
    HRESULT   m_hrSignatureInfoStatus;
    DWORD        m_dwSignatureInfo;    
#ifdef FEATURE_MIXEDMODE
    //@TODO:workaround: Remove this when we have one PEImage per mapped image,
    //@TODO:workaround: and move the lock there
    // This is for IJW thunk initialization, as it is no longer guaranteed
    // that the initialization will occur under the loader lock.
    static CrstStatic   s_ijwHashLock;
    static PtrHashMap   *s_ijwFixupDataHash;

public:
    class IJWFixupData
    {
    private:
        Crst            m_lock;
        void           *m_base;
        DWORD           m_flags;
        PTR_LoaderHeap  m_DllThunkHeap;

        // the fixup for the next iteration in FixupVTables
        // we use it to make sure that we do not try to fix up the same entry twice
        // if there was a pass that was aborted in the middle
        COUNT_T         m_iNextFixup;
        COUNT_T         m_iNextMethod;

        enum {
            e_FIXED_UP = 0x1
        };

    public:
        IJWFixupData(void *pBase);
        ~IJWFixupData();
        void *GetBase() { LIMITED_METHOD_CONTRACT; return m_base; }
        Crst *GetLock() { LIMITED_METHOD_CONTRACT; return &m_lock; }
        BOOL IsFixedUp() { LIMITED_METHOD_CONTRACT; return m_flags & e_FIXED_UP; }
        void SetIsFixedUp() { LIMITED_METHOD_CONTRACT; m_flags |= e_FIXED_UP; }
        PTR_LoaderHeap  GetThunkHeap();
        void MarkMethodFixedUp(COUNT_T iFixup, COUNT_T iMethod);
        BOOL IsMethodFixedUp(COUNT_T iFixup, COUNT_T iMethod);
    };

    static IJWFixupData *GetIJWData(void *pBase);
    static PTR_LoaderHeap GetDllThunkHeap(void *pBase);
    static void UnloadIJWModule(void *pBase);
#endif //FEATURE_MIXEDMODE
private:
    DWORD m_dwPEKind;
    DWORD m_dwMachine;
    BOOL  m_fCachedKindAndMachine;

#ifdef FEATURE_APTCA
    BOOL  m_fMayBeConditionalAptca;
#endif // FEATURE_APTCA

#ifdef FEATURE_FUSION
    PEFingerprint *m_pILFingerprint; // has to be the real type (as opposed to an interface) so we can delete it
#endif // FEATURE_FUSION

public:
    void CachePEKindAndMachine();
    void GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine);
#ifdef FEATURE_FUSION	
    PEKIND GetFusionProcessorArchitecture();
#endif

#ifdef FEATURE_APTCA
    inline BOOL MayBeConditionalAptca();
    inline void SetIsNotConditionalAptca();
#endif // FEATURE_APTCA
};

FORCEINLINE void PEImageRelease(PEImage *i)
{
    WRAPPER_NO_CONTRACT;
    i->Release();
}

typedef Wrapper<PEImage *, DoNothing, PEImageRelease> PEImageHolder;

// ================================================================================
// Inline definitions
// ================================================================================

#include "peimage.inl"

#endif  // PEIMAGE_H_
