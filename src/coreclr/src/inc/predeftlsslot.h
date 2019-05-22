// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef __PREDEFTLSSLOT_H__
#define __PREDEFTLSSLOT_H__

// ******************************************************************************
// WARNING!!!: These enums are used by SOS in the diagnostics repo. Values should 
// added or removed in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/master/src/inc/predeftlsslot.h
// ******************************************************************************

// And here are the predefined slots for accessing TLS from various DLLs of the CLR.
// Note that we want to support combinations of Debug and Retail DLLs for testing
// purposes, so we burn the slots into the retail EE even if a debug CLR dll needs
// them.
enum PredefinedTlsSlots
{
    TlsIdx_StrongName,
    TlsIdx_JitPerf,
    TlsIdx_JitX86Perf,
    TlsIdx_JitLogEnv,
    TlsIdx_IceCap,
    TlsIdx_StressLog,
    TlsIdx_CantStopCount, // Can't-stop counter for any thread
    TlsIdx_Check,
    TlsIdx_ForbidGCLoaderUseCount,
    TlsIdx_ClrDebugState,         // Pointer to ClrDebugState* structure
    TlsIdx_StressThread,

    // Add more indices here.
    TlsIdx_ThreadType, // bit flags to indicate special thread's type 
    TlsIdx_OwnedCrstsChain, // slot to store the Crsts owned by this thread
    TlsIdx_AppDomainAgilePendingTable,
    TlsIdx_CantAllocCount, //Can't allocate memory on heap in this thread
    TlsIdx_AssertDlgStatus, // Whether the thread is displaying an assert dialog

    // A transient thread value that indicates this thread is currently walking its stack
    // or the stack of another thread. This value is useful to help short-circuit
    // some problematic checks in the loader, guarantee that types & assemblies
    // encountered during the walk must already be loaded, and provide information to control
    // assembly loading behavior during stack walks.
    // 
    // This value is set around the main portions of the stack walk (as those portions may
    // enter the type & assembly loaders). This is also explicitly cleared while the
    // walking thread calls the stackwalker callback or needs to execute managed code, as
    // such calls may execute arbitrary code unrelated to the actual stack walking, and	
    // may never return, in the case of exception stackwalk callbacks.
    TlsIdx_StackWalkerWalkingThread, // Thread* that the stack walker is currently walking.

    // Save the last exception info.  Sometimes we need this info in our EX_CATCH, such as for SO.
    // It will be better if VC can supply this in catch(...) block.
    // !!! These data may become stale.  Use it only inside exception handling code.
    // !!! Please access these fields through GetCurrentExceptionPointers which validates the data to some level.
    TlsIdx_EXCEPTION_CODE,
    TlsIdx_PEXCEPTION_RECORD,
    TlsIdx_PCONTEXT,

    MAX_PREDEFINED_TLS_SLOT
};

enum TlsThreadTypeFlag // flag used for thread type in Tls data
{
    ThreadType_GC                       = 0x00000001,
    ThreadType_Timer                    = 0x00000002,
    ThreadType_Gate                     = 0x00000004,
    ThreadType_DbgHelper                = 0x00000008,
    ThreadType_Shutdown                 = 0x00000010,
    ThreadType_DynamicSuspendEE         = 0x00000020,
    ThreadType_Finalizer                = 0x00000040,
    ThreadType_ADUnloadHelper           = 0x00000200,
    ThreadType_ShutdownHelper           = 0x00000400,
    ThreadType_Threadpool_IOCompletion  = 0x00000800,
    ThreadType_Threadpool_Worker        = 0x00001000,
    ThreadType_Wait                     = 0x00002000,
    ThreadType_ProfAPI_Attach           = 0x00004000,
    ThreadType_ProfAPI_Detach           = 0x00008000,
    ThreadType_ETWRundownThread         = 0x00010000,
    ThreadType_GenericInstantiationCompare= 0x00020000, // Used to indicate that the thread is determining if a generic instantiation in an ngen image matches a lookup. 
};

static_assert(TlsIdx_ThreadType == 11, "SOS in diagnostics repo has a dependency on this value.");

#endif

