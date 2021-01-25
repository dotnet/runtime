// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "portable_pdb.h"
#include <time.h>
#include "assembler.h"

//*****************************************************************************
// Document
//*****************************************************************************
Document::Document()
{
    m_name = NULL;
    m_token = mdDocumentNil;
}

Document::~Document()
{
    if (m_name)
    {
        delete[] m_name;
        m_name = NULL;
    }
    m_token = mdDocumentNil;
};

char* Document::GetName()
{
    return m_name;
}

void Document::SetName(char* name)
{
    m_name = new char[strlen(name) + 1];
    strcpy_s(m_name, strlen(name) + 1, name);
}

mdDocument Document::GetToken()
{
    return m_token;
}

void Document::SetToken(mdDocument token)
{
    m_token = token;
}


//*****************************************************************************
// PortablePdbWriter
//*****************************************************************************
PortablePdbWriter::PortablePdbWriter()
{
    m_pdbStream.id.pdbGuid = { 0 };
    m_pdbStream.id.pdbTimeStamp = 0;
    m_pdbStream.entryPoint = mdMethodDefNil;
    m_pdbStream.referencedTypeSystemTables = 0UL;
    m_pdbStream.typeSystemTableRows = new ULONG[TBL_COUNT];
    m_pdbStream.typeSystemTableRowsSize = 0;
    m_currentDocument = NULL;
    m_pdbEmitter = NULL;
}

PortablePdbWriter::~PortablePdbWriter()
{
    if (m_pdbEmitter != NULL)
    {
        m_pdbEmitter->Release();
        m_pdbEmitter = NULL;
    }
    if (m_pdbStream.typeSystemTableRows != NULL)
    {
        delete[] m_pdbStream.typeSystemTableRows;
        m_pdbStream.typeSystemTableRows = NULL;
    }

    m_documentList.RESET(true);
}

HRESULT PortablePdbWriter::Init(IMetaDataDispenserEx2* mdDispenser)
{
    HRESULT hr = S_OK;
    if (m_pdbEmitter != NULL)
    {
        m_pdbEmitter->Release();
        m_pdbEmitter = NULL;
    }
    m_currentDocument = NULL;
    m_documentList.RESET(true);

    memset(m_pdbStream.typeSystemTableRows, 0, sizeof(ULONG) * TBL_COUNT);
    time_t now;
    time(&now);
    m_pdbStream.id.pdbTimeStamp = (ULONG)now;
    hr = CoCreateGuid(&m_pdbStream.id.pdbGuid);

    if (FAILED(hr)) goto exit;

    hr = mdDispenser->DefinePortablePdbScope(
        CLSID_CorMetaDataRuntime,
        0,
        IID_IMetaDataEmit3,
        (IUnknown**)&m_pdbEmitter);
exit:
    return hr;
}

IMetaDataEmit3* PortablePdbWriter::GetEmitter()
{
    return m_pdbEmitter;
}

GUID* PortablePdbWriter::GetGuid()
{
    return &m_pdbStream.id.pdbGuid;
}

ULONG PortablePdbWriter::GetTimestamp()
{
    return m_pdbStream.id.pdbTimeStamp;
}

Document* PortablePdbWriter::GetCurrentDocument()
{
    return m_currentDocument;
}

HRESULT PortablePdbWriter::BuildPdbStream(IMetaDataEmit3* peEmitter, mdMethodDef entryPoint)
{
    HRESULT hr = S_OK;

    m_pdbStream.entryPoint = entryPoint;

    if (FAILED(hr = peEmitter->GetReferencedTypeSysTables(
        &m_pdbStream.referencedTypeSystemTables,
        m_pdbStream.typeSystemTableRows,
        TBL_COUNT,
        &m_pdbStream.typeSystemTableRowsSize))) goto exit;

    if (FAILED(hr = m_pdbEmitter->DefinePdbStream(&m_pdbStream))) goto exit;

exit:
    return hr;
}

HRESULT PortablePdbWriter::DefineDocument(char* name, GUID* language)
{
    HRESULT hr = S_OK;
    Document* document = NULL;
    unsigned i = 0;

    // did we already add a document with this name
    while ((document = m_documentList.PEEK(i++)) != NULL)
    {
        if (!strcmp(name, document->GetName()))
            break;
    }

    if (document)
    {
        // found one in the list
        // set it as current
        m_currentDocument = document;
    }
    else
    {
        // define a new document
        document = new Document();
        // save the document name, the 'name' parameter will be overriten - tokenized
        document->SetName(name);

        // TODO: make use of hash algorithm and hash value
        GUID    hashAlgorithmUnknown = { 0 };
        BYTE*   hashValue = NULL;
        ULONG   cbHashValue = 0;
        mdDocument docToken = mdDocumentNil;

        if (FAILED(hr = m_pdbEmitter->DefineDocument(
            name, // will be tokenized 
            &hashAlgorithmUnknown,
            hashValue,
            cbHashValue,
            language,
            &docToken)))
        {
            delete document;
            m_currentDocument = NULL;
            hr = E_FAIL;
        }
        else
        {
            document->SetToken(docToken);
            m_currentDocument = document;
            m_documentList.PUSH(document);
        }
    }

    return hr;
}

HRESULT PortablePdbWriter::DefineSequencePoints(Method* method)
{
    HRESULT hr = S_OK;
    BinStr* blob = new BinStr();

    // Blob ::= header SequencePointRecord (SequencePointRecord | document-record)*
    // SequencePointRecord :: = sequence-point-record | hidden-sequence-point-record

    ULONG localSigRid = 0;
    ULONG offset = 0;
    ULONG deltaLines = 0;
    LONG deltaColumns = 0;
    LONG deltaStartLine = 0;
    LONG deltaStartColumn = 0;
    LinePC* currSeqPoint = NULL;
    LinePC* prevSeqPoint = NULL;
    LinePC* prevNonHiddenSeqPoint = NULL;
    LinePC* nextSeqPoint = NULL;
    BOOL isValid = TRUE;
    BOOL hasEmptyMethodBody = method->m_LinePCList.COUNT() == 0;

    // header ::= {LocalSignature, InitialDocument}
    if (!hasEmptyMethodBody)
    {
        // LocalSignature
        localSigRid = RidFromToken(method->m_LocalsSig);
        CompressUnsignedLong(localSigRid, blob);
        // InitialDocument TODO: skip this for now
    }

    // SequencePointRecord :: = sequence-point-record | hidden-sequence-point-record
    for (UINT32 i = 0; i < method->m_LinePCList.COUNT(); i++)
    {
        currSeqPoint = method->m_LinePCList.PEEK(i);
        if (i < (method->m_LinePCList.COUNT() - 1))
            nextSeqPoint = method->m_LinePCList.PEEK(i + 1);
        else
            nextSeqPoint = NULL;

        isValid = VerifySequencePoint(currSeqPoint, nextSeqPoint);
        if (!isValid)
        {
            method->m_pAssembler->report->warn("Sequence point at line: [0x%x] and offset: [0x%x] in method '%s' is not valid!\n",
                currSeqPoint->Line,
                currSeqPoint->PC,
                method->m_szName);
            hr = E_FAIL;
            break; // TODO: break or ignore?
        }

        if (!currSeqPoint->IsHidden)
        {
            //offset
            offset = (i == 0) ? currSeqPoint->PC : currSeqPoint->PC - prevSeqPoint->PC;
            CompressUnsignedLong(offset, blob);

            //delta lines
            deltaLines = currSeqPoint->LineEnd - currSeqPoint->Line;
            CompressUnsignedLong(deltaLines, blob);

            //delta columns
            deltaColumns = currSeqPoint->ColumnEnd - currSeqPoint->Column;
            if (deltaLines == 0)
                CompressUnsignedLong(deltaColumns, blob);
            else
                CompressSignedLong(deltaColumns, blob);

            //delta start line
            if (prevNonHiddenSeqPoint == NULL)
            {
                deltaStartLine = currSeqPoint->Line;
                CompressUnsignedLong(deltaStartLine, blob);
            }
            else
            {
                deltaStartLine = currSeqPoint->Line - prevNonHiddenSeqPoint->Line;
                CompressSignedLong(deltaStartLine, blob);
            }

            //delta start column
            if (prevNonHiddenSeqPoint == NULL)
            {
                deltaStartColumn = currSeqPoint->Column;
                CompressUnsignedLong(deltaStartColumn, blob);
            }
            else
            {
                deltaStartColumn = currSeqPoint->Column - prevNonHiddenSeqPoint->Column;
                CompressSignedLong(deltaStartColumn, blob);
            }

            prevNonHiddenSeqPoint = currSeqPoint;
        }
        else
        {
            //offset
            offset = (i == 0) ? currSeqPoint->PC : currSeqPoint->PC - prevSeqPoint->PC;
            CompressUnsignedLong(offset, blob);

            //delta lines
            deltaLines = 0;
            CompressUnsignedLong(deltaLines, blob);

            //delta lines
            deltaColumns = 0;
            CompressUnsignedLong(deltaColumns, blob);
        }
        prevSeqPoint = currSeqPoint;
    }

    // finally define sequence points for the method
    if ((isValid && currSeqPoint != NULL) || hasEmptyMethodBody)
    {
        mdDocument document = hasEmptyMethodBody ? m_currentDocument->GetToken() : currSeqPoint->pOwnerDocument->GetToken();
        ULONG documentRid = RidFromToken(document);
        hr = m_pdbEmitter->DefineSequencePoints(documentRid, blob->ptr(), blob->length());
    }

    delete blob;
    return hr;
}

HRESULT PortablePdbWriter::DefineLocalScope(Method* method)
{
    if (!_DefineLocalScope(method->m_Tok, &method->m_MainScope))
        return E_FAIL;
    else
        return S_OK;
}

BOOL PortablePdbWriter::_DefineLocalScope(mdMethodDef methodDefToken, Scope* currScope)
{
    BOOL fSuccess = FALSE;
    ARG_NAME_LIST* pLocalVar = currScope->pLocals;
    ULONG methodRid = RidFromToken(methodDefToken);
    ULONG importScopeRid = RidFromToken(mdImportScopeNil);          // TODO: not supported for now
    ULONG firstLocalVarRid = 0;
    ULONG firstLocalConstRid = RidFromToken(mdLocalConstantNil);    // TODO: not supported for now
    ULONG start = 0;
    ULONG length = 0;
    mdLocalVariable firstLocVarToken = mdLocalScopeNil;

    while (pLocalVar != NULL)
    {
        if (pLocalVar->szName != NULL)
        {
            mdLocalVariable locVarToken = mdLocalScopeNil;
            USHORT attribute = 0;                                       // TODO: not supported for now
            USHORT index = pLocalVar->dwAttr & 0xffff; // slot
            if (FAILED(m_pdbEmitter->DefineLocalVariable(attribute, index, (char*)pLocalVar->szName, &locVarToken))) goto exit;

            if (firstLocVarToken == mdLocalScopeNil)
                firstLocVarToken = locVarToken;
        }

        pLocalVar = pLocalVar->pNext;
    }

    if (firstLocVarToken != mdLocalScopeNil)
    {
        firstLocalVarRid = RidFromToken(firstLocVarToken);
        start = currScope->dwStart;
        length = currScope->dwEnd - currScope->dwStart;
        if (FAILED(m_pdbEmitter->DefineLocalScope(methodRid, importScopeRid, firstLocalVarRid, firstLocalConstRid, start, length))) goto exit;
    }

    fSuccess = TRUE;
    for (ULONG i = 0; i < currScope->SubScope.COUNT(); i++)
        fSuccess &= _DefineLocalScope(methodDefToken, currScope->SubScope.PEEK(i));

exit:
    return fSuccess;
}

BOOL PortablePdbWriter::VerifySequencePoint(LinePC* curr, LinePC* next)
{
    if (!curr->IsHidden)
    {
        if ((curr->PC >= 0 && curr->PC < 0x20000000) &&
            (next == NULL || (next != NULL && curr->PC < next->PC)) &&
            (curr->Line >= 0 && curr->Line < 0x20000000 && curr->Line != 0xfeefee) &&
            (curr->LineEnd >= 0 && curr->LineEnd < 0x20000000 && curr->LineEnd != 0xfeefee) &&
            (curr->Column >= 0 && curr->Column < 0x10000) &&
            (curr->ColumnEnd >= 0 && curr->ColumnEnd < 0x10000) &&
            (curr->LineEnd > curr->Line || (curr->Line == curr->LineEnd && curr->ColumnEnd > curr->Column)))
        {
            return TRUE;
        }
        else
        {
            return FALSE;
        }
    }
    return TRUE;
}

void PortablePdbWriter::CompressUnsignedLong(ULONG srcData, BinStr* dstBuffer)
{
    ULONG cnt = CorSigCompressData(srcData, dstBuffer->getBuff(sizeof(ULONG) + 1));
    dstBuffer->remove((sizeof(ULONG) + 1) - cnt);
}

void PortablePdbWriter::CompressSignedLong(LONG srcData, BinStr* dstBuffer)
{
    ULONG cnt = CorSigCompressSignedInt(srcData, dstBuffer->getBuff(sizeof(LONG) + 1));
    dstBuffer->remove((sizeof(LONG) + 1) - cnt);
}
