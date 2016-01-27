// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapMetadata.cpp
//

//
// Metadata zapping
// 
// ======================================================================================

#include "common.h"

#include "zapmetadata.h"

//-----------------------------------------------------------------------------
//
// ZapMetaData is the barebone ZapNode to save metadata scope
//
#ifdef CLR_STANDALONE_BINDER
static BYTE metadataStart [] =
{
    0x42, 0x53, 0x4a, 0x42, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x00, 0x00,
    0x76, 0x34, 0x2e, 0x30, 0x2e, 0x33, 0x30, 0x32, 0x31, 0x35, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00,
    0x6c, 0x00, 0x00, 0x00, 0x8c, 0x00, 0x00, 0x00, 0x23, 0x7e, 0x00, 0x00, 0xf8, 0x00, 0x00, 0x00,
    0x2c, 0x00, 0x00, 0x00, 0x23, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x73, 0x00, 0x00, 0x00, 0x00,
    0x24, 0x01, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x23, 0x55, 0x53, 0x00, 0x2c, 0x01, 0x00, 0x00,
    0x10, 0x00, 0x00, 0x00, 0x23, 0x47, 0x55, 0x49, 0x44, 0x00, 0x00, 0x00, 0x3c, 0x01, 0x00, 0x00,
    0xc0, 0x00, 0x00, 0x00, 0x23, 0x42, 0x6c, 0x6f, 0x62, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x02, 0x00, 0x00, 0x01, 0x05, 0x40, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0xfa, 0x01, 0x33,
    0x00, 0x16, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
    0x0b, 0x00, 0x06, 0x00, 0x01, 0x00, 0x04, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 
};

#define TableHeaderIndex 0x20
#define StringHeaderIndex 0x2C
#define UStringHeaderIndex 0x40
#define GUIDHeaderIndex 0x4C
#define BlobHeaderIndex 0x5C
#define TableStartIndex 0x6C
#define AssemblyRefRowsIndex 0x94
#define ModuleMvidIndex 0x9C
//#define DeclSecPermissionSetIndex 0xB4
#define AssemblyVersionIndex 0xBA
#define AssemblyFlagsIndex 0xC2
#define AssemblyPublicKeyIndex 0xC6
#define AssemblyShortNameIndex 0xC8
#define AssemblyCultureIndex 0xCA
#define AssemblyRefStartIndex 0xCC

#define AssemblyRefSize 0x14
#define AssemblyRefVersionOffset 0x0
#define AssemblyRefFlagsOffset 0x8
#define AssemblyRefTokenOffset 0xC
#define AssemblyRefShortNameOffset 0xE
#define AssemblyRefCultureOffset 0x10
#define AssemblyRefHashOffset 0x12



static BYTE stringStart [] =
{
    0x00, 0x3C, 0x4D, 0x6F, 0x64, 0x75, 0x6C, 0x65, 0x3E, 0x00,
};

#define StringHeapStartOffset 0xA

#define GuidSize 0x10
#define MaxGuidCount 20
#define GuidStartOffset 0x8

static BYTE guidStart [GuidStartOffset] = 
{
    0x03, 0x20
};


static BYTE blobStart[] =
{
    0x00,
    // PermissionSet, starts at 1, length 182 (0xB6) bytes
    0x80, 0xb4, 0x3c, 0x00, 0x50, 0x00, 0x65, 0x00, 0x72, 0x00, 0x6d, 0x00, 0x69, 0x00, 0x73, 0x00,
    0x73, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x6e, 0x00, 0x53, 0x00, 0x65, 0x00, 0x74, 0x00, 0x20, 0x00,
    0x63, 0x00, 0x6c, 0x00, 0x61, 0x00, 0x73, 0x00, 0x73, 0x00, 0x3d, 0x00, 0x22, 0x00, 0x53, 0x00,
    0x79, 0x00, 0x73, 0x00, 0x74, 0x00, 0x65, 0x00, 0x6d, 0x00, 0x2e, 0x00, 0x53, 0x00, 0x65, 0x00,
    0x63, 0x00, 0x75, 0x00, 0x72, 0x00, 0x69, 0x00, 0x74, 0x00, 0x79, 0x00, 0x2e, 0x00, 0x50, 0x00,
    0x65, 0x00, 0x72, 0x00, 0x6d, 0x00, 0x69, 0x00, 0x73, 0x00, 0x73, 0x00, 0x69, 0x00, 0x6f, 0x00,
    0x6e, 0x00, 0x53, 0x00, 0x65, 0x00, 0x74, 0x00, 0x22, 0x00, 0x0d, 0x00, 0x0a, 0x00, 0x76, 0x00,
    0x65, 0x00, 0x72, 0x00, 0x73, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x6e, 0x00, 0x3d, 0x00, 0x22, 0x00,
    0x31, 0x00, 0x22, 0x00, 0x0d, 0x00, 0x0a, 0x00, 0x55, 0x00, 0x6e, 0x00, 0x72, 0x00, 0x65, 0x00,
    0x73, 0x00, 0x74, 0x00, 0x72, 0x00, 0x69, 0x00, 0x63, 0x00, 0x74, 0x00, 0x65, 0x00, 0x64, 0x00,
    0x3d, 0x00, 0x22, 0x00, 0x74, 0x00, 0x72, 0x00, 0x75, 0x00, 0x65, 0x00, 0x22, 0x00, 0x2f, 0x00,
    0x3e, 0x00, 0x0d, 0x00, 0x0a, 0x00

};


#define BlobHeapStartOffset 0xB7

#define AssemblyNameStartIndex 0x10B
#define MaxAssemblyNameLength  0x14
#define MethodImplFlagsFromPRva(pRva) (*(USHORT*)((DWORD*)(pRva) + 1))

#define FieldRidPRvaSixByteFieldRVARecord(pRva) ((DWORD)(*(USHORT*)((DWORD*)(pRva) + 1)))
#define FieldRidPRvaEightByteFieldRVARecord(pRva) ((DWORD)(*(DWORD*)((DWORD*)(pRva) + 1)))
#endif

void ZapMetaData::SetMetaData(IUnknown * pEmit)
{
#ifndef CLR_STANDALONE_BINDER // TritonTBD: Commented out while we use a pre-defined copy of metadata.
    _ASSERTE(m_pEmit == NULL);
    _ASSERTE(pEmit != NULL);

    IfFailThrow(pEmit->QueryInterface(IID_IMetaDataEmit, (void **)&m_pEmit));
#endif
}

#ifdef CLR_STANDALONE_BINDER
void ZapMetaData::FixupMetaData()
{
    ULONG curSize;
    // fixup header information
    *((ULONG*)(&m_metadataHeap[TableHeaderIndex+4])) = m_metadataHeap.GetCount() - TableStartIndex;

    *((ULONG*)(&m_metadataHeap[StringHeaderIndex  ])) = m_metadataHeap.GetCount();
    *((ULONG*)(&m_metadataHeap[StringHeaderIndex+4])) = m_stringHeap.GetCount();
    curSize = m_metadataHeap.GetCount() + m_stringHeap.GetCount();

    *((ULONG*)(&m_metadataHeap[UStringHeaderIndex  ])) = curSize;
    *((ULONG*)(&m_metadataHeap[UStringHeaderIndex+4])) = GuidStartOffset;
   
    *((ULONG*)(&m_metadataHeap[GUIDHeaderIndex  ])) = curSize + GuidStartOffset;
    *((ULONG*)(&m_metadataHeap[GUIDHeaderIndex+4])) = m_guidHeap.GetCount() - GuidStartOffset;
    curSize += m_guidHeap.GetCount();

    *((ULONG*)(&m_metadataHeap[BlobHeaderIndex  ])) = curSize;
    *((ULONG*)(&m_metadataHeap[BlobHeaderIndex+4])) = m_blobHeap.GetCount();
    curSize += m_blobHeap.GetCount();

    m_bFixedUp = TRUE;

    _ASSERTE(curSize == m_dwSize);
}


// adds a string (defined in UTF-16) to the "string heap"
//    - converts the string to UTF-8
//    - length is either string length in WCHAR or -1 (assumes zero terminated string)
//    - (tries to avoid duplicate strings)
//    - returns the starting offset of the string
//      (0 for all errors, including "empty string"
//    - updates m_cbString (if not a duplicate)

ULONG ZapMetaData::AddString(__in_z LPWSTR pName, __in int length)
{

    if (pName == NULL || *pName == (WCHAR) 0 || length < -1)
        return 0;

    int cbUtf8Len;

    //determine the length
    cbUtf8Len = WideCharToMultiByte(CP_UTF8, 0, //to UTF-8, no flags
                                    pName, length,  // incoming argument, zero terminated
                                    NULL, // target
                                    0,
                                    NULL,
                                    NULL);

    if (cbUtf8Len == 0)
        return 0;

    COUNT_T cbString = m_stringHeap.GetCount();
    m_stringHeap.SetCount(cbString + cbUtf8Len + 1);

    cbUtf8Len = WideCharToMultiByte(CP_UTF8, 0, //to UTF-8, no flags
                                    pName, length,  // incoming argument, zero terminated
                                    (LPSTR) &m_stringHeap[cbString], // target
                                    cbUtf8Len, // target buffer size
                                    NULL,
                                    NULL);

    // check for duplicates
    BYTE * pCur = &m_stringHeap[1];
    BYTE * pEnd = &m_stringHeap[cbString];
    while (pCur + cbUtf8Len < pEnd) {
        if (!memcmp(pCur, pEnd, cbUtf8Len) && *(pCur+cbUtf8Len) == 0) {
            // same string, return startindex of existing string
            m_stringHeap.SetCount(cbString);
            return (ULONG) (pCur - &m_stringHeap[0]);
        }
        // not the same string, skip to next string
        while (pCur < pEnd && *pCur++ != 0)
            ;
    }

    // this is a "new" string
    // zero terminate string heap entry
    m_stringHeap[cbString + cbUtf8Len] = 0;

    return cbString;
}

ULONG ZapMetaData::AddString(LPCSTR pName, int length)
{

    if (pName == NULL || *pName == 0 || length < -1)
        return 0;

    unsigned cbUtf8Len = 0;

    if (length == -1) {
        CHAR * p = (CHAR*) pName;
        while (*p++ != 0) {
            cbUtf8Len++;
        }
    }
    else
        cbUtf8Len = (unsigned)length;

    if (cbUtf8Len == 0)
        return 0;

    COUNT_T cbString = m_stringHeap.GetCount();

    // check for duplicates
    BYTE * pCur = &m_stringHeap[1];
    BYTE * pEnd = &m_stringHeap[cbString];
    while (pCur + cbUtf8Len < pEnd) {
        if (!memcmp(pCur, pName, cbUtf8Len) && *(pCur+cbUtf8Len) == 0) {
            // same string, return startindex of existing string
            return (ULONG) (pCur - &m_stringHeap[0]);
        }
        // not the same string, skip to next string
        while (pCur < pEnd && *pCur++ != 0)
            ;
    }

    m_stringHeap.SetCount(cbString + cbUtf8Len + 1);

    // this is a "new" string
    memcpy (&m_stringHeap[cbString], pName, cbUtf8Len);
    m_stringHeap[cbString + cbUtf8Len] = 0;

    return cbString;
}


#if 0 // turns out, an assembly name can end with .exe or .dll and we shouldn't strip it...
      // for now I leave in the StripExtension functions just in case we will need it again.
int ZapMetaData::StripExtension(LPWSTR pName)
{
    WCHAR *pCur = pName;
    int cChar = 0;

    // strip last file extension

    // move to the end of string
    while (*pCur != (WCHAR) 0)
    {
        pCur++; cChar++;
    }

    if (cChar > 4 && pName[cChar-4] == W('.')) {
        if ((pName[cChar-3] == W('e') && pName[cChar-2] == W('x') && pName[cChar-1] == W('e')) ||
            (pName[cChar-3] == W('d') && pName[cChar-2] == W('l') && pName[cChar-1] == W('l')))
        {
            cChar -= 4;
        }
    }
    return cChar;
}

int ZapMetaData::StripExtension(LPCSTR pName)
{
    CHAR *pCur = (CHAR*)pName;
    int cChar = 0;

    // strip last file extension

    // move to the end of string
    while (*pCur != (CHAR) 0)
    {
        pCur++; cChar++;
    }

    if (cChar > 4 && pName[cChar-4] == '.') {
        if ((pName[cChar-3] == 'e' && pName[cChar-2] == 'x' && pName[cChar-1] == 'e') ||
            (pName[cChar-3] == 'd' && pName[cChar-2] == 'l' && pName[cChar-1] == 'l'))
        {
            cChar -= 4;
        }
    }
    return cChar;
}
#endif

ULONG ZapMetaData::AddBlob(LPCVOID blob, COUNT_T cbBlob)
{
    _ASSERTE(blob != NULL);
    _ASSERTE(cbBlob > 0);
    COUNT_T startValue = m_blobHeap.GetCount();
    _ASSERTE(startValue > 0);
    ULONG cbSize = 1;
    
    if (cbBlob <= 0x7F)
    {
        m_blobHeap.SetCount(startValue + 1 + cbBlob);
        m_blobHeap[startValue] = (BYTE) cbBlob;
        memcpy(&m_blobHeap[startValue+1], blob, cbBlob);
    }
    else if (cbBlob < 0x3FFF)
    {
        m_blobHeap.SetCount(startValue + 2 + cbBlob);
        m_blobHeap[startValue  ] = (BYTE) (((cbBlob >> 8) & 0x3F) | 0x80);
        m_blobHeap[startValue+1] = (BYTE) (cbBlob & 0xFF);
        memcpy(&m_blobHeap[startValue+2], blob, cbBlob);
    }
    else
    {
        _ASSERTE(!"NYI - large blob heaps");
    }

    return startValue;
}

void ZapMetaData::SetAssembly(
                     __in_z LPWSTR name,
                     __in_z LPWSTR culture,
                     NativeAssemblyData *pNad)
{
    if (name == NULL || *name == (WCHAR) 0)
        return;

    ULONG nameStart = AddString(name, -1);

    ULONG cultureStart = 0;
    if (culture != NULL)
        cultureStart = AddString(culture, -1);

    CorAssemblyFlags flags = pNad->m_flags;

    if (pNad->m_cbPublicKey > 0 && pNad->m_publicKey != NULL)
    {
        ULONG blobStart = AddBlob(pNad->m_publicKey, pNad->m_cbPublicKey);
        if (blobStart > 0)
        {
            flags = (CorAssemblyFlags)(flags | afPublicKey);
            *((USHORT*) (&m_metadataHeap[AssemblyPublicKeyIndex])) = (USHORT) blobStart;
        }
    }
    
    *((CorAssemblyFlags *) (&m_metadataHeap[AssemblyFlagsIndex])) = flags;

    *((USHORT*) (&m_metadataHeap[AssemblyVersionIndex  ])) = pNad->m_majorVersion;
    *((USHORT*) (&m_metadataHeap[AssemblyVersionIndex+2])) = pNad->m_minorVersion;
    *((USHORT*) (&m_metadataHeap[AssemblyVersionIndex+4])) = pNad->m_buildNumber;
    *((USHORT*) (&m_metadataHeap[AssemblyVersionIndex+6])) = pNad->m_revisionNumber;
    *((USHORT*) (&m_metadataHeap[AssemblyShortNameIndex])) = (USHORT) nameStart;
    *((USHORT*) (&m_metadataHeap[AssemblyCultureIndex  ])) = (USHORT) cultureStart;

    SetMVIDOfModule(&NGEN_IMAGE_MVID);
}

void ZapMetaData::SetMVIDOfModule(LPCVOID mvid)
{
#define cbMVID 16
   //copy MVID to the GUIDHeap and fix up the Module entry
    if (mvid != NULL)
    {
        COUNT_T cbGuid = m_guidHeap.GetCount();
        m_guidHeap.SetCount(cbGuid + cbMVID);
        BYTE * dst = &m_guidHeap[cbGuid];
        memcpy(dst, mvid, cbMVID);
        // fix up module entry (1-based index into GUID heap)
        // NOTE: this version has just one buffer for US and GUID heap !!

        *((USHORT *)(&m_metadataHeap[ModuleMvidIndex])) =
                    (USHORT) ((cbGuid - GuidStartOffset)/cbMVID) + 1;
    }
}

void ZapMetaData::SetAssemblyReference(
                              __in_z LPWSTR name,
                              __in_z LPWSTR culture,
                              NativeAssemblyData *pNad)
{
    int strongNameLevel = 0; // 0: no strong name, 1: publicKeyToken, 2: publicKey (very unusual)
    BYTE *pKey = NULL;
    ULONG cbKey = 0;

    if (name == NULL || *name == (WCHAR) 0)
        return;

    if (pNad->m_cbPublicKeyToken > 0 && pNad->m_publicKeyToken != NULL) {
        strongNameLevel = 1;
        cbKey = pNad->m_cbPublicKeyToken;
        pKey = pNad->m_publicKeyToken;
    }
    else if (pNad->m_cbPublicKey > 0 && pNad->m_publicKey != NULL) {
        strongNameLevel = 2;
        cbKey = pNad->m_cbPublicKey;
        pKey = pNad->m_publicKey;
    }

    _ASSERTE(pKey == NULL || strongNameLevel == 1 || cbKey > 8);

    COUNT_T cbTable = m_metadataHeap.GetCount();
    m_metadataHeap.SetCount(cbTable + AssemblyRefSize);

    USHORT * pAssemblyRef = (USHORT*) (&m_metadataHeap[cbTable]);
    memset(pAssemblyRef, 0, AssemblyRefSize);

    ULONG nameStart = AddString(name, -1);

    ULONG cultureStart = 0;
    if (culture != NULL)
        cultureStart = AddString(culture, -1);

    pAssemblyRef[AssemblyRefVersionOffset  ] = pNad->m_majorVersion;
    pAssemblyRef[AssemblyRefVersionOffset+1] = pNad->m_minorVersion;
    pAssemblyRef[AssemblyRefVersionOffset+2] = pNad->m_buildNumber;
    pAssemblyRef[AssemblyRefVersionOffset+3] = pNad->m_revisionNumber;

    // all offset constants are byte offsets, convert them into "short offsets"
    if (cbKey > 0)
    {
        pAssemblyRef[AssemblyRefTokenOffset/2] = (USHORT) AddBlob(pKey, cbKey);
    }

    CorAssemblyFlags flags = pNad->m_flags;

    if (strongNameLevel == 2)
    {
        flags = (CorAssemblyFlags)(flags | afPublicKey);
    }

    *((CorAssemblyFlags *) (&pAssemblyRef[AssemblyRefFlagsOffset/2])) = flags;

    pAssemblyRef[AssemblyRefShortNameOffset/2] = (USHORT)nameStart;
    pAssemblyRef[AssemblyRefCultureOffset/2] = (USHORT)cultureStart;
    pAssemblyRef[AssemblyRefHashOffset/2] = 0;

    (*((USHORT*)&m_metadataHeap[AssemblyRefRowsIndex]))++;
}

ZapMetaData::ZapMetaData()
{
    m_bFixedUp = FALSE;

    _ASSERTE(AssemblyRefStartIndex == sizeof(metadataStart));
    m_metadataHeap.SetCount(sizeof(metadataStart));
    memcpy(&m_metadataHeap[0], metadataStart, sizeof(metadataStart));

    _ASSERTE(StringHeapStartOffset == sizeof(stringStart));
    m_stringHeap.SetCount(sizeof(stringStart));
    memcpy(&m_stringHeap[0], stringStart, sizeof(stringStart));

    _ASSERTE(GuidStartOffset == sizeof(guidStart));
    m_guidHeap.SetCount(sizeof(guidStart));
    memcpy(&m_guidHeap[0], guidStart, sizeof(guidStart));

    _ASSERTE(BlobHeapStartOffset == sizeof(blobStart));
    m_blobHeap.SetCount(sizeof(blobStart));
    memcpy(&m_blobHeap[0], blobStart, sizeof(blobStart));
}
#endif

DWORD ZapMetaData::GetSize()
{
    if (m_dwSize == 0)
    {
#ifdef CLR_STANDALONE_BINDER 
        // round up tables (divisible by 4)
        // for reproducibility pad with 0 bytes
        while (m_metadataHeap.GetCount() & 3)
            m_metadataHeap.Append(0);

        while (m_stringHeap.GetCount() & 3)
            m_stringHeap.Append(0);

        while (m_guidHeap.GetCount() & 3)
            m_guidHeap.Append(0);

        while (m_blobHeap.GetCount() & 3)
            m_blobHeap.Append(0);

       m_dwSize = m_metadataHeap.GetCount() + m_stringHeap.GetCount() + m_guidHeap.GetCount() + m_blobHeap.GetCount();
#else
        IfFailThrow(m_pEmit->GetSaveSize(cssAccurate, &m_dwSize));
#endif
        _ASSERTE(m_dwSize != 0);
    }
    return m_dwSize;
}

void ZapMetaData::Save(ZapWriter * pZapWriter)
{
#ifdef CLR_STANDALONE_BINDER // TritonTBD
    ULONG cbWritten;
    FixupMetaData();

    ((IStream*)pZapWriter)->Write(&m_metadataHeap[0], m_metadataHeap.GetCount(), &cbWritten);
    _ASSERTE(cbWritten == m_metadataHeap.GetCount());

    ((IStream*)pZapWriter)->Write(&m_stringHeap[0], m_stringHeap.GetCount(), &cbWritten);
    _ASSERTE(cbWritten == m_stringHeap.GetCount());

    ((IStream*)pZapWriter)->Write(&m_guidHeap[0], m_guidHeap.GetCount(), &cbWritten);
    _ASSERTE(cbWritten == m_guidHeap.GetCount());

    ((IStream*)pZapWriter)->Write(&m_blobHeap[0], m_blobHeap.GetCount(), &cbWritten);
    _ASSERTE(cbWritten == m_blobHeap.GetCount());
#else
    IfFailThrow(m_pEmit->SaveToStream(pZapWriter, 0));
#endif
}

//-----------------------------------------------------------------------------
//
// ZapILMetaData copies both the metadata and IL to the NGEN image.
//

void ZapILMetaData::Save(ZapWriter * pZapWriter)
{
#ifdef CLR_STANDALONE_BINDER
    // Make a copy IL metadata, so we can fixup RVAs.
    LPVOID metaDataCopy = new BYTE[m_metaDataSize];
    memcpy(metaDataCopy, m_metaDataStart, m_metaDataSize);

    // Fixup RVA of MethodDef records in metadata
    ULONG *pRva = (ULONG *)((LPBYTE)metaDataCopy + m_firstMethodRvaOffset);
    for (DWORD rid = 1; rid <= m_methodDefCount; rid++, pRva = (ULONG *)((LPBYTE)pRva + m_methodDefRecordSize))
    {
        _ASSERTE((LPBYTE)pRva > (LPBYTE)metaDataCopy && (LPBYTE)pRva + m_methodDefRecordSize <= (LPBYTE)metaDataCopy + m_metaDataSize);
        ULONG rva = *pRva;
        USHORT flags = MethodImplFlagsFromPRva(pRva);

        if (!IsMiIL(flags) || (rva == 0))
            continue;

        // Set the actual RVA of the method
        const ILMethod * pILMethod = m_ILMethods.LookupPtr(TokenFromRid(rid, mdtMethodDef));

        *pRva = (pILMethod != NULL) ? pILMethod->m_pIL->GetRVA() : 0;
    }

    // Fixup RVA of FieldRVA records in metadata
    pRva = (ULONG *)((LPBYTE)metaDataCopy + m_firstFieldRvaOffset);
    for (DWORD rid = 1; rid <= m_fieldRvaCount; rid++, pRva = (ULONG *)((LPBYTE)pRva + m_fieldRvaRecordSize))
    {
        _ASSERTE((LPBYTE)pRva > (LPBYTE)metaDataCopy && (LPBYTE)pRva + m_fieldRvaRecordSize <= (LPBYTE)metaDataCopy + m_metaDataSize);

        // field rid associated with this FieldRVA field.
        DWORD ridField;
        if (m_fieldRvaRecordSize == 6)
        {
            ridField = FieldRidPRvaSixByteFieldRVARecord(pRva);
        }
        else if (m_fieldRvaRecordSize == 8)
        {
            ridField = FieldRidPRvaEightByteFieldRVARecord(pRva);
        }
        else
        {
            ridField = 0;
            _ASSERTE(!"FieldRVA row of invalid size.");
        }

        mdToken tkField = TokenFromRid(ridField, mdtFieldDef);

        ULONG rva;
        if (this->m_fieldToRVAMapping.Lookup(tkField, &rva))
        {
            *pRva = rva;
        }
        else
        {
            // Invalid RVA. This should cause reliable runtime exceptions instead of anything more unpredictable.
            // This can happen for fields on types for which could not be loaded in the binder for any reason
            // In most cases, this shouldn't be a problem due to the cases where existing compilers will generate
            // field rvas in the triton scenario, but we there could be problems if the set of types loadable at
            // runtime is greater than the set of types loadable by the CTL binder, and those types have RVA static
            // fields.
            // We should be falling back to using the IL image if this happens. 
            IfFailThrow(COR_E_TYPELOAD);
            *pRva = 0xFFFFFFFF;
        }
    }

    ULONG cbWritten;
    ((IStream*)pZapWriter)->Write(metaDataCopy, m_metaDataSize, &cbWritten);
    _ASSERTE(cbWritten == m_metaDataSize);

    delete[] metaDataCopy;
#else // CLR_STANDALONGE_BINDER
    IMDInternalImport * pMDImport = m_pImage->m_pMDImport;

    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtMethodDef);

    mdMethodDef md;
    while (pMDImport->EnumNext(&hEnum, &md))
    {
        DWORD flags;
        ULONG rva;
        IfFailThrow(pMDImport->GetMethodImplProps(md, &rva, &flags));

        if (!IsMiIL(flags) || (rva == 0))
            continue;

        // Set the actual RVA of the method
        const ILMethod * pILMethod = m_ILMethods.LookupPtr(md);

        IfFailThrow(m_pEmit->SetRVA(md, (pILMethod != NULL) ? pILMethod->m_pIL->GetRVA() : 0));
    }

    if (IsReadyToRunCompilation())
    {
        HENUMInternalHolder hEnum(pMDImport);
        hEnum.EnumAllInit(mdtFieldDef);

        mdFieldDef fd;
        while (pMDImport->EnumNext(&hEnum, &fd))
        {
            DWORD dwRVA = 0;
            if (pMDImport->GetFieldRVA(fd, &dwRVA) == S_OK)
            {
                PVOID pData = NULL;
                DWORD cbSize = 0;
                DWORD cbAlignment = 0;

                m_pImage->m_pPreloader->GetRVAFieldData(fd, &pData, &cbSize, &cbAlignment);

                ZapRVADataNode * pRVADataNode = m_rvaData.Lookup(pData);
                m_pEmit->SetRVA(fd, pRVADataNode->GetRVA());
            }
        }
    }
    else
    {
       ZapImage::GetImage(pZapWriter)->m_pPreloader->SetRVAsForFields(m_pEmit);
    }

    ZapMetaData::Save(pZapWriter);
#endif // CLR_STANDALONGE_BINDER
}

ZapRVADataNode * ZapILMetaData::GetRVAField(void * pData)
{
    ZapRVADataNode * pRVADataNode = m_rvaData.Lookup(pData);

    if (pRVADataNode == NULL)
    {
        pRVADataNode = new (m_pImage->GetHeap()) ZapRVADataNode(pData);

        m_rvaData.Add(pRVADataNode);
    }

    return pRVADataNode;
}

struct RVAField
{
    PVOID pData;
    DWORD cbSize;
    DWORD cbAlignment;
};

// Used by qsort
int __cdecl RVAFieldCmp(const void * a_, const void * b_)
{
    RVAField * a = (RVAField *)a_;
    RVAField * b = (RVAField *)b_;

    if (a->pData != b->pData)
    {
        return (a->pData > b->pData) ? 1 : -1;
    }

    return 0;
}

void ZapILMetaData::CopyRVAFields()
{
    IMDInternalImport * pMDImport = m_pImage->m_pMDImport;

    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtFieldDef);

    SArray<RVAField> fields;

    mdFieldDef fd;
    while (pMDImport->EnumNext(&hEnum, &fd))
    {
        DWORD dwRVA = 0;
        if (pMDImport->GetFieldRVA(fd, &dwRVA) == S_OK)
        {
            RVAField field;
            m_pImage->m_pPreloader->GetRVAFieldData(fd, &field.pData, &field.cbSize, &field.cbAlignment);
            fields.Append(field);
        }
    }

    if (fields.GetCount() == 0)
        return;

    // Managed C++ binaries depend on the order of RVA fields
    qsort(&fields[0], fields.GetCount(), sizeof(RVAField), RVAFieldCmp);

    for (COUNT_T i = 0; i < fields.GetCount(); i++)
    {
        RVAField field = fields[i];

        ZapRVADataNode * pRVADataNode = GetRVAField(field.pData);

        // Handle overlapping fields by reusing blobs based on the address, and just updating size and alignment.
        pRVADataNode->UpdateSizeAndAlignment(field.cbSize, field.cbAlignment);

        if (!pRVADataNode->IsPlaced())
             m_pImage->m_pReadOnlyDataSection->Place(pRVADataNode);
    }
}

void ZapILMetaData::CopyIL()
{
    // The IL is emited into NGen image in the following priority order:
    //  1. Public inlineable method (may be needed by JIT inliner)
    //  2. Generic method (may be needed to compile non-NGened instantiations)
    //  3. Other potentially warm instances (private inlineable methods, methods that failed to NGen)
    //  4. Everything else (should be touched in rare scenarios like reflection or profiling only)

    SArray<ZapBlob *> priorityLists[CORCOMPILE_ILREGION_COUNT];

#ifndef CLR_STANDALONE_BINDER
    IMDInternalImport * pMDImport = m_pImage->m_pMDImport;

    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtMethodDef);
#endif

    //
    // Build the list for each priority in first pass, and then place
    // the IL blobs in each list. The two passes are needed because of 
    // interning of IL blobs (one IL blob can be on multiple lists).
    //

#ifndef CLR_STANDALONE_BINDER
    mdMethodDef md;
    while (pMDImport->EnumNext(&hEnum, &md))
#else
    mdMethodDef mdMax = TokenFromRid(m_methodDefCount, mdtMethodDef);
    for (mdMethodDef md = TokenFromRid(1, mdtMethodDef); md <= mdMax; md++)
#endif
    {
        const ILMethod * pILMethod = m_ILMethods.LookupPtr(md);

        if (pILMethod == NULL)
            continue;

        CorCompileILRegion region = m_pImage->m_pPreloader->GetILRegion(md);
        _ASSERTE(region < CORCOMPILE_ILREGION_COUNT);

        // Preallocate space to avoid wasting too much time by reallocations
        if (priorityLists[region].IsEmpty())
            priorityLists[region].Preallocate(m_ILMethods.GetCount() / 16);

        priorityLists[region].Append(pILMethod->m_pIL);
    }

    for (int iList = 0; iList < CORCOMPILE_ILREGION_COUNT; iList++)
    {
        SArray<ZapBlob *> & priorityList = priorityLists[iList];

        // Use just one section for IL for now. Once the touches of IL for method preparation are fixed change it to:
        // ZapVirtualSection * pSection = (iList == CORCOMPILE_ILREGION_COLD) ? m_pImage->m_pColdILSection : m_pImage->m_pILSection;

        ZapVirtualSection * pSection = m_pImage->m_pILSection;

        COUNT_T nBlobs = priorityList.GetCount();
        for (COUNT_T iBlob = 0; iBlob < nBlobs; iBlob++)
        {
            ZapBlob * pIL = priorityList[iBlob];
            if (!pIL->IsPlaced())
                pSection->Place(pIL);
        }
    }
}

void ZapILMetaData::CopyMetaData()
{
#if defined(CLR_STANDALONE_BINDER)
    // Triton TBD
#else // 
    //
    // Copy metadata from IL image and open it so we can update IL rva's
    //

    COUNT_T cMeta;
    const void *pMeta = m_pImage->m_ModuleDecoder.GetMetadata(&cMeta);

    IMetaDataDispenserEx * pMetaDataDispenser = m_pImage->m_zapper->m_pMetaDataDispenser;

    //
    // Transfer the metadata version string from IL image to native image
    //
    LPCSTR pRuntimeVersionString;
    IfFailThrow(GetImageRuntimeVersionString((PVOID)pMeta, &pRuntimeVersionString));

    SString ssRuntimeVersion;
    ssRuntimeVersion.SetUTF8(pRuntimeVersionString);

    BSTRHolder strVersion(SysAllocString(ssRuntimeVersion.GetUnicode()));

    VARIANT versionOption;
    V_VT(&versionOption) = VT_BSTR;
    V_BSTR(&versionOption) = strVersion;
    IfFailThrow(pMetaDataDispenser->SetOption(MetaDataRuntimeVersion, &versionOption));
    
    // Preserve local refs. WinMD adapter depends on them at runtime.
    VARIANT preserveLocalRefsOption;
    V_VT(&preserveLocalRefsOption) = VT_UI4;
    V_UI4(&preserveLocalRefsOption) = MDPreserveLocalTypeRef | MDPreserveLocalMemberRef;
    IfFailThrow(pMetaDataDispenser->SetOption(MetaDataPreserveLocalRefs, &preserveLocalRefsOption));
    
    // ofNoTransform - Get the raw metadata for WinRT, not the adapter view
    HRESULT hr = pMetaDataDispenser->OpenScopeOnMemory(pMeta, cMeta,
                                                       ofWrite | ofNoTransform,
                                                       IID_IMetaDataEmit,
                                                       (IUnknown **) &m_pEmit);
    if (hr == CLDB_E_BADUPDATEMODE)
    {
        // This must be incrementally-updated metadata. It needs to be opened
        // specially.
        VARIANT incOption;
        V_VT(&incOption) = VT_UI4;
        V_UI4(&incOption) = MDUpdateIncremental;
        IfFailThrow(pMetaDataDispenser->SetOption(MetaDataSetUpdate, &incOption));

        hr = pMetaDataDispenser->OpenScopeOnMemory(pMeta, cMeta,
                                                   ofWrite | ofNoTransform,
                                                   IID_IMetaDataEmit,
                                                   (IUnknown **) &m_pEmit);
    }

    // Check the result of OpenScopeOnMemory()
    IfFailThrow(hr);

    if (!IsReadyToRunCompilation())
    {
        // Communicate the profile data to the meta data emitter so it can hot/cold split it
        NonVMComHolder<IMetaDataCorProfileData> pIMetaDataCorProfileData;
        IfFailThrow(m_pEmit->QueryInterface(IID_IMetaDataCorProfileData,
                                            (void**)&pIMetaDataCorProfileData));

        // unless we're producing an instrumented version - the IBC logging for meta data doesn't
        // work for the hot/cold split version.
        if (m_pImage->m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_BBINSTR)
            IfFailThrow(pIMetaDataCorProfileData->SetCorProfileData(NULL));
        else
            IfFailThrow(pIMetaDataCorProfileData->SetCorProfileData(m_pImage->GetProfileData()));
    }

    // If we are ngening with the tuning option, the IBC data that is
    // generated gets reordered and may be  inconsistent with the
    // metadata in the original IL image. Let's just skip that case.
    if (!(m_pImage->m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_BBINSTR))
    {
        // Communicate the reordering option for saving
        NonVMComHolder<IMDInternalMetadataReorderingOptions> pIMDInternalMetadataReorderingOptions;
        IfFailThrow(m_pEmit->QueryInterface(IID_IMDInternalMetadataReorderingOptions,
                                            (void**)&pIMDInternalMetadataReorderingOptions));
        IfFailThrow(pIMDInternalMetadataReorderingOptions->SetMetaDataReorderingOptions(ReArrangeStringPool));
    }
#endif // CLR_STANDALONE_BINDER
}

// Emit IL for a method def into the ngen image
void ZapILMetaData::EmitMethodIL(mdMethodDef md)
{
#ifdef CLR_STANDALONE_BINDER
    const ULONG *pRva = (ULONG *)((LPBYTE)m_metaDataStart + m_firstMethodRvaOffset + (RidFromToken(md) - 1) * m_methodDefRecordSize);
    _ASSERTE((LPBYTE)pRva > (LPBYTE)m_metaDataStart && (LPBYTE)pRva + m_methodDefRecordSize <= (LPBYTE)m_metaDataStart + m_metaDataSize);
    DWORD flags = MethodImplFlagsFromPRva(pRva);
    ULONG rva = *pRva;
#else
    DWORD flags;
    ULONG rva;
    IfFailThrow(m_pImage->m_pMDImport->GetMethodImplProps(md, &rva, &flags));
#endif

    if (!IsMiIL(flags) || (rva == 0))
        return;

#ifndef BINDER
    if (!m_pImage->m_ModuleDecoder.CheckILMethod(rva))
        IfFailThrow(COR_E_BADIMAGEFORMAT); // BFA_BAD_IL_RANGE
#endif

    PVOID pMethod = (PVOID)m_pImage->m_ModuleDecoder.GetRvaData(rva);

    SIZE_T cMethod = PEDecoder::ComputeILMethodSize((TADDR)pMethod);

    //
    // Emit copy of IL method in native image.
    //
    ZapBlob * pIL = m_blobs.Lookup(ZapBlob::SHashKey(pMethod, cMethod));

    if (pIL == NULL)
    {
        pIL = new (m_pImage->GetHeap()) ILBlob(pMethod, cMethod);

        m_blobs.Add(pIL);
    }

    ILMethod ilMethod;
    ilMethod.m_md = md;
    ilMethod.m_pIL = pIL;
    m_ILMethods.Add(ilMethod);
}

#ifdef CLR_STANDALONE_BINDER
DWORD ZapILMetaData::GetSize()
{
    return m_metaDataSize;
}

void ZapILMetaData::EmitFieldRVA(mdToken fieldDefToken, RVA fieldRVA)
{
    this->m_fieldToRVAMapping.Add(fieldDefToken, fieldRVA);
}
#endif
