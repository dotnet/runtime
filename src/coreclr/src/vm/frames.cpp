// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// FRAMES.CPP



#include "common.h"
#include "log.h"
#include "frames.h"
#include "threads.h"
#include "object.h"
#include "method.hpp"
#include "class.h"
#include "excep.h"
#include "security.h"
#include "stublink.h"
#include "fieldmarshaler.h"
#include "objecthandle.h"
#include "siginfo.hpp"
#include "gc.h"
#include "dllimportcallback.h"
#include "stackwalk.h"
#include "dbginterface.h"
#include "gms.h"
#include "eeconfig.h"
#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif
#include "ecall.h"
#include "clsload.hpp"
#include "cgensys.h"
#include "virtualcallstub.h"
#include "mdaassistants.h"
#include "dllimport.h"
#include "gcrefmap.h"
#include "asmconstants.h"

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif // FEATURE_INTERPRETER

#include "argdestination.h"

#if CHECK_APP_DOMAIN_LEAKS
#define CHECK_APP_DOMAIN    GC_CALL_CHECK_APP_DOMAIN
#else
#define CHECK_APP_DOMAIN    0
#endif

//-----------------------------------------------------------------------
#if _DEBUG
//-----------------------------------------------------------------------

#ifndef DACCESS_COMPILE

unsigned dbgStubCtr = 0;
unsigned dbgStubTrip = 0xFFFFFFFF;

void Frame::Log() {
    WRAPPER_NO_CONTRACT;

    if (!LoggingOn(LF_STUBS, LL_INFO1000000))
        return;

    dbgStubCtr++;
    if (dbgStubCtr > dbgStubTrip) {
        dbgStubCtr++;      // basicly a nop to put a breakpoint on.
    }

    MethodDesc* method = GetFunction();

#ifdef _TARGET_X86_
    if (GetVTablePtr() == UMThkCallFrame::GetMethodFrameVPtr())
        method = ((UMThkCallFrame*) this)->GetUMEntryThunk()->GetMethod();
#endif

    STRESS_LOG3(LF_STUBS, LL_INFO1000000, "STUBS: In Stub with Frame %p assoc Method %pM FrameType = %pV\n", this, method, *((void**) this));

    char buff[64];
    const char* frameType;
    if (GetVTablePtr() == PrestubMethodFrame::GetMethodFrameVPtr())
        frameType = "PreStub";
#ifdef _TARGET_X86_
    else if (GetVTablePtr() == UMThkCallFrame::GetMethodFrameVPtr())
        frameType = "UMThkCallFrame";
#endif
    else if (GetVTablePtr() == PInvokeCalliFrame::GetMethodFrameVPtr()) 
    {
        sprintf_s(buff, COUNTOF(buff), "PInvoke CALLI target" FMT_ADDR,
                  DBG_ADDR(((PInvokeCalliFrame*)this)->GetPInvokeCalliTarget()));
        frameType = buff;
    }
    else if (GetVTablePtr() == StubDispatchFrame::GetMethodFrameVPtr())
        frameType = "StubDispatch";
    else if (GetVTablePtr() == ExternalMethodFrame::GetMethodFrameVPtr())
        frameType = "ExternalMethod";
    else 
        frameType = "Unknown";

    if (method != 0)
        LOG((LF_STUBS, LL_INFO1000000, 
             "IN %s Stub Method = %s::%s SIG %s ESP of return" FMT_ADDR "\n",
             frameType, 
             method->m_pszDebugClassName,
             method->m_pszDebugMethodName,
             method->m_pszDebugMethodSignature,
             DBG_ADDR(GetReturnAddressPtr())));
    else 
        LOG((LF_STUBS, LL_INFO1000000, 
             "IN %s Stub Method UNKNOWN ESP of return" FMT_ADDR "\n", 
             frameType, 
             DBG_ADDR(GetReturnAddressPtr()) ));

    _ASSERTE(GetThread()->PreemptiveGCDisabled());
}

//-----------------------------------------------------------------------
// This function is used to log transitions in either direction 
// between unmanaged code and CLR/managed code.
// This is typically done in a stub that sets up a Frame, which is
// passed as an argument to this function.

void __stdcall Frame::LogTransition(Frame* frame)
{

    CONTRACTL {
        DEBUG_ONLY;
        NOTHROW;
        ENTRY_POINT;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    BEGIN_ENTRYPOINT_VOIDRET;

#ifdef _TARGET_X86_
    // On x86, StubLinkerCPU::EmitMethodStubProlog calls Frame::LogTransition
    // but the caller of EmitMethodStubProlog sets the GSCookie later on.
    // So the cookie is not initialized by the point we get here.
#else
    _ASSERTE(*frame->GetGSCookiePtr() == GetProcessGSCookie());
#endif

    if (Frame::ShouldLogTransitions())
        frame->Log();

    END_ENTRYPOINT_VOIDRET;
} // void Frame::Log()

#endif // #ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------
#endif // _DEBUG
//-----------------------------------------------------------------------


// TODO [DAVBR]: For the full fix for VsWhidbey 450273, all the below
// may be uncommented once isLegalManagedCodeCaller works properly
// with non-return address inputs, and with non-DEBUG builds
#if 0
//-----------------------------------------------------------------------
// returns TRUE if retAddr, is a return address that can call managed code

bool isLegalManagedCodeCaller(PCODE retAddr) {
    WRAPPER_NO_CONTRACT;
#ifdef _TARGET_X86_

        // we expect to be called from JITTED code or from special code sites inside
        // mscorwks like callDescr which we have put a NOP (0x90) so we know that they
        // are specially blessed. 
    if (!ExecutionManager::IsManagedCode(retAddr) &&
        (
#ifdef DACCESS_COMPILE
         !(PTR_BYTE(retAddr).IsValid()) ||
#endif
         ((*PTR_BYTE(retAddr) != 0x90) &&
          (*PTR_BYTE(retAddr) != 0xcc))))
    {
        LOG((LF_GC, LL_INFO10, "Bad caller to managed code: retAddr=0x%08x, *retAddr=0x%x\n",
             retAddr, *(BYTE*)PTR_BYTE(retAddr)));
        
        return false;
    }

        // it better be a return address of some kind 
    TADDR dummy;
    if (isRetAddr(retAddr, &dummy))
        return true;

#ifndef DACCESS_COMPILE
#ifdef DEBUGGING_SUPPORTED
    // The debugger could have dropped an INT3 on the instruction that made the call
    // Calls can be 2 to 7 bytes long
    if (CORDebuggerAttached()) {
        PTR_BYTE ptr = PTR_BYTE(retAddr);
        for (int i = -2; i >= -7; --i)
            if (ptr[i] == 0xCC)
                return true;
        return false;
    }
#endif // DEBUGGING_SUPPORTED
#endif // #ifndef DACCESS_COMPILE

    _ASSERTE(!"Bad return address on stack");
    return false;
#else  // _TARGET_X86_
    return true;
#endif // _TARGET_X86_
}
#endif //0


//-----------------------------------------------------------------------
// Count of the number of frame types
const size_t FRAME_TYPES_COUNT = 
#define FRAME_TYPE_NAME(frameType) +1
#include "frames.h"
;

#if defined (_DEBUG_IMPL)   // _DEBUG and !DAC

//-----------------------------------------------------------------------
// Implementation of the global table of names.  On the DAC side, just the global pointer.
//  On the runtime side, the array of names.
    #define FRAME_TYPE_NAME(x) {x::GetMethodFrameVPtr(), #x} ,
    static FrameTypeName FrameTypeNameTable[] = {
    #include "frames.h"
    };


/* static */
PTR_CSTR Frame::GetFrameTypeName(TADDR vtbl)
{
    LIMITED_METHOD_CONTRACT;
    for (size_t i=0; i<FRAME_TYPES_COUNT; ++i)
    {
        if (vtbl == FrameTypeNameTable[(int)i].vtbl)
        {
            return FrameTypeNameTable[(int)i].name;
        }
    }

    return NULL;
} // char* Frame::FrameTypeName()


//-----------------------------------------------------------------------


void Frame::LogFrame(
    int         LF,                     // Log facility for this call.
    int         LL)                     // Log Level for this call.
{
    char        buf[32];
    const char  *pFrameType;
    pFrameType = GetFrameTypeName();
    
    if (pFrameType == NULL)
    {
        pFrameType = GetFrameTypeName(GetVTablePtr());
    }

    if (pFrameType == NULL)
    {
        _ASSERTE(!"New Frame type needs to be added to FrameTypeName()");
        // Pointer is up to 17chars + vtbl@ = 22 chars
        sprintf_s(buf, COUNTOF(buf), "vtbl@%p", GetVTablePtr());
        pFrameType = buf;
    }

    LOG((LF, LL, "FRAME: addr:%p, next:%p, type:%s\n",
         this, m_Next, pFrameType));
} // void Frame::LogFrame()

void Frame::LogFrameChain(
    int         LF,                     // Log facility for this call.
    int         LL)                     // Log Level for this call.
{
    if (!LoggingOn(LF, LL))
        return;
    
    Frame *pFrame = this;
    while (pFrame != FRAME_TOP)
    {
        pFrame->LogFrame(LF, LL);
        pFrame = pFrame->m_Next;
    }
} // void Frame::LogFrameChain()

//-----------------------------------------------------------------------
#endif // _DEBUG_IMPL
//-----------------------------------------------------------------------

#ifndef DACCESS_COMPILE

// This hashtable contains the vtable value of every Frame type.
static PtrHashMap* s_pFrameVTables = NULL;

// static
void Frame::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // create a table big enough for all the frame types, not in asynchronous mode, and with no lock owner
    s_pFrameVTables = ::new PtrHashMap;
    s_pFrameVTables->Init(2 * FRAME_TYPES_COUNT, FALSE, &g_lockTrustMeIAmThreadSafe);
#define FRAME_TYPE_NAME(frameType)                          \
    s_pFrameVTables->InsertValue(frameType::GetMethodFrameVPtr(), \
                               (LPVOID) frameType::GetMethodFrameVPtr());
#include "frames.h"

} // void Frame::Init()

// static
void Frame::Term()
{
    LIMITED_METHOD_CONTRACT;
    delete s_pFrameVTables;
    s_pFrameVTables = NULL;
}

#endif // DACCESS_COMPILE

// Returns true if the Frame's VTablePtr is valid

// static
bool Frame::HasValidVTablePtr(Frame * pFrame)
{
    WRAPPER_NO_CONTRACT;

    if (pFrame == NULL || pFrame == FRAME_TOP)
        return false;
    
#ifndef DACCESS_COMPILE
    TADDR vptr = pFrame->GetVTablePtr();
    //
    // Helper MethodFrame,GCFrame,DebuggerSecurityCodeMarkFrame are the most 
    // common frame types, explicitly check for them.
    //
    if (vptr == HelperMethodFrame::GetMethodFrameVPtr())
        return true;

    if (vptr == GCFrame::GetMethodFrameVPtr())
        return true;

    if (vptr == DebuggerSecurityCodeMarkFrame::GetMethodFrameVPtr())
        return true;

    //
    // otherwise consult the hashtable
    //
    if (s_pFrameVTables->LookupValue(vptr, (LPVOID) vptr) == (LPVOID) INVALIDENTRY)
        return false;
#endif

    return true;
}

// Returns the location of the expected GSCookie,
// Return NULL if the frame's vtable pointer is corrupt
//
// Note that Frame::GetGSCookiePtr is a virtual method, 
// and so it cannot be used without first checking if
// the vtable is valid.

// static
PTR_GSCookie Frame::SafeGetGSCookiePtr(Frame * pFrame)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pFrame != FRAME_TOP);

    if (Frame::HasValidVTablePtr(pFrame))
        return pFrame->GetGSCookiePtr();
    else
        return NULL;
}

//-----------------------------------------------------------------------
#ifndef DACCESS_COMPILE
//-----------------------------------------------------------------------
// Link and Unlink this frame.
//-----------------------------------------------------------------------

VOID Frame::Push()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    Push(GetThread());
}

VOID Frame::Push(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    _ASSERTE(*GetGSCookiePtr() == GetProcessGSCookie());
    
    m_Next = pThread->GetFrame();

    // PAGE_SIZE is used to relax the assert for cases where two Frames are
    // declared in the same source function. We cannot predict the order
    // in which the C compiler will lay them out in the stack frame.
    // So PAGE_SIZE is a guess of the maximum stack frame size of any method
    // with multiple Frames in mscorwks.dll
    _ASSERTE(((m_Next == FRAME_TOP) ||
              (PBYTE(m_Next) + (2 * PAGE_SIZE)) > PBYTE(this)) &&
             "Pushing a frame out of order ?");
    
    _ASSERTE(// If AssertOnFailFast is set, the test expects to do stack overrun 
             // corruptions. In that case, the Frame chain may be corrupted,
             // and the rest of the assert is not valid.
             // Note that the corrupted Frame chain will be detected 
             // during stack-walking.
             !g_pConfig->fAssertOnFailFast() ||
             (m_Next == FRAME_TOP) ||
             (*m_Next->GetGSCookiePtr() == GetProcessGSCookie()));
    
    pThread->SetFrame(this);
}

VOID Frame::Pop()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    Pop(GetThread());
}

VOID Frame::Pop(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    _ASSERTE(pThread->GetFrame() == this && "Popping a frame out of order ?");
    _ASSERTE(*GetGSCookiePtr() == GetProcessGSCookie());
    _ASSERTE(// If AssertOnFailFast is set, the test expects to do stack overrun 
             // corruptions. In that case, the Frame chain may be corrupted,
             // and the rest of the assert is not valid.
             // Note that the corrupted Frame chain will be detected 
             // during stack-walking.
             !g_pConfig->fAssertOnFailFast() ||
             (m_Next == FRAME_TOP) ||
             (*m_Next->GetGSCookiePtr() == GetProcessGSCookie()));

    pThread->SetFrame(m_Next);
    m_Next = NULL;
}

#ifdef FEATURE_PAL     
Frame::~Frame()        
{      
    if (m_Next != NULL)
    {
        // When the frame is destroyed, make sure it is no longer in the
        // frame chain managed by the Thread.
        Thread* pThread = GetThread();
        if (pThread != NULL && pThread->GetFrame() == this)
        {
            Pop(pThread);
        }
    }
}      
#endif // FEATURE_PAL

//-----------------------------------------------------------------------
#endif // #ifndef DACCESS_COMPILE
//---------------------------------------------------------------
// Get the extra param for shared generic code.
//---------------------------------------------------------------
PTR_VOID TransitionFrame::GetParamTypeArg()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // This gets called while creating stack traces during exception handling.
    // Using the ArgIterator constructor calls ArgIterator::Init which calls GetInitialOfsAdjust
    // which calls SizeOfArgStack, which thinks it may load value types.
    // However all these will have previously been loaded.
    // 
    // I'm not entirely convinced this is the best places to put this: CrawlFrame::GetExactGenericArgsToken
    // may be another option.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    MethodDesc *pFunction = GetFunction();
    _ASSERTE (pFunction->RequiresInstArg());

    MetaSig msig(pFunction);
    ArgIterator argit (&msig);
        
    INT offs = argit.GetParamTypeArgOffset();

    TADDR taParamTypeArg = *PTR_TADDR(GetTransitionBlock() + offs);
    return PTR_VOID(taParamTypeArg);
}

TADDR TransitionFrame::GetAddrOfThis()
{
    WRAPPER_NO_CONTRACT;
    return GetTransitionBlock() + ArgIterator::GetThisOffset();
}

VASigCookie * TransitionFrame::GetVASigCookie()
{
#if defined(_TARGET_X86_)
    LIMITED_METHOD_CONTRACT;
    return dac_cast<PTR_VASigCookie>(
        *dac_cast<PTR_TADDR>(GetTransitionBlock() +
        sizeof(TransitionBlock)));
#else
    WRAPPER_NO_CONTRACT;
    MetaSig msig(GetFunction());
    ArgIterator argit(&msig);
    return PTR_VASigCookie(
        *dac_cast<PTR_TADDR>(GetTransitionBlock() + argit.GetVASigCookieOffset()));
#endif
}

#ifndef DACCESS_COMPILE
PrestubMethodFrame::PrestubMethodFrame(TransitionBlock * pTransitionBlock, MethodDesc * pMD)
    : FramedMethodFrame(pTransitionBlock, pMD)
{
    LIMITED_METHOD_CONTRACT;
}
#endif // #ifndef DACCESS_COMPILE

BOOL PrestubMethodFrame::TraceFrame(Thread *thread, BOOL fromPatch,
                                    TraceDestination *trace, REGDISPLAY *regs)
{
    WRAPPER_NO_CONTRACT;

    //
    // We want to set a frame patch, unless we're already at the
    // frame patch, in which case we'll trace stable entrypoint which 
    // should be set by now.
    //

    if (fromPatch)
    {
        trace->InitForStub(GetFunction()->GetStableEntryPoint());
    }
    else
    {
        trace->InitForStub(GetPreStubEntryPoint());
    }

    LOG((LF_CORDB, LL_INFO10000,
         "PrestubMethodFrame::TraceFrame: ip=" FMT_ADDR "\n", DBG_ADDR(trace->GetAddress()) ));
    
    return TRUE;
}

#ifndef DACCESS_COMPILE
//-----------------------------------------------------------------------
// A rather specialized routine for the exclusive use of StubDispatch.
//-----------------------------------------------------------------------
StubDispatchFrame::StubDispatchFrame(TransitionBlock * pTransitionBlock)
    : FramedMethodFrame(pTransitionBlock, NULL)
{
    LIMITED_METHOD_CONTRACT;

    m_pRepresentativeMT = NULL;
    m_representativeSlot = 0;

    m_pZapModule = NULL;
    m_pIndirection = NULL;

    m_pGCRefMap = NULL;
}

#endif // #ifndef DACCESS_COMPILE

MethodDesc* StubDispatchFrame::GetFunction()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    MethodDesc * pMD = m_pMD;

    if (m_pMD == NULL)
    {
        if (m_pRepresentativeMT != NULL)
        {
            pMD = m_pRepresentativeMT->GetMethodDescForSlot(m_representativeSlot);
#ifndef DACCESS_COMPILE
            m_pMD = pMD;
#endif
        }
    }

    return pMD;
}

static PTR_BYTE FindGCRefMap(PTR_Module pZapModule, TADDR ptr)
{
    LIMITED_METHOD_DAC_CONTRACT;

    PEImageLayout *pNativeImage = pZapModule->GetNativeOrReadyToRunImage();

    RVA rva = pNativeImage->GetDataRva(ptr);

    PTR_CORCOMPILE_IMPORT_SECTION pImportSection = pZapModule->GetImportSectionForRVA(rva);
    if (pImportSection == NULL)
        return NULL;

    COUNT_T index = (rva - pImportSection->Section.VirtualAddress) / pImportSection->EntrySize;

    PTR_BYTE pGCRefMap = dac_cast<PTR_BYTE>(pNativeImage->GetRvaData(pImportSection->AuxiliaryData));
    _ASSERTE(pGCRefMap != NULL);

    // GCRefMap starts with lookup index to limit size of linear scan that follows.
    PTR_BYTE p = pGCRefMap + dac_cast<PTR_DWORD>(pGCRefMap)[index / GCREFMAP_LOOKUP_STRIDE];
    COUNT_T remaining = index % GCREFMAP_LOOKUP_STRIDE;

    while (remaining > 0)
    {
        while ((*p & 0x80) != 0)
            p++;
        p++;

        remaining--;
    }

    return p;
}

PTR_BYTE StubDispatchFrame::GetGCRefMap()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PTR_BYTE pGCRefMap = m_pGCRefMap;

    if (pGCRefMap == NULL)
    {
        if (m_pIndirection != NULL)
        {
            if (m_pZapModule == NULL)
            {
                m_pZapModule = ExecutionManager::FindModuleForGCRefMap(m_pIndirection);
            }

            if (m_pZapModule != NULL)
            {
                pGCRefMap = FindGCRefMap(m_pZapModule, m_pIndirection);
            }

#ifndef DACCESS_COMPILE
            if (pGCRefMap != NULL)
            {
                m_pGCRefMap = pGCRefMap;
            }
            else
            {
                // Clear the indirection to avoid retrying
                m_pIndirection = NULL;
            }
#endif
        }
    }

    return pGCRefMap;
}

void StubDispatchFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    FramedMethodFrame::GcScanRoots(fn, sc);

    PTR_BYTE pGCRefMap = GetGCRefMap();
    if (pGCRefMap != NULL)
    { 
        PromoteCallerStackUsingGCRefMap(fn, sc, pGCRefMap);
    }
    else
    {
        PromoteCallerStack(fn, sc);
    }
}

BOOL StubDispatchFrame::TraceFrame(Thread *thread, BOOL fromPatch,
                                    TraceDestination *trace, REGDISPLAY *regs)
{
    WRAPPER_NO_CONTRACT;

    //
    // We want to set a frame patch, unless we're already at the
    // frame patch, in which case we'll trace stable entrypoint which 
    // should be set by now.
    //

    if (fromPatch)
    {
        trace->InitForStub(GetFunction()->GetStableEntryPoint());
    }
    else
    {
        trace->InitForStub(GetPreStubEntryPoint());
    }

    LOG((LF_CORDB, LL_INFO10000,
         "StubDispatchFrame::TraceFrame: ip=" FMT_ADDR "\n", DBG_ADDR(trace->GetAddress()) ));
    
    return TRUE;
}

Frame::Interception StubDispatchFrame::GetInterception()
{
    LIMITED_METHOD_CONTRACT;

    return INTERCEPTION_NONE;
}

#ifndef DACCESS_COMPILE
ExternalMethodFrame::ExternalMethodFrame(TransitionBlock * pTransitionBlock)
    : FramedMethodFrame(pTransitionBlock, NULL)
{
    LIMITED_METHOD_CONTRACT;

    m_pIndirection = NULL;
    m_pZapModule = NULL;

    m_pGCRefMap = NULL;
}
#endif // !DACCESS_COMPILE

void ExternalMethodFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    FramedMethodFrame::GcScanRoots(fn, sc);
    PromoteCallerStackUsingGCRefMap(fn, sc, GetGCRefMap());
}

PTR_BYTE ExternalMethodFrame::GetGCRefMap()
{
    LIMITED_METHOD_DAC_CONTRACT;

    PTR_BYTE pGCRefMap = m_pGCRefMap;

    if (pGCRefMap == NULL)
    {
        if (m_pIndirection != NULL)
        {
            pGCRefMap = FindGCRefMap(m_pZapModule, m_pIndirection);
#ifndef DACCESS_COMPILE
            m_pGCRefMap = pGCRefMap;
#endif
        }
    }

    _ASSERTE(pGCRefMap != NULL);
    return pGCRefMap;
}

Frame::Interception ExternalMethodFrame::GetInterception()
{
    LIMITED_METHOD_CONTRACT;

    return INTERCEPTION_NONE;
}

Frame::Interception PrestubMethodFrame::GetInterception()
{
    LIMITED_METHOD_DAC_CONTRACT;

    //
    // The only direct kind of interception done by the prestub 
    // is class initialization.
    //

    return INTERCEPTION_PRESTUB;
}

#ifdef FEATURE_READYTORUN

#ifndef DACCESS_COMPILE
DynamicHelperFrame::DynamicHelperFrame(TransitionBlock * pTransitionBlock, int dynamicHelperFrameFlags)
    : FramedMethodFrame(pTransitionBlock, NULL)
{
    LIMITED_METHOD_CONTRACT;

    m_dynamicHelperFrameFlags = dynamicHelperFrameFlags;
}
#endif // !DACCESS_COMPILE

void DynamicHelperFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    FramedMethodFrame::GcScanRoots(fn, sc);

    PTR_PTR_Object pArgumentRegisters = dac_cast<PTR_PTR_Object>(GetTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters());

    if (m_dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg)
    {
        TADDR pArgument = GetTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters();
#ifdef _TARGET_X86_
        // x86 is special as always
        pArgument += offsetof(ArgumentRegisters, ECX);
#endif
        (*fn)(dac_cast<PTR_PTR_Object>(pArgument), sc, CHECK_APP_DOMAIN);
    }

    if (m_dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg2)
    {
        TADDR pArgument = GetTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters();
#ifdef _TARGET_X86_
        // x86 is special as always
        pArgument += offsetof(ArgumentRegisters, EDX);
#else
        pArgument += sizeof(TADDR);
#endif
        (*fn)(dac_cast<PTR_PTR_Object>(pArgument), sc, CHECK_APP_DOMAIN);
    }
}

#endif // FEATURE_READYTORUN


#ifndef DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
//-----------------------------------------------------------------------
// A rather specialized routine for the exclusive use of the COM PreStub.
//-----------------------------------------------------------------------
VOID
ComPrestubMethodFrame::Init()
{
    WRAPPER_NO_CONTRACT;

    // Initializes the frame's VPTR. This assumes C++ puts the vptr
    // at offset 0 for a class not using MI, but this is no different
    // than the assumption that COM Classic makes.
    *((TADDR*)this) = GetMethodFrameVPtr();
    *GetGSCookiePtr() = GetProcessGSCookie();
}
#endif // FEATURE_COMINTEROP

//-----------------------------------------------------------------------
// GCFrames
//-----------------------------------------------------------------------


//--------------------------------------------------------------------
// This constructor pushes a new GCFrame on the frame chain.
//--------------------------------------------------------------------
GCFrame::GCFrame(OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    Init(GetThread(), pObjRefs, numObjRefs, maybeInterior);
}

GCFrame::GCFrame(Thread *pThread, OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    Init(pThread, pObjRefs, numObjRefs, maybeInterior);
}

void GCFrame::Init(Thread *pThread, OBJECTREF *pObjRefs, UINT numObjRefs, BOOL maybeInterior)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifdef USE_CHECKED_OBJECTREFS
    if (!maybeInterior) {
        UINT i;
        for(i = 0; i < numObjRefs; i++)
            Thread::ObjectRefProtected(&pObjRefs[i]);
        
        for (i = 0; i < numObjRefs; i++) {
            pObjRefs[i].Validate();
        }
    }

#if 0  // We'll want to restore this goodness check at some time. For now, the fact that we use
       // this as temporary backstops in our loader exception conversions means we're highly
       // exposed to infinite stack recursion should the loader be invoked during a stackwalk.
       // So we'll do without.

    if (g_pConfig->GetGCStressLevel() != 0 && IsProtectedByGCFrame(pObjRefs)) {
        _ASSERTE(!"This objectref is already protected by a GCFrame. Protecting it twice will corrupt the GC.");
    }
#endif

#endif

    m_pObjRefs      = pObjRefs;
    m_numObjRefs    = numObjRefs;
    m_pCurThread    = pThread;
    m_MaybeInterior = maybeInterior;

    Frame::Push(m_pCurThread);
}


//
// GCFrame Object Scanning
//
// This handles scanning/promotion of GC objects that were
// protected by the programmer explicitly protecting it in a GC Frame
// via the GCPROTECTBEGIN / GCPROTECTEND facility...
//

#endif // !DACCESS_COMPILE

void GCFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    PTR_PTR_Object pRefs = dac_cast<PTR_PTR_Object>(m_pObjRefs);

    for (UINT i = 0;i < m_numObjRefs; i++)  {

        LOG((LF_GC, INFO3, "GC Protection Frame Promoting" FMT_ADDR "to",
             DBG_ADDR(OBJECTREF_TO_UNCHECKED_OBJECTREF(m_pObjRefs[i])) ));
        if (m_MaybeInterior)
            PromoteCarefully(fn, pRefs + i, sc, GC_CALL_INTERIOR|CHECK_APP_DOMAIN);
        else
            (*fn)(pRefs + i, sc, 0);
        LOG((LF_GC, INFO3, FMT_ADDR "\n", DBG_ADDR(OBJECTREF_TO_UNCHECKED_OBJECTREF(m_pObjRefs[i])) ));
    }
}

#ifdef FEATURE_REMOTING
#include "objectclone.h"
void GCSafeCollectionFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;
    
    PTR_GCSafeCollection collection = dac_cast<PTR_GCSafeCollection>(m_pCollection);
    collection->ReportGCRefs(fn, sc);
}

#ifndef DACCESS_COMPILE
GCSafeCollectionFrame::GCSafeCollectionFrame(void *collection)
{
    WRAPPER_NO_CONTRACT;
    
    _ASSERTE(collection != NULL);
    m_pCollection = collection;

    Frame::Push();
}

VOID GCSafeCollectionFrame::Pop()
{
    LIMITED_METHOD_CONTRACT;
    Frame::Pop();
}
#endif // !DACCESS_COMPILE
#endif // FEATURE_REMOTING

#ifndef DACCESS_COMPILE
//--------------------------------------------------------------------
// Pops the GCFrame and cancels the GC protection.
//--------------------------------------------------------------------
VOID GCFrame::Pop()
{
    WRAPPER_NO_CONTRACT;

    Frame::Pop(m_pCurThread);
#ifdef _DEBUG
    m_pCurThread->EnableStressHeap();
    for(UINT i = 0; i < m_numObjRefs; i++)
        Thread::ObjectRefNew(&m_pObjRefs[i]);       // Unprotect them
#endif
}

#ifdef FEATURE_INTERPRETER
// Methods of IntepreterFrame.
InterpreterFrame::InterpreterFrame(Interpreter* interp) 
  : Frame(), m_interp(interp)
{
    Push();
}


MethodDesc* InterpreterFrame::GetFunction()
{
    return m_interp->GetMethodDesc();
}

void InterpreterFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    return m_interp->GCScanRoots(fn, sc);
}

#endif // FEATURE_INTERPRETER

#if defined(_DEBUG) && !defined (DACCESS_COMPILE)

struct IsProtectedByGCFrameStruct
{
    OBJECTREF       *ppObjectRef;
    UINT             count;
};

static StackWalkAction IsProtectedByGCFrameStackWalkFramesCallback(
    CrawlFrame      *pCF,
    VOID            *pData
)
{
    DEBUG_ONLY_FUNCTION;
    WRAPPER_NO_CONTRACT;

    IsProtectedByGCFrameStruct *pd = (IsProtectedByGCFrameStruct*)pData;
    Frame *pFrame = pCF->GetFrame();
    if (pFrame) {
        if (pFrame->Protects(pd->ppObjectRef)) {
            pd->count++;
        }
    }
    return SWA_CONTINUE;
}

BOOL IsProtectedByGCFrame(OBJECTREF *ppObjectRef)
{
    DEBUG_ONLY_FUNCTION;
    WRAPPER_NO_CONTRACT;

    // Just report TRUE if GCStress is not on.  This satisfies the asserts that use this
    // code without the cost of actually determining it.
    if (!GCStress<cfg_any>::IsEnabled())
        return TRUE;

    if (ppObjectRef == NULL) {
        return TRUE;
    }

    CONTRACT_VIOLATION(ThrowsViolation);
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE ();
    IsProtectedByGCFrameStruct d = {ppObjectRef, 0};
    GetThread()->StackWalkFrames(IsProtectedByGCFrameStackWalkFramesCallback, &d);
    if (d.count > 1) {
        _ASSERTE(!"Multiple GCFrames protecting the same pointer. This will cause GC corruption!");
    }
    return d.count != 0;
}
#endif // _DEBUG

#endif //!DACCESS_COMPILE

void ProtectByRefsFrame::GcScanRoots(promote_func *fn, ScanContext *sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    ByRefInfo *pByRefInfos = m_brInfo;
    while (pByRefInfos)
    {
        if (!CorIsPrimitiveType(pByRefInfos->typ))
        {
            TADDR pData = PTR_HOST_MEMBER_TADDR(ByRefInfo, pByRefInfos, data);

            if (pByRefInfos->typeHandle.IsValueType())
            {
                ReportPointersFromValueType(fn, sc, pByRefInfos->typeHandle.GetMethodTable(), PTR_VOID(pData));
            }
            else
            {
                PTR_PTR_Object ppObject = PTR_PTR_Object(pData);

                LOG((LF_GC, INFO3, "ProtectByRefs Frame Promoting" FMT_ADDR "to ", DBG_ADDR(*ppObject)));

                (*fn)(ppObject, sc, CHECK_APP_DOMAIN);

                LOG((LF_GC, INFO3, FMT_ADDR "\n", DBG_ADDR(*ppObject) ));
            }
        }
        pByRefInfos = pByRefInfos->pNext;
    }
}

void ProtectValueClassFrame::GcScanRoots(promote_func *fn, ScanContext *sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    ValueClassInfo *pVCInfo = m_pVCInfo;
    while (pVCInfo != NULL)
    {
        _ASSERTE(pVCInfo->pMT->IsValueType());
        ReportPointersFromValueType(fn, sc, pVCInfo->pMT, pVCInfo->pData);
        pVCInfo = pVCInfo->pNext;
    }
}

//
// Promote Caller Stack
//
//

void TransitionFrame::PromoteCallerStack(promote_func* fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;
    
    // I believe this is the contract:
    //CONTRACTL
    //{
    //    INSTANCE_CHECK;
    //    NOTHROW;
    //    GC_NOTRIGGER;
    //    FORBID_FAULT;
    //    MODE_ANY;
    //}
    //CONTRACTL_END
    
    MethodDesc *pFunction;
    
    LOG((LF_GC, INFO3, "    Promoting method caller Arguments\n" ));

    // We're going to have to look at the signature to determine
    // which arguments a are pointers....First we need the function
    pFunction = GetFunction();
    if (pFunction == NULL)
        return;

    // Now get the signature...
    Signature callSignature = pFunction->GetSignature();
    if (callSignature.IsEmpty())
    {
        return;
    }

    //If not "vararg" calling convention, assume "default" calling convention
    if (!MetaSig::IsVarArg(pFunction->GetModule(), callSignature))
    {
        MetaSig msig(pFunction);
        PromoteCallerStackHelper (fn, sc, pFunction, &msig);
    }
    else
    {
        VASigCookie *varArgSig = GetVASigCookie();

        //Note: no instantiations needed for varargs
        MetaSig msig(varArgSig->signature,
                     varArgSig->pModule,
                     NULL);
        PromoteCallerStackHelper (fn, sc, pFunction, &msig);
    }
}

void TransitionFrame::PromoteCallerStackHelper(promote_func* fn, ScanContext* sc,
                                                 MethodDesc *pFunction, MetaSig *pmsig)
{
    WRAPPER_NO_CONTRACT;
    // I believe this is the contract:
    //CONTRACTL
    //{
    //    INSTANCE_CHECK;
    //    NOTHROW;
    //    GC_NOTRIGGER;
    //    FORBID_FAULT;
    //    MODE_ANY;
    //}
    //CONTRACTL_END

    ArgIterator argit(pmsig);

    TADDR pTransitionBlock = GetTransitionBlock();

    // promote 'this' for non-static methods
    if (argit.HasThis() && pFunction != NULL)
    {
        BOOL interior = pFunction->GetMethodTable()->IsValueType() && !pFunction->IsUnboxingStub();

        PTR_PTR_VOID pThis = dac_cast<PTR_PTR_VOID>(pTransitionBlock + argit.GetThisOffset());
        LOG((LF_GC, INFO3, 
             "    'this' Argument at " FMT_ADDR "promoted from" FMT_ADDR "\n", 
             DBG_ADDR(pThis), DBG_ADDR(*pThis) ));

        if (interior)
            PromoteCarefully(fn, PTR_PTR_Object(pThis), sc, GC_CALL_INTERIOR|CHECK_APP_DOMAIN);
        else
            (fn)(PTR_PTR_Object(pThis), sc, CHECK_APP_DOMAIN);
    }

    if (argit.HasRetBuffArg())
    {
        PTR_PTR_VOID pRetBuffArg = dac_cast<PTR_PTR_VOID>(pTransitionBlock + argit.GetRetBuffArgOffset());
        LOG((LF_GC, INFO3, "    ret buf Argument promoted from" FMT_ADDR "\n", DBG_ADDR(*pRetBuffArg) ));
        PromoteCarefully(fn, PTR_PTR_Object(pRetBuffArg), sc, GC_CALL_INTERIOR|CHECK_APP_DOMAIN);
    }

    int argOffset;
    while ((argOffset = argit.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        ArgDestination argDest(dac_cast<PTR_VOID>(pTransitionBlock), argOffset, argit.GetArgLocDescForStructInRegs());
        pmsig->GcScanRoots(&argDest, fn, sc);
    }
}

#ifdef _TARGET_X86_
UINT TransitionFrame::CbStackPopUsingGCRefMap(PTR_BYTE pGCRefMap)
{
    LIMITED_METHOD_CONTRACT;

    GCRefMapDecoder decoder(pGCRefMap);
    return decoder.ReadStackPop() * sizeof(TADDR);
}
#endif

void TransitionFrame::PromoteCallerStackUsingGCRefMap(promote_func* fn, ScanContext* sc, PTR_BYTE pGCRefMap)
{
    WRAPPER_NO_CONTRACT;

    GCRefMapDecoder decoder(pGCRefMap);

#ifdef _TARGET_X86_
    // Skip StackPop
    decoder.ReadStackPop();
#endif

    TADDR pTransitionBlock = GetTransitionBlock();

    while (!decoder.AtEnd())
    {
        int pos = decoder.CurrentPos();
        int token = decoder.ReadToken();

        int ofs;

#ifdef _TARGET_X86_
        ofs = (pos < NUM_ARGUMENT_REGISTERS) ?
            (TransitionBlock::GetOffsetOfArgumentRegisters() + ARGUMENTREGISTERS_SIZE - (pos + 1) * sizeof(TADDR)) :
            (TransitionBlock::GetOffsetOfArgs() + (pos - NUM_ARGUMENT_REGISTERS) * sizeof(TADDR));
#else
        ofs = TransitionBlock::GetOffsetOfArgumentRegisters() + pos * sizeof(TADDR);
#endif

        PTR_TADDR ppObj = dac_cast<PTR_TADDR>(pTransitionBlock + ofs);

        switch (token)
        {
        case GCREFMAP_SKIP:
            break;
        case GCREFMAP_REF:
            fn(dac_cast<PTR_PTR_Object>(ppObj), sc, CHECK_APP_DOMAIN);
            break;
        case GCREFMAP_INTERIOR:
            PromoteCarefully(fn, dac_cast<PTR_PTR_Object>(ppObj), sc, GC_CALL_INTERIOR | GC_CALL_CHECK_APP_DOMAIN);
            break;
        case GCREFMAP_METHOD_PARAM:
            if (sc->promotion)
            {
#ifndef DACCESS_COMPILE
                MethodDesc *pMDReal = dac_cast<PTR_MethodDesc>(*ppObj);
                if (pMDReal != NULL)
                    GcReportLoaderAllocator(fn, sc, pMDReal->GetLoaderAllocator());
#endif
            }
            break;
        case GCREFMAP_TYPE_PARAM:
            if (sc->promotion)
            {
#ifndef DACCESS_COMPILE
                MethodTable *pMTReal = dac_cast<PTR_MethodTable>(*ppObj);
                if (pMTReal != NULL)
                    GcReportLoaderAllocator(fn, sc, pMTReal->GetLoaderAllocator());
#endif
            }
            break;
        case GCREFMAP_VASIG_COOKIE:
            {
                VASigCookie *varArgSig = dac_cast<PTR_VASigCookie>(*ppObj);

                //Note: no instantiations needed for varargs
                MetaSig msig(varArgSig->signature,
                                varArgSig->pModule,
                                NULL);
                PromoteCallerStackHelper (fn, sc, NULL, &msig);
            }
            break;
        default:
            _ASSERTE(!"Unknown GCREFMAP token");
            break;
        }
    }
}

void PInvokeCalliFrame::PromoteCallerStack(promote_func* fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, INFO3, "    Promoting CALLI caller Arguments\n" ));

    // get the signature
    VASigCookie *varArgSig = GetVASigCookie();
    if (varArgSig->signature.IsEmpty())
    {
        return;
    }

    // no instantiations needed for varargs 
    MetaSig msig(varArgSig->signature,
                 varArgSig->pModule, 
                 NULL);
    PromoteCallerStackHelper(fn, sc, NULL, &msig);
}

#ifndef DACCESS_COMPILE
PInvokeCalliFrame::PInvokeCalliFrame(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget)
    : FramedMethodFrame(pTransitionBlock, NULL)
{
    LIMITED_METHOD_CONTRACT;

    m_pVASigCookie = pVASigCookie;
    m_pUnmanagedTarget = pUnmanagedTarget;
}
#endif // #ifndef DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE
ComPlusMethodFrame::ComPlusMethodFrame(TransitionBlock * pTransitionBlock, MethodDesc * pMD)
    : FramedMethodFrame(pTransitionBlock, pMD)
{
    LIMITED_METHOD_CONTRACT;
}
#endif // #ifndef DACCESS_COMPILE

//virtual
void ComPlusMethodFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    // ComPlusMethodFrame is only used in the event call / late bound call code path where we do not have IL stub
    // so we need to promote the arguments and return value manually.

    FramedMethodFrame::GcScanRoots(fn, sc);
    PromoteCallerStack(fn, sc);

    MetaSig::RETURNTYPE returnType = GetFunction()->ReturnsObject();

    // Promote the returned object
    if(returnType == MetaSig::RETOBJ)
        (*fn)(GetReturnObjectPtr(), sc, CHECK_APP_DOMAIN);
    else if (returnType == MetaSig::RETBYREF)
        PromoteCarefully(fn, GetReturnObjectPtr(), sc, GC_CALL_INTERIOR|CHECK_APP_DOMAIN);
}
#endif // FEATURE_COMINTEROP

#if defined (_DEBUG) && !defined (DACCESS_COMPILE)
// For IsProtectedByGCFrame, we need to know whether a given object ref is protected 
// by a ComPlusMethodFrame or a ComMethodFrame. Since GCScanRoots for those frames are 
// quite complicated, we don't want to duplicate their logic so we call GCScanRoots with 
// IsObjRefProtected (a fake promote function) and an extended ScanContext to do the checking.

struct IsObjRefProtectedScanContext : public ScanContext
{
    OBJECTREF * oref_to_check;
    BOOL        oref_protected;
    IsObjRefProtectedScanContext (OBJECTREF * oref)
    {
        thread_under_crawl = GetThread ();
        promotion = TRUE;
        oref_to_check = oref;
        oref_protected = FALSE;
    }
};

void IsObjRefProtected (Object** ppObj, ScanContext* sc, uint32_t)
{
    LIMITED_METHOD_CONTRACT;
    IsObjRefProtectedScanContext * orefProtectedSc = (IsObjRefProtectedScanContext *)sc;
    if (ppObj == (Object **)(orefProtectedSc->oref_to_check))
        orefProtectedSc->oref_protected = TRUE;
}

BOOL TransitionFrame::Protects(OBJECTREF * ppORef)
{
    WRAPPER_NO_CONTRACT;
    IsObjRefProtectedScanContext sc (ppORef);
    // Set the stack limit for the scan to the SP of the managed frame above the transition frame
    sc.stack_limit = GetSP();
    GcScanRoots (IsObjRefProtected, &sc);
    return sc.oref_protected;
}
#endif //defined (_DEBUG) && !defined (DACCESS_COMPILE)

//+----------------------------------------------------------------------------
//
//  Method:     TPMethodFrame::GcScanRoots    public
//
//  Synopsis:   GC protects arguments on the stack
//

//
//+----------------------------------------------------------------------------
#ifdef FEATURE_REMOTING

#ifndef DACCESS_COMPILE
TPMethodFrame::TPMethodFrame(TransitionBlock * pTransitionBlock)
    : FramedMethodFrame(pTransitionBlock, NULL)
{
    LIMITED_METHOD_CONTRACT;
}
#endif // #ifndef DACCESS_COMPILE

void TPMethodFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    // Delegate to FramedMethodFrame
    FramedMethodFrame::GcScanRoots(fn, sc);
    FramedMethodFrame::PromoteCallerStack(fn, sc);

    MetaSig::RETURNTYPE returnType = GetFunction()->ReturnsObject();

    // Promote the returned object
    if(returnType == MetaSig::RETOBJ)
    {
        (*fn)(GetReturnObjectPtr(), sc, CHECK_APP_DOMAIN);
    }
    else if (returnType == MetaSig::RETBYREF)
    {
        PromoteCarefully(fn, GetReturnObjectPtr(), sc, GC_CALL_INTERIOR|CHECK_APP_DOMAIN);
    }

    return;
}

//+----------------------------------------------------------------------------
//
//  Method:     TPMethodFrame::TraceFrame    public
//
//  Synopsis:   Return where the frame will execute next - the result is filled
//              into the given "trace" structure.  The frame is responsible for
//              detecting where it is in its execution lifetime.
//
//

//
//+----------------------------------------------------------------------------
BOOL TPMethodFrame::TraceFrame(Thread *thread, BOOL fromPatch, 
                               TraceDestination *trace, REGDISPLAY *regs)
{
    WRAPPER_NO_CONTRACT;

    // We want to set a frame patch, unless we're already at the
    // frame patch, in which case we'll trace stable entrypoint which 
    // should be set by now.

    if (fromPatch)
    {
        trace->InitForStub(GetFunction()->GetStableEntryPoint());
    }
    else
    {
#ifdef DACCESS_COMPILE
        DacNotImpl();
#else
        trace->InitForStub(GetEEFuncEntryPoint(TransparentProxyStubPatchLabel));
#endif
    }
    return TRUE;

}
#endif // FEATURE_REMOTING

#ifdef FEATURE_COMINTEROP

#ifdef _TARGET_X86_
// Return the # of stack bytes pushed by the unmanaged caller.
UINT ComMethodFrame::GetNumCallerStackBytes()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    ComCallMethodDesc* pCMD = PTR_ComCallMethodDesc((TADDR)GetDatum());
    PREFIX_ASSUME(pCMD != NULL);
    // assumes __stdcall
    // compute the callee pop stack bytes
    return pCMD->GetNumStackBytes();
}
#endif // _TARGET_X86_

#ifndef DACCESS_COMPILE
void ComMethodFrame::DoSecondPassHandlerCleanup(Frame * pCurFrame)
{
    LIMITED_METHOD_CONTRACT;

    // Find ComMethodFrame, noting any ContextTransitionFrame along the way

    while ((pCurFrame != FRAME_TOP) && 
           (pCurFrame->GetVTablePtr() != ComMethodFrame::GetMethodFrameVPtr()))
    {
        if (pCurFrame->GetVTablePtr() == ContextTransitionFrame::GetMethodFrameVPtr())
        {
            // If there is a context transition before we find a ComMethodFrame, do nothing.  Expect that 
            // the AD transition code will perform the corresponding work after it pops its context 
            // transition frame and before it rethrows the exception.
            return;
        }
        pCurFrame = pCurFrame->PtrNextFrame();
    }

    if (pCurFrame == FRAME_TOP)
        return;

    ComMethodFrame * pComMethodFrame = (ComMethodFrame *)pCurFrame;

    _ASSERTE(pComMethodFrame != NULL);
    Thread * pThread = GetThread();
    GCX_COOP_THREAD_EXISTS(pThread);
    // Unwind the frames till the entry frame (which was ComMethodFrame)
    pCurFrame = pThread->GetFrame();
    while ((pCurFrame != NULL) && (pCurFrame <= pComMethodFrame))
    {
        pCurFrame->ExceptionUnwind();
        pCurFrame = pCurFrame->PtrNextFrame();
    }

    // At this point, pCurFrame would be the ComMethodFrame's predecessor frame
    // that we need to reset to.
    _ASSERTE((pCurFrame != NULL) && (pComMethodFrame->PtrNextFrame() == pCurFrame));
    pThread->SetFrame(pCurFrame);
}
#endif // !DACCESS_COMPILE

#endif // FEATURE_COMINTEROP


#ifdef _TARGET_X86_

PTR_UMEntryThunk UMThkCallFrame::GetUMEntryThunk()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<PTR_UMEntryThunk>(GetDatum());
}

#ifdef DACCESS_COMPILE
void UMThkCallFrame::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    UnmanagedToManagedFrame::EnumMemoryRegions(flags);

    // Pieces of the UMEntryThunk need to be saved.
    UMEntryThunk *pThunk = GetUMEntryThunk();
    DacEnumMemoryRegion(dac_cast<TADDR>(pThunk), sizeof(UMEntryThunk));        
    
    UMThunkMarshInfo *pMarshInfo = pThunk->GetUMThunkMarshInfo();
    DacEnumMemoryRegion(dac_cast<TADDR>(pMarshInfo), sizeof(UMThunkMarshInfo));        
}
#endif

#endif // _TARGET_X86_

#ifndef DACCESS_COMPILE

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("y", on)   // Small critical routines, don't put in EBP frame 
#endif

// Initialization of HelperMethodFrame.
void HelperMethodFrame::Push()
{
    CONTRACTL {
        if (m_Attribs & FRAME_ATTR_NO_THREAD_ABORT) NOTHROW; else THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    //
    // Finish initialization
    //

    // Compiler would not inline GetGSCookiePtr() because of it is virtual method.
    // Inline it manually and verify that it gives same result.
    _ASSERTE(GetGSCookiePtr() == (((GSCookie *)(this)) - 1));
    *(((GSCookie *)(this)) - 1) = GetProcessGSCookie();

    _ASSERTE(!m_MachState.isValid());

    Thread * pThread = ::GetThread();
    m_pThread = pThread;

    // Push the frame
    Frame::Push(pThread);

    if (!pThread->HasThreadStateOpportunistic((Thread::ThreadState)(Thread::TS_YieldRequested | Thread::TS_AbortRequested)))
        return;

    // Outline the slow path for better perf
    PushSlowHelper();
}

void HelperMethodFrame::Pop()
{
    CONTRACTL {
        if (m_Attribs & FRAME_ATTR_NO_THREAD_ABORT) NOTHROW; else THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    Thread * pThread = m_pThread;
    
    if ((m_Attribs & FRAME_ATTR_NO_THREAD_ABORT) || !pThread->HasThreadStateOpportunistic(Thread::TS_AbortInitiated))
    {
        Frame::Pop(pThread);
        return;
    }

    // Outline the slow path for better perf
    PopSlowHelper();
}

#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("", on)     // Go back to command line default optimizations
#endif

NOINLINE void HelperMethodFrame::PushSlowHelper()
{
    CONTRACTL {
        if (m_Attribs & FRAME_ATTR_NO_THREAD_ABORT) NOTHROW; else THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    if (!(m_Attribs & FRAME_ATTR_NO_THREAD_ABORT))
    {
        if (m_pThread->IsAbortRequested())
        {
            m_pThread->HandleThreadAbort();
        }

    }

    if (m_pThread->IsYieldRequested())
    {
        __SwitchToThread(0, CALLER_LIMITS_SPINNING);
    }
}

NOINLINE void HelperMethodFrame::PopSlowHelper()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    m_pThread->HandleThreadAbort();
    Frame::Pop(m_pThread);
}

#endif // #ifndef DACCESS_COMPILE

MethodDesc* HelperMethodFrame::GetFunction()
{
    WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
    InsureInit(false, NULL);
    return m_pMD;
#else
    if (m_MachState.isValid())
    {
        return m_pMD;
    }
    else
    {
        return ECall::MapTargetBackToMethod(m_FCallEntry);
    }
#endif
}

//---------------------------------------------------------------------------------------
//
// Ensures the HelperMethodFrame gets initialized, if not already.
//
// Arguments:
//      * initialInit -
//         * true: ensure the simple, first stage of initialization has been completed.
//             This is used when the HelperMethodFrame is first created.
//         * false: complete any initialization that was left to do, if any.
//      * unwindState - [out] DAC builds use this to return the unwound machine state.
//      * hostCallPreference - (See code:HelperMethodFrame::HostCallPreference.)
//
// Return Value:
//     Normally, the function always returns TRUE meaning the initialization succeeded.
//     
//     However, if hostCallPreference is NoHostCalls, AND if a callee (like
//     LazyMachState::unwindLazyState) needed to acquire a JIT reader lock and was unable
//     to do so (lest it re-enter the host), then InsureInit will abort and return FALSE.
//     So any callers that specify hostCallPreference = NoHostCalls (which is not the
//     default), should check for FALSE return, and refuse to use the HMF in that case.
//     Currently only asynchronous calls made by profilers use that code path.
//

BOOL HelperMethodFrame::InsureInit(bool initialInit,
                                    MachState * unwindState,
                                    HostCallPreference hostCallPreference /* = AllowHostCalls */) 
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        if ((hostCallPreference == AllowHostCalls) && !m_MachState.isValid()) { HOST_CALLS; } else { HOST_NOCALLS; }
        SUPPORTS_DAC;
    } CONTRACTL_END;

    if (m_MachState.isValid())
    {
        return TRUE;
    }

    _ASSERTE(m_Attribs != 0xCCCCCCCC);

#ifndef DACCESS_COMPILE
    if (!initialInit)
    {
        m_pMD = ECall::MapTargetBackToMethod(m_FCallEntry);

        // if this is an FCall, we should find it
        _ASSERTE(m_FCallEntry == 0 || m_pMD != 0);
    }
#endif

    // Because TRUE FCalls can be called from via reflection, com-interop, etc.,
    // we can't rely on the fact that we are called from jitted code to find the
    // caller of the FCALL.   Thus FCalls must erect the frame directly in the
    // FCall.  For JIT helpers, however, we can rely on this, and so they can
    // be sneakier and defer the HelperMethodFrame setup to a called worker method.
   
    // Work with a copy so that we only write the values once.
    // this avoids race conditions.
    LazyMachState* lazy = &m_MachState;
    DWORD threadId = m_pThread->GetOSThreadId();
    MachState unwound;
    
    if (!initialInit &&
        m_FCallEntry == 0 &&
        !(m_Attribs & Frame::FRAME_ATTR_EXACT_DEPTH)) // Jit Helper
    {
        LazyMachState::unwindLazyState(
            lazy, 
            &unwound, 
            threadId,
            0,
            hostCallPreference);

#if !defined(DACCESS_COMPILE)
        if (!unwound.isValid())
        {
            // This only happens if LazyMachState::unwindLazyState had to abort as a
            // result of failing to take a reader lock (because we told it not to yield,
            // but the writer lock was already held).  Since we've not yet updated
            // m_MachState, this HelperMethodFrame will still be considered not fully
            // initialized (so a future call into InsureInit() will attempt to complete
            // initialization again).
            // 
            // Note that, in DAC builds, the contract with LazyMachState::unwindLazyState
            // is a bit different, and it's expected that LazyMachState::unwindLazyState
            // will commonly return an unwound state with _pRetAddr==NULL (which counts
            // as an "invalid" MachState). So have DAC builds deliberately fall through
            // rather than aborting when unwound is invalid.
            _ASSERTE(hostCallPreference == NoHostCalls);
            return FALSE;
        }
#endif // !defined(DACCESS_COMPILE)
    }
    else if (!initialInit &&
             (m_Attribs & Frame::FRAME_ATTR_CAPTURE_DEPTH_2) != 0)
    {
        // explictly told depth
        LazyMachState::unwindLazyState(lazy, &unwound, threadId, 2);
    }
    else
    {
        // True FCall 
        LazyMachState::unwindLazyState(lazy, &unwound, threadId, 1);
    }

    _ASSERTE(unwound.isValid());

#if !defined(DACCESS_COMPILE)
    lazy->setLazyStateFromUnwind(&unwound);
#else  // DACCESS_COMPILE
    if (unwindState)
    {
        *unwindState = unwound;
    }
#endif // DACCESS_COMPILE

    return TRUE;
}


#include "comdelegate.h"

Assembly* SecureDelegateFrame::GetAssembly()
{
    WRAPPER_NO_CONTRACT;

#if !defined(DACCESS_COMPILE)
    // obtain the frame off the delegate pointer
    DELEGATEREF delegate = (DELEGATEREF) GetThis();
    _ASSERTE(delegate);
    if (!delegate->IsWrapperDelegate())
    {
        MethodDesc* pMethod = (MethodDesc*) delegate->GetMethodPtrAux();
        Assembly* pAssembly = pMethod->GetAssembly();
        _ASSERTE(pAssembly != NULL);
        return pAssembly;        
    }
    else
        return NULL;
#else
    DacNotImpl();
    return NULL;
#endif
}

BOOL SecureDelegateFrame::TraceFrame(Thread *thread, BOOL fromPatch, TraceDestination *trace, REGDISPLAY *regs)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(!fromPatch);

    // Unlike multicast delegates, secure delegates only call one method.  So, we should just return false here
    // and let the step out logic continue to the caller of the secure delegate stub.
    LOG((LF_CORDB, LL_INFO1000, "SDF::TF: return FALSE\n"));

    return FALSE;
}

BOOL MulticastFrame::TraceFrame(Thread *thread, BOOL fromPatch,
                                TraceDestination *trace, REGDISPLAY *regs)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(!fromPatch);

#ifdef DACCESS_COMPILE
    return FALSE;

#else // !DACCESS_COMPILE
    LOG((LF_CORDB,LL_INFO10000, "MulticastFrame::TF FromPatch:0x%x, at 0x%x\n", fromPatch, GetControlPC(regs)));

    // At this point we have no way to recover the Stub object from the control pc.  We can't use the MD stored
    // in the MulticastFrame because it points to the dummy Invoke() method, not the method we want to call.

    BYTE *pbDel = NULL;
    int delegateCount = 0;

#if defined(_TARGET_X86_)
    // At this point the counter hasn't been incremented yet.
    delegateCount = *regs->pEdi + 1;
    pbDel = *(BYTE **)( (size_t)*(regs->pEsi) + GetOffsetOfTransitionBlock() + ArgIterator::GetThisOffset());
#elif defined(_TARGET_AMD64_)
    // At this point the counter hasn't been incremented yet.
    delegateCount = (int)regs->pCurrentContext->Rdi + 1;
    pbDel = *(BYTE **)( (size_t)(regs->pCurrentContext->Rsi) + GetOffsetOfTransitionBlock() + ArgIterator::GetThisOffset());
#elif defined(_TARGET_ARM_)
    // At this point the counter has not yet been incremented. Counter is in R7, frame pointer in R4.
    delegateCount = regs->pCurrentContext->R7 + 1;
    pbDel = *(BYTE **)( (size_t)(regs->pCurrentContext->R4) + GetOffsetOfTransitionBlock() + ArgIterator::GetThisOffset());
#else
    delegateCount = 0;
    PORTABILITY_ASSERT("MulticastFrame::TraceFrame (frames.cpp)");
#endif

    int totalDelegateCount = (int)*(size_t*)(pbDel + DelegateObject::GetOffsetOfInvocationCount());

    _ASSERTE( COMDelegate::IsTrueMulticastDelegate( ObjectToOBJECTREF((Object*)pbDel) ) );

    if (delegateCount == totalDelegateCount)
    {
        LOG((LF_CORDB, LL_INFO1000, "MF::TF: Executed all stubs, should return\n"));
        // We've executed all the stubs, so we should return
        return FALSE;
    }
    else
    {
        // We're going to execute stub delegateCount next, so go and grab it.
        BYTE *pbDelInvocationList = *(BYTE **)(pbDel + DelegateObject::GetOffsetOfInvocationList());

        pbDel = *(BYTE**)( ((ArrayBase *)pbDelInvocationList)->GetDataPtr() +
                           ((ArrayBase *)pbDelInvocationList)->GetComponentSize()*delegateCount);

        _ASSERTE(pbDel);
        return DelegateInvokeStubManager::TraceDelegateObject(pbDel, trace);
    }
#endif // !DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

VOID InlinedCallFrame::Init()
{
    WRAPPER_NO_CONTRACT;

    *((TADDR *)this) = GetMethodFrameVPtr();
    
    // GetGSCookiePtr contains a virtual call and this is a perf critical method so we don't want to call it in ret builds
    GSCookie *ptrGS = (GSCookie *)((BYTE *)this - sizeof(GSCookie));
    _ASSERTE(ptrGS == GetGSCookiePtr());

    *ptrGS = GetProcessGSCookie();

    m_Datum = NULL;
    m_pCallSiteSP = NULL;
    m_pCallerReturnAddress = NULL;
}

#if defined(_WIN64) && !defined(FEATURE_PAL)

EXTERN_C void PInvokeStubForHostInner(DWORD dwStackSize, LPVOID pStackFrame, LPVOID pTarget);

// C++ piece of one static stub for host on 64-bit. This is called by PInvokeStubForHost (PInvokeStubs.asm).
void __stdcall PInvokeStubForHostWorker(DWORD dwStackSize, LPVOID pStackFrame, LPVOID pThis)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    InlinedCallFrame *pFrame = (InlinedCallFrame *)GetThread()->GetFrame();
    _ASSERTE(InlinedCallFrame::FrameHasActiveCall(pFrame));

    LPVOID pTarget = NULL;
    MethodDesc *pMD = pFrame->GetFunction();

    if (pMD == NULL)
    {
        // This is a CALLI and m_Datum is a mangled target
        pTarget = (LPVOID)((UINT_PTR)pFrame->m_Datum >> 1);
    }
    else if (pMD->IsNDirect())
    {
        pTarget = ((NDirectMethodDesc *)pMD)->ndirect.m_pNativeNDirectTarget;
        if (pMD->IsQCall())
        {
#ifdef FEATURE_STACK_PROBE
            // We need just the stack probe for QCalls
            RetailStackProbe(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT));
#endif
            PInvokeStubForHostInner(dwStackSize, pStackFrame, pTarget);
            return;
        }
    }
#ifdef FEATURE_COMINTEROP
    else if (pMD->IsComPlusCall() || pMD->IsEEImpl())
    {
        LPVOID *lpVtbl = *(LPVOID **)pThis;
        ComPlusCallInfo *pComPlusCallInfo = ComPlusCallInfo::FromMethodDesc(pMD);
        pTarget = lpVtbl[pComPlusCallInfo->m_cachedComSlot];
    }
#endif // FEATURE_COMINTEROP
    else
    {
        UNREACHABLE_MSG("Unexpected MethodDesc kind encountered in PInvokeStubForHostWorker");
    }

    // We ask the host on every call. This is different from x86 but we keep this
    // behavior for maximum compatibility with previous releases.
    if (CallNeedsHostHook((size_t)pTarget))
    {
        // call LeaveRuntime
        class LeaveRuntimeHolderThrowComplus
        {
        public:
            LeaveRuntimeHolderThrowComplus(size_t target)
            {
                Thread::LeaveRuntimeThrowComplus(target);
            }
            ~LeaveRuntimeHolderThrowComplus()
            {
                Thread::EnterRuntime();
            }
        } holder((size_t)pTarget);

        PInvokeStubForHostInner(dwStackSize, pStackFrame, pTarget);
        // ~LeaveRuntimeHolderThrowComplus calls EnterRuntime here
    }
    else
    {
        // The host doesn't want to be notified - just call the target
        PInvokeStubForHostInner(dwStackSize, pStackFrame, pTarget);
    }
}
#endif // _WIN64 && !FEATURE_PAL



void UnmanagedToManagedFrame::ExceptionUnwind()
{
    WRAPPER_NO_CONTRACT;

    AppDomain::ExceptionUnwind(this);
}

#endif // !DACCESS_COMPILE

void ContextTransitionFrame::GcScanRoots(promote_func *fn, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    // Don't check app domains here - m_ReturnExecutionContext is in the parent frame's app domain
    (*fn)(dac_cast<PTR_PTR_Object>(PTR_HOST_MEMBER_TADDR(ContextTransitionFrame, this, m_ReturnExecutionContext)), sc, 0);
    LOG((LF_GC, INFO3, "    " FMT_ADDR "\n", DBG_ADDR(m_ReturnExecutionContext) ));

    // Don't check app domains here - m_LastThrownObjectInParentContext is in the parent frame's app domain
    (*fn)(dac_cast<PTR_PTR_Object>(PTR_HOST_MEMBER_TADDR(ContextTransitionFrame, this, m_LastThrownObjectInParentContext)), sc, 0);
    LOG((LF_GC, INFO3, "    " FMT_ADDR "\n", DBG_ADDR(m_LastThrownObjectInParentContext) ));
    
    // don't need to worry about the object moving as it is stored in a weak handle
    // but do need to report it so it doesn't get collected if the only reference to
    // it is in this frame. So only do something if are in promotion phase. And if are
    // in reloc phase this could cause invalid refs as the object may have been moved.
    if (! sc->promotion)
        return;
        
    // The dac only cares about strong references at the moment.  Since this is always
    // in a weak ref, we don't report it here.
#if defined(FEATURE_REMOTING) && !defined(DACCESS_COMPILE)
    Context *returnContext = GetReturnContext();
    PREFIX_ASSUME(returnContext != NULL);
    _ASSERTE(returnContext);
    _ASSERTE(returnContext->GetDomain());    // this will make sure is a valid pointer

    // In the VM we operate on a local to avoid double relocation.  In the dac we actually want
    // to know the real location.
#ifdef DACCESS_COMPILE
    PTR_PTR_Object ppRef = returnContext->GetExposedObjectRawUncheckedPtr();
    
    if (*ppRef == NULL)
        return;
        
    (*fn)(ppRef, sc, 0);

#else // DACCESS_COMPILE
    // We are in the middle of the GC. OBJECTREFs can't be used here since their built-in validation
    // chokes on objects that have been relocated already
    Object *pRef = returnContext->GetExposedObjectRawUnchecked();
    
    if (pRef == NULL)
        return;

    LOG((LF_GC, INFO3, "ContextTransitionFrame Protection Frame Promoting" FMT_ADDR "to ", DBG_ADDR(pRef) ));
    // Don't check app domains here - the objects are in the parent frame's app domain

    (*fn)(&pRef, sc, 0);
    LOG((LF_GC, INFO3, FMT_ADDR "\n", DBG_ADDR(pRef)));    
#endif // !DACCESS_COMPILE

#endif    
}


PCODE UnmanagedToManagedFrame::GetReturnAddress()
{
    WRAPPER_NO_CONTRACT;
    
    PCODE pRetAddr = Frame::GetReturnAddress();

    if (InlinedCallFrame::FrameHasActiveCall(m_Next) &&
        pRetAddr == m_Next->GetReturnAddress())
    {
        // there's actually no unmanaged code involved - we were called directly
        // from managed code using an InlinedCallFrame
        return NULL;
    }
    else
    {
        return pRetAddr;
    }
}
