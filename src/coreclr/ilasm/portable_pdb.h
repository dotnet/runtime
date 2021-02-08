// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PORTABLE_PDB_H
#define PORTABLE_PDB_H

#include "ilasmpch.h"
#include "asmtemplates.h"
#include "portablepdbmdds.h"
#include "portablepdbmdi.h"

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
// PortablePdbWriter
//*****************************************************************************
class PortablePdbWriter
{
public:
    PortablePdbWriter();
    ~PortablePdbWriter();
    HRESULT         Init(IMetaDataDispenserEx2* mdDispenser);
    IMetaDataEmit3* GetEmitter();
    GUID*           GetGuid();
    ULONG           GetTimestamp();
    Document*       GetCurrentDocument();
    HRESULT         BuildPdbStream(IMetaDataEmit3* peEmitter, mdMethodDef entryPoint);
    HRESULT         DefineDocument(char* name, GUID* language);
    HRESULT         DefineSequencePoints(Method* method);
    HRESULT         DefineLocalScope(Method* method);

private:
    BOOL            VerifySequencePoint(LinePC* curr, LinePC* next);
    void            CompressUnsignedLong(ULONG srcData, BinStr* dstBuffer);
    void            CompressSignedLong(LONG srcData, BinStr* dstBuffer);
    BOOL            _DefineLocalScope(mdMethodDef methodDefToken, Scope* currScope);

private:
    IMetaDataEmit3* m_pdbEmitter;
    PORT_PDB_STREAM m_pdbStream;
    DocumentList    m_documentList;
    Document*       m_currentDocument;
};

#endif
