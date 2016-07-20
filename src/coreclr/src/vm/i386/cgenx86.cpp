// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// CGENX86.CPP -
//
// Various helper routines for generating x86 assembly code.
//
//

// Precompiled Header

#include "common.h"

#include "field.h"
#include "stublink.h"
#include "cgensys.h"
#include "frames.h"
#include "excep.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "log.h"
#include "security.h"
#include "comdelegate.h"
#include "array.h"
#include "jitinterface.h"
#include "codeman.h"
#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif
#include "dbginterface.h"
#include "eeprofinterfaces.h"
#include "eeconfig.h"
#include "asmconstants.h"
#include "class.h"
#include "virtualcallstub.h"
#include "mdaassistants.h"
#include "jitinterface.h"

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#include "runtimecallablewrapper.h"
#include "comcache.h"
#include "olevariant.h"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

#include "stublink.inl"

extern "C" DWORD STDCALL GetSpecificCpuTypeAsm(void);
extern "C" DWORD STDCALL GetSpecificCpuFeaturesAsm(DWORD *pInfo);

// NOTE on Frame Size C_ASSERT usage in this file 
// if the frame size changes then the stubs have to be revisited for correctness
// kindly revist the logic and then update the constants so that the C_ASSERT will again fire
// if someone changes the frame size.  You are expected to keep this hard coded constant
// up to date so that changes in the frame size trigger errors at compile time if the code is not altered

void generate_noref_copy (unsigned nbytes, StubLinkerCPU* sl);

#ifndef DACCESS_COMPILE

//=============================================================================
// Runtime test to see if the OS has enabled support for the SSE2 instructions
//
//
BOOL Runtime_Test_For_SSE2()
{
#ifdef FEATURE_CORESYSTEM
    return TRUE;
#else

    BOOL result = IsProcessorFeaturePresent(PF_XMMI64_INSTRUCTIONS_AVAILABLE);

    if (result == FALSE)
        return FALSE;

    // **********************************************************************
    // ***                                                                ***
    // ***   IMPORTANT NOTE:                                              ***
    // ***                                                                ***
    // ***     All of these RunningOnXXX APIs return true when            ***
    // ***     the OS that you are running on is that OS or later.        ***
    // ***     For example RunningOnWin2003() will return true            ***
    // ***     when you are running on Win2k3, Vista, Win7 or later.      ***
    // ***                                                                ***
    // **********************************************************************


    // Windows 7 and later should alwys be using SSE2 instructions
    //  this is true for both for native and Wow64
    //
    if (RunningOnWin7())
        return TRUE;

    if (RunningInWow64())
    {
        // There is an issue with saving/restoring the SSE2 registers under wow64 
        // So we figure out if we are running on an impacted OS and Service Pack level
        //     See DevDiv Bugs 89587 for the wow64 bug.
        //

        _ASSERTE(ExOSInfoAvailable());  // This is always available on Vista and later

        //
        // The issue is fixed in Windows Server 2008 or Vista/SP1
        //
        // It is not fixed in Vista/RTM, so check for that case
        // 
        if ((ExOSInfoRunningOnServer() == FALSE))
        {
            OSVERSIONINFOEX osvi;

            ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
            osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
            osvi.wServicePackMajor = 0;

            DWORDLONG dwlConditionMask = 0;
            VER_SET_CONDITION( dwlConditionMask, CLR_VER_SERVICEPACKMAJOR, VER_EQUAL);
                
            if (VerifyVersionInfo(&osvi, CLR_VER_SERVICEPACKMAJOR, dwlConditionMask))
                result = FALSE;
        }
    }

    return result;
#endif
}

//---------------------------------------------------------------
// Returns the type of CPU (the value of x of x86)
// (Please note, that it returns 6 for P5-II)
//---------------------------------------------------------------
void GetSpecificCpuInfo(CORINFO_CPU * cpuInfo)
{
    LIMITED_METHOD_CONTRACT;

    static CORINFO_CPU val = { 0, 0, 0 };

    if (val.dwCPUType)
    {
        *cpuInfo = val;
        return;
    }

    CORINFO_CPU tempVal;
    tempVal.dwCPUType = GetSpecificCpuTypeAsm();  // written in ASM & doesn't participate in contracts
    _ASSERTE(tempVal.dwCPUType);
    
#ifdef _DEBUG
    {
        SO_NOT_MAINLINE_REGION();

    /* Set Family+Model+Stepping string (eg., x690 for Banias, or xF30 for P4 Prescott)
     * instead of Family only
     */
     
    const DWORD cpuDefault = 0xFFFFFFFF;
    static ConfigDWORD cpuFamily;
    DWORD configCpuFamily = cpuFamily.val_DontUse_(CLRConfig::INTERNAL_CPUFamily, cpuDefault);
    if (configCpuFamily != cpuDefault)
    {
        assert((configCpuFamily & 0xFFF) == configCpuFamily);
        tempVal.dwCPUType = (tempVal.dwCPUType & 0xFFFF0000) | configCpuFamily;
    }
    }
#endif

    tempVal.dwFeatures = GetSpecificCpuFeaturesAsm(&tempVal.dwExtendedFeatures);  // written in ASM & doesn't participate in contracts

#ifdef _DEBUG
    {
        SO_NOT_MAINLINE_REGION();

    /* Set the 32-bit feature mask
     */
    
    const DWORD cpuFeaturesDefault = 0xFFFFFFFF;
    static ConfigDWORD cpuFeatures;
    DWORD configCpuFeatures = cpuFeatures.val_DontUse_(CLRConfig::INTERNAL_CPUFeatures, cpuFeaturesDefault);
    if (configCpuFeatures != cpuFeaturesDefault)
    {
        tempVal.dwFeatures = configCpuFeatures;
    }
    }
#endif

    val = *cpuInfo = tempVal;
}

#endif // #ifndef DACCESS_COMPILE


//---------------------------------------------------------------------------------------
//
// Initialize the EHContext using the resume PC and the REGDISPLAY.  The EHContext is currently used in two
// scenarios: to store the register state before calling an EH clause, and to retrieve the ambient SP of a 
// particular stack frame.  resumePC means different things in the two scenarios.  In the former case, it
// is the IP at which we are going to resume execution when we call an EH clause.  In the latter case, it 
// is just the current IP.
//
// Arguments:
//    resumePC - refer to the comment above
//    regs     - This is the REGDISPLAY obtained from the CrawlFrame used in the stackwalk.  It represents the
//               stack frame of the method containing the EH clause we are about to call.  For getting the 
//               ambient SP, this is the stack frame we are interested in.
//

void EHContext::Setup(PCODE resumePC, PREGDISPLAY regs)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // EAX ECX EDX are scratch
    this->Esp  = regs->Esp;
    this->Ebx = *regs->pEbx;
    this->Esi = *regs->pEsi;
    this->Edi = *regs->pEdi;
    this->Ebp = *regs->pEbp;

    this->Eip = (ULONG)(size_t)resumePC;
}

//
// Update the registers using new context
//
// This is necessary to reflect GC pointer changes during the middle of a unwind inside a 
// finally clause, because:
// 1. GC won't see the part of stack inside try (which has thrown an exception) that is already 
// unwinded and thus GC won't update GC pointers for this portion of the stack, but rather the 
// call stack in finally.
// 2. upon return of finally, the unwind process continues and unwinds stack based on the part 
// of stack inside try and won't see the updated values in finally.
// As a result, we need to manually update the context using register values upon return of finally
//
// Note that we only update the registers for finally clause because
// 1. For filter handlers, stack walker is able to see the whole stack (including the try part)
// with the help of ExceptionFilterFrame as filter handlers are called in first pass
// 2. For catch handlers, the current unwinding is already finished
//
void EHContext::UpdateFrame(PREGDISPLAY regs)
{
    LIMITED_METHOD_CONTRACT;
    
    // EAX ECX EDX are scratch. 
    // No need to update ESP as unwinder takes care of that for us

    LOG((LF_EH, LL_INFO1000, "Updating saved EBX: *%p= %p\n", regs->pEbx, this->Ebx));
    LOG((LF_EH, LL_INFO1000, "Updating saved ESI: *%p= %p\n", regs->pEsi, this->Esi));
    LOG((LF_EH, LL_INFO1000, "Updating saved EDI: *%p= %p\n", regs->pEdi, this->Edi));
    LOG((LF_EH, LL_INFO1000, "Updating saved EBP: *%p= %p\n", regs->pEbp, this->Ebp));
    
    *regs->pEbx = this->Ebx;
    *regs->pEsi = this->Esi;
    *regs->pEdi = this->Edi;
    *regs->pEbp = this->Ebp;
}

void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    MethodDesc * pFunc = GetFunction();
    _ASSERTE(pFunc != NULL);
    UpdateRegDisplayHelper(pRD, pFunc->CbStackPop());

    RETURN;
}

void TransitionFrame::UpdateRegDisplayHelper(const PREGDISPLAY pRD, UINT cbStackPop)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    CalleeSavedRegisters* regs = GetCalleeSavedRegisters();

    // reset pContext; it's only valid for active (top-most) frame

    pRD->pContext = NULL;

    pRD->pEdi = (DWORD*) &regs->edi;
    pRD->pEsi = (DWORD*) &regs->esi;
    pRD->pEbx = (DWORD*) &regs->ebx;
    pRD->pEbp = (DWORD*) &regs->ebp;
    pRD->PCTAddr = GetReturnAddressPtr();
    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);
    pRD->Esp  = (DWORD)(pRD->PCTAddr + sizeof(TADDR) + cbStackPop);

    RETURN;
}

void HelperMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        PRECONDITION(m_MachState.isValid());               // InsureInit has been called
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;

#ifdef DACCESS_COMPILE

    //
    // In the dac case we may have gotten here
    // without the frame being initialized, so
    // try and initialize on the fly.
    //

    if (!m_MachState.isValid())
    {
        MachState unwindState;

        InsureInit(false, &unwindState);
        pRD->PCTAddr = dac_cast<TADDR>(unwindState.pRetAddr());
        pRD->ControlPC = unwindState.GetRetAddr();
        pRD->Esp = unwindState._esp;

        // Get some special host instance memory
        // so we have a place to point to.
        // This host memory has no target address
        // and so won't be looked up or used for
        // anything else.
        MachState* thisState = (MachState*)
            DacAllocHostOnlyInstance(sizeof(*thisState), true);

        thisState->_edi = unwindState._edi;
        pRD->pEdi = (DWORD *)&thisState->_edi;
        thisState->_esi = unwindState._esi;
        pRD->pEsi = (DWORD *)&thisState->_esi;
        thisState->_ebx = unwindState._ebx;
        pRD->pEbx = (DWORD *)&thisState->_ebx;
        thisState->_ebp = unwindState._ebp;
        pRD->pEbp = (DWORD *)&thisState->_ebp;

        // InsureInit always sets m_RegArgs to zero
        // in the real code.  I'm not sure exactly
        // what should happen in the on-the-fly case,
        // but go with what would happen from an InsureInit.
        RETURN;
    }

#endif // #ifdef DACCESS_COMPILE
    
    // DACCESS: The MachState pointers are kept as PTR_TADDR so
    // the host pointers here refer to the appropriate size and
    // these casts are not a problem.
    pRD->pEdi = (DWORD*) m_MachState.pEdi();
    pRD->pEsi = (DWORD*) m_MachState.pEsi();
    pRD->pEbx = (DWORD*) m_MachState.pEbx();
    pRD->pEbp = (DWORD*) m_MachState.pEbp();
    pRD->PCTAddr = dac_cast<TADDR>(m_MachState.pRetAddr());
    pRD->ControlPC = m_MachState.GetRetAddr();
    pRD->Esp  = (DWORD) m_MachState.esp();

    RETURN;
}

#ifdef _DEBUG_IMPL
// Confirm that if the machine state was not initialized, then
// any unspilled callee saved registers did not change
EXTERN_C MachState* STDCALL HelperMethodFrameConfirmState(HelperMethodFrame* frame, void* esiVal, void* ediVal, void* ebxVal, void* ebpVal) 
    {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    MachState* state = frame->MachineState(); 

    // if we've already executed this check once for this helper method frame then
    // we don't do the check again because it is very expensive.
    if (frame->HaveDoneConfirmStateCheck())
    {
        return state;
    }

    // probe to avoid a kazillion violations in the code that follows.
    BEGIN_DEBUG_ONLY_CODE;
    if (!state->isValid())
    {
        frame->InsureInit(false, NULL);
        _ASSERTE(state->_pEsi != &state->_esi || state->_esi  == (TADDR)esiVal);
        _ASSERTE(state->_pEdi != &state->_edi || state->_edi  == (TADDR)ediVal);
        _ASSERTE(state->_pEbx != &state->_ebx || state->_ebx  == (TADDR)ebxVal);
        _ASSERTE(state->_pEbp != &state->_ebp || state->_ebp  == (TADDR)ebpVal);
    }
    END_DEBUG_ONLY_CODE;

    // set that we have executed this check once for this helper method frame.
    frame->SetHaveDoneConfirmStateCheck();

    return state;
}
#endif

void ExternalMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    UpdateRegDisplayHelper(pRD, CbStackPopUsingGCRefMap(GetGCRefMap()));

    RETURN;
}


void StubDispatchFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    PTR_BYTE pGCRefMap = GetGCRefMap();
    if (pGCRefMap != NULL)
    {
        UpdateRegDisplayHelper(pRD, CbStackPopUsingGCRefMap(pGCRefMap));
    }
    else
    if (GetFunction() != NULL)
    {
        FramedMethodFrame::UpdateRegDisplay(pRD);
    }
    else
    {
        UpdateRegDisplayHelper(pRD, 0);

        // If we do not have owning MethodDesc, we need to pretend that 
        // the call happened on the call instruction to get the ESP unwound properly.
        //
        // This path is hit when we are throwing null reference exception from
        // code:VSD_ResolveWorker or code:StubDispatchFixupWorker
        pRD->ControlPC = GetAdjustedCallAddress(pRD->ControlPC);
    }

    RETURN;
}

PCODE StubDispatchFrame::GetReturnAddress()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PCODE retAddress = FramedMethodFrame::GetReturnAddress();
    if (GetFunction() == NULL && GetGCRefMap() == NULL)
    {
        // See comment in code:StubDispatchFrame::UpdateRegDisplay
        retAddress = GetAdjustedCallAddress(retAddress);
    }
    return retAddress;
}

void FaultingExceptionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    CalleeSavedRegisters* regs = GetCalleeSavedRegisters();

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;

    pRD->pEdi = (DWORD*) &regs->edi;
    pRD->pEsi = (DWORD*) &regs->esi;
    pRD->pEbx = (DWORD*) &regs->ebx;
    pRD->pEbp = (DWORD*) &regs->ebp;
    pRD->PCTAddr = GetReturnAddressPtr();
    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);
    pRD->Esp = m_Esp;
    RETURN;
}

void InlinedCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        // We should skip over InlinedCallFrame if it is not active.
        // It will be part of a JITed method's frame, and the stack-walker
        // can handle such a case.
#ifdef PROFILING_SUPPORTED        
        PRECONDITION(CORProfilerStackSnapshotEnabled() || InlinedCallFrame::FrameHasActiveCall(this));
#endif
        HOST_NOCALLS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // @TODO: Remove this after the debugger is fixed to avoid stack-walks from bad places
    // @TODO: This may be still needed for sampling profilers
    if (!InlinedCallFrame::FrameHasActiveCall(this))
    {
        LOG((LF_CORDB, LL_ERROR, "WARNING: InlinedCallFrame::UpdateRegDisplay called on inactive frame %p\n", this));
        return;
    }
    
    DWORD stackArgSize = (DWORD) dac_cast<TADDR>(m_Datum);   

    if (stackArgSize & ~0xFFFF)
    {
        NDirectMethodDesc * pMD = PTR_NDirectMethodDesc(m_Datum);

        /* if this is not an NDirect frame, something is really wrong */

        _ASSERTE(pMD->SanityCheck() && pMD->IsNDirect());

        stackArgSize = pMD->GetStackArgumentSize();
    }

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;


    pRD->pEbp = (DWORD*) &m_pCalleeSavedFP;

    /* The return address is just above the "ESP" */
    pRD->PCTAddr = PTR_HOST_MEMBER_TADDR(InlinedCallFrame, this,
                                         m_pCallerReturnAddress);
    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);

    /* Now we need to pop off the outgoing arguments */
    pRD->Esp  = (DWORD) dac_cast<TADDR>(m_pCallSiteSP) + stackArgSize;
    RETURN;
}

#ifdef FEATURE_HIJACK
//==========================
// Resumable Exception Frame
//
TADDR ResumableFrame::GetReturnAddressPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, Eip);
}

void ResumableFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;

    CONTEXT* pUnwoundContext = m_Regs;

#if !defined(DACCESS_COMPILE)
    // "pContextForUnwind" field is only used on X86 since not only is it initialized just for it,
    // but its used only under the confines of STACKWALKER_MAY_POP_FRAMES preprocessor define,
    // which is defined for x86 only (refer to its definition in stackwalk.cpp).
    if (pRD->pContextForUnwind != NULL)
    {
        pUnwoundContext = pRD->pContextForUnwind;

        pUnwoundContext->Eax = m_Regs->Eax;
        pUnwoundContext->Ecx = m_Regs->Ecx;
        pUnwoundContext->Edx = m_Regs->Edx;

        pUnwoundContext->Edi = m_Regs->Edi;
        pUnwoundContext->Esi = m_Regs->Esi;
        pUnwoundContext->Ebx = m_Regs->Ebx;
        pUnwoundContext->Ebp = m_Regs->Ebp;
        pUnwoundContext->Eip = m_Regs->Eip;
    }
#endif // !defined(DACCESS_COMPILE)

    pRD->pEax = &pUnwoundContext->Eax;
    pRD->pEcx = &pUnwoundContext->Ecx;
    pRD->pEdx = &pUnwoundContext->Edx;

    pRD->pEdi = &pUnwoundContext->Edi;
    pRD->pEsi = &pUnwoundContext->Esi;
    pRD->pEbx = &pUnwoundContext->Ebx;
    pRD->pEbp = &pUnwoundContext->Ebp;

    pRD->ControlPC = pUnwoundContext->Eip;
    pRD->PCTAddr = dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, Eip);

    pRD->Esp  = m_Regs->Esp;

    RETURN;
}

// The HijackFrame has to know the registers that are pushed by OnHijackTripThread
void HijackFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // This only describes the top-most frame
    pRD->pContext = NULL;

    pRD->pEdi = &m_Args->Edi;
    pRD->pEsi = &m_Args->Esi;
    pRD->pEbx = &m_Args->Ebx;
    pRD->pEdx = &m_Args->Edx;
    pRD->pEcx = &m_Args->Ecx;
    pRD->pEax = &m_Args->Eax;

    pRD->pEbp = &m_Args->Ebp;
    pRD->PCTAddr = dac_cast<TADDR>(m_Args) + offsetof(HijackArgs, Eip);
    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);
    pRD->Esp  = (DWORD)(pRD->PCTAddr + sizeof(TADDR));
}

#endif  // FEATURE_HIJACK

void PInvokeCalliFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    VASigCookie *pVASigCookie = GetVASigCookie();
    UpdateRegDisplayHelper(pRD, pVASigCookie->sizeOfArgs+sizeof(int));

    RETURN;
}

void TailCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;

    pRD->pEdi = (DWORD*)&m_regs.edi;
    pRD->pEsi = (DWORD*)&m_regs.esi;
    pRD->pEbx = (DWORD*)&m_regs.ebx;
    pRD->pEbp = (DWORD*)&m_regs.ebp;

    pRD->PCTAddr = GetReturnAddressPtr();
    pRD->ControlPC = *PTR_PCODE(pRD->PCTAddr);
    pRD->Esp  = (DWORD)(pRD->PCTAddr + sizeof(TADDR));

    RETURN;
}

//------------------------------------------------------------------------
// This is declared as returning WORD instead of PRD_TYPE because of
// header issues with cgencpu.h including dbginterface.h.
WORD GetUnpatchedCodeData(LPCBYTE pAddr)
{
#ifndef _TARGET_X86_
#error Make sure this works before porting to platforms other than x86.
#endif
    CONTRACT(WORD) {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CORDebuggerAttached());
        PRECONDITION(CheckPointer(pAddr));
        SO_TOLERANT;
    } CONTRACT_END;

    // Ordering is because x86 is little-endien.
    BYTE bLow  = pAddr[0];
    BYTE bHigh = pAddr[1];

#ifndef DACCESS_COMPILE
    // Need to make sure that the code we're reading is free of breakpoint patches.
    PRD_TYPE unpatchedOpcode;
    if (g_pDebugInterface->CheckGetPatchedOpcode((CORDB_ADDRESS_TYPE *)pAddr,
                                                 &unpatchedOpcode))
    {
        // PRD_TYPE is supposed to be an opaque debugger structure representing data to remove a patch.
        // Although PRD_TYPE is currently typedef'ed to be a DWORD_PTR, it's actually semantically just a BYTE. 
        // (since a patch on x86 is just an 0xCC instruction).
        // Ideally, the debugger subsystem would expose a patch-code stripper that returns BYTE/WORD/etc, and
        // not force us to crack it ourselves here. 
        bLow = (BYTE) unpatchedOpcode;
    }
    // 
#endif

    WORD w = bLow + (bHigh << 8);
    RETURN w;
}


#ifndef DACCESS_COMPILE

//-------------------------------------------------------------------------
// One-time creation of special prestub to initialize UMEntryThunks.
//-------------------------------------------------------------------------
Stub *GenerateUMThunkPrestub()
{
    CONTRACT(Stub*)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CPUSTUBLINKER sl;
    CPUSTUBLINKER *psl = &sl;

    CodeLabel* rgRareLabels[] = { psl->NewCodeLabel(),
                                  psl->NewCodeLabel(),
                                  psl->NewCodeLabel()
                                };


    CodeLabel* rgRejoinLabels[] = { psl->NewCodeLabel(),
                                    psl->NewCodeLabel(),
                                    psl->NewCodeLabel()
                                };

    // emit the initial prolog
    psl->EmitComMethodStubProlog(UMThkCallFrame::GetMethodFrameVPtr(), rgRareLabels, rgRejoinLabels, FALSE /*Don't profile*/);

    // mov ecx, [esi+UMThkCallFrame.pUMEntryThunk]
    psl->X86EmitIndexRegLoad(kECX, kESI, UMThkCallFrame::GetOffsetOfUMEntryThunk());

    // The call conv is a __stdcall   
    psl->X86EmitPushReg(kECX);

    // call UMEntryThunk::DoRunTimeInit
    psl->X86EmitCall(psl->NewExternalCodeLabel((LPVOID)UMEntryThunk::DoRunTimeInit), 4);

    // mov ecx, [esi+UMThkCallFrame.pUMEntryThunk]
    psl->X86EmitIndexRegLoad(kEAX, kESI, UMThkCallFrame::GetOffsetOfUMEntryThunk());

    //    lea eax, [eax + UMEntryThunk.m_code]  // point to fixedup UMEntryThunk
    psl->X86EmitOp(0x8d, kEAX, kEAX, 
                   UMEntryThunk::GetCodeOffset() + UMEntryThunkCode::GetEntryPointOffset());

    psl->EmitComMethodStubEpilog(UMThkCallFrame::GetMethodFrameVPtr(), rgRareLabels, rgRejoinLabels, FALSE /*Don't profile*/);

    RETURN psl->Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());
}

Stub *GenerateInitPInvokeFrameHelper()
{
    CONTRACT(Stub*)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CPUSTUBLINKER sl;
    CPUSTUBLINKER *psl = &sl;

    CORINFO_EE_INFO::InlinedCallFrameInfo FrameInfo;
    InlinedCallFrame::GetEEInfo(&FrameInfo);

    // EDI contains address of the frame on stack (the frame ptr, not its negspace)
    unsigned negSpace = FrameInfo.offsetOfFrameVptr;

    // mov esi, GetThread()
    psl->X86EmitCurrentThreadFetch(kESI, (1<<kEDI)|(1<<kEBX)|(1<<kECX)|(1<<kEDX));

    // mov [edi + FrameInfo.offsetOfGSCookie], GetProcessGSCookie()
    psl->X86EmitOffsetModRM(0xc7, (X86Reg)0x0, kEDI, FrameInfo.offsetOfGSCookie - negSpace);
    psl->Emit32(GetProcessGSCookie());

    // mov [edi + FrameInfo.offsetOfFrameVptr], InlinedCallFrame::GetFrameVtable()
    psl->X86EmitOffsetModRM(0xc7, (X86Reg)0x0, kEDI, FrameInfo.offsetOfFrameVptr - negSpace);
    psl->Emit32(InlinedCallFrame::GetMethodFrameVPtr());

    // mov eax, [esi + offsetof(Thread, m_pFrame)]
    // mov [edi + FrameInfo.offsetOfFrameLink], eax
    psl->X86EmitIndexRegLoad(kEAX, kESI, offsetof(Thread, m_pFrame));
    psl->X86EmitIndexRegStore(kEDI, FrameInfo.offsetOfFrameLink - negSpace, kEAX);

    // mov [edi + FrameInfo.offsetOfCalleeSavedEbp], ebp
    psl->X86EmitIndexRegStore(kEDI, FrameInfo.offsetOfCalleeSavedFP - negSpace, kEBP);

    // mov [edi + FrameInfo.offsetOfReturnAddress], 0
    psl->X86EmitOffsetModRM(0xc7, (X86Reg)0x0, kEDI, FrameInfo.offsetOfReturnAddress - negSpace);
    psl->Emit32(0);

    // mov [esi + offsetof(Thread, m_pFrame)], edi
    psl->X86EmitIndexRegStore(kESI, offsetof(Thread, m_pFrame), kEDI);

    // leave current Thread in ESI
    psl->X86EmitReturn(0);

    // A single process-wide stub that will never unload
    RETURN psl->Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());
}

#ifdef FEATURE_INCLUDE_ALL_INTERFACES

static void STDCALL LeaveRuntimeHelperWithFrame (Thread *pThread, size_t target, Frame *pFrame)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;
    
    Thread::LeaveRuntimeThrowComplus(target);
    GCX_COOP_THREAD_EXISTS(pThread); 
    pFrame->Push(pThread);

}

static void STDCALL EnterRuntimeHelperWithFrame (Thread *pThread, Frame *pFrame)
{
    // make sure we restore the original Win32 last error before leaving this function - we are
    // called right after returning from the P/Invoke target and the error has not been saved yet
    BEGIN_PRESERVE_LAST_ERROR;

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;
    
    {
        HRESULT hr = Thread::EnterRuntimeNoThrow();
        GCX_COOP_THREAD_EXISTS(pThread);
        if (FAILED(hr))
        {
            INSTALL_UNWIND_AND_CONTINUE_HANDLER;
            ThrowHR (hr);
            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        }

        pFrame->Pop(pThread);
    }

    END_PRESERVE_LAST_ERROR;
}

// "ip" is the return address
// This function disassembles the code at the return address to determine
// how many arguments to pop off.
// Returns the number of DWORDs that should be popped off on return.

static int STDCALL GetStackSizeForVarArgCall(BYTE* ip)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    int retValue = 0;
    //BEGIN_ENTRYPOINT_VOIDRET;

    // The instruction immediately following the call may be a move into esp used for
    // P/Invoke stack resilience. For caller-pop calls it's always mov esp, [ebp-n].
    if (ip[0] == 0x8b)
    {
        if (ip[1] == 0x65)
        {
            // mov esp, [ebp+disp8]
            ip += 3;
        }
        else if (ip[1] == 0xa5)
        {
            // mov esp, [ebp+disp32]
            ip += 6;
        }
    }

    if (ip[0] == 0x81 && ip[1] == 0xc4)
    {
        // add esp, imm32
        retValue = (*(int*)&ip[2])/4;
    }
    else if (ip[0] == 0x83 && ip[1] == 0xc4)
    {
        // add esp, imm8
        retValue = ip[2]/4;
    }
    else if (ip[0] == 0x59)
    {
        // pop ecx
        retValue = 1;
    }
    else
    {
        retValue = 0;
    }
    //END_ENTRYPOINT_VOIDRET;
    return retValue;
}

void LeaveRuntimeStackProbeOnly()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        ENTRY_POINT;
    }
    CONTRACTL_END;

#ifdef FEATURE_STACK_PROBE
    RetailStackProbe(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT));
#endif
}

//-----------------------------------------------------------------------------
// Hosting stub for calls from CLR code to unmanaged code
//
// We push a LeaveRuntimeFrame, and then re-push all the arguments.
// Note that we have to support all the different native calling conventions
// viz. stdcall, thiscall, cdecl, varargs

#if 0

This is a diagramatic description of what the stub does:

                (lower addresses)

                               |                |
                               +----------------+ <--- ESP
                               |                |
                               |     copied     |
                               |   arguments    |
                               |                |
                               |                |
                               +----------------+
                               |      EDX       |
                               |      ECX       |
                               +----------------+
|                |             |   GSCookie     |
|                |             +----------------+ <--- ESI
|                |             |     vptr       |
|                |             +----------------+
|                |             |    m_Next      |
|                |             +----------------+
|                |             |      EDI       |   Scratch register
|                |             |      ESI       |   For LeaveRuntimeFrame*
|                |             |      EBX       |   For Thread*
|                |             +----------------+ <--- EBP
|                |             |      EBP       |
+----------------+ <---ESP     +----------------+
|    ret addr    |             |    ret addr    |
+----------------+             +----------------+
|                |             |                |
|   arguments    |             |   arguments    |
|                |             |                |
|                |             |                |
+----------------+             +----------------+
|                |             |                |
| caller's frame |             | caller's frame |
|                |             |                |

                (higher addresses)

  Stack on entry               Stack before the call 
   to this stub.                to unmanaged code.

#endif

//-----------------------------------------------------------------------------
// This the layout of the frame of the stub

struct StubForHostStackFrame
{
    LPVOID                  m_outgingArgs[1];
    ArgumentRegisters       m_argumentRegisters;
    GSCookie                m_gsCookie;
    LeaveRuntimeFrame       m_LeaveRuntimeFrame;
    CalleeSavedRegisters    m_calleeSavedRegisters;
    LPVOID                  m_retAddr;
    LPVOID                  m_incomingArgs[1];

public:

    // Where does the FP/EBP point to?
    static INT32 GetFPpositionOffset()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(StubForHostStackFrame, m_calleeSavedRegisters) + 
               offsetof(CalleeSavedRegisters, ebp); 
    }

    static INT32 GetFPrelOffsOfArgumentRegisters() 
    { 
        LIMITED_METHOD_CONTRACT;
        return offsetof(StubForHostStackFrame, m_argumentRegisters) - GetFPpositionOffset(); 
    }

    static INT32 GetFPrelOffsOfCalleeSavedRegisters() 
    { 
        LIMITED_METHOD_CONTRACT;
        return offsetof(StubForHostStackFrame, m_calleeSavedRegisters) - GetFPpositionOffset(); 
    }

    static INT32 GetFPrelOffsOfRetAddr() 
    { 
        LIMITED_METHOD_CONTRACT;
        return offsetof(StubForHostStackFrame, m_retAddr) - GetFPpositionOffset(); 
    }

    static INT32 GetFPrelOffsOfIncomingArgs() 
    { 
        LIMITED_METHOD_CONTRACT;
        return offsetof(StubForHostStackFrame, m_incomingArgs) - GetFPpositionOffset(); 
    }
};

static Stub *GenerateStubForHostWorker(LoaderHeap *pHeap,
                                       LPVOID pNativeTarget,    // NULL to fetch from the last pushed argument (COM)
                                       Stub *pInnerStub,        // stub to call instead of pNativeTarget, or NULL
                                       LONG dwComSlot,          // only valid if pNativeTarget is NULL
                                       WORD wStackArgumentSize, // -1 for varargs
                                       WORD wStackPopSize)      // 0 for cdecl
{
    STANDARD_VM_CONTRACT;
    
    // We need to call LeaveRuntime before the target, and EnterRuntime after the target
    CPUSTUBLINKER sl;

    sl.X86EmitPushEBPframe();
    
    // save EBX, ESI, EDI
    sl.X86EmitPushReg(kEBX);
    sl.X86EmitPushReg(kESI);
    sl.X86EmitPushReg(kEDI);

    // Frame
    sl.X86EmitPushReg(kDummyPushReg); // m_Next
    sl.X86EmitPushImm32((UINT)(size_t)LeaveRuntimeFrame::GetMethodFrameVPtr());

    // mov esi, esp;  esi is Frame
    sl.X86EmitMovRegSP(kESI);

    sl.X86EmitPushImmPtr((LPVOID)GetProcessGSCookie());

    // Save outgoing arguments on the stack
    sl.X86EmitPushReg(kECX);
    sl.X86EmitPushReg(kEDX);

    INT32 offs = 0;
    if (wStackArgumentSize == (WORD)-1)
    {
        // Re-push the return address as an argument to GetStackSizeForVarArgCall()
        // This will return the number of stack arguments (in DWORDs)
        sl.X86EmitIndexPush(kEBP, StubForHostStackFrame::GetFPrelOffsOfRetAddr());
        sl.X86EmitCall(sl.NewExternalCodeLabel((LPVOID)GetStackSizeForVarArgCall), 4);

        // We generate the following code sequence to re-push all the arguments
        //
        // Note that we cannot use "sub ESP, EAX" as ESP might jump past the
        // stack guard-page.
        //
        //      cmp EAX, 0
        // LoopTop:
        //      jz LoopDone
        //      push dword ptr[EBP + EAX*4 + 4]
        //      sub EAX, 1
        //      jmp LoopTop
        // LoopDone:
        //      ...

        sl.X86EmitCmpRegImm32(kEAX, 0);
        CodeLabel * pLoopTop = sl.EmitNewCodeLabel();
        CodeLabel * pLoopDone = sl.NewCodeLabel();
        sl.X86EmitCondJump(pLoopDone, X86CondCode::kJZ);
        sl.X86EmitBaseIndexPush(kEBP, kEAX, 4, StubForHostStackFrame::GetFPrelOffsOfIncomingArgs() - sizeof(LPVOID));
        sl.X86EmitSubReg(kEAX, 1);
        sl.X86EmitNearJump(pLoopTop);
        sl.EmitLabel(pLoopDone);
    }
    else
    {
        offs = StubForHostStackFrame::GetFPrelOffsOfIncomingArgs() + wStackArgumentSize;

        int numStackSlots = wStackArgumentSize / sizeof(LPVOID);
        for (int i = 0; i < numStackSlots; i++) {
            offs -= sizeof(LPVOID);
            sl.X86EmitIndexPush(kEBP, offs);
        }
    }

    //-------------------------------------------------------------------------
    
    // EBX has Thread*
    // X86TLSFetch_TRASHABLE_REGS will get trashed
    sl.X86EmitCurrentThreadFetch(kEBX, 0);

    if (pNativeTarget != NULL)
    {
        // push Frame
        sl.X86EmitPushReg(kESI);

        // push target
        if (pNativeTarget == (LPVOID)-1)
        {
            // target comes right above arguments
            sl.X86EmitIndexPush(kEBP, StubForHostStackFrame::GetFPrelOffsOfIncomingArgs() + wStackArgumentSize);
        }
        else
        {
            // target is fixed
            sl.X86EmitPushImm32((UINT)(size_t)pNativeTarget);
        }
    }
    else
    {
        // mov eax, [first_arg]
        // mov eax, [eax]
        // push [eax + slot_offset]
        sl.X86EmitIndexRegLoad(kEAX, kEBP, offs);
        sl.X86EmitIndexRegLoad(kEAX, kEAX, 0);
        sl.X86EmitIndexPush(kEAX, sizeof(LPVOID) * dwComSlot);

        // push Frame
        sl.X86EmitPushReg(kESI);
        // push [esp + 4]
        sl.X86EmitEspOffset(0xff, (X86Reg)6, 4);
    }

    // push Thread
    sl.X86EmitPushReg(kEBX);
    sl.X86EmitCall(sl.NewExternalCodeLabel((LPVOID)LeaveRuntimeHelperWithFrame), 0xc);

    //-------------------------------------------------------------------------
    // call NDirect
    // See diagram above to see what the stack looks like at this point

    // Restore outgoing arguments
    unsigned offsToArgRegs = StubForHostStackFrame::GetFPrelOffsOfArgumentRegisters();
    sl.X86EmitIndexRegLoad(kECX, kEBP, offsToArgRegs + offsetof(ArgumentRegisters, ECX));
    sl.X86EmitIndexRegLoad(kEDX, kEBP, offsToArgRegs + offsetof(ArgumentRegisters, EDX));
    
    if (pNativeTarget != NULL || pInnerStub != NULL)
    {
        if (pNativeTarget == (LPVOID)-1)
        {
            // mov eax, target
            sl.X86EmitIndexRegLoad(kEAX, kEBP, StubForHostStackFrame::GetFPrelOffsOfIncomingArgs() + wStackArgumentSize);
            // call eax
            sl.Emit16(X86_INSTR_CALL_EAX);
        }
        else
        {
            if (pNativeTarget == NULL)
            {
                // pop target and discard it (we go to the inner stub)
                _ASSERTE(pInnerStub != NULL);
                sl.X86EmitPopReg(kEAX);
            }

            LPVOID pTarget = (pInnerStub != NULL ? (LPVOID)pInnerStub->GetEntryPoint() : pNativeTarget);
            sl.X86EmitCall(sl.NewExternalCodeLabel(pTarget), wStackPopSize / 4);
        }
    }
    else
    {
        // pop target
        sl.X86EmitPopReg(kEAX);
        // call eax
        sl.Emit16(X86_INSTR_CALL_EAX);
    }

    //-------------------------------------------------------------------------
    // Save return value registers and call EnterRuntimeHelperWithFrame
    //
    
    sl.X86EmitPushReg(kEAX);
    sl.X86EmitPushReg(kEDX);

    // push Frame
    sl.X86EmitPushReg(kESI);
    // push Thread
    sl.X86EmitPushReg(kEBX);
    // call EnterRuntime
    sl.X86EmitCall(sl.NewExternalCodeLabel((LPVOID)EnterRuntimeHelperWithFrame), 8);

    sl.X86EmitPopReg(kEDX);
    sl.X86EmitPopReg(kEAX);

    //-------------------------------------------------------------------------
    // Tear down the frame
    //
    
    sl.EmitCheckGSCookie(kESI, LeaveRuntimeFrame::GetOffsetOfGSCookie());
    
    // lea esp, [ebp - offsToCalleeSavedRegs]
    unsigned offsToCalleeSavedRegs = StubForHostStackFrame::GetFPrelOffsOfCalleeSavedRegisters();
    sl.X86EmitIndexLea((X86Reg)kESP_Unsafe, kEBP, offsToCalleeSavedRegs);

    sl.X86EmitPopReg(kEDI);
    sl.X86EmitPopReg(kESI);
    sl.X86EmitPopReg(kEBX);

    sl.X86EmitPopReg(kEBP);

    // ret [wStackPopSize]
    sl.X86EmitReturn(wStackPopSize);

    if (pInnerStub != NULL)
    {
        // this stub calls another stub
        return sl.LinkInterceptor(pHeap, pInnerStub, pNativeTarget);
    }
    else
    {
        return sl.Link(pHeap);
    }
}


//-----------------------------------------------------------------------------
Stub *NDirectMethodDesc::GenerateStubForHost(LPVOID pNativeTarget, Stub *pInnerStub)
{
    STANDARD_VM_CONTRACT;
    
    // We need to call LeaveRuntime before the target, and EnterRuntime after the target

    if (IsQCall())
    {
        // We need just the stack probe for QCalls
        CPUSTUBLINKER sl;
        sl.X86EmitCall(sl.NewExternalCodeLabel((LPVOID)LeaveRuntimeStackProbeOnly), 0);

        sl.X86EmitNearJump(sl.NewExternalCodeLabel((LPVOID)pNativeTarget));

        return sl.Link(GetLoaderAllocator()->GetStubHeap());
    }

    WORD wArgSize = (IsVarArgs() ? (WORD)-1 : GetStackArgumentSize());
    WORD wPopSize = ((IsStdCall() || IsThisCall()) ? GetStackArgumentSize() : 0);

    return GenerateStubForHostWorker(GetDomain()->GetLoaderAllocator()->GetStubHeap(),
                                     pNativeTarget,
                                     pInnerStub,
                                     0,
                                     wArgSize,
                                     wPopSize);
}


#ifdef FEATURE_COMINTEROP

//-----------------------------------------------------------------------------
Stub *ComPlusCallInfo::GenerateStubForHost(LoaderHeap *pHeap, Stub *pInnerStub)
{
    STANDARD_VM_CONTRACT;
    
    WORD wArgSize = GetStackArgumentSize();

    return GenerateStubForHostWorker(pHeap,
                                     NULL,
                                     pInnerStub,
                                     m_cachedComSlot,
                                     wArgSize,
                                     wArgSize); //  always stdcall
}

#endif // FEATURE_COMINTEROP

//-----------------------------------------------------------------------------
// static
Stub *COMDelegate::GenerateStubForHost(MethodDesc *pInvokeMD, MethodDesc *pStubMD, LPVOID pNativeTarget, Stub *pInnerStub)
{
    STANDARD_VM_CONTRACT;
    
    // get unmanaged calling convention from pInvokeMD's metadata
    PInvokeStaticSigInfo sigInfo(pInvokeMD);
    CorPinvokeMap callConv = sigInfo.GetCallConv();

    WORD wArgSize = pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize();
    WORD wPopSize = (callConv == pmCallConvCdecl ? 0 : wArgSize);

    return GenerateStubForHostWorker(NULL, // we want to free this stub when the delegate dies
                                     pNativeTarget,
                                     pInnerStub,
                                     0,
                                     wArgSize,
                                     wPopSize);
}

//-----------------------------------------------------------------------------
// static
Stub *NDirect::GenerateStubForHost(Module *pModule, CorUnmanagedCallingConvention callConv, WORD wArgSize)
{
    STANDARD_VM_CONTRACT;

    // This one is for unmanaged CALLI where the target is passed as last argument
    // (first pushed to stack)

    WORD wPopSize = (callConv == IMAGE_CEE_CS_CALLCONV_C ? 0 : (wArgSize + STACK_ELEM_SIZE));

    return GenerateStubForHostWorker(pModule->GetDomain()->GetLoaderAllocator()->GetStubHeap(),
                                     (LPVOID)-1,
                                     NULL,
                                     0,
                                     wArgSize,
                                     wPopSize);
}

#endif // FEATURE_INCLUDE_ALL_INTERFACES


#ifdef MDA_SUPPORTED

//-----------------------------------------------------------------------------
Stub *NDirectMethodDesc::GenerateStubForMDA(LPVOID pNativeTarget, Stub *pInnerStub, BOOL fCalledByStub)
{
    STANDARD_VM_CONTRACT;

    CPUSTUBLINKER sl;
    sl.X86EmitPushEBPframe();

    DWORD callConv = (DWORD)(IsThisCall() ? pmCallConvThiscall : (IsStdCall() ? pmCallConvStdcall : pmCallConvCdecl));
    _ASSERTE((callConv & StackImbalanceCookie::HAS_FP_RETURN_VALUE) == 0);

    MetaSig msig(this);
    if (msig.HasFPReturn())
    {
        // check for the HRESULT swapping impl flag
        DWORD dwImplFlags;
        IfFailThrow(GetMDImport()->GetMethodImplProps(GetMemberDef(), NULL, &dwImplFlags));

        if (dwImplFlags & miPreserveSig)
        {
            // pass a flag to PInvokeStackImbalanceHelper that it should save & restore FPU return value
            callConv |= StackImbalanceCookie::HAS_FP_RETURN_VALUE;
        }
    }

    // init StackImbalanceCookie
    sl.X86EmitPushReg(kEAX);       // m_dwSavedEsp (just making space)
    sl.X86EmitPushImm32(callConv); // m_callConv

    if (IsVarArgs())
    {
        // Re-push the return address as an argument to GetStackSizeForVarArgCall()
        if (fCalledByStub)
        {
            // We will be called by another stub that doesn't know the stack size,
            // so we need to skip a frame to get to the managed caller.
            sl.X86EmitIndexRegLoad(kEAX, kEBP, 0);
            sl.X86EmitIndexPush(kEAX, 4);
        }
        else
        {
            sl.X86EmitIndexPush(kEBP, 4);
        }

        // This will return the number of stack arguments (in DWORDs)
        sl.X86EmitCall(sl.NewExternalCodeLabel((LPVOID)GetStackSizeForVarArgCall), 4);
        
        // shl eax,2
        sl.Emit16(0xe0c1);
        sl.Emit8(0x02);
        
        sl.X86EmitPushReg(kEAX); // m_dwStackArgSize
    }
    else
    {
        sl.X86EmitPushImm32(GetStackArgumentSize()); // m_dwStackArgSize
    }

    LPVOID pTarget = (pInnerStub != NULL ? (LPVOID)pInnerStub->GetEntryPoint() : pNativeTarget);
    sl.X86EmitPushImmPtr(pTarget);       // m_pTarget
    sl.X86EmitPushImmPtr(this);          // m_pMD

    // stack layout at this point

    // |          ...          |
    // |    stack arguments    | EBP + 8
    // +-----------------------+
    // |    return address     | EBP + 4
    // +-----------------------+
    // |      saved EBP        | EBP + 0
    // +-----------------------+
    // | SIC::m_dwSavedEsp     |
    // | SIC::m_callConv       |
    // | SIC::m_dwStackArgSize |
    // | SIC::m_pTarget        |
    // | SIC::m_pMD            | EBP - 20
    // ------------------------

    // call the helper
    sl.X86EmitCall(sl.NewExternalCodeLabel(PInvokeStackImbalanceHelper), sizeof(StackImbalanceCookie));

    //  pop StackImbalanceCookie
    sl.X86EmitMovSPReg(kEBP);

    sl.X86EmitPopReg(kEBP);
    sl.X86EmitReturn((IsStdCall() || IsThisCall()) ? GetStackArgumentSize() : 0);

    if (pInnerStub)
    {
        return sl.LinkInterceptor(GetLoaderAllocator()->GetStubHeap(), pInnerStub, pNativeTarget);
    }
    else
    {
        return sl.Link(GetLoaderAllocator()->GetStubHeap());
    }
}

//-----------------------------------------------------------------------------
// static
Stub *COMDelegate::GenerateStubForMDA(MethodDesc *pInvokeMD, MethodDesc *pStubMD, LPVOID pNativeTarget, Stub *pInnerStub)
{
    STANDARD_VM_CONTRACT;

    WORD wStackArgSize = pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize();

    // get unmanaged calling convention from pInvokeMD's metadata
    PInvokeStaticSigInfo sigInfo(pInvokeMD);
    DWORD callConv = (DWORD)sigInfo.GetCallConv();
    _ASSERTE((callConv & StackImbalanceCookie::HAS_FP_RETURN_VALUE) == 0);

    MetaSig msig(pInvokeMD);
    if (msig.HasFPReturn())
    {
        // pass a flag to PInvokeStackImbalanceHelper that it should save & restore FPU return value
        callConv |= StackImbalanceCookie::HAS_FP_RETURN_VALUE;
    }

    CPUSTUBLINKER sl;
    sl.X86EmitPushEBPframe();

    LPVOID pTarget = (pInnerStub != NULL ? (LPVOID)pInnerStub->GetEntryPoint() : pNativeTarget);

    // init StackImbalanceCookie
    sl.X86EmitPushReg(kEAX);             // m_dwSavedEsp (just making space)
    sl.X86EmitPushImm32(callConv);       // m_callConv
    sl.X86EmitPushImm32(wStackArgSize);  // m_dwStackArgSize
    sl.X86EmitPushImmPtr(pTarget);       // m_pTarget
    sl.X86EmitPushImmPtr(pInvokeMD);     // m_pMD

    // stack layout at this point

    // |          ...          |
    // |    stack arguments    | EBP + 8
    // +-----------------------+
    // |    return address     | EBP + 4
    // +-----------------------+
    // |      saved EBP        | EBP + 0
    // +-----------------------+
    // | SIC::m_dwSavedEsp     |
    // | SIC::m_callConv       |
    // | SIC::m_dwStackArgSize |
    // | SIC::m_pTarget        |
    // | SIC::m_pMD            | EBP - 20
    // ------------------------

    // call the helper
    sl.X86EmitCall(sl.NewExternalCodeLabel(PInvokeStackImbalanceHelper), sizeof(StackImbalanceCookie));

    //  pop StackImbalanceCookie
    sl.X86EmitMovSPReg(kEBP);

    sl.X86EmitPopReg(kEBP);
    sl.X86EmitReturn(callConv == pmCallConvCdecl ? 0 : wStackArgSize);

    if (pInnerStub != NULL)
    {
        return sl.LinkInterceptor(pInnerStub, pNativeTarget);
    }
    else
    {
        return sl.Link(); // don't use loader heap as we want to be able to free the stub
    }
}

#endif // MDA_SUPPORTED

extern "C" VOID STDCALL StubRareEnableWorker(Thread *pThread)
{
    WRAPPER_NO_CONTRACT;

    //printf("RareEnable\n");
    pThread->RareEnablePreemptiveGC();
}




// Disable when calling into managed code from a place that fails via Exceptions
extern "C" VOID STDCALL StubRareDisableTHROWWorker(Thread *pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // Do not add a CONTRACT here.  We haven't set up SEH.  We rely
    // on HandleThreadAbort and COMPlusThrowBoot dealing with this situation properly.

    // WARNING!!!!
    // when we start executing here, we are actually in cooperative mode.  But we
    // haven't synchronized with the barrier to reentry yet.  So we are in a highly
    // dangerous mode.  If we call managed code, we will potentially be active in
    // the GC heap, even as GC's are occuring!

    // Check for ShutDown scenario.  This happens only when we have initiated shutdown 
    // and someone is trying to call in after the CLR is suspended.  In that case, we
    // must either raise an unmanaged exception or return an HRESULT, depending on the
    // expectations of our caller.
    if (!CanRunManagedCode())
    {
        // DO NOT IMPROVE THIS EXCEPTION!  It cannot be a managed exception.  It
        // cannot be a real exception object because we cannot execute any managed
        // code here.
        pThread->m_fPreemptiveGCDisabled = 0;
        COMPlusThrowBoot(E_PROCESS_SHUTDOWN_REENTRY);
    }

    // We must do the following in this order, because otherwise we would be constructing
    // the exception for the abort without synchronizing with the GC.  Also, we have no
    // CLR SEH set up, despite the fact that we may throw a ThreadAbortException.
    pThread->RareDisablePreemptiveGC();
    pThread->HandleThreadAbort();
}

// Note that this logic is copied below, in PopSEHRecords
__declspec(naked)
VOID __cdecl PopSEHRecords(LPVOID pTargetSP)
{
    // No CONTRACT possible on naked functions
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    __asm{
        mov     ecx, [esp+4]        ;; ecx <- pTargetSP
        mov     eax, fs:[0]         ;; get current SEH record
  poploop:
        cmp     eax, ecx
        jge     done
        mov     eax, [eax]          ;; get next SEH record
        jmp     poploop
  done:
        mov     fs:[0], eax
        retn
    }
}

//////////////////////////////////////////////////////////////////////////////
//
// JITInterface
//
//////////////////////////////////////////////////////////////////////////////

/*********************************************************************/
#ifdef EnC_SUPPORTED
#pragma warning (disable : 4731)
void ResumeAtJit(PCONTEXT pContext, LPVOID oldESP)
{
    // No CONTRACT here, because we can't run the risk of it pushing any SEH into the
    // current method.

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef _DEBUG
    DWORD curESP;
    __asm mov curESP, esp
#endif

    if (oldESP)
    {
        _ASSERTE(curESP < (DWORD)(size_t)oldESP);
        // should have popped the SEH records by now as stack has been overwritten
        _ASSERTE(GetCurrentSEHRecord() > oldESP);
    }

    // For the "push Eip, ..., ret"
    _ASSERTE(curESP < pContext->Esp - sizeof(DWORD));
    pContext->Esp -= sizeof(DWORD);

    __asm {
        mov     ebp, pContext

        // Push Eip onto the targetESP, so that the final "ret" will consume it
        mov     ecx, [ebp]CONTEXT.Esp
        mov     edx, [ebp]CONTEXT.Eip
        mov     [ecx], edx

        // Restore all registers except Esp, Ebp, Eip
        mov     eax, [ebp]CONTEXT.Eax
        mov     ebx, [ebp]CONTEXT.Ebx
        mov     ecx, [ebp]CONTEXT.Ecx
        mov     edx, [ebp]CONTEXT.Edx
        mov     esi, [ebp]CONTEXT.Esi
        mov     edi, [ebp]CONTEXT.Edi

        push    [ebp]CONTEXT.Esp  // pContext->Esp is (targetESP-sizeof(DWORD))
        push    [ebp]CONTEXT.Ebp
        pop     ebp
        pop     esp

        // esp is (targetESP-sizeof(DWORD)), and [esp] is the targetEIP.
        // The ret will set eip to targetEIP and esp will be automatically
        // incremented to targetESP

        ret
    }
}
#pragma warning (default : 4731)
#endif // !EnC_SUPPORTED


#pragma warning(push)
#pragma warning(disable: 4035)
DWORD getcpuid(DWORD arg, unsigned char result[16])
{
    LIMITED_METHOD_CONTRACT

    __asm
    {
        push    ebx
        push    esi
        mov     eax, arg
        cpuid
        mov     esi, result
        mov     [esi+ 0], eax
        mov     [esi+ 4], ebx
        mov     [esi+ 8], ecx
        mov     [esi+12], edx
        pop     esi
        pop     ebx
    }
}

// The following function uses Deterministic Cache Parameter leafs to determine the cache hierarchy information on Prescott & Above platforms. 
//  This function takes 3 arguments:
//     Arg1 is an input to ECX. Used as index to specify which cache level to return infoformation on by CPUID.
//     Arg2 is an input to EAX. For deterministic code enumeration, we pass in 4H in arg2.
//     Arg3 is a pointer to the return buffer
//   No need to check whether or not CPUID is supported because we have already called CPUID with success to come here.

DWORD getextcpuid(DWORD arg1, DWORD arg2, unsigned char result[16])
{
    LIMITED_METHOD_CONTRACT

    __asm
    {
        push    ebx
        push    esi
        mov     ecx, arg1
        mov     eax, arg2
        cpuid
        mov     esi, result
        mov     [esi+ 0], eax
        mov     [esi+ 4], ebx
        mov     [esi+ 8], ecx
        mov     [esi+12], edx
        pop     esi
        pop     ebx
    }
}

#pragma warning(pop)


// This function returns the number of logical processors on a given physical chip.  If it cannot
// determine the number of logical cpus, or the machine is not populated uniformly with the same
// type of processors, this function returns 1.
DWORD GetLogicalCpuCount()
{
    // No CONTRACT possible because GetLogicalCpuCount uses SEH

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

    static DWORD val = 0;

    // cache value for later re-use
    if (val)
    {
        return val;
    }

    struct Param : DefaultCatchFilterParam
    {
        DWORD retVal;
    } param;
    param.pv = COMPLUS_EXCEPTION_EXECUTE_HANDLER;
    param.retVal = 1;

    PAL_TRY(Param *, pParam, &param)
    {
        unsigned char buffer[16];

        DWORD maxCpuId = getcpuid(0, buffer);

        if (maxCpuId < 1)
            goto lDone;

        DWORD* dwBuffer = (DWORD*)buffer;

        if (dwBuffer[1] == 'uneG') {
            if (dwBuffer[3] == 'Ieni') {
                if (dwBuffer[2] == 'letn')  {  // get SMT/multicore enumeration for Intel EM64T 

                    // TODO: Currently GetLogicalCpuCountFromOS() and GetLogicalCpuCountFallback() are broken on 
                    // multi-core processor, but we never call into those two functions since we don't halve the
                    // gen0size when it's prescott and above processor. We keep the old version here for earlier
                    // generation system(Northwood based), perf data suggests on those systems, halve gen0 size 
                    // still boost the performance(ex:Biztalk boosts about 17%). So on earlier systems(Northwood) 
                    // based, we still go ahead and halve gen0 size.  The logic in GetLogicalCpuCountFromOS() 
                    // and GetLogicalCpuCountFallback() works fine for those earlier generation systems. 
                    // If it's a Prescott and above processor or Multi-core, perf data suggests not to halve gen0 
                    // size at all gives us overall better performance. 
                    // This is going to be fixed with a new version in orcas time frame. 

                    if( (maxCpuId > 3) && (maxCpuId < 0x80000000) ) 
                        goto lDone;

                    val = GetLogicalCpuCountFromOS(); //try to obtain HT enumeration from OS API
                    if (val )
                    {
                        pParam->retVal = val;     // OS API HT enumeration successful, we are Done        
                        goto lDone;
                    }

                    val = GetLogicalCpuCountFallback();    // OS API failed, Fallback to HT enumeration using CPUID
                    if( val )
                        pParam->retVal = val;
                }
            }
        }
lDone: ;
    }
    PAL_EXCEPT_FILTER(DefaultCatchFilter)
    {
    }
    PAL_ENDTRY

    if (val == 0)
    {
        val = param.retVal;
    }

    return param.retVal;
}

void UMEntryThunkCode::Encode(BYTE* pTargetCode, void* pvSecretParam)
{
    LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG
    m_alignpad[0] = X86_INSTR_INT3;
    m_alignpad[1] = X86_INSTR_INT3;
#endif // _DEBUG
    m_movEAX     = X86_INSTR_MOV_EAX_IMM32;
    m_uet        = pvSecretParam;
    m_jmp        = X86_INSTR_JMP_REL32;
    m_execstub   = (BYTE*) ((pTargetCode) - (4+((BYTE*)&m_execstub)));

    FlushInstructionCache(GetCurrentProcess(),GetEntryPoint(),sizeof(UMEntryThunkCode));
}

UMEntryThunk* UMEntryThunk::Decode(LPVOID pCallback)
{
    LIMITED_METHOD_CONTRACT;

    if (*((BYTE*)pCallback) != X86_INSTR_MOV_EAX_IMM32 ||
        ( ((size_t)pCallback) & 3) != 2) {
        return NULL;
    }
    return *(UMEntryThunk**)( 1 + (BYTE*)pCallback );
}

BOOL DoesSlotCallPrestub(PCODE pCode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION(pCode != NULL);
        PRECONDITION(pCode != GetPreStubEntryPoint());
    } CONTRACTL_END;

    // x86 has the following possible sequences for prestub logic:
    // 1. slot -> temporary entrypoint -> prestub
    // 2. slot -> precode -> prestub
    // 3. slot -> precode -> jumprel32 (NGEN case) -> prestub

#ifdef HAS_COMPACT_ENTRYPOINTS
    if (MethodDescChunk::GetMethodDescFromCompactEntryPoint(pCode, TRUE) != NULL)
    {
        return TRUE;
    }
#endif // HAS_COMPACT_ENTRYPOINTS

    if (!IS_ALIGNED(pCode, PRECODE_ALIGNMENT))
    {
        return FALSE;
    }

#ifdef HAS_FIXUP_PRECODE
    if (*PTR_BYTE(pCode) == X86_INSTR_CALL_REL32)
    {
        // Note that call could have been patched to jmp in the meantime
        pCode = rel32Decode(pCode+1);

        // NGEN case
        if (*PTR_BYTE(pCode) == X86_INSTR_JMP_REL32) {
            pCode = rel32Decode(pCode+1);
        }

        return pCode == (TADDR)PrecodeFixupThunk;
    }
#endif

    if (*PTR_BYTE(pCode) != X86_INSTR_MOV_EAX_IMM32 ||
        *PTR_BYTE(pCode+5) != X86_INSTR_MOV_RM_R ||
        *PTR_BYTE(pCode+7) != X86_INSTR_JMP_REL32)
    {
        return FALSE;
    }
    pCode = rel32Decode(pCode+8);

    // NGEN case
    if (*PTR_BYTE(pCode) == X86_INSTR_JMP_REL32) {
        pCode = rel32Decode(pCode+1);
    }

    return pCode == GetPreStubEntryPoint();
}

//==========================================================================================
// In NGen image, virtual slots inherited from cross-module dependencies point to jump thunks.
// These jump thunk initially point to VirtualMethodFixupStub which transfers control here.
// This method 'VirtualMethodFixupWorker' will patch the jump thunk to point to the actual
// inherited method body after we have execute the precode and a stable entry point.
//
EXTERN_C PVOID STDCALL VirtualMethodFixupWorker(Object * pThisPtr,  CORCOMPILE_VIRTUAL_IMPORT_THUNK *pThunk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    _ASSERTE(pThisPtr != NULL);
    VALIDATEOBJECT(pThisPtr);

    MethodTable * pMT = pThisPtr->GetTrueMethodTable();

    WORD slotNumber = pThunk->slotNum;
    _ASSERTE(slotNumber != (WORD)-1);

    PCODE pCode = pMT->GetRestoredSlot(slotNumber);

    if (!DoesSlotCallPrestub(pCode))
    {
        // Skip fixup precode jump for better perf
        PCODE pDirectTarget = Precode::TryToSkipFixupPrecode(pCode);
        if (pDirectTarget != NULL)
            pCode = pDirectTarget;

        INT64 oldValue = *(INT64*)pThunk;
        BYTE* pOldValue = (BYTE*)&oldValue;

        if (pOldValue[0] == X86_INSTR_CALL_REL32)
        {
            INT64 newValue = oldValue;
            BYTE* pNewValue = (BYTE*)&newValue;
            pNewValue[0] = X86_INSTR_JMP_REL32;

            INT_PTR pcRelOffset = (BYTE*)pCode - &pThunk->callJmp[5];
            *(INT32 *)(&pNewValue[1]) = (INT32) pcRelOffset;

            _ASSERTE(IS_ALIGNED(pThunk, sizeof(INT64)));
            if (EnsureWritableExecutablePagesNoThrow(pThunk, sizeof(INT64)))
                FastInterlockCompareExchangeLong((INT64*)pThunk, newValue, oldValue);

            FlushInstructionCache(GetCurrentProcess(), pThunk, 8);
        }
    }

    return PVOID(pCode);
}


#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//

#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    SIZE_T cb = size; \
    SIZE_T cbAligned = ALIGN_UP(cb, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * pStart = (BYTE *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(cbAligned, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * p = pStart;

#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(pStart + cb == p); \
    while (p < pStart + cbAligned) *p++ = X86_INSTR_INT3; \
    ClrFlushInstructionCache(pStart, cbAligned); \
    return (PCODE)pStart

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(10);

    *p++ = 0xB9; // mov ecx, XXXXXX
    *(INT32 *)p = (INT32)arg;
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

void DynamicHelpers::EmitHelperWithArg(BYTE*& p, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        PRECONDITION(p != NULL && target != NULL);
    }
    CONTRACTL_END;

    // Move an an argument into the second argument register and jump to a target function.

    *p++ = 0xBA; // mov edx, XXXXXX
    *(INT32 *)p = (INT32)arg;
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target);
    p += 4;
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(10);

    EmitHelperWithArg(p, pAllocator, arg, target);

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(15);

    *p++ = 0xB9; // mov ecx, XXXXXX
    *(INT32 *)p = (INT32)arg;
    p += 4;

    *p++ = 0xBA; // mov edx, XXXXXX
    *(INT32 *)p = (INT32)arg2;
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(12);

    *(UINT16 *)p = 0xD18B; // mov edx, ecx
    p += 2;

    *p++ = 0xB9; // mov ecx, XXXXXX
    *(INT32 *)p = (INT32)arg;
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturn(LoaderAllocator * pAllocator)
{
    BEGIN_DYNAMIC_HELPER_EMIT(1);

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    BEGIN_DYNAMIC_HELPER_EMIT(6);

    *p++ = 0xB8; // mov eax, XXXXXX
    *(INT32 *)p = (INT32)arg;
    p += 4;

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    BEGIN_DYNAMIC_HELPER_EMIT((offset != 0) ? 9 : 6);

    *p++ = 0xA1; // mov eax, [XXXXXX]
    *(INT32 *)p = (INT32)arg;
    p += 4;

    if (offset != 0)
    {
        // add eax, <offset>
        *p++ = 0x83;
        *p++ = 0xC0;
        *p++ = offset;
    }

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(12);

    // pop eax
    *p++ = 0x58;

    // push arg
    *p++ = 0x68;
    *(INT32 *)p = arg;
    p += 4;

    // push eax
    *p++ = 0x50;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(17);

    // pop eax
    *p++ = 0x58;

    // push arg
    *p++ = 0x68;
    *(INT32 *)p = arg;
    p += 4;

    // push arg2
    *p++ = 0x68;
    *(INT32 *)p = arg2;
    p += 4;

    // push eax
    *p++ = 0x50;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    STANDARD_VM_CONTRACT;

    PCODE helperAddress = (pLookup->helper == CORINFO_HELP_RUNTIMEHANDLE_METHOD ?
        GetEEFuncEntryPoint(JIT_GenericHandleMethodWithSlotAndModule) :
        GetEEFuncEntryPoint(JIT_GenericHandleClassWithSlotAndModule));

    GenericHandleArgs * pArgs = (GenericHandleArgs *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(sizeof(GenericHandleArgs), DYNAMIC_HELPER_ALIGNMENT);
    pArgs->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
    pArgs->signature = pLookup->signature;
    pArgs->module = (CORINFO_MODULE_HANDLE)pModule;

    // It's available only via the run-time helper function
    if (pLookup->indirections == CORINFO_USEHELPER)
    {
        BEGIN_DYNAMIC_HELPER_EMIT(10);

        // ecx contains the generic context parameter
        // mov edx,pArgs
        // jmp helperAddress
        EmitHelperWithArg(p, pAllocator, (TADDR)pArgs, helperAddress);

        END_DYNAMIC_HELPER_EMIT();
    }
    else
    {
        int indirectionsSize = 0;
        for (WORD i = 0; i < pLookup->indirections; i++)
            indirectionsSize += (pLookup->offsets[i] >= 0x80 ? 6 : 3);

        int codeSize = indirectionsSize + (pLookup->testForNull ? 21 : 3);

        BEGIN_DYNAMIC_HELPER_EMIT(codeSize);

        if (pLookup->testForNull)
        {
            // ecx contains the generic context parameter. Save a copy of it in the eax register
            // mov eax,ecx
            *(UINT16*)p = 0xc889; p += 2;
        }

        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            // mov ecx,qword ptr [ecx+offset]
            if (pLookup->offsets[i] >= 0x80)
            {
                *(UINT16*)p = 0x898b; p += 2;
                *(UINT32*)p = (UINT32)pLookup->offsets[i]; p += 4;
            }
            else
            {
                *(UINT16*)p = 0x498b; p += 2;
                *p++ = (BYTE)pLookup->offsets[i];
            }
        }

        // No null test required
        if (!pLookup->testForNull)
        {
            // No fixups needed for R2R

            // mov eax,ecx
            *(UINT16*)p = 0xc889; p += 2;
            *p++ = 0xC3;    // ret
        }
        else
        {
            // ecx contains the value of the dictionary slot entry

            _ASSERTE(pLookup->indirections != 0);

            // test ecx,ecx
            *(UINT16*)p = 0xc985; p += 2;

            // je 'HELPER_CALL' (a jump of 3 bytes)
            *(UINT16*)p = 0x0374; p += 2;

            // mov eax,ecx
            *(UINT16*)p = 0xc889; p += 2;
            *p++ = 0xC3;    // ret

            // 'HELPER_CALL'
            {
                // Put the generic context back into rcx (was previously saved in eax)
                // mov ecx,eax
                *(UINT16*)p = 0xc189; p += 2;

                // mov edx,pArgs
                // jmp helperAddress
                EmitHelperWithArg(p, pAllocator, (TADDR)pArgs, helperAddress);
            }
        }

        END_DYNAMIC_HELPER_EMIT();
    }
}

#endif // FEATURE_READYTORUN


#endif // DACCESS_COMPILE
