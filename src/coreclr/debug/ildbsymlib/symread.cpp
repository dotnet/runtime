// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: symread.cpp
//

// ===========================================================================
#include "pch.h"
#include "symread.h"
#include "corimage.h"

#define CODE_WITH_NO_SOURCE 0xfeefee
// -------------------------------------------------------------------------
// SymReader class
// -------------------------------------------------------------------------

//-----------------------------------------------------------
// NewSymReader
// Static function used to create a new instance of SymReader
//-----------------------------------------------------------
HRESULT
SymReader::NewSymReader(
    REFCLSID clsid,
    void** ppObj
    )
{
    HRESULT hr = S_OK;
    SymReader* pSymReader = NULL;

    _ASSERTE(IsValidCLSID(clsid));
    _ASSERTE(IsValidWritePtr(ppObj, IUnknown*));

    if (clsid != IID_ISymUnmanagedReader)
        return (E_UNEXPECTED);

    IfFalseGo(ppObj, E_INVALIDARG);

    *ppObj = NULL;
    IfNullGo( pSymReader = NEW(SymReader()));

    *ppObj = pSymReader;
    pSymReader->AddRef();
    pSymReader = NULL;

ErrExit:

    RELEASE( pSymReader );

    return hr;
}


//-----------------------------------------------------------
// ~SymReader
//-----------------------------------------------------------
SymReader::~SymReader()
{
    Cleanup();
}

//-----------------------------------------------------------
// Cleanup
// Release all memory and clear initialized data structures
// (eg. as a result of a failed Initialization attempt)
//-----------------------------------------------------------
void SymReader::Cleanup()
{
    if (m_pDocs)
    {
        unsigned i;
        for(i = 0; i < m_pPDBInfo->m_CountOfDocuments; i++)
        {
            RELEASE(m_pDocs[i]);
        }
    }

    DELETE(m_pPDBInfo);
    m_pPDBInfo = NULL;

    // If we loaded from stream, then free the memory we allocated
    if (m_fInitializeFromStream)
    {
        DELETEARRAY(m_DataPointers.m_pBytes);
        DELETEARRAY(m_DataPointers.m_pConstants);
        DELETEARRAY(m_DataPointers.m_pDocuments);
        DELETEARRAY(m_DataPointers.m_pMethods);
        DELETEARRAY(m_DataPointers.m_pScopes);
        DELETEARRAY(m_DataPointers.m_pSequencePoints);
        DELETEARRAY(m_DataPointers.m_pStringsBytes);
        DELETEARRAY(m_DataPointers.m_pUsings);
        DELETEARRAY(m_DataPointers.m_pVars);
    }

    DELETEARRAY(m_pDocs);
    m_pDocs = NULL;

    RELEASE(m_pImporter);
    m_pImporter = NULL;

    memset(&m_DataPointers, 0, sizeof(PDBDataPointers));
    m_szPath[0] = '\0';
}

//-----------------------------------------------------------
// ~QueryInterface
//-----------------------------------------------------------
HRESULT
SymReader::QueryInterface(
    REFIID riid,
    void **ppvObject
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(IsValidIID(riid));
    _ASSERTE(IsValidWritePtr(ppvObject, void*));

    IfFalseGo(ppvObject, E_INVALIDARG);
    if (riid == IID_ISymUnmanagedReader)
    {
        *ppvObject = (ISymUnmanagedReader*) this;
    }
    else
    if (riid == IID_IUnknown)
    {
        *ppvObject = (IUnknown*)this;
    }
    else
    {
        *ppvObject = NULL;
        hr = E_NOINTERFACE;
    }

    if (*ppvObject)
    {
        AddRef();
    }

ErrExit:

    return hr;
}

static HRESULT ReadFromStream(IStream *pIStream, void *pv, ULONG cb)
{
    HRESULT hr = NOERROR;
    ULONG ulBytesRead;

    IfFailGo(pIStream->Read(pv, cb, &ulBytesRead));
    if (ulBytesRead != cb)
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// Initialize
// Pass in the required information to read in the debug info
// If a stream is passed in, it is used, otherwise a filename
// must be passed in
//-----------------------------------------------------------
HRESULT SymReader::Initialize(
    IUnknown *importer,         // Cash it to be consistent with CLR
    const WCHAR* szFileName,    // File name of the ildb
    const WCHAR* szsearchPath,  // Search Path
    IStream *pIStream           // IStream
    )
{
    HRESULT hr = NOERROR;
    _ASSERTE(szFileName || pIStream);
    IfFalseGo(szFileName || pIStream, E_INVALIDARG );

    _ASSERTE(!m_fInitialized);
    IfFalseGo(!m_fInitialized, E_UNEXPECTED);

    // If it's passed in, we need to AddRef to be consistent the
    // desktop version since ReleaseImporterFromISymUnmanagedReader (ceeload.cpp)
    // assumes there's an addref
    if (importer)
    {
        m_pImporter = importer;
        m_pImporter->AddRef();
    }

    // See if we're reading from a file or stream
    if (pIStream == NULL)
    {
        // We're initializing from a file
        m_fInitializeFromStream = false;
        IfFailGo(InitializeFromFile(szFileName, szsearchPath));
    }
    else
    {
        // We're reading in from a stream
        m_fInitializeFromStream = true;
        IfFailGo(InitializeFromStream(pIStream));
    }

    // Note that up to this point, the data we've read in has not been validated.  Since we don't trust
    // our input, it's important that we don't proceed with using this data until validation has been
    // successful.
    IfFailGo(ValidateData());


ErrExit:
    // If we have not succeeded, then we need to clean up our data structures.  This would allow a client to call
    // Initialize again, but also ensures we can't possibly use partial or otherwise invalid (possibly
    // malicious) data.
    if (FAILED(hr))
    {
        Cleanup();
    }
    else
    {
        // Otherwise we are not properly initialized
        m_fInitialized = true;
    }

    return hr;
}

//-----------------------------------------------------------
// Initialize the data structures by reading from the supplied stream
// Note that upon completion the data has not yet been validated for safety.
//-----------------------------------------------------------
HRESULT SymReader::InitializeFromStream(
    IStream *pIStream           // IStream
    )
{
    GUID GuidVersion;
    BYTE *pSignature;
    HRESULT hr = S_OK;

    // Reset the stream to the beginning
    LARGE_INTEGER li;
    li.u.HighPart = 0;
    li.u.LowPart = 0;

    // Make sure we're at the beginning of the stream
    IfFailGo(pIStream->Seek(li, STREAM_SEEK_SET, NULL));

    IfNullGo(pSignature = (BYTE *)_alloca(ILDB_SIGNATURE_SIZE));
    IfFailGo(ReadFromStream(pIStream, pSignature, ILDB_SIGNATURE_SIZE));

    // Verify that we're looking at an ILDB File
    if (memcmp(pSignature, ILDB_SIGNATURE, ILDB_SIGNATURE_SIZE))
    {
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
    }

    IfFailGo(ReadFromStream(pIStream, &GuidVersion, sizeof(GUID)));

    SwapGuid(&GuidVersion);

    if (memcmp(&GuidVersion, &ILDB_VERSION_GUID, sizeof(GUID)))
    {
        IfFailGo(HrFromWin32(ERROR_INVALID_DATA));
    }

    IfNullGo(m_pPDBInfo = NEW(PDBInfo));

    memset(m_pPDBInfo, 0 , sizeof(PDBInfo));
    IfFailGo(ReadFromStream(pIStream, m_pPDBInfo, sizeof(PDBInfo)));

    // Swap the counts
    m_pPDBInfo->ConvertEndianness();

    if (m_pPDBInfo->m_CountOfConstants)
    {
        IfNullGo(m_DataPointers.m_pConstants = NEW(SymConstant[m_pPDBInfo->m_CountOfConstants]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pConstants, m_pPDBInfo->m_CountOfConstants*sizeof(SymConstant)));
    }

    if (m_pPDBInfo->m_CountOfMethods)
    {
        IfNullGo(m_DataPointers.m_pMethods = NEW(SymMethodInfo[m_pPDBInfo->m_CountOfMethods]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pMethods, m_pPDBInfo->m_CountOfMethods*sizeof(SymMethodInfo)));
    }

    if (m_pPDBInfo->m_CountOfScopes)
    {
        IfNullGo(m_DataPointers.m_pScopes = NEW(SymLexicalScope[m_pPDBInfo->m_CountOfScopes]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pScopes, m_pPDBInfo->m_CountOfScopes*sizeof(SymLexicalScope)));
    }

    if (m_pPDBInfo->m_CountOfVars)
    {
        IfNullGo(m_DataPointers.m_pVars = NEW(SymVariable[m_pPDBInfo->m_CountOfVars]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pVars, m_pPDBInfo->m_CountOfVars*sizeof(SymVariable)));
    }

    if (m_pPDBInfo->m_CountOfUsing)
    {
        IfNullGo(m_DataPointers.m_pUsings = NEW(SymUsingNamespace[m_pPDBInfo->m_CountOfUsing]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pUsings, m_pPDBInfo->m_CountOfUsing*sizeof(SymUsingNamespace)));
    }

    if (m_pPDBInfo->m_CountOfSequencePoints)
    {
        IfNullGo(m_DataPointers.m_pSequencePoints = NEW(SequencePoint[m_pPDBInfo->m_CountOfSequencePoints]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pSequencePoints, m_pPDBInfo->m_CountOfSequencePoints*sizeof(SequencePoint)));
    }

    if (m_pPDBInfo->m_CountOfDocuments)
    {
        IfNullGo(m_DataPointers.m_pDocuments = NEW(DocumentInfo[m_pPDBInfo->m_CountOfDocuments]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pDocuments, m_pPDBInfo->m_CountOfDocuments*sizeof(DocumentInfo)));
    }

    if (m_pPDBInfo->m_CountOfBytes)
    {
        IfNullGo(m_DataPointers.m_pBytes = NEW(BYTE[m_pPDBInfo->m_CountOfBytes]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pBytes, m_pPDBInfo->m_CountOfBytes*sizeof(BYTE)));
    }


    if (m_pPDBInfo->m_CountOfStringBytes)
    {
        IfNullGo(m_DataPointers.m_pStringsBytes = NEW(BYTE[m_pPDBInfo->m_CountOfStringBytes]));
        IfFailGo(ReadFromStream(pIStream, m_DataPointers.m_pStringsBytes, m_pPDBInfo->m_CountOfStringBytes));
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// ValidateData
// Checks the contents of everything in m_DataPointers (i.e. all the structures read from the file)
// to make sure it is valid.  Specifically, validates that all indexes are within bounds for the
// sizes allocated.
//-----------------------------------------------------------
HRESULT SymReader::ValidateData()
{
    HRESULT hr = S_OK;

    for (UINT32 i = 0; i < m_pPDBInfo->m_CountOfConstants; i++)
    {
        SymConstant & c = m_DataPointers.m_pConstants[i];
        IfFalseGo(c.ParentScope() < m_pPDBInfo->m_CountOfScopes, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(c.Name() < m_pPDBInfo->m_CountOfStringBytes, HrFromWin32(ERROR_BAD_FORMAT));
        IfFailGo(ValidateBytes(c.Signature(), c.SignatureSize()));
    }

    for (UINT32 i = 0; i < m_pPDBInfo->m_CountOfMethods; i++)
    {
        // Note that start/end values may equal the count (i.e. point one past the end) because
        // the end is the extent, and start can equal end to indicate no entries.
        SymMethodInfo & m = m_DataPointers.m_pMethods[i];
        IfFalseGo(m.StartScopes() <= m_pPDBInfo->m_CountOfScopes, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.EndScopes() <= m_pPDBInfo->m_CountOfScopes, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartScopes() <= m.EndScopes(), HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartVars() <= m_pPDBInfo->m_CountOfVars, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.EndVars() <= m_pPDBInfo->m_CountOfVars, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartVars() <= m.EndVars(), HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartUsing() <= m_pPDBInfo->m_CountOfUsing, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.EndUsing() <= m_pPDBInfo->m_CountOfUsing, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartUsing() <= m.EndUsing(), HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartConstant() <= m_pPDBInfo->m_CountOfConstants, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.EndConstant() <= m_pPDBInfo->m_CountOfConstants, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartConstant() <= m.EndConstant(), HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartDocuments() <= m_pPDBInfo->m_CountOfDocuments, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.EndDocuments() <= m_pPDBInfo->m_CountOfDocuments, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartDocuments() <= m.EndDocuments(), HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartSequencePoints() <= m_pPDBInfo->m_CountOfSequencePoints, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.EndSequencePoints() <= m_pPDBInfo->m_CountOfSequencePoints, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(m.StartSequencePoints() <= m.EndSequencePoints(), HrFromWin32(ERROR_BAD_FORMAT));
    }

    for (UINT32 i = 0; i < m_pPDBInfo->m_CountOfScopes; i++)
    {
        SymLexicalScope & s = m_DataPointers.m_pScopes[i];
        IfFalseGo((s.ParentScope() == (UINT32)-1) || (s.ParentScope() < m_pPDBInfo->m_CountOfScopes), HrFromWin32(ERROR_BAD_FORMAT));
    }

    for (UINT32 i = 0; i < m_pPDBInfo->m_CountOfVars; i++)
    {
        SymVariable & v = m_DataPointers.m_pVars[i];
        IfFalseGo(v.Scope() < m_pPDBInfo->m_CountOfScopes, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(v.Name() < m_pPDBInfo->m_CountOfStringBytes, HrFromWin32(ERROR_BAD_FORMAT));
        IfFailGo(ValidateBytes(v.Signature(), v.SignatureSize()));
    }

    for (UINT32 i = 0; i < m_pPDBInfo->m_CountOfUsing; i++)
    {
        SymUsingNamespace & u = m_DataPointers.m_pUsings[i];
        IfFalseGo(u.ParentScope() < m_pPDBInfo->m_CountOfScopes, HrFromWin32(ERROR_BAD_FORMAT));
        IfFalseGo(u.Name() < m_pPDBInfo->m_CountOfStringBytes, HrFromWin32(ERROR_BAD_FORMAT));
    }

    for (UINT32 i = 0; i < m_pPDBInfo->m_CountOfSequencePoints; i++)
    {
        SequencePoint & s = m_DataPointers.m_pSequencePoints[i];
        IfFalseGo(s.Document() < m_pPDBInfo->m_CountOfDocuments, HrFromWin32(ERROR_BAD_FORMAT));
    }

    for (UINT32 i = 0; i < m_pPDBInfo->m_CountOfDocuments; i++)
    {
        DocumentInfo & d = m_DataPointers.m_pDocuments[i];
        IfFailGo(ValidateBytes(d.CheckSumEntry(), d.CheckSumSize()));
        IfFailGo(ValidateBytes(d.SourceEntry(), d.SourceSize()));
        IfFalseGo(d.UrlEntry() < m_pPDBInfo->m_CountOfStringBytes, HrFromWin32(ERROR_BAD_FORMAT));
    }

    // Nothing to validate for the m_pBytes array - each reference must validate above that it's
    // length doesn't exceed the array

    // We expect all strings to be null terminated.  To ensure no string operation overruns the buffer
    // it sufficies to check that the buffer ends in a null character
    if (m_pPDBInfo->m_CountOfStringBytes > 0)
    {
        IfFalseGo(m_DataPointers.m_pStringsBytes[m_pPDBInfo->m_CountOfStringBytes-1] == '\0', HrFromWin32(ERROR_BAD_FORMAT));
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// Validate a reference to the bytes array
//-----------------------------------------------------------
HRESULT SymReader::ValidateBytes(UINT32 bytesIndex, UINT32 bytesLen)
{
    S_UINT32 extent = S_UINT32(bytesIndex) + S_UINT32(bytesLen);
    if (!extent.IsOverflow() &&
        (extent.Value() <= m_pPDBInfo->m_CountOfBytes))
    {
        return S_OK;
    }

    return HrFromWin32(ERROR_BAD_FORMAT);
}

//-----------------------------------------------------------
// VerifyPEDebugInfo
// Verify that the debug info in the PE is the one we want
//-----------------------------------------------------------
HRESULT SymReader::VerifyPEDebugInfo(const WCHAR* szFileName)
{
    HRESULT hr = E_FAIL;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hMapFile = INVALID_HANDLE_VALUE;
    BYTE *pMod = NULL;
    DWORD dwFileSize;
    IMAGE_DEBUG_DIRECTORY *pDebugDir;
    RSDSI *pDebugInfo;
    DWORD dwUtf8Length;
    DWORD dwUnicodeLength;

    // We need to change the .pdb extension to .ildb
    // compatible with VS7
    WCHAR fullpath[_MAX_PATH];
    WCHAR drive[_MAX_DRIVE];
    WCHAR dir[_MAX_DIR];
    WCHAR fname[_MAX_FNAME];

    IMAGE_NT_HEADERS*pNT;

    hFile = WszCreateFile(szFileName,
                         GENERIC_READ,
                         FILE_SHARE_READ,
                         NULL,
                         OPEN_EXISTING,
                         FILE_ATTRIBUTE_NORMAL,
                         NULL);

    if (hFile == INVALID_HANDLE_VALUE)
    {
        // Get the last error if we can
        return HrFromWin32(GetLastError());
    }

    dwFileSize = GetFileSize(hFile, NULL);
    if (dwFileSize < ILDB_HEADER_SIZE)
    {
        IfFailGo(HrFromWin32(ERROR_INVALID_DATA));
    }

    hMapFile = WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
    if (hMapFile == NULL)
        IfFailGo(HrFromWin32(GetLastError()));

    pMod = (BYTE *) MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
    if (pMod == NULL)
        IfFailGo(HrFromWin32(GetLastError()));

    pNT = Cor_RtlImageNtHeader(pMod, dwFileSize);

    // If there is no DebugEntry, then just error out
    if (VAL32(pNT->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].VirtualAddress) == 0)
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));

    // NOTE: This code is not secure against malformed PE files - any of the pointer additions below
    // may be outside the range of memory mapped for the file.  If we ever want to use this code
    // on untrusted PE files, we should properly validate everything (probably by using PEDecoder).

    DWORD offset;
    offset = Cor_RtlImageRvaToOffset(pNT, VAL32(pNT->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].VirtualAddress), dwFileSize);
    if (offset == NULL)
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
    pDebugDir = (IMAGE_DEBUG_DIRECTORY *)(pMod + offset);
    pDebugInfo = (RSDSI *)(pMod + VAL32(pDebugDir->PointerToRawData));

    if (pDebugInfo->dwSig != VAL32(0x53445352)) // "SDSR"
    {
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
    }


    // Try the returned Stored Name since it might be a fully qualified path
    dwUtf8Length = VAL32(pDebugDir->SizeOfData) - sizeof(RSDSI);
    dwUnicodeLength = MultiByteToWideChar(CP_UTF8, 0, (LPCSTR) pDebugInfo->szPDB, dwUtf8Length, fullpath, COUNTOF(fullpath) - 1);

    // Make sure it's NULL terminated
    _ASSERTE(dwUnicodeLength < COUNTOF(fullpath));
    fullpath[dwUnicodeLength]='\0';

    // Replace the extension with ildb
    if (_wsplitpath_s( fullpath, drive, COUNTOF(drive), dir, COUNTOF(dir), fname, COUNTOF(fname), NULL, 0 ))
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
    if (_wmakepath_s(m_szStoredSymbolName, MAX_LONGPATH, drive, dir, fname, W("ildb") ))
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));

    // It looks valid, make sure to set the return code
    hr = S_OK;
ErrExit:
    if (pMod)
      UnmapViewOfFile(pMod);
    if (hMapFile != INVALID_HANDLE_VALUE)
      CloseHandle(hMapFile);
    if (hFile != INVALID_HANDLE_VALUE)
      CloseHandle(hFile);
    return hr;

}

//-----------------------------------------------------------
// InitializeFromFile
// Initialize the reader using the passed in file name
// Note that upon completion the data hasn't yet been validated for safety.
//-----------------------------------------------------------
HRESULT
SymReader::InitializeFromFile(
    const WCHAR* szFileName,
    const WCHAR* szsearchPath)
{
    HRESULT hr = S_OK;
    WCHAR fullpath[_MAX_PATH];
    WCHAR drive[_MAX_DRIVE];
    WCHAR dir[_MAX_DIR];
    WCHAR fname[_MAX_FNAME];
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hMapFile = INVALID_HANDLE_VALUE;
    HMODULE hMod = NULL;
    BYTE *CurrentOffset;
    DWORD dwFileSize;
    S_UINT32 dwDataSize;
    GUID VersionInfo;

    _ASSERTE(szFileName);
    IfFalseGo(szFileName, E_INVALIDARG );

    IfFailGo(VerifyPEDebugInfo(szFileName));
    // We need to open the exe and check to see if the DebugInfo matches

    if (_wsplitpath_s( szFileName, drive, COUNTOF(drive), dir, COUNTOF(dir), fname, COUNTOF(fname), NULL, 0 ))
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
    if (_wmakepath_s( fullpath, _MAX_PATH, drive, dir, fname, W("ildb") ))
        IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
    if (wcsncpy_s( m_szPath, COUNTOF(m_szPath), fullpath, _TRUNCATE) == STRUNCATE)
        IfFailGo(HrFromWin32(ERROR_INSUFFICIENT_BUFFER));

    hFile = WszCreateFile(m_szPath,
                          GENERIC_READ,
                          FILE_SHARE_READ,
                          NULL,
                          OPEN_EXISTING,
                          FILE_ATTRIBUTE_NORMAL,
                          NULL);

    if (hFile == INVALID_HANDLE_VALUE)
    {

        // If the stored string is empty, don't do anything
        if (m_szStoredSymbolName[0] == '\0')
        {
            return HrFromWin32(GetLastError());
        }

        if (_wsplitpath_s( m_szStoredSymbolName, drive, COUNTOF(drive), dir, COUNTOF(dir), fname, COUNTOF(fname), NULL, 0 ))
            IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
        if (_wmakepath_s( fullpath, _MAX_PATH, drive, dir, fname, W("ildb") ))
            IfFailGo(HrFromWin32(ERROR_BAD_FORMAT));
        if (wcsncpy_s( m_szPath, COUNTOF(m_szPath), fullpath, _TRUNCATE) == STRUNCATE)
            IfFailGo(HrFromWin32(ERROR_INSUFFICIENT_BUFFER));

        hFile = WszCreateFile(m_szPath,
                           GENERIC_READ,
                           FILE_SHARE_READ,
                           NULL,
                           OPEN_EXISTING,
                           FILE_ATTRIBUTE_NORMAL,
                           NULL);

        if (hFile == INVALID_HANDLE_VALUE)
        {
            return HrFromWin32(GetLastError());
        }
    }

    dwFileSize = GetFileSize(hFile, NULL);
    if (dwFileSize < ILDB_HEADER_SIZE)
    {
        IfFailGo(HrFromWin32(ERROR_INVALID_DATA));
    }

    hMapFile = WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
    if (hMapFile == NULL)
        IfFailGo(HrFromWin32(GetLastError()));

    hMod = (HMODULE) MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
    if (hMod == NULL)
        IfFailGo(HrFromWin32(GetLastError()));

    // We've opened the file, now let's get the pertinent info
    CurrentOffset = (BYTE *)hMod;

    // Verify that we're looking at an ILDB File
    if (memcmp(CurrentOffset, ILDB_SIGNATURE, ILDB_SIGNATURE_SIZE))
    {
        IfFailGo(E_FAIL);
    }
    CurrentOffset += ILDB_SIGNATURE_SIZE;

    memcpy( &VersionInfo, CurrentOffset, sizeof(GUID));
    SwapGuid( &VersionInfo );
    CurrentOffset += sizeof(GUID);

    if (memcmp(&VersionInfo, &ILDB_VERSION_GUID, sizeof(GUID)))
    {
        IfFailGo(HrFromWin32(ERROR_INVALID_DATA));
    }

    IfNullGo(m_pPDBInfo = NEW(PDBInfo));

    memcpy(m_pPDBInfo, CurrentOffset, sizeof(PDBInfo));

    // Swap the counts
    m_pPDBInfo->ConvertEndianness();

    // Check to make sure we have enough data to be read in.
    dwDataSize = S_UINT32(ILDB_HEADER_SIZE);
    dwDataSize += m_pPDBInfo->m_CountOfConstants*sizeof(SymConstant);
    dwDataSize += m_pPDBInfo->m_CountOfMethods * sizeof(SymMethodInfo);
    dwDataSize += m_pPDBInfo->m_CountOfScopes*sizeof(SymLexicalScope);
    dwDataSize += m_pPDBInfo->m_CountOfVars*sizeof(SymVariable);
    dwDataSize += m_pPDBInfo->m_CountOfUsing*sizeof(SymUsingNamespace);
    dwDataSize += m_pPDBInfo->m_CountOfSequencePoints*sizeof(SequencePoint);
    dwDataSize += m_pPDBInfo->m_CountOfDocuments*sizeof(DocumentInfo);
    dwDataSize += m_pPDBInfo->m_CountOfBytes*sizeof(BYTE);
    dwDataSize += m_pPDBInfo->m_CountOfStringBytes*sizeof(BYTE);

    if (dwDataSize.IsOverflow() || dwDataSize.Value() > dwFileSize)
    {
        IfFailGo(HrFromWin32(ERROR_INVALID_DATA));
    }

    CurrentOffset += sizeof(PDBInfo);

    if (m_pPDBInfo->m_CountOfConstants)
    {
        m_DataPointers.m_pConstants = (SymConstant*)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfConstants*sizeof(SymConstant));
    }

    if (m_pPDBInfo->m_CountOfMethods)
    {
        m_DataPointers.m_pMethods = (SymMethodInfo *)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfMethods*sizeof(SymMethodInfo));
    }

    if (m_pPDBInfo->m_CountOfScopes)
    {
        m_DataPointers.m_pScopes = (SymLexicalScope *)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfScopes*sizeof(SymLexicalScope));
    }

    if (m_pPDBInfo->m_CountOfVars)
    {
        m_DataPointers.m_pVars = (SymVariable *)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfVars*sizeof(SymVariable));
    }

    if (m_pPDBInfo->m_CountOfUsing)
    {
        m_DataPointers.m_pUsings = (SymUsingNamespace *)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfUsing*sizeof(SymUsingNamespace));
    }

    if (m_pPDBInfo->m_CountOfSequencePoints)
    {
        m_DataPointers.m_pSequencePoints = (SequencePoint*)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfSequencePoints*sizeof(SequencePoint));
    }

    if (m_pPDBInfo->m_CountOfDocuments)
    {
        m_DataPointers.m_pDocuments = (DocumentInfo*)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfDocuments*sizeof(DocumentInfo));
    }

    if (m_pPDBInfo->m_CountOfBytes)
    {
        m_DataPointers.m_pBytes = (BYTE*)CurrentOffset;
        CurrentOffset += (m_pPDBInfo->m_CountOfBytes*sizeof(BYTE));
    }

    if (m_pPDBInfo->m_CountOfStringBytes)
    {
        m_DataPointers.m_pStringsBytes = (BYTE*)CurrentOffset;
    }

ErrExit:
    if (hMod)
        UnmapViewOfFile(hMod);
    if (hMapFile != INVALID_HANDLE_VALUE)
        CloseHandle(hMapFile);
    if (hFile != INVALID_HANDLE_VALUE)
        CloseHandle(hFile);

    return hr;
}

//-----------------------------------------------------------
// GetDocument
// Get the document for the passed in information
//-----------------------------------------------------------
HRESULT
SymReader::GetDocument(
    __in LPWSTR wcsUrl,   // URL of the document
    GUID language,        // Language for the file
    GUID languageVendor,  // Language vendor
    GUID documentType,    // Type of document
    ISymUnmanagedDocument **ppRetVal  // [out] Document
    )
{
    HRESULT hr = S_OK;
    unsigned i;
    SymDocument* pDoc = NULL;
    WCHAR *wcsDocumentUrl = NULL;
    WCHAR *wcsDocumentUrlAlloc = NULL;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(ppRetVal && wcsUrl);
    IfFalseGo(ppRetVal, E_INVALIDARG);
    IfFalseGo(wcsUrl, E_INVALIDARG);

    // Init Out Parameter
    *ppRetVal = NULL;

    for (i = 0; i < m_pPDBInfo->m_CountOfDocuments; i++)
    {
        int cchName;

        // Convert the UTF8 string to Wide
        cchName = (ULONG32) MultiByteToWideChar(CP_UTF8,
                                                0,
                                                (LPCSTR)&(m_DataPointers.m_pStringsBytes[m_DataPointers.m_pDocuments[i].UrlEntry()]),
                                                -1,
                                                0,
                                                NULL);
        IfNullGo( wcsDocumentUrlAlloc = NEW(WCHAR[cchName]) );

        cchName = (ULONG32) MultiByteToWideChar(CP_UTF8,
                                                0,
                                                (LPCSTR)&(m_DataPointers.m_pStringsBytes[m_DataPointers.m_pDocuments[i].UrlEntry()]),
                                                -1,
                                                wcsDocumentUrlAlloc,
                                                cchName);
        wcsDocumentUrl = wcsDocumentUrlAlloc;

        // Compare the url
        if (wcscmp(wcsUrl, wcsDocumentUrl) == 0)
        {
            IfFailGo(GetDocument(i, &pDoc));
            break;
        }
        DELETEARRAY(wcsDocumentUrlAlloc);
        wcsDocumentUrlAlloc = NULL;
    }

    if (pDoc)
    {
        IfFailGo( pDoc->QueryInterface( IID_ISymUnmanagedDocument,
                                        (void**) ppRetVal ) );
    }

ErrExit:
    DELETEARRAY(wcsDocumentUrlAlloc);

    RELEASE( pDoc );

    return hr;
}

//-----------------------------------------------------------
// GetDocuments
// Get the documents for this reader
//-----------------------------------------------------------
HRESULT
SymReader::GetDocuments(
    ULONG32 cDocs,
    ULONG32 *pcDocs,
    ISymUnmanagedDocument *pDocs[]
    )
{
    HRESULT hr = S_OK;
    unsigned cDocCount = 0;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(pDocs || pcDocs);
    IfFalseGo(pDocs || pcDocs, E_INVALIDARG);

    cDocs = min(cDocs, m_pPDBInfo->m_CountOfDocuments);

    for (cDocCount = 0; cDocCount < cDocs; cDocCount++)
    {
        if (pDocs)
        {
            SymDocument *pDoc;
            IfFailGo(GetDocument(cDocCount, &pDoc));
            pDocs[cDocCount] = pDoc;
        }
    }
    if (pcDocs)
    {
        *pcDocs = (ULONG32)m_pPDBInfo->m_CountOfDocuments;
    }

ErrExit:
    if (FAILED(hr))
    {
        unsigned i;
        for (i = 0; i < cDocCount; i++)
        {
            RELEASE(pDocs[cDocCount]);
        }
    }
    return hr;
}

//-----------------------------------------------------------
// GetUserEntryPoint
// Get the entry point for the pe
//-----------------------------------------------------------
HRESULT
SymReader::GetUserEntryPoint(
    mdMethodDef *pRetVal
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);

    // If it wasn't set then return E_FAIL
    if (m_pPDBInfo->m_userEntryPoint == mdTokenNil)
    {
        hr = E_FAIL;
    }
    else
    {
        *pRetVal = m_pPDBInfo->m_userEntryPoint;
    }
ErrExit:
    return hr;
}

// Compare the method token with the SymMethodInfo Entry and return the
// value expected by bsearch
int __cdecl CompareMethodToToken(const void *pMethodToken, const void *pMethodInfoEntry)
{
    mdMethodDef MethodDef = *(mdMethodDef *)pMethodToken;
    SymMethodInfo *pMethodInfo = (SymMethodInfo *)pMethodInfoEntry;

    return MethodDef - pMethodInfo->MethodToken();
}

//-----------------------------------------------------------
// GetMethod
// Get the method for the methoddef
//-----------------------------------------------------------
HRESULT
SymReader::GetMethod(
    mdMethodDef method,   // MethodDef
    ISymUnmanagedMethod **ppRetVal  // [out] Method
    )
{
    HRESULT hr = S_OK;
    UINT32 MethodEntry = 0;
    SymMethodInfo *pMethodInfo;
    SymMethod * pMethod = NULL;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(ppRetVal);
    IfFalseGo(ppRetVal, E_INVALIDARG);

    pMethodInfo = (SymMethodInfo *)bsearch(&method, m_DataPointers.m_pMethods, m_pPDBInfo->m_CountOfMethods, sizeof(SymMethodInfo), CompareMethodToToken);
    IfFalseGo(pMethodInfo, E_FAIL); // no matching method found

    // Found a match
    MethodEntry = UINT32 (pMethodInfo - m_DataPointers.m_pMethods);
    _ASSERTE(m_DataPointers.m_pMethods[MethodEntry].MethodToken() == method);
    IfNullGo( pMethod = NEW(SymMethod(this, &m_DataPointers, MethodEntry)) );
    *ppRetVal = pMethod;
    pMethod->AddRef();
    hr = S_OK;

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetMethodByVersion
//-----------------------------------------------------------
HRESULT
SymReader::GetMethodByVersion(
    mdMethodDef method,
    int version,
    ISymUnmanagedMethod **ppRetVal
    )
{
    // Don't support multiple version of the same Method so just
    // call GetMethod
    return GetMethod(method, ppRetVal);
}


//-----------------------------------------------------------
// GetMethodFromDocumentPosition
//-----------------------------------------------------------
HRESULT
SymReader::GetMethodFromDocumentPosition(
    ISymUnmanagedDocument *document,
    ULONG32 line,
    ULONG32 column,
    ISymUnmanagedMethod **ppRetVal
)
{
    HRESULT hr = S_OK;
    UINT32 DocumentEntry;
    UINT32 Method;
    UINT32 point;
    SequencePoint *pSequencePointBefore;
    SequencePoint *pSequencePointAfter;
    bool found = false;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(document && ppRetVal);
    IfFalseGo(document, E_INVALIDARG);
    IfFalseGo(ppRetVal, E_INVALIDARG);

    DocumentEntry = ((SymDocument *)document)->GetDocumentEntry();

    // Init out parameter
    *ppRetVal = NULL;

    // Walk all Methods, check their Document and SequencePoints to see if it's in this doc
    // and the line/column

    // This function returns the first match if more than one methods cover the specified position.
    for (Method = 0; Method < m_pPDBInfo->m_CountOfMethods; Method++)
    {
        pSequencePointBefore = NULL;
        pSequencePointAfter = NULL;

        // Walk the sequence points
        for (point = m_DataPointers.m_pMethods[Method].StartSequencePoints();
             point < m_DataPointers.m_pMethods[Method].EndSequencePoints();
             point++)
        {
            // Check to see if this sequence point is in this doc
            if (m_DataPointers.m_pSequencePoints[point].Document() == DocumentEntry)
            {
                // If the point is position is within the sequence point then
                // we're done.
                if (m_DataPointers.m_pSequencePoints[point].IsWithin(line, column))
                {
                    IfFailGo(GetMethod(m_DataPointers.m_pMethods[Method].MethodToken(), ppRetVal));
                    found = true;
                    break;
                }

                // If the sequence is before the point then just remember the point
                if (m_DataPointers.m_pSequencePoints[point].IsUserLine() &&
                    m_DataPointers.m_pSequencePoints[point].IsLessThan(line, column))
                {
                    pSequencePointBefore = &m_DataPointers.m_pSequencePoints[point];
                }

                // If the sequence is before the point then just remember the point
                if (m_DataPointers.m_pSequencePoints[point].IsUserLine() &&
                    m_DataPointers.m_pSequencePoints[point].IsGreaterThan(line, column))
                {
                    pSequencePointAfter = &m_DataPointers.m_pSequencePoints[point];
                }
            }
        }

        // If we found an exact match, we're done.
        if (found)
        {
            break;
        }

        // If we found sequence points within the method before and after
        // the line/column then we may have found the method. Record the return value, but keep looking
        // to see if we find an exact match. This may not actually be the right method. Iron Python, for instance,
        // issues a "method" containing sequence points for all the method headers in a class. Sequence points
        // in this "method" would then span the sequence points in the bodies of all but the last method.
        if (pSequencePointBefore && pSequencePointAfter)
        {
            IfFailGo(GetMethod(m_DataPointers.m_pMethods[Method].MethodToken(), ppRetVal));
        }
    }

    // This function returns E_FAIL if no match is found.
    // Note that this is different from the behaviour of GetMethodsFromDocumentPosition() (see below).
    if (*ppRetVal == NULL)
    {
        hr = E_FAIL;
    }

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Return all methods with sequence points covering the specified source location.  This
// is actually not as straighforward as it sounds, since we need to mimic the behaviour of
// diasymreader and PDBs here.  For PDBs, diasymreader actually does two passes over the
// sequence points.  It tries to find an exact match in the first pass, and if that fails,
// it'll do a second pass looking for an approximate match.  An approximate match is a sequence
// point which doesn't start on the specified line but covers it.  If there's an exact match,
// then it ignores all the approximate matches.  In both cases, diasymreader only checks the
// start line number of the sequence point and it ignores the column number.
//
// For backward compatibility, I'm leaving GetMethodFromDocumentPosition() unchanged.
//

HRESULT
SymReader::GetMethodsFromDocumentPosition(
    ISymUnmanagedDocument *document,
    ULONG32 line,
    ULONG32 column,
    ULONG32 cMethod,
    ULONG32* pcMethod,  //[Optional]: How many method actually returned
    ISymUnmanagedMethod** ppRetVal
    )
{
    HRESULT hr = S_OK;
    UINT32 DocumentEntry;
    UINT32 Method;
    UINT32 point;

    UINT CurMethod = 0;
    bool found = false;
    bool fExactMatch = true;

    ULONG32 maxPreLine = 0;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(document);
    IfFalseGo(document, E_INVALIDARG);

    _ASSERTE((cMethod == 0) || (ppRetVal != NULL));
    IfFalseGo(cMethod == 0 || ppRetVal != NULL, E_INVALIDARG);

    // Initialize the out parameter if it has been provided.
    if (pcMethod != NULL)
    {
        *pcMethod = 0;
    }

    DocumentEntry = ((SymDocument *)document)->GetDocumentEntry();

    // Enumerate the sequence points in two passes.
    while (true)
    {
        // Walk all Methods, check their Document and SequencePoints to see if it's in this doc
        // and the line/column

        for (Method = 0; Method < m_pPDBInfo->m_CountOfMethods; Method++)
        {
            found = false;

            // Walk the sequence points
            for (point = m_DataPointers.m_pMethods[Method].StartSequencePoints();
                 point < m_DataPointers.m_pMethods[Method].EndSequencePoints();
                 point++)
            {
                // Check to see if this sequence point is in this doc
                if (m_DataPointers.m_pSequencePoints[point].Document() == DocumentEntry)
                {
                    // PDBs (more specifically the DIA APIs) only check the start line number and not the end line number when
                    // trying to determine whether a sequence point covers the specified line number.  We need to match this
                    // behaviour here.  For backward compatibility reasons, GetMethodFromDocumentPosition() is still checking
                    // against the entire range of a sequence point, but we should revisit that in the next release.
                    ULONG32 curLine = m_DataPointers.m_pSequencePoints[point].StartLine();

                    if (fExactMatch)
                    {
                        if (curLine == line)
                        {
                            found = true;
                        }
                        else if ((maxPreLine < curLine) && (curLine < line))
                        {
                            // This is not an exact match, but let's keep track of the sequence point closest to the specified
                            // line.  We'll use this info if we have to do a second pass.
                            maxPreLine = curLine;
                        }
                    }
                    else
                    {
                        // We are in the second pass, looking for approximate matches.
                        if ((maxPreLine != 0) && (maxPreLine == curLine))
                        {
                            // Make sure the sequence point covers the specified line.
                            if (m_DataPointers.m_pSequencePoints[point].IsWithinLineOnly(line))
                            {
                                found = true;
                            }
                        }
                    }

                    // If we have found a match (whether it's exact or approximate), then save this method unless the caller is
                    // only interested in the number of matching methods or the array provided by the caller isn't big enough.
                    if (found)
                    {
                        if (CurMethod < cMethod)
                        {
                            IfFailGo(GetMethod(m_DataPointers.m_pMethods[Method].MethodToken(), &(ppRetVal[CurMethod])));
                        }
                        CurMethod++;
                        break;
                    }
                }
            }

            if (found)
            {
                // If we have already filled out the entire array provided by the caller, then we are done.
                if ((cMethod > 0) && (cMethod == CurMethod))
                {
                    break;
                }
                else
                {
                    // Otherwise move on to the next method.
                    continue;
                }
            }
        }

        // If we haven't found an exact match, then try it again looking for a sequence point covering the specified line.
        if (fExactMatch && (CurMethod == 0))
        {
            fExactMatch = false;
            continue;
        }
        else
        {
            // If we have found an exact match, or if we have done two passes already, then bail.
            break;
        }
    }

    // Note that unlike GetMethodFromDocumentPosition(), this function returns S_OK even if a match is not found.
    if (SUCCEEDED(hr))
    {
        if (pcMethod != NULL)
        {
            *pcMethod = CurMethod;
        }
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetSymbolStoreFileName
//-----------------------------------------------------------
HRESULT
SymReader::GetSymbolStoreFileName(
    ULONG32 cchName,    // Length of szName
    ULONG32 *pcchName,  // [Optional]
    __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]      // [Optional]
    )
{
    _ASSERTE(m_fInitialized);
    if (!m_fInitialized)
        return E_UNEXPECTED;

    if (pcchName)
    {
        *pcchName = (ULONG32)(wcslen(m_szPath)+1);
    }

    if( szName )
    {
        if (wcsncpy_s( szName, cchName, m_szPath, _TRUNCATE) == STRUNCATE)
            return HrFromWin32(ERROR_INSUFFICIENT_BUFFER);
    }

    return NOERROR;
}

//-----------------------------------------------------------
// GetMethodVersion
//-----------------------------------------------------------
HRESULT
SymReader::GetMethodVersion(
    ISymUnmanagedMethod * pMethod,
    int* pVersion
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(pMethod && pVersion);
    IfFalseGo( pMethod && pVersion, E_INVALIDARG);
    // This symbol store only supports one version of a method
    *pVersion = 0;
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetDocumentVersion
//-----------------------------------------------------------
HRESULT
SymReader::GetDocumentVersion(
    ISymUnmanagedDocument* pDoc,
    int* pVersion,
    BOOL* pbCurrent // [Optional]
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(pVersion && pDoc);
    IfFalseGo(pVersion, E_INVALIDARG);
    IfFalseGo(pDoc, E_INVALIDARG);

    // This symbol store only supports one version of a document
    *pVersion = 0;
    if (pbCurrent)
    {
        *pbCurrent = TRUE;
    }
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetDocument
// Return the document for the given entry
//-----------------------------------------------------------
HRESULT SymReader::GetDocument(
    UINT32 DocumentEntry,
    SymDocument **ppDocument)
{
    HRESULT hr = NOERROR;

    _ASSERTE(m_fInitialized);
    IfFalseGo(m_fInitialized, E_UNEXPECTED);

    _ASSERTE(ppDocument);
    IfFalseGo(ppDocument, E_INVALIDARG);

    _ASSERTE(DocumentEntry < m_pPDBInfo->m_CountOfDocuments);
    IfFalseGo(DocumentEntry < m_pPDBInfo->m_CountOfDocuments, E_INVALIDARG);

    if (m_pDocs == NULL)
    {
        IfNullGo(m_pDocs = NEW(SymDocument *[m_pPDBInfo->m_CountOfDocuments]));
        memset(m_pDocs, 0, m_pPDBInfo->m_CountOfDocuments * sizeof(void *));
    }

    if (m_pDocs[DocumentEntry] == NULL)
    {
        m_pDocs[DocumentEntry] = NEW(SymDocument(this, &m_DataPointers, m_pPDBInfo->m_CountOfMethods, DocumentEntry));
        IfNullGo(m_pDocs[DocumentEntry]);
        // AddRef the table version
        m_pDocs[DocumentEntry]->AddRef();

    }

    //Set and AddRef the Out Parameter
    *ppDocument = m_pDocs[DocumentEntry];
    (*ppDocument)->AddRef();

ErrExit:
    return hr;
}

HRESULT
SymReader::UpdateSymbolStore(
    const WCHAR *filename,
    IStream *pIStream
    )
{
    // This symbol store doesn't support updating the symbol store.
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

HRESULT
SymReader::ReplaceSymbolStore(
    const WCHAR *filename,
    IStream *pIStream
    )
{
    // This symbol store doesn't support updating the symbol store.
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetVariables
//-----------------------------------------------------------
HRESULT
SymReader::GetVariables(
    mdToken parent,
    ULONG32 cVars,
    ULONG32 *pcVars,
    ISymUnmanagedVariable *pVars[]
    )
{
    //
    // This symbol reader doesn't support non-local variables.
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetGlobalVariables
//-----------------------------------------------------------
HRESULT
SymReader::GetGlobalVariables(
    ULONG32 cVars,
    ULONG32 *pcVars,
    ISymUnmanagedVariable *pVars[]
    )
{
    //
    // This symbol reader doesn't support non-local variables.
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetSymAttribute
//-----------------------------------------------------------
HRESULT
SymReader::GetSymAttribute(
    mdToken parent,
    __in LPWSTR name,
    ULONG32 cBuffer,
    ULONG32 *pcBuffer,
    __out_bcount_part_opt(cBuffer, *pcBuffer) BYTE buffer[]
    )
{
    // This symbol store doesn't support attributes
    // VS may query for certain attributes, but will survive without them,
    // so don't ASSERT here.
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetNamespaces
//-----------------------------------------------------------
HRESULT
SymReader::GetNamespaces(
    ULONG32 cNameSpaces,
    ULONG32 *pcNameSpaces,
    ISymUnmanagedNamespace *namespaces[]
    )
{
    // This symbol store doesn't support this
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

/* ------------------------------------------------------------------------- *
 * SymDocument class
 * ------------------------------------------------------------------------- */

HRESULT
SymDocument::QueryInterface(
    REFIID riid,
    void **ppInterface
    )
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ISymUnmanagedDocument)
        *ppInterface = (ISymUnmanagedDocument*)this;
    else if (riid == IID_IUnknown)
        *ppInterface = (IUnknown*)(ISymUnmanagedDocument*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}


//-----------------------------------------------------------
// GetURL
//-----------------------------------------------------------
HRESULT
SymDocument::GetURL(
    ULONG32 cchUrl,   // The allocated size of the buffer
    ULONG32 *pcchUrl, // [optional,out] The number of characters available for return
    __out_ecount_part_opt(cchUrl, *pcchUrl) WCHAR szUrl[]     // [optional,out] The string buffer.
    )
{
    if (pcchUrl)
    {
        // Convert the UTF8 string to Wide
        *pcchUrl = (ULONG32) MultiByteToWideChar(CP_UTF8,
                                                0,
                                                (LPCSTR)&(m_pData->m_pStringsBytes[m_pData->m_pDocuments[m_DocumentEntry].UrlEntry()]),
                                                -1,
                                                0,
                                                NULL);
    }

    if( szUrl )
    {
        // Convert the UTF8 string to Wide
        MultiByteToWideChar(CP_UTF8,
                            0,
                            (LPCSTR)&(m_pData->m_pStringsBytes[m_pData->m_pDocuments[m_DocumentEntry].UrlEntry()]),
                            -1,
                            szUrl,
                            cchUrl);
    }
    return NOERROR;
}

//-----------------------------------------------------------
// GetDocumentType
//-----------------------------------------------------------
HRESULT
SymDocument::GetDocumentType(
    GUID *pRetVal
    )
{
    HRESULT hr = NOERROR;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);
    *pRetVal = m_pData->m_pDocuments[m_DocumentEntry].DocumentType();
    SwapGuid(pRetVal);
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetLanguage
//-----------------------------------------------------------
HRESULT
SymDocument::GetLanguage(
    GUID *pRetVal
    )
{
    HRESULT hr = NOERROR;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);

    *pRetVal = m_pData->m_pDocuments[m_DocumentEntry].Language();
    SwapGuid(pRetVal);
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetLanguageVendor
//-----------------------------------------------------------
HRESULT
SymDocument::GetLanguageVendor(
    GUID *pRetVal
    )
{
    HRESULT hr = NOERROR;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);
    *pRetVal = m_pData->m_pDocuments[m_DocumentEntry].LanguageVendor();
    SwapGuid(pRetVal);
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetCheckSumAlgorithmId
//-----------------------------------------------------------
HRESULT
SymDocument::GetCheckSumAlgorithmId(
    GUID *pRetVal
    )
{
    HRESULT hr = NOERROR;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);
    *pRetVal = m_pData->m_pDocuments[m_DocumentEntry].AlgorithmId();
    SwapGuid(pRetVal);
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetCheckSum
//-----------------------------------------------------------
HRESULT
SymDocument::GetCheckSum(
    ULONG32 cData,    // The allocated size of the buffer.
    ULONG32 *pcData,  // [optional] The number of bytes available for return
    BYTE data[])      // [optional] The buffer to receive the checksum.
{
    BYTE *pCheckSum = &m_pData->m_pBytes[m_pData->m_pDocuments[m_DocumentEntry].CheckSumEntry()];
    ULONG32 CheckSumSize = m_pData->m_pDocuments[m_DocumentEntry].CheckSumSize();
    if (pcData)
    {
        *pcData = CheckSumSize;
    }
    if(data)
    {
        memcpy(data, pCheckSum, min(CheckSumSize, cData));
    }
    return NOERROR;
}

//-----------------------------------------------------------
// FindClosestLine
// Search the sequence points looking a line that is closest
// line following this one that is a sequence point
//-----------------------------------------------------------
HRESULT
SymDocument::FindClosestLine(
    ULONG32 line,
    ULONG32 *pRetVal
    )
{
    HRESULT hr = NOERROR;
    UINT32 Method;
    UINT32 point;
    ULONG32 closestLine = 0;  // GCC can't tell this isn't used before initialization
    bool found = false;

    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);

    // Walk all Methods, check their Document and SequencePoints to see if it's in this doc
    // and the line/column
    for (Method = 0; Method < m_CountOfMethods; Method++)
    {
        // Walk the sequence points
        for (point = m_pData->m_pMethods[Method].StartSequencePoints();
             point < m_pData->m_pMethods[Method].EndSequencePoints();
             point++)
        {
            SequencePoint & sp = m_pData->m_pSequencePoints[point];
            // Check to see if this sequence point is in this doc, and is at or
            // after the requested line
            if ((sp.Document() == m_DocumentEntry) && sp.IsUserLine())
            {
                if (sp.IsWithin(line, 0) || sp.IsGreaterThan(line, 0))
                {
                    // This sequence point is at or after the requested line.  If we haven't
                    // already found a "closest", or this is even closer than the one we have,
                    // then mark this as the best line so far.
                    if (!found || m_pData->m_pSequencePoints[point].StartLine() < closestLine)
                    {
                        found = true;
                        closestLine = m_pData->m_pSequencePoints[point].StartLine();
                    }
                }
            }
        }
    }

    if (found)
    {
        *pRetVal = closestLine;
    }
    else
    {
        // Didn't find any lines at or after the one requested.
        hr = E_FAIL;
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// SymDocument HasEmbeddedSource
//-----------------------------------------------------------
HRESULT
SymDocument::HasEmbeddedSource(
    BOOL *pRetVal
    )
{
    //
    // This symbol reader doesn't support embedded source.
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// SymDocument GetSourceLength
//-----------------------------------------------------------
HRESULT
SymDocument::GetSourceLength(
    ULONG32 *pRetVal
    )
{
    //
    // This symbol reader doesn't support embedded source.
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// SymDocument GetSourceRange
//-----------------------------------------------------------
HRESULT
SymDocument::GetSourceRange(
    ULONG32 startLine,
    ULONG32 startColumn,
    ULONG32 endLine,
    ULONG32 endColumn,
    ULONG32 cSourceBytes,
    ULONG32 *pcSourceBytes,
    BYTE source[]
    )
{
    //
    // This symbol reader doesn't support embedded source.
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

/* ------------------------------------------------------------------------- *
 * SymMethod class
 * ------------------------------------------------------------------------- */
HRESULT
SymMethod::QueryInterface(
    REFIID riid,
    void **ppInterface
    )
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ISymUnmanagedMethod)
        *ppInterface = (ISymUnmanagedMethod*)this;
    else if (riid == IID_IUnknown)
        *ppInterface = (IUnknown*)(ISymUnmanagedMethod*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

//-----------------------------------------------------------
// GetToken
//-----------------------------------------------------------
HRESULT
SymMethod::GetToken(
    mdMethodDef *pRetVal
)
{
    HRESULT hr = S_OK;

    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);
    *pRetVal = m_pData->m_pMethods[m_MethodEntry].MethodToken();
ErrExit:
    return hr;
}


//-----------------------------------------------------------
// GetSequencePointCount
//-----------------------------------------------------------
HRESULT
SymMethod::GetSequencePointCount(
    ULONG32* pRetVal
    )
{

    HRESULT hr = S_OK;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);

    *pRetVal = (ULONG32)(m_pData->m_pMethods[m_MethodEntry].EndSequencePoints() -
                         m_pData->m_pMethods[m_MethodEntry].StartSequencePoints());
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetSequencePoints
//-----------------------------------------------------------
HRESULT
SymMethod::GetSequencePoints(
    ULONG32 cPoints,    // The size of the allocated arrays.
    ULONG32* pcPoints,  // [optional] The number of sequence points available for return.
    ULONG32 offsets[],  // [optional]
    ISymUnmanagedDocument *documents[], // [Optional]
    ULONG32 lines[],      // [Optional]
    ULONG32 columns[],    // [Optional]
    ULONG32 endLines[],   // [Optional]
    ULONG32 endColumns[]  // [Optional]
    )
{
    HRESULT hr = NOERROR;
    UINT32 i = 0;
    ULONG32 Points = 0;

    for (i = m_pData->m_pMethods[m_MethodEntry].StartSequencePoints();
         (i < m_pData->m_pMethods[m_MethodEntry].EndSequencePoints());
         i++, Points++)
    {
        if (Points < cPoints)
        {
            if (documents)
            {
                SymDocument *pDoc;
                IfFailGo(m_pReader->GetDocument(m_pData->m_pSequencePoints[i].Document(), &pDoc));
                documents[Points] = pDoc;
            }

            if (offsets)
            {
                offsets[Points] = m_pData->m_pSequencePoints[i].Offset();
            }

            if (lines)
            {
                lines[Points] = m_pData->m_pSequencePoints[i].StartLine();
            }
            if (columns)
            {
                columns[Points] = m_pData->m_pSequencePoints[i].StartColumn();
            }
            if (endLines)
            {
                endLines[Points] = m_pData->m_pSequencePoints[i].EndLine();
            }
            if (endColumns)
            {
                endColumns[Points] = m_pData->m_pSequencePoints[i].EndColumn();
            }
        }
    }

    if (pcPoints)
    {
        *pcPoints = Points;
    }

ErrExit:
    if (FAILED(hr))
    {
        if (documents)
        {
            unsigned j;
            for (j = 0; j < i; j++)
            {
                RELEASE(documents[i]);
            }
        }
    }
    return hr;
}

//-----------------------------------------------------------
// GetRootScope
//-----------------------------------------------------------
HRESULT
SymMethod::GetRootScope(
    ISymUnmanagedScope **ppRetVal
    )
{
    HRESULT hr = S_OK;
    SymScope *pScope = NULL;
    _ASSERTE(ppRetVal);
    IfFalseGo(ppRetVal, E_INVALIDARG);

    // Init Out Param
    *ppRetVal = NULL;
    if (m_pData->m_pMethods[m_MethodEntry].EndScopes() - m_pData->m_pMethods[m_MethodEntry].StartScopes())
    {
        IfNullGo(pScope = NEW(SymScope(this, m_pData, m_MethodEntry, m_pData->m_pMethods[m_MethodEntry].StartScopes())));
        pScope->AddRef();
        *ppRetVal = pScope;
    }
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetOffset
// Given a position in a document, gets the offset within the
// method that corresponds to the position.
//-----------------------------------------------------------
HRESULT
SymMethod::GetOffset(
    ISymUnmanagedDocument *document,
    ULONG32 line,
    ULONG32 column,
    ULONG32 *pRetVal
    )
{
    HRESULT hr = S_OK;
    bool fFound = false;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);

    UINT32 point;
    UINT32 DocumentEntry;

    DocumentEntry = ((SymDocument *)document)->GetDocumentEntry();

    // Walk the sequence points
    for (point = m_pData->m_pMethods[m_MethodEntry].StartSequencePoints();
         point < m_pData->m_pMethods[m_MethodEntry].EndSequencePoints();
         point++)
    {
        // Check to see if this sequence point is in this doc
        if (m_pData->m_pSequencePoints[point].Document() == DocumentEntry)
        {
            // Check to see if it's within the sequence point
            if (m_pData->m_pSequencePoints[point].IsWithin(line, column))
            {
                *pRetVal = m_pData->m_pSequencePoints[point].Offset();
                fFound = true;
                break;
            }
        }
    }
    if (!fFound)
    {
        hr = E_FAIL;
    }
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetRanges
//-----------------------------------------------------------
HRESULT
SymMethod::GetRanges(
    ISymUnmanagedDocument *pDocument, // [in] Document we're working on
    ULONG32 line,                     // [in] The document line corresponding to the ranges.
    ULONG32 column,                   // [in] Ignored
    ULONG32 cRanges,                  // [in] The size of the allocated ranges[] array.
    ULONG32 *pcRanges,                // [out] The number of ranges available for return
    ULONG32 ranges[]                  // [out] The range array.
    )
{
    HRESULT hr = NOERROR;
    DWORD iRange = 0;
    UINT32 DocumentEntry;
    UINT32 point;
    bool fFound = false;

    // Validate some of the parameters
    _ASSERTE(pDocument && (cRanges % 2) == 0);
    IfFalseGo(pDocument, E_INVALIDARG);
    IfFalseGo((cRanges % 2) == 0, E_INVALIDARG);

    // Init out parameter
    if (pcRanges)
    {
        *pcRanges=0;
    }

    DocumentEntry = ((SymDocument *)pDocument)->GetDocumentEntry();

    // Walk the sequence points
    for (point = m_pData->m_pMethods[m_MethodEntry].StartSequencePoints();
         point < m_pData->m_pMethods[m_MethodEntry].EndSequencePoints();
         point++)
    {
        // Check to see if this sequence point is in this doc
        if (m_pData->m_pSequencePoints[point].Document() == DocumentEntry)
        {
            // Check to see if the line is within this sequence
            // Note, to be compatible with VS7, ignore the column information
            if (line >= m_pData->m_pSequencePoints[point].StartLine() &&
                line <= m_pData->m_pSequencePoints[point].EndLine())
            {
                fFound = true;
                break;
            }
        }
    }

    if (fFound)
    {
        for (;point < m_pData->m_pMethods[m_MethodEntry].EndSequencePoints(); point++)
        {

            // Search through all the sequence points since line might have there
            // IL spread across multiple ranges (for loops for example)
            if (m_pData->m_pSequencePoints[point].Document() == DocumentEntry &&
                line >= m_pData->m_pSequencePoints[point].StartLine() &&
                line <= m_pData->m_pSequencePoints[point].EndLine())
            {
                if (iRange < cRanges)
                {
                    ranges[iRange] = m_pData->m_pSequencePoints[point].Offset();
                }
                iRange++;
                if (iRange < cRanges)
                {
                    if (point+1 < m_pData->m_pMethods[m_MethodEntry].EndSequencePoints())
                    {
                        ranges[iRange] = m_pData->m_pSequencePoints[point+1].Offset();
                    }
                    else
                    {
                        // Then it must be till the end of the function which is the root scope's endoffset
                        ranges[iRange] = m_pData->m_pScopes[m_pData->m_pMethods[m_MethodEntry].StartScopes()].EndOffset()+1;
                    }
                }
                iRange++;
            }
        }
        if (pcRanges)
        {
            // If cRanges passed in, return the number
            // of elements actually filled in
            if (cRanges)
            {
                *pcRanges = min(iRange, cRanges);
            }
            else
            {
                // Otherwise return the max number
                *pcRanges = iRange;
            }
        }
    }
    else
    {
        return E_FAIL;
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetScopeFromOffset
//-----------------------------------------------------------
HRESULT
SymMethod::GetScopeFromOffset(
    ULONG32 offset,
    ISymUnmanagedScope **pRetVal
    )
{
    //
    // This symbol reader doesn't support this functionality
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetParameters
//-----------------------------------------------------------
HRESULT
SymMethod::GetParameters(
    ULONG32 cParams,
    ULONG32 *pcParams,
    ISymUnmanagedVariable *params[]
    )
{
    //
    // This symbol reader doesn't support parameter access. Parameters
    // can be found in the normal metadata.
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetNamespace
//-----------------------------------------------------------
HRESULT
SymMethod::GetNamespace(
    ISymUnmanagedNamespace **ppRetVal
    )
{
    //
    // This symbol reader doesn't support namespaces
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetSourceStartEnd
//-----------------------------------------------------------
HRESULT
SymMethod::GetSourceStartEnd(
    ISymUnmanagedDocument *docs[2],
    ULONG32 lines[2],
    ULONG32 columns[2],
    BOOL *pRetVal
    )
{
    //
    // This symbol reader doesn't support source start/end for methods.
    //
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

/* ------------------------------------------------------------------------- *
 * SymScope class
 * ------------------------------------------------------------------------- */

//-----------------------------------------------------------
// QueryInterface
//-----------------------------------------------------------
HRESULT
SymScope::QueryInterface(
    REFIID riid,
    void **ppInterface
    )
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ISymUnmanagedScope)
        *ppInterface = (ISymUnmanagedScope*)this;
    else if (riid == IID_IUnknown)
        *ppInterface = (IUnknown*)(ISymUnmanagedScope*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

//-----------------------------------------------------------
// GetMethod
//-----------------------------------------------------------
HRESULT
SymScope::GetMethod(
    ISymUnmanagedMethod **ppRetVal
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(ppRetVal);
    IfFalseGo(ppRetVal, E_INVALIDARG);

    *ppRetVal = m_pSymMethod;
    m_pSymMethod->AddRef();

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetParent
//-----------------------------------------------------------
HRESULT
SymScope::GetParent(
    ISymUnmanagedScope **ppRetVal
    )
{
    HRESULT hr = S_OK;
    _ASSERTE(ppRetVal);
    IfFalseGo(ppRetVal, E_INVALIDARG);
    if (m_pData->m_pScopes[m_ScopeEntry].ParentScope() != (UINT32)-1)
    {
        IfNullGo(*ppRetVal = static_cast<ISymUnmanagedScope *>(NEW(SymScope(m_pSymMethod, m_pData, m_MethodEntry,
            m_pData->m_pScopes[m_ScopeEntry].ParentScope()))));
        (*ppRetVal)->AddRef();
    }
    else
    {
        *ppRetVal = NULL;
    }
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetChildren
//-----------------------------------------------------------
HRESULT
SymScope::GetChildren(
    ULONG32 cChildren,    // [optional] Number of entries in children
    ULONG32 *pcChildren,  // [optional, out] Number of Children available for retur
    ISymUnmanagedScope *children[] // [optional] array to store children into
    )
{
    HRESULT hr = S_OK;
    ULONG32 ChildrenCount = 0;
    _ASSERTE(pcChildren || (children && cChildren));
    IfFalseGo((pcChildren || (children && cChildren)), E_INVALIDARG);

    if (m_pData->m_pScopes[m_ScopeEntry].HasChildren())
    {
        UINT32 ScopeEntry;
        for(ScopeEntry = m_pData->m_pMethods[m_MethodEntry].StartScopes();
            (ScopeEntry < m_pData->m_pMethods[m_MethodEntry].EndScopes());
            ScopeEntry++)
        {
            if (m_pData->m_pScopes[ScopeEntry].ParentScope() == m_ScopeEntry)
            {
                if (children && ChildrenCount < cChildren)
                {
                    SymScope *pScope;
                    // Found a child
                    IfNullGo(pScope = NEW(SymScope(m_pSymMethod, m_pData, m_MethodEntry, ScopeEntry)));
                    children[ChildrenCount] = pScope;
                    pScope->AddRef();
                }
                ChildrenCount++;
            }
        }
    }

    if (pcChildren)
    {
        *pcChildren = ChildrenCount;
    }

ErrExit:
    if (FAILED(hr) && ChildrenCount)
    {
        unsigned i;
        for (i =0; i< ChildrenCount; i++)
        {
            RELEASE(children[i]);
        }
    }
    return hr;
}

//-----------------------------------------------------------
// GetStartOffset
//-----------------------------------------------------------
HRESULT
SymScope::GetStartOffset(
    ULONG32* pRetVal
    )
{
    HRESULT hr = S_OK;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);
    *pRetVal = m_pData->m_pScopes[m_ScopeEntry].StartOffset();
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetEndOffset
//-----------------------------------------------------------
HRESULT
SymScope::GetEndOffset(
    ULONG32* pRetVal
    )
{
    HRESULT hr = S_OK;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);
    *pRetVal = m_pData->m_pScopes[m_ScopeEntry].EndOffset();
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetLocalCount
//-----------------------------------------------------------
HRESULT
SymScope::GetLocalCount(
    ULONG32 *pRetVal
    )
{
    HRESULT hr = S_OK;
    ULONG32 LocalCount = 0;
    _ASSERTE(pRetVal);
    IfFalseGo(pRetVal, E_INVALIDARG);

    // Init out parameter
    *pRetVal = 0;
    if (m_pData->m_pScopes[m_ScopeEntry].HasVars())
    {
        UINT32 var;
        // Walk and get the locals for this Scope
        for (var = m_pData->m_pMethods[m_MethodEntry].StartVars();
             var < m_pData->m_pMethods[m_MethodEntry].EndVars();
             var++)
        {
            if (m_pData->m_pVars[var].Scope() == m_ScopeEntry &&
                m_pData->m_pVars[var].IsParam() == false)
            {
                LocalCount++;
            }
        }
    }

    *pRetVal = LocalCount;
ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetLocals
// Input: either pcLocals or
//        cLocals and pLocals
//-----------------------------------------------------------
HRESULT
SymScope::GetLocals(
    ULONG32 cLocals,    // [optional] available entries in pLocals
    ULONG32 *pcLocals,  // [optional, out] Number of locals returned
    ISymUnmanagedVariable *pLocals[] // [optional] array to store locals into
    )
{
    HRESULT hr = S_OK;

    ULONG32 LocalCount = 0;
    _ASSERTE(pcLocals || pLocals);
    IfFalseGo(pcLocals || pLocals, E_INVALIDARG);

    if (m_pData->m_pScopes[m_ScopeEntry].HasVars())
    {
        UINT32 var;
        // Walk and get the locals for this Scope
        for (var = m_pData->m_pMethods[m_MethodEntry].StartVars();
             var < m_pData->m_pMethods[m_MethodEntry].EndVars();
             var++)
        {
            if (m_pData->m_pVars[var].Scope() == m_ScopeEntry &&
                m_pData->m_pVars[var].IsParam() == false)
            {
                if (pLocals && LocalCount < cLocals)
                {
                    SymReaderVar *pVar;
                    IfNullGo( pVar = NEW(SymReaderVar(this, m_pData, var)));
                    pLocals[LocalCount] = pVar;
                    pVar->AddRef();
                }
                LocalCount++;
            }
        }
    }
    if (pcLocals)
    {
        *pcLocals = LocalCount;
    }
ErrExit:
    if (FAILED(hr) && LocalCount != 0)
    {
        unsigned i;
        for (i =0; i < LocalCount; i++)
        {
            RELEASE(pLocals[i]);
        }
    }
    return hr;
}

//-----------------------------------------------------------
// GetNamespaces
// Input: either pcNameSpaces or
//        cNameSpaces and pNameSpaces
//-----------------------------------------------------------
HRESULT
SymScope::GetNamespaces(
    ULONG32 cNameSpaces,    // [optional] number of entries pNameSpaces
    ULONG32 *pcNameSpaces,  // [optional, out] Maximum number of Namespace
    ISymUnmanagedNamespace *pNameSpaces[] // [optional] array to store namespaces into
    )
{
    HRESULT hr = NOERROR;
    unsigned i;
    UINT32 NameSpace;
    unsigned NameSpaceCount = 0;

    _ASSERTE(pcNameSpaces || (pNameSpaces && cNameSpaces));
    IfFalseGo(pcNameSpaces || (pNameSpaces && cNameSpaces), E_INVALIDARG);

    for (NameSpace = m_pData->m_pMethods[m_MethodEntry].StartUsing();
         NameSpace < m_pData->m_pMethods[m_MethodEntry].EndUsing();
         NameSpace++)
    {
        if (m_pData->m_pUsings[NameSpace].ParentScope() == m_ScopeEntry)
        {
            if (pNameSpaces && (NameSpaceCount < cNameSpaces) )
            {
                IfNullGo(pNameSpaces[NameSpaceCount] = NEW(SymReaderNamespace(this, m_pData, NameSpace)));
                pNameSpaces[NameSpaceCount]->AddRef();
            }
            NameSpaceCount++;
        }
    }
    if (pcNameSpaces)
    {
       *pcNameSpaces = NameSpaceCount;
    }
ErrExit:
    if (FAILED(hr) && pNameSpaces)
    {
        for (i = 0; (i < cNameSpaces) && (i < NameSpaceCount); i++)
        {
            RELEASE(pNameSpaces[i]);
        }
    }
    return hr;
}

/* ------------------------------------------------------------------------- *
 * SymReaderVar class
 * ------------------------------------------------------------------------- */

//-----------------------------------------------------------
// QueryInterface
//-----------------------------------------------------------
HRESULT
SymReaderVar::QueryInterface(
    REFIID riid,
    void **ppInterface
    )
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ISymUnmanagedVariable)
        *ppInterface = (ISymUnmanagedVariable*)this;
    else if (riid == IID_IUnknown)
        *ppInterface = (IUnknown*)(ISymUnmanagedVariable*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

//-----------------------------------------------------------
// GetName
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetName(
    ULONG32 cchName,    // [optional] Length of szName buffer
    ULONG32 *pcchName,  // [optional, out] Total size needed to return the name
    __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]      // [optional] Buffer to store the name into.
    )
{
    HRESULT hr = S_OK;

    // We must have at least one combination
    _ASSERTE(pcchName || (szName && cchName));
    IfFalseGo( (pcchName || (szName && cchName)), E_INVALIDARG );

    if (pcchName)
    {
        // Convert the UTF8 string to Wide
        *pcchName = (ULONG32) MultiByteToWideChar(CP_UTF8,
                                                0,
                                                (LPCSTR)&(m_pData->m_pStringsBytes[m_pData->m_pVars[m_VarEntry].Name()]),
                                                -1,
                                                0,
                                                NULL);

    }
    if (szName)
    {
        // Convert the UTF8 string to Wide
        MultiByteToWideChar(CP_UTF8,
                            0,
                            (LPCSTR)&(m_pData->m_pStringsBytes[m_pData->m_pVars[m_VarEntry].Name()]),
                            -1,
                            szName,
                            cchName);
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetAttributes
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetAttributes(
    ULONG32 *pRetVal // [out]
    )
{
    if (pRetVal == NULL)
        return E_INVALIDARG;

    *pRetVal = m_pData->m_pVars[m_VarEntry].Attributes();
    return S_OK;
}

//-----------------------------------------------------------
// GetSignature
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetSignature(
    ULONG32 cSig,   // Size of allocated buffer passed in (sig)
    ULONG32 *pcSig, // [optional, out] Total size needed to return the signature
    BYTE sig[] // [Optional] Signature
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(pcSig || sig);
    IfFalseGo( pcSig || sig, E_INVALIDARG );
    if (pcSig)
    {
        *pcSig = m_pData->m_pVars[m_VarEntry].SignatureSize();
    }
    if (sig)
    {
        cSig = min(m_pData->m_pVars[m_VarEntry].SignatureSize(), cSig);
        memcpy(sig, &m_pData->m_pBytes[m_pData->m_pVars[m_VarEntry].Signature()],cSig);
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetAddressKind
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetAddressKind(
    ULONG32 *pRetVal // [out]
    )
{
    HRESULT hr = S_OK;
    _ASSERTE(pRetVal);
    IfFalseGo( pRetVal, E_INVALIDARG );
    *pRetVal = m_pData->m_pVars[m_VarEntry].AddrKind();
ErrExit:
    return S_OK;
}

//-----------------------------------------------------------
// GetAddressField1
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetAddressField1(
    ULONG32 *pRetVal // [out]
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(pRetVal);
    IfFalseGo( pRetVal, E_INVALIDARG );

    *pRetVal = m_pData->m_pVars[m_VarEntry].Addr1();

ErrExit:

    return hr;
}

//-----------------------------------------------------------
// GetAddressField2
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetAddressField2(
    ULONG32 *pRetVal // [out]
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(pRetVal);
    IfFalseGo( pRetVal, E_INVALIDARG );

    *pRetVal = m_pData->m_pVars[m_VarEntry].Addr2();

ErrExit:

    return hr;
}

//-----------------------------------------------------------
// GetAddressField3
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetAddressField3(
    ULONG32 *pRetVal // [out]
    )
{
    HRESULT hr = S_OK;

    _ASSERTE(pRetVal);
    IfFalseGo( pRetVal, E_INVALIDARG );

    *pRetVal = m_pData->m_pVars[m_VarEntry].Addr3();

ErrExit:

    return hr;
}

//-----------------------------------------------------------
// GetStartOffset
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetStartOffset(
    ULONG32 *pRetVal
    )
{
    //
    // This symbol reader doesn't support variable sub-offsets.
    //
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetEndOffset
//-----------------------------------------------------------
HRESULT
SymReaderVar::GetEndOffset(
    ULONG32 *pRetVal
    )
{
    //
    // This symbol reader doesn't support variable sub-offsets.
    //
    return E_NOTIMPL;
}


/* ------------------------------------------------------------------------- *
 * SymReaderNamespace class
 * ------------------------------------------------------------------------- */

//-----------------------------------------------------------
// QueryInterface
//-----------------------------------------------------------
HRESULT
SymReaderNamespace::QueryInterface(
    REFIID riid,
    void** ppInterface
    )
{
    if (ppInterface == NULL)
        return E_INVALIDARG;

    if (riid == IID_ISymUnmanagedNamespace)
        *ppInterface = (ISymUnmanagedNamespace*)this;
    else if (riid == IID_IUnknown)
        *ppInterface = (IUnknown*)(ISymUnmanagedNamespace*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

//-----------------------------------------------------------
// GetName
//-----------------------------------------------------------
HRESULT
SymReaderNamespace::GetName(
    ULONG32 cchName,    // [optional] Chars available in szName
    ULONG32 *pcchName,  // [optional] Total size needed to return the name
    __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[]      // [optional] Location to store the name into.
    )
{
    HRESULT hr = S_OK;
    _ASSERTE(pcchName || (szName && cchName));
    IfFalseGo( (pcchName || (szName && cchName)), E_INVALIDARG );

    if (pcchName)
    {
        *pcchName = (ULONG32) MultiByteToWideChar(CP_UTF8,
                                                0,
                                                (LPCSTR)&(m_pData->m_pStringsBytes[m_pData->m_pUsings[m_NamespaceEntry].Name()]),
                                                -1,
                                                0,
                                                NULL);
    }
    if (szName)
    {
        MultiByteToWideChar(CP_UTF8,
                            0,
                            (LPCSTR)&(m_pData->m_pStringsBytes[m_pData->m_pUsings[m_NamespaceEntry].Name()]),
                            -1,
                            szName,
                            cchName);
    }

ErrExit:
    return hr;
}

//-----------------------------------------------------------
// GetNamespaces
//-----------------------------------------------------------
HRESULT
SymReaderNamespace::GetNamespaces(
    ULONG32 cNamespaces,
    ULONG32 *pcNamespaces,
    ISymUnmanagedNamespace* namespaces[]
    )
{
    // This symbol store doesn't support namespaces.
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// GetVariables
//-----------------------------------------------------------
HRESULT
SymReaderNamespace::GetVariables(
    ULONG32 cVariables,
    ULONG32 *pcVariables,
    ISymUnmanagedVariable *pVars[])
{
    // This symbol store doesn't support namespaces.
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
}


/* ------------------------------------------------------------------------- *
 * SequencePoint struct functions
 * ------------------------------------------------------------------------- */

//-----------------------------------------------------------
// IsWithin - Is the point given within this sequence point
//-----------------------------------------------------------
bool SequencePoint::IsWithin(
    ULONG32 line,
    ULONG32 column)
{
    // If the sequence point starts on the same line
    // Check the start column (if present)
    if (StartLine() == line)
    {
        if (0 < column && StartColumn() > column)
        {
            return false;
        }
    }

    // If the sequence point ends on the same line
    // Check the end column
    if (EndLine() == line)
    {
        if (EndColumn() < column)
        {
            return false;
        }
    }

    // Make sure the line is within this sequence point
    if (!((StartLine() <= line) && (EndLine() >= line)))
    {
        return false;
    }

    // Yep it's within this sequence point
    return true;

}

//-----------------------------------------------------------
// IsWithinLineOnly - Is the given line within this sequence point
//-----------------------------------------------------------
bool SequencePoint::IsWithinLineOnly(
        ULONG32 line)
{
    return ((StartLine() <= line) && (line <= EndLine()));
}

//-----------------------------------------------------------
// IsGreaterThan - Is the sequence point greater than the position
//-----------------------------------------------------------
bool SequencePoint::IsGreaterThan(
    ULONG32 line,
    ULONG32 column)
{
    return (StartLine() > line) ||
           (StartLine() == line && StartColumn() > column);
}

//-----------------------------------------------------------
// IsLessThan - Is the sequence point less than the position
//-----------------------------------------------------------
bool SequencePoint::IsLessThan
(
    ULONG32 line,
    ULONG32 column
)
{
    return (StartLine() < line) ||
           (StartLine() == line && StartColumn() < column);
}

//-----------------------------------------------------------
// IsUserLine - Is the sequence part of user code
//-----------------------------------------------------------
bool SequencePoint::IsUserLine()
{
    return StartLine() != CODE_WITH_NO_SOURCE;
}
