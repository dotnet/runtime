// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include <bundle.h>

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
    BOOL IsOpened();
    BOOL HasLoadedLayout();

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
#ifndef TARGET_UNIX
    static PTR_PEImage LoadImage(
        HMODULE hMod);
#endif // !TARGET_UNIX
    static PTR_PEImage OpenImage(
        LPCWSTR pPath,
        MDInternalImportFlags flags = MDInternalImport_Default,
        BundleFileLocation bundleFileLocation = BundleFileLocation::Invalid());


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

    static PTR_PEImage FindById(UINT64 uStreamAsmId, DWORD dwModuleId);
    static PTR_PEImage FindByPath(LPCWSTR pPath,
                                  BOOL isInBundle = TRUE);
    static PTR_PEImage FindByShortPath(LPCWSTR pPath);
    static PTR_PEImage FindByLongPath(LPCWSTR pPath);
    void AddToHashMap();

    void   Load();
    void   SetLoadedHMODULE(HMODULE hMod);
    void   LoadNoMetaData();
    void   LoadNoFile();
    void   LoadFromMapped();
#endif

    BOOL   HasID();
    ULONG GetIDHash();

    // Refcount above images.
    ULONG AddRef();
    ULONG Release();

    // Accessors
    const SString &GetPath();
    const SString& GetPathToLoad();
    BOOL IsFile();
    BOOL IsInBundle() const;
    HANDLE GetFileHandle();
    INT64 GetOffset() const;
    INT64 GetSize() const;

    void SetFileHandle(HANDLE hFile);
    HRESULT TryOpenFile();

    LPCWSTR GetPathForErrorMessages();

    // Equality
    BOOL Equals(PEImage *pImage);

    void GetMVID(GUID *pMvid);
    BOOL HasV1Metadata();
    IMDInternalImport* GetMDImport();
    BOOL MDImportLoaded();
    IMDInternalImport* GetNativeMDImport(BOOL loadAllowed = TRUE);

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

#ifndef TARGET_UNIX
    static void GetPathFromDll(HINSTANCE hMod, SString &result);
#endif // !TARGET_UNIX
    static BOOL PathEquals(const SString &p1, const SString &p2);
    BOOL IsTrustedNativeImage(){LIMITED_METHOD_CONTRACT; return m_bIsTrustedNativeImage;};
    void SetIsTrustedNativeImage(){LIMITED_METHOD_CONTRACT; m_bIsTrustedNativeImage=TRUE;};

    void SetModuleFileNameHintForDAC();
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    const SString &GetModuleFileNameHintForDAC();
#endif

    BOOL HasNTHeaders();
    BOOL HasCorHeader();
    BOOL HasReadyToRunHeader();
    BOOL IsReferenceAssembly();
    BOOL IsComponentAssembly();
#ifdef FEATURE_PREJIT
    BOOL IsNativeILILOnly();
    BOOL IsNativeILDll();
    void GetNativeILPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine);
#endif
    PTR_CVOID GetNativeManifestMetadata(COUNT_T *pSize = NULL);
    BOOL HasDirectoryEntry(int entry);
    mdToken GetEntryPointToken();
    DWORD GetCorHeaderFlags();
    BOOL IsILOnly();
    BOOL IsDll();
    WORD GetSubsystem();
    BOOL  IsFileLocked();

    BOOL IsIbcOptimized();
    BOOL Has32BitNTHeaders();

    void VerifyIsAssembly();
    void VerifyIsNIAssembly();


    static void GetAll(SArray<PEImage*> &images);

private:
#ifndef DACCESS_COMPILE
    // Get or create the layout corresponding to the mask, with an AddRef
    PTR_PEImageLayout GetLayoutInternal(DWORD imageLayoutMask, DWORD flags);

    // Create the mapped layout
    PTR_PEImageLayout CreateLayoutMapped();

    // Create the flat layout
    PTR_PEImageLayout CreateLayoutFlat(BOOL bPermitWriteableSections);
#endif
    // Get an existing layout corresponding to the mask, no AddRef
    PTR_PEImageLayout GetExistingLayoutInternal(DWORD imageLayoutMask);

    void OpenMDImport();
    void OpenNativeMDImport();
    // ------------------------------------------------------------
    // Private routines
    // ------------------------------------------------------------

    void Init(LPCWSTR pPath, BundleFileLocation bundleFileLocation);

    void VerifyIsILOrNIAssembly(BOOL fIL);

    struct PEImageLocator
    {

        LPCWSTR m_pPath;
        BOOL m_bIsInBundle;

        PEImageLocator(LPCWSTR pPath, BOOL bIsInBundle)
            : m_pPath(pPath),
              m_bIsInBundle(bIsInBundle)
        {
        }

        PEImageLocator(PEImage * pImage)
            : m_pPath(pImage->m_path.GetUnicode())
        {
            m_bIsInBundle = pImage->IsInBundle();
        }
    };

    static BOOL CompareImage(UPTR image1, UPTR image2);
    static BOOL CompareIJWDataBase(UPTR base, UPTR mapping);

    void DECLSPEC_NORETURN ThrowFormat(HRESULT hr);

    static CHECK CheckLayoutFormat(PEDecoder *pe);

    // ------------------------------------------------------------
    // Instance members
    // ------------------------------------------------------------

    SString     m_path;
    LONG        m_refCount;

    BundleFileLocation m_bundleFileLocation; // If this image is located within a single-file bundle, 
                                             // the location within the bundle. If m_bundleFileLocation is valid,
                                             // it takes precedence over m_path for loading.

    // This variable will have the data of module name.
    // It is only used by DAC to remap fusion loaded modules back to
    // disk IL. This really is a workaround. The real fix is for fusion loader
    // hook (public API on hosting) to take an additional file name hint.
    // We are piggy backing on the fact that module name is the same as file name!!!
    //
    SString     m_sModuleFileNameHintUsedByDac; // This is only used by DAC
private:
    BOOL        m_bIsTrustedNativeImage;

protected:

    enum
    {
        IMAGE_FLAT=0,
        IMAGE_MAPPED=1,
        IMAGE_LOADED=2,
        IMAGE_COUNT=3
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


private:


    // ------------------------------------------------------------
    // Static members
    // ------------------------------------------------------------

    static CrstStatic   s_hashLock;

    static PtrHashMap   *s_Images;

    HANDLE m_hFile;
    bool   m_bOwnHandle;

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

private:
    DWORD m_dwPEKind;
    DWORD m_dwMachine;
    BOOL  m_fCachedKindAndMachine;



public:
    void CachePEKindAndMachine();
    void GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine);

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
