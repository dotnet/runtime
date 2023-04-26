// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_LOONGARCH64)

// The LOONGARCH64 instructions are all 32 bits in size.
// we use an unsigned int to hold the encoded instructions.
// This typedef defines the type that we use to hold encoded instructions.
//
typedef unsigned int code_t;

/************************************************************************/
/*         Routines that compute the size of / encode instructions      */
/************************************************************************/

struct CnsVal
{
    ssize_t cnsVal;
    bool    cnsReloc;
};

#ifdef DEBUG
/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/
void emitDisInsName(code_t code, const BYTE* addr, instrDesc* id);
#endif // DEBUG

void emitIns_J_cond_la(instruction ins, BasicBlock* dst, regNumber reg1 = REG_R0, regNumber reg2 = REG_R0);
void emitIns_I_la(emitAttr attr, regNumber reg, ssize_t imm);

/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
instrDesc* emitNewInstrCallDir(int              argCnt,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize));

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize));

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool emitInsIsLoad(instruction ins);
bool emitInsIsStore(instruction ins);
bool emitInsIsLoadOrStore(instruction ins);

emitter::code_t emitInsCode(instruction ins /*, insFormat fmt*/);

// Generate code for a load or store operation and handle the case of contained GT_LEA op1 with [base + offset]
void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);

//  Emit the 32-bit LOONGARCH64 instruction 'code' into the 'dst'  buffer
unsigned emitOutput_Instr(BYTE* dst, code_t code);

// Method to do check if mov is redundant with respect to the last instruction.
// If yes, the caller of this method can choose to omit current mov instruction.
static bool IsMovInstruction(instruction ins);
bool IsRedundantMov(instruction ins, emitAttr size, regNumber dst, regNumber src, bool canSkip);

/************************************************************************/
/*           Public inline informational methods                        */
/************************************************************************/

public:
// Returns true if 'value' is a legal signed immediate 12 bit encoding.
static bool isValidSimm12(ssize_t value)
{
    return -(((int)1) << 11) <= value && value < (((int)1) << 11);
};

// Returns true if 'value' is a legal unsigned immediate 12 bit encoding.
static bool isValidUimm12(ssize_t value)
{
    return (0 == (value >> 12));
}

// Returns true if 'value' is a legal unsigned immediate 11 bit encoding.
static bool isValidUimm11(ssize_t value)
{
    return (0 == (value >> 11));
}

// Returns true if 'value' is a legal signed immediate 20 bit encoding.
static bool isValidSimm20(ssize_t value)
{
    return -(((int)1) << 19) <= value && value < (((int)1) << 19);
};

// Returns true if 'value' is a legal signed immediate 38 bit encoding.
static bool isValidSimm38(ssize_t value)
{
    return -(((ssize_t)1) << 37) <= value && value < (((ssize_t)1) << 37);
};

// Returns the number of bits used by the given 'size'.
inline static unsigned getBitWidth(emitAttr size)
{
    assert(size <= EA_8BYTE);
    return (unsigned)size * BITS_PER_BYTE;
}

inline static bool isGeneralRegister(regNumber reg)
{
    return (reg >= REG_INT_FIRST) && (reg <= REG_INT_LAST);
}

inline static bool isGeneralRegisterOrR0(regNumber reg)
{
    return (reg >= REG_FIRST) && (reg <= REG_INT_LAST);
} // Includes REG_R0

inline static bool isFloatReg(regNumber reg)
{
    return (reg >= REG_FP_FIRST && reg <= REG_FP_LAST);
}

/************************************************************************/
/*                   Output target-independent instructions             */
/************************************************************************/

void emitIns_J(instruction ins, BasicBlock* dst, int instrCount = 0);

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);
void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);
void emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs);

void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt = INS_OPTS_NONE);

void emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags)
{
    emitIns_R_R(ins, attr, reg1, reg2);
}

void emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R_I(instruction ins,
                     emitAttr    attr,
                     regNumber   reg1,
                     regNumber   reg2,
                     regNumber   reg3,
                     ssize_t     imm,
                     insOpts     opt      = INS_OPTS_NONE,
                     emitAttr    attrReg2 = EA_UNKNOWN);

void emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4);

void emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber tmpReg, CORINFO_FIELD_HANDLE fldHnd, int offs);

void emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_AI(instruction ins,
                  emitAttr    attr,
                  regNumber   reg,
                  ssize_t disp DEBUGARG(size_t targetHandle = 0) DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

enum EmitCallType
{

    // I have included here, but commented out, all the values used by the x86 emitter.
    // However, LOONGARCH has a much reduced instruction set, and so the LOONGARCH emitter only
    // supports a subset of the x86 variants.  By leaving them commented out, it becomes
    // a compile time error if code tries to use them (and hopefully see this comment
    // and know why they are unavailable on LOONGARCH), while making it easier to stay
    // in-sync with x86 and possibly add them back in if needed.

    EC_FUNC_TOKEN, //   Direct call to a helper/static/nonvirtual/global method
                   //  EC_FUNC_TOKEN_INDIR,    // Indirect call to a helper/static/nonvirtual/global method
    // EC_FUNC_ADDR,  // Direct call to an absolute address

    //  EC_FUNC_VIRTUAL,        // Call to a virtual method (using the vtable)
    EC_INDIR_R, // Indirect call via register
                //  EC_INDIR_SR,            // Indirect call via stack-reference (local var)
                //  EC_INDIR_C,             // Indirect call via static class var
                //  EC_INDIR_ARD,           // Indirect call via an addressing mode

    EC_COUNT
};

void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,
                  INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                  void*    addr,
                  ssize_t  argSize,
                  emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  const DebugInfo& di,
                  regNumber        ireg   = REG_NA,
                  regNumber        xreg   = REG_NA,
                  unsigned         xmul   = 0,
                  ssize_t          disp   = 0,
                  bool             isJump = false);

unsigned emitOutputCall(insGroup* ig, BYTE* dst, instrDesc* id, code_t code);

unsigned get_curTotalCodeSize(); // bytes of code

#endif // TARGET_LOONGARCH64
