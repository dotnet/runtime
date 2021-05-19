// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_ARM)

// This typedef defines the type that we use to hold encoded instructions.
//
typedef unsigned int code_t;

/************************************************************************/
/*         Routines that compute the size of / encode instructions      */
/************************************************************************/

insSize emitInsSize(insFormat insFmt);

#ifdef FEATURE_ITINSTRUCTION
BYTE* emitOutputIT(BYTE* dst, instruction ins, insFormat fmt, code_t condcode);
#endif // FEATURE_ITINSTRUCTION

BYTE* emitOutputLJ(insGroup* ig, BYTE* dst, instrDesc* id);
BYTE* emitOutputShortBranch(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, instrDescJmp* id);

unsigned emitOutput_Thumb1Instr(BYTE* dst, code_t code);
unsigned emitOutput_Thumb2Instr(BYTE* dst, code_t code);

/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/

#ifdef DEBUG

void emitDispInst(instruction ins, insFlags flags);
void emitDispImm(int imm, bool addComma, bool alwaysHex = false);
void emitDispReloc(BYTE* addr);
void emitDispCond(int cond);
void emitDispShiftOpts(insOpts opt);
void emitDispRegmask(int imm, bool encodedPC_LR);
void emitDispRegRange(regNumber reg, int len, emitAttr attr);
void emitDispReg(regNumber reg, emitAttr attr, bool addComma);
void emitDispAddrR(regNumber reg, emitAttr attr);
void emitDispAddrRI(regNumber reg, int imm, emitAttr attr);
void emitDispAddrRR(regNumber reg1, regNumber reg2, emitAttr attr);
void emitDispAddrRRI(regNumber reg1, regNumber reg2, int imm, emitAttr attr);
void emitDispAddrPUW(regNumber reg, int imm, insOpts opt, emitAttr attr);
void emitDispGC(emitAttr attr);

void emitDispInsHelp(instrDesc* id,
                     bool       isNew,
                     bool       doffs,
                     bool       asmfm,
                     unsigned   offs = 0,
                     BYTE*      code = 0,
                     size_t     sz   = 0,
                     insGroup*  ig   = NULL);
void emitDispIns(instrDesc* id,
                 bool       isNew,
                 bool       doffs,
                 bool       asmfm,
                 unsigned   offs = 0,
                 BYTE*      code = 0,
                 size_t     sz   = 0,
                 insGroup*  ig   = NULL);

#endif // DEBUG

/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
instrDesc* emitNewInstrCallDir(
    int argCnt, VARSET_VALARG_TP GCvars, regMaskTP gcrefRegs, regMaskTP byrefRegs, emitAttr retSize);

instrDesc* emitNewInstrCallInd(
    int argCnt, ssize_t disp, VARSET_VALARG_TP GCvars, regMaskTP gcrefRegs, regMaskTP byrefRegs, emitAttr retSize);

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool emitInsIsCompare(instruction ins);
bool emitInsIsLoad(instruction ins);
bool emitInsIsStore(instruction ins);
bool emitInsIsLoadOrStore(instruction ins);

emitter::insFormat emitInsFormat(instruction ins);
emitter::code_t emitInsCode(instruction ins, insFormat fmt);

// Generate code for a load or store operation and handle the case
// of contained GT_LEA op1 with [base + index<<scale + offset]
void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);
void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir, int offset);

static bool IsMovInstruction(instruction ins);
static bool isModImmConst(int imm);
static int encodeModImmConst(int imm);

static int insUnscaleImm(instruction ins, int imm);

/************************************************************************/
/*           Public inline informational methods                        */
/************************************************************************/

public:
inline static bool isLowRegister(regNumber reg)
{
    return (reg <= REG_R7);
}

inline static bool isGeneralRegister(regNumber reg)
{
    return (reg <= REG_R15);
}

inline static bool isFloatReg(regNumber reg)
{
    return (reg >= REG_F0 && reg <= REG_F31);
}

inline static bool isDoubleReg(regNumber reg)
{
    return isFloatReg(reg) && ((reg % 2) == 0);
}

inline static bool insSetsFlags(insFlags flags)
{
    return (flags != INS_FLAGS_NOT_SET);
}

inline static bool insDoesNotSetFlags(insFlags flags)
{
    return (flags != INS_FLAGS_SET);
}

inline static insFlags insMustSetFlags(insFlags flags)
{
    return (flags == INS_FLAGS_SET) ? INS_FLAGS_SET : INS_FLAGS_NOT_SET;
}

inline static insFlags insMustNotSetFlags(insFlags flags)
{
    return (flags == INS_FLAGS_NOT_SET) ? INS_FLAGS_NOT_SET : INS_FLAGS_SET;
}

inline static bool insOptsNone(insOpts opt)
{
    return (opt == INS_OPTS_NONE);
}

inline static bool insOptAnyInc(insOpts opt)
{
    return (opt == INS_OPTS_LDST_PRE_DEC) || (opt == INS_OPTS_LDST_POST_INC);
}

inline static bool insOptsPreDec(insOpts opt)
{
    return (opt == INS_OPTS_LDST_PRE_DEC);
}

inline static bool insOptsPostInc(insOpts opt)
{
    return (opt == INS_OPTS_LDST_POST_INC);
}

inline static bool insOptAnyShift(insOpts opt)
{
    return ((opt >= INS_OPTS_RRX) && (opt <= INS_OPTS_ROR));
}

inline static bool insOptsRRX(insOpts opt)
{
    return (opt == INS_OPTS_RRX);
}

inline static bool insOptsLSL(insOpts opt)
{
    return (opt == INS_OPTS_LSL);
}

inline static bool insOptsLSR(insOpts opt)
{
    return (opt == INS_OPTS_LSR);
}

inline static bool insOptsASR(insOpts opt)
{
    return (opt == INS_OPTS_ASR);
}

inline static bool insOptsROR(insOpts opt)
{
    return (opt == INS_OPTS_ROR);
}

// Returns the number of bits used by the given 'size'.
inline static unsigned getBitWidth(emitAttr size)
{
    assert(size <= EA_8BYTE);
    return (unsigned)size * BITS_PER_BYTE;
}

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
static bool emitIns_valid_imm_for_alu(int imm);
static bool emitIns_valid_imm_for_mov(int imm);
static bool emitIns_valid_imm_for_small_mov(regNumber reg, int imm, insFlags flags);
static bool emitIns_valid_imm_for_add(int imm, insFlags flags = INS_FLAGS_DONT_CARE);
static bool emitIns_valid_imm_for_cmp(int imm, insFlags flags);
static bool emitIns_valid_imm_for_add_sp(int imm);
static bool emitIns_valid_imm_for_ldst_offset(int imm, emitAttr size);
static bool emitIns_valid_imm_for_vldst_offset(int imm);

void emitIns(instruction ins);

void emitIns_I(instruction ins, emitAttr attr, target_ssize_t imm);

void emitIns_R(instruction ins, emitAttr attr, regNumber reg);

void emitIns_R_I(
    instruction ins, emitAttr attr, regNumber reg, target_ssize_t imm, insFlags flags = INS_FLAGS_DONT_CARE);
void emitIns_MovRelocatableImmediate(instruction ins, emitAttr attr, regNumber reg, BYTE* addr);

void emitIns_Mov(instruction ins,
                 emitAttr    attr,
                 regNumber   dstReg,
                 regNumber   srgReg,
                 bool        canSkip,
                 insFlags    flags = INS_FLAGS_DONT_CARE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags = INS_FLAGS_DONT_CARE);

void emitIns_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, int imm1, int imm2, insFlags flags = INS_FLAGS_DONT_CARE);

void emitIns_R_R_I(instruction ins,
                   emitAttr    attr,
                   regNumber   reg1,
                   regNumber   reg2,
                   int         imm,
                   insFlags    flags = INS_FLAGS_DONT_CARE,
                   insOpts     opt   = INS_OPTS_NONE);

void emitIns_R_R_R(instruction ins,
                   emitAttr    attr,
                   regNumber   reg1,
                   regNumber   reg2,
                   regNumber   reg3,
                   insFlags    flags = INS_FLAGS_DONT_CARE);

void emitIns_R_R_I_I(instruction ins,
                     emitAttr    attr,
                     regNumber   reg1,
                     regNumber   reg2,
                     int         imm1,
                     int         imm2,
                     insFlags    flags = INS_FLAGS_DONT_CARE);

void emitIns_R_R_R_I(instruction ins,
                     emitAttr    attr,
                     regNumber   reg1,
                     regNumber   reg2,
                     regNumber   reg3,
                     int         imm,
                     insFlags    flags = INS_FLAGS_DONT_CARE,
                     insOpts     opt   = INS_OPTS_NONE);

void emitIns_R_R_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4);

void emitIns_C(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, int offs);

void emitIns_S(instruction ins, emitAttr attr, int varx, int offs);

void emitIns_genStackOffset(regNumber r, int varx, int offs, bool isFloatUsage, regNumber* pBaseReg);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs, regNumber* pBaseReg = nullptr);

void emitIns_S_I(instruction ins, emitAttr attr, int varx, int offs, int val);

void emitIns_R_C(instruction ins, emitAttr attr, regNumber reg, CORINFO_FIELD_HANDLE fldHnd, int offs);

void emitIns_C_R(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, regNumber reg, int offs);

void emitIns_C_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, int offs, ssize_t val);

void emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_R_D(instruction ins, emitAttr attr, unsigned offs, regNumber reg);

void emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_I_AR(instruction ins, emitAttr attr, int val, regNumber reg, int offs);

void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_AI(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp);

void emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_ARR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_ARR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, unsigned mul, int disp);

enum EmitCallType
{

    // I have included here, but commented out, all the values used by the x86 emitter.
    // However, ARM has a much reduced instruction set, and so the ARM emitter only
    // supports a subset of the x86 variants.  By leaving them commented out, it becomes
    // a compile time error if code tries to use them (and hopefully see this comment
    // and know why they are unavailible on ARM), while making it easier to stay
    // in-sync with x86 and possibly add them back in if needed.

    EC_FUNC_TOKEN, //   Direct call to a helper/static/nonvirtual/global method
                   //  EC_FUNC_TOKEN_INDIR,    // Indirect call to a helper/static/nonvirtual/global method
    EC_FUNC_ADDR,  // Direct call to an absolute address

    //  EC_FUNC_VIRTUAL,        // Call to a virtual method (using the vtable)
    EC_INDIR_R, // Indirect call via register
                //  EC_INDIR_SR,            // Indirect call via stack-reference (local var)
                //  EC_INDIR_C,             // Indirect call via static class var
                //  EC_INDIR_ARD,           // Indirect call via an addressing mode

    EC_COUNT
};

void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,                   // used for pretty printing
                  INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                  void*            addr,
                  int              argSize,
                  emitAttr         retSize,
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  IL_OFFSETX       ilOffset = BAD_IL_OFFSET,
                  regNumber        ireg     = REG_NA,
                  regNumber        xreg     = REG_NA,
                  unsigned         xmul     = 0,
                  ssize_t          disp     = 0,
                  bool             isJump   = false);

/*****************************************************************************
 *
 *  Given an instrDesc, return true if it's a conditional jump.
 */

inline bool emitIsCondJump(instrDesc* jmp)
{
    return (jmp->idInsFmt() == IF_T2_J1) || (jmp->idInsFmt() == IF_T1_K) || (jmp->idInsFmt() == IF_LARGEJMP);
}

/*****************************************************************************
 *
 *  Given an instrDesc, return true if it's a comapre and jump.
 */

inline bool emitIsCmpJump(instrDesc* jmp)
{
    return (jmp->idInsFmt() == IF_T1_I);
}

/*****************************************************************************
 *
 *  Given a instrDesc, return true if it's an unconditional jump.
 */

inline bool emitIsUncondJump(instrDesc* jmp)
{
    return (jmp->idInsFmt() == IF_T2_J2) || (jmp->idInsFmt() == IF_T1_M);
}

/*****************************************************************************
 *
 *  Given a instrDesc, return true if it's a load label instruction.
 */

inline bool emitIsLoadLabel(instrDesc* jmp)
{
    return (jmp->idInsFmt() == IF_T2_M1) || (jmp->idInsFmt() == IF_T1_J3) || (jmp->idInsFmt() == IF_T2_N1);
}

#endif // TARGET_ARM
