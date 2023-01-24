// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

//
// This header contains binder-generated data structures that the runtime consumes.
//
#include "TargetPtrs.h"

class MethodTable;

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

enum class DispatchCellType
{
    InterfaceAndSlot = 0x0,
    MetadataToken = 0x1,
    VTableOffset = 0x2,
};

struct DispatchCellInfo
{
    DispatchCellType CellType;
    MethodTable *InterfaceType = nullptr;
    uint16_t InterfaceSlot = 0;
    uint8_t HasCache = 0;
    uint32_t MetadataToken = 0;
    uint32_t VTableOffset = 0;
};

struct InterfaceDispatchCacheHeader
{
private:
    enum Flags
    {
        CH_TypeAndSlotIndex = 0x0,
        CH_MetadataToken = 0x1,
        CH_Mask = 0x3,
        CH_Shift = 0x2,
    };

public:
    void Initialize(MethodTable *pInterfaceType, uint16_t interfaceSlot, uint32_t metadataToken)
    {
        if (pInterfaceType != nullptr)
        {
            ASSERT(metadataToken == 0);
            m_pInterfaceType = pInterfaceType;
            m_slotIndexOrMetadataTokenEncoded = CH_TypeAndSlotIndex | (((uint32_t)interfaceSlot) << CH_Shift);
        }
        else
        {
            ASSERT(pInterfaceType == nullptr);
            ASSERT(interfaceSlot == 0);
            m_pInterfaceType = nullptr;
            m_slotIndexOrMetadataTokenEncoded = CH_MetadataToken | (metadataToken << CH_Shift);
        }
    }

    void Initialize(const DispatchCellInfo *pCellInfo)
    {
        ASSERT((pCellInfo->CellType == DispatchCellType::InterfaceAndSlot) ||
               (pCellInfo->CellType == DispatchCellType::MetadataToken));
        if (pCellInfo->CellType == DispatchCellType::InterfaceAndSlot)
        {
            ASSERT(pCellInfo->MetadataToken == 0);
            Initialize(pCellInfo->InterfaceType, pCellInfo->InterfaceSlot, 0);
        }
        else
        {
            ASSERT(pCellInfo->CellType == DispatchCellType::MetadataToken);
            ASSERT(pCellInfo->InterfaceType == nullptr);
            Initialize(nullptr, 0, pCellInfo->MetadataToken);
        }
    }

    DispatchCellInfo GetDispatchCellInfo()
    {
        DispatchCellInfo cellInfo;

        if ((m_slotIndexOrMetadataTokenEncoded & CH_Mask) == CH_TypeAndSlotIndex)
        {
            cellInfo.InterfaceType = m_pInterfaceType;
            cellInfo.InterfaceSlot = (uint16_t)(m_slotIndexOrMetadataTokenEncoded >> CH_Shift);
            cellInfo.CellType = DispatchCellType::InterfaceAndSlot;
        }
        else
        {
            cellInfo.MetadataToken = m_slotIndexOrMetadataTokenEncoded >> CH_Shift;
            cellInfo.CellType = DispatchCellType::MetadataToken;
        }
        cellInfo.HasCache = 1;
        return cellInfo;
    }

private:
    MethodTable *    m_pInterfaceType;   // MethodTable of interface to dispatch on
    uint32_t      m_slotIndexOrMetadataTokenEncoded;
};

// One of these is allocated per interface call site. It holds the stub to call, data to pass to that stub
// (cache information) and the interface contract, i.e. the interface type and slot being called.
struct InterfaceDispatchCell
{
    // The first two fields must remain together and at the beginning of the structure. This is due to the
    // synchronization requirements of the code that updates these at runtime and the instructions generated
    // by the binder for interface call sites.
    UIntTarget      m_pStub;    // Call this code to execute the interface dispatch
    volatile UIntTarget m_pCache;   // Context used by the stub above (one or both of the low two bits are set
                                    // for initial dispatch, and if not set, using this as a cache pointer or
                                    // as a vtable offset.)
                                    //
                                    // In addition, there is a Slot/Flag use of this field. DispatchCells are
                                    // emitted as a group, and the final one in the group (identified by m_pStub
                                    // having the null value) will have a Slot field is the low 16 bits of the
                                    // m_pCache field, and in the second lowest 16 bits, a Flags field. For the interface
                                    // case Flags shall be 0, and for the metadata token case, Flags shall be 1.

    //
    // Keep these in sync with the managed copy in src\Common\src\Internal\Runtime\InterfaceCachePointerType.cs
    //
    enum Flags
    {
        // The low 2 bits of the m_pCache pointer are treated specially so that we can avoid the need for
        // extra fields on this type.
        // OR if the m_pCache value is less than 0x1000 then this it is a vtable offset and should be used as such
        IDC_CachePointerIsInterfaceRelativePointer = 0x3,
        IDC_CachePointerIsIndirectedInterfaceRelativePointer = 0x2,
        IDC_CachePointerIsInterfacePointerOrMetadataToken = 0x1, // Metadata token is a 30 bit number in this case.
                                                                 // Tokens are required to have at least one of their upper 20 bits set
                                                                 // But they are not required by this part of the system to follow any specific
                                                                 // token format
        IDC_CachePointerPointsAtCache = 0x0,
        IDC_CachePointerMask = 0x3,
        IDC_CachePointerMaskShift = 0x2,
        IDC_MaxVTableOffsetPlusOne = 0x1000,
    };

    DispatchCellInfo GetDispatchCellInfo()
    {
        // Capture m_pCache into a local for safe access (this is a volatile read of a value that may be
        // modified on another thread while this function is executing.)
        UIntTarget cachePointerValue = m_pCache;
        DispatchCellInfo cellInfo;

        if ((cachePointerValue < IDC_MaxVTableOffsetPlusOne) && ((cachePointerValue & IDC_CachePointerMask) == IDC_CachePointerPointsAtCache))
        {
            cellInfo.VTableOffset = (uint32_t)cachePointerValue;
            cellInfo.CellType = DispatchCellType::VTableOffset;
            cellInfo.HasCache = 1;
            return cellInfo;
        }

        // If there is a real cache pointer, grab the data from there.
        if ((cachePointerValue & IDC_CachePointerMask) == IDC_CachePointerPointsAtCache)
        {
            return ((InterfaceDispatchCacheHeader*)cachePointerValue)->GetDispatchCellInfo();
        }

        // Otherwise, walk to cell with Flags and Slot field

        // The slot number/flags for a dispatch cell is encoded once per run of DispatchCells
        // The run is terminated by having an dispatch cell with a null stub pointer.
        const InterfaceDispatchCell *currentCell = this;
        while (currentCell->m_pStub != 0)
        {
            currentCell = currentCell + 1;
        }
        UIntTarget cachePointerValueFlags = currentCell->m_pCache;

        DispatchCellType cellType = (DispatchCellType)(cachePointerValueFlags >> 16);
        cellInfo.CellType = cellType;

        if (cellType == DispatchCellType::InterfaceAndSlot)
        {
            cellInfo.InterfaceSlot = (uint16_t)cachePointerValueFlags;

            switch (cachePointerValue & IDC_CachePointerMask)
            {
            case IDC_CachePointerIsInterfacePointerOrMetadataToken:
                cellInfo.InterfaceType = (MethodTable*)(cachePointerValue & ~IDC_CachePointerMask);
                break;

            case IDC_CachePointerIsInterfaceRelativePointer:
            case IDC_CachePointerIsIndirectedInterfaceRelativePointer:
                {
                    UIntTarget interfacePointerValue = (UIntTarget)&m_pCache + (int32_t)cachePointerValue;
                    interfacePointerValue &= ~IDC_CachePointerMask;
                    if ((cachePointerValue & IDC_CachePointerMask) == IDC_CachePointerIsInterfaceRelativePointer)
                    {
                        cellInfo.InterfaceType = (MethodTable*)interfacePointerValue;
                    }
                    else
                    {
                        cellInfo.InterfaceType = *(MethodTable**)interfacePointerValue;
                    }
                }
                break;
            }
        }
        else
        {
            cellInfo.MetadataToken = (uint32_t)(cachePointerValue >> IDC_CachePointerMaskShift);
        }

        return cellInfo;
    }

    static bool IsCache(UIntTarget value)
    {
        if (((value & IDC_CachePointerMask) != 0) || (value < IDC_MaxVTableOffsetPlusOne))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    InterfaceDispatchCacheHeader* GetCache() const
    {
        // Capture m_pCache into a local for safe access (this is a volatile read of a value that may be
        // modified on another thread while this function is executing.)
        UIntTarget cachePointerValue = m_pCache;
        if (IsCache(cachePointerValue))
        {
            return (InterfaceDispatchCacheHeader*)cachePointerValue;
        }
        else
        {
            return 0;
        }
    }
};

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

#ifdef TARGET_ARM
// Note for ARM: try and keep the flags in the low 16-bits, since they're not easy to load into a register in
// a single instruction within our stubs.
enum PInvokeTransitionFrameFlags
{
    // NOTE: Keep in sync with src\coreclr\nativeaot\Runtime\arm\AsmMacros.h

    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp

    // standard preserved registers
    PTFF_SAVE_R4        = 0x00000001,
    PTFF_SAVE_R5        = 0x00000002,
    PTFF_SAVE_R6        = 0x00000004,
    PTFF_SAVE_R7        = 0x00000008,   // should never be used, we require FP frames for methods with
                                        // pinvoke and it is saved into the frame pointer field instead
    PTFF_SAVE_R8        = 0x00000010,
    PTFF_SAVE_R9        = 0x00000020,
    PTFF_SAVE_R10       = 0x00000040,
    PTFF_SAVE_SP        = 0x00000100,   // Used for 'coop pinvokes' in runtime helper routines.  Methods with
                                        // PInvokes are required to have a frame pointers, but methods which
                                        // call runtime helpers are not.  Therefore, methods that call runtime
                                        // helpers may need SP to seed the stackwalk.

    // scratch registers
    PTFF_SAVE_R0        = 0x00000200,
    PTFF_SAVE_R1        = 0x00000400,
    PTFF_SAVE_R2        = 0x00000800,
    PTFF_SAVE_R3        = 0x00001000,
    PTFF_SAVE_LR        = 0x00002000,   // this is useful for the case of loop hijacking where we need both
                                        // a return address pointing into the hijacked method and that method's
                                        // lr register, which may hold a gc pointer

    PTFF_R0_IS_GCREF    = 0x00004000,   // used by hijack handler to report return value of hijacked method
    PTFF_R0_IS_BYREF    = 0x00008000,   // used by hijack handler to report return value of hijacked method

    PTFF_THREAD_ABORT   = 0x00010000,   // indicates that ThreadAbortException should be thrown when returning from the transition
};
#elif defined(TARGET_ARM64)
enum PInvokeTransitionFrameFlags : uint64_t
{
    // NOTE: Keep in sync with src\coreclr\nativeaot\Runtime\arm64\AsmMacros.h

    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp

    // standard preserved registers
    PTFF_SAVE_X19       = 0x0000000000000001,
    PTFF_SAVE_X20       = 0x0000000000000002,
    PTFF_SAVE_X21       = 0x0000000000000004,
    PTFF_SAVE_X22       = 0x0000000000000008,
    PTFF_SAVE_X23       = 0x0000000000000010,
    PTFF_SAVE_X24       = 0x0000000000000020,
    PTFF_SAVE_X25       = 0x0000000000000040,
    PTFF_SAVE_X26       = 0x0000000000000080,
    PTFF_SAVE_X27       = 0x0000000000000100,
    PTFF_SAVE_X28       = 0x0000000000000200,

    PTFF_SAVE_SP        = 0x0000000000000400,   // Used for 'coop pinvokes' in runtime helper routines.  Methods with
                                                // PInvokes are required to have a frame pointers, but methods which
                                                // call runtime helpers are not.  Therefore, methods that call runtime
                                                // helpers may need SP to seed the stackwalk.

    // Scratch registers
    PTFF_SAVE_X0        = 0x0000000000000800,
    PTFF_SAVE_X1        = 0x0000000000001000,
    PTFF_SAVE_X2        = 0x0000000000002000,
    PTFF_SAVE_X3        = 0x0000000000004000,
    PTFF_SAVE_X4        = 0x0000000000008000,
    PTFF_SAVE_X5        = 0x0000000000010000,
    PTFF_SAVE_X6        = 0x0000000000020000,
    PTFF_SAVE_X7        = 0x0000000000040000,
    PTFF_SAVE_X8        = 0x0000000000080000,
    PTFF_SAVE_X9        = 0x0000000000100000,
    PTFF_SAVE_X10       = 0x0000000000200000,
    PTFF_SAVE_X11       = 0x0000000000400000,
    PTFF_SAVE_X12       = 0x0000000000800000,
    PTFF_SAVE_X13       = 0x0000000001000000,
    PTFF_SAVE_X14       = 0x0000000002000000,
    PTFF_SAVE_X15       = 0x0000000004000000,
    PTFF_SAVE_X16       = 0x0000000008000000,
    PTFF_SAVE_X17       = 0x0000000010000000,
    PTFF_SAVE_X18       = 0x0000000020000000,

    PTFF_SAVE_FP        = 0x0000000040000000,   // should never be used, we require FP frames for methods with
                                                // pinvoke and it is saved into the frame pointer field instead

    PTFF_SAVE_LR        = 0x0000000080000000,   // this is useful for the case of loop hijacking where we need both
                                                // a return address pointing into the hijacked method and that method's
                                                // lr register, which may hold a gc pointer

    // used by hijack handler to report return value of hijacked method
    PTFF_X0_IS_GCREF    = 0x0000000100000000,
    PTFF_X0_IS_BYREF    = 0x0000000200000000,
    PTFF_X1_IS_GCREF    = 0x0000000400000000,
    PTFF_X1_IS_BYREF    = 0x0000000800000000,

    PTFF_THREAD_ABORT   = 0x0000001000000000,   // indicates that ThreadAbortException should be thrown when returning from the transition
};


#else // TARGET_ARM
enum PInvokeTransitionFrameFlags
{
    // NOTE: Keep in sync with src\coreclr\nativeaot\Runtime\[amd64|i386]\AsmMacros.inc

    // NOTE: The order in which registers get pushed in the PInvokeTransitionFrame's m_PreservedRegs list has
    //       to match the order of these flags (that's also the order in which they are read in StackFrameIterator.cpp

    // standard preserved registers
    PTFF_SAVE_RBX       = 0x00000001,
    PTFF_SAVE_RSI       = 0x00000002,
    PTFF_SAVE_RDI       = 0x00000004,
    PTFF_SAVE_RBP       = 0x00000008,   // should never be used, we require RBP frames for methods with
                                        // pinvoke and it is saved into the frame pointer field instead
    PTFF_SAVE_R12       = 0x00000010,
    PTFF_SAVE_R13       = 0x00000020,
    PTFF_SAVE_R14       = 0x00000040,
    PTFF_SAVE_R15       = 0x00000080,

    PTFF_SAVE_RSP       = 0x00008000,   // Used for 'coop pinvokes' in runtime helper routines.  Methods with
                                        // PInvokes are required to have a frame pointers, but methods which
                                        // call runtime helpers are not.  Therefore, methods that call runtime
                                        // helpers may need RSP to seed the stackwalk.
                                        //
                                        // NOTE: despite the fact that this flag's bit is out of order, it is
                                        // still expected to be saved here after the preserved registers and
                                        // before the scratch registers
    PTFF_SAVE_RAX       = 0x00000100,
    PTFF_SAVE_RCX       = 0x00000200,
    PTFF_SAVE_RDX       = 0x00000400,
    PTFF_SAVE_R8        = 0x00000800,
    PTFF_SAVE_R9        = 0x00001000,
    PTFF_SAVE_R10       = 0x00002000,
    PTFF_SAVE_R11       = 0x00004000,

    PTFF_RAX_IS_GCREF   = 0x00010000,   // used by hijack handler to report return value of hijacked method
    PTFF_RAX_IS_BYREF   = 0x00020000,   
    PTFF_RDX_IS_GCREF   = 0x00040000,   
    PTFF_RDX_IS_BYREF   = 0x00080000,   

    PTFF_THREAD_ABORT   = 0x00100000,   // indicates that ThreadAbortException should be thrown when returning from the transition
};
#endif // TARGET_ARM

#pragma warning(push)
#pragma warning(disable:4200) // nonstandard extension used: zero-sized array in struct/union
class Thread;
#if defined(USE_PORTABLE_HELPERS)
//the members of this structure are currently unused except m_pThread and exist only to allow compilation
//of StackFrameIterator their values are not currently being filled in and will require significant rework
//in order to satisfy the runtime requirements of StackFrameIterator
struct PInvokeTransitionFrame
{
    void*       m_RIP;
    Thread*     m_pThread;  // unused by stack crawler, this is so GetThread is only called once per method
                            // can be an invalid pointer in universal transition cases (which never need to call GetThread)
    uint32_t    m_Flags;    // PInvokeTransitionFrameFlags
};
#else // USE_PORTABLE_HELPERS
struct PInvokeTransitionFrame
{
#ifdef TARGET_ARM
    TgtPTR_Void     m_ChainPointer; // R11, used by OS to walk stack quickly
#endif
#ifdef TARGET_ARM64
    // On arm64, the FP and LR registers are pushed in that order when setting up frames
    TgtPTR_Void     m_FramePointer;
    TgtPTR_Void     m_RIP;
#else
    TgtPTR_Void     m_RIP;
    TgtPTR_Void     m_FramePointer;
#endif
    TgtPTR_Thread   m_pThread;  // unused by stack crawler, this is so GetThread is only called once per method
                                // can be an invalid pointer in universal transition cases (which never need to call GetThread)
#ifdef TARGET_ARM64
    uint64_t          m_Flags;  // PInvokeTransitionFrameFlags
#else
    uint32_t          m_Flags;  // PInvokeTransitionFrameFlags
#endif
    UIntTarget      m_PreservedRegs[];
};
#endif // USE_PORTABLE_HELPERS
#pragma warning(pop)

#ifdef TARGET_AMD64
// RBX, RSI, RDI, R12, R13, R14, R15, RAX, RSP
#define PInvokeTransitionFrame_SaveRegs_count 9
#elif defined(TARGET_X86)
// RBX, RSI, RDI, RAX, RSP
#define PInvokeTransitionFrame_SaveRegs_count 5
#elif defined(TARGET_ARM)
// R4-R10, R0, SP
#define PInvokeTransitionFrame_SaveRegs_count 9
#endif
#define PInvokeTransitionFrame_MAX_SIZE (sizeof(PInvokeTransitionFrame) + (POINTER_SIZE * PInvokeTransitionFrame_SaveRegs_count))

#ifdef TARGET_AMD64
#define OFFSETOF__Thread__m_pTransitionFrame 0x40
#elif defined(TARGET_ARM64)
#define OFFSETOF__Thread__m_pTransitionFrame 0x40
#elif defined(TARGET_X86)
#define OFFSETOF__Thread__m_pTransitionFrame 0x2c
#elif defined(TARGET_ARM)
#define OFFSETOF__Thread__m_pTransitionFrame 0x2c
#endif

typedef DPTR(MethodTable) PTR_EEType;
typedef DPTR(PTR_EEType) PTR_PTR_EEType;

struct EETypeRef
{
    union
    {
        MethodTable *    pEEType;
        MethodTable **   ppEEType;
        uint8_t *     rawPtr;
        UIntTarget  rawTargetPtr; // x86_amd64: keeps union big enough for target-platform pointer
    };

    static const size_t DOUBLE_INDIR_FLAG = 1;

    PTR_EEType GetValue()
    {
        if (dac_cast<TADDR>(rawTargetPtr) & DOUBLE_INDIR_FLAG)
            return *dac_cast<PTR_PTR_EEType>(rawTargetPtr - DOUBLE_INDIR_FLAG);
        else
            return dac_cast<PTR_EEType>(rawTargetPtr);
    }
};

// Blobs are opaque data passed from the compiler, through the binder and into the native image. At runtime we
// provide a simple API to retrieve these blobs (they're keyed by a simple integer ID). Blobs are passed to
// the binder from the compiler and stored in native images by the binder in a sequential stream, each blob
// having the following header.
struct BlobHeader
{
    uint32_t m_flags;  // Flags describing the blob (used by the binder only at the moment)
    uint32_t m_id;     // Unique identifier of the blob (used to access the blob at runtime)
                     // also used by BlobTypeFieldPreInit to identify (at bind time) which field to pre-init.
    uint32_t m_size;   // Size of the individual blob excluding this header (DWORD aligned)
};

#ifdef FEATURE_CUSTOM_IMPORTS
struct CustomImportDescriptor
{
    uint32_t  RvaEATAddr;  // RVA of the indirection cell of the address of the EAT for that module
    uint32_t  RvaIAT;      // RVA of IAT array for that module
    uint32_t  CountIAT;    // Count of entries in the above array
};
#endif // FEATURE_CUSTOM_IMPORTS

enum RhEHClauseKind
{
    RH_EH_CLAUSE_TYPED              = 0,
    RH_EH_CLAUSE_FAULT              = 1,
    RH_EH_CLAUSE_FILTER             = 2,
    RH_EH_CLAUSE_UNUSED             = 3
};

#define RH_EH_CLAUSE_TYPED_INDIRECT RH_EH_CLAUSE_UNUSED

// mapping of cold code blocks to the corresponding hot entry point RVA
// format is a as follows:
// -------------------
// | subSectionCount |     # of subsections, where each subsection has a run of hot bodies
// -------------------     followed by a run of cold bodies
// | hotMethodCount  |     # of hot bodies in subsection
// | coldMethodCount |     # of cold bodies in subsection
// -------------------
// ... possibly repeated on ARM
// -------------------
// | hotRVA #1       |     RVA of the hot entry point corresponding to the 1st cold body
// | hotRVA #2       |     RVA of the hot entry point corresponding to the 2nd cold body
// ... one entry for each cold body containing the corresponding hot entry point

// number of hot and cold bodies in a subsection of code
// in x86 and x64 there's only one subsection, on ARM there may be several
// for large modules with > 16 MB of code
struct SubSectionDesc
{
    uint32_t          hotMethodCount;
    uint32_t          coldMethodCount;
};

// this is the structure describing the cold to hot mapping info
struct ColdToHotMapping
{
    uint32_t          subSectionCount;
    SubSectionDesc  subSection[/*subSectionCount*/1];
    //  UINT32   hotRVAofColdMethod[/*coldMethodCount*/];
};

