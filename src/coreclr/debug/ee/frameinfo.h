// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: frameinfo.h
//

//
// Debugger stack walker
//
//*****************************************************************************

#ifndef FRAMEINFO_H_
#define FRAMEINFO_H_

/* ========================================================================= */

/* ------------------------------------------------------------------------- *
 * Classes
 * ------------------------------------------------------------------------- */

class DebuggerJitInfo;

// struct FrameInfo:  Contains the information that will be handed to
// DebuggerStackCallback functions (along with their own, individual
// pData pointers).
//
// Frame *frame:  The current explicit frame.  NULL implies that
//      the method frame is frameless, meaning either unmanaged or managed.  This
//      is set to be FRAME_TOP (0xFFffFFff) if the frame is the topmost, EE
//      placed frame.
//
// MethodDesc *md:  MetdhodDesc for the method that's
//      executing in this method frame.  Will be NULL if there is no MethodDesc
//      If we're in generic code this may be a representative (i.e. canonical)
//      MD, and extra information is available in the exactGenericArgsToken.
//      For explicit frames, this may point to the method the explicit frame refers to
//      (i.e. the method being jitted, or the interface method being called through
//      COM interop), however it must always point to a method within the same
//      domain of the explicit frame.  Therefore, it is not used to point to the target of
//      FuncEval frames since the target may be in a different domain.
//
// void *fp:  frame pointer.  Actually filled in from
//      caller (parent) frame, so the DebuggerStackWalkProc must delay
//      the user callback for one frame.  This is not technically necessary on WIN64, but
//      we follow the x86 model to keep things simpler.  We should really consider changing
//      the real stackwalker on x86 to unwind one frame ahead of time like the 64-bit one.
struct FrameInfo
{
public:
    Frame               *frame;
    MethodDesc          *md;

    // the register set of the frame being reported
    REGDISPLAY           registers;
    FramePointer         fp;

    // This field is propagated to the right side to become CordbRegisterSet::m_quicklyUnwind.
    // If it is true, then the registers reported in the REGDISPLAY are invalid.  It is only set to
    // true in InitForEnterManagedChain().  In that case, we are passing a NULL REGDISPLAY anyway.
    // This is such a misnomer.
    bool                 quickUnwind;

    // Set to true if we are dealing with an internal explicit frame.  Currently this is only true
    // for prestub frames, security frames, funceval frames, and certain debugger-specific frames
    // (e.g. DebuggerClassInitMarkFrame, DebuggerSecurityCodeMarkFrame).
    // This affects HasMethodFrame() below.
    bool                 internal;

    // whether the state contained in the FrameInfo represents a managed or unmanaged method frame/stub/chain;
    // corresponds to ICorDebugChain::IsManaged()
    bool                 managed;

    // Native offset from beginning of the method.
    ULONG                relOffset;

    // The ambient stackpointer. This can be use to compute esp-relative local variables,
    // which can be common in frameless methods.
    TADDR                ambientSP;

    // These two fields are only set for managed method frames.
    IJitManager         *pIJM;
    METHODTOKEN          MethodToken;

    // This represents the current domain of the frame itself, and which
    // the method specified by 'md' is executing in.
    AppDomain           *currentAppDomain;

    // only set for stackwalking, not stepping
    void                *exactGenericArgsToken;

#if defined(FEATURE_EH_FUNCLETS)
    // This field is only used on IA64 to determine which registers are available and
    // whether we need to adjust the IP.
    bool                 fIsLeaf;

    // These two fields are used for funclets.
    bool                 fIsFunclet;
    bool                 fIsFilter;

    bool IsFuncletFrame()          { return fIsFunclet; }
    bool IsFilterFrame()           { return fIsFilter;  }
    bool IsNonFilterFuncletFrame() { return (fIsFunclet && !fIsFilter); }
#endif // FEATURE_EH_FUNCLETS


    // A ridiculous flag that is targeting a very narrow fix at issue 650903 (4.5.1/Blue).
    // This is set when the currently walked frame is a ComPlusMethodFrameGeneric. If the
    // dude doing the walking is trying to ignore such frames (see
    // code:ControllerStackInfo::m_suppressUMChainFromComPlusMethodFrameGeneric), AND
    // this is set, then the walker just continues on to the next frame, without
    // erroneously identifying this frame as the target frame. Only used during "Step
    // Out" to a managed frame (i.e., managed-only debugging).
    bool                fIgnoreThisFrameIfSuppressingUMChainFromComPlusMethodFrameGeneric;

    // In addition to a Method, a FrameInfo may also represent either a Chain or a Stub (but not both).
    // chainReason corresponds to ICorDebugChain::GetReason().
    CorDebugChainReason  chainReason;
    CorDebugInternalFrameType eStubFrameType;

    // Helpers for initializing a FrameInfo for a chain or a stub frame.
    void InitForM2UInternalFrame(CrawlFrame * pCF);
    void InitForU2MInternalFrame(CrawlFrame * pCF);
    void InitForADTransition(CrawlFrame * pCF);
    void InitForDynamicMethod(CrawlFrame * pCF);
    void InitForFuncEval(CrawlFrame * pCF);
    void InitForThreadStart(Thread *thread, REGDISPLAY * pRDSrc);
    void InitForUMChain(FramePointer fpRoot, REGDISPLAY * pRDSrc);
    void InitForEnterManagedChain(FramePointer fpRoot);

    // Does this FrameInfo represent a method frame? (aka a frameless frame)
    // This may be combined w/ both StubFrames and ChainMarkers.
    bool HasMethodFrame() const { return md != NULL && !internal; }

    // Is this frame for a stub?
    // This is mutually exclusive w/ Chain Markers.
    // StubFrames may also have a method frame as a "hint". Ex, a stub frame for a
    // M2U transition may have the Method for the Managed Wrapper for the unmanaged call.
    // Stub frames map to internal frames on the RS.  They use the same enum
    // (CorDebugInternalFrameType) to represent the type of the frame.
    bool HasStubFrame() const { return eStubFrameType != STUBFRAME_NONE; }

    // Does this FrameInfo mark the start of a new chain? (A Frame info may both
    // start a chain and represent a method)
    bool HasChainMarker() const { return chainReason != CHAIN_NONE; }

    // Helper functions for retrieving the DJI and the DMI
    DebuggerJitInfo * GetJitInfoFromFrame() const;
    DebuggerMethodInfo * GetMethodInfoFromFrameOrThrow();

    // Debug helper which nops in retail; and asserts invariants in debug.
#ifdef _DEBUG
    void AssertValid();

    // Debug helpers to get name of frame. Useful in asserts + log statements.
    LPCUTF8 DbgGetClassName();
    LPCUTF8 DbgGetMethodName();
#endif

protected:
    // These are common internal helpers shared by the other Init*() helpers above.
    void InitForScratchFrameInfo();
    void InitFromStubHelper(CrawlFrame * pCF, MethodDesc * pMDHint, CorDebugInternalFrameType type);

};

//StackWalkAction (*DebuggerStackCallback):  This callback will
// be invoked by DebuggerWalkStackProc at each method frame and explicit frame, passing the FrameInfo
// and callback-defined pData to the method.  The callback then returns a
// SWA - if SWA_ABORT is returned then the walk stops immediately.  If
// SWA_CONTINUE is called, then the frame is walked & the next higher frame
// will be used.  If the current explicit frame is at the root of the stack, then
// in the next iteration, DSC will be invoked with FrameInfo::frame == FRAME_TOP
typedef StackWalkAction (*DebuggerStackCallback)(FrameInfo *frame, void *pData);

//StackWalkAction DebuggerWalkStack():  Sets up everything for a
// stack walk for the debugger, starts the stack walk (via
// g_pEEInterface->StackWalkFramesEx), then massages the output.  Note that it
// takes a DebuggerStackCallback as an argument, but at each method frame and explicit frame
// DebuggerWalkStackProc gets called, which in turn calls the
// DebuggerStackCallback.
// Thread * thread:  the thread on which to do a stackwalk
// void *targetFP:  If you're looking for a specific frame, then
//  this should be set to the fp for that frame, and the callback won't
//  be called until that frame is reached.  Otherwise, set it to LEAF_MOST_FRAME &
//  the callback will be called on every frame.
// CONTEXT *context:  Never NULL, b/c the callbacks require the
//  CONTEXT as a place to store some information.  Either it points to an
//  uninitialized CONTEXT (contextValid should be false), or
//  a pointer to a valid CONTEXT for the thread.  If it's NULL, InitRegDisplay
//  will fill it in for us, so we shouldn't go out of our way to set this up.
// bool contextValid:  TRUE if context points to a valid CONTEXT, FALSE
//  otherwise.
// DebuggerStackCallback pCallback:  User supplied callback to
//  be invoked at every frame that's at targetFP or higher.
// void *pData:   User supplied data that we shuffle around,
//  and then hand to pCallback.
// BOOL fIgnoreNonmethodFrames: Generally true for end user stackwalking (e.g. displaying a stack trace) and
//  false for stepping (e.g. stepping out).

StackWalkAction DebuggerWalkStack(Thread *thread,
                                  FramePointer targetFP,
                                  T_CONTEXT *pContext,
                                  BOOL contextValid,
                                  DebuggerStackCallback pCallback,
                                  void *pData,
                                  BOOL fIgnoreNonmethodFrames);

#endif // FRAMEINFO_H_
