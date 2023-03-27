// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEDecoder.inl
//

// --------------------------------------------------------------------------------

#ifndef _PEDECODER_INL_
#define _PEDECODER_INL_

#include "pedecoder.h"
#include "ex.h"

#ifndef DACCESS_COMPILE

inline PEDecoder::PEDecoder()
  : m_base(0),
    m_size(0),
    m_flags(0),
    m_pNTHeaders(nullptr),
    m_pCorHeader(nullptr),
    m_pReadyToRunHeader(nullptr)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        CANNOT_TAKE_LOCK;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
}
#else
inline PEDecoder::PEDecoder()
{
    LIMITED_METHOD_CONTRACT;
}
#endif // #ifndef DACCESS_COMPILE

inline PTR_VOID PEDecoder::GetBase() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return PTR_VOID(m_base);
}

inline BOOL PEDecoder::IsMapped() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return (m_flags & FLAG_MAPPED) != 0;
}

inline BOOL PEDecoder::IsRelocated() const
{
    LIMITED_METHOD_CONTRACT;

    return (m_flags & FLAG_RELOCATED) != 0;
}

inline void PEDecoder::SetRelocated()
{
    m_flags |= FLAG_RELOCATED;
}

inline BOOL PEDecoder::IsFlat() const
{
    LIMITED_METHOD_CONTRACT;

    return HasContents() && !IsMapped();
}

inline BOOL PEDecoder::HasContents() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return (m_flags & FLAG_CONTENTS) != 0;
}

inline COUNT_T PEDecoder::GetSize() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_size;
}

inline PEDecoder::PEDecoder(PTR_VOID mappedBase, bool fixedUp /*= FALSE*/)
  : m_base(dac_cast<TADDR>(mappedBase)),
    m_size(0),
    m_flags(FLAG_MAPPED | FLAG_CONTENTS | FLAG_NT_CHECKED | (fixedUp ? FLAG_RELOCATED : 0)),
    m_pNTHeaders(nullptr),
    m_pCorHeader(nullptr),
    m_pReadyToRunHeader(nullptr)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(mappedBase));
        PRECONDITION(PEDecoder(mappedBase,fixedUp).CheckNTHeaders());
        THROWS;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Temporarily set the size to 2 pages, so we can get the headers.
    m_size = GetOsPageSize()*2;

    m_pNTHeaders = PTR_IMAGE_NT_HEADERS(FindNTHeaders());
    if (!m_pNTHeaders)
        ThrowHR(COR_E_BADIMAGEFORMAT);

    m_size = VAL32(m_pNTHeaders->OptionalHeader.SizeOfImage);
}

#ifndef DACCESS_COMPILE

//REM
//what's the right way to do this?
//we want to use some against TADDR, but also want to do
//some against what's just in the current process.
//m_base is a TADDR in DAC all the time, though.
//Have to implement separate fn to do the lookup??
inline PEDecoder::PEDecoder(void *flatBase, COUNT_T size)
  : m_base((TADDR)flatBase),
    m_size(size),
    m_flags(FLAG_CONTENTS),
    m_pNTHeaders(NULL),
    m_pCorHeader(NULL),
    m_pReadyToRunHeader(NULL)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(flatBase));
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;
}

inline void PEDecoder::Init(void *flatBase, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION((size == 0) || CheckPointer(flatBase));
        PRECONDITION(!HasContents());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_base = (TADDR)flatBase;
    m_size = size;
    m_flags = FLAG_CONTENTS;
}



inline HRESULT PEDecoder::Init(void *mappedBase, bool fixedUp /*= FALSE*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(mappedBase));
        PRECONDITION(!HasContents());
    }
    CONTRACTL_END;

    m_base = (TADDR)mappedBase;
    m_flags = FLAG_MAPPED | FLAG_CONTENTS;
    if (fixedUp)
        m_flags |= FLAG_RELOCATED;

    // Temporarily set the size to 2 pages, so we can get the headers.
    m_size = GetOsPageSize()*2;

    m_pNTHeaders = FindNTHeaders();
    if (!m_pNTHeaders)
        return COR_E_BADIMAGEFORMAT;

    m_size = VAL32(m_pNTHeaders->OptionalHeader.SizeOfImage);
    return S_OK;
}

inline void PEDecoder::Reset()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    m_base=NULL;
    m_flags=NULL;
    m_size=NULL;
    m_pNTHeaders=NULL;
    m_pCorHeader=NULL;
    m_pReadyToRunHeader=NULL;
}
#endif // #ifndef DACCESS_COMPILE


inline IMAGE_NT_HEADERS32 *PEDecoder::GetNTHeaders32() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return dac_cast<PTR_IMAGE_NT_HEADERS32>(FindNTHeaders());
}

inline IMAGE_NT_HEADERS64 *PEDecoder::GetNTHeaders64() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return dac_cast<PTR_IMAGE_NT_HEADERS64>(FindNTHeaders());
}

inline BOOL PEDecoder::Has32BitNTHeaders() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return (FindNTHeaders()->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC));
}

inline const void *PEDecoder::GetHeaders(COUNT_T *pSize) const
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    //even though some data in OptionalHeader is different for 32 and 64, this field is the same
    if (pSize != NULL)
        *pSize = VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders);

    RETURN (const void *) m_base;
}

inline BOOL PEDecoder::IsDll() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return ((FindNTHeaders()->FileHeader.Characteristics & VAL16(IMAGE_FILE_DLL)) != 0);
}

inline BOOL PEDecoder::HasBaseRelocations() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return ((FindNTHeaders()->FileHeader.Characteristics & VAL16(IMAGE_FILE_RELOCS_STRIPPED)) == 0);
}

inline const void *PEDecoder::GetPreferredBase() const
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (Has32BitNTHeaders())
        RETURN (const void *) (SIZE_T) VAL32(GetNTHeaders32()->OptionalHeader.ImageBase);
    else
        RETURN (const void *) (SIZE_T) VAL64(GetNTHeaders64()->OptionalHeader.ImageBase);
}

inline COUNT_T PEDecoder::GetVirtualSize() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    //even though some data in OptionalHeader is different for 32 and 64,  this field is the same
    return VAL32(FindNTHeaders()->OptionalHeader.SizeOfImage);
}

inline WORD PEDecoder::GetSubsystem() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    //even though some data in OptionalHeader is different for 32 and 64,  this field is the same
    return VAL16(FindNTHeaders()->OptionalHeader.Subsystem);
}

inline WORD PEDecoder::GetDllCharacteristics() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //even though some data in OptionalHeader is different for 32 and 64,  this field is the same
    return VAL16(FindNTHeaders()->OptionalHeader.DllCharacteristics);
}

inline DWORD PEDecoder::GetTimeDateStamp() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return VAL32(FindNTHeaders()->FileHeader.TimeDateStamp);
}

inline DWORD PEDecoder::GetCheckSum() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //even though some data in OptionalHeader is different for 32 and 64,  this field is the same
    return VAL32(FindNTHeaders()->OptionalHeader.CheckSum);
}

inline DWORD PEDecoder::GetFileAlignment() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //even though some data in OptionalHeader is different for 32 and 64,  this field is the same
    return VAL32(FindNTHeaders()->OptionalHeader.FileAlignment);
}

inline DWORD PEDecoder::GetSectionAlignment() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //even though some data in OptionalHeader is different for 32 and 64,  this field is the same
    return VAL32(FindNTHeaders()->OptionalHeader.SectionAlignment);
}

inline WORD PEDecoder::GetMachine() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return VAL16(FindNTHeaders()->FileHeader.Machine);
}

inline WORD PEDecoder::GetCharacteristics() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return VAL16(FindNTHeaders()->FileHeader.Characteristics);
}

inline SIZE_T PEDecoder::GetSizeOfStackReserve() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return (SIZE_T) VAL32(GetNTHeaders32()->OptionalHeader.SizeOfStackReserve);
    else
        return (SIZE_T) VAL64(GetNTHeaders64()->OptionalHeader.SizeOfStackReserve);
}


inline SIZE_T PEDecoder::GetSizeOfStackCommit() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return (SIZE_T) VAL32(GetNTHeaders32()->OptionalHeader.SizeOfStackCommit);
    else
        return (SIZE_T) VAL64(GetNTHeaders64()->OptionalHeader.SizeOfStackCommit);
}


inline SIZE_T PEDecoder::GetSizeOfHeapReserve() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return (SIZE_T) VAL32(GetNTHeaders32()->OptionalHeader.SizeOfHeapReserve);
    else
        return (SIZE_T) VAL64(GetNTHeaders64()->OptionalHeader.SizeOfHeapReserve);
}


inline SIZE_T PEDecoder::GetSizeOfHeapCommit() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return (SIZE_T) VAL32(GetNTHeaders32()->OptionalHeader.SizeOfHeapCommit);
    else
        return (SIZE_T) VAL64(GetNTHeaders64()->OptionalHeader.SizeOfHeapCommit);
}

inline UINT32 PEDecoder::GetLoaderFlags() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return VAL32(GetNTHeaders32()->OptionalHeader.LoaderFlags);
    else
        return VAL32(GetNTHeaders64()->OptionalHeader.LoaderFlags);
}

inline UINT32 PEDecoder::GetWin32VersionValue() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return VAL32(GetNTHeaders32()->OptionalHeader.Win32VersionValue);
    else
        return VAL32(GetNTHeaders64()->OptionalHeader.Win32VersionValue);
}

inline COUNT_T PEDecoder::GetNumberOfRvaAndSizes() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return VAL32(GetNTHeaders32()->OptionalHeader.NumberOfRvaAndSizes);
    else
        return VAL32(GetNTHeaders64()->OptionalHeader.NumberOfRvaAndSizes);
}

inline BOOL PEDecoder::HasDirectoryEntry(int entry) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (Has32BitNTHeaders())
        return (GetNTHeaders32()->OptionalHeader.DataDirectory[entry].VirtualAddress != 0);
    else
        return (GetNTHeaders64()->OptionalHeader.DataDirectory[entry].VirtualAddress != 0);
}

inline IMAGE_DATA_DIRECTORY *PEDecoder::GetDirectoryEntry(int entry) const
{
    CONTRACT(IMAGE_DATA_DIRECTORY *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (Has32BitNTHeaders())
        RETURN dac_cast<PTR_IMAGE_DATA_DIRECTORY>(
            dac_cast<TADDR>(GetNTHeaders32()) +
            offsetof(IMAGE_NT_HEADERS32, OptionalHeader.DataDirectory) +
            entry * sizeof(IMAGE_DATA_DIRECTORY));
    else
        RETURN dac_cast<PTR_IMAGE_DATA_DIRECTORY>(
            dac_cast<TADDR>(GetNTHeaders64()) +
            offsetof(IMAGE_NT_HEADERS64, OptionalHeader.DataDirectory) +
            entry * sizeof(IMAGE_DATA_DIRECTORY));
}

inline TADDR PEDecoder::GetDirectoryEntryData(int entry, COUNT_T *pSize) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckDirectoryEntry(entry, 0, NULL_OK));
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer((void *)RETVAL, NULL_OK));
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = GetDirectoryEntry(entry);

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN GetDirectoryData(pDir);
}

inline TADDR PEDecoder::GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckDirectory(pDir, 0, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        POSTCONDITION(CheckPointer((void *)RETVAL, NULL_OK));
        CANNOT_TAKE_LOCK;
    }
    CONTRACT_END;

    RETURN GetRvaData(VAL32(pDir->VirtualAddress));
}

inline TADDR PEDecoder::GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir, COUNT_T *pSize) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckDirectory(pDir, 0, NULL_OK));
        PRECONDITION(CheckPointer(pSize));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        POSTCONDITION(CheckPointer((void *)RETVAL, NULL_OK));
        CANNOT_TAKE_LOCK;
    }
    CONTRACT_END;

    *pSize = VAL32(pDir->Size);

    RETURN GetRvaData(VAL32(pDir->VirtualAddress));
}

inline TADDR PEDecoder::GetInternalAddressData(SIZE_T address) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckInternalAddress(address, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer((void *)RETVAL));
    }
    CONTRACT_END;

    RETURN GetRvaData(InternalAddressToRva(address));
}

inline BOOL PEDecoder::HasCorHeader() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        SUPPORTS_DAC;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_COMHEADER);
}

inline BOOL PEDecoder::IsILOnly() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Pretend that ready-to-run images are IL-only
    return((GetCorHeader()->Flags & VAL32(COMIMAGE_FLAGS_ILONLY)) != 0) || HasReadyToRunHeader();
}

inline COUNT_T PEDecoder::RvaToOffset(RVA rva) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckRva(rva,NULL_OK));
        NOTHROW;
        CANNOT_TAKE_LOCK;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;
    if(rva > 0)
    {
        IMAGE_SECTION_HEADER *section = RvaToSection(rva);
        if (section == NULL)
            return rva;

        return rva - VAL32(section->VirtualAddress) + VAL32(section->PointerToRawData);
    }
    else return 0;
}

inline RVA PEDecoder::OffsetToRva(COUNT_T fileOffset) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckOffset(fileOffset,NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;
    if(fileOffset > 0)
    {
        IMAGE_SECTION_HEADER *section = OffsetToSection(fileOffset);
        PREFIX_ASSUME (section!=NULL); //TODO: actually it is possible that it si null we need to rethink how we handle this cases and do better there

        return fileOffset - VAL32(section->PointerToRawData) + VAL32(section->VirtualAddress);
    }
    else return 0;
}


inline BOOL PEDecoder::IsStrongNameSigned() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return ((GetCorHeader()->Flags & VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED)) != 0);
}


inline BOOL PEDecoder::HasStrongNameSignature() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return (GetCorHeader()->StrongNameSignature.VirtualAddress != 0);
}

inline CHECK PEDecoder::CheckStrongNameSignature() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(HasCorHeader());
        PRECONDITION(HasStrongNameSignature());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    return CheckDirectory(&GetCorHeader()->StrongNameSignature, IMAGE_SCN_MEM_WRITE, NULL_OK);
}

inline PTR_CVOID PEDecoder::GetStrongNameSignature(COUNT_T *pSize) const
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        PRECONDITION(HasCorHeader());
        PRECONDITION(HasStrongNameSignature());
        PRECONDITION(CheckStrongNameSignature());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->StrongNameSignature;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN dac_cast<PTR_CVOID>(GetDirectoryData(pDir));
}

inline BOOL PEDecoder::HasTls() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_TLS);
}

inline CHECK PEDecoder::CheckTls() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    CHECK(CheckDirectoryEntry(IMAGE_DIRECTORY_ENTRY_TLS, 0, NULL_OK));

    IMAGE_TLS_DIRECTORY *pTlsHeader = (IMAGE_TLS_DIRECTORY *) GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_TLS);

    CHECK(CheckUnderflow(VALPTR(pTlsHeader->EndAddressOfRawData), VALPTR(pTlsHeader->StartAddressOfRawData)));
    CHECK(VALPTR(pTlsHeader->EndAddressOfRawData) - VALPTR(pTlsHeader->StartAddressOfRawData) <= COUNT_T_MAX);

    CHECK(CheckInternalAddress(VALPTR(pTlsHeader->StartAddressOfRawData),
        (COUNT_T) (VALPTR(pTlsHeader->EndAddressOfRawData) - VALPTR(pTlsHeader->StartAddressOfRawData))));

    CHECK_OK;
}

inline PTR_VOID PEDecoder::GetTlsRange(COUNT_T * pSize) const
{
    CONTRACT(void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(HasTls());
        PRECONDITION(CheckTls());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    IMAGE_TLS_DIRECTORY *pTlsHeader =
        PTR_IMAGE_TLS_DIRECTORY(GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_TLS));

    if (pSize != 0)
        *pSize = (COUNT_T) (VALPTR(pTlsHeader->EndAddressOfRawData) - VALPTR(pTlsHeader->StartAddressOfRawData));
    PREFIX_ASSUME (pTlsHeader!=NULL);
    RETURN PTR_VOID(GetInternalAddressData(pTlsHeader->StartAddressOfRawData));
}

inline UINT32 PEDecoder::GetTlsIndex() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(HasTls());
        PRECONDITION(CheckTls());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    IMAGE_TLS_DIRECTORY *pTlsHeader = (IMAGE_TLS_DIRECTORY *) GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_TLS);

    return (UINT32)*PTR_UINT32(GetInternalAddressData((SIZE_T)VALPTR(pTlsHeader->AddressOfIndex)));
}

inline IMAGE_COR20_HEADER *PEDecoder::GetCorHeader() const
{
    CONTRACT(IMAGE_COR20_HEADER *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(HasCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (m_pCorHeader == NULL)
        const_cast<PEDecoder *>(this)->m_pCorHeader =
            dac_cast<PTR_IMAGE_COR20_HEADER>(FindCorHeader());

    RETURN m_pCorHeader;
}

inline BOOL PEDecoder::IsNativeMachineFormat() const
{
    if (!HasContents() || !HasNTHeaders() )
        return FALSE;
    _ASSERTE(m_pNTHeaders);
    WORD expectedFormat = HasCorHeader() && HasReadyToRunHeader() ?
        IMAGE_FILE_MACHINE_NATIVE_NI :
        IMAGE_FILE_MACHINE_NATIVE;
    //do not call GetNTHeaders as we do not want to bother with PE32->PE32+ conversion
    return m_pNTHeaders->FileHeader.Machine==expectedFormat;
}

inline BOOL PEDecoder::IsI386() const
{
    if (!HasContents() || !HasNTHeaders() )
        return FALSE;
    _ASSERTE(m_pNTHeaders);
    //do not call GetNTHeaders as we do not want to bother with PE32->PE32+ conversion
    return m_pNTHeaders->FileHeader.Machine==IMAGE_FILE_MACHINE_I386;
}

// static
inline PTR_IMAGE_SECTION_HEADER PEDecoder::FindFirstSection(IMAGE_NT_HEADERS * pNTHeaders)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return dac_cast<PTR_IMAGE_SECTION_HEADER>(
        dac_cast<TADDR>(pNTHeaders) +
        offsetof(IMAGE_NT_HEADERS, OptionalHeader) +
        VAL16(pNTHeaders->FileHeader.SizeOfOptionalHeader));
}

inline COUNT_T PEDecoder::GetNumberOfSections() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return VAL16(FindNTHeaders()->FileHeader.NumberOfSections);
}


inline DWORD PEDecoder::GetImageIdentity() const
{
    WRAPPER_NO_CONTRACT;
    return GetTimeDateStamp() ^ GetCheckSum() ^ DWORD( GetVirtualSize() );
}


inline PTR_IMAGE_SECTION_HEADER PEDecoder::FindFirstSection() const
{
    CONTRACT(IMAGE_SECTION_HEADER *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN FindFirstSection(FindNTHeaders());
}

inline IMAGE_NT_HEADERS *PEDecoder::FindNTHeaders() const
{
    CONTRACT(IMAGE_NT_HEADERS *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN PTR_IMAGE_NT_HEADERS(m_base + VAL32(PTR_IMAGE_DOS_HEADER(m_base)->e_lfanew));
}

inline IMAGE_COR20_HEADER *PEDecoder::FindCorHeader() const
{
    CONTRACT(IMAGE_COR20_HEADER *)
    {
        INSTANCE_CHECK;
        PRECONDITION(HasCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    const IMAGE_COR20_HEADER * pCor=PTR_IMAGE_COR20_HEADER(GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_COMHEADER));
    RETURN ((IMAGE_COR20_HEADER*)pCor);
}

inline CHECK PEDecoder::CheckBounds(RVA rangeBase, COUNT_T rangeSize, RVA rva)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(rangeBase, rangeSize));
    CHECK(rva >= rangeBase);
    CHECK(rva <= rangeBase + rangeSize);
    CHECK_OK;
}

inline CHECK PEDecoder::CheckBounds(RVA rangeBase, COUNT_T rangeSize, RVA rva, COUNT_T size)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(rangeBase, rangeSize));
    CHECK(CheckOverflow(rva, size));
    CHECK(rva >= rangeBase);
    CHECK(rva + size <= rangeBase + rangeSize);
    CHECK_OK;
}

inline CHECK PEDecoder::CheckBounds(const void *rangeBase, COUNT_T rangeSize, const void *pointer)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(dac_cast<PTR_CVOID>(rangeBase), rangeSize));
    CHECK(dac_cast<TADDR>(pointer) >= dac_cast<TADDR>(rangeBase));
    CHECK(dac_cast<TADDR>(pointer) <= dac_cast<TADDR>(rangeBase) + rangeSize);
    CHECK_OK;
}

inline CHECK PEDecoder::CheckBounds(PTR_CVOID rangeBase, COUNT_T rangeSize, PTR_CVOID pointer, COUNT_T size)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(rangeBase, rangeSize));
    CHECK(CheckOverflow(pointer, size));
    CHECK(dac_cast<TADDR>(pointer) >= dac_cast<TADDR>(rangeBase));
    CHECK(dac_cast<TADDR>(pointer) + size <= dac_cast<TADDR>(rangeBase) + rangeSize);
    CHECK_OK;
}

inline void PEDecoder::GetPEKindAndMachine(DWORD * pdwPEKind, DWORD *pdwMachine)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD dwKind=0,dwMachine=0;
    if(HasContents() && HasNTHeaders())
    {
        dwMachine = GetMachine();

        BOOL fIsPE32Plus = !Has32BitNTHeaders();

        if (fIsPE32Plus)
            dwKind |= (DWORD)pe32Plus;

        if (HasCorHeader())
        {
            IMAGE_COR20_HEADER * pCorHdr = GetCorHeader();
            if(pCorHdr != NULL)
            {
                DWORD dwCorFlags = pCorHdr->Flags;

                if (dwCorFlags & VAL32(COMIMAGE_FLAGS_ILONLY))
                {
                    dwKind |= (DWORD)peILonly;
#ifdef HOST_64BIT
                    // compensate for shim promotion of PE32/ILONLY headers to PE32+ on WIN64
                    if (fIsPE32Plus && (GetMachine() == IMAGE_FILE_MACHINE_I386))
                        dwKind &= ~((DWORD)pe32Plus);
#endif
                }

                if (COR_IS_32BIT_REQUIRED(dwCorFlags))
                    dwKind |= (DWORD)pe32BitRequired;
                else if (COR_IS_32BIT_PREFERRED(dwCorFlags))
                    dwKind |= (DWORD)pe32BitPreferred;

                // compensate for MC++ peculiarity
                if(dwKind == 0)
                    dwKind = (DWORD)pe32BitRequired;
            }
            else
            {
                dwKind |= (DWORD)pe32Unmanaged;
            }

            if (HasReadyToRunHeader())
            {
                if (dwMachine == IMAGE_FILE_MACHINE_NATIVE_NI)
                {
                    // Supply the original machine type to the assembly binder
                    dwMachine = IMAGE_FILE_MACHINE_NATIVE;
                }

                if ((GetReadyToRunHeader()->CoreHeader.Flags & READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE) != 0)
                {
                    // Supply the original PEKind/Machine to the assembly binder to make the full assembly name look like the original
                    dwKind = peILonly;
                    dwMachine = IMAGE_FILE_MACHINE_I386;
                }
            }
        }
        else
        {
            dwKind |= (DWORD)pe32Unmanaged;
        }
    }

    *pdwPEKind = dwKind;
    *pdwMachine = dwMachine;
}

inline BOOL PEDecoder::IsPlatformNeutral()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD dwKind, dwMachine;
    GetPEKindAndMachine(&dwKind, &dwMachine);
    return ((dwKind & (peILonly | pe32Plus | pe32BitRequired)) == peILonly) && (dwMachine == IMAGE_FILE_MACHINE_I386);
}

inline BOOL PEDecoder::IsComponentAssembly() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return HasReadyToRunHeader() && (m_pReadyToRunHeader->CoreHeader.Flags & READYTORUN_FLAG_COMPONENT) != 0;
}

inline BOOL PEDecoder::HasReadyToRunHeader() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (m_flags & FLAG_HAS_NO_READYTORUN_HEADER)
        return FALSE;

    if (m_pReadyToRunHeader != NULL)
        return TRUE;

    return FindReadyToRunHeader() != NULL;
}

inline READYTORUN_HEADER * PEDecoder::GetReadyToRunHeader() const
{
    CONTRACT(READYTORUN_HEADER *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(HasCorHeader());
        PRECONDITION(HasReadyToRunHeader());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
        CANNOT_TAKE_LOCK;
    }
    CONTRACT_END;

    if (m_pReadyToRunHeader != NULL)
        RETURN m_pReadyToRunHeader;

    RETURN FindReadyToRunHeader();
}

#endif // _PEDECODER_INL_
