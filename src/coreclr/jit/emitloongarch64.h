// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) Loongson Technology. All rights reserved.

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

const char* emitFPregName(unsigned reg, bool varName = true);
const char* emitVectorRegName(regNumber reg);

//NOTE: At least 32bytes within dst.
void emitDisInsName(code_t code, const BYTE* dst, instrDesc* id);
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
                               emitAttr         retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize));

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize));

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

//NOTEADD: New functions in emitarm64.h
// Method to do check if mov is redundant with respect to the last instruction.
// If yes, the caller of this method can choose to omit current mov instruction.
static bool IsMovInstruction(instruction ins);
bool IsRedundantMov(instruction ins, emitAttr size, regNumber dst, regNumber src, bool canSkip);
bool IsRedundantLdStr(instruction ins, regNumber reg1, regNumber reg2, ssize_t imm, emitAttr size, insFormat fmt);//New functions end.

/************************************************************************
*
* This union is used to to encode/decode the special LOONGARCH64 immediate values
* that is listed as imm(N,r,s) and referred to as 'bitmask immediate'
*/

union bitMaskImm {
    struct
    {
        unsigned immS : 6; // bits 0..5
        unsigned immR : 6; // bits 6..11
        unsigned immN : 1; // bits 12
    };
    unsigned immNRS; // concat N:R:S forming a 13-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a 64-bit immediate and its 'bitmask immediate'
*   representation imm(i16,hw)
*/

//static emitter::bitMaskImm emitEncodeBitMaskImm(INT64 imm, emitAttr size);

//static INT64 emitDecodeBitMaskImm(const emitter::bitMaskImm bmImm, emitAttr size);

/************************************************************************
*
* This union is used to to encode/decode the special LOONGARCH64 immediate values
* that is listed as imm(i16,hw) and referred to as 'halfword immediate'
*/

union halfwordImm {
    struct
    {
        unsigned immVal : 16; // bits  0..15
        unsigned immHW : 2;   // bits 16..17
    };
    unsigned immHWVal; // concat HW:Val forming a 18-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a 64-bit immediate and its 'halfword immediate'
*   representation imm(i16,hw)
*/

//static emitter::halfwordImm emitEncodeHalfwordImm(INT64 imm, emitAttr size);

//static INT64 emitDecodeHalfwordImm(const emitter::halfwordImm hwImm, emitAttr size);

/************************************************************************
*
* This union is used to encode/decode the special LOONGARCH64 immediate values
* that is listed as imm(i16,by) and referred to as 'byteShifted immediate'
*/

union byteShiftedImm {
    struct
    {
        unsigned immVal : 8;  // bits  0..7
        unsigned immBY : 2;   // bits  8..9
        unsigned immOnes : 1; // bit   10
    };
    unsigned immBSVal; // concat Ones:BY:Val forming a 10-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a 16/32-bit immediate and its 'byteShifted immediate'
*   representation imm(i8,by)
*/

//static emitter::byteShiftedImm emitEncodeByteShiftedImm(INT64 imm, emitAttr size, bool allow_MSL);

//static INT32 emitDecodeByteShiftedImm(const emitter::byteShiftedImm bsImm, emitAttr size);

/************************************************************************
*
* This union is used to to encode/decode the special LOONGARCH64 immediate values
* that are use for FMOV immediate and referred to as 'float 8-bit immediate'
*/

union floatImm8 {
    struct
    {
        unsigned immMant : 4; // bits 0..3
        unsigned immExp : 3;  // bits 4..6
        unsigned immSign : 1; // bits 7
    };
    unsigned immFPIVal; // concat Sign:Exp:Mant forming an 8-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a double and its 'float 8-bit immediate' representation
*/

//static emitter::floatImm8 emitEncodeFloatImm8(double immDbl);

//static double emitDecodeFloatImm8(const emitter::floatImm8 fpImm);

/************************************************************************
*
*  This union is used to to encode/decode the cond, nzcv and imm5 values for
*   instructions that use them in the small constant immediate field
*/

union condFlagsImm {
    struct
    {
        //insCond   cond : 4;  // bits  0..3
        //insCflags flags : 4; // bits  4..7
        unsigned  imm5 : 5;  // bits  8..12
    };
    unsigned immCFVal; // concat imm5:flags:cond forming an 13-bit unsigned immediate
};

// Returns true if 'reg' represents an integer register.
static bool isIntegerRegister(regNumber reg)
{
    return (reg >= REG_INT_FIRST) && (reg <= REG_INT_LAST);
}

// Returns true if 'value' is a legal signed immediate 12 bit encoding.
static bool isValidSimm12(ssize_t value)
{
    return -( ((int)1) << 11 ) <= value && value < ( ((int)1) << 11 );
};

// Returns true if 'value' is a legal signed immediate 16 bit encoding.
static bool isValidSimm16(ssize_t value)
{
    return -( ((int)1) << 15 ) <= value && value < ( ((int)1) << 15 );
};

// Returns true if 'value' is a legal signed immediate 20 bit encoding.
static bool isValidSimm20(ssize_t value)
{
    return -( ((int)1) << 19 ) <= value && value < ( ((int)1) << 19 );
};

/************************************************************************/
/*           Public inline informational methods                        */
/************************************************************************/

public:

// Returns the number of bits used by the given 'size'.
inline static unsigned getBitWidth(emitAttr size)
{
    assert(size <= EA_8BYTE);
    return (unsigned)size * BITS_PER_BYTE;
}

inline static bool isGeneralRegister(regNumber reg)
{
    // Excludes REG_R0 ??
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
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);
void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);
void emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs);

void emitIns_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, ssize_t hint, ssize_t off, insOpts opt = INS_OPTS_NONE);

void emitIns_R(instruction ins, emitAttr attr, regNumber reg);

void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt = INS_OPTS_NONE);

//NOTEADD: NEW function in emitarm64.
void emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags)
{
    emitIns_R_R(ins, attr, reg1, reg2);
}

void emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt = INS_OPTS_NONE);

// Checks for a large immediate that needs a second instruction
void emitIns_R_R_Imm(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm);

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

void emitIns_R_R_R_Ext(instruction ins,
                       emitAttr    attr,
                       regNumber   reg1,
                       regNumber   reg2,
                       regNumber   reg3,
                       insOpts     opt         = INS_OPTS_NONE,
                       int         shiftAmount = -1);

//NODECHANGE: ADD an arg.
void emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4);

//void emitIns_BARR(instruction ins, insBarrier barrier);

void emitIns_C(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, int offs);

void emitIns_S(instruction ins, emitAttr attr, int varx, int offs);

void emitIns_S_S_R_R(
    instruction ins, emitAttr attr, emitAttr attr2, regNumber ireg, regNumber ireg2, int varx, int offs);

//void emitIns_R_R_S(
//    instruction ins, emitAttr attr, regNumber ireg, regNumber ireg2, int sa);

void emitIns_R_R_S_S(
    instruction ins, emitAttr attr, emitAttr attr2, regNumber ireg, regNumber ireg2, int varx, int offs);

void emitIns_S_I(instruction ins, emitAttr attr, int varx, int offs, int val);

void emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber tmpReg, CORINFO_FIELD_HANDLE fldHnd, int offs);

void emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_C_R(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, regNumber reg, int offs);

void emitIns_C_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, ssize_t offs, ssize_t val);

void emitIns_R_D(instruction ins, emitAttr attr, unsigned offs, regNumber reg);

void emitIns_J_R_I(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg, int instrCount);

void emitIns_I_AR(instruction ins, emitAttr attr, int val, regNumber reg, int offs);

void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

//NODECHANGE: ADD a description of arguments "disp"
void emitIns_R_AI(instruction ins,
                  emitAttr    attr,
                  regNumber   reg,
                  ssize_t disp DEBUGARG(size_t targetHandle = 0) DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));


void emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_ARR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_ARR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, unsigned mul, int disp);

enum EmitCallType
{

    // I have included here, but commented out, all the values used by the x86 emitter.
    // However, LOONGARCH has a much reduced instruction set, and so the LOONGARCH emitter only
    // supports a subset of the x86 variants.  By leaving them commented out, it becomes
    // a compile time error if code tries to use them (and hopefully see this comment
    // and know why they are unavailible on LOONGARCH), while making it easier to stay
    // in-sync with x86 and possibly add them back in if needed.

    EC_FUNC_TOKEN, //   Direct call to a helper/static/nonvirtual/global method
                   //  EC_FUNC_TOKEN_INDIR,    // Indirect call to a helper/static/nonvirtual/global method
    //EC_FUNC_ADDR,  // Direct call to an absolute address

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
                  void*            addr,
                  ssize_t          argSize,
                  emitAttr         retSize
                  MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  const DebugInfo& di,
                  regNumber        ireg          = REG_NA,
                  regNumber        xreg          = REG_NA,
                  unsigned         xmul          = 0,
                  ssize_t          disp          = 0,
                  bool             isJump        = false);

unsigned emitOutputCall(insGroup* ig, BYTE* dst, instrDesc* id, code_t code);
//BYTE* emitOutputLJ(insGroup* ig, BYTE* dst, instrDesc* i);
//BYTE* emitOutputLoadLabel(BYTE* dst, BYTE* srcAddr, BYTE* dstAddr, instrDescJmp* id);
//BYTE* emitOutputShortBranch(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, instrDescJmp* id);
//BYTE* emitOutputShortAddress(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, regNumber reg);
//BYTE* emitOutputShortConstant(
//    BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, regNumber reg, emitAttr opSize);

unsigned  get_curTotalCodeSize(); // bytes of code

#endif // TARGET_LOONGARCH64
