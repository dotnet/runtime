// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The C++ GC/EE interface is a set of abstract classes, which the C# port sees as C++ vtables:
// a pointer to an array of function pointers, in declaration order, each taking the object as
// its first argument. Each struct below mirrors one such vtable, one field per virtual slot, in
// exactly the order the methods are declared in the C++ header. Adding, removing or reordering a
// method in the C++ header requires the same change here.
//
// C++ `bool` is mapped to `byte` because `bool` is not blittable in a `delegate* unmanaged`
// signature, and `Object*` is mapped to `byte*` because the GC never holds a managed reference.

namespace Internal.Runtime.GarbageCollection
{
    /// <summary>
    /// Virtual method table of gcinterface.h `IGCHandleStore`, in declaration order (6 slots).
    /// </summary>
    internal unsafe struct IGCHandleStoreVtable
    {
        /// <summary>Number of virtual slots this vtable describes.</summary>
        public const int SlotCount = 6;

        public delegate* unmanaged<void*, void> Uproot;
        public delegate* unmanaged<void*, OBJECTHANDLE, byte> ContainsHandle;
        public delegate* unmanaged<void*, byte*, HandleType, OBJECTHANDLE> CreateHandleOfType;
        public delegate* unmanaged<void*, byte*, HandleType, int, OBJECTHANDLE> CreateHandleOfType_2;
        public delegate* unmanaged<void*, byte*, HandleType, void*, OBJECTHANDLE> CreateHandleWithExtraInfo;
        public delegate* unmanaged<void*, byte*, byte*, OBJECTHANDLE> CreateDependentHandle;

        // IGCHandleStore also declares a virtual destructor, which the Itanium C++ ABI
        // places in two additional slots after the ones above. It is declared last, so the
        // slots that matter here are unaffected; any new method must be added before it.
    }

    /// <summary>
    /// Virtual method table of gcinterface.h `IGCHandleManager`, in declaration order (18 slots).
    /// </summary>
    internal unsafe struct IGCHandleManagerVtable
    {
        /// <summary>Number of virtual slots this vtable describes.</summary>
        public const int SlotCount = 18;

        public delegate* unmanaged<void*, byte> Initialize;
        public delegate* unmanaged<void*, void> Shutdown;
        public delegate* unmanaged<void*, void*> GetGlobalHandleStore;
        public delegate* unmanaged<void*, void*> CreateHandleStore;
        public delegate* unmanaged<void*, void*, void> DestroyHandleStore;
        public delegate* unmanaged<void*, byte*, HandleType, OBJECTHANDLE> CreateGlobalHandleOfType;
        public delegate* unmanaged<void*, OBJECTHANDLE, OBJECTHANDLE> CreateDuplicateHandle;
        public delegate* unmanaged<void*, OBJECTHANDLE, HandleType, void> DestroyHandleOfType;
        public delegate* unmanaged<void*, OBJECTHANDLE, void> DestroyHandleOfUnknownType;
        public delegate* unmanaged<void*, OBJECTHANDLE, HandleType, void*, void> SetExtraInfoForHandle;
        public delegate* unmanaged<void*, OBJECTHANDLE, void*> GetExtraInfoFromHandle;
        public delegate* unmanaged<void*, OBJECTHANDLE, byte*, void> StoreObjectInHandle;
        public delegate* unmanaged<void*, OBJECTHANDLE, byte*, byte> StoreObjectInHandleIfNull;
        public delegate* unmanaged<void*, OBJECTHANDLE, byte*, void> SetDependentHandleSecondary;
        public delegate* unmanaged<void*, OBJECTHANDLE, byte*> GetDependentHandleSecondary;
        public delegate* unmanaged<void*, OBJECTHANDLE, byte*, byte*, byte*> InterlockedCompareExchangeObjectInHandle;
        public delegate* unmanaged<void*, OBJECTHANDLE, HandleType> HandleFetchType;
        public delegate* unmanaged<void*, delegate* unmanaged<byte**, nuint*, nuint, nuint, void>, nuint, nuint, void> TraceRefCountedHandles;
    }

    /// <summary>
    /// Virtual method table of gcinterface.h `IGCHeap`, in declaration order (89 slots).
    /// </summary>
    internal unsafe struct IGCHeapVtable
    {
        /// <summary>Number of virtual slots this vtable describes.</summary>
        public const int SlotCount = 89;

        public delegate* unmanaged<void*, nuint, byte> IsValidSegmentSize;
        public delegate* unmanaged<void*, nuint, byte> IsValidGen0MaxSize;
        public delegate* unmanaged<void*, byte, nuint> GetValidSegmentSize;
        public delegate* unmanaged<void*, nuint, void> SetReservedVMLimit;
        public delegate* unmanaged<void*, void> WaitUntilConcurrentGCComplete;
        public delegate* unmanaged<void*, byte> IsConcurrentGCInProgress;
        public delegate* unmanaged<void*, void> TemporaryEnableConcurrentGC;
        public delegate* unmanaged<void*, void> TemporaryDisableConcurrentGC;
        public delegate* unmanaged<void*, byte> IsConcurrentGCEnabled;
        public delegate* unmanaged<void*, int, int> WaitUntilConcurrentGCCompleteAsync;
        public delegate* unmanaged<void*, nuint> GetNumberOfFinalizable;
        public delegate* unmanaged<void*, byte*> GetNextFinalizable;
        public delegate* unmanaged<void*, ulong*, ulong*, ulong*, ulong*, ulong*, ulong*, ulong*, ulong*, ulong*, ulong*, uint*, uint*, byte*, byte*, ulong*, ulong*, int, void> GetMemoryInfo;
        public delegate* unmanaged<void*, uint> GetMemoryLoad;
        public delegate* unmanaged<void*, int> GetGcLatencyMode;
        public delegate* unmanaged<void*, int, int> SetGcLatencyMode;
        public delegate* unmanaged<void*, int> GetLOHCompactionMode;
        public delegate* unmanaged<void*, int, void> SetLOHCompactionMode;
        public delegate* unmanaged<void*, uint, uint, byte> RegisterForFullGCNotification;
        public delegate* unmanaged<void*, byte> CancelFullGCNotification;
        public delegate* unmanaged<void*, int, int> WaitForFullGCApproach;
        public delegate* unmanaged<void*, int, int> WaitForFullGCComplete;
        public delegate* unmanaged<void*, byte*, uint> WhichGeneration;
        public delegate* unmanaged<void*, int, int, int> CollectionCount;
        public delegate* unmanaged<void*, ulong, byte, ulong, byte, int> StartNoGCRegion;
        public delegate* unmanaged<void*, int> EndNoGCRegion;
        public delegate* unmanaged<void*, nuint> GetTotalBytesInUse;
        public delegate* unmanaged<void*, ulong> GetTotalAllocatedBytes;
        public delegate* unmanaged<void*, int, byte, int, int> GarbageCollect;
        public delegate* unmanaged<void*, uint> GetMaxGeneration;
        public delegate* unmanaged<void*, byte*, void> SetFinalizationRun;
        public delegate* unmanaged<void*, int, byte*, byte> RegisterForFinalization;
        public delegate* unmanaged<void*, int> GetLastGCPercentTimeInGC;
        public delegate* unmanaged<void*, int, nuint> GetLastGCGenerationSize;
        public delegate* unmanaged<void*, int> Initialize;
        public delegate* unmanaged<void*, byte*, byte> IsPromoted;
        public delegate* unmanaged<void*, void*, byte, byte> IsHeapPointer;
        public delegate* unmanaged<void*, uint> GetCondemnedGeneration;
        public delegate* unmanaged<void*, byte, byte> IsGCInProgressHelper;
        public delegate* unmanaged<void*, uint> GetGcCount;
        public delegate* unmanaged<void*, gc_alloc_context*, int, byte> IsThreadUsingAllocationContextHeap;
        public delegate* unmanaged<void*, byte*, byte> IsEphemeral;
        public delegate* unmanaged<void*, byte, uint> WaitUntilGCComplete;
        public delegate* unmanaged<void*, gc_alloc_context*, void*, void*, void> FixAllocContext;
        public delegate* unmanaged<void*, nuint> GetCurrentObjSize;
        public delegate* unmanaged<void*, byte, void> SetGCInProgress;
        public delegate* unmanaged<void*, byte> RuntimeStructuresValid;
        public delegate* unmanaged<void*, byte, void> SetSuspensionPending;
        public delegate* unmanaged<void*, float, void> SetYieldProcessorScalingFactor;
        public delegate* unmanaged<void*, void> Shutdown;
        public delegate* unmanaged<void*, int, nuint> GetLastGCStartTime;
        public delegate* unmanaged<void*, int, nuint> GetLastGCDuration;
        public delegate* unmanaged<void*, nuint> GetNow;
        public delegate* unmanaged<void*, gc_alloc_context*, nuint, uint, byte*> Alloc;
        public delegate* unmanaged<void*, byte*, void> PublishObject;
        public delegate* unmanaged<void*, void> SetWaitForGCEvent;
        public delegate* unmanaged<void*, void> ResetWaitForGCEvent;
        public delegate* unmanaged<void*, byte*, byte> IsLargeObject;
        public delegate* unmanaged<void*, byte*, void> ValidateObjectMember;
        public delegate* unmanaged<void*, byte*, byte*> NextObj;
        public delegate* unmanaged<void*, void*, byte, byte*> GetContainingObject;
        public delegate* unmanaged<void*, byte*, delegate* unmanaged<byte*, void*, byte>, void*, void> DiagWalkObject;
        public delegate* unmanaged<void*, byte*, delegate* unmanaged<byte*, byte**, void*, byte>, void*, void> DiagWalkObject2;
        public delegate* unmanaged<void*, delegate* unmanaged<byte*, void*, byte>, void*, int, byte, void> DiagWalkHeap;
        public delegate* unmanaged<void*, void*, delegate* unmanaged<byte*, byte*, nint, void*, byte, byte, void>, void*, walk_surv_type, int, void> DiagWalkSurvivorsWithType;
        public delegate* unmanaged<void*, void*, delegate* unmanaged<byte, void*, void>, void> DiagWalkFinalizeQueue;
        public delegate* unmanaged<void*, delegate* unmanaged<byte**, ScanContext*, uint, void>, ScanContext*, void> DiagScanFinalizeQueue;
        public delegate* unmanaged<void*, delegate* unmanaged<byte**, byte*, uint, ScanContext*, byte, void>, int, ScanContext*, void> DiagScanHandles;
        public delegate* unmanaged<void*, delegate* unmanaged<byte**, byte*, uint, ScanContext*, byte, void>, int, ScanContext*, void> DiagScanDependentHandles;
        public delegate* unmanaged<void*, delegate* unmanaged<void*, int, byte*, byte*, byte*, void>, void*, void> DiagDescrGenerations;
        public delegate* unmanaged<void*, void> DiagTraceGCSegments;
        public delegate* unmanaged<void*, EtwGCSettingsInfo*, void> DiagGetGCSettings;
        public delegate* unmanaged<void*, gc_alloc_context*, byte> StressHeap;
        public delegate* unmanaged<void*, segment_info*, segment_handle> RegisterFrozenSegment;
        public delegate* unmanaged<void*, segment_handle, void> UnregisterFrozenSegment;
        public delegate* unmanaged<void*, byte*, byte> IsInFrozenSegment;
        public delegate* unmanaged<void*, GCEventKeyword, GCEventLevel, void> ControlEvents;
        public delegate* unmanaged<void*, GCEventKeyword, GCEventLevel, void> ControlPrivateEvents;
        public delegate* unmanaged<void*, byte*, byte**, byte**, byte**, uint> GetGenerationWithRange;
        public delegate* unmanaged<void*, long> GetTotalPauseDuration;
        public delegate* unmanaged<void*, void*, delegate* unmanaged<void*, byte*, byte*, GCConfigurationType, long, void>, void> EnumerateConfigurationValues;
        public delegate* unmanaged<void*, segment_handle, byte*, byte*, void> UpdateFrozenSegment;
        public delegate* unmanaged<void*, int> RefreshMemoryLimit;
        public delegate* unmanaged<void*, NoGCRegionCallbackFinalizerWorkItem*, ulong, enable_no_gc_region_callback_status> EnableNoGCRegionCallback;
        public delegate* unmanaged<void*, FinalizerWorkItem*> GetExtraWorkForFinalization;
        public delegate* unmanaged<void*, int, ulong> GetGenerationBudget;
        public delegate* unmanaged<void*, nuint> GetLOHThreshold;
        public delegate* unmanaged<void*, delegate* unmanaged<byte*, void*, byte>, void*, int, byte, void> DiagWalkHeapWithACHandling;
        public delegate* unmanaged<void*, nuint, void*, void> NullBridgeObjectsWeakRefs;
    }

    /// <summary>
    /// Virtual method table of gcinterface.ee.h `IGCToCLR`, in declaration order (52 slots).
    /// </summary>
    internal unsafe struct IGCToCLRVtable
    {
        /// <summary>Number of virtual slots this vtable describes.</summary>
        public const int SlotCount = 52;

        public delegate* unmanaged<void*, SUSPEND_REASON, void> SuspendEE;
        public delegate* unmanaged<void*, byte, void> RestartEE;
        public delegate* unmanaged<void*, delegate* unmanaged<byte**, ScanContext*, uint, void>, int, int, ScanContext*, void> GcScanRoots;
        public delegate* unmanaged<void*, int, int, void> GcStartWork;
        public delegate* unmanaged<void*, int, byte, byte, void> BeforeGcScanRoots;
        public delegate* unmanaged<void*, int, int, ScanContext*, void> AfterGcScanRoots;
        public delegate* unmanaged<void*, int, void> GcDone;
        public delegate* unmanaged<void*, byte*, byte> RefCountedHandleCallbacks;
        public delegate* unmanaged<void*, delegate* unmanaged<byte**, nuint*, nuint, nuint, void>, nuint, nuint, void> SyncBlockCacheWeakPtrScan;
        public delegate* unmanaged<void*, int, void> SyncBlockCacheDemote;
        public delegate* unmanaged<void*, int, void> SyncBlockCachePromotionsGranted;
        public delegate* unmanaged<void*, uint> GetActiveSyncBlockCount;
        public delegate* unmanaged<void*, byte> IsPreemptiveGCDisabled;
        public delegate* unmanaged<void*, byte> EnablePreemptiveGC;
        public delegate* unmanaged<void*, void> DisablePreemptiveGC;
        public delegate* unmanaged<void*, void*> GetThread;
        public delegate* unmanaged<void*, gc_alloc_context*> GetAllocContext;
        public delegate* unmanaged<void*, delegate* unmanaged<gc_alloc_context*, void*, void>, void*, void> GcEnumAllocContexts;
        public delegate* unmanaged<void*, byte*, byte*> GetLoaderAllocatorObjectForGC;
        public delegate* unmanaged<void*, delegate* unmanaged<void*, void>, void*, byte, byte*, byte> CreateThread;
        public delegate* unmanaged<void*, int, byte, void> DiagGCStart;
        public delegate* unmanaged<void*, void> DiagUpdateGenerationBounds;
        public delegate* unmanaged<void*, nuint, int, int, byte, void> DiagGCEnd;
        public delegate* unmanaged<void*, void*, void> DiagWalkFReachableObjects;
        public delegate* unmanaged<void*, void*, byte, void> DiagWalkSurvivors;
        public delegate* unmanaged<void*, void*, int, void> DiagWalkUOHSurvivors;
        public delegate* unmanaged<void*, void*, void> DiagWalkBGCSurvivors;
        public delegate* unmanaged<void*, WriteBarrierParameters*, void> StompWriteBarrier;
        public delegate* unmanaged<void*, byte, void> EnableFinalization;
        public delegate* unmanaged<void*, uint, void> HandleFatalError;
        public delegate* unmanaged<void*, byte*, byte> EagerFinalized;
        public delegate* unmanaged<void*, void*> GetFreeObjectMethodTable;
        public delegate* unmanaged<void*, byte*, byte*, byte*, byte> GetBooleanConfigValue;
        public delegate* unmanaged<void*, byte*, byte*, long*, byte> GetIntConfigValue;
        public delegate* unmanaged<void*, byte*, byte*, byte**, byte> GetStringConfigValue;
        public delegate* unmanaged<void*, byte*, void> FreeStringConfigValue;
        public delegate* unmanaged<void*, byte> IsGCThread;
        public delegate* unmanaged<void*, byte> WasCurrentThreadCreatedByGC;
        public delegate* unmanaged<void*, byte*, ScanContext*, delegate* unmanaged<byte**, ScanContext*, uint, void>, void> WalkAsyncPinnedForPromotion;
        public delegate* unmanaged<void*, byte*, void*, delegate* unmanaged<byte*, byte*, void*, void>, void> WalkAsyncPinned;
        public delegate* unmanaged<void*, void*> EventSink;
        public delegate* unmanaged<void*, uint> GetTotalNumSizedRefHandles;
        public delegate* unmanaged<void*, int, byte> AnalyzeSurvivorsRequested;
        public delegate* unmanaged<void*, nuint, int, ulong, delegate* unmanaged<void>, void> AnalyzeSurvivorsFinished;
        public delegate* unmanaged<void*, void> VerifySyncTableEntry;
        public delegate* unmanaged<void*, int, int, int, int, void> UpdateGCEventStatus;
        public delegate* unmanaged<void*, uint, uint, void*, void> LogStressMsg;
        public delegate* unmanaged<void*, uint> GetCurrentProcessCpuCount;
        public delegate* unmanaged<void*, int, byte*, byte*, byte*, void> DiagAddNewRegion;
        public delegate* unmanaged<void*, byte*, void> LogErrorToHost;
        public delegate* unmanaged<void*, void*, ulong> GetThreadOSThreadId;
        public delegate* unmanaged<void*, MarkCrossReferencesArgs*, void> TriggerClientBridgeProcessing;
    }

    /// <summary>
    /// Virtual method table of gcinterface.ee.h `IGCToCLREventSink`, in declaration order (38 slots).
    /// </summary>
    internal unsafe struct IGCToCLREventSinkVtable
    {
        /// <summary>Number of virtual slots this vtable describes.</summary>
        public const int SlotCount = 38;

        public delegate* unmanaged<void*, byte*, void*, uint, void> FireDynamicEvent;
        public delegate* unmanaged<void*, uint, uint, uint, uint, void> FireGCStart_V2;
        public delegate* unmanaged<void*, uint, uint, void> FireGCEnd_V1;
        public delegate* unmanaged<void*, byte, void*, ulong, ulong, void> FireGCGenerationRange;
        public delegate* unmanaged<void*, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, uint, uint, uint, void> FireGCHeapStats_V2;
        public delegate* unmanaged<void*, void*, nuint, uint, void> FireGCCreateSegment_V1;
        public delegate* unmanaged<void*, void*, void> FireGCFreeSegment_V1;
        public delegate* unmanaged<void*, void> FireGCCreateConcurrentThread_V1;
        public delegate* unmanaged<void*, void> FireGCTerminateConcurrentThread_V1;
        public delegate* unmanaged<void*, uint, void> FireGCTriggered;
        public delegate* unmanaged<void*, uint, uint, ulong, void> FireGCMarkWithType;
        public delegate* unmanaged<void*, uint, uint, uint, uint, void> FireGCJoin_V2;
        public delegate* unmanaged<void*, ulong, int, uint, uint, uint, uint, uint, uint, uint, uint, uint, uint, void*, void> FireGCGlobalHeapHistory_V4;
        public delegate* unmanaged<void*, uint, uint, void> FireGCAllocationTick_V1;
        public delegate* unmanaged<void*, ulong, uint, uint, void*, ulong, void> FireGCAllocationTick_V4;
        public delegate* unmanaged<void*, void*, byte**, void> FirePinObjectAtGCTime;
        public delegate* unmanaged<void*, byte*, byte*, byte*, void> FirePinPlugAtGCTime;
        public delegate* unmanaged<void*, void*, void*, void*, void*, void*, void*, uint, uint, uint, uint, uint, uint, void*, uint, uint, void*, void> FireGCPerHeapHistory_V3;
        public delegate* unmanaged<void*, ushort, uint, void*, void> FireGCLOHCompact;
        public delegate* unmanaged<void*, ushort, nuint, ushort, uint, void*, void> FireGCFitBucketInfo;
        public delegate* unmanaged<void*, void> FireBGCBegin;
        public delegate* unmanaged<void*, void> FireBGC1stNonConEnd;
        public delegate* unmanaged<void*, void> FireBGC1stConEnd;
        public delegate* unmanaged<void*, uint, void> FireBGC1stSweepEnd;
        public delegate* unmanaged<void*, void> FireBGC2ndNonConBegin;
        public delegate* unmanaged<void*, void> FireBGC2ndNonConEnd;
        public delegate* unmanaged<void*, void> FireBGC2ndConBegin;
        public delegate* unmanaged<void*, void> FireBGC2ndConEnd;
        public delegate* unmanaged<void*, ulong, void> FireBGCDrainMark;
        public delegate* unmanaged<void*, ulong, ulong, uint, void> FireBGCRevisit;
        public delegate* unmanaged<void*, ulong, ulong, ulong, uint, uint, void> FireBGCOverflow_V1;
        public delegate* unmanaged<void*, uint, void> FireBGCAllocWaitBegin;
        public delegate* unmanaged<void*, uint, void> FireBGCAllocWaitEnd;
        public delegate* unmanaged<void*, uint, uint, void> FireGCFullNotify_V1;
        public delegate* unmanaged<void*, void*, void*, uint, uint, void> FireSetGCHandle;
        public delegate* unmanaged<void*, void*, void*, uint, uint, void> FirePrvSetGCHandle;
        public delegate* unmanaged<void*, void*, void> FireDestroyGCHandle;
        public delegate* unmanaged<void*, void*, void> FirePrvDestroyGCHandle;
    }
}
