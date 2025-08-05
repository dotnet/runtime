// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Emulate hardware single-step on LOONGARCH64.
//

#include "common.h"
#include "loongarch64singlestepper.h"

inline uint64_t SignExtend(uint64_t value, unsigned int signbit)
{
    _ASSERTE(signbit < 64);

    if (signbit == 63)
      return value;

    uint64_t sign = value & (1ull << signbit);

    if (sign)
        return value | (~0ull << signbit);
    else
        return value;
}

inline uint64_t BitExtract(uint64_t value, unsigned int highbit, unsigned int lowbit, bool signExtend = false)
{
    _ASSERTE((highbit < 64) && (lowbit < 64) && (highbit >= lowbit));
    uint64_t extractedValue = (value >> lowbit) & ((1ull << ((highbit - lowbit) + 1)) - 1);

    return signExtend ? SignExtend(extractedValue, highbit - lowbit) : extractedValue;
}

//
// LoongArch64SingleStepper methods.
//
LoongArch64SingleStepper::LoongArch64SingleStepper()
    : m_originalPc(0), m_targetPc(0), m_rgCode(0), m_state(Disabled),
      m_fEmulate(false), m_fBypass(false)
{
    m_opcodes[0] = 0;
}

LoongArch64SingleStepper::~LoongArch64SingleStepper()
{
#if !defined(DACCESS_COMPILE)
    SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->BackoutMem(m_rgCode, kMaxCodeBuffer * sizeof(uint32_t));
#endif
}

void LoongArch64SingleStepper::Init()
{
#if !defined(DACCESS_COMPILE)
    if (m_rgCode == NULL)
    {
        m_rgCode = (uint32_t *)(void *)SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->AllocMem(S_SIZE_T(kMaxCodeBuffer * sizeof(uint32_t)));
    }
#endif
}

// Given the context with which a thread will be resumed, modify that context such that resuming the thread
// will execute a single instruction before raising an EXCEPTION_BREAKPOINT. The thread context must be
// cleaned up via the Fixup method below before any further exception processing can occur (at which point the
// caller can behave as though EXCEPTION_SINGLE_STEP was raised).
void LoongArch64SingleStepper::Enable()
{
    _ASSERTE(m_state != Applied);

    if (m_state == Enabled)
    {
        // We allow single-stepping to be enabled multiple times before the thread is resumed, but we require
        // that the thread state is the same in all cases (i.e. additional step requests are treated as
        // no-ops).
        _ASSERTE(!m_fBypass);
        _ASSERTE(m_opcodes[0] == 0);

        return;
    }

    LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::Enable\n"));

    m_fBypass = false;
    m_opcodes[0] = 0;
    m_state = Enabled;
}

void LoongArch64SingleStepper::Bypass(uint64_t ip, uint32_t opcode)
{
    _ASSERTE(m_state != Applied);

    if (m_state == Enabled)
    {
        // We allow single-stepping to be enabled multiple times before the thread is resumed, but we require
        // that the thread state is the same in all cases (i.e. additional step requests are treated as
        // no-ops).
        if (m_fBypass)
        {
            _ASSERTE(m_opcodes[0] == opcode);
            _ASSERTE(m_originalPc == ip);
            return;
        }
    }

    LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::Bypass(pc=%lx, opcode=%x)\n", ip, opcode));

    m_fBypass = true;
    m_originalPc = ip;
    m_opcodes[0] = opcode;
    m_state = Enabled;
}

void LoongArch64SingleStepper::Apply(T_CONTEXT *pCtx)
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
        uint64_t pc = pCtx->Pc;
        m_opcodes[0] = *(uint32_t*)pc; // Opcodes are always in little endian, we only support little endian execution mode
    }

    uint32_t opcode = m_opcodes[0];

    LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::Apply(pc=%lx, opcode=%x)\n", (uint64_t)pCtx->Pc, opcode));

#ifdef _DEBUG
    // Make sure that we aren't trying to step through our own buffer.  If this asserts, something is horribly
    // wrong with the debugging layer.  Likely GetManagedStoppedCtx is retrieving a Pc that points to our
    // buffer, even though the single stepper is disabled.
    uint64_t codestart = (uint64_t)m_rgCode;
    uint64_t codeend = codestart + (kMaxCodeBuffer * sizeof(uint32_t));
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

    // Cache thread's initial PC since we'll overwrite them as part of the emulation and we need
    // to get back to the correct values at fixup time. We also cache a target PC (set below) since some
    // instructions will set the PC directly or otherwise make it difficult for us to compute the final PC
    // from the original. We still need the original PC however since this is the one we'll use if an
    // exception (other than a breakpoint) occurs.
    _ASSERTE((!m_fBypass || (m_originalPc == pCtx->Pc)));

    m_originalPc = pCtx->Pc;

    // By default assume the next PC is right after the current instruction.
    m_targetPc = m_originalPc + sizeof(uint32_t);
    m_fEmulate = false;

    // There are two different scenarios we must deal with (listed in priority order). In all cases we will
    // redirect the thread to execute code from our buffer and end by raising a breakpoint exception:
    //  1) The current instruction either takes the PC as an input or modifies the PC.
    //     We can't easily run these instructions from the redirect buffer so we emulate their effect (i.e.
    //     update the current context in the same way as executing the instruction would). The breakpoint
    //     fixup logic will restore the PC to the real resultant PC we cache in m_targetPc.
    //  2) For all other cases (including emulation cases where we aborted due to a memory fault) we copy the
    //     single instruction into the redirect buffer for execution followed by a breakpoint (once we regain
    //     control in the breakpoint fixup logic we can then reset the PC to its proper location.

    unsigned int idxNextInstruction = 0;

    ExecutableWriterHolder<DWORD> codeWriterHolder(m_rgCode, kMaxCodeBuffer * sizeof(m_rgCode[0]));

    if (TryEmulate(pCtx, opcode, false))
    {
        LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper: Case 1: Emulate\n"));
        // Case 1: Emulate an instruction that reads or writes the PC.
        m_fEmulate = true;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper: Case 2: CopyInstruction.\n"));
        // Case 2: In all other cases copy the instruction to the buffer and we'll run it directly.
        codeWriterHolder.GetRW()[idxNextInstruction++] = opcode;
    }

    // Always terminate the redirection buffer with a breakpoint.
    codeWriterHolder.GetRW()[idxNextInstruction++] = kBreakpointOp;
    _ASSERTE(idxNextInstruction <= kMaxCodeBuffer);

    // Set the thread up so it will redirect to our buffer when execution resumes.
    pCtx->Pc = (uint64_t)m_rgCode;

    // Make sure the CPU sees the updated contents of the buffer.
    FlushInstructionCache(GetCurrentProcess(), m_rgCode, kMaxCodeBuffer * sizeof(m_rgCode[0]));

    // Done, set the state.
    m_state = Applied;
}

void LoongArch64SingleStepper::Disable()
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
bool LoongArch64SingleStepper::Fixup(T_CONTEXT *pCtx, DWORD dwExceptionCode)
{
#ifdef _DEBUG
    uint64_t codestart = (uint64_t)m_rgCode;
    uint64_t codeend = codestart + (kMaxCodeBuffer * sizeof(uint32_t));
#endif

    // If we reach fixup, we should either be Disabled or Applied.  If we reach here with Enabled it means
    // that the debugging layer Enabled the single stepper, but we never applied it to a CONTEXT.
    _ASSERTE(m_state != Enabled);

    // Nothing to do if the stepper is disabled on this thread.
    if (m_state == Disabled)
    {
        // We better not be inside our internal code buffer though.
        _ASSERTE((pCtx->Pc < codestart) || (pCtx->Pc > codeend));
        return false;
    }

    // Turn off the single stepper after we have executed one instruction.
    m_state = Disabled;

    // We should always have a PC somewhere in our redirect buffer.
#ifdef _DEBUG
    _ASSERTE((pCtx->Pc >= codestart) && (pCtx->Pc <= codeend));
#endif

    if (dwExceptionCode == EXCEPTION_BREAKPOINT)
    {
        // The single step went as planned. Set the PC back to its real value (either following the
        // instruction we stepped or the computed destination we cached after emulating an instruction that
        // modifies the PC).
        if (!m_fEmulate)
        {
            if (m_rgCode[0] != kBreakpointOp)
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::Fixup executed code, ip = %lx\n", m_targetPc));

                pCtx->Pc = m_targetPc;
            }
            else
            {
                // We've hit a breakpoint in the code stream.  We will return false here (which causes us to NOT
                // replace the breakpoint code with single step), and place the Pc back to the original Pc.  The
                // debugger patch skipping code will move past this breakpoint.
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::Fixup emulated breakpoint\n"));
                pCtx->Pc = m_originalPc;

                _ASSERTE((pCtx->Pc & 0x3) == 0);
                return false;
            }
        }
        else
        {
            bool res = TryEmulate(pCtx, m_opcodes[0], true);
            _ASSERTE(res);  // We should always successfully emulate since we ran it through TryEmulate already.

            pCtx->Pc = m_targetPc;

            LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::Fixup emulated, ip = %lx\n", pCtx->Pc));
        }
    }
    else
    {
        // The stepped instruction caused an exception. Reset the PC to its original values we
        // cached before stepping.
        _ASSERTE(m_fEmulate == false);
        pCtx->Pc = m_originalPc;

        LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::Fixup hit exception pc = %lx ex = %x\n", pCtx->Pc, dwExceptionCode));
    }

    _ASSERTE((pCtx->Pc & 0x3) == 0);

    return true;
}

// Get the current value of a register.
uint64_t LoongArch64SingleStepper::GetReg(T_CONTEXT *pCtx, uint64_t reg)
{
    _ASSERTE(reg <= 31);
    _ASSERTE(pCtx->R0 == 0);

    return (&pCtx->R0)[reg];
}

// Set the current value of a register.
void LoongArch64SingleStepper::SetReg(T_CONTEXT *pCtx, uint64_t reg, uint64_t value)
{
    _ASSERTE(reg <= 31);

    if (reg != 0) // Do nothing for R0, register R0 is hardwired to the constant 0.
        (&pCtx->R0)[reg] = value;
}

// Parse the instruction opcode. If the instruction reads or writes the PC it will be emulated by updating
// the thread context appropriately and true will be returned. If the instruction is not one of those cases
// (or it is but we faulted trying to read memory during the emulation) no state is updated and false is
// returned instead.
bool LoongArch64SingleStepper::TryEmulate(T_CONTEXT *pCtx, uint32_t opcode, bool execute)
{
    LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate(opcode=%x, execute=%s)\n", opcode, execute ? "true" : "false"));

    // Track whether instruction emulation wrote a modified PC.
    bool fRedirectedPc = false;

    // Track whether we successfully emulated an instruction. If we did and we didn't modify the PC (e.g. a
    // PCALAU12I instruction or a conditional branch not taken) then we'll need to explicitly set the PC to the next
    // instruction (since our caller expects that whenever we return true m_pCtx->Pc holds the next
    // instruction address).
    bool fEmulated = false;

    uint32_t op = (opcode >> 26) & 0x3f;

    if ((op == 0x6) || (op == 0x7)) // PC-Rel addressing (PCADDU12I, PCALAU12I, PCADDU18I)
    {
        fEmulated = true;
        if (execute)
        {
            uint64_t P   = BitExtract(opcode, 25, 25);
            uint64_t imm = BitExtract(opcode, 24, 5, true);
            uint64_t Rd  = BitExtract(opcode, 4, 0);

            if ((op == 0x6) && P) // PCALAU12I
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate PCALAU12I\n"));
                uint64_t value = (m_originalPc & ~0xfffull) + (imm << 12);
                SetReg(pCtx, Rd, value);
            }
            else if (P)// PCADDU18I
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate PCADDU18I\n"));
                uint64_t value = m_originalPc + (imm << 18);
                SetReg(pCtx, Rd, value);
            }
            else // PCADDU12I
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate PCADDU12I\n"));
                uint64_t value = m_originalPc + (imm << 12);
                SetReg(pCtx, Rd, value);
            }
        }
    }
    else if ((op == 0x16) || (op == 0x17) || (op == 0x18) || (op == 0x19) || (op == 0x1a) ||(op == 0x1b)) // B.cond: BEQ, BNE, BLT, BGE, BLTU, BGEU
    {
        fEmulated = true;
        if (execute)
        {
            LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate B.cond: BEQ, BNE, BLT[U], BGE[U]\n"));
            uint64_t imm16 = BitExtract(opcode, 25, 10, true);
            uint64_t rd    = BitExtract(opcode, 4, 0);
            uint64_t rj    = BitExtract(opcode, 9, 5);

            int64_t  rdSValue = GetReg(pCtx, rd);
            int64_t  rjSValue = GetReg(pCtx, rj);
            uint64_t rdUValue = GetReg(pCtx, rd);
            uint64_t rjUValue = GetReg(pCtx, rj);

            if (((op == 0x16) && (rjSValue == rdSValue)) ||
                ((op == 0x17) && (rjSValue != rdSValue)) ||
                ((op == 0x18) && (rjSValue <  rdSValue)) ||
                ((op == 0x19) && (rjSValue >= rdSValue)) ||
                ((op == 0x1a) && (rjUValue <  rdUValue)) ||
                ((op == 0x1b) && (rjUValue >= rdUValue)))
            {
                uint64_t imm  = (imm16 << 2);
                fRedirectedPc = true;
                m_targetPc    = m_originalPc + imm;
            }
        }
    }
    else if ((op == 0x10) || (op == 0x11)) // Compare and branch BEQZ, BNEZ
    {
        fEmulated = true;
        if (execute)
        {
            LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate BEQZ, BNEZ\n"));

            uint64_t immlo = BitExtract(opcode, 25, 10);
            uint64_t immhi = BitExtract(opcode, 4, 0, true);
            uint64_t imm   = ((immhi << 16) | immlo) << 2;

            uint64_t rj      = BitExtract(opcode, 9, 5);
            uint64_t rjValue = GetReg(pCtx, rj);

            if (((op == 0x10) && (rjValue == 0)) ||
                ((op == 0x11) && (rjValue != 0)))
            {
                fRedirectedPc = true;
                m_targetPc    = m_originalPc + imm;
            }
        }
    }
    else if ((op == 0x14) || (op == 0x15)) // Unconditional branch immediate (B, BL)
    {
        fEmulated = true;
        if (execute)
        {
            uint64_t immlo = BitExtract(opcode, 25, 10);
            uint64_t immhi = BitExtract(opcode, 9, 0, true);
            uint64_t imm   = ((immhi << 16) | immlo) << 2;

            fRedirectedPc = true;
            m_targetPc = m_originalPc + imm;

            if (op == 0x14) //B
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate B\n"));
            }
            else // BL
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate BL\n"));
                SetReg(pCtx, 1, m_originalPc + 4);
            }
        }
    }
    else if (op == 0x13) // Unconditional branch register JIRL
    {
        fEmulated = true;
        if (execute)
        {
            int imm = (short)((opcode >> 10) & 0xffff);
            imm <<= 2;

            uint64_t rd    = BitExtract(opcode, 4, 0);
            uint64_t rj    = BitExtract(opcode, 9, 5);

            if (rd == 0) // JIRL 0, RJ
            {
                fRedirectedPc = true;
                m_targetPc    = GetReg(pCtx, rj) + (int)imm;
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate JIRL 0 RA\n"));
            }
            else if (rd == 1) // JIRL RA, RJ
            {
                SetReg(pCtx, 1, m_originalPc + 4);
                fRedirectedPc = true;
                m_targetPc    = GetReg(pCtx, rj) + (int)imm;
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate JIRL RA target\n"));
            }
            else
            {
                LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate unexpected for JIRL !\n"));
            }
        }
    }

    LOG((LF_CORDB, LL_INFO100000, "LoongArch64SingleStepper::TryEmulate(opcode=%x) emulated=%s redirectedPc=%s\n",
        opcode, fEmulated ? "true" : "false", fRedirectedPc ? "true" : "false"));

    return fEmulated;
}
