// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// LiteWeightStgdb.cpp
//

//
// This contains definition of class CLiteWeightStgDB. This is light weight
// read-only implementation for accessing compressed meta data format.
//
//*****************************************************************************
#include "stdafx.h"                     // Precompiled header.
#include "mdfileformat.h"
#include "metamodelro.h"
#include "liteweightstgdb.h"
#include "metadatatracker.h"

__checkReturn
HRESULT _CallInitOnMemHelper(CLiteWeightStgdb<CMiniMd> *pStgdb, ULONG cbData, LPCVOID pData)
{
    return pStgdb->InitOnMem(cbData,pData);
}

//*****************************************************************************
// Open an in-memory metadata section for read
//*****************************************************************************
template <class MiniMd>
__checkReturn
HRESULT
CLiteWeightStgdb<MiniMd>::InitOnMem(
    ULONG   cbData,     // count of bytes in pData
    LPCVOID pData)      // points to meta data section in memory
{
    STORAGEHEADER  sHdr;                // Header for the storage.
    PSTORAGESTREAM pStream;             // Pointer to each stream.
    int            bFoundMd = false;    // true when compressed data found.
    int            i;                   // Loop control.
    HRESULT        hr = S_OK;
    ULONG          cbStreamBuffer;

    // Don't double open.
    _ASSERTE((m_pvMd == NULL) && (m_cbMd == 0));

    // Validate the signature of the format, or it isn't ours.
    IfFailGo(MDFormat::VerifySignature((PSTORAGESIGNATURE)pData, cbData));

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
        // Pick off the location and size of the data.
        if (pStream->GetOffset() >= cbData)
        {   // Stream data are not in the buffer. Stream header is corrupted.
            Debug_ReportError("Stream data are not within MetaData block.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        void *pvCurrentData = (void *)((BYTE *)pData + pStream->GetOffset());
        ULONG cbCurrentData = pStream->GetSize();

        // Get next stream.
        PSTORAGESTREAM pNext = pStream->NextStream_Verify();
        if (pNext == NULL)
        {   // Stream header is corrupted.
            Debug_ReportError("Invalid stream header - cannot get next stream header.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }

        // Range check
        if ((LPBYTE)pNext > ((LPBYTE)pData + cbData))
        {
            Debug_ReportError("Stream header is not within MetaData block.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }

        // Stream end must fit into the buffer and we have to check integer overflow (stream start is already checked)
        if ((((LPBYTE)pvCurrentData + cbCurrentData) < (LPBYTE)pvCurrentData)  ||
            (((LPBYTE)pvCurrentData + cbCurrentData) > ((LPBYTE)pData + cbData)))
        {
            Debug_ReportError("Stream data are not within MetaData block.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }

        // String pool.
        if (strcmp(pStream->GetName(), STRING_POOL_STREAM_A) == 0)
        {
            METADATATRACKER_ONLY(MetaDataTracker::NoteSection(TBL_COUNT + MDPoolStrings, pvCurrentData, cbCurrentData, 1));
            // String pool has to end with a null-terminator, therefore we don't have to check string pool content on access.
            // Shrink size of the pool to the last null-terminator found.
            while (cbCurrentData != 0)
            {
                if (((LPBYTE)pvCurrentData)[cbCurrentData - 1] == 0)
                {   // We have found last null terminator
                    break;
                }
                // Shrink size of the pool
                cbCurrentData--;
                Debug_ReportError("String heap/pool does not end with null-terminator ... shrinking the heap.");
            }
            // Initialize string heap with null-terminated block of data
            IfFailGo(m_MiniMd.m_StringHeap.Initialize(
                MetaData::DataBlob((BYTE *)pvCurrentData, cbCurrentData),
                FALSE));        // fCopyData
        }

        // Literal String Blob pool.
        else if (strcmp(pStream->GetName(), US_BLOB_POOL_STREAM_A) == 0)
        {
            METADATATRACKER_ONLY(MetaDataTracker::NoteSection(TBL_COUNT + MDPoolUSBlobs, pvCurrentData, cbCurrentData, 1));
            // Initialize user string heap with block of data
            IfFailGo(m_MiniMd.m_UserStringHeap.Initialize(
                MetaData::DataBlob((BYTE *)pvCurrentData, cbCurrentData),
                FALSE));        // fCopyData
        }

        // GUID pool.
        else if (strcmp(pStream->GetName(), GUID_POOL_STREAM_A) == 0)
        {
            METADATATRACKER_ONLY(MetaDataTracker::NoteSection(TBL_COUNT + MDPoolGuids, pvCurrentData, cbCurrentData, 1));
            // Initialize guid heap with block of data
            IfFailGo(m_MiniMd.m_GuidHeap.Initialize(
                MetaData::DataBlob((BYTE *)pvCurrentData, cbCurrentData),
                FALSE));        // fCopyData
        }

        // Blob pool.
        else if (strcmp(pStream->GetName(), BLOB_POOL_STREAM_A) == 0)
        {
            METADATATRACKER_ONLY(MetaDataTracker::NoteSection(TBL_COUNT + MDPoolBlobs, pvCurrentData, cbCurrentData, 1));
            // Initialize blob heap with block of data
            IfFailGo(m_MiniMd.m_BlobHeap.Initialize(
                MetaData::DataBlob((BYTE *)pvCurrentData, cbCurrentData),
                FALSE));        // fCopyData
        }

        // Found the compressed meta data stream.
        else if (strcmp(pStream->GetName(), COMPRESSED_MODEL_STREAM_A) == 0)
        {
            IfFailGo( m_MiniMd.InitOnMem(pvCurrentData, cbCurrentData) );
            bFoundMd = true;
        }

        // Found the hot meta data stream
        else if (strcmp(pStream->GetName(), HOT_MODEL_STREAM_A) == 0)
        {
            Debug_ReportError("MetaData hot stream is peresent, but ngen is not supported.");
            // Ignore the stream
        }
        // Pick off the next stream if there is one.
        pStream = pNext;
        cbStreamBuffer = (ULONG)((LPBYTE)pData + cbData - (LPBYTE)pNext);
    }

    // If the meta data wasn't found, we can't handle this file.
    if (!bFoundMd)
    {
        Debug_ReportError("MetaData compressed model stream #~ not found.");
        IfFailGo(CLDB_E_FILE_CORRUPT);
    }
    else
    {   // Validate sensible heaps.
        IfFailGo(m_MiniMd.PostInit(0));
    }

    // Save off the location.
    m_pvMd = pData;
    m_cbMd = cbData;

ErrExit:
    return hr;
} // CLiteWeightStgdb<MiniMd>::InitOnMem
