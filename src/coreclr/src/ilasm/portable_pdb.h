// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef PORTABLE_PDB_H
#define PORTABLE_PDB_H

#include "ilasmpch.h"
#include "asmtemplates.h"

//*****************************************************************************
// Document
//*****************************************************************************
class Document
{
public:
    Document();
    ~Document();

    char* GetName();
    void                SetName(char* name);
    mdDocument          GetToken();
    void                SetToken(mdDocument token);

private:
    char* m_name;
    mdDocument          m_token;
};

typedef FIFO<Document> DocumentList;

//*****************************************************************************
// PortablePdbWritter
//*****************************************************************************
class PortablePdbWritter
{
public:
    PortablePdbWritter();
    ~PortablePdbWritter();
    HRESULT             Init(IMetaDataEmit2* pdbEmitter);
    IMetaDataEmit2*     GetEmitter();
    GUID*               GetGuid();
    ULONG               GetTimestamp();
    HRESULT             BuildPdbStream(IMetaDataEmit2* peEmitter, mdMethodDef entryPoint);
    HRESULT             DefineDocument(char* name, GUID* language);

private:
    IMetaDataEmit2*     m_pdbEmitter;
    PORT_PDB_STREAM     m_pdbStream;
    DocumentList        m_documentList;
    Document*           m_currentDocument;
};

#endif
