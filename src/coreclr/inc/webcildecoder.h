// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// WebcilDecoder.h
//
// Utility class for reading and verifying Webcil files.
// Webcil is an alternative container format for ECMA-335 assemblies
// that replaces PE headers with a minimal Webcil header and section table.
// See docs/design/mono/webcil.md for the format specification.
// --------------------------------------------------------------------------------

#ifndef WEBCILDECODER_H_
#define WEBCILDECODER_H_

#ifdef FEATURE_WEBCIL

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

#include "windows.h"
#include "clrtypes.h"
#include "pedecoder.h"
#include "check.h"
#include "contract.h"
#include "cor.h"
#include "corhdr.h"
#include "readytorun.h"

// --------------------------------------------------------------------------------
// Webcil format structures (from docs/design/mono/webcil.md)
// --------------------------------------------------------------------------------

#define WEBCIL_MAGIC_W 'W'
#define WEBCIL_MAGIC_B 'b'
#define WEBCIL_MAGIC_I 'I'
#define WEBCIL_MAGIC_L 'L'

#define WEBCIL_VERSION_MAJOR 0
#define WEBCIL_VERSION_MINOR 0

#pragma pack(push, 1)

struct WebcilHeader
{
    uint8_t Id[4];           // 'W' 'b' 'I' 'L'
    uint16_t VersionMajor;   // 0
    uint16_t VersionMinor;   // 0
    uint16_t CoffSections;
    uint16_t Reserved0;
    uint32_t PeCliHeaderRva;
    uint32_t PeCliHeaderSize;
    uint32_t PeDebugRva;
    uint32_t PeDebugSize;
};

struct WebcilSectionHeader
{
    uint32_t VirtualSize;
    uint32_t VirtualAddress;
    uint32_t SizeOfRawData;
    uint32_t PointerToRawData;
};

#pragma pack(pop)

static_assert(sizeof(WebcilHeader) == 28, "WebcilHeader must be 28 bytes");
static_assert(sizeof(WebcilSectionHeader) == 16, "WebcilSectionHeader must be 16 bytes");

// Maximum number of sections we support in a Webcil image
#define WEBCIL_MAX_SECTIONS 16

// --------------------------------------------------------------------------------
// WebcilDecoder class
// --------------------------------------------------------------------------------

class WebcilDecoder
{
    friend class PEImageLayout;

public:

    // ------------------------------------------------------------
    // Format detection (static)
    // ------------------------------------------------------------
    static bool DetectWebcilFormat(const void* data, COUNT_T size);

    // ------------------------------------------------------------
    // Construction / Initialization
    // ------------------------------------------------------------

    WebcilDecoder();
    void Init(void *flatBase, COUNT_T size);
    void Reset();

private:

    // ------------------------------------------------------------
    // Basic properties
    // ------------------------------------------------------------

    PTR_VOID GetBase() const;
    COUNT_T GetSize() const;
    BOOL HasContents() const;

    // Webcil is always flat, never mapped in the PE sense
    BOOL IsMapped() const { return FALSE; }
    BOOL IsRelocated() const { return FALSE; }
    BOOL IsFlat() const { return HasContents(); }

    // ------------------------------------------------------------
    // Header checks
    // ------------------------------------------------------------

    BOOL HasWebcilHeaders() const;
    CHECK CheckWebcilHeaders() const;

    // PE-specific — always false for Webcil
    BOOL HasNTHeaders() const { return FALSE; }
    CHECK CheckNTHeaders() const;
    BOOL Has32BitNTHeaders() const { return FALSE; }
    BOOL HasBaseRelocations() const { return FALSE; }
    BOOL HasWriteableSections() const { return FALSE; }
    BOOL HasTls() const { return FALSE; }

    // Webcil assemblies are always DLLs conceptually
    BOOL IsDll() const { return TRUE; }

    // ------------------------------------------------------------
    // Format checks — Webcil is always IL-only
    // ------------------------------------------------------------

    CHECK CheckFormat() const;
    CHECK CheckILFormat() const;
    CHECK CheckILOnlyFormat() const;

    // ------------------------------------------------------------
    // COR header
    // ------------------------------------------------------------

    BOOL HasCorHeader() const;
    CHECK CheckCorHeader() const;
    IMAGE_COR20_HEADER *GetCorHeader() const;

    // ------------------------------------------------------------
    // IL-only properties
    // ------------------------------------------------------------

    BOOL IsILOnly() const { return TRUE; }
    BOOL IsStrongNameSigned() const;
    BOOL HasStrongNameSignature() const;
    PTR_CVOID GetStrongNameSignature(COUNT_T *pSize = NULL) const;

    // ------------------------------------------------------------
    // Metadata
    // ------------------------------------------------------------

    PTR_CVOID GetMetadata(COUNT_T *pSize = NULL) const;

    // ------------------------------------------------------------
    // Entry point
    // ------------------------------------------------------------

    BOOL HasManagedEntryPoint() const;
    ULONG GetEntryPointToken() const;
    BOOL HasNativeEntryPoint() const { return FALSE; }
    void *GetNativeEntryPoint() const { return NULL; }

    // ------------------------------------------------------------
    // R2R — not supported for Webcil
    // ------------------------------------------------------------

    BOOL HasReadyToRunHeader() const { return FALSE; }
    BOOL IsComponentAssembly() const { return FALSE; }
    READYTORUN_HEADER *GetReadyToRunHeader() const { return NULL; }
    BOOL IsNativeMachineFormat() const { return FALSE; }
    PTR_CVOID GetNativeManifestMetadata(COUNT_T *pSize = NULL) const;

    // ------------------------------------------------------------
    // RVA operations
    // ------------------------------------------------------------

    CHECK CheckRva(RVA rva, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckRva(RVA rva, COUNT_T size, int forbiddenFlags = 0, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetRvaData(RVA rva, IsNullOK ok = NULL_NOT_OK) const;
    RVA GetDataRva(const TADDR data) const;
    BOOL PointerInPE(PTR_CVOID data) const;

    // ------------------------------------------------------------
    // Offset operations
    // ------------------------------------------------------------

    CHECK CheckOffset(COUNT_T fileOffset, IsNullOK ok = NULL_NOT_OK) const;
    CHECK CheckOffset(COUNT_T fileOffset, COUNT_T size, IsNullOK ok = NULL_NOT_OK) const;
    TADDR GetOffsetData(COUNT_T fileOffset, IsNullOK ok = NULL_NOT_OK) const;
    COUNT_T RvaToOffset(RVA rva) const;
    RVA OffsetToRva(COUNT_T fileOffset) const;

    // ------------------------------------------------------------
    // Section access
    // ------------------------------------------------------------

    COUNT_T GetNumberOfSections() const;

    // ------------------------------------------------------------
    // PE properties — safe defaults for Webcil
    // ------------------------------------------------------------

    const void *GetPreferredBase() const { return NULL; }
    COUNT_T GetVirtualSize() const;
    WORD GetMachine() const { return IMAGE_FILE_MACHINE_UNKNOWN; }
    DWORD GetTimeDateStamp() const { return 0; }
    DWORD GetCheckSum() const { return 0; }
    WORD GetSubsystem() const { return 0; }
    WORD GetDllCharacteristics() const { return 0; }
    WORD GetCharacteristics() const { return 0; }
    void GetPEKindAndMachine(DWORD *pdwPEKind, DWORD *pdwMachine);
    BOOL IsPlatformNeutral() { return TRUE; }

    // ------------------------------------------------------------
    // Directory entries
    // Webcil has no PE IMAGE_DATA_DIRECTORY array, but stores
    // debug directory info (PeDebugRva/PeDebugSize) in the header.
    // ------------------------------------------------------------

    BOOL HasDirectoryEntry(int entry) const;
    TADDR GetDirectoryEntryData(int entry, COUNT_T *pSize = NULL) const;

    // Debug directory
    PTR_IMAGE_DEBUG_DIRECTORY GetDebugDirectoryEntry(UINT index) const;

    // ------------------------------------------------------------
    // Resources
    // ------------------------------------------------------------

    const void *GetResources(COUNT_T *pSize = NULL) const;
    CHECK CheckResource(COUNT_T offset) const;
    const void *GetResource(COUNT_T offset, COUNT_T *pSize = NULL) const;

    // VTable fixups — not supported
    IMAGE_COR_VTABLEFIXUP *GetVTableFixups(COUNT_T *pCount = NULL) const { return NULL; }

    // IL method validation
    CHECK CheckILMethod(RVA rva);

    // Exports — not supported
    PTR_VOID GetExport(LPCSTR exportName) const { return NULL; }

    // TLS — not supported
    PTR_VOID GetTlsRange(COUNT_T *pSize = NULL) const { return NULL; }
    UINT32 GetTlsIndex() const { return 0; }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags, bool enumThis);
#endif

private:

    // Internal helpers
    const WebcilSectionHeader *RvaToSection(RVA rva) const;
    const WebcilSectionHeader *OffsetToSection(COUNT_T fileOffset) const;
    void FindCorHeader() const;

    // Instance members
    TADDR                m_base;
    COUNT_T              m_size;
    BOOL                 m_hasContents;
    const WebcilHeader  *m_pHeader;
    mutable IMAGE_COR20_HEADER *m_pCorHeader;
};

#endif // FEATURE_WEBCIL

#endif // WEBCILDECODER_H_
