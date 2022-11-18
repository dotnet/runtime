// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
//
// Internal data access functionality.
//
//*****************************************************************************

#ifndef _DACPRIVATE_H_
#define _DACPRIVATE_H_

#include <cor.h>
#include <clrdata.h>
#include <xclrdata.h>
#include <sospriv.h>

#ifndef TARGET_UNIX
// It is unfortunate having to include this header just to get the definition of GenericModeBlock
#include <msodw.h>
#endif // TARGET_UNIX

//
// Whenever a structure is marshalled between different platforms, we need to ensure the
// layout is the same in both cases.  We tell GCC to use the MSVC-style packing with
// the following attribute.  The main thing this appears to control is whether
// 8-byte values are aligned at 4-bytes (GCC default) or 8-bytes (MSVC default).
// This attribute affects only the immediate struct it is applied to, you must also apply
// it to any nested structs if you want their layout affected as well.  You also must
// apply this to unions embedded in other structures, since it can influence the starting
// alignment.
//
// Note that there doesn't appear to be any disadvantage to applying this a little
// more agressively than necessary, so we generally use it on all classes / structures
// defined in a file that defines marshalled data types (eg. DacDbiStructures.h)
// The -mms-bitfields compiler option also does this for the whole file, but we don't
// want to go changing the layout of, for example, structures defined in OS header files
// so we explicitly opt-in with this attribute.
//
#if defined(__GNUC__) && defined(HOST_X86)
#define MSLAYOUT __attribute__((__ms_struct__))
#else
#define MSLAYOUT
#endif

#include <livedatatarget.h>

//----------------------------------------------------------------------------
//
// Internal CLRData requests.
//
//----------------------------------------------------------------------------


// Private requests for DataModules
enum
{
    DACDATAMODULEPRIV_REQUEST_GET_MODULEPTR = 0xf0000000,
    DACDATAMODULEPRIV_REQUEST_GET_MODULEDATA = 0xf0000001
};


// Private requests for stack walkers.
enum
{
    DACSTACKPRIV_REQUEST_FRAME_DATA = 0xf0000000
};

enum DacpObjectType { OBJ_STRING=0,OBJ_FREE,OBJ_OBJECT,OBJ_ARRAY,OBJ_OTHER };
struct MSLAYOUT DacpObjectData
{
    CLRDATA_ADDRESS MethodTable = 0;
    DacpObjectType ObjectType = DacpObjectType::OBJ_STRING;
    ULONG64 Size = 0;
    CLRDATA_ADDRESS ElementTypeHandle = 0;
    CorElementType ElementType = CorElementType::ELEMENT_TYPE_END;
    DWORD dwRank = 0;
    ULONG64 dwNumComponents = 0;
    ULONG64 dwComponentSize = 0;
    CLRDATA_ADDRESS ArrayDataPtr = 0;
    CLRDATA_ADDRESS ArrayBoundsPtr = 0;
    CLRDATA_ADDRESS ArrayLowerBoundsPtr = 0;

    CLRDATA_ADDRESS RCW = 0;
    CLRDATA_ADDRESS CCW = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetObjectData(addr, this);
    }
};

struct MSLAYOUT DacpExceptionObjectData
{
    CLRDATA_ADDRESS   Message = 0;
    CLRDATA_ADDRESS   InnerException = 0;
    CLRDATA_ADDRESS   StackTrace = 0;
    CLRDATA_ADDRESS   WatsonBuckets = 0;
    CLRDATA_ADDRESS   StackTraceString = 0;
    CLRDATA_ADDRESS   RemoteStackTraceString = 0;
    INT32             HResult = 0;
    INT32             XCode = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        HRESULT hr;
        ISOSDacInterface2 *psos2 = NULL;
        if (SUCCEEDED(hr = sos->QueryInterface(__uuidof(ISOSDacInterface2), (void**) &psos2)))
        {
            hr = psos2->GetObjectExceptionData(addr, this);
            psos2->Release();
        }
        return hr;
    }
};

struct MSLAYOUT DacpUsefulGlobalsData
{
    CLRDATA_ADDRESS ArrayMethodTable = 0;
    CLRDATA_ADDRESS StringMethodTable = 0;
    CLRDATA_ADDRESS ObjectMethodTable = 0;
    CLRDATA_ADDRESS ExceptionMethodTable = 0;
    CLRDATA_ADDRESS FreeMethodTable = 0;
};

struct MSLAYOUT DacpFieldDescData
{
    CorElementType Type = CorElementType::ELEMENT_TYPE_END;
    CorElementType sigType = CorElementType::ELEMENT_TYPE_END;     // ELEMENT_TYPE_XXX from signature. We need this to disply pretty name for String in minidump's case
    CLRDATA_ADDRESS MTOfType = 0; // NULL if Type is not loaded

    CLRDATA_ADDRESS ModuleOfType = 0;
    mdTypeDef TokenOfType = 0;

    mdFieldDef mb = 0;
    CLRDATA_ADDRESS MTOfEnclosingClass = 0;
    DWORD dwOffset = 0;
    BOOL bIsThreadLocal = FALSE;
    BOOL bIsContextLocal = FALSE;
    BOOL bIsStatic = FALSE;
    CLRDATA_ADDRESS NextField = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetFieldDescData(addr, this);
    }
};

struct MSLAYOUT DacpMethodTableFieldData
{
    WORD wNumInstanceFields = 0;
    WORD wNumStaticFields = 0;
    WORD wNumThreadStaticFields = 0;

    CLRDATA_ADDRESS FirstField = 0; // If non-null, you can retrieve more

    WORD wContextStaticOffset = 0;
    WORD wContextStaticsSize = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodTableFieldData(addr, this);
    }
};

struct MSLAYOUT DacpMethodTableCollectibleData
{
    CLRDATA_ADDRESS LoaderAllocatorObjectHandle = 0;
    BOOL bCollectible = FALSE;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        HRESULT hr;
        ISOSDacInterface6 *pSOS6 = NULL;
        if (SUCCEEDED(hr = sos->QueryInterface(__uuidof(ISOSDacInterface6), (void**)&pSOS6)))
        {
            hr = pSOS6->GetMethodTableCollectibleData(addr, this);
            pSOS6->Release();
        }

        return hr;
    }
};

struct MSLAYOUT DacpMethodTableTransparencyData
{
    BOOL bHasCriticalTransparentInfo = FALSE;
    BOOL bIsCritical = FALSE;
    BOOL bIsTreatAsSafe = FALSE;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodTableTransparencyData(addr, this);
    }
};

struct MSLAYOUT DacpDomainLocalModuleData
{
    // These two parameters are used as input params when calling the
    // no-argument form of Request below.
    CLRDATA_ADDRESS appDomainAddr = 0;
    ULONG64  ModuleID = 0;

    CLRDATA_ADDRESS pClassData = 0;
    CLRDATA_ADDRESS pDynamicClassTable = 0;
    CLRDATA_ADDRESS pGCStaticDataStart = 0;
    CLRDATA_ADDRESS pNonGCStaticDataStart = 0;

    // Called when you have a pointer to the DomainLocalModule
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetDomainLocalModuleData(addr, this);
    }
};


struct MSLAYOUT DacpThreadLocalModuleData
{
    // These two parameters are used as input params when calling the
    // no-argument form of Request below.
    CLRDATA_ADDRESS threadAddr = 0;
    ULONG64 ModuleIndex = 0;

    CLRDATA_ADDRESS pClassData = 0;
    CLRDATA_ADDRESS pDynamicClassTable = 0;
    CLRDATA_ADDRESS pGCStaticDataStart = 0;
    CLRDATA_ADDRESS pNonGCStaticDataStart = 0;
};


struct MSLAYOUT DacpModuleData
{
    CLRDATA_ADDRESS Address = 0;
    CLRDATA_ADDRESS PEAssembly = 0; // A PEAssembly addr
    CLRDATA_ADDRESS ilBase = 0;
    CLRDATA_ADDRESS metadataStart = 0;
    ULONG64 metadataSize = 0;
    CLRDATA_ADDRESS Assembly = 0; // Assembly pointer
    BOOL bIsReflection = FALSE;
    BOOL bIsPEFile = FALSE;
    ULONG64 dwBaseClassIndex = 0;
    ULONG64 dwModuleID = 0;

    DWORD dwTransientFlags = 0;

    CLRDATA_ADDRESS TypeDefToMethodTableMap = 0;
    CLRDATA_ADDRESS TypeRefToMethodTableMap = 0;
    CLRDATA_ADDRESS MethodDefToDescMap = 0;
    CLRDATA_ADDRESS FieldDefToDescMap = 0;
    CLRDATA_ADDRESS MemberRefToDescMap = 0;
    CLRDATA_ADDRESS FileReferencesMap = 0;
    CLRDATA_ADDRESS ManifestModuleReferencesMap = 0;

    CLRDATA_ADDRESS pLookupTableHeap = 0;
    CLRDATA_ADDRESS pThunkHeap = 0;

    ULONG64 dwModuleIndex = 0;

    DacpModuleData()
    {
    }

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetModuleData(addr, this);
    }

private:
    // Ensure that this data structure is not copied.
    DacpModuleData(const DacpModuleData&);
    void operator=(const DacpModuleData&);
};

struct MSLAYOUT DacpMethodTableData
{
    BOOL bIsFree = FALSE; // everything else is NULL if this is true.
    CLRDATA_ADDRESS Module = 0;
    CLRDATA_ADDRESS Class = 0;
    CLRDATA_ADDRESS ParentMethodTable = 0;
    WORD wNumInterfaces = 0;
    WORD wNumMethods = 0;
    WORD wNumVtableSlots = 0;
    WORD wNumVirtuals = 0;
    DWORD BaseSize = 0;
    DWORD ComponentSize = 0;
    mdTypeDef cl = 0; // Metadata token
    DWORD dwAttrClass = 0; // cached metadata
    BOOL bIsShared = FALSE;  // Always false, preserved for backward compatibility
    BOOL bIsDynamic = FALSE;
    BOOL bContainsPointers = FALSE;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodTableData(addr, this);
    }
};


// Copied from util.hpp, for DacpThreadStoreData.fHostConfig below.
#define CLRMEMORYHOSTED                             0x1
#define CLRTASKHOSTED                               0x2
#define CLRSYNCHOSTED                               0x4
#define CLRTHREADPOOLHOSTED                         0x8
#define CLRIOCOMPLETIONHOSTED                       0x10
#define CLRASSEMBLYHOSTED                           0x20
#define CLRGCHOSTED                                 0x40
#define CLRSECURITYHOSTED                           0x80
#define CLRHOSTED           0x80000000

struct MSLAYOUT DacpThreadStoreData
{
    LONG threadCount = 0;
    LONG unstartedThreadCount = 0;
    LONG backgroundThreadCount = 0;
    LONG pendingThreadCount = 0;
    LONG deadThreadCount = 0;
    CLRDATA_ADDRESS firstThread = 0;
    CLRDATA_ADDRESS finalizerThread = 0;
    CLRDATA_ADDRESS gcThread = 0;
    DWORD fHostConfig = 0;          // Uses hosting flags defined above

    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetThreadStoreData(this);
    }
};

struct MSLAYOUT DacpAppDomainStoreData
{
    CLRDATA_ADDRESS sharedDomain = 0;
    CLRDATA_ADDRESS systemDomain = 0;
    LONG DomainCount = 0;

    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetAppDomainStoreData(this);
    }
};

struct MSLAYOUT DacpCOMInterfacePointerData
{
    CLRDATA_ADDRESS methodTable = 0;
    CLRDATA_ADDRESS interfacePtr = 0;
    CLRDATA_ADDRESS comContext = 0;
};

struct MSLAYOUT DacpRCWData
{
    CLRDATA_ADDRESS identityPointer = 0;
    CLRDATA_ADDRESS unknownPointer = 0;
    CLRDATA_ADDRESS managedObject = 0;
    CLRDATA_ADDRESS jupiterObject = 0;
    CLRDATA_ADDRESS vtablePtr = 0;
    CLRDATA_ADDRESS creatorThread = 0;
    CLRDATA_ADDRESS ctxCookie = 0;

    LONG refCount = 0;
    LONG interfaceCount = 0;

    BOOL isJupiterObject = FALSE;
    BOOL supportsIInspectable = FALSE;
    BOOL isAggregated = FALSE;
    BOOL isContained = FALSE;
    BOOL isFreeThreaded = FALSE;
    BOOL isDisconnected = FALSE;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS rcw)
    {
        return sos->GetRCWData(rcw, this);
    }

    HRESULT IsDCOMProxy(ISOSDacInterface *sos, CLRDATA_ADDRESS rcw, BOOL* isDCOMProxy)
    {
        ISOSDacInterface2 *pSOS2 = nullptr;
        HRESULT hr = sos->QueryInterface(__uuidof(ISOSDacInterface2), reinterpret_cast<LPVOID*>(&pSOS2));
        if (SUCCEEDED(hr))
        {
            hr = pSOS2->IsRCWDCOMProxy(rcw, isDCOMProxy);
            pSOS2->Release();
        }

        return hr;
    }
};

struct MSLAYOUT DacpCCWData
{
    CLRDATA_ADDRESS outerIUnknown = 0;
    CLRDATA_ADDRESS managedObject = 0;
    CLRDATA_ADDRESS handle = 0;
    CLRDATA_ADDRESS ccwAddress = 0;

    LONG refCount = 0;
    LONG interfaceCount = 0;
    BOOL isNeutered = FALSE;

    LONG jupiterRefCount = 0;
    BOOL isPegged = FALSE;
    BOOL isGlobalPegged = FALSE;
    BOOL hasStrongRef = FALSE;
    BOOL isExtendsCOMObject = FALSE;
    BOOL isAggregated = FALSE;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS ccw)
    {
        return sos->GetCCWData(ccw, this);
    }
};

enum DacpAppDomainDataStage {
    STAGE_CREATING,
    STAGE_READYFORMANAGEDCODE,
    STAGE_ACTIVE,
    STAGE_OPEN,
    STAGE_UNLOAD_REQUESTED,
    STAGE_EXITING,
    STAGE_EXITED,
    STAGE_FINALIZING,
    STAGE_FINALIZED,
    STAGE_HANDLETABLE_NOACCESS,
    STAGE_CLEARED,
    STAGE_COLLECTED,
    STAGE_CLOSED
};

// Information about a BaseDomain (AppDomain, SharedDomain or SystemDomain).
// For types other than AppDomain, some fields (like dwID, DomainLocalBlock, etc.) will be 0/null.
struct MSLAYOUT DacpAppDomainData
{
    // The pointer to the BaseDomain (not necessarily an AppDomain).
    // It's useful to keep this around in the structure
    CLRDATA_ADDRESS AppDomainPtr = 0;
    CLRDATA_ADDRESS AppSecDesc = 0;
    CLRDATA_ADDRESS pLowFrequencyHeap = 0;
    CLRDATA_ADDRESS pHighFrequencyHeap = 0;
    CLRDATA_ADDRESS pStubHeap = 0;
    CLRDATA_ADDRESS DomainLocalBlock = 0;
    CLRDATA_ADDRESS pDomainLocalModules = 0;
    // The creation sequence number of this app domain (starting from 1)
    DWORD dwId = 0;
    LONG AssemblyCount = 0;
    LONG FailedAssemblyCount = 0;
    DacpAppDomainDataStage appDomainStage = DacpAppDomainDataStage::STAGE_CREATING;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetAppDomainData(addr, this);
    }
};

struct MSLAYOUT DacpAssemblyData
{
    CLRDATA_ADDRESS AssemblyPtr = 0; //useful to have
    CLRDATA_ADDRESS ClassLoader = 0;
    CLRDATA_ADDRESS ParentDomain = 0;
    CLRDATA_ADDRESS BaseDomainPtr = 0;
    CLRDATA_ADDRESS AssemblySecDesc = 0;
    BOOL isDynamic = FALSE;
    UINT ModuleCount = FALSE;
    UINT LoadContext = FALSE;
    BOOL isDomainNeutral = FALSE; // Always false, preserved for backward compatibility
    DWORD dwLocationFlags = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr, CLRDATA_ADDRESS baseDomainPtr)
    {
        return sos->GetAssemblyData(baseDomainPtr, addr, this);
    }

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return Request(sos, addr, NULL);
    }
};


struct MSLAYOUT DacpThreadData
{
    DWORD corThreadId = 0;
    DWORD osThreadId = 0;
    int state = 0;
    ULONG preemptiveGCDisabled = 0;
    CLRDATA_ADDRESS allocContextPtr = 0;
    CLRDATA_ADDRESS allocContextLimit = 0;
    CLRDATA_ADDRESS context = 0;
    CLRDATA_ADDRESS domain = 0;
    CLRDATA_ADDRESS pFrame = 0;
    DWORD lockCount = 0;
    CLRDATA_ADDRESS firstNestedException = 0; // Pass this pointer to DacpNestedExceptionInfo
    CLRDATA_ADDRESS teb = 0;
    CLRDATA_ADDRESS fiberData = 0;
    CLRDATA_ADDRESS lastThrownObjectHandle = 0;
    CLRDATA_ADDRESS nextThread = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetThreadData(addr, this);
    }
};


struct MSLAYOUT DacpReJitData
{
    enum Flags
    {
        kUnknown,
        kRequested,
        kActive,
        kReverted,
    };

    CLRDATA_ADDRESS                 rejitID = 0;
    Flags                           flags = Flags::kUnknown;
    CLRDATA_ADDRESS                 NativeCodeAddr = 0;
};

struct MSLAYOUT DacpReJitData2
{
    enum Flags
    {
        kUnknown,
        kRequested,
        kActive,
        kReverted,
    };

    ULONG                           rejitID = 0;
    Flags                           flags = Flags::kUnknown;
    CLRDATA_ADDRESS                 il = 0;
    CLRDATA_ADDRESS                 ilCodeVersionNodePtr = 0;
};

struct MSLAYOUT DacpProfilerILData
{
    enum ModificationType
    {
        Unmodified,
        ILModified,
        ReJITModified,
    };

    ModificationType                type = ModificationType::Unmodified;
    CLRDATA_ADDRESS                 il = 0;
    ULONG                           rejitID = 0;
};

struct MSLAYOUT DacpMethodDescData
{
    BOOL            bHasNativeCode = FALSE;
    BOOL            bIsDynamic = FALSE;
    WORD            wSlotNumber = 0;
    CLRDATA_ADDRESS NativeCodeAddr = 0;
    // Useful for breaking when a method is jitted.
    CLRDATA_ADDRESS AddressOfNativeCodeSlot = 0;

    CLRDATA_ADDRESS MethodDescPtr = 0;
    CLRDATA_ADDRESS MethodTablePtr = 0;
    CLRDATA_ADDRESS ModulePtr = 0;

    mdToken                  MDToken = 0;
    CLRDATA_ADDRESS GCInfo = 0;
    CLRDATA_ADDRESS GCStressCodeCopy = 0;

    // This is only valid if bIsDynamic is true
    CLRDATA_ADDRESS managedDynamicMethodObject = 0;

    CLRDATA_ADDRESS requestedIP = 0;

    // Gives info for the single currently active version of a method
    DacpReJitData       rejitDataCurrent = {};

    // Gives info corresponding to requestedIP (for !ip2md)
    DacpReJitData       rejitDataRequested = {};

    // Total number of rejit versions that have been jitted
    ULONG               cJittedRejitVersions = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodDescData(
            addr,
            NULL,   // IP address
            this,
            0,      // cRejitData
            NULL,   // rejitData[]
            NULL    // pcNeededRejitData
            );
    }
};


struct MSLAYOUT DacpMethodDescTransparencyData
{
    BOOL            bHasCriticalTransparentInfo = FALSE;
    BOOL            bIsCritical = FALSE;
    BOOL            bIsTreatAsSafe = FALSE;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodDescTransparencyData(addr, this);
    }
};

struct MSLAYOUT DacpTieredVersionData
{
    enum OptimizationTier
    {
        OptimizationTier_Unknown,
        OptimizationTier_MinOptJitted,
        OptimizationTier_Optimized,
        OptimizationTier_QuickJitted,
        OptimizationTier_OptimizedTier1,
        OptimizationTier_ReadyToRun,
        OptimizationTier_OptimizedTier1OSR,
        OptimizationTier_QuickJittedInstrumented,
        OptimizationTier_OptimizedTier1Instrumented,
    };

    CLRDATA_ADDRESS NativeCodeAddr;
    OptimizationTier OptimizationTier;
    CLRDATA_ADDRESS NativeCodeVersionNodePtr;
};

// for JITType
enum JITTypes {TYPE_UNKNOWN=0,TYPE_JIT,TYPE_PJIT};

struct MSLAYOUT DacpCodeHeaderData
{
    CLRDATA_ADDRESS GCInfo = 0;
    JITTypes                   JITType = JITTypes::TYPE_UNKNOWN;
    CLRDATA_ADDRESS MethodDescPtr = 0;
    CLRDATA_ADDRESS MethodStart = 0;
    DWORD                    MethodSize = 0;
    CLRDATA_ADDRESS ColdRegionStart = 0;
    DWORD           ColdRegionSize = 0;
    DWORD           HotRegionSize = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS IPAddr)
    {
        return sos->GetCodeHeaderData(IPAddr, this);
    }
};

struct MSLAYOUT DacpWorkRequestData
{
    CLRDATA_ADDRESS Function = 0;
    CLRDATA_ADDRESS Context = 0;
    CLRDATA_ADDRESS NextWorkRequest = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetWorkRequestData(addr, this);
    }
};

struct MSLAYOUT DacpHillClimbingLogEntry
{
    DWORD TickCount = 0;
    int Transition = 0;
    int NewControlSetting = 0;
    int LastHistoryCount = 0;
    double LastHistoryMean = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS entry)
    {
        return sos->GetHillClimbingLogEntry(entry, this);
    }
};


// Used for CLR versions >= 4.0
struct MSLAYOUT DacpThreadpoolData
{
    LONG cpuUtilization = 0;
    int NumIdleWorkerThreads = 0;
    int NumWorkingWorkerThreads = 0;
    int NumRetiredWorkerThreads = 0;
    LONG MinLimitTotalWorkerThreads = 0;
    LONG MaxLimitTotalWorkerThreads = 0;

    CLRDATA_ADDRESS FirstUnmanagedWorkRequest = 0;

    CLRDATA_ADDRESS HillClimbingLog = 0;
    int HillClimbingLogFirstIndex = 0;
    int HillClimbingLogSize = 0;

    DWORD NumTimers = 0;
    // TODO: Add support to enumerate timers too.

    LONG   NumCPThreads = 0;
    LONG   NumFreeCPThreads = 0;
    LONG   MaxFreeCPThreads = 0;
    LONG   NumRetiredCPThreads = 0;
    LONG   MaxLimitTotalCPThreads = 0;
    LONG   CurrentLimitTotalCPThreads = 0;
    LONG   MinLimitTotalCPThreads = 0;

    CLRDATA_ADDRESS AsyncTimerCallbackCompletionFPtr = 0;

    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetThreadpoolData(this);
    }
};

struct MSLAYOUT DacpGenerationData
{
    CLRDATA_ADDRESS start_segment = 0;
    CLRDATA_ADDRESS allocation_start = 0;

    // These are examined only for generation 0, otherwise NULL
    CLRDATA_ADDRESS allocContextPtr = 0;
    CLRDATA_ADDRESS allocContextLimit = 0;
};

#define DAC_NUMBERGENERATIONS 4


struct MSLAYOUT DacpAllocData
{
    CLRDATA_ADDRESS allocBytes = 0;
    CLRDATA_ADDRESS allocBytesLoh = 0;
};

struct MSLAYOUT DacpGenerationAllocData
{
    DacpAllocData allocData[DAC_NUMBERGENERATIONS] = {};
};

struct MSLAYOUT DacpGcHeapDetails
{
    CLRDATA_ADDRESS heapAddr = 0; // Only filled in server mode, otherwise NULL
    CLRDATA_ADDRESS alloc_allocated = 0;

    CLRDATA_ADDRESS mark_array = 0;
    CLRDATA_ADDRESS current_c_gc_state = 0;
    CLRDATA_ADDRESS next_sweep_obj = 0;
    CLRDATA_ADDRESS saved_sweep_ephemeral_seg = 0;
    CLRDATA_ADDRESS saved_sweep_ephemeral_start = 0;
    CLRDATA_ADDRESS background_saved_lowest_address = 0;
    CLRDATA_ADDRESS background_saved_highest_address = 0;

    DacpGenerationData generation_table [DAC_NUMBERGENERATIONS] = {};
    CLRDATA_ADDRESS ephemeral_heap_segment = 0;
    CLRDATA_ADDRESS finalization_fill_pointers [DAC_NUMBERGENERATIONS + 3] = {};
    CLRDATA_ADDRESS lowest_address = 0;
    CLRDATA_ADDRESS highest_address = 0;
    CLRDATA_ADDRESS card_table = 0;

    // Use this for workstation mode (DacpGcHeapDat.bServerMode==FALSE).
    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetGCHeapStaticData(this);
    }

    // Use this for Server mode, as there are multiple heaps,
    // and you need to pass a heap address in addr.
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetGCHeapDetails(addr, this);
    }
};

struct MSLAYOUT DacpGcHeapData
{
    BOOL bServerMode = FALSE;
    BOOL bGcStructuresValid = FALSE;
    UINT HeapCount = 0;
    UINT g_max_generation = 0;

    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetGCHeapData(this);
    }
};

struct MSLAYOUT DacpHeapSegmentData
{
    CLRDATA_ADDRESS segmentAddr = 0;
    CLRDATA_ADDRESS allocated = 0;
    CLRDATA_ADDRESS committed = 0;
    CLRDATA_ADDRESS reserved = 0;
    CLRDATA_ADDRESS used = 0;
    CLRDATA_ADDRESS mem = 0;
    // pass this to request if non-null to get the next segments.
    CLRDATA_ADDRESS next = 0;
    CLRDATA_ADDRESS gc_heap = 0; // only filled in server mode, otherwise NULL
    // computed field: if this is the ephemeral segment highMark includes the ephemeral generation
    CLRDATA_ADDRESS highAllocMark = 0;

    size_t flags = 0;
    CLRDATA_ADDRESS background_allocated = 0;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr, const DacpGcHeapDetails& heap)
    {
        // clear this here to make sure we don't get stale values
        this->highAllocMark = 0;

        HRESULT hr = sos->GetHeapSegmentData(addr, this);

        // if this is the start segment, and the Dac hasn't set highAllocMark, set it here.
        if (SUCCEEDED(hr) && this->highAllocMark == 0)
        {
            if (this->segmentAddr == heap.ephemeral_heap_segment)
                highAllocMark = heap.alloc_allocated;
            else
                highAllocMark = allocated;
        }
        return hr;
    }
};

struct MSLAYOUT DacpOomData
{
    int reason = 0;
    ULONG64 alloc_size = 0;
    ULONG64 available_pagefile_mb = 0;
    ULONG64 gc_index = 0;
    int fgm = 0;
    ULONG64 size = 0;
    BOOL loh_p = FALSE;

    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetOOMStaticData(this);
    }

    // Use this for Server mode, as there are multiple heaps,
    // and you need to pass a heap address in addr.
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetOOMData(addr, this);
    }
};

#define   DAC_NUM_GC_DATA_POINTS 9
#define   DAC_MAX_COMPACT_REASONS_COUNT 11
#define   DAC_MAX_EXPAND_MECHANISMS_COUNT 6
#define   DAC_MAX_GC_MECHANISM_BITS_COUNT 2
#define   DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT 6
struct MSLAYOUT DacpGCInterestingInfoData
{
    size_t interestingDataPoints[DAC_NUM_GC_DATA_POINTS] = {};
    size_t compactReasons[DAC_MAX_COMPACT_REASONS_COUNT] = {};
    size_t expandMechanisms[DAC_MAX_EXPAND_MECHANISMS_COUNT] = {};
    size_t bitMechanisms[DAC_MAX_GC_MECHANISM_BITS_COUNT] = {};
    size_t globalMechanisms[DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT] = {};

    HRESULT RequestGlobal(ISOSDacInterface *sos)
    {
        HRESULT hr;
        ISOSDacInterface3 *psos3 = NULL;
        if (SUCCEEDED(hr = sos->QueryInterface(__uuidof(ISOSDacInterface3), (void**) &psos3)))
        {
            hr = psos3->GetGCGlobalMechanisms(globalMechanisms);
            psos3->Release();
        }
        return hr;
    }

    HRESULT Request(ISOSDacInterface *sos)
    {
        HRESULT hr;
        ISOSDacInterface3 *psos3 = NULL;
        if (SUCCEEDED(hr = sos->QueryInterface(__uuidof(ISOSDacInterface3), (void**) &psos3)))
        {
            hr = psos3->GetGCInterestingInfoStaticData(this);
            psos3->Release();
        }
        return hr;
    }

    // Use this for Server mode, as there are multiple heaps,
    // and you need to pass a heap address in addr.
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        HRESULT hr;
        ISOSDacInterface3 *psos3 = NULL;
        if (SUCCEEDED(hr = sos->QueryInterface(__uuidof(ISOSDacInterface3), (void**) &psos3)))
        {
            hr = psos3->GetGCInterestingInfoData(addr, this);
            psos3->Release();
        }
        return hr;
    }
};

struct MSLAYOUT DacpGcHeapAnalyzeData
{
    CLRDATA_ADDRESS heapAddr = 0; // Only filled in server mode, otherwise NULL

    CLRDATA_ADDRESS internal_root_array = 0;
    ULONG64         internal_root_array_index = 0;
    BOOL            heap_analyze_success = FALSE;

    // Use this for workstation mode (DacpGcHeapDat.bServerMode==FALSE).
    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetHeapAnalyzeStaticData(this);
    }

    // Use this for Server mode, as there are multiple heaps,
    // and you need to pass a heap address in addr.
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetHeapAnalyzeData(addr, this);
    }
};


#define SYNCBLOCKDATA_COMFLAGS_CCW 1
#define SYNCBLOCKDATA_COMFLAGS_RCW 2
#define SYNCBLOCKDATA_COMFLAGS_CF 4

struct MSLAYOUT DacpSyncBlockData
{
    CLRDATA_ADDRESS Object = 0;
    BOOL            bFree = FALSE; // if set, no other fields are useful

    // fields below provide data from this, so it's just for display
    CLRDATA_ADDRESS SyncBlockPointer = 0;
    DWORD           COMFlags = 0;
    UINT            MonitorHeld = 0;
    UINT            Recursion = 0;
    CLRDATA_ADDRESS HoldingThread = 0;
    UINT            AdditionalThreadCount = 0;
    CLRDATA_ADDRESS appDomainPtr = 0;

    // SyncBlockCount will always be filled in with the number of SyncBlocks.
    // SyncBlocks may be requested from [1,SyncBlockCount]
    UINT            SyncBlockCount = 0;

    // SyncBlockNumber must be from [1,SyncBlockCount]
    // If there are no SyncBlocks, a call to Request with SyncBlockCount = 1
    // will return E_FAIL.
    HRESULT Request(ISOSDacInterface *sos, UINT SyncBlockNumber)
    {
        return sos->GetSyncBlockData(SyncBlockNumber, this);
    }
};

struct MSLAYOUT DacpSyncBlockCleanupData
{
    CLRDATA_ADDRESS SyncBlockPointer = 0;

    CLRDATA_ADDRESS nextSyncBlock = 0;
    CLRDATA_ADDRESS blockRCW = 0;
    CLRDATA_ADDRESS blockClassFactory = 0;
    CLRDATA_ADDRESS blockCCW = 0;

    // Pass NULL on the first request to start a traversal.
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS psyncBlock)
    {
        return sos->GetSyncBlockCleanupData(psyncBlock, this);
    }
};

///////////////////////////////////////////////////////////////////////////

enum EHClauseType {EHFault, EHFinally, EHFilter, EHTyped, EHUnknown};

struct MSLAYOUT DACEHInfo
{
    EHClauseType clauseType = EHClauseType::EHFault;
    CLRDATA_ADDRESS tryStartOffset = 0;
    CLRDATA_ADDRESS tryEndOffset = 0;
    CLRDATA_ADDRESS handlerStartOffset = 0;
    CLRDATA_ADDRESS handlerEndOffset = 0;
    BOOL isDuplicateClause = FALSE;
    CLRDATA_ADDRESS filterOffset = 0;   // valid when clauseType is EHFilter
    BOOL isCatchAllHandler = FALSE;     // valid when clauseType is EHTyped
    CLRDATA_ADDRESS moduleAddr = 0;     // when == 0 mtCatch contains a MethodTable, when != 0 tokCatch contains a type token
    CLRDATA_ADDRESS mtCatch = 0;        // the method table of the TYPED clause type
    mdToken tokCatch = 0;               // the type token of the TYPED clause type
};

struct MSLAYOUT DacpGetModuleAddress
{
    CLRDATA_ADDRESS ModulePtr = 0;
    HRESULT Request(IXCLRDataModule* pDataModule)
    {
        return pDataModule->Request(DACDATAMODULEPRIV_REQUEST_GET_MODULEPTR, 0, NULL, sizeof(*this), (PBYTE) this);
    }
};

struct MSLAYOUT DacpGetModuleData
{
    BOOL IsDynamic = FALSE;
    BOOL IsInMemory = FALSE;
    BOOL IsFileLayout = FALSE;
    CLRDATA_ADDRESS PEAssembly = 0;
    CLRDATA_ADDRESS LoadedPEAddress = 0;
    ULONG64 LoadedPESize = 0;
    CLRDATA_ADDRESS InMemoryPdbAddress = 0;
    ULONG64 InMemoryPdbSize = 0;

    HRESULT Request(IXCLRDataModule* pDataModule)
    {
        return pDataModule->Request(DACDATAMODULEPRIV_REQUEST_GET_MODULEDATA, 0, NULL, sizeof(*this), (PBYTE) this);
    }
};

struct MSLAYOUT DacpFrameData
{
    CLRDATA_ADDRESS frameAddr = 0;

    // Could also be implemented for IXCLRDataFrame if desired.
    HRESULT Request(IXCLRDataStackWalk* dac)
    {
        return dac->Request(DACSTACKPRIV_REQUEST_FRAME_DATA,
                            0, NULL,
                            sizeof(*this), (PBYTE)this);
    }
};

struct MSLAYOUT DacpJitManagerInfo
{
    CLRDATA_ADDRESS managerAddr = 0;
    DWORD codeType = 0; // for union below
    CLRDATA_ADDRESS ptrHeapList = 0;    // A HeapList * if IsMiIL(codeType)
};

enum CodeHeapType {CODEHEAP_LOADER=0,CODEHEAP_HOST,CODEHEAP_UNKNOWN};

struct MSLAYOUT DacpJitCodeHeapInfo
{
    DWORD codeHeapType = 0; // for union below

    union
    {
        CLRDATA_ADDRESS LoaderHeap = 0;    // if CODEHEAP_LOADER
        struct MSLAYOUT
        {
            CLRDATA_ADDRESS baseAddr = 0; // if CODEHEAP_HOST
            CLRDATA_ADDRESS currentAddr = 0;
        } HostData;
    };

    DacpJitCodeHeapInfo() : codeHeapType(0), LoaderHeap(0) {}
};

#include "static_assert.h"

/* DAC datastructures are frozen as of dev11 shipping.  Do NOT add fields, remove fields, or change the fields of
 * these structs in any way.  The correct way to get new data out of the runtime is to create a new struct and
 * add a new function to the latest Dac<-->SOS interface to produce this data.
 */
static_assert(sizeof(DacpAllocData) == 0x10, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpGenerationAllocData) == 0x40, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpSyncBlockCleanupData) == 0x28, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpThreadStoreData) == 0x38, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpAppDomainStoreData) == 0x18, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpAppDomainData) == 0x48, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpAssemblyData) == 0x40, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpThreadData) == 0x68, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpMethodDescData) == 0x98, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpCodeHeaderData) == 0x38, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpThreadpoolData) == 0x58, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpObjectData) == 0x60, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpMethodTableData) == 0x48, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpWorkRequestData) == 0x18, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpFieldDescData) == 0x40, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpModuleData) == 0xa0, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpGcHeapData) == 0x10, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpJitManagerInfo) == 0x18, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpHeapSegmentData) == 0x58, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpDomainLocalModuleData) == 0x30, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpUsefulGlobalsData) == 0x28, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DACEHInfo) == 0x58, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpRCWData) == 0x58, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpCCWData) == 0x48, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpMethodTableFieldData) == 0x18, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpMethodTableTransparencyData) == 0xc, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpThreadLocalModuleData) == 0x30, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpCOMInterfacePointerData) == 0x18, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpMethodDescTransparencyData) == 0xc, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpHillClimbingLogEntry) == 0x18, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpGenerationData) == 0x20, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpGcHeapDetails) == 0x120, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpOomData) == 0x38, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpGcHeapAnalyzeData) == 0x20, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpSyncBlockData) == 0x48, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpGetModuleAddress) == 0x8, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpFrameData) == 0x8, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpJitCodeHeapInfo) == 0x18, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpExceptionObjectData) == 0x38, "Dacp structs cannot be modified due to backwards compatibility.");
static_assert(sizeof(DacpMethodTableCollectibleData) == 0x10, "Dacp structs cannot be modified due to backwards compatibility.");

#endif  // _DACPRIVATE_H_
