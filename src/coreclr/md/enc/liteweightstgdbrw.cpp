// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// LiteWeightStgdb.cpp
//
// This contains definition of class CLiteWeightStgDB. This is light weight
// read-only implementation for accessing compressed meta data format.
//
//*****************************************************************************
#include "stdafx.h"                     // Precompiled header.

#include "metamodelrw.h"
#include "liteweightstgdb.h"

// include stgdatabase.h for GUID_POOL_STREAM definition
// #include "stgdatabase.h"

// include StgTiggerStorage for TiggerStorage definition
#include "stgtiggerstorage.h"
#include "stgio.h"
#include "pedecoder.h"

#include <log.h>

//*****************************************************************************
// Checks the given storage object to see if it is an NT PE image.
//*****************************************************************************
int _IsNTPEImage(                       // true if file is NT PE image.
    StgIO       *pStgIO)                // Storage object.
{
    LONG        lfanew=0;               // Offset in DOS header to NT header.
    ULONG       lSignature=0;           // For NT header signature.
    HRESULT     hr;

    // Read DOS header to find the NT header offset.
    if (FAILED(hr = pStgIO->Seek(60, FILE_BEGIN)) ||
        FAILED(hr = pStgIO->Read(&lfanew, sizeof(LONG), 0)))
    {
        return (false);
    }

    // Seek to the NT header and read the signature.
    if (FAILED(hr = pStgIO->Seek(VAL32(lfanew), FILE_BEGIN)) ||
        FAILED(hr = pStgIO->Read(&lSignature, sizeof(ULONG), 0)) ||
        FAILED(hr = pStgIO->Seek(0, FILE_BEGIN)))
    {
        return (false);
    }

    // If the signature is a match, then we have a PE format.
    if (lSignature == VAL32(IMAGE_NT_SIGNATURE))
        return (true);
    else
        return (false);
}

HRESULT _GetFileTypeForPath(StgIO *pStgIO, FILETYPE *piType)
{
    ULONG       lSignature=0;
    HRESULT     hr;

    // Assume native file.
    *piType = FILETYPE_CLB;

    // Need to read signature to see what type it is.
    if (!(pStgIO->GetFlags() & DBPROP_TMODEF_CREATE))
    {
        if (FAILED(hr = pStgIO->Read(&lSignature, sizeof(ULONG), 0)) ||
            FAILED(hr = pStgIO->Seek(0, FILE_BEGIN)))
        {
            return (hr);
        }
        lSignature = VAL32(lSignature);
        if (lSignature == STORAGE_MAGIC_SIG)
            *piType = FILETYPE_CLB;
        else if ((WORD) lSignature ==IMAGE_DOS_SIGNATURE && _IsNTPEImage(pStgIO))
            *piType = FILETYPE_NTPE;
        else
            return CLDB_E_FILE_CORRUPT;
    }
    return S_OK;
}

//*****************************************************************************
// Prepare to go away.
//*****************************************************************************
CLiteWeightStgdbRW::~CLiteWeightStgdbRW()
{
    // Free up this stacks reference on the I/O object.
    if (m_pStgIO != NULL)
    {
        m_pStgIO->Release();
        m_pStgIO = NULL;
    }

    if (m_pStreamList != NULL)
    {
        delete m_pStreamList;
    }

    if (m_wszFileName != NULL)
    {
        delete [] m_wszFileName;
    }
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    if (m_pPdbHeap != NULL)
    {
        delete m_pPdbHeap;
        m_pPdbHeap = NULL;
    }
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
}

//*****************************************************************************
// Open an in-memory metadata section for read
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::InitOnMem(
    ULONG       cbData,                 // count of bytes in pData
    LPCVOID     pData,                  // points to meta data section in memory
    int         bReadOnly)              // If true, read-only.
{
    StgIO       *pStgIO = NULL;         // For file i/o.
    HRESULT     hr = NOERROR;

    if ((pStgIO = new (nothrow) StgIO) == 0)
        IfFailGo( E_OUTOFMEMORY);

    // Open the storage based on the pbData and cbData
    IfFailGo( pStgIO->Open(
        NULL,   // filename
        STGIO_READ,
        pData,
        cbData,
        NULL,   // IStream*
        NULL)   // LPSecurityAttributes
         );

    IfFailGo( InitFileForRead(pStgIO, bReadOnly) );

ErrExit:
    if (SUCCEEDED(hr))
    {
        m_pStgIO = pStgIO;
    }
    else
    {
        if (pStgIO)
            pStgIO->Release();
    }
    return hr;
} // CLiteWeightStgdbRW::InitOnMem


//*****************************************************************************
// Given an StgIO, opens compressed streams and do proper initialization.
// This is a helper for other Init functions.
//*****************************************************************************
__checkReturn
HRESULT
CLiteWeightStgdbRW::InitFileForRead(
    StgIO * pStgIO,     // For file i/o.
    int     bReadOnly)  // If read-only open.
{
    TiggerStorage * pStorage = NULL;
    void          * pvData;
    ULONG           cbData;
    HRESULT         hr = NOERROR;

    // Allocate a new storage object which has IStorage on it.
    pStorage = new (nothrow) TiggerStorage();
    IfNullGo(pStorage);

    // Init the storage object on the backing storage.
    OptionValue ov;
    IfFailGo(m_MiniMd.GetOption(&ov));
    IfFailGo(pStorage->Init(pStgIO, ov.m_RuntimeVersion));

    // Save pointers to header structure for version string.
    _ASSERTE((m_pvMd == NULL) && (m_cbMd == 0));
    IfFailGo(pStorage->GetHeaderPointer(&m_pvMd, &m_cbMd));

    // Check to see if this is a minimal metadata
    if (SUCCEEDED(pStorage->OpenStream(MINIMAL_MD_STREAM, &cbData, &pvData)))
    {
        m_MiniMd.m_fMinimalDelta = TRUE;
    }

    // Load the string pool.
    if (SUCCEEDED(hr = pStorage->OpenStream(STRING_POOL_STREAM, &cbData, &pvData)))
    {
        // String pool has to end with a null-terminator, therefore we don't have to check string pool
        // content on access.
        // Shrink size of the pool to the last null-terminator found.
        while (cbData != 0)
        {
            if (((LPBYTE)pvData)[cbData - 1] == 0)
            {   // We have found last null terminator
                break;
            }
            // Shrink size of the pool
            cbData--;
            Debug_ReportError("String heap/pool does not end with null-terminator ... shrinking the heap.");
        }
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolStrings, pvData, cbData, bReadOnly));
    }
    else
    {
        if (hr != STG_E_FILENOTFOUND)
        {
            IfFailGo(hr);
        }
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolStrings, NULL, 0, bReadOnly));
    }

    // Load the user string blob pool.
    if (SUCCEEDED(hr = pStorage->OpenStream(US_BLOB_POOL_STREAM, &cbData, &pvData)))
    {
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolUSBlobs, pvData, cbData, bReadOnly));
    }
    else
    {
        if (hr != STG_E_FILENOTFOUND)
        {
            IfFailGo(hr);
        }
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolUSBlobs, NULL, 0, bReadOnly));
    }

    // Load the guid pool.
    if (SUCCEEDED(hr = pStorage->OpenStream(GUID_POOL_STREAM, &cbData, &pvData)))
    {
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolGuids, pvData, cbData, bReadOnly));
    }
    else
    {
        if (hr != STG_E_FILENOTFOUND)
        {
            IfFailGo(hr);
        }
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolGuids, NULL, 0, bReadOnly));
    }

    // Load the blob pool.
    if (SUCCEEDED(hr = pStorage->OpenStream(BLOB_POOL_STREAM, &cbData, &pvData)))
    {
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolBlobs, pvData, cbData, bReadOnly));
    }
    else
    {
        if (hr != STG_E_FILENOTFOUND)
        {
            IfFailGo(hr);
        }
        IfFailGo(m_MiniMd.InitPoolOnMem(MDPoolBlobs, NULL, 0, bReadOnly));
    }

    // Open the metadata.
    hr = pStorage->OpenStream(COMPRESSED_MODEL_STREAM, &cbData, &pvData);
    if (hr == STG_E_FILENOTFOUND)
    {
        IfFailGo(pStorage->OpenStream(ENC_MODEL_STREAM, &cbData, &pvData));
    }
    IfFailGo(m_MiniMd.InitOnMem(pvData, cbData, bReadOnly));
    IfFailGo(m_MiniMd.PostInit(0));

ErrExit:
    if (pStorage != NULL)
    {
        delete pStorage;
    }
    return hr;
} // CLiteWeightStgdbRW::InitFileForRead

//*****************************************************************************
// Open a metadata section for read
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::OpenForRead(
    LPCWSTR     szDatabase,             // Name of database.
    void        *pbData,                // Data to open on top of, 0 default.
    ULONG       cbData,                 // How big is the data.
    DWORD       dwFlags)                // Flags for the open.
{
    LPCWSTR     pNoFile=W("");            // Constant for empty file name.
    StgIO       *pStgIO = NULL;         // For file i/o.
    HRESULT     hr;

    m_pImage = NULL;
    m_dwImageSize = 0;
    m_eFileType = FILETYPE_UNKNOWN;
    // szDatabase, and pbData are mutually exclusive.  Only one may be
    // non-NULL.  Having both NULL means empty stream creation.
    //
    _ASSERTE(!(szDatabase && (pbData)));
    _ASSERTE(!(pbData && (szDatabase)));

    // Open on memory needs there to be something to work with.
    if (pbData && cbData == 0)
        IfFailGo(CLDB_E_NO_DATA);

    // Make sure we have a path to work with.
    if (!szDatabase)
        szDatabase = pNoFile;

    // Sanity check the name lentgh.
    if (!IsValidFileNameLength(szDatabase))
    {
        IfFailGo(E_INVALIDARG);
    }

    // If we have storage to work with, init it and get type.
    if (*szDatabase || pbData)
    {
        // Allocate a storage instance to use for i/o.
        if ((pStgIO = new (nothrow) StgIO) == 0)
            IfFailGo( E_OUTOFMEMORY );

        DBPROPMODE dmOpenFlags = DBPROP_TMODEF_READ;

        // If we're taking ownership of this memory.....
        if (IsOfTakeOwnership(dwFlags))
        {
            dmOpenFlags = (DBPROPMODE)(dmOpenFlags | DBPROP_TMODEF_SHAREDMEM);
        }
#ifdef FEATURE_METADATA_LOAD_TRUSTED_IMAGES
        if (IsOfTrustedImage(dwFlags))
            dmOpenFlags = (DBPROPMODE)(dmOpenFlags | DBPROP_TMODEF_TRYLOADLIBRARY);
#endif

        // Open the storage so we can read the signature if there is already data.
        IfFailGo( pStgIO->Open(szDatabase,
                               dmOpenFlags,
                               pbData,
                               cbData,
                               0, // IStream*
                               NULL) );

        // Determine the type of file we are working with.
        IfFailGo( _GetFileTypeForPath(pStgIO, &m_eFileType) );
    }

    // Check for default type.
    if (m_eFileType == FILETYPE_CLB)
    {
        // If user wanted us to make a local copy of the data, do that now.
        if (IsOfCopyMemory(dwFlags))
            IfFailGo(pStgIO->LoadFileToMemory());

        // Try the native .clb file.
        IfFailGo( InitFileForRead(pStgIO, IsOfRead(dwFlags)) );
    }
    // PE/COFF executable/object format.  This requires us to find the .clb
    // inside the binary before doing the Init.
    else if (m_eFileType == FILETYPE_NTPE)
    {
        //<TODO>@FUTURE: Ideally the FindImageMetaData function
        //@FUTURE:  would take the pStgIO and map only the part of the file where
        //@FUTURE:  our data lives, leaving the rest alone.  This would be smaller
        //@FUTURE:  working set for us.</TODO>
        void        *ptr;
        ULONG       cbSize;

        // Map the entire binary for the FindImageMetaData function.
        IfFailGo( pStgIO->MapFileToMem(ptr, &cbSize) );

        // Find the .clb inside of the content.
        m_pImage = ptr;
        m_dwImageSize = cbSize;
        hr = FindImageMetaData(ptr,
                               cbSize,
                               pStgIO->GetMemoryMappedType() == MTYPE_IMAGE,
                               &ptr,
                               &cbSize);

        // Was the metadata found inside the PE file?
        IfFailGo(hr);

        // Metadata was found inside the file.
        // Now reset the base of the stg object so that all memory accesses
        // are relative to the .clb content.
        //
        IfFailGo( pStgIO->SetBaseRange(ptr, cbSize) );

        // If user wanted us to make a local copy of the data, do that now.
        if (IsOfCopyMemory(dwFlags))
        {
            // Cache the PEKind, Machine.
            GetPEKind(pStgIO->GetMemoryMappedType(), NULL, NULL);
            // Copy the file into memory; releases the file.
            IfFailGo(pStgIO->LoadFileToMemory());
            // No longer have the image.
            m_pImage = NULL;
            m_dwImageSize = 0;
        }

        // Defer to the normal lookup.
        IfFailGo( InitFileForRead(pStgIO, IsOfRead(dwFlags)) );
    }
    // This spells trouble, we need to handle all types we might find.
    else
    {
        _ASSERTE(!"Unknown file type.");
        IfFailGo( E_FAIL );
    }

    // Save off everything.
    IfFailGo(SetFileName(szDatabase));

    // If this was a file...
    if (pbData == NULL)
    {
        WIN32_FILE_ATTRIBUTE_DATA faData;
        if (!WszGetFileAttributesEx(szDatabase, GetFileExInfoStandard, &faData))
            IfFailGo(E_FAIL);
        m_dwDatabaseLFS = faData.nFileSizeLow;
        m_dwDatabaseLFT = faData.ftLastWriteTime.dwLowDateTime;
    }

ErrExit:
    if (SUCCEEDED(hr))
    {
        m_pStgIO = pStgIO;
    }
    else
    {
        if (pStgIO != NULL)
            pStgIO->Release();
    }
    return hr;
}

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
// Open a metadata section for read/write
__checkReturn
HRESULT CLiteWeightStgdbRW::OpenForRead(
    IMDCustomDataSource *pDataSource,   // data to open on top of
    DWORD       dwFlags)                // Flags for the open.
{
    LPCWSTR     pNoFile = W("");            // Constant for empty file name.
    StgIO       *pStgIO = NULL;         // For file i/o.
    HRESULT     hr;

    m_pImage = NULL;
    m_dwImageSize = 0;
    m_eFileType = FILETYPE_UNKNOWN;

    IfFailGo(m_MiniMd.InitOnCustomDataSource(pDataSource));
    IfFailGo(m_MiniMd.PostInit(0));

    // Save off everything.
    IfFailGo(SetFileName(pNoFile));

ErrExit:
    return hr;
}
#endif

// Read/Write versions.
//*****************************************************************************
// Init the Stgdb and its subcomponents.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::InitNew()
{
    InitializeLogging();
    LOG((LF_METADATA, LL_INFO10, "Metadata logging enabled\n"));
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    m_pPdbHeap = new PdbHeap();
#endif
    //<TODO>@FUTURE: should probably init the pools here instead of in the MiniMd.</TODO>
    return m_MiniMd.InitNew();
}

//*****************************************************************************
// Determine what the size of the saved data will be.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::GetSaveSize(// S_OK or error.
    CorSaveSize               fSave,                // Quick or accurate?
    UINT32                   *pcbSaveSize,          // Put the size here.
    MetaDataReorderingOptions reorderingOptions)
{
    HRESULT hr = S_OK;              // A result.
    UINT32  cbTotal = 0;            // The total size.
    UINT32  cbSize = 0;             // Size of a component.

    m_cbSaveSize = 0;

    // Allocate stream list if not already done.
    if (m_pStreamList == NULL)
    {
        IfNullGo(m_pStreamList = new (nothrow) STORAGESTREAMLST);
    }
    else
    {
        m_pStreamList->Clear();
    }

    // Make sure the user string pool is not empty. An empty user string pool causes
    // problems with edit and continue

    if (m_MiniMd.m_UserStringHeap.GetUnalignedSize() <= 1)
    {
        if (!IsENCDelta(m_MiniMd.m_OptionValue.m_UpdateMode) &&
            !m_MiniMd.IsMinimalDelta())
        {
            BYTE   rgData[] = {' ', 0, 0};
            UINT32 nIndex_Ignore;
            IfFailGo(m_MiniMd.PutUserString(
                MetaData::DataBlob(rgData, sizeof(rgData)),
                &nIndex_Ignore));
        }
    }

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    // [IMPORTANT]:
    // Apparently, string pool must exist in the portable PDB metadata in order to be recognized
    // by the VS debugger. For this purpose, we are adding a dummy " " value in cases when
    // the string pool is empty.
    // Interestingly enough, similar is done for user string pool (check above #549).
    // TODO: do this in a more clever way, and check if/why/when this is necessary
    if (m_MiniMd.m_StringHeap.GetUnalignedSize() <= 1)
    {
        if (!IsENCDelta(m_MiniMd.m_OptionValue.m_UpdateMode) &&
            !m_MiniMd.IsMinimalDelta())
        {
            UINT32 nIndex_Ignore;
            IfFailGo(m_MiniMd.m_StringHeap.AddString(" ", &nIndex_Ignore));
        }
    }
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

    // If we're saving a delta metadata, figure out how much space it will take to
    // save the minimal metadata stream (used only to identify that we have a delta
    // metadata... nothing should be in that stream.
    if ((m_MiniMd.m_OptionValue.m_UpdateMode & MDUpdateMask) == MDUpdateDelta)
    {
        IfFailGo(AddStreamToList(0, MINIMAL_MD_STREAM));
        // Ask the storage system to add stream fixed overhead.
        IfFailGo(TiggerStorage::GetStreamSaveSize(MINIMAL_MD_STREAM, 0, &cbSize));
        cbTotal += cbSize;
    }

    if (reorderingOptions & ReArrangeStringPool)
    {
        // get string pool save size
        IfFailGo(GetPoolSaveSize(STRING_POOL_STREAM, MDPoolStrings, &cbSize));
        cbTotal += cbSize;
    }

    // Query the MiniMd for its size.
    IfFailGo(GetTablesSaveSize(fSave, &cbSize, reorderingOptions));
    cbTotal += cbSize;

    // Get the pools' sizes.
    if( !(reorderingOptions & ReArrangeStringPool) )
    {
        IfFailGo(GetPoolSaveSize(STRING_POOL_STREAM, MDPoolStrings, &cbSize));
        cbTotal += cbSize;
    }
    IfFailGo(GetPoolSaveSize(US_BLOB_POOL_STREAM, MDPoolUSBlobs, &cbSize));
    cbTotal += cbSize;
    IfFailGo(GetPoolSaveSize(GUID_POOL_STREAM, MDPoolGuids, &cbSize));
    cbTotal += cbSize;
    IfFailGo(GetPoolSaveSize(BLOB_POOL_STREAM, MDPoolBlobs, &cbSize));
    cbTotal += cbSize;
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    IfFailGo(GetPoolSaveSize(PDB_STREAM, NULL, &cbSize));
    cbTotal += cbSize;
#endif

    // Finally, ask the storage system to add fixed overhead it needs for the
    // file format.  The overhead of each stream has already be calculated as
    // part of GetStreamSaveSize.  What's left is the signature and header
    // fixed size overhead.
    IfFailGo(TiggerStorage::GetStorageSaveSize((ULONG *)&cbTotal, 0, m_MiniMd.m_OptionValue.m_RuntimeVersion));

    // Log the size info.
    LOG((LF_METADATA, LL_INFO10, "Metadata: GetSaveSize total is %d.\n", cbTotal));

    // The list of streams that will be saved are now in the stream save list.
    // Next step is to walk that list and fill out the correct offsets.  This is
    // done here so that the data can be streamed without fixing up the header.
    TiggerStorage::CalcOffsets(m_pStreamList, 0, m_MiniMd.m_OptionValue.m_RuntimeVersion);

    if (pcbSaveSize != NULL)
    {
        *pcbSaveSize = cbTotal;
    }

    // Don't cache the value for the EnC case
    if (!IsENCDelta(m_MiniMd.m_OptionValue.m_UpdateMode))
        m_cbSaveSize = cbTotal;

ErrExit:
    return hr;
} // CLiteWeightStgdbRW::GetSaveSize

//*****************************************************************************
// Get the save size of one of the pools.  Also adds the pool's stream to
//  the list of streams to be saved.
//*****************************************************************************
__checkReturn
HRESULT
CLiteWeightStgdbRW::GetPoolSaveSize(
    LPCWSTR szHeap,         // Name of the heap stream.
    int     iPool,          // The pool of which to get size.
    UINT32 *pcbSaveSize)    // Add pool data to this value.
{
    UINT32  cbSize = 0;     // Size of pool data.
    UINT32  cbStream;       // Size of just the stream.
    HRESULT hr = S_OK;

    *pcbSaveSize = 0;

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    // Treat PDB stream differently since we are not using StgPools
    if (wcscmp(PDB_STREAM, szHeap) == 0)
    {
        if (m_pPdbHeap && !m_pPdbHeap->IsEmpty())
        {
            cbSize = m_pPdbHeap->GetSize();
            IfFailGo(AddStreamToList(cbSize, szHeap));
            IfFailGo(TiggerStorage::GetStreamSaveSize(szHeap, cbSize, &cbSize));
            *pcbSaveSize = cbSize;
        }
        else
        {
            goto ErrExit;
        }
    }
    else
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
    {
        // If there is no data, then don't bother.
        if (m_MiniMd.IsPoolEmpty(iPool))
            return (S_OK);

        // Ask the pool to size its data.
        IfFailGo(m_MiniMd.GetPoolSaveSize(iPool, &cbSize));
        cbStream = cbSize;

        // Add this item to the save list.
        IfFailGo(AddStreamToList(cbSize, szHeap));


        // Ask the storage system to add stream fixed overhead.
        IfFailGo(TiggerStorage::GetStreamSaveSize(szHeap, cbSize, &cbSize));

        // Log the size info.
        LOG((LF_METADATA, LL_INFO10, "Metadata: GetSaveSize for %ls: %d data, %d total.\n",
            szHeap, cbStream, cbSize));

        // Give the size of the pool to the caller's total.
        *pcbSaveSize = cbSize;
    }

ErrExit:
    return hr;
}

//*****************************************************************************
// Get the save size of the metadata tables.  Also adds the tables stream to
//  the list of streams to be saved.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::GetTablesSaveSize(
    CorSaveSize               fSave,
    UINT32                   *pcbSaveSize,
    MetaDataReorderingOptions reorderingOptions)
{
    UINT32  cbSize = 0;             // Size of pool data.
    UINT32  cbHotSize = 0;          // Size of pool data.
    UINT32  cbStream;               // Size of just the stream.
    DWORD   bCompressed;            // Will the stream be compressed data?
    LPCWSTR szName;                 // What will the name of the pool be?
    HRESULT hr;

    *pcbSaveSize = 0;

    // Ask the metadata to size its data.
    IfFailGo(m_MiniMd.GetSaveSize(fSave, &cbSize, &bCompressed));
    cbStream = cbSize;
    m_bSaveCompressed = bCompressed;
    szName = m_bSaveCompressed ? COMPRESSED_MODEL_STREAM : ENC_MODEL_STREAM;

    // Add this item to the save list.
    IfFailGo(AddStreamToList(cbSize, szName));

    // Ask the storage system to add stream fixed overhead.
    IfFailGo(TiggerStorage::GetStreamSaveSize(szName, cbSize, &cbSize));

    // Log the size info.
    LOG((LF_METADATA, LL_INFO10, "Metadata: GetSaveSize for %ls: %d data, %d total.\n",
        szName, cbStream, cbSize));

    // Give the size of the pool to the caller's total.
    *pcbSaveSize = cbHotSize + cbSize;

ErrExit:
    return hr;
} // CLiteWeightStgdbRW::GetTablesSaveSize

//*****************************************************************************
// Add a stream, and its size, to the list of streams to be saved.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::AddStreamToList(
    UINT32  cbSize,
    LPCWSTR szName)
{
    HRESULT     hr = S_OK;
    PSTORAGESTREAM pItem;               // New item to allocate & fill.

    // Add a new item to the end of the list.
    IfNullGo(pItem = m_pStreamList->Append());

    // Fill out the data.
    pItem->SetOffset(0);
    pItem->SetSize((ULONG)cbSize);
    pItem->SetName(szName);

ErrExit:
    return hr;
}

//*****************************************************************************
// Save the data to a stream.  A TiggerStorage sub-allocates streams within
//   the stream.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::SaveToStream(
    IStream                  *pIStream,
    MetaDataReorderingOptions reorderingOptions)
{
    HRESULT     hr = S_OK;              // A result.
    StgIO       *pStgIO = 0;
    TiggerStorage *pStorage = 0;

    // Allocate a storage subsystem and backing store.
    IfNullGo(pStgIO = new (nothrow) StgIO);
    IfNullGo(pStorage = new (nothrow) TiggerStorage);

    // Open around this stream for write.
    IfFailGo(pStgIO->Open(W(""),
        DBPROP_TMODEF_DFTWRITEMASK,
        0, 0,                           // pbData, cbData
        pIStream,
        0));                            // LPSecurityAttributes
    OptionValue ov;
    IfFailGo(m_MiniMd.GetOption(&ov));
    IfFailGo(pStorage->Init(pStgIO, ov.m_RuntimeVersion));

    // Save worker will do tables, pools.
    IfFailGo(SaveToStorage(pStorage, reorderingOptions));

ErrExit:
    if (pStgIO != NULL)
        pStgIO->Release();
    if (pStorage != NULL)
        delete pStorage;
    return hr;
} // CLiteWeightStgdbRW::SaveToStream

//*****************************************************************************
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::SaveToStorage(
    TiggerStorage            *pStorage,
    MetaDataReorderingOptions reorderingOptions)
{
    HRESULT  hr;                     // A result.
    LPCWSTR  szName;                 // Name of the tables stream.
    IStream *pIStreamTbl = 0;
    UINT32   cb;
    UINT32   cbSaveSize = m_cbSaveSize;

    // Must call GetSaveSize to cache the streams up front.
    // Don't trust cached values in the delta case... if there was a previous call to get
    // a non-delta size, it will be incorrect.
    if ((m_cbSaveSize == 0) || IsENCDelta(m_MiniMd.m_OptionValue.m_UpdateMode))
    {
        IfFailGo(GetSaveSize(cssAccurate, &cbSaveSize));
    }

    // Save the header of the data file.
    IfFailGo(pStorage->WriteHeader(m_pStreamList, 0, NULL));

    // If this is a minimal delta, write a stream marker
    if (IsENCDelta(m_MiniMd.m_OptionValue.m_UpdateMode))
    {
        IfFailGo(pStorage->CreateStream(MINIMAL_MD_STREAM,
            STGM_DIRECT | STGM_READWRITE | STGM_SHARE_EXCLUSIVE,
            0, 0, &pIStreamTbl));
        pIStreamTbl->Release();
        pIStreamTbl = 0;
    }

    if (reorderingOptions & ReArrangeStringPool)
    {
        // Save the string pool before the tables when we do not have the string pool cache
        IfFailGo(SavePool(STRING_POOL_STREAM, pStorage, MDPoolStrings));
    }

    // Create a stream and save the tables.
    szName = m_bSaveCompressed ? COMPRESSED_MODEL_STREAM : ENC_MODEL_STREAM;
    IfFailGo(pStorage->CreateStream(szName,
            STGM_DIRECT | STGM_READWRITE | STGM_SHARE_EXCLUSIVE,
            0, 0, &pIStreamTbl));
    IfFailGo(m_MiniMd.SaveTablesToStream(pIStreamTbl, NoReordering));
    pIStreamTbl->Release();
    pIStreamTbl = 0;

    // Save the pools.
    if (!(reorderingOptions & ReArrangeStringPool))
    {
        // string pool must be saved after the tables when we have the string pool cache
        IfFailGo(SavePool(STRING_POOL_STREAM, pStorage, MDPoolStrings));
    }
    IfFailGo(SavePool(US_BLOB_POOL_STREAM, pStorage, MDPoolUSBlobs));
    IfFailGo(SavePool(GUID_POOL_STREAM, pStorage, MDPoolGuids));
    IfFailGo(SavePool(BLOB_POOL_STREAM, pStorage, MDPoolBlobs));
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    IfFailGo(SavePool(PDB_STREAM, pStorage, NULL));
#endif

    // Write the header to disk.
    OptionValue ov;
    IfFailGo(m_MiniMd.GetOption(&ov));

    IfFailGo(pStorage->WriteFinished(m_pStreamList, (ULONG *)&cb, IsENCDelta(ov.m_UpdateMode)));

    _ASSERTE(cbSaveSize == cb);

    // Let the Storage release some memory.
    pStorage->ResetBackingStore();

    IfFailGo(m_MiniMd.SaveDone());

ErrExit:
    if (pIStreamTbl != NULL)
        pIStreamTbl->Release();
    delete m_pStreamList;
    m_pStreamList = 0;
    m_cbSaveSize = 0;
    return hr;
} // CLiteWeightStgdbRW::SaveToStorage

//*****************************************************************************
// Save a pool of data out to a stream.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::SavePool(   // Return code.
    LPCWSTR     szName,                 // Name of stream on disk.
    TiggerStorage *pStorage,            // The storage to put data in.
    int         iPool)                  // The pool to save.
{
    IStream     *pIStream=0;            // For writing.
    HRESULT     hr = S_OK;

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    // Treat PDB stream differently since we are not using StgPools
    if (wcscmp(PDB_STREAM, szName) == 0)
    {
        if (m_pPdbHeap && !m_pPdbHeap->IsEmpty())
        {
            IfFailGo(pStorage->CreateStream(szName,
                STGM_DIRECT | STGM_READWRITE | STGM_SHARE_EXCLUSIVE,
                0, 0, &pIStream));
            IfFailGo(m_pPdbHeap->SaveToStream(pIStream));
        }
        else
        {
            goto ErrExit;
        }
    }
    else
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB
    {
        // If there is no data, then don't bother.
        if (m_MiniMd.IsPoolEmpty(iPool))
            return (S_OK);

        // Create the new stream to hold this table and save it.
        IfFailGo(pStorage->CreateStream(szName,
            STGM_DIRECT | STGM_READWRITE | STGM_SHARE_EXCLUSIVE,
            0, 0, &pIStream));
        IfFailGo(m_MiniMd.SavePoolToStream(iPool, pIStream));
    }

ErrExit:
    if (pIStream)
        pIStream->Release();
    return hr;
} // CLiteWeightStgdbRW::SavePool


//*****************************************************************************
// Save the metadata to a file.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::Save(
    LPCWSTR szDatabase,     // Name of file to which to save.
    DWORD   dwSaveFlags)    // Flags for the save.
{
    TiggerStorage * pStorage = NULL;    // IStorage object.
    StgIO *         pStgIO = NULL;      // Backing storage.
    HRESULT         hr = S_OK;

    if (m_wszFileName == NULL)
    {
        if (szDatabase == NULL)
        {
            // Make sure that a NULL is not passed in the first time around.
            _ASSERTE(!"Not allowed to pass a NULL for filename on the first call to Save.");
            return E_INVALIDARG;
        }
        else
        {
            // Save the file name.
            IfFailGo(SetFileName(szDatabase));
        }
    }
    else if ((szDatabase != NULL) && (SString::_wcsicmp(szDatabase, m_wszFileName) != 0))
    {
        // Save the file name.
        IfFailGo(SetFileName(szDatabase));
    }

    // Sanity check the name.
    if (!IsValidFileNameLength(m_wszFileName))
    {
        IfFailGo(E_INVALIDARG);
    }

    m_eFileType = FILETYPE_CLB;

    // Allocate a new storage object.
    IfNullGo(pStgIO = new (nothrow) StgIO);

    // Create the output file.
    IfFailGo(pStgIO->Open(m_wszFileName,
        DBPROP_TMODEF_DFTWRITEMASK,
        0,0,                // pbData, cbData
        0,                  // IStream*
        0));                // LPSecurityAttributes

    // Allocate an IStorage object to use.
    IfNullGo(pStorage = new (nothrow) TiggerStorage);

    // Init the storage object on the i/o system.
    OptionValue ov;
    IfFailGo(m_MiniMd.GetOption(&ov));
    IfFailGo(pStorage->Init(pStgIO, ov.m_RuntimeVersion));

    // Save the data.
    IfFailGo(SaveToStorage(pStorage));

ErrExit:
    if (pStgIO != NULL)
        pStgIO->Release();
    if (pStorage != NULL)
        delete pStorage;
    return hr;
} // CLiteWeightStgdbRW::Save

//*****************************************************************************
// Pull the PEKind and Machine out of PE headers -- if we have PE headers.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::GetPEKind(  // S_OK or error.
    MAPPINGTYPE mtMapping,              // The type of mapping the image has
    DWORD       *pdwPEKind,             // [OUT] The kind of PE (0 - not a PE)
    DWORD       *pdwMachine)            // [OUT] Machine as defined in NT header
{
    HRESULT     hr = NOERROR;
    DWORD       dwPEKind=0;             // Working copy of pe kind.
    DWORD       dwMachine=0;            // Working copy of machine.

#ifndef DACCESS_COMPILE
    // Do we already have cached information?
    if (m_dwPEKind != (DWORD)(-1))
    {
        dwPEKind = m_dwPEKind;
        dwMachine = m_dwMachine;
    }
    else if (m_pImage)
    {
        PEDecoder pe;

        // We need to use different PEDecoder initialization based on the type of data we give it.
        // We use the one with a 'bool' as the second argument when dealing with a mapped file,
        // and we use the one that takes a COUNT_T as the second argument when dealing with a
        // flat file.

        if (mtMapping == MTYPE_IMAGE)
        {
            if (FAILED(pe.Init(m_pImage, false)) ||
                !pe.CheckNTHeaders())
            {
                IfFailRet(COR_E_BADIMAGEFORMAT);
            }
        }
        else
        {
            pe.Init(m_pImage, (COUNT_T)(m_dwImageSize));
        }

        if (pe.HasContents() && pe.HasNTHeaders())
        {
            pe.GetPEKindAndMachine(&dwPEKind, &dwMachine);


            // Cache entries.
            m_dwPEKind = dwPEKind;
            m_dwMachine = dwMachine;
        }
        else // if (pe.HasContents()...
        {
            hr = COR_E_BADIMAGEFORMAT;
        }
    }
    else
    {
        hr = S_FALSE;
    }
#endif
    if (pdwPEKind)
        *pdwPEKind = dwPEKind;
    if (pdwMachine)
        *pdwMachine = dwMachine;

    return hr;
} // CLiteWeightStgdbRW::GetPEKind

//*****************************************************************************
// Low level access to the data.  Intended for metainfo, and such.
//*****************************************************************************
__checkReturn
HRESULT CLiteWeightStgdbRW::GetRawData(
    const void **ppvMd,                 // [OUT] put pointer to MD section here (aka, 'BSJB').
    ULONG   *pcbMd)                     // [OUT] put size of the stream here.
{
#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    if (m_pStgIO == NULL)
        return COR_E_NOTSUPPORTED;
#endif

    *ppvMd = (const void*) m_pStgIO->m_pData;
    *pcbMd = m_pStgIO->m_cbData;
    return S_OK;
} // CLiteWeightStgdbRW::GetRawData

//*****************************************************************************
// Get info about the MD stream.
// Low level access to stream data.  Intended for metainfo, and such.
//*****************************************************************************
__checkReturn
STDMETHODIMP
CLiteWeightStgdbRW::GetRawStreamInfo(
    ULONG        ix,            // [IN] Stream ordinal desired.
    const char **ppchName,      // [OUT] put pointer to stream name here.
    const void **ppv,           // [OUT] put pointer to MD stream here.
    ULONG       *pcb)           // [OUT] put size of the stream here.
{
    HRESULT        hr = NOERROR;
    STORAGEHEADER  sHdr;            // Header for the storage.
    PSTORAGESTREAM pStream;         // Pointer to each stream.
    ULONG          i;               // Loop control.
    void          *pData;
    ULONG          cbData;

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    if (m_pStgIO == NULL)
        IfFailGo(COR_E_NOTSUPPORTED);
#endif

    pData = m_pStgIO->m_pData;
    cbData = m_pStgIO->m_cbData;

    // Validate the signature of the format, or it isn't ours.
    IfFailGo(MDFormat::VerifySignature((PSTORAGESIGNATURE) pData, cbData));

    // Get back the first stream.
    pStream = MDFormat::GetFirstStream(&sHdr, pData);
    if (pStream == NULL)
    {
        Debug_ReportError("Invalid MetaData storage signature - cannot get the first stream header.");
        IfFailGo(CLDB_E_FILE_CORRUPT);
    }

    // Check that the requested stream exists.
    if (ix >= sHdr.GetiStreams())
        return S_FALSE;

    // Skip to the desired stream.
    for (i = 0; i < ix; i++)
    {
        PSTORAGESTREAM pNext = pStream->NextStream();

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
            ((pStream->GetSize() + pStream->GetOffset()) > cbData))
        {
            Debug_ReportError("Stream data are not within MetaData block.");
            hr = CLDB_E_FILE_CORRUPT;
            goto ErrExit;
        }

        // Pick off the next stream if there is one.
        pStream = pNext;
    }

    if (pStream != NULL)
    {
        *ppv = (const void *)((const BYTE *)pData + pStream->GetOffset());
        *pcb = pStream->GetSize();
        *ppchName = pStream->GetName();
    }
    else
    {
        *ppv = NULL;
        *pcb = 0;
        *ppchName = NULL;

        // Invalid input to the method
        hr = CLDB_E_FILE_CORRUPT;
    }

ErrExit:
    return hr;
} // CLiteWeightStgdbRW::GetRawStreamInfo

//=======================================================================================
//
// Set file name of this database (makes copy of the file name).
//
// Return value: S_OK or E_OUTOFMEMORY
//
__checkReturn
HRESULT
CLiteWeightStgdbRW::SetFileName(
    const WCHAR * wszFileName)
{
    HRESULT hr = S_OK;

    if (m_wszFileName != NULL)
    {
        delete [] m_wszFileName;
        m_wszFileName = NULL;
    }

    if ((wszFileName == NULL) || (*wszFileName == 0))
    {   // The new file name is empty
        _ASSERTE(m_wszFileName == NULL);

        // No need to allocate anything, NULL means empty name
        hr = S_OK;
        goto ErrExit;
    }

    // Size of the file name incl. null terminator
    size_t cchFileName;
    cchFileName = wcslen(wszFileName) + 1;

    // Allocate and copy the file name
    m_wszFileName = new (nothrow) WCHAR[cchFileName];
    IfNullGo(m_wszFileName);
    wcscpy_s(m_wszFileName, cchFileName, wszFileName);

ErrExit:
    return hr;
} // CLiteWeightStgdbRW::SetFileName

//=======================================================================================
//
// Returns TRUE if wszFileName has valid path length (MAX_PATH or 32767 if prefixed with \\?\).
//
//static
BOOL
CLiteWeightStgdbRW::IsValidFileNameLength(
    const WCHAR * wszFileName)
{
    return TRUE;
} // CLiteWeightStgdbRW::IsValidFileNameLength
