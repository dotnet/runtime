// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCHeader.h
//
// Define the LegacyPrivate header format for COM+ memory mapped files. Everyone
// outside of IPCMan.lib will use the public header, IPCManagerInterface.h
//
//*****************************************************************************

#ifndef _IPCManagerPriv_h_
#define _IPCManagerPriv_h_


//-----------------------------------------------------------------------------
// We must pull in the headers of all client blocks
// <TODO>@todo - resolve these directory links</TODO>
//-----------------------------------------------------------------------------
#include "../debug/inc/dbgappdomain.h"
#include "perfcounterdefs.h"

#include "ndpversion.h"
#include "ipcenums.h"

//-----------------------------------------------------------------------------
// Entry in the IPC Directory. Ensure binary compatibility across versions
// if we add (or remove) entries. If we remove an block, the entry should 
// be EMPTY_ENTRY_OFFSET
//-----------------------------------------------------------------------------

const DWORD EMPTY_ENTRY_OFFSET  = 0xFFFFFFFF;
const DWORD EMPTY_ENTRY_SIZE    = 0;

struct IPCEntry
{
    DWORD m_Offset; // offset of the IPC Block from the end of the Full IPC Header
    DWORD m_Size;       // size (in bytes) of the block
};

// Newer versions of the CLR use m_Flags field
const USHORT IPC_FLAG_USES_FLAGS  = 0x1;
const USHORT IPC_FLAG_INITIALIZED = 0x2; 
const USHORT IPC_FLAG_X86         = 0x4;  

/******************************************************************************
 * The CLR opens memory mapped files to expose perfcounter values and other
 * information to other processes. Historically there have been three memory 
 * mapped files: the BlockTable, the LegacyPrivateBlock, and the 
 * LegacyPublicBlock.
 *
 * BlockTable - The block table was designed to work with multiple runtimes
 *     running side by side in the same process (SxS in-proc). We have defined
 *     semantics using interlocked operations that allow a runtime to allocate
 *     a block from the block table in a thread-safe manner.
 *
 * LegacyPrivateBlock - The legacy private block was used by older versions
 *     of the runtime to expose various information to the debugger. The 
 *     legacy private block is not compatible with in-proc SxS, and thus it
 *     must be removed in the near future. Currently it is being used to expose
 *     information about AppDomains to the debugger. We will need to keep the
 *     code that knows how to read the legacy private block as long as we 
 *     continue to support .NET 3.5 SP1.
 *
 * LegacyPublicBlock - The legacy public block was used by older versions
 *     of the runtime to expose perfcounter values. The legacy public block is
 *     not compatible with in-proc SxS, and thus it has been removed. We will
 *     need to keep the code that knows how to read the legacy public block as
 *     long as we continue to support .NET 3.5 SP1.
 ******************************************************************************/

/**************************************** BLOCK TABLE ****************************************/

const DWORD IPC_BLOCK_TABLE_SIZE = 65536;
const DWORD IPC_BLOCK_SIZE = 2048;
const DWORD IPC_NUM_BLOCKS_IN_TABLE = 32;

static_assert_no_msg(IPC_BLOCK_TABLE_SIZE == IPC_NUM_BLOCKS_IN_TABLE * IPC_BLOCK_SIZE);

#if defined(TIA64)
#define IA64MemoryBarrier()        MemoryBarrier()
#else
#define IA64MemoryBarrier()
#endif

struct IPCHeader
{
    // Chunk header
    Volatile<LONG> m_Counter; // value of 0 is special; means that this block has never been touched before by a writer
    DWORD m_RuntimeId;        // value of 0 is special; means that chunk is currently free (runtime ids are always greater than 0)
    DWORD m_Reserved1;
    DWORD m_Reserved2;

    // Standard header (m_hInstance was removed)
    USHORT      m_Version;      // version of the IPC Block
    USHORT      m_Flags;        // flags field
    DWORD       m_blockSize;    // Size of the entire shared memory block
    USHORT      m_BuildYear;    // stamp for year built
    USHORT      m_BuildNumber;  // stamp for Month/Day built
    DWORD       m_numEntries;   // Number of entries in the table

    // Directory
    IPCEntry m_table[eIPC_MAX]; // entry describing each client's block
};

extern BOOL __SwitchToThread(DWORD dwSleepMSec, DWORD dwSwitchCount);

class IPCHeaderLockHolder
{
    LONG m_Counter;
    BOOL m_HaveLock;
    IPCHeader & m_Header;

  public:

    IPCHeaderLockHolder(IPCHeader & header) : m_HaveLock(FALSE), m_Header(header) {}

    BOOL TryGetLock()
    {
        _ASSERTE(!m_HaveLock);
        LONG oldCounter = m_Header.m_Counter;
        if ((oldCounter & 1) != 0)
            return FALSE;
        m_Counter = oldCounter + 1;
        if (InterlockedCompareExchange((LONG *)(&(m_Header.m_Counter)), m_Counter, oldCounter) != oldCounter)
            return FALSE;
        m_HaveLock = TRUE;

        return TRUE;
    }

    BOOL TryGetLock(DWORD numRetries)
    {
        DWORD dwSwitchCount = 0;

        for (;;)
        {
            if (TryGetLock())
                return TRUE;

            if (numRetries == 0)
                return FALSE;

            --numRetries;
            __SwitchToThread(0, ++dwSwitchCount);
       }
    }

    void FreeLock()
    {
        _ASSERTE(m_HaveLock);
        _ASSERTE(m_Header.m_Counter == m_Counter);
        ++m_Counter;
        m_Counter = (m_Counter == 0) ? 2 : m_Counter;
        m_Header.m_Counter = m_Counter;
        m_HaveLock = FALSE;
    }

    ~IPCHeaderLockHolder() 
    { 
        if (m_HaveLock)
            FreeLock();
    }
};

class IPCHeaderReadHelper
{
    IPCHeader m_CachedHeader;
    IPCHeader * m_pUnreliableHeader;
    BOOL m_IsOpen;
    
  public:

    IPCHeaderReadHelper() : m_pUnreliableHeader(NULL), m_IsOpen(FALSE) {}

    BOOL TryOpenHeader(IPCHeader * header)
    {
        _ASSERTE(!m_IsOpen);
        
        m_pUnreliableHeader = header;

        // Read the counter and the runtime ID from the header
        m_CachedHeader.m_Counter = m_pUnreliableHeader->m_Counter;
        if ((m_CachedHeader.m_Counter & 1) != 0)
            return FALSE;
        m_CachedHeader.m_RuntimeId = m_pUnreliableHeader->m_RuntimeId;

        // If runtime ID is 0, then this block is not allocated by 
        // a runtime, and thus there is no further work to do
        if (m_CachedHeader.m_RuntimeId == 0)
        {
            m_IsOpen = TRUE;
            return TRUE;
        }

        // Read the rest of the values from the header
        m_CachedHeader.m_Reserved1   = m_pUnreliableHeader->m_Reserved1;
        m_CachedHeader.m_Reserved2   = m_pUnreliableHeader->m_Reserved2;
        m_CachedHeader.m_Version     = m_pUnreliableHeader->m_Version;
        m_CachedHeader.m_Flags       = m_pUnreliableHeader->m_Flags;
        m_CachedHeader.m_blockSize   = m_pUnreliableHeader->m_blockSize;
        m_CachedHeader.m_BuildYear   = m_pUnreliableHeader->m_BuildYear;
        m_CachedHeader.m_BuildNumber = m_pUnreliableHeader->m_BuildNumber;
        m_CachedHeader.m_numEntries  = m_pUnreliableHeader->m_numEntries;

        // Verify that the header did not change during the read
        LONG counter = m_pUnreliableHeader->m_Counter;
        if (m_CachedHeader.m_Counter != counter)
            return FALSE;

        // Since we know we got a clean read of numEntries, we
        // should be able to assert this with confidence
        if (m_CachedHeader.m_numEntries == 0)
        {
            _ASSERTE(!"numEntries from IPCBlock is zero");
            return FALSE;
        }
        else if (m_CachedHeader.m_numEntries > eIPC_MAX)
        {
            _ASSERTE(!"numEntries from IPCBlock is too big");
            return FALSE;
        }

        if (m_CachedHeader.m_blockSize == 0)
        {
            _ASSERTE(!"blockSize from IPCBlock is zero");
            return FALSE;
        }
        else if (m_CachedHeader.m_blockSize > IPC_BLOCK_SIZE)
        {
            _ASSERTE(!"blockSize from IPCBlock is too big");
            return FALSE;
        }

        // Copy the table
        for (DWORD i = 0; i < m_CachedHeader.m_numEntries; ++i)
        {
            m_CachedHeader.m_table[i].m_Offset = m_pUnreliableHeader->m_table[i].m_Offset;
            m_CachedHeader.m_table[i].m_Size = m_pUnreliableHeader->m_table[i].m_Size;
            if (i == eIPC_PerfCounters)
            {
                if(!((SIZE_T)m_CachedHeader.m_table[i].m_Offset < IPC_BLOCK_SIZE) && ((SIZE_T)m_CachedHeader.m_table[i].m_Offset + m_CachedHeader.m_table[i].m_Size <= IPC_BLOCK_SIZE))
                {
                    _ASSERTE(!"PerfCounter section offset + size is too large");
                    return FALSE;
                }
            }
        }

        // If eIPC_MAX > numEntries, then mark the left over
        // slots in m_table as "empty".
        for (DWORD i = m_CachedHeader.m_numEntries; i < eIPC_MAX; ++i)
        {
            m_CachedHeader.m_table[i].m_Offset = EMPTY_ENTRY_OFFSET;
            m_CachedHeader.m_table[i].m_Size = EMPTY_ENTRY_SIZE;
        }

        // Verify again that the header did not change during the read
        counter = m_pUnreliableHeader->m_Counter;
        if (m_CachedHeader.m_Counter != counter)
            return FALSE;

        m_IsOpen = TRUE;
        return TRUE;
    }
    
    void CloseHeader()
    {
        m_IsOpen = FALSE;
        m_pUnreliableHeader = NULL;
    }

    BOOL HeaderHasChanged()
    {
        _ASSERTE(m_IsOpen);
        LONG counter = m_pUnreliableHeader->m_Counter;
        return (m_CachedHeader.m_Counter != counter) ? TRUE : FALSE;
    }

    BOOL IsSentinal()
    {
        _ASSERTE(m_IsOpen);
        return (m_CachedHeader.m_Counter == 0);
    }

    DWORD GetRuntimeId()
    {
        _ASSERTE(m_IsOpen);
        return m_CachedHeader.m_RuntimeId;
    }

    USHORT GetIPCVersion()
    {
        _ASSERTE(m_IsOpen);
        return m_CachedHeader.m_Version;
    }

    BOOL UseWow64Structs()
    {
        _ASSERTE(m_IsOpen);
#if !defined(_TARGET_X86_)
        return ((m_CachedHeader.m_Flags & IPC_FLAG_X86) != 0) ? TRUE : FALSE;
#else
        return FALSE;
#endif
    }

    IPCHeader * GetCachedCopyOfHeader()
    {
        _ASSERTE(m_IsOpen);
        return &m_CachedHeader;
    }

    IPCHeader * GetUnreliableHeader()
    {
        _ASSERTE(m_IsOpen);
        return m_pUnreliableHeader;
    }

  private:

    void * GetUnreliableSection(EIPCClient eClient)
    {
        if (!m_IsOpen)
        {
            _ASSERTE(!"IPCHeaderReadHelper is not open");
            return NULL;
        }

        if (eClient < 0 || eClient >= eIPC_MAX)
        {
            _ASSERTE(!"eClient is out of bounds");
            return NULL;
        }

        if (m_CachedHeader.m_table[eClient].m_Offset == EMPTY_ENTRY_OFFSET)
        {
            _ASSERTE(!"Section is empty");
            return NULL;
        }

        return (BYTE*)m_pUnreliableHeader + (SIZE_T)m_CachedHeader.m_table[eClient].m_Offset;
    }

  public:

    // We opted to return void* instead of PerfCounterIPCControlBlock* because this
    // forces the caller to do an explicit cast. If UseWow64Structs() returns FALSE,
    // then the caller should cast to PerfCounterIPCControlBlock*. If UseWow64Structs()
    // return TRUE, then the caller should cast to PerfCounterWow64IPCControlBlock*

    void * GetUnreliablePerfBlock()
    {
        return GetUnreliableSection(eIPC_PerfCounters);
    }
};

const DWORD SXSPUBLIC_IPC_SIZE_NO_PADDING = sizeof(IPCHeader) + sizeof(struct PerfCounterIPCControlBlock);
const DWORD SXSPUBLIC_WOW64_IPC_SIZE_NO_PADDING = sizeof(IPCHeader) + sizeof(struct PerfCounterWow64IPCControlBlock);

const DWORD SXSPUBLIC_IPC_PAD_SIZE = IPC_BLOCK_SIZE - SXSPUBLIC_IPC_SIZE_NO_PADDING;
const DWORD SXSPUBLIC_WOW64_IPC_PAD_SIZE = IPC_BLOCK_SIZE - SXSPUBLIC_WOW64_IPC_SIZE_NO_PADDING;

struct IPCControlBlock
{
// Header
    IPCHeader m_Header;

// Client blocks
    struct PerfCounterIPCControlBlock   m_perf;

// Padding
    BYTE m_Padding[SXSPUBLIC_IPC_PAD_SIZE];
};

#pragma pack(push, 4)
struct IPCControlBlockWow64
{
// Header
    IPCHeader m_Header;

// Client blocks
    PerfCounterWow64IPCControlBlock   m_perf;

// Padding
    BYTE m_Padding[SXSPUBLIC_WOW64_IPC_PAD_SIZE];
};
#pragma pack(pop)

static_assert_no_msg(sizeof(IPCControlBlock) == IPC_BLOCK_SIZE);
static_assert_no_msg(sizeof(IPCControlBlockWow64) == IPC_BLOCK_SIZE);

struct IPCControlBlockTable
{
    IPCControlBlock m_blocks[IPC_NUM_BLOCKS_IN_TABLE];

    IPCControlBlock * GetBlock(DWORD index) 
    { return &(m_blocks[index]); }    
};

static_assert_no_msg(sizeof(IPCControlBlockTable) == IPC_BLOCK_TABLE_SIZE);

typedef DPTR(IPCControlBlockTable) PTR_IPCControlBlockTable;
typedef DPTR(IPCControlBlock) PTR_IPCControlBlock;


/**************************************** LEGACY ****************************************/

//-----------------------------------------------------------------------------
// LegacyPrivate header - put in its own structure so we can easily get the
// size of the header. It will compile to the same thing either way.
// This header must remain completely binary compatible w/ older versions.
// Notes:
// This header contains a "module handle" field which is platform dependent in size.
// That means for a 64 bit process to read IPC header of a 32 bit process, we cannot
// use the same class definition. The class would be different on the two platforms.
// Hence LegacyPrivateIPCHeaderTemplate is templatized on the type of the module handle. The 
// IPC writers always use HINSTANCE as the parameter. The IPC reader has to know
// whether it is reading IPC block of a 32 bit process. If so it uses DWORD as the
// parameter so that the resulting LegacyPrivateIPCHeader is similar in format to the one
// written by the 32 bit process at the other end.
// The DWORD 'm_dwVersion' was split into two two USHORTS named 'm_Version' and 
// 'm_Flags'. The upper bits of 'm_dwVersion' were never used, nor was 'm_dwVersion'
// used to determine whether an IPC block was valid or compatible. Thus, splitting 
// the 'm_dwVersion' into two USHORTs should not introduce any compatibility issues.
//-----------------------------------------------------------------------------
template <class TModuleHandle>
struct LegacyPrivateIPCHeaderTemplate
{
// header
    USHORT      m_Version;    // version of the IPC Block
    USHORT      m_Flags;      // flags field
    DWORD       m_blockSize;    // Size of the entire shared memory block
    TModuleHandle   m_hInstance;    // instance of module that created this header
    USHORT      m_BuildYear;    // stamp for year built
    USHORT      m_BuildNumber;  // stamp for Month/Day built
    DWORD       m_numEntries;   // Number of entries in the table
};

typedef LegacyPrivateIPCHeaderTemplate<HINSTANCE> LegacyPrivateIPCHeader;

//-----------------------------------------------------------------------------
// This fixes alignment & packing issues.
// This header must remain completely binary compatible w/ older versions.
//-----------------------------------------------------------------------------
struct FullIPCHeaderLegacyPrivate
{
// Header
    LegacyPrivateIPCHeader             m_header;

// Directory
    IPCEntry m_table[eLegacyPrivateIPC_MAX]; // entry describing each client's block

};

//-----------------------------------------------------------------------------
// This fixes alignment & packing issues.
// This header must remain completely binary compatible w/ older versions.
//-----------------------------------------------------------------------------
template <class TModuleHandle>
struct FullIPCHeaderLegacyPublicTemplate
{
// Header
    struct LegacyPrivateIPCHeaderTemplate<TModuleHandle>             m_header;

// Directory
    IPCEntry m_table[eLegacyPublicIPC_MAX]; // entry describing each client's block

};

// In hindsight, we should have made the offsets be absolute, but we made them
// relative to the end of the FullIPCHeader. 
// The problem is that as future versions added new Entries to the directory,
// the header size grew. 
// Thus we make IPCEntry::m_Offset is relative to LEGACY_IPC_ENTRY_OFFSET_BASE, which
// corresponds to sizeof(LegacyPrivateIPCHeader) for an v1.0 /v1.1 build. 
#ifdef _TARGET_X86_
    const DWORD LEGACY_IPC_ENTRY_OFFSET_BASE = 0x14;
#else
    // On non-x86 platforms, we don't need to worry about backwards compat. 
    // But we do need to worry about alignment, so just pretend that everett was 0,
    // and solve both problems.
    const DWORD LEGACY_IPC_ENTRY_OFFSET_BASE = 0x0;
#endif

// When a 64 bit process reads IPC block of a 32 bit process, we need to know the
// LEGACY_IPC_ENTRY_OFFSET_BASE of the latter from the former. So this constant is defined
const DWORD LEGACY_IPC_ENTRY_OFFSET_BASE_WOW64 = 0x14;

// Size of LegacyPublicIPCControlBlock for Everett and Whidbey
const DWORD LEGACYPUBLIC_IPC_BLOCK_SIZE_32 = 0x134;
const DWORD LEGACYPUBLIC_IPC_BLOCK_SIZE_64 = 0x1a0;

//-----------------------------------------------------------------------------
// LegacyPrivate (per process) IPC Block for COM+ apps
//-----------------------------------------------------------------------------
struct LegacyPrivateIPCControlBlock
{
    FullIPCHeaderLegacyPrivate m_FullIPCHeader;


// Client blocks
    struct PerfCounterIPCControlBlock    m_perf;        // no longer used but kept for compat
    struct AppDomainEnumerationIPCBlock m_appdomain;
    WCHAR                               m_instancePath[MAX_LONGPATH];
};

typedef DPTR(LegacyPrivateIPCControlBlock) PTR_LegacyPrivateIPCControlBlock;

#if defined(_TARGET_X86_)
// For perf reasons, we'd like to keep the IPC block small enough to fit on 
// a single page. This assert ensures it won't silently grow past the page boundary
// w/o us knowing about it. If this assert fires, then either:
// - consciously adjust it to let the IPC block be 2 pages.
// - shrink the IPC blocks.
static_assert_no_msg(sizeof(LegacyPrivateIPCControlBlock) <= 4096);
#endif

//-----------------------------------------------------------------------------
// LegacyPublic (per process) IPC Block for CLR apps
//-----------------------------------------------------------------------------
struct LegacyPublicIPCControlBlock
{
    FullIPCHeaderLegacyPublicTemplate<HINSTANCE> m_FullIPCHeaderLegacyPublic;

// Client blocks
    struct PerfCounterIPCControlBlock   m_perf;
};

//-----------------------------------------------------------------------------
// LegacyPublicWow64IPCControlBlock is used by a 64 bit process to read the IPC block
// of a 32 bit process. This struct is similar to LegacyPublicIPCControlBlock, except
// that all pointer (ie platform dependent) sized fields are substituted with 
// DWORDs, so as to match the exact layout of LegacyPublicIPCControlBlock in a 32 bit process
//-----------------------------------------------------------------------------
#pragma pack(push, 4)
struct LegacyPublicWow64IPCControlBlock
{
    FullIPCHeaderLegacyPublicTemplate<DWORD> m_FullIPCHeaderLegacyPublic;

// Client blocks
    PerfCounterWow64IPCControlBlock   m_perf;
};
#pragma pack(pop)

typedef DPTR(LegacyPublicIPCControlBlock) PTR_LegacyPublicIPCControlBlock;

//-----------------------------------------------------------------------------
// Inline definitions
//-----------------------------------------------------------------------------

#include "ipcheader.inl"

#endif // _IPCManagerPriv_h_
