// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include <windows.h>
#include <corhdr.h>
#include "corerror.h"
#include "pedecoder.h"


static const char g_szCORMETA[] = ".cormeta";


HRESULT CLiteWeightStgdbRW::FindImageMetaData(PVOID pImage, DWORD dwFileLength, BOOL bMappedImage, PVOID *ppMetaData, ULONG *pcbMetaData)
{
#ifndef DACCESS_COMPILE
    PEDecoder pe;

    // We need to use different PEDecoder initialization based on the type of data we give it.
    // We use the one with a 'bool' as the second argument when dealing with a mapped file,
    // and we use the one that takes a COUNT_T as the second argument when dealing with a
    // flat file.
    if (bMappedImage)
    {
        if (FAILED(pe.Init(pImage, false)) ||
            !pe.CheckNTHeaders())
        {
            return COR_E_BADIMAGEFORMAT;
        }
    }
    else
    {
        pe.Init(pImage, (COUNT_T)dwFileLength);
    }

    // Minimally validate image
    if (!pe.CheckCorHeader())
        return COR_E_BADIMAGEFORMAT;


    COUNT_T size = 0;

    *ppMetaData = (void *)pe.GetMetadata(&size);

    // Couldn't find any IL metadata in this image
    if (*ppMetaData == NULL)
        return CLDB_E_NO_DATA;

    if (pcbMetaData != NULL)
        *pcbMetaData = size;

    return S_OK;
#else
    DacNotImpl();
    return E_NOTIMPL;
#endif
} // CLiteWeightStgdbRW::FindImageMetaData

//
// Note: Remove once defined in winnt.h
//
typedef struct ANON_OBJECT_HEADER2 {
    WORD    Sig1;            // Must be IMAGE_FILE_MACHINE_UNKNOWN
    WORD    Sig2;            // Must be 0xffff
    WORD    Version;         // >= 2 (implies the CLSID field, Flags and metadata info are present)
    WORD    Machine;
    DWORD   TimeDateStamp;
    CLSID   ClassID;         // Used to invoke CoCreateInstance
    DWORD   SizeOfData;      // Size of data that follows the header
    DWORD   Flags;
    DWORD   MetaDataSize;    // Size of CLR metadata
    DWORD   MetaDataOffset;  // Offset of CLR metadata
} ANON_OBJECT_HEADER2;

#define ANON_OBJECT_HAS_CORMETA 0x00000001
#define ANON_OBJECT_IS_PUREMSIL 0x00000002

HRESULT CLiteWeightStgdbRW::FindObjMetaData(PVOID pImage, DWORD dwFileLength, PVOID *ppMetaData, ULONG *pcbMetaData)
{
    DWORD   dwSize = 0;
    DWORD   dwOffset = 0;

    ANON_OBJECT_HEADER2 *pAnonImageHdr = (ANON_OBJECT_HEADER2 *) pImage;    // Anonymous object header

    // Check to see if this is a LTCG object
    if (dwFileLength >= sizeof(ANON_OBJECT_HEADER2) &&
         pAnonImageHdr->Sig1 == VAL16(IMAGE_FILE_MACHINE_UNKNOWN) &&
         pAnonImageHdr->Sig2 == VAL16(IMPORT_OBJECT_HDR_SIG2))
    {
        // Version 1 anonymous objects don't have metadata info
        if (VAL16(pAnonImageHdr->Version) < 2)
            goto BadFormat;

        // Anonymous objects contain the metadata info in the header
        dwOffset = VAL32(pAnonImageHdr->MetaDataOffset);
        dwSize = VAL32(pAnonImageHdr->MetaDataSize);
    }
    else
    {
        // Check to see if we have enough data
        if (dwFileLength < sizeof(IMAGE_FILE_HEADER))
            goto BadFormat;

        IMAGE_FILE_HEADER *pImageHdr = (IMAGE_FILE_HEADER *) pImage;            // Header for the .obj file.

        // Walk each section looking for .cormeta.
        DWORD nSections = VAL16(pImageHdr->NumberOfSections);

        // Check to see if we have enough data
        S_UINT32 nSectionsSize = S_UINT32(sizeof(IMAGE_FILE_HEADER)) + S_UINT32(nSections) * S_UINT32(sizeof(IMAGE_SECTION_HEADER));
        if (nSectionsSize.IsOverflow() || (dwFileLength < nSectionsSize.Value()))
            goto BadFormat;

        IMAGE_SECTION_HEADER *pSectionHdr = (IMAGE_SECTION_HEADER *)(pImageHdr + 1);  // Section header.

        for (DWORD i=0; i<nSections;  i++, pSectionHdr++)
        {
            // Simple comparison to section name.
            if (memcmp((const char *) pSectionHdr->Name, g_szCORMETA, sizeof(pSectionHdr->Name)) == 0)
            {
                dwOffset = VAL32(pSectionHdr->PointerToRawData);
                dwSize = VAL32(pSectionHdr->SizeOfRawData);
                break;
            }
        }
    }

    if (dwOffset == 0 || dwSize == 0)
        goto BadFormat;

    // Check that raw data in the section is actually within the file.
    {
        S_UINT32 dwEndOffset = S_UINT32(dwOffset) + S_UINT32(dwSize);
        if ((dwOffset >= dwFileLength) || dwEndOffset.IsOverflow() || (dwEndOffset.Value() > dwFileLength))
            goto BadFormat;
    }

    *ppMetaData = (PVOID) ((ULONG_PTR) pImage + dwOffset);
    *pcbMetaData = dwSize;
    return (S_OK);

BadFormat:
    *ppMetaData = NULL;
    *pcbMetaData = 0;
    return (COR_E_BADIMAGEFORMAT);
}
