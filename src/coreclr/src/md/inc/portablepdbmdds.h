// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** portablepdbmdds.h - contains data structures and types used for         **
 **                     portable PDB metadata generation.                   **
 **                                                                         **
 *****************************************************************************/

#ifndef _PORTABLEPDBMDDS_H_
#define _PORTABLEPDBMDDS_H_

#if _MSC_VER >= 1100
#pragma once
#endif

#include "corhdr.h"

//-------------------------------------
//--- PDB stream data structure
//-------------------------------------
typedef struct _PDB_ID
{
    GUID            pdbGuid;
    ULONG           pdbTimeStamp;
} PDB_ID;

typedef struct _PORT_PDB_STREAM
{
    PDB_ID          id;
    mdMethodDef     entryPoint;
    ULONG64         referencedTypeSystemTables;
    ULONG           *typeSystemTableRows;
    ULONG           typeSystemTableRowsSize;
} PORT_PDB_STREAM;

//-------------------------------------
//--- Portable PDB table tokens
//-------------------------------------

// PPdb token definitions
typedef mdToken mdDocument;
typedef mdToken mdMethodDebugInformation;
typedef mdToken mdLocalScope;
typedef mdToken mdLocalVariable;
typedef mdToken mdLocalConstant;
typedef mdToken mdImportScope;
// TODO:
// typedef mdToken mdStateMachineMethod;
// typedef mdToken mdCustomDebugInformation;

// PPdb token tags
typedef enum PPdbCorTokenType
{
    mdtDocument                 = 0x30000000,
    mdtMethodDebugInformation   = 0x31000000,
    mdtLocalScope               = 0x32000000,
    mdtLocalVariable            = 0x33000000,
    mdtLocalConstant            = 0x34000000,
    mdtImportScope              = 0x35000000,
    // TODO:
    // mdtStateMachineMethod       = 0x36000000,
    // mdtCustomDebugInformation   = 0x37000000,
} PPdbCorTokenType;

// PPdb Nil tokens
#define mdDocumentNil               ((mdDocument)mdtDocument)
#define mdMethodDebugInformationNil ((mdMethodDebugInformation)mdtMethodDebugInformation)
#define mdLocalScopeNil             ((mdLocalScope)mdtLocalScope)
#define mdLocalVariableNil          ((mdLocalVariable)mdtLocalVariable)
#define mdLocalConstantNil          ((mdLocalConstant)mdtLocalConstant)
#define mdImportScopeNil            ((mdImportScope)mdtImportScope)
// TODO:
// #define mdStateMachineMethodNil     ((mdStateMachineMethod)mdtStateMachineMethod)
// #define mdCustomDebugInformationNil ((mdCustomDebugInformation)mdtCustomDebugInformation)

#endif // _PORTABLEPDBMDDS_H_
