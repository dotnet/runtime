// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDFileFormat.cpp
//

//
// This file contains a set of helpers to verify and read the file format.
// This code does not handle the paging of the data, or different types of
// I/O.  See the StgTiggerStorage and StgIO code for this level of support.
//
//*****************************************************************************

#include "stdafx.h"                     // Standard header file.
#include "mdfileformat.h"               // The format helpers.
#include "posterror.h"                  // Error handling code.

//*****************************************************************************
// Verify the signature at the front of the file to see what type it is.
//*****************************************************************************
#define STORAGE_MAGIC_OLD_SIG   0x2B4D4F43  // +MOC (old version of BSJB signature code:STORAGE_MAGIC_SIG)
HRESULT
MDFormat::VerifySignature(
    PSTORAGESIGNATURE pSig,     // The signature to check.
    ULONG             cbData)
{
    HRESULT hr = S_OK;

    // If signature didn't match, you shouldn't be here.
    ULONG dwSignature = pSig->GetSignature();
    if (dwSignature == STORAGE_MAGIC_OLD_SIG)
    {
        Debug_ReportError("Invalid MetaData storage signature - old magic signature +MOC.");
        return PostError(CLDB_E_FILE_OLDVER, 1, 0);
    }
    if (dwSignature != STORAGE_MAGIC_SIG)
    {
        Debug_ReportError("Invalid MetaData storage signature - unrecognized magic signature, should be BSJB.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }

    // Check for overflow
    ULONG lVersionString = pSig->GetVersionStringLength();
    ULONG sum = sizeof(STORAGESIGNATURE) + lVersionString;
    if ((sum < sizeof(STORAGESIGNATURE)) || (sum < lVersionString))
    {
        Debug_ReportError("Invalid MetaData storage signature - version string too long, integer overflow.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }

    // Check for invalid version string size
    if ((sizeof(STORAGESIGNATURE) + lVersionString) > cbData)
    {
        Debug_ReportError("Invalid MetaData storage signature - version string too long.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }

    // Check that the version string is null terminated. This string
    // is ANSI, so no double-null checks need to be made.
    {
        BYTE *pStart = &pSig->pVersion[0];
        BYTE *pEnd = pStart + lVersionString + 1; // Account for terminating NULL
        BYTE *pCur;

        for (pCur = pStart; pCur < pEnd; pCur++)
        {
            if (*pCur == 0)
                break;
        }

        // If we got to the end without hitting a NULL, we have a bad version string
        if (pCur == pEnd)
        {
            Debug_ReportError("Invalid MetaData storage signature - version string has not null-terminator.");
            return PostError(CLDB_E_FILE_CORRUPT);
        }
    }

    // Only a specific version of the 0.x format is supported by this code
    // in order to support the NT 5 beta clients which used this format.
    if (pSig->GetMajorVer() == FILE_VER_MAJOR_v0)
    {
        if (pSig->GetMinorVer() < FILE_VER_MINOR_v0)
        {
            Debug_ReportError("Invalid MetaData storage signature - unrecognized version, should be 1.1.");
            hr = CLDB_E_FILE_OLDVER;
        }
    }
    else
    // There is currently no code to migrate an old format of the 1.x.  This
    // would be added only under special circumstances.
    if ((pSig->GetMajorVer() != FILE_VER_MAJOR) || (pSig->GetMinorVer() != FILE_VER_MINOR))
    {
        Debug_ReportError("Invalid MetaData storage signature - unrecognized version, should be 1.1.");
        hr = CLDB_E_FILE_OLDVER;
    }

    if (FAILED(hr))
        hr = PostError(hr, (int)pSig->GetMajorVer(), (int)pSig->GetMinorVer());
    return hr;
} // MDFormat::VerifySignature

//*****************************************************************************
// Skip over the header and find the actual stream data.
// It doesn't perform any checks for buffer overflow - use GetFirstStream_Verify
// instead.
//*****************************************************************************
PSTORAGESTREAM
MDFormat::GetFirstStream(
    PSTORAGEHEADER pHeader,     // Return copy of header struct.
    const void    *pvMd)        // Pointer to the full file.
{
    const BYTE *pbMd;

    // Header data starts after signature.
    pbMd = (const BYTE *) pvMd;
    pbMd += sizeof(STORAGESIGNATURE);
    pbMd += ((STORAGESIGNATURE*)pvMd)->GetVersionStringLength();
    PSTORAGEHEADER pHdr = (PSTORAGEHEADER) pbMd;
    *pHeader = *pHdr;
    pbMd += sizeof(STORAGEHEADER);

    // ECMA specifies that the flags field is "reserved, must be 0".
	if (pHdr->GetFlags() != 0)
		return NULL;

    // The pointer is now at the first stream in the list.
    return ((PSTORAGESTREAM) pbMd);
} // MDFormat::GetFirstStream

//*****************************************************************************
// Skip over the header and find the actual stream data.  Secure version of
// GetFirstStream method.
// The header is supposed to be verified by VerifySignature.
//
// Returns pointer to the first stream (behind storage header) and the size of
// the remaining buffer in *pcbMd (could be 0).
// Returns NULL if there is not enough buffer for reading the headers. The *pcbMd
// could be changed if NULL returned.
//
// Caller has to check available buffer size before using the first stream.
//*****************************************************************************
PSTORAGESTREAM
MDFormat::GetFirstStream_Verify(
    PSTORAGEHEADER pHeader,     // Return copy of header struct.
    const void    *pvMd,        // Pointer to the full file.
    ULONG         *pcbMd)       // [in, out] Size of pvMd buffer (we don't want to read behind it)
{
    const BYTE *pbMd;

    // Header data starts after signature.
    pbMd = (const BYTE *)pvMd;
    // Check read buffer overflow
    if (*pcbMd < sizeof(STORAGESIGNATURE))
    {
        Debug_ReportError("Invalid MetaData - Storage signature doesn't fit.");
        return NULL;
    }
    pbMd += sizeof(STORAGESIGNATURE);
    *pcbMd -= sizeof(STORAGESIGNATURE);

    ULONG cbVersionString = ((STORAGESIGNATURE *)pvMd)->GetVersionStringLength();
    // Check read buffer overflow
    if (*pcbMd < cbVersionString)
    {
        Debug_ReportError("Invalid MetaData storage signature - Version string doesn't fit.");
        return NULL;
    }
    pbMd += cbVersionString;
    *pcbMd -= cbVersionString;

    // Is there enough space for storage header?
    if (*pcbMd < sizeof(STORAGEHEADER))
    {
        Debug_ReportError("Invalid MetaData storage header - Storage header doesn't fit.");
        return NULL;
    }
    PSTORAGEHEADER pHdr = (PSTORAGEHEADER) pbMd;
    *pHeader = *pHdr;
    pbMd += sizeof(STORAGEHEADER);
    *pcbMd -= sizeof(STORAGEHEADER);

    // ECMA specifies that the flags field is "reserved, must be 0".
    if (pHdr->GetFlags() != 0)
    {
        Debug_ReportError("Invalid MetaData storage header - Flags are not 0.");
        return NULL;
    }

    // The pointer is now at the first stream in the list.
    return (PSTORAGESTREAM)pbMd;
} // MDFormat::GetFirstStream
