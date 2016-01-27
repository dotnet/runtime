// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCManagerImpl.inl
//
// Defines Classes to implement InterProcess Communication Manager for a COM+
//
//*****************************************************************************

#ifndef _IPCManagerImpl_INL_
#define _IPCManagerImpl_INL_

#include "ipcmanagerimpl.h"

//-----------------------------------------------------------------------------
// Return true if the IPCBlockTable is available.
//-----------------------------------------------------------------------------
inline BOOL IPCWriterImpl::IsBlockTableOpen() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return (m_pBlockTable != NULL);
}

//-----------------------------------------------------------------------------
// Return true if our LegacyPrivate block is available.
//-----------------------------------------------------------------------------
inline BOOL IPCWriterImpl::IsLegacyPrivateBlockOpen() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return (m_ptrLegacyPrivateBlock != NULL);
}

//-----------------------------------------------------------------------------
// Returns a BOOL indicating whether the Wow64 structs should be used instead
// of the normal structs.
//
// It is NOT safe to call this function before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline BOOL IPCReaderImpl::UseWow64StructsLegacy()
{
#if !defined(_X86_)
    _ASSERTE(m_fInitialized);
    return m_fIsTarget32Bit;
#else
    return FALSE;
#endif
}

//-----------------------------------------------------------------------------
// Returns the value of the flags field of the specified IPC block. This is
// safe to call before m_fIsTarget32Bit has been initialized since the flags 
// field is in the same position for 32-bit and 64-bit targets.
//
// It is safe to call this function before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline USHORT IPCReaderImpl::GetFlagsLegacy(void * pBlock)
{
    return ((LegacyPrivateIPCHeader*)pBlock)->m_Flags;
}

//-----------------------------------------------------------------------------
// Returns the value of the block size field of the specified IPC block. 
//
// It is safe to call this function before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline DWORD IPCReaderImpl::GetBlockSizeLegacy(void * pBlock)
{
    return ((LegacyPrivateIPCHeader*)pBlock)->m_blockSize;
}

//-----------------------------------------------------------------------------
// Returns true if the specified entry is empty and false if the entry is
// usable. This is an internal helper that enforces the formal definition 
// for an "empty" entry
//
// Arguments:
//   Id - index of the entry to check
//
// It is NOT safe to call this function before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline BOOL IPCReaderImpl::Internal_CheckEntryEmptyLegacyPublic(DWORD Id)
{
    // Directory has offset in bytes of block
    return (GetDirectoryLegacy(m_ptrLegacyPublicBlock)[Id].m_Offset == EMPTY_ENTRY_OFFSET);
}

//-----------------------------------------------------------------------------
// Returns a BYTE* to a block within a header. This is an internal 
// helper that encapsulates error-prone math.
//
// Arguments:
//   Id - index of the entry containing the desired LegacyPublic block
//
// It is NOT safe to call this function before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline BYTE * IPCReaderImpl::Internal_GetBlockLegacyPublic(DWORD Id)
{
    // This block has been removed. Callee should have caught that and not called us.
    _ASSERTE(!Internal_CheckEntryEmptyLegacyPublic(Id));

    return ((BYTE*) m_ptrLegacyPublicBlock) + (SIZE_T)GetOffsetBaseLegacy() + 
        (SIZE_T)GetNumEntriesLegacy(m_ptrLegacyPublicBlock) * sizeof(IPCEntry) + 
        (SIZE_T)GetDirectoryLegacy(m_ptrLegacyPublicBlock)[Id].m_Offset;
}

//-----------------------------------------------------------------------------
// Returns a value that is used to calculate the actual offset of an entry in
// an IPC block. Internal_GetBlockLegacyPublic() shows how to use GetOffsetBaseLegacy()
// to calculate the actual offset of an entry.
//
// It is NOT safe to call this funciton before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline DWORD IPCReaderImpl::GetOffsetBaseLegacy()
{
    if (UseWow64StructsLegacy())
        return LEGACY_IPC_ENTRY_OFFSET_BASE_WOW64;

    return LEGACY_IPC_ENTRY_OFFSET_BASE;
}

//-----------------------------------------------------------------------------
// Returns the expected value for the specified offset of the first entry in
// an IPC block.
//
// It is NOT safe to call this funciton before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline DWORD IPCReaderImpl::GetFirstExpectedOffsetLegacy()
{
    if (GetOffsetBaseLegacy() == 0)
        return sizeof(LegacyPrivateIPCHeader);
    
    return 0;
}

//-----------------------------------------------------------------------------
// Returns the number of entries in the specified IPC block.
//
// It is NOT safe to call this funciton before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline DWORD IPCReaderImpl::GetNumEntriesLegacy(void * pBlock)
{
    if (UseWow64StructsLegacy())    
        return ((LegacyPrivateIPCHeaderTemplate<DWORD>*)pBlock)->m_numEntries;
    
    return ((LegacyPrivateIPCHeader*)pBlock)->m_numEntries;
}

//-----------------------------------------------------------------------------
// Returns a pointer to the directory ('m_table') in the specified IPC block.
//
// It is NOT safe to call this funciton before m_fIsTarget32Bit has been 
// initialized.
//-----------------------------------------------------------------------------
inline IPCEntry * IPCReaderImpl::GetDirectoryLegacy(void * pBlock)
{
    if (UseWow64StructsLegacy())
        return ((FullIPCHeaderLegacyPublicTemplate<DWORD>*)pBlock)->m_table;

    return ((FullIPCHeaderLegacyPublicTemplate<HINSTANCE>*)pBlock)->m_table;
}

//-----------------------------------------------------------------------------
// Compile-time asserts that check the values of LEGACY_IPC_ENTRY_OFFSET_BASE and
// LEGACY_IPC_ENTRY_OFFSET_BASE_WOW64
//-----------------------------------------------------------------------------

#ifdef _TARGET_X86_
    C_ASSERT(sizeof(LegacyPrivateIPCHeaderTemplate<HINSTANCE>) == LEGACY_IPC_ENTRY_OFFSET_BASE);
#endif

C_ASSERT(sizeof(LegacyPrivateIPCHeaderTemplate<DWORD>) == LEGACY_IPC_ENTRY_OFFSET_BASE_WOW64);


#endif // _IPCManagerImpl_INL_

