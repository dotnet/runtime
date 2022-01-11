// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgTiggerStorage.cpp
//

//
// TiggerStorage is a stripped down version of compound doc files.  Doc files
// have some very useful and complex features to them, unfortunately nothing
// comes for free.  Given the incredibly tuned format of existing .tlb files,
// every single byte counts and 10% added by doc files is just too expensive.
//
//*****************************************************************************
#include "stdafx.h"                     // Standard header.
#include "stgio.h"                      // I/O subsystem.
#include "stgtiggerstorage.h"           // Our interface.
#include "stgtiggerstream.h"            // Stream interface.
#include "corerror.h"
#include "posterror.h"
#include "mdfileformat.h"
#include "sstring.h"
#include "clrversion.h"

TiggerStorage::TiggerStorage() :
    m_pStgIO(0),
    m_cRef(1),
    m_pStreamList(0),
    m_pbExtra(0)
{
    memset(&m_StgHdr, 0, sizeof(STORAGEHEADER));
}


TiggerStorage::~TiggerStorage()
{
    if (m_pStgIO)
    {
        m_pStgIO->Release();
        m_pStgIO = 0;
    }
}


//*****************************************************************************
// Init this storage object on top of the given storage unit.
//*****************************************************************************
HRESULT
TiggerStorage::Init(
    StgIO            *pStgIO,       // The I/O subsystem.
    _In_ _In_z_ LPSTR pVersion)     // 'Compiled for' CLR version
{
    PSTORAGESIGNATURE pSig;         // Signature data for file.
    ULONG             cbData;       // Offset of header data.
    void             *ptr;          // Signature.
    HRESULT           hr = S_OK;

    // Make sure we always start at the beginning.
    //
    pStgIO->Seek(0, FILE_BEGIN);

    // Save the storage unit.
    m_pStgIO = pStgIO;
    m_pStgIO->AddRef();

    // For cases where the data already exists, verify the signature.
    if ((pStgIO->GetFlags() & DBPROP_TMODEF_CREATE) == 0)
    {
        // Map the contents into memory for easy access.
        IfFailGo(pStgIO->MapFileToMem(ptr, &cbData));

        // Get a pointer to the signature of the file, which is the first part.
        IfFailGo(pStgIO->GetPtrForMem(0, sizeof(STORAGESIGNATURE), ptr));

        // Finally, we can check the signature.
        pSig = (PSTORAGESIGNATURE)ptr;
        IfFailGo(MDFormat::VerifySignature(pSig, cbData));

        // Read and verify the header.
        IfFailGo(ReadHeader());
    }
    // For write case, dump the signature into the file up front.
    else
    {
        IfFailGo(WriteSignature(pVersion));
    }

ErrExit:
    if (FAILED(hr) && (m_pStgIO != NULL))
    {
        m_pStgIO->Release();
        m_pStgIO = NULL;
    }
    return hr;
} // TiggerStorage::Init

//*****************************************************************************
// This function is a workaround to allow access to the "version requested" string.
//*****************************************************************************
HRESULT
TiggerStorage::GetHeaderPointer(
    const void **ppv,   // Put pointer to header here.
    ULONG       *pcb)   // Put size of pointer here.
{
    void   *ptr;    // Working pointer.
    HRESULT hr;

    // Read the signature
    if (FAILED(hr = m_pStgIO->GetPtrForMem(0, sizeof(STORAGESIGNATURE), ptr)))
        return hr;

    PSTORAGESIGNATURE pStorage = (PSTORAGESIGNATURE) ptr;
    // Header data starts after signature.
    *pcb = sizeof(STORAGESIGNATURE) + pStorage->GetVersionStringLength();

    *ppv = ptr;

    return S_OK;

} // TiggerStorage::GetHeaderPointer

//*****************************************************************************
//  Get the default "Compiled for" version used to emit the meta-data
//*****************************************************************************
HRESULT
TiggerStorage::GetDefaultVersion(
    LPCSTR *ppVersion)
{
    *ppVersion = CLR_METADATA_VERSION;
    return S_OK;
} // TiggerStorage::GetDefaultVersion

HRESULT
TiggerStorage::SizeOfStorageSignature(LPCSTR pVersion, ULONG *pcbSignatureSize)
{
    HRESULT     hr;

    if (pVersion == NULL)
    {
        IfFailRet(GetDefaultVersion(&pVersion));
    }
    _ASSERTE(pVersion != NULL);

    ULONG versionSize = (ULONG)strlen(pVersion)+1;
    ULONG alignedVersionSize = (ULONG)ALIGN_UP(versionSize, 4);

    *pcbSignatureSize = sizeof(STORAGESIGNATURE) + alignedVersionSize;
    return S_OK;
}


//*****************************************************************************
// Retrieves a the size and a pointer to the extra data that can optionally be
// written in the header of the storage system.  This data is not required to
// be in the file, in which case *pcbExtra will come back as 0 and pbData will
// be set to NULL. You must have initialized the storage using Init() before
// calling this function.
//
// Return value: S_OK if found, S_FALSE, or error.
//*****************************************************************************
HRESULT
TiggerStorage::GetExtraData(
    ULONG *pcbExtra,    // Return size of extra data.
    BYTE *&pbData)      // Return a pointer to extra data.
{
    // Assuming there is extra data, then return the size and a pointer to it.
    if (m_pbExtra != NULL)
    {
        if ((m_StgHdr.GetFlags() & STGHDR_EXTRADATA) == 0)
        {
            Debug_ReportError("Inconsistent information about extra data in MetaData.");
            return PostError(CLDB_E_FILE_CORRUPT);
        }
        *pcbExtra = *(ULONG *)m_pbExtra;
        pbData = (BYTE *)((ULONG *) m_pbExtra + 1);
    }
    else
    {
        *pcbExtra = 0;
        pbData = NULL;
        return S_FALSE;
    }
    return S_OK;
} // TiggerStorage::GetExtraData


//*****************************************************************************
// Called when this stream is going away.
//*****************************************************************************
HRESULT
TiggerStorage::WriteHeader(
    STORAGESTREAMLST *pList,        // List of streams.
    ULONG             cbExtraData,  // Size of extra data, may be 0.
    BYTE             *pbExtraData)  // Pointer to extra data for header.
{
    ULONG   iLen;       // For variable sized data.
    ULONG   cbWritten;  // Track write quantity.
    HRESULT hr;
    SAVETRACE(ULONG cbDebugSize);   // Track debug size of header.

    SAVETRACE(DbgWriteEx(W("PSS:  Header:\n")));

    // Save the count and set flags.
    m_StgHdr.SetiStreams(pList->Count());
    if (cbExtraData != 0)
        m_StgHdr.AddFlags(STGHDR_EXTRADATA);

    // Write out the header of the file.
    IfFailRet(m_pStgIO->Write(&m_StgHdr, sizeof(STORAGEHEADER), &cbWritten));

    // Write out extra data if there is any.
    if (cbExtraData != 0)
    {
        _ASSERTE(pbExtraData);
        _ASSERTE((cbExtraData % 4) == 0);

        // First write the length value.
        IfFailRet(m_pStgIO->Write(&cbExtraData, sizeof(ULONG), &cbWritten));

        // And then the data.
        IfFailRet(m_pStgIO->Write(pbExtraData, cbExtraData, &cbWritten));
        SAVETRACE(DbgWriteEx(W("PSS:    extra data size %d\n"), m_pStgIO->GetCurrentOffset() - cbDebugSize);cbDebugSize=m_pStgIO->GetCurrentOffset());
    }

    // Save off each data stream.
    for (int i = 0; i < pList->Count(); i++)
    {
        PSTORAGESTREAM pStream = pList->Get(i);

        // How big is the structure (aligned) for this struct.
        iLen = (ULONG)(sizeof(STORAGESTREAM) - MAXSTREAMNAME + strlen(pStream->GetName()) + 1);

        // Write the header including the name to disk.  Does not include
        // full name buffer in struct, just string and null terminator.
        IfFailRet(m_pStgIO->Write(pStream, iLen, &cbWritten));

        // Align the data out to 4 bytes.
        if (iLen != ALIGN4BYTE(iLen))
        {
            IfFailRet(m_pStgIO->Write(&hr, ALIGN4BYTE(iLen) - iLen, 0));
        }
        SAVETRACE(DbgWriteEx(W("PSS:    Table %hs header size %d\n"), pStream->rcName, m_pStgIO->GetCurrentOffset() - cbDebugSize);cbDebugSize=m_pStgIO->GetCurrentOffset());
    }
    SAVETRACE(DbgWriteEx(W("PSS:  Total size of header data %d\n"), m_pStgIO->GetCurrentOffset()));
    // Make sure the whole thing is 4 byte aligned.
    _ASSERTE((m_pStgIO->GetCurrentOffset() % 4) == 0);
    return S_OK;
} // TiggerStorage::WriteHeader


//*****************************************************************************
// Called when all data has been written.  Forces cached data to be flushed
// and stream lists to be validated.
//*****************************************************************************
HRESULT
TiggerStorage::WriteFinished(
    STORAGESTREAMLST *pList,        // List of streams.
    ULONG            *pcbSaveSize,  // Return size of total data.
    BOOL              fDeltaSave)   // Was this a delta
{
    PSTORAGESTREAM pEntry;      // Loop control.
    HRESULT        hr;

    // If caller wants the total size of the file, we are there right now.
    if (pcbSaveSize != NULL)
        *pcbSaveSize = m_pStgIO->GetCurrentOffset();

    // Flush our internal write cache to disk.
    IfFailRet(m_pStgIO->FlushCache());

    // Force user's data onto disk right now so that Commit() can be
    // more accurate (although not totally up to the D in ACID).
    hr = m_pStgIO->FlushFileBuffers();
    _ASSERTE(SUCCEEDED(hr));

    // Run through all of the streams and validate them against the expected
    // list we wrote out originally.

    // Robustness check: stream counts must match what was written.
    _ASSERTE(pList->Count() == m_Streams.Count());
    if (pList->Count() != m_Streams.Count())
    {
        _ASSERTE_MSG(FALSE, "Mismatch in streams, save would cause corruption.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }

    // If we're saving a true delta, then this sanity check won't help.
    // @TODO - Implement a sanity check for the deltas
    if (!fDeltaSave)
    {
        // Sanity check each saved stream data size and offset.
        for (int i = 0; i < pList->Count(); i++)
        {
            pEntry = pList->Get(i);

            _ASSERTE(pEntry->GetOffset() == m_Streams[i].GetOffset());
            _ASSERTE(pEntry->GetSize() == m_Streams[i].GetSize());
            _ASSERTE(strcmp(pEntry->GetName(), m_Streams[i].GetName()) == 0);

            // For robustness, check that everything matches expected value,
            // and if it does not, refuse to save the data and force a rollback.
            // The alternative is corruption of the data file.
            if ((pEntry->GetOffset() != m_Streams[i].GetOffset()) ||
                (pEntry->GetSize() != m_Streams[i].GetSize()) ||
                (strcmp(pEntry->GetName(), m_Streams[i].GetName()) != 0))
            {
                _ASSERTE_MSG(FALSE, "Mismatch in streams, save would cause corruption.");
                hr = PostError(CLDB_E_FILE_CORRUPT);
                break;
            }

            //<REVISIT_TODO>@future:
            // if iOffset or iSize mismatches, it means a bug in GetSaveSize
            // which we can successfully detect right here.  In that case, we
            // could use the pStgIO and seek back to the header and correct the
            // mistmake.  This will break any client who lives on the GetSaveSize
            // value which came back originally, but would be more robust than
            // simply throwing back an error which will corrupt the file.</REVISIT_TODO>
        }
    }
    return hr;
} // TiggerStorage::WriteFinished


//*****************************************************************************
// Called after a successful rewrite of an existing file.  The in memory
// backing store is no longer valid because all new data is in memory and
// on disk.  This is essentially the same state as created, so free up some
// working set and remember this state.
//*****************************************************************************
HRESULT TiggerStorage::ResetBackingStore()  // Return code.
{
    return (m_pStgIO->ResetBackingStore());
}


//*****************************************************************************
// Given the name of a stream that will be persisted into a stream in this
// storage type, figure out how big that stream would be including the user's
// stream data and the header overhead the file format incurs.  The name is
// stored in ANSI and the header struct is aligned to 4 bytes.
//*****************************************************************************
HRESULT
TiggerStorage::GetStreamSaveSize(
    LPCWSTR szStreamName,       // Name of stream.
    UINT32  cbDataSize,         // Size of data to go into stream.
    UINT32 *pcbSaveSize)        // Return data size plus stream overhead.
{
    UINT32 cbTotalSize;            // Add up each element.

    // Find out how large the name will be.
    cbTotalSize = ::WszWideCharToMultiByte(CP_ACP, 0, szStreamName, -1, 0, 0, 0, 0);
    _ASSERTE(cbTotalSize != 0);

    // Add the size of the stream header minus the static name array.
    cbTotalSize += sizeof(STORAGESTREAM) - MAXSTREAMNAME;

    // Finally align the header value.
    cbTotalSize = ALIGN4BYTE(cbTotalSize);

    // Return the size of the user data and the header data.
    *pcbSaveSize = cbTotalSize + cbDataSize;
    return S_OK;
} // TiggerStorage::GetStreamSaveSize


//*****************************************************************************
// Return the fixed size overhead for the storage implementation.  This includes
// the signature and fixed header overhead.  The overhead in the header for each
// stream is calculated as part of GetStreamSaveSize because these structs are
// variable sized on the name.
//*****************************************************************************
HRESULT TiggerStorage::GetStorageSaveSize( // Return code.
    ULONG       *pcbSaveSize,           // [in] current size, [out] plus overhead.
    ULONG       cbExtra,                // How much extra data to store in header.
    LPCSTR      pRuntimeVersion)
{
    HRESULT hr;

    ULONG cbSignatureSize;
    IfFailRet(SizeOfStorageSignature(pRuntimeVersion, &cbSignatureSize));

    *pcbSaveSize += cbSignatureSize + sizeof(STORAGEHEADER);
    if (cbExtra)
        *pcbSaveSize += sizeof(ULONG) + cbExtra;
    return (S_OK);
}


//*****************************************************************************
// Adjust the offset in each known stream to match where it will wind up after
// a save operation.
//*****************************************************************************
HRESULT TiggerStorage::CalcOffsets(     // Return code.
    STORAGESTREAMLST *pStreamList,      // List of streams for header.
    ULONG       cbExtra,                // Size of variable extra data in header.
    LPCSTR      pRuntimeVersion)        // The version string as it's length is part of the total size.
{
    PSTORAGESTREAM pEntry;              // Each entry in the list.
    ULONG       cbOffset=0;             // Running offset for streams.
    int         i;                      // Loop control.

    // Prime offset up front.
    GetStorageSaveSize(&cbOffset, cbExtra, pRuntimeVersion);

    // Add on the size of each header entry.
    for (i=0;  i<pStreamList->Count();  i++)
    {
        VERIFY(pEntry = pStreamList->Get(i));
        cbOffset += sizeof(STORAGESTREAM) - MAXSTREAMNAME;
        cbOffset += (ULONG)(strlen(pEntry->GetName()) + 1);
        cbOffset = ALIGN4BYTE(cbOffset);
    }

    // Go through each stream and reset its expected offset.
    for (i=0;  i<pStreamList->Count();  i++)
    {
        VERIFY(pEntry = pStreamList->Get(i));
        pEntry->SetOffset(cbOffset);
        cbOffset += pEntry->GetSize();
    }
    return (S_OK);
}



HRESULT STDMETHODCALLTYPE TiggerStorage::CreateStream(
    const OLECHAR *pwcsName,
    DWORD       grfMode,
    DWORD       reserved1,
    DWORD       reserved2,
    IStream     **ppstm)
{
    char        rcStream[MAXSTREAMNAME];// For converted name.
    VERIFY(Wsz_wcstombs(rcStream, pwcsName, sizeof(rcStream)));
    return (CreateStream(rcStream, grfMode, reserved1, reserved2, ppstm));
}


#ifndef DACCESS_COMPILE
HRESULT STDMETHODCALLTYPE TiggerStorage::CreateStream(
    LPCSTR      szName,
    DWORD       grfMode,
    DWORD       reserved1,
    DWORD       reserved2,
    IStream     **ppstm)
{
    PSTORAGESTREAM pStream;             // For lookup.
    HRESULT     hr;

    _ASSERTE(szName && *szName);

    // Check for existing stream, which might be an error or more likely
    // a rewrite of a file.
    if (SUCCEEDED(FindStream(szName, &pStream)))
    {
        // <REVISIT_TODO>REVIEW: STGM_FAILIFTHERE is 0, the following condition will be always false</REVISIT_TODO>
        if (pStream->GetOffset() != 0xffffffff && ((grfMode & STGM_CREATE) == STGM_FAILIFTHERE))
            return (PostError(STG_E_FILEALREADYEXISTS));
    }
    // Add a control to track this stream.
    else if (!pStream && (pStream = m_Streams.Append()) == 0)
        return (PostError(OutOfMemory()));
    pStream->SetOffset(0xffffffff);
    pStream->SetSize(0);
    strcpy_s(pStream->GetName(), 32, szName);

    // Now create a stream object to allow reading and writing.
    TiggerStream *pNew = new (nothrow) TiggerStream;
    if (!pNew)
        return (PostError(OutOfMemory()));
    *ppstm = (IStream *) pNew;

    // Init the new object.
    if (FAILED(hr = pNew->Init(this, pStream->GetName())))
    {
        delete pNew;
        return (hr);
    }
    return (S_OK);
}
#endif //!DACCESS_COMPILE


HRESULT STDMETHODCALLTYPE TiggerStorage::OpenStream(
    const OLECHAR *pwcsName,
    void        *reserved1,
    DWORD       grfMode,
    DWORD       reserved2,
    IStream     **ppstm)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::CreateStorage(
    const OLECHAR *pwcsName,
    DWORD       grfMode,
    DWORD       dwStgFmt,
    DWORD       reserved2,
    IStorage    **ppstg)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE
TiggerStorage::OpenStorage(
    const OLECHAR * wcsName,
    IStorage *      pStgPriority,
    DWORD           dwMode,
  _In_
    SNB             snbExclude,
    DWORD           reserved,
    IStorage **     ppStg)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
TiggerStorage::CopyTo(
    DWORD       cIidExclude,
    const IID * rgIidExclude,
  _In_
    SNB         snbExclude,
    IStorage *  pStgDest)
{
    return E_NOTIMPL;
}


HRESULT STDMETHODCALLTYPE TiggerStorage::MoveElementTo(
    const OLECHAR *pwcsName,
    IStorage    *pstgDest,
    const OLECHAR *pwcsNewName,
    DWORD       grfFlags)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::Commit(
    DWORD       grfCommitFlags)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::Revert()
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::EnumElements(
    DWORD       reserved1,
    void        *reserved2,
    DWORD       reserved3,
    IEnumSTATSTG **ppenum)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::DestroyElement(
    const OLECHAR *pwcsName)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::RenameElement(
    const OLECHAR *pwcsOldName,
    const OLECHAR *pwcsNewName)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::SetElementTimes(
    const OLECHAR *pwcsName,
    const FILETIME *pctime,
    const FILETIME *patime,
    const FILETIME *pmtime)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::SetClass(
    REFCLSID    clsid)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::SetStateBits(
    DWORD       grfStateBits,
    DWORD       grfMask)
{
    return (E_NOTIMPL);
}


HRESULT STDMETHODCALLTYPE TiggerStorage::Stat(
    STATSTG     *pstatstg,
    DWORD       grfStatFlag)
{
    return (E_NOTIMPL);
}



HRESULT STDMETHODCALLTYPE TiggerStorage::OpenStream(
    LPCWSTR     szStream,
    ULONG       *pcbData,
    void        **ppAddress)
{
    PSTORAGESTREAM pStream;             // For lookup.
    char        rcName[MAXSTREAMNAME];  // For conversion.
    HRESULT     hr;

    // Convert the name for internal use.
    VERIFY(::WszWideCharToMultiByte(CP_ACP, 0, szStream, -1, rcName, sizeof(rcName), 0, 0));

    // Look for the stream which must be found for this to work.  Note that
    // this error is explicitly not posted as an error object since unfound streams
    // are a common occurrence and do not warrant a resource file load.
    IfFailRet(FindStream(rcName, &pStream));

    // Get the memory for the stream.
    IfFailRet( m_pStgIO->GetPtrForMem(pStream->GetOffset(), pStream->GetSize(), *ppAddress) );
    *pcbData = pStream->GetSize();
    return (S_OK);
}



//
// Protected.
//


//*****************************************************************************
// Called by the stream implementation to write data out to disk.
//*****************************************************************************
HRESULT
TiggerStorage::Write(
    LPCSTR      szName,         // Name of stream we're writing.
    const void *pData,          // Data to write.
    ULONG       cbData,         // Size of data.
    ULONG      *pcbWritten)     // How much did we write.
{
    PSTORAGESTREAM pStream;     // Update size data.
    ULONG   iOffset = 0;        // Offset for write.
    ULONG   cbWritten;          // Handle null case.
    HRESULT hr;

    // Get the stream descriptor.
    if (FAILED(FindStream(szName, &pStream)))
        return CLDB_E_FILE_BADWRITE;

    // If we need to know the offset, keep it now.
    if (pStream->GetOffset() == 0xffffffff)
    {
        iOffset = m_pStgIO->GetCurrentOffset();

        // Align the storage on a 4 byte boundary.
        if ((iOffset % 4) != 0)
        {
            ULONG cb;
            ULONG pad = 0;

            if (FAILED(hr = m_pStgIO->Write(&pad, ALIGN4BYTE(iOffset) - iOffset, &cb)))
                return hr;
            iOffset = m_pStgIO->GetCurrentOffset();

            _ASSERTE((iOffset % 4) == 0);
        }
    }

    // Avoid confusion.
    if (pcbWritten == NULL)
        pcbWritten = &cbWritten;
    *pcbWritten = 0;

    // Let OS do the write.
    if (SUCCEEDED(hr = m_pStgIO->Write(pData, cbData, pcbWritten)))
    {
        // On success, record the new data.
        if (pStream->GetOffset() == 0xffffffff)
            pStream->SetOffset(iOffset);
        pStream->SetSize(pStream->GetSize() + *pcbWritten);
        return S_OK;
    }
    else
    {
        return hr;
    }
} // TiggerStorage::Write


//
// Private
//

HRESULT
TiggerStorage::FindStream(
    LPCSTR                szName,
    _Out_ PSTORAGESTREAM *stream)
{
    *stream = NULL;
    // In read mode, just walk the list and return one.
    if (m_pStreamList != NULL)
    {
        PSTORAGESTREAM p = m_pStreamList;

        SIZE_T pStartMD = (SIZE_T)(m_pStgIO->m_pData);
        SIZE_T pEndMD = NULL;

        if (!ClrSafeInt<SIZE_T>::addition(pStartMD, m_pStgIO->m_cbData, pEndMD))
        {
            Debug_ReportError("Invalid MetaData storage headers - size overflow.");
            return CLDB_E_FILE_CORRUPT;
        }

        for (int i = 0; i < m_StgHdr.GetiStreams(); i++)
        {
            // Make sure this stream pointer is still inside the metadata
            if (((SIZE_T)p < pStartMD) || ((SIZE_T)p > pEndMD))
            {
                Debug_ReportError("Invalid MetaData storage header - reached outside headers block.");
                return CLDB_E_FILE_CORRUPT;
            }

            if (SString::_stricmp(p->GetName(), szName) == 0)
            {
                *stream = p;
                return S_OK;
            }
            p = p->NextStream();
        }
    }
    // In write mode, walk the array which is not on disk yet.
    else
    {
        for (int j = 0; j < m_Streams.Count(); j++)
        {
            if (SString::_stricmp(m_Streams[j].GetName(), szName) == 0)
            {
                *stream = &m_Streams[j];
                return S_OK;
            }
        }
    }
    return STG_E_FILENOTFOUND;
} // TiggerStorage::FindStream


//*****************************************************************************
// Write the signature area of the file format to disk.  This includes the
// "magic" identifier and the version information.
//*****************************************************************************
HRESULT
TiggerStorage::WriteSignature(
    LPCSTR pVersion)
{
    STORAGESIGNATURE sSig;
    ULONG   cbWritten;
    HRESULT hr = S_OK;

    if (pVersion == NULL)
    {
        IfFailRet(GetDefaultVersion(&pVersion));
    }
    _ASSERTE(pVersion != NULL);

    ULONG versionSize = (ULONG)strlen(pVersion) + 1;
    ULONG alignedVersionSize = (ULONG)ALIGN_UP(versionSize, 4);

    // Signature belongs at the start of the file.
    _ASSERTE(m_pStgIO->GetCurrentOffset() == 0);

    sSig.SetSignature(STORAGE_MAGIC_SIG);
    sSig.SetMajorVer(FILE_VER_MAJOR);
    sSig.SetMinorVer(FILE_VER_MINOR);
    sSig.SetExtraDataOffset(0); // We have no extra inforation
    sSig.SetVersionStringLength(alignedVersionSize);
    IfFailRet(m_pStgIO->Write(&sSig, sizeof(STORAGESIGNATURE), &cbWritten));
    IfFailRet(m_pStgIO->Write(pVersion, versionSize, &cbWritten));

    // Write padding
    if (alignedVersionSize - versionSize != 0)
    {
        BYTE padding[4];
        ZeroMemory(padding, sizeof(padding));
        IfFailRet(m_pStgIO->Write(padding, alignedVersionSize - versionSize, &cbWritten));
    }

    return hr;
} // TiggerStorage::WriteSignature


//*****************************************************************************
// Read the header from disk.  This reads the header for the most recent version
// of the file format which has the header at the front of the data file.
//*****************************************************************************
HRESULT
TiggerStorage::ReadHeader()
{
    PSTORAGESTREAM pAppend, pStream;    // For copy of array.
    void          *ptr;                 // Working pointer.
    ULONG          iOffset;             // Offset of header data.
    ULONG          cbExtra;             // Size of extra data.
    ULONG          cbRead;              // For calc of read sizes.
    HRESULT        hr;

    // Read the signature
    if (FAILED(hr = m_pStgIO->GetPtrForMem(0, sizeof(STORAGESIGNATURE), ptr)))
    {
        Debug_ReportError("Cannot read MetaData storage signature header.");
        return hr;
    }

    PSTORAGESIGNATURE pStorage = (PSTORAGESIGNATURE)ptr;

    // Header data starts after signature.
    iOffset = sizeof(STORAGESIGNATURE) + pStorage->GetVersionStringLength();

    // Read the storage header which has the stream counts.  Throw in the extra
    // count which might not exist, but saves us down stream.
    if (FAILED(hr = m_pStgIO->GetPtrForMem(iOffset, sizeof(STORAGEHEADER) + sizeof(ULONG), ptr)))
    {
        Debug_ReportError("Cannot read first MetaData storage header.");
        return hr;
    }
    _ASSERTE(m_pStgIO->IsAlignedPtr((ULONG_PTR) ptr, 4));

    // Read the storage header which has the stream counts.  Throw in the extra
    // count which might not exist, but saves us down stream.
    if (FAILED(hr = m_pStgIO->GetPtrForMem(iOffset, sizeof(STORAGEHEADER) + sizeof(ULONG), ptr)))
    {
        Debug_ReportError("Cannot read second MetaData storage header.");
        return hr;
    }
    if (!m_pStgIO->IsAlignedPtr((ULONG_PTR)ptr, 4))
    {
        Debug_ReportError("Invalid MetaData storage headers - unaligned size.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }

    // Copy the header into memory and check it.
    memcpy(&m_StgHdr, ptr, sizeof(STORAGEHEADER));
    IfFailRet( VerifyHeader() );
    ptr = (void *)((PSTORAGEHEADER)ptr + 1);
    iOffset += sizeof(STORAGEHEADER);

    // Save off a pointer to the extra data.
    if ((m_StgHdr.GetFlags() & STGHDR_EXTRADATA) != 0)
    {
        m_pbExtra = ptr;
        cbExtra = sizeof(ULONG) + *(ULONG *)ptr;

        // Force the extra data to get faulted in.
        IfFailRet(m_pStgIO->GetPtrForMem(iOffset, cbExtra, ptr));
        if (!m_pStgIO->IsAlignedPtr((ULONG_PTR)ptr, 4))
        {
            Debug_ReportError("Invalid MetaData storage signature - unaligned extra data.");
            return PostError(CLDB_E_FILE_CORRUPT);
        }
    }
    else
    {
        m_pbExtra = 0;
        cbExtra = 0;
    }
    iOffset += cbExtra;

    // Force the worst case scenario of bytes to get faulted in for the
    // streams.  This makes the rest of this code very simple.
    cbRead = sizeof(STORAGESTREAM) * m_StgHdr.GetiStreams();
    if (cbRead != 0)
    {
        cbRead = min(cbRead, m_pStgIO->GetDataSize() - iOffset);
        if (FAILED(hr = m_pStgIO->GetPtrForMem(iOffset, cbRead, ptr)))
        {
            Debug_ReportError("Invalid MetaData stogare headers.");
            return hr;
        }
        if (!m_pStgIO->IsAlignedPtr((ULONG_PTR)ptr, 4))
        {
            Debug_ReportError("Invalid MetaData stogare headers - unaligned start.");
            return PostError(CLDB_E_FILE_CORRUPT);
        }

        // For read only, just access the header data.
        if (m_pStgIO->IsReadOnly())
        {
            // Save a pointer to the current list of streams.
            m_pStreamList = (PSTORAGESTREAM)ptr;
        }
        // For writeable, need a copy we can modify.
        else
        {
            pStream = (PSTORAGESTREAM)ptr;

            // Copy each of the stream headers.
            for (int i = 0; i < m_StgHdr.GetiStreams(); i++)
            {
                if ((pAppend = m_Streams.Append()) == NULL)
                    return PostError(OutOfMemory());
                // Validate that the stream header is not too big.
                ULONG sz = pStream->GetStreamSize();
                if (sz > sizeof(STORAGESTREAM))
                {
                    Debug_ReportError("Invalid MetaData storage stream - data too big.");
                    return PostError(CLDB_E_FILE_CORRUPT);
                }
                memcpy (pAppend, pStream, sz);
                pStream = pStream->NextStream();
                if (!m_pStgIO->IsAlignedPtr((ULONG_PTR)pStream, 4))
                {
                    Debug_ReportError("Invalid MetaData storage stream - unaligned data.");
                    return PostError(CLDB_E_FILE_CORRUPT);
                }
            }

            // All must be loaded and accounted for.
            _ASSERTE(m_StgHdr.GetiStreams() == m_Streams.Count());
        }
    }
    return S_OK;
} // TiggerStorage::ReadHeader


//*****************************************************************************
// Verify the header is something this version of the code can support.
//*****************************************************************************
HRESULT TiggerStorage::VerifyHeader()
{
    //<REVISIT_TODO>@FUTURE: add version check for format.</REVISIT_TODO>
    return S_OK;
}

//*****************************************************************************
// Print the sizes of the various streams.
//*****************************************************************************
#if defined(_DEBUG)
ULONG TiggerStorage::PrintSizeInfo(bool verbose)
{
    ULONG total = 0;

    printf("Storage Header:  %zu\n", sizeof(STORAGEHEADER));
    if (m_pStreamList != NULL)
    {
        PSTORAGESTREAM storStream = m_pStreamList;
        PSTORAGESTREAM pNext;
        for (int i = 0; i < m_StgHdr.GetiStreams(); i++)
        {
            pNext = storStream->NextStream();
            printf("Stream #%d (%s) Header: %zd, Data: %lu\n",i,storStream->GetName(), (size_t)((BYTE*)pNext - (BYTE*)storStream), storStream->GetSize());
            total += storStream->GetSize();
            storStream = pNext;
        }
    }
    else
    {
        //<REVISIT_TODO>todo: Add support for the case where m_Streams exists and m_pStreamList does not</REVISIT_TODO>
    }

    if (m_pbExtra != NULL)
    {
        printf("Extra bytes: %d\n",*(ULONG*)m_pbExtra);
        total += *(ULONG*)m_pbExtra;
    }
    return total;
}
#endif // _DEBUG
