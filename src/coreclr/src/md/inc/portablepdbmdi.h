// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** portablepdbmdi.h - contains COM interface definitions for portable PDB  **
 **                    metadata generation.                                 **
 **                                                                         **
 *****************************************************************************/

#ifndef _PORTABLEPDBMDI_H_
#define _PORTABLEPDBMDI_H_

#if _MSC_VER >= 1100
#pragma once
#endif

#include "cor.h"
#include "portablepdbmdds.h"

#ifdef __cplusplus
extern "C" {
#endif

//-------------------------------------
//--- IMetaDataEmit3
//-------------------------------------
// {1a5abcd7-854e-4f07-ace4-3f09e6092939}
EXTERN_GUID(IID_IMetaDataEmit3, 0x1a5abcd7, 0x854e, 0x4f07, 0xac, 0xe4, 0x3f, 0x09, 0xe6, 0x09, 0x29, 0x39);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataEmit3
DECLARE_INTERFACE_(IMetaDataEmit3, IMetaDataEmit2)
{
    
    STDMETHOD(GetReferencedTypeSysTables)(  // S_OK or error.
        ULONG64     *refTables,             // [OUT] Bit vector of referenced type system metadata tables.
        ULONG       refTableRows[],         // [OUT] Array of number of rows for each referenced type system table.
        const ULONG maxTableRowsSize,       // [IN]  Max size of the rows array.
        ULONG       *tableRowsSize) PURE;   // [OUT] Actual size of the rows array.

    STDMETHOD(DefinePdbStream)(             // S_OK or error.
        PORT_PDB_STREAM *pdbStream) PURE;   // [IN] Portable pdb stream data.

    STDMETHOD(DefineDocument)(              // S_OK or error.
        char        *docName,               // [IN] Document name (string will be tokenized).
        GUID        *hashAlg,               // [IN] Hash algorithm GUID.
        BYTE        *hashVal,               // [IN] Hash value.
        ULONG       hashValSize,            // [IN] Hash value size.
        GUID        *lang,                  // [IN] Language GUID.
        mdDocument  *docMdToken) PURE;      // [OUT] Token of the defined document.

    STDMETHOD(DefineSequencePoints)(        // S_OK or error.
        ULONG       docRid,                 // [IN] Document RID.
        BYTE        *sequencePtsBlob,       // [IN] Sequence point blob.
        ULONG       sequencePtsBlobSize) PURE; // [IN] Sequence point blob size.

    STDMETHOD(DefineLocalScope)(            // S_OK or error.
        ULONG       methodDefRid,           // [IN] Method RID.
        ULONG       importScopeRid,         // [IN] Import scope RID.
        ULONG       firstLocalVarRid,       // [IN] First local variable RID (of the continous run).
        ULONG       firstLocalConstRid,     // [IN] First local constant RID (of the continous run).
        ULONG       startOffset,            // [IN] Start offset of the scope.
        ULONG       length) PURE;           // [IN] Scope length.

    STDMETHOD(DefineLocalVariable)(         // S_OK or error.
        USHORT      attribute,              // [IN] Variable attribute.
        USHORT      index,                  // [IN] Variable index (slot).
        char        *name,                  // [IN] Variable name.
        mdLocalVariable *locVarToken) PURE; // [OUT] Token of the defined variable.
};

//-------------------------------------
//--- IMetaDataDispenserEx2
//-------------------------------------

// {23aaef0d-49bf-43f0-9744-1c3e9c56322a}
EXTERN_GUID(IID_IMetaDataDispenserEx2, 0x23aaef0d, 0x49bf, 0x43f0, 0x97, 0x44, 0x1c, 0x3e, 0x9c, 0x56, 0x32, 0x2a);

#undef  INTERFACE
#define INTERFACE IMetaDataDispenserEx2
DECLARE_INTERFACE_(IMetaDataDispenserEx2, IMetaDataDispenserEx)
{
    STDMETHOD(DefinePortablePdbScope)(      // Return code.
        REFCLSID    rclsid,                 // [IN] What version to create.
        DWORD       dwCreateFlags,          // [IN] Flags on the create.
        REFIID      riid,                   // [IN] The interface desired.
        IUnknown * *ppIUnk) PURE;           // [OUT] Return interface on success.
};

#ifdef __cplusplus
}
#endif

#endif // _PORTABLEPDBMDI_H_
