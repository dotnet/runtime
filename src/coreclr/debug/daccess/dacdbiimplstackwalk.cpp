// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// DacDbiImplStackWalk.cpp
//

//
// This file contains the implementation of the stackwalking-related functions on the DacDbiInterface.
//
// ======================================================================================

#include "stdafx.h"
#include "dacdbiinterface.h"
#include "dacdbiimpl.h"
#include "excepcpu.h"

#if defined(FEATURE_COMINTEROP)
#include "comtoclrcall.h"
#include "comcallablewrapper.h"
#endif // FEATURE_COMINTEROP

typedef IDacDbiInterface::StackWalkHandle StackWalkHandle;


// Persistent data needed to do a stackwalk. This is allocated on the forDbi heap.
// It can survive across multiple DD calls.
// However, it has data structures that have raw pointers into the DAC cache, and so it must
// be re-iniatialized after each time the Dac cache is flushed.
struct StackWalkData
{
public:
    StackWalkData(Thread * pThread, Frame * pFrame, ULONG32 flags) :
        m_iterator(pThread, NULL, flags)
    {   SUPPORTS_DAC;
    }

    // Unwrap a handle to get StackWalkData instance.
    static StackWalkData * FromHandle(StackWalkHandle handle)
    {
        SUPPORTS_DAC;
        _ASSERTE(handle != NULL);
        return reinterpret_cast<StackWalkData *>(handle);
    }

    // The stackwalk iterator. This has lots of pointers into the DAC cache.
    StackFrameIterator m_iterator;

    // The context buffer, which can be pointed to by the RegDisplay.
    T_CONTEXT m_context;

    // A regdisplay used by the stackwalker.
    REGDISPLAY m_regdisplay;
};

// Helper to allocate stackwalk datastructures for given parameters.
// This is allocated on the local heap (and not via the forDbi allocator on the dac-cache), and then
// freed via code:DacDbiInterfaceImpl::DeleteStackWalk
//
// Throws on error (mainly OOM).
void AllocateStackwalk(StackWalkHandle * pHandle, Thread * pThread, Frame * pFrame, ULONG32 flags)
{
    SUPPORTS_DAC;

    StackWalkData * p = new StackWalkData(pThread, NULL, flags); // throews

    StackWalkHandle h = reinterpret_cast<StackWalkHandle>(p);
    *pHandle = h;
}
void DeleteStackwalk(StackWalkHandle pHandle)
{
    SUPPORTS_DAC;

    StackWalkData * pBuffer = (StackWalkData *) pHandle;
    _ASSERTE(pBuffer != NULL);
    delete pBuffer;
}


// Helper to get the StackFrameIterator from a Stackwalker handle
StackFrameIterator * GetIteratorFromHandle(StackWalkHandle pSFIHandle)
{
    SUPPORTS_DAC;

    StackWalkData * pBuffer = StackWalkData::FromHandle(pSFIHandle);
    return &(pBuffer->m_iterator);
}

// Helper to get a RegDisplay from a Stackwalker handle
REGDISPLAY * GetRegDisplayFromHandle(StackWalkHandle pSFIHandle)
{
    SUPPORTS_DAC;
    StackWalkData * pBuffer = StackWalkData::FromHandle(pSFIHandle);
    return &(pBuffer->m_regdisplay);
}

// Helper to get a Context buffer from a Stackwalker handle
T_CONTEXT * GetContextBufferFromHandle(StackWalkHandle pSFIHandle)
{
    SUPPORTS_DAC;
    StackWalkData * pBuffer = StackWalkData::FromHandle(pSFIHandle);
    return &(pBuffer->m_context);
}


// Create and return a stackwalker on the specified thread.
void DacDbiInterfaceImpl::CreateStackWalk(VMPTR_Thread      vmThread,
                                          DT_CONTEXT *      pInternalContextBuffer,
                                          StackWalkHandle * ppSFIHandle)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(ppSFIHandle != NULL);

    Thread * pThread = vmThread.GetDacPtr();

    // Set the stackwalk flags.  We pretty much want to stop at everything.
    DWORD dwFlags = (NOTIFY_ON_U2M_TRANSITIONS |
                     NOTIFY_ON_NO_FRAME_TRANSITIONS |
                     NOTIFY_ON_INITIAL_NATIVE_CONTEXT);

    // allocate memory for various stackwalker buffers (StackFrameIterator, RegDisplay, Context)
    AllocateStackwalk(ppSFIHandle, pThread, NULL, dwFlags);

    // initialize the CONTEXT.
    // SetStackWalk will initial the RegDisplay from this context.
    GetContext(vmThread, pInternalContextBuffer);

    // initialize the stackwalker
    SetStackWalkCurrentContext(vmThread,
                               *ppSFIHandle,
                               SET_CONTEXT_FLAG_ACTIVE_FRAME,
                               pInternalContextBuffer);
}

// Delete the stackwalk object allocated by code:AllocateStackwalk
void DacDbiInterfaceImpl::DeleteStackWalk(StackWalkHandle ppSFIHandle)
{
    DeleteStackwalk(ppSFIHandle);
}

// Get the CONTEXT of the current frame at which the stackwalker is stopped.
void DacDbiInterfaceImpl::GetStackWalkCurrentContext(StackWalkHandle pSFIHandle,
                                                     DT_CONTEXT *    pContext)
{
    DD_ENTER_MAY_THROW;

    StackFrameIterator * pIter = GetIteratorFromHandle(pSFIHandle);

    GetStackWalkCurrentContext(pIter, pContext);
}

// Internal Worker for GetStackWalkCurrentContext().
void DacDbiInterfaceImpl::GetStackWalkCurrentContext(StackFrameIterator * pIter,
                                                     DT_CONTEXT *         pContext)
{
    // convert the current REGDISPLAY to a CONTEXT
    CrawlFrame * pCF = &(pIter->m_crawl);
    UpdateContextFromRegDisp(pCF->GetRegisterSet(), reinterpret_cast<T_CONTEXT *>(pContext));
}



// Set the stackwalker to the specified CONTEXT.
void DacDbiInterfaceImpl::SetStackWalkCurrentContext(VMPTR_Thread           vmThread,
                                                     StackWalkHandle        pSFIHandle,
                                                     CorDebugSetContextFlag flag,
                                                     DT_CONTEXT *           pContext)
{
    DD_ENTER_MAY_THROW;

    StackFrameIterator * pIter = GetIteratorFromHandle(pSFIHandle);
    REGDISPLAY * pRD  = GetRegDisplayFromHandle(pSFIHandle);

#if defined(_DEBUG)
    // The caller should have checked this already.
    _ASSERTE(CheckContext(vmThread, pContext) == S_OK);
#endif  // _DEBUG

    // DD can't keep pointers back into the RS address space.
    // Allocate a context in DDImpl's memory space. DDImpl can't contain raw pointers back into
    // the client space since that may not marshal.
    T_CONTEXT * pContext2 = GetContextBufferFromHandle(pSFIHandle);
    *pContext2  = *reinterpret_cast<T_CONTEXT *>(pContext); // memcpy

    // update the REGDISPLAY with the given CONTEXT.
    // Be sure that the context is in DDImpl's memory space and not the Right-sides.
    FillRegDisplay(pRD, pContext2);
    BOOL fSuccess = pIter->ResetRegDisp(pRD, (flag == SET_CONTEXT_FLAG_ACTIVE_FRAME));
    if (!fSuccess)
    {
        // ResetRegDisp() may fail for the same reason Init() may fail, i.e.
        // because the stackwalker tries to unwind one frame ahead of time,
        // or because the stackwalker needs to filter out some frames based on the stackwalk flags.
        ThrowHR(E_FAIL);
    }
}


// Unwind the stackwalker to the next frame.
BOOL DacDbiInterfaceImpl::UnwindStackWalkFrame(StackWalkHandle pSFIHandle)
{
    DD_ENTER_MAY_THROW;

    StackFrameIterator * pIter = GetIteratorFromHandle(pSFIHandle);

    CrawlFrame * pCF = &(pIter->m_crawl);

    if ((pIter->GetFrameState() == StackFrameIterator::SFITER_INITIAL_NATIVE_CONTEXT) ||
        (pIter->GetFrameState() == StackFrameIterator::SFITER_NATIVE_MARKER_FRAME))
    {
        if (IsRuntimeUnwindableStub(GetControlPC(pCF->GetRegisterSet())))
        {
            // This is a native stack frame which the StackFrameIterator doesn't know how to unwind.
            // Use our special unwind logic.
            return UnwindRuntimeStackFrame(pIter);
        }
    }

    // On x86, we need to adjust the stack pointer for the callee parameter adjustment.
    // This requires us to save the number of bytes used for the stack parameters of the callee.
    // Thus, let's save it here before we unwind.
    DWORD cbStackParameterSize = 0;
    if (pIter->GetFrameState() == StackFrameIterator::SFITER_FRAMELESS_METHOD)
    {
        cbStackParameterSize = GetStackParameterSize(pCF->GetCodeInfo());
    }

    // If the stackwalker is invalid to begin with, we'll just say that it is at the end of the stack.
    BOOL fIsAtEndOfStack = TRUE;
    while (pIter->IsValid())
    {
        StackWalkAction swa = pIter->Next();

        if (swa == SWA_FAILED)
        {
            // The stackwalker is valid to begin with, so this must be a failure case.
            ThrowHR(E_FAIL);
        }
        else if (swa == SWA_CONTINUE)
        {
            if (pIter->GetFrameState() == StackFrameIterator::SFITER_DONE)
            {
                // We are at the end of the stack. We will break at the end of the loop and fIsAtEndOfStack
                // will be TRUE.
            }
            else if ((pIter->GetFrameState() == StackFrameIterator::SFITER_FRAME_FUNCTION) ||
                     (pIter->GetFrameState() == StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION))
            {
                // If the stackwalker is stopped at an explicit frame, unwind directly to the next frame.
                // The V3 stackwalker doesn't stop on explicit frames.
                continue;
            }
            else if (pIter->GetFrameState() == StackFrameIterator::SFITER_NO_FRAME_TRANSITION)
            {
                // No frame transitions are not exposed in V2.
                // Just continue onto the next managed stack frame.
                continue;
            }
            else
            {
                fIsAtEndOfStack = FALSE;
            }
        }
        else
        {
            UNREACHABLE();
        }

        // If we get here, then we want to stop at this current frame.
        break;
    }

    if (fIsAtEndOfStack == FALSE)
    {
        // Currently the only case where we adjust the stack pointer is at M2U transitions.
        if (pIter->GetFrameState() == StackFrameIterator::SFITER_NATIVE_MARKER_FRAME)
        {
            _ASSERTE(!pCF->IsActiveFrame());
            AdjustRegDisplayForStackParameter(pCF->GetRegisterSet(),
                                              cbStackParameterSize,
                                              pCF->IsActiveFrame(),
                                              kFromManagedToUnmanaged);
        }
    }

    return (fIsAtEndOfStack == FALSE);
}

bool g_fSkipStackCheck     = false;
bool g_fSkipStackCheckInit = false;

// Check whether the specified CONTEXT is valid.  The only check we perform right now is whether the
// SP in the specified CONTEXT is in the stack range of the thread.
HRESULT DacDbiInterfaceImpl::CheckContext(VMPTR_Thread       vmThread,
                                          const DT_CONTEXT * pContext)
{
    DD_ENTER_MAY_THROW;

    // If the SP in the CONTEXT isn't valid, then there's no point in checking.
    if ((pContext->ContextFlags & CONTEXT_CONTROL) == 0)
    {
        return S_OK;
    }

    if (!g_fSkipStackCheckInit)
    {
        g_fSkipStackCheck = (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_DbgSkipStackCheck) != 0);
        g_fSkipStackCheckInit = true;
    }

    // Skip this check if the customer has set the reg key/env var.  This is necessary for AutoCad.  They
    // enable fiber mode by calling the Win32 API ConvertThreadToFiber(), but when a managed debugger is
    // attached, they don't actually call into our hosting APIs such as SwitchInLogicalThreadState().  This
    // leads to the cached stack range on the Thread object being stale.
    if (!g_fSkipStackCheck)
    {
        // We don't have the backing store boundaries stored on the thread, but this is just
        // a sanity check anyway.
        Thread * pThread = vmThread.GetDacPtr();
        PTR_VOID sp = GetSP(reinterpret_cast<const T_CONTEXT *>(pContext));

        if ((sp < pThread->GetCachedStackLimit()) || (pThread->GetCachedStackBase() <= sp))
        {
            return CORDBG_E_NON_MATCHING_CONTEXT;
        }
    }

    return S_OK;
}

// Retrieve information about the current frame from the stackwalker.
IDacDbiInterface::FrameType DacDbiInterfaceImpl::GetStackWalkCurrentFrameInfo(StackWalkHandle        pSFIHandle,
                                                                              DebuggerIPCE_STRData * pFrameData)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pSFIHandle != NULL);

    StackFrameIterator * pIter = GetIteratorFromHandle(pSFIHandle);

    FrameType ftResult = kInvalid;
    if (pIter->GetFrameState() == StackFrameIterator::SFITER_DONE)
    {
        _ASSERTE(!pIter->IsValid());
        ftResult = kAtEndOfStack;
    }
    else
    {
        BOOL fInitFrameData = FALSE;
        switch (pIter->GetFrameState())
        {
            case StackFrameIterator::SFITER_UNINITIALIZED:
                ftResult = kInvalid;
                break;

            case StackFrameIterator::SFITER_FRAMELESS_METHOD:
                ftResult = kManagedStackFrame;
                fInitFrameData = TRUE;
                break;

            case StackFrameIterator::SFITER_FRAME_FUNCTION:
                //
                // fall through
                //
            case StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION:
                ftResult = kExplicitFrame;
                fInitFrameData = TRUE;
                break;

            case StackFrameIterator::SFITER_NO_FRAME_TRANSITION:
                // no-frame transition represents an ExInfo for a native exception on x86.
                // For all intents and purposes this should be treated just like another explicit frame.
                ftResult = kExplicitFrame;
                fInitFrameData = TRUE;
                break;

            case StackFrameIterator::SFITER_NATIVE_MARKER_FRAME:
                //
                // fall through
                //
            case StackFrameIterator::SFITER_INITIAL_NATIVE_CONTEXT:
                if (IsRuntimeUnwindableStub(GetControlPC(pIter->m_crawl.GetRegisterSet())))
                {
                    ftResult = kNativeRuntimeUnwindableStackFrame;
                    fInitFrameData = TRUE;
                }
                else
                {
                    ftResult = kNativeStackFrame;
                }
                break;

            default:
                UNREACHABLE();
        }

        if ((fInitFrameData == TRUE) && (pFrameData != NULL))
        {
            InitFrameData(pIter, ftResult, pFrameData);
        }
    }

    return ftResult;
}

//---------------------------------------------------------------------------------------
//
// Return the number of internal frames on the specified thread.
//
// Arguments:
//    vmThread - the thread to be walked
//
// Return Value:
//    Return the number of interesting internal frames on the thread.
//
// Notes:
//    Internal frames are interesting if they are not of type STUBFRAME_NONE.
//

ULONG32 DacDbiInterfaceImpl::GetCountOfInternalFrames(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    Frame *  pFrame  = pThread->GetFrame();

    // We could call EnumerateInternalFrames() here, but it would be a lot of overhead for what we need.
    ULONG32 uCount = 0;
    while (pFrame != FRAME_TOP)
    {
        CorDebugInternalFrameType ift = GetInternalFrameType(pFrame);
        if (ift != STUBFRAME_NONE)
        {
            uCount++;
        }
        pFrame = pFrame->Next();
    }
    return uCount;
}

//---------------------------------------------------------------------------------------
//
// Enumerate the internal frames on the specified thread and invoke the provided callback on each of them.
//
// Arguments:
//    vmThread   - the thread to be walked
//    fpCallback - callback function to be invoked for each interesting internal frame
//    pUserData  - user-defined custom data to be passed to the callback
//

void DacDbiInterfaceImpl::EnumerateInternalFrames(VMPTR_Thread                           vmThread,
                                                  FP_INTERNAL_FRAME_ENUMERATION_CALLBACK fpCallback,
                                                  void *                                 pUserData)
{
    DD_ENTER_MAY_THROW;

    DebuggerIPCE_STRData frameData;

    Thread *    pThread    = vmThread.GetDacPtr();
    Frame *     pFrame     = pThread->GetFrame();
    AppDomain * pAppDomain = pThread->GetDomain(INDEBUG(TRUE));

    // This used to be only true for Enter-Managed chains.
    // Since we don't have chains anymore, this can always be false.
    frameData.quicklyUnwound = false;
    frameData.eType = DebuggerIPCE_STRData::cStubFrame;

    while (pFrame != FRAME_TOP)
    {
        // check if the internal frame is interesting
        frameData.stubFrame.frameType = GetInternalFrameType(pFrame);
        if (frameData.stubFrame.frameType != STUBFRAME_NONE)
        {
            frameData.fp = FramePointer::MakeFramePointer(PTR_HOST_TO_TADDR(pFrame));

            frameData.vmCurrentAppDomainToken.SetHostPtr(pAppDomain);

            MethodDesc * pMD = pFrame->GetFunction();
#if defined(FEATURE_COMINTEROP)
            if (frameData.stubFrame.frameType == STUBFRAME_U2M)
            {
                _ASSERTE(pMD == NULL);

                // U2M transition frame generally don't store the target MD because we know what the target
                // is by looking at the callee stack frame.  However, for reverse COM interop, we can try
                // to get the MD for the interface.
                //
                // Note that some reverse COM interop cases don't have an intermediate interface MD, so
                // pMD may still be NULL.
                //
                // Even if there is an MD on the ComMethodFrame, it could be in a different appdomain than
                // the ComMethodFrame itself.  The only known scenario is a cross-appdomain reverse COM
                // interop call.  We need to check for this case.  The end result is that GetFunction() and
                // GetFunctionToken() on ICDInternalFrame will return NULL.

                // Minidumps without full memory don't guarantee to capture the CCW since we can do without
                // it.  In this case, pMD will remain NULL.
                EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
                {
                    if (pFrame->GetVTablePtr() == ComMethodFrame::GetMethodFrameVPtr())
                    {
                        ComMethodFrame * pCOMFrame = dac_cast<PTR_ComMethodFrame>(pFrame);
                        PTR_VOID pUnkStackSlot     = pCOMFrame->GetPointerToArguments();
                        PTR_IUnknown pUnk          = dac_cast<PTR_IUnknown>(*dac_cast<PTR_TADDR>(pUnkStackSlot));
                        ComCallWrapper * pCCW      = ComCallWrapper::GetWrapperFromIP(pUnk);

                        ComCallMethodDesc * pCMD = NULL;
                        pCMD = dac_cast<PTR_ComCallMethodDesc>(pCOMFrame->ComMethodFrame::GetDatum());
                        pMD  = pCMD->GetInterfaceMethodDesc();
                    }
                }
                EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY
            }
#endif // FEATURE_COMINTEROP

            Module *     pModule = (pMD ? pMD->GetModule() : NULL);
            DomainAssembly * pDomainAssembly = (pModule ? pModule->GetDomainAssembly() : NULL);

            if (frameData.stubFrame.frameType == STUBFRAME_FUNC_EVAL)
            {
                FuncEvalFrame * pFEF = dac_cast<PTR_FuncEvalFrame>(pFrame);
                DebuggerEval *  pDE  = pFEF->GetDebuggerEval();

                frameData.stubFrame.funcMetadataToken = pDE->m_methodToken;
                frameData.stubFrame.vmDomainAssembly.SetHostPtr(
                    pDE->m_debuggerModule ? pDE->m_debuggerModule->GetDomainAssembly() : NULL);
                frameData.stubFrame.vmMethodDesc = VMPTR_MethodDesc::NullPtr();
            }
            else
            {
                frameData.stubFrame.funcMetadataToken = (pMD == NULL ? NULL : pMD->GetMemberDef());
                frameData.stubFrame.vmDomainAssembly.SetHostPtr(pDomainAssembly);
                frameData.stubFrame.vmMethodDesc.SetHostPtr(pMD);
            }

            // invoke the callback
            fpCallback(&frameData, pUserData);
        }

        // move on to the next internal frame
        pFrame = pFrame->Next();
    }
}

// Given the FramePointer of the parent frame and the FramePointer of the current frame,
// check if the current frame is the parent frame.
BOOL DacDbiInterfaceImpl::IsMatchingParentFrame(FramePointer fpToCheck, FramePointer fpParent)
{
    DD_ENTER_MAY_THROW;

#ifdef FEATURE_EH_FUNCLETS
    StackFrame sfToCheck = StackFrame((UINT_PTR)fpToCheck.GetSPValue());

    StackFrame sfParent  = StackFrame((UINT_PTR)fpParent.GetSPValue());

    // Ask the ExceptionTracker to figure out the answer.
    // Don't try to compare the StackFrames/FramePointers ourselves.
    return ExceptionTracker::IsUnwoundToTargetParentFrame(sfToCheck, sfParent);

#else // !FEATURE_EH_FUNCLETS
    return FALSE;

#endif // FEATURE_EH_FUNCLETS
}

// Return the stack parameter size of the given method.
ULONG32 DacDbiInterfaceImpl::GetStackParameterSize(CORDB_ADDRESS controlPC)
{
    DD_ENTER_MAY_THROW;

    PCODE currentPC = PCODE(controlPC);

    EECodeInfo codeInfo(currentPC);
    return GetStackParameterSize(&codeInfo);
}

// Return the FramePointer of the current frame at which the stackwalker is stopped.
FramePointer DacDbiInterfaceImpl::GetFramePointer(StackWalkHandle pSFIHandle)
{
    DD_ENTER_MAY_THROW;

    StackFrameIterator * pIter = GetIteratorFromHandle(pSFIHandle);
    return GetFramePointerWorker(pIter);
}

// Internal helper for GetFramePointer.
FramePointer DacDbiInterfaceImpl::GetFramePointerWorker(StackFrameIterator * pIter)
{
    CrawlFrame * pCF = &(pIter->m_crawl);
    REGDISPLAY * pRD = pCF->GetRegisterSet();

    FramePointer fp;
    switch (pIter->GetFrameState())
    {
        // For managed methods, we have the full CONTEXT.  Additionally, we also have the caller CONTEXT
        // on WIN64.
        case StackFrameIterator::SFITER_FRAMELESS_METHOD:
            fp = FramePointer::MakeFramePointer(GetRegdisplayStackMark(pRD));
            break;

        // In these cases, we only have the full CONTEXT, not the caller CONTEXT.
        case StackFrameIterator::SFITER_NATIVE_MARKER_FRAME:
            //
            // fall through
            //
        case StackFrameIterator::SFITER_INITIAL_NATIVE_CONTEXT:
            fp = FramePointer::MakeFramePointer(GetRegdisplayStackMark(pRD));
            break;

        // In these cases, we use the address of the explicit frame as the frame marker.
        case StackFrameIterator::SFITER_FRAME_FUNCTION:
            //
            // fall through
            //
        case StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION:
            fp = FramePointer::MakeFramePointer(PTR_HOST_TO_TADDR(pCF->GetFrame()));
            break;

        // No-frame transition represents an ExInfo for a native exception on x86.
        // For all intents and purposes this should be treated just like another explicit frame.
        case StackFrameIterator::SFITER_NO_FRAME_TRANSITION:
            fp = FramePointer::MakeFramePointer(pCF->GetNoFrameTransitionMarker());
            break;

        case StackFrameIterator::SFITER_UNINITIALIZED:
            //
            // fall through
            //
        default:
            UNREACHABLE();
    }

    return fp;
}

// Return TRUE if the specified CONTEXT is the CONTEXT of the leaf frame.
// @dbgtodo  filter CONTEXT - Currently we check for the filter CONTEXT first.
BOOL DacDbiInterfaceImpl::IsLeafFrame(VMPTR_Thread       vmThread,
                                      const DT_CONTEXT * pContext)
{
    DD_ENTER_MAY_THROW;

    DT_CONTEXT ctxLeaf;
    GetContext(vmThread, &ctxLeaf);

    // Call a platform-specific helper to compare the two contexts.
    return CompareControlRegisters(pContext, &ctxLeaf);
}

// This is a simple helper function to convert a CONTEXT to a DebuggerREGDISPLAY.  We need to do this
// inside DDI because the RS has no notion of REGDISPLAY.
void DacDbiInterfaceImpl::ConvertContextToDebuggerRegDisplay(const DT_CONTEXT * pInContext,
                                                             DebuggerREGDISPLAY * pOutDRD,
                                                             BOOL fActive)
{
    DD_ENTER_MAY_THROW;

    // This is a bit cumbersome.  First we need to convert the CONTEXT into a REGDISPLAY.  Then we need
    // to convert the REGDISPLAY to a DebuggerREGDISPLAY.
    REGDISPLAY rd;
    FillRegDisplay(&rd, reinterpret_cast<T_CONTEXT *>(const_cast<DT_CONTEXT *>(pInContext)));
    SetDebuggerREGDISPLAYFromREGDISPLAY(pOutDRD, &rd);
}

//---------------------------------------------------------------------------------------
//
// Fill in the structure with information about the current frame at which the stackwalker is stopped.
//
// Arguments:
//    pIter      - the stackwalker
//    pFrameData - the structure to be filled out
//

void DacDbiInterfaceImpl::InitFrameData(StackFrameIterator *   pIter,
                                        FrameType              ft,
                                        DebuggerIPCE_STRData * pFrameData)
{
    CrawlFrame * pCF = &(pIter->m_crawl);

    //
    // do common initialization of DebuggerIPCE_STRData for both managed stack frames and explicit frames
    //

    pFrameData->fp = GetFramePointerWorker(pIter);

    // This used to be only true for Enter-Managed chains.
    // Since we don't have chains anymore, this can always be false.
    pFrameData->quicklyUnwound = false;

    pFrameData->vmCurrentAppDomainToken.SetHostPtr(AppDomain::GetCurrentDomain());

    if (ft == kNativeRuntimeUnwindableStackFrame)
    {
        pFrameData->eType = DebuggerIPCE_STRData::cRuntimeNativeFrame;

        GetStackWalkCurrentContext(pIter, &(pFrameData->ctx));
    }
    else if (ft == kManagedStackFrame)
    {
        MethodDesc * pMD = pCF->GetFunction();
        Module *     pModule = (pMD ? pMD->GetModule() : NULL);
        // Although MiniDumpNormal tries to dump all AppDomains, it's possible
        // target corruption will keep one from being present.  This should mean
        // we'll just fail later, but struggle on for now.
        DomainAssembly *pDomainAssembly = NULL;
        EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
        {
            pDomainAssembly = (pModule ? pModule->GetDomainAssembly() : NULL);
            _ASSERTE(pDomainAssembly != NULL);
        }
        EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY

        //
        // This is a managed stack frame.
        //

        _ASSERTE(pMD != NULL);
        _ASSERTE(pModule != NULL);

        //
        // initialize the rest of the DebuggerIPCE_STRData
        //

        pFrameData->eType = DebuggerIPCE_STRData::cMethodFrame;

        SetDebuggerREGDISPLAYFromREGDISPLAY(&(pFrameData->rd), pCF->GetRegisterSet());

        GetStackWalkCurrentContext(pIter, &(pFrameData->ctx));

        //
        // initialize the fields in DebuggerIPCE_STRData::v
        //

        // These fields will be filled in later.  We don't have the sequence point mapping information here.
        pFrameData->v.ILOffset = (SIZE_T)(-1);
        pFrameData->v.mapping  = MAPPING_NO_INFO;

        // Check if this is a vararg method by getting the managed calling convention from the signature.
        // Strictly speaking, we can do this in CordbJITILFrame::Init(), but it's just easier and more
        // efficiently to do it here.  CordbJITILFrame::Init() will initialize the other vararg-related
        // fields.  We don't have the native var info here to fully initialize everything.
        pFrameData->v.fVarArgs = (pMD->IsVarArg() == TRUE);

        pFrameData->v.fNoMetadata = (pMD->IsNoMetadata() == TRUE);

        pFrameData->v.taAmbientESP = pCF->GetAmbientSPFromCrawlFrame();
        if (pMD->IsSharedByGenericInstantiations())
        {
            // This method has a generic type token which is required to figure out the exact instantiation
            // of the method.  CrawlFrame::GetExactGenericArgsToken() can't always successfully retrieve
            // the token because the JIT doesn't generate the required information all the time.  As such,
            // we need to save the variable index of the generic type token in order to do the look up later.
            ALLOW_DATATARGET_MISSING_MEMORY(
                pFrameData->v.exactGenericArgsToken = (GENERICS_TYPE_TOKEN)(dac_cast<TADDR>(pCF->GetExactGenericArgsToken()));
            );

            if (pMD->AcquiresInstMethodTableFromThis())
            {
                // The generic type token is the "this" object.
                pFrameData->v.dwExactGenericArgsTokenIndex = 0;
            }
            else
            {
                // The generic type token is one of the secret arguments.
                pFrameData->v.dwExactGenericArgsTokenIndex = (DWORD)ICorDebugInfo::TYPECTXT_ILNUM;
            }
        }
        else
        {
            pFrameData->v.exactGenericArgsToken = NULL;
            pFrameData->v.dwExactGenericArgsTokenIndex = (DWORD)ICorDebugInfo::MAX_ILNUM;
        }

        //
        // initialize the DebuggerIPCE_FuncData and DebuggerIPCE_JITFuncData
        //

        DebuggerIPCE_FuncData *    pFuncData    = &(pFrameData->v.funcData);
        DebuggerIPCE_JITFuncData * pJITFuncData = &(pFrameData->v.jitFuncData);

        //
        // initialize the "easy" fields of DebuggerIPCE_FuncData
        //

        pFuncData->funcMetadataToken = pMD->GetMemberDef();
        pFuncData->vmDomainAssembly.SetHostPtr(pDomainAssembly);

        // PERF: this is expensive to get so I stopped fetching it eagerly
        // It is only needed if we haven't already got a cached copy
        pFuncData->classMetadataToken = mdTokenNil;

        //
        // initialize the remaining fields of DebuggerIPCE_FuncData to the default values
        //

        pFuncData->ilStartAddress = NULL;
        pFuncData->ilSize = 0;
        pFuncData->currentEnCVersion = CorDB_DEFAULT_ENC_FUNCTION_VERSION;
        pFuncData->localVarSigToken = mdSignatureNil;

        //
        // inititalize the fields of DebuggerIPCE_JITFuncData
        //

        // For MiniDumpNormal, we do not guarantee method region info for all JIT tokens
        // is present in the dump.
        ALLOW_DATATARGET_MISSING_MEMORY(
            pJITFuncData->nativeStartAddressPtr = PCODEToPINSTR(pCF->GetCodeInfo()->GetStartAddress());
        );

        // PERF: this is expensive to get so I stopped fetching it eagerly
        // It is only needed if we haven't already got a cached copy
        pJITFuncData->nativeHotSize = 0;
        pJITFuncData->nativeStartAddressColdPtr = 0;
        pJITFuncData->nativeColdSize = 0;

        pJITFuncData->nativeOffset = pCF->GetRelOffset();

        // Here we detect (and set the appropriate flag) if the nativeOffset in the current frame points to the return address of IL_Throw()
        // (or other exception related JIT helpers like IL_Throw, IL_Rethrow, JIT_RngChkFail, IL_VerificationError, JIT_Overflow etc).
        // Since return addres point to the next(!) instruction after [call IL_Throw] this sometimes can lead to incorrect exception stacktraces
        // where a next source line is spotted as an exception origin. This happens when the next instruction after [call IL_Throw] belongs to
        // a sequence point and a source line different from a sequence point and a source line of [call IL_Throw].
        // Later on this flag is used in order to adjust nativeOffset and make ICorDebugILFrame::GetIP return IL offset withing
        // the same sequence point as an actuall IL throw instruction.

        // Here is how we detect it:
        // We can assume that nativeOffset points to an the instruction after [call IL_Throw] when these conditioins are met:
        //  1. pCF->IsInterrupted() - Exception has been thrown by this managed frame (frame attr FRAME_ATTR_EXCEPTION)
        //  2. !pCF->HasFaulted() - It wasn't a "hardware" exception (Access violation, dev by 0, etc.)
        //  3. !pCF->IsIPadjusted() - It hasn't been previously adjusted to point to [call IL_Throw]
        //  4. pJITFuncData->nativeOffset != 0 - nativeOffset contains something that looks like a real return address.
        pJITFuncData->jsutAfterILThrow = pCF->IsInterrupted()
                                     && !pCF->HasFaulted()
                                     && !pCF->IsIPadjusted()
                                     && pJITFuncData->nativeOffset != 0;

        pJITFuncData->nativeCodeJITInfoToken.Set(NULL);
        pJITFuncData->vmNativeCodeMethodDescToken.SetHostPtr(pMD);

        InitParentFrameInfo(pCF, pJITFuncData);

        ALLOW_DATATARGET_MISSING_MEMORY(
            pJITFuncData->isInstantiatedGeneric = pMD->HasClassOrMethodInstantiation();
        );
        pJITFuncData->enCVersion = CorDB_DEFAULT_ENC_FUNCTION_VERSION;

        // PERF: this is expensive to get so I stopped fetching it eagerly
        // It is only needed if we haven't already got a cached copy
        pFuncData->localVarSigToken = 0;
        pFuncData->ilStartAddress = 0;
        pFuncData->ilSize = 0;


        // See the comment for LookupEnCVersions().
        // PERF: this is expensive to get so I stopped fetching it eagerly
        pFuncData->currentEnCVersion = 0;
        pJITFuncData->enCVersion = 0;
    }
    else
    {
        _ASSERTE(!"DDII::InitFrameData() - We should never stop at internal frames.");
        ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
    }
}

//---------------------------------------------------------------------------------------
//
// Initialize the address and the size of the jitted code, including both hot and cold regions.
//
// Arguments:
//    methodToken  - METHODTOKEN of the method in question; this should actually be the CodeHeader address
//    pJITFuncData - structure to be filled out
//

void DacDbiInterfaceImpl::InitNativeCodeAddrAndSize(TADDR                      taStartAddr,
                                                    DebuggerIPCE_JITFuncData * pJITFuncData)
{
    PTR_CORDB_ADDRESS_TYPE pAddr = dac_cast<PTR_CORDB_ADDRESS_TYPE>(taStartAddr);
    CodeRegionInfo crInfo = CodeRegionInfo::GetCodeRegionInfo(NULL, NULL, pAddr);

    pJITFuncData->nativeStartAddressPtr = PCODEToPINSTR(crInfo.getAddrOfHotCode());
    pJITFuncData->nativeHotSize = crInfo.getSizeOfHotCode();

    pJITFuncData->nativeStartAddressColdPtr = PCODEToPINSTR(crInfo.getAddrOfColdCode());
    pJITFuncData->nativeColdSize = crInfo.getSizeOfColdCode();
}

//---------------------------------------------------------------------------------------
//
// Initialize the funclet-related fields of DebuggerIPCE_JITFuncData.  This is an nop on non-WIN64 platforms.
//
// Arguments:
//    pCF          - the CrawlFrame for the current frame
//    pJITFuncData - the structure to be filled out
//

void DacDbiInterfaceImpl::InitParentFrameInfo(CrawlFrame * pCF,
                                              DebuggerIPCE_JITFuncData * pJITFuncData)
{
#ifdef FEATURE_EH_FUNCLETS
    pJITFuncData->fIsFilterFrame = pCF->IsFilterFunclet();

    if (pCF->IsFunclet())
    {
        DWORD dwParentOffset;
        StackFrame sfParent = ExceptionTracker::FindParentStackFrameEx(pCF, &dwParentOffset, NULL);

        //
        // For funclets, fpParentOrSelf is the FramePointer of the parent.
        // Don't mess around with this FramePointer.  The only thing we can do with it is to pass it back
        // to the ExceptionTracker when we are checking if a particular frame is the parent frame.
        //

        pJITFuncData->fpParentOrSelf = FramePointer::MakeFramePointer(sfParent.SP);
        pJITFuncData->parentNativeOffset = dwParentOffset;
    }
    else
    {
        StackFrame sfSelf = ExceptionTracker::GetStackFrameForParentCheck(pCF);

        //
        // For non-funclets, fpParentOrSelf is the FramePointer of the current frame itself.
        // Don't mess around with this FramePointer.  The only thing we can do with it is to pass it back
        // to the ExceptionTracker when we are checking if a particular frame is the parent frame.
        //

        pJITFuncData->fpParentOrSelf = FramePointer::MakeFramePointer(sfSelf.SP);
        pJITFuncData->parentNativeOffset = 0;
    }
#endif // FEATURE_EH_FUNCLETS
}

// Return the stack parameter size of the given method.
// Refer to the full comment for the overloaded version.
ULONG32 DacDbiInterfaceImpl::GetStackParameterSize(EECodeInfo * pCodeInfo)
{
    return pCodeInfo->GetCodeManager()->GetStackParameterSize(pCodeInfo);
}


//---------------------------------------------------------------------------------------
//
// Adjust the stack pointer in the CONTEXT for the stack parameters.
// This is a nop on non-x86 platforms.
//
// Arguments:
//    pRD                      - the REGDISPLAY to be adjusted
//    cbStackParameterSize     - the number of bytes for the stack parameters
//    fIsActiveFrame           - whether the CONTEXT is for an active frame
//    StackAdjustmentDirection - whether we are changing a CONTEXT from the managed convention
//                               to the unmanaged convention
//
// Notes:
//    Consider this code:
//
//       push 1
//       push 2
//       call Foo
//    -> inc eax
//
//    Here we are assuming that the return instruction in Foo() pops the stack arguments.
//
//    Suppose the IP in the CONTEXT is at the arrow.  The question is, where should the stack pointer be?
//
//    0x0   ret addr for Foo
//    0x4   2
//    0x8   1
//    0xc   .....
//
//    If the CONTEXT is the active frame, i.e. the IP is the active instruction,
//    not the instruction at the return address, then the SP should be at 0xc.
//    However, if the CONTEXT is not active, then the SP can be at either 0x4 or 0xc, depending on
//    the convention used by the stackwalker.  The managed stackwalker reports 0xc, but dbghelp reports
//    0x4.  To bridge the gap we have to shim it in the DDI.
//
//    Currently, we have no way to reliably shim the CONTEXT in all cases.  Consider this stack,
//    where U* are native stack frames and M* are managed stack frames:
//
//    [leaf]
//    U2
//    U1
//    ------- (M2U transition)
//    M2
//    M1
//    M0
//    ------- (U2M transition)
//    U0
//    [root]
//
//    There are only two transition cases where we can reliably adjust for the callee stack parameter size:
//    1) when the debugger calls SetContext() with the CONTEXT of the first managed stack frame in a
//       managed stack chain (i.e. SetContext() with M2's CONTEXT)
//      - the M2U transition is protected by an explicit frame (aka Frame-chain frame)
//    2) when the debugger calls GetContext() on the first native stack frame in a native stack chain
//       (i.e. GetContext() at U0)
//      - we unwind from M0 to U0, so we know the stack parameter size of M0
//
//    If we want to do the adjustment in all cases, we need to ask the JIT to store the callee stack
//    parameter size in either the unwind info.
//

void DacDbiInterfaceImpl::AdjustRegDisplayForStackParameter(REGDISPLAY *             pRD,
                                                            DWORD                    cbStackParameterSize,
                                                            BOOL                     fIsActiveFrame,
                                                            StackAdjustmentDirection direction)
{
#if defined(TARGET_X86)
    // If the CONTEXT is active then no adjustment is needed.
    if (!fIsActiveFrame)
    {
        UINT_PTR sp = GetRegdisplaySP(pRD);
        if (direction == kFromManagedToUnmanaged)
        {
            // The CONTEXT comes from the managed world.
            sp -= cbStackParameterSize;
        }
        else
        {
            _ASSERTE(!"Currently, we should not hit this case.\n");

            // The CONTEXT comes from the unmanaged world.
            sp += cbStackParameterSize;
        }
        SetRegdisplaySP(pRD, reinterpret_cast<LPVOID>(sp));
    }
#endif // TARGET_X86
}

//---------------------------------------------------------------------------------------
//
// Given an explicit frame, return its frame type in terms of CorDebugInternalFrameType.
//
// Arguments:
//    pFrame - the explicit frame in question
//
// Return Value:
//    Return the CorDebugInternalFrameType of the explicit frame
//
// Notes:
//    I wish this function were simpler, but it's not.  The logic in this function is adopted
//    from the logic in the old in-proc debugger stackwalker.
//

CorDebugInternalFrameType DacDbiInterfaceImpl::GetInternalFrameType(Frame * pFrame)
{
    CorDebugInternalFrameType resultType = STUBFRAME_NONE;

    Frame::ETransitionType tt = pFrame->GetTransitionType();
    Frame::Interception it = pFrame->GetInterception();
    int ft = pFrame->GetFrameType();

    switch (tt)
    {
        case Frame::TT_NONE:
            if (it == Frame::INTERCEPTION_CLASS_INIT)
            {
                resultType = STUBFRAME_CLASS_INIT;
            }
            else if (it == Frame::INTERCEPTION_EXCEPTION)
            {
                resultType = STUBFRAME_EXCEPTION;
            }
            else if (it == Frame::INTERCEPTION_SECURITY)
            {
                resultType = STUBFRAME_SECURITY;
            }
            else if (it == Frame::INTERCEPTION_PRESTUB)
            {
                resultType = STUBFRAME_JIT_COMPILATION;
            }
            else
            {
                if (ft == Frame::TYPE_FUNC_EVAL)
                {
                    resultType = STUBFRAME_FUNC_EVAL;
                }
                else if (ft == Frame::TYPE_EXIT)
                {
                    if ((pFrame->GetVTablePtr() != InlinedCallFrame::GetMethodFrameVPtr()) ||
                        InlinedCallFrame::FrameHasActiveCall(pFrame))
                    {
                        resultType = STUBFRAME_M2U;
                    }
                }
            }
            break;

        case Frame::TT_M2U:
            // Refer to the comment in DebuggerWalkStackProc() for StubDispatchFrame.
            if (pFrame->GetVTablePtr() != StubDispatchFrame::GetMethodFrameVPtr())
            {
                if (it == Frame::INTERCEPTION_SECURITY)
                {
                    resultType = STUBFRAME_SECURITY;
                }
                else
                {
                    resultType = STUBFRAME_M2U;
                }
            }
            break;

        case Frame::TT_U2M:
            resultType = STUBFRAME_U2M;
            break;

        case Frame::TT_AppDomain:
            resultType = STUBFRAME_APPDOMAIN_TRANSITION;
            break;

        case Frame::TT_InternalCall:
            if (it == Frame::INTERCEPTION_EXCEPTION)
            {
                resultType = STUBFRAME_EXCEPTION;
            }
            else
            {
                resultType = STUBFRAME_INTERNALCALL;
            }
            break;

        default:
            UNREACHABLE();
            break;
    }

    return resultType;
}

//---------------------------------------------------------------------------------------
//
// This is just a simpler helper function to convert a REGDISPLAY to a CONTEXT.
//
// Arguments:
//    pRegDisp - the REGDISPLAY to be converted
//    pContext - the buffer for storing the converted CONTEXT
//

void DacDbiInterfaceImpl::UpdateContextFromRegDisp(REGDISPLAY * pRegDisp,
                                                   T_CONTEXT *  pContext)
{
#if defined(TARGET_X86) && !defined(FEATURE_EH_FUNCLETS)
    // Do a partial copy first.
    pContext->ContextFlags = (CONTEXT_INTEGER | CONTEXT_CONTROL);

    pContext->Edi = *pRegDisp->GetEdiLocation();
    pContext->Esi = *pRegDisp->GetEsiLocation();
    pContext->Ebx = *pRegDisp->GetEbxLocation();
    pContext->Ebp = *pRegDisp->GetEbpLocation();
    pContext->Eax = *pRegDisp->GetEaxLocation();
    pContext->Ecx = *pRegDisp->GetEcxLocation();
    pContext->Edx = *pRegDisp->GetEdxLocation();
    pContext->Esp = pRegDisp->SP;
    pContext->Eip = pRegDisp->ControlPC;

    // If we still have the pointer to the leaf CONTEXT, and the leaf CONTEXT is the same as the CONTEXT for
    // the current frame (i.e. the stackwalker is at the leaf frame), then we do a full copy.
    if ((pRegDisp->pContext != NULL) &&
        (CompareControlRegisters(const_cast<const DT_CONTEXT *>(reinterpret_cast<DT_CONTEXT *>(pContext)),
                                 const_cast<const DT_CONTEXT *>(reinterpret_cast<DT_CONTEXT *>(pRegDisp->pContext)))))
    {
        *pContext = *pRegDisp->pContext;
    }
#else // TARGET_X86 && !FEATURE_EH_FUNCLETS
    *pContext = *pRegDisp->pCurrentContext;
#endif // !TARGET_X86 || FEATURE_EH_FUNCLETS
}

//---------------------------------------------------------------------------------------
//
// Given the REGDISPLAY of a stack frame for one of the redirect functions, retrieve the original CONTEXT
// before the thread redirection.
//
// Arguments:
//    pRD - the REGDISPLAY of the stack frame in question
//
// Return Value:
//    Return the original CONTEXT before the thread got redirected.
//
// Assumptions:
//    The caller has checked that the REGDISPLAY is indeed for one of the redirect functions.
//

PTR_CONTEXT DacDbiInterfaceImpl::RetrieveHijackedContext(REGDISPLAY * pRD)
{
    CORDB_ADDRESS ContextPointerAddr = NULL;

    TADDR controlPC = PCODEToPINSTR(GetControlPC(pRD));

    // Check which thread redirection mechanism is used.
    if (g_pDebugger->m_rgHijackFunction[Debugger::kUnhandledException].IsInRange(controlPC))
    {
        //  The thread is redirected because of an unhandled exception.

        // The CONTEXT pointer is the last thing pushed onto the stack.
        // So just read the stack slot at ESP.  That will be the TADDR to the CONTEXT.
        ContextPointerAddr = PTR_TO_CORDB_ADDRESS(GetRegdisplaySP(pRD));

        // Read the CONTEXT from OOP.
        return *dac_cast<PTR_PTR_CONTEXT>((TADDR)ContextPointerAddr);
    }
    else
    {
        // The thread is redirected by the EE via code:Thread::RedirectThreadAtHandledJITCase.

        // Convert the REGDISPLAY to a CONTEXT;
        T_CONTEXT * pContext = NULL;

#if defined(TARGET_X86)
        T_CONTEXT ctx;
        pContext = &ctx;
        UpdateContextFromRegDisp(pRD, pContext);
#else
        pContext = pRD->pCurrentContext;
#endif

        // Retrieve the original CONTEXT.
        return GetCONTEXTFromRedirectedStubStackFrame(pContext);
    }
}

//---------------------------------------------------------------------------------------
//
// Unwind special native stack frame which the runtime knows how to unwind.
//
// Arguments:
//    pIter - the StackFrameIterator we are currently using to walk the stack
//
// Return Value:
//    Return TRUE if there are more frames to walk, i.e. if we are NOT at the end of the stack.
//
// Assumptions:
//    pIter is currently stopped at a special stub which the runtime knows how to unwind.
//
// Notes:
//    * Refer to code:DacDbiInterfaceImpl::IsRuntimeUnwindableStub to see how we determine whether a control
//        PC is in a runtime-unwindable stub
//

BOOL DacDbiInterfaceImpl::UnwindRuntimeStackFrame(StackFrameIterator * pIter)
{
    _ASSERTE(IsRuntimeUnwindableStub(GetControlPC(pIter->m_crawl.GetRegisterSet())));

    T_CONTEXT *    pContext = NULL;
    REGDISPLAY * pRD = pIter->m_crawl.GetRegisterSet();

    //
    // Retrieve the CONTEXT to unwind to and unwind the REGDISPLAY.
    //
    pContext = RetrieveHijackedContext(pRD);

    FillRegDisplay(pRD, pContext);

    // Update the StackFrameIterator.
    BOOL fSuccess = pIter->ResetRegDisp(pRD, true);
    if (!fSuccess)
    {
        // ResetRegDisp() may fail for the same reason Init() may fail, i.e.
        // because the stackwalker tries to unwind one frame ahead of time,
        // or because the stackwalker needs to filter out some frames based on the stackwalk flags.
        ThrowHR(E_FAIL);
    }

    // Currently we only unwind the hijack function, which will never be the last stack frame.
    // So return TRUE to indicate that this is not the end of stack.
    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// To aid in doing the stack walk, the shim needs to know if either TS_SyncSuspended or
// TS_Hijacked is set on a given thread. This DAC helper provides that access.
//
// Arguments:
//    vmThread - Thread on which to check the TS_SyncSuspended & TS_Hijacked states
//
// Return Value:
//    Return true iff TS_SyncSuspended or TS_Hijacked is set on the specified thread.
//

bool DacDbiInterfaceImpl::IsThreadSuspendedOrHijacked(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    Thread::ThreadState ts = pThread->GetSnapshotState();
    if ((ts & Thread::TS_SyncSuspended) != 0)
    {
        return true;
    }

#ifdef FEATURE_HIJACK
    if ((ts & Thread::TS_Hijacked) != 0)
    {
        return true;
    }
#endif

    return false;
}
