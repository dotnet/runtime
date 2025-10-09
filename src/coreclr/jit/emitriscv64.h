// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_RISCV64)

// The RISCV64 instructions are all 32 / 16 bits in size.
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
                               emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                               bool             hasAsyncRet);

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                               bool             hasAsyncRet);

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool emitInsIsLoad(instruction ins);
bool emitInsIsStore(instruction ins);
bool emitInsIsLoadOrStore(instruction ins);

// RVC emitters
bool tryEmitCompressedIns_R_R_R(
    instruction ins, emitAttr attr, regNumber rd, regNumber rs1, regNumber rs2, insOpts opt);

// RVC helpers
instruction tryGetCompressedIns_R_R_R(
    instruction ins, emitAttr attr, regNumber rd, regNumber rs1, regNumber rs2, insOpts opt);
unsigned    tryGetRvcRegisterNumber(regNumber reg);
instruction getCompressedArithmeticIns(instruction ins);
regNumber   getRegNumberFromRvcReg(unsigned rvcReg);

void emitDispInsName(
    code_t code, const BYTE* addr, bool doffs, unsigned insOffset, const instrDesc* id, const insGroup* ig);
void emitDispInsInstrNum(const instrDesc* id) const;
bool emitDispBranch(unsigned opcode2, unsigned rs1, unsigned rs2, const instrDesc* id, const insGroup* ig) const;
void emitDispBranchOffset(const instrDesc* id, const insGroup* ig) const;
void emitDispBranchLabel(const instrDesc* id) const;
bool emitDispBranchInstrType(unsigned opcode2, bool is_zero_reg, bool& print_second_reg) const;
void emitDispIllegalInstruction(code_t instructionCode);
void emitDispImmediate(ssize_t imm, bool newLine = true, unsigned regBase = REG_ZERO);

static emitter::code_t emitInsCode(instruction ins /*, insFormat fmt*/);

// Generate code for a load or store operation and handle the case of contained GT_LEA op1 with [base + offset]
void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);

// Emit the 16/32-bit RISCV64 instruction 'code' into the 'dst'  buffer
unsigned emitOutput_Instr(BYTE* dst, code_t code) const;

ssize_t emitOutputInstrJumpDistance(const BYTE* src, const insGroup* ig, instrDescJmp* jmp);
void    emitOutputInstrJumpDistanceHelper(const insGroup* ig,
                                          instrDescJmp*   jmp,
                                          UNATIVE_OFFSET& dstOffs,
                                          const BYTE*&    dstAddr) const;

// Method to do check if mov is redundant with respect to the last instruction.
// If yes, the caller of this method can choose to omit current mov instruction.
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

static code_t insEncodeCRTypeInstr(instruction ins, unsigned rdRs1, unsigned rs2);
static code_t insEncodeCATypeInstr(instruction ins, unsigned rdRs1Rvc, unsigned rs2Rvc);

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
BYTE* emitOutputInstr_OptsRc(BYTE* dst, const instrDesc* id, instruction* ins);
BYTE* emitOutputInstr_OptsRl(BYTE* dst, instrDesc* id, instruction* ins);
BYTE* emitOutputInstr_OptsJump(BYTE* dst, instrDescJmp* jmp, const insGroup* ig, instruction* ins);
BYTE* emitOutputInstr_OptsC(BYTE* dst, instrDesc* id, const insGroup* ig, size_t* size);
BYTE* emitOutputInstr_OptsI(BYTE* dst, instrDesc* id, instruction* ins);

static unsigned TrimSignedToImm12(ssize_t imm12);
static unsigned TrimSignedToImm13(ssize_t imm13);
static unsigned TrimSignedToImm20(ssize_t imm20);
static unsigned TrimSignedToImm21(ssize_t imm21);

// Major opcode of 32-bit & 16-bit instructions as per "The RISC-V Instruction Set Manual", chapter "RV32/64G
// Instruction Set Listings", table "RISC-V base opcode map" and chapter "RVC Instruction Set Listings", table "RVC
// opcode map instructions"
enum class MajorOpcode
{
    // clang-format off
    // inst[1:0] = 11
    // inst[4:2]    000,    001,     010,      011,     100,    101,   110,          111 (>32Bit)
    /* inst[6:5] */
    /*        00 */ Load,   LoadFp,  Custom0,  MiscMem, OpImm,  Auipc, OpImm32,      Encoding48Bit1,
    /*        01 */ Store,  StoreFp, Custom1,  Amo,     Op,     Lui,   Op32,         Encoding64Bit,
    /*        11 */ MAdd,   MSub,    NmSub,    NmAdd,   OpFp,   OpV,   Custom2Rv128, Encoding48Bit2,
    /*        11 */ Branch, Jalr,    Reserved, Jal,     System, OpVe,  Custom3Rv128, Encoding80Bit,

    // Compressed (RVC) instructions
    // inst[15:13]  000,      001,   010,  011,         100,         101,   110,  111
    /* inst[1:0] */
    /*        00 */ Addi4Spn, Fld,   Lw,   Ld,          Reserved2,   Fsd,   Sw,   Sd,
    /*        01 */ Addi,     Addiw, Li,   LuiAddi16Sp, MiscAlu,     J,     Beqz, Bnez,
    /*        10 */ Slli,     FldSp, LwSp, Ldsp,        JrJalrMvAdd, FsdSp, SwSp, SdSp,
    // clang-format on
};

//------------------------------------------------------------------------
// GetMajorOpcode: extracts major opcode from an instruction
//
// Arguments:
//    instr - instruction code
//
// Return Value:
//    Major opcode
//
static MajorOpcode GetMajorOpcode(code_t instr);

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

// Returns true if 'value' is a legal signed immediate 32-bit encoding with the offset adjustment.
static bool isValidSimm32(ssize_t value)
{
    return (-(((ssize_t)1) << 31) - 0x800) <= value && value < (((ssize_t)1) << 31) - 0x800;
}

//------------------------------------------------------------------------
// isSingleInstructionFpImm: checks if the floating-point constant can be synthesized with one instruction
//
// Arguments:
//    value   - the constant to be imm'ed
//    size    - size of the target immediate
//    outBits - [out] the bits of the immediate
//
// Return Value:
//    Whether the floating-point immediate can be synthesized with one instruction
//
static bool isSingleInstructionFpImm(double value, emitAttr size, int64_t* outBits)
{
    assert(size == EA_4BYTE || size == EA_8BYTE);
    *outBits = (size == EA_4BYTE)
                   ? (int32_t)BitOperations::SingleToUInt32Bits(FloatingPointUtils::convertToSingle(value))
                   : (int64_t)BitOperations::DoubleToUInt64Bits(value);
    return isValidSimm12(*outBits) || (((*outBits & 0xfff) == 0) && isValidSimm20(*outBits >> 12));
}

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

void emitIns_J(instruction ins, BasicBlock* dst);

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

void emitIns_R_C(instruction ins, emitAttr attr, regNumber destReg, regNumber addrReg, CORINFO_FIELD_HANDLE fldHnd);

void emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_AI(instruction  ins,
                  emitAttr     attr,
                  regNumber    reg,
                  ssize_t disp DEBUGARG(size_t targetHandle = 0) DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

unsigned emitOutputCall(const insGroup* ig, BYTE* dst, instrDesc* id);

unsigned get_curTotalCodeSize(); // bytes of code

//------------------------------------------------------------------------
// emitIsCmpJump: checks if it's a compare and jump (branch)
//
// Arguments:
//    jmp - the instruction to check
//
inline static bool emitIsCmpJump(instrDesc* jmp)
{
    return jmp->idInsIs(INS_beqz, INS_bnez, INS_bne, INS_beq, INS_blt, INS_bltu, INS_bge, INS_bgeu);
}

//------------------------------------------------------------------------
// emitIsUncondJump: checks if it's an unconditional jump
//
// Arguments:
//    jmp - the instruction to check
//
inline static bool emitIsUncondJump(instrDesc* jmp)
{
    return jmp->idInsIs(INS_j, INS_jal);
}

#endif // TARGET_RISCV64
