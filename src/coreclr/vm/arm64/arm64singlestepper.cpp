// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Emulate hardware single-step on ARM.
//

#include "common.h"
#include "arm64singlestepper.h"

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
// Arm64SingleStepper methods.
//
Arm64SingleStepper::Arm64SingleStepper()
    : m_originalPc(0), m_targetPc(0), m_rgCode(0), m_state(Disabled),
      m_fEmulate(false), m_fBypass(false)
{
     m_opcodes[0] = 0;
}

Arm64SingleStepper::~Arm64SingleStepper()
{
#if !defined(DACCESS_COMPILE)
    SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->BackoutMem(m_rgCode, kMaxCodeBuffer * sizeof(uint32_t));
#endif
}

void Arm64SingleStepper::Init()
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
void Arm64SingleStepper::Enable()
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

    LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::Enable\n"));

    m_fBypass = false;
    m_opcodes[0] = 0;
    m_state = Enabled;
}

void Arm64SingleStepper::Bypass(uint64_t ip, uint32_t opcode)
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


    LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::Bypass(pc=%lx, opcode=%x)\n", ip, opcode));

    m_fBypass = true;
    m_originalPc = ip;
    m_opcodes[0] = opcode;
    m_state = Enabled;
}

void Arm64SingleStepper::Apply(T_CONTEXT *pCtx)
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

    LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::Apply(pc=%lx, opcode=%x)\n", (uint64_t)pCtx->Pc, opcode));

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
    // still peform context fixup in this case but we don't attempt to alter any exception code other than
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
        LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper: Case 1: Emulate\n"));
        // Case 1: Emulate an instruction that reads or writes the PC.
        m_fEmulate = true;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper: Case 2: CopyInstruction.\n"));
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

void Arm64SingleStepper::Disable()
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
bool Arm64SingleStepper::Fixup(T_CONTEXT *pCtx, DWORD dwExceptionCode)
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
                LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::Fixup executed code, ip = %lx\n", m_targetPc));

                pCtx->Pc = m_targetPc;
            }
            else
            {
                // We've hit a breakpoint in the code stream.  We will return false here (which causes us to NOT
                // replace the breakpoint code with single step), and place the Pc back to the original Pc.  The
                // debugger patch skipping code will move past this breakpoint.
                LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::Fixup emulated breakpoint\n"));
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

            LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::Fixup emulated, ip = %lx\n", pCtx->Pc));
        }
    }
    else
    {
        // The stepped instruction caused an exception. Reset the PC to its original values we
        // cached before stepping.
        _ASSERTE(m_fEmulate == false);
        pCtx->Pc = m_originalPc;

        LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::Fixup hit exception pc = %lx ex = %x\n", pCtx->Pc, dwExceptionCode));
    }

    _ASSERTE((pCtx->Pc & 0x3) == 0);
    return true;
}

// Return true if the given condition (C, N, Z or V) holds in the current context.
#define GET_FLAG(pCtx, _flag)                         \
    ((pCtx->Cpsr & (1 << APSR_##_flag)) != 0)

// Returns true if the current context indicates the ARM condition specified holds.
bool Arm64SingleStepper::ConditionHolds(T_CONTEXT *pCtx, uint64_t cond)
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
        return false;
    default:
        UNREACHABLE();
        return false;
    }
}

// Get the current value of a register.
uint64_t Arm64SingleStepper::GetReg(T_CONTEXT *pCtx, uint64_t reg)
{
    _ASSERTE(reg <= 31);

    return (&pCtx->X0)[reg];
}

// Set the current value of a register.
void Arm64SingleStepper::SetReg(T_CONTEXT *pCtx, uint64_t reg, uint64_t value)
{
    _ASSERTE(reg <= 31);

    (&pCtx->X0)[reg] = value;
}

// Set the current value of a register.
void Arm64SingleStepper::SetFPReg(T_CONTEXT *pCtx, uint64_t reg, uint64_t valueLo, uint64_t valueHi)
{
    _ASSERTE(reg <= 31);

    pCtx->V[reg].Low = valueLo;
    pCtx->V[reg].High = valueHi;
}

// Attempt to read a 4, or 8 byte value from memory, zero or sign extend it to a 8-byte value and place
// that value into the buffer pointed at by pdwResult. Returns false if attempting to read the location
// caused a fault.
bool Arm64SingleStepper::GetMem(uint64_t *pdwResult, uint8_t* pAddress, int cbSize, bool fSignExtend)
{
    struct Param
    {
        uint64_t *pdwResult;
        uint8_t *pAddress;
        int cbSize;
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
        case 4:
            *pParam->pdwResult = *(uint32_t*)pParam->pAddress;
            if (pParam->fSignExtend && (*pParam->pdwResult & 0x80000000))
                *pParam->pdwResult |= 0xffffffff00000000;
            break;
        case 8:
            *pParam->pdwResult = *(uint64_t*)pParam->pAddress;
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

// Parse the instruction opcode. If the instruction reads or writes the PC it will be emulated by updating
// the thread context appropriately and true will be returned. If the instruction is not one of those cases
// (or it is but we faulted trying to read memory during the emulation) no state is updated and false is
// returned instead.
bool Arm64SingleStepper::TryEmulate(T_CONTEXT *pCtx, uint32_t opcode, bool execute)
{
    LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate(opcode=%x, execute=%s)\n", opcode, execute ? "true" : "false"));

    // Track whether instruction emulation wrote a modified PC.
    bool fRedirectedPc = false;

    // Track whether we successfully emulated an instruction. If we did and we didn't modify the PC (e.g. a
    // ADR instruction or a conditional branch not taken) then we'll need to explicitly set the PC to the next
    // instruction (since our caller expects that whenever we return true m_pCtx->Pc holds the next
    // instruction address).
    bool fEmulated = false;

    if ((opcode & 0x1f000000) == 0x10000000) // PC-Rel addressing (ADR & ADRP)
    {
        fEmulated = true;
        if (execute)
        {
            uint64_t P =     BitExtract(opcode, 31, 31);
            uint64_t immlo = BitExtract(opcode, 30, 29);
            uint64_t immhi = BitExtract(opcode, 23, 5, true);
            uint64_t Rd =    BitExtract(opcode, 4, 0);

            if (P) // ADRP
            {
                LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate ADRP\n"));
                uint64_t imm = (immhi << 14) | (immlo << 12);
                uint64_t value = (m_originalPc & ~0xfffull) + imm;
                SetReg(pCtx, Rd, value);
            }
            else // ADR
            {
                LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate ADR\n"));
                uint64_t imm = (immhi << 2) | (immlo);
                uint64_t value = m_originalPc + imm;
                SetReg(pCtx, Rd, value);
            }
        }
    }
    else if ((opcode & 0xff000010) == 0x54000000) // Conditional branch immediate (B.cond)
    {
        fEmulated = true;
        if (execute)
        {
            LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate B.cond\n"));
            uint64_t imm19 = BitExtract(opcode, 23, 5, true);
            uint64_t cond =  BitExtract(opcode, 3, 0);

            if (ConditionHolds(pCtx, cond))
            {
                uint64_t imm = (imm19 << 2);

                fRedirectedPc = true;
                m_targetPc = m_originalPc + imm;
            }
        }
    }
    else if ((opcode & 0xf7000000) == 0xd6000000) // Unconditional branch register
    {
        if (((opcode & 0xfffffc1f) == 0xd61f0000)  // BR
          ||((opcode & 0xfffffc1f) == 0xd63f0000)  // BLR
          ||((opcode & 0xfffffc1f) == 0xd65f0000)) // RET
        {
            fEmulated = true;
            if (execute)
            {
                LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate Unconditional branch register\n"));
                uint64_t Rn = BitExtract(opcode, 9, 5);
                uint64_t target = GetReg(pCtx, Rn);

                // arm64 supports tagged addresses (bit 55 is treated as a sign bit and extended when tagged addresses are enabled).
                // assumes we don't need to emulate tagged addresses
                _ASSERTE(target == BitExtract(target, 55, 0, true));

                fRedirectedPc = true;
                m_targetPc = target;

                if ((opcode & 0xfffffc1f) == 0xd63f0000)  // BLR
                    SetReg(pCtx, 30, m_originalPc + 4);
            }
        }
        else
        {
            LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate unexpected\n"));
            // These are either:
            // - Unallocated instructions
            // - Unallocated on armv8 in EL0 (ERET, DRPS)
            // - Several armv8.3 pointer authentication branch related instructions
            //   Note: We do not use armv8.3 pointer authentication forms in JIT or generate arm64 native code with v8.3 enabled
        }
    }
    else if ((opcode & 0x7c000000) == 0x14000000) // Unconditional branch immediate (B & BL)
    {
        fEmulated = true;
        if (execute)
        {
            uint64_t L =     BitExtract(opcode, 31, 31);
            uint64_t imm26 = BitExtract(opcode, 25, 0, true);

            uint64_t imm = (imm26 << 2);

            fRedirectedPc = true;
            m_targetPc = m_originalPc + imm;

            if (L) // BL
            {
                LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate BL\n"));
                SetReg(pCtx, 30, m_originalPc + 4);
            }
            else
            {
                LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate B\n"));
            }
        }
    }
    else if ((opcode & 0x7e000000) == 0x34000000) // Compare and branch CBZ & CBNZ
    {
        fEmulated = true;
        if (execute)
        {
            LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate CBZ/CBNZ\n"));

            uint64_t sf =    BitExtract(opcode, 31, 31);
            uint64_t NZ =    BitExtract(opcode, 24, 24);
            uint64_t imm19 = BitExtract(opcode, 23, 5, true);
            uint64_t Rt =    BitExtract(opcode, 4, 0);

            uint64_t regValue = GetReg(pCtx, Rt);

            if (sf == 0)
            {
                // 32-bit instruction form
                regValue = BitExtract(regValue, 31, 0);
            }

            if ((regValue == 0) == (NZ == 0))
            {
                uint64_t imm = (imm19 << 2);

                fRedirectedPc = true;
                m_targetPc = m_originalPc + imm;
            }
        }
    }
    else if ((opcode & 0x7e000000) == 0x36000000) // Test and branch (TBZ & TBNZ)
    {
        fEmulated = true;
        if (execute)
        {
            LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate TBZ/TBNZ\n"));

            uint64_t b5 =    BitExtract(opcode, 31, 31);
            uint64_t NZ =    BitExtract(opcode, 24, 24);
            uint64_t b40 =   BitExtract(opcode, 23, 19);
            uint64_t imm14 = BitExtract(opcode, 18, 5, true);
            uint64_t Rt =    BitExtract(opcode, 4, 0);

            uint64_t regValue = GetReg(pCtx, Rt);

            uint64_t bit = (b5 << 5) | b40;
            uint64_t bitValue = BitExtract(regValue, bit, bit);

            if (bitValue == NZ)
            {
                uint64_t imm = (imm14 << 2);

                fRedirectedPc = true;
                m_targetPc = m_originalPc + imm;
            }
        }
    }
    else if ((opcode & 0x3b000000) == 0x18000000) // Load register (literal)
    {
        uint64_t opc = BitExtract(opcode, 31, 30);

        fEmulated = (opc != 3);
        if (execute)
        {
            LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate Load register (literal)\n"));

            uint64_t V =     BitExtract(opcode, 26, 26);
            uint64_t imm19 = BitExtract(opcode, 23, 5, true);
            uint64_t Rt =    BitExtract(opcode, 4, 0);

            uint64_t imm = (imm19 << 2);

            uint8_t* address = (uint8_t*)(m_originalPc + imm);

            uint64_t value = 0;
            uint64_t valueHi;

            switch (opc)
            {
            case 0: // 32-bit
                GET_MEM(&value, address, 4, false);

                if (V == 0)
                    SetReg(pCtx, Rt, value);
                else
                    SetFPReg(pCtx, Rt, value);
                break;
            case 1: // 64-bit GPR
                GET_MEM(&value, address, 8, false);

                if (V == 0)
                    SetReg(pCtx, Rt, value);
                else
                    SetFPReg(pCtx, Rt, value);
                break;
            case 2:
                if (V == 0)
                {
                    // 32-bit GPR Sign extended
                    GET_MEM(&value, address, 4, true);
                    SetReg(pCtx, Rt, value);
                }
                else
                {
                    // 128-bit FP & SIMD
                    GET_MEM(&value, address, 8, false);
                    GET_MEM(&valueHi, address + 8, 8, false);
                    SetFPReg(pCtx, Rt, value, valueHi);
                }
                break;
            default:
                _ASSERTE(FALSE);
            }
        }
    }

    LOG((LF_CORDB, LL_INFO100000, "Arm64SingleStepper::TryEmulate(opcode=%x) emulated=%s redirectedPc=%s\n",
        opcode, fEmulated ? "true" : "false", fRedirectedPc ? "true" : "false"));

    return fEmulated;
}
