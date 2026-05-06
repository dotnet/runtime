// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Emulate hardware single-step on ARM.
//

#include "common.h"
#include "armsinglestepper.h"

//
// ITState methods.
//

ITState::ITState()
{
#ifdef _DEBUG
    m_fValid = false;
#endif
}

// Must call Get() (or Init()) to initialize this instance from a specific context before calling any other
// (non-static) method.
void ITState::Get(T_CONTEXT *pCtx)
{
    m_bITState = (BYTE)((BitExtract((WORD)pCtx->Cpsr, 15, 10) << 2) |
                        BitExtract((WORD)(pCtx->Cpsr >> 16), 10, 9));
#ifdef _DEBUG
    m_fValid = true;
#endif
}

// Must call Init() (or Get()) to initialize this instance from a raw byte value before calling any other
// (non-static) method.
void ITState::Init(BYTE bState)
{
    m_bITState = bState;
#ifdef _DEBUG
    m_fValid = true;
#endif
}

// Does the current IT state indicate we're executing within an IT block?
bool ITState::InITBlock()
{
    _ASSERTE(m_fValid);
    return (m_bITState & 0x1f) != 0;
}

// Only valid within an IT block. Returns the condition code which will be evaluated for the current
// instruction.
DWORD ITState::CurrentCondition()
{
    _ASSERTE(m_fValid);
    _ASSERTE(InITBlock());
    return BitExtract(m_bITState, 7, 4);
}

// Transition the IT state to that for the next instruction.
void ITState::Advance()
{
    _ASSERTE(m_fValid);
    if ((m_bITState & 0x7) == 0)
        m_bITState = 0;
    else
        m_bITState = (m_bITState & 0xe0) | ((m_bITState << 1) & 0x1f);
}

// Write the current IT state back into the given context.
void ITState::Set(T_CONTEXT *pCtx)
{
    _ASSERTE(m_fValid);

    Clear(pCtx);
    pCtx->Cpsr |= BitExtract(m_bITState, 1, 0) << 25;
    pCtx->Cpsr |= BitExtract(m_bITState, 7, 2) << 10;
}

// Clear IT state (i.e. force execution to be outside of an IT block) in the given context.
/* static */ void ITState::Clear(T_CONTEXT *pCtx)
{
    pCtx->Cpsr &= 0xf9ff03ff;
}

//
// ArmSingleStepper methods.
//
ArmSingleStepper::ArmSingleStepper()
    : m_originalPc(0), m_targetPc(0), m_rgCode(0), m_state(Disabled),
      m_fEmulatedITInstruction(false), m_fRedirectedPc(false), m_fEmulate(false), m_fBypass(false), m_fSkipIT(false)
{
     m_opcodes[0] = 0;
     m_opcodes[1] = 0;
}

ArmSingleStepper::~ArmSingleStepper()
{
#if !defined(DACCESS_COMPILE)
    SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->BackoutMem(m_rgCode, kMaxCodeBuffer * sizeof(WORD));
#endif
}

void ArmSingleStepper::Init()
{
#if !defined(DACCESS_COMPILE)
    if (m_rgCode == NULL)
    {
        m_rgCode = (WORD *)(void *)SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->AllocMem(S_SIZE_T(kMaxCodeBuffer * sizeof(WORD)));
    }
#endif
}

// Given the context with which a thread will be resumed, modify that context such that resuming the thread
// will execute a single instruction before raising an EXCEPTION_BREAKPOINT. The thread context must be
// cleaned up via the Fixup method below before any further exception processing can occur (at which point the
// caller can behave as though EXCEPTION_SINGLE_STEP was raised).
void ArmSingleStepper::Enable()
{
    _ASSERTE(m_state != Applied);

    if (m_state == Enabled)
    {
        // We allow single-stepping to be enabled multiple times before the thread is resumed, but we require
        // that the thread state is the same in all cases (i.e. additional step requests are treated as
        // no-ops).
        _ASSERTE(!m_fBypass);
        _ASSERTE(m_opcodes[0] == 0);
        _ASSERTE(m_opcodes[1] == 0);

        return;
    }

    LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Enable\n"));

    m_fBypass = false;
    m_opcodes[0] = 0;
    m_opcodes[1] = 0;
    m_state = Enabled;
}

void ArmSingleStepper::Bypass(DWORD ip, WORD opcode1, WORD opcode2)
{
    _ASSERTE(m_state != Applied);

    if (m_state == Enabled)
    {
        // We allow single-stepping to be enabled multiple times before the thread is resumed, but we require
        // that the thread state is the same in all cases (i.e. additional step requests are treated as
        // no-ops).
        if (m_fBypass)
        {
            _ASSERTE(m_opcodes[0] == opcode1);
            _ASSERTE(m_opcodes[1] == opcode2);
            _ASSERTE(m_originalPc == ip);
            return;
        }
    }


    LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Bypass(pc=%x, opcode=%x %x)\n", (DWORD)ip, (DWORD)opcode1, (DWORD)opcode2));

    m_fBypass = true;
    m_originalPc = ip;
    m_opcodes[0] = opcode1;
    m_opcodes[1] = opcode2;
    m_state = Enabled;
}

void ArmSingleStepper::Apply(T_CONTEXT *pCtx)
{
    if (m_rgCode == NULL)
    {
        Init();

        // OOM.  We will simply ignore the single step.
        if (m_rgCode == NULL)
            return;
    }

    _ASSERTE(pCtx != NULL);

    if (!m_fBypass)
    {
        DWORD pc = ((DWORD)pCtx->Pc) & ~THUMB_CODE;
        m_opcodes[0] = *(WORD*)pc;
        if (Is32BitInstruction( m_opcodes[0]))
            m_opcodes[1] = *(WORD*)(pc+2);
    }

    WORD opcode1 = m_opcodes[0];
    WORD opcode2 = m_opcodes[1];

    LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Apply(pc=%x, opcode=%x %x)\n",
                                  (DWORD)pCtx->Pc, (DWORD)opcode1, (DWORD)opcode2));

#ifdef _DEBUG
    // Make sure that we aren't trying to step through our own buffer.  If this asserts, something is horribly
    // wrong with the debugging layer.  Likely GetManagedStoppedCtx is retrieving a Pc that points to our
    // buffer, even though the single stepper is disabled.
    DWORD codestart = (DWORD)(DWORD_PTR)m_rgCode;
    DWORD codeend = codestart + (kMaxCodeBuffer * sizeof(WORD));
    _ASSERTE((pCtx->Pc < codestart) || (pCtx->Pc >= codeend));
#endif

    // All stepping is simulated using a breakpoint instruction. Since other threads are not suspended while
    // we step, we avoid race conditions and other complexity by redirecting the thread into a thread-local
    // execution buffer. We can either copy the instruction we wish to step over into the buffer followed by a
    // breakpoint or we can emulate the instruction (which is useful for instruction that depend on the value
    // of the PC or that branch or call to an alternate location). Even in the emulation case we still
    // redirect execution into the buffer and insert a breakpoint; this simplifies our interface since the
    // rest of the runtime is not set up to expect single stepping to occur inline. Instead there is always a
    // 1:1 relationship between setting the single-step mode and receiving an exception once the thread is
    // restarted.
    //
    // There are two parts to the emulation:
    //  1) In this method we either emulate the instruction (updating the input thread context as a result) or
    //     copy the single instruction into the execution buffer. In both cases we copy a breakpoint into the
    //     execution buffer as well then update the thread context to redirect execution into this buffer.
    //  2) In the runtime's first chance vectored exception handler we perform the necessary fixups to make
    //     the exception look like the result of a single step. This includes resetting the PC to its correct
    //     value (either the instruction following the stepped instruction or the target PC cached in this
    //     object when we emulated an instruction that alters the PC). It also involves switching
    //     EXCEPTION_BREAKPOINT to EXCEPTION_SINGLE_STEP.
    //
    // If we encounter an exception while emulating an instruction (currently this can only happen if we A/V
    // trying to read a value from memory) then we abandon emulation and fall back to the copy instruction
    // mechanism. When we run the execution buffer the exception should be raised and handled as normal (we
    // still perform context fixup in this case but we don't attempt to alter any exception code other than
    // EXCEPTION_BREAKPOINT to EXCEPTION_SINGLE_STEP). There is a very small timing window here where another
    // thread could alter memory protections to avoid the A/V when we run the instruction for real but the
    // liklihood of this happening (in managed code no less) is judged sufficiently small that it's not worth
    // the alternate solution (where we'd have to set the thread up to raise an exception with exactly the
    // right thread context).
    //
    // Matters are complicated by the ARM IT instruction (upto four following instructions are executed
    // conditionally based on a single condition or its negation). The issues are that the current instruction
    // may be rendered into a no-op or that a breakpoint immediately following the current instruction may not
    // be executed. To simplify matters we may modify the IT state to force our instructions to execute. We
    // cache the real state and re-apply it along with the rest of our fixups when handling the breakpoint
    // exception. Note that when executing general instructions we can't simply disable any IT state since
    // many instructions alter their behavior depending on whether they're executing within an IT block
    // (mostly it's used to determine whether these instructions set condition flags or not).

    // Cache thread's initial PC and IT state since we'll overwrite them as part of the emulation and we need
    // to get back to the correct values at fixup time. We also cache a target PC (set below) since some
    // instructions will set the PC directly or otherwise make it difficult for us to compute the final PC
    // from the original. We still need the original PC however since this is the one we'll use if an
    // exception (other than a breakpoint) occurs.
    _ASSERTE(!m_fBypass || (m_originalPc == pCtx->Pc));

    m_originalPc = pCtx->Pc;
    m_originalITState.Get(pCtx);

    // By default assume the next PC is right after the current instruction.
    m_targetPc = m_originalPc + (Is32BitInstruction(opcode1) ? 4 : 2);
    m_fEmulate = false;

    // One more special case: if we attempt to single-step over an IT instruction it's easier to emulate this,
    // set the new IT state in m_originalITState and set a special flag that lets Fixup() know we don't need
    // to advance the state (this only works because we know IT will never raise an exception so we don't need
    // m_originalITState to store the real original IT state, though in truth a legal IT instruction cannot be
    // executed inside an IT block anyway). This flag (and m_originalITState) will be set inside TryEmulate()
    // as needed.
    m_fEmulatedITInstruction = false;
    m_fSkipIT = false;

    // There are three different scenarios we must deal with (listed in priority order). In all cases we will
    // redirect the thread to execute code from our buffer and end by raising a breakpoint exception:
    //  1) We're executing in an IT block and the current instruction doesn't meet the condition requirements.
    //     We leave the state unchanged and in fixup will advance the PC to the next instruction slot.
    //  2) The current instruction either takes the PC as an input or modifies the PC in a non-trivial manner.
    //     We can't easily run these instructions from the redirect buffer so we emulate their effect (i.e.
    //     update the current context in the same way as executing the instruction would). The breakpoint
    //     fixup logic will restore the PC to the real resultant PC we cache in m_targetPc.
    //  3) For all other cases (including emulation cases where we aborted due to a memory fault) we copy the
    //     single instruction into the redirect buffer for execution followed by a breakpoint (once we regain
    //     control in the breakpoint fixup logic we can then reset the PC to its proper location.

    DWORD idxNextInstruction = 0;

    ExecutableWriterHolder<WORD> codeWriterHolder(m_rgCode, kMaxCodeBuffer * sizeof(m_rgCode[0]));

    if (m_originalITState.InITBlock() && !ConditionHolds(pCtx, m_originalITState.CurrentCondition()))
    {
        LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper: Case 1: ITState::Clear;\n"));
        // Case 1: The current instruction is a no-op because due to the IT instruction. We've already set the
        //         target PC to the next instruction slot. Disable the IT block since we want our breakpoint
        //         to execute. We'll put the correct value back during fixup.
        ITState::Clear(pCtx);
        m_fSkipIT = true;
        codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
    }
    else if (TryEmulate(pCtx, opcode1, opcode2, false))
    {
        LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper: Case 2: Emulate\n"));
        // Case 2: Successfully emulated an instruction that reads or writes the PC. Cache the new target PC
        //         so upon fixup we'll resume execution there rather than the following instruction. No need
        //         to mess with IT state since we know the next instruction is scheduled to execute (we dealt
        //         with the case where it wasn't above) and we're going to execute a breakpoint in that slot.
        m_targetPc = pCtx->Pc;
        m_fEmulate = true;

        // Set breakpoints to stop the execution.  This will get us right back here.
        codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
        codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper: Case 3: CopyInstruction. Is32Bit=%d\n", (DWORD)Is32BitInstruction(opcode1)));
        // Case 3: In all other cases copy the instruction to the buffer and we'll run it directly. If we're
        //         in an IT block there could be up to three instructions following this one whose execution
        //         is skipped. We could try to be clever here and either alter IT state to force the next
        //         instruction to execute or calculate the how many filler instructions we need to insert
        //         before we're guaranteed our breakpoint will be respected. But it's easier to just insert
        //         three additional breakpoints here (code below will add the fourth) and that way we'll
        //         guarantee one of them will be hit (we don't care which one -- the fixup code will update
        //         the PC and IT state to make it look as though the CPU just executed the current
        //         instruction).
        codeWriterHolder.GetRW()[idxNextInstruction++] = opcode1;
        if (Is32BitInstruction(opcode1))
            codeWriterHolder.GetRW()[idxNextInstruction++] = opcode2;

        codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
        codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
        codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
    }

    // Always terminate the redirection buffer with a breakpoint.
    codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
    _ASSERTE(idxNextInstruction <= kMaxCodeBuffer);

    // Set the thread up so it will redirect to our buffer when execution resumes.
    pCtx->Pc = ((DWORD)(DWORD_PTR)m_rgCode) | THUMB_CODE;

    // Make sure the CPU sees the updated contents of the buffer.
    FlushInstructionCache(GetCurrentProcess(), m_rgCode, kMaxCodeBuffer * sizeof(m_rgCode[0]));

    // Done, set the state.
    m_state = Applied;
}

void ArmSingleStepper::Disable()
{
    _ASSERTE(m_state != Applied);
    m_state = Disabled;
}

// When called in response to an exception (preferably in a first chance vectored handler before anyone else
// has looked at the thread context) this method will (a) determine whether this exception was raised by a
// call to Enable() above, in which case true will be returned and (b) perform final fixup of the thread
// context passed in to complete the emulation of a hardware single step. Note that this routine must be
// called even if the exception code is not EXCEPTION_BREAKPOINT since the instruction stepped might have
// raised its own exception (e.g. A/V) and we still need to fix the thread context in this case.
bool ArmSingleStepper::Fixup(T_CONTEXT *pCtx, DWORD dwExceptionCode)
{
#ifdef _DEBUG
    DWORD codestart = (DWORD)(DWORD_PTR)m_rgCode;
    DWORD codeend = codestart + (kMaxCodeBuffer * sizeof(WORD));
#endif

    // If we reach fixup, we should either be Disabled or Applied.  If we reach here with Enabled it means
    // that the debugging layer Enabled the single stepper, but we never applied it to a CONTEXT.
    _ASSERTE(m_state != Enabled);

    // Nothing to do if the stepper is disabled on this thread.
    if (m_state == Disabled)
    {
        // We better not be inside our internal code buffer though.
        _ASSERTE((pCtx->Pc < codestart) || (pCtx->Pc >= codeend));
        return false;
    }

    // Turn off the single stepper after we have executed one instruction.
    m_state = Disabled;

    // We should always have a PC somewhere in our redirect buffer.
#ifdef _DEBUG
    _ASSERTE((pCtx->Pc >= codestart) && (pCtx->Pc < codeend));
#endif

    if (dwExceptionCode == EXCEPTION_BREAKPOINT)
    {
        // The single step went as planned. Set the PC back to its real value (either following the
        // instruction we stepped or the computed destination we cached after emulating an instruction that
        // modifies the PC). Advance the IT state from the value we cached before the single step (unless we
        // stepped an IT instruction itself, in which case m_originalITState holds the new state and we should
        // just set that).
        if (!m_fEmulate)
        {
            if (m_rgCode[0] != kBreakpointOp)
            {
                LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Fixup executed code, ip = %x\n", m_targetPc));

                pCtx->Pc = m_targetPc;
                if (!m_fEmulatedITInstruction)
                    m_originalITState.Advance();

                m_originalITState.Set(pCtx);
            }
            else
            {
                if (m_fSkipIT)
                {
                    // We needed to skip over an instruction due to a false condition in an IT block.
                    LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Fixup skipped instruction due to IT\n"));
                    pCtx->Pc = m_targetPc;

                    _ASSERTE(!m_fEmulatedITInstruction);
                    m_originalITState.Advance();
                    m_originalITState.Set(pCtx);
                }
                else
                {
                    // We've hit a breakpoint in the code stream.  We will return false here (which causes us to NOT
                    // replace the breakpoint code with single step), and place the Pc back to the original Pc.  The
                    // debugger patch skipping code will move past this breakpoint.
                    LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Fixup emulated breakpoint\n"));
                    pCtx->Pc = m_originalPc;

                    _ASSERTE(pCtx->Pc & THUMB_CODE);
                    return false;
                }
            }
        }
        else
        {
            bool res = TryEmulate(pCtx, m_opcodes[0], m_opcodes[1], true);
            _ASSERTE(res);  // We should always successfully emulate since we ran it through TryEmulate already.

            if (!m_fRedirectedPc)
                pCtx->Pc = m_targetPc;

            LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Fixup emulated, ip = %x\n", pCtx->Pc));
        }
    }
    else
    {
        // The stepped instruction caused an exception. Reset the PC and IT state to their original values we
        // cached before stepping. (We should never seen this when stepping an IT instruction which overwrites
        // m_originalITState).
        _ASSERTE(!m_fEmulatedITInstruction);
        _ASSERTE(m_fEmulate == false);
        pCtx->Pc = m_originalPc;
        m_originalITState.Set(pCtx);

        LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::Fixup hit exception pc = %x ex = %x\n", pCtx->Pc, dwExceptionCode));
    }

    _ASSERTE(pCtx->Pc & THUMB_CODE);
    return true;
}

// Count the number of bits set in a DWORD.
DWORD ArmSingleStepper::BitCount(DWORD dwValue)
{
    // There are faster implementations but speed isn't critical here.
    DWORD cBits = 0;
    while (dwValue)
    {
        cBits += dwValue & 1;
        dwValue >>= 1;
    }
    return cBits;
}

// Return true if the given condition (C, N, Z or V) holds in the current context.
#define GET_FLAG(pCtx, _flag)                         \
    ((pCtx->Cpsr & (1 << APSR_##_flag)) != 0)

// Returns true if the current context indicates the ARM condition specified holds.
bool ArmSingleStepper::ConditionHolds(T_CONTEXT *pCtx, DWORD cond)
{
    switch (cond)
    {
    case 0:                 // EQ (Z==1)
        return GET_FLAG(pCtx, Z);
    case 1:                 // NE (Z==0)
        return !GET_FLAG(pCtx, Z);
    case 2:                 // CS (C==1)
        return GET_FLAG(pCtx, C);
    case 3:                 // CC (C==0)
        return !GET_FLAG(pCtx, C);
    case 4:                 // MI (N==1)
        return GET_FLAG(pCtx, N);
    case 5:                 // PL (N==0)
        return !GET_FLAG(pCtx, N);
    case 6:                 // VS (V==1)
        return GET_FLAG(pCtx, V);
    case 7:                 // VC (V==0)
        return !GET_FLAG(pCtx, V);
    case 8:                 // HI (C==1 && Z==0)
        return GET_FLAG(pCtx, C) && !GET_FLAG(pCtx, Z);
    case 9:                 // LS (C==0 || Z==1)
        return !GET_FLAG(pCtx, C) || GET_FLAG(pCtx, Z);
    case 10:                // GE (N==V)
        return GET_FLAG(pCtx, N) == GET_FLAG(pCtx, V);
    case 11:                // LT (N!=V)
        return GET_FLAG(pCtx, N) != GET_FLAG(pCtx, V);
    case 12:                // GT (Z==0 && N==V)
        return !GET_FLAG(pCtx, Z) && (GET_FLAG(pCtx, N) == GET_FLAG(pCtx, V));
    case 13:                // LE (Z==1 || N!=V)
        return GET_FLAG(pCtx, Z) || (GET_FLAG(pCtx, N) != GET_FLAG(pCtx, V));
    case 14:                // AL
        return true;
    case 15:
        _ASSERTE(!"Unsupported condition code: 15");
        return false;
    default:
//        UNREACHABLE();
        return false;
    }
}

// Get the current value of a register. PC (register 15) is always reported as the current instruction PC + 4
// as per the ARM architecture.
DWORD ArmSingleStepper::GetReg(T_CONTEXT *pCtx, DWORD reg)
{
    _ASSERTE(reg <= 15);

    if (reg == 15)
        return (m_originalPc + 4) & ~THUMB_CODE;

    return (&pCtx->R0)[reg];
}

// Set the current value of a register. If the PC (register 15) is set then m_fRedirectedPc is set to true.
void ArmSingleStepper::SetReg(T_CONTEXT *pCtx, DWORD reg, DWORD value)
{
    _ASSERTE(reg <= 15);

    if (reg == 15)
    {
        value |= THUMB_CODE;
        m_fRedirectedPc = true;
    }

    (&pCtx->R0)[reg] = value;
}

// Attempt to read a 1, 2 or 4 byte value from memory, zero or sign extend it to a 4-byte value and place that
// value into the buffer pointed at by pdwResult. Returns false if attempting to read the location caused a
// fault.
bool ArmSingleStepper::GetMem(DWORD *pdwResult, DWORD_PTR pAddress, DWORD cbSize, bool fSignExtend)
{
    struct Param
    {
        DWORD *pdwResult;
        DWORD_PTR pAddress;
        DWORD cbSize;
        bool fSignExtend;
        bool bReturnValue;
    } param;

    param.pdwResult = pdwResult;
    param.pAddress = pAddress;
    param.cbSize = cbSize;
    param.fSignExtend = fSignExtend;
    param.bReturnValue = true;

    PAL_TRY(Param *, pParam, &param)
    {
        switch (pParam->cbSize)
        {
        case 1:
            *pParam->pdwResult = *(BYTE*)pParam->pAddress;
            if (pParam->fSignExtend && (*pParam->pdwResult & 0x00000080))
                *pParam->pdwResult |= 0xffffff00;
            break;
        case 2:
            *pParam->pdwResult = *(WORD*)pParam->pAddress;
            if (pParam->fSignExtend && (*pParam->pdwResult & 0x00008000))
                *pParam->pdwResult |= 0xffff0000;
            break;
        case 4:
            *pParam->pdwResult = *(DWORD*)pParam->pAddress;
            break;
        default:
            UNREACHABLE();
            pParam->bReturnValue = false;
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        param.bReturnValue = false;
    }
    PAL_ENDTRY;

    return param.bReturnValue;
}

// Wrapper around GetMem above that will automatically return from TryEmulate() indicating the instruction
// could not be emulated if we try to read memory and fail due to an exception. This logic works (i.e. we can
// simply return without worrying whether we've already updated the thread context) due to the fact that we
// either (a) read memory before updating any registers (the various LDR literal variants) or (b) update the
// register list before the base register in LDM-like operations (and this should therefore be an idempotent
// operation when we re-execute the instruction). If this ever changes we will have to store a copy of the
// original context we can use to revert changes (it gets even more complex if we ever have to emulate an
// instruction that writes memory).
#define GET_MEM(_result, _addr, _size, _signextend)                     \
    do {                                                                \
        if (!GetMem((_result), (_addr), (_size), (_signextend)))        \
            return false;                                               \
    } while (false)

// Implements the various LDM-style multi-register load instructions (these include POP).
#define LDM(ctx, _base, _registerlist, _writeback, _ia)     \
    do {                                                    \
        DWORD _pAddr = GetReg(ctx, _base);                  \
        if (!(_ia))                                         \
            _pAddr -= BitCount(_registerlist) * sizeof(void*); \
        DWORD _pStartAddr = _pAddr;                         \
        for (DWORD _i = 0; _i < 16; _i++)                   \
        {                                                   \
            if ((_registerlist) & (1 << _i))                \
            {                                               \
                DWORD _tmpresult;                           \
                GET_MEM(&_tmpresult, _pAddr, 4, false);     \
                SetReg(ctx, _i, _tmpresult);                \
                _pAddr += sizeof(void*);                    \
            }                                               \
        }                                                   \
        if (_writeback)                                     \
            SetReg(ctx, _base, (_ia) ? _pAddr : _pStartAddr); \
    } while (false)

// Parse the instruction whose first word is given in opcode1 (if the instruction is 32-bit TryEmulate will
// fetch the second word using the value of the PC stored in the current context). If the instruction reads or
// writes the PC or is the IT instruction then it will be emulated by updating the thread context
// appropriately and true will be returned. If the instruction is not one of those cases (or it is but we
// faulted trying to read memory during the emulation) no state is updated and false is returned instead.
bool ArmSingleStepper::TryEmulate(T_CONTEXT *pCtx, WORD opcode1, WORD opcode2, bool execute)
{
    LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::TryEmulate(opcode=%x %x, execute=%s)\n", (DWORD)opcode1, (DWORD)opcode2, execute ? "true" : "false"));

    // Track whether instruction emulation wrote a modified PC.
    m_fRedirectedPc = false;

    // Track whether we successfully emulated an instruction. If we did and we didn't modify the PC (e.g. a
    // ADR instruction or a conditional branch not taken) then we'll need to explicitly set the PC to the next
    // instruction (since our caller expects that whenever we return true m_pCtx->Pc holds the next
    // instruction address).
    bool fEmulated = false;

    if (Is32BitInstruction(opcode1))
    {
        if (((opcode1 & 0xfbff) == 0xf2af) &&
            ((opcode2 & 0x8000) == 0x0000))
        {
            // ADR.W : T2
            if (execute)
            {
                DWORD Rd = BitExtract(opcode2, 11, 8);
                DWORD i = BitExtract(opcode1, 10, 10);
                DWORD imm3 = BitExtract(opcode2, 14, 12);
                DWORD imm8 = BitExtract(opcode2, 7, 0);

                SetReg(pCtx, Rd, (GetReg(pCtx, 15) & ~3) - ((i << 11) | (imm3 << 8) | imm8));
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xfbff) == 0xf20f) &&
                 ((opcode2 & 0x8000) == 0x0000))
        {
            // ADR.W : T3
            if (execute)
            {
                DWORD Rd = BitExtract(opcode2, 11, 8);
                DWORD i = BitExtract(opcode1, 10, 10);
                DWORD imm3 = BitExtract(opcode2, 14, 12);
                DWORD imm8 = BitExtract(opcode2, 7, 0);

                SetReg(pCtx, Rd, (GetReg(pCtx, 15) & ~3) + ((i << 11) | (imm3 << 8) | imm8));
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xf800) == 0xf000) &&
                 ((opcode2 & 0xd000) == 0x8000) &&
                 ((opcode1 & 0x0380) != 0x0380))
        {
            // B.W : T3
            if (execute)
            {
                DWORD S = BitExtract(opcode1, 10, 10);
                DWORD cond = BitExtract(opcode1, 9, 6);
                DWORD imm6 = BitExtract(opcode1, 5, 0);
                DWORD J1 = BitExtract(opcode2, 13, 13);
                DWORD J2 = BitExtract(opcode2, 11, 11);
                DWORD imm11 = BitExtract(opcode2, 10, 0);

                if (ConditionHolds(pCtx, cond) && execute)
                {
                    DWORD disp = (S ? 0xfff00000 : 0) | (J2 << 19) | (J1 << 18) | (imm6 << 12) | (imm11 << 1);
                    SetReg(pCtx, 15, GetReg(pCtx, 15) + disp);
                }
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xf800) == 0xf000) &&
                 ((opcode2 & 0xd000) == 0x9000))
        {
            // B.W : T4
            if (execute)
            {
                DWORD S = BitExtract(opcode1, 10, 10);
                DWORD imm10 = BitExtract(opcode1, 9, 0);
                DWORD J1 = BitExtract(opcode2, 13, 13);
                DWORD J2 = BitExtract(opcode2, 11, 11);
                DWORD imm11 = BitExtract(opcode2, 10, 0);

                DWORD I1 = (J1 ^ S) ^ 1;
                DWORD I2 = (J2 ^ S) ^ 1;

                DWORD disp = (S ? 0xff000000 : 0) | (I1 << 23) | (I2 << 22) | (imm10 << 12) | (imm11 << 1);
                SetReg(pCtx, 15, GetReg(pCtx, 15) + disp);
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xf800) == 0xf000) &&
                 ((opcode2 & 0xd000) == 0xd000))
        {
            // BL (immediate) : T1
            if (execute)
            {
                DWORD S = BitExtract(opcode1, 10, 10);
                DWORD imm10 = BitExtract(opcode1, 9, 0);
                DWORD J1 = BitExtract(opcode2, 13, 13);
                DWORD J2 = BitExtract(opcode2, 11, 11);
                DWORD imm11 = BitExtract(opcode2, 10, 0);

                DWORD I1 = (J1 ^ S) ^ 1;
                DWORD I2 = (J2 ^ S) ^ 1;

                SetReg(pCtx, 14, GetReg(pCtx, 15) | 1);

                DWORD disp = (S ? 0xff000000 : 0) | (I1 << 23) | (I2 << 22) | (imm10 << 12) | (imm11 << 1);
                SetReg(pCtx, 15, GetReg(pCtx, 15) + disp);
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xffd0) == 0xe890) &&
                 ((opcode2 & 0x2000) == 0x0000))
        {
            // LDM.W : T2, POP.W : T2
            if (execute)
            {
                DWORD W = BitExtract(opcode1, 5, 5);
                DWORD Rn = BitExtract(opcode1, 3, 0);
                DWORD registerList = opcode2;

                LDM(pCtx, Rn, registerList, W, true);
                fEmulated = true;
            }
            else
            {
                // We should only emulate this instruction if Pc is set
                if (opcode2 & (1<<15))
                    fEmulated = true;
            }
        }
        else if (((opcode1 & 0xffd0) == 0xe410) &&
                 ((opcode2 & 0x2000) == 0x0000))
        {
            // LDMDB : T1
            if (execute)
            {
                DWORD W = BitExtract(opcode1, 5, 5);
                DWORD Rn = BitExtract(opcode1, 3, 0);
                DWORD registerList = opcode2;

                LDM(pCtx, Rn, registerList, W, false);
                fEmulated = true;
            }
            else
            {
                // We should only emulate this instruction if Pc is set
                if (opcode2 & (1<<15))
                    fEmulated = true;
            }
        }
        else if (((opcode1 & 0xfff0) == 0xf8d0) &&
                 ((opcode1 & 0x000f) != 0x000f))
        {
            // LDR.W (immediate): T3
            DWORD Rt = BitExtract(opcode2, 15, 12);
            DWORD Rn = BitExtract(opcode1, 3, 0);
            if (execute)
            {
                DWORD imm12 = BitExtract(opcode2, 11, 0);

                DWORD value;
                GET_MEM(&value, GetReg(pCtx, Rn) + imm12, 4, false);

                SetReg(pCtx, Rt, value);
                fEmulated = true;
            }
            else
            {
                // We should only emulate this instruction if Pc is used
                if (Rt == 15 || Rn == 15)
                    fEmulated = true;
            }
        }
        else if (((opcode1 & 0xfff0) == 0xf850) &&
                 ((opcode2 & 0x0800) == 0x0800) &&
                 ((opcode1 & 0x000f) != 0x000f))
        {
            // LDR (immediate) : T4, POP : T3
            DWORD Rn = BitExtract(opcode1, 3, 0);
            DWORD Rt = BitExtract(opcode2, 15, 12);
            if (execute)
            {
                DWORD P = BitExtract(opcode2, 10, 10);
                DWORD U = BitExtract(opcode2, 9, 9);
                DWORD W = BitExtract(opcode2, 8, 8);
                DWORD imm8 = BitExtract(opcode2, 7, 0);

                DWORD offset_addr = U ? GetReg(pCtx, Rn) + imm8 : GetReg(pCtx, Rn) - imm8;
                DWORD addr = P ? offset_addr : GetReg(pCtx, Rn);

                DWORD value;
                GET_MEM(&value, addr, 4, false);

                if (W)
                    SetReg(pCtx, Rn, offset_addr);

                SetReg(pCtx, Rt, value);
                fEmulated = true;
            }
            else
            {
                // We should only emulate this instruction if Pc is used
                if (Rt == 15 || Rn == 15)
                    fEmulated = true;
            }
        }
        else if (((opcode1 & 0xff7f) == 0xf85f))
        {
            // LDR.W (literal) : T2
            DWORD Rt = BitExtract(opcode2, 15, 12);
            if (execute)
            {
                DWORD U = BitExtract(opcode1, 7, 7);
                DWORD imm12 = BitExtract(opcode2, 11, 0);

                // This instruction always reads relative to R15/PC
                DWORD addr = GetReg(pCtx, 15) & ~3;
                addr = U ? addr + imm12 : addr - imm12;

                DWORD value;
                GET_MEM(&value, addr, 4, false);

                SetReg(pCtx, Rt, value);
            }

            // We should ALWAYS emulate this instruction, because this instruction
            // always reads the memory relative to PC
            fEmulated = true;
        }
        else if (((opcode1 & 0xfff0) == 0xf850) &&
                 ((opcode2 & 0x0fc0) == 0x0000) &&
                 ((opcode1 & 0x000f) != 0x000f))
        {
            // LDR.W : T2
            DWORD Rn = BitExtract(opcode1, 3, 0);
            DWORD Rt = BitExtract(opcode2, 15, 12);
            DWORD Rm = BitExtract(opcode2, 3, 0);
            if (execute)
            {
                DWORD imm2 = BitExtract(opcode2, 5, 4);
                DWORD addr = GetReg(pCtx, Rn) + (GetReg(pCtx, Rm) << imm2);

                DWORD value;
                GET_MEM(&value, addr, 4, false);

                SetReg(pCtx, Rt, value);
                fEmulated = true;
            }
            else
            {
                // We should only emulate this instruction if Pc is used
                if (Rt == 15 || Rn == 15 || Rm == 15)
                    fEmulated = true;
            }
        }
        else if (((opcode1 & 0xff7f) == 0xf81f) &&
                 ((opcode2 & 0xf000) != 0xf000))
        {
            // LDRB (literal) : T2
            if (execute)
            {
                DWORD U = BitExtract(opcode1, 7, 7);
                DWORD Rt = BitExtract(opcode2, 15, 12);
                DWORD imm12 = BitExtract(opcode2, 11, 0);

                DWORD addr = (GetReg(pCtx, 15) & ~3);
                addr = U ? addr + imm12 : addr - imm12;

                DWORD value;
                GET_MEM(&value, addr, 1, false);

                SetReg(pCtx, Rt, value);
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xfe5f) == 0xe85f) &&
                 ((opcode1 & 0x0120) != 0x0000))
        {
            // LDRD (literal) : T1
            if (execute)
            {
                DWORD U = BitExtract(opcode1, 7, 7);
                DWORD Rt = BitExtract(opcode2, 15, 12);
                DWORD Rt2 = BitExtract(opcode2, 11, 8);
                DWORD imm8 = BitExtract(opcode2, 7, 0);

                DWORD addr = (GetReg(pCtx, 15) & ~3);
                addr = U ? addr + (imm8 << 2) : addr - (imm8 << 2);

                DWORD value1;
                GET_MEM(&value1, addr, 4, false);

                DWORD value2;
                GET_MEM(&value2, addr + 4, 4, false);

                SetReg(pCtx, Rt, value1);
                SetReg(pCtx, Rt2, value2);
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xff7f) == 0xf83f) &&
                 ((opcode2 & 0xf000) != 0xf000))
        {
            // LDRH (literal) : T1
            if (execute)
            {
                DWORD U = BitExtract(opcode1, 7, 7);
                DWORD Rt = BitExtract(opcode2, 15, 12);
                DWORD imm12 = BitExtract(opcode2, 11, 0);

                DWORD addr = (GetReg(pCtx, 15) & ~3);
                addr = U ? addr + imm12 : addr - imm12;

                DWORD value;
                GET_MEM(&value, addr, 2, false);

                SetReg(pCtx, Rt, value);
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xff7f) == 0xf91f) &&
                 ((opcode2 & 0xf000) != 0xf000))
        {
            // LDRSB (literal) : T1
            if (execute)
            {
                DWORD U = BitExtract(opcode1, 7, 7);
                DWORD Rt = BitExtract(opcode2, 15, 12);
                DWORD imm12 = BitExtract(opcode2, 11, 0);

                DWORD addr = (GetReg(pCtx, 15) & ~3);
                addr = U ? addr + imm12 : addr - imm12;

                DWORD value;
                GET_MEM(&value, addr, 1, true);

                SetReg(pCtx, Rt, value);
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xff7f) == 0xf53f) &&
                 ((opcode2 & 0xf000) != 0xf000))
        {
            // LDRSH (literal) : T1
            if (execute)
            {
                DWORD U = BitExtract(opcode1, 7, 7);
                DWORD Rt = BitExtract(opcode2, 15, 12);
                DWORD imm12 = BitExtract(opcode2, 11, 0);

                DWORD addr = (GetReg(pCtx, 15) & ~3);
                addr = U ? addr + imm12 : addr - imm12;

                DWORD value;
                GET_MEM(&value, addr, 2, true);

                SetReg(pCtx, Rt, value);
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xfff0) == 0xe8d0) &&
                 ((opcode2 & 0xffe0) == 0xf000))
        {
            // TBB/TBH : T1
            if (execute)
            {
                DWORD Rn = BitExtract(opcode1, 3, 0);
                DWORD H = BitExtract(opcode2, 4, 4);
                DWORD Rm = BitExtract(opcode2, 3, 0);

                DWORD addr = GetReg(pCtx, Rn);

                DWORD value;
                if (H)
                    GET_MEM(&value, addr + (GetReg(pCtx, Rm) << 1), 2, false);
                else
                    GET_MEM(&value, addr + GetReg(pCtx, Rm), 1, false);

                SetReg(pCtx, 15, GetReg(pCtx, 15) + (value << 1));
            }

            fEmulated = true;
        }

        // If we emulated an instruction but didn't set the PC explicitly we have to do so now (in such cases
        // the next PC will always point directly after the instruction we just emulated).
        if (fEmulated && !m_fRedirectedPc)
            SetReg(pCtx, 15, GetReg(pCtx, 15));
    }
    else
    {
        // Handle 16-bit instructions.

        if ((opcode1 & 0xf800) == 0xa000)
        {
            // ADR : T1
            if (execute)
            {
                DWORD Rd = BitExtract(opcode1, 10, 8);
                DWORD imm8 = BitExtract(opcode1, 7, 0);

                SetReg(pCtx, Rd, (GetReg(pCtx, 15) & 3) + (imm8 << 2));
            }

            fEmulated = true;
        }
        else if ((opcode1 & 0xff00) == 0x4400)
        {
            // A8.8.6 ADD (register, Thumb) : T2
            DWORD Rm = BitExtract(opcode1, 6, 3);

            // We should only emulate this instruction if Pc is used
            if (Rm == 15)
                fEmulated = true;

            if (execute)
            {
                DWORD Rd = BitExtract(opcode1, 2, 0) | BitExtract(opcode1, 7, 7) << 3;
                SetReg(pCtx, Rd, GetReg(pCtx, Rm) + GetReg(pCtx, Rd));
            }
        }
        else if (((opcode1 & 0xf000) == 0xd000) && ((opcode1 & 0x0f00) != 0x0e00))
        {
            // B : T1

            // We only emulate this instruction if we take the conditional
            // jump.  If not we'll pass right over the jump and set the
            // target IP as normal.
            DWORD cond = BitExtract(opcode1, 11, 8);
            if (execute)
            {
                _ASSERTE(ConditionHolds(pCtx, cond));

                DWORD imm8 = BitExtract(opcode1, 7, 0);
                DWORD disp = (imm8 << 1) | ((imm8 & 0x80) ? 0xffffff00 : 0);

                SetReg(pCtx, 15, GetReg(pCtx, 15) + disp);
                fEmulated = true;
            }
            else
            {
                if (ConditionHolds(pCtx, cond))
                {
                    fEmulated = true;
                }
            }
        }
        else if ((opcode1 & 0xf800) == 0xe000)
        {
            if (execute)
            {
                // B : T2
                DWORD imm11 = BitExtract(opcode1, 10, 0);
                DWORD disp = (imm11 << 1) | ((imm11 & 0x400) ? 0xfffff000 : 0);

                SetReg(pCtx, 15, GetReg(pCtx, 15) + disp);
            }

            fEmulated = true;
        }
        else if ((opcode1 & 0xff87) == 0x4780)
        {
            // BLX (register) : T1
            if (execute)
            {
                DWORD Rm = BitExtract(opcode1, 6, 3);
                DWORD addr = GetReg(pCtx, Rm);

                SetReg(pCtx, 14, (GetReg(pCtx, 15) - 2) | 1);
                SetReg(pCtx, 15, addr);
            }

            fEmulated = true;
        }
        else if ((opcode1 & 0xff87) == 0x4700)
        {
            // BX : T1
            if (execute)
            {
                DWORD Rm = BitExtract(opcode1, 6, 3);
                SetReg(pCtx, 15, GetReg(pCtx, Rm));
            }

            fEmulated = true;
        }
        else if ((opcode1 & 0xf500) == 0xb100)
        {
            // CBNZ/CBZ : T1
            if (execute)
            {
                DWORD op = BitExtract(opcode1, 11, 11);
                DWORD i = BitExtract(opcode1, 9, 9);
                DWORD imm5 = BitExtract(opcode1, 7, 3);
                DWORD Rn = BitExtract(opcode1, 2, 0);

                if ((op && (GetReg(pCtx, Rn) != 0)) ||
                    (!op && (GetReg(pCtx, Rn) == 0)))
                {
                    SetReg(pCtx, 15, GetReg(pCtx, 15) + ((i << 6) | (imm5 << 1)));
                }
            }

            fEmulated = true;
        }
        else if (((opcode1 & 0xff00) == 0xbf00) &&
                 ((opcode1 & 0x000f) != 0x0000))
        {
            // IT : T1
            if (execute)
            {
                DWORD firstcond = BitExtract(opcode1, 7, 4);
                DWORD mask = BitExtract(opcode1, 3, 0);

                // The IT instruction is special. We compute the IT state bits for the CPSR and cache them in
                // m_originalITState. We then set m_fEmulatedITInstruction so that Fixup() knows not to advance
                // this state (simply write it as-is back into the CPSR).
                m_originalITState.Init((BYTE)((firstcond << 4) | mask));
                m_originalITState.Set(pCtx);
                m_fEmulatedITInstruction = true;
            }

            fEmulated = true;
        }
        else if ((opcode1 & 0xf800) == 0x4800)
        {
            // LDR (literal) : T1
            if (execute)
            {
                DWORD Rt = BitExtract(opcode1, 10, 8);
                DWORD imm8 = BitExtract(opcode1, 7, 0);

                DWORD addr = (GetReg(pCtx, 15) & ~3) + (imm8 << 2);

                DWORD value = 0;
                GET_MEM(&value, addr, 4, false);

                SetReg(pCtx, Rt, value);
            }

            fEmulated = true;
        }
        else if ((opcode1 & 0xff00) == 0x4600)
        {
            // MOV (register) : T1
            DWORD D = BitExtract(opcode1, 7, 7);
            DWORD Rm = BitExtract(opcode1, 6, 3);
            DWORD Rd = (D << 3) | BitExtract(opcode1, 2, 0);

            if (execute)
            {
                SetReg(pCtx, Rd, GetReg(pCtx, Rm));
                fEmulated = true;
            }
            else
            {
                // Only emulate if we change Pc
                if (Rm == 15 || Rd == 15)
                    fEmulated = true;
            }
        }
        else if ((opcode1 & 0xfe00) == 0xbc00)
        {
            // POP : T1
            DWORD P = BitExtract(opcode1, 8, 8);
            DWORD registerList = (P << 15) | BitExtract(opcode1, 7, 0);
            if (execute)
            {
                LDM(pCtx, 13, registerList, true, true);
                fEmulated = true;
            }
            else
            {
                // Only emulate if Pc is in the register list
                if (registerList & (1<<15))
                    fEmulated = true;
            }
        }

        // If we emulated an instruction but didn't set the PC explicitly we have to do so now (in such cases
        // the next PC will always point directly after the instruction we just emulated).
        if (execute && fEmulated && !m_fRedirectedPc)
            SetReg(pCtx, 15, GetReg(pCtx, 15) - 2);
    }

    LOG((LF_CORDB, LL_INFO100000, "ArmSingleStepper::TryEmulate(opcode=%x %x) emulated=%s redirectedPc=%s\n",
        (DWORD)opcode1, (DWORD)opcode2, fEmulated ? "true" : "false", m_fRedirectedPc ? "true" : "false"));
    return fEmulated;
}
