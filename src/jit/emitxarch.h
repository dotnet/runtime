// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if defined(_TARGET_XARCH_)

/************************************************************************/
/*           Public inline informational methods                        */
/************************************************************************/

public:
inline static bool isGeneralRegister(regNumber reg)
{
    return (reg <= REG_INT_LAST);
}

inline static bool isFloatReg(regNumber reg)
{
    return (reg >= REG_FP_FIRST && reg <= REG_FP_LAST);
}

inline static bool isDoubleReg(regNumber reg)
{
    return isFloatReg(reg);
}

/************************************************************************/
/*         Routines that compute the size of / encode instructions      */
/************************************************************************/

struct CnsVal
{
    ssize_t cnsVal;
#ifdef RELOC_SUPPORT
    bool cnsReloc;
#endif
};

UNATIVE_OFFSET emitInsSize(size_t code);
UNATIVE_OFFSET emitInsSizeRM(instruction ins);
UNATIVE_OFFSET emitInsSizeSV(size_t code, int var, int dsp);
UNATIVE_OFFSET emitInsSizeSV(instrDesc* id, int var, int dsp, int val);
UNATIVE_OFFSET emitInsSizeRR(instruction ins, regNumber reg1, regNumber reg2, emitAttr attr);
UNATIVE_OFFSET emitInsSizeAM(instrDesc* id, size_t code);
UNATIVE_OFFSET emitInsSizeAM(instrDesc* id, size_t code, int val);
UNATIVE_OFFSET emitInsSizeCV(instrDesc* id, size_t code);
UNATIVE_OFFSET emitInsSizeCV(instrDesc* id, size_t code, int val);

BYTE* emitOutputAM(BYTE* dst, instrDesc* id, size_t code, CnsVal* addc = nullptr);
BYTE* emitOutputSV(BYTE* dst, instrDesc* id, size_t code, CnsVal* addc = nullptr);
BYTE* emitOutputCV(BYTE* dst, instrDesc* id, size_t code, CnsVal* addc = nullptr);

BYTE* emitOutputR(BYTE* dst, instrDesc* id);
BYTE* emitOutputRI(BYTE* dst, instrDesc* id);
BYTE* emitOutputRR(BYTE* dst, instrDesc* id);
BYTE* emitOutputIV(BYTE* dst, instrDesc* id);

#ifdef FEATURE_AVX_SUPPORT
BYTE* emitOutputRRR(BYTE* dst, instrDesc* id);
#endif

BYTE* emitOutputLJ(BYTE* dst, instrDesc* id);

unsigned emitOutputRexOrVexPrefixIfNeeded(instruction ins, BYTE* dst, size_t& code);
unsigned emitGetRexPrefixSize(instruction ins);
unsigned emitGetVexPrefixSize(instruction ins, emitAttr attr);
unsigned emitGetPrefixSize(size_t code);
unsigned emitGetVexPrefixAdjustedSize(instruction ins, emitAttr attr, size_t code);

unsigned insEncodeReg345(instruction ins, regNumber reg, emitAttr size, size_t* code);
unsigned insEncodeReg012(instruction ins, regNumber reg, emitAttr size, size_t* code);
size_t insEncodeReg3456(instruction ins, regNumber reg, emitAttr size, size_t code);
unsigned insEncodeRegSIB(instruction ins, regNumber reg, size_t* code);

size_t insEncodeMRreg(instruction ins, size_t code);
size_t insEncodeMRreg(instruction ins, regNumber reg, emitAttr size, size_t code);
size_t insEncodeRRIb(instruction ins, regNumber reg, emitAttr size);
size_t insEncodeOpreg(instruction ins, regNumber reg, emitAttr size);

bool IsAVXInstruction(instruction ins);
size_t insEncodeMIreg(instruction ins, regNumber reg, emitAttr size, size_t code);

size_t AddRexWPrefix(instruction ins, size_t code);
size_t AddRexRPrefix(instruction ins, size_t code);
size_t AddRexXPrefix(instruction ins, size_t code);
size_t AddRexBPrefix(instruction ins, size_t code);
size_t AddRexPrefix(instruction ins, size_t code);

#ifdef FEATURE_AVX_SUPPORT
// 3-byte VEX prefix starts with byte 0xC4
#define VEX_PREFIX_MASK_3BYTE 0xC4000000000000LL
bool TakesVexPrefix(instruction ins);
// Returns true if the instruction encoding already contains VEX prefix
bool hasVexPrefix(size_t code)
{
    return (code & VEX_PREFIX_MASK_3BYTE) != 0;
}
size_t AddVexPrefix(instruction ins, size_t code, emitAttr attr);
size_t AddVexPrefixIfNeeded(instruction ins, size_t code, emitAttr size)
{
    if (TakesVexPrefix(ins))
    {
        code = AddVexPrefix(ins, code, size);
    }
    return code;
}
size_t AddVexPrefixIfNeededAndNotPresent(instruction ins, size_t code, emitAttr size)
{
    if (TakesVexPrefix(ins) && !hasVexPrefix(code))
    {
        code = AddVexPrefix(ins, code, size);
    }
    return code;
}
bool useAVXEncodings;
bool UseAVX()
{
    return useAVXEncodings;
}
void SetUseAVX(bool value)
{
    useAVXEncodings = value;
}
bool IsThreeOperandBinaryAVXInstruction(instruction ins);
bool IsThreeOperandMoveAVXInstruction(instruction ins);
bool IsThreeOperandAVXInstruction(instruction ins)
{
    return (IsThreeOperandBinaryAVXInstruction(ins) || IsThreeOperandMoveAVXInstruction(ins));
}
#else  // !FEATURE_AVX_SUPPORT
bool UseAVX()
{
    return false;
}
bool hasVexPrefix(size_t code)
{
    return false;
}
bool IsThreeOperandBinaryAVXInstruction(instruction ins)
{
    return false;
}
bool IsThreeOperandMoveAVXInstruction(instruction ins)
{
    return false;
}
bool IsThreeOperandAVXInstruction(instruction ins)
{
    return false;
}
bool TakesVexPrefix(instruction ins)
{
    return false;
}
size_t AddVexPrefixIfNeeded(instruction ins, size_t code, emitAttr attr)
{
    return code;
}
size_t AddVexPrefixIfNeededAndNotPresent(instruction ins, size_t code, emitAttr size)
{
    return code;
}
#endif // !FEATURE_AVX_SUPPORT

/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/

#ifdef DEBUG

const char* emitFPregName(unsigned reg, bool varName = true);

void emitDispReloc(ssize_t value);
void emitDispAddrMode(instrDesc* id, bool noDetail = false);
void emitDispShift(instruction ins, int cnt = 0);

void emitDispIns(instrDesc* id,
                 bool       isNew,
                 bool       doffs,
                 bool       asmfm,
                 unsigned   offs = 0,
                 BYTE*      code = nullptr,
                 size_t     sz   = 0,
                 insGroup*  ig   = nullptr);

const char* emitXMMregName(unsigned reg);
const char* emitYMMregName(unsigned reg);

#endif

/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
void emitSetAmdDisp(instrDescAmd* id, ssize_t dsp);
instrDesc* emitNewInstrAmd(emitAttr attr, ssize_t dsp);
instrDesc* emitNewInstrAmdCns(emitAttr attr, ssize_t dsp, int cns);

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

void emitGetInsCns(instrDesc* id, CnsVal* cv);
ssize_t emitGetInsAmdCns(instrDesc* id, CnsVal* cv);
void emitGetInsDcmCns(instrDesc* id, CnsVal* cv);
ssize_t emitGetInsAmdAny(instrDesc* id);

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
insFormat emitInsModeFormat(instruction ins, insFormat base, insFormat FPld, insFormat FPst);

bool emitVerifyEncodable(instruction ins, emitAttr size, regNumber reg1, regNumber reg2 = REG_NA);

bool emitInsCanOnlyWriteSSE2OrAVXReg(instrDesc* id);

/*****************************************************************************
*
*  Convert between an index scale in bytes to a smaller encoding used for
*  storage in instruction descriptors.
*/

inline emitter::opSize emitEncodeScale(size_t scale)
{
    assert(scale == 1 || scale == 2 || scale == 4 || scale == 8);

    return emitSizeEncode[scale - 1];
}

inline emitAttr emitDecodeScale(unsigned ensz)
{
    assert(ensz < 4);

    return emitter::emitSizeDecode[ensz];
}

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitLoopAlign();

void emitIns(instruction ins);

void emitIns(instruction ins, emitAttr attr);

void emitInsRMW(instruction inst, emitAttr attr, GenTreeStoreInd* storeInd, GenTreePtr src);

void emitInsRMW(instruction inst, emitAttr attr, GenTreeStoreInd* storeInd);

void emitIns_Nop(unsigned size);

void emitIns_I(instruction ins, emitAttr attr, int val);

void emitIns_R(instruction ins, emitAttr attr, regNumber reg);

void emitIns_C(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, int offs);

void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t val);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2);

void emitIns_R_R_I(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int ival);

#ifdef FEATURE_AVX_SUPPORT
void emitIns_R_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3);
#endif

void emitIns_S(instruction ins, emitAttr attr, int varx, int offs);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_S_I(instruction ins, emitAttr attr, int varx, int offs, int val);

void emitIns_R_C(instruction ins, emitAttr attr, regNumber reg, CORINFO_FIELD_HANDLE fldHnd, int offs);

void emitIns_C_R(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, regNumber reg, int offs);

void emitIns_C_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, int offs, int val);

void emitIns_IJ(emitAttr attr, regNumber reg, unsigned base);

void emitIns_J_S(instruction ins, emitAttr attr, BasicBlock* dst, int varx, int offs);

void emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_R_D(instruction ins, emitAttr attr, unsigned offs, regNumber reg);

void emitIns_I_AR(
    instruction ins, emitAttr attr, int val, regNumber reg, int offs, int memCookie = 0, void* clsCookie = nullptr);

void emitIns_I_AI(instruction ins, emitAttr attr, int val, ssize_t disp);

void emitIns_R_AR(instruction ins,
                  emitAttr    attr,
                  regNumber   ireg,
                  regNumber   reg,
                  int         offs,
                  int         memCookie = 0,
                  void*       clsCookie = nullptr);

void emitIns_R_AI(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp);

void emitIns_AR_R(instruction ins,
                  emitAttr    attr,
                  regNumber   ireg,
                  regNumber   reg,
                  int         offs,
                  int         memCookie = 0,
                  void*       clsCookie = nullptr);

void emitIns_AI_R(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp);

void emitIns_I_ARR(instruction ins, emitAttr attr, int val, regNumber reg, regNumber rg2, int disp);

void emitIns_R_ARR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_ARR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_I_ARX(instruction ins, emitAttr attr, int val, regNumber reg, regNumber rg2, unsigned mul, int disp);

void emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, unsigned mul, int disp);

void emitIns_ARX_R(
    instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, unsigned mul, int disp);

void emitIns_I_AX(instruction ins, emitAttr attr, int val, regNumber reg, unsigned mul, int disp);

void emitIns_R_AX(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, unsigned mul, int disp);

void emitIns_AX_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, unsigned mul, int disp);

#if FEATURE_STACK_FP_X87
void emitIns_F_F0(instruction ins, unsigned fpreg);

void emitIns_F0_F(instruction ins, unsigned fpreg);
#endif // FEATURE_STACK_FP_X87

enum EmitCallType
{
    EC_FUNC_TOKEN,       //   Direct call to a helper/static/nonvirtual/global method
    EC_FUNC_TOKEN_INDIR, // Indirect call to a helper/static/nonvirtual/global method
    EC_FUNC_ADDR,        // Direct call to an absolute address

    EC_FUNC_VIRTUAL, // Call to a virtual method (using the vtable)
    EC_INDIR_R,      // Indirect call via register
    EC_INDIR_SR,     // Indirect call via stack-reference (local var)
    EC_INDIR_C,      // Indirect call via static class var
    EC_INDIR_ARD,    // Indirect call via an addressing mode

    EC_COUNT
};

void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,
                  CORINFO_SIG_INFO*     sigInfo, // used to report call sites to the EE
                  void*                 addr,
                  ssize_t               argSize,
                  emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  GenTreeIndir*    indir,
                  bool             isJump = false,
                  bool             isNoGC = false);

void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,
                  INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                  void*    addr,
                  ssize_t  argSize,
                  emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  IL_OFFSETX       ilOffset = BAD_IL_OFFSET,
                  regNumber        ireg     = REG_NA,
                  regNumber        xreg     = REG_NA,
                  unsigned         xmul     = 0,
                  ssize_t          disp     = 0,
                  bool             isJump   = false,
                  bool             isNoGC   = false);

#ifdef _TARGET_AMD64_
// Is the last instruction emitted a call instruction?
bool emitIsLastInsCall();

// Insert a NOP at the end of the the current instruction group if the last emitted instruction was a 'call',
// because the next instruction group will be an epilog.
void emitOutputPreEpilogNOP();
#endif // _TARGET_AMD64_

/*****************************************************************************
 *
 *  Given a jump, return true if it's a conditional jump.
 */

inline bool emitIsCondJump(instrDesc* jmp)
{
    instruction ins = jmp->idIns();

    assert(jmp->idInsFmt() == IF_LABEL);

    return (ins != INS_call && ins != INS_jmp);
}

/*****************************************************************************
 *
 *  Given a jump, return true if it's an unconditional jump.
 */

inline bool emitIsUncondJump(instrDesc* jmp)
{
    instruction ins = jmp->idIns();

    assert(jmp->idInsFmt() == IF_LABEL);

    return (ins == INS_jmp);
}

#endif // _TARGET_XARCH_
