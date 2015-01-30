//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
//    +-GCFrame                 - this frame doesn't represent a method call.
//    |                           it's sole purpose is to let the EE gc-protect
//    |                           object references that it is manipulating.
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
#ifdef FEATURE_REMOTING
//    +-GCSafeCollectionFrame   - this handles reporting for GCSafeCollections, which are
//    |                           generally used during appdomain transitions
//    |
#endif // FEATURE_REMOTING
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
//    + +-HelperMethodFrame_PROTECTOBJ - reports additional object references
//    |
//    +-TransitionFrame         - this abstract frame represents a transition from
//    | |                         one or more nested frameless method calls
//    | |                         to either a EE runtime helper function or
//    | |                         a framed method.
//    | |
//    | +-StubHelperFrame       - for instantiating stubs that need to grow stack arguments
//    | |
//    | +-SecureDelegateFrame   - represents a call Delegate.Invoke for secure delegate
//    |   |
//    |   +-MulticastFrame      - this frame protects arguments to a MulticastDelegate
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
//    |   |
//    |   +-ExternalMethodFrame  - represents a call from an ExternalMethdThunk
//    |   |
//    |   +-TPMethodFrame       - for calls on transparent proxy
//    |
//    +-UnmanagedToManagedFrame - this frame represents a transition from
//    | |                         unmanaged code back to managed code. It's
//    | |                         main functions are to stop COM+ exception
//    | |                         propagation and to expose unmanaged parameters.
//    | |
#ifdef FEATURE_COMINTEROP
//    | |
//    | +-ComMethodFrame        - this frame represents a transition from
//    |   |                       com to com+
//    |   |
//    |   +-ComPrestubMethodFrame - prestub frame for calls from COM to CLR
//    |
#endif //FEATURE_COMINTEROP
//    | +-UMThkCallFrame        - this frame represents an unmanaged->managed
//    |                           transition through N/Direct
//    |
//    +-ContextTransitionFrame  - this frame is used to mark an appdomain transition
//    |
//    |
//    +-TailCallFrame           - padding for tailcalls
//    |
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
#if defined(FEATURE_INCLUDE_ALL_INTERFACES) && defined(_TARGET_X86_)
//    |
//    +-ReverseEnterRuntimeFrame
//    |
//    +-LeaveRuntimeFrame
//    |
#endif
//    |
//    +-ExceptionFilterFrame - this frame wraps call to exception filter
//    |
//    +-SecurityContextFrame - place the security context of an assembly on the stack to ensure it will be included in security demands
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
 x86: The stub is generated by UMEntryThunk::CompileUMThunkWorker
    (in DllImportCallback.cpp) and it is frameless. It calls directly
    the managed target or to IL stub if marshaling is required.
 non-x86: The stub exists statically as UMThunkStub and calls to IL stub.
Prestub:
 The prestub is generated by GenerateUMThunkPrestub (x86) or exists statically
    as TheUMEntryPrestub (64-bit), and it erects an UMThkCallFrame frame.

Reverse P/Invoke AppDomain selector stub:
 The asm helper is IJWNOADThunkJumpTarget (in asmhelpers.asm) and it is frameless.

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
FRAME_TYPE_NAME(HelperMethodFrame_PROTECTOBJ)
FRAME_ABSTRACT_TYPE_NAME(FramedMethodFrame)
#ifdef FEATURE_REMOTING
FRAME_TYPE_NAME(TPMethodFrame)
#endif
FRAME_TYPE_NAME(SecureDelegateFrame)
FRAME_TYPE_NAME(MulticastFrame)
FRAME_ABSTRACT_TYPE_NAME(UnmanagedToManagedFrame)
#ifdef FEATURE_COMINTEROP
FRAME_TYPE_NAME(ComMethodFrame)
FRAME_TYPE_NAME(ComPlusMethodFrame)
FRAME_TYPE_NAME(ComPrestubMethodFrame)
#endif // FEATURE_COMINTEROP
FRAME_TYPE_NAME(PInvokeCalliFrame)
#ifdef FEATURE_HIJACK
FRAME_TYPE_NAME(HijackFrame)
#endif // FEATURE_HIJACK
FRAME_TYPE_NAME(PrestubMethodFrame)
FRAME_TYPE_NAME(StubDispatchFrame)
FRAME_TYPE_NAME(ExternalMethodFrame)
#ifdef FEATURE_READYTORUN
FRAME_TYPE_NAME(DynamicHelperFrame)
#endif
#if defined(_WIN64) || defined(_TARGET_ARM_)
FRAME_TYPE_NAME(StubHelperFrame)
#endif
FRAME_TYPE_NAME(GCFrame)
#ifdef FEATURE_INTERPRETER
FRAME_TYPE_NAME(InterpreterFrame)
#endif // FEATURE_INTERPRETER
FRAME_TYPE_NAME(ProtectByRefsFrame)
FRAME_TYPE_NAME(ProtectValueClassFrame)
#ifdef FEATURE_REMOTING
FRAME_TYPE_NAME(GCSafeCollectionFrame)
#endif // FEATURE_REMOTING
FRAME_TYPE_NAME(DebuggerClassInitMarkFrame)
FRAME_TYPE_NAME(DebuggerSecurityCodeMarkFrame)
FRAME_TYPE_NAME(DebuggerExitFrame)
FRAME_TYPE_NAME(DebuggerU2MCatchHandlerFrame)
#ifdef _TARGET_X86_
FRAME_TYPE_NAME(UMThkCallFrame)
#endif
#if defined(FEATURE_INCLUDE_ALL_INTERFACES) && defined(_TARGET_X86_)
FRAME_TYPE_NAME(ReverseEnterRuntimeFrame)
FRAME_TYPE_NAME(LeaveRuntimeFrame)
#endif
FRAME_TYPE_NAME(InlinedCallFrame)
FRAME_TYPE_NAME(ContextTransitionFrame)
FRAME_TYPE_NAME(TailCallFrame)
FRAME_TYPE_NAME(ExceptionFilterFrame)
#if defined(_DEBUG)
FRAME_TYPE_NAME(AssumeByrefFromJITStack)
#endif // _DEBUG
FRAME_TYPE_NAME(SecurityContextFrame)

#undef FRAME_ABSTRACT_TYPE_NAME
#undef FRAME_TYPE_NAME

//------------------------------------------------------------------------

#ifndef __frames_h__
#define __frames_h__
#if defined(_MSC_VER) && defined(_TARGET_X86_) && !defined(FPO_ON)
#pragma optimize("y", on)   // Small critical routines, don't put in EBP frame 
#define FPO_ON 1
#define FRAMES_TURNED_FPO_ON 1
#endif

#include "util.hpp"
#include "vars.hpp"
#include "regdisp.h"
#include "object.h"
#include "objecthandle.h"
#include <stddef.h>
#include "siginfo.hpp"
// context headers
#include "context.h"
#include "method.hpp"
#include "stackwalk.h"
#include "stubmgr.h"
#include "gms.h"
#include "threads.h"
#include "callingconvention.h"

// Forward references
class Frame;
class FieldMarshaler;
class FramedMethodFrame;
typedef VPTR(class FramedMethodFrame) PTR_FramedMethodFrame;
struct HijackArgs;
class UMEntryThunk;
class UMThunkMarshInfo;
class Marshaler;
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

#define DEFINE_VTABLE_GETTER(klass)             \
    public:                                     \
        static TADDR GetMethodFrameVPtr() {     \
            LIMITED_METHOD_CONTRACT;            \
            klass boilerplate(false);           \
            return *((TADDR*)&boilerplate);     \
        }                                       \
        klass(bool dummy) { LIMITED_METHOD_CONTRACT; }

#define DEFINE_VTABLE_GETTER_AND_CTOR(klass)    \
        DEFINE_VTABLE_GETTER(klass)             \
    protected:                                  \
        klass() { LIMITED_METHOD_CONTRACT; }

#else

#define DEFINE_VTABLE_GETTER(klass)             \
    public:                                     \
        static TADDR GetMethodFrameVPtr() {     \
            LIMITED_METHOD_CONTRACT;            \
            return klass::VPtrTargetVTable();   \
        }                                       \

#define DEFINE_VTABLE_GETTER_AND_CTOR(klass)    \
        DEFINE_VTABLE_GETTER(klass)             \

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
        FRAME_ATTR_OUT_OF_LINE = 2,         // The exception out of line (IP of the frame is not correct)
        FRAME_ATTR_FAULTED = 4,             // Exception caused by Win32 fault
        FRAME_ATTR_RESUMABLE = 8,           // We may resume from this frame
        FRAME_ATTR_CAPTURE_DEPTH_2 = 0x10,  // This is a helperMethodFrame and the capture occured at depth 2
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
    virtual const PTR_BYTE GetIP()
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

    virtual PCODE GetReturnAddress()
    {
        WRAPPER_NO_CONTRACT;
        TADDR ptr = GetReturnAddressPtr();
        return (ptr != NULL) ? *PTR_PCODE(ptr) : NULL;
    }

    virtual PTR_Context* GetReturnContextAddr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return NULL;
    }

    Context *GetReturnContext()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        PTR_Context* ppReturnContext = GetReturnContextAddr();
        if (! ppReturnContext)
            return NULL;
        return *ppReturnContext;
    }

    AppDomain *GetReturnDomain()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (! GetReturnContext())
            return NULL;
        return GetReturnContext()->GetDomain();
    }

#ifndef DACCESS_COMPILE
    virtual Object **GetReturnExecutionContextAddr()
    {
        LIMITED_METHOD_CONTRACT;
        return NULL;
    }

    void SetReturnAddress(TADDR val)
    {
        WRAPPER_NO_CONTRACT;
        TADDR ptr = GetReturnAddressPtr();
        _ASSERTE(ptr != NULL);
        *(TADDR*)ptr = val;
    }

#ifndef DACCESS_COMPILE
    void SetReturnContext(Context *pReturnContext)
    {
        WRAPPER_NO_CONTRACT;
        PTR_Context* ppReturnContext = GetReturnContextAddr();
        _ASSERTE(ppReturnContext);
        *ppReturnContext = pReturnContext;
    }
#endif

    void SetReturnExecutionContext(OBJECTREF ref)
    {
        WRAPPER_NO_CONTRACT;
        Object **pRef = GetReturnExecutionContextAddr();
        if (pRef != NULL)
            *pRef = OBJECTREFToObject(ref);
    }

    OBJECTREF GetReturnExecutionContext()
    {
        WRAPPER_NO_CONTRACT;
        Object **pRef = GetReturnExecutionContextAddr();
        if (pRef == NULL)
            return NULL;
        else
            return ObjectToOBJECTREF(*pRef);
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
    static void Term();

    // Callers, note that the REGDISPLAY parameter is actually in/out. While
    // UpdateRegDisplay is generally used to fill out the REGDISPLAY parameter, some
    // overrides (e.g., code:ResumableFrame::UpdateRegDisplay) will actually READ what
    // you pass in. So be sure to pass in a valid or zeroed out REGDISPLAY.
    virtual void UpdateRegDisplay(const PREGDISPLAY)
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
#ifdef FEATURE_REMOTING        
        TYPE_TP_METHOD_FRAME,
#endif        
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

#ifdef _DEBUG
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
    friend VOID RealCOMPlusThrow(OBJECTREF
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        , CorruptionSeverity severity
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        );
    friend FCDECL0(VOID, JIT_StressGC);
#ifdef _DEBUG
    friend LONG WINAPI CLRVectoredExceptionHandlerShim(PEXCEPTION_POINTERS pExceptionInfo);
#endif // _DEBUG
#ifdef _WIN64
    friend Thread * __stdcall JIT_InitPInvokeFrame(InlinedCallFrame *pFrame, PTR_VOID StubSecretArg);
#endif
#ifdef WIN64EXCEPTIONS
    friend class ExceptionTracker;
#endif
#if defined(DACCESS_COMPILE)
    friend class DacDbiInterfaceImpl;
#endif // DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP
    friend void COMToCLRWorkerBodyWithADTransition(Thread *pThread, ComMethodFrame *pFrame, ComCallWrapper *pWrap, UINT64 *pRetValOut);
#endif // FEATURE_COMINTEROP

    PTR_Frame  Next()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_Next;
    }

protected:
    // Frame is considered an abstract class: this protected constructor
    // causes any attempt to instantiate one to fail at compile-time.
    Frame()
    { 
        LIMITED_METHOD_CONTRACT;
    }
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
// caller-save-regsiters will be potential roots).
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

    virtual void UpdateRegDisplay(const PREGDISPLAY pRD);

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
    DEFINE_VTABLE_GETTER_AND_CTOR(ResumableFrame)
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

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(RedirectedThreadFrame)
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

    virtual void UpdateRegDisplay(const PREGDISPLAY);
#ifdef _TARGET_X86_
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

#ifdef _TARGET_X86_
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

// The define USE_FEF controls how this class is used.  Look for occurances
//  of USE_FEF.

class FaultingExceptionFrame : public Frame
{
    friend class CheckAsmOffsets;

#if defined(_TARGET_X86_)
    DWORD                   m_Esp;
    CalleeSavedRegisters    m_regs;
    TADDR                   m_ReturnAddress;
#endif

#ifdef WIN64EXCEPTIONS
    BOOL                    m_fFilterExecuted;  // Flag for FirstCallToHandler
    TADDR                   m_ReturnAddress;
    T_CONTEXT               m_ctx;
#endif // WIN64EXCEPTIONS

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
        return FRAME_ATTR_EXCEPTION | FRAME_ATTR_FAULTED;
    }

#if defined(_TARGET_X86_)
    CalleeSavedRegisters *GetCalleeSavedRegisters()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return &m_regs;
    }
#endif

#ifdef WIN64EXCEPTIONS
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
#endif // WIN64EXCEPTIONS

    virtual BOOL NeedsUpdateRegDisplay()
    {
        return TRUE;
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY);

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER(FaultingExceptionFrame)
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

    virtual void UpdateRegDisplay(const PREGDISPLAY);

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
    DEFINE_VTABLE_GETTER_AND_CTOR(FuncEvalFrame)
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

    virtual void UpdateRegDisplay(const PREGDISPLAY);

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
    DEFINE_VTABLE_GETTER_AND_CTOR(HelperMethodFrame)
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
#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
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
    DEFINE_VTABLE_GETTER_AND_CTOR(HelperMethodFrame_1OBJ)
};


//-----------------------------------------------------------------------------
// HelperMethodFrame_2OBJ
//-----------------------------------------------------------------------------

class HelperMethodFrame_2OBJ : public HelperMethodFrame
{
    VPTR_VTABLE_CLASS(HelperMethodFrame_2OBJ, HelperMethodFrame)

public:
#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
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
    DEFINE_VTABLE_GETTER_AND_CTOR(HelperMethodFrame_2OBJ)
};


//-----------------------------------------------------------------------------
// HelperMethodFrame_PROTECTOBJ
//-----------------------------------------------------------------------------

class HelperMethodFrame_PROTECTOBJ : public HelperMethodFrame
{
    VPTR_VTABLE_CLASS(HelperMethodFrame_PROTECTOBJ, HelperMethodFrame)

public:
#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
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
    DEFINE_VTABLE_GETTER_AND_CTOR(HelperMethodFrame_PROTECTOBJ)
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
            SO_TOLERANT;
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
#ifdef _TARGET_AMD64_
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

//+----------------------------------------------------------------------------
//
//  Class:      TPMethodFrame            private
//
//  Synopsis:   This frame is pushed onto the stack for calls on transparent
//              proxy
// 
//
//+----------------------------------------------------------------------------
#ifdef FEATURE_REMOTING
class TPMethodFrame : public FramedMethodFrame
{
    VPTR_VTABLE_CLASS(TPMethodFrame, FramedMethodFrame)

public:
    TPMethodFrame(TransitionBlock * pTransitionBlock);

    virtual int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_TP_METHOD_FRAME;
    }

    // GC protect arguments
    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

    // Our base class is a a M2U TransitionType; but we're not. So override and set us back to None.
    ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_NONE;
    }

#if defined(_TARGET_X86_) && !defined(DACCESS_COMPILE)
    void SetCbStackPop(UINT cbStackPop)
    {
        LIMITED_METHOD_CONTRACT;
        // Number of bytes to pop for x86 is stored right before the return value
        void * pReturnValue = GetReturnValuePtr();
        *(((DWORD *)pReturnValue) - 1) = cbStackPop;
    }
#endif

    // Aid the debugger in finding the actual address of callee
    virtual BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                            TraceDestination *trace, REGDISPLAY *regs);

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(TPMethodFrame)
};
#endif // FEATURE_REMOTING

//------------------------------------------------------------------------
// This represents a call Delegate.Invoke for secure delegate
// It's only used to gc-protect the arguments during the call.
// Actually the only reason to have this frame is so a proper
// Assembly can be reported
//------------------------------------------------------------------------

class SecureDelegateFrame : public TransitionFrame
{
    VPTR_VTABLE_CLASS(SecureDelegateFrame, TransitionFrame)

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
        return PTR_HOST_MEMBER_TADDR(SecureDelegateFrame, this,
                                     m_TransitionBlock);
    }

    static BYTE GetOffsetOfDatum()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return offsetof(SecureDelegateFrame, m_pMD);
    }

    static int GetOffsetOfTransitionBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return offsetof(SecureDelegateFrame, m_TransitionBlock);
    }

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc)
    {
        WRAPPER_NO_CONTRACT;
        TransitionFrame::GcScanRoots(fn, sc);
        PromoteCallerStack(fn, sc);
    }

    virtual Assembly *GetAssembly();

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
    DEFINE_VTABLE_GETTER_AND_CTOR(SecureDelegateFrame)
};


//------------------------------------------------------------------------
// This represents a call Multicast.Invoke. It's only used to gc-protect
// the arguments during the iteration.
//------------------------------------------------------------------------

class MulticastFrame : public SecureDelegateFrame
{
    VPTR_VTABLE_CLASS(MulticastFrame, SecureDelegateFrame)

    public:

    virtual Assembly *GetAssembly()
    {
        WRAPPER_NO_CONTRACT;
        return Frame::GetAssembly();
    }

    int GetFrameType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TYPE_MULTICAST;
    }

    virtual BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                            TraceDestination *trace, REGDISPLAY *regs);

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(MulticastFrame)
};


//-----------------------------------------------------------------------
// Transition frame from unmanaged to managed
//-----------------------------------------------------------------------

class UnmanagedToManagedFrame : public Frame
{
    friend class CheckAsmOffsets;

    VPTR_ABSTRACT_VTABLE_CLASS(UnmanagedToManagedFrame, Frame)

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

    // Retrives pointer to the lowest-addressed argument on
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
#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
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

#ifdef _TARGET_X86_
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

#if defined(_TARGET_X86_)
    CalleeSavedRegisters  m_calleeSavedRegisters;
    TADDR           m_ReturnAddress;
#elif defined(_TARGET_ARM_)
    TADDR           m_R11; // R11 chain
    TADDR           m_ReturnAddress;
    ArgumentRegisters m_argumentRegisters;
#elif defined (_TARGET_ARM64_)
    TADDR           m_fp;
    TADDR           m_ReturnAddress;
    ArgumentRegisters m_argumentRegisters;
#else
    TADDR           m_ReturnAddress;  // return address into unmanaged code
#endif
};

#ifdef FEATURE_COMINTEROP

//------------------------------------------------------------------------
// This frame represents a transition from COM to COM+
//------------------------------------------------------------------------

class ComMethodFrame : public UnmanagedToManagedFrame
{
    VPTR_VTABLE_CLASS(ComMethodFrame, UnmanagedToManagedFrame)
    VPTR_UNIQUE(VPTR_UNIQUE_ComMethodFrame)

public:

#ifdef _TARGET_X86_
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
    DEFINE_VTABLE_GETTER_AND_CTOR(ComMethodFrame)
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
    DEFINE_VTABLE_GETTER_AND_CTOR(ComPlusMethodFrame)
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

#ifdef _TARGET_X86_
    virtual void UpdateRegDisplay(const PREGDISPLAY);
#endif // _TARGET_X86_

    BOOL TraceFrame(Thread *thread, BOOL fromPatch,
                    TraceDestination *trace, REGDISPLAY *regs)
    {
        WRAPPER_NO_CONTRACT;

        trace->InitForUnmanaged(GetPInvokeCalliTarget());
        return TRUE;
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(PInvokeCalliFrame)
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

    virtual void UpdateRegDisplay(const PREGDISPLAY);

    // HijackFrames are created by trip functions. See OnHijackObjectTripThread()
    // and OnHijackScalarTripThread().  They are real C++ objects on the stack.  So
    // it's a public function -- but that doesn't mean you should make some.
    HijackFrame(LPVOID returnAddress, Thread *thread, HijackArgs *args);

protected:

    TADDR               m_ReturnAddress;
    PTR_Thread          m_Thread;
    DPTR(HijackArgs)    m_Args;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(HijackFrame)
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

    // Our base class is a a M2U TransitionType; but we're not. So override and set us back to None.
    ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_NONE;
    }

    Interception GetInterception();

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(PrestubMethodFrame)
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

#ifdef _TARGET_X86_
    virtual void UpdateRegDisplay(const PREGDISPLAY pRD);
    virtual PCODE GetReturnAddress();
#endif // _TARGET_X86_

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

private:
    friend class VirtualCallStubManager;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(StubDispatchFrame)
};

typedef VPTR(class StubDispatchFrame) PTR_StubDispatchFrame;


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

#ifdef _TARGET_X86_
    virtual void UpdateRegDisplay(const PREGDISPLAY pRD);
#endif
    
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(ExternalMethodFrame)
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

#ifdef _TARGET_X86_
    virtual void UpdateRegDisplay(const PREGDISPLAY pRD)
    {
        WRAPPER_NO_CONTRACT;
        UpdateRegDisplayHelper(pRD, 0);
    }
#endif

    virtual ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_InternalCall;
    }
    
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(DynamicHelperFrame)
};

typedef VPTR(class DynamicHelperFrame) PTR_DynamicHelperFrame;
#endif // FEATURE_READYTORUN

//------------------------------------------------------------------------
// This frame is used for instantiating stubs when the argument transform
// is too complex to generate a tail-calling stub.
//------------------------------------------------------------------------
#if !defined(_TARGET_X86_)
class StubHelperFrame : public TransitionFrame
{
    friend class CheckAsmOffsets;
    friend class StubLinkerCPU;

    VPTR_VTABLE_CLASS(StubHelperFrame, TransitionFrame)
    VPTR_UNIQUE(VPTR_UNIQUE_StubHelperFrame)

    TransitionBlock m_TransitionBlock;

    virtual TADDR GetTransitionBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_HOST_MEMBER_TADDR(StubHelperFrame, this,
            m_TransitionBlock);
    }

    static int GetOffsetOfTransitionBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return offsetof(StubHelperFrame, m_TransitionBlock);
    }

private:
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(StubHelperFrame)
};
#endif // _TARGET_X86_

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

    // Our base class is a a M2U TransitionType; but we're not. So override and set us back to None.
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
    DEFINE_VTABLE_GETTER_AND_CTOR(ComPrestubMethodFrame)
};

#endif // FEATURE_COMINTEROP


//------------------------------------------------------------------------
// This frame protects object references for the EE's convenience.
// This frame type actually is created from C++.
//------------------------------------------------------------------------
class GCFrame : public Frame
{
    VPTR_VTABLE_CLASS(GCFrame, Frame)

public:


    //--------------------------------------------------------------------
    // This constructor pushes a new GCFrame on the frame chain.
    //--------------------------------------------------------------------
#ifndef DACCESS_COMPILE
    GCFrame() {
        LIMITED_METHOD_CONTRACT;
    };

    GCFrame(OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior);
    GCFrame(Thread *pThread, OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior);
#endif
    void Init(Thread *pThread, OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior);


    //--------------------------------------------------------------------
    // Pops the GCFrame and cancels the GC protection. Also
    // trashes the contents of pObjRef's in _DEBUG.
    //--------------------------------------------------------------------
    VOID Pop();

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

#ifdef _DEBUG
    virtual BOOL Protects(OBJECTREF *ppORef)
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

#ifndef DACCESS_COMPILE
    void *operator new (size_t sz, void* p)
    {
        LIMITED_METHOD_CONTRACT;
        return p ;
    }
#endif

#if defined(_DEBUG_IMPL)
    const char* GetFrameTypeName() { LIMITED_METHOD_CONTRACT; return "GCFrame"; }
#endif

private:
    PTR_OBJECTREF m_pObjRefs;
    UINT          m_numObjRefs;
    PTR_Thread    m_pCurThread;
    BOOL          m_MaybeInterior;

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER(GCFrame)
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

    DEFINE_VTABLE_GETTER(InterpreterFrame)

};

typedef VPTR(class InterpreterFrame) PTR_InterpreterFrame;
#endif // FEATURE_INTERPRETER

#ifdef FEATURE_REMOTING
class GCSafeCollectionFrame : public Frame
{
    VPTR_VTABLE_CLASS(GCSafeCollectionFrame, Frame)
    PTR_VOID m_pCollection;

    public:
        //--------------------------------------------------------------------
        // This constructor pushes a new GCFrame on the frame chain.
        //--------------------------------------------------------------------
#ifndef DACCESS_COMPILE
        GCSafeCollectionFrame() { }
        GCSafeCollectionFrame(void *collection);
#endif

        virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

        VOID Pop();
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER(GCSafeCollectionFrame)
};
#endif // FEATURE_REMOTING

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
    DEFINE_VTABLE_GETTER_AND_CTOR(ProtectByRefsFrame)
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
    DEFINE_VTABLE_GETTER(ProtectValueClassFrame)
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
    DEFINE_VTABLE_GETTER(DebuggerClassInitMarkFrame)
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
    DEFINE_VTABLE_GETTER(DebuggerSecurityCodeMarkFrame)
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
    DEFINE_VTABLE_GETTER(DebuggerExitFrame)
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
    DEFINE_VTABLE_GETTER(DebuggerU2MCatchHandlerFrame)
};


class UMThunkMarshInfo;
typedef DPTR(class UMThunkMarshInfo) PTR_UMThunkMarshInfo;

class UMEntryThunk;
typedef DPTR(class UMEntryThunk) PTR_UMEntryThunk;

#ifdef _TARGET_X86_
//------------------------------------------------------------------------
// This frame guards an unmanaged->managed transition thru a UMThk
//------------------------------------------------------------------------

class UMThkCallFrame : public UnmanagedToManagedFrame
{
    VPTR_VTABLE_CLASS(UMThkCallFrame, UnmanagedToManagedFrame)

public:

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    PTR_UMEntryThunk GetUMEntryThunk();

    static int GetOffsetOfUMEntryThunk()
    {
        WRAPPER_NO_CONTRACT;
        return GetOffsetOfDatum();
    }

protected:

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(UMThkCallFrame)
};
#endif // _TARGET_X86_

#if defined(_TARGET_X86_)
//-------------------------------------------------------------------------
// Exception handler for COM to managed frame
//  and the layout of the exception registration record structure in the stack
//  the layout is similar to the NT's EXCEPTIONREGISTRATION record
//  followed by the UnmanagedToManagedFrame specific info

struct ComToManagedExRecord
{
    EXCEPTION_REGISTRATION_RECORD   m_ExReg;
    ArgumentRegisters               m_argRegs;
    GSCookie                        m_gsCookie;
    UMThkCallFrame                  m_frame;

    UnmanagedToManagedFrame * GetCurrFrame()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_frame;
    }
};
#endif // _TARGET_X86_

#if defined(FEATURE_INCLUDE_ALL_INTERFACES) && defined(_TARGET_X86_)
//-----------------------------------------------------------------------------
// ReverseEnterRuntimeFrame
//-----------------------------------------------------------------------------

class ReverseEnterRuntimeFrame : public Frame
{
    VPTR_VTABLE_CLASS(ReverseEnterRuntimeFrame, Frame)

public:
    //------------------------------------------------------------------------
    // Performs cleanup on an exception unwind
    //------------------------------------------------------------------------
#ifndef DACCESS_COMPILE
    virtual void ExceptionUnwind()
    {
        WRAPPER_NO_CONTRACT;
         GetThread()->ReverseLeaveRuntime();
    }
#endif

    //---------------------------------------------------------------
    // Expose key offsets and values for stub generation.
    //---------------------------------------------------------------

    static UINT32 GetNegSpaceSize()
    {
        LIMITED_METHOD_CONTRACT;
        return sizeof(GSCookie);
    }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(ReverseEnterRuntimeFrame)
};

//-----------------------------------------------------------------------------
// LeaveRuntimeFrame
//-----------------------------------------------------------------------------

class LeaveRuntimeFrame : public Frame
{
    VPTR_VTABLE_CLASS(LeaveRuntimeFrame, Frame)

public:
    //------------------------------------------------------------------------
    // Performs cleanup on an exception unwind
    //------------------------------------------------------------------------
#ifndef DACCESS_COMPILE
    virtual void ExceptionUnwind()
    {
        WRAPPER_NO_CONTRACT;
         Thread::EnterRuntime();
    }
#endif

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(LeaveRuntimeFrame)
};
#endif

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
            return PTR_MethodDesc(m_Datum);
        else
            return NULL;
    }

    BOOL HasFunction()
    {
        WRAPPER_NO_CONTRACT;

#ifdef _WIN64
        return ((m_Datum != NULL) && !(dac_cast<TADDR>(m_Datum) & 0x1));
#else // _WIN64
        return ((dac_cast<TADDR>(m_Datum) & ~0xffff) != 0);
#endif // _WIN64
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
#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
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
#elif defined(_WIN64)
        // On 64bit, the actual interop MethodDesc is saved off in a field off the InlinedCrawlFrame
        // which is populated by the JIT. Refer to JIT_InitPInvokeFrame for details.
        return PTR_MethodDesc(m_StubSecretArg);
#else
        _ASSERTE(!"NYI - Interop method reporting for this architecture!");
        return NULL;
#endif // defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    }

    virtual void UpdateRegDisplay(const PREGDISPLAY);

    // m_Datum contains MethodDesc ptr or
    // - on AMD64: CALLI target address (if lowest bit is set)
    // - on X86: argument stack size (if value is <64k)
    // See code:HasFunction.
    PTR_NDirectMethodDesc   m_Datum;

#ifdef _WIN64
    // IL stubs fill this field with the incoming secret argument when they erect
    // InlinedCallFrame so we know which interop method was invoked even if the frame
    // is not active at the moment.
    PTR_VOID                m_StubSecretArg;
#endif // _WIN64

protected:
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
    DEFINE_VTABLE_GETTER_AND_CTOR(InlinedCallFrame)
};

//------------------------------------------------------------------------
// This frame is used to mark a Context/AppDomain Transition
//------------------------------------------------------------------------

class ContextTransitionFrame : public Frame
{
private:
    PTR_Context m_pReturnContext;
    PTR_Object  m_ReturnExecutionContext;
    PTR_Object  m_LastThrownObjectInParentContext;                                        
    ULONG_PTR   m_LockCount;            // Number of locks the thread takes
                                        // before the transition.
    ULONG_PTR   m_CriticalRegionCount;

    VPTR_VTABLE_CLASS(ContextTransitionFrame, Frame)

public:
    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);

    virtual PTR_Context* GetReturnContextAddr()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return &m_pReturnContext;
    }

    virtual Object **GetReturnExecutionContextAddr()
    {
        LIMITED_METHOD_CONTRACT;
        return (Object **) &m_ReturnExecutionContext;
    }

    OBJECTREF GetLastThrownObjectInParentContext()
    {
        return ObjectToOBJECTREF(m_LastThrownObjectInParentContext);
    }

    void SetLastThrownObjectInParentContext(OBJECTREF lastThrownObject)
    {
        m_LastThrownObjectInParentContext = OBJECTREFToObject(lastThrownObject);
    }

    void SetLockCount(DWORD lockCount, DWORD criticalRegionCount)
    {
        LIMITED_METHOD_CONTRACT;
        m_LockCount = lockCount;
        m_CriticalRegionCount = criticalRegionCount;
    }
    void GetLockCount(DWORD* pLockCount, DWORD* pCriticalRegionCount)
    {
        LIMITED_METHOD_CONTRACT;
        *pLockCount = (DWORD) m_LockCount;
        *pCriticalRegionCount = (DWORD) m_CriticalRegionCount;
    }

    // Let debugger know that we're transitioning between AppDomains.
    ETransitionType GetTransitionType()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TT_AppDomain;
    }

#ifndef DACCESS_COMPILE
    ContextTransitionFrame()
    : m_pReturnContext(NULL)
    , m_ReturnExecutionContext(NULL)
    , m_LastThrownObjectInParentContext(NULL)
    , m_LockCount(0)
    , m_CriticalRegionCount(0)
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER(ContextTransitionFrame)
};

// TODO [DAVBR]: For the full fix for VsWhidbey 450273, this
// may be uncommented once isLegalManagedCodeCaller works properly
// with non-return address inputs, and with non-DEBUG builds
//bool isLegalManagedCodeCaller(TADDR retAddr);
bool isRetAddr(TADDR retAddr, TADDR* whereCalled);

//------------------------------------------------------------------------
#ifdef _TARGET_X86_
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
#else
// This frame is used as padding for tailcalls which require more space
// than the caller has in it's incoming argument space.
// To do a tail call from A to B, A calls JIT_TailCall, which unwinds A's frame
// and sets up a TailCallFrame and the arguments. It then jumps to B.
// If B also does a tail call, then we reuse the
// existing TailCallFrame instead of setting up a second one.
// 
// This is also used whenever value types that aren't enregisterable are
// passed by value instead of ref. This is currently not a very important
// scenario as tail calls are uncommon.
#endif
//------------------------------------------------------------------------

class TailCallFrame : public Frame
{
    VPTR_VTABLE_CLASS(TailCallFrame, Frame)

#if defined(_TARGET_X86_)
    TADDR           m_CallerAddress;    // the address the tailcall was initiated from
    CalleeSavedRegisters    m_regs;     // callee saved registers - the stack walk assumes that all non-JIT frames have them
    TADDR           m_ReturnAddress;    // the return address of the tailcall
#elif defined(_TARGET_AMD64_)
    TADDR                 m_pGCLayout;
    TADDR                 m_padding;    // code:StubLinkerCPU::CreateTailCallCopyArgsThunk expects the size of TailCallFrame to be 16-byte aligned
    CalleeSavedRegisters  m_calleeSavedRegisters;
    TADDR                 m_ReturnAddress;
#elif defined(_TARGET_ARM_)
    union {
        CalleeSavedRegisters m_calleeSavedRegisters;
        // alias saved link register as m_ReturnAddress
        struct {
            INT32 r4, r5, r6, r7, r8, r9, r10;
            INT32 r11;
            TADDR m_ReturnAddress;
        };
    };
#else
    TADDR                 m_ReturnAddress;
#endif

public:
#if !defined(_TARGET_X86_)

#ifndef DACCESS_COMPILE
    TailCallFrame(T_CONTEXT * pContext, Thread * pThread) 
    {
        InitFromContext(pContext);
        m_Next = pThread->GetFrame();
    }

    void InitFromContext(T_CONTEXT * pContext);

    // Architecture-specific method to initialize a CONTEXT record as if the first
    // part of the TailCallHelperStub had executed
    static TailCallFrame * AdjustContextForTailCallHelperStub(_CONTEXT * pContext, size_t cbNewArgArea, Thread * pThread);
#endif

    static TailCallFrame * GetFrameFromContext(CONTEXT * pContext);
#endif // !_TARGET_X86_

#if defined(_TARGET_X86_)
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
#endif // _TARGET_X86_

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

    virtual void UpdateRegDisplay(const PREGDISPLAY pRD);

#ifdef _TARGET_AMD64_
    void SetGCLayout(TADDR pGCLayout)
    {
        LIMITED_METHOD_CONTRACT;
        m_pGCLayout = pGCLayout;
    }

    virtual void GcScanRoots(promote_func *fn, ScanContext* sc);
#else
    void SetGCLayout(TADDR pGCLayout)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pGCLayout == NULL);
    }
#endif

private:
    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(TailCallFrame)
};

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
    DEFINE_VTABLE_GETTER_AND_CTOR(ExceptionFilterFrame)
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
    DEFINE_VTABLE_GETTER(AssumeByrefFromJITStack)
}; //AssumeByrefFromJITStack

#endif //_DEBUG

//-----------------------------------------------------------------------------
// FrameWithCookie is used to declare a Frame in source code with a cookie
// immediately preceeding it.
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

    // TailCallFrame
    FrameWithCookie(T_CONTEXT * pContext, Thread *thread) :
        m_gsCookie(GetProcessGSCookie()), m_frame(pContext, thread) { WRAPPER_NO_CONTRACT; }

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


// The frame doesn't represent a transition of any sort, it's simply placed on the stack to represent an assembly that will be found
// and checked by stackwalking security demands. This can be used in scenarios where an assembly is implicitly controlling a
// security sensitive operation without being explicitly represented on the stack. For example, an assembly decorating one of its
// classes or methods with a custom attribute can implicitly cause the ctor or property setters for that attribute to be executed by
// a third party if they happen to browse the attributes on the assembly.
// Note: This frame is pushed from managed code, so be sure to keep the layout synchronized with that in
// bcl\system\reflection\customattribute.cs.
class SecurityContextFrame : public Frame
{
    VPTR_VTABLE_CLASS(SecurityContextFrame, Frame)

    Assembly *m_pAssembly;

public:
    virtual Assembly *GetAssembly() { LIMITED_METHOD_CONTRACT; return m_pAssembly; }

    void SetAssembly(Assembly *pAssembly) { LIMITED_METHOD_CONTRACT; m_pAssembly = pAssembly; }

    // Keep as last entry in class
    DEFINE_VTABLE_GETTER_AND_CTOR(SecurityContextFrame)
};


// This holder is defined for addressing a very specific issue:
// Frames that use GCPROTECT_BEGIN/GCPROTECT_END can end up referencing corrupted object refs
// when an exception is thrown until the point where the Frame is actually popped from the thread's Frame-chain.
// Stack space allocated for OBJECTREFs in a try block may be reused in the catch block by other structures, 
// corrupting our protected OBJECTREFs and the Frame containing them. While the Frame is still on the callstack
// a GC may occur, detecting the corrupt OBJECTREF and taking down the process. The FrameWithCookieHolder 
// forces the Frame to be popped out when exiting the current scope, therefore before the OBJECTREF is corrupted.
//
// This holder explicitly calls Thread::SetFrame, therefore potentially removing Frames from the thread's frame 
// chain without properly calling their corresponding ExceptionUnwind() method. This is extremely dangerous to 
// use unless it is backed by a call to UnwindAndContinueRethrowHelperInsideCatch() which does the same thing
// (and has been vetted to be correct in doing so). Using this holder in any other circumstances may lead to bugs that 
// are extremely difficult to track down.
template <typename TYPE>
class FrameWithCookieHolder 
{
    protected:
		FrameWithCookie<TYPE>	m_frame;

	public:
		FORCEINLINE FrameWithCookieHolder()
			: m_frame()
		{
		}

		//	GCFrame 
		FORCEINLINE	FrameWithCookieHolder(OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior)
			: m_frame(pObjRefs, numObjRefs, maybeInterior)
		{
		}

		FORCEINLINE ~FrameWithCookieHolder()
		{
#ifndef DACCESS_COMPILE

			Thread* pThread = GetThread();
			if (pThread)
			{
			    GCX_COOP();
			    pThread->SetFrame(&m_frame);
			    m_frame.Pop();
			}

#endif // #ifndef DACCESS_COMPILE
		}

};

#ifndef DACCESS_COMPILE
// Restrictions from FrameWithCookieHolder are also applying for GCPROTECT_HOLDER. 
// Please read the FrameWithCookieHolder comments before using GCPROTECT_HOLDER.  
#define	GCPROTECT_HOLDER(ObjRefStruct)									\
                FrameWithCookieHolder<GCFrame>	__gcframe((OBJECTREF*)&(ObjRefStruct),  \
                        sizeof(ObjRefStruct)/sizeof(OBJECTREF),         \
                        FALSE);                                          

#else // #ifndef DACCESS_COMPILE

#define	GCPROTECT_HOLDER(ObjRefStruct)

#endif // #ifndef DACCESS_COMPILE


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
//     non-const refernce arguments. Unfortunately, this is necessary in
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
                FrameWithCookie<GCFrame> __gcframe(                     \
                        (OBJECTREF*)&(ObjRefStruct),                    \
                        sizeof(ObjRefStruct)/sizeof(OBJECTREF),         \
                        FALSE);                                         \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_BEGIN_THREAD(pThread, ObjRefStruct)           do {    \
                FrameWithCookie<GCFrame> __gcframe(                     \
                        pThread,                                        \
                        (OBJECTREF*)&(ObjRefStruct),                    \
                        sizeof(ObjRefStruct)/sizeof(OBJECTREF),         \
                        FALSE);                                         \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_ARRAY_BEGIN(ObjRefArray,cnt) do {                     \
                FrameWithCookie<GCFrame> __gcframe(                     \
                        (OBJECTREF*)&(ObjRefArray),                     \
                        cnt * sizeof(ObjRefArray) / sizeof(OBJECTREF),  \
                        FALSE);                                         \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_BEGININTERIOR(ObjRefStruct)           do {            \
                FrameWithCookie<GCFrame> __gcframe(                     \
                        (OBJECTREF*)&(ObjRefStruct),                    \
                        sizeof(ObjRefStruct)/sizeof(OBJECTREF),         \
                        TRUE);                                          \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define GCPROTECT_BEGININTERIOR_ARRAY(ObjRefArray,cnt) do {             \
                FrameWithCookie<GCFrame> __gcframe(                     \
                        (OBJECTREF*)&(ObjRefArray),                     \
                        cnt,                                            \
                        TRUE);                                          \
                /* work around unreachable code warning */              \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)


#define GCPROTECT_END()                                                 \
                DEBUG_ASSURE_NO_RETURN_END(GCPROTECT) }                 \
                __gcframe.Pop(); } while(0)


#else // #ifndef DACCESS_COMPILE

#define GCPROTECT_BEGIN(ObjRefStruct)
#define GCPROTECT_ARRAY_BEGIN(ObjRefArray,cnt)
#define GCPROTECT_BEGININTERIOR(ObjRefStruct)
#define GCPROTECT_END()

#endif // #ifndef DACCESS_COMPILE


#define ASSERT_ADDRESS_IN_STACK(address) _ASSERTE (GetThread () && GetThread ()->IsAddressInStack (address));

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

//------------------------------------------------------------------------

#if defined(FRAMES_TURNED_FPO_ON)
#pragma optimize("", on)    // Go back to command line default optimizations
#undef FRAMES_TURNED_FPO_ON
#undef FPO_ON
#endif

#endif  //__frames_h__
