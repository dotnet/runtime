// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// CorDecoderHelpers.h
//
// Static template helper functions for COR header data access.
// These are shared between PEDecoder and WebcilDecoder to avoid
// duplicating logic that operates on IMAGE_COR20_HEADER fields
// (metadata, resources, strong name, entry points, IL methods, etc.).
//
// The template parameter TDecoder must provide:
//   - IMAGE_COR20_HEADER *GetCorHeader() const
//   - CHECK CheckCorHeader() const
//   - CHECK CheckRva(RVA rva, ...) const
//   - TADDR GetRvaData(RVA rva, ...) const
//   - BOOL HasDirectoryEntry(int entry) const
//   - TADDR GetDirectoryEntryData(int entry, COUNT_T *pSize) const
//   - BOOL HasCorHeader() const
// --------------------------------------------------------------------------------

#ifndef CORDECODERHELPERS_H_
#define CORDECODERHELPERS_H_

#include "corhdr.h"
#include "check.h"
#include "contract.h"
#include "corhlpr.h"
#include "safemath.h"

typedef DPTR(COR_ILMETHOD_TINY) PTR_COR_ILMETHOD_TINY;
typedef DPTR(COR_ILMETHOD_FAT) PTR_COR_ILMETHOD_FAT;
typedef DPTR(COR_ILMETHOD_SECT_SMALL) PTR_COR_ILMETHOD_SECT_SMALL;
typedef DPTR(COR_ILMETHOD_SECT_FAT) PTR_COR_ILMETHOD_SECT_FAT;

namespace CorDecoderHelpers
{

// Checking utilities — pure static, no decoder state needed
inline CHECK CheckBounds(RVA rangeBase, COUNT_T rangeSize, RVA rva)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(rangeBase, rangeSize));
    CHECK(rva >= rangeBase);
    CHECK(rva <= rangeBase + rangeSize);
    CHECK_OK;
}

inline CHECK CheckBounds(RVA rangeBase, COUNT_T rangeSize, RVA rva, COUNT_T size)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(rangeBase, rangeSize));
    CHECK(CheckOverflow(rva, size));
    CHECK(rva >= rangeBase);
    CHECK(rva + size <= rangeBase + rangeSize);
    CHECK_OK;
}

inline CHECK CheckBounds(const void *rangeBase, COUNT_T rangeSize, const void *pointer)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(dac_cast<PTR_CVOID>(rangeBase), rangeSize));
    CHECK(dac_cast<TADDR>(pointer) >= dac_cast<TADDR>(rangeBase));
    CHECK(dac_cast<TADDR>(pointer) <= dac_cast<TADDR>(rangeBase) + rangeSize);
    CHECK_OK;
}

inline CHECK CheckBounds(PTR_CVOID rangeBase, COUNT_T rangeSize, PTR_CVOID pointer, COUNT_T size)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    CHECK(CheckOverflow(rangeBase, rangeSize));
    CHECK(CheckOverflow(pointer, size));
    CHECK(dac_cast<TADDR>(pointer) >= dac_cast<TADDR>(rangeBase));
    CHECK(dac_cast<TADDR>(pointer) + size <= dac_cast<TADDR>(rangeBase) + rangeSize);
    CHECK_OK;
}

// ------------------------------------------------------------
// Strong name helpers
// ------------------------------------------------------------

template<typename TDecoder>
inline BOOL IsStrongNameSigned(const TDecoder& decoder)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(decoder.HasCorHeader());
    }
    CONTRACTL_END;

    return ((decoder.GetCorHeader()->Flags & VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED)) != 0);
}

template<typename TDecoder>
inline BOOL HasStrongNameSignature(const TDecoder& decoder)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(decoder.HasCorHeader());
    }
    CONTRACTL_END;

    return (decoder.GetCorHeader()->StrongNameSignature.VirtualAddress != 0);
}

// ------------------------------------------------------------
// Metadata
// ------------------------------------------------------------

template<typename TDecoder>
inline PTR_CVOID GetMetadata(const TDecoder& decoder, COUNT_T *pSize)
{
    CONTRACT(PTR_CVOID)
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(decoder.CheckCorHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &decoder.GetCorHeader()->MetaData;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RVA rva = VAL32(pDir->VirtualAddress);
    if (rva == 0)
        RETURN NULL;

    RETURN dac_cast<PTR_VOID>(decoder.GetRvaData(rva));
}

// ------------------------------------------------------------
// Entry point
// ------------------------------------------------------------

template<typename TDecoder>
inline BOOL HasManagedEntryPoint(const TDecoder& decoder)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(decoder.CheckCorHeader());
    }
    CONTRACTL_END;

    ULONG flags = decoder.GetCorHeader()->Flags;
    return (!(flags & VAL32(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (!IsNilToken(VAL32(IMAGE_COR20_HEADER_FIELD(*decoder.GetCorHeader(), EntryPointToken)))));
}

template<typename TDecoder>
inline ULONG GetEntryPointToken(const TDecoder& decoder)
{
    CONTRACT(ULONG)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(decoder.CheckCorHeader());
    }
    CONTRACT_END;

    RETURN VAL32(IMAGE_COR20_HEADER_FIELD(*decoder.GetCorHeader(), EntryPointToken));
}

// ------------------------------------------------------------
// Resources
// ------------------------------------------------------------

template<typename TDecoder>
inline const void *GetResources(const TDecoder& decoder, COUNT_T *pSize)
{
    CONTRACT(const void *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(decoder.CheckCorHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &decoder.GetCorHeader()->Resources;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RVA rva = VAL32(pDir->VirtualAddress);
    if (rva == 0)
        RETURN NULL;

    RETURN (void *)decoder.GetRvaData(rva);
}

template<typename TDecoder>
inline CHECK CheckResource(const TDecoder& decoder, COUNT_T offset)
{
    CONTRACT_CHECK
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(decoder.CheckCorHeader());
    }
    CONTRACT_CHECK_END;

    IMAGE_DATA_DIRECTORY *pDir = &decoder.GetCorHeader()->Resources;

    CHECK(CheckOverflow(VAL32(pDir->VirtualAddress), offset));

    RVA rva = VAL32(pDir->VirtualAddress) + offset;

    // Need at least a length DWORD for the resource size
    CHECK(decoder.CheckRva(rva, sizeof(DWORD)));

    // Make sure resource is within resource section
    COUNT_T resourceSize = GET_UNALIGNED_VAL32((LPVOID)decoder.GetRvaData(rva));
    CHECK(CheckBounds(VAL32(pDir->VirtualAddress), VAL32(pDir->Size),
                      rva + sizeof(DWORD), resourceSize));

    CHECK_OK;
}

template<typename TDecoder>
inline const void *GetResource(const TDecoder& decoder, COUNT_T offset, COUNT_T *pSize)
{
    CONTRACT(const void *)
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(decoder.CheckCorHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &decoder.GetCorHeader()->Resources;

    if (CheckResource(decoder, offset) == FALSE)
        RETURN NULL;

    void *resourceBlob = (void *)decoder.GetRvaData(VAL32(pDir->VirtualAddress) + offset);
    _ASSERTE(resourceBlob != NULL);

    if (pSize != NULL)
    {
        DWORD resourceSize = GET_UNALIGNED_VAL32(resourceBlob);
        *pSize = resourceSize;
    }

    // ECMA-335 II.24.2.4: Each resource entry is preceded by a 4-byte length prefix.
    RETURN (const void *)((BYTE *)resourceBlob + sizeof(DWORD));
}

// ------------------------------------------------------------
// Debug directory
// ------------------------------------------------------------

template<typename TDecoder>
inline PTR_IMAGE_DEBUG_DIRECTORY GetDebugDirectoryEntry(const TDecoder& decoder, UINT index)
{
    CONTRACT(PTR_IMAGE_DEBUG_DIRECTORY)
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (!decoder.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG))
        RETURN NULL;

    COUNT_T cbDebugDir;
    TADDR taDebugDir = decoder.GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_DEBUG, &cbDebugDir);

    if (taDebugDir == (TADDR)0)
        RETURN NULL;

    UINT cNumEntries = cbDebugDir / sizeof(IMAGE_DEBUG_DIRECTORY);
    if (index >= cNumEntries)
        RETURN NULL;

    PTR_IMAGE_DEBUG_DIRECTORY pDebugEntry = dac_cast<PTR_IMAGE_DEBUG_DIRECTORY>(taDebugDir);
    pDebugEntry += index;

    RETURN pDebugEntry;
}

// ------------------------------------------------------------
// IL method validation
// ------------------------------------------------------------

template<typename TDecoder>
inline CHECK CheckILMethod(TDecoder& decoder, RVA rva)
{
    CONTRACT_CHECK
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    // We need to have at least the tiny header
    CHECK(decoder.CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_TINY)));

    TADDR pIL = decoder.GetRvaData(rva);

    PTR_COR_ILMETHOD_TINY pMethodTiny = PTR_COR_ILMETHOD_TINY(pIL);

    if (pMethodTiny->IsTiny())
    {
        // Tiny header has no optional sections - we are done.
        CHECK(decoder.CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_TINY) + pMethodTiny->GetCodeSize()));
        CHECK_OK;
    }

    //
    // Fat header
    //

    CHECK(decoder.CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_FAT)));

    PTR_COR_ILMETHOD_FAT pMethodFat = PTR_COR_ILMETHOD_FAT(pIL);

    CHECK(pMethodFat->IsFat());

    S_UINT32 codeEnd = S_UINT32(4) * S_UINT32(pMethodFat->GetSize()) + S_UINT32(pMethodFat->GetCodeSize());
    CHECK(!codeEnd.IsOverflow());

    // Check minimal size of the header
    CHECK(pMethodFat->GetSize() >= (sizeof(COR_ILMETHOD_FAT) / 4));

    CHECK(decoder.CheckRva(rva, codeEnd.Value()));

    if (!pMethodFat->More())
    {
        CHECK_OK;
    }

    // DACized copy of code:COR_ILMETHOD_FAT::GetSect
    TADDR pSect = AlignUp(pIL + codeEnd.Value(), 4);

    //
    // Optional sections following the code
    //

    while (true)
    {
        CHECK(decoder.CheckRva(rva, UINT32(pSect - pIL) + sizeof(IMAGE_COR_ILMETHOD_SECT_SMALL)));

        PTR_COR_ILMETHOD_SECT_SMALL pSectSmall = PTR_COR_ILMETHOD_SECT_SMALL(pSect);

        UINT32 sectSize;

        if (pSectSmall->IsSmall())
        {
            sectSize = pSectSmall->DataSize;

            // Workaround for bug in shipped compilers - see comment in code:COR_ILMETHOD_SECT::DataSize
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_SMALL::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL));
        }
        else
        {
            CHECK(decoder.CheckRva(rva, UINT32(pSect - pIL) + sizeof(IMAGE_COR_ILMETHOD_SECT_FAT)));

            PTR_COR_ILMETHOD_SECT_FAT pSectFat = PTR_COR_ILMETHOD_SECT_FAT(pSect);

            sectSize = pSectFat->GetDataSize();

            // Workaround for bug in shipped compilers - see comment in code:COR_ILMETHOD_SECT::DataSize
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_FAT::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
        }

        // Section has to be non-empty to avoid infinite loop below
        CHECK(sectSize > 0);

        S_UINT32 sectEnd = S_UINT32(UINT32(pSect - pIL)) + S_UINT32(sectSize);
        CHECK(!sectEnd.IsOverflow());

        CHECK(decoder.CheckRva(rva, sectEnd.Value()));

        if (!pSectSmall->More())
        {
            CHECK_OK;
        }

        // DACized copy of code:COR_ILMETHOD_FAT::Next
        pSect = AlignUp(pIL + sectEnd.Value(), 4);
    }
}

} // namespace CorDecoderHelpers

#endif // CORDECODERHELPERS_H_
