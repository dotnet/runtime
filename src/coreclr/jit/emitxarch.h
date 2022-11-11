// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_XARCH)

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

// code_t is a type used to accumulate bits of opcode + prefixes. On amd64, it must be 64 bits
// to support the REX prefixes. On both x86 and amd64, it must be 64 bits to support AVX, with
// its 3-byte VEX prefix.
typedef unsigned __int64 code_t;

struct CnsVal
{
    ssize_t cnsVal;
    bool    cnsReloc;
};

UNATIVE_OFFSET emitInsSize(code_t code, bool includeRexPrefixSize);
UNATIVE_OFFSET emitInsSizeSVCalcDisp(instrDesc* id, code_t code, int var, int dsp);
UNATIVE_OFFSET emitInsSizeSV(instrDesc* id, code_t code, int var, int dsp);
UNATIVE_OFFSET emitInsSizeSV(instrDesc* id, code_t code, int var, int dsp, int val);
UNATIVE_OFFSET emitInsSizeRR(instrDesc* id, code_t code);
UNATIVE_OFFSET emitInsSizeRR(instrDesc* id, code_t code, int val);
UNATIVE_OFFSET emitInsSizeRR(instruction ins, regNumber reg1, regNumber reg2, emitAttr attr);
UNATIVE_OFFSET emitInsSizeAM(instrDesc* id, code_t code);
UNATIVE_OFFSET emitInsSizeAM(instrDesc* id, code_t code, int val);
UNATIVE_OFFSET emitInsSizeCV(instrDesc* id, code_t code);
UNATIVE_OFFSET emitInsSizeCV(instrDesc* id, code_t code, int val);

BYTE* emitOutputNOP(BYTE* dst, size_t nBytes);
BYTE* emitOutputAlign(insGroup* ig, instrDesc* id, BYTE* dst);
BYTE* emitOutputAM(BYTE* dst, instrDesc* id, code_t code, CnsVal* addc = nullptr);
BYTE* emitOutputSV(BYTE* dst, instrDesc* id, code_t code, CnsVal* addc = nullptr);
BYTE* emitOutputCV(BYTE* dst, instrDesc* id, code_t code, CnsVal* addc = nullptr);

BYTE* emitOutputR(BYTE* dst, instrDesc* id);
BYTE* emitOutputRI(BYTE* dst, instrDesc* id);
BYTE* emitOutputRR(BYTE* dst, instrDesc* id);
BYTE* emitOutputIV(BYTE* dst, instrDesc* id);

BYTE* emitOutputRRR(BYTE* dst, instrDesc* id);

BYTE* emitOutputLJ(insGroup* ig, BYTE* dst, instrDesc* id);

unsigned emitOutputSimdPrefixIfNeeded(instruction ins, BYTE* dst, code_t& code);
unsigned emitOutputRexOrVexPrefixIfNeeded(instruction ins, BYTE* dst, code_t& code);
unsigned emitGetRexPrefixSize(instruction ins);
unsigned emitGetVexPrefixSize(instruction ins, emitAttr attr);
unsigned emitGetEvexPrefixSize(instruction ins);
unsigned emitGetPrefixSize(code_t code, bool includeRexPrefixSize);
unsigned emitGetAdjustedSize(instruction ins, emitAttr attr, code_t code);
unsigned emitGetAdjustedSizeEvexAware(instruction ins, emitAttr attr, code_t code);

unsigned insEncodeReg012(instruction ins, regNumber reg, emitAttr size, code_t* code);
unsigned insEncodeReg345(instruction ins, regNumber reg, emitAttr size, code_t* code);
code_t insEncodeReg3456(instruction ins, regNumber reg, emitAttr size, code_t code);
unsigned insEncodeRegSIB(instruction ins, regNumber reg, code_t* code);

code_t insEncodeMRreg(instruction ins, code_t code);
code_t insEncodeRMreg(instruction ins, code_t code);
code_t insEncodeMRreg(instruction ins, regNumber reg, emitAttr size, code_t code);
code_t insEncodeRRIb(instruction ins, regNumber reg, emitAttr size);
code_t insEncodeOpreg(instruction ins, regNumber reg, emitAttr size);

unsigned insSSval(unsigned scale);

static bool IsSSEInstruction(instruction ins);
static bool IsSSEOrAVXInstruction(instruction ins);
static bool IsAvx512OrPriorInstruction(instruction ins);
static bool IsAVXOnlyInstruction(instruction ins);
static bool IsAvx512OnlyInstruction(instruction ins);
static bool IsFMAInstruction(instruction ins);
static bool IsAVXVNNIInstruction(instruction ins);
static bool IsBMIInstruction(instruction ins);

static regNumber getBmiRegNumber(instruction ins);
static regNumber getSseShiftRegNumber(instruction ins);
bool IsVexEncodedInstruction(instruction ins) const;
bool IsEvexEncodedInstruction(instruction ins) const;
bool IsVexOrEvexEncodedInstruction(instruction ins) const;

code_t insEncodeMIreg(instruction ins, regNumber reg, emitAttr size, code_t code);

code_t AddRexWPrefix(instruction ins, code_t code);
code_t AddRexRPrefix(instruction ins, code_t code);
code_t AddRexXPrefix(instruction ins, code_t code);
code_t AddRexBPrefix(instruction ins, code_t code);
code_t AddRexPrefix(instruction ins, code_t code);

bool EncodedBySSE38orSSE3A(instruction ins);
bool Is4ByteSSEInstruction(instruction ins);
static bool IsMovInstruction(instruction ins);
bool HasSideEffect(instruction ins, emitAttr size);
bool IsRedundantMov(
    instruction ins, insFormat fmt, emitAttr size, regNumber dst, regNumber src, bool canIgnoreSideEffects);
bool EmitMovsxAsCwde(instruction ins, emitAttr size, regNumber dst, regNumber src);

bool IsRedundantStackMov(instruction ins, insFormat fmt, emitAttr size, regNumber ireg, int varx, int offs);

static bool IsJccInstruction(instruction ins);
static bool IsJmpInstruction(instruction ins);

bool AreUpper32BitsZero(regNumber reg);

bool AreFlagsSetToZeroCmp(regNumber reg, emitAttr opSize, genTreeOps treeOps);
bool AreFlagsSetForSignJumpOpt(regNumber reg, emitAttr opSize, GenTree* tree);

bool hasRexPrefix(code_t code)
{
#ifdef TARGET_AMD64
    const code_t REX_PREFIX_MASK = 0xFF00000000LL;
    return (code & REX_PREFIX_MASK) != 0;
#else  // !TARGET_AMD64
    return false;
#endif // !TARGET_AMD64
}

// 3-byte VEX prefix starts with byte 0xC4
#define VEX_PREFIX_MASK_3BYTE 0xFF000000000000ULL
#define VEX_PREFIX_CODE_3BYTE 0xC4000000000000ULL

bool TakesVexPrefix(instruction ins) const;
static bool TakesRexWPrefix(instruction ins, emitAttr attr);

// Returns true if the instruction encoding already contains VEX prefix
bool hasVexPrefix(code_t code)
{
    return (code & VEX_PREFIX_MASK_3BYTE) == VEX_PREFIX_CODE_3BYTE;
}
code_t AddVexPrefix(instruction ins, code_t code, emitAttr attr);
code_t AddVexPrefixIfNeeded(instruction ins, code_t code, emitAttr size)
{
    if (TakesVexPrefix(ins))
    {
        code = AddVexPrefix(ins, code, size);
    }
    return code;
}
code_t AddVexPrefixIfNeededAndNotPresent(instruction ins, code_t code, emitAttr size)
{
    if (TakesVexPrefix(ins) && !hasVexPrefix(code))
    {
        code = AddVexPrefix(ins, code, size);
    }
    return code;
}

//------------------------------------------------------------------------
// IsWEvexOpcodeExtension: Some instructions use W bit as an opcode extension bit.
// Identify instructions which requires W bit to be set to 1
// for Evex encoding.
// TODO-XArch-AVX512: Explore adding this as a flag to instr table.
//
// Arguments:
//    ins - The instruction to check.
//
// Returns:
//    `true` if W bit needs to be set to 1.
//
bool IsWEvexOpcodeExtension(instruction ins)
{
    if (!TakesEvexPrefix(ins))
    {
        return false;
    }

    switch (ins)
    {
        case INS_movq:
        case INS_addpd:
        case INS_addsd:
        case INS_movsd:
        case INS_movsdsse2:
        case INS_mulsd:
        case INS_mulpd:
        case INS_movntpd:
        case INS_movlpd:
        case INS_movhpd:
        case INS_movapd:
        case INS_movupd:
        case INS_shufpd:
        case INS_subsd:
        case INS_subpd:
        case INS_minsd:
        case INS_minpd:
        case INS_divsd:
        case INS_divpd:
        case INS_maxsd:
        case INS_maxpd:
        case INS_xorpd:
        case INS_andpd:
        case INS_sqrtsd:
        case INS_sqrtpd:
        case INS_andnpd:
        case INS_orpd:
        case INS_cvtpd2ps:
        case INS_cvtsd2ss:
        case INS_cvtpd2dq:
        case INS_cvttpd2dq:
        case INS_comisd:
        case INS_ucomisd:
        case INS_paddq:
        case INS_psubq:
        case INS_pmuludq:
        case INS_psllq:
        case INS_psrlq:
        case INS_punpckhqdq:
        case INS_punpcklqdq:
        case INS_unpckhpd:
        case INS_pmuldq:
        case INS_movddup:
        case INS_pinsrq:
        case INS_pextrq:
        case INS_vpbroadcastq:
        case INS_vpermq:
        case INS_vpsrlvq:
        case INS_vpsllvq:
        case INS_vpermilpd:
        case INS_vpermpd:
        case INS_vpgatherdq:
        case INS_vpgatherqq:
        case INS_vgatherdpd:
        case INS_vgatherqpd:
        case INS_vfmadd132pd:
        case INS_vfmadd213pd:
        case INS_vfmadd231pd:
        case INS_vfmadd132sd:
        case INS_vfmadd213sd:
        case INS_vfmadd231sd:
        case INS_vfmaddsub132pd:
        case INS_vfmaddsub213pd:
        case INS_vfmaddsub231pd:
        case INS_vfmsubadd132pd:
        case INS_vfmsubadd213pd:
        case INS_vfmsubadd231pd:
        case INS_vfmsub132pd:
        case INS_vfmsub213pd:
        case INS_vfmsub231pd:
        case INS_vfmsub132sd:
        case INS_vfmsub213sd:
        case INS_vfmsub231sd:
        case INS_vfnmadd132pd:
        case INS_vfnmadd213pd:
        case INS_vfnmadd231pd:
        case INS_vfnmadd132sd:
        case INS_vfnmadd213sd:
        case INS_vfnmadd231sd:
        case INS_vfnmsub132pd:
        case INS_vfnmsub213pd:
        case INS_vfnmsub231pd:
        case INS_vfnmsub132sd:
        case INS_vfnmsub213sd:
        case INS_vfnmsub231sd:
        case INS_unpcklpd:
        case INS_vpermilpdvar:
        {
            return true; // W1
        }
        case INS_movd:
        case INS_punpckldq:
        case INS_movntdq:
        case INS_movntps:
        case INS_movlps:
        case INS_movhps:
        case INS_movss:
        case INS_movaps:
        case INS_movups:
        case INS_movhlps:
        case INS_movlhps:
        case INS_unpckhps:
        case INS_unpcklps:
        case INS_shufps:
        case INS_punpckhdq:
        case INS_addps:
        case INS_addss:
        case INS_mulss:
        case INS_mulps:
        case INS_subss:
        case INS_subps:
        case INS_minss:
        case INS_minps:
        case INS_divss:
        case INS_divps:
        case INS_maxss:
        case INS_maxps:
        case INS_xorps:
        case INS_andps:
        case INS_sqrtss:
        case INS_sqrtps:
        case INS_andnps:
        case INS_orps:
        case INS_cvtss2sd:
        case INS_cvtdq2ps:
        case INS_cvtps2dq:
        case INS_cvttps2dq:
        case INS_cvtdq2pd:
        case INS_comiss:
        case INS_ucomiss:
        case INS_paddd:
        case INS_psubd:
        case INS_pslld:
        case INS_psrld:
        case INS_psrad:
        case INS_pshufd:
        case INS_packssdw:
        case INS_insertps:
        case INS_pmulld:
        case INS_pabsd:
        case INS_pminsd:
        case INS_pminud:
        case INS_pmaxud:
        case INS_pmovsxdq:
        case INS_pmovzxdq:
        case INS_packusdw:
        case INS_movntdqa:
        case INS_movsldup:
        case INS_movshdup:
        case INS_pinsrd:
        case INS_pextrd:
        case INS_vbroadcastss:
        case INS_vbroadcastsd:
        case INS_vpbroadcastb:
        case INS_vpbroadcastw:
        case INS_vpbroadcastd:
        case INS_vpsravd:
        case INS_vpsllvd:
        case INS_vpermilps:
        case INS_vpermd:
        case INS_vpermps:
        case INS_vpgatherdd:
        case INS_vpgatherqd:
        case INS_vgatherdps:
        case INS_vgatherqps:
        case INS_vfmadd132ps:
        case INS_vfmadd213ps:
        case INS_vfmadd231ps:
        case INS_vfmadd132ss:
        case INS_vfmadd213ss:
        case INS_vfmadd231ss:
        case INS_vfmaddsub132ps:
        case INS_vfmaddsub213ps:
        case INS_vfmaddsub231ps:
        case INS_vfmsubadd132ps:
        case INS_vfmsubadd213ps:
        case INS_vfmsubadd231ps:
        case INS_vfmsub132ps:
        case INS_vfmsub213ps:
        case INS_vfmsub231ps:
        case INS_vfmsub132ss:
        case INS_vfmsub213ss:
        case INS_vfmsub231ss:
        case INS_vfnmadd132ps:
        case INS_vfnmadd213ps:
        case INS_vfnmadd231ps:
        case INS_vfnmadd132ss:
        case INS_vfnmadd213ss:
        case INS_vfnmadd231ss:
        case INS_vfnmsub132ps:
        case INS_vfnmsub213ps:
        case INS_vfnmsub231ps:
        case INS_vfnmsub132ss:
        case INS_vfnmsub213ss:
        case INS_vfnmsub231ss:
        case INS_vpdpbusd:
        case INS_vpdpwssd:
        case INS_vpdpbusds:
        case INS_vpdpwssds:
        case INS_vpermilpsvar:
        {
            return false; // W0
        }
        default:
        {
            return false; // WIG
        }
    }
}

//------------------------------------------------------------------------
// HasKMaskRegisterDest: Temporary check to identify instructions that can
// be Evex encoded but require Opmask(KMask) register support.
// These are cases where for comparison instructions, result is written
//  to KMask when Evex encoded.
// TODO-XArch-AVX512: Refactor once KMask is added.
//
// Arguments:
//    ins - The instruction to check.
//
// Returns:
//    `true` if Evex encoding requires KMAsk support.
//
bool HasKMaskRegisterDest(instruction ins) const
{
    assert(UseEvexEncoding() == true);
    switch (ins)
    {
        // Requires KMask.
        case INS_pcmpgtb:
        case INS_pcmpgtd:
        case INS_pcmpgtw:
        case INS_pcmpgtq:
        case INS_pcmpeqb:
        case INS_pcmpeqd:
        case INS_pcmpeqq:
        case INS_pcmpeqw:
        case INS_cmpps:
        case INS_cmpss:
        case INS_cmppd:
        case INS_cmpsd:
        case INS_vpgatherdd:
        case INS_vpgatherqd:
        case INS_vpgatherdq:
        case INS_vpgatherqq:
        case INS_vgatherdps:
        case INS_vgatherqps:
        case INS_vgatherdpd:
        case INS_vgatherqpd:
        {
            return true;
        }
        default:
        {
            return false;
        }
    }
}

bool useVEXEncodings;
bool UseVEXEncoding() const
{
    return useVEXEncodings;
}
void SetUseVEXEncoding(bool value)
{
    useVEXEncodings = value;
}

// Is Evex encoding supported.
bool useEvexEncodings;
bool UseEvexEncoding() const
{
    return useEvexEncodings;
}
void SetUseEvexEncoding(bool value)
{
    useEvexEncodings = value;
}

//------------------------------------------------------------------------
// UseSimdEncoding: Returns true if either VEX or EVEX encoding is supported
// contains Evex prefix.
//
// Returns:
//    `true` if target supports either.
//
bool UseSimdEncoding() const
{
    return UseVEXEncoding() || UseEvexEncoding();
}

// 4-byte EVEX prefix starts with byte 0x62
#define EVEX_PREFIX_MASK 0xFF00000000000000ULL
#define EVEX_PREFIX_CODE 0x6200000000000000ULL

bool TakesEvexPrefix(instruction ins) const;

//------------------------------------------------------------------------
// hasEvexPrefix: Returns true if the instruction encoding already
// contains Evex prefix.
//
// Arguments:
//    code - opcode + prefixes bits at some stage of encoding.
//
// Returns:
//    `true` if code has an Evex prefix.
//
bool hasEvexPrefix(code_t code)
{
    return (code & EVEX_PREFIX_MASK) == EVEX_PREFIX_CODE;
}
code_t AddEvexPrefix(instruction ins, code_t code, emitAttr attr);

//------------------------------------------------------------------------
// AddSimdPrefixIfNeeded: Add the correct SIMD prefix if required.
//
// Arguments:
//    ins - the instruction being encoded.
//    code - opcode + prefixes bits at some stage of encoding.
//    size - operand size
//
// Returns:
//    code with prefix added.
code_t AddSimdPrefixIfNeeded(instruction ins, code_t code, emitAttr size)
{
    if (TakesEvexPrefix(ins))
    {
        code = AddEvexPrefix(ins, code, size);
    }
    else if (TakesVexPrefix(ins))
    {
        code = AddVexPrefix(ins, code, size);
    }
    return code;
}

//------------------------------------------------------------------------
// AddSimdPrefixIfNeeded: Add the correct SIMD prefix.
// Check if the prefix already exists befpre adding.
//
// Arguments:
//    ins - the instruction being encoded.
//    code - opcode + prefixes bits at some stage of encoding.
//    size - operand size
//
// Returns:
//    `true` if code has an Evex prefix.
//
code_t AddSimdPrefixIfNeededAndNotPresent(instruction ins, code_t code, emitAttr size)
{
    if (TakesEvexPrefix(ins))
    {
        code = !hasEvexPrefix(code) ? AddEvexPrefix(ins, code, size) : code;
    }
    else if (TakesVexPrefix(ins))
    {
        code = !hasVexPrefix(code) ? AddVexPrefix(ins, code, size) : code;
    }
    return code;
}

bool TakesSimdPrefix(instruction ins) const;

//------------------------------------------------------------------------
// hasVexOrEvexPrefix: Returns true if the instruction encoding already
// contains a Vex or Evex prefix.
//
// Arguments:
//    code - opcode + prefixes bits at some stage of encoding.
//
// Returns:
//    `true` if code has a SIMD prefix.
//
bool hasVexOrEvexPrefix(code_t code)
{
    return (hasVexPrefix(code) || hasEvexPrefix(code));
}

ssize_t TryEvexCompressDisp8Byte(instrDesc* id, ssize_t dsp, bool* dspInByte);

//------------------------------------------------------------------------
// codeEvexMigrationCheck: Temporary check to use when adding EVEX codepaths
// TODO-XArch-AVX512: Remove implementation and uses once all Evex paths are
// completed.
//
// Arguments:
//    code - opcode + prefixes bits at some stage of encoding.
//
// Returns:
//    `true` if code has an Evex prefix.
//
bool codeEvexMigrationCheck(code_t code)
{
    return hasEvexPrefix(code);
}

ssize_t GetInputSizeInBytes(instrDesc* id);

bool containsAVXInstruction = false;
bool ContainsAVX()
{
    return containsAVXInstruction;
}
void SetContainsAVX(bool value)
{
    containsAVXInstruction = value;
}

bool contains256bitAVXInstruction = false;
bool Contains256bitAVX()
{
    return contains256bitAVXInstruction;
}
void SetContains256bitAVX(bool value)
{
    contains256bitAVXInstruction = value;
}

bool IsDstDstSrcAVXInstruction(instruction ins);
bool IsDstSrcSrcAVXInstruction(instruction ins);
bool HasRegularWideForm(instruction ins);
bool HasRegularWideImmediateForm(instruction ins);
static bool DoesWriteZeroFlag(instruction ins);
bool DoesWriteSignFlag(instruction ins);
bool DoesResetOverflowAndCarryFlags(instruction ins);
bool IsFlagsAlwaysModified(instrDesc* id);

bool IsThreeOperandAVXInstruction(instruction ins)
{
    return (IsDstDstSrcAVXInstruction(ins) || IsDstSrcSrcAVXInstruction(ins));
}

bool isAvxBlendv(instruction ins)
{
    return ins == INS_vblendvps || ins == INS_vblendvpd || ins == INS_vpblendvb;
}
bool isSse41Blendv(instruction ins)
{
    return ins == INS_blendvps || ins == INS_blendvpd || ins == INS_pblendvb;
}
bool isPrefetch(instruction ins)
{
    return (ins == INS_prefetcht0) || (ins == INS_prefetcht1) || (ins == INS_prefetcht2) || (ins == INS_prefetchnta);
}

/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/

void emitDispReloc(ssize_t value);
void emitDispAddrMode(instrDesc* id, bool noDetail = false);
void emitDispShift(instruction ins, int cnt = 0);

const char* emitXMMregName(unsigned reg);
const char* emitYMMregName(unsigned reg);

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

#if FEATURE_FIXED_OUT_ARGS
void emitAdjustStackDepthPushPop(instruction ins)
{
}
void emitAdjustStackDepth(instruction ins, ssize_t val)
{
}
#else  // !FEATURE_FIXED_OUT_ARGS
void emitAdjustStackDepthPushPop(instruction ins);
void emitAdjustStackDepth(instruction ins, ssize_t val);
#endif // !FEATURE_FIXED_OUT_ARGS

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
/*                   Output target-independent instructions             */
/************************************************************************/

void emitIns_J(instruction ins, BasicBlock* dst, int instrCount = 0, bool isRemovableJmpCandidate = false);

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);

void emitIns(instruction ins, emitAttr attr);

void emitInsRMW(instruction inst, emitAttr attr, GenTreeStoreInd* storeInd, GenTree* src);

void emitInsRMW(instruction inst, emitAttr attr, GenTreeStoreInd* storeInd);

void emitIns_Nop(unsigned size);

void emitIns_I(instruction ins, emitAttr attr, cnsval_ssize_t val);

void emitIns_R(instruction ins, emitAttr attr, regNumber reg);

void emitIns_C(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, int offs);

void emitIns_A(instruction ins, emitAttr attr, GenTreeIndir* indir);

void emitIns_R_I(instruction ins,
                 emitAttr    attr,
                 regNumber   reg,
                 ssize_t val DEBUGARG(size_t targetHandle = 0) DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

void emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srgReg, bool canSkip);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2);

void emitIns_R_R_I(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int ival);

void emitIns_AR(instruction ins, emitAttr attr, regNumber base, int offs);

void emitIns_AR_R_R(instruction ins, emitAttr attr, regNumber op2Reg, regNumber op3Reg, regNumber base, int offs);

void emitIns_R_A(instruction ins, emitAttr attr, regNumber reg1, GenTreeIndir* indir);

void emitIns_R_A_I(instruction ins, emitAttr attr, regNumber reg1, GenTreeIndir* indir, int ival);

void emitIns_R_AR_I(instruction ins, emitAttr attr, regNumber reg1, regNumber base, int offs, int ival);

void emitIns_R_C_I(instruction ins, emitAttr attr, regNumber reg1, CORINFO_FIELD_HANDLE fldHnd, int offs, int ival);

void emitIns_R_S_I(instruction ins, emitAttr attr, regNumber reg1, int varx, int offs, int ival);

void emitIns_R_R_A(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, GenTreeIndir* indir);

void emitIns_R_R_AR(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber base, int offs);

void emitIns_R_AR_R(instruction ins,
                    emitAttr    attr,
                    regNumber   reg1,
                    regNumber   reg2,
                    regNumber   base,
                    regNumber   index,
                    int         scale,
                    int         offs);

void emitIns_R_R_C(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, CORINFO_FIELD_HANDLE fldHnd, int offs);

void emitIns_R_R_S(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int varx, int offs);

void emitIns_R_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3);

void emitIns_R_R_A_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, GenTreeIndir* indir, int ival, insFormat fmt);
void emitIns_R_R_AR_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber base, int offs, int ival);

void emitIns_C_R_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, int offs, regNumber reg, int ival);
void emitIns_S_R_I(instruction ins, emitAttr attr, int varNum, int offs, regNumber reg, int ival);
void emitIns_A_R_I(instruction ins, emitAttr attr, GenTreeIndir* indir, regNumber reg, int imm);

void emitIns_R_R_C_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, CORINFO_FIELD_HANDLE fldHnd, int offs, int ival);

void emitIns_R_R_R_I(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, int ival);

void emitIns_R_R_S_I(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int varx, int offs, int ival);

void emitIns_R_R_A_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, GenTreeIndir* indir);

void emitIns_R_R_AR_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, regNumber base, int offs);

void emitIns_R_R_C_R(instruction          ins,
                     emitAttr             attr,
                     regNumber            targetReg,
                     regNumber            op1Reg,
                     regNumber            op3Reg,
                     CORINFO_FIELD_HANDLE fldHnd,
                     int                  offs);

void emitIns_R_R_S_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op3Reg, int varx, int offs);

void emitIns_R_R_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4);

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

void emitIns_I_AR(instruction ins, emitAttr attr, int val, regNumber reg, int offs);

void emitIns_I_AI(instruction ins, emitAttr attr, int val, ssize_t disp);

void emitIns_R_AR(instruction ins, emitAttr attr, regNumber reg, regNumber base, int disp);

void emitIns_R_AI(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp);

void emitIns_AR_R(instruction ins, emitAttr attr, regNumber reg, regNumber base, cnsval_ssize_t disp);

void emitIns_AI_R(instruction ins, emitAttr attr, regNumber ireg, ssize_t disp);

void emitIns_I_ARR(instruction ins, emitAttr attr, int val, regNumber reg, regNumber rg2, int disp);

void emitIns_R_ARR(instruction ins, emitAttr attr, regNumber reg, regNumber base, regNumber index, int disp);

void emitIns_ARR_R(instruction ins, emitAttr attr, regNumber reg, regNumber base, regNumber index, int disp);

void emitIns_I_ARX(instruction ins, emitAttr attr, int val, regNumber reg, regNumber rg2, unsigned mul, int disp);

void emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber reg, regNumber base, regNumber index, unsigned scale, int disp);

void emitIns_ARX_R(instruction    ins,
                   emitAttr       attr,
                   regNumber      reg,
                   regNumber      base,
                   regNumber      index,
                   unsigned       scale,
                   cnsval_ssize_t disp);

void emitIns_I_AX(instruction ins, emitAttr attr, int val, regNumber reg, unsigned mul, int disp);

void emitIns_R_AX(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, unsigned mul, int disp);

void emitIns_AX_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, unsigned mul, int disp);

void emitIns_SIMD_R_R_I(instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, int ival);

void emitIns_SIMD_R_R_A(instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, GenTreeIndir* indir);
void emitIns_SIMD_R_R_AR(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber base, int offset);
void emitIns_SIMD_R_R_C(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, CORINFO_FIELD_HANDLE fldHnd, int offs);
void emitIns_SIMD_R_R_R(instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg);
void emitIns_SIMD_R_R_S(instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, int varx, int offs);

#ifdef FEATURE_HW_INTRINSICS
void emitIns_SIMD_R_R_A_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, GenTreeIndir* indir, int ival);
void emitIns_SIMD_R_R_AR_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber base, int ival);
void emitIns_SIMD_R_R_C_I(instruction          ins,
                          emitAttr             attr,
                          regNumber            targetReg,
                          regNumber            op1Reg,
                          CORINFO_FIELD_HANDLE fldHnd,
                          int                  offs,
                          int                  ival);
void emitIns_SIMD_R_R_R_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, int ival);
void emitIns_SIMD_R_R_S_I(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, int varx, int offs, int ival);

void emitIns_SIMD_R_R_R_A(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, GenTreeIndir* indir);
void emitIns_SIMD_R_R_R_AR(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, regNumber base);
void emitIns_SIMD_R_R_R_C(instruction          ins,
                          emitAttr             attr,
                          regNumber            targetReg,
                          regNumber            op1Reg,
                          regNumber            op2Reg,
                          CORINFO_FIELD_HANDLE fldHnd,
                          int                  offs);
void emitIns_SIMD_R_R_R_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, regNumber op3Reg);
void emitIns_SIMD_R_R_R_S(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, int varx, int offs);

void emitIns_SIMD_R_R_A_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, GenTreeIndir* indir);
void emitIns_SIMD_R_R_AR_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, regNumber base);
void emitIns_SIMD_R_R_C_R(instruction          ins,
                          emitAttr             attr,
                          regNumber            targetReg,
                          regNumber            op1Reg,
                          regNumber            op2Reg,
                          CORINFO_FIELD_HANDLE fldHnd,
                          int                  offs);
void emitIns_SIMD_R_R_S_R(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, int varx, int offs);
#endif // FEATURE_HW_INTRINSICS

enum EmitCallType
{
    EC_FUNC_TOKEN, //   Direct call to a helper/static/nonvirtual/global method (call addr with RIP-relative encoding)
    EC_FUNC_TOKEN_INDIR, // Indirect call to a helper/static/nonvirtual/global method (call [addr]/call [rip+addr])
    EC_INDIR_R,          // Indirect call via register (call rax)
    EC_INDIR_ARD,        // Indirect call via an addressing mode (call [rax+rdx*8+disp])

    EC_COUNT
};

// clang-format off
void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,
                  INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                  void*                 addr,
                  ssize_t               argSize,
                  emitAttr              retSize
                  MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                  VARSET_VALARG_TP      ptrVars,
                  regMaskTP             gcrefRegs,
                  regMaskTP             byrefRegs,
                  const DebugInfo& di = DebugInfo(),
                  regNumber             ireg     = REG_NA,
                  regNumber             xreg     = REG_NA,
                  unsigned              xmul     = 0,
                  ssize_t               disp     = 0,
                  bool                  isJump   = false);
// clang-format on

#ifdef TARGET_AMD64
// Is the last instruction emitted a call instruction?
bool emitIsLastInsCall();

// Insert a NOP at the end of the current instruction group if the last emitted instruction was a 'call',
// because the next instruction group will be an epilog.
void emitOutputPreEpilogNOP();
#endif // TARGET_AMD64

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

//------------------------------------------------------------------------
// HasEmbeddedBroadcast: Do we consider embedded broadcast while encoding.
// TODO-XArch-AVX512: Add eventual check on the instrDesc
//
// Arguments:
//    id - Instruction descriptor.
//
// Returns:
//    `true` if the instruction does embedded broadcast.
//
inline bool HasEmbeddedBroadcast(instrDesc* id)
{
    return false;
}

#endif // TARGET_XARCH
