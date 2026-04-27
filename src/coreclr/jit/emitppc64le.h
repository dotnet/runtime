// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_POWERPC64)

#define ppc_emit32(c,x) do { *((uint32_t *) (c)) = (uint32_t) (x); (c) = ((uint8_t *)(c) + sizeof (uint32_t));} while (0)

// Branch instructions
#define ppc_blr(c)         ppc_emit32 (c, 0x4e800020)
#define ppc_blrl(c)        ppc_emit32 (c, 0x4e800021)
#define ppc_b(c,li)        ppc_emit32 (c, (18 << 26) | ((li) << 2))
#define ppc_bl(c,li)       ppc_emit32 (c, (18 << 26) | ((li) << 2) | 1)
#define ppc_bcx(c,BO,BI,BD,AA,LK) ppc_emit32(c, (16 << 26) | ((BO) << 21 )| ((BI) << 16) | (BD << 2) | ((AA) << 1) | LK)
#define ppc_bc(c,BO,BI,BD) ppc_bcx(c,BO,BI,BD,0,0)
#define ppc_bcctrx(c,BO,BI,LK) ppc_emit32(c, (19 << 26) | (BO << 21 )| (BI << 16) | (0 << 11) | (528 << 1) | LK)
#define ppc_bcctr(c,BO,BI) ppc_bcctrx(c,BO,BI,0)
#define ppc_bcctrl(c,BO,BI) ppc_bcctrx(c,BO,BI,1)

// Branch condition codes
#define PPC_BR_FALSE  4
#define PPC_BR_TRUE   12
#define PPC_BR_LT     0
#define PPC_BR_GT     1
#define PPC_BR_EQ     2

// Special purpose register instructions
#define ppc_mfspr(c,D,spr) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((spr) << 11) | (339 << 1))
#define ppc_mflr(c,D)      ppc_mfspr  (c, D, 256)
#define ppc_mtspr(c,spr,S) ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((spr) << 11) | (467 << 1))
#define ppc_mtlr(c,S)      ppc_mtspr  (c, 256, S)
#define ppc_mtctr(c,S)     ppc_mtspr  (c, 288, S)

// Logical instructions
#define ppc_or(c,a,s,b)    ppc_emit32 (c, (31 << 26) | ((s) << 21) | ((a) << 16) | ((b) << 11) | 888)
#define ppc_mr(c,a,s)      ppc_or     (c, a, s, s)
#define ppc_ori(c,S,A,ui)  ppc_emit32 (c, (24 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(ui))
#define ppc_oris(c,S,A,ui) ppc_emit32 (c, (25 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(ui))
#define ppc_nop(c)         ppc_ori    (c, 0, 0, 0)

// Arithmetic instructions
#define ppc_addi(c,D,A,i)  ppc_emit32 (c, (14 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(i))
#define ppc_addis(c,D,A,i) ppc_emit32 (c, (15 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(i))
#define ppc_li(c,D,v)      ppc_addi   (c, D, 0, (uint16_t)(v))
#define ppc_lis(c,D,v)     ppc_addis  (c, D, 0, (uint16_t)(v))

// Rotate and shift instructions
#define ppc_rldicr(c,A,S,n,b) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | (((n) & 0x1f) << 11) | (((b) & 0x1f) << 6) | ((((n) & 0x20) >> 5) << 1) | ((((b) & 0x20) >> 5) << 5) | (1 << 2))
#define ppc_sldi(c,A,S,n)  ppc_rldicr(c, A, S, n, 63 - (n))

// Compare instructions
#define ppc_cmp(c,cfrD,L,A,B)   ppc_emit32(c, (31 << 26) | ((cfrD) << 23) | (0 << 22) | ((L) << 21) | ((A) << 16) | ((B) << 11) | (0 << 1) | 0)
#define ppc_cmpi(c,cfrD,L,A,B)  ppc_emit32(c, (11 << 26) | (cfrD << 23) | (0 << 22) | (L << 21) | (A << 16) | (uint16_t)(B))
#define ppc_cmpw(c,cfrD,A,B)    ppc_cmp(c, (cfrD), 0, (A), (B))
#define ppc_cmpd(c,cfrD,A,B)    ppc_cmp(c, (cfrD), 1, (A), (B))
#define ppc_cmpwi(c,cfrD,A,B)   ppc_cmpi(c, (cfrD), 0, (A), (B))
#define ppc_cmpdi(c,cfrD,A,B)   ppc_cmpi(c, (cfrD), 1, (A), (B))

// Load instructions
#define ppc_lbz(c,D,d,A)   ppc_emit32 (c, (34 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lhz(c,D,d,A)   ppc_emit32 (c, (40 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lha(c,D,d,A)   ppc_emit32 (c, (42 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lwz(c,D,d,A)   ppc_emit32 (c, (32 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lwa(c,D,ds,A)  ppc_emit32 (c, (58 << 26) | ((D) << 21) | ((A) << 16) | ((ds) & 0xfffc) | 2)
#define ppc_ld(c,D,ds,A)   ppc_emit32 (c, (58 << 26) | ((D) << 21) | ((A) << 16) | ((uint32_t)(ds) & 0xfffc) | 0)
#define ppc_lfd(c,D,d,A)   ppc_emit32 (c, (50 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))

// Store instructions
#define ppc_stb(c,S,d,A)   ppc_emit32 (c, (38 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_sth(c,S,d,A)   ppc_emit32 (c, (44 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_stw(c,S,d,A)   ppc_emit32 (c, (36 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_std(c,S,ds,A)  ppc_emit32 (c, (62 << 26) | ((S) << 21) | ((A) << 16) | ((uint32_t)(ds) & 0xfffc) | 0)
#define ppc_stdu(c,S,ds,A) ppc_emit32 (c, (62 << 26) | ((S) << 21) | ((A) << 16) | ((uint32_t)(ds) & 0xfffc) | 1)
#define ppc_stfd(c,S,d,a)  ppc_emit32 (c, (54 << 26) | ((S) << 21) | ((a) << 16) | (uint16_t)(d))

// Trap instruction
#define ppc_trap(c)        ppc_emit32 (c, 0x7FE00008)

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

void emitIns_R_R_I(instruction ins,
                   emitAttr    attr,
                   regNumber   reg1,
                   regNumber   reg2,
                   ssize_t     imm,
                   insOpts     opt = INS_OPTS_NONE);

bool emitIns_valid_imm_for_li(ssize_t imm);

void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);

void emitIns_J(instruction ins, BasicBlock* dst, int instrCount = 0);

#endif
#ifdef DEBUG
const char* emitDisInsName(code_t code, const BYTE* addr, instrDesc* id);
#endif

size_t emitInsSize(instrDesc* id);
