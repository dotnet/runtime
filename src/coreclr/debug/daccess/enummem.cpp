// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: enummem.cpp
//

//
// ICLRDataEnumMemoryRegions implementation.
//
//*****************************************************************************

#include "stdafx.h"

#include <eeconfig.h>
#include <ecall.h>
#include <gcinfodecoder.h>

#include "typestring.h"
#include "daccess.h"
#include "binder.h"
#include "win32threadpool.h"
#include "runtimeinfo.h"

#ifdef FEATURE_COMWRAPPERS
#include <interoplibinterface.h>
#include <interoplibabi.h>
#endif // FEATURE_COMWRAPPERS

extern HRESULT GetDacTableAddress(ICorDebugDataTarget* dataTarget, ULONG64 baseAddress, PULONG64 dacTableAddress);

#if defined(DAC_MEASURE_PERF)

unsigned __int64 g_nTotalTime;
unsigned __int64 g_nStackTotalTime;
unsigned __int64 g_nReadVirtualTotalTime;
unsigned __int64 g_nFindTotalTime;
unsigned __int64 g_nFindHashTotalTime;
unsigned __int64 g_nFindHits;
unsigned __int64 g_nFindCalls;
unsigned __int64 g_nFindFails;
unsigned __int64 g_nStackWalk;
unsigned __int64 g_nFindStackTotalTime;

#endif // #if defined(DAC_MEASURE_PERF)


//
// EnumMemCollectImages - collect all images of interest for heap dumps
//
// This is used primarily to save ngen images.
// This is necessary so that heap dumps contain the full native code for the
// process.  Normally mini/heap dump debugging requires that the images be
// available at debug-time, (in fact, watson explicitly does not want to
// be downloading 3rd party images).  Not including images is the main size
// advantage of heap dumps over full dumps.  However, since ngen images are
// produced on the client, we can't always ensure that the debugger will
// have access to the exact ngen image used in the dump.  Therefore, managed
// heap dumps also include full copies of all NGen images in the process.
//
// We also currently include in-memory modules (provided by a host, or loaded
// from a Byte[]).
//
HRESULT ClrDataAccess::EnumMemCollectImages()
{
    SUPPORTS_DAC;

    ProcessModIter modIter;
    Module* modDef = NULL;
    HRESULT status = S_OK;
    PEFile  *file;
    TADDR pStartAddr = 0;
    ULONG32 ulSize = 0;
    ULONG32 ulSizeBlock;

    TSIZE_T cbMemoryReported = m_cbMemoryReported;

    //
    // Collect the ngen images - Iterating through module list
    //
    EX_TRY
    {
        while ((modDef = modIter.NextModule()))
        {
            EX_TRY
            {
                ulSize = 0;
                file = modDef->GetFile();

                // We want to save all native images
                if (file->HasNativeImage())
                {
                    // We should only skip if signed by Microsoft!
                    pStartAddr = PTR_TO_TADDR(file->GetLoadedNative()->GetBase());
                    ulSize = file->GetLoadedNative()->GetSize();
                }
                // We also want to save any in-memory images.  These show up like mapped files
                // and so would not be in a heap dump by default.  Technically it's not clear we
                // should include them in the dump - you can often have the files available
                // after-the-fact. But in-memory modules may be harder to track down at debug time
                // and people may have come to rely on this - so we'll include them for now.
                else
                if (
                    file->GetPath().IsEmpty() && // is in-memory
                    file->HasMetadata() &&       // skip resource assemblies
                    file->IsLoaded(FALSE) &&     // skip files not yet loaded
                    !file->IsDynamic())          // skip dynamic (GetLoadedIL asserts anyway)
                {
                    pStartAddr = PTR_TO_TADDR(file->GetLoadedIL()->GetBase());
                    ulSize = file->GetLoadedIL()->GetSize();
                }

                // memory are mapped in in GetOsPageSize() size.
                // Some memory are mapped in but some are not. You cannot
                // write all in one block. So iterating through page size
                //
                while (ulSize > 0)
                {
                    //
                    // Note that we have talked about not writing IL and Metadata to save size.
                    // It turns out IL was rarely mapped in.
                    // Metadata is needed. The RVA field is needed for it is pointed to a
                    // MethodHeader MethodDesc::GetILHeader. Without this RVA,
                    // all locals are broken. In case, you are asked about this question again.
                    //
                    ulSizeBlock = ulSize > GetOsPageSize() ? GetOsPageSize() : ulSize;
                    ReportMem(pStartAddr, ulSizeBlock, false);
                    pStartAddr += ulSizeBlock;
                    ulSize -= ulSizeBlock;
                }
            }
            EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED
        }
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

    m_dumpStats.m_cbNgen = m_cbMemoryReported - cbMemoryReported;
    return status;
}



//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// collecting memory for mscorwks's heap dump critical statics
// This include the stress log, config structure, and IPC block
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemCLRHeapCrticalStatic(IN CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    TSIZE_T cbMemoryReported = m_cbMemoryReported;

    // Write out the stress log structure itself
    DacEnumHostDPtrMem(g_pStressLog);

    // This is pointing to a static buffer
    DacEnumHostDPtrMem(g_pConfig);

    // dump GC heap structures. Note that the managed heap is not dumped out.
    // We are just dump the GC heap structures.
    //
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( EnumWksGlobalMemoryRegions(flags); );
#ifdef FEATURE_SVR_GC
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( EnumSvrGlobalMemoryRegions(flags); );
#endif

    m_dumpStats.m_cbClrHeapStatics = m_cbMemoryReported - cbMemoryReported;

    return S_OK;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// collecting memory for mscorwks's statics. This is the minimal
// set of global and statics that we need to have !threads, !pe, !ClrStack
// to work.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemCLRStatic(IN CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    TSIZE_T cbMemoryReported = m_cbMemoryReported;

    //
    // write out the static and global content that we care.
    //

    // The followig macro will report memory all of the dac related mscorwks static and
    // global variables. But it won't report the structures that are pointed by
    // global pointers.
    //
#define DEFINE_DACVAR(id_type, size_type, id, var) \
    ReportMem(m_globalBase + g_dacGlobals.id, sizeof(size_type));

#ifdef TARGET_UNIX
    ULONG64 dacTableAddress;
    HRESULT hr = GetDacTableAddress(m_pTarget, m_globalBase, &dacTableAddress);
    if (FAILED(hr)) {
        return hr;
    }
    // Add the dac table memory in coreclr
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED ( ReportMem((TADDR)dacTableAddress, sizeof(g_dacGlobals)); )
#endif

    // Cannot use CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED
    // around conditional preprocessor directives in a sane fashion.
    EX_TRY
    {
#include "dacvars.h"
    }
    EX_CATCH
    {
        // Catch the exception and keep going unless COR_E_OPERATIONCANCELED
        // was thrown. Used generating dumps, where rethrow will cancel dump.
    }
    EX_END_CATCH(RethrowCancelExceptions)

    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED ( ReportMem(m_globalBase + g_dacGlobals.dac__g_pStressLog, sizeof(StressLog *)); )

    EX_TRY
    {
        // These two static pointers are pointed to static data of byte[]
        // then run constructor in place
        //
        ReportMem(m_globalBase + g_dacGlobals.SystemDomain__m_pSystemDomain, sizeof(SystemDomain));

        // We need IGCHeap pointer to make EEVersion work
        ReportMem(m_globalBase + g_dacGlobals.dac__g_pGCHeap, sizeof(IGCHeap *));

        // see synblk.cpp, the pointer is pointed to a static byte[]
        SyncBlockCache::s_pSyncBlockCache.EnumMem();

        ReportMem(m_globalBase + g_dacGlobals.dac__g_FCDynamicallyAssignedImplementations,
                  sizeof(TADDR)*ECall::NUM_DYNAMICALLY_ASSIGNED_FCALL_IMPLEMENTATIONS);

        ReportMem(g_gcDacGlobals.GetAddr(), sizeof(GcDacVars));

        // We need all of the dac variables referenced by the GC DAC global struct.
        // This struct contains pointers to pointers, so we first dereference the pointers
        // to obtain the location of the variable that's reported.
#define GC_DAC_VAR(type, name) ReportMem(g_gcDacGlobals->name.GetAddr(), sizeof(type));
#include "../../gc/gcinterface.dacvars.def"
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

#ifndef TARGET_UNIX
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_runtimeLoadedBaseAddress.EnumMem(); )
#endif // !TARGET_UNIX

    // These are the structures that are pointed by global pointers and we care.
    // Some may reside in heap and some may reside as a static byte array in mscorwks.dll
    // That is ok. We will report them explicitly.
    //
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pConfig.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pPredefinedArrayTypes.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pObjectClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pStringClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pArrayClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pExceptionClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pThreadAbortExceptionClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pOutOfMemoryExceptionClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pStackOverflowExceptionClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pExecutionEngineExceptionClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pDelegateClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pMulticastDelegateClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pValueTypeClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pEnumClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pThreadClass.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pFreeObjectMethodTable.EnumMem(); )

    // These two static pointers are pointed to static data of byte[]
    // then run constructor in place
    //
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( SystemDomain::m_pSystemDomain.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pDebugger.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pEEInterface.EnumMem(); )
    if (g_pDebugInterface != nullptr)
    {
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED(g_pDebugInterface.EnumMem(); )
    }
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pEEDbgInterfaceImpl.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_CORDebuggerControlFlags.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_CoreLib.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT].EnumMemoryRegions(flags); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( StubManager::EnumMemoryRegions(flags); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pFinalizerThread.EnumMem(); )
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pSuspensionThread.EnumMem(); )

    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_heap_type.EnumMem(); )

    m_dumpStats.m_cbClrStatics = m_cbMemoryReported - cbMemoryReported;

    return S_OK;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// This function reports memory that a heap dump need to debug CLR
// and managed code efficiently.
//
// We will write out -
// 1. mscorwks.dll's image read/write pages
// 2. IPC blocks - shared memory (needed for debugging service and perf counter)
// 3. ngen images excluding Metadata and IL for size perf
// 4. We may want to touch the code pages on the stack - to be safe....
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemoryRegionsWorkerHeap(IN CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    HRESULT status = S_OK;

    // clear all of the previous cached memory
    Flush();

    // collect ngen image
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCollectImages(); );

    // collect CLR static
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRStatic(flags); );
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRHeapCrticalStatic(flags); );

    // Note that we do not need to flush out all of the dac instance manager's instance.
    // This is because it is a heap dump here. Assembly and AppDomain objects will be reported
    // by the default heap collection mechanism by dbghelp.lib
    //
    // Microsoft: I suspect if we have all private read-write pages the preceding statement
    // would be true, but I don't think we have that guarantee here.
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpModuleList(flags); );

    // Iterating to all threads' stacks, as we have to collect data stored inside (core)clr.dll
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAllThreadsStack(flags); )

    // Dump AppDomain-specific info
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAppDomainInfo(flags); )

    // Dump the Debugger object data needed
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pDebugger->EnumMemoryRegions(flags); )

    // now dump the memory get dragged in by using DAC API implicitly.
    m_dumpStats.m_cbImplicity = m_instances.DumpAllInstances(m_enumMemCb);

    // Do not let any remaining implicitly enumerated memory leak out.
    Flush();

    return S_OK;
}   // EnumMemoryRegionsWorkerHeap

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Helper function for skinny mini-dump
// Pass in an managed object, this function will dump the EEClass hierachy
// and field desc of object so SOS's !DumpObj will work
//
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::DumpManagedObject(CLRDataEnumMemoryFlags flags, OBJECTREF objRef)
{
    SUPPORTS_DAC;

    HRESULT status = S_OK;

    if (objRef == NULL)
    {
        return status;
    }

    if (*g_gcDacGlobals->gc_structures_invalid_cnt != 0)
    {
        // GC is in progress, don't dump this object
        return S_OK;
    }

    EX_TRY
    {
        // write out the current EE class and the direct/indirect inherited EE Classes
        MethodTable *pMethodTable = objRef->GetGCSafeMethodTable();

        while (pMethodTable)
        {
            EX_TRY
            {
                pMethodTable->EnumMemoryRegions(flags);

                StackSString s;

                // This might look odd. We are not using variable s after forming it.
                // That is because our DAC inspecting API is using TypeString to form
                // full type name. Form the full name here is a implicit reference to needed
                // memory.
                //
                TypeString::AppendType(s, TypeHandle(pMethodTable), TypeString::FormatNamespace|TypeString::FormatFullInst);
            }
            EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

            // Walk up to parent MethodTable
            pMethodTable = pMethodTable->GetParentMethodTable();
        }

        // now dump the content for the managed object
        objRef->EnumMemoryRegions();
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

    return status;
}


//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Helper function for skinny mini-dump
// Pass in an managed excption object, this function will dump
// the managed exception object and some of its fields, such as message, stack trace string,
// inner exception.
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::DumpManagedExcepObject(CLRDataEnumMemoryFlags flags, OBJECTREF objRef)
{
    SUPPORTS_DAC;

    if (objRef == NULL)
    {
        return S_OK;
    }

    if (*g_gcDacGlobals->gc_structures_invalid_cnt != 0)
    {
        // GC is in progress, don't dump this object
        return S_OK;
    }

    // write out the managed object for exception. This will only write out the
    // direct field value. After this, we need to visit some selected fields, such as
    // exception message and stack trace field, and dump out the object referenced via
    // the fields.
    //
    DumpManagedObject(flags, objRef);

    // If this is not a pre-allocated exception type, then we'll need to dump out enough memory to ensure
    // that the lookup codepath from the Module to information for the type of this Exception will
    // be present.  Simply dumping the managed object itself isn't enough.  Sos doesn't need this.
    EX_TRY
    {
        MethodTable * pMethodTable = objRef->GetGCSafeMethodTable();
        PTR_Module pModule = pMethodTable->GetModule();
        mdTypeDef exceptionTypeDef = pMethodTable->GetCl();

        if (TypeFromToken(exceptionTypeDef) != mdtTypeDef)
        {
            _ASSERTE(!"Module should have contained a TypeDef, dump will likely be missing exception type lookup!");
        }

        // The lookup from the Module that contains this TypeDef:
        pModule->LookupTypeDef(RidFromToken(exceptionTypeDef));

        // If it's a generic class, we need to implicitly enumerate the memory needed to look it up
        // and enable the calls that ICD will want to make against the TypeHandle when retrieving the
        // Exception info.
        TypeHandle th;
        th = ClassLoader::LookupTypeDefOrRefInModule(pModule, exceptionTypeDef);
        th.EnumMemoryRegions(flags);
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    // store the exception type name
    EX_TRY
    {
        MethodTable * pMethodTable = objRef->GetGCSafeMethodTable();
        StackSString s;
        TypeString::AppendType(s, TypeHandle(pMethodTable), TypeString::FormatNamespace|TypeString::FormatFullInst);
        DacMdCacheAddEEName(dac_cast<TADDR>(pMethodTable), s);
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    EXCEPTIONREF exceptRef = (EXCEPTIONREF)objRef;

    if (flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        // dump the exception message field
        DumpManagedObject(flags, (OBJECTREF)exceptRef->GetMessage());
    }

    // dump the exception's stack trace field
    DumpManagedStackTraceStringObject(flags, exceptRef->GetStackTraceString());

    // dump the exception's remote stack trace field only if we are not generating a triage dump, or the exception type does not override
    // the StackTrace getter (see Exception.InternalPreserveStackTrace to understand why)
    if (flags != CLRDATA_ENUM_MEM_TRIAGE ||
        !ExceptionTypeOverridesStackTraceGetter(exceptRef->GetGCSafeMethodTable()))
    {
        DumpManagedStackTraceStringObject(flags, exceptRef->GetRemoteStackTraceString());
    }

    // Dump inner exception
    DumpManagedExcepObject(flags, exceptRef->GetInnerException());

    // Dump the stack trace array object and its underlying type
    I1ARRAYREF stackTraceArrayObj = exceptRef->GetStackTraceArrayObject();

    // There are cases where a managed exception does not have a stack trace.
    // These cases are:
    // * exception was thrown by VM and no managed frames are on the thread.
    // * exception thrown is a preallocated exception.
    if (stackTraceArrayObj != NULL)
    {
        // first dump the array's element type
        TypeHandle arrayTypeHandle = stackTraceArrayObj->GetTypeHandle();
        TypeHandle elementTypeHandle = arrayTypeHandle.GetArrayElementTypeHandle();
        elementTypeHandle.AsMethodTable()->EnumMemoryRegions(flags);
        elementTypeHandle.AsMethodTable()->GetClass()->EnumMemoryRegions(flags, elementTypeHandle.AsMethodTable());

        // now dump the actual stack trace array object
        DumpManagedObject(flags, (OBJECTREF)stackTraceArrayObj);
    }

    // Dump the stack trace native structure. Unfortunately, we need to write out the
    // native structure and also dump the MethodDesc that we care about!
    // We need to ensure the entire _stackTrace from the Exception is enumerated and
    // included in the dump.  When we touch the header and each element looking for the
    // MD this happens.
    StackTraceArray stackTrace;
    exceptRef->GetStackTrace(stackTrace);
    for(size_t i = 0; i < stackTrace.Size(); i++)
    {
        MethodDesc* pMD = stackTrace[i].pFunc;
        if (!DacHasMethodDescBeenEnumerated(pMD) && DacValidateMD(pMD))
        {
            pMD->EnumMemoryRegions(flags);

            // The following calls are to ensure that mscordacwks!DacDbiInterfaceImpl::GetNativeCodeInfo
            // will succeed for all dumps.

            // Pulls in data to translate from token to MethodDesc
            FindLoadedMethodRefOrDef(pMD->GetMethodTable()->GetModule(), pMD->GetMemberDef());

            // Pulls in sequence points.
            DebugInfoManager::EnumMemoryRegionsForMethodDebugInfo(flags, pMD);
            PCODE addr = pMD->GetNativeCode();
            if (addr != NULL)
            {
                EECodeInfo codeInfo(addr);
                if (codeInfo.IsValid())
                {
                    IJitManager::MethodRegionInfo methodRegionInfo = { NULL, 0, NULL, 0 };
                    codeInfo.GetMethodRegionInfo(&methodRegionInfo);
                }
            }
        }

        // Enumerate the code around call site to help SOS resolve the source lines
        TADDR callEnd = PCODEToPINSTR(stackTrace[i].ip);
        DacEnumCodeForStackwalk(callEnd);
    }

    return S_OK;
}

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Helper function for skinny mini-dump
// Pass in a string object representing a managed stack trace, this function will
// dump it and "poison" the contents with a PII-free version of the stack trace.
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::DumpManagedStackTraceStringObject(CLRDataEnumMemoryFlags flags, STRINGREF orefStackTrace)
{
    SUPPORTS_DAC;

    if (orefStackTrace == NULL)
    {
        return S_OK;
    }

    // dump the stack trace string object
    DumpManagedObject(flags, (OBJECTREF)orefStackTrace);

    if (flags == CLRDATA_ENUM_MEM_TRIAGE)
    {
        // StringObject::GetSString does not support DAC, use GetBuffer/GetStringLength
        SString stackTrace(dac_cast<PTR_WSTR>((TADDR)orefStackTrace->GetBuffer()), orefStackTrace->GetStringLength());

        StripFileInfoFromStackTrace(stackTrace);

        COUNT_T traceCharCount = stackTrace.GetCount();
        _ASSERTE(traceCharCount <= orefStackTrace->GetStringLength());

        // fill the rest of the string with \0
        WCHAR *buffer = stackTrace.OpenUnicodeBuffer(orefStackTrace->GetStringLength());
        memset(buffer + traceCharCount, 0, sizeof(WCHAR) * (orefStackTrace->GetStringLength() - traceCharCount));

        // replace the string
        DacUpdateMemoryRegion(dac_cast<TADDR>(orefStackTrace) + StringObject::GetBufferOffset(), sizeof(WCHAR) * orefStackTrace->GetStringLength(), (BYTE *)buffer);
    }

    return S_OK;
}

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Iterating through module list and report the memory.
// Remember to call
//  m_instances.DumpAllInstances(m_enumMemCb);
// when all memory enumeration are done if you call this function!
// This is because using ProcessModIter will drag in some memory implicitly.
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemDumpModuleList(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    ProcessModIter  modIter;
    Module*         modDef;
    TADDR           base;
    ULONG32         length;
    PEFile          *file;
    TSIZE_T         cbMemoryReported = m_cbMemoryReported;

    //
    // Iterating through module list
    //

    // Cannot use CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED around
    // conditional pre-processor directives in a sane fashion
    EX_TRY
    {
        while ((modDef = modIter.NextModule()))
        {
            // We also want to dump the link from the Module back to the AppDomain,
            // since the stackwalker uses it to find the AD.
            CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED
            (
                // Pass false to ensure we force enumeration of this module's references.
                modDef->EnumMemoryRegions(flags, false);
            );

            EX_TRY
            {
                // To enable a debugger to check on whether a module is an NI or IL image, they need
                // the DOS header, PE headers, and IMAGE_COR20_HEADER for the Flags member.
                // We expose no API today to find this out.
                PTR_PEFile pPEFile = modDef->GetFile();
                PEImage * pILImage = pPEFile->GetILimage();
                PEImage * pNIImage = pPEFile->GetNativeImage();

                // Implicitly gets the COR header.
                if ((pILImage) && (pILImage->HasLoadedLayout()))
                {
                    pILImage->GetCorHeaderFlags();
                }
                if ((pNIImage) && (pNIImage->HasLoadedLayout()))
                {
                    pNIImage->GetCorHeaderFlags();
                }
            }
            EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED


            EX_TRY
            {
                file = modDef->GetFile();
                base = PTR_TO_TADDR(file->GetLoadedImageContents(&length));
                file->EnumMemoryRegions(flags);
            }
            EX_CATCH
            {
                // Catch the exception and keep going unless COR_E_OPERATIONCANCELED
                // was thrown. Used generating dumps, where rethrow will cancel dump.
            }
            EX_END_CATCH(RethrowCancelExceptions)
        }
    }
    EX_CATCH
    {
        // Catch the exception and keep going unless COR_E_OPERATIONCANCELED
        // was thrown. Used generating dumps, where rethrow will cancel dump.
    }
    EX_END_CATCH(RethrowCancelExceptions)

    m_dumpStats.m_cbModuleList = m_cbMemoryReported - cbMemoryReported;

    return S_OK;
}

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Iterate through AppDomains and report specific memory needed
//  for all dumps, such as the Module lookup path.
// This is intended for MiniDumpNormal and should be kept small.
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemDumpAppDomainInfo(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    AppDomainIterator adIter(FALSE);
    EX_TRY
    {
        while (adIter.Next())
        {
            CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED
            (
                // Note that the flags being CLRDATA_ENUM_MEM_MINI prevents
                // you from pulling entire files loaded into memory into the dump.
                adIter.GetDomain()->EnumMemoryRegions(flags, true);
            );
        }
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

    return S_OK;
}

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Iterating through each frame to make sure
// we dump out MethodDesc, DJI etc related info
// This is a generic helper for walking stack. However, if you call
// this function, make sure to flush instance in the DAC Instance manager.
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemWalkStackHelper(CLRDataEnumMemoryFlags flags,
                                              IXCLRDataStackWalk  *pStackWalk,
                                              Thread * pThread)
{
    SUPPORTS_DAC;

#if defined(DAC_MEASURE_PERF)
    g_nStackWalk = 1;
    unsigned __int64 nStart= GetCycleCount();
#endif

    HRESULT status = S_OK;
    ReleaseHolder<IXCLRDataFrame> pFrame(NULL);
    ReleaseHolder<IXCLRDataMethodInstance> pMethod(NULL);
    ReleaseHolder<IXCLRDataMethodDefinition> pMethodDefinition(NULL);
    ReleaseHolder<IXCLRDataTypeInstance> pTypeInstance(NULL);

    MethodDesc * pMethodDesc = NULL;
    EX_TRY
    {
        TADDR previousSP = 0; //start at zero; this allows first check to always succeed.
        TADDR currentSP;
        currentSP = dac_cast<TADDR>(pThread->GetCachedStackLimit()) + sizeof(TADDR);

        // exhaust the frames using DAC api
        for (; status == S_OK; )
        {
            bool frameHadContext = false;
            status = pStackWalk->GetFrame(&pFrame);
            PCODE addr = NULL;
            if (status == S_OK && pFrame != NULL)
            {
                // write out the code that ip pointed to
                T_CONTEXT context;
                REGDISPLAY regDisp;
                if ((status=pFrame->GetContext(CONTEXT_ALL, sizeof(T_CONTEXT),
                                                   NULL, (BYTE *)&context))==S_OK)
                {
                    // Enumerate the code around the call site to help debugger stack walking heuristics
                    ::FillRegDisplay(&regDisp, &context);
                    addr = GetControlPC(&regDisp);
                    TADDR callEnd = PCODEToPINSTR(addr);
                    DacEnumCodeForStackwalk(callEnd);
                    frameHadContext = true;
                }

                //
                // There are identical stack pointer checking semantics in code:Thread::EnumMemoryRegionsWorker
                // See that code for comments.
                // You ***MUST*** maintain identical semantics for both checks!
                //
                CLRDataSimpleFrameType simpleFrameType;
                CLRDataDetailedFrameType detailedFrameType;
                if (SUCCEEDED(pFrame->GetFrameType(&simpleFrameType, &detailedFrameType)))
                {
                    if (!frameHadContext)
                    {
                        _ASSERTE(!"Stack frame should always have an associated context!");
                        break;
                    }

                    // This is StackFrameIterator::SFITER_FRAMELESS_METHOD, initialized by Code:ClrDataStackWalk::GetFrame
                    //  from code:ClrDataStackWalk::RawGetFrameType
                    if (simpleFrameType == CLRDATA_SIMPFRAME_MANAGED_METHOD)
                    {
                        currentSP = (TADDR)GetRegdisplaySP(&regDisp);

                        if (currentSP <= previousSP)
                        {
                            _ASSERTE(!"Target stack has been corrupted, SP for current frame must be larger than previous frame.");
                            break;
                        }

                        if (currentSP % sizeof(TADDR) != 0)
                        {
                            _ASSERTE(!"Target stack has been corrupted, SP must be aligned.");
                            break;
                        }

                        if (!pThread->IsAddressInStack(currentSP))
                        {
                            _ASSERTE(!"Target stack has been corrupted, SP must in in the stack range.");
                            break;
                        }
                    }
                }
                else
                {
                    _ASSERTE(!"The stack frame should always know what type it is!");
                    break;
                }

                status = pFrame->GetMethodInstance(&pMethod);
                if (status == S_OK && pMethod != NULL)
                {
                    // managed frame
                    if (SUCCEEDED(pMethod->GetTypeInstance(&pTypeInstance)) &&
                        (pTypeInstance != NULL))
                    {
                        pTypeInstance.Clear();
                    }

                    if(SUCCEEDED(pMethod->GetDefinition(&pMethodDefinition)) &&
                       (pMethodDefinition != NULL))
                    {
                        pMethodDesc = ((ClrDataMethodDefinition *)pMethodDefinition.GetValue())->GetMethodDesc();
                        if (pMethodDesc)
                        {

                            // If this is a generic, we'll need to pull in enough extra info that
                            // we get decent results later when stackwalking.  Note that we do not guarantee
                            // we'll always get an exact type for any reference type; most of the time the
                            // stack walk will just show System.__Canon, which is the level of support we
                            // guarantee for minidumps without full memory.
                            EX_TRY
                            {
                                if ((pMethodDesc->AcquiresInstMethodTableFromThis()) ||
                                    (pMethodDesc->RequiresInstMethodTableArg()))
                                {
                                    // MethodTable
                                    ReleaseHolder<IXCLRDataValue> pDV(NULL);
                                    ReleaseHolder<IXCLRDataValue> pAssociatedValue(NULL);
                                    CLRDATA_ADDRESS address;
                                    PTR_Object pObjThis = NULL;

                                    if (SUCCEEDED(pFrame->GetArgumentByIndex(0, &pDV, 0, NULL, NULL)) &&
                                        SUCCEEDED(pDV->GetAssociatedValue(&pAssociatedValue)) &&
                                        SUCCEEDED(pAssociatedValue->GetAddress(&address)))
                                    {
                                        // Implicitly enumerate the object itself.
                                        TADDR addrObjThis = CLRDATA_ADDRESS_TO_TADDR(address);
                                        pObjThis = dac_cast<PTR_Object>(addrObjThis);
                                    }

                                    // And now get the extra info we need for the AcquiresInstMethodTableFromThis case.
                                    if (pMethodDesc->AcquiresInstMethodTableFromThis())
                                    {
                                        // When working with the 'this' case, we need to pick up the MethodTable from
                                        // object lookup.
                                        PTR_MethodTable pMT = NULL;
                                        if (pObjThis != NULL)
                                        {
                                            pMT = pObjThis->GetMethodTable();
                                        }

                                        TypeHandle th;
                                        if (pMT != NULL)
                                        {
                                            th = TypeHandle(pMT);
                                        }

                                        Instantiation classInst = pMethodDesc->GetExactClassInstantiation(th);
                                        Instantiation methodInst = pMethodDesc->GetMethodInstantiation();
                                    }

                                }
                                else if (pMethodDesc->RequiresInstMethodDescArg())
                                {
                                    // This method has a generic type token which is required to figure out the exact instantiation
                                    // of the method.
                                    // We need to to use the variable index of the generic type token in order to do the look up.
                                    CLRDATA_ADDRESS address = NULL;
                                    DWORD dwExactGenericArgsTokenIndex = 0;
                                    ReleaseHolder<IXCLRDataValue> pDV(NULL);
                                    ReleaseHolder<IXCLRDataValue> pAssociatedValue(NULL);
                                    ReleaseHolder<IXCLRDataFrame2> pFrame2(NULL);

                                    if (SUCCEEDED(pFrame->QueryInterface(__uuidof(IXCLRDataFrame2), (void**)&pFrame2)) &&
                                        SUCCEEDED(pFrame2->GetExactGenericArgsToken(&pDV)) &&
                                        SUCCEEDED(pDV->GetAssociatedValue(&pAssociatedValue)) &&
                                        SUCCEEDED(pAssociatedValue->GetAddress(&address)))
                                    {
                                        TADDR addrMD = CLRDATA_ADDRESS_TO_TADDR(address);
                                        PTR_MethodDesc pMD = dac_cast<PTR_MethodDesc>(addrMD);
                                        pMD->EnumMemoryRegions(flags);
                                    }

                                    pMethodDesc->EnumMemoryRegions(flags);
                                    MethodTable * pCanonicalMT = pMethodDesc->GetCanonicalMethodTable();
                                    MethodTable * pNormalMT = pMethodDesc->GetMethodTable();
                                    pCanonicalMT->EnumMemoryRegions(flags);
                                    pNormalMT->EnumMemoryRegions(flags);
                                }
                            }
                            EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

                            pMethodDesc->EnumMemoryRegions(flags);

                            // The following calls are to ensure that mscordacwks!DacDbiInterfaceImpl::GetNativeCodeSequencePointsAndVarInfo
                            // will succeed for all dumps.  Local variable info usefulness is somewhat questionable
                            // since most dumps will be for optimized targets.  However, being able to map
                            // back to source lines for functions on stacks is very useful and we don't
                            // want to allow the function to fail for all targets.

                            // Pulls in sequence points and local variable info
                            DebugInfoManager::EnumMemoryRegionsForMethodDebugInfo(flags, pMethodDesc);

#if defined(FEATURE_EH_FUNCLETS) && defined(USE_GC_INFO_DECODER)

                            if (addr != NULL)
                            {
                                EECodeInfo codeInfo(addr);

                                if (codeInfo.IsValid())
                                {
                                    // We want IsFilterFunclet to work for anything on the stack
                                    codeInfo.GetJitManager()->IsFilterFunclet(&codeInfo);

                                    // The stackwalker needs GC info to find the parent 'stack pointer' or PSP
                                    GCInfoToken gcInfoToken = codeInfo.GetGCInfoToken();
                                    PTR_BYTE pGCInfo = dac_cast<PTR_BYTE>(gcInfoToken.Info);
                                    if (pGCInfo != NULL)
                                    {
                                        GcInfoDecoder gcDecoder(gcInfoToken, DECODE_PSP_SYM, 0);
                                        DacEnumMemoryRegion(dac_cast<TADDR>(pGCInfo), gcDecoder.GetNumBytesRead(), true);
                                    }
                                }
                            }
#endif // FEATURE_EH_FUNCLETS && USE_GC_INFO_DECODER
                        }
                        pMethodDefinition.Clear();
                    }
                    pMethod.Clear();
                }
                pFrame.Clear();
            }

            previousSP = currentSP;
            status = pStackWalk->Next();
        }

    }
    EX_CATCH
    {
        status = E_FAIL;
        // Catch the exception and keep going unless a COR_E_OPERATIONCANCELED
        // was thrown. In which case, rethrow to cancel the dump gathering
    }
    EX_END_CATCH(RethrowCancelExceptions)

#if defined(DAC_MEASURE_PERF)
    unsigned __int64 nEnd = GetCycleCount();
    g_nStackTotalTime += nEnd - nStart;
    g_nStackWalk = 0;
#endif // #if defined(DAC_MEASURE_PERF)

    return status;
}

// code: ClrDataAccess::EnumMemDumpAllThreadsStack needs a trivial implementation of
// an un-DACized container class to track what exceptions have happened so far.
// It shouldn't get used anywhere else.
class DebuggingExceptionTrackerList
{
private:

    struct TrivialTADDRNode
    {
        TADDR m_exceptionAddress;
        TrivialTADDRNode * m_pNext;

        TrivialTADDRNode(TrivialTADDRNode *pNext, TADDR address)
            : m_exceptionAddress(address), m_pNext(pNext)
        {
            SUPPORTS_DAC_HOST_ONLY;
        }

    private:
        TrivialTADDRNode() { _ASSERTE(!"You should never call this ctor."); }
    };

    TrivialTADDRNode *m_pHead;

    bool Find(TADDR address)
    {
        SUPPORTS_DAC_HOST_ONLY;
        for (TrivialTADDRNode *pFind = m_pHead; pFind != NULL; pFind = pFind->m_pNext)
            if (pFind->m_exceptionAddress == address)
                return true;

        return false;
    }

public:
    DebuggingExceptionTrackerList()
        : m_pHead(NULL)
    {
        SUPPORTS_DAC_HOST_ONLY;
    }

    bool AddNewAddressOnly(TADDR address)
    {
        SUPPORTS_DAC_HOST_ONLY;
        if (Find(address))
        {
            return false;
        }
        else
        {
            TrivialTADDRNode *pNew = new TrivialTADDRNode(m_pHead, address);
            m_pHead = pNew;
            return true;
        }
    }

    ~DebuggingExceptionTrackerList()
    {
        SUPPORTS_DAC_HOST_ONLY;
        for (TrivialTADDRNode *pTemp = m_pHead; m_pHead != NULL; pTemp = m_pHead)
        {
            m_pHead = m_pHead->m_pNext;
            delete pTemp;
        }
    }
};


//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// This function will walk all threads, all the context in the
// exception state to report memory. This can also drag in memory implicitly.
// So do call
//      m_instances.DumpAllInstances(m_enumMemCb);
// when function is done.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemDumpAllThreadsStack(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

#if (defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)) && !defined(TARGET_UNIX)
    // Dump the exception object stored in the WinRT stowed exception
    EnumMemStowedException(flags);
#endif // defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)

    HRESULT     status = S_OK;
    TSIZE_T     cbMemoryReported = m_cbMemoryReported;

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    // Duplicate the enumeration code below, to allow Exception stacks to be enumerated first.
    // These exception stacks will get MethodDesc names cached to the DacStreamManager before
    // MethodDescs residing on the "regular" callstacks
    EX_TRY
    {
        DebuggingExceptionTrackerList exceptionTrackingInner;

        CLRDATA_ENUM        handle;
        ReleaseHolder<IXCLRDataTask> pIXCLRDataTask(NULL);
        ReleaseHolder<IXCLRDataExceptionState> pExcepState(NULL);
        Thread              *pThread = NULL;

        // enumerating through each thread
        StartEnumTasks(&handle);
        status = EnumTask(&handle, &pIXCLRDataTask);
        for (unsigned nbThreads = 0; status == S_OK && pIXCLRDataTask != NULL; nbThreads++)
        {
            // Avoid infinite loop if target process is corrupted.
            if (nbThreads > 100000)
            {
                break;
            }
            EX_TRY
            {
                // get Thread *
                pThread = ((ClrDataTask *)pIXCLRDataTask.GetValue())->GetThread();

                // dump the exception object
                DumpManagedExcepObject(flags, pThread->LastThrownObject());

                // Now probe into the exception info
                status = pIXCLRDataTask->GetCurrentExceptionState(&pExcepState);
                while (status == S_OK && pExcepState != NULL)
                {
                    EX_TRY
                    {
                        // touch the throwable in exception state
                        PTR_UNCHECKED_OBJECTREF throwRef(((ClrDataExceptionState *)pExcepState.GetValue())->m_throwable);

                        // If we've already attempted enumeration for this exception, it's time to quit.
                        if (!exceptionTrackingInner.AddNewAddressOnly(throwRef.GetAddr()))
                        {
                            break;
                        }

                        DumpManagedExcepObject(flags, *throwRef);
                    }
                    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

                    // get the previous exception
                    IXCLRDataExceptionState * pExcepStatePrev = NULL;
                    status = pExcepState->GetPrevious(&pExcepStatePrev);

                    // Release our current exception object, and transfer ref ownership of the previous
                    // exception object into the holder.
                    pExcepState = pExcepStatePrev;
                }
            }
            EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

            // get next thread
            pIXCLRDataTask.Clear();
            status = EnumTask(&handle, &pIXCLRDataTask);
        }
        EndEnumTasks(handle);
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    // exceptionTracking is used for exactly that; it is a per-dump list of the
    // addresses of all exceptions enumerated for this dump.  If an exception is
    // enumerated more than once it indicates that we have multiple threads pointing to
    // the same object, or the same thread has an InnerException chain with a cycle.
    // In either case, we need to terminate exception reporting.
    DebuggingExceptionTrackerList exceptionTracking;

    EX_TRY
    {
        CLRDATA_ENUM        handle;
        ReleaseHolder<IXCLRDataTask> pIXCLRDataTask(NULL);
        ReleaseHolder<IXCLRDataExceptionState> pExcepState(NULL);
        ReleaseHolder<IXCLRDataStackWalk> pStackWalk(NULL);
        Thread              *pThread = NULL;

        // enumerating through each thread's each frame, dump out some interesting
        // code memory needed to debugger to recognize frame
        //
        ThreadStore::EnumMemoryRegions(flags);

        // enumerating through each thread
        StartEnumTasks(&handle);
        status = EnumTask(&handle, &pIXCLRDataTask);
        for (unsigned nbThreads = 0; status == S_OK && pIXCLRDataTask != NULL; nbThreads++)
        {
            // Avoid infinite loop if target process is corrupted.
            if (nbThreads > 100000)
            {
                break;
            }
            EX_TRY
            {
                // get Thread *
                pThread = ((ClrDataTask *)pIXCLRDataTask.GetValue())->GetThread();

                // Write out the Thread instance
                DacEnumHostDPtrMem(pThread);

                // @TODO
                // write TEB pointed by the thread
                // DacEnumHostDPtrMem(pThread->GetTEB());

                // @TODO
                // If CLR is hosted, we want to write out fiber data

                // Dump the managed thread object
                DumpManagedObject(flags, pThread->GetExposedObjectRaw());

#ifndef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
                // dump the exception object
                DumpManagedExcepObject(flags, pThread->LastThrownObject());
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

                // Stack Walking
                // We need for the ClrDataTask::CreateStackWalk from IXCLRDataTask to work, which is the
                // following walk.  However, the CordbStackWalk code requires some different (extra) data
                // to walk the stack, such as info being present for
                // mscordacwks!DacDbiInterfaceImpl::GetNativeCodeSequencePointsAndVarInfo.
                status = pIXCLRDataTask->CreateStackWalk(CLRDATA_SIMPFRAME_UNRECOGNIZED | CLRDATA_SIMPFRAME_MANAGED_METHOD | CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE | CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                            &pStackWalk);
                if (status == S_OK && pStackWalk != NULL)
                {
                    status = EnumMemWalkStackHelper(flags, pStackWalk, pThread);
                    pStackWalk.Clear();
                }

                // Now probe into the exception info
                status = pIXCLRDataTask->GetCurrentExceptionState(&pExcepState);
                while (status == S_OK && pExcepState != NULL)
                {
                    EX_TRY
                    {
                        // touch the throwable in exception state
                        PTR_UNCHECKED_OBJECTREF throwRef(((ClrDataExceptionState *)pExcepState.GetValue())->m_throwable);

                        // If we've already attempted enumeration for this exception, it's time to quit.
                        if (!exceptionTracking.AddNewAddressOnly(throwRef.GetAddr()))
                        {
                            break;
                        }

#ifndef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
                        DumpManagedExcepObject(flags, *throwRef);
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

                        // get the type of the exception
                        ReleaseHolder<IXCLRDataValue> pValue(NULL);
                        status = pExcepState->GetManagedObject(&pValue);
                        if (status == S_OK && pValue != NULL)
                        {
                            ReleaseHolder<IXCLRDataTypeInstance> pTypeInstance(NULL);
                            // Make sure that we can get back a TypeInstance during inspection
                            status = pValue->GetType(&pTypeInstance);
                            pValue.Clear();
                        }

                        // If Exception state has a new context, we will walk with the stashed context as well.
                        // Note that in stack overflow exception's case, m_pContext is null.
                        //
                        // It is possible that we are in exception's catch clause when we
                        // try to walk the stack below. This is a very weird situation where
                        // stack is logically unwind and not physically unwind. We may not be able
                        // to walk the stack correctly here. Anyway, we try to catch exception thrown
                        // by bad stack walk in EnumMemWalkStackHelper.
                        //
                        PTR_CONTEXT pContext = ((ClrDataExceptionState*)pExcepState.GetValue())->GetCurrentContextRecord();
                        if (pContext != NULL)
                        {
                            T_CONTEXT newContext;
                            newContext = *pContext;

                            // We need to trigger stack walk again using the exception's context!
                            status = pIXCLRDataTask->CreateStackWalk(CLRDATA_SIMPFRAME_UNRECOGNIZED | CLRDATA_SIMPFRAME_MANAGED_METHOD | CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE | CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                                        &pStackWalk);
                            if (status == S_OK && pStackWalk != NULL)
                            {
                                status = pStackWalk->SetContext2(CLRDATA_STACK_SET_CURRENT_CONTEXT, sizeof(T_CONTEXT), (BYTE *) &newContext);
                                if (status == S_OK)
                                {
                                    status = EnumMemWalkStackHelper(flags, pStackWalk, pThread);
                                }
                                pStackWalk.Clear();
                            }
                        }
                    }
                    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

                    // get the previous exception
                    IXCLRDataExceptionState * pExcepStatePrev = NULL;
                    status = pExcepState->GetPrevious(&pExcepStatePrev);

                    // Release our current exception object, and transfer ref ownership of the previous
                    // exception object into the holder.
                    pExcepState = pExcepStatePrev;
                }
            }
            EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

            // get next thread
            pIXCLRDataTask.Clear();
            status = EnumTask(&handle, &pIXCLRDataTask);
        }
        EndEnumTasks(handle);
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

    // updating the statistics
    m_dumpStats.m_cbStack = m_cbMemoryReported - cbMemoryReported;

    return status;
}


#if (defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)) && !defined(TARGET_UNIX)
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// WinRT stowed exception holds the (CCW)pointer to a managed exception object.
// We should check for the presence of a such an exception object and dump it if available.
// This can also drag in memory implicitly.
// So do call
//      m_instances.DumpAllInstances(m_enumMemCb);
// when function is done.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemStowedException(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    ICLRDataTarget3 *pTarget3 = GetLegacyTarget3();
    if (pTarget3 == NULL)
        return S_OK;

    // get the thread that raised the exception
    ULONG32 exThreadID = 0;
    if (FAILED(pTarget3->GetExceptionThreadID(&exThreadID)) || exThreadID == 0)
        return S_OK;

    //
    // check that the thread is one of the known managed threads
    //
    BOOL foundThread = FALSE;
    CLRDATA_ENUM handle;
    ReleaseHolder<IXCLRDataTask> pIXCLRDataTask(NULL);

    // enumerate through each thread
    StartEnumTasks(&handle);
    HRESULT status = EnumTask(&handle, &pIXCLRDataTask);
    for (unsigned nbThreads = 0; status == S_OK && pIXCLRDataTask != NULL; ++nbThreads)
    {
        // Avoid infinite loop if target process is corrupted.
        if (nbThreads > 100000)
        {
            break;
        }
        EX_TRY
        {
            if (((ClrDataTask *)pIXCLRDataTask.GetValue())->GetThread()->GetOSThreadId() == exThreadID)
            {
                // found the thread
                foundThread = TRUE;
                break;
            }
        }
        EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

        // get next thread
        pIXCLRDataTask.Clear();
        status = EnumTask(&handle, &pIXCLRDataTask);
    }
    EndEnumTasks(handle);

    if (!foundThread)
        return S_OK;


    //
    // Read the remote stowed exceptions.
    //
    // EXCEPTION_RECORD.ExceptionCode: STATUS_STOWED_EXCEPTION.
    // EXCEPTION_RECORD.NumberParameters: 2.
    // EXCEPTION_RECORD.ExceptionInformation[0]: pointer to an array of pointers
    //  to STOWED_EXCEPTION_INFORMATION structures.
    // EXCEPTION_RECORD.ExceptionInformation[1]: count of elements in the array.
    //
    ULONG32 bytesRead = 0;
    MINIDUMP_EXCEPTION minidumpException = { 0 };
    if (FAILED(pTarget3->GetExceptionRecord(sizeof(MINIDUMP_EXCEPTION), &bytesRead, (PBYTE)&minidumpException)))
        return S_OK;

    TADDR remoteStowedExceptionArray = (TADDR)minidumpException.ExceptionInformation[0];
    ULONG stowedExceptionCount = (ULONG)minidumpException.ExceptionInformation[1];
    if (bytesRead != sizeof(MINIDUMP_EXCEPTION)
        || minidumpException.ExceptionCode != STATUS_STOWED_EXCEPTION
        || minidumpException.NumberParameters != 2
        || stowedExceptionCount < 1             // there must atleast be 1 stowed exception
        || stowedExceptionCount > 256           // upper bound: 256
        || remoteStowedExceptionArray == NULL)
    {
        return S_OK;
    }

    // Make sure we include the whole stowed exception array so we can debug a stowed exception
    // in a minidump
    ReportMem(remoteStowedExceptionArray, stowedExceptionCount * sizeof(TADDR));

    for (ULONG i = 0; i < stowedExceptionCount; ++i)
    {
        // Read the i-th stowed exception
        TADDR remoteStowedException = NULL;
        if (FAILED(m_pTarget->ReadVirtual(TO_CDADDR(remoteStowedExceptionArray + (i * sizeof(TADDR))),
            (PBYTE)&remoteStowedException, sizeof(TADDR), &bytesRead))
            || bytesRead != sizeof(TADDR)
            || remoteStowedException == NULL)
        {
            continue;
        }

        ReportMem(remoteStowedException, sizeof(STOWED_EXCEPTION_INFORMATION_HEADER));

        // check if this is a v2 stowed exception
        STOWED_EXCEPTION_INFORMATION_V2 stowedException = { {0} };
        if (FAILED(m_pTarget->ReadVirtual(TO_CDADDR(remoteStowedException),
            (PBYTE)&stowedException, sizeof(STOWED_EXCEPTION_INFORMATION_HEADER), &bytesRead))
            || bytesRead != sizeof(STOWED_EXCEPTION_INFORMATION_HEADER)
            || stowedException.Header.Signature != STOWED_EXCEPTION_INFORMATION_V2_SIGNATURE)
        {
            continue;
        }

        ReportMem(remoteStowedException, sizeof(STOWED_EXCEPTION_INFORMATION_V2));

        // Read the full v2 stowed exception and get the CCW pointer out of it
        if (FAILED(m_pTarget->ReadVirtual(TO_CDADDR(remoteStowedException),
            (PBYTE)&stowedException, sizeof(STOWED_EXCEPTION_INFORMATION_V2), &bytesRead))
            || bytesRead != sizeof(STOWED_EXCEPTION_INFORMATION_V2)
            || stowedException.NestedExceptionType != STOWED_EXCEPTION_NESTED_TYPE_LEO
            || stowedException.NestedException == NULL)
        {
            continue;
        }

        // Find out if NestedException is a pointer to CCW and then dump the exception object in it
        DumpStowedExceptionObject(flags, TO_CDADDR(stowedException.NestedException));
    }

    return S_OK;
}

HRESULT ClrDataAccess::DumpStowedExceptionObject(CLRDataEnumMemoryFlags flags, CLRDATA_ADDRESS ccwPtr)
{
    SUPPORTS_DAC;
    if (ccwPtr == NULL)
        return S_OK;

    OBJECTREF managedExceptionObject = NULL;

#ifdef FEATURE_COMWRAPPERS
    OBJECTREF wrappedObjAddress;
    if (DACTryGetComWrappersObjectFromCCW(ccwPtr, &wrappedObjAddress) == S_OK)
    {
        managedExceptionObject = wrappedObjAddress;
        // Now report the CCW itself
        ReportMem(TO_TADDR(ccwPtr), sizeof(TADDR));
        TADDR managedObjectWrapperPtrPtr = ccwPtr & InteropLib::ABI::DispatchThisPtrMask;
        ReportMem(managedObjectWrapperPtrPtr, sizeof(TADDR));

        // Plus its QI and VTable that we query to determine if it is a ComWrappers CCW
        TADDR vTableAddress = NULL;
        TADDR qiAddress = NULL;
        DACGetComWrappersCCWVTableQIAddress(ccwPtr, &vTableAddress, &qiAddress);
        ReportMem(vTableAddress, sizeof(TADDR));
        ReportMem(qiAddress, sizeof(TADDR));

        // And the MOW it points to
        TADDR mow = DACGetManagedObjectWrapperFromCCW(ccwPtr);
        ReportMem(mow, sizeof(InteropLib::ABI::ManagedObjectWrapperLayout));
    }
#endif
#ifdef FEATURE_COMINTEROP
    if (managedExceptionObject == NULL)
    {
        // dump the managed exception object wrapped in CCW
        // memory of the CCW object itself is dumped later by DacInstanceManager::DumpAllInstances
        DacpCCWData ccwData;
        GetCCWData(ccwPtr, &ccwData);   // this call collects some memory implicitly
        managedExceptionObject = OBJECTREF(CLRDATA_ADDRESS_TO_TADDR(ccwData.managedObject));
    }
#endif
    DumpManagedExcepObject(flags, managedExceptionObject);

    // dump memory of the 2nd slot in the CCW's vtable
    // this is used in DACGetCCWFromAddress to identify if the passed in pointer is a valid CCW.
    ULONG32 bytesRead = 0;
    TADDR vTableAddress = NULL;
    if (FAILED(m_pTarget->ReadVirtual(ccwPtr, (PBYTE)&vTableAddress, sizeof(TADDR), &bytesRead))
        || bytesRead != sizeof (TADDR)
        || vTableAddress == NULL)
    {
        return S_OK;
    }

    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED
    (
        ReportMem(vTableAddress, sizeof(TADDR)); // Report the QI slot on the vtable for ComWrappers
        ReportMem(vTableAddress + sizeof(PBYTE) * TEAR_OFF_SLOT, sizeof(TADDR)); // Report the AddRef slot on the vtable for built-in CCWs
    );

    return S_OK;
}
#endif

#define IMAGE_DIRECTORY_ENTRY_RESOURCE        2   // Resource Directory
#define IMAGE_DIRECTORY_ENTRY_DEBUG           6   // Debug Directory

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Reports critical data from the CLR main module
// that needs to be present in all minidumps.
// Implicitly reports memory, so remember to call
// m_instances.DumpAllInstances(m_enumMemCb);
// after this function completes.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemCLRMainModuleInfo()
{
    SUPPORTS_DAC;

    HRESULT status = S_OK;

    // PEDecoder is DACized, so we just need to touch what we want to
    // make subsequent lookup work.
    PEDecoder pe(m_globalBase);

    // We currently only actually have one debug directory entry.
    // Post-processing, such as optimization, may add an extra directory.
    // These directories are of type IMAGE_DEBUG_TYPE_RESERVED10, while our
    // standard CodeView directory with pdb info is IMAGE_DEBUG_TYPE_CODEVIEW.
    UINT i;
    for (i = 0; pe.GetDebugDirectoryEntry(i); i++)
    {
    }

    if (i < 1)
    {
        status = E_UNEXPECTED;
        _ASSERTE(!"Collecting dump of target with no debug directory entries!");
    }

    // For CLRv4+, the resource directory contains the necessary info
    // to retrieve the DBI/DAC from a symbol server.
    // Specifically, in v4 it contains a mscoree!PE_FIXEDFILEINFO.
    // This is also required since OpenVirtualProcess will check against
    // this content to determine if a target module is indeed a CLR
    // main module.

    // Retrieve all resources in clr.dll.  Right now, the entire resource
    // content is very small (~0x600 bytes of raw data), so getting all is
    // the easy thing to do.  If resources become larger in later
    // releases, we'll have to specifically get just the debugging-related resources.
    _ASSERTE(pe.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_RESOURCE));
    if (pe.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_RESOURCE))
    {
        COUNT_T size = 0;
        TADDR pResourceDirData = pe.GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_RESOURCE, &size);

        _ASSERTE(size < 0x2000);
        ReportMem((TADDR)pResourceDirData, size, true);
    }
    else
    {
        // In later releases, we should log the ERROR_RESOURCE_DATA_NOT_FOUND.
        status = E_UNEXPECTED;
    }

    if (pe.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_EXPORT))
    {
        COUNT_T size = 0;
        TADDR pExportTablesOffset = pe.GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_EXPORT, &size);

        ReportMem(pExportTablesOffset, size, true);

        PTR_VOID runtimeExport = pe.GetExport(RUNTIME_INFO_SIGNATURE);
        const char *runtimeExportSignature = dac_cast<PTR_STR>(runtimeExport);
        if (runtimeExport != NULL &&
            strcmp(runtimeExportSignature, RUNTIME_INFO_SIGNATURE) == 0)
        {
            ReportMem(dac_cast<TADDR>(runtimeExport), sizeof(RuntimeInfo), true);
        }
    }
    else
    {
        // We always expect (attach state, metrics and such on windows, and dac table on mac and linux).
        return E_UNEXPECTED;
    }

    return status;
}


//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Generating skinny mini-dump. Skinny mini-dump will only support stack trace, module list,
// and Exception list viewing.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemoryRegionsWorkerSkinny(IN CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    HRESULT status = S_OK;

    // clear all of the previous cached memory
    Flush();

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    // Enable caching enumerated metadata of interest
    InitStreamsForWriting(flags);
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    //TODO: actually *do* something with potential failures.  It would be relatively easy to
    // hook up an official dump stream to put info on our failures and other 'metadata'
    // about dumping into in a generic sort of way.  Our code doesn't have access to
    // MDWD's callbacks, so we can't just do it ourselves.  Thus we could have useful info
    // baked into the dump, like we failed to enumerate mem for certain threads, etc.

    // Each enumeration function below should be wrapped in a try/catch
    // so that we have a chance to create a debuggable dump in the face of target problems.

    // Iterating to all threads' stacks
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAllThreadsStack(flags); )

    // Iterating to module list.
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpModuleList(flags); )

    //
    // iterating through static that we care
    //
    // collect CLR static
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRStatic(flags); )

    // Dump AppDomain-specific info needed for MiniDumpNormal.
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAppDomainInfo(flags); )

    // Dump the Debugger object data needed
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pDebugger->EnumMemoryRegions(flags); )

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    // Dump the extra data needed for metadata-free debugging
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( EnumStreams(flags); )
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    // now dump the memory get dragged in by using DAC API implicitly.
    m_dumpStats.m_cbImplicity = m_instances.DumpAllInstances(m_enumMemCb);

    // Do not let any remaining implicitly enumerated memory leak out.
    Flush();

    return S_OK;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Generating triage micro-dump. Triage dumps will only support stack trace
// and Exception viewing.More than that triage dumps have to be PII free,
// so all exception messages have to be poisoned with 0xcc mask.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemoryRegionsWorkerMicroTriage(IN CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    HRESULT status = S_OK;

    // clear all of the previous cached memory
    Flush();

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    // Enable caching enumerated metadata of interest
    InitStreamsForWriting(flags);
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    // Iterating to all threads' stacks
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAllThreadsStack(flags); )

    // Iterating to module list.
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpModuleList(flags); )

    // collect CLR static
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRStatic(flags); )

    // Dump AppDomain-specific info needed for triage dumps methods enumeration (k command).
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAppDomainInfo(flags); )

    // Dump the Debugger object data needed
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( g_pDebugger->EnumMemoryRegions(flags); )

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
    // Dump the extra data needed for metadata-free debugging
    CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( EnumStreams(flags); )
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

    // now dump the memory get dragged in by using DAC API implicitly.
    m_dumpStats.m_cbImplicity = m_instances.DumpAllInstances(m_enumMemCb);

    // Do not let any remaining implicitly enumerated memory leak out.
    Flush();

    return S_OK;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Write out mscorwks's data segment. This will write out the whole
// data segment for mscorwks. It is about 200 or 300K. Most of it (90%) are
// vtable definition that we don't really care. But we don't have a
// good walk to just write out all globals and statics.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemWriteDataSegment()
{
    SUPPORTS_DAC;

    NewHolder<PEDecoder> pedecoder(NULL);

    EX_TRY
    {
        // Collecting mscorwks's data segment
        {
            // m_globalBase is the base address of target process's mscorwks module
            pedecoder = new PEDecoder(dac_cast<PTR_VOID>(m_globalBase));

            PTR_IMAGE_SECTION_HEADER pSection = (PTR_IMAGE_SECTION_HEADER) pedecoder->FindFirstSection();
            PTR_IMAGE_SECTION_HEADER pSectionEnd = pSection + VAL16(pedecoder->GetNumberOfSections());

            while (pSection < pSectionEnd)
            {
                if (pSection->Name[0] == '.' &&
                    pSection->Name[1] == 'd' &&
                    pSection->Name[2] == 'a' &&
                    pSection->Name[3] == 't' &&
                    pSection->Name[4] == 'a')
                {
                    // This is the .data section of mscorwks
                    ReportMem(m_globalBase + pSection->VirtualAddress, pSection->Misc.VirtualSize);
                }
                pSection++;
             }
        }
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

    return S_OK;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Custom Dump. Depending on the value of g_ECustomDumpFlavor, different dump
// will be taken. You can set this global variable using hosting API
// ICLRErrorReportingManager::BeginCustomDump.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemoryRegionsWorkerCustom()
{
    SUPPORTS_DAC;

    HRESULT status = S_OK;

    ECustomDumpFlavor eFlavor;

    eFlavor = DUMP_FLAVOR_Default;

    m_enumMemFlags = CLRDATA_ENUM_MEM_MINI;

    // clear all of the previous cached memory
    Flush();

    if (eFlavor == DUMP_FLAVOR_Mini)
    {
        // Iterating to all threads' stacks
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAllThreadsStack(m_enumMemFlags); )

        // Iterating to module list.
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpModuleList(m_enumMemFlags); )

        //
        // iterating through static that we care
        //
        // collect CLR static
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRStatic(m_enumMemFlags); )

        // we are done...

        // now dump the memory get dragged in implicitly
        m_dumpStats.m_cbImplicity = m_instances.DumpAllInstances(m_enumMemCb);

    }
    else if (eFlavor == DUMP_FLAVOR_CriticalCLRState)
    {
        // We need to walk Threads stack to view managed frames.
        // Iterating through module list

        // Iterating to all threads' stacks
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpAllThreadsStack(m_enumMemFlags); )

        // Iterating to module list.
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemDumpModuleList(m_enumMemFlags); )

        //
        // iterating through static that we care
        //
        // collect CLR static
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRStatic(m_enumMemFlags); )

        // Collecting some CLR secondary critical data
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRHeapCrticalStatic(m_enumMemFlags); )
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemWriteDataSegment(); )

        // we are done...

        // now dump the memory get dragged in implicitly
        m_dumpStats.m_cbImplicity = m_instances.DumpAllInstances(m_enumMemCb);

    }
    else if (eFlavor == DUMP_FLAVOR_NonHeapCLRState)
    {
        // since all CLR hosted heap will be dump by the host,
        // the EE structures that are not loaded using LoadLibrary will
        // be included by the host.
        //
        // Thus we only need to include mscorwks's critical data and ngen images

        m_enumMemFlags = CLRDATA_ENUM_MEM_HEAP;

        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRStatic(m_enumMemFlags); )

        // Collecting some CLR secondary critical data
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCLRHeapCrticalStatic(m_enumMemFlags); )

        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemWriteDataSegment(); )
        CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED( status = EnumMemCollectImages(); )
    }
    else
    {
        status = E_INVALIDARG;
    }

    return S_OK;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Minidumps traverse a giant static calltree. We already try to catch
// exceptions at various lower level places and continue to report memory.
//
// However, if we'll jump to the top-level catcher and skip the rest of the tree,
// that may mean some key data may not get emitted to the minidump.
// In the case that a user requests a dump is canceled, we should skip the rest
// of the tree. When a COR_E_OPERATIONCANCELED exception is thrown, is allowed to
// escape all the way to this function. If any exception makes it here and is not
// COR_E_OPERATIONCANCELED that indicates an issue, and the assert is meant to catch that.
// Unfortunately the stack unwind will already have happened.
//
// Internal API to support minidump and heap dump. It just delegate
// to proper function but with a top level catch.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
HRESULT ClrDataAccess::EnumMemoryRegionsWrapper(IN CLRDataEnumMemoryFlags flags)
{
    // This is infrastructure code - we don't want DacCop complaining about the calls as a result
    // of the use of EX_CATCH_HRESULT here.  We're careful to mark EnumMemoryRegionsWorkerSkinny
    // and EnumMemoryRegionsWorkerHeap as just SUPPORTS_DAC so that we still get analysis.
    SUPPORTS_DAC_HOST_ONLY;

    HRESULT status = S_OK;
    m_enumMemFlags = flags;
    EX_TRY
    {
        // The various EnumMemoryRegions() implementations should understand
        // CLRDATA_ENUM_MEM_MINI to mean that the bare minimimum memory
        // to make a MiniDumpNormal work should be included.
        if (flags == CLRDATA_ENUM_MEM_MINI)
        {
            // skinny mini-dump
            status = EnumMemoryRegionsWorkerSkinny(flags);
        }
        else if (flags == CLRDATA_ENUM_MEM_TRIAGE)
        {
            // triage micro-dump
            status = EnumMemoryRegionsWorkerMicroTriage(flags);
        }
        else if (flags == CLRDATA_ENUM_MEM_HEAP)
        {
            status = EnumMemoryRegionsWorkerHeap(flags);
        }
        else
        {
            _ASSERTE(!"Bad flags passing to EnumMemoryRegionsWrapper!");
        }
    }
    EX_CATCH_HRESULT(status);

    // The only exception that should reach here is the cancel exception
    _ASSERTE(SUCCEEDED(status) || status == COR_E_OPERATIONCANCELED);

    return status;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Entry function for generating CLR aware dump. This function is called
// for minidump, heap dump, and custom dumps. CLR specific memory will
// be reported to outer level dumper (usually dbghelp's MiniDumpWriteDump api)
// through the callback. We do not write out to file directly.
//
// N.B.: The CLR may report duplicate memory chunks and it's up to
// the debugger to coalesce memory.  *However* the debugger's current
// implementation coalesces memory we enumerate and memory that
// they enumerate; the two sets of memory are not guaranteed to be
// coalesced.  The dump produced may thus have memory blocks in the
// MemoryListStream that overlap or are totally contained in other blocks.
// This issue was resolved by-design by dbgteam.  Win7 #407019.
// Note also that Memory64ListStream (when passing MiniDumpWithFullMemory)
// will have no duplicates, be sorted, etc.  In that case, none of
// our code is called.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
STDMETHODIMP
ClrDataAccess::EnumMemoryRegions(IN ICLRDataEnumMemoryRegionsCallback* callback,
                                 IN ULONG32 miniDumpFlags,
                                 IN CLRDataEnumMemoryFlags flags)    // reserved not used
{
    SUPPORTS_DAC;
    HRESULT status;

#if defined(DAC_MEASURE_PERF)

    g_nTotalTime = 0;
    g_nStackTotalTime = 0;
    g_nReadVirtualTotalTime = 0;
    g_nFindTotalTime = 0;
    g_nFindHashTotalTime = 0;
    g_nFindHits = 0;
    g_nFindCalls = 0;
    g_nFindFails = 0;
    g_nStackWalk = 0;
    g_nFindStackTotalTime = 0;

    LARGE_INTEGER nClockFrequency;
    unsigned __int64 nStart = 0;
    unsigned __int64 nEnd = 0;

    QueryPerformanceFrequency(&nClockFrequency);

    FILE* fp = fopen("c:\\dumpLog.txt", "a");
    if (fp)
    {
        fprintf(fp, "\nMinidumpFlags = %d\n", miniDumpFlags);
        fclose(fp);
    }

    nStart = GetCycleCount();

#endif // #if defined(DAC_MEASURE_PERF)

    DAC_ENTER();

    // We should not be trying to enumerate while we have an enumeration outstanding
    _ASSERTE(m_enumMemCb==NULL);
    m_enumMemCb = callback;

    // QI for ICLRDataEnumMemoryRegionsCallback2 will succeed only for Win8+.
    // It is expected to fail on pre Win8 OSes.
    callback->QueryInterface(IID_ICLRDataEnumMemoryRegionsCallback2, (void **)&m_updateMemCb);

    EX_TRY
    {
        ClearDumpStats();
        if (miniDumpFlags & MiniDumpWithPrivateReadWriteMemory)
        {
            // heap dump
            status = EnumMemoryRegionsWrapper(CLRDATA_ENUM_MEM_HEAP);
        }
        else if (miniDumpFlags & MiniDumpWithFullAuxiliaryState)
        {
            // This is the host custom dump.
            status = EnumMemoryRegionsWorkerCustom();
        }
        else if (miniDumpFlags & MiniDumpFilterTriage)
        {
            // triage micro-dump
            status = EnumMemoryRegionsWrapper(CLRDATA_ENUM_MEM_TRIAGE);
        }
        else
        {
            // minidump
            status = EnumMemoryRegionsWrapper(CLRDATA_ENUM_MEM_MINI);
        }

#ifndef TARGET_UNIX
        // For all dump types, we need to capture the chain to the IMAGE_DIRECTORY_ENTRY_DEBUG
        // contents, so that DAC can validate against the TimeDateStamp even if the
        // debugger can't find the main CLR module on disk.
        // If we already failed, don't bother.
        if (SUCCEEDED(status))
        {
            // In case there's implicitly enumerated memory hanging around
            // let's not accidentally pick it up.
            Flush();
            if (SUCCEEDED(status = EnumMemCLRMainModuleInfo()))
            {
                m_instances.DumpAllInstances(m_enumMemCb);
            }
        }
#endif
        Flush();
    }
    EX_CATCH
    {
        m_enumMemCb = NULL;

        // We should never actually be here b/c none of the EMR functions should throw.
        // They should all either be written robustly w/ ptr.IsValid() and catching their
        // own exceptions.
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            _ASSERTE_MSG(false, "Got unexpected exception in EnumMemoryRegions");
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    // fix for issue 866100: DAC is too late in releasing ICLRDataEnumMemoryRegionsCallback2*
    if (m_updateMemCb)
    {
        m_updateMemCb->Release();
        m_updateMemCb = NULL;
    }
    m_enumMemCb = NULL;

    DAC_LEAVE();

#if defined(DAC_MEASURE_PERF)

    nEnd = GetCycleCount();
    g_nTotalTime= nEnd - nStart;
    fp = fopen("c:\\dumpLog.txt", "a");
    fprintf(fp, "Total = %g msec\n"
               "ReadVirtual = %g msec\n"
               "StackWalk = %g msec; Find: %g msec\n"
               "Find = %g msec; Hash = %g msec; Calls = %I64u; Hits = %I64u; Not found = %I64u\n\n=====\n",
               (float) (1000*g_nTotalTime/nClockFrequency.QuadPart),
               (float) (1000*g_nReadVirtualTotalTime/nClockFrequency.QuadPart),
               (float) (1000*g_nStackTotalTime/nClockFrequency.QuadPart), (float) (1000*g_nFindStackTotalTime/nClockFrequency.QuadPart),
               (float) (1000*g_nFindTotalTime/nClockFrequency.QuadPart), (float) (1000*g_nFindHashTotalTime/nClockFrequency.QuadPart),
               g_nFindCalls, g_nFindHits, g_nFindFails
               );
    fclose(fp);

#endif // #if defined(DAC_MEASURE_PERF)

    return status;
}


//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Clear the statistics for the dump. For each dump generation, we
// clear the dump statistics. At the end of the dump generation, you can
// view the statics data member m_dumpStats and see how many bytes that
// we have reported to our debugger host.
//
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
void ClrDataAccess::ClearDumpStats()
{
    SUPPORTS_DAC;

    m_cbMemoryReported = 0;
    memset(&m_dumpStats, 0, sizeof(DumpMemoryReportStatics));
}
