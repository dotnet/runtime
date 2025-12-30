// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

/*============================================================
**
** File:  COMUtilNative
**
**
**
** Purpose: A dumping ground for classes which aren't large
** enough to get their own file in the EE.
**
**
**
===========================================================*/
#include "common.h"
#include "object.h"
#include "excep.h"
#include "vars.hpp"
#include "comutilnative.h"

#include "utilcode.h"
#include "frames.h"
#include "field.h"
#include "winwrap.h"
#include "gcheaputilities.h"
#include "fcall.h"
#include "invokeutil.h"
#include "eeconfig.h"
#include "typestring.h"
#include "finalizerthread.h"
#include "threadsuspend.h"
#include <minipal/memorybarrierprocesswide.h>
#include "string.h"
#include "sstring.h"
#include "array.h"
#include "eepolicy.h"
#include <minipal/cpuid.h>

#ifdef FEATURE_COMINTEROP
    #include "comcallablewrapper.h"
    #include "comcache.h"
#endif // FEATURE_COMINTEROP

#include "arraynative.inl"

//
//
// EXCEPTION NATIVE
//
//
FCIMPL1(FC_BOOL_RET, ExceptionNative::IsImmutableAgileException, Object* pExceptionUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pExceptionUNSAFE != NULL);

    OBJECTREF pException = (OBJECTREF) pExceptionUNSAFE;

    // The preallocated exception objects may be used from multiple AppDomains
    // and therefore must remain immutable from the application's perspective.
    FC_RETURN_BOOL(CLRException::IsPreallocatedExceptionObject(pException));
}
FCIMPLEND

// This FCall sets a flag against the thread exception state to indicate to
// IL_Throw and the StackTraceInfo implementation to account for the fact
// that we have restored a foreign exception dispatch details.
//
// Refer to the respective methods for details on how they use this flag.
FCIMPL0(VOID, ExceptionNative::PrepareForForeignExceptionRaise)
{
    FCALL_CONTRACT;

    PTR_ThreadExceptionState pCurTES = GetThread()->GetExceptionState();

    // Set a flag against the TES to indicate this is a foreign exception raise.
    pCurTES->SetRaisingForeignException();
}
FCIMPLEND

// Given an exception object, this method will mark its stack trace as frozen and return it to the caller.
// Frozen stack traces are immutable, when a thread attempts to add a frame to it, the stack trace is cloned first.
extern "C" void QCALLTYPE ExceptionNative_GetFrozenStackTrace(QCall::ObjectHandleOnStack exception, QCall::ObjectHandleOnStack ret)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    _ASSERTE(exception.Get() != NULL);

    struct
    {
        StackTraceArray stackTrace;
        EXCEPTIONREF refException = NULL;
        PTRARRAYREF keepAliveArray = NULL; // Object array of Managed Resolvers / AssemblyLoadContexts
    } gc;
    GCPROTECT_BEGIN(gc);

    // Get the exception object reference
    gc.refException = (EXCEPTIONREF)exception.Get();

    gc.refException->GetStackTrace(gc.stackTrace, &gc.keepAliveArray);

    gc.stackTrace.MarkAsFrozen();

    if (gc.keepAliveArray != NULL)
    {
        ret.Set(gc.keepAliveArray);
    }
    else
    {
        ret.Set(gc.stackTrace.Get());
    }
    GCPROTECT_END();

    END_QCALL;
}

#ifdef FEATURE_COMINTEROP

static BSTR BStrFromString(STRINGREF s)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    WCHAR *wz;
    int cch;
    BSTR bstr;

    if (s == NULL)
        return NULL;

    s->RefInterpretGetStringValuesDangerousForGC(&wz, &cch);

    bstr = SysAllocString(wz);
    if (bstr == NULL)
        COMPlusThrowOM();

    return bstr;
}

static BSTR GetExceptionDescription(OBJECTREF objException)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION( IsException(objException->GetMethodTable()) );
    }
    CONTRACTL_END;

    BSTR bstrDescription;

    STRINGREF MessageString = NULL;
    GCPROTECT_BEGIN(MessageString)
    GCPROTECT_BEGIN(objException)
    {
        // read Exception.Message property
        MethodDescCallSite getMessage(METHOD__EXCEPTION__GET_MESSAGE, &objException);

        ARG_SLOT GetMessageArgs[] = { ObjToArgSlot(objException)};
        MessageString = getMessage.Call_RetSTRINGREF(GetMessageArgs);

        // if the message string is empty then use the exception classname.
        if (MessageString == NULL || MessageString->GetStringLength() == 0) {
            // call GetClassName
            MethodDescCallSite getClassName(METHOD__EXCEPTION__GET_CLASS_NAME, &objException);
            ARG_SLOT GetClassNameArgs[] = { ObjToArgSlot(objException)};
            MessageString = getClassName.Call_RetSTRINGREF(GetClassNameArgs);
            _ASSERTE(MessageString != NULL && MessageString->GetStringLength() != 0);
        }

        // Allocate the description BSTR.
        int DescriptionLen = MessageString->GetStringLength();
        bstrDescription = SysAllocStringLen(MessageString->GetBuffer(), DescriptionLen);
    }
    GCPROTECT_END();
    GCPROTECT_END();

    return bstrDescription;
}

static BSTR GetExceptionSource(OBJECTREF objException)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION( IsException(objException->GetMethodTable()) );
    }
    CONTRACTL_END;

    STRINGREF refRetVal;
    GCPROTECT_BEGIN(objException)

    // read Exception.Source property
    MethodDescCallSite getSource(METHOD__EXCEPTION__GET_SOURCE, &objException);

    ARG_SLOT GetSourceArgs[] = { ObjToArgSlot(objException)};

    refRetVal = getSource.Call_RetSTRINGREF(GetSourceArgs);

    GCPROTECT_END();
    return BStrFromString(refRetVal);
}

static void GetExceptionHelp(OBJECTREF objException, BSTR *pbstrHelpFile, DWORD *pdwHelpContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(IsException(objException->GetMethodTable()));
        PRECONDITION(CheckPointer(pbstrHelpFile));
        PRECONDITION(CheckPointer(pdwHelpContext));
    }
    CONTRACTL_END;

    *pdwHelpContext = 0;

    GCPROTECT_BEGIN(objException);

    // call managed code to parse help context
    MethodDescCallSite getHelpContext(METHOD__EXCEPTION__GET_HELP_CONTEXT, &objException);

    ARG_SLOT GetHelpContextArgs[] =
    {
        ObjToArgSlot(objException),
        PtrToArgSlot(pdwHelpContext)
    };
    *pbstrHelpFile = BStrFromString(getHelpContext.Call_RetSTRINGREF(GetHelpContextArgs));

    GCPROTECT_END();
}

// NOTE: caller cleans up any partially initialized BSTRs in pED
void ExceptionNative::GetExceptionData(OBJECTREF objException, ExceptionData *pED)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsException(objException->GetMethodTable()));
        PRECONDITION(CheckPointer(pED));
    }
    CONTRACTL_END;

    ZeroMemory(pED, sizeof(ExceptionData));

    GCPROTECT_BEGIN(objException);
    pED->hr = GetExceptionHResult(objException);
    pED->bstrDescription = GetExceptionDescription(objException);
    pED->bstrSource = GetExceptionSource(objException);
    GetExceptionHelp(objException, &pED->bstrHelpFile, &pED->dwHelpContext);
    GCPROTECT_END();
    return;
}

HRESULT SimpleComCallWrapper::IErrorInfo_hr()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionHResult(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrDescription()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionDescription(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrSource()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionSource(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrHelpFile()
{
    WRAPPER_NO_CONTRACT;
    BSTR  bstrHelpFile;
    DWORD dwHelpContext;
    GetExceptionHelp(this->GetObjectRef(), &bstrHelpFile, &dwHelpContext);
    return bstrHelpFile;
}

DWORD SimpleComCallWrapper::IErrorInfo_dwHelpContext()
{
    WRAPPER_NO_CONTRACT;
    BSTR  bstrHelpFile;
    DWORD dwHelpContext;
    GetExceptionHelp(this->GetObjectRef(), &bstrHelpFile, &dwHelpContext);
    SysFreeString(bstrHelpFile);
    return dwHelpContext;
}

GUID SimpleComCallWrapper::IErrorInfo_guid()
{
    LIMITED_METHOD_CONTRACT;
    return GUID_NULL;
}

#endif // FEATURE_COMINTEROP

FCIMPL0(EXCEPTION_POINTERS*, ExceptionNative::GetExceptionPointers)
{
    FCALL_CONTRACT;

    EXCEPTION_POINTERS* retVal = NULL;

    Thread *pThread = GetThread();
    if (pThread->IsExceptionInProgress())
    {
        retVal = pThread->GetExceptionState()->GetExceptionPointers();
    }

    return retVal;
}
FCIMPLEND

FCIMPL0(INT32, ExceptionNative::GetExceptionCode)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;

    Thread *pThread = GetThread();
    if (pThread->IsExceptionInProgress())
    {
        retVal = pThread->GetExceptionState()->GetExceptionCode();
    }

    return retVal;
}
FCIMPLEND

extern uint32_t g_exceptionCount;
FCIMPL0(UINT32, ExceptionNative::GetExceptionCount)
{
    FCALL_CONTRACT;
    return g_exceptionCount;
}
FCIMPLEND


//
// This must be implemented as an FCALL because managed code cannot
// swallow a thread abort exception without resetting the abort,
// which we don't want to do.  Additionally, we can run into deadlocks
// if we use the ResourceManager to do resource lookups - it requires
// taking managed locks when initializing Globalization & Security,
// but a thread abort on a separate thread initializing those same
// systems would also do a resource lookup via the ResourceManager.
// We've deadlocked in CompareInfo.GetCompareInfo &
// Environment.GetResourceString.  It's not practical to take all of
// our locks within CER's to avoid this problem - just use the CLR's
// unmanaged resources.
//
extern "C" void QCALLTYPE ExceptionNative_GetMessageFromNativeResources(ExceptionMessageKind kind, QCall::StringHandleOnStack retMesg)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    SString buffer;
    HRESULT hr = S_OK;
    const WCHAR * wszFallbackString = NULL;

    switch(kind) {
    case ExceptionMessageKind::ThreadAbort:
        hr = buffer.LoadResourceAndReturnHR(IDS_EE_THREAD_ABORT);
        if (FAILED(hr)) {
            wszFallbackString = W("Thread was being aborted.");
        }
        break;

    case ExceptionMessageKind::ThreadInterrupted:
        hr = buffer.LoadResourceAndReturnHR(IDS_EE_THREAD_INTERRUPTED);
        if (FAILED(hr)) {
            wszFallbackString = W("Thread was interrupted from a waiting state.");
        }
        break;

    case ExceptionMessageKind::OutOfMemory:
        hr = buffer.LoadResourceAndReturnHR(IDS_EE_OUT_OF_MEMORY);
        if (FAILED(hr)) {
            wszFallbackString = W("Insufficient memory to continue the execution of the program.");
        }
        break;

    default:
        _ASSERTE(!"Unknown ExceptionMessageKind value!");
    }
    if (FAILED(hr)) {
        STRESS_LOG1(LF_BCL, LL_ALWAYS, "LoadResource error: %x", hr);
        _ASSERTE(wszFallbackString != NULL);
        retMesg.Set(wszFallbackString);
    }
    else {
        retMesg.Set(buffer);
    }

    END_QCALL;
}

extern "C" void QCALLTYPE ExceptionNative_GetMethodFromStackTrace(QCall::ObjectHandleOnStack stacktrace, QCall::ObjectHandleOnStack retMethodInfo)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    MethodDesc* pMD = NULL;
    // See ExceptionObject::GetStackTrace() and ExceptionObject::SetStackTrace()
    // for details on the stacktrace array.
    {
        ARRAYBASEREF arrayBaseRef = (ARRAYBASEREF)stacktrace.Get();
        _ASSERTE(arrayBaseRef != NULL);

        // The stacktrace can be either sbyte[] or Object[]. In the latter case,
        // the first entry is the actual stack trace sbyte[], the rest are pointers
        // to the method info objects. We only care about the first entry here.
        CorElementType elemType = arrayBaseRef->GetArrayElementType();
        if (elemType != ELEMENT_TYPE_I1)
        {
            _ASSERTE(elemType == ELEMENT_TYPE_CLASS); // object[]
            PTRARRAYREF ptrArrayRef = (PTRARRAYREF)arrayBaseRef;
            arrayBaseRef = (ARRAYBASEREF)OBJECTREFToObject(ptrArrayRef->GetAt(0));
        }

        I1ARRAYREF arrayRef = (I1ARRAYREF)arrayBaseRef;
        StackTraceArray stackArray(arrayRef);
        _ASSERTE(stackArray.Size() > 0);
        pMD = stackArray[0].pFunc;
    }

    // The managed stack trace classes always return typical method definition,
    // so we don't need to bother providing exact instantiation.
    MethodDesc* pMDTypical = pMD->LoadTypicalMethodDefinition();
    retMethodInfo.Set(pMDTypical->AllocateStubMethodInfo());
    _ASSERTE(pMDTypical->IsRuntimeMethodHandle());

    END_QCALL;
}

extern "C" void QCALLTYPE ExceptionNative_ThrowAmbiguousResolutionException(
    MethodTable* pTargetClass,
    MethodTable* pInterfaceMT,
    MethodDesc* pInterfaceMD)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    ThrowAmbiguousResolutionException(pTargetClass, pInterfaceMT, pInterfaceMD);

    END_QCALL;
}

extern "C" void QCALLTYPE ExceptionNative_ThrowEntryPointNotFoundException(
    MethodTable* pTargetClass,
    MethodTable* pInterfaceMT,
    MethodDesc* pInterfaceMD)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    ThrowEntryPointNotFoundException(pTargetClass, pInterfaceMT, pInterfaceMD);

    END_QCALL;
}

extern "C" void QCALLTYPE ExceptionNative_ThrowMethodAccessException(MethodDesc* caller, MethodDesc* callee)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    _ASSERTE(caller != NULL);
    AccessCheckContext accessContext(caller);
    ThrowMethodAccessException(&accessContext, callee);

    END_QCALL;
}

extern "C" void QCALLTYPE ExceptionNative_ThrowFieldAccessException(MethodDesc* caller, FieldDesc* callee)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    _ASSERTE(caller != NULL);
    AccessCheckContext accessContext(caller);
    ThrowFieldAccessException(&accessContext, callee);

    END_QCALL;
}

extern "C" void QCALLTYPE ExceptionNative_ThrowClassAccessException(MethodDesc* caller, EnregisteredTypeHandle callee)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    _ASSERTE(caller != NULL);
    AccessCheckContext accessContext(caller);
    ThrowTypeAccessException(&accessContext, TypeHandle::FromPtr(callee).GetMethodTable());

    END_QCALL;
}

extern "C" void QCALLTYPE Buffer_Clear(void *dst, size_t length)
{
    QCALL_CONTRACT;

#if defined(HOST_X86) || defined(HOST_AMD64)
    if (length > 0x100)
    {
        // memset ends up calling rep stosb if the hardware claims to support it efficiently. rep stosb is up to 2x slower
        // on misaligned blocks. Workaround this issue by aligning the blocks passed to memset upfront.

        *(uint64_t*)dst = 0;
        *((uint64_t*)dst + 1) = 0;
        *((uint64_t*)dst + 2) = 0;
        *((uint64_t*)dst + 3) = 0;

        void* end = (uint8_t*)dst + length;
        *((uint64_t*)end - 1) = 0;
        *((uint64_t*)end - 2) = 0;
        *((uint64_t*)end - 3) = 0;
        *((uint64_t*)end - 4) = 0;

        dst = ALIGN_UP((uint8_t*)dst + 1, 32);
        length = ALIGN_DOWN((uint8_t*)end - 1, 32) - (uint8_t*)dst;
    }
#endif

    memset(dst, 0, length);
}

extern "C" void QCALLTYPE Buffer_MemMove(void *dst, void *src, size_t length)
{
    QCALL_CONTRACT;

    memmove(dst, src, length);
}

FCIMPL3(VOID, Buffer::BulkMoveWithWriteBarrier, void *dst, void *src, size_t byteCount)
{
    FCALL_CONTRACT;

    if (dst != src && byteCount != 0)
        InlinedMemmoveGCRefsHelper(dst, src, byteCount);
}
FCIMPLEND

//
// EnvironmentNative
//
extern "C" VOID QCALLTYPE Environment_Exit(INT32 exitcode)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via setting
    // Environment's ExitCode property.
    SetLatchedExitCode(exitcode);

    ForceEEShutdown();

    END_QCALL;
}

FCIMPL1(VOID,EnvironmentNative::SetExitCode,INT32 exitcode)
{
    FCALL_CONTRACT;

    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via setting
    // Environment's ExitCode property.
    SetLatchedExitCode(exitcode);
}
FCIMPLEND

FCIMPL0(INT32, EnvironmentNative::GetExitCode)
{
    FCALL_CONTRACT;

    // Return whatever has been latched so far.  This is uninitialized to 0.
    return GetLatchedExitCode();
}
FCIMPLEND

extern "C" INT32 QCALLTYPE Environment_GetProcessorCount()
{
    QCALL_CONTRACT;

    INT32 processorCount = 0;

    BEGIN_QCALL;

    processorCount = GetCurrentProcessCpuCount();

    END_QCALL;

    return processorCount;
}

struct FindFailFastCallerStruct {
    StackCrawlMark* pStackMark;
    UINT_PTR        retAddress;
};

// This method is called by the GetMethod function and will crawl backward
//  up the stack for integer methods.
static StackWalkAction FindFailFastCallerCallback(CrawlFrame* frame, VOID* data) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    FindFailFastCallerStruct* pFindCaller = (FindFailFastCallerStruct*) data;

    // The check here is between the address of a local variable
    // (the stack mark) and a pointer to the EIP for a frame
    // (which is actually the pointer to the return address to the
    // function from the previous frame). So we'll actually notice
    // which frame the stack mark was in one frame later. This is
    // fine since we only implement LookForMyCaller.
    _ASSERTE(*pFindCaller->pStackMark == LookForMyCaller);
    if (!frame->IsInCalleesFrames(pFindCaller->pStackMark))
        return SWA_CONTINUE;

    pFindCaller->retAddress = GetControlPC(frame->GetRegisterSet());
    return SWA_ABORT;
}

static thread_local int8_t alreadyFailing = 0;

extern "C" void QCALLTYPE Environment_FailFast(QCall::StackCrawlMarkHandle mark, PCWSTR message, QCall::ObjectHandleOnStack exception, PCWSTR errorSource)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    FindFailFastCallerStruct findCallerData;
    findCallerData.pStackMark = mark;
    findCallerData.retAddress = 0;
    GetThread()->StackWalkFrames(FindFailFastCallerCallback, &findCallerData, FUNCTIONSONLY | QUICKUNWIND);

    if (message == NULL || message[0] == W('\0'))
    {
        OutputDebugString(W("CLR: Managed code called FailFast without specifying a reason.\r\n"));
    }
    else
    {
        OutputDebugString(W("CLR: Managed code called FailFast.\r\n"));
        OutputDebugString(message);
        OutputDebugString(W("\r\n"));
    }

    LPCWSTR argExceptionString = NULL;
    StackSString msg;
    // Because Environment_FailFast should kill the process, any subsequent calls are likely nested call from managed while formatting the exception message or the stack trace.
    // Only collect exception string if this is the first attempt to fail fast on this thread.
    alreadyFailing++;
    if (alreadyFailing != 1)
    {
        argExceptionString = W("Environment.FailFast called recursively.");
    }
    else if (exception.Get() != NULL)
    {
        GetExceptionMessage(exception.Get(), msg);
        argExceptionString = msg.GetUnicode();
    }

    Thread *pThread = GetThread();

#ifndef TARGET_UNIX
    // If we have the exception object, then try to setup
    // the watson bucket if it has any details.
    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this, if required.
    if (IsWatsonEnabled())
    {
        if ((exception.Get() == NULL) || !SetupWatsonBucketsForFailFast((EXCEPTIONREF)exception.Get()))
        {
            PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pThread->GetExceptionState()->GetUEWatsonBucketTracker();
            _ASSERTE(pUEWatsonBucketTracker != NULL);
            pUEWatsonBucketTracker->SaveIpForWatsonBucket(findCallerData.retAddress);
            pUEWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::FatalError, pThread, NULL);
            if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
            {
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            }
        }
    }
#endif // !TARGET_UNIX

    // stash the user-provided exception object. this will be used as
    // the inner exception object to the FatalExecutionEngineException.
    if (exception.Get() != NULL)
        pThread->SetLastThrownObject(exception.Get());

    EEPolicy::HandleFatalError(COR_E_FAILFAST, findCallerData.retAddress, message, NULL, errorSource, argExceptionString);

    END_QCALL;
}

#if defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE X86BaseCpuId(int cpuInfo[4], int functionId, int subFunctionId)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    __cpuidex(cpuInfo, functionId, subFunctionId);

    END_QCALL;
}

#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

//
// ObjectNative
//
extern "C" INT32 QCALLTYPE ObjectNative_GetHashCodeSlow(QCall::ObjectHandleOnStack objHandle)
{
    QCALL_CONTRACT;

    INT32 idx = 0;

    BEGIN_QCALL;

    GCX_COOP();

    _ASSERTE(objHandle.Get() != NULL);
    idx = objHandle.Get()->GetHashCodeEx();

    END_QCALL;

    return idx;
}

FCIMPL1(INT32, ObjectNative::TryGetHashCode, Object* obj)
{
    FCALL_CONTRACT;

    if (obj == NULL)
        return 0;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    return objRef->TryGetHashCode();
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ObjectNative::ContentEquals, Object *pThisRef, Object *pCompareRef)
{
    FCALL_CONTRACT;

    // Should be ensured by caller
    _ASSERTE(pThisRef != NULL);
    _ASSERTE(pCompareRef != NULL);
    _ASSERTE(pThisRef->GetMethodTable() == pCompareRef->GetMethodTable());

    MethodTable *pThisMT = pThisRef->GetMethodTable();

    // Compare the contents
    BOOL ret = memcmp(
        pThisRef->GetData(),
        pCompareRef->GetData(),
        pThisMT->GetNumInstanceFieldBytes()) == 0;

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

extern "C" void QCALLTYPE ObjectNative_AllocateUninitializedClone(QCall::ObjectHandleOnStack objHandle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF refClone = objHandle.Get();
    _ASSERTE(refClone != NULL); // Should be handled at managed side
    MethodTable* pMT = refClone->GetMethodTable();

    // assert that String has overloaded the Clone() method
    _ASSERTE(pMT != g_pStringClass);

    if (pMT->IsArray())
    {
        objHandle.Set(DupArrayForCloning((BASEARRAYREF)refClone));
    }
    else
    {
        // We don't need to call the <cinit> because we know
        //  that it has been called....(It was called before this was created)
        objHandle.Set(AllocateObject(pMT));
    }

    END_QCALL;
}

//
//
// COMInterlocked
//

#include <optsmallperfcritical.h>

FCIMPL2(INT32,COMInterlocked::Exchange32, INT32 *location, INT32 value)
{
    FCALL_CONTRACT;

    return InterlockedExchange((LONG *) location, value);
}
FCIMPLEND

FCIMPL2_IV(INT64,COMInterlocked::Exchange64, INT64 *location, INT64 value)
{
    FCALL_CONTRACT;

    return InterlockedExchange64((INT64 *) location, value);
}
FCIMPLEND

FCIMPL3(INT32, COMInterlocked::CompareExchange32, INT32* location, INT32 value, INT32 comparand)
{
    FCALL_CONTRACT;

    return InterlockedCompareExchange((LONG*)location, value, comparand);
}
FCIMPLEND

FCIMPL3_IVV(INT64, COMInterlocked::CompareExchange64, INT64* location, INT64 value, INT64 comparand)
{
    FCALL_CONTRACT;

    return InterlockedCompareExchange64((INT64*)location, value, comparand);
}
FCIMPLEND

FCIMPL2(LPVOID,COMInterlocked::ExchangeObject, LPVOID*location, LPVOID value)
{
    FCALL_CONTRACT;

    LPVOID ret = InterlockedExchangeT(location, value);
#ifdef _DEBUG
    Thread::ObjectRefAssign((OBJECTREF *)location);
#endif
    ErectWriteBarrier((OBJECTREF*) location, ObjectToOBJECTREF((Object*) value));
    return ret;
}
FCIMPLEND

FCIMPL3(LPVOID,COMInterlocked::CompareExchangeObject, LPVOID *location, LPVOID value, LPVOID comparand)
{
    FCALL_CONTRACT;

    // <TODO>@todo: only set ref if is updated</TODO>
    LPVOID ret = InterlockedCompareExchangeT(location, value, comparand);
    if (ret == comparand) {
#ifdef _DEBUG
        Thread::ObjectRefAssign((OBJECTREF *)location);
#endif
        ErectWriteBarrier((OBJECTREF*) location, ObjectToOBJECTREF((Object*) value));
    }
    return ret;
}
FCIMPLEND

FCIMPL2(INT32,COMInterlocked::ExchangeAdd32, INT32 *location, INT32 value)
{
    FCALL_CONTRACT;

    return InterlockedExchangeAdd((LONG *) location, value);
}
FCIMPLEND

FCIMPL2_IV(INT64,COMInterlocked::ExchangeAdd64, INT64 *location, INT64 value)
{
    FCALL_CONTRACT;

    return InterlockedExchangeAdd64((INT64 *) location, value);
}
FCIMPLEND

#include <optdefault.h>

extern "C" void QCALLTYPE Interlocked_MemoryBarrierProcessWide()
{
    QCALL_CONTRACT;

    minipal_memory_barrier_process_wide();
}

static BOOL HasOverriddenMethod(MethodTable* mt, MethodTable* classMT, WORD methodSlot)
{
    CONTRACTL{
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE(mt != NULL);
    _ASSERTE(classMT != NULL);
    _ASSERTE(methodSlot != 0);

    PCODE actual = mt->GetRestoredSlot(methodSlot);
    PCODE base = classMT->GetRestoredSlot(methodSlot);

    if (actual == base)
    {
        return FALSE;
    }

    // If CoreLib is JITed, the slots can be patched and thus we need to compare the actual MethodDescs
    // to detect match reliably
    if (NonVirtualEntry2MethodDesc(actual) == NonVirtualEntry2MethodDesc(base))
    {
        return FALSE;
    }

    return TRUE;
}

BOOL CanCompareBitsOrUseFastGetHashCode(MethodTable* mt)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE(mt != NULL);

    if (mt->HasCheckedCanCompareBitsOrUseFastGetHashCode())
    {
        return mt->CanCompareBitsOrUseFastGetHashCode();
    }

    if (mt->ContainsGCPointers()
        || mt->IsNotTightlyPacked()
        || mt->GetClass()->IsInlineArray())
    {
        mt->SetHasCheckedCanCompareBitsOrUseFastGetHashCode();
        return FALSE;
    }

    MethodTable* valueTypeMT = CoreLibBinder::GetClass(CLASS__VALUE_TYPE);
    WORD slotEquals = CoreLibBinder::GetMethod(METHOD__VALUE_TYPE__EQUALS)->GetSlot();
    WORD slotGetHashCode = CoreLibBinder::GetMethod(METHOD__VALUE_TYPE__GET_HASH_CODE)->GetSlot();

    // Check the input type.
    if (HasOverriddenMethod(mt, valueTypeMT, slotEquals)
        || HasOverriddenMethod(mt, valueTypeMT, slotGetHashCode))
    {
        mt->SetHasCheckedCanCompareBitsOrUseFastGetHashCode();

        // If overridden Equals or GetHashCode found, stop searching further.
        return FALSE;
    }

    BOOL canCompareBitsOrUseFastGetHashCode = TRUE;

    // The type itself did not override Equals or GetHashCode, go for its fields.
    ApproxFieldDescIterator iter = ApproxFieldDescIterator(mt, ApproxFieldDescIterator::INSTANCE_FIELDS);
    for (FieldDesc* pField = iter.Next(); pField != NULL; pField = iter.Next())
    {
        if (pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
        {
            // Check current field type.
            MethodTable* fieldMethodTable = pField->GetApproxFieldTypeHandleThrowing().GetMethodTable();
            if (!CanCompareBitsOrUseFastGetHashCode(fieldMethodTable))
            {
                canCompareBitsOrUseFastGetHashCode = FALSE;
                break;
            }
        }
        else if (pField->GetFieldType() == ELEMENT_TYPE_R8
                || pField->GetFieldType() == ELEMENT_TYPE_R4)
        {
            // We have double/single field, cannot compare in fast path.
            canCompareBitsOrUseFastGetHashCode = FALSE;
            break;
        }
    }

    // We've gone through all instance fields. It's time to cache the result.
    // Note SetCanCompareBitsOrUseFastGetHashCode(BOOL) ensures the checked flag
    // and canCompare flag being set atomically to avoid race.
    mt->SetCanCompareBitsOrUseFastGetHashCode(canCompareBitsOrUseFastGetHashCode);

    return canCompareBitsOrUseFastGetHashCode;
}

extern "C" BOOL QCALLTYPE MethodTable_CanCompareBitsOrUseFastGetHashCode(MethodTable* mt)
{
    QCALL_CONTRACT;

    BOOL ret = FALSE;

    BEGIN_QCALL;

    if (mt->GetClass()->IsInlineArray())
        COMPlusThrow(kNotSupportedException, W("NotSupported_InlineArrayEqualsGetHashCode"));

    ret = CanCompareBitsOrUseFastGetHashCode(mt);

    END_QCALL;

    return ret;
}

enum ValueTypeHashCodeStrategy
{
    None,
    ReferenceField,
    DoubleField,
    SingleField,
    FastGetHashCode,
    ValueTypeOverride,
};

static ValueTypeHashCodeStrategy GetHashCodeStrategy(MethodTable* mt, QCall::ObjectHandleOnStack objHandle, UINT32* fieldOffset, UINT32* fieldSize, MethodTable** fieldMTOut)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // Should be handled by caller
    _ASSERTE(!mt->CanCompareBitsOrUseFastGetHashCode());

    ValueTypeHashCodeStrategy ret = ValueTypeHashCodeStrategy::None;

    // Grab the first non-null field and return its hash code or 'it' as hash code
    ApproxFieldDescIterator fdIterator(mt, ApproxFieldDescIterator::INSTANCE_FIELDS);

    FieldDesc *field;
    while ((field = fdIterator.Next()) != NULL)
    {
        _ASSERTE(!field->IsRVA());
        if (field->IsObjRef())
        {
            GCX_COOP();
            // if we get an object reference we get the hash code out of that
            if (*(Object**)((BYTE *)objHandle.Get()->UnBox() + *fieldOffset + field->GetOffsetUnsafe()) != NULL)
            {
                *fieldOffset += field->GetOffsetUnsafe();
                ret = ValueTypeHashCodeStrategy::ReferenceField;
            }
            else
            {
                // null object reference, try next
                continue;
            }
        }
        else
        {
            CorElementType fieldType = field->GetFieldType();
            if (fieldType == ELEMENT_TYPE_R8)
            {
                *fieldOffset += field->GetOffsetUnsafe();
                ret = ValueTypeHashCodeStrategy::DoubleField;
            }
            else if (fieldType == ELEMENT_TYPE_R4)
            {
                *fieldOffset += field->GetOffsetUnsafe();
                ret = ValueTypeHashCodeStrategy::SingleField;
            }
            else if (fieldType != ELEMENT_TYPE_VALUETYPE)
            {
                *fieldOffset += field->GetOffsetUnsafe();
                *fieldSize = field->LoadSize();
                ret = ValueTypeHashCodeStrategy::FastGetHashCode;
            }
            else
            {
                // got another value type. Get the type
                TypeHandle fieldTH = field->GetFieldTypeHandleThrowing();
                _ASSERTE(!fieldTH.IsNull());
                MethodTable* fieldMT = fieldTH.GetMethodTable();
                if (CanCompareBitsOrUseFastGetHashCode(fieldMT))
                {
                    *fieldOffset += field->GetOffsetUnsafe();
                    *fieldSize = field->LoadSize();
                    ret = ValueTypeHashCodeStrategy::FastGetHashCode;
                }
                else if (HasOverriddenMethod(fieldMT,
                                             CoreLibBinder::GetClass(CLASS__VALUE_TYPE),
                                             CoreLibBinder::GetMethod(METHOD__VALUE_TYPE__GET_HASH_CODE)->GetSlot()))
                {
                    *fieldOffset += field->GetOffsetUnsafe();
                    *fieldMTOut = fieldMT;
                    ret = ValueTypeHashCodeStrategy::ValueTypeOverride;
                }
                else
                {
                    *fieldOffset += field->GetOffsetUnsafe();
                    ret = GetHashCodeStrategy(fieldMT, objHandle, fieldOffset, fieldSize, fieldMTOut);
                }
            }
        }
        break;
    }

    return ret;
}

extern "C" INT32 QCALLTYPE ValueType_GetHashCodeStrategy(MethodTable* mt, QCall::ObjectHandleOnStack objHandle, UINT32* fieldOffset, UINT32* fieldSize, MethodTable** fieldMT)
{
    QCALL_CONTRACT;

    ValueTypeHashCodeStrategy ret = ValueTypeHashCodeStrategy::None;
    *fieldOffset = 0;
    *fieldSize = 0;
    *fieldMT = NULL;

    BEGIN_QCALL;

    ret = GetHashCodeStrategy(mt, objHandle, fieldOffset, fieldSize, fieldMT);

    END_QCALL;

    return ret;
}

FCIMPL1(UINT32, MethodTableNative::GetNumInstanceFieldBytes, MethodTable* mt)
{
    FCALL_CONTRACT;
    return mt->GetNumInstanceFieldBytes();
}
FCIMPLEND

FCIMPL1(CorElementType, MethodTableNative::GetPrimitiveCorElementType, MethodTable* mt)
{
    FCALL_CONTRACT;

    _ASSERTE(mt->IsTruePrimitive() || mt->IsEnum());

    // MethodTable::GetInternalCorElementType has unnecessary overhead for primitives and enums
    // Call EEClass::GetInternalCorElementType directly to avoid it
    return mt->GetClass()->GetInternalCorElementType();
}
FCIMPLEND

FCIMPL2(MethodTable*, MethodTableNative::GetMethodTableMatchingParentClass, MethodTable *mt, MethodTable* parent)
{
    FCALL_CONTRACT;

    return mt->GetMethodTableMatchingParentClass(parent);
}
FCIMPLEND

FCIMPL1(MethodTable*, MethodTableNative::InstantiationArg0, MethodTable* mt);
{
    FCALL_CONTRACT;

    return mt->GetInstantiation()[0].AsMethodTable();
}
FCIMPLEND

FCIMPL1(OBJECTHANDLE, MethodTableNative::GetLoaderAllocatorHandle, MethodTable *mt)
{
    FCALL_CONTRACT;

    return mt->GetLoaderAllocatorObjectHandle();
}
FCIMPLEND

extern "C" BOOL QCALLTYPE MethodTable_AreTypesEquivalent(MethodTable* mta, MethodTable* mtb)
{
    QCALL_CONTRACT;

    BOOL bResult = FALSE;

    BEGIN_QCALL;

    bResult = mta->IsEquivalentTo(mtb);

    END_QCALL;

    return bResult;
}

extern "C" BOOL QCALLTYPE TypeHandle_CanCastTo_NoCacheLookup(void* fromTypeHnd, void* toTypeHnd)
{
    QCALL_CONTRACT;

    BOOL ret = false;

    BEGIN_QCALL;

    // Cache lookup and trivial cases are already handled at managed side. Call the uncached versions directly.
    _ASSERTE(fromTypeHnd != toTypeHnd);

    GCX_COOP();

    TypeHandle fromTH = TypeHandle::FromPtr(fromTypeHnd);
    TypeHandle toTH = TypeHandle::FromPtr(toTypeHnd);

    if (fromTH.IsTypeDesc())
    {
        ret = fromTH.AsTypeDesc()->CanCastTo(toTH, NULL);
    }
    else
    {
        ret = fromTH.AsMethodTable()->CanCastTo(toTH.AsMethodTable(), NULL);
    }

    END_QCALL;

    return ret;
}

extern "C" INT32 QCALLTYPE TypeHandle_GetCorElementType(void* typeHnd)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    return (INT32)TypeHandle::FromPtr(typeHnd).GetSignatureCorElementType();
}

static bool HasOverriddenStreamMethod(MethodTable* streamMT, MethodTable* pMT, WORD slot)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(streamMT != NULL);
        PRECONDITION(pMT != NULL);
    } CONTRACTL_END;

    PCODE actual = pMT->GetRestoredSlot(slot);
    PCODE base = streamMT->GetRestoredSlot(slot);

    // If the PCODEs match, then there is no override.
    if (actual == base)
        return false;

    // If CoreLib is JITed, the slots can be patched and thus we need to compare
    // the actual MethodDescs to detect match reliably.
    return NonVirtualEntry2MethodDesc(actual) != NonVirtualEntry2MethodDesc(base);
}

extern "C" BOOL QCALLTYPE Stream_HasOverriddenSlow(MethodTable* pMT, BOOL isRead)
{
    QCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    BOOL readOverride = FALSE;
    BOOL writeOverride = FALSE;

    BEGIN_QCALL;

    MethodTable* pStreamMT = CoreLibBinder::GetClass(CLASS__STREAM);
    WORD slotBeginRead = CoreLibBinder::GetMethod(METHOD__STREAM__BEGIN_READ)->GetSlot();
    WORD slotEndRead = CoreLibBinder::GetMethod(METHOD__STREAM__END_READ)->GetSlot();
    WORD slotBeginWrite = CoreLibBinder::GetMethod(METHOD__STREAM__BEGIN_WRITE)->GetSlot();
    WORD slotEndWrite = CoreLibBinder::GetMethod(METHOD__STREAM__END_WRITE)->GetSlot();

    // Check the current MethodTable for Stream overrides and set state on the MethodTable.
    readOverride = HasOverriddenStreamMethod(pStreamMT, pMT, slotBeginRead) || HasOverriddenStreamMethod(pStreamMT, pMT, slotEndRead);
    writeOverride = HasOverriddenStreamMethod(pStreamMT, pMT, slotBeginWrite) || HasOverriddenStreamMethod(pStreamMT, pMT, slotEndWrite);
    pMT->GetAuxiliaryDataForWrite()->SetStreamOverrideState(readOverride, writeOverride);

    END_QCALL;

    return isRead ? readOverride : writeOverride;
}
