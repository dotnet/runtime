// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef PEIMAGEVIEW_INL_
#define PEIMAGEVIEW_INL_

#include "util.hpp"
#include "peimage.h"

inline void PEImageLayout::AddRef()
{
    CONTRACT_VOID
    {
        PRECONDITION(m_refCount>0 && m_refCount < COUNT_T_MAX);
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    InterlockedIncrement(&m_refCount);

    RETURN;
}

inline ULONG PEImageLayout::Release()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

#ifdef DACCESS_COMPILE
    // when DAC accesses layouts via PEImage it does not addref
    if (m_pOwner)
        return m_refCount;
#endif

    ULONG result=InterlockedDecrement(&m_refCount);
    if (result == 0 )
    {
        delete this;
    }
    return result;
}


inline PEImageLayout::~PEImageLayout()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
}

inline PEImageLayout::PEImageLayout()
    : m_refCount(1)
    , m_format(FORMAT_PE)
    , m_pOwner(NULL)
{
    LIMITED_METHOD_CONTRACT;
}

inline BOOL PEImageLayout::CompareBase(UPTR base, UPTR mapping)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer((PEImageLayout *)mapping));
        PRECONDITION(CheckPointer((PEImageLayout *)(base<<1),NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (base==0) //we were searching for 'Any'
        return TRUE;
    return ((PEImageLayout*)mapping)->GetBase()==((PEImageLayout*)(base<<1))->GetBase();

}

// -----------------------------------------------
// Forwarding methods — inline implementations
// -----------------------------------------------

// Dispatch to the active decoder (Webcil or PE) — same method name, value return
#ifdef FEATURE_WEBCIL
#define DECODER_DISPATCH(expr) \
    if (IsWebcilFormat()) \
        return m_webcilDecoder.expr; \
    return m_peDecoder.expr;
#else
#define DECODER_DISPATCH(expr) \
    return m_peDecoder.expr;
#endif

// Dispatch to the active decoder (Webcil or PE) — same method name, CHECK return
#ifdef FEATURE_WEBCIL
#define DECODER_CHECK(expr) \
    if (IsWebcilFormat()) \
    { \
        CHECK(m_webcilDecoder.expr); \
        CHECK_OK; \
    } \
    CHECK(m_peDecoder.expr); \
    CHECK_OK;
#else
#define DECODER_CHECK(expr) \
    CHECK(m_peDecoder.expr); \
    CHECK_OK;
#endif

// Forward to PE decoder, or return a constant for Webcil format
#define PE_OR_WEBCIL(peExpr, constVal) \
    if (IsWebcilFormat()) \
        return constVal; \
    return m_peDecoder.peExpr;

inline PTR_VOID PEImageLayout::GetBase() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    DECODER_DISPATCH(GetBase())
}

inline BOOL PEImageLayout::IsMapped() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(IsMapped(), FALSE)
}

inline BOOL PEImageLayout::IsRelocated() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(IsRelocated(), FALSE)
}

inline BOOL PEImageLayout::IsFlat() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    DECODER_DISPATCH(IsFlat())
}

inline BOOL PEImageLayout::HasContents() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    DECODER_DISPATCH(HasContents())
}

inline COUNT_T PEImageLayout::GetSize() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    DECODER_DISPATCH(GetSize())
}

inline BOOL PEImageLayout::HasHeaders() const
{
    LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_WEBCIL
    if (IsWebcilFormat())
        return m_webcilDecoder.HasWebcilHeaders();
#endif
    return m_peDecoder.HasNTHeaders();
}

inline CHECK PEImageLayout::CheckHeaders() const
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_WEBCIL
    if (IsWebcilFormat())
    {
        CHECK(m_webcilDecoder.CheckWebcilHeaders());
        CHECK_OK;
    }
#endif
    CHECK(m_peDecoder.CheckNTHeaders());
    CHECK_OK;
}

inline CHECK PEImageLayout::CheckFormat() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckFormat())
}

inline CHECK PEImageLayout::CheckNTFormat() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    CHECK(m_peDecoder.CheckNTFormat());
    CHECK_OK;
}

inline CHECK PEImageLayout::CheckCORFormat() const
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_WEBCIL
    if (IsWebcilFormat())
    {
        CHECK(m_webcilDecoder.CheckILFormat());
        CHECK_OK;
    }
#endif
    CHECK(m_peDecoder.CheckCORFormat());
    CHECK_OK;
}

inline CHECK PEImageLayout::CheckILFormat() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckILFormat())
}

inline CHECK PEImageLayout::CheckILOnlyFormat() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckILOnlyFormat())
}

inline BOOL PEImageLayout::HasNTHeaders() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(HasNTHeaders(), FALSE)
}

inline CHECK PEImageLayout::CheckNTHeaders() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    CHECK(m_peDecoder.CheckNTHeaders());
    CHECK_OK;
}

inline IMAGE_NT_HEADERS32 *PEImageLayout::GetNTHeaders32() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetNTHeaders32();
}

inline IMAGE_NT_HEADERS64 *PEImageLayout::GetNTHeaders64() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetNTHeaders64();
}

inline BOOL PEImageLayout::Has32BitNTHeaders() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(Has32BitNTHeaders(), FALSE)
}

inline BOOL PEImageLayout::IsDll() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(IsDll(), TRUE)
}

inline BOOL PEImageLayout::HasBaseRelocations() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(HasBaseRelocations(), FALSE)
}

inline const void *PEImageLayout::GetPreferredBase() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(GetPreferredBase(), NULL)
}

inline COUNT_T PEImageLayout::GetVirtualSize() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetVirtualSize())
}

inline DWORD PEImageLayout::GetTimeDateStamp() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(GetTimeDateStamp(), 0)
}

inline WORD PEImageLayout::GetMachine() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(GetMachine(), IMAGE_FILE_MACHINE_UNKNOWN)
}

inline COUNT_T PEImageLayout::GetNumberOfSections() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetNumberOfSections())
}

inline PTR_IMAGE_SECTION_HEADER PEImageLayout::FindFirstSection() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.FindFirstSection();
}

inline IMAGE_SECTION_HEADER *PEImageLayout::FindSection(LPCSTR sectionName) const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.FindSection(sectionName);
}

inline BOOL PEImageLayout::HasWriteableSections() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(HasWriteableSections(), FALSE)
}

inline BOOL PEImageLayout::HasDirectoryEntry(int entry) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(HasDirectoryEntry(entry))
}

inline IMAGE_DATA_DIRECTORY *PEImageLayout::GetDirectoryEntry(int entry) const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetDirectoryEntry(entry);
}

inline TADDR PEImageLayout::GetDirectoryEntryData(int entry, COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetDirectoryEntryData(entry, pSize))
}

inline TADDR PEImageLayout::GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir) const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetDirectoryData(pDir);
}

inline TADDR PEImageLayout::GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir, COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetDirectoryData(pDir, pSize);
}

inline CHECK PEImageLayout::CheckRva(RVA rva, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckRva(rva, ok))
}

inline CHECK PEImageLayout::CheckRva(RVA rva, COUNT_T size, int forbiddenFlags, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckRva(rva, size, forbiddenFlags, ok))
}

inline TADDR PEImageLayout::GetRvaData(RVA rva, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetRvaData(rva, ok))
}

inline CHECK PEImageLayout::CheckData(const void *data, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    CHECK(m_peDecoder.CheckData(data, ok));
    CHECK_OK;
}

inline CHECK PEImageLayout::CheckData(const void *data, COUNT_T size, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    CHECK(m_peDecoder.CheckData(data, size, ok));
    CHECK_OK;
}

inline RVA PEImageLayout::GetDataRva(const TADDR data) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetDataRva(data))
}

inline BOOL PEImageLayout::PointerInPE(PTR_CVOID data) const
{
    LIMITED_METHOD_DAC_CONTRACT;
    DECODER_DISPATCH(PointerInPE(data))
}

inline CHECK PEImageLayout::CheckOffset(COUNT_T fileOffset, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckOffset(fileOffset, ok))
}

inline CHECK PEImageLayout::CheckOffset(COUNT_T fileOffset, COUNT_T size, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckOffset(fileOffset, size, ok))
}

inline TADDR PEImageLayout::GetOffsetData(COUNT_T fileOffset, IsNullOK ok) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetOffsetData(fileOffset, ok))
}

inline COUNT_T PEImageLayout::RvaToOffset(RVA rva) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(RvaToOffset(rva))
}

inline RVA PEImageLayout::OffsetToRva(COUNT_T fileOffset) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(OffsetToRva(fileOffset))
}

inline BOOL PEImageLayout::IsILOnly() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(IsILOnly(), TRUE)
}

inline CHECK PEImageLayout::CheckILOnly() const
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_WEBCIL
    if (IsWebcilFormat())
    {
        CHECK(m_webcilDecoder.CheckILOnlyFormat());
        CHECK_OK;
    }
#endif
    CHECK(m_peDecoder.CheckILOnly());
    CHECK_OK;
}

inline BOOL PEImageLayout::HasStrongNameSignature() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(HasStrongNameSignature())
}

inline CHECK PEImageLayout::CheckStrongNameSignature() const
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_WEBCIL
    if (IsWebcilFormat())
    {
        if (m_webcilDecoder.HasStrongNameSignature())
        {
            IMAGE_DATA_DIRECTORY *pDir = &m_webcilDecoder.GetCorHeader()->StrongNameSignature;
            CHECK(m_webcilDecoder.CheckRva(VAL32(pDir->VirtualAddress), VAL32(pDir->Size)));
        }
        CHECK_OK;
    }
#endif
    CHECK(m_peDecoder.CheckStrongNameSignature());
    CHECK_OK;
}

inline PTR_CVOID PEImageLayout::GetStrongNameSignature(COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetStrongNameSignature(pSize))
}

inline BOOL PEImageLayout::IsStrongNameSigned() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(IsStrongNameSigned())
}

inline BOOL PEImageLayout::HasTls() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(HasTls(), FALSE)
}

inline CHECK PEImageLayout::CheckTls() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    CHECK(m_peDecoder.CheckTls());
    CHECK_OK;
}

inline PTR_VOID PEImageLayout::GetTlsRange(COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetTlsRange(pSize);
}

inline UINT32 PEImageLayout::GetTlsIndex() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetTlsIndex();
}

inline BOOL PEImageLayout::HasCorHeader() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(HasCorHeader())
}

inline CHECK PEImageLayout::CheckCorHeader() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckCorHeader())
}

inline IMAGE_COR20_HEADER *PEImageLayout::GetCorHeader() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetCorHeader())
}

inline PTR_CVOID PEImageLayout::GetMetadata(COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetMetadata(pSize))
}

inline const void *PEImageLayout::GetResources(COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetResources(pSize))
}

inline CHECK PEImageLayout::CheckResource(COUNT_T offset) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckResource(offset))
}

inline const void *PEImageLayout::GetResource(COUNT_T offset, COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetResource(offset, pSize))
}

inline BOOL PEImageLayout::HasManagedEntryPoint() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(HasManagedEntryPoint())
}

inline ULONG PEImageLayout::GetEntryPointToken() const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetEntryPointToken())
}

inline IMAGE_COR_VTABLEFIXUP *PEImageLayout::GetVTableFixups(COUNT_T *pCount) const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(GetVTableFixups(pCount), NULL)
}

inline BOOL PEImageLayout::IsNativeMachineFormat() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(IsNativeMachineFormat(), FALSE)
}

inline BOOL PEImageLayout::IsI386() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(IsI386(), FALSE)
}

inline void PEImageLayout::GetPEKindAndMachine(DWORD *pdwPEKind, DWORD *pdwMachine)
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_WEBCIL
    if (IsWebcilFormat())
    {
        m_webcilDecoder.GetPEKindAndMachine(pdwPEKind, pdwMachine);
        return;
    }
#endif
    m_peDecoder.GetPEKindAndMachine(pdwPEKind, pdwMachine);
}

inline BOOL PEImageLayout::IsPlatformNeutral()
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(IsPlatformNeutral(), TRUE)
}

inline CHECK PEImageLayout::CheckILMethod(RVA rva)
{
    WRAPPER_NO_CONTRACT;
    DECODER_CHECK(CheckILMethod(rva))
}

inline PTR_IMAGE_DEBUG_DIRECTORY PEImageLayout::GetDebugDirectoryEntry(UINT index) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetDebugDirectoryEntry(index))
}

inline PTR_CVOID PEImageLayout::GetNativeManifestMetadata(COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    DECODER_DISPATCH(GetNativeManifestMetadata(pSize))
}

inline BOOL PEImageLayout::IsComponentAssembly() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(IsComponentAssembly(), FALSE)
}

inline BOOL PEImageLayout::HasReadyToRunHeader() const
{
    LIMITED_METHOD_DAC_CONTRACT;
    PE_OR_WEBCIL(HasReadyToRunHeader(), FALSE)
}

inline READYTORUN_HEADER *PEImageLayout::GetReadyToRunHeader() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(GetReadyToRunHeader(), NULL)
}

inline BOOL PEImageLayout::HasNativeEntryPoint() const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(HasNativeEntryPoint(), FALSE)
}

inline void *PEImageLayout::GetNativeEntryPoint() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsPEFormat());
    return m_peDecoder.GetNativeEntryPoint();
}

inline PTR_VOID PEImageLayout::GetExport(LPCSTR exportName) const
{
    WRAPPER_NO_CONTRACT;
    PE_OR_WEBCIL(GetExport(exportName), NULL)
}

inline DWORD PEImageLayout::GetCorHeaderFlags() const
{
    WRAPPER_NO_CONTRACT;
    return GetCorHeader()->Flags;
}

#undef DECODER_DISPATCH
#undef DECODER_CHECK
#undef PE_OR_WEBCIL

#endif //PEIMAGEVIEW_INL_
