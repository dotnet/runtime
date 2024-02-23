// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ShimStackWalk.cpp
//

//
// This file contains the implementation of the Arrowhead stackwalking shim.  This shim builds on top of
// the public Arrowhead ICD stackwalking API, and it is intended to be backward-compatible with the existing
// debuggers using the V2.0 ICD API.
//
// ======================================================================================

#include "stdafx.h"
#include "primitives.h"

#if defined(TARGET_X86)
static const ULONG32 REGISTER_X86_MAX  = REGISTER_X86_FPSTACK_7 + 1;
static const ULONG32 MAX_MASK_COUNT    = (REGISTER_X86_MAX + 7) >> 3;
#elif defined(TARGET_AMD64)
static const ULONG32 REGISTER_AMD64_MAX = REGISTER_AMD64_XMM15 + 1;
static const ULONG32 MAX_MASK_COUNT     = (REGISTER_AMD64_MAX + 7) >> 3;
#endif

ShimStackWalk::ShimStackWalk(ShimProcess * pProcess, ICorDebugThread * pThread)
  : m_pChainEnumList(NULL),
    m_pFrameEnumList(NULL)
{
    // The following assignments increment the ref count.
    m_pProcess.Assign(pProcess);
    m_pThread.Assign(pThread);

    Populate();
}

ShimStackWalk::~ShimStackWalk()
{
    Clear();
}

// ----------------------------------------------------------------------------
// ShimStackWalk::Clear
//
// Description:
//    Clear all the memory used by this ShimStackWalk, including the array of frames, the array of chains,
//    the linked list of ShimChainEnums, and the linked list of ShimFrameEnums.
//

void ShimStackWalk::Clear()
{
    // call Release() on each of the ShimChains
    for (int i = 0; i < m_stackChains.Count(); i++)
    {
        (*m_stackChains.Get(i))->Neuter();
        (*m_stackChains.Get(i))->Release();
    }
    m_stackChains.Clear();

    // call Release() on each of the ICDFrames
    for (int i = 0; i < m_stackFrames.Count(); i++)
    {
        (*m_stackFrames.Get(i))->Release();
    }
    m_stackFrames.Clear();

    // call Release() on each of the ShimChainEnums
    while (m_pChainEnumList != NULL)
    {
        ShimChainEnum * pCur =  m_pChainEnumList;
        m_pChainEnumList = m_pChainEnumList->GetNext();
        pCur->Neuter();
        pCur->Release();
    }

    // call Release() on each of the ShimFrameEnums
    while (m_pFrameEnumList != NULL)
    {
        ShimFrameEnum * pCur =  m_pFrameEnumList;
        m_pFrameEnumList = m_pFrameEnumList->GetNext();
        pCur->Neuter();
        pCur->Release();
    }

    // release the references
    m_pProcess.Clear();
    m_pThread.Clear();
}

//---------------------------------------------------------------------------------------
//
// Helper used by the stackwalker to determine whether a given UM chain should be tracked
// during the stackwalk for eventual transmission to the debugger. This function is the
// V4 equivalent of Whidbey's code:ShouldSendUMLeafChain (which ran on the LS, from
// Debug\EE\frameinfo.cpp).
//
// Note that code:ShouldSendUMLeafChain still exists today (to facilitate some in-process
// debugging stackwalks that are still necessary). So consult the comments in
// code:ShouldSendUMLeafChain for a more thorough discussion of why we do the checks we
// do to decide whether to track the chain.
//
// Arguments:
//    pswInfo - StackWalkInfo representing the frame in question
//
// Return Value:
//     nonzero iff the chain should be tracked
//

BOOL ShimStackWalk::ShouldTrackUMChain(StackWalkInfo * pswInfo)
{
    _ASSERTE (pswInfo != NULL);

    // Always track chains for non-leaf UM frames
    if (!pswInfo->IsLeafFrame())
        return TRUE;

    // Sometimes we want to track leaf UM chains, and sometimes we don't.  Check all the
    // reasons not to track the chain, and return FALSE if any of them are hit.

    CorDebugUserState threadUserState;
    HRESULT hr = m_pThread->GetUserState(&threadUserState);
    IfFailThrow(hr);

    // ShouldSendUMLeafChain checked IsInWaitSleepJoin which is just USER_WAIT_SLEEP_JOIN
    if ((threadUserState & USER_WAIT_SLEEP_JOIN) != 0)
        return FALSE;

    // This check is the same as Thread::IsUnstarted() from ShouldSendUMLeafChain
    if ((threadUserState & USER_UNSTARTED) != 0)
        return FALSE;

    // This check is the same as Thread::IsDead() from ShouldSendUMLeafChain
    if ((threadUserState & USER_STOPPED) != 0)
        return FALSE;

    // #DacShimSwWorkAround
    //
    // This part cannot be determined using DBI alone. We must call through to the DAC
    // because we have no other way to get at TS_Hijacked & TS_SyncSuspended.  When the
    // rearchitecture is complete, this DAC call should be able to go away, and we
    // should be able to use DBI for all the info we need.
    //
    // One might think one could avoid the DAC for TS_SyncSuspended by just checking
    // USER_SUSPENDED, but that won't work. Although USER_SUSPENDED will be returned some
    // of the time when TS_SyncSuspended is set, that will not be the case when the
    // debugger must suspend the thread. Example: if the given thread is in the middle of
    // throwing a managed exception when the debugger breaks in, then TS_SyncSuspended
    // will be set due to the debugger's breaking, but USER_SUSPENDED will not be set, so
    // we'll think the UM chain should be tracked, resulting in a stack that differs from
    // Whidbey.
    if (m_pProcess->IsThreadSuspendedOrHijacked(m_pThread))
        return FALSE;

    return TRUE;
}

// ----------------------------------------------------------------------------
// ShimStackWalk::Populate
//
// Description:
//    Walk the entire stack and populate the arrays of stack frames and stack chains.
//

void ShimStackWalk::Populate()
{
    HRESULT hr = S_OK;

    // query for the ICDThread3 interface
    RSExtSmartPtr<ICorDebugThread3> pThread3;
    hr = m_pThread->QueryInterface(IID_ICorDebugThread3, reinterpret_cast<void **>(&pThread3));
    IfFailThrow(hr);

    // create the ICDStackWalk
    RSExtSmartPtr<ICorDebugStackWalk> pSW;
    hr = pThread3->CreateStackWalk(&pSW);
    IfFailThrow(hr);

    // structs used to store information during the stackwalk
    ChainInfo     chainInfo;
    StackWalkInfo swInfo;

    // use the ICDStackWalk to retrieve the internal frames
    hr = pThread3->GetActiveInternalFrames(0, &(swInfo.m_cInternalFrames), NULL);
    IfFailThrow(hr);

    // allocate memory for the internal frames
    if (swInfo.m_cInternalFrames > 0)
    {
        // allocate memory for the array of RSExtSmartPtrs
        swInfo.m_ppInternalFrame2.AllocOrThrow(swInfo.m_cInternalFrames);

        // create a temporary buffer of raw ICDInternalFrame2 to pass to the ICD API
        NewArrayHolder<ICorDebugInternalFrame2 *> pTmpArray(new ICorDebugInternalFrame2* [swInfo.m_cInternalFrames]);
        hr = pThread3->GetActiveInternalFrames(swInfo.m_cInternalFrames,
                                               &(swInfo.m_cInternalFrames),
                                               pTmpArray);
        IfFailThrow(hr);

        // transfer the raw array to the RSExtSmartPtr array
        for (UINT32 i = 0; i < swInfo.m_cInternalFrames; i++)
        {
            // Assign() increments the ref count
            swInfo.m_ppInternalFrame2.Assign(i, pTmpArray[i]);
            pTmpArray[i]->Release();
        }
        pTmpArray.Clear();
    }

    //
    // This is basically how the loop works:
    //     1)  Determine whether we should process the next internal frame or the next stack frame.
    //     2)  If we are skipping frames, the only thing we need to do is to check whether we have reached
    //         the parent frame.
    //     3)  Process CHAIN_ENTER_MANAGED/CHAIN_ENTER_UNMANAGED chains
    //     4)  Append the frame to the cache.
    //     5)  Handle other types of chains.
    //     6)  Advance to the next frame.
    //     7)  Check if we should exit the loop.
    //
    while (true)
    {
        // reset variables used in the loop
        swInfo.ResetForNextFrame();

        // retrieve the next stack frame if it's available
        RSExtSmartPtr<ICorDebugFrame> pFrame;
        if (!swInfo.ExhaustedAllStackFrames())
        {
            hr = pSW->GetFrame(&pFrame);
            IfFailThrow(hr);
        }

        // This next clause processes the current frame, regardless of whether it's an internal frame or a
        // stack frame.  Normally, "pFrame != NULL" is a good enough check, except for the case where we
        // have exhausted all the stack frames but still have internal frames to process.
        if ((pFrame != NULL) || swInfo.ExhaustedAllStackFrames())
        {
            // prefetch the internal frame type
            if (!swInfo.ExhaustedAllInternalFrames())
            {
                swInfo.m_internalFrameType = GetInternalFrameType(swInfo.GetCurrentInternalFrame());
            }

            // We cannot have exhausted both the stack frames and the internal frames when we get to here.
            // We should have exited the loop if we have exhausted both types of frames.
            if (swInfo.ExhaustedAllStackFrames())
            {
                swInfo.m_fProcessingInternalFrame = true;
            }
            else if (swInfo.ExhaustedAllInternalFrames())
            {
                swInfo.m_fProcessingInternalFrame = false;
            }
            else
            {
                // check whether we should process the next internal frame or the next stack frame
                swInfo.m_fProcessingInternalFrame = (CheckInternalFrame(pFrame, &swInfo, pThread3, pSW) == TRUE);
            }

            // The only thing we do while we are skipping frames is to check whether we have reached the
            // parent frame, and we only need to check if we are processing a stack frame.
            if (swInfo.IsSkippingFrame())
            {
                if (!swInfo.m_fProcessingInternalFrame)
                {
                    // Check whether we have reached the parent frame yet.
                    RSExtSmartPtr<ICorDebugNativeFrame2> pNFrame2;
                    hr = pFrame->QueryInterface(IID_ICorDebugNativeFrame2, reinterpret_cast<void **>(&pNFrame2));
                    IfFailThrow(hr);

                    BOOL fIsParent = FALSE;
                    hr = swInfo.m_pChildFrame->IsMatchingParentFrame(pNFrame2, &fIsParent);
                    IfFailThrow(hr);

                    if (fIsParent)
                    {
                        swInfo.m_pChildFrame.Clear();
                    }
                }
            }
            else if(swInfo.m_fProcessingInternalFrame && !chainInfo.m_fLeafNativeContextIsValid &&
                swInfo.m_internalFrameType == STUBFRAME_M2U)
            {
                // Filter this frame out entirely
                // This occurs because InlinedCallFrames get placed inside leaf managed methods.
                // The frame gets erected before the native call is made and destroyed afterwards
                // but there is a window in which the debugger could stop where the internal frame
                // is live but we are executing jitted code. See Dev10 issue 743230
                // It is quite possible other frames have this same pattern if the debugger were
                // stopped right at the spot where they are being constructed. And that is
                // just a facet of the general data structure consistency problems the debugger
                // will always face
            }
            else
            {
                // Don't add any frame just yet.  We need to deal with any unmanaged chain
                // we are tracking first.

                // track the current enter-unmanaged chain and/or enter-managed chain
                TrackUMChain(&chainInfo, &swInfo);

                if (swInfo.m_fProcessingInternalFrame)
                {
                    // Check if this is a leaf internal frame.  If so, check its frame type.
                    // In V2, code:DebuggerWalkStackProc doesn't expose chains derived from leaf internal
                    // frames of type TYPE_INTERNAL.  However, V2 still exposes leaf M2U and U2M internal
                    // frames.
                    if (swInfo.IsLeafFrame())
                    {
                        if (swInfo.m_internalFrameType == STUBFRAME_EXCEPTION)
                        {
                            // We need to make sure we don't accidentally send an enter-unmanaged chain
                            // because of the leaf STUBFRAME_EXCEPTION.
                            chainInfo.CancelUMChain();
                            swInfo.m_fSkipChain = true;
                            swInfo.m_fHasException = true;
                        }
                    }

                    _ASSERTE(!swInfo.IsSkippingFrame());
                    if (ConvertInternalFrameToDynamicMethod(&swInfo))
                    {
                        // We have just converted a STUBFRAME_JIT_COMPILATION to a
                        // STUBFRAME_LIGHTWEIGHT_FUNCTION (or to NULL).  Since the latter frame type doesn't
                        // map to any  chain in V2, let's skip the chain handling.
                        swInfo.m_fSkipChain = true;

                        // We may have converted to NULL, which means that we are dealing with an IL stub
                        // and we shouldn't expose it.
                        if (swInfo.GetCurrentInternalFrame() != NULL)
                        {
                            AppendFrame(swInfo.GetCurrentInternalFrame(), &swInfo);
                        }
                    }
                    else
                    {
                        // One more check before we append the internal frame: make sure the frame type is a
                        // V2 frame type first.
                        if (!IsV3FrameType(swInfo.m_internalFrameType))
                        {
                            AppendFrame(swInfo.GetCurrentInternalFrame(), &swInfo);
                        }
                    }
                }
                else
                {
                    if (!chainInfo.m_fNeedEnterManagedChain)
                    {
                        // If we have hit any managed stack frame, then we may need to send
                        // an enter-managed chain later.  Save the CONTEXT now.
                        SaveChainContext(pSW, &chainInfo, &(chainInfo.m_leafManagedContext));
                        chainInfo.m_fNeedEnterManagedChain = true;
                    }

                    // We are processing a stack frame.
                    // Only append the frame if it's NOT a dynamic method.
                    _ASSERTE(!swInfo.IsSkippingFrame());
                    if (ConvertStackFrameToDynamicMethod(pFrame, &swInfo))
                    {
                        // We have converted a ICDNativeFrame for an IL method without metadata to an
                        // ICDInternalFrame of type STUBFRAME_LIGHTWEIGHT_FUNCTION (or to NULL).
                        // Fortunately, we don't have to update any state here
                        // (e.g. m_fProcessingInternalFrame) because the rest of the loop doesn't care.
                        if (swInfo.GetCurrentInternalFrame() != NULL)
                        {
                            AppendFrame(swInfo.GetCurrentInternalFrame(), &swInfo);
                        }
                    }
                    else
                    {
                        AppendFrame(pFrame, &swInfo);
                    }

                    // If we have just processed a child frame, we should start skipping.
                    // Get the ICDNativeFrame2 pointer to check.
                    RSExtSmartPtr<ICorDebugNativeFrame2> pNFrame2;
                    hr = pFrame->QueryInterface(IID_ICorDebugNativeFrame2, reinterpret_cast<void **>(&pNFrame2));
                    IfFailThrow(hr);

                    if (pNFrame2 != NULL)
                    {
                        BOOL fIsChild = FALSE;
                        hr = pNFrame2->IsChild(&fIsChild);
                        IfFailThrow(hr);

                        if (fIsChild)
                        {
                            swInfo.m_pChildFrame.Assign(pNFrame2);
                        }
                    }
                }
            }
        }  // process the current frame (managed stack frame or internal frame)

        // We can take care of other types of chains here, but only do so if we are not currently skipping
        // child frames.
        if (!swInfo.IsSkippingFrame())
        {
            if ((pFrame == NULL) &&
                !swInfo.ExhaustedAllStackFrames())
            {
                // We are here because we are processing a native marker stack frame, not because
                // we have exhausted all the stack frames.

                // We need to save the CONTEXT to start tracking an unmanaged chain.
                SaveChainContext(pSW, &chainInfo, &(chainInfo.m_leafNativeContext));
                chainInfo.m_fLeafNativeContextIsValid = true;

                // begin tracking UM chain if we're supposed to
                if (ShouldTrackUMChain(&swInfo))
                {
                    chainInfo.m_reason = CHAIN_ENTER_UNMANAGED;
                }
            }
            else
            {
                // handle other types of chains
                if (swInfo.m_fProcessingInternalFrame)
                {
                    if (!swInfo.m_fSkipChain)
                    {
                        BOOL fNewChain = FALSE;

                        switch (swInfo.m_internalFrameType)
                        {
                            case STUBFRAME_M2U:                     // fall through
                            case STUBFRAME_U2M:                     // fall through
                                // These frame types are tracked specially.
                                break;

                            case STUBFRAME_APPDOMAIN_TRANSITION:    // fall through
                            case STUBFRAME_LIGHTWEIGHT_FUNCTION:    // fall through
                            case STUBFRAME_INTERNALCALL:
                                // These frame types don't correspond to chains.
                                break;

                            case STUBFRAME_FUNC_EVAL:
                                chainInfo.m_reason = CHAIN_FUNC_EVAL;
                                fNewChain = TRUE;
                                break;

                            case STUBFRAME_CLASS_INIT:                  // fall through
                            case STUBFRAME_JIT_COMPILATION:
                                // In Whidbey, these two frame types are the same.
                                chainInfo.m_reason = CHAIN_CLASS_INIT;
                                fNewChain = TRUE;
                                break;

                            case STUBFRAME_EXCEPTION:
                                chainInfo.m_reason = CHAIN_EXCEPTION_FILTER;
                                fNewChain = TRUE;
                                break;

                            case STUBFRAME_SECURITY:
                                chainInfo.m_reason = CHAIN_SECURITY;
                                fNewChain = TRUE;
                                break;

                            default:
                                // We can only reach this case if we have converted an IL stub to NULL.
                                _ASSERTE(swInfo.HasConvertedFrame());
                                break;
                        }

                        if (fNewChain)
                        {
                            chainInfo.m_rootFP = GetFramePointerForChain(swInfo.GetCurrentInternalFrame());
                            AppendChain(&chainInfo, &swInfo);
                        }
                    }
                } // chain handling for an internl frame
            } // chain handling for a managed stack frame or an internal frame
        } // chain handling

        // Reset the flag for leaf frame if we have processed any frame.  The only case where we should
        // not reset this flag is if the ICDStackWalk is stopped at a native stack frame on creation.
        if (swInfo.IsLeafFrame())
        {
            if (swInfo.m_fProcessingInternalFrame || (pFrame != NULL))
            {
                swInfo.m_fLeafFrame = false;
            }
        }

        // advance to the next frame
        if (swInfo.m_fProcessingInternalFrame)
        {
            swInfo.m_curInternalFrame += 1;
        }
        else
        {
            hr = pSW->Next();
            IfFailThrow(hr);

            // check for the end of stack condition
            if (hr == CORDBG_S_AT_END_OF_STACK)
            {
                // By the time we finish the stackwalk, all child frames should have been matched with their
                // respective parent frames.
                _ASSERTE(!swInfo.IsSkippingFrame());

                swInfo.m_fExhaustedAllStackFrames = true;
            }
        }

        // Break out of the loop if we have exhausted all the frames.
        if (swInfo.ExhaustedAllFrames())
        {
            break;
        }
    }

    // top off the stackwalk with a thread start chain
    chainInfo.m_reason = CHAIN_THREAD_START;
    chainInfo.m_rootFP = ROOT_MOST_FRAME;       // In Whidbey, we actually use the cached stack base value.
    AppendChain(&chainInfo, &swInfo);
}

// the caller is responsible for addref and release
ICorDebugThread * ShimStackWalk::GetThread()
{
    return m_pThread;
}

// the caller is responsible for addref and release
ShimChain * ShimStackWalk::GetChain(UINT32 index)
{
    if (index >= (UINT32)(m_stackChains.Count()))
    {
        return NULL;
    }
    else
    {
        return *(m_stackChains.Get((int)index));
    }
}

// the caller is responsible for addref and release
ICorDebugFrame * ShimStackWalk::GetFrame(UINT32 index)
{
    if (index >= (UINT32)(m_stackFrames.Count()))
    {
        return NULL;
    }
    else
    {
        return *(m_stackFrames.Get((int)index));
    }
}

ULONG ShimStackWalk::GetChainCount()
{
    return m_stackChains.Count();
}

ULONG ShimStackWalk::GetFrameCount()
{
    return m_stackFrames.Count();
}

RSLock * ShimStackWalk::GetShimLock()
{
    return m_pProcess->GetShimLock();
}


// ----------------------------------------------------------------------------
// ShimStackWalk::AddChainEnum
//
// Description:
//    Add the specified ShimChainEnum to the head of the linked list of ShimChainEnums on the ShimStackWalk.
//
// Arguments:
//    * pChainEnum - the ShimChainEnum to be added
//

void ShimStackWalk::AddChainEnum(ShimChainEnum * pChainEnum)
{
    pChainEnum->SetNext(m_pChainEnumList);
    if (m_pChainEnumList != NULL)
    {
        m_pChainEnumList->Release();
    }

    m_pChainEnumList = pChainEnum;
    if (m_pChainEnumList != NULL)
    {
        m_pChainEnumList->AddRef();
    }
}

// ----------------------------------------------------------------------------
// ShimStackWalk::AddFrameEnum
//
// Description:
//    Add the specified ShimFrameEnum to the head of the linked list of ShimFrameEnums on the ShimStackWalk.
//
// Arguments:
//    * pFrameEnum - the ShimFrameEnum to be added
//

void ShimStackWalk::AddFrameEnum(ShimFrameEnum * pFrameEnum)
{
    pFrameEnum->SetNext(m_pFrameEnumList);
    if (m_pFrameEnumList != NULL)
    {
        m_pFrameEnumList->Release();
    }

    m_pFrameEnumList = pFrameEnum;
    if (m_pFrameEnumList != NULL)
    {
        m_pFrameEnumList->AddRef();
    }
}

// Return the ICDThread associated with the current ShimStackWalk as a key for ShimStackWalkHashTableTraits.
ICorDebugThread * ShimStackWalk::GetKey()
{
    return m_pThread;
}

// Hash a given ICDThread, which is used as the key for ShimStackWalkHashTableTraits.
//static
UINT32 ShimStackWalk::Hash(ICorDebugThread * pThread)
{
    // just return the pointer value
    return (UINT32)(size_t)pThread;
}

// ----------------------------------------------------------------------------
// ShimStackWalk::IsLeafFrame
//
// Description:
//    Check whether the specified frame is the leaf frame.
//
// Arguments:
//    * pFrame - frame to be checked
//
// Return Value:
//    Return TRUE if the specified frame is the leaf frame.
//    Return FALSE otherwise.
//
// Notes:
//    * The definition of the leaf frame in V2 is the frame at the leaf of the leaf chain.
//

BOOL ShimStackWalk::IsLeafFrame(ICorDebugFrame * pFrame)
{
    // check if we have any chain
    if (GetChainCount() > 0)
    {
        // check if the leaf chain has any frame
        if (GetChain(0)->GetLastFrameIndex() > 0)
        {
            return IsSameFrame(pFrame, GetFrame(0));
        }
    }
    return FALSE;
}

// ----------------------------------------------------------------------------
// ShimStackWalk::IsSameFrame
//
// Description:
//    Given two ICDFrames, check if they refer to the same frame.
//    This is much more than a pointer comparison.  This function actually checks the frame address,
//    the stack pointer, etc. to make sure if the frames are the same.
//
// Arguments:
//    * pLeft  - frame to be compared
//    * pRight - frame to be compared
//
// Return Value:
//    Return TRUE if the two ICDFrames represent the same frame.
//

BOOL ShimStackWalk::IsSameFrame(ICorDebugFrame * pLeft, ICorDebugFrame * pRight)
{
    HRESULT hr = E_FAIL;

    // Quick check #1: If the pointers are the same then the two frames are the same (duh!).
    if (pLeft == pRight)
    {
        return TRUE;
    }

    RSExtSmartPtr<ICorDebugNativeFrame> pLeftNativeFrame;
    hr = pLeft->QueryInterface(IID_ICorDebugNativeFrame, reinterpret_cast<void **>(&pLeftNativeFrame));

    if (SUCCEEDED(hr))
    {
        // The left frame is a stack frame.
        RSExtSmartPtr<ICorDebugNativeFrame> pRightNativeFrame;
        hr = pRight->QueryInterface(IID_ICorDebugNativeFrame, reinterpret_cast<void **>(&pRightNativeFrame));

        if (FAILED(hr))
        {
            // The right frame is NOT a stack frame.
            return FALSE;
        }
        else
        {
            // Quick check #2: If the IPs are different then the two frames are not the same (duh!).
            ULONG32 leftOffset;
            ULONG32 rightOffset;

            hr = pLeftNativeFrame->GetIP(&leftOffset);
            IfFailThrow(hr);

            hr = pRightNativeFrame->GetIP(&rightOffset);
            IfFailThrow(hr);

            if (leftOffset != rightOffset)
            {
                return FALSE;
            }

            // real check
            CORDB_ADDRESS leftStart;
            CORDB_ADDRESS leftEnd;
            CORDB_ADDRESS rightStart;
            CORDB_ADDRESS rightEnd;

            hr = pLeftNativeFrame->GetStackRange(&leftStart, &leftEnd);
            IfFailThrow(hr);

            hr = pRightNativeFrame->GetStackRange(&rightStart, &rightEnd);
            IfFailThrow(hr);

            return ((leftStart == rightStart) && (leftEnd == rightEnd));
        }
    }
    else
    {
        RSExtSmartPtr<ICorDebugInternalFrame2> pLeftInternalFrame2;
        hr = pLeft->QueryInterface(IID_ICorDebugInternalFrame2,
                                   reinterpret_cast<void **>(&pLeftInternalFrame2));

        if (SUCCEEDED(hr))
        {
            // The left frame is an internal frame.
            RSExtSmartPtr<ICorDebugInternalFrame2> pRightInternalFrame2;
            hr = pRight->QueryInterface(IID_ICorDebugInternalFrame2,
                                        reinterpret_cast<void **>(&pRightInternalFrame2));

            if (FAILED(hr))
            {
                return FALSE;
            }
            else
            {
                // The right frame is also an internal frame.

                // Check the frame address.
                CORDB_ADDRESS leftFrameAddr;
                CORDB_ADDRESS rightFrameAddr;

                hr = pLeftInternalFrame2->GetAddress(&leftFrameAddr);
                IfFailThrow(hr);

                hr = pRightInternalFrame2->GetAddress(&rightFrameAddr);
                IfFailThrow(hr);

                return (leftFrameAddr == rightFrameAddr);
            }
        }

        return FALSE;
    }
}

// This is the shim implementation of ICDThread::EnumerateChains().
void ShimStackWalk::EnumerateChains(ICorDebugChainEnum ** ppChainEnum)
{
    NewHolder<ShimChainEnum> pChainEnum(new ShimChainEnum(this, GetShimLock()));

    *ppChainEnum = pChainEnum;
    (*ppChainEnum)->AddRef();
    AddChainEnum(pChainEnum);

    pChainEnum.SuppressRelease();
}

// This is the shim implementation of ICDThread::GetActiveChain().
void ShimStackWalk::GetActiveChain(ICorDebugChain ** ppChain)
{
    if (GetChainCount() == 0)
    {
        *ppChain = NULL;
    }
    else
    {
        *ppChain = static_cast<ICorDebugChain *>(GetChain(0));
        (*ppChain)->AddRef();
    }
}

// This is the shim implementation of ICDThread::GetActiveFrame().
void ShimStackWalk::GetActiveFrame(ICorDebugFrame ** ppFrame)
{
    //
    // Make sure two things:
    //     1)  We have at least one frame.
    //     2)  The leaf frame is in the leaf chain, i.e. the leaf chain is not empty.
    //
    if ((GetFrameCount() == 0) ||
        (GetChain(0)->GetLastFrameIndex() == 0))
    {
        *ppFrame = NULL;
    }
    else
    {
        *ppFrame = GetFrame(0);
        (*ppFrame)->AddRef();
    }
}

// This is the shim implementation of ICDThread::GetRegisterSet().
void ShimStackWalk::GetActiveRegisterSet(ICorDebugRegisterSet ** ppRegisterSet)
{
    _ASSERTE(GetChainCount() != 0);
    _ASSERTE(GetChain(0) != NULL);

    // Return the register set of the leaf chain.
    HRESULT hr = GetChain(0)->GetRegisterSet(ppRegisterSet);
    IfFailThrow(hr);
}

// This is the shim implementation of ICDFrame::GetChain().
void ShimStackWalk::GetChainForFrame(ICorDebugFrame * pFrame, ICorDebugChain ** ppChain)
{
    CORDB_ADDRESS frameStart;
    CORDB_ADDRESS frameEnd;
    IfFailThrow(pFrame->GetStackRange(&frameStart, &frameEnd));

    for (UINT32 i = 0; i < GetChainCount(); i++)
    {
        ShimChain * pCurChain = GetChain(i);

        CORDB_ADDRESS chainStart;
        CORDB_ADDRESS chainEnd;
        IfFailThrow(pCurChain->GetStackRange(&chainStart, &chainEnd));

        if ((chainStart <= frameStart) && (frameEnd <= chainEnd))
        {
            // We need to check the next chain as well since some chains overlap at the boundary.
            // If the current chain is the last one, no additional checking is required.
            if (i < (GetChainCount() - 1))
            {
                ShimChain * pNextChain = GetChain(i + 1);

                CORDB_ADDRESS nextChainStart;
                CORDB_ADDRESS nextChainEnd;
                IfFailThrow(pNextChain->GetStackRange(&nextChainStart, &nextChainEnd));

                if ((nextChainStart <= frameStart) && (frameEnd <= nextChainEnd))
                {
                    // The frame lies in the stack ranges of two chains.  This can only happn at the boundary.
                    if (pCurChain->GetFirstFrameIndex() == pCurChain->GetLastFrameIndex())
                    {
                        // Make sure the next chain is not empty.
                        _ASSERTE(pNextChain->GetFirstFrameIndex() != pNextChain->GetLastFrameIndex());

                        // The current chain is empty, so the chain we want is the next one.
                        pCurChain = pNextChain;
                    }
                    // If the next chain is empty, then we'll just return the current chain and no additional
                    // work is needed.  If the next chain is not empty, then we have more checking to do.
                    else if (pNextChain->GetFirstFrameIndex() != pNextChain->GetLastFrameIndex())
                    {
                        // Both chains are non-empty.
                        if (IsSameFrame(GetFrame(pNextChain->GetFirstFrameIndex()), pFrame))
                        {
                            // The same frame cannot be in both chains.
                            _ASSERTE(!IsSameFrame(GetFrame(pCurChain->GetLastFrameIndex() - 1), pFrame));
                            pCurChain = pNextChain;
                        }
                        else
                        {
                            _ASSERTE(IsSameFrame(GetFrame(pCurChain->GetLastFrameIndex() - 1), pFrame));
                        }
                    }
                }
            }

            *ppChain = static_cast<ICorDebugChain *>(pCurChain);
            (*ppChain)->AddRef();
            return;
        }
    }
}

// This is the shim implementation of ICDFrame::GetCaller().
void ShimStackWalk::GetCallerForFrame(ICorDebugFrame * pFrame, ICorDebugFrame ** ppCallerFrame)
{
    for (UINT32 i = 0; i < GetChainCount(); i++)
    {
        ShimChain * pCurChain = GetChain(i);

        for (UINT32 j = pCurChain->GetFirstFrameIndex(); j < pCurChain->GetLastFrameIndex(); j++)
        {
            if (IsSameFrame(GetFrame(j), pFrame))
            {
                // Check whether this is the last frame in the chain.
                UINT32 callerFrameIndex = j + 1;
                if (callerFrameIndex < pCurChain->GetLastFrameIndex())
                {
                    *ppCallerFrame = static_cast<ICorDebugFrame *>(GetFrame(callerFrameIndex));
                    (*ppCallerFrame)->AddRef();
                }
                else
                {
                    *ppCallerFrame = NULL;
                }
                return;
            }
        }
    }
}

// This is the shim implementation of ICDFrame::GetCallee().
void ShimStackWalk::GetCalleeForFrame(ICorDebugFrame * pFrame, ICorDebugFrame ** ppCalleeFrame)
{
    for (UINT32 i = 0; i < GetChainCount(); i++)
    {
        ShimChain * pCurChain = GetChain(i);

        for (UINT32 j = pCurChain->GetFirstFrameIndex(); j < pCurChain->GetLastFrameIndex(); j++)
        {
            if (IsSameFrame(GetFrame(j), pFrame))
            {
                // Check whether this is the first frame in the chain.
                if (j > pCurChain->GetFirstFrameIndex())
                {
                    UINT32 calleeFrameIndex = j - 1;
                    *ppCalleeFrame = static_cast<ICorDebugFrame *>(GetFrame(calleeFrameIndex));
                    (*ppCalleeFrame)->AddRef();
                }
                else
                {
                    *ppCalleeFrame = NULL;
                }
                return;
            }
        }
    }
}

FramePointer ShimStackWalk::GetFramePointerForChain(DT_CONTEXT * pContext)
{
    return FramePointer::MakeFramePointer(CORDbgGetSP(pContext));
}

FramePointer ShimStackWalk::GetFramePointerForChain(ICorDebugInternalFrame2 * pInternalFrame2)
{
    CORDB_ADDRESS frameAddr;
    HRESULT hr = pInternalFrame2->GetAddress(&frameAddr);
    IfFailThrow(hr);

    return FramePointer::MakeFramePointer(reinterpret_cast<void *>(frameAddr));
}

CorDebugInternalFrameType ShimStackWalk::GetInternalFrameType(ICorDebugInternalFrame2 * pFrame2)
{
    HRESULT hr = E_FAIL;

    // Retrieve the frame type of the internal frame.
    RSExtSmartPtr<ICorDebugInternalFrame> pFrame;
    hr = pFrame2->QueryInterface(IID_ICorDebugInternalFrame, reinterpret_cast<void **>(&pFrame));
    IfFailThrow(hr);

    CorDebugInternalFrameType type;
    hr = pFrame->GetFrameType(&type);
    IfFailThrow(hr);

    return type;
}

// ----------------------------------------------------------------------------
// ShimStackWalk::AppendFrame
//
// Description:
//    Append the specified frame to the array and increment the counter.
//
// Arguments:
//    * pFrame         - the frame to be added
//    * pStackWalkInfo - contains information of the stackwalk
//

void ShimStackWalk::AppendFrame(ICorDebugFrame * pFrame, StackWalkInfo * pStackWalkInfo)
{
    if (pStackWalkInfo->m_fHasException && pStackWalkInfo->m_cFrame == 0)
    {
        RSExtSmartPtr<ICorDebugILFrame> pNFrame3;
        HRESULT hr = pFrame->QueryInterface(IID_ICorDebugILFrame, reinterpret_cast<void **>(&pNFrame3));
        if (pNFrame3 != NULL)
        {
            CordbJITILFrame* JITILFrameToAdjustIP = (static_cast<CordbJITILFrame*>(pNFrame3.GetValue()));
            JITILFrameToAdjustIP->AdjustIPAfterException();
            pStackWalkInfo->m_fHasException = false;                                    
        }
    }
    // grow the
    ICorDebugFrame ** ppFrame = m_stackFrames.AppendThrowing();

    // Be careful of the AddRef() below.  Once we do the addref, we need to save the pointer and
    // explicitly release it.
    *ppFrame = pFrame;
    (*ppFrame)->AddRef();

    pStackWalkInfo->m_cFrame += 1;
}

// ----------------------------------------------------------------------------
// Refer to comment of the overloaded function.
//

void ShimStackWalk::AppendFrame(ICorDebugInternalFrame2 * pInternalFrame2, StackWalkInfo * pStackWalkInfo)
{
    RSExtSmartPtr<ICorDebugFrame> pFrame;
    HRESULT hr = pInternalFrame2->QueryInterface(IID_ICorDebugFrame, reinterpret_cast<void **>(&pFrame));
    IfFailThrow(hr);

    AppendFrame(pFrame, pStackWalkInfo);
}

// ----------------------------------------------------------------------------
// ShimStackWalk::AppendChainWorker
//
// Description:
//    Append the specified chain to the array.
//
// Arguments:
//    * pStackWalkInfo  - contains information regarding the stackwalk
//    * pLeafContext    - the leaf CONTEXT of the chain to be added
//    * fpRoot          - the root boundary of the chain to be added
//    * chainReason     - the chain reason of the chain to be added
//    * fIsManagedChain - whether the chain to be added is managed
//

void ShimStackWalk::AppendChainWorker(StackWalkInfo *     pStackWalkInfo,
                                      DT_CONTEXT *        pLeafContext,
                                      FramePointer        fpRoot,
                                      CorDebugChainReason chainReason,
                                      BOOL                fIsManagedChain)
{
    // first, create the chain
    NewHolder<ShimChain> pChain(new ShimChain(this,
                                              pLeafContext,
                                              fpRoot,
                                              pStackWalkInfo->m_cChain,
                                              pStackWalkInfo->m_firstFrameInChain,
                                              pStackWalkInfo->m_cFrame,
                                              chainReason,
                                              fIsManagedChain,
                                              GetShimLock()));

    // Grow the array and add the newly created chain.
    // Once we call AddRef() we own the ShimChain and need to release it.
    ShimChain ** ppChain = m_stackChains.AppendThrowing();
    *ppChain = pChain;
    (*ppChain)->AddRef();

    // update the counters on the StackWalkInfo
    pStackWalkInfo->m_cChain += 1;
    pStackWalkInfo->m_firstFrameInChain = pStackWalkInfo->m_cFrame;

    // If all goes well, suppress the release so that the ShimChain won't go away.
    pChain.SuppressRelease();
}

// ----------------------------------------------------------------------------
// ShimStackWalk::AppendChain
//
// Description:
//    Append the chain to the array.  This function is also smart enough to send an enter-managed chain
//    if necessary.  In other words, this function may append two chains at the same time.
//
// Arguments:
//    * pChainInfo     - information on the chain to be added
//    * pStackWalkInfo - information regarding the current stackwalk
//

void ShimStackWalk::AppendChain(ChainInfo * pChainInfo, StackWalkInfo * pStackWalkInfo)
{
    // Check if the chain to be added is managed or not.
    BOOL fManagedChain = FALSE;
    if ((pChainInfo->m_reason == CHAIN_ENTER_MANAGED) ||
        (pChainInfo->m_reason == CHAIN_CLASS_INIT) ||
        (pChainInfo->m_reason == CHAIN_SECURITY) ||
        (pChainInfo->m_reason == CHAIN_FUNC_EVAL))
    {
        fManagedChain = TRUE;
    }

    DT_CONTEXT * pChainContext = NULL;
    if (fManagedChain)
    {
        // The chain to be added is managed itself.  So we don't need to send an enter-managed chain.
        pChainInfo->m_fNeedEnterManagedChain = false;
        pChainContext = &(pChainInfo->m_leafManagedContext);
    }
    else
    {
        // The chain to be added is unmanaged.  Check if we need to send an enter-managed chain.
        if (pChainInfo->m_fNeedEnterManagedChain)
        {
            // We need to send an extra enter-managed chain.
            _ASSERTE(pChainInfo->m_fLeafNativeContextIsValid);
            BYTE * sp = reinterpret_cast<BYTE *>(CORDbgGetSP(&(pChainInfo->m_leafNativeContext)));
#if !defined(TARGET_ARM) &&  !defined(TARGET_ARM64)
            // Dev11 324806: on ARM we use the caller's SP for a frame's ending delimiter so we cannot
            // subtract 4 bytes from the chain's ending delimiter else the frame might never be in range.
            // TODO: revisit overlapping ranges on ARM, it would be nice to make it consistent with the other architectures.
            sp -= sizeof(LPVOID);
#endif
            FramePointer fp = FramePointer::MakeFramePointer(sp);

            AppendChainWorker(pStackWalkInfo,
                              &(pChainInfo->m_leafManagedContext),
                              fp,
                              CHAIN_ENTER_MANAGED,
                              TRUE);

            pChainInfo->m_fNeedEnterManagedChain = false;
        }
        _ASSERTE(pChainInfo->m_fLeafNativeContextIsValid);
        pChainContext = &(pChainInfo->m_leafNativeContext);
    }

    // Add the actual chain.
    AppendChainWorker(pStackWalkInfo,
                      pChainContext,
                      pChainInfo->m_rootFP,
                      pChainInfo->m_reason,
                      fManagedChain);
}

// ----------------------------------------------------------------------------
// ShimStackWalk::SaveChainContext
//
// Description:
//    Save the current CONTEXT on the ICDStackWalk into the specified CONTEXT.  Also update the root end
//    of the chain on the ChainInfo.
//
// Arguments:
//    * pSW        - the ICDStackWalk for the current stackwalk
//    * pChainInfo - the ChainInfo keeping track of the current chain
//    * pContext   - the destination CONTEXT
//

void ShimStackWalk::SaveChainContext(ICorDebugStackWalk * pSW, ChainInfo * pChainInfo, DT_CONTEXT * pContext)
{
    HRESULT hr = pSW->GetContext(CONTEXT_FULL,
                                 sizeof(*pContext),
                                 NULL,
                                 reinterpret_cast<BYTE *>(pContext));
    IfFailThrow(hr);

    pChainInfo->m_rootFP = GetFramePointerForChain(pContext);
}

// ----------------------------------------------------------------------------
// ShimStackWalk::CheckInternalFrame
//
// Description:
//    Check whether the next frame to be processed should be the next internal frame or the next stack frame.
//
// Arguments:
//    * pNextStackFrame - the next stack frame
//    * pStackWalkInfo  - information regarding the current stackwalk; also contains the next internal frame
//    * pThread3        - the thread we are walking
//    * pSW             - the current stackwalk
//
// Return Value:
//    Return TRUE if we should process an internal frame next.
//

BOOL ShimStackWalk::CheckInternalFrame(ICorDebugFrame *     pNextStackFrame,
                                       StackWalkInfo *      pStackWalkInfo,
                                       ICorDebugThread3 *   pThread3,
                                       ICorDebugStackWalk * pSW)
{
    _ASSERTE(pNextStackFrame != NULL);
    _ASSERTE(!pStackWalkInfo->ExhaustedAllInternalFrames());

    HRESULT hr = E_FAIL;
    BOOL fIsInternalFrameFirst = FALSE;

    // Special handling for the case where a managed method contains a M2U internal frame.
    // Normally only IL stubs contain M2U internal frames, but we may have inlined pinvoke calls in
    // optimized code.  In that case, we would have an InlinedCallFrame in a normal managed method on x86.
    // On WIN64, we would have a normal NDirectMethodFrame* in a normal managed method.
    if (pStackWalkInfo->m_internalFrameType == STUBFRAME_M2U)
    {
        // create a temporary ICDStackWalk
        RSExtSmartPtr<ICorDebugStackWalk> pTmpSW;
        hr = pThread3->CreateStackWalk(&pTmpSW);
        IfFailThrow(hr);

        // retrieve the current CONTEXT
        DT_CONTEXT ctx;
        ctx.ContextFlags = DT_CONTEXT_FULL;
        hr = pSW->GetContext(ctx.ContextFlags, sizeof(ctx), NULL, reinterpret_cast<BYTE *>(&ctx));
        IfFailThrow(hr);

        // set the CONTEXT on the temporary ICDStackWalk
        hr = pTmpSW->SetContext(SET_CONTEXT_FLAG_ACTIVE_FRAME, sizeof(ctx), reinterpret_cast<BYTE *>(&ctx));
        IfFailThrow(hr);

        // unwind the temporary ICDStackWalk by one frame
        hr = pTmpSW->Next();
        IfFailThrow(hr);

        // Unwinding from a managed stack frame will land us either in a managed stack frame or a native
        // stack frame.  In either case, we have a CONTEXT.
        hr = pTmpSW->GetContext(ctx.ContextFlags, sizeof(ctx), NULL, reinterpret_cast<BYTE *>(&ctx));
        IfFailThrow(hr);

        // Get the SP from the CONTEXT.  This is the caller SP.
        CORDB_ADDRESS sp = PTR_TO_CORDB_ADDRESS(CORDbgGetSP(&ctx));

        // get the frame address
        CORDB_ADDRESS frameAddr = 0;
        hr = pStackWalkInfo->GetCurrentInternalFrame()->GetAddress(&frameAddr);
        IfFailThrow(hr);

        // Compare the frame address with the caller SP of the stack frame for the IL method without metadata.
        fIsInternalFrameFirst = (frameAddr < sp);
    }
    else
    {
        hr = pStackWalkInfo->GetCurrentInternalFrame()->IsCloserToLeaf(pNextStackFrame, &fIsInternalFrameFirst);
        IfFailThrow(hr);
    }

    return fIsInternalFrameFirst;
}

// ----------------------------------------------------------------------------
// ShimStackWalk::ConvertInternalFrameToDynamicMethod
//
// Description:
//    In V2, PrestubMethodFrames (PMFs) are exposed as one of two things: a chain of type
//    CHAIN_CLASS_INIT in most cases, or an internal frame of type STUBFRAME_LIGHTWEIGHT_FUNCTION if
//    the method being jitted is a dynamic method.  On the other hand, in Arrowhead, we consistently expose
//    PMFs as STUBFRAME_JIT_COMPILATION.  This function determines if a STUBFRAME_JIT_COMPILATION should
//    be exposed, and, if so, how to expose it.  In the case where conversion is necessary, this function
//    also updates the stackwalk information with the converted frame.
//
//    Here are the rules for conversion:
//    1)  If the method being jitted is an IL stub, we set the converted frame to NULL, and we return TRUE.
//    2)  If the method being jitted is an LCG method, we set the converted frame to a
//        STUBFRAME_LIGHTWEIGHT_FUNCTION, and we return NULL.
//    3)  Otherwise, we return FALSE.
//
// Arguments:
//    * pStackWalkInfo - information about the current stackwalk
//
// Return Value:
//    Return TRUE if a conversion has taken place.
//

BOOL ShimStackWalk::ConvertInternalFrameToDynamicMethod(StackWalkInfo * pStackWalkInfo)
{
    HRESULT hr = E_FAIL;

    // QI for ICDFrame
    RSExtSmartPtr<ICorDebugFrame> pOriginalFrame;
    hr = pStackWalkInfo->GetCurrentInternalFrame()->QueryInterface(
        IID_ICorDebugFrame,
        reinterpret_cast<void **>(&pOriginalFrame));
    IfFailThrow(hr);

    // Ask the RS to do the real work.
    CordbThread * pThread = static_cast<CordbThread *>(m_pThread.GetValue());
    pStackWalkInfo->m_fHasConvertedFrame = (TRUE == pThread->ConvertFrameForILMethodWithoutMetadata(
        pOriginalFrame,
        &(pStackWalkInfo->m_pConvertedInternalFrame2)));

    if (pStackWalkInfo->HasConvertedFrame())
    {
        // We have a conversion.
        if (pStackWalkInfo->GetCurrentInternalFrame() != NULL)
        {
            // We have a converted internal frame, so let's update the internal frame type.
            RSExtSmartPtr<ICorDebugInternalFrame> pInternalFrame;
            hr = pStackWalkInfo->GetCurrentInternalFrame()->QueryInterface(
                IID_ICorDebugInternalFrame,
                reinterpret_cast<void **>(&pInternalFrame));
            IfFailThrow(hr);

            hr = pInternalFrame->GetFrameType(&(pStackWalkInfo->m_internalFrameType));
            IfFailThrow(hr);
        }
        else
        {
            // The method being jitted is an IL stub, so let's not expose it.
            pStackWalkInfo->m_internalFrameType = STUBFRAME_NONE;
        }
    }

    return pStackWalkInfo->HasConvertedFrame();
}

// ----------------------------------------------------------------------------
// ShimStackWalk::ConvertInternalFrameToDynamicMethod
//
// Description:
//    In V2, LCG methods are exposed as internal frames of type STUBFRAME_LIGHTWEIGHT_FUNCTION.  However,
//    in Arrowhead, LCG methods are exposed as first-class stack frames, not internal frames.  Thus,
//    the shim needs to convert an ICDNativeFrame for a dynamic method in Arrowhead to an
//    ICDInternalFrame of type STUBFRAME_LIGHTWEIGHT_FUNCTION in V2.  Furthermore, IL stubs are not exposed
//    in V2 at all.
//
//    Here are the rules for conversion:
//    1)  If the stack frame is for an IL stub, we set the converted frame to NULL, and we return TRUE.
//    2)  If the stack frame is for an LCG method, we set the converted frame to a
//        STUBFRAME_LIGHTWEIGHT_FUNCTION, and we return NULL.
//    3)  Otherwise, we return FALSE.
//
// Arguments:
//    * pFrame         - the frame to be checked and converted if necessary
//    * pStackWalkInfo - information about the current stackwalk
//
// Return Value:
//    Return TRUE if a conversion has taken place.
//

BOOL ShimStackWalk::ConvertStackFrameToDynamicMethod(ICorDebugFrame * pFrame, StackWalkInfo * pStackWalkInfo)
{
    // If this is not a dynamic method (i.e. LCG method or IL stub), then we don't need to do a conversion.
    if (!IsILFrameWithoutMetadata(pFrame))
    {
        return FALSE;
    }

    // Ask the RS to do the real work.
    CordbThread * pThread = static_cast<CordbThread *>(m_pThread.GetValue());
    pStackWalkInfo->m_fHasConvertedFrame = (TRUE == pThread->ConvertFrameForILMethodWithoutMetadata(
        pFrame,
        &(pStackWalkInfo->m_pConvertedInternalFrame2)));

    return pStackWalkInfo->HasConvertedFrame();
}

// ----------------------------------------------------------------------------
// ShimStackWalk::TrackUMChain
//
// Description:
//    Keep track of enter-unmanaged chains.  Extend or cancel the chain as necesasry.
//
// Arguments:
//    * pChainInfo     - information on the current chain we are tracking
//    * pStackWalkInfo - information regarding the current stackwalk
//
// Notes:
//    * This logic is based on code:TrackUMChain on the LS.
//

void ShimStackWalk::TrackUMChain(ChainInfo * pChainInfo, StackWalkInfo * pStackWalkInfo)
{
    if (!pChainInfo->IsTrackingUMChain())
    {
        if (pStackWalkInfo->m_fProcessingInternalFrame)
        {
            if (pStackWalkInfo->m_internalFrameType == STUBFRAME_M2U)
            {
                // If we hit an M2U frame out in the wild, convert it to an enter-unmanaged chain.

                // We can't hit an M2U frame without hitting a native stack frame
                // first (we filter those).  We should have already saved the CONTEXT.
                // So just update the chain reason.
                pChainInfo->m_reason = CHAIN_ENTER_UNMANAGED;
            }
        }
    }

    BOOL fCreateUMChain = FALSE;
    if (pChainInfo->IsTrackingUMChain())
    {
        if (pStackWalkInfo->m_fProcessingInternalFrame)
        {
            // Extend the root end of the unmanaged chain.
            pChainInfo->m_rootFP = GetFramePointerForChain(pStackWalkInfo->GetCurrentInternalFrame());

            // Sometimes we may not want to show an UM chain b/c we know it's just
            // code inside of mscorwks. (Eg: Funcevals & AD transitions both fall into this category).
            // These are perfectly valid UM chains and we could give them if we wanted to.
            if ((pStackWalkInfo->m_internalFrameType == STUBFRAME_APPDOMAIN_TRANSITION) ||
                (pStackWalkInfo->m_internalFrameType == STUBFRAME_FUNC_EVAL))
            {
                pChainInfo->CancelUMChain();
            }
            else if (pStackWalkInfo->m_internalFrameType == STUBFRAME_M2U)
            {
                // If we hit an M2U frame, then go ahead and dispatch the UM chain now.
                // This will likely also be an exit frame.
                fCreateUMChain = TRUE;
            }
            else if ((pStackWalkInfo->m_internalFrameType == STUBFRAME_CLASS_INIT) ||
                     (pStackWalkInfo->m_internalFrameType == STUBFRAME_EXCEPTION) ||
                     (pStackWalkInfo->m_internalFrameType == STUBFRAME_SECURITY) ||
                     (pStackWalkInfo->m_internalFrameType == STUBFRAME_JIT_COMPILATION))
            {
                fCreateUMChain = TRUE;
            }
        }
        else
        {
            // If we hit a managed stack frame when we are processing an unmanaged chain, then
            // the chain is done.
            fCreateUMChain = TRUE;
        }
    }

    if (fCreateUMChain)
    {
        // check whether we get any stack range
        _ASSERTE(pChainInfo->m_fLeafNativeContextIsValid);
        FramePointer fpLeaf = GetFramePointerForChain(&(pChainInfo->m_leafNativeContext));

        // Don't bother creating an unmanaged chain if the stack range is empty.
        if (fpLeaf != pChainInfo->m_rootFP)
        {
            AppendChain(pChainInfo, pStackWalkInfo);
        }
        pChainInfo->CancelUMChain();
    }
}

BOOL ShimStackWalk::IsV3FrameType(CorDebugInternalFrameType type)
{
    // These frame types are either new in Arrowhead or not used in V2.
    if ((type == STUBFRAME_INTERNALCALL) ||
        (type == STUBFRAME_CLASS_INIT) ||
        (type == STUBFRAME_EXCEPTION) ||
        (type == STUBFRAME_SECURITY) ||
        (type == STUBFRAME_JIT_COMPILATION))
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

// Check whether a stack frame is for a dynamic method.  The way to tell is if the stack frame has
// an ICDNativeFrame but no ICDILFrame.
BOOL ShimStackWalk::IsILFrameWithoutMetadata(ICorDebugFrame * pFrame)
{
    HRESULT hr = E_FAIL;

    RSExtSmartPtr<ICorDebugNativeFrame> pNativeFrame;
    hr = pFrame->QueryInterface(IID_ICorDebugNativeFrame, reinterpret_cast<void **>(&pNativeFrame));
    IfFailThrow(hr);

    if (pNativeFrame != NULL)
    {
        RSExtSmartPtr<ICorDebugILFrame> pILFrame;
        hr = pFrame->QueryInterface(IID_ICorDebugILFrame, reinterpret_cast<void **>(&pILFrame));

        if (FAILED(hr) || (pILFrame == NULL))
        {
            return TRUE;
        }
    }

    return FALSE;
}

ShimStackWalk::StackWalkInfo::StackWalkInfo()
  : m_cChain(0),
    m_cFrame(0),
    m_firstFrameInChain(0),
    m_cInternalFrames(0),
    m_curInternalFrame(0),
    m_internalFrameType(STUBFRAME_NONE),
    m_fExhaustedAllStackFrames(false),
    m_fProcessingInternalFrame(false),
    m_fSkipChain(false),
    m_fLeafFrame(true),
    m_fHasConvertedFrame(false),
    m_fHasException(false)
{
    m_pChildFrame.Assign(NULL);
    m_pConvertedInternalFrame2.Assign(NULL);
}

ShimStackWalk::StackWalkInfo::~StackWalkInfo()
{
    if (m_pChildFrame != NULL)
    {
        m_pChildFrame.Clear();
    }

    if (m_pConvertedInternalFrame2 != NULL)
    {
        m_pConvertedInternalFrame2.Clear();
    }

    if (!m_ppInternalFrame2.IsEmpty())
    {
        m_ppInternalFrame2.Clear();
    }
}

void ShimStackWalk::StackWalkInfo::ResetForNextFrame()
{
    m_pConvertedInternalFrame2.Clear();
    m_internalFrameType = STUBFRAME_NONE;
    m_fProcessingInternalFrame = false;
    m_fSkipChain = false;
    m_fHasConvertedFrame = false;
}

// Check whether we have exhausted both internal frames and stack frames.
bool ShimStackWalk::StackWalkInfo::ExhaustedAllFrames()
{
    return (ExhaustedAllStackFrames() && ExhaustedAllInternalFrames());
}

bool ShimStackWalk::StackWalkInfo::ExhaustedAllStackFrames()
{
    return m_fExhaustedAllStackFrames;
}

bool ShimStackWalk::StackWalkInfo::ExhaustedAllInternalFrames()
{
    return (m_curInternalFrame == m_cInternalFrames);
}

ICorDebugInternalFrame2 * ShimStackWalk::StackWalkInfo::GetCurrentInternalFrame()
{
    _ASSERTE(!ExhaustedAllInternalFrames() || HasConvertedFrame());

    if (HasConvertedFrame())
    {
        return m_pConvertedInternalFrame2;
    }
    else
    {
        return m_ppInternalFrame2[m_curInternalFrame];
    }
}

BOOL ShimStackWalk::StackWalkInfo::IsLeafFrame()
{
    return m_fLeafFrame;
}

BOOL ShimStackWalk::StackWalkInfo::IsSkippingFrame()
{
    return (m_pChildFrame != NULL);
}

BOOL ShimStackWalk::StackWalkInfo::HasConvertedFrame()
{
    return m_fHasConvertedFrame;
}


ShimChain::ShimChain(ShimStackWalk *     pSW,
                     DT_CONTEXT *        pContext,
                     FramePointer        fpRoot,
                     UINT32              chainIndex,
                     UINT32              frameStartIndex,
                     UINT32              frameEndIndex,
                     CorDebugChainReason chainReason,
                     BOOL                fIsManaged,
                     RSLock *            pShimLock)
  : m_context(*pContext),
    m_fpRoot(fpRoot),
    m_pStackWalk(pSW),
    m_refCount(0),
    m_chainIndex(chainIndex),
    m_frameStartIndex(frameStartIndex),
    m_frameEndIndex(frameEndIndex),
    m_chainReason(chainReason),
    m_fIsManaged(fIsManaged),
    m_fIsNeutered(FALSE),
    m_pShimLock(pShimLock)
{
}

ShimChain::~ShimChain()
{
    _ASSERTE(IsNeutered());
}

void ShimChain::Neuter()
{
    m_fIsNeutered = TRUE;
}

BOOL ShimChain::IsNeutered()
{
    return m_fIsNeutered;
}

ULONG STDMETHODCALLTYPE ShimChain::AddRef()
{
    return InterlockedIncrement((LONG *)&m_refCount);
}

ULONG STDMETHODCALLTYPE ShimChain::Release()
{
    LONG newRefCount = InterlockedDecrement((LONG *)&m_refCount);
    _ASSERTE(newRefCount >= 0);

    if (newRefCount == 0)
    {
        delete this;
    }
    return newRefCount;
}

HRESULT ShimChain::QueryInterface(REFIID id, void ** pInterface)
{
    if (id == IID_ICorDebugChain)
    {
        *pInterface = static_cast<ICorDebugChain *>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugChain *>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

// Returns the thread to which this chain belongs.
HRESULT ShimChain::GetThread(ICorDebugThread ** ppThread)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppThread, ICorDebugThread **);

    *ppThread = m_pStackWalk->GetThread();
    (*ppThread)->AddRef();

    return S_OK;
}

// Get the range on the stack that this chain matches against.
// pStart is the leafmost; pEnd is the rootmost.
// This is particularly used in interop-debugging to get native stack traces
// for the UM portions of the stack
HRESULT ShimChain::GetStackRange(CORDB_ADDRESS * pStart, CORDB_ADDRESS * pEnd)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        THROW_IF_NEUTERED(this);

        VALIDATE_POINTER_TO_OBJECT_OR_NULL(pStart, CORDB_ADDRESS *);
        VALIDATE_POINTER_TO_OBJECT_OR_NULL(pEnd, CORDB_ADDRESS *);

        // Return the leafmost end of the stack range.
        // The leafmost end is represented by the register set.
        if (pStart)
        {
            *pStart = PTR_TO_CORDB_ADDRESS(CORDbgGetSP(&m_context));
        }

        // Return the rootmost end of the stack range.  It is represented by the frame pointer of the chain.
        if (pEnd)
        {
            *pEnd = PTR_TO_CORDB_ADDRESS(m_fpRoot.GetSPValue());
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT ShimChain::GetContext(ICorDebugContext ** ppContext)
{
    return E_NOTIMPL;
}

// Return the next chain which is closer to the root.
// Currently this is just a wrapper over GetNext().
HRESULT ShimChain::GetCaller(ICorDebugChain ** ppChain)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppChain, ICorDebugChain **);

    return GetNext(ppChain);
}

// Return the previous chain which is closer to the leaf.
// Currently this is just a wrapper over GetPrevious().
HRESULT ShimChain::GetCallee(ICorDebugChain ** ppChain)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppChain, ICorDebugChain **);

    return GetPrevious(ppChain);
}

// Return the previous chain which is closer to the leaf.
HRESULT ShimChain::GetPrevious(ICorDebugChain ** ppChain)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppChain, ICorDebugChain **);

    *ppChain = NULL;
    if (m_chainIndex != 0)
    {
        *ppChain = m_pStackWalk->GetChain(m_chainIndex - 1);
    }

    if (*ppChain != NULL)
    {
        (*ppChain)->AddRef();
    }

    return S_OK;
}

// Return the next chain which is closer to the root.
HRESULT ShimChain::GetNext(ICorDebugChain ** ppChain)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppChain, ICorDebugChain **);

    *ppChain = m_pStackWalk->GetChain(m_chainIndex + 1);
    if (*ppChain != NULL)
    {
        (*ppChain)->AddRef();
    }

    return S_OK;
}

// Return whether the chain contains frames running managed code.
HRESULT ShimChain::IsManaged(BOOL * pManaged)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pManaged, BOOL *);

    *pManaged = m_fIsManaged;

    return S_OK;
}

// Return an enumerator to iterate through the frames contained in this chain.
HRESULT ShimChain::EnumerateFrames(ICorDebugFrameEnum ** ppFrames)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFrames, ICorDebugFrameEnum **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        ShimStackWalk * pSW = GetShimStackWalk();
        NewHolder<ShimFrameEnum> pFrameEnum(new ShimFrameEnum(pSW, this, m_frameStartIndex, m_frameEndIndex, m_pShimLock));

        *ppFrames = pFrameEnum;
        (*ppFrames)->AddRef();

        // link the new ShimFramEnum into the list on the ShimStackWalk
        pSW->AddFrameEnum(pFrameEnum);

        pFrameEnum.SuppressRelease();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

// Return an enumerator to iterate through the frames contained in this chain.
// Note that this function will only succeed if the cached stack trace is valid.
HRESULT ShimChain::GetActiveFrame(ICorDebugFrame ** ppFrame)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFrame, ICorDebugFrame **);
    (*ppFrame) = NULL;

    HRESULT hr = S_OK;

    // Chains may be empty, so they have no active frame.
    if (m_frameStartIndex == m_frameEndIndex)
    {
        *ppFrame = NULL;
    }
    else
    {
        *ppFrame = m_pStackWalk->GetFrame(m_frameStartIndex);
        (*ppFrame)->AddRef();
    }

    return hr;
}

// Return the register set of the leaf end of the chain
HRESULT ShimChain::GetRegisterSet(ICorDebugRegisterSet ** ppRegisters)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppRegisters, ICorDebugRegisterSet **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        CordbThread * pThread = static_cast<CordbThread *>(m_pStackWalk->GetThread());

        // This is a private hook for calling back into the RS.  Alternatively, we could have created a
        // ShimRegisterSet, but that's too much work for now.
        pThread->CreateCordbRegisterSet(&m_context,
                                        (m_chainIndex == 0),
                                        m_chainReason,
                                        ppRegisters);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// Return the chain reason
HRESULT ShimChain::GetReason(CorDebugChainReason * pReason)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pReason, CorDebugChainReason *);

    *pReason = m_chainReason;

    return S_OK;
}

ShimStackWalk * ShimChain::GetShimStackWalk()
{
    return m_pStackWalk;
}

UINT32 ShimChain::GetFirstFrameIndex()
{
    return this->m_frameStartIndex;
}

UINT32 ShimChain::GetLastFrameIndex()
{
    return this->m_frameEndIndex;
}


ShimChainEnum::ShimChainEnum(ShimStackWalk * pSW, RSLock * pShimLock)
  : m_pStackWalk(pSW),
    m_pNext(NULL),
    m_currentChainIndex(0),
    m_refCount(0),
    m_fIsNeutered(FALSE),
    m_pShimLock(pShimLock)
{
}

ShimChainEnum::~ShimChainEnum()
{
    _ASSERTE(IsNeutered());
}

void ShimChainEnum::Neuter()
{
    if (IsNeutered())
    {
        return;
    }

    m_fIsNeutered = TRUE;
}

BOOL ShimChainEnum::IsNeutered()
{
    return m_fIsNeutered;
}


ULONG STDMETHODCALLTYPE ShimChainEnum::AddRef()
{
    return InterlockedIncrement((LONG *)&m_refCount);
}

ULONG STDMETHODCALLTYPE ShimChainEnum::Release()
{
    LONG newRefCount = InterlockedDecrement((LONG *)&m_refCount);
    _ASSERTE(newRefCount >= 0);

    if (newRefCount == 0)
    {
        delete this;
    }
    return newRefCount;
}

HRESULT ShimChainEnum::QueryInterface(REFIID id, void ** ppInterface)
{
    if (id == IID_ICorDebugChainEnum)
    {
        *ppInterface = static_cast<ICorDebugChainEnum *>(this);
    }
    else if (id == IID_ICorDebugEnum)
    {
        *ppInterface = static_cast<ICorDebugEnum *>(static_cast<ICorDebugChainEnum *>(this));
    }
    else if (id == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown *>(static_cast<ICorDebugChainEnum *>(this));
    }
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

// Skip the specified number of chains.
HRESULT ShimChainEnum::Skip(ULONG celt)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);

    // increment the index by the specified amount
    m_currentChainIndex += celt;
    return S_OK;
}

HRESULT ShimChainEnum::Reset()
{
    m_currentChainIndex = 0;
    return S_OK;
}

// Clone the chain enumerator and set the new one to the same current chain
HRESULT ShimChainEnum::Clone(ICorDebugEnum ** ppEnum)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugEnum **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        NewHolder<ShimChainEnum> pChainEnum(new ShimChainEnum(m_pStackWalk, m_pShimLock));

        // set the index in the new enumerator
        pChainEnum->m_currentChainIndex = this->m_currentChainIndex;

        *ppEnum = pChainEnum;
        (*ppEnum)->AddRef();
        m_pStackWalk->AddChainEnum(pChainEnum);

        pChainEnum.SuppressRelease();
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// Return the number of chains on the thread
HRESULT ShimChainEnum::GetCount(ULONG * pcChains)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pcChains, ULONG *);

    *pcChains = m_pStackWalk->GetChainCount();
    return S_OK;
}

// Retrieve the next x number of chains on the thread into "chains", where x is specified by "celt".
// "pcChainsFetched" is set to be the actual number of chains retrieved.
// Return S_FALSE if the number of chains actually retrieved is less than the number of chains requested.
HRESULT ShimChainEnum::Next(ULONG cChains, ICorDebugChain * rgpChains[], ULONG * pcChainsFetched)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(rgpChains, ICorDebugChain *, cChains, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcChainsFetched, ULONG *);

    // if the out parameter is NULL, then we can only return one chain at a time
    if ((pcChainsFetched == NULL) && (cChains != 1))
    {
        return E_INVALIDARG;
    }

    // Check for the trivial case where no chain is actually requested.
    // This is probably a user error.
    if (cChains == 0)
    {
        if (pcChainsFetched != NULL)
        {
            *pcChainsFetched = 0;
        }
        return S_OK;
    }

    ICorDebugChain ** ppCurrentChain = rgpChains;

    while ((m_currentChainIndex < m_pStackWalk->GetChainCount()) &&
           (cChains > 0))
    {
        *ppCurrentChain = m_pStackWalk->GetChain(m_currentChainIndex);
        (*ppCurrentChain)->AddRef();

        ppCurrentChain++;       // increment the pointer into the buffer
        m_currentChainIndex++;  // increment the index
        cChains--;
    }

    // set the number of chains actually returned
    if (pcChainsFetched != NULL)
    {
        *pcChainsFetched = (ULONG)(ppCurrentChain - rgpChains);
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (cChains > 0)
    {
        return S_FALSE;
    }

    return S_OK;
}

ShimChainEnum * ShimChainEnum::GetNext()
{
    return m_pNext;
}

void ShimChainEnum::SetNext(ShimChainEnum * pNext)
{
    if (m_pNext != NULL)
    {
        m_pNext->Release();
    }

    m_pNext = pNext;

    if (m_pNext != NULL)
    {
        m_pNext->AddRef();
    }
}


ShimFrameEnum::ShimFrameEnum(ShimStackWalk * pSW,
                             ShimChain *     pChain,
                             UINT32          frameStartIndex,
                             UINT32          frameEndIndex,
                             RSLock *        pShimLock)
  : m_pStackWalk(pSW),
    m_pChain(pChain),
    m_pShimLock(pShimLock),
    m_pNext(NULL),
    m_currentFrameIndex(frameStartIndex),
    m_endFrameIndex(frameEndIndex),
    m_refCount(0),
    m_fIsNeutered(FALSE)
{
}

ShimFrameEnum::~ShimFrameEnum()
{
    _ASSERTE(IsNeutered());
}

void ShimFrameEnum::Neuter()
{
    if (IsNeutered())
    {
        return;
    }

    m_fIsNeutered = TRUE;
}

BOOL ShimFrameEnum::IsNeutered()
{
    return m_fIsNeutered;
}


ULONG STDMETHODCALLTYPE ShimFrameEnum::AddRef()
{
    return InterlockedIncrement((LONG *)&m_refCount);
}

ULONG STDMETHODCALLTYPE ShimFrameEnum::Release()
{
    LONG newRefCount = InterlockedDecrement((LONG *)&m_refCount);
    _ASSERTE(newRefCount >= 0);

    if (newRefCount == 0)
    {
        delete this;
    }
    return newRefCount;
}

HRESULT ShimFrameEnum::QueryInterface(REFIID id, void ** ppInterface)
{
    if (id == IID_ICorDebugFrameEnum)
    {
        *ppInterface = static_cast<ICorDebugFrameEnum *>(this);
    }
    else if (id == IID_ICorDebugEnum)
    {
        *ppInterface = static_cast<ICorDebugEnum *>(static_cast<ICorDebugFrameEnum *>(this));
    }
    else if (id == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown *>(static_cast<ICorDebugFrameEnum *>(this));
    }
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

// Skip the specified number of chains.
HRESULT ShimFrameEnum::Skip(ULONG celt)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);

    // increment the index by the specified amount
    m_currentFrameIndex += celt;
    return S_OK;
}

HRESULT ShimFrameEnum::Reset()
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);

    m_currentFrameIndex = m_pChain->GetFirstFrameIndex();
    return S_OK;
}

// Clone the chain enumerator and set the new one to the same current chain
HRESULT ShimFrameEnum::Clone(ICorDebugEnum ** ppEnum)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugEnum **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        NewHolder<ShimFrameEnum> pFrameEnum(new ShimFrameEnum(m_pStackWalk,
                                                              m_pChain,
                                                              m_currentFrameIndex,
                                                              m_endFrameIndex,
                                                              m_pShimLock));

        *ppEnum = pFrameEnum;
        (*ppEnum)->AddRef();
        m_pStackWalk->AddFrameEnum(pFrameEnum);

        pFrameEnum.SuppressRelease();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

// Return the number of chains on the thread
HRESULT ShimFrameEnum::GetCount(ULONG * pcFrames)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pcFrames, ULONG *);

    *pcFrames = m_pChain->GetLastFrameIndex() - m_pChain->GetFirstFrameIndex();
    return S_OK;
}

// Retrieve the next x number of chains on the thread into "chains", where x is specified by "celt".
// "pcChainsFetched" is set to be the actual number of chains retrieved.
// Return S_FALSE if the number of chains actually retrieved is less than the number of chains requested.
HRESULT ShimFrameEnum::Next(ULONG cFrames, ICorDebugFrame * rgpFrames[], ULONG * pcFramesFetched)
{
    RSLockHolder lockHolder(m_pShimLock);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(rgpFrames, ICorDebugFrame *, cFrames, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcFramesFetched, ULONG *);

    // if the out parameter is NULL, then we can only return one chain at a time
    if ((pcFramesFetched == NULL) && (cFrames != 1))
    {
        return E_INVALIDARG;
    }

    // Check for the trivial case where no chain is actually requested.
    // This is probably a user error.
    if (cFrames == 0)
    {
        if (pcFramesFetched != NULL)
        {
            *pcFramesFetched = 0;
        }
        return S_OK;
    }

    ICorDebugFrame ** ppCurrentFrame = rgpFrames;

    while ((m_currentFrameIndex < m_endFrameIndex) &&
           (cFrames > 0))
    {
        *ppCurrentFrame = m_pStackWalk->GetFrame(m_currentFrameIndex);
        (*ppCurrentFrame)->AddRef();

        ppCurrentFrame++;       // increment the pointer into the buffer
        m_currentFrameIndex++;  // increment the index
        cFrames--;
    }

    // set the number of chains actually returned
    if (pcFramesFetched != NULL)
    {
        *pcFramesFetched = (ULONG)(ppCurrentFrame - rgpFrames);
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (cFrames > 0)
    {
        return S_FALSE;
    }

    return S_OK;
}

ShimFrameEnum * ShimFrameEnum::GetNext()
{
    return m_pNext;
}

void ShimFrameEnum::SetNext(ShimFrameEnum * pNext)
{
    if (m_pNext != NULL)
    {
        m_pNext->Release();
    }

    m_pNext = pNext;

    if (m_pNext != NULL)
    {
        m_pNext->AddRef();
    }
}
