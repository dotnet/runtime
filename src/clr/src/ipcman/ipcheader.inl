// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCHeader.inl
//
// Define the LegacyPrivate header format for COM+ memory mapped files. Everyone
// outside of IPCMan.lib will use the public header, IPCManagerInterface.h
//
//*****************************************************************************

#ifndef _IPCManagerPriv_inl_
#define _IPCManagerPriv_inl_

#include "ipcheader.h"

//=============================================================================
// Internal Helpers: Encapsulate any error-prone math / comparisons.
// The helpers are very streamlined and don't handle error conditions.
// Also, Table access functions use DWORD instead of typesafe Enums
// so they can be more flexible (not just for LegacyPrivate blocks).
//=============================================================================

//-----------------------------------------------------------------------------
// Returns true if the entry is empty and false if the entry is usable.
// This is an internal helper that Enforces a formal definition for an 
// "empty" entry
//
// Arguments:
//   block - IPC block of interest
//   Id - index of the desired entry in the IPC block's table
//-----------------------------------------------------------------------------
inline bool Internal_CheckEntryEmptyLegacyPrivate(
    const LegacyPrivateIPCControlBlock & block,   
    DWORD Id                                
)
{
// Directory has offset in bytes of block
    const DWORD offset = block.m_FullIPCHeader.m_table[Id].m_Offset;

    return (EMPTY_ENTRY_OFFSET == offset);
}

//-----------------------------------------------------------------------------
// Returns the base that entry offsets for the specified IPC block
// are relative to.
//
// Arguments:
//   block - IPC block of interest
//-----------------------------------------------------------------------------
inline SIZE_T Internal_GetOffsetBaseLegacyPrivate(const LegacyPrivateIPCControlBlock & block)
{
    return LEGACY_IPC_ENTRY_OFFSET_BASE + 
           block.m_FullIPCHeader.m_header.m_numEntries 
            * sizeof(IPCEntry);            // skip over directory (variable size)
}

//-----------------------------------------------------------------------------
// Returns a BYTE* to a block within a header. This is an internal 
// helper that encapsulates error-prone math.
//
// Arguments:
//   block - IPC block of interest
//   Id - index of the desired entry in the IPC block's table
//-----------------------------------------------------------------------------
inline BYTE* Internal_GetBlockLegacyPrivate(
    const LegacyPrivateIPCControlBlock & block,   
    DWORD Id                                
)
{

// Directory has offset in bytes of block
    const DWORD offset = block.m_FullIPCHeader.m_table[Id].m_Offset;


// This block has been removed. Callee should have caught that and not called us.
    _ASSERTE(!Internal_CheckEntryEmptyLegacyPrivate(block, Id));
    return
        ((BYTE*) &block)                    // base pointer to start of block
        + Internal_GetOffsetBaseLegacyPrivate(block)
        +offset;                            // jump to block
}

#endif // _IPCManagerPriv_inl_
