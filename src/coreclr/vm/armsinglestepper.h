// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Emulate hardware single-step on ARM.
//

#ifndef __ARM_SINGLE_STEPPER_INCLUDED
#define __ARM_SINGLE_STEPPER_INCLUDED

// Class abstracting state kept for the IT instruction within the CPSR.
class ITState
{
public:
    ITState();

    // Must call Get() (or Init()) to initialize this instance from a specific context before calling any
    // other (non-static) method.
    void Get(T_CONTEXT *pCtx);

    // Must call Init() (or Get()) to initialize this instance from a raw byte value before calling any other
    // (non-static) method.
    void Init(BYTE bState);

    // Does the current IT state indicate we're executing within an IT block?
    bool InITBlock();

    // Only valid within an IT block. Returns the condition code which will be evaluated for the current
    // instruction.
    DWORD CurrentCondition();

    // Transition the IT state to that for the next instruction.
    void Advance();

    // Write the current IT state back into the given context.
    void Set(T_CONTEXT *pCtx);

    // Clear IT state (i.e. force execution to be outside of an IT block) in the given context.
    static void Clear(T_CONTEXT *pCtx);

private:
    BYTE    m_bITState;
#ifdef _DEBUG
    bool    m_fValid;
#endif
};

// Class that encapsulates the context needed to single step one thread.
class ArmSingleStepper
{
public:
    ArmSingleStepper();
    ~ArmSingleStepper();

    // Given the context with which a thread will be resumed, modify that context such that resuming the
    // thread will execute a single instruction before raising an EXCEPTION_BREAKPOINT. The thread context
    // must be cleaned up via the Fixup method below before any further exception processing can occur (at
    // which point the caller can behave as though EXCEPTION_SINGLE_STEP was raised).
    void Enable();

    void Bypass(DWORD ip, WORD opcode1, WORD opcode2);

    void Apply(T_CONTEXT *pCtx);

    // Disables the single stepper.
    void Disable();

    // Returns whether or not the stepper is enabled.
    inline bool IsEnabled() const
    {
        return m_state == Enabled || m_state == Applied;
    }

    // When called in response to an exception (preferably in a first chance vectored handler before anyone
    // else has looked at the thread context) this method will (a) determine whether this exception was raised
    // by a call to Enable() above, in which case true will be returned and (b) perform final fixup of the
    // thread context passed in to complete the emulation of a hardware single step. Note that this routine
    // must be called even if the exception code is not EXCEPTION_BREAKPOINT since the instruction stepped
    // might have raised its own exception (e.g. A/V) and we still need to fix the thread context in this
    // case.
    bool Fixup(T_CONTEXT *pCtx, DWORD dwExceptionCode);

private:
    enum
    {
        kMaxCodeBuffer = 2 + 3 + 1, // WORD slots in our redirect buffer (2 for current instruction, 3 for
                                    // breakpoint instructions used to pad out slots in an IT block and one
                                    // for the final breakpoint)
#ifdef __linux__
        kBreakpointOp = 0xde01,     // Opcode for the breakpoint instruction used on ARM Linux
#else
        kBreakpointOp = 0xdefe,     // Opcode for the breakpoint instruction used on CoreARM
#endif
    };

    // Bit numbers of the condition flags in the CPSR.
    enum APSRBits
    {
        APSR_N = 31,
        APSR_Z = 30,
        APSR_C = 29,
        APSR_V = 28,
    };

    enum StepperState
    {
        Disabled,
        Enabled,
        Applied
    };

    DWORD         m_originalPc;               // PC value before stepping
    ITState       m_originalITState;          // IT state before stepping
    DWORD         m_targetPc;                 // Final PC value after stepping if no exception is raised
    WORD         *m_rgCode;                   // Buffer execution is redirected to during the step
    StepperState  m_state;                    // Tracks whether the stepper is Enabled, Disabled, or enabled and applied to a context
    WORD          m_opcodes[2];               // Set if we are emulating a non-IT instruction
    bool          m_fEmulatedITInstruction;   // Set to true if Enable() emulated an IT instruction
    bool          m_fRedirectedPc;            // Used during TryEmulate() to track where PC was written
    bool          m_fEmulate;
    bool          m_fBypass;
    bool          m_fSkipIT;                  // We are skipping an instruction due to an IT condition.

    // Initializes m_rgCode.  Not thread safe.
    void Init();

    // Count the number of bits set in a DWORD.
    static DWORD BitCount(DWORD dwValue);

    // Returns true if the current context indicates the ARM condition specified holds.
    bool ConditionHolds(T_CONTEXT *pCtx, DWORD cond);

    // Get the current value of a register. PC (register 15) is always reported as the current instruction PC
    // + 4 as per the ARM architecture.
    DWORD GetReg(T_CONTEXT *pCtx, DWORD reg);

    // Set the current value of a register. If the PC (register 15) is set then m_fRedirectedPc is set to
    // true.
    void SetReg(T_CONTEXT *pCtx, DWORD reg, DWORD value);

    // Attempt to read a 1, 2 or 4 byte value from memory, zero or sign extend it to a 4-byte value and place
    // that value into the buffer pointed at by pdwResult. Returns false if attempting to read the location
    // caused a fault.
    bool GetMem(DWORD *pdwResult, DWORD_PTR pAddress, DWORD cbSize, bool fSignExtend);

    // Parse the instruction whose first word is given in opcode1 (if the instruction is 32-bit TryEmulate
    // will fetch the second word using the value of the PC stored in the current context). If the instruction
    // reads or writes the PC or is the IT instruction then it will be emulated by updating the thread context
    // appropriately and true will be returned. If the instruction is not one of those cases (or it is but we
    // faulted trying to read memory during the emulation) no state is updated and false is returned instead.
    bool TryEmulate(T_CONTEXT *pCtx, WORD opcode1, WORD opcode2, bool execute);
};

#endif // !__ARM_SINGLE_STEPPER_INCLUDED
