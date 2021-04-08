// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEDecoder.h
//

// --------------------------------------------------------------------------------

// --------------------------------------------------------------------------------
// PEDecoder - Utility class for reading and verifying PE files.
//
// Note that the Check step is optional if you are willing to trust the
// integrity of the image.
// (Or at any rate can be factored into an initial verification step.)
//
// Functions which access the memory of the PE file take a "flat" flag - this
// indicates whether the PE images data has been loaded flat the way it resides in the file,
// or if the sections have been mapped into memory at the proper base addresses.
//
// Finally, some functions take an optional "size" argument, which can be used for
// range verification.  This is an optional parameter, but if you omit it be sure
// you verify the size in some other way.
// --------------------------------------------------------------------------------


#ifndef PEDECODER_H_
#define PEDECODER_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

#include "windows.h"
#include "clrtypes.h"
#include "check.h"
#include "contract.h"
#include "cor.h"
#include "corhdr.h"

#include "corcompile.h"

#include "readytorun.h"
typedef DPTR(struct READYTORUN_CORE_HEADER) PTR_READYTORUN_CORE_HEADER;
typedef DPTR(struct READYTORUN_HEADER) PTR_READYTORUN_HEADER;
typedef DPTR(struct READYTORUN_SECTION) PTR_READYTORUN_SECTION;

typedef DPTR(IMAGE_COR20_HEADER)    PTR_IMAGE_COR20_HEADER;

// --------------------------------------------------------------------------------
// Forward declared types
// --------------------------------------------------------------------------------

class Module;

// --------------------------------------------------------------------------------
// RVA definition
// --------------------------------------------------------------------------------

// Needs to be DWORD to avoid conflict with <imagehlp.h>
typedef DWORD RVA;

#ifdef _MSC_VER
// Wrapper to suppress ambigous overload problems with MSVC.
inline CHECK CheckOverflow(RVA value1, COUNT_T value2)
{
    WRAPPER_NO_CONTRACT;
    CHECK(CheckOverflow((UINT32) value1, (UINT32) value2));
    CHECK_OK;
}
#endif  // _MSC_VER

// --------------------------------------------------------------------------------
// IMAGE_FILE_MACHINE_NATIVE
// --------------------------------------------------------------------------------

#if defined(TARGET_X86)
#define IMAGE_FILE_MACHINE_NATIVE   IMAGE_FILE_MACHINE_I386
#elif defined(TARGET_AMD64)
#define IMAGE_FILE_MACHINE_NATIVE   IMAGE_FILE_MACHINE_AMD64
#elif defined(TARGET_ARM)
#define IMAGE_FILE_MACHINE_NATIVE   IMAGE_FILE_MACHINE_ARMNT
#elif defined(TARGET_ARM64)
#define IMAGE_FILE_MACHINE_NATIVE   IMAGE_FILE_MACHINE_ARM64
#else
#error "port me"
#endif

// Machine code for native images
#if defined(__APPLE__)
#define IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE 0x4644
#elif defined(__FreeBSD__)
#define IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE 0xADC4
#elif defined(__linux__)
#define IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE 0x7B79
#elif defined(__NetBSD__)
#define IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE 0x1993
#elif defined(__sun)
#define IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE 0x1992
#else
#define IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE 0
#endif

#define IMAGE_FILE_MACHINE_NATIVE_NI (IMAGE_FILE_MACHINE_NATIVE ^ IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE)

// --------------------------------------------------------------------------------
// Types
// --------------------------------------------------------------------------------

typedef DPTR(class PEDecoder) PTR_PEDecoder;

typedef bool (*PEDecoder_ResourceTypesCallbackFunction)(LPCWSTR lpType, void* context);
typedef bool (*PEDecoder_ResourceNamesCallbackFunction)(LPCWSTR lpName, LPCWSTR lpType, void* context);
typedef bool (*PEDecoder_ResourceCallbackFunction)(LPCWSTR lpName, LPCWSTR lpType, DWORD langid, BYTE* data, COUNT_T cbData, void* context);

class PEDecoder
{
  public:

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    // Access functions are divided into 3 categories:
    //  Has - check if the element is present
    //  Check - Do consistency checks on the element (requires Has).
    //          This step is optional if you are willing to trust the integrity of the
    //          image. (It is asserted in a checked build.)
    //  Get - Access the element (requires Has and Check)

    PEDecoder();
    PEDecoder(void *flatBase, COUNT_T size);              // flatBase is the raw disk layout data (using MapViewOfFile)
    PEDecoder(PTR_VOID mappedBase, bool relocated = FALSE);  // mappedBase is the mapped/expanded file (using LoadLibrary)

    void Init(void *flatBase, COUNT_T size);
    HRESULT Init(void *mappedBase, bool relocated = FALSE);
    void   Reset();  //make sure you don't have a thread race

    PTR_VOID GetBase() const;            // Currently loaded base, as opposed to GetPreferredBase()
    BOOL IsMapped() const;
    BOOL IsRelocated() const;
    BOOL IsFlat() const;
    BOOL HasContents() const;
    COUNT_T GetSize() const;          // size of file on disk, as opposed to GetVirtualSize()

    // High level image checks:

    CHECK CheckFormat() const;        // Check whatever is present
    CHECK CheckNTFormat() const;      // Check a PE file image
    CHECK CheckCORFormat() const;     // Check a COR image (IL or native)
    CHECK CheckILFormat() const;      // Check a managed image
    CHECK CheckILOnlyFormat() const;  // Check an IL only image
    CHECK CheckNativeFormat() const;  // Check a native image

    // NT header access

    BOOL HasNTHeaders() const;
    CHECK CheckNTHeaders() const;

    IMAGE_NT_HEADERS32 *GetNTHeaders32() const;
    IMAGE_NT_HEADERS64 *GetNTHeaders64() const;
    BOOL Has32BitNTHeaders() const;

    const void *GetHeaders(COUNT_T *pSize = NULL) const;

    BOOL IsDll() const;
    BOOL HasBaseRelocations() const;
    const void *GetPreferredBase() const; // OptionalHeaders.ImageBase
    COUNT_T GetVirtualSize() const; // OptionalHeaders.SizeOfImage - size of mapped/expanded image in memory
    WORD GetSubsystem() const;
    WORD GetDllCharacteristics() const;
    DWORD GetTimeDateStamp() const;
    DWORD GetCheckSum() const;
    WORD GetMachine() const;
    WORD GetCharacteristics() const;
    DWORD GetFileAlignment() const;
    DWORD GetSectionAlignment() const;
    SIZE_T GetSizeOfStackReserve() const;
    SIZE_T GetSizeOfStackCommit() const;
    SIZE_T GetSizeOfHeapReserve() const;
    SIZE_T GetSizeOfHeapCommit() const;
    UINT32 GetLoaderFlags() const;
    UINT32 GetWin32VersionValue() const;
    COUNT_T GetNumberOfRvaAndSizes() const;
    COUNT_T GetNumberOfSections() const;
    PTR_IMAGE_SECTION_HEADER FindFirstSection() const;
    IMAGE_SECTION_HEADER *FindSection(LPCSTR sectionName) const;

    DWORD GetImageIdentity() const;

    BOOL HasWriteableSections() const;

    // Directory entry access

    BOOL HasDirectoryEntry(int entry) const;
    CHECK CheckDirectoryEntry(int entry, int forbiddenFlags = 0, IsNullOK ok = NULL_NOT_OK) const;
    IMAGE_DATA_DIRECTORY *GetDirectoryEntry(int entry) const;
    TADDR GetDirectoryEntryData(int entry, COUNT_T *pSize = NULL) const;

    // IMAGE_DATA_DIRECTORY access

    CHECK CheckDirectory(IMAGE_DATA_DIRECTORY *pDir, int forbiddenFlags = 0, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir) const;
    TADDR GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir, COUNT_T *pSize) const;

    // Basic RVA access

    CHECK CheckRva(RVA rva, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckRva(RVA rva, COUNT_T size, int forbiddenFlags=0, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetRvaData(RVA rva, IsNullOK ok = NULL_NOT_OK) const;
    // Called with ok=NULL_OK only for mapped fields (RVA statics)

    CHECK CheckData(const void *data, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckData(const void *data, COUNT_T size, IsNullOK ok = NULL_NOT_OK) const;
    RVA GetDataRva(const TADDR data) const;
    BOOL PointerInPE(PTR_CVOID data) const;

    // Flat mapping utilities - using PointerToRawData instead of (Relative)VirtualAddress
    CHECK CheckOffset(COUNT_T fileOffset, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckOffset(COUNT_T fileOffset, COUNT_T size, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetOffsetData(COUNT_T fileOffset, IsNullOK ok = NULL_NOT_OK) const;
    // Called with ok=NULL_OK only for mapped fields (RVA statics)

    // Mapping between RVA and file offsets
    COUNT_T RvaToOffset(RVA rva) const;
    RVA OffsetToRva(COUNT_T fileOffset) const;

    // Base intra-image pointer access
    // (These are for pointers retrieved out of the PE image)

    CHECK CheckInternalAddress(SIZE_T address, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckInternalAddress(SIZE_T address, COUNT_T size, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetInternalAddressData(SIZE_T address) const;

    // CLR loader IL Image verification - these checks apply to IL_ONLY images.

    BOOL IsILOnly() const;
    CHECK CheckILOnly() const;

    void LayoutILOnly(void *base, bool enableExecution) const;

    // Strong name & hashing support

    BOOL HasStrongNameSignature() const;
    CHECK CheckStrongNameSignature() const;
    PTR_CVOID GetStrongNameSignature(COUNT_T *pSize = NULL) const;

    // CorHeader flag support

    // IsStrongNameSigned indicates whether the signature has been filled in.
    // (otherwise if it has a signature it is delay signed.)
    BOOL IsStrongNameSigned() const;    // TRUE if the COMIMAGE_FLAGS_STRONGNAMESIGNED flag is set

    // TLS

    BOOL HasTls() const;
    CHECK CheckTls() const;
    PTR_VOID GetTlsRange(COUNT_T *pSize = NULL) const;
    UINT32 GetTlsIndex() const;

    // Win32 resources
    void *GetWin32Resource(LPCWSTR lpName, LPCWSTR lpType, COUNT_T *pSize = NULL) const;
    bool EnumerateWin32ResourceTypes(PEDecoder_ResourceTypesCallbackFunction callback, void* context) const;
    bool EnumerateWin32ResourceNames(LPCWSTR lpType, PEDecoder_ResourceNamesCallbackFunction callback, void* context) const;
    bool EnumerateWin32Resources(LPCWSTR lpName, LPCWSTR lpType, PEDecoder_ResourceCallbackFunction callback, void* context) const;
  public:

    // COR header fields

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

    // Native header access
    BOOL HasNativeHeader() const;
    CHECK CheckNativeHeader() const;
    CORCOMPILE_HEADER *GetNativeHeader() const;
    BOOL IsNativeMachineFormat() const;
    BOOL IsI386() const;

    void GetPEKindAndMachine(DWORD * pdwPEKind, DWORD *pdwMachine);  // Returns CorPEKind flags
    BOOL IsPlatformNeutral(); // Returns TRUE for IL-only platform neutral images

    //
    // Verifies that the IL is within the bounds of the image.
    //
    CHECK CheckILMethod(RVA rva);

    //
    // Compute size of IL blob. Assumes that the IL is within the bounds of the image - make sure
    // to call CheckILMethod before calling this method.
    //
    static SIZE_T ComputeILMethodSize(TADDR pIL);

    // Debug directory access, returns NULL if no such entry
    PTR_IMAGE_DEBUG_DIRECTORY GetDebugDirectoryEntry(UINT index) const;

    PTR_CVOID GetNativeManifestMetadata(COUNT_T* pSize = NULL) const;

#ifdef FEATURE_PREJIT
    CHECK CheckNativeHeaderVersion() const;

    // ManagedNative fields
    CORCOMPILE_CODE_MANAGER_ENTRY *GetNativeCodeManagerTable() const;
    CORCOMPILE_EE_INFO_TABLE *GetNativeEEInfoTable() const;
    void *GetNativeHelperTable(COUNT_T *pSize = NULL) const;
    CORCOMPILE_VERSION_INFO *GetNativeVersionInfo() const;
    CORCOMPILE_VERSION_INFO *GetNativeVersionInfoMaybeNull(bool skipCheckNativeHeader = false) const;
    BOOL HasNativeDebugMap() const;
    TADDR GetNativeDebugMap(COUNT_T *pSize = NULL) const;
    Module *GetPersistedModuleImage(COUNT_T *pSize = NULL) const;
    PCODE GetNativeHotCode(COUNT_T * pSize = NULL) const;
    PCODE GetNativeCode(COUNT_T * pSize = NULL) const;
    PCODE GetNativeColdCode(COUNT_T * pSize = NULL) const;

    CORCOMPILE_METHOD_PROFILE_LIST *GetNativeProfileDataList(COUNT_T *pSize = NULL) const;
    const void *GetNativePreferredBase() const;
    BOOL GetNativeILHasSecurityDirectory() const;
    BOOL GetNativeILIsIbcOptimized() const;
    BOOL GetNativeILHasReadyToRunHeader() const;
    BOOL IsNativeILILOnly() const;
    BOOL IsNativeILDll() const;
    void GetNativeILPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine) const;
    CORCOMPILE_DEPENDENCY * GetNativeDependencies(COUNT_T *pCount = NULL) const;

    PTR_CORCOMPILE_IMPORT_SECTION GetNativeImportSections(COUNT_T *pCount = NULL) const;
    PTR_CORCOMPILE_IMPORT_SECTION GetNativeImportSectionFromIndex(COUNT_T index) const;
    PTR_CORCOMPILE_IMPORT_SECTION GetNativeImportSectionForRVA(RVA rva) const;

    TADDR GetStubsTable(COUNT_T *pSize = NULL) const;
    TADDR GetVirtualSectionsTable(COUNT_T *pSize = NULL) const;
#endif // FEATURE_PREJIT

    BOOL IsComponentAssembly() const;
    BOOL HasReadyToRunHeader() const;
    READYTORUN_HEADER *GetReadyToRunHeader() const;

    void  GetEXEStackSizes(SIZE_T *PE_SizeOfStackReserve, SIZE_T *PE_SizeOfStackCommit) const;

    CHECK CheckWillCreateGuardPage() const;

    // Native DLLMain Entrypoint
    BOOL HasNativeEntryPoint() const;
    void *GetNativeEntryPoint() const;

    // Look up a named symbol in the export directory
    void *GetExport(LPCSTR exportName) const;

#ifdef _DEBUG
    // Stress mode for relocations
    static BOOL GetForceRelocs();
    static BOOL ForceRelocForDLL(LPCWSTR lpFileName);
#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags, bool enumThis);
#endif

  protected:

    // ------------------------------------------------------------
    // Protected API for subclass use
    // ------------------------------------------------------------

    // Checking utilites
    static CHECK CheckBounds(RVA rangeBase, COUNT_T rangeSize, RVA rva);
    static CHECK CheckBounds(RVA rangeBase, COUNT_T rangeSize, RVA rva, COUNT_T size);

    static CHECK CheckBounds(const void *rangeBase, COUNT_T rangeSize, const void *pointer);
    static CHECK CheckBounds(PTR_CVOID rangeBase, COUNT_T rangeSize, PTR_CVOID pointer, COUNT_T size);

  protected:

    // Flat mapping utilities - using PointerToRawData instead of (Relative)VirtualAddress
    IMAGE_SECTION_HEADER *RvaToSection(RVA rva) const;
    IMAGE_SECTION_HEADER *OffsetToSection(COUNT_T fileOffset) const;

    void SetRelocated();

  private:

    // ------------------------------------------------------------
    // Internal functions
    // ------------------------------------------------------------

    enum METADATA_SECTION_TYPE
    {
        METADATA_SECTION_FULL,
#ifdef FEATURE_PREJIT
        METADATA_SECTION_MANIFEST
#endif
    };

    IMAGE_DATA_DIRECTORY *GetMetaDataHelper(METADATA_SECTION_TYPE type) const;

    static PTR_IMAGE_SECTION_HEADER FindFirstSection(IMAGE_NT_HEADERS * pNTHeaders);

    IMAGE_NT_HEADERS *FindNTHeaders() const;
    IMAGE_COR20_HEADER *FindCorHeader() const;
    CORCOMPILE_HEADER *FindNativeHeader() const;
    READYTORUN_HEADER *FindReadyToRunHeader() const;

    // Flat mapping utilities
    RVA InternalAddressToRva(SIZE_T address) const;

    // NT header subchecks
    CHECK CheckSection(COUNT_T previousAddressEnd, COUNT_T addressStart, COUNT_T addressSize,
                       COUNT_T previousOffsetEnd, COUNT_T offsetStart, COUNT_T offsetSize) const;

    // Pure managed subchecks
    CHECK CheckILOnlyImportDlls() const;
    CHECK CheckILOnlyImportByNameTable(RVA rva) const;
    CHECK CheckILOnlyBaseRelocations() const;
    CHECK CheckILOnlyEntryPoint() const;

    // ------------------------------------------------------------
    // Instance members
    // ------------------------------------------------------------

    enum
    {
        FLAG_MAPPED             = 0x01, // the file is mapped/hydrated (vs. the raw disk layout)
        FLAG_CONTENTS           = 0x02, // the file has contents
        FLAG_RELOCATED          = 0x04, // relocs have been applied
        FLAG_NT_CHECKED         = 0x10,
        FLAG_COR_CHECKED        = 0x20,
        FLAG_IL_ONLY_CHECKED    = 0x40,
        FLAG_NATIVE_CHECKED     = 0x80,

        FLAG_HAS_NO_READYTORUN_HEADER = 0x100,
    };

    TADDR               m_base;
    COUNT_T             m_size;     // size of file on disk, as opposed to OptionalHeaders.SizeOfImage
    ULONG               m_flags;

    PTR_IMAGE_NT_HEADERS   m_pNTHeaders;
    PTR_IMAGE_COR20_HEADER m_pCorHeader;
    PTR_CORCOMPILE_HEADER  m_pNativeHeader;
    PTR_READYTORUN_HEADER  m_pReadyToRunHeader;
};

//
//  MethodSectionIterator class is used to iterate hot (or) cold method section in an ngen image.
//  It can also iterate nibble maps generated by the JIT in a regular HeapList.
//
class MethodSectionIterator
{
  private:
    PTR_DWORD m_codeTableStart;
    PTR_DWORD m_codeTable;
    PTR_DWORD m_codeTableEnd;

    BYTE *m_code;

    DWORD m_dword;
    DWORD m_index;

    BYTE *m_current;

  public:

    //If code is a target pointer, then GetMethodCode and FindMethodCode return
    //target pointers.  codeTable may be a pointer of either type, since it is
    //converted internally into a host pointer.
    MethodSectionIterator(const void *code, SIZE_T codeSize,
                          const void *codeTable, SIZE_T codeTableSize);
    BOOL Next();
    BYTE *GetMethodCode() { return m_current; } // Get the start of method code of the current method in the iterator
};

#include "pedecoder.inl"

#endif  // PEDECODER_H_
