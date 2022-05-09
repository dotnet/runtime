// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Keep in sync with llvm/tools/objwriter/cordebuginfo.h in current objwriter branch in https://github.com/dotnet/llvm-project repo
//

/**********************************************************************************/
// DebugInfo types shared by JIT-EE interface and EE-Debugger interface

class ICorDebugInfo
{
public:
    /*----------------------------- Boundary-info ---------------------------*/

    enum MappingTypes
    {
        NO_MAPPING  = -1,
        PROLOG      = -2,
        EPILOG      = -3,
        MAX_MAPPING_VALUE = -3 // Sentinal value. This should be set to the largest magnitude value in the enum
                               // so that the compression routines know the enum's range.
    };

    enum BoundaryTypes
    {
        NO_BOUNDARIES           = 0x00,     // No implicit boundaries
        STACK_EMPTY_BOUNDARIES  = 0x01,     // Boundary whenever the IL evaluation stack is empty
        NOP_BOUNDARIES          = 0x02,     // Before every CEE_NOP instruction
        CALL_SITE_BOUNDARIES    = 0x04,     // Before every CEE_CALL, CEE_CALLVIRT, etc instruction

        // Set of boundaries that debugger should always reasonably ask the JIT for.
        DEFAULT_BOUNDARIES      = STACK_EMPTY_BOUNDARIES | NOP_BOUNDARIES | CALL_SITE_BOUNDARIES
    };

    // Note that SourceTypes can be OR'd together - it's possible that
    // a sequence point will also be a stack_empty point, and/or a call site.
    // The debugger will check to see if a boundary offset's source field &
    // SEQUENCE_POINT is true to determine if the boundary is a sequence point.

    enum SourceTypes
    {
        SOURCE_TYPE_INVALID        = 0x00, // To indicate that nothing else applies
        SEQUENCE_POINT             = 0x01, // The debugger asked for it.
        STACK_EMPTY                = 0x02, // The stack is empty here
        CALL_SITE                  = 0x04, // This is a call site.
        NATIVE_END_OFFSET_UNKNOWN  = 0x08, // Indicates a epilog endpoint
        CALL_INSTRUCTION           = 0x10  // The actual instruction of a call.

    };

    struct OffsetMapping
    {
        uint32_t        nativeOffset;
        uint32_t        ilOffset; // IL offset or one of the special values in MappingTypes
        SourceTypes     source; // The debugger needs this so that
                                // we don't put Edit and Continue breakpoints where
                                // the stack isn't empty.  We can put regular breakpoints
                                // there, though, so we need a way to discriminate
                                // between offsets.
    };

    /*------------------------------ Var-info -------------------------------*/

    // Note: The debugger needs to target register numbers on platforms other than which the debugger itself
    // is running. To this end it maintains its own values for REGNUM_SP and REGNUM_AMBIENT_SP across multiple
    // platforms. So any change here that may effect these values should be reflected in the definitions
    // contained in debug/inc/DbgIPCEvents.h.
    enum RegNum
    {
#ifdef TARGET_X86
        REGNUM_EAX,
        REGNUM_ECX,
        REGNUM_EDX,
        REGNUM_EBX,
        REGNUM_ESP,
        REGNUM_EBP,
        REGNUM_ESI,
        REGNUM_EDI,
#elif TARGET_ARM
        REGNUM_R0,
        REGNUM_R1,
        REGNUM_R2,
        REGNUM_R3,
        REGNUM_R4,
        REGNUM_R5,
        REGNUM_R6,
        REGNUM_R7,
        REGNUM_R8,
        REGNUM_R9,
        REGNUM_R10,
        REGNUM_R11,
        REGNUM_R12,
        REGNUM_SP,
        REGNUM_LR,
        REGNUM_PC,
#elif TARGET_ARM64
        REGNUM_X0,
        REGNUM_X1,
        REGNUM_X2,
        REGNUM_X3,
        REGNUM_X4,
        REGNUM_X5,
        REGNUM_X6,
        REGNUM_X7,
        REGNUM_X8,
        REGNUM_X9,
        REGNUM_X10,
        REGNUM_X11,
        REGNUM_X12,
        REGNUM_X13,
        REGNUM_X14,
        REGNUM_X15,
        REGNUM_X16,
        REGNUM_X17,
        REGNUM_X18,
        REGNUM_X19,
        REGNUM_X20,
        REGNUM_X21,
        REGNUM_X22,
        REGNUM_X23,
        REGNUM_X24,
        REGNUM_X25,
        REGNUM_X26,
        REGNUM_X27,
        REGNUM_X28,
        REGNUM_FP,
        REGNUM_LR,
        REGNUM_SP,
        REGNUM_PC,
#elif TARGET_AMD64
        REGNUM_RAX,
        REGNUM_RCX,
        REGNUM_RDX,
        REGNUM_RBX,
        REGNUM_RSP,
        REGNUM_RBP,
        REGNUM_RSI,
        REGNUM_RDI,
        REGNUM_R8,
        REGNUM_R9,
        REGNUM_R10,
        REGNUM_R11,
        REGNUM_R12,
        REGNUM_R13,
        REGNUM_R14,
        REGNUM_R15,
#elif TARGET_LOONGARCH64
        REGNUM_R0,
        REGNUM_RA,
        REGNUM_TP,
        REGNUM_SP,
        REGNUM_A0,
        REGNUM_A1,
        REGNUM_A2,
        REGNUM_A3,
        REGNUM_A4,
        REGNUM_A5,
        REGNUM_A6,
        REGNUM_A7,
        REGNUM_T0,
        REGNUM_T1,
        REGNUM_T2,
        REGNUM_T3,
        REGNUM_T4,
        REGNUM_T5,
        REGNUM_T6,
        REGNUM_T7,
        REGNUM_T8,
        REGNUM_X0,
        REGNUM_FP,
        REGNUM_S0,
        REGNUM_S1,
        REGNUM_S2,
        REGNUM_S3,
        REGNUM_S4,
        REGNUM_S5,
        REGNUM_S6,
        REGNUM_S7,
        REGNUM_S8,
        REGNUM_PC,
#else
        PORTABILITY_WARNING("Register numbers not defined on this platform")
#endif
        REGNUM_COUNT,
        REGNUM_AMBIENT_SP, // ambient SP support. Ambient SP is the original SP in the non-BP based frame.
                           // Ambient SP should not change even if there are push/pop operations in the method.

#ifdef TARGET_X86
        REGNUM_FP = REGNUM_EBP,
        REGNUM_SP = REGNUM_ESP,
#elif TARGET_AMD64
        REGNUM_SP = REGNUM_RSP,
#elif TARGET_ARM
#ifdef REDHAWK
        REGNUM_FP = REGNUM_R7,
#else
        REGNUM_FP = REGNUM_R11,
#endif //REDHAWK
#elif TARGET_ARM64
        //Nothing to do here. FP is already alloted.
#elif TARGET_LOONGARCH64
        //Nothing to do here. FP is already alloted.
#else
        // RegNum values should be properly defined for this platform
        REGNUM_FP = 0,
        REGNUM_SP = 1,
#endif

    };

    // VarLoc describes the location of a native variable.  Note that currently, VLT_REG_BYREF and VLT_STK_BYREF
    // are only used for value types on X64.

    enum VarLocType
    {
        VLT_REG,        // variable is in a register
        VLT_REG_BYREF,  // address of the variable is in a register
        VLT_REG_FP,     // variable is in an fp register
        VLT_STK,        // variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_STK_BYREF,  // address of the variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_REG_REG,    // variable lives in two registers
        VLT_REG_STK,    // variable lives partly in a register and partly on the stack
        VLT_STK_REG,    // reverse of VLT_REG_STK
        VLT_STK2,       // variable lives in two slots on the stack
        VLT_FPSTK,      // variable lives on the floating-point stack
        VLT_FIXED_VA,   // variable is a fixed argument in a varargs function (relative to VARARGS_HANDLE)

        VLT_COUNT,
        VLT_INVALID,
    };

    // VLT_REG/VLT_REG_FP -- Any pointer-sized enregistered value (TYP_INT, TYP_REF, etc)
    // eg. EAX
    // VLT_REG_BYREF -- the specified register contains the address of the variable
    // eg. [EAX]

    struct vlReg
    {
        RegNum      vlrReg;
    };

    // VLT_STK -- Any 32 bit value which is on the stack
    // eg. [ESP+0x20], or [EBP-0x28]
    // VLT_STK_BYREF -- the specified stack location contains the address of the variable
    // eg. mov EAX, [ESP+0x20]; [EAX]

    struct vlStk
    {
        RegNum      vlsBaseReg;
        signed      vlsOffset;
    };

    // VLT_REG_REG -- TYP_LONG with both uint32_ts enregistred
    // eg. RBM_EAXEDX

    struct vlRegReg
    {
        RegNum      vlrrReg1;
        RegNum      vlrrReg2;
    };

    // VLT_REG_STK -- Partly enregistered TYP_LONG
    // eg { LowerDWord=EAX UpperDWord=[ESP+0x8] }

    struct vlRegStk
    {
        RegNum      vlrsReg;
        struct
        {
            RegNum      vlrssBaseReg;
            signed      vlrssOffset;
        }           vlrsStk;
    };

    // VLT_STK_REG -- Partly enregistered TYP_LONG
    // eg { LowerDWord=[ESP+0x8] UpperDWord=EAX }

    struct vlStkReg
    {
        struct
        {
            RegNum      vlsrsBaseReg;
            signed      vlsrsOffset;
        }           vlsrStk;
        RegNum      vlsrReg;
    };

    // VLT_STK2 -- Any 64 bit value which is on the stack,
    // in 2 successive DWords.
    // eg 2 DWords at [ESP+0x10]

    struct vlStk2
    {
        RegNum      vls2BaseReg;
        signed      vls2Offset;
    };

    // VLT_FPSTK -- enregisterd TYP_DOUBLE (on the FP stack)
    // eg. ST(3). Actually it is ST("FPstkHeigth - vpFpStk")

    struct vlFPstk
    {
        unsigned        vlfReg;
    };

    // VLT_FIXED_VA -- fixed argument of a varargs function.
    // The argument location depends on the size of the variable
    // arguments (...). Inspecting the VARARGS_HANDLE indicates the
    // location of the first arg. This argument can then be accessed
    // relative to the position of the first arg

    struct vlFixedVarArg
    {
        unsigned        vlfvOffset;
    };

    // VLT_MEMORY

    struct vlMemory
    {
        void        *rpValue; // pointer to the in-process
        // location of the value.
    };

    struct VarLoc
    {
        VarLocType      vlType;

        union
        {
            ICorDebugInfo::vlReg           vlReg;
            ICorDebugInfo::vlStk           vlStk;
            ICorDebugInfo::vlRegReg        vlRegReg;
            ICorDebugInfo::vlRegStk        vlRegStk;
            ICorDebugInfo::vlStkReg        vlStkReg;
            ICorDebugInfo::vlStk2          vlStk2;
            ICorDebugInfo::vlFPstk         vlFPstk;
            ICorDebugInfo::vlFixedVarArg   vlFixedVarArg;
            ICorDebugInfo::vlMemory        vlMemory;
        };
    };

    // This is used to report implicit/hidden arguments

    enum
    {
        VARARGS_HND_ILNUM   = -1, // Value for the CORINFO_VARARGS_HANDLE varNumber
        RETBUF_ILNUM        = -2, // Pointer to the return-buffer
        TYPECTXT_ILNUM      = -3, // ParamTypeArg for CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG

        UNKNOWN_ILNUM       = -4, // Unknown variable

        MAX_ILNUM           = -4  // Sentinal value. This should be set to the largest magnitude value in th enum
                                  // so that the compression routines know the enum's range.
    };

    struct ILVarInfo
    {
        uint32_t        startOffset;
        uint32_t        endOffset;
        uint32_t        varNumber;
    };

    struct NativeVarInfo
    {
        uint32_t        startOffset;
        uint32_t        endOffset;
        uint32_t        varNumber;
        VarLoc          loc;
    };
};
