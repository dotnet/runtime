// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgPoolReadOnly.cpp
//

//
// Read only pools are used to reduce the amount of data actually required in the database.
//
//*****************************************************************************
#include "stdafx.h"                     // Standard include.
#include <stgpool.h>                    // Our interface definitions.
#include "metadatatracker.h"
//
//
// StgPoolReadOnly
//
//

#if METADATATRACKER_ENABLED
MetaDataTracker  *MetaDataTracker::m_MDTrackers = NULL;
BOOL MetaDataTracker::s_bEnabled = FALSE;

void        (*MetaDataTracker::s_IBCLogMetaDataAccess)(const void *addr) = NULL;
void        (*MetaDataTracker::s_IBCLogMetaDataSearch)(const void *result) = NULL;

#endif // METADATATRACKER_ENABLED

const BYTE StgPoolSeg::m_zeros[64] = {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                                      0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                                      0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                                      0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};


//*****************************************************************************
// Free any memory we allocated.
//*****************************************************************************
StgPoolReadOnly::~StgPoolReadOnly()
{
    LIMITED_METHOD_CONTRACT;
}


//*****************************************************************************
// Init the pool from existing data.
//*****************************************************************************
HRESULT StgPoolReadOnly::InitOnMemReadOnly(// Return code.
        void        *pData,             // Predefined data.
        ULONG       iSize)              // Size of data.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END

    // Make sure we aren't stomping anything and are properly initialized.
    _ASSERTE(m_pSegData == m_zeros);

    // Create case requires no further action.
    if (pData == NULL)
        return E_INVALIDARG;

    // Keep m_zeros data pointer if there's no content of the pool
    if (iSize != 0)
    {
        m_pSegData = reinterpret_cast<BYTE*>(pData);
    }
    m_cbSegSize = iSize;
    m_cbSegNext = iSize;
    return S_OK;
}

//*****************************************************************************
// Prepare to shut down or reinitialize.
//*****************************************************************************
void StgPoolReadOnly::Uninit()
{
    LIMITED_METHOD_CONTRACT;

    m_pSegData = (BYTE*)m_zeros;
    m_pNextSeg = 0;
}


//*****************************************************************************
// Convert a string to UNICODE into the caller's buffer.
//*****************************************************************************
HRESULT StgPoolReadOnly::GetStringW(        // Return code.
    ULONG       iOffset,                    // Offset of string in pool.
    _Out_writes_(cchBuffer) LPWSTR szOut,   // Output buffer for string.
    int         cchBuffer)                  // Size of output buffer.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

    HRESULT hr;
    LPCSTR  pString;                // The string in UTF8.
    int     iChars;

    IfFailRet(GetString(iOffset, &pString));
    iChars = ::WszMultiByteToWideChar(CP_UTF8, 0, pString, -1, szOut, cchBuffer);
    if (iChars == 0)
        return (BadError(HRESULT_FROM_NT(GetLastError())));
    return S_OK;
}

//*****************************************************************************
// Return a pointer to a null terminated blob given an offset previously
// handed out by Addblob or Findblob.
//*****************************************************************************
HRESULT
StgPoolReadOnly::GetBlob(
    UINT32              nOffset,    // Offset of blob in pool.
    MetaData::DataBlob *pData)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    HRESULT hr;
    UINT32  cbBlobContentSize;

    // This should not be a necessary special case.  The zero byte at the
    //  start of the pool will code for a length of zero.  We will return
    //  a pointer to the next length byte, but the caller should notice that
    //  the size is zero, and should not look at any bytes.
    // [SL] Yes, but we don't need all further computations and checks if iOffset==0

    if (nOffset == 0)
    {
        pData->Clear();
        return S_OK;
    }

    // Is the offset within this heap?
    if (!IsValidOffset(nOffset))
    {
        Debug_ReportError("Invalid blob offset.");
        IfFailGo(CLDB_E_INDEX_NOTFOUND);
    }

    IfFailGo(GetDataReadOnly(nOffset, pData));
    if (!pData->GetCompressedU(&cbBlobContentSize))
    {
        Debug_ReportError("Invalid blob - size compression.");
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }
    if (!pData->TruncateToExactSize(cbBlobContentSize))
    {
        Debug_ReportError("Invalid blob - reaches behind the end of data block.");
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }

    return S_OK;
ErrExit:
    pData->Clear();
    return hr;
} // StgPoolReadOnly::GetBlob

//*****************************************************************************
// code:StgPoolReadOnly::GetBlob specialization with inlined check for valid offsets to avoid redundant code:StgPoolReadOnly::GetDataReadOnly calls.
// code:StgPoolReadOnly::GetDataReadOnly is not cheap because of it performs binary lookup in hot metadata.
//*****************************************************************************
HRESULT
StgBlobPoolReadOnly::GetBlob(
    UINT32              nOffset,    // Offset of blob in pool.
    MetaData::DataBlob *pData)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    HRESULT hr;
    UINT32  cbBlobContentSize;

    // This should not be a necessary special case.  The zero byte at the
    //  start of the pool will code for a length of zero.  We will return
    //  a pointer to the next length byte, but the caller should notice that
    //  the size is zero, and should not look at any bytes.
    // [SL] Yes, but we don't need all further computations and checks if iOffset==0

    if (nOffset == 0)
    {
        pData->Clear();
        return S_OK;
    }

    if (m_pSegData == m_zeros)
    {
        Debug_ReportError("Invalid blob offset.");
        IfFailGo(CLDB_E_INDEX_NOTFOUND);
    }

    IfFailGo(GetDataReadOnly(nOffset, pData));
    if (!pData->GetCompressedU(&cbBlobContentSize))
    {
        Debug_ReportError("Invalid blob - size compression.");
        IfFailGo(CLDB_E_INDEX_NOTFOUND);
    }
    if (!pData->TruncateToExactSize(cbBlobContentSize))
    {
        Debug_ReportError("Invalid blob - reaches behind the end of data block.");
        IfFailGo(CLDB_E_INDEX_NOTFOUND);
    }

    return S_OK;
ErrExit:
    pData->Clear();
    return hr;
} // StgBlobPoolReadOnly::GetBlob
