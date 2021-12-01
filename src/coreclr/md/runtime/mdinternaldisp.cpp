// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
//  File: MDInternalDisp.CPP
//

//  Notes:
//
//
// ===========================================================================
#include "stdafx.h"
#include "mdinternaldisp.h"
#include "mdinternalro.h"
#include "posterror.h"
#include "corpriv.h"
#include "assemblymdinternaldisp.h"
#include "pedecoder.h"
#include "metamodel.h"

#ifdef FEATURE_METADATA_INTERNAL_APIS

// forward declaration
HRESULT GetInternalWithRWFormat(
    LPVOID      pData,
    ULONG       cbData,
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk);              // [out] Return interface on success.

//*****************************************************************************
// CheckFileFormat
// This function will determine if the in-memory image is a readonly, readwrite,
// or ICR format.
//*****************************************************************************
HRESULT
CheckFileFormat(
    LPVOID        pData,
    ULONG         cbData,
    MDFileFormat *pFormat)  // [OUT] the file format
{
    HRESULT        hr = NOERROR;
    STORAGEHEADER  sHdr;        // Header for the storage.
    PSTORAGESTREAM pStream;     // Pointer to each stream.
    int            i;
    ULONG          cbStreamBuffer;

    _ASSERTE(pFormat != NULL);

    *pFormat = MDFormat_Invalid;

    // Validate the signature of the format, or it isn't ours.
    if (FAILED(hr = MDFormat::VerifySignature((PSTORAGESIGNATURE) pData, cbData)))
        goto ErrExit;

    // Remaining buffer size behind the stream header (pStream).
    cbStreamBuffer = cbData;
    // Get back the first stream.
    pStream = MDFormat::GetFirstStream_Verify(&sHdr, pData, &cbStreamBuffer);
    if (pStream == NULL)
    {
        Debug_ReportError("Invalid MetaData storage signature - cannot get the first stream header.");
        IfFailGo(CLDB_E_FILE_CORRUPT);
    }

    // Loop through each stream and pick off the ones we need.
    for (i = 0; i < sHdr.GetiStreams(); i++)
    {
        // Do we have enough buffer to read stream header?
        if (cbStreamBuffer < sizeof(*pStream))
        {
            Debug_ReportError("Stream header is not within MetaData block.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        // Get next stream.
        PSTORAGESTREAM pNext = pStream->NextStream_Verify();
        if (pNext == NULL)
        {   // Stream header is corrupted.
            Debug_ReportError("Invalid stream header - cannot get next stream header.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }

        // Check that stream header is within the buffer.
        if (((LPBYTE)pStream >= ((LPBYTE)pData + cbData)) ||
            ((LPBYTE)pNext   >  ((LPBYTE)pData + cbData)))
        {
            Debug_ReportError("Stream header is not within MetaData block.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
        }

        // Check that the stream data starts and fits within the buffer.
        //  need two checks on size because of wraparound.
        if ((pStream->GetOffset() > cbData) ||
            (pStream->GetSize() > cbData) ||
            ((pStream->GetSize() + pStream->GetOffset()) < pStream->GetOffset()) ||
            ((pStream->GetSize() + pStream->GetOffset()) > cbData))
        {
            Debug_ReportError("Stream data are not within MetaData block.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
        }

        // Pick off the location and size of the data.

        if (strcmp(pStream->GetName(), COMPRESSED_MODEL_STREAM_A) == 0)
        {
            // Validate that only one of compressed/uncompressed is present.
            if (*pFormat != MDFormat_Invalid)
            {   // Already found a good stream.
                Debug_ReportError("Compressed model stream #~ is second important stream.");
                hr = CLDB_E_FILE_CORRUPT;
                goto ErrExit;
            }
            // Found the compressed meta data stream.
            *pFormat = MDFormat_ReadOnly;
        }
        else if (strcmp(pStream->GetName(), ENC_MODEL_STREAM_A) == 0)
        {
            // Validate that only one of compressed/uncompressed is present.
            if (*pFormat != MDFormat_Invalid)
            {   // Already found a good stream.
                Debug_ReportError("ENC model stream #- is second important stream.");
                hr = CLDB_E_FILE_CORRUPT;
                goto ErrExit;
            }
            // Found the ENC meta data stream.
            *pFormat = MDFormat_ReadWrite;
        }
        else if (strcmp(pStream->GetName(), SCHEMA_STREAM_A) == 0)
        {
            // Found the uncompressed format
            *pFormat = MDFormat_ICR;

            // keep going. We may find the compressed format later.
            // If so, we want to use the compressed format.
        }

        // Pick off the next stream if there is one.
        pStream = pNext;
        cbStreamBuffer = (ULONG)((LPBYTE)pData + cbData - (LPBYTE)pNext);
    }

    if (*pFormat == MDFormat_Invalid)
    {   // Didn't find a good stream.
        Debug_ReportError("Cannot find MetaData stream.");
        hr = CLDB_E_FILE_CORRUPT;
    }

ErrExit:
    return hr;
} // CheckFileFormat



//*****************************************************************************
// GetMDInternalInterface.
// This function will check the metadata section and determine if it should
// return an interface which implements ReadOnly or ReadWrite.
//*****************************************************************************
STDAPI GetMDInternalInterface(
    LPVOID      pData,
    ULONG       cbData,
    DWORD       flags,                  // [IN] ofRead or ofWrite.
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk)               // [out] Return interface on success.
{
    HRESULT     hr = NOERROR;
    MDInternalRO *pInternalRO = NULL;
    IMDCommon    *pInternalROMDCommon = NULL;
    MDFileFormat format;

    if (ppIUnk == NULL)
        IfFailGo(E_INVALIDARG);

    // Determine the file format we're trying to read.
    IfFailGo( CheckFileFormat(pData, cbData, &format) );

    // Found a fully-compressed, read-only format.
    if ( format == MDFormat_ReadOnly )
    {
        pInternalRO = new (nothrow) MDInternalRO;
        IfNullGo( pInternalRO );

        IfFailGo( pInternalRO->Init(const_cast<void*>(pData), cbData) );

        IfFailGo(pInternalRO->QueryInterface(riid, ppIUnk));

    }
    else
    {
        // Found a not-fully-compressed, ENC format.
        _ASSERTE( format == MDFormat_ReadWrite );
        IfFailGo( GetInternalWithRWFormat( pData, cbData, flags, riid, ppIUnk ) );
    }

ErrExit:

    // clean up
    if ( pInternalRO )
        pInternalRO->Release();
    if ( pInternalROMDCommon )
        pInternalROMDCommon->Release();

    return hr;
}   // GetMDInternalInterface



#endif //FEATURE_METADATA_INTERNAL_APIS
