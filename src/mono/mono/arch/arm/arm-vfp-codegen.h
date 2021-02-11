//
// Copyright 2011 Xamarin Inc
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __MONO_ARM_VFP_CODEGEN_H__
#define __MONO_ARM_VFP_CODEGEN_H__

#include "arm-codegen.h"

enum {
	/* VFP registers */
	ARM_VFP_F0,
	ARM_VFP_F1,
	ARM_VFP_F2,
	ARM_VFP_F3,
	ARM_VFP_F4,
	ARM_VFP_F5,
	ARM_VFP_F6,
	ARM_VFP_F7,
	ARM_VFP_F8,
	ARM_VFP_F9,
	ARM_VFP_F10,
	ARM_VFP_F11,
	ARM_VFP_F12,
	ARM_VFP_F13,
	ARM_VFP_F14,
	ARM_VFP_F15,
	ARM_VFP_F16,
	ARM_VFP_F17,
	ARM_VFP_F18,
	ARM_VFP_F19,
	ARM_VFP_F20,
	ARM_VFP_F21,
	ARM_VFP_F22,
	ARM_VFP_F23,
	ARM_VFP_F24,
	ARM_VFP_F25,
	ARM_VFP_F26,
	ARM_VFP_F27,
	ARM_VFP_F28,
	ARM_VFP_F29,
	ARM_VFP_F30,
	ARM_VFP_F31,

	ARM_VFP_D0 = ARM_VFP_F0,
	ARM_VFP_D1 = ARM_VFP_F2,
	ARM_VFP_D2 = ARM_VFP_F4,
	ARM_VFP_D3 = ARM_VFP_F6,
	ARM_VFP_D4 = ARM_VFP_F8,
	ARM_VFP_D5 = ARM_VFP_F10,
	ARM_VFP_D6 = ARM_VFP_F12,
	ARM_VFP_D7 = ARM_VFP_F14,
	ARM_VFP_D8 = ARM_VFP_F16,
	ARM_VFP_D9 = ARM_VFP_F18,
	ARM_VFP_D10 = ARM_VFP_F20,
	ARM_VFP_D11 = ARM_VFP_F22,
	ARM_VFP_D12 = ARM_VFP_F24,
	ARM_VFP_D13 = ARM_VFP_F26,
	ARM_VFP_D14 = ARM_VFP_F28,
	ARM_VFP_D15 = ARM_VFP_F30,

	ARM_VFP_COPROC_SINGLE = 10,
	ARM_VFP_COPROC_DOUBLE = 11,

#define ARM_VFP_OP(p,q,r,s) (((p) << 23) | ((q) << 21) | ((r) << 20) | ((s) << 6))
#define ARM_VFP_OP2(Fn,N) (ARM_VFP_OP (1,1,1,1) | ((Fn) << 16) | ((N) << 7))

	ARM_VFP_MUL = ARM_VFP_OP (0,1,0,0),
	ARM_VFP_NMUL = ARM_VFP_OP (0,1,0,1),
	ARM_VFP_ADD = ARM_VFP_OP (0,1,1,0),
	ARM_VFP_SUB = ARM_VFP_OP (0,1,1,1),
	ARM_VFP_DIV = ARM_VFP_OP (1,0,0,0),

	ARM_VFP_CPY = ARM_VFP_OP2 (0,0),
	ARM_VFP_ABS = ARM_VFP_OP2 (0,1),
	ARM_VFP_NEG = ARM_VFP_OP2 (1,0),
	ARM_VFP_SQRT = ARM_VFP_OP2 (1,1),
	ARM_VFP_CMP = ARM_VFP_OP2 (4,0),
	ARM_VFP_CMPE = ARM_VFP_OP2 (4,1),
	ARM_VFP_CMPZ = ARM_VFP_OP2 (5,0),
	ARM_VFP_CMPEZ = ARM_VFP_OP2 (5,1),
	ARM_VFP_CVT = ARM_VFP_OP2 (7,1),
	ARM_VFP_UITO = ARM_VFP_OP2 (8,0),
	ARM_VFP_SITO = ARM_VFP_OP2 (8,1),
	ARM_VFP_TOUI = ARM_VFP_OP2 (12,0),
	ARM_VFP_TOSI = ARM_VFP_OP2 (13,0),
	ARM_VFP_TOUIZ = ARM_VFP_OP2 (12,1),
	ARM_VFP_TOSIZ = ARM_VFP_OP2 (13,1),

	ARM_VFP_SID = 0,
	ARM_VFP_SCR = 1 << 1,
	ARM_VFP_EXC = 8 << 1
};

#define ARM_DEF_VFP_DYADIC(cond,cp,op,Fd,Fn,Fm)	\
	(14 << 24)				|	\
	((cp) << 8)				|	\
	(op)					|	\
	(((Fd) >> 1) << 12)			|	\
	(((Fd) & 1) << 22)			|	\
	(((Fn) >> 1) << 16)			|	\
	(((Fn) & 1) << 7)			|	\
	(((Fm) >> 1) << 0)			|	\
	(((Fm) & 1) << 5)			|	\
	ARM_DEF_COND(cond)

#define ARM_DEF_VFP_MONADIC(cond,cp,op,Fd,Fm)	\
	(14 << 24)				|	\
	((cp) << 8)				|	\
	(op)					|	\
	(((Fd) >> 1) << 12)			|	\
	(((Fd) & 1) << 22)			|	\
	(((Fm) >> 1) << 0)			|	\
	(((Fm) & 1) << 5)			|	\
	ARM_DEF_COND(cond)

#define ARM_DEF_VFP_LSF(cond,cp,post,ls,wback,basereg,Fd,offset)	\
	((offset) >= 0? (offset)>>2: -(offset)>>2)	|	\
	(6 << 25)					|	\
	((cp) << 8)					|	\
	(((Fd) >> 1) << 12)				|	\
	(((Fd) & 1) << 22)				|	\
	((basereg) << 16)				|	\
	((ls) << 20)					|	\
	((wback) << 21)					|	\
	(((offset) >= 0) << 23)				|	\
	((wback) << 21)					|	\
	((post) << 24)					|	\
	ARM_DEF_COND(cond)

#define ARM_DEF_VFP_CPT(cond,cp,op,L,Fn,Rd)	\
	(14 << 24)				|	\
	(1 << 4)				|	\
	((cp) << 8)				|	\
	((op) << 21)				|	\
	((L) << 20)				|	\
	((Rd) << 12)				|	\
	(((Fn) >> 1) << 16)			|	\
	(((Fn) & 1) << 7)			|	\
	ARM_DEF_COND(cond)

/* FP load and stores */
#define ARM_FLDS_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_VFP_LSF((cond),ARM_VFP_COPROC_SINGLE,1,ARMOP_LDR,0,(base),(freg),(offset)))
#define ARM_FLDS(p,freg,base,offset)	\
	ARM_FLDS_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_FLDD_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_VFP_LSF((cond),ARM_VFP_COPROC_DOUBLE,1,ARMOP_LDR,0,(base),(freg),(offset)))
#define ARM_FLDD(p,freg,base,offset)	\
	ARM_FLDD_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_FSTS_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_VFP_LSF((cond),ARM_VFP_COPROC_SINGLE,1,ARMOP_STR,0,(base),(freg),(offset)))
#define ARM_FSTS(p,freg,base,offset)	\
	ARM_FSTS_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_FSTD_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_VFP_LSF((cond),ARM_VFP_COPROC_DOUBLE,1,ARMOP_STR,0,(base),(freg),(offset)))
#define ARM_FSTD(p,freg,base,offset)	\
	ARM_FSTD_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_FLDMD_COND(p,first_reg,nregs,base,cond)							\
	ARM_EMIT((p), ARM_DEF_VFP_LSF((cond),ARM_VFP_COPROC_DOUBLE,0,ARMOP_LDR,0,(base),(first_reg),((nregs) * 2) << 2))

#define ARM_FLDMD(p,first_reg,nregs,base)		\
	ARM_FLDMD_COND(p,first_reg,nregs,base,ARMCOND_AL)

#define ARM_FSTMD_COND(p,first_reg,nregs,base,cond)							\
	ARM_EMIT((p), ARM_DEF_VFP_LSF((cond),ARM_VFP_COPROC_DOUBLE,0,ARMOP_STR,0,(base),(first_reg),((nregs) * 2) << 2))

#define ARM_FSTMD(p,first_reg,nregs,base)		\
	ARM_FSTMD_COND(p,first_reg,nregs,base,ARMCOND_AL)

#include <mono/arch/arm/arm_vfpmacros.h>

/* coprocessor register transfer */
#define ARM_FMSR(p,freg,reg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_SINGLE,0,0,(freg),(reg)))
#define ARM_FMRS(p,reg,freg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_SINGLE,0,1,(freg),(reg)))

#define ARM_FMDLR(p,freg,reg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_DOUBLE,0,0,(freg),(reg)))
#define ARM_FMRDL(p,reg,freg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_DOUBLE,0,1,(freg),(reg)))
#define ARM_FMDHR(p,freg,reg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_DOUBLE,1,0,(freg),(reg)))
#define ARM_FMRDH(p,reg,freg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_DOUBLE,1,1,(freg),(reg)))

#define ARM_FMXR(p,freg,reg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_SINGLE,7,0,(freg),(reg)))
#define ARM_FMRX(p,reg,fcreg)	\
	ARM_EMIT((p), ARM_DEF_VFP_CPT(ARMCOND_AL,ARM_VFP_COPROC_SINGLE,7,1,(fcreg),(reg)))

#define ARM_FMSTAT(p)   \
	ARM_FMRX((p),ARMREG_R15,ARM_VFP_SCR)

#define ARM_DEF_MCRR(cond,cp,rn,rd,Fm,M) \
	((Fm) << 0) |					   \
	(1 << 4)   |					   \
	((M) << 5) |					   \
	((cp) << 8) |					   \
	((rd) << 12) |					   \
	((rn) << 16) |					   \
	((2) << 21) |					   \
	(12 << 24) |					   \
	ARM_DEF_COND(cond)

#define ARM_FMDRR(p,rd,rn,dm)   \
	ARM_EMIT((p), ARM_DEF_MCRR(ARMCOND_AL,ARM_VFP_COPROC_DOUBLE,(rn),(rd),(dm) >> 1, (dm) & 1))

#define ARM_DEF_FMRRD(cond,cp,rn,rd,Dm,D)		\
	((Dm) << 0) |					   \
	(1 << 4)   |					   \
	((cp) << 8) |					   \
	((rd) << 12) |					   \
	((rn) << 16) |					   \
	((0xc5) << 20) |					   \
	ARM_DEF_COND(cond)

#define ARM_FMRRD(p,rd,rn,dm)   \
	ARM_EMIT((p), ARM_DEF_FMRRD(ARMCOND_AL,ARM_VFP_COPROC_DOUBLE,(rn),(rd),(dm) >> 1, (dm) & 1))

#define ARM_DEF_FUITOS(cond,Dd,D,Fm,M) ((cond) << 28) | ((0x1d) << 23) | ((D) << 22) | ((0x3) << 20) | ((8) << 16) | ((Dd) << 12) | ((0xa) << 8) | ((1) << 6) | ((M) << 5) | ((Fm) << 0)

#define ARM_FUITOS(p,dreg,sreg) \
	ARM_EMIT((p), ARM_DEF_FUITOS (ARMCOND_AL, (dreg) >> 1, (dreg) & 1, (sreg) >> 1, (sreg) & 1))

#define ARM_DEF_FUITOD(cond,Dd,D,Fm,M) ((cond) << 28) | ((0x1d) << 23) | ((D) << 22) | ((0x3) << 20) | ((8) << 16) | ((Dd) << 12) | ((0xb) << 8) | ((1) << 6) | ((M) << 5) | ((Fm) << 0)

#define ARM_FUITOD(p,dreg,sreg) \
	ARM_EMIT((p), ARM_DEF_FUITOD (ARMCOND_AL, (dreg) >> 1, (dreg) & 1, (sreg) >> 1, (sreg) & 1))

#define ARM_DEF_FSITOS(cond,Dd,D,Fm,M) ((cond) << 28) | ((0x1d) << 23) | ((D) << 22) | ((0x3) << 20) | ((8) << 16) | ((Dd) << 12) | ((0xa) << 8) | ((1) << 7) | ((1) << 6) | ((M) << 5) | ((Fm) << 0)

#define ARM_FSITOS(p,dreg,sreg) \
	ARM_EMIT((p), ARM_DEF_FSITOS (ARMCOND_AL, (dreg) >> 1, (dreg) & 1, (sreg) >> 1, (sreg) & 1))

#define ARM_DEF_FSITOD(cond,Dd,D,Fm,M) ((cond) << 28) | ((0x1d) << 23) | ((D) << 22) | ((0x3) << 20) | ((8) << 16) | ((Dd) << 12) | ((0xb) << 8) | ((1) << 7) | ((1) << 6) | ((M) << 5) | ((Fm) << 0)

#define ARM_FSITOD(p,dreg,sreg) \
	ARM_EMIT((p), ARM_DEF_FSITOD (ARMCOND_AL, (dreg) >> 1, (dreg) & 1, (sreg) >> 1, (sreg) & 1))

#endif /* __MONO_ARM_VFP_CODEGEN_H__ */

