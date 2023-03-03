// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "PalRedhawkCommon.h"
#include "CommonMacros.h"
#include "config.h"
#include "daccess.h"
#include "regdisplay.h"
#include "UnixContext.h"
#include "HardwareExceptions.h"
#include "UnixSignals.h"

#if defined(HOST_OSX)
#include <mach/mach.h>
#include <mach/mach_error.h>
#include <mach/exception.h>
#include <mach/task.h>
#endif

#if !HAVE_SIGINFO_T
#error Cannot handle hardware exceptions on this platform
#endif

#define REDHAWK_PALEXPORT extern "C"
#define REDHAWK_PALAPI

#define EXCEPTION_ACCESS_VIOLATION          0xC0000005u
#define EXCEPTION_DATATYPE_MISALIGNMENT     0x80000002u
#define EXCEPTION_BREAKPOINT                0x80000003u
#define EXCEPTION_SINGLE_STEP               0x80000004u
#define EXCEPTION_ARRAY_BOUNDS_EXCEEDED     0xC000008Cu
#define EXCEPTION_FLT_DENORMAL_OPERAND      0xC000008Du
#define EXCEPTION_FLT_DIVIDE_BY_ZERO        0xC000008Eu
#define EXCEPTION_FLT_INEXACT_RESULT        0xC000008Fu
#define EXCEPTION_FLT_INVALID_OPERATION     0xC0000090u
#define EXCEPTION_FLT_OVERFLOW              0xC0000091u
#define EXCEPTION_FLT_STACK_CHECK           0xC0000092u
#define EXCEPTION_FLT_UNDERFLOW             0xC0000093u
#define EXCEPTION_INT_DIVIDE_BY_ZERO        0xC0000094u
#define EXCEPTION_INT_OVERFLOW              0xC0000095u
#define EXCEPTION_PRIV_INSTRUCTION          0xC0000096u
#define EXCEPTION_IN_PAGE_ERROR             0xC0000006u
#define EXCEPTION_ILLEGAL_INSTRUCTION       0xC000001Du
#define EXCEPTION_NONCONTINUABLE_EXCEPTION  0xC0000025u
#define EXCEPTION_STACK_OVERFLOW            0xC00000FDu
#define EXCEPTION_INVALID_DISPOSITION       0xC0000026u
#define EXCEPTION_GUARD_PAGE                0x80000001u
#define EXCEPTION_INVALID_HANDLE            0xC0000008u

#define EXCEPTION_CONTINUE_EXECUTION (-1)
#define EXCEPTION_CONTINUE_SEARCH (0)
#define EXCEPTION_EXECUTE_HANDLER (1)

struct sigaction g_previousSIGSEGV;
struct sigaction g_previousSIGFPE;

// Exception handler for hardware exceptions
static PHARDWARE_EXCEPTION_HANDLER g_hardwareExceptionHandler = NULL;

#ifdef HOST_AMD64

// Get value of an instruction operand represented by the ModR/M field
// Parameters:
// 	uint8_t rex :           REX prefix, 0 if there was none
// 	uint8_t* ip :           instruction pointer pointing to the ModR/M field
// 	void* context :     context containing the registers
// 	bool is8Bit :           true if the operand size is 8 bit
// 	bool hasOpSizePrefix :  true if the instruction has op size prefix (0x66)
uint64_t GetModRMOperandValue(uint8_t rex, uint8_t* ip, void* context, bool is8Bit, bool hasOpSizePrefix)
{
    uint64_t result;
    uint64_t resultReg;

    uint8_t rex_b = (rex & 0x1);       // high bit to modrm r/m field or SIB base field
    uint8_t rex_x = (rex & 0x2) >> 1;  // high bit to sib index field
    uint8_t rex_r = (rex & 0x4) >> 2;  // high bit to modrm reg field
    uint8_t rex_w = (rex & 0x8) >> 3;  // 1 = 64 bit operand size, 0 = operand size determined by hasOpSizePrefix

    uint8_t modrm = *ip++;

    ASSERT(modrm != 0);

    uint8_t mod = (modrm & 0xC0) >> 6;
    uint8_t reg = (modrm & 0x38) >> 3;
    uint8_t rm = (modrm & 0x07);

    reg |= (rex_r << 3);
    uint8_t rmIndex = rm | (rex_b << 3);

    // 8 bit idiv without the REX prefix uses registers AH, CH, DH, BH for rm 4..8
    // which is an exception from the regular register indexes.
    bool isAhChDhBh = is8Bit && (rex == 0) && (rm >= 4);

    // See: Tables A-15,16,17 in AMD Dev Manual 3 for information
    //      about how the ModRM/SIB/REX uint8_ts interact.

    switch (mod)
    {
    case 0:
    case 1:
    case 2:
        if (rm == 4) // we have an SIB uint8_t following
        {
            //
            // Get values from the SIB uint8_t
            //
            uint8_t sib = *ip++;

            ASSERT(sib != 0);

            uint8_t ss = (sib & 0xC0) >> 6;
            uint8_t index = (sib & 0x38) >> 3;
            uint8_t base = (sib & 0x07);

            index |= (rex_x << 3);
            base |= (rex_b << 3);

            //
            // Get starting value
            //
            if ((mod == 0) && (base == 5))
            {
                result = 0;
            }
            else
            {
                result = GetRegisterValueByIndex(context, base);
            }

            //
            // Add in the [index]
            //
            if (index != 4)
            {
                result += GetRegisterValueByIndex(context, index) << ss;
            }

            //
            // Finally add in the offset
            //
            if (mod == 0)
            {
                if (base == 5)
                {
                    result += *((int32_t*)ip);
                }
            }
            else if (mod == 1)
            {
                result += *((int8_t*)ip);
            }
            else // mod == 2
            {
                result += *((int32_t*)ip);
            }

        }
        else
        {
            //
            // Get the value we need from the register.
            //

            // Check for RIP-relative addressing mode.
            if ((mod == 0) && (rm == 5))
            {
                result = (uint64_t)ip + sizeof(int32_t) + *(int32_t*)ip;
            }
            else
            {
                result = GetRegisterValueByIndex(context, rmIndex);

                if (mod == 1)
                {
                    result += *((int8_t*)ip);
                }
                else if (mod == 2)
                {
                    result += *((int32_t*)ip);
                }
            }
        }

        break;

    case 3:
    default:
        // The operand is stored in a register.
        if (isAhChDhBh)
        {
            // 8 bit idiv without the REX prefix uses registers AH, CH, DH or BH for rm 4..8.
            // So we shift the register index to get the real register index.
            rmIndex -= 4;
        }

        resultReg = GetRegisterValueByIndex(context, rmIndex);
        result = (uint64_t)&resultReg;

        if (isAhChDhBh)
        {
            // Move one uint8_t higher to get an address of the AH, CH, DH or BH
            result++;
        }

        break;

    }

    // Now dereference thru the result to get the resulting value.
    if (is8Bit)
    {
        result = *((uint8_t*)result);
    }
    else if (rex_w != 0)
    {
        result = *((uint64_t*)result);
    }
    else if (hasOpSizePrefix)
    {
        result = *((uint16_t*)result);
    }
    else
    {
        result = *((uint32_t*)result);
    }

    return result;
}

// Skip all prefixes until the instruction code or the REX prefix is found
// Parameters:
// 	uint8_t** ip :          Pointer to the current instruction pointer. Updated
//                          as the function walks the codes.
//  bool* hasOpSizePrefix : Pointer to bool, on exit set to true if a op size prefix
//                          was found.
// Return value :
//  Code of the REX prefix or the instruction code after the prefixes.
uint8_t SkipPrefixes(uint8_t **ip, bool* hasOpSizePrefix)
{
    *hasOpSizePrefix = false;

    while (true)
    {
        uint8_t code = *(*ip)++;

        switch (code)
        {
        case 0x66: // Operand-Size
            *hasOpSizePrefix = true;
            break;

            // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS
        case 0x64: // FS
        case 0x65: // GS

            // Size overrides
        case 0x67: // Address-Size

            // Lock
        case 0xf0:

            // String REP prefixes
        case 0xf2: // REPNE/REPNZ
        case 0xf3:
            break;

        default:
            // Return address of the nonprefix code
            return code;
        }
    }
}

// Check if a division by zero exception is in fact a division overflow. The
// x64 processor generate the same exception in both cases for the IDIV / DIV
// instruction. So we need to decode the instruction argument and check
// whether it was zero or not.
bool IsDivByZeroAnIntegerOverflow(void* context)
{
    uint8_t * ip = (uint8_t*)GetPC(context);
    uint8_t rex = 0;
    bool hasOpSizePrefix = false;

    uint8_t code = SkipPrefixes(&ip, &hasOpSizePrefix);

    // The REX prefix must directly precede the instruction code
    if ((code & 0xF0) == 0x40)
    {
        rex = code;
        code = *ip++;
    }

    uint64_t divisor = 0;

    // Check if the instruction is IDIV or DIV. The instruction code includes the three
    // 'reg' bits in the ModRM uint8_t. These are 7 for IDIV and 6 for DIV
    uint8_t regBits = (*ip & 0x38) >> 3;
    if ((code == 0xF7 || code == 0xF6) && (regBits == 7 || regBits == 6))
    {
        bool is8Bit = (code == 0xF6);
        divisor = GetModRMOperandValue(rex, ip, context, is8Bit, hasOpSizePrefix);
    }
    else
    {
        ASSERT_UNCONDITIONALLY("Invalid instruction (expected IDIV or DIV)");
    }

    // If the division operand is zero, it was division by zero. Otherwise the failure
    // must have been an overflow.
    return divisor != 0;
}
#endif //HOST_AMD64

// Translates signal and context information to a Win32 exception code.
uint32_t GetExceptionCodeForSignal(const siginfo_t *siginfo, const void *context)
{
    // IMPORTANT NOTE: This function must not call any signal unsafe functions
    // since it is called from signal handlers.
#ifdef ILL_ILLOPC
    switch (siginfo->si_signo)
    {
        case SIGILL:
            switch (siginfo->si_code)
            {
                case ILL_ILLOPC:    // Illegal opcode
                case ILL_ILLOPN:    // Illegal operand
                case ILL_ILLADR:    // Illegal addressing mode
                case ILL_ILLTRP:    // Illegal trap
                case ILL_COPROC:    // Co-processor error
                    return EXCEPTION_ILLEGAL_INSTRUCTION;
                case ILL_PRVOPC:    // Privileged opcode
                case ILL_PRVREG:    // Privileged register
                    return EXCEPTION_PRIV_INSTRUCTION;
                case ILL_BADSTK:    // Internal stack error
                    return EXCEPTION_STACK_OVERFLOW;
                default:
                    break;
            }
            break;
        case SIGFPE:
            switch (siginfo->si_code)
            {
                case FPE_INTDIV:
                    return EXCEPTION_INT_DIVIDE_BY_ZERO;
                case FPE_INTOVF:
                    return EXCEPTION_INT_OVERFLOW;
                case FPE_FLTDIV:
                    return EXCEPTION_FLT_DIVIDE_BY_ZERO;
                case FPE_FLTOVF:
                    return EXCEPTION_FLT_OVERFLOW;
                case FPE_FLTUND:
                    return EXCEPTION_FLT_UNDERFLOW;
                case FPE_FLTRES:
                    return EXCEPTION_FLT_INEXACT_RESULT;
                case FPE_FLTINV:
                    return EXCEPTION_FLT_INVALID_OPERATION;
                case FPE_FLTSUB:
                    return EXCEPTION_FLT_INVALID_OPERATION;
                default:
                    break;
            }
            break;
        case SIGSEGV:
            switch (siginfo->si_code)
            {
                case SI_USER:       // User-generated signal, sometimes sent
                                    // for SIGSEGV under normal circumstances
                case SEGV_MAPERR:   // Address not mapped to object
                case SEGV_ACCERR:   // Invalid permissions for mapped object
                    return EXCEPTION_ACCESS_VIOLATION;

#ifdef SI_KERNEL
                case SI_KERNEL:
                {
                    return EXCEPTION_ACCESS_VIOLATION;
                }
#endif
                default:
                    break;
            }
            break;
        case SIGBUS:
            switch (siginfo->si_code)
            {
                case BUS_ADRALN:    // Invalid address alignment
                    return EXCEPTION_DATATYPE_MISALIGNMENT;
                case BUS_ADRERR:    // Non-existent physical address
                    return EXCEPTION_ACCESS_VIOLATION;
                case BUS_OBJERR:    // Object-specific hardware error
                default:
                    break;
            }
            break;
        case SIGTRAP:
            switch (siginfo->si_code)
            {
#ifdef SI_KERNEL
                case SI_KERNEL:
#endif
                case SI_USER:
                case TRAP_BRKPT:    // Process breakpoint
                    return EXCEPTION_BREAKPOINT;
                case TRAP_TRACE:    // Process trace trap
                    return EXCEPTION_SINGLE_STEP;
                default:
                    // Got unknown SIGTRAP signal with code siginfo->si_code;
                    return EXCEPTION_ILLEGAL_INSTRUCTION;
            }
        default:
            break;
    }

    // Got unknown signal number siginfo->si_signo with code siginfo->si_code;
    return EXCEPTION_ILLEGAL_INSTRUCTION;
#else   // ILL_ILLOPC
    int trap;

    if (siginfo->si_signo == SIGFPE)
    {
        // Floating point exceptions are mapped by their si_code.
        switch (siginfo->si_code)
        {
            case FPE_INTDIV :
                return EXCEPTION_INT_DIVIDE_BY_ZERO;
            case FPE_INTOVF :
                return EXCEPTION_INT_OVERFLOW;
            case FPE_FLTDIV :
                return EXCEPTION_FLT_DIVIDE_BY_ZERO;
            case FPE_FLTOVF :
                return EXCEPTION_FLT_OVERFLOW;
            case FPE_FLTUND :
                return EXCEPTION_FLT_UNDERFLOW;
            case FPE_FLTRES :
                return EXCEPTION_FLT_INEXACT_RESULT;
            case FPE_FLTINV :
                return EXCEPTION_FLT_INVALID_OPERATION;
            case FPE_FLTSUB :/* subscript out of range */
                return EXCEPTION_FLT_INVALID_OPERATION;
            default:
                // Got unknown signal code siginfo->si_code;
                return 0;
        }
    }

    trap = ((ucontext_t*)context)->uc_mcontext.mc_trapno;
    switch (trap)
    {
        case T_PRIVINFLT : /* privileged instruction */
            return EXCEPTION_PRIV_INSTRUCTION;
        case T_BPTFLT :    /* breakpoint instruction */
            return EXCEPTION_BREAKPOINT;
        case T_ARITHTRAP : /* arithmetic trap */
            return 0;      /* let the caller pick an exception code */
#ifdef T_ASTFLT
        case T_ASTFLT :    /* system forced exception : ^C, ^\. SIGINT signal
                              handler shouldn't be calling this function, since
                              it doesn't need an exception code */
            // Trap code T_ASTFLT received, shouldn't get here;
            return 0;
#endif  // T_ASTFLT
        case T_PROTFLT :   /* protection fault */
            return EXCEPTION_ACCESS_VIOLATION;
        case T_TRCTRAP :   /* debug exception (sic) */
            return EXCEPTION_SINGLE_STEP;
        case T_PAGEFLT :   /* page fault */
            return EXCEPTION_ACCESS_VIOLATION;
        case T_ALIGNFLT :  /* alignment fault */
            return EXCEPTION_DATATYPE_MISALIGNMENT;
        case T_DIVIDE :
            return EXCEPTION_INT_DIVIDE_BY_ZERO;
        case T_NMI :       /* non-maskable trap */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_OFLOW :
            return EXCEPTION_INT_OVERFLOW;
        case T_BOUND :     /* bound instruction fault */
            return EXCEPTION_ARRAY_BOUNDS_EXCEEDED;
        case T_DNA :       /* device not available fault */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_DOUBLEFLT : /* double fault */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_FPOPFLT :   /* fp coprocessor operand fetch fault */
            return EXCEPTION_FLT_INVALID_OPERATION;
        case T_TSSFLT :    /* invalid tss fault */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_SEGNPFLT :  /* segment not present fault */
            return EXCEPTION_ACCESS_VIOLATION;
        case T_STKFLT :    /* stack fault */
            return EXCEPTION_STACK_OVERFLOW;
        case T_MCHK :      /* machine check trap */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_RESERVED :  /* reserved (unknown) */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        default:
            // Got unknown trap code trap;
            break;
    }
    return EXCEPTION_ILLEGAL_INSTRUCTION;
#endif  // ILL_ILLOPC
}

// Common handler for hardware exception signals
bool HardwareExceptionHandler(int code, siginfo_t *siginfo, void *context, void* faultAddress)
{
    if (g_hardwareExceptionHandler != NULL)
    {
        uintptr_t faultCode = GetExceptionCodeForSignal(siginfo, context);

#ifdef HOST_AMD64
        // It is possible that an overflow was mapped to a divide-by-zero exception.
        // This happens when we try to divide the maximum negative value of a
        // signed integer with -1.
        //
        // Thus, we will attempt to decode the instruction @ RIP to determine if that
        // is the case using the faulting context.
        if ((faultCode == EXCEPTION_INT_DIVIDE_BY_ZERO) && IsDivByZeroAnIntegerOverflow(context))
        {
            // The exception was an integer overflow, so augment the fault code.
            faultCode = EXCEPTION_INT_OVERFLOW;
        }
#endif //HOST_AMD64

        PAL_LIMITED_CONTEXT palContext;
        NativeContextToPalContext(context, &palContext);

        uintptr_t arg0Reg;
        uintptr_t arg1Reg;
        int32_t disposition = g_hardwareExceptionHandler(faultCode, (uintptr_t)faultAddress, &palContext, &arg0Reg, &arg1Reg);
        if (disposition == EXCEPTION_CONTINUE_EXECUTION)
        {
            // TODO: better name
            RedirectNativeContext(context, &palContext, arg0Reg, arg1Reg);
            return true;
        }
    }

    return false;
}

// Handler for the SIGSEGV signal
void SIGSEGVHandler(int code, siginfo_t *siginfo, void *context)
{
    bool isHandled = HardwareExceptionHandler(code, siginfo, context, siginfo->si_addr);
    if (isHandled)
    {
        return;
    }

    if (g_previousSIGSEGV.sa_sigaction != NULL)
    {
        g_previousSIGSEGV.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        RestoreSignalHandler(code, &g_previousSIGSEGV);
    }
}

// Handler for the SIGFPE signal
void SIGFPEHandler(int code, siginfo_t *siginfo, void *context)
{
    bool isHandled = HardwareExceptionHandler(code, siginfo, context, NULL);
    if (isHandled)
    {
        return;
    }

    if (g_previousSIGFPE.sa_sigaction != NULL)
    {
        g_previousSIGFPE.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        RestoreSignalHandler(code, &g_previousSIGFPE);
    }
}

// Initialize hardware exception handling
bool InitializeHardwareExceptionHandling()
{
    if (!AddSignalHandler(SIGSEGV, SIGSEGVHandler, &g_previousSIGSEGV))
    {
        return false;
    }

    if (!AddSignalHandler(SIGFPE, SIGFPEHandler, &g_previousSIGFPE))
    {
        return false;
    }

#if defined(HOST_OSX)
	// LLDB installs task-wide Mach exception handlers. XNU dispatches Mach
	// exceptions first to any registered "activation" handler and then to
	// any registered task handler before dispatching the exception to a
	// host-wide Mach exception handler that does translation to POSIX
	// signals. This makes it impossible to use LLDB with implicit null
    // checks in NativeAOT; continuing execution after LLDB traps an
    // EXC_BAD_ACCESS will result in LLDB's EXC_BAD_ACCESS handler being
    // invoked again. This also interferes with the translation of SIGFPEs
    // to .NET-level ArithmeticExceptions. Work around this here by
	// installing a no-op task-wide Mach exception handler for
	// EXC_BAD_ACCESS and EXC_ARITHMETIC.
	kern_return_t kr = task_set_exception_ports(
		mach_task_self(),
		EXC_MASK_BAD_ACCESS | EXC_MASK_ARITHMETIC, /* SIGSEGV, SIGFPE */
		MACH_PORT_NULL,
		EXCEPTION_STATE_IDENTITY,
		MACHINE_THREAD_STATE);
    ASSERT(kr == KERN_SUCCESS);
#endif

    return true;
}

// Set hardware exception handler
REDHAWK_PALEXPORT void REDHAWK_PALAPI PalSetHardwareExceptionHandler(PHARDWARE_EXCEPTION_HANDLER handler)
{
    ASSERT_MSG(g_hardwareExceptionHandler == NULL, "Hardware exception handler already set")
    g_hardwareExceptionHandler = handler;
}
