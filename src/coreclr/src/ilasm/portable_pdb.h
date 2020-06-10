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
    char*           GetName();
    void            SetName(char* name);
    mdDocument      GetToken();
    void            SetToken(mdDocument token);

private:
    char*           m_name;
    mdDocument      m_token;
};

typedef FIFO<Document> DocumentList;

class BinStr;
class Method;
class Scope;
struct LinePC;

//*****************************************************************************
// PortablePdbWritter
//*****************************************************************************
class PortablePdbWritter
{
public:
    PortablePdbWritter();
    ~PortablePdbWritter();
    HRESULT         Init(IMetaDataEmit2* pdbEmitter);
    IMetaDataEmit2* GetEmitter();
    GUID*           GetGuid();
    ULONG           GetTimestamp();
    Document*       GetCurrentDocument();
    HRESULT         BuildPdbStream(IMetaDataEmit2* peEmitter, mdMethodDef entryPoint);
    HRESULT         DefineDocument(char* name, GUID* language);
    HRESULT         DefineSequencePoints(Method* method);
    HRESULT         DefineLocalScope(Method* method);

private:
    BOOL            VerifySequencePoint(LinePC* curr, LinePC* next);
    void            CompressUnsignedLong(ULONG srcData, BinStr* dstBuffer);
    void            CompressSignedLong(LONG srcData, BinStr* dstBuffer);
    BOOL            _DefineLocalScope(mdMethodDef methodDefToken, Scope* currScope, mdLocalVariable* firstLocVarToken);

private:
    IMetaDataEmit2* m_pdbEmitter;
    PORT_PDB_STREAM m_pdbStream;
    DocumentList    m_documentList;
    Document*       m_currentDocument;
};

#endif
