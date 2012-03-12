/*
 * Copyright 2005 Novell Inc
 * Copyright 2011 Xamarin Inc
 */

#ifndef __MONO_ARM_FPA_CODEGEN_H__
#define __MONO_ARM_FPA_CODEGEN_H__

#include "arm-codegen.h"

enum {
	/* FPA registers */
	ARM_FPA_F0,
	ARM_FPA_F1,
	ARM_FPA_F2,
	ARM_FPA_F3,
	ARM_FPA_F4,
	ARM_FPA_F5,
	ARM_FPA_F6,
	ARM_FPA_F7,

	/* transfer length for LDF/STF (T0/T1), already shifted */
	ARM_FPA_SINGLE = 0,
	ARM_FPA_DOUBLE = 1 << 15,

	ARM_FPA_ADF = 0 << 20,
	ARM_FPA_MUF = 1 << 20,
	ARM_FPA_SUF = 2 << 20,
	ARM_FPA_RSF = 3 << 20,
	ARM_FPA_DVF = 4 << 20,
	ARM_FPA_RDF = 5 << 20,
	ARM_FPA_POW = 6 << 20,
	ARM_FPA_RPW = 7 << 20,
	ARM_FPA_RMF = 8 << 20,
	ARM_FPA_FML = 9 << 20,
	ARM_FPA_FDV = 10 << 20,
	ARM_FPA_FRD = 11 << 20,
	ARM_FPA_POL = 12 << 20,

	/* monadic */
	ARM_FPA_MVF = (0 << 20) | (1 << 15),
	ARM_FPA_MNF = (1 << 20) | (1 << 15),
	ARM_FPA_ABS = (2 << 20) | (1 << 15),
	ARM_FPA_RND = (3 << 20) | (1 << 15),
	ARM_FPA_SQT = (4 << 20) | (1 << 15),
	ARM_FPA_LOG = (5 << 20) | (1 << 15),
	ARM_FPA_LGN = (6 << 20) | (1 << 15),
	ARM_FPA_EXP = (7 << 20) | (1 << 15),
	ARM_FPA_SIN = (8 << 20) | (1 << 15),
	ARM_FPA_COS = (9 << 20) | (1 << 15),
	ARM_FPA_TAN = (10 << 20) | (1 << 15),
	ARM_FPA_ASN = (11 << 20) | (1 << 15),
	ARM_FPA_ACS = (12 << 20) | (1 << 15),
	ARM_FPA_ATN = (13 << 20) | (1 << 15),
	ARM_FPA_URD = (14 << 20) | (1 << 15),
	ARM_FPA_NRM = (15 << 20) | (1 << 15),

	/* round modes */
	ARM_FPA_ROUND_NEAREST = 0,
	ARM_FPA_ROUND_PINF = 1,
	ARM_FPA_ROUND_MINF = 2,
	ARM_FPA_ROUND_ZERO = 3,

	/* round precision */
	ARM_FPA_ROUND_SINGLE = 0,
	ARM_FPA_ROUND_DOUBLE = 1,

	/* constants */
	ARM_FPA_CONST_0 = 8,
	ARM_FPA_CONST_1_0 = 9,
	ARM_FPA_CONST_2_0 = 10,
	ARM_FPA_CONST_3_0 = 11,
	ARM_FPA_CONST_4_0 = 12,
	ARM_FPA_CONST_5_0 = 13,
	ARM_FPA_CONST_0_5 = 14,
	ARM_FPA_CONST_10 = 15,
	
	/* compares */
	ARM_FPA_CMF = 4,
	ARM_FPA_CNF = 5,
	ARM_FPA_CMFE = 6,
	ARM_FPA_CNFE = 7,

	/* CPRT ops */
	ARM_FPA_FLT = 0,
	ARM_FPA_FIX = 1,
	ARM_FPA_WFS = 2,
	ARM_FPA_RFS = 3,
	ARM_FPA_WFC = 4,
	ARM_FPA_RFC = 5
};

#define ARM_DEF_FPA_LDF_STF(cond,post,ls,fptype,wback,basereg,fdreg,offset)	\
	((offset) >= 0? (offset)>>2: -(offset)>>2)	|	\
	((1 << 8) | (fptype))				|	\
	((fdreg) << 12)					|	\
	((basereg) << 16)				|	\
	((ls) << 20)					|	\
	((wback) << 21)					|	\
	(((offset) >= 0) << 23)				|	\
	((wback) << 21)					|	\
	((post) << 24)					|	\
	(6 << 25)					|	\
	ARM_DEF_COND(cond)

/* FP load and stores */
#define ARM_FPA_LDFS_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_FPA_LDF_STF((cond),1,ARMOP_LDR,ARM_FPA_SINGLE,0,(base),(freg),(offset)))
#define ARM_FPA_LDFS(p,freg,base,offset)	\
	ARM_FPA_LDFS_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_FPA_LDFD_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_FPA_LDF_STF((cond),1,ARMOP_LDR,ARM_FPA_DOUBLE,0,(base),(freg),(offset)))
#define ARM_FPA_LDFD(p,freg,base,offset)	\
	ARM_FPA_LDFD_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_FPA_STFS_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_FPA_LDF_STF((cond),1,ARMOP_STR,ARM_FPA_SINGLE,0,(base),(freg),(offset)))
#define ARM_FPA_STFS(p,freg,base,offset)	\
	ARM_FPA_STFS_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_FPA_STFD_COND(p,freg,base,offset,cond)	\
	ARM_EMIT((p), ARM_DEF_FPA_LDF_STF((cond),1,ARMOP_STR,ARM_FPA_DOUBLE,0,(base),(freg),(offset)))
#define ARM_FPA_STFD(p,freg,base,offset)	\
	ARM_FPA_STFD_COND(p,freg,base,offset,ARMCOND_AL)

#define ARM_DEF_FPA_CPDO_MONADIC(cond,op,dreg,sreg,round,prec)	\
	(1 << 8) | (14 << 24)		|	\
	(op)				|	\
	((sreg) << 0)			|	\
	((round) << 5)			|	\
	((dreg) << 12)			|	\
	((prec) << 7)			|	\
	ARM_DEF_COND(cond)

#define ARM_DEF_FPA_CPDO_DYADIC(cond,op,dreg,sreg1,sreg2,round,prec)	\
	(1 << 8) | (14 << 24)		|	\
	(op)				|	\
	((sreg1) << 16)			|	\
	((sreg2) << 0)			|	\
	((round) << 5)			|	\
	((dreg) << 12)			|	\
	((prec) << 7)			|	\
	ARM_DEF_COND(cond)

#define ARM_DEF_FPA_CMP(cond,op,sreg1,sreg2)	\
	(1 << 4) | (1 << 8) | (15 << 12)	|	\
	(1 << 20) | (14 << 24)			|	\
	(op) << 21				|	\
	(sreg1) << 16				|	\
	(sreg2)					|	\
	ARM_DEF_COND(cond)

#define ARM_DEF_FPA_CPRT(cond,op,fn,fm,rd,ftype,round)	\
	(1 << 4) | (1 << 8) | (14 << 24)	|	\
	(op) << 20				|	\
	(fm)					|	\
	(fn) << 16				|	\
	(rd) << 12				|	\
	((round) << 5)				|	\
	((ftype) << 7)				|	\
	ARM_DEF_COND(cond)


#include "arm_fpamacros.h"

#define ARM_FPA_RNDDZ_COND(p,dreg,sreg,cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_RND,(dreg),(sreg),ARM_FPA_ROUND_ZERO,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_RNDDZ(p,dreg,sreg)      ARM_FPA_RNDD_COND(p,dreg,sreg,ARMCOND_AL)

/* compares */
#define ARM_FPA_FCMP_COND(p,op,sreg1,sreg2,cond)	\
	ARM_EMIT(p, ARM_DEF_FPA_CMP(cond,op,sreg1,sreg2))
#define ARM_FPA_FCMP(p,op,sreg1,sreg2) ARM_FPA_FCMP_COND(p,op,sreg1,sreg2,ARMCOND_AL)

/* coprocessor register transfer */
#define ARM_FPA_FLTD(p,fn,rd)	\
	ARM_EMIT(p, ARM_DEF_FPA_CPRT(ARMCOND_AL,ARM_FPA_FLT,(fn),0,(rd),ARM_FPA_ROUND_DOUBLE,ARM_FPA_ROUND_NEAREST))
#define ARM_FPA_FLTS(p,fn,rd)	\
	ARM_EMIT(p, ARM_DEF_FPA_CPRT(ARMCOND_AL,ARM_FPA_FLT,(fn),0,(rd),ARM_FPA_ROUND_SINGLE,ARM_FPA_ROUND_NEAREST))

#define ARM_FPA_FIXZ(p,rd,fm)	\
	ARM_EMIT(p, ARM_DEF_FPA_CPRT(ARMCOND_AL,ARM_FPA_FIX,0,(fm),(rd),0,ARM_FPA_ROUND_ZERO))

#define ARM_FPA_WFS(p,rd)	\
	ARM_EMIT(p, ARM_DEF_FPA_CPRT(ARMCOND_AL,ARM_FPA_WFS,0,0,(rd),0,ARM_FPA_ROUND_NEAREST))

#define ARM_FPA_RFS(p,rd)	\
	ARM_EMIT(p, ARM_DEF_FPA_CPRT(ARMCOND_AL,ARM_FPA_RFS,0,0,(rd),0,ARM_FPA_ROUND_NEAREST))

#define ARM_FPA_WFC(p,rd)	\
	ARM_EMIT(p, ARM_DEF_FPA_CPRT(ARMCOND_AL,ARM_FPA_WFC,0,0,(rd),0,ARM_FPA_ROUND_NEAREST))

#define ARM_FPA_RFC(p,rd)	\
	ARM_EMIT(p, ARM_DEF_FPA_CPRT(ARMCOND_AL,ARM_FPA_RFC,0,0,(rd),0,ARM_FPA_ROUND_NEAREST))

#endif /* __MONO_ARM_FPA_CODEGEN_H__ */

