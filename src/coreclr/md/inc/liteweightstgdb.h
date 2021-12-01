// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// LiteWeightStgdb.h
//

//
// This contains definition of class CLiteWeightStgDB. This is light weight
// read-only implementation for accessing compressed meta data format.
//
//*****************************************************************************
#ifndef __LiteWeightStgdb_h__
#define __LiteWeightStgdb_h__

#include "metadata.h"
#include "metamodelro.h"
#include "metamodelrw.h"

#include "stgtiggerstorage.h"

class StgIO;

#include "mdcommon.h"
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
#include "pdbheap.h"
#endif

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:28718)    // public header missing SAL annotations
#endif // _PREFAST_
class TiggerStorage;
#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_

//*****************************************************************************
// This class provides common definitions for heap segments.  It is both the
//  base class for the heap, and the class for heap extensions (additional
//  memory that must be allocated to grow the heap).
//*****************************************************************************
template <class MiniMd>
class CLiteWeightStgdb
{
    friend class VerifyLayoutsMD;
public:
    CLiteWeightStgdb() : m_pvMd(NULL), m_cbMd(0)
    {}

    ~CLiteWeightStgdb()
    { Uninit(); }

    // open an in-memory metadata section for read.
    __checkReturn
    HRESULT InitOnMem(
        ULONG cbData,
        LPCVOID pbData);

    void Uninit();

protected:
    MiniMd      m_MiniMd;               // embedded compress meta data schemas definition
    const void  *m_pvMd;                // Pointer to meta data.
    ULONG       m_cbMd;                 // Size of the meta data.

    friend class CorMetaDataScope;
    friend class COR;
    friend class RegMeta;
    friend class MDInternalRO;
    friend class MDInternalRW;
};

//*****************************************************************************
// Open an in-memory metadata section for read
//*****************************************************************************
template <class MiniMd>
void CLiteWeightStgdb<MiniMd>::Uninit()
{
    m_MiniMd.m_StringHeap.Delete();
    m_MiniMd.m_UserStringHeap.Delete();
    m_MiniMd.m_GuidHeap.Delete();
    m_MiniMd.m_BlobHeap.Delete();
    m_pvMd = NULL;
    m_cbMd = 0;
}

class CLiteWeightStgdbRW : public CLiteWeightStgdb<CMiniMdRW>
{
    friend class CImportTlb;
    friend class RegMeta;
    friend class VerifyLayoutsMD;
    friend HRESULT TranslateSigHelper(
            IMDInternalImport*      pImport,
            IMDInternalImport*      pAssemImport,
            const void*             pbHashValue,
            ULONG                   cbHashValue,
            PCCOR_SIGNATURE         pbSigBlob,
            ULONG                   cbSigBlob,
            IMetaDataAssemblyEmit*  pAssemEmit,
            IMetaDataEmit*          emit,
            CQuickBytes*            pqkSigEmit,
            ULONG*                  pcbSig);
public:
    CLiteWeightStgdbRW() : m_cbSaveSize(0), m_pStreamList(0), m_pNextStgdb(NULL), m_pStgIO(NULL)
    {
        m_wszFileName = NULL;
        m_pImage = NULL;
        m_dwImageSize = 0;
        m_dwPEKind = (DWORD)(-1);
        m_dwDatabaseLFS = 0;
        m_dwDatabaseLFT = 0;
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
        m_pPdbHeap = NULL;
#endif
    }
    ~CLiteWeightStgdbRW();

    __checkReturn
    HRESULT InitNew();

    // open an in-memory metadata section for read.
    __checkReturn
    HRESULT InitOnMem(
        ULONG   cbData,
        LPCVOID pbData,
        int     bReadOnly);

    __checkReturn
    HRESULT GetSaveSize(
        CorSaveSize               fSize,
        UINT32                   *pcbSaveSize,
        MetaDataReorderingOptions reorderingOptions = NoReordering,
        CorProfileData           *pProfileData = NULL); // optional IBC profile data for working set optimization

    __checkReturn
    HRESULT SaveToStream(
        IStream                  *pIStream,                 // Stream to which to write
        MetaDataReorderingOptions reorderingOptions = NoReordering,
        CorProfileData           *pProfileData = NULL);     // optional IBC profile data for working set optimization

    __checkReturn
    HRESULT Save(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags);

    // Open a metadata section for read/write
    __checkReturn
    HRESULT OpenForRead(
        LPCWSTR     szDatabase,             // Name of database.
        void        *pbData,                // Data to open on top of, 0 default.
        ULONG       cbData,                 // How big is the data.
        DWORD       dwFlags);               // Flags for the open.

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    // Open a metadata section for read/write
    __checkReturn
    HRESULT OpenForRead(
        IMDCustomDataSource *pDataSource,   // data to open on top of
        DWORD       dwFlags);               // Flags for the open.
#endif

    __checkReturn
    HRESULT FindImageMetaData(
        PVOID pImage,                       // Pointer to head of a file
        DWORD dwFileLength,                 // length of a flat file
        BOOL  bMappedImage,                 // Is the file mapped
        PVOID *ppMetaData,                  // [out] pointer to the metadata
        ULONG *pcbMetaData);                // [out] size of the metadata

    __checkReturn
    HRESULT FindObjMetaData(
        PVOID pImage,                       // Pointer to an OBJ file
        DWORD dwFileLength,                 // Length of the file
        PVOID *ppMetaData,                  // [out] pointer to the metadata
        ULONG *pcbMetaData);                // [out] size of the metadata

    __checkReturn
    HRESULT GetPEKind(  					// S_OK or error.
        MAPPINGTYPE mtMapping,              // The type of mapping the image has
        DWORD* pdwPEKind,                   // [OUT] The kind of PE (0 - not a PE)
        DWORD* pdwMachine);                 // [OUT] Machine as defined in NT header

    // Low level data access; not useful for most clients.
    __checkReturn
    HRESULT GetRawData(
        const void **ppvMd,                 // [OUT] put pointer to MD section here (aka, 'BSJB').
        ULONG   *pcbMd);                    // [OUT] put size of the stream here.

    __checkReturn
    STDMETHODIMP GetRawStreamInfo(          // Get info about the MD stream.
        ULONG   ix,                         // [IN] Stream ordinal desired.
        const char **ppchName,              // [OUT] put pointer to stream name here.
        const void **ppv,                   // [OUT] put pointer to MD stream here.
        ULONG   *pcb);                      // [OUT] put size of the stream here.

    DAC_ALIGNAS(CLiteWeightStgdb<CMiniMdRW>) // Align the first member to the alignment of the base class
    UINT32      m_cbSaveSize;               // Size of the saved streams.
    int         m_bSaveCompressed;          // If true, save as compressed stream (#-, not #~)
    VOID*       m_pImage;                   // Set in OpenForRead, NULL for anything but PE files
    DWORD       m_dwImageSize;              // On-disk size of image

protected:
    DWORD       m_dwPEKind;                 // The kind of PE - 0: not a PE.
    DWORD       m_dwMachine;                // Machine as defined in NT header.

    __checkReturn
    HRESULT GetPoolSaveSize(
        LPCWSTR szHeap,                 // Name of the heap stream.
        int     iPool,                  // The pool whose size to get.
        UINT32 *pcbSaveSize);           // Add pool data to this value.

    __checkReturn
    HRESULT GetTablesSaveSize(
        CorSaveSize               fSave,
        UINT32                   *pcbSaveSize,
        MetaDataReorderingOptions reorderingOptions,
        CorProfileData           *pProfileData = NULL); // Add pool data to this value.

    __checkReturn
    HRESULT AddStreamToList(
        UINT32  cbSize,         // Size of the stream data.
        LPCWSTR szName);        // Name of the stream.

    __checkReturn
    HRESULT SaveToStorage(
        TiggerStorage            *pStorage,
        MetaDataReorderingOptions reorderingOptions = NoReordering,
        CorProfileData            *pProfileData = NULL);

    __checkReturn
    HRESULT SavePool(LPCWSTR szName, TiggerStorage *pStorage, int iPool);

    STORAGESTREAMLST *m_pStreamList;

    __checkReturn
    HRESULT InitFileForRead(
        StgIO       *pStgIO,            // For file i/o.
        int         bReadOnly=true);    // If read-only.

    // Set file name of this database (makes copy of the file name).
    __checkReturn HRESULT SetFileName(const WCHAR * wszFileName);
    // Returns TRUE if wszFileName has valid file name length.
    static BOOL IsValidFileNameLength(const WCHAR * wszFileName);

    CLiteWeightStgdbRW *m_pNextStgdb;

public:
    FORCEINLINE FILETYPE GetFileType() { return m_eFileType; }

private:
    FILETYPE m_eFileType;
    WCHAR *  m_wszFileName;     // Database file name (NULL or non-empty string)
    DWORD    m_dwDatabaseLFT;   // Low bytes of the database file's last write time
    DWORD    m_dwDatabaseLFS;   // Low bytes of the database file's size
    StgIO *  m_pStgIO;          // For file i/o.
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    PdbHeap *m_pPdbHeap;
#endif
};  // class CLiteWeightStgdbRW

#endif // __LiteWeightStgdb_h__
