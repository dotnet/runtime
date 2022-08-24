// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgIO.h
//

//
// This module handles disk/memory i/o for a generic set of storage solutions,
// including:
//  * File system handle (HFILE)
//  * IStream
//  * User supplied memory buffer (non-movable)
//
// The Read, Write, Seek, ... functions are all directed to the corresponding
// method for each type of file, allowing the consumer to use one set of api's.
//
// File system data can be paged fully into memory in two scenarios:
//  read:   Normal memory mapped file is created to manage paging.
//  write:  A custom paging system provides storage for pages as required.  This
//              data is invalidated when you call Rewrite on the file.
//
// Transactions and backups are handled in the existing file case only.  The
// Rewrite function can make a backup of the current contents, and the Restore
// function can be used to recover the data into the current scope.  The backup
// file is flushed to disk (which is slower but safer) after the copy.  The
// Restore also flushed the recovered changes to disk.  Worst case scenario you
// get a crash after calling Rewrite but before Restore, in which case you will
// have a foo.clb.txn file in the same directory as the source file, foo.clb in
// this example.
//<REVISIT_TODO>
// @FUTURE: issues,
//  1.  For reading a .clb in an image, it would be great to memory map
//      only the portion of the file with the .clb in it.
//</REVISIT_TODO>
//*****************************************************************************
#include "stdafx.h"                     // Standard headers.
#include "stgio.h"                      // Our definitions.
#include "corerror.h"
#include "posterror.h"
#include "pedecoder.h"
#include "pedecoder.inl"

//********** Types. ***********************************************************
#define SMALL_ALLOC_MAP_SIZE (64 * 1024) // 64 kb is the minimum size of virtual
                                        // memory you can allocate, so anything
                                        // less is a waste of VM resources.


#define MIN_WRITE_CACHE_BYTES (16 * 1024) // 16 kb for a write back cache


//********** Locals. **********************************************************
HRESULT MapFileError(DWORD error);
static void *AllocateMemory(int iSize);
static void FreeMemory(void *pbData);
inline HRESULT MapFileError(DWORD error)
{
    return (PostError(HRESULT_FROM_WIN32(error)));
}

// Static to class.
int StgIO::m_iPageSize=0;               // Size of an OS page.
int StgIO::m_iCacheSize=0;              // Size for the write cache.



//********** Code. ************************************************************
StgIO::StgIO(
    bool        bAutoMap) :             // Memory map for read on open?
    m_bAutoMap(bAutoMap)
{
    CtorInit();

    // If the system page size has not been queried, do so now.
    if (m_iPageSize == 0)
    {
        SYSTEM_INFO sInfo;              // Some O/S information.

        // Query the system page size.
        GetSystemInfo(&sInfo);
        m_iPageSize = sInfo.dwPageSize;
        m_iCacheSize = ((MIN_WRITE_CACHE_BYTES - 1) & ~(m_iPageSize - 1)) + m_iPageSize;
    }
}


void StgIO::CtorInit()
{
    m_bWriteThrough = false;
    m_bRewrite = false;
    m_bFreeMem = false;
    m_pIStream = 0;
    m_hFile = INVALID_HANDLE_VALUE;
    m_hModule = NULL;
    m_hMapping = 0;
    m_pBaseData = 0;
    m_pData = 0;
    m_cbData = 0;
    m_fFlags = 0;
    m_iType = STGIO_NODATA;
    m_cbOffset = 0;
    m_rgBuff = 0;
    m_cbBuff = 0;
    m_rgPageMap = 0;
    m_FileType = FILETYPE_UNKNOWN;
    m_cRef = 1;
    m_mtMappedType = MTYPE_NOMAPPING;
}



StgIO::~StgIO()
{
    if (m_rgBuff)
    {
        FreeMemory(m_rgBuff);
        m_rgBuff = 0;
    }

    Close();
}


//*****************************************************************************
// Open the base file on top of: (a) file, (b) memory buffer, or (c) stream.
// If create flag is specified, then this will create a new file with the
// name supplied.  No data is read from an opened file.  You must call
// MapFileToMem before doing direct pointer access to the contents.
//*****************************************************************************
HRESULT StgIO::Open(                    // Return code.
    LPCWSTR     szName,                 // Name of the storage.
    int        fFlags,                 // How to open the file.
    const void  *pbBuff,                // Optional buffer for memory.
    ULONG       cbBuff,                 // Size of buffer.
    IStream     *pIStream,              // Stream for input.
    LPSECURITY_ATTRIBUTES pAttributes)  // Security token.
{
    HRESULT hr;

    // If we were given the storage memory to begin with, then use it.
    if (pbBuff && cbBuff)
    {
        _ASSERTE((fFlags & DBPROP_TMODEF_WRITE) == 0);

        // Save the memory address and size only.  No handles.
        m_pData = (void *) pbBuff;
        m_cbData = cbBuff;

        // All access to data will be by memory provided.
        if ((fFlags & DBPROP_TMODEF_SHAREDMEM) == DBPROP_TMODEF_SHAREDMEM)
        {
            // We're taking ownership of this memory
            m_pBaseData = m_pData;
            m_iType = STGIO_SHAREDMEM;
        }
        else
        {
            m_iType = STGIO_MEM;
        }
        goto ErrExit;
    }
    // Check for data backed by a stream pointer.
    else if (pIStream)
    {
        // If this is for the non-create case, get the size of existing data.
        if ((fFlags & DBPROP_TMODEF_CREATE) == 0)
        {
            LARGE_INTEGER   iMove = { { 0, 0 } };
            ULARGE_INTEGER  iSize;

            // Need the size of the data so we can map it into memory.
            if (FAILED(hr = pIStream->Seek(iMove, STREAM_SEEK_END, &iSize)))
                return (hr);
            m_cbData = iSize.u.LowPart;
        }
        // Else there is nothing.
        else
            m_cbData = 0;

        // Save an addref'd copy of the stream.
        m_pIStream = pIStream;
        m_pIStream->AddRef();

        // All access to data will be by memory provided.
        m_iType = STGIO_STREAM;
        goto ErrExit;
    }

    // If not on memory, we need a file to do a create/open.
    if (!szName || !*szName)
    {
        return (PostError(E_INVALIDARG));
    }
    // Check for create of a new file.
    else if (fFlags & DBPROP_TMODEF_CREATE)
    {
        //<REVISIT_TODO>@future: This could chose to open the file in write through
        // mode, which would provide better Duribility (from ACID props),
        // but would be much slower.</REVISIT_TODO>

        // Create the new file, overwriting only if caller allows it.
        if ((m_hFile = WszCreateFile(szName, GENERIC_READ | GENERIC_WRITE, 0, 0,
                (fFlags & DBPROP_TMODEF_FAILIFTHERE) ? CREATE_NEW : CREATE_ALWAYS,
                0, 0)) == INVALID_HANDLE_VALUE)
        {
            return (MapFileError(GetLastError()));
        }

        // Data will come from the file.
        m_iType = STGIO_HFILE;
    }
    // For open in read mode, need to open the file on disk.  If opening a shared
    // memory view, it has to be opened already, so no file open.
    else if ((fFlags & DBPROP_TMODEF_WRITE) == 0)
    {
        // We have not opened the file nor loaded it as module
        _ASSERTE(m_hFile == INVALID_HANDLE_VALUE);
        _ASSERTE(m_hModule == NULL);

        // Open the file for read.  Sharing is determined by caller, it can
        // allow other readers or be exclusive.
        DWORD dwFileSharingFlags = FILE_SHARE_DELETE;
        if (!(fFlags & DBPROP_TMODEF_EXCLUSIVE))
        {
            dwFileSharingFlags |= FILE_SHARE_READ;

#if !defined(DACCESS_COMPILE) && !defined(TARGET_UNIX)
            // PEDecoder is not defined in DAC

            // We prefer to use LoadLibrary if we can because it will share already loaded images (used for execution)
            // which saves virtual memory. We only do this if our caller has indicated that this PE file is trusted
            // and thus it is OK to do LoadLibrary (note that we still only load it as a resource, which mitigates
            // most of the security risk anyway).
            if ((fFlags & DBPROP_TMODEF_TRYLOADLIBRARY) != 0)
            {
                m_hModule = WszLoadLibraryEx(szName, NULL, LOAD_LIBRARY_AS_IMAGE_RESOURCE);
                if (m_hModule != NULL)
                {
                    m_iType = STGIO_HMODULE;

                    m_mtMappedType = MTYPE_IMAGE;

                    // LoadLibraryEx returns 2 lowest bits indicating how the module was loaded
                    m_pBaseData = m_pData = (void *)(((INT_PTR)m_hModule) & ~(INT_PTR)0x3);

                    PEDecoder peDecoder;
                    if (SUCCEEDED(peDecoder.Init(
                                m_pBaseData,
                                false)) &&  // relocated
                        peDecoder.CheckNTHeaders())
                    {
                        m_cbData = peDecoder.GetNTHeaders32()->OptionalHeader.SizeOfImage;
                    }
                    else
                    {
                        // PEDecoder failed on loaded library, let's backout all our changes to this object
                        // and fall back to file mapping
                        m_iType = STGIO_NODATA;
                        m_mtMappedType = MTYPE_NOMAPPING;
                        m_pBaseData = m_pData = NULL;

                        FreeLibrary(m_hModule);
                        m_hModule = NULL;
                    }
                }
            }
#endif //!DACCESS_COMPILE && !TARGET_UNIX
        }

        if (m_hModule == NULL)
        {   // We didn't get the loaded module (we either didn't want to or it failed)
            HandleHolder hFile(WszCreateFile(szName,
                                             GENERIC_READ,
                                             dwFileSharingFlags,
                                             0,
                                             OPEN_EXISTING,
                                             0,
                                             0));

            if (hFile == INVALID_HANDLE_VALUE)
                return (MapFileError(GetLastError()));

            // Get size of file.
            m_cbData = ::SetFilePointer(hFile, 0, 0, FILE_END);

            // Can't read anything from an empty file.
            if (m_cbData == 0)
                return (PostError(CLDB_E_NO_DATA));

            // Data will come from the file.
            m_hFile = hFile.Extract();

            m_iType = STGIO_HFILE;
        }
    }

ErrExit:

    // If we will ever write, then we need the buffer cache.
    if (fFlags & DBPROP_TMODEF_WRITE)
    {
        // Allocate a cache buffer for writing.
        if ((m_rgBuff = (BYTE *) AllocateMemory(m_iCacheSize)) == NULL)
        {
            Close();
            return PostError(OutOfMemory());
        }
        m_cbBuff = 0;
    }

    // Save flags for later.
    m_fFlags = fFlags;
    if ((szName != NULL) && (*szName != 0))
    {
        WCHAR rcExt[_MAX_PATH];
        SplitPath(szName, NULL, 0, NULL, 0, NULL, 0, rcExt, _MAX_PATH);
        if (SString::_wcsicmp(rcExt, W(".obj")) == 0)
        {
            m_FileType = FILETYPE_NTOBJ;
        }
        else if (SString::_wcsicmp(rcExt, W(".tlb")) == 0)
        {
            m_FileType = FILETYPE_TLB;
        }
    }

    // For auto map case, map the view of the file as part of open.
    if (m_bAutoMap &&
        (m_iType == STGIO_HFILE || m_iType == STGIO_STREAM) &&
        !(fFlags & DBPROP_TMODEF_CREATE))
    {
        void * ptr;
        ULONG  cb;

        if (FAILED(hr = MapFileToMem(ptr, &cb, pAttributes)))
        {
            Close();
            return hr;
        }
    }
    return S_OK;
} // StgIO::Open


//*****************************************************************************
// Shut down the file handles and allocated objects.
//*****************************************************************************
void StgIO::Close()
{
    switch (m_iType)
    {
        // Free any allocated memory.
        case STGIO_SHAREDMEM:
        if (m_pBaseData != NULL)
        {
            CoTaskMemFree(m_pBaseData);
            m_pBaseData = NULL;
            break;
        }

        FALLTHROUGH;

        case STGIO_MEM:
        case STGIO_HFILEMEM:
        if (m_bFreeMem && m_pBaseData)
        {
            FreeMemory(m_pBaseData);
            m_pBaseData = m_pData = 0;
        }
        // Intentional fall through to file case, if we kept handle open.
        FALLTHROUGH;

        case STGIO_HFILE:
        {
            // Free the file handle.
            if (m_hFile != INVALID_HANDLE_VALUE)
                CloseHandle(m_hFile);

            // If we allocated space for in memory paging, then free it.
        }
        break;

        case STGIO_HMODULE:
        {
            if (m_hModule != NULL)
                FreeLibrary(m_hModule);
            m_hModule = NULL;
            break;
        }

        // Free the stream pointer.
        case STGIO_STREAM:
                {
            if (m_pIStream != NULL)
                m_pIStream->Release();
        }
        break;

        // Weird to shut down what you didn't open, isn't it?  Allow for
        // error case where dtor shuts down as an afterthought.
        case STGIO_NODATA:
        default:
        return;
    }

    // Free any page map and base data.
    FreePageMap();

    // Reset state values so we don't get confused.
    CtorInit();
}

//*****************************************************************************
// Called to read the data into allocated memory and release the backing store.
//  Only available on read-only data.
//*****************************************************************************
HRESULT
StgIO::LoadFileToMemory()
{
    HRESULT hr;
    void   *pData;          // Allocated buffer for file.
    ULONG   cbData;         // Size of the data.
    ULONG   cbRead = 0;     // Data actually read.

    // Make sure it is a read-only file.
    if (m_fFlags & DBPROP_TMODEF_WRITE)
        return E_INVALIDARG;

    // Try to allocate the buffer.
    cbData = m_cbData;
    pData = AllocateMemory(cbData);
    IfNullGo(pData);

    // Try to read the file into the buffer.
    IfFailGo(Read(pData, cbData, &cbRead));
    if (cbData != cbRead)
    {
        _ASSERTE_MSG(FALSE, "Read didn't succeed.");
        IfFailGo(CLDB_E_FILE_CORRUPT);
    }

    // Done with the old data.
    Close();

    // Open with new data.
    hr = Open(NULL /* szName */, STGIO_READ, pData, cbData, NULL /* IStream* */, NULL /* lpSecurityAttributes */);
    _ASSERTE(SUCCEEDED(hr)); // should not be a failure code path with open on buffer.

    // Mark the new memory so that it will be freed later.
    m_pBaseData = m_pData;
    m_bFreeMem = true;

ErrExit:
    if (FAILED(hr) && pData)
       FreeMemory(pData);

    return hr;
} // StgIO::LoadFileToMemory


//*****************************************************************************
// Read data from the storage source.  This will handle all types of backing
// storage from mmf, streams, and file handles.  No read ahead or MRU
// caching is done.
//*****************************************************************************
HRESULT StgIO::Read(                    // Return code.
    void        *pbBuff,                // Write buffer here.
    ULONG       cbBuff,                 // How much to read.
    ULONG       *pcbRead)               // How much read.
{
    ULONG       cbCopy;                 // For boundary checks.
    void        *pbData;                // Data buffer for mem read.
    HRESULT     hr = S_OK;

    // Validate arguments, don't call if you don't need to.
    _ASSERTE(pbBuff != 0);
    _ASSERTE(cbBuff > 0);

    // Get the data based on type.
    switch (m_iType)
    {
        // For data on file, there are two possibilities:
        // (1) We have an in memory backing store we should use, or
        // (2) We just need to read from the file.
        case STGIO_HFILE:
        case STGIO_HMODULE:
        {
            _ASSERTE((m_hFile != INVALID_HANDLE_VALUE) || (m_hModule != NULL));

            // Backing store does its own paging.
            if (IsBackingStore() || IsMemoryMapped())
            {
                // Force the data into memory.
                if (FAILED(hr = GetPtrForMem(GetCurrentOffset(), cbBuff, pbData)))
                    goto ErrExit;

                // Copy it back for the user and save the size.
                memcpy(pbBuff, pbData, cbBuff);
                if (pcbRead)
                    *pcbRead = cbBuff;
            }
            // If there is no backing store, this is just a read operation.
            else
            {
                _ASSERTE((m_iType == STGIO_HFILE) && (m_hFile != INVALID_HANDLE_VALUE));
                _ASSERTE(m_hModule == NULL);

                ULONG   cbTemp = 0;
                if (!pcbRead)
                    pcbRead = &cbTemp;
                hr = ReadFromDisk(pbBuff, cbBuff, pcbRead);
                m_cbOffset += *pcbRead;
            }
        }
        break;

        // Data in a stream is always just read.
        case STGIO_STREAM:
        {
            _ASSERTE((IStream *) m_pIStream);
            if (!pcbRead)
                pcbRead = &cbCopy;
            *pcbRead = 0;
            hr = m_pIStream->Read(pbBuff, cbBuff, pcbRead);
            if (SUCCEEDED(hr))
                m_cbOffset += *pcbRead;
        }
        break;

        // Simply copy the data from our data.
        case STGIO_MEM:
        case STGIO_SHAREDMEM:
        case STGIO_HFILEMEM:
        {
            _ASSERTE(m_pData && m_cbData);

            // Check for read past end of buffer and adjust.
            if (GetCurrentOffset() + cbBuff > m_cbData)
                cbCopy = m_cbData - GetCurrentOffset();
            else
                cbCopy = cbBuff;

            // Copy the data into the callers buffer.
            memcpy(pbBuff, (void *) ((DWORD_PTR)m_pData + GetCurrentOffset()), cbCopy);
            if (pcbRead)
                *pcbRead = cbCopy;

            // Save a logical offset.
            m_cbOffset += cbCopy;
        }
        break;

        case STGIO_NODATA:
        default:
        _ASSERTE(0);
        break;
    }

ErrExit:
    return (hr);
}


//*****************************************************************************
// Write to disk.  This function will cache up to a page of data in a buffer
// and peridocially flush it on overflow and explicit request.  This makes it
// safe to do lots of small writes without too much performance overhead.
//*****************************************************************************
HRESULT StgIO::Write(                   // true/false.
    const void  *pbBuff,                // Data to write.
    ULONG       cbWrite,                // How much data to write.
    ULONG       *pcbWritten)            // How much did get written.
{
    ULONG       cbWriteIn=cbWrite;      // Track amount written.
    ULONG       cbCopy;
    HRESULT     hr = S_OK;

    _ASSERTE(m_rgBuff != 0);
    _ASSERTE(cbWrite);

    while (cbWrite)
    {
        // In the case where the buffer is already huge, write the whole thing
        // and avoid the cache.
        if (m_cbBuff == 0 && cbWrite >= (ULONG) m_iPageSize)
        {
            if (SUCCEEDED(hr = WriteToDisk(pbBuff, cbWrite, pcbWritten)))
                m_cbOffset += cbWrite;
            break;
        }
        // Otherwise cache as much as we can and flush.
        else
        {
            // Determine how much data goes into the cache buffer.
            cbCopy = m_iPageSize - m_cbBuff;
            cbCopy = min(cbCopy, cbWrite);

            // Copy the data into the cache and adjust counts.
            memcpy(&m_rgBuff[m_cbBuff], pbBuff, cbCopy);
            pbBuff = (void *) ((DWORD_PTR)pbBuff + cbCopy);
            m_cbBuff += cbCopy;
            m_cbOffset += cbCopy;
            cbWrite -= cbCopy;

            // If there is enough data, then flush it to disk and reset count.
            if (m_cbBuff >= (ULONG) m_iPageSize)
            {
                if (FAILED(hr = FlushCache()))
                    break;
            }
        }
    }

    // Return value for caller.
    if (SUCCEEDED(hr) && pcbWritten)
        *pcbWritten = cbWriteIn;
    return (hr);
}


//*****************************************************************************
// Moves the file pointer to the new location.  This handles the different
// types of storage systems.
//*****************************************************************************
HRESULT StgIO::Seek(                    // New offset.
    int        lVal,                   // How much to move.
    ULONG       fMoveType)              // Direction, use Win32 FILE_xxxx.
{
    ULONG       cbRtn = 0;
    HRESULT     hr = NOERROR;

    _ASSERTE(fMoveType >= FILE_BEGIN && fMoveType <= FILE_END);

    // Action taken depends on type of storage.
    switch (m_iType)
    {
        case STGIO_HFILE:
        {
            // Use the file system's move.
            _ASSERTE(m_hFile != INVALID_HANDLE_VALUE);
            cbRtn = ::SetFilePointer(m_hFile, lVal, 0, fMoveType);

            // Save the location redundantly.
            if (cbRtn != 0xffffffff)
            {
                // make sure that m_cbOffset will stay within range
                if (cbRtn > m_cbData || cbRtn < 0)
                {
                    IfFailGo(STG_E_INVALIDFUNCTION);
                }
                m_cbOffset = cbRtn;
            }
        }
        break;

        case STGIO_STREAM:
        {
            LARGE_INTEGER   iMove;
            ULARGE_INTEGER  iNewLoc;

            // Need a 64-bit int.
            iMove.QuadPart = lVal;

            // The move types are named differently, but have same value.
            if (FAILED(hr = m_pIStream->Seek(iMove, fMoveType, &iNewLoc)))
                return (hr);

            // make sure that m_cbOffset will stay within range
            if (iNewLoc.u.LowPart > m_cbData || iNewLoc.u.LowPart < 0)
                IfFailGo(STG_E_INVALIDFUNCTION);

            // Save off only out location.
            m_cbOffset = iNewLoc.u.LowPart;
        }
        break;

        case STGIO_MEM:
        case STGIO_SHAREDMEM:
        case STGIO_HFILEMEM:
        case STGIO_HMODULE:
        {
            // We own the offset, so change our value.
            switch (fMoveType)
            {
                case FILE_BEGIN:

                // make sure that m_cbOffset will stay within range
                if ((ULONG) lVal > m_cbData || lVal < 0)
                {
                    IfFailGo(STG_E_INVALIDFUNCTION);
                }
                m_cbOffset = lVal;
                break;

                case FILE_CURRENT:

                // make sure that m_cbOffset will stay within range
                if (m_cbOffset + lVal > m_cbData)
                {
                    IfFailGo(STG_E_INVALIDFUNCTION);
                }
                m_cbOffset = m_cbOffset + lVal;
                break;

                case FILE_END:
                _ASSERTE(lVal < (LONG) m_cbData);
                // make sure that m_cbOffset will stay within range
                if (m_cbData + lVal > m_cbData)
                {
                    IfFailGo(STG_E_INVALIDFUNCTION);
                }
                m_cbOffset = m_cbData + lVal;
                break;
            }

            cbRtn = m_cbOffset;
        }
        break;

        // Weird to seek with no data.
        case STGIO_NODATA:
        default:
        _ASSERTE(0);
        break;
    }

ErrExit:
    return hr;
}


//*****************************************************************************
// Retrieves the current offset for the storage being used.  This value is
// tracked based on Read, Write, and Seek operations.
//*****************************************************************************
ULONG StgIO::GetCurrentOffset()         // Current offset.
{
    return (m_cbOffset);
}


//*****************************************************************************
// Map the file contents to a memory mapped file and return a pointer to the
// data.  For read/write with a backing store, map the file using an internal
// paging system.
//*****************************************************************************
HRESULT StgIO::MapFileToMem(            // Return code.
    void        *&ptr,                  // Return pointer to file data.
    ULONG       *pcbSize,               // Return size of data.
    LPSECURITY_ATTRIBUTES pAttributes)  // Security token.
{
    char        rcShared[MAXSHMEM];     // ANSI version of shared name.
    HRESULT     hr = S_OK;

    // Don't penalize for multiple calls.  Also, allow calls for mem type so
    // callers don't need to do so much checking.
    if (IsBackingStore() ||
        IsMemoryMapped() ||
        (m_iType == STGIO_MEM) ||
        (m_iType == STGIO_SHAREDMEM) ||
        (m_iType == STGIO_HFILEMEM))
    {
        ptr = m_pData;
        if (pcbSize)
            *pcbSize = m_cbData;
        return (S_OK);
    }

    //#CopySmallFiles
    // Check the size of the data we want to map.  If it is small enough, then
    // simply allocate a chunk of memory from a finer grained heap.  This saves
    // virtual memory space, page table entries, and should reduce overall working set.
    // Also, open for read/write needs a full backing store.
    if ((m_cbData <= SMALL_ALLOC_MAP_SIZE) && (SMALL_ALLOC_MAP_SIZE > 0))
    {
        DWORD cbRead = m_cbData;
        _ASSERTE(m_pData == 0);

        // Just malloc a chunk of data to use.
        m_pBaseData = m_pData = AllocateMemory(m_cbData);
        if (!m_pData)
        {
            hr = OutOfMemory();
            goto ErrExit;
        }

        // Read all of the file contents into this piece of memory.
        IfFailGo( Seek(0, FILE_BEGIN) );
        if (FAILED(hr = Read(m_pData, cbRead, &cbRead)))
        {
            FreeMemory(m_pData);
            m_pData = 0;
            goto ErrExit;
        }
        _ASSERTE(cbRead == m_cbData);

        // If the file isn't being opened for exclusive mode, then free it.
        // If it is for exclusive, then we need to keep the handle open so the
        // file is locked, preventing other readers.  Also leave it open if
        // in read/write mode so we can truncate and rewrite.
        if (m_hFile == INVALID_HANDLE_VALUE ||
            ((m_fFlags & DBPROP_TMODEF_EXCLUSIVE) == 0 && (m_fFlags & DBPROP_TMODEF_WRITE) == 0))
        {
            // If there was a handle open, then free it.
            if (m_hFile != INVALID_HANDLE_VALUE)
            {
                VERIFY(CloseHandle(m_hFile));
                m_hFile = INVALID_HANDLE_VALUE;
            }
            // Free the stream pointer.
            else
            if (m_pIStream != 0)
            {
                m_pIStream->Release();
                m_pIStream = 0;
            }

            // Switch the type to memory only access.
            m_iType = STGIO_MEM;
        }
        else
            m_iType = STGIO_HFILEMEM;

        // Free the memory when we shut down.
        m_bFreeMem = true;
    }
    // Finally, a real mapping file must be created.
    else
    {
        // Now we will map, so better have it right.
        _ASSERTE(m_hFile != INVALID_HANDLE_VALUE || m_iType == STGIO_STREAM);
        _ASSERTE(m_rgPageMap == 0);

        // For read mode, use a memory mapped file since the size will never
        // change for the life of the handle.
        if ((m_fFlags & DBPROP_TMODEF_WRITE) == 0 && m_iType != STGIO_STREAM)
        {
            // Create a mapping object for the file.
            _ASSERTE(m_hMapping == 0);

            DWORD dwProtectionFlags = PAGE_READONLY;

            if ((m_hMapping = WszCreateFileMapping(m_hFile, pAttributes, dwProtectionFlags,
                0, 0, nullptr)) == 0)
            {
                return (MapFileError(GetLastError()));
            }
            m_mtMappedType = MTYPE_FLAT;
            // Check to see if the memory already exists, in which case we have
            // no guarantees it is the right piece of data.
            if (GetLastError() == ERROR_ALREADY_EXISTS)
            {
                hr = PostError(CLDB_E_SMDUPLICATE, rcShared);
                goto ErrExit;
            }

            // Now map the file into memory so we can read from pointer access.
            // <REVISIT_TODO>Note: Added a check for IsBadReadPtr per the Services team which
            // indicates that under some conditions this API can give you back
            // a totally bogus pointer.</REVISIT_TODO>
            if ((m_pBaseData = m_pData = MapViewOfFile(m_hMapping, FILE_MAP_READ,
                        0, 0, 0)) == 0)
            {
                hr = MapFileError(GetLastError());
                if (SUCCEEDED(hr))
                {
                    _ASSERTE_MSG(FALSE, "Error code doesn't indicate error.");
                    hr = PostError(CLDB_E_FILE_CORRUPT);
                }

                // In case we got back a bogus pointer.
                m_pBaseData = m_pData = NULL;
                goto ErrExit;
            }
        }
        // In write mode, we need the hybrid combination of being able to back up
        // the data in memory via cache, but then later rewrite the contents and
        // throw away our cached copy.  Memory mapped files are not good for this
        // case due to poor write characteristics.
        else
        {
            ULONG iMaxSize;         // How much memory required for file.

            // Figure out how many pages we'll require, round up actual data
            // size to page size.
            iMaxSize = (((m_cbData - 1) & ~(m_iPageSize - 1)) + m_iPageSize);
            // Check integer overflow in previous statement
            if (iMaxSize < m_cbData)
            {
                IfFailGo(PostError(COR_E_OVERFLOW));
            }

            // Allocate a bit vector to track loaded pages.
            if ((m_rgPageMap = new (nothrow) BYTE[iMaxSize / m_iPageSize]) == 0)
                return (PostError(OutOfMemory()));
            memset(m_rgPageMap, 0, sizeof(BYTE) * (iMaxSize / m_iPageSize));

            // Allocate space for the file contents.
            if ((m_pBaseData = m_pData = ::ClrVirtualAlloc(0, iMaxSize, MEM_RESERVE, PAGE_NOACCESS)) == 0)
            {
                hr = PostError(OutOfMemory());
                goto ErrExit;
            }
        }
    }

    // Reset any changes made by mapping.
    IfFailGo( Seek(0, FILE_BEGIN) );

ErrExit:

    // Check for errors and clean up.
    if (FAILED(hr))
    {
        if (m_hMapping)
            CloseHandle(m_hMapping);
        m_hMapping = 0;
        m_pBaseData = m_pData = 0;
        m_cbData = 0;
    }
    ptr = m_pData;
    if (pcbSize)
        *pcbSize = m_cbData;
    return (hr);
}


//*****************************************************************************
// Free the mapping object for shared memory but keep the rest of the internal
// state intact.
//*****************************************************************************
HRESULT StgIO::ReleaseMappingObject()   // Return code.
{
    // Check type first.
    if (m_iType != STGIO_SHAREDMEM)
    {
        _ASSERTE(FALSE);
        return S_OK;
    }

    // Must have an allocated handle.
    _ASSERTE(m_hMapping != 0);

    // Freeing the mapping object doesn't do any good if you still have the file.
    _ASSERTE(m_hFile == INVALID_HANDLE_VALUE);

    // Unmap the memory we allocated before freeing the handle.  But keep the
    // memory address intact.
    if (m_pData)
        VERIFY(UnmapViewOfFile(m_pData));

    // Free the handle.
    if (m_hMapping != 0)
    {
        VERIFY(CloseHandle(m_hMapping));
        m_hMapping = 0;
    }
    return S_OK;
}



//*****************************************************************************
// Resets the logical base address and size to the value given.  This is for
// cases like finding a section embedded in another format, like the .clb inside
// of an image.  GetPtrForMem, Read, and Seek will then behave as though only
// data from pbStart to cbSize is valid.
//*****************************************************************************
HRESULT StgIO::SetBaseRange(            // Return code.
    void        *pbStart,               // Start of file data.
    ULONG       cbSize)                 // How big is the range.
{
    if (m_iType == STGIO_SHAREDMEM)
    {
        // The base range must be inside of the current range.
        _ASSERTE((m_pBaseData != NULL) && (m_cbData != 0));
        _ASSERTE(((LONG_PTR) pbStart >= (LONG_PTR) m_pBaseData));
        _ASSERTE(((LONG_PTR) pbStart + cbSize <= (LONG_PTR) m_pBaseData + m_cbData));
    }

    // Save the base range per user request.
    m_pData = pbStart;
    m_cbData = cbSize;
    return S_OK;
}


//*****************************************************************************
// Caller wants a pointer to a chunk of the file.  This function will make sure
// that the memory for that chunk has been committed and will load from the
// file if required.  This algorithm attempts to load no more data from disk
// than is necessary.  It walks the required pages from lowest to highest,
// and for each block of unloaded pages, the memory is committed and the data
// is read from disk.  If all pages are unloaded, all of them are loaded at
// once to speed throughput from disk.
//*****************************************************************************
HRESULT StgIO::GetPtrForMem(            // Return code.
    ULONG       cbStart,                // Where to start getting memory.
    ULONG       cbSize,                 // How much data.
    void        *&ptr)                  // Return pointer to memory here.
{
    int         iFirst, iLast;          // First and last page required.
    ULONG       iOffset, iSize;         // For committing ranges of memory.
    int         i, j;                   // Loop control.
    HRESULT     hr;

    // We need either memory (mmf or user supplied) or a backing store to
    // return a pointer.  Call Read if you don't have these.
    if (!IsBackingStore() && m_pData == 0)
        return (PostError(BadError(E_UNEXPECTED)));

    // Validate the caller isn't asking for a data value out of range.
    if (!(ClrSafeInt<ULONG>::addition(cbStart, cbSize, iOffset)
          && (iOffset <= m_cbData)))
        return (PostError(E_INVALIDARG));

    // This code will check for pages that need to be paged from disk in
    // order for us to return a pointer to that memory.
    if (IsBackingStore())
    {
        // Backing store is bogus when in rewrite mode.
        if (m_bRewrite)
            return (PostError(BadError(E_UNEXPECTED)));

        // Must have the page map to continue.
        _ASSERTE(m_rgPageMap && m_iPageSize && m_pData);

        // Figure out the first and last page that are required for commit.
        iFirst = cbStart / m_iPageSize;
        iLast = (cbStart + cbSize - 1) / m_iPageSize;

        // Avoid confusion.
        ptr = 0;

        // Do a smart load of every page required.  Do not reload pages that have
        // already been brought in from disk.
        //<REVISIT_TODO>@FUTURE: add an optimization so that when all pages have been faulted, we no
        // longer to a page by page search.</REVISIT_TODO>
        for (i=iFirst;  i<=iLast;  )
        {
            // Find the first page that hasn't already been loaded.
            while (GetBit(m_rgPageMap, i) && i<=iLast)
                ++i;
            if (i > iLast)
                break;

            // Offset for first thing to load.
            iOffset = i * m_iPageSize;
            iSize = 0;

            // See how many in a row have not been loaded.
            for (j=i;  i<=iLast && !GetBit(m_rgPageMap, i);  i++)
            {
                // Safe: iSize += m_iPageSize;
                if (!(ClrSafeInt<ULONG>::addition(iSize, m_iPageSize, iSize)))
                {
                    return PostError(E_INVALIDARG);
                }
            }

            // First commit the memory for this part of the file.
            if (::ClrVirtualAlloc((void *) ((DWORD_PTR) m_pData + iOffset),
                    iSize, MEM_COMMIT, PAGE_READWRITE) == 0)
                return (PostError(OutOfMemory()));

            // Now load that portion of the file from disk.
            if (FAILED(hr = Seek(iOffset, FILE_BEGIN)) ||
                FAILED(hr = ReadFromDisk((void *) ((DWORD_PTR) m_pData + iOffset), iSize, 0)))
            {
                return (hr);
            }

            // Change the memory to read only to avoid any modifications.  Any faults
            // that occur indicate a bug whereby the engine is trying to write to
            // protected memory.
            _ASSERTE(::ClrVirtualAlloc((void *) ((DWORD_PTR) m_pData + iOffset),
                    iSize, MEM_COMMIT, PAGE_READONLY) != 0);

            // Record each new loaded page.
            for (;  j<i;  j++)
                SetBit(m_rgPageMap, j, true);
        }

        // Everything was brought into memory, so now return pointer to caller.
        ptr = (void *) ((DWORD_PTR) m_pData + cbStart);
    }
    // Memory version or memory mapped file work the same way.
    else if (IsMemoryMapped() ||
             (m_iType == STGIO_MEM) ||
             (m_iType == STGIO_SHAREDMEM) ||
             (m_iType == STGIO_HFILEMEM))
    {
        if (!(cbStart <= m_cbData))
            return (PostError(E_INVALIDARG));

        ptr = (void *) ((DWORD_PTR) m_pData + cbStart);
    }
    // What's left?!  Add some defense.
    else
    {
        _ASSERTE(0);
        ptr = 0;
        return (PostError(BadError(E_UNEXPECTED)));
    }
    return (S_OK);
}


//*****************************************************************************
// For cached writes, flush the cache to the data store.
//*****************************************************************************
HRESULT StgIO::FlushCache()
{
    ULONG       cbWritten;
    HRESULT     hr;

    if (m_cbBuff)
    {
        if (FAILED(hr = WriteToDisk(m_rgBuff, m_cbBuff, &cbWritten)))
            return (hr);
        m_cbBuff = 0;
    }
    return (S_OK);
}

//*****************************************************************************
// Tells the file system to flush any cached data it may have.  This is
// expensive, but if successful guarantees you won't lose writes short of
// a disk failure.
//*****************************************************************************
HRESULT StgIO::FlushFileBuffers()
{
    _ASSERTE(!IsReadOnly());

    if (m_hFile != INVALID_HANDLE_VALUE)
    {
        if (::FlushFileBuffers(m_hFile))
            return (S_OK);
        else
            return (MapFileError(GetLastError()));
    }
    return (S_OK);
}


//*****************************************************************************
// Called after a successful rewrite of an existing file.  The in memory
// backing store is no longer valid because all new data is in memory and
// on disk.  This is essentially the same state as created, so free up some
// working set and remember this state.
//*****************************************************************************
HRESULT StgIO::ResetBackingStore()      // Return code.
{
    // Don't be calling this function for read only data.
    _ASSERTE(!IsReadOnly());

    // Free up any backing store data we no longer need now that everything
    // is in memory.
    FreePageMap();
    return (S_OK);
}


//
// Private.
//



//*****************************************************************************
// This version will force the data in cache out to disk for real.  The code
// can handle the different types of storage we might be sitting on based on
// the open type.
//*****************************************************************************
HRESULT StgIO::WriteToDisk(             // Return code.
    const void  *pbBuff,                // Buffer to write.
    ULONG       cbWrite,                // How much.
    ULONG       *pcbWritten)            // Return how much written.
{
    ULONG       cbWritten;              // Buffer for write funcs.
    HRESULT     hr = S_OK;

    // Pretty obvious.
    _ASSERTE(!IsReadOnly());

    // Always need a buffer to write this data to.
    if (!pcbWritten)
        pcbWritten = &cbWritten;

    // Action taken depends on type of storage.
    switch (m_iType)
    {
        case STGIO_HFILE:
        case STGIO_HFILEMEM:
        {
            // Use the file system's move.
            _ASSERTE(m_hFile != INVALID_HANDLE_VALUE);

            // Do the write to disk.
            if (!::WriteFile(m_hFile, pbBuff, cbWrite, pcbWritten, 0))
                hr = MapFileError(GetLastError());
        }
        break;

        // Free the stream pointer.
        case STGIO_STREAM:
        {
            // Delegate write to stream code.
            hr = m_pIStream->Write(pbBuff, cbWrite, pcbWritten);
        }
        break;

        // We cannot write to fixed read/only memory or LoadLibrary module.
        case STGIO_HMODULE:
        case STGIO_MEM:
        case STGIO_SHAREDMEM:
        _ASSERTE(0);
        hr = BadError(E_UNEXPECTED);
        break;

        // Weird to seek with no data.
        case STGIO_NODATA:
        default:
        _ASSERTE(0);
        break;
    }
    return (hr);
}


//*****************************************************************************
// This version only reads from disk.
//*****************************************************************************
HRESULT StgIO::ReadFromDisk(            // Return code.
    void        *pbBuff,                // Write buffer here.
    ULONG       cbBuff,                 // How much to read.
    ULONG       *pcbRead)               // How much read.
{
    ULONG       cbRead;

    _ASSERTE(m_iType == STGIO_HFILE || m_iType == STGIO_STREAM);

    // Need to have a buffer.
    if (!pcbRead)
        pcbRead = &cbRead;

    // Read only from file to avoid recursive logic.
    if (m_iType == STGIO_HFILE || m_iType == STGIO_HFILEMEM)
    {
        if (::ReadFile(m_hFile, pbBuff, cbBuff, pcbRead, 0))
            return (S_OK);
        return (MapFileError(GetLastError()));
    }
    // Read directly from stream.
    else
    {
        return (m_pIStream->Read(pbBuff, cbBuff, pcbRead));
    }
}


//*****************************************************************************
// Copy the contents of the file for this storage to the target path.
//*****************************************************************************
HRESULT StgIO::CopyFileInternal(        // Return code.
    LPCWSTR     szTo,                   // Target save path for file.
    int         bFailIfThere,           // true to fail if target exists.
    int         bWriteThrough)          // Should copy be written through OS cache.
{
    DWORD       iCurrent;               // Save original location.
    DWORD       cbRead;                 // Byte count for buffer.
    DWORD       cbWrite;                // Check write of bytes.
    const DWORD cbBuff = 4096;          // Size of buffer for copy (in bytes).
    BYTE       *pBuff = (BYTE*)alloca(cbBuff); // Buffer for copy.
    HANDLE      hFile;                  // Target file.
    HRESULT     hr = S_OK;

    // Create target file.
    if ((hFile = ::WszCreateFile(szTo, GENERIC_WRITE, 0, 0,
            (bFailIfThere) ? CREATE_NEW : CREATE_ALWAYS,
            (bWriteThrough) ? FILE_FLAG_WRITE_THROUGH : 0,
            0)) == INVALID_HANDLE_VALUE)
    {
        return (MapFileError(GetLastError()));
    }

    // Save current location and reset it later.
    iCurrent = ::SetFilePointer(m_hFile, 0, 0, FILE_CURRENT);
    ::SetFilePointer(m_hFile, 0, 0, FILE_BEGIN);

    // Copy while there are bytes.
    while (::ReadFile(m_hFile, pBuff, cbBuff, &cbRead, 0) && cbRead)
    {
        if (!::WriteFile(hFile, pBuff, cbRead, &cbWrite, 0) || cbWrite != cbRead)
        {
            hr = STG_E_WRITEFAULT;
            break;
        }
    }

    // Reset file offset.
    ::SetFilePointer(m_hFile, iCurrent, 0, FILE_BEGIN);

    // Close target.
    if (!bWriteThrough)
        VERIFY(::FlushFileBuffers(hFile));
    ::CloseHandle(hFile);
    return (hr);
}


//*****************************************************************************
// Free the data used for backing store from disk in read/write scenario.
//*****************************************************************************
void StgIO::FreePageMap()
{
    // If a small file was allocated, then free that memory.
    if (m_bFreeMem && m_pBaseData)
        FreeMemory(m_pBaseData);
    // For mmf, close handles and free resources.
    else if (m_hMapping && m_pBaseData)
    {
        VERIFY(UnmapViewOfFile(m_pBaseData));
        VERIFY(CloseHandle(m_hMapping));
    }
    // For our own system, free memory.
    else if (m_rgPageMap && m_pBaseData)
    {
        delete [] m_rgPageMap;
        m_rgPageMap = 0;
        VERIFY(::ClrVirtualFree(m_pBaseData, (((m_cbData - 1) & ~(m_iPageSize - 1)) + m_iPageSize), MEM_DECOMMIT));
        VERIFY(::ClrVirtualFree(m_pBaseData, 0, MEM_RELEASE));
        m_pBaseData = 0;
        m_cbData = 0;
    }

    m_pBaseData = 0;
    m_hMapping = 0;
    m_cbData = 0;
}


//*****************************************************************************
// Check the given pointer and ensure it is aligned correct.  Return true
// if it is aligned, false if it is not.
//*****************************************************************************
int StgIO::IsAlignedPtr(ULONG_PTR Value, int iAlignment)
{
    HRESULT     hr;
    void        *ptrStart = NULL;

    if ((m_iType == STGIO_STREAM) ||
        (m_iType == STGIO_SHAREDMEM) ||
        (m_iType == STGIO_MEM))
    {
        return ((Value - (ULONG_PTR) m_pData) % iAlignment == 0);
    }
    else
    {
        hr = GetPtrForMem(0, 1, ptrStart);
        _ASSERTE(hr == S_OK && "GetPtrForMem failed");
        _ASSERTE(Value > (ULONG_PTR) ptrStart);
        return (((Value - (ULONG_PTR) ptrStart) % iAlignment) == 0);
    }
} // int StgIO::IsAlignedPtr()





//*****************************************************************************
// These helper functions are used to allocate fairly large pieces of memory,
// more than should be taken from the runtime heap, but less that would require
// virtual memory overhead.
//*****************************************************************************
// #define _TRACE_MEM_ 1

void *AllocateMemory(int iSize)
{
    void * ptr;
    ptr = new (nothrow) BYTE[iSize];

#if defined(_DEBUG) && defined(_TRACE_MEM_)
    static int i=0;
    printf("AllocateMemory: (%d) 0x%p, size %d\n", ++i, ptr, iSize);
#endif
    return (ptr);
}


void FreeMemory(void *pbData)
{
#if defined(_DEBUG) && defined(_TRACE_MEM_)
    static int i=0;
    printf("FreeMemory: (%d) 0x%p\n", ++i, pbData);
#endif

    _ASSERTE(pbData);
    delete [] (BYTE *) pbData;
}

