// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_RISCV64)

// The RISCV64 instructions are all 32 bits in size.
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

const char* emitFPregName(unsigned reg, bool varName = true);
const char* emitVectorRegName(regNumber reg);
#endif // DEBUG

void emitIns_J_cond_la(instruction ins, BasicBlock* dst, regNumber reg1 = REG_R0, regNumber reg2 = REG_R0);

void emitLoadImmediate(emitAttr attr, regNumber reg, ssize_t imm);

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

void emitDispInsName(
    code_t code, const BYTE* addr, bool doffs, unsigned insOffset, const instrDesc* id, const insGroup* ig);
void emitDispInsInstrNum(const instrDesc* id) const;
bool emitDispBranch(unsigned         opcode2,
                    const char*      register1Name,
                    const char*      register2Name,
                    const instrDesc* id,
                    const insGroup*  ig) const;
void emitDispBranchOffset(const instrDesc* id, const insGroup* ig) const;
void emitDispBranchLabel(const instrDesc* id) const;
bool emitDispBranchInstrType(unsigned opcode2) const;
void emitDispIllegalInstruction(code_t instructionCode);

emitter::code_t emitInsCode(instruction ins /*, insFormat fmt*/) const;

// Generate code for a load or store operation and handle the case of contained GT_LEA op1 with [base + offset]
void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);

// Emit the 32-bit RISCV64 instruction 'code' into the 'dst'  buffer
unsigned emitOutput_Instr(BYTE* dst, code_t code) const;

ssize_t emitOutputInstrJumpDistance(const BYTE* src, const insGroup* ig, instrDescJmp* jmp);
void emitOutputInstrJumpDistanceHelper(const insGroup* ig,
                                       instrDescJmp*   jmp,
                                       UNATIVE_OFFSET& dstOffs,
                                       const BYTE*&    dstAddr) const;

// Method to do check if mov is redundant with respect to the last instruction.
// If yes, the caller of this method can choose to omit current mov instruction.
static bool IsMovInstruction(instruction ins);
bool IsRedundantMov(instruction ins, emitAttr size, regNumber dst, regNumber src, bool canSkip);
bool IsRedundantLdStr(
    instruction ins, regNumber reg1, regNumber reg2, ssize_t imm, emitAttr size, insFormat fmt); // New functions end.

static code_t insEncodeRTypeInstr(
    unsigned opcode, unsigned rd, unsigned funct3, unsigned rs1, unsigned rs2, unsigned funct7);
static code_t insEncodeITypeInstr(unsigned opcode, unsigned rd, unsigned funct3, unsigned rs1, unsigned imm12);
static code_t insEncodeSTypeInstr(unsigned opcode, unsigned funct3, unsigned rs1, unsigned rs2, unsigned imm12);
static code_t insEncodeUTypeInstr(unsigned opcode, unsigned rd, unsigned imm20);
static code_t insEncodeBTypeInstr(unsigned opcode, unsigned funct3, unsigned rs1, unsigned rs2, unsigned imm13);
static code_t insEncodeJTypeInstr(unsigned opcode, unsigned rd, unsigned imm21);

#ifdef DEBUG
static void emitOutput_RTypeInstr_SanityCheck(instruction ins, regNumber rd, regNumber rs1, regNumber rs2);
static void emitOutput_ITypeInstr_SanityCheck(
    instruction ins, regNumber rd, regNumber rs1, unsigned immediate, unsigned opcode);
static void emitOutput_STypeInstr_SanityCheck(instruction ins, regNumber rs1, regNumber rs2);
static void emitOutput_UTypeInstr_SanityCheck(instruction ins, regNumber rd);
static void emitOutput_BTypeInstr_SanityCheck(instruction ins, regNumber rs1, regNumber rs2);
static void emitOutput_JTypeInstr_SanityCheck(instruction ins, regNumber rd);
#endif // DEBUG

static unsigned castFloatOrIntegralReg(regNumber reg);

unsigned emitOutput_RTypeInstr(BYTE* dst, instruction ins, regNumber rd, regNumber rs1, regNumber rs2) const;
unsigned emitOutput_ITypeInstr(BYTE* dst, instruction ins, regNumber rd, regNumber rs1, unsigned imm12) const;
unsigned emitOutput_STypeInstr(BYTE* dst, instruction ins, regNumber rs1, regNumber rs2, unsigned imm12) const;
unsigned emitOutput_UTypeInstr(BYTE* dst, instruction ins, regNumber rd, unsigned imm20) const;
unsigned emitOutput_BTypeInstr(BYTE* dst, instruction ins, regNumber rs1, regNumber rs2, unsigned imm13) const;
unsigned emitOutput_BTypeInstr_InvertComparation(
    BYTE* dst, instruction ins, regNumber rs1, regNumber rs2, unsigned imm13) const;
unsigned emitOutput_JTypeInstr(BYTE* dst, instruction ins, regNumber rd, unsigned imm21) const;

BYTE* emitOutputInstr_OptsReloc(BYTE* dst, const instrDesc* id, instruction* ins);
BYTE* emitOutputInstr_OptsI(BYTE* dst, const instrDesc* id);
BYTE* emitOutputInstr_OptsI8(BYTE* dst, const instrDesc* id, ssize_t immediate, regNumber reg1);
BYTE* emitOutputInstr_OptsI32(BYTE* dst, ssize_t immediate, regNumber reg1);
BYTE* emitOutputInstr_OptsRc(BYTE* dst, const instrDesc* id, instruction* ins);
BYTE* emitOutputInstr_OptsRcReloc(BYTE* dst, instruction* ins, unsigned offset, regNumber reg1);
BYTE* emitOutputInstr_OptsRcNoReloc(BYTE* dst, instruction* ins, unsigned offset, regNumber reg1);
BYTE* emitOutputInstr_OptsRl(BYTE* dst, instrDesc* id, instruction* ins);
BYTE* emitOutputInstr_OptsRlReloc(BYTE* dst, ssize_t igOffs, regNumber reg1);
BYTE* emitOutputInstr_OptsRlNoReloc(BYTE* dst, ssize_t igOffs, regNumber reg1);
BYTE* emitOutputInstr_OptsJalr(BYTE* dst, instrDescJmp* jmp, const insGroup* ig, instruction* ins);
BYTE* emitOutputInstr_OptsJalr8(BYTE* dst, const instrDescJmp* jmp, instruction ins, ssize_t immediate);
BYTE* emitOutputInstr_OptsJalr24(BYTE* dst, ssize_t immediate);
BYTE* emitOutputInstr_OptsJalr28(BYTE* dst, const instrDescJmp* jmp, instruction ins, ssize_t immediate);
BYTE* emitOutputInstr_OptsJCond(BYTE* dst, instrDesc* id, const insGroup* ig, instruction* ins);
BYTE* emitOutputInstr_OptsJ(BYTE* dst, instrDesc* id, const insGroup* ig, instruction* ins);
BYTE* emitOutputInstr_OptsC(BYTE* dst, instrDesc* id, const insGroup* ig, size_t* size);

static unsigned TrimSignedToImm12(int imm12);
static unsigned TrimSignedToImm13(int imm13);
static unsigned TrimSignedToImm20(int imm20);
static unsigned TrimSignedToImm21(int imm21);

/************************************************************************/
/*           Public inline informational methods                        */
/************************************************************************/

public:
// Returns true if 'value' is a legal signed immediate 13 bit encoding.
static bool isValidSimm13(ssize_t value)
{
    return -(((int)1) << 12) <= value && value < (((int)1) << 12);
};

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

// Returns true if 'value' is a legal unsigned immediate 5 bit encoding.
static bool isValidUimm5(ssize_t value)
{
    return (0 == (value >> 5));
}

// Returns true if 'value' is a legal signed immediate 20 bit encoding.
static bool isValidSimm20(ssize_t value)
{
    return -(((int)1) << 19) <= value && value < (((int)1) << 19);
};

// Returns true if 'value' is a legal unsigned immediate 20 bit encoding.
static bool isValidUimm20(ssize_t value)
{
    return (0 == (value >> 20));
};

// Returns true if 'value' is a legal signed immediate 21 bit encoding.
static bool isValidSimm21(ssize_t value)
{
    return -(((int)1) << 20) <= value && value < (((int)1) << 20);
};

// Returns true if 'value' is a legal signed immediate 32 bit encoding.
static bool isValidSimm32(ssize_t value)
{
    return -(((ssize_t)1) << 31) <= value && value < (((ssize_t)1) << 31);
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
void emitIns_S_R_R(instruction ins, emitAttr attr, regNumber ireg, regNumber tmpReg, int varx, int offs);
void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);
void emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs);

void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt = INS_OPTS_NONE);

void emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt = INS_OPTS_NONE);

void emitIns_Mov(emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip = false);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags)
{
    _ASSERTE(!"RISCV64: NYI");
}

void emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt = INS_OPTS_NONE);

void emitIns_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, ssize_t imm1, ssize_t imm2, insOpts opt = INS_OPTS_NONE);

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
    // However, RISCV64 has a much reduced instruction set, and so the RISCV64 emitter only
    // supports a subset of the x86 variants.  By leaving them commented out, it becomes
    // a compile time error if code tries to use them (and hopefully see this comment
    // and know why they are unavailable on RISCV64), while making it easier to stay
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

unsigned emitOutputCall(const insGroup* ig, BYTE* dst, instrDesc* id, code_t code);

unsigned get_curTotalCodeSize(); // bytes of code

#endif // TARGET_RISCV64
