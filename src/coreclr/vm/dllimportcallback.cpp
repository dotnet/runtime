// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DllImportCallback.cpp
//

//


#include "common.h"

#include "threads.h"
#include "excep.h"
#include "object.h"
#include "dllimportcallback.h"
#include "mlinfo.h"
#include "comdelegate.h"
#include "ceeload.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "stubgen.h"
#include "appdomain.inl"
#include "callingconvention.h"
#include "customattribute.h"
#include "typeparse.h"

#ifndef CROSSGEN_COMPILE

struct UM2MThunk_Args
{
    UMEntryThunk *pEntryThunk;
    void *pAddr;
    void *pThunkArgs;
    int argLen;
};

class UMEntryThunkFreeList
{
public:
    UMEntryThunkFreeList(size_t threshold) :
        m_threshold(threshold),
        m_count(0),
        m_pHead(NULL),
        m_pTail(NULL)
    {
        WRAPPER_NO_CONTRACT;

        m_crst.Init(CrstLeafLock, CRST_UNSAFE_ANYMODE);
    }

    UMEntryThunk *GetUMEntryThunk()
    {
        WRAPPER_NO_CONTRACT;

        if (m_count < m_threshold)
            return NULL;

        CrstHolder ch(&m_crst);

        UMEntryThunk *pThunk = m_pHead;

        if (pThunk == NULL)
            return NULL;

        m_pHead = m_pHead->m_pNextFreeThunk;
        --m_count;

        return pThunk;
    }

    void AddToList(UMEntryThunk *pThunk)
    {
        CONTRACTL
        {
            NOTHROW;
        }
        CONTRACTL_END;

        CrstHolder ch(&m_crst);

#if defined(HOST_OSX) && defined(HOST_ARM64)
        auto jitWriteEnableHolder = PAL_JITWriteEnable(true);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)

        if (m_pHead == NULL)
        {
            m_pHead = pThunk;
            m_pTail = pThunk;
        }
        else
        {
            m_pTail->m_pNextFreeThunk = pThunk;
            m_pTail = pThunk;
        }

        pThunk->m_pNextFreeThunk = NULL;

        ++m_count;
    }

private:
    // Used to delay reusing freed thunks
    size_t m_threshold;
    size_t m_count;
    UMEntryThunk *m_pHead;
    UMEntryThunk *m_pTail;
    CrstStatic m_crst;
};

#define DEFAULT_THUNK_FREE_LIST_THRESHOLD 64

static UMEntryThunkFreeList s_thunkFreeList(DEFAULT_THUNK_FREE_LIST_THRESHOLD);

#ifdef TARGET_X86

#ifdef FEATURE_STUBS_AS_IL

EXTERN_C void UMThunkStub(void);

PCODE UMThunkMarshInfo::GetExecStubEntryPoint()
{
    LIMITED_METHOD_CONTRACT;

    return GetEEFuncEntryPoint(UMThunkStub);
}

#else // FEATURE_STUBS_AS_IL

EXTERN_C VOID __cdecl UMThunkStubRareDisable();
EXTERN_C Thread* __stdcall CreateThreadBlockThrow();

// argument stack offsets are multiple of sizeof(SLOT) so we can tag them by OR'ing with 1
static_assert_no_msg((sizeof(SLOT) & 1) == 0);
#define MAKE_BYVAL_STACK_OFFSET(x) (x)
#define MAKE_BYREF_STACK_OFFSET(x) ((x) | 1)
#define IS_BYREF_STACK_OFFSET(x)   ((x) & 1)
#define GET_STACK_OFFSET(x)        ((x) & ~1)

// -1 means not used
#define UNUSED_STACK_OFFSET        (UINT)-1

// static
VOID UMEntryThunk::CompileUMThunkWorker(UMThunkStubInfo *pInfo,
                                        CPUSTUBLINKER *pcpusl,
                                        UINT *psrcofsregs, // NUM_ARGUMENT_REGISTERS elements
                                        UINT *psrcofs,     // pInfo->m_cbDstStack/STACK_ELEM_SIZE elements
                                        UINT retbufofs)    // the large structure return buffer ptr arg offset (if any)
{
    STANDARD_VM_CONTRACT;

    CodeLabel* pSetupThreadLabel    = pcpusl->NewCodeLabel();
    CodeLabel* pRejoinThreadLabel   = pcpusl->NewCodeLabel();
    CodeLabel* pDisableGCLabel      = pcpusl->NewCodeLabel();
    CodeLabel* pRejoinGCLabel       = pcpusl->NewCodeLabel();

    // We come into this code with UMEntryThunk in EAX
    const X86Reg kEAXentryThunk = kEAX;

    // For ThisCall, we make it look like a normal stdcall so that
    // the rest of the code (like repushing the arguments) does not
    // have to worry about it.

    if (pInfo->m_wFlags & umtmlThisCall)
    {
        // pop off the return address into EDX
        pcpusl->X86EmitPopReg(kEDX);

        if (pInfo->m_wFlags & umtmlThisCallHiddenArg)
        {
            // exchange ecx ( "this") with the hidden structure return buffer
            //  xchg ecx, [esp]
            pcpusl->X86EmitOp(0x87, kECX, (X86Reg)kESP_Unsafe);
        }

        // jam ecx (the "this" param onto stack. Now it looks like a normal stdcall.)
        pcpusl->X86EmitPushReg(kECX);

        // push edx - repush the return address
        pcpusl->X86EmitPushReg(kEDX);
    }
    
    // The native signature doesn't have a return buffer
    // but the managed signature does.
    // Set up the return buffer address here.
    if (pInfo->m_wFlags & umtmlBufRetValToEnreg)
    {
        // Calculate the return buffer address
        // Calculate the offset to the return buffer we establish for EAX:EDX below.
        // lea edx [esp - offset to EAX:EDX return buffer]
        pcpusl->X86EmitEspOffset(0x8d, kEDX, -0xc /* skip return addr, EBP, EBX */ -0x8 /* point to start of EAX:EDX return buffer */ );
        
        // exchange edx (which has the return buffer address)
        // with the return address
        // xchg edx, [esp]
        pcpusl->X86EmitOp(0x87, kEDX, (X86Reg)kESP_Unsafe);   
     
        // push edx
        pcpusl->X86EmitPushReg(kEDX);
    }

    // Setup the EBP frame
    pcpusl->X86EmitPushEBPframe();

    // Save EBX
    pcpusl->X86EmitPushReg(kEBX);

    // Make space for return value - instead of repeatedly doing push eax edx <trash regs> pop edx eax
    // we will save the return value once and restore it just before returning.
    pcpusl->X86EmitSubEsp(sizeof(PCONTEXT(NULL)->Eax) + sizeof(PCONTEXT(NULL)->Edx));

    // Load thread descriptor into ECX
    const X86Reg kECXthread = kECX;

    // save UMEntryThunk
    pcpusl->X86EmitPushReg(kEAXentryThunk);

    pcpusl->EmitSetup(pSetupThreadLabel);

    pcpusl->X86EmitMovRegReg(kECX, kEBX);

    pcpusl->EmitLabel(pRejoinThreadLabel);

    // restore UMEntryThunk
    pcpusl->X86EmitPopReg(kEAXentryThunk);

#ifdef _DEBUG
    // Save incoming registers
    pcpusl->X86EmitPushReg(kEAXentryThunk); // UMEntryThunk
    pcpusl->X86EmitPushReg(kECXthread); // thread descriptor

    pcpusl->X86EmitPushReg(kEAXentryThunk);
    pcpusl->X86EmitCall(pcpusl->NewExternalCodeLabel((LPVOID) LogUMTransition), 4);

    // Restore registers
    pcpusl->X86EmitPopReg(kECXthread);
    pcpusl->X86EmitPopReg(kEAXentryThunk);
#endif

#ifdef PROFILING_SUPPORTED
    // Notify profiler of transition into runtime, before we disable preemptive GC
    if (CORProfilerTrackTransitions())
    {
        // Load the methoddesc into EBX (UMEntryThunk->m_pMD)
        pcpusl->X86EmitIndexRegLoad(kEBX, kEAXentryThunk, UMEntryThunk::GetOffsetOfMethodDesc());

        // Save registers
        pcpusl->X86EmitPushReg(kEAXentryThunk); // UMEntryThunk
        pcpusl->X86EmitPushReg(kECXthread); // pCurThread

        // Push arguments and notify profiler
        pcpusl->X86EmitPushImm32(COR_PRF_TRANSITION_CALL);    // Reason
        pcpusl->X86EmitPushReg(kEBX);          // MethodDesc*
        pcpusl->X86EmitCall(pcpusl->NewExternalCodeLabel((LPVOID)ProfilerUnmanagedToManagedTransitionMD), 8);

        // Restore registers
        pcpusl->X86EmitPopReg(kECXthread);
        pcpusl->X86EmitPopReg(kEAXentryThunk);

        // Push the MethodDesc* (in EBX) for use by the transition on the way out.
        pcpusl->X86EmitPushReg(kEBX);
    }
#endif // PROFILING_SUPPORTED

    pcpusl->EmitDisable(pDisableGCLabel, TRUE, kECXthread);

    pcpusl->EmitLabel(pRejoinGCLabel);

    // construct a FrameHandlerExRecord

    // push [ECX]Thread.m_pFrame - corresponding to FrameHandlerExRecord::m_pEntryFrame
    pcpusl->X86EmitIndexPush(kECXthread, offsetof(Thread, m_pFrame));

    // push offset FastNExportExceptHandler
    pcpusl->X86EmitPushImm32((INT32)(size_t)FastNExportExceptHandler);

    // push fs:[0]
    const static BYTE codeSEH1[] = { 0x64, 0xFF, 0x35, 0x0, 0x0, 0x0, 0x0};
    pcpusl->EmitBytes(codeSEH1, sizeof(codeSEH1));
    // EmitBytes doesn't know to increase the stack size
    // so we do so manually
    pcpusl->SetStackSize(pcpusl->GetStackSize() + 4);

    // link in the exception frame
    // mov dword ptr fs:[0], esp
    const static BYTE codeSEH2[] = { 0x64, 0x89, 0x25, 0x0, 0x0, 0x0, 0x0};
    pcpusl->EmitBytes(codeSEH2, sizeof(codeSEH2));

    // EBX will hold address of start of arguments. Calculate here so the AD switch case can access
    // the arguments at their original location rather than re-copying them to the inner frame.
    // lea ebx, [ebp + 8]
    pcpusl->X86EmitIndexLea(kEBX, kEBP, 8);

    //
    // ----------------------------------------------------------------------------------------------
    //
    // From this point on (until noted) we might be executing as the result of calling into the
    // runtime in order to switch AppDomain. In order for the following code to function in both
    // scenarios it must be careful when making assumptions about the current stack layout (in the AD
    // switch case a new inner frame has been pushed which is not identical to the original outer
    // frame).
    //
    // Our guaranteed state at this point is as follows:
    //   EAX: Pointer to UMEntryThunk
    //   EBX: Pointer to start of caller's arguments
    //   ECX: Pointer to current Thread
    //   EBP: Equals EBX - 8 (no AD switch) or unspecified (AD switch)
    //
    // Stack:
    //
    //            +-------------------------+
    //    ESP + 0 |                         |
    //
    //            |         Varies          |
    //
    //            |                         |
    //            +-------------------------+
    //   EBX - 20 | Saved Result: EAX/ST(0) |
    //            +- - - - - - - - - - - - -+
    //   EBX - 16 | Saved Result: EDX/ST(0) |
    //            +-------------------------+
    //   EBX - 12 |      Caller's EBX       |
    //            +-------------------------+
    //    EBX - 8 |      Caller's EBP       |
    //            +-------------------------+
    //    EBX - 4 |     Return address      |
    //            +-------------------------+
    //    EBX + 0 |                         |
    //
    //            |   Caller's arguments    |
    //
    //            |                         |
    //            +-------------------------+
    //

    // save the thread pointer
    pcpusl->X86EmitPushReg(kECXthread);

    // reserve the space for call slot
    pcpusl->X86EmitSubEsp(4);

    // remember stack size for offset computations
    INT iStackSizeAtCallSlot = pcpusl->GetStackSize();

    if (!(pInfo->m_wFlags & umtmlSkipStub))
    {
        // save EDI (it's used by the IL stub invocation code)
        pcpusl->X86EmitPushReg(kEDI);
    }

    // repush any stack arguments
    int arg = pInfo->m_cbDstStack/STACK_ELEM_SIZE;

    while (arg--)
    {
        if (IS_BYREF_STACK_OFFSET(psrcofs[arg]))
        {
            // lea ecx, [ebx + ofs]
            pcpusl->X86EmitIndexLea(kECX, kEBX, GET_STACK_OFFSET(psrcofs[arg]));

            // push ecx
            pcpusl->X86EmitPushReg(kECX);
        }
        else
        {
            // push dword ptr [ebx + ofs]
            pcpusl->X86EmitIndexPush(kEBX, GET_STACK_OFFSET(psrcofs[arg]));
        }
    }

    // load register arguments
    int regidx = 0;

#define ARGUMENT_REGISTER(regname)                                                                 \
    if (psrcofsregs[regidx] != UNUSED_STACK_OFFSET)                                                \
    {                                                                                              \
        if (IS_BYREF_STACK_OFFSET(psrcofsregs[regidx]))                                            \
        {                                                                                          \
            /* lea reg, [ebx + ofs] */                                                             \
            pcpusl->X86EmitIndexLea(k##regname, kEBX, GET_STACK_OFFSET(psrcofsregs[regidx]));      \
        }                                                                                          \
        else                                                                                       \
        {                                                                                          \
            /* mov reg, [ebx + ofs] */                                                             \
            pcpusl->X86EmitIndexRegLoad(k##regname, kEBX, GET_STACK_OFFSET(psrcofsregs[regidx]));  \
        }                                                                                          \
    }                                                                                              \
    regidx++;

    ENUM_ARGUMENT_REGISTERS_BACKWARD();

#undef ARGUMENT_REGISTER

    if (!(pInfo->m_wFlags & umtmlSkipStub))
    {
        //
        // Call the IL stub which will:
        // 1) marshal
        // 2) call the managed method
        // 3) unmarshal
        //

        // the delegate object is extracted by the stub from UMEntryThunk
        _ASSERTE(pInfo->m_wFlags & umtmlIsStatic);

        // mov EDI, [EAX + UMEntryThunk.m_pUMThunkMarshInfo]
        pcpusl->X86EmitIndexRegLoad(kEDI, kEAXentryThunk, offsetof(UMEntryThunk, m_pUMThunkMarshInfo));

        // mov EDI, [EDI + UMThunkMarshInfo.m_pILStub]
        pcpusl->X86EmitIndexRegLoad(kEDI, kEDI, UMThunkMarshInfo::GetOffsetOfStub());

        // EAX still contains the UMEntryThunk pointer, so we cannot really use SCRATCHREG
        // we can use EDI, though

        INT iCallSlotOffset = pcpusl->GetStackSize() - iStackSizeAtCallSlot;

        // mov [ESP+iCallSlotOffset], EDI
        pcpusl->X86EmitIndexRegStore((X86Reg)kESP_Unsafe, iCallSlotOffset, kEDI);

        // call [ESP+iCallSlotOffset]
        pcpusl->X86EmitOp(0xff, (X86Reg)2, (X86Reg)kESP_Unsafe, iCallSlotOffset);

        // Emit a NOP so we know that we can call managed code
        INDEBUG(pcpusl->Emit8(X86_INSTR_NOP));

        // restore EDI
        pcpusl->X86EmitPopReg(kEDI);
    }
    else if (!(pInfo->m_wFlags & umtmlIsStatic))
    {
        //
        // This is call on delegate
        //

        // mov THIS, [EAX + UMEntryThunk.m_pObjectHandle]
        pcpusl->X86EmitOp(0x8b, THIS_kREG, kEAXentryThunk, offsetof(UMEntryThunk, m_pObjectHandle));

        // mov THIS, [THIS]
        pcpusl->X86EmitOp(0x8b, THIS_kREG, THIS_kREG);

        //
        // Inline Delegate.Invoke for perf
        //

        // mov SCRATCHREG, [THISREG + Delegate.FP]  ; Save target stub in register
        pcpusl->X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, THIS_kREG, DelegateObject::GetOffsetOfMethodPtr());

        // mov THISREG, [THISREG + Delegate.OR]  ; replace "this" pointer
        pcpusl->X86EmitIndexRegLoad(THIS_kREG, THIS_kREG, DelegateObject::GetOffsetOfTarget());

        INT iCallSlotOffset = pcpusl->GetStackSize() - iStackSizeAtCallSlot;

        // mov [ESP+iCallSlotOffset], SCRATCHREG
        pcpusl->X86EmitIndexRegStore((X86Reg)kESP_Unsafe,iCallSlotOffset,SCRATCH_REGISTER_X86REG);

        // call [ESP+iCallSlotOffset]
        pcpusl->X86EmitOp(0xff, (X86Reg)2, (X86Reg)kESP_Unsafe, iCallSlotOffset);

        INDEBUG(pcpusl->Emit8(X86_INSTR_NOP)); // Emit a NOP so we know that we can call managed code
    }
    else
    {
        //
        // Call the managed method
        //

        INT iCallSlotOffset = pcpusl->GetStackSize() - iStackSizeAtCallSlot;

        // mov SCRATCH, [SCRATCH + offsetof(UMEntryThunk.m_pManagedTarget)]
        pcpusl->X86EmitIndexRegLoad(SCRATCH_REGISTER_X86REG, SCRATCH_REGISTER_X86REG, offsetof(UMEntryThunk, m_pManagedTarget));

        // mov [ESP+iCallSlotOffset], SCRATCHREG
        pcpusl->X86EmitIndexRegStore((X86Reg)kESP_Unsafe, iCallSlotOffset, SCRATCH_REGISTER_X86REG);

        // call [ESP+iCallSlotOffset]
        pcpusl->X86EmitOp(0xff, (X86Reg)2, (X86Reg)kESP_Unsafe, iCallSlotOffset);

        INDEBUG(pcpusl->Emit8(X86_INSTR_NOP)); // Emit a NOP so we know that we can call managed code
    }

    // skip the call slot
    pcpusl->X86EmitAddEsp(4);

    // Save the return value to the outer frame
    if (pInfo->m_wFlags & umtmlFpu)
    {
        // save FP return value

        // fstp qword ptr [ebx - 0x8 - 0xc]
        pcpusl->X86EmitOffsetModRM(0xdd, (X86Reg)3, kEBX, -0x8 /* to outer EBP */ -0xc /* skip saved EBP, EBX */);
    }
    else
    {
        // save EDX:EAX
        if (retbufofs == UNUSED_STACK_OFFSET)
        {
            pcpusl->X86EmitIndexRegStore(kEBX, -0x8 /* to outer EBP */ -0xc /* skip saved EBP, EBX, EDX */, kEAX);
            pcpusl->X86EmitIndexRegStore(kEBX, -0x8 /* to outer EBP */ -0x8 /* skip saved EBP, EBX */, kEDX);
        }
        // In the umtmlBufRetValToEnreg case,
        // we set up the return buffer to output 
        // into the EDX:EAX buffer we set up for the register return case.
        // So we don't need to do more work here.
        else if ((pInfo->m_wFlags & umtmlBufRetValToEnreg) == 0)
        {
            if (pInfo->m_wFlags & umtmlEnregRetValToBuf)
            {
                pcpusl->X86EmitPushReg(kEDI); // Save EDI register
                // Move the return value from the enregistered return from the JIT
                // to the return buffer that the native calling convention expects.
                // NOTE: Since the managed calling convention does not enregister 8-byte
                // struct returns on x86, we only need to handle the single-register 4-byte case.
                pcpusl->X86EmitIndexRegLoad(kEDI, kEBX, retbufofs);
                pcpusl->X86EmitIndexRegStore(kEDI, 0x0, kEAX);
                pcpusl->X86EmitPopReg(kEDI); // Restore EDI register
            }
            // pretend that the method returned the ret buf hidden argument
            // (the structure ptr); C++ compiler seems to rely on this

            // mov dword ptr eax, [ebx + retbufofs]
            pcpusl->X86EmitIndexRegLoad(kEAX, kEBX, retbufofs);

            // save it as the return value
            pcpusl->X86EmitIndexRegStore(kEBX, -0x8 /* to outer EBP */ -0xc /* skip saved EBP, EBX, EDX */, kEAX);
        }
    }

    // restore the thread pointer
    pcpusl->X86EmitPopReg(kECXthread);

    //
    // Once we reach this point in the code we're back to a single scenario: the outer frame of the
    // reverse p/invoke.
    //
    // ----------------------------------------------------------------------------------------------
    //

    // move byte ptr [ecx + Thread.m_fPreemptiveGCDisabled],0
    pcpusl->X86EmitOffsetModRM(0xc6, (X86Reg)0, kECXthread, Thread::GetOffsetOfGCFlag());
    pcpusl->Emit8(0);

    CodeLabel *pRareEnable, *pEnableRejoin;
    pRareEnable    = pcpusl->NewCodeLabel();
    pEnableRejoin    = pcpusl->NewCodeLabel();

    // test byte ptr [ecx + Thread.m_State], TS_CatchAtSafePoint
    pcpusl->X86EmitOffsetModRM(0xf6, (X86Reg)0, kECXthread, Thread::GetOffsetOfState());
    pcpusl->Emit8(Thread::TS_CatchAtSafePoint);

    pcpusl->X86EmitCondJump(pRareEnable,X86CondCode::kJNZ);

    pcpusl->EmitLabel(pEnableRejoin);

    // *** unhook SEH frame

    // mov edx,[esp]  ;;pointer to the next exception record
    pcpusl->X86EmitEspOffset(0x8B, kEDX, 0);

    // mov dword ptr fs:[0], edx
    static const BYTE codeSEH[] = { 0x64, 0x89, 0x15, 0x0, 0x0, 0x0, 0x0 };
    pcpusl->EmitBytes(codeSEH, sizeof(codeSEH));

    // deallocate SEH frame
    pcpusl->X86EmitAddEsp(sizeof(FrameHandlerExRecord));

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackTransitions())
    {
        // Load the MethodDesc* we pushed on the entry transition into EBX.
        pcpusl->X86EmitPopReg(kEBX);

        // Save registers
        pcpusl->X86EmitPushReg(kECX);

        // Push arguments and notify profiler
        pcpusl->X86EmitPushImm32(COR_PRF_TRANSITION_RETURN);    // Reason
        pcpusl->X86EmitPushReg(kEBX); // MethodDesc*
        pcpusl->X86EmitCall(pcpusl->NewExternalCodeLabel((LPVOID)ProfilerManagedToUnmanagedTransitionMD), 8);

        // Restore registers
        pcpusl->X86EmitPopReg(kECX);
    }
#endif // PROFILING_SUPPORTED

    // Load the saved return value
    if (pInfo->m_wFlags & umtmlFpu)
    {
        // fld qword ptr [esp]
        pcpusl->Emit8(0xdd);
        pcpusl->Emit16(0x2404);

        pcpusl->X86EmitAddEsp(8);
    }
    else
    {
        pcpusl->X86EmitPopReg(kEAX);
        pcpusl->X86EmitPopReg(kEDX);
    }

    // Restore EBX, which was saved in prolog
    pcpusl->X86EmitPopReg(kEBX);

    pcpusl->X86EmitPopReg(kEBP);

    //retn n
    pcpusl->X86EmitReturn(pInfo->m_cbRetPop);

    //-------------------------------------------------------------
    // coming here if the thread is not set up yet
    //

    pcpusl->EmitLabel(pSetupThreadLabel);

    // call CreateThreadBlock
    pcpusl->X86EmitCall(pcpusl->NewExternalCodeLabel((LPVOID) CreateThreadBlockThrow), 0);

    // mov ecx,eax
    pcpusl->Emit16(0xc189);

    // jump back into the main code path
    pcpusl->X86EmitNearJump(pRejoinThreadLabel);

    //-------------------------------------------------------------
    // coming here if g_TrapReturningThreads was true
    //

    pcpusl->EmitLabel(pDisableGCLabel);

    // call UMThunkStubRareDisable.  This may throw if we are not allowed
    // to enter.  Note that we have not set up our SEH yet (deliberately).
    // This is important to handle the case where we cannot enter the CLR
    // during shutdown and cannot coordinate with the GC because of
    // deadlocks.
    pcpusl->X86EmitCall(pcpusl->NewExternalCodeLabel((LPVOID) UMThunkStubRareDisable), 0);

    // jump back into the main code path
    pcpusl->X86EmitNearJump(pRejoinGCLabel);

    //-------------------------------------------------------------
    // Coming here for rare case when enabling GC pre-emptive mode
    //

    pcpusl->EmitLabel(pRareEnable);

    // Thread object is expected to be in EBX. So first save caller's EBX
    pcpusl->X86EmitPushReg(kEBX);
    // mov ebx, ecx
    pcpusl->X86EmitMovRegReg(kEBX, kECXthread);

    pcpusl->EmitRareEnable(NULL);

    // restore ebx
    pcpusl->X86EmitPopReg(kEBX);

    // return to mainline of function
    pcpusl->X86EmitNearJump(pEnableRejoin);
}

namespace
{
    // Templated function to compute if a char string begins with a constant string.
    template<size_t S2LEN>
    bool BeginsWith(ULONG s1Len, const char* s1, const char (&s2)[S2LEN])
    {
        WRAPPER_NO_CONTRACT;

        ULONG s2Len = (ULONG)S2LEN - 1; // Remove null
        if (s1Len < s2Len)
            return false;

        return (0 == strncmp(s1, s2, s2Len));
    }
}

VOID UMThunkMarshInfo::SetUpForUnmanagedCallersOnly()
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pMD = GetMethod();
    _ASSERTE(pMD != NULL && pMD->HasUnmanagedCallersOnlyAttribute());

    // Validate usage
    COMDelegate::ThrowIfInvalidUnmanagedCallersOnlyUsage(pMD);

    BYTE* pData = NULL;
    LONG cData = 0;

    bool nativeCallableInternalData = false;
    HRESULT hr = pMD->GetCustomAttribute(WellKnownAttribute::UnmanagedCallersOnly, (const VOID **)(&pData), (ULONG *)&cData);
    if (hr == S_FALSE)
    {
        hr = pMD->GetCustomAttribute(WellKnownAttribute::NativeCallableInternal, (const VOID **)(&pData), (ULONG *)&cData);
        nativeCallableInternalData = SUCCEEDED(hr);
    }

    IfFailThrow(hr);

    _ASSERTE(cData > 0);

    CustomAttributeParser ca(pData, cData);

    // UnmanagedCallersOnly and NativeCallableInternal each
    // have optional named arguments.
    CaNamedArg namedArgs[2];

    // For the UnmanagedCallersOnly scenario.
    CaType caCallConvs;

    // Define attribute specific optional named properties
    if (nativeCallableInternalData)
    {
        namedArgs[0].InitI4FieldEnum("CallingConvention", "System.Runtime.InteropServices.CallingConvention", (ULONG)(CorPinvokeMap)0);
    }
    else
    {
        caCallConvs.Init(SERIALIZATION_TYPE_SZARRAY, SERIALIZATION_TYPE_TYPE, SERIALIZATION_TYPE_UNDEFINED, NULL, 0);
        namedArgs[0].Init("CallConvs", SERIALIZATION_TYPE_SZARRAY, caCallConvs);
    }

    // Define common optional named properties
    CaTypeCtor caEntryPoint(SERIALIZATION_TYPE_STRING);
    namedArgs[1].Init("EntryPoint", SERIALIZATION_TYPE_STRING, caEntryPoint);

    InlineFactory<SArray<CaValue>, 4> caValueArrayFactory;
    DomainAssembly* domainAssembly = pMD->GetLoaderModule()->GetDomainAssembly();
    IfFailThrow(Attribute::ParseAttributeArgumentValues(
        pData,
        cData,
        &caValueArrayFactory,
        NULL,
        0,
        namedArgs,
        lengthof(namedArgs),
        domainAssembly));

    // If the value isn't defined, then return without setting anything.
    if (namedArgs[0].val.type.tag == SERIALIZATION_TYPE_UNDEFINED)
        return;

    CorPinvokeMap callConvLocal = (CorPinvokeMap)0;
    if (nativeCallableInternalData)
    {
        callConvLocal = (CorPinvokeMap)(namedArgs[0].val.u4 << 8);
    }
    else
    {
        // Set WinAPI as the default
        callConvLocal = CorPinvokeMap::pmCallConvWinapi;

        CaValue* arrayOfTypes = &namedArgs[0].val;
        for (ULONG i = 0; i < arrayOfTypes->arr.length; i++)
        {
            CaValue& typeNameValue = arrayOfTypes->arr[i];

            // According to ECMA-335, type name strings are UTF-8. Since we are
            // looking for type names that are equivalent in ASCII and UTF-8,
            // using a const char constant is acceptable. Type name strings are
            // in Fully Qualified form, so we include the ',' delimiter.
            if (BeginsWith(typeNameValue.str.cbStr, typeNameValue.str.pStr, "System.Runtime.CompilerServices.CallConvCdecl,"))
            {
                callConvLocal = CorPinvokeMap::pmCallConvCdecl;
            }
            else if (BeginsWith(typeNameValue.str.cbStr, typeNameValue.str.pStr, "System.Runtime.CompilerServices.CallConvStdcall,"))
            {
                callConvLocal = CorPinvokeMap::pmCallConvStdcall;
            }
            else if (BeginsWith(typeNameValue.str.cbStr, typeNameValue.str.pStr, "System.Runtime.CompilerServices.CallConvFastcall,"))
            {
                callConvLocal = CorPinvokeMap::pmCallConvFastcall;
            }
            else if (BeginsWith(typeNameValue.str.cbStr, typeNameValue.str.pStr, "System.Runtime.CompilerServices.CallConvThiscall,"))
            {
                callConvLocal = CorPinvokeMap::pmCallConvThiscall;
            }
        }
    }

    m_callConv = (UINT16)callConvLocal;
}

// Compiles an unmanaged to managed thunk for the given signature.
Stub *UMThunkMarshInfo::CompileNExportThunk(LoaderHeap *pLoaderHeap, PInvokeStaticSigInfo* pSigInfo, MetaSig *pMetaSig, BOOL fNoStub)
{
    STANDARD_VM_CONTRACT;

    // stub is always static
    BOOL fIsStatic = (fNoStub ? pSigInfo->IsStatic() : TRUE);

    ArgIterator argit(pMetaSig);

    UINT nStackBytes = argit.SizeOfArgStack();
    _ASSERTE((nStackBytes % STACK_ELEM_SIZE) == 0);

    // size of stack passed to us from unmanaged, may be bigger that nStackBytes if there are
    // parameters with copy constructors where we perform value-to-reference transformation
    UINT nStackBytesIncoming = nStackBytes;

    UINT *psrcofs = (UINT *)_alloca((nStackBytes / STACK_ELEM_SIZE) * sizeof(UINT));
    UINT psrcofsregs[NUM_ARGUMENT_REGISTERS];
    UINT retbufofs = UNUSED_STACK_OFFSET;

    for (int i = 0; i < NUM_ARGUMENT_REGISTERS; i++)
        psrcofsregs[i] = UNUSED_STACK_OFFSET;

    UINT nNumArgs = pMetaSig->NumFixedArgs();

    UINT nOffset = 0;
    int numRegistersUsed = 0;
    int numStackSlotsIndex = nStackBytes / STACK_ELEM_SIZE;
    
    // This could have been set in the UnmanagedCallersOnly scenario.
    if (m_callConv == UINT16_MAX)
        m_callConv = static_cast<UINT16>(pSigInfo->GetCallConv());

    UMThunkStubInfo stubInfo;
    memset(&stubInfo, 0, sizeof(stubInfo));

    // process this
    if (!fIsStatic)
    {
        // just reserve ECX, instance target is special-cased in the thunk compiler
        numRegistersUsed++;
    }

    // process the return buffer parameter
    if (argit.HasRetBuffArg() || (m_callConv == pmCallConvThiscall && argit.HasValueTypeReturn()))
    {
        // Only copy the retbuf arg from the src call when both the managed call and native call
        // have a return buffer.
        if (argit.HasRetBuffArg())
        {
            // managed has a return buffer
            if (m_callConv != pmCallConvThiscall &&
                argit.HasValueTypeReturn() &&
                pMetaSig->GetReturnTypeSize() == ENREGISTERED_RETURNTYPE_MAXSIZE)
            {
                // Only managed has a return buffer.
                // Native returns in registers.
                // We add a flag so the stub correctly sets up the return buffer.
                stubInfo.m_wFlags |= umtmlBufRetValToEnreg;
            }
            numRegistersUsed++;
            _ASSERTE(numRegistersUsed - 1 < NUM_ARGUMENT_REGISTERS);
            psrcofsregs[NUM_ARGUMENT_REGISTERS - numRegistersUsed] = nOffset;
        }
        retbufofs = nOffset;
        nOffset += StackElemSize(sizeof(LPVOID));
    }

    // process ordinary parameters
    for (DWORD i = nNumArgs; i > 0; i--)
    {
        TypeHandle thValueType;
        CorElementType type = pMetaSig->NextArgNormalized(&thValueType);

        UINT cbSize = MetaSig::GetElemSize(type, thValueType);

        BOOL fPassPointer = FALSE;
        if (!fNoStub && type == ELEMENT_TYPE_PTR)
        {
            // this is a copy-constructed argument - get its size
            TypeHandle thPtr = pMetaSig->GetLastTypeHandleThrowing();

            _ASSERTE(thPtr.IsPointer());
            cbSize = thPtr.AsTypeDesc()->GetTypeParam().GetSize();

            // the incoming stack may be bigger that the outgoing (IL stub) stack
            nStackBytesIncoming += (StackElemSize(cbSize) - StackElemSize(sizeof(LPVOID)));
            fPassPointer = TRUE;
        }

        if (ArgIterator::IsArgumentInRegister(&numRegistersUsed, type, thValueType))
        {
            _ASSERTE(numRegistersUsed - 1 < NUM_ARGUMENT_REGISTERS);
            psrcofsregs[NUM_ARGUMENT_REGISTERS - numRegistersUsed] =
                (fPassPointer ?
                MAKE_BYREF_STACK_OFFSET(nOffset) :  // the register will get pointer to the incoming stack slot
                MAKE_BYVAL_STACK_OFFSET(nOffset));  // the register will get the incoming stack slot
        }
        else if (fPassPointer)
        {
            // the stack slot will get pointer to the incoming stack slot
            psrcofs[--numStackSlotsIndex] = MAKE_BYREF_STACK_OFFSET(nOffset);
        }
        else
        {
            // stack slots will get incoming stack slots (we may need more stack slots for larger parameters)
            for (UINT nSlotOfs = StackElemSize(cbSize); nSlotOfs > 0; nSlotOfs -= STACK_ELEM_SIZE)
            {
                // note the reverse order here which is necessary to maintain
                // the original layout of the structure (it'll be reversed once
                // more when repushing)
                psrcofs[--numStackSlotsIndex] = MAKE_BYVAL_STACK_OFFSET(nOffset + nSlotOfs - STACK_ELEM_SIZE);
            }
        }

        nOffset += StackElemSize(cbSize);
    }
    _ASSERTE(numStackSlotsIndex == 0);

    UINT cbActualArgSize = nStackBytesIncoming + (numRegistersUsed * STACK_ELEM_SIZE);

    if (!fIsStatic)
    {
        // do not count THIS
        cbActualArgSize -= StackElemSize(sizeof(LPVOID));
    }

    m_cbActualArgSize = cbActualArgSize;

    if (!FitsInU2(m_cbActualArgSize))
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);

    stubInfo.m_cbSrcStack = static_cast<UINT16>(m_cbActualArgSize);
    stubInfo.m_cbDstStack = nStackBytes;

    if (m_callConv == pmCallConvCdecl)
    {
        // caller pop
        m_cbRetPop = 0;
    }
    else
    {
        // callee pop
        m_cbRetPop = static_cast<UINT16>(m_cbActualArgSize);

        if (m_callConv == pmCallConvThiscall)
        {
            stubInfo.m_wFlags |= umtmlThisCall;
            if (argit.HasRetBuffArg())
            {
                stubInfo.m_wFlags |= umtmlThisCallHiddenArg;
            }
            else if (argit.HasValueTypeReturn())
            {
                stubInfo.m_wFlags |= umtmlThisCallHiddenArg | umtmlEnregRetValToBuf;
                // When the native signature has a return buffer but the
                // managed one does not, we need to handle popping the
                // the return buffer of the stack manually, which we do here.
                m_cbRetPop += 4;
            }
        }
    }

    stubInfo.m_cbRetPop = m_cbRetPop;

    if (fIsStatic) stubInfo.m_wFlags |= umtmlIsStatic;
    if (fNoStub) stubInfo.m_wFlags |= umtmlSkipStub;

    if (pMetaSig->HasFPReturn()) stubInfo.m_wFlags |= umtmlFpu;

    CPUSTUBLINKER cpusl;
    CPUSTUBLINKER *pcpusl = &cpusl;

    // call the worker to emit the actual thunk
    UMEntryThunk::CompileUMThunkWorker(&stubInfo, pcpusl, psrcofsregs, psrcofs, retbufofs);

    return pcpusl->Link(pLoaderHeap);
}

#endif // FEATURE_STUBS_AS_IL

#else // TARGET_X86

PCODE UMThunkMarshInfo::GetExecStubEntryPoint()
{
    LIMITED_METHOD_CONTRACT;

    return m_pILStub;
}

#endif // TARGET_X86

UMEntryThunkCache::UMEntryThunkCache(AppDomain *pDomain) :
    m_crst(CrstUMEntryThunkCache),
    m_pDomain(pDomain)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pDomain != NULL);
}

UMEntryThunkCache::~UMEntryThunkCache()
{
    WRAPPER_NO_CONTRACT;

    for (SHash<ThunkSHashTraits>::Iterator i = m_hash.Begin(); i != m_hash.End(); i++)
    {
        // UMEntryThunks in this cache own UMThunkMarshInfo in 1-1 fashion
        DestroyMarshInfo(i->m_pThunk->GetUMThunkMarshInfo());
        UMEntryThunk::FreeUMEntryThunk(i->m_pThunk);
    }
}

UMEntryThunk *UMEntryThunkCache::GetUMEntryThunk(MethodDesc *pMD)
{
    CONTRACT (UMEntryThunk *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    UMEntryThunk *pThunk;

    CrstHolder ch(&m_crst);

    const CacheElement *pElement = m_hash.LookupPtr(pMD);
    if (pElement != NULL)
    {
        pThunk = pElement->m_pThunk;
    }
    else
    {
        // cache miss -> create a new thunk
        pThunk = UMEntryThunk::CreateUMEntryThunk();
        Holder<UMEntryThunk *, DoNothing, UMEntryThunk::FreeUMEntryThunk> umHolder;
        umHolder.Assign(pThunk);

        UMThunkMarshInfo *pMarshInfo = (UMThunkMarshInfo *)(void *)(m_pDomain->GetStubHeap()->AllocMem(S_SIZE_T(sizeof(UMThunkMarshInfo))));
        Holder<UMThunkMarshInfo *, DoNothing, UMEntryThunkCache::DestroyMarshInfo> miHolder;
        miHolder.Assign(pMarshInfo);

        pMarshInfo->LoadTimeInit(pMD);
        pThunk->LoadTimeInit(NULL, NULL, pMarshInfo, pMD);

        // add it to the cache
        CacheElement element;
        element.m_pMD = pMD;
        element.m_pThunk = pThunk;
        m_hash.Add(element);

        miHolder.SuppressRelease();
        umHolder.SuppressRelease();
    }

    RETURN pThunk;
}

// FailFast if a method marked UnmanagedCallersOnlyAttribute is
// invoked directly from managed code. UMThunkStub.asm check the
// mode and call this function to failfast.
extern "C" VOID STDCALL ReversePInvokeBadTransition()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    // Fail
    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(
                                             COR_E_EXECUTIONENGINE,
                                             W("Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code.")
                                            );
}

// Disable from a place that is calling into managed code via a UMEntryThunk.
extern "C" VOID STDCALL UMThunkStubRareDisableWorker(Thread *pThread, UMEntryThunk *pUMEntryThunk)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // Do not add a CONTRACT here.  We haven't set up SEH.

    // WARNING!!!!
    // when we start executing here, we are actually in cooperative mode.  But we
    // haven't synchronized with the barrier to reentry yet.  So we are in a highly
    // dangerous mode.  If we call managed code, we will potentially be active in
    // the GC heap, even as GC's are occuring!

    // We must do the following in this order, because otherwise we would be constructing
    // the exception for the abort without synchronizing with the GC.  Also, we have no
    // CLR SEH set up, despite the fact that we may throw a ThreadAbortException.
    pThread->RareDisablePreemptiveGC();
    pThread->HandleThreadAbort();

#ifdef DEBUGGING_SUPPORTED
    // If the debugger is attached, we use this opportunity to see if
    // we're disabling preemptive GC on the way into the runtime from
    // unmanaged code. We end up here because
    // Increment/DecrementTraceCallCount() will bump
    // g_TrapReturningThreads for us.
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall((const BYTE *)pUMEntryThunk->GetManagedTarget());
#endif // DEBUGGING_SUPPORTED
}

PCODE TheUMEntryPrestubWorker(UMEntryThunk * pUMEntryThunk)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    Thread * pThread = GetThreadNULLOk();
    if (pThread == NULL)
        pThread = CreateThreadBlockThrow();

    GCX_COOP_THREAD_EXISTS(pThread);

    if (pThread->IsAbortRequested())
        pThread->HandleThreadAbort();

    UMEntryThunk::DoRunTimeInit(pUMEntryThunk);

    return (PCODE)pUMEntryThunk->GetCode();
}

void RunTimeInit_Wrapper(LPVOID /* UMThunkMarshInfo * */ ptr)
{
    WRAPPER_NO_CONTRACT;

    UMEntryThunk::DoRunTimeInit((UMEntryThunk*)ptr);
}


// asm entrypoint
void STDCALL UMEntryThunk::DoRunTimeInit(UMEntryThunk* pUMEntryThunk)
{

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pUMEntryThunk));
    }
    CONTRACTL_END;

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    // this method is called by stubs which are called by managed code,
    // so we need an unwind and continue handler so that our internal
    // exceptions don't leak out into managed code.
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    {
        GCX_PREEMP();
        pUMEntryThunk->RunTimeInit();
    }

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;
}

UMEntryThunk* UMEntryThunk::CreateUMEntryThunk()
{
    CONTRACT (UMEntryThunk*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    UMEntryThunk * p;

    p = s_thunkFreeList.GetUMEntryThunk();

    if (p == NULL)
        p = (UMEntryThunk *)(void *)SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->AllocMem(S_SIZE_T(sizeof(UMEntryThunk)));

    RETURN p;
}

void UMEntryThunk::Terminate()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_code.Poison();

    if (GetObjectHandle())
    {
#if defined(HOST_OSX) && defined(HOST_ARM64)
        auto jitWriteEnableHolder = PAL_JITWriteEnable(true);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)

        DestroyLongWeakHandle(GetObjectHandle());
        m_pObjectHandle = 0;
    }

    s_thunkFreeList.AddToList(this);
}

VOID UMEntryThunk::FreeUMEntryThunk(UMEntryThunk* p)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(p));
    }
    CONTRACTL_END;

    p->Terminate();
}

#endif // CROSSGEN_COMPILE

//-------------------------------------------------------------------------
// This function is used to report error when we call collected delegate.
// But memory that was allocated for thunk can be reused, due to it this
// function will not be called in all cases of the collected delegate call,
// also it may crash while trying to report the problem.
//-------------------------------------------------------------------------
VOID __fastcall UMEntryThunk::ReportViolation(UMEntryThunk* pEntryThunk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pEntryThunk));
    }
    CONTRACTL_END;

    MethodDesc* pMethodDesc = pEntryThunk->GetMethod();

    SString namespaceOrClassName;
    SString methodName;
    SString moduleName;

    pMethodDesc->GetMethodInfoNoSig(namespaceOrClassName, methodName);
    moduleName.SetUTF8(pMethodDesc->GetModule()->GetSimpleName());

    SString message;

    message.Printf(W("A callback was made on a garbage collected delegate of type '%s!%s::%s'."),
        moduleName.GetUnicode(),
        namespaceOrClassName.GetUnicode(),
        methodName.GetUnicode());

    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, message.GetUnicode());
}

UMThunkMarshInfo::~UMThunkMarshInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#if defined(TARGET_X86) && !defined(FEATURE_STUBS_AS_IL)
    if (m_pExecStub)
        m_pExecStub->DecRef();
#endif

#ifdef _DEBUG
    FillMemory(this, sizeof(*this), 0xcc);
#endif
}

MethodDesc* UMThunkMarshInfo::GetILStubMethodDesc(MethodDesc* pInvokeMD, PInvokeStaticSigInfo* pSigInfo, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStubMD = NULL;
    dwStubFlags |= NDIRECTSTUB_FL_REVERSE_INTEROP;  // could be either delegate interop or not--that info is passed in from the caller

#if defined(DEBUGGING_SUPPORTED)
    // Combining the next two lines, and eliminating jitDebuggerFlags, leads to bad codegen in x86 Release builds using Visual C++ 19.00.24215.1.
    CORJIT_FLAGS jitDebuggerFlags = GetDebuggerCompileFlags(pSigInfo->GetModule(), CORJIT_FLAGS());
    if (jitDebuggerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
    {
        dwStubFlags |= NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL;
    }
#endif // DEBUGGING_SUPPORTED

    pStubMD = NDirect::CreateCLRToNativeILStub(
        pSigInfo,
        dwStubFlags,
        pInvokeMD // may be NULL
        );

    return pStubMD;
}

//----------------------------------------------------------
// This initializer is called during load time.
// It does not do any stub initialization or sigparsing.
// The RunTimeInit() must be called subsequently to fully
// UMThunkMarshInfo.
//----------------------------------------------------------
VOID UMThunkMarshInfo::LoadTimeInit(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(pMD != NULL);

    LoadTimeInit(pMD->GetSignature(), pMD->GetModule(), pMD);
}

VOID UMThunkMarshInfo::LoadTimeInit(Signature sig, Module * pModule, MethodDesc * pMD)
{
    LIMITED_METHOD_CONTRACT;

    FillMemory(this, sizeof(UMThunkMarshInfo), 0); // Prevent problems with partial deletes

    // This will be overwritten by the actual code pointer (or NULL) at the end of UMThunkMarshInfo::RunTimeInit()
    m_pILStub = (PCODE)1;

    m_pMD = pMD;
    m_pModule = pModule;
    m_sig = sig;

#if defined(TARGET_X86) && !defined(FEATURE_STUBS_AS_IL)
    m_callConv = UINT16_MAX;
    INDEBUG(m_cbRetPop = 0xcccc;)
#endif
}

#ifndef CROSSGEN_COMPILE
//----------------------------------------------------------
// This initializer finishes the init started by LoadTimeInit.
// It does stub creation and can throw an exception.
//
// It can safely be called multiple times and by concurrent
// threads.
//----------------------------------------------------------
VOID UMThunkMarshInfo::RunTimeInit()
{
    STANDARD_VM_CONTRACT;

    // Nothing to do if already inited
    if (IsCompletelyInited())
        return;

    PCODE pFinalILStub = NULL;
    MethodDesc* pStubMD = NULL;

    MethodDesc * pMD = GetMethod();

#if defined(TARGET_X86) && !defined(FEATURE_STUBS_AS_IL)
    if (pMD != NULL
        && pMD->HasUnmanagedCallersOnlyAttribute())
    {
        SetUpForUnmanagedCallersOnly();
    }
#endif // TARGET_X86 && !FEATURE_STUBS_AS_IL

    // Lookup NGened stub - currently we only support ngening of reverse delegate invoke interop stubs
    if (pMD != NULL && pMD->IsEEImpl())
    {
        DWORD dwStubFlags = NDIRECTSTUB_FL_NGENEDSTUB | NDIRECTSTUB_FL_REVERSE_INTEROP | NDIRECTSTUB_FL_DELEGATE;

#if defined(DEBUGGING_SUPPORTED)
        // Combining the next two lines, and eliminating jitDebuggerFlags, leads to bad codegen in x86 Release builds using Visual C++ 19.00.24215.1.
        CORJIT_FLAGS jitDebuggerFlags = GetDebuggerCompileFlags(GetModule(), CORJIT_FLAGS());
        if (jitDebuggerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
        {
            dwStubFlags |= NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL;
        }
#endif // DEBUGGING_SUPPORTED

        pFinalILStub = GetStubForInteropMethod(pMD, dwStubFlags, &pStubMD);
    }

#if defined(TARGET_X86) && !defined(FEATURE_STUBS_AS_IL)
    PInvokeStaticSigInfo sigInfo;

    if (pMD != NULL)
        new (&sigInfo) PInvokeStaticSigInfo(pMD);
    else
        new (&sigInfo) PInvokeStaticSigInfo(GetSignature(), GetModule());

    Stub *pFinalExecStub = NULL;

    // we will always emit the argument-shuffling thunk, m_cbActualArgSize is set inside
    LoaderHeap *pHeap = (pMD == NULL ? NULL : pMD->GetLoaderAllocator()->GetStubHeap());

    if (pFinalILStub != NULL ||
        NDirect::MarshalingRequired(pMD, GetSignature().GetRawSig(), GetModule()))
    {
        if (pFinalILStub == NULL)
        {
            DWORD dwStubFlags = 0;

            if (sigInfo.IsDelegateInterop())
                dwStubFlags |= NDIRECTSTUB_FL_DELEGATE;

            pStubMD = GetILStubMethodDesc(pMD, &sigInfo, dwStubFlags);
            pFinalILStub = JitILStub(pStubMD);
        }

        MetaSig msig(pStubMD);
        pFinalExecStub = CompileNExportThunk(pHeap, &sigInfo, &msig, FALSE);
    }
    else
    {
        MetaSig msig(GetSignature(), GetModule(), NULL);
        pFinalExecStub = CompileNExportThunk(pHeap, &sigInfo, &msig, TRUE);
    }

    if (FastInterlockCompareExchangePointer(&m_pExecStub,
                                            pFinalExecStub,
                                            NULL) != NULL)
    {

        // Some thread swooped in and set us. Our stub is now a
        // duplicate, so throw it away.
        if (pFinalExecStub)
            pFinalExecStub->DecRef();
    }

#else // TARGET_X86 && !FEATURE_STUBS_AS_IL

    if (pFinalILStub == NULL)
    {
        PInvokeStaticSigInfo sigInfo;

        if (pMD != NULL)
            new (&sigInfo) PInvokeStaticSigInfo(pMD);
        else
            new (&sigInfo) PInvokeStaticSigInfo(GetSignature(), GetModule());

        DWORD dwStubFlags = 0;

        if (sigInfo.IsDelegateInterop())
            dwStubFlags |= NDIRECTSTUB_FL_DELEGATE;

        pStubMD = GetILStubMethodDesc(pMD, &sigInfo, dwStubFlags);
        pFinalILStub = JitILStub(pStubMD);
    }

#if defined(TARGET_X86)
    MetaSig sig(pMD);
    int numRegistersUsed = 0;
    UINT16 cbRetPop = 0;

    //
    // cbStackArgSize represents the number of arg bytes for the MANAGED signature
    //
    UINT32 cbStackArgSize = 0;

    int offs = 0;

#ifdef UNIX_X86_ABI
    if (HasRetBuffArgUnmanagedFixup(&sig))
    {
        // callee should pop retbuf
        numRegistersUsed += 1;
        offs += STACK_ELEM_SIZE;
        cbRetPop += STACK_ELEM_SIZE;
    }
#endif // UNIX_X86_ABI

    for (UINT i = 0 ; i < sig.NumFixedArgs(); i++)
    {
        TypeHandle thValueType;
        CorElementType type = sig.NextArgNormalized(&thValueType);
        int cbSize = sig.GetElemSize(type, thValueType);
        if (ArgIterator::IsArgumentInRegister(&numRegistersUsed, type, thValueType))
        {
            offs += STACK_ELEM_SIZE;
        }
        else
        {
            offs += StackElemSize(cbSize);
            cbStackArgSize += StackElemSize(cbSize);
        }
    }
    m_cbStackArgSize = cbStackArgSize;
    m_cbActualArgSize = (pStubMD != NULL) ? pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize() : offs;

    PInvokeStaticSigInfo sigInfo;
    if (pMD != NULL)
        new (&sigInfo) PInvokeStaticSigInfo(pMD);
    else
        new (&sigInfo) PInvokeStaticSigInfo(GetSignature(), GetModule());
    if (sigInfo.GetCallConv() == pmCallConvCdecl)
    {
        m_cbRetPop = cbRetPop;
    }
    else
    {
        // For all the other calling convention except cdecl, callee pops the stack arguments
        m_cbRetPop = cbRetPop + static_cast<UINT16>(m_cbActualArgSize);
    }
#endif // TARGET_X86

#endif // TARGET_X86 && !FEATURE_STUBS_AS_IL

    // Must be the last thing we set!
    InterlockedCompareExchangeT<PCODE>(&m_pILStub, pFinalILStub, (PCODE)1);
}

#if defined(TARGET_X86) && defined(FEATURE_STUBS_AS_IL)
VOID UMThunkMarshInfo::SetupArguments(char *pSrc, ArgumentRegisters *pArgRegs, char *pDst)
{
    MethodDesc *pMD = GetMethod();

    _ASSERTE(pMD);

    //
    // x86 native uses the following stack layout:
    // | saved eip |
    // | --------- | <- CFA
    // | stkarg 0  |
    // | stkarg 1  |
    // | ...       |
    // | stkarg N  |
    //
    // x86 managed, however, uses a bit different stack layout:
    // | saved eip |
    // | --------- | <- CFA
    // | stkarg M  | (NATIVE/MANAGE may have different number of stack arguments)
    // | ...       |
    // | stkarg 1  |
    // | stkarg 0  |
    //
    // This stub bridges the gap between them.
    //
    char *pCurSrc = pSrc;
    char *pCurDst = pDst + m_cbStackArgSize;

    MetaSig sig(pMD);

    int numRegistersUsed = 0;

#ifdef UNIX_X86_ABI
    if (HasRetBuffArgUnmanagedFixup(&sig))
    {
        // Pass retbuf via Ecx
        numRegistersUsed += 1;
        pArgRegs->Ecx = *((UINT32 *)pCurSrc);
        pCurSrc += STACK_ELEM_SIZE;
    }
#endif // UNIX_X86_ABI

    for (UINT i = 0 ; i < sig.NumFixedArgs(); i++)
    {
        TypeHandle thValueType;
        CorElementType type = sig.NextArgNormalized(&thValueType);
        int cbSize = sig.GetElemSize(type, thValueType);
        int elemSize = StackElemSize(cbSize);

        if (ArgIterator::IsArgumentInRegister(&numRegistersUsed, type, thValueType))
        {
            _ASSERTE(elemSize == STACK_ELEM_SIZE);

            if (numRegistersUsed == 1)
                pArgRegs->Ecx = *((UINT32 *)pCurSrc);
            else if (numRegistersUsed == 2)
                pArgRegs->Edx = *((UINT32 *)pCurSrc);
        }
        else
        {
            pCurDst -= elemSize;
            memcpy(pCurDst, pCurSrc, elemSize);
        }

        pCurSrc += elemSize;
    }

    _ASSERTE(pDst == pCurDst);
}

EXTERN_C VOID STDCALL UMThunkStubSetupArgumentsWorker(UMThunkMarshInfo *pMarshInfo,
                                                      char *pSrc,
                                                      UMThunkMarshInfo::ArgumentRegisters *pArgRegs,
                                                      char *pDst)
{
    pMarshInfo->SetupArguments(pSrc, pArgRegs, pDst);
}
#endif // TARGET_X86 && FEATURE_STUBS_AS_IL

#ifdef _DEBUG
void STDCALL LogUMTransition(UMEntryThunk* thunk)
{
    CONTRACTL
    {
        NOTHROW;
        DEBUG_ONLY;
        GC_NOTRIGGER;
        ENTRY_POINT;
        if (GetThread()) MODE_PREEMPTIVE; else MODE_ANY;
        DEBUG_ONLY;
        PRECONDITION(CheckPointer(thunk));
        PRECONDITION((GetThread() != NULL) ? (!GetThread()->PreemptiveGCDisabled()) : TRUE);
    }
    CONTRACTL_END;

    BEGIN_ENTRYPOINT_VOIDRET;

    void** retESP = ((void**) &thunk) + 4;

    MethodDesc* method = thunk->GetMethod();
    if (method)
    {
        LOG((LF_STUBS, LL_INFO1000000, "UNMANAGED -> MANAGED Stub To Method = %s::%s SIG %s Ret Address ESP = 0x%x ret = 0x%x\n",
            method->m_pszDebugClassName,
            method->m_pszDebugMethodName,
            method->m_pszDebugMethodSignature, retESP, *retESP));
    }

    END_ENTRYPOINT_VOIDRET;

    }
#endif

#endif // CROSSGEN_COMPILE
