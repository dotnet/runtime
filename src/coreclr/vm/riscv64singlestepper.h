// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Emulate hardware single-step on RISCV64.
//

#ifndef __RISCV64_SINGLE_STEPPER_H__
#define __RISCV64_SINGLE_STEPPER_H__

// Class that encapsulates the context needed to single step one thread.
class RiscV64SingleStepper
{
public:
    RiscV64SingleStepper();
    ~RiscV64SingleStepper();

    // Given the context with which a thread will be resumed, modify that context such that resuming the
    // thread will execute a single instruction before raising an EXCEPTION_BREAKPOINT. The thread context
    // must be cleaned up via the Fixup method below before any further exception processing can occur (at
    // which point the caller can behave as though EXCEPTION_SINGLE_STEP was raised).
    void Enable();

    void Bypass(uint64_t ip, uint32_t opcode);

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
        kMaxCodeBuffer = 2, // max slots in our redirect buffer
                            // 1 for current instruction
                            // 1 for final breakpoint

        kBreakpointOp = 0x00100073, // Opcode for the breakpoint instruction used on RiscV64 Unix
    };

    enum StepperState
    {
        Disabled,
        Enabled,
        Applied
    };

    uint64_t      m_originalPc;               // PC value before stepping
    uint64_t      m_targetPc;                 // Final PC value after stepping if no exception is raised
    uint32_t      *m_rgCode;                  // Buffer execution is redirected to during the step
    StepperState  m_state;                    // Tracks whether the stepper is Enabled, Disabled, or enabled and applied to a context
    uint32_t      m_opcodes[1];
    bool          m_fEmulate;
    bool          m_fBypass;

    // Initializes m_rgCode.  Not thread safe.
    void Init();

    // Get the current value of a register.
    uint64_t GetReg(T_CONTEXT *pCtx, uint64_t reg);
    // Set the current value of a register.
    void SetReg(T_CONTEXT *pCtx, uint64_t reg, uint64_t value);

    // Parse the instruction opcode. If the instruction reads or writes the PC it will be emulated by updating
    // the thread context appropriately and true will be returned. If the instruction is not one of those cases
    // (or it is but we faulted trying to read memory during the emulation) no state is updated and false is
    // returned instead.
    bool TryEmulate(T_CONTEXT *pCtx, uint32_t opcode, bool execute);
};

#endif // !__RISCV64_SINGLE_STEPPER_H__
