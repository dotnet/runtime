// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*
 *    CallHelpers.CPP: helpers to call managed code
 *

 */

#include "common.h"
#include "dbginterface.h"

// To include declaration of "AppDomainTransitionExceptionFilter"
#include "excep.h"

// To include declaration of "SignatureNative"
#include "runtimehandles.h"

#include "invokeutil.h"
#include "argdestination.h"

#if defined(FEATURE_MULTICOREJIT) && defined(_DEBUG)

// Allow system module for Appx

void AssertMulticoreJitAllowedModule(PCODE pTarget)
{
    MethodDesc* pMethod = Entry2MethodDesc(pTarget, NULL);

    Module * pModule = pMethod->GetModule_NoLogging();

    _ASSERTE(pModule->IsSystem());
}

#endif

// For X86, INSTALL_COMPLUS_EXCEPTION_HANDLER grants us sufficient protection to call into
// managed code.
//
// But on 64-bit, the personality routine will not pop frames or trackers as exceptions unwind
// out of managed code.  Instead, we rely on explicit cleanup like CLRException::HandlerState::CleanupTry
// or UMThunkUnwindFrameChainHandler.
//
// So most callers should call through CallDescrWorkerWithHandler (or a wrapper like MethodDesc::Call)
// and get the platform-appropriate exception handling.  A few places try to optimize by calling direct
// to managed methods (see ArrayInitializeWorker or FastCallFinalize).  This sort of thing is
// dangerous.  You have to worry about marking yourself as a legal managed caller and you have to
// worry about how exceptions will be handled on a FEATURE_EH_FUNCLETS plan.  It is generally only suitable
// for X86.

//*******************************************************************************
void CallDescrWorkerWithHandler(
                CallDescrData *   pCallDescrData,
                BOOL              fCriticalCall)
{
#if defined(FEATURE_MULTICOREJIT) && defined(_DEBUG)

    // For multicore JITting, background thread should not call managed code, except when calling system code (e.g. throwing managed exception)
    if (GetThread()->HasThreadStateNC(Thread::TSNC_CallingManagedCodeDisabled))
    {
        AssertMulticoreJitAllowedModule(pCallDescrData->pTarget);
    }

#endif

    BEGIN_CALL_TO_MANAGEDEX(fCriticalCall ? EEToManagedCriticalCall : EEToManagedDefault);

    CallDescrWorker(pCallDescrData);

    END_CALL_TO_MANAGED();
}


#if !defined(HOST_64BIT) && defined(_DEBUG)

//*******************************************************************************
// assembly code, in i386/asmhelpers.asm
void CallDescrWorker(CallDescrData * pCallDescrData)
{
    //
    // This function must not have a contract ... it's caller has pushed an FS:0 frame (COMPlusFrameHandler) that must
    // be the first handler on the stack. The contract causes, at a minimum, a C++ exception handler to be pushed to
    // handle the destruction of the contract object. If there is an exception in the managed code called from here,
    // and that exception is handled in that same block of managed code, then the COMPlusFrameHandler will actually
    // unwind the C++ handler before branching to the catch clause in managed code. That essentially causes an
    // out-of-order destruction of the contract object, resulting in very odd crashes later.
    //
#if 0
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;
#endif // 0
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    TRIGGERSGC_NOSTOMP(); // Can't stomp object refs because they are args to the function

    // Save a copy of dangerousObjRefs in table.
    Thread* curThread;
    DWORD_PTR ObjRefTable[OBJREF_TABSIZE];

    curThread = GetThread();

    static_assert_no_msg(sizeof(curThread->dangerousObjRefs) == sizeof(ObjRefTable));
    memcpy(ObjRefTable, curThread->dangerousObjRefs, sizeof(ObjRefTable));

#ifndef FEATURE_INTERPRETER
    // When the interpreter is used, this mayb be called from preemptive code.
    _ASSERTE(curThread->PreemptiveGCDisabled());  // Jitted code expects to be in cooperative mode
#endif

    // If the current thread owns spinlock or unbreakable lock, it cannot call managed code.
    _ASSERTE(!curThread->HasUnbreakableLock() &&
             (curThread->m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

#ifdef TARGET_ARM
    _ASSERTE(IsThumbCode(pCallDescrData->pTarget));
#endif

    CallDescrWorkerInternal(pCallDescrData);

    // Restore dangerousObjRefs when we return back to EE after call
    memcpy(curThread->dangerousObjRefs, ObjRefTable, sizeof(ObjRefTable));

    TRIGGERSGC();

    ENABLESTRESSHEAP();
}
#endif // !defined(HOST_64BIT) && defined(_DEBUG)

void DispatchCallDebuggerWrapper(
    CallDescrData *   pCallDescrData,
    BOOL fCriticalCall
)
{
    // Use static contracts b/c we have SEH.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    struct Param : NotifyOfCHFFilterWrapperParam
    {
        CallDescrData * pCallDescrData;
        BOOL fCriticalCall;
    } param;

    param.pFrame = NULL;
    param.pCallDescrData = pCallDescrData;
    param.fCriticalCall = fCriticalCall;

    PAL_TRY(Param *, pParam, &param)
    {
        CallDescrWorkerWithHandler(
            pParam->pCallDescrData,
            pParam->fCriticalCall);
    }
    PAL_EXCEPT_FILTER(AppDomainTransitionExceptionFilter)
    {
        // Should never reach here b/c handler should always continue search.
        _ASSERTE(!"Unreachable");
    }
    PAL_ENDTRY
}

// Helper for VM->managed calls with simple signatures.
void * DispatchCallSimple(
                    SIZE_T *pSrc,
                    DWORD numStackSlotsToCopy,
                    PCODE pTargetAddress,
                    DWORD dwDispatchCallSimpleFlags)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall((const BYTE *)pTargetAddress);
#endif // DEBUGGING_SUPPORTED

    CallDescrData callDescrData;

#ifdef CALLDESCR_ARGREGS
    callDescrData.pSrc = pSrc + NUM_ARGUMENT_REGISTERS;
    callDescrData.numStackSlots = numStackSlotsToCopy;
    callDescrData.pArgumentRegisters = (ArgumentRegisters *)pSrc;
#else
    callDescrData.pSrc = pSrc;
    callDescrData.numStackSlots = numStackSlotsToCopy;
#endif

#ifdef CALLDESCR_RETBUFFARGREG
    UINT64 retBuffArgPlaceholder = 0;
    callDescrData.pRetBuffArg = &retBuffArgPlaceholder;
#endif

#ifdef CALLDESCR_FPARGREGS
    callDescrData.pFloatArgumentRegisters = NULL;
#endif
#ifdef CALLDESCR_REGTYPEMAP
    callDescrData.dwRegTypeMap = 0;
#endif
    callDescrData.fpReturnSize = 0;
    callDescrData.pTarget = pTargetAddress;

    if ((dwDispatchCallSimpleFlags & DispatchCallSimple_CatchHandlerFoundNotification) != 0)
    {
        DispatchCallDebuggerWrapper(
            &callDescrData,
            dwDispatchCallSimpleFlags & DispatchCallSimple_CriticalCall);
    }
    else
    {
        CallDescrWorkerWithHandler(&callDescrData, dwDispatchCallSimpleFlags & DispatchCallSimple_CriticalCall);
    }

    return *(void **)(&callDescrData.returnValue);
}

#ifdef CALLDESCR_REGTYPEMAP
//*******************************************************************************
void FillInRegTypeMap(int argOffset, CorElementType typ, BYTE * pMap)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(CheckPointer(pMap, NULL_NOT_OK));
    }
    CONTRACTL_END;

    int regArgNum = TransitionBlock::GetArgumentIndexFromOffset(argOffset);

    // Create a map of the first 8 argument types.  This is used in
    // CallDescrWorkerInternal to load args into general registers or
    // floating point registers.
    //
    // we put these in order from the LSB to the MSB so that we can keep
    // the map in a register and just examine the low byte and then shift
    // right for each arg.

    if (regArgNum < NUM_ARGUMENT_REGISTERS)
    {
        pMap[regArgNum] = typ;
    }
}
#endif // CALLDESCR_REGTYPEMAP

//*******************************************************************************
#ifdef FEATURE_INTERPRETER
void MethodDescCallSite::CallTargetWorker(const ARG_SLOT *pArguments, ARG_SLOT *pReturnValue, int cbReturnValue, bool transitionToPreemptive)
#else
void MethodDescCallSite::CallTargetWorker(const ARG_SLOT *pArguments, ARG_SLOT *pReturnValue, int cbReturnValue)
#endif
{
    //
    // WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING
    //
    // This method needs to have a GC_TRIGGERS contract because it
    // calls managed code.  However, IT MAY NOT TRIGGER A GC ITSELF
    // because the argument array is not protected and may contain gc
    // refs.
    //
    // WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING
    //
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_COOPERATIVE;
        PRECONDITION(GetAppDomain()->CheckCanExecuteManagedCode(m_pMD));
        PRECONDITION(m_pMD->CheckActivated());          // EnsureActive will trigger, so we must already be activated
    }
    CONTRACTL_END;

    // If we're invoking an CoreLib method, lift the restriction on type load limits. Calls into CoreLib are
    // typically calls into specific and controlled helper methods for security checks and other linktime tasks.
    //
    // @todo: In an ideal world, we would require each of those sites to do the override rather than disabling
    // the assert broadly here. However, by limiting the override to CoreLib methods, we should still be able
    // to effectively enforce the more general rule about loader recursion.
    MAYBE_OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED, m_pMD->GetModule()->IsSystem());

    LPBYTE pTransitionBlock;
    UINT   nStackBytes;
    UINT   fpReturnSize;
#ifdef CALLDESCR_REGTYPEMAP
    UINT64 dwRegTypeMap;
#endif
#ifdef CALLDESCR_FPARGREGS
    FloatArgumentRegisters *pFloatArgumentRegisters = NULL;
#endif
    void*  pvRetBuff = NULL;

    {
        //
        // the incoming argument array is not gc-protected, so we
        // may not trigger a GC before we actually call managed code
        //
        GCX_FORBID();

        // Record this call if required
        g_IBCLogger.LogMethodDescAccess(m_pMD);

        //
        // All types must already be loaded. This macro also sets up a FAULT_FORBID region which is
        // also required for critical calls since we cannot inject any failure points between the
        // caller of MethodDesc::CallDescr and the actual transition to managed code.
        //
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

#ifdef FEATURE_INTERPRETER
        _ASSERTE(isCallConv(m_methodSig.GetCallingConvention(), IMAGE_CEE_CS_CALLCONV_DEFAULT)
                 || isCallConv(m_methodSig.GetCallingConvention(), CorCallingConvention(IMAGE_CEE_CS_CALLCONV_C))
                 || isCallConv(m_methodSig.GetCallingConvention(), CorCallingConvention(IMAGE_CEE_CS_CALLCONV_VARARG))
                 || isCallConv(m_methodSig.GetCallingConvention(), CorCallingConvention(IMAGE_CEE_CS_CALLCONV_NATIVEVARARG))
                 || isCallConv(m_methodSig.GetCallingConvention(), CorCallingConvention(IMAGE_CEE_CS_CALLCONV_STDCALL)));
#else
        _ASSERTE(isCallConv(m_methodSig.GetCallingConvention(), IMAGE_CEE_CS_CALLCONV_DEFAULT));
        _ASSERTE(!(m_methodSig.GetCallingConventionInfo() & CORINFO_CALLCONV_PARAMTYPE));
#endif

#ifdef DEBUGGING_SUPPORTED
        if (CORDebuggerTraceCall())
        {
            g_pDebugInterface->TraceCall((const BYTE *)m_pCallTarget);
        }
#endif // DEBUGGING_SUPPORTED

#ifdef _DEBUG
        {
#ifdef UNIX_AMD64_ABI
            // Validate that the return value is not too big for the buffer passed
            if (m_pMD->GetMethodTable()->IsRegPassedStruct())
            {
                TypeHandle thReturnValueType;
                if (m_methodSig.GetReturnTypeNormalized(&thReturnValueType) == ELEMENT_TYPE_VALUETYPE)
                {
                    _ASSERTE((DWORD)cbReturnValue >= thReturnValueType.GetSize());
                }
            }
#endif // UNIX_AMD64_ABI

            // The metasig should be reset
            _ASSERTE(m_methodSig.GetArgNum() == 0);

            // Check to see that any value type args have been loaded and restored.
            // This is because we may be calling a FramedMethodFrame which will use the sig
            // to trace the args, but if any are unloaded we will be stuck if a GC occurs.
            CorElementType argType;
            while ((argType = m_methodSig.NextArg()) != ELEMENT_TYPE_END)
            {
                if (argType == ELEMENT_TYPE_VALUETYPE)
                {
                    TypeHandle th = m_methodSig.GetLastTypeHandleThrowing(ClassLoader::DontLoadTypes);
                    CONSISTENCY_CHECK(th.CheckFullyLoaded());
                    CONSISTENCY_CHECK(th.IsRestored_NoLogging());
                }
            }
            m_methodSig.Reset();
        }
#endif // _DEBUG

        DWORD   arg = 0;

        nStackBytes = m_argIt.SizeOfFrameArgumentArray();

        // Create a fake FramedMethodFrame on the stack.

        // Note that SizeOfFrameArgumentArray does overflow checks with sufficient margin to prevent overflows here
        DWORD dwAllocaSize = TransitionBlock::GetNegSpaceSize() + sizeof(TransitionBlock) + nStackBytes;

        LPBYTE pAlloc = (LPBYTE)_alloca(dwAllocaSize);

        pTransitionBlock = pAlloc + TransitionBlock::GetNegSpaceSize();

#ifdef CALLDESCR_REGTYPEMAP
        dwRegTypeMap            = 0;
        BYTE*   pMap            = (BYTE*)&dwRegTypeMap;
#endif // CALLDESCR_REGTYPEMAP

        if (m_argIt.HasThis())
        {
            *((LPVOID*)(pTransitionBlock + m_argIt.GetThisOffset())) = ArgSlotToPtr(pArguments[arg++]);
        }

        if (m_argIt.HasRetBuffArg())
        {
            *((LPVOID*)(pTransitionBlock + m_argIt.GetRetBuffArgOffset())) = ArgSlotToPtr(pArguments[arg++]);
        }
#ifdef FEATURE_HFA
#ifdef FEATURE_INTERPRETER
        // Something is necessary for HFA's, but what's below (in the FEATURE_INTERPRETER ifdef)
        // doesn't seem to do the proper test.  It fires,
        // incorrectly, for a one-word struct that *doesn't* have a ret buff.  So we'll try this, instead:
        // We're here because it doesn't have a ret buff.  If it would, except that the struct being returned
        // is an HFA, *then* assume the invoker made this slot a ret buff pointer.
        // It's an HFA if the return type is a struct, but it has a non-zero FP return size.
        // (If it were an HFA, but had a ret buff because it was varargs, then we wouldn't be here.
        // Also this test won't work for float enums.
        else if (m_methodSig.GetReturnType() == ELEMENT_TYPE_VALUETYPE
                  && m_argIt.GetFPReturnSize() > 0)
#else  // FEATURE_INTERPRETER
        else if (ELEMENT_TYPE_VALUETYPE == m_methodSig.GetReturnTypeNormalized())
#endif // FEATURE_INTERPRETER
        {
            pvRetBuff = ArgSlotToPtr(pArguments[arg++]);
        }
#endif // FEATURE_HFA


#ifdef FEATURE_INTERPRETER
        if (m_argIt.IsVarArg())
        {
            *((LPVOID*)(pTransitionBlock + m_argIt.GetVASigCookieOffset())) = ArgSlotToPtr(pArguments[arg++]);
        }

        if (m_argIt.HasParamType())
        {
            *((LPVOID*)(pTransitionBlock + m_argIt.GetParamTypeArgOffset())) = ArgSlotToPtr(pArguments[arg++]);
        }
#endif

        int ofs;
        for (; TransitionBlock::InvalidOffset != (ofs = m_argIt.GetNextOffset()); arg++)
        {
#ifdef CALLDESCR_REGTYPEMAP
            FillInRegTypeMap(ofs, m_argIt.GetArgType(), pMap);
#endif

#ifdef CALLDESCR_FPARGREGS
            // Under CALLDESCR_FPARGREGS -ve offsets indicate arguments in floating point registers. If we
            // have at least one such argument we point the call worker at the floating point area of the
            // frame (we leave it null otherwise since the worker can perform a useful optimization if it
            // knows no floating point registers need to be set up).
            if (TransitionBlock::HasFloatRegister(ofs, m_argIt.GetArgLocDescForStructInRegs()) &&
                (pFloatArgumentRegisters == NULL))
            {
                pFloatArgumentRegisters = (FloatArgumentRegisters*)(pTransitionBlock +
                                                                    TransitionBlock::GetOffsetOfFloatArgumentRegisters());
            }
#endif

            ArgDestination argDest(pTransitionBlock, ofs, m_argIt.GetArgLocDescForStructInRegs());

            UINT32 stackSize = m_argIt.GetArgSize();
            // We need to pass in a pointer, but be careful of the ARG_SLOT calling convention. We might already have a pointer in the ARG_SLOT.
            PVOID pSrc = stackSize > sizeof(ARG_SLOT) ? (LPVOID)ArgSlotToPtr(pArguments[arg]) : (LPVOID)ArgSlotEndianessFixup((ARG_SLOT*)&pArguments[arg], stackSize);

#if defined(UNIX_AMD64_ABI)
            if (argDest.IsStructPassedInRegs())
            {
                TypeHandle th;
                m_argIt.GetArgType(&th);

                argDest.CopyStructToRegisters(pSrc, th.AsMethodTable()->GetNumInstanceFieldBytes(), 0);
            }
            else
#elif defined(TARGET_LOONGARCH64)
            if (argDest.IsStructPassedInRegs())
            {
                argDest.CopyStructToRegisters(pSrc, stackSize);
            }
            else
#endif // TARGET_LOONGARCH64
            {
                PVOID pDest = argDest.GetDestinationAddress();

                switch (stackSize)
                {
#if defined(TARGET_LOONGARCH64)
                    case 1:
                        if (m_argIt.GetArgType() == ELEMENT_TYPE_U1 || m_argIt.GetArgType() == ELEMENT_TYPE_BOOLEAN)
                            *((INT64*)pDest) = (UINT8)pArguments[arg];
                        else
                            *((INT64*)pDest) = (INT8)pArguments[arg];
                        break;
                    case 2:
                        if (m_argIt.GetArgType() == ELEMENT_TYPE_U2 || m_argIt.GetArgType() == ELEMENT_TYPE_CHAR)
                            *((INT64*)pDest) = (UINT16)pArguments[arg];
                        else
                            *((INT64*)pDest) = (INT16)pArguments[arg];
                        break;
                    case 4:
                        if (m_argIt.GetArgType() == ELEMENT_TYPE_U4)
                            *((INT64*)pDest) = (UINT32)pArguments[arg];
                        else
                            *((INT64*)pDest) = (INT32)pArguments[arg];
                        break;
#else
                    case 1:
                    case 2:
                    case 4:
                        *((INT32*)pDest) = (INT32)pArguments[arg];
                        break;
#endif

                    case 8:
                        *((INT64*)pDest) = pArguments[arg];
                        break;

                    default:
                        // The ARG_SLOT contains a pointer to the value-type
    #ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
                        if (m_argIt.IsArgPassedByRef())
                        {
                            *(PVOID*)pDest = pSrc;
                        }
                        else
    #endif // ENREGISTERED_PARAMTYPE_MAXSIZE
                        if (stackSize > sizeof(ARG_SLOT))
                        {
                            CopyMemory(pDest, ArgSlotToPtr(pArguments[arg]), stackSize);
                        }
                        else
                        {
                            CopyMemory(pDest, (LPVOID) (&pArguments[arg]), stackSize);
                        }
                        break;
                }
            }
        }

        fpReturnSize = m_argIt.GetFPReturnSize();

    } // END GCX_FORBID & ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE

    CallDescrData callDescrData;

    callDescrData.pSrc = pTransitionBlock + sizeof(TransitionBlock);
    _ASSERTE((nStackBytes % TARGET_POINTER_SIZE) == 0);
    callDescrData.numStackSlots = nStackBytes / TARGET_POINTER_SIZE;
#ifdef CALLDESCR_ARGREGS
    callDescrData.pArgumentRegisters = (ArgumentRegisters*)(pTransitionBlock + TransitionBlock::GetOffsetOfArgumentRegisters());
#endif
#ifdef CALLDESCR_RETBUFFARGREG
    callDescrData.pRetBuffArg = (UINT64*)(pTransitionBlock + TransitionBlock::GetOffsetOfRetBuffArgReg());
#endif
#ifdef CALLDESCR_FPARGREGS
    callDescrData.pFloatArgumentRegisters = pFloatArgumentRegisters;
#endif
#ifdef CALLDESCR_REGTYPEMAP
    callDescrData.dwRegTypeMap = dwRegTypeMap;
#endif
    callDescrData.fpReturnSize = fpReturnSize;
    callDescrData.pTarget = m_pCallTarget;

#ifdef FEATURE_INTERPRETER
    if (transitionToPreemptive)
    {
        GCPreemp transitionIfILStub(transitionToPreemptive);
        DWORD* pLastError = &GetThread()->m_dwLastErrorInterp;
        CallDescrWorkerInternal(&callDescrData);
        *pLastError = GetLastError();
    }
    else
#endif // FEATURE_INTERPRETER
    {
        CallDescrWorkerWithHandler(&callDescrData);
    }

    if (pvRetBuff != NULL)
    {
        memcpyNoGCRefs(pvRetBuff, &callDescrData.returnValue, sizeof(callDescrData.returnValue));
    }

    if (pReturnValue != NULL)
    {
        _ASSERTE((DWORD)cbReturnValue <= sizeof(callDescrData.returnValue));
        memcpyNoGCRefs(pReturnValue, &callDescrData.returnValue, cbReturnValue);

#if !defined(HOST_64BIT) && BIGENDIAN
        {
            GCX_FORBID();

            if (!m_methodSig.Is64BitReturn())
            {
                pReturnValue[0] >>= 32;
            }
        }
#endif // !defined(HOST_64BIT) && BIGENDIAN
    }
}

void CallDefaultConstructor(OBJECTREF ref)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    MethodTable *pMT = ref->GetMethodTable();

    PREFIX_ASSUME(pMT != NULL);

    if (!pMT->HasDefaultConstructor())
    {
        SString ctorMethodName(SString::Utf8, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, ctorMethodName.GetUnicode());
    }

    GCPROTECT_BEGIN (ref);

    MethodDesc *pMD = pMT->GetDefaultConstructor();

    PREPARE_NONVIRTUAL_CALLSITE_USING_METHODDESC(pMD);
    DECLARE_ARGHOLDER_ARRAY(CtorArgs, 1);
    CtorArgs[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(ref);

    // Call the ctor...
    CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
    CALL_MANAGED_METHOD_NORET(CtorArgs);

    GCPROTECT_END ();
}
