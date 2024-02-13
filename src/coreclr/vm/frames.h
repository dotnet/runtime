// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// FRAMES.H


//
// These C++ classes expose activation frames to the rest of the EE.
// Activation frames are actually created by JIT-generated or stub-generated
// code on the machine stack. Thus, the layout of the Frame classes and
// the JIT/Stub code generators are tightly interwined.
//
// IMPORTANT: Since frames are not actually constructed by C++,
// don't try to define constructor/destructor functions. They won't get
// called.
//
// IMPORTANT: Not all methods have full-fledged activation frames (in
// particular, the JIT may create frameless methods.) This is one reason
// why Frame doesn't expose a public "Next()" method: such a method would
// skip frameless method calls. You must instead use one of the
// StackWalk methods.
//
//
// The following is the hierarchy of frames:
//
//    Frame                     - the root class. There are no actual instances
//    |                           of Frames.
//    |
//    +- FaultingExceptionFrame - this frame was placed on a method which faulted
//    |                           to save additional state information
//    |
#ifdef FEATURE_HIJACK
//    |
//    +-HijackFrame             - if a method's return address is hijacked, we
//    |                           construct one of these to allow crawling back
//    |                           to where the return should have gone.
//    |
//    +-ResumableFrame          - this abstract frame provides the context necessary to
//    | |                         allow garbage collection during handling of
//    | |                         a resumable exception (e.g. during edit-and-continue,
//    | |                         or under GCStress4).
//    | |
//    | +-RedirectedThreadFrame - this frame is used for redirecting threads during suspension
//    |
#endif // FEATURE_HIJACK
//    |
//    |
//    |
//    +-InlinedCallFrame        - if a call to unmanaged code is hoisted into
//    |                           a JIT'ted caller, the calling method keeps
//    |                           this frame linked throughout its activation.
//    |
//    +-HelperMethodFrame       - frame used allow stack crawling inside jit helpers and fcalls
//    | |
//    + +-HelperMethodFrame_1OBJ- reports additional object references
//    | |
//    + +-HelperMethodFrame_2OBJ- reports additional object references
//    | |
//    + +-HelperMethodFrame_3OBJ- reports additional object references
//    | |
//    + +-HelperMethodFrame_PROTECTOBJ - reports additional object references
//    |
//    +-TransitionFrame         - this abstract frame represents a transition from
//    | |                         one or more nested frameless method calls
//    | |                         to either a EE runtime helper function or
//    | |                         a framed method.
//    | |
//    | +-MulticastFrame        - this frame protects arguments to a MulticastDelegate
//    |                           Invoke() call while calling each subscriber.
//    |
//    | +-FramedMethodFrame     - this abstract frame represents a call to a method
//    |   |                       that generates a full-fledged frame.
//    |   |
#ifdef FEATURE_COMINTEROP
//    |   |
//    |   +-ComPlusMethodFrame  - represents a CLR to COM call using the generic worker
//    |   |
#endif //FEATURE_COMINTEROP
//    |   |
//    |   +-PInvokeCalliFrame   - protects arguments when a call to GetILStubForCalli is made
//    |   |                       to get or create IL stub for an unmanaged CALLI
//    |   |
//    |   +-PrestubMethodFrame  - represents a call to a prestub
//    |   |
//    |   +-StubDispatchFrame   - represents a call into the virtual call stub manager
//    |   |
//    |   +-CallCountingHelperFrame - represents a call into the call counting helper when the
//    |   |                           call count threshold is reached
//    |   |
//    |   +-ExternalMethodFrame  - represents a call from an ExternalMethdThunk
//    |   |
//    |   +-TPMethodFrame       - for calls on transparent proxy
//    |
#ifdef FEATURE_COMINTEROP
//    +-UnmanagedToManagedFrame - this frame represents a transition from
//    | |                         unmanaged code back to managed code. It's
//    | |                         main functions are to stop COM+ exception
//    | |                         propagation and to expose unmanaged parameters.
//    | |
//    | +-ComMethodFrame        - this frame represents a transition from
//    |   |                       com to com+
//    |   |
//    |   +-ComPrestubMethodFrame - prestub frame for calls from COM to CLR
//    |
#endif //FEATURE_COMINTEROP
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
//    +-TailCallFrame           - padding for tailcalls
//    |
#endif
//    +-ProtectByRefsFrame
//    |
//    +-ProtectValueClassFrame
//    |
//    +-DebuggerClassInitMarkFrame - marker frame to indicate that "class init" code is running
//    |
//    +-DebuggerSecurityCodeMarkFrame - marker frame to indicate that security code is running
//    |
//    +-DebuggerExitFrame - marker frame to indicate that a "break" IL instruction is being executed
//    |
//    +-DebuggerU2MCatchHandlerFrame - marker frame to indicate that native code is going to catch and
//    |                                swallow a managed exception
//    |
#ifdef DEBUGGING_SUPPORTED
//    +-FuncEvalFrame         - frame for debugger function evaluation
#endif // DEBUGGING_SUPPORTED
//    |
//    |
//    +-ExceptionFilterFrame - this frame wraps call to exception filter
//
//------------------------------------------------------------------------
#if 0
//------------------------------------------------------------------------

This is the list of Interop stubs & transition helpers with information
regarding what (if any) Frame they used and where they were set up:

P/Invoke:
 JIT inlined: The code to call the method is inlined into the caller by the JIT.
    InlinedCallFrame is erected by the JITted code.
 Requires marshaling: The stub does not erect any frames explicitly but contains
    an unmanaged CALLI which turns it into the JIT inlined case.

Delegate over a native function pointer:
 The same as P/Invoke but the raw JIT inlined case is not present (the call always
 goes through an IL stub).

Calli:
 The same as P/Invoke.
 PInvokeCalliFrame is erected in stub generated by GenerateGetStubForPInvokeCalli
 before calling to GetILStubForCalli which generates the IL stub. This happens only
 the first time a call via the corresponding VASigCookie is made.

ClrToCom:
 Late-bound or eventing: The stub is generated by GenerateGenericComplusWorker
    (x86) or exists statically as GenericComPlusCallStub[RetBuffArg] (64-bit),
    and it erects a ComPlusMethodFrame frame.
 Early-bound: The stub does not erect any frames explicitly but contains an
    unmanaged CALLI which turns it into the JIT inlined case.

ComToClr:
  Normal stub:
 Interpreted: The stub is generated by ComCall::CreateGenericComCallStub
    (in ComToClrCall.cpp) and it erects a ComMethodFrame frame.
Prestub:
 The prestub is ComCallPreStub (in ComCallableWrapper.cpp) and it erects
    a ComPrestubMethodFrame frame.

Reverse P/Invoke (used for C++ exports & fixups as well as delegates
obtained from function pointers):
  Normal stub:
 The stub exists statically as UMThunkStub and calls to IL stub.
Prestub:
 The prestub exists statically as TheUMEntryPrestub.

//------------------------------------------------------------------------
#endif // 0
//------------------------------------------------------------------------

#ifndef FRAME_ABSTRACT_TYPE_NAME
#define FRAME_ABSTRACT_TYPE_NAME(frameType)
#endif
#ifndef FRAME_TYPE_NAME
#define FRAME_TYPE_NAME(frameType)
#endif

FRAME_ABSTRACT_TYPE_NAME(FrameBase)
FRAME_ABSTRACT_TYPE_NAME(Frame)
FRAME_ABSTRACT_TYPE_NAME(TransitionFrame)
#ifdef FEATURE_HIJACK
FRAME_TYPE_NAME(ResumableFrame)
FRAME_TYPE_NAME(RedirectedThreadFrame)
#endif // FEATURE_HIJACK
FRAME_TYPE_NAME(FaultingExceptionFrame)
#ifdef DEBUGGING_SUPPORTED
FRAME_TYPE_NAME(FuncEvalFrame)
#endif // DEBUGGING_SUPPORTED
FRAME_TYPE_NAME(HelperMethodFrame)
FRAME_TYPE_NAME(HelperMethodFrame_1OBJ)
FRAME_TYPE_NAME(HelperMethodFrame_2OBJ)
FRAME_TYPE_NAME(HelperMethodFrame_3OBJ)
FRAME_TYPE_NAME(HelperMethodFrame_PROTECTOBJ)
FRAME_ABSTRACT_TYPE_NAME(FramedMethodFrame)
FRAME_TYPE_NAME(MulticastFrame)
#ifdef FEATURE_COMINTEROP
FRAME_ABSTRACT_TYPE_NAME(UnmanagedToManagedFrame)
FRAME_TYPE_NAME(ComMethodFrame)
FRAME_TYPE_NAME(ComPlusMethodFrame)
FRAME_TYPE_NAME(ComPrestubMethodFrame)
#endif // FEATURE_COMINTEROP
FRAME_TYPE_NAME(PInvokeCalliFrame)
#ifdef FEATURE_HIJACK
FRAME_TYPE_NAME(HijackFrame)
#endif // FEATURE_HIJACK
FRAME_TYPE_NAME(PrestubMethodFrame)
FRAME_TYPE_NAME(CallCountingHelperFrame)
FRAME_TYPE_NAME(StubDispatchFrame)
FRAME_TYPE_NAME(ExternalMethodFrame)
#ifdef FEATURE_READYTORUN
FRAME_TYPE_NAME(DynamicHelperFrame)
#endif
#ifdef FEATURE_INTERPRETER
FRAME_TYPE_NAME(InterpreterFrame)
#endif // FEATURE_INTERPRETER
FRAME_TYPE_NAME(ProtectByRefsFrame)
FRAME_TYPE_NAME(ProtectValueClassFrame)
FRAME_TYPE_NAME(DebuggerClassInitMarkFrame)
FRAME_TYPE_NAME(DebuggerSecurityCodeMarkFrame)
FRAME_TYPE_NAME(DebuggerExitFrame)
FRAME_TYPE_NAME(DebuggerU2MCatchHandlerFrame)
FRAME_TYPE_NAME(InlinedCallFrame)
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
FRAME_TYPE_NAME(TailCallFrame)
#endif
FRAME_TYPE_NAME(ExceptionFilterFrame)
#if defined(_DEBUG)
FRAME_TYPE_NAME(AssumeByrefFromJITStack)
#endif // _DEBUG

#undef FRAME_ABSTRACT_TYPE_NAME
#undef FRAME_TYPE_NAME

//------------------------------------------------------------------------

#ifndef __frames_h__
#define __frames_h__
#if defined(_MSC_VER) && defined(TARGET_X86) && !defined(FPO_ON)
#pragma optimize("y", on)   // Small critical routines, don't put in EBP frame
#define FPO_ON 1
#define FRAMES_TURNED_FPO_ON 1
#endif

#include "util.hpp"
#include "vars.hpp"
#include "regdisp.h"
#include "object.h"
#include <stddef.h>
#include "siginfo.hpp"
#include "method.hpp"
#include "stackwalk.h"
#include "stubmgr.h"
#include "gms.h"
#include "threads.h"
#include "callingconvention.h"

// Forward references
class Frame;
class FramedMethodFrame;
typedef VPTR(class FramedMethodFrame) PTR_FramedMethodFrame;
struct HijackArgs;
struct ResolveCacheElem;
#if defined(DACCESS_COMPILE)
class DacDbiInterfaceImpl;
#endif // DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP
class ComMethodFrame;
class ComCallMethodDesc;
#endif // FEATURE_COMINTEROP

// Note: the value (-1) is used to generate the largest possible pointer value: this keeps frame addresses
// increasing upward. Because we want to ensure that we don't accidentally change this, we have a C_ASSERT
// in stackwalk.cpp. Since it requires constant values as args, we need to define FRAME_TOP in two steps.
// First we define FRAME_TOP_VALUE which we'll use when we do the compile-time check, then we'll define
// FRAME_TOP in terms of FRAME_TOP_VALUE. Defining FRAME_TOP as a PTR_Frame means we don't have to type cast
// whenever we compare it to a PTR_Frame value (the usual use of the value).
#define FRAME_TOP_VALUE  ~0     // we want to say -1 here, but gcc has trouble with the signed value
#define FRAME_TOP (PTR_Frame(FRAME_TOP_VALUE))

#ifndef DACCESS_COMPILE

#if defined(TARGET_UNIX)

#define DEFINE_DTOR(klass)                      \
    public:                                     \
        virtual ~klass() { PopIfChained(); }

#else

#define DEFINE_DTOR(klass)

#endif // TARGET_UNIX

#define DEFINE_VTABLE_GETTER(klass)             \
    public:                                     \
        static TADDR GetMethodFrameVPtr() {     \
            LIMITED_METHOD_CONTRACT;            \
            klass boilerplate(false);           \
            return *((TADDR*)&boilerplate);     \
        }                                       \
        klass(bool dummy) { LIMITED_METHOD_CONTRACT; }

#define DEFINE_VTABLE_GETTER_AND_DTOR(klass)    \
        DEFINE_VTABLE_GETTER(klass)             \
        DEFINE_DTOR(klass)

#define DEFINE_VTABLE_GETTER_AND_CTOR(klass)    \
        DEFINE_VTABLE_GETTER(klass)             \
    protected:                                  \
        klass() { LIMITED_METHOD_CONTRACT; }

#define DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(klass)    \
        DEFINE_VTABLE_GETTER_AND_DTOR(klass)             \
    protected:                                           \
        klass() { LIMITED_METHOD_CONTRACT; }

#else

#define DEFINE_VTABLE_GETTER(klass)             \
    public:                                     \
        static TADDR GetMethodFrameVPtr() {     \
            LIMITED_METHOD_CONTRACT;            \
            return klass::VPtrTargetVTable();   \
        }                                       \

#define DEFINE_VTABLE_GETTER_AND_DTOR(klass)    \
        DEFINE_VTABLE_GETTER(klass)             \

#define DEFINE_VTABLE_GETTER_AND_CTOR(klass)    \
        DEFINE_VTABLE_GETTER(klass)             \

#define DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(klass)    \
        DEFINE_VTABLE_GETTER_AND_CTOR(klass)             \

#endif // #ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// For reporting on types of frames at runtime.
class FrameTypeName
{
public:
    TADDR vtbl;
    PTR_CSTR name;
};
typedef DPTR(FrameTypeName) PTR_FrameTypeName;

//-----------------------------------------------------------------------------
// Frame depends on the location of its vtable within the object. This
// superclass ensures that the vtable for Frame objects is in the same
// location under both MSVC and GCC.
//-----------------------------------------------------------------------------

class FrameBase
{
    VPTR_BASE_VTABLE_CLASS(FrameBase)

public:
    FrameBase() {LIMITED_METHOD_CONTRACT; }

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc) {
        LIMITED_METHOD_CONTRACT;
        // Nothing to protect
    }

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags) = 0;
#endif
};

//------------------------------------------------------------------------
// Frame defines methods common to all frame types. There are no actual
// instances of root frames.
//------------------------------------------------------------------------

class Frame : public FrameBase
{
    friend class CheckAsmOffsets;
#ifdef DACCESS_COMPILE
    friend void Thread::EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    VPTR_ABSTRACT_VTABLE_CLASS(Frame, FrameBase)

public:

    //------------------------------------------------------------------------
    // Special characteristics of a frame
    //------------------------------------------------------------------------
    enum FrameAttribs {
        FRAME_ATTR_NONE = 0,
        FRAME_ATTR_EXCEPTION = 1,           // This frame caused an exception
        FRAME_ATTR_FAULTED = 4,             // Exception caused by Win32 fault
        FRAME_ATTR_RESUMABLE = 8,           // We may resume from this frame
        FRAME_ATTR_CAPTURE_DEPTH_2 = 0x10,  // This is a helperMethodFrame and the capture occurred at depth 2
        FRAME_ATTR_EXACT_DEPTH = 0x20,      // This is a helperMethodFrame and a jit helper, but only crawl to the given depth
        FRAME_ATTR_NO_THREAD_ABORT = 0x40,  // This is a helperMethodFrame that should not trigger thread aborts on entry
    };
    virtual unsigned GetFrameAttribs()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return FRAME_ATTR_NONE;
    }

    //------------------------------------------------------------------------
    // Performs cleanup on an exception unwind
    //------------------------------------------------------------------------
#ifndef DACCESS_COMPILE
    virtual void ExceptionUnwind()
    {
        // Nothing to do here.
        LIMITED_METHOD_CONTRACT;
    }
#endif

    // Should be overridden to return TRUE if the frame contains register
    // state of the caller.
    virtual BOOL NeedsUpdateRegDisplay()
    {
        return FALSE;
    }

    //------------------------------------------------------------------------
    // Is this a frame used on transition to native code from jitted code?
    //------------------------------------------------------------------------
    virtual BOOL IsTransitionToNativeFrame()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }

    virtual MethodDesc *GetFunction()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return NULL;
    }

    virtual Assembly *GetAssembly()
    {
        WRAPPER_NO_CONTRACT;
        MethodDesc *pMethod = GetFunction();
        if (pMethod != NULL)
            return pMethod->GetModule()->GetAssembly();
        else
            return NULL;
    }

    // indicate the current X86 IP address within the current method
    // return 0 if the information is not available
    virtual PTR_BYTE GetIP()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }

    // DACCESS: GetReturnAddressPtr should return the
    // target address of the return address in the frame.
    virtual TADDR GetReturnAddressPtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return NULL;
    }

    // ASAN doesn't like us messing with the return address.
    virtual DISABLE_ASAN PCODE GetReturnAddress()
    {
        WRAPPER_NO_CONTRACT;
        TADDR ptr = GetReturnAddressPtr();
        return (ptr != NULL) ? *PTR_PCODE(ptr) : NULL;
    }

#ifndef DACCESS_COMPILE
    virtual Object **GetReturnExecutionContextAddr()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }

    // ASAN doesn't like us messing with the return address.
    void DISABLE_ASAN SetReturnAddress(TADDR val)
    {
        WRAPPER_NO_CONTRACT;
        TADDR ptr = GetReturnAddressPtr();
        _ASSERTE(ptr != NULL);
        *(TADDR*)ptr = val;
    }
#endif // #ifndef DACCESS_COMPILE

    PTR_GSCookie GetGSCookiePtr()
    {
        WRAPPER_NO_CONTRACT;
        return dac_cast<PTR_GSCookie>(dac_cast<TADDR>(this) + GetOffsetOfGSCookie());
    }

    static int GetOffsetOfGSCookie()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return -(int)sizeof(GSCookie);
    }

    static bool HasValidVTablePtr(Frame * pFrame);
    static PTR_GSCookie SafeGetGSCookiePtr(Frame * pFrame);
    static void Init();

    // Callers, note that the REGDISPLAY parameter is actually in/out. While
    // UpdateRegDisplay is generally used to fill out the REGDISPLAY parameter, some
    // overrides (e.g., code:ResumableFrame::UpdateRegDisplay) will actually READ what
    // you pass in. So be sure to pass in a valid or zeroed out REGDISPLAY.
    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return;
    }

    //------------------------------------------------------------------------
    // Debugger support
    //------------------------------------------------------------------------


public:
    enum ETransitionType
    {
        TT_NONE,
        TT_M2U, // we can safely cast to a FramedMethodFrame
        TT_U2M, // we can safely cast to a UnmanagedToManagedFrame
        TT_AppDomain, // transitioniting between AppDomains.
        TT_InternalCall, // calling into the CLR (ecall/fcall).
    };

    // Get the type of transition.
    // M-->U, U-->M
    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_NONE;
    }

    enum
    {
        TYPE_INTERNAL,
        TYPE_ENTRY,
        TYPE_EXIT,
        TYPE_CONTEXT_CROSS,
        TYPE_INTERCEPTION,
        TYPE_SECURITY,
        TYPE_CALL,
        TYPE_FUNC_EVAL,
        TYPE_MULTICAST,

        // HMFs and derived classes should use this so the profiling API knows it needs
        // to ensure HMF-specific lazy initialization gets done w/out re-entering to the host.
        TYPE_HELPER_METHOD_FRAME,

        TYPE_COUNT
    };

    virtual int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_INTERNAL;
    };

    // When stepping into a method, various other methods may be called.
    // These are refererred to as interceptors. They are all invoked
    // with frames of various types. GetInterception() indicates whether
    // the frame was set up for execution of such interceptors

    enum Interception
    {
        INTERCEPTION_NONE,
        INTERCEPTION_CLASS_INIT,
        INTERCEPTION_EXCEPTION,
        INTERCEPTION_CONTEXT,
        INTERCEPTION_SECURITY,
        INTERCEPTION_PRESTUB,
        INTERCEPTION_OTHER,

        INTERCEPTION_COUNT
    };

    virtual Interception GetInterception()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return INTERCEPTION_NONE;
    }

    // Return information about an unmanaged call the frame
    // will make.
    // ip - the unmanaged routine which will be called
    // returnIP - the address in the stub which the unmanaged routine
    //            will return to.
    // returnSP - the location returnIP is pushed onto the stack
    //            during the call.
    //
    virtual void GetUnmanagedCallSite(TADDR* ip,
                                      TADDR* returnIP,
                                      TADDR* returnSP)
    {
        LIMITED_METHOD_CONTRACT;
        if (ip)
            *ip = NULL;

        if (returnIP)
            *returnIP = NULL;

        if (returnSP)
            *returnSP = NULL;
    }

    // Return where the frame will execute next - the result is filled
    // into the given "trace" structure.  The frame is responsible for
    // detecting where it is in its execution lifetime.
    virtual BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                            TraceDestination *trace, REGDISPLAY *regs)
    {
        LIMITED_METHOD_CONTRACT;
        LOG((LF_CORDB, LL_INFO10000,
             "Default TraceFrame always returns false.\n"));
        return FALSE;
    }

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        WRAPPER_NO_CONTRACT;
        DAC_ENUM_VTHIS();

        // Many frames store a MethodDesc pointer in m_Datum
        // so pick that up automatically.
        MethodDesc* func = GetFunction();
        if (func)
        {
            func->EnumMemoryRegions(flags);
        }

        // Include the NegSpace
        GSCookie * pGSCookie = GetGSCookiePtr();
        _ASSERTE(FitsIn<ULONG32>(PBYTE(pGSCookie) - PBYTE(this)));
        ULONG32 negSpaceSize = static_cast<ULONG32>(PBYTE(pGSCookie) - PBYTE(this));
        DacEnumMemoryRegion(dac_cast<TADDR>(this) - negSpaceSize, negSpaceSize);
    }
#endif

    //---------------------------------------------------------------
    // Expose key offsets and values for stub generation.
    //---------------------------------------------------------------
    static BYTE GetOffsetOfNextLink()
    {
        WRAPPER_NO_CONTRACT;
        size_t ofs = offsetof(class Frame, m_Next);
        _ASSERTE(FitsInI1(ofs));
        return (BYTE)ofs;
    }

    // get your VTablePointer (can be used to check what type the frame is)
    TADDR GetVTablePtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return VPTR_HOST_VTABLE_TO_TADDR(*(LPVOID*)this);
    }

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    virtual BOOL Protects(OBJECTREF *ppObjectRef)
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }
#endif

#ifndef DACCESS_COMPILE
    // Link and Unlink this frame
    VOID Push();
    VOID Pop();
    VOID Push(Thread *pThread);
    VOID Pop(Thread *pThread);
#endif // DACCESS_COMPILE

#ifdef _DEBUG_IMPL
    void Log();
    static BOOL ShouldLogTransitions() { WRAPPER_NO_CONTRACT; return LoggingOn(LF_STUBS, LL_INFO1000000); }
    static void __stdcall LogTransition(Frame* frame);
    void LogFrame(int LF, int LL);       // General purpose logging.
    void LogFrameChain(int LF, int LL);  // Log the whole chain.
    virtual const char* GetFrameTypeName() {return NULL;}
    static PTR_CSTR GetFrameTypeName(TADDR vtbl);
#endif

    //------------------------------------------------------------------------
    // Returns the address of a security object or
    // null if there is no space for an object on this frame.
    //------------------------------------------------------------------------
    virtual OBJECTREF *GetAddrOfSecurityDesc()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }

private:
    // Pointer to the next frame up the stack.

protected:
    PTR_Frame m_Next;        // offset +4

public:
    PTR_Frame PtrNextFrame() { return m_Next; }

private:
    // Because JIT-method activations cannot be expressed as Frames,
    // everyone must use the StackCrawler to walk the frame chain
    // reliably. We'll expose the Next method only to the StackCrawler
    // to prevent mistakes.
    /*<TODO>@NICE: Restrict "friendship" again to the StackWalker method;
      not done because of circular dependency with threads.h</TODO>
    */
    //        friend Frame* Thread::StackWalkFrames(PSTACKWALKFRAMESCALLBACK pCallback, VOID *pData);
    friend class Thread;
    friend void CrawlFrame::GotoNextFrame();
    friend class StackFrameIterator;
    friend class TailCallFrame;
    friend class AppDomain;
    friend VOID RealCOMPlusThrow(OBJECTREF);
    friend FCDECL0(VOID, JIT_StressGC);
#ifdef _DEBUG
    friend LONG WINAPI CLRVectoredExceptionHandlerShim(PEXCEPTION_POINTERS pExceptionInfo);
#endif
#ifdef HOST_64BIT
    friend Thread * __stdcall JIT_InitPInvokeFrame(InlinedCallFrame *pFrame, PTR_VOID StubSecretArg);
#endif
#ifdef FEATURE_EH_FUNCLETS
    friend class ExceptionTracker;
#endif
#if defined(DACCESS_COMPILE)
    friend class DacDbiInterfaceImpl;
#endif // DACCESS_COMPILE

    PTR_Frame  Next()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_Next;
    }

protected:
    // Frame is considered an abstract class: this protected constructor
    // causes any attempt to instantiate one to fail at compile-time.
    Frame()
    : m_Next(dac_cast<PTR_Frame>(nullptr))
    {
        LIMITED_METHOD_CONTRACT;
    }

#ifndef DACCESS_COMPILE
#if !defined(TARGET_X86) || defined(TARGET_UNIX)
    static void UpdateFloatingPointRegisters(const PREGDISPLAY pRD);
#endif // !TARGET_X86 || TARGET_UNIX
#endif // DACCESS_COMPILE

#if defined(TARGET_UNIX) && !defined(DACCESS_COMPILE)
    virtual ~Frame() { LIMITED_METHOD_CONTRACT; }

    void PopIfChained();
#endif // TARGET_UNIX && !DACCESS_COMPILE
};


//-----------------------------------------------------------------------------
// This frame provides context for a frame that
// took an exception that is going to be resumed.
//
// It is necessary to create this frame if garbage
// collection may happen during handling of the
// exception.  The FRAME_ATTR_RESUMABLE flag tells
// the GC that the preceding frame needs to be treated
// like the top of stack (with the important implication that
// caller-save-registers will be potential roots).
//-----------------------------------------------------------------------------
#ifdef FEATURE_HIJACK
//-----------------------------------------------------------------------------

class ResumableFrame : public Frame
{
    VPTR_VTABLE_CLASS(ResumableFrame, Frame)

public:
#ifndef DACCESS_COMPILE
    ResumableFrame(T_CONTEXT* regs) {
        LIMITED_METHOD_CONTRACT;
        m_Regs = regs;
    }
#endif

    virtual TADDR GetReturnAddressPtr();

    virtual BOOL NeedsUpdateRegDisplay()
    {
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats = false);

    virtual unsigned GetFrameAttribs() {
        LIMITED_METHOD_DAC_CONTRACT;
        return FRAME_ATTR_RESUMABLE;    // Treat the next frame as the top frame.
    }

    T_CONTEXT *GetContext() {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_Regs);
    }

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        WRAPPER_NO_CONTRACT;
        Frame::EnumMemoryRegions(flags);
        m_Regs.EnumMem();
    }
#endif

protected:
    PTR_CONTEXT m_Regs;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(ResumableFrame)
};


//-----------------------------------------------------------------------------
// RedirectedThreadFrame
//-----------------------------------------------------------------------------

class RedirectedThreadFrame : public ResumableFrame
{
    VPTR_VTABLE_CLASS(RedirectedThreadFrame, ResumableFrame)
    VPTR_UNIQUE(VPTR_UNIQUE_RedirectedThreadFrame)

public:
#ifndef DACCESS_COMPILE
    RedirectedThreadFrame(T_CONTEXT *regs) : ResumableFrame(regs) {
        LIMITED_METHOD_CONTRACT;
    }

    virtual void ExceptionUnwind();
#endif

    virtual void GcScanRoots(promote_func* fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
#if defined(FEATURE_CONSERVATIVE_GC) && !defined(DACCESS_COMPILE)
        if (sc->promotion && g_pConfig->GetGCConservative())
        {

#ifdef TARGET_AMD64
            Object** firstIntReg = (Object**)&this->GetContext()->Rax;
            Object** lastIntReg  = (Object**)&this->GetContext()->R15;
#elif defined(TARGET_X86)
            Object** firstIntReg = (Object**)&this->GetContext()->Edi;
            Object** lastIntReg  = (Object**)&this->GetContext()->Ebp;
#elif defined(TARGET_ARM)
            Object** firstIntReg = (Object**)&this->GetContext()->R0;
            Object** lastIntReg  = (Object**)&this->GetContext()->R12;
#elif defined(TARGET_ARM64)
            Object** firstIntReg = (Object**)&this->GetContext()->X0;
            Object** lastIntReg  = (Object**)&this->GetContext()->X28;
#elif defined(TARGET_LOONGARCH64)
            Object** firstIntReg = (Object**)&this->GetContext()->Tp;
            Object** lastIntReg  = (Object**)&this->GetContext()->S8;
#elif defined(TARGET_RISCV64)
            Object** firstIntReg = (Object**)&this->GetContext()->Gp;
            Object** lastIntReg  = (Object**)&this->GetContext()->T6;
#else
            _ASSERTE(!"nyi for platform");
#endif
            for (Object** ppObj = firstIntReg; ppObj <= lastIntReg; ppObj++)
            {
                fn(ppObj, sc, GC_CALL_INTERIOR | GC_CALL_PINNED);
            }
        }
#endif
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(RedirectedThreadFrame)
};

typedef DPTR(RedirectedThreadFrame) PTR_RedirectedThreadFrame;

inline BOOL ISREDIRECTEDTHREAD(Thread * thread)
{
    WRAPPER_NO_CONTRACT;
    return (thread->GetFrame() != FRAME_TOP &&
            thread->GetFrame()->GetVTablePtr() ==
            RedirectedThreadFrame::GetMethodFrameVPtr());
}

inline T_CONTEXT * GETREDIRECTEDCONTEXT(Thread * thread)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(ISREDIRECTEDTHREAD(thread));
    return dac_cast<PTR_RedirectedThreadFrame>(thread->GetFrame())->GetContext();
}

//------------------------------------------------------------------------
#else // FEATURE_HIJACK
//------------------------------------------------------------------------

inline BOOL ISREDIRECTEDTHREAD(Thread * thread) { LIMITED_METHOD_CONTRACT; return FALSE; }
inline CONTEXT * GETREDIRECTEDCONTEXT(Thread * thread) { LIMITED_METHOD_CONTRACT; return (CONTEXT*) NULL; }

//------------------------------------------------------------------------
#endif // FEATURE_HIJACK
//------------------------------------------------------------------------
// This frame represents a transition from one or more nested frameless
// method calls to either a EE runtime helper function or a framed method.
// Because most stackwalks from the EE start with a full-fledged frame,
// anything but the most trivial call into the EE has to push this
// frame in order to prevent the frameless methods inbetween from
// getting lost.
//------------------------------------------------------------------------

class TransitionFrame : public Frame
{
    VPTR_ABSTRACT_VTABLE_CLASS(TransitionFrame, Frame)

public:
    virtual TADDR GetTransitionBlock() = 0;

    // DACCESS: GetReturnAddressPtr should return the
    // target address of the return address in the frame.
    virtual TADDR GetReturnAddressPtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetTransitionBlock() + TransitionBlock::GetOffsetOfReturnAddress();
    }

    //---------------------------------------------------------------
    // Get the "this" object.
    //---------------------------------------------------------------
    OBJECTREF GetThis()
    {
        WRAPPER_NO_CONTRACT;
        Object* obj = PTR_Object(*PTR_TADDR(GetAddrOfThis()));
        return ObjectToOBJECTREF(obj);
    }

    PTR_OBJECTREF GetThisPtr()
    {
        WRAPPER_NO_CONTRACT;
        return PTR_OBJECTREF(GetAddrOfThis());
    }

    //---------------------------------------------------------------
    // Get the extra info for shared generic code.
    //---------------------------------------------------------------
    PTR_VOID GetParamTypeArg();

    //---------------------------------------------------------------
    // Gets value indicating whether the generic parameter type
    // argument should be suppressed.
    //---------------------------------------------------------------
    virtual BOOL SuppressParamTypeArg()
    {
        return FALSE;
    }

protected:  // we don't want people using this directly
    //---------------------------------------------------------------
    // Get the address of the "this" object. WARNING!!! Whether or not "this"
    // is gc-protected is depends on the frame type!!!
    //---------------------------------------------------------------
    TADDR GetAddrOfThis();

public:
    //---------------------------------------------------------------
    // For vararg calls, return cookie.
    //---------------------------------------------------------------
    VASigCookie *GetVASigCookie();

    CalleeSavedRegisters *GetCalleeSavedRegisters()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_CalleeSavedRegisters>(
            GetTransitionBlock() + TransitionBlock::GetOffsetOfCalleeSavedRegisters());
    }

    ArgumentRegisters *GetArgumentRegisters()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_ArgumentRegisters>(
            GetTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters());
    }

    TADDR GetSP()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetTransitionBlock() + sizeof(TransitionBlock);
    }

    virtual BOOL NeedsUpdateRegDisplay()
    {
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false);
#ifdef TARGET_X86
    void UpdateRegDisplayHelper(const PREGDISPLAY, UINT cbStackPop);
#endif

#if defined (_DEBUG) && !defined (DACCESS_COMPILE)
    virtual BOOL Protects(OBJECTREF *ppORef);
#endif //defined (_DEBUG) && defined (DACCESS_COMPILE)

    // For use by classes deriving from FramedMethodFrame.
    void PromoteCallerStack(promote_func* fn, ScanContext* sc);

    void PromoteCallerStackHelper(promote_func* fn, ScanContext* sc,
        MethodDesc * pMD, MetaSig *pmsig);

    void PromoteCallerStackUsingGCRefMap(promote_func* fn, ScanContext* sc, PTR_BYTE pGCRefMap);

#ifdef TARGET_X86
    UINT CbStackPopUsingGCRefMap(PTR_BYTE pGCRefMap);
#endif

protected:
    TransitionFrame()
    {
        LIMITED_METHOD_CONTRACT;
    }
};

//-----------------------------------------------------------------------
// TransitionFrames for exceptions
//-----------------------------------------------------------------------

// The define USE_FEF controls how this class is used.  Look for occurrences
//  of USE_FEF.

class FaultingExceptionFrame : public Frame
{
    friend class CheckAsmOffsets;

#ifndef FEATURE_EH_FUNCLETS
#ifdef TARGET_X86
    DWORD                   m_Esp;
    CalleeSavedRegisters    m_regs;
    TADDR                   m_ReturnAddress;
#else  // TARGET_X86
    #error "Unsupported architecture"
#endif // TARGET_X86
#else // FEATURE_EH_FUNCLETS
    BOOL                    m_fFilterExecuted;  // Flag for FirstCallToHandler
    TADDR                   m_ReturnAddress;
    T_CONTEXT               m_ctx;
#endif // !FEATURE_EH_FUNCLETS

    VPTR_VTABLE_CLASS(FaultingExceptionFrame, Frame)

public:
#ifndef DACCESS_COMPILE
    FaultingExceptionFrame() {
        LIMITED_METHOD_CONTRACT;
    }
#endif

    virtual TADDR GetReturnAddressPtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_HOST_MEMBER_TADDR(FaultingExceptionFrame, this, m_ReturnAddress);
    }

    void Init(T_CONTEXT *pContext);
    void InitAndLink(T_CONTEXT *pContext);

    Interception GetInterception()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return INTERCEPTION_EXCEPTION;
    }

    unsigned GetFrameAttribs()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_EH_FUNCLETS
        return FRAME_ATTR_EXCEPTION | (!!(m_ctx.ContextFlags & CONTEXT_EXCEPTION_ACTIVE) ? FRAME_ATTR_FAULTED : 0);
#else
        return FRAME_ATTR_EXCEPTION | FRAME_ATTR_FAULTED;
#endif        
    }

#ifndef FEATURE_EH_FUNCLETS
    CalleeSavedRegisters *GetCalleeSavedRegisters()
    {
#ifdef TARGET_X86
        LIMITED_METHOD_DAC_CONTRACT;
        return &m_regs;
#else
        PORTABILITY_ASSERT("GetCalleeSavedRegisters");
#endif // TARGET_X86
    }
#endif // FEATURE_EH_FUNCLETS

#ifdef FEATURE_EH_FUNCLETS
    T_CONTEXT *GetExceptionContext ()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_ctx;
    }

    BOOL * GetFilterExecutedFlag()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_fFilterExecuted;
    }
#endif // FEATURE_EH_FUNCLETS

    virtual BOOL NeedsUpdateRegDisplay()
    {
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false);

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(FaultingExceptionFrame)
};

//-----------------------------------------------------------------------
// Frame for debugger function evaluation
//
// This frame holds a ptr to a DebuggerEval object which contains a copy
// of the thread's context at the time it was hijacked for the func
// eval.
//
// UpdateRegDisplay updates all registers inthe REGDISPLAY, not just
// the callee saved registers, because we can hijack for a func eval
// at any point in a thread's execution.
//
//-----------------------------------------------------------------------

#ifdef DEBUGGING_SUPPORTED
class DebuggerEval;
typedef DPTR(class DebuggerEval) PTR_DebuggerEval;

class FuncEvalFrame : public Frame
{
    VPTR_VTABLE_CLASS(FuncEvalFrame, Frame)

    TADDR           m_ReturnAddress;
    PTR_DebuggerEval m_pDebuggerEval;

    BOOL            m_showFrame;

public:
#ifndef DACCESS_COMPILE
    FuncEvalFrame(DebuggerEval *pDebuggerEval, TADDR returnAddress, BOOL showFrame)
    {
        LIMITED_METHOD_CONTRACT;
        m_pDebuggerEval = pDebuggerEval;
        m_ReturnAddress = returnAddress;
        m_showFrame = showFrame;
    }
#endif

    virtual BOOL IsTransitionToNativeFrame()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }

    virtual int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_FUNC_EVAL;
    }

    virtual unsigned GetFrameAttribs();

    virtual BOOL NeedsUpdateRegDisplay()
    {
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false);

    virtual DebuggerEval * GetDebuggerEval();

    virtual TADDR GetReturnAddressPtr();

    /*
     * ShowFrame
     *
     * Returns if this frame should be returned as part of a stack trace to a debugger or not.
     *
     */
    BOOL ShowFrame()
    {
        LIMITED_METHOD_CONTRACT;

        return m_showFrame;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(FuncEvalFrame)
};

typedef VPTR(FuncEvalFrame) PTR_FuncEvalFrame;
#endif // DEBUGGING_SUPPORTED

//----------------------------------------------------------------------------------------------
// A HelperMethodFrame is created by jit helper (Modified slightly it could be used
// for native routines).   This frame just does the callee saved register fixup.
// It does NOT protect arguments; you must use GCPROTECT or one of the HelperMethodFrame
// subclases. (see JitInterface for sample use, YOU CAN'T RETURN WHILE IN THE PROTECTED STATE!)
//----------------------------------------------------------------------------------------------

class HelperMethodFrame : public Frame
{
    VPTR_VTABLE_CLASS(HelperMethodFrame, Frame);

public:
#ifndef DACCESS_COMPILE
    // Lazy initialization of HelperMethodFrame.  Need to
    // call InsureInit to complete initialization
    // If this is an FCall, the first param is the entry point for the FCALL.
    // The MethodDesc will be looked up form this (lazily), and this method
    // will be used in stack reporting, if this is not an FCall pass a 0
    FORCEINLINE HelperMethodFrame(void* fCallFtnEntry, unsigned attribs = 0)
    {
        WRAPPER_NO_CONTRACT;
        // Most of the initialization is actually done in HelperMethodFrame::Push()
        INDEBUG(memset(&m_Attribs, 0xCC, sizeof(HelperMethodFrame) - offsetof(HelperMethodFrame, m_Attribs));)
        m_Attribs = attribs;
        m_FCallEntry = (TADDR)fCallFtnEntry;
    }
#endif // DACCESS_COMPILE

    virtual int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_HELPER_METHOD_FRAME;
    };

    virtual PCODE GetReturnAddress()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (!m_MachState.isValid())
        {
#if defined(DACCESS_COMPILE)
            MachState unwoundState;
            InsureInit(false, &unwoundState);
            return unwoundState.GetRetAddr();
#else  // !DACCESS_COMPILE
            _ASSERTE(!"HMF's should always be initialized in the non-DAC world.");
            return NULL;

#endif // !DACCESS_COMPILE
        }

        return m_MachState.GetRetAddr();
    }

    virtual MethodDesc* GetFunction();

    virtual BOOL NeedsUpdateRegDisplay()
    {
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false);

    virtual Interception GetInterception()
    {
        WRAPPER_NO_CONTRACT;
        LIMITED_METHOD_DAC_CONTRACT;
        if (GetFrameAttribs() & FRAME_ATTR_EXCEPTION)
            return(INTERCEPTION_EXCEPTION);
        return(INTERCEPTION_NONE);
    }

    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_InternalCall;
    }

#ifdef _DEBUG
    void SetAddrOfHaveCheckedRestoreState(BOOL* pDoneCheck)
    {
        m_pDoneCheck = pDoneCheck;
    }

    BOOL HaveDoneConfirmStateCheck()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pDoneCheck != NULL);
        return *m_pDoneCheck;
    }

    void SetHaveDoneConfirmStateCheck()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pDoneCheck != NULL);
        *m_pDoneCheck = TRUE;
    }
#endif

    virtual unsigned GetFrameAttribs()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return(m_Attribs);
    }

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        WRAPPER_NO_CONTRACT;
        Frame::EnumMemoryRegions(flags);
    }
#endif

#ifndef DACCESS_COMPILE
    void Push();
    void Pop();

    FORCEINLINE void Poll()
    {
        WRAPPER_NO_CONTRACT;
        if (m_pThread->CatchAtSafePointOpportunistic())
            CommonTripThread();
    }
#endif // DACCESS_COMPILE

    BOOL InsureInit(bool initialInit, struct MachState* unwindState, HostCallPreference hostCallPreference = AllowHostCalls);

    LazyMachState * MachineState() {
        LIMITED_METHOD_CONTRACT;
        return &m_MachState;
    }

    Thread * GetThread() {
        LIMITED_METHOD_CONTRACT;
        return m_pThread;
    }

private:
    // Slow paths of Push/Pop are factored into a separate functions for better perf.
    NOINLINE void PushSlowHelper();
    NOINLINE void PopSlowHelper();

protected:
    PTR_MethodDesc m_pMD;
    unsigned m_Attribs;
    INDEBUG(BOOL* m_pDoneCheck;)
    PTR_Thread m_pThread;
    TADDR m_FCallEntry;              // used to determine our identity for stack traces

    LazyMachState m_MachState;       // pRetAddr points to the return address and the stack arguments

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(HelperMethodFrame)
};

// Restores registers saved in m_MachState
EXTERN_C int __fastcall HelperMethodFrameRestoreState(
        INDEBUG_COMMA(HelperMethodFrame *pFrame)
        MachState *pState
    );


// workhorse for our promotion efforts
inline void DoPromote(promote_func *fn, ScanContext* sc, OBJECTREF *address, BOOL interior)
{
    WRAPPER_NO_CONTRACT;

    // We use OBJECTREF_TO_UNCHECKED_OBJECTREF since address may be an interior pointer
    LOG((LF_GC, INFO3,
         "    Promoting pointer argument at" FMT_ADDR "from" FMT_ADDR "to ",
         DBG_ADDR(address), DBG_ADDR(OBJECTREF_TO_UNCHECKED_OBJECTREF(*address)) ));

    if (interior)
        PromoteCarefully(fn, PTR_PTR_Object(address), sc);
    else
        (*fn) (PTR_PTR_Object(address), sc, 0);

    LOG((LF_GC, INFO3, "    " FMT_ADDR "\n", DBG_ADDR(OBJECTREF_TO_UNCHECKED_OBJECTREF(*address)) ));
}


//-----------------------------------------------------------------------------
// a HelplerMethodFrames that also report additional object references
//-----------------------------------------------------------------------------

class HelperMethodFrame_1OBJ : public HelperMethodFrame
{
    VPTR_VTABLE_CLASS(HelperMethodFrame_1OBJ, HelperMethodFrame)

public:
#if !defined(DACCESS_COMPILE)
    HelperMethodFrame_1OBJ(void* fCallFtnEntry, unsigned attribs, OBJECTREF* aGCPtr1)
        : HelperMethodFrame(fCallFtnEntry, attribs)
    {
            LIMITED_METHOD_CONTRACT;
            gcPtrs[0] = aGCPtr1;
            INDEBUG(Thread::ObjectRefProtected(aGCPtr1);)
            INDEBUG((*aGCPtr1).Validate ();)
    }
#endif

    void SetProtectedObject(PTR_OBJECTREF objPtr)
    {
        LIMITED_METHOD_CONTRACT;
        gcPtrs[0] = objPtr;
        INDEBUG(Thread::ObjectRefProtected(objPtr);)
        }

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        DoPromote(fn, sc, gcPtrs[0], FALSE);
        HelperMethodFrame::GcScanRoots(fn, sc);
    }

#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    void Pop()
    {
        WRAPPER_NO_CONTRACT;
        HelperMethodFrame::Pop();
        Thread::ObjectRefNew(gcPtrs[0]);
    }
#endif // DACCESS_COMPILE

    BOOL Protects(OBJECTREF *ppORef)
    {
        LIMITED_METHOD_CONTRACT;
        return (ppORef == gcPtrs[0]) ? TRUE : FALSE;
    }

#endif

private:
    PTR_OBJECTREF gcPtrs[1];

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(HelperMethodFrame_1OBJ)
};


//-----------------------------------------------------------------------------
// HelperMethodFrame_2OBJ
//-----------------------------------------------------------------------------

class HelperMethodFrame_2OBJ : public HelperMethodFrame
{
    VPTR_VTABLE_CLASS(HelperMethodFrame_2OBJ, HelperMethodFrame)

public:
#if !defined(DACCESS_COMPILE)
    HelperMethodFrame_2OBJ(
            void* fCallFtnEntry,
            unsigned attribs,
            OBJECTREF* aGCPtr1,
            OBJECTREF* aGCPtr2)
        : HelperMethodFrame(fCallFtnEntry, attribs)
    {
            LIMITED_METHOD_CONTRACT;
        gcPtrs[0] = aGCPtr1;
        gcPtrs[1] = aGCPtr2;
        INDEBUG(Thread::ObjectRefProtected(aGCPtr1);)
        INDEBUG(Thread::ObjectRefProtected(aGCPtr2);)
        INDEBUG((*aGCPtr1).Validate ();)
        INDEBUG((*aGCPtr2).Validate ();)
    }
#endif

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        DoPromote(fn, sc, gcPtrs[0], FALSE);
        DoPromote(fn, sc, gcPtrs[1], FALSE);
        HelperMethodFrame::GcScanRoots(fn, sc);
    }

#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    void Pop()
    {
        WRAPPER_NO_CONTRACT;
        HelperMethodFrame::Pop();
        Thread::ObjectRefNew(gcPtrs[0]);
        Thread::ObjectRefNew(gcPtrs[1]);
    }
#endif // DACCESS_COMPILE

    BOOL Protects(OBJECTREF *ppORef)
    {
        LIMITED_METHOD_CONTRACT;
        return (ppORef == gcPtrs[0] || ppORef == gcPtrs[1]) ? TRUE : FALSE;
    }
#endif

private:
    PTR_OBJECTREF gcPtrs[2];

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(HelperMethodFrame_2OBJ)
};

//-----------------------------------------------------------------------------
// HelperMethodFrame_3OBJ
//-----------------------------------------------------------------------------

class HelperMethodFrame_3OBJ : public HelperMethodFrame
{
    VPTR_VTABLE_CLASS(HelperMethodFrame_3OBJ, HelperMethodFrame)

public:
#if !defined(DACCESS_COMPILE)
    HelperMethodFrame_3OBJ(
            void* fCallFtnEntry,
            unsigned attribs,
            OBJECTREF* aGCPtr1,
            OBJECTREF* aGCPtr2,
            OBJECTREF* aGCPtr3)
        : HelperMethodFrame(fCallFtnEntry, attribs)
    {
        LIMITED_METHOD_CONTRACT;
        gcPtrs[0] = aGCPtr1;
        gcPtrs[1] = aGCPtr2;
        gcPtrs[2] = aGCPtr3;
        INDEBUG(Thread::ObjectRefProtected(aGCPtr1);)
        INDEBUG(Thread::ObjectRefProtected(aGCPtr2);)
        INDEBUG(Thread::ObjectRefProtected(aGCPtr3);)
        INDEBUG((*aGCPtr1).Validate();)
        INDEBUG((*aGCPtr2).Validate();)
        INDEBUG((*aGCPtr3).Validate();)
    }
#endif

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        DoPromote(fn, sc, gcPtrs[0], FALSE);
        DoPromote(fn, sc, gcPtrs[1], FALSE);
        DoPromote(fn, sc, gcPtrs[2], FALSE);
        HelperMethodFrame::GcScanRoots(fn, sc);
    }

#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    void Pop()
    {
        WRAPPER_NO_CONTRACT;
        HelperMethodFrame::Pop();
        Thread::ObjectRefNew(gcPtrs[0]);
        Thread::ObjectRefNew(gcPtrs[1]);
        Thread::ObjectRefNew(gcPtrs[2]);
    }
#endif // DACCESS_COMPILE

    BOOL Protects(OBJECTREF *ppORef)
    {
        LIMITED_METHOD_CONTRACT;
        return (ppORef == gcPtrs[0] || ppORef == gcPtrs[1] || ppORef == gcPtrs[2]) ? TRUE : FALSE;
    }
#endif

private:
    PTR_OBJECTREF gcPtrs[3];

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(HelperMethodFrame_3OBJ)
};


//-----------------------------------------------------------------------------
// HelperMethodFrame_PROTECTOBJ
//-----------------------------------------------------------------------------

class HelperMethodFrame_PROTECTOBJ : public HelperMethodFrame
{
    VPTR_VTABLE_CLASS(HelperMethodFrame_PROTECTOBJ, HelperMethodFrame)

public:
#if !defined(DACCESS_COMPILE)
    HelperMethodFrame_PROTECTOBJ(void* fCallFtnEntry, unsigned attribs, OBJECTREF* pObjRefs, int numObjRefs)
        : HelperMethodFrame(fCallFtnEntry, attribs)
    {
        LIMITED_METHOD_CONTRACT;
        m_pObjRefs = pObjRefs;
        m_numObjRefs = numObjRefs;
#ifdef _DEBUG
        for (UINT i = 0; i < m_numObjRefs; i++) {
            Thread::ObjectRefProtected(&m_pObjRefs[i]);
            m_pObjRefs[i].Validate();
        }
#endif
    }
#endif

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        for (UINT i = 0; i < m_numObjRefs; i++) {
            DoPromote(fn, sc, &m_pObjRefs[i], FALSE);
        }
        HelperMethodFrame::GcScanRoots(fn, sc);
    }

#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    void Pop()
    {
        WRAPPER_NO_CONTRACT;
        HelperMethodFrame::Pop();
        for (UINT i = 0; i < m_numObjRefs; i++) {
            Thread::ObjectRefNew(&m_pObjRefs[i]);
        }
    }
#endif // DACCESS_COMPILE

    BOOL Protects(OBJECTREF *ppORef)
    {
        LIMITED_METHOD_CONTRACT;
        for (UINT i = 0; i < m_numObjRefs; i++) {
            if (ppORef == &m_pObjRefs[i])
                return TRUE;
        }
        return FALSE;
    }
#endif

private:
    PTR_OBJECTREF m_pObjRefs;
    UINT       m_numObjRefs;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(HelperMethodFrame_PROTECTOBJ)
};

class FramedMethodFrame : public TransitionFrame
{
    VPTR_ABSTRACT_VTABLE_CLASS(FramedMethodFrame, TransitionFrame)

    TADDR m_pTransitionBlock;

protected:
    PTR_MethodDesc m_pMD;

public:
#ifndef DACCESS_COMPILE
    FramedMethodFrame(TransitionBlock * pTransitionBlock, MethodDesc * pMD)
        : m_pTransitionBlock(dac_cast<TADDR>(pTransitionBlock)), m_pMD(pMD)
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif // DACCESS_COMPILE

    virtual TADDR GetTransitionBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pTransitionBlock;
    }

    virtual MethodDesc *GetFunction()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pMD;
    }

#ifndef DACCESS_COMPILE
    void SetFunction(MethodDesc *pMD)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE; // Frame MethodDesc should be always updated in cooperative mode to avoid racing with GC stackwalk
        }
        CONTRACTL_END;

        m_pMD = pMD;
    }
#endif

    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_M2U; // we can safely cast to a FramedMethodFrame
    }

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_CALL;
    }

#ifdef COM_STUBS_SEPARATE_FP_LOCATIONS
    static int GetFPArgOffset(int iArg)
    {
#ifdef TARGET_AMD64
        // Floating point spill area is between return value and transition block for frames that need it
        // (code:TPMethodFrame and code:ComPlusMethodFrame)
        return -(4 * 0x10 /* floating point args */ + 0x8 /* alignment pad */ + TransitionBlock::GetNegSpaceSize()) + (iArg * 0x10);
#endif
    }
#endif

    //
    // GetReturnObjectPtr and GetReturnValuePtr are only valid on frames
    // that allocate
    //
    PTR_PTR_Object GetReturnObjectPtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_PTR_Object(GetReturnValuePtr());
    }

    // Get return value address
    PTR_VOID GetReturnValuePtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef COM_STUBS_SEPARATE_FP_LOCATIONS
        TADDR p = GetTransitionBlock() + GetFPArgOffset(0);
#else
        TADDR p = GetTransitionBlock() - TransitionBlock::GetNegSpaceSize();
#endif
        // Return value is right before the transition block (or floating point spill area on AMD64) for frames that need it
        // (code:TPMethodFrame and code:ComPlusMethodFrame)
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
        p -= ENREGISTERED_RETURNTYPE_MAXSIZE;
#else
        p -= sizeof(ARG_SLOT);
#endif
        return dac_cast<PTR_VOID>(p);
    }

protected:
    FramedMethodFrame()
    {
        LIMITED_METHOD_CONTRACT;
    }
};

//------------------------------------------------------------------------
// This represents a call Multicast.Invoke. It's only used to gc-protect
// the arguments during the iteration.
//------------------------------------------------------------------------

class MulticastFrame : public TransitionFrame
{
    VPTR_VTABLE_CLASS(MulticastFrame, TransitionFrame)

    PTR_MethodDesc m_pMD;
    TransitionBlock m_TransitionBlock;

public:

    virtual MethodDesc* GetFunction()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMD;
    }

    virtual TADDR GetTransitionBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_HOST_MEMBER_TADDR(MulticastFrame, this,
                                     m_TransitionBlock);
    }

    static int GetOffsetOfTransitionBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return offsetof(MulticastFrame, m_TransitionBlock);
    }

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        TransitionFrame::GcScanRoots(fn, sc);
        PromoteCallerStack(fn, sc);
    }

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_MULTICAST;
    }

    // For the debugger:
    // Our base class, FramedMethodFrame, is a M2U transition;
    // but Delegate.Invoke isn't. So override and fix it here.
    // If we didn't do this, we'd see a Managed/Unmanaged transition in debugger's stack trace.
    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_NONE;
    }

    virtual BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                            TraceDestination *trace, REGDISPLAY *regs);

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(MulticastFrame)
};


#ifdef FEATURE_COMINTEROP

//-----------------------------------------------------------------------
// Transition frame from unmanaged to managed
//-----------------------------------------------------------------------

class UnmanagedToManagedFrame : public Frame
{
    friend class CheckAsmOffsets;

    VPTR_ABSTRACT_VTABLE_CLASS_AND_CTOR(UnmanagedToManagedFrame, Frame)

public:

    // DACCESS: GetReturnAddressPtr should return the
    // target address of the return address in the frame.
    virtual TADDR GetReturnAddressPtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_HOST_MEMBER_TADDR(UnmanagedToManagedFrame, this,
                                     m_ReturnAddress);
    }

    virtual PCODE GetReturnAddress();

    // Retrieves pointer to the lowest-addressed argument on
    // the stack. Depending on the calling convention, this
    // may or may not be the first argument.
    TADDR GetPointerToArguments()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<TADDR>(this) + GetOffsetOfArgs();
    }

    // Exposes an offset for stub generation.
    static BYTE GetOffsetOfArgs()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
        size_t ofs = offsetof(UnmanagedToManagedFrame, m_argumentRegisters);
#else
        size_t ofs = sizeof(UnmanagedToManagedFrame);
#endif
        _ASSERTE(FitsInI1(ofs));
        return (BYTE)ofs;
    }

    // depends on the sub frames to return approp. type here
    TADDR GetDatum()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pvDatum;
    }

    static int GetOffsetOfDatum()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(UnmanagedToManagedFrame, m_pvDatum);
    }

#ifdef TARGET_X86
    static int GetOffsetOfCalleeSavedRegisters()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(UnmanagedToManagedFrame, m_calleeSavedRegisters);
    }
#endif

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_ENTRY;
    }

    //------------------------------------------------------------------------
    // For the debugger.
    //------------------------------------------------------------------------
    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_U2M;
    }

    //------------------------------------------------------------------------
    // Performs cleanup on an exception unwind
    //------------------------------------------------------------------------
#ifndef DACCESS_COMPILE
    virtual void ExceptionUnwind();
#endif

protected:
    TADDR           m_pvDatum;        // type depends on the sub class

#if defined(TARGET_X86)
    CalleeSavedRegisters  m_calleeSavedRegisters;
    TADDR           m_ReturnAddress;
#elif defined(TARGET_ARM)
    TADDR           m_R11; // R11 chain
    TADDR           m_ReturnAddress;
    ArgumentRegisters m_argumentRegisters;
#elif defined (TARGET_ARM64)
    TADDR           m_fp;
    TADDR           m_ReturnAddress;
    TADDR           m_x8; // ret buff arg
    ArgumentRegisters m_argumentRegisters;
#elif defined (TARGET_LOONGARCH64) || defined (TARGET_RISCV64)
    TADDR           m_fp;
    TADDR           m_ReturnAddress;
    ArgumentRegisters m_argumentRegisters;
#else
    TADDR           m_ReturnAddress;  // return address into unmanaged code
#endif
};

//------------------------------------------------------------------------
// This frame represents a transition from COM to COM+
//------------------------------------------------------------------------

class ComMethodFrame : public UnmanagedToManagedFrame
{
    VPTR_VTABLE_CLASS(ComMethodFrame, UnmanagedToManagedFrame)
    VPTR_UNIQUE(VPTR_UNIQUE_ComMethodFrame)

public:

#ifdef TARGET_X86
    // Return the # of stack bytes pushed by the unmanaged caller.
    UINT GetNumCallerStackBytes();
#endif

    PTR_ComCallMethodDesc GetComCallMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_ComCallMethodDesc>(m_pvDatum);
    }

#ifndef DACCESS_COMPILE
    static void DoSecondPassHandlerCleanup(Frame * pCurFrame);
#endif // !DACCESS_COMPILE

protected:
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(ComMethodFrame)
};

typedef DPTR(class ComMethodFrame) PTR_ComMethodFrame;

//------------------------------------------------------------------------
// This represents a generic call from CLR to COM
//------------------------------------------------------------------------

class ComPlusMethodFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(ComPlusMethodFrame, FramedMethodFrame)

public:
    ComPlusMethodFrame(TransitionBlock * pTransitionBlock, MethodDesc * pMethodDesc);

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

    virtual BOOL IsTransitionToNativeFrame()
    {
        LIMITED_METHOD_CONTRACT;
        return TRUE;
    }

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_EXIT;
    }

    void GetUnmanagedCallSite(TADDR* ip,
                              TADDR* returnIP,
                              TADDR* returnSP);

    BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                    TraceDestination *trace, REGDISPLAY *regs);

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(ComPlusMethodFrame)
};

#endif // FEATURE_COMINTEROP

//------------------------------------------------------------------------
// This represents a call from a helper to GetILStubForCalli
//------------------------------------------------------------------------

class PInvokeCalliFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(PInvokeCalliFrame, FramedMethodFrame)

    PTR_VASigCookie m_pVASigCookie;
    PCODE m_pUnmanagedTarget;

public:
    PInvokeCalliFrame(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget);

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        FramedMethodFrame::GcScanRoots(fn, sc);
        PromoteCallerStack(fn, sc);
    }

    void PromoteCallerStack(promote_func* fn, ScanContext* sc);

    // not a method
    virtual MethodDesc *GetFunction()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return NULL;
    }

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_INTERCEPTION;
    }

    PCODE GetPInvokeCalliTarget()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pUnmanagedTarget;
    }

    PTR_VASigCookie GetVASigCookie()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pVASigCookie;
    }

#ifdef TARGET_X86
    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false);
#endif // TARGET_X86

    BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                    TraceDestination *trace, REGDISPLAY *regs)
    {
        WRAPPER_NO_CONTRACT;

        trace->InitForUnmanaged(GetPInvokeCalliTarget());
        return TRUE;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(PInvokeCalliFrame)
};

// Some context-related forwards.
#ifdef FEATURE_HIJACK
//------------------------------------------------------------------------
// This frame represents a hijacked return.  If we crawl back through it,
// it gets us back to where the return should have gone (and eventually will
// go).
//------------------------------------------------------------------------
class HijackFrame : public Frame
{
    VPTR_VTABLE_CLASS(HijackFrame, Frame)
    VPTR_UNIQUE(VPTR_UNIQUE_HijackFrame);

public:
    // DACCESS: GetReturnAddressPtr should return the
    // target address of the return address in the frame.
    virtual TADDR GetReturnAddressPtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_HOST_MEMBER_TADDR(HijackFrame, this,
                                     m_ReturnAddress);
    }

    virtual BOOL NeedsUpdateRegDisplay()
    {
        LIMITED_METHOD_CONTRACT;
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false);
    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

    // HijackFrames are created by trip functions. See OnHijackTripThread()
    // They are real C++ objects on the stack.
    // So, it's a public function -- but that doesn't mean you should make some.
    HijackFrame(LPVOID returnAddress, Thread *thread, HijackArgs *args);

protected:

    TADDR               m_ReturnAddress;
    PTR_Thread          m_Thread;
    DPTR(HijackArgs)    m_Args;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(HijackFrame)
};

#endif // FEATURE_HIJACK

//------------------------------------------------------------------------
// This represents a call to a method prestub. Because the prestub
// can do gc and throw exceptions while building the replacement
// stub, we need this frame to keep things straight.
//------------------------------------------------------------------------

class PrestubMethodFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(PrestubMethodFrame, FramedMethodFrame)

public:
    PrestubMethodFrame(TransitionBlock * pTransitionBlock, MethodDesc * pMD);

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        FramedMethodFrame::GcScanRoots(fn, sc);
        PromoteCallerStack(fn, sc);
    }

    BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                    TraceDestination *trace, REGDISPLAY *regs);

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_INTERCEPTION;
    }

    // Our base class is an M2U TransitionType; but we're not. So override and set us back to None.
    ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_NONE;
    }

    Interception GetInterception();

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(PrestubMethodFrame)
};

//------------------------------------------------------------------------
// This represents a call into the virtual call stub manager
// Because the stub manager can do gc and throw exceptions while
// building the resolve and dispatch stubs and needs to communicate
// if we need to setup for a methodDesc call or do a direct call
// we need this frame to keep things straight.
//------------------------------------------------------------------------

class StubDispatchFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(StubDispatchFrame, FramedMethodFrame)

    // Representative MethodTable * and slot. They are used to
    // compute the MethodDesc* lazily
    PTR_MethodTable m_pRepresentativeMT;
    UINT32          m_representativeSlot;

    // Indirection cell and containing module. Used to compute pGCRefMap lazily.
    PTR_Module      m_pZapModule;
    TADDR           m_pIndirection;

    // Cached pointer to native ref data.
    PTR_BYTE        m_pGCRefMap;

public:
    StubDispatchFrame(TransitionBlock * pTransitionBlock);

    MethodDesc* GetFunction();

    // Returns this frame GC ref map if it has one
    PTR_BYTE GetGCRefMap();

#ifdef TARGET_X86
    virtual void UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats = false);
    virtual PCODE GetReturnAddress();
#endif // TARGET_X86

    PCODE GetUnadjustedReturnAddress()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return FramedMethodFrame::GetReturnAddress();
    }

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

#ifndef DACCESS_COMPILE
    void SetRepresentativeSlot(MethodTable * pMT, UINT32 representativeSlot)
    {
        LIMITED_METHOD_CONTRACT;

        m_pRepresentativeMT = pMT;
        m_representativeSlot = representativeSlot;
    }

    void SetCallSite(Module * pZapModule, TADDR pIndirection)
    {
        LIMITED_METHOD_CONTRACT;

        m_pZapModule = pZapModule;
        m_pIndirection = pIndirection;
    }

    void SetForNullReferenceException()
    {
        LIMITED_METHOD_CONTRACT;

        // Nothing to do. Everything is initialized in Init.
    }
#endif

    BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                    TraceDestination *trace, REGDISPLAY *regs);

    int GetFrameType()
    {
        LIMITED_METHOD_CONTRACT;
        return TYPE_CALL;
    }

    Interception GetInterception();

    virtual BOOL SuppressParamTypeArg()
    {
        //
        // Shared default interface methods (i.e. virtual interface methods with an implementation) require
        // an instantiation argument. But if we're in the stub dispatch frame, we haven't actually resolved
        // the method yet (we could end up in class's override of this method, for example).
        //
        // So we need to pretent that unresolved default interface methods are like any other interface
        // methods and don't have an instantiation argument.
        //
        // See code:getMethodSigInternal
        //
        assert(GetFunction()->GetMethodTable()->IsInterface());
        return TRUE;
    }

private:
    friend class VirtualCallStubManager;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(StubDispatchFrame)
};

typedef VPTR(class StubDispatchFrame) PTR_StubDispatchFrame;

class CallCountingHelperFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(CallCountingHelperFrame, FramedMethodFrame);

public:
    CallCountingHelperFrame(TransitionBlock *pTransitionBlock, MethodDesc *pMD);

    virtual void GcScanRoots(promote_func *fn, ScanContext *sc); // override
    virtual BOOL TraceFrame(Thread *thread, BOOL fromPatch, TraceDestination *trace, REGDISPLAY *regs); // override

    virtual int GetFrameType() // override
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_CALL;
    }

    virtual Interception GetInterception() // override
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return INTERCEPTION_NONE;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(CallCountingHelperFrame)
};

//------------------------------------------------------------------------
// This represents a call from an ExternalMethodThunk or a VirtualImportThunk
// Because the resolving of the target address can do gc and/or
//  throw exceptions we need this frame to report the gc references.
//------------------------------------------------------------------------

class ExternalMethodFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(ExternalMethodFrame, FramedMethodFrame)

    // Indirection and containing module. Used to compute pGCRefMap lazily.
    PTR_Module      m_pZapModule;
    TADDR           m_pIndirection;

    // Cached pointer to native ref data.
    PTR_BYTE        m_pGCRefMap;

public:
    ExternalMethodFrame(TransitionBlock * pTransitionBlock);

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

    // Returns this frame GC ref map if it has one
    PTR_BYTE GetGCRefMap();

#ifndef DACCESS_COMPILE
    void SetCallSite(Module * pZapModule, TADDR pIndirection)
    {
        LIMITED_METHOD_CONTRACT;

        m_pZapModule = pZapModule;
        m_pIndirection = pIndirection;
    }
#endif

    int GetFrameType()
    {
        LIMITED_METHOD_CONTRACT;
        return TYPE_CALL;
    }

    Interception GetInterception();

#ifdef TARGET_X86
    virtual void UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats = false);
#endif

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(ExternalMethodFrame)
};

typedef VPTR(class ExternalMethodFrame) PTR_ExternalMethodFrame;

#ifdef FEATURE_READYTORUN
class DynamicHelperFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(DynamicHelperFrame, FramedMethodFrame)

    int m_dynamicHelperFrameFlags;

public:
    DynamicHelperFrame(TransitionBlock * pTransitionBlock, int dynamicHelperFrameFlags);

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

#ifdef TARGET_X86
    virtual void UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats = false);
#endif

    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_InternalCall;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(DynamicHelperFrame)
};

typedef VPTR(class DynamicHelperFrame) PTR_DynamicHelperFrame;
#endif // FEATURE_READYTORUN

#ifdef FEATURE_COMINTEROP

//------------------------------------------------------------------------
// This represents a com to com+ call method prestub.
// we need to catch exceptions etc. so this frame is not the same
// as the prestub method frame
// Note that in rare IJW cases, the immediate caller could be a managed method
// which pinvoke-inlined a call to a COM interface, which happenned to be
// implemented by a managed function via COM-interop.
//------------------------------------------------------------------------
class ComPrestubMethodFrame : public ComMethodFrame
{
    friend class CheckAsmOffsets;

    VPTR_VTABLE_CLASS(ComPrestubMethodFrame, ComMethodFrame)

public:
    // Set the vptr and GSCookie
    VOID Init();

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_INTERCEPTION;
    }

    // ComPrestubMethodFrame should return the same interception type as
    // code:PrestubMethodFrame.GetInterception.
    virtual Interception GetInterception()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return INTERCEPTION_PRESTUB;
    }

    // Our base class is an M2U TransitionType; but we're not. So override and set us back to None.
    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_NONE;
    }

    virtual void ExceptionUnwind()
    {
    }

private:
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(ComPrestubMethodFrame)
};

#endif // FEATURE_COMINTEROP

//------------------------------------------------------------------------
// This frame protects object references for the EE's convenience.
// This frame type actually is created from C++.
// There is a chain of GCFrames on a Thread, separate from the
// explicit frames derived from the Frame class.
//------------------------------------------------------------------------
class GCFrame
{
public:

#ifndef DACCESS_COMPILE
    //--------------------------------------------------------------------
    // This constructor pushes a new GCFrame on the GC frame chain.
    //--------------------------------------------------------------------
    GCFrame(OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior)
        : GCFrame(GetThread(), pObjRefs, numObjRefs, maybeInterior)
    {
        WRAPPER_NO_CONTRACT;
    }

    GCFrame(Thread *pThread, OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior);
    ~GCFrame();

    // Push and pop this frame from the thread's stack.
    void Push(Thread* pThread);
    void Pop();
    // Remove this frame from any position in the thread's stack
    void Remove();

#endif // DACCESS_COMPILE

    void GcScanRoots(promote_func *fn, ScanContext* sc);

#ifdef _DEBUG
    BOOL Protects(OBJECTREF *ppORef)
    {
        LIMITED_METHOD_CONTRACT;
        for (UINT i = 0; i < m_numObjRefs; i++) {
            if (ppORef == m_pObjRefs + i) {
                return TRUE;
            }
        }
        return FALSE;
    }
#endif

    PTR_GCFrame PtrNextFrame()
    {
        WRAPPER_NO_CONTRACT;
        return m_Next;
    }

private:
    PTR_GCFrame   m_Next;
    PTR_Thread    m_pCurThread;
    PTR_OBJECTREF m_pObjRefs;
    UINT          m_numObjRefs;
    BOOL          m_MaybeInterior;
};

#ifdef FEATURE_INTERPRETER
class InterpreterFrame: public Frame
{
    VPTR_VTABLE_CLASS(InterpreterFrame, Frame)

    class Interpreter* m_interp;

public:

#ifndef DACCESS_COMPILE
    InterpreterFrame(class Interpreter* interp);

    class Interpreter* GetInterpreter() { return m_interp; }

    // Override.
    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

    MethodDesc* GetFunction();
#endif

    DEFINE_VTABLE_GETTER_AND_DTOR(InterpreterFrame)

};

typedef VPTR(class InterpreterFrame) PTR_InterpreterFrame;
#endif // FEATURE_INTERPRETER


//-----------------------------------------------------------------------------

struct ByRefInfo;
typedef DPTR(ByRefInfo) PTR_ByRefInfo;

struct ByRefInfo
{
    PTR_ByRefInfo pNext;
    INT32      argIndex;
    CorElementType typ;
    TypeHandle typeHandle;
    char       data[1];
};

//-----------------------------------------------------------------------------
// ProtectByRefsFrame
//-----------------------------------------------------------------------------

class ProtectByRefsFrame : public Frame
{
    VPTR_VTABLE_CLASS(ProtectByRefsFrame, Frame)

public:
#ifndef DACCESS_COMPILE
    ProtectByRefsFrame(Thread *pThread, ByRefInfo *brInfo)
        : m_brInfo(brInfo)
    {
        WRAPPER_NO_CONTRACT;
        Frame::Push(pThread);
    }
#endif

    virtual void GcScanRoots(promote_func *fn, ScanContext *sc);

private:
    PTR_ByRefInfo m_brInfo;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(ProtectByRefsFrame)
};


//-----------------------------------------------------------------------------

struct ValueClassInfo;
typedef DPTR(struct ValueClassInfo) PTR_ValueClassInfo;

struct ValueClassInfo
{
    PTR_ValueClassInfo  pNext;
    PTR_MethodTable     pMT;
    PTR_VOID            pData;

    ValueClassInfo(PTR_VOID aData, PTR_MethodTable aMT, PTR_ValueClassInfo aNext)
        : pNext(aNext), pMT(aMT), pData(aData)
    {
    }
};

//-----------------------------------------------------------------------------
// ProtectValueClassFrame
//-----------------------------------------------------------------------------


class ProtectValueClassFrame : public Frame
{
    VPTR_VTABLE_CLASS(ProtectValueClassFrame, Frame)

public:
#ifndef DACCESS_COMPILE
    ProtectValueClassFrame()
        : m_pVCInfo(NULL)
    {
        WRAPPER_NO_CONTRACT;
        Frame::Push();
    }

    ProtectValueClassFrame(Thread *pThread, ValueClassInfo *vcInfo)
        : m_pVCInfo(vcInfo)
    {
        WRAPPER_NO_CONTRACT;
        Frame::Push(pThread);
    }
#endif

    virtual void GcScanRoots(promote_func *fn, ScanContext *sc);

    ValueClassInfo ** GetValueClassInfoList()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_pVCInfo;
    }

private:

    ValueClassInfo *m_pVCInfo;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(ProtectValueClassFrame)
};


#ifdef _DEBUG
BOOL IsProtectedByGCFrame(OBJECTREF *ppObjectRef);
#endif


//------------------------------------------------------------------------
// DebuggerClassInitMarkFrame is a small frame whose only purpose in
// life is to mark for the debugger that "class initialization code" is
// being run. It does nothing useful except return good values from
// GetFrameType and GetInterception.
//------------------------------------------------------------------------

class DebuggerClassInitMarkFrame : public Frame
{
    VPTR_VTABLE_CLASS(DebuggerClassInitMarkFrame, Frame)

public:

#ifndef DACCESS_COMPILE
    DebuggerClassInitMarkFrame()
    {
        WRAPPER_NO_CONTRACT;
        Push();
    };
#endif

    virtual int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_INTERCEPTION;
    }

    virtual Interception GetInterception()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return INTERCEPTION_CLASS_INIT;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(DebuggerClassInitMarkFrame)
};


//------------------------------------------------------------------------
// DebuggerSecurityCodeMarkFrame is a small frame whose only purpose in
// life is to mark for the debugger that "security code" is
// being run. It does nothing useful except return good values from
// GetFrameType and GetInterception.
//------------------------------------------------------------------------

class DebuggerSecurityCodeMarkFrame : public Frame
{
    VPTR_VTABLE_CLASS(DebuggerSecurityCodeMarkFrame, Frame)

public:
#ifndef DACCESS_COMPILE
    DebuggerSecurityCodeMarkFrame()
    {
        WRAPPER_NO_CONTRACT;
        Push();
    }
#endif

    virtual int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_INTERCEPTION;
    }

    virtual Interception GetInterception()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return INTERCEPTION_SECURITY;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(DebuggerSecurityCodeMarkFrame)
};

//------------------------------------------------------------------------
// DebuggerExitFrame is a small frame whose only purpose in
// life is to mark for the debugger that there is an exit transiton on
// the stack.  This is special cased for the "break" IL instruction since
// it is an fcall using a helper frame which returns TYPE_CALL instead of
// an ecall (as in System.Diagnostics.Debugger.Break()) which returns
// TYPE_EXIT.  This just makes the two consistent for debugging services.
//------------------------------------------------------------------------

class DebuggerExitFrame : public Frame
{
    VPTR_VTABLE_CLASS(DebuggerExitFrame, Frame)

public:
#ifndef DACCESS_COMPILE
    DebuggerExitFrame()
    {
        WRAPPER_NO_CONTRACT;
        Push();
    }
#endif

    virtual int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_EXIT;
    }

    // Return information about an unmanaged call the frame
    // will make.
    // ip - the unmanaged routine which will be called
    // returnIP - the address in the stub which the unmanaged routine
    //            will return to.
    // returnSP - the location returnIP is pushed onto the stack
    //            during the call.
    //
    virtual void GetUnmanagedCallSite(TADDR* ip,
                                      TADDR* returnIP,
                                      TADDR* returnSP)
    {
        LIMITED_METHOD_CONTRACT;
        if (ip)
            *ip = NULL;

        if (returnIP)
            *returnIP = NULL;

        if (returnSP)
            *returnSP = NULL;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(DebuggerExitFrame)
};

//---------------------------------------------------------------------------------------
//
// DebuggerU2MCatchHandlerFrame is a small frame whose only purpose in life is to mark for the debugger
// that there is catch handler inside the runtime which may catch and swallow managed exceptions.  The
// debugger needs this frame to send a CatchHandlerFound (CHF) notification.  Without this frame, the
// debugger doesn't know where a managed exception is caught.
//
// Notes:
//    Currently this frame is only used in code:DispatchInfo.InvokeMember, which is an U2M transition.
//

class DebuggerU2MCatchHandlerFrame : public Frame
{
    VPTR_VTABLE_CLASS(DebuggerU2MCatchHandlerFrame, Frame)

public:
#ifndef DACCESS_COMPILE
    DebuggerU2MCatchHandlerFrame()
    {
        WRAPPER_NO_CONTRACT;
        Frame::Push();
    }

    DebuggerU2MCatchHandlerFrame(Thread * pThread)
    {
        WRAPPER_NO_CONTRACT;
        Frame::Push(pThread);
    }
#endif

    ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_U2M;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(DebuggerU2MCatchHandlerFrame)
};

// Frame for the Reverse PInvoke (i.e. UnmanagedCallersOnlyAttribute).
struct ReversePInvokeFrame
{
    Thread* currentThread;
    MethodDesc* pMD;
#ifndef FEATURE_EH_FUNCLETS
    FrameHandlerExRecord record;
#endif
};

//------------------------------------------------------------------------
// This frame is pushed by any JIT'ted method that contains one or more
// inlined N/Direct calls. Note that the JIT'ted method keeps it pushed
// the whole time to amortize the pushing cost across the entire method.
//------------------------------------------------------------------------

typedef DPTR(class InlinedCallFrame) PTR_InlinedCallFrame;

class InlinedCallFrame : public Frame
{
    VPTR_VTABLE_CLASS(InlinedCallFrame, Frame)

public:
    virtual MethodDesc *GetFunction()
    {
        WRAPPER_NO_CONTRACT;
        if (FrameHasActiveCall(this) && HasFunction())
            // Mask off marker bits
            return PTR_MethodDesc((dac_cast<TADDR>(m_Datum) & ~(sizeof(TADDR) - 1)));
        else
            return NULL;
    }

    BOOL HasFunction()
    {
        WRAPPER_NO_CONTRACT;

#ifdef HOST_64BIT
        // See code:GenericPInvokeCalliHelper
        return ((m_Datum != NULL) && !(dac_cast<TADDR>(m_Datum) & 0x1));
#else // HOST_64BIT
        return ((dac_cast<TADDR>(m_Datum) & ~0xffff) != 0);
#endif // HOST_64BIT
    }

    // Retrieves the return address into the code that called out
    // to managed code
    virtual TADDR GetReturnAddressPtr()
    {
        WRAPPER_NO_CONTRACT;

        if (FrameHasActiveCall(this))
            return PTR_HOST_MEMBER_TADDR(InlinedCallFrame, this,
                                            m_pCallerReturnAddress);
        else
            return NULL;
    }

    virtual BOOL NeedsUpdateRegDisplay()
    {
        WRAPPER_NO_CONTRACT;
        return FrameHasActiveCall(this);
    }

    // Given a methodDesc representing an ILStub for a pinvoke call,
    // this method will return the MethodDesc for the actual interop
    // method if the current InlinedCallFrame is inactive.
    PTR_MethodDesc GetActualInteropMethodDesc()
    {
#if defined(TARGET_X86) || defined(TARGET_ARM)
        // Important: This code relies on the way JIT lays out frames. Keep it in sync
        // with code:Compiler.lvaAssignFrameOffsets.
        //
        // |        ...         |
        // +--------------------+
        // | lvaStubArgumentVar | <= filled with EAX in prolog          |
        // +--------------------+                                       |
        // |                    |                                       |
        // |  InlinedCallFrame  |                                       |
        // |                    | <= m_pCrawl.pFrame                    | to lower addresses
        // +--------------------+                                       V
        // |        ...         |
        //
        // Extract the actual MethodDesc to report from the InlinedCallFrame.
        TADDR addr = dac_cast<TADDR>(this) + sizeof(InlinedCallFrame);
        return PTR_MethodDesc(*PTR_TADDR(addr));
#elif defined(HOST_64BIT)
        // On 64bit, the actual interop MethodDesc is saved off in a field off the InlinedCrawlFrame
        // which is populated by the JIT. Refer to JIT_InitPInvokeFrame for details.
        return PTR_MethodDesc(m_StubSecretArg);
#else
        _ASSERTE(!"NYI - Interop method reporting for this architecture!");
        return NULL;
#endif // defined(TARGET_X86) || defined(TARGET_ARM)
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY, bool updateFloats = false);

    // m_Datum contains MethodDesc ptr or
    // - on 64 bit host: CALLI target address (if lowest bit is set)
    // - on windows x86 host: argument stack size (if value is <64k)
    // When m_Datum contains MethodDesc ptr, then on other than windows x86 host
    // - bit 1 set indicates invoking new exception handling helpers
    // - bit 2 indicates CallCatchFunclet or CallFinallyFunclet
    // See code:HasFunction.
    PTR_NDirectMethodDesc   m_Datum;

#ifdef HOST_64BIT
    // IL stubs fill this field with the incoming secret argument when they erect
    // InlinedCallFrame so we know which interop method was invoked even if the frame
    // is not active at the moment.
    PTR_VOID                m_StubSecretArg;
#endif // HOST_64BIT

    // X86: ESP after pushing the outgoing arguments, and just before calling
    // out to unmanaged code.
    // Other platforms: the field stays set throughout the declaring method.
    PTR_VOID             m_pCallSiteSP;

    // EIP where the unmanaged call will return to. This will be a pointer into
    // the code of the managed frame which has the InlinedCallFrame
    // This is set to NULL in the method prolog. It gets set just before the
    // call to the target and reset back to NULL after the stop-for-GC check
    // following the call.
    TADDR                m_pCallerReturnAddress;

    // This is used only for EBP. Hence, a stackwalk will miss the other
    // callee-saved registers for the method with the InlinedCallFrame.
    // To prevent GC-holes, we do not keep any GC references in callee-saved
    // registers across an NDirect call.
    TADDR                m_pCalleeSavedFP;

    // This field is used to cache the current thread object where this frame is
    // executing. This is especially helpful on Unix platforms for the PInvoke assembly
    // stubs, since there is no easy way to inline an implementation of GetThread.
    PTR_VOID             m_pThread;

#ifdef TARGET_ARM
    // Store the value of SP after prolog to ensure we can unwind functions that use
    // stackalloc. In these functions, the m_pCallSiteSP can already be augmented by
    // the stackalloc size, which is variable.
    TADDR               m_pSPAfterProlog;
#endif // TARGET_ARM

public:
    //---------------------------------------------------------------
    // Expose key offsets and values for stub generation.
    //---------------------------------------------------------------

    static void GetEEInfo(CORINFO_EE_INFO::InlinedCallFrameInfo * pEEInfo);

    // Is the specified frame an InlinedCallFrame which has an active call
    // inside it right now?
    static BOOL FrameHasActiveCall(Frame *pFrame)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return pFrame &&
            pFrame != FRAME_TOP &&
            InlinedCallFrame::GetMethodFrameVPtr() == pFrame->GetVTablePtr() &&
            dac_cast<TADDR>(dac_cast<PTR_InlinedCallFrame>(pFrame)->m_pCallerReturnAddress) != NULL;
    }

    // Marks the frame as inactive.
    void Reset()
    {
        m_pCallerReturnAddress = NULL;
    }

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_EXIT;
    }

    virtual BOOL IsTransitionToNativeFrame()
    {
        LIMITED_METHOD_CONTRACT;
        return TRUE;
    }

    PTR_VOID GetCallSiteSP()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCallSiteSP;
    }

    TADDR GetCalleeSavedFP()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pCalleeSavedFP;
    }

    // Set the vptr and GSCookie
    VOID Init();

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(InlinedCallFrame)
};

// TODO [DAVBR]: For the full fix for VsWhidbey 450273, this
// may be uncommented once isLegalManagedCodeCaller works properly
// with non-return address inputs, and with non-DEBUG builds
//bool isLegalManagedCodeCaller(TADDR retAddr);
bool isRetAddr(TADDR retAddr, TADDR* whereCalled);

#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
//------------------------------------------------------------------------
// This frame is used as padding for virtual stub dispatch tailcalls.
// When A calls B via virtual stub dispatch, the stub dispatch stub resolves
// the target code for B and jumps to it. If A wants to do a tail call,
// it does not get a chance to unwind its frame since the virtual stub dispatch
// stub is not set up to return the address of the target code (rather
// than just jumping to it).
// To do a tail call, A calls JIT_TailCall, which unwinds A's frame
// and sets up a TailCallFrame. It then calls the stub dispatch stub
// which disassembles the caller (JIT_TailCall, in this case) to get some information,
// resolves the target code for B, and then jumps to B.
// If B also does a virtual stub dispatch tail call, then we reuse the
// existing TailCallFrame instead of setting up a second one.
//
// We could eliminate TailCallFrame if we factor the VSD stub to return
// the target code address. This is currently not a very important scenario
// as tail calls on interface calls are uncommon.
//------------------------------------------------------------------------

class TailCallFrame : public Frame
{
    VPTR_VTABLE_CLASS(TailCallFrame, Frame)

    TADDR           m_CallerAddress;    // the address the tailcall was initiated from
    CalleeSavedRegisters    m_regs;     // callee saved registers - the stack walk assumes that all non-JIT frames have them
    TADDR           m_ReturnAddress;    // the return address of the tailcall

public:
    static TailCallFrame* FindTailCallFrame(Frame* pFrame)
    {
        LIMITED_METHOD_CONTRACT;
        // loop through the frame chain
        while (pFrame->GetVTablePtr() != TailCallFrame::GetMethodFrameVPtr())
            pFrame = pFrame->m_Next;
        return (TailCallFrame*)pFrame;
    }

    TADDR GetCallerAddress()
    {
        LIMITED_METHOD_CONTRACT;
        return m_CallerAddress;
    }

    virtual TADDR GetReturnAddressPtr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_HOST_MEMBER_TADDR(TailCallFrame, this,
                                        m_ReturnAddress);
    }

    virtual BOOL NeedsUpdateRegDisplay()
    {
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats = false);

private:
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(TailCallFrame)
};
#endif // TARGET_X86 && !UNIX_X86_ABI

//------------------------------------------------------------------------
// ExceptionFilterFrame is a small frame whose only purpose in
// life is to set SHADOW_SP_FILTER_DONE during unwind from exception filter.
//------------------------------------------------------------------------

class ExceptionFilterFrame : public Frame
{
    VPTR_VTABLE_CLASS(ExceptionFilterFrame, Frame)
    size_t* m_pShadowSP;

public:
#ifndef DACCESS_COMPILE
    ExceptionFilterFrame(size_t* pShadowSP)
    {
        WRAPPER_NO_CONTRACT;
        m_pShadowSP = pShadowSP;
        Push();
    }

    void Pop()
    {
        // Nothing to do here.
        WRAPPER_NO_CONTRACT;
        SetFilterDone();
        Frame::Pop();
    }

    void SetFilterDone()
    {
        LIMITED_METHOD_CONTRACT;

        // Mark the filter as having completed
        if (m_pShadowSP)
        {
            // Make sure that CallJitEHFilterHelper marked us as being in the filter.
            _ASSERTE(*m_pShadowSP & ICodeManager::SHADOW_SP_IN_FILTER);
            *m_pShadowSP |= ICodeManager::SHADOW_SP_FILTER_DONE;
        }
    }
#endif

private:
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR_AND_DTOR(ExceptionFilterFrame)
};

#ifdef _DEBUG
// We use IsProtectedByGCFrame to check if some OBJECTREF pointers are protected
// against GC. That function doesn't know if a byref is from managed stack thus
// protected by JIT. AssumeByrefFromJITStack is used to bypass that check if an
// OBJECTRef pointer is passed from managed code to an FCall and it's in stack.
class AssumeByrefFromJITStack : public Frame
{
    VPTR_VTABLE_CLASS(AssumeByrefFromJITStack, Frame)
public:
#ifndef DACCESS_COMPILE
    AssumeByrefFromJITStack(OBJECTREF *pObjRef)
    {
        m_pObjRef      = pObjRef;
    }
#endif

    BOOL Protects(OBJECTREF *ppORef)
    {
        LIMITED_METHOD_CONTRACT;
        return ppORef == m_pObjRef;
    }

private:
    OBJECTREF *m_pObjRef;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_DTOR(AssumeByrefFromJITStack)
}; //AssumeByrefFromJITStack

#endif //_DEBUG

//-----------------------------------------------------------------------------
// FrameWithCookie is used to declare a Frame in source code with a cookie
// immediately preceding it.
// This is just a specialized version of GSCookieFor<T>
//
// For Frames that are set up by stubs, the stub is responsible for setting up
// the GSCookie.
//
// Note that we have to play all these games for the GSCookie as the GSCookie
// needs to precede the vtable pointer, so that the GSCookie is guaranteed to
// catch any stack-buffer-overrun corruptions that overwrite the Frame data.
//
//-----------------------------------------------------------------------------

class DebuggerEval;

class GCSafeCollection;

template <typename FrameType>
class FrameWithCookie
{
protected:

    GSCookie        m_gsCookie;
    FrameType       m_frame;

public:

    //
    // Overload all the required constructors
    //

    FrameWithCookie() :
        m_gsCookie(GetProcessGSCookie()), m_frame() { WRAPPER_NO_CONTRACT; }

    FrameWithCookie(Thread * pThread) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pThread) { WRAPPER_NO_CONTRACT; }

    FrameWithCookie(T_CONTEXT * pContext) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pContext) { WRAPPER_NO_CONTRACT; }

    FrameWithCookie(TransitionBlock * pTransitionBlock) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pTransitionBlock) { WRAPPER_NO_CONTRACT; }

    FrameWithCookie(TransitionBlock * pTransitionBlock, MethodDesc * pMD) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pTransitionBlock, pMD) { WRAPPER_NO_CONTRACT; }

    FrameWithCookie(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pTransitionBlock, pVASigCookie, pUnmanagedTarget) { WRAPPER_NO_CONTRACT; }

    FrameWithCookie(TransitionBlock * pTransitionBlock, int frameFlags) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pTransitionBlock, frameFlags) { WRAPPER_NO_CONTRACT; }


    // GCFrame
    FrameWithCookie(Thread * pThread, OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pThread, pObjRefs, numObjRefs, maybeInterior) { WRAPPER_NO_CONTRACT; }

    FrameWithCookie(OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pObjRefs, numObjRefs, maybeInterior) { WRAPPER_NO_CONTRACT; }

    // GCSafeCollectionFrame
    FrameWithCookie(GCSafeCollection *gcSafeCollection) :
        m_gsCookie(GetProcessGSCookie()), m_frame(gcSafeCollection) { WRAPPER_NO_CONTRACT; }

#ifdef FEATURE_INTERPRETER
    // InterpreterFrame
    FrameWithCookie(Interpreter* interp) :
        m_gsCookie(GetProcessGSCookie()), m_frame(interp) { WRAPPER_NO_CONTRACT; }
#endif

    // HijackFrame
    FrameWithCookie(LPVOID returnAddress, Thread *thread, HijackArgs *args) :
        m_gsCookie(GetProcessGSCookie()), m_frame(returnAddress, thread, args) { WRAPPER_NO_CONTRACT; }

#ifdef DEBUGGING_SUPPORTED
    // FuncEvalFrame
    FrameWithCookie(DebuggerEval *pDebuggerEval, TADDR returnAddress, BOOL showFrame) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pDebuggerEval, returnAddress, showFrame) { WRAPPER_NO_CONTRACT; }
#endif // DEBUGGING_SUPPORTED

#ifndef DACCESS_COMPILE
    // GSCookie for HelperMethodFrames is initialized in a common HelperMethodFrame init method

    // HelperMethodFrame
    FORCEINLINE FrameWithCookie(void* fCallFtnEntry, unsigned attribs = 0) :
        m_frame(fCallFtnEntry, attribs) { WRAPPER_NO_CONTRACT; }

    // HelperMethodFrame_1OBJ
    FORCEINLINE FrameWithCookie(void* fCallFtnEntry, unsigned attribs, OBJECTREF * aGCPtr1) :
        m_frame(fCallFtnEntry, attribs, aGCPtr1) { WRAPPER_NO_CONTRACT; }

    // HelperMethodFrame_2OBJ
    FORCEINLINE FrameWithCookie(void* fCallFtnEntry, unsigned attribs, OBJECTREF * aGCPtr1, OBJECTREF * aGCPtr2) :
        m_frame(fCallFtnEntry, attribs, aGCPtr1, aGCPtr2) { WRAPPER_NO_CONTRACT; }

    // HelperMethodFrame_3OBJ
    FORCEINLINE FrameWithCookie(void* fCallFtnEntry, unsigned attribs, OBJECTREF * aGCPtr1, OBJECTREF * aGCPtr2, OBJECTREF * aGCPtr3) :
        m_frame(fCallFtnEntry, attribs, aGCPtr1, aGCPtr2, aGCPtr3) { WRAPPER_NO_CONTRACT; }

    // HelperMethodFrame_PROTECTOBJ
    FORCEINLINE FrameWithCookie(void* fCallFtnEntry, unsigned attribs, OBJECTREF* pObjRefs, int numObjRefs) :
        m_frame(fCallFtnEntry, attribs, pObjRefs, numObjRefs) { WRAPPER_NO_CONTRACT; }

#endif // DACCESS_COMPILE

    // ProtectByRefsFrame
    FrameWithCookie(Thread * pThread, ByRefInfo * pByRefs) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pThread, pByRefs) { WRAPPER_NO_CONTRACT; }

    // ProtectValueClassFrame
    FrameWithCookie(Thread * pThread, ValueClassInfo * pValueClasses) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pThread, pValueClasses) { WRAPPER_NO_CONTRACT; }

    // ExceptionFilterFrame
    FrameWithCookie(size_t* pShadowSP) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pShadowSP) { WRAPPER_NO_CONTRACT; }

#ifdef _DEBUG
    // AssumeByrefFromJITStack
    FrameWithCookie(OBJECTREF *pObjRef) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pObjRef) { WRAPPER_NO_CONTRACT; }

    void SetAddrOfHaveCheckedRestoreState(BOOL* pDoneCheck)
    {
        WRAPPER_NO_CONTRACT;
        m_frame.SetAddrOfHaveCheckedRestoreState(pDoneCheck);
    }

#endif //_DEBUG

    //
    // Overload some common Frame methods for easy redirection
    //

    void Push() { WRAPPER_NO_CONTRACT; m_frame.Push(); }
    void Pop() { WRAPPER_NO_CONTRACT; m_frame.Pop(); }
    void Push(Thread * pThread) { WRAPPER_NO_CONTRACT; m_frame.Push(pThread); }
    void Pop(Thread * pThread) { WRAPPER_NO_CONTRACT; m_frame.Pop(pThread); }
    PCODE GetReturnAddress() { WRAPPER_NO_CONTRACT; return m_frame.GetReturnAddress(); }
    T_CONTEXT * GetContext() { WRAPPER_NO_CONTRACT; return m_frame.GetContext(); }
    FrameType* operator&() { LIMITED_METHOD_CONTRACT; return &m_frame; }
    LazyMachState * MachineState() { WRAPPER_NO_CONTRACT; return m_frame.MachineState(); }
    Thread * GetThread() { WRAPPER_NO_CONTRACT; return m_frame.GetThread(); }
    BOOL InsureInit(bool initialInit, struct MachState* unwindState)
        { WRAPPER_NO_CONTRACT; return m_frame.InsureInit(initialInit, unwindState); }
    void Poll() { WRAPPER_NO_CONTRACT; m_frame.Poll(); }
    void SetStackPointerPtr(TADDR sp) { WRAPPER_NO_CONTRACT; m_frame.SetStackPointerPtr(sp); }
    void InitAndLink(T_CONTEXT *pContext) { WRAPPER_NO_CONTRACT; m_frame.InitAndLink(pContext); }
    void Init(Thread *pThread, OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior)
        { WRAPPER_NO_CONTRACT; m_frame.Init(pThread, pObjRefs, numObjRefs, maybeInterior); }
    ValueClassInfo ** GetValueClassInfoList() { WRAPPER_NO_CONTRACT; return m_frame.GetValueClassInfoList(); }

#if 0
    //
    // Access to the underlying Frame
    // You should only need to use this if none of the above overloads work for you
    // Consider adding the required overload to the list above
    //

    FrameType& operator->() { LIMITED_METHOD_CONTRACT; return m_frame; }
#endif

    // Since the "&" operator is overloaded, use this function to get to the
    // address of FrameWithCookie, rather than that of FrameWithCookie::m_frame.
    GSCookie * GetGSCookiePtr() { LIMITED_METHOD_CONTRACT; return &m_gsCookie; }
};

//------------------------------------------------------------------------
// These macros GC-protect OBJECTREF pointers on the EE's behalf.
// In between these macros, the GC can move but not discard the protected
// objects. If the GC moves an object, it will update the guarded OBJECTREF's.
// Typical usage:
//
//   OBJECTREF or = <some valid objectref>;
//   GCPROTECT_BEGIN(or);
//
//   ...<do work that can trigger GC>...
//
//   GCPROTECT_END();
//
//
// These macros can also protect multiple OBJECTREF's if they're packaged
// into a structure:
//
//   struct xx {
//      OBJECTREF o1;
//      OBJECTREF o2;
//   } gc;
//
//   GCPROTECT_BEGIN(gc);
//   ....
//   GCPROTECT_END();
//
//
// Notes:
//
//   - GCPROTECT_BEGININTERIOR() can be used in place of GCPROTECT_BEGIN()
//     to handle the case where one or more of the OBJECTREFs is potentially
//     an interior pointer.  This is a rare situation, because boxing would
//     normally prevent us from encountering it.  Be aware that the OBJECTREFs
//     we protect are not validated in this situation.
//
//   - GCPROTECT_ARRAY_BEGIN() can be used when an array of object references
//     is allocated on the stack.  The pointer to the first element is passed
//     along with the number of elements in the array.
//
//   - The argument to GCPROTECT_BEGIN should be an lvalue because it
//     uses "sizeof" to count the OBJECTREF's.
//
//   - GCPROTECT_BEGIN spiritually violates our normal convention of not passing
//     non-const reference arguments. Unfortunately, this is necessary in
//     order for the sizeof thing to work.
//
//   - GCPROTECT_BEGIN does _not_ zero out the OBJECTREF's. You must have
//     valid OBJECTREF's when you invoke this macro.
//
//   - GCPROTECT_BEGIN begins a new C nesting block. Besides allowing
//     GCPROTECT_BEGIN's to nest, it also has the advantage of causing
//     a compiler error if you forget to code a maching GCPROTECT_END.
//
//   - If you are GCPROTECTing something, it means you are expecting a GC to occur.
//     So we assert that GC is not forbidden. If you hit this assert, you probably need
//     a HELPER_METHOD_FRAME to protect the region that can cause the GC.
//------------------------------------------------------------------------

#ifndef DACCESS_COMPILE

#ifdef _PREFAST_
// Suppress prefast warning #6384: Dividing sizeof a pointer by another value
#pragma warning(disable:6384)
#endif /*_PREFAST_ */

#define GCPROTECT_BEGIN(ObjRefStruct)                           do {    \
                GCFrame __gcframe(                                      \
                        (OBJECTREF*)&(ObjRefStruct),                    \
                        sizeof(ObjRefStruct)/sizeof(OBJECTREF),         \
                        FALSE);                                         \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_BEGIN_THREAD(pThread, ObjRefStruct)           do {    \
                GCFrame __gcframe(                                      \
                        pThread,                                        \
                        (OBJECTREF*)&(ObjRefStruct),                    \
                        sizeof(ObjRefStruct)/sizeof(OBJECTREF),         \
                        FALSE);                                         \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_ARRAY_BEGIN(ObjRefArray,cnt) do {                     \
                GCFrame __gcframe(                                      \
                        (OBJECTREF*)&(ObjRefArray),                     \
                        cnt * sizeof(ObjRefArray) / sizeof(OBJECTREF),  \
                        FALSE);                                         \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_BEGININTERIOR(ObjRefStruct)           do {            \
                /* work around Wsizeof-pointer-div warning as we */     \
                /* mean to capture pointer or object size */            \
                UINT subjectSize = sizeof(ObjRefStruct);                \
                GCFrame __gcframe(                                      \
                        (OBJECTREF*)&(ObjRefStruct),                    \
                        subjectSize/sizeof(OBJECTREF),                  \
                        TRUE);                                          \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_BEGININTERIOR_ARRAY(ObjRefArray,cnt) do {             \
                GCFrame __gcframe(                                      \
                        (OBJECTREF*)&(ObjRefArray),                     \
                        cnt,                                            \
                        TRUE);                                          \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)


#define GCPROTECT_END()                                                 \
                DEBUG_ASSURE_NO_RETURN_END(GCPROTECT) }                 \
                } while(0)


#else // #ifndef DACCESS_COMPILE

#define GCPROTECT_BEGIN(ObjRefStruct)
#define GCPROTECT_ARRAY_BEGIN(ObjRefArray,cnt)
#define GCPROTECT_BEGININTERIOR(ObjRefStruct)
#define GCPROTECT_END()

#endif // #ifndef DACCESS_COMPILE


#define ASSERT_ADDRESS_IN_STACK(address) _ASSERTE (Thread::IsAddressInCurrentStack (address));

#if defined (_DEBUG) && !defined (DACCESS_COMPILE)
#define ASSUME_BYREF_FROM_JIT_STACK_BEGIN(__objRef)                                      \
                /* make sure we are only called inside an FCall */                       \
                if (__me == 0) {};                                                       \
                /* make sure the address is in stack. If the address is an interior */   \
                /*pointer points to GC heap, the FCall still needs to protect it explicitly */             \
                ASSERT_ADDRESS_IN_STACK (__objRef);                                      \
                do {                                                                     \
                FrameWithCookie<AssumeByrefFromJITStack> __dummyAssumeByrefFromJITStack ((__objRef));       \
                __dummyAssumeByrefFromJITStack.Push ();                                  \
                /* work around unreachable code warning */                               \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GC_PROTECT)

#define ASSUME_BYREF_FROM_JIT_STACK_END()                                          \
                DEBUG_ASSURE_NO_RETURN_END(GC_PROTECT) }                                            \
                __dummyAssumeByrefFromJITStack.Pop(); } while(0)
#else //defined (_DEBUG) && !defined (DACCESS_COMPILE)
#define ASSUME_BYREF_FROM_JIT_STACK_BEGIN(__objRef)
#define ASSUME_BYREF_FROM_JIT_STACK_END()
#endif //defined (_DEBUG) && !defined (DACCESS_COMPILE)

void ComputeCallRefMap(MethodDesc* pMD,
                       GCRefMapBuilder * pBuilder,
                       bool isDispatchCell);

bool CheckGCRefMapEqual(PTR_BYTE pGCRefMap, MethodDesc* pMD, bool isDispatchCell);

//------------------------------------------------------------------------

#if defined(FRAMES_TURNED_FPO_ON)
#pragma optimize("", on)    // Go back to command line default optimizations
#undef FRAMES_TURNED_FPO_ON
#undef FPO_ON
#endif

#include "crossloaderallocatorhash.inl"

#endif  //__frames_h__
