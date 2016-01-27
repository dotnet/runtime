// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: pdbdata.h
//

// ===========================================================================

#ifndef PDBDATA_H_
#define PDBDATA_H_

#include "umisc.h"
#include "palclr.h"

struct SymMethodInfo;
struct SymLexicalScope;
struct SymVariable;
struct SymUsingNamespace;
struct SymConstant;
struct SequencePoint;
struct DocumentInfo;
struct MethodInfo;

extern "C" const GUID ILDB_VERSION_GUID_FSR;
extern "C" const GUID ILDB_VERSION_GUID;

#define ILDB_MINOR_VERSION_NUMBER 0
#define ILDB_SIGNATURE "_ildb_signature"
#define ILDB_SIGNATURE_SIZE (16)

typedef struct PDBInfo {

    // Entry point of the PE
    mdMethodDef         m_userEntryPoint;

    UINT32 m_CountOfMethods;
    UINT32 m_CountOfScopes;
    UINT32 m_CountOfVars;
    UINT32 m_CountOfUsing;
    UINT32 m_CountOfConstants;
    UINT32 m_CountOfDocuments;
    UINT32 m_CountOfSequencePoints;
    UINT32 m_CountOfBytes;
    UINT32 m_CountOfStringBytes;

public:
    PDBInfo()
    {
        memset(this, 0, sizeof(PDBInfo));
        // Make sure m_userEntryPoint initialized correctly
        _ASSERTE(mdTokenNil == 0);
    }

#if BIGENDIAN
    void ConvertEndianness() {
        m_userEntryPoint = VAL32(m_userEntryPoint);
        m_CountOfMethods = VAL32(m_CountOfMethods);
        m_CountOfScopes = VAL32(m_CountOfScopes);
        m_CountOfVars = VAL32(m_CountOfVars);
        m_CountOfUsing = VAL32(m_CountOfUsing);
        m_CountOfConstants = VAL32(m_CountOfConstants);
        m_CountOfSequencePoints = VAL32(m_CountOfSequencePoints);
        m_CountOfDocuments = VAL32(m_CountOfDocuments);
        m_CountOfBytes = VAL32(m_CountOfBytes);
        m_CountOfStringBytes = VAL32(m_CountOfStringBytes);
    }
#else
    void ConvertEndianness() {}
#endif

} PDBInfo;

// The signature, Guid version + PDBInfo data
#define ILDB_HEADER_SIZE (ILDB_SIGNATURE_SIZE + sizeof(GUID) + sizeof(PDBInfo) )

typedef struct PDBDataPointers
{
    SymMethodInfo * m_pMethods;   // Method information
    SymLexicalScope *m_pScopes;   // Scopes
    SymVariable *m_pVars;         // Local Variables
    SymUsingNamespace *m_pUsings; // list of using/imports
    SymConstant *m_pConstants;    // Constants
    DocumentInfo *m_pDocuments;   // Documents
    SequencePoint *m_pSequencePoints;  // Sequence Points
    // Array of various bytes (variable signature, etc)
    BYTE *m_pBytes;
    // Strings
    BYTE *m_pStringsBytes;
} PDBDataPointers;

#endif /* PDBDATA_H_ */
