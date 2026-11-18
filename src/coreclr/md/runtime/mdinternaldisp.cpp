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
#include "pedecoder.h"
#include "metamodel.h"

#ifdef FEATURE_METADATA_INTERNAL_APIS

//*****************************************************************************
// CheckFileFormat
// This function verifies that the in-memory metadata uses the compressed (#~)
// table stream. The uncompressed (#-) format is not supported for loaded images.
//*****************************************************************************
HRESULT
CheckFileFormat(
    LPVOID        pData,
    ULONG         cbData)
{
    HRESULT        hr = NOERROR;
    STORAGEHEADER  sHdr;        // Header for the storage.
    PSTORAGESTREAM pStream;     // Pointer to each stream.
    int            i;
    ULONG          cbStreamBuffer;
    bool           fFoundTables = false;

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
            if (fFoundTables)
            {
                Debug_ReportError("Compressed model stream #~ is second important stream.");
                hr = CLDB_E_FILE_CORRUPT;
                goto ErrExit;
            }
            fFoundTables = true;
        }
        else if (strcmp(pStream->GetName(), ENC_MODEL_STREAM_A) == 0)
        {
            Debug_ReportError("Uncompressed model stream #- is not supported.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
        }

        // Pick off the next stream if there is one.
        pStream = pNext;
        cbStreamBuffer = (ULONG)((LPBYTE)pData + cbData - (LPBYTE)pNext);
    }

    if (!fFoundTables)
    {   // Didn't find a good stream.
        Debug_ReportError("Cannot find MetaData stream.");
        hr = CLDB_E_FILE_CORRUPT;
    }

ErrExit:
    return hr;
} // CheckFileFormat



//*****************************************************************************
// GetMDInternalInterface.
// Returns a read-only internal interface on a metadata section that uses the
// compressed (#~) table stream.
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

    if (ppIUnk == NULL)
        IfFailGo(E_INVALIDARG);

    IfFailGo( CheckFileFormat(pData, cbData) );

    pInternalRO = new (nothrow) MDInternalRO;
    IfNullGo( pInternalRO );

    IfFailGo( pInternalRO->Init(const_cast<void*>(pData), cbData) );

    IfFailGo(pInternalRO->QueryInterface(riid, ppIUnk));

ErrExit:

    // clean up
    if ( pInternalRO )
        pInternalRO->Release();
    if ( pInternalROMDCommon )
        pInternalROMDCommon->Release();

    return hr;
}   // GetMDInternalInterface



#endif //FEATURE_METADATA_INTERNAL_APIS
