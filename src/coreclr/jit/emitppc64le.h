// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_POWERPC64)

#define ppc_emit32(c,x) do { *((uint32_t *) (c)) = (uint32_t) (x); (c) = ((uint8_t *)(c) + sizeof (uint32_t));} while (0)
#define   ppc_ori(c,S,A,ui) ppc_emit32 (c, (24 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(ui))
#define          ppc_nop(c)       ppc_ori    (c, 0, 0, 0)
#define   ppc_blr(c)       ppc_emit32 (c, 0x4e800020)

// The POWERPC64 instructions are all 32 bits in size.
// we use an unsigned int to hold the encoded instructions.
// This typedef defines the type that we use to hold encoded instructions.

//TODO POWERPC64

typedef unsigned int code_t;
/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
instrDesc* emitNewInstrCallDir(int              argCnt,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize,
                               emitAttr         secondRetSize);

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize,
                               emitAttr         secondRetSize);

/************************************************************************/
/*   enum to allow instruction optimisation to specify register order   */
/************************************************************************/
enum RegisterOrder
{
    eRO_none = 0,
    eRO_ascending,
    eRO_descending
};

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool     emitInsIsCompare(instruction ins);
bool     emitInsIsLoad(instruction ins);
bool     emitInsIsStore(instruction ins);
bool     emitInsIsLoadOrStore(instruction ins);
bool     emitInsIsVectorRightShift(instruction ins);
bool     emitInsIsVectorLong(instruction ins);
bool     emitInsIsVectorNarrow(instruction ins);
bool     emitInsIsVectorWide(instruction ins);
bool     emitInsDestIsOp2(instruction ins);
emitAttr emitInsTargetRegSize(instrDesc* id);
emitAttr emitInsLoadStoreSize(instrDesc* id);
bool IsRedundantMov(instruction ins, emitAttr size, regNumber dst, regNumber src, bool canSkip);
bool IsMovInstruction(instruction ins);

public:
inline static bool isFloatReg(regNumber reg)
{
    return (reg >= REG_F0 && reg <= REG_F31);
}

inline static bool isGeneralRegister(regNumber reg)
{
    return (reg >= REG_R0 && reg <= REG_R31);
} // Excludes REG_ZR

inline static bool insOptsNone(insOpts opt)
{
    return (opt == INS_OPTS_NONE);
}


void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

enum EmitCallType
{
    EC_FUNC_TOKEN, // Direct call to a helper/static/nonvirtual/global method
    EC_INDIR_R,    // Indirect call via register
    EC_COUNT
};

void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,
                  INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                  void*            addr,
                  ssize_t          argSize,
                  emitAttr         retSize,
                  emitAttr         secondRetSize,
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  const DebugInfo& di,
                  regNumber        ireg,
                  regNumber        xreg,
                  unsigned         xmul,
                  ssize_t          disp,
                  bool             isJump,
                  bool             noSafePoint = false);

void emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt = INS_OPTS_NONE);


/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);

void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);

void emitInsSve_I(instruction ins, emitAttr attr, ssize_t imm);

void emitIns_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt = INS_OPTS_NONE);

void emitInsSve_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt = INS_OPTS_NONE);

void emitIns_R_I(instruction     ins,
                 emitAttr        attr,
                 regNumber       reg,
                 ssize_t         imm,
                 insOpts         opt  = INS_OPTS_NONE,
                 insScalableOpts sopt = INS_SCALABLE_OPTS_NONE DEBUGARG(size_t targetHandle = 0)
                     DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

void emitIns_R_R(instruction     ins,
		emitAttr        attr,
		regNumber       reg1,
		regNumber       reg2,
		insOpts         opt  = INS_OPTS_NONE,
		insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitInsSve_R_R(instruction     ins,
		emitAttr        attr,
		regNumber       reg1,
		regNumber       reg2,
		insOpts         opt  = INS_OPTS_NONE,
		insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags)
{
	emitIns_R_R(ins, attr, reg1, reg2);
}


void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);






#endif
