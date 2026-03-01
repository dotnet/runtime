// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEImageLayout.h
//

// --------------------------------------------------------------------------------


#ifndef PEIMAGELAYOUT_H_
#define PEIMAGELAYOUT_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

#include "clrtypes.h"
#include "pedecoder.h"
#include "holder.h"
#ifdef FEATURE_WEBCIL
#include "webcildecoder.h"
#endif

// --------------------------------------------------------------------------------
// Forward declarations
// --------------------------------------------------------------------------------

class Crst;
class PEImage;


typedef VPTR(class PEImageLayout) PTR_PEImageLayout;

class PEImageLayout
{
    VPTR_BASE_CONCRETE_VTABLE_CLASS(PEImageLayout)
public:
    // ------------------------------------------------------------
    // Image format discriminator
    // ------------------------------------------------------------
    enum ImageFormat
    {
        FORMAT_PE       = 0,
        FORMAT_WEBCIL   = 1,
    };

    // ------------------------------------------------------------
    // Public constants
    // ------------------------------------------------------------
    enum
    {
        LAYOUT_FLAT   = 2,
        LAYOUT_LOADED = 4,
        LAYOUT_ANY = 0xf
    };

public:
#ifndef DACCESS_COMPILE
    static PEImageLayout* CreateFromByteArray(PEImage* pOwner, const BYTE* array, COUNT_T size);
#ifndef TARGET_UNIX
    static PEImageLayout* CreateFromHMODULE(HMODULE hModule,PEImage* pOwner);
#endif
    static PEImageLayout* Load(PEImage* pOwner, HRESULT* loadFailure);
    static PEImageLayout* LoadFlat(PEImage* pOwner);
    static PEImageLayout* LoadConverted(PEImage* pOwner, bool disableMapping);
    static PEImageLayout* LoadNative(LPCWSTR fullPath);
#endif
    PEImageLayout();
    virtual ~PEImageLayout();
    static BOOL CompareBase(UPTR path, UPTR mapping);

    // Refcount above images.
    void AddRef();
    ULONG Release();

    void ApplyBaseRelocations(bool relocationMustWriteCopy);

    // ------------------------------------------------------------
    // Format query
    // ------------------------------------------------------------
    ImageFormat GetImageFormat() const { return m_format; }
#ifdef FEATURE_WEBCIL
    BOOL IsPEFormat() const { return m_format == FORMAT_PE; }
    BOOL IsWebcilFormat() const { return m_format == FORMAT_WEBCIL; }
#else
    BOOL IsPEFormat() const { return TRUE; }
    BOOL IsWebcilFormat() const { return FALSE; }
#endif

    // ------------------------------------------------------------
    // Generalized header checks (format-agnostic)
    // ------------------------------------------------------------
    BOOL HasHeaders() const;
    CHECK CheckHeaders() const;

    // ------------------------------------------------------------
    // Forwarding methods — delegate to the active decoder
    // These provide the same API surface as PEDecoder so that
    // all existing callers continue to work unchanged.
    // ------------------------------------------------------------

    // Basic properties
    PTR_VOID GetBase() const;
    BOOL IsMapped() const;
    BOOL IsRelocated() const;
    BOOL IsFlat() const;
    BOOL HasContents() const;
    COUNT_T GetSize() const;

    // Format checks
    CHECK CheckFormat() const;
    CHECK CheckNTFormat() const;
    CHECK CheckCORFormat() const;
    CHECK CheckILFormat() const;
    CHECK CheckILOnlyFormat() const;

    // NT header access
    BOOL HasNTHeaders() const;
    CHECK CheckNTHeaders() const;
    IMAGE_NT_HEADERS32 *GetNTHeaders32() const;
    IMAGE_NT_HEADERS64 *GetNTHeaders64() const;
    BOOL Has32BitNTHeaders() const;

    BOOL IsDll() const;
    BOOL HasBaseRelocations() const;
    const void *GetPreferredBase() const;
    COUNT_T GetVirtualSize() const;
    DWORD GetTimeDateStamp() const;
    WORD GetMachine() const;
    COUNT_T GetNumberOfSections() const;
    PTR_IMAGE_SECTION_HEADER FindFirstSection() const;
    IMAGE_SECTION_HEADER *FindSection(LPCSTR sectionName) const;
    BOOL HasWriteableSections() const;

    // Directory entry access
    BOOL HasDirectoryEntry(int entry) const;
    IMAGE_DATA_DIRECTORY *GetDirectoryEntry(int entry) const;
    TADDR GetDirectoryEntryData(int entry, COUNT_T *pSize = NULL) const;

    // IMAGE_DATA_DIRECTORY access
    TADDR GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir) const;
    TADDR GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir, COUNT_T *pSize) const;

    // RVA access
    CHECK CheckRva(RVA rva, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckRva(RVA rva, COUNT_T size, int forbiddenFlags=0, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetRvaData(RVA rva, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckData(const void *data, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckData(const void *data, COUNT_T size, IsNullOK ok = NULL_NOT_OK) const;
    RVA GetDataRva(const TADDR data) const;
    BOOL PointerInPE(PTR_CVOID data) const;

    // Flat mapping utilities
    CHECK CheckOffset(COUNT_T fileOffset, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckOffset(COUNT_T fileOffset, COUNT_T size, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetOffsetData(COUNT_T fileOffset, IsNullOK ok = NULL_NOT_OK) const;
    COUNT_T RvaToOffset(RVA rva) const;
    RVA OffsetToRva(COUNT_T fileOffset) const;

    // IL-only properties
    BOOL IsILOnly() const;
    CHECK CheckILOnly() const;

    // Strong name
    BOOL HasStrongNameSignature() const;
    CHECK CheckStrongNameSignature() const;
    PTR_CVOID GetStrongNameSignature(COUNT_T *pSize = NULL) const;
    BOOL IsStrongNameSigned() const;

    // TLS
    BOOL HasTls() const;
    CHECK CheckTls() const;
    PTR_VOID GetTlsRange(COUNT_T *pSize = NULL) const;
    UINT32 GetTlsIndex() const;

    // COR header
    BOOL HasCorHeader() const;
    CHECK CheckCorHeader() const;
    IMAGE_COR20_HEADER *GetCorHeader() const;
    PTR_CVOID GetMetadata(COUNT_T *pSize = NULL) const;
    const void *GetResources(COUNT_T *pSize = NULL) const;
    CHECK CheckResource(COUNT_T offset) const;
    const void *GetResource(COUNT_T offset, COUNT_T *pSize = NULL) const;
    BOOL HasManagedEntryPoint() const;
    ULONG GetEntryPointToken() const;
    IMAGE_COR_VTABLEFIXUP *GetVTableFixups(COUNT_T *pCount = NULL) const;

    BOOL IsNativeMachineFormat() const;
    BOOL IsI386() const;
    void GetPEKindAndMachine(DWORD *pdwPEKind, DWORD *pdwMachine);
    BOOL IsPlatformNeutral();

    CHECK CheckILMethod(RVA rva);

    PTR_IMAGE_DEBUG_DIRECTORY GetDebugDirectoryEntry(UINT index) const;
    PTR_CVOID GetNativeManifestMetadata(COUNT_T *pSize = NULL) const;

    BOOL IsComponentAssembly() const;
    BOOL HasReadyToRunHeader() const;
    READYTORUN_HEADER *GetReadyToRunHeader() const;

    BOOL HasNativeEntryPoint() const;
    void *GetNativeEntryPoint() const;
    PTR_VOID GetExport(LPCSTR exportName) const;

    DWORD GetCorHeaderFlags() const;

private:
    PEDecoder& GetPEDecoder() { return m_peDecoder; }
    const PEDecoder& GetPEDecoder() const { return m_peDecoder; }
#ifdef FEATURE_WEBCIL
    WebcilDecoder& GetWebcilDecoder() { return m_webcilDecoder; }
    const WebcilDecoder& GetWebcilDecoder() const { return m_webcilDecoder; }
#endif

public:
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

protected:
    // Protected forwarding helpers for subclass access to PEDecoder protected members
    IMAGE_NT_HEADERS* FindNTHeaders() const { return m_peDecoder.FindNTHeaders(); }
    IMAGE_SECTION_HEADER* RvaToSection(RVA rva) const { return m_peDecoder.RvaToSection(rva); }
    void SetRelocated() { m_peDecoder.SetRelocated(); }

private:
    Volatile<LONG> m_refCount;

protected:
    ImageFormat m_format;

    PEDecoder m_peDecoder;
#ifdef FEATURE_WEBCIL
    WebcilDecoder m_webcilDecoder;
#endif

public:
    PEImage* m_pOwner;

    friend struct cdac_data<PEImageLayout>;
};

template<>
struct cdac_data<PEImageLayout>
{
    static constexpr size_t Base = offsetof(PEImageLayout, m_peDecoder) + offsetof(PEDecoder, m_base);
    static constexpr size_t Size = offsetof(PEImageLayout, m_peDecoder) + offsetof(PEDecoder, m_size);
    static constexpr size_t Flags = offsetof(PEImageLayout, m_peDecoder) + offsetof(PEDecoder, m_flags);
    static constexpr size_t Format = offsetof(PEImageLayout, m_format);
};

typedef ReleaseHolder<PEImageLayout> PEImageLayoutHolder;

// A simple layout where data stays the same as in the input (file or a byte array)
class FlatImageLayout : public PEImageLayout
{
    VPTR_VTABLE_CLASS(FlatImageLayout, PEImageLayout)
        VPTR_UNIQUE(0x59)
public:
#ifndef DACCESS_COMPILE
    FlatImageLayout(PEImage* pOwner);
    FlatImageLayout(PEImage* pOwner, const BYTE* array, COUNT_T size);
    void* LoadImageByCopyingParts(SIZE_T* m_imageParts) const;
#if TARGET_WINDOWS
    void* LoadImageByMappingParts(SIZE_T* m_imageParts) const;
#endif
#endif

private:
    // Handles for the mapped image.
    // These will be null if the image data is not mapped by the runtime (for example, provided via an external assembly probe).
    CLRMapViewHolder m_FileView;
    HandleHolder m_FileMap;
};

// ConvertedImageView is for the case when we construct a loaded
// layout by mapping or copying portions of a flat layout
class ConvertedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(ConvertedImageLayout,PEImageLayout)
public:
    static const int MAX_PARTS = 16;
#ifndef DACCESS_COMPILE
    ConvertedImageLayout(FlatImageLayout* source, bool disableMapping);
    virtual ~ConvertedImageLayout();
    void  FreeImageParts();
#endif
private:
    PT_RUNTIME_FUNCTION m_pExceptionDir;
    SIZE_T              m_imageParts[MAX_PARTS];
};

// LoadedImageLayout is for the case when we construct a loaded layout directly
class LoadedImageLayout: public PEImageLayout
{
    VPTR_VTABLE_CLASS(LoadedImageLayout,PEImageLayout)
protected:
#ifndef TARGET_UNIX
    HINSTANCE m_Module;
#else
    PALPEFileHolder m_LoadedFile;
#endif
public:
#ifndef DACCESS_COMPILE
    LoadedImageLayout(PEImage* pOwner, HRESULT* returnDontThrow);
#if !defined(TARGET_UNIX)
    LoadedImageLayout(PEImage* pOwner, HMODULE hModule);
#endif // !TARGET_UNIX
    ~LoadedImageLayout();
#endif // !DACCESS_COMPILE
};

// A special layout that is used to load standalone composite r2r files.
// This layout is not owned by a PEImage and created by simply loading the file
// at the given path.
class NativeImageLayout : public PEImageLayout
{
    VPTR_VTABLE_CLASS(NativeImageLayout, PEImageLayout)
    
    public:
#ifndef DACCESS_COMPILE
    NativeImageLayout(LPCWSTR fullPath);
#endif
};

#endif  // PEIMAGELAYOUT_H_

