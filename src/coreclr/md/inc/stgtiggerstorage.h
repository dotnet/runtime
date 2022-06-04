// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgTiggerStorage.h
//

//
// TiggerStorage is a stripped down version of compound doc files.  Doc files
// have some very useful and complex features to them, unfortunately nothing
// comes for free.  Given the incredibly tuned format of existing .tlb files,
// every single byte counts and 10% added by doc files is just too exspensive.
//
// The storage itself is made up of a bunch of streams (each aligned to a 4 byte
// value), followed at the end of the file with the header.  The header is
// put at the end so that you can continue to write as many streams as you
// like without thrashing the disk.
//  +-------------------+
//  | Signature         |
//  +-------------------+
//  | Stream 1, 2, []   |
//  +-------------------+
//  | STORAGEHEADER     |
//  |   Extra data      |
//  |   STORAGESTREAM[] |
//  +-------------------+
//  | offset            |
//  +-------------------+
//
// The STORAGEHEADER contains flags describing the rest of the file, including
// the ability to have extra data stored in the header.  If there is extra
// data, then immediately after the STORAGEHEADER struct is a 4 byte size of
// that data, followed immediately by the extra data.  The length must be
// 4 byte aligned so that the first STORAGESTREAM starts on an aligned
// boundary.  The contents of the extra data is caller defined.
//
// This code handles the signature at the start of the file, and the list of
// streams at the end (kept in the header).  The data in each stream is, of
// course, caller specific.
//
// This code requires the StgIO code to handle the input and output from the
// backing storage, whatever scheme that may be.  There are no consistency
// checks on the data (for example crc's) due to the expense in computation
// required.  There is a signature at the front of the file and in the header.
//
//*****************************************************************************
#ifndef __StgTiggerStorage_h__
#define __StgTiggerStorage_h__

//#include "utilcode.h"                   // Helpers.

#include "mdfileformat.h"

typedef CDynArray<STORAGESTREAM> STORAGESTREAMLST;


// Forwards.
class TiggerStream;
class StgIO;



class TiggerStorage :
    public IStorage
{
friend class TiggerStream;
public:
    TiggerStorage();
    virtual ~TiggerStorage();

// IUnknown so you can ref count this thing.
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, PVOID *pp)
    { return (BadError(E_NOTIMPL)); }
    virtual ULONG STDMETHODCALLTYPE AddRef()
    { return (InterlockedIncrement(&m_cRef)); }
    virtual ULONG STDMETHODCALLTYPE Release()
    {
        SUPPORTS_DAC_HOST_ONLY;
        ULONG   cRef;
        if ((cRef = InterlockedDecrement(&m_cRef)) == 0)
            delete this;
        return (cRef);
    }


//*****************************************************************************
// Init this storage object on top of the given storage unit.
//*****************************************************************************
    HRESULT Init(                           // Return code.
        StgIO       *pStgIO,                // The I/O subsystem.
        _In_ _In_z_ LPSTR       pVersion);  // Compiler-supplied CLR version

//*****************************************************************************
// Retrieves a the size and a pointer to the extra data that can optionally be
// written in the header of the storage system.  This data is not required to
// be in the file, in which case *pcbExtra will come back as 0 and pbData will
// be set to null.  You must have initialized the storage using Init() before
// calling this function.
//*****************************************************************************
    HRESULT GetExtraData(                   // Return code.
        ULONG       *pcbExtra,              // Return size of extra data.
        BYTE        *&pbData);              // Return a pointer to extra data.

//*****************************************************************************
// Flushes the header to disk.
//*****************************************************************************
    HRESULT WriteHeader(                    // Return code.
        STORAGESTREAMLST *pList,            // List of streams.
        ULONG       cbExtraData,            // Size of extra data, may be 0.
        BYTE        *pbExtraData);          // Pointer to extra data for header.

//*****************************************************************************
// Called when all data has been written.  Forces cached data to be flushed
// and stream lists to be validated.
//*****************************************************************************
    HRESULT WriteFinished(                  // Return code.
        STORAGESTREAMLST *pList,            // List of streams.
        ULONG       *pcbSaveSize,               // Return size of total data.
        BOOL           fDeltaSave);                // Was this a delta

//*****************************************************************************
// Called after a successful rewrite of an existing file.  The in memory
// backing store is no longer valid because all new data is in memory and
// on disk.  This is essentially the same state as created, so free up some
// working set and remember this state.
//*****************************************************************************
    HRESULT ResetBackingStore();        // Return code.

//*****************************************************************************
// Called to restore the original file.  If this operation is successful, then
// the backup file is deleted as requested.  The restore of the file is done
// in write through mode to the disk help ensure the contents are not lost.
// This is not good enough to fulfill ACID props, but it ain't that bad.
//*****************************************************************************
    HRESULT Restore(                        // Return code.
        _In_ _In_z_ LPWSTR szBackup,        // If non-0, backup the file.
        int         bDeleteOnSuccess);      // Delete backup file if successful.

//*****************************************************************************
// Given the name of a stream that will be persisted into a stream in this
// storage type, figure out how big that stream would be including the user's
// stream data and the header overhead the file format incurs.  The name is
// stored in ANSI and the header struct is aligned to 4 bytes.
//*****************************************************************************
    static HRESULT GetStreamSaveSize(       // Return code.
        LPCWSTR szStreamName,       // Name of stream.
        UINT32  cbDataSize,         // Size of data to go into stream.
        UINT32 *pcbSaveSize);       // Return data size plus stream overhead.

//*****************************************************************************
// Return the fixed size overhead for the storage implementation.  This includes
// the signature and fixed header overhead.  The overhead in the header for each
// stream is calculated as part of GetStreamSaveSize because these structs are
// variable sized on the name.
//*****************************************************************************
    static HRESULT GetStorageSaveSize(      // Return code.
        ULONG       *pcbSaveSize,           // [in] current size, [out] plus overhead.
        ULONG       cbExtra,                // How much extra data to store in header.
        LPCSTR      pRuntimeVersion);       // The version string as it's length is part of the total size.

//*****************************************************************************
// Adjust the offset in each known stream to match where it will wind up after
// a save operation.
//*****************************************************************************
    static HRESULT CalcOffsets(             // Return code.
        STORAGESTREAMLST *pStreamList,      // List of streams for header.
        ULONG       cbExtra,                // Size of variable extra data in header.
        LPCSTR      pRuntimeVersion);       // The version string as it's length is part of the total size.



//*****************************************************************************
// Returns the size of the signature plus the verion information
//*****************************************************************************
    static HRESULT SizeOfStorageSignature(
        LPCSTR      pRuntimeVersion,        // The version string as it's length is part of the total size.
        ULONG       *pcbSignatureSize);

// IStorage
    virtual HRESULT STDMETHODCALLTYPE CreateStream(
        const OLECHAR *pwcsName,
        DWORD       grfMode,
        DWORD       reserved1,
        DWORD       reserved2,
        IStream     **ppstm);

    virtual HRESULT STDMETHODCALLTYPE CreateStream(
        LPCSTR      szName,
        DWORD       grfMode,
        DWORD       reserved1,
        DWORD       reserved2,
        IStream     **ppstm)
        DAC_UNEXPECTED();

    virtual HRESULT STDMETHODCALLTYPE OpenStream(
        const OLECHAR *pwcsName,
        void        *reserved1,
        DWORD       grfMode,
        DWORD       reserved2,
        IStream     **ppstm);

    virtual HRESULT STDMETHODCALLTYPE CreateStorage(
        const OLECHAR *pwcsName,
        DWORD       grfMode,
        DWORD       dwStgFmt,
        DWORD       reserved2,
        IStorage    **ppstg);

    virtual HRESULT STDMETHODCALLTYPE OpenStorage(
        const OLECHAR * wcsName,
        IStorage *      pStgPriority,
        DWORD           dwMode,
      _In_
        SNB             snbExclude,
        DWORD           reserved,
        IStorage **     ppStg);

    virtual HRESULT STDMETHODCALLTYPE CopyTo(
        DWORD       cIidExclude,
        const IID * rgIidExclude,
      _In_
        SNB         snbExclude,
        IStorage *  pStgDest);

    virtual HRESULT STDMETHODCALLTYPE MoveElementTo(
        const OLECHAR *pwcsName,
        IStorage    *pstgDest,
        const OLECHAR *pwcsNewName,
        DWORD       grfFlags);

    virtual HRESULT STDMETHODCALLTYPE Commit(
        DWORD       grfCommitFlags);

    virtual HRESULT STDMETHODCALLTYPE Revert();

    virtual HRESULT STDMETHODCALLTYPE EnumElements(
        DWORD       reserved1,
        void        *reserved2,
        DWORD       reserved3,
        IEnumSTATSTG **ppenum);

    virtual HRESULT STDMETHODCALLTYPE DestroyElement(
        const OLECHAR *pwcsName);

    virtual HRESULT STDMETHODCALLTYPE RenameElement(
        const OLECHAR *pwcsOldName,
        const OLECHAR *pwcsNewName);

    virtual HRESULT STDMETHODCALLTYPE SetElementTimes(
        const OLECHAR *pwcsName,
        const FILETIME *pctime,
        const FILETIME *patime,
        const FILETIME *pmtime);

    virtual HRESULT STDMETHODCALLTYPE SetClass(
        REFCLSID    clsid);

    virtual HRESULT STDMETHODCALLTYPE SetStateBits(
        DWORD       grfStateBits,
        DWORD       grfMask);

    virtual HRESULT STDMETHODCALLTYPE Stat(
        STATSTG     *pstatstg,
        DWORD       grfStatFlag);

    virtual HRESULT STDMETHODCALLTYPE OpenStream(
        LPCWSTR     szStream,
        ULONG       *pcbData,
        void        **ppAddress);

    // Access storage object.
    StgIO *GetStgIO()
    { return (m_pStgIO); }

#if defined(_DEBUG)
    ULONG PrintSizeInfo(                // Size of streams.
        bool verbose);                  // Be verbose?
#endif

protected:
    HRESULT Write(                      // Return code.
        LPCSTR      szName,             // Name of stream we're writing.
        const void *pData,              // Data to write.
        ULONG       cbData,             // Size of data.
        ULONG       *pcbWritten);       // How much did we write.

private:
    HRESULT FindStream(LPCSTR szName, _Out_ PSTORAGESTREAM *stream);
    HRESULT WriteSignature(LPCSTR pVersion);
    HRESULT VerifySignature(PSTORAGESIGNATURE pSig);
    HRESULT ReadHeader();
    HRESULT VerifyHeader();

    static HRESULT GetDefaultVersion(LPCSTR* ppVersion);

public:
    // This function is a workaround to allow access to the "version requested" string.
    HRESULT GetHeaderPointer(const void **ppv, ULONG *pcb);

private:
    // State data.
    StgIO       *m_pStgIO;              // Storage subsystem.
    LONG        m_cRef;                 // Ref count for COM.

    // Header data.
    STORAGEHEADER m_StgHdr;             // Header for storage.
    STORAGESTREAMLST m_Streams;         // List of streams in the storage.
    PSTORAGESTREAM m_pStreamList;       // For read mode.
    void        *m_pbExtra;             // Pointer to extra data if on disk.
};

#endif // StgTiggerStorage
