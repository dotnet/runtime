// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#ifndef FEATURE_PAL
// It is unfortunate having to include this header just to get the definition of GenericModeBlock
#include <msodw.h>
#endif // FEATURE_PAL

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
#ifdef __GNUC__
#define MSLAYOUT __attribute__((__ms_struct__))
#else
#define MSLAYOUT
#endif

//----------------------------------------------------------------------------
//
// Utility class to allow for zero initialization of our Dacp- structs.
//
//----------------------------------------------------------------------------

template <class T>
struct ZeroInit
{
    ZeroInit()
    { memset(static_cast<T*>(this), 0, sizeof(T)); }
};

    
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
struct MSLAYOUT DacpObjectData : ZeroInit<DacpObjectData>
{
    CLRDATA_ADDRESS MethodTable;
    DacpObjectType ObjectType;
    ULONG64 Size;
    CLRDATA_ADDRESS ElementTypeHandle;
    CorElementType ElementType;
    DWORD dwRank;
    ULONG64 dwNumComponents;
    ULONG64 dwComponentSize;
    CLRDATA_ADDRESS ArrayDataPtr;
    CLRDATA_ADDRESS ArrayBoundsPtr;
    CLRDATA_ADDRESS ArrayLowerBoundsPtr;

    CLRDATA_ADDRESS RCW;
    CLRDATA_ADDRESS CCW;
    
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetObjectData(addr, this);
    }
};

struct MSLAYOUT DacpExceptionObjectData : ZeroInit<DacpExceptionObjectData>
{
    CLRDATA_ADDRESS   Message;
    CLRDATA_ADDRESS   InnerException;
    CLRDATA_ADDRESS   StackTrace;
    CLRDATA_ADDRESS   WatsonBuckets;
    CLRDATA_ADDRESS   StackTraceString;
    CLRDATA_ADDRESS   RemoteStackTraceString;
    INT32             HResult;
    INT32             XCode;

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

struct MSLAYOUT DacpUsefulGlobalsData : ZeroInit<DacpUsefulGlobalsData>
{
    CLRDATA_ADDRESS ArrayMethodTable;
    CLRDATA_ADDRESS StringMethodTable;
    CLRDATA_ADDRESS ObjectMethodTable;
    CLRDATA_ADDRESS ExceptionMethodTable;
    CLRDATA_ADDRESS FreeMethodTable;
};

struct MSLAYOUT DacpFieldDescData : ZeroInit<DacpFieldDescData>
{
    CorElementType Type;
    CorElementType sigType;     // ELEMENT_TYPE_XXX from signature. We need this to disply pretty name for String in minidump's case
    CLRDATA_ADDRESS MTOfType; // NULL if Type is not loaded
    
    CLRDATA_ADDRESS ModuleOfType;
    mdTypeDef TokenOfType;
    
    mdFieldDef mb;
    CLRDATA_ADDRESS MTOfEnclosingClass;
    DWORD dwOffset;
    BOOL bIsThreadLocal;
    BOOL bIsContextLocal;
    BOOL bIsStatic;
    CLRDATA_ADDRESS NextField;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetFieldDescData(addr, this);
    }
};

struct MSLAYOUT DacpMethodTableFieldData : ZeroInit<DacpMethodTableFieldData>
{
    WORD wNumInstanceFields;
    WORD wNumStaticFields;
    WORD wNumThreadStaticFields;

    CLRDATA_ADDRESS FirstField; // If non-null, you can retrieve more
    
    WORD wContextStaticOffset;
    WORD wContextStaticsSize;
    
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodTableFieldData(addr, this);
    }
};

struct MSLAYOUT DacpMethodTableTransparencyData : ZeroInit<DacpMethodTableTransparencyData>
{
    BOOL bHasCriticalTransparentInfo;
    BOOL bIsCritical;
    BOOL bIsTreatAsSafe;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodTableTransparencyData(addr, this);
    }
};

struct MSLAYOUT DacpDomainLocalModuleData : ZeroInit<DacpDomainLocalModuleData>
{
    // These two parameters are used as input params when calling the
    // no-argument form of Request below.
    CLRDATA_ADDRESS appDomainAddr;
    ULONG64  ModuleID;
    
    CLRDATA_ADDRESS pClassData;   
    CLRDATA_ADDRESS pDynamicClassTable;   
    CLRDATA_ADDRESS pGCStaticDataStart;
    CLRDATA_ADDRESS pNonGCStaticDataStart; 

    // Called when you have a pointer to the DomainLocalModule
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetDomainLocalModuleData(addr, this);
    }
};


struct MSLAYOUT DacpThreadLocalModuleData : ZeroInit<DacpThreadLocalModuleData>
{
    // These two parameters are used as input params when calling the
    // no-argument form of Request below.
    CLRDATA_ADDRESS threadAddr;
    ULONG64 ModuleIndex;
    
    CLRDATA_ADDRESS pClassData;   
    CLRDATA_ADDRESS pDynamicClassTable;   
    CLRDATA_ADDRESS pGCStaticDataStart;
    CLRDATA_ADDRESS pNonGCStaticDataStart;
};


struct MSLAYOUT DacpModuleData : ZeroInit<DacpModuleData>
{
    CLRDATA_ADDRESS Address;
    CLRDATA_ADDRESS File; // A PEFile addr
    CLRDATA_ADDRESS  ilBase;
    CLRDATA_ADDRESS metadataStart;
    ULONG64 metadataSize;
    CLRDATA_ADDRESS Assembly; // Assembly pointer
    BOOL bIsReflection;
    BOOL bIsPEFile;
    ULONG64 dwBaseClassIndex;
    ULONG64 dwModuleID;

    DWORD dwTransientFlags;
    
    CLRDATA_ADDRESS TypeDefToMethodTableMap;
    CLRDATA_ADDRESS TypeRefToMethodTableMap;
    CLRDATA_ADDRESS MethodDefToDescMap;
    CLRDATA_ADDRESS FieldDefToDescMap;
    CLRDATA_ADDRESS MemberRefToDescMap;
    CLRDATA_ADDRESS FileReferencesMap;
    CLRDATA_ADDRESS ManifestModuleReferencesMap;

    CLRDATA_ADDRESS pLookupTableHeap;
    CLRDATA_ADDRESS pThunkHeap;

    ULONG64 dwModuleIndex;

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

struct MSLAYOUT DacpMethodTableData : ZeroInit<DacpMethodTableData>
{
    BOOL bIsFree; // everything else is NULL if this is true.
    CLRDATA_ADDRESS Module;
    CLRDATA_ADDRESS Class;
    CLRDATA_ADDRESS ParentMethodTable;
    WORD wNumInterfaces;
    WORD wNumMethods;
    WORD wNumVtableSlots;
    WORD wNumVirtuals;
    DWORD BaseSize;
    DWORD ComponentSize;
    mdTypeDef cl; // Metadata token    
    DWORD dwAttrClass; // cached metadata
    BOOL bIsShared; // flags & enum_flag_DomainNeutral
    BOOL bIsDynamic;
    BOOL bContainsPointers;

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

struct MSLAYOUT DacpThreadStoreData : ZeroInit<DacpThreadStoreData>
{
    LONG threadCount;
    LONG unstartedThreadCount;
    LONG backgroundThreadCount;
    LONG pendingThreadCount;
    LONG deadThreadCount;
    CLRDATA_ADDRESS firstThread;
    CLRDATA_ADDRESS finalizerThread;
    CLRDATA_ADDRESS gcThread;
    DWORD fHostConfig;          // Uses hosting flags defined above

    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetThreadStoreData(this);
    }
};

struct MSLAYOUT DacpAppDomainStoreData : ZeroInit<DacpAppDomainStoreData>
{
    CLRDATA_ADDRESS sharedDomain;
    CLRDATA_ADDRESS systemDomain;
    LONG DomainCount;

    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetAppDomainStoreData(this);
    }
};

struct MSLAYOUT DacpCOMInterfacePointerData : ZeroInit<DacpCOMInterfacePointerData>
{
    CLRDATA_ADDRESS methodTable;
    CLRDATA_ADDRESS interfacePtr;
    CLRDATA_ADDRESS comContext;
};

struct MSLAYOUT DacpRCWData : ZeroInit<DacpRCWData>
{
    CLRDATA_ADDRESS identityPointer;
    CLRDATA_ADDRESS unknownPointer;
    CLRDATA_ADDRESS managedObject;
    CLRDATA_ADDRESS jupiterObject;
    CLRDATA_ADDRESS vtablePtr;
    CLRDATA_ADDRESS creatorThread;
    CLRDATA_ADDRESS ctxCookie;
    
    LONG refCount;
    LONG interfaceCount;

    BOOL isJupiterObject;
    BOOL supportsIInspectable;
    BOOL isAggregated;
    BOOL isContained;
    BOOL isFreeThreaded;
    BOOL isDisconnected;
    
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

struct MSLAYOUT DacpCCWData : ZeroInit<DacpCCWData>
{
    CLRDATA_ADDRESS outerIUnknown;
    CLRDATA_ADDRESS managedObject;
    CLRDATA_ADDRESS handle;
    CLRDATA_ADDRESS ccwAddress;

    LONG refCount;
    LONG interfaceCount;
    BOOL isNeutered;

    LONG jupiterRefCount;
    BOOL isPegged;
    BOOL isGlobalPegged;
    BOOL hasStrongRef;
    BOOL isExtendsCOMObject;
    BOOL isAggregated;
    
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
struct MSLAYOUT DacpAppDomainData : ZeroInit<DacpAppDomainData>
{
    // The pointer to the BaseDomain (not necessarily an AppDomain).  
    // It's useful to keep this around in the structure
    CLRDATA_ADDRESS AppDomainPtr; 
    CLRDATA_ADDRESS AppSecDesc;
    CLRDATA_ADDRESS pLowFrequencyHeap;
    CLRDATA_ADDRESS pHighFrequencyHeap;
    CLRDATA_ADDRESS pStubHeap;
    CLRDATA_ADDRESS DomainLocalBlock;
    CLRDATA_ADDRESS pDomainLocalModules;    
    // The creation sequence number of this app domain (starting from 1)
    DWORD dwId;
    LONG AssemblyCount;
    LONG FailedAssemblyCount;
    DacpAppDomainDataStage appDomainStage; 
    
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetAppDomainData(addr, this);
    }
};

struct MSLAYOUT DacpAssemblyData : ZeroInit<DacpAssemblyData>
{
    CLRDATA_ADDRESS AssemblyPtr; //useful to have
    CLRDATA_ADDRESS ClassLoader;
    CLRDATA_ADDRESS ParentDomain;
    CLRDATA_ADDRESS BaseDomainPtr;
    CLRDATA_ADDRESS AssemblySecDesc;
    BOOL isDynamic;
    UINT ModuleCount;
    UINT LoadContext;
    BOOL isDomainNeutral;
    DWORD dwLocationFlags;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr, CLRDATA_ADDRESS baseDomainPtr)
    {
        return sos->GetAssemblyData(baseDomainPtr, addr, this);
    }

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return Request(sos, addr, NULL);
    }
};


struct MSLAYOUT DacpThreadData : ZeroInit<DacpThreadData>
{
    DWORD corThreadId;
    DWORD osThreadId;
    int state;
    ULONG preemptiveGCDisabled;
    CLRDATA_ADDRESS allocContextPtr;
    CLRDATA_ADDRESS allocContextLimit;
    CLRDATA_ADDRESS context;
    CLRDATA_ADDRESS domain;
    CLRDATA_ADDRESS pFrame;
    DWORD lockCount;
    CLRDATA_ADDRESS firstNestedException; // Pass this pointer to DacpNestedExceptionInfo
    CLRDATA_ADDRESS teb;
    CLRDATA_ADDRESS fiberData;
    CLRDATA_ADDRESS lastThrownObjectHandle;
    CLRDATA_ADDRESS nextThread;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetThreadData(addr, this);
    }
};


struct MSLAYOUT DacpReJitData : ZeroInit<DacpReJitData>
{
    enum Flags
    {
        kUnknown,
        kRequested,
        kActive,
        kReverted,
    };

    CLRDATA_ADDRESS                 rejitID;
    Flags                           flags;
    CLRDATA_ADDRESS                 NativeCodeAddr;
};


struct MSLAYOUT DacpMethodDescData : ZeroInit<DacpMethodDescData>
{
    BOOL            bHasNativeCode;
    BOOL            bIsDynamic;
    WORD            wSlotNumber;
    CLRDATA_ADDRESS NativeCodeAddr;
    // Useful for breaking when a method is jitted.
    CLRDATA_ADDRESS AddressOfNativeCodeSlot;
    
    CLRDATA_ADDRESS MethodDescPtr;
    CLRDATA_ADDRESS MethodTablePtr;
    CLRDATA_ADDRESS ModulePtr;
    
    mdToken                  MDToken;
    CLRDATA_ADDRESS GCInfo;
    CLRDATA_ADDRESS GCStressCodeCopy;

    // This is only valid if bIsDynamic is true
    CLRDATA_ADDRESS managedDynamicMethodObject;

    CLRDATA_ADDRESS requestedIP;

    // Gives info for the single currently active version of a method
    DacpReJitData       rejitDataCurrent;

    // Gives info corresponding to requestedIP (for !ip2md)
    DacpReJitData       rejitDataRequested;
    
    // Total number of rejit versions that have been jitted
    ULONG               cJittedRejitVersions;

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


struct MSLAYOUT DacpMethodDescTransparencyData : ZeroInit<DacpMethodDescTransparencyData>
{
    BOOL            bHasCriticalTransparentInfo;
    BOOL            bIsCritical;
    BOOL            bIsTreatAsSafe;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetMethodDescTransparencyData(addr, this);
    }
};

struct MSLAYOUT DacpTieredVersionData
{
    enum TieredState 
    {
        NON_TIERED,
        TIERED_0,
        TIERED_1,
        TIERED_UNKNOWN
    };
    
    CLRDATA_ADDRESS NativeCodeAddr;
    TieredState     TieredInfo;
    CLRDATA_ADDRESS NativeCodeVersionNodePtr;
};

// for JITType
enum JITTypes {TYPE_UNKNOWN=0,TYPE_JIT,TYPE_PJIT};

struct MSLAYOUT DacpCodeHeaderData : ZeroInit<DacpCodeHeaderData>
{        
    CLRDATA_ADDRESS GCInfo;
    JITTypes                   JITType;
    CLRDATA_ADDRESS MethodDescPtr;
    CLRDATA_ADDRESS MethodStart;
    DWORD                    MethodSize;
    CLRDATA_ADDRESS ColdRegionStart;
    DWORD           ColdRegionSize;
    DWORD           HotRegionSize;
    
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS IPAddr)
    {
        return sos->GetCodeHeaderData(IPAddr, this);
    }
};

struct MSLAYOUT DacpWorkRequestData : ZeroInit<DacpWorkRequestData>
{
    CLRDATA_ADDRESS Function;
    CLRDATA_ADDRESS Context;
    CLRDATA_ADDRESS NextWorkRequest;
        
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr)
    {
        return sos->GetWorkRequestData(addr, this);
    }
};

struct MSLAYOUT DacpHillClimbingLogEntry : ZeroInit<DacpHillClimbingLogEntry>
{
    DWORD TickCount;
    int Transition;
    int NewControlSetting;
    int LastHistoryCount;
    double LastHistoryMean;
    
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS entry)
    {
        return sos->GetHillClimbingLogEntry(entry, this);   
    }
};


// Used for CLR versions >= 4.0
struct MSLAYOUT DacpThreadpoolData : ZeroInit<DacpThreadpoolData>
{
    LONG cpuUtilization;    
    int NumIdleWorkerThreads;
    int NumWorkingWorkerThreads;
    int NumRetiredWorkerThreads;
    LONG MinLimitTotalWorkerThreads;
    LONG MaxLimitTotalWorkerThreads;

    CLRDATA_ADDRESS FirstUnmanagedWorkRequest;

    CLRDATA_ADDRESS HillClimbingLog;
    int HillClimbingLogFirstIndex;
    int HillClimbingLogSize;

    DWORD NumTimers;
    // TODO: Add support to enumerate timers too.

    LONG   NumCPThreads;
    LONG   NumFreeCPThreads;
    LONG   MaxFreeCPThreads; 
    LONG   NumRetiredCPThreads;
    LONG   MaxLimitTotalCPThreads;
    LONG   CurrentLimitTotalCPThreads;
    LONG   MinLimitTotalCPThreads;

    CLRDATA_ADDRESS AsyncTimerCallbackCompletionFPtr;
    
    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetThreadpoolData(this);
    }
};

struct MSLAYOUT DacpGenerationData : ZeroInit<DacpGenerationData>
{    
    CLRDATA_ADDRESS start_segment;
    CLRDATA_ADDRESS allocation_start;

    // These are examined only for generation 0, otherwise NULL
    CLRDATA_ADDRESS allocContextPtr;
    CLRDATA_ADDRESS allocContextLimit;
};

#define DAC_NUMBERGENERATIONS 4


struct MSLAYOUT DacpAllocData : ZeroInit<DacpAllocData>
{
    CLRDATA_ADDRESS allocBytes;
    CLRDATA_ADDRESS allocBytesLoh;
};

struct MSLAYOUT DacpGenerationAllocData : ZeroInit<DacpGenerationAllocData>
{
    DacpAllocData allocData[DAC_NUMBERGENERATIONS]; 
};

struct MSLAYOUT DacpGcHeapDetails : ZeroInit<DacpGcHeapDetails>
{
    CLRDATA_ADDRESS heapAddr; // Only filled in in server mode, otherwise NULL
    CLRDATA_ADDRESS alloc_allocated;

    CLRDATA_ADDRESS mark_array;
    CLRDATA_ADDRESS current_c_gc_state;
    CLRDATA_ADDRESS next_sweep_obj;
    CLRDATA_ADDRESS saved_sweep_ephemeral_seg;
    CLRDATA_ADDRESS saved_sweep_ephemeral_start;
    CLRDATA_ADDRESS background_saved_lowest_address;
    CLRDATA_ADDRESS background_saved_highest_address;

    DacpGenerationData generation_table [DAC_NUMBERGENERATIONS]; 
    CLRDATA_ADDRESS ephemeral_heap_segment;        
    CLRDATA_ADDRESS finalization_fill_pointers [DAC_NUMBERGENERATIONS + 3];
    CLRDATA_ADDRESS lowest_address;
    CLRDATA_ADDRESS highest_address;
    CLRDATA_ADDRESS card_table;

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
    : ZeroInit<DacpGcHeapData>
{
    BOOL bServerMode;
    BOOL bGcStructuresValid;
    UINT HeapCount;
    UINT g_max_generation;
    
    HRESULT Request(ISOSDacInterface *sos)
    {
        return sos->GetGCHeapData(this);
    }
};

struct MSLAYOUT DacpHeapSegmentData
    : ZeroInit<DacpHeapSegmentData>
{
    CLRDATA_ADDRESS segmentAddr;
    CLRDATA_ADDRESS allocated;
    CLRDATA_ADDRESS committed;
    CLRDATA_ADDRESS reserved;
    CLRDATA_ADDRESS used;
    CLRDATA_ADDRESS mem;
    // pass this to request if non-null to get the next segments.
    CLRDATA_ADDRESS next;
    CLRDATA_ADDRESS gc_heap; // only filled in in server mode, otherwise NULL
    // computed field: if this is the ephemeral segment highMark includes the ephemeral generation
    CLRDATA_ADDRESS highAllocMark;

    size_t flags;
    CLRDATA_ADDRESS background_allocated;

    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS addr, const DacpGcHeapDetails& heap)
    {
        HRESULT hr = sos->GetHeapSegmentData(addr, this);

        // if this is the start segment, set highAllocMark too.
        if (SUCCEEDED(hr))
        {
            // TODO:  This needs to be put on the Dac side.
            if (this->segmentAddr == heap.generation_table[0].start_segment)
                highAllocMark = heap.alloc_allocated;
            else
                highAllocMark = allocated;
        }    
        return hr;
    }
};

struct MSLAYOUT DacpOomData : ZeroInit<DacpOomData>
{
    int reason;
    ULONG64 alloc_size;
    ULONG64 available_pagefile_mb;
    ULONG64 gc_index;
    int fgm;
    ULONG64 size;
    BOOL loh_p;

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
struct MSLAYOUT DacpGCInterestingInfoData : ZeroInit<DacpGCInterestingInfoData>
{
    size_t interestingDataPoints[DAC_NUM_GC_DATA_POINTS];
    size_t compactReasons[DAC_MAX_COMPACT_REASONS_COUNT];
    size_t expandMechanisms[DAC_MAX_EXPAND_MECHANISMS_COUNT];
    size_t bitMechanisms[DAC_MAX_GC_MECHANISM_BITS_COUNT];
    size_t globalMechanisms[DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT];

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
    : ZeroInit<DacpGcHeapAnalyzeData>
{
    CLRDATA_ADDRESS heapAddr; // Only filled in in server mode, otherwise NULL

    CLRDATA_ADDRESS internal_root_array;
    ULONG64         internal_root_array_index;
    BOOL            heap_analyze_success;

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

struct MSLAYOUT DacpSyncBlockData : ZeroInit<DacpSyncBlockData>
{        
    CLRDATA_ADDRESS Object;
    BOOL            bFree; // if set, no other fields are useful
    
    // fields below provide data from this, so it's just for display
    CLRDATA_ADDRESS SyncBlockPointer;
    DWORD           COMFlags;
    UINT            MonitorHeld;
    UINT            Recursion;
    CLRDATA_ADDRESS HoldingThread;
    UINT            AdditionalThreadCount;
    CLRDATA_ADDRESS appDomainPtr;
    
    // SyncBlockCount will always be filled in with the number of SyncBlocks.
    // SyncBlocks may be requested from [1,SyncBlockCount]
    UINT            SyncBlockCount;

    // SyncBlockNumber must be from [1,SyncBlockCount]    
    // If there are no SyncBlocks, a call to Request with SyncBlockCount = 1
    // will return E_FAIL.
    HRESULT Request(ISOSDacInterface *sos, UINT SyncBlockNumber)
    {
        return sos->GetSyncBlockData(SyncBlockNumber, this);
    }
};

struct MSLAYOUT DacpSyncBlockCleanupData : ZeroInit<DacpSyncBlockCleanupData>
{
    CLRDATA_ADDRESS SyncBlockPointer;
    
    CLRDATA_ADDRESS nextSyncBlock;
    CLRDATA_ADDRESS blockRCW;
    CLRDATA_ADDRESS blockClassFactory;
    CLRDATA_ADDRESS blockCCW;
    
    // Pass NULL on the first request to start a traversal.
    HRESULT Request(ISOSDacInterface *sos, CLRDATA_ADDRESS psyncBlock)
    {
        return sos->GetSyncBlockCleanupData(psyncBlock, this);
    }
};

///////////////////////////////////////////////////////////////////////////

enum EHClauseType {EHFault, EHFinally, EHFilter, EHTyped, EHUnknown};
                
struct MSLAYOUT DACEHInfo : ZeroInit<DACEHInfo>
{
    EHClauseType clauseType;
    CLRDATA_ADDRESS tryStartOffset;
    CLRDATA_ADDRESS tryEndOffset;
    CLRDATA_ADDRESS handlerStartOffset;
    CLRDATA_ADDRESS handlerEndOffset;
    BOOL isDuplicateClause;
    CLRDATA_ADDRESS filterOffset;   // valid when clauseType is EHFilter
    BOOL isCatchAllHandler;             // valid when clauseType is EHTyped
    CLRDATA_ADDRESS moduleAddr;    // when == 0 mtCatch contains a MethodTable, when != 0 tokCatch contains a type token
    CLRDATA_ADDRESS mtCatch;   // the method table of the TYPED clause type
    mdToken tokCatch;          // the type token of the TYPED clause type
};

struct MSLAYOUT DacpGetModuleAddress : ZeroInit<DacpGetModuleAddress>
{
    CLRDATA_ADDRESS ModulePtr;
    HRESULT Request(IXCLRDataModule* pDataModule)
    {
        return pDataModule->Request(DACDATAMODULEPRIV_REQUEST_GET_MODULEPTR, 0, NULL, sizeof(*this), (PBYTE) this);
    }
};

struct MSLAYOUT DacpGetModuleData : ZeroInit<DacpGetModuleData>
{
    BOOL IsDynamic;
    BOOL IsInMemory;
    BOOL IsFileLayout;
    CLRDATA_ADDRESS PEFile;
    CLRDATA_ADDRESS LoadedPEAddress;
    ULONG64 LoadedPESize;
    CLRDATA_ADDRESS InMemoryPdbAddress;
    ULONG64 InMemoryPdbSize;

    HRESULT Request(IXCLRDataModule* pDataModule)
    {
        return pDataModule->Request(DACDATAMODULEPRIV_REQUEST_GET_MODULEDATA, 0, NULL, sizeof(*this), (PBYTE) this);
    }
};

struct MSLAYOUT DacpFrameData : ZeroInit<DacpFrameData>
{
    CLRDATA_ADDRESS frameAddr;

    // Could also be implemented for IXCLRDataFrame if desired.
    HRESULT Request(IXCLRDataStackWalk* dac)
    {
        return dac->Request(DACSTACKPRIV_REQUEST_FRAME_DATA,
                            0, NULL,
                            sizeof(*this), (PBYTE)this);
    }
};

struct MSLAYOUT DacpJitManagerInfo : ZeroInit<DacpJitManagerInfo>
{
    CLRDATA_ADDRESS managerAddr;
    DWORD codeType; // for union below
    CLRDATA_ADDRESS ptrHeapList;    // A HeapList * if IsMiIL(codeType)
};

enum CodeHeapType {CODEHEAP_LOADER=0,CODEHEAP_HOST,CODEHEAP_UNKNOWN};

struct MSLAYOUT DacpJitCodeHeapInfo : ZeroInit<DacpJitCodeHeapInfo>
{
    DWORD codeHeapType; // for union below

    union
    {
        CLRDATA_ADDRESS LoaderHeap;    // if CODEHEAP_LOADER
        struct MSLAYOUT
        {
            CLRDATA_ADDRESS baseAddr; // if CODEHEAP_HOST
            CLRDATA_ADDRESS currentAddr;
        } HostData;
    };
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

#endif  // _DACPRIVATE_H_
