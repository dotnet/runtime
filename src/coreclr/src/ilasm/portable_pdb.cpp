// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "portable_pdb.h"
#include <time.h>

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
// PortablePdbWritter
//*****************************************************************************
PortablePdbWritter::PortablePdbWritter()
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

PortablePdbWritter::~PortablePdbWritter()
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

HRESULT PortablePdbWritter::Init(IMetaDataEmit2* pdbEmitter)
{
    m_currentDocument = NULL;
    m_documentList.RESET(true);
    m_pdbEmitter = pdbEmitter;
    memset(m_pdbStream.typeSystemTableRows, 0, sizeof(ULONG) * TBL_COUNT);
    time_t now;
    time(&now);
    m_pdbStream.id.pdbTimeStamp = (ULONG)now;
    return CoCreateGuid(&m_pdbStream.id.pdbGuid);
}

IMetaDataEmit2* PortablePdbWritter::GetEmitter()
{
    return m_pdbEmitter;
}

GUID* PortablePdbWritter::GetGuid()
{
    return &m_pdbStream.id.pdbGuid;
}

ULONG PortablePdbWritter::GetTimestamp()
{
    return m_pdbStream.id.pdbTimeStamp;
}

HRESULT PortablePdbWritter::BuildPdbStream(IMetaDataEmit2* peEmitter, mdMethodDef entryPoint)
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

HRESULT PortablePdbWritter::DefineDocument(char* name, GUID* language)
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
